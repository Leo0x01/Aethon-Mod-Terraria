using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// v6.50.86 — LA COPIA ESTELAR DEL GRIMORIO HAMBRIENTO. La letra del
    /// usuario: «en esta tercera copia quiero que animes las estrellas que
    /// tiene el grimorio en la tapa y animes las venas de luz morada que
    /// recorren el libro».
    ///
    /// LAS ESTRELLAS — las de verdad: las cruces DORADAS y los signos +
    /// MORADOS sobre la tapa negra (tools/gen_grimorio_v65086.py las separa
    /// de los remaches del anillo y de los brillos del marco dorado exigiendo
    /// fondo local OSCURO). Titilan en 3 GRUPOS desfasados 120°: nunca se
    /// apagan del todo (brillo base 14 %) y suben a 92 % con onda² — un cielo
    /// que respira, no un estrobo. Cada máscara lleva los PÍXELES DEL ARTE
    /// del usuario ×1,8 + halo: la fidelidad de la .85, ahora animada.
    ///
    /// LAS VENAS DE LUZ MORADA — la ola que RECORRE el libro: 4 bandas por
    /// distancia radial AL OJO (la fuente del poder), la banda k se enciende
    /// cuando la onda sin(2πt/P − k·π/2) pasa por ella — la luz NACE en el
    /// ojo y viaja hacia afuera, 3,4 s por ciclo. Los píxeles del arte ×1,6
    /// + halo de 3 px: a 36×49 la vena del arte es sub-píxel — el halo es el
    /// que hace LEER la ola.
    ///
    /// EL HAMBRE ACELERA EL CIELO: las estrellas pasan de 2,6 s a 1,3 s por
    /// ciclo y las venas de 3,4 s a 1,7 s — el libro entero se agita con el
    /// apetito. El ciclo del original queda INTACTO (mismo ojo, mismo
    /// parpadeo PURO de código v6.50.86, misma descarga, mismo reinicio) con
    /// SU PROPIO EstadoGrimorio: las tres copias pasan hambre por separado.
    /// </summary>
    public class GrimorioHambrientoEstelar : GrimorioHambriento
    {
        // los periodos (s) saciado → hambriento
        private const float PERIODO_ESTRELLAS_SACIADO = 2.6f;
        private const float PERIODO_ESTRELLAS_HAMBRIENTO = 1.3f;
        private const float PERIODO_VENAS_SACIADO = 3.4f;
        private const float PERIODO_VENAS_HAMBRIENTO = 1.7f;

        private const int N_GRUPOS_ESTRELLAS = 3;
        private const int N_BANDAS_VENAS = 4;

        private static readonly EstadoGrimorio _estadoEstelar = new EstadoGrimorio();
        protected override EstadoGrimorio Estado => _estadoEstelar;

        private static Color ConAlfa(float a) => new Color(255, 255, 255, (int)(255f * Math.Clamp(a, 0f, 1f)));

        private static Texture2D Estrellas(int g) =>
            ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambrientoEstelar_Estrellas" + (g + 1)).Value;

        private static Texture2D Venas(int k) =>
            ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambrientoEstelar_Venas" + (k + 1)).Value;

        /// <summary>EL ALFA DE CADA CAPA en el instante t (el corazón de la animación).</summary>
        private (float[] alfaEstrellas, float[] alfaVenas) Olas(float t)
        {
            float h = Estado.Hambre;
            var aE = new float[N_GRUPOS_ESTRELLAS];
            var aV = new float[N_BANDAS_VENAS];

            // ESTRELLAS: titilan en 3 grupos desfasados — nunca mueren del todo
            float pE = MathHelper.Lerp(PERIODO_ESTRELLAS_SACIADO, PERIODO_ESTRELLAS_HAMBRIENTO, h);
            for (int g = 0; g < N_GRUPOS_ESTRELLAS; g++)
            {
                float brillo = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * t / pE
                    - g * MathHelper.TwoPi / N_GRUPOS_ESTRELLAS);
                aE[g] = 0.14f + 0.78f * brillo * brillo;
            }

            // VENAS: la ola radial — la banda k se enciende cuando la onda la alcanza
            float pV = MathHelper.Lerp(PERIODO_VENAS_SACIADO, PERIODO_VENAS_HAMBRIENTO, h);
            for (int k = 0; k < N_BANDAS_VENAS; k++)
            {
                float onda = Math.Max(0f, (float)Math.Sin(MathHelper.TwoPi * t / pV
                    - k * MathHelper.PiOver2));
                aV[k] = 0.95f * (float)Math.Pow(onda, 1.6);
            }
            return (aE, aV);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // primero el ojo (la base), DESPUÉS el cielo: las máscaras son
            // frames completos — encima del párpado las estrellas siguen
            // vivas (el párpado es la MISMA corrida de arte: mismas estrellas).
            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);

            var (aE, aV) = Olas(Main.GameUpdateCount / 60f);
            for (int g = 0; g < N_GRUPOS_ESTRELLAS; g++)
                if (aE[g] > 0.02f)
                    spriteBatch.Draw(Estrellas(g), position, frame, ConAlfa(aE[g]), 0f,
                        origin, scale, SpriteEffects.None, 0f);
            for (int k = 0; k < N_BANDAS_VENAS; k++)
                if (aV[k] > 0.02f)
                    spriteBatch.Draw(Venas(k), position, frame, ConAlfa(aV[k]), 0f,
                        origin, scale, SpriteEffects.None, 0f);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);

            // las máscaras con la MISMA transform vanilla del libro (v6.50.84)
            Main.GetItemDrawFrame(Item.type, out var _, out var frame);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pivote = Item.Bottom - Main.screenPosition - new Vector2(0f, origen.Y);

            var (aE, aV) = Olas(Main.GameUpdateCount / 60f);
            for (int g = 0; g < N_GRUPOS_ESTRELLAS; g++)
                if (aE[g] > 0.02f)
                    spriteBatch.Draw(Estrellas(g), pivote, frame, ConAlfa(aE[g]), rotation,
                        origen, scale, SpriteEffects.None, 0f);
            for (int k = 0; k < N_BANDAS_VENAS; k++)
                if (aV[k] > 0.02f)
                    spriteBatch.Draw(Venas(k), pivote, frame, ConAlfa(aV[k]), rotation,
                        origen, scale, SpriteEffects.None, 0f);
        }

        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            // el libro + el ojo (la base agrega TODO)
            base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            // EL CIELO encima — MISMA transform del held item (v6.50.84): la
            // máscara es un frame completo, el espejo la voltea JUNTO al libro
            var (aE, aV) = Olas(Main.GameUpdateCount / 60f);
            for (int g = 0; g < N_GRUPOS_ESTRELLAS; g++)
                if (aE[g] > 0.02f)
                    drawInfo.DrawDataCache.Add(new DrawData(Estrellas(g), drawData.position,
                        drawData.sourceRect, ConAlfa(aE[g]), drawData.rotation, drawData.origin,
                        drawData.scale, drawData.effect));
            for (int k = 0; k < N_BANDAS_VENAS; k++)
                if (aV[k] > 0.02f)
                    drawInfo.DrawDataCache.Add(new DrawData(Venas(k), drawData.position,
                        drawData.sourceRect, ConAlfa(aV[k]), drawData.rotation, drawData.origin,
                        drawData.scale, drawData.effect));

            return false;
        }
    }
}
