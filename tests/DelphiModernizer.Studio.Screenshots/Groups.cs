using System;
using System.IO;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>Every state of the handoff's Screens table, reached through the app, dmz or a workspace's files.</summary>
/// <remarks>
/// No step approves or revokes: an approval dialog is opened and cancelled, and the record is checked unchanged.
/// The states that need an approval are listed in docs/design/ui-screenshots.md as not captured.
/// </remarks>
internal static class Groups
{
    public const string Sample = "sample";
    public const string Clean = "clean";
    public const string Unscanned = "unscanned";
    public const string Large = "large";

    private const string _recent = "Recent workspaces";
    private const string _approvals = ".modernizer/approvals.json";

    private static readonly TimeSpan _run = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan _brief = TimeSpan.FromSeconds(30);

    /// <summary>Creating from the large tree copies 10,000 files and commits them.</summary>
    private static readonly TimeSpan _create = TimeSpan.FromMinutes(5);

    /// <summary>Step 4 as the canvas fills it: every answer but Constraints, which is left Unknown.</summary>
    private static readonly (string Field, string Answer)[] _context = [
        ("Domain", "ERP, a Delphi 7 application on the BDE"),
        ("Objectives", "Move to C#/.NET with the same business rules"),
        ("Users", "40 sales and accounting staff"),
        ("Geography", "One office in Lyon, France"),
        ("Criticality", "Invoicing must not stop for more than a day"),
    ];

    /// <summary>Start, New workspace and Settings.</summary>
    /// <remarks>
    /// First run is a launch of its own, so that Back has nowhere to go, as drawn. Settings is drawn with a workspace
    /// open and the wizard without one.
    /// </remarks>
    public static void Start(Harness h, string theme)
    {
        h.Run(
            theme,
            new Launch([]),
            app => h.Step(app, theme, "start first run", () => app.Capture("start", "first-run")));

        h.Run(theme, new Launch([Sample]), app => {
            h.Step(app, theme, "start", () => app.Capture("start", "default"));
            h.Step(app, theme, "settings", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Navigate("Settings", 5000);
                app.Capture("settings", "found");
            });
        });

        foreach (var size in StudioDriver.Sizes)
        {
            h.Run(
                theme,
                new Launch([Sample]),
                app => h.Step(app, theme, $"new workspace {size.Width}", () => Wizard(h, app, size)));
        }

        h.Run(theme, new Launch([Sample], Claude: false), app => {
            h.Step(app, theme, "start claude missing", () => {
                app.Capture("start", "claude-missing");
                app.Invoke("Set up Claude Code");
                app.WaitFor("Claude Code isn’t installed", _brief);
                app.Capture("start", "claude-missing-dialog");
                app.Invoke("Not now");
            });
            h.Step(app, theme, "settings not found", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Navigate("Settings", 5000);
                app.Capture("settings", "not-found");
            });
            h.Step(app, theme, "settings script shim", () => {
                app.Type("Claude Code executable", h.Workspaces.Shim);
                app.Invoke("Test", 4000);
                app.Capture("settings", "script-shim");
            });
        });

        h.Run(theme, new Launch([Sample], TooOld: true), app => {
            h.Step(app, theme, "start claude too old", () => app.Capture("start", "claude-too-old"));
            h.Step(app, theme, "claude update dialog", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Navigate("Workstreams", 4000);
                app.Invoke("Set up in Settings");
                app.WaitFor("Claude Code needs an update", _brief);
                app.Capture("workstreams", "claude-update-dialog");
                app.Invoke("Not now");
            });
        });
    }

    /// <summary>Overview, Sign-offs and the shared title bar and pane.</summary>
    public static void Overview(Harness h, string theme)
    {
        h.Run(theme, new Launch([Sample, Unscanned], Scenario: "stopped"), app => {
            h.Step(app, theme, "overview no workspace", () => {
                app.Navigate("Overview");
                app.Capture("overview", "no-workspace");
            });
            h.Step(app, theme, "overview no scan", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Unscanned]);
                app.Capture("overview", "no-scan");
            });
            h.Step(app, theme, "overview", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Capture("overview", "default");
                // WinUI opens a pointer's tool tip centred on it and just under it. The canvas draws it 236 wide, 12 in
                // from the 52 tall card's left and 6 below it, so the pointer rests at 12 + 118 across, 38 down.
                app.Capture(
                    "overview",
                    "hover",
                    () => app.Hover("Team0", new System.Drawing.Point(12 + (236 / 2), 38)));
                app.Capture("overview", "scrolled", () => app.ScrollTo("Team0", 250), [StudioDriver.Minimum]);
            });
            h.Step(app, theme, "shared", () => {
                app.CapturePart("shared", "title-bar", pane: false);
                app.CapturePart("shared", "nav-pane", pane: true);
                app.TogglePane();
                app.Capture("overview", "pane-compact", () => app.ScrollTo("Team0", 0));
                app.TogglePane();
            });
            h.Step(app, theme, "sign-offs", () => {
                app.Navigate("SignOffs");
                app.Expand("Discovery");
                app.Capture("signoffs", "blocked");
            });
            h.Step(
                app,
                theme,
                "approve dialog",
                () => ApproveDialog(h, app, "Approve the Project setup sign-off?", "approve-dialog"));

            // A keyboard focus ring stays on whatever holds focus, so this comes after the Overview, title bar, pane and
            // Sign-offs states, which the canvas draws without one. The workspace is opened again, so Back is off.
            h.Step(app, theme, "overview focus", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Capture("overview", "focus", () => {
                    app.ScrollTo("Team0", 0);
                    app.FocusWithKeyboard("Gate1");
                });
            });
            h.Step(app, theme, "overview running", () => {
                StartRun(app);
                app.WaitFor("Working…", _run);
                app.Navigate("Overview", 4000);
                app.Capture("overview", "running");
            });
        });

        h.Run(theme, new Launch([Sample], Claude: false, Identity: false), app => {
            h.Step(app, theme, "overview no claude", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Capture("overview", "no-claude");
            });
            h.Step(app, theme, "no git identity", () => {
                app.Navigate("SignOffs");
                ApproveDialog(h, app, "No git identity.", "no-identity");
            });
        });
    }

    /// <summary>Overview and Sign-offs as Windows' high contrast draws them.</summary>
    public static void HighContrast(Harness h, string theme)
    {
        h.Run(theme, new Launch([Sample]), app => {
            h.Step(app, theme, "overview", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Capture("overview", "default");
            });
            h.Step(app, theme, "sign-offs", () => {
                app.Navigate("SignOffs");
                app.Expand("Discovery");
                app.Capture("signoffs", "blocked");
            });
        });
    }

    /// <summary>Inventory, Deliverables, Traceability and Overview's loading state.</summary>
    public static void Pages(Harness h, string theme)
    {
        h.Run(theme, new Launch([Sample, Clean, Unscanned, Large]), app => {
            h.Step(app, theme, "inventory no workspace", () => {
                app.Navigate("Inventory");
                app.Capture("inventory", "no-workspace");
            });
            h.Step(app, theme, "inventory", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Navigate("Inventory");
                app.ForEachSection("Inventory sections", s => app.Capture("inventory", StudioDriver.Slug(s)));
            });
            h.Step(app, theme, "traceability", () => {
                app.Navigate("Traceability", 4000);
                app.Capture("traceability", "issues");
                app.Select("Errors");
                app.Capture("traceability", "errors-only");
                app.Select("All");
                app.Toggle("Strict: warnings count as errors");
                app.Capture("traceability", "strict");
                app.Toggle("Strict: warnings count as errors");
                app.Select("Matrix");
                app.Capture("traceability", "matrix");
            });
            h.Step(app, theme, "deliverables", () => Deliverables(h, app));
            h.Step(app, theme, "traceability clean", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Clean]);
                app.Navigate("Traceability", 4000);
                app.WaitFor("No errors or warnings", _brief);
                app.Capture("traceability", "clean");
            });
            h.Step(app, theme, "inventory not scanned", () => {
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Unscanned]);
                app.Navigate("Inventory");
                app.Capture("inventory", "not-scanned");
            });
            // The canvas draws a first scan, so each size scans the large tree with no scan before it.
            h.Step(app, theme, "inventory large", () => {
                string scans = Path.Combine(h.Opened[Large], ".modernizer", "scans");
                app.Navigate("Start");
                app.ClickItem(_recent, h.Opened[Large]);
                app.CaptureWhileBusy(
                    "inventory",
                    "scanning",
                    () => {
                        app.WaitGone("Scanning legacy/…", _run);

                        if (Directory.Exists(scans))
                        {
                            Directory.Delete(scans, recursive: true);
                        }

                        app.Navigate("Overview");
                        app.Navigate("Inventory", 4000);
                        app.Invoke("Scan", 0);
                    },
                    c => c.ByControlType(FlaUI.Core.Definitions.ControlType.ProgressBar));
                app.WaitGone("Scanning legacy/…", _run);
                app.Capture("inventory", "large");
            });
        });

        // The copy scanned above, whose inventory makes the loading last.
        var scanned = new Launch([Large], Reuse: true);

        // Opened from Start right after a launch, as the canvas draws it: Back is off before the click and after it.
        // A Back that turns off as the brief state shows can still be drawn on when the screen is taken.
        foreach (var size in StudioDriver.Sizes)
        {
            h.Run(theme, scanned, app => h.Step(app, theme, $"overview loading {size.Width}", () => {
                bool again = false;
                app.CaptureWhileBusy(
                    "overview",
                    "loading",
                    () => {
                        // A second try starts on Overview, and Start from the pane turns Back on until the click.
                        if (again)
                        {
                            app.Navigate("Start");
                        }

                        again = true;
                        app.ClickItem(_recent, h.Opened[Large], settle: 0);
                        app.WaitBackOff();
                    },
                    c => c.ByName("Checking sign-offs, deliverables and traceability"),
                    [size]);
            }));
        }
    }

    /// <summary>Workstreams and the Run page, each run played by the stand-in Claude Code.</summary>
    public static void Agents(Harness h, string theme)
    {
        h.Run(theme, new Launch([Sample], Scenario: "stopped"), app => {
            h.Step(app, theme, "workstreams", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                app.Navigate("Workstreams", 4000);
                app.Select("Legacy discovery");
                app.Capture("workstreams", "default");
            });
            h.Step(app, theme, "run dialog and running", () => {
                OpenRunDialog(app);
                app.Capture("workstreams", "run-dialog");
                app.Invoke("Run");
                app.WaitFor("Working…", _run);
                app.Capture("run", "running");
            });
            h.Step(app, theme, "blocked", () => {
                app.Navigate("Workstreams", 4000);
                app.Select("Requirements");
                app.Capture("workstreams", "blocked");
            });
            h.Step(app, theme, "stopped", () => {
                app.Invoke("Open run", 4000);
                Stop(app);
                app.Capture("run", "stopped");
            });
        });

        h.Run(theme, new Launch([Sample], Scenario: "starting"), app => {
            h.Step(app, theme, "starting", () => {
                app.ClickItem(_recent, h.Opened[Sample]);
                StartRun(app);
                app.WaitFor("Starting Claude Code…", _run);
                app.Capture("run", "starting");
                app.Invoke("Stop");
            });
        });

        // The history artboard lists a live run above a completed, a stopped, a budget-reached and a failed one, so
        // these launches share one workspace and run in that order, oldest first.
        var history = new Launch([Sample], Reuse: true);
        h.Run(theme, new Launch([Sample], Scenario: "failed"), app => h.Step(app, theme, "failed", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            StartRun(app);
            app.WaitFor("The run failed.", _run);
            app.Capture("run", "failed");
        }));

        h.Run(theme, history with { Scenario = "budget" }, app => h.Step(app, theme, "budget", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            StartRun(app);
            app.WaitFor("This run reached its budget", _run);
            app.Capture("run", "budget-dialog");
            app.Invoke("Close");
        }));

        h.Run(theme, history with { Scenario = "stopped" }, app => h.Step(app, theme, "stop", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            StartRun(app);
            Stop(app);
        }));

        h.Run(theme, history with { Scenario = "completed" }, app => h.Step(app, theme, "completed", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            StartRun(app);
            app.WaitFor("The agent finished.", _run);
            app.Capture("run", "completed");
        }));

        h.Run(theme, history with { Scenario = "stopped" }, app => h.Step(app, theme, "history", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            StartRun(app);
            app.WaitFor("Working…", _run);
            app.Navigate("Workstreams", 4000);
            app.Select("Legacy discovery");
            app.Capture("workstreams", "history");
            app.Invoke("Open run", 4000);
            Stop(app);
        }));

        h.Run(theme, new Launch([Sample], Claude: false), app => h.Step(app, theme, "no claude", () => {
            app.ClickItem(_recent, h.Opened[Sample]);
            app.Navigate("Workstreams", 4000);
            app.Capture("workstreams", "no-claude");
            app.Invoke("Set up in Settings");
            app.WaitFor("Claude Code isn’t installed", _brief);
            app.Capture("workstreams", "claude-dialog");
            app.Invoke("Not now");
        }));
    }

    /// <summary>The wizard from Start to Created at one size.</summary>
    /// <remarks>
    /// The large tree is the source, so its preview and its copy last long enough to capture. Creating is captured
    /// while the tree is copied, after the kit, so that the list shows a step done, one working and others waiting.
    /// </remarks>
    private static void Wizard(Harness h, StudioDriver app, System.Drawing.Size size)
    {
        var at = new[] { size };
        string folder = h.Workspaces.NewFolder();
        app.Invoke("New workspace");
        app.Type("Workspace name", Workspaces.Name);
        app.Type("Folder", folder);

        // Step 1 comes first: once a folder is refused, the page moves focus to it, which the canvas draws only there.
        app.Capture("newworkspace", "step1", sizes: at);
        app.Type("Folder", h.Workspaces.NotEmpty);
        app.Invoke("Next");
        app.WaitFor("This folder isn’t empty.", _brief);
        app.Capture("newworkspace", "folder-not-empty", sizes: at);
        app.Type("Folder", folder);
        app.Invoke("Next");

        app.Type("Legacy source folder", h.Workspaces.NoDelphi, tabOut: true);
        app.WaitFor("Couldn’t preview this folder.", _brief);
        app.Capture("newworkspace", "preview-failed", sizes: at);
        app.Type("Legacy source folder", h.Workspaces.Legacy, tabOut: true);
        app.WaitEnabled("Next", _run);
        app.Capture("newworkspace", "step2", sizes: at);
        // A preview starts only when the folder changes, so a second try clears it first.
        app.CaptureWhileBusy(
            "newworkspace",
            "preview-running",
            () => {
                app.Type("Legacy source folder", string.Empty, settle: 0);
                app.Type("Legacy source folder", h.Workspaces.LargeLegacy, tabOut: true, settle: 0);
            },
            c => c.ByControlType(FlaUI.Core.Definitions.ControlType.ProgressBar),
            at);
        app.WaitEnabled("Next", _run);
        app.Invoke("Next");

        app.Select("C#/.NET");
        app.Capture("newworkspace", "step3", sizes: at);
        app.Invoke("Next");
        foreach (var (field, answer) in _context)
        {
            app.Type(field, answer);
        }

        app.Toggle("Constraints unknown. Adds an open question.");
        app.Capture("newworkspace", "step4", sizes: at);
        app.Invoke("Next");
        app.Capture("newworkspace", "step5", sizes: at);
        app.Capture(
            "newworkspace",
            "creating",
            () => {
                app.Invoke("Create", 0);
                app.WaitBriefly(c => c.ByName("Done"), "The kit's copy done", _brief);
            },
            at);
        app.WaitFor("Open the Overview", _create);
        app.Capture("newworkspace", "created", sizes: at);
    }

    private static void Deliverables(Harness h, StudioDriver app)
    {
        string root = h.Opened[Sample];
        app.Navigate("Deliverables", 4000);
        app.Capture("deliverables", "all");
        app.Select("Draft");
        app.Capture("deliverables", "draft");
        app.Select("All");

        File.Delete(Path.Combine(root, "docs", "01-discovery", "D02-system-inventory.md"));
        app.Navigate("Overview");
        app.Navigate("Deliverables", 4000);
        app.Capture("deliverables", "missing");
        app.Invoke("Restore from template", 4000);

        // The charter is the one deliverable init drafts; a person sets it back to a template here.
        string charter = Path.Combine(root, "docs", "00-project-charter", "D01-project-charter.md");
        string text = File.ReadAllText(charter);
        File.WriteAllText(charter, text.Replace("status: draft", "status: template", StringComparison.Ordinal));
        app.Navigate("Overview");
        app.Navigate("Deliverables", 4000);
        app.WaitFor("Every deliverable is still a template.", _brief);
        app.Capture("deliverables", "templates");
    }

    /// <summary>Opens an approval dialog, captures it and cancels; the approval record must not change.</summary>
    private static void ApproveDialog(Harness h, StudioDriver app, string shows, string state)
    {
        string record = Path.Combine(h.Opened[Sample], _approvals);
        string? before = File.Exists(record) ? File.ReadAllText(record) : null;
        app.Expand("Project setup");
        app.Click("Approve Project setup");
        app.WaitFor(shows, _brief);
        app.Capture("signoffs", state);
        app.Invoke("Cancel");
        string? after = File.Exists(record) ? File.ReadAllText(record) : null;

        if (before != after)
        {
            throw new InvalidOperationException($"{_approvals} changed with the dialog open; only a person approves.");
        }
    }

    /// <summary>Opens the Run an agent task dialog for the Run board's task, on legacy/Sales.</summary>
    private static void OpenRunDialog(StudioDriver app)
    {
        app.Resize(StudioDriver.Wide);
        app.Navigate("Workstreams", 4000);
        app.Select("Legacy discovery");
        app.ClickBeside("Extract business rules", "Run…");
        app.WaitFor("Run Extract business rules", _brief);
        app.Type("Scope", "legacy/Sales");
    }

    private static void StartRun(StudioDriver app)
    {
        OpenRunDialog(app);
        app.Invoke("Run");
    }

    /// <summary>Stops the run the Run page shows once it is working.</summary>
    private static void Stop(StudioDriver app)
    {
        app.WaitFor("Working…", _run);
        app.Invoke("Stop");
        app.WaitFor("You stopped the run.", _run);
    }
}
