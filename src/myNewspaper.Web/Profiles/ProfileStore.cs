using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

public enum OpenOutcome { Opened, WrongPin, Locked, NeedsPin, NotFound, NoDevice }

public sealed record OpenResult(OpenOutcome Outcome, int TriesLeft = 0)
{
    public static readonly OpenResult Opened = new(OpenOutcome.Opened);
    public static readonly OpenResult Locked = new(OpenOutcome.Locked);
    public static readonly OpenResult NeedsPin = new(OpenOutcome.NeedsPin);
    public static readonly OpenResult NotFound = new(OpenOutcome.NotFound);
    public static readonly OpenResult NoDevice = new(OpenOutcome.NoDevice);
}

/// <summary>A profile name that's already used. The message is safe to show.</summary>
public sealed class ProfileNameTakenException() : ArgumentException("That name is already used. Choose another.");

/// <summary>The reader side of profiles: devices, opening with a PIN, creating, settings (profiles handoff 2.1 to 2.3).</summary>
public static class ProfileStore
{
    // Locks the profile row for the rest of the transaction, so parallel wrong PINs can't each read the same count.
    // A context that already tracks the row would hand back its cached copy, so that copy is reloaded under the lock.
    internal static async Task<Profile?> LockAsync(WebScraperContext db, int id, CancellationToken ct)
    {
        var profile = await db.Profiles.FromSql($"SELECT * FROM dbo.MyDailyFeedProfiles WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(ct);
        if (profile is not null && db.Entry(profile).State == EntityState.Unchanged) await db.Entry(profile).ReloadAsync(ct);
        return profile;
    }

    /// <summary>Joins the caller's transaction (tests wrap one) or starts its own.</summary>
    internal static async Task<IDbContextTransaction?> BeginAsync(WebScraperContext db, CancellationToken ct) =>
        db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;

    public static async Task<Household> GetHouseholdAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.Household.AsNoTracking().SingleOrDefaultAsync(h => h.Id == 1, ct) ?? new Household();

    /// <summary>
    /// The device behind a cookie, and its profile when it has a usable one. A wrong secret, a revoked device, or one
    /// unused for longer than the household's limit is no device; a hidden profile is no profile.
    /// </summary>
    public static async Task<(ProfileDevice? Device, Profile? Profile)> LoadDeviceAsync(WebScraperContext db, DeviceToken token,
        int? forgetDevicesDays, DateTime nowUtc, CancellationToken ct = default)
    {
        var device = await db.ProfileDevices.AsNoTracking().Include(d => d.Profile).SingleOrDefaultAsync(d => d.Id == token.DeviceId, ct);
        if (device is null || device.RevokedAt is not null || !CryptographicOperations.FixedTimeEquals(device.TokenHash, token.SecretHash))
            return (null, null);
        if (forgetDevicesDays is { } days && device.LastSeenAt < nowUtc.AddDays(-days)) return (null, null);
        var profile = device.Profile is { Hidden: false } p ? p : null;
        device.Profile = null;
        return (device, profile);
    }

    public static async Task<ProfileDevice> CreateDeviceAsync(WebScraperContext db, DeviceToken token, string? userAgent,
        string pickerMode, CancellationToken ct = default)
    {
        var agent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        var device = new ProfileDevice
        {
            Id = token.DeviceId,
            TokenHash = token.SecretHash,
            Label = ProfileGate.DescribeDevice(agent),
            UserAgent = agent?.Length > 300 ? agent[..300] : agent,
            PickerMode = ProfileGate.PickerModes.Contains(pickerMode) ? pickerMode : ProfileGate.EveryVisit,
        };
        db.ProfileDevices.Add(device);
        await db.SaveChangesAsync(ct);
        db.Entry(device).State = EntityState.Detached;
        return device;
    }

    /// <summary>Records use at most once a minute: the device's LastSeenAt, and LastReadAt for the profile it reads as.</summary>
    public static async Task TouchAsync(WebScraperContext db, ProfileDevice device, Profile? profile, DateTime nowUtc, CancellationToken ct = default)
    {
        var stale = nowUtc.AddMinutes(-1);
        if (device.LastSeenAt < stale)
            await db.ProfileDevices.Where(d => d.Id == device.Id).ExecuteUpdateAsync(s => s.SetProperty(d => d.LastSeenAt, nowUtc), ct);
        if (profile is not null && (profile.LastReadAt is null || profile.LastReadAt < stale))
            await db.Profiles.Where(p => p.Id == profile.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.LastReadAt, nowUtc), ct);
    }

    /// <summary>
    /// Opens a profile on a device with its PIN. Five wrong PINs from any device lock it until the admin unlocks it or
    /// resets the PIN; there's no timed unlock (decision 2).
    /// </summary>
    public static async Task<OpenResult> OpenAsync(WebScraperContext db, ProfilePins pins, Guid deviceId, int profileId, string? pin,
        DateTime nowUtc, CancellationToken ct = default)
    {
        await using var tx = await BeginAsync(db, ct);
        var device = await db.ProfileDevices.SingleOrDefaultAsync(d => d.Id == deviceId && d.RevokedAt == null, ct);
        if (device is null) return OpenResult.NoDevice;
        var profile = await LockAsync(db, profileId, ct);
        if (profile is null || profile.Hidden) return OpenResult.NotFound;
        if (profile.LockedAt is not null) return OpenResult.Locked;
        if (profile.PinHash is null) return OpenResult.NeedsPin;

        OpenResult result;
        if (pins.Matches(profile, pin))
        {
            profile.FailedPinCount = 0;
            device.ProfileId = profile.Id;
            result = OpenResult.Opened;
        }
        else
        {
            (profile.FailedPinCount, var locked) = ProfileRules.AfterWrongPin(profile.FailedPinCount);
            if (locked)
            {
                profile.LockedAt = nowUtc;
                profile.LockedByDeviceId = deviceId;
            }
            result = locked ? OpenResult.Locked : new(OpenOutcome.WrongPin, ProfileRules.TriesLeft(profile.FailedPinCount));
        }
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return result;
    }

    /// <summary>Only for a profile without a PIN (the one 021 may create): sets it and opens the profile.</summary>
    public static async Task<OpenResult> SetFirstPinAsync(WebScraperContext db, ProfilePins pins, Guid deviceId, int profileId,
        string? pin, CancellationToken ct = default)
    {
        if (!ProfileRules.IsValidPin(pin)) throw new ArgumentException("A PIN is 4 digits.");
        await using var tx = await BeginAsync(db, ct);
        var device = await db.ProfileDevices.SingleOrDefaultAsync(d => d.Id == deviceId && d.RevokedAt == null, ct);
        if (device is null) return OpenResult.NoDevice;
        var profile = await LockAsync(db, profileId, ct);
        if (profile is null || profile.Hidden || profile.PinHash is not null) return OpenResult.NotFound;
        if (profile.LockedAt is not null) return OpenResult.Locked;
        profile.PinHash = pins.Hash(profile, pin);
        profile.FailedPinCount = 0;
        device.ProfileId = profile.Id;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return OpenResult.Opened;
    }

    /// <summary>
    /// A new profile (setup, or Add a profile in admin). Settings start as a copy of the base edition, with the
    /// person's own choices on top, and every household channel starts turned on (decision 11 and 12).
    /// </summary>
    public static async Task<Profile> CreateAsync(WebScraperContext db, ProfilePins pins, string? name, string? color, string? pin,
        JsonObject? choices, CancellationToken ct = default)
    {
        var profile = new Profile { Name = ProfileRules.NormalizeName(name), Color = ProfileRules.NormalizeColor(color) };
        profile.PinHash = pins.Hash(profile, pin);
        var household = await GetHouseholdAsync(db, ct);
        var settings = ProfileSettings.FillMissing([], ProfileSettings.Parse(household.BaseSettings));
        if (choices is not null) settings = ProfileSettings.Merge(settings, choices);
        profile.Settings = ProfileSettings.Serialize(settings);

        await using var tx = await BeginAsync(db, ct);
        profile.SortOrder = (await db.Profiles.MaxAsync(p => (int?)p.SortOrder, ct) ?? 0) + 1;
        db.Profiles.Add(profile);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex)) { throw new ProfileNameTakenException(); }
        await db.Database.ExecuteSqlAsync($"""
            INSERT dbo.MyDailyFeedProfileChannelFollows (ProfileId, YoutubeVideoFeedId)
            SELECT {profile.Id}, r.YoutubeVideoFeedId FROM dbo.MyDailyFeedChannelRollouts r
            """, ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return profile;
    }

    /// <summary>Opens a profile on a device without a PIN check: only right after that device created it.</summary>
    public static Task<int> AssignDeviceAsync(WebScraperContext db, Guid deviceId, int profileId, CancellationToken ct = default) =>
        db.ProfileDevices.Where(d => d.Id == deviceId && d.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.ProfileId, profileId), ct);

    public static Task<int> SignOutAsync(WebScraperContext db, Guid deviceId, CancellationToken ct = default) =>
        db.ProfileDevices.Where(d => d.Id == deviceId).ExecuteUpdateAsync(s => s.SetProperty(d => d.ProfileId, (int?)null), ct);

    public static Task<int> SetPickerModeAsync(WebScraperContext db, Guid deviceId, string? mode, CancellationToken ct = default) =>
        mode is not null && ProfileGate.PickerModes.Contains(mode)
            ? db.ProfileDevices.Where(d => d.Id == deviceId).ExecuteUpdateAsync(s => s.SetProperty(d => d.PickerMode, mode), ct)
            : throw new ArgumentException("Choose every-visit, daily or until-signout.");

    /// <summary>Settings → Change PIN. A wrong current PIN counts toward the lock, like the picker.</summary>
    public static async Task<OpenResult> ChangePinAsync(WebScraperContext db, ProfilePins pins, Guid deviceId, int profileId,
        string? currentPin, string? newPin, DateTime nowUtc, CancellationToken ct = default)
    {
        if (!ProfileRules.IsValidPin(newPin)) throw new ArgumentException("A PIN is 4 digits.");
        await using var tx = await BeginAsync(db, ct);
        var profile = await LockAsync(db, profileId, ct);
        if (profile is null) return OpenResult.NotFound;
        if (profile.LockedAt is not null) return OpenResult.Locked;
        OpenResult result;
        if (pins.Matches(profile, currentPin))
        {
            profile.PinHash = pins.Hash(profile, newPin);
            profile.FailedPinCount = 0;
            result = OpenResult.Opened;
        }
        else
        {
            (profile.FailedPinCount, var locked) = ProfileRules.AfterWrongPin(profile.FailedPinCount);
            if (locked) { profile.LockedAt = nowUtc; profile.LockedByDeviceId = deviceId; }
            result = locked ? OpenResult.Locked : new(OpenOutcome.WrongPin, ProfileRules.TriesLeft(profile.FailedPinCount));
        }
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return result;
    }

    public static async Task<JsonObject> GetSettingsAsync(WebScraperContext db, int profileId, CancellationToken ct = default) =>
        ProfileSettings.Parse(await db.Profiles.Where(p => p.Id == profileId).Select(p => p.Settings).SingleOrDefaultAsync(ct));

    /// <summary>PUT /api/me/settings: a merge-patch of top-level keys, each validated.</summary>
    public static async Task<JsonObject> PatchSettingsAsync(WebScraperContext db, int profileId, JsonObject patch, CancellationToken ct = default)
    {
        await using var tx = await BeginAsync(db, ct);
        var profile = await LockAsync(db, profileId, ct) ?? throw new InvalidOperationException("No profile selected.");
        var settings = ProfileSettings.Merge(ProfileSettings.Parse(profile.Settings), patch);
        profile.Settings = ProfileSettings.Serialize(settings);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return settings;
    }

    /// <summary>
    /// The one-time import of this browser's old localStorage values (2.5): fills only keys the profile doesn't have,
    /// and only once per profile. Later calls change nothing.
    /// </summary>
    public static async Task<JsonObject> ImportBrowserAsync(WebScraperContext db, int profileId, JsonObject values, DateTime nowUtc,
        CancellationToken ct = default)
    {
        await using var tx = await BeginAsync(db, ct);
        var profile = await LockAsync(db, profileId, ct) ?? throw new InvalidOperationException("No profile selected.");
        var settings = ProfileSettings.Parse(profile.Settings);
        if (profile.BrowserImportAt is null)
        {
            settings = ProfileSettings.FillMissing(settings, values);
            profile.Settings = ProfileSettings.Serialize(settings);
            profile.BrowserImportAt = nowUtc;
            await db.SaveChangesAsync(ct);
        }
        if (tx is not null) await tx.CommitAsync(ct);
        return settings;
    }
}
