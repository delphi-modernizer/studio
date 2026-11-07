using DelphiModernizer.Studio.Application.Abstractions.Evidence;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A link to the legacy code a claim comes from; it opens the file in the person's editor at the cited lines.</summary>
public sealed partial class EvidenceLink : UserControl
{
    public static readonly DependencyProperty ReferenceProperty = DependencyProperty.Register(
        nameof(Reference), typeof(string), typeof(EvidenceLink), new PropertyMetadata(string.Empty, OnReferenceChanged));

    private EvidenceReference? _parsed;

    public EvidenceLink()
    {
        InitializeComponent();
    }

    /// <summary>The link as the documents write it, such as <c>legacy/Sales/Pricing.pas#L18-L29</c>.</summary>
    public string Reference
    {
        get => (string)GetValue(ReferenceProperty);
        set => SetValue(ReferenceProperty, value);
    }

    private static void OnReferenceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var link = (EvidenceLink)sender;
        var text = (string)args.NewValue;
        link._parsed = EvidenceReference.TryParse(text, out var parsed) ? parsed : null;
        link.Words.Text = text;
        link.Link.IsEnabled = link._parsed is not null;
        AutomationProperties.SetName(link.Link, link._parsed?.Describe() ?? text);
    }

    private async void OnClick(object sender, RoutedEventArgs e)
    {
        if (_parsed is { } evidence)
        {
            await ContractServices.Provider
                .GetRequiredService<IEditorLauncher>()
                .OpenAsync(evidence.Path, evidence.StartLine);
        }
    }
}
