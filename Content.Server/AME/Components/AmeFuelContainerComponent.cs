namespace Content.Server.AME.Components
{
    [Access(typeof(AmeSystem)), RegisterComponent]
    public sealed class AmeFuelContainerComponent : Component
    {
        /// <summary>
        /// The amount of fuel in the jar.
        /// </summary>
        [DataField("fuelAmount", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint FuelAmount;

        /// <summary>
        /// The maximum fuel capacity of the jar.
        /// </summary>
        [DataField("maxFuelAmount", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint MaxFuelAmount;
    }
}
