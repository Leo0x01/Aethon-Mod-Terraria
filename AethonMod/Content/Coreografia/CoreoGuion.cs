using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Coreografia
{
    // ======================================================================
    //  v6.52.0 — COREOGUION: EL MOTOR DE COREOGRAFÍA de los jefes.
    //
    //  LA INVESTIGACIÓN (Task 84 / R3 — notas en
    //  download/research/notes/R3-boss-choreography.md): el video del
    //  usuario («Terraria's NEWEST Mod Has a Crazy Boss Fight» — el
    //  Meowzers Boss Fight de Zenovia, el dev de Lunar Veil) y CINCO
    //  fuentes de código (LunarVeilLegacy + Calamity + Fargo's + Stars
    //  Above + Calamity Overhaul) destiladas a UNA gramática:
    //
    //    CADA ATAQUE = TELEGRAPH → ACTIVO → RECUPERACIÓN
    //      · telegraph 20–30 t (rápido) · 45–60 (normal) · 90–120
    //        (pesado) · 216 (el nuke estándar de Calamity/Ares)
    //      · la recuperación es la VENTANA VISIBLE de oportunidad (el
    //        jefe «exhala»: se apaga, baja el ritmo)
    //      · UNA señal por ataque: un sonido, un anuncio, una sacudida,
    //        un golpe de luz — la UNIFORMIDAD es lo que lee como
    //        coreografía (la lección del director de teatro)
    //
    //  LA IMPLEMENTACIÓN: una capa ENTRE la máquina de estados del jefe
    //  y su código por tick — NO reemplaza la selección de ataques: la
    //  ESTANDARIZA. Cada guion declara sus ventanas sobre el Compas de
    //  la casa y el DIRECTOR dispara los beats (anuncio/sonido/sacudida/
    //  luz) UNA sola vez, en el instante exacto.
    //
    //  EL CONTRATO DE RENDER (la lección v6.51.14): los callbacks Visual
    //  SOLO ENCOLAN quads de VFXCore — el volcado único vive en el
    //  PreDraw del jefe. Cero Begin/End en los guiones.
    // ======================================================================

    /// <summary>UN GUION DE ATAQUE completo: ventanas + callbacks + beats.</summary>
    public sealed class GuionAtaque
    {
        /// <summary>El nombre ANUNCIADO (la barra de conjuro y el aviso).</summary>
        public string ClaveNombre;

        /// <summary>El peso en la bolsa de selección (más peso = más frecuente).</summary>
        public int Peso = 2;

        /// <summary>El tinte del telegraph (frío, latiendo — se AVISA).</summary>
        public Color ColorTelegrafo = new(150, 110, 230);

        /// <summary>El tinte del golpe (caliente, el «AHORA»).</summary>
        public Color ColorActivo = new(255, 240, 210);

        /// <summary>El sonido al ARRANCAR el telegraph (el aviso de oído).</summary>
        public Terraria.Audio.SoundStyle SonidoInicio = SoundID.Item27;

        /// <summary>El sonido al ENTRAR en lo activo (el golpe).</summary>
        public Terraria.Audio.SoundStyle SonidoGolpe = SoundID.Item14;

        /// <summary>La sacudida de pantalla al entrar en lo activo (0 = nada).</summary>
        public float SacudidaGolpe = 3f;

        /// <summary>EL COMPÁS: las ventanas nombradas (telegrafo/activo/recuperación…).</summary>
        public Compas Compas;

        /// <summary>LA LÓGICA por tick (TODAS las máquinas — IA de juego).</summary>
        public Action<NPC, Player, Instante> IA;

        /// <summary>EL VISUAL por tick (SOLO encolar quads — clientes).</summary>
        public Action<NPC, Player, Instante> Visual;

        /// <summary>UNA vez al entrar en el ataque (spawn de proyectiles, sonidos).</summary>
        public Action<NPC, Player> AlEntrar;

        /// <summary>UNA vez al salir del ataque (limpieza).</summary>
        public Action<NPC, Player> AlSalir;

        /// <summary>¿Se ANUNCIA? (barra de conjuro + latido — el falso se calla).</summary>
        public bool Anunciar = true;

        /// <summary>EL BEAT DE SILENCIO: apaga el mundo un momento (la
        /// ilusión de tiempo-detenido SIN detours — el sustituto CWR).</summary>
        public bool SilencioDramatico;
    }

    /// <summary>EL DIRECTOR — ejecuta el guion tick a tick y dispara los beats.</summary>
    public static class CoreoGuion
    {
        // Los nombres de las ventanas canónicas (literales internados —
        // la comparación de Instante.En es por referencia, gratis).
        public const string Telegrafo = "telegrafo";
        public const string Activo = "activo";
        public const string Recuperacion = "recuperacion";

        /// <summary>
        /// LA TRISECCIÓN CANÓNICA: el guion de tres ventanas con sus
        /// tiempos en TICKS (la gramática de la investigación R3).
        /// </summary>
        public static GuionAtaque Triseccion(string claveNombre,
            float telegrafoT, float activoT, float recuperacionT,
            Action<NPC, Player, Instante> ia, Action<NPC, Player, Instante> visual,
            int peso = 2, float sacudida = 3f,
            Terraria.Audio.SoundStyle? inicio = null, Terraria.Audio.SoundStyle? golpe = null,
            Color? colorTelegrafo = null, Color? colorActivo = null,
            bool anunciar = true, bool silencio = false)
        {
            var guion = new GuionAtaque
            {
                ClaveNombre = claveNombre,
                Peso = peso,
                IA = ia,
                Visual = visual,
                SacudidaGolpe = sacudida,
                Anunciar = anunciar,
                SilencioDramatico = silencio,
                Compas = Compas.Nuevo(telegrafoT + activoT + recuperacionT)
                    .Con(Movimiento.Nuevo(Telegrafo, 0f, telegrafoT))
                    .Con(Movimiento.Nuevo(Activo, telegrafoT, activoT))
                    .Con(Movimiento.Nuevo(Recuperacion, telegrafoT + activoT, recuperacionT)),
            };
            if (inicio.HasValue) guion.SonidoInicio = inicio.Value;
            if (golpe.HasValue) guion.SonidoGolpe = golpe.Value;
            if (colorTelegrafo.HasValue) guion.ColorTelegrafo = colorTelegrafo.Value;
            if (colorActivo.HasValue) guion.ColorActivo = colorActivo.Value;
            return guion;
        }

        /// <summary>
        /// UN TICK DEL GUION: resuelve el <see cref="Instante"/> sobre el
        /// reloj del ataque, dispara los beats EXACTAMENTE UNA vez y
        /// llama a IA/Visual. La máscara de beats vive en npc.localAI[0]
        /// (bits 1/2/4/8 — se limpia al ENTRAR en el ataque).
        /// </summary>
        public static void Tick(NPC npc, Player presa, GuionAtaque guion, float reloj)
        {
            if (npc == null || guion == null) return;
            Instante inst = guion.Compas.En(reloj);
            int mascara = (int)npc.localAI[0];
            bool esCliente = Main.netMode != NetmodeID.Server;

            // === BEAT 1: EL TELEGRAPH ARRANCA (una señal de aviso por
            //     ataque: sonido + barra + el latido de luz fría) ===
            if (inst.PrimerTick && inst.En(Telegrafo) && (mascara & 1) == 0)
            {
                npc.localAI[0] = mascara | 1;
                if (esCliente)
                {
                    Terraria.Audio.SoundEngine.PlaySound(guion.SonidoInicio, npc.Center);
                    if (guion.Anunciar)
                        BarraConjuroUI.Mostrar(guion.ClaveNombre, guion.ColorTelegrafo,
                            inst.Movimiento.Duracion + DuracionDe(guion, Activo));
                }
                LuzEscenica.Latido(0, guion.ColorTelegrafo, 4f);
            }

            // === BEAT 2: LO ACTIVO ARRANCA (el golpe: sonido + sacudida +
            //     el destello de luz caliente + la barra se apaga) ===
            if (inst.PrimerTick && inst.En(Activo) && (mascara & 2) == 0)
            {
                npc.localAI[0] = mascara | 2;
                if (esCliente)
                {
                    Terraria.Audio.SoundEngine.PlaySound(guion.SonidoGolpe, npc.Center);
                    if (guion.SacudidaGolpe > 0f)
                        Pantalla.Sacudir(guion.SacudidaGolpe, 0.4f, "coreo");
                    if (guion.SilencioDramatico)
                        Pantalla.Vineta(0.8f, 0.9f);
                }
                LuzEscenica.Golpe(0, guion.ColorActivo, 2.2f);
                BarraConjuroUI.Ocultar();
            }

            // === BEAT 3: LA RECUPERACIÓN (el jefe «exhala»: la luz se
            //     vuelve ámbar tenue — la ventana de oportunidad SE VE) ===
            if (inst.PrimerTick && inst.En(Recuperacion) && (mascara & 4) == 0)
            {
                npc.localAI[0] = mascara | 4;
                LuzEscenica.Foco(1, npc.Center, new Color(190, 150, 90), 1.0f);
            }

            // === EL CUERPO: la lógica en todas las máquinas, el visual
            //     solo en clientes (y SOLO encola — el contrato) ===
            guion.IA?.Invoke(npc, presa, inst);
            if (esCliente)
                guion.Visual?.Invoke(npc, presa, inst);
        }

        /// <summary>Marca el guion como RECIÉN entrado (limpia la máscara).</summary>
        public static void Entrar(NPC npc, Player presa, GuionAtaque guion)
        {
            if (npc == null) return;
            npc.localAI[0] = 0f;                    // la máscara de beats nace limpia
            guion.AlEntrar?.Invoke(npc, presa);
        }

        /// <summary>Marca el guion como TERMINADO (dispara AlSalir una vez).</summary>
        public static void Salir(NPC npc, Player presa, GuionAtaque guion)
        {
            if (npc == null || guion == null) return;
            int mascara = (int)npc.localAI[0];
            if ((mascara & 8) != 0) return;         // ya salió
            npc.localAI[0] = mascara | 8;
            guion.AlSalir?.Invoke(npc, presa);
            BarraConjuroUI.Ocultar();
        }

        /// <summary>¿El reloj terminó el compás completo?</summary>
        public static bool Termino(GuionAtaque guion, float reloj) =>
            guion != null && reloj >= guion.Compas.Ciclo;

        private static float DuracionDe(GuionAtaque guion, string nombre)
        {
            foreach (var mov in guion.Compas.Movimientos)
                if (mov.Nombre == nombre) return mov.Duracion;
            return 0f;
        }
    }
}
