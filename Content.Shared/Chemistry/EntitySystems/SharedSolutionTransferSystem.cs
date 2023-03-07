using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract class SharedSolutionTransferSystem : EntitySystem
{
    public bool CanPredictSend(EntityUid sourceUid, EntityUid targetUid, DrainableSolutionComponent? drainable = null, RefillableSolutionComponent? refillable = null,
        PredictedSolutionComponent? sourcePredictedSolution = null, PredictedSolutionComponent? targetPredictedSolution = null)
    {
        return Resolve(sourceUid, ref drainable)
            && Resolve(sourceUid, ref sourcePredictedSolution)
            && Resolve(targetUid, ref refillable)
            && Resolve(targetUid, ref targetPredictedSolution)
            && drainable.Solution == sourcePredictedSolution.Solution
            && refillable.Solution == targetPredictedSolution.Solution;
    }
}


/// <summary>
/// Raised when attempting to transfer from one solution to another.
/// </summary>
public sealed class SolutionTransferAttemptEvent : CancellableEntityEventArgs
{
    public SolutionTransferAttemptEvent(EntityUid from, EntityUid to)
    {
        From = from;
        To = to;
    }

    public EntityUid From { get; }
    public EntityUid To { get; }

    /// <summary>
    /// Why the transfer has been cancelled.
    /// </summary>
    public string? CancelReason { get; private set; }

    /// <summary>
    /// Cancels the transfer.
    /// </summary>
    public void Cancel(string reason)
    {
        base.Cancel();
        CancelReason = reason;
    }
}
