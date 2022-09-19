using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedReagentDispenser
    {
        public const string OutputSlotName = "beakerSlot";
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserSetDispenseAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentDispenserDispenseAmount ReagentDispenserDispenseAmount;

        public ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount amount)
        {
            ReagentDispenserDispenseAmount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserDispenseReagentMessage : BoundUserInterfaceMessage
    {
        public readonly string ReagentId;

        public ReagentDispenserDispenseReagentMessage(string reagentId)
        {
            ReagentId = reagentId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage
    {

    }

    public enum ReagentDispenserDispenseAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U15 = 15,
        U20 = 20,
        U25 = 25,
        U30 = 30,
        U50 = 50,
        U100 = 100,
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer;
        /// <summary>
        /// A list of the reagents which this dispenser can dispense.
        /// </summary>
        public readonly List<string> Inventory;

        public readonly string DispenserName;
        public readonly ReagentDispenserDispenseAmount SelectedDispenseAmount;

        public ReagentDispenserBoundUserInterfaceState(string dispenserName, ContainerInfo? outputContainer, List<string> inventory, ReagentDispenserDispenseAmount selectedDispenseAmount)
        {
            DispenserName = dispenserName;
            OutputContainer = outputContainer;
            Inventory = inventory;
            SelectedDispenseAmount = selectedDispenseAmount;
        }
    }

    [Serializable, NetSerializable]
    public enum ReagentDispenserUiKey
    {
        Key
    }
}
