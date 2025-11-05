using Terraria.ModLoader;
using Terraria;
using Terraria.ModLoader.Config;

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
            var config = ModContent.GetInstance<DamageEditorConfig>();
            config.DealDamage = amount;
            caller.Reply($"DealDamage set to {amount}.");
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
            var config = ModContent.GetInstance<DamageEditorConfig>();
            config.TakeDamage = amount;
            caller.Reply($"TakeDamage set to {amount}.");
        }
    }
}
