using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Weapons.Escriba;

namespace AethonMod.Content.Skills
{
    /// <summary>
    /// INSCRIPCIONJUGADOR — v6.52.0 — LO QUE VIVE CON EL JUGADOR (el
    /// modelo asimétrico de CWR: la biblioteca de inscripciones
    /// DESBLOQUEADAS, el progreso de los HECHOS y TODA la conciencia del
    /// Escriba — mientras que las ranuras equipadas y el nivel viajan con
    /// el ÍTEM en <see cref="InscripcionItem"/>).
    ///
    /// LA CONCIENCIA (la receta R1 §1, casa-ificada):
    ///   · EL PRIMER ENCUENTRO: la primera vez que sostienes la pluma,
    ///     el Escriba HABLA — cuatro líneas espaciadas (la escena del
    ///     torii de Onikiri hecha chat: cero cinemática invasiva, todo
    ///     en la voz).
    ///   · LOS REGALOS: al vencer cada jefe con la pluma en el
    ///     inventario, el Escriba REACCIONA (una línea propia por jefe,
    ///     una sola vez) y desbloquea una inscripción de regalo — las
    ///     «Himayo gifts» de CWR, que son la vía de desbloqueo más
    ///     memorable.
    ///   · LOS SUSURROS: de vez en cuando (20%, cooldown 3 min) al matar
    ///     con la pluma, una línea de ambiente.
    /// </summary>
    public class InscripcionJugador : ModPlayer
    {
        // === LA BIBLIOTECA (inscripciones desbloqueadas — por jugador) ===
        public HashSet<string> Biblioteca = new HashSet<string>();

        // === EL PROGRESO DE LOS HECHOS (clave → contador actual) ===
        public Dictionary<string, int> Hechos = new Dictionary<string, int>();

        // === LA CONCIENCIA (persistente) ===
        public bool PrimerEncuentroHecho;
        public HashSet<string> RegalosHechos = new HashSet<string>();   // netID del jefe, como texto

        // === EL RASTREO EN SESIÓN (no persiste) ===
        private int _ticksSosteniendo;              // HECHO TintaSeca
        private int _golpesJefeNetID = -1;          // HECHO ManoFirme (el jefe actual)
        private int _golpesJefeCuenta;
        private uint _frameUltimaKill;              // HECHO RachaViva (la cadena)
        private int _cadenaLarga;
        private uint _frameUltimoSusurro;
        private uint _frameEncuentro;               // el primer encuentro (escena por marcas de frame)
        private bool _encuentroActivo;

        // ------------------------------------------------------------------
        //  LOS REGALOS: netID del jefe → la inscripción que desbloquea.
        //  (El mapa de la v6.52.0 — 10 regalos.)
        // ------------------------------------------------------------------
        public static readonly (int netID, string inscripcion)[] Regalos =
        {
            (NPCID.EyeofCthulhu,    "Subrayado"),
            (NPCID.EaterofWorldsHead, "LetraCapital"),
            (NPCID.BrainofCthulhu,  "LetraCapital"),
            (NPCID.SkeletronHead,   "TintaCorrosiva"),
            (NPCID.WallofFlesh,     "Firma"),
            (NPCID.Retinazer,       "VersoCiego"),
            (NPCID.Spazmatism,      "VersoCiego"),
            (NPCID.TheDestroyer,    "VersoCiego"),
            (NPCID.SkeletronPrime,  "VersoCiego"),
            (NPCID.Plantera,        "RimaInterna"),
            (NPCID.Golem,           "Caligrafia"),
            (NPCID.CultistBoss,     "Hemistiquio"),
            (NPCID.MoonLordCore,    "TintaMadre"),
        };

        public static InscripcionJugador De(Player p) => p?.GetModPlayer<InscripcionJugador>();

        /// <summary>¿El jugador lleva la pluma consigo (mano o inventario)?</summary>
        public static Item PlumaEnInventario(Player p)
        {
            if (p == null) return null;
            int tipo = ModContent.ItemType<PlumaConsciente>();
            if (p.HeldItem != null && p.HeldItem.type == tipo) return p.HeldItem;
            for (int i = 0; i < p.inventory.Length; i++)
                if (p.inventory[i] != null && !p.inventory[i].IsAir && p.inventory[i].type == tipo)
                    return p.inventory[i];
            return null;
        }

        // ==============================================================
        //  EL PULSO DE LA CONCIENCIA (cada tick, jugador local)
        // ==============================================================
        public override void PostUpdate()
        {
            Item pluma = PlumaEnInventario(Player);
            if (pluma == null)
            {
                _encuentroActivo = false;
                return;
            }

            // === EL PRIMER ENCUENTRO (la escena de las cuatro líneas) ===
            if (!PrimerEncuentroHecho && !_encuentroActivo &&
                Player.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                PrimerEncuentroHecho = true;
                _encuentroActivo = true;
                _frameEncuentro = Main.GameUpdateCount;
                // La semilla de la biblioteca: las tres de nacimiento.
                Desbloquear("TintaLigera", false);
                Desbloquear("VersoSencillo", false);
                Desbloquear("ColofonHumilde", false);
            }
            if (_encuentroActivo)
            {
                // Las cuatro líneas, una cada 150 ticks (2.5 s) — por MARCAS
                // DE FRAME (el contrato PantallaLib: deriva, sin timers).
                uint edad = Main.GameUpdateCount - _frameEncuentro;
                HablarSiMarca(edad, 0u, "Mods.AethonMod.Escriba.Encuentro1");
                HablarSiMarca(edad, 150u, "Mods.AethonMod.Escriba.Encuentro2");
                HablarSiMarca(edad, 300u, "Mods.AethonMod.Escriba.Encuentro3");
                HablarSiMarca(edad, 450u, "Mods.AethonMod.Escriba.Encuentro4");
                if (edad > 460u) _encuentroActivo = false;
            }

            // === EL HECHO TINTA SECA (sostener la pluma 10 minutos = 36000 t) ===
            if (Player.whoAmI == Main.myPlayer)
            {
                _ticksSosteniendo++;
                AvanzarHecho(HechosCatalogo.TintaSeca, _ticksSosteniendo, true);
            }
        }

        private void HablarSiMarca(uint edad, uint marca, string clave)
        {
            if (edad == marca)
                Main.NewText(Terraria.Localization.Language.GetTextValue(clave),
                    new Color(212, 190, 255));
        }

        // ==============================================================
        //  LOS GOLPES CON LA PLUMA (HECHO ManoFirme: 25 golpes al MISMO
        //  jefe sin soltar el arte)
        // ==============================================================
        public void RegistrarGolpePluma(NPC objetivo)
        {
            if (objetivo == null || !objetivo.boss && objetivo.netID != NPCID.SkeletronHead) return;
            if (_golpesJefeNetID != objetivo.netID)
            {
                _golpesJefeNetID = objetivo.netID;
                _golpesJefeCuenta = 0;
            }
            _golpesJefeCuenta++;
            AvanzarHecho(HechosCatalogo.ManoFirme, _golpesJefeCuenta, false);
        }

        // ==============================================================
        //  LAS MUERTES (hechos de kill + regalos + pruebas de nivel).
        //  LA VÍA DE LA CASA: NO existe ModPlayer.OnKillNPC — el anzuelo
        //  de kill es GlobalNPC.OnKill con npc.lastInteraction (el patrón
        //  GlobalNPCXP); InscripcionGlobalNPC (abajo) desvía aquí.
        // ==============================================================
        public void RegistrarCaza(NPC objetivo)
        {
            Item pluma = PlumaEnInventario(Player);
            if (pluma == null) return;

            // === EL HECHO PRIMERAS PALABRAS (20 muertes con la pluma) ===
            AvanzarHecho(HechosCatalogo.PrimerasPalabras, ContarHecho(HechosCatalogo.PrimerasPalabras) + 1, false);

            // === EL HECHO RACHA VIVA (8 muertes encadenadas ≤ 3 s) ===
            uint ahora = Main.GameUpdateCount;
            if (ahora - _frameUltimaKill <= 180u) _cadenaLarga++; else _cadenaLarga = 1;
            _frameUltimaKill = ahora;
            AvanzarHecho(HechosCatalogo.RachaViva, _cadenaLarga, false);

            // === EL HECHO AL MARGEN (5 jefes DISTINTOS) ===
            if (objetivo.boss)
            {
                string claveMargen = "margen." + objetivo.netID;
                if (!MargenVistos.Contains(claveMargen)) MargenVistos.Add(claveMargen);
                AvanzarHecho(HechosCatalogo.AlMargen, MargenVistos.Count, true);
            }

            // === LA PRUEBA DE NIVEL (el nivel viaja con el ÍTEM) ===
            InscripcionItem datos = InscripcionItem.De(pluma);
            if (datos != null)
            {
                string clavePrueba = null;
                foreach (var prueba in PruebasPluma.Ruta)
                    if (PruebasPluma.Cumple(prueba, objetivo.netID)) { clavePrueba = prueba.Clave; break; }
                if (objetivo.type == ModContent.NPCType<NPCs.Escriba.EscribaJefe>())
                    clavePrueba = PruebasPluma.ClaveEscriba;
                if (clavePrueba != null && !datos.Pruebas.Contains(clavePrueba))
                {
                    datos.Pruebas.Add(clavePrueba);
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Escriba.PruebaVencida", datos.Nivel),
                            new Color(255, 226, 160));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item124.WithPitchOffset(0.25f),
                            Player.Center);
                    }
                }
            }

            // === LOS REGALOS (la reacción del Escriba a cada jefe) ===
            for (int i = 0; i < Regalos.Length; i++)
            {
                if (Regalos[i].netID != objetivo.netID) continue;
                string marca = "g." + objetivo.netID;
                if (RegalosHechos.Contains(marca)) break;
                RegalosHechos.Add(marca);

                if (Player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Escriba.Regalo." + Regalos[i].inscripcion),
                        new Color(212, 190, 255));
                }
                Desbloquear(Regalos[i].inscripcion, true);
                break;
            }

            // === EL REGALO DEL ESCRIBA MISMO (vencerlo: El Colofón) ===
            if (objetivo.type == ModContent.NPCType<NPCs.Escriba.EscribaJefe>() &&
                !RegalosHechos.Contains("g.escriba"))
            {
                RegalosHechos.Add("g.escriba");
                if (Player.whoAmI == Main.myPlayer)
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Escriba.Regalo.ElColofon"),
                        new Color(212, 190, 255));
                Desbloquear("ElColofon", true);
            }

            // === LOS SUSURROS (20% con cooldown de 3 min — ambiente) ===
            if (Player.whoAmI == Main.myPlayer && !Main.dedServ &&
                Main.GameUpdateCount - _frameUltimoSusurro > 10800u &&
                Main.rand.NextFloat() < 0.20f)
            {
                _frameUltimoSusurro = Main.GameUpdateCount;
                int cual = Main.rand.Next(4);
                string clave = cual == 0 ? "Susurro1" : cual == 1 ? "Susurro2"
                                : cual == 2 ? "Susurro3" : "Susurro4";
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Escriba." + clave), new Color(150, 132, 190));
            }
        }

        // El conjunto de jefes distintos vistos para «Al Margen» (persiste).
        public HashSet<string> MargenVistos = new HashSet<string>();

        // ==============================================================
        //  LOS HECHOS — avance y desbloqueo
        // ==============================================================

        /// <summary>El contador actual de un hecho (0 si no existe).</summary>
        public int ContarHecho(HechoDef hecho) =>
            hecho != null && Hechos.TryGetValue(hecho.Clave, out int n) ? n : 0;

        /// <summary>
        /// AVANZA un hecho al valor dado (Math.Max — la regla CWR: el
        /// progreso JAMÁS retrocede) y lo COMPLETA si llega al objetivo:
        /// anuncia y desbloquea su inscripción.
        /// </summary>
        private void AvanzarHecho(HechoDef hecho, int valor, bool silenciosoAvance)
        {
            if (hecho == null) return;
            int actual = ContarHecho(hecho);
            if (actual >= hecho.Objetivo) return;                 // ya completo

            int nuevo = Math.Max(actual, valor);
            Hechos[hecho.Clave] = nuevo;
            if (nuevo >= hecho.Objetivo)
            {
                Hechos[hecho.Clave] = hecho.Objetivo;
                Desbloquear(hecho.ClaveInscripcion, true);
                if (Player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Escriba.HechoCumplido",
                        InscripcionDefinicion.PorClave(hecho.ClaveInscripcion)?.Nombre ?? hecho.ClaveInscripcion),
                        new Color(180, 255, 200));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item124, Player.Center);
                }
            }
        }

        /// <summary>
        /// DESBLOQUEA una inscripción en la biblioteca (idempotente). El
        /// anuncio solo cuando es NUEVA (la primera vez se CELEBRA).
        /// </summary>
        public bool Desbloquear(string clave, bool anunciar)
        {
            if (string.IsNullOrEmpty(clave)) return false;
            InscripcionDefinicion def = InscripcionDefinicion.PorClave(clave);
            if (def == null) return false;
            if (Biblioteca.Contains(clave)) return false;
            Biblioteca.Add(clave);
            if (anunciar && Player.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Escriba.InscripcionDesbloqueada", def.Nombre),
                    new Color(212, 190, 255));
            }
            return true;
        }

        // ==============================================================
        //  LA PERSISTENCIA (el patrón de la casa: try/catch SIEMPRE —
        //  PlayerLoader.SaveData no envuelve)
        // ==============================================================
        public override void SaveData(TagCompound tag)
        {
            try
            {
                if (Biblioteca.Count > 0) tag["bib"] = Biblioteca.ToList();
                if (Hechos.Count > 0)
                {
                    tag["hK"] = Hechos.Keys.ToList();
                    tag["hV"] = Hechos.Values.ToList();
                }
                if (PrimerEncuentroHecho) tag["enc"] = true;
                if (RegalosHechos.Count > 0) tag["reg"] = RegalosHechos.ToList();
                if (MargenVistos.Count > 0) tag["mar"] = MargenVistos.ToList();
            }
            catch { }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                Biblioteca = new HashSet<string>();
                Hechos = new Dictionary<string, int>();
                RegalosHechos = new HashSet<string>();
                MargenVistos = new HashSet<string>();
                PrimerEncuentroHecho = tag.GetBool("enc");

                if (tag.TryGet<List<string>>("bib", out var bib) && bib != null)
                    foreach (string k in bib)
                        if (InscripcionDefinicion.PorClave(k) != null) Biblioteca.Add(k);

                if (tag.TryGet<List<string>>("hK", out var hK) &&
                    tag.TryGet<List<int>>("hV", out var hV) && hK != null && hV != null)
                    for (int i = 0; i < hK.Count && i < hV.Count; i++)
                        Hechos[hK[i]] = hV[i];

                if (tag.TryGet<List<string>>("reg", out var reg) && reg != null)
                    foreach (string k in reg) RegalosHechos.Add(k);

                if (tag.TryGet<List<string>>("mar", out var mar) && mar != null)
                    foreach (string k in mar) MargenVistos.Add(k);
            }
            catch
            {
                Biblioteca = new HashSet<string>();
                Hechos = new Dictionary<string, int>();
                RegalosHechos = new HashSet<string>();
                MargenVistos = new HashSet<string>();
            }
        }
    }

    /// <summary>
    /// EL ANZUELO DE LAS MUERTES: GlobalNPC.OnKill + npc.lastInteraction
    /// (el patrón GlobalNPCXP de la casa — la kill PERSONAL del jugador
    /// que portaba la pluma alimenta hechos, regalos y pruebas).
    /// </summary>
    public class InscripcionGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            try
            {
                if (npc?.lastInteraction is not int quien || quien < 0 || quien >= Main.player.Length)
                    return;
                Player cazador = Main.player[quien];
                if (cazador == null || !cazador.active) return;
                InscripcionJugador.De(cazador)?.RegistrarCaza(npc);
            }
            catch { }
        }
    }
}
