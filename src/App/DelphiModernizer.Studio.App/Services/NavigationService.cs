using System;
using System.Collections.Generic;
using System.Linq;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>
/// Shows pages in the shell's frame. Each page resolves in its own scope and stays alive on the back stack, so Back
/// returns to the same page with its scroll position and selection, as Windows Settings does.
/// </summary>
/// <remarks>A linked list keeps the stack bounded: past the limit the oldest page and its scope are released.</remarks>
public sealed class NavigationService : INavigationService
{
    private const string _suffix = "Page";
    private const int _depth = 30;

    private readonly IServiceScopeFactory _scopes;
    private readonly Dictionary<string, Type> _pages = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _back = new();
    private Frame? _frame;
    private Entry? _current;

    public NavigationService(IServiceScopeFactory scopes, IEnumerable<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(modules);
        _scopes = scopes;

        var pages = modules.SelectMany(module => module
            .GetNavigationItems()
            .Select(item => item.PageType)
            .Concat(module.GetPages()));

        foreach (var page in pages)
        {
            _pages[GetPageKey(page)] = page;
        }
    }

    public event EventHandler<NavigationEvent>? Navigated;

    public bool CanGoBack => _back.Count > 0;

    public void Initialize(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _frame = frame;
    }

    public string GetPageKey(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        return pageType.Name.EndsWith(_suffix, StringComparison.Ordinal)
            ? pageType.Name[..^_suffix.Length]
            : throw new ArgumentException($"{pageType.Name} must end with {_suffix}.", nameof(pageType));
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        NavTrace.Write($"NavigateTo {pageKey} from {_current?.Key} back={_back.Count}");

        if (Create(pageKey, parameter) is not { } entry)
        {
            return false;
        }

        if (_current is not null)
        {
            _back.AddLast(_current);

            if (_back.Count > _depth)
            {
                _back.First!.Value.Scope.Dispose();
                _back.RemoveFirst();
            }
        }

        Show(entry);

        return true;
    }

    public bool NavigateFresh(string pageKey, object? parameter = null)
    {
        NavTrace.Write($"NavigateFresh {pageKey} from {_current?.Key} back={_back.Count}");

        if (Create(pageKey, parameter) is not { } entry)
        {
            return false;
        }

        foreach (var old in _back)
        {
            old.Scope.Dispose();
        }

        _back.Clear();
        _current?.Scope.Dispose();
        _current = null;
        Show(entry);

        return true;
    }

    public void GoBack()
    {
        if (_back.Last is not { } last)
        {
            return;
        }

        _back.RemoveLast();
        _current?.Scope.Dispose();
        _current = null;
        Show(last.Value);
    }

    private Entry? Create(string pageKey, object? parameter)
    {
        if (_frame is null || !_pages.TryGetValue(pageKey, out var type))
        {
            return null;
        }

        var scope = _scopes.CreateScope();
        var page = (Page)scope.ServiceProvider.GetRequiredService(type);
        (page as IHasViewModel)?.ViewModel.OnNavigatedTo(parameter);

        return new Entry(pageKey, type, page, scope);
    }

    private void Show(Entry entry)
    {
        _current = entry;
        _frame!.Content = entry.Page;
        Navigated?.Invoke(this, new NavigationEvent(entry.Key, entry.Type));
    }

    private sealed record Entry(string Key, Type Type, Page Page, IServiceScope Scope);
}
