using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    public class FishingHotspotEvent : Event
    {
        public FishingHotspotEvent() : base("Fishing Hotspot", biomeSpecific: true)
        {
        }

        protected override void OnEventStart()
        {
            // No chat broadcast here. Let the central StartEvent/packet synchronization handle displaying
            // the start message once per client/server.
        }

        public override bool CanPlayerBenefit(Player player)
        {
            if (!IsActive || string.IsNullOrEmpty(TargetBiomeName))
                return false;

            return CheckPlayerInBiome(player, TargetBiomeName);
        }

        private bool CheckPlayerInBiome(Player player, string biomeName)
        {
            switch (biomeName)
            {
                case "Forest":
                    return !player.ZoneBeach && !player.ZoneDesert && !player.ZoneSnow &&
                           !player.ZoneJungle && !player.ZoneCorrupt && !player.ZoneCrimson &&
                           !player.ZoneHallow && !player.ZoneDungeon && player.ZoneOverworldHeight;

                case "Desert":
                    return player.ZoneDesert && player.ZoneOverworldHeight;

                case "Snow":
                    return player.ZoneSnow && player.ZoneOverworldHeight;

                case "Jungle":
                    return player.ZoneJungle && player.ZoneOverworldHeight;

                case "Ocean":
                    return player.ZoneBeach;

                case "Underground":
                    return player.ZoneDirtLayerHeight;

                case "Caverns":
                    return player.ZoneRockLayerHeight;

                case "Underground Desert":
                    return player.ZoneDesert && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                case "Underground Snow":
                    return player.ZoneSnow && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                case "Underground Jungle":
                    return player.ZoneJungle && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                case "Underground Corruption":
                    return player.ZoneCorrupt && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                case "Underground Crimson":
                    return player.ZoneCrimson && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                case "Underground Hallow":
                    return player.ZoneHallow && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

                default:
                    return false;
            }
        }

        public float GetFishingPowerBonus()
        {
            if (!IsActive)
                return 0f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.FishingHotspot?.FishingPowerBonus ?? 25f;
        }

        public float GetLootMultiplier()
        {
            if (!IsActive)
                return 1f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.FishingHotspot?.LootMultiplier ?? 1.5f;
        }

        public float GetFishingPowerMultiplier()
        {
            if (!IsActive)
                return 1f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.FishingHotspot?.FishingPowerMultiplier ?? 1.5f;
        }
    }

    public class FishingHotspotPlayer : ModPlayer
    {
        public override void GetFishingLevel(Item fishingRod, Item bait, ref float fishingLevel)
        {
            if (EventSystem.ActiveEvent is FishingHotspotEvent hotspot && hotspot.CanPlayerBenefit(Player))
            {
                fishingLevel += hotspot.GetFishingPowerBonus();
                fishingLevel *= hotspot.GetFishingPowerMultiplier();
            }
        }

        public override void ModifyCaughtFish(Item fish)
        {
            if (EventSystem.ActiveEvent is FishingHotspotEvent hotspot && hotspot.CanPlayerBenefit(Player))
            {
                float multiplier = hotspot.GetLootMultiplier();

                if (multiplier > 1f && fish.stack > 0)
                {
                    int additionalStack = (int)((fish.stack * (multiplier - 1f)) + 0.5f);
                    fish.stack += Math.Max(1, additionalStack);
                }
            }
        }
    }
}
