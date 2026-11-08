using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.ClaudeStandIn;

/// <summary>The Run board's agent run, written as Claude Code's stream-json lines on standard output.</summary>
/// <remarks>
/// Reads and greps return the workspace's files, and every edit asks the engine's guard first, as the workspace's
/// PreToolUse hook does; an edit it allows is made.
/// </remarks>
internal sealed class StandInRun(
    TextWriter output,
    string workspaceRoot,
    string sessionId,
    Version version,
    decimal budgetUsd,
    string deliverableFile)
{
    private const string _read = "legacy/Sales/Pricing.pas";
    private const string _grepPath = "legacy/Sales";
    private const string _grepPattern = "CREDIT_LIMIT";

    /// <summary>The command the kit's .claude/settings.json gives the hook; Claude Code names it on refusal.</summary>
    private const string _hookCommand = "dmz guard pre-tool-use || exit 2";

    /// <summary>The duration the result reports: a scripted run's length, not the milliseconds it takes.</summary>
    private const long _durationMs = 252_000;

    /// <summary>The harness stops a waiting run within minutes; a stand-in left behind ends on its own.</summary>
    private static readonly TimeSpan _longestWait = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan _beforeNote = TimeSpan.FromMilliseconds(500);

    private readonly JsonArray _denials = [];
    private int _turns;
    private int _toolIds;

    public int Completed()
    {
        Session();
        Work();
        Result("success", isError: false, "I recorded the business rules in the functional requirements as a Draft.",
            Math.Round(budgetUsd * 0.38m, 2));

        return 0;
    }

    /// <remarks>Claude Code ends this way when its API call fails: a message on standard error, no result.</remarks>
    public int Failed()
    {
        Session();
        Work();
        Console.Error.WriteLine("API Error: the stand-in ends this run without a result, as the failed scenario asks.");

        return 1;
    }

    public int WaitsToBeStopped()
    {
        Session();
        Work();
        Thread.Sleep(_longestWait);

        return 1;
    }

    public int ReachesBudget()
    {
        Session();
        Work();
        Result("error_max_budget_usd", isError: true, null, budgetUsd);

        return 1;
    }

    public static int WaitsBeforeTheSession()
    {
        Thread.Sleep(_longestWait);

        return 1;
    }

    private void Session()
    {
        Write(new JsonObject {
            ["type"] = "system",
            ["subtype"] = "init",
            ["session_id"] = sessionId,
            ["cwd"] = workspaceRoot,
            ["model"] = "stand-in",
            ["claude_code_version"] = version.ToString(),
            ["tools"] = new JsonArray("Read", "Grep", "Edit"),
        });
    }

    private void Work()
    {
        Say("I’ll read the Sales units first, then record each business rule in the functional requirements with "
            + "its evidence and a fact label.");
        string source = Path.Combine(workspaceRoot, _read);
        Tool("Read", new JsonObject { ["file_path"] = _read }, () => Numbered(File.ReadAllLines(source)));
        Tool(
            "Grep",
            new JsonObject { ["pattern"] = _grepPattern, ["path"] = _grepPath },
            () => Grep(Path.Combine(workspaceRoot, _grepPath)));
        Say(string.Join(
            '\n',
            "**[Confirmed]** BR-001 Gold customer discount: 7.5 % at a credit limit of 10,000 or more, else 2.5 %, "
                + "rounded half to even. legacy/Sales/Pricing.pas#L18-L29",
            "**[Confirmed]** The customer search builds its SQL by concatenating the typed name. "
                + "legacy/Sales/CustomerForm.pas#L35",
            "**[Inferred]** Gold status seems to follow from the credit limit alone, not from a customer flag.",
            "**[Unknown]** Whether the discount also applies to invoices printed with QuickReport."));
        Edit(_read, "  GoldThreshold = 10000;", "  GoldThreshold = 10000; { BR-001 }");
        string status = File
            .ReadLines(Path.Combine(workspaceRoot, deliverableFile))
            .First(l => l.StartsWith("status:", StringComparison.Ordinal));
        Edit(deliverableFile, status, "status: draft");

        // The runner reads standard error apart from the events, so the note waits until they are read and comes last.
        Thread.Sleep(_beforeNote);
        Console.Error.WriteLine("stand-in: the screenshot harness's Claude Code; no model was called.");
    }

    private void Say(string text)
    {
        _turns++;
        Assistant(new JsonObject { ["type"] = "text", ["text"] = text });
    }

    private void Tool(string name, JsonObject input, Func<string> run)
    {
        string id = Assistant(name, input);
        string content;
        bool isError = false;

        try
        {
            content = run();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            (content, isError) = (exception.Message, true);
        }

        ToolResult(id, content, isError);
    }

    /// <summary>Asks the guard as the hook does, then edits or reports the refusal in Claude Code's words.</summary>
    /// <remarks>An edit that changes nothing is skipped, so a second run in one workspace still succeeds.</remarks>
    private void Edit(string path, string oldText, string newText)
    {
        var input = new JsonObject { ["file_path"] = path, ["old_string"] = oldText, ["new_string"] = newText };
        var decision = Guard.Evaluate(new JsonObject {
            ["session_id"] = sessionId,
            ["cwd"] = workspaceRoot,
            ["hook_event_name"] = "PreToolUse",
            ["tool_name"] = "Edit",
            ["tool_input"] = input.DeepClone(),
        }.ToJsonString());

        if (!decision.Allowed)
        {
            string id = Assistant("Edit", input);
            _denials.Add(new JsonObject {
                ["tool_name"] = "Edit",
                ["tool_use_id"] = id,
                ["tool_input"] = input.DeepClone(),
            });
            ToolResult(id, $"PreToolUse:Edit hook error: [{_hookCommand}]: {decision.Reason}", isError: true);

            return;
        }

        if (oldText == newText)
        {
            return;
        }

        Tool("Edit", input, () => {
            string file = Path.Combine(workspaceRoot, path);
            string text = File.ReadAllText(file);
            File.WriteAllText(file, text.Replace(oldText, newText, StringComparison.Ordinal));

            return $"The file {path} has been updated.";
        });
    }

    private string Assistant(string name, JsonObject input)
    {
        _turns++;
        string id = $"toolu_stand_in_{++_toolIds:D2}";
        Assistant(new JsonObject { ["type"] = "tool_use", ["id"] = id, ["name"] = name, ["input"] = input });

        return id;
    }

    private void Assistant(JsonObject block)
    {
        Write(new JsonObject {
            ["type"] = "assistant",
            ["parent_tool_use_id"] = null,
            ["session_id"] = sessionId,
            ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = new JsonArray(block) },
        });
    }

    private void ToolResult(string id, string content, bool isError)
    {
        var block = new JsonObject {
            ["type"] = "tool_result",
            ["tool_use_id"] = id,
            ["content"] = content,
            ["is_error"] = isError,
        };
        Write(new JsonObject {
            ["type"] = "user",
            ["parent_tool_use_id"] = null,
            ["session_id"] = sessionId,
            ["message"] = new JsonObject { ["role"] = "user", ["content"] = new JsonArray(block) },
        });
    }

    private void Result(string subtype, bool isError, string? text, decimal costUsd)
    {
        Write(new JsonObject {
            ["type"] = "result",
            ["subtype"] = subtype,
            ["is_error"] = isError,
            ["result"] = text,
            ["session_id"] = sessionId,
            ["duration_ms"] = _durationMs,
            ["num_turns"] = _turns,
            ["total_cost_usd"] = costUsd,
            ["permission_denials"] = _denials.DeepClone(),
        });
    }

    private void Write(JsonObject line)
    {
        output.WriteLine(line.ToJsonString());
        output.Flush();
    }

    private static string Numbered(string[] lines)
    {
        return string.Join('\n', lines.Select((l, i) => $"{(i + 1).ToString(CultureInfo.InvariantCulture),6}\t{l}"));
    }

    /// <summary>Each matching line as <c>path:line:text</c>, the paths relative to the workspace.</summary>
    private string Grep(string folder)
    {
        var matches = Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .SelectMany(file => File
                .ReadLines(file)
                .Select((text, index) => (file, line: index + 1, text))
                .Where(m => m.text.Contains(_grepPattern, StringComparison.Ordinal)))
            .Select(m => $"{Relative(m.file)}:{m.line}:{m.text.Trim()}")
            .ToList();

        return matches.Count == 0 ? "No matches found" : string.Join('\n', matches);
    }

    private string Relative(string file)
    {
        return Path
            .GetRelativePath(workspaceRoot, file)
            .Replace('\\', '/');
    }
}
