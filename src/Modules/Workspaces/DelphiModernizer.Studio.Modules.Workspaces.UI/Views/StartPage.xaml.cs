using System;
using DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.Views;

/// <summary>The Start page.</summary>
public sealed partial class StartPage : Page, IHasViewModel
{
    public StartPage(StartViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public StartViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    private async void OnRecentClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentWorkspaceRow row)
        {
            await ViewModel.OpenRecentCommand.ExecuteAsync(row);
        }
    }

    /// <summary>The link under the Claude Code card: Settings, the set-up dialog, or a new test.</summary>
    private async void OnClaudeLinkClicked(object sender, RoutedEventArgs e)
    {
        var card = ViewModel.Claude;

        switch (card.Action)
        {
            case ClaudeCardAction.OpenSettings:
                ViewModel.OpenSettings();
                break;
            case ClaudeCardAction.SetUp when card.Status is { } status:
                ViewModel.ShowClaude(await ClaudeCodeSetup.ShowAsync(XamlRoot, status));
                break;
            case ClaudeCardAction.TestAgain:
                await ViewModel.CheckClaudeCommand.ExecuteAsync(parameter: null);
                break;
        }
    }
}
