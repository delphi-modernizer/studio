using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>Scans the open workspace's legacy/ and replaces the latest inventory.</summary>
public sealed record ScanLegacyCommand : IRequest<Result<LegacyInventory>>;

internal sealed class ScanLegacyCommandHandler(IWorkspaceSession session)
    : IRequestHandler<ScanLegacyCommand, Result<LegacyInventory>>
{
    public async Task<Result<LegacyInventory>> Handle(ScanLegacyCommand request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<LegacyInventory>(WorkspaceRequired.Error);
        }

        var options = new ScanOptions(workspace.Manifest.Legacy.CodePage);
        var inventory = await Task.Run(
            () => LegacyScanner.Scan(workspace.LegacyRoot, options, cancellationToken),
            cancellationToken);
        await InventoryFiles.WriteAsync(workspace, inventory, cancellationToken);

        return inventory;
    }
}
