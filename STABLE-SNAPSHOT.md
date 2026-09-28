# AethonMod — ESTADO ACTUAL (v6.50.32)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: 2026-09-28 (tag `v6.50.32`, release publicada).

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

- **GitHub = fuente de la verdad** (regla de la casa, re-confirmada por el usuario:
  "el repo de github siempre es el verdadero"): tag `v6.50.32`.
- **v6.50.32 = SLIFER, EL DRAGÓN DEL CIELO** (el arte del jefe replicado de las
  referencias del usuario — 100 % código, cero sprites):
  1. **LOS PINCELES DE CÓDIGO**: triángulo (apuntiable, anclado a la base) y rombo
     generados EN RUNTIME desde píxeles (auto-regenerables si el device los pierde) —
     la mejora de precisión que pidió el usuario.
  2. **LA CABEZA**: cráneo en cuña ESCARLATA + HOCICO PLATEADO (40 %) con
     cel-shading + DOS COLMILLOS SABLE de marfil + mandíbula de acero GIRANDO con
     la abertura + dientes-clavija + OJOS almendrados (dorado → cian fase 3 →
     violeta) + LA GEMA AZUL (rombo) + CORONA DE 5 LLAMAS con puntas a brasa.
  3. **EL CUERPO DE DOS TONOS**: lomo escarlata + vientre de pizarra + sombra de
     placa (segmentado) + VELAS DORSALES cada 2 huesos; luz de la columna granate.
  4. **LAS ALAS DE MURCIÉLAGO** (nuevas): 4 struts + paño granate festoneado +
     garra de marfil + brasa del borde; aleteo 1.35 Hz con paño que rezaga; ancladas
     a la 3ª vértebra, arqueadas AL CIELO, detrás de todo el cuerpo.
  5. **Cohesión**: garganta a brasa granate, anillos 76/106·esc, leviatán del fondo
     granate-gris (ojos dorados intactos).
- **v6.50.31 = FIXES FORENSES del client.log** (verificados por el usuario: "bien,
  ya no hay errores"): Begin-sobre-Begin del PreDraw, funeral de texturas al hilo
  principal, .plr corrupto documentado, simetría hjson ×18.
- **Build headless 0 errores / 0 warnings** contra tModLoader 2026.07.3.0 REAL
  (verify.csproj + el `-build` real, mismo hash 666f6996 que el build del usuario).
- **`.tmod` v6.50.32**: 6.330.804 bytes, md5 4a774bef…, 399 entradas auditadas byte
  a byte (tabla→22485, suma comprimida EXACTA, las 399 inflan a su tamaño declarado);
  DLL inspeccionada (11 firmas nuevas PRESENTES, 5 muertas AUSENTES).
- **hjson es-ES/en-US simétricos** (621=621 claves, parser con soporte de bloques ''').
- **Servidor headless CARGA sin excepciones** (Sandboxing → Finalizing → Choose World).
- **Release de GitHub** con el `AethonMod.tmod` adjunto:
  <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.50.32>

## 🎮 PENDIENTE DE VERIFICACIÓN EN JUEGO (por el usuario)

Toda la cadena v6.50.24 → v6.50.32 está implementada y build-verificada, pero **el usuario aún
no la ha probado en partida**. Checklist:

1. **EL JEFE AETHON — EL ARTE DE SLIFER (v6.50.32, LO PRIMERO)**: pelear al jefe y verificar el
   DRAGÓN DEL CIELO: (a) la CABEZA en cuña escarlata con el HOCICO PLATEADO y los DOS
   COLMILLOS SABLE de marfil que sobresalen de la boca cerrada, (b) LA CORONA DE 5 LLAMAS
   con puntas a brasa y EL DIAMANTE AZUL de la frente, (c) LOS OJOS dorados que se vuelven
   CIAN en fase 3 y violeta en la 4+, (d) el CUERPO DE DOS TONOS (lomo escarlata + vientre
   de pizarra) con las velas dorsales cada 2 segmentos, (e) LAS DOS ALAS DE MURCIÉLAGO
   aleteando despacio detrás del cuello (paño granate translúcido, garra de marfil, se
   arquean al CIELO), (f) la mandíbula ABRIÉNDOSE al rugir con el fuego interior en las
   fauces, (g) que TODO se ve tras el terreno al bucear (behindTiles) y que NO hay
   «Excepción silenciosa» nuevas en el client.log.
2. **LOS FIXES v6.50.31 (ya confirmados por el usuario: "bien, ya no hay errores")**: garganta
   al cargar el aliento, corona de anillos, motas, arco boca→presa y salida de mundo limpia.
3. **Las 5 armas de rayo** (bolsa 18): el Bastón de Rayo Primordial, el Arco de Sobretensión
   (anclado a la mano), el **Colmillo de Vena Trueno** (trío naranja+amarillo cayendo con
   recada/parpadeo), el Rúnico y el Perlin (el arco que sigue al cursor).
4. **LA FURIA con el motor de vanilla (v6.50.29)**: la chusma del bioma NACE (5º intento —
   ahora via el motor natural de spawn de Terraria, anillo 0.52-0.7× pantalla, nunca en
   paredes), el **indicador de oleada** abajo-derecha («Oleada k: X %» + barra estilo
   invasión), el guardián por zona al borde del cuadro, el avance por muertes (18 en la 1).
5. **El dado del invierno (v6.50.30)**: Deerclops SOLO al 1 % por jefe de oleada (borde
   opuesto, anuncio propio) — ya NO es guardián de nieve ni del Juicio.
6. **Los diálogos de devorar** (v6.50.30): cada esencia usa la voz del SABOR DE SU JEFE
   (12 jefes × 3 variantes), con tiempo de lectura 4-10 s.
7. **El libro YA NO RUGE al hablar** (v6.50.30 — el rugido solo suena cuando LLEGA un jefe).
8. **El jefe Aethon — combate**: la avalancha del EMERGER murió (una sola volleada por
   emersión), la barra de vida con icono, el RAM y el Aliento.
9. **El destello del Sol / Supernova / BlackHole**: la cruz de 8 rayos (`DestelloFinal`) sin
   disco plano (los velos de la capa de UI murieron en v6.50.28).
10. **El brillo de los rayos**: funda gaussiana al 24 % + soft-add — dos rayos cruzándose ya
    no hacen "cortes" ni clipean.

## 🗑️ DOC-ROT / DEUDA TÉCNICA CONOCIDA (detectada, sin arreglar)

Prioridad baja — arreglar en la próxima sesión de código si el usuario aprueba:

1. **Tooltip de La Carnada** (es-ES y en-US) aún dice "11 = EL JUICIO: los **7** guardianes
   ×15" — desde v6.50.30 son **SEIS** (los anuncios ya dicen SEIS; el tooltip quedó viejo).
2. **Precio de las esencias**: `Item.value = buyPrice(0,10,0,0)` = **10 de ORO**, pero los
   comentarios (TheWitness.cs, EsenciasJefes.cs) y docs dicen "10 de PLATINO". Decidir cuál
   es el valor deseado y alinear código+comentarios+hjson.
3. **Sprites muertos**: `AethonSierpeCabeza/Mandibula/Cola.png` sin ninguna referencia .cs
   (arte viejo v6.50.19-26) — candidatos a borrar. (`AethonBoss.png` y
   `AethonSierpeVertebra.png` SÍ son requeridos por el cargador aunque `PreDraw` devuelva false.)
4. **Los otros 4 jefes no tienen icono `_Head_Boss`** (solo Aethon tiene barra con icono).
5. **Sin bestiario** (cero `SetBestiary` en jefes; segmentos ocultos).
6. **`ParticlePresets.NovaFlash` + `VFXCore.NovaBurst` sin llamadores** (comentarios viejos
   de SunProjectile/BlackHoleProjectile aún las citan).
7. **Código muerto**: `RayoLib.Quad/Angulo/Junta` (pincel de banda pre-primitivas); assets
   retirados aún cargados por convención (BoltHalo/BoltCore/BoltChain → `BandaTex`/`VenaTex`).
8. **TODOs de red**: voces del libro en servidor DEDICADO; `ShardSyncSystem.SyncResonance`
   es un stub heredado (la resonancia viaja por `MsgCronica`).
9. Ítems de prueba pendientes de retirar antes de un release "público": `LevelUpTester`,
   `CarnadaDelGrimorio`, `BossSummonBag`, los 4 tests de VFX.

## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)

1. **El usuario prueba v6.50.31 en juego** con el checklist de arriba — la pelea contra Aethon
   es LA prueba de los fixes (garganta + corona + arco visibles y log limpio).
2. Según lo que reporte: pulir lo que falle (rayos/oleadas/diálogos/destello son los frentes
   calientes).
3. Limpieza de doc-rot (la lista de arriba, ~1 sesión pequeña).
4. Ideas ya investigadas y LISTAS para implementar (material en `research/`):
   - **5 ideas de Coralite** (worklog 2-a): rayo-trío (YA hecho = Vena Trueno), cañón
     electromagnético, rayo persecutor de ReverseFlash, dash-relámpago de la ThunderveinBlade,
     capa "flow" ¼ de ancho.
   - **Eventos cósmicos por nivel** (esqueleto en ShardLevelSystem: "Lluvia de luz estelar,
     Rifts… Fase 10").
   - Accesorio con aura que evoluciona negro→dorado→rojo (idea antigua del usuario;
     AuraLib ya soporta perfiles).

## 🔒 REGLAS INVIOLABLES AL RETOMAR

- **GitHub manda**: `git fetch` + comparar ANTES de tocar nada (el sandbox se resetea y el
  repo local puede quedar atrás).
- **Leer el worklog** (`/home/z/my-project/worklog.md`): historial completo de sesiones
  (R1…R64 + exploraciones 3-a…3-d).
- **No tocar**: el icono del Orbe Cósmico (`CosmicOrbMinion.png` — imagen aportada por el
  usuario, intacta desde v6.01); la fórmula del sello idempotente de `OleadaNPC.Marcar`;
  el motor de spawn de vanilla de la furia (la lección de los 5 intentos — worklog v6.50.29).
- **Al entregar**: build 0/0, auditoría `.tmod`, commit+tag+push+release con `.tmod`,
  entrada en worklog, y actualizar este snapshot + README.

---

## 📜 HISTORIAL DE ESTADO (contexto de versiones)

| Versión | Estado | Notas |
|---|---|---|
| **v6.50.31** | ✅ Build-verificada, ⏳ en juego | FIXES FORENSES del client.log: Begin-sobre-Begin del jefe (garganta/corona/arco no se dibujaban) · funeral de texturas al hilo principal (leak de GPU) · simetría hjson (18 claves es-ES) |
| **v6.50.30** (`1d26716`) | ✅ Build-verificada, ⏳ en juego | El dado del invierno (Deerclops 1 %) · diálogos de devorar por jefe · voz en silencio · avalancha del emerger |
| v6.50.29 | ✅ Build-verificada | **EL MOTOR DE OLEADAS DE VANILLA** (5ª y buena) + indicador de oleada |
| v6.50.28 | ✅ | Muerte del velo: el destello circular plano de la UI |
| v6.50.27 | ✅ | El arte del jefe Aethon 100 % código (la Sierpe Estelar) + halo de rayos al 24 % |
| v6.50.26 | ✅ | Aethon: barra, tamaño, RAM, aliento · rayos: kinks agudos + brillo sin cortes |
| v6.50.24 (`71b35b4`) | ✅ | Colmillo de Vena Trueno (Coralite) + la bolsa 18 + RayoLib al pixel |
| v6.50.18 | ✅ | LOS RAYOS DE TERRARIA DE VERDAD (RayoLib 1:1) + Arc Surge + círculo plano desterrado |
| **stable-v6.01** (`0cc89cf`) | 🔒 PUNTO DE RETORNO SEGURO | "LA GRAN LIMPIEZA" — confirmada estable por el usuario en su día; tag `stable-v6.01` |

> Si todo lo nuevo se rompiera de forma catastrófica: `git checkout stable-v6.01` es el
> punto de retorno documentado (esa versión la jugó y validó el usuario de punta a punta).
> NOTA: entre v6.01 y v6.50.x hay reescrituras masivas (grimorio, furia, rayos) — volver
> atrás también pierde contenido y GUARDADOS del libro/oleadas (el formato de persistencia
> cambió). Volver solo en emergencia.
