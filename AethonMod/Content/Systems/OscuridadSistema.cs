using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.NPCs;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles.Jefes;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LA OSCURIDAD PRIMORDIAL (v6.50.37) — EL MEDIO DÍA DE LA OSCURIDAD.
    //
    //  Petición del usuario: «…luego el sol se vuelve negro, con un
    //  mensaje que diga que toda la luz a sido concentrada en un lugar,
    //  esto hace que no solo el sol se vuelva negro sino que la oscuridad
    //  misma toma el control de todo, usa la oscuridad que tiene terraria
    //  para la colaboracion con dont starve y mejora esa oscuridad.
    //  el mundo se vuelve oscuro menos los alrededores del jugador y
    //  Aethon, en el caso de Aethon el brilla con intensidad, pues es luz
    //  pura, en el caso del jugador solo un pequeño circulo a su alrededor
    //  solo si el grimorio es nivel 50 o superior».
    //
    //  LA TÉCNICA — LA MÁSCARA DE LUZ (la oscuridad de The Constant,
    //  MEJORADA): la oscuridad de la colaboración Don't Starve es un
    //  apagón plano; esta es una MÁSCARA DE LUZ REAL:
    //  1. LA MÁSCARA (RenderTarget a media resolución) se limpia a un
    //     GRIS que representa cuánta luz le queda al mundo (1 = día
    //     normal · 0.035 = la oscuridad casi total — las siluetas
    //     fantasma) y sobre ella se pintan ADITIVAMENTe los AGUJEROS DE
    //     LUZ (la textura AgujeroLuz: blanco→negro): AETHON (dorado,
    //     inmenso — ES luz pura), EL JUGADOR (blanco frío, pequeño,
    //     SOLO con Grimorio nivel 50+) y LAS BALAS DEL JEFE (sus pernos,
    //     columnas y runas — la única luz que se mueve). Los agujeros
    //     que se tocan SUMAN: la luz nunca se resta a sí misma.
    //  2. LA MULTIPLICACIÓN: la máscara se dibuja sobre la escena con un
    //     BlendState multiplicativo (escena × máscara) — el blanco del
    //     agujero RESTAURA la escena, el negro la MATA, el falloff es la
    //     penumbra. El FADE (la oscuridad «toma el control») es gratis:
    //     el gris del Clear sube de 1 a su valor final — el mundo se
    //     APAGA en vivo, no se tapa con un rectángulo.
    //  3. EL SOL NEGRO (dibujado DESPUÉS, en píxeles de dispositivo vía
    //     BackgroundViewMatrix.EffectMatrix — la posición EXACTA del sol
    //     de vanilla congelado en el mediodía): el disco negro absoluto
    //     cubre al sol real + el RIM dorado fino + la corona de rayos
    //     girando — un eclipse con vida propia.
    //  4. EL VELO VIOLETA y EL FLASH BLANCO del climax completan el
    //     cuadro; los TELEGRAPHS del jefe (línea guía, pulso de nova) se
    //     REDIBUJAN encima de la oscuridad — el aviso siempre se ve.
    //
    //  EL RELOJ (la otra mitad de la petición): ModifyTimeRate corre el
    //  tiempo (240×: «el tiempo avanza hasta que el sol quede centrado»)
    //  y luego lo CONGELA en el mediodía exacto (time=27000 → el sol en
    //  el centro EXACTO del cielo, literal del decompile de
    //  Main.DrawSunAndMoon). El espejo de PreUpdateTime reconstruye el
    //  estado en TODAS las máquinas leyendo ai[] del jefe — el servidor
    //  manda, cada cliente padece.
    // ======================================================================
    public class OscuridadSistema : ModSystem
    {
        // === EL ESTADO CLIENTE (los fades respiran en cada máquina) ===
        private static float _faseOscuridad = 0f;
        private static float _solNegro = 0f;
        private static float _flashBlanco = 0f;
        private static bool _avisoGrimorioHecho = false;

        /// <summary>
        /// v6.50.38 — EL CERROJO. Si la máscara de luz falla UNA vez (GPU
        /// exótica, asset ausente, device lost), el sistema cae al MODO
        /// VELO (oscuridad simple sin agujeros) y LO ESCRIBE en el log —
        /// el juego NUNCA se queda con la pantalla muerta y el fallo
        /// queda diagnosticado para la casa.
        /// </summary>
        private static bool _mascaraRota = false;

        /// <summary>La intensidad de LA OSCURIDAD en esta máquina (0..1).</summary>
        public static float FaseOscuridad => _faseOscuridad;

        // === LA MÁSCARA DE LUZ ===
        private static RenderTarget2D _mascara;
        private static Texture2D _agujeroCache;
        private static Texture2D _discoNegroCache;

        private static Texture2D Agujero
        {
            get
            {
                if (_agujeroCache == null || _agujeroCache.IsDisposed)
                    _agujeroCache = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/AgujeroLuz").Value;
                return _agujeroCache;
            }
        }

        private static Texture2D DiscoNegro
        {
            get
            {
                if (_discoNegroCache == null || _discoNegroCache.IsDisposed)
                    _discoNegroCache = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/BlackDisk").Value;
                return _discoNegroCache;
            }
        }

        /// <summary>
        /// EL BLENDSTATE MULTIPLICATIVO (escena × máscara): la receta
        /// clásica de XNA/FNA — Source=DestinationColor, Dest=Zero. El
        /// canal alfa se preserva (One/One) — el backbuffer queda opaco.
        /// </summary>
        private static readonly BlendState Multiplicar = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        // ==================================================================
        //  EL ESPEJO — PRE UPDATE TIME (corre en TODAS las máquinas,
        //  DESPUÉS de la IA del jefe y ANTES de UpdateTime: el reloj
        //  acelera/congela en el MISMO tick en que la IA decide)
        // ==================================================================
        public override void PreUpdateTime()
        {
            if (Main.gameMenu)
            {
                _faseOscuridad = 0f;
                _solNegro = 0f;
                _flashBlanco = 0f;
                _avisoGrimorioHecho = false;
                AethonBoss.SubLlegada = 0;
                AethonBoss.TiempoCorriendo = false;
                AethonBoss.TiempoCongelado = false;
                AethonBoss.OscuridadObjetivo = false;
                return;
            }

            // === BUSCAR A LA LUZ ===
            NPC jefe = null;
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo) { jefe = n; break; }
                }
            }
            catch { return; }

            if (jefe == null)
            {
                // la luz murió o se fue: el mundo vuelve a ser del mundo
                AethonBoss.SubLlegada = 0;
                AethonBoss.TiempoCorriendo = false;
                AethonBoss.TiempoCongelado = false;   // el sol recupera su curso
                AethonBoss.OscuridadObjetivo = false;
                _avisoGrimorioHecho = false;
            }
            else
            {
                bool enLlegada = jefe.ai[0] == 0f;               // EST_NACIENDO
                int sub = enLlegada ? (int)jefe.ai[1] : 0;
                AethonBoss.SubLlegada = sub;

                // «si es de noche se hace de día» — el corte al alba,
                // también en cada cliente (el servidor ya lo hizo en su IA)
                if (sub >= 10 && !Main.dayTime)
                {
                    Main.dayTime = true;
                    Main.time = 0.0;
                }

                // LA CARRERA: mientras el sol corre, el reloj corre en
                // TODAS las máquinas (cada cliente ve su propio barrido).
                if (sub == 11) AethonBoss.TiempoCorriendo = true;
                else if (AethonBoss.TiempoCorriendo)
                {
                    // el mediodía visto localmente: clavar el sol en el centro
                    AethonBoss.TiempoCorriendo = false;
                    Main.dayTime = true;
                    Main.time = 27000.0;
                }

                // EL CONGELAMIENTO: desde el climax hasta que la luz muere
                AethonBoss.TiempoCongelado = sub >= 12 ||
                    (jefe.ai[0] >= 1f && jefe.ai[0] <= 7f);

                // LA OSCURIDAD: nace con el sol negro y vive toda la pelea
                AethonBoss.OscuridadObjetivo = sub >= 13 ||
                    (jefe.ai[0] >= 1f && jefe.ai[0] <= 7f);

                // EL AVISO DEL GRIMORIO (una vez por llegada, cliente local)
                if (sub == 13 && !_avisoGrimorioHecho &&
                    Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    _avisoGrimorioHecho = true;
                    int nivel = NivelGrimorio();
                    string clave = nivel >= 50
                        ? "Mods.AethonMod.Jefe.Aethon.OscuridadGrimorio"
                        : "Mods.AethonMod.Jefe.Aethon.OscuridadSinGrimorio";
                    Main.NewText(Language.GetTextValue(clave, nivel),
                        new Color(196, 150, 255));
                }

                // EL TEMBLOR DE LA LLEGADA (los kicks de la casa — el mundo
                // SACUDE la pantalla: sub 10 crece hasta 13 px, sub 11 se
                // sostiene suave mientras el tiempo corre)
                if (Main.netMode != NetmodeID.Server && !Main.dedServ &&
                    sub >= 10 && sub <= 11 && (Main.GameUpdateCount % 13u) == 0u)
                {
                    float f = sub == 10 ? Math.Min(1f, jefe.ai[2] / 150f) : 0.55f;
                    OndaLib.Kick(4f + 9f * f, 13);
                }
            }

            // === LOS FADES (la respiración de la oscuridad) ===
            // la SALIDA es rápida (45 t): cuando la luz muere, el FLASH
            // final del estallido tiene que CEGAR — la oscuridad se
            // disuelve DURANTE la contracción, no después.
            float objetivoOsc = AethonBoss.OscuridadObjetivo ? 1f : 0f;
            float pasoOsc = AethonBoss.OscuridadObjetivo ? 1f / 55f : 1f / 45f;
            _faseOscuridad = MathHelper.Clamp(
                _faseOscuridad + MathF.Sign(objetivoOsc - _faseOscuridad) * pasoOsc, 0f, 1f);

            // EL SOL NEGRO entra con la oscuridad y se despide LENTO
            float objetivoSol = AethonBoss.OscuridadObjetivo ? 1f : 0f;
            float pasoSol = AethonBoss.OscuridadObjetivo ? 1f / 40f : 1f / 130f;
            _solNegro = MathHelper.Clamp(
                _solNegro + MathF.Sign(objetivoSol - _solNegro) * pasoSol, 0f, 1f);

            // EL FLASH DEL CLIMAX (sub 12, tick ≥ 90: sube rápido a lo
            // cegador; después decae mientras la oscuridad entra)
            bool enFlash = AethonBoss.SubLlegada == 12 && jefe != null && jefe.ai[2] >= 90f;
            if (enFlash) _flashBlanco = Math.Min(1f, _flashBlanco + 0.10f);
            else _flashBlanco = Math.Max(0f, _flashBlanco - 0.035f);
        }

        // ==================================================================
        //  EL RELOJ — MODIFY TIME RATE (corre en TODAS las máquinas;
        //  el sol barre y se congela en sincronía perfecta)
        // ==================================================================
        public override void ModifyTimeRate(ref double timeRate,
            ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (AethonBoss.TiempoCorriendo)
            {
                // «el tiempo avanza hasta que el sol quede centrado»:
                // 240× — el día entero cruza el cielo en ~4 s.
                timeRate = 240.0;
                tileUpdateRate = 1.0;     // el mundo físico sigue normal
                eventUpdateRate = 0.0;    // cero tiradas de eventos en el barrido
            }
            else if (AethonBoss.TiempoCongelado)
            {
                // EL MEDIO DÍA ETERNO: el sol clavado en el centro.
                timeRate = 0.0;
            }
        }

        // ==================================================================
        //  LA CAPA DE INTERFAZ — LA OSCURIDAD SE DIBUJA SOBRE EL MUNDO
        //  Y BAJO LA INTERFAZ (la primera capa de todas: el mapa, la
        //  barra, los tooltips — todo el HUD queda USABLE en la oscuridad)
        // ==================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: La Oscuridad Primordial",
                () => { DibujarOscuridad(); return true; },
                InterfaceScaleType.Game));   // lote con GameViewMatrix.ZoomMatrix
        }

        /// <summary>El nivel del Grimorio del jugador local (0 si no lo lleva).</summary>
        private static int NivelGrimorio()
        {
            try
            {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return 0;
                var sp = p.GetModPlayer<ShardPlayer>();
                return sp != null ? sp.NivelGrimorioPublico : 0;
            }
            catch { return 0; }
        }

        // ==================================================================
        //  EL DIBUJO DE LA OSCURIDAD (la capa de interfaz #0)
        //
        //  EL CONTRATO DEL LOTE: tML abre el lote de la capa (Deferred ·
        //  AlphaBlend · LinearClamp · DepthStencil.None · CullCCW ·
        //  ZoomMatrix) y lo CIERRA al volver — este método lo cierra
        //  para sus pases propios y lo devuelve ABIERTO con los
        //  parámetros EXACTOS (literal del decompile de
        //  GameInterfaceLayer.Draw de tML 2026.07).
        //
        //  v6.50.38 — LA CORRECCIÓN DEL ENFOQUE: los AGUJEROS se pintan
        //  en la máscara YA TRANSFORMADOS a píxeles de dispositivo
        //  (Vector2.Transform(posVista, ZoomMatrix) — la MISMA receta de
        //  vanilla para su barra de respiración), así que la máscara SE
        //  ESTAMPA CON IDENTIDAD sobre el rect del VIEWPORT. La versión
        //  anterior estampaba con ZoomMatrix: con zoom 100% ZoomMatrix
        //  es la identidad y todo coincidía POR CASUALIDAD, pero Terraria
        //  FUERZA zoom > 1 en pantallas grandes (ForcedMinimumZoom =
        //  max(ancho/1920, alto/1200) — 1440p = 1.33×, 4K = 2×) y los
        //  agujeros salían desplazados hasta VOLAR fuera de la pantalla:
        //  el mundo entero quedaba bajo el negro («la oscuridad solo
        //  hace que la pantalla se apague»). Ahora: UNA sola
        //  transformación, a CUALQUIER zoom.
        //
        //  v6.50.38 — EL BLINDAJE: el render target se devuelve en un
        //  finally que NO se salta nada (antes, cualquier excepción
        //  dejaba la máscara AMARRADA al dispositivo: TODO el juego se
        //  dibujaba dentro de ella y la pantalla MORÍA), y las
        //  excepciones se ESCRIBEN en el log de verdad — cero catch
        //  vacíos, cero diagnósticos ciegos.
        // ==================================================================
        private static void DibujarOscuridad()
        {
            if (Main.gameMenu || Main.dedServ) return;
            if (_faseOscuridad <= 0.002f && _solNegro <= 0.002f && _flashBlanco <= 0.002f)
                return;   // nada que hacer: el lote de la capa queda intacto

            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (sb == null || gd == null) return;

            try
            {
                int w = Main.screenWidth;
                int h = Main.screenHeight;
                if (w < 8 || h < 8) return;

                Matrix mVista = Main.GameViewMatrix.ZoomMatrix;
                Viewport vp = gd.Viewport;    // el destino REAL del frame
                float t = Main.GlobalTimeWrappedHourly;
                float respira = 1f + 0.03f * MathF.Sin(t * 0.7f);   // la oscuridad respira

                // cerrar el lote de la capa (tML lo cerrará de nuevo al salir)
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    RenderTargetBinding[] previos = null;
                    try
                    {
                        // ========= 1. LA MÁSCARA DE LUZ (media resolución) =========
                        if (!_mascaraRota)
                        {
                            AsegurarMascara(gd, vp.Width, vp.Height);
                            if (_mascara != null)
                            {
                                // EL GRIS DEL MUNDO: 1 = día normal → 0.035 = la
                                // oscuridad casi total (las siluetas fantasma —
                                // la mejora sobre The Constant, que apaga TODO).
                                float gris = MathHelper.Lerp(1f, 0.035f, _faseOscuridad);
                                previos = gd.GetRenderTargets();
                                gd.SetRenderTarget(_mascara);
                                try
                                {
                                    gd.Clear(new Color(gris, gris, gris));

                                    sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                                        SamplerState.LinearClamp, DepthStencilState.None,
                                        RasterizerState.CullNone, null, Matrix.Identity);
                                    try
                                    {
                                        // la máscara vive a media resolución del
                                        // VIEWPORT (el destino real del frame)
                                        float esc = (float)_mascara.Width /
                                            Math.Max(1, vp.Width);

                                        // === EL AGUJERO DE AETHON (ES la luz pura) ===
                                        NPC jefe = BuscarJefe();
                                        if (jefe != null && jefe.alpha < 200)
                                        {
                                            bool eclipse = jefe.ai[1] == 3f;
                                            // en el ECLIPSE hasta SU luz muere: el agujero
                                            // se encoge y se vuelve violeta — la oscuridad
                                            // le cierra la mano encima
                                            float radio = eclipse ? 360f : 1060f;
                                            Color tinte = eclipse
                                                ? new Color(150, 105, 220)
                                                : new Color(255, 240, 200);
                                            DibujarAgujero(sb, jefe.Center - Main.screenPosition,
                                                radio * respira, tinte, esc, mVista);
                                        }

                                        // === EL CÍRCULO DEL JUGADOR (solo Grimorio ≥ 50) ===
                                        if (NivelGrimorio() >= 50)
                                        {
                                            Player p = Main.LocalPlayer;
                                            if (p != null && p.active)
                                                DibujarAgujero(sb, p.Center - Main.screenPosition,
                                                    235f * respira, new Color(225, 232, 255), esc, mVista);
                                        }

                                        // === LAS BALAS DE LA LUZ (pernos, columnas, runas:
                                        //     la única luz que se MUEVE por el mundo) ===
                                        int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
                                        int huecos = 0;
                                        for (int i = 0; i < Main.maxProjectiles && huecos < 26; i++)
                                        {
                                            Projectile pr = Main.projectile[i];
                                            if (pr == null || !pr.active || pr.type != tipo) continue;
                                            int estilo = (int)pr.ai[0];
                                            if (estilo != AtaqueJefeProjectile.EstiloPernoEstelar &&
                                                estilo != AtaqueJefeProjectile.EstiloColumnaJuicio &&
                                                estilo != AtaqueJefeProjectile.EstiloRunaMemorizada) continue;
                                            float rBala = estilo == AtaqueJefeProjectile.EstiloColumnaJuicio
                                                ? 130f : 88f;
                                            DibujarAgujero(sb, pr.Center - Main.screenPosition,
                                                rBala, new Color(255, 244, 214), esc, mVista);
                                            huecos++;
                                        }
                                    }
                                    finally { sb.End(); }
                                }
                                finally
                                {
                                    // EL BLINDAJE: el dispositivo SIEMPRE vuelve a su
                                    // destino — ni una excepción puede amarrar la
                                    // máscara y matar la pantalla del jugador
                                    gd.SetRenderTargets(previos);
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        // EL CERROJO: una falla y el sistema cae al velo simple —
                        // el juego SIGUE VIVO y el log cuenta qué pasó
                        _mascaraRota = true;
                        try
                        {
                            Terraria.ModLoader.Logging.PublicLogger.Error(
                                "[AethonMod] OscuridadSistema: la máscara de luz falló — cayendo al velo simple (reportar con el client.log)", e);
                        }
                        catch { }
                        try { if (previos != null) gd.SetRenderTargets(previos); }
                        catch { }
                    }

                    // ========= 2. LA MULTIPLICACIÓN (escena × máscara) =========
                    // LA CORRECCIÓN DEL ENFOQUE: la máscara vive en PÍXELES DE
                    // DISPOSITIVO (los agujeros ya llevan el ZoomMatrix
                    // transformado dentro) → se estampa con IDENTIDAD sobre el
                    // rect del VIEWPORT: UNA transformación, a CUALQUIER zoom.
                    if (!_mascaraRota && _mascara != null)
                    {
                        sb.Begin(SpriteSortMode.Deferred, Multiplicar,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Matrix.Identity);
                        try
                        {
                            sb.Draw(_mascara, new Rectangle(0, 0, vp.Width, vp.Height),
                                Color.White);
                        }
                        finally { sb.End(); }
                    }

                    // ========= 3. EL VELO Y EL FLASH (alfa, espacio de vista) =========
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, mVista);
                    try
                    {
                        // EL VELO VIOLETA — la oscuridad tiene temperatura
                        if (_faseOscuridad > 0.01f)
                            sb.Draw(VFXCore.Pixel, new Rectangle(0, 0, w, h),
                                new Color(16, 8, 32) * (0.08f * _faseOscuridad));

                        // EL FLASH BLANCO DEL CLIMAX (la luz inunda TODO)
                        if (_flashBlanco > 0.002f)
                            sb.Draw(VFXCore.Pixel, new Rectangle(0, 0, w, h),
                                Color.White * _flashBlanco);
                    }
                    finally { sb.End(); }

                    // ========= 4. EL SOL NEGRO (píxeles de dispositivo:
                    //     la posición EXACTA del sol de vanilla congelado) =========
                    if (_solNegro > 0.002f)
                        DibujarSolNegro(sb, gd, t);

                    // ========= 5. LOS TELEGRAPHS SOBRE LA OSCURIDAD =========
                    // (la línea guía del destello y el pulso de la nova —
                    //  el aviso de la casa SIEMPRE se ve)
                    if (_faseOscuridad >= 0.3f)
                    {
                        NPC jefe = BuscarJefe();
                        if (jefe != null)
                        {
                            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                                SamplerState.LinearClamp, DepthStencilState.None,
                                RasterizerState.CullNone, null, mVista);
                            try
                            {
                                AethonBoss.DibujarTelegrafos(sb, jefe,
                                    jefe.Center - Main.screenPosition);
                            }
                            finally { sb.End(); }
                        }
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
            catch (System.Exception e)
            {
                // ni un catch vacío más: si ALGO rompe la oscuridad, el log
                // lo canta — el diagnóstico llega a la casa
                try
                {
                    Terraria.ModLoader.Logging.PublicLogger.Error(
                        "[AethonMod] OscuridadSistema: error dibujando la oscuridad", e);
                }
                catch { }
            }
        }

        /// <summary>Un agujero de luz en la máscara (aditivo: las luces suman).</summary>
        private static void DibujarAgujero(SpriteBatch sb, Vector2 posVista,
            float radio, Color tinte, float esc, Matrix mVista)
        {
            Texture2D tex = Agujero;
            if (tex == null) return;
            // a píxeles de dispositivo (la máscara vive en identidad)
            Vector2 pos = Vector2.Transform(posVista, mVista) * esc;
            // AgujeroLuz: núcleo pleno hasta el 68% del radio, penumbra al
            // 100% → el lado dibujado es 4× el radio exterior
            float lado = radio * 4f * esc;
            sb.Draw(tex, pos, null, tinte, 0f,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                lado / tex.Width, SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  EL SOL NEGRO — EL ECLIPSE DE LA CONCENTRACIÓN
        //
        //  Posición LITERAL del sol de vanilla (DrawSunAndMoon decompile):
        //  congelado en el mediodía, x = centro EXACTO de la pantalla,
        //  y = bgTopY + 180 — transformado a píxeles con
        //  BackgroundViewMatrix.EffectMatrix (el MISMO lote de vanilla).
        // ==================================================================
        private static void DibujarSolNegro(SpriteBatch sb, GraphicsDevice gd, float t)
        {
            try
            {
                Texture2D disco = DiscoNegro;
                if (disco == null) return;

                Vector2 posCielo = AethonBoss.PosicionSolEnCielo();
                Vector2 pos = Vector2.Transform(posCielo,
                    Main.BackgroundViewMatrix.EffectMatrix);

                // el tamaño del sol de vanilla al mediodía (escala 1.2×1.1
                // del decompile) + el margen del eclipse
                float solW = 80f;
                try { solW = Terraria.GameContent.TextureAssets.Sun.Value.Width; }
                catch { }
                float radioDisco = solW * 1.32f * 0.5f + 44f;
                Vector2 origenD = new Vector2(disco.Width, disco.Height) * 0.5f;

                // === EL DISCO NEGRO (alfa: cubre al sol real) ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Matrix.Identity);
                try
                {
                    // BlackDisk: núcleo sólido hasta el 70% del radio
                    float lado = (radioDisco / 0.70f) * 2f;
                    sb.Draw(disco, pos, null, Color.White * _solNegro, 0f, origenD,
                        lado / disco.Width, SpriteEffects.None, 0f);

                    // el aura oscura que se COME el cielo alrededor
                    Texture2D glow = VFXCore.SoftGlow;
                    sb.Draw(glow, pos, null,
                        new Color(8, 4, 18) * (0.55f * _solNegro), 0f,
                        new Vector2(glow.Width, glow.Height) * 0.5f,
                        new Vector2(radioDisco * 4.2f / glow.Width,
                                    radioDisco * 4.2f / glow.Height),
                        SpriteEffects.None, 0f);
                }
                finally { sb.End(); }

                // === EL RIM DORADO Y LA CORONA (aditivo: el eclipse VIVE) ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Matrix.Identity);
                try
                {
                    // EL RIM: el anillo fino justo en el borde del disco
                    Texture2D ring = VFXCore.Ring;
                    float ladoRing = radioDisco / 0.46f * 2f;   // la receta de VFXCore
                    float latido = 0.86f + 0.14f * MathF.Sin(t * 1.3f);
                    sb.Draw(ring, pos, null,
                        new Color(255, 214, 120) * (0.85f * _solNegro * latido), 0f,
                        new Vector2(ring.Width, ring.Height) * 0.5f,
                        ladoRing / ring.Width, SpriteEffects.None, 0f);
                    // el segundo rim interior (la corona doble)
                    sb.Draw(ring, pos, null,
                        new Color(180, 130, 255) * (0.35f * _solNegro * latido), 0f,
                        new Vector2(ring.Width, ring.Height) * 0.5f,
                        ladoRing * 0.82f / ring.Width, SpriteEffects.None, 0f);

                    // LA CORONA DE RAYOS: doce filamentos girando LENTO
                    Texture2D g2 = VFXCore.SoftGlow;
                    Vector2 origenG = new Vector2(g2.Width, g2.Height) * 0.5f;
                    float giro = t * 0.05f;
                    for (int i = 0; i < 12; i++)
                    {
                        float ang = giro + i * MathHelper.Pi / 6f;
                        float largo = radioDisco * (1.5f + 0.7f * MathF.Sin(t * 0.9f + i * 1.7f));
                        Color cR = (i % 3 == 0) ? new Color(190, 140, 255) : new Color(255, 214, 130);
                        sb.Draw(g2, pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            (radioDisco + largo * 0.5f), null,
                            cR * (0.10f * _solNegro), ang, origenG,
                            new Vector2(largo / g2.Width, 6f / g2.Height),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { sb.End(); }

                // === LOS DESTELLOS LEJANOS (la oscuridad está VIVA: el
                //     cielo recuerda la luz con chispas fantasma) ===
                if (_faseOscuridad > 0.5f)
                {
                    sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Matrix.Identity);
                    try
                    {
                        Texture2D g3 = VFXCore.SoftGlow;
                        Vector2 origenG3 = new Vector2(g3.Width, g3.Height) * 0.5f;
                        int wDev = gd.Viewport.Width;
                        int hDev = gd.Viewport.Height;
                        for (int i = 0; i < 7; i++)
                        {
                            // hash determinista de la casa: el MISMO cielo
                            // en todas las máquinas, sin sincronizar nada
                            float hx = VFXCore.Hash01(977, i, (int)(t * 0.35f));
                            float hy = VFXCore.Hash01(131, i, (int)(t * 0.35f));
                            float fase = VFXCore.Hash01(571, i, (int)(t * 0.35f));
                            float brillo = MathF.Max(0f, MathF.Sin((t * 0.35f % 1f) * 6f + fase * 40f));
                            if (brillo <= 0f) continue;
                            Vector2 dp = new Vector2(hx * wDev, hy * hDev * 0.45f);
                            sb.Draw(g3, dp, null,
                                new Color(140, 110, 210) * (0.05f * brillo * _faseOscuridad),
                                0f, origenG3,
                                new Vector2(70f / g3.Width, 70f / g3.Height),
                                SpriteEffects.None, 0f);
                        }
                    }
                    finally { sb.End(); }
                }
            }
            catch { }
        }

        /// <summary>La máscara a media resolución (se recrea si cambia el tamaño).</summary>
        private static void AsegurarMascara(GraphicsDevice gd, int w, int h)
        {
            int mw = Math.Max(1, w / 2);
            int mh = Math.Max(1, h / 2);
            if (_mascara != null && !_mascara.IsDisposed &&
                _mascara.Width == mw && _mascara.Height == mh) return;
            try
            {
                if (_mascara != null && !_mascara.IsDisposed) _mascara.Dispose();
                _mascara = new RenderTarget2D(gd, mw, mh, false,
                    SurfaceFormat.Color, DepthFormat.None, 0,
                    RenderTargetUsage.DiscardContents);
            }
            catch { _mascara = null; }
        }

        /// <summary>El jefe de la luz (o null si no está).</summary>
        private static NPC BuscarJefe()
        {
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo) return n;
                }
            }
            catch { }
            return null;
        }

        // ==================================================================
        //  LA DESCARGA (la casa: nada muerto toca ModContent)
        // ==================================================================
        public override void Unload()
        {
            _faseOscuridad = 0f;
            _solNegro = 0f;
            _flashBlanco = 0f;
            _avisoGrimorioHecho = false;
            _mascaraRota = false;
            try { if (_mascara != null && !_mascara.IsDisposed) _mascara.Dispose(); }
            catch { }
            _mascara = null;
            _agujeroCache = null;
            _discoNegroCache = null;
            AethonBoss.SubLlegada = 0;
            AethonBoss.TiempoCorriendo = false;
            AethonBoss.TiempoCongelado = false;
            AethonBoss.OscuridadObjetivo = false;
        }
    }
}
