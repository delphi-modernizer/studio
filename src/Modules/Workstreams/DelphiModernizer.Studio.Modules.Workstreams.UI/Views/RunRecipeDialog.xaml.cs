using System;
using System.Linq;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>A sign-off a gate argument names: the person reads its name, the agent gets its ID.</summary>
/// <remarks>The ComboBox shows <see cref="ToString"/>, so the record needs no binding metadata.</remarks>
public sealed record GateChoice(string Gate, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}

/// <summary>Run an agent task: its Scope or Area when the recipe takes one, then this run's budget.</summary>
/// <remarks>A recipe whose argument is <c>gate</c> offers the sign-offs by name, so no gate code is shown.</remarks>
public sealed partial class RunRecipeDialog : ContentDialog
{
    private const string _gateArgument = "gate";

    private readonly Recipe _recipe;
    private readonly bool _choosesGate;
    private bool _touched;

    /// <param name="argument">The argument to start with, as Run again passes the earlier run's.</param>
    /// <param name="nextGate">
    /// The sign-off a gate argument starts from when <paramref name="argument"/> is null.
    /// </param>
    public RunRecipeDialog(Recipe recipe, string roleName, string? argument, decimal budgetUsd, string? nextGate)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        InitializeComponent();
        _recipe = recipe;
        _choosesGate = recipe.Argument.Name == _gateArgument;
        RoleText.Text = roleName;
        TitleText.Text = $"Run {recipe.Title}";
        Lead.Text = $"The agent writes the {RunWords.Writes(recipe)} as a draft, "
            + "each claim with a link to its evidence and a fact label. You review the draft afterwards.";
        var name = RunWords.Capitalized(recipe.Argument.Name);
        ArgumentBox.Header = $"{name} ({(recipe.Argument.Required ? "required" : "optional")})";
        ArgumentBox.PlaceholderText = recipe.Argument.Description;
        ArgumentBox.Text = argument ?? string.Empty;
        AutomationProperties.SetName(ArgumentBox, name);
        AutomationProperties.SetIsRequiredForForm(ArgumentBox, recipe.Argument.Required);
        BudgetBox.Value = (double)budgetUsd;

        if (_choosesGate)
        {
            ShowGates(name, argument ?? nextGate);
        }

        ArgumentBox.TextChanged += OnArgumentChanged;
        BudgetBox.ValueChanged += OnBudgetChanged;
        Opened += OnOpened;
        Validate();
    }

    public string? Argument
    {
        get {
            if (_choosesGate)
            {
                return (GateBox.SelectedItem as GateChoice)?.Gate;
            }

            return string.IsNullOrWhiteSpace(ArgumentBox.Text) ? null : ArgumentBox.Text.Trim();
        }
    }

    public decimal BudgetUsd => double.IsNaN(BudgetBox.Value) ? 0 : (decimal)BudgetBox.Value;

    /// <summary>Shows why the engine refused to start the run, keeping the dialog open for another try.</summary>
    public void ShowProblem(string message)
    {
        StartProblem.Message = message;
        StartProblem.IsOpen = true;
    }

    private void ShowGates(string name, string? selected)
    {
        var gates = KitBundle.Embedded.Spec.Gates.Gates
            .Select(g => new GateChoice(g.Id, SignOffNames.Of(g.Id)))
            .ToList();
        ArgumentBox.Visibility = Visibility.Collapsed;
        GateBox.Visibility = Visibility.Visible;
        GateBox.Header = $"Sign-off ({(_recipe.Argument.Required ? "required" : "optional")})";
        GateBox.ItemsSource = gates;
        GateBox.SelectedItem = gates.Find(g => g.Gate == selected);
        AutomationProperties.SetName(GateBox, name);
        ArgumentHelp.Text = "The sign-off the Principal Architect prepares.";
        GateBox.SelectionChanged += OnGateChanged;
    }

    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        (_choosesGate ? (Control)GateBox : ArgumentBox).Focus(FocusState.Programmatic);
    }

    private void OnGateChanged(object sender, SelectionChangedEventArgs e)
    {
        Validate();
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
        var oneLine = _choosesGate || !ArgumentBox.Text.Contains('\n', StringComparison.Ordinal);
        IsPrimaryButtonEnabled = !missing && BudgetUsd > 0 && oneLine;
    }
}
