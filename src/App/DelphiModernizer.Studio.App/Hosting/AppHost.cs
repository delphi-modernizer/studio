using System.IO;
using DelphiModernizer.Studio.App.Composition;
using DelphiModernizer.Studio.App.Services;
using DelphiModernizer.Studio.App.UI;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DelphiModernizer.Studio.App.Hosting;

/// <summary>Builds the container: logging, the platform services, the window and shell, and every module.</summary>
internal static class AppHost
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging => logging
            .AddDebug()
            .AddProvider(new FileLoggerProvider(Path.Combine(StudioPaths.DataDirectory, "logs"))));

        services.AddSingleton<IWorkspaceSession, WorkspaceSession>();
        services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        services.AddSingleton<IStudioSettings, StudioSettings>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IEditorLauncher, EditorLauncher>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<ShellPage>();

        foreach (var module in ModuleCatalog.Modules)
        {
            services.AddSingleton(module);
            module.RegisterServices(services);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
