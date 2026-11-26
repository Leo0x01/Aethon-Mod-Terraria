#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_pilar.py — la aspiradora unificada en PrismaInteriorUI.

v6.177 (la letra): «la aspiradora por algun motivo solo parecer funcionar
en el mundo interior» + «esta animacion se debe hacer solo la segunda vez
que el Devoramundos sea invocado». El pilar YA NO arranca el cine: el
vigía universal (FaucesGlobalNPC.OnSpawn) cuenta TODAS las invocaciones
del Devorador (pilar · oleadas · comida de gusano) y arranca el cine SOLO
en la segunda. Y el nivel del sello pierde el tope de 10 (la letra del
nivel infinito)."""
import sys

R = 'AethonMod/Content/UI/PrismaInteriorUI.cs'
s = open(R, encoding='utf-8').read()
orig = s

# ── 1. EL NIVEL SIN TOPE + la aspiradora se va del pilar ──────────
viejo = """                var sp = yo.GetModPlayer<Players.ShardPlayer>();
                int kills = sp != null ? sp.KillsInteriorDe(tipoJefe) : 0;
                int nivelIA = Math.Min(10, 1 + kills);"""
nuevo = """                var sp = yo.GetModPlayer<Players.ShardPlayer>();
                int kills = sp != null ? sp.KillsInteriorDe(tipoJefe) : 0;
                // v6.177 — EL NIVEL SIN TOPE (la letra: «ademas en el mundo
                // interior los jefes no necesitan estar limitados a nivel
                // 10, que este nivel sea infitino y cuanto mas nivels mas
                // complicado, mas inteligente y tiene nuevos ataques,
                // combos y nuevos movimientos, asi como una IA mas
                // avanzada»): 1 + kills, SIN tope — la escalera de
                // desbloqueos del motor crece por siempre y la barra
                // muestra la estatura REAL.
                int nivelIA = 1 + kills;"""
assert viejo in s, 'nivelIA no encontrado'
s = s.replace(viejo, nuevo, 1)

# ── 2. EL BLOQUE DE LA ASPIRADORA MUERE (el vigía universal manda) ─
import re
pat = re.compile(
    r"\n[ \t]*// ={10,}\n[ \t]*//  v6\.175 — LA ASPIRADORA DEL DEVORADOR.*?"
    r"int vecesInvocado = sp != null \? sp\.MarcarInvocacionInterior\(tipoJefe\) : 1;\n",
    re.S)
m = pat.search(s)
assert m, 'el bloque de la aspiradora no se encontró'
reemplazo = """
                // v6.177 — LA ASPIRADORA YA NO NACE AQUÍ: el vigía
                // universal de FaucesGlobalNPC.OnSpawn cuenta TODAS las
                // invocaciones del Devorador (pilar · oleadas · comida de
                // gusano del mundo real) y arranca el cine espacial SOLO en
                // la SEGUNDA invocación — el pilar solo viste al jefe de
                // arena. (La letra: la aspiradora funciona en TODAS partes,
                // no solo en el interior.)
"""
s = s[:m.start()] + reemplazo + s[m.end():]

# ── 3. EL SPAWN DEL CINE DENTRO DEL SELLO MUERE ────────────────────
pat2 = re.compile(
    r"\n[ \t]*// LA SEGUNDA INVOCACIÓN DEL GUSANO: LA ASPIRADORA\n"
    r"[ \t]*if \(esGusano && vecesInvocado >= 2\)\n"
    r"[ \t]*\{.*?\n[ \t]*\}\n", re.S)
m2 = pat2.search(s)
assert m2, 'el spawn del cine no se encontró'
s = s[:m2.start()] + "\n" + s[m2.end():]

# ── 4. El clamp del nivel de la OLEADA también pierde el tope ────
# (ClicEnLanzar y Alternar usan Math.Min(10, ...) — la oleada crece igual)
# El selector [-] N [+] del pilar muestra maxNivel — el tope vive en
# PilarDelLibro; este archivo solo pasa el nivel.
s = s.replace("""                nivel = Math.Max(1, Math.Min(10, nivel));
                LanzarLaOleada(yo, nivel);""",
"""                nivel = Math.Max(1, nivel);   // v6.177: sin tope — el nivel crece por siempre
                LanzarLaOleada(yo, nivel);""", 1)
s = s.replace("""                Systems.GrimorioFuriaSistema.Provocar(yo,
                    Math.Max(1, Math.Min(10, nivel)), natural: false);""",
"""                Systems.GrimorioFuriaSistema.Provocar(yo,
                    Math.Max(1, nivel), natural: false);   // v6.177: sin tope""", 1)

open(R, 'w', encoding='utf-8', newline='\n').write(s)
print('OK PrismaInteriorUI:', len(orig), '→', len(s), 'bytes')
