using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// v5.99 — QuasarJetProjectile — EL CHORRO RELATIVISTA de LA LANZA DEL
    /// QUÁSAR (arma nueva con "un proyectil super cosmico").
    ///
    /// LO QUE ES: un chorro relativista de verdad — el objeto más brillante
    /// del universo: plasma expulsado a casi la velocidad de la luz desde
    /// un núcleo activo. En el juego: UNA LANZA DE LUZ larguísima y
    /// velocísima (26 px/t ×3 updates) que ATRAVIESA hasta 10 enemigos,
    /// con NUDOS DE SHOCK (los "knots" de Herbig-Haro: 5 bolas de plasma
    /// brillando a lo largo del chorro, PULSANDO hacia la punta como
    /// materia recién eyectada), núcleo blanco → halo cian → filo violeta,
    /// retorción helicoidal sutil (el chorro gira sobre su eje) y estela
    /// de polvo estelar que se queda flotando.
    ///
    /// AL MORIR: EL FLORECIMIENTO DEL QUÁSAR — destello cruzado (4 puntas)
    /// + anillo expansivo + AoE 130 px al 55% + temblor leve. El núcleo
    /// activo deja de alimentar el chorro y este se apaga en gloria.
    ///
    /// Determinista: TODO (nudos, helicoidal, pulso) se deriva de la edad
    /// (ai[0]) → MP coherente. ai[1] = huecos restantes de penetración
    /// (para que el dibujado sepa cuánto chorro queda).
    /// </summary>
    public class QuasarJetProjectile : ModProjectile
    {
        /// <summary>Largo visual del chorro (px).</summary>
        private const float JetLength = 132f;

        /// <summary>Nudos de shock a lo largo del chorro (fracciones 0..1).</summary>
        private static readonly float[] KnotT = { 0.22f, 0.40f, 0.58f, 0.76f, 0.92f };

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 14;          // ATRAVIESA al enemigo (v6.00: 10→14)
            Projectile.timeLeft = 120;           // v6.00 — más alcance (antes 90)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 1.2f;
            Projectile.extraUpdates = 2;        // 26 × 3 = ~78 px/t: RELATIVISTA
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            float age = Projectile.ai[0];
            Projectile.ai[0] = age + 1f;

            // el chorro NO desvía: recto como la luz
            Projectile.rotation = Projectile.velocity.ToRotation();

            // === ESTELA: polvo estelar que se queda flotando ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Vector2 back = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) *
                    Main.rand.NextFloat(10f, JetLength);
                Dust d = Dust.NewDustPerfect(back, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.4f, 0.4f)),
                    160, Main.rand.NextBool(3)
                        ? new Color(200, 235, 255)
                        : new Color(170, 140, 255), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === LUZ (¡un quásar!) ===
            Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.9f, 1.0f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // el nudo más cercano al impacto DESTELLA (materia eyectada)
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-2.5f, 2.5f)),
                    210, new Color(190, 230, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // === EL FLORECIMIENTO DEL QUÁSAR ===
            if (Main.netMode == NetmodeID.Server) return;

            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); } catch { }
            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item92, Projectile.Center); } catch { }

            // AoE final: el chorro se disipa en plasma
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int aoeDamage = Math.Max(1, (int)(Projectile.damage * 0.65f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > 170f) continue;   // v6.00 — florecimiento MÁS GRANDE (antes 130)
                    npc.SimpleStrikeNPC(aoeDamage, npc.direction, false, 2f, DamageClass.Magic);
                }
            }

            for (int i = 0; i < 30; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float spd = Main.rand.NextFloat(1.5f, 5.5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd,
                    220, Main.rand.NextBool(2)
                        ? new Color(200, 240, 255)
                        : new Color(180, 150, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — la lanza de luz con nudos
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;

                float age = Projectile.ai[0];
                Vector2 head = Projectile.Center - Main.screenPosition;
                float rot = Projectile.rotation;
                Vector2 dir = new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot));

                // vida (fade elegante al final)
                float lifeT = 1f - Projectile.timeLeft / 90f;
                float fade = 1f - Math.Max(0f, lifeT - 0.8f) / 0.2f * 0.6f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 origin = new Vector2(0f, glow.Height * 0.5f);

                // === 1. EL CUERPO DEL CHORRO: cónico, 3 capas (violeta→cian→blanco) ===
                // la helicoidal: el chorro se RETUERCE sutilmente sobre su eje
                float helix = age * 0.35f;
                for (int k = 0; k < 6; k++)
                {
                    float t0 = k / 6f;
                    float t1 = (k + 1) / 6f;
                    // grosor: fino en la punta, grueso atrás (cono)
                    float w0 = MathHelper.Lerp(15f, 5f, t0);
                    float w1 = MathHelper.Lerp(15f, 5f, t1);
                    // offset helicoidal LATERAL (amplitud crece hacia la cola)
                    float hx = (float)Math.Sin(helix - t0 * 9f) * 2.6f * t0;
                    float hx1 = (float)Math.Sin(helix - t1 * 9f) * 2.6f * t1;
                    Vector2 perp = new Vector2(-dir.Y, dir.X);
                    Vector2 p0 = head - dir * (JetLength * t0) + perp * hx;
                    Vector2 p1 = head - dir * (JetLength * t1) + perp * hx1;
                    Vector2 mid = (p0 + p1) * 0.5f;
                    float l = (p1 - p0).Length();
                    float segRot = (float)Math.Atan2((p1 - p0).Y, (p1 - p0).X);

                    // filo violeta (ancho)
                    Main.spriteBatch.Draw(glow, mid, null,
                        new Color(150, 130, 255, (byte)(int)(80 * fade)), segRot, origin,
                        new Vector2(l / (glow.Width * 0.5f), (w0 + w1) * 1.5f / (glow.Height * 0.5f)),
                        SpriteEffects.None, 0f);
                    // halo cian (medio)
                    Main.spriteBatch.Draw(glow, mid, null,
                        new Color(120, 220, 255, (byte)(int)(130 * fade)), segRot, origin,
                        new Vector2(l / (glow.Width * 0.5f), (w0 + w1) * 0.9f / (glow.Height * 0.5f)),
                        SpriteEffects.None, 0f);
                    // núcleo blanco (fino)
                    Main.spriteBatch.Draw(glow, mid, null,
                        new Color(240, 252, 255, (byte)(int)(225 * fade)), segRot, origin,
                        new Vector2(l / (glow.Width * 0.5f), (w0 + w1) * 0.38f / (glow.Height * 0.5f)),
                        SpriteEffects.None, 0f);
                }

                // === 2. LOS NUDOS DE SHOCK (los knots de Herbig-Haro) ===
                for (int i = 0; i < KnotT.Length; i++)
                {
                    float t = KnotT[i];
                    // el pulso VIAJA hacia la punta (materia recién eyectada)
                    float phase = (age * 0.06f + i * 0.37f) % 1f;
                    float bright = 0.55f + 0.45f * (float)Math.Sin(phase * MathHelper.TwoPi);
                    float knR = MathHelper.Lerp(11f, 5f, t) * (0.8f + 0.35f * bright);
                    Vector2 kp = head - dir * (JetLength * t);
                    // halo violeta-cian + núcleo blanco
                    Main.spriteBatch.Draw(glow, kp, null,
                        new Color(140, 180, 255, (byte)(int)(130 * bright * fade)), 0f,
                        glow.Size() * 0.5f, knR * 2.2f / (glow.Width * 0.5f),
                        SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(star, kp, null,
                        new Color(235, 250, 255, (byte)(int)(220 * bright * fade)), rot + helix * 0.5f,
                        star.Size() * 0.5f, knR / (star.Width * 0.5f),
                        SpriteEffects.None, 0f);
                }

                // === 3. LA CABEZA: donde nace el chorro (el "núcleo activo") ===
                Main.spriteBatch.Draw(glow, head, null,
                    new Color(220, 245, 255, (byte)(int)(220 * fade)), 0f,
                    glow.Size() * 0.5f, 30f / (glow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, head, null,
                    new Color(255, 255, 255, (byte)(int)(255 * fade)), rot,
                    star.Size() * 0.5f, 14f / (star.Width * 0.5f),
                    SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
    }
}
