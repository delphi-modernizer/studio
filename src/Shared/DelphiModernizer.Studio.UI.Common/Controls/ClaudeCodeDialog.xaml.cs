using System;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Says why agent runs are off and offers the way to turn them on: install, choose claude.exe, or update.</summary>
public sealed partial class ClaudeCodeDialog : ContentDialog
{
    public ClaudeCodeDialog(ClaudeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        InitializeComponent();
        IsUpdate = status.State == ClaudeState.TooOld;
        Title = IsUpdate ? "Claude Code needs an update" : "Claude Code isn’t installed";
        Lead.Text = IsUpdate
            ? $"The studio drives Claude Code {status.Minimum} or later. Update it, then test it again; nothing else in the workspace is affected."
            : "Running an agent task needs Claude Code on this PC. Install it, or point the studio at claude.exe in Settings.";
        EngineMessage.Text = status.Problem ?? string.Empty;
        PrimaryButtonText = IsUpdate ? "Test again" : "Get Claude Code";
        SecondaryButtonText = IsUpdate ? "Open Settings" : "Choose claude.exe…";
        Opened += (_, _) => FocusPrimary();
    }

    /// <summary>True when Claude Code is found but too old, so the buttons offer a test and Settings.</summary>
    public bool IsUpdate { get; }

    /// <summary>Focus starts on the primary action, so Tab goes on to the secondary one and then Not now.</summary>
    private void FocusPrimary()
    {
        if (GetTemplateChild("PrimaryButton") is Button primary)
        {
            primary.Focus(FocusState.Programmatic);
        }
    }
}
