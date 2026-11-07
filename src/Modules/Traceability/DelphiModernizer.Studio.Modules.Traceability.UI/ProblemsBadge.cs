using System;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Traceability.Application;
using DelphiModernizer.Studio.UI.Contracts;

namespace DelphiModernizer.Studio.Modules.Traceability.UI;

/// <summary>The Traceability item's badge: the problems the trace finds in the open workspace's documents.</summary>
internal sealed class ProblemsBadge : IBadgeSource
{
    private readonly IWorkspaceSession _session;
    private readonly ISender _sender;

    public ProblemsBadge(IWorkspaceSession session, ISender sender)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sender);
        _session = session;
        _sender = sender;
        _session.Changed += OnSessionChanged;
    }

    public event EventHandler? Changed;

    public BadgeTone Tone => BadgeTone.Critical;

    public int Count { get; private set; }

    public string Describe(int count)
    {
        return count == 1 ? "1 problem in the documents" : $"{count} problems in the documents";
    }

    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        var trace = _session.Current is null ? null : await _sender.Send(new RunTraceQuery(Strict: false));
        Count = trace is { IsSuccess: true } ? trace.Value.Errors : 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
