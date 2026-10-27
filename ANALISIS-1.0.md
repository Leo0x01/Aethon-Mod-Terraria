# ANÁLISIS DE DISEÑO — CAMINO A LA 1.0

> **Task 68 · v6.50.116** — Análisis sin código. Todo lo aquí listado sale del código real de la v6.50.116.
> Este documento es la base de diseño acordada para las próximas versiones (0.9x → 1.0).

---

## 1 · INVENTARIO ACTUAL DEL GRIMORIO (con números reales)

### 1.1 El ítem — `GrimoireEternal.cs`

| Stat | Valor | Nota |
|---|---|---|
| Daño base | **11** (mágico) | El «once» es temático: escuadrón de 11 |
| Maná | **0** | «El hambre ES el coste» — la regla de la casa |
| Cadencia | 22 / retraso 10, autoReuse | |
| Rareza / valor | Quest / 10 oro | |
| Disparo | **EL EQUINOCCIO**: 1 pareja día/noche (Nightglow dorado + violeta autoguiado) + 1 pareja extra cada 3 niveles | |

### 1.2 Escalado por nivel — `WeaponScaling.cs`

| Dato | Valor |
|---|---|
| Daño mágico | +2,2 %/nivel (sin tope) |
| Daño invocación | +1 %/nivel (sin tope) |
| Crítico | +0,2 %/nivel (tope 100 %) |
| Penetración | +2 % cada 5 niveles (tope 50 %) |
| Velocidad de uso | −0,3 %/nivel (tope −25 %) |
| Vida/Maná máx. | +2 cada 20 / +1 cada 4 niveles |
| Regen | +0,5 vida/s cada 20 · +1 maná/s cada 4 (topes 5 y 10) |
| Reducción de daño | +1 % cada 100 niveles (tope 10 %) |
| Robo de vida | 0,1 % cada 7 niveles (desde nivel 7) |
| Cadena del rayo | +1 salto cada 20 (tope 7) |
| Minion contacto/velocidad/rango | +50 % cada 15/30 niveles · +100 px cada 10 (tope 1500) |
| Hitos | Cada 5 niveles · evento dorado cada 50 |

### 1.3 El escuadrón (v6.50.116)

- **Techo fijo: 11 ojos** (anillo interior 5 + exterior 6 = dos ruedas llenas).
- **La 12.ª invocación FORTALECE** al ojo más rezagado: 4 mejoras máximas × 1,5 acumulables → **×3 el daño del rayo**.
- Con el libro guardado: **corona doble entrelazada** (arcos exterior/interior con medio paso de desfase — fin del apiñamiento).

### 1.4 Niveles / XP

- Nivel máximo **infinito** (config servidor). Curva: **100 × nivel^1,5**.
- XP de mob: **5 × estrellas²** del bestiario · **×3 primera kill** de especie · **×2 hardmode**.
- XP de jefe: 100 000 (Moon Lord) / 25 000 (lifeMax>20 000) / 5 000 (resto) + lifeMax/10 + estrellas + 1,1 × nivel × 111.
- **Esencia de jefe = +1 nivel completo** (7 jefes vanilla).

### 1.5 El hambre (doble reloj)

- **Reloj del libro**: 0→100 % en **1 hora real** (1/216 000). 25 % sale a cazar (sombra que muerde 10 % vida máx/30 t y absorbe sin loot). 50 % el Impaciente. 100 % oleadas.
- Pago: jefe devorado **−10 %** · mob −0,1…5 % (por estrellas) · presa de la sombra −1 %.
- **Reloj del portador**: susurros cada 75 s ×4 momentos → **FURIA** (oleadas contra ti). Kill con el libro limpia; el libro furioso solo descuenta 45 s por kill.

### 1.6 El combate

- **La Sombra de la Página** (única fauce viva): daño 160, drena max(2,5 % vida, 25)/6 t, devoración de 3,5 s con loot completo.
- **El Juicio** (oleada 11 / Carnada): los **6 guardianes** a la vez, ×15 stats, entra 1 cada 15 s. DerrotaOleada10 persistida.
- **La Primera Cena**: el primer jefe de cada tipo se detiene a 100 HP y recibe 7,2 s de coreografía antes de morir con loot.
- Jefes de oleada 100 % aislados de vanilla (sin logros/mazmorra/meteorito/loot).
- **El pacto del guardián**: el Dungeon Guardian se detiene a 250 px, el libro lo devora, +1 nivel, gracia de 2 min.

### 1.7 Personalidad

≈150+ líneas ×3 idiomas: 60 de derrota (20 claves ×3), 36 de bioma, 18 de celos (opina si cambias de arma con hambre), furia, venganza, esencias. Sardónico, glotón, celoso, vengativo.

---

## 2 · OBTENCIÓN: DIAGNÓSTICO Y PROPUESTA CANÓNICA

**Existen 4 vías en código, pero ninguna es la puerta del jugador real:**

1. Bolsa de pruebas SP al entrar al mundo (marcada «eliminar antes de publicar») — **es testing, no diseño**.
2. Crafteo: 1 Fragmento Génesis + 5 lingotes oro/platino — canónica pero invisible para un jugador nuevo.
3. Drop de Génesis de KS/EoC (solo si no tienes grimorio) — llega TARDE para «el inicio del juego».
4. 3 altares antiguos subterráneos — buena idea, poco descubrible.

**Propuesta aprobada para implementar (la 1.0 necesita exactamente esto):**

- **La Caída (día 1, primera noche)**: al caer la primera noche de un mundo nuevo, un cometa de tinta cruza el cielo y el Grimorio aterriza a ~30 pantallas del spawn con cráter y bruma. El libro «escoge» al jugador: nadie más puede levantarlo (en MP, el primero que lo toca queda marcado). Coreografía de 10 s, voz inicial, sin stats que maten al jugador nuevo (los primeros niveles el libro apenas muerde).
- **El Génesis queda como semilla alternativa** (el camino «científico»: quien lo craftea se salta la coreografía pero pierde la voz de La Caída — decisión con sabor).
- Las bolsas de pruebas se mueven a un **ModConfig oculto** (por defecto OFF) para el desarrollo.

## 3 · LA MECÁNICA NUEVA: EL GRIMORIO SE ALIMENTA DE LAS ARMAS

**Opinión: es la mecánica que faltaba.** Convierte el problema universal de Terraria (armas obsoletas acumulándose en cofres) en el bucle central del mod, y es 100 % compatible con cualquier mod porque no necesita saber de DÓNDE viene el arma.

Diseño con guardarraíles:

- **Nutrición por rareza + daño normalizado**: cada arma tiene «calorías» = f(rareza, daño, tier de progresión detectada). Una arma de otro mod se mide por sus stats, no por su origen → nunca rota.
- **Alimentar sacia hambre** (cantidad según calorías) y da **XP limitada** (tope por arma según rareza — comer un arma endgame de los mods grandes no da 40 niveles; da lo mismo que una Moon Lord vanilla por tier).
- **El sabor**: comer un ARCO te presta su puntería (proyectiles del Equinoccio +velocidad), una ESPADA su filo (+daño contacto de los ojos), un LIBRO su maná… el buff dura «hasta la próxima comida». El grimorio es un libro: aprende lo que lee.
- **Anti-explotación**: (a) un mismo «sabor» no se apila; (b) armas únicas/quest del propio mod NO comestibles; (c) confirmación con diálogo («¿Estás seguro? Esto no se puede deshacer»); (d) cooldown de digestión (30 s) con la voz comentando la textura.
- **La excepción de 1 en 100**: al comer un arma, 1 % de que el libro escupe una **Página Asimilada** — versión mejorada del arma comida (+15 % daño, sprite del grimorio). El sueño del coleccionista y el pánico del que alimentó su arma favorita sin guardar.

## 4 · IDEAS POR ETAPA DE PROGRESIÓN VANILLA

| Etapa | Hito activador | Idea |
|---|---|---|
| Día 1 | La Caída | El libro nivel 1-5 solo susurra; los ojos hacen 1 de daño y BLOQUEAN (escudos) más que matan |
| Pre-jefes | 200 kills | La Sombra de la Página se despierta (hoy se regala con 5 madera — pasaría a hito) |
| Rey Slime / EoC | Primer jefe devorado | Primer slot de esencia + el libro aprende «sabor de jefe» (buff pasivo por crónica) |
| EoW / BoC | Nivel 10 | Los ojos ganan la ira en cadena |
| Skeletron | Mazmorra + pacto | El guardián devorado cuenta crónica doble |
| Hardmode | Muro de Carne devorado | **LA SEGUNDA CENA**: el libro se divide — 11 ojos se convierten en 5 «capítulos» (minions fusionados) |
| Mechanicals | 3 almas de esencia | El Equinoccio gana tercera fase (crepúsculo) — 3 colores |
| Plantera/Golem | Nivel 60 | La Sombra muerde desde DOS ángulos (la pinza que ya existió, resucitada como evolución) |
| Moon Lord | Derrota + crónica completa | **LA ÚLTIMA PÁGINA**: el festín total (arma 4 resucitada como recompensa final, no como arma de inicio) |
| Post-todo | Nivel 100 + DerrotaOleada10 | El grimorio revela su nombre verdadero (renombrado del ítem + diálogos nuevos) |

**Regla de oro propuesta**: el grimorio NUNCA reemplaza el árbol de armas vanilla — lo **comenta** (celos), lo **come** (armas) y lo **cobre** (jefes). Terraria sigue siendo Terraria.

## 5 · OPINIÓN DE LANZAMIENTO (Steam / biblioteca tModLoader)

**Está más cerca de lo que parece, pero la 1.0 NO es pulir — es cerrar.** El contenido está (311 .cs, 3 idiomas, jefe propio, 20 bolsas, 390 entradas empacadas limpias). Lo que separa hoy de un lanzamiento digno:

1. **Sacar el contenido de pruebas del flujo real** (bolsas SP, verdugo de niveles, dummies) → ModConfig OFF por defecto. *(bloqueante)*
2. **Método canónico de obtención** (La Caída). *(bloqueante — es la puerta del mod)*
3. **Balance pass de la curva** contra contenido vanilla: +2,2 %/nivel sin tope con nivel infinito rompe tarde (nivel 100 = ×3,2 daño mágico gratis). Tope suave de rendimiento (~nivel 120) o curva logarítmica tras 50.
4. **SetupResearch** (Journey) — hoy 0 resultados en el mod.
5. **Sonido propio** — todo es vanilla re-vestido; 3-4 sonidos (la voz, la mordida, la cena) elevarían muchísimo la identidad.
6. **Prueba MP dura** — el aislamiento de oleadas y EcoRed están diseñados, pero falta la sesión real de verificación.
7. **Wiki/página del workshop** con la progresión documentada.

**Roadmap propuesto**: v6.51 (La Caída + purge de pruebas) → v0.9 (comer armas + balance) → v0.95 (MP + Journey + sonido) → **v1.0** (página de workshop, wiki, la Última Página). El salto 6.50→v0.9 no es retroceso: es declarar que lo que viene ya no es beta.

---

*Documento vivo — actualizar por versión hasta 1.0. Task 68, chat cc462764.*
