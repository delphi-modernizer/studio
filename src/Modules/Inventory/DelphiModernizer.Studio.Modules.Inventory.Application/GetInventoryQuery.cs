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
public sealed record GetInventoryQuery : IRequest<Result<LegacyInventory>>;

internal sealed class GetInventoryQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetInventoryQuery, Result<LegacyInventory>>
{
    public async Task<Result<LegacyInventory>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<LegacyInventory>(WorkspaceRequired.Error);
        }

        string path = InventoryFiles.JsonPath(workspace);

        if (!File.Exists(path))
        {
            return Result.Failure<LegacyInventory>(InventoryErrors.NotScanned);
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var inventory = await JsonSerializer.DeserializeAsync(
                stream,
                InventoryJsonContext.Default.LegacyInventory,
                cancellationToken);

            return inventory is null
                ? Result.Failure<LegacyInventory>(InventoryErrors.Unreadable("the file is empty"))
                : inventory;
        }
        catch (JsonException exception)
        {
            return Result.Failure<LegacyInventory>(InventoryErrors.Unreadable(exception.Message));
        }
    }
}
