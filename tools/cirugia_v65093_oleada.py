#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.93 — cirugía OleadaNPC.cs (bloque PreAI de zonas + docs)."""
import io, sys

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Globals/OleadaNPC.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()

def reemplazo(viejo, nuevo, etiqueta):
    global t
    if viejo not in t:
        print("FALLO: no hallé ->", etiqueta)
        sys.exit(1)
    if t.count(viejo) != 1:
        print("FALLO: apariciones %d de -> %s" % (t.count(viejo), etiqueta))
        sys.exit(1)
    t = t.replace(viejo, nuevo)
    print("OK ->", etiqueta)

# ---------------------------------------------------------------- 1. PreAI: la reestructura completa
viejo = u"""                if (_prestamoAbierto != null) DevolverAmbiente();

                if (EsDeOleada && EsJefeDeOleada &&
                    npc.target >= 0 && npc.target < Main.maxPlayers)
                {
                    Player presa = Main.player[npc.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        // LOS GUARDIANES DE VERDAD (los tipos del fest\u00edn \u2014
                        // ni las manos de Skeletron ni las piezas en
                        // cascada, aunque hereden el sello de jefe)
                        bool esGuardian =
                            npc.type == NPCID.KingSlime ||
                            npc.type == NPCID.EyeofCthulhu ||
                            npc.type == NPCID.QueenBee ||
                            npc.type == NPCID.BrainofCthulhu ||
                            npc.type == NPCID.EaterofWorldsHead ||
                            npc.type == NPCID.SkeletronHead ||
                            npc.type == NPCID.Deerclops;
                        if (esGuardian)
                        {
                            bool algoPrestado = false;

                            // === LAS ZONAS DEL GUARDI\u00c1N ===
                            bool prestaCrimson = npc.type == NPCID.BrainofCthulhu && !presa.ZoneCrimson;
                            bool prestaCorrupt = npc.type == NPCID.EaterofWorldsHead && !presa.ZoneCorrupt;
                            bool prestaJungla = npc.type == NPCID.QueenBee && !presa.ZoneJungle;
                            bool prestaNieve = npc.type == NPCID.Deerclops && !presa.ZoneSnow;
                            if (prestaCrimson || prestaCorrupt || prestaJungla || prestaNieve)
                            {
                                _zonaCrimsonOriginal = presa.ZoneCrimson;
                                _zonaCorruptOriginal = presa.ZoneCorrupt;
                                _junglaOriginal = presa.ZoneJungle;
                                _nieveOriginal = presa.ZoneSnow;
                                if (prestaCrimson) presa.ZoneCrimson = true;
                                if (prestaCorrupt) presa.ZoneCorrupt = true;
                                if (prestaJungla) presa.ZoneJungle = true;
                                if (prestaNieve) presa.ZoneSnow = true;
                                _presaDeZonas = presa;
                                _zonaPrestada = true;
                                algoPrestado = true;
                            }
"""

nuevo = u"""                if (_prestamoAbierto != null) DevolverAmbiente();

                // v6.50.93 \u2014 EL REGISTRO VIVO: el sello de ESTE slot
                // refresca su hora (un NPC muerto ya no corre PreAI) y,
                // cada ~8,5 s, los sellos sin frescura expiran (slots
                // reciclados por NewNPC \u2014 el barrido los olvida antes de
                // que alguien ajeno herede un sello por accidente).
                if (EsDeOleada && _sellados.TryGetValue(npc.whoAmI, out SelloVivo vivo))
                    vivo.Tick = Main.GameUpdateCount;
                if ((Main.GameUpdateCount & 511u) == 0u && _sellados.Count > 0)
                {
                    List<int> vencidos = null;
                    foreach (var par in _sellados)
                    {
                        if (Main.GameUpdateCount - par.Value.Tick > TTL_SELLO)
                        {
                            if (vencidos == null) vencidos = new List<int>();
                            vencidos.Add(par.Key);
                        }
                    }
                    if (vencidos != null)
                        for (int i = 0; i < vencidos.Count; i++)
                            _sellados.Remove(vencidos[i]);
                }

                if (EsDeOleada)
                {
                    // LOS GUARDIANES DE VERDAD (los tipos del fest\u00edn \u2014
                    // ni las manos de Skeletron ni las piezas en
                    // cascada, aunque hereden el sello de jefe)
                    bool esGuardian =
                        npc.type == NPCID.KingSlime ||
                        npc.type == NPCID.EyeofCthulhu ||
                        npc.type == NPCID.QueenBee ||
                        npc.type == NPCID.BrainofCthulhu ||
                        npc.type == NPCID.EaterofWorldsHead ||
                        npc.type == NPCID.SkeletronHead ||
                        npc.type == NPCID.Deerclops;
                    bool algoPrestado = false;

                    // === v6.50.93 \u2014 LAS ZONAS DE LA OLEADA, A TODA LA MESA
                    //     (la letra del usuario: \u00ablos jefes y monstruos
                    //     invocados en las oleadas no se vean afectados por
                    //     los parametros de biomas o climas que sus
                    //     versiones originales, ten en cuenta que las
                    //     versiones de oleada son entidades separadas de
                    //     las originales por lo tanto no se ven afectadas\u00bb).
                    //     EL DEVORADOR DE OLEADA \u2014 y los Devoradores que
                    //     escupe en el subsuelo \u2014 NO leen la Corrupci\u00f3n
                    //     del mundo: su IA vanilla pregunta \u00ab\u00bfALGUIEN vive
                    //     en la Corrupci\u00f3n?\u00bb y, sin respuesta, lo entierra
                    //     y APAGA la cadena entera (active=false directo,
                    //     que ning\u00fan hook detiene). Se presta la zona a
                    //     TODOS los vivos durante su AI, SIN target (su
                    //     chequeo nace con target=255 en el primer tick y
                    //     con el target muerto cuando alguien cae). El
                    //     Cerebro su Carmes\u00ed, la Reina su Jungla, Deerclops
                    //     su Nieve: la oleada ES su propio ambiente. ===
                    bool prestaCorrupt = npc.type == NPCID.EaterofWorldsHead ||
                                         npc.type == NPCID.DevourerHead;
                    bool prestaCrimson = esGuardian && EsJefeDeOleada && npc.type == NPCID.BrainofCthulhu;
                    bool prestaJungla = esGuardian && EsJefeDeOleada && npc.type == NPCID.QueenBee;
                    bool prestaNieve = esGuardian && EsJefeDeOleada && npc.type == NPCID.Deerclops;
                    if (prestaCorrupt || prestaCrimson || prestaJungla || prestaNieve)
                    {
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player pl = Main.player[i];
                            if (pl == null || !pl.active || pl.dead) continue;
                            _presasDeZonas.Add(pl);
                            _zoCrimson.Add(pl.ZoneCrimson);
                            _zoCorrupt.Add(pl.ZoneCorrupt);
                            _zoJungla.Add(pl.ZoneJungle);
                            _zoNieve.Add(pl.ZoneSnow);
                            if (prestaCrimson) pl.ZoneCrimson = true;
                            if (prestaCorrupt) pl.ZoneCorrupt = true;
                            if (prestaJungla) pl.ZoneJungle = true;
                            if (prestaNieve) pl.ZoneSnow = true;
                        }
                        _zonaPrestada = _presasDeZonas.Count > 0;
                        if (_zonaPrestada) algoPrestado = true;
                    }

                    if (esGuardian && EsJefeDeOleada)
                    {
                        bool algoDelGuardian = false;
"""
reemplazo(viejo, nuevo, "1. PreAI: registro vivo + zonas a toda la mesa")

# ---------------------------------------------------------------- 2. el resto del bloque guardián: re-indentar y cerrar
viejo2 = u"""                            // === LA NOCHE PERPETUA (solo los que leen el
                            // d\u00eda: el Ojo que huye y el guardi\u00e1n que gira)
                            if ((npc.type == NPCID.EyeofCthulhu || npc.type == NPCID.SkeletronHead) &&
                                Main.dayTime)
                            {
                                _diaOriginal = Main.dayTime;
                                Main.dayTime = false;
                                _prestaDia = true;
                                algoPrestado = true;
                            }

                            // === LA COLMENA FLOTANTE (la Reina jam\u00e1s
                            // \u00absobre la superficie\u00bb) ===
                            if (npc.type == NPCID.QueenBee && Main.worldSurface > 0.0)
                            {
                                _superficieOriginal = Main.worldSurface;
                                Main.worldSurface = 0.0; // su Y/16 jam\u00e1s ser\u00e1 \u00absobre\u00bb 0
                                _prestaSuperficie = true;
                                algoPrestado = true;
                            }

                            // === LA IA DEL MODO MAESTRO (los overrides) ===
                            if (!_prestaModo)
                            {
                                PrepararReflexion();
                                if (_campoExperto != null && _campoMaestro != null)
                                {
                                    _expertoOriginal = (bool?)_campoExperto.GetValue(null);
                                    _maestroOriginal = (bool?)_campoMaestro.GetValue(null);
                                    _campoExperto.SetValue(null, (bool?)true);
                                    _campoMaestro.SetValue(null, (bool?)true);
                                    _prestaModo = true;
                                    algoPrestado = true;
                                }
                            }

                            if (algoPrestado)
                            {
                                _prestamoAbierto = this;
                            }
                        }
                    }
                }
"""

nuevo2 = u"""                        // === LA NOCHE PERPETUA (solo los que leen el
                        // d\u00eda: el Ojo que huye y el guardi\u00e1n que gira)
                        if ((npc.type == NPCID.EyeofCthulhu || npc.type == NPCID.SkeletronHead) &&
                            Main.dayTime)
                        {
                            _diaOriginal = Main.dayTime;
                            Main.dayTime = false;
                            _prestaDia = true;
                            algoDelGuardian = true;
                        }

                        // === LA COLMENA FLOTANTE (la Reina jam\u00e1s
                        // \u00absobre la superficie\u00bb) ===
                        if (npc.type == NPCID.QueenBee && Main.worldSurface > 0.0)
                        {
                            _superficieOriginal = Main.worldSurface;
                            Main.worldSurface = 0.0; // su Y/16 jam\u00e1s ser\u00e1 \u00absobre\u00bb 0
                            _prestaSuperficie = true;
                            algoDelGuardian = true;
                        }

                        // === LA IA DEL MODO MAESTRO (los overrides) ===
                        if (!_prestaModo)
                        {
                            PrepararReflexion();
                            if (_campoExperto != null && _campoMaestro != null)
                            {
                                _expertoOriginal = (bool?)_campoExperto.GetValue(null);
                                _maestroOriginal = (bool?)_campoMaestro.GetValue(null);
                                _campoExperto.SetValue(null, (bool?)true);
                                _campoMaestro.SetValue(null, (bool?)true);
                                _prestaModo = true;
                                algoDelGuardian = true;
                            }
                        }

                        if (algoDelGuardian)
                            algoPrestado = true;
                    }

                    if (algoPrestado)
                    {
                        _prestamoAbierto = this;
                    }
                }
"""
reemplazo(viejo2, nuevo2, "2. PreAI: bloque guardián re-indentado + cierre")

with io.open(RUTA, "w", encoding="utf-8", newline="") as f:
    f.write(t)
print("OleadaNPC.cs: cirugía PreAI completa")
