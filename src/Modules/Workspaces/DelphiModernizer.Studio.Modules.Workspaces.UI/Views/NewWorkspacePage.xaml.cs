using System;
using DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.Views;

/// <summary>The New workspace wizard.</summary>
public sealed partial class NewWorkspacePage : Page, IHasViewModel
{
    public NewWorkspacePage(NewWorkspaceViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    public NewWorkspaceViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadCommand.ExecuteAsync(parameter: null);
        NameBox.Focus(FocusState.Programmatic);
    }

    private async void OnNext(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextCommand.ExecuteAsync(parameter: null);
        FocusStep();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        ViewModel.BackCommand.Execute(parameter: null);
        FocusStep();
    }

    /// <summary>A path typed by hand is previewed once the person leaves the box, as a browsed one is at once.</summary>
    private async void OnLegacySourceLostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Preview == PreviewState.None)
        {
            await ViewModel.PreviewCommand.ExecuteAsync(parameter: null);
        }
    }

    /// <summary>Focus moves to the first field of each step, and to Open the Overview once it is created.</summary>
    private void FocusStep()
    {
        Control? first = ViewModel switch {
            { IsCreated: true } => OpenOverviewButton,
            { IsStep1: true } => NameBox,
            { IsStep2: true } => LegacyBox,
            { IsStep3: true } => TargetChoices,
            _ => null,
        };

        first?.Focus(FocusState.Programmatic);
    }
}
