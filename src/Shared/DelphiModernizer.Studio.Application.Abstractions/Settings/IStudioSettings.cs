namespace DelphiModernizer.Studio.Application.Abstractions.Settings;

/// <summary>The person's studio settings; the App persists them, modules read them.</summary>
public interface IStudioSettings
{
    AgentSettings Agents { get; }

    /// <summary>The code page for legacy files without a byte order mark when a new workspace is created.</summary>
    int DefaultCodePage { get; }

    /// <summary>
    /// The folder holding the bundled <c>dmz</c>, put first on an agent's PATH; null when not bundled.
    /// </summary>
    string? ToolDirectory { get; }

    void SaveAgents(AgentSettings settings);

    void SaveDefaultCodePage(int codePage);
}
