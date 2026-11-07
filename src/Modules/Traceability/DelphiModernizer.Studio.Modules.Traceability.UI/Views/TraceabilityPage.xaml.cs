using System;
using DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Traceability.UI.Views;

/// <summary>The Traceability page.</summary>
public sealed partial class TraceabilityPage : Page, IHasViewModel
{
    public TraceabilityPage(TraceabilityViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public TraceabilityViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
