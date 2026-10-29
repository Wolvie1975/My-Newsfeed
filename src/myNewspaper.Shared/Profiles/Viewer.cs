namespace myNewspaper.Web.Profiles;

/// <summary>
/// Who a public TV or YouTube read is for. <see cref="Household"/> is today's behavior (everything collected) and exists
/// only while profiles are switched off; once they're on, every public caller passes the current profile.
/// </summary>
public sealed class Viewer
{
    private Viewer(int? profileId, bool household) { ProfileId = profileId; IsHousehold = household; }

    /// <summary>Switch-off mode only: every enabled series and every channel.</summary>
    public static Viewer Household { get; } = new(null, true);

    /// <summary>Sees nothing. Used when profiles are on but no profile is open, so a missed gate fails closed.</summary>
    public static Viewer Nobody { get; } = new(null, false);

    public static Viewer ForProfile(int profileId) =>
        profileId > 0 ? new(profileId, false) : throw new ArgumentOutOfRangeException(nameof(profileId));

    public int? ProfileId { get; }
    public bool IsHousehold { get; }

    /// <summary>The ID the follow filters compare against; 0 matches no profile, because IDs start at 1.</summary>
    internal int FilterId => ProfileId ?? 0;
}
