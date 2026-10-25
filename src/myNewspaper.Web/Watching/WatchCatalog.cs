using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
namespace myNewspaper.Web.Watching;

public sealed record WatchTitle(int Id,string Kind,string Name,string? Year,string? Poster,string Overview,IReadOnlyList<string>? MatchingServices=null,DateOnly? ReleaseDate=null,double? Popularity=null,IReadOnlyList<WatchOffer>? Offers=null,string? WatchLink=null,bool OffersUnavailable=false,IReadOnlyList<WatchProvider>? MatchingProviders=null);
public sealed record WatchListing(IReadOnlyList<WatchTitle> Items,int Pages,bool Failed=false,bool CanContinue=false);
public sealed record WatchProvider(int Id,string Name,string? Logo);
public sealed record WatchOffer(string Category,IReadOnlyList<string> Providers,IReadOnlyList<WatchProvider>? Options=null);
public sealed record WatchAvailability(WatchTitle Title,IReadOnlyList<WatchOffer> Offers,string? Link,DateTime Checked);
public sealed class WatchCatalog(IHttpClientFactory factory,IConfiguration configuration)
{
    private readonly MemoryCache cache=new(new MemoryCacheOptions {SizeLimit=512});
    private T Save<T>(string key,T value,TimeSpan ttl)=>cache.Set(key,value,new MemoryCacheEntryOptions {Size=1,AbsoluteExpirationRelativeToNow=ttl});
    private readonly SemaphoreSlim gate=new(4);
    public bool Configured=>!string.IsNullOrWhiteSpace(configuration["Movies:AccessToken"]);
    public static bool Kind(string kind)=>kind is "movie" or "tv";
    public static string Term(string? query)=>Regex.Replace(query?.Trim() ?? "",@"\s+"," ") is {} s ? s[..Math.Min(s.Length,120)] : "";
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
    public async Task<WatchListing> List(string kind,string? query,int page=1,string? providers=null)
    {
        var batch=await ListBatch(kind,query,ProviderFilter(query,providers),Math.Clamp(page,1,10));
        return batch with {Pages=Math.Min(10,batch.Pages)};
    }
    // Browsing only: TMDB filters discover results to subscription/free titles on any chosen US service.
    private static string ProviderFilter(string? query,string? providers)=>Term(query).Length>0?"":string.Join("|",ServiceIds(providers));
    private async Task<WatchListing> ListBatch(string kind,string? query,string providers,int page)
    {
        if(!Kind(kind)&&!(kind=="multi"&&Term(query).Length>0))return new([],0,true);
        var term=Term(query);page=Math.Clamp(page,1,50);
        var key=$"watch:list:{kind}:{term}:{providers}:{page}";
        if(cache.TryGetValue<WatchListing>(key,out var saved))return saved!;
        var path=term.Length>0 ? $"search/{kind}?query={Uri.EscapeDataString(term)}&include_adult=false&page={page}" : $"discover/{kind}?include_adult=false&sort_by=popularity.desc&page={page}&watch_region=US"+(providers.Length>0?$"&with_watch_providers={Uri.EscapeDataString(providers)}&with_watch_monetization_types=flatrate%7Cfree%7Cads":"");
        // TMDB: returning, planned, in production or pilot; exclude ended/canceled only in TV browsing.
        if(kind=="tv" && term.Length==0)path+="&with_status=0%7C1%7C2%7C5";
        return await Batch(key,path,kind);
    }
    private async Task<WatchListing> Batch(string key,string path,string kind)
    {
        if(cache.TryGetValue<WatchListing>(key,out var saved))return saved!;
        using var doc=await Fetch(path);
        if(doc==null)return Save(key,new WatchListing([],0,true),TimeSpan.FromSeconds(30));
        try {
            var root=doc.RootElement;
            var items=root.GetProperty("results").EnumerateArray().Where(x=>!x.TryGetProperty("adult",out var a)||a.ValueKind!=JsonValueKind.True).Where(x=>kind!="multi"||Kind(Text(x,"media_type")??"")).Select(x=>Title(x,kind=="multi"?Text(x,"media_type")!:kind)).Where(x=>x.Id>0&&!string.IsNullOrWhiteSpace(x.Name)).ToList();
            return Save(key,new WatchListing(items,Math.Min(50,root.GetProperty("total_pages").GetInt32())),TimeSpan.FromMinutes(15));
        }catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or FormatException){return new([],0,true);}
    }

    // Featured rows: up to ShelfSize titles with US viewing options, scanning at most three upstream pages per kind.
    public const int ShelfSize=12;
    public Task<WatchListing> TrendingShelf(string kind,string? selected)=>Kind(kind)
        ? Shelf($"trending:{kind}",[kind],(k,page)=>Batch($"watch:trending:{k}:{page}",$"trending/{k}/week?page={page}",k),selected,false)
        : Task.FromResult(new WatchListing([],0,true));
    public Task<WatchListing> NewMoviesShelf(DateOnly today,string? selected)=>
        Shelf($"new:{today:yyyyMMdd}",["movie"],(k,page)=>Batch($"watch:new:{today:yyyyMMdd}:{page}",$"discover/movie?include_adult=false&sort_by=popularity.desc&page={page}&watch_region=US&with_watch_monetization_types=flatrate%7Cfree%7Cads%7Crent%7Cbuy&primary_release_date.gte={today.AddDays(-60):yyyy-MM-dd}&primary_release_date.lte={today:yyyy-MM-dd}",k),selected,false);
    public Task<WatchListing> ServicesShelf(string? selected)
    {
        var providers=ProviderFilter(null,selected);
        return providers.Length==0 ? Task.FromResult(new WatchListing([],0))
            : Shelf($"services:{providers}",["movie","tv"],(k,page)=>ListBatch(k,null,providers,page),selected,true);
    }
    private async Task<WatchListing> Shelf(string key,string[] kinds,Func<string,int,Task<WatchListing>> fetch,string? selected,bool requireMatch)
    {
        key="watch:shelf:"+key;
        if(!cache.TryGetValue<WatchListing>(key,out var shelf)){
            var failed=false;
            var lists=await Task.WhenAll(kinds.Select(async kind=>{
                var titles=new List<WatchTitle>();var seen=new HashSet<int>();
                for(var page=1;page<=3&&titles.Count<ShelfSize;page++){
                    var batch=await fetch(kind,page);
                    if(batch.Failed){failed=true;break;}
                    var eligible=await Prioritize(batch with {Items=batch.Items.Where(t=>seen.Add(t.Id)).ToList()},requireMatch?selected:null,reorder:false);
                    titles.AddRange(requireMatch?eligible.Items.Where(t=>t.MatchingServices is null||t.MatchingServices.Count>0):eligible.Items);
                    if(page>=batch.Pages)break;
                }
                return titles;
            }));
            // Mixed rows alternate kinds so neither movies nor TV crowds out the other.
            var mixed=Enumerable.Range(0,ShelfSize).SelectMany(i=>lists.Where(l=>i<l.Count).Select(l=>l[i])).Take(ShelfSize).ToList();
            shelf=new WatchListing(mixed,1,Failed:failed&&mixed.Count==0);
            Save(key,shelf,shelf.Failed?TimeSpan.FromSeconds(30):TimeSpan.FromMinutes(15));
        }
        return await Prioritize(shelf!,selected,false,reorder:false);
    }
    private sealed class ResultWindow
    {
        public readonly SemaphoreSlim Gate=new(1);
        public readonly List<WatchTitle> Titles=[];
        public readonly HashSet<(string Kind,int Id)> Seen=[];
        public int NextBatch=1;
        public bool Exhausted;
    }
    private readonly MemoryCache windows=new(new MemoryCacheOptions {SizeLimit=32});
    private readonly object windowLock=new();
    public async Task<WatchListing> FilledPage(string kind,string? query,int page=1,string? selected=null,bool onlySelected=false)
    {
        if(!Kind(kind)&&!(kind=="multi"&&Term(query).Length>0))return new([],0,true);
        page=Math.Clamp(page,1,10);
        var providers=onlySelected?ProviderFilter(query,selected):"";
        var key=$"{kind}:{Term(query)}:{providers}";
        ResultWindow window;
        lock(windowLock){
            if(!windows.TryGetValue<ResultWindow>(key,out var saved)){
                saved=new();windows.Set(key,saved,new MemoryCacheEntryOptions {Size=1,AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(15)});
            }
            window=saved!;
        }
        await window.Gate.WaitAsync();
        WatchListing result;
        try {
            var needed=page*20+(page<10?1:0);var scanned=0;var batchFailed=false;
            while(window.Titles.Count<needed&&!window.Exhausted&&scanned++<10){
                var batch=await ListBatch(kind,query,providers,window.NextBatch);
                if(batch.Failed){batchFailed=true;break;}
                var unique=batch with {Items=batch.Items.Where(t=>window.Seen.Add((t.Kind,t.Id))).ToList()};
                var eligible=await Prioritize(unique,providers.Length>0?selected:null);
                // Recheck TMDB's provider filter against the cached offers; unknown availability stays visible.
                window.Titles.AddRange(providers.Length>0?eligible.Items.Where(t=>t.MatchingServices is null||t.MatchingServices.Count>0):eligible.Items);
                window.Exhausted=window.NextBatch>=batch.Pages||window.NextBatch>=50;
                window.NextBatch++;
            }
            var items=window.Titles.Skip((page-1)*20).Take(20).ToList();
            var more=window.Titles.Count>page*20;
            var pending=!window.Exhausted&&window.Titles.Count<needed;
            result=new(items,more?Math.Min(10,page+1):Math.Max(1,(window.Titles.Count+19)/20),Failed:batchFailed&&items.Count==0,CanContinue:pending);
        }finally{window.Gate.Release();}
        // Membership and upstream order stay fixed within the cached window; only service badges are personalized.
        return await Prioritize(result,selected,false,reorder:false);
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
                offers.AddRange(ReadOffers(us));
                link=ProviderLink(us);
            }
            return Save(key,new WatchAvailability(Title(title.RootElement,kind),offers,link,DateTime.UtcNow),TimeSpan.FromHours(6));
        }catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or FormatException){return null;}
    }
    private static IReadOnlyList<WatchOffer> ReadOffers(JsonElement us)
    {
        var offers=new List<WatchOffer>();
        foreach(var (key,label) in new[]{("flatrate","Subscription"),("free","Free"),("ads","Free with ads"),("rent","Rent"),("buy","Buy")}){
            if(!us.TryGetProperty(key,out var list)||list.ValueKind!=JsonValueKind.Array)continue;
            var options=list.EnumerateArray().Select(item=>{
                var id=item.TryGetProperty("provider_id",out var value)&&value.TryGetInt32(out var number)?number:0;
                var name=Text(item,"provider_name");
                return !string.IsNullOrWhiteSpace(name)&&HomeViewingService(id,name)
                    ? new WatchProvider(id,name,ProviderLogo(Text(item,"logo_path"))) : null;
            }).OfType<WatchProvider>().DistinctBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).ToList();
            if(options.Count>0)offers.Add(new(label,options.Select(p=>p.Name).ToList(),options));
        }
        return offers;
    }
    private static string? ProviderLogo(string? path)=>path!=null&&Regex.IsMatch(path,@"^/[A-Za-z0-9_.-]+$")
        ? "https://image.tmdb.org/t/p/w92"+path : null;
    private static string? ProviderLink(JsonElement us)=>Uri.TryCreate(Text(us,"link"),UriKind.Absolute,out var uri)
        &&uri.Scheme=="https"&&uri.Host=="www.themoviedb.org"?uri.AbsoluteUri:null;
    public sealed record Service(int Id,string Name,string? Logo=null);
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
                if(item.TryGetProperty("provider_id",out var id)&&id.TryGetInt32(out var number)&&Text(item,"provider_name") is {} name&&HomeViewingService(number,name))services.Add(new(number,name,ProviderLogo(Text(item,"logo_path"))));
            }
        }
        return Save<IReadOnlyList<Service>>(key,services.DistinctBy(s=>s.Id).OrderBy(s=>s.Name).ToList(),TimeSpan.FromHours(12));
    }
    private sealed record Access(IReadOnlyList<Service> Services,bool Failed=false,bool HasUsOptions=false,IReadOnlyList<WatchOffer>? Offers=null,string? Link=null);
    private async Task<Access> AccessFor(string kind,int id)
    {
        var key=$"watch:access:{kind}:{id}";
        if(cache.TryGetValue<Access>(key,out var saved))return saved!;
        using var doc=await Fetch($"{kind}/{id}/watch/providers");
        if(doc is null)return Save(key,new Access([],true),TimeSpan.FromSeconds(30));
        try {
            var available=new List<Service>();var hasOptions=false;IReadOnlyList<WatchOffer> offers=[];string? link=null;
            if(!doc.RootElement.TryGetProperty("results",out var regions)||regions.ValueKind!=JsonValueKind.Object)
                return Save(key,new Access([],true),TimeSpan.FromSeconds(30));
            if(regions.TryGetProperty("US",out var us)){
                if(us.ValueKind!=JsonValueKind.Object)return Save(key,new Access([],true),TimeSpan.FromSeconds(30));
                offers=ReadOffers(us);link=ProviderLink(us);
                foreach(var category in new[]{"flatrate","free","ads","rent","buy"}){
                    if(!us.TryGetProperty(category,out var list))continue;
                    if(list.ValueKind!=JsonValueKind.Array)return Save(key,new Access([],true),TimeSpan.FromSeconds(30));
                    foreach(var item in list.EnumerateArray()){
                        var number=item.TryGetProperty("provider_id",out var provider)&&provider.TryGetInt32(out var idValue)?idValue:0;
                        if(Text(item,"provider_name") is not {} name||string.IsNullOrWhiteSpace(name)||!HomeViewingService(number,name))continue;
                        hasOptions=true;
                        if(category is "flatrate" or "free" or "ads")available.Add(new(number,name));
                    }
                }
            }
            return Save(key,new Access(available.DistinctBy(s=>s.Id).ToList(),HasUsOptions:hasOptions,Offers:offers,Link:link),TimeSpan.FromHours(6));
        }catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or FormatException){return Save(key,new Access([],true),TimeSpan.FromSeconds(30));}
    }
    public async Task<WatchListing> Prioritize(WatchListing listing,string? selected,bool filterUnavailable=true,bool reorder=true)
    {
        var ids=ServiceIds(selected).ToHashSet();
        if(listing.Failed)return listing;
        var titles=await Task.WhenAll(listing.Items.Select(async title=>{
            var access=await AccessFor(title.Kind,title.Id);
            if(filterUnavailable&&!access.Failed&&!access.HasUsOptions)return null;
            var included=new[]{"Subscription","Free","Free with ads"};
            var logos=access.Failed?null:(access.Offers??[]).Where(o=>included.Contains(o.Category)).SelectMany(o=>o.Options??[]).Where(p=>ids.Contains(p.Id)).DistinctBy(p=>p.Id).ToList();
            return title with {MatchingServices=access.Failed?null:access.Services.Where(s=>ids.Contains(s.Id)).Select(s=>s.Name).ToList(),Offers=access.Offers,WatchLink=access.Link,OffersUnavailable=access.Failed,MatchingProviders=logos};
        }));
        var kept=titles.OfType<WatchTitle>();
        return listing with {Items=(reorder?kept.OrderByDescending(t=>t.MatchingServices?.Count>0):kept).ToList()};
    }

    // Searches default to TMDB's relevance order ("Best match"); browsing defaults to the viewer's services first.
    public static string SortKey(string? sort,bool searching=false)=>sort is "title" or "newest" or "popular" ? sort
        : searching&&(string.IsNullOrEmpty(sort)||sort=="relevance") ? "relevance" : "services";
    public static WatchListing Sort(WatchListing listing,string? sort)
    {
        if(sort=="relevance")return listing;
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
