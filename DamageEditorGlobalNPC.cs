using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace DamageEditor
{
    public class DamageEditorGlobalNPC : GlobalNPC
    {
        // --- Configurable Pace ---
        // Default dead zone is now per-spawn and derived from expected boss minutes / 5.
        // Keep a sensible fallback in case config is missing.
        private double deadZoneMinutes = 2.0;

        private const double SCALING_CONSTANT = 1.0;
        // ---

        // Computed per-spawn from config.Boss.ExpectedTotalMinutes (minutes -> ticks)
        private double idealTotalTicks = 12.0 * 3600.0; // default fallback

        public override bool InstancePerEntity => true;

        public double spawnTime = -1;
        public double currentDefenseModifier = 1.0;
        public double currentOffenseModifier = 1.0;
        public int lastHpInterval = 100;
        public double lastTimeDifference = 0.0;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (npc.boss)
            {
                spawnTime = Main.time;
                currentDefenseModifier = 1.0;
                currentOffenseModifier = 1.0;
                lastHpInterval = 100;
                lastTimeDifference = 0.0;

                try
                {
                    var config = ModContent.GetInstance<DamageEditorConfig>();
                    if (config != null && config.Boss != null && config.Boss.ExpectedTotalMinutes > 0)
                    {
                        idealTotalTicks = config.Boss.ExpectedTotalMinutes * 3600.0;
                        // Dead zone is expected minutes divided by 5 (per request)
                        deadZoneMinutes = config.Boss.ExpectedTotalMinutes / 5.0;
                    }
                    else
                    {
                        idealTotalTicks = 12.0 * 3600.0;
                        deadZoneMinutes = 12.0 / 5.0;
                    }
                }
                catch
                {
                    idealTotalTicks = 12.0 * 3600.0;
                    deadZoneMinutes = 12.0 / 5.0;
                }
            }
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (!npc.boss || spawnTime < 0)
            {
                return;
            }

            double timeAlive = Main.time - spawnTime;

            if (timeAlive <= 60)
            {
                return;
            }

            double hpPercent = npc.life / (double)npc.lifeMax;

            int currentHpInterval = (int)Math.Floor(hpPercent * 10) * 10;

            if (currentHpInterval < lastHpInterval)
            {
                double hpLost = 1.0 - (currentHpInterval / 100.0);
                double idealTime = idealTotalTicks * hpLost;

                double timeDifferenceMinutes = (timeAlive - idealTime) / 3600.0;
                lastTimeDifference = timeDifferenceMinutes;

                double absTimeDiff = Math.Abs(timeDifferenceMinutes);

                if (absTimeDiff <= deadZoneMinutes)
                {
                    currentDefenseModifier = 1.0;
                    currentOffenseModifier = 1.0;
                }
                else
                {
                    double scaledDifference = absTimeDiff - deadZoneMinutes;

                    // Quadratic formula: 1 + c * (x - deadzone)^2
                    double modifier = 1.0 + SCALING_CONSTANT * Math.Pow(scaledDifference, 2);
                    modifier = Math.Min(modifier, 10.0);

                    if (timeDifferenceMinutes > 0) // Player is SLOW
                    {
                        currentOffenseModifier = modifier;
                        currentDefenseModifier = 1.0;
                    }
                    else // Player is FAST
                    {
                        currentDefenseModifier = modifier;
                        currentOffenseModifier = 1.0;
                    }
                }

                lastHpInterval = currentHpInterval;

                // --- Show Debug Text ---
                string paceMessage = "On Pace";
                double displayDefMod = 1.0; // What we show as the boss defense multiplier

                if (currentOffenseModifier > 1.0)
                {
                    // Player is slow: we increase player damage via currentOffenseModifier.
                    // For debug we want to show the equivalent boss defense multiplier (<1.0).
                    paceMessage = $"Pace: +{lastTimeDifference:F1} min";
                    displayDefMod = 1.0 / currentOffenseModifier;
                }
                else if (currentDefenseModifier > 1.0)
                {
                    // Player is fast: boss defense is increased (player deals less).
                    paceMessage = $"Pace: {lastTimeDifference:F1} min";
                    displayDefMod = currentDefenseModifier;
                }

                // Main.NewText($"{(int)(hpPercent * 100.0)}% HP | {paceMessage} | {displayDefMod:F2}x Def", Color.Gray);
            }

            modifiers.FinalDamage /= (float)currentDefenseModifier;
            modifiers.FinalDamage *= (float)currentOffenseModifier;
        }
    }
}