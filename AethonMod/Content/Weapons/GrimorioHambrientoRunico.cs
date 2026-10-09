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
    /// v6.50.86 — LA COPIA RÚNICA DEL GRIMORIO HAMBRIENTO (la animación del
    /// AUTOR). La letra del usuario: «te pido que hagas una copia del grimorio
    /// pero con alguna animacion que creas que sea correcta».
    ///
    /// MI ANIMACIÓN — el grimorio es un objeto VIVO que devora: lo correcto
    /// es que SE NOTE la magia circulando. Tres RUNAS DORADAS orbitan el ojo
    /// como satélites guardianes (elipse 11,5×8 px — justo por fuera del
    /// anillo dorado), cada una con su halo; cuando una pasa por DETRÁS del
    /// libro se atenúa (la ilusión de profundidad) y su brillo pulsa con su
    /// propia fase. Debajo de todo, un AURA que RESPIRA: el libro entero
    /// cubierto por un velo morado que late cada 3,5 s (dibujado BAJO el
    /// iris — el ojo nunca se entinta). EL HAMBRE LO ACELERA TODO: la órbita
    /// pasa de 5,2 s a 2,6 s por vuelta, el aura vira de morado a ROJO y las
    /// runas se encienden — el ciclo completo del original intacto (mismo
    /// ojo, mismo parpadeo PURO de código v6.50.86, misma descarga, mismo
    /// clic derecho que reinicia el apetito), con SU PROPIO EstadoGrimorio:
    /// las tres copias pasan hambre por separado.
    /// </summary>
    public class GrimorioHambrientoRunico : GrimorioHambriento
    {
        // la órbita: por fuera del anillo dorado (r≈9,7 px de juego)
        private const float ORBITA_X = 11.5f;
        private const float ORBITA_Y = 8.0f;
        private const float PERIODO_ORBITA_SACIADO = 5.2f;   // s por vuelta
        private const float PERIODO_ORBITA_HAMBRIENTO = 2.6f;

        private static readonly EstadoGrimorio _estadoRunico = new EstadoGrimorio();
        protected override EstadoGrimorio Estado => _estadoRunico;

        private static Color ConAlfa(Color c, float a) =>
            new Color(c.R, c.G, c.B, (int)(255f * Math.Clamp(a, 0f, 1f)));

        // --- EL AURA QUE RESPIRA (morado → rojo con el hambre) ---
        private Color ColorAura() =>
            Color.Lerp(new Color(198, 150, 255), new Color(255, 95, 80), Estado.NivelRojo());

        private float AlfaAura(float t) =>
            0.10f + 0.08f * (0.5f + 0.5f * (float)Math.Sin(t * MathHelper.TwoPi / 3.5f));

        private static Texture2D Runa(int i)
        {
            string[] nombres = { "RunaA", "RunaB", "RunaC" };
            return ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambrientoRunico_" + nombres[i]).Value;
        }

        /// <summary>LA ÓRBITA — las 3 runas alrededor del ojo (pantalla, no
        /// rotación del libro: son satélites, no pintura del libro).</summary>
        private void DibujarRunas(SpriteBatch spriteBatch, Vector2 posOjo, float escala)
        {
            float t = Main.GameUpdateCount / 60f;
            float periodo = MathHelper.Lerp(PERIODO_ORBITA_SACIADO, PERIODO_ORBITA_HAMBRIENTO, Estado.Hambre);
            Color oro = Color.Lerp(new Color(255, 216, 138), new Color(255, 110, 90), Estado.NivelRojo());

            for (int i = 0; i < 3; i++)
            {
                float ang = MathHelper.TwoPi * t / periodo + i * MathHelper.TwoPi / 3f;
                Vector2 offs = new Vector2((float)Math.Cos(ang) * ORBITA_X,
                                           (float)Math.Sin(ang) * ORBITA_Y);
                float pulso = 0.60f + 0.30f * (0.5f + 0.5f * (float)Math.Sin(ang * 2f + i * 2.1f));
                if ((float)Math.Sin(ang) < 0f)
                    pulso *= 0.55f;    // pasa por detrás: se atenúa (profundidad)

                var tex = Runa(i);
                Vector2 pos = posOjo + offs * escala;
                // el halo difuso detrás + el glifo nítido encima
                spriteBatch.Draw(tex, pos, null, ConAlfa(oro, pulso * 0.35f), 0f,
                    tex.Size() * 0.5f, escala * 1.9f, SpriteEffects.None, 0f);
                spriteBatch.Draw(tex, pos, null, ConAlfa(oro, pulso), 0f,
                    tex.Size() * 0.5f, escala * 0.72f, SpriteEffects.None, 0f);
            }
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // EL AURA primero — respira BAJO el ojo (la base dibuja el iris encima)
            float t = Main.GameUpdateCount / 60f;
            var aura = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(aura, position, frame, ConAlfa(ColorAura(), AlfaAura(t)), 0f,
                origin, scale * 1.05f, SpriteEffects.None, 0f);

            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);

            // LAS RUNAS: orbitan el ojo ENCIMA de todo
            Vector2 posOjo = position + (OJO - origin) * scale;
            DibujarRunas(spriteBatch, posOjo, scale);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // el aura con la MISMA transform vanilla del libro (v6.50.84)
            Main.GetItemDrawFrame(Item.type, out var _, out var frame);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pivote = Item.Bottom - Main.screenPosition - new Vector2(0f, origen.Y);

            float t = Main.GameUpdateCount / 60f;
            var aura = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(aura, pivote, frame, ConAlfa(ColorAura(), AlfaAura(t)), rotation,
                origen, scale * 1.05f, SpriteEffects.None, 0f);

            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);

            // el ojo del socket (sin el iris desplazado — las runas orbitan el OJO)
            Vector2 posOjo = pivote + ((OJO - origen) * scale).RotatedBy(rotation);
            DibujarRunas(spriteBatch, posOjo, scale);
        }

        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            float t = Main.GameUpdateCount / 60f;

            // EL AURA DEBAJO del libro (solo el borde sangra: está ×1,05)
            var aura = ModContent.Request<Texture2D>(Texture).Value;
            drawInfo.DrawDataCache.Add(new DrawData(aura, drawData.position, drawData.sourceRect,
                ConAlfa(ColorAura(), AlfaAura(t)), drawData.rotation, drawData.origin,
                drawData.scale * 1.05f, drawData.effect));

            // el libro + el ojo (la base agrega TODO — return false)
            base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            // LAS RUNAS: el texel del ojo con el ESPEJO del held item (v6.50.84)
            bool espejoX = (drawData.effect & SpriteEffects.FlipHorizontally) != 0;
            bool espejoY = (drawData.effect & SpriteEffects.FlipVertically) != 0;
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);
            Vector2 q = new Vector2(
                (espejoX ? fr.Width - OJO.X : OJO.X) - drawData.origin.X,
                (espejoY ? fr.Height - OJO.Y : OJO.Y) - drawData.origin.Y);
            Vector2 posOjo = drawData.position + new Vector2(
                q.X * drawData.scale.X, q.Y * drawData.scale.Y).RotatedBy(drawData.rotation);

            float periodo = MathHelper.Lerp(PERIODO_ORBITA_SACIADO, PERIODO_ORBITA_HAMBRIENTO, Estado.Hambre);
            Color oro = Color.Lerp(new Color(255, 216, 138), new Color(255, 110, 90), Estado.NivelRojo());
            float escala = Math.Abs(drawData.scale.X);

            for (int i = 0; i < 3; i++)
            {
                float ang = MathHelper.TwoPi * t / periodo + i * MathHelper.TwoPi / 3f;
                Vector2 offs = new Vector2((float)Math.Cos(ang) * ORBITA_X,
                                           (float)Math.Sin(ang) * ORBITA_Y);
                float pulso = 0.60f + 0.30f * (0.5f + 0.5f * (float)Math.Sin(ang * 2f + i * 2.1f));
                if ((float)Math.Sin(ang) < 0f)
                    pulso *= 0.55f;

                var tex = Runa(i);
                Vector2 pos = posOjo + offs * escala;
                drawInfo.DrawDataCache.Add(new DrawData(tex, pos, null,
                    ConAlfa(oro, pulso * 0.35f), 0f, tex.Size() * 0.5f, escala * 1.9f,
                    SpriteEffects.None));
                drawInfo.DrawDataCache.Add(new DrawData(tex, pos, null,
                    ConAlfa(oro, pulso), 0f, tex.Size() * 0.5f, escala * 0.72f,
                    SpriteEffects.None));
            }

            return false;
        }
    }
}
