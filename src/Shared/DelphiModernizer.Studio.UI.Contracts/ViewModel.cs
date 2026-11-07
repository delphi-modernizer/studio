using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>The base of every view model: change notification plus the navigation and disposal lifecycle.</summary>
public abstract class ViewModel : ObservableObject, IDisposable
{
    /// <summary>Called before the page loads, with the parameter the navigation passed.</summary>
    public virtual void OnNavigatedTo(object? parameter)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases what a derived view model holds; the scope that owns it calls this once.</summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
