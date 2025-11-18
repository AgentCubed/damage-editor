using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DamageEditor.Boss
{
    public sealed class BossGlobalNPC : GlobalNPC
    {
        private const double DefaultExpectedMinutes = 12.0;
        private const double DeadZoneDivisor = 5.0;
        private const double ScalingConstant = 1.0;
        private const double MaxModifier = 50.0;
        private const int HitPointBuckets = 10;
        private const int FullHealthPercent = 100;
        private const float Range = 300f * 16f;
        private const int AggroBoost = 1500;

        private static int totalBossDeaths;
        private Dictionary<int, int> originalAggro = new Dictionary<int, int>();

        private double spawnTime = -1.0;
        private double currentDefenseModifier = 1.0;
        private double currentOffenseModifier = 1.0;
        private double lastTimeDifferenceMinutes = 0.0;
        private int lastHpInterval = FullHealthPercent;
        private double idealTotalTicks = DefaultExpectedMinutes * 3600.0;
        private double deadZoneMinutes = DefaultExpectedMinutes / DeadZoneDivisor;
        // Weapon adaptation state
        private Dictionary<int, double> weaponDamage = new Dictionary<int, double>();
        private Dictionary<int, float> weaponAdaptationFactor = new Dictionary<int, float>();
        private HashSet<int> weaponAdaptationWarned = new HashSet<int>();
        private double totalWeaponDamage = 0.0;
        private int topWeaponKey = 0;
        private double topWeaponDamage = 0.0;

        public override bool InstancePerEntity => true;

        public int PlayerDeathsThisFight { get; private set; }

        public static int TotalBossDeaths => totalBossDeaths;

        public static void RecordBossDeath()
        {
            totalBossDeaths++;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.boss)
                {
                    continue;
                }

                npc.GetGlobalNPC<BossGlobalNPC>().PlayerDeathsThisFight++;
            }
        }

        public static int GetCurrentFightDeathCount()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.boss)
                {
                    continue;
                }

                return npc.GetGlobalNPC<BossGlobalNPC>().PlayerDeathsThisFight;
            }

            return 0;
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (!npc.boss)
            {
                return;
            }

            ResetForNewBoss();
            ApplyConfigOverrides();
        }

        public override void ResetEffects(NPC npc)
        {
            // not used for bosses here, but ResetForNewBoss handles resets on spawn
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (!npc.boss || spawnTime < 0)
            {
                return;
            }

            double timeAlive = Main.time - spawnTime;
            if (timeAlive <= 60.0)
            {
                return;
            }

            double hpPercent = npc.life / (double)npc.lifeMax;
            int currentHpInterval = CalculateHpInterval(hpPercent);

            if (currentHpInterval < lastHpInterval)
            {
                UpdatePaceModifiers(timeAlive, currentHpInterval);
                EmitDebugText(hpPercent);
            }

            ApplyDamageModifiers(ref modifiers);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (!npc.boss)
            {
                return;
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss?.WeaponAdaptationEnabled != true)
            {
                return;
            }

            int key = GetWeaponKeyFromItem(item);
            if (weaponAdaptationFactor.TryGetValue(key, out float factor) && factor < 1f)
            {
                modifiers.FinalDamage *= factor;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!npc.boss)
            {
                return;
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss?.WeaponAdaptationEnabled != true)
            {
                return;
            }

            int key = GetWeaponKeyFromProjectile(projectile);
            if (weaponAdaptationFactor.TryGetValue(key, out float factor) && factor < 1f)
            {
                modifiers.FinalDamage *= factor;
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (!npc.boss)
            {
                return base.PreAI(npc);
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss?.TargetHighestHealth != true)
            {
                return base.PreAI(npc);
            }

            originalAggro.Clear();
            int targetPlayer = FindHighestHealthPlayer(npc);

            if (targetPlayer >= 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead)
                    {
                        originalAggro[i] = player.aggro;

                        if (i == targetPlayer)
                        {
                            player.aggro = AggroBoost;
                        }
                    }
                }
            }

            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            if (!npc.boss)
            {
                return;
            }

            foreach (var kvp in originalAggro)
            {
                if (Main.player[kvp.Key].active)
                {
                    Main.player[kvp.Key].aggro = kvp.Value;
                }
            }
            originalAggro.Clear();
        }

        private int FindHighestHealthPlayer(NPC npc)
        {
            int bestPlayer = -1;
            int highestHealth = 0;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.ghost)
                {
                    continue;
                }

                float distance = Vector2.Distance(npc.Center, player.Center);

                if (distance <= Range)
                {
                    if (player.statLife > highestHealth || (player.statLife == highestHealth && distance < bestDistance))
                    {
                        highestHealth = player.statLife;
                        bestPlayer = i;
                        bestDistance = distance;
                    }
                }
            }

            return bestPlayer;
        }

        private static int CalculateHpInterval(double hpPercent)
        {
            return (int)Math.Floor(hpPercent * HitPointBuckets) * (FullHealthPercent / HitPointBuckets);
        }

        private void ResetForNewBoss()
        {
            spawnTime = Main.time;
            currentDefenseModifier = 1.0;
            currentOffenseModifier = 1.0;
            lastHpInterval = FullHealthPercent;
            lastTimeDifferenceMinutes = 0.0;
            PlayerDeathsThisFight = 0;
            SetDefaultConfigValues();
            weaponDamage.Clear();
            weaponAdaptationFactor.Clear();
            weaponAdaptationWarned.Clear();
            totalWeaponDamage = 0.0;
            topWeaponKey = 0;
            topWeaponDamage = 0.0;
        }

        private void ApplyConfigOverrides()
        {
            try
            {
                var config = ModContent.GetInstance<DamageEditorConfig>();
                if (config?.Boss?.ExpectedTotalMinutes > 0)
                {
                    idealTotalTicks = config.Boss.ExpectedTotalMinutes * 3600.0;
                    deadZoneMinutes = config.Boss.ExpectedTotalMinutes / DeadZoneDivisor;
                }
            }
            catch
            {
                SetDefaultConfigValues();
            }
        }

        private void SetDefaultConfigValues()
        {
            idealTotalTicks = DefaultExpectedMinutes * 3600.0;
            deadZoneMinutes = DefaultExpectedMinutes / DeadZoneDivisor;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (!npc.boss)
            {
                return;
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss?.WeaponAdaptationEnabled != true)
            {
                return;
            }

            int key = GetWeaponKeyFromProjectile(projectile);
            RecordWeaponDamage(npc, key, damageDone);
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (!npc.boss)
            {
                return;
            }

            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss?.WeaponAdaptationEnabled != true)
            {
                return;
            }

            int key = GetWeaponKeyFromItem(item);
            RecordWeaponDamage(npc, key, damageDone);
        }

        private void RecordWeaponDamage(NPC npc, int weaponKey, int amount)
        {
            if (weaponKey == 0 || amount <= 0)
            {
                return;
            }

            totalWeaponDamage += amount;

            if (weaponDamage.TryGetValue(weaponKey, out double cur))
            {
                cur += amount;
                weaponDamage[weaponKey] = cur;
            }
            else
            {
                cur = amount;
                weaponDamage[weaponKey] = cur;
            }

            if (cur > topWeaponDamage)
            {
                topWeaponKey = weaponKey;
                topWeaponDamage = cur;
            }

            CheckAdaptationForWeapon(npc, weaponKey);
        }

        private int GetWeaponKeyFromItem(Item item)
        {
            if (item == null)
            {
                return ItemID.None;
            }

            return item.type + 1; // positive key for items
        }

        private int GetWeaponKeyFromProjectile(Projectile projectile)
        {
            if (projectile == null)
            {
                return 0;
            }

            // Prefer mapping to the player's current held item if available
            if (projectile.TryGetOwner(out Player player) && player != null && player.HeldItem != null && player.HeldItem.type != ItemID.None)
            {
                return player.HeldItem.type + 1;
            }

            // fallback to projectile type as negative key
            return -(projectile.type + 1);
        }

        private void CheckAdaptationForWeapon(NPC npc, int weaponKey)
        {
            var config = ModContent.GetInstance<DamageEditorConfig>();
            if (config?.Boss == null)
            {
                return;
            }

            if (!weaponDamage.TryGetValue(weaponKey, out double weaponDmg))
            {
                return;
            }

            int weaponCount = weaponDamage.Count;
            if (weaponCount <= 1)
            {
                return;
            }

            double meanOthers = (totalWeaponDamage - weaponDmg) / Math.Max(1, weaponCount - 1);
            if (meanOthers <= 0.0)
            {
                return;
            }

            double startMultiplier = config.Boss.WeaponAdaptationStartMultiplier;
            double completeMultiplier = config.Boss.WeaponAdaptationCompleteMultiplier;
            double minDamage = config.Boss.WeaponAdaptationMinDamage;
            double maxReduction = config.Boss.WeaponAdaptationMaxReduction;

            if (weaponDmg < minDamage)
            {
                return;
            }

            double ratio = meanOthers > 0.0 ? weaponDmg / meanOthers : double.PositiveInfinity;

            // warn if surpass start multiplier, but not yet adapted
            if (ratio >= startMultiplier && !weaponAdaptationWarned.Contains(weaponKey) && !weaponAdaptationFactor.ContainsKey(weaponKey))
            {
                weaponAdaptationWarned.Add(weaponKey);
                string weaponName = GetWeaponNameFromKey(weaponKey);
                Main.NewText($"{npc.GivenOrTypeName} is beginning to adapt to {weaponName}...", Color.Orange);
            }

            // check for adaptation trigger
            if (ratio >= completeMultiplier)
            {
                // compute factor to reduce future damage to be close to mean but not below maxReduction
                float factor = (float)Math.Max(maxReduction, Math.Min(1.0, meanOthers / weaponDmg));
                if (weaponAdaptationFactor.TryGetValue(weaponKey, out float existingFactor))
                {
                    if (factor < existingFactor - 0.001f)
                    {
                        weaponAdaptationFactor[weaponKey] = factor;
                        string weaponName = GetWeaponNameFromKey(weaponKey);
                        Main.NewText($"{npc.GivenOrTypeName} has further adapted to {weaponName}!", Color.Yellow);
                    }
                }
                else
                {
                    weaponAdaptationFactor[weaponKey] = factor;
                    string weaponName = GetWeaponNameFromKey(weaponKey);
                    Main.NewText($"{npc.GivenOrTypeName} has successfully adapted to {weaponName}!", Color.Yellow);
                }
            }
        }

        private string GetWeaponNameFromKey(int key)
        {
            if (key > 0)
            {
                int itemType = key - 1;
                return Lang.GetItemNameValue(itemType);
            }
            int projType = -key - 1;
            return Lang.GetProjectileName(projType).Value;
        }

        private void UpdatePaceModifiers(double timeAlive, int currentHpInterval)
        {
            double hpLost = 1.0 - currentHpInterval / (double)FullHealthPercent;
            double idealTime = idealTotalTicks * hpLost;
            double timeDifferenceMinutes = (timeAlive - idealTime) / 3600.0;
            lastTimeDifferenceMinutes = timeDifferenceMinutes;

            double absTimeDiff = Math.Abs(timeDifferenceMinutes);

            if (absTimeDiff <= deadZoneMinutes)
            {
                currentDefenseModifier = 1.0;
                currentOffenseModifier = 1.0;
            }
            else
            {
                double scaledDifference = absTimeDiff - deadZoneMinutes;
                double modifier = 1.0 + ScalingConstant * Math.Pow(scaledDifference, 2);
                modifier = Math.Min(modifier, MaxModifier);

                if (timeDifferenceMinutes > 0)
                {
                    currentOffenseModifier = modifier;
                    currentDefenseModifier = 1.0;
                }
                else
                {
                    currentDefenseModifier = modifier;
                    currentOffenseModifier = 1.0;
                }
            }

            lastHpInterval = currentHpInterval;
        }

        private void EmitDebugText(double hpPercent)
        {
            string paceMessage = "On Pace";
            double displayDefMod = 1.0;

            if (currentOffenseModifier > 1.0)
            {
                paceMessage = $"Pace: +{lastTimeDifferenceMinutes:F1} min";
                displayDefMod = 1.0 / currentOffenseModifier;
            }
            else if (currentDefenseModifier > 1.0)
            {
                paceMessage = $"Pace: {lastTimeDifferenceMinutes:F1} min";
                displayDefMod = currentDefenseModifier;
            }

            // Main.NewText($"{(int)(hpPercent * FullHealthPercent)}% HP | {paceMessage} | {displayDefMod:F2}x Def", Color.Gray);
        }

        private void ApplyDamageModifiers(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage /= (float)currentDefenseModifier;
            modifiers.FinalDamage *= (float)currentOffenseModifier;
        }
    }
}
