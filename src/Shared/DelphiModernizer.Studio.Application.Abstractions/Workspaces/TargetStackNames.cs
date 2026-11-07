namespace DelphiModernizer.Studio.Application.Abstractions.Workspaces;

/// <summary>The names people read for the manifest's target stacks.</summary>
public static class TargetStackNames
{
    public static string Of(string stack)
    {
        return stack switch {
            "dotnet" => "C#/.NET",
            "jvm" => "Java/JVM",
            _ => "Undecided",
        };
    }
}
