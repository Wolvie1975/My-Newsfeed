using System.Text;
using System.Text.RegularExpressions;

namespace myNewspaper.Web.Profiles;

/// <summary>What a profile's name, color and PIN may be, and when wrong PINs lock it (profiles handoff, decision 2).</summary>
public static partial class ProfileRules
{
    public const int MaxWrongPins = 5;   // fixed, not a household setting
    public const int MaxNameLength = 40;

    // \z, not $: $ also matches before a trailing newline.
    [GeneratedRegex(@"^[0-9]{4}\z")]
    private static partial Regex PinPattern();

    // The color ends up in style attributes, so nothing else may pass.
    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}\z")]
    private static partial Regex ColorPattern();

    /// <summary>Exactly 4 ASCII digits. Other Unicode digits are rejected.</summary>
    public static bool IsValidPin(string? pin) => pin is not null && PinPattern().IsMatch(pin);

    public static string NormalizeName(string? name)
    {
        var value = (name ?? "").Normalize(NormalizationForm.FormC).Trim();
        if (value.Length == 0) throw new ArgumentException("A profile name is required.");
        if (value.Length > MaxNameLength) throw new ArgumentException($"A profile name can be at most {MaxNameLength} characters.");
        if (value.Any(char.IsControl)) throw new ArgumentException("A profile name can't contain control characters.");
        return value;
    }

    public static string NormalizeColor(string? color) => color is not null && ColorPattern().IsMatch(color)
        ? color.ToUpperInvariant() : throw new ArgumentException("A color must look like #0051BA.");

    /// <summary>The count after one more wrong PIN. The 5th locks the profile and starts the count again.</summary>
    public static (int FailedPinCount, bool Locked) AfterWrongPin(int failedPinCount) =>
        failedPinCount + 1 >= MaxWrongPins ? (0, true) : (failedPinCount + 1, false);

    public static int TriesLeft(int failedPinCount) => Math.Max(0, MaxWrongPins - failedPinCount);
}
