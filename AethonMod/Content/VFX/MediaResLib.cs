using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// MediaResLib — v6.43 — LOS PASES DE FUEGO/HUMO A MEDIA RESOLUCIÓN.
    ///
    /// LA IDEA: las partículas volumétricas DIFUSAS — fuego, humo, chispas
    /// blandas en gran número — no necesitan la resolución nativa de la
    /// pantalla: su textura ya es un degradado suave y su aporte es "una
    /// mancha de luz", no un trazo definido. El pase las dibuja a un render
    /// target de MEDIA resolución (ancho/2 × alto/2) y luego compone la capa
    /// escalada ×2 sobre el destino del llamador:
    ///
    ///   · FILL-RATE ÷4: cada texel del pase cubre un bloque 2×2 de píxeles
    ///     de la pantalla; las capas de docenas de quads soft solapados
    ///     cuestan una CUARTA parte de píxeles sombreados.
    ///   · LOOK DELIBERADO: con el sampler lineal (por defecto) la
    ///     composición difumina 2× y en materiales difusos el resultado es
    ///     visualmente indistinguible del pase nativo — MISMAS texturas,
    ///     MISMOS colores, MISMAS escalas. Con el sampler de punto (modo
    ///     retro, <paramref name="pixelado"/>) cada texel del half-res es
    ///     un "píxel gordo" 2×2 nítido — pixelado intencional.
    ///
    /// EL CONTRATO (un pase por frame-ámbito):
    /// <code>
    ///   if (MediaResLib.Disponible)
    ///   {
    ///       MediaResLib.Empezar(pixelado: false);
    ///       try
    ///       {
    ///           // ... dibujar el fuego/humo con Main.spriteBatch, en las
    ///           //     coordenadas de SIEMPRE (vista = mundo − screenPosition)
    ///       }
    ///       finally { MediaResLib.Terminar(); }
    ///   }
    /// </code>
    ///
    /// LA MATRIZ ESCALADA hace la media resolución: el lote del pase abre
    /// con (matriz de cámara × escala 0,5), así que el llamador dibuja en
    /// sus coordenadas de vista de siempre SIN dividir nada — la matriz
    /// divide por él. La composición ×2 devuelve cada píxel exactamente a
    /// su sitio de pantalla (el zoom de la cámara viaja dentro de la
    /// matriz: funciona a cualquier zoom).
    ///
    /// LOS DUSTS de Terraria NO se pueden mover de pase (los dibuja el
    /// motor en su propio lote); el beneficio es para los QUADS de
    /// librería que el llamador emite él mismo. Dentro del pase NO se
    /// puede usar VFXCore.FlushAdditive (abre su propio lote con la matriz
    /// SIN escalar y volcaría a resolución completa): los quads van
    /// DIRECTOS al lote del pase — para eso está <see cref="Quad"/>, que
    /// replica la matemática de FlushAdditive (invTex y compañía) en
    /// coordenadas de MUNDO.
    ///
    /// ==================================================================
    /// LECCIÓN v6.43 — FNA LIMPIA EL TARGET RECIÉN BINDEADO (verificado al
    /// IL del FNA.dll real, la técnica de sondeo de la casa): GraphicsDevice
    /// .SetRenderTargets termina en «si el uso del target NUEVO es
    /// DiscardContents → Clear(...)» — BINDEAR un render target
    /// DiscardContents LO BORRA. Y Main.screenTarget (los 8 targets de
    /// Main.InitTargets) nacen con usage = 0 (DiscardContents): un restore
    /// CIEGO del binding al terminar el pase BORRARÍA EL MUNDO ya dibujado
    /// en él — exactamente la pantalla negra que sufrió la lente en
    /// v5.86-v5.88, ahora con el mecanismo exacto identificado.
    ///
    /// AUDITORÍA v6.43 (T4) — LA VACUNA, AL DETALLE DE ESTA BUILD (ambas
    /// medidas contra el MISMO FNA.dll contra el que compila el mod):
    ///   · RenderTarget2D.RenderTargetUsage tiene el setter PRIVADO aquí —
    ///     NO se puede vacunar un render target ajeno: un llamador cuyo
    ///     destino sea un RT DiscardContents (p. ej. Main.screenTarget)
    ///     pierde su contenido al restore y debe REDIBUJARLO completo, como
    ///     hace BlackHoleLensSystem (v5.89). Ningún llamador actual cae ahí.
    ///   · PresentationParameters.RenderTargetUsage SÍ es pública de
    ///     lectura Y escritura, y FNA la lee EN VIVO en el momento de
    ///     bindear (IL: get → cgt.un → Clear solo con DiscardContents): la
    ///     vacuna del BACKBUFFER — prestarle PreserveContents SOLO el
    ///     instante del re-bind y devolver su valor — está implementada en
    ///     <see cref="VolverAlDestino"/>: el mundo acumulado del frame
    ///     SOBREVIVE al pase. (El caso real del único llamador actual: el
    ///     PreDraw de la vela, que dibuja al backbuffer, a mitad del pase
    ///     del mundo.)
    /// (Des-bindear NO limpia nada — solo bindear: irse al RT propio del
    /// pase es gratis.)
    /// ==================================================================
    ///
    /// ==================================================================
    /// LECCIÓN v6.43 — LA COMPOSICIÓN NO USA BlendState.Additive: el
    /// Additive de FNA es (SourceAlpha, One) — el alfa de la fuente MODULA
    /// (lección verificada en v6.40 al decodificar el IL). El pase aditivo
    /// ya PREmultiplicó el color de cada quad por su alfa (el rgb del RT
    /// acumula Σ C·A) mientras su canal alfa acumula la basura Σ A²;
    /// componer con Additive multiplicaría rgb·(Σ A²) y los velos tenues
    /// CASI DESAPARECERÍAN (un velo de alfa 0,35 aportaría C·0,35³ en vez
    /// de C·0,35: 8 veces más tenue). El blend correcto para una capa
    /// aditiva PREmultiplicada es (Uno, Uno): final += rgb del RT — la
    /// MISMA suma, píxel a píxel, que dibujar los quads directo al destino
    /// del llamador. Cero cambio visual, por construcción.
    /// ==================================================================
    ///
    /// ANIDAMIENTO PROHIBIDO: UN pase por frame-ámbito. Si Empezar se
    /// llama con el pase ya abierto en el MISMO frame, se ignora en
    /// silencio (el pase exterior manda; los draws del anidado seguirían
    /// yendo al lote del pase exterior, que es lo único abierto). Si un
    /// llamador futuro abandona un pase sin Terminar (bug de
    /// integración), el Empezar del frame siguiente lo SANA (End
    /// defensivo + restauración del destino) en vez de dejar la bandera
    /// colgada para siempre — la lección v6.41: un lote abierto corrompe
    /// el render hasta relogear; esta librería se autorrepara.
    ///
    /// Hilo y ciclo de vida: el RT se cachea y solo se recrea si cambió el
    /// tamaño del backbuffer o se perdió el contenido (IsContentLost) — el
    /// patrón de la lente; su destrucción al descargar el mod corre en el
    /// hilo principal vía <see cref="MediaResSistema"/> (lección v5.87:
    /// FNA3D exige Dispose de recursos gráficos en el hilo principal).
    /// Cero GC por frame: el RT y el blend se cachean; el array de
    /// bindings de GetRenderTargets se guarda SOLO durante el pase y se
    /// suelta al Terminar (nunca entre frames).
    /// </summary>
    public static class MediaResLib
    {
        // ------------------------------------------------------------------
        //  EL ESTADO
        // ------------------------------------------------------------------

        /// <summary>El render target de media resolución (cacheado: solo se
        /// recrea al cambiar el backbuffer o al perder el contenido).</summary>
        private static RenderTarget2D _rt;

        /// <summary>Los bindings del destino del llamador, guardados SOLO
        /// durante el pase (GetRenderTargets devuelve un array fresco — se
        /// suelta en Terminar, jamás entre frames).</summary>
        private static RenderTargetBinding[] _bindingsGuardados;

        /// <summary>El modo retro del volcado (PointClamp = píxeles gordos
        /// 2×2 nítidos) — lo elige Empezar, lo consume Terminar.</summary>
        private static bool _pixelado;

        /// <summary>El frame en el que se abrió el pase (deduplicación del
        /// anidamiento y sanado de pases colgados — el mismo dedup de
        /// VFXCore.Presupuesto).</summary>
        private static uint _frameDelPase;

        /// <summary>
        /// AUDITORÍA v6.43 (T4) — ¿Tenía el LLAMADOR su lote ABIERTO cuando
        /// Empezar cerró defensivamente? Terminar reabre el lote estándar
        /// SOLO si esto es true: el contrato real de restauración de estado
        /// («cerrado→cerrado, abierto→abierto», el de FlushAdditive y el de
        /// cualquier PreDraw de la casa). El bug que corrige: con la
        /// reapertura incondicional, un llamador que llega con el lote YA
        /// CERRADO (el patrón PreDraw de la vela) recibía un lote ABIERTO
        /// que no era suyo → su propio Begin siguiente tiraba (FNA: «Begin
        /// has been called before calling End») y su dibujado se perdía
        /// dentro del catch.
        /// </summary>
        private static bool _loteDelLlamadorAbierto;

        /// <summary>El blend de la COMPOSICIÓN: aditivo de capa
        /// PREmultiplicada (Uno, Uno) — final += rgb del RT (véase la
        /// lección de arriba: el Additive de FNA modula por el alfa de la
        /// fuente y apagaría los velos tenues). Lazy: se construye la
        /// primera vez, en contexto de render.</summary>
        private static BlendState _blendCapa;

        /// <summary>Acceso lazy al blend de la composición.</summary>
        private static BlendState BlendCapa
        {
            get
            {
                if (_blendCapa == null)
                {
                    _blendCapa = new BlendState
                    {
                        ColorSourceBlend = Blend.One,
                        ColorDestinationBlend = Blend.One,
                        AlphaSourceBlend = Blend.One,
                        AlphaDestinationBlend = Blend.One,
                    };
                }
                return _blendCapa;
            }
        }

        /// <summary>
        /// ¿Hay un pase de media resolución ABIERTO ahora mismo? (los
        /// asserts de <see cref="Terminar"/> y la regla de no-anidamiento
        /// dependen de esta bandera).
        /// </summary>
        public static bool EnPase { get; private set; }

        /// <summary>
        /// ¿Se PUEDE abrir un pase? False en servidor (no hay render), en
        /// menú (sin dispositivo estable) o con el GraphicsDevice caído.
        /// Consúltalo ANTES de Empezar para poder elegir el camino nativo
        /// de respaldo (el pase que no se abre no dibuja nada: Terminar es
        /// no-op).
        /// </summary>
        public static bool Disponible
        {
            get
            {
                if (Main.netMode == NetmodeID.Server || Main.gameMenu)
                    return false;
                try { return Main.graphics?.GraphicsDevice != null; }
                catch { return false; }
            }
        }

        // ------------------------------------------------------------------
        //  EL PASE
        // ------------------------------------------------------------------

        /// <summary>
        /// ABRE el pase de media resolución: asegura el RT (recreación lazy
        /// por tamaño del backbuffer o contenido perdido — patrón de la
        /// lente), guarda los bindings del destino del llamador, bindea el
        /// RT, lo limpia a transparente y abre un lote ADITIVO con la
        /// MATRIZ DE CÁMARA ESCALADA ×0,5 — a partir de aquí el llamador
        /// dibuja en sus coordenadas de vista de siempre (mundo −
        /// Main.screenPosition) y la matriz hace la media resolución.
        /// </summary>
        /// <param name="pixelado">True = volcado con SamplerState.PointClamp
        /// (cada texel del half-res es un píxel gordo 2×2 nítido — modo
        /// retro); false = LinearClamp (difumina 2×: indistinguible en
        /// materiales difusos). Solo afecta al VOLCADO final.</param>
        public static void Empezar(bool pixelado = false)
        {
            // === ANIDAMIENTO PROHIBIDO (un pase por frame-ámbito). ===
            if (EnPase)
            {
                if (_frameDelPase == Main.GameUpdateCount)
                    return;                 // anidamiento real: se ignora en silencio
                SanarPaseColgado();         // pase abandonado de un frame anterior
            }

            if (Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            GraphicsDevice gd = null;
            try { gd = Main.graphics?.GraphicsDevice; } catch { }
            if (gd == null)
                return;

            try
            {
                // === 1. EL RT (patrón de la lente: recreación lazy). ===
                int ancho = Math.Max(1, gd.PresentationParameters.BackBufferWidth / 2);
                int alto = Math.Max(1, gd.PresentationParameters.BackBufferHeight / 2);
                if (_rt == null || _rt.IsDisposed || _rt.IsContentLost ||
                    _rt.Width != ancho || _rt.Height != alto)
                {
                    // El Dispose directo es seguro AQUÍ: Empezar corre en el
                    // hilo de dibujado (el hilo principal) — la lección v5.87
                    // solo prohíbe el Dispose desde el hilo de descarga.
                    try { _rt?.Dispose(); } catch { }
                    _rt = new RenderTarget2D(gd, ancho, alto, false,
                        SurfaceFormat.Color, DepthFormat.None, 0,
                        RenderTargetUsage.DiscardContents);
                }

                // === 2. Lote abierto del llamador: se cierra DEFENSIVAMENTE
                //        (sus draws pendientes se vuelcan al destino ACTUAL,
                //        que todavía es el suyo). Si HABÍA lote, Terminar
                //        reabrirá el estándar del mundo; si no lo había, el
                //        estado que el llamador encontró (cerrado) es el que
                //        se le devuelve — restauración exacta. ===
                try { Main.spriteBatch.End(); _loteDelLlamadorAbierto = true; }
                catch { _loteDelLlamadorAbierto = false; }

                // === 3. Guardar los bindings actuales (solo durante el pase). ===
                _bindingsGuardados = gd.GetRenderTargets();
                _pixelado = pixelado;
                _frameDelPase = Main.GameUpdateCount;

                // === 4. Bindear el RT y limpiarlo a transparente. ===
                gd.SetRenderTarget(_rt);
                gd.Clear(Color.Transparent);

                // === 5. EL LOTE DEL PASE: aditivo + la MATRIZ ESCALADA. ===
                // El llamador dibuja en coordenadas de VISTA de siempre; la
                // matriz divide por 2 (la cámara ya viene dentro, así que el
                // zoom viaja con ella y el pase funciona a cualquier zoom).
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix * Matrix.CreateScale(0.5f, 0.5f, 1f));

                EnPase = true;
            }
            catch
            {
                // Apertura fallida: el estado queda LIMPIO (nada abierto, el
                // destino del llamador restaurado, bandera caída) — el frame
                // siguiente lo reintenta sin daño.
                SanarPaseColgado();
            }
        }

        /// <summary>
        /// CIERRA el pase y COMPONE: cierra el lote del pase (los quads
        /// quedan en el RT), vuelve al destino del llamador (con la
        /// protección de uso de la lección de arriba — el contenido
        /// SOBREVIVE al restore), vuelca el RT COMPLETO escalado ×2 a
        /// pantalla del destino con el blend de capa premultiplicada y el
        /// sampler elegido, y REABRE el lote estándar del mundo SOLO si el
        /// llamador tenía el lote ABIERTO al llegar (cerrado→cerrado,
        /// abierto→abierto — el contrato de FlushAdditive: el que llega
        /// con el lote ya cerrado gestiona sus lotes él mismo). TODO el
        /// cuerpo en try/catch/finally: si algo tira, la restauración de
        /// emergencia hace el mejor esfuerzo (End defensivo, restore de
        /// bindings, bandera caída) — la lección v6.41: un lote abierto
        /// corrompe el render para siempre.
        /// </summary>
        public static void Terminar()
        {
            // Sin pase abierto: no-op (documentado — el llamador con
            // respaldo nativo nunca llega aquí con la bandera caída).
            if (!EnPase)
                return;

            GraphicsDevice gd = null;
            try { gd = Main.graphics?.GraphicsDevice; } catch { }

            bool destinoRestaurado = false;
            try
            {
                // === 1. Cerrar el lote del pase (los quads van al RT). ===
                try { Main.spriteBatch.End(); } catch { }

                // === 2. VOLVER al destino del llamador (con la protección
                //        de uso: sin ella, re-bindear un DiscardContents como
                //        screenTarget BORRARÍA el mundo ya dibujado). ===
                destinoRestaurado = VolverAlDestino(gd);

                // === 3. LA COMPOSICIÓN: el RT COMPLETO a pantalla del
                //        destino, ×2 (rectángulo del viewport — el patrón de
                //        la lente; matemáticamente idéntico a la escala (2,2)
                //        cuando el destino mide el doble del RT, que es su
                //        tamaño de nacimiento). ===
                if (destinoRestaurado && gd != null && _rt != null && !_rt.IsDisposed)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendCapa,
                            _pixelado ? SamplerState.PointClamp : SamplerState.LinearClamp,
                            DepthStencilState.None, RasterizerState.CullNone,
                            null, Matrix.Identity);

                        // Pantalla completa del destino: con PointClamp cada
                        // texel del half-res es un píxel gordo 2×2 nítido
                        // (retro); con LinearClamp difumina 2× (suave).
                        Main.spriteBatch.Draw(_rt,
                            new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height),
                            Color.White);
                    }
                    finally
                    {
                        try { Main.spriteBatch.End(); } catch { }
                    }
                }

                // === 4. REABRIR el lote del mundo — SOLO si el llamador lo
                //        tenía abierto al llegar (el estándar de restore de
                //        la casa, idéntico al de cualquier PreDraw del mod:
                //        cerrado→cerrado, abierto→abierto). ===
                if (_loteDelLlamadorAbierto)
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
            }
            catch
            {
                // === RESTAURACIÓN DE EMERGENCIA (mejor esfuerzo). ===
                try { Main.spriteBatch.End(); } catch { }
                if (!destinoRestaurado)
                    VolverAlDestino(gd);
                if (_loteDelLlamadorAbierto)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                    }
                    catch { }
                }
            }
            finally
            {
                // El array de bindings se suelta SIEMPRE (nunca entre frames).
                _bindingsGuardados = null;
                EnPase = false;
                _loteDelLlamadorAbierto = false;
            }
        }

        /// <summary>
        /// EL QUAD DIRECTO AL PASE: dibuja un cuadro de luz al lote de
        /// media resolución con la MISMA matemática de
        /// VFXCore.FlushAdditive (posición en COORDENADAS DE MUNDO — el
        /// helper resta Main.screenPosition; escala en píxeles finales;
        /// invTex para normalizar la resolución de la textura) — el
        /// sustituto de FlushAdditive DENTRO del pase (el volcado de
        /// VFXCore abre su propio lote con la matriz sin escalar: no se
        /// puede usar aquí).
        /// </summary>
        /// <param name="posicionMundo">Centro del quad en coordenadas de
        /// mundo (la matriz escalada del pase hace la media
        /// resolución).</param>
        /// <param name="color">Color con alfa.</param>
        /// <param name="escala">Tamaño del quad en píxeles de PANTALLA
        /// (ancho×alto), como en FlushAdditive.</param>
        /// <param name="tex">Textura (null = VFXCore.SoftGlow).</param>
        public static void Quad(Vector2 posicionMundo, Color color, Vector2 escala, Texture2D tex = null)
        {
            if (!EnPase || color.A == 0)
                return;

            Texture2D t = tex ?? VFXCore.SoftGlow;
            if (t == null || t.IsDisposed)
                return;

            // La matemática de FlushAdditive, copiada exacta: invTex para
            // que la escala cuente píxeles finales, origen al centro.
            Vector2 invTex = new Vector2(1f / t.Width, 1f / t.Height);
            Main.spriteBatch.Draw(t, posicionMundo - Main.screenPosition, null, color, 0f,
                t.Size() * 0.5f, escala * invTex, SpriteEffects.None, 0f);
        }

        // ------------------------------------------------------------------
        //  EL RESTAURADOR DEL DESTINO (con la protección de uso)
        // ------------------------------------------------------------------

        /// <summary>
        /// Vuelve a bindear el destino que el llamador tenía al abrir el
        /// pase. Para el BACKBUFFER lleva LA VACUNA DE LA LECCIÓN (la del
        /// encabezado): PresentationParameters.RenderTargetUsage es pública
        /// de escritura y FNA la lee EN VIVO al bindear — se presta a
        /// PreserveContents solo el instante del re-bind (el valor se
        /// devuelve en finally) y el mundo acumulado del frame SOBREVIVE.
        /// Para un RENDER TARGET destino no hay vacuna posible (el setter de
        /// RenderTarget2D.RenderTargetUsage es privado en esta build): el
        /// re-bind lo limpia si es DiscardContents — un llamador así debe
        /// redibujar su contenido completo, como hace la lente (v5.89).
        /// Re-bindear el MISMO binding ya activo es no-op en FNA (compara
        /// bindings antes de tocar nada), así que el camino de emergencia
        /// es igual de seguro.
        /// </summary>
        private static bool VolverAlDestino(GraphicsDevice gd)
        {
            if (gd == null)
                return false;

            try
            {
                RenderTargetBinding[] bindings = _bindingsGuardados;

                // Sin bindings guardados: el llamador dibujaba al BACKBUFFER.
                if (bindings == null || bindings.Length == 0)
                {
                    // AUDITORÍA v6.43 (T4) — EL BUG DE LA PANTALLA NEGRA:
                    // sin la vacuna, este SetRenderTarget(null) LIMPIABA el
                    // backbuffer (el mundo dibujado hasta el pase) y solo
                    // quedaba la capa de media resolución — la pantalla
                    // negra de la lente v5.86-v5.88 reproducida en el pase
                    // de la vela. Con la vacuna, el re-bind NO limpia nada.
                    RenderTargetUsage usoOriginal = gd.PresentationParameters.RenderTargetUsage;
                    try
                    {
                        gd.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
                        gd.SetRenderTarget(null);
                    }
                    finally
                    {
                        gd.PresentationParameters.RenderTargetUsage = usoOriginal;
                    }
                    return true;
                }

                // Con bindings: el destino es el render target del llamador
                // (p. ej. Main.screenTarget durante el pase del mundo). El
                // binding expone Texture en esta versión: comprobación de
                // tipo y re-bind directo.
                RenderTarget2D destino = bindings[0].RenderTarget as RenderTarget2D;
                if (destino != null && !destino.IsDisposed)
                    gd.SetRenderTarget(destino);
                else
                    gd.SetRenderTarget(null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// SANA un pase colgado (un llamador olvidó su Terminar): End
        /// defensivo del lote abandonado, restauración del destino SOLO si
        /// el pase era de ESTE frame (imponer bindings de un frame anterior
        /// rompería el pase actual de Terraria — mejor soltarlos) y bandera
        /// caída. La librería se autorrepara en un frame.
        /// </summary>
        private static void SanarPaseColgado()
        {
            GraphicsDevice gd = null;
            try { gd = Main.graphics?.GraphicsDevice; } catch { }

            // El lote del pase abandonado: End defensivo (sus draws pendientes
            // caen donde caigan — el daño de un pase abandonado ya está hecho;
            // esto desbloquea el FUTURO).
            try { Main.spriteBatch.End(); } catch { }

            if (gd != null && _bindingsGuardados != null &&
                _frameDelPase == Main.GameUpdateCount)
            {
                VolverAlDestino(gd);
            }

            _bindingsGuardados = null;
            EnPase = false;
            _loteDelLlamadorAbierto = false;
        }

        // ------------------------------------------------------------------
        //  EL CICLO DE VIDA (lo dispara MediaResSistema.Unload)
        // ------------------------------------------------------------------

        /// <summary>
        /// Suelta el render target al DESCARGAR el mod. Lección v5.87 (el
        /// patrón exacto de la lente): tModLoader descarga en un hilo
        /// secundario y FNA3D exige el Dispose de recursos gráficos en el
        /// hilo principal — el dispose se ENCOLA vía
        /// Main.QueueMainThreadAction (cola drenada en Main.Update cada
        /// frame, incluso durante la pantalla de carga del reload) y la
        /// referencia se anula INMEDIATAMENTE: el closure captura una
        /// variable LOCAL, así que la acción es autosuficiente aunque esta
        /// clase ya esté descargada.
        /// </summary>
        internal static void Detach()
        {
            // Estado del pase: fuera (por si quedó uno colgado).
            _bindingsGuardados = null;
            EnPase = false;
            _loteDelLlamadorAbierto = false;
            _pixelado = false;

            RenderTarget2D target = _rt;
            _rt = null;                        // la referencia se anula YA
            if (target == null || target.IsDisposed)
                return;

            try
            {
                Main.QueueMainThreadAction(() =>
                {
                    try { target.Dispose(); }
                    catch
                    {
                        // Defensivo: una excepción aquí subiría hasta
                        // Main.Update() y rompería el bucle del juego.
                    }
                });
            }
            catch
            {
                // Encolado imposible (apagado total del proceso): se abandona
                // la referencia — el driver libera los recursos del proceso
                // al terminar de todos modos.
            }
        }
    }

    /// <summary>
    /// MediaResSistema — el ciclo de vida del render target de
    /// <see cref="MediaResLib"/>. No tiene hooks de update ni de dibujado:
    /// la librería vive en los PreDraw de sus llamadores; este sistema
    /// solo existe para que el RT muera BIEN al descargar/recargar el mod
    /// (lección v5.87: el Dispose de recursos gráficos DEBE correr en el
    /// hilo principal — Unload corre en el hilo de carga secundario de
    /// tModLoader, así que el detach real se encola a Main.Update).
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class MediaResSistema : ModSystem
    {
        /// <summary>
        /// Al descargar el mod: detach del RT de media resolución (encolado
        /// al hilo principal + anulación inmediata de la referencia — el
        /// patrón exacto de la lente). TODO en try/catch: el detach jamás
        /// puede impedir que la desactivación del mod continúe.
        /// </summary>
        public override void Unload()
        {
            try { MediaResLib.Detach(); }
            catch
            {
                // Programación defensiva: nunca romper la descarga.
            }
        }
    }
}
