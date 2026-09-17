using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// SelloGenesisDrawLayer — v6.35 — LOS SELLOS QUE RODEAN AL JUGADOR.
    ///
    /// La capa que pinta EL SELLO DEL GÉNESIS: DOS círculos mágicos de
    /// SigiloLib centrados en el cuerpo del portador — el ARO MAYOR
    /// dorado (aro doble + las ocho runas de pie + los cuatro nodos
    /// cardinales + el sigilo maestro ×2.3) girando a la cadencia de la
    /// casa, y el SELLO INTERIOR azul-estelar contrarrotando más
    /// rápido con el glifo del ASTRO en el corazón. Todo sale del
    /// buffer de VFXCore y entra al PlayerDrawSet por la puerta oficial
    /// (AppendToPlayerDraw — el mismo camino de las coronas, sin tocar
    /// el estado del renderer).
    /// </summary>
    public class SelloGenesisDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Los sellos ENVUELVEN al jugador: por delante de la cara
            // pero respetando al sprite — las runas cruzan el cuerpo
            // como la escritura que lo firma.
            return new AfterParent(PlayerDrawLayers.FaceAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<SellosPlayer>().SelloGenesis;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            float time = Main.GlobalTimeWrappedHourly;

            // El radio del sello MAYOR: rodea al cuerpo entero (el
            // jugador mide ~42 px — el aro lo envuelve con aire).
            float R = p.height * 1.05f;

            // La respiración conjunta (los sellos están VIVOS).
            float aliento = 0.78f + 0.08f * (float)System.Math.Sin(time * 1.30f);

            VFXCore.Begin();

            // --- 1. EL SELLO MAYOR: la escritura solar dorada completa
            //     (aro doble + 8 runas + 4 nodos + sigilo maestro). ---
            SigiloLib.SelloSolar(p.Center, R, time, p.whoAmI * 17 + 3,
                body: null, tip: null, alpha: aliento,
                giro: 0.22f, glifoCentral: 7);

            // --- 2. EL SELLO INTERIOR: azul-estelar, CONTRARROTANDO
            //     (−0.35 rad/s — el contrapeso frío), con el glifo del
            //     ASTRO (índice 0) en el corazón. ---
            SigiloLib.SelloSolar(p.Center, R * 0.55f, time, p.whoAmI * 31 + 11,
                body: SigiloLib.CuerpoAzul, tip: SigiloLib.PuntaAzul,
                alpha: aliento * 0.78f, giro: -0.35f, glifoCentral: 0);

            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }

    /// <summary>
    /// AnillosSolaresDrawLayer — v6.35 — EL SISTEMA DE ANILLOS DEL SOL
    /// RODEANDO AL JUGADOR.
    ///
    /// La capa que pinta LOS ANILLOS DEL SOL RÚNICO: SigiloLib.
    /// SistemaAnillos en SU configuración viva — tier 7 (los planos
    /// PRECESAN y cada tercer aro viste el azul-estelar), giros
    /// alternos par/impar, runas a lomos de cada tangente. El jugador
    /// es el corazón de la constelación; el sistema entero respira
    /// con él por la puerta oficial del PlayerDrawSet.
    /// </summary>
    public class AnillosSolaresDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Los anillos orbitan ALREDEDOR: por delante de la cara,
            // encima del sprite (una órbita pasa por delante del cuerpo
            // — así se lee la tercera dimensión).
            return new AfterParent(PlayerDrawLayers.FaceAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<SellosPlayer>().AnillosSol;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            float time = Main.GlobalTimeWrappedHourly;

            // La base del sistema: el primer aro (×1.62) despeja el
            // cuerpo con holgura y el séptimo llega a ~4.3×R — la
            // constelación envuelve sin sepultar.
            float R = p.height * 0.46f;

            VFXCore.Begin();

            // EL SISTEMA COMPLETO — tier 7 de 20 (precesión viva +
            // azul-estelar cada tercer aro), vida plena (lifeT 0),
            // intensidad de accesorio (0.8 — presente sin gritar).
            SigiloLib.SistemaAnillos(p.Center, R, time,
                p.whoAmI * 7 + 5, tier: 7, rg: 0f, lifeT: 0f, alpha: 0.80f);

            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
