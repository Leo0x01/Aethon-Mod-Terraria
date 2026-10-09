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
    /// v6.50.87 — LA COPIA TEMBLOROSA DEL GRIMORIO HAMBRIENTO (nació Estelar
    /// en la .86; el usuario: «deja solo el grimorio hambriento normal y
    /// modifica las dos copias con otros efectos, borra los anteriores, esta
    /// vez que sean mas suave los efectos… que la otra lo que haga sea
    /// temblar»). Las estrellas titilando y las venas viajeras quedaron
    /// BORRADAS — el usuario las vio «bastante mal y exageradas».
    ///
    /// EL TEMBLOR — DOS SENOS INCOMMENSURABLES POR EJE: 9,3 Hz y 17,3 Hz en
    /// X, 11,9 Hz y 19,1 Hz en Y (fases distintas). El batido de los dos
    /// senos por eje es un escalofrío ORGÁNICO — nada de ruido por frame:
    /// el ruido titila (la cara de la .86), los senos respiran. La amplitud
    /// crece con el apetito: 0,2 px saciado (apenas un susurro — el libro
    /// se ve entero) → 1,3 px famélico (el libro entero tiembla; aún SUAVE:
    /// ~3 % del alto del libro, cuantizado al píxel como manda el pixel-art
    /// de Terraria).
    ///
    /// El corrimiento se aplica al libro Y a sus capas (el párpado y el iris
    /// tiemblan JUNTO al libro, nunca desalineados) en los TRES estados:
    /// inventario (PreDraw false + réplica del draw vanilla), mundo (la
    /// convención v6.50.84 con la rotación de vuelo) y mano (el temblor se
    /// inyecta en la DrawData: la base dibuja el ojo ya corrido). El ciclo
    /// del original queda INTACTO con SU PROPIO EstadoGrimorio.
    /// </summary>
    public class GrimorioHambrientoTembloroso : GrimorioHambriento
    {
        private static readonly EstadoGrimorio _estadoTembloroso = new EstadoGrimorio();
        protected override EstadoGrimorio Estado => _estadoTembloroso;

        /// <summary>EL TEMBLOR de este tick (px de juego, en unidades del
        /// libro — el llamador lo escala a su contexto). Determinista puro:
        /// una función del tick, sin estado.</summary>
        private static Vector2 Temblor()
        {
            float t = Main.GameUpdateCount / 60f;
            float amp = 0.20f + 1.10f * _estadoTembloroso.Hambre;   // 0,2 px → 1,3 px
            float x = (float)Math.Sin(t * 9.3f * MathHelper.TwoPi) * 0.80f
                    + (float)Math.Sin(t * 17.3f * MathHelper.TwoPi) * 0.25f;
            float y = (float)Math.Sin(t * 11.9f * MathHelper.TwoPi + 1.7f) * 0.80f
                    + (float)Math.Sin(t * 19.1f * MathHelper.TwoPi + 0.6f) * 0.25f;
            return new Vector2(x, y) * amp;
        }

        // =================================================================
        // INVENTARIO — el libro entero lo dibuja la copia (PreDraw false):
        // vanilla dibujaría el libro fijo; aquí viene corrido por el temblor.
        // =================================================================
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
            => false;

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 temblor = Temblor() * scale;

            // el libro entero, corrido — réplica del draw vanilla (ItemSlot:
            // position es el CENTRO, origin el centro del frame, rotación 0)
            spriteBatch.Draw(ModContent.Request<Texture2D>(Texture).Value,
                position + temblor, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

            // el ojo (párpado o iris) — el MISMO corrimiento: tiembla con el libro
            base.PostDrawInInventory(spriteBatch, position + temblor, frame, drawColor,
                itemColor, origin, scale);
        }

        // =================================================================
        // MUNDO — la convención vanilla v6.50.84 (pivote + rotación de vuelo).
        // =================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
            => false;

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 temblor = Temblor() * scale;
            Vector2 pivote = PivoteEnMundo(out var frame, out var origen);

            // el libro, temblando, con la rotación de vuelo
            spriteBatch.Draw(ModContent.Request<Texture2D>(Texture).Value,
                pivote + temblor, frame, lightColor, rotation, origen, scale, SpriteEffects.None, 0f);

            // el ojo (párpado o iris) — el mismo corrimiento
            PostDrawInWorldCore(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI, temblor);
        }

        // =================================================================
        // MANO — el temblor se INYECTA en la DrawData (posición): la base
        // agrega el libro, el párpado y el iris ya corridos — el ojo tiembla
        // JUNTO al libro, con el espejo y la gravedad de la v6.50.84 intactos.
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            drawData.position += Temblor() * Math.Abs(drawData.scale.X);
            return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);
        }
    }
}
