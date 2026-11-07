using System;
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
