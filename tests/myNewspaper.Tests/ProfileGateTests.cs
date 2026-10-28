using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

public class ProfileGateTests
{
    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
    private static readonly Guid Device = Guid.NewGuid();
    // 11:30 PM Central on October 28.
    private static readonly DateTime Passed = new(2026, 10, 29, 4, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Without_a_profile_the_picker_always_shows()
    {
        foreach (var mode in ProfileGate.PickerModes)
            Assert.True(ProfileGate.NeedsPicker(null, mode, new GatePass(Device, 1, Passed), Device, Passed, Central));
    }

    [Fact]
    public void Every_visit_needs_this_sessions_gate_cookie()
    {
        Assert.True(ProfileGate.NeedsPicker(1, ProfileGate.EveryVisit, null, Device, Passed, Central));
        Assert.False(ProfileGate.NeedsPicker(1, ProfileGate.EveryVisit, new GatePass(Device, 1, Passed), Device, Passed.AddDays(3), Central));
    }

    [Fact]
    public void Once_a_day_returns_at_midnight_central_time()
    {
        var pass = new GatePass(Device, 1, Passed);
        Assert.False(ProfileGate.NeedsPicker(1, ProfileGate.Daily, pass, Device, Passed.AddMinutes(29), Central));
        Assert.True(ProfileGate.NeedsPicker(1, ProfileGate.Daily, pass, Device, Passed.AddMinutes(31), Central));
        Assert.True(ProfileGate.NeedsPicker(1, ProfileGate.Daily, null, Device, Passed, Central));
    }

    [Fact]
    public void Until_sign_out_never_returns_while_the_profile_is_open()
    {
        Assert.False(ProfileGate.NeedsPicker(1, ProfileGate.UntilSignOut, null, Device, Passed.AddDays(300), Central));
    }

    [Fact]
    public void A_gate_cookie_only_counts_for_the_device_and_profile_that_earned_it()
    {
        Assert.True(ProfileGate.NeedsPicker(1, ProfileGate.EveryVisit, new GatePass(Guid.NewGuid(), 1, Passed), Device, Passed, Central));
        Assert.True(ProfileGate.NeedsPicker(1, ProfileGate.EveryVisit, new GatePass(Device, 2, Passed), Device, Passed, Central));
    }

    [Theory]
    [InlineData("/editions", true)]
    [InlineData("/profiles/3/open", true)]
    [InlineData("/account/login", true)]
    [InlineData("/login", true)]
    [InlineData("/admin", true)]
    [InlineData("/admin/api/profiles", true)]
    [InlineData("/", false)]
    [InlineData("/api/saved", false)]
    [InlineData("/api/me", false)]
    [InlineData("/feed/more", false)]
    [InlineData("/editionsx", false)]
    [InlineData("/administrator", false)]
    public void Exemptions_cover_only_the_picker_profiles_sign_in_and_admin(string path, bool exempt) =>
        Assert.Equal(exempt, ProfileGate.IsExempt(path));

    [Theory]
    [InlineData("/healthz", true)]
    [InlineData("/service-worker.js", true)]
    [InlineData("/manifest.webmanifest", true)]
    [InlineData("/offline.html", true)]
    [InlineData("/_framework/blazor.web.js", true)]
    [InlineData("/_blazor/negotiate", true)]
    [InlineData("/healthz/extra", false)]
    [InlineData("/tv", false)]
    public void Plumbing_passes_straight_through(string path, bool through) => Assert.Equal(through, ProfileGate.IsPassThrough(path));

    [Theory]
    [InlineData("GET", "text/html,application/xhtml+xml", true)]
    [InlineData("GET", "application/json", false)]
    [InlineData("GET", null, false)]
    [InlineData("POST", "text/html", false)]
    public void Only_browser_page_loads_count_as_page_requests(string method, string? accept, bool page)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        if (accept is not null) http.Request.Headers.Accept = accept;
        Assert.Equal(page, ProfileGate.IsPageRequest(http.Request));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1", "iPad · Safari")]
    [InlineData("Mozilla/5.0 (Linux; Android 15) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Mobile Safari/537.36", "Android · Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36 Edg/130.0", "Windows · Edge")]
    [InlineData(null, "Device")]
    public void Devices_are_described_briefly(string? userAgent, string description) =>
        Assert.Equal(description, ProfileGate.DescribeDevice(userAgent));

    [Fact]
    public void Cookies_round_trip_and_reject_tampering_and_swaps()
    {
        var cookies = new ProfileCookies(new EphemeralDataProtectionProvider());
        var token = ProfileCookies.NewDevice();
        var write = new DefaultHttpContext();
        cookies.WriteDevice(write, token);
        cookies.WriteGate(write, new GatePass(token.DeviceId, 7, Passed));
        var set = write.Response.Headers.SetCookie.ToString();
        Assert.Contains("httponly", set, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", set, StringComparison.OrdinalIgnoreCase);

        var values = write.Response.Headers.SetCookie.Select(c => c!.Split(';')[0].Split('=', 2)).ToDictionary(p => p[0], p => p[1]);
        var read = new DefaultHttpContext();
        read.Request.Headers.Cookie = string.Join("; ", values.Select(v => $"{v.Key}={v.Value}"));
        var device = cookies.ReadDevice(read)!;
        Assert.Equal(token.DeviceId, device.DeviceId);
        Assert.Equal(token.SecretHash, device.SecretHash);
        Assert.Equal(new GatePass(token.DeviceId, 7, Passed), cookies.ReadGate(read));

        // The gate value isn't a device cookie, a tampered value is nothing, and a forged plain value is nothing.
        var swapped = new DefaultHttpContext();
        swapped.Request.Headers.Cookie = $"{ProfileCookies.DeviceCookie}={values[ProfileCookies.GateCookie]}; "
            + $"{ProfileCookies.GateCookie}={values[ProfileCookies.GateCookie][..^2]}xx";
        Assert.Null(cookies.ReadDevice(swapped));
        Assert.Null(cookies.ReadGate(swapped));
        var forged = new DefaultHttpContext();
        forged.Request.Headers.Cookie = $"{ProfileCookies.DeviceCookie}={token.DeviceId:N}.abc";
        Assert.Null(cookies.ReadDevice(forged));
    }
}
