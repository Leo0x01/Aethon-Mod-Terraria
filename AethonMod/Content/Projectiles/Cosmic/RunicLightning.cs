using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RunicLightning — v6.19 — EL RAYO DEL CETRO DEL TRUENO RÚNICO.
    ///
    /// Un LINE-STRIKE (el kit de la investigación): el rayo nace en la
    /// punta del jugador y golpea AL INSTANTE el punto del cursor — el
    /// proyectil NUNCA SE MUEVE (velocity → 0 en su primer tick): es una
    /// descarga, no un proyectil viajero.
    ///
    /// EFECTOS APLICADOS AL RAYO (todos con la LIBRERÍA LightningCore
    /// del proyecto — v6.18):
    ///   1. RAYO PRINCIPAL zigzag vivo: se re-genera a ~14 Hz (FlickTick)
    ///      con doble tira cuerpo-oro + núcleo-blanco, ramas y gorros.
    ///   2. DAÑO EN LÍNEA: Collision.CheckAABBvLineCollision barre TODO lo
    ///      que cruza la descarga (ventana de daño en los primeros ticks).
    ///   3. CADENA ELÉCTRICA: desde el impacto saltan rayos secundarios a
    ///      hasta 3 enemigos cercanos (60% del daño, más tenues).
    ///   4. ARCOS DE IMPACTO: LightningCore.Arc alrededor del punto de
    ///      golpe, vibrando mientras vive la descarga.
    ///   5. ELECTRIFICACIÓN: todo lo tocado queda Electrified (240 ticks).
    ///   6. ONDA DE CHOQUE: un anillo expandiéndose desde el impacto + luz
    ///      dinámica a lo largo de toda la línea.
    /// </summary>
    public class RunicLightning : ModProjectile
    {
        /// <summary>Duración total de la descarga (ticks).</summary>
        private const int BoltLife = 18;

        /// <summary>Alcance máximo del rayo (px).</summary>
        private const float MaxRange = 560f;

        /// <summary>Raíz cuadrada del par de destellos — regeneración ~14 Hz.</summary>
        private const float FlickHz = 14f;

        // === EL CAMINO DEL RAYO (coords de MUNDO — campos visuales) ===
        private Vector2 _start;
        private Vector2 _end;
        private bool _anchored;

        // === LAS CADENAS (hasta 3 saltos desde el impacto) ===
        private readonly Vector2[] _chainFrom = new Vector2[3];
        private readonly Vector2[] _chainTo = new Vector2[3];
        private readonly bool[] _chainAlive = new bool[3];

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BoltLife;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            _age += 1f;

            // === EL ANCLAJE: el rayo NO viaja — es una descarga fija ===
            if (!_anchored)
            {
                _anchored = true;
                _start = Projectile.Center;
                // velocity llegó como el VECTOR COMPLETO al objetivo
                // (clampeado a MaxRange por el arma).
                Vector2 toTarget = Projectile.velocity;
                float len = toTarget.Length();
                if (len > MaxRange) toTarget *= MaxRange / len;
                if (len < 24f) toTarget = Vector2.Normalize(toTarget == Vector2.Zero
                    ? Vector2.UnitX : toTarget) * 24f;
                _end = _start + toTarget;
                Projectile.velocity = Vector2.Zero;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
            }

            // === 2+3. LA VENTANA DE DAÑO: línea + cadena (2 golpes: al
            // anclarse y al despedirse — el rayo "persiste" un instante) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                (_age == 2f || _age == 8f))
            {
                StrikeLine();
                StrikeChains();
            }

            // === 6. LUZ a lo largo de la descarga ===
            Vector2 mid = (_start + _end) * 0.5f;
            Lighting.AddLight(_start, 0.75f, 0.68f, 0.42f);
            Lighting.AddLight(mid, 0.75f, 0.68f, 0.42f);
            Lighting.AddLight(_end, 0.95f, 0.88f, 0.55f);
        }

        /// <summary>Daño en LÍNEA: todo lo que cruza la descarga (con margen).</summary>
        private void StrikeLine()
        {
            const float Margin = 14f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 c1 = npc.position - new Vector2(Margin, Margin);
                Vector2 c2 = npc.Size + new Vector2(Margin * 2f, Margin * 2f);
                if (!Collision.CheckAABBvLineCollision(c1, c2, _start, _end)) continue;

                npc.SimpleStrikeNPC(Projectile.damage, npc.direction, false,
                    2f, DamageClass.Magic);
                try { npc.AddBuff(BuffID.Electrified, 240); } catch { }
            }
        }

        /// <summary>La CADENA: hasta 3 saltos desde el impacto (60% del daño).</summary>
        private void StrikeChains()
        {
            int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.60f));
            Vector2 origin = _end;

            for (int c = 0; c < 3; c++)
            {
                NPC best = null;
                float bestDist = 300f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - origin).Length();
                    if (dist < bestDist)
                    {
                        // Que no sea el mismo punto de impacto dos veces.
                        bool already = false;
                        for (int p = 0; p < c; p++)
                            if (_chainAlive[p] && (_chainTo[p] - npc.Center).Length() < 8f)
                                already = true;
                        if (already) continue;
                        bestDist = dist;
                        best = npc;
                    }
                }
                if (best == null) { _chainAlive[c] = false; continue; }

                _chainFrom[c] = origin;
                _chainTo[c] = best.Center;
                _chainAlive[c] = true;
                origin = best.Center; // la cadena sigue desde el último golpeado.

                best.SimpleStrikeNPC(chainDamage, best.direction, false, 1.5f, DamageClass.Magic);
                try { best.AddBuff(BuffID.Electrified, 240); } catch { }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Guard: hasta anclarse no hay descarga que dibujar.
            if (!_anchored) return false;

            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10 — la lección de
            //  los agujeros): durante PreDraw el batch de tML está ABIERTO;
            //  hay que CERRARLO antes de que DrawBolt() llame a Begin()
            //  aditivo. Sin esto: InvalidOperationException "Begin has been
            //  called before calling End" → el rayo NUNCA se pinta (arma
            //  invisible + error en client.log) — bug v6.19 corregido.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawBolt();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles) — el mismo
        /// contrato validado de la familia de agujeros negros.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private void DrawBolt()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Math.Max(1, Projectile.identity + 7);
                int flick = LightningCore.FlickTick(time, FlickHz);
                float lifeT = MathHelper.Clamp(_age / BoltLife, 0f, 1f);
                // La descarga NACE a plena potencia y se APAGA al final.
                float fade = 1f - MathHelper.Clamp((lifeT - 0.55f) / 0.45f, 0f, 1f);
                if (fade <= 0.02f) return;

                Vector2 screen = Main.screenPosition;
                Vector2 start = _start - screen;
                Vector2 end = _end - screen;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Color haloGold = Tint(new Color(255, 195, 85), 0.62f * fade);
                Color coreWhite = Tint(new Color(255, 250, 235), 0.95f * fade);

                // === 1. EL RAYO PRINCIPAL (zigzag vivo, doble tira) ===
                // Nace en el AURA DE MANO (glow de arranque).
                Quad(GlowTex(), start, new Vector2(34f, 34f), 0f,
                    Tint(new Color(255, 210, 120), 0.55f * fade));
                LightningCore.Bolt(Main.spriteBatch, start, end,
                    seed, flick, 7.5f, haloGold, coreWhite, 1f, 8, 16f);

                // === 3. LAS CADENAS (tenues, azul-estelar) ===
                for (int c = 0; c < 3; c++)
                {
                    if (!_chainAlive[c]) continue;
                    Vector2 from = _chainFrom[c] - screen;
                    Vector2 to = _chainTo[c] - screen;
                    LightningCore.Bolt(Main.spriteBatch, from, to,
                        seed + 61 + c * 37, flick, 4.2f,
                        Tint(new Color(150, 180, 255), 0.45f * fade),
                        Tint(new Color(230, 240, 255), 0.75f * fade),
                        1f, 5, 9f);
                }

                // === 4. LOS ARCOS DE IMPACTO (coronas eléctricas) ===
                if (LightningCore.Flicker(seed + 40, flick, 0.80f))
                {
                    float arcR = 20f + 26f * lifeT;
                    LightningCore.Arc(Main.spriteBatch, end, arcR,
                        time * 2.1f, time * 2.1f + 1.9f,
                        seed + 40, flick, 3.4f,
                        Tint(new Color(255, 195, 85), 0.50f * fade), coreWhite, 1f, 8);
                    LightningCore.Arc(Main.spriteBatch, end, arcR * 0.66f,
                        -time * 2.7f + 2.5f, -time * 2.7f + 4.1f,
                        seed + 41, flick, 2.8f,
                        Tint(new Color(150, 180, 255), 0.45f * fade),
                        Tint(new Color(230, 240, 255), 0.70f * fade), 1f, 8);
                }

                // === 6. LA ONDA DE CHOQUE (anillo expandiéndose) ===
                float wavePhase = MathHelper.Clamp(_age / 12f, 0f, 1f);
                float waveR = 18f + 90f * wavePhase;
                float waveFade = (1f - wavePhase) * (1f - wavePhase) * fade;
                Quad(RingTex(), end,
                    VFXCore.RingQuadSize(waveR), wavePhase * 2.5f,
                    Tint(new Color(255, 230, 160), 0.45f * waveFade));

                // === EL NÚCLEO DEL IMPACTO (la chispa cegadora) ===
                Quad(GlowTex(), end, new Vector2(52f, 52f), 0f,
                    Tint(new Color(255, 210, 120), 0.60f * fade));
                Quad(GlowTex(), end, new Vector2(24f, 24f), 0f,
                    Tint(new Color(255, 250, 235), 0.85f * fade));

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        private static Texture2D GlowTex() => VFXCore.SoftGlow;

        private static Texture2D RingTex() => VFXCore.Ring;

        private static void Quad(Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        public override void OnKill(int timeLeft)
        {
            // Chispas finales en el punto de impacto.
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 10; i++)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(_end, DustID.Electric,
                    new Vector2((float)Math.Cos(ang) * Main.rand.NextFloat(2f, 6f),
                                (float)Math.Sin(ang) * Main.rand.NextFloat(2f, 6f)),
                    220, new Color(255, 235, 170), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
