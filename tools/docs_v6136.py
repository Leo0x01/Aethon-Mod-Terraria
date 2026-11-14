#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.136 — LA ACTUALIZACIÓN DE LA DOCUMENTACIÓN (la letra: «Actualiza los
.md del proyecto completo, crea un md maestro…»). MAESTRO.md y README.md
ya se escribieron a mano; este script hace el resto:

  (1) STABLE-SNAPSHOT.md — REESCRIBE completo a v6.135
  (2) COMPILACION.md — actualiza versión y tML a 2026.09.3.1 donde esté viejo
  (3) EFECTOS-Y-LIBRERIAS.md — banner de vigencia (78 librerías v6.135)
  (4) CARACTERISTICAS.md / DISEÑO_DEL_MOD.md / HOJA-DE-RUTA.md /
      ANALISIS-1.0.md / EL-PIPELINE-DE-CREACION.md / LA-SUPERFORJA.md /
      AUDITORIA-LIBRERIAS.md / PROMPT-MUNDO-INTERIOR.md — banner
      «doc histórica — manda MAESTRO.md» bajo el título
"""
import re

BASE = '/home/z/my-project/mod/'

# ============ (1) STABLE-SNAPSHOT.md — completo, a v6.135 ============
snapshot = '''# SNAPSHOT ESTABLE — v6.135 — EL ARCÁNGEL DE 6 ALAS Y LAS SEIS MITOLÓGICAS

> **PUBLICADA — [release 445558359](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.135) + CDN verificado byte a byte (md5 `6d378f1c5d9a6fdfe5281785bb54e917`, 19.367.611 B, 526 entradas)**

## LA LETRA (el usuario — la sesión 16)

«necesitamos mejorar la estetica de las alas, ahora que tienes como hacerlas correctamente con las alas runicas de vacio, quiero que modifiques esas alas teniendo en cuenta las alas de arcangeles superiores que tiene 3 pares de alas o sea 6 alas y hacer otras versiones de las alas runicas del vacio basandote en otras mitologias o creaciones y tipos de alas» + «continua y crea mas tipos de alas»

## LA CORONA DEL ARCÁNGEL — EL TERCER PAR

Las **Runas del Vacío** ganan el par superior (el campo `Coronas`, escala 0,72): el abanico nace **sobre la espalda** (16·escala px más arriba), **alzado** (inclinación −0,18: abre hacia arriba) y con el aleteo más abierto. El orden de dibujo quedó **corona → alitas → principal** (de atrás hacia adelante). El Vacío conserva su hoja lisa v6.134 INTACTA: **el Vacío es EL ARCÁNGEL, 6 alas en tres niveles.**

## LAS SEIS MITOLÓGICAS — estilos 6..11 del catálogo (ahora 12)

Todas nacen del Vacío v6.134 (la geometría exacta del Eclipse + el ala lisa), cada una con SU alma (paleta · runas · ruido · plumas · curva · pares):

| # | Alas | Mito | El alma | Pares |
|---|---|---|---|---|
| 6 | Serafín del Vacío | hebrea | fuego violeta que no quema, la llama se encorva (curva 0,34) | 3 (6 alas) |
| 7 | Valquiria Rúnica | nórdica | 7 plumas de lanza rectas, plata de batalla, futhark solar | 1 |
| 8 | Quetzal Emplumado | azteca | la serpiente de jade que ondea (curva 0,42 — la mayor del catálogo) | 3 (6 alas) |
| 9 | Fénix del Renacer | griega | 3 llamas anidadas (blanco→fuego→brasa), el tono ARDE a lo largo (hueLargo 0,22), flap 1,15 | 3 (6 alas) |
| 10 | Tengu del Crepúsculo | japonesa | 7 plumas primarias de tinta azul-negra (filosa 0,55), glifos ámbar del abismo | 1 |
| 11 | Dragón del Vacío | membrana | la única SIN abanico: la vela de DOS DEDOS (Plumas=2, punta 24, vientre 0,62) en escamas rojo-vino | 3 (6 alas) |

**Los items**: 6 clases nuevas (`Content/Items/Alas/AlasMitologicasV6135.cs`) + 6 sprites 30×30 · **La Bolsa de las Alas pasa de 5 a 11 alas** · hjson ×3 (es-ES · es-MX · en-US) con **paridad de claves 1691/1691/1691**.

## PARA VERIFICAR

- Oráculo verify.csproj **0 errores / 0 warnings**
- **-build REAL 0/0** (526 entradas, 19.367.611 B, md5 `6d378f1c5d9a6fdfe5281785bb54e917`)
- **GUARDIÁN de texturas: TODO VIVE ✔**
- **AUDIT v6.135 TODO VERIFICADO ✔** — 40 checks: EOF exacto · 526 blobs · hjson == fuente ×3 · la geometría v6.133 intacta · la receta lisa v6.134 sin cambio · el arcángel (coronas 0.72 en el Vacío) · los seis mitos con su paleta y pares · las clases y los sprites (.rawimg) · el catálogo a 12 · la no-regresión de las purgas · paridad 1691³
- **Sim 1:1** (`tools/sim_alas_v6135.py`, parámetros leídos del .cs REAL): tres anclas (corona −14,4 px · espalda · alitas +14,4 px) · la corona alzada 9,2° sobre el principal · el espejo con la fórmula del motor (Δ 0,000 px en los TRES pares) · los seis paneles con SU alma. **QA VLM**: zoom del panel confirmó los tres pares con orígenes distintos + los seis colores diferenciados (el primer VLM a escala pequeña dio falso negativo — lección aplicada: zoom antes de fallar).
- **headless**: Sandboxing v6.135, **0 excepciones**

## PENDIENTE PARA LA PRÓXIMA SESIÓN

1. **LA DECISIÓN DE LAS INVESTIGACIONES** — ver `ANALISIS-REFERENCIAS-EXTERNAS.md` (la carpeta `Content/Weapons/Investigaciones/` con las recreaciones de Calamity Entropy: el usuario debe decidir su destino antes de cualquier release pública).
2. **Verificación en juego** de las seis mitológicas por el usuario (el sim y el QA VLM aprobado, pero la palabra final es del jugador).
3. Doc-rot conocida: `CARACTERISTICAS.md` (v6.50.30) y `DISEÑO_DEL_MOD.md` (v6.50.61) están desactualizados en los conteos — se reconstruirán en una pasada futura; MAESTRO.md lleva los datos vigentes.
'''
open(BASE + 'STABLE-SNAPSHOT.md', 'w', encoding='utf-8').write(snapshot)
print('(1) STABLE-SNAPSHOT.md → v6.135')

# ============ (2) COMPILACION.md — versiones viejas ============
P = BASE + 'COMPILACION.md'
s = open(P, encoding='utf-8').read()
n = 0
# tML viejo → 2026.09.3.1
for viejo in ('2026.07.3.0', '2026.7.3', '2026.07'):
    if viejo in s:
        s = s.replace(viejo, '2026.09.3.1')
        n += 1
# versión del mod si aparece vieja
s = re.sub(r'versión actual:?\s*6\.\d+(\.\d+)*', 'versión actual: 6.135', s, flags=re.I)
open(P, 'w', encoding='utf-8').write(s)
print(f'(2) COMPILACION.md — {n} referencias de tML actualizadas + versión')

# ============ (3)+(4) banners de vigencia ============
BANNER_HIST = '''
> 📜 **DOC HISTÓRICA** — Este documento describe una era anterior del
> proyecto. Para el estado vigente y las reglas actuales manda
> **[MAESTRO.md](MAESTRO.md)**; esta copia se conserva como referencia.
'''
BANNER_VIG = '''
> ✅ **VIGENTE como referencia de detalle** (el índice vivo y las reglas
> actuales están en **[MAESTRO.md](MAESTRO.md)** — actualizada a v6.135).
'''

hist = ['CARACTERISTICAS.md', 'DISEÑO_DEL_MOD.md', 'HOJA-DE-RUTA.md',
        'ANALISIS-1.0.md', 'EL-PIPELINE-DE-CREACION.md', 'LA-SUPERFORJA.md',
        'AUDITORIA-LIBRERIAS.md', 'PROMPT-MUNDO-INTERIOR.md']
for f in hist:
    P = BASE + f
    s = open(P, encoding='utf-8').read()
    if 'MAESTRO.md' in s[:600]:
        print(f'(4) {f} — banner ya presente, se salta')
        continue
    m = re.search(r'^# .+$', s, re.M)
    if not m:
        print(f'(4) {f} — SIN título H1, se salta')
        continue
    s = s[:m.end()] + '\n' + BANNER_HIST + s[m.end():]
    open(P, 'w', encoding='utf-8').write(s)
    print(f'(4) {f} — banner histórico añadido')

# EFECTOS: banner de vigencia + conteo 78
P = BASE + 'EFECTOS-Y-LIBRERIAS.md'
s = open(P, encoding='utf-8').read()
if 'MAESTRO.md' not in s[:700]:
    m = re.search(r'^# .+$', s, re.M)
    s = s[:m.end()] + '\n' + BANNER_VIG + s[m.end():]
    open(P, 'w', encoding='utf-8').write(s)
    print('(3) EFECTOS-Y-LIBRERIAS.md — banner de vigencia añadido')
else:
    print('(3) EFECTOS-Y-LIBRERIAS.md — ya tiene banner')

print('DOCS ACTUALIZADAS ✔')
