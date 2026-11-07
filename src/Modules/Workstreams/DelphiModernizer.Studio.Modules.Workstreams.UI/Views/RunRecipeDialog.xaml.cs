using System;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>Run an agent task: its Scope or Area when the recipe takes one, then this run's budget.</summary>
public sealed partial class RunRecipeDialog : ContentDialog
{
    private readonly Recipe _recipe;
    private bool _touched;

    /// <param name="argument">The argument to start with, as Run again passes the earlier run's.</param>
    public RunRecipeDialog(Recipe recipe, string roleName, string? argument, decimal budgetUsd)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        InitializeComponent();
        _recipe = recipe;
        RoleText.Text = roleName;
        TitleText.Text = $"Run {recipe.Title}";
        Lead.Text = $"The agent writes the {RunWords.Writes(recipe)} as a draft, each claim with a link to its evidence and a fact label. You review the draft afterwards.";
        var name = RunWords.Capitalized(recipe.Argument.Name);
        ArgumentBox.Header = $"{name} ({(recipe.Argument.Required ? "required" : "optional")})";
        ArgumentBox.PlaceholderText = recipe.Argument.Description;
        ArgumentBox.Text = argument ?? string.Empty;
        AutomationProperties.SetName(ArgumentBox, name);
        AutomationProperties.SetIsRequiredForForm(ArgumentBox, recipe.Argument.Required);
        BudgetBox.Value = (double)budgetUsd;
        ArgumentBox.TextChanged += OnArgumentChanged;
        BudgetBox.ValueChanged += OnBudgetChanged;
        Opened += OnOpened;
        Validate();
    }

    public string? Argument => string.IsNullOrWhiteSpace(ArgumentBox.Text) ? null : ArgumentBox.Text.Trim();

    public decimal BudgetUsd => double.IsNaN(BudgetBox.Value) ? 0 : (decimal)BudgetBox.Value;

    /// <summary>Shows why the engine refused to start the run, keeping the dialog open for another try.</summary>
    public void ShowProblem(string message)
    {
        StartProblem.Message = message;
        StartProblem.IsOpen = true;
    }

    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        ArgumentBox.Focus(FocusState.Programmatic);
    }

    private void OnArgumentChanged(object sender, TextChangedEventArgs e)
    {
        _touched = true;
        Validate();
    }

    private void OnBudgetChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        Validate();
    }

    private void Validate()
    {
        var missing = _recipe.Argument.Required && Argument is null;
        ArgumentError.Text = $"This task needs a {_recipe.Argument.Name}.";
        ArgumentError.Visibility = missing && _touched ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetHelpText(ArgumentBox, missing && _touched ? ArgumentError.Text : string.Empty);
        IsPrimaryButtonEnabled = !missing && BudgetUsd > 0 && !ArgumentBox.Text.Contains('\n', StringComparison.Ordinal);
    }
}
