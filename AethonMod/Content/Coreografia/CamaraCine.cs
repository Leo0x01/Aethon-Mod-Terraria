using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Coreografia
{
    // ======================================================================
    //  v6.52.0 — CAMARACINE: LA CÁMARA CINEMÁTICA (el «FocusOn» de
    //  Lunar Veil — el dev del video del usuario — re-implementado con
    //  las reglas de la casa).
    //
    //  LA RECETA (R3 §2.1 — Sylia: FocusOn + Screen_Black): en los
    //  momentos clave (transiciones de fase, el nuke, la muerte), la
    //  cámara SE ACERCA al jefe — SmoothStep 0.05/t hacia el objetivo,
    //  sostiene, y vuelve suave. Nunca si el jugador está muerto o el
    //  foco lejos (> 2000 px — la cámara no abandona al jugador).
    //
    //  LA IMPLEMENTACIÓN CASA: ModifyScreenPosition (el hook oficial) +
    //  marcas de frame (PantallaLib contract: deriva, sin timers) y
    //  ADITIVO con la sacudida de PantallaLib (primero el foco, luego
    //  el temblor encima).
    // ======================================================================
    public class CamaraCine : ModSystem
    {
        // === EL ESTADO (marcas de frame — todo derivado) ===
        private static Vector2 _objetivo;
        private static uint _frameInicio;
        private static uint _duracion;
        private static bool _activa;

        /// <summary>
        /// ENFOCA la cámara en un punto del mundo durante N segundos
        /// (con ease-in/out suave — el «FocusOn» de Lunar Veil).
        /// </summary>
        public static void Enfocar(Vector2 posMundo, float segundos)
        {
            if (Main.netMode == NetmodeID.Server) return;
            _objetivo = posMundo;
            _frameInicio = Main.GameUpdateCount;
            _duracion = (uint)MathHelper.Max(30f, segundos * 60f);
            _activa = true;
        }

        public static void Cancelar() => _activa = false;

        public override void ModifyScreenPosition()
        {
            if (!_activa || Main.gameMenu) return;

            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead)
            {
                _activa = false;
                return;
            }

            uint edad = Main.GameUpdateCount - _frameInicio;
            if (edad >= _duracion)
            {
                // El regreso termina solo cuando el offset VOLVIÓ (el
                // ease-out de la última fase ya está en ~0 aquí).
                _activa = false;
                return;
            }

            // NUNCA abandonar al jugador: el foco lejos no engancha.
            Vector2 centroPantalla = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            if (Vector2.Distance(centroPantalla, _objetivo) > 2000f) return;

            // LA ENVOLVENTE: ease-in (primer 15%) → sostener → ease-out
            // (último 30%) — la curva del cine.
            float t = edad / (float)_duracion;
            float envolvente;
            if (t < 0.15f) envolvente = Suave(t / 0.15f);
            else if (t > 0.70f) envolvente = Suave((1f - t) / 0.30f);
            else envolvente = 1f;

            // EL DESPLAZAMIENTO: hacia el objetivo, JAMÁS más de 420 px
            // (el centro se acerca, no se entrega).
            Vector2 delta = _objetivo - centroPantalla;
            if (delta.LengthSquared() > 1f)
            {
                float clamp = MathHelper.Min(delta.Length(), 420f);
                delta = Vector2.Normalize(delta) * clamp;
            }
            Main.screenPosition += delta * (0.55f * envolvente);
        }

        private static float Suave(float p)
        {
            p = MathHelper.Clamp(p, 0f, 1f);
            return p * p * (3f - 2f * p);
        }

        public override void OnWorldUnload() => _activa = false;
        public override void Unload() => _activa = false;
    }
}
