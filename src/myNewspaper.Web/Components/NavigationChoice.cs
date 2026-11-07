namespace myNewspaper.Web.Components;

/// <summary>One choice in a <see cref="SectionSelect"/>. Consecutive choices with the same <paramref name="Group"/>
/// share an optgroup heading in the phone dropdown (YouTube: Categories, Channels); null means no heading.</summary>
public sealed record NavigationChoice(string Href, string Label, string? Group = null);
