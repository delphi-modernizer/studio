using System;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>The Workstreams page.</summary>
public sealed partial class WorkstreamsPage : Page, IHasViewModel
{
    public WorkstreamsPage(WorkstreamsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public WorkstreamsViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
