using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>The base of every view model: change notification plus the navigation and disposal lifecycle.</summary>
public abstract class ViewModel : ObservableObject, IDisposable
{
    private bool _disposed;

    /// <summary>Called before the page loads, with the parameter the navigation passed.</summary>
    public virtual void OnNavigatedTo(object? parameter)
    {
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
