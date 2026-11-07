namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>Where a run stands, as its status pill says it.</summary>
public enum RunOutcome
{
    Running,
    Completed,
    Failed,
    Stopped,
    BudgetReached,
}

public static class RunOutcomes
{
    /// <param name="outcome">
    /// The engine's <see cref="Engine.Agents.RunCompleted.Outcome"/>; null while the run goes on.
    /// </param>
    public static RunOutcome Of(string? outcome)
    {
        return outcome switch {
            null => RunOutcome.Running,
            "success" => RunOutcome.Completed,
            "cancelled" => RunOutcome.Stopped,
            "error_max_budget_usd" => RunOutcome.BudgetReached,
            _ => RunOutcome.Failed,
        };
    }
}
