using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This event alerts system that the solution was changed
/// </summary>
public sealed class SolutionChangedEvent : EntityEventArgs
{
    public readonly Solution Solution;

    public SolutionChangedEvent(Solution solution)
    {
        Solution = solution;
    }
}

/// <summary>
/// Part of Chemistry system deal with SolutionContainers
/// </summary>
public abstract partial class SharedSolutionContainerSystem : EntitySystem
{
    //TODO: SolutionContainerManagerComponent optional parameters
    public abstract bool TryTransferSolution(EntityUid sourceUid, EntityUid targetUid, String source, String target, FixedPoint2 quantity);
}
