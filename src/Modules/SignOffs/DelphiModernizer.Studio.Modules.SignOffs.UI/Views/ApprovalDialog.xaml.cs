using System;
using System.Threading;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.Views;

/// <summary>Asks the person to confirm an approval or a revocation, with their git identity and a note.</summary>
public sealed partial class ApprovalDialog : ContentDialog
{
    public ApprovalDialog(ApprovalDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();

        if (viewModel.IsApprove)
        {
            PrimaryButtonStyle = (Style)Microsoft.UI.Xaml.Application.Current.Resources["AccentButtonStyle"];
        }

        Opened += OnOpened;
        PrimaryButtonClick += OnPrimaryButtonClick;
        ShowProblemsButton.Click += OnShowProblems;
    }

    public ApprovalDialogViewModel ViewModel { get; }

    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        NoteBox.Focus(FocusState.Programmatic);
    }

    /// <summary>The engine checks again on confirm; a refusal cancels the close, so the dialog keeps the note.</summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            args.Cancel = !await ViewModel.ConfirmAsync(CancellationToken.None);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnShowProblems(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowProblemsAfter = true;
        Hide();
    }
}
