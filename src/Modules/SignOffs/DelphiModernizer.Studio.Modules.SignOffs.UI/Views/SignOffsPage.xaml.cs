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

    /// <summary>A revocation shows the history glyph; an approval, the filled certificate.</summary>
    public static string HistoryGlyph(bool isApproval)
    {
        return isApproval ? StudioIcons.Approved : StudioIcons.History;
    }

    /// <summary>A file changed since approval is drawn in the caution colour, with its "changed" mark.</summary>
    public static Brush CoveredBrush(bool changed)
    {
        return Resource(changed ? "SystemFillColorCautionBrush" : "TextFillColorSecondaryBrush");
    }

    /// <summary>Wide: the checks and the approval panel side by side, 24 apart; narrow: the panel 16 below.</summary>
    public static double ColumnGap(bool isNarrow)
    {
        return isNarrow ? 0 : 24;
    }

    public static double RowGap(bool isNarrow)
    {
        return isNarrow ? 16 : 0;
    }

    public static GridLength PanelWidth(bool isNarrow)
    {
        return new GridLength(isNarrow ? 0 : 320);
    }

    public static int PanelRow(bool isNarrow)
    {
        return isNarrow ? 1 : 0;
    }

    public static int PanelColumn(bool isNarrow)
    {
        return isNarrow ? 0 : 1;
    }

    private static Brush Resource(string key)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    private void OnLayoutChanged(object sender, VisualStateChangedEventArgs e)
    {
        ViewModel.IsNarrow = e.NewState?.Name == nameof(Narrow);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsNarrow = Layout.CurrentState?.Name == nameof(Narrow);
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
