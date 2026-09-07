using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Bolsa de Invocadores (TEMPORAL) — item de testing.
    /// Al usarse, da 999 invocadores de cada jefe vanilla invocable por item.
    /// Útil para probar drops de jefes y progresión rápidamente.
    /// </summary>
    public class BossSummonBag : ModItem
    {
        // Lista de invocadores de jefes vanilla (items que invocan un jefe al usarse).
        // Todos los IDs verificados en Terraria 1.4.4 / tModLoader 1.4.4.
        private static readonly int[] BossSummoners = new int[]
        {
            ItemID.SlimeCrown,            // King Slime
            ItemID.SuspiciousLookingEye,  // Eye of Cthulhu
            ItemID.WormFood,              // Eater of Worlds
            ItemID.BloodySpine,           // Brain of Cthulhu
            ItemID.Abeemination,          // Queen Bee
            ItemID.ClothierVoodooDoll,    // Skeletron (vía Sastre)
            ItemID.GuideVoodooDoll,       // Wall of Flesh
            ItemID.DeerThing,             // Deerclops
            ItemID.QueenSlimeCrystal,     // Queen Slime
            ItemID.MechanicalEye,         // The Twins
            ItemID.MechanicalWorm,        // The Destroyer
            ItemID.MechanicalSkull,       // Skeletron Prime
            ItemID.TruffleWorm,            // Duke Fishron
            ItemID.LihzahrdPowerCell,     // Golem
            ItemID.EmpressButterfly,      // Empress of Light
            ItemID.CelestialSigil,        // Moon Lord
        };

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

            int given = 0;
            foreach (int summonerId in BossSummoners)
            {
                if (summonerId <= 0) continue;
                int idx = Item.NewItem(
                    player.GetSource_GiftOrReward(),
                    player.Center,
                    summonerId, 999);
                if (idx >= 0 && idx < Main.item.Length)
                {
                    Main.item[idx].noGrabDelay = 0;
                    given++;
                }
            }

            Main.NewText($"Bolsa abierta: {given} tipos de invocadores recibidos (999 c/u).",
                new Microsoft.Xna.Framework.Color(245, 196, 81));
            return true;
        }
    }
}
