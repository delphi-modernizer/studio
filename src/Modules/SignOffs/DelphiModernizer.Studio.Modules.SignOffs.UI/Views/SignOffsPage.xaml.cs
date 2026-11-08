using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
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
        if (sender is FrameworkElement { Tag: SignOffRow signOff })
        {
            await ShowDialogAsync(signOff, isRevoke: false);
        }
    }

    private async void OnRevoke(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: SignOffRow signOff })
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
