using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.SolutionManager;

[NetworkedComponent]
public abstract class SharedSolutionContainerManagerComponent : Component
{
    [DataField("predictedSolution"), ViewVariables(VVAccess.ReadWrite)]
    public String? PredictedSolution;
}

[Serializable, NetSerializable]
public sealed class SolutionContainerManagerComponentState : ComponentState
{
    public FixedPoint2 Volume;
    public FixedPoint2 MaxVolume;
    public Color Color;
}
