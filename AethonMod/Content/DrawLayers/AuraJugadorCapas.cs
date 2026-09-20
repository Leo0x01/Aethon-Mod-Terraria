using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// AuraJugadorTrasera — v6.47 — LA CENIZA DETRÁS DEL PORTADOR.
    ///
    /// La capa TRASERA del aura del hambre del grimorio: se dibuja ANTES
    /// de las capas del cuerpo (BeforeParent de MountBack — el sitio de
    /// las cosas que el jugador pisa con su sprite) por el camino DrawData
    /// de VFXCore (AppendToPlayerDraw), el mismo de las coronas de la
    /// casa. Solo el jugador LOCAL la viste: el hambre es tuya.
    /// </summary>
    public class AuraJugadorTrasera : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.MountBack);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            if (p.whoAmI != Main.myPlayer) return false; // el hambre es del local
            var sp = p.GetModPlayer<ShardPlayer>();
            return sp != null && sp.MomentosHambre > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            try
            {
                Player p = drawInfo.drawPlayer;
                if (p == null || p.dead) return;
                var sp = p.GetModPlayer<ShardPlayer>();
                if (sp == null || sp.MomentosHambre <= 0) return;

                AuraLib.DibujarJugador(ref drawInfo, sp.AuraHambrePublica(), frontal: false);
            }
            catch { }
        }
    }

    /// <summary>
    /// AuraJugadorFrontal — v6.47 — EL VELO DEL HAMBRE SOBRE EL PORTADOR.
    ///
    /// La capa FRONTAL (AfterParent de FaceAcc — la altura de las coronas
    /// de la casa): la misma ceniza al 5% pisando el cuerpo. La criatura
    /// emite desde dentro.
    /// </summary>
    public class AuraJugadorFrontal : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.FaceAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            if (p.whoAmI != Main.myPlayer) return false;
            var sp = p.GetModPlayer<ShardPlayer>();
            return sp != null && sp.MomentosHambre > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            try
            {
                Player p = drawInfo.drawPlayer;
                if (p == null || p.dead) return;
                var sp = p.GetModPlayer<ShardPlayer>();
                if (sp == null || sp.MomentosHambre <= 0) return;

                AuraLib.DibujarJugador(ref drawInfo, sp.AuraHambrePublica(), frontal: true);
            }
            catch { }
        }
    }
}
