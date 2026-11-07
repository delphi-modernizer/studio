using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;

/// <summary>Scans a Delphi tree before it is copied, so the person can see they picked the right folder.</summary>
public sealed record PreviewLegacyQuery(string Folder, int CodePage) : IRequest<Result<LegacyPreview>>;

/// <param name="DelphiVersion">The first version the project files suggest, or null.</param>
public sealed record LegacyPreview(
    int Files,
    int Projects,
    int Units,
    int Forms,
    int BinaryForms,
    int CodeLines,
    string? DelphiVersion,
    ImmutableArray<string> DatabaseAccess,
    ImmutableArray<string> ThirdParty,
    int Cycles);

internal sealed class PreviewLegacyQueryHandler : IRequestHandler<PreviewLegacyQuery, Result<LegacyPreview>>
{
    public async Task<Result<LegacyPreview>> Handle(PreviewLegacyQuery request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.Folder))
        {
            return Result.Failure<LegacyPreview>(WorkspacesErrors.FolderMissing(request.Folder));
        }

        var inventory = await Task.Run(() => LegacyScanner.Scan(request.Folder, new ScanOptions(request.CodePage), cancellationToken), cancellationToken);
        var summary = inventory.Summary;

        return new LegacyPreview(
            summary.Files,
            summary.Projects,
            summary.Units,
            summary.Forms,
            summary.BinaryForms,
            summary.CodeLines,
            summary.DelphiVersions.FirstOrDefault(),
            [.. inventory.DatabaseAccess.Select(t => t.Name)],
            [.. inventory.ThirdParty.Select(t => t.Name)],
            inventory.Dependencies.Cycles.Length);
    }
}
