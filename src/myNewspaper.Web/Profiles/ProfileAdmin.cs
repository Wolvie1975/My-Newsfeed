using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// What the admin sees of a profile (decision 3): who it is, whether it's locked or hidden, its devices and its
/// requests. Never its follows, settings, Saved list or reading times. There's no way to open a profile from here.
/// </summary>
public sealed record AdminProfileRow(int Id, string Name, string Color, bool Hidden, int SortOrder, bool HasPin,
    DateTime? LockedAt, string? LockedByDevice, int Devices, int WaitingRequests);

public sealed record AdminProfileTotals(int Profiles, int Devices, int Locked, int WaitingRequests);

/// <summary>A remembered device. No last-used time: the admin needs devices to unlock and sign out, not to see reading.</summary>
public sealed record AdminDeviceRow(Guid Id, string Label, string Description, bool CausedLock);

public sealed record HouseholdSettings(string DefaultPickerMode, int? ForgetDevicesDays);

public static class ProfileAdmin
{
    public const int MaxForgetDevicesDays = 3650;

    public static async Task<IReadOnlyList<AdminProfileRow>> ListAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var rows = await db.Profiles.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Color,
                p.Hidden,
                p.SortOrder,
                HasPin = p.PinHash != null,
                p.LockedAt,
                LockedBy = db.ProfileDevices.Where(d => d.Id == p.LockedByDeviceId).Select(d => d.Label).FirstOrDefault(),
                Devices = db.ProfileDevices.Count(d => d.ProfileId == p.Id && d.RevokedAt == null),
                Waiting = db.ContentRequests.Count(r => r.ProfileId == p.Id && r.Status == "waiting"),
            }).ToListAsync(ct);
        return rows.Select(r => new AdminProfileRow(r.Id, r.Name, r.Color, r.Hidden, r.SortOrder, r.HasPin, r.LockedAt,
            r.LockedAt is null ? null : r.LockedBy, r.Devices, r.Waiting)).ToList();
    }

    public static async Task<AdminProfileTotals> TotalsAsync(WebScraperContext db, CancellationToken ct = default) => new(
        await db.Profiles.CountAsync(ct),
        await db.ProfileDevices.CountAsync(d => d.RevokedAt == null && d.ProfileId != null, ct),
        await db.Profiles.CountAsync(p => p.LockedAt != null, ct),
        await db.ContentRequests.CountAsync(r => r.Status == "waiting", ct));

    /// <summary>Name, color and the picker switch. Hiding a profile signs out every device it's open on.</summary>
    public static async Task<bool> UpdateAsync(WebScraperContext db, int id, string? name, string? color, bool hidden,
        DateTime nowUtc, CancellationToken ct = default)
    {
        var normalizedName = ProfileRules.NormalizeName(name);
        var normalizedColor = ProfileRules.NormalizeColor(color);
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var profile = await ProfileStore.LockAsync(db, id, ct);
        if (profile is null) return false;
        profile.Name = normalizedName;
        profile.Color = normalizedColor;
        profile.Hidden = hidden;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex)) { throw new ProfileNameTakenException(); }
        if (hidden) await SignOutEverywhereAsync(db, id, nowUtc, ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return true;
    }

    /// <summary>Clears the lock; the PIN stays the same.</summary>
    public static async Task<bool> UnlockAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.Profiles.Where(p => p.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(p => p.LockedAt, (DateTime?)null)
            .SetProperty(p => p.LockedByDeviceId, (Guid?)null)
            .SetProperty(p => p.FailedPinCount, 0), ct) > 0;

    /// <summary>
    /// The admin sets a new PIN (decision 2): it replaces the old one, clears any lock, and signs the profile out of
    /// every device, so only someone told the new PIN can open it again.
    /// </summary>
    public static async Task<bool> ResetPinAsync(WebScraperContext db, ProfilePins pins, int id, string? pin, DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (!ProfileRules.IsValidPin(pin)) throw new ArgumentException("A PIN is 4 digits.");
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var profile = await ProfileStore.LockAsync(db, id, ct);
        if (profile is null) return false;
        profile.PinHash = pins.Hash(profile, pin);
        profile.LockedAt = null;
        profile.LockedByDeviceId = null;
        profile.FailedPinCount = 0;
        await db.SaveChangesAsync(ct);
        await SignOutEverywhereAsync(db, id, nowUtc, ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return true;
    }

    /// <summary>Cascading foreign keys remove the profile's Saved list, follows and requests; its devices keep no profile.</summary>
    public static async Task<bool> DeleteAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.Profiles.Where(p => p.Id == id).ExecuteDeleteAsync(ct) > 0;

    /// <summary>The picker order: the listed profiles get 1, 2, 3…; any not listed keep theirs after them.</summary>
    public static async Task ReorderAsync(WebScraperContext db, IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count != ids.Distinct().Count()) throw new ArgumentException("Each profile can appear once.");
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var profiles = await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
            if (profiles.SingleOrDefault(p => p.Id == ids[i]) is { } profile) profile.SortOrder = i + 1;
        var rest = await db.Profiles.Where(p => !ids.Contains(p.Id)).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(ct);
        for (var i = 0; i < rest.Count; i++) rest[i].SortOrder = ids.Count + i + 1;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
    }

    public static async Task<IReadOnlyList<AdminDeviceRow>?> DevicesAsync(WebScraperContext db, int profileId, CancellationToken ct = default)
    {
        var lockedBy = await db.Profiles.Where(p => p.Id == profileId).Select(p => new { p.LockedByDeviceId }).SingleOrDefaultAsync(ct);
        if (lockedBy is null) return null;
        var devices = await db.ProfileDevices.AsNoTracking()
            .Where(d => (d.ProfileId == profileId && d.RevokedAt == null) || d.Id == lockedBy.LockedByDeviceId)
            .OrderBy(d => d.Label).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Label, d.UserAgent }).ToListAsync(ct);
        return devices.Select(d => new AdminDeviceRow(d.Id, d.Label, ProfileGate.DescribeDevice(d.UserAgent), d.Id == lockedBy.LockedByDeviceId))
            .ToList();
    }

    public static async Task<bool> RenameDeviceAsync(WebScraperContext db, int profileId, Guid deviceId, string? label, CancellationToken ct = default)
    {
        var value = (label ?? "").Trim();
        if (value.Length is 0 or > 100 || value.Any(char.IsControl)) throw new ArgumentException("A device name is 1 to 100 characters.");
        return await db.ProfileDevices.Where(d => d.Id == deviceId && (d.ProfileId == profileId
                || db.Profiles.Any(p => p.Id == profileId && p.LockedByDeviceId == d.Id)))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Label, value), ct) > 0;
    }

    /// <summary>Revokes the device: its cookie stops working, and the next visit starts as a new device at the picker.</summary>
    public static async Task<bool> SignOutDeviceAsync(WebScraperContext db, int profileId, Guid deviceId, DateTime nowUtc, CancellationToken ct = default) =>
        await db.ProfileDevices.Where(d => d.Id == deviceId && d.ProfileId == profileId && d.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.RevokedAt, nowUtc), ct) > 0;

    public static Task<int> SignOutEverywhereAsync(WebScraperContext db, int profileId, DateTime nowUtc, CancellationToken ct = default) =>
        db.ProfileDevices.Where(d => d.ProfileId == profileId && d.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.RevokedAt, nowUtc), ct);

    public static async Task<HouseholdSettings> GetHouseholdAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var household = await ProfileStore.GetHouseholdAsync(db, ct);
        return new(household.DefaultPickerMode, household.ForgetDevicesDays);
    }

    /// <summary>The picker default applies to devices seen from now on; existing devices keep their own setting.</summary>
    public static async Task SaveHouseholdAsync(WebScraperContext db, HouseholdSettings settings, CancellationToken ct = default)
    {
        if (!ProfileGate.PickerModes.Contains(settings.DefaultPickerMode)) throw new ArgumentException("Choose every-visit, daily or until-signout.");
        if (settings.ForgetDevicesDays is < 1 or > MaxForgetDevicesDays) throw new ArgumentException($"Forget devices after 1 to {MaxForgetDevicesDays} days, or never.");
        await EnsureHouseholdAsync(db, ct);
        await db.Household.Where(h => h.Id == 1).ExecuteUpdateAsync(s => s
            .SetProperty(h => h.DefaultPickerMode, settings.DefaultPickerMode)
            .SetProperty(h => h.ForgetDevicesDays, settings.ForgetDevicesDays), ct);
    }

    /// <summary>The base edition. It's never read for an existing profile, only copied into new ones (decision 12).</summary>
    public static async Task<JsonObject> GetBaseEditionAsync(WebScraperContext db, CancellationToken ct = default) =>
        ProfileSettings.Parse((await ProfileStore.GetHouseholdAsync(db, ct)).BaseSettings);

    /// <summary>Replaces the base edition, validated key by key like PUT /api/me/settings. An empty document resets it.</summary>
    public static async Task<JsonObject> SaveBaseEditionAsync(WebScraperContext db, JsonObject settings, CancellationToken ct = default)
    {
        var normalized = ProfileSettings.Normalize(settings);
        var json = ProfileSettings.Serialize(normalized);
        await EnsureHouseholdAsync(db, ct);
        await db.Household.Where(h => h.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(h => h.BaseSettings, json), ct);
        return normalized;
    }

    // 020 inserts the row, but don't depend on nobody having removed it.
    private static Task EnsureHouseholdAsync(WebScraperContext db, CancellationToken ct) => db.Database.ExecuteSqlAsync(
        $"IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedHousehold WHERE Id = 1) INSERT dbo.MyDailyFeedHousehold(Id) VALUES (1)", ct);
}
