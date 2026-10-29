namespace myNewspaper.Web.Components.Settings;

/// <summary>The fixed messages the /me forms come back with (MeEndpoints). Codes only, so no name is ever in the address.</summary>
public static class SettingsMessages
{
    public static string? Section(string? code) => code switch
    {
        null => null,
        var c when c.StartsWith("show", StringComparison.Ordinal) => "shows",
        var c when c.StartsWith("channel", StringComparison.Ordinal) => "channels",
        var c when c.StartsWith("request", StringComparison.Ordinal) || c == "too-many-requests" => "requests",
        _ => "profile",
    };

    public static string? Done(string? code) => code switch
    {
        "profile-saved" => "Name and color saved.",
        "pin-changed" => "PIN changed. Use the new one next time you open your edition.",
        "device-saved" => "Saved for this device.",
        "show-followed" => "Following. Episodes appear on your TV calendar after the next update.",
        "show-unfollowed" => "Unfollowed. It’s off your TV calendar.",
        "channel-on" => "Channel on. Its videos are in your YouTube page.",
        "channel-off" => "Channel off. Its videos are gone from your YouTube page.",
        "request-sent" => "Sent. The admin will look at it; you’ll see the answer under Your requests.",
        _ => null,
    };

    public static string? Error(string? code, int? triesLeft) => code switch
    {
        "name-taken" => "Another profile already has that name.",
        "profile-invalid" => "Use a name of 1 to 40 characters and one of the colors.",
        "pin-invalid" => "A PIN is 4 digits.",
        "pin-mismatch" => "The two new PINs don’t match.",
        "pin-wrong" => $"That isn’t your current PIN.{(triesLeft is { } n ? $" {n} {(n == 1 ? "try" : "tries")} left before your edition locks." : "")}",
        "device-invalid" => "Choose one of the options.",
        "show-invalid" or "show-unavailable" => "TVmaze couldn’t confirm that show. Try again in a moment.",
        "channel-unknown" => "That channel isn’t available.",
        "too-many-requests" => "You have 5 requests waiting. Wait for the admin to answer one before sending another.",
        "request-invalid" => "Give the request a name (and keep the note under 500 characters).",
        _ => null,
    };
}
