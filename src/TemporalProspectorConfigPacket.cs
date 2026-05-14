using ProtoBuf;

namespace TemporalProspector
{
    [ProtoContract]
    public class TemporalProspectorConfigPacket
    {
        [ProtoMember(1)] public int MaxParticleTargets { get; set; }
        [ProtoMember(2)] public bool EnableChalkRecipe { get; set; }
        [ProtoMember(3)] public bool EnableHaliteRecipe { get; set; }
        [ProtoMember(4)] public bool EnableLimestoneRecipe { get; set; }
        [ProtoMember(5)] public int CopperDurability { get; set; }
        [ProtoMember(6)] public int TinBronzeDurability { get; set; }
        [ProtoMember(7)] public int BismuthBronzeDurability { get; set; }
        [ProtoMember(8)] public int BlackBronzeDurability { get; set; }
        [ProtoMember(9)] public int GoldDurability { get; set; }
        [ProtoMember(10)] public int SilverDurability { get; set; }
        [ProtoMember(11)] public int IronDurability { get; set; }
        [ProtoMember(12)] public int MeteoricIronDurability { get; set; }
        [ProtoMember(13)] public int SteelDurability { get; set; }
        [ProtoMember(14)] public int ShortSearchRadius { get; set; }
        [ProtoMember(15)] public int MediumSearchRadius { get; set; }
        [ProtoMember(16)] public int LongSearchRadius { get; set; }
        [ProtoMember(17)] public int ExtraLongSearchRadius { get; set; }
        [ProtoMember(18)] public double DurabilityCostPercent { get; set; }
    }
}
