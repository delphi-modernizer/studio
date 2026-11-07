using System;
using System.Globalization;
using System.Linq;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;

/// <summary>
/// One role in the master list: its name, how many of its deliverables are started and whether it runs.
/// </summary>
public sealed record RoleRow(Workstream Workstream, int Started, bool IsRunning)
{
    public string Code => Workstream.Role.Code;

    public string Name => Workstream.Role.Name;

    public string Glyph => RoleGlyphs.Of(Code);

    public string Progress => $"{Started} of {Workstream.Deliverables.Count} started";

    public double Percent => Workstream.Deliverables.Count == 0 ? 0 : 100.0 * Started / Workstream.Deliverables.Count;

    public string AnnouncedName => IsRunning
        ? $"{Name}, {Started} of {Workstream.Deliverables.Count} deliverables started, run in progress"
        : $"{Name}, {Started} of {Workstream.Deliverables.Count} deliverables started";

    /// <summary>The role list is a ListView, which names an item's container from the item's text.</summary>
    public override string ToString()
    {
        return AnnouncedName;
    }
}

public sealed record DeliverableRow(string Title, PillState Pill);

/// <summary>One agent task: what it asks for and writes, and whether another run holds what it writes.</summary>
/// <param name="Blocker">The run in progress that writes the same deliverable, or null.</param>
public sealed record TaskRow(Recipe Recipe, string RoleName, bool ClaudeReady, LiveRun? Blocker, string BlockedReason)
{
    public string Title => Recipe.Title;

    public string ArgumentLine => RunWords.ArgumentLine(Recipe.Argument);

    public string WritesLine => $"Writes the {RunWords.Writes(Recipe)}";

    public bool IsBlocked => Blocker is not null;

    public bool CanRun => ClaudeReady && !IsBlocked;

    public string AnnouncedName => RunNames.Task(Title, RunWords.Writes(Recipe), IsBlocked ? BlockedReason : null);
}

/// <summary>One run in a role's history, in progress or recorded.</summary>
public sealed record HistoryRow(RunRecord Run)
{
    public string RunId => Run.RunId;

    public string Title => Run.Recipe?.Title ?? Run.Agent;

    public string ArgumentText => RunWords.ArgumentText(Run.Recipe, Run.Argument);

    public string When => Run.FinishedAt is { } finished
        ? $"{RunWords.Time(Run.StartedAt)} – {RunWords.Time(finished)}"
        : $"{RunWords.Time(Run.StartedAt)} · running";

    public PillState Pill => RunWords.PillOf(Run.Outcome);

    public string Cost => Run.CostUsd is { } cost ? RunWords.Dollars(cost) : "—";

    public string Turns => Run.Turns?.ToString(CultureInfo.CurrentCulture) ?? "—";

    public string AnnouncedName => $"{Title}, {ArgumentText}, {PillLook.Of(Pill).Word}, {When}";

    /// <summary>Run history is a ListView, which names an item's container from the item's text.</summary>
    public override string ToString()
    {
        return AnnouncedName;
    }
}

/// <summary>The words the Workstreams and Run pages put around recipes, runs and money.</summary>
public static class RunWords
{
    /// <summary>The kit's name for an argument that picks a sign-off, which people see by name, not code.</summary>
    private const string _gateArgument = "gate";

    public static PillState PillOf(RunOutcome outcome)
    {
        return outcome switch {
            RunOutcome.Running => PillState.Running,
            RunOutcome.Completed => PillState.Completed,
            RunOutcome.Stopped => PillState.Stopped,
            RunOutcome.BudgetReached => PillState.BudgetReached,
            _ => PillState.Failed,
        };
    }

    /// <summary>"Scope, required: a unit, form, module or folder under legacy/".</summary>
    public static string ArgumentLine(RecipeArgument argument)
    {
        var need = argument.Required ? "required" : "optional";

        return argument.Name == _gateArgument
            ? $"Sign-off, {need}: the sign-off the Principal Architect prepares"
            : $"{Capitalized(argument.Name)}, {need}: {Sentence(argument.Description)}";
    }

    /// <summary>"scope legacy/Sales", or "no focus" for an optional argument left empty.</summary>
    public static string ArgumentText(Recipe? recipe, string? argument)
    {
        if (recipe is null)
        {
            return argument ?? string.Empty;
        }

        if (recipe.Argument.Name == _gateArgument)
        {
            return argument is null ? "no sign-off" : $"sign-off {SignOffNames.Of(argument)}";
        }

        return argument is null ? $"no {recipe.Argument.Name}" : $"{recipe.Argument.Name} {argument}";
    }

    /// <summary>The titles of the deliverables a recipe writes, as the kit names them.</summary>
    public static string Writes(Recipe recipe)
    {
        var deliverables = KitBundle.Embedded.Spec.Deliverables.Deliverables;
        var titles = recipe.Writes.Select(id => deliverables.FirstOrDefault(d => d.Id == id)?.Title ?? id);

        return string.Join(" and the ", titles);
    }

    public static string Dollars(decimal amount)
    {
        return amount.ToString("$0.00", CultureInfo.InvariantCulture);
    }

    public static string Time(DateTimeOffset at)
    {
        return at.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
    }

    public static string Duration(TimeSpan span)
    {
        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public static string Capitalized(string word)
    {
        return word.Length == 0 ? word : string.Concat(word[..1].ToUpperInvariant(), word[1..]);
    }

    private static string Sentence(string description)
    {
        var text = description.TrimEnd('.');

        return text.Length > 1 && char.IsLower(text[1]) ? string.Concat(text[..1].ToLowerInvariant(), text[1..]) : text;
    }
}
