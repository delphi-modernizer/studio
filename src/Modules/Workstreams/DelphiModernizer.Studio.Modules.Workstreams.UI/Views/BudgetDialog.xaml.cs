using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>A run reached its budget: what it spent, and a new run of the same task with a new budget.</summary>
public sealed partial class BudgetDialog : ContentDialog
{
    public BudgetDialog(string lead, string spent, decimal newBudgetUsd)
    {
        InitializeComponent();
        Lead.Text = lead;
        Spent.Text = spent;
        BudgetBox.Value = (double)newBudgetUsd;
        Opened += OnOpened;
    }

    public decimal BudgetUsd => double.IsNaN(BudgetBox.Value) ? 0 : (decimal)BudgetBox.Value;

    /// <summary>Focus starts on Close; Tab then goes to the budget, Run again and Open the draft.</summary>
    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        (GetTemplateChild("CloseButton") as Control)?.Focus(FocusState.Programmatic);
    }
}
