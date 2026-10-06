using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// SELLOPAGINAPROJECTILE — v6.50.68 — ARMA NUEVA 3: EL SELLO DE LA PÁGINA.
    ///
    /// La letra del usuario: «cambia los otros bastones por conceptos
    /// diferentes». EL SELLO es lo que APRISIONA la página: el círculo
    /// rúnico con el que el grimorio firma sus contratos. Concepto
    /// DISTINTO al tentáculo: no persigue ni perfora — MARCA EL TERRENO
    /// y lo reclama.
    ///
    /// EL RITUAL (tres fases):
    ///   TRAZADO  (36t) — el círculo rúnico se DIBUJA SOLO donde apunta
    ///   el cursor (anillos dobles contrarrotantes + marcas radiales +
    ///   el piso de sombra). Sin daño: la anticipación ES el arma.
    ///   ERUPCIÓN (28t) — SEIS GARRAS brotan del perímetro hacia el
    ///   centro (brote escalonado, la jaula que se cierra), la bruma
    ///   estalla y TODO lo que está dentro recibe el golpe fuerte.
    ///   POSO    (116t) — la bruma negra queda mordiendo el terreno
    ///   (daño de contacto lento) mientras el círculo se desvanece.
    ///
    /// Si un golpe del sello mata a un jefe: LA DEVORACIÓN, con su
    /// firma — el festín estilo 10, EL SELLO DEL JUICIO (el círculo que
    /// se ciñe sobre el reo). 100% código, cero sprites.
    /// </summary>
    public class SelloPaginaProjectile : ModProjectile
    {
        private const byte FASE_TRAZADO = 0;
        private const byte FASE_ERUPCION = 1;
        private const byte FASE_POSO = 2;

        private const float RADIO = 128f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 34;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            Projectile.ai[1]++;
            float t = Projectile.ai[1];
            byte fase = (byte)Projectile.ai[0];
            Projectile.velocity = Vector2.Zero;     // el sello NO se mueve: se TRAZA

            switch (fase)
            {
                case FASE_TRAZADO:
                {
                    if (t == 1f)
                        Sonar(SoundID.Item123.WithPitchOffset(-0.22f).WithVolumeScale(0.55f), Projectile.Center);
                    if (t >= 36f)
                    {
                        Projectile.ai[0] = FASE_ERUPCION;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                        Sonar(SoundID.Item122.WithPitchOffset(-0.5f).WithVolumeScale(0.95f), Projectile.Center);
                        Sonar(SoundID.NPCHit9.WithPitchOffset(-0.3f).WithVolumeScale(0.6f), Projectile.Center);
                        if (Main.netMode != NetmodeID.Server)
                            for (int i = 0; i < 10; i++)
                            {
                                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 6f),
                                    128, default, 0.9f);
                                d.noGravity = true;
                            }
                    }
                    break;
                }
                case FASE_ERUPCION:
                {
                    if (t >= 28f)
                    {
                        Projectile.ai[0] = FASE_POSO;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                    }
                    break;
                }
                case FASE_POSO:
                {
                    // la bruma que queda mordiendo: motas perezosas
                    if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(9))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(Projectile.Center +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * RADIO * 0.5f,
                            DustID.Shadowflame, new Vector2(0f, -0.6f), 128, default, 0.7f);
                        d.noGravity = true;
                    }
                    if (t >= 116f) Projectile.Kill();
                    break;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA DEVORACIÓN: si ESTE golpe mata, la muerte es NUESTRA —
            // el festín estilo 10 (el sello del juicio)
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 10);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // el círculo del sello: el centro del blanco DENTRO (con
            // margen — los jefes grandes también caen en la firma)
            if (Projectile.ai[0] == FASE_TRAZADO) return false;
            float margen = RADIO + 14f + MathF.Min(targetHitbox.Width, targetHitbox.Height) * 0.4f;
            Vector2 pc = targetHitbox.Center();
            return Vector2.Distance(Projectile.Center, pc) < margen;
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
            int semilla = Projectile.whoAmI * 23 + 7;
            byte fase = (byte)Projectile.ai[0];
            float t = Projectile.ai[1];
            Vector2 c = Projectile.Center;

            // LA VIDA DEL CÍRCULO: crece al trazarse, entero en la
            // erupción, se desvanece en el poso
            float vivo = fase switch
            {
                FASE_TRAZADO => SombrasLib.DeGolpe(t / 36f),
                FASE_ERUPCION => 1f,
                _ => MathHelper.Clamp(1f - t / 116f, 0f, 1f),
            };

            // === EL PISO DEL SELLO — el disco de sombra (donde la firma
            // moja el terreno) ===
            if (vivo > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Negro, 0.40f * vivo),
                    new Vector2(RADIO * 1.62f, RADIO * 1.62f) * (0.7f + 0.3f * vivo), 0f, VFXCore.GlowOrb);
                VFXCore.FlushAlpha();
            }

            // === LOS ANILLOS RÚNICOS — dobles, contrarrotantes, vivos ===
            if (vivo > 0.02f)
            {
                VFXCore.Begin();
                float late = 0.8f + 0.2f * MathF.Sin(tiempo * 3.6f + SombrasLib.semille(semilla));
                VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Violeta, 0.55f * vivo * late),
                    VFXCore.RingQuadSize(RADIO), tiempo * 0.55f, VFXCore.Ring);
                VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Violeta, 0.36f * vivo * late),
                    VFXCore.RingQuadSize(RADIO * 0.64f), -tiempo * 0.8f, VFXCore.Ring);
                VFXCore.FlushAdditive();
            }

            // === LAS MARCAS — ocho rayitas radiales parpadeando + los
            // puntos de la contrafase (la escritura del borde) ===
            if (vivo > 0.02f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 8; k++)
                {
                    float ang = k * MathHelper.PiOver4 + tiempo * 0.4f;
                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                    float parp = 0.5f + 0.5f * MathF.Sin(tiempo * 3.2f + k * 2.1f + SombrasLib.semille(semilla + k));
                    VFXCore.Line(c + dir * RADIO * 0.85f, c + dir * RADIO * 1.02f,
                        SombrasLib.Alfa(SombrasLib.Blanco, 0.55f * vivo * parp), 4f);
                    float angP = k * MathHelper.PiOver4 - tiempo * 0.55f + MathHelper.PiOver4 * 0.5f;
                    Vector2 dirP = new(MathF.Cos(angP), MathF.Sin(angP));
                    VFXCore.Quad(c + dirP * RADIO * 0.74f,
                        SombrasLib.Alfa(SombrasLib.Violeta, 0.50f * vivo * parp), new Vector2(7f, 7f));
                }
                VFXCore.FlushAdditive(VFXCore.Pixel);
            }

            // === EL CENTRO — la firma que late (la tinta aún fresca) ===
            if (vivo > 0.02f)
            {
                VFXCore.Begin();
                float pulso = 0.65f + 0.35f * MathF.Sin(tiempo * 4.4f);
                VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Violeta, 0.35f * vivo * pulso),
                    new Vector2(46f, 46f) * pulso);
                VFXCore.FlushAdditive();
            }

            // === LA ERUPCIÓN — LAS GARRAS DEL SELLO: seis zarpos brotan
            // del perímetro hacia el centro (brote escalonado) + la
            // bruma estalla + la herida roja del centro ===
            if (fase == FASE_ERUPCION)
            {
                for (int k = 0; k < 6; k++)
                {
                    float brote = MathHelper.Clamp((t - k * 4f) / 15f, 0f, 1f);
                    if (brote <= 0.02f) continue;
                    brote = SombrasLib.DeGolpe(brote);
                    float ang = k / 6f * MathHelper.TwoPi + SombrasLib.semille(semilla + k * 3) * 0.5f;
                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang) * 0.88f);
                    Vector2 basePos = c + dir * RADIO * 0.96f;
                    // la garra apunta al CENTRO y un pelo hacia arriba (brotando)
                    Vector2 hacia = new(-dir.X, -dir.Y - 0.30f);
                    SombrasLib.Garra(basePos, hacia, RADIO * 0.60f * brote,
                        0.95f * brote, 0.42f * (k % 2 == 0 ? 1f : -1f));
                }

                // LA BRUMA DEL ESTALLIDO — la exhalación del sello
                float estalla = MathHelper.Clamp(1f - t / 28f, 0f, 1f);
                SombrasLib.Bruma(c, RADIO * (0.7f + 0.25f * estalla), 0.55f + 0.35f * estalla,
                    tiempo, semilla + 13, new Vector2(0f, -14f * estalla));

                // LA HERIDA — el centro arde rojo al brotar
                if (t < 14f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Rojo, 0.55f * (1f - t / 14f)),
                        new Vector2(70f, 70f));
                    VFXCore.FlushAdditive();
                    if (t < 6f)
                        SombrasLib.OndaChoque(c, RADIO * (0.5f + 0.5f * t / 6f), 0.6f * (1f - t / 6f));
                }
            }

            // === EL POSO — la bruma que queda MORDIENDO el terreno ===
            if (fase == FASE_POSO)
            {
                float apaga = MathHelper.Clamp(1f - t / 116f, 0f, 1f);
                SombrasLib.Bruma(c + new Vector2(0f, RADIO * 0.18f), RADIO * 0.72f,
                    0.42f * apaga, tiempo, semilla + 29, new Vector2(0f, -8f));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            SombrasLib.Bruma(Projectile.Center, 54f, 0.55f, Main.GlobalTimeWrappedHourly,
                Projectile.whoAmI * 23 + 7);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
