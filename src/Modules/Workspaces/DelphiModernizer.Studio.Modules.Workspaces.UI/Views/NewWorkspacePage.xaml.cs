using System;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.Views;

/// <summary>The New workspace wizard.</summary>
/// <remarks>
/// Each new step puts focus on its heading, so Narrator reads "Step 2 of 5, Legacy source" before the first field;
/// while creating, each step that starts or ends is read politely.
/// </remarks>
public sealed partial class NewWorkspacePage : Page, IHasViewModel
{
    public NewWorkspacePage(NewWorkspaceViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += (_, _) => Listen(false);
    }

    public NewWorkspaceViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
        Listen(true);
        NameBox.Focus(FocusState.Programmatic);
    }

    private void Listen(bool listen)
    {
        ViewModel.PropertyChanged -= OnViewModelChanged;

        foreach (var row in ViewModel.Creation)
        {
            row.PropertyChanged -= OnCreationStepChanged;
        }

        if (!listen)
        {
            return;
        }

        ViewModel.PropertyChanged += OnViewModelChanged;

        foreach (var row in ViewModel.Creation)
        {
            row.PropertyChanged += OnCreationStepChanged;
        }
    }

    /// <remarks>Focus moves once the bindings have shown the new step; a collapsed element cannot take it.</remarks>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        UIElement? target = e.PropertyName switch {
            nameof(NewWorkspaceViewModel.StepHeading) when ViewModel.IsCreated => OpenOverviewButton,
            nameof(NewWorkspaceViewModel.StepHeading) => StepHeading,
            nameof(NewWorkspaceViewModel.HasFolderProblem) when ViewModel.HasFolderProblem => FolderBox,
            _ => null,
        };

        if (e.PropertyName == nameof(NewWorkspaceViewModel.StepHeading))
        {
            PageScroller.ChangeView(horizontalOffset: null, verticalOffset: 0, zoomFactor: null, disableAnimation: true);
        }

        if (target is not null)
        {
            DispatcherQueue.TryEnqueue(() => target.Focus(FocusState.Programmatic));
        }
    }

    /// <summary>The chosen target card carries the accent border (canvas .choice.on).</summary>
    private void OnTargetChoicesChanged(object sender, RoutedEventArgs e)
    {
        var choices = TargetChoices.Items
            .OfType<RadioButton>()
            .ToList();

        for (int i = 0; i < choices.Count; i++)
        {
            choices[i].Style = (Style)Resources[i == TargetChoices.SelectedIndex ? "ChosenChoiceStyle" : "ChoiceStyle"];
        }
    }

    private void OnStepPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        FitStep(args.Element);
    }

    private void OnWidthStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        for (int i = 0; i < ViewModel.Steps.Count; i++)
        {
            if (StepsRepeater.TryGetElement(i) is { } step)
            {
                FitStep(step);
            }
        }
    }

    /// <summary>Sets a step row as the WizardMin board draws it under 1100: 4 around, its label at 12.</summary>
    /// <remarks>A visual state cannot reach into the repeater's template, so the rows are set here.</remarks>
    private void FitStep(UIElement element)
    {
        if (element is not Grid step)
        {
            return;
        }

        bool narrow = WidthStates.CurrentState?.Name == "Narrow";
        var padding = narrow ? new Thickness(4) : new Thickness(8, 4, 8, 4);
        step.Padding = padding;

        foreach (var child in step.Children)
        {
            if (child is TextBlock label)
            {
                label.FontSize = narrow ? 12 : 14;
            }
            else if (child is Border { Child: null } backplate)
            {
                backplate.Margin = new Thickness(-padding.Left, -padding.Top, -padding.Right, -padding.Bottom);
            }
        }
    }

    private void OnCreationStepChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CreationStepRow { State: not null } row && e.PropertyName == nameof(CreationStepRow.State))
        {
            Narration.Announce(StepHeading, row.AnnouncedName);
        }
    }

    /// <summary>
    /// A path typed by hand is previewed once the person leaves the box, as a browsed one is at once.
    /// </summary>
    private async void OnLegacySourceLostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Preview == PreviewState.None)
        {
            await ViewModel.PreviewCommand.ExecuteAsync(parameter: null);
        }
    }
}
