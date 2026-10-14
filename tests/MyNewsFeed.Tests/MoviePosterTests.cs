using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewsFeed.Web.Components;
namespace MyNewsFeed.Tests;

public class MoviePosterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://example.com/poster.jpg", true)]
    public async Task Posters_encode_title_use_safe_images_and_retain_a_missing_image_fallback(string? url, bool hasImage)
    {
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<MoviePoster>(ParameterView.FromDictionary(
            new Dictionary<string,object?> { ["Url"]=url,["Title"]="Film <one>",["Class"]="boxoffice-poster" }))).ToHtmlString());
        Assert.Contains("boxoffice-poster",html);Assert.Contains("Poster unavailable for Film &lt;one&gt;",html);
        Assert.Equal(hasImage,html.Contains("<img"));Assert.DoesNotContain("javascript:",html);
        if(hasImage){Assert.Contains("alt=\"Film &lt;one&gt;\"",html);Assert.Contains("data-poster-fallback",html);Assert.Contains("this.remove()",html);}
        else Assert.DoesNotContain("hidden",html);
    }
}
