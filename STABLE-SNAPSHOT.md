# AethonMod — ESTADO ACTUAL (v6.50.31)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: 2026-09-28 (tag `v6.50.31`, release publicada).

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

- **GitHub = fuente de la verdad** (regla de la casa, re-confirmada por el usuario:
  "el repo de github siempre es el verdadero"): tag `v6.50.31`.
- **v6.50.31 = FIXES FORENSES del client.log del usuario** (sesión 28/9/2026, 8
  «Excepción silenciosa» de 3 familias):
  1. **AethonBoss.PreDraw — la pareja Begin-sobre-Begin** (fauces abiertas →
     FlushAdditive reventaba; fauces cerradas → el Begin de la corona): una línea
     (`VFXCore.CerrarLoteSiAbierto()` tras `Cabeza()`) cura los DOS caminos. La
     garganta ardiendo, la corona, las motas y el arco del aliento vuelven a dibujarse.
  2. **AuraLib.Reiniciar — ThreadStateException + leak de GPU** en cada salida de
     mundo (OnWorldUnload corre en el ThreadPool): funeral de texturas al hilo
     principal via `Main.QueueMainThreadAction` (patrón v5.87) + `DesecharLote`.
  3. **FormatException "Expected Re-Logic file format"** = un `.plr` corrupto del
     jugador (NO del mod): borrar el dañado de `Players\`.
  4. **Simetría hjson**: 18 claves activadas y traducidas en es-ES (TownNPCMood del
     Testigo ×8, Labels de config ×9, HollowSanctumBiome ×2 — más 4 DisplayName de
     los proyectiles de las armas de rayo).
- **Build headless 0 errores / 0 warnings** contra tModLoader 2026.07.3.0 REAL (verify.csproj
  reconstruido tras el wipe del sandbox — mismo hash 666f6996 que el build del usuario + el `-build` real).
- **`.tmod` v6.50.31**: 399 entradas, 6.328.798 bytes, tabla auditada byte a byte; DLL
  inspeccionada (DesecharLote/QueueMainThreadAction/CerrarLoteSiAbierto presentes,
  DisposeRuido/DisposeFlipbooks vivos para device-lost).
- **hjson es-ES/en-US simétricos** (auditados en el paquete con parser propio).
- **Servidor headless CARGA sin excepciones** (Sandboxing → Finalizing → Choose World).
- **Release de GitHub** con el `AethonMod.tmod` adjunto:
  <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.50.31>

## 🎮 PENDIENTE DE VERIFICACIÓN EN JUEGO (por el usuario)

Toda la cadena v6.50.24 → v6.50.31 está implementada y build-verificada, pero **el usuario aún
no la ha probado en partida**. Checklist:

1. **EL JEFE AETHON — LOS FIXES v6.50.31 (LO PRIMERO)**: pelear al jefe y verificar que (a) NO
   aparecen «Excepción silenciosa» nuevas en el client.log, (b) la GARGANTA ARDIENDO se ve al
   cargar el aliento, (c) la CORONA DE ANILLOS + las 3 motas orbitantes se ven, (d) el ARCO DE
   RAYO boca→presa durante el Aliento Primordial, (e) al SALIR DEL MUNDO (guardar y salir) NO
   hay ThreadStateException en el log (y el juego no engorda la VRAM con los días).
2. **Las 5 armas de rayo** (bolsa 18): el Bastón de Rayo Primordial, el Arco de Sobretensión
   (anclado a la mano), el **Colmillo de Vena Trueno** (trío naranja+amarillo cayendo con
   recada/parpadeo), el Rúnico y el Perlin (el arco que sigue al cursor).
3. **LA FURIA con el motor de vanilla (v6.50.29)**: la chusma del bioma NACE (5º intento —
   ahora via el motor natural de spawn de Terraria, anillo 0.52-0.7× pantalla, nunca en
   paredes), el **indicador de oleada** abajo-derecha («Oleada k: X %» + barra estilo
   invasión), el guardián por zona al borde del cuadro, el avance por muertes (18 en la 1).
4. **El dado del invierno (v6.50.30)**: Deerclops SOLO al 1 % por jefe de oleada (borde
   opuesto, anuncio propio) — ya NO es guardián de nieve ni del Juicio.
5. **Los diálogos de devorar** (v6.50.30): cada esencia usa la voz del SABOR DE SU JEFE
   (12 jefes × 3 variantes), con tiempo de lectura 4-10 s.
6. **El libro YA NO RUGE al hablar** (v6.50.30 — el rugido solo suena cuando LLEGA un jefe).
7. **El jefe Aethon**: la avalancha del EMERGER murió (una sola volleada por emersión), el
   arte estelar 100 % código (eclipse + fauces + 46 vértebras + leviatán de llegada + fondo
   ColaSierpeSky), la barra de vida con icono, el RAM y el Aliento.
8. **El destello del Sol / Supernova / BlackHole**: la cruz de 8 rayos (`DestelloFinal`) sin
   disco plano (los velos de la capa de UI murieron en v6.50.28).
9. **El brillo de los rayos**: funda gaussiana al 24 % + soft-add — dos rayos cruzándose ya
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
