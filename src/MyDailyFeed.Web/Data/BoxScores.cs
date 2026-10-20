using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace MyDailyFeed.Web.Data;

public sealed record BoxIdentity(string Sport, string League, string EventId)
{
    public string Key => Sport + "|" + League + "|" + EventId;
    public bool Valid => Regex.IsMatch(Sport ?? "", "^[a-z-]{1,40}$") && Regex.IsMatch(League ?? "", "^[a-z0-9.-]{1,60}$")
        && Regex.IsMatch(EventId ?? "", "^[0-9]{1,20}$") && League != "womens-college-basketball";
    public string Href => $"box-score/{Uri.EscapeDataString(Sport)}/{Uri.EscapeDataString(League)}/{Uri.EscapeDataString(EventId)}";
    public static BoxIdentity? Match(string provider, int gameId) => provider switch {
        "espn-mls" => new("soccer", "usa.1", gameId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        "espn-championship" => new("soccer", "eng.2", gameId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        "espn-nhl" => new("hockey", "nhl", gameId.ToString(System.Globalization.CultureInfo.InvariantCulture)), _ => null };
}
public sealed record BoxStat(string? Group, string Key, string Label, string? Value);
public sealed record BoxLine(string? Period, string? DisplayValue);
public sealed record BoxTeam(string Id, string Name, string? HomeAway, IReadOnlyList<BoxStat> Statistics, IReadOnlyList<BoxLine> Lines);
public sealed record BoxColumn(string? Key, string Label, string? Description);
public sealed record BoxAthlete(string Name, string? Position, bool? Starter, IReadOnlyList<string?> Stats);
public sealed record BoxPlayers(string TeamId, string Name, IReadOnlyList<BoxColumn> Columns, IReadOnlyList<BoxAthlete> Athletes, IReadOnlyList<string?> Totals);
public sealed record BoxData(BoxIdentity Identity, IReadOnlyList<BoxTeam> Teams, IReadOnlyList<BoxPlayers> Players)
{
    public string Label => Players.Count > 0 ? "Box score" : "Game stats";
    private static string? Text(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key,out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static IEnumerable<JsonElement> Array(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key,out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray() : [];
    private static IReadOnlyList<string?> Values(JsonElement e, string key) => Array(e,key).Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : null).ToList();
    public static BoxData? Parse(string? json, BoxIdentity id)
    {
        if (!id.Valid || string.IsNullOrWhiteSpace(json)) return null;
        try {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 48 });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version",out var version) || !version.TryGetInt32(out var n) || n != 1
                || Text(root,"sport") != id.Sport || Text(root,"league") != id.League || Text(root,"eventId") != id.EventId) return null;
            var teams = Array(root,"teams").Select(t => new BoxTeam(Text(t,"id") ?? "", Text(t,"name") ?? "Team", Text(t,"homeAway"),
                Array(t,"statistics").Where(s => !string.IsNullOrWhiteSpace(Text(s,"key")) && !string.IsNullOrWhiteSpace(Text(s,"label")))
                    .Select(s=>new BoxStat(Text(s,"group"),Text(s,"key")!,Text(s,"label")!,Text(s,"value"))).ToList(),
                Array(t,"lineScores").Select(s=>new BoxLine(s.ValueKind==JsonValueKind.Object && s.TryGetProperty("period",out var period) && period.ValueKind==JsonValueKind.Number ? period.GetRawText() : Text(s,"period"),Text(s,"displayValue"))).Where(s=>s.DisplayValue!=null).ToList()))
                .OrderBy(t=> t.HomeAway == "away" ? 0 : t.HomeAway == "home" ? 1 : 2).ToList();
            var players = new List<BoxPlayers>();
            foreach (var group in Array(root,"playerGroups")) {
                var team = Text(group,"teamId");
                if (string.IsNullOrWhiteSpace(team) || !teams.Any(t=>t.Id==team)) continue;
                // Retain original indices when skipping columns with unusable labels.
                var columns = Array(group,"columns").Select((c,index)=>new { Column=new BoxColumn(Text(c,"key"),Text(c,"label") ?? "",Text(c,"description")), Index=index })
                    .Where(c=>!string.IsNullOrWhiteSpace(c.Column.Label)).ToList();
                if (columns.Count == 0) continue;
                IReadOnlyList<string?> SelectValues(IReadOnlyList<string?> values) => columns.Select(c=>c.Index<values.Count ? values[c.Index] : null).ToList();
                var athletes = Array(group,"athletes").Where(a=>!string.IsNullOrWhiteSpace(Text(a,"name"))).Select(a=>new BoxAthlete(Text(a,"name")!,Text(a,"position"),
                    a.TryGetProperty("starter",out var starter) && starter.ValueKind is JsonValueKind.True or JsonValueKind.False ? starter.GetBoolean() : null,
                    SelectValues(Values(a,"stats")))).Where(a=>a.Stats.Any(v=>v!=null)).ToList();
                var totals = SelectValues(Values(group,"totals"));
                if (athletes.Count>0 || totals.Any(v=>v!=null)) players.Add(new(team,Text(group,"name") ?? "Players",columns.Select(c=>c.Column).ToList(),athletes,totals));
            }
            return teams.Any(t=>t.Statistics.Any(s=>s.Value!=null)) || players.Count>0 ? new(id,teams,players) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { return null; }
    }
}
public sealed class BoxSnapshot
{
    public string Sport {get;set;} = "";
    public string League {get;set;} = "";
    public string EventId {get;set;} = "";
    public bool HasBoxScore {get;set;}
    public string Availability {get;set;} = "";
    public string? Payload {get;set;}
    public DateTime? FetchedAt {get;set;}
    public string? SummaryUrl {get;set;}
    public string? BoxScoreUrl {get;set;}
}
public sealed record BoxDetail(BoxData Data, DateTime? Updated, string? ProviderUrl);
public sealed class BoxScoresRepository(IDbContextFactory<WebScraperContext> factory, ILogger<BoxScoresRepository> log)
{
    public async Task<IReadOnlyDictionary<string,string>> AvailableAsync(IEnumerable<BoxIdentity> identities)
    {
        var ids=identities.Where(i=>i.Valid).Distinct().Take(200).ToList();
        if(ids.Count==0)return new Dictionary<string,string>();
        try {
            await using var db=await factory.CreateDbContextAsync();
            var parameters=ids.Select((id,i)=>new SqlParameter("@k"+i,id.Key)).ToArray();
            var sql="SELECT Sport,League,EventId,HasBoxScore,Availability,Payload,FetchedAt,SummaryUrl,BoxScoreUrl FROM dbo.SportsBoxScores WHERE Sport+N'|'+League+N'|'+EventId IN ("+string.Join(",",parameters.Select(p=>p.ParameterName))+")";
            var rows=await db.Database.SqlQueryRaw<BoxSnapshot>(sql,parameters.Cast<object>().ToArray()).ToListAsync();
            // Validate cached JSON in one bounded batch, so corrupt/unsupported entries never advertise a dead action.
            return rows.Where(r=>r.HasBoxScore && r.Availability!="unavailable").Select(r=>BoxData.Parse(r.Payload,new(r.Sport,r.League,r.EventId)))
                .OfType<BoxData>().DistinctBy(d=>d.Identity.Key).ToDictionary(d=>d.Identity.Key,d=>d.Label);
        } catch(Exception ex) when(ex is DbException or InvalidOperationException) {log.LogWarning(ex,"Optional box score availability unavailable");return new Dictionary<string,string>();}
    }
    public async Task<BoxDetail?> GetAsync(BoxIdentity id)
    {
        if(!id.Valid)return null;
        try {
            await using var db=await factory.CreateDbContextAsync();
            // Match only games which are still public through existing followed results or current season source queries.
            var ordinary=await db.ResultGames.AsNoTracking().AnyAsync(g=>g.Sport==id.Sport && g.League==id.League && g.EventId==id.EventId && g.Completed
                && db.ResultFollows.Any(f=>f.Enabled && f.Sport==g.Sport && f.League==g.League && (f.Kind=="league"
                    || f.Kind=="team" && (f.TeamId==g.HomeTeamId || f.TeamId==g.AwayTeamId)
                    || f.Kind=="conference" && db.ResultFollowTeams.Any(t=>t.FollowId==f.Id && (t.TeamId==g.HomeTeamId || t.TeamId==g.AwayTeamId)))));
            var seasonVisible=false;
            if(!ordinary && id.Sport=="soccer" && int.TryParse(id.EventId,out var gameId)) {
                if(id.League=="usa.1") { var page=await MlsResultsQuery.GetAsync(db); seasonVisible=page.Source!=null && await MlsResultsQuery.Visible(db,page.Source.Id).AnyAsync(g=>g.Season==page.Season && g.GameId==gameId); }
                if(id.League=="eng.2") { var page=await ChampionshipResultsQuery.GetAsync(db,false); seasonVisible=page.Source!=null && await ChampionshipResultsQuery.Visible(db,page.Source.Id).AnyAsync(g=>g.Season==page.Season && g.GameId==gameId); }
            }
            if(!ordinary && !seasonVisible && id.Sport=="hockey" && id.League=="nhl"
                && int.TryParse(id.EventId,out var nhlGameId))
                seasonVisible=await NhlBoxScoreVisibility.ContainsAsync(db,nhlGameId);
            if(!ordinary && !seasonVisible)return null;
            var row=await db.Database.SqlQuery<BoxSnapshot>($"SELECT Sport,League,EventId,HasBoxScore,Availability,Payload,FetchedAt,SummaryUrl,BoxScoreUrl FROM dbo.SportsBoxScores WHERE Sport={id.Sport} AND League={id.League} AND EventId={id.EventId}").SingleOrDefaultAsync();
            if(row?.HasBoxScore!=true || row.Availability=="unavailable" || BoxData.Parse(row.Payload,id) is not {} data)return null;
            return new(data,row.FetchedAt,ProviderUrl(row.BoxScoreUrl,id) ?? ProviderUrl(row.SummaryUrl,id));
        }catch(Exception ex) when(ex is DbException or InvalidOperationException){log.LogWarning(ex,"Optional box score detail unavailable");return null;}
    }
    public static string? ProviderUrl(string? value,BoxIdentity id) => Uri.TryCreate(value,UriKind.Absolute,out var uri) && uri.Scheme=="https"
        && (uri.Host=="espn.com" || uri.Host.EndsWith(".espn.com",StringComparison.OrdinalIgnoreCase))
        && uri.Segments.Any(s=>s.Trim('/')==id.EventId) ? uri.AbsoluteUri : null;
}
