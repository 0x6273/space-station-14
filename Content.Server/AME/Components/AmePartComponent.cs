using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.AME.Components;

[Access(typeof(AmePartSystem)), RegisterComponent]
public sealed class AmePartComponent : Component
{
    [DataField("unwrapSound")]
    public SoundSpecifier? UnwrapSound;

    [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>), required: true)]
    public string QualityNeeded = default!;

    [DataField("spawnPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string SpawnPrototype = default!;
}
