using System;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Modules.Settings.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DelphiModernizer.Studio.Modules.Settings.UI.Views;

/// <summary>The Settings page.</summary>
public sealed partial class SettingsPage : Page, IHasViewModel
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestCommand.ExecuteAsync(parameter: null);
    }

    private void OnCopyVersions(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(StudioVersions.Current.Line);
        Clipboard.SetContent(package);
    }
}
