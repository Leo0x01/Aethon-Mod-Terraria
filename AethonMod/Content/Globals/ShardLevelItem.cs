using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalItem — guarda el nivel y XP de CADA arma Aethon individualmente.
    ///
    /// Esto permite que cada arma tenga su propio progreso independiente:
    /// - El SolbrandEdge que el jugador elige sube de nivel con kills.
    /// - Las otras 2 armas (Lumina/Grimorio) NO suben hasta que sean elegidas.
    /// - Si el jugador consigue otra copia del mismo item, esa copia empieza en nivel 1.
    ///
    /// El nivel se persiste en el item via SaveData/LoadData del TagCompound.
    /// </summary>
    public class ShardLevelItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        /// <summary>Nivel actual del arma (1 = sin XP todavía).</summary>
        public int Level = 1;
        /// <summary>XP acumulada hacia el próximo nivel.</summary>
        public int XP = 0;
        /// <summary>
        /// True si este item ya disparó su evento global de primera subida.
        /// Cada item tiene su propio flag (no es global por personaje).
        /// </summary>
        public bool FirstLevelUpTriggered = false;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            // Solo aplica a las 3 armas Aethon.
            return item.type == ModContent.ItemType<Weapons.SolbrandEdge>() ||
                   item.type == ModContent.ItemType<Weapons.LuminaStarbow>() ||
                   item.type == ModContent.ItemType<Weapons.GrimoireEternal>() ||
                   item.type == ModContent.ItemType<Items.GenesisShard>();
        }

        /// <summary>
        /// XP necesaria para subir al próximo nivel.
        /// Nivel 1→2: 1 XP (cualquier kill).
        /// Nivel 2+: 80 * nivel^1.5.
        /// </summary>
        public int XPForNextLevel()
        {
            if (Level <= 1) return 1;
            return (int)(80 * System.Math.Pow(Level, 1.5));
        }

        /// <summary>
        /// Otorga XP a ESTE item específico. Si sube de nivel, dispara OnLevelUp.
        /// </summary>
        public void GrantXP(int amount)
        {
            // Solo sube de nivel si es un arma (no el FragmentoGenesis base).
            // El FragmentoGenesis no tiene nivel de arma (se transforma al elegir rama).
            if (Item.type == ModContent.ItemType<Items.GenesisShard>()) return;

            XP += amount;
            while (XP >= XPForNextLevel())
            {
                XP -= XPForNextLevel();
                Level++;
                OnLevelUp();
            }
        }

        /// <summary>
        /// Se llama cuando ESTE item sube de nivel.
        /// </summary>
        private void OnLevelUp()
        {
            // Efectos visuales en la posición del jugador
            Player? owner = Main.LocalPlayer;
            if (owner != null)
            {
                Main.NewText($"✦ {Item.Name} alcanzó el nivel {Level}!",
                    new Microsoft.Xna.Framework.Color(245, 196, 81));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                for (int i = 0; i < 40; i++)
                    Dust.NewDustPerfect(owner.Center, Terraria.ID.DustID.GoldFlame,
                        new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                        100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
            }

            // === EVENTO GLOBAL: primera subida de nivel (solo una vez por item) ===
            if (!FirstLevelUpTriggered && Main.myPlayer == owner?.whoAmI)
            {
                FirstLevelUpTriggered = true;
                LevelUpEventSystem.Trigger();
            }

            // Hito especial cada 50 niveles (infinito)
            if (Level % 50 == 0)
            {
                Main.NewText($"✦✦ Hito nivel {Level}! {Item.Name} resuena con poder. ✦✦",
                    new Microsoft.Xna.Framework.Color(245, 196, 81));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
            }
        }

        // === Persistencia: el nivel/XP se guarda en el TagCompound del item ===

        public override void SaveData(Item item, TagCompound tag)
        {
            if (Level > 1 || XP > 0 || FirstLevelUpTriggered)
            {
                tag["aethonLevel"] = Level;
                tag["aethonXP"] = XP;
                tag["aethonFirstLU"] = FirstLevelUpTriggered;
            }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            // Defensivo: si el item no tiene datos guardados, queda en nivel 1.
            try
            {
                Level = tag.GetInt("aethonLevel");
                if (Level < 1) Level = 1;
                XP = tag.GetInt("aethonXP");
                FirstLevelUpTriggered = tag.GetBool("aethonFirstLU");
            }
            catch
            {
                Level = 1;
                XP = 0;
                FirstLevelUpTriggered = false;
            }
        }

        /// <summary>
        /// Los items Aethon pueden stackear? No, porque cada uno tiene su nivel.
        /// Esto previene que Terraria combine dos SolbrandEdge con niveles distintos.
        /// </summary>
        public override bool CanStack(Item item1, Item item2)
        {
            // Nunca stackear armas Aethon (cada una tiene su propio nivel)
            return false;
        }
    }
}
