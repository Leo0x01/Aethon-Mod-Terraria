using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.51.5 — LA LISTA DEL DESPERTAR TE DICE CUÁNDO YA PUEDES (la
    //  letra: «los requisitos que aparecen en el despertar para subir
    //  su nivel, deberia mostrar cuando cumples uno de ellos, ademas
    //  deberia mostrar el sprite del arma muy pequeño para saber que
    //  arma se necesita»).
    //
    //  LA TÉCNICA (la del nombre que arde de la .51.4 —
    //  NombreEternoGlobalItem, el patrón PROBADO): el hook
    //  PostDrawTooltipLine corre DESPUÉS de que tML dibuja cada línea
    //  (con su X/Y exactos) — ahí viven:
    //
    //    · EL ✓ DORADO LATIENDO: una estrella de 4 puntas al INICIO de
    //      cada requisito cumplido (verde) — el sello del logro,
    //      pulsando a su compás (dibujo directo, sin tocar el lote).
    //    · LA FILA DE LOS SPRITES MINI: en la línea «DespertarArmas»
    //      (una línea de espacios que RESERVA el ancho) se pintan los
    //      sprites de TODAS las alternativas del peldaño (16 px — «el
    //      sprite del arma muy pequeño para saber que arma se
    //      necesita»)… y las que ya llevas CORRUPTAS ARDEN: halo dorado
    //      latiendo detrás + su broche brillante (la que cumple).
    //
    //  El TEXTO vivo (el conteo 3/5 y el verde) lo pone
    //  GrimoireEternal.ModifyTooltips — este global pone el JUICE.
    // ======================================================================
    public class DespertarRequisitosGlobalItem : GlobalItem
    {
        public override GlobalItem Clone(Item from, Item to) => this;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            // SOLO el Grimorio del Eterno (la ventana de info del despertar).
            return entity.type == ModContent.ItemType<
                global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        public override void PostDrawTooltipLine(Item item, DrawableTooltipLine line)
        {
            if (Main.gameMenu) return;
            try
            {
                Player portador = Main.LocalPlayer;
                if (portador == null) return;
                // v6.51.6 — la escalera es DEL ITEM señalizado (cada libro
                // tiene SU siguiente peldaño).
                int siguiente = Systems.DespertarGrimorio.NivelDespertarSiguiente(item);
                if (siguiente < 1 || siguiente > Systems.DespertarGrimorio.NivelMax) return;

                float t = Main.GlobalTimeWrappedHourly;
                var fuente = line.Font;

                // ============================================================
                //  EL ✓ DORADO LATIENDO — al inicio de cada requisito
                //  cumplido (el sello del logro: dos cápsulas cruzadas
                //  de blanco incandescente + el corazón dorado — la firma
                //  de los soles de la casa, la técnica del nombre .51.4).
                // ============================================================
                bool? cumplido = line.Name switch
                {
                    "DespertarReq2" => Systems.DespertarGrimorio.EsenciasEnManos(portador)
                        >= Systems.DespertarGrimorio.EsenciasPara(siguiente),
                    "DespertarReq3" => Systems.DespertarGrimorioSistema.ContarMonedas(portador)
                        >= Systems.DespertarGrimorio.MonedasPara(siguiente),
                    // v6.51.8 — EL REQUISITO 3 ES DUAL: los peldaños de
                    //     ARMAS cuentan las corruptas del peldaño… y los
                    //     peldaños MINERALES, los lingotes del inventario
                    //     (la ofrenda mineral — sin corrupción).
                    "DespertarReq4" => RequisitoDeLaOfrenda(portador, siguiente),
                    _ => null,
                };
                if (cumplido == true)
                {
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    if (glow != null && fuente != null)
                    {
                        float late = 0.70f + 0.30f * MathF.Sin(t * 3.2f);
                        Vector2 centro = new Vector2(line.X - 7f, line.Y + fuente.MeasureString("A").Y * 0.5f);
                        Color blanco = new Color(255, 250, 232) * (0.85f * late);
                        Main.spriteBatch.Draw(glow, centro, null, blanco, 0f,
                            glow.Size() * 0.5f,
                            new Vector2(7.2f, 2.0f) / glow.Size(),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(glow, centro, null, blanco, 0f,
                            glow.Size() * 0.5f,
                            new Vector2(2.0f, 7.2f) / glow.Size(),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(glow, centro, null,
                            new Color(255, 216, 122) * (0.80f * late), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(3.4f, 3.4f) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }
                }

                // ============================================================
                //  v6.51.6 — LA FILA DE SPRITES POST-DRAW MURIÓ: los
                //  sprites del arma ahora viven INLINE en la propia línea
                //  («DespertarReq4») con el tag nativo [i:id] de tML —
                //  nombre y sprite JUNTOS, renderizados por el parser de
                //  vanilla (a prueba de builds locales). Queda SOLO el
                //  ✓ dorado latiendo al inicio de cada requisito cumplido
                //  (el jugo de siempre — decoración pura).
                // ============================================================
            }
            catch { }
        }

        /// <summary>¿Lleva alguna de las alternativas del peldaño
        /// CORRUPTA? (el estado del requisito del arma).</summary>
        private static bool LlevaAlgunaCorrupta(Player p, int nivel)
        {
            if (nivel < 1 || nivel > Systems.DespertarGrimorio.NivelMax) return false;
            int[] peldano = Systems.DespertarGrimorio.ArmasDelNivel[nivel];
            for (int k = 0; k < peldano.Length; k++)
                if (Systems.DespertarGrimorio.LlevaCorrupta(p, peldano[k]))
                    return true;
            return false;
        }

        /// <summary>v6.51.8 — ¿EL REQUISITO DE LA OFRENDA ya está? (dual:
        /// peldaño de ARMAS → alguna corrupta del peldaño · peldaño
        /// MINERAL → los lingotes completos en el inventario).</summary>
        private static bool RequisitoDeLaOfrenda(Player p, int nivel)
        {
            if (nivel < 1 || nivel > Systems.DespertarGrimorio.NivelMax) return false;
            if (Systems.DespertarGrimorio.EsNivelMineral(nivel))
                return Systems.DespertarGrimorio.LlevaMineralCompleto(p, nivel);
            return LlevaAlgunaCorrupta(p, nivel);
        }
    }
}
