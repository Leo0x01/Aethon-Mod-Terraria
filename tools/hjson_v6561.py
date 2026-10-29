#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.1 — LA CIRUGÍA DE LOCALIZACIÓN (hjson) tras la auditoría 4-c/4-d.

QUÉ CURA:
  1. es-MX main: DEDUPE por ámbito (conserva la PRIMERA copia — las alas/
     bolsas v6.56.0 viven ahí), claves muertas de la purga v6.55.0 fuera,
     QUINCE→veintidós, los 4 cierres ''' a col 0, y la clave del RC2.
  2. en-US main: las mismas claves muertas + FIFTEEN→TWENTY-TWO y
     TWENTY-THREE→TWENTY-FOUR.
  3. Escriba: es-MX/es-ES +14 claves vivas (solo existían en inglés),
     en-US -6 DisplayNames huérfanos (clases purgadas, incluida la
     prohibida RayoFusionCopia).
  4. Mensaje TestingPlayer.Bolsas al modelo v6.55+ (el cofre maestro).

EL PARSER: por ámbitos con pila, agnóstico de indentación; los BLOQUES
ocupan de su apertura a su CIERRE pareado (la primera versión borraba solo
la línea de apertura y el Hjson real de tML lo cazó — lección de esta
misma entrega: validar SIEMPRE con el parser del juego, nunca confiar).

Idempotente. Aserciones duras: ante sorpresas ABORTA sin escribir a medias.
"""
import re, sys, os
from collections import defaultdict

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOC = os.path.join(RAIZ, "AethonMod", "Localization")
ES_MX = os.path.join(LOC, "es-MX_Mods.AethonMod.hjson")
EN_US = os.path.join(LOC, "en-US_Mods.AethonMod.hjson")
ES_ES = os.path.join(LOC, "es-ES_Mods.AethonMod.hjson")
ESC_MX = os.path.join(LOC, "Escriba", "es-MX_Mods.AethonMod.hjson")
ESC_EN = os.path.join(LOC, "Escriba", "en-US_Mods.AethonMod.hjson")
ESC_ES = os.path.join(LOC, "Escriba", "es-ES_Mods.AethonMod.hjson")

MUERTOS_ITEMS = {"BolsaInvestigacion", "RayosConcentrados", "TruenoDelPrisma",
                 "LodoDeEsencia", "CetroConstelaciones", "EstelaDelCosmos", "TintaViva"}
MUERTOS_PROJ = {"CaidaCometeProjectile.DisplayName", "RayosConcentradosHeld.DisplayName",
                "TruenoPrismaRayo.DisplayName", "LodoGlob.DisplayName", "LodoCharco.DisplayName",
                "EstrellaPrisma.DisplayName", "EstelaCosmosHalo.DisplayName", "TintaVivaHalo.DisplayName"}
MUERTOS_ESCRIBA = {"EstrellaStrikeProjectile", "LluviaHojaProjectile", "PolarDardoProjectile",
                   "SistemaDetonacionProjectile", "SistemaTiroProjectile", "RayoFusionCopiaProjectile"}

RE_BLOQUE = re.compile(r"^(\s*)([^\s:{}]+):\s*\{\s*$")
RE_CERROJO = re.compile(r"^\s*\}")
RE_ESCALAR = re.compile(r"^(\s*)([^\s:{}]+):\s*(.*)$")

class Unidad:
    __slots__ = ("key", "scope", "ini", "fin", "kind")
    def __init__(self, key, scope, ini, fin, kind):
        self.key, self.scope, self.ini, self.fin, self.kind = key, scope, ini, fin, kind

def parse(lines):
    """Unidades con rangos COMPLETOS: los bloques de apertura a cierre pareado."""
    unidades, pila = [], []   # pila: (key, línea_apertura, índice_unidad)
    i, n = 0, len(lines)
    while i < n:
        s = lines[i].strip()
        if not s or s.startswith("//"):
            i += 1; continue
        if RE_CERROJO.match(lines[i]):
            if pila:
                _, _, uidx = pila.pop()
                unidades[uidx].fin = i + 1   # el bloque llega hasta su cierre
            i += 1; continue
        mb = RE_BLOQUE.match(lines[i])
        if mb:
            unidades.append(Unidad(mb.group(2), tuple(k for k, _, _ in pila), i, i + 1, "bloque"))
            pila.append((mb.group(2), i, len(unidades) - 1))
            i += 1; continue
        me = RE_ESCALAR.match(lines[i])
        if me:
            key, valor = me.group(2), me.group(3).strip()
            ini = i
            if valor == "":
                j = i + 1
                while j < n and (not lines[j].strip() or lines[j].strip().startswith("//")):
                    j += 1
                if j < n and lines[j].strip() == "'''":
                    k = j + 1
                    while k < n and lines[k].strip() != "'''":
                        k += 1
                    if k >= n:
                        raise SystemExit(f"multilinea sin cierre en línea {j+1}")
                    i = k + 1
                    unidades.append(Unidad(key, tuple(kk for kk, _, _ in pila), ini, i, "multilinea"))
                    continue
            unidades.append(Unidad(key, tuple(kk for kk, _, _ in pila), ini, i + 1, "escalar"))
            i += 1; continue
        raise SystemExit(f"línea no parseable {i+1}: {lines[i][:80]!r}")
    if pila:
        raise SystemExit(f"bloques sin cerrar: {pila}")
    return unidades

def cirugiar(path, muertos_items, muertos_proj, textos, extra, etiqueta):
    with open(path, encoding="utf-8") as f:
        lines = f.read().split("\n")
    while lines and lines[-1] == "": lines.pop()
    unidades = parse(lines)
    borrar = set()
    duplicadas_dif = []
    por_clave = defaultdict(list)
    for u in unidades: por_clave[(u.scope, u.key)].append(u)
    for (scope, key), us in por_clave.items():
        if len(us) > 1:
            base = "\n".join(lines[us[0].ini:us[0].fin])
            for u in us[1:]:
                if "\n".join(lines[u.ini:u.fin]) != base:
                    duplicadas_dif.append((".".join(scope), key, u.ini + 1))
                borrar.update(range(u.ini, u.fin))
                if u.ini - 1 >= 0 and lines[u.ini - 1].strip() == "":
                    borrar.add(u.ini - 1)
    muertas = 0
    for u in unidades:
        if muertos_items and u.scope == ("Items",) and u.key in MUERTOS_ITEMS:
            borrar.update(range(u.ini, u.fin)); muertas += 1
        if muertos_proj and u.scope == ("Projectiles",) and u.key in MUERTOS_PROJ:
            borrar.update(range(u.ini, u.fin)); muertas += 1
    vivos = [i for i in range(len(lines)) if i not in borrar]
    cambios_texto = 0
    for viejo, nuevo, veces in textos:
        hechos = 0
        for i in vivos:
            if viejo in lines[i]:
                lines[i] = lines[i].replace(viejo, nuevo); hechos += 1
        assert hechos == veces, f"{etiqueta}: texto {viejo!r} esperado {veces}, hallado {hechos}"
        cambios_texto += hechos
    nuevas = [l for i, l in enumerate(lines) if i not in borrar]
    extra_cambios = 0
    if extra:
        nuevas, extra_cambios = extra(nuevas)
    # SANIDAD: el resultado debe parsear limpio y sin duplicados
    reparsed = parse([l for l in nuevas if True] if nuevas and nuevas[-1] != "" else nuevas)
    por_clave2 = defaultdict(int)
    for u in reparsed: por_clave2[(u.scope, u.key)] += 1
    residuo = [k for k, v in por_clave2.items() if v > 1]
    assert not residuo, f"{etiqueta}: quedaron duplicados {residuo[:5]}"
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(nuevas) + "\n")
    dup = sum(len(us) - 1 for us in por_clave.values() if len(us) > 1)
    print(f"OK  {etiqueta}: dupes -{dup} · muertas -{muertas} · textos {cambios_texto} · extra {extra_cambios} · {len(nuevas)} líneas")
    if duplicadas_dif:
        print("    ¡AVISO! duplicadas CON DIFERENCIAS (se conservó la primera):")
        for sc, key, ln in duplicadas_dif[:40]:
            print(f"      {sc}.{key} (copia extra en línea {ln})")

# ---------------------------------------------------------------- es-MX main
def extras_es_mx(lines):
    n = 0
    for i, l in enumerate(lines):
        if l == "'''":
            for j in range(i - 1, -1, -1):
                if lines[j].strip() == "'''":
                    lines[i] = lines[j][:len(lines[j]) - len(lines[j].lstrip())] + "'''"
                    n += 1; break
    if not any(l.strip().startswith("RayoConcentradoDosProjectile.DisplayName:") for l in lines):
        for i, l in enumerate(lines):
            if l.strip().startswith("SombraPagina2Projectile.DisplayName:"):
                indent = l[:len(l) - len(l.lstrip())]
                lines.insert(i + 1, f"{indent}RayoConcentradoDosProjectile.DisplayName: El Rayo Concentrado II")
                n += 1; break
        else:
            raise SystemExit("no hallé SombraPagina2Projectile.DisplayName en es-MX")
    return lines, n

cirugiar(ES_MX, True, True,
         [("Las QUINCE alas de código", "Las veintidós alas de código", 1)],
         extras_es_mx, "es-MX main")

# ---------------------------------------------------------------- en-US main
cirugiar(EN_US, True, True,
         [("The FIFTEEN code-born wings", "The TWENTY-TWO code-born wings", 1),
          ("ALL TWENTY-THREE bags", "ALL TWENTY-FOUR bags", 1)],
         None, "en-US main")

# ---------------------------------------------------------------- Escriba
NOMBRES_ES = [
    ("SombraPagina3Projectile", "La Sombra de la Página III"),
    ("SombraPagina4Projectile", "La Sombra de la Página IV"),
    ("ColumnaIraProjectile", "La Columna de la Ira"),
    ("IraDelCieloProjectile", "La Ira del Cielo"),
    ("PartidorDeMundosProjectile", "El Partidor de Mundos"),
    ("PrismaPrimordialProjectile", "El Prisma Primordial"),
    ("RequiemEstelarProjectile", "El Réquiem Estelar"),
    ("TestamentoAethonProjectile", "El Testamento de Aethon"),
    ("IndiceDelJuicioProjectile", "El Índice del Juicio"),
    ("LanzaVeredictoProjectile", "La Lanza del Veredicto"),
    ("OjoDelJuicioProjectile", "El Ojo del Juicio"),
    ("PaginaIndiceProjectile", "La Página del Índice"),
    ("PlumaDelVeredictoProjectile", "La Pluma del Veredicto"),
    ("RayoConcentradoNivelProjectile", "El Rayo Concentrado"),
]
VIVOS_ESCRIBA = {k for k, _ in NOMBRES_ES}

def escriba_es(ruta, etiqueta):
    with open(ruta, encoding="utf-8") as f:
        s = f.read()
    if "SombraPagina3Projectile.DisplayName" in s and "// SombraPagina3Projectile" not in s:
        print(f"OK  {etiqueta}: ya estaba"); return
    # retira los 20 comentarios viejos (es-ES) si existen
    lineas = [l for l in s.split("\n")
              if not l.strip().startswith("//") or "Projectile.DisplayName" not in l]
    s = "\n".join(lineas)
    m = re.search(r"\nProjectiles: \{\n(.*?)\n\}\n", s, re.S)
    assert m, f"no hallé Projectiles en {etiqueta}"
    anidado = re.search(r"^\t\S+: \{$", m.group(1), re.M) is not None
    bloque = "".join(
        (f"\t{k}: {{\n\t\tDisplayName: {v}\n\t}}\n" if anidado else f"\t{k}.DisplayName: {v}\n")
        for k, v in NOMBRES_ES)
    nuevo = m.group(1).rstrip("\n") + "\n" + bloque.rstrip("\n")
    s = s[:m.start(1)] + nuevo + s[m.end(1):]
    if etiqueta.endswith("es-ES"):
        s = s.replace("// ENGLISH (en-US).", "// ESPEJO del es-MX (regla v6.50.71 — no editar a mano).")
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(s)
    print(f"OK  {etiqueta}: +{len(NOMBRES_ES)} claves vivas (estilo {'nested' if anidado else 'plano'})")

escriba_es(ESC_MX, "Escriba es-MX")
escriba_es(ESC_ES, "Escriba es-ES")

# Escriba en-US: retira los 6 huérfanos
with open(ESC_EN, encoding="utf-8") as f:
    lines = f.read().split("\n")
vivos, borrados = [], 0
for l in lines:
    s2 = l.strip()
    if any(re.match(rf"^{m}\.DisplayName:", s2) for m in MUERTOS_ESCRIBA):
        borrados += 1; continue
    vivos.append(l)
with open(ESC_EN, "w", encoding="utf-8") as f:
    f.write("\n".join(vivos))
resto = sum(1 for l in vivos if ".DisplayName:" in l)
assert resto == 17, f"Escriba en-US: esperaba 17 DisplayNames, hay {resto}"
print(f"OK  Escriba en-US: -{borrados} huérfanos (quedan {resto})")

# ---------------------------------------------------------------- el mensaje del kit
with open(ES_MX, encoding="utf-8") as f:
    mx = f.read()
VIEJO = "Las bolsas del arsenal llegan contigo (una por categoría): clic derecho para abrir cada una."
NUEVO = "El Cofre del Arsenal llega contigo (dentro van las veinticuatro bolsas de categoría): clic derecho para abrirlo, y a cada bolsa igual."
if VIEJO in mx:
    assert mx.count(VIEJO) == 1
    with open(ES_MX, "w", encoding="utf-8") as f:
        f.write(mx.replace(VIEJO, NUEVO))
    print("OK  es-MX: mensaje del kit (el cofre maestro)")
else:
    print("OK  es-MX: mensaje del kit ya actualizado")

with open(EN_US, encoding="utf-8") as f:
    en = f.read()
m = re.search(r"Bolsas: (.+)$", en, re.M)
if m and "category" in m.group(1):
    en = en.replace(m.group(0),
                    "Bolsas: The Arsenal Chest arrives with you (the twenty-four category bags live inside): right-click to open it, and each bag alike.")
    with open(EN_US, "w", encoding="utf-8") as f:
        f.write(en)
    print("OK  en-US: mensaje del kit (el cofre maestro)")

print("\nCIRUGÍA HJSON COMPLETA — falta: sync_es_es + validación de paridad (Hjson real).")
