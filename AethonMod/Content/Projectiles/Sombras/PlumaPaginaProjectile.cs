using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// PLUMAPAGINAPROJECTILE — v6.50.68 — ARMA NUEVA 1: LA PLUMA DE LA PÁGINA.
    ///
    /// La letra del usuario: «cambia los otros bastones por conceptos
    /// diferentes» (La Sombra de la Página queda intacta — esta NO es
    /// una copia del tentáculo: es un concepto DISTINTO). LA PLUMA es
    /// la pluma del escriba del grimorio: la que ESCRIBE la página —
    /// cada disparo es un TRAZO de tinta viva.
    ///
    /// EL CONCEPTO: una AGUJA DE HUESO (la textura Colmillo, la firma
    /// de la casa) volando RECTA Y PRECISA con homing SUAVE (la tinta
    /// busca la palabra), dejando un RASTRO DE TINTA — la cinta de
    /// carne ondulando detrás (el trazo húmedo del pincel). Atraviesa
    /// hasta 4 enemigos; cada impacto SALPICA (anillo violeta + bruma +
    /// gotas de tinta). Si el golpe mata a un jefe: LA DEVORACIÓN, con
    /// su firma propia — el festín estilo 8, LA LLUVIA DE TINTA.
    ///
    /// 100% código: ni un sprite del arma (el icono del item sí, como
    /// toda la familia). Cero dependencias.
    /// </summary>
    public class PlumaPaginaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];

            // la aguja apunta al vuelo (la textura Colmillo apunta ARRIBA)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // === EL HOMING SUAVE (cada 8 ticks: la tinta busca la
            // palabra más cercana — giro acotado, NUNCA un misil) ===
            if (t % 8f == 0f)
            {
                NPC presa = BuscarCerca(Projectile.Center, 540f);
                if (presa != null)
                {
                    Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    float giro = MathHelper.WrapAngle(hacia.ToRotation() - Projectile.velocity.ToRotation());
                    giro = MathHelper.Clamp(giro, -0.55f, 0.55f);
                    Projectile.velocity = Projectile.velocity.RotatedBy(giro * 0.55f);
                }
            }

            // motas de tinta al vuelo
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(6))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    -Projectile.velocity * 0.04f, 128, default, 0.5f);
                d.noGravity = true;
            }

            // el splat de impacto decae
            if (Projectile.localAI[0] > 0f) Projectile.localAI[0]--;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA DEVORACIÓN: si ESTE golpe mata, la muerte es NUESTRA —
            // el festín estilo 8 (la lluvia de tinta)
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 8);

            // EL SPLAT: dónde cayó la tinta (lo dibuja PreDraw)
            Projectile.localAI[0] = 9f;
            Projectile.localAI[1] = target.Center.X;
            Projectile.localAI[2] = target.Center.Y;

            try { Terraria.Audio.SoundEngine.PlaySound(
                SoundID.NPCHit9.WithPitchOffset(0.25f).WithVolumeScale(0.35f), Projectile.Center); } catch { }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // la aguja es un SEGMENTO (la punta a la cola): colisión de
            // línea — acertar con el RASTRO también cuenta
            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, Projectile.Center - rumbo * 34f, 7f, ref punto);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 3;
            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            Vector2 perp = new(-rumbo.Y, rumbo.X);
            float vel = Projectile.velocity.Length();

            // === EL RASTRO DE TINTA — la cinta que ESCRIBE el vuelo:
            // ondulación suave detrás de la aguja (el trazo húmedo) ===
            var carne = VFXCore.Carne;
            float largo = MathHelper.Clamp(vel * 3.2f, 44f, 130f);
            var espina = new Vector2[6];
            var anchos = new float[6];
            var bordes = new Color[6];
            for (int i = 0; i < 6; i++)
            {
                float f = i / 5f;
                float onda = MathF.Sin(tiempo * 6.5f - f * 7f + SombrasLib.semille(semilla)) * 5.5f * f;
                espina[i] = Projectile.Center - rumbo * (largo * f) + perp * onda;
                anchos[i] = MathHelper.Lerp(11f, 1.5f, f);
                bordes[i] = SombrasLib.Alfa(SombrasLib.Violeta, (0.30f - 0.24f * f));
            }
            if (carne != null)
            {
                VFXCore.Begin();
                VFXCore.Ribbon(espina, anchos, SombrasLib.Alfa(SombrasLib.HumoNegro, 0.62f), carne,
                    uvFlow: tiempo * 0.22f);
                VFXCore.FlushAlpha();
                VFXCore.Begin();
                VFXCore.RibbonTinted(espina, anchos, bordes, carne,
                    uvFlow: tiempo * 0.22f, edgeInset: 0.36f);
                VFXCore.FlushAdditive();
            }

            // === LA AGUJA DE HUESO (la pluma sin cañón: solo la punta
            // que escribe — blanca, afilada, brillando) ===
            if (VFXCore.Colmillo != null)
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Blanco, 0.95f),
                    new Vector2(11f, 46f), Projectile.rotation, VFXCore.Colmillo);
                // el halo de tinta alrededor de la punta
                VFXCore.Quad(Projectile.Center + rumbo * 8f, SombrasLib.Alfa(SombrasLib.Violeta, 0.30f),
                    new Vector2(30f, 52f), Projectile.rotation);
                VFXCore.FlushAdditive();
            }

            // === LA PLUMA RESPIRA — un puff cíclico pequeño en la cola ===
            {
                float edad = SombrasLib.Frac(tiempo * 0.55f + SombrasLib.semille(semilla) * 3f);
                Vector2 cola = Projectile.Center - rumbo * 26f;
                Vector2 o = Main.screenPosition;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    BrumaFX.Puff(cola - o, 12f + 16f * edad, SombrasLib.HumoNegro,
                        semilla * 7 + 1, tiempo,
                        MathHelper.Clamp(0.42f * MathF.Sin(edad * MathF.PI), 0.04f, 0.5f),
                        quality: 0.4f);
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }
            }

            // === EL SPLAT DEL IMPACTO (donde cayó la tinta hace poco) ===
            if (Projectile.localAI[0] > 0f)
            {
                float f = 1f - Projectile.localAI[0] / 9f;
                Vector2 pos = new(Projectile.localAI[1], Projectile.localAI[2]);
                SombrasLib.OndaChoque(pos, 20f + 46f * f, 0.55f * (1f - f), roja: false);
                SombrasLib.Bruma(pos, 30f + 18f * f, 0.5f * (1f - f * 0.5f), tiempo, semilla + 9);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // la mancha final: la gota que cae cuando el trazo se corta
            if (Main.netMode == NetmodeID.Server) return;
            SombrasLib.Bruma(Projectile.Center, 42f, 0.6f, Main.GlobalTimeWrappedHourly,
                Projectile.whoAmI * 17 + 3);
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), Main.rand.NextFloat(-3f, 1f)), 128, default, 0.8f);
                d.noGravity = true;
            }
        }

        /// <summary>La palabra más cercana (la tinta busca dónde caer).</summary>
        private static NPC BuscarCerca(Vector2 desde, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }
    }
}
