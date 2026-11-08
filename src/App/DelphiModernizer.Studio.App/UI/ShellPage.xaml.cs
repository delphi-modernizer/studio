using System;
using System.Collections.Generic;
using System.Linq;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DelphiModernizer.Studio.App.UI;

/// <summary>
/// The title bar and the navigation pane around the page frame; the pane is built from the modules' items.
/// </summary>
/// <remarks>
/// The pane stays open down to the 960 px minimum and collapses only from the title-bar toggle (design assumption,
/// open question 3). Every page stays selectable without a workspace; badges hide until one is open.
/// </remarks>
public sealed partial class ShellPage : Page
{
    private readonly INavigationService _navigation;
    private readonly IWorkspaceSession _session;
    private readonly IServiceProvider _services;
    private readonly List<NavigationItem> _items;
    private readonly Dictionary<string, NavigationViewItem> _containers = new(StringComparer.Ordinal);
    private readonly List<(NavigationViewItem Container, NavigationItem Item, IBadgeSource Source)> _badges = [];
    private bool _isLoaded;
    private NavigationViewItem? _shownOwner;

    public ShellPage(
        ShellViewModel viewModel,
        INavigationService navigation,
        IWorkspaceSession session,
        IEnumerable<IModule> modules,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(services);
        ViewModel = viewModel;
        _navigation = navigation;
        _session = session;
        _services = services;
        _items = [.. modules
            .SelectMany(m => m.GetNavigationItems())
            .OrderBy(i => i.Placement)
            .ThenBy(i => i.Order)];
        InitializeComponent();
        AppTitleBar.BackRequested += (_, _) => _navigation.GoBack();
        AppTitleBar.PaneToggleRequested += (_, _) => NavView.IsPaneOpen = !NavView.IsPaneOpen;
        NavView.Loaded += (_, _) => MakePaneOneTabStop();
        AppTitleBar.Loaded += (_, _) => DrawPaneToggleGlyph();
    }

    public ShellViewModel ViewModel { get; }

    /// <summary>The drag region the window hands to <c>SetTitleBar</c>.</summary>
    public UIElement TitleBar => AppTitleBar;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        _navigation.Initialize(NavFrame);
        _navigation.Navigated += OnNavigated;
        _session.Changed += (_, _) => DispatcherQueue.TryEnqueue(RefreshBadges);

        foreach (var item in _items)
        {
            AddItem(item);
        }

        _navigation.NavigateTo("Start");
    }

    private void AddItem(NavigationItem item)
    {
        string key = _navigation.GetPageKey(item.PageType);
        var container = new NavigationViewItem {
            Tag = key,
            Content = item.Title,
            Icon = Glyph(item.Glyph),
        };
        AutomationProperties.SetName(container, item.Title);
        AutomationProperties.SetAutomationId(container, key);
        _containers[key] = container;

        if (item.Badge is { } badge)
        {
            var source = badge(_services);
            string style = source.Tone == BadgeTone.Critical
                ? "CriticalValueInfoBadgeStyle"
                : "AccentValueInfoBadgeStyle";
            container.InfoBadge = new InfoBadge {
                Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[style],
            };
            _badges.Add((container, item, source));
            source.Changed += (_, _) => DispatcherQueue.TryEnqueue(() => ShowBadge(container, item, source));
            ShowBadge(container, item, source);
        }

        if (item.Placement == NavigationPlacement.Footer)
        {
            // The NavPane board draws a rule between the pages and Settings.
            if (NavView.FooterMenuItems.Count == 0)
            {
                NavView.FooterMenuItems.Add(new NavigationViewItemSeparator());
            }

            NavView.FooterMenuItems.Add(container);
        }
        else
        {
            NavView.MenuItems.Add(container);
        }
    }

    private static FontIcon Glyph(string glyph)
    {
        return new FontIcon {
            Glyph = glyph,
            Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["IconStyle"],
        };
    }

    /// <summary>Shows the count on the item's own badge, hidden at zero.</summary>
    /// <remarks>
    /// The badge is updated, never replaced: a new or removed InfoBadge drops the selected item's backplate, as on Run
    /// while the Workstreams badge counts the run.
    /// </remarks>
    private void ShowBadge(NavigationViewItem container, NavigationItem item, IBadgeSource source)
    {
        int count = _session.Current is null ? 0 : source.Count;
        var shown = container.InfoBadge;

        if (count == 0)
        {
            shown.Visibility = Visibility.Collapsed;
            AutomationProperties.SetName(container, item.Title);

            return;
        }

        shown.Value = count;
        shown.Visibility = Visibility.Visible;
        AutomationProperties.SetName(container, $"{item.Title}, {source.Describe(count)}");
    }

    private void RefreshBadges()
    {
        foreach (var (container, item, source) in _badges)
        {
            ShowBadge(container, item, source);
        }
    }

    /// <summary>Gives the selected item its filled glyph and every other item its regular one.</summary>
    private void ShowSelectedGlyphs(string? selectedKey)
    {
        foreach (var item in _items)
        {
            string key = _navigation.GetPageKey(item.PageType);

            if (_containers[key].Icon is FontIcon icon)
            {
                icon.Glyph = key == selectedKey ? item.SelectedGlyph : item.Glyph;
            }
        }
    }

    /// <summary>Opens the page of an item a person selects; selecting the shown page's own item navigates nowhere.</summary>
    /// <remarks>
    /// OnNavigated selects the shown page's item, and NavigationView may raise this after it returns, as under load
    /// after a fresh navigation; navigating again would put the page on the back stack a second time and turn Back on.
    /// </remarks>
    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem { Tag: string key } container
            && !ReferenceEquals(container, _shownOwner))
        {
            _navigation.NavigateTo(key);
        }
    }

    /// <summary>
    /// Clicking the selected item again returns to its page, as from a page reached inside it such as Run.
    /// </summary>
    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem { Tag: string key } container
            && ReferenceEquals(container, NavView.SelectedItem)
            && NavFrame.Content?.GetType().Name != key + "Page")
        {
            _navigation.NavigateTo(key);
        }
    }

    /// <summary>Follows Back and navigation from inside pages without navigating again.</summary>
    private void OnNavigated(object? sender, NavigationEvent e)
    {
        var owner = _containers.GetValueOrDefault(e.PageKey) ?? OwnerOf(e.PageType);
        _shownOwner = owner;
        NavView.SelectedItem = owner;

        ShowSelectedGlyphs(owner?.Tag as string);
    }

    /// <summary>The pane item of the module a page belongs to, such as Workstreams for Run.</summary>
    private NavigationViewItem? OwnerOf(Type pageType)
    {
        var owner = _items.FirstOrDefault(i => i.PageType.Assembly == pageType.Assembly);

        return owner is null ? null : _containers[_navigation.GetPageKey(owner.PageType)];
    }

    private void OnBackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _navigation.GoBack();
        args.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsXButton1Pressed)
        {
            _navigation.GoBack();
            e.Handled = true;
        }
    }

    private void OnNextRegionInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = MoveRegion(1);
    }

    private void OnPreviousRegionInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = MoveRegion(-1);
    }

    /// <summary>Moves focus to the next or previous of the three regions: title bar, pane, page.</summary>
    private bool MoveRegion(int step)
    {
        UIElement?[] regions = [SearchBox, NavView, NavFrame];
        var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        int current = Array.FindIndex(regions, region => region is not null && Contains(region, focused));
        var next = regions[(Math.Max(current, 0) + step + regions.Length) % regions.Length];

        return next switch {
            AutoSuggestBox search => search.Focus(FocusState.Keyboard),
            NavigationView => PaneItemToFocus().Focus(FocusState.Keyboard),
            _ => FocusManager.FindFirstFocusableElement(NavFrame) is UIElement first
                && first.Focus(FocusState.Keyboard),
        };
    }

    /// <summary>The pane, Settings in its footer included, is one tab stop with arrow keys inside.</summary>
    /// <remarks>NavigationView makes its menu and footer two stops; the grid holding both is a template part.</remarks>
    private void MakePaneOneTabStop()
    {
        if (Descendant(NavView, "PaneContentGrid") is { } pane)
        {
            pane.TabFocusNavigation = KeyboardNavigationMode.Once;
            pane.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
        }
    }

    /// <summary>Draws the pane toggle's glyph from Studio Icons, 12 wide as the TitleBar board has it.</summary>
    /// <remarks>WinUI's toggle draws Segoe Fluent's 16 wide glyph and exposes no property for it.</remarks>
    private void DrawPaneToggleGlyph()
    {
        if (Descendant(AppTitleBar, "PART_PaneToggleButton") is Button toggle)
        {
            toggle.FontFamily = (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["StudioIconsFontFamily"];
            toggle.Content = StudioIcons.Navigation;
        }
    }

    private static FrameworkElement? Descendant(DependencyObject root, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            if (Descendant(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>The pane is one tab stop, entered at the selected item.</summary>
    private Control PaneItemToFocus()
    {
        return NavView.SelectedItem as Control ?? NavView.MenuItems
            .OfType<Control>()
            .First();
    }

    private static bool Contains(DependencyObject region, DependencyObject? element)
    {
        for (var node = element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (ReferenceEquals(node, region))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Suggests the studio's pages by name.</summary>
    /// <remarks>
    /// What search covers is an open question (handoff, question 7); pages are the one answer that needs no data.
    /// </remarks>
    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        sender.ItemsSource = string.IsNullOrWhiteSpace(sender.Text)
            ? null
            : _items
                .Where(i => i.Title.Contains(sender.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Title)
                .ToList();
    }

    private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        Go(args.SelectedItem as string);
    }

    private void OnSearchSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Go(args.ChosenSuggestion as string ?? (sender.ItemsSource as IList<string>)?.FirstOrDefault());
    }

    private void Go(string? title)
    {
        if (_items.FirstOrDefault(i => i.Title == title) is { } item)
        {
            _navigation.NavigateTo(_navigation.GetPageKey(item.PageType));
            SearchBox.Text = string.Empty;
        }
    }
}
