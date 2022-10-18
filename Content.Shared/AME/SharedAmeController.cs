using Robust.Shared.Serialization;

namespace Content.Shared.AME
{
    public sealed class SharedAmeController
    {
        public const string FuelSlotName = "fuel_container";
    }

    [Serializable, NetSerializable]
    public sealed class AmeControllerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly bool IsInjecting;
        public readonly uint? FuelAmount;
        public readonly uint InjectionAmount;
        public readonly uint CoreCount;

        public AmeControllerBoundUserInterfaceState(bool isInjecting, uint? fuelAmount, uint injectionAmount, uint coreCount)
        {
            IsInjecting = isInjecting;
            FuelAmount = fuelAmount;
            InjectionAmount = injectionAmount;
            CoreCount = coreCount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ToggleInjectionMessage : BoundUserInterfaceMessage {}

    [Serializable, NetSerializable]
    public sealed class IncreaseInjectionAmountMessage : BoundUserInterfaceMessage {}

    [Serializable, NetSerializable]
    public sealed class DecreaseInjectionAmountMessage : BoundUserInterfaceMessage {}

    [Serializable, NetSerializable]
    public enum AmeControllerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum AmeControllerVisualLayer
    {
        DisplayState,
    }

    [Serializable, NetSerializable]
    public enum AmeControllerDisplayState
    {
        Off,
        Normal,
        Critical,
        Fuck,
    }
}
