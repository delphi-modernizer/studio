using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Modules.SignOffs.Application;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;

/// <summary>The Approve or Revoke dialog for one sign-off: who approves, the note, and what the engine said.</summary>
/// <remarks>Security decision: this is the only path to an approval; it never passes an approver name.</remarks>
public sealed partial class ApprovalDialogViewModel : ObservableObject
{
    private readonly ISender _sender;

    public ApprovalDialogViewModel(ISender sender, SignOffRow signOff, bool isRevoke, GitIdentity? approver)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(signOff);
        _sender = sender;
        SignOff = signOff;
        IsRevoke = isRevoke;
        HasIdentity = approver is not null;
        ApproverName = approver?.Name ?? string.Empty;
        ApproverDetail = approver?.Email is { } email
            ? $"{email} · from git config in this workspace"
            : "From git config in this workspace";
        string announced = approver?.Email is { } address ? $"{approver.Name}, {address}" : ApproverName;
        AnnouncedApprover = $"{AsLabel} {announced}";
    }

    public SignOffRow SignOff { get; }

    public bool IsRevoke { get; }

    public bool IsApprove => !IsRevoke;

    /// <summary>Approving the last sign-off lets agents write the new code, so the dialog says so.</summary>
    public bool IsFinalPlan => IsApprove && SignOff.IsLast;

    public bool HasIdentity { get; }

    public bool HasNoIdentity => !HasIdentity;

    public string ApproverName { get; }

    public string ApproverDetail { get; }

    public string AnnouncedApprover { get; }

    public string Title => IsRevoke ? $"Revoke the {SignOff.Name} approval?" : $"Approve the {SignOff.Name} sign-off?";

    public string Lead => IsRevoke
        ? $"{SignOff.Name} goes back to Ready or Blocked, whichever its checks say. "
            + "Later sign-offs can’t be approved until it is approved again."
        : "Approving records that you, a person, reviewed what this sign-off covers. "
            + "The record goes into the workspace’s git history; an agent can’t make it.";

    public string NoIdentityMessage => "An approval records who approved it. "
        + $"Set your name and email for this workspace, then {(IsRevoke ? "revoke" : "approve")}.";

    public string AsLabel => IsRevoke ? "Revoking as" : "Approving as";

    public string FieldLabel => IsRevoke ? "Reason" : "Note";

    public string FieldName => $"{FieldLabel}, required";

    public string Placeholder => IsRevoke
        ? "Why the approval no longer holds"
        : "What you reviewed, and anything you accept as still open";

    public string Fine => IsRevoke
        ? "Adds a revoke entry to .modernizer/approvals.json and commits it. "
            + "The history keeps both the approval and the revocation."
        : "Adds an approve entry to .modernizer/approvals.json and commits it. "
            + "If a file it covers changes later, this sign-off turns Stale.";

    public string PrimaryText => IsRevoke ? "Revoke approval" : "Approve";


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial bool IsRefused { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string Refusal { get; set; } = string.Empty;

    /// <summary>Whether the refusal comes from problems in the documents, so it offers to show them.</summary>
    [ObservableProperty]
    public partial bool RefusalShowsProblems { get; set; }

    /// <summary>
    /// Set when the person chose Show the problems, so the page goes to Traceability after the dialog.
    /// </summary>
    public bool ShowProblemsAfter { get; set; }

    public bool CanConfirm => HasIdentity && !IsRefused && !IsRecording && !string.IsNullOrWhiteSpace(Note);

    /// <returns>
    /// True when the engine recorded the entry; false keeps the dialog open with the refusal and the note.
    /// </returns>
    public async Task<bool> ConfirmAsync(CancellationToken cancellationToken)
    {
        if (!CanConfirm)
        {
            return false;
        }

        IsRecording = true;
        string note = Note.Trim();
        var recorded = IsRevoke
            ? await _sender.Send(new RevokeGateCommand(SignOff.Gate, note), cancellationToken)
            : await _sender.Send(new ApproveGateCommand(SignOff.Gate, note), cancellationToken);
        IsRecording = false;

        if (recorded.IsSuccess)
        {
            return true;
        }

        await ShowRefusalAsync(recorded.Error, cancellationToken);

        return false;
    }

    private async Task ShowRefusalAsync(Error error, CancellationToken cancellationToken)
    {
        if (!SignOffRefusal.IsRefusal(error))
        {
            Refusal = error.Message;
            IsRefused = true;

            return;
        }

        var signOffs = await _sender.Send(new GetSignOffsQuery(), cancellationToken);
        var now = signOffs.IsSuccess ? signOffs.Value.FirstOrDefault(s => s.Status.Gate == SignOff.Gate) : null;
        string reason = now is null ? "a check fails now" : SignOffRefusal.Reason(now);
        Refusal = $"{SignOff.Name} can’t be approved yet: {reason}. "
            + "A check changed after this dialog opened. Your note is kept.";
        RefusalShowsProblems = now?.Checks.Any(c => c is { Passed: false, ShowsProblems: true }) == true;
        IsRefused = true;
    }
}
