using System;
using System.IO;
using System.Linq;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Tests;

public sealed class RecentWorkspaceStoreTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dmz-studio-").FullName;

    [Fact]
    public void The_newest_comes_first_and_a_reopened_folder_moves_up_once()
    {
        var store = new RecentWorkspaceStore(Path.Combine(_folder, "recent.json"));

        store.Remember(Recent("A", @"C:\work\a"));
        store.Remember(Recent("B", @"C:\work\b"));
        store.Remember(Recent("A again", @"C:\WORK\A"));

        var names = store
            .Load()
            .Select(w => w.Name);

        Assert.Equal(["A again", "B"], names);
    }

    [Fact]
    public void The_list_keeps_the_ten_newest()
    {
        var store = new RecentWorkspaceStore(Path.Combine(_folder, "recent.json"));

        foreach (var index in Enumerable.Range(1, 12))
        {
            store.Remember(Recent($"W{index}", $"/work/{index}"));
        }

        Assert.Equal(10, store.Load().Count);
        Assert.Equal("W12", store.Load()[0].Name);
    }

    [Fact]
    public void A_damaged_file_reads_as_an_empty_list()
    {
        var path = Path.Combine(_folder, "recent.json");
        File.WriteAllText(path, "{ not json");

        Assert.Empty(new RecentWorkspaceStore(path).Load());
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    private static RecentWorkspace Recent(string name, string root)
    {
        return new RecentWorkspace(name, root, "dotnet", DateTimeOffset.UnixEpoch);
    }
}
