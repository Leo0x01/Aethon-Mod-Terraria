#!/usr/bin/env python3
"""v6.50.91 — actualización de docs (CHANGES + STABLE-SNAPSHOT + README)."""
import io

# ============ CHANGES.md ============
CH = 'CHANGES.md'
t = io.open(CH, encoding='utf-8').read()
entrada = '''# AethonMod — Historial de Cambios

## Commit v6.50.91 — EL NERVIOSO CAZA POR SU CUENTA: LA CAZA, LA FUGA Y EL CÓDICE VIVO MUERE

**Petición del usuario**: "no es que este nervioso por tener miedo, esta mas
bien intranquilo, hambriento, enojado, inquieto, quiero comer y tiene hambre
[…] cuando su hambre llegue a 100, hagamos que escape del jugador algo asi
a como se usa El Codice Vivo […] El Codice Vivo lo puedes borrar por
completo ya no es necesario […] si tiene hambre y el jugador deja de moverse
por ejemplo 5 segundos ahora que es una prueba, el grimorio sale y comienza
a flotar encima del jugador mirando al jugador hasta que algo se mueva cerca
de el, cualquier criatura no hostil u hostil sera atacada por el libro,
todas menos los NPC que viven en las casas […] del libro sale La Sombra de
la Pagina haciendo un 10% de daño a la criatura hasta que la mata y la
absorbe, esta muerte no cuenta como que la hizo el jugador y la criatura no
deja loot, pero si cuenta como exp […] ademas hacer que el
ACTUALIZAR-FUENTE.bat que esta dentro del proyecto primero borre la version
anterior para copiar la nueva version".

1. **NO ES MIEDO — ES HAMBRE**: el temperamento del Nervioso se relee tal
   como pidió el usuario: UN APETITO CON CUERPO (intranquilo, hambriento,
   enojado). La familia queda con CUATRO TEMPERAMENTOS: normal SERENO,
   Errático CAÓTICO, Nervioso HAMBRIENTO, Inestable POSEÍDO (su aura roja
   de firma NO se hereda).
2. **LAS DOS VIDAS DEL NERVIOSO (el umbral es el 50%)**:
   - **DEBAJO DEL 50% — EL CAZADOR**: tranquilo y paciente — SIN temblor,
     SIN glitch, SIN sobresaltos (los efectos ahora nacen AL 50% y escalan
     al 100% vía `HambreEfectos`; el ojo ansioso se enciende al cruzar
     `NerviosoActivo()`, debajo corre el ciclo SERENO del original). Si el
     jugador queda QUIETO 5 s (valor de PRUEBA), el libro SALE y flota
     encima de él MIRÁNDOLO (`GrimorioNerviosoFlotante`: la levitación del
     difunto Códice Vivo — muelle suave + vaivén + halo violeta, el ojo
     clavado en su dueño). Cuando algo SE MUEVE cerca (≤480 px — cualquier
     criatura hostil o no, MENOS los NPC que viven en las casas), del
     libro sale **LA SOMBRA DE LA PÁGINA** (`SombraPaginaCaza`: la columna
     viva de verlet NACIENDO DEL LIBRO, la masa negra, los ojos que miran,
     la bruma y EL DEVORADOR cubriendo a la presa) y muerde: **10% de la
     vida MÁXIMA por golpe** cada 30 t (diez golpes) hasta matarla y
     **ABSORBERLA** — sin loot, sin gore, sin crédito del jugador (la
     criatura se APAGA bajo la sombra: `checkDead` jamás corre; los jefes
     multi-segmento caen EN CADENA); la **XP SÍ cobra** (el pipeline de la
     casa al Grimorio del Eterno visible); el hambre **baja 1% por presa**
     (50% → 49%) y el libro CAZA HASTA EL 0% — el reloj del hambre se
     CONGELA mientras patrulla (`EstadoGrimorio.PasoSinHambre`: el ojo
     vive, el apetito no avanza).
   - **DEL 50% EN ADELANTE — EL IMPACIENTE**: «el libro se vuelve inquieto
     y muy hambriento, comienza a decir que tiene hambre» — el libro
     HABLA (`FrasesDeHambre`: «Tengo hambre…» → «¡Quiero comer. ¡YA!» →
     «¡HAMBRE! ¡HAMBRE! ¡HAMBRE!», cada vez más seguido y más desesperado,
     en la pantalla del dueño) y **comienzan las PROBABILIDADES de las
     OLEADAS DE HAMBRE** (4% → 20% cada 5 s según la urgencia,
     `GrimorioFuriaSistema.Provocar` natural con el nivel del portador).
   - **AL 100% — EL FUGITIVO**: «que escape del jugador» — el libro se
     desprende y HUYE flotando (modo fuga: guarda 380–640 px de
     distancia, quiebres nerviosos cada 30–55 t, sesgo hacia arriba, ojo
     ROJO al máximo, cuerpo ROTO en tiras — todo el repertorio) hasta que
     lo alimenten o lo reinicien (clic derecho).
3. **EL CÓDICE VIVO BORRADO POR COMPLETO**: ítem + 2 proyectiles + 2
   rawimg + sus registraciones (la animación del ítem en
   `PostSetupContent`, la entrada de la Bolsa del Probador) + localización
   ×3. Su levitación — el libro que se suelta y vela flotando — vive ahora
   en el ESPÍRITU del Grimorio Nervioso (caza y fuga).
4. **ACTUALIZAR-FUENTE.bat / actualizar-fuente.sh**: ahora PRIMERO BORRAN
   la versión anterior de `ModSources\\AethonMod` antes de copiar la nueva
   (rmdir + robocopy /MIR y rm -rf) — copia LIMPIA, sin archivos muertos
   de versiones viejas colgando del compilador (como los del difunto).
5. **VERIFICACIÓN**: oráculo 0/0 (308 .cs) · build real 0/0 · .tmod
   2.564.967 B md5 `ce511c4ea682c4012603fe390a0ad537`: 386 entradas
   (388 de la .90 − 2 rawimg del Códice), EOF exacto, blob diff whitelist
   exacto, arte byte-idéntico, CECIL: los 3 difuntos AUSENTES + los 2
   nuevos presentes + las 11 firmas del Nervioso + Errático/Inestable/
   clase-ítem-base con IL IDÉNTICO a .90 + EstadoGrimorio documentado ·
   hjson: es-ES espejo de es-MX por CONTENIDO, 789 hojas simétricas
   es↔en, en-US con sus tabs · headless «Sandboxing v6.50.91 → menú de
   mundos» 0 excepciones · EL 19º INCIDENTE DEL ESPEJO cazado por la
   auditoría (la herramienta de edición normalizó tabs→espacios el en-US
   entero — cura: restaurar de git + parche byte-exacto por script).

'''
assert t.startswith('# AethonMod — Historial de Cambios\n\n')
t = entrada + t[len('# AethonMod — Historial de Cambios\n\n'):]
io.open(CH, 'w', encoding='utf-8', newline='').write(t)
print('CHANGES.md: entrada .91 insertada')

# ============ STABLE-SNAPSHOT.md ============
SN = 'STABLE-SNAPSHOT.md'
t = io.open(SN, encoding='utf-8').read()

viejo_titulo = '# AethonMod — ESTADO ACTUAL (v6.50.90)'
nuevo_titulo = '# AethonMod — ESTADO ACTUAL (v6.50.91)'
assert t.count(viejo_titulo) == 1
t = t.replace(viejo_titulo, nuevo_titulo)

viejo_ult = ('> Última actualización: v6.50.90 (EL GRIMORIO NERVIOSO: el tercer '
             'asiento cambia de dueño — temblor + glitch + nervios; el Tembloroso BORRADO)')
nuevo_ult = ('> Última actualización: v6.50.91 (EL NERVIOSO CAZA POR SU CUENTA: '
             'las dos vidas del umbral 50% — el CAZADOR que absorbe presas con '
             'La Sombra de la Página y el IMPACIENTE que habla y provoca oleadas; '
             'LA FUGA al 100%; EL CÓDICE VIVO BORRADO)')
assert t.count(viejo_ult) == 1, 'línea de última actualización no encontrada'
t = t.replace(viejo_ult, nuevo_ult)

fila = ('| **v6.50.91** | ✅ Build-verificada (oráculo 0/0 sobre 308 .cs, build real 0/0, '
        '.tmod 2.564.967 B md5 ce511c4ea682c4012603fe390a0ad537, 386 entradas = 388 de .90 − '
        '2 rawimg del Códice Vivo, EOF exacto, blob diff whitelist exacto (dll/pdb/Info + '
        '3 hjson + los 2 rawimg salientes), arte byte-idéntico, CECIL: '
        'CodiceVivo/CodiceVivoProjectile/CodiceVivoChispa AUSENTES + '
        'GrimorioNerviosoFlotante/SombraPaginaCaza presentes + las 11 firmas del Nervioso '
        '(PasoCaza/AbsorberPresa/HambreEfectos/FrasesDeHambre/DecirLocal/CobrarXPLibro/'
        'LibroFuera/CaceriaActiva/UpdateInventory/ModifyTooltips/get_EstadoCompartido + '
        'constantes 0.5/300/0.01) + Errático/Inestable/GrimorioHambriento(clase ítem) con IL '
        'IDÉNTICO a .90 + EstadoGrimorio CAMBIA documentado (NerviosoActivo + PasoSinHambre '
        '+ las 3 ramas del ojo), espejo es-ES==es-MX por CONTENIDO (789 hojas simétricas '
        'es↔en), en-US con tabs, headless «Sandboxing v6.50.91 → Adding Recipes → menú de '
        'mundos» 0 excepciones), ⏳ PUBLICACIÓN (falta token GitHub — commit+tag locales, '
        'respaldo /home/sync/AethonMod-v6.50.91.tmod), ⏳ en juego | LA LETRA: «no es que '
        'este nervioso por tener miedo, esta mas bien intranquilo, hambriento, enojado, '
        'inquieto, quiero comer y tiene hambre […] cuando su hambre llegue a 100, hagamos '
        'que escape del jugador algo asi a como se usa El Codice Vivo […] El Codice Vivo lo '
        'puedes borrar por completo ya no es necesario». LAS DOS VIDAS DEL NERVIOSO '
        '(umbral 50%): EL CAZADOR (quieto 5 s → el libro flota encima MIRANDO al jugador; '
        'algo se mueve ≤480 px → LA SOMBRA DE LA PÁGINA muerde 10% de la vida máxima por '
        'golpe hasta ABSORBER: sin loot/gore/crédito, XP sí, −1% hambre por presa, hasta el '
        '0% con el reloj CONGELADO — PasoSinHambre), EL IMPACIENTE (≥50%: efectos desde '
        'cero vía HambreEfectos + el libro HABLA + oleadas probables 4%→20% cada 5 s) y EL '
        'FUGITIVO (100%: escapa flotando con la levitación del difunto Códice Vivo). EL '
        'CÓDICE VIVO BORRADO por completo (ítem + 2 proyectiles + 2 rawimg + '
        'registraciones + localización ×3). ACTUALIZAR-FUENTE.bat/.sh ahora BORRA la '
        'versión anterior antes de copiar. LECCIONES: (1) el 19º incidente del espejo — la '
        'herramienta de edición NORMALIZA tabs→espacios el en-US entero: restaurar de git '
        '+ parche por script Python byte-exacto; (2) el paquete re-serializa el es-ES — el '
        'espejo se verifica por CONTENIDO parseado (hjson), no por bytes; (3) los const '
        'internal viajan como FIELD en el dump Cecil, no en el IL de los métodos; (4) una '
        'muerte sin loot ni crédito = desactivación directa + SyncNPC en cadena, JAMÁS '
        'checkDead |\n')
ancla_tabla = '| Versión | Estado | Notas |\n|---|---|---|\n'
assert t.count(ancla_tabla) == 1
t = t.replace(ancla_tabla, ancla_tabla + fila)

paso = '''## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)

1. **✅ v6.50.91 CONSTRUIDA Y VERIFICADA — ⏳ PUBLICACIÓN (falta token
   GitHub)** — el usuario pidió la relectura del Nervioso (HAMBRE, no
   miedo) + LA CAZA (quieto 5 s → el libro flota encima mirando al
   jugador; algo SE MUEVE cerca → La Sombra de la Página muerde 10% de la
   vida máxima por golpe hasta ABSORBER: sin loot, sin gore, sin crédito,
   XP sí al Grimorio del Eterno, −1% de hambre por presa, hasta el 0% con
   el reloj congelado) + EL IMPACIENTE (≥50%: los efectos nacen de cero,
   el libro HABLA, comienzan las probabilidades de oleadas) + LA FUGA (al
   100% escapa flotando) + EL CÓDICE VIVO BORRADO por completo + el
   ACTUALIZAR-FUENTE que BORRA la versión anterior antes de copiar.
   .tmod 2.564.967 B md5 ce511c4ea682c4012603fe390a0ad537 (386
   entradas), auditoría 0 fallos, headless 0 excepciones. CHECKLIST DEL
   USUARIO: (a) Nervioso en inventario con hambre < 50%: quedarse QUIETO
   5 s — el libro sale flotando y te MIRA; (b) que una criatura SE MUEVA
   cerca (¡un conejo vale!) — la sombra la muerde 10% por golpe y la
   ABSORBE: sin loot, sin gore, el Grimorio del Eterno cobra XP, −1%
   hambre por presa; (c) el libro caza hasta el 0% y vuelve «…saciado.
   Por ahora.»; (d) ≥ 50%: los efectos nacen DE CERO al cruzar el umbral
   y el libro EMPIEZA A HABLAR (mira el chat); (e) al 100%: ¡SE ESCAPA
   flotando y mantiene distancia! (clic derecho lo reinicia y vuelve);
   (f) los NPC de las casas NUNCA son presa — que pase el Guía cerca;
   (g) el tooltip del Nervioso muestra SUS fases (Tranquilo/Cazando/
   Impaciente/Fugitivo); (h) correr ACTUALIZAR-FUENTE.bat — primero
   BORRA la vieja y luego copia. PENDIENTE CASA: token GitHub → push +
   tag v6.50.91 + release con AethonMod.tmod (md5
   ce511c4ea682c4012603fe390a0ad537) + verificar CDN + fila de la tabla
   con el release ID real.
'''
viejo_paso = '## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n'
assert t.count(viejo_paso) == 1
t = t.replace(viejo_paso, paso)
io.open(SN, 'w', encoding='utf-8', newline='').write(t)
print('STABLE-SNAPSHOT.md: título + última actualización + fila .91 + paso 1')

# ============ README.md ============
RD = 'README.md'
t = io.open(RD, encoding='utf-8').read()
viejo = '> **Versión actual:** 6.50.90'
nuevo = '> **Versión actual:** 6.50.91'
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
io.open(RD, 'w', encoding='utf-8', newline='').write(t)
print('README.md: versión 6.50.91')
