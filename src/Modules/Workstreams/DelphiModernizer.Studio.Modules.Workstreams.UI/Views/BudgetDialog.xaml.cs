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
    }

    public decimal BudgetUsd => double.IsNaN(BudgetBox.Value) ? 0 : (decimal)BudgetBox.Value;
}
