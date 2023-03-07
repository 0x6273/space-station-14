using Content.Server.Administration.Logs;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class SolutionTransferSystem : SharedSolutionTransferSystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        /// <summary>
        ///     Default transfer amounts for the set-transfer verb.
        /// </summary>
        public static readonly List<int> DefaultTransferAmounts = new() { 1, 5, 10, 25, 50, 100, 250, 500, 1000};

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionTransferComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
            SubscribeLocalEvent<SolutionTransferComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<SolutionTransferComponent, TransferAmountSetValueMessage>(OnTransferAmountSetValueMessage);
        }

        private void OnTransferAmountSetValueMessage(EntityUid uid, SolutionTransferComponent solutionTransfer, TransferAmountSetValueMessage message)
        {
            var newTransferAmount = FixedPoint2.Clamp(message.Value, solutionTransfer.MinimumTransferAmount, solutionTransfer.MaximumTransferAmount);
            solutionTransfer.TransferAmount = newTransferAmount;

            if (message.Session.AttachedEntity is {Valid: true} user)
                _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", newTransferAmount)), uid, user);
        }

        private void AddSetTransferVerbs(EntityUid uid, SolutionTransferComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.CanChangeTransferAmount || args.Hands == null)
                return;

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            // Custom transfer verb
            AlternativeVerb custom = new();
            custom.Text = Loc.GetString("comp-solution-transfer-verb-custom-amount");
            custom.Category = VerbCategory.SetTransferAmount;
            custom.Act = () => _userInterfaceSystem.TryOpen(args.Target, TransferAmountUiKey.Key, actor.PlayerSession);
            custom.Priority = 1;
            args.Verbs.Add(custom);

            // Add specific transfer verbs according to the container's size
            var priority = 0;
            foreach (var amount in DefaultTransferAmounts)
            {
                if ( amount < component.MinimumTransferAmount.Int() || amount > component.MaximumTransferAmount.Int())
                    continue;

                AlternativeVerb verb = new();
                verb.Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount));
                verb.Category = VerbCategory.SetTransferAmount;
                verb.Act = () =>
                {
                    component.TransferAmount = FixedPoint2.New(amount);
                    _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), uid, args.User);
                };

                // we want to sort by size, not alphabetically by the verb text.
                verb.Priority = priority;
                priority--;

                args.Verbs.Add(verb);
            }
        }

        private void OnAfterInteract(EntityUid uid, SolutionTransferComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            var target = args.Target!.Value;
            if (!TryComp(uid, out SolutionContainerManagerComponent? sourceSolution)
                || !TryComp(target, out SolutionContainerManagerComponent? targetSolution))
                return;

            //Special case for reagent tanks, because normally clicking another container will give solution, not take it.
            if (component.CanReceive  && !HasComp<RefillableSolutionComponent>(target) // target must not be refillable (e.g. Reagent Tanks)
                                      && _solutionContainerSystem.TryGetDrainableSolution(target, out var targetDrain) // target must be drainable
                                      && TryComp(uid, out RefillableSolutionComponent? refillComp)
                                      && _solutionContainerSystem.TryGetRefillableSolution(uid, out var ownerRefill, refillable: refillComp))

            {

                var transferAmount = component.TransferAmount; // This is the player-configurable transfer amount of "uid," not the target reagent tank.

                if (TryComp(uid, out RefillableSolutionComponent? refill) && refill.MaxRefill != null) // uid is the entity receiving solution from target.
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill); // if the receiver has a smaller transfer limit, use that instead
                }

                var transferred = Transfer(args.User, target, targetDrain, uid, ownerRefill, transferAmount, predicted: false);
                if (transferred > 0)
                {
                    var toTheBrim = ownerRefill.AvailableVolume == 0;
                    var msg = toTheBrim
                        ? "comp-solution-transfer-fill-fully"
                        : "comp-solution-transfer-fill-normal";

                    _popupSystem.PopupEntity(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)), uid, args.User);

                    args.Handled = true;
                    return;
                }
            }

            // if target is refillable, and owner is drainable
            if (component.CanSend && _solutionContainerSystem.TryGetRefillableSolution(target, out var targetRefill)
                                  && _solutionContainerSystem.TryGetDrainableSolution(uid, out var ownerDrain))
            {
                var predicted = sourceSolution.PredictedSolution == ownerDrain.Name && targetSolution.PredictedSolution == targetRefill.Name;

                var transferAmount = component.TransferAmount;

                if (EntityManager.TryGetComponent(target, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill);
                }

                var transferred = Transfer(args.User, uid, ownerDrain, target, targetRefill, transferAmount, predicted);

                if (transferred > 0)
                {
                    if (!predicted)
                    {
                        var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", target));
                        _popupSystem.PopupEntity(message, uid, args.User);
                    }

                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Transfer from a solution to another.
        /// </summary>
        /// <returns>The actual amount transferred.</returns>
        public FixedPoint2 Transfer(
            EntityUid user,
            EntityUid sourceEntity,
            Solution source,
            EntityUid targetEntity,
            Solution target,
            FixedPoint2 amount,
            bool predicted)
        {
            var transferAttempt = new SolutionTransferAttemptEvent(sourceEntity, targetEntity);

            // Check if the source is cancelling the transfer
            RaiseLocalEvent(sourceEntity, transferAttempt, broadcast: true);
            if (transferAttempt.Cancelled)
            {
                if (!predicted)
                    _popupSystem.PopupEntity(transferAttempt.CancelReason!, sourceEntity, user);
                return FixedPoint2.Zero;
            }

            if (source.Volume == 0)
            {
                if (!predicted)
                    _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)), sourceEntity, user);
                return FixedPoint2.Zero;
            }

            // Check if the target is cancelling the transfer
            RaiseLocalEvent(targetEntity, transferAttempt, broadcast: true);
            if (transferAttempt.Cancelled)
            {
                if (!predicted)
                    _popupSystem.PopupEntity(transferAttempt.CancelReason!, sourceEntity, user);
                return FixedPoint2.Zero;
            }

            if (target.AvailableVolume == 0)
            {
                if (!predicted)
                    _popupSystem.PopupEntity(Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)), targetEntity, user);
                return FixedPoint2.Zero;
            }

            var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(source.Volume, target.AvailableVolume));

            var solution = _solutionContainerSystem.Drain(sourceEntity, source, actualAmount);
            _solutionContainerSystem.Refill(targetEntity, target, solution);

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{EntityManager.ToPrettyString(user):player} transferred {string.Join(", ", solution.Contents)} to {EntityManager.ToPrettyString(targetEntity):entity}, which now contains {string.Join(", ", target.Contents)}");

            return actualAmount;
        }
    }
}
