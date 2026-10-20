using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    /// <summary>
    /// LA BRUMA DEL CRÁTER (v6.50.130) — LA LETRA DEL USUARIO: «en el
    /// crater debe estar un item invisible que es la bruma en si de esa
    /// forma los jugadores pueden tomarla y colocarla donde quieran,
    /// este item de bruma sera usado como altar principal y cualquier
    /// cosa que se lance se convierte en Esencia Corrupta, pero las
    /// cosas aptas para corromper como las armas de cualquier tipo
    /// obtendran la etiqueta corrupto y ya sabes no se podran
    /// modificar, ademas que no sea necesario lanzar nada, que desde
    /// el inventario funcione en el apartado creacion, que se permita
    /// poder corromper las armas y convertir todo en Esencia Corrupta».
    ///
    /// EL ITEM INVISIBLE QUE ES LA BRUMA: en el suelo NO se ve como un
    /// ítem — se ve como BRUMA (PreDrawInWorld dibuja la nube violeta y
    /// calla al sprite: la bruma EN PERSONA). Los jugadores la TOMAN
    /// (caminar encima la recoge, como cualquier drop) y la COLOCAN
    /// donde quieran: al colocarla nace LA TILE DE BRUMA — EL ALTAR
    /// PRINCIPAL (BrumaAltarTile):
    ///  · CUALQUIER COSA QUE SE LANCE a la bruma colocada se convierte
    ///    en ESENCIA CORRUPTA (y las ARMAS ganan la etiqueta CORRUPTO —
    ///    el camino de siempre del AltarDevoradorSistema: la bruma
    ///    colocada registra SU zona al ponerse).
    ///  · EL APARTADO CREACIÓN: la tile de bruma es una ESTACIÓN DE
    ///    FABRICACIÓN (RecetasCorrupcionSistema): al lado de la bruma,
    ///    el menú de creación ofrece CORROMPER LAS ARMAS (arma → arma
    ///    con la etiqueta Corrupto — sin lanzar nada) y CONVERTIR TODO
    ///    LO DEMÁS EN ESENCIA CORRUPTA (item → esencias).
    ///  · Y SI LA RECOGEN DE NUEVO (pico sobre la bruma): el item
    ///    vuelve al inventario — el altar viaja contigo.
    /// </summary>
    public class BrumaDelCrater : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Altar/BrumaDelCrater";

        public override void SetStaticDefaults()
        {
            // El DisplayName y el Tooltip viven en la localización
            // (Items.BrumaDelCrater) — la regla de la casa.
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = 0;                     // no vale nada: vale el altar que crea

            // LA COLOCACIÓN: el item ES la bruma — al usarlo nace LA TILE
            // DE BRUMA (el altar principal: estación de fabricación y
            // zona de corrupción).
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.consumable = true;
            Item.autoReuse = false;
            // v6.51.0 — LA COLOCACIÓN MURIÓ: la bruma es SOLO ESTÉTICA en
            // LA LLEGADA (la letra) — este item es un VESTIGIO de la
            // vieja caída (ya no nace de cráter alguno).
        }

        /// <summary>LA COLOCACIÓN (v6.50.130): ESTE tML 2026.08 no trae
        ///         hook de colocación en ModItem — la AUTORIDAD descubre
        ///         la bruma colocada SOLA (LaCaidaSistema.
        ///         DescubrirBrumasColocadas: un escaneo periódico alrededor
        ///         de cada jugador registra la tile nueva y difunde su
        ///         zona — la colocación SIEMPRE pasa cerca de un jugador,
        ///         en SP y en MP por igual, sin paquetes).</summary>

        /// <summary>EL ITEM INVISIBLE QUE ES LA BRUMA: en el mundo NO hay
        /// sprite — hay BRUMA (tres mechones de niebla violeta que
        /// respiran y se mecen, el polvo que gotea de ellos y un brillo
        /// suave para hallarla de noche). La bruma del cráter que se ve…
        /// ES este ítem.</summary>
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return true;
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow != null)
                {
                    float t = Main.GlobalTimeWrappedHourly;
                    Vector2 pos = Item.Center - Main.screenPosition;

                    // EL ALIENTO VIOLETA (la lectura de noche).
                    float pulso = 0.22f + 0.10f * MathF.Sin(t * 1.9f);
                    spriteBatch.Draw(glow, pos, null, new Color(110, 50, 180) * pulso, 0f,
                        glow.Size() * 0.5f, new Vector2(52f, 52f) / glow.Size(),
                        SpriteEffects.None, 0f);

                    // LOS TRES MECHONES (la nube que respira: cada uno a su
                    // compás, meciéndose como hierba en el viento).
                    for (int m = 0; m < 3; m++)
                    {
                        float fase = t * (1.1f + m * 0.27f) + m * 2.2f;
                        float ancho = 30f + 8f * MathF.Sin(fase * 0.7f + m);
                        Vector2 mechón = pos + new Vector2(
                            (m - 1) * 12f + MathF.Sin(fase) * 4f,
                            MathF.Sin(fase * 1.3f) * 5f - 6f);
                        Color tono = m == 1
                            ? new Color(72, 44, 108) * 0.55f
                            : new Color(96, 62, 140) * 0.45f;
                        spriteBatch.Draw(glow, mechón, null, tono, 0f,
                            glow.Size() * 0.5f, new Vector2(ancho, ancho * 0.8f) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }

                    // EL CORAZÓN (un fulgor claro: la bruma ALBERGA algo).
                    spriteBatch.Draw(glow, pos + new Vector2(0f, -4f), null,
                        new Color(196, 150, 246) * (0.30f + 0.12f * MathF.Sin(t * 2.6f)), 0f,
                        glow.Size() * 0.5f, new Vector2(16f, 16f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // El goteo de la bruma (motas violetas que se apagan).
                if (Main.rand.NextBool(22))
                {
                    Dust d = Dust.NewDustPerfect(
                        Item.Center + new Vector2(Main.rand.NextFloat(-10f, 10f),
                            Main.rand.NextFloat(-12f, 2f)),
                        DustID.Shadowflame, new Vector2(0f, -0.5f), 140,
                        new Color(120, 50, 180), 0.5f);
                    d.noGravity = true;
                }
            }
            catch { }
            return false;   // SIN sprite: la bruma ES el ítem
        }
    }
}
