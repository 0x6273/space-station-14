using Content.Client.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Client.Chemistry.EntitySystems;

[UsedImplicitly]
public class SolutionTransferSystem : SharedSolutionTransferSystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<SolutionTransferComponent, AfterInteractEvent>(OnAfterInteract);
        //SubscribeLocalEvent<SolutionTransferComponent, TransferAmountSetValueMessage>(OnTransferAmountSetValueMessage);
    }

    private void OnAfterInteract(EntityUid uid, SolutionTransferComponent component, AfterInteractEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!args.CanReach || args.Target == null)
            return;

        var target = args.Target!.Value;

        // if target is refillable, and owner is drainable
        if (!component.CanSend
            || !TryComp<SolutionContainerManagerComponent>(uid, out var sourceSolution)
            || !TryComp<SolutionContainerManagerComponent>(target, out var targetSolution)
            || !TryComp<DrainableSolutionComponent>(target, out var drainable)
            || !TryComp<RefillableSolutionComponent>(target, out var refillable)
            || sourceSolution.PredictedSolution != drainable.Solution
            || targetSolution.PredictedSolution != refillable.Solution)
            return;

        var transferAmount = FixedPoint2.Min(component.TransferAmount, refillable.MaxRefill.GetValueOrDefault(FixedPoint2.MaxValue));

        var transferred = Transfer(uid, target, sourceSolution, targetSolution, transferAmount, args.User);

        if (transferred > 0)
        {
            var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", target));
            _popupSystem.PopupEntity(message, uid, args.User);

            args.Handled = true;
        }
    }

    private FixedPoint2 Transfer(EntityUid sourceUid, EntityUid targetUid, SolutionContainerManagerComponent sourceSolution, SolutionContainerManagerComponent targetSolution, FixedPoint2 quantity, EntityUid? user)
    {
        var attemptEvent = new SolutionTransferAttemptEvent(sourceUid, targetUid);

        RaiseLocalEvent(sourceUid, attemptEvent, broadcast: true);
        if (attemptEvent.Cancelled)
        {
            if (user is not null)
                _popupSystem.PopupEntity(attemptEvent.CancelReason!, sourceUid, user.Value);
            return FixedPoint2.Zero;
        }

        if (sourceSolution.Volume == 0)
        {
            if (user is not null)
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceUid)), sourceUid, user.Value);
            return FixedPoint2.Zero;
        }

        RaiseLocalEvent(targetUid, attemptEvent, broadcast: true);
        if (attemptEvent.Cancelled)
        {
            if (user is not null)
                _popupSystem.PopupEntity(attemptEvent.CancelReason!, sourceUid, user.Value);
            return FixedPoint2.Zero;
        }

        if (targetSolution.Volume >= targetSolution.MaxVolume)
        {
            if (user is not null)
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-full", ("target", targetUid)), targetUid, user.Value);
            return FixedPoint2.Zero;
        }

        var actualQuantity = FixedPoint2.Min(quantity, sourceSolution.Volume, targetSolution.MaxVolume - targetSolution.Volume);
        _solutionContainerSystem.TryTransferSolution(sourceUid, targetUid, sourceSolution.PredictedSolution!, targetSolution.PredictedSolution!, actualQuantity);
        return actualQuantity;
    }
}
