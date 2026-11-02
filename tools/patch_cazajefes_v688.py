#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — CazaJefesPlayer: añade VecesEmperatriz + LegadoEmperatriz."""
import io

RUTA = "AethonMod/Content/Players/CazaJefesPlayer.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()

# --- 1) VecesEmperatriz + LegadoEmperatriz tras LegadoMoonLord ---
viejo = "        public bool LegadoMoonLord = false;\n"
nuevo = viejo + """
        /// <summary>v6.88 — CUÁNTAS VECES tiró ESTE cazador a la Emperatriz
        /// de la Luz (la letra de la cima: «5 emperatris de la luz»).
        /// El legacy: el que ya la tenía muerta al cargar la v6.88 cobra
        /// 1 de las 5, una sola vez.</summary>
        public int VecesEmperatriz = 0;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 2) persistencia SaveData ---
viejo = '                tag["mlVeces"] = VecesMoonLord;\n'
nuevo = ('                tag["mlVeces"] = VecesMoonLord;\n'
         '                tag["eolVeces"] = VecesEmperatriz;\n')
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 3) persistencia LoadData ---
viejo = '                LegadoMoonLord = tag.GetBool("mlLegado");\n'
nuevo = ('                LegadoMoonLord = tag.GetBool("mlLegado");\n'
         '                LegadoEmperatriz = tag.GetBool("eolLegado");\n')
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

viejo = 'VecesAethon = Math.Max(0, Math.Min(999, tag.GetInt("aethonVeces")));\n'
nuevo = viejo + '                VecesEmperatriz = Math.Max(0, Math.Min(999, tag.GetInt("eolVeces")));\n'
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 4) el legacy de la Emperatriz en OnEnterWorld ---
viejo = """                if (LegadoMoonLord || VecesMoonLord > 0) { LegadoMoonLord = true; return; }
                if (NPC.downedMoonlord)
                {
                    VecesMoonLord = 1;
                    LegadoMoonLord = true;
                }
"""
nuevo = """                if (LegadoMoonLord || VecesMoonLord > 0) { LegadoMoonLord = true; }
                else if (NPC.downedMoonlord)
                {
                    VecesMoonLord = 1;
                    LegadoMoonLord = true;
                }
                // v6.88 — EL LEGADO DE LA EMPERATRIZ (la cima compuesta
                //     exige 5: el que ya la mató ANTES cobra 1 de las 5).
                if (!LegadoEmperatriz && VecesEmperatriz == 0 && NPC.downedEmpressOfLight)
                {
                    VecesEmperatriz = 1;
                    LegadoEmperatriz = true;
                }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print("OK — CazaJefesPlayer: VecesEmperatriz + LegadoEmperatriz + legacy EoL")
