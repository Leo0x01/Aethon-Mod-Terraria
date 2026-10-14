using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.50.119 — LA BOLSA DE LA CRÍA (la letra del usuario: «crea una
    //  bolsa donde esten las mascotas, las 2»).
    //
    //  v6.50.122 — LAS DOS MASCOTAS DE VERDAD (la letra que desenredó los
    //  conceptos): la PAREJA DEL RECUERDO — la cría de LUZ
    //  (AethonMenorPet) y su gemela de OSCURIDAD (AethonMenorOscuroPet,
    //  la engendra el buff mientras la de luz viva: la miniatura del
    //  JEFE, un solo ítem las despierta a las dos) — y EL GRIMORIO
    //  MENOR (GrimorioMenorItem: la miniatura del LIBRO, con sus mini
    //  minions y sus travesuras). Dos ítems, dos mascotas, una bolsa.
    //
    //  LA VÍA (la canónica de la .118, ahora con su envoltura): el
    //  RECUERDO COMPLETO la suelta SIEMPRE al morir — y el jefe de luz
    //  un 20% de las veces (su recuerdo vivo). Y la vía de pruebas de
    //  la casa: garantizada en cada ENTRADA AL MUNDO (TestingPlayer,
    //  como las veintiuna bolsas del kit). Se abre con CLIC DERECHO
    //  (el patrón de la casa: la apertura corre SOLO en el cliente que
    //  clica — el inventario es client-authoritative y se sincroniza
    //  solo, la lección v6.50.2 de las 16 bolsas).
    // ======================================================================
    public class BolsaDeLaCria : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;                   // la convención de las bolsas (30×30)
            Item.height = 30;
            Item.maxStack = 30;                // se pueden apilar varias
            Item.consumable = true;            // se abre (clic derecho)
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;    // la familia de la cría
            Item.value = Item.buyPrice(gold: 5);
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            // v6.50.122 — LAS DOS MASCOTAS DESPIERTAN (la letra que
            // desenredó los conceptos): el AETHON MENOR (la miniatura del
            // JEFE de luz — alumbra, estalla y se fusiona con su gemela
            // oscura: las dos nacen juntas) y EL GRIMORIO MENOR (la
            // miniatura del LIBRO — sus mini minions, su sombrilla, sus
            // travesuras contra los vecinos). El patrón de Dar de la
            // casa: primera ranura libre; si el inventario no tiene
            // hueco, cae a sus pies — la bolsa nunca pierde a una cría.
            Dar(player, ModContent.ItemType<AethonMenorItem>());
            Dar(player, ModContent.ItemType<GrimorioMenorItem>());

            // LA APERTURA DE LA CASA (el momento): la MITAD de las
            // chispas DORADAS (la cría de luz) y la MITAD MORADAS (la
            // gemela oscura y el mini grimorio) — las mascotas
            // despertando alrededor tuyo.
            if (!Main.dedServ)
            {
                for (int i = 0; i < 30; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    bool luz = (i & 1) == 0;
                    Dust d = Dust.NewDustPerfect(player.Center,
                        luz ? DustID.GoldFlame : DustID.PurpleTorch,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(1.6f, 4.2f),
                        180, luz ? new Color(255, 222, 130) : new Color(150, 70, 220), 0.9f);
                    d.noGravity = true;
                }
                Lighting.AddLight(player.Center, 0.75f, 0.62f, 0.95f);
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item4.WithPitchOffset(-0.25f), player.Center);

            if (player.whoAmI == Main.myPlayer)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.BolsaDeLaCria.Abierta"),
                    new Color(230, 196, 255));
        }

        /// <summary>El patrón de Dar de la casa: primera ranura libre; si
        /// el inventario no tiene hueco, cae a sus pies — la bolsa nunca
        /// pierde a una de sus crías.</summary>
        private static void Dar(Player player, int tipo)
        {
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] == null || player.inventory[i].type == ItemID.None)
                {
                    player.inventory[i].SetDefaults(tipo);
                    player.inventory[i].stack = 1;
                    return;
                }
            }
            int drop = Item.NewItem(player.GetSource_GiftOrReward(),
                player.Center, tipo, 1);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
