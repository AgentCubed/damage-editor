using Terraria.ModLoader;

namespace DamageEditor.Events
{
    /// <summary>
    /// Global system responsible for registering all biome events at mod load time.
    /// This ensures a single, predictable registration point before world/player loading.
    /// </summary>
    public class EventsGlobalSystem : ModSystem
    {
        public override void OnModLoad()
        {
            // Register all built-in events at mod load time.
            // This happens once during mod initialization, providing a stable event registry
            // that persists across world loads and doesn't get cleared.

            EventSystem.RegisterEvent(new FishingHotspotEvent());
            EventSystem.RegisterEvent(new MobTakeoverEvent());
            EventSystem.RegisterEvent(new GlobalDamageBoostEvent());
            // Bingo event - integrated with MultiplayerItemChecklist
            EventSystem.RegisterEvent(new BingoEvent());

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText("[DEBUG] EventsGlobalSystem.OnModLoad: Registered all events", 100, 255, 100);
        }
    }
}
