#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_congelado.py — v6.176 — LOS JEFES DEVORADOS CALLAN (la
letra: «mientras los jefes estan siendo devorados por el grimorio del
eterno siguen lanzando proyectiles de jefes, en el momento en el que
empiezan a ser devorado ellos deben dejar de lanzar proyectiles»).

LA AUTOPSIA: el festín clásico y la Primera Cena YA congelan a su
preso (PreAI → return false), pero la ASPIRADORA del Devorador (la
segunda invocación) solo ponía velocity=0 y damage=0 desde SU AI —
la IA de vanilla del gusano seguía VIVA cada tick: la cabeza seguía
escupiendo bolas corrosivas y el motor de la arena disparando voleas
mientras el libro se lo comía.

LA CURA: EL CONGELADO DE LA ASPIRADORA — un campo en FaucesGlobalNPC
que el cine pone al capturar y quita al terminar (o al morir el cine:
jamás un gusano zombi). El PreAI lo respeta ANTES que nada: ni IA, ni
animación, ni UN proyectil. El motor de combate también lo consulta
(via cirugia_v6176_barra.py).
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F = RAIZ / "AethonMod" / "Content" / "Globals" / "FaucesGlobalNPC.cs"

# ---------- (1) EL CAMPO ----------
V1 = """        public byte DevorarEstado;"""

N1 = """        public byte DevorarEstado;

        /// <summary>v6.176 — EL CONGELADO DE LA ASPIRADORA: el cine
        /// espacial del Devorador congela al gusano SIN pasar por el
        /// motor del festín (la letra: «en el momento en el que
        /// empiezan a ser devorados ellos deben dejar de lanzar
        /// proyectiles»): la aspiradora lo pone al capturar y lo quita
        /// al terminar — o al morir el cine a medias (jamás un gusano
        /// zombi: DescongelarAspiradora barre el mundo entero).</summary>
        public bool CongeladoPorAspiradora;"""

# ---------- (2) EL FREEZE EN PreAI (antes de cualquier gatillo) ----------
V2 = """            if (_cenaActiva >= 0 &&
                (_cenaActiva >= Main.maxNPCs || Main.npc[_cenaActiva] is not { active: true }))
                _cenaActiva = -1;"""

N2 = """            if (_cenaActiva >= 0 &&
                (_cenaActiva >= Main.maxNPCs || Main.npc[_cenaActiva] is not { active: true }))
                _cenaActiva = -1;

            // === v6.176 — EL CONGELADO DE LA ASPIRADORA (la letra:
            //     «mientras los jefes estan siendo devorados por el
            //     grimorio del eterno siguen lanzando proyectiles de
            //     jefes, en el momento en el que empiezan a ser
            //     devorado ellos deben dejar de lanzar proyectiles»):
            //     la pieza en el cine espacial NI PIENSA — ni IA, ni
            //     animación, ni UN solo proyectil: solo la posición que
            //     el motor del cine dicta. Va PRIMERO: nada de
            //     guardianes, cenas ni gatillos para un preso del
            //     cine. ===
            if (CongeladoPorAspiradora && npc.active)
            {
                npc.velocity *= 0f;
                npc.damage = 0;
                if (npc.lifeRegen < 0) npc.lifeRegen = 0;
                return false;
            }"""

# ---------- (3) EL DESCONGELADO GLOBAL ----------
V3 = """        public static void ArrancarPrimeraCena(NPC jefe, Player portador)"""

N3 = """        /// <summary>v6.176 — EL DESCONGELADO: la aspiradora terminó (o
        /// murió a medias — el cine interrumpido) — NINGÚN gusano queda
        /// congelado para siempre. Barre el mundo entero: el flag es de
        /// un solo cine a la vez.</summary>
        public static void DescongelarAspiradora()
        {
            try
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active) continue;
                    if (n.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) && g.CongeladoPorAspiradora)
                    {
                        g.CongeladoPorAspiradora = false;
                        n.netUpdate = true;
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("FaucesGlobalNPC", exRegistro); }
        }

        public static void ArrancarPrimeraCena(NPC jefe, Player portador)"""


def main() -> int:
    t = F.read_text(encoding="utf-8")
    ya = sum(1 for v in (N1, N2, N3) if v in t)
    if ya:
        print(f"YA APLICADO parcialmente ({ya}/3) — se aborta")
        return 1 if ya < 3 else 0
    for etiqueta, viejo, nuevo in [
        ("campo CongeladoPorAspiradora", V1, N1),
        ("freeze en PreAI", V2, N2),
        ("descongelado global", V3, N3),
    ]:
        assert viejo in t, f"PATRÓN NO ENCONTRADO: {etiqueta}"
        t = t.replace(viejo, nuevo, 1)
        print(f"✔ {etiqueta}")
    F.write_text(t, encoding="utf-8")
    print("✔ FaucesGlobalNPC: el congelado de la aspiradora vive")
    return 0


if __name__ == "__main__":
    sys.exit(main())
