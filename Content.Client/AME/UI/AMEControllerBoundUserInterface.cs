using Content.Shared.AME;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.AME.UI
{
    [UsedImplicitly]
    public sealed class AmeControllerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private AmeControllerWindow? _window;

        public AmeControllerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new(this);

            _window.OnClose += Close;
            _window.OpenCentered();

            _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(SharedAmeController.FuelSlotName));
            _window.ToggleInjectionButton.OnPressed += _ => SendMessage(new ToggleInjectionMessage());
            _window.IncreaseFuelButton.OnPressed += _ => SendMessage(new IncreaseInjectionAmountMessage());
            _window.DecreaseFuelButton.OnPressed += _ => SendMessage(new DecreaseInjectionAmountMessage());
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="AmeControllerComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (AmeControllerBoundUserInterfaceState) state;
            _window?.UpdateState(castState); //Update window state
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
