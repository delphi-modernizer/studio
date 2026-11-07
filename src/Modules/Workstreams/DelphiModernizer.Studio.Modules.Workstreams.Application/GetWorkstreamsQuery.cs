using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// The seven roles with the deliverables each owns and the recipes its agent runs, from the embedded kit.
/// </summary>
public sealed record GetWorkstreamsQuery : IRequest<IReadOnlyList<Workstream>>;

public sealed record Workstream(
    WorkstreamRole Role,
    IReadOnlyList<Deliverable> Deliverables,
    IReadOnlyList<Recipe> Recipes);

internal sealed class GetWorkstreamsQueryHandler : IRequestHandler<GetWorkstreamsQuery, IReadOnlyList<Workstream>>
{
    public Task<IReadOnlyList<Workstream>> Handle(GetWorkstreamsQuery request, CancellationToken cancellationToken)
    {
        var spec = KitBundle.Embedded.Spec;
        IReadOnlyList<Workstream> workstreams = [.. WorkstreamRole.All.Select(role => new Workstream(
            role,
            [.. spec.Deliverables.Deliverables.Where(d => d.Owner == role.Code)],
            [.. spec.Recipes.Recipes.Where(r => r.Agent == role.Agent)]))];

        return Task.FromResult(workstreams);
    }
}
