using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
namespace MyDailyFeed.Web.Watching;

public sealed record WatchTitle(int Id,string Kind,string Name,string? Year,string? Poster,string Overview,IReadOnlyList<string>? MatchingServices=null,DateOnly? ReleaseDate=null,double? Popularity=null);
public sealed record WatchListing(IReadOnlyList<WatchTitle> Items,int Pages,bool Failed=false);
public sealed record WatchOffer(string Category,IReadOnlyList<string> Providers);
public sealed record WatchAvailability(WatchTitle Title,IReadOnlyList<WatchOffer> Offers,string? Link,DateTime Checked);
public sealed class WatchCatalog(IHttpClientFactory factory,IConfiguration configuration)
{
    private readonly MemoryCache cache=new(new MemoryCacheOptions {SizeLimit=512});
    private T Save<T>(string key,T value,TimeSpan ttl)=>cache.Set(key,value,new MemoryCacheEntryOptions {Size=1,AbsoluteExpirationRelativeToNow=ttl});
    private readonly SemaphoreSlim gate=new(4);
    public bool Configured=>!string.IsNullOrWhiteSpace(configuration["Movies:AccessToken"]);
    public static bool Kind(string kind)=>kind is "movie" or "tv";
    public static string Term(string? query)=>Regex.Replace(query?.Trim() ?? "",@"\s+"," ") is {} s ? s[..Math.Min(s.Length,120)] : "";
    public static readonly IReadOnlyDictionary<int,string> Genres=new Dictionary<int,string>{{16,"Animation"},{35,"Comedy"},{80,"Crime"},{99,"Documentary"},{18,"Drama"}};
    private async Task<JsonDocument?> Fetch(string path)
    {
        if(!Configured)return null;
        await gate.WaitAsync();
        try {
            using var request=new HttpRequestMessage(HttpMethod.Get,path);
            request.Headers.Authorization=new("Bearer",configuration["Movies:AccessToken"]);
            using var response=await factory.CreateClient("watch-catalog").SendAsync(request);
            if(!response.IsSuccessStatusCode)return null;
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        } catch(Exception e) when(e is HttpRequestException or TaskCanceledException or JsonException){return null;}
        finally{gate.Release();}
    }
    public async Task<WatchListing> List(string kind,string? query,int genre=0,int page=1)
    {
        if(!Kind(kind))return new([],0,true);
        var term=Term(query);page=Math.Clamp(page,1,10);genre=Genres.ContainsKey(genre)?genre:0;
        var key=$"watch:list:{kind}:{term}:{genre}:{page}";
        if(cache.TryGetValue<WatchListing>(key,out var saved))return saved!;
        var path=term.Length>0 ? $"search/{kind}?query={Uri.EscapeDataString(term)}&include_adult=false&page={page}" : $"discover/{kind}?include_adult=false&sort_by=popularity.desc&page={page}&watch_region=US"+(genre>0?$"&with_genres={genre}":"");
        // TMDB: returning, planned, in production or pilot; exclude ended/canceled only in TV browsing.
        if(kind=="tv" && term.Length==0)path+="&with_status=0%7C1%7C2%7C5";
        using var doc=await Fetch(path);
        if(doc==null)return Save(key,new WatchListing([],0,true),TimeSpan.FromSeconds(30));
        try {
            var root=doc.RootElement;
            var items=root.GetProperty("results").EnumerateArray().Where(x=>!x.TryGetProperty("adult",out var a)||a.ValueKind!=JsonValueKind.True).Select(x=>Title(x,kind)).Where(x=>x.Id>0&&!string.IsNullOrWhiteSpace(x.Name)).ToList();
            return Save(key,new WatchListing(items,Math.Min(10,root.GetProperty("total_pages").GetInt32())),TimeSpan.FromMinutes(15));
        }catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or FormatException){return new([],0,true);}
    }
    public async Task<WatchAvailability?> Detail(string kind,int id)
    {
        if(!Kind(kind)||id<=0)return null;
        var key=$"watch:detail:{kind}:{id}";
        if(cache.TryGetValue<WatchAvailability>(key,out var saved))return saved;
        using var title=await Fetch($"{kind}/{id}");using var providers=await Fetch($"{kind}/{id}/watch/providers");
        if(title==null||providers==null)return null;
        try {
            if(title.RootElement.TryGetProperty("adult",out var adult)&&adult.ValueKind==JsonValueKind.True)return null;
            var offers=new List<WatchOffer>();string? link=null;
            if(providers.RootElement.TryGetProperty("results",out var regions)&&regions.TryGetProperty("US",out var us)){
                foreach(var (keyName,label) in new[]{("flatrate","Subscription"),("free","Free"),("ads","Free with ads"),("rent","Rent"),("buy","Buy")}){
                    if(us.TryGetProperty(keyName,out var list)&&list.ValueKind==JsonValueKind.Array){
                        var names=list.EnumerateArray().Select(x=>Text(x,"provider_name")).Where(x=>!string.IsNullOrWhiteSpace(x)&&HomeViewingService(0,x)).Cast<string>().Distinct().ToList();
                        if(names.Count>0)offers.Add(new(label,names));
                    }
                }
                var raw=Text(us,"link");
                if(Uri.TryCreate(raw,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&uri.Host=="www.themoviedb.org")link=uri.AbsoluteUri;
            }
            return Save(key,new WatchAvailability(Title(title.RootElement,kind),offers,link,DateTime.UtcNow),TimeSpan.FromHours(6));
        }catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or FormatException){return null;}
    }
    public sealed record Service(int Id,string Name);
    public static bool HomeViewingService(int id,string? name)=>id!=60 && !new[]{"Fandango","AMC Theatres","AMC Theaters","Regal Cinemas","Cinemark"}.Contains(name?.Trim(),StringComparer.OrdinalIgnoreCase);
    public static int[] ServiceIds(string? value)=> (value??"").Split(',').Select(s=>int.TryParse(s,out var id)?id:0).Where(id=>id>0&&HomeViewingService(id,null)).Distinct().Order().Take(20).ToArray();
    public async Task<IReadOnlyList<Service>> Services()
    {
        const string key="watch:services";
        if(cache.TryGetValue<IReadOnlyList<Service>>(key,out var saved))return saved!;
        var services=new List<Service>();
        foreach(var kind in new[]{"movie","tv"}){
            using var doc=await Fetch($"watch/providers/{kind}?watch_region=US");
            if(doc is null)return [];
            if(doc.RootElement.TryGetProperty("results",out var list))foreach(var item in list.EnumerateArray()){
                if(item.TryGetProperty("provider_id",out var id)&&id.TryGetInt32(out var number)&&Text(item,"provider_name") is {} name&&HomeViewingService(number,name))services.Add(new(number,name));
            }
        }
        return Save<IReadOnlyList<Service>>(key,services.DistinctBy(s=>s.Id).OrderBy(s=>s.Name).ToList(),TimeSpan.FromHours(12));
    }
    private sealed record Access(IReadOnlyList<Service> Services,bool Failed=false);
    private async Task<Access> AccessFor(string kind,int id)
    {
        var key=$"watch:access:{kind}:{id}";
        if(cache.TryGetValue<Access>(key,out var saved))return saved!;
        using var doc=await Fetch($"{kind}/{id}/watch/providers");
        if(doc is null)return Save(key,new Access([],true),TimeSpan.FromSeconds(30));
        var available=new List<Service>();
        if(doc.RootElement.TryGetProperty("results",out var regions)&&regions.TryGetProperty("US",out var us))
            foreach(var category in new[]{"flatrate","free","ads"})
                if(us.TryGetProperty(category,out var list))foreach(var item in list.EnumerateArray())
                    if(item.TryGetProperty("provider_id",out var provider)&&provider.TryGetInt32(out var number)&&Text(item,"provider_name") is {} name&&HomeViewingService(number,name))available.Add(new(number,name));
        return Save(key,new Access(available.DistinctBy(s=>s.Id).ToList()),TimeSpan.FromHours(6));
    }
    public async Task<WatchListing> Prioritize(WatchListing listing,string? selected)
    {
        var ids=ServiceIds(selected).ToHashSet();
        if(ids.Count==0||listing.Failed)return listing;
        var titles=await Task.WhenAll(listing.Items.Select(async title=>{
            var access=await AccessFor(title.Kind,title.Id);
            return title with {MatchingServices=access.Failed?null:access.Services.Where(s=>ids.Contains(s.Id)).Select(s=>s.Name).ToList()};
        }));
        return listing with {Items=titles.OrderByDescending(t=>t.MatchingServices?.Count>0).ToList()};
    }
    public static string SortKey(string? sort)=>sort is "title" or "newest" or "popular" ? sort : "services";
    public static WatchListing Sort(WatchListing listing,string? sort)
    {
        var titles=listing.Items;
        IOrderedEnumerable<WatchTitle> sorted=SortKey(sort) switch {
            "title"=>titles.OrderBy(t=>t.Name,StringComparer.InvariantCultureIgnoreCase),
            "newest"=>titles.OrderByDescending(t=>t.ReleaseDate??DateOnly.MinValue).ThenBy(t=>t.Name,StringComparer.InvariantCultureIgnoreCase),
            "popular"=>titles.OrderByDescending(t=>t.Popularity??double.NegativeInfinity).ThenBy(t=>t.Name,StringComparer.InvariantCultureIgnoreCase),
            _=>titles.OrderByDescending(t=>t.MatchingServices?.Count>0).ThenBy(t=>t.Name,StringComparer.InvariantCultureIgnoreCase)
        };
        return listing with {Items=sorted.ThenBy(t=>t.Id).ToList()};
    }
    private static string? Text(JsonElement e,string key)=>e.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
    private static WatchTitle Title(JsonElement e,string kind){
        var date=Text(e,kind=="movie"?"release_date":"first_air_date");var poster=Text(e,"poster_path");
        var image=poster!=null&&Regex.IsMatch(poster,@"^/[A-Za-z0-9_.-]+$")?"https://image.tmdb.org/t/p/w342"+poster:null;
        return new(e.GetProperty("id").GetInt32(),kind,Text(e,kind=="movie"?"title":"name")??"",date?.Length>=4?date[..4]:null,image,Text(e,"overview")??"",ReleaseDate:DateOnly.TryParseExact(date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var released)?released:null,Popularity:e.TryGetProperty("popularity",out var pop)&&pop.TryGetDouble(out var value)?value:null);
    }
}
