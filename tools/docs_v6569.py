#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.56.9 — docs: build.txt + CHANGES + README + STABLE-SNAPSHOT + anexo auditoría."""
import io, os

BASE = os.path.join(os.path.dirname(__file__), "..")

def leer(r):
    with io.open(os.path.join(BASE, r), "r", encoding="utf-8") as f:
        return f.read()

def escribir(r, t):
    with io.open(os.path.join(BASE, r), "w", encoding="utf-8", newline="") as f:
        f.write(t)

# ============ 1) BUILD.TXT ============
t = leer("AethonMod/build.txt")
t = t.replace("version = 6.56.8", "version = 6.56.9")
escribir("AethonMod/build.txt", t)

# ============ 2) CHANGES.MD — la entrada nueva al frente ============
cambios = """# v6.56.9 — LA PILA DE CAPAS GPU-UNIVERSAL, LA IMPLOSIÓN DE SOLES Y LA AUDITORÍA EJECUTADA

**La letra del usuario**: «tengo que decir que ahora se ve igual pero con un color diferente jajaja, realmente añadiste glow, desenfoque y demas efectos visuales y vfx, o es que no se renderisa el shader, tienes que decirme como debe verse… lo que le faltan son varias capas de texturas con mas difuminado, glow entre demas efectos visuales» + «sobre los brillos de soles, no deben dar vuelta hacia el centro, lo que deben hacer es aparecer lejos y contraerse hacia el centro pero sin dar vueltas, como una implosion de brillos de soles» + «ponte a investigar como mejorar el rayo concentrado 2, y copia ese rc2 en varias armas con los cambios diseñados asi no dañamos el codigo original y cambiamos las copias hasta tener la correcta» + «necesito que cualquier gpu incluso los que no son dedicadas deben ver el shader» + «entonces es momento de mejorar las librerias» + «si esto lo solucionaste debes documentarlo para que no vuelva a ocurrir nunca».

## (1) EL DIAGNÓSTICO — el shader SÍ renderizaba: le faltaba MUNDO alrededor
El análisis de la captura nueva del jugador (píxel a píxel + visión independiente) clavó la verdad: el haz de la .56.8 **SÍ tenía** gradiente, textura orgánica y portal rúnico (el .fx funcionaba) — pero **el núcleo saturado ocupaba el 99 % del halo medido** (412 px de núcleo contra 424 de halo en la boca): TODO el universo visual vivía DENTRO de un solo strip de polígono firme y **NO EXISTÍA envolvente luminosa fuera de la banda**. Por eso el ojo leía «una sola capa, sin glow, sin desenfoque»: por más riqueza per-pixel que tenga el shader, la SILUETA era una banda sola. **El shader no estaba roto — le faltaban capas ALREDEDOR.**

## (2) LA CURA — LA PILA DE CAPAS GPU-UNIVERSAL (la ley nueva §0)
La MISMA polilínea de 1500 puntos de la técnica 607df36 se dibuja **SEIS veces**, de atrás hacia adelante, cada una con su ancho y su color: **(1) LA BRUMA** ×2.6-3.6 (RampaGlow, violeta profundo al 9-20 % — el resplandor AMBIENTAL que tiñe el fondo) → **(2) EL GLOW** ×1.75-2.5 (la paleta del conjuro VIAJANDO al 20-28 %) → **(3) EL GLOW²** (receta C: la envolvente interior extra) → **(4) EL CUERPO** (el strip del HazConcentradoShader EXACTO del RC2 — el esqueleto 607df36 INTACTO) → **(5) EL NÚCLEO** ×0.28-0.42 (blanco-violeta al 65-95 % — «el láser dentro de la explosión de energía») → **(6) LOS SOLES CONDUCTO** (los destellos de 4 puntas del accesorio corriendo por las MÁRGENES del haz hacia la punta, al ritmo propio de cada sol).
**POR QUÉ CUALQUIER GPU LA VE** (la letra: «incluso los que no son dedicadas»): las capas 1, 2, 3, 5 y 6 se dibujan con **EL SHADER VANILLA DE SPRITES** (`Main.pixelShader` — el mismo pipeline que dibuja TODO Terraria, presente en cualquier GPU desde 2006) + RampaGlow con textura en `GraphicsDevice.Textures[0]`. El .fx del cuerpo es un PLUS: si no carga, el cuerpo cae a una tira de CarnePlasma por el vanilla y **LA PILA SE VE IGUAL** — seis capas visibles en cualquier máquina, shader o no shader.

## (3) LA PRUEBA ANTI-PICOS (la regla de la casa: «siempre analisa lo que haces»)
Sim 1:1 con la matemática exacta (`tools/sim_pila_v6569.py`): **CERO picos de borde en las tres recetas** pasado el borde de ataque del cono (que es empinado POR DISEÑO — el nacimiento del conducto). El borde de cada capa es el perfil gaussiano de RampaGlow (0 en el borde, pico al centro) fundido por píxel — **los picos de la .56.6 nacieron de los QUADS poligonales y el remolino binario crudo, jamás del strip**. La ley nueva del códice (§0): **PILA DE CAPAS = STRIPS sobre la MISMA polilínea, NUNCA quads** (si las capas siguen polilíneas distintas, sus bordes se cruzan y nacen picos). Verificación visual independiente de la sim: «5 capas distinguibles, glow muy pronunciado, desenfoque significativo, NO hay picos» — 8/10.

## (4) LAS TRES COPIAS — «cambiamos las copias hasta tener la correcta»
El RC2 **original queda INTACTO** (byte a byte su técnica). Tres armas de prueba nuevas disparan la pila con SU receta (viaja en `ai[1]`): **RAYO DE PRUEBA A — LA EQUILIBRADA** (el diseño base: bruma ×2.6 al 9 %, glow ×1.75 al 20 %, núcleo ×0.34 al 80 %, 5 soles), **B — EL TITÁN** (todo al máximo: ×3.1/14 %, ×2.1/28 %, ×0.42/95 %, 8 soles — el haz más gordo) y **C — EL SUEÑO** (el desenfoque máximo: ×3.6/20 %, DOBLE glow ×2.5+×1.9, núcleo suave ×0.28/65 %, 6 soles — todo fundido como a través de un velo). Las tres en la Bolsa de las Definitivas, con iconos procedurales de familia (VLM 8/8/7.5) y tooltips explicando cada receta. La ignición estrena **EL GOLPE DE CINE**: kick escalado + flash local + baño de luz + **EL HITSTOP** — el mundo contiene la respiración 6 ticks mientras el conducto se enciende.

## (5) LA IMPLOSIÓN DE SOLES — «aparecer lejos y contraerse sin dar vueltas»
LA CAUSA: el ángulo de cada sol llevaba `+ tiempo · velocidad` — **ESA era la órbita** que el usuario veía dar vueltas al centro. LA CURA: el término de deriva angular MUERE — el ángulo de cada sol es **SU HASH, FIJO de por vida** (cero órbita) y el radio NACE LEJOS (150-240 px) y CAE al centro con **ease-in (vida²: lento lejos, acelerando al caer — el tirón de la absorción)**. La banda lejana se aprieta con la carga (×1.15→×0.55) y el hervidero aviva — la implosión crece hasta el disparo.

## (6) LA AUDITORÍA EJECUTADA — «es momento de mejorar las librerias»
El plan de `AUDITORIA-LIBRERIAS.md` ejecutado en tres frentes (anexo nuevo en ese archivo):
- **ENTERRADAS**: BeamLib (376 l) + BoltRenderer (120 l) — 0 usuarios verificados — y la parte muerta de HazConcentradoLib (~313 l: Dibujar/AberracionCromatica/DiscoNegro/pozos — murieron con la purga de las 9 variantes). Lo que vive: los soles rúnicos (ahora con LA IMPLOSIÓN). **−1.809 líneas.**
- **40 MÉTODOS MUERTOS PURGADOS** (StormLib SeekArc/FractalBolt/ForkTree/Boil, FormaLib ×9, PyraLib.EmberField, PulsoLib ×6, OndaLib.TelegrafoAnillo, DefinitivasLib ×5, TajoLib.ArcoToca, AuraLib ×21 setters, VoidQueen ENTERA, VFXCore.RegisterLayer + el subsistema de oclusión…): cada uno verificado por búsqueda negativa ANTES de borrar — **1 falso muerto cazado y preservado** (FormaLib.PuntoEnBanda, vivo vía ApuestasProjectiles3). **−1.317 líneas netas.**
- **LOS HELPERS CANÓNICOS EN VFXCore** (la consolidación de las ~125 copias): `VFXCore.Tint` (premul) / `TintAlfa` (alpha-gate) / `AbrirAdditive` + `AbrirAlpha` (con SONDA — el estándar v6.50.58) / `Capsule` / `RingQuad`. **96 cuerpos privados migrados por DELEGACIÓN** en 30 archivos (cero call-sites tocados, la lógica vive UNA vez): Hash01 ×13, Tint premul ×19, TintAlfa ×13, Begin* crudos ×29, Capsule ×11, RingQuad ×10. **Clasificación por CUERPO, no por nombre** (los doc-comments mentían): los Begin Immediate de shader (RuneSun/CicloEstelar) quedan INTACTOS (semántica distinta), las Capsule subpíxel con guard quedan, las Ellipse son parametrización sana.
- **2 LIBRERÍAS NUEVAS con consumidores reales**: **TiemposLib** (EL HITSTOP — Congelar(ticks), solo SP, solo la IA de los NPC, cola priorizada; consumidor: la ignición de cine de las copias) e **ImpactoLib** (EL MOMENTO DEL GOLPE — Golpe(pos, i) = kick+flash+luz, GolpeDeCine = +hitstop; la fachada que ordena OndaLib/PantallaLib). *(PostFXLib y AgujeroBase quedan documentados como siguiente paso — alto riesgo, techo FactorCalidad.)*

## (7) LA ANTI-RECURRENCIA DEL DESYNC — «documentarlo para que no vuelva a ocurrir nunca»
El hallazgo transversal de la auditoría (el códice desincronizado del código en 6 puntos) ya estaba reconciliado en la v1.4 — ahora **LA LEY DEL CÓDICE SINCRONIZADO vive en la Ley Primera (§0.6)**: **(a) EL MISMO COMMIT** — toda librería/método que nazca, muera o se purgue actualiza SU entrada del catálogo EN LA MISMA ENTREGA (el funeral y la partida de nacimiento van juntos); **(b) LA MUESTRA POR AUDITORÍA** — cada auditoría verifica una muestra de entradas del catálogo contra `rg`. Un códice que miente es el mayor generador de duplicación futura (la Ley Primera OBLIGA a consultarlo). Esta versión además CORRIGIÓ dos entradas que mentían: la escalera del rayo (documentada viva tras la purga .56.6) y la fila v6.56.7 duplicada de la bitácora.

## (8) PIPELINE
Oráculo 0 errores/0 warnings (tras cada frente: RC2, purga, helpers, librerías nuevas) · sim 1:1 CERO picos · VLM iconos 8/8/7.5 + sim 5 capas · hjson es-ES fuente + 4 espejos regenerados (sync) · -build REAL · .tmod auditado · headless sin excepciones · códice v1.5 + AUDITORIA anexo + snapshot.

"""
t = leer("CHANGES.md")
escribir("CHANGES.md", cambios + t)

# ============ 3) STABLE-SNAPSHOT.MD — el checklist nuevo al frente ============
snap = """# v6.56.9 — LA PILA DE CAPAS GPU-UNIVERSAL + LA IMPLOSIÓN DE SOLES + LA AUDITORÍA EJECUTADA — EL CHECKLIST

> ⏳ **PUBLICACIÓN PENDIENTE** — el release ID se anota al publicar (la lección de la fila pre-rellena de la .65: JAMÁS copiar el ID de la versión anterior).

**Qué pasó** (la letra): «ahora se ve igual pero con un color diferente… lo que le faltan son varias capas de texturas con mas difuminado, glow entre demas efectos» + «los brillos de soles… aparecer lejos y contraerse hacia el centro pero sin dar vueltas, como una implosion» + «copia ese rc2 en varias armas con los cambios diseñados asi no dañamos el codigo original» + «cualquier gpu incluso los que no son dedicadas deben ver el shader» + «es momento de mejorar las librerias».

1. **LA PILA DE CAPAS (la cura de «una sola capa»)**: [ ] el RAYO DE PRUEBA A muestra BRUMA ambiental alrededor del conducto (violeta difuso que tiñe el fondo); [ ] ENVOLVENTE de glow con la paleta viajando (violeta→morado→rojo→magenta); [ ] el CUERPO de plasma del shader de siempre; [ ] la VENA NÚCLEO blanca-violeta en el centro; [ ] SOLES DE 4 PUNTAS corriendo por las márgenes hacia la punta. **Cómo debe verse: 5-6 capas distinguibles, glow que se extiende MUCHO más allá del núcleo saturado, bordes difuminados.**
2. **LAS TRES RECETAS (elige la ganadora)**: [ ] **A LA EQUILIBRADA** (elegante, el diseño base); [ ] **B EL TITÁN** (el más gordo y dramático — bruma y glow al máximo); [ ] **C EL SUEÑO** (todo fundido, máximo desenfoque, doble glow). El RC2 original queda INTACTO para comparar.
3. **CERO PICOS**: [ ] los bordes de todas las capas son LISOS (sin dientes de sierra ni peines) — la sim 1:1 dio 0 picos; si ves algo raro, captura y reporta.
4. **GPU-INTEGRADA**: [ ] en la máquina del jugador (sin GPU dedicada) la PILA SE VE COMPLETA — las capas usan el pipeline vanilla de Terraria (el .fx es un plus: si falla, la pila sigue).
5. **LA IMPLOSIÓN DE SOLES (la carga)**: [ ] los soles de 4 puntas APARECEN LEJOS y CAEN RECTOS hacia la boca (NADIE da vueltas/orbita); [ ] la caída ACELERA al acercarse; [ ] el enjambre se aprieta conforme la carga llena; [ ] al disparar salen ESCUPIDOS en abanico hacia el rayo.
6. **LA IGNICIÓN DE CINE (las copias)**: [ ] al disparar: EL MUNDO SE CONGELA un instante (los NPC quietos ~6 ticks — solo en un jugador) + kick + flash + baño de luz.
7. **LAS LIBRERÍAS**: [ ] TODO el mod se ve igual que siempre (la purga de 40 métodos muertos + BeamLib/BoltRenderer enterradas + helpers canónicos NO cambió NI UN efecto — solo borró lo muerto y unificó la lógica); [ ] el F8 sin errores nuevos.
8. **LOS CHECKLIST VIEJOS**: el de la .56.8 (paleta del conjuro, boca sin discos) y el de la .56.7 (cero picos en el RC2 original) siguen vigentes.

---

"""
t = leer("STABLE-SNAPSHOT.md")
escribir("STABLE-SNAPSHOT.md", snap + t)

# ============ 4) README.MD — versión + novedades ============
t = leer("README.md")
t = t.replace("> **Versión actual:** 6.56.8", "> **Versión actual:** 6.56.9")
escribir("README.md", t)

# ============ 5) AUDITORIA-LIBRERIAS.MD — el anexo de ejecución ============
anexo = """# AUDITORÍA DE LIBRERÍAS VFX — AethonMod (Task C)

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

"""
t = leer("AUDITORIA-LIBRERIAS.md")
escribir("AUDITORIA-LIBRERIAS.md", anexo + t)

print("DOCS v6.56.9 COMPLETOS: build.txt + CHANGES + SNAPSHOT + README + ANEXO AUDITORÍA")
