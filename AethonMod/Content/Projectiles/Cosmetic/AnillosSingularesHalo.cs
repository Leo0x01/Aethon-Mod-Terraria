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
    /// AnillosSingularesHalo — v6.35 — LOS ANILLOS DEL AGUJERO NEGRO
    /// RODEANDO AL JUGADOR.
    ///
    /// EL HALO COSMÉTICO del accesorio: un proyectivo sin daño que
    /// vive pegado a su dueño y pinta, con UNA sola llamada, EL SELLO
    /// DEL VACÍO de OrbitaLib — el disco de acreción con sus veinte
    /// bandas viajando (la fórmula fiel del shader), los ecos en
    /// resonancia, el aro fino del horizonte, los fotones corriendo el
    /// vórtice y las ondas de distorsión pulsando. El JUGADOR es el
    /// núcleo negro: el sello no lo dibuja porque el cuerpo del
    /// portador YA ocupa ese lugar.
    ///
    /// Por qué un proyectil y no una capa: el contrato de SelloVacio
    /// abre y CIERRA su propio SpriteBatch — en el PreDraw de un
    /// proyectivo el lote está BAJO NUESTRO CONTROL (el patrón a prueba
    /// de balas de los desgarros), mientras que las capas de jugador
    /// deben salir por la puerta oficial de DrawData. Además el orden
    /// de vanilla dibuja los proyectivos ANTES que los jugadores: los
    /// anillos quedan DETRÁS del cuerpo — el vórtice te envuelve.
    ///
    /// El spawn y la vida los lleva SellosPlayer (el dueño local lo
    /// invoca; tML lo sincroniza — netImportant).
    /// </summary>
    public class AnillosSingularesHalo : ModProjectile
    {
        /// <summary>Radio del vórtice (px) — el anillo llega a ~1.9×.</summary>
        private const float Radio = 26f;

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
            Projectile.timeLeft = 6;          // SellosPlayer lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35 — SIN hide (la lección de los desgarros: tML no
            // dibuja los proyectivos ocultos y PreDraw jamás corría).
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // El halo vive mientras su dueño viva y lleve el anillo.
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<SellosPlayer>().AnillosVacio)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al cuerpo del portador (el jugador ES el núcleo).
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // La luz del disco de acreción (rojo-naranja tenue).
            Lighting.AddLight(Projectile.Center, 0.24f, 0.08f, 0.02f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // EL PATRÓN A PRUEBA DE BALAS (v6.10): el lote del pase se
            // cierra, SelloVacio abre y cierra el SUYO (aditivo con
            // GameViewMatrix), y el lote del pase se reabre TAL CUAL.
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                OrbitaLib.SelloVacio(
                    Projectile.Center - Main.screenPosition,
                    Radio,
                    Main.GlobalTimeWrappedHourly,
                    Projectile.owner * 13 + 7,
                    vivo: null, profundo: null, caliente: null,
                    alpha: 0.90f);
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
