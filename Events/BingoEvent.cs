using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Chat;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    public class BingoEvent : Event
    {
        private const int GridSize = 9; // 3x3
        private int[] boardItems; // item types for the board
        private Dictionary<string, bool[]> playerProgress; // guid -> mask of found grid items for that player
        private HashSet<string> rewardedPlayers; // who has been awarded for a bingo
        private Mod checklistMod;
        private Action<int, string, string> discoveryCallback;

        public BingoEvent() : base("Bingo Event", biomeSpecific: false)
        {
            boardItems = new int[GridSize];
            for (int i = 0; i < GridSize; i++) boardItems[i] = -1;
            playerProgress = new Dictionary<string, bool[]>();
            rewardedPlayers = new HashSet<string>();
            discoveryCallback = OnChecklistDiscovery;
        }

        protected override void OnEventStart()
        {
            // Force this event to be 1 hour long (1 hour * 60 minutes * 60 seconds) times 60 (ticks)
            Duration = 60 * 60; // 60 minutes in seconds
            RemainingDuration = Duration;

            // Attempt to find and integrate with the MultiplayerItemChecklist
            if (ModLoader.TryGetMod("MultiplayerItemChecklist", out Mod checklist))
            {
                checklistMod = checklist;
                // Build the board
                BuildBoard(checklist);
                // Register for discovery callbacks (server-side)
                checklist.Call("RegisterForNewItem", discoveryCallback);
            }
            else
            {
                // If no checklist mod, just choose random tracked items from the world as a fallback
                BuildBoardFallback();
            }

            // Announce and show the 3x3 board in chat
            BroadcastBoard();
        }

        protected override void OnEventEnd()
        {
            // Unregister callbacks
            if (checklistMod != null)
            {
                try { checklistMod.Call("UnregisterForNewItem", discoveryCallback); } catch { }
                checklistMod = null;
            }

            // Clear state
            Array.Fill(boardItems, -1);
            playerProgress.Clear();
            rewardedPlayers.Clear();
        }

        // Build board using the checklist's undiscovered tracked items, preferring items from this mod if possible
        private void BuildBoard(Mod checklist)
        {
            var res = checklist.Call("GetUndiscoveredItemIds");
            int[] undiscovered = res as int[] ?? new int[0];

            List<int> pool = new List<int>();
            // Prefer items originating from this mod (same name) where possible
            foreach (int t in undiscovered)
            {
                if (t <= 0 || t >= ItemLoader.ItemCount)
                    continue;
                var modItem = ItemLoader.GetItem(t);
                if (modItem != null && modItem.Mod != null && modItem.Mod.Name == ModContent.GetInstance<DamageEditor>().Name)
                    pool.Add(t);
            }

            // If not enough preferred items, fall back to all undiscovered
            if (pool.Count < GridSize)
            {
                var needed = undiscovered.Where(i => !pool.Contains(i) && i >= 0).ToList();
                pool.AddRange(needed);
            }

            // As a last resort include any findable items
            if (pool.Count < GridSize)
            {
                var all = checklist.Call("GetTrackedItemIds") as int[] ?? new int[0];
                pool.AddRange(all.Where(i => !pool.Contains(i)));
            }

            // Pick 9 random distinct items
            var rand = Main.rand;
            var chosen = new List<int>();
            var poolArr = pool.ToArray();
            if (poolArr.Length == 0)
            {
                // Nothing to choose
                for (int i = 0; i < GridSize; i++) boardItems[i] = -1;
                return;
            }

            var available = poolArr.ToList();
            for (int i = 0; i < GridSize; i++)
            {
                if (available.Count == 0) break;
                int idx = available[rand.Next(available.Count)];
                chosen.Add(idx);
                available.Remove(idx);
            }

            for (int i = 0; i < GridSize; i++)
            {
                boardItems[i] = i < chosen.Count ? chosen[i] : -1;
            }
        }

        // Fallback if checklist mod is missing - pick arbitrary tracked items
        private void BuildBoardFallback()
        {
            List<int> found = new List<int>();
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                if (i <= 0 || i >= ItemLoader.ItemCount) continue;
                found.Add(i);
            }
            var rand = Main.rand;
            for (int i = 0; i < GridSize; i++)
                boardItems[i] = found[rand.Next(found.Count)];
        }

        public void BroadcastBoard()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Bingo Event started! 3x3 Board:");
            for (int r = 0; r < 3; r++)
            {
                var row = new List<string>();
                for (int c = 0; c < 3; c++)
                {
                    int idx = r * 3 + c;
                    int itemType = boardItems[idx];
                    if (itemType <= 0)
                    {
                        row.Add("[none]");
                        continue;
                    }
                    Item item = null;
                    if (!ContentSamples.ItemsByType.TryGetValue(itemType, out item) || item == null)
                    {
                        item = new Item();
                        item.SetDefaults(itemType);
                    }
                    if (item.ModItem?.Mod != null && item.ModItem.Mod.Name != "Terraria")
                        row.Add($"[i:{item.ModItem.Mod.Name}/{item.Name}]");
                    else
                        row.Add($"[i:{itemType}]");
                }
                sb.AppendLine(string.Join(" ", row));
            }

            string message = sb.ToString();
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.LightGreen);
            }
            else
            {
                Main.NewText(message, Color.LightGreen);
            }
        }

        private void OnChecklistDiscovery(int itemType, string discovererGuid, string discovererName)
        {
            // Only process on server/host
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (discovererGuid == null) discovererGuid = "";
            // Find indices in grid that match the itemType
            var matchedIndices = new List<int>();
            for (int i = 0; i < boardItems.Length; i++)
            {
                if (boardItems[i] == itemType)
                    matchedIndices.Add(i);
            }
            if (matchedIndices.Count == 0) return; // not in our board

            if (!playerProgress.TryGetValue(discovererGuid, out var mask))
            {
                mask = new bool[GridSize];
                playerProgress[discovererGuid] = mask;
            }

            bool changed = false;
            foreach (int idx in matchedIndices)
            {
                if (!mask[idx])
                {
                    mask[idx] = true;
                    changed = true;
                }
            }

            if (!changed) return;

            // Check if the player completed any line
            if (!rewardedPlayers.Contains(discovererGuid) && CheckPlayerHasBingo(mask))
            {
                // Award the player
                rewardedPlayers.Add(discovererGuid);
                // Try to find Player object by name
                Player awarded = null;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    var pl = Main.player[i];
                    if (pl != null && pl.active && pl.name == discovererName)
                    {
                        awarded = pl;
                        break;
                    }
                }

                if (awarded != null)
                {
                    // Spawn a modest reward (1 gold coin)
                    try
                    {
                        awarded.QuickSpawnItem(awarded.GetSource_GiftOrReward(), ItemID.GoldCoin, 1);
                    }
                    catch { }
                    string rewardMsg = $"{awarded.name} completed a Bingo row/col/diagonal and received a reward!";
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(rewardMsg), Color.Gold);
                }
                else
                {
                    // If player not found (e.g., Uncredited), broadcast it
                    string rewardMsg = $"{discovererName ?? discovererGuid} completed a Bingo row/col/diagonal!";
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(rewardMsg), Color.Gold);
                }
            }
        }

        private bool CheckPlayerHasBingo(bool[] mask)
        {
            // Rows
            for (int r = 0; r < 3; r++)
            {
                if (mask[r * 3] && mask[r * 3 + 1] && mask[r * 3 + 2]) return true;
            }
            // Cols
            for (int c = 0; c < 3; c++)
            {
                if (mask[c] && mask[c + 3] && mask[c + 6]) return true;
            }
            // Diagonals
            if (mask[0] && mask[4] && mask[8]) return true;
            if (mask[2] && mask[4] && mask[6]) return true;
            return false;
        }

        public int[] GetBoardArray()
        {
            return boardItems.ToArray();
        }

        public void SetBoardFromArray(int[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < Math.Min(arr.Length, GridSize); i++)
                boardItems[i] = arr[i];
        }

        public override bool CanPlayerBenefit(Player player)
        {
            // Global event
            return IsActive;
        }
    }
}
