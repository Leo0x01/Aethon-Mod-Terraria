using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  v6.56.4 — EL CAMINO GPU DEL RC2, PORTADO A LAS ARMAS.
    //  La letra del usuario: «las armas que tienen picos tienen aún más
    //  picos… solo debes adaptar el código del rayo concentrado 2 a las
    //  características de las armas con pico, usando el código y las
    //  técnicas del rayo concentrado 2».
    //
    //  LA AUTOPSIA DE LA v6.56.3 (por qué empeoró): curó los picos en el
    //  dominio de la GEOMETRÍA (ventana V encogida contra la fila saturada
    //  de la textura + período U pisado a 563 px + sobre trapezoidal con
    //  micro-escalón al 12.5%) — el haz quedó como BARRA PLANA cuya
    //  silueta es la polilínea mitrada desnuda, y el hervor degeneró en
    //  bandas de baja frecuencia que viajan (leídas como sierra). El RC2
    //  no tiene picos porque su borde lo funde EL SHADER POR PÍXEL (envV
    //  smoothstep sobre el 30% del ancho), no la geometría.
    //
    //  ESTA CASA extrae el camino GPU del RC2 (VertexStrip +
    //  HazConcentradoShader, el par .fx+.fxc SANO desde la v6.55.1 —
    //  códice §1/§5.4) con SU fallback de la casa (RayoStrip ×3 — la red
    //  de seguridad de la ley §1.4-3). Los consumidores: la escalera
    //  (Alba/Veredicto/Juicio), el Prisma (la espina verlet), el Partidor
    //  (la carga y el corte) y la Columna de la Ira.
    //
    //  CONTRATO DE COORDENADAS: ptsMundo va en coords de MUNDO (el
    //  contrato del RC2: la tira recibe -Main.screenPosition como offset).
    //  PERFIL: semiancho por nodo en px. COLORES: por nodo, convención de
    //  la casa (Alfa/rgb premultiplicado) — el shader usa el RGB (el alfa
    //  de salida lo pone el propio .fx); el fallback usa el mismo array.
    // ====================================================================
    public static class HazConcentradoLib
    {
        // ==================== EL SHADER (el patrón del RC2) ====================
        private static bool _fallo;

        private static Effect _cache;

        public static Effect ObtenerShader()
        {
            if (_fallo)
            {
                return null;
            }
            if (_cache != null && !((GraphicsResource)_cache).IsDisposed)
            {
                return _cache;
            }
            try
            {
                _cache = ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/HazConcentradoShader", (AssetRequestMode)1).Value;
                return _cache;
            }
            catch
            {
                _fallo = true;
                return null;
            }
        }

        // ==================== EL DISCO DEL HORIZONTE ====================
        //  BlackDisk (256², el disco del agujero — sólido hasta ~0.88 del
        //  semiancho): el patrón `_disk ??=` de los renderers de agujeros.
        private static Texture2D _disco;

        public static Texture2D DiscoNegro
        {
            get
            {
                try
                {
                    if (_disco == null || _disco.IsDisposed)
                    {
                        _disco = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk", (AssetRequestMode)1).Value;
                    }
                    return _disco;
                }
                catch
                {
                    return null;
                }
            }
        }

        // ==================== LOS POZOS (cero-GC, la ley de la casa) ====================
        //  El patrón de los pozos del RC2 (v6.56.1): nada que alocar por frame.
        private const int NResample = 128;
        private static readonly Vector2[] _pts = new Vector2[NResample];
        private static readonly float[] _rotas = new float[NResample];

        private const int NFallback = 64;
        private static readonly Vector2[] _ptsF = new Vector2[NFallback];
        private static readonly float[] _semisF = new float[NFallback];
        private static readonly float[] _semisFA = new float[NFallback];
        private static readonly float[] _semisFB = new float[NFallback];
        private static readonly Color[] _coloresF = new Color[NFallback];

        private static readonly Vector2[] _ptsR = new Vector2[NFallback];
        private static readonly Vector2[] _ptsC = new Vector2[NFallback];
        private static readonly float[] _semisE = new float[NFallback];

        private static readonly VertexStrip _strip = new VertexStrip();

        // Las funciones de tira leen del INPUT del llamador (single-threaded
        // de dibujado — el mismo contrato del RC2 con sus métodos de instancia).
        private static float[] _perfilActivo;
        private static Color[] _coloresActivo;
        private static int _nActivo;

        // ==================== EL CONDUCTO ====================
        /// <summary>
        /// DIBUJA el conducto por el camino GPU del RC2: remuestrea la espina
        /// del llamador a 128 puntos (la densidad del RC2 a escala de arma),
        /// la viste con el HazConcentradoShader (las 4 texturas multiplicadas
        /// — carne·remolino·rampa·bits) dentro de un LOTE ADITIVO (el shader
        /// nació aditivo: sus texturas llevan bordes V negro puro y bajo
        /// AlphaBlend el borde OSCURECE — las franjas negras del RC2 de la
        /// v6.56.3, curadas aquí de paso), y si el shader no carga, cae al
        /// FALLBACK ×3 de la casa (RayoStrip: carne 420/1.0 · remolino
        /// 240/1.9 · núcleo en banda 380/2.6 — «bonito y curado de picos»,
        /// códice §1.2).
        /// </summary>
        /// <param name="ptsMundo">La espina, coords de MUNDO, n nodos (2..128).</param>
        /// <param name="perfil">Semiancho por nodo (px) — la fórmula propia del arma (AnchoCono y familia).</param>
        /// <param name="colores">Color por nodo (la convención de la casa: el gradiente viajero + lente).</param>
        /// <param name="uTime">El reloj del flujo: −reloj·0.025·flujoPropio (positivo = hierve hacia la raíz — el nodo 0; la .56.4 documentaba el signo AL REVÉS, auditoría A-2).</param>
        /// <param name="uEscalaV">La compensación de escala V del shader; −1 = automática por ancho (2·perfilMáx/(texel·2.2), piso 0.18 — la curva uEscalaV de la v6.55, aquí por PÍXELES de ancho).</param>
        public static void Dibujar(Vector2[] ptsMundo, int n, float[] perfil, Color[] colores, float uTime, float uEscalaV = -1f)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ)
            {
                return;
            }
            if (ptsMundo == null || perfil == null || colores == null)
            {
                return;
            }
            if (perfil.Length < n || colores.Length < n || ptsMundo.Length < n)
            {
                // v6.56.5 — LA AUDITORÍA A (MENOR-4): faltaba ptsMundo en el
                // clamp — un llamador futuro con n mayor que la espina leía
                // más allá del array (los 5 actuales pasan tamaños exactos,
                // pero el contrato lo promete para cualquiera).
                n = Math.Min(Math.Min(Math.Min(n, perfil.Length), colores.Length), ptsMundo.Length);
            }
            if (n < 2)
            {
                return;
            }

            Texture2D carne = DefinitivasLib.TexRuido;
            Texture2D remolino = DefinitivasLib.TexRemolino;
            Texture2D rampa = DefinitivasLib.TexRampa;
            Texture2D bits = DefinitivasLib.TexBits;

            // 1) EL REMUESTREO — 128 puntos, rotaciones por tangente (la
            //    curva del llamador entra al strip con la densidad del RC2).
            int N = _pts.Length;
            for (int k = 0; k < N; k++)
            {
                float f = (float)k / (N - 1) * (n - 1);
                int i0 = (int)f;
                if (i0 > n - 2)
                {
                    i0 = n - 2;
                }
                float fr = f - i0;
                int i1 = i0 + 1;
                _pts[k] = Vector2.Lerp(ptsMundo[i0], ptsMundo[i1], fr);
                Vector2 d = ptsMundo[i1] - ptsMundo[i0];
                if (d.LengthSquared() < 1e-6f)
                {
                    d = Vector2.UnitX;
                }
                _rotas[k] = MathF.Atan2(d.Y, d.X);
            }

            // 2) uEscalaV AUTOMÁTICA por ancho (la compensación del RC2 —
            //    la del RC2 era por POTENCIA (clamp(realSize)); aquí lo que
            //    varía es el ANCHO del arma: cada texel debe medir ≥2.2 px.
            float maxPerfil = 0.001f;
            for (int i = 0; i < n; i++)
            {
                if (perfil[i] > maxPerfil)
                {
                    maxPerfil = perfil[i];
                }
            }
            if (uEscalaV < 0f)
            {
                float texel = ((carne != null) ? carne.Height : 256);
                uEscalaV = MathHelper.Clamp(maxPerfil * 2f / (texel * 2.2f), 0.18f, 1f);
            }

            _perfilActivo = perfil;
            _coloresActivo = colores;
            _nActivo = n;

            Effect fx = (carne != null && remolino != null && rampa != null && bits != null) ? ObtenerShader() : null;
            if (fx != null)
            {
                // 3) EL LOTE ADITIVO (la cura de las franjas negras): lo
                //    encolado del llamador queda DEBAJO; el dispositivo pasa
                //    a aditivo para el DrawTrail y vuelve al pase vanilla.
                VFXCore.CerrarLoteSiAbierto();
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                gd.BlendState = BlendState.Additive;
                try
                {
                    _strip.PrepareStripWithProceduralPadding(_pts, _rotas,
                        new VertexStrip.StripColorFunction(StripColor),
                        new VertexStrip.StripHalfWidthFunction(StripWidth),
                        -Main.screenPosition, true, true);
                    fx.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                    fx.Parameters["uTexturaCarne"].SetValue((Texture)carne);
                    fx.Parameters["uTexturaSegunda"].SetValue((Texture)remolino);
                    fx.Parameters["uTexturaRampa"].SetValue((Texture)rampa);
                    fx.Parameters["uTexturaBits"].SetValue((Texture)bits);
                    fx.Parameters["uTime"].SetValue(uTime);
                    fx.Parameters["uEscalaV"].SetValue(uEscalaV);
                    fx.CurrentTechnique.Passes[0].Apply();
                    _strip.DrawTrail();
                    Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                }
                finally
                {
                    gd.BlendState = BlendState.AlphaBlend;
                    // v6.56.5 — LA AUDITORÍA B (MENOR-1): el Reabrir vivía
                    // FUERA del finally — una excepción dentro del try dejaba
                    // el lote CERRADO (ya se había corrido CerrarLoteSiAbierto)
                    // y tML mata al proyectil que dibuja así. La cura ahora
                    // también corre por el camino de la excepción.
                    VFXCore.ReabrirLoteVanilla();
                }
                return;
            }

            // 4) EL FALLBACK ×3 (la red de seguridad de la casa — el mismo
            //    del RC2: carne 420/1.0 · remolino 240/1.9 · núcleo 380/2.6
            //    V 0.36..0.64), con los colores POR NODO del llamador.
            //    (v6.56.5, auditoría A-3, documentado: el fallback NO consume
            //    uTime — usa flujos fijos con GlobalTime. Solo visible si el
            //    .fxc falla; los sentidos de hervor de GPU y fallback pueden
            //    discrepar en los conductos que negaron uTime.)
            Vector2 sp = Main.screenPosition;
            for (int k = 0; k < NFallback; k++)
            {
                float f = (float)k / (NFallback - 1) * (n - 1);
                int i0 = (int)f;
                if (i0 > n - 2)
                {
                    i0 = n - 2;
                }
                float fr = f - i0;
                int i1 = i0 + 1;
                _ptsF[k] = Vector2.Lerp(ptsMundo[i0], ptsMundo[i1], fr) - sp;
                _semisF[k] = MathF.Max(MathHelper.Lerp(perfil[i0], perfil[i1], fr), 1f);
                _coloresF[k] = Color.Lerp(colores[i0], colores[i1], fr);
            }
            Multiplicar(_semisF, 0.7f, _semisFA);
            Multiplicar(_semisF, 0.4f, _semisFB);
            RayoStrip.AbrirLote();
            try
            {
                if (carne != null)
                {
                    // LA CARNE (1.0×): los colores por nodo del llamador — el gradiente viajero
                    RayoStrip.FajaTexturadaEnLote(_ptsF, _semisF, _coloresF, Color.White,
                        carne, 420f, 1f, Main.GlobalTimeWrappedHourly, 0f, 1f);
                }
                if (remolino != null)
                {
                    // EL REMOLINO (0.7×): la carne que hierve — espuma blanca sobre el gradiente
                    RayoStrip.FajaTexturadaEnLote(_ptsF, _semisFA, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.42f),
                        remolino, 240f, 1.9f, Main.GlobalTimeWrappedHourly);
                }
                if (carne != null)
                {
                    // EL NÚCLEO EN BANDA (0.4×): la vena caliente
                    RayoStrip.FajaTexturadaEnLote(_ptsF, _semisFB, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f),
                        carne, 380f, 2.6f, Main.GlobalTimeWrappedHourly, 0.36f, 0.64f);
                }
            }
            finally
            {
                RayoStrip.CerrarLote();
            }
        }

        private static Color StripColor(float progress)
        {
            float f = progress * (_nActivo - 1);
            int i0 = (int)f;
            if (i0 > _nActivo - 2)
            {
                i0 = _nActivo - 2;
            }
            if (i0 < 0)
            {
                i0 = 0;
            }
            return Color.Lerp(_coloresActivo[i0], _coloresActivo[i0 + 1], f - i0);
        }

        private static float StripWidth(float progress)
        {
            float f = progress * (_nActivo - 1);
            int i0 = (int)f;
            if (i0 > _nActivo - 2)
            {
                i0 = _nActivo - 2;
            }
            if (i0 < 0)
            {
                i0 = 0;
            }
            return MathF.Max(MathHelper.Lerp(_perfilActivo[i0], _perfilActivo[i0 + 1], f - i0), 0.35f);
        }

        private static void Multiplicar(float[] src, float k, float[] destino)
        {
            for (int i = 0; i < src.Length; i++)
            {
                destino[i] = src[i] * k;
            }
        }

        // ==================== LA ABERRACIÓN CROMÁTICA ====================
        /// <summary>
        /// EL BORDE PRISMÁTICO (la PASADA 6 de la vieja HazFusionEspina,
        /// extraída a su propia casa): dos tiras finas rojo/cian a ±(2 +
        /// perfil·10%) px del eje, 0.30× de semiancho, período 300 — se
        /// dibujan por la vía CPU (fina y lisa: su riesgo de picos es nulo).
        /// El Veredicto y el Juicio la conservan; el Alba nace limpio.
        /// </summary>
        public static void AberracionCromatica(Vector2[] ptsMundo, int n, float[] perfil,
            Color rojo, Color cian, float flujo, float tiempo, float intensidad)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ || intensidad <= 0.35f)
            {
                return;
            }
            if (ptsMundo == null || perfil == null || n < 2 || perfil.Length < n)
            {
                return;
            }
            Texture2D carne = DefinitivasLib.TexRuido;
            if (carne == null)
            {
                return;
            }
            Vector2 sp = Main.screenPosition;
            int N = _ptsR.Length;
            for (int k = 0; k < N; k++)
            {
                // resample del llamador a 64 (el pozo se llena COMPLETO —
                // nada de entries huérfanas de frames viejos)
                float f = (float)k / (N - 1) * (n - 1);
                int i0 = (int)f;
                if (i0 > n - 2)
                {
                    i0 = n - 2;
                }
                float fr = f - i0;
                int i1 = i0 + 1;
                Vector2 p = Vector2.Lerp(ptsMundo[i0], ptsMundo[i1], fr) - sp;
                Vector2 d = ptsMundo[i1] - ptsMundo[i0];
                Vector2 perp = new(-d.Y, d.X);
                if (perp.LengthSquared() < 0.0001f)
                {
                    perp = Vector2.UnitY;
                }
                else
                {
                    perp = Vector2.Normalize(perp);
                }
                float ancho = MathHelper.Lerp(perfil[i0], perfil[i1], fr);
                float desplaz = 2f + ancho * 0.10f;
                _ptsR[k] = p + perp * desplaz;
                _ptsC[k] = p - perp * desplaz;
                _semisE[k] = MathF.Max(ancho * 0.30f, 0.5f);
            }
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(_ptsR, _semisE, rojo, carne, 300f, 1.4f * flujo, tiempo, 0.40f, 0.60f);
                RayoStrip.FajaTexturadaEnLote(_ptsC, _semisE, cian, carne, 300f, 1.4f * flujo, tiempo, 0.40f, 0.60f);
            }
            finally
            {
                RayoStrip.CerrarLote();
            }
        }

        // ==================== LOS DESTELLOS DE LOS SOLES RÚNICOS ====================
        //  La letra: «en el momento de carga… los brillos de los soles
        //  rúnicos se reúnen a gran velocidad en el centro antes del
        //  disparo y luego cuando el rayo dispara, salen de forma aleatoria
        //  estos brillos hacia la dirección del rayo» (el reemplazo de la
        //  aguja de puntería, que avanzaba y retrocedía — «se ve horrible»).
        //  Cero Dust (ley §0) y determinista por Hash01 (ley §0.4): el
        //  sistema de partículas de la casa (SparkleStar + FadeIn/FadeOut/
        //  EmitLight — el patrón SpawnLibraryTwinkles de SunProjectile).

        // ====================================================================
        //  v6.56.6 — LOS SOLES RÚNICOS DE LA CARGA (la letra del usuario:
        //  «sobre los brillos de soles runicos que deben ser absorbidos por
        //  el arma al realizar la carga… debes usar los mismos brillos que
        //  usa el accesorio de El brillo de soles runicos»). LA RECETA ES
        //  DE BrilloSolarHalo (DrawTwinkles de los soles de 3 anillos+):
        //  EL DESTELLO DE 4 PUNTAS — dos cápsulas cruzadas de blanco
        //  incandescente + el corazón dorado, que nacen, arden y mueren.
        //  Los antiguos DestellosDeCarga/Estallido (SparkleStar genérico)
        //  MURIERON: no eran «ese brillo».
        // ====================================================================

        /// <summary>EL DESTELLO DE 4 PUNTAS — la firma del sol rúnico:
        /// dos cápsulas cruzadas de blanco incandescente + el corazón
        /// dorado. Se dibuja en el LOTE ADITIVO de VFXCore (el llamador
        /// abre Begin/FlushAdditive). Receta verbatim de BrilloSolarHalo.
        /// [size] en px: cápsulas 2.2·size, núcleo 1.5·size.</summary>
        public static void SolRunico4Puntas(Vector2 pos, float size, float arde,
            float doradoExtra = 0f)
        {
            if (arde <= 0.03f || size <= 0.4f) return;
            float fino = MathF.Max(1.6f, 0.035f * 34f) * arde;
            Color blanco = new Color(255, 248, 225) * (0.55f * arde);
            Color dorado = new Color(255, 214, 120) * ((0.50f + doradoExtra) * arde);
            VFXCore.Quad(pos, blanco, new Vector2(size * 2.2f, fino));
            VFXCore.Quad(pos, blanco, new Vector2(fino, size * 2.2f));
            VFXCore.Quad(pos, dorado, new Vector2(size * 1.5f, size * 1.5f));
        }

        /// <summary>LA CARGA ABSORBE SOLES: los destellos de 4 puntas del
        /// accesorio nacen en la banda elíptica de siempre, pero la banda
        /// ENCOGE con el progreso (se absorben hacia el arma: 170→22 px) y
        /// el ciclo nace-arde-muere ACELERA (el hervidero se aviva). Pura
        /// función de (progreso, tiempo): determinista, sin estado.</summary>
        public static void SolesRunicosCarga(Vector2 centro, float progreso, float tiempo)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            progreso = MathHelper.Clamp(progreso, 0f, 1f);
            const int cuenta = 11;
            float banda = MathHelper.Lerp(170f, 22f, progreso * progreso);
            float velocidad = 0.5f + 2.1f * progreso;      // el hervidero acelera
            for (int i = 0; i < cuenta; i++)
            {
                float h = VFXCore.Hash01(31, 300 + i, 7);
                float vida = (tiempo * velocidad * 0.5f + h * 3f) % 1f;
                float arde = MathF.Sin(vida * MathHelper.Pi);
                if (arde < 0.05f) continue;

                float ang = VFXCore.Hash01(31, 310 + i, 11) * MathHelper.TwoPi
                            + tiempo * (0.07f + 0.35f * progreso);
                float dist = (1.25f + 1.55f * VFXCore.Hash01(31, 320 + i, 13))
                             * banda * (1f - 0.35f * vida);   // la última mirada se acerca
                Vector2 pos = centro + new Vector2(MathF.Cos(ang) * dist,
                    MathF.Sin(ang) * dist * 0.72f);
                float size = (0.30f + 0.25f * VFXCore.Hash01(31, 330 + i, 17))
                             * 34f * (0.55f + 0.45f * progreso);
                SolRunico4Puntas(pos, size, arde);
            }
        }

        /// <summary>EL ESTALLIDO DE SOLES: al disparar, los destellos de 4
        /// puntas salen EXPULSADOS en abanico por la dirección del rayo
        /// (±0.5 rad) y arden muriendo — el arma escupe lo que absorbió.
        /// [edad01] 0→1 durante los primeros ~22 ticks del disparo.</summary>
        public static void SolesRunicosEstallido(Vector2 centro, float angulo, float edad01,
            float tiempo)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            edad01 = MathHelper.Clamp(edad01, 0f, 1f);
            const int cuenta = 16;
            for (int i = 0; i < cuenta; i++)
            {
                float a = angulo + (VFXCore.Hash01(77, 500 + i, 7) - 0.5f) * 1.0f;
                float v = 6f + 9f * VFXCore.Hash01(77, 510 + i, 11);
                float arr = 14f + 26f * VFXCore.Hash01(77, 520 + i, 13);
                float arde = MathF.Sin((1f - edad01) * MathHelper.Pi)
                             * (0.6f + 0.4f * VFXCore.Hash01(77, 530 + i, 17));
                Vector2 pos = centro + new Vector2(MathF.Cos(a), MathF.Sin(a))
                              * (arr + v * edad01 * 22f);
                float size = (0.34f + 0.30f * VFXCore.Hash01(77, 540 + i, 19)) * 34f;
                SolRunico4Puntas(pos, size, arde);
            }
        }
    }
}
