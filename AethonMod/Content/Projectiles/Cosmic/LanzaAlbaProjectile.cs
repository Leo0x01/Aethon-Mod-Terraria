using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// LanzaAlbaProjectile — v6.24 — LA LANZA DEL ALBA.
    ///
    /// EL PROYECTIL DE LA LANZA — el filo del amanecer con las tres
    /// librerías:
    ///
    ///   · LA HOJA (LumenLib): Lance (la hoja de luz con doble pasada)
    ///     + LanceTrail (8 fantasmas creciendo hacia atrás) + el destello
    ///     de 4 puntas en la punta + el RAYO DE SOL que la precede (el
    ///     alba abriendo el camino).
    ///   · EL ROCÍO (BrumaFX): una VOLUTA de bruma (Tendril) serpenteando
    ///     sobre la estela real de la lanza — la niebla del amanecer que
    ///     deja tras de sí.
    ///   · EL TRUENO (StormLib): al golpear, CADENAS saltan de la lanza a
    ///     los 2 enemigos más cercanos (60% daño) + EndCap en la punta.
    ///   · LA RUNA: un glifo rúnico ardiendo en el corazón de la hoja.
    ///
    /// Perfora 4 enemigos (daño de vanilla); las cadenas y el drama son
    /// propios. La estela vive en un ring buffer de 8 posiciones.
    /// </summary>
    public class LanzaAlbaProjectile : ModProjectile
    {
        /// <summary>Regeneración de los rayos (Hz).</summary>
        private const float FlickHz = 12f;

        /// <summary>Profundidad del historial de la estela.</summary>
        private const int TrailDepth = 8;

        // === LA ESTELA (ring buffer de posiciones del mundo) ===
        private readonly Vector2[] _trail = new Vector2[TrailDepth];
        private bool _trailInit;

        // === LAS CADENAS de los golpes (2 saltos simultáneos) ===
        private readonly Vector2[] _chainFrom = new Vector2[2];
        private readonly Vector2[] _chainTo = new Vector2[2];
        private readonly float[] _chainLife = new float[2];

        // === v6.25 — EL PULSO DE IMPACTO (OndaLib.Pulse en el golpe) ===
        private float _hitPulse;

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;               // daño de vanilla (perforante)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista de la lanza.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 43);

        public override void AI()
        {
            _age += 1f;

            // v6.25 — el pulso de impacto envejece (se dibuja en PreDraw).
            if (_hitPulse > 0f) _hitPulse -= 1f;

            // === LA ESTELA: historial en ring buffer ===
            if (!_trailInit)
            {
                for (int i = 0; i < TrailDepth; i++) _trail[i] = Projectile.Center;
                _trailInit = true;
            }
            else
            {
                for (int i = TrailDepth - 1; i > 0; i--) _trail[i] = _trail[i - 1];
                _trail[0] = Projectile.Center;
            }

            // El filo gana ímpetu (el alba no se frena).
            Projectile.velocity *= 1.012f;

            // La vida de las cadenas decae.
            for (int c = 0; c < 2; c++)
                if (_chainLife[c] > 0f) _chainLife[c] -= 1f;

            // === LA LUZ de la lanza (muestreada a lo largo de la hoja) ===
            if (_age % 3f == 0f)
            {
                Vector2 tip = Projectile.Center + Projectile.velocity;
                LumenLib.LightAlong(Projectile.Center, tip,
                    new Color(255, 235, 190), 0.8f, 24f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // v6.30 — LAS ARMAS ELÉCTRICAS ELECTRIFICAN (petición del
            // usuario): la hoja que desata cadenas de rayo prende al contacto.
            try { target.AddBuff(BuffID.Electrified, 180); } catch { }

            // === v6.25 — EL PULSO DE IMPACTO (OndaLib: la onda se dibuja
            //     en PreDraw; la SACUDIDA suave ya) + LAS ASCUAS de la
            //     tabla SolarFire (PyraLib.Sparks → ParticleManager) ===
            _hitPulse = 10f;
            if (Main.netMode != NetmodeID.Server)
            {
                OndaLib.Kick(3f, 8);
                PyraLib.Sparks(Projectile.Center, Vector2.Zero, 6,
                    PyraPalettes.SolarFire, Seed + 500, out ParticleData[] ascuas);
                if (ascuas != null)
                    for (int s = 0; s < ascuas.Length; s++)
                        ParticleManager.Spawn(ascuas[s]);
            }

            // === LAS CHISPAS del golpe ===
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                        Main.rand.NextFloat(2f, 6f),
                        220, new Color(255, 240, 190), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item93.WithPitchOffset(0.45f).WithVolumeScale(0.5f),
                    Projectile.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // === LA CADENA: 2 saltos desde el golpe (60% daño) ===
            int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.60f));
            var hitList = new List<NPC> { target };
            Vector2 origin = Projectile.Center;
            for (int c = 0; c < 2; c++)
            {
                NPC best = null;
                float bestDist = 300f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc) || hitList.Contains(npc)) continue;
                    float dist = (npc.Center - origin).Length();
                    if (dist < bestDist) { bestDist = dist; best = npc; }
                }
                if (best == null) { _chainLife[c] = 0f; continue; }

                _chainFrom[c] = Projectile.Center;
                _chainTo[c] = best.Center;
                _chainLife[c] = 14f;
                origin = best.Center;
                hitList.Add(best);

                best.SimpleStrikeNPC(chainDamage, best.direction, false,
                    1.5f, DamageClass.Magic);
                try { best.AddBuff(BuffID.Electrified, 180); } catch { }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // La despedida: un puñado de chispas + un puff de bruma.
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 8; i++)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                    Main.rand.NextFloat(1.5f, 5f),
                    200, new Color(255, 225, 150), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawLance();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ------------------------------------------------------------------
        //  LA PALETA DEL ALBA
        // ------------------------------------------------------------------

        private static readonly Color DawnGold = new(255, 225, 150);
        private static readonly Color DawnWhite = new(255, 250, 240);
        private static readonly Color StarBlue = new(150, 180, 255);
        private static readonly Color WhiteIncan = new(255, 250, 235);
        private static readonly Color BrumaAlba = new(148, 120, 190);

        // ------------------------------------------------------------------
        //  EL DIBUJO DE LA LANZA
        // ------------------------------------------------------------------

        private void DrawLance()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;

                Vector2 dir = Projectile.velocity;
                if (dir.LengthSquared() < 0.001f) dir = Vector2.UnitX;
                dir.Normalize();
                float lifeFade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. EL RAYO DE SOL que la precede (LumenLib — el alba
                //     abriendo el camino, con grosor animado) ===
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 4.2f);
                LumenLib.Ray(Main.spriteBatch, center + dir * 14f, dir,
                    92f, 9f, DawnGold, 0.30f + 0.18f * pulse, pulse);

                // === 2. LA ESTELA DE FANTASMAS (LumenLib.LanceTrail — las
                //     copias creciendo hacia atrás) ===
                LumenLib.LanceTrail(Main.spriteBatch, center - dir * 10f, dir,
                    46f, 10f, DawnGold, 0.55f * lifeFade, 8, 8f);

                // === 3. LA HOJA (LumenLib.Lance — la doble pasada de la hoja) ===
                LumenLib.Lance(Main.spriteBatch, center, dir, 50f, 11f,
                    DawnGold, 0.85f * lifeFade);

                // === 3b. v6.25 — EL FUEGO DEL ALBA (PyraLib.Tongue con la
                //     tabla SolarFire): la punta de la lanza ARDE — la
                //     llama ancla en la punta y crece CONTRA la marcha
                //     (rot = marcha − 90°), con su parpadeo inconmensurable,
                //     su punta vaga y su núcleo blanco en la base ===
                {
                    Vector2 punta = center + dir * 22f;
                    float rotMarcha = dir.ToRotation() - MathHelper.PiOver2;
                    PyraLib.Tongue(Main.spriteBatch, punta, 20f, 7.5f,
                        PyraPalettes.SolarFire, 0.82f, seed + 91, time,
                        0.75f * lifeFade, 0f, 1f, rotMarcha);
                }

                // === 3c. v6.25 — EL PULSO DE IMPACTO (OndaLib.Pulse: el
                //     eco de cada golpe — anillo doble que nace y muere) ===
                if (_hitPulse > 0f)
                {
                    float prog = 1f - _hitPulse / 10f;
                    OndaLib.Pulse(Main.spriteBatch, center, prog, 95f,
                        DawnGold, _hitPulse / 10f, seed + 3);
                }

                // === 4. EL CORAZÓN: destello + la RUNA ardiente ===
                LumenLib.Flare(Main.spriteBatch, center, 20f, DawnWhite,
                    0.40f * lifeFade, time * 0.6f);
                DrawHeartRune(center, time, lifeFade);

                // === 5. EL ROCÍO (BrumaFX.Tendril sobre la estela REAL) ===
                if (_trailInit)
                {
                    Vector2[] path = new Vector2[5];
                    for (int k = 0; k < 5; k++)
                        path[k] = _trail[Math.Min(k * 2, TrailDepth - 1)] - Main.screenPosition;
                    BrumaFX.Tendril(path, 13f, BrumaAlba, seed + 77, time,
                        alpha: 0.24f * lifeFade, fade: 0.75f);
                }

                // === 6. EL TRUENO: la punta eléctrica + las cadenas de los golpes ===
                Vector2 tip = center + dir * 24f;
                StormLib.EndCap(Main.spriteBatch, tip, 8f,
                    Tint(DawnGold, 0.55f * lifeFade), Tint(WhiteIncan, 0.85f * lifeFade),
                    lifeFade);
                for (int c = 0; c < 2; c++)
                {
                    if (_chainLife[c] <= 0f) continue;
                    if (!StormLib.IsLit(seed + 61 + c * 37, flick, 0.60f)) continue;
                    float a = MathHelper.Clamp(_chainLife[c] / 14f, 0f, 1f);
                    StormLib.ChainBolt(Main.spriteBatch,
                        tip, _chainTo[c] - Main.screenPosition,
                        seed + 61 + c * 37, flick, 4.0f,
                        Tint(StarBlue, 0.75f * a), Tint(new Color(230, 240, 255), 0.9f * a),
                        1f, 7, 10f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>LA RUNA del corazón de la hoja (la firma del amanecer).</summary>
        private void DrawHeartRune(Vector2 center, float time, float fade)
        {
            // LA ESTRELLA DOBLE — el glifo del alba.
            Vector2[] strokes =
            {
                new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f),
                new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f),
            };
            float gScale = 0.66f;
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 3.1f);
            float rot = time * 0.9f;

            Quad(VFXCore.SoftGlow, center, new Vector2(24f * gScale, 24f * gScale), 0f,
                Tint(DawnGold, 0.22f * pulse * fade));

            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 a = center + strokes[s].RotatedBy(rot) * gScale;
                Vector2 b = center + strokes[s + 1].RotatedBy(rot) * gScale;
                Vector2 mid = (a + b) * 0.5f;
                Vector2 d = b - a;
                float len = d.Length();
                if (len < 0.01f) continue;
                Capsule(mid, len, 2.9f * gScale, (float)Math.Atan2(d.Y, d.X),
                    Tint(DawnWhite, 0.85f * pulse * fade));
            }
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(VFXCore.SoftGlow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
