using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DelphiModernizer.Studio.Modules.Settings.UI.ViewModels;

/// <summary>How agent runs use Claude Code, and the studio's defaults; each change is saved as it is made.</summary>
public sealed partial class SettingsViewModel : ViewModel
{
    private static readonly ElementTheme[] _themes = [ElementTheme.Default, ElementTheme.Light, ElementTheme.Dark];

    private readonly ISender _sender;
    private readonly IStudioSettings _settings;
    private readonly IThemeService _theme;
    private readonly IPickerService _picker;
    private readonly IClipboardService _clipboard;
    private readonly bool _loaded;
    private DateTimeOffset? _testedAt;

    public SettingsViewModel(
        ISender sender,
        IStudioSettings settings,
        IThemeService theme,
        IPickerService picker,
        IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentNullException.ThrowIfNull(clipboard);
        _sender = sender;
        _settings = settings;
        _theme = theme;
        _picker = picker;
        _clipboard = clipboard;
        var agents = settings.Agents;
        ClaudePath = agents.ClaudePath ?? string.Empty;
        Model = agents.Model ?? string.Empty;
        EffortIndex = Math.Max(0, Array.IndexOf(Efforts, agents.Effort ?? Efforts[0]));
        Budget = (double)agents.BudgetUsd;
        SelectedCodePage = CodePages.Common.FirstOrDefault(c => c.Number == settings.DefaultCodePage)
            ?? CodePages.Common[0];
        ThemeIndex = Math.Max(0, Array.IndexOf(_themes, theme.Theme));
        _loaded = true;
    }

    /// <summary>What Effort offers; the first keeps Claude Code's own default.</summary>
    public static string[] Efforts { get; } = ["Claude Code’s default", "low", "medium", "high", "xhigh", "max"];

    public static IReadOnlyList<string> Models { get; } = ["opus", "sonnet", "haiku"];

    [ObservableProperty]
    public partial string ClaudePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Pill), nameof(PillText), nameof(Help), nameof(PathHelp), nameof(Problem))]
    [NotifyPropertyChangedFor(nameof(HasProblem), nameof(IsMissing), nameof(HasStatus))]
    public partial ClaudeStatus? Claude { get; set; }

    public bool HasStatus => Claude is not null;

    public PillState Pill => Claude?.State switch {
        ClaudeState.Ready => PillState.Found,
        ClaudeState.TooOld => PillState.TooOld,
        ClaudeState.Unusable => PillState.Unusable,
        _ => PillState.NotFound,
    };

    public string? PillText => Claude is { State: ClaudeState.Ready, Version: { } version }
        ? $"Claude Code {version}"
        : null;

    public string Help => Claude switch {
        null => "Testing Claude Code…",
        { State: ClaudeState.Ready } ready =>
            $"Tested {_testedTime}: version {ready.Version} meets the {ready.Minimum} minimum.",
        { State: ClaudeState.NotFound } missing =>
            $"Agent runs need Claude Code {missing.Minimum} or later on this PC.",
        _ => $"Agent runs need the native claude.exe, version {Claude.Minimum} or later.",
    };

    /// <summary>The engine's message for a file that cannot be used, as it wrote it.</summary>
    public string? Problem => Claude is { State: ClaudeState.TooOld or ClaudeState.Unusable } ? Claude.Problem : null;

    public bool HasProblem => Problem is not null;

    /// <summary>
    /// What Narrator adds to the path box: the problem with the file chosen, else the card's description.
    /// </summary>
    public string PathHelp => Problem ?? Help;

    public bool IsMissing => Claude is { State: ClaudeState.NotFound };

    [ObservableProperty]
    public partial string Model { get; set; }

    [ObservableProperty]
    public partial int EffortIndex { get; set; }

    [ObservableProperty]
    public partial double Budget { get; set; }

    [ObservableProperty]
    public partial CodePage SelectedCodePage { get; set; }

    /// <summary>0 follows Windows, 1 is light, 2 is dark; high contrast always follows Windows.</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    private string _testedTime =>
        (_testedAt ?? DateTimeOffset.Now).LocalDateTime.ToString("t", CultureInfo.CurrentCulture);

    [RelayCommand]
    private async Task TestAsync(CancellationToken cancellationToken)
    {
        SaveAgents();
        Claude = null;
        var status = await _sender.Send(new GetClaudeStatusQuery(), cancellationToken);
        _testedAt = DateTimeOffset.Now;
        Claude = status;
    }

    [RelayCommand]
    private async Task BrowseAsync(CancellationToken cancellationToken)
    {
        if (await _picker.PickFileAsync(".exe") is { } path)
        {
            ClaudePath = path;
            await TestAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private void CopyVersions()
    {
        _clipboard.SetText(StudioVersions.Current.Line);
    }

    partial void OnModelChanged(string value)
    {
        SaveAgents();
    }

    partial void OnEffortIndexChanged(int value)
    {
        SaveAgents();
    }

    partial void OnBudgetChanged(double value)
    {
        SaveAgents();
    }

    partial void OnSelectedCodePageChanged(CodePage value)
    {
        if (_loaded)
        {
            _settings.SaveDefaultCodePage(value.Number);
        }
    }

    partial void OnThemeIndexChanged(int value)
    {
        if (_loaded && value >= 0 && value < _themes.Length)
        {
            _theme.SetTheme(_themes[value]);
        }
    }

    /// <remarks>An empty box means the default; a budget that is not a positive number keeps the one saved.</remarks>
    private void SaveAgents()
    {
        if (!_loaded)
        {
            return;
        }

        var saved = _settings.Agents;
        var budget = double.IsFinite(Budget) && Budget > 0 ? (decimal)Budget : saved.BudgetUsd;
        _settings.SaveAgents(new AgentSettings(
            NullIfEmpty(ClaudePath),
            NullIfEmpty(Model),
            EffortIndex > 0 ? Efforts[EffortIndex] : null,
            budget));
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
