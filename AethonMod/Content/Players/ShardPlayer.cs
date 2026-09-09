using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Players
{
    public class ShardPlayer : ModPlayer
    {
        public int ResonanceShards = 0;
        public bool FirstLevelUpTriggered = false;

        /// <summary>
        /// Flag activado por el buff "Empoderamiento Cósmico" (Sello de Aethon).
        /// Se resetea en ResetEffects. El lifesteal se aplica en
        /// GlobalNPCXP.ApplyCosmicEmpowermentLifesteal (OnHitByItem / OnHitByProjectile).
        /// </summary>
        public bool HasCosmicEmpowerment = false;

        /// <summary>
        /// Flag activado por bastones de prueba con lifesteal mejorado.
        /// Aplica 4% de lifesteal extra (total 5% combinado con CosmicEmpowerment).
        /// </summary>
        public bool HasEnhancedLifesteal = false;

        /// <summary>
        /// ResetEffects: al inicio de cada frame, reseteamos los flags
        /// que son aplicados por buffs/equipamiento.
        /// </summary>
        public override void ResetEffects()
        {
            HasCosmicEmpowerment = false;
            HasEnhancedLifesteal = false;
        }

        /// <summary>
        /// PostUpdateEquips: aplica los bonuses del Grimorio que persisten
        /// aunque cambies de arma. Busca el Grimorio en TODO el inventario.
        /// </summary>
        public override void PostUpdateEquips()
        {
            for (int i = 0; i < 58; i++)
            {
                Item item = Player.inventory[i];
                if (item == null || item.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;

                try
                {
                    var sl = item.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                    {
                        int level = sl.Level;
                        // Slots de minion
                        Player.maxMinions += WeaponScaling.BonusMinionSlots(level);
                        // Mana max
                        Player.statManaMax2 += WeaponScaling.BonusMana(level);
                        // Vida max
                        Player.statLifeMax2 += WeaponScaling.BonusLife(level);
                        break;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// PostUpdate: regeneración de mana y vida + reducción de daño.
        /// </summary>
        public override void PostUpdate()
        {
            int level = 0;
            for (int i = 0; i < 58; i++)
            {
                Item item = Player.inventory[i];
                if (item == null || item.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;
                try
                {
                    var sl = item.GetGlobalItem<ShardLevelItem>();
                    if (sl != null) { level = sl.Level; break; }
                }
                catch { }
            }
            if (level == 0) return;

            // Regeneración de mana (por segundo, 60 frames)
            int manaRegen = WeaponScaling.ManaRegen(level);
            if (manaRegen > 0 && Player.statMana < Player.statManaMax2)
            {
                // Distribuir el regen a lo largo de 60 frames
                Player.manaRegen += manaRegen * 60 / 60;
            }

            // Regeneración de vida (por segundo)
            float lifeRegen = WeaponScaling.LifeRegen(level);
            if (lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
            {
                Player.lifeRegen += (int)(lifeRegen * 2); // lifeRegen es en 1/2 vida/seg
            }
        }

        /// <summary>
        /// Modifica el daño recibido (reducción por nivel del Grimorio).
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            int level = 0;
            for (int i = 0; i < 58; i++)
            {
                Item item = Player.inventory[i];
                if (item == null || item.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;
                try
                {
                    var sl = item.GetGlobalItem<ShardLevelItem>();
                    if (sl != null) { level = sl.Level; break; }
                }
                catch { }
            }
            if (level == 0) return;

            float reduction = WeaponScaling.DamageReduction(level);
            if (reduction > 0f)
            {
                modifiers.FinalDamage *= 1f - reduction;
            }
        }

        public override void OnHurt(Player.HurtInfo info) { }

        public override void SaveData(TagCompound tag)
        {
            tag["resonanceShards"] = ResonanceShards;
            tag["firstLevelUpTriggered"] = FirstLevelUpTriggered;
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ResonanceShards = tag.GetInt("resonanceShards");
                FirstLevelUpTriggered = tag.GetBool("firstLevelUpTriggered");
            }
            catch { }
        }
    }
}
