#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_crashes.py — v6.176 — LA CURA DE LOS DOS CRASHES DEL
client.log (la letra: «algo fallo en el mundo interior al inveocar al
Devoramundos»).

EL AUTOPSIA (client.log v6.175, 211 repeticiones registradas = ~42.000
frames de spam):
  1. JefeInteriorVoleaProjectile.PreDraw L163 — «Draw was called, but
     Begin has not yet been called»: el PreDraw cerraba el lote de
     vanilla (CerrarLoteSiAbierto) y luego dibujaba con
     Main.spriteBatch.Draw ¡SIN ABRIR NINGÚN LOTE! (la volea nació
     muerta: cada proyectil reventaba en su primer dibujo).
  2. AspiradoraDevoradorProjectile.PreDraw L280 — el mismo pecado en el
     halo del libro: tras el finally{End()} del bloque del libro, los
     DOS Draw del halo corrían con el lote cerrado.

LA CURA (el patrón de la casa, R6 — la convención del lote):
  1. La volea abre SU lote aditivo con VFXCore.AbrirAdditive() (la
     sonda cierra el lote ajeno antes) y sale con
     VFXCore.RestaurarLoteVanilla() (LA SALIDA correcta de todo PreDraw
     que dibuja directo — la lección v6.56.1).
  2. El halo de la aspiradora viaja en SU propio lote aditivo
     (AbrirAdditive + CerrarLoteSiAbierto en finally) — el mismo
     contrato del festín.

El fix (2) vive en la REESCRITURA completa del archivo (la animación
también cambia por la letra de la sesión); este script solo cura la
VOLEA — el otro crash muere con el archivo nuevo.
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F = RAIZ / "AethonMod" / "Content" / "Projectiles" / "Jefes" / "JefeInteriorVoleaProjectile.cs"

viejo = """            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // LA ESTELA (tres fantasmas del camino recorrido)"""

nuevo = """            // v6.176 — EL LOTE DE LA CASA (la cura del crash del
            // client.log: «Draw was called, but Begin has not yet been
            // called» ×42.000 frames — la volea cerraba el lote de
            // vanilla y dibujaba SIN abrir el suyo). Ahora abre SU lote
            // aditivo (R6) y sale con RestaurarLoteVanilla — LA SALIDA
            // correcta de todo PreDraw que dibuja directo.
            VFXCore.AbrirAdditive();
            try
            {
                // LA ESTELA (tres fantasmas del camino recorrido)"""

viejo_salida = """            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorVolea", exRegistro);
            }
            VFXCore.ReabrirLoteVanilla();
            return false;"""

nuevo_salida = """            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorVolea", exRegistro);
            }
            VFXCore.RestaurarLoteVanilla();   // cierra NUESTRO aditivo y reabre el de vanilla
            return false;"""


def main() -> int:
    t = F.read_text(encoding="utf-8")
    if nuevo in t:
        print("YA CURADO — el parche v6.176 ya vive en el archivo")
        return 0
    assert viejo in t, "PATRÓN 1 NO ENCONTRADO (apertura del lote) — el archivo cambió de forma"
    assert viejo_salida in t, "PATRÓN 2 NO ENCONTRADO (salida del PreDraw) — el archivo cambió de forma"
    t = t.replace(viejo, nuevo, 1)
    t = t.replace(viejo_salida, nuevo_salida, 1)
    F.write_text(t, encoding="utf-8")
    print("✔ JefeInteriorVoleaProjectile: lote aditivo propio + salida RestaurarLoteVanilla")
    return 0


if __name__ == "__main__":
    sys.exit(main())
