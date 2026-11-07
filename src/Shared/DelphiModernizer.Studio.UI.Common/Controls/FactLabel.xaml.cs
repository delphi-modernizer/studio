using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>
/// How sure an agent is of a claim; it sits right after the claim and travels with it into every summary.
/// </summary>
public enum FactKind
{
    Confirmed,
    Inferred,
    Assumption,
    Unknown,
}

/// <summary>An outlined tag naming a claim's <see cref="FactKind"/>.</summary>
public sealed partial class FactLabel : UserControl
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(FactKind), typeof(FactLabel), new PropertyMetadata(FactKind.Unknown, OnKindChanged));

    public FactLabel()
    {
        InitializeComponent();
        Show();
    }

    public FactKind Kind
    {
        get => (FactKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnKindChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((FactLabel)sender).Show();
    }

    private void Show()
    {
        Word.Text = Kind.ToString();
        AutomationProperties.SetName(this, Word.Text);
    }
}
