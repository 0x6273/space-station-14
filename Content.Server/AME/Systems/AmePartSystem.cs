using System.Linq;
using Content.Server.AME.Components;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using JetBrains.Annotations;

namespace Content.Server.AME;

[UsedImplicitly]
public sealed class AmePartSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmePartComponent, InteractUsingEvent>(OnPartInteractUsing);
    }

    private void OnPartInteractUsing(EntityUid uid, AmePartComponent amePart, InteractUsingEvent args)
    {
        if (!HasComp<HandsComponent>(args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-part-component-interact-using-no-hands"), uid, Filter.Entities(args.User));
            return;
        }

        if (!_toolSystem.HasQuality(args.Used, amePart.QualityNeeded))
            return;

        if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridUid(EntityManager), out var mapGrid))
            return; // No AME in space.

        var snapPos = mapGrid.TileIndicesFor(args.ClickLocation);
        if (mapGrid.GetAnchoredEntities(snapPos).Any(sc => HasComp<AmeShieldingComponent>(sc)))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-part-component-shielding-already-present"), uid, Filter.Entities(args.User));
            return;
        }

        var ent = EntityManager.SpawnEntity(amePart.SpawnPrototype, mapGrid.GridTileToLocal(snapPos));

        _audioSystem.PlayPvs(amePart.UnwrapSound, uid);

        EntityManager.QueueDeleteEntity(uid);
    }
}
