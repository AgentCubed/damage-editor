using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DamageEditor.Events;
using Terraria.ModLoader;

namespace DamageEditor
{
    public class DamageEditor : Mod
    {
        // Keep references to delegates to avoid GC and allow unregistering on unload
        private Action<int> onChecklistItemDiscoveredInt;
        private Action<int, string, string> onChecklistItemDiscoveredFull;

        public override void Load()
        {
            base.Load();

            // Register with MultiplayerItemChecklist if present
            if (ModLoader.TryGetMod("MultiplayerItemChecklist", out Mod checklist))
            {
                try
                {
                    onChecklistItemDiscoveredInt = OnChecklistItemDiscovered;
                    checklist.Call("RegisterForNewItem", onChecklistItemDiscoveredInt);

                    onChecklistItemDiscoveredFull = OnChecklistItemDiscoveredFull;
                    checklist.Call("RegisterForNewItem", onChecklistItemDiscoveredFull);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DamageEditor] Failed to register for checklist discoveries: {ex}");
                }
            }
        }

        public override void Unload()
        {
            // Unregister callbacks to avoid resource/leak issues on reload
            if (ModLoader.TryGetMod("MultiplayerItemChecklist", out Mod checklist))
            {
                try
                {
                    if (onChecklistItemDiscoveredInt != null)
                    {
                        checklist.Call("UnregisterForNewItem", onChecklistItemDiscoveredInt);
                        onChecklistItemDiscoveredInt = null;
                    }
                    if (onChecklistItemDiscoveredFull != null)
                    {
                        checklist.Call("UnregisterForNewItem", onChecklistItemDiscoveredFull);
                        onChecklistItemDiscoveredFull = null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DamageEditor] Failed to unregister from checklist discoveries: {ex}");
                }
            }

            base.Unload();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte packetType = reader.ReadByte();

            switch (packetType)
            {
                case 0:
                    // Forward to whatever packet handler the existing mod uses; keep unchanged behavior
                    EventPacket.ReceiveEventState(reader);
                    break;
            }
        }

        private void OnChecklistItemDiscovered(int itemType)
        {
            try
            {
                Logger.Info($"[DamageEditor] Checklist discovered item: type={itemType}");
                // TODO: Implement DamageEditor-specific behavior for a discovered item
            }
            catch (Exception ex)
            {
                Logger.Error($"[DamageEditor] OnChecklistItemDiscovered handler threw: {ex}");
            }
        }

        private void OnChecklistItemDiscoveredFull(int itemType, string discovererGuid, string discovererName)
        {
            try
            {
                Logger.Info($"[DamageEditor] Checklist discovered item: type={itemType}, discoverer={discovererName} ({discovererGuid})");
                // TODO: Implement DamageEditor-specific behavior using discoverer info
            }
            catch (Exception ex)
            {
                Logger.Error($"[DamageEditor] OnChecklistItemDiscoveredFull handler threw: {ex}");
            }
        }
    }
}
