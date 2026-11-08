using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>
/// What a card or page shows when it has nothing yet, and what would fill it; its content is the actions.
/// </summary>
/// <remarks>Every empty state says what agents would add or how to get data in (design rule).</remarks>
[ContentProperty(Name = nameof(Actions))]
public sealed partial class EmptyState : UserControl
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty IsPageLevelProperty = DependencyProperty.Register(
        nameof(IsPageLevel), typeof(bool), typeof(EmptyState), new PropertyMetadata(false, OnIsPageLevelChanged));

    public EmptyState()
    {
        InitializeComponent();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>
    /// Whether it fills a page: a 48 px glyph and a Subtitle heading instead of 32 px and BodyStrong.
    /// </summary>
    public bool IsPageLevel
    {
        get => (bool)GetValue(IsPageLevelProperty);
        set => SetValue(IsPageLevelProperty, value);
    }

    private static void OnIsPageLevelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var state = (EmptyState)sender;
        bool pageLevel = (bool)args.NewValue;
        state.Icon.FontSize = pageLevel ? 48 : 32;
        string style = pageLevel ? "SubtitleTextBlockStyle" : "BodyStrongTextBlockStyle";
        state.Heading.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[style];
        // Canvas .empty p: 44ch, in body text on a page and in caption text in a card.
        string description = pageLevel ? "SecondaryBodyTextStyle" : "SecondaryCaptionTextStyle";
        state.Description.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources[description];
        state.Description.MaxWidth = pageLevel ? 344 : 296;
    }
}
