using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Shared.Actions;

/// <summary>
///     This System handled interactions for the <see cref="ActionOnInteractComponent"/>.
/// </summary>
public sealed class ActionOnInteractSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionOnInteractComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActionOnInteractComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnActivate(EntityUid uid, ActionOnInteractComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || component.ActivateAction is not InstantAction action)
            return;

        if (!ValidAction(action))
            return;

        if (action.Event != null)
            action.Event.Performer = args.User;

        action.Provider = uid;
        _actions.PerformAction(args.User, null, action, action.Event, _timing.CurTime);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, ActionOnInteractComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        // First, try entity target actions
        if (
            args.Target != null
            && component.EntityAction is EntityTargetAction entityAction
            && ValidAction(entityAction, args.CanReach)
            && _actions.ValidateEntityTarget(args.User, args.Target.Value, entityAction)
        )
        {
            if (entityAction.Event != null)
            {
                entityAction.Event.Performer = args.User;
                entityAction.Event.Target = args.Target.Value;
            }

            entityAction.Provider = uid;
            _actions.PerformAction(args.User, null, entityAction, entityAction.Event, _timing.CurTime);
            args.Handled = true;
        }
        // else: try world target actions
        else if (
            component.WorldAction is WorldTargetAction worldAction
            && ValidAction(worldAction, args.CanReach)
            && _actions.ValidateWorldTarget(args.User, args.ClickLocation, worldAction)
        )
        {
            if (worldAction.Event != null)
            {
                worldAction.Event.Performer = args.User;
                worldAction.Event.Target = args.ClickLocation;
            }

            worldAction.Provider = uid;
            _actions.PerformAction(args.User, null, worldAction, worldAction.Event, _timing.CurTime);
            args.Handled = true;
        }
    }

    private bool ValidAction(ActionType act, bool canReach = true)
    {
        if (!act.Enabled)
            return false;

        if (act.Charges.HasValue && act.Charges <= 0)
            return false;

        var curTime = _timing.CurTime;
        if (act.Cooldown.HasValue && act.Cooldown.Value.End > curTime)
            return false;

        return canReach || act is TargetedAction { CheckCanAccess: false };
    }
}
