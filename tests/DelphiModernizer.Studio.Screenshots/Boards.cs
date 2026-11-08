using System.Collections.Frozen;
using System.Collections.Generic;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>The canvas artboard each capture is paired with, and its label as the canvas names it.</summary>
/// <remarks>
/// Labels are the board titles in the canvas's project/canvas.json. A page's default state pairs with its light, dark,
/// minimum-window and high-contrast boards; any other state with the one board that draws it.
/// </remarks>
internal static class Boards
{
    /// <summary>Each page's default state and the stem of its first-row boards.</summary>
    private static readonly FrozenDictionary<string, (string State, string Stem)> _defaults =
        new Dictionary<string, (string, string)> {
            ["start"] = ("default", "Start"),
            ["newworkspace"] = ("step2", "Wizard"),
            ["overview"] = ("default", "Overview"),
            ["inventory"] = ("code-modules", "Inventory"),
            ["workstreams"] = ("default", "Workstreams"),
            ["run"] = ("running", "Run"),
            ["deliverables"] = ("all", "Deliverables"),
            ["traceability"] = ("issues", "Traceability"),
            ["signoffs"] = ("blocked", "Gates"),
            ["settings"] = ("found", "Settings"),
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<(string Page, string State), string> _states =
        new Dictionary<(string, string), string> {
            [("start", "first-run")] = "StartFirstRun",
            [("start", "claude-missing")] = "StartClaudeMissing",
            [("start", "claude-too-old")] = "StartClaudeOld",
            [("start", "claude-missing-dialog")] = "ClaudeDialog",
            [("newworkspace", "step1")] = "WizardStep1",
            [("newworkspace", "folder-not-empty")] = "WizardFolderNotEmpty",
            [("newworkspace", "preview-running")] = "WizardPreviewRunning",
            [("newworkspace", "preview-failed")] = "WizardPreviewFailed",
            [("newworkspace", "step3")] = "WizardStep3",
            [("newworkspace", "step4")] = "WizardStep4",
            [("newworkspace", "step5")] = "WizardStep5",
            [("newworkspace", "creating")] = "WizardCreating",
            [("newworkspace", "created")] = "WizardCreated",
            [("overview", "running")] = "OverviewRunning",
            [("overview", "no-scan")] = "OverviewNoScan",
            [("overview", "no-claude")] = "OverviewNoClaude",
            [("overview", "hover")] = "OverviewHover",
            [("overview", "focus")] = "OverviewHover",
            [("overview", "no-workspace")] = "OverviewNoWorkspace",
            [("overview", "loading")] = "OverviewLoading",
            [("overview", "pane-compact")] = "OverviewCompact",
            [("overview", "scrolled")] = "OverviewMinScrolled",
            [("shared", "title-bar")] = "TitleBar",
            [("shared", "nav-pane")] = "NavPane",
            [("inventory", "screens")] = "InventoryForms",
            [("inventory", "data-and-libraries")] = "InventoryData",
            [("inventory", "sql-and-risks")] = "InventoryRisks",
            [("inventory", "large")] = "InventoryLarge",
            [("inventory", "not-scanned")] = "InventoryNotScanned",
            [("inventory", "scanning")] = "InventoryScanning",
            [("inventory", "no-workspace")] = "InventoryNoWorkspace",
            [("workstreams", "history")] = "WorkstreamsHistory",
            [("workstreams", "blocked")] = "WorkstreamsBlocked",
            [("workstreams", "run-dialog")] = "WorkstreamsRunDialog",
            [("workstreams", "no-claude")] = "WorkstreamsNoClaude",
            [("workstreams", "claude-dialog")] = "WorkstreamsClaudeDialog",
            [("workstreams", "claude-update-dialog")] = "ClaudeDialog",
            [("run", "starting")] = "RunStarting",
            [("run", "completed")] = "RunCompleted",
            [("run", "failed")] = "RunFailed",
            [("run", "stopped")] = "RunStopped",
            [("run", "budget-dialog")] = "RunBudget",
            [("deliverables", "templates")] = "DeliverablesTemplates",
            [("deliverables", "missing")] = "DeliverablesMissing",
            [("deliverables", "draft")] = "DeliverablesFiltered",
            [("traceability", "errors-only")] = "TraceabilityErrorsOnly",
            [("traceability", "strict")] = "TraceabilityStrict",
            [("traceability", "clean")] = "TraceabilityClean",
            [("traceability", "matrix")] = "TraceabilityMatrix",
            [("signoffs", "approve-dialog")] = "GatesApprove",
            [("signoffs", "no-identity")] = "GatesNoIdentity",
            [("settings", "not-found")] = "SettingsClaudeMissing",
            [("settings", "script-shim")] = "SettingsInvalidPath",
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> _labels = new Dictionary<string, string> {
        ["ClaudeDialog"] = "Dialog · Claude Code not installed or too old",
        ["Deliverables"] = "Deliverables · Light · 1280 × 800",
        ["DeliverablesDark"] = "Deliverables · Dark · 1280 × 800",
        ["DeliverablesFiltered"] = "Deliverables · Filtered to Draft",
        ["DeliverablesMin"] = "Deliverables · Light · 960 × 576 (minimum)",
        ["DeliverablesMinDark"] = "Deliverables · Dark · 960 × 576 (minimum)",
        ["DeliverablesMissing"] = "Deliverables · A missing file · Dark",
        ["DeliverablesTemplates"] = "Deliverables · All templates (new workspace)",
        ["Gates"] = "Sign-offs · Light · 1280 × 800 · Discovery blocked",
        ["GatesApprove"] = "Sign-offs · Approve dialog",
        ["GatesDark"] = "Sign-offs · Dark · 1280 × 800",
        ["GatesHC"] = "Sign-offs · High contrast (Aquatic) · Discovery approved",
        ["GatesMin"] = "Sign-offs · Light · 960 × 576 (minimum)",
        ["GatesMinDark"] = "Sign-offs · Dark · 960 × 576 (minimum)",
        ["GatesNoIdentity"] = "Sign-offs · Approve without a git identity",
        ["Inventory"] = "Inventory · Light · 1280 × 800",
        ["InventoryDark"] = "Inventory · Dark · 1280 × 800",
        ["InventoryData"] = "Inventory · Data and libraries, redacted connection · Dark",
        ["InventoryForms"] = "Inventory · Forms, a binary form that can’t be read",
        ["InventoryLarge"] = "Inventory · Thousands of units",
        ["InventoryMin"] = "Inventory · Light · 960 × 576 (minimum)",
        ["InventoryMinDark"] = "Inventory · Dark · 960 × 576 (minimum)",
        ["InventoryNoWorkspace"] = "Inventory · No workspace open",
        ["InventoryNotScanned"] = "Inventory · Not scanned",
        ["InventoryRisks"] = "Inventory · Risk markers and SQL",
        ["InventoryScanning"] = "Inventory · Scanning · Dark",
        ["Main"] = "Overview · Light · 1280 × 800",
        ["NavPane"] = "Part · NavigationView pane",
        ["OverviewCompact"] = "Overview · pane collapsed to compact (title-bar toggle)",
        ["OverviewDark"] = "Overview · Dark · 1280 × 800",
        ["OverviewHC"] = "Overview · High contrast (Aquatic)",
        ["OverviewHover"] =
            "Overview · Diagram parts are buttons: hover shows a tooltip, keyboard focus shows the focus ring",
        ["OverviewLoading"] = "Overview · Loading · Dark",
        ["OverviewMin"] = "Overview · Light · 960 × 576 (minimum)",
        ["OverviewMinDark"] = "Overview · Dark · 960 × 576 (minimum)",
        ["OverviewMinScrolled"] = "Overview · Light · 960 × 576, scrolled down to the diagram",
        ["OverviewNoClaude"] = "Overview · Claude Code not set up",
        ["OverviewNoScan"] = "Overview · No scan yet (new workspace)",
        ["OverviewNoWorkspace"] = "Overview · No workspace open",
        ["OverviewRunning"] = "Overview · A run in progress",
        ["Run"] = "Run · Light · 1280 × 800",
        ["RunBudget"] = "Run · Budget reached dialog",
        ["RunCompleted"] = "Run · Completed · Dark",
        ["RunDark"] = "Run · Dark · 1280 × 800",
        ["RunFailed"] = "Run · Failed",
        ["RunMin"] = "Run · Light · 960 × 576 (minimum)",
        ["RunMinDark"] = "Run · Dark · 960 × 576 (minimum)",
        ["RunStarting"] = "Run · Starting",
        ["RunStopped"] = "Run · Stopped by the person",
        ["Settings"] = "Settings · Light · 1280 × 800",
        ["SettingsClaudeMissing"] = "Settings · Claude Code missing",
        ["SettingsDark"] = "Settings · Dark · 1280 × 800",
        ["SettingsInvalidPath"] = "Settings · Invalid path · Dark",
        ["SettingsMin"] = "Settings · Light · 960 × 576 (minimum)",
        ["SettingsMinDark"] = "Settings · Dark · 960 × 576 (minimum)",
        ["Start"] = "Start · Light · 1280 × 800",
        ["StartClaudeMissing"] = "Start · Claude Code missing",
        ["StartClaudeOld"] = "Start · Claude Code older than 2.1.259 · Dark",
        ["StartDark"] = "Start · Dark · 1280 × 800",
        ["StartFirstRun"] = "Start · First run, no recent workspaces",
        ["StartMin"] = "Start · Light · 960 × 576 (minimum)",
        ["StartMinDark"] = "Start · Dark · 960 × 576 (minimum)",
        ["TitleBar"] = "Part · TitleBar",
        ["Traceability"] = "Traceability · Light · 1280 × 800",
        ["TraceabilityClean"] = "Traceability · Clean",
        ["TraceabilityDark"] = "Traceability · Dark · 1280 × 800",
        ["TraceabilityErrorsOnly"] = "Traceability · Errors only",
        ["TraceabilityMatrix"] = "Traceability · Matrix",
        ["TraceabilityMin"] = "Traceability · Light · 960 × 576 (minimum)",
        ["TraceabilityMinDark"] = "Traceability · Dark · 960 × 576 (minimum)",
        ["TraceabilityStrict"] = "Traceability · Strict, warnings count as errors · Dark",
        ["Wizard"] = "New workspace · Light · 1280 × 800",
        ["WizardCreated"] = "New workspace · Created",
        ["WizardCreating"] = "New workspace · Creating, per-step progress",
        ["WizardDark"] = "New workspace · Dark · 1280 × 800",
        ["WizardFolderNotEmpty"] = "New workspace · Folder not empty",
        ["WizardMin"] = "New workspace · Light · 960 × 576 (minimum)",
        ["WizardMinDark"] = "New workspace · Dark · 960 × 576 (minimum)",
        ["WizardPreviewFailed"] = "New workspace · 2 Preview failed",
        ["WizardPreviewRunning"] = "New workspace · 2 Preview running · Dark",
        ["WizardStep1"] = "New workspace · 1 Name and folder",
        ["WizardStep3"] = "New workspace · 3 Target platform",
        ["WizardStep4"] = "New workspace · 4 Context",
        ["WizardStep5"] = "New workspace · 5 Create · Dark",
        ["Workstreams"] = "Workstreams · Light · 1280 × 800",
        ["WorkstreamsBlocked"] = "Workstreams · Recipe blocked by another run · Dark",
        ["WorkstreamsClaudeDialog"] = "Workstreams · Claude Code not installed dialog · Dark",
        ["WorkstreamsDark"] = "Workstreams · Dark · 1280 × 800",
        ["WorkstreamsHistory"] = "Workstreams · Run history",
        ["WorkstreamsMin"] = "Workstreams · Light · 960 × 576 (minimum)",
        ["WorkstreamsMinDark"] = "Workstreams · Dark · 960 × 576 (minimum)",
        ["WorkstreamsNoClaude"] = "Workstreams · Claude Code unavailable",
        ["WorkstreamsRunDialog"] = "Workstreams · Run an agent task (scope and budget)",
    }.ToFrozenDictionary();

    /// <returns>The board's path in the canvas artifact and its label; nulls for a state no artboard draws.</returns>
    public static (string? Board, string? Label) For(string page, string state, string theme, int width)
    {
        string? stem = null;

        if (_states.TryGetValue((page, state), out var board))
        {
            stem = board;
        }
        else if (_defaults.TryGetValue(page, out var first) && first.State == state)
        {
            stem = First(first.Stem, theme, width);
        }

        return stem is not null && _labels.TryGetValue(stem, out var label)
            ? ($"project/{stem}.dc.html", label)
            : (null, null);
    }

    /// <remarks>Overview's light board is Main; the other first-row boards follow the stem.</remarks>
    private static string First(string stem, string theme, int width)
    {
        var suffix = (theme, width < 1100) switch {
            ("highcontrast", _) => "HC",
            ("dark", true) => "MinDark",
            ("dark", false) => "Dark",
            (_, true) => "Min",
            _ => string.Empty,
        };

        return stem == "Overview" && suffix.Length == 0 ? "Main" : stem + suffix;
    }
}
