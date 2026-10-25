using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Skills
{
    // ======================================================================
    //  v6.52.0 — LAS INSCRIPCIONES DE LA PLUMA: el sistema de ARMAS VIVAS
    //  con ÁRBOL DE HABILIDADES funcional por arma.
    //
    //  LA INVESTIGACIÓN (Task 84 / R1 — notas en
    //  download/research/notes/R1-calamity-overhaul.md, extraídas del
    //  Calamity Overhaul DECOMPILADO): su katana Onikiri guarda la
    //  conciencia de Himura Mayo y su «árbol» son 36 INSCRIPCIONES
    //  (OniMei) repartidas en 3 ranuras (Nakago/Hi/Horimono), cada una
    //  con PODER y CARGA (power + burden), desbloqueadas por HECHOS
    //  (deeds: retos de juego) o REGALOS (reacciones del espíritu a cada
    //  jefe), con la biblioteca en el JUGADOR (ModPlayer) y los SLOTS en
    //  el ÍTEM (GlobalItem) — el arma crece CONTIGO, la biblioteca crece
    //  por jugador.
    //
    //  EL ADN COPIADO (las reglas de diseño de CWR, robadas verbatim):
    //    · Las definiciones son SINGLETONES GLOBALES INMUTABLES — cero
    //      estado en ellas; el estado vive en el jugador o el ítem.
    //    · Composición SOLO multiplicativa («*=», nunca asignar): los
    //      slots jamás se pisan entre sí.
    //    · Todo poder lleva su CARGA (el trade-off que hace que armar
    //      la pluma sea una DECISIÓN).
    //    · CLAVES estables de tipo («renombrar es romper guardados»).
    //    · Los nodos bloqueados muestran PROGRESO, no efectos.
    //    · CERO moneda de puntos: la progresión SON los hechos y los
    //      regalos (la filosofía declarada de CWR).
    //
    //  LA VOZ: la conciencia de la pluma es EL ESCRIBA MISMO — la mano
    //  que escribió las páginas que Aethon devoró, atada a su última
    //  pluma. Habla, regaña, celebra y desbloquea sus propias palabras
    //  cuando el portador las gana.
    // ======================================================================

    /// <summary>Las TRES ranuras de la pluma (una inscripción por ranura).</summary>
    public enum RanuraInscripcion
    {
        /// <summary>FILIGRANA — la ornamentación: utilidad, economía, cadencia.</summary>
        Filigrana = 0,
        /// <summary>VERSO — la ofensiva: daño, proyectiles, buscapresas.</summary>
        Verso = 1,
        /// <summary>COLOFÓN — el remate: crítico, ejecución, robo de vida.</summary>
        Colofon = 2,
    }

    /// <summary>
    /// EL PERFIL DE COMBATE — la composición de las tres inscripciones
    /// equipadas (la regla de la casa copiada de OniMeiCombatProfile: SOLO
    /// multiplicadores y sumas semánticas; jamás asignaciones).
    /// </summary>
    public struct PerfilInscripcion
    {
        public float DañoMul;          // *= — multiplicador de daño
        public float VelocidadMul;     // *= — multiplicador de cadencia
        public float ManaMul;          // *= — multiplicador de coste de maná
        public float CriticoSuma;      // += — crítico plano
        public int ProyectilesExtra;   // += — bolts adicionales por disparo
        public int PerforacionExtra;   // += — perforación adicional
        public bool BuscaPresa;        // |= — los bolts persiguen
        public bool EstelaTinta;       // |= — estela de tinta viva
        public bool RoboVida;          // |= — cada golpe devuelve vida
        public bool Ejecutar;          // |= — remate contra heridas de muerte

        /// <summary>El perfil NEUTRO (todas las ranuras vacías).</summary>
        public static PerfilInscripcion Neutral() => new PerfilInscripcion
        {
            DañoMul = 1f,
            VelocidadMul = 1f,
            ManaMul = 1f,
        };

        /// <summary>Acumula otra inscripción (composición multiplicativa).</summary>
        public void Acumular(PerfilInscripcion otra)
        {
            DañoMul *= otra.DañoMul;
            VelocidadMul *= otra.VelocidadMul;
            ManaMul *= otra.ManaMul;
            CriticoSuma += otra.CriticoSuma;
            ProyectilesExtra += otra.ProyectilesExtra;
            PerforacionExtra += otra.PerforacionExtra;
            BuscaPresa |= otra.BuscaPresa;
            EstelaTinta |= otra.EstelaTinta;
            RoboVida |= otra.RoboVida;
            Ejecutar |= otra.Ejecutar;
        }
    }

    /// <summary>
    /// UNA INSCRIPCIÓN — definición global inmutable. Subclasearla ES
    /// registrarla (el escáner de reflexión de Load las encuentra solo —
    /// el patrón ICWRLoader/VaultUtils de CWR, sin una línea de registro).
    /// </summary>
    public abstract class InscripcionDefinicion
    {
        /// <summary>
        /// LA CLAVE ESTABLE de guardado/red = el nombre del tipo.
        /// RENOMBRAR LA CLASE ROMPE LOS GUARDADOS (la lección CWR:
        /// «改名即断档» — renombrar es perder el progreso).
        /// </summary>
        public virtual string Clave => GetType().Name;

        /// <summary>La ranura donde vive (una por ranura a la vez).</summary>
        public abstract RanuraInscripcion Ranura { get; }

        /// <summary>El orden dentro de su ranura (para el árbol y el registro).</summary>
        public virtual int Orden => 50;

        /// <summary>Su nombre localizado (Inscripciones.{Clave}.Nombre).</summary>
        public string Nombre => Language.GetTextValue(
            "Mods.AethonMod.Inscripciones." + Clave + ".Nombre");

        /// <summary>Su descripción localizada (Inscripciones.{Clave}.Descripcion).</summary>
        public string Descripcion => Language.GetTextValue(
            "Mods.AethonMod.Inscripciones." + Clave + ".Descripcion");

        /// <summary>La línea de PODER para el tooltip (Inscripciones.{Clave}.Poder).</summary>
        public string Poder => Language.GetTextValue(
            "Mods.AethonMod.Inscripciones." + Clave + ".Poder");

        /// <summary>La línea de CARGA para el tooltip (Inscripciones.{Clave}.Carga).</summary>
        public string Carga => Language.GetTextValue(
            "Mods.AethonMod.Inscripciones." + Clave + ".Carga");

        /// <summary>LA COMPOSICIÓN: multiplica/suma en el perfil — nunca asigna.</summary>
        public virtual void Perfil(ref PerfilInscripcion p) { }

        // ------------------------------------------------------------------
        //  EL REGISTRO — reflexión en el primer uso (inmutable después)
        // ------------------------------------------------------------------
        private static List<InscripcionDefinicion> _registro;
        private static Dictionary<string, InscripcionDefinicion> _porClave;

        /// <summary>TODAS las inscripciones registradas (ranura, luego orden).</summary>
        public static IReadOnlyList<InscripcionDefinicion> Registro
        {
            get
            {
                AsegurarRegistro();
                return _registro;
            }
        }

        /// <summary>Busca por clave estable (null si no existe — saneo de guardados).</summary>
        public static InscripcionDefinicion PorClave(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return null;
            AsegurarRegistro();
            InscripcionDefinicion def;
            return _porClave.TryGetValue(clave, out def) ? def : null;
        }

        private static void AsegurarRegistro()
        {
            if (_registro != null) return;
            try
            {
                _registro = typeof(InscripcionDefinicion).Assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(InscripcionDefinicion)))
                    .Select(t => (InscripcionDefinicion)Activator.CreateInstance(t))
                    .OrderBy(d => (int)d.Ranura).ThenBy(d => d.Orden).ThenBy(d => d.Clave)
                    .ToList();
                _porClave = _registro.ToDictionary(d => d.Clave);
            }
            catch
            {
                _registro = new List<InscripcionDefinicion>();
                _porClave = new Dictionary<string, InscripcionDefinicion>();
            }
        }

        /// <summary>
        /// RESUELVE el perfil de las tres ranuras de un ítem (la pluma):
        /// neutral si no es la pluma o no hay nada equipado.
        /// </summary>
        public static PerfilInscripcion Resolver(Item item)
        {
            PerfilInscripcion perfil = PerfilInscripcion.Neutral();
            if (item == null || item.IsAir) return perfil;
            InscripcionItem datos = item.GetGlobalItem<InscripcionItem>();
            if (datos == null || datos.Ranuras == null) return perfil;
            for (int r = 0; r < datos.Ranuras.Length && r < 3; r++)
            {
                InscripcionDefinicion def = PorClave(datos.Ranuras[r]);
                if (def == null) continue;
                PerfilInscripcion paso = PerfilInscripcion.Neutral();
                def.Perfil(ref paso);
                perfil.Acumular(paso);
            }
            return perfil;
        }
    }

    // ======================================================================
    //  LOS HECHOS — los desafíos que desbloquean inscripciones (el
    //  «blade karma» de Onikiri: la progresión SON los retos, no los puntos)
    // ======================================================================

    /// <summary>La definición de UN hecho (deed) — dato inmutable.</summary>
    public sealed class HechoDef
    {
        /// <summary>Clave estable (Hechos.{Clave}.Nombre/.Descripcion).</summary>
        public readonly string Clave;
        /// <summary>La inscripción que desbloquea al completarse.</summary>
        public readonly string ClaveInscripcion;
        /// <summary>El objetivo numérico (n de kills, golpes, etc.).</summary>
        public readonly int Objetivo;

        public HechoDef(string clave, string claveInscripcion, int objetivo)
        {
            Clave = clave;
            ClaveInscripcion = claveInscripcion;
            Objetivo = objetivo;
        }

        public string Nombre => Language.GetTextValue("Mods.AethonMod.Hechos." + Clave + ".Nombre");
        public string Descripcion => Language.GetTextValue("Mods.AethonMod.Hechos." + Clave + ".Descripcion");

        /// <summary>El progreso visible «n/N» (para nodos bloqueados).</summary>
        public string Progreso(int actual) => actual + "/" + Objetivo;
    }

    /// <summary>
    /// EL CATÁLOGO DE HECHOS (los 5 de la v6.52.0). El progreso vive en
    /// <see cref="InscripcionJugador"/> (por jugador, persistente).
    /// </summary>
    public static class HechosCatalogo
    {
        public static readonly HechoDef PrimerasPalabras = new("PrimerasPalabras", "VersoDoble", 20);
        public static readonly HechoDef ManoFirme = new("ManoFirme", "MargenEstrecho", 25);
        public static readonly HechoDef AlMargen = new("AlMargen", "PuntoFinal", 5);
        public static readonly HechoDef TintaSeca = new("TintaSeca", "PlumaAjena", 36000);
        public static readonly HechoDef RachaViva = new("RachaViva", "Interlineado", 8);

        /// <summary>TODOS los hechos, en orden fijo (los índices son estables).</summary>
        public static readonly HechoDef[] Todos =
        {
            PrimerasPalabras,
            ManoFirme,
            AlMargen,
            TintaSeca,
            RachaViva,
        };

        public static HechoDef PorIndice(int i) =>
            i >= 0 && i < Todos.Length ? Todos[i] : null;
    }

    // ======================================================================
    //  LAS PRUEBAS DE NIVEL — el crecimiento del ARMA viaja con el ÍTEM
    //  (la lección estructural de CWR: LegendData vive en el item, la
    //  biblioteca en el jugador). Cada jefe vencido PERSONALMENTE con la
    //  pluma en el inventario suma un nivel permanente al arma.
    // ======================================================================
    public static class PruebasPluma
    {
        /// <summary>Una prueba: clave estable + los netIDs que la cumplen.</summary>
        public sealed class Prueba
        {
            public readonly string Clave;
            public readonly int[] NetIDs;
            public Prueba(string clave, params int[] netIDs) { Clave = clave; NetIDs = netIDs; }
        }

        /// <summary>
        /// LA RUTA de las 10 pruebas (vanilla + el Escriba) — el orden es
        /// el de progresión; la clave ES estable (jamás reindexar: los
        /// guardados guardan claves, no posiciones).
        /// </summary>
        public static readonly Prueba[] Ruta =
        {
            new Prueba("v.eoc",        Terraria.ID.NPCID.EyeofCthulhu),
            new Prueba("v.gusano",     Terraria.ID.NPCID.EaterofWorldsHead,
                                     Terraria.ID.NPCID.EaterofWorldsBody,
                                     Terraria.ID.NPCID.EaterofWorldsTail),
            new Prueba("v.cerebro",    Terraria.ID.NPCID.BrainofCthulhu),
            new Prueba("v.esqueleto",  Terraria.ID.NPCID.SkeletronHead),
            new Prueba("v.muro",       Terraria.ID.NPCID.WallofFlesh),
            new Prueba("v.mecanico",   Terraria.ID.NPCID.Retinazer,
                                     Terraria.ID.NPCID.Spazmatism,
                                     Terraria.ID.NPCID.TheDestroyer,
                                     Terraria.ID.NPCID.SkeletronPrime),
            new Prueba("v.plantera",   Terraria.ID.NPCID.Plantera),
            new Prueba("v.golem",      Terraria.ID.NPCID.Golem),
            new Prueba("v.cultista",   Terraria.ID.NPCID.CultistBoss),
            new Prueba("v.lordlunar",  Terraria.ID.NPCID.MoonLordCore),
        };

        /// <summary>La prueba del propio Escriba (el nivel 10 — se resuelve por tipo de mod).</summary>
        public const string ClaveEscriba = "v.escriba";

        /// <summary>¿Este netID cumple la prueba p?</summary>
        public static bool Cumple(Prueba p, int netID)
        {
            for (int i = 0; i < p.NetIDs.Length; i++)
                if (p.NetIDs[i] == netID) return true;
            return false;
        }
    }
}
