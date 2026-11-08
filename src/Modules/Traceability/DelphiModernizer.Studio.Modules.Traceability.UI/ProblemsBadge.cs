using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Traceability.Application;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Traceability.UI;

/// <summary>The Traceability item's badge: the problems the trace finds in the open workspace's documents.</summary>
/// <remarks>
/// It checks again on every navigation, so a document changed outside the studio, such as one deleted, shows on the
/// next page; it keeps the strictness of the Traceability page's last check.
/// </remarks>
public sealed class ProblemsBadge : IBadgeSource
{
    private readonly IWorkspaceSession _session;
    private readonly ISender _sender;
    private bool _strict;

    public ProblemsBadge(IWorkspaceSession session, ISender sender, INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        _session = session;
        _sender = sender;
        _session.Changed += async (_, _) => await RefreshAsync();
        navigation.Navigated += async (_, _) => await RefreshAsync();
    }

    public event EventHandler? Changed;

    public BadgeTone Tone => BadgeTone.Critical;

    public int Count { get; private set; }

    public string Describe(int count)
    {
        return count == 1 ? "1 problem in the documents" : $"{count} problems in the documents";
    }

    /// <summary>Shows the errors of a check the Traceability page just ran, so the badge never lags the page.</summary>
    public void Show(int errors, bool strict)
    {
        _strict = strict;
        Count = errors;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task RefreshAsync()
    {
        var trace = _session.Current is null ? null : await _sender.Send(new RunTraceQuery(_strict));
        Show(trace is { IsSuccess: true } ? trace.Value.Trace.Errors : 0, _strict);
    }
}
