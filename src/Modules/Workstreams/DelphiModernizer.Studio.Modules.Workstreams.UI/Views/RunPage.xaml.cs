using System;
using System.ComponentModel;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>The Run page: one agent run, live or recorded.</summary>
public sealed partial class RunPage : Page, IHasViewModel
{
    /// <summary>
    /// How much a new run's budget grows after a run reached its own; the design draws "[higher budget]".
    /// </summary>
    private const decimal _budgetGrowth = 2m;

    public RunPage(RunViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChanged;
        ViewModel.BudgetReached += OnBudgetReached;
    }

    public RunViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    public static string FailedText(string? message)
    {
        return $"{message ?? "The CLI ended without a result."} Anything it wrote before that stays as Draft.";
    }

    public static string CostName(string cost)
    {
        return $"Cost {cost}";
    }

    public static string OpenName(string name)
    {
        return $"Open {name} in your editor";
    }

    public static Visibility Not(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Open the draft is the accent action once the agent finished; otherwise a plain button.</summary>
    public static Style DraftStyle(RunOutcome outcome)
    {
        string key = outcome == RunOutcome.Completed ? "AccentButtonStyle" : "DefaultButtonStyle";

        return (Style)Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Outcome and IsStarting both raise Pill; the end card changes with Outcome alone.
        var changed = e.PropertyName switch {
            nameof(RunViewModel.Pill) => (FrameworkElement)Pill,
            nameof(RunViewModel.Outcome) => EndCard,
            _ => null,
        };

        if (changed is null)
        {
            return;
        }

        // The element takes its new words through its binding first, so the announcement waits a turn.
        DispatcherQueue.TryEnqueue(() => {
            var peer = FrameworkElementAutomationPeer.FromElement(changed);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        });
    }

    private async void OnBudgetReached(object? sender, EventArgs e)
    {
        string turns = ViewModel.Turns == "1" ? "1 turn" : $"{ViewModel.Turns} turns";
        var dialog = new BudgetDialog(
            $"{ViewModel.Title} stopped at its {RunWords.Dollars(ViewModel.BudgetUsd)} budget after {turns}. "
                + $"What it wrote so far is in the {ViewModel.Writing} as a Draft.",
            ViewModel.Cost,
            ViewModel.BudgetUsd * _budgetGrowth).Over(XamlRoot);

        switch (await dialog.ShowAsync())
        {
            case ContentDialogResult.Primary:
                await ViewModel.RunAgainAsync(ViewModel.Argument, dialog.BudgetUsd);
                break;
            case ContentDialogResult.Secondary:
                await ViewModel.OpenDraftAsync();
                break;
        }
    }

    private async void OnRunAgain(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Recipe is not { } recipe)
        {
            return;
        }

        var dialog = new RunRecipeDialog(
            recipe, ViewModel.RoleName, ViewModel.Argument, ViewModel.NextBudgetUsd, nextGate: null).Over(XamlRoot);

        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? problem = await ViewModel.RunAgainAsync(dialog.Argument, dialog.BudgetUsd);

            if (problem is null)
            {
                return;
            }

            dialog.ShowProblem(problem);
        }
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        ViewModel.Stop();
    }

    private void OnOpenWorkstreams(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenWorkstreams();
    }

    private void OnOpenRole(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenRole();
    }

    private async void OnOpenDraft(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenDraftAsync();
    }

    private void OnExpandAll(object sender, RoutedEventArgs e)
    {
        ViewModel.ExpandAll();
    }

    private void OnTranscriptItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolRow row)
        {
            ViewModel.Toggle(row);
        }
    }

    private async void OnOpenFile(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: FileRow row })
        {
            await ViewModel.OpenFileAsync(row.Path);
        }
    }
}
