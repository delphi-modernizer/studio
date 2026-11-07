using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

public static class WorkstreamsErrors
{
    public static Error UnknownRecipe(string id)
    {
        return new Error("Workstreams.UnknownRecipe", $"The kit has no recipe {id}.");
    }
}
