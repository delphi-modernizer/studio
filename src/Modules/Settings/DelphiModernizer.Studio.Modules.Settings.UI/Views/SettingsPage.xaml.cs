using System;
using System.ComponentModel;
using DelphiModernizer.Studio.Modules.Settings.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Settings.UI.Views;

/// <summary>The Settings page.</summary>
public sealed partial class SettingsPage : Page, IHasViewModel
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelChanged;
    }

    public SettingsViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    /// <remarks>The test the page runs on opening is not read out; the ones the person starts are.</remarks>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestCommand.ExecuteAsync(parameter: null);
        ViewModel.PropertyChanged += OnViewModelChanged;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.Claude) && ViewModel.Claude is { } status)
        {
            Narration.Announce(TestButton, status.Describe());
        }
    }
}
