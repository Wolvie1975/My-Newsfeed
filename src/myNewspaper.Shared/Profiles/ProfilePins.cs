using Microsoft.AspNetCore.Identity;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

/// <summary>Hashes and checks 4-digit PINs with ASP.NET Core's password hasher (PBKDF2); no Identity database.</summary>
public sealed class ProfilePins(PasswordHasher<Profile> hasher)
{
    public string Hash(Profile profile, string? pin) => ProfileRules.IsValidPin(pin)
        ? hasher.HashPassword(profile, pin!) : throw new ArgumentException("A PIN is 4 digits.");

    /// <summary>False for a missing PIN, a profile without one, or a wrong one.</summary>
    public bool Matches(Profile profile, string? pin) =>
        profile.PinHash is not null && ProfileRules.IsValidPin(pin)
        && hasher.VerifyHashedPassword(profile, profile.PinHash, pin!) != PasswordVerificationResult.Failed;
}
