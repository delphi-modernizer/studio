using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Traceability.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;

/// <summary>Problems in the documents, by severity, and the requirements matrix.</summary>
public sealed partial class TraceabilityViewModel : WorkspacePageViewModel
{
    private const string _dash = "—";

    private readonly ISender _sender;
    private readonly IEditorLauncher _editor;
    private readonly ProblemsBadge _badge;
    private List<ProblemRow> _all = [];

    public TraceabilityViewModel(IWorkspaceSession session, ISender sender, IEditorLauncher editor, ProblemsBadge badge)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(badge);
        _sender = sender;
        _editor = editor;
        _badge = badge;
    }

    /// <summary>Whether warnings count as errors, as the final plan's sign-off counts them.</summary>
    [ObservableProperty]
    public partial bool Strict { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadProblem))]
    public partial string? LoadProblem { get; set; }

    public bool HasLoadProblem => LoadProblem is not null;

    [ObservableProperty]
    public partial IReadOnlyList<StatCard> Stats { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Choice<TraceView>> Views { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Choice<SeverityFilter>> Severities { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIssues), nameof(IsMatrix), nameof(IsClean), nameof(HasProblems))]
    public partial TraceView View { get; set; }

    [ObservableProperty]
    public partial SeverityFilter Severity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsClean), nameof(HasProblems))]
    public partial IReadOnlyList<ProblemRow> Problems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MatrixRow> Matrix { get; set; } = [];

    public bool IsIssues => View == TraceView.Issues;

    public bool IsMatrix => View == TraceView.Matrix;

    public bool IsClean => IsIssues && _all.Count == 0;

    public bool HasProblems => IsIssues && _all.Count > 0;

    public Task<bool> OpenAsync(string path, int line)
    {
        return _editor.OpenAsync(path, line);
    }

    protected override void OnWorkspaceChanged()
    {
        IsOpen = false;

        if (HasWorkspace)
        {
            CheckCommand.Execute(parameter: null);
        }
    }

    [RelayCommand]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (!HasWorkspace)
        {
            return;
        }

        var checkedAt = DateTimeOffset.Now;
        var report = await _sender.Send(new RunTraceQuery(Strict), cancellationToken);
        LoadProblem = report.IsFailure ? report.Error.Message : null;

        if (report.IsSuccess)
        {
            Show(report.Value, checkedAt);

            if (!Strict)
            {
                _badge.Show(report.Value.Trace.Errors);
            }
        }

        IsOpen = report.IsSuccess;
    }

    partial void OnStrictChanged(bool value)
    {
        CheckCommand.Execute(parameter: null);
    }

    partial void OnSeverityChanged(SeverityFilter value)
    {
        Problems = Filtered();
    }

    private void Show(TraceabilityReport report, DateTimeOffset checkedAt)
    {
        var trace = report.Trace;
        var time = checkedAt.LocalDateTime.ToString("t", CultureInfo.CurrentCulture);
        var questions = report.Problems.Count(p => p.Issue.Rule == "open-question");
        _all = [.. report.Problems.Select(Row)];
        Stats = [
            new StatCard(
                "Errors",
                StudioIcons.Error,
                Number(trace.Errors),
                "errors",
                Strict ? $"Warnings included · last check {time}" : $"Last check {time}",
                trace.Errors == 0 ? StudioIcons.CheckPassed : StudioIcons.ErrorFilled,
                trace.Errors == 0 ? StatTone.Success : StatTone.Critical),
            new StatCard(
                "Warnings",
                StudioIcons.Warning,
                Number(trace.Warnings),
                "warnings",
                WarningsDetail(trace.Warnings, questions),
                StudioIcons.WarningFilled,
                trace.Warnings == 0 ? StatTone.None : StatTone.Caution),
            new StatCard("Sign-offs", StudioIcons.SignOffs, Number(report.HeldUp), "held up", "by problems in the documents", string.Empty, StatTone.None),
            new StatCard("Checks", StudioIcons.Traceability, Number(report.Checks), "checks", "Run over every document in docs/", string.Empty, StatTone.None),
        ];
        Views = [new(TraceView.Issues, "Issues", _all.Count), new(TraceView.Matrix, "Matrix", trace.Matrix.Length)];
        Severities = [
            new(SeverityFilter.All, "All", _all.Count),
            new(SeverityFilter.Errors, "Errors", _all.Count(p => p.IsError)),
            new(SeverityFilter.Warnings, "Warnings", _all.Count(p => !p.IsError)),
        ];
        Matrix = [.. trace.Matrix.Select(m => new MatrixRow(
            m.Id,
            m.Path,
            m.Line,
            Joined(m.BusinessRules),
            [.. m.Evidence],
            Joined(m.Decisions),
            Joined(m.WorkPackages),
            string.Join(", ", m.Tests)))];
        Problems = Filtered();
        OnPropertyChanged(nameof(IsClean));
        OnPropertyChanged(nameof(HasProblems));
    }

    private string WarningsDetail(int warnings, int questions)
    {
        if (Strict)
        {
            return "Counted as errors";
        }

        if (warnings == 0)
        {
            return "Clean for the final plan too";
        }

        var noun = questions == 1 ? "question" : "questions";

        return questions == 0 ? "The final plan needs none" : $"Including {Number(questions)} open {noun}";
    }

    private List<ProblemRow> Filtered()
    {
        return Severity switch {
            SeverityFilter.Errors => _all.FindAll(p => p.IsError),
            SeverityFilter.Warnings => _all.FindAll(p => !p.IsError),
            _ => _all,
        };
    }

    private static ProblemRow Row(Problem problem)
    {
        var issue = problem.Issue;
        var pill = issue.Severity == "error" ? PillState.Error : PillState.Warning;
        var rule = problem.Subject is null ? problem.Wording.Label : $"{problem.Wording.Label} · {problem.Subject}";
        var folder = Path.GetDirectoryName(issue.Path)?.Replace('\\', '/') is { Length: > 0 } directory ? directory + "/" : string.Empty;
        var link = issue.Line > 0 ? $"{Path.GetFileName(issue.Path)}#L{issue.Line}" : Path.GetFileName(issue.Path);

        return new ProblemRow(pill, problem.Description, rule, issue.Path, issue.Line, link, folder);
    }

    private static string Joined(IReadOnlyCollection<string> ids)
    {
        return ids.Count == 0 ? _dash : string.Join(", ", ids);
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
