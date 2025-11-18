using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.UI.Chat;
using DamageEditor.Boss;
using DamageEditor.Events;

namespace DamageEditor
{
    public class DealCommand : ModCommand
    {
        public override string Command => "deal";
        public override CommandType Type => CommandType.Chat;
        public override string Usage => "/deal <amount>";
        public override string Description => "Set the DealDamage config value.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out int amount))
            {
                caller.Reply("Usage: " + Usage);
                return;
            }
            var active = ModContent.GetInstance<DamageEditorConfig>();
            // Use a populated clone as pending config so clients can request the server to accept the change.
            var pending = ConfigManager.GeneratePopulatedClone(active) as DamageEditorConfig;
            if (pending == null)
            {
                // Fallback to direct set if clone fails for any reason
                active.DealDamage = amount;
                caller.Reply($"DealDamage set to {amount}.");
                return;
            }
            pending.DealDamage = amount;
            // SaveChanges will send a request to the server when invoked from a client, or apply immediately on the server.
            active.SaveChanges(pending, (msg, color) => caller.Reply(msg), silent: false, broadcast: true);
        }
    }

    public class TakeCommand : ModCommand
    {
        public override string Command => "take";
        public override CommandType Type => CommandType.Chat;
        public override string Usage => "/take <amount>";
        public override string Description => "Set the TakeDamage config value.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out int amount))
            {
                caller.Reply("Usage: " + Usage);
                return;
            }
            var active = ModContent.GetInstance<DamageEditorConfig>();
            var pending = ConfigManager.GeneratePopulatedClone(active) as DamageEditorConfig;
            if (pending == null)
            {
                active.TakeDamage = amount;
                caller.Reply($"TakeDamage set to {amount}.");
                return;
            }
            pending.TakeDamage = amount;
            active.SaveChanges(pending, (msg, color) => caller.Reply(msg), silent: false, broadcast: true);
        }
    }

    public class BossDeathsCommand : ModCommand
    {
        public override string Command => "bossdeaths";
        public override CommandType Type => CommandType.Chat;
        public override string Usage => "/bossdeaths";
        public override string Description => "Show boss death statistics.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length != 0)
            {
                caller.Reply("Usage: " + Usage);
                return;
            }

            int totalDeaths = BossGlobalNPC.TotalBossDeaths;
            int playerDeathsThisFight = caller.Player.GetModPlayer<DamageEditorPlayer>().DeathsThisBossFight;

            caller.Reply($"Total boss deaths so far: {totalDeaths}. Your deaths this fight: {playerDeathsThisFight}.");
        }
    }

    public class ToggleEventCommand : ModCommand
    {
        public override string Command => "toggleevent";
        public override CommandType Type => CommandType.Chat | CommandType.Server;
        public override string Usage => "/toggleevent [biome]";
        public override string Description => "Toggle a random biome event or force one in a specific biome.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // Forward client-issued commands to the server so the event actually runs in multiplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ChatHelper.SendChatMessageFromClient(ChatManager.Commands.CreateOutgoingMessage(input));
                return;
            }

            // Allow any player to request toggling an event. The server will process the command and run the event changes.

            if (EventSystem.ActiveEvent != null)
            {
                EventSystem.ForceEndActiveEvent();
                caller.Reply("Event ended.");
            }
            else
            {
                string targetBiome = args.Length > 0 ? string.Join(" ", args) : null;

                if (EventSystem.RegisteredEvents == null || EventSystem.RegisteredEvents.Count == 0)
                {
                    caller.Reply("No events registered.");
                    return;
                }

                var debugEnabled = ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true;
                if (debugEnabled)
                {
                    caller.Reply($"[DEBUG] Total registered events: {EventSystem.RegisteredEvents.Count}");
                    for (int i = 0; i < EventSystem.RegisteredEvents.Count; i++)
                    {
                        caller.Reply($"[DEBUG] Event {i}: {EventSystem.RegisteredEvents[i].EventName}");
                    }
                }

                int randomEventIndex = Main.rand.Next(EventSystem.RegisteredEvents.Count);
                Event randomEvent = EventSystem.RegisteredEvents[randomEventIndex];

                if (targetBiome != null)
                {
                    EventSystem.ForceStartEvent(randomEvent, targetBiome);
                    caller.Reply($"Started {randomEvent.EventName} in {targetBiome}.");
                }
                else
                {
                    // Build the same available biomes list as the system uses so we respect blacklist and hardmode.
                    var config = ModContent.GetInstance<DamageEditorConfig>();
                    List<string> blacklistedBiomes = config?.Events?.BlacklistedBiomes ?? new List<string> { "Forest" };

                    List<string> availableBiomes = new List<string>
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

                    if (Main.hardMode)
                    {
                        availableBiomes.AddRange(new string[] { "Underground Corruption", "Underground Crimson", "Underground Hallow" });
                    }

                    availableBiomes.RemoveAll(b => blacklistedBiomes.Contains(b));

                    if (availableBiomes.Count == 0)
                    {
                        caller.Reply("No available biomes to start an event (all biomes are blacklisted).");
                        return;
                    }

                    string randomBiome = availableBiomes[Main.rand.Next(availableBiomes.Count)];
                    EventSystem.ForceStartEvent(randomEvent, randomBiome);
                    caller.Reply($"Started {randomEvent.EventName} in {randomBiome}.");
                }
            }
        }
    }
}
