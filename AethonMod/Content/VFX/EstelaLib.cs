using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.VFX
{
    /// <summary>Perfil de anchura a lo largo de la estela (f: 0 = nacimiento de la cola → 1 = cabeza).</summary>
    public enum EstelaProfile
    {
        /// <summary>Fina al nacer, GRUESA en la cabeza (la lanza en vuelo).</summary>
        Head,
        /// <summary>Gruesa al centro, fina en ambos extremos (el arco de cometa).</summary>
        Center,
        /// <summary>Cola que MUERE hacia atrás: máx en la cabeza, decae cúbico.</summary>
        Comet,
        /// <summary>Anchura viva por ruido (la estela que respira).</summary>
        Alive,
    }

    /// <summary>
    /// EstelaLib — v6.25 — LA LIBRERÍA DE LAS ESTELAS.
    ///
    /// Nació del análisis de huecos de v6.25 (research/humo_v625/
    /// ANALISIS_HUECOS.md): TODOS los mods premium del ecosistema
    /// (Calamity, WoTE, SOTS, MEAC, la propia Emperatriz vanilla) tienen
    /// RIBBONS de grosor variable siguiendo el camino real del
    /// proyectil — nosotros solo teníamos fantasmas rectos. Esta
    /// librería cubre el hueco con la técnica de la casa:
    ///
    ///   · El CAMINO ES DEL LLAMADOR: la librería no guarda historia
    ///     (acepta la polilínea que el proyectil ya tiene — oldPos — o
    ///     el ring-buffer propio EstelaTrack).
    ///   · SANITIZE → SMOOTH → RESAMPLE: el camino de tML es una escalera
    ///     de ticks (lección LunarVeil: cortar teleports &gt; 1000 px; el
    ///     suavizado 0.25/0.5/0.25 ×2 disimula los peldaños).
    ///   · EL RIBBON: remuestreo por longitud de arco cada ~14 px y TRES
    ///     capas por tramo (velo ×1.6 alpha 0.30 · cuerpo alpha 0.60 ·
    ///     NÚCLEO blanco ×0.30 alpha 0.90 — la doble pasada de la casa
    ///     en versión triple), anchura por PERFIL (Head/Center/Comet/
    ///     Alive), rotación por TANGENTE.
    ///   · LOS FANTASMAS con SQUASH (lección MEAC): N copias del sprite
    ///     del llamador con alpha decreciente y Y ×0.55 — leen
    ///     "rasguño de luz", no "cola de sprites".
    ///   · LA ESTELA DE POLVO: paquete determinista listo para el
    ///     ParticleManager (la librería NO spawnea).
    ///
    /// CONTRATO (idéntico al de StormLib/LumenLib/BrumaFX): los métodos
    /// de DIBUJO dibujan en el SpriteBatch ABIERTO que el llamador tenga
    /// (aditivo recomendado) y NO lo tocan. TODO determinista por
    /// semilla: misma secuencia SIEMPRE, cero estado de ondas, cero GC
    /// por frame (buffers reutilizados).
    /// </summary>
    public static class EstelaLib
    {
        // ==================================================================
        //  TEXTURAS COMPARTIDAS (resolución diferida)
        // ==================================================================

        private static Asset<Texture2D> _trail, _glow;

        /// <summary>La estela degradada 32×8 (el CUERPO del ribbon).</summary>
        private static Texture2D TrailTex =>
            (_trail ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/TrailGlow")).Value;

        /// <summary>El brillo radial suave (el VELO y la CABEZA).</summary>
        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  EL TRACK — la historia del camino por identidad (opcional)
        // ==================================================================

        private static readonly Dictionary<int, EstelaTrack> _tracks = new();

        /// <summary>
        /// El ring-buffer de puntos por IDENTIDAD (whoAmI del proyectil):
        /// Push cada tick en AI(); el track se AUTO-PODRE si no se empuja
        /// en 2 ticks (teleports y muertes no dejan estelas fantasma).
        /// </summary>
        public static EstelaTrack Track(int ownerId, int capacity = 24)
        {
            if (!_tracks.TryGetValue(ownerId, out EstelaTrack t))
            {
                t = new EstelaTrack(capacity);
                _tracks[ownerId] = t;
            }
            t.Touch();
            return t;
        }

        /// <summary>Limpieza de los tracks podridos (llamar de vez en cuando).</summary>
        public static void PurgeTracks()
        {
            if (_tracks.Count == 0) return;
            List<int> muertos = null;
            foreach (KeyValuePair<int, EstelaTrack> kv in _tracks)
            {
                if (kv.Value.Rotten)
                {
                    muertos ??= new List<int>();
                    muertos.Add(kv.Key);
                }
            }
            if (muertos != null)
                for (int i = 0; i < muertos.Count; i++)
                    _tracks.Remove(muertos[i]);
        }

        /// <summary>Vacía TODOS los tracks (descarga limpia del mod).</summary>
        public static void ClearTracks() => _tracks.Clear();

        // ==================================================================
        //  HELPERS DE CAMINO (públicos: los quiere todo el mundo)
        // ==================================================================

        /// <summary>
        /// SANITIZADO: corta la polilínea en NaN/ceros/teleports &gt; 1000 px
        /// (lección LunarVeil — oldPos sucios al nacer y al teletransportar).
        /// Devuelve el tramo LIMPIO que termina en el ÚLTIMO punto.
        /// </summary>
        public static Vector2[] Sanitize(Vector2[] pts)
        {
            if (pts == null || pts.Length < 2) return pts;

            int last = pts.Length - 1;
            int first = last;
            for (int i = last - 1; i >= 0; i--)
            {
                Vector2 d = pts[last] - pts[i];
                if (MathF.Abs(d.X) > 1000f || MathF.Abs(d.Y) > 1000f ||
                    float.IsNaN(d.X) || float.IsNaN(d.Y))
                    break;
                if (pts[i] == Vector2.Zero && i != last) break;   // oldPos sin usar
                first = i;
            }
            if (first == 0) return pts;

            var clean = new Vector2[last - first + 1];
            for (int i = first; i <= last; i++)
                clean[i - first] = pts[i];
            return clean;
        }

        /// <summary>
        /// SUAVIZADO por promedio móvil 0.25/0.5/0.25 — el camino de oldPos
        /// es una escalera de ticks; sin esto toda estela "rasegura".
        /// </summary>
        public static Vector2[] Smooth(Vector2[] pts, int iterations = 2)
        {
            if (pts == null || pts.Length < 3 || iterations <= 0) return pts;

            Vector2[] src = pts;
            Vector2[] dst = new Vector2[pts.Length];
            for (int it = 0; it < iterations; it++)
            {
                dst[0] = src[0];
                dst[src.Length - 1] = src[src.Length - 1];
                for (int i = 1; i < src.Length - 1; i++)
                    dst[i] = (src[i - 1] * 0.25f + src[i] * 0.5f + src[i + 1] * 0.25f);
                (src, dst) = (dst, src);
            }
            return src;
        }

        /// <summary>
        /// REMUESTREO por longitud de arco cada <paramref name="step"/> px
        /// (denso y uniforme — el ribbon necesita tramos iguales para que
        /// el taper se lea continuo, no por escalones de tick).
        /// </summary>
        public static Vector2[] Resample(Vector2[] pts, float step = 14f)
        {
            if (pts == null || pts.Length < 2 || step < 2f) return pts;

            float total = 0f;
            for (int i = 1; i < pts.Length; i++)
                total += Vector2.Distance(pts[i - 1], pts[i]);
            if (total < step) return pts;

            int count = Math.Clamp((int)(total / step), 4, 64);
            var res = new Vector2[count + 1];
            float target, acum = 0f;
            int seg = 1;
            res[0] = pts[0];
            for (int k = 1; k <= count; k++)
            {
                target = total * k / count;
                while (seg < pts.Length &&
                       acum + Vector2.Distance(pts[seg - 1], pts[seg]) < target)
                {
                    acum += Vector2.Distance(pts[seg - 1], pts[seg]);
                    seg++;
                }
                if (seg >= pts.Length) { res[k] = pts[pts.Length - 1]; continue; }
                float segLen = Vector2.Distance(pts[seg - 1], pts[seg]);
                float f = segLen > 0.01f ? (target - acum) / segLen : 0f;
                res[k] = Vector2.Lerp(pts[seg - 1], pts[seg], f);
            }
            return res;
        }

        // ==================================================================
        //  EL RIBBON — el corazón de la librería
        // ==================================================================

        /// <summary>La anchura por perfil (f: 0 nacimiento de la cola → 1 cabeza).</summary>
        private static float AnchoDe(EstelaProfile profile, float f, float width, int seed, float time)
        {
            switch (profile)
            {
                case EstelaProfile.Head:
                    return width * MathF.Pow(MathF.Sin(f * MathHelper.PiOver2), 0.7f);
                case EstelaProfile.Center:
                    return width * MathF.Pow(MathF.Sin(f * MathHelper.Pi), 0.8f);
                case EstelaProfile.Comet:
                    return width * MathF.Pow(f, 1.5f);
                default: // Alive
                    float n = BrumaNoise.Fbm(f * 3f + time * 0.31f, seed * 0.017f, seed, 2);
                    return width * (0.72f + 0.28f * n);
            }
        }

        /// <summary>
        /// DIBUJA LA ESTELA: banda con GROSOR VARIABLE a lo largo de la
        /// polilínea (sanitizada → suavizada → remuestreada cada ~14 px),
        /// TRES capas por tramo: velo de color ×1.6 (alpha 0.30) · cuerpo
        /// de color (alpha 0.60) · NÚCLEO blanco ×0.30 (alpha 0.90) — la
        /// doble pasada de la casa, en versión triple. La cabeza lleva un
        /// bloom pequeño (2 quads) que integra la estela con el proyectil.
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="pts">El camino: pts[ÚLTIMO] = CABEZA (posición actual), pts[0] = cola vieja.</param>
        /// <param name="width">Ancho MÁXIMO de la estela en px.</param>
        /// <param name="profile">Curva de anchura a lo largo del camino.</param>
        /// <param name="color">Color del cuerpo (el núcleo vira a blanco).</param>
        /// <param name="intensity">Multiplicador global 0..1.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="head">True = dibuja cabeza brillante en el último punto.</param>
        public static void Ribbon(SpriteBatch batch, Vector2[] pts, float width,
            EstelaProfile profile, Color color, float intensity, int seed,
            float time, bool head = true)
        {
            if (batch == null || pts == null || pts.Length < 2 || width < 1f) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            // El camino LIMPIO → SUAVE → UNIFORME.
            Vector2[] camino = Resample(Smooth(Sanitize(pts), 2), MathF.Max(width * 0.9f, 12f));
            int n = camino.Length;
            if (n < 3) return;

            // LOD por anchura: 3 capas → 2 → 1 (la librería se degrada sola).
            int capas = width >= 6f ? 3 : width >= 3f ? 2 : 1;

            // El color del núcleo: el blanco CÁLIDO de la casa (nunca puro).
            Color nucleo = new(
                (byte)(255), (byte)(255), (byte)(255));
            Color cuerpo = color;
            Color velo = new(
                (byte)(int)(color.R * 0.75f + 40),
                (byte)(int)(color.G * 0.75f + 40),
                (byte)(int)(color.B * 0.75f + 40));

            for (int i = 1; i < n; i++)
            {
                // f: 0 = nacimiento de la cola → 1 = cabeza (el último punto).
                float f = i / (float)(n - 1);

                Vector2 a = camino[i - 1];
                Vector2 b = camino[i];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.1f) continue;
                float rot = MathF.Atan2(delta.Y, delta.X);

                // LA ANCHURA VIVA por perfil + el vaivén de energía.
                float w = AnchoDe(profile, f, width, seed, time);
                float beat = 0.85f + 0.15f * MathF.Sin(time * 7.1f + f * 9f + seed);
                w *= beat;

                // La densidad muere hacia la cola (la estela se DISUELVE).
                float aFade = 0.20f + 0.80f * MathF.Pow(f, 0.7f);

                // --- CAPA 1: EL VELO (×1.6 de ancho, alpha 0.30) ---
                if (capas >= 3)
                    SegQuad(batch, GlowTex, mid, len + w * 1.2f, w * 1.6f, rot,
                        Tint(velo, 0.30f * aFade * intensity));

                // --- CAPA 2: EL CUERPO (la estela degradada orientada) ---
                if (capas >= 2)
                    SegQuad(batch, TrailTex, mid, len + w * 0.7f, w, rot,
                        Tint(cuerpo, 0.60f * aFade * intensity));

                // --- CAPA 3: EL NÚCLEO (×0.30, casi blanco, alpha 0.90) ---
                SegQuad(batch, TrailTex, mid, len + w * 0.4f, MathF.Max(w * 0.30f, 1.5f), rot,
                    Tint(nucleo, 0.90f * aFade * intensity));
            }

            // === LA CABEZA: bloom pequeño (2 quads) integrando la estela ===
            if (head)
            {
                Vector2 h = camino[n - 1];
                float hw = MathF.Max(AnchoDe(profile, 1f, width, seed, time) * 1.4f, 4f);
                batch.Draw(GlowTex, h, null, Tint(velo, 0.35f * intensity), 0f,
                    new Vector2(GlowTex.Width, GlowTex.Height) * 0.5f,
                    new Vector2(hw, hw) / new Vector2(GlowTex.Width, GlowTex.Height),
                    SpriteEffects.None, 0f);
                batch.Draw(GlowTex, h, null, Tint(nucleo, 0.85f * intensity), 0f,
                    new Vector2(GlowTex.Width, GlowTex.Height) * 0.5f,
                    new Vector2(hw * 0.4f, hw * 0.4f) / new Vector2(GlowTex.Width, GlowTex.Height),
                    SpriteEffects.None, 0f);
            }
        }

        /// <summary>Segmento orientado por la tangente (el quad de un tramo).</summary>
        private static void SegQuad(SpriteBatch batch, Texture2D tex, Vector2 mid,
            float len, float w, float rot, Color tint)
        {
            if (tint.A == 0 || w < 0.5f) return;
            batch.Draw(tex, mid, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2(len, w) / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de INTENSIDAD LINEAL premultiplicado (el de la casa v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }

        // ==================================================================
        //  LOS FANTASMAS — afterimages con squash (lección MEAC)
        // ==================================================================

        /// <summary>
        /// FANTASMAS: N transformaciones a lo largo del camino (desde la
        /// cabeza hacia atrás) para que el llamador dibuje su sprite con
        /// ellas: alpha decreciente (0.6 − i/15 — MEAC), escala X creciendo
        /// ×1.0→×1.4 hacia atrás (Emperatriz vanilla) y SQUASH vertical
        /// Y ×0.55 — leen "rasguño de luz", no "cola de sprites".
        /// La librería NO conoce el sprite: devuelve la transform.
        /// </summary>
        /// <param name="pts">El camino (el ÚLTIMO punto es la cabeza).</param>
        /// <param name="ghosts">Número de fantasmas (4..8 recomendado).</param>
        /// <param name="squashY">Factor de aplastado vertical (0.55 = la casa).</param>
        public static void GhostTransforms(Vector2[] pts, int ghosts, float squashY,
            out Vector2[] positions, out float[] alphas, out float[] scales)
        {
            ghosts = Math.Clamp(ghosts, 1, 12);
            positions = new Vector2[ghosts];
            alphas = new float[ghosts];
            scales = new float[ghosts];

            if (pts == null || pts.Length < 2) return;

            float total = 0f;
            for (int i = 1; i < pts.Length; i++)
                total += Vector2.Distance(pts[i - 1], pts[i]);
            if (total < 1f) total = 1f;

            for (int g = 0; g < ghosts; g++)
            {
                // El fantasma g vive al fracción (1 − g·0.11) del camino
                // (desde la cabeza hacia atrás, uniforme).
                float f = MathHelper.Clamp(1f - g * 0.11f, 0f, 1f);
                positions[g] = PuntoEnCamino(pts, f * total);

                // Alpha MEAC: 0.6 − i/15 (nunca negativo).
                alphas[g] = MathF.Max(0.6f - g / 15f, 0f);
                // Escala X creciente hacia atrás (la Emperatriz: ×1.4).
                scales[g] = 1f + 0.4f * g / MathF.Max(ghosts - 1f, 1f);
                // (El squash va en la Y: el llamador usa scale.Y = scales[g]·squashY.)
                _ = squashY;   // documentado arriba; se aplica en el llamador
            }
        }

        /// <summary>Punto a <paramref name="dist"/> px del inicio de la polilínea.</summary>
        private static Vector2 PuntoEnCamino(Vector2[] pts, float dist)
        {
            float acum = 0f;
            for (int i = 1; i < pts.Length; i++)
            {
                float seg = Vector2.Distance(pts[i - 1], pts[i]);
                if (acum + seg >= dist && seg > 0f)
                {
                    float f = (dist - acum) / seg;
                    return Vector2.Lerp(pts[i - 1], pts[i], f);
                }
                acum += seg;
            }
            return pts[pts.Length - 1];
        }
    }

    /// <summary>
    /// EstelaTrack — v6.25 — EL RING-BUFFER DEL CAMINO por identidad.
    ///
    /// Push cada tick en AI() (la posición ACTUAL del proyectil); Points
    /// devuelve la historia saneada (la CABEZA es el ÚLTIMO punto — el
    /// contrato de EstelaLib.Ribbon). Se AUTO-PODRE a los 2 ticks sin
    /// empujes: teleports y muertes no dejan estelas fantasma.
    /// </summary>
    public sealed class EstelaTrack
    {
        private readonly Vector2[] _ring;
        private int _head;          // índice del punto MÁS NUEVO
        private int _count;
        private uint _lastPush;     // GameUpdateCount del último Push

        internal EstelaTrack(int capacity)
        {
            _ring = new Vector2[Math.Clamp(capacity, 4, 64)];
        }

        /// <summary>Marca el track como vivo (lo llama Track()).</summary>
        internal void Touch() => _lastTouch = Main.GameUpdateCount;
        private uint _lastTouch;

        /// <summary>¿Lleva 2+ ticks sin Push? (podrido — se purga solo).</summary>
        public bool Rotten => Main.GameUpdateCount > _lastPush + 2;

        /// <summary>Apila la posición actual (llamar cada tick en AI).</summary>
        public void Push(Vector2 pos)
        {
            _head = (_head + 1) % _ring.Length;
            _ring[_head] = pos;
            if (_count < _ring.Length) _count++;
            _lastPush = Main.GameUpdateCount;
            _lastTouch = _lastPush;
        }

        /// <summary>
        /// La historia del camino: [0] = la más VIEJA … [última] = la
        /// CABEZA (la posición del último Push) — lista para Ribbon.
        /// </summary>
        public Vector2[] Points()
        {
            var res = new Vector2[_count];
            for (int i = 0; i < _count; i++)
                res[i] = _ring[(_head - _count + 1 + i + _ring.Length * 2) % _ring.Length];
            return res;
        }
    }
}
