using System;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
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
            TabBar.Items.Add(new SelectorBarItem { Text = tab.Text, Tag = tab.Tab });
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

    private async void OnLinkClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string link })
        {
            await ViewModel.OpenAsync(link);
        }
    }
}
