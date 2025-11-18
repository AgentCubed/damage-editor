using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DamageEditor.Events
{
    public class EventSystem : ModSystem
    {
        // Note: NetSend and NetReceive hooks are removed in favor of ModPacket-based synchronization
        // The EventPacket class now handles all event state synchronization
        // This prevents aggregation with other mods' WorldData and avoids packet overflow
        private static readonly string[] PossibleBiomes = new string[]
        {
            "Forest",
            "Desert",
            "Snow",
            "Jungle",
            "Ocean",
            "Underground",
            "Caverns",
            "Underground Desert",
            "Underground Snow",
            "Underground Jungle"
        };

        private static readonly string[] HardmodeBiomes = new string[]
        {
            "Underground Corruption",
            "Underground Crimson",
            "Underground Hallow"
        };

        public static List<Event> RegisteredEvents { get; private set; }
        public static Event ActiveEvent { get; private set; }

        private static int ticksUntilNextEvent;
        private static bool eventsInitialized = false;

        public static void SetActiveEvent(Event @event)
        {
            ActiveEvent = @event;
        }

        public override void OnModLoad()
        {
            // Initialize the registered events list at mod load time
            if (!eventsInitialized && RegisteredEvents == null)
            {
                RegisteredEvents = new List<Event>();
                eventsInitialized = true;
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText("[DEBUG] EventSystem.OnModLoad: Initialized RegisteredEvents list", 255, 200, 100);
            }
        }

        public override void OnWorldLoad()
        {
            // Reset only the active event and timer, NOT the registered events list
            // This preserves the event registry across world reloads
            ActiveEvent = null;
            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] EventSystem.OnWorldLoad() called. Registered events: {RegisteredEvents?.Count ?? 0}", 255, 200, 100);

            ResetEventTimer();
        }

        public override void OnWorldUnload()
        {
            RegisteredEvents?.Clear();
            ActiveEvent = null;
        }

        public override void PostUpdateWorld()
        {
            // Only process events on the server or in single-player (netMode 0 or 2)
            // Clients (netMode 1) should NOT run event logic, they receive state via NetReceive
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText("[DEBUG] PostUpdateWorld: Client mode detected, skipping event processing", 150, 150, 255);
                return;
            }

            if (ActiveEvent != null)
            {
                ActiveEvent.Update();

                if (!ActiveEvent.IsActive)
                {
                    if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                        Terraria.Main.NewText($"[DEBUG] PostUpdateWorld: Event '{ActiveEvent.EventName}' ended", 150, 100, 200);

                    ActiveEvent = null;
                    ResetEventTimer();
                    SyncEventToClients();
                }
            }
            else
            {
                ticksUntilNextEvent--;

                if (ticksUntilNextEvent <= 0)
                {
                    if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                        Terraria.Main.NewText("[DEBUG] PostUpdateWorld: Time to start new event!", 255, 200, 100);

                    TryStartRandomEvent();
                }
            }
        }

        private static void SyncEventToClients()
        {
            // Use ModPacket for targeted event sync instead of WorldData broadcast
            // This avoids aggregating with all other mods' data and prevents packet overflow
            EventPacket.SendEventState();
            
            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] EventSystem: Sending event state via ModPacket (netMode={Main.netMode})", 100, 200, 255);
        }

        private static void ResetEventTimer()
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            int avgTicks = (config?.Events?.AvgMinutesBetweenEvents ?? 210) * 60 * 60;
            ticksUntilNextEvent = avgTicks;
        }

        private static void TryStartRandomEvent()
        {
            if (RegisteredEvents == null || RegisteredEvents.Count == 0)
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText("[DEBUG] No events registered!", 255, 100, 100);
                ResetEventTimer();
                return;
            }

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
            {
                Terraria.Main.NewText($"[DEBUG] Total registered events: {RegisteredEvents.Count}", 255, 255, 100);
                for (int i = 0; i < RegisteredEvents.Count; i++)
                {
                    Terraria.Main.NewText($"[DEBUG] Event {i}: {RegisteredEvents[i].EventName}", 200, 200, 255);
                }
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            List<string> blacklistedBiomes = config?.Events?.BlacklistedBiomes ?? new List<string> { "Forest" };

            List<string> availableBiomes = new List<string>(PossibleBiomes);

            if (Main.hardMode)
            {
                availableBiomes.AddRange(HardmodeBiomes);
            }

            // Remove blacklisted biomes
            availableBiomes.RemoveAll(b => blacklistedBiomes.Contains(b));

            if (availableBiomes.Count == 0)
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText("[DEBUG] No available biomes after filtering!", 255, 100, 100);
                ResetEventTimer();
                return;
            }

            int randomEventIndex = Main.rand.Next(RegisteredEvents.Count);
            Event chosenEvent = RegisteredEvents[randomEventIndex];
            string chosenBiome = null;

            if (chosenEvent.BiomeSpecific)
            {
                chosenBiome = availableBiomes[Main.rand.Next(availableBiomes.Count)];
            }

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
            {
                Terraria.Main.NewText($"[DEBUG] Raw rand call: Main.rand.Next({RegisteredEvents.Count}) returned {randomEventIndex}", 100, 255, 100);
                Terraria.Main.NewText($"[DEBUG] Selected event index {randomEventIndex}: {chosenEvent.EventName}", 100, 255, 100);
                Terraria.Main.NewText($"[DEBUG] Selected biome: {chosenBiome ?? "Global"}", 100, 255, 100);
            }

            ActiveEvent = chosenEvent;
            ActiveEvent.StartEvent(chosenBiome);

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] Sending WorldData sync (netMode={Main.netMode})", 100, 200, 255);

            SyncEventToClients();
        }

        public static void RegisterEvent(Event @event)
        {
            if (RegisteredEvents == null)
                RegisteredEvents = new List<Event>();

            if (RegisteredEvents.Contains(@event))
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText($"[DEBUG] RegisterEvent: Event '{@event.EventName}' already registered, skipping duplicate", 255, 150, 100);
                return;
            }

            foreach (var existing in RegisteredEvents)
            {
                if (existing.EventName == @event.EventName)
                {
                    if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                        Terraria.Main.NewText($"[DEBUG] RegisterEvent: Event with name '{@event.EventName}' already exists, skipping duplicate", 255, 150, 100);
                    return;
                }
            }

            RegisteredEvents.Add(@event);
            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] Registered event: {@event.EventName} (Total: {RegisteredEvents.Count})", 150, 150, 255);
        }

        public static void ForceEndActiveEvent()
        {
            if (ActiveEvent != null)
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText($"[DEBUG] ForceEndActiveEvent: Ending {ActiveEvent.EventName}", 200, 100, 150);

                ActiveEvent.EndEvent();
                ActiveEvent = null;
                ResetEventTimer();

                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText($"[DEBUG] Sending WorldData sync for event end (netMode={Main.netMode})", 200, 100, 150);

                SyncEventToClients();
            }
        }

        public static void ForceStartEvent(Event eventToStart, string targetBiome = null)
        {
            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] ForceStartEvent called: {eventToStart.EventName} in {targetBiome}", 200, 150, 100);

            if (ActiveEvent != null)
            {
                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Terraria.Main.NewText($"[DEBUG] Ending previous event: {ActiveEvent.EventName}", 200, 150, 100);

                ActiveEvent.EndEvent();
            }

            ActiveEvent = eventToStart;
            ActiveEvent.StartEvent(targetBiome);

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Terraria.Main.NewText($"[DEBUG] Sending WorldData sync for forced event (netMode={Main.netMode})", 200, 150, 100);

            SyncEventToClients();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ticksUntilNextEvent"] = ticksUntilNextEvent;

            if (ActiveEvent != null)
            {
                tag["hasActiveEvent"] = true;
                tag["activeEventIndex"] = RegisteredEvents.IndexOf(ActiveEvent);
                tag["activeEventBiome"] = ActiveEvent.TargetBiomeName;
                tag["activeEventDuration"] = ActiveEvent.RemainingDuration;
                // Allow events to persist custom data (BingoEvent board)
                if (ActiveEvent is BingoEvent bingo)
                {
                    tag["bingoBoard"] = bingo.GetBoardArray();
                }
            }
            else
            {
                tag["hasActiveEvent"] = false;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            ticksUntilNextEvent = tag.GetInt("ticksUntilNextEvent");

            if (ticksUntilNextEvent == 0)
                ResetEventTimer();

            if (tag.GetBool("hasActiveEvent"))
            {
                int eventIndex = tag.GetInt("activeEventIndex");
                if (eventIndex >= 0 && eventIndex < RegisteredEvents.Count)
                {
                    ActiveEvent = RegisteredEvents[eventIndex];
                    string biome = tag.GetString("activeEventBiome");
                    int savedDuration = tag.GetInt("activeEventDuration");

                    // Restore the event state without displaying the start message.
                    ActiveEvent.InitializeEvent(biome);
                    ActiveEvent.SetRemainingDuration(savedDuration);

                    // Restore Bingo event board if available
                    if (ActiveEvent is BingoEvent bingo && tag.ContainsKey("bingoBoard"))
                    {
                        try
                        {
                            var arr = tag.Get<int[]>("bingoBoard");
                            bingo.SetBoardFromArray(arr);
                        }
                        catch { }
                    }

                    if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                        Terraria.Main.NewText($"[DEBUG] LoadWorldData: Restored event '{ActiveEvent.EventName}' in biome '{biome}' with duration {savedDuration}", 100, 200, 255);
                }
            }
            else
            {
                ActiveEvent = null;
            }
        }
    }
}
