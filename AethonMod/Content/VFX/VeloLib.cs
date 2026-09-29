using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  VELOLIB — LA LIBRERÍA DE LA OSCURIDAD (v6.50.40).
    //
    //  La petición de esta versión, dos reglas nuevas:
    //  «la capa de oscuridad no debe estar sobre todo, la capa debe estar
    //  por debajo de la interfaz de usuario» y «no debe cubrir ni al
    //  jugador ni al jefe, la luz que tienen se supone que quita esa
    //  oscuridad».
    //
    //  1. EL VELO VIVE BAJO LA INTERFAZ: ya no es Main.OnPostDraw (encima
    //     del frame terminado — la receta WotG de v6.50.39, que apagaba
    //     también el HUD). Ahora es LA PRIMERA CAPA DE LA INTERFAZ
    //     (ModifyInterfaceLayers + LegacyGameInterfaceLayer en el índice
    //     0): el mundo y sus criaturas quedan bajo la oscuridad, pero el
    //     HUD, el mapa, la barra del jefe, el chat y el cursor quedan
    //     USABLES — se apaga el mundo, no el juego. (El contrato del lote
    //     de la capa: tML lo abre Deferred·AlphaBlend·LinearClamp·
    //     DepthStencil.None·CullCCW·ZoomMatrix, llama al delegado y lo
    //     cierra al volver — el delegado debe devolverlo ABIERTO con los
    //     parámetros EXACTOS. Literal del decompile de tML 2026.07,
    //     probado a fondo en v6.50.37/.38.)
    //
    //  2. LA LUZ QUITA LA OSCURIDAD — EL MOSAICO: el velo ya no es un
    //     rectángulo entero que lo cubre TODO. Es un MOSAICO DISJUNTO de
    //     piezas de sprite: las BANDAS del velo pleno (rectángulos del
    //     pixel blanco) que esquivan las PLAZAS de las luces + una DONA
    //     radial por luz (VeloDona.png: núcleo transparente, penumbra
    //     smoothstep, borde opaco) recortada alrededor de las plazas ya
    //     dibujadas. Invariante del mosaico: CADA PÍXEL del velo lo pinta
    //     UNA sola pieza — nunca hay doble oscurecimiento, nunca hay
    //     costuras, y la penumbra de una luz grande JAMÁS tapa el núcleo
    //     limpio de una chica (las luces se dibujan de la chica a la
    //     grande y las grandes se recortan alrededor). Aethon brilla
    //     dorado en SU círculo, el jugador con Grimorio ≥50 tiene SU
    //     pequeño círculo, las balas de la luz se ven venir — la oscuridad
    //     de Don't Starve, ahora de verdad.
    //
    //  EL CONTRATO DE ROBUSTEZ (el fix de «al compilar el juego se
    //  cierra», v6.50.39 — INTACTO): cero manipulación del GraphicsDevice
    //  (ni render targets, ni Get/SetRenderTargets, ni stencils, ni
    //  shaders, ni blends custom — solo rectángulos de SpriteBatch y UNA
    //  textura). El dibujado completo vive en try/catch con CERROJO: si
    //  cae tres veces, el velo se retira para siempre y el juego SIGUE
    //  VIVO — y todo se escribe en el log (cero catch vacíos).
    // ======================================================================
    public static class Velo
    {
        // === EL ESTADO DEL VELO ===
        private static Color _color = Color.White;          // lo que se ve ahora
        private static Color _colorObjetivo = Color.White;  // a dónde va
        private static float _intensidad = 0f;              // cuánto cubre (0..1)
        private static float _intensidadObjetivo = 0f;      // a dónde va
        private static float _paso = 1f / 45f;              // el ritmo del fade

        /// <summary>Cuánto cubre el velo ahora (0..1) — para consultar sin registrarse.</summary>
        public static float Intensidad => _intensidad;

        // === EL CERROJO (tres caídas y el velo se retira — el juego sigue vivo) ===
        private static int _fallas = 0;
        internal static bool Roto => _fallas >= 3;

        // === LAS LUCES (arrays pre-asignados — cero GC por frame) ===
        internal const int MaxLuces = 40;
        internal static readonly Vector2[] LuzPos = new Vector2[MaxLuces];
        internal static readonly float[] LuzAgujero = new float[MaxLuces];  // el radio que ABRE en la oscuridad
        internal static readonly float[] LuzRadio = new float[MaxLuces];    // el radio del BRILLO aditivo
        internal static readonly Color[] LuzColor = new Color[MaxLuces];
        internal static readonly bool[] LuzBrasa = new bool[MaxLuces];
        internal static int Luces = 0;

        // === LA VIDA DE UN TICK (no de un frame) ========================
        // Las luces se registran durante el UPDATE (PreUpdateTime) y el
        // dibujado pasa DESPUÉS, entre ticks: a 144 Hz un limpiado por
        // FRAME apagaría las luces 2 de cada 3 frames (parpadeo). El
        // sello de tick lo arregla: la PRIMERA luz del tick barre las del
        // tick anterior y TODOS los frames de ese tick las dibujan.
        private static uint _tickLuces = 0u;
        private static uint _tickPintores = 0u;
        private static bool _pintoresSiempre = false;

        // === LOS PINTORES (el DrawAfterWhiteEvent de la casa) ===
        private static List<Action<SpriteBatch>> _pintores = new List<Action<SpriteBatch>>(8);

        // === LA DONA (la textura del agujero con su penumbra) ===
        private static Texture2D _donaCache;

        /// <summary>La fracción del semilado de la dona que es núcleo LIMPIO
        /// (el resto es penumbra hasta el borde opaco — el perfil de
        /// tools/gen_velo_dona_v65040.py).</summary>
        internal const float FRACCION_NUCLEO = 0.76f;

        // === EL MOSAICO — scratch pre-asignado (cero GC por frame) ===
        private static readonly int[] _plzX0 = new int[MaxLuces];
        private static readonly int[] _plzY0 = new int[MaxLuces];
        private static readonly int[] _plzX1 = new int[MaxLuces];
        private static readonly int[] _plzY1 = new int[MaxLuces];
        private static readonly float[] _plzR = new float[MaxLuces];
        private static readonly int[] _orden = new int[MaxLuces];
        private static readonly int[] _cortesY = new int[MaxLuces * 2 + 2];
        private static readonly int[] _cortesX = new int[MaxLuces * 2 + 2];
        private static readonly int[] _activos = new int[MaxLuces];

        // las piezas del recorte (ping-pong de dos buffers)
        private const int MaxPiezas = 256;
        private static readonly int[] _aX0 = new int[MaxPiezas];
        private static readonly int[] _aY0 = new int[MaxPiezas];
        private static readonly int[] _aX1 = new int[MaxPiezas];
        private static readonly int[] _aY1 = new int[MaxPiezas];
        private static readonly int[] _bX0 = new int[MaxPiezas];
        private static readonly int[] _bY0 = new int[MaxPiezas];
        private static readonly int[] _bX1 = new int[MaxPiezas];
        private static readonly int[] _bY1 = new int[MaxPiezas];

        // ==================================================================
        //  LA API
        // ==================================================================

        /// <summary>
        /// Encender el velo hacia un color y una intensidad (el fade camina
        /// al ritmo dado, en unidades por tick). Llamar cada tick mientras
        /// dure (o una sola vez: el objetivo se conserva hasta el próximo Ver).
        /// </summary>
        public static void Ver(Color color, float intensidad, float paso)
        {
            _colorObjetivo = color;
            _intensidadObjetivo = MathHelper.Clamp(intensidad, 0f, 1f);
            if (paso > 0f) _paso = paso;
        }

        /// <summary>Apagar el velo (fade hacia 0 al ritmo dado).</summary>
        public static void Apagar(float paso)
        {
            _intensidadObjetivo = 0f;
            if (paso > 0f) _paso = paso;
        }

        /// <summary>
        /// Registrar UNA LUZ (se limpia sola al empezar el próximo tick —
        /// hay que registrarla cada tick que deba verse). posMundo en
        /// coordenadas del MUNDO; radioAgujero = lo que su luz ABRE en la
        /// oscuridad (el núcleo limpio donde el mundo se ve — Aethon, el
        /// círculo del Grimorio, las balas); radioBrillo = el alcance del
        /// resplandor aditivo que vive ENCIMA del velo.
        /// </summary>
        public static void Luz(Vector2 posMundo, float radioAgujero,
            float radioBrillo, Color color, bool brasa = true)
        {
            if (Luces >= MaxLuces) return;
            if (Main.GameUpdateCount != _tickLuces)
            {
                Luces = 0;   // el primer registro del tick barre el tick viejo
                _tickLuces = Main.GameUpdateCount;
            }
            if (radioAgujero <= 0f && radioBrillo <= 1f) return;
            LuzPos[Luces] = posMundo;
            LuzAgujero[Luces] = MathF.Max(0f, radioAgujero);
            LuzRadio[Luces] = MathF.Max(1f, radioBrillo);
            LuzColor[Luces] = color;
            LuzBrasa[Luces] = brasa;
            Luces++;
        }

        /// <summary>
        /// Registrar un pintor (se invoca cada dibujado con el lote ABIERTO
        /// en aditivo·identidad; el contrato de la casa: devolverlo abierto
        /// en aditivo·identidad). Se registra UNA vez (OnModLoad) y vive
        /// hasta QuitarPintor/descarga.
        /// </summary>
        public static void SobreElVelo(Action<SpriteBatch> pintor)
        {
            if (pintor == null || _pintores == null) return;
            if (!_pintores.Contains(pintor)) _pintores.Add(pintor);
        }

        /// <summary>Retirar un pintor (la descarga llama esto — nunca puede lanzar).</summary>
        public static bool QuitarPintor(Action<SpriteBatch> pintor)
        {
            try { return _pintores != null && pintor != null && _pintores.Remove(pintor); }
            catch { return false; }
        }

        /// <summary>
        /// Pintar el contenido (luces/pintores) aunque el velo esté apagado —
        /// se llama CADA TICK que deba valer (vence a los dos ticks de no
        /// llamarse): para las despedidas lentas (el sol negro).
        /// </summary>
        public static void PintoresSiempre(bool si)
        {
            _pintoresSiempre = si;
            _tickPintores = Main.GameUpdateCount;
        }

        // ==================================================================
        //  EL MOTOR (llamado por VeloSistema)
        // ==================================================================

        /// <summary>El caminar del velo (una vez por tick, en PreUpdateTime).</summary>
        internal static void Paso()
        {
            // la intensidad camina hacia su objetivo — SIN rebasarlo
            if (_intensidad < _intensidadObjetivo)
                _intensidad = MathF.Min(_intensidadObjetivo, _intensidad + _paso);
            else if (_intensidad > _intensidadObjetivo)
                _intensidad = MathF.Max(_intensidadObjetivo, _intensidad - _paso);

            // el color camina el DOBLE de rápido (el crossfade blanco→negro
            // del climax es medio segundo de respiración)
            _color = Color.Lerp(_color, _colorObjetivo, MathF.Min(1f, _paso * 2f));
        }

        /// <summary>El color listo para el lote (el frame entero × su intensidad).</summary>
        internal static Color ColorDelFrame()
        {
            Color c = _color;
            c.A = 255;
            return c * _intensidad;
        }

        /// <summary>¿Los pintores siguen vivos? (la bandera vale dos ticks).</summary>
        internal static bool PintoresVivos =>
            _pintoresSiempre && (Main.GameUpdateCount - _tickPintores) <= 2u;

        /// <summary>¿Hay algo que dibujar SOBRE el velo?</summary>
        internal static bool HayContenido()
        {
            return Luces > 0 || PintoresVivos;
        }

        /// <summary>Una caída contada (el cerrojo: a la tercera, retiro definitivo).</summary>
        internal static void NotaFalla()
        {
            _fallas++;
        }

        /// <summary>El funeral del estado transitorio (el menú y la recarga).</summary>
        internal static void Reset()
        {
            _color = Color.White;
            _colorObjetivo = Color.White;
            _intensidad = 0f;
            _intensidadObjetivo = 0f;
            _paso = 1f / 45f;
            _fallas = 0;
            Luces = 0;
            _tickLuces = 0u;
            _tickPintores = 0u;
            _pintoresSiempre = false;
            // OJO: los PINTORES NO se tocan — están registrados para toda
            // la vida del mod (OnModLoad→Unload): el menú mata el velo,
            // no a sus pintores (la lección de esta línea: borrarlos aquí
            // enterraba el sol negro para SIEMPRE tras un viaje al menú).
        }

        /// <summary>Limpiar los pintores de raíz (la descarga de la librería).</summary>
        internal static void OlvidarPintores()
        {
            _pintores = null;
        }

        /// <summary>Soltar la dona (la descarga: nada muerto toca ModContent).</summary>
        internal static void OlvidarDona()
        {
            _donaCache = null;
        }

        /// <summary>La dona del mosaico (el agujero de luz con su penumbra).</summary>
        internal static Texture2D Dona
        {
            get
            {
                try
                {
                    if (_donaCache == null || _donaCache.IsDisposed)
                        _donaCache = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/VeloDona").Value;
                    return _donaCache;
                }
                catch { return null; }
            }
        }

        // ==================================================================
        //  EL MOSAICO — EL VELO CON SUS AGUJEROS (la petición de esta
        //  versión: «no debe cubrir ni al jugador ni al jefe, la luz que
        //  tienen se supone que quita esa oscuridad»).
        //
        //  TODO en píxeles de DISPOSITIVO (la lección v6.50.38: el zoom
        //  forzado de las pantallas grandes — la transformación es UNA
        //  sola: Transform(posMundo - screenPosition, ZoomMatrix)).
        //
        //  1. LAS PLAZAS: el cuadrado de cada luz (semilado = agujero /
        //     FRACCION_NUCLEO — el núcleo de la dona cae EXACTO en el
        //     radio pedido).
        //  2. LA BASE: bandas horizontales cortadas por las plazas — un
        //     rectángulo del velo pleno por cada celda que NINGUNA plaza
        //     toca (los cortes son los bordes de las plazas: ninguna celda
        //     queda a caballo de un borde).
        //  3. LAS DONAS: plaza por plaza, de la luz CHICA a la GRANDE,
        //     cada una RECORTADA alrededor de las plazas ya dibujadas (la
        //     resta de rectángulos de siempre: una pieza tocada se parte
        //     en franjas izquierda/derecha/arriba/abajo). El resultado es
        //     DISJUNTO: cada píxel del velo lo pinta UNA sola pieza y la
        //     penumbra de una luz grande jamás tapa el núcleo de una chica.
        // ==================================================================
        internal static void DibujarVeloMosaico(SpriteBatch sb, int w, int h, Matrix mVista)
        {
            Color color = ColorDelFrame();
            Texture2D pixel = VFXCore.Pixel;
            if (pixel == null) return;

            // === LAS PLAZAS (el cuadrado de cada luz, en dispositivo) ===
            Texture2D dona = Dona;
            int n = 0;
            if (Luces > 0 && dona != null)
            {
                // el zoom de vista (la lección v6.50.38: ForcedMinimumZoom
                // ≠ 1 en pantallas grandes — las plazas escalan con el mundo)
                float esc = MathF.Max(0.35f, (mVista.M11 + mVista.M22) * 0.5f);
                for (int i = 0; i < Luces; i++)
                {
                    float radioDev = LuzAgujero[i] * esc;
                    if (radioDev < 2f) continue;   // agujero subpíxel: solo el brillo vive
                    Vector2 pos = Vector2.Transform(LuzPos[i] - Main.screenPosition, mVista);
                    int s = (int)MathF.Round(radioDev / FRACCION_NUCLEO);
                    int cx = (int)MathF.Round(pos.X);
                    int cy = (int)MathF.Round(pos.Y);
                    _plzX0[n] = cx - s; _plzY0[n] = cy - s;
                    _plzX1[n] = cx + s; _plzY1[n] = cy + s;
                    _plzR[n] = radioDev;
                    _orden[n] = n;
                    n++;
                }

                // EL ORDEN: las luces CHICAS primero — sus donas se dibujan
                // enteras y las GRANDES se recortan a su alrededor. Así el
                // núcleo limpio de una luz chica NUNCA es tapado por la
                // penumbra de una grande: la luz que tienen QUITA la
                // oscuridad (inserción estable, n ≤ 40).
                if (n > 1)
                {
                    for (int a = 1; a < n; a++)
                    {
                        int idx = _orden[a];
                        float r = _plzR[idx];
                        int b = a - 1;
                        while (b >= 0 && _plzR[_orden[b]] > r)
                        {
                            _orden[b + 1] = _orden[b];
                            b--;
                        }
                        _orden[b + 1] = idx;
                    }
                }
            }

            // sin plazas: el velo entero en UNA pieza (la receta del frame)
            if (n == 0)
            {
                sb.Draw(pixel, new Rectangle(0, 0, w, h), color);
                return;
            }

            // === LA BASE: bandas cortadas por las plazas ================
            int cy2 = 0;
            _cortesY[cy2++] = 0;
            _cortesY[cy2++] = h;
            for (int i = 0; i < n; i++)
            {
                if (_plzY0[i] > 0 && _plzY0[i] < h) _cortesY[cy2++] = _plzY0[i];
                if (_plzY1[i] > 0 && _plzY1[i] < h) _cortesY[cy2++] = _plzY1[i];
            }
            Array.Sort(_cortesY, 0, cy2);

            for (int a = 0; a + 1 < cy2; a++)
            {
                int y0 = _cortesY[a], y1 = _cortesY[a + 1];
                if (y1 <= y0) continue;

                // las plazas que viven en ESTA banda
                int na = 0;
                for (int i = 0; i < n; i++)
                    if (_plzY0[i] < y1 && _plzY1[i] > y0) _activos[na++] = i;

                if (na == 0)
                {
                    sb.Draw(pixel, new Rectangle(0, y0, w, y1 - y0), color);
                    continue;
                }

                int cx2 = 0;
                _cortesX[cx2++] = 0;
                _cortesX[cx2++] = w;
                for (int j = 0; j < na; j++)
                {
                    int i = _activos[j];
                    if (_plzX0[i] > 0 && _plzX0[i] < w) _cortesX[cx2++] = _plzX0[i];
                    if (_plzX1[i] > 0 && _plzX1[i] < w) _cortesX[cx2++] = _plzX1[i];
                }
                Array.Sort(_cortesX, 0, cx2);

                for (int b = 0; b + 1 < cx2; b++)
                {
                    int x0 = _cortesX[b], x1 = _cortesX[b + 1];
                    if (x1 <= x0) continue;

                    // ¿el segmento vive DENTRO de una plaza? (la banda ya
                    // está dentro de su y: los cortes nunca dejan un
                    // segmento a caballo de un borde)
                    bool dentro = false;
                    for (int j = 0; j < na && !dentro; j++)
                    {
                        int i = _activos[j];
                        if (_plzX0[i] <= x0 && _plzX1[i] >= x1) dentro = true;
                    }
                    if (!dentro)
                        sb.Draw(pixel, new Rectangle(x0, y0, x1 - x0, y1 - y0), color);
                }
            }

            // === LAS DONAS: plaza por plaza (chica → grande), cada una
            //     RECORTADA alrededor de las ya dibujadas ================
            for (int k = 0; k < n; k++)
            {
                int i = _orden[k];
                int ancho = _plzX1[i] - _plzX0[i];
                int alto = _plzY1[i] - _plzY0[i];
                if (ancho <= 0 || alto <= 0) continue;

                // la plaza i MENOS las plazas ya dibujadas (ping-pong)
                int np = 1;
                _aX0[0] = _plzX0[i]; _aY0[0] = _plzY0[i];
                _aX1[0] = _plzX1[i]; _aY1[0] = _plzY1[i];
                for (int j = 0; j < k && np > 0; j++)
                {
                    int q = _orden[j];
                    np = RestarPlaza(np, _plzX0[q], _plzY0[q], _plzX1[q], _plzY1[q]);
                }

                for (int p = 0; p < np; p++)
                {
                    // cull barato: la pieza fuera de pantalla no se dibuja
                    if (_aX1[p] <= 0 || _aY1[p] <= 0 || _aX0[p] >= w || _aY0[p] >= h) continue;
                    int pw = _aX1[p] - _aX0[p];
                    int ph = _aY1[p] - _aY0[p];
                    if (pw <= 0 || ph <= 0) continue;

                    // el mapeo lineal al espacio de la dona (256×256 ↔ la plaza entera)
                    int u0 = (_aX0[p] - _plzX0[i]) * 256 / ancho;
                    int v0 = (_aY0[p] - _plzY0[i]) * 256 / alto;
                    int u1 = (_aX1[p] - _plzX0[i]) * 256 / ancho;
                    int v1 = (_aY1[p] - _plzY0[i]) * 256 / alto;
                    if (u1 - u0 <= 0 || v1 - v0 <= 0) continue;
                    var dest = new Rectangle(_aX0[p], _aY0[p], pw, ph);
                    var src = new Rectangle(u0, v0, u1 - u0, v1 - v0);
                    sb.Draw(dona, dest, src, color);
                }
            }
        }

        /// <summary>
        /// Restar una plaza de las piezas actuales (ping-pong A→B→A): una
        /// pieza tocada por la plaza se parte en hasta 4 franjas (izquierda,
        /// derecha, arriba, abajo) — la resta de rectángulos de siempre.
        /// Devuelve la nueva cuenta de piezas.
        /// </summary>
        private static int RestarPlaza(int np, int qx0, int qy0, int qx1, int qy1)
        {
            int m = 0;
            for (int p = 0; p < np; p++)
            {
                int ix0 = Math.Max(_aX0[p], qx0), iy0 = Math.Max(_aY0[p], qy0);
                int ix1 = Math.Min(_aX1[p], qx1), iy1 = Math.Min(_aY1[p], qy1);

                // no se tocan: la pieza pasa entera
                if (ix0 >= ix1 || iy0 >= iy1)
                {
                    if (m < MaxPiezas)
                    {
                        _bX0[m] = _aX0[p]; _bY0[m] = _aY0[p];
                        _bX1[m] = _aX1[p]; _bY1[m] = _aY1[p];
                        m++;
                    }
                    continue;
                }

                // sin lugar para las 4 franjas posibles: la pieza pasa entera
                // (degradación silenciosa del tope — en la práctica no llega:
                // harían falta ~85 plazas encimadas)
                if (m + 4 > MaxPiezas)
                {
                    _bX0[m] = _aX0[p]; _bY0[m] = _aY0[p];
                    _bX1[m] = _aX1[p]; _bY1[m] = _aY1[p];
                    m++;
                    continue;
                }

                // las franjas alrededor de la intersección
                if (_aX0[p] < ix0)
                {
                    _bX0[m] = _aX0[p]; _bY0[m] = _aY0[p];
                    _bX1[m] = ix0; _bY1[m] = _aY1[p]; m++;
                }
                if (ix1 < _aX1[p])
                {
                    _bX0[m] = ix1; _bY0[m] = _aY0[p];
                    _bX1[m] = _aX1[p]; _bY1[m] = _aY1[p]; m++;
                }
                if (_aY0[p] < iy0)
                {
                    _bX0[m] = ix0; _bY0[m] = _aY0[p];
                    _bX1[m] = ix1; _bY1[m] = iy0; m++;
                }
                if (iy1 < _aY1[p])
                {
                    _bX0[m] = ix0; _bY0[m] = iy1;
                    _bX1[m] = ix1; _bY1[m] = _aY1[p]; m++;
                }
            }
            // ping-pong: B pasa a ser el buffer de lectura
            Array.Copy(_bX0, _aX0, m);
            Array.Copy(_bY0, _aY0, m);
            Array.Copy(_bX1, _aX1, m);
            Array.Copy(_bY1, _aY1, m);
            return m;
        }

        // ==================================================================
        //  EL DIBUJADO DE LAS LUCES (aditivo, sobre el velo — el brillo
        //  que REBOZA el agujero y se derrama sobre la oscuridad)
        // ==================================================================
        internal static void DibujarLuces(SpriteBatch sb, Matrix mVista)
        {
            if (Luces <= 0) return;
            Texture2D glow = VFXCore.SoftGlow;
            Texture2D orbe = VFXCore.GlowOrb;
            if (glow == null || orbe == null) return;

            Vector2 centroG = new Vector2(glow.Width, glow.Height) * 0.5f;
            Vector2 centroO = new Vector2(orbe.Width, orbe.Height) * 0.5f;
            // el zoom de vista (la lección v6.50.38: ForcedMinimumZoom ≠ 1 en
            // pantallas grandes — las luces escalan con el mundo que tapan)
            float esc = MathF.Max(0.35f, (mVista.M11 + mVista.M22) * 0.5f);

            for (int i = 0; i < Luces; i++)
            {
                Vector2 pos = Vector2.Transform(LuzPos[i] - Main.screenPosition, mVista);
                float radio = LuzRadio[i] * esc;

                // EL CÍRCULO — el resplandor ancho (la penumbra que ALCANZA)
                sb.Draw(glow, pos, null, LuzColor[i] * 0.40f, 0f, centroG,
                    new Vector2(radio * 2.6f / glow.Width, radio * 2.6f / glow.Height),
                    SpriteEffects.None, 0f);
                // un segundo velo interior (el círculo se siente LLENO)
                sb.Draw(glow, pos, null, LuzColor[i] * 0.30f, 0f, centroG,
                    new Vector2(radio * 1.5f / glow.Width, radio * 1.5f / glow.Height),
                    SpriteEffects.None, 0f);

                // LA BRASA — el corazón pleno (lo que arde de verdad)
                if (LuzBrasa[i])
                    sb.Draw(orbe, pos, null, LuzColor[i] * 0.60f, 0f, centroO,
                        new Vector2(radio * 0.9f / orbe.Width, radio * 0.9f / orbe.Height),
                        SpriteEffects.None, 0f);
            }
        }

        /// <summary>Invocar los pintores (cada uno en su try — uno no tumba al resto).</summary>
        internal static void InvocarPintores(SpriteBatch sb, Matrix mVista)
        {
            if (_pintores == null || _pintores.Count == 0) return;
            for (int i = _pintores.Count - 1; i >= 0; i--)
            {
                try { _pintores[i]?.Invoke(sb); }
                catch (Exception e)
                {
                    try
                    {
                        Terraria.ModLoader.Logging.PublicLogger.Error(
                            "[AethonMod] VeloLib: un pintor del velo falló — retirado", e);
                    }
                    catch { }
                    try { _pintores.RemoveAt(i); } catch { }
                }
            }
        }
    }

    // ======================================================================
    //  VELOSISTEMA — LA CAPA ÚNICA DEL FRAME.
    //
    //  EL PUNTO DEL DIBUJADO (v6.50.40): LA PRIMERA CAPA DE LA INTERFAZ —
    //  el mundo ya está dibujado (tiles, criaturas, polvo, agua) y la
    //  interfaz todavía no: el velo cae SOBRE el mundo y BAJO el HUD, el
    //  mapa, la barra del jefe, el chat y el cursor. La oscuridad es del
    //  MUNDO, no del juego.
    //
    //  Cero manipulación del GraphicsDevice: Begin/End propios con
    //  identidad y el lote de la capa devuelto EXACTO como llegó.
    // ======================================================================
    public class VeloSistema : ModSystem
    {
        /// <summary>La recarga del mod: mismo funeral, otra razón.</summary>
        public override void Unload()
        {
            Velo.Reset();
            Velo.OlvidarPintores();
            Velo.OlvidarDona();
        }

        /// <summary>El caminar del velo (una vez por tick, en todas las máquinas).</summary>
        public override void PreUpdateTime()
        {
            if (Main.gameMenu)
            {
                Velo.Reset();   // el menú no tiene oscuridad que heredar
                return;
            }
            Velo.Paso();
        }

        // ==================================================================
        //  LA CAPA DE INTERFAZ — insertada SOLO cuando hay algo que
        //  dibujar (el velo activo o contenido encima): sin oscuridad,
        //  cero costo para el frame.
        // ==================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Velo.Roto) return;
            if (Velo.Intensidad <= 0.002f && !Velo.HayContenido()) return;
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: VeloLib — la Oscuridad",
                () => { CapaDelVelo(); return true; },
                InterfaceScaleType.Game));   // el lote de la capa: ZoomMatrix
        }

        // ==================================================================
        //  EL DIBUJADO — LA CAPA #0 DE LA INTERFAZ:
        //  1. EL VELO CON SUS AGUJEROS: el mosaico (alfa, identidad) —
        //     el mundo se apaga MENOS donde la luz llega.
        //  2. LO QUE BRILLA: las luces y los pintores, DESPUÉS, en
        //     aditivo — el brillo rebosa el agujero y vive sobre la
        //     oscuridad (el orden del DrawWhite de WotG).
        //
        //  EL CONTRATO DEL LOTE: tML abre el lote de la capa (Deferred ·
        //  AlphaBlend · LinearClamp · DepthStencil.None · CullCCW ·
        //  ZoomMatrix) y lo CIERRA al volver — este delegado lo cierra
        //  para sus pases propios y lo devuelve ABIERTO con los
        //  parámetros EXACTOS (literal del decompile de
        //  GameInterfaceLayer.Draw de tML 2026.07).
        // ==================================================================
        private static void CapaDelVelo()
        {
            try
            {
                if (Velo.Roto) return;   // el cerrojo: a la tercera, retiro
                if (Main.gameMenu || Main.dedServ || Main.netMode == NetmodeID.Server)
                    return;
                if (Main.mapFullscreen)
                    return;   // el mapa a pantalla completa NO se apaga

                float inten = Velo.Intensidad;
                bool veloVisible = inten > 0.002f;
                if (!veloVisible && !Velo.HayContenido())
                    return;   // nada que hacer: el lote de la capa queda intacto

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics == null ? null : Main.graphics.GraphicsDevice;
                if (sb == null || gd == null) return;

                int w = Main.screenWidth;
                int h = Main.screenHeight;
                if (w < 8 || h < 8) return;

                Matrix mVista = Main.GameViewMatrix.ZoomMatrix;   // vista→dispositivo

                // cerrar el lote que tML abrió para la capa
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    // === 1. EL VELO CON SUS AGUJEROS (alfa · identidad) ===
                    if (veloVisible)
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Matrix.Identity);
                        try { Velo.DibujarVeloMosaico(sb, w, h, mVista); }
                        finally { sb.End(); }
                    }

                    // === 2. LO QUE BRILLA — las luces y los pintores,
                    //     DESPUÉS del velo, en aditivo (el orden WotG) ===
                    if (Velo.HayContenido())
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Matrix.Identity);
                        try
                        {
                            Velo.DibujarLuces(sb, mVista);
                            Velo.InvocarPintores(sb, mVista);
                        }
                        finally { sb.End(); }
                    }
                }
                finally
                {
                    // DEVOLVER EL LOTE DE LA CAPA EXACTO (lo que tML abrió:
                    // Deferred · AlphaBlend · LinearClamp · DepthStencil.NONE
                    // · CullCCW · ZoomMatrix — literal del decompile)
                    try
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullCounterClockwise, null, mVista);
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                Velo.NotaFalla();
                try
                {
                    Terraria.ModLoader.Logging.PublicLogger.Error(
                        "[AethonMod] VeloLib: el velo falló al dibujarse (reportar con el client.log)", e);
                }
                catch { }
            }
        }
    }
}
