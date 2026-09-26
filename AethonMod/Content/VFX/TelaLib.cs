using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// TelaLib — v6.31 — LA SUPER LIBRERÍA DE LAS CINTAS: "una cinta para
    /// gobernarlas todas".
    ///
    /// Nace de la super investigación (INFORME_TOP_MODS_LIBRERIAS §C.2) y
    /// reúne LAS TRES PIEZAS que ninguna librería pública combina:
    ///
    ///   1. · FASE ANCLADA A ARCO MUNDIAL: la textura de la cinta avanza con
    ///        los PÍXELES RECORRIDOS, no con el tiempo — al girar, la estela
    ///        NO resbala (el patinaje visual que delata a las cintas baratas).
    ///   2. · ANCHURA POR VÉRTICE CON MESETA: ancho 100% en el 80% del largo
    ///        + tapas redondas (la lección medida del desgarro v6.31); el
    ///        latido vive en la ALPHA, jamás en el ancho.
    ///   3. · SOLAPE ADAPTATIVO POR GIRO REAL: cada segmento solo se alarga
    ///        w/2·tan(δ/2) — la luz aditiva NO se apila en cuentas brillantes
    ///        (medido por el mock del desgarro).
    ///
    /// Además: POOL de cintas (cero GC por disparo), SUB-STEP AWARE (los
    /// proyectiles con extraUpdates empujan una vez por TICK de render, no
    /// por sub-paso) y determinismo MP total (la forma la pone el dueño).
    ///
    /// Uso:
    /// <code>
    ///   Cinta c = Cinta.Adquirir(Projectile.whoAmI, 24);
    ///   c.Empujar(Projectile.Center);
    ///   // en el dibujo:
    ///   c.Dibujar(batch, new CintaSpec { ... });
    ///   // al morir:
    ///   Cinta.Soltar(Projectile.whoAmI);
    /// </code>
    /// CONTRATO DE BATCH: Dibujar pinta en el batch ABIERTO que le pasen
    /// (aditivo recomendado) y NUNCA lo abre/cierra.
    /// </summary>
    public sealed class Cinta
    {
        // ==================================================================
        //  EL POOL (por dueño: whoAmI → cinta viva)
        // ==================================================================

        private static readonly Dictionary<int, Cinta> _vivas = new Dictionary<int, Cinta>(16);
        private static readonly List<Cinta> _libres = new List<Cinta>(8);

        // v6.49 — EL BARRENDERO (hallazgo AUD-A): _vivas solo vaciaba con
        // Soltar manual — una muerte sin OnKill (despawn de red,
        // active=false por sincronización) dejaba la cinta viva PARA
        // SIEMPRE y el whoAmI reciclado HEREDABA la estela ajena. La
        // cinta ahora ROTTEN a los 3 ticks sin empuje (el mismo contrato
        // que EstelaTrack.PurgeTracks) y Purgar() se lleva a los muertos
        // al pool — lo llama un ModSystem (BrumaSystem) cada 120 ticks.
        private uint _últimoEmpujeAbsoluto;
        private static uint _tickUltimaPurga;

        /// <summary>
        /// v6.49 — LA PURGA PERIÓDICA: las cintas cuyo dueño lleva más de
        /// 3 ticks sin empujar están huérfanas (muerte sin OnKill, despawn
        // de red) — vuelven al pool. Barato: solo cuando hay vivas.
        /// </summary>
        public static void Purgar()
        {
            try
            {
                if (_vivas.Count == 0) return;
                uint ahora = Main.GameUpdateCount;
                // claves a liberar (el diccionario no se muta mientras se
                // enumera: se apuntan primero).
                _purgarBuffer.Clear();
                foreach (var par in _vivas)
                    if (ahora - par.Value._últimoEmpujeAbsoluto > 3u)
                        _purgarBuffer.Add(par.Key);
                for (int i = 0; i < _purgarBuffer.Count; i++)
                    Soltar(_purgarBuffer[i]);
            }
            catch { }
        }
        private static readonly List<int> _purgarBuffer = new List<int>(8);

        /// <summary>Tick de la purga (lo llama BrumaSystem con su PostUpdate).</summary>
        internal static void TickPurga()
        {
            try
            {
                // v6.50.2 — FIX: la purga corre CADA 120 TICKS, no cada tick —
                // el `GameUpdateCount % 120` viejo cambia de valor CADA tick,
                // así que la comparación con _tickUltimaPurga casi siempre
                // daba distinto y Purgar() corría TODOS los ticks (el "cada
                // 120" del documento mentía; el coste era trivial pero el
                // contrato era cada 120). Ahora solo en el tick múltiplo, y
                // el reloj absoluto evita la doble purga si un frame llama
                // dos veces (sub-pasos).
                uint ahora = Main.GameUpdateCount;
                if (ahora % 120u != 0u) return;      // solo el tick múltiplo
                if (ahora == _tickUltimaPurga) return; // ya purgada en ESTE tick
                _tickUltimaPurga = ahora;
                Purgar();
            }
            catch { }
        }

        /// <summary>
        /// ADQUIERE (o reutiliza del pool) la cinta de este dueño con la
        /// capacidad pedida. Reutiliza la existente si ya tenía una (cambia
        /// la capacidad conservando el historial que quepa).
        /// </summary>
        public static Cinta Adquirir(int dueñoId, int capacidad = 24)
        {
            if (_vivas.TryGetValue(dueñoId, out Cinta viva))
            {
                viva.Redimensionar(capacidad);
                return viva;
            }
            Cinta c = _libres.Count > 0 ? _libres[_libres.Count - 1] : new Cinta();
            if (_libres.Count > 0) _libres.RemoveAt(_libres.Count - 1);
            c._dueño = dueñoId;
            c.Redimensionar(capacidad);
            _vivas[dueñoId] = c;
            return c;
        }

        /// <summary>La cinta viva de este dueño (null si no tiene).</summary>
        public static Cinta De(int dueñoId)
            => _vivas.TryGetValue(dueñoId, out Cinta c) ? c : null;

        /// <summary>DEVUELVE la cinta del dueño al pool (al morir el proyectil).</summary>
        public static void Soltar(int dueñoId)
        {
            if (_vivas.TryGetValue(dueñoId, out Cinta c))
            {
                _vivas.Remove(dueñoId);
                c._n = 0;
                c._arcoAcumulado = 0f;
                // v6.49 — el reloj también se suelta: una re-adquisición del
                // MISMO índice en el mismo frame no pierde su primer push
                // por el path sub-step.
                c._últimoFrameEmpujado = 0u;
                c._últimoEmpujeAbsoluto = Main.GameUpdateCount;
                if (_libres.Count < 12) _libres.Add(c);
            }
        }

        // ==================================================================
        //  EL ESTADO (ring buffer de posiciones + el ARCO acumulado)
        // ==================================================================

        private Vector2[] _pts = new Vector2[24];
        private float[] _arco = new float[24];      // arco recorrido HASTA cada punto (la fase ancla)
        private int _n;
        private int _cabeza;                        // índice del punto MÁS NUEVO
        private float _arcoAcumulado;
        private uint _últimoFrameEmpujado;
        internal int _dueño;

        private Cinta() { }

        private void Redimensionar(int capacidad)
        {
            capacidad = Math.Clamp(capacidad, 4, 96);
            if (capacidad == _pts.Length) return;
            var pts = new Vector2[capacidad];
            var arco = new float[capacidad];
            int copiar = Math.Min(_n, capacidad);
            for (int i = 0; i < copiar; i++)
            {
                int src = (_cabeza - i + _pts.Length) % _pts.Length;
                pts[i] = _pts[src];
                arco[i] = _arco[src];
            }
            _pts = pts;
            _arco = arco;
            _n = copiar;
            _cabeza = Math.Max(0, _n - 1);
        }

        /// <summary>
        /// EMPUJA la posición actual (en coords de MUNDO). SUB-STEP AWARE:
        /// solo acepta UN empuje por tick de juego — los proyectiles con
        /// extraUpdates llaman esto desde cada sub-paso y la cinta toma el
        /// ritmo del RENDER (sin estelas de 6× puntos comprimidos).
        /// </summary>
        public void Empujar(Vector2 posMundo)
        {
            uint frame = Main.GameUpdateCount;
            _últimoEmpujeAbsoluto = frame; // v6.49 — el reloj del barrendero
            if (frame == _últimoFrameEmpujado)
            {
                // sub-paso: refresza la punta, no añade historial.
                if (_n > 0) _pts[_cabeza] = posMundo;
                return;
            }
            _últimoFrameEmpujado = frame;

            if (_n > 0)
                _arcoAcumulado += Vector2.Distance(_pts[_cabeza], posMundo);

            _cabeza = (_cabeza + 1) % _pts.Length;
            _pts[_cabeza] = posMundo;
            _arco[_cabeza] = _arcoAcumulado;
            if (_n < _pts.Length) _n++;
        }

        /// <summary>Cuántos puntos tiene el historial.</summary>
        public int Puntos => _n;

        // ==================================================================
        //  LA ESPECIE (spec) — todo lo que la cinta puede ser
        // ==================================================================

        /// <summary>EL PERFIL DE ANCHURA a lo largo de la cinta.</summary>
        public enum PerfilAncho
        {
            /// <summary>Constante de punta a punta (la cinta pareja).</summary>
            Constante,
            /// <summary>Meseta 80% + tapas redondas (la lección del desgarro).</summary>
            Meseta,
            /// <summary>Huso: gorda al centro, aguja en las puntas.</summary>
            Huso,
            /// <summary>Cometa: gorda en la CABEZA (el punto nuevo), fina en la cola.</summary>
            Cometa,
        }

        /// <summary>TODO lo que Dibujar necesita (struct: cero allocs).</summary>
        public struct CintaSpec
        {
            /// <summary>Ancho MÁXIMO en px (en la CABEZA de la cinta).</summary>
            public float Ancho;
            /// <summary>El perfil longitudinal del ancho.</summary>
            public PerfilAncho Perfil;
            /// <summary>Color del CUERPO (α final ≈ este · 0.55).</summary>
            public Color ColorCuerpo;
            /// <summary>Color del NÚCLEO interior (más claro, ×0.45 de ancho).</summary>
            public Color ColorNúcleo;
            /// <summary>Intensidad global 0..1 (el latido vive AQUÍ, no en el ancho).</summary>
            public float Intensidad;
            /// <summary>Velocidad de la TEXTURA (px de avance de fase por px de arco —
            /// la fase ancla: 1 = la textura viaja pegada al mundo).</summary>
            public float EscalaFase;
            /// <summary>Dibujar también el núcleo interior claro.</summary>
            public bool ConNúcleo;
            /// <summary>La longitud visible MÁXIMA (px) — la cola vieja se corta.</summary>
            public float LargoMáximo;
        }

        // ==================================================================
        //  EL DIBUJO (quads + perlas con SOLAPE ADAPTATIVO — la escuela v6.31)
        // ==================================================================

        // (los pinceles de la casa)
        private static Asset<Texture2D> _taperLuz;
        private static Texture2D TaperLuz =>
            (_taperLuz ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperNucleo")).Value;

        /// <summary>
        /// DIBUJA la cinta en el batch ABIERTO (aditivo recomendado — no lo
        /// abre ni lo cierra, el contrato de la casa). La FASE viaja anclada
        /// al arco recorrido (la textura avanza con el MUNDO, no con el
        /// reloj): la estela se ve SÓLIDA al girar. Cada segmento solo
        /// solapa lo que su giro real pide — cero cuentas aditivas.
        /// </summary>
        public void Dibujar(SpriteBatch batch, in CintaSpec spec)
        {
            if (batch == null || _n < 2 || spec.Intensidad <= 0.02f || spec.Ancho < 0.8f) return;

            // recorre del MÁS VIEJO al MÁS NUEVO, cortando por largo máximo
            Texture2D tex = TaperLuz;
            Rectangle src = new Rectangle(tex.Width * 3 / 10, 0, tex.Width * 2 / 5, tex.Height);
            Vector2 srcSize = new Vector2(src.Width, src.Height);

            float arcoCabeza = _arco[_cabeza];
            Color cuerpo = Tinte(spec.ColorCuerpo, 0.55f * spec.Intensidad);
            Color núcleo = Tinte(spec.ColorNúcleo, 0.85f * spec.Intensidad);

            for (int i = _n - 1; i > 0; i--)
            {
                // índices del ring (i = más nuevo → i-1... ojo: recorremos pares)
                int idxNuevo = (_cabeza - (_n - 1 - i) + _pts.Length * 2) % _pts.Length;
                int idxViejo = (_cabeza - (_n - i) + _pts.Length * 2) % _pts.Length;

                Vector2 b = _pts[idxNuevo];     // el extremo MÁS NUEVO del segmento
                Vector2 a = _pts[idxViejo];
                float len = Vector2.Distance(a, b);
                if (len < 0.30f) continue;

                // EL CORTE por largo máximo (la cola vieja se suelta).
                float arcoB = arcoCabeza - _arco[idxNuevo];
                if (arcoB > spec.LargoMáximo) continue;

                // LA FASE ANCLADA: u de textura por los PÍXELES recorridos.
                float uFase = (_arcoAcumulado - _arco[idxNuevo]) * MathF.Max(spec.EscalaFase, 0.05f);

                // EL ANCHO en los vértices compartidos (meseta/huso/cometa
                // sobre la fracción de arco desde la CABEZA, no desde la cola).
                float fB = arcoB / MathF.Max(spec.LargoMáximo, 1f);                       // 0 = cabeza
                float fA = (arcoB + len) / Mathf_Max(spec.LargoMáximo, 1f);
                float wB = AnchoEn(spec, 1f - fB);
                float wA = AnchoEn(spec, 1f - fA);
                float wseg = (wA + wB) * 0.5f;
                float wmax = MathF.Max(wA, wB);

                // EL GIRO real del segmento (para el solape adaptativo).
                float giroA = i < _n - 1 ? GiroEntre(idxViejo) : 0f;
                float giroB = i > 1 ? GiroEntre(idxNuevo) : 0f;
                float largo = len + Extensión(wmax, giroA) + Extensión(wmax, giroB);

                Vector2 mid = (a + b) * 0.5f - Main.screenPosition;
                float rot = MathF.Atan2(b.Y - a.Y, b.X - a.X);

                // EL CUERPO (con la FASE ANCLADA como offset de sourceRect:
                // la textura corre con el mundo — el antideslizamiento).
                Rectangle srcFase = src;
                float fasePx = (uFase % 1f) * src.Width;
                srcFase.X = src.X + (int)fasePx;
                srcFase.Width = Math.Max(2, src.Width - (int)fasePx);

                DibujarSegmento(batch, tex, srcFase, srcSize, mid, largo, wseg, rot, cuerpo);
                if (spec.ConNúcleo)
                    DibujarSegmento(batch, tex, src, srcSize, mid, largo, wseg * 0.45f, rot, núcleo);

                // LA PERLA SOLO con giro real (la luz aditiva no se apila).
                if (giroA > 0.14f)
                {
                    Vector2 pa = a - Main.screenPosition;
                    DibujarSegmento(batch, tex, src, srcSize, pa, wmax * 1.25f, wseg, rot, cuerpo);
                    if (spec.ConNúcleo)
                        DibujarSegmento(batch, tex, src, srcSize, pa, wmax * 1.25f, wseg * 0.45f, rot, núcleo);
                }
            }
        }

        private static float Mathf_Max(float a, float b) => a > b ? a : b;

        /// <summary>El ancho del perfil en la fracción t (0 = cola vieja, 1 = cabeza).</summary>
        private static float AnchoEn(in CintaSpec s, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            switch (s.Perfil)
            {
                case PerfilAncho.Meseta:
                    // 100% en t∈[0.10,0.90] + tapas circulares (la lección v6.31).
                    if (t < 0.10f) return s.Ancho * MathF.Sqrt(MathF.Max(1f - (1f - t / 0.10f) * (1f - t / 0.10f), 0f));
                    if (t > 0.90f) return s.Ancho * MathF.Sqrt(MathF.Max(1f - (t - 0.90f) / 0.10f * ((t - 0.90f) / 0.10f), 0f));
                    return s.Ancho;
                case PerfilAncho.Huso:
                    return s.Ancho * MathF.Pow(MathF.Sin(t * MathHelper.Pi), 0.6f);
                case PerfilAncho.Cometa:
                    return s.Ancho * MathF.Pow(t, 0.45f);
                default:
                    return s.Ancho;
            }
        }

        /// <summary>El giro (rad) en el vértice idx (entre su segmento y el siguiente hacia la cola).</summary>
        private float GiroEntre(int idx)
        {
            int prev = (idx - 1 + _pts.Length * 2) % _pts.Length;
            int next = (idx + 1) % _pts.Length;
            Vector2 d0 = _pts[idx] - _pts[prev];
            Vector2 d1 = _pts[next] - _pts[idx];
            if (d0.LengthSquared() < 0.0001f || d1.LengthSquared() < 0.0001f) return 0f;
            float a0 = MathF.Atan2(d0.Y, d0.X);
            float a1 = MathF.Atan2(d1.Y, d1.X);
            float d = MathF.Abs(a1 - a0);
            if (d > MathHelper.Pi) d = MathHelper.TwoPi - d;
            return d;
        }

        /// <summary>LA EXTENSIÓN DE SOLAPE por giro (la geometría del round-join v6.31).</summary>
        private static float Extensión(float w, float giro)
            => MathHelper.Clamp(w * 0.5f * MathF.Tan(giro * 0.5f), 0f, w * 0.5f);

        /// <summary>El segmento estirado (dest = largo × ancho, fase viaja en el sourceRect).</summary>
        private static void DibujarSegmento(SpriteBatch batch, Texture2D tex, Rectangle src,
            Vector2 srcSize, Vector2 pos, float largo, float ancho, float rot, Color tint)
        {
            if (tint.A == 0 || largo < 0.1f || ancho < 0.1f) return;
            batch.Draw(tex, pos, src, tint, rot,
                srcSize * 0.5f,
                new Vector2(largo, ancho) / srcSize,
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — RGB intacto,
        /// alfa=f; el Additive de FNA es (SourceAlpha, One): el alfa GATEA).</summary>
        private static Color Tinte(Color c, float f)
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
