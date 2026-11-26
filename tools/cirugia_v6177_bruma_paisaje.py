#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_bruma_paisaje.py — LA TUMBA DE NIEBLA de los tentáculos del paisaje.

v6.177 (la letra): «la base de los tentaculos en el fondo del mundo
interior no tiene bruma, cuando digo en el fondo del mundo interior me
refiero al fondo del paisaje, esos tentaculos que se ven a los lejos en
el paisaje en el fondo necesitan brumas en el fondo para que no se vean
cortados».

LA AUTOPSIA: la v6.176 puso UN Puff por tentáculo (radio 64-118, alfa
0.26-0.34) EXACTO en la punta — un velo tímido: la punta se seguía
leyendo CORTADA (el borde del cuerpo pasaba por debajo del velo).

LA CURA — LA TUMBA DE NIEBLA: por cada tentáculo, TRES masas que
SEPULTAN la punta a lo largo de SU rumbo final: EN la punta · BAJO ella
(0.62·radio más allá) · más ancha y floja debajo. El tentáculo se APAGA
en la bruma — no termina, se disuelve.
"""
R = 'AethonMod/Content/Effects/CieloInteriorEterno.cs'
s = open(R, encoding='utf-8').read()
orig = s

def tumba(viejo_puff, tinta, radio, semilla, alfa, esc, calidade):
    return f"""// v6.177 — LA TUMBA DE NIEBLA (la letra: «cuando digo en el
                            // fondo del mundo interior me refiero al fondo del
                            // paisaje, esos tentaculos que se ven a los lejos
                            // en el paisaje en el fondo necesitan brumas en el
                            // fondo para que no se vean cortados»): UN solo
                            // puff a ras de la punta era un velo tímido — el
                            // borde se leía cortado DEBAJO. Ahora TRES masas a
                            // lo largo del rumbo final SEPULTAN la punta: en
                            // ella · bajo ella · más ancha y floja más abajo.
                            // El tentáculo se APAGA en la bruma, no termina.
                            Vector2 punta = espina[espina.Length - 1];
                            Vector2 rumboFin = espina.Length > 1
                                ? (punta - espina[espina.Length - 2]).SafeNormalize(Vector2.UnitY)
                                : Vector2.UnitY;
                            float radioTumba = {radio}f + {esc}f * VFXCore.Hash01(sem, 5, {semilla});
                            for (int b = 0; b < 3; b++)
                            {{
                                Vector2 posB = punta + rumboFin * (radioTumba * 0.62f * b);
                                BrumaFX.Puff(posB,
                                    radioTumba * (1f - 0.16f * b) + (b > 0 ? 30f : 0f),
                                    {tinta}, sem * 3 + b * 7, t + b * 0.31f,
                                    alpha: ({alfa}f - 0.05f * b) * a, quality: {calidade}f);
                            }}"""

# ── CAPA LEJOS (la tinta más tenue) ────────────────────────────────
viejo = """                            // v6.176 — LA BRUMA DE LA BASE (la letra: «cada
                            // tentaculo de fondo debe tener en su base una
                            // bruma de esa forma no se ve cortado el
                            // tentaculo»): la punta se DISUELVE en niebla de
                            // SU MISMA tinta — el tentáculo se apaga, no se
                            // corta. (Puff en el lote ABIERTO de la capa —
                            // el contrato de BrumaFX.)
                            BrumaFX.Puff(espina[espina.Length - 1],
                                64f + 24f * VFXCore.Hash01(sem, 5, 923),
                                tintaLejos, sem * 3, t,
                                alpha: 0.26f * a, quality: 0.5f);"""
nuevo = tumba(viejo, 'tintaLejos', 78, 26, 0.46, 923, 0.55)
assert viejo in s, 'la bruma de LEJOS no se encontró'
s = s.replace(viejo, nuevo, 1)

# ── CAPA MEDIO ─────────────────────────────────────────────────────
viejo = """                            // v6.176 — LA BRUMA DE LA BASE (misma letra): la
                            // tinta media se deshace en SU niebla al llegar a
                            // la punta — nada termina cortado.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                80f + 28f * VFXCore.Hash01(sem, 5, 941),
                                new Color(38, 24, 66), sem * 3, t,
                                alpha: 0.30f * a, quality: 0.5f);"""
nuevo = tumba(viejo, 'new Color(38, 24, 66)', 96, 32, 0.48, 941, 0.6)
assert viejo in s, 'la bruma de MEDIO no se encontró'
s = s.replace(viejo, nuevo, 1)

# ── CAPA CERCA (los titanes: la tinta gruesa) ──────────────────────
viejo = """                            // v6.176 — LA BRUMA DE LA BASE (misma letra): el
                            // titán es TINTA GRUESA — su punta se disuelve en
                            // la niebla más ancha del telón.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                118f + 42f * VFXCore.Hash01(sem, 5, 959),
                                new Color(58, 36, 96), sem * 3, t,
                                alpha: 0.34f * a, quality: 0.6f);"""
nuevo = tumba(viejo, 'new Color(58, 36, 96)', 138, 46, 0.52, 959, 0.65)
assert viejo in s, 'la bruma de CERCA no se encontró'
s = s.replace(viejo, nuevo, 1)

open(R, 'w', encoding='utf-8', newline='\n').write(s)
print(f'OK CieloInteriorEterno: la tumba de niebla en las tres capas '
      f'({len(orig)} → {len(s)} bytes)')
