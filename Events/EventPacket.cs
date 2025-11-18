using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    internal static class EventPacket
    {
        public static void SendEventState()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = ModContent.GetInstance<DamageEditor>().GetPacket();
            packet.Write((byte)0);

            if (EventSystem.ActiveEvent != null)
            {
                packet.Write(true);
                packet.Write(EventSystem.RegisteredEvents.IndexOf(EventSystem.ActiveEvent));
                packet.Write(EventSystem.ActiveEvent.TargetBiomeName ?? "");
                packet.Write(EventSystem.ActiveEvent.RemainingDuration);
                // If it's a BingoEvent, include its board
                if (EventSystem.ActiveEvent is BingoEvent bingo)
                {
                    packet.Write(true);
                    int[] board = bingo.GetBoardArray();
                    for (int i = 0; i < (board?.Length ?? 0); i++)
                        packet.Write(board[i]);
                }
                else
                {
                    packet.Write(false);
                }

                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Main.NewText($"[DEBUG] EventPacket sending: Event='{EventSystem.ActiveEvent.EventName}' Biome='{EventSystem.ActiveEvent.TargetBiomeName}' Duration={EventSystem.ActiveEvent.RemainingDuration}", 200, 200, 100);
            }
            else
            {
                packet.Write(false);

                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Main.NewText("[DEBUG] EventPacket sending: Event deactivated", 200, 200, 100);
            }

            if (Main.netMode == NetmodeID.Server)
                packet.Send();
            else
                packet.Send(-1, Main.myPlayer);
        }

        public static void ReceiveEventState(BinaryReader reader)
        {
            bool hasActive = reader.ReadBoolean();

            if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                Main.NewText($"[DEBUG] EventPacket received: hasActive={hasActive}", 100, 200, 200);

            if (hasActive)
            {
                int eventIndex = reader.ReadInt32();
                string biome = reader.ReadString();
                int duration = reader.ReadInt32();
                bool hasBingoData = reader.ReadBoolean();
                int[] bingoBoard = null;
                if (hasBingoData)
                {
                    bingoBoard = new int[9];
                    for (int i = 0; i < 9; i++) bingoBoard[i] = reader.ReadInt32();
                }

                if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                    Main.NewText($"[DEBUG] EventPacket received: eventIndex={eventIndex}, biome={biome}, duration={duration}", 100, 200, 200);

                if (EventSystem.RegisteredEvents != null && eventIndex >= 0 && eventIndex < EventSystem.RegisteredEvents.Count)
                {
                    bool isNewEvent = EventSystem.ActiveEvent == null ||
                                      EventSystem.RegisteredEvents.IndexOf(EventSystem.ActiveEvent) != eventIndex ||
                                      EventSystem.ActiveEvent.TargetBiomeName != biome;

                        if (isNewEvent)
                        {
                            EventSystem.SetActiveEvent(EventSystem.RegisteredEvents[eventIndex]);
                            // If we're on a client, display the start message. On the server, the event was already
                            // initialized and the server should not show the message again to avoid duplication.
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                EventSystem.ActiveEvent.StartEvent(biome);
                                    // If this is a BingoEvent and the packet provides board data, restore it
                                    if (EventSystem.ActiveEvent is BingoEvent bingo && bingoBoard != null)
                                    {
                                        bingo.SetBoardFromArray(bingoBoard);
                                        // Re-broadcast board visually for clients to ensure they match the server
                                        bingo.BroadcastBoard();
                                    }
                            }
                            else
                            {
                                // Server: initialize state silently
                                EventSystem.ActiveEvent.InitializeEvent(biome);
                            }

                        if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                            Main.NewText($"[DEBUG] EventPacket: Started NEW event '{EventSystem.ActiveEvent.EventName}' in biome '{biome}'", 100, 255, 150);
                    }
                    else
                    {
                        if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                            Main.NewText("[DEBUG] EventPacket: Event already active, just updating duration", 100, 200, 150);
                    }

                    EventSystem.ActiveEvent.SetRemainingDuration(duration);
                }
                else
                {
                    if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                        Main.NewText("[DEBUG] EventPacket: FAILED - eventIndex out of range or RegisteredEvents is null!", 255, 100, 100);
                }
            }
                else
                {
                    if (EventSystem.ActiveEvent != null)
                    {
                        if (ModContent.GetInstance<DamageEditorConfig>()?.Events?.DebugMode == true)
                            Main.NewText($"[DEBUG] EventPacket: Deactivating event (was '{EventSystem.ActiveEvent.EventName}')", 200, 100, 200);

                        // If client, show the end message; server should not re-announce its own end event
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            EventSystem.ActiveEvent.ShowEndMessage();
                        }
                        EventSystem.SetActiveEvent(null);
                    }
                }
        }
    }
}
