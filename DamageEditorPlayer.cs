using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using DamageEditor.Boss;
using DamageEditor.Events;

#nullable enable

namespace DamageEditor
{
    public class DamageEditorPlayer : ModPlayer
    {
        // --- Static Tuning Variables ---
        // These should be set once at startup or via configuration.
        private const double TICK_INTENSITY = 0.0003;
        private const double DIFFERENCE_FROM_AVG_INTENSITY = 0.15;
        private const double NUMER_ALIVE_INTENSITY = 0.5;
        private const int TICKS_PER_SECOND = 60;

        private static long cumulativeShortHandedTickCounter = 0;
        private static int cachedAlivePlayers = 0;
        private static int cachedOnlinePlayers = 1;
        private static uint lastStateUpdateFrame = uint.MaxValue;

        private int deathsThisBossFight = 0;

        public int DeathsThisBossFight => deathsThisBossFight;

        private DamageEditorConfig.PlayerTuningConfig? ResolvePlayerTuning(DamageEditorConfig config)
        {
            if (config.PlayerOverrides == null)
            {
                return null;
            }

            if (config.PlayerOverrides.TryGetValue(Player.name, out var specific))
            {
                return specific;
            }

            if (config.PlayerOverrides.TryGetValue("playername", out var fallback))
            {
                return fallback;
            }

            return null;
        }

        private int GetDealModification(DamageEditorConfig config, DamageEditorConfig.PlayerTuningConfig? tuning)
        {
            int offset = tuning?.DealDamageModifierDifference ?? 0;
            return config.DealDamage + deathsThisBossFight + offset;
        }

        private int GetTakeModification(DamageEditorConfig config, DamageEditorConfig.PlayerTuningConfig? tuning)
        {
            int offset = tuning?.TakeDamageModifierDifference ?? 0;
            return config.TakeDamage + deathsThisBossFight * 2 + offset;
        }

        private static bool ShouldProcessSharedState(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return player.whoAmI == Main.myPlayer;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                return player.whoAmI == 0;
            }

            return player.whoAmI == Main.myPlayer;
        }

        private static void UpdateSharedFightState()
        {
            uint frame = Main.GameUpdateCount;
            if (lastStateUpdateFrame == frame)
            {
                return;
            }

            lastStateUpdateFrame = frame;

            int online = 0;
            int alive = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                {
                    continue;
                }

                online++;
                if (!player.dead && player.statLife > 0 && !player.ghost)
                {
                    alive++;
                }
            }

            cachedOnlinePlayers = Math.Max(1, online);
            cachedAlivePlayers = Math.Min(alive, cachedOnlinePlayers);

            if (Main.CurrentFrameFlags.AnyActiveBossNPC && cachedAlivePlayers < cachedOnlinePlayers)
            {
                cumulativeShortHandedTickCounter++;
            }
            else
            {
                cumulativeShortHandedTickCounter = 0;
            }
        }

        /// <summary>
        /// Calculates the damage multiplier based on three combined scaling factors.
        /// </summary>
        public static double GetCombinedDamageMultiplier(
            long cumulativeTickCounter,
            int alivePlayers,
            int onlinePlayers,
            int totalDeaths,
            int yourDeaths)
        {
            if (alivePlayers >= onlinePlayers)
            {
                return 1.0;
            }

            double aliveFactor = (double)(onlinePlayers - alivePlayers) / onlinePlayers;
            double nam = 1.0 + aliveFactor * NUMER_ALIVE_INTENSITY;

            double averageDeaths = onlinePlayers > 0 ? (double)totalDeaths / onlinePlayers : 0.0;
            double individualDifference = averageDeaths - yourDeaths;
            double dim = Math.Max(1.0, 1.0 + individualDifference * DIFFERENCE_FROM_AVG_INTENSITY);

            double timeInSeconds = (double)cumulativeTickCounter / TICKS_PER_SECOND;
            double tm = 1.0 + Math.Pow(timeInSeconds, 2) * TICK_INTENSITY;

            double finalMultiplier = nam * dim * tm;

            return finalMultiplier;
        }

        /// <summary>
        /// Calculate the damage dealt multiplier based on the config value and current boss death count
        /// </summary>
        private float GetDealDamageMultiplier(int totalModification)
        {
            return (float)Math.Pow(1.2, totalModification);
        }

        /// <summary>
        /// Calculate the damage taken multiplier based on the config value and current boss death count
        /// </summary>
        private float GetTakeDamageMultiplier(int totalModification)
        {
            return (float)Math.Pow(1.2, totalModification);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, Terraria.DataStructures.PlayerDeathReason damageSource)
        {
            if (Main.CurrentFrameFlags.AnyActiveBossNPC)
            {
                deathsThisBossFight++;
                BossGlobalNPC.RecordBossDeath();

                int totalDeaths = BossGlobalNPC.TotalBossDeaths;
                // string message = $"You have fallen {deathsThisBossFight} time(s) this boss fight. Total boss deaths so far: {totalDeaths}.";
                // Main.NewText(message, Color.Orange);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            var tuning = ResolvePlayerTuning(config);
            int totalModification = GetDealModification(config, tuning);
            if (totalModification != 0)
            {
                float multiplier = GetDealDamageMultiplier(totalModification);
                modifiers.FinalDamage *= multiplier;
            }

            // Apply global damage boost event
            if (EventSystem.ActiveEvent is GlobalDamageBoostEvent boost && boost.CanPlayerBenefit(Player))
            {
                modifiers.FinalDamage *= boost.GetDamageMultiplier();
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            var tuning = ResolvePlayerTuning(config);
            int totalModification = GetTakeModification(config, tuning);
            if (totalModification != 0)
            {
                float multiplier = GetTakeDamageMultiplier(totalModification);
                modifiers.IncomingDamageMultiplier *= multiplier;
            }

            if (Main.CurrentFrameFlags.AnyActiveBossNPC && tuning?.SkilledMode == true)
            {
                UpdateSharedFightState();
                int totalDeaths = BossGlobalNPC.GetCurrentFightDeathCount();
                double skilledMultiplier = GetCombinedDamageMultiplier(
                    cumulativeShortHandedTickCounter,
                    cachedAlivePlayers,
                    cachedOnlinePlayers,
                    totalDeaths,
                    deathsThisBossFight);

                modifiers.IncomingDamageMultiplier *= (float)skilledMultiplier;
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            var tuning = ResolvePlayerTuning(config);
            int totalModification = GetTakeModification(config, tuning);
            if (totalModification != 0)
            {
                float multiplier = GetTakeDamageMultiplier(totalModification);
                modifiers.IncomingDamageMultiplier *= multiplier;
            }

            if (Main.CurrentFrameFlags.AnyActiveBossNPC && tuning?.SkilledMode == true)
            {
                UpdateSharedFightState();
                int totalDeaths = BossGlobalNPC.GetCurrentFightDeathCount();
                double skilledMultiplier = GetCombinedDamageMultiplier(
                    cumulativeShortHandedTickCounter,
                    cachedAlivePlayers,
                    cachedOnlinePlayers,
                    totalDeaths,
                    deathsThisBossFight);

                modifiers.IncomingDamageMultiplier *= (float)skilledMultiplier;
            }
        }

        // Run once per tick after player update to safely reset stacks when no boss is active.
        public override void PostUpdate()
        {
            if (ShouldProcessSharedState(Player))
            {
                UpdateSharedFightState();
            }

            // If stacks exist but no active boss NPCs are present this frame, reset.
            if (deathsThisBossFight > 0 && !Main.CurrentFrameFlags.AnyActiveBossNPC)
            {
                deathsThisBossFight = 0;
            }
        }

        public override void ModifyLuck(ref float luck)
        {
            if (EventSystem.ActiveEvent is MobTakeoverEvent takeover && takeover.CanPlayerBenefit(Player))
            {
                float luckBonus = takeover.GetLuckBonus();
                luck += luckBonus;
            }
        }
    }
}
