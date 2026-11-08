using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A StackPanel as wide as it is offered, up to its MaxWidth, whatever its children ask for.</summary>
/// <remarks>
/// A left-aligned StackPanel is as wide as its widest child, so a page's cards change width from state to state; this
/// one keeps its alignment and asks for the whole width.
/// </remarks>
public sealed partial class FullWidthStackPanel : StackPanel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        return double.IsInfinity(availableSize.Width) ? size : new Size(availableSize.Width, size.Height);
    }
}
