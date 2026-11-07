using System;
using System.IO;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>The person's agent settings and defaults, kept with the other local settings.</summary>
public sealed class StudioSettings : IStudioSettings
{
    private const string _agentsKey = "Agents";
    private const string _codePageKey = "DefaultCodePage";
    private const int _westernEuropean = 1252;

    private readonly ILocalSettingsService _settings;

    public StudioSettings(ILocalSettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    public AgentSettings Agents => _settings.Read<AgentSettings>(_agentsKey) ?? AgentSettings.Default;

    public int DefaultCodePage => _settings.Read<int?>(_codePageKey) ?? _westernEuropean;

    /// <summary>The <c>dmz</c> the build bundles beside the app, which an agent's guard hook calls.</summary>
    public string? ToolDirectory
    {
        get {
            var tools = Path.Combine(AppContext.BaseDirectory, "tools");
            var dmz = OperatingSystem.IsWindows() ? "dmz.exe" : "dmz";

            return File.Exists(Path.Combine(tools, dmz)) ? tools : null;
        }
    }

    public void SaveAgents(AgentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings.Save(_agentsKey, settings);
    }

    public void SaveDefaultCodePage(int codePage)
    {
        _settings.Save(_codePageKey, codePage);
    }
}
