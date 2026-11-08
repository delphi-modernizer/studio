using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

public static class WorkstreamsErrors
{
    public static Error NoBudget { get; } = new("Workstreams.NoBudget", "A run needs a budget above $0.");

    public static Error UnknownRun(string id)
    {
        return new Error("Workstreams.UnknownRun", $"The run store has no run {id}.");
    }

    public static Error UnknownRecipe(string id)
    {
        return new Error("Workstreams.UnknownRecipe", $"The kit has no recipe {id}.");
    }
}
