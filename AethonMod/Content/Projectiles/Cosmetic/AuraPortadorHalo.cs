using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AuraPortadorHalo — v6.48 — EL PORTADOR DEL AURA DEL JUGADOR.
    ///
    /// LA MEJORA DE AURALIB que cierra el huecho del camino del jugador:
    /// los NPCs dibujan su aura ADITIVA (neón) desde PreDraw; el jugador
    /// iba por DrawData (AlphaBlend) y las auras de color vivo salían
    /// PLANAS (la lección v6.40). Este proyectil cosmético pegado a su
    /// dueño dibuja la CAPA TRASERA del aura por el CAMINO ADITIVO:
    /// vanilla dibuja los proyectiles ANTES que los jugadores → la capa
    /// queda DETRÁS del cuerpo (la profundidad del look, igual que los
    /// NPCs) y con el brillo de neón de verdad.
    ///
    /// LOS TRES PORTADORES (ai[0] = modo):
    ///   0 — LA CENIZA DEL HAMBRE: el aura gris del grimorio hambriento
    ///       (la lee de ShardPlayer, el jugador local la viste).
    ///   1 — LA CORONA RÚNICA: el pentágono Polígono(5) violeta-oro de
    ///       la Bolsa de Cosméticos (CosmeticPlayer la enciende).
    ///   2 — LA FORMA ASCENDIDA: el aura dorada-violeta de la Luz
    ///       Primordial (el drop cumplido de Aethon).
    ///
    /// El VELO FRONTAL (el 6% que pisa el cuerpo) sigue por DrawData
    /// (AuraJugadorFrontal): a esa transparencia no necesita neón y la
    /// capa frontal debe pisar el sprite — los proyectiles dibujan antes.
    ///
    /// Patrón probado de AnilloRunicoDorsalHalo (v6.37): PreDraw cierra
    /// el lote del pase, AuraLib vuelca el SUYO aditivo y el lote del
    /// pase se reabre TAL CUAL. SIN hide (la lección v6.35).
    /// </summary>
    public class AuraPortadorHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;      // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;          // el dueño lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        /// <summary>El modo del portador (0 hambre · 1 corona · 2 ascendida).</summary>
        private int Modo => (int)Projectile.ai[0];

        /// <summary>¿El dueño sigue VISTIENDO el aura de este modo?</summary>
        private bool SigueViva(Player duenio)
        {
            switch (Modo)
            {
                case 0:
                    return duenio.whoAmI == Main.myPlayer &&
                           duenio.GetModPlayer<ShardPlayer>().MomentosHambre > 0;
                case 1:
                    return duenio.GetModPlayer<CosmeticPlayer>().CoronaRunicaAura;
                case 2:
                    return duenio.GetModPlayer<CosmeticPlayer>().FormaAscendida;
            }
            return false;
        }

        /// <summary>El PERFIL del aura de este modo.</summary>
        private AuraPerfil Perfil(Player duenio)
        {
            switch (Modo)
            {
                case 0: return duenio.GetModPlayer<ShardPlayer>().AuraHambrePublica();
                case 1: return AuraPerfil.CoronaRunica();
                case 2: return AuraPerfil.FormaAscendida();
            }
            return null;
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            if (duenio == null || !duenio.active || duenio.dead || !SigueViva(duenio))
            {
                Projectile.Kill();
                return;
            }

            // Pegado al dueño, siempre.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // LAS PARTÍCULAS corren en la AI (el contrato de la casa —
            // nunca en el render).
            AuraPerfil p = Perfil(duenio);
            if (p != null)
                AuraLib.ActualizarJugador(duenio, p);

            // La luz suave del modo (la corona ilumina, el hambre oscurece).
            if (Modo == 1) Lighting.AddLight(Projectile.Center, 0.14f, 0.08f, 0.02f);
            else if (Modo == 2) Lighting.AddLight(Projectile.Center, 0.20f, 0.16f, 0.08f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return false;

            AuraPerfil p = Perfil(duenio);
            if (p == null) return false;

            // EL PATRÓN A PRUEBA DE BALAS (v6.10): cerrar el lote del pase,
            // AuraLib vuelca el SUYO aditivo y reabrir TAL CUAL estaba.
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                AuraLib.DibujarJugadorAditivo(duenio, p);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
