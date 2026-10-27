using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// REQUIEMESTELARPROJECTILE — v6.50.66 — ARMA 10: EL RÉQUIEM
    /// ESTELAR.
    ///
    /// La receta del «The Set» de Thorium (la onda dorada que atraviesa
    /// el mundo) + los anillos de Fargo's: LA CAMPANA DE LUZ — tres
    /// ANILLOS que se expanden desde el portador (1400 px de radio, uno
    /// cada 32 t), cada uno ILUMINANDO el mundo a su paso; el tercero es
    /// DOBLE (la campana mayor) con su lluvia de estrellas del cierre.
    ///
    /// El golpe de cada anillo: la CIRCUNFERENCIA (el frente de la onda
    /// — no el disco): segmentos gordos perpendiculares muestreados en
    /// 16 direcciones.
    /// </summary>
    public class RequiemEstelarProjectile : ModProjectile
    {
        private const int NAnillos = 3;
        private const int Cadencia = 32;
        private const float RadioFinal = 1400f;

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
            Projectile.timeLeft = Cadencia * NAnillos + 70;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 26;
            Projectile.netImportant = true;
        }

        /// <summary>El radio de un anillo k en su vida v (0..1).</summary>
        private static float RadioAnillo(float v) => RadioFinal * OndaLib.Expansion(v);

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL ORIGEN: donde estaba el portador al tañer (la campana no se mueve con él)
            if (t < 2f) Projectile.Center = dueño.MountedCenter;

            // EL TAÑIDO de cada anillo
            for (int k = 0; k < NAnillos; k++)
            {
                float momento = 6f + k * Cadencia;
                if (MathF.Abs(t - momento) < 0.5f)
                {
                    bool doble = k == NAnillos - 1;
                    DefinitivasLib.Sonar(SoundID.Item27.WithPitchOffset(-0.3f + k * 0.15f).WithVolumeScale(doble ? 1.1f : 0.8f),
                        Projectile.Center);
                    if (doble)
                    {
                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.9f), Projectile.Center);
                        OndaLib.Kick(11, 16);
                        OndaLib.Flash(new Color(255, 240, 200), 0.30f, 12, Projectile.Center);
                    }
                    else OndaLib.Kick(5, 8);
                }
            }

            // LA LLUVIA DE ESTRELLAS del cierre (el tercer anillo siembra chispas que caen)
            float tCierre = 6f + (NAnillos - 1) * Cadencia;
            if (t > tCierre && t < tCierre + 26 && !Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float radio = Main.rand.NextFloat(200f, RadioFinal * 0.7f);
                Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio - new Vector2(0f, 300f);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0f, Main.rand.NextFloat(4f, 9f)), 0,
                    DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 1.0f);
                d.noGravity = true;
            }

            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 1.6f, 1.4f, 1.0f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL FRENTE DE LA ONDA: solo la CIRCUNFERENCIA actual de cada anillo vivo golpea
            for (int k = 0; k < NAnillos; k++)
            {
                float nace = 6f + k * Cadencia;
                float t = Projectile.ai[2] - nace;
                if (t < 0f || t > 46f) continue;
                float v = MathHelper.Clamp(t / 46f, 0f, 1f);
                float radio = RadioAnillo(v);
                float dist = Vector2.Distance(targetHitbox.Center(), Projectile.Center);
                float margen = 60f + MathF.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
                if (MathF.Abs(dist - radio) < margen) return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;

            for (int k = 0; k < NAnillos; k++)
            {
                float nace = 6f + k * Cadencia;
                float tk = t - nace;
                if (tk < 0f) continue;
                float v = MathHelper.Clamp(tk / 46f, 0f, 1f);
                if (v >= 1f) continue;

                bool doble = k == NAnillos - 1;
                float radio = RadioAnillo(v);
                Color c = doble ? DefinitivasLib.GradienteJuicio(tiempo * 0.06f) : DefinitivasLib.Oro;

                // ==================== EL ANILLO (la tira circular de primitivas) ====================
                const int SEG = 42;
                var pts = new Vector2[SEG];
                for (int i = 0; i < SEG; i++)
                {
                    float ang = i / (float)SEG * MathHelper.TwoPi;
                    float ondula = MathF.Sin(ang * 7f + tiempo * 5f + k * 2f) * 10f;
                    pts[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio + ondula)
                        - Main.screenPosition;
                }
                float ancho = (doble ? 34f : 22f) * (1f - v * 0.4f);
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(pts, ancho, c, DefinitivasLib.BlancoCalido,
                        0.85f * OndaLib.Falloff(v, OndaFalloff.Quadratic),
                        StormTaper.Flat, 100 + k, 0, true, true, true, null, null);
                    if (doble)
                    {
                        // LA CAMPANA MAYOR: el segundo anillo desfasado (el eco del bronce)
                        var pts2 = new Vector2[SEG];
                        for (int i = 0; i < SEG; i++)
                        {
                            float ang = i / (float)SEG * MathHelper.TwoPi;
                            float ondula = MathF.Sin(ang * 5f - tiempo * 4f) * 12f;
                            pts2[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio * 0.82f + ondula)
                                - Main.screenPosition;
                        }
                        RayoStrip.FilamentoEnLote(pts2, ancho * 0.7f, DefinitivasLib.Ambar, DefinitivasLib.Oro,
                            0.7f * OndaLib.Falloff(v, OndaFalloff.Quadratic),
                            StormTaper.Flat, 104, 0, true, true, true, null, null);
                    }
                }
                finally { RayoStrip.CerrarLote(); }

                // EL DESTELLO que cabalga el frente (4 fuegos en la circunferencia)
                VFXCore.Begin();
                for (int j = 0; j < 4; j++)
                {
                    float ang = tiempo * (1.2f + k * 0.3f) + j * MathHelper.PiOver2;
                    Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.5f * (1f - v)), new Vector2(120f, 120f));
                }
                VFXCore.FlushAdditive();
            }

            // LA CAMPANA (el corazón: siempre un latido dorado en el centro)
            VFXCore.Begin();
            float late = 0.6f + 0.4f * MathF.Sin(tiempo * 4f);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.4f * late), new Vector2(130f, 130f));
            if (DefinitivasLib.TexStar != null)
                DefinitivasLib.Blit(Main.spriteBatch, DefinitivasLib.TexStar, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.8f), new Vector2(70f, 70f), tiempo);
            VFXCore.FlushAdditive();
            // v6.54.5 — el contrato del lote: el Flush dejaba el lote CERRADO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
