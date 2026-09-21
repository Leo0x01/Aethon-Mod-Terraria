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
    /// SelloGenesisHalo — v6.40 — LOS SELLOS DEL GÉNESIS EN EL VACÍO ADITIVO.
    ///
    /// EL HALO COSMÉTICO del Sello del Génesis — v6.40 LA MIGRACIÓN DEL
    /// CAMINO: el sello YA NO sale por la capa de jugador (DrawData): el
    /// pase de dibujado del jugador mezcla con `BlendState.AlphaBlend` (la
    /// fórmula PREmultiplicada One/InverseSourceAlpha de FNA — verificada
    /// contra el IL del FNA.dll real) y los tintes de la casa llevan la
    /// intensidad SOLO en el alfa con RGB intacto → en esa fórmula el RGB
    /// entra ENTERO donde la textura tiene alfa (y SoftGlow lo tiene en
    /// toda su extensión) → el sello se dibujaba como MANCHAS DE COLOR
    /// PLANO (el reporte del usuario: "un intento de anillo pero se ve
    /// color plano").
    ///
    /// El camino NUEVO (el de los soles rúnicos que el usuario cita como
    /// referencia — y el de los desgarros, el dorsal y los anillos del
    /// horizonte): un proyectilo halo sin daño que pega a su dueño y
    /// vuelca LOS MISMOS sellos por VFXCore.FlushAdditive — el lote
    /// aditivo propio (SourceAlpha/One: el alfa GATEA el color, el glow
    /// es un glow de verdad). Las llamadas son LITERALMENTE las de la
    /// capa vieja (ni un número cambiado — la ley de la casa); solo
    /// cambió la PUERTA por la que salen.
    ///
    /// Orden de vanilla: los proyectiles se dibujan ANTES que los
    /// jugadores → el sello pasa POR DETRÁS del cuerpo — la escritura
    /// ENVUELVE al portador (como el vórtice del horizonte y la corona
    /// dorsal).
    ///
    /// El spawn y la vida los lleva SellosPlayer (el dueño local lo
    /// invoca; tML lo sincroniza — netImportant).
    /// </summary>
    public class SelloGenesisHalo : ModProjectile
    {
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
            // SIN hide (la lección v6.35: tML no dibuja los proyectiles
            // ocultos y PreDraw jamás corría).
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // El sello vive mientras su dueño viva y lleve el accesorio.
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<SellosPlayer>().SelloGenesis)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al cuerpo del portador (la escritura lo envuelve).
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // Luz de oro suave (la escritura ilumina).
            Lighting.AddLight(Projectile.Center, 0.26f, 0.20f, 0.07f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active) return false;

            // EL PATRÓN A PRUEBA DE BALAS: el lote del pase se cierra, los
            // sellos vuelcan por SU lote aditivo (FlushAdditive abre y
            // cierra el suyo con GameViewMatrix), y el lote del pase se
            // reabre TAL CUAL estaba.
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            float time = Main.GlobalTimeWrappedHourly;

            // El radio del sello MAYOR: rodea al cuerpo entero (la
            // constante de la capa de siempre — ni un número cambiado).
            float R = duenio.height * 1.05f;

            // La respiración conjunta (los sellos están VIVOS).
            float aliento = 0.78f + 0.08f * (float)System.Math.Sin(time * 1.30f);

            try
            {
                VFXCore.Begin();

                // --- 1. EL SELLO MAYOR: la escritura solar dorada completa
                //     (aro doble + 8 runas + 4 nodos + sigilo maestro). ---
                SigiloLib.SelloSolar(duenio.Center, R, time, duenio.whoAmI * 17 + 3,
                    body: null, tip: null, alpha: aliento,
                    giro: 0.22f, glifoCentral: 7);

                // --- 2. EL SELLO INTERIOR: azul-estelar, CONTRARROTANDO
                //     (−0.35 rad/s), con el glifo del ASTRO en el corazón. ---
                SigiloLib.SelloSolar(duenio.Center, R * 0.55f, time, duenio.whoAmI * 31 + 11,
                    body: SigiloLib.CuerpoAzul, tip: SigiloLib.PuntaAzul,
                    alpha: aliento * 0.78f, giro: -0.35f, glifoCentral: 0);

                VFXCore.FlushAdditive(null, false);   // el lote del pase YA está cerrado
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

    /// <summary>
    /// AnillosSolaresHalo — v6.40 — LOS ANILLOS DEL SOL RÚNICO EN EL VACÍO
    /// ADITIVO.
    ///
    /// EL HALO COSMÉTICO de los Anillos del Sol Rúnico — la misma
    /// migración v6.40 que el Sello del Génesis (la capa DrawData dibujaba
    /// los anillos PLANOS: el pase del jugador es premultiplicado y los
    /// tintes de la casa no lo son). El sistema completo de anillos del
    /// sol rúnico — tier 7, precesión viva, azul-estelar cada tercer aro,
    /// giros alternos — sale ahora por el lote aditivo propio, EL MISMO
    /// volcado por el que salen los anillos de los SOLES RÚNICOS reales
    /// (FlushAdditive: SourceAlpha/One — el alfa gatea el color y el glow
    /// es un glow). Las llamadas son las de la capa de siempre, literales.
    ///
    /// Orden de vanilla: proyectiles antes que jugadores → los anillos
    /// pasan POR DETRÁS del cuerpo — la constelación ENVUELVE al portador
    /// (el jugador es el corazón; su cuerpo ocupa el centro).
    /// </summary>
    public class AnillosSolaresHalo : ModProjectile
    {
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
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<SellosPlayer>().AnillosSol)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al cuerpo (el jugador ES el corazón de la constelación).
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // Luz cálida de estrella viva.
            Lighting.AddLight(Projectile.Center, 0.30f, 0.22f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            float time = Main.GlobalTimeWrappedHourly;

            // La base del sistema de la capa de siempre (el primer aro
            // despeja el cuerpo y el séptimo llega a ~4.3×R).
            float R = duenio.height * 0.46f;

            try
            {
                VFXCore.Begin();

                // EL SISTEMA COMPLETO — tier 7 de 20 (precesión viva +
                // azul-estelar cada tercer aro), vida plena, intensidad
                // de accesorio — LITERAL de la capa vieja.
                SigiloLib.SistemaAnillos(duenio.Center, R, time,
                    duenio.whoAmI * 7 + 5, tier: 7, rg: 0f, lifeT: 0f, alpha: 0.80f);

                VFXCore.FlushAdditive(null, false);   // el lote del pase YA está cerrado
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
