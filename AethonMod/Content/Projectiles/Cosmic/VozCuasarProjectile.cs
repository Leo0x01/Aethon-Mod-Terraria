using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// VozCuasarProjectile — LA VOZ DEL CUÁSAR.
    ///
    /// LA CRONOLOGÍA (105 ticks):
    ///   · LA CARGA (40 ticks): el cuásar —disco de acreción compacto
    ///     blanco-azul + anillo fino inclinado— TELEGRAFÍA: el anillo se
    ///     TENSA (contrae de 2.6R a 1.3R), el disco se APRIETA (gira más
    ///     rápido) y un PITIDO sube cada 8 ticks.
    ///   · LA VOZ (60 ticks): HAZ CONTINUO de 460 px (quad RiftLib.Tear)
    ///     en la dirección de disparo. PERFORA TODO (i-frames 6 por
    ///     objetivo), con DOPPLER cromático: dos quads — la cola roja,
    ///     el borde delantero azul. Si toca una pared, REBOTA una vez
    ///     (refleja la dirección y agota el resto del largo).
    ///   · EL SILENCIO: muere con un destello.
    ///
    /// Determinismo MP: la dirección viaja en ai[0]; el rebote se
    /// recalcula por tick DESDE EL MUNDO (mismo resultado en todas las
    /// máquinas); la semilla visual es Projectile.identity.
    /// </summary>
    public class VozCuasarProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int CargaTicks = 40;
        private const int HazTicks = 60;

        /// <summary>Largo total del haz (px).</summary>
        private const float LongitudHaz = 460f;

        /// <summary>Ancho de colisión del haz (la cápsula).</summary>
        private const float AnchoColision = 44f;

        /// <summary>i-frames por objetivo (un golpe cada 6 ticks).</summary>
        private const int IframesHaz = 6;

        /// <summary>Daño relativo por golpe de haz (perfora: muchos golpes).</summary>
        private const float DañoHaz = 0.35f;

        // === LA PALETA (cuásar blanco-azul MUY caliente) ===
        private static readonly Color ColorBlanco = new(240, 249, 255);
        private static readonly Color ColorCian = new(150, 220, 255);
        private static readonly Color ColorAzul = new(70, 130, 255);

        /// <summary>Paleta DOPPLER — la COLA del haz (desplazada al ROJO).</summary>
        private static readonly Color[] PaletaRoja =
        {
            new Color(255, 118, 88),
            new Color(255, 185, 145),
            new Color(255, 232, 218),
        };

        /// <summary>Paleta DOPPLER — el BORDE DELANTERO (desplazado al AZUL).</summary>
        private static readonly Color[] PaletaAzul =
        {
            new Color(88, 158, 255),
            new Color(150, 216, 255),
            new Color(240, 250, 255),
        };

        private float _age;
        private bool _nacio;
        private Vector2 _dir = Vector2.UnitX;

        // Los segmentos vivos del haz (recalculados cada tick desde el mundo).
        private float _seg1Len;
        private bool _reboto;
        private Vector2 _reboteOrigen;
        private Vector2 _reboteDir = Vector2.UnitX;
        private float _seg2Len;

        /// <summary>i-frames por objetivo: whoAmI → tick del último golpe.</summary>
        private readonly Dictionary<int, int> _ultimoGolpe = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            // Daño 100% sub-ataques — v6.50 — GolpeMotor (el cauce del
            // motor): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = CargaTicks + HazTicks + 5;
            Projectile.ignoreWater = true;
            // La voz del cuásar no la frenan las paredes: las REBOTA.
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 67);

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _dir = Projectile.ai[0] != 0f
                    ? new Vector2(MathF.Cos(Projectile.ai[0]), MathF.Sin(Projectile.ai[0]))
                    : Vector2.UnitX;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }

            // El cuásar flota QUIETO donde nació.
            Projectile.velocity = Vector2.Zero;

            float luz = 0.30f + 0.30f * MathHelper.Clamp(_age / CargaTicks, 0f, 1f);
            Lighting.AddLight(Projectile.Center, luz * 0.75f, luz * 0.85f, luz);

            if (_age < CargaTicks)
            {
                // ============================================================
                //  LA CARGA — telegrafía: el pitido sube cada 8 ticks.
                // ============================================================
                if (Main.netMode != NetmodeID.Server && _age % 8f == 0f)
                {
                    float subida = -0.35f + 0.95f * (_age / CargaTicks);
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15.WithPitchOffset(subida), Projectile.Center); }
                    catch { }
                }
            }
            else
            {
                // ============================================================
                //  LA VOZ — el haz continuo (segmentos desde el mundo).
                // ============================================================
                float hazAge = _age - CargaTicks;
                float env = MathHelper.Clamp(hazAge / 6f, 0f, 1f)
                          * MathHelper.Clamp((HazTicks - hazAge) / 10f, 0f, 1f);

                ResolverSegmentos(hazAge);

                // La luz RECORRE el haz.
                for (int i = 0; i <= 4; i++)
                {
                    Vector2 p = Projectile.Center + _dir * (_seg1Len * i / 4f);
                    Lighting.AddLight(p, 0.28f * env, 0.34f * env, 0.46f * env);
                }
                if (_reboto)
                    Lighting.AddLight(_reboteOrigen + _reboteDir * (_seg2Len * 0.5f),
                        0.20f * env, 0.26f * env, 0.36f * env);

                if (env >= 0.15f)
                    GolpearHaz();
            }
        }

        /// <summary>
        /// LOS SEGMENTOS DEL HAZ: raycast del centro hasta la primera pared;
        /// si encuentra pared y NO ha rebotado aún, REFLEJA la dirección
        /// (una sola vez) y agota el largo restante.
        /// </summary>
        private void ResolverSegmentos(float hazAge)
        {
            float largo = LongitudHaz * MathHelper.Clamp(hazAge / 8f, 0f, 1f);

            float d1 = Trazar(Projectile.Center, _dir, largo);
            _seg1Len = d1;
            _reboto = false;
            _seg2Len = 0f;

            if (d1 < largo - 4f)
            {
                // PARED: calcular la NORMAL por el truco de las dos muestras.
                Vector2 golpe = Projectile.Center + _dir * d1;
                Vector2 prev = golpe - _dir * 8f;
                Vector2 next = golpe + _dir * 8f;
                int px = (int)(prev.X / 16f), py = (int)(prev.Y / 16f);
                int nx = (int)(next.X / 16f), ny = (int)(next.Y / 16f);

                bool flipX = WorldGen.SolidTile(nx, py);   // pared vertical
                bool flipY = WorldGen.SolidTile(px, ny);   // suelo/techo
                if (!flipX && !flipY) { flipX = true; flipY = true; }   // esquina

                _reboteDir = new Vector2(flipX ? -_dir.X : _dir.X, flipY ? -_dir.Y : _dir.Y);
                _reboteOrigen = golpe;
                _reboto = true;

                float restante = largo - d1;
                _seg2Len = Trazar(golpe + _reboteDir * 8f, _reboteDir, restante);
            }
        }

        /// <summary>Raycast por pasos de 10 px: distancia a la primera pared.</summary>
        private static float Trazar(Vector2 origen, Vector2 dir, float max)
        {
            const float paso = 10f;
            for (float d = paso; d <= max; d += paso)
                if (WorldGen.SolidTile((int)((origen.X + dir.X * d) / 16f),
                                       (int)((origen.Y + dir.Y * d) / 16f)))
                    return d - paso * 0.5f;
            return max;
        }

        /// <summary>
        /// EL HAZ QUE PERFORA TODO (×0.35, i-frames 6 por objetivo): prueba
        /// el segmento principal y el rebote. v6.50 — GolpeMotor (el cauce
        /// del motor: crítica real, varianza, on-hit y sync MP del propio
        /// motor, resuelto en el cliente dueño; el debuff sigue autoridad).
        /// </summary>
        private void GolpearHaz()
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoHaz));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;

                bool tocado = RiftLib.LineaToca(Projectile.Center, _dir, _seg1Len, AnchoColision, npc.Hitbox);
                if (!tocado && _reboto && _seg2Len > 4f)
                    tocado = RiftLib.LineaToca(_reboteOrigen, _reboteDir, _seg2Len, AnchoColision, npc.Hitbox);
                if (!tocado) continue;

                if (_ultimoGolpe.TryGetValue(npc.whoAmI, out int ultimo) && _age - ultimo < IframesHaz)
                    continue;
                _ultimoGolpe[npc.whoAmI] = (int)_age;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 240); } catch { }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // MUERE CON UN DESTELLO.
            OndaLib.Flash(ColorBlanco, 0.24f, 8);
            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(0.55f), Projectile.Center); }
            catch { }
            RiftLib.ChispasAnomalia(Projectile.Center, 14,
                new Color[] { ColorBlanco, ColorCian, ColorAzul }, Seed + 4, out ParticleData[] motas);
            if (motas != null)
                for (int i = 0; i < motas.Length; i++)
                    ParticleManager.Spawn(motas[i]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar, dibujar, restaurar.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawCuasar(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private void DrawCuasar()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            bool cargando = _age < CargaTicks;
            float hazAge = _age - CargaTicks;
            float env = cargando ? 0f
                : MathHelper.Clamp(hazAge / 6f, 0f, 1f)
                  * MathHelper.Clamp((HazTicks - hazAge) / 10f, 0f, 1f);
            float tCarga = MathHelper.Clamp(_age / CargaTicks, 0f, 1f);

            // El disco se APRIETA al cargar (R 24→19) y respira al hablar.
            float R = MathHelper.Lerp(24f, 19f, cargando ? tCarga : 0f)
                    * (cargando ? 1f : 1f + 0.05f * MathF.Sin(time * 9f + seed));
            float fade = 1f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // 1. EL BLOOM DEL DISCO DE ACRECIÓN.
            LumenLib.Bloom(Main.spriteBatch, drawPos, R * (2.4f + 0.5f * tCarga),
                ColorCian, (0.34f + 0.18f * tCarga) * fade, 3);

            // 2. EL ANILLO FINO INCLINADO — se TENSA al cargar (2.6R → 1.3R).
            float tension = cargando ? MathHelper.Lerp(2.6f, 1.3f, tCarga * tCarga) : 1.35f;
            float ringRot = 0.95f + MathF.Sin(time * (cargando ? 2.5f + 5f * tCarga : 1.2f) + seed) * 0.18f;
            Vector2 ringSize = VFXCore.RingQuadSize(R * tension);
            ringSize.Y *= 0.34f;
            Main.spriteBatch.Draw(VFXCore.Ring, drawPos, null,
                Tint(ColorAzul, 0.45f * fade), ringRot,
                VFXCore.Ring.Size() * 0.5f,
                ringSize / VFXCore.Ring.Size(), SpriteEffects.None, 0f);

            // 3. LA TELEGRAFÍA: la línea tenue del disparo venidero.
            if (cargando)
            {
                Seg(Main.spriteBatch, drawPos + _dir * (R * 0.8f),
                    drawPos + _dir * (R * 0.8f + LongitudHaz * 0.22f * tCarga),
                    3f, Tint(ColorCian, 0.06f + 0.16f * tCarga));
            }

            // 4. LA VOZ: EL HAZ CONTINUO CON DOPPLER — dos quads (cola roja,
            //    borde delantero azul) + el segmento del rebote.
            if (!cargando && env > 0.02f)
            {
                float prog = MathHelper.Clamp(hazAge / HazTicks, 0f, 1f);
                float ancho = 26f;

                // a) LA COLA (roja): primer 45% del segmento principal.
                float cola = _seg1Len * 0.45f;
                if (cola > 8f)
                    RiftLib.Tear(Main.spriteBatch, drawPos, _dir, cola, prog, ancho * 0.85f,
                        PaletaRoja, 0.85f * env, seed, time);

                // b) EL BORDE DELANTERO (azul): resto del segmento principal.
                float frente = _seg1Len - cola;
                if (frente > 8f)
                    RiftLib.Tear(Main.spriteBatch, drawPos + _dir * cola, _dir, frente, prog, ancho,
                        PaletaAzul, 0.95f * env, seed + 31, time);

                // c) EL REBOTE: el eco de la voz contra la pared.
                if (_reboto && _seg2Len > 8f)
                {
                    RiftLib.Tear(Main.spriteBatch, _reboteOrigen - Main.screenPosition + _reboteDir * 4f,
                        _reboteDir, _seg2Len, prog, ancho * 0.8f,
                        PaletaAzul, 0.70f * env, seed + 63, time);

                    // El chisporroteo del impacto en la pared.
                    StormLib.ImpactFlash(Main.spriteBatch, _reboteOrigen - Main.screenPosition,
                        30f, ColorBlanco, 0.55f * env, time * 4f);
                }

                // La boca del haz (el gorro de descarga).
                LumenLib.Bloom(Main.spriteBatch, drawPos + _dir * (R * 0.6f), R * 1.5f,
                    ColorBlanco, 0.55f * env * fade, 2);
            }

            Main.spriteBatch.End();

            // 5. EL DISCO DE ACRECIÓN (DrawSunBody gestiona sus lotes): gira
            //    cada vez más RÁPIDO al cargarse (2.2 → 6.5: el apriete).
            float spin = cargando ? MathHelper.Lerp(2.2f, 6.5f, tCarga) : 5.5f;
            RuneSunRenderer.DrawSunBody(drawPos, R, time * 0.9f, time,
                new Color(242, 250, 255),   // main — blanco caliente
                new Color(140, 190, 250),   // darker — azul fusión
                new Color(60, 120, 250),    // accent — azul profundo
                new Color(160, 220, 255),   // backHot — halo cian
                new Color(80, 140, 255),    // backRed — halo azul
                new Color(235, 248, 255),   // shine — destello
                spin, 0.95f * fade);
        }

        /// <summary>Segmento tenue (la telegrafía de la carga).</summary>
        private static void Seg(SpriteBatch batch, Vector2 a, Vector2 b, float grosor, Color tint)
        {
            Vector2 seg = b - a;
            float len = seg.Length();
            if (len < 1f) return;
            var texSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);
            batch.Draw(VFXCore.SoftGlow, (a + b) * 0.5f, null, tint,
                MathF.Atan2(seg.Y, seg.X), texSize * 0.5f,
                new Vector2(len, grosor) / texSize, SpriteEffects.None, 0f);
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
