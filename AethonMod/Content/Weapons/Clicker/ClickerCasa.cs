using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Clicker
{
    // =====================================================================
    //  ClickerCasa — v6.82 — LA CLASE CLICKER DE LA CASA (standalone).
    //
    //  LA LETRA: «crea un arma nueva o varias con lo que creas correcto
    //  de la clase clicker, dale los efectos que sean necesarios,
    //  asegurate de que se vea bien tienes todas las librerias a tu
    //  disposicion».
    //
    //  LA INVESTIGACIÓN (el informe del subagente contra el código REAL
    //  del Clicker Class de SamsonAllen13/DivermanSam — los principios
    //  que hacen que la clase SE SIENTA BIEN):
    //   · EL CLIC ES EL USO DEL ARMA: useStyle=Shoot, useTime corto,
    //     autoReuse=FALSE (el clic manual ES la mecánica — mantener
    //     apretado no dispara: cada clic cuenta).
    //   · EL RADIO COMO CONTRATO VISIBLE: un círculo de color alrededor
    //     del JUGADOR dibujado SIEMPRE que se sostiene el arma (fade
    //     suave al cambiar), que BRILLA cuando el cursor está dentro
    //     («puedes clickear aquí») — las reglas del juego SIN texto.
    //   · FEEDBACK INMEDIATO MULTICAPA EN CADA CLIC: sonido seco, anillo
    //     PING en el cursor (partículas en círculo — la firma visual del
    //     Clicker Class), y el daño cae DONDE clickeas.
    //   · EL RITMO CON PROPÓSITO: cada N clics el COMBO detona el efecto
    //     especial del arma («3 clics más y sale el especial» — la
    //     cadencia convierte el spam en juego).
    //
    //  ESTA IMPLEMENTACIÓN (standalone — CERO dependencia del mod
    //  Clicker Class): el radio lo dibuja ClickerSistema (la capa de
    //  interfaz de la casa, patrón HazSobreElMundoSistema — ENCIMA del
    //  mundo, DEBAJO de la UI), el conteo de clics vive en ClickerJugador
    //  (ModPlayer) y el golpe nace en Main.MouseWorld como proyectil
    //  propio (30×30, atraviesa todo, 10 ticks — el pipeline vanilla lo
    //  sincroniza solo en MP).
    // =====================================================================

    /// <summary>
    /// EL ESTILO VISUAL DEL RADIO por arma: cómo se dibuja el círculo de
    /// contrato (todas comparten el aro fino + el brillo del cursor
    /// dentro; cada estilo añade SU firma encima).
    /// </summary>
    public enum EstiloClicker : byte
    {
        /// <summary>Anillo rúnico (el Compás Rúnico): CirculoRunico mini.</summary>
        Runico = 0,
        /// <summary>Perlas cardinales (la Furia Solar): 10 perlas doradas.</summary>
        Solar = 1,
        /// <summary>v6.100 — Sellos de tinta orbitando (el Sello del Escriba):
        /// 6 sellos violetas con su lluvia hacia adentro.</summary>
        Tinta = 2,
        /// <summary>v6.100 — El ojo que parpadea (el Parpadeo del Eterno):
        /// 4 pestañas de luz + el destino marcado.</summary>
        Ojo = 3,
    }

    /// <summary>
    /// EL NÚCLEO ESTÁTICO DE LA CLASE: el registro de armas clicker y el
    /// estado del frame (el arma sostenida por el jugador local + su
    /// alfa de fade — lo lee ClickerSistema para dibujar el radio).
    /// </summary>
    public static class ClickerCasa
    {
        /// <summary>Un arma clicker registrada: radio, color y estilo.</summary>
        public readonly struct ArmaClicker
        {
            public readonly float RadioPx;
            public readonly Color Color;
            public readonly EstiloClicker Estilo;
            public ArmaClicker(float radioPx, Color color, EstiloClicker estilo)
            {
                RadioPx = radioPx; Color = color; Estilo = estilo;
            }
        }

        /// <summary>Las armas registradas (la clave es el tipo de ítem).</summary>
        private static readonly Dictionary<int, ArmaClicker> _armas = new();

        /// <summary>Registra un arma clicker (lo llama su SetStaticDefaults).</summary>
        public static void Registrar(int itemType, float radioPx, Color color, EstiloClicker estilo)
        {
            _armas[itemType] = new ArmaClicker(radioPx, color, estilo);
        }

        /// <summary>Busca el arma del tipo dado (null si no es clicker).</summary>
        public static ArmaClicker? Buscar(int itemType)
            => _armas.TryGetValue(itemType, out ArmaClicker a) ? a : null;

        // --- EL ESTADO DEL FRAME (el fade del radio — lo escribe
        //     ClickerJugador cada tick, lo lee ClickerSistema al dibujar) ---

        /// <summary>¿El jugador local sostiene un clicker ESTE tick?</summary>
        internal static bool Sostenido;

        /// <summary>El alfa del radio (fade suave 0→1 al empuñar, 1→0 al soltar).</summary>
        internal static float AlfaRadio;

        /// <summary>¿El cursor está DENTRO del radio? (el brillo del contrato).</summary>
        internal static bool CursorDentro;

        /// <summary>El arma sostenida este tick (para el dibujado).</summary>
        internal static ArmaClicker Sostenida;
    }

    /// <summary>
    /// ClickerJugador — EL CONTEO DE CLICS por jugador (el combo de la
    /// casa): cada arma suma SUS clics mientras se empuña; el contador
    /// vive en el ModPlayer y las armas le preguntan cuándo detona el
    /// especial.
    /// </summary>
    public class ClickerJugador : ModPlayer
    {
        /// <summary>Los clics acumulados con el arma actual.</summary>
        public int Clics;

        /// <summary>El tipo del arma con la que se acumularon (cambiar de arma reinicia el combo).</summary>
        private int _tipoArma;

        public override void ResetEffects()
        {
            // EL ESTADO DEL FRAME para el dibujado del radio (solo el
            // jugador local: el radio es un contrato VISUAL del dueño).
            if (Player.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                ClickerCasa.Sostenida = default;
                ClickerCasa.Sostenido = false;
                Item mano = Player.HeldItem;
                if (mano != null && !mano.IsAir && mano.active)
                {
                    ClickerCasa.ArmaClicker? a = ClickerCasa.Buscar(mano.type);
                    if (a is ClickerCasa.ArmaClicker arma)
                    {
                        ClickerCasa.Sostenido = true;
                        ClickerCasa.Sostenida = arma;
                        float d = Vector2.DistanceSquared(Main.MouseWorld, Player.Center);
                        ClickerCasa.CursorDentro = d <= arma.RadioPx * arma.RadioPx;
                    }
                }
                // EL FADE SUAVE del contrato (la lección del Clicker
                // Class: sin pop, sin parpadeo — ±1/24 por tick).
                float objetivo = ClickerCasa.Sostenido ? 1f : 0f;
                ClickerCasa.AlfaRadio += MathHelper.Clamp(objetivo - ClickerCasa.AlfaRadio, -1f / 24f, 1f / 24f);
            }
        }

        /// <summary>
        /// REGISTRA UN CLIC del arma dada y devuelve el conteo NUEVO (las
        /// armas llaman esto desde Shoot y detonan su especial cuando el
        /// módulo llega a cero).
        /// </summary>
        public int RegistraClic(int tipoArma)
        {
            if (_tipoArma != tipoArma)
            {
                _tipoArma = tipoArma;
                Clics = 0;
            }
            Clics++;
            return Clics;
        }
    }

    /// <summary>
    /// ClickerSistema — LA CAPA DEL RADIO: el círculo de contrato del
    /// arma clicker dibujado alrededor del JUGADOR LOCAL (patrón de la
    /// casa HazSobreElMundoSistema: la capa propia en el íNDICE 0 —
    /// ENCIMA de todo el mundo, DEBAJO de toda la interfaz; el frame sin
    /// clicker no toca el spriteBatch NI UNA vez).
    /// </summary>
    public class ClickerSistema : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: El radio clicker",
                DibujarRadio, InterfaceScaleType.UI));
        }

        /// <summary>EL DELEGADO DE LA CAPA — el contrato visible.</summary>
        private bool DibujarRadio()
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return true;
            if (ClickerCasa.AlfaRadio <= 0.02f) return true;

            try
            {
                // (1) CERRAR el lote de interfaz que la capa abrió.
                VFXCore.CerrarLoteSiAbierto();

                // (2) EL LOTE ADITIVO DEL MUNDO (el ABRIR canónico de la
                //     casa — coordenadas de mundo, blend aditivo).
                VFXCore.AbrirAdditive();
                try
                {
                    DibujarElContrato();
                }
                finally
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            finally
            {
                // (4) LA DEVOLUCIÓN: el lote de UI que la capa espera.
                if (!VFXCore.LoteAbierto)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                    }
                    catch { }
                }
            }
            return true;
        }

        /// <summary>EL CÍRCULO DEL CONTRATO: aro fino + la firma del arma
        /// + el brillo cuando el cursor está dentro.</summary>
        private static void DibujarElContrato()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            ClickerCasa.ArmaClicker arma = ClickerCasa.Sostenida;
            float alfa = ClickerCasa.AlfaRadio;
            float radio = arma.RadioPx;
            // LAS COORDENADAS DE LA CASA: el lote aditivo abre con la
            // GameViewMatrix (zoom, sin traslación) — coords de PANTALLA
            // (el patrón de TODOS los renderers del mod).
            Vector2 centro = p.Center - Main.screenPosition;
            float t = Main.GlobalTimeWrappedHourly;

            // EL ARO FINO — el contrato en sí: más vivo cuando el cursor
            // está DENTRO (el «puedes clickear aquí» del Clicker Class).
            float vida = ClickerCasa.CursorDentro ? 0.55f : 0.28f;
            float pulso = 0.85f + 0.15f * MathF.Sin(t * 3.2f);
            VFXCore.RingQuad(centro, radio, t * 0.10f,
                VFXCore.TintAlfa(arma.Color, vida * pulso * alfa));

            // EL ARO INTERIOR tenue (la profundidad del contrato).
            VFXCore.RingQuad(centro, radio * 0.965f, -t * 0.06f,
                VFXCore.TintAlfa(arma.Color, 0.10f * alfa));

            // LA FIRMA DEL ARMA encima del aro.
            if (arma.Estilo == EstiloClicker.Runico)
            {
                // EL COMPÁS RÚNICO: 8 runas pequeñas viajando por el aro
                // (la librería de anillos rúnicos de la casa — el mismo
                // trazo de los agujeros negros, a escala de contrato).
                if (VFXCore.Presupuesto(60))
                    OrbitaLib.CirculoRunico(centro, radio, t, 1f, 8, 0.14f,
                        arma.Color, Color.White, 0.65f,
                        offset: 0, ringAlpha: 0f);
            }
            else if (arma.Estilo == EstiloClicker.Solar)
            {
                // LA FURIA SOLAR: 10 perlas cardinales latiendo en el aro
                // (el patrón de las perlas de la casa — el sol cuenta sus
                // horas alrededor del contrato).
                for (int i = 0; i < 10; i++)
                {
                    float ang = i * MathHelper.TwoPi / 10f + t * 0.10f;
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang) * radio, MathF.Sin(ang) * radio);
                    float late = 0.60f + 0.40f * MathF.Sin(t * 4f + i * 0.63f);
                    Color c = Color.Lerp(arma.Color, new Color(255, 246, 210), 0.55f);
                    Quad(VFXCore.SoftGlow, pos, new Vector2(9f, 9f) * late, 0f,
                        VFXCore.TintAlfa(c, 0.50f * late * alfa));
                }
            }
            else if (arma.Estilo == EstiloClicker.Tinta)
            {
                // v6.100 — EL SELLO DEL ESCRIBA: 6 sellos de tinta viajan
                // por el aro CONTRA el reloj, latiendo; y la tinta LLOVE
                // hacia el centro (los trazos que caen del contrato — cada
                // sello suelta su gota hacia adentro).
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * MathHelper.TwoPi / 6f - t * 0.35f;
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang) * radio, MathF.Sin(ang) * radio);
                    float late = 0.65f + 0.35f * MathF.Sin(t * 3f + i * 1.05f);
                    Quad(VFXCore.SoftGlow, pos, new Vector2(11f, 11f) * late, ang,
                        VFXCore.TintAlfa(arma.Color, 0.55f * late * alfa));
                    // LA GOTA: nace del sello y cae hacia el centro.
                    float cae = (t * 0.5f + i * 0.17f) % 1f;
                    Vector2 posGota = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang))
                        * radio * (1f - cae);
                    Quad(VFXCore.SoftGlow, posGota, new Vector2(4f, 7f), ang,
                        VFXCore.TintAlfa(new Color(120, 60, 190), 0.45f * (1f - cae) * alfa));
                }
            }
            else if (arma.Estilo == EstiloClicker.Ojo)
            {
                // v6.100 — EL PARPADEO DEL ETERNO: 4 pestañas de luz
                // orbitando lentamente (el ojo que no cierra del todo) +
                // EL DESTINO: un destello hacia el cursor cuando está
                // dentro del contrato (la mirada que te sigue).
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * MathHelper.TwoPi / 4f + t * 0.12f;
                    float late = 0.3f + 0.7f * MathF.Abs(MathF.Sin(t * 1.6f + i * 0.9f));
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang) * radio, MathF.Sin(ang) * radio);
                    Quad(VFXCore.SoftGlow, pos, new Vector2(8f, 16f) * late, ang,
                        VFXCore.TintAlfa(Color.Lerp(arma.Color, Color.White, 0.4f),
                            0.55f * late * alfa));
                }
                Vector2 destino = ClickerCasa.CursorDentro
                    ? Main.MouseWorld - Main.screenPosition : centro;
                Quad(VFXCore.SoftGlow, Vector2.Lerp(centro, destino, 0.35f),
                    new Vector2(14f, 14f) * (0.8f + 0.2f * MathF.Sin(t * 5f)), t,
                    VFXCore.TintAlfa(new Color(210, 180, 255), 0.35f * alfa));
            }

            // LA LUZ DEL CONTRATO: un baño tenue del color del arma sobre
            // el jugador (la presencia del arma — barato: UN quad).
            Quad(VFXCore.SoftGlow, centro, new Vector2(radio * 0.9f, radio * 0.9f), 0f,
                VFXCore.TintAlfa(arma.Color, 0.045f * alfa));
        }

        /// <summary>Quad centrado directo al lote aditivo activo (el
        /// pincel mínimo de la casa — SoftGlow estirado a size px).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }
    }
}
