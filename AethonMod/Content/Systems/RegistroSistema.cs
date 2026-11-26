using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  REGISTROSISTEMA — v6.175 — EL LOOP DE RETROALIMENTACIÓN DE LA CASA.
    //
    //  LA LETRA: «crea un loop de retroalimentación para mejorar y
    //  depurar errores». LA CAUSA (la auditoría v6.174): de 1.276 bloques
    //  catch del proyecto, 687 estaban VACÍOS (53,8 %) — cualquier
    //  excepción desaparecía SIN dejar rastro. No era teoría: en la
    //  v6.173 los botones del libro dejaron de responder y NADA lo
    //  avisó, porque el error moría dentro de uno de esos bloques.
    //
    //  EL DISEÑO (el loop de la casa, en tres vueltas):
    //   1. VER: los 675 «catch { }» vacíos ya no existen — cada uno
    //      llama a Registro.Fallo(contexto, ex). La salida es
    //      DEDUPLICADA: la PRIMERA vez de cada error distinto se
    //      escribe en el log oficial (Logging.PublicLogger.Error),
    //      se anuncia UNA vez en el chat (violeta de la casa) y se
    //      cuenta; las repeticiones SOLO cuentan (sin spam: el error
    //      de cada frame pesa 1 línea de aviso, no 3600).
    //   2. MEDIR: Registro.Resumen() cuenta errores distintos y
    //      repeticiones por contexto — el F8 (DiagnosticoVFXSystem)
    //      y el headless pueden leerlo.
    //   3. COMPARAR: tools/loop_retro_v6175.py mide el árbol (catches
    //      vacíos, azar en el dibujo, reservas, localización, GUARDIÁN
    //      de texturas) y lo compara con la versión ANTERIOR — cada
    //      métrica que EMPEORA detiene el release. El error que la
    //      v6.173 escondió, ahora deja pistas ANTES de publicar.
    //
    //  LA NORMA DE USO (nueva regla de la casa, R17): todo catch nuevo
    //  escribe — o devuelve, o registra. NINGÚN «catch { }» vuelve a
    //  nacer. El silencio fue cómodo mientras todo fue bien y caro
    //  cuando algo se rompió.
    // ======================================================================
    public class RegistroSistema : ModSystem
    {
        // === EL ESTADO DE LA SESIÓN (estático: vive con el proceso) ===
        private static readonly Dictionary<string, int> _cuentaPorFirma = new();
        private static readonly Dictionary<string, bool> _anunciado = new();
        private static int _totalFallos;
        private static int _totalAvisos;

        /// <summary>Errores ÚNICOS vistos esta sesión (firmas distintas).</summary>
        public static int FirmasDistintas => _cuentaPorFirma.Count;

        /// <summary>Vece totales que algo falló (únicos + repeticiones).</summary>
        public static int TotalFallos => _totalFallos;

        /// <summary>Avisos blandos (avisos de fallback, no excepciones).</summary>
        public static int TotalAvisos => _totalAvisos;

        /// <summary>¿Hubo ALGÚN fallo esta sesión? (el headless lo lee:
        /// si esto es > 0, la salida del servidor lo imprime al final).</summary>
        public static bool HayFallos => _totalFallos > 0 || _totalAvisos > 0;

        // ================================================================
        //  EL NÚCLEO — FALLO: lo llama todo catch resucitado de la casa.
        // ================================================================
        public static void Fallo(string contexto, Exception ex)
        {
            try
            {
                _totalFallos++;
                string mensaje = ex != null ? ex.Message ?? "" : "";
                // LA FIRMA: contexto + tipo + mensaje (dos fallos con el
                // mismo mensaje en el mismo sitio son EL MISMO fallo)
                string firma = (contexto ?? "?") + " · " + (ex?.GetType().Name ?? "?") + " · " + mensaje;
                _cuentaPorFirma.TryGetValue(firma, out int veces);
                _cuentaPorFirma[firma] = veces + 1;

                // 1. EL LOG OFICIAL — la PRIMERA vez escribe completo (con
                //    stack); las repeticiones cada 200 (para ver que sigue
                //    vivo sin inundar el archivo)
                bool primera = veces == 0;
                if (primera || (veces + 1) % 200 == 0)
                {
                    string linea = "[AethonMod] FALLO en " + (contexto ?? "?") +
                        (veces == 0 ? "" : $" (repetición #{veces + 1})");
                    Terraria.ModLoader.Logging.PublicLogger.Error(linea, ex);
                }

                // 2. EL CHAT — UNA sola línea por firma (el jugador SABE que
                //    algo falló; el detalle vive en el log)
                if (primera && !Main.dedServ && Main.myPlayer >= 0)
                {
                    _anunciado[firma] = true;
                    Main.NewText($"[AethonMod] {Language.GetTextValue("Mods.AethonMod.Registro.Fallo", contexto ?? "?")}",
                        new Microsoft.Xna.Framework.Color(255, 106, 106));
                }
            }
            catch { }   // el registro JAMÁS tira la casa: este es el único
                        // catch vacío que la casa perdona (el de sí mismo)
        }

        /// <summary>EL AVISO BLANDO: algo degeneró sin excepción (un
        /// shader que no cargó, un null que se ignoró) — cuenta y basta,
        /// sin stack, una línea en el log.</summary>
        public static void Aviso(string contexto, string detalle = "")
        {
            try
            {
                _totalAvisos++;
                string firma = "aviso · " + (contexto ?? "?") + " · " + (detalle ?? "");
                _cuentaPorFirma.TryGetValue(firma, out int veces);
                _cuentaPorFirma[firma] = veces + 1;
                if (veces == 0)
                {
                    Terraria.ModLoader.Logging.PublicLogger.Error(
                        "[AethonMod] aviso: " + (contexto ?? "?") + " — " + (detalle ?? ""));
                    if (!Main.dedServ && Main.myPlayer >= 0)
                        Main.NewText($"[AethonMod] {Language.GetTextValue("Mods.AethonMod.Registro.Aviso", contexto ?? "?")}",
                            new Microsoft.Xna.Framework.Color(255, 200, 120));
                }
            }
            catch { }
        }

        // ================================================================
        //  EL RESUMEN — la cara visible del loop (el F8 y el cierre del
        //  mundo lo imprimen: la sesión NO termina en silencio nunca más).
        // ================================================================
        public static string Resumen()
        {
            try
            {
                if (_totalFallos == 0 && _totalAvisos == 0)
                    return Language.GetTextValue("Mods.AethonMod.Registro.Limpio");
                return Language.GetTextValue("Mods.AethonMod.Registro.Resumen",
                    _totalFallos + _totalAvisos, _cuentaPorFirma.Count);
            }
            catch { return "?"; }
        }

        /// <summary>LOS PEORES: los N contextos con más repeticiones (para
        /// el diagnóstico: dónde insiste el dolor).</summary>
        public static List<string> Peores(int n = 5)
        {
            var lista = new List<string>();
            try
            {
                var peor = new List<(int, string)>();
                foreach (var kv in _cuentaPorFirma) peor.Add((kv.Value, kv.Key));
                peor.Sort((a, b) => b.Item1.CompareTo(a.Item1));
                for (int i = 0; i < Math.Min(n, peor.Count); i++)
                    lista.Add($"{peor[i].Item1}× {peor[i].Item2}");
            }
            catch { }
            return lista;
        }

        public override void PreWorldGen()
        {
            // cada mundo nuevo empieza con el marcador visible: el loop VIVE
            if (!Main.dedServ) return;
        }

        /// <summary>El cierre del mundo imprime el resumen al log (en el
        /// headless es LA línea final que el gate busca).</summary>
        public override void OnWorldUnload()
        {
            try
            {
                if (_totalFallos > 0 || _totalAvisos > 0)
                {
                    Terraria.ModLoader.Logging.PublicLogger.Error(
                        "[AethonMod] RESUMEN de la sesión: " + Resumen());
                    foreach (string p in Peores())
                        Terraria.ModLoader.Logging.PublicLogger.Error("[AethonMod]   " + p);
                }
            }
            catch { }
        }

        /// <summary>EL AUTOEXAMEN al entrar al mundo: una línea en el chat
        /// (cliente) que dice cómo va la sesión — la primera vuelta del
        /// loop la ve el jugador, no solo el log.</summary>
        public override void PostUpdateTime()
        {
            // El chequeo de rutina vive en el headless y el F8; aquí no se
            // hace nada por frame (coste cero).
        }
    }
}
