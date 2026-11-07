using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Application.Abstractions.Agents;

/// <summary>Asks the Workstreams module whether Claude Code is installed and recent enough.</summary>
/// <remarks>A shared contract so the Start and Settings pages can ask without referencing that module.</remarks>
public sealed record GetClaudeStatusQuery : IRequest<ClaudeStatus>;
