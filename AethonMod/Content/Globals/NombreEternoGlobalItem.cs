using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.51.4 — EL NOMBRE DEL GRIMORIO DEL ETERNO ARDE (la letra:
    //  «ahora el mismo brillo que tiene legendeario supremo es el que
    //  debe tener el nombre del grimorio del eterno en su nombre pero
    //  con mas brillo y sus particulas mas pequeñas»).
    //
    //  EL MISMO FIRMA VISUAL del rótulo ✦ LEGENDARIO SUPREMO ✦ de la
    //  ficha del bestiario (los DESTELLOS DE 4 PUNTAS de los soles
    //  rúnicos de 3 anillos+ — dos cápsulas cruzadas de blanco
    //  incandescente + el corazón dorado)… pero EN EL NOMBRE del arma,
    //  MÁS BRILLANTE (alfa 0.95) y con PARTÍCULAS MÁS PEQUEÑAS (4-7 px
    //  — el rótulo usaba 7-11): la primera línea de la ventana de info
    //  del Grimorio del Eterno VIVE.
    //
    //  LA TÉCNICA (sin tocar vanilla): el hook PostDrawTooltipLine del
    //  GlobalItem corre DESPUÉS de que tML dibuja cada línea — la línea
    //  del nombre (Mod "Terraria", Name "ItemName") llega con su X/Y
    //  EXACTOS de dibujo: ahí nacen y mueren los destellos, a lo largo
    //  del texto, cada uno a su compás (deterministas — sin RNG).
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

        public override void PostDrawTooltipLine(Item item, DrawableTooltipLine line)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName") return;
            if (Main.gameMenu) return;
            try
            {
                var fuente = line.Font;
                if (fuente == null || string.IsNullOrEmpty(line.Text)) return;

                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow == null) return;

                // EL RECT DEL NOMBRE: la posición exacta donde tML dibujó
                // el texto (línea por línea — el nombre es la primera).
                Vector2 posTexto = new Vector2(line.X, line.Y);
                Vector2 medida = fuente.MeasureString(line.Text);
                Vector2 centro = posTexto + medida * 0.5f;

                float t = Main.GlobalTimeWrappedHourly;

                // === LOS DESTELLOS DE 4 PUNTAS — MÁS BRILLANTES y MÁS
                //     CHICOS que los del rótulo del bestiario (la letra):
                //     9 partículas de 4-7 px ardiendo a 0.95 de alfa. ===
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
