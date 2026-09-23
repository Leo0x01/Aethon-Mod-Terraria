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
    /// FiloHorizonteProjectile — EL FILO DEL HORIZONTE.
    ///
    /// EL HORIZONTE DE SUCESOS portátil: un DISCO NEGRO girando muy
    /// rápido con un anillo de acreción fino ámbar-violeta y UNA línea
    /// de fotón en el ecuador. Sale disparado, CURVA su trayectoria
    /// suavemente hacia el enemigo más cercano (giro suave, no homing),
    /// ATRAVIESA a los enemigos (i-frames 10, daño ×1.0) perdiendo
    /// velocidad con cada golpe... y vuelve como bumerán al dueño.
    ///
    /// LA MARCA: cada enemigo golpeado queda marcado con un halo fino
    /// que se CONTRAE durante 30 ticks y entonces IMPLOSIONA: daño
    /// adicional ×0.7 + una atracción de 6 px hacia el punto del golpe
    /// (la atracción es server/SP; el daño, v6.50 — GolpeMotor). Si el
    /// enemigo muere antes, la marca se apaga
    /// en silencio.
    /// </summary>
    public class FiloHorizonteProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int VueloTicks = 52;    // ida antes de volver
        private const int Iframes = 10;       // por enemigo
        private const int MarcaTicks = 30;    // la marca tarda esto en implosionar

        // === LA FÍSICA ===
        private const float Radio = 32f;      // radio visual del disco
        private const float VelIni = 15f;     // px/tick de salida
        private const float CurvaMax = 0.032f;// rad/tick de curva (suave)
        private const float Giro = 0.55f;     // rad/tick de spin
        private const float PerdidaGolpe = 0.92f;

        // === LA PALETA (vacío + acreción) ===
        private static readonly Color ColorAmbar = new(255, 186, 92);
        private static readonly Color ColorVioleta = new(178, 110, 255);
        private static readonly Color ColorFoton = new(240, 220, 255);

        /// <summary>Una marca de implosión: cuándo se clavó y dónde.</summary>
        private struct Marca
        {
            public int Tick;
            public Vector2 Pos;
        }

        private float _age;
        private bool _nacio;
        private bool _volviendo;
        private float _sentido = 1f;

        /// <summary>i-frames por objetivo: whoAmI → tick del último golpe.</summary>
        private readonly Dictionary<int, int> _golpes = new();

        /// <summary>Las marcas vivas: whoAmI → (tick, punto del golpe).</summary>
        private readonly Dictionary<int, Marca> _marcas = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 71);

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _sentido = Projectile.ai[0] >= 0f ? 1f : -1f;
            }

            Projectile.rotation += Giro * _sentido;
            Lighting.AddLight(Projectile.Center, 0.30f, 0.22f, 0.44f);

            if (!_volviendo)
            {
                CurvarHaciaPresa();
                if (_age > VueloTicks || Projectile.velocity.LengthSquared() < 25f)
                    _volviendo = true;
            }
            else
            {
                if (VolverAlDueno()) return;
            }

            Golpear();
            ImplodarMarcas();
        }

        /// <summary>LA CURVA: giro suave (máx 0.032 rad/tick) hacia el enemigo más cercano.</summary>
        private void CurvarHaciaPresa()
        {
            NPC cercano = null;
            float mejor = 460f * 460f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < mejor)
                {
                    mejor = d;
                    cercano = npc;
                }
            }
            if (cercano == null) return;

            float deseado = (cercano.Center - Projectile.Center).ToRotation();
            float actual = Projectile.velocity.ToRotation();
            float delta = MathHelper.WrapAngle(deseado - actual);
            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Clamp(delta, -CurvaMax, CurvaMax));
        }

        /// <summary>LA VUELTA: acelera hacia el dueño; al llegar, muere (se puede relanzar).</summary>
        private bool VolverAlDueno()
        {
            Player p = Main.player[Projectile.owner];
            if (p != null && p.active && !p.dead)
            {
                Vector2 hacia = p.MountedCenter - Projectile.Center;
                if (hacia.LengthSquared() < 44f * 44f)
                {
                    Projectile.Kill();
                    return true;
                }
                if (hacia.LengthSquared() > 1f)
                    Projectile.velocity += Vector2.Normalize(hacia) * 0.7f;
                if (Projectile.velocity.LengthSquared() > 19f * 19f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 19f;
            }
            else if (_age > 220f)
            {
                Projectile.Kill();
                return true;
            }
            return false;
        }

        /// <summary>
        /// EL CORTE: atraviesa (i-frames 10, ×1.0), pierde velocidad con
        /// cada golpe y deja LA MARCA. La detección corre en todas las
        /// máquinas (para dibujar los halos); el daño va por
        /// v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
        /// on-hit y sync MP del propio motor).
        /// </summary>
        private void Golpear()
        {
            int dmg = Math.Max(1, Projectile.damage);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float alcance = Radio * 0.75f + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > alcance * alcance) continue;
                if (_golpes.TryGetValue(npc.whoAmI, out int u) && _age - u < Iframes) continue;

                _golpes[npc.whoAmI] = (int)_age;
                _marcas[npc.whoAmI] = new Marca { Tick = (int)_age, Pos = Projectile.Center };

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 4f, true);

                // ATRAVIESA perdiendo velocidad.
                Projectile.velocity *= PerdidaGolpe;
                Projectile.netUpdate = true;
            }
        }

        /// <summary>LA IMPLOSIÓN: a los 30 ticks, cada marca revienta (×0.7 + 6 px de arrastre).</summary>
        private void ImplodarMarcas()
        {
            if (_marcas.Count == 0) return;

            List<int> maduras = null;
            foreach (KeyValuePair<int, Marca> kv in _marcas)
            {
                if (_age - kv.Value.Tick >= MarcaTicks)
                {
                    maduras ??= new List<int>();
                    maduras.Add(kv.Key);
                }
            }
            if (maduras == null) return;

            foreach (int who in maduras)
            {
                Marca m = _marcas[who];
                _marcas.Remove(who);

                NPC npc = who >= 0 && who < Main.maxNPCs ? Main.npc[who] : null;
                if (npc == null || !npc.active || !VFXCore.EsObjetivo(npc))
                    continue;   // la marca se apaga en silencio

                {
                    // v6.50 — GolpeMotor (el cauce del motor: crítica real,
                    // varianza, on-hit y sync MP del propio motor).
                    int dmg = Math.Max(1, (int)(Projectile.damage * 0.7f));
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2f, true);

                    // LA ATRACCIÓN: 6 px hacia el punto del golpe (server/SP).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 hacia = m.Pos - npc.Center;
                        if (hacia.LengthSquared() > 0.01f)
                            npc.position += Vector2.Normalize(hacia) * 6f;
                    }
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    RiftLib.ChispasAnomalia(npc.Center, 6, PyraPalettes.VoidFire,
                        Seed + who * 7, out ParticleData[] motas);
                    if (motas != null)
                        for (int i = 0; i < motas.Length; i++)
                            ParticleManager.Spawn(motas[i]);
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

            try { DrawDisco(); }
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

        private void DrawDisco()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float spin = Projectile.rotation;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);

            // ============================================================
            //  1. EL VACÍO — el disco NEGRO (pase alfa: OCULUYE).
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            var orbSize = new Vector2(VFXCore.GlowOrb.Width, VFXCore.GlowOrb.Height);
            var glowSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);

            Main.spriteBatch.Draw(VFXCore.GlowOrb, drawPos, null,
                Tint(Color.Black, 0.92f * fade), 0f, orbSize * 0.5f,
                new Vector2(Radio * 2.1f, Radio * 2.1f) / orbSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                Tint(Color.Black, 0.40f * fade), 0f, glowSize * 0.5f,
                new Vector2(Radio * 3.4f, Radio * 3.4f) / glowSize, SpriteEffects.None, 0f);

            Main.spriteBatch.End();

            // ============================================================
            //  2. EL ANILLO DE ACRECIÓN + LA LÍNEA DE FOTÓN (aditivo).
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            var ringSize = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);

            // el anillo ÁMBAR, girando con el disco
            Main.spriteBatch.Draw(VFXCore.Ring, drawPos, null,
                Tint(ColorAmbar, 0.72f * fade), spin * 0.5f, ringSize * 0.5f,
                VFXCore.RingQuadSize(Radio * 1.26f) / ringSize, SpriteEffects.None, 0f);
            // el anillo VIOLETA, contrarrotando, más afuera y tenue
            Main.spriteBatch.Draw(VFXCore.Ring, drawPos, null,
                Tint(ColorVioleta, 0.34f * fade), -spin * 0.35f, ringSize * 0.5f,
                VFXCore.RingQuadSize(Radio * 1.5f) / ringSize, SpriteEffects.None, 0f);

            // LA LÍNEA DE FOTÓN: una sola, fina, en el ecuador del disco.
            if (Projectile.velocity.LengthSquared() > 0.5f)
            {
                Vector2 perp = Vector2.Normalize(new Vector2(-Projectile.velocity.Y, Projectile.velocity.X));
                Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                    Tint(ColorFoton, 0.90f * fade), perp.ToRotation(), glowSize * 0.5f,
                    new Vector2(Radio * 2.5f, 1.9f) / glowSize, SpriteEffects.None, 0f);
            }

            // el arrastre violeta detrás del disco
            if (Projectile.velocity.LengthSquared() > 1f)
                LumenLib.Ray(Main.spriteBatch, drawPos, -Vector2.Normalize(Projectile.velocity),
                    Radio * 1.7f, Radio * 0.55f, ColorVioleta, 0.22f * fade, 0.8f);

            // el corazón brillante del horizonte
            LumenLib.Bloom(Main.spriteBatch, drawPos, Radio * 0.7f, ColorVioleta, 0.30f * fade, 2);

            // LAS MARCAS: halos finos que se contraen hasta la implosión.
            foreach (KeyValuePair<int, Marca> kv in _marcas)
            {
                NPC npc = kv.Key >= 0 && kv.Key < Main.maxNPCs ? Main.npc[kv.Key] : null;
                if (npc == null || !npc.active) continue;
                float t = MathHelper.Clamp((_age - kv.Value.Tick) / (float)MarcaTicks, 0f, 1f);
                float r = MathHelper.Lerp(46f, 8f, t);
                Vector2 p = npc.Center - Main.screenPosition;
                Main.spriteBatch.Draw(VFXCore.Ring, p, null,
                    Tint(t > 0.75f ? ColorFoton : ColorAmbar, (0.35f + 0.4f * t) * fade),
                    time * 3f + kv.Key, ringSize * 0.5f,
                    VFXCore.RingQuadSize(r) / ringSize, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
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
