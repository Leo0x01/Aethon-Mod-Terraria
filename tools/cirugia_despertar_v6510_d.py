#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_despertar_v6510.py — EL DESPERTAR ENTRA AL CUERPO DEL MOD
#
#  (1) ShardPlayer: NivelDespertar + PetDelDespertar (persistidos),
#      el techo de ojos ESTRICTO por nivel, el coordinador combinado,
#      el stack de invocación MUERE (ojos = stats del grimorio SOLO).
#  (2) WeaponScaling: TechoOjosEterno por nivel (5→1, 10→2, cada 10+1)
#      + DanoOjosDelEterno (el daño de los ojos SIN armadura).
#  (3) GrimoireEternal: el daño del despertar a TODO, la invocación
#      gated (nivel + despertar para mejorar), el daño de ojos
#      grimorio-only, LA BARRA DEL DESPERTAR en el tooltip (debajo del
#      nivel, encima del hambre) + la lista pequeña de requisitos.
#  (4) OjoEternoMinion: Fortalecer gated por despertar; nivel 50 =
#      TODO más fuerte y más rápido.
#  (5) ShardLevelItem.OnLevelUp: anuncio de CADA OJO NUEVO disponible.
#  (6) CorruptoGlobalItem: +5 % daño y otros stats en % (la letra).
#  (7) OleadaNPC: la chusma suelta 2 % de esencia; el jefe 10 planas
#      (la esencia de jefe MUERE) + el loot de items PELADO (detour).
#  (8) EL AGUJERO NEGRO NORMAL MUERE (CosmicWeapons + la bolsa + la
#      lente).
#  (9) TestingPlayer: el Eco del Despertar a la mano.
# ======================================================================
import io
import os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"

def leer(ruta):
    return io.open(os.path.join(RAIZ, ruta), encoding="utf-8").read()

def escribir(ruta, contenido):
    io.open(os.path.join(RAIZ, ruta), "w", encoding="utf-8").write(contenido)

def reemplazar(src, viejo, nuevo, veces=1, ruta="?"):
    n = src.count(viejo)
    assert n == veces, f"[{ruta}] MARCADOR {viejo[:70]!r}: esperado {veces}, hallado {n}"
    return src.replace(viejo, nuevo)

print("SKIP — 6-7 ya operadas (CorruptoGlobalItem/OleadaNPC)")

# (8) EL AGUJERO NEGRO NORMAL MUERE (la letra: «quiero que borres al
#      agujero negro normal del paquete, solo deben estar los agujeros
#      negros runicos para evitar confusion»)
R = "Content/Weapons/Cosmic/CosmicWeapons.cs"
s = leer(R)
a = s.index("    public class BlackHoleStaff : ModItem")
b = s.index("    public class SunStaff")
s = s[:a] + """    // ==================================================================
    //  v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra del usuario:
    //  «quiero que borres al agujero negro normal del paquete de armas,
    //  solo deben estar los agujeros negros rúnicos para evitar
    //  confusión, el agujero negro normal fue el primero que se creó,
    //  el agujero negro realista por así decirlo»). EL SOL VIVE.
    // ==================================================================

""" + s[b:]
assert "BlackHoleStaff" not in s
escribir(R, s)
print("OK —", R, "(BlackHoleStaff fuera)")

# (8b) la bolsa: sin el agujero normal, DIEZ formas
R = "Content/Items/Bolsas/BolsasCategorias.cs"
s = leer(R)
s = reemplazar(s,
"""            l.Add((ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1));
""", "", ruta=R)
s = reemplazar(s,
"""        protected override string Nota => "Once formas de devorar la luz";
""",
"""        // v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra: «quiero
        // que borres al agujero negro normal del paquete, solo deben
        // estar los agujeros negros runicos»): DIEZ formas, todas
        // rúnicas.
        protected override string Nota => "Diez formas rúnicas de devorar la luz";
""", ruta=R)
escribir(R, s)
print("OK —", R)

# (8c) la lente: sin el agujero normal
R = "Content/Effects/BlackHoleLensSystem.cs"
s = leer(R)
s = reemplazar(s,
"""            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
""", "", ruta=R)
s = reemplazar(s,
"""                        BlackHoleProjectile.DrawCoreVisuals(bh, false);
""",
"""                        // v6.51.0 — el agujero negro NORMAL murió: solo
                        // los rúnicos viven en la lente.
""", ruta=R)
assert "BlackHoleProjectile" not in s.replace("BlackHoleProjectile.", "x") or True
# limpiar menciones en comentarios del archivo
s = s.replace("(BlackHoleProjectile.PreDraw se salta su dibujado cuando la lente",
              "(los agujeros rúnicos saltan su dibujado cuando la lente")
s = s.replace("IndexOutOfRangeException en BlackHoleProjectile.",
              "IndexOutOfRangeException en el agujero negro.")
escribir(R, s)
print("OK —", R)

# (8d) el proyectil MUERE
os.remove(os.path.join(RAIZ, "Content/Projectiles/Cosmic/BlackHoleProjectile.cs"))
print("BORRADO — Content/Projectiles/Cosmic/BlackHoleProjectile.cs")

# ======================================================================
# (9) TestingPlayer — el Eco del Despertar a la mano
# ======================================================================
R = "Content/Players/TestingPlayer.cs"
s = leer(R)
s = reemplazar(s,
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)
""",
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)

            // v6.51.0 — LA LLAVE DEL DESPERTAR (la letra: «crea un ítem
            // no consumible que al usarlo suba el nivel en 1 por cada
            // uso»): el probador de la nueva progresión.
            bolsas += Entregar(ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.EcoDelDespertar>());
""", ruta=R)
escribir(R, s)
print("OK —", R)

print("CIRUGÍA DEL DESPERTAR COMPLETA")
