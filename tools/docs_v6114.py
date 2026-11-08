#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v6114.py — LA DOC DE LA v6.114: CHANGES.md + README + STABLE-SNAPSHOT."""
import subprocess, datetime, re

COMMIT = subprocess.run(['git','rev-parse','--short','HEAD'],capture_output=True,text=True).stdout.strip()
FECHA = subprocess.run(['git','log','-1','--format=%ci'],capture_output=True,text=True).stdout.strip()[:19]
MSG = subprocess.run(['git','log','-1','--format=%B'],capture_output=True,text=True).stdout.strip()
ARCHIVOS = subprocess.run(['git','diff-tree','--no-commit-id','--name-only','-r','HEAD'],capture_output=True,text=True).stdout.strip().split('\n')

# ===== 1. CHANGES.md =====
entrada = f"""## Commit {COMMIT} — {MSG.splitlines()[0]}
- Commit: {COMMIT}
- Fecha: {FECHA}
- Mensaje: {MSG}
- Archivos modificados:
"""
for a in ARCHIVOS:
    entrada += f"  - {a}\n"

src = open('CHANGES.md', encoding='utf-8').read()
src = src.replace('# AethonMod — Historial de Cambios\n\n',
                  '# AethonMod — Historial de Cambios\n\n' + entrada, 1)
open('CHANGES.md','w',encoding='utf-8').write(src)
print('CHANGES.md OK')

# ===== 2. README =====
src = open('README.md', encoding='utf-8').read()
src = src.replace('**Versión actual:** 6.113', '**Versión actual:** 6.114')
open('README.md','w',encoding='utf-8').write(src)
print('README OK')

# ===== 3. STABLE-SNAPSHOT.md =====
cabecera = """# SNAPSHOT ESTABLE — v6.114 — LA MEMBRANA DEL VACÍO + EL MUNDO INTERIOR DEL ETERNO + LA CEREMONIA DE LA LLAMADA + LA FORMA ETERNA

> ⏳ **PENDIENTE DE PUBLICAR** — .tmod v6.114 (11.502.465 B, 489 entradas, EOF exacto, md5 943d7802b78462ce8b11fc5216e1106f) — **LA CURA DE LA SIERRA** (la letra: «las alas runicas del vacio siguen teniendo picos de sierra» — la v6.113 redondeó cada pluma pero el CONTORNO seguía siendo CINCO tiras separadas por valles; LA CURA: LA MEMBRANA (el velo entre plumas + EL ARCO exterior de Bezier: el borde es UNA curva continua y convexa) + LA PUNTA REDONDA (el hombro del plectro, no la aguja) + la niebla corriendo por dentro del ala) · **EL MUNDO INTERIOR DEL GRIMORIO DEL ETERNO** (la letra: «investigues como crear mundos separados… estara lleno de bruma y de momento se puede entrar usando el altar» — la mecánica aprendida de SubworldLibrary + TODO el código de la casa: entrar por EL ALTAR con EL GRIMORIO EN LA MANO, 800×400 de islas de amatista encendida, la medianoche eterna, EL VELO DE BRUMA con EL OJO COLOSAL que parpadea y sigue al jugador, el silencio de las fieras, la música del hambre, el CERROJO que bloquea TODO guardado mientras dentro, salir por el altar de la plaza, la red de caída que devuelve a la plaza — SP por ahora) · **LA CEREMONIA DE LA LLAMADA** (la letra: «en la llamada debe aparecer el altar y justo encima debe estar el grimorio del eterno» — EL NOMBRE DE AETHON materializa el altar delante del portador con el grimorio flotando JUSTO ENCIMA rodeado del anillo de seis runas doradas; a los 130 t el libro se abre y nacen los dos fragmentos con la fórmula de la Emperatriz) · **LA FORMA ETERNA** (la letra: «es momento de crear el item cosmetico definitivo» — el UNO QUE VISTE LOS OCHO: las dos luces + vuelo infinito, la brasa, el anillo dorsal, el brillo solar, los dos sellos con sus stats y el aura del vacío; + LA BOLSA DE LA FORMA ETERNA con los ocho + el definitivo, entregada en el kit) — VERIFICACIÓN: oráculo 0/0 (441 .cs) · -build REAL 0/0 ×3 · GUARDIÁN 510 clases 0 faltantes · AUDIT v6.114 TODO VERIFICADO (489 entradas, EOF exacto, EMPAQUETADO==FUENTE ×3, las 15 firmas nuevas VIVAS, los 2 iconos rawimg 30×30, keysets 319=319=319, custodia repo==ModSources) · headless 0 EXCEPCIONES con el hook del guardado instalado limpio. LA SESIÓN EMPEZÓ EN DESASTRE: el sandbox fue restaurado a un snapshot del 6 de octubre (la era v6.50.65) y TODO hasta la v6.113 vivía SOLO en GitHub — recuperado con fetch + reset duro a origin/main (03fb27fe) y el entorno renacido (SDK + tML + oráculo + ModSources).

## LA LETRA (el usuario)

«las alas runicas del vacio siguen teniendo picos de sierra, debes arreglar eso / tambien quiero que investigues como crear mundos separados, creo que una vez se investigo eso, necesito crear un mundo nuevo que sera el mundo intenrior del grimorio del eterno, estara lleno de bruma y de momento se puede entrar usando el altar, ademas en la llamada debe aparecer el altar y justo encima debe estar el grimorio del eterno / es momento de crear el item cosmetico definitivo para este se tiene que usar los siguiente accesorios, asi que con esto tienes que hacer un unico accesorio, los accesorios mencionados se deben gaurdar en una nueva bolsa junto con el accesorio definitivo: la brasa del eclipse · el anillo runico dorsal · el sello del genesis · los anillos del sol runico · el brillo de los soles runicos · la forma ascendida · la forma ascendida 8 · aura del vacio»

## PARA VERIFICAR EN EL JUEGO

"""
src = open('STABLE-SNAPSHOT.md', encoding='utf-8').read()
# quitar el título y el bloque viejos (todo hasta el primer '## ' después de la letra vieja... simple: reemplazar hasta LA LETRA vieja)
idx = src.find('## LA LETRA')
fin_letra = src.find('## ', src.find('## PARA VERIFICAR'))
src = cabecera + src[fin_letra:]
open('STABLE-SNAPSHOT.md','w',encoding='utf-8').write(src)
print('STABLE-SNAPSHOT OK')
