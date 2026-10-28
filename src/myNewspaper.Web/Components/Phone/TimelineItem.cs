namespace myNewspaper.Web.Components.Phone;

/// <summary>
/// One row of a <see cref="PhoneTimeline"/>. <paramref name="Color"/> is one of the .mx-c-* classes; an
/// <paramref name="External"/> link opens in a new tab, as article links do everywhere else.
/// </summary>
public sealed record TimelineItem(string Time, string Title, string? Sub, string? Tag, string Href, string Color = "mx-c-world",
    bool External = false);
