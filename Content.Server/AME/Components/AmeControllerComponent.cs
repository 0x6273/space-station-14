using Robust.Shared.Audio;

namespace Content.Server.AME.Components;

[Access(typeof(AmeSystem)), RegisterComponent]
public sealed class AmeControllerComponent : Component
{
    /// <summary>
    /// How much fuel is injected each InjectionInterval.
    /// Configured via the UI, and affects how much power is produced.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] //FIXME: VV won't update ui/visuals
    public uint InjectionAmount = 2;


    /// <summary>
    /// The limit to how high you can set the injection amount in the UI.
    /// </summary>
    [DataField("maxInjectionAmount", required: true), ViewVariables(VVAccess.ReadWrite)]
    public uint MaxInjectionAmount;

    /// <summary>
    /// The interval between fuel injections.
    /// </summary>
    [DataField("injectionIntervalSeconds", required: true), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan InjectionInverval;

    /// <summary>
    /// Used with InjectionAmount and core count to calculate power output.
    /// </summary>
    [DataField("powerOutputConstant", required: true), ViewVariables(VVAccess.ReadWrite)]
    public uint PowerOutputConstant;

    [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ClickSound;

    [DataField("injectSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? InjectSound;
}

/// <summary>
/// Component for an AME that is injecting.
/// <summary>
[Access(typeof(AmeSystem)), RegisterComponent]
public sealed class ActiveAmeControllerComponent : Component
{
    /// <summary>
    /// When it hits 0, fuel is injected and it is reset to InjectionInverval.
    /// </summary>
    public float InjectionTimer;
}
