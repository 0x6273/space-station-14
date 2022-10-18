namespace Content.Server.AME.Components;

[RegisterComponent]
public sealed class AmeShieldingComponent : Component
{
}

[RegisterComponent]
public sealed class AmeCoreComponent : Component
{
    /// <summary>
    /// How instable the core is. Injecting fuel increases instability, and it decreses over time.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Instability;

    /// <summary>
    /// TODO
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float UpdateTimer = 5;
}
