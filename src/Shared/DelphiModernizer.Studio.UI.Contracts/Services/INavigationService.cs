using System;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>Shows pages, resolved from the container, in the shell's frame; a page's key is its type name without "Page".</summary>
/// <remarks>Every navigation pushes the page it leaves, so Back returns to it with its scroll position and selection.</remarks>
public interface INavigationService
{
    event EventHandler<NavigationEvent>? Navigated;

    bool CanGoBack { get; }

    void Initialize(Frame frame);

    string GetPageKey(Type pageType);

    bool NavigateTo(string pageKey, object? parameter = null);

    /// <summary>Navigates and forgets every earlier page, as opening a workspace does.</summary>
    bool NavigateFresh(string pageKey, object? parameter = null);

    void GoBack();
}

public sealed record NavigationEvent(string PageKey, Type PageType);
