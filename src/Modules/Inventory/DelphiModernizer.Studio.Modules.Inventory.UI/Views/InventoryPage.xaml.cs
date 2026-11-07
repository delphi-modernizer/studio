using System;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Inventory.UI.Views;

/// <summary>The Inventory page.</summary>
public sealed partial class InventoryPage : Page, IHasViewModel
{
    public InventoryPage(InventoryViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChanged;

        ListViewBase[] tables = [UnitsList, ScreensList, SqlList, RisksList, DiagnosticsList];

        foreach (var table in tables)
        {
            table.ContainerContentChanging += (_, args) => NameRow(args);
        }

        // A table owns its scrolling, no taller than the page, so a large codebase's rows stay virtualised.
        PageScroller.SizeChanged += (_, args) => Array.ForEach(tables, t => t.MaxHeight = args.NewSize.Height);
    }

    public InventoryViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    /// <summary>The tabs are rebuilt with their counts after every scan, keeping the person's tab.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(InventoryViewModel.Tabs))
        {
            return;
        }

        TabBar.Items.Clear();

        foreach (var tab in ViewModel.Tabs)
        {
            var item = new SelectorBarItem { Text = tab.Text, Tag = tab.Tab };
            AutomationProperties.SetName(item, tab.AnnouncedName);
            TabBar.Items.Add(item);
        }

        TabBar.SelectedItem = TabBar.Items.FirstOrDefault(i => (InventoryTab)i.Tag == ViewModel.Tab);
    }

    private void OnTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is InventoryTab tab)
        {
            ViewModel.Tab = tab;
        }
    }

    private async void OnOpenMarkdown(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenMarkdownAsync();
    }

    /// <summary>A table row names its container, which is what Narrator reads as the arrow keys move.</summary>
    private static void NameRow(ContainerContentChangingEventArgs args)
    {
        var name = args.Item switch {
            UnitRow row => row.AnnouncedName,
            ScreenRow row => row.AnnouncedName,
            SqlRow row => row.AnnouncedName,
            RiskRow row => row.AnnouncedName,
            DiagnosticRow row => row.Message,
            _ => null,
        };

        if (name is not null)
        {
            AutomationProperties.SetName(args.ItemContainer, name);
        }
    }

    private void OnSortClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SortHeader header })
        {
            ViewModel.SortBy(header.Column);
        }
    }

    /// <summary>Enter or a click on a row opens what it points to, as its own button or link does.</summary>
    private async void OnRowClicked(object sender, ItemClickEventArgs e)
    {
        var link = e.ClickedItem switch {
            UnitRow { IsUnit: true } row => row.Path,
            ScreenRow row => row.Path,
            SqlRow row => row.Link,
            RiskRow row => row.Link,
            DiagnosticRow row => row.Link,
            _ => null,
        };

        if (link is not null)
        {
            await ViewModel.OpenAsync(link);
        }
    }

    private async void OnLinkClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string link })
        {
            await ViewModel.OpenAsync(link);
        }
    }
}
