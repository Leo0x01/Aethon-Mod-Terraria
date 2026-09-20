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
        /// PostUpdateBuffs: regeneración de mana y vida.
        /// v6.44 (auditoría R44): EL HOOK CORRECTO. En el binario real,
        /// Player.Update consume lifeRegen/manaRegen en UpdateLifeRegen y
        /// UpdateManaRegen, que corren ANTES de PostUpdate — las escritas
        /// del hook viejo eran letra muerta (ResetEffects las borra al
        /// tick siguiente sin que nadie las lea). PostUpdateBuffs corre
        /// entre ResetEffects y el consumo: aquí sí cuentan.
        /// </summary>
        public override void PostUpdateBuffs()
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

            // Regeneración de mana (por segundo)
            // v6.44 (auditoría R44): Player.manaRegen lo REESCRIBE
            // UpdateManaRegen desde cero cada tick — el campo que
            // ACUMULA aportes externos es manaRegenBonus. Unidades del
            // motor: 120 cuentas = 1 maná → para R maná/seg hay que
            // aportar 2·R cuentas por tick (el viejo `* 60 / 60` era un
            // no-op además de letra muerta).
            int manaRegen = WeaponScaling.ManaRegen(level);
            if (manaRegen > 0 && Player.statMana < Player.statManaMax2)
            {
                Player.manaRegenBonus += manaRegen * 2;
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
