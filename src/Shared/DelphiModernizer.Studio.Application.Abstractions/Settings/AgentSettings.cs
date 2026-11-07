namespace DelphiModernizer.Studio.Application.Abstractions.Settings;

/// <summary>How the studio launches Claude Code, as the person set it on the Settings page.</summary>
/// <param name="ClaudePath">The executable; null finds <c>claude</c> on PATH.</param>
/// <param name="Model">A model alias passed to <c>--model</c>; null keeps the CLI's default.</param>
/// <param name="Effort">Passed to <c>--effort</c>; null keeps the CLI's default.</param>
/// <param name="BudgetUsd">The spend at which a run stops.</param>
public sealed record AgentSettings(string? ClaudePath, string? Model, string? Effort, decimal BudgetUsd)
{
    public static readonly AgentSettings Default = new(null, null, null, 2m);
}
