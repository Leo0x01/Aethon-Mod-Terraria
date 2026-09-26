using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// VFXCore — v6.03 — EL NÚCLEO DE LA BIBLIOTECA VISUAL AETHON.
    ///
    /// Idea central: todos los efectos de brillo del mod (coronas, discos,
    /// rayos, halos) se describen como LISTAS DE CUADROS DE LUZ (GlowQuad:
    /// posición en MUNDO, color, escala, rotación) y luego se "vuelcan" al
    /// destino que haga falta con UNA sola llamada:
    ///
    ///   - FlushAdditive(...): dibujo directo aditivo en el mundo
    ///     (proyectiles, efectos de pantalla) — el brillo SUMA, look de neón.
    ///   - AppendToPlayerDraw(...): emisión de DrawData para las capas de
    ///     dibujado del jugador (PlayerDrawLayer) — el camino 100% seguro
    ///     con el pipeline de tML, sin tocar el SpriteBatch del renderer.
    ///
    /// Así un mismo renderizador (p. ej. la corona de arcos) sirve igual en
    /// un proyectil que sobre la cabeza de un jugador, sin duplicar la
    /// matemática y sin riesgo de romper estados de dibujo ajenos.
    ///
    /// El buffer de cuadros es estático y reutilizable: la generación de un
    /// efecto típico no aloca nada (cero GC por frame).
    /// </summary>
    public static class VFXCore
    {
        // ------------------------------------------------------------------
        //  EL CUADRO DE LUZ
        // ------------------------------------------------------------------

        /// <summary>
        /// Un "punto de luz" del tamaño y forma que haga falta: posición en
        /// COORDENADAS DE MUNDO, color (con alfa), escala (x≠y = estirado),
        /// rotación y textura propia (null = SoftGlow). En modo Inmediato el
        /// SpriteBatch envía cada Draw al momento: mezclar texturas en un
        /// mismo volcado es gratis.
        /// </summary>
        public struct GlowQuad
        {
            public Vector2 Position;
            public Color Color;
            public Vector2 Scale;
            public float Rotation;
            public Texture2D Texture;
        }

        /// <summary>Buffer reutilizable de cuadros (evita GC por frame).</summary>
        private static readonly List<GlowQuad> _quads = new List<GlowQuad>(512);

        /// <summary>Prepara el buffer para recibir un efecto nuevo.</summary>
        public static void Begin()
        {
            _quads.Clear();
        }

        /// <summary>Añade un cuadro de luz (posición en coords de mundo).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation = 0f)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = null });
        }

        /// <summary>Añade un cuadro con TEXTURA propia (Ring, GlowOrb...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = 0f, Texture = texture });
        }

        /// <summary>v6.08 — Cuadro con TEXTURA propia Y ROTACIÓN: cintas de
        /// luz orientadas por la tangente (alas de mariposa, colas de
        /// cometa, rastros de acreción...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = texture });
        }

        /// <summary>Añade un cuadro circular (atajo: escala uniforme).</summary>
        public static void Quad(Vector2 position, Color color, float scale)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = new Vector2(scale, scale), Rotation = 0f, Texture = null });
        }

        /// <summary>
        /// v6.41 — EL CUADRO ESTIRADO DE A A B (la línea de la casa): un
        /// quad de <paramref name="grosor"/> px de ancho cubriendo TODO el
        /// segmento A→B (posición = punto medio, rotación = ángulo del
        /// segmento, escala X = longitud). EL primitivo de los rayos, las
        /// líneas de telegraph y los beams — antes cada arma lo recomponía
        /// a mano con senos y cosenos.
        /// </summary>
        public static void Line(Vector2 a, Vector2 b, Color color, float grosor)
        {
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 0.5f || grosor <= 0f || color.A == 0) return;

            _quads.Add(new GlowQuad
            {
                Position = a + delta * 0.5f,
                Color = color,
                Scale = new Vector2(len, grosor),
                Rotation = delta.ToRotation(),
                Texture = null,
            });
        }

        /// <summary>Cuántos cuadros lleva el buffer (diagnóstico).</summary>
        public static int QuadCount => _quads.Count;

        // ------------------------------------------------------------------
        //  TEXTURAS COMPARTIDAS (resolución diferida: Asset, no .Value)
        // ------------------------------------------------------------------

        private static Asset<Texture2D> _softGlow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _glowOrb;

        /// <summary>Textura de brillo radial suave (la workhorse de la librería).</summary>
        public static Texture2D SoftGlow
        {
            get
            {
                if (_softGlow == null)
                    _softGlow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow");
                return _softGlow.Value;
            }
        }

        /// <summary>Orbe con NÚCLEO SÓLIDO y borde suave (para vacíos negros
        /// absolutos y cuerpos compactos de luz).</summary>
        public static Texture2D GlowOrb
        {
            get
            {
                if (_glowOrb == null)
                    _glowOrb = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb");
                return _glowOrb.Value;
            }
        }

        /// <summary>Anillo fino (anillo de fotones, ecos, halos anulares).</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring == null)
                    _ring = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Ring");
                return _ring.Value;
            }
        }

        private static Texture2D _pixel;

        /// <summary>
        /// v6.50.22 — EL PIXEL BLANCO 1×1 DEL MOTOR (TextureAssets.MagicPixel
        /// de vanilla): EL PINCEL DEL DIBUJO 100% CÓDIGO. No es un asset del
        /// mod ni un sprite de rayo — es la primitiva de rectángulo sólido
        /// que el propio motor expone (el mismo que usa el cursor, las
        /// barras y mil detalles de vanilla). Con él, un filamento eléctrico
        /// se construye APILANDO PASADAS SÓLIDAS de ancho decreciente (la
        /// receta del lightning 466 de vanilla): el degradado transversal ES
        /// LA SUMA de las pasadas — cero textura de banda, cero arte.
        /// </summary>
        public static Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                    _pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                return _pixel;
            }
        }

        /// <summary>
        /// Tamaño de cuadro para que el TRAZO VISIBLE de la textura Ring
        /// (1024px, círculo gráfico a ~0.92 del semiancho) caiga en el radio
        /// pedido: el tamaño final del sprite debe ser ~2.17× ese radio.
        /// </summary>
        public static Vector2 RingQuadSize(float visibleRadius)
        {
            float s = visibleRadius * 2.174f;
            return new Vector2(s, s);
        }

        // ------------------------------------------------------------------
        //  VOLCADO 1 — DIBUJO DIRECTO ADITIVO (mundo / proyectiles)
        // ------------------------------------------------------------------

        /// <summary>
        /// Vuelca el buffer actual con blending ADITIVO: el brillo suma sobre
        /// lo que ya hay en pantalla (neón real). Los cuadros están en coords
        /// de mundo; se les resta Main.screenPosition aquí, una sola vez.
        ///
        /// Semántica de escala: el cuadro mide Scale píxeles FINALES en
        /// pantalla (ancho×alto), sea cual sea la resolución de la textura.
        /// </summary>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="endActiveBatch">True si puede haber un batch abierto
        /// que haya que cerrar antes (p. ej. venimos de un PreDraw).</param>
        public static void FlushAdditive(Texture2D texture = null, bool endActiveBatch = true)
        {
            if (_quads.Count == 0) return;
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            Texture2D defaultTex = texture ?? SoftGlow;

            // v6.49 — EL END TAMBIÉN BLINDADO (hallazgo AUD-C): el End del
            // lote del llamador vivía FUERA del try/finally — si lanzaba
            // (lote ya cerrado por un consumidor del patrón viejo), el
            // finally NUNCA corría: _quads quedaba sin limpiar y los
            // cuadros muertos se re-volcaban y re-contaban CADA frame.
            // Ahora TODO el vuelva es atómico: el búfer se limpia pase lo
            // que pase, incluso en el error.
            try
            {
                // v6.50.11 — SONDA: el End del lote del llamador sin
                // first-chance (antes: End pelado que lanzaba si el
                // consumidor ya lo había cerrado — la excepción escapaba
                // del try/finally al llamador tras la limpieza).
                if (endActiveBatch)
                    CerrarLoteSiAbierto();

                // v6.41 — EL VOLCADO BLINDADO (try/finally): si UN Draw lanza
                // (dispositivo perdido, textura nula por descarga caliente), el
                // lote ANTERIOR quedaba ABIERTO para siempre → TODO el render
                // del juego se corrompía hasta relogear, y el búfer nunca se
                // limpiaba (los cuadros muertos se re-volcaban cada frame).
                // Ahora: el End y la limpieza se garantizan pase lo que pase.
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 screen = Main.screenPosition;
                for (int i = 0; i < _quads.Count; i++)
                {
                    GlowQuad q = _quads[i];
                    if (q.Color.A == 0) continue;

                    Texture2D tex = q.Texture ?? defaultTex;
                    Vector2 invTex = new Vector2(1f / tex.Width, 1f / tex.Height);
                    Main.spriteBatch.Draw(tex, q.Position - screen, null,
                        q.Color, q.Rotation, tex.Size() * 0.5f, q.Scale * invTex, SpriteEffects.None, 0f);
                }
            }
            finally
            {
                // v6.50.11 — sonda: cierra NUESTRO lote aditivo (y solo si
                // sigue vivo — cero first-chance).
                CerrarLoteSiAbierto();
                ContarQuads(_quads.Count);
                _quads.Clear();
            }
        }

        /// <summary>
        /// Vuelca el buffer como DrawData dentro de la capa de dibujado de un
        /// jugador (PlayerDrawLayer): se añaden al DrawDataCache y tML los
        /// compone con el resto del jugador — el camino oficial, sin tocar
        /// el estado del renderer. Los cuadros van en coords de MUNDO.
        /// </summary>
        /// <param name="drawInfo">El PlayerDrawSet de la capa.</param>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="effects">Efectos de espejado del jugador (para
        /// respetar su dirección).</param>
        public static void AppendToPlayerDraw(ref PlayerDrawSet drawInfo, Texture2D texture = null,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (_quads.Count == 0) return;

            Texture2D defaultTex = texture ?? SoftGlow;
            Vector2 screen = Main.screenPosition;

            for (int i = 0; i < _quads.Count; i++)
            {
                GlowQuad q = _quads[i];
                if (q.Color.A == 0) continue;

                Texture2D tex = q.Texture ?? defaultTex;
                Vector2 invTex = new Vector2(1f / tex.Width, 1f / tex.Height);

                // La posición de DrawData vive en coords de PANTALLA.
                Vector2 pos = q.Position - screen;

                drawInfo.DrawDataCache.Add(new DrawData(
                    tex, pos, null, q.Color, q.Rotation,
                    tex.Size() * 0.5f, q.Scale * invTex, effects));
            }

            _quads.Clear();
        }

        // ==================================================================
        //  SECCIÓN v6.50.11 · LA SONDA DE LOTE (el fin de las first-chance)
        // ==================================================================

        /// <summary>
        /// v6.50.11 — ¿Tiene Main.spriteBatch un Begin vivo?
        ///
        /// LA HISTORIA: el End defensivo de la casa ({try { End } catch {}})
        /// nunca dejó escapar nada, pero cada disparo sobre un lote ya
        /// cerrado lanzaba una InvalidOperationException FIRST-CHANCE — y
        /// tML 2026.07 LAS REGISTRA (AppDomain.FirstChanceException → el
        /// WARN "Excepción silenciosa", deduplicado una vez por stack
        /// único: el client.log v6.50.6 del usuario llevaba 27 de NUESTROS
        /// End defensivos, TODAS capturadas por el propio catch — ruido
        /// puro que ensuciaba el diagnóstico de cualquier otra cosa).
        ///
        /// La sonda PREGUNTA antes de tocar: el campo privado "beginCalled"
        /// del SpriteBatch de FNA (verificado en el decompile del tML
        /// 2026.07.3.0 real), cacheado por reflexión una sola vez. Si un
        /// FNA futuro lo renombrara, la sonda devuelve false y los
        /// ayudantes caen al End defensivo clásico (compatible).
        /// </summary>
        private static readonly System.Reflection.FieldInfo _fiBeginCalled =
            typeof(SpriteBatch).GetField("beginCalled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>True si Main.spriteBatch tiene un Begin vivo (sonda).</summary>
        public static bool LoteAbierto =>
            _fiBeginCalled != null && Main.spriteBatch != null &&
            _fiBeginCalled.GetValue(Main.spriteBatch) is bool abierto && abierto;

        /// <summary>
        /// Cierra el lote del juego SOLO si hay un Begin vivo — cero
        /// excepciones, cero first-chance en el log (el End defensivo de
        /// la casa SIN su costo). DEVUELVE true si cerró un lote vivo (el
        /// rastreo exacto del "lote ajeno" para quien deba reapertura).
        /// Fallback: si la sonda no está disponible (FNA futuro), el End
        /// defensivo clásico.
        /// </summary>
        public static bool CerrarLoteSiAbierto()
        {
            if (_fiBeginCalled == null)
            {
                try { Main.spriteBatch.End(); return true; }
                catch { return false; }
            }
            if (LoteAbierto)
            {
                Main.spriteBatch.End();
                return true;
            }
            return false;
        }

        /// <summary>
        /// v6.50.11 — EL CONTRATO DE CURACIÓN. Reabre el lote con los
        /// parámetros EXACTOS del pase de entidades de vanilla
        /// (Main.DrawProjectiles, medido en el decompile: Deferred ·
        /// AlphaBlend · DefaultSamplerState · None · Main.Rasterizer ·
        /// null · Main.Transform — el patrón v6.50.2).
        ///
        /// LA LECCIÓN DEL client.log: el restore condicional de la casa
        /// ({if (wasActive) Begin}) dejaba el lote CERRADO cuando el
        /// PreDraw lo encontró cerrado — "restauración exacta" que en
        /// realidad devolvía el veneno: tML mata al proyectil que dibuja
        /// con el lote cerrado (try/catch de DrawProjectiles →
        /// projectile.active = false) y el End final del bucle lanza. Un
        /// PreDraw de la casa SIEMPRE sale con el lote ABIERTO y válido —
        /// si llegó roto (mod ajeno), se CURA. Idempotente por sonda: si
        /// ya hay un Begin vivo no lo pisa (Begin sobre Begin lanza en
        /// FNA) — el estado abierto preexistente se respeta y el Begin se
        /// envuelve a prueba de todo.
        /// </summary>
        public static void ReabrirLoteVanilla()
        {
            if (_fiBeginCalled != null && LoteAbierto)
                return; // ya hay un Begin vivo: no lo pisamos
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.Transform);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        //  HELPERS DE MOVIMIENTO (los "latidos" de la librería)
        // ------------------------------------------------------------------

        /// <summary>Respiración: 0..1→0.94..1.06 con la fase pedida.</summary>
        public static float Breathe(float time, float speed = 2.2f, float phase = 0f, float amplitude = 0.06f)
        {
            return 1f + amplitude * (float)Math.Sin(time * speed + phase);
        }

        /// <summary>Oscilación suave -1..1 (balanceo, flotación).</summary>
        public static float Sway(float time, float speed = 0.9f, float phase = 0f)
        {
            return (float)Math.Sin(time * speed + phase);
        }

        /// <summary>
        /// Hash determinista [0,1): la MISMA secuencia en todas las máquinas
        /// sin sincronizar nada (rayos, destellos por índice).
        /// </summary>
        public static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Elipse paramétrica: punto en el ángulo t (rad).</summary>
        public static Vector2 Ellipse(Vector2 center, float semiMajor, float semiMinor, float tilt, float t)
        {
            float ct = (float)Math.Cos(t);
            float st = (float)Math.Sin(t);
            // Punto en la elipse sin girar (eje mayor = X).
            Vector2 local = new Vector2(semiMajor * ct, semiMinor * st);
            float cR = (float)Math.Cos(tilt);
            float sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        // ==================================================================
        //  v6.31 — EL FILTRO DE OBJETIVOS: EL MISMO DE TERRARIA BASE
        // ==================================================================

        /// <summary>
        /// ¿Es este NPC un objetivo VÁLIDO para el daño manual de la casa
        /// (escuela A)? v6.31 — LA PETICIÓN LITERAL DEL USUARIO: "que el
        /// filtro sea el mismo que usan las armas de Terraria base y otros
        /// mods". ES EL PREDICADO EXACTO de la puerta de daño de vanilla
        /// (Projectile.cs, la puerta principal proyectil→NPC, medida sobre
        /// el decompile real):
        /// <code>
        ///   npc.active &amp;&amp; !npc.dontTakeDamage &amp;&amp; !npc.friendly
        /// </code>
        /// Consecuencias medibles (todas = comportamiento vanilla):
        ///   · EL TARGET DUMMY CUENTA (muestra números; immortal, su vida
        ///     jamás baja — exactamente como con las armas base).
        ///   · Los NPC AMISTOSOS (pueblos, atados) NO reciben daño — igual
        ///     que una espada base los atraviesa sin herirlos.
        ///   · Los NPC con dontTakeDamage (escenas/inmunes de evento) se
        ///     respetan.
        /// (La vacuna del Guía [type 22 + killGuide] y el gate de i-frames
        /// por jugador viven en el motor de vanilla; nuestro daño manual
        /// lleva SU PROPIA cadencia por diseño — los cooldowns de la casa.)
        /// </summary>
        public static bool EsObjetivo(NPC npc)
            => npc != null && npc.active && !npc.dontTakeDamage && !npc.friendly;

        // ==================================================================
        //  v6.31 — EL MOTOR v2: capas de oclusión + janitor + calidad + presupuesto
        //  (la guía de ingeniería de la super investigación: lo que los mods
        //  top hacen y nuestro motor no tenía)
        // ==================================================================

        /// <summary>
        /// LAS CAPAS DE DIBUJO del motor (v6.31): los efectos que OCULLEN (el
        /// vacío de un desgarro, el horizonte de un agujero negro) necesitan
        /// dibujarse DEBAJO de los NPCs — que el cuerpo del enemigo TAPE el
        /// horizonte de sucesos vende la profundidad que el aditivo no puede.
        /// </summary>
        public enum VFXLayer
        {
            /// <summary>Debajo de todo (detrás de tiles y NPCs).</summary>
            DetrasDeTodo = 0,
            /// <summary>Sobre los tiles, DEBAJO de los NPCs (la capa de la oclusión).</summary>
            DetrasDeNPCs = 1,
            /// <summary>Sobre los NPCs (el brillo final).</summary>
            SobreNPCs = 2,
        }

        // (delegados por capa: el llamador registra su dibujo del frame).
        // v6.50.5 — SIN readonly: el barrendero de Unload lo anula por
        // reflexión y .NET 8 prohíbe escribir campos initonly (la traza
        // del client.log v6.50.4 — FieldAccessException) — el ancla se
        // quedaba viva tras la descarga ("mod class still using memory").
        private static List<Action<SpriteBatch>>[] _capas =
        {
            new List<Action<SpriteBatch>>(32),
            new List<Action<SpriteBatch>>(32),
            new List<Action<SpriteBatch>>(32),
        };

        /// <summary>
        /// REGISTRA un dibujo con CAPA para este frame (v6.31): el delegado se
        /// ejecuta en <see cref="FlushOcclusion"/> en orden de capa. El dibujo
        /// debe abrir/cerrar SUS lotes (el batch que recibe ya está preparado
        /// en NonPremultiplied). Para la luz normal sigue habiendo
        /// <see cref="FlushAdditive"/> — esto es SOLO para lo que OCULLE.
        /// </summary>
        public static void RegisterLayer(Action<SpriteBatch> draw, VFXLayer layer)
        {
            if (draw == null || Main.netMode == NetmodeID.Server) return;
            _capas[(int)layer].Add(draw);
            if (_capas[(int)layer].Count > 64)       // techo de seguridad
                _capas[(int)layer].RemoveAt(0);
        }

        /// <summary>
        /// VUELCA las capas registradas (v6.31): los dibujos oclusivos en su
        /// orden (detrás de todo → detrás de NPCs). El llamador decide DÓNDE
        /// del pipeline lo invoca (p. ej. un ModSystem.PostDrawTiles para que
        /// los NPCs tapen la oclusión). Limpia los registros del frame.
        /// </summary>
        public static void FlushOcclusion()
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int c = 0; c < _capas.Length; c++)
            {
                if (_capas[c].Count == 0) continue;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < _capas[c].Count; i++)
                {
                    try { _capas[c][i](Main.spriteBatch); }
                    catch { }
                }
                Main.spriteBatch.End();
                _capas[c].Clear();
            }
            Janitor();
        }

        /// <summary>
        /// EL CONSERJE DE ESTADO (v6.31 — la lección de higiene de pipeline de
        /// la super investigación): los shaders que registran texturas en los
        /// SLOTS 1..3 del dispositivo los dejan PEGADOS si el frame termina a
        /// mitad de pase (pausa, teleport, excepción) — y el siguiente dibujo
        /// que use esos slots sale CORROMPIDO. Este barrido los anula al
        /// terminar el volcado de capas: un solo lugar, cero víctimas.
        /// </summary>
        private static void Janitor()
        {
            try
            {
                var device = Main.graphics?.GraphicsDevice;
                if (device == null) return;
                device.Textures[1] = null;
                device.Textures[2] = null;
                device.Textures[3] = null;
                // v6.50.3 — FIX (los samplers quedaban PEGADOS): los shaders
                // del sol registran SamplerStates[1..2] = LinearWrap y NADIE
                // los restauraba (el conservaje cubría solo las texturas —
                // un draw futuro del slot 1 sin sampler propio muestreaba en
                // WRAP). LinearClamp: el neutro de los slots de ruido.
                device.SamplerStates[1] = SamplerState.LinearClamp;
                device.SamplerStates[2] = SamplerState.LinearClamp;
                device.SamplerStates[3] = SamplerState.LinearClamp;
            }
            catch { }
        }

        // --- LA PUERTA DE CALIDAD ---

        /// <summary>Las familias de efectos que PUEDEN no dibujarse según la
        /// configuración del jugador (v6.31 — el quality-gate de la casa).</summary>
        public enum CalidadFX
        {
            /// <summary>Post-proceso de pantalla (bloom propio, aberración fuerte).</summary>
            PostProceso,
            /// <summary>Warp/distorsión de pantalla (lentes, calor).</summary>
            WarpPantalla,
            /// <summary>Brillos HDR apilados.</summary>
            Bloom,
            /// <summary>Lluvias de partículas cosméticas densas.</summary>
            ParticulasAltas,
        }

        private static bool _renderEspecial;
        private static uint _frameDeCalidad;

        /// <summary>
        /// ¿Está PERMITIDA esta familia de efectos? (v6.31): la puerta que
        /// hay que consultar ANTES de cualquier post-proceso — en el menú o
        /// sin dispositivo el post-proceso rompe (la lección de seguridad de
        /// render de la investigación; la detección de iluminación Retro/
        /// Trippy no es API pública en tML 2026.07: el gate es conservador
        /// con lo disponible, listo para ensancharse).
        /// </summary>
        public static bool CalidadPermitida(CalidadFX f)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDeCalidad)
            {
                _frameDeCalidad = frame;
                try { _renderEspecial = Main.gameMenu || Main.graphics?.GraphicsDevice == null; }
                catch { _renderEspecial = true; }
            }
            return !_renderEspecial;
        }

        // --- EL PRESUPUESTO DE CUADROS ---

        private static int _quadsDelFrame;
        private static uint _frameDelPresupuesto;

        /// <summary>
        /// EL PRESUPUESTO (v6.31 — anti-"abrumador"): cuenta los cuadros
        /// vuelcados este frame; los llamadores COSMÉTICOS consultan esto
        /// antes de emitir (si hay 3 jefes con bruma a la vez, las lluvias de
        /// partículas decorativas se saltan solas). Techo ~24000 quads/frame
        /// — que desde v6.34 RESPIRA: se multiplica por <see cref="FactorCalidad"/>
        /// (con factor 1 el techo es el de siempre, idéntico).
        /// </summary>
        public static bool Presupuesto(int quadsQueQuieroEmitir)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDelPresupuesto)
            {
                _frameDelPresupuesto = frame;
                _quadsDelFrame = 0;
            }
            // v6.34 — EL TECHO RESPIRA: el factor adaptativo multiplica el
            // límite efectivo (factor 1 → 24000 exactos, comportamiento
            // IDÉNTICO al clásico; factor 0.5 → la mitad de techo).
            return _quadsDelFrame + quadsQueQuieroEmitir <= (int)(24000f * _factorCalidad);
        }

        /// <summary>Registra consumo del presupuesto (lo llama FlushAdditive).</summary>
        private static void ContarQuads(int n)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDelPresupuesto)
            {
                _frameDelPresupuesto = frame;
                _quadsDelFrame = 0;
            }
            _quadsDelFrame += n;
        }

        // --- v6.34 — EL PRESUPUESTO ADAPTATIVO (la calidad que respira) ---

        /// <summary>
        /// La media móvil EXPONENCIAL de los FPS (0.9·prev + 0.1·último):
        /// nace en 60 — un hijack de un frame (carga, pausa) NO tira la
        /// calidad; solo el hundimiento sostenido cuenta.
        /// </summary>
        private static float _fpsSuave = 60f;

        /// <summary>
        /// El factor de calidad ACTUAL (0.5..1): multiplica el techo de
        /// <see cref="Presupuesto"/>. Arranca en 1 (sin recortes).
        /// </summary>
        private static float _factorCalidad = 1f;

        /// <summary>
        /// El factor de calidad ACTUAL (0.5..1) — público para que los
        /// renderizadores que quieran acompañen la respiración (escalar sus
        /// propias lluvias de partículas, etc.). 1 = el comportamiento de
        /// siempre.
        /// </summary>
        public static float FactorCalidad => _factorCalidad;

        /// <summary>
        /// v6.34 — REPORTA los FPS reales del juego. v6.49 (auditoría
        /// AUD-C): la llama CalidadFpsSystem.PostUpdateEverything — el
        /// "nadie la llama" del comentario viejo era doc-rot.
        /// Lógica: media móvil EXPONENCIAL (0.9·prev + 0.1·fps) y el factor
        /// de calidad RESPIRA con ella — si el promedio cae por debajo de
        /// 45 FPS, el factor BAJA 0.05 por reporte (suelo 0.5: ni a la mitad
        /// del techo se recorta más); si supera los 55, SUBE 0.02 por reporte
        /// (techo 1: recuperación LENTA a propósito — se pierde calidad en un
        /// pico y se recupera con calma, sin ver el yo-yó). El factor
        /// multiplica el límite efectivo del presupuesto: LA CALIDAD SE
        /// ADAPTA SOLA — si los FPS caen, el mod adelgaza sus efectos; nadie
        /// tiene que configurar nada.
        /// </summary>
        public static void ReportarFps(float fps)
        {
            // Robustez: un NaN/infinito envenenaría la media PARA SIEMPRE
            // (0.9·NaN = NaN) y dejaría el factor congelado.
            if (float.IsNaN(fps) || float.IsInfinity(fps)) return;

            _fpsSuave = _fpsSuave * 0.9f + fps * 0.1f;
            if (_fpsSuave < 45f)
                _factorCalidad = Math.Max(_factorCalidad - 0.05f, 0.5f);
            else if (_fpsSuave > 55f)
                _factorCalidad = Math.Min(_factorCalidad + 0.02f, 1f);
        }

        // ------------------------------------------------------------------
        //  v6.49 — LA HIGIENE DEL NÚCLEO (hallazgo AUD-C: "todas las
        //  hermanas tienen Reiniciar; el núcleo no"). El búfer, las
        //  texturas perezosas y el presupuesto se sueltan al recargar
        //  el mod / cambiar de mundo — lo llama DiagnosticoVFXSystem
        //  (que también es el overlay F8 de la casa).
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.49 — LA LIMPIEZA DEL NÚCLEO: búfer vacío, presupuesto en
        /// cero, factor y FPS de vuelta al nacimiento. Para Unload y
        /// OnWorldUnload (las texturas pediosas se repiden solas).
        /// </summary>
        public static void Reiniciar()
        {
            try { _quads.Clear(); } catch { }
            _quadsDelFrame = 0;
            _frameDelPresupuesto = 0;
            _factorCalidad = 1f;
            _fpsSuave = 60f;
        }

        /// <summary>v6.49 — EL DIAGNÓSTICO del núcleo (lo pinta el overlay F8).</summary>
        public static int QuadsDelFrame => _quadsDelFrame;
    }

    /// <summary>
    /// VFXCoreSystem — v6.50.2 — FIX: EL SUBSISTEMA QUE NADIE LLAMABA.
    /// RegisterLayer/FlushOcclusion/Janitor existían desde v6.31 como
    /// la protección documentada (volcar las capas de oclusión y anular
    /// las texturas que RuneSunRenderer/CicloEstelarRenderer/SunProjectile/
    /// BlackHoleProjectile dejan BIND-EADAS en los slots 1..3 del
    /// dispositivo), pero grep-verificado NADIE invocaba la puerta
    /// pública: la protección jamás corrió (subsistema muerto — el doc
    /// prometía higiene que no existía).
    ///
    /// Este ModSystem la CONECTA en PostDrawTiles — el punto exacto que
    /// la documentación de <see cref="VFXCore.FlushOcclusion"/> propone
    /// («p. ej. un ModSystem.PostDrawTiles para que los NPCs tapen la
    /// oclusión»): vanilla llama SystemLoader.PostDrawTiles() justo
    /// después de cerrar el lote de tiles y ANTES de dibujar los
    /// proyectiles (decompile: spriteBatch.End() → PostDrawTiles() →
    /// DrawProjectiles()), así que el barrido del Janitor despeja los
    /// slots ANTES del pase de entidades de cada frame (las texturas
    /// bind-eadas por los renderizadores del frame anterior ya no
    /// contaminan el dibujado siguiente).
    ///
    /// EL GUARD (netMode == Server) es el MISMO de todo el stack VFX de
    /// la casa (ParticleManager.PostDrawTiles, RuneSun, auras…): devuelve
    /// al servidor dedicado Y AL HOST del listen-server (netMode 2), donde
    /// este mod no renderiza nada — no hay slots que limpiar ahí
    /// (FlushOcclusion se auto-guarda igual, doble puerta). En el menú no
    /// hay mundo ni renderizadores registrando nada.
    /// </summary>
    public class VFXCoreSystem : ModSystem
    {
        /// <summary>
        /// EL PUNTO ÚNICO DEL VOLCADO DE CAPAS + LA HIGIENE: vuelca las
        /// capas de oclusión registradas (nadie hoy: los _capas quedan
        /// vacíos y el volcado es no-op) y corre el Janitor — la limpieza
        /// de los slots de textura que los shaders del mod dejan pegados.
        /// Con las capas vacías el coste es el barrido del Janitor (tres
        /// asignaciones de slot nulas) — cero lotes abiertos.
        /// </summary>
        public override void PostDrawTiles()
        {
            // v6.50.2 — FIX (subsistema muerto): la llamada que faltaba.
            // Solo procesos que dibujan; jamás romper el frame por higiene.
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            try { VFXCore.FlushOcclusion(); }
            catch { }
        }
    }
}
