using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>
/// What a page that needs a workspace shows without one: what it would show, New workspace and Open folder.
/// </summary>
public sealed partial class NoWorkspaceState : UserControl
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(NoWorkspaceState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(NoWorkspaceState), new PropertyMetadata(string.Empty));

    private readonly IWorkspaceActions _actions;

    public NoWorkspaceState()
    {
        _actions = ContractServices.Provider.GetRequiredService<IWorkspaceActions>();
        InitializeComponent();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>What the page would show once a workspace is open.</summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private void OnNewWorkspace(object sender, RoutedEventArgs e)
    {
        _actions.StartNewWorkspace();
    }

    private async void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        var problem = await _actions.OpenFolderAsync();
        Problem.Message = problem ?? string.Empty;
        Problem.IsOpen = problem is not null;
    }
}
