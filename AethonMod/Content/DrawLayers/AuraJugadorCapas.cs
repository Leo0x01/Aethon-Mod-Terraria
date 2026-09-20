using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// AuraJugadorFrontal — v6.47 — EL VELO DEL HAMBRE SOBRE EL PORTADOR.
    ///
    /// La capa FRONTAL (AfterParent de FaceAcc — la altura de las coronas
    /// de la casa): la ceniza al 5% pisando el cuerpo. La criatura emite
    /// desde dentro.
    ///
    /// v6.48 — EL VELO DE TODAS LAS AURAS DEL JUGADOR: la CAPA TRASERA se
    /// mudó al PORTADOR (AuraPortadorHalo, el camino aditivo del
    /// halo-proyectil — el neón de verdad de los NPCs); ESTA capa queda
    /// para el VELO FRONTAL que PISA el sprite (los proyectiles dibujan
    /// antes que el jugador: no pueden pisarlo). El velo elige el perfil
    /// de la primera aura viva: la ceniza del hambre (solo el local), la
    /// FORMA ASCENDIDA (el drop cumplido de Aethon) o la CORONA RÚNICA.
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
            return PerfilDe(p) != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            try
            {
                Player p = drawInfo.drawPlayer;
                if (p == null || p.dead) return;
                AuraPerfil perfil = PerfilDe(p);
                if (perfil == null) return;

                AuraLib.DibujarJugador(ref drawInfo, perfil, frontal: true);
            }
            catch { }
        }

        /// <summary>
        /// El perfil del VELO: la primera aura viva del jugador (hambre →
        /// forma ascendida → corona rúnica). La ceniza del hambre es solo
        /// del jugador LOCAL (el hambre es tuya); los cosméticos, de quien
        /// los lleve puesto.
        /// </summary>
        internal static AuraPerfil PerfilDe(Player p)
        {
            if (p == null || p.dead) return null;
            if (p.whoAmI == Main.myPlayer)
            {
                var sp = p.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.MomentosHambre > 0) return sp.AuraHambrePublica();
            }
            var cp = p.GetModPlayer<CosmeticPlayer>();
            if (cp != null)
            {
                if (cp.FormaAscendida) return AuraPerfil.FormaAscendida();
                if (cp.CoronaRunicaAura) return AuraPerfil.CoronaRunica();
            }
            return null;
        }
    }
}
