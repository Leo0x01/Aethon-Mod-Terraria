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
    /// RayoGammaProjectile — EL RAYO GAMMA (la técnica del juicio diferido).
    ///
    /// Al disparar, un HAZ FINO HITSCAN de 700 px cruza TODO al
    /// instante — un flash blanco-cian deslumbrante con aberración
    /// cromática: tres líneas R/G/B desplazadas 1 px que CONVERGEN —
    /// pero NO hace daño aún: cada enemigo tocado queda MARCADO con un
    /// contorno fino brillante.
    ///
    /// Tras 30 ticks (el jugador ya bajó la guardia), TODAS las marcas
    /// LIQUIDAN a la vez: "clang" sonoro + destello en cada víctima +
    /// el daño ×1.0 acumulado + Quemadura Cósmica 5 s. Si un enemigo
    /// marcado muere antes, su marca se apaga en silencio.
    /// </summary>
    public class RayoGammaProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int MarcaTicks = 30;   // el juicio tarda esto en llegar
        private const int HazTicks = 6;      // el flash inicial del haz
        private const int PostTicks = 6;     // el afterglow de la liquidación

        // === LA LÍNEA ===
        private const float Largo = 700f;
        private const float AnchoHaz = 18f;

        // === LA PALETA (blanco-cian + aberración R/G/B) ===
        private static readonly Color ColorCian = new(150, 240, 255);
        private static readonly Color ColorBlanco = new(245, 253, 255);
        private static readonly Color ColorRojo = new(255, 60, 60);
        private static readonly Color ColorVerde = new(70, 255, 110);
        private static readonly Color ColorAzul = new(90, 130, 255);
        private static readonly Color[] PaletaJuicio =
        {
            new Color(200, 245, 255), new Color(130, 220, 255)
        };

        private float _age;
        private bool _nacio;
        private Vector2 _origen;
        private Vector2 _dir = Vector2.UnitX;

        /// <summary>Los whoAmI marcados por el haz.</summary>
        private readonly List<int> _marcas = new();

        /// <summary>Las posiciones de las víctimas al liquidar (los destellos).</summary>
        private readonly List<Vector2> _destellos = new();

        private bool _liquidada;
        private float _liqAge;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MarcaTicks + PostTicks + 4;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 73);

        public override void AI()
        {
            _age += 1f;
            Projectile.velocity = Vector2.Zero;

            if (!_nacio)
            {
                _nacio = true;
                _origen = Projectile.Center;
                _dir = Projectile.ai[0].ToRotationVector2();
                ResolverHitscan();
            }

            // LA LUZ: el haz al nacer; luego, un temblor en cada marca.
            if (_age <= HazTicks)
                LumenLib.LightAlong(_origen, _origen + _dir * Largo, ColorCian, 0.85f, 90f);
            else if (!_liquidada)
            {
                for (int i = 0; i < _marcas.Count; i++)
                {
                    NPC npc = _marcas[i] >= 0 && _marcas[i] < Main.maxNPCs ? Main.npc[_marcas[i]] : null;
                    if (npc != null && npc.active)
                        Lighting.AddLight(npc.Center, 0.18f, 0.26f, 0.32f);
                }
            }

            if (!_liquidada && _age >= MarcaTicks)
                Liquidar();

            if (_liquidada)
            {
                _liqAge += 1f;
                if (_liqAge >= PostTicks)
                    Projectile.Kill();
            }
        }

        /// <summary>
        /// EL CRUZE: la línea fina de 700 px toca a TODO (lectura pura —
        /// corre en todas las máquinas para dibujar los contornos).
        /// </summary>
        private void ResolverHitscan()
        {
            _marcas.Clear();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (RiftLib.LineaToca(_origen, _dir, Largo, AnchoHaz, npc.Hitbox))
                    _marcas.Add(npc.whoAmI);
            }
        }

        /// <summary>LA LIQUIDACIÓN: todas las marcas revientan a la vez.</summary>
        private void Liquidar()
        {
            _liquidada = true;
            _liqAge = 0f;
            _destellos.Clear();

            foreach (int who in _marcas)
            {
                NPC npc = who >= 0 && who < Main.maxNPCs ? Main.npc[who] : null;
                if (npc == null || !npc.active || !VFXCore.EsObjetivo(npc))
                    continue;   // su marca se apaga en silencio

                _destellos.Add(npc.Center);

                // v6.50 — GolpeMotor (el cauce del motor: crítica real,
                // varianza, on-hit y sync MP del propio motor).
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Math.Max(1, Projectile.damage), 5f, true);
                // La quemadura del rayo: server/SP (autoridad del debuff).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 300); } catch { }
            }
            _marcas.Clear();

            if (Main.netMode != NetmodeID.Server && _destellos.Count > 0)
            {
                // EL CLANG del juicio + el destello en cada víctima + el kick suave.
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item9.WithPitchOffset(0.55f), Projectile.Center);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item27.WithPitchOffset(0.4f), Projectile.Center);
                }
                catch { }
                OndaLib.Kick(2.6f, 9);
                OndaLib.Flash(ColorCian, 0.14f, 8, Projectile.Center);

                for (int i = 0; i < _destellos.Count; i++)
                {
                    RiftLib.ChispasAnomalia(_destellos[i], 8, PaletaJuicio,
                        Seed + i * 7, out ParticleData[] motas);
                    if (motas != null)
                        for (int k = 0; k < motas.Length; k++)
                            ParticleManager.Spawn(motas[k]);
                }
            }
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

            try { DrawJuicio(); }
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

        private void DrawJuicio()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 origen = _origen - Main.screenPosition;
            Vector2 perp = new(-_dir.Y, _dir.X);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            var glowSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);

            if (!_liquidada)
            {
                // ========================================================
                //  1. EL HAZ — dibujado UNA sola vez: flash blanco-cian
                //     deslumbrante + aberración cromática convergente.
                // ========================================================
                float fade = MathHelper.Clamp(1f - _age / (float)HazTicks, 0f, 1f);
                if (fade > 0.01f)
                {
                    float conv = MathHelper.Clamp(_age / (float)HazTicks, 0f, 1f);
                    float off = (1f - conv) * 1.2f;   // las R/G/B convergen a 0

                    LumenLib.Ray(Main.spriteBatch, origen + perp * -off, _dir, Largo, 2.6f,
                        ColorRojo, 0.50f * fade, 0.8f);
                    LumenLib.Ray(Main.spriteBatch, origen, _dir, Largo, 2.6f,
                        ColorVerde, 0.50f * fade, 0.8f);
                    LumenLib.Ray(Main.spriteBatch, origen + perp * off, _dir, Largo, 2.6f,
                        ColorAzul, 0.50f * fade, 0.8f);
                    LumenLib.Ray(Main.spriteBatch, origen, _dir, Largo, 9f,
                        ColorCian, 0.95f * fade, time % 1f);
                    LumenLib.Ray(Main.spriteBatch, origen, _dir, Largo, 3f,
                        ColorBlanco, 0.90f * fade, time % 1f);
                }

                // ========================================================
                //  2. LOS CONTORNOS de las marcas (el juicio pendiente).
                // ========================================================
                for (int i = 0; i < _marcas.Count; i++)
                {
                    NPC npc = _marcas[i] >= 0 && _marcas[i] < Main.maxNPCs ? Main.npc[_marcas[i]] : null;
                    if (npc == null || !npc.active) continue;

                    Vector2 c = npc.Center - Main.screenPosition;
                    float w = npc.width * 0.5f + 5f;
                    float h = npc.height * 0.5f + 5f;
                    float pulso = 0.55f + 0.35f * MathF.Sin(time * 11f + i * 2.3f);
                    Color col = Color.Lerp(ColorCian, ColorBlanco,
                        0.5f + 0.5f * MathF.Sin(time * 9f + i));

                    // los 4 filos del contorno (quads finos).
                    DibujarFilo(c + Vector2.UnitX * w, 0f, h * 2f, col, pulso, glowSize);
                    DibujarFilo(c - Vector2.UnitX * w, 0f, h * 2f, col, pulso, glowSize);
                    DibujarFilo(c + Vector2.UnitY * h, MathHelper.PiOver2, w * 2f, col, pulso, glowSize);
                    DibujarFilo(c - Vector2.UnitY * h, MathHelper.PiOver2, w * 2f, col, pulso, glowSize);
                }
            }
            else
            {
                // ========================================================
                //  3. LOS DESTELLOS de la liquidación (el veredicto).
                // ========================================================
                for (int i = 0; i < _destellos.Count; i++)
                {
                    float pr = MathHelper.Clamp(_liqAge / PostTicks, 0f, 1f);
                    Vector2 p = _destellos[i] - Main.screenPosition;
                    OndaLib.Pulse(Main.spriteBatch, p, pr, 60f, ColorCian, 0.65f * (1f - pr), Seed + i * 11);
                    LumenLib.Bloom(Main.spriteBatch, p, 20f * (1f - pr) + 6f,
                        ColorBlanco, 0.6f * (1f - pr), 2);
                }
            }

            Main.spriteBatch.End();
        }

        /// <summary>Un filo del contorno: quad fino estirado.</summary>
        private void DibujarFilo(Vector2 pos, float rot, float largo, Color col, float a, Vector2 glowSize)
        {
            Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, Tint(col, a),
                rot, glowSize * 0.5f, new Vector2(largo, 2.2f) / glowSize, SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.7 — REVERSIÓN AL PREMULTIPLICADO (la sonda v6.50.3
            // estaba incompleta): el pipeline REAL premultiplica los PNG al
            // cargar (ReLogic PngReader.PreMultiplyAlpha, verificado en el
            // decompilado del tML 2026.07.3.0) y el AlphaBlend de FNA es
            // (One, InvSourceAlpha) — compositing PREMULTIPLICADO, donde el
            // RGB del tinte ES la intensidad. El tinte lineal dejaba el
            // color SIN escalar en los lotes de masa (bruma fantasma
            // saturada) y sobrealimentaba los aditivos hasta ×10 (destellos
            // que inundaban la pantalla). El (RGB·f, A·f) de v6.25 es el
            // correcto para AMBOS presets de FNA.
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
