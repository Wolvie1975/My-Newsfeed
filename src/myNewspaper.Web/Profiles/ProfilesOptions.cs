namespace myNewspaper.Web.Profiles;

public sealed class ProfilesOptions
{
    public const string Section = "Profiles";

    /// <summary>
    /// Off until switch-on (handoff stage U1): no gate, no public profile routes, and the household viewer for TV and
    /// YouTube. Admin profile pages work either way. Turning it back off is the rollback.
    /// </summary>
    public bool Enabled { get; set; }
}
