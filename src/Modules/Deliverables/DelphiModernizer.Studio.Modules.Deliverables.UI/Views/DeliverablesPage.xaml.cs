using System;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Deliverables.UI.Views;

/// <summary>The Deliverables page.</summary>
public sealed partial class DeliverablesPage : Page, IHasViewModel
{
    public DeliverablesPage(DeliverablesViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChanged;
        Table.ContainerContentChanging += (_, args) => NameRow(args);
    }

    public DeliverablesViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    /// <summary>The status bar is rebuilt with the counts after every load, keeping the person's choice.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DeliverablesViewModel.Filters))
        {
            return;
        }

        FilterBar.Items.Clear();

        foreach (var choice in ViewModel.Filters)
        {
            FilterBar.Items.Add(
                CountedSelectorBarItem.Create(choice.Label, choice.CountText, choice.AnnouncedName, choice.Filter));
        }

        FilterBar.SelectedItem = FilterBar.Items.FirstOrDefault(i => (DeliverableFilter)i.Tag == ViewModel.Filter);
    }

    private void OnFilterChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is DeliverableFilter filter)
        {
            ViewModel.Filter = filter;
        }
    }

    private async void OnOpenDocs(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenDocsAsync();
    }

    private void OnGoToWorkstreams(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenWorkstreams();
    }

    /// <summary>A row names its container, which is what Narrator reads as the arrow keys move.</summary>
    private static void NameRow(ContainerContentChangingEventArgs args)
    {
        if (args.Item is DeliverableRow row)
        {
            AutomationProperties.SetName(args.ItemContainer, row.AnnouncedName);
        }
    }

    /// <summary>Enter or a click on a deliverable opens its file, as its own button does.</summary>
    private async void OnRowClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DeliverableRow { CanOpen: true } row)
        {
            await ViewModel.OpenAsync(row);
        }
    }

    private async void OnOpenDeliverable(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DeliverableRow row })
        {
            await ViewModel.OpenAsync(row);
        }
    }
}
