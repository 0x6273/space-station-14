using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerManagerComponent))]
    [Access(typeof(SolutionContainerSystem))]
    public sealed class SolutionContainerManagerComponent : SharedSolutionContainerManagerComponent
    {
        [DataField("solutions")]
        [Access(typeof(SolutionContainerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
