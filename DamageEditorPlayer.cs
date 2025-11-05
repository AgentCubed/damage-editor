using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework; // for Color
using Terraria.Localization; // for NetworkText
using Terraria.ID; // for NetmodeID
using Terraria.Chat; // for ChatHelper

namespace DamageEditor
{
    public class DamageEditorPlayer : ModPlayer
    {
        public int bossDamageStack = 0;

        /// <summary>
        /// Calculate the damage dealt multiplier based on the config value and boss death stacks
        /// </summary>
        private float GetDealDamageMultiplier()
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int totalModification = config.DealDamage + bossDamageStack;
            return (float)Math.Pow(1.2, totalModification);
        }

        /// <summary>
        /// Calculate the damage taken multiplier based on the config value and boss stacks
        /// </summary>
        private float GetTakeDamageMultiplier()
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int totalModification = config.TakeDamage + bossDamageStack * 2;
            return (float)Math.Pow(1.2, totalModification);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, Terraria.DataStructures.PlayerDeathReason damageSource)
        {
            if (Main.CurrentFrameFlags.AnyActiveBossNPC)
            {
                bossDamageStack++;

                // Notify that boss stacks increased
                string message = $"The Boss consumes a soul and is now stronger...";
                Color messageColor = Color.Orange;

                // Show locally
                Main.NewText(message, messageColor);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int totalModification = config.DealDamage + bossDamageStack;
            if (totalModification != 0)
            {
                float multiplier = GetDealDamageMultiplier();
                modifiers.FinalDamage *= multiplier;
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int totalModification = config.TakeDamage + bossDamageStack;
            if (totalModification != 0)
            {
                float multiplier = GetTakeDamageMultiplier();
                modifiers.IncomingDamageMultiplier *= multiplier;
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int totalModification = config.TakeDamage + bossDamageStack;
            if (totalModification != 0)
            {
                float multiplier = GetTakeDamageMultiplier();
                modifiers.IncomingDamageMultiplier *= multiplier;
            }
        }

        // Run once per tick after player update to safely reset stacks when no boss is active.
        public override void PostUpdate()
        {
            // If stacks exist but no active boss NPCs are present this frame, reset.
            if (bossDamageStack > 0 && !Main.CurrentFrameFlags.AnyActiveBossNPC)
            {
                bossDamageStack = 0;
            }
        }
    }
}
