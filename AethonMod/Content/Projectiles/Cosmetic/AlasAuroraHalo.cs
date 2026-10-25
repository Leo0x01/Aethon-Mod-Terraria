using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// ALASAURORAHALO — v6.52.1 — EL PORTADOR DE LAS ALAS DE CÓDIGO.
    ///
    /// El patrón probado de la casa (AuraPortadorHalo v6.49 / el contrato
    /// documentado en AuraLib: «vanilla dibuja los proyectiles ANTES que
    /// los jugadores → la capa queda DETRÁS del cuerpo» — la posición
    /// EXACTA de unas alas): un proyectil cosmético pegado al dueño,
    /// intocable, que vive mientras la LA AURORA DE TINTA esté equipada.
    ///
    /// LA ANIMACIÓN (los drivers del original, respetados):
    ///   · WINGSPREAD — el ala SEMI-ABIERTA en el suelo (0.35), se abre
    ///     con la velocidad y EL VUELO (techo 1), suavizada con Lerp
    ///     0.085 (el despliegue es un acto, no un switch).
    ///   · WINGFLAP — la fase acumulada: 0.055/t al descansar, +0.15 al
    ///     volar, +spread·0.03 (aletea más fuerte cuanto más abierta).
    ///   · LA MEZCLA DÍA/NOCHE — la forma diurna es TINTA BLANCO-ORO, la
    ///     nocturna es el PRISMA COMPLETO (la dualidad del original
    ///     adaptada a la tinta del Escriba), transición lenta (0.025).
    ///
    /// EL DESCANSO: si el portador pone ALAS REALES (Player.wings ≠ 0) o
    /// oculta el visual, el ala se PLEGÁ suavemente (spread → 0) — la
    /// aurora cede el sitio; la FÍSICA del accesorio sigue (ver
    /// AlasAuroraPlayer).
    ///
    /// El estado visual vive en localAI (cada cliente lo aproxima de la
    /// velocidad — como el original: «visual de cada extremo local»).
    /// </summary>
    public class AlasAuroraHalo : ModProjectile
    {
        // v6.52.1 — el placeholder de la casa (el cuerpo ES la librería).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;          // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;              // el dueño lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        // === EL ESTADO VISUAL (localAI: 0 spread · 1 flap · 2 mezcla día) ===
        private float Spread
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }
        private float Flap
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }
        private float MezclaDia
        {
            get => Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead || !SigueViva(duenio))
            {
                // LA DESPEDIDA: un suspiro de tinta prismática al morir.
                if (Main.netMode != NetmodeID.Server && Spread > 0.05f)
                    SuspiroFinal();
                Projectile.Kill();
                return;
            }

            // Pegado al dueño, siempre.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // === LOS DRIVERS DEL ORIGINAL (UpdateWingVisual, respetado) ===
            float objetivo = 0f;
            bool volando = false;
            if (!duenio.GetModPlayer<AlasAuroraPlayer>().OcultarVisual &&
                duenio.wings == 0)   // alas reales puestas → la aurora DESCANSA
            {
                float rapidez = duenio.velocity.Length();
                volando = duenio.velocity.Y != 0f;
                objetivo = MathHelper.Clamp(0.35f + rapidez * 0.055f + (volando ? 0.25f : 0f), 0.35f, 1f);
            }
            Spread = MathHelper.Lerp(Spread, objetivo, 0.085f);
            Flap += 0.055f + (volando ? 0.15f : 0f) + Spread * 0.03f;
            MezclaDia = MathHelper.Lerp(MezclaDia, Main.dayTime ? 1f : 0f, 0.025f);

            // === LA LUZ (el prisma que ilumina — la fórmula del original
            //     adaptada a la tinta: violeta-noche ↔ blanco-oro-día) ===
            if (Spread > 0.05f && Main.netMode != NetmodeID.Server)
            {
                float h = (Flap * 0.02f) % 1f;
                Color prisma = ColorPrisma(h);
                float d = MezclaDia;
                Vector3 luz = new(
                    (prisma.R / 255f * (1f - d) + 0.95f * d) * 0.42f * Spread,
                    (prisma.G / 255f * (1f - d) + 0.88f * d) * 0.42f * Spread,
                    (prisma.B / 255f * (1f - d) + 0.66f * d) * 0.42f * Spread);
                Lighting.AddLight(duenio.Center, luz);
            }
        }

        /// <summary>¿La aurora sigue equipada y viva en su dueño?</summary>
        private static bool SigueViva(Player duenio)
        {
            return duenio.GetModPlayer<AlasAuroraPlayer>().VisualViva;
        }

        /// <summary>El color prisma (el de la casa).</summary>
        private static Color ColorPrisma(float h)
        {
            h -= MathF.Floor(h);
            float r, g, b;
            if (h < 1f / 6f) { r = 1f; g = h * 6f; b = 0f; }
            else if (h < 2f / 6f) { r = 2f - h * 6f; g = 1f; b = 0f; }
            else if (h < 3f / 6f) { r = 0f; g = 1f; b = h * 6f - 2f; }
            else if (h < 4f / 6f) { r = 0f; g = 4f - h * 6f; b = 1f; }
            else if (h < 5f / 6f) { r = h * 6f - 4f; g = 0f; b = 1f; }
            else { r = 1f; g = 0f; b = 6f - h * 6f; }
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f));
        }

        /// <summary>La despedida: 14 gotas de tinta prismática cayendo.</summary>
        private void SuspiroFinal()
        {
            for (int i = 0; i < 14; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.ShadowbeamStaff,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(0.8f, 2.6f),
                    160, ColorPrisma(i / 14f), 1.1f);
                d.noGravity = true;
            }
        }

        // ==================================================================
        //  EL DIBUJO (el contrato del BOOL de la casa — AuraPortadorHalo
        //  v6.49: la librería cierra el lote del pase, vuelca SUS
        //  primitivas y el lote se reabre TAL CUAL)
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return false;
            if (Spread <= 0.02f) return false;

            // LA TINTA QUE GOTEA: de vez en cuando una mota prismática
            // cae de la punta de un ala (la vida de la aurora — el
            // original siembra chispas de luz en su AI).
            if (Main.rand.NextBool(22))
            {
                Vector2 punta = duenio.MountedCenter + new Vector2(
                    -duenio.direction * Main.rand.NextFloat(20f, 60f),
                    -Main.rand.NextFloat(30f, 80f) * duenio.gravDir);
                Dust d = Dust.NewDustPerfect(punta, DustID.ShadowbeamStaff,
                    new Vector2(0f, 0.4f), 140,
                    ColorPrisma(Main.rand.NextFloat()), 0.8f);
                d.noGravity = false;
            }

            try
            {
                AlasAuroraLib.Dibujar(duenio, Spread, Flap, MezclaDia);
            }
            catch { try { VFXCore.ReabrirLoteVanilla(); } catch { } }

            return false; // el halo SE dibuja solo (las primitivas fueron el dibujo)
        }
    }
}
