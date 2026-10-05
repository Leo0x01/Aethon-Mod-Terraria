using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// TAJOSOMBRAPROJECTILE — v6.50.62 — ARMA 2: EL TAJO + LA ESFERA.
    ///
    /// La letra del usuario: «un tajo con las formas del poder de sombras
    /// que se lanza al jefe, lo encierra en una ESFERA de sombras con
    /// OJOS y BOCA CON DIENTES y lo devoran; luego se desintegra en
    /// bruma y polvo».
    ///
    /// · FASE 0 — VUELO (0-…): EL TAJO (tajo.png del usuario) vuela al
    ///   jefe — la media luna roja con la masa de ojos y bocas detrás.
    ///   Homing que escala con distancia: SIEMPRE llega.
    /// · FASE 1 — ENVOLVER (30 t): el tajo EXPLOTA en el jefe y la
    ///   esfera se cierra alrededor: disco negro (GlowOrb) del tamaño
    ///   del jefe, ojos que se abren DE GOLPE por toda la superficie
    ///   (todos MIRAN AL JUGADOR — la esfera te mira mientras come) y
    ///   la boca ecuatorial con colmillos que se abre.
    /// · FASE 2 — DEVORAR: tres MORDIDAS (t=40/80/120), cada una drena
    ///   12% de la vida actual. Al tocar 1 HP → Iniciar(estilo 2) →
    ///   EL FESTÍN (la esfera lo desintegra en bruma y polvo).
    /// · FASE 3 — COLAPSO: la esfera se deshace en bruma.
    /// </summary>
    public class TajoSombraProjectile : ModProjectile
    {
        private const byte FASE_VUELO = 0;
        private const byte FASE_ENVOLVER = 1;
        private const byte FASE_DEVORAR = 2;
        private const byte FASE_COLAPSO = 3;

        /// <summary>ai[0] = whoAmI de la presa + 1.</summary>
        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_COLAPSO && !presaValida)
            {
                Projectile.ai[1] = FASE_COLAPSO;
                Projectile.ai[2] = 0;
                fase = FASE_COLAPSO;
                t = 0;
            }

            switch (fase)
            {
                case FASE_VUELO:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.06f, 20f, 54f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.3f, 0.3f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // llegó: el tajo EXPLOTA dentro del jefe
                        if (dist < MathF.Max(presa.width, presa.height) * 0.75f + 30f)
                        {
                            Projectile.ai[1] = FASE_ENVOLVER;
                            Projectile.ai[2] = 0;
                            Projectile.Center = presa.Center;
                            Sonar(SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.85f), Projectile.Center);
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.4f).WithVolumeScale(0.8f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_COLAPSO; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_ENVOLVER:
                {
                    if (presa != null && presa.active) Projectile.Center = presa.Center;
                    if (t == 2) Sonar(SoundID.Item74.WithPitchOffset(-0.5f).WithVolumeScale(0.7f), Projectile.Center);
                    if (t >= 30) { Projectile.ai[1] = FASE_DEVORAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DEVORAR:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;

                        // === LAS TRES MORDIDAS (server/SP) ===
                        if (Main.netMode != NetmodeID.MultiplayerClient &&
                            (t == 40 || t == 80 || t == 120))
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.3f).WithVolumeScale(0.75f), Projectile.Center);
                                float quitar = MathF.Max(dueñoPool.life * 0.12f, 60f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: LA MUERTE SE DETIENE → EL FESTÍN (estilo esfera) ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 2);
                                    Projectile.ai[1] = FASE_COLAPSO;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                            }
                        }
                        // sin jefe (chusma): la mordida de contacto del proyectil la mata
                        if (t > 150) { Projectile.ai[1] = FASE_COLAPSO; Projectile.ai[2] = 0; }
                    }
                    else { Projectile.ai[1] = FASE_COLAPSO; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_COLAPSO:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.3f), Projectile.Center);
                    if (t >= 36) Projectile.Kill();
                    break;
                }
            }

            // ojos del tajo en vuelo (partículas)
            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] == FASE_VUELO && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(30, 30), 60, 60,
                    DustID.Shadowflame, -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f, 128, default, 0.55f);
                Main.dust[d].noGravity = true;
            }
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
            int semilla = Projectile.whoAmI * 29 + 3;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            NPC presa = Presa;

            if (fase == FASE_VUELO)
            {
                DrawTajoVuelo(tiempo);
                return;
            }

            // ============ LA ESFERA ============
            float radio = 90f;
            if (presa != null && presa.active)
                radio = MathHelper.Clamp(presa.Size.Length() * 0.62f, 80f, 260f);

            // LA APERTURA: 0→1 de golpe (envolver) / respira (devorar) / colapsa
            float apertura = fase switch
            {
                FASE_ENVOLVER => SombrasLib.DeGolpe(t / 26f),
                FASE_DEVORAR => 1f,
                _ => MathHelper.Clamp(1f - t / 30f, 0f, 1f),
            };

            // === EL DISCO NEGRO (el cuerpo de la esfera) ===
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.96f * apertura),
                new Vector2(radio * 2.05f, radio * 2.05f), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            // === EL VELO OSCURO que crece DENTRO (el jefe se apaga) ===
            if (fase == FASE_DEVORAR)
            {
                float comido = MathHelper.Clamp(t / 130f, 0f, 1f);
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.85f * comido),
                    new Vector2(radio * 1.5f * (0.4f + 0.6f * comido), radio * 1.5f * (0.4f + 0.6f * comido)));
                VFXCore.FlushAlpha();
            }

            // === LOS OJOS de la superficie — MIRAN AL JUGADOR ===
            Player mirada = Main.player[Projectile.owner];
            VFXCore.Begin();
            int nOjos = 11;
            for (int k = 0; k < nOjos; k++)
            {
                float f = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.37f + k * 0.618034f);
                float ang = f * MathHelper.TwoPi;
                float rr = radio * (0.25f + 0.62f * SombrasLib.Frac(f * 7.7f));
                Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.85f) * rr;
                // apertura escalonada + latido individual
                float local = apertura * nOjos * 0.8f - k * 0.8f;
                float abierto = MathHelper.Clamp(local, 0f, 1f);
                abierto *= 0.82f + 0.18f * MathF.Sin(tiempo * 3.7f + k * 1.9f + f * 9f);
                if (abierto <= 0.05f) continue;
                float tamaño = 13f + 12f * SombrasLib.Frac(f * 5.3f);
                SombrasLib.Ojo(pos, tamaño, mirada.Center - pos, abierto);
            }
            VFXCore.FlushAdditive();

            // === LA BOCA ECUATORIAL con colmillos: tres mordidas ===
            float boca = fase switch
            {
                FASE_ENVOLVER => 0.9f * SombrasLib.DeGolpe(t / 22f),
                FASE_DEVORAR => CicloMordida(t),
                _ => 0.5f * (1f - t / 30f),
            };
            Vector2 rumbo = new(MathF.Cos(tiempo * 0.5f), MathF.Sin(tiempo * 0.35f));
            rumbo = rumbo.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(rumbo);
            SombrasLib.Fauces(Projectile.Center + rumbo * radio * 0.15f, rumbo, boca,
                radio * 0.85f, semilla);

            // === ALMAS: la vida vuela al portador durante el devorar ===
            if (fase == FASE_DEVORAR && presa != null && presa.active)
            {
                for (int k = 0; k < 5; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.014f + k * 0.2f);
                    Vector2 a = Projectile.Center + new Vector2(MathF.Cos(k * 2.4f + tiempo), MathF.Sin(k * 2.1f)) * radio * 0.5f;
                    Vector2 b = Main.player[Projectile.owner].MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(0, -140f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Alma(pos, 10f + 6f * MathF.Sin(tiempo * 5f + k), 0.8f * (1f - prog * 0.4f));
                }
            }
        }

        /// <summary>El ciclo de la mordida: abre (0.35) · cierra DE GOLPE (0.25) · mastica (0.4).</summary>
        private static float CicloMordida(float t)
        {
            float m = t % 40f;      // mordida cada 40 t (las de verdad: t=40/80/120)
            if (m < 8f) return MathHelper.Lerp(0.25f, 1f, m / 8f);          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.05f, SombrasLib.DeGolpe((m - 8f) / 8f)); // ¡CIERRA!
            return 0.3f + 0.15f * MathF.Sin(m * 0.8f);                       // mastica
        }

        /// <summary>EL TAJO en vuelo: la media luna del usuario + su velo.</summary>
        private void DrawTajoVuelo(float tiempo)
        {
            // el velo rojo del filo (la energía del corte)
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Rojo, 0.30f),
                new Vector2(220f, 220f), Projectile.rotation);
            VFXCore.FlushAdditive();

            // el sprite del usuario (la escala Vive del ancho real de la
            // textura: el tajo vuela a ~640 px de filo en pantalla)
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            float escala = (640f / tex.Width) * (1f + 0.06f * MathF.Sin(tiempo * 7f));
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null,
                luz, Projectile.rotation, tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // ojos sueltos que asoman del rastro del tajo
            VFXCore.Begin();
            int semilla = Projectile.whoAmI * 29 + 3;
            for (int k = 0; k < 4; k++)
            {
                float f = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.51f + k * 0.31f);
                Vector2 pos = Projectile.Center - Projectile.velocity * (0.8f + f * 2.4f) * (0.5f + f);
                float abierto = 0.5f + 0.5f * MathF.Sin(tiempo * 4f + k * 2.2f);
                SombrasLib.Ojo(pos, 14f + 8f * f, Projectile.velocity, abierto);
            }
            VFXCore.FlushAdditive();
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
