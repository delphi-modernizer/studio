using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>The latest scan's two files, written exactly as <c>dmz scan</c> writes them.</summary>
internal static class InventoryFiles
{
    public static string JsonPath(Workspace workspace)
    {
        return Path.Combine(workspace.LatestScanFolder, "inventory.json");
    }

    public static async Task WriteAsync(Workspace workspace, LegacyInventory inventory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(workspace.LatestScanFolder);
        await File.WriteAllBytesAsync(JsonPath(workspace), JsonSerializer.SerializeToUtf8Bytes(inventory, InventoryJsonContext.Default.LegacyInventory), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workspace.LatestScanFolder, "inventory.md"), InventoryMarkdown.Render(inventory), cancellationToken);
    }
}
