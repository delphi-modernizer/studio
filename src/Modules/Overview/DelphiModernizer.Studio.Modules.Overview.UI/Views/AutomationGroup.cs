using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Overview.UI.Views;

/// <summary>A grid that Narrator reads as a group under its AutomationProperties.Name.</summary>
/// <remarks>Panels have no automation peer, so a name set on a plain Grid or Canvas is never read.</remarks>
public sealed partial class AutomationGroup : Grid
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new GroupPeer(this);
    }

    private sealed partial class GroupPeer(AutomationGroup owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetClassNameCore()
        {
            return nameof(AutomationGroup);
        }
    }
}
