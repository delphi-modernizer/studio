using System;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.Views;

/// <summary>The Sign-offs page.</summary>
public sealed partial class SignOffsPage : Page, IHasViewModel
{
    public SignOffsPage(SignOffsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SignOffsViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;
}
