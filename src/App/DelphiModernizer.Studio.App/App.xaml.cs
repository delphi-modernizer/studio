using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.App.Hosting;
using DelphiModernizer.Studio.App.UI;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace DelphiModernizer.Studio.App;

/// <summary>
/// The composition root: builds the services, opens the main window with the shell and logs what escapes.
/// </summary>
/// <remarks>
/// The base type is spelled out because the <c>DelphiModernizer.Studio.Application</c> namespace shadows it.
/// </remarks>
public sealed partial class App : Microsoft.UI.Xaml.Application
{
    private ServiceProvider? _services;

    public App()
    {
        InitializeComponent();

        // No text backplates in high contrast: every brush has its own high-contrast value, and a backplate draws
        // on-accent text white on black. Set in code: the same value as an App.xaml attribute left the backplates.
        HighContrastAdjustment = ApplicationHighContrastAdjustment.None;
        UnhandledException += OnUnhandledException;

        // XAML's handler misses faulted fire-and-forget tasks and exceptions off the UI thread.
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _services = AppHost.Build();
        ContractServices.Configure(_services);

        var window = _services.GetRequiredService<MainWindow>();
        window.AttachShell(_services.GetRequiredService<ShellPage>());
        _services
            .GetRequiredService<IThemeService>()
            .Initialize();
        window.Activate();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log("Unhandled exception.", e.Exception);
    }

    /// <remarks>SetObserved keeps the exception from ending the process later, from the finalizer thread.</remarks>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }

    private void Log(string message, Exception? exception)
    {
        _services?
            .GetService<ILogger<App>>()?
            .LogError(exception, "{Message}", message);
    }
}
