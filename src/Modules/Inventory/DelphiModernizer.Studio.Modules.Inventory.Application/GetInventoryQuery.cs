using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>Reads the open workspace's latest inventory without scanning again.</summary>
public sealed record GetInventoryQuery : IRequest<Result<InventoryScan>>;

/// <summary>The latest inventory and when its file was written.</summary>
public sealed record InventoryScan(LegacyInventory Inventory, DateTimeOffset ScannedAt);

internal sealed class GetInventoryQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetInventoryQuery, Result<InventoryScan>>
{
    public async Task<Result<InventoryScan>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<InventoryScan>(WorkspaceRequired.Error);
        }

        string path = InventoryFiles.JsonPath(workspace);

        if (!File.Exists(path))
        {
            return Result.Failure<InventoryScan>(InventoryErrors.NotScanned);
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var inventory = await JsonSerializer.DeserializeAsync(
                stream,
                InventoryJsonContext.Default.LegacyInventory,
                cancellationToken);

            return inventory is null
                ? Result.Failure<InventoryScan>(InventoryErrors.Unreadable("the file is empty"))
                : new InventoryScan(inventory, InventoryFiles.WrittenAt(workspace));
        }
        catch (JsonException exception)
        {
            return Result.Failure<InventoryScan>(InventoryErrors.Unreadable(exception.Message));
        }
    }
}
