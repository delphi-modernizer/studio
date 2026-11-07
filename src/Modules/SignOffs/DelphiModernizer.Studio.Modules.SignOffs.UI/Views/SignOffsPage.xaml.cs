using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

    /// <summary>The next sign-off's number carries the accent ring; x:Bind calls these from the row template.</summary>
    public static Brush NumberBrush(bool isNext)
    {
        return Resource(isNext ? "AccentFillColorDefaultBrush" : "ControlStrongStrokeColorDefaultBrush");
    }

    public static Thickness NumberThickness(bool isNext)
    {
        return new Thickness(isNext ? 2 : 1);
    }

    public static Brush CheckBrush(CheckTone tone)
    {
        return Resource(tone switch {
            CheckTone.Passed => "SystemFillColorSuccessBrush",
            CheckTone.Failing => "SystemFillColorCriticalBrush",
            _ => "TextFillColorSecondaryBrush",
        });
    }

    /// <summary>A current approval shows the filled certificate in the success colour; a stale one, the outline.</summary>
    public static Brush ApprovalBrush(bool isApproved)
    {
        return Resource(isApproved ? "SystemFillColorSuccessBrush" : "TextFillColorSecondaryBrush");
    }

    public static string ApprovalGlyph(bool isApproved)
    {
        return isApproved ? StudioIcons.Approved : StudioIcons.Approve;
    }

    private static Brush Resource(string key)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    private async void OnApprove(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SignOffRow signOff })
        {
            await ShowDialogAsync(signOff, isRevoke: false);
        }
    }

    private async void OnRevoke(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SignOffRow signOff })
        {
            await ShowDialogAsync(signOff, isRevoke: true);
        }
    }

    /// <summary>Only this dialog records an approval or a revocation; the list is read again after one.</summary>
    private async Task ShowDialogAsync(SignOffRow signOff, bool isRevoke)
    {
        var dialogViewModel = await ViewModel.PrepareDialogAsync(signOff, isRevoke);
        var dialog = new ApprovalDialog(dialogViewModel) { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (dialogViewModel.ShowProblemsAfter)
        {
            ViewModel.ShowProblems();
        }
        else if (result == ContentDialogResult.Primary || dialogViewModel.IsRefused)
        {
            await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
        }
    }

    private void OnShowProblems(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowProblems();
    }

    private void OnPrepare(object sender, RoutedEventArgs e)
    {
        ViewModel.Prepare();
    }

    private void OnCopyFingerprint(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string fingerprint })
        {
            ViewModel.Copy(fingerprint);
        }
    }
}
