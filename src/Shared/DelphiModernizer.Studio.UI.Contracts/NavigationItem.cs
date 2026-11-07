using System;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>Where the shell puts an item in the navigation pane.</summary>
public enum NavigationPlacement
{
    Menu,
    Footer,
}

/// <summary>A page a module adds to the navigation pane.</summary>
/// <param name="Glyph">A Studio Icons glyph from <c>StudioIcons</c>.</param>
/// <param name="SelectedGlyph">The filled variant the pane shows while the page is selected.</param>
/// <param name="Order">The position in the pane; lower comes first.</param>
public sealed record NavigationItem(string Title, string Glyph, string SelectedGlyph, Type PageType, int Order)
{
    public NavigationPlacement Placement { get; init; } = NavigationPlacement.Menu;

    /// <summary>The count the item shows in an InfoBadge, resolved once the shell is built; null shows none.</summary>
    public Func<IServiceProvider, IBadgeSource>? Badge { get; init; }
}
