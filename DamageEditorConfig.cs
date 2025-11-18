using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DamageEditor
{
    public class DamageEditorConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(0)]
        [Range(-10, 10)]
        [Increment(1)]
        public int DealDamage { get; set; }

        [DefaultValue(0)]
        [Range(-10, 10)]
        [Increment(1)]
        public int TakeDamage { get; set; }

        // Moved into a Boss subsection so labels can be provided via localization.
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.Label")]
        public BossSettings Boss { get; set; } = new BossSettings();

        public Dictionary<string, PlayerTuningConfig> PlayerOverrides { get; set; } = new Dictionary<string, PlayerTuningConfig>
        {
            ["playername"] = new PlayerTuningConfig()
        };

        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.Label")]
        public EventsSettings Events { get; set; } = new EventsSettings();

        public class BossSettings
        {
            [DefaultValue(12)]
            [Range(1, 240)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.ExpectedTotalMinutes.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.ExpectedTotalMinutes.Tooltip")]
            public int ExpectedTotalMinutes { get; set; }

            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.TargetHighestHealth.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.TargetHighestHealth.Tooltip")]
            [DefaultValue(false)]
            public bool TargetHighestHealth { get; set; }

            public BossSettings()
            {
                ExpectedTotalMinutes = 12;
                TargetHighestHealth = false;
                WeaponAdaptationEnabled = false;
                WeaponAdaptationStartMultiplier = 2.0f;
                WeaponAdaptationCompleteMultiplier = 3.0f;
                WeaponAdaptationMinDamage = 100f;
                WeaponAdaptationMaxReduction = 0.5f;
            }

            [DefaultValue(false)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptation.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptation.Tooltip")]
            public bool WeaponAdaptationEnabled { get; set; }

            [DefaultValue(2.0f)]
            [Range(1f, 10f)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationStartMultiplier.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationStartMultiplier.Tooltip")]
            public float WeaponAdaptationStartMultiplier { get; set; }

            [DefaultValue(3.0f)]
            [Range(1f, 20f)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationCompleteMultiplier.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationCompleteMultiplier.Tooltip")]
            public float WeaponAdaptationCompleteMultiplier { get; set; }

            [DefaultValue(100f)]
            [Range(0f, 1000000f)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationMinDamage.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationMinDamage.Tooltip")]
            public float WeaponAdaptationMinDamage { get; set; }

            [DefaultValue(0.5f)]
            [Range(0.1f, 1f)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationMaxReduction.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Boss.WeaponAdaptationMaxReduction.Tooltip")]
            public float WeaponAdaptationMaxReduction { get; set; }

            // constructor merged into first occurrence
        }

        public class EventsSettings
        {
            [DefaultValue(210)]
            [Range(1, 600)]
            [Increment(30)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.AvgMinutesBetweenEvents.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.AvgMinutesBetweenEvents.Tooltip")]
            public int AvgMinutesBetweenEvents { get; set; }

            [DefaultValue(5)]
            [Range(1, 30)]
            [Increment(1)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.EventDurationMinutes.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.EventDurationMinutes.Tooltip")]
            public int EventDurationMinutes { get; set; }

            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.BlacklistedBiomes.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.BlacklistedBiomes.Tooltip")]
            public List<string> BlacklistedBiomes { get; set; } = new List<string> { "Forest" };

            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.Label")]
            public FishingHotspotSettings FishingHotspot { get; set; } = new FishingHotspotSettings();
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.Label")]
            public MobTakeoverSettings MobTakeover { get; set; } = new MobTakeoverSettings();

            public class FishingHotspotSettings
            {
                [DefaultValue(25f)]
                [Range(0f, 100f)]
                [Increment(5f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.FishingPowerBonus.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.FishingPowerBonus.Tooltip")]
                public float FishingPowerBonus { get; set; }

                [DefaultValue(1.5f)]
                [Range(1f, 5f)]
                [Increment(0.25f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.FishingPowerMultiplier.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.FishingPowerMultiplier.Tooltip")]
                public float FishingPowerMultiplier { get; set; }

                [DefaultValue(1.5f)]
                [Range(1f, 5f)]
                [Increment(0.25f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.LootMultiplier.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.FishingHotspot.LootMultiplier.Tooltip")]
                public float LootMultiplier { get; set; }

                public FishingHotspotSettings()
                {
                    FishingPowerBonus = 25f;
                    FishingPowerMultiplier = 1.5f;
                    LootMultiplier = 1.5f;
                }
            }

            [DefaultValue(false)]
            [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.DebugMode.Label")]
            [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.DebugMode.Tooltip")]
            public bool DebugMode { get; set; }

            public EventsSettings()
            {
                AvgMinutesBetweenEvents = 210;
                EventDurationMinutes = 5;
                BlacklistedBiomes = new List<string> { "Forest" };
                FishingHotspot = new FishingHotspotSettings();
                MobTakeover = new MobTakeoverSettings();
                DebugMode = false;
            }

            public class MobTakeoverSettings
            {
                [DefaultValue(2f)]
                [Range(1f, 10f)]
                [Increment(0.1f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.SpawnRateMultiplier.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.SpawnRateMultiplier.Tooltip")]
                public float SpawnRateMultiplier { get; set; }

                [DefaultValue(2f)]
                [Range(0.1f, 10f)]
                [Increment(0.5f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.MaxSpawnsMultiplier.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.MaxSpawnsMultiplier.Tooltip")]
                public float MaxSpawnsMultiplier { get; set; }

                [DefaultValue(0.25f)]
                [Range(0f, 2f)]
                [Increment(0.05f)]
                [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.LuckBonus.Label")]
                [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.Events.MobTakeover.LuckBonus.Tooltip")]
                public float LuckBonus { get; set; }

                public MobTakeoverSettings()
                {
                    SpawnRateMultiplier = 2f;
                    MaxSpawnsMultiplier = 2f;
                    LuckBonus = 0.25f;
                }
            }
        }

        [DefaultValue(true)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.Tweaks.Label")]
        public bool StrongerReforgesTweaksEnabled { get; set; } = true;

        [DefaultValue(-3)]
        [Range(-100, 100)]
        [Increment(1)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.WardingDamagePercent.Label")]
        [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.WardingDamagePercent.Tooltip")]
        public int WardingDamagePercent { get; set; } = -3;

        [DefaultValue(true)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.ReplaceWardingMoveSpeedWithDamage.Label")]
        [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.ReplaceWardingMoveSpeedWithDamage.Tooltip")]
        public bool ReplaceWardingMoveSpeedWithDamage { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.AllrounderRebalance.Label")]
        public bool AllrounderRebalanceEnabled { get; set; } = true;

        [DefaultValue(4)]
        [Range(0, 100)]
        [Increment(1)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.AllrounderDefense.Label")]
        [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.StrongerReforges.AllrounderDefense.Tooltip")]
        public int AllrounderDefense { get; set; } = 4;

        [DefaultValue(3)]
        [Range(-50, 50)]
        [Increment(1)]
        [LabelKey("$Mods.DamageEditor.Configs.DamageEditorConfig.GlobalArmor.FlatDefenseIncrease.Label")]
        [TooltipKey("$Mods.DamageEditor.Configs.DamageEditorConfig.GlobalArmor.FlatDefenseIncrease.Tooltip")]
        public int FlatArmorDefenseIncrease { get; set; } = 3;

        public class PlayerTuningConfig
        {
            [DefaultValue(0)]
            public int DealDamageModifierDifference { get; set; }

            [DefaultValue(0)]
            public int TakeDamageModifierDifference { get; set; }

            [DefaultValue(false)]
            public bool SkilledMode { get; set; }

            public PlayerTuningConfig()
            {
                DealDamageModifierDifference = 0;
                TakeDamageModifierDifference = 0;
                SkilledMode = false;
            }
        }
    }
}
