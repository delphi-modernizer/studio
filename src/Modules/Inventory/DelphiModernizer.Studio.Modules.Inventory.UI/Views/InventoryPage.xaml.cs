using System;
using DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
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
    }

    public InventoryViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
