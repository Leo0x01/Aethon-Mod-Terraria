#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.9 — LA ACTUALIZACIÓN DEL CÓDICE (EFECTOS-Y-LIBRERIAS.md → v1.5).
- §0: LA PILA DE CAPAS HECHA BIEN (strips múltiples + vanilla shader) y
  LA LEY DEL CÓDICE SINCRONIZADO (anti-recurrencia del desync).
- §0: la línea de sprites de rayo pierde a BeamLib (enterrada).
- §4.5/§4.6: BeamLib y BoltRenderer ENTERRADAS (el catálogo ya no miente).
- §4.25: la escalera del rayo marcada PURGADA (.56.6) + TiemposLib +
  ImpactoLib (las librerías nuevas).
- §5.4: el camino GPU portado RETIRADO (.56.9) + LA TÉCNICA DE LA PILA.
- §11: la fila v6.56.9 + arreglo de la fila v6.56.7 duplicada.
"""
import io, os

RUTA = os.path.join(os.path.dirname(__file__), "..", "EFECTOS-Y-LIBRERIAS.md")

with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()

def reemplazar(viejo, nuevo, n=1):
    global t
    assert t.count(viejo) == n, f"ancla ({t.count(viejo)}): {viejo[:70]!r}"
    t = t.replace(viejo, nuevo)

# ============ 2) §0 — LA PILA DE CAPAS HECHA BIEN + LA LEY DEL CÓDICE ============
ancla_ley = """   - **Negro puro en lote aditivo** → suma cero = invisible."""
nueva_ley = """   - **LA PILA DE CAPAS HECHA BIEN — STRIPS, NO QUADS (LEY v6.56.9)** →
     la prohibición de la .56.7 era sobre los QUADS CPU (borde poligonal
     duro + texturas crudas = peines). La forma SANCIONADA de darle capas
     a un haz de shader: **DIBUJAR EL MISMO STRIP VARIAS VECES** (la MISMA
     polilínea, mismos arrays — solo cambia el factor de ancho por capa)
     con **EL SHADER VANILLA DE SPRITES (`Main.pixelShader`)** + una
     textura de PERFIL SUAVE (RampaGlow: 0 en el borde, pico al centro):
     bruma ×2.6-3.6 al 9-20% → glow ×1.75-2.5 con la paleta viajando →
     EL CUERPO del .fx → núcleo ×0.28-0.42 blanco-violeta. Cada borde se
     funde EN CURVA por píxel (cero picos por construcción — verificado
     sim 1:1) y el pipeline es el de TODO Terraria: **GPU-UNIVERSAL**, se
     ve en CUALQUIER máquina (integrada, vieja, sin soporte de .fx — la
     letra: «necesito que cualquier gpu incluso los que no son
     dedicadas deben ver el shader»). LA REGLA DE LAS DISTINTAS
     POLILÍNEAS: si las capas siguen polilíneas DIFERENTES (una curva y
     otra recta), sus bordes se CRUZAN y nacen picos nuevos — misma
     polilínea SIEMPRE. Caso ejemplar: las copias del RC2 (v6.56.9).
   - **Negro puro en lote aditivo** → suma cero = invisible."""
reemplazar(ancla_ley, nueva_ley)

ancla_codice = """---
## 1. EL DESCUBRIMIENTO .fx/.fxc"""
ley_codice = """5. **Los sonidos van en `AI()` gated `!= Server`** en proyectiles (suena en
   SP y en clientes), sin duplicar los del NPC (que corren server-side);
   los de combate pasan por **AudioLib.Sonar** (anti-spam incluido).
6. **LA LEY DEL CÓDICE SINCRONIZADO (v6.56.9 — la anti-recurrencia del
   DESYNC)**: la auditoría de la .56.8 encontró este códice DESINCRONIZADO
   del código en 6 puntos (librerías «con usuarios» que no tenían, la
   «paleta estrella» VoidQueen con 0 usos, la escalera del rayo documentada
   viva tras la purga…) — y como la Ley Primera OBLIGA a consultarlo antes
   de crear efectos, UN CÓDICE QUE MIENTE ES EL MAYOR GENERADOR DE
   DUPLICACIÓN FUTURA. LAS DOS REGLAS QUE QUEDAN:
   **(a) EL MISMO COMMIT**: toda librería/método que nazca, muera o se
   purgue actualiza SU entrada del catálogo (§2-§4) EN LA MISMA ENTREGA —
   el funeral y la partida de nacimiento van juntos o el códice miente.
   **(b) LA MUESTRA ANUAL**: cada auditoría de librerías verifica una
   muestra de entradas del catálogo contra `rg` (¿la librería vive? ¿los
   usuarios listados existen?) — la telemetría del F8 es la vara. Una
   entrada sin verificación reciente es una entrada bajo sospecha.

---
## 1. EL DESCUBRIMIENTO .fx/.fxc"""
# (la ancla original de la lista 5 ya existe; insertamos la 6 después)
reemplazar("""5. **Los sonidos van en `AI()` gated `!= Server`** en proyectiles (suena en
   SP y en clientes), sin duplicar los del NPC (que corren server-side);
   los de combate pasan por **AudioLib.Sonar** (anti-spam incluido).""",
"""5. **Los sonidos van en `AI()` gated `!= Server`** en proyectiles (suena en
   SP y en clientes), sin duplicar los del NPC (que corren server-side);
   los de combate pasan por **AudioLib.Sonar** (anti-spam incluido).
6. **LA LEY DEL CÓDICE SINCRONIZADO (v6.56.9 — la anti-recurrencia del
   DESYNC de la .56.8)**: la auditoría encontró este códice desincronizado
   del código en 6 puntos (librerías «con usuarios» que no tenían, la
   «paleta estrella» VoidQueen con 0 usos, la escalera del rayo documentada
   viva tras la purga…) — y como la Ley Primera OBLIGA a consultarlo antes
   de crear efectos, UN CÓDICE QUE MIENTE ES EL MAYOR GENERADOR DE
   DUPLICACIÓN FUTURA. LAS DOS REGLAS QUE QUEDAN: **(a) EL MISMO COMMIT** —
   toda librería/método que nazca, muera o se purgue actualiza SU entrada
   del catálogo (§2-§4) EN LA MISMA ENTREGA (el funeral y la partida de
   nacimiento van juntos); **(b) LA MUESTRA POR AUDITORÍA** — cada
   auditoría de librerías verifica una muestra de entradas del catálogo
   contra `rg` (¿la librería vive? ¿los usuarios listados existen?). Una
   entrada sin verificación reciente es una entrada bajo sospecha.""")

# la línea de sprites de rayo pierde a BeamLib
reemplazar("""   - **Sprites de rayos** → cero sprites de rayo en la vida: RayoLib,
     StormLib, RayoStrip, BeamLib.""",
"""   - **Sprites de rayos** → cero sprites de rayo en la vida: RayoLib,
     StormLib, RayoStrip. (BeamLib fue enterrada en la v6.56.9 — §4.5.)""")

# ============ 3) §4.5/§4.6 — LAS ENTERRADAS ============
reemplazar("""### 4.5 BeamLib.cs (v6.52.0) — EL RAYO MASIVO DE 7 CAPAS

Raycast suavizado (lerp 0.5-0.9 = motion blur del barrido), cintas de VFXCore sobre espina fBm (3.5% del largo — "vivo, no espástico"), halo ×2.3 → cuerpo → core blanco, luz a lo largo cada 64 px, DIAMANTES VIAJEROS `sin(along/92 − t·9)^7`, tapas y telegraph. `Rayo(origen, dir, largo, ancho, …)` · `Telegrafo(...)`. **Escuela A: NUNCA toca el lote** — encola al buffer de VFXCore. **Usuarios:** PrimerTrazoFajo, RayoConvergente.

### 4.6 BoltRenderer.cs (v6.50.25) — el relámpago determinista 1ª gen
Montado sobre RayoStrip (la cura de los bultos). `ComputeQuads(start, end, seed, flick, …)`. Mundo. **Usuarios:** RunicLightning, NebulaLightning, armas de rayo antiguas.""",
"""### 4.5 BeamLib.cs — ⚰️ ENTERRADA (v6.56.9, auditoría §3)

El rayo masivo de 7 capas (v6.52.0) murió con 0 usuarios: sus consumidores
documentados (PrimerTrazoFajo, RayoConvergente) ya no existen — la búsqueda
negativa de la auditoría lo certificó y esta entrada mintió hasta la v1.4
reconciliada. **Borrada del mod (376 líneas).** Si algún día se necesita un
haz masivo por raycast: el camino GPU del RC2 (§5.4) o RayoLib/RayoStrip.

### 4.6 BoltRenderer.cs — ⚰️ ENTERRADO (v6.56.9, auditoría §3)

El relámpago determinista de 1ª gen (v6.50.25, montado sobre RayoStrip):
todas sus menciones eran comentarios históricos — el propio StormLib lo
sustituyó. **Borrado del mod (120 líneas).** El chisporroteo eléctrico
vivo es StormLib.Bolt (§4.3).""")

# ============ 4) §4.25 — LA ESCALERA PURGADA + LAS NUEVAS ============
reemplazar("""- **RayoConcentradoNivelProjectile.cs** (v6.56.0) — **LA ESCALERA DEL RAYO**: un proyectil para los niveles 1-3 (el nivel viaja en **`ai[1]`** — el array `ai` del tML real llega hasta `ai2`; `ai[3]` es `IndexOutOfRangeException`); la cima son clases propias (el Prisma reformado y el RC2). **v6.56.1: `EscaleraDelRayo.HazLibre` es CRUZADO** — UN solo haz definitivo a la vez entre TODAS las familias de la escalera (niveles + RC2 + Prisma). Escala 0.45 (Rayo del Alba) · 0.60 (Veredicto) · 0.75+aberración (Juicio) · 0.85 **EL PRISMA REFORMADO** (la ESPINA VIVA verlet vestida de la pila completa de plasma — los 7 haces y TODO Dust fuera; la sentencia del Testamento intacta) · 1.0 **EL RC2** (la cima, que conserva su clase propia). `EscaleraDelRayo.HazLibre(player)` evita el doble haz. Los ítems en `Content/Weapons/Definitivas/RayosConcentrados.cs`.""",
"""- **RayoConcentradoNivelProjectile.cs** — ⚰️ **PURGADA en la v6.56.6** (la
  letra: «borra todas las nuevas armas y solo deja al rayo concentrado 2»):
  la escalera (Alba/Veredicto/Juicio), el Prisma, el Partidor, el Réquiem,
  la Ira, el Índice, la Pluma y el Ojo MURIERON con sus proyectiles. El RC2
  es LA ÚNICA arma definitiva y su exclusión vive en su propio CanUseItem.
  (Esta entrada mintió desde la .56.6 — la v1.5 la corrige: LEY DEL CÓDICE
  SINCRONIZADO §0.6.)
- **TiemposLib.cs + TiemposSistema + TiemposGlobalNPC** (v6.56.9) — **EL
  HITSTOP DE LA CASA** (la librería nueva nº1 de la auditoría §5.1):
  `Congelar(ticks)` — el mundo CONTIENE LA RESPIRACIÓN. Solo SP (MP = no-op
  documentado), solo la IA de los NPC (PreAI → false): los proyectiles del
  jugador siguen vivos, los golpes siguen conectando. Cola priorizada por
  duración. Consumidor de nacimiento: la ignición de las copias del RC2.
- **ImpactoLib.cs** (v6.56.9) — **EL MOMENTO DEL GOLPE UNIFICADO** (la
  librería nueva nº3 de la auditoría §5.3): `Golpe(pos, intensidad,
  color)` = kick escalado + flash local + baño de luz en 3 focos;
  `GolpeDeCine(pos, i, ticks, color)` = el golpe + EL HITSTOP. La fachada
  que ordena OndaLib/PantallaLib/TiemposLib (Ley Primera §0: no se inventa
  nada, se compone). Hoy: 88 Kick + 18 Flash + 244 AddLight a mano son LA
  DEUDA — cada migración a esta fachada se documenta aquí.
- **RayosPruebaProjectile.cs** (v6.56.9) — **LAS COPIAS DEL RC2**: el
  mismo conducto con LA PILA DE CAPAS (§0 LEY v6.56.9) y LA RECETA en
  `ai[1]` (0=A equilibrada · 1=B titán · 2=C sueño). El RC2 original
  queda INTACTO; las copias se ajustan hasta el look correcto y la
  ganadora se porta al original. Ignición con ImpactoLib.GolpeDeCine.""")

# ============ 5) §5.4 — el camino portado retirado + LA PILA ============
reemplazar("""- **Uso (v6.56.4 — EL CAMINO GPU PORTADO A LAS ARMAS):** el RC2 conserva su
  strip de 1500 puntos (color prismático viajero, ancho `p^0.8·cbrt·realSize·3500·(1.1+cos·respira)`);
  **HazConcentradoLib.Dibujar(ptsMundo, n, perfil, colores, uTime, uEscalaV)**
  lo trae a escala de arma: remuestrea a 128, rotaciones por tangente,
  `uEscalaV` AUTOMÁTICA por ancho (2·perfilMáx/(texel·2.2), piso 0.18) y el
  COLOR QUE VIAJA + FADE DE LENTE por nodo (la fórmula porNodo de la vieja
  HazFusionEspina, que vive ahora en los llamadores). Los consumidores:
  **la escalera (Alba/Veredicto/Juicio — uTime con SU Flujo), el Prisma (la
  espina verlet Catmull-Rom 44), el Partidor (la carga senoide + el corte
  vertical, flujo −1.15) y la Columna de la Ira (flujo −1.2)**.""",
"""- **Uso:** el RC2 (y sus copias de prueba v6.56.9) dibujan SU strip de
  1500 puntos (color del conjuro viajero, ancho `p^0.8·cbrt·realSize·3500·(1.1+cos·respira)`).
  *(El «camino GPU portado a las armas» — `HazConcentradoLib.Dibujar` —
  fue PURGADO en la v6.56.9: sus consumidores la escalera/Prisma/Partidor/
  Ira murieron en la purga de la .56.6 y la búsqueda negativa de la
  auditoría lo certificó. Lo que vive de esa librería son LOS SOLES RÚNICOS:
  `SolRunico4Puntas` + `SolesRunicosCarga` — LA IMPLOSIÓN v6.56.9: ángulo
  FIJO por hash, cero deriva angular; nacen a 150-240 px y CAEN al centro
  con ease-in vida² — «aparecer lejos y contraerse sin dar vueltas» — +
  `SolesRunicosEstallido`.)*
- **v6.56.9 — LA PILA DE CAPAS DEL CONDUCTO (las copias del RC2):** el
  diagnóstico forense de la captura del jugador: el núcleo saturado ocupaba
  el 99% del halo medido — TODO el universo visual vivía DENTRO de un solo
  strip y NO EXISTÍA envolvente luminosa fuera de la banda (por eso el ojo
  leía «una sola capa», con shader perfecto). LA CURA (LEY §0 v6.56.9): la
  MISMA polilínea dibujada 6 veces — bruma ×2.6-3.6 (RampaGlow, violeta
  profundo al 9-20%) → glow ×1.75-2.5 (la paleta viajando) → glow² (receta
  C) → **EL CUERPO del .fx** → núcleo ×0.28-0.42 (blanco-violeta) → soles
  conducto en las márgenes. Las capas de rampa van con **Main.pixelShader
  (EL VANILLA)** + textura en `gd.Textures[0]` → **GPU-UNIVERSAL**: si el
  .fxc no carga, el cuerpo cae a una tira de CarnePlasma por el vanilla y
  LA PILA SE VE IGUAL — seis capas en cualquier máquina. Verificado: sim
  1:1 con CERO picos de borde (el único salto es el borde de ataque del
  cono, empinado por diseño) + verificación visual 5 capas legibles.""")

# ============ 6) §11 — LA FILA v6.56.9 ============
ancla_fila = """| **v6.56.8** |"""
fila_nueva = """| **v6.56.9** | **LA PILA DE CAPAS GPU-UNIVERSAL** (strips múltiples sobre la MISMA polilínea + Main.pixelShader + RampaGlow — la cura de «una sola capa»: el núcleo ocupaba el 99% del halo, no había envolvente; 3 recetas A/B/C en las COPIAS del RC2, el original intacto) · **LA IMPLOSIÓN DE SOLES** (ángulo fijo, caída radial ease-in — «sin dar vueltas») · **LA AUDITORÍA EJECUTADA**: BeamLib+BoltRenderer+HazConcentradoLib-muerta ENTERRADAS (−1.809 l), 40 métodos muertos purgados (−1.317 l), **LOS HELPERS CANÓNICOS** en VFXCore (Tint/TintAlfa/AbrirAdditive/AbrirAlpha/Capsule/RingQuad — 96 cuerpos delegados) · **TiemposLib (EL HITSTOP) + ImpactoLib (EL GOLPE)** con la ignición de cine de las copias · **LA LEY DEL CÓDICE SINCRONIZADO** (§0.6, la anti-recurrencia del desync de 6 puntos) — y **ESTE CÓDICE (v1.5)** | §0, §4.5-4.6, §4.25, §5.4, §11, AUDITORIA-LIBRERIAS.md |
| **v6.56.8** |"""
reemplazar(ancla_fila, fila_nueva)

# el header de versión
reemplazar("# EFECTOS-Y-LIBRERIAS.md — EL CÓDICE VISUAL Y SONORO DE LA CASA",
           "# EFECTOS-Y-LIBRERIAS.md — EL CÓDICE VISUAL Y SONORO DE LA CASA (v1.5)")

with io.open(RUTA, "w", encoding="utf-8", newline="") as f:
    f.write(t)
print("CÓDICE v1.5 ACTUALIZADO")
