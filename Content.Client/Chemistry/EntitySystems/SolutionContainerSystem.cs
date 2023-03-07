using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using JetBrains.Annotations;

namespace Content.Client.Chemistry.EntitySystems;

[UsedImplicitly]
public sealed partial class SolutionContainerSystem : SharedSolutionContainerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictedSolutionComponent, ComponentHandleState>(HandleCompState);
    }

    public override bool TryTransferSolution(
        EntityUid sourceUid, EntityUid targetUid, String source, String target, FixedPoint2 quantity,
        PredictedSolutionComponent? sourceSolution = null, PredictedSolutionComponent? targetSolution = null)
    {
        if (quantity < 0)
            return TryTransferSolution(targetUid, sourceUid, source: target, target: source, -quantity, sourceSolution, targetSolution);

        if (!Resolve(sourceUid, ref sourceSolution)
            || sourceSolution.Solution != source
            || !Resolve(targetUid, ref targetSolution)
            || targetSolution.Solution != target)
            return false;

        var targetSolutionAvailableVolume = targetSolution.MaxVolume - targetSolution.Volume;
        quantity = FixedPoint2.Min(quantity, targetSolutionAvailableVolume, sourceSolution.Volume);
        if (quantity == 0)
            return false;

        targetSolution.Color = Color.InterpolateBetween(sourceSolution.Color, targetSolution.Color, quantity.Float() / targetSolution.Volume.Float()); //TODO verify this works
        targetSolution.Volume += quantity;
        sourceSolution.Volume -= quantity;

        return true;
    }

    private void HandleCompState(EntityUid uid, PredictedSolutionComponent predictedSolution, ref ComponentHandleState args)
    {
        if (args.Current is not PredictedSolutionComponentState state)
            return;

        predictedSolution.Volume = state.Volume;
        predictedSolution.MaxVolume = state.MaxVolume;
        predictedSolution.Color = state.Color;
    }
}
