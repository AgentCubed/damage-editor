using System;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DamageEditor
{
    public class DamageEditorStrongerReforgesSystem : ModSystem
    {
        public static int AllrounderPrefixId = -1;
        public static int WardingPrefixId = -1;
        public static float StrongerPosMult = 1f;
        public static float StrongerNegMult = 1f;

        public override void PostSetupContent()
        {
            if (ModLoader.TryGetMod("StrongerReforges", out Mod srMod))
            {
                if (ModContent.TryFind<ModPrefix>("StrongerReforges", "Allrounder", out var allp))
                {
                    AllrounderPrefixId = allp.Type;
                }
                if (ModContent.TryFind<ModPrefix>("StrongerReforges", "Warding", out var wardp))
                {
                    WardingPrefixId = wardp.Type;
                }

                try
                {
                    // Attempt to read the ReforgeConfig positive and negative multiplier via reflection from the mod's assembly.
                    var asm = srMod.GetType().Assembly;
                    var reforgeType = asm.GetType("StrongerReforges.ReforgeConfig");
                    if (reforgeType != null)
                    {
                        var instanceField = reforgeType.GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        var reforgeInstance = instanceField?.GetValue(null);
                        var posProp = reforgeType.GetProperty("PositiveMultiplier");
                        var negProp = reforgeType.GetProperty("NegativeMultiplier");
                        if (reforgeInstance != null)
                        {
                            if (posProp != null)
                                StrongerPosMult = Convert.ToSingle(posProp.GetValue(reforgeInstance));
                            if (negProp != null)
                                StrongerNegMult = Convert.ToSingle(negProp.GetValue(reforgeInstance));
                        }
                    }
                }
                catch
                {
                    StrongerPosMult = 1f;
                    StrongerNegMult = 1f;
                }
            }
        }
    }
}
