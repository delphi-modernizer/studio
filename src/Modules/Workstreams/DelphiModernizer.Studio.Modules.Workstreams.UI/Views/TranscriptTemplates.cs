using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>Picks the transcript template for each kind of row.</summary>
public sealed partial class TranscriptTemplates : DataTemplateSelector
{
    public DataTemplate? Session { get; set; }

    public DataTemplate? Agent { get; set; }

    public DataTemplate? Tool { get; set; }

    public DataTemplate? Refusal { get; set; }

    public DataTemplate? Diagnostic { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch {
            SessionRow => Session,
            AgentRow => Agent,
            ToolRow => Tool,
            RefusalRow => Refusal,
            _ => Diagnostic,
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
