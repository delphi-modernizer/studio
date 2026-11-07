using System;
using DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Overview.UI.Views;

/// <summary>The Overview page.</summary>
public sealed partial class OverviewPage : Page, IHasViewModel
{
    public OverviewPage(OverviewViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public OverviewViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
