using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  VELOLIB — LA LIBRERÍA DE LA OSCURIDAD (v6.50.39).
    //
    //  Petición del usuario: «quitemos ese sistema que pusiste para
    //  oscurecer el mundo, pues está mal, solo hace que todo este negro
    //  y no es el oscurecer que quiero, en su lugar revisa como lo hace
    //  el mod wrath of the gods, y crea una libreria para eso».
    //
    //  LA TÉCNICA — INGENIERÍA INVERSA DE WRATH OF THE GODS
    //  (TheFifthCircle/WrathOfTheGodsPublic, el addon de Calamity):
    //  su oscuridad famosa (el ataque «Darkness with Light Slashes» de
    //  Nameless Deity) NO usa máscaras de luz ni render targets: es
    //  TotalScreenOverlaySystem — UN VELO dibujado sobre el frame
    //  TERMINADO, en el evento Main.OnPostDraw (encima del mundo, de la
    //  interfaz y del cursor), con un interpolante que respira; y lo
    //  que debe verse en la oscuridad (sus tajos de luz) se dibuja
    //  DESPUÉS del velo, en el MISMO lote (el evento DrawAfterWhiteEvent
    //  de su DrawWhite). Simple a propósito: cero shaders, cero render
    //  targets, cero estados exóticos del dispositivo — por eso NUNCA
    //  rompe: es la técnica que la casa adopta.
    //
    //  EL VELO: una capa de color semitransparente sobre TODO el frame
    //  (el mundo se apaga a siluetas — el resto es oscuridad). El
    //  interpolante camina hacia su objetivo al ritmo que el llamador
    //  decree (Ver/Apagar): los fades son parte del contrato.
    //
    //  LAS LUCES: lo que BRILLA vive SOBRE el velo — cada luz es un
    //  resplandor suave + una brasa (aditivo): la luz de Don't Starve
    //  dibujada al estilo WotG. Se registran POR FRAME (posiciones
    //  frescas) y se limpian al final del dibujado.
    //
    //  LOS PINTORES: el DrawAfterWhiteEvent de la casa — callbacks que
    //  reciben el lote ABIERTO en aditivo·identidad y pueden dibujar
    //  lo suyo encima de la oscuridad (devolviéndolo abierto en
    //  aditivo·identidad — el contrato de lote de la casa).
    //
    //  EL CONTRATO DE ROBUSTEZ (el fix de «al compilar el juego se
    //  cierra»): cero manipulación del GraphicsDevice (ni targets, ni
    //  blends custom, ni capas de interfaz que restaurar). El dibujado
    //  completo vive en try/catch con CERROJO: si cae tres veces, el
    //  velo se retira para siempre y el juego SIGUE VIVO — y todo se
    //  escribe en el log (cero catch vacíos).
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
        internal static readonly float[] LuzRadio = new float[MaxLuces];
        internal static readonly Color[] LuzColor = new Color[MaxLuces];
        internal static readonly bool[] LuzBrasa = new bool[MaxLuces];
        internal static int Luces = 0;

        // === LOS PINTORES (el DrawAfterWhiteEvent de la casa) ===
        private static List<Action<SpriteBatch>> _pintores = new List<Action<SpriteBatch>>(8);

        // === LA BANDERA DE FRAME: pintar contenido aunque el velo ya se apagó
        //     (el sol negro se despide LENTO — más lento que la oscuridad) ===
        private static bool _pintoresSiempre = false;

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
        /// Registrar UNA LUZ que vive sobre la oscuridad (se limpia sola al
        /// final del frame — hay que registrarla cada tick que deba verse).
        /// posMundo en coordenadas del MUNDO; radio en píxeles de mundo.
        /// </summary>
        public static void Luz(Vector2 posMundo, float radio, Color color, bool brasa = true)
        {
            if (Luces >= MaxLuces || radio <= 1f) return;
            LuzPos[Luces] = posMundo;
            LuzRadio[Luces] = radio;
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
        /// bandera de UN frame: para las despedidas lentas (el sol negro).
        /// </summary>
        public static void PintoresSiempre(bool si)
        {
            _pintoresSiempre = si;
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

        /// <summary>¿Hay algo que dibujar SOBRE el velo?</summary>
        internal static bool HayContenido()
        {
            return Luces > 0 || _pintoresSiempre;
        }

        /// <summary>El fin del frame: las luces y banderas viven UN frame.</summary>
        internal static void FinDelFrame()
        {
            Luces = 0;
            _pintoresSiempre = false;
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

        // ==================================================================
        //  EL DIBUJADO DE LAS LUCES (aditivo, sobre el velo)
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
    //  VELOSISTEMA — EL PUNTO ÚNICO DEL FRAME (Main.OnPostDraw, la receta
    //  WotG: el velo se dibuja sobre el frame TERMINADO — el mundo, la
    //  interfaz y el cursor ya están; la oscuridad lo cubre TODO).
    // ======================================================================
    public class VeloSistema : ModSystem
    {
        public override void OnModLoad()
        {
            Main.OnPostDraw += DibujarElVelo;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DibujarElVelo;
        }

        /// <summary>La recarga del mod: mismo funeral, otra razón.</summary>
        public override void Unload()
        {
            try { Main.OnPostDraw -= DibujarElVelo; } catch { }
            Velo.Reset();
            Velo.OlvidarPintores();
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
        //  EL DIBUJADO — Main.OnPostDraw (la técnica de WotG):
        //  1. EL VELO: la capa oscura sobre TODO el frame (alfa).
        //  2. LAS LUCES Y LOS PINTORES: lo que brilla, DESPUÉS, en aditivo.
        //
        //  Cero manipulación del GraphicsDevice: Begin/End propios con
        //  identidad — nada que restaurar, nada que romper.
        // ==================================================================
        private static void DibujarElVelo(GameTime tiempo)
        {
            if (Velo.Roto) return;   // el cerrojo: a la tercera, retiro
            try
            {
                if (Main.gameMenu || Main.netMode == NetmodeID.Server)
                {
                    Velo.FinDelFrame();
                    return;
                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics == null ? null : Main.graphics.GraphicsDevice;
                if (sb == null || gd == null)
                {
                    Velo.FinDelFrame();
                    return;
                }

                int w = Main.screenWidth;
                int h = Main.screenHeight;
                if (w < 8 || h < 8)
                {
                    Velo.FinDelFrame();
                    return;
                }

                float inten = Velo.Intensidad;
                bool veloVisible = inten > 0.002f;
                if (!veloVisible && !Velo.HayContenido())
                {
                    Velo.FinDelFrame();
                    return;   // nada que hacer: este frame no toca el lote
                }

                Matrix mVista = Main.GameViewMatrix.ZoomMatrix;   // vista→dispositivo

                // === 1. EL VELO — la oscuridad sobre TODO (la receta WotG) ===
                if (veloVisible)
                {
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Matrix.Identity);
                    try { sb.Draw(VFXCore.Pixel, new Rectangle(0, 0, w, h), Velo.ColorDelFrame()); }
                    finally { sb.End(); }
                }

                // === 2. LO QUE BRILLA — las luces y los pintores, DESPUÉS
                //     del velo, en aditivo (el orden del DrawWhite de WotG) ===
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
            finally
            {
                Velo.FinDelFrame();
            }
        }
    }
}
