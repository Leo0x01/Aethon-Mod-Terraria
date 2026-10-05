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
    /// FAUCESTENTACULOPROJECTILE — v6.50.62 — ARMA 1: EL TENTÁCULO.
    ///
    /// La letra del usuario: «este tentáculo o fauces nacen del jugador
    /// y lanzan un latigazo en dirección al jefe, NO IMPORTA LO LEJOS QUE
    /// ESTÉ, el latigazo llegará al jefe y se lo comerá… el tentáculo se
    /// puede estirar y SIEMPRE llega al jefe».
    ///
    /// · FASE 0 — EMERGER (24 t): el tentáculo se alza de la sombra del
    ///   portador (frames 0-5 del gif del usuario), apuntando a su presa.
    /// · FASE 1 — CAZA: la CABEZA-BOCA viaja al jefe a velocidad que
    ///   ESCALA CON LA DISTANCIA (22-56 px/t) — no hay rango: el jefe
    ///   está en este mundo, el látigo llega. El CUERPO es la columna
    ///   Bézier viva (SombrasLib) del jugador a la cabeza, con ojos que
    ///   se abren de golpe y MIRAN al jefe.
    /// · FASE 2 — MORDISCO: enganchada la boca al jefe, DRENA su vida
    ///   (2.5% cada 6 t — la barra baja A MORDIDAS). Al tocar 1 HP:
    ///   FaucesGlobalNPC.Iniciar(estilo 1) → EL FESTÍN (la muerte
    ///   devoradora del motor: el jefe queda POSADO mientras lo comen).
    /// · FASE 3 — DISIPACIÓN (42 t): frames 12-16, la masa se deshace.
    ///
    /// EL SPRITE: el spritesheet 6×3 del usuario (FaucesTentaculo.png,
    /// 17 frames de 462×482, cortados con rectángulo a mano).
    /// </summary>
    public class FaucesTentaculoProjectile : ModProjectile
    {
        // fases (ai[1])
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MORDISCO = 2;
        private const byte FASE_DISIPAR = 3;

        /// <summary>ai[0] = whoAmI de la presa + 1 (0 = sin presa).</summary>
        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetStaticDefaults()
        {
            // sin Main.projFrames: el corte 6×3 se hace a mano en el dibujo
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;          // no muere por edad
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            Projectile.ai[2]++;                              // el tick local
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            // === LA PRESA VIVA ===
            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPAR;
                t = 0;
            }

            switch (fase)
            {
                case FASE_EMERGER:
                {
                    Vector2 rumbo = presa != null
                        ? (presa.Center - dueño.MountedCenter).SafeNormalize(Vector2.UnitX)
                        : Vector2.Normalize(Projectile.velocity);
                    if (rumbo.LengthSquared() < 0.01f) rumbo = Vector2.UnitX;
                    float avance = MathHelper.Clamp(t / 24f, 0f, 1f);
                    Projectile.Center = dueño.MountedCenter + rumbo * (40f + 110f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 4) Sonar(SoundID.Item122.WithPitchOffset(-0.35f).WithVolumeScale(0.8f), Projectile.Center);
                    if (t >= 24) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = presa.Center - Projectile.Center;
                        float dist = hacia.Length();
                        hacia = hacia.SafeNormalize(Vector2.UnitX);
                        // VELOCIDAD QUE ESCALA CON LA DISTANCIA — siempre llega
                        float vel = MathHelper.Clamp(dist * 0.055f, 22f, 56f);
                        // el rumbo GIRA suave hacia la presa (el steering de la casa)
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        float nuevo = actual + MathHelper.Clamp(giro, -0.28f, 0.28f);
                        Projectile.velocity = nuevo.ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // el jefe está AQUÍ: la boca se abalanza
                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.25f).WithVolumeScale(0.9f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        // enganchada a la presa
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - dueño.MountedCenter).ToRotation();

                        // === EL DREN: la vida baja A MORDIDAS (server/SP) ===
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 20 && t % 6 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.025f, 25f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: LA MUERTE SE DETIENE → EL FESTÍN ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 1);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 30 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.15f).WithVolumeScale(0.55f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1)
                        Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.35f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 42) Projectile.Kill();
                    break;
                }
            }

            // la base respira (bruma ocasional)
            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(26, 26), 52, 52,
                    DustID.Shadowflame, 0f, -0.4f, 128, default, 0.6f);
                Main.dust[d].noGravity = true;
            }
        }

        /// <summary>El daño de contacto cubre TODA la columna (Colliding sobre la curva).</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2[] col = SombrasLib.Columna(dueño.MountedCenter, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 13 + 7, 10, 0.2f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 8, ref punto))
                    return true;
            }
            return false;
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
            int semilla = Projectile.whoAmI * 13 + 7;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            // === EL CHARCO de donde nace (a los pies del portador) ===
            SombrasLib.Charco(dueño.MountedCenter + new Vector2(0, dueño.height * 0.4f),
                40f, 0.75f, tiempo, semilla);

            // === LA COLUMNA (del jugador a la cabeza) + LA MASA ===
            Vector2[] col = SombrasLib.Columna(dueño.MountedCenter, Projectile.Center, tiempo, semilla, 18, 0.22f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 42f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 46f, 16f, 0.95f * disipa, semilla, tiempo);

            // === LOS OJOS de la masa (miran TODOS a la presa) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 24f) : 1f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 5);

            // === LA CABEZA-BOCA: el frame 6×3 del usuario ===
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frame = FrameDeFase(fase, t);
            Rectangle src = new Rectangle((frame % 6) * (tex.Width / 6),
                (frame / 6) * (tex.Height / 3), tex.Width / 6, tex.Height / 3);

            float escala = 0.62f;
            if (fase == FASE_MORDISCO) escala *= 1f + 0.07f * MathF.Sin(tiempo * 9f);   // la boca APRIETA
            if (fase == FASE_DISIPAR) escala *= 0.8f + 0.2f * disipa;

            // EL ORIGEN: la base del tentáculo (abajo del frame — donde
            // aterriza la columna); el arte golpea hacia +X
            Vector2 origen = new Vector2(src.Width * 0.42f, src.Height * 0.94f);
            float rot = Projectile.rotation;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Color tint = new Color(luz.R, luz.G, luz.B, 255) * disipa;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src,
                tint, rot, origen, escala, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // === EL VELO ROJO de la boca (la luz que mata la sombra) ===
            if (fase == FASE_CAZA || fase == FASE_MORDISCO)
            {
                VFXCore.Begin();
                float a = (fase == FASE_CAZA ? 0.42f : 0.62f) * disipa;
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Rojo, a),
                    new Vector2(160f, 160f), rot);
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>El frame del gif según la fase (0-5 emerger · 8-9 caza · 10-12 muerde · 12-16 disipa).</summary>
        private static int FrameDeFase(byte fase, float t)
        {
            switch (fase)
            {
                case FASE_EMERGER: return (int)MathHelper.Clamp(t / 24f * 5f, 0f, 5f);
                case FASE_CAZA: return 8 + ((int)(t * 0.12f) % 2);            // 8↔9, boca abierta
                case FASE_MORDISCO:
                {
                    float ciclo = (t % 24f) / 24f;                             // el ciclo del mordisco
                    if (ciclo < 0.4f) return 10;
                    if (ciclo < 0.8f) return 11;
                    return 12;
                }
                default: return 12 + (int)MathHelper.Clamp(t / 42f * 4f, 0f, 4f);   // disipar
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
