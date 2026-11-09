using DelphiModernizer.Studio.UI.Common.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>A run reached its budget: what it spent, and a new run of the same task with a new budget.</summary>
public sealed partial class BudgetDialog : ContentDialog
{
    private bool _closeTakesFirstFocus = true;

    public BudgetDialog(string lead, string spent, decimal newBudgetUsd)
    {
        InitializeComponent();
        DialogMargins.Apply(this);
        Lead.Text = lead;
        Spent.Text = spent;
        BudgetBox.Value = (double)newBudgetUsd;
        Opened += OnOpened;
        BudgetBox.GettingFocus += OnBudgetGettingFocus;
    }

    public decimal BudgetUsd => double.IsNaN(BudgetBox.Value) ? 0 : (decimal)BudgetBox.Value;

    /// <summary>Focus starts on Close; Tab then goes to the budget, Run again and Open the draft.</summary>
    /// <remarks>
    /// No default button, so Enter never spends money and Close is drawn plain; Escape and Enter on Close close it.
    /// </remarks>
    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        if (GetTemplateChild("PrimaryButton") is Button runAgain)
        {
            runAgain.ContentTemplate = (DataTemplate)Resources["PlayContent"];
        }

        (GetTemplateChild("CloseButton") as Control)?.Focus(FocusState.Programmatic);
    }

    /// <remarks>
    /// The dialog also focuses its first field, the budget, when its layout loads, which can come after Opened. That
    /// first programmatic move goes to Close; any later move, and every move the person makes, stays as it is.
    /// </remarks>
    private void OnBudgetGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (_closeTakesFirstFocus
            && args.FocusState == FocusState.Programmatic
            && GetTemplateChild("CloseButton") is Control close)
        {
            args.TrySetNewFocusedElement(close);
        }

        _closeTakesFirstFocus = false;
    }
}
