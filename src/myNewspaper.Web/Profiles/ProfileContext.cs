using Microsoft.Extensions.Options;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// The device and profile behind the current request, filled in by <see cref="ProfileGateMiddleware"/>. Stays empty
/// while profiles are switched off. Read it during the HTTP request (static rendering and endpoints), never later from
/// inside an interactive circuit.
/// </summary>
public sealed class ProfileContext(IOptions<ProfilesOptions> options)
{
    public bool Enabled => options.Value.Enabled;
    public Profile? Profile { get; internal set; }
    public ProfileDevice? Device { get; internal set; }
    public bool HasProfile => Profile is not null;
    public int Id => Profile?.Id ?? throw new InvalidOperationException("No profile selected.");

    /// <summary>The open profile is the household admin's (Manage profile), so its pages show "Admin sign in".</summary>
    public bool IsHouseholdAdmin { get; internal set; }

    /// <summary>Who public TV and YouTube reads are for. With profiles on and none open, nothing is shown.</summary>
    public Viewer Viewer => !Enabled ? Viewer.Household : Profile is { } p ? Viewer.ForProfile(p.Id) : Viewer.Nobody;

    /// <summary>The Saved list to use: null is the legacy shared list, used only while profiles are off.</summary>
    public int? SavedListProfileId => Enabled ? Id : null;
}
