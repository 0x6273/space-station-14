using Content.Client.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components.SolutionManager;
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

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentHandleState>(HandleCompState);
    }

    public override bool TryTransferSolution(EntityUid sourceUid, EntityUid targetUid, String source, String target, FixedPoint2 quantity)
    {
        if (quantity < 0)
            return TryTransferSolution(targetUid, sourceUid, source: target, target: source, -quantity);

        SolutionContainerManagerComponent? sourceSolution = null; //TODO method parameter
        SolutionContainerManagerComponent? targetSolution = null;
        if (!Resolve(sourceUid, ref sourceSolution)
            || sourceSolution.PredictedSolution != source
            || !Resolve(targetUid, ref targetSolution)
            || targetSolution.PredictedSolution != target)
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

    private void HandleCompState(EntityUid uid, SolutionContainerManagerComponent solutionContainer, ref ComponentHandleState args)
    {
        if (args.Current is not SolutionContainerManagerComponentState state)
            return;

        solutionContainer.Volume = state.Volume;
        solutionContainer.MaxVolume = state.MaxVolume;
        solutionContainer.Color = state.Color;
    }
}
