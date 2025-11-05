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
        public BossSettings Boss { get; set; } = new BossSettings();

        public class BossSettings
        {
            [DefaultValue(12)]
            [Range(1, 240)]
            [Tooltip("The total minutes you expect the boss fight to last.")]
            public int ExpectedTotalMinutes { get; set; }
        }
    }
}
