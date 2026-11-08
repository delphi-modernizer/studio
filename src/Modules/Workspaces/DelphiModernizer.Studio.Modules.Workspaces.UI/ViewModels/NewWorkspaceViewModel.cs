using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

public enum WizardPhase
{
    Editing,
    Creating,
    Created,
}

public enum PreviewState
{
    None,
    Running,
    Done,
    Failed,
}

/// <summary>The five steps of New workspace; nothing is written to disk until Create.</summary>
public sealed partial class NewWorkspaceViewModel : ViewModel
{
    private const int _lastStep = 5;

    private static readonly string[] _titles = [
        "Name and folder", "Legacy source", "Target platform", "Essential context", "Create the workspace",
    ];

    private static readonly string[] _labels = [
        "Name and folder", "Legacy source", "Target platform", "Context", "Create",
    ];

    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IPickerService _picker;
    private CancellationTokenSource? _previewing;
    private string? _commit;

    public NewWorkspaceViewModel(
        ISender sender,
        INavigationService navigation,
        IPickerService picker,
        IStudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentNullException.ThrowIfNull(settings);
        _sender = sender;
        _navigation = navigation;
        _picker = picker;
        Steps = [.. _labels.Select((label, i) => new WizardStep(i + 1, label))];
        SelectedCodePage = CodePages.Common.FirstOrDefault(c => c.Number == settings.DefaultCodePage)
            ?? CodePages.Common[0];
        Steps[0].Show(1);
    }

    public IReadOnlyList<WizardStep> Steps { get; }

    public IReadOnlyList<ContextField> Context { get; } = [
        new("Domain"),
        new("Objectives"),
        new("Users"),
        new("Geography"),
        new("Criticality"),
        new("Constraints"),
    ];

    public ObservableCollection<PreviewFact> Facts { get; } = [];

    public IReadOnlyList<CreationStepRow> Creation { get; } = [
        new(InitStep.Kit, "Copy the kit into the folder"),
        new(InitStep.Legacy, "Copy the legacy tree into legacy/, read-only"),
        new(InitStep.Records, "Write the settings, the decision and the open questions"),
        new(InitStep.Git, "Initialise git"),
        new(InitStep.FirstCommit, "Make the first commit"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepOf), nameof(StepTitle), nameof(StepHeading), nameof(NextLabel))]
    [NotifyPropertyChangedFor(nameof(CanGoBack), nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsStep4), nameof(IsStep5))]
    public partial int Step { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepOf), nameof(StepTitle), nameof(StepHeading))]
    [NotifyPropertyChangedFor(nameof(CanGoBack), nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsStep4), nameof(IsStep5))]
    [NotifyPropertyChangedFor(nameof(IsCreating), nameof(IsCreated), nameof(HasFooter), nameof(CanCancel))]
    public partial WizardPhase Phase { get; set; }

    public string StepOf => $"Step {Math.Min(Step, _lastStep)} of {_lastStep}";

    public string StepTitle => Phase switch {
        WizardPhase.Creating => "Creating the workspace",
        WizardPhase.Created => "Created",
        _ => _titles[Step - 1],
    };

    /// <summary>What Narrator reads for the step heading, such as "Step 2 of 5, Legacy source".</summary>
    public string StepHeading => $"{StepOf}, {StepTitle}";

    public bool IsStep1 => IsEditing(1);

    public bool IsStep2 => IsEditing(2);

    public bool IsStep3 => IsEditing(3);

    public bool IsStep4 => IsEditing(4);

    public bool IsStep5 => IsEditing(5);

    public bool IsCreating => Phase == WizardPhase.Creating;

    public bool IsCreated => Phase == WizardPhase.Created;

    public bool HasFooter => Phase != WizardPhase.Created;

    public bool CanCancel => Phase != WizardPhase.Creating;

    public bool CanGoBack => Phase == WizardPhase.Editing && Step > 1;

    public string NextLabel => Step == _lastStep ? "Create" : "Next";

    public bool CanGoNext => Phase == WizardPhase.Editing && Step switch {
        1 => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Folder) && FolderProblem is null,
        2 => Preview == PreviewState.Done,
        3 => Target is not null && !string.IsNullOrWhiteSpace(DecidedBy),
        _ => true,
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    public partial string Folder { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext), nameof(HasFolderProblem))]
    public partial string? FolderProblem { get; set; }

    public bool HasFolderProblem => FolderProblem is not null;

    [ObservableProperty]
    public partial string LegacySource { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CodePage SelectedCodePage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsPreviewRunning), nameof(IsPreviewDone), nameof(IsPreviewFailed))]
    public partial PreviewState Preview { get; set; }

    public bool IsPreviewRunning => Preview == PreviewState.Running;

    public bool IsPreviewDone => Preview == PreviewState.Done;

    public bool IsPreviewFailed => Preview == PreviewState.Failed;

    [ObservableProperty]
    public partial string? PreviewProblem { get; set; }

    /// <summary>The files the scan has read so far, of all it found; null until it reports its first count.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanText), nameof(ScanPercent))]
    public partial ScanProgress? Scanned { get; set; }

    public string ScanText => Scanned is { } scanned
        ? $"Scanning the folder… {Number(scanned.FilesDone)} of {Number(scanned.FilesTotal)} files"
        : "Scanning the folder…";

    public double ScanPercent => Scanned is { FilesTotal: > 0 } scanned
        ? 100.0 * scanned.FilesDone / scanned.FilesTotal
        : 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCredentials))]
    public partial string? CredentialsNote { get; set; }

    public bool HasCredentials => CredentialsNote is not null;

    /// <summary>0 for C#/.NET, 1 for Java/JVM, 2 to decide after discovery, -1 until a person chooses.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Target), nameof(CanGoNext))]
    public partial int TargetIndex { get; set; } = -1;

    public string? Target => TargetIndex switch {
        0 => "dotnet",
        1 => "jvm",
        2 => "deferred",
        _ => null,
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    public partial string DecidedBy { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool InitializeGit { get; set; } = true;

    [ObservableProperty]
    public partial string LegacySummary { get; set; } = string.Empty;

    public string TargetSummary => Target switch {
        null => string.Empty,
        "deferred" => $"Decided after discovery; the question is raised by {DecidedBy.Trim()}",
        _ => $"{TargetStackNames.Of(Target)}, decided by {DecidedBy.Trim()}",
    };

    public string ContextSummary
    {
        get {
            var unknown = Context.Count(c => c.Answer is null);
            var filled = $"{Context.Count - unknown} fields filled";
            var questions = unknown == 1 ? "becomes an open question" : "become open questions";

            return unknown == 0 ? filled : $"{filled} · {unknown} unknown {questions}";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCreateProblem))]
    public partial string? CreateProblem { get; set; }

    public bool HasCreateProblem => CreateProblem is not null;

    public string CreatedTitle => $"{Name.Trim()} is ready";

    public string CreatedText => _commit is null
        ? $"Created in {Folder}. Project setup can be signed off once you’ve read the project charter."
        : $"Created in {Folder} with the first commit {_commit}. "
            + "Project setup can be signed off once you’ve read the project charter.";

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (DecidedBy.Length == 0 && await _sender.Send(new GetDeciderQuery(), cancellationToken) is { } decider)
        {
            DecidedBy = decider;
        }
    }

    [RelayCommand]
    private async Task NextAsync(CancellationToken cancellationToken)
    {
        if (Step == 1)
        {
            var check = await _sender.Send(new CheckWorkspaceFolderQuery(Folder.Trim()), cancellationToken);
            FolderProblem = check.IsFailure ? "This folder isn’t empty. Pick a new folder or an empty one." : null;

            if (check.IsFailure)
            {
                return;
            }
        }

        if (Step == _lastStep)
        {
            await CreateAsync();

            return;
        }

        Step++;
        OnStepEntered();
    }

    [RelayCommand]
    private void Back()
    {
        Step--;
        OnStepEntered();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_navigation.CanGoBack)
        {
            _navigation.GoBack();

            return;
        }

        _navigation.NavigateTo("Start");
    }

    /// <summary>
    /// Picks where the workspace goes: an empty folder as it is, or a new folder named after the workspace inside it.
    /// </summary>
    [RelayCommand]
    private async Task BrowseFolderAsync(CancellationToken cancellationToken)
    {
        if (await _picker.PickFolderAsync() is not { } picked)
        {
            return;
        }

        var check = await _sender.Send(new CheckWorkspaceFolderQuery(picked), cancellationToken);
        Folder = check.IsSuccess ? picked : Path.Combine(picked, Slug(Name));
    }

    [RelayCommand]
    private async Task BrowseLegacyAsync()
    {
        if (await _picker.PickFolderAsync() is { } picked)
        {
            LegacySource = picked;
            await PreviewAsync();
        }
    }

    /// <summary>
    /// Scans the source folder read-only and shows what it holds; a new run replaces one still going.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task PreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(LegacySource))
        {
            Preview = PreviewState.None;

            return;
        }

        if (_previewing is not null)
        {
            await _previewing.CancelAsync();
            _previewing.Dispose();
        }

        _previewing = new CancellationTokenSource();
        var token = _previewing.Token;
        // Made on the UI thread, so the scan's reports post here; a replaced preview's late reports are dropped.
        var progress = new Progress<ScanProgress>(p => {
            if (!token.IsCancellationRequested)
            {
                Scanned = p;
            }
        });
        var query = new PreviewLegacyQuery(LegacySource.Trim(), SelectedCodePage.Number, progress);
        Scanned = null;
        Preview = PreviewState.Running;

        try
        {
            var preview = await _sender.Send(query, token);

            if (preview.IsFailure)
            {
                PreviewProblem = $"{preview.Error.Message} Pick the folder that holds the .dpr or .dpk files.";
                Preview = PreviewState.Failed;

                return;
            }

            ShowPreview(preview.Value);
            Preview = PreviewState.Done;
        }
        catch (OperationCanceledException)
        {
            // A newer preview replaced this one, or the person left the wizard; that run reports instead.
        }
    }

    [RelayCommand]
    private void OpenOverview()
    {
        _navigation.NavigateFresh("Overview");
    }

    partial void OnFolderChanged(string value)
    {
        FolderProblem = null;
    }

    partial void OnLegacySourceChanged(string value)
    {
        Preview = PreviewState.None;
    }

    partial void OnSelectedCodePageChanged(CodePage value)
    {
        if (Preview != PreviewState.None)
        {
            PreviewCommand.Execute(parameter: null);
        }
    }

    private bool IsEditing(int step)
    {
        return Phase == WizardPhase.Editing && Step == step;
    }

    private void OnStepEntered()
    {
        foreach (var step in Steps)
        {
            step.Show(Step);
        }

        if (Step == _lastStep)
        {
            OnPropertyChanged(nameof(TargetSummary));
            OnPropertyChanged(nameof(ContextSummary));
        }
    }

    private void ShowPreview(LegacyPreview preview)
    {
        var summary = preview.Summary;
        var extensions = summary.FilesByExtension
            .OrderByDescending(e => e.Count)
            .Select(e => e.Count > 1 ? $"{e.Count} {e.Name}" : e.Name);
        var unreadable = summary.BinaryForms > 0 ? $" · {summary.BinaryForms} can’t be read" : string.Empty;
        var version = preview.DelphiVersion is { } v
            ? $"{v.Version} · from {v.File}"
            : "Not named in the project files";
        var cycles = preview.Cycles.Count == 0
            ? "None"
            : $"{preview.Cycles.Count} · {string.Join(" ↔ ", preview.Cycles[0])}";

        Facts.Clear();
        Facts.Add(new PreviewFact("Files", $"{Number(summary.Files)} · {string.Join(", ", extensions)}"));
        Facts.Add(new PreviewFact("Project files", Named(summary.Projects, preview.ProjectNames)));
        Facts.Add(new PreviewFact("Code modules", Number(summary.Units)));
        Facts.Add(new PreviewFact("Screens", Number(summary.Forms) + unreadable));
        Facts.Add(new PreviewFact("Code lines", $"{Number(summary.CodeLines)} of {Number(summary.Lines)} lines"));
        Facts.Add(new PreviewFact("Delphi version", version));
        Facts.Add(new PreviewFact("Database access", Places(preview.DatabaseAccess)));
        Facts.Add(new PreviewFact("Third-party libraries", Places(preview.ThirdParty)));
        Facts.Add(new PreviewFact("Dependency cycles", cycles));
        CredentialsNote = preview.Credentials switch {
            0 => null,
            1 => "1 hard-coded credential found. It appears as *** everywhere in the studio.",
            _ => $"{Number(preview.Credentials)} hard-coded credentials found. "
                + "They appear as *** everywhere in the studio.",
        };
        var codePage = SelectedCodePage.Label.Split(' ')[0];
        var delphi = preview.DelphiVersion?.Version ?? "version unknown";
        LegacySummary = $"{Number(summary.Files)} files · {delphi} · code page {codePage}";
    }

    private async Task CreateAsync()
    {
        Phase = WizardPhase.Creating;
        CreateProblem = null;

        foreach (var row in Creation)
        {
            row.State = null;
            row.Detail = null;
        }

        var context = new ProjectContext(
            Context[0].Answer,
            Context[1].Answer,
            Context[2].Answer,
            Context[3].Answer,
            Context[4].Answer,
            Context[5].Answer);
        var request = new InitRequest(
            Folder.Trim(),
            Name.Trim(),
            LegacySource.Trim(),
            Target!,
            SelectedCodePage.Number,
            DecidedBy.Trim(),
            context,
            InitializeGit);
        var progress = new Progress<InitProgress>(OnProgress);
        // Creation runs to the end once started: Cancel is off, and a half-written folder helps nobody.
        var created = await _sender.Send(new CreateWorkspaceCommand(request, progress), CancellationToken.None);

        if (created.IsFailure)
        {
            CreateProblem = created.Error.Message;
            Phase = WizardPhase.Editing;

            return;
        }

        OnPropertyChanged(nameof(CreatedTitle));
        OnPropertyChanged(nameof(CreatedText));
        Phase = WizardPhase.Created;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewing?.Cancel();
            _previewing?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnProgress(InitProgress report)
    {
        var row = Creation.First(r => r.Step == report.Step);
        row.State = report.State;
        row.Detail = report.Detail;

        if (report is { Step: InitStep.FirstCommit, State: InitStepState.Done })
        {
            _commit = report.Detail;
        }
    }

    private static string Places(IReadOnlyList<NamedCount> uses)
    {
        return uses.Count == 0
            ? "None found"
            : string.Join(
                ", ",
                uses.Select(u => $"{u.Name} · {Number(u.Count)} {(u.Count == 1 ? "place" : "places")}"));
    }

    private static string Named(int count, IReadOnlyList<string> names)
    {
        return names.Count == 0 ? Number(count) : $"{Number(count)} · {string.Join(", ", names)}";
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static string Slug(string name)
    {
        var lower = name.ToLowerInvariant();
        var dashed = NonWord().Replace(lower, "-");
        var slug = dashed.Trim('-');

        return slug.Length == 0 ? "modernization" : slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NonWord();
}
