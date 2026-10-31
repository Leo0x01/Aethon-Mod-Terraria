# HOJA DE RUTA — EL TERCER ACTO (backlog Tasks 86-103)

> **Espejo compacto del worklog** (que vive en el sandbox y los wipes se lo han comido
> dos veces — lecciones de las Tasks 68-84). La especificación gorda de cada tarea está
> en el worklog; aquí vive el MAPA. Creado en la Task 85, era v6.56.2 — actualizado en la
> Task 108 (era v6.56.4), la 109 (era v6.56.5), la **Task 110 (era v6.56.6: LA PURGA de
> las armas nuevas —solo el RC2 queda— + la cura REAL de la simetría de las alas —el
> barrido rígido murió: Δ 0.0000 px— + el RC2 desnudo —sin disco negro ni lente, coronas
> al 53%, la pila de capas y LOS SOLES RÚNICOS del accesorio— + LAS 9 VARIANTES con una
> técnica VFX cada una + la cura de los textos crudos —el en-US espejo de seguridad— +
> la auditoría de las 33 librerías con su top 10) y la **Task 111 (era v6.56.7: LAS 9
> VARIANTES PURGADAS por la letra + LA CURA DE LOS PICOS —la técnica del commit 607df36
> vuelve ENTERA: la PILA DE CAPAS de la .56.6 era la causa, quads poligonales + el
> remolino BINARIO crudo; LEY DEL PIXEL vs EL QUAD en el códice— + EL COLOR DEL MOD
> (ColorPrisma, perdido en la reconstrucción .56.0) + LOS SOLES RÚNICOS VISIBLES —el
> contrato de coordenadas VFXCore: nacían fuera de pantalla— + LA BOCA de 4 glows con
> alfa a CERO contra el «círculo plano» de la carga + EL SHADER MEJORADO: halo per-pixel,
> filamentos por auto-interferencia, núcleo que late y franja prismática)**.
>
> **La tesis**: las Definitivas son los fragmentos del poder de un dios, y el Testamento
> es, literalmente, SU testamento. ¿Quién firma un testamento? Alguien que va a morir…
> o alguien que planea **volver a por lo que dejó escrito**. El tercer acto es
> EL QUE EMPUÑA LA SENTENCIA (Task 101) — el jefe espejo que usa el arsenal contra el
> jugador. Todo lo demás son los cimientos y la publicidad de ese momento.

## El mapa

| Fase | Task | Título | Qué es | Costo | Depende de |
|---|---|---|---|---|---|
| **0 · LA DEUDA** | 86 | La deuda en cero | SentenciaActual por derivación («computar, no sincronizar» desde ExtraAI) + los 3 pinceles GC calientes a estáticos (patrón pozos del RC2) + la LEY DE UN ARCHIVO POR VERSIÓN (Main.rand→Hash01, 1ª cuota de ~196) escrita en el códice | 1 ver | — |
| **1 · GAME FEEL** | 87 | El golpe que se siente | Hitstop (2-4 frames) enganchado al GolpeMotor (~115 call-sites, UN solo sitio) + screen shake escalado por % del daño + frames de anticipación del jefe | 1-2 ver | — |
| **1 · GAME FEEL** | 88 | El silencio y la sentencia habla | 0.3 s de silencio pre-golpe (SilenciarZona ya cableado) + kill-cam de sentencia (estudiar la cámara lenta vanilla del Señor de la Luna) + viñeta del acento | 1 ver | 87 |
| **2 · EL ESCRIBA** | 89 | El Escriba nace | NPC ciudadano: vende páginas en blanco, comenta sentencias, diálogo reactivo al arsenal; sprite por generador + VLM | 1 ver | — |
| **2 · EL ESCRIBA** | 90 | Las sentencias despiertan | Cada N dictaduras el Testamento ESCRIBE UNA PÁGINA = pasivo permanente por arma (7 armas × 3-5 páginas). El arma crece por usarse | 1-2 ver | 86, 89 |
| **2 · EL ESCRIBA** | 91 | El Códice jugable | Libro UI que colecciona las páginas con lore + datos + viñeta del acento; siluetas para las faltantes. La documentación de la casa hecha contenido | 1-2 ver | 90, 89 |
| **2 · EL ESCRIBA** | 92 | El idioma solar | Alfabeto de glifos; frases del Testamento que se decodifican letra a letra según el nivel del Grimorio. La comunidad hará wikis (caso Tunic) | 1 ver | — |
| **3 · EL MUNDO** | 93 | El mundo reacciona | Cielo teñido por sentencia (peso 0→1) + cicatrices donde caen jefes (BrumaFX persistente) + el Rayo del Alba pega más al amanecer (bonus, nunca penalización) | 1 ver | 88 ideal |
| **4 · PROGRESIÓN** | 94 | Los versículos de maestría | Kills ocultos por Definitiva; en umbrales (50/250/1000) el arma gana VERBOS NUEVOS (2ª aguja del RC2, la Pluma que ancla dos) — anunciados por el Escriba | 1-2 ver | 89 |
| **4 · PROGRESIÓN** | 95 | Las 7 pruebas | Una prueba por Definitiva («sobrevive a tu propia Ira del Cielo», «parte 100 enemigos con UN uso del Partidor»), checklist visible, progreso persistente. **LA LLAVE del jefe final** | 1-2 ver | 90/91 ideal |
| **5 · VFX** | 96 | VFX del tercer acto | Aberración cromática del Partidor (RGB split 3-4 frames sobre Main.screenTarget) + succión de polvo pre-strike + calor/refracción en la Escalera + estelas fantasma del jugador | 2 ver | — |
| **5 · VFX** | 97 | Modo Cine + ModConfig | Slider Reducido/Normal/Cine (×0.5/×1.0/×2.0 del presupuesto) — techo/piso del adaptativo, no reemplazo | ½ ver | — |
| **6 · SONIDO** | 98 | El leitmotiv de Aethon | 4 notas transportadas/filtradas en cada Definitiva y jefe (branding sonoro) + música en capas por sentencia (opcional, escalonable) | 1 ver | — |
| **7 · MP** | 99 | La sentencia compartida | Dos jugadores conectan Definitivas con <0.5 s → fusión de acentos + flash + bonus (+15-20%). Detectada con la MISMA derivación de Task 86 | 1 ver | 86 |
| **7 · MP** | 100 | El banco de pruebas | Sala de maniquíes + medidor de DPS (ventana 5 s, enganchado al GolpeMotor) + botón de estrés VFX (200 maniquíes, 1 clic). Balance medible + theorycrafting | 1 ver | — |
| **8 · EL ACTO FINAL** | 101 | EL QUE EMPUÑA LA SENTENCIA | El jefe final espejo: cada fase una Definitiva usada CONTRA el jugador. Entrada: cielo del acento + 3 s de silencio + el Escriba escribe la sentencia contra ti. Recompensa: LA HERENCIA (forjada con las 7). **Gate: las 7 pruebas** | 2-4 ver | 95 (+87/88/93/89) |
| **8 · EL ACTO FINAL** | 102 | Modo Sentencia (NG+) | Flag de mundo al matar al jefe: «el mundo ha sido juzgado» — enemigos +50%, las Definitivas despiertan niveles. Un flag, rejugabilidad infinita | ½ ver | 101 |
| **8 · EL ACTO FINAL** | 103 | El fin de semana del servidor + canal beta | Estreno público del MP blindado con la Sentencia Compartida de protagonista + tags -beta con el pipeline byte a byte + receta de hosting en los docs | ½ ver + fecha | 99 ideal |

## El orden de ataque recomendado

1. **86** — la deuda en cero primero: el worklog limpio antes de estrenar contenido.
2. **87 → 88** — game feel: el máximo impacto por hora invertida de todo el backlog.
3. **89 → 90 → 91 → 92** — el Escriba y la palabra escrita (el mod gana narrador, progresión y libro).
4. **93** — el mundo reacciona.
5. **94 → 95** — la progresión larga (versículos y las 7 pruebas).
6. **96 → 97 → 98 → 99 → 100** — VFX, cine, sonido, MP y laboratorio (orden libre; varias no dependen de nada).
7. **101** — EL JEFE ESPEJO (la puerta solo abre con las 7 pruebas completas).
8. **102 → 103** — NG+ y el estreno del servidor: el círculo se cierra con gente jugando junto.

## v6.56.8 — LA CURA DE LA BANDA PLANA (30-oct)

- **Qué pasó**: el .fxc recompilado de la .56.7 (reescritura completa del PS, 9 muestreos) no renderizaba el plasma en la GPU del jugador — banda plana sin textura. LA CURA: esqueleto .55.1 PROBADO + cirugía de deltas (venas rojas/pulso/halo/latido) + `ColorConjuro` (morado/rojo/negro — ColorPrisma muere en el RC2) + la boca de glows apilados PROHIBIDA (era el círculo plano) + fallback hermoso. **AUDITORÍA DE LIBRERÍAS entregada** (`AUDITORIA-LIBRERIAS.md`).
- **Lecciones**: (1) el .fx del RC2 se cambia CON CIRUGÍA, jamás reescritura completa — cada delta se compila/valida/inspecciona su GLSL/se simula; (2) una pila de glows «con alfa a cero» también es UN CÍRCULO PLANO para el ojo; (3) MojoShader-parsear bien NO garantiza renderizar bien en la GPU del jugador — el esqueleto probado es la única vara; (4) el tML 2026.8 guarda los blobs incomprimibles CRUDOS y el resto en deflate-raw (el auditor aprendió).
- **Siguientes candidatos** (de la auditoría): purgar BeamLib+BoltRenderer (496 líneas muertas), consolidar helpers en VFXCore (~900 líneas), decidir HazConcentradoLib muerto, TiemposLib (hitstop), PostFXLib, ImpactoLib.

## v6.57 — LA VUELTA DEL HAZ C (31-oct)

- **Qué pasó**: el usuario eligió EL HAZ C como ganador de las tres copias y afinó la letra en 6 puntos: círculos amarillos gigantes que no deben aparecer (era EL CORAZÓN DORADO del sol rúnico a escala de haz → `corazon: 0`, LA LEY DEL CORAZÓN DORADO en el códice §0), la textura blanca a TODO EL ANCHO (EL ALMA — la 7ª capa: la misma RampaGlow del núcleo a ×0.88-0.96), los soles rúnicos naciendo más lejos (150-240 → 320-520 px), el temblor suave de pocos píxeles al lanzar (`suave:` en ImpactoLib — la vía dulce de PantallaLib; sostenido 6.5→2.6), la corona rúnica del color del haz (la paleta del conjuro viajando) y la punta afilada + la muerte estrechándose (LA AGUJA ×0.06 + LA ESTRANGULACIÓN ×0.07 en `StripWidth`).
- **Lecciones**: (1) los efectos reciclados de accesorio a haz SE PARAMETRIZAN — lo que a escala de amuleto es una firma, a escala de cataclismo es un defecto; (2) el kick de impulso no es la única sacudida: para el momento del disparo, los DOBLES SENOS suaves de PantallaLib leen mejor que el golpe seco; (3) las curvas de ancho del strip aceptan factores MONÓTONOS nuevos (aguja/cierre) sin crear picos — verificado sim en vida Y en muerte.
- **Siguientes candidatos**: portar la receta C ganadora al RC2 original cuando el usuario dé el visto bueno visual · el resto del plan de la auditoría (AgujeroBase para los 10 agujeros negros, PostFXLib).

## v6.58 — EL ATAQUE SOBRE TODO + LA IGNICIÓN A CIEGAS (1-nov)

- **Qué pasó**: dos ajustes de letra sobre el haz C: (1) «algunas sprites del juego como las hojitas que caen de los árboles se ven sobre el ataque… el ataque debe estar sobre todo» — LOS PROYECTILES SE DIBUJAN ANTES del polvo/gore/partículas ambientales de vanilla; la cura NO toca el efecto: MUDA EL DIBUJADO ENTERO (boca + pila de 7 capas) a `RayosPruebaEncimaSistema` (`PostDrawInterface`, el punto aprobado de la casa) con el BAILE DEL LOTE (lote de mundo con GameViewMatrix.TransformationMatrix → dibujar → reabrir el lote de UI del fix v6.50.2). Misma geometría, mismo volumen: cero costo nuevo, cero picos nuevos. **EL DATO DEL IL** (el oráculo + el decompil del tML 2026.08 real): `Main.Transform` es solo-lectura y SIEMPRE devuelve `GameViewMatrix.TransformationMatrix` — los `ReabrirLoteVanilla` reabren en mundo desde cualquier contexto; y los `PlayerInput.SetZoom_*` son REMAPEADORES DE RATÓN (la primera versión del sistema los llamaba y habría corrompido el puntero — cazado antes de publicar). (2) «justo en el primer segundo de lanzar el rayo hay un pequeño destello blanco… es solo de iluminación» — DOS fuentes blancas morían: el flash de pantalla + baño AddLight ×2.75 del `GolpeDeCine` de la ignición y el sprite `DestelloFinal` a 0.9 de alfa. Ambas mueren (`destello: false` en ImpactoLib + el bloque del sprite borrado): quedan temblor suave + hitstop + estallido de soles — el momento se lee por el MOVIMIENTO. Y LA RESPUESTA A LA PREGUNTA DE LAS ALAS: los «picos» de TODAS las alas SON LAS PLUMAS (el diseño del abanico 2-7 plumas de puntas afiladas) — NO el bug de los quads de las armas (v6.56.6); documentado en §4.24.
- **Lecciones**: (1) un efecto puede estar PERFECTO y vivir en el LUGAR EQUIVOCADO del frame — cuando algo «se ve tapado», preguntar QUÉ pase lo tapa antes de tocar el efecto; (2) en el tML 2026.08 `Main.Transform` es un getter de la matriz del MUNDO siempre — el punto PostDrawInterface acepta dibujado de mundo sin tocar zoom-contextos; (3) los `SetZoom_*` de `PlayerInput` en este tML remapean el RATÓN — jamás llamarlos por transform; (4) un momento de ignición no necesita LUZ para leerse — temblor + hitstop + estallido bastan.
- **Siguientes candidatos**: portar la receta C ganadora al RC2 original cuando el usuario dé el visto bueno (el sistema encima suma el tipo del original al escaneo) · el resto del plan de la auditoría (AgujeroBase para los 10 agujeros negros, PostFXLib).

## Reglas del backlog

- Cada tarea se ejecuta con el RITUAL COMPLETO de la casa: oráculo 0/0 → -build REAL 0/0 →
  auditoría del .tmod (entradas, EOF, blobs, md5) → headless sin excepciones → release con
  CDN verificado byte a byte → STABLE-SNAPSHOT.md con su checklist.
- Las tareas 94 (versículos) y 101 (jefe) son ESCALONABLES: se pueden lanzar con un
  subconjunto y crecer por versión.
- La deuda de Main.rand (~196 emisiones) se paga CON LA LEY de la Task 86 (un archivo por
  versión), no con un gran bang.
- Si una tarea del bloque MP toca la derivación de la sentencia y la 86 no la cerró,
  cerrarla ahí de paso — la misma medicina sirve dos veces.
