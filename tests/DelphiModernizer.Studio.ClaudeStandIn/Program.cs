using System;
using System.Globalization;
using System.IO;
using System.Linq;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Kit;

namespace DelphiModernizer.Studio.ClaudeStandIn;

/// <summary>Answers <c>--version</c> and plays the scenario <c>STUDIO_CLAUDE_SCENARIO</c> names.</summary>
/// <remarks>The scenarios and variables are in docs/design/ui-screenshots.md; the harness sets them on the app.</remarks>
internal static class Program
{
    public const string ScenarioVariable = "STUDIO_CLAUDE_SCENARIO";
    public const string VersionVariable = "STUDIO_CLAUDE_VERSION";

    private static int Main(string[] args)
    {
        if (args is ["--version"])
        {
            Console.WriteLine($"{Version()} (Claude Code)");

            return 0;
        }

        var prompt = Value(args, "-p");
        var sessionId = Value(args, "--session-id");
        var budget = Value(args, "--max-budget-usd");
        var skill = prompt?.Split(' ', 2)[0].TrimStart('/');
        var recipe = KitBundle.Embedded.Spec.Recipes.Recipes.FirstOrDefault(r => r.Skill == skill);

        if (sessionId is null || budget is null || recipe is null)
        {
            Console.Error.WriteLine("The stand-in takes the arguments AgentRunner passes: -p /<skill> …, --session-id, …");

            return 2;
        }

        var deliverable = KitBundle.Embedded.Spec.Deliverables.Deliverables.First(d => d.Id == recipe.Writes[0]);
        var run = new StandInRun(
            Console.Out,
            Directory.GetCurrentDirectory(),
            sessionId,
            Version(),
            decimal.Parse(budget, CultureInfo.InvariantCulture),
            deliverable.File);

        return Environment.GetEnvironmentVariable(ScenarioVariable) switch {
            null or "" or "completed" => run.Completed(),
            "failed" => run.Failed(),
            "stopped" => run.WaitsToBeStopped(),
            "budget" => run.ReachesBudget(),
            "starting" => StandInRun.WaitsBeforeTheSession(),
            var other => Unknown(other),
        };
    }

    /// <summary>The engine's minimum, or one patch below it for "too old".</summary>
    private static Version Version()
    {
        var minimum = ClaudeExecutable.MinimumVersion;

        return Environment.GetEnvironmentVariable(VersionVariable) == "too-old"
            ? new Version(minimum.Major, minimum.Minor, minimum.Build - 1)
            : minimum;
    }

    private static string? Value(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static int Unknown(string scenario)
    {
        Console.Error.WriteLine($"{ScenarioVariable}={scenario} is not a scenario.");

        return 2;
    }
}
