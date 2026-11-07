using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Gives a user control that shows text a peer of its own, read as text by its Name.</summary>
/// <remarks>A UserControl has no peer, so without one its Name never reaches Narrator.</remarks>
internal sealed partial class TextAutomationPeer(FrameworkElement owner, string className)
    : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore()
    {
        return className;
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Text;
    }
}
