using Robust.Shared.Serialization;

namespace Content.Shared.AME
{
    [Serializable, NetSerializable]
    public enum AmeShieldingVisualLayer
    {
        IsCore,
        CoreState
    }

    [Serializable, NetSerializable]
    public enum AmeCoreState
    {
        Off,
        Weak,
        Strong
    }
}
