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
    /// v6.50.64 — LA PINZA DOBLE: el gif del usuario va DOS veces en la
    /// punta — una cabeza en cada esquina, desplegadas en V, y MUERDEN
    /// como DIENTES (la petición: «has que el gif esté doble en la punta,
    /// uno en cada esquina, así se comporta como dientes»). Y LA BRUMA
    /// NEGRA en todo el cuerpo y en las dos bocas (la misma petición).
    ///
    /// La letra del usuario: «este tentáculo o fauces nacen del jugador
    /// y lanzan un latigazo en dirección al jefe, NO IMPORTA LO LEJOS QUE
    /// ESTÉ, el latigazo llegará al jefe y se lo comerá… el tentáculo se
    /// puede estirar y SIEMPRE llega al jefe».
    ///
    /// · FASE 0 — EMERGER (24 t): la pinza brota PLEGADA de la sombra del
    ///   portador (frames 0-5 del gif, ×2), apuntando a su presa.
    /// · FASE 1 — CAZA: las DOS CABEZAS-BOCA viajan al jefe abiertas en
    ///   pinza, a velocidad que ESCALA CON LA DISTANCIA (22-56 px/t) — no
    ///   hay rango: el jefe está en este mundo, el látigo llega. El CUERPO
    ///   es la columna Bézier viva (SombrasLib) del jugador a la
    ///   horquilla, con ojos que se abren de golpe y MIRAN al jefe, y
    ///   TODO el cuerpo EXHALA BRUMA NEGRA (BrumaColumna).
    /// · FASE 2 — MORDISCO: las dos cabezas CIERRAN la pinza sobre el
    ///   jefe desde esquinas opuestas y DRENAN su vida (2.5% cada 6 t —
    ///   la barra baja A MORDIDAS). Al tocar 1 HP:
    ///   FaucesGlobalNPC.Iniciar(estilo 1) → EL FESTÍN (la muerte
    ///   devoradora del motor: el jefe queda POSADO mientras lo comen).
    /// · FASE 3 — DISIPACIÓN (42 t): frames 12-16, la pinza se pliega y
    ///   la masa se deshace en bruma.
    ///
    /// EL SPRITE: el spritesheet 6×3 del usuario (FaucesTentaculo.png,
    /// 17 frames de 462×482, cortados con rectángulo a mano) — dibujado
    /// DOS veces, una por esquina de la pinza.
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

            // === LA COLUMNA (del jugador a la HORQUILLA de la pinza) + LA MASA ===
            Vector2[] col = SombrasLib.Columna(dueño.MountedCenter, Projectile.Center, tiempo, semilla, 18, 0.22f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 42f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 46f, 16f, 0.95f * disipa, semilla, tiempo);

            // === LA BRUMA NEGRA DEL CUERPO (v6.50.64 — «a todo el tentáculo
            // ponle bruma negra»): la masa EXHALA puﬀs vivos todo el rato ===
            SombrasLib.BrumaColumna(col, 44f, 0.45f * disipa, tiempo, semilla, 10);

            // === LOS OJOS de la masa (miran TODOS a la presa) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 24f) : 1f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 5);

            // ==============================================================
            //  LA PINZA DOBLE (v6.50.64): el gif del usuario, DOS veces en
            //  la punta — una cabeza en cada esquina, mordiendo como
            //  DIENTES. La columna termina en la HORQUILLA; de ahí salen
            //  las dos cabezas abiertas en V (caza) o cerradas sobre el
            //  jefe desde esquinas opuestas (mordisco).
            // ==============================================================
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frame = FrameDeFase(fase, t);
            Rectangle src = new Rectangle((frame % 6) * (tex.Width / 6),
                (frame / 6) * (tex.Height / 3), tex.Width / 6, tex.Height / 3);

            float escala = 0.56f;
            if (fase == FASE_MORDISCO) escala *= 1f + 0.07f * MathF.Sin(tiempo * 9f);   // la pinza APRIETA
            if (fase == FASE_DISIPAR) escala *= 0.8f + 0.2f * disipa;

            // EL ORIGEN: la base del tentáculo (abajo del frame — donde
            // aterriza la columna); el arte golpea hacia +X
            Vector2 origen = new Vector2(src.Width * 0.42f, src.Height * 0.94f);
            float rot = Projectile.rotation;
            Vector2 eje = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
            Vector2 perp = new Vector2(-eje.Y, eje.X);

            // LA APERTURA de la pinza según la fase (cómo se despliegan los dientes)
            float abre = fase switch
            {
                FASE_EMERGER => 0.12f + 0.16f * (t / 24f),                                // brota plegada
                FASE_CAZA => 0.34f + 0.08f * MathF.Sin(tiempo * 6f),                     // abierta, viva
                FASE_MORDISCO => 0.09f + 0.06f * (0.5f + 0.5f * MathF.Sin(tiempo * 9f)), // MASTICA apretando
                _ => 0.05f * disipa,
            };

            // cada cabeza muerde desde SU esquina: las bases en la
            // horquilla, separadas a lo ancho del jefe cuando está enganchada
            float agarre = fase == FASE_MORDISCO && presa != null
                ? MathHelper.Clamp(presa.width * 0.16f, 8f, 24f) : 0f;
            Vector2 baseA = Projectile.Center + perp * agarre;
            Vector2 baseB = Projectile.Center - perp * agarre;

            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Color tint = new Color(luz.R, luz.G, luz.B, 255) * disipa;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
            Main.EntitySpriteDraw(tex, baseA - Main.screenPosition, src,
                tint, rot + abre, origen, escala, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(tex, baseB - Main.screenPosition, src,
                tint, rot - abre, origen, escala, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // === LA SOLDADURA de la horquilla (la carne donde la columna
            // se parte en dos cabezas — tapa la costura) ===
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.95f * disipa),
                new Vector2(64f, 64f), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            // === EL ALIENTO de las dos bocas (bruma negra de las fauces) ===
            float aliento = fase switch
            {
                FASE_EMERGER => 0.5f,
                FASE_CAZA => 0.95f,
                FASE_MORDISCO => 0.6f,
                _ => 0.4f,
            } * disipa;
            Vector2 dirA = new Vector2(MathF.Cos(rot + abre), MathF.Sin(rot + abre));
            Vector2 dirB = new Vector2(MathF.Cos(rot - abre), MathF.Sin(rot - abre));
            SombrasLib.BrumaBoca(baseA + dirA * (src.Width * 0.46f * escala), dirA,
                aliento, 0.5f * disipa, tiempo, semilla + 3, 4);
            SombrasLib.BrumaBoca(baseB + dirB * (src.Width * 0.46f * escala), dirB,
                aliento, 0.5f * disipa, tiempo, semilla + 9, 4);

            // === EL VELO ROJO entre los dos dientes (la luz que mata la sombra) ===
            if (fase == FASE_CAZA || fase == FASE_MORDISCO)
            {
                VFXCore.Begin();
                float a = (fase == FASE_CAZA ? 0.42f : 0.62f) * disipa;
                VFXCore.Quad(Projectile.Center + eje * (src.Width * 0.18f * escala),
                    SombrasLib.Alfa(SombrasLib.Rojo, a), new Vector2(160f, 160f), rot);
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
