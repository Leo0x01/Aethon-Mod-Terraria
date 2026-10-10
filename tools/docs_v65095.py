#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.95 — LA CIRUGÍA DE LOS DOCS (CHANGES + SNAPSHOT + README).
Strings EXACTOS con aserciones de conteo — la casa.

Estados: ESTADO='LISTA' (antes de publicar — el ítem 1 de PRÓXIMOS PASOS
nace ⏳) o ESTADO='PUBLICADA' (después — sella con el release ID real).
"""
import io
import sys

ESTADO = sys.argv[1] if len(sys.argv) > 1 else 'LISTA'
RELEASE_ID = sys.argv[2] if len(sys.argv) > 2 else 'PENDIENTE'
ASSET_ID = sys.argv[3] if len(sys.argv) > 3 else 'PENDIENTE'

LETRA = '''**Petición del usuario**: "perfecto ya funciona bien la mecanica del
grimorio hambriento, ahora antes de continuar debes guardar la version
actual como version estable / luego quitar la restriccion del compás de
la sombra — tus dos reglas a la vez: un ataque cada 10 s (marcado al
nacer) y nunca antes de 2 s de terminar el anterior (marcado al morir).
El re-lanzamiento al matar murió. — creo que mejor es dejarlo atacar
cuando quiera / esto debe cambiar: la descarga prestada — el disparo del
jugador sigue siendo el Nightglow vanilla (proyectil de prueba) […]
tengo una idea, te dare un sprite para un disparo, tu has que el
disparo tenga efectos, te dare el codigo como siempre, eso es mejor que
darte la imagen png, quiero que con este proyectil seas creativo, lo
animes y crees una nueva arma con el proyectil".'''

CUERPO = '''1. **LA .94 GUARDADA COMO VERSIÓN ESTABLE** (la letra: «antes de
   continuar debes guardar la version actual como version estable»):
   tag `stable-v6.50.94` + rama `stable-v6.50.94-backup` → `80d4d7b`
   (el commit de la versión — su árbol ES la .94, patrón .81: jamás en
   la punta de main); STABLE-SNAPSHOT con la sección del canal estable
   de la .94 + manifiesto SHA-256 de las 384 entradas; README apuntando
   al retorno seguro. La .94 es la que el usuario PROBÓ: «perfecto ya
   funciona bien la mecanica del grimorio hambriento».
2. **EL COMPÁS DE LA SOMBRA MURIÓ** (la letra: «creo que mejor es
   dejarlo atacar cuando quiera»): `TICKS_ENTRE_ATAQUES` (600 = 10 s),
   `TICKS_TRAS_TERMINAR` (120 = 2 s), `_proximoAtaque`, `MarcarAtaque`,
   `AtaqueListo`, `SombraTerminada` y el `OnKill` que las tocaba —
   **ELIMINADOS** de `SombraPaginaCaza`; el espíritu lanza su sombra
   CUANDO HAY PRESA que se mueva cerca (la única cola es la natural:
   una presa por sombra, `SombraActiva` — y el silencio mientras el
   dueño dispara, `JugadorAtacoReciente`, sigue vivo: eso no era un
   reloj de ataque sino la caza del ausente).
3. **LA DESCARGA PRESTADA MURIÓ — LA LÁGRIMA DE TINTA** (la letra:
   «te dare un sprite para un disparo […] seas creativo, lo animes»):
   el clic izq del grimorio YA NO dispara el Nightglow vanilla (931) —
   escupe **LA LÁGRIMA DE TINTA** (`LagrimaDeTintaProjectile`), nacida
   del SPRITE EXACTO del usuario (607×1344 RLE «sin pérdida · sin
   manipulación», decodificado por `tools/gen_tinta_viva_v65095.py`):
   una página doblada en sombra con corazón de marfil. LA ANIMACIÓN:
   strip de 4 frames 28×76 (`Main.projFrames`) — las puntas laten
   (escala 0.94→1.0 a lo largo) y el corazón se aviva ×1.0→×1.45 por
   frame (el pulso vive en los PÍXELES, el halo aditivo del draw sólo
   lo corona: violeta fuera, marfil dentro). LOS EFECTOS: homing que
   ACELERA (12→19 px/t, giro capado 0.11 rad/t, re-busca cada 8 t),
   EL GOTEO (polvo violeta sin gravedad + gotas que SÍ caen), LAS
   ESTELAS (5 fantasmas por `oldPos` desvaneciéndose) + EL VAIVÉN
   (balanceo perpendicular SOLO en el draw — el hitbox viaja honesto),
   LA LUZ (violeta de sombra + marfil que late con el frame),
   atraviesa MUROS (`tileCollide false`: las sombras no conocen
   puertas), 3 presas por lágrima (`penetrate 3`), y al romperse: la
   SALPICADURA (anillo de tinta) + **LA MANCHA** (`ManchaDeTinta`: el
   charco de `SombrasLib.Charco` que se desvanece 36 t donde la
   lágrima murió — el recuerdo del disparo).
4. **LA NUEVA ARMA — LA TINTA VIVA** (`TintaViva`, la letra: «crees
   una nueva arma con el proyectil»): el arma mágica (90 daño, 9 maná,
   24 useTime, 15.5 shootSpeed, autoReuse) que escupe las lágrimas —
   con su ICONO 44×44 (la lágrima rotada −35° al Lanczos desde la
   fuente, rotar ANTES de reducir) y su tooltip literario ×3 idiomas.
   Coherencia total: si el NERVIOSO anda FUERA, la tinta sale DEL
   LIBRO flotante (`PosicionDelLibro` — la letra .93 manda para toda
   la familia). Entrega: Bolsa de las Sombras + receta de 5 maderas
   (protocolo de pruebas).
5. **BUG .94 CAZADO EN EL TEXTO**: el tooltip del grimorio ORIGINAL
   seguía prometiendo la GRACIA de los 10 s («Si pasan 10 segundos sin
   comer…» + «clic der (y lo calma 10 s)») — esa gracia MURIÓ en la
   .94 (el reloj suma SIEMPRE): el texto ya no miente. Y el tooltip del
   Nervioso ya no promete el compás muerto.
6. **VERIFICACIÓN**: build real 0/0 (308 .cs = 306 + la lágrima y su
   arma) · .tmod 2.577.464 B md5 a52cb68693c5decb4cd45f1baa97fb7a:
   386 entradas (384 de la .94 + LagrimaDeTintaProjectile.rawimg +
   TintaViva.rawimg), EOF exacto, blob diff whitelist exacto (dll/pdb/
   Info + 3 hjson), arte byte-idéntico (378), CECIL (audit_v65095.py,
   0 fallos): el compás ENTERO ELIMINADO + el espíritu sin relojes
   pero con SombraActiva/JugadorAtacoReciente + el Shoot del libro SIN
   el 931 y CON la lágrima + LagrimaDeTintaProjectile con toda su
   máquina (projFrames 4, OnHitNPC/OnKill/BuscarPresa) + ManchaDeTinta
   con el Charco + TintaViva con su Shoot desde el libro + la bolsa
   entregándola + GrimorioFuriaSistema/OleadaNPC IL IDÉNTICOS a la
   .94 · hjson ×3: TintaViva presente, compás y gracia muertos en el
   texto, es-ES espejo por contenido, 787 hojas simétricas es↔en
   (+2 de TintaViva), en-US tabs 4278, paquete == árbol ×3 · headless
   «Sandboxing v6.50.95 → Adding Recipes → Server started»
   0 EXCEPCIONES.'''

PUBLICACION = '''   PUBLICACIÓN: tag v6.50.95 + release {rid} (make_latest) + asset
   AethonMod.tmod {aid} 2.577.464 B (md5 a52cb68693c5decb4cd45f1baa97fb7a),
   CDN VERIFICADO BYTE A BYTE, /releases/latest = v6.50.95, respaldo
   /home/sync/AethonMod-v6.50.95.tmod.'''

CHECKLIST = '''   CHECKLIST DEL USUARIO (la prueba EN JUEGO): (a) el libro ataca
   CUANDO QUIERE: mata una presa y la siguiente sombra sale ENSEGUIDA
   si algo se mueve cerca (sin esperar 10 s ni 2 s); (b) el clic izq
   del grimorio YA NO dispara el Nightglow de hadas: escupe LA LÁGRIMA
   DE TINTA — página oscura con corazón de marfil que RESPIRA (4
   frames), PERSIGUE a la presa (curva y acelera), GOTEA estelas
   violetas, deja ESTELAS de fantasma, pasa MUROS y al romperse
   SALPICA y deja LA MANCHA en el suelo; (c) con el espíritu fuera, la
   lágrima nace DEL LIBRO flotante (y la mano va vacía); (d) LA TINTA
   VIVA llega en la Bolsa de las Sombras (icono diagonal): cada disparo
   es una lágrima (3 presas por lágrima) y si el Nervioso anda fuera,
   TAMBIÉN dispara desde el libro; (e) el tooltip de ambos libros ya no
   promete ni el compás ni la calma de 10 s; (f) la .94 quedó marcada
   como estable: tag stable-v6.50.94 + rama stable-v6.50.94-backup en
   el repo.'''


def cambios():
    titulo = ('## Commit v6.50.95 — EL COMPÁS MURIÓ + LA LÁGRIMA DE TINTA '
              '(el disparo propio) + LA TINTA VIVA + LA .94 ESTABLE\n\n')
    entrada = titulo + LETRA + '\n\n' + CUERPO
    if ESTADO == 'PUBLICADA':
        entrada += '\n' + PUBLICACION.format(rid=RELEASE_ID, aid=ASSET_ID)
    entrada += '\n\n' + CHECKLIST + '\n'
    return entrada


def main():
    # === CHANGES.md: la entrada .95 arriba ===
    ch = io.open('CHANGES.md', encoding='utf-8', newline='').read()
    ancla = '## Commit v6.50.94'
    pos = ch.find(ancla)
    assert pos > 0, 'no encontré la entrada v6.50.94 en CHANGES'
    ch = ch[:pos] + cambios() + '\n' + ch[pos:]
    io.open('CHANGES.md', 'w', encoding='utf-8', newline='').write(ch)
    print(f'CHANGES: entrada v6.50.95 ({ESTADO}) insertada')

    # === STABLE-SNAPSHOT.md ===
    sn = io.open('STABLE-SNAPSHOT.md', encoding='utf-8', newline='').read()

    # el título
    t_viejo = '# AethonMod — ESTADO ACTUAL (v6.50.94)'
    t_nuevo = '# AethonMod — ESTADO ACTUAL (v6.50.95)'
    assert sn.count(t_viejo) == 1
    sn = sn.replace(t_viejo, t_nuevo)

    # el bloque «Última actualización» (del > inicial al cierre del bloque)
    import re
    m = re.search(r'> Última actualización: v6\.50\.94.*?\n(> .*\n)*', sn)
    assert m, 'no encontré el bloque de última actualización'
    # el bloque termina en la línea «headless 0 excepciones.)»
    fin = sn.find('headless 0 excepciones.)')
    assert fin > 0
    fin = sn.find('\n', fin) + 1
    nuevo_bloque = '''> Última actualización: v6.50.95 (EL COMPÁS MURIÓ + LA LÁGRIMA DE
> TINTA. La letra: «luego quitar la restriccion del compás de la
> sombra — tus dos reglas a la vez […] creo que mejor es dejarlo
> atacar cuando quiera / esto debe cambiar: la descarga prestada […]
> te dare un sprite para un disparo, tu has que el disparo tenga
> efectos, te dare el codigo como siempre […] quiero que con este
> proyectil seas creativo, lo animes y crees una nueva arma con el
> proyectil» — la .94 quedó GUARDADA COMO ESTABLE (tag
> stable-v6.50.94 + rama -backup → 80d4d7b, manifiesto SHA-256 de las
> 384 entradas — el punto de retorno); el libro YA NO espera 10 s ni
> 2 s para lanzar su sombra: ataca cuando QUIERE; y el disparo propio
> nació del sprite del usuario: LA LÁGRIMA DE TINTA (4 frames
> respirando, homing que acelera, goteo, estelas, salpicadura, la
> mancha) + LA TINTA VIVA, el arma que la escupe (bolsa de sombras) —
> el Nightglow prestado (931) MURIÓ en el Shoot del libro. .tmod
> 2.577.464 B md5 a52cb68693c5decb4cd45f1baa97fb7a, 386 entradas,
> auditoría TODO OK (audit_v65095.py), headless 0 excepciones.)
'''
    sn = sn[:m.start()] + nuevo_bloque + sn[fin:]

    # el ítem 1 de PRÓXIMOS PASOS
    ancla_pp = '## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n'
    assert sn.count(ancla_pp) == 1
    estado_pp = ('✅ v6.50.95 PUBLICADA — release ' + RELEASE_ID
                 if ESTADO == 'PUBLICADA' else '⏳ v6.50.95 LISTA (pendiente de publicación)')
    item = f'''1. **{estado_pp}** — EL COMPÁS MURIÓ + LA LÁGRIMA DE TINTA +
   LA TINTA VIVA + LA .94 GUARDADA COMO ESTABLE. {LETRA[:0]}{'La letra completa en CHANGES.md v6.50.95.'}
   (1) ESTABLE: tag stable-v6.50.94 + rama stable-v6.50.94-backup →
   80d4d7b + manifiesto SHA-256 (384) + README. (2) SIN RELOJES:
   la sombra sale cuando hay presa (una presa por sombra; el silencio
   mientras el dueño dispara SIGUE — es la caza del ausente, no un
   compás). (3) LA LÁGRIMA: sprite del usuario → strip 4×28×76
   (latido en los píxeles: escala 0.94→1.0 + marfil ×1.0→×1.45) +
   homing acelerante + goteo + estelas oldPos + vaivén de draw + luz
   violeta/marfil + muros abiertos + 3 penetraciones + salpicadura +
   LA MANCHA (Charco 36 t). (4) EL ARMA: TintaViva 90/9/24, icono
   −35°, bolsa + madera; dispara DESDE el libro si el Nervioso está
   fuera. (5) TEXTO HONESTO: el tooltip del original ya no promete la
   gracia muerta de la .92/.94. VERIFICACIÓN: build real 0/0 (308
   .cs) · audit_v65095.py 0 fallos (386 entradas, EOF exacto, blob
   whitelist, CECIL compás muerto + lágrima completa + sistemas
   intactos, hjson ×3 espejo + 787 hojas + tabs 4278) · headless 0
   excepciones.'''
    if ESTADO == 'PUBLICADA':
        item += '\n' + PUBLICACION.format(rid=RELEASE_ID, aid=ASSET_ID)
    item += '\n' + CHECKLIST[len('   '):].replace('   CHECKLIST', '\n   CHECKLIST', 1) if False else ''
    sn = sn.replace(ancla_pp, ancla_pp + item + '\n\n', 1)

    # la fila del HISTORIAL
    ancla_h = '''## 📜 HISTORIAL DE ESTADO (contexto de versiones)
| Versión | Estado | Notas |
|---|---|---|
'''
    assert sn.count(ancla_h) == 1
    pub_h = (f', ✔ PUBLICADA (release {RELEASE_ID} + asset {ASSET_ID} '
             '2.577.464 B, CDN verificado byte a byte md5 a52cb686…, '
             '/releases/latest = v6.50.95)' if ESTADO == 'PUBLICADA'
             else ', ⏳ pendiente de release')
    fila = ('| **v6.50.95** | ✅ Build-verificada (build real 0/0 sobre 308 '
            '.cs = 306 + la lágrima y su arma, .tmod 2.577.464 B md5 '
            'a52cb68693c5decb4cd45f1baa97fb7a, 386 entradas = 384 de la .94 '
            '+ LagrimaDeTintaProjectile.rawimg + TintaViva.rawimg, EOF '
            'exacto, blob diff whitelist exacto (dll/pdb/Info + 3 hjson + '
            '2 rawimg nuevos), arte byte-idéntico en las 378 restantes, '
            'CECIL: compás ENTERO eliminado de SombraPaginaCaza '
            '(TICKS_ENTRE_ATAQUES/TICKS_TRAS_TERMINAR/_proximoAtaque/'
            'MarcarAtaque/AtaqueListo/SombraTerminada/OnKill) + espíritu '
            'sin relojes pero con SombraActiva/JugadorAtacoReciente + el '
            'Shoot del libro SIN el ldc.i4 931 y CON la lágrima + projFrames '
            '4 + OnHitNPC/OnKill/BuscarPresa de la lágrima + ManchaDeTinta '
            'con Charco + TintaViva con Shoot/PosicionDelLibro + la bolsa '
            'entregándola + GrimorioFuriaSistema/OleadaNPC IL IDÉNTICOS a '
            'la .94, hjson ×3 con TintaViva + compás y gracia muertos en '
            'el texto, es-ES espejo por contenido, 787 hojas es↔en, en-US '
            'tabs 4278, paquete == árbol ×3, headless «Sandboxing '
            'v6.50.95 → Adding Recipes → Server started» 0 excepciones)'
            + pub_h + ' | LA LETRA: «luego quitar la restriccion del '
            'compás de la sombra — tus dos reglas a la vez […] creo que '
            'mejor es dejarlo atacar cuando quiera / te dare un sprite '
            'para un disparo […] seas creativo, lo animes y crees una '
            'nueva arma con el proyectil». (1) ESTABLE: la .94 marcada '
            '(tag + rama backup + manifiesto SHA-256). (2) EL COMPÁS '
            'MUERTO: la sombra sale cuando hay presa — sin 10 s ni 2 s. '
            '(3) LA LÁGRIMA DE TINTA: el sprite EXACTO del usuario → strip '
            'de 4 frames (el latido vive en los PÍXELES), homing '
            'acelerante, goteo, estelas, vaivén, muros abiertos, 3 presas, '
            'salpicadura y LA MANCHA. (4) LA TINTA VIVA: el arma (icono '
            '−35°, bolsa, dispara desde el libro). (5) textos honestos. '
            'LECCIONES: (1) el sprite del usuario viaja como CÓDIGO RLE — '
            'decodificar con la casa y bajar al Lanczos (rotar ANTES de '
            'reducir para el icono); (2) un cooldown que molesta al '
            'usuario no se afina: se BORRA — «atacar cuando quiera» es '
            'sin reloj, la cola natural (una presa por sombra) basta; (3) '
            'Dust.NewDustPerfect devuelve Dust (NO int: no se indexa '
            'Main.dust[d]) | ' + '\n')
    sn = sn.replace(ancla_h, ancla_h + fila, 1)

    io.open('STABLE-SNAPSHOT.md', 'w', encoding='utf-8', newline='').write(sn)
    print(f'SNAPSHOT: título v6.50.95 + bloque actualización + ítem 1 ({ESTADO}) + fila historial')

    # === README.md ===
    rd = io.open('README.md', encoding='utf-8', newline='').read()
    v_vieja = '> **Versión actual:** 6.50.94 ·'
    v_nueva = '> **Versión actual:** 6.50.95 ·'
    assert rd.count(v_vieja) == 1
    rd = rd.replace(v_vieja, v_nueva)
    e_vieja = ('> **Versión estable:** 6.50.81 (tag `stable-v6.50.81` + rama '
               '[`stable-v6.50.81-backup`]')
    e_nueva = ('> **Versión estable:** 6.50.94 (tag `stable-v6.50.94` + rama '
               '[`stable-v6.50.94-backup`]')
    assert rd.count(e_vieja) == 1
    rd = rd.replace(e_vieja, e_nueva)
    io.open('README.md', 'w', encoding='utf-8', newline='').write(rd)
    print('README: versión actual 6.50.95 + versión estable 6.50.94')


if __name__ == '__main__':
    main()
