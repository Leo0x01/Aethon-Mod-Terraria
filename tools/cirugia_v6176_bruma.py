#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_bruma.py — v6.176 — LA BRUMA DE LA BASE DE LOS TENTÁCULOS
(la letra: «en el mundo interior cada tentaculo de fondo debe tener en
su base una bruma de esa forma no se ve cortado el tentaculo»).

LA AUTOPSIA: las tres capas de tentáculos del fondo (lejos · medio ·
cerca) cuelgan desde arriba de la pantalla y su PUNTA termina A SECAS
en el aire — el ojo ve el corte brusco de la tinta. LA CURA: cada
tentáculo lleva SU bruma en la punta (BrumaFX.Puff — R3: la niebla de
la casa, JAMÁS óvalos): la tinta se DISUELVE en niebla y el tentáculo
se apaga, no se corta. El radio y el matiz nacen de la MISMA semilla
del tentáculo (determinista — la misma niebla en cada máquina).
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F = RAIZ / "AethonMod" / "Content" / "Effects" / "CieloInteriorEterno.cs"

# ---------- (1) CAPA LEJOS ----------
V1 = """                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, tK, sem, 16, 0.16f);
                            Color tintaLejos = new Color(30, 18, 52);
                            DibujarTira(sb, espina, 22f, 46f, tintaLejos,
                                (0.60f + 0.20f * latido) * a, flujo: tK * 0.026f + k * 0.19f);"""

N1 = """                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, tK, sem, 16, 0.16f);
                            Color tintaLejos = new Color(30, 18, 52);
                            DibujarTira(sb, espina, 22f, 46f, tintaLejos,
                                (0.60f + 0.20f * latido) * a, flujo: tK * 0.026f + k * 0.19f);
                            // v6.176 — LA BRUMA DE LA BASE (la letra: «cada
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

# ---------- (2) CAPA MEDIO ----------
V2 = """                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, tK, sem, 18, 0.19f);
                            DibujarTira(sb, espina, 22f, 52f, new Color(38, 24, 66),
                                (0.50f + 0.16f * latido) * a, flujo: tK * 0.020f + k * 0.21f);"""

N2 = """                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, tK, sem, 18, 0.19f);
                            DibujarTira(sb, espina, 22f, 52f, new Color(38, 24, 66),
                                (0.50f + 0.16f * latido) * a, flujo: tK * 0.020f + k * 0.21f);
                            // v6.176 — LA BRUMA DE LA BASE (misma letra): la
                            // tinta media se deshace en SU niebla al llegar a
                            // la punta — nada termina cortado.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                80f + 28f * VFXCore.Hash01(sem, 5, 941),
                                new Color(38, 24, 66), sem * 3, t,
                                alpha: 0.30f * a, quality: 0.5f);"""

# ---------- (3) CAPA CERCA (los titanes) ----------
V3 = """                            // EL CUERPO (fusiforme: raíz 34 → vientre 104 → cabeza 66)
                            DibujarTira(sb, espina, 34f, 104f, new Color(58, 36, 96),
                                (0.78f + 0.14f * latido) * a, flujo: tK * 0.030f + k * 0.29f,
                                grosorCentroAlt: 66f);"""

N3 = """                            // EL CUERPO (fusiforme: raíz 34 → vientre 104 → cabeza 66)
                            DibujarTira(sb, espina, 34f, 104f, new Color(58, 36, 96),
                                (0.78f + 0.14f * latido) * a, flujo: tK * 0.030f + k * 0.29f,
                                grosorCentroAlt: 66f);
                            // v6.176 — LA BRUMA DE LA BASE (misma letra): el
                            // titán es TINTA GRUESA — su punta se disuelve en
                            // la niebla más ancha del telón.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                118f + 42f * VFXCore.Hash01(sem, 5, 959),
                                new Color(58, 36, 96), sem * 3, t,
                                alpha: 0.34f * a, quality: 0.6f);"""


def main() -> int:
    t = F.read_text(encoding="utf-8")
    ya = sum(1 for v in (N1, N2, N3) if v in t)
    if ya:
        print(f"YA APLICADO parcialmente ({ya}/3) — se aborta")
        return 1 if ya < 3 else 0
    for etiqueta, viejo, nuevo in [
        ("bruma base capa lejos", V1, N1),
        ("bruma base capa medio", V2, N2),
        ("bruma base capa cerca", V3, N3),
    ]:
        assert viejo in t, f"PATRÓN NO ENCONTRADO: {etiqueta}"
        t = t.replace(viejo, nuevo, 1)
        print(f"✔ {etiqueta}")
    F.write_text(t, encoding="utf-8")
    print("✔ CieloInteriorEterno: bruma en la punta de cada tentáculo de fondo")
    return 0


if __name__ == "__main__":
    sys.exit(main())
