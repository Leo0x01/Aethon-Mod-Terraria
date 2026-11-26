#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6178.py — LAS CIRUGÍAS DE LA v6.178 (la letra de la sesión 31).

Todas con asserts (si el patrón no está, el script MUERE en vez de
corromper — la ley de la casa §1.2 paso 4):

  1. LA BRUMA DEL INTERIOR (la letra: «la bruma que añadiste esta mal
     colocada hace que los fps bajen hasta solo 5 fps, la bruma es
     demasiada en el fondo, debes dejar el mundo interior como hace 2
     commit atras, y luego asegurarte de que los tentaculos que
     aparecen en el fondo en el paisaje tengan una pequeña bruma en la
     base»): se RETIRAN enteros los 3 puffs de la v6.176 Y las 3 tumbas
     de niebla de la v6.177 (radios hasta ~1100 px × 3 masas × 14
     tentáculos en una GPU integrada = la muerte del fill-rate) y
     nace UNA bruma PEQUEÑA por tentáculo: radio acotado al ancho de
     SU punta, calidad MÍNIMA (pocos sub-blobs), alfa bajo.
  2. LA NOCHE PRESTADA UNIVERSAL (la letra: «en el mundo interior
     intento invocar al ojo de cuthulu pero este se va, eso quiere
     decir que el mundo interior no cumple mi peticion de que debe ser
     apto para todos los jefes»): el interior vive en MEDIODÍA ETERNO y
     las IA de vanilla (EoC, mechs, abejas…) ven el día y SE VAN — la
     noche prestada se extiende de SOLO SkeletronHead a TODO NPC.
  3. LOS PROYECTILES QUE NO MUEREN (la letra: «los proyectiles que
     lanza el Rey Slime en nivel 26 se quedan en el mapa
     permanentemente, eso esta mal, esos proyectiles deben desaparecer
     en segundos, analiza que este error no este en mas jefes»): el
     default vanilla de timeLeft es 3600 ticks = 60 SEGUNDOS (verificado
     contra el decompile) y el Spike del Rey Slime no tiene override —
     TODA volea de jefe (los proyectiles vanilla temáticos) nace ya con
     la vida contada: timeLeft = 180 (3 segundos).
  4. LA PÁGINA EN BLANCO RESETEA AL DEVORAMUNDOS (la letra: «si uso el
     item pagina en blanco para borrar el festin, entonces debe volver
     a comerse al Devoramundos con su segunda invocacion desde ahi»):
     el olvido también borra EL VIGÍA (InvocacionesInteriorTipos/Cuenta).
  5. EL NOMBRE BAJO LA RANURA (la letra: «los 3 slot del arma estan en
     blanco aunque si tienen el arma»): la persistencia quedó PROBADA
     viva en el banco de pruebas headless (DiagnosticoPersistencia)
     — el blanco era del dibujo; ahora cada ranura del trío lleva el
     NOMBRE de su arma cosido debajo y el icono se blinda con fallback:
     si el icono no carga, el nombre igual SE LEE.
"""
import re, sys, os

RAIZ = os.path.join(os.path.dirname(__file__), "..", "AethonMod")
def leer(ruta):
    return open(os.path.join(RAIZ, ruta), encoding="utf-8").read()
def escribir(ruta, s):
    open(os.path.join(RAIZ, ruta), "w", encoding="utf-8").write(s)
    print(f"  [ok] {ruta}")

# ═══════════════════════════════════════════════════════════════════
# 1. LA BRUMA DEL INTERIOR — CieloInteriorEterno.cs
# ═══════════════════════════════════════════════════════════════════
print("[1] la bruma del interior: fuera la tumba, dentro la bruma PEQUEÑA")
s = leer("Content/Effects/CieloInteriorEterno.cs")

# 1a. LAS TRES TUMBAS DE LA v6.177 (bloques completos) — se retiran ENTERAS
#     (hasta la llave del `for` de las 3 masas — la llave del bucle k
#     del tentáculo queda VIVA)
tumba = re.compile(
    r"\n// v6\.177 — LA TUMBA DE NIEBLA.*?for \(int b = 0; b < 3; b\+\+\)\s*\{\s*"
    r"Vector2 posB = punta \+ rumboFin \* \(radioTumba \* 0\.62f \* b\);.*?"
    r"alpha: \(0\.\d+f - 0\.05f \* b\) \* a, quality: 0\.\d+f\);\s*\}",
    re.S)
n_tumbas = len(tumba.findall(s))
assert n_tumbas == 3, f"esperaba 3 tumbas v6.177, hallé {n_tumbas}"
s = tumba.sub("", s)
print(f"  [ok] -3 tumbas de niebla v6.177 ({n_tumbas})")

# 1b. LOS TRES PUFFS DE LA v6.176 — se retiran ENTEROS
puff176 = re.compile(
    r"\n\s*// v6\.176 — LA BRUMA DE LA BASE.*?BrumaFX\.Puff\(espina\[espina\.Length - 1\],\s*"
    r"\d+f \+ \d+f \* VFXCore\.Hash01\(sem, 5, 9\d\d\),\s*"
    r"(?:tintaLejos|new Color\(\d+, \d+, \d+\)), sem \* 3, t,\s*"
    r"alpha: 0\.\d+f \* a, quality: 0\.[56]f\);",
    re.S)
n_puffs = len(puff176.findall(s))
# (la v6.177 ya había COMIDO los puffs de la v6.176 al poner sus tumbas —
# tras retirar las tumbas, el cielo volvió SOLITO al estado pre-bruma)
assert n_puffs in (0, 3), f"puffs v6.176 inesperados: {n_puffs}"
if n_puffs > 0:
    s = puff176.sub("", s)
    print(f"  [ok] -3 puffs v6.176 ({n_puffs})")
else:
    print("  [—] los puffs v6.176 ya no vivían (v6.177 los comió)")

# 1c. LA BRUMA PEQUEÑA NUEVA — una por tentáculo, en la punta (la base
#     visible del tentáculo que cuelga): radio acotado al ancho de SU
#     punta ×~1.6 (lejos 46→~72 · medio 52→~82 · titán 104→~150),
#     calidad 0.16 (≈3-5 sub-blobs), alfa bajo. LA LECCIÓN v6.178 que
#     nace: en el CIELO del interior, un puff por tentáculo y NUNCA
#     radio > ~160 — la cámara es una GPU integrada con el mundo entero
#     encima.
NUEVA = {
    # capa lejana (después de DibujarTira … 22f, 46f, tintaLejos)
    "flujo: tK * 0.026f + k * 0.19f);":
    """
                            // v6.178 — LA BRUMA PEQUEÑA DE LA BASE (la
                            // letra: «los tentaculos que aparecen en el
                            // fondo en el paisaje tengan una pequeña
                            // bruma en la base» — y la LECCIÓN de la 5 fps:
                            // el cielo del interior es un DECORADO de
                            // pantalla completa en GPUs integradas — la
                            // bruma de la base es PEQUEÑA (radio al ras
                            // del ancho de la punta, calidad mínima) y
                            // UNA por tentáculo: se apaga, no tapa.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                52f + 20f * VFXCore.Hash01(sem, 5, 178),
                                tintaLejos, sem * 3, t,
                                alpha: 0.34f * a, quality: 0.16f);""",
    # capa media
    "flujo: tK * 0.020f + k * 0.21f);":
    """
                            // v6.178 — LA BRUMA PEQUEÑA DE LA BASE (misma
                            // letra): una sola, al ras de la punta.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                62f + 22f * VFXCore.Hash01(sem, 5, 278),
                                new Color(38, 24, 66), sem * 3, t,
                                alpha: 0.36f * a, quality: 0.16f);""",
    # capa cercana (titanes)
    "grosorCentroAlt: 66f);":
    """
                            // v6.178 — LA BRUMA PEQUEÑA DE LA BASE (misma
                            // letra): el titán es TINTA GRUESA — su bruma
                            // es apenas más ancha que su punta y YA.
                            BrumaFX.Puff(espina[espina.Length - 1],
                                104f + 34f * VFXCore.Hash01(sem, 5, 378),
                                new Color(58, 36, 96), sem * 3, t,
                                alpha: 0.38f * a, quality: 0.18f);""",
}
for ancla, bloque in NUEVA.items():
    assert ancla in s, f"ancla no hallada: {ancla}"
    s = s.replace(ancla, ancla + bloque, 1)
    print(f"  [ok] +bruma pequeña ({ancla[:34]}…)")

escribir("Content/Effects/CieloInteriorEterno.cs", s)

# ═══════════════════════════════════════════════════════════════════
# 2. LA NOCHE PRESTADA UNIVERSAL — JefeInteriorGlobalNPC.cs
# ═══════════════════════════════════════════════════════════════════
print("[2] la noche prestada UNIVERSAL: todos los jefes se quedan")
s = leer("Content/Globals/JefeInteriorGlobalNPC.cs")
VIEJO = """            _prestaNocheInterior = false;
            if (npc.type == NPCID.SkeletronHead &&
                Systems.SubmundoGrimorio.Activo && Main.dayTime)
            {
                Main.dayTime = false;
                _prestaNocheInterior = true;
            }"""
NUEVO2 = """            _prestaNocheInterior = false;
            // v6.178 — LA NOCHE PRESTADA UNIVERSAL (la letra: «en el mundo
            //     interior intento invocar al ojo de cuthulu pero este se
            //     va, eso quiere decir que el mundo interior no cumple mi
            //     peticion de que debe ser apto para todos los jefes»):
            //     el interior vive en MEDIODÍA ETERNO y las IA de vanilla
            //     HUYEN del día (el Ojo se va volando, los mechs se
            //     retiran, las abejas reina se elevan — el decompile:
            //     EncourageDespawn por IsItDay en media docena de jefes).
            //     La noche se presta a TODO NPC mientras el interior
            //     viva: dentro, NADIE ve el amanecer y todos pelean.
            //     (La v6.152 la prestaba solo a SkeletronHead — la cabeza
            //     era el único cliente; ahora el EoC del usuario también.)
            if (Systems.SubmundoGrimorio.Activo && Main.dayTime)
            {
                Main.dayTime = false;
                _prestaNocheInterior = true;
            }"""
assert VIEJO in s, "ancla de la noche prestada no hallada"
s = s.replace(VIEJO, NUEVO2, 1)
escribir("Content/Globals/JefeInteriorGlobalNPC.cs", s)
print("  [ok] noche prestada: de SkeletronHead a TODO el interior")

# ═══════════════════════════════════════════════════════════════════
# 3. LOS PROYECTILES QUE NO MUEREN — JefeInteriorGlobalNPC.cs (voleas)
# ═══════════════════════════════════════════════════════════════════
print("[3] los proyectiles del Rey Slime y TODOS los jefes: 3 segundos y fuera")
s = leer("Content/Globals/JefeInteriorGlobalNPC.cs")
VIEJO = """                int dano = Math.Max(8, (int)(npc.damage * 0.55f));
                var fuente = npc.GetSource_FromAI();"""
NUEVO3 = """                int dano = Math.Max(8, (int)(npc.damage * 0.55f));
                var fuente = npc.GetSource_FromAI();
                // v6.178 — LA VIDA CONTADA (la letra: «los proyectiles
                //     que lanza el Rey Slime en nivel 26 se quedan en el
                //     mapa permanentemente […] deben desaparecer en
                //     segundos, analiza que este error no este en mas
                //     jefes»): el default vanilla de timeLeft es 3600
                //     ticks = 60 SEGUNDOS (verificado en el decompile —
                //     y el Spike del Rey Slime NO tiene override) — con
                //     la furia del nivel alto lloviendo, la arena se
                //     llenaba de basura eterna. TODA volea de TODOS los
                //     jefes nace con la vida contada: 180 ticks = 3 s
                //     (la ley nueva del motor: nadie dispara nada que
                //     no muera en segundos).
                int vidaUtil = 180;"""
assert VIEJO in s, "ancla del daño de la volea no hallada"
s = s.replace(VIEJO, NUEVO3, 1)

# ahora los 4 NewProjectile de la volea: guardar el índice y sellar la vida
patron = re.compile(
    r"(Projectile\.NewProjectile\(fuente, (?:npc\.Center|pos), vel, tipo, dano, 2f,\s*\n\s*Main\.myPlayer, [0-3]f, tema, 0f\);)")
def sellar(m):
    return "int _npx = " + m.group(1) + "\n                            if (_npx >= 0 && _npx < Main.maxProjectiles)\n                                Main.projectile[_npx].timeLeft = vidaUtil;"
n_reemplazos = len(patron.findall(s))
assert n_reemplazos == 4, f"esperaba 4 NewProjectile en la volea, hallé {n_reemplazos}"
s = patron.sub(sellar, s)
print(f"  [ok] {n_reemplazos} voleas selladas a 180 ticks")
escribir("Content/Globals/JefeInteriorGlobalNPC.cs", s)

# ═══════════════════════════════════════════════════════════════════
# 4. LA PÁGINA EN BLANCO RESETEA AL DEVORAMUNDOS — PaginaEnBlanco.cs
# ═══════════════════════════════════════════════════════════════════
print("[4] la página en blanco también borra EL VIGÍA del Devoramundos")
s = leer("Content/Items/PaginaEnBlanco.cs")
VIEJO = """                sp.GuardianMazmorraDevorado = false;"""
NUEVO4 = """                sp.GuardianMazmorraDevorado = false;
                // v6.178 — EL VIGÍA TAMBIÉN SE BORRA (la letra: «si uso
                //     el item pagina en blanco para borrar el festin,
                //     entonces debe volver a comerse al Devoramundos con
                //     su segunda invocacion desde ahi»): el reloj de
                //     invocaciones del Devorador vuelve a CERO — desde
                //     este olvido, la SEGUNDA vez que el gusano nazca
                //     (pilar, oleada o mundo real) la ASPIRADORA vuelve
                //     a llevarse ENTERO al Devoramundos.
                sp.InvocacionesInteriorTipos?.Clear();
                sp.InvocacionesInteriorCuenta?.Clear();"""
assert VIEJO in s, "ancla GuardianMazmorra no hallada"
s = s.replace(VIEJO, NUEVO4, 1)
escribir("Content/Items/PaginaEnBlanco.cs", s)
print("  [ok] el olvido borra las invocaciones contadas")

# ═══════════════════════════════════════════════════════════════════
# 5. EL NOMBRE BAJO LA RANURA — LibroDelEternoUI.cs
# ═══════════════════════════════════════════════════════════════════
print("[5] las ranuras del trío: el nombre cosido debajo + icono blindado")
s = leer("Content/UI/LibroDelEternoUI.cs")
VIEJO = """                bool llena = ra != null && !ra.IsAir;
                sb.Draw(TextureAssets.MagicPixel.Value, s, llena ? PapelSombra * 1.25f : PapelSombra * 0.9f);
                LibroMarco.Marco(sb, s, hov ? TintaVioleta : llena ? TintaOro : TintaOro * 0.4f);
                if (llena)
                {
                    Texture2D tex = TextureAssets.Item[ra.type].Value;
                    float esc = Math.Min(50f / tex.Width, 50f / tex.Height);
                    sb.Draw(tex, s.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                }
                else
                    LibroLineas.Texto(sb, (i + 1).ToString(), s.Center.ToVector2(), TintaApagado, 1.0f, 0.5f, 0.5f);"""
NUEVO5 = """                bool llena = ra != null && !ra.IsAir;
                sb.Draw(TextureAssets.MagicPixel.Value, s, llena ? PapelSombra * 1.25f : PapelSombra * 0.9f);
                LibroMarco.Marco(sb, s, hov ? TintaVioleta : llena ? TintaOro : TintaOro * 0.4f);
                if (llena)
                {
                    // v6.178 — EL ICONO BLINDADO (la letra: «los 3 slot
                    //     del arma estan en blanco aunque si tienen el
                    //     arma»): la persistencia quedó PROBADA viva en
                    //     el banco headless (DiagnosticoPersistencia —
                    //     las tres ranuras sobreviven el ciclo entero
                    //     del disco) — el blanco era del dibujo. El
                    //     icono vive en try/catch con respaldo (la
                    //     ranura JAMÁS se ve vacía mintiendo)…
                    try
                    {
                        Texture2D tex = TextureAssets.Item[ra.type].Value;
                        float esc = Math.Min(46f / tex.Width, 46f / tex.Height);
                        sb.Draw(tex, s.Center.ToVector2() - new Vector2(0f, 7f), null, Color.White, 0f,
                            tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                    }
                    catch (System.Exception exIcono)
                    { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroTrioIcono", exIcono); }
                    // …y el NOMBRE cosido debajo: la ranura arma se LEE
                    // con nombre y todo (nada de blanco mudo).
                    string nombreR = ra.Name ?? "";
                    if (nombreR.Length > 0)
                    {
                        float escN = LibroLineas.EscalaQueCabe(nombreR, 0.78f, 60f);
                        LibroLineas.Texto(sb, nombreR,
                            new Vector2(s.Center.X, s.Bottom - 9f), Tinta, escN, 0.5f, 0.5f);
                    }
                }
                else
                    LibroLineas.Texto(sb, (i + 1).ToString(), s.Center.ToVector2(), TintaApagado, 1.0f, 0.5f, 0.5f);"""
assert VIEJO in s, "ancla del dibujo de ranura no hallada"
s = s.replace(VIEJO, NUEVO5, 1)
escribir("Content/UI/LibroDelEternoUI.cs", s)
print("  [ok] ranura blindada + nombre visible")

print("\n[cirugía v6.178] LAS CINCO CIRUGÍAS LISTAS — el oráculo manda a continuación.")
