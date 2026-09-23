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
    /// TelarEstrellaProjectile — EL TELAR DE CONSTELACIONES (la estrellita clavada).
    ///
    /// Cada estrella vive QUIETA 480 ticks donde la clavó el cursor (el
    /// punto viaja en ai[0..1]) con su color propio por semilla (4 puntas).
    /// LAS LÍNEAS: cada estrella dibuja su hilo fino de luz hacia la
    /// SIGUIENTE estrella clavada (orden por edad: timeLeft ascendente) —
    /// la constelación se DIBUJA sola, cerrándose (N → 1).
    ///
    /// EL ENCENDIDO (cada 30 ticks, con ≥4 estrellas): el polígono cerrado
    /// parpadea blanco y TODO enemigo DENTRO (ray-casting clásico
    /// punto-en-polígono) recibe daño ×2.2 POR ESTRELLA de la figura +
    /// destello en cada vértice. Solo la estrella LÍDER (la más vieja)
    /// golpea: un solo veredicto por encendido.
    ///
    /// Las estrellas se APAGAN EN ORDEN (la más vieja primero) y la 7ª
    /// reemplaza a la más vieja (lo resuelve el ítem).
    /// </summary>
    public class TelarEstrellaProjectile : ModProjectile
    {
        /// <summary>Vida de cada estrellita (ticks).</summary>
        private const int VidaTicks = 480;

        /// <summary>Cadencia del ENCENDIDO de la figura (ticks).</summary>
        private const int CadenciaEncendido = 30;

        /// <summary>Estrellas mínimas para ENCENDER la figura.</summary>
        private const int MinimasFigura = 4;

        /// <summary>Daño del encendido POR ESTRELLA de la figura.</summary>
        private const float DañoEncendido = 2.2f;

        private static readonly Color ColorHilo = new(210, 235, 255);
        private static readonly Color ColorBlanco = new(255, 255, 255);

        /// <summary>
        /// v6.50.2 — FIX (alocación por frame): el búfer de estrellas se
        /// REUTILIZA (el patrón de la casa — _clavesPicotazo de
        /// MetronomoPulsarHalo): EstrellasDe() construía y ordenaba un List
        /// NUEVO por estrella por tick (la AI y el hilo de cada estrella —
        /// O(N²) + GC cada frame). Estático y despejado por uso: la AI corre
        /// de una en una y el render nunca solapa con el update. Los
        /// llamadores lo consumen íntegro ANTES de la siguiente llamada.
        /// </summary>
        private static readonly List<Projectile> _bufEstrellas = new List<Projectile>(16);

        private float _age;
        private bool _nacio;
        private Vector2 _target;
        private Color _color = Color.White;

        /// <summary>El destello del encendido (decae).</summary>
        private float _flash;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            // Daño por sub-ataques — v6.50 — GolpeMotor (el cauce del
            // motor): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 79);

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _target = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                if (_target == Vector2.Zero) _target = Projectile.Center;
                Projectile.Center = _target;   // CLAVADA EXACTA donde estaba el cursor
                Projectile.velocity = Vector2.Zero;

                // EL COLOR PROPIO: hue por semilla (determinista, sin Main.rand).
                _color = LumenLib.Hue(VFXCore.Hash01(Seed, 5, 9) * 0.86f, 0.62f, 0.92f);
            }

            Projectile.velocity = Vector2.Zero;

            // La luz propia de la estrellita.
            float fade = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
            Lighting.AddLight(Projectile.Center,
                _color.R / 255f * 0.35f * fade + 0.05f,
                _color.G / 255f * 0.35f * fade + 0.05f,
                _color.B / 255f * 0.35f * fade + 0.08f);

            // El destello del encendido se apaga.
            if (_flash > 0.003f) _flash *= 0.86f;
            else _flash = 0f;

            // ============================================================
            //  EL ENCENDIDO: cada 30 ticks, con ≥4 estrellas. TODAS se
            //  enciencen (visual); solo la LÍDER (la más vieja) golpea.
            // ============================================================
            long reloj = (long)Main.GameUpdateCount;
            List<Projectile> estrellas = EstrellasDe(Projectile.owner);

            if (estrellas.Count >= MinimasFigura && reloj % CadenciaEncendido == 0)
            {
                _flash = 1f;
                bool lider = estrellas[0].whoAmI == Projectile.whoAmI;
                if (lider)
                    Encender(estrellas);
            }
        }

        /// <summary>
        /// EL VEREDICTO DEL POLÍGONO: todo enemigo DENTRO de la figura
        /// cerrada recibe daño ×2.2 por estrella. v6.50 — GolpeMotor (el
        /// cauce del motor: crítica real, varianza, on-hit y sync MP del
        /// propio motor) + ray-casting.
        /// </summary>
        private void Encender(List<Projectile> estrellas)
        {
            // La figura: los vértices en orden de clavado (viejo → nuevo), cerrada.
            Vector2[] poligono = new Vector2[estrellas.Count];
            for (int i = 0; i < estrellas.Count; i++)
                poligono[i] = estrellas[i].Center;

            int dmg = Math.Max(1, (int)(Projectile.damage * DañoEncendido * estrellas.Count));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (!PuntoEnPoligono(npc.Center, poligono)) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 4f, true);
            }
        }

        /// <summary>
        /// LAS ESTRELLAS DEL TEJEDOR, ordenadas por EDAD (timeLeft
        /// ascendente: la más vieja primero — el orden de clavado).
        /// v6.50.2 — FIX: devuelve el BÚFER ESTÁTICO reutilizado (Clear +
        /// relleno + Sort por llamada — cero GC). El delegado del Sort no
        /// captura nada → el compilador lo cachea (no se aloca por llamada);
        /// sin LINQ.
        /// </summary>
        public static List<Projectile> EstrellasDe(int owner)
        {
            _bufEstrellas.Clear();
            int tipo = ModContent.ProjectileType<TelarEstrellaProjectile>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != owner)
                    continue;
                _bufEstrellas.Add(p);
            }

            _bufEstrellas.Sort((a, b) => a.timeLeft.CompareTo(b.timeLeft));
            return _bufEstrellas;
        }

        /// <summary>
        /// EL RAY-CASTING CLÁSICO punto-en-polígono: cruza la semirrecta
        /// horizontal con cada arista y cuenta los cortes — impar = dentro.
        /// El polígono se toma CERRADO (último → primer vértice).
        /// </summary>
        private static bool PuntoEnPoligono(Vector2 p, Vector2[] poly)
        {
            if (poly == null || poly.Length < 3) return false;

            bool dentro = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                // ¿La arista cruza la altura del punto?
                if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                    p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y)
                         / (poly[j].Y - poly[i].Y) + poly[i].X)
                {
                    dentro = !dentro;
                }
            }
            return dentro;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // La estrellita se despide con su polvo (determinista).
            RiftLib.ChispasAnomalia(Projectile.Center, 4,
                new Color[] { _color, ColorHilo }, Seed + 6, out ParticleData[] motas);
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

            try { DrawEstrella(); }
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

        private void DrawEstrella()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Nace con un pop pequeño y se APAGA suave al final (en orden).
            float nacer = MathHelper.Clamp(_age / 5f, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f) * nacer;
            float titila = 0.85f + 0.15f * MathF.Sin(time * 4.2f + seed);

            float R = 17f * (1f + 0.07f * MathF.Sin(time * 3f + seed)) * nacer;
            float rot = time * 0.5f + seed * 0.7f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // ============================================================
            //  EL HILO DE LUZ: mi línea fina hacia la SIGUIENTE estrella
            //  clavada (el orden cierra la figura: N → 1).
            // ============================================================
            List<Projectile> estrellas = EstrellasDe(Projectile.owner);
            if (estrellas.Count >= 2)
            {
                int miIndice = -1;
                for (int i = 0; i < estrellas.Count; i++)
                    if (estrellas[i].whoAmI == Projectile.whoAmI) { miIndice = i; break; }

                if (miIndice >= 0)
                {
                    Projectile siguiente = estrellas[(miIndice + 1) % estrellas.Count];
                    if (siguiente.whoAmI != Projectile.whoAmI)
                    {
                        Vector2 a = drawPos;
                        Vector2 b = siguiente.Center - Main.screenPosition;
                        Color hilo = Color.Lerp(_color, ColorHilo, 0.5f);

                        Seg(Main.spriteBatch, a, b, 3.2f, Tint(hilo, 0.32f * fade));

                        // EL ENCENDIDO: el hilo PARPADEA BLANCO.
                        if (_flash > 0.02f)
                            Seg(Main.spriteBatch, a, b, 5.5f, Tint(ColorBlanco, 0.85f * _flash * fade));
                    }
                }
            }

            // ============================================================
            //  LA ESTRELLITA DE 4 PUNTAS: dos cruces estiradas + núcleo.
            // ============================================================
            var texSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);
            var punta = new Vector2(R * 2.6f, R * 0.34f) / texSize;
            var nucleo = new Vector2(R * 0.85f, R * 0.85f) / texSize;

            Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                Tint(_color, 0.55f * titila * fade), rot, texSize * 0.5f,
                punta, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                Tint(_color, 0.55f * titila * fade), rot + MathHelper.PiOver2, texSize * 0.5f,
                punta, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                Tint(ColorBlanco, 0.85f * titila * fade), 0f, texSize * 0.5f,
                nucleo, SpriteEffects.None, 0f);

            // EL DESTELLO DEL VÉRTICE al encenderse la figura.
            if (_flash > 0.05f)
                StormLib.ImpactFlash(Main.spriteBatch, drawPos, 40f,
                    ColorBlanco, 0.70f * _flash * fade, time * 3f + seed);

            Main.spriteBatch.End();
        }

        /// <summary>Segmento de luz (el hilo del telar).</summary>
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
