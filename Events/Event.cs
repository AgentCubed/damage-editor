using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DamageEditor.Events
{
    public abstract class Event
    {
        public string EventName { get; protected set; }
        public string TargetBiomeName { get; protected set; }
        public bool BiomeSpecific { get; protected set; }
        public bool IsActive { get; protected set; }
        public int Duration { get; protected set; }
        public int RemainingDuration { get; protected set; }

        protected Event(string eventName, bool biomeSpecific = false)
        {
            EventName = eventName;
            BiomeSpecific = biomeSpecific;
            RemainingDuration = 0;
            IsActive = false;
        }

        public void SetRemainingDuration(int duration)
        {
            RemainingDuration = duration;
        }

        public virtual void StartEvent(string targetBiome = null)
        {
            // Initialize state and show the start message locally
            InitializeEvent(targetBiome);
            ShowStartMessage();
        }

        public virtual void EndEvent()
        {
            // Show end message first, then initialize end state so message includes the biome name
            ShowEndMessage();
            InitializeEndEvent();
        }

        // Initialize only the internal event state without displaying messages.
        public void InitializeEvent(string targetBiome)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            Duration = (config?.Events?.EventDurationMinutes ?? 5) * 60 * 60;

            IsActive = true;
            TargetBiomeName = targetBiome;
            RemainingDuration = Duration;
            OnEventStart();
        }

        // Initialize only internal state for ending an event without displaying messages.
        public void InitializeEndEvent()
        {
            IsActive = false;
            TargetBiomeName = null;
            RemainingDuration = 0;
            OnEventEnd();
        }

        // Display the localized start message only (no networking/state changes).
        public void ShowStartMessage()
        {
            string location = !BiomeSpecific ? "the world" : GetBiomeDisplayName(TargetBiomeName);
            var message = NetworkText.FromKey("Mods.DamageEditor.Events.Event.Start", EventName, location);
            var color = new Microsoft.Xna.Framework.Color(50, 255, 130);
            Main.NewText(message.ToString(), color);
        }

        // Display the localized end message only (no networking/state changes).
        public void ShowEndMessage()
        {
            string location = !BiomeSpecific ? "the world" : GetBiomeDisplayName(TargetBiomeName);
            var message = NetworkText.FromKey("Mods.DamageEditor.Events.Event.End", EventName, location);
            var color = new Microsoft.Xna.Framework.Color(255, 200, 50);
            Main.NewText(message.ToString(), color);
        }

        private string GetBiomeDisplayName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName))
                return "Unknown";

            return internalName switch
            {
                "Forest" => "Forest",
                "Desert" => "Desert",
                "Snow" => "Snow",
                "Jungle" => "Jungle",
                "Ocean" => "Ocean",
                "Underground" => "Underground",
                "Caverns" => "Caverns",
                "Underground Desert" => "Underground Desert",
                "Underground Snow" => "Underground Snow",
                "Underground Jungle" => "Underground Jungle",
                "Underground Corruption" => "Underground Corruption",
                "Underground Crimson" => "Underground Crimson",
                "Underground Hallow" => "Underground Hallow",
                _ => internalName
            };
        }

        public virtual void Update()
        {
            if (!IsActive)
                return;

            RemainingDuration--;

            if (RemainingDuration <= 0)
            {
                EndEvent();
            }
        }

        public abstract bool CanPlayerBenefit(Player player);

        protected virtual void OnEventStart() { }
        protected virtual void OnEventEnd() { }
    }
}
