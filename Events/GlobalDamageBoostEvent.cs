using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    public class GlobalDamageBoostEvent : Event
    {
        public GlobalDamageBoostEvent() : base("Global Damage Boost", biomeSpecific: false)
        {
        }

        protected override void OnEventStart()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("A global damage boost event has begun! All players gain increased damage!", 255, 255, 100);
            }
        }

        protected override void OnEventEnd()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("The global damage boost event has ended.", 255, 200, 100);
            }
        }

        public override bool CanPlayerBenefit(Player player)
        {
            return IsActive; // Global event - affects all players
        }

        public float GetDamageMultiplier()
        {
            return 1.25f; // 25% damage boost
        }
    }
}