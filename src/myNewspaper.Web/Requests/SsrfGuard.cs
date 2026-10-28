using System.Net;
using System.Net.Sockets;

namespace myNewspaper.Web.Requests;

/// <summary>
/// Keeps request links from reaching inside the network (profiles handoff 5.2). The address is checked when the socket
/// connects, not in an earlier DNS lookup that could change before the client connects, and redirects aren't followed
/// automatically.
/// </summary>
public static class SsrfGuard
{
    private static readonly (IPAddress Network, int Prefix)[] Blocked =
    [
        (IPAddress.Parse("0.0.0.0"), 8),        // "this network"
        (IPAddress.Parse("10.0.0.0"), 8),       // private
        (IPAddress.Parse("100.64.0.0"), 10),    // carrier-grade NAT, which includes Tailscale
        (IPAddress.Parse("127.0.0.0"), 8),      // loopback
        (IPAddress.Parse("169.254.0.0"), 16),   // link-local, including cloud metadata
        (IPAddress.Parse("172.16.0.0"), 12),    // private (Docker networks too)
        (IPAddress.Parse("192.0.0.0"), 24),     // protocol assignments
        (IPAddress.Parse("192.168.0.0"), 16),   // private
        (IPAddress.Parse("198.18.0.0"), 15),    // benchmarking
        (IPAddress.Parse("224.0.0.0"), 4),      // multicast
        (IPAddress.Parse("240.0.0.0"), 4),      // reserved and broadcast
        (IPAddress.Parse("::"), 128),           // unspecified
        (IPAddress.Parse("::1"), 128),          // loopback
        (IPAddress.Parse("fc00::"), 7),         // unique local, including Tailscale's fd7a:115c:a1e0::/48
        (IPAddress.Parse("fe80::"), 10),        // link-local
        (IPAddress.Parse("fec0::"), 10),        // old site-local
        (IPAddress.Parse("ff00::"), 8),         // multicast
    ];

    private static readonly IPAddress Nat64 = IPAddress.Parse("64:ff9b::");

    public static bool IsAllowedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        // NAT64 addresses carry an IPv4 address in their last 32 bits; judge that one.
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && InNetwork(address, Nat64, 96))
            address = new IPAddress(address.GetAddressBytes()[12..]);
        return !Blocked.Any(b => b.Network.AddressFamily == address.AddressFamily && InNetwork(address, b.Network, b.Prefix));
    }

    /// <summary>Only absolute http and https links without user info.</summary>
    public static bool IsAllowedUri(Uri? uri) =>
        uri is { IsAbsoluteUri: true } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.IsNullOrEmpty(uri.UserInfo) && uri.HostNameType != UriHostNameType.Unknown;

    private static bool InNetwork(IPAddress address, IPAddress network, int prefix)
    {
        var a = address.GetAddressBytes();
        var n = network.GetAddressBytes();
        if (a.Length != n.Length) return false;
        for (var i = 0; i < a.Length && prefix > 0; i++, prefix -= 8)
        {
            var mask = prefix >= 8 ? 0xFF : 0xFF << (8 - prefix) & 0xFF;
            if ((a[i] & mask) != (n[i] & mask)) return false;
        }
        return true;
    }

    /// <summary>The handler for the "request-checker" client: no proxy, no automatic redirects, guarded connects.</summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        ConnectCallback = ConnectAsync,
    };

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host.Trim('[', ']'), out var literal) ? [literal] : await Dns.GetHostAddressesAsync(host, ct);
        var allowed = addresses.Where(IsAllowedAddress).ToArray();
        if (allowed.Length == 0 || allowed.Length != addresses.Length)
            throw new HttpRequestException($"{host} isn't a public address.");
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            // Connect to the checked addresses themselves, so a second DNS answer can't swap in another one.
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
