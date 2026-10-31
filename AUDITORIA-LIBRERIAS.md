# AUDITORÍA DE LIBRERÍAS VFX — AethonMod (Task C)

> ## ⚡ ANEXO v6.56.9 — ESTADO DE EJECUCIÓN DEL PLAN
>
> **Ejecutado en la v6.56.9** (cada punto verificado con el oráculo 0/0):
>
> | # del top-10 | Estado | Detalle |
> |---|---|---|
> | 1. Purgar BeamLib + BoltRenderer | ✅ HECHO | Enterradas (−496 l) + el catálogo del códice corregido (§4.5/§4.6 ya no mienten) |
> | 5. Destino de HazConcentradoLib | ✅ HECHO | La parte muerta purgada (−313 l: Dibujar/AberracionCromatica/DiscoNegro); viven los soles rúnicos (con LA IMPLOSIÓN v6.56.9) |
> | 6. Purga de métodos muertos | ✅ HECHO | 40 métodos en 19 librerías (−1.317 l netas); 1 falso muerto cazado (FormaLib.PuntoEnBanda — vivo); restos huérfanos documentados abajo |
> | 2. Consolidar helpers en VFXCore | ✅ HECHO (v1) | Canónicas Tint/TintAlfa/AbrirAdditive/AbrirAlpha (CON SONDA)/Capsule/RingQuad + **96 cuerpos delegados** en 30 archivos (−201 l netas, la lógica vive UNA vez); Begin Immediate de shader y familias con guard quedan INTACTAS por semántica distinta; faltan ~22 Tint en Projectiles/Cosmic (tarea futura) |
> | 8. TiemposLib (nueva) | ✅ NACIÓ | EL HITSTOP — Congelar(ticks), solo SP, solo IA de NPC, cola priorizada; consumidor: la ignición de cine de las copias del RC2 |
> | 5.3 ImpactoLib (nueva) | ✅ NACIÓ | EL MOMENTO DEL GOLPE — Golpe/GolpeDeCine (kick+flash+luz+hitstop); consumidor de nacimiento: las copias del RC2; la migración de los 88 Kick/18 Flash/244 AddLight a mano es DEUDA documentada |
> | 10. Reconciliar el códice | ✅ HECHO | v1.4 reconciliada (.56.8) + **LA LEY DEL CÓDICE SINCRONIZADO (§0.6, la anti-recurrencia)** en la v1.5: el mismo commit + la muestra por auditoría |
> | 4. Unificar gestos de pantalla | ⏳ SIGUIENTE | PantallaLib dueño único del pipeline; Kick/Flash como alias delgados (no se tocan los 106 llamadores) |
> | 3. AgujeroBase (los 10 agujeros) | ⏳ SIGUIENTE | ~1.250 l recuperables; sim 1:1 antes/después obligatorio (regresión visual) |
> | 9. PostFXLib | ⏳ MÁS ADELANTE | El patrón RT de BlackHoleLensSystem + techo FactorCalidad |
>
> **Restos muertos huérfanos encontrados al purgar** (sin warning del compilador, tarea futura): PyraLib.Simular/CapCeldas/SeedCell, PulsoLib.Material (enum)/Marcar/Liquidar/VeredictoDe/CerrarVeredictosVencidos (el juicio diferido — conectar a TiemposLib o enterrar), FormaLib.PlanoLiangBarsky, EstelaLib.PuntoEnCamino.

# AUDITORÍA DE LIBRERÍAS VFX — AethonMod (Task C)

**Fecha:** sobre el árbol v6.56.7 · **Alcance:** `AethonMod/Content/VFX/` — **69 archivos .cs, 43.600 líneas** (de 379 .cs del mod).
**Método:** conteo de referencias por archivo (rg sobre todo `AethonMod/`), análisis de métodos públicos muertos (búsqueda negativa en TODO el mod, distinguiendo "sin llamador externo" de "muerto de verdad" — sin llamador NI externo NI interno), y conteo de definiciones duplicadas por nombre.

> Cómo leer la columna **usos**: número de archivos **fuera** de `Content/VFX/` que referencian la librería. Los `ModSystem` (OndaSystem, PulsoSistema…) marcan 0 porque los llama el motor, no el código. "(+N)" = además N archivos VFX hermanos la usan.

---

## 1. INVENTARIO COMPLETO (ordenado por tamaño)

| Librería | Líneas | Qué dibuja | Usos ext (+VFX) |
|---|---:|---|---:|
| AuraLib | 4.347 | Auras alrededor de NPC/jugador: 9 patrones + Forma Divina + halo arcoíris | 12 (+1) |
| RiftLib (+3 parciales) | 2.512 | Desgarros de realidad, portales, glitch, vórtices (4 archivos) | 23 (+4) |
| StormLib | 1.624 | Arcos eléctricos por pila de pasadas (receta vanilla LightningArc) | 40 (+28) |
| VFXCore | 1.501 | EL NÚCLEO: buffer de quads de luz, lotes, Ribbon, Hash01, presupuesto | 136 (+57) |
| SierpesLib | 1.352 | Criaturas segmentadas: 11 patrones de locomoción (sierpes, anguilas…) | 3 |
| RuneSunRenderer | 1.291 | Soles rúnicos por tier 1-10 + el disco estelar compartido | 8 (+9) |
| AlasAuroraLib | 1.147 | Alas 100% procedurales: abanico de plumas vectorial que se pliega | 5 |
| OrbitaLib | 1.139 | Anillos rúnicos del vacío: conjuros, coronas de lazos, ecos | 11 (+16) |
| SombrasLib | 1.127 | Sombras devoradoras: masa negra, ojos que miran, fauces, garras | 20 |
| RayoLib | 977 | Rayos estilo vanilla 1.4.5 (clima/Arc Surge): ola + horquillas + taper | 5 (+2) |
| SupremoAuroraBlackHoleRenderer | 960 | Agujero negro supremo-aurora (gradiente radial) | 1 |
| SupremoBlackHoleRenderer | 949 | Agujero negro supremo (fusión de los 4) | 1 (+1) |
| UmbralBlackHoleRenderer | 943 | Agujero negro umbral: disco oblicuo con Doppler | 1 (+1) |
| CicloEstelarRenderer | 927 | Una vida estelar completa en 5 actos (~18 s) | 1 (+1) |
| UmbralAscendidoBlackHoleRenderer | 888 | Agujero negro umbral-ascendido (+ lluvia de rayos) | 1 |
| PantallaLib | 879 | Gestos de pantalla: sacudida (ICameraModifier), flash, viñeta, onda UI | 7 (+3) |
| EclipsePrimordialRenderer | 831 | Eclipse total: ocultador negro, perlas de Baily, anillo de diamante | 1 |
| SigiloLib | 784 | Anillos rúnicos solares: SelloSolar, ArcoGloria, glifos | 9 (+4) |
| CosmicAscendidoBlackHoleRenderer | 778 | Agujero negro cósmico-ascendido (+ tormenta en los picos) | 1 |
| OlvidoAscendidoBlackHoleRenderer | 776 | Agujero negro olvido-ascendido (+ rayos espirales) | 1 |
| BrumaAscendidoBlackHoleRenderer | 711 | Agujero negro bruma-ascendido (+ escarcha eléctrica) | 1 |
| RayoStrip | 710 | Tiras de vértices GPU lisas (Filamento/FajaTexturada) — infraestructura | 7 (+7) |
| MoldeLib | 705 | Cuerpos compuestos declarativos (piezas ancladas a un cuerpo) | 1 (+2) |
| OlvidoBlackHoleRenderer | 680 | Agujero negro clásico: vacío + plasma + runas doradas | 1 (+1) |
| CodigosLib | 656 | Códigos vivos: DanzaOrbes, OjoAbismo, SolVivo, SierpeEstelar | 2 (+1) |
| CompasLib | 637 | Coreografía declarativa: Compas/EnVentana/Cada + depuración F7 | 5 (+4) |
| MediaResLib | 628 | Pases a ½ resolución para fuego/humo (fill-rate ÷4) | 2 (+2) |
| PyraLib | 606 | Fuego como temperatura: rampas 1D, lenguas, brasas | 20 (+6) |
| FormaLib | 589 | Colisiones de formas: segmento/banda/cono/anillo/sector | 1 (+2) |
| CosmicBlackHoleRenderer | 557 | Agujero negro cósmico: anillo magenta de 20 bandas | 1 (+1) |
| LumenLib | 551 | Luz viva: Bloom/Flare/Ray/Lance + paletas de color | 31 (+10) |
| EstelaLib | 543 | Estelas: pipeline Sanitize→Smooth→Resample + ribbon de 3 capas | 13 (+6) |
| OndaLib | 516 | Ondas de impacto: Shock/Pulse/Telegrafo + Kick/Flash de pantalla | 35 (+9) |
| AlasEstilo | 499 | Catálogo declarativo de anatomías de alas (23 estilos) | 3 (+1) |
| HazConcentradoLib | 499 | Camino GPU del RC2 (strip+shader) + soles rúnicos de carga | 1 (+2) |
| BrumaBlackHoleRenderer | 490 | Agujero negro de bruma (demo de BrumaFX: teal/cian) | 1 (+1) |
| RelojArenaRenderer | 478 | Reloj de arena: 26 motas con física + inversión | 2 |
| PulsoLib | 475 | Veredicto diferido + retroceso + cámara + pantalla (por material) | 8 (+2) |
| DefinitivasLib | 425 | Soporte de las definitivas: paleta, presas, cono, 4 texturas plasma | 2 (+3) |
| TelaLib | 412 | Cinta con fase anclada al arco (no resbala al girar) | 2 |
| EcoLib | 404 | Las voces: texto dramático en pantalla (la voz del libro) | 10 (+2) |
| **BeamLib** | **376** | **Haz masivo de 7 capas — ⚠ 0 USUARIOS (muerta)** | **0** |
| TajoLib | 367 | Media luna de tajo anime (aparece tras el corte) | 4 |
| MagnetarRenderer | 351 | Estrella magnetar: campo estrangulado + reconexión | 1 |
| RedSupergiantRenderer | 350 | Supergigante roja: celdas de convección + colapso | 1 |
| NeutronStarRenderer | 337 | Estrella de neutrones: starquake con aberración R/B | 2 (+1) |
| MareaGravitatoriaRenderer | 331 | Ola rotada por la pendiente del terreno | 1 |
| PenduloJuicioRenderer | 319 | Maza péndulo con runas grabadas | 1 |
| NebulaLib | 296 | Nebulosas volumétricas que nadan por curl noise | 1 |
| OndaSystem | 295 | ModSystem: aplica la sacudida (ModifyScreenPosition) + flash local | 0 (+3) |
| PulsarRenderer | 281 | Púlsar: el faro de 2 haces | 1 |
| WhiteDwarfRenderer | 273 | Enana blanca: destellos hexagonales + motas Leech | 1 |
| DeadStarRenderer | 271 | Enana oscura: masa que APAGA la luz | 1 |
| AudioLib | 270 | Identidad sonora: 6 familias × 5 momentos, anti-spam | 6 |
| OcasoSystem | 269 | ModSystem del ocaso (ciclo de vida del efecto) | 1 (+3) |
| CoroEspectralRenderer | 264 | Bastón-coro: notas musicales dibujadas por código | 1 |
| EspectroLib | 262 | Fantasmas: Memoria, ColaHistoria, EcoOrbital | 6 |
| GravLens | 261 | Distorsión gravitacional del fondo (cualquier efecto la registra) | 5 (+1) |
| EnjambrePrismaticoRenderer | 238 | 12 abispajas prismáticas del bastón-enjambre | 1 |
| RiftLib.Glitch (parcial) | 200 | EcoGlitch (aberración por triple redibujo) + DesgarroGlitch | — (parte de RiftLib) |
| VFXPalettes | 144 | Paletas nombradas (VoidQueen/CrimsonCourt/RuneStars) | 1 (+2) |
| **BoltRenderer** | **120** | **Relámpago determinista 1ª gen — ⚠ 0 USUARIOS (muerto)** | **0** |
| AnilloDorsalRenderer | 112 | Facade de corona dorsal → OrbitaLib.CoronaConjuro | 3 |
| OcasoBurstFX | 103 | El desgarro que respira al morir (ocaso) | 2 (+1) |
| ColorPrisma | 76 | EL COLOR DEL MOD: arcoíris de aceite (Oil/ValueAt/ALoLargo) | 3 (+2) |
| RuneCrownRenderer | 47 | Facade de corona rúnica → SigiloLib.ArcoGloria | 2 |
| PulsoSistema | 46 | ModSystem de PulsoLib | 0 (+1) |
| ArcCrownRenderer | 38 | Facade de corona de arcos → OrbitaLib.CoronaArcos | 1 |

**Radiografía rápida:** VFXCore (136 archivos) y OndaLib (35), LumenLib (31), StormLib (40), SombrasLib (20), PyraLib (20) son las caballos de batalla. En el otro extremo: **2 librerías muertas** (BeamLib, BoltRenderer), **4 librerías de un solo consumidor** (MoldeLib, NebulaLib, TelaLib, MediaResLib) y los 10 renderers de agujero negro, cada uno con su único projectile dueño.

---

## 2. LOS 3 GRUPOS DE FUSIÓN RECOMENDADOS

### Grupo A — Los 10 agujeros negros → extraer "AgujeroBase" (el 47% de todo el VFX)

**Miembros:** Olvido, Umbral, Cosmic, Supremo, SupremoAurora, Bruma, + sus 4 Ascendidos = **7.532 líneas (17% del VFX)**.

**Qué se solapa DE VERDAD (verificado):** cada renderer trae su propia copia privada de los mismos helpers (Tint, Hash01, Ellipse, Capsule, BeginAdditive, BeginAlpha, RingQuad, Doppler…) y de **métodos de dibujo completos idénticos salvo la paleta**: `DrawDistortionWaves` ×10, `DrawPhotonRunners` ×7, `DrawNebulas` ×6, `DrawPolarFlares` ×5, `DrawFallingTendrils` ×4, `DrawPolarJets` ×4, `DrawGoldenRunes` ×4, `DrawLuminousMotes` ×4, `DrawSpiralArms` ×4, `DrawBolt` ×3, `DrawElectricBolts` ×3, `DrawEmbers` ×3, `DrawRuneCircle(s)` ×6 — **~1.563 líneas duplicadas** (muestra verificada: Cosmic vs Olvido de `DrawDistortionWaves` = 10 de 11 líneas idénticas).

**Qué NO se solapa:** el CARÁCTER de cada uno (el Doppler del Umbral, la escarcha del BrumaAscendido, la tormenta del CosmicAscendido, los rayos espirales del OlvidoAscendido) — esa parte es composición única y debe quedarse.

**Veredicto:** fusión con valor real, no cosmética. Un `AgujeroBase` estático (primitivas + helpers parametrizados por paleta) deja cada renderer en ~250-350 líneas de pura personalidad. **~1.250-1.500 líneas recuperables** y, sobre todo, un bug se cura UNA vez (hoy un fix en DrawDistortionWays hay que replicarlo 10 veces). Esfuerzo: alto. Riesgo: medio (regresión visual) — mitigable con el método de la casa: sim 1:1 + verificación visual antes/después.

### Grupo B — Gestos de pantalla/cámara → PantallaLib como dueño único

**Miembros:** PantallaLib (879), OndaLib.Kick/Flash + OndaSystem (295), PulsoLib.GolpearCamara/EmpujarPantalla + PulsoSistema (46).

**Qué se solapa (verificado):** **TRES APIs de sacudida** — `OndaLib.Kick` (impulsos, ModifyScreenPosition; **88 llamadas externas**, la más usada del mod), `Pantalla.Sacudir` (trauma acumulativo con ICameraModifier REAL; 5 llamadores), `PulsoLib.GolpearCamara` (golpe-tirón; 0 llamadores externos) — y **TRES APIs de flash** — `OndaLib.Flash` (gradiente local, ~18 llamadores), `Pantalla.Flash` (cruz de 8 rayos, 1 llamador externo), `PulsoLib.EmpujarPantalla` (tinte, 5 llamadores). Dos ModSystems distintos aplican efectos de pantalla (OndaSystem: ModifyScreenPosition + PostDrawInterface; PantallaSistema: PostDrawInterface).

**Qué NO se solapa:** la viñeta y la onda de UI solo viven en PantallaLib; el flash GRADIENTE LOCAL centrado en el arma solo en OndaSystem; el modelo trauma de PantallaLib es cualitativamente superior al impulso suelto.

**Veredicto:** fusión de la CAPA DE APLICACIÓN, no de las APIs. PantallaLib (o su sistema) debe ser el único dueño del estado de pantalla y del pipeline (una sola sacudida compuesta, un solo overlay). `OndaLib.Kick/Flash` se conservan como **alias delgados** para no tocar 106 llamadas. Valor alto: hoy un Kick y un Sacudir simultáneos se SUMAN sin control (nadie compone). Esfuerzo: medio. Riesgo: medio-bajo.

### Grupo C — La familia eléctrica → NO fusionar las vivas; ENTERRAR las muertas

**Miembros:** RayoLib (977), StormLib (1.624), RayoStrip (710), BeamLib (376), BoltRenderer (120), HazConcentradoLib (499).

**La diferencia REAL de cada una (verificada en código y uso):**
- **RayoStrip** = infraestructura GPU: pozo de vértices + BasicEffect, tiras lisas. No es un "estilo de rayo", es la tubería. La usan RayoLib-vecinos, StormLib, EstelaLib-vecinos, el RC2, PrimerTrazo.
- **StormLib** = arcos eléctricos CPU por PILA DE PASADAS (6 anchos × alfas, receta LightningArc vanilla). 40 usuarios. Es "el chisporroteo".
- **RayoLib** = recreación 1:1 del LightningGenerator de vanilla 1.4.5: raymarch direccional, timón, ola, horquillas, taper. 5 usuarios. Es "el rayo-trueno de cielo".
- **BeamLib** = el haz masivo de 7 capas… **con 0 usuarios** (sus consumidores documentados, PrimerTrazoFajo y RayoConvergente, ya no lo llaman). **Muerta.**
- **BoltRenderer** = relámpago determinista de 1ª gen montado sobre RayoStrip. **0 llamadores reales** (todas sus menciones son comentarios; el propio StormLib dice que lo sustituyó). **Muerto.**
- **HazConcentradoLib** = el camino GPU del RC2 portado a armas en v6.56.4… pero la purga de las 9 variantes (v6.56.6) mató a TODOS sus consumidores (Alba/Veredicto/Juicio/Prisma ya no existen): `Dibujar` (~161 líneas), `AberracionCromatica` (~58) y su cargador de shader no tienen NINGÚN llamador. Viven solo `SolesRunicosCarga/Estallido` (~72 líneas, el RC2 los usa).

**Veredicto:** RayoLib + StormLib + RayoStrip **NO se fusionan** — tres escuelas distintas y cada una sana y usada; fusionarlas sería cosmético y peligroso. La acción correcta es **purgar**: BeamLib + BoltRenderer enteros (496 líneas) y la parte muerta de HazConcentradoLib (~313 líneas) — o, si el plan es volver a tener armas de haz escalonadas, revivir `Dibujar` documentándolo como consumidor futuro. Las estelas (EstelaLib) y la cinta (TelaLib) tampoco se tocan: ribbons de propósitos distintos (estela que sigue la historia vs cinta anclada a arco) — solo conviene compartir el pipeline Sanitize/Smooth/Resample.

---

## 3. MÉTODOS PÚBLICOS MUERTOS (verificados con búsqueda negativa en TODO AethonMod/)

Criterio estricto: el método no aparece llamado en NINGÚN otro .cs ni dentro de su propio archivo. Los métodos solo-usados-internamente se listan aparte como "candidatos a rebajarse a private".

### Muertos de verdad (~793 líneas recuperables en métodos + librerías muertas aparte)

| Librería | Muertos | ~Líneas | Nombres |
|---|---:|---:|---|
| StormLib | 4 | 184 | SeekArc (73), FractalBolt (50), ForkTree (47), Boil (14) |
| FormaLib | 10 | 89 | RecorrerNPCs, NPCEnAnillo/Banda/Sector, PuntoEnAnillo/Banda/Capsula/Sector, SegmentoIntersecaAABB, PuntoMasCercanoEnSegmento |
| PyraLib | 1 | 81 | EmberField (el autómata de brasas) |
| PulsoLib | 6 | 66+ | Sonido (26), AbrirVeredicto (13), OffsetRetroceso, Retroceder, TieneMarcas, ContarMarcas |
| OndaLib | 1 | 44 | TelegrafoAnillo |
| DefinitivasLib | 5 | 43 | Blit, GradienteEscalera, PinchosDifraccion, AnguloHacia, GradienteJuicioOscuro |
| TajoLib | 1 | 42 | ArcoToca (¡la colisión del tajo!) |
| RuneSunRenderer | 2 | 37 | DrawOrbitalSystem (26), RingGlyphWorld (11) |
| CompasLib | 2 | 33 | EnVentana, SuavizadoInicio (el struct Compas SÍ vive — lo usan CoreoGuion y Apuestas) |
| LumenLib | 2 | 32 | BloomTriple, DoublePass (la doble pasada del Last Prism) |
| EspectroLib | 1 | 30 | CurvaAproximacion |
| EstelaLib | 1 | 30 | GhostTransforms (los fantasmas documentados en §4.7) |
| AuraLib | 21 | 21 | Los setters fluidos ConCantidad, ConSolidas, ConTasa, ConVelocidad, ConAscenso, ConTamano, ConVida, ConAnillos, ConGajos, ConGiro, ConAlfaTrasera/Frontal/Bruma, ConVelo, ConBorde, ConParpadeo, ConHervor, ConRayos, ConLatido, ConBloom, ConSemilla |
| MoldeLib | 4 | 20 | ReEscalar, EmitirPases, ConTextura, ConSeguirDireccion |
| VFXPalettes | 2 | 11+ | DiskDoppler, OuterGlow — y con ellas muere la CLASE VoidQueen entera (~55 líneas, "la paleta estrella" del catálogo, sin un solo usuario) |
| PantallaLib | 1 | 8 | PresetGolpeSeco |
| VFXCore | 1 | 7+ | RegisterLayer — y con él el SUBSISTEMA de capas de oclusión (enum VFXLayer + registro + FlushOcclusion vacío, ~70 líneas: nadie registra capas) |
| CodigosLib | 1 | 9 | EsAleta |
| RayoLib | 1 | 6 | PuntoImpacto |

### Librerías enteras muertas (verificado: cero llamadores en todo el mod)

| Librería | Líneas | Nota |
|---|---:|---|
| **BeamLib** | 376 | El catálogo §2/§4.5 aún la recomienda — MIENTE: 0 usuarios |
| **BoltRenderer** | 120 | Todas sus menciones son comentarios históricos |
| **HazConcentradoLib (parte muerta)** | ~313 de 499 | Dibujar + AberracionCromatica + ObtenerShader + DiscoNegro: murieron con la purga de las 9 variantes |

### Solo-internos (públicos sin llamador externo — rebajar a private/internal, no borrar)

AuraLib: DibujarDivino/2/3/4, DibujarHaloArcoiris, ConRadio, ConForma, ConPatron, ConLados, ConGlow, ConTrasera/Frontal… (los usan las factorías internas) · StormLib: FractalPath, PathLength, BuildTree · RayoLib: SiguienteF (98 líneas), Contiene, CalcRotaciones, Generar, OlaColor, OlaAncho, Remap, Siguiente · VFXCore: FlushOcclusion, AprenderHiloPrincipal · RiftLib: AnchosCamino, LongitudCamino · OrbitaLib: RunaVacia · PulsoLib: GolpearCamara, Sonar(Material).

### Recuperable en total (purga + consolidación de duplicados, sin perder NI UN efecto)

| Concepto | ~Líneas |
|---|---:|
| Métodos muertos verificados | 793 |
| BeamLib + BoltRenderer | 496 |
| HazConcentradoLib (parte muerta) | 313 |
| VoidQueen + capa de oclusión VFXCore | ~125 |
| Consolidar helpers duplicados (Grupo de abajo) | ~900 |
| Familia de renderers (Grupo A) | ~1.250 |
| **TOTAL** | **≈ 3.900 líneas ≈ 9% del VFX** |

---

## 4. COPIAS PRIVADAS DE HELPERS (conteo exacto por helper)

Definiciones reales contadas archivo por archivo en `Content/VFX/`:

| Helper | Copias | ~Líneas totales | Detalle |
|---|---:|---:|---|
| `Tint(Color c, float f)` | **35** (31 privadas) | 381 | La regla de la casa es `Color·f`… reimplementada 35 veces. Solo StormLib la expone pública |
| `BeginAdditive` | **23** (todas privadas) | 146 | El abrir-lote aditivo casero; OrbitaLib ya expone `AbrirAdditive` pública |
| `Hash01(seed,a,b)` | **12 privadas** + 3 alias `H01` | 114 | **Siendo VFXCore.Hash01 la canónica y LA LEY §0** — 12 renderers traen su copia propia |
| `Capsule` | **15 privadas** + 1 pública | 89 | La cápsula glow de 2 quads |
| `BeginAlpha` | **14** (privadas) | 84 | Ídem BeginAdditive para el lote alfa |
| `RingQuad` | **12 privadas** | 74 | El anillo por textura Ring; VFXCore ya tiene `RingQuadSize` |
| `Ellipse` | **9 privadas** + 2 públicas | 46 | VFXCore.Ellipse existe con firma superior (a, b, tilt) |
| `CapsulaWorld` | 4 | 36 | Variante en coords mundo de los bastones |
| `Doppler` | 4 | 20 | El blanco/rojo del disco de acreción, copiado entre Supremos/Umbral |
| `ScaleOf` | 7 | 14 | La escala común de las estrellas |
| `Frac` | 3 | 7 | AuraLib, DefinitivasLib, SombrasLib |
| `H01` (alias Hash01) | 3 | 17 | OndaLib, PyraLib, RiftLib |
| `DeGolpe` | 2 | 11 | DefinitivasLib + SombrasLib |
| `Alfa` | 2 | 11 | DefinitivasLib + SombrasLib |
| **TOTAL** | **~125 definiciones** | **~1.038** | |

**Y además los métodos de FAMILIA enteros duplicados** (Grupo A): DrawDistortionWaves ×10, DrawPhotonRunners ×7, DrawNebulas ×6, DrawPolarFlares ×5, DrawFallingTendrils/DrawPolarJets/DrawGoldenRunes/DrawLuminousMotes/DrawSpiralArms ×4, DrawEstela/DrawBolt/DrawElectricBolts/DrawEmbers/DrawRuneCircle ×3, DrawRuneCircles ×3 → **~1.563 líneas**.

**Cura:** promover a VFXCore una sola versión canónica de `Tint`, `Hash01` (ya existe), `Ellipse`, `Capsule`, `RingQuad` y un par `AbrirAdditive/AbrirAlpha` (copiar el de OrbitaLib), y borrar las copias. La v6.56.6 ya anotó "57 copias privadas de Tint/Hash01" — hoy son ~125 y creciendo.

---

## 5. LIBRERÍAS NUEVAS PROPUESTAS (verificada su inexistencia)

### 5.1 TiemposLib — el hitstop / congelación / ralentí del juego
- **Qué haría:** congelar o ralentizar EL JUEGO unos ticks en momentos clave (impactos de definitivas, muertes de jefe, cambio de fase de la Furia, la ignición del RC2), con cola priorizada, rampa de entrada/salida y correcto en multijugador.
- **Por qué no existe (verificado):** 0 resultados de `TiemposLib`, `hitstop`, `timeScale`, `SlowMo` en todo el mod. Lo más parecido es `AethonBoss.TiempoCongelado`, un flag ad-hoc de UN jefe que congela a los NPC pero no al tiempo visual.
- **Quién la usaría:** el "clang" del juicio diferido de PulsoLib (hoy muerto — renacería aquí), las 4 sentencias, la Furia (fase La Nebulosa), CicloEstelar en la supernova, el RC2.
- **Riesgo a vigilar:** en MP el tiempo de juego es compartido — la librería debe congelar IA/animación de forma determinista, no `Main.time` a lo loco.

### 5.2 PostFXLib — la cadena de post-proceso de pantalla completa
- **Qué haría:** efectos de PANTALLA COMPLETA: aberración cromática real (espacio-pantalla), viñeta de color, distorsión de calor, radial-blur de impacto — alimentada por el presupuesto de VFXCore (`FactorCalidad`).
- **Por qué no existe (verificado):** 0 resultados de `PostFX`. La "aberración" de hoy es POR-TIRA: `RiftLib.EcoGlitch` re-dibuja 3 veces desplazado, y `HazConcentradoLib.AberracionCromatica` (muerta) hacía 2 tiras R/C. El único efecto de pantalla es la lente de `BlackHoleDistortionShader`, que solo deforma el FONDO en círculos registrados.
- **Quién la usaría:** RC2 (ignición), RayoGamma (ya intenta su aberración por-tira con paleta R/G/B), los desgarros RiftLib, las muertes de jefe, TiemposLib.
- **Riesgo a vigilar:** render targets — el mapa §2 prohíbe "warp con render targets frágiles". La vía segura ya existe en casa: el patrón de `BlackHoleLensSystem` (RT con caché + bandera + fallback, §1.4).

### 5.3 ImpactoLib — el "momento del golpe" unificado
- **Qué haría:** UNA llamada (`Golpe(pos, tipo, intensidad)`) que compone lo que hoy cada proyectil ensambla a mano con números mágicos: flash local + sacudida escalada + onda + golpe de luz + sonido con prioridad (+ mañana: hitstop de TiemposLib).
- **Por qué no existe (verificado):** el embrión es `PantallaLib.PresetImpacto` (2 usuarios) y `PresetGolpeSeco` (muerto). Hoy hay 88 llamadas a `OndaLib.Kick`, ~18 a `OndaLib.Flash`, 244 `AddLight` en 118 archivos, cada una con su constelación de números.
- **Quién la usaría:** las 379 armas/proyectiles; es además la landing natural de la fusión del Grupo B (los gestos de pantalla quedarían detrás de esta fachada).
- **Nota:** no es una librería de DIBUJO nueva — es la fachada que ordena las existentes (respeta la Ley Primera §0: no se inventa nada, se compone).

*(Opcional de bajo coste: TexturaLib — registro central perezoso de texturas de efecto, para matar el patrón `_tex ??=` repetido en cada librería y unificar la detección de huérfanas de §6.F.)*

---

## 6. TOP 10 MEJORAS POR VALOR/ESFUERZO

| # | Mejora | Librería afectada | Valor | Esfuerzo | Riesgo |
|---|---|---|---|---|---|
| 1 | **Purgar BeamLib + BoltRenderer** (496 líneas, 0 usuarios verificados) y corregir el catálogo §2/§4.5/§4.6 que aún las recomienda | BeamLib, BoltRenderer | Medio (el mapa MIENTE → riesgo de que alguien las use mañana) | Bajo | Casi nulo |
| 2 | **Consolidar helpers en VFXCore** (Tint/Hash01/Ellipse/Capsule/RingQuad/AbrirAdditive-AbrirAlpha): ~125 copias → 6 canónicas, ~900 líneas | Todas (35+ archivos) | Alto (un bug se cura una vez; -2% del VFX) | Medio (mecánico) | Bajo |
| 3 | **Extraer AgujeroBase** de los 10 agujeros negros (familia + helpers compartidos, ~1.250 líneas) | Los 10 *BlackHoleRenderer | Alto (mantenimiento ×10 hoy) | Alto | Medio (regresión visual → sim 1:1 antes/después) |
| 4 | **Unificar gestos de pantalla**: PantallaLib dueño único del pipeline; OndaLib.Kick/Flash y PulsoLib.EmpujarPantalla como alias delgados (no se tocan los 106 llamadas) | PantallaLib, OndaSystem, PulsoLib | Alto (sacudidas hoy se suman sin control) | Medio | Medio-bajo |
| 5 | **Decidir el destino de HazConcentradoLib**: purgar la parte muerta (~313 líneas: Dibujar/AberracionCromatica) o revivirla con un consumidor real documentado | HazConcentradoLib | Medio | Bajo | Bajo |
| 6 | **Purga de métodos muertos verificados** (793 líneas: StormLib 184, FormaLib 89, PyraLib 81, PulsoLib 142…) — lista exacta en §3 | 19 librerías | Medio | Bajo | Bajo |
| 7 | **Matar o revivir el juicio diferido** (AbrirVeredicto/Marcar/Liquidar muertos): o se conecta a TiemposLib como el "clang" del impacto definitivo, o sale | PulsoLib | Alto (si revive) / Medio (si muere) | Medio | Bajo |
| 8 | **TiemposLib (nueva)** — hitstop/ralentí coordinado (§5.1) | Nueva | Alto (el lenguaje del impacto moderno) | Medio-alto | Medio (MP) |
| 9 | **PostFXLib (nueva)** — post-proceso pantalla completa con el patrón RT-seguro de BlackHoleLensSystem (§5.2) | Nueva | Alto | Alto | Alto (rendimiento → techarla con FactorCalidad) |
| 10 | **Reconciliar el códice con la realidad** + telemetría de uso en el F8 (contador de llamadas por librería): VoidQueen muerta, RegisterLayer muerto, MediaResLib/NebulaLib/TelaLib con 1 usuario, AudioLib.Sonar con solo 8 llamadas en 3 archivos frente a la ambición "todo el combate" | EFECTOS-Y-LIBRERIAS.md, DiagnosticoVFXSystem | Medio (la documentación es la memoria de la casa: si miente, el siguiente reinventa mal) | Bajo | Nulo |

**Totales del diagnóstico:** 69 librerías · 43.600 líneas · 2 librerías muertas (BeamLib, BoltRenderer) · ~1 método muerto por cada 40 líneas de API pública (~793 líneas en métodos + 929 en librerías/parts muertas) · ~125 copias de helpers (~1.038 líneas) · ~1.563 líneas de familia duplicada en los agujeros negros · **≈ 3.900 líneas (9%) recuperables sin perder un solo efecto**.

---

### Notas de método (para reproducibilidad)
- Los conteos de uso cuentan ARCHIVOS que nombran la clase/método fuera de `Content/VFX/` (una referencia en comentario cuenta como uso — criterio conservador: puede haber algún muerto menos, no más).
- "Muerto" exige: sin aparición en ningún otro .cs Y sin llamada interna en su propio archivo (excluidos los ganchos del motor tML: Load/PostDraw*/ModifyScreenPosition… y los overrides).
- Muestras verificadas a mano: RegisterLayer, SeekArc, FractalBolt, ForkTree, PuntoImpacto, ConCantidad, DrawOrbitalSystem, BeamLib (solo comentarios), BoltRenderer.ComputeQuads, HazConcentradoLib.Dibujar, VoidQueen, EnVentana, GradienteJuicio, Blit, AbrirVeredicto.
