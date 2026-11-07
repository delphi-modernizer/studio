using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;

/// <summary>Scans a Delphi tree before it is copied, so the person can see they picked the right folder.</summary>
/// <remarks>The scan only reads; nothing is written until the workspace is created.</remarks>
public sealed record PreviewLegacyQuery(string Folder, int CodePage) : IRequest<Result<LegacyPreview>>;

/// <param name="ProjectNames">The names the project files give, each once.</param>
/// <param name="DelphiVersion">
/// The first version a project file suggests, with that file's name; null when none does.
/// </param>
/// <param name="Cycles">The circular dependencies, each as the units in it.</param>
/// <param name="Credentials">Connections with a password written into them; the studio shows them only as ***.</param>
public sealed record LegacyPreview(
    InventorySummary Summary,
    IReadOnlyList<string> ProjectNames,
    DelphiVersionSource? DelphiVersion,
    IReadOnlyList<NamedCount> DatabaseAccess,
    IReadOnlyList<NamedCount> ThirdParty,
    IReadOnlyList<IReadOnlyList<string>> Cycles,
    int Credentials);

/// <param name="File">The project file the version is read from, such as ErpApp.dof.</param>
public sealed record DelphiVersionSource(string Version, string File);

internal sealed class PreviewLegacyQueryHandler : IRequestHandler<PreviewLegacyQuery, Result<LegacyPreview>>
{
    public async Task<Result<LegacyPreview>> Handle(PreviewLegacyQuery request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.Folder))
        {
            return Result.Failure<LegacyPreview>(WorkspacesErrors.FolderMissing(request.Folder));
        }

        if (!LegacyFiles.ContainsDelphiSource(request.Folder))
        {
            return Result.Failure<LegacyPreview>(WorkspaceErrors.LegacySourceMissing(request.Folder));
        }

        var options = new ScanOptions(request.CodePage);
        var inventory = await Task.Run(
            () => LegacyScanner.Scan(request.Folder, options, cancellationToken),
            cancellationToken);
        var versioned = inventory.Projects.FirstOrDefault(p => p.DelphiVersion is not null);

        return new LegacyPreview(
            inventory.Summary,
            [.. inventory.Projects
                .Select(p => p.Name)
                .OfType<string>()
                .Distinct()],
            versioned is null
                ? null
                : new DelphiVersionSource(versioned.DelphiVersion!, Path.GetFileName(versioned.Path)),
            [.. inventory.DatabaseAccess.Select(t => new NamedCount(t.Name, t.Occurrences))],
            [.. inventory.ThirdParty.Select(t => new NamedCount(t.Name, t.Occurrences))],
            [.. inventory.Dependencies.Cycles.Select(c => (IReadOnlyList<string>)[.. c])],
            inventory.Connections.Count(c => c.HasEmbeddedPassword));
    }
}
