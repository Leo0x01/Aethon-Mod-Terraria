using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Escriba;

namespace AethonMod.Content.Items.Bolsas
{
    /// <summary>
    /// BOLSADELESCRIBA — v6.52.0 — LA BOLSA DEL ESCRIBA: la bolsa nueva
    /// que pidió el usuario para TODO lo creado en este ciclo de
    /// investigación («todo lo creado nuevo lo puedes meter en una nueva
    /// bolsa y dárselo al jugador"):
    ///
    ///   · EL PRIMER TRAZO — el arma del rayo masivo (la investigación
    ///     de los beams de 7 capas: Perlin, glow, motion-blur, luz).
    ///   · LA PLUMA CONSCIENTE — el arma viva con su árbol de
    ///     inscripciones (la investigación de Calamity Overhaul).
    ///   · LA PLUMA CONVOCADORA — el ítem del jefe demo EL ESCRIBA (la
    ///     investigación de coreografía del video de Lunar Veil).
    ///   · LA AURORA DE TINTA — v6.52.1 — LAS ALAS CREADAS POR CÓDIGO
    ///     (la investigación de las alas procedurales de Calamity
    ///     Overhaul: el abanico de plumas-quad, la onda de aleteo, la
    ///     dispersión de hue, el ruido que fluye y la física inyectada
    ///     sin sprite).
    ///
    /// Se abre con CLIC DERECHO (el patrón de las veintiún bolsas de la
    /// casa: solo entrega lo que falte — la bolsa nunca duplica).
    /// </summary>
    public class BolsaDelEscriba : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;                   // la convención de las bolsas
            Item.height = 30;
            Item.maxStack = 30;
            Item.consumable = true;            // se abre (clic derecho)
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;    // la familia de las bolsas
            Item.value = Item.buyPrice(gold: 5);
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            // LAS CUATRO PIEZAS DEL CICLO (el patrón de Dar de la casa:
            // primera ranura libre; si no hay hueco, cae a sus pies).
            Dar(player, ModContent.ItemType<PrimerTrazo>());
            Dar(player, ModContent.ItemType<PlumaConsciente>());
            Dar(player, ModContent.ItemType<Items.Escriba.PlumaConvocadora>());
            Dar(player, ModContent.ItemType<Items.Escriba.AuroraDeTinta>());

            // LA APERTURA: la tinta despierta alrededor tuyo (mitad
            // violeta, mitad blanca — la tinta y la página).
            if (!Main.dedServ)
            {
                for (int i = 0; i < 30; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    bool pagina = (i & 1) == 0;
                    Dust d = Dust.NewDustPerfect(player.Center,
                        pagina ? DustID.ShadowbeamStaff : DustID.PurpleTorch,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(1.6f, 4.2f),
                        180, pagina ? new Color(240, 236, 250) : new Color(150, 70, 220), 0.9f);
                    d.noGravity = true;
                }
                Lighting.AddLight(player.Center, 0.62f, 0.52f, 0.95f);
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item4.WithPitchOffset(-0.25f), player.Center);

            if (player.whoAmI == Main.myPlayer)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.BolsaDelEscriba.Abierta"),
                    new Color(212, 190, 255));
        }

        /// <summary>El patrón de Dar de la casa (BolsaDeLaCria): primera
        /// ranura libre; si el inventario no tiene hueco, cae a sus pies.</summary>
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
