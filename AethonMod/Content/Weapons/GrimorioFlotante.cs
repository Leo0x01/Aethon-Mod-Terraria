using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// GrimorioFlotante — v6.85 — EL GRIMORIO FLOTANDO EN VEZ DE EN LA
    /// ESPALDA.
    ///
    /// LA LETRA: «he visto que algunos mods ponen el arma del jugador en
    /// la espalda del personaje, hay alguna forma de hacer que si un mod
    /// intenta hacer eso y forzar esa posicion en el grimorio, en vez de
    /// la espalda, que se quede flotando, igual a como se ve cuando esta
    /// en la fase de hambre, pero en este caso no buscaria comida hasta
    /// que no entre a la fase de hambre».
    ///
    /// LA RESPUESTA DE LA CASA: una capa PlayerDrawLayer DETRÁS del
    /// jugador que dibuja EL LIBRO (el sprite del Nervioso — la misma
    /// fuente de arte del Eterno) flotando a su espalda con vaivén
    /// propio, SIEMPRE que el Eterno viaje en el inventario GUARDADO
    /// (no en la mano — la mano es del jugador). NINGÚN otro mod puede
    /// forzarle la espalda: su sitio natural es el aire.
    ///
    /// ES PURAMENTE VISUAL: la capa no toca NI UNA regla — la búsqueda
    /// de comida sigue siendo EXCLUSIVA de la fase de hambre (el espíritu
    /// del Nervioso, como siempre).
    /// </summary>
    public class GrimorioFlotante : PlayerDrawLayer
    {
        private static Asset<Texture2D> _libro;

        public override Position GetDefaultPosition()
            => PlayerDrawLayers.BeforeFirstVanillaLayer;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || !p.active || p.dead || p.stealth > 0.94f)
                return false;

            // v6.121 — EL LIBRO NO CABE DENTRO DE SÍ MISMO (la letra: el
            // grimorio NO debe aparecer en el mundo interior): el
            // portador siempre lleva al Eterno en la bolsa — y dentro
            // del CUERPO del dios, el libro que flota detrás del jugador
            // es un absurdo: estás DENTRO de él. AHÍ el libro no flota:
            // te envuelve. La capa muere mientras el interior viva.
            if (global::AethonMod.Content.Systems.SubmundoGrimorio.Activo)
                return false;

            // SOLO GUARDADO: si lo lleva en la mano, la mano manda (y el
            // uso del arma es del jugador).
            if (p.HeldItem != null && !p.HeldItem.IsAir &&
                p.HeldItem.type == ModContent.ItemType<GrimoireEternal>())
                return false;

            // ¿LLEVA EL ETERNO? (inventario — el libro guardado).
            for (int i = 0; i < p.inventory.Length; i++)
            {
                Item it = p.inventory[i];
                if (it != null && !it.IsAir &&
                    it.type == ModContent.ItemType<GrimoireEternal>())
                    return true;
            }
            return false;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (_libro == null)
                _libro = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambrientoNervioso",
                    AssetRequestMode.ImmediateLoad);

            Player p = drawInfo.drawPlayer;
            float t = Main.GlobalTimeWrappedHourly + p.whoAmI * 1.7f;

            float bob = (float)System.Math.Sin(t * 1.35f) * 4f;
            float tilt = 0.09f * (float)System.Math.Sin(t * 0.9f);
            Vector2 pos = new Vector2(
                p.Center.X - 34f * p.direction,
                p.Center.Y - 34f + bob + p.gfxOffY) - Main.screenPosition;

            var orig = _libro.Value.Size() * 0.5f;
            var origG = global::AethonMod.Content.VFX.VFXCore.SoftGlow.Size() * 0.5f;
            float esc = 0.85f;
            float dirX = p.direction == 1 ? 1f : -1f;

            var glow = global::AethonMod.Content.VFX.VFXCore.SoftGlow;
            drawInfo.DrawDataCache.Add(new DrawData(
                glow, pos, null, new Color(150, 90, 220) * 0.28f, 0f,
                origG, 1.0f, SpriteEffects.None, 0f));

            drawInfo.DrawDataCache.Add(new DrawData(
                _libro.Value, pos, null, Color.White,
                tilt, orig, esc * dirX, SpriteEffects.None, 0f));
        }
    }
}
