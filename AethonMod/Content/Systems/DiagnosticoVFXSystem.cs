using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// DiagnosticoVFXSystem — v6.49 — EL OVERLAY DE PRUEBAS (la idea nº4
    /// de la auditoría AUD-C): el mod ES de pruebas — el usuario lo pide
    /// así ("el jugador debe tener la posibilidad de acceder fácil a
    /// TODO"). F8 enciende el panel de las entrañas:
    ///
    /// · FPS suaves y el FACTOR DE CALIDAD (el presupuesto adaptativo
    ///   que por fin respira: v6.49 lo conectó a AuraLib).
    /// · LOS QUADS del frame y el techo del presupuesto (con barra).
    /// · EcoLib: la voz activa y la cola pendiente.
    /// · AuraLib: emisores vivos (NPCs + jugador).
    /// · PulsoLib: trauma y fuerza de pantalla (el sistema CONECTADO hoy).
    /// · TiemposLib: el hitstop vivo — ticks de congelación restantes
    ///   (v6.80: la línea que la doc de la librería prometía y faltaba).
    /// · CalorLib: el filtro de cadena del aire que hierve — estado,
    ///   fuentes y el modo «por lente» (v6.81: la línea que habría cazado
    ///   el catch-22 en un segundo — «off · fuentes 2» era la firma).
    /// · GrimorioFuriaSistema: la oleada y la fase del festín.
    ///
    /// REGLA DE LA CASA: cero alocaciones por frame — los textos se
    /// reconstruyen cada 15 ticks (4 Hz, suficiente para diagnóstico) y
    /// las medidas se cachean con el texto. La tecla F8 se lee con el
    /// patrón de CompasLib (flanco, no nivel). Y la HIGIENE: este mismo
    /// sistema también barre VFXCore al descargar (Reiniciar).
    /// </summary>
    public class DiagnosticoVFXSystem : ModSystem
    {
        private static bool _abierto;
        private static bool _teclaAntes;

        /// <summary>
        /// v6.50.3 — SHOWDEBUGINFO CONECTADO (la config fantasma): la opción
        /// existía desde el inicio y nadie la leía. true = el panel arranca
        /// ABIERTO al entrar al mundo (la herramienta del probador a la
        /// vista sin tocar F8); F8 lo alterna como siempre. En MP es una
        /// decisión de PANTALLA — la config ClientSide de cada jugador.
        /// </summary>
        public override void OnWorldLoad()
        {
            try { _abierto = ModContent.GetInstance<global::AethonMod.Content.AethonConfig>()?.ShowDebugInfo ?? false; }
            catch { _abierto = false; }
        }

        /// <summary>Al descargar el mundo: el panel muere con él.</summary>
        public override void OnWorldUnload() => _abierto = false;

        // EL CACHE (4 Hz — cero GC entre refrescos).
        // v6.50.2 — _tickRefresco jubilado: el flanco del % 15u manda.
        // v6.80 — 9 líneas: HITSTOP se suma (la auditoría v6.78: el F8 no
        // mostraba TiemposLib.CongeladoRestante pese a que su doc lo
        // prometía — doc-rot).
        // v6.81 — 10 líneas: CALOR se suma (la telemetría de la cura del
        // catch-22: «off · fuentes 2» habría delatado el encierro al
        // instante — el filtro jamás podía activarse desde dentro del
        // hook de EndCapture).
        private static readonly string[] _lineas = new string[10];
        private static readonly Vector2[] _medidas = new Vector2[10];
        private static int _nLineas;

        public override void UpdateUI(GameTime gameTime)
        {
            // EL FLANCO DE LA TECLA (patrón de la casa: borde, no nivel).
            bool tecla = Main.keyState.IsKeyDown(Keys.F8);
            if (tecla && !_teclaAntes)
            {
                _abierto = !_abierto;
                if (_abierto) Refrescar(); // al abrir: datos YA
            }
            _teclaAntes = tecla;

            if (!_abierto) return;

            // v6.50.2 — FIX (refresco CADA frame): el viejo `% 15` cambiaba
            // de valor en CADA tick (0..14 cíclico) → el != disparaba
            // Refrescar() a 60 Hz — justo lo que el cache de 4 Hz prometía
            // evitar. Refresco solo al flanco del período.
            if (Main.GameUpdateCount % 15u == 0u)
                Refrescar();
        }

        /// <summary>Reconstruye las líneas del panel (4 Hz cuando abierto).</summary>
        private static void Refrescar()
        {
            try
            {
                var font = Terraria.GameContent.FontAssets.MouseText.Value;

                int i = 0;
                void Linea(string clave, params object[] args)
                {
                    if (i >= _lineas.Length) return;
                    string texto = Language.GetTextValue(clave, args);
                    _lineas[i] = texto;
                    _medidas[i] = font.MeasureString(texto) * 0.85f;
                    i++;
                }

                Linea("Mods.AethonMod.Diag.Fps", (int)Main.frameRate);
                Linea("Mods.AethonMod.Diag.Factor", VFXCore.FactorCalidad);
                Linea("Mods.AethonMod.Diag.Quads", VFXCore.QuadsDelFrame, 24000);
                Linea("Mods.AethonMod.Diag.Eco", EcoLib.ColasPendientes);
                Linea("Mods.AethonMod.Diag.Aura", AuraLib.EmisoresVivos);
                Linea("Mods.AethonMod.Diag.Pulso", PulsoLib.FuerzaPantalla);
                // v6.80 — LA LÍNEA DEL HITSTOP (higiene de doc de la
                // auditoría v6.78): TiemposLib documenta «la casa lo
                // muestra en el F8» y el panel jamás lo pintaba. Es un
                // int (ticks restantes; 0 = mundo respirando normal).
                Linea("Mods.AethonMod.Diag.Hitstop", TiemposLib.CongeladoRestante);
                // v6.81 — LA LÍNEA DEL CALOR: el filtro de cadena vive o
                // muere en el PostUpdate (la cura del catch-22) y esta
                // línea lo delata todo: «off» con fuentes registradas = el
                // encierro volvió; «por lente» = el forzador de captura
                // sosteniendo a la lente con el campo vacío.
                Linea("Mods.AethonMod.Diag.Calor", CalorLib.Diagnostico());
                // v6.50.2 — la furia en clientes MP llega por los estáticos
                // que EcoRed.MsgHambre llena (GrimorioFuriaSistema.
                // Diagnostico decide server vs cliente por netMode).
                Linea("Mods.AethonMod.Diag.Furia", GrimorioFuriaSistema.Diagnostico());
                Linea("Mods.AethonMod.Diag.Ayuda");
                _nLineas = i;
            }
            catch { _nLineas = 0; }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!_abierto) return;

            // Tras la barra del grimorio (la familia de UI de la casa).
            int idx = layers.FindIndex(l => l.Name == "AethonMod: Barra XP Grimorio");
            if (idx == -1) idx = layers.FindIndex(l => l.Name == "Vanilla: Hotbar");
            if (idx == -1) return;
            layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                "AethonMod: Diagnostico VFX",
                () => { Dibujar(); return true; },
                InterfaceScaleType.UI));
        }

        private static void Dibujar()
        {
            try
            {
                if (!_abierto || _nLineas == 0) return;

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                var font = Terraria.GameContent.FontAssets.MouseText.Value;

                // EL PANEL: columna en la esquina inferior izquierda (bajo
                // la barra dorada del grimorio, la familia junta).
                float x = 20f;
                float y = Main.screenHeight - 40f - _nLineas * 20f;

                // EL FONDO del panel (una sola pasada).
                sb.Draw(pixel, new Rectangle((int)(x - 6), (int)(y - 6), 360, _nLineas * 20 + 12),
                    new Color(12, 10, 20) * 0.72f);

                for (int i = 0; i < _nLineas; i++)
                {
                    if (_lineas[i] == null) continue;
                    // v6.50.2 — FIX (tinte de la línea de ayuda caía en la de
                    // FURIA); v6.81 — el orden real de Linea() hoy: 0 Fps,
                    // 1 Factor, 2 Quads, 3 Eco, 4 Aura, 5 Pulso, 6 Hitstop,
                    // 7 Calor, 8 Furia, 9 AYUDA — ayuda es la 9 (la última).
                    Color tinte = i == 9
                        ? new Color(150, 148, 142) // la línea de ayuda, discreta
                        : new Color(196, 232, 255);
                    sb.DrawString(font, _lineas[i], new Vector2(x, y + i * 20f),
                        tinte * 0.92f, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                }
            }
            catch { }
        }
    }
}
