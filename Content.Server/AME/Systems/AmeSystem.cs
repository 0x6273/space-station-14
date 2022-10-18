using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.AME.Components;
using Content.Server.Destructible;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.AME;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using JetBrains.Annotations;

namespace Content.Server.AME;

[UsedImplicitly]
public sealed class AmeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger= default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, ToggleInjectionMessage>(OnToggleInjectionMessage);
        SubscribeLocalEvent<AmeControllerComponent, IncreaseInjectionAmountMessage>(OnIncreaseInjectionAmountMessage);
        SubscribeLocalEvent<AmeControllerComponent, DecreaseInjectionAmountMessage>(OnDecreaseInjectionAmountMessage);
        SubscribeLocalEvent<AmeControllerComponent, ComponentStartup>((uid, comp, _) => UpdateUiState(uid, comp));
        SubscribeLocalEvent<AmeControllerComponent, EntInsertedIntoContainerMessage>((uid, comp, _) => UpdateUiState(uid, comp));
        SubscribeLocalEvent<AmeControllerComponent, EntRemovedFromContainerMessage>((uid, comp, _) => UpdateUiState(uid, comp));
        SubscribeLocalEvent<AmeControllerComponent, NodeGroupsRebuilt>(
            (EntityUid uid, AmeControllerComponent comp, ref NodeGroupsRebuilt _) => UpdateController(uid, comp));

        SubscribeLocalEvent<AmeShieldingComponent, DamageThresholdReached>(OnShieldingDamageThresholdReached);
        SubscribeLocalEvent<AmeShieldingComponent, NodeGroupsRebuilt>(
            (EntityUid uid, AmeShieldingComponent _, ref NodeGroupsRebuilt _) => UpdateShielding(uid));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Fuel injection
        foreach (var (ame, active) in EntityQuery<AmeControllerComponent, ActiveAmeControllerComponent>())
        {
            var uid = ame.Owner;
            active.InjectionTimer -= frameTime;
            if (active.InjectionTimer > 0)
                continue;

            if (InjectFuel(uid, ame) > 0)
                active.InjectionTimer += (float) ame.InjectionInverval.TotalSeconds;
            else
                StopInjecting(uid, ame);
        }

        // Instability
        foreach (var core in EntityQuery<AmeCoreComponent>())
        {
            var uid = core.Owner;
            core.UpdateTimer -= frameTime;
            if (core.UpdateTimer > 0)
                continue;

            core.UpdateTimer += 5;
            core.Instability -= Math.Min(1.001f, core.Instability);

            if (core.Instability > 10)
            {
                var damage = new DamageSpecifier();
                damage.DamageDict.Add("Blunt", core.Instability);
                _damageableSystem.TryChangeDamage(uid, damage);
            }

            foreach (var controllerUid in GetControllers(uid))
            {
                UpdateControllerVisuals(controllerUid);
            }
        }
    }

    /// <summary>
    /// Inject fuel into the AME cores.
    /// <summary>
    /// <returns>Amount of fuel that was injected.</returns>
    public uint InjectFuel(EntityUid uid, AmeControllerComponent ame)
    {
        // Are we injecting?
        if (ame.InjectionAmount == 0)
            return 0;

        // Does controller have a fuel jar?
        var fuelJar = _itemSlotsSystem.GetItemOrNull(uid, SharedAmeController.FuelSlotName);
        if (fuelJar is null || !TryComp<AmeFuelContainerComponent>(fuelJar.Value, out var fuel))
            return 0;

        // Does it have fuel?
        if (fuel.FuelAmount == 0)
            return 0;

        // Does the AME have at least 1 core?
        var cores = GetCores(uid).ToList();
        if (cores.Count < 1)
            return 0;

        // Decrease fuel in jar by injection amount.
        var injectAmount = Math.Min(ame.InjectionAmount, fuel.FuelAmount);
        fuel.FuelAmount -= injectAmount;

        // Add instability based on how much fuel was injected.
        foreach (var coreUid in cores)
        {
            Comp<AmeCoreComponent>(coreUid).Instability += (float) injectAmount / cores.Count;
        }

        // Update power output.
        if (TryComp<PowerSupplierComponent>(uid, out var powerSupplier))
            powerSupplier.MaxSupply = injectAmount * injectAmount / (float) cores.Count * ame.PowerOutputConstant;

        // Play bang sound with volume based on injection amount.
        var volume = injectAmount > cores.Count * 2 ? 10f : 0f;
        _audioSystem.PlayPvs(ame.InjectSound, uid, AudioParams.Default.WithVolume(volume));

        UpdateUiState(uid, ame);

        return injectAmount;
    }

    public void StartInjecting(EntityUid uid, AmeControllerComponent ame)
    {
        if (ame.InjectionAmount == 0)
            return;

        EnsureComp<ActiveAmeControllerComponent>(uid);
        _itemSlotsSystem.SetLock(uid, SharedAmeController.FuelSlotName, locked: true);
        UpdateControllerVisuals(uid, isOn: true);
        UpdateAllCores(uid);
        UpdateUiState(uid, ame);
    }

    public void StopInjecting(EntityUid uid, AmeControllerComponent ame)
    {
        RemComp<ActiveAmeControllerComponent>(uid);
        _itemSlotsSystem.SetLock(uid, SharedAmeController.FuelSlotName, locked: false);
        UpdateControllerVisuals(uid, isOn: false);
        UpdateAllCores(uid);
        UpdateUiState(uid, ame);
    }

    public void SetInjectionAmount(EntityUid uid, AmeControllerComponent ame, uint injectionAmount)
    {
        ame.InjectionAmount = injectionAmount;
        UpdateUiState(uid, ame);
        if (HasComp<ActiveAmeControllerComponent>(uid))
            UpdateAllCores(uid);
    }

    private void UpdateUiState(EntityUid uid, AmeControllerComponent ame, NodeContainerComponent? nodeContainer = null)
    {
        var isInjecting = HasComp<ActiveAmeControllerComponent>(uid);
        var fuelJar = _itemSlotsSystem.GetItemOrNull(uid, SharedAmeController.FuelSlotName);
        var fuelAmount = CompOrNull<AmeFuelContainerComponent>(fuelJar)?.FuelAmount;
        var coreCount = (uint) GetCores(uid, nodeContainer).Count();

        var state = new AmeControllerBoundUserInterfaceState(isInjecting, fuelAmount, ame.InjectionAmount, coreCount);
        _userInterfaceSystem.TrySetUiState(uid, AmeControllerUiKey.Key, state);
    }

    private void UpdateControllerVisuals(EntityUid uid, bool? isOn = null)
    {
        isOn ??= HasComp<ActiveAmeControllerComponent>(uid);
        if (!isOn.Value)
        {
            _appearanceSystem.SetData(uid, AmeControllerVisualLayer.DisplayState, AmeControllerDisplayState.Off);
            return;
        }

        var cores = GetCores(uid);
        var damage = cores
            .Select(nodeUid => CompOrNull<DamageableComponent>(nodeUid)?.TotalDamage)
            .Max();

        var state = AmeControllerDisplayState.Normal;

        if (damage > 75)
            state = AmeControllerDisplayState.Fuck;
        else
        {
            var instability = cores
                .Select(nodeUid => Comp<AmeCoreComponent>(nodeUid).Instability)
                .Max();

            if (instability > 10)
                state = AmeControllerDisplayState.Critical;
        }

        _appearanceSystem.SetData(uid, AmeControllerVisualLayer.DisplayState, state);
    }

    private void UpdateShielding(EntityUid uid, NodeContainerComponent? nodeContainer = null, PointLightComponent? pointLight = null)
    {
        var tf = Transform(uid);
        var isCore = (_mapManager.TryGetGrid(tf.GridUid, out var grid) ? grid : null)?
            .GetCellsInSquareArea(tf.Coordinates, 1)?
            .Where(e => uid != e)?
            .Where(e => HasComp<AmeShieldingComponent>(e))?
            .Count() >= 8;

        if (isCore)
            EnsureComp<AmeCoreComponent>(uid);
        else
            RemCompDeferred<AmeCoreComponent>(uid);

        _appearanceSystem.SetData(uid, AmeShieldingVisualLayer.IsCore, isCore);
        if (Resolve(uid, ref pointLight, logMissing: false))
            pointLight.Enabled = isCore;

        if (!isCore)
        {
            _appearanceSystem.SetData(uid, AmeShieldingVisualLayer.CoreState, AmeCoreState.Off);
            return;
        }

        var coreCount = GetCores(uid, nodeContainer).Count();
        var totalInjectionAmount = GetControllers(uid, nodeContainer)
            .Where(nodeUid => HasComp<ActiveAmeControllerComponent>(nodeUid))
            .Select(nodeUid => CompOrNull<AmeControllerComponent>(nodeUid)?.InjectionAmount)
            .OfType<uint>()
            .Aggregate(0u, (acc, next) => acc + next);

        var injectionStrength = coreCount > 0 ? totalInjectionAmount / (float) coreCount : 0;

        var coreState = injectionStrength switch {
            <= 0 or float.NaN => AmeCoreState.Off,
            <= 2 => AmeCoreState.Weak,
            > 2 => AmeCoreState.Strong,
        };

        _appearanceSystem.SetData(uid, AmeShieldingVisualLayer.CoreState, coreState);
    }

    /// <summary>
    /// Update both UI and appearance.
    /// </summary>
    private void UpdateController(EntityUid uid, AmeControllerComponent ame, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref nodeContainer, logMissing: false))
            return;

        UpdateUiState(uid, ame, nodeContainer);
    }

    private void UpdateAllCores(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref nodeContainer, logMissing: false))
            return;

        foreach (var coreUid in GetCores(uid, nodeContainer))
            UpdateShielding(coreUid);
    }

    private void BoomBoomBoomBoom(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        var cores = GetCores(uid, nodeContainer).ToList();
        if (cores.Count < 1)
            return;

        var randomCoreUid = _random.Pick(cores);
        var radius = cores.Count * 8;
        var intensity = _explosionSystem.RadiusToIntensity(radius, 5, 60);

        if (!HasComp<TransformComponent>(randomCoreUid))
            return;

        _explosionSystem.QueueExplosion(
            randomCoreUid,
            typeId: "Default",
            totalIntensity: intensity,
            slope: 5,
            maxTileIntensity: 60,
            addLog: true
        );
    }

    private void OnShieldingDamageThresholdReached(EntityUid uid, AmeShieldingComponent _, DamageThresholdReached args)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer))
            return;

        // If the shielding is destroyed while the AME is on, explode it.
        if (GetControllers(uid, nodeContainer).Any(nodeUid => HasComp<ActiveAmeControllerComponent>(nodeUid)))
            BoomBoomBoomBoom(uid);
    }

    private void OnToggleInjectionMessage(EntityUid uid, AmeControllerComponent ame, ToggleInjectionMessage args)
    {
        var wasInjecting = HasComp<ActiveAmeControllerComponent>(uid);
        if (wasInjecting)
            StopInjecting(uid, ame);
        else
            StartInjecting(uid, ame);

        var player = args.Session.AttachedEntity;
        if (player is not null)
            _adminLogger.Add(LogType.Action, LogImpact.Extreme,
                $"{ToPrettyString(player.Value):player} has set the AME to {(wasInjecting ? "Not inject" : "Inject")}");

        ClickSound(uid, ame);
    }

    private void OnIncreaseInjectionAmountMessage(EntityUid uid, AmeControllerComponent ame, IncreaseInjectionAmountMessage args)
    {
        if (ame.InjectionAmount + 2 > ame.MaxInjectionAmount)
            return;

        SetInjectionAmount(uid, ame, ame.InjectionAmount + 2);

        var player = args.Session.AttachedEntity;
        if (player is not null)
        {
            var isInjecting = HasComp<ActiveAmeControllerComponent>(uid);
            _adminLogger.Add(LogType.Action, LogImpact.Extreme,
                $"{ToPrettyString(player.Value):player} has set the AME to inject {ame.InjectionAmount} while set to {(isInjecting ? "Inject" : "Not inject")}");
        }
        ClickSound(uid, ame);
    }

    private void OnDecreaseInjectionAmountMessage(EntityUid uid, AmeControllerComponent ame, DecreaseInjectionAmountMessage args)
    {
        if (ame.InjectionAmount < 2)
            return;

        SetInjectionAmount(uid, ame, ame.InjectionAmount - 2);

        var player = args.Session.AttachedEntity;
        if (player is not null)
        {
            var isInjecting = HasComp<ActiveAmeControllerComponent>(uid);
            _adminLogger.Add(LogType.Action, LogImpact.Extreme,
                $"{ToPrettyString(player.Value):player} has set the AME to inject {ame.InjectionAmount} while set to {(isInjecting ? "Inject" : "Not inject")}");
        }
        ClickSound(uid, ame);
    }

    private IEnumerable<Node> GetNodes(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref nodeContainer, logMissing: false))
            return Enumerable.Empty<Node>();

        return (nodeContainer.TryGetNode("ame", out Node? node) ? node : null)?
            .NodeGroup?
            .Nodes ?? Enumerable.Empty<Node>();
    }

    private IEnumerable<EntityUid> GetControllers(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        return GetNodes(uid, nodeContainer)
            .Select(node => node.Owner)
            .Where(nodeUid => HasComp<AmeControllerComponent>(nodeUid));
    }

    private IEnumerable<EntityUid> GetCores(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        return GetNodes(uid, nodeContainer)
            .Select(node => node.Owner)
            .Where(nodeUid => HasComp<AmeCoreComponent>(nodeUid));
    }

    private void ClickSound(EntityUid uid, AmeControllerComponent ame)
    {
        _audioSystem.PlayPvs(ame.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
    }
}
