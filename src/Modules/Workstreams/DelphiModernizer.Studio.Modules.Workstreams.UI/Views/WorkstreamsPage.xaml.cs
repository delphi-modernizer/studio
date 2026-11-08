using System;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>The Workstreams page.</summary>
public sealed partial class WorkstreamsPage : Page, IHasViewModel
{
    public WorkstreamsPage(WorkstreamsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public WorkstreamsViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    private void OnRoleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (RoleList.SelectedItem is RoleRow role && role != ViewModel.SelectedRole)
        {
            ViewModel.SelectedRole = role;
        }
    }

    /// <summary>
    /// Run…: the dialog stays open with the engine's reason until the run starts or the person cancels.
    /// </summary>
    private async void OnRun(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TaskRow task })
        {
            return;
        }

        if (!task.ClaudeReady)
        {
            OnSetUpClaude(sender, e);

            return;
        }

        var dialog = new RunRecipeDialog(
            task.Recipe, task.RoleName, null, ViewModel.DefaultBudget, ViewModel.NextGate).Over(XamlRoot);

        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? problem = await ViewModel.StartAsync(task.Recipe, dialog.Argument, dialog.BudgetUsd);

            if (problem is null)
            {
                return;
            }

            dialog.ShowProblem(problem);
        }
    }

    private void OnOpenBlockingRun(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TaskRow { Blocker: { } blocker } })
        {
            ViewModel.OpenRun(blocker.RunId);
        }
    }

    private void OnHistoryClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryRow row)
        {
            ViewModel.OpenRun(row.RunId);
        }
    }

    private async void OnSetUpClaude(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Claude is { IsReady: false } status)
        {
            ViewModel.Claude = await ClaudeCodeSetup.ShowAsync(XamlRoot, status);
            await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
        }
    }
}
