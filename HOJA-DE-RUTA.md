# HOJA DE RUTA — EL TERCER ACTO (backlog Tasks 86-103)

> **Espejo compacto del worklog** (que vive en el sandbox y los wipes se lo han comido
> dos veces — lecciones de las Tasks 68-84). La especificación gorda de cada tarea está
> en el worklog; aquí vive el MAPA. Creado en la Task 85, era v6.56.2 — actualizado en la
> Task 108 (era v6.56.4: el camino GPU del RC2 portado a las armas + el espejo completo
> de las alas + el portal del conjuro + localización monolingüe) y en la Task 109
> (era v6.56.5: la cura del pitido de la carga + la auditoría de los 4 agentes — el
> temblor perceptible, el portal a tamaño real, la espina de la Ira/Partidor al derecho,
> la paridad de las runas y el empaquetado sin .fx).
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
