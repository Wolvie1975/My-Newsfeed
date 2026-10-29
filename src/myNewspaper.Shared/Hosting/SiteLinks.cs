using Microsoft.Extensions.Options;
namespace myNewspaper.Hosting;

public sealed class SiteLinkOptions
{
    public string PublicBaseUrl { get; set; } = "http://localhost:5295";
    public string AdminBaseUrl { get; set; } = "https://admin.mynewspaper";
}

public sealed class SiteLinks(IOptions<SiteLinkOptions> options)
{
    public string Public => options.Value.PublicBaseUrl.TrimEnd('/') + "/";
    public string Admin => options.Value.AdminBaseUrl.TrimEnd('/') + "/admin";
}
