using System.Security.Cryptography;
using System.Text;

namespace myNewspaper.Web.Data;

/// <summary>A shuffled cycle shared by every reader, advancing at each UTC hour boundary.</summary>
public static class SportsFrontRotation
{
    public static IReadOnlyList<T> Order<T>(IEnumerable<T> sources, Func<T, int> id, DateTime utcNow, string panel)
    {
        var shuffled = sources.OrderBy(source => Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{panel}:{id(source)}"))), StringComparer.Ordinal).ToList();
        if (shuffled.Count == 0) return [];
        var offset = (int)(utcNow.Ticks / TimeSpan.TicksPerHour % shuffled.Count);
        return shuffled.Skip(offset).Concat(shuffled.Take(offset)).ToList();
    }
}
