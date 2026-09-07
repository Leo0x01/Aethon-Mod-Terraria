using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Verdugo de Niveles (TEMPORAL) — item de testing.
    /// Da +10 niveles al primer Grimorio del Eterno encontrado en el inventario.
    /// No requiere sostenerlo: busca en todos los slots.
    /// </summary>
    public class LevelUpTester : ModItem
    {
        public const int LevelsPerUse = 10;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return null;

            // Buscar el primer Grimorio del Eterno en todo el inventario.
            for (int i = 0; i < 58; i++)
            {
                Item inv = player.inventory[i];
                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>()) continue;

                var sl = inv.GetGlobalItem<ShardLevelItem>();
                if (sl == null) continue;

                sl.Level += LevelsPerUse;
                Main.NewText($"✦ {inv.Name} subió +{LevelsPerUse} niveles (ahora nivel {sl.Level}).",
                    new Microsoft.Xna.Framework.Color(245, 196, 81));

                // Efectos visuales de subida de nivel.
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                for (int d = 0; d < 30; d++)
                {
                    Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                        new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                        100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
                }
                return true;
            }

            Main.NewText("No se encontró un Grimorio del Eterno en el inventario.",
                new Microsoft.Xna.Framework.Color(255, 120, 120));
            return false;
        }
    }
}
