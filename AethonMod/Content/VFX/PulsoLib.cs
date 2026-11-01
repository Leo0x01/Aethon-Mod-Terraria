using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PulsoLib — v6.31 — LA SUPER LIBRERÍA DEL GAME FEEL: "el sistema
    /// nervioso" del mod.
    ///
    /// Nace de la super investigación (INFORME_TOP_MODS_LIBRERIAS §C.2): lo
    /// que hace que un golpe SE SIENTA — la cámara, el trauma y el estado
    /// de pantalla — estaba disperso en improvisaciones de cada renderer.
    /// Los mods top lo llevan PEGADO a miles de archivos; NADIE lo publica
    /// como API. (v6.80 — la purga de muertos dejó las CINCO piezas
    /// originales en TRES: solo respira lo que alguien llama.)
    ///
    ///   · EL JUICIO DIFERIDO — purgado v6.80 (inerte desde v6.56: su único
    ///     escritor murió en la purga de la auditoría §3, la lista de
    ///     veredictos nunca volvió a llenarse y Marcar/Liquidar acabaron
    ///     sin un solo llamador en todo el mod).
    ///   · EL RETROCESO — purgado v6.80 (el struct Derivar quedó huérfano:
    ///     dos números que ya nadie pasaba — cero llamadores en el mod).
    ///   · LA CÁMARA: un solo punto de acceso al PunchCameraModifier real,
    ///     con try/catch a prueba de balas.
    ///   · v6.34 — EL TRAUMA: el shake ACUMULATIVO con decaimiento — el castigo
    ///     sostenido SANGRA la cámara a razón de trauma² (los golpes pequeños
    ///     casi no se sienten, los grandes sacuden) y se disipa solo.
    ///   · EL SONIDO POR MATERIAL — purgado v6.80 (el enum Material nació
    ///     sin boca: el reproductor por material nunca llegó a escribirse y
    ///     nadie leía el enum — cero llamadores).
    ///   · LA PANTALLA: un estado push/decay con apilamiento limitado —
    ///     todos los flashes del mod pasan por aquí y decaen iguales.
    ///     (v6.80 — y por fin SE VEN: PulsoSistema dibuja EL VELO DE TINTE
    ///     en PostDrawInterface; desde v6.31 hasta v6.79 el estado vivía
    ///     ciego — el bug nº1 del Grupo B de la auditoría, curado.)
    ///
    /// Contrato: NADA de Main.rand (todo determinista por semilla/whoAmI).
    /// </summary>
    public static class PulsoLib
    {
        // ==================================================================
        //  3 — LA CÁMARA (un solo punto de acceso, a prueba de balas)
        // ==================================================================

        /// <summary>
        /// GOLPEA LA CÁMARA en una dirección (pásale la OPUESTA al golpe para
        /// el feel de reacción física). Envuelve el PunchCameraModifier real
        /// con try/catch — NUNCA revienta un tiro por la cámara.
        /// </summary>
        public static void GolpearCamara(Vector2 dirección, float fuerzaPx, int ticks)
        {
            if (Main.netMode == NetmodeID.Server || fuerzaPx <= 0.05f) return;
            try
            {
                Vector2 dir = dirección.LengthSquared() > 0.0001f ? Vector2.Normalize(dirección) : Vector2.UnitY;
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Main.LocalPlayer.Center, dir, fuerzaPx, ticks, Math.Min(ticks, 12), 0.35f,
                    "AethonPulso"));
            }
            catch { }
        }

        // ------------------------------------------------------------------
        //  v6.34 — EL TRAUMA (el shake que se ACUMULA y se disipa solo)
        // ------------------------------------------------------------------

        /// <summary>
        /// La fuerza del shake con el trauma AL 100% (px) — el techo del temblor.
        /// </summary>
        private const float FuerzaTraumaMax = 8f;

        /// <summary>El castigo acumulado (0..1): se suma con <see cref="Trauma"/>.</summary>
        private static float _trauma;

        private static uint _frameTrauma;

        /// <summary>
        /// v6.34 — ACUMULA TRAUMA DE CÁMARA (0..1, clampeado): a diferencia del
        /// shake directo (GolpearCamara: un golpe, un tirón, se acabó), el
        /// trauma SE SUMA y el sistema lo va sangrando solo (−0.02/tick en
        /// <see cref="ActualizarPantalla"/>). EL TRAUMA AL CUADRADO es el
        /// concepto: un golpe pequeño (trauma 0.1 → shake 0.08 px) es casi
        /// IMPERCEPTIBLE; uno grande (trauma 0.8 → shake ×0.64 de la fuerza
        /// máxima) SACUDE de verdad — la curva no lineal es lo que separa el
        /// "golpecito" del "porrazo". El shake sale por la MISMA puerta que
        /// GolpearCamara (el PunchCameraModifier de la casa): UN solo sistema
        /// de cámara, nunca dos shakes desacoplados.
        /// </summary>
        /// <param name="cantidad">Cuánto trauma se suma (0..1 recomendado;
        /// típico 0.10-0.35 por golpe).</param>
        public static void Trauma(float cantidad)
            => _trauma = MathHelper.Clamp(_trauma + cantidad, 0f, 1f);

        /// <summary>
        /// EL LATIDO DEL TRAUMA (v6.34, una vez por frame desde
        /// <see cref="ActualizarPantalla"/>): calcula el shake como
        /// trauma² × <see cref="FuerzaTraumaMax"/> y lo empuja por la puerta de
        /// cámara de la casa — dirección por Hash01 (el "random" determinista
        /// de la casa; JAMÁS Main.rand) — y desangra el trauma a 0.02/tick
        /// (50 ticks de vida desde el tope, ~1 s a 60 fps).
        /// </summary>
        private static void LatidoTrauma()
        {
            if (_trauma <= 0f) return;
            uint frame = Main.GameUpdateCount;
            if (frame == _frameTrauma) return;   // un latido por frame
            _frameTrauma = frame;

            // EL SHAKE AL CUADRADO: los golpes pequeños casi no se sienten,
            // los grandes sacuden (ver Trauma).
            float shake = _trauma * _trauma * FuerzaTraumaMax;
            if (shake > 0.05f)
            {
                float ang = VFXCore.Hash01((int)frame, 17, 977) * MathHelper.TwoPi;
                GolpearCamara(new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)),
                    shake, 6);
            }

            // EL DESANGRADO: −0.02 por tick, jamás negativo.
            _trauma = Math.Max(0f, _trauma - 0.02f);
        }

        // ==================================================================
        //  5 — LA PANTALLA (estado push/decay con apilamiento limitado)
        // ==================================================================

        private static float _fuerzaPantalla;
        private static Color _tintePantalla = Color.White;
        private static int _ticksPantalla;
        private static uint _framePantalla;

        /// <summary>La fuerza ACTUAL del estado de pantalla (0..1+; decae sola).</summary>
        public static float FuerzaPantalla => _fuerzaPantalla;

        /// <summary>El TINTE actual del estado de pantalla (para el vignette).</summary>
        public static Color TintePantalla => _tintePantalla;

        /// <summary>
        /// EMPUJA el estado de pantalla (flash suave global): apila MÁXIMO 3
        /// empujes y decae exponencialmente — todos los flashes del mod
        /// pasan por aquí y mueren IGUAL (la consistencia que los
        /// improvisaciones no tenían).
        /// v6.80 — EL VELO VIVE: PulsoSistema.PostDrawInterface dibuja este
        /// estado como un baño de tinte plano de pantalla completa (alfa =
        /// FuerzaPantalla·0,6). El que empuja ya no escribe en el vacío.
        /// </summary>
        public static void EmpujarPantalla(Color tinte, float fuerza, int ticks)
        {
            if (Main.netMode == NetmodeID.Server) return;
            _fuerzaPantalla = MathHelper.Clamp(_fuerzaPantalla + fuerza, 0f, 0.85f);
            _tintePantalla = tinte;
            _ticksPantalla = Math.Max(_ticksPantalla, Math.Max(4, ticks));
            _framePantalla = Main.GameUpdateCount;
        }

        /// <summary>
        /// EL DECAIMIENTO (lo llama un sistema por frame — o el primer
        /// Empujar del frame siguiente): expónencial, sin estados colgados.
        /// </summary>
        public static void ActualizarPantalla()
        {
            LatidoTrauma();   // v6.34 — el trauma también late aquí (el latido único por frame)
            if (_ticksPantalla <= 0) { _fuerzaPantalla = 0f; return; }
            uint frame = Main.GameUpdateCount;
            if (frame == _framePantalla) return;   // un decay por frame
            _framePantalla = frame;
            _ticksPantalla--;
            _fuerzaPantalla *= 0.86f;
            if (_ticksPantalla <= 0 || _fuerzaPantalla < 0.004f)
            {
                _fuerzaPantalla = 0f;
                _ticksPantalla = 0;
            }
        }

        /// <summary>
        /// v6.49 — LA HIGIENE (hallazgo AUD-A: las estáticas del trauma y
        /// de la pantalla sobrevivían al cambio de mundo). Lo llama
        /// PulsoSistema en OnWorldUnload/Unload: cero trauma heredado,
        /// cero flash colgando en el mundo siguiente.
        /// </summary>
        public static void Reset()
        {
            _trauma = 0f;
            _frameTrauma = 0;
            _fuerzaPantalla = 0f;
            _tintePantalla = Color.White;
            _ticksPantalla = 0;
            _framePantalla = 0;
        }
    }
}
