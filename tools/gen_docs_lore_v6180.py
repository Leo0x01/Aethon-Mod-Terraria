#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_docs_lore_v6180.py — v6.180 — LOS DOS DOCUMENTOS DEL LORE.

La letra: «ademas crear un documento con todo el lore antes de organizar
y despues de organizarlo».

Genera (desde la fuente viva — el hjson es-ES):
  · LORE-ANTES.md   — el orden VIEJO: el Codex (Prólogo + I..XI +
    Epílogo) y, debajo, EL ARCHIVO VIEJO DE LA CASA (los 5 capítulos del
    Compendio original), con SUS reglas de desbloqueo de siempre.
  · LORE-DESPUES.md — el orden NUEVO de la letra (los 19 títulos
    entrelazados), con las reglas nuevas: TODO sellado por el Despertar
    Y las palabras del Cronista (menos EL COMPENDIO DEL ETERNO) + las
    NARRACIONES del botón Lore.
"""
import re
from pathlib import Path

RAIZ = Path("/home/z/my-project/mod")
HJSON = RAIZ / "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"

def cargar():
    """Lee TODAS las claves del hjson en un dict (la fuente viva)."""
    t = HJSON.read_text(encoding="utf-8")
    claves = {}
    # claves de una línea (raíz o anidadas)
    for m in re.finditer(r"\n\t?([A-Za-z0-9_]+):\s*(.+?)(?=\n)", t):
        k, v = m.group(1), m.group(2).strip()
        if not v.startswith("'''"):
            claves[k] = v
    return claves

K = cargar()

def sec(titulo, *claves, umbral=""):
    """Una sección del documento: título + párrafos."""
    out = [f"## {titulo}\n"]
    if umbral:
        out.append(f"*{umbral}*\n")
    for c in claves:
        if c in K:
            out.append(K[c] + "\n")
    return "\n".join(out)

# ══════════════════════════════════════════════════════════════════
#  LORE-ANTES — el orden de siempre (la v6.179)
# ══════════════════════════════════════════════════════════════════
antes = f"""# EL COMPENDIO DEL ETERNO — el lore ANTES de la reorganización (v6.179)

> Documento generado el día de la v6.180 — la foto del archivo ANTES de
> entrelazarlo. La página derecha del libro de información tenía EL
> CÓDEX (el prólogo, los once capítulos y el epílogo) y, debajo, EL
> ARCHIVO VIEJO DE LA CASA (los cinco capítulos del Compendio
> original). Las reglas de siempre: el Codex se abría SOLO con el
> Despertar (2/12/22/32/42/52/62/72/82/92/102/118) y las cinco
> historias del archivo pedían ADEMÁS la charla del Cronista
> (4/24/44/64/84). La intro (SecLore) siempre libre.

---

## EL COMPENDIO DEL ETERNO (la intro — SIEMPRE abierta)

{K.get('LoreIntro', '')}

*Epígrafe: {K.get('LoreEpigrafe0', '')}*

---

{sec('EL CÓDEX — Prólogo: ' + K.get('CodexPrologoTit', ''),
     'CodexPrologoP0', 'CodexPrologoP1', 'CodexPrologoP2', 'CodexPrologoP3',
     umbral='Se abría al Despertar 0 (siempre)')}
{sec('EL CÓDEX — Capítulo I: ' + K.get('CodexITit', ''),
     'CodexIP0', 'CodexIP1', 'CodexIP2', 'CodexIP3',
     umbral='Se abría al Despertar 2')}
{sec('EL CÓDEX — Capítulo II: ' + K.get('CodexIITit', ''),
     'CodexIIP0', 'CodexIIP1', 'CodexIIP2', 'CodexIIP3',
     umbral='Se abría al Despertar 12')}
{sec('EL CÓDEX — Capítulo III: ' + K.get('CodexIIITit', ''),
     'CodexIIIP0', 'CodexIIIP1', 'CodexIIIP2', 'CodexIIIP3',
     umbral='Se abría al Despertar 22')}
{sec('EL CÓDEX — Capítulo IV: ' + K.get('CodexIVTit', ''),
     'CodexIVP0', 'CodexIVP1', 'CodexIVP2', 'CodexIVP3', 'CodexIVP4',
     umbral='Se abría al Despertar 32')}
{sec('EL CÓDEX — Capítulo V: ' + K.get('CodexVTit', ''),
     'CodexVP0', 'CodexVP1', 'CodexVP2', 'CodexVP3',
     umbral='Se abría al Despertar 42')}
{sec('EL CÓDEX — Capítulo VI: ' + K.get('CodexVITit', ''),
     'CodexVIP0', 'CodexVIP1', 'CodexVIP2', 'CodexVIP3',
     umbral='Se abría al Despertar 52')}
{sec('EL CÓDEX — Capítulo VII: ' + K.get('CodexVIITit', ''),
     'CodexVIIP0', 'CodexVIIP1', 'CodexVIIP2', 'CodexVIIP3', 'CodexVIIP4',
     'CodexVIIP5', 'CodexVIIP6', 'CodexVIIP7', 'CodexVIIP8', 'CodexVIIP9',
     'CodexVIIP10', 'CodexVIIP11', 'CodexVIIP12',
     umbral='Se abría al Despertar 62')}
{sec('EL CÓDEX — Capítulo VIII: ' + K.get('CodexVIIITit', ''),
     'CodexVIIIP0', 'CodexVIIIP1', 'CodexVIIIP2', 'CodexVIIIP3',
     umbral='Se abría al Despertar 72')}
{sec('EL CÓDEX — Capítulo IX: ' + K.get('CodexIXTit', ''),
     'CodexIXP0', 'CodexIXP1', 'CodexIXP2', 'CodexIXP3',
     umbral='Se abría al Despertar 82')}
{sec('EL CÓDEX — Capítulo X: ' + K.get('CodexXTit', ''),
     'CodexXP0', 'CodexXP1', 'CodexXP2', 'CodexXP3', 'CodexXP4', 'CodexXP5',
     umbral='Se abría al Despertar 92')}
{sec('EL CÓDEX — Capítulo XI: ' + K.get('CodexXITit', ''),
     'CodexXIP0', 'CodexXIP1', 'CodexXIP2', 'CodexXIP3',
     umbral='Se abría al Despertar 102')}
{sec('EL CÓDEX — Epílogo: ' + K.get('CodexEpilogoTit', ''),
     'CodexEpilogoP0', 'CodexEpilogoP1', 'CodexEpilogoP2', 'CodexEpilogoP3',
     umbral='Se abría al Despertar 118')}

---

# EL ARCHIVO VIEJO DE LA CASA — el Compendio original

{sec('1. ' + K.get('LoreSecI', ''), 'LoreI_Loquedevora', 'LoreI_LaCuarentenadeTer', 'LoreI_ElSellado',
     umbral='Pedía Despertar 4 Y la charla del Cronista (la puerta 4/24/44/64/84)')}
{sec('2. ' + K.get('LoreSecII', ''), 'LoreII_ElNivel', 'LoreII_ElDespertar', 'LoreII_Doshambresdistinta',
     umbral='Pedía Despertar 24 Y la charla del Cronista')}
{sec('3. ' + K.get('LoreSecIII', ''), 'LoreIII_LaLlegada', 'LoreIII_ElGrimoriodelEter', 'LoreIII_ElAltar',
     umbral='Pedía Despertar 44 Y la charla del Cronista')}
{sec('4. ' + K.get('LoreSecIV', ''), 'LoreIV_Laimposibilidaddel', 'LoreIV_Laentidadinmortal', 'LoreIV_Elaburrimientoyla',
     umbral='Pedía Despertar 64 Y la charla del Cronista')}
{sec('5. ' + K.get('LoreSecV', ''), 'LoreV_LaIra', 'LoreV_ElPilar', 'LoreV_ElSellado',
     'LoreV_Loscompaneros', 'LoreV_Lassombras', 'LoreV_Larespuestafinal',
     umbral='Pedía Despertar 84 Y la charla del Cronista')}
"""

# ══════════════════════════════════════════════════════════════════
#  LORE-DESPUES — el orden NUEVO de la letra (los 19 entrelazados)
# ══════════════════════════════════════════════════════════════════
despues = f"""# EL COMPENDIO DEL ETERNO — el lore DESPUÉS de la reorganización (v6.180)

> La letra: «sobre el libro de informacion, todo el lore debe estar
> oculto detras del Despertar y El Cronista […] esto es lo unico que no
> estara bloqueado por el Despertar ni por el cronista». El archivo
> ENTRELAZADO: el Codex y el archivo viejo tejidos en UNA sola obra —
> cada tres capítulos del Codex vive una historia del archivo viejo (las
> posiciones 4 · 7 · 10 · 13 · 16). TODO sellado por la puerta de
> Despertar Y las palabras del Cronista — menos EL COMPENDIO DEL ETERNO,
> la intro, la única libre. El Cronista lo escribió TODO (lo dice él
> mismo en la primera charla) y lo cuenta por pedacitos con SU botón
> LORE en el cuadro de diálogo.

---

## [0] {K.get('SecLore', 'EL COMPENDIO DEL ETERNO')} — LA ÚNICA LIBRE

{K.get('LoreIntro', '')}

*Epígrafe: {K.get('LoreEpigrafe0', '')}*

**Desbloqueo: SIEMPRE — ni Despertar ni Cronista.**

---

{sec('[1] ' + K.get('CodexPrologoTit', ''), 'CodexPrologoP0', 'CodexPrologoP1', 'CodexPrologoP2', 'CodexPrologoP3',
     umbral='Despertar 4 + el botón LORE del Cronista')}
{sec('[2] ' + K.get('CodexITit', ''), 'CodexIP0', 'CodexIP1', 'CodexIP2', 'CodexIP3',
     umbral='Despertar 8 + el botón LORE del Cronista')}
{sec('[3] ' + K.get('LoreSecI', ''), 'LoreI_Loquedevora', 'LoreI_LaCuarentenadeTer', 'LoreI_ElSellado',
     umbral='Despertar 12 + el botón LORE del Cronista · Epígrafe: ' + K.get('LoreEpigrafe1', ''))}
{sec('[4] ' + K.get('CodexIITit', ''), 'CodexIIP0', 'CodexIIP1', 'CodexIIP2', 'CodexIIP3',
     umbral='Despertar 16 + el botón LORE del Cronista')}
{sec('[5] ' + K.get('CodexIIITit', ''), 'CodexIIIP0', 'CodexIIIP1', 'CodexIIIP2', 'CodexIIIP3',
     umbral='Despertar 20 + el botón LORE del Cronista')}
{sec('[6] ' + K.get('LoreSecII', ''), 'LoreII_ElNivel', 'LoreII_ElDespertar', 'LoreII_Doshambresdistinta',
     umbral='Despertar 26 + el botón LORE del Cronista · Epígrafe: ' + K.get('LoreEpigrafe2', ''))}
{sec('[7] ' + K.get('CodexIVTit', ''), 'CodexIVP0', 'CodexIVP1', 'CodexIVP2', 'CodexIVP3', 'CodexIVP4',
     umbral='Despertar 32 + el botón LORE del Cronista')}
{sec('[8] ' + K.get('CodexVTit', ''), 'CodexVP0', 'CodexVP1', 'CodexVP2', 'CodexVP3',
     umbral='Despertar 38 + el botón LORE del Cronista')}
{sec('[9] ' + K.get('LoreSecIII', ''), 'LoreIII_LaLlegada', 'LoreIII_ElGrimoriodelEter', 'LoreIII_ElAltar',
     umbral='Despertar 44 + el botón LORE del Cronista · Epígrafe: ' + K.get('LoreEpigrafe3', ''))}
{sec('[10] ' + K.get('CodexVITit', ''), 'CodexVIP0', 'CodexVIP1', 'CodexVIP2', 'CodexVIP3',
     umbral='Despertar 52 + el botón LORE del Cronista')}
{sec('[11] ' + K.get('CodexVIITit', ''), 'CodexVIIP0', 'CodexVIIP1', 'CodexVIIP2', 'CodexVIIP3', 'CodexVIIP4',
     'CodexVIIP5', 'CodexVIIP6', 'CodexVIIP7', 'CodexVIIP8', 'CodexVIIP9',
     'CodexVIIP10', 'CodexVIIP11', 'CodexVIIP12',
     umbral='Despertar 60 + el botón LORE del Cronista')}
{sec('[12] ' + K.get('LoreSecIV', ''), 'LoreIV_Laimposibilidaddel', 'LoreIV_Laentidadinmortal', 'LoreIV_Elaburrimientoyla',
     umbral='Despertar 68 + el botón LORE del Cronista')}
{sec('[13] ' + K.get('CodexVIIITit', ''), 'CodexVIIIP0', 'CodexVIIIP1', 'CodexVIIIP2', 'CodexVIIIP3',
     umbral='Despertar 76 + el botón LORE del Cronista')}
{sec('[14] ' + K.get('CodexIXTit', ''), 'CodexIXP0', 'CodexIXP1', 'CodexIXP2', 'CodexIXP3',
     umbral='Despertar 84 + el botón LORE del Cronista')}
{sec('[15] ' + K.get('LoreSecV', ''), 'LoreV_LaIra', 'LoreV_ElPilar', 'LoreV_ElSellado',
     'LoreV_Loscompaneros', 'LoreV_Lassombras', 'LoreV_Larespuestafinal',
     umbral='Despertar 90 + el botón LORE del Cronista')}
{sec('[16] ' + K.get('CodexXTit', ''), 'CodexXP0', 'CodexXP1', 'CodexXP2', 'CodexXP3', 'CodexXP4', 'CodexXP5',
     umbral='Despertar 96 + el botón LORE del Cronista')}
{sec('[17] ' + K.get('CodexXITit', ''), 'CodexXIP0', 'CodexXIP1', 'CodexXIP2', 'CodexXIP3',
     umbral='Despertar 102 + el botón LORE del Cronista')}
{sec('[18] ' + K.get('CodexEpilogoTit', ''), 'CodexEpilogoP0', 'CodexEpilogoP1', 'CodexEpilogoP2', 'CodexEpilogoP3',
     umbral='Despertar 110 + el botón LORE del Cronista')}

---

# LAS NARRACIONES DEL CRONISTA (el botón LORE — su pedacito + su pensamiento)

> La letra: «al hablra con el cronista dira una pequeña perte de lo que
> es el lore que se debe desbloquear y su propio pensamiento». Cada
> charla de LORE abre UNA sección (en orden) y cuenta su pedacito con
> el pensamiento del autor. La primera vez que el portador habla con
> él, dice que TODO el lore lo escribió ÉL.

## La primera charla — LA AUTORÍA

{K.get('LorePrimeraVez', '')}

## Las narraciones (una por sección)

{K.get('LoreHabla1', '')}

{K.get('LoreHabla2', '')}

{K.get('LoreHabla3', '')}

{K.get('LoreHabla4', '')}

{K.get('LoreHabla5', '')}

{K.get('LoreHabla6', '')}

{K.get('LoreHabla7', '')}

{K.get('LoreHabla8', '')}

{K.get('LoreHabla9', '')}

{K.get('LoreHabla10', '')}

{K.get('LoreHabla11', '')}

{K.get('LoreHabla12', '')}

{K.get('LoreHabla13', '')}

{K.get('LoreHabla14', '')}

{K.get('LoreHabla15', '')}

{K.get('LoreHabla16', '')}

{K.get('LoreHabla17', '')}

{K.get('LoreHabla18', '')}

## Si el Despertar no alcanza

{K.get('LoreTemedor', '')}

## Cuando TODO está contado

{K.get('LoreCompleto', '')}
"""

(RAIZ / "LORE-ANTES.md").write_text(antes, encoding="utf-8")
(RAIZ / "LORE-DESPUES.md").write_text(despues, encoding="utf-8")
print("LORE-ANTES.md:", len(antes), "chars")
print("LORE-DESPUES.md:", len(despues), "chars")
