using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>The top of every page: its title, one line saying what it is for, and its actions on the right.</summary>
/// <remarks>Actions are 8 px apart, at most one of them accent (handoff notes).</remarks>
[ContentProperty(Name = nameof(Actions))]
public sealed partial class PageHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PurposeProperty = DependencyProperty.Register(
        nameof(Purpose), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

    public PageHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Purpose
    {
        get => (string)GetValue(PurposeProperty);
        set => SetValue(PurposeProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}
