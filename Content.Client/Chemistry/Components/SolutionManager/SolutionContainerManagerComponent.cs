using Content.Client.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;

namespace Content.Client.Chemistry.Components.SolutionManager;

[RegisterComponent, ComponentReference(typeof(SharedSolutionContainerManagerComponent)), Access(typeof(SolutionContainerSystem))]
public sealed class SolutionContainerManagerComponent : SharedSolutionContainerManagerComponent
{
    [DataField("volume"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Volume;

    [DataField("maxVolume"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxVolume;

    [DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    public Color Color;
}
