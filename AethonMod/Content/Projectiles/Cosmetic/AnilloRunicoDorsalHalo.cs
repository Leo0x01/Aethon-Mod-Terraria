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
    /// AnilloRunicoDorsalHalo — v6.37 — EL CÍRCULO RÚNICO EN LA ESPALDA.
    ///
    /// EL HALO COSMÉTICO del anillo dorsal: un proyectil sin daño pegado
    /// a su dueño que pinta LA CORONA DE CONJURO DEL VACÍO — la TRIPLE
    /// CORONA RÚNICA LITERAL de los agujeros negros (los círculos
    /// dorado/violeta/blanco con las runas de pie y sus perlas + el
    /// anillo de fotones) — detrás del cuerpo del portador.
    ///
    /// Por qué un proyectil y no una capa (v6.37 — el patrón probado de
    /// AnillosSingularesHalo): el renderer de la corona abre y CIERRA su
    /// propio SpriteBatch (el contrato de SelloVacio) — en el PreDraw de
    /// un proyectil el lote está BAJO NUESTRO CONTROL, mientras que las
    /// capas de jugador deben salir por la puerta oficial de DrawData.
    /// Y el orden de vanilla dibuja los proyectiles ANTES que los
    /// jugadores: la corona queda DETRÁS del cuerpo — colgada de LA
    /// ESPALDA, como pidió el usuario. (La v6.36 lo dibujaba por la capa
    /// BackAcc; la reconstrucción sobre la librería corregida pasa al
    /// camino del proyectil para invocar las primitivas 1:1 de OrbitaLib.)
    ///
    /// El spawn y la vida los lleva CosmeticPlayer (el dueño local lo
    /// invoca; tML lo sincroniza — netImportant).
    /// </summary>
    public class AnilloRunicoDorsalHalo : ModProjectile
    {
        // La textura FANTASMA de los proyectiles 100%-código (el patrón de
        // AuraPortadorHalo/AtaqueJefeProjectile): tML exige el asset default
        // de la clase aunque el PreDraw jamás lo dibuje — sin este override,
        // MissingResourceException y TODO el mod se desactiva al cargar
        // (la lección del client.log de la v6.50.3).
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
            Projectile.timeLeft = 6;          // CosmeticPlayer lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // SIN hide (la lección v6.35 de los desgarros: tML no dibuja
            // los proyectiles ocultos y PreDraw jamás corría).
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // La corona vive mientras su dueño viva y lleve el anillo.
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<CosmeticPlayer>().AnilloDorsal)
            {
                Projectile.Kill();
                return;
            }

            // Pegado a la espalda del portador.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // La luz de la corona (dorada cálida con el borde violeta).
            Lighting.AddLight(Projectile.Center, 0.20f, 0.14f, 0.06f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active) return false;

            // EL PATRÓN A PRUEBA DE BALAS (v6.10): el lote del pase se
            // cierra, la corona abre y cierra el SUYO (aditivo con
            // GameViewMatrix), y el lote del pase se reabre TAL CUAL.
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            // El ancla: el centro de la espalda (consciente de la
            // gravedad invertida — la corona viaja con el pecho).
            Vector2 espalda = duenio.Center -
                new Vector2(0f, duenio.height * 0.04f * duenio.gravDir);

            try
            {
                AnilloDorsalRenderer.Draw(espalda, duenio.height,
                    Main.GlobalTimeWrappedHourly, 1f);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }
    }
}
