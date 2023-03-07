using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Indicates that one of the entity's solutions should be shared with the client so it can be used for prediction.
/// Solution prediction is opt-in to avoid sending solutions over the network unnecessarily.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PredictedSolutionComponent : Component
{
    /// <summary>
    /// The solution that should be predicted.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public String Solution = default!;

    // The following is set on component init and when the solution in SolutionContainerManagerComponent changes
    [DataField("volume"), ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 Volume;

    [DataField("maxVolume"), ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 MaxVolume;

    [DataField("color"), ViewVariables(VVAccess.ReadOnly)]
    public Color Color;
}

[Serializable, NetSerializable]
public sealed class PredictedSolutionComponentState : ComponentState
{
    public FixedPoint2 Volume;
    public FixedPoint2 MaxVolume;
    public Color Color;
}
