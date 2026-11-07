using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace DelphiModernizer.Studio.UI.Common;

/// <summary>Has Narrator read a change that moves no focus, after what it is saying, as a polite live region.</summary>
public static class Narration
{
    /// <param name="source">
    /// An element on the page with an automation peer of its own, such as a button or a text block; panels have none.
    /// </param>
    public static void Announce(UIElement source, string text)
    {
        ArgumentNullException.ThrowIfNull(source);
        var peer = FrameworkElementAutomationPeer.FromElement(source)
            ?? FrameworkElementAutomationPeer.CreatePeerForElement(source);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.All,
            text,
            nameof(Narration));
    }
}
