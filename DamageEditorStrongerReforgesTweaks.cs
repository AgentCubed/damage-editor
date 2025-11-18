using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DamageEditor
{
    public class DamageEditorStrongerReforgesTweaks : ModPlayer
    {
        // We use the static values stored in DamageEditorStrongerReforgesSystem which are initialized in PostSetupContent

        public override void UpdateEquips()
        {
            var cfg = ModContent.GetInstance<DamageEditorConfig>();

            if (!cfg.StrongerReforgesTweaksEnabled)
            {
                // Still may need to process global armor increase
            }

            // Apply global flat armor defense increase for all armor pieces (head/body/legs and any item with defense value)
            if (cfg.FlatArmorDefenseIncrease != 0)
            {
                foreach (Item it in Player.armor)
                {
                    if (it == null || it.type == ItemID.None)
                        continue;
                    if (it.defense > 0)
                    {
                        Player.statDefense += cfg.FlatArmorDefenseIncrease;
                    }
                }
            }

            if (!cfg.StrongerReforgesTweaksEnabled)
                return;

            // If StrongerReforges is available and we located prefixes, apply tweaks accordingly.
            // We'll compute derived defaults using StrongerReforges multipliers where possible.
            int baseAllrounderDefense = (int)(2f * DamageEditorStrongerReforgesSystem.StrongerPosMult);
            int desiredAllrounderDefense = cfg.AllrounderRebalanceEnabled ? cfg.AllrounderDefense : baseAllrounderDefense;
            int extraAllrounderDef = desiredAllrounderDefense - baseAllrounderDefense;

            float baseWardingMoveSpeed = 0.03f * DamageEditorStrongerReforgesSystem.StrongerNegMult;
            float desiredWardingDamage = cfg.WardingDamagePercent / 100f;

            for (int i = 0; i < Player.armor.Length; i++)
            {
                Item it = Player.armor[i];
                if (it == null || it.type == ItemID.None)
                    continue;

                // Allrounder tweak: add/subtract to reach desired Allrounder defense
                if (cfg.AllrounderRebalanceEnabled && DamageEditorStrongerReforgesSystem.AllrounderPrefixId > 0 && it.prefix == DamageEditorStrongerReforgesSystem.AllrounderPrefixId)
                {
                    Player.statDefense += extraAllrounderDef;
                }

                // Warding tweak: give -X% damage in lieu of movement change (or in addition)
                if (DamageEditorStrongerReforgesSystem.WardingPrefixId > 0 && it.prefix == DamageEditorStrongerReforgesSystem.WardingPrefixId)
                {
                    // Optionally remove the movement penalty from StrongerReforges by adding it back
                    if (cfg.ReplaceWardingMoveSpeedWithDamage)
                    {
                        Player.moveSpeed += baseWardingMoveSpeed;
                    }

                    // Apply the damage penalty (WardingDamagePercent is expected as an integer percent, e.g., -3)
                    if (cfg.WardingDamagePercent != 0)
                    {
                        float damageDiff = cfg.WardingDamagePercent / 100f; // negative values reduce damage
                        Player.GetDamage(DamageClass.Generic) += damageDiff;
                    }
                }
            }
        }
    }
}
