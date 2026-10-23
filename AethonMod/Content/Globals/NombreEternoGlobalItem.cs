using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.51.4 — EL NOMBRE DEL GRIMORIO DEL ETERNO ARDE… v6.51.9 — SOLO
    //  CUANDO ES LEGENDARIO SUPREMO (la letra: «y ahora es cuando el
    //  nombre consigue el brillo caracteristico de Legendario supremo,
    //  entiendes ademas los colores deberian ser morado y negro con
    //  bordes dorados, negros y morados, mas los brillos caracteristicos
    //  de Legendario supremo»): el nombre gana SU TRAJE SUPREMO recién
    //  en la etiqueta EXCLUSIVA del mod (nivel 95 del libro — 50 niveles
    //  después de Legendario). Antes de eso el nombre viste SOLO el
    //  COLOR de su etiqueta de escalera (Común → Raro → Épico → Mítico
    //  → Legendario — GrimoireEternal.ColorEtiqueta), sin brillos: la
    //  escalera se GANA el resplandor.
    //
    //  EL TRAJE SUPREMO (PreDrawTooltipLine — reemplaza el dibujo
    //  vanilla de la línea del nombre):
    //   · EL RELLENO morado y negro que RESPIRA (el corazón violeta
    //     late y se apaga hacia la negrura, y vuelve).
    //   · LOS BORDES — dorados, negros y morados: el aura MORADA que
    //     emana por fuera, el borde DORADO legendario y el filete NEGRO
    //     de tinta, tres pasadas concéntricas que abrazan cada letra.
    //   · LOS DESTELLOS DE 4 PUNTAS de los soles rúnicos de 3 anillos+
    //     (la firma de siempre — dos cápsulas cruzadas de blanco
    //     incandescente + el corazón dorado), MÁS BRILLANTES y con
    //     PARTÍCULAS MÁS PEQUEÑAS que las del rótulo del bestiario
    //     (los destellos viven en PostDrawTooltipLine — se apilan
    //     ENCIMA del traje).
    // ======================================================================
    public class NombreEternoGlobalItem : GlobalItem
    {
        public override GlobalItem Clone(Item from, Item to) => this;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            // SOLO el Grimorio del Eterno (el arma del título).
            return entity.type == ModContent.ItemType<
                global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        /// <summary>¿ESTE libro ya es LEGENDARIO SUPREMO? (la etiqueta
        /// exclusiva del mod — v6.51.10 LA REFORJA VIVA: el último
        /// peldaño de la escalera de modificadores, nivel 446+ = 50
        /// niveles tras el Legendario 396: SOLO ahí vive el brillo del
        /// nombre; la escalera anterior viste solo su color).</summary>
        private static bool EsSupremo(Item item)
        {
            try
            {
                if (item == null || item.IsAir) return false;
                var sl = item.GetGlobalItem<ShardLevelItem>();
                return global::AethonMod.Content.Weapons.GrimoireEternal
                    .IndiceEtiqueta(sl?.Level ?? 1) >=
                    global::AethonMod.Content.Weapons.GrimoireEternal.LaReforja.Length - 1;
            }
            catch { return false; }
        }

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName") return true;
            if (Main.gameMenu) return true;
            // v6.51.9 — EL TRAJE ES SOLO DEL SUPREMO (antes el nombre ardía
            // SIEMPRE — v6.51.4; la letra nueva guarda el brillo para la
            // etiqueta exclusiva del mod).
            if (!EsSupremo(item)) return true;

            try
            {
                var fuente = line.Font;
                if (fuente == null || string.IsNullOrEmpty(line.Text)) return true;

                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = new Vector2(line.X, line.Y);
                string texto = line.Text;

                // === EL RELLENO: morado y negro que RESPIRA (el corazón
                //     violeta late hacia la negrura y vuelve — «los
                //     colores deberian ser morado y negro»). ===
                float latido = 0.5f + 0.5f * MathF.Sin(t * 1.35f);
                Color morado = new Color(122, 44, 198);
                Color negrura = new Color(16, 6, 34);
                Color relleno = Color.Lerp(negrura, morado, 0.35f + 0.65f * latido);

                // === LOS BORDES — «dorados, negros y morados» (de fuera
                //     hacia dentro): el aura MORADA que emana → el borde
                //     DORADO legendario → el filete NEGRO de tinta. ===
                Color bordeMorado = new Color(168, 96, 236) * 0.85f;
                Color bordeDorado = new Color(255, 202, 96) * 0.95f;
                Color bordeNegro = new Color(10, 4, 20);

                // PASADA 1 — EL AURA MORADA (la emanación: ±2,4 px, 8 rumbos).
                for (int k = 0; k < 8; k++)
                {
                    Vector2 off = new Vector2(
                        MathF.Cos(k * MathHelper.PiOver4),
                        MathF.Sin(k * MathHelper.PiOver4)) * 2.4f;
                    Main.spriteBatch.DrawString(fuente, texto, pos + off,
                        bordeMorado, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                // PASADA 2 — EL BORDE DORADO (±1,5 px, 8 rumbos girados).
                for (int k = 0; k < 8; k++)
                {
                    Vector2 off = new Vector2(
                        MathF.Cos(k * MathHelper.PiOver4 + 0.2f),
                        MathF.Sin(k * MathHelper.PiOver4 + 0.2f)) * 1.5f;
                    Main.spriteBatch.DrawString(fuente, texto, pos + off,
                        bordeDorado, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                // PASADA 3 — EL FILETE NEGRO DE TINTA (±0,8 px, 4 rumbos).
                for (int k = 0; k < 4; k++)
                {
                    Vector2 off = new Vector2(
                        MathF.Cos(k * MathHelper.PiOver2),
                        MathF.Sin(k * MathHelper.PiOver2)) * 0.8f;
                    Main.spriteBatch.DrawString(fuente, texto, pos + off,
                        bordeNegro, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                // PASADA 4 — EL RELLENO morado-negro vivo.
                Main.spriteBatch.DrawString(fuente, texto, pos,
                    relleno, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                return false;   // el nombre supremo YA está dibujado (nosotros)
            }
            catch { return true; }
        }

        public override void PostDrawTooltipLine(Item item, DrawableTooltipLine line)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName") return;
            if (Main.gameMenu) return;
            // v6.51.9 — LOS DESTELLOS SON DEL SUPREMO (antes ardían SIEMPRE
            // desde la .51.4; la letra nueva los guarda para la etiqueta
            // exclusiva del mod — «mas los brillos caracteristicos de
            // Legendario supremo»).
            if (!EsSupremo(item)) return;
            try
            {
                var fuente = line.Font;
                if (fuente == null || string.IsNullOrEmpty(line.Text)) return;

                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow == null) return;

                // EL RECT DEL NOMBRE: la posición exacta donde se dibujó
                // el texto (línea por línea — el nombre es la primera).
                Vector2 posTexto = new Vector2(line.X, line.Y);
                Vector2 medida = fuente.MeasureString(line.Text);
                Vector2 centro = posTexto + medida * 0.5f;

                float t = Main.GlobalTimeWrappedHourly;

                // === LOS DESTELLOS DE 4 PUNTAS — MÁS BRILLANTES y MÁS
                //     CHICOS que los del rótulo del bestiario: 9 partículas
                //     de 4-7 px ardiendo a 0.95 de alfa. ===
                const int destellos = 9;
                for (int i = 0; i < destellos; i++)
                {
                    float vida = (t * 0.55f + i * 0.331f) % 1f;     // su reloj propio
                    float arde = MathF.Sin(vida * MathHelper.Pi);   // nace → arde → muere
                    if (arde < 0.10f) continue;

                    // Su sitio: repartido por el largo del nombre (con su
                    // deriva fija — determinista, sin RNG).
                    float f = (i + 0.5f) / destellos;
                    float cabeceo = 0.5f * MathF.Sin(t * 0.8f + i * 1.7f);
                    Vector2 pos = centro + new Vector2(
                        (f - 0.5f) * (medida.X + 14f),
                        (cabeceo - 0.30f) * medida.Y +
                        MathF.Sin(t * 1.0f + i * 2.3f) * 2.5f);

                    float size = (3.4f + 2.0f * (i % 3)) * arde;    // 3.4-5.4 px: CHICOS
                    Color blanco = new Color(255, 250, 232) * (0.95f * arde);  // BRILLANTES
                    // LAS DOS CÁPSULAS CRUZADAS (el trazo del destello solar).
                    Main.spriteBatch.Draw(glow, pos, null, blanco, 0f,
                        glow.Size() * 0.5f,
                        new Vector2(size * 2.4f, 1.7f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glow, pos, null, blanco, 0f,
                        glow.Size() * 0.5f,
                        new Vector2(1.7f, size * 2.4f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    // EL CORAZÓN DORADO (el núcleo del brillo).
                    Main.spriteBatch.Draw(glow, pos, null,
                        new Color(255, 216, 122) * (0.85f * arde), 0f,
                        glow.Size() * 0.5f,
                        new Vector2(size * 1.05f, size * 1.05f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
            }
            catch { }
        }
    }
}
