using System;
using System.ComponentModel;
using DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Overview.UI.Views;

/// <summary>The Overview page.</summary>
public sealed partial class OverviewPage : Page, IHasViewModel
{
    public OverviewPage(OverviewViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChanged;
    }

    public OverviewViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    /// <summary>The next-step bar's severity; x:Bind calls it as the step changes.</summary>
    public static InfoBarSeverity SeverityOf(NextStep step)
    {
        return step.Tone switch {
            NextStepTone.Success => InfoBarSeverity.Success,
            NextStepTone.Caution => InfoBarSeverity.Warning,
            NextStepTone.Critical => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational,
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverviewViewModel.Gates))
        {
            ShowNextGate();
        }
    }

    private void OnReviewSignOffs(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSignOffs(gate: null);
    }

    private void OnNextStepAction(object sender, RoutedEventArgs e)
    {
        var step = ViewModel.NextStep;

        if (step.Action == NextStepAction.Traceability)
        {
            ViewModel.OpenTraceability();

            return;
        }

        ViewModel.OpenSignOffs(step.Gate);
    }

    private void OnLegacyClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenInventory();
    }

    private void OnTeamClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: TeamNode team })
        {
            ViewModel.OpenWorkstreams(team.Code);
        }
    }

    private void OnArchitectClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenWorkstreams(ViewModel.Architect.Code);
    }

    private void OnGateClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: GateNode gate })
        {
            ViewModel.OpenSignOffs(gate.Gate);
        }
    }

    private async void OnQuestionClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: QuestionRow question })
        {
            await ViewModel.OpenQuestionAsync(question);
        }
    }

    private void OnAllRuns(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenWorkstreams(roleCode: null);
    }

    private async void OnRunsEmptyLink(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Claude is { IsReady: false } status)
        {
            ViewModel.Claude = await ClaudeCodeSetup.ShowAsync(XamlRoot, status);

            return;
        }

        ViewModel.OpenWorkstreams(roleCode: null);
    }
}
