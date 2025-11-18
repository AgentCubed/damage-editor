using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    public class MobTakeoverEvent : Event
    {
        public MobTakeoverEvent() : base("Mob Takeover", biomeSpecific: true)
        {
        }

        protected override void OnEventStart()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("Monsters are swarming! Increased spawns and loot drops!", 255, 100, 100);
            }
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

        public float GetSpawnRateMultiplier()
        {
            if (!IsActive)
                return 1f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.MobTakeover?.SpawnRateMultiplier ?? 2f;
        }

        public float GetMaxSpawnsMultiplier()
        {
            if (!IsActive)
                return 1f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.MobTakeover?.MaxSpawnsMultiplier ?? 2f;
        }

        public float GetLuckBonus()
        {
            if (!IsActive)
                return 0f;

            var config = ModContent.GetInstance<DamageEditorConfig>();
            return config?.Events?.MobTakeover?.LuckBonus ?? 0.25f;
        }
    }

    public class MobTakeoverGlobalNPC : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (EventSystem.ActiveEvent is MobTakeoverEvent takeover && takeover.CanPlayerBenefit(player))
            {
                spawnRate = (int)(spawnRate / takeover.GetSpawnRateMultiplier());
                maxSpawns = (int)(maxSpawns * takeover.GetMaxSpawnsMultiplier());
            }
        }
    }
}
