using System;
using Vintagestory.API.Common;

namespace TemporalProspector
{
    public class TemporalProspectorConfig
    {
        public const string FileName = "TemporalProspectorConfig.json";
        public const int DefaultMaxParticleTargets = 128;
        public const int MinMaxParticleTargets = 0;
        public const int MaxMaxParticleTargets = 512;
        public const int MinSearchRadius = 1;
        public const int MaxSearchRadius = 512;
        public const int DefaultShortSearchRadius = 15;
        public const int DefaultMediumSearchRadius = 30;
        public const int DefaultLongSearchRadius = 60;
        public const int DefaultExtraLongSearchRadius = 90;
        public const double MinDurabilityCostPercent = 0d;
        public const double MaxDurabilityCostPercent = 1000d;
        public const double DefaultDurabilityCostPercent = 33;
        public const int MinDurability = 1;
        public const int MaxDurability = 100000;
        public const int DefaultCopperDurability = 150;
        public const int DefaultTinBronzeDurability = 250;
        public const int DefaultBismuthBronzeDurability = 300;
        public const int DefaultBlackBronzeDurability = 350;
        public const int DefaultGoldDurability = 60;
        public const int DefaultSilverDurability = 80;
        public const int DefaultIronDurability = 650;
        public const int DefaultMeteoricIronDurability = 900;
        public const int DefaultSteelDurability = 1625;

        public int MaxParticleTargets { get; set; } = DefaultMaxParticleTargets;
        public int ShortSearchRadius { get; set; } = DefaultShortSearchRadius;
        public int MediumSearchRadius { get; set; } = DefaultMediumSearchRadius;
        public int LongSearchRadius { get; set; } = DefaultLongSearchRadius;
        public int ExtraLongSearchRadius { get; set; } = DefaultExtraLongSearchRadius;
        public double DurabilityCostPercent { get; set; } = DefaultDurabilityCostPercent;
        public bool EnableChalkRecipe { get; set; } = true;
        public bool EnableHaliteRecipe { get; set; } = true;
        public bool EnableLimestoneRecipe { get; set; } = true;
        public int CopperDurability { get; set; } = DefaultCopperDurability;
        public int TinBronzeDurability { get; set; } = DefaultTinBronzeDurability;
        public int BismuthBronzeDurability { get; set; } = DefaultBismuthBronzeDurability;
        public int BlackBronzeDurability { get; set; } = DefaultBlackBronzeDurability;
        public int GoldDurability { get; set; } = DefaultGoldDurability;
        public int SilverDurability { get; set; } = DefaultSilverDurability;
        public int IronDurability { get; set; } = DefaultIronDurability;
        public int MeteoricIronDurability { get; set; } = DefaultMeteoricIronDurability;
        public int SteelDurability { get; set; } = DefaultSteelDurability;

        public static TemporalProspectorConfig CreateDefault()
        {
            return new TemporalProspectorConfig().Normalize();
        }

        public static TemporalProspectorConfig LoadLocal(ICoreAPI api)
        {
            TemporalProspectorConfig config =
                (api.LoadModConfig<TemporalProspectorConfig>(FileName) ?? new TemporalProspectorConfig()).Normalize();
            api.StoreModConfig(config, FileName);
            return config;
        }

        public static int ClampDurability(int value)
        {
            return Math.Clamp(value, MinDurability, MaxDurability);
        }

        public static double ClampDurabilityCostPercent(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return DefaultDurabilityCostPercent;
            }

            return Math.Clamp(value, MinDurabilityCostPercent, MaxDurabilityCostPercent);
        }

        public static int ClampSearchRadius(int value)
        {
            return Math.Clamp(value, MinSearchRadius, MaxSearchRadius);
        }

        public TemporalProspectorConfig Normalize()
        {
            MaxParticleTargets = Math.Clamp(MaxParticleTargets, MinMaxParticleTargets, MaxMaxParticleTargets);
            ShortSearchRadius = ClampSearchRadius(ShortSearchRadius);
            MediumSearchRadius = ClampSearchRadius(MediumSearchRadius);
            LongSearchRadius = ClampSearchRadius(LongSearchRadius);
            ExtraLongSearchRadius = ClampSearchRadius(ExtraLongSearchRadius);
            DurabilityCostPercent = ClampDurabilityCostPercent(DurabilityCostPercent);
            CopperDurability = ClampDurability(CopperDurability);
            TinBronzeDurability = ClampDurability(TinBronzeDurability);
            BismuthBronzeDurability = ClampDurability(BismuthBronzeDurability);
            BlackBronzeDurability = ClampDurability(BlackBronzeDurability);
            GoldDurability = ClampDurability(GoldDurability);
            SilverDurability = ClampDurability(SilverDurability);
            IronDurability = ClampDurability(IronDurability);
            MeteoricIronDurability = ClampDurability(MeteoricIronDurability);
            SteelDurability = ClampDurability(SteelDurability);
            return this;
        }

        public TemporalProspectorConfig Clone()
        {
            return new TemporalProspectorConfig
            {
                MaxParticleTargets = MaxParticleTargets,
                ShortSearchRadius = ShortSearchRadius,
                MediumSearchRadius = MediumSearchRadius,
                LongSearchRadius = LongSearchRadius,
                ExtraLongSearchRadius = ExtraLongSearchRadius,
                DurabilityCostPercent = DurabilityCostPercent,
                EnableChalkRecipe = EnableChalkRecipe,
                EnableHaliteRecipe = EnableHaliteRecipe,
                EnableLimestoneRecipe = EnableLimestoneRecipe,
                CopperDurability = CopperDurability,
                TinBronzeDurability = TinBronzeDurability,
                BismuthBronzeDurability = BismuthBronzeDurability,
                BlackBronzeDurability = BlackBronzeDurability,
                GoldDurability = GoldDurability,
                SilverDurability = SilverDurability,
                IronDurability = IronDurability,
                MeteoricIronDurability = MeteoricIronDurability,
                SteelDurability = SteelDurability
            };
        }

        public TemporalProspectorConfigPacket ToPacket()
        {
            return new TemporalProspectorConfigPacket
            {
                MaxParticleTargets = MaxParticleTargets,
                ShortSearchRadius = ShortSearchRadius,
                MediumSearchRadius = MediumSearchRadius,
                LongSearchRadius = LongSearchRadius,
                ExtraLongSearchRadius = ExtraLongSearchRadius,
                DurabilityCostPercent = DurabilityCostPercent,
                EnableChalkRecipe = EnableChalkRecipe,
                EnableHaliteRecipe = EnableHaliteRecipe,
                EnableLimestoneRecipe = EnableLimestoneRecipe,
                CopperDurability = CopperDurability,
                TinBronzeDurability = TinBronzeDurability,
                BismuthBronzeDurability = BismuthBronzeDurability,
                BlackBronzeDurability = BlackBronzeDurability,
                GoldDurability = GoldDurability,
                SilverDurability = SilverDurability,
                IronDurability = IronDurability,
                MeteoricIronDurability = MeteoricIronDurability,
                SteelDurability = SteelDurability
            };
        }

        public static TemporalProspectorConfig FromPacket(TemporalProspectorConfigPacket packet)
        {
            return new TemporalProspectorConfig
            {
                MaxParticleTargets = packet.MaxParticleTargets,
                ShortSearchRadius = packet.ShortSearchRadius,
                MediumSearchRadius = packet.MediumSearchRadius,
                LongSearchRadius = packet.LongSearchRadius,
                ExtraLongSearchRadius = packet.ExtraLongSearchRadius,
                DurabilityCostPercent = packet.DurabilityCostPercent,
                EnableChalkRecipe = packet.EnableChalkRecipe,
                EnableHaliteRecipe = packet.EnableHaliteRecipe,
                EnableLimestoneRecipe = packet.EnableLimestoneRecipe,
                CopperDurability = packet.CopperDurability,
                TinBronzeDurability = packet.TinBronzeDurability,
                BismuthBronzeDurability = packet.BismuthBronzeDurability,
                BlackBronzeDurability = packet.BlackBronzeDurability,
                GoldDurability = packet.GoldDurability,
                SilverDurability = packet.SilverDurability,
                IronDurability = packet.IronDurability,
                MeteoricIronDurability = packet.MeteoricIronDurability,
                SteelDurability = packet.SteelDurability
            }.Normalize();
        }
    }
}
