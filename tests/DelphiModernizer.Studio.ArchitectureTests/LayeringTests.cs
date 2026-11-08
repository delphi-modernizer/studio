using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DelphiModernizer.Studio.ArchitectureTests;

/// <summary>The layering of docs/ARCHITECTURE.md, checked on the project files.</summary>
public sealed class LayeringTests
{
    private static readonly string[] _layers = ["Domain", "Application", "UI"];
    private static readonly string[] _uiPackages = ["Microsoft.WindowsAppSDK", "WinUIEx", "CommunityToolkit.Mvvm"];

    [Fact]
    public void Every_module_has_a_UI_and_only_the_three_layers()
    {
        var modules = ProjectGraph.Source
            .Where(p => p.Module is not null)
            .GroupBy(p => p.Module!);

        Assert.All(modules, module => {
            Assert.Contains(module, p => p.Name.EndsWith(".UI", StringComparison.Ordinal));
            Assert.All(module, p => Assert.Contains(p.Name.Split('.')[^1], _layers));
        });
    }

    [Fact]
    public void Modules_never_reference_one_another()
    {
        var crossings = ProjectGraph.Source
            .Where(p => p.Module is not null)
            .SelectMany(p => p.References
                .Where(r => r.StartsWith("DelphiModernizer.Studio.Modules.", StringComparison.Ordinal)
                    && r.Split('.')[3] != p.Module)
                .Select(r => $"{p.Name} -> {r}"));

        Assert.Empty(crossings);
    }

    [Fact]
    public void Domain_and_Application_projects_know_nothing_of_the_UI()
    {
        var leaks = ProjectGraph.Source
            .Where(p => p.IsApplication
                || p.IsDomain
                || p.Name is "DelphiModernizer.Studio.Application.Abstractions" or "DelphiModernizer.Studio.Mediator")
            .SelectMany(p => p.References
                .Where(r => r.EndsWith(".UI", StringComparison.Ordinal)
                    || r.Contains(".UI.", StringComparison.Ordinal)
                    || r.EndsWith(".App", StringComparison.Ordinal))
                .Concat(p.Packages.Intersect(_uiPackages))
                .Select(r => $"{p.Name} -> {r}"));

        Assert.Empty(leaks);
    }

    [Fact]
    public void Only_Application_projects_and_the_abstractions_use_the_engine_directly()
    {
        var direct = ProjectGraph.Source
            .Where(p => p.EngineReferences.Count > 0
                && !p.IsApplication
                && p.Name != "DelphiModernizer.Studio.Application.Abstractions")
            .Select(p => p.Name);

        Assert.Empty(direct);
    }

    [Fact]
    public void Shared_projects_never_reference_a_module_or_the_App()
    {
        var upward = ProjectGraph.Source
            .Where(p => p.IsShared)
            .SelectMany(p => p.References
                .Where(r => r.StartsWith("DelphiModernizer.Studio.Modules.", StringComparison.Ordinal)
                    || r == "DelphiModernizer.Studio.App")
                .Select(r => $"{p.Name} -> {r}"));

        Assert.Empty(upward);
    }

    [Fact]
    public void Only_the_App_composes_the_module_UIs_and_its_catalog_lists_every_module()
    {
        var composers = ProjectGraph.Source
            .Where(p => p.References.Any(r => r.StartsWith("DelphiModernizer.Studio.Modules.", StringComparison.Ordinal)
                && r.EndsWith(".UI", StringComparison.Ordinal)))
            .Select(p => p.Name);
        var app = ProjectGraph.Source.Single(p => p.Name == "DelphiModernizer.Studio.App");
        var moduleUis = ProjectGraph.Source
            .Where(p => p.IsModuleUi)
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)
            .ToList();
        string catalog = File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(app.Path)!, "Composition", "ModuleCatalog.cs"));

        Assert.Equal(["DelphiModernizer.Studio.App"], composers);

        var composed = app.References
            .Where(r => r.EndsWith(".UI", StringComparison.Ordinal)
                && r.Contains(".Modules.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        Assert.Equal(moduleUis, composed);
        Assert.All(
            moduleUis,
            ui => Assert.Contains($"new {ui.Split('.')[3]}Module()", catalog, StringComparison.Ordinal));
    }

    [Fact]
    public void The_solution_lists_every_project()
    {
        string solution = File.ReadAllText(Path.Combine(ProjectGraph.Root, "DelphiModernizer.Studio.slnx"));
        var missing = ProjectGraph.Source
            .Concat(ProjectGraph.Tests)
            .Where(p => !solution.Contains(Relative(p.Path), StringComparison.Ordinal))
            .Select(p => p.Name);

        Assert.Empty(missing);
    }

    private static string Relative(string path)
    {
        return Path
            .GetRelativePath(ProjectGraph.Root, path)
            .Replace('\\', '/');
    }
}
