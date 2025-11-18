using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;

namespace DamageEditor
{
    public class DPSGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            try
            {
                Player player = Main.LocalPlayer;
                if (player == null)
                    return;

                // Find vanilla damage and speed lines
                TooltipLine dmgLine = tooltips.Find(l => l.Name == "Damage");
                if (dmgLine == null)
                    return; // no damage line -> not a damaging item

                // Do not display for summons/minions
                if (item.CountsAsClass(DamageClass.Summon))
                    return;

                // Determine hits per second using similar logic used in TrueTooltips
                float attackSpeedModifier = item.CountsAsClass(DamageClass.Melee) ? player.GetAttackSpeed(DamageClass.Melee) : 1f;
                float useTime = item.useAnimation / Math.Max(attackSpeedModifier, 0.0001f);
                float totalDelay = item.reuseDelay > 0 ? item.reuseDelay : useTime;
                if (totalDelay <= 0f) totalDelay = 1f; // avoid division by 0
                float hitsPerSecond = 60f / totalDelay;

                // Find ammo in inventory to account for combined weapon+ammo damage (like TrueTooltips does)
                Item currentAmmo = null;
                if (item.useAmmo > 0)
                {
                    foreach (Item invItem in player.inventory)
                    {
                        if (invItem != null && invItem.active && invItem.ammo == item.useAmmo)
                        {
                            currentAmmo = invItem;
                            break;
                        }
                    }
                }

                // Compute effective weapon damage accounting for player modifiers and ammo
                int effectiveDamage = player.GetWeaponDamage(item);
                if (currentAmmo != null)
                    effectiveDamage += player.GetWeaponDamage(currentAmmo);

                // DPS = damage * hits per second
                float dps = effectiveDamage * hitsPerSecond;

                // Insert the DPS tooltip after the speed line if there is one, otherwise after damage
                int insertAt = -1;
                var speedLine = tooltips.Find(l => l.Name == "Speed");
                if (speedLine != null)
                {
                    insertAt = tooltips.IndexOf(speedLine) + 1;
                }
                else
                {
                    insertAt = tooltips.IndexOf(dmgLine) + 1;
                }

                // Format the tooltip value with two decimal places
                string dpsText = Math.Round(dps, 2).ToString() + " DPS";
                TooltipLine dpsLine = new TooltipLine(Mod, "DPS", dpsText) { OverrideColor = Color.LightGoldenrodYellow };

                if (insertAt >= 0 && insertAt <= tooltips.Count)
                    tooltips.Insert(insertAt, dpsLine);
                else
                    tooltips.Add(dpsLine);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("[DamageEditor] DPS tooltip failed: " + ex);
            }
        }
    }
}
