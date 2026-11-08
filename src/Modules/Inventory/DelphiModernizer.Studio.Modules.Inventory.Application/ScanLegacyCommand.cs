using System;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>Scans the open workspace's legacy/ and replaces the latest inventory.</summary>
/// <param name="Progress">Told files read of files found while the scan runs; null when nobody shows it.</param>
public sealed record ScanLegacyCommand(IProgress<ScanProgress>? Progress = null) : IRequest<Result<InventoryScan>>;

internal sealed class ScanLegacyCommandHandler(IWorkspaceSession session)
    : IRequestHandler<ScanLegacyCommand, Result<InventoryScan>>
{
    public async Task<Result<InventoryScan>> Handle(ScanLegacyCommand request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<InventoryScan>(WorkspaceRequired.Error);
        }

        var options = new ScanOptions(workspace.Manifest.Legacy.CodePage);
        var inventory = await Task.Run(
            () => LegacyScanner.Scan(workspace.LegacyRoot, options, request.Progress, cancellationToken),
            cancellationToken);
        await InventoryFiles.WriteAsync(workspace, inventory, cancellationToken);

        return new InventoryScan(inventory, InventoryFiles.WrittenAt(workspace));
    }
}
