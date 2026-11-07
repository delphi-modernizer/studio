using System;
using DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
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
    }

    public DeliverablesViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
