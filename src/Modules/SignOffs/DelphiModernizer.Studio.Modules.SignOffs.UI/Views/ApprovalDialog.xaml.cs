using System;
using System.ComponentModel;
using System.Threading;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using DelphiModernizer.Studio.UI.Common.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.Views;

/// <summary>Asks the person to confirm an approval or a revocation, with their git identity and a note.</summary>
public sealed partial class ApprovalDialog : ContentDialog
{
    private bool _targetTakesFirstFocus = true;

    public ApprovalDialog(ApprovalDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        DialogMargins.Apply(this);

        if (viewModel.IsApprove)
        {
            PrimaryButtonStyle = (Style)Microsoft.UI.Xaml.Application.Current.Resources["AccentButtonStyle"];
        }

        Opened += OnOpened;
        Body.GettingFocus += OnBodyGettingFocus;
        PrimaryButtonClick += OnPrimaryButtonClick;
        ShowProblemsButton.Click += OnShowProblems;
        ViewModel.PropertyChanged += OnViewModelChanged;
    }

    public ApprovalDialogViewModel ViewModel { get; }

    /// <summary>Focus starts in the note; without a git identity the note is disabled and Cancel takes it.</summary>
    /// <remarks>
    /// No default button, so Enter never approves and Cancel is drawn plain, not accent; Escape cancels.
    /// </remarks>
    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        FocusTarget().Focus(FocusState.Programmatic);
    }

    /// <remarks>
    /// The dialog also focuses its first field when its layout loads, which can come after Opened. That first
    /// programmatic move onto the first field goes to the target; any other move, and every move the person makes,
    /// stays as it is.
    /// </remarks>
    private void OnBodyGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (!_targetTakesFirstFocus)
        {
            return;
        }

        if (args.FocusState != FocusState.Programmatic)
        {
            _targetTakesFirstFocus = false;

            return;
        }

        var target = FocusTarget();

        if (args.NewFocusedElement == FocusManager.FindFirstFocusableElement(Body) && args.NewFocusedElement != target)
        {
            args.TrySetNewFocusedElement(target);
            _targetTakesFirstFocus = false;
        }
    }

    private Control FocusTarget()
    {
        return ViewModel.HasIdentity ? NoteBox : GetTemplateChild("CloseButton") as Control ?? NoteBox;
    }

    /// <summary>
    /// The engine checks again on confirm; a refusal cancels the close, so the dialog keeps the note.
    /// </summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            args.Cancel = !await ViewModel.ConfirmAsync(CancellationToken.None);
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>The refusal InfoBar is assertive; opening it is the change Narrator reads at once.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ApprovalDialogViewModel.IsRefused) || !ViewModel.IsRefused)
        {
            return;
        }

        // The InfoBar opens through its binding first, so the announcement waits a turn.
        DispatcherQueue.TryEnqueue(() => {
            var peer = FrameworkElementAutomationPeer.FromElement(RefusalBar);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        });
    }

    private void OnShowProblems(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowProblemsAfter = true;
        Hide();
    }
}
