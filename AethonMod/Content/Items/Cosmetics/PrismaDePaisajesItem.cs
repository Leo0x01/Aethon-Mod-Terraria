using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// PrismaDePaisajesItem — v6.43 — EL PRISMA DE PAISAJES.
    ///
    /// EL TESTER de CieloLib, la librería del fondo (paisajes, capas,
    /// parallax): cada uso despliega una ESCENA COMPLETA sobre el fondo
    /// del mundo — capas con parallax propio, deriva y profundidad, el
    /// cielo teñido, la luz de fondo matizada y el brillo respirando.
    ///
    /// EL CICLO (v6.44 — cuatro escenas): cielo limpio → Eclipse Umbral →
    /// Lluvia Estelar → Amanecer Primordial → Sagrario Violeta → cielo
    /// limpio. Cada cambio entra con su fundido cruzado (la escena vieja
    /// se desvanece mientras la nueva sube). El Sagrario Violeta (v6.44)
    /// despliega EL PAISAJE DEL LUGAR — las mismas capas del fondo del
    /// bioma — donde estés: el lugar hecho accesible con un clic.
    ///
    /// Arma de PRUEBAS de la casa: sin maná, sin daño — solo paisaje.
    /// </summary>
    public class PrismaDePaisajesItem : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.mana = 0;             // LA REGLA DE LA CASA: cero maná
        }

        public override bool? UseItem(Player player)
        {
            // El paisaje es un estado VISUAL de la máquina del jugador local:
            // en los demás clientes (y el servidor) el prisma solo "se usa".
            if (Main.myPlayer != player.whoAmI) return null;

            // === EL CICLO DEL PRISMA (v6.44: cuatro escenas — el
            // Sagrario Violeta cierra el ciclo con EL LUGAR) ===
            string actual = CieloLib.NombreEscenaActiva;
            string siguiente;
            if (actual == null)                        siguiente = CieloEscenas.NombreEclipse;
            else if (actual == CieloEscenas.NombreEclipse)    siguiente = CieloEscenas.NombreEstelar;
            else if (actual == CieloEscenas.NombreEstelar)    siguiente = CieloEscenas.NombreAlba;
            else if (actual == CieloEscenas.NombreAlba)       siguiente = CieloEscenas.NombreSagrario;
            else if (actual == CieloEscenas.NombreSagrario)   siguiente = null;
            else                                        siguiente = CieloEscenas.NombreEclipse;

            if (siguiente == null)
            {
                CieloLib.DesactivarTodas();
                Main.NewText("El prisma se apaga: el cielo vuelve a su dueño.",
                    new Color(150, 150, 170));
            }
            else
            {
                EscenaDeCielo escena = siguiente == CieloEscenas.NombreEclipse ? CieloEscenas.EclipseUmbral
                    : siguiente == CieloEscenas.NombreEstelar ? CieloEscenas.LluviaEstelar
                    : siguiente == CieloEscenas.NombreSagrario ? CieloEscenas.SagrarioVioleta
                    : CieloEscenas.AmanecerPrimordial;
                CieloLib.Activar(escena);
                Main.NewText($"El prisma despliega: {siguiente}.",
                    new Color(210, 160, 255));
            }

            // LA FIESTA DEL PRISMA: chispas del color de la escena entrante.
            if (Main.netMode != NetmodeID.Server)
            {
                Color color = siguiente switch
                {
                    CieloEscenas.NombreEclipse => new Color(200, 60, 160),
                    CieloEscenas.NombreEstelar => new Color(110, 180, 255),
                    CieloEscenas.NombreAlba => new Color(255, 190, 90),
                    CieloEscenas.NombreSagrario => new Color(200, 130, 255),
                    _ => new Color(170, 170, 190),
                };
                for (int i = 0; i < 14; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(1.5f, 3.5f),
                        150, color, 0.9f);
                    d.noGravity = true;
                }
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "C",
                "[c/BE8CFF:═══ EL PRISMA DE PAISAJES ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/78788C:El probador de la librería del cielo: despliega paisajes enteros\nsobre el fondo del mundo — capas con parallax, deriva y tintes\n(v6.44: incluye EL SAGRARIO VIOLETA, el lugar hecho escena)]"));
        }
    }
}
