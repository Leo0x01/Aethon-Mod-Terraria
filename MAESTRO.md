# EL LIBRO MAESTRO DE LA CASA — AethonMod v6.146

> **EL DOCUMENTO ÚNICO DE VERDAD.** Todo lo que una IA (o un humano) necesita para
> retomar, entender y continuar este proyecto está aquí. Si este documento
> contradice a otro, **manda este documento**. Última actualización: **v6.146**
> (2026-11-17, release GitHub 448346708). Los .md especializados siguen siendo
> válidos como referencia de detalle; este es el índice vivo y la norma superior.

---

## 0 · IDENTIDAD DEL PROYECTO

| Campo | Valor |
|---|---|
| **Nombre** | AethonMod — «Aethon, the Eternal Grimoire» / «Aethon, el Grimorio Eterno» |
| **Autor** | AethonModTeam (Leo0x01) |
| **Qué es** | Mod de contenido masivo para **Terraria** vía **tModLoader 2026.09.3.1** |
| **Lenguaje** | C# (.NET 8, `net8.0`, LangVersion latest) |
| **Repo** | `https://github.com/Leo0x01/Aethon-Mod-Terraria` (privado durante el desarrollo) |
| **Versión actual** | **6.178** (vive en `AethonMod/build.txt`, línea `version = 6.178`) |
| **Última release** | [v6.146](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.146) — md5 `a5fa29117116c8d8c1e0dfad4991819d`, 19.400.025 B, 528 entradas |
| **Licencia** | Propietaria — © 2026 Leo0x01. VER README §AVISO LEGAL. Nada de terceros. |
| **Idiomas** | Código y comentarios en **español**; localización del juego en es-MX (primario), es-ES, en-US |
| **Tamaño** | 486 archivos .cs · 500 png · 79 librerías VFX · 446 herramientas · 50 versiones publicadas en CHANGES.md |

**La premisa**: el jugador es el portador del Grimorio Eterno, un libro vivo
devorador de jefes. El mod crece POR SESIONES con el usuario (cada versión nace
de SU letra, citada textualmente en CHANGES.md y en los comentarios del código).
Todo es **100% de la casa**: sprites generados por código, música sintetizada
por código, shaders propios, librerías VFX propias. **Cero material de terceros.**

---

## 1 · LAS REGLAS DE LA CASA (no negociables)

### 1.1 La cadena de mando

1. **GitHub manda.** Si el sandbox se recicló y el árbol local quedó atrás:
   `git fetch origin && git reset --hard origin/main` (antes guarda cualquier
   WIP local en stash con mensaje claro). El remoto es la única verdad.
2. **La letra del usuario manda.** Cada petición se cita textual
   («la letra: "…"») en comentarios, CHANGES.md y notas de release. Si la letra
   contradice un documento viejo, gana la letra.
3. **Este documento manda** sobre el resto de la documentación.
4. **El .tmod publicado es el snapshot** — STABLE-SNAPSHOT.md describe el
   estado de la última versión publicada.

### 1.2 El ciclo de una petición (PROTOCOLO OBLIGATORIO)

```
PETICIÓN → [1] SINCRONIZAR (GitHub manda)
         → [2] LEER el sistema afectado (NUNCA tocar a ciegas)
         → [3] PLAN (TodoWrite: cada requisito → una tarea)
         → [4] CIRUGÍA con script persistido en mod/tools/<nombre>_v<versión>.py
         → [5] GATES (oráculo → build REAL → GUARDIÁN → AUDIT → headless)
         → [6] SIM 1:1 + QA VLM cuando haya algo visual
         → [7] PUBLICAR (CHANGES → commit → tag → push → release + CDN)
         → [8] DOCUMENTAR (MAESTRO si cambió la técnica · STABLE-SNAPSHOT)
         → [9] ENTREGABLES a download/ + reply final con qué se hizo
```

Detalles de cada paso:

- **[1] SINCRONIZAR**: `cd /home/z/my-project/mod && git fetch origin && git status`.
  Si hay WIP local no commiteado y origin avanzó: `git stash push -u -m "wip-<contexto>"`
  ANTES de resetear.
- **[2] LEER**: grep/Read del sistema afectado. Las 78 librerías VFX tienen
  cabecera con su versión y su historia. **Antes de crear CUALQUIER efecto se
  lee `EFECTOS-Y-LIBRERIAS.md`** (regla de oro, ver §6).
- **[3] PLAN**: TodoWrite con TODOS los requisitos explícitos de la letra.
- **[4] CIRUGÍA**: JAMÁS editar a mano los archivos generados. Se escribe un
  script Python (persistido en `mod/tools/`) que hace el cambio con asserts
  (`assert viejo in s`) — si el patrón no está, el script MUERE en vez de
  corromper. Editar con la herramienta Edit para parches de una línea.
- **[5] GATES** (ver §5): NADA se publica sin pasar TODOS.
- **[6] SIM 1:1**: todo lo visual se simula en Python (PIL) con los parámetros
  REALES leídos del .cs (nunca valores copiados a mano) y se valida con VLM
  (`z-ai vision`). Un VLM puede dar falso negativo a escala pequeña — hacer
  zoom del panel antes de fallar (lección v6.134/v6.135).
- **[7] PUBLICAR**: ver §5.4 (el publicador).
- **[8] DOCUMENTAR**: efecto nuevo → EFECTOS-Y-LIBRERIAS.md; versión →
  CHANGES.md + STABLE-SNAPSHOT.md; técnica/sistema nuevo → este MAESTRO.
- **[9] ENTREGABLES**: el .tmod, el sim y el informe van a
  `/home/z/my-project/download/` (única carpeta visible para el usuario).

### 1.3 Reglas técnicas de la casa

| # | Regla | Por qué |
|---|---|---|
| R1 | **Cero sprites de rayo**: todo rayo por `RayoLib`/`RayoStrip` | La técnica de primitivas de vértices |
| R2 | **Destellos SIEMPRE con degradado monotónico** (nunca círculos planos) | Estética |
| R3 | **Nubes/niebla/humo SIEMPRE con `BrumaFX`** (nunca óvalos) | El error histórico «óvalos-en-lugar-de-bruma» |
| R4 | **Color premultiplicado en lotes aditivos** | Correctness de render |
| R5 | **`Hash01` determinista en render, cero `Main.rand`** | Determinismo visual |
| R6 | **Convención del lote del llamador** (`VFXCore.ReabrirLoteVanilla()`) | No romper el sprite batch de vanilla |
| R7 | **hjson es-MX + es-ES + en-US SIMÉTRICOS** (paridad de claves exacta) | Localización triple |
| R8 | **Toda arma nueva se registra en DOS sitios**: su archivo + el kit de bolsas (`BolsasCategorias.cs`) / `TestingPlayer.cs` | Que el usuario la encuentre |
| R9 | **`NoWarn` vacío — 0 warnings ES un error** | El oráculo es 0/0 |
| R10 | **Todo .fx NUEVO o modificado viaja con su .fxc recompilado** (pipeline fx_2_0) | tML 2026.09 solo carga .fxc |
| R11 | **Los nombres de clases nuevas en el hjson VAN CON SU SPRITE** (.png con el MISMO nombre de la clase, junto al .cs) | Convención tML autoload |
| R12 | **JAMÁS renombrar la carpeta `AethonMod/`** | El guardián de identidad (AethonMod.cs) huérfana los .plr/.twld |
| R13 | **La carpeta `Content/Weapons/Investigaciones/` es SOLO contenido de prueba de la casa** (v6.136: las recreaciones ajenas murieron — ver §10) | Orden del usuario v6.54.0 + v6.136 |
| R14 | **Sandbox reciclado = restaurar entorno** con `scripts/restaura_entorno_v6121.sh` | Ver §4 |
| R15 | **La versión del mod vive SOLO en `build.txt`** | `version = 6.138` |
| R16 | **Persistir TODO script** (>10 líneas) en `mod/tools/` o `/home/z/my-project/scripts/` | Artefacto recuperable |
| R17 | **Cero secretos en el repo**: el token de GitHub vive SOLO en `remote.origin.url` (nunca en un .md del repo) | Seguridad |
| R18 | **Música/texturas/sonidos = generados por código de la casa** (numpy/PIL) | Cero derechos de terceros |
| R19 | **NINGÚN «catch { }» vacío vuelve a nacer (v6.175)**: todo catch registra (`RegistroSistema.Fallo`) o devuelve. El silencio costó la v6.173 (botones muertos sin pista). El gate mide: `loop_retro_v6175.py` marca regresión si suben de 0 | El loop de retroalimentación |
| R20 | **EL LOOP DE RETROALIMENTACIÓN corre antes de cada release** (`tools/loop_retro_v6175.py`): mide catches vacíos · azar en el dibujo · claves de loc · PNGs y COMPARA con la medición anterior | medir → comparar → arreglar |
| R21 | **EL PREQUEL DEL PREDRAW** (v6.176, la lección de los dos crashes del client.log — 42.000 frames de «Draw was called, but Begin has not yet been called»): TODO PreDraw/PostDraw que dibuja DIRECTO con `Main.spriteBatch.Draw` abre SU lote con `VFXCore.AbrirAdditive()` (o un `Begin` propio) y sale con `VFXCore.RestaurarLoteVanilla()` — JAMÁS un Draw tras un `End` sin `Begin`. El oráculo NO caza los errores de estado de dibujo: solo el patrón los evita | Los dos crashes nacieron el mismo día que el patrón se escribió: la regla es el único antídoto |
| R22 | **EL AZAR DEL DIBUJO SE SIEMBRA** (v6.176, la letra: «semilla fija por entidad (whoAmI·edad), como ya hace TentaculoCosmico»): el polvo decorativo que nace DENTRO del dibujo usa `VFXCore.Hash01(whoAmI·edad, a, b)` — NUNCA `Main.rand` en PreDraw/PostDraw. El loop mide SOLO código (los comentarios no cuentan) | R5 medido y blindado |
| R23 | **LA VIDA CONTADA** (v6.178, la letra: «los proyectiles que lanza el Rey Slime en nivel 26 se quedan en el mapa permanentemente… deben desaparecer en segundos»): el default vanilla de `Projectile.timeLeft` es **3600 ticks = 60 segundos** (verificado contra el decompile) y MUCHOS proyectiles vanilla NO tienen override — TODO proyectil que la casa dispare para un jefe nace con `timeLeft` explícito (180 ticks en las voleas). Nadie dispara nada que no muera en segundos | La arena sembrada de basura eterna |
| R24 | **LA BRUMA DEL DECORADO ES PEQUEÑA** (v6.178, la letra: «la bruma… hace que los fps bajen hasta solo 5 fps… la bruma es demasiada en el fondo»): el cielo del Mundo Interior es un DECORADO de pantalla completa que corre en GPUs integradas con el mundo entero encima — la niebla de sus tentáculos es UNA bruma PEQUEÑA por tentáculo (radio al ras del ancho de SU punta, calidad ≤0.18, alfa bajo) — JAMÁS radios de cientos de px ni varias masas por tentáculo | Los 5 FPS de la v6.176/.177 |
| R25 | **LA CIUDAD VACÍA DEL INTERIOR** (v6.179, LA AUTOPSIA del client.log: «System.IndexOutOfRangeException en NPC.AI_007_FindGoodRestingSpot» 31 s después de entrar): `WorldGen.clearWorld()` limpia TILES, **NO NPCs** — los vecinos del pueblo del mundo real seguían VIVOS dentro del interior corriendo su IA con las coordenadas de casa DEL MUNDO GRANDE (homeTileX 3.400 en un mundo de 800×600 → fuera de rango). Al ENTRAR al interior se apagan TODOS los Main.npc (el interior regenera los suyos; al volver, el disco los devuelve) | El crash silencioso del log |
| R26 | **LA MARCA DE LA LLAMADA** (v6.179, la letra: «no se activa el festin… se invoca mas de 2 veces despues de borrar la lista del festin»): un contador con disparo «== 2» EXACTO JAMÁS puede contar eventos que el jugador no controla (las oleadas parían cabezas de Devorador entre invocación e invocación y el «== 2» ya nunca caía en una llamada de verdad). La regla: el contador cuenta SOLO lo que marca la LLAMADA del portador (ShardPlayer.InvocacionDevoradorPendiente — la marcan el ítem en la mano y el pilar; la gasta el OnSpawn de la cabeza) | El vigía universal de la v6.177 contaba TODO — y TODO contaminaba |
| R27 | **EL ALTAR LEE EL TRÍO MÁS ARMADO** (v6.179, la letra: «los slot… todavia se muestran vacios cuando entro, el item solo vuelve a aparecer si lo desequipo y lo vuelvo a equipar»): el item es CRAFTABLE (5 maderas) — el portador puede llevar DOS; si `Del()` devolvía «el primero que encontraba», el altar leía el VACÍO. La persistencia estaba VIVA (el banco v6.178 lo probó) — el fantasma era el trío equivocado. La regla: cuando buscas UN ítem por tipo para leer SU estado, busca el de MÁS estado | El bug que sobrevivió a un banco de pruebas por mirar el ítem que no era |
| R34 | **EL GUARDIÁN DE LAS LLAVES** (v6.183, la letra: «hay mucho texto en crudo… por que hay tanto texto en crudo»): el mod arma MUCHAS claves de localización CONCATENANDO («Categoría.» + clave) — y una clave USADA que no vive en los hjson se pinta CRUDA en el juego. EL GATE (`tools/audit_claves_codigo_v6183.py`, corre en cada release): TODO literal del código + TODA llamada de los helpers de concatenación (ternarios incluidos) + las bases dinámicas + las variantes de EcoLib (base+N) + paridad es-MX=es-ES exacta y en-US sin faltantes (los autogenerados del build son INFO). Un helper NUEVO que arme claves SE DECLARA en su tabla o el gate no lo ve | Cinco crudos vistos por el usuario que cuatro gates no vieron |
| R33 | **EL BUILD CON CAMINO ABSOLUTO** (v6.182, el build fantasma): `start-tModLoader.sh -build AethonMod` (SOLO el nombre) «compila 0 errores» y empaqueta UNA CÁSCARA VACÍA (9 KB, 3 entradas, versión 1.0 — sin build.txt, sin fuentes: compilar NADA da 0 errores). El camino REAL es el ABSOLUTO: `start-tModLoader.sh -build "$PWD/AethonMod"` (el pipeline siempre lo documentó así — y la desviación casi publica una cáscara). VERIFICAR SIEMPRE: el .tmod pesa ~19 MB, versión y nº de entradas con `tools/parse_tmod.py` | El 0-errores más mentiroso de la casa: compiló la nada |
| R32 | **EL GUARDIÁN DEL HOGAR** (v6.182, la segunda «Excepción silenciosa: IndexOutOfRangeException en NPC.AI_007_FindGoodRestingSpot» — 5 minutos DESPUÉS de entrar al interior): LA CAUSA RAÍZ (decompile NPC.cs:56255) — FindGoodRestingSpot dibuja su rejilla con EL HOGAR del NPC (homeTileX/Y), NO con su posición; R25 mató a los que ENTRARON, pero cualquier vecino que nazca/adentre DESPUÉS con el homeTile del mundo grande revienta igual. EL CERROJO PERMANENTE: dentro del interior, TODO townNPC con hogar fuera de rango lo coge prestado AQUÍ (su tile, clamped) — el crash es imposible venga de donde venga | La ciudad vacía despejó la entrada; el guardián vigila la puerta abierta |
| R31 | **EL ANCLA DE TEXTO ESCALA CON EL TEXTO** (v6.182, la letra: «los textos de los botones en la ventana del pilar no estan centrados y se desbordan»): un helper de texto anclado (`pos − medido·ancla`) que DIBUJA escalado (`esc`) miente: el desplazamiento debe ser `pos − medido·esc·ancla` — sin la escala, el texto queda corrido a la izquierda por `medido·(1−esc)/2` (a esc 0,62 con texto de 202 px: 62 px de desvío, 50 px fuera del botón). Y LA MARCA DE LA LLAMADA REDUX (la misma cepa de R26): una marca «una vez por portador» JAMÁS se arma con «lo primero que nazca cerca» — LOS GUARDIANES DE LAS OLEADAS también son Devoradores (LosSeis) y nacen fuera de pantalla: la marca la pone SOLO la invocación REAL (el ítem y el pilar) y la consume `OnSpawn` de ESA cabeza; las oleadas JAMÁS la queman | El festín que no iniciaba porque una oleada se lo comió primero |
| R29 | **LOS ICONOS PASAN POR EL INVENTARIO** (v6.180, los slots eternos en blanco): las texturas de los ítems VANILLA son LAZY — `TextureAssets.Item[tipo]` sin `Main.GetItemDrawFrame` devuelve un placeholder 1×1 TRANSPARENTE (el icono se "dibuja" y no se ve). TODO icono de ítem del mod pasa por `GetItemDrawFrame`: carga la textura Y respeta el frame animado | El workaround del usuario (desequipar+equipar) era el inventario cargándola |
| R28 | **LAS ANCLAS DE MEMORIA CUENTAN LOS ÍTEMS** (v6.179, el warn «AethonMod mod class still using memory» que siguió vivo): un `static Item` con un ítem del mod dentro sostiene SU ModItem tras la descarga — el barrendero de AethonMod.Unload cuenta `Terraria.Item` como ancla (nulifica el campo; el mundo lo re-crea al volver). Y el banco de pruebas de la puerta JAMÁS deja estado vivo: guarda y devuelve TODAS las variables que toca (la v6.179 lo cazó dejando PuertaViva=true en mundos de verdad) | Dos fugas, un solo barrendero |

### 1.4 Estilo del código

- Comentarios de cabecera estilo casa: `v6.XX — EL NOMBRE DE LA TÉCNICA (la
  letra: «…»)` — cada técnica nace documentada con SU historia.
- Los archivos reconstruidos del .tmod llevan la nota de proveniencia (ej.
  AlasEstilo/AlasAuroraLib, wipe del sandbox v6.56).
- Español en TODO: comentarios, nombres de técnicas, docs.

---

## 2 · ARQUITECTURA DEL MOD — DÓNDE VIVE CADA COSA

```
Aethon-Mod-Terraria/              ← RAÍZ DEL REPO (docs y tools, FUERA del .tmod)
├── MAESTRO.md                    ← ESTE DOCUMENTO (la norma superior)
├── README.md                     ← Presentación + mapa de docs + aviso legal
├── CHANGES.md                    ← Historial: 38 versiones, la letra de cada una
├── STABLE-SNAPSHOT.md            ← Estado de la última publicada (v6.138)
├── CARACTERISTICAS.md            ← Inventario de contenido (qué existe)
├── DISEÑO_DEL_MOD.md             ← Arquitectura de sistemas (cómo funciona)
├── EFECTOS-Y-LIBRERIAS.md        ← EL CÓDICE VISUAL (78 librerías — consulta obligatoria)
├── COMPILACION.md                ← Instalación/compilación/pipeline/publicación
├── HOJA-DE-RUTA.md / ANALISIS-1.0.md / PROMPT-MUNDO-INTERIOR.md / EL-PIPELINE-DE-CREACION.md / LA-SUPERFORJA.md / AUDITORIA-LIBRERIAS.md  ← docs históricos/especializados
├── BandaSonoraV1/                ← Las 5 bandas sonoras originales pre-v6.121 (fuera del .tmod)
├── SPRINTES-FAVORITOS/           ← Sprites maestros de referencia (no viajan en el .tmod)
├── ACTUALIZAR-FUENTE.bat / .sh   ← repo → ModSources (usuario final)
├── tools/                        ← 436 herramientas (ver §8)
└── AethonMod/                    ← EL MOD (lo que se compila)
    ├── build.txt                 # ← LA VERSIÓN (6.138) + side=Both + buildIgnore
    ├── AethonMod.cs              # Punto de entrada + GUARDIÁN DE IDENTIDAD
    ├── AethonMod.csproj          # (viaja en buildIgnore)
    ├── LICENSE                   # aviso corto dentro del .tmod
    ├── Content/                  # EL CONTENIDO (autoload de tML)
    │   ├── AethonConfig.cs / AethonConfigServidor.cs   # configuración ModConfig
    │   ├── Bestiary/ Biomes/ Buffs/ Coreografia/ DrawLayers/ Dusts/ Effects/
    │   ├── Globals/ Items/ NPCs/ Particles/ Players/ Projectiles/ Rarities/
    │   ├── Sounds/ Systems/ Tiles/ UI/ VFX/ Weapons/
    │   └── Localization/         # es-MX_Mods.AethonMod.hjson (PRIMARIO) + es-ES + en-US
    ├── Music/                    # 5 .ogg sintetizados por la casa
    └── sounds/ Content/Sounds/   # 5 .ogg de efectos
```

### 2.1 La raíz del mod

| Archivo | Rol |
|---|---|
| `AethonMod.cs` | Punto de entrada (`Mod`). Carga el GUARDIÁN DE IDENTIDAD: si `Name != "AethonMod"` lanza excepción con instrucciones (protege los guardados). Contiene el aviso anti-copia con URL del repo. |
| `AethonConfig.cs` / `AethonConfigServidor.cs` | ModConfig cliente y servidor (opciones del mod). |

### 2.2 Content/ — los 22 subsistemas

| Carpeta | Qué vive | Piezas clave |
|---|---|---|
| `Players/` | Los ModPlayer (13) | `EscuadronPersistPlayer` (el escuadrón de minions persistente — LA fotografía del mundo real, v6.124 retrato de dagas), `ShardPlayer`, `AlasAuroraPlayer` (equipada/vanidad/estilo), `SigiloRatero` (investigación), `SkillTreePlayer`, `CazaJefesPlayer`, `OcasoPlayer`, `CosmeticPlayer`, `SellosPlayer`, `ApuestasPlayer`, `TestingPlayer` (el kit de pruebas), `TraccionRecuerdoPlayer`, `BranchType` |
| `Weapons/` | Las armas (10 familias) | `GrimoireEternal` (EL LIBRO), `GrimorioFlotante`, `GrimorioHambriento*` (la familia hambrienta), `Investigaciones/` (SOLO investigación — ver §10), `Definitivas/` (las Doce Sentencias), `TresCasas/`, `Cosmic/`, `Sombras/`, `Emperatriz/`, `Clicker/`, `Auras/`, `V20/` |
| `Items/` | Items no-arma | `Alas/` (11 alas — ver §7.3), `Bolsas/` (28 bolsas de categorías — `BolsasCategorias.cs`), `Altar/`, `Cosmetics/`, `Esencias/`, `Llamados/` (invocaciones), `Accessories/`, `Investigaciones/` |
| `Projectiles/` | Los proyectiles (13 familias) | `Cosmetic/` (alas halo, mascotas), `Cosmic/`, `Definitivas/`, `Jefes/`, `Nervioso/`, `Oleadas/`, `Sombras/`, `TresCasas/`, `Investigaciones/` (DagaEterna…), `Auras/`, `Clicker/`, `V20/` |
| `VFX/` | **LAS 78 LIBRERÍAS** | Ver §6 (el catálogo) |
| `Systems/` | Los ModSystem (24) | `DespertarGrimorio(Sistema)`, `EcoRed`/`EcoSistema` (el eco de red), `OleadaHambreMusica`, `ShardLevelSystem`, `ShardHUDSystem`, `ShardSyncSystem`, `SubmundoGrimorio` (EL MUNDO INTERIOR), `SeleccionGrimorioSistema`, `GrimorioFuriaSistema`, `LaCaidaSistema`, `AmbienteOleadaSistema`, `AtaquesCombinadosSistema`, `CalidadFpsSystem`, `DiagnosticoVFXSystem`, `GolpeMotor`, `EsenciasModSistema`, `MascotasTeclaB`, `RecetasCorrupcionSistema`, `AethonLlegadaSistema`, `EscuadronUtil`, `WeaponScaling` |
| `Globals/` | Los Global (25) | `GlobalNPCXP` (XP de jefes), `LoteSeguroFX`, `CosmicProjectileFX`, `CazaJefesGlobalNPC`, `OleadaNPC`, `VacioGlobalNPC`, `FaucesGlobal*`, `InvestigacionesGlobalNPC`, tooltips, drops… |
| `Effects/` | Los assets de efecto | `Procedural/` (66 png — Ruido_0..19, SegundoRuido, SoftGlow, Star, Trail, Vortex, Sol*, Rift*, Lumen*, Bolt*, Jellyfish*, Nova, Pulsar, Comet*, Crescent, CarnePlasma, OblivionBlob, FireRing, RampaGlow, Noise, Slash, DestelloFinal, BlackDisk, BitsChispa, AgujaVacioProj, TajoOscuroProj…), `Textures/` (FireNoiseB, MapaDistorsion, RuidoManchas, RuidoDendritico, ResplandorChico…), `Bruma/` (BrumaBrushes/FX/Noise/System.cs + texturas), `Shaders/` (4 shaders .fx + .fxc), `Investigacion/` (DestelloEstelar, Espacio_1, ResplandorChico, RuidoDendritico) |
| `NPCs/` | NPCs | Los jefes del grimorio, `DummyNegroNPC` (usa textura vanilla NPC_113 — ver §10), bestiario |
| `Buffs/` | Buffs | La Quemadura del Vacío, auras, `Investigaciones/BuffsEternos` |
| `Tiles/` | Tiles | El altar, la puerta del submundo |
| `UI/` | UI del mod | HUD del shard, menús |
| `Dusts/` | Dusts propios | + uso masivo de DustID vanilla (446 refs — es la API, no un asset) |
| `Rarities/` | Rarezas | Las rarezas de las definitivas |
| `Sounds/` + `Music/` | Audio | 10 .ogg TODOS sintetizados por código de la casa (numpy) |
| `Bestiary/ Biomes/ Coreografia/ DrawLayers/ Particles/` | Soporte | Bestiario, biomas, coreografías de ataques, capas de dibujo, partículas |

### 2.3 Los sistemas de juego mayores

1. **EL GRIMORIO ETERNO** — `GrimoireEternal` + `EscuadronPersistPlayer` +
   `DespertarGrimorio*`: el libro invoca UN ESCUADRÓN de minions que persiste
   entre sesiones y mundos (la fotografía del mundo real: niveles, mejoras y
   daños por minion — y desde v6.124 el retrato de las dagas con su tipo y
   daño de nacimiento). El escuadrón aparece tenga o no tenga el grimorio.
2. **LAS DOCE SENTENCIAS** — `Weapons/Definitivas/` + `DefinitivasLib`: las
   armas definitivas masivas (El Juicio de los Dos Soles, La Última Palabra,
   la Ira del Cielo, el Partidor de Mundos, el Réquiem Estelar, el Testamento,
   el Índice del Juicio, la Pluma del Veredicto, el Ojo del Juicio, la
   Devoración Eterna…). Cada una con su librería de firma visual.
3. **LAS 28 BOLSAS** — `BolsasCategorias.cs`: el Cofre del Arsenal entrega
   TODAS las bolsas al entrar al mundo; cada bolsa agrupa su familia (armas,
   alas — 11 desde v6.135 —, esencias, cosméticos…).
4. **EL MUNDO INTERIOR DEL ETERNO** — `SubmundoGrimorio`: el altar devora las
   mil esencias y te traga al interior del dios (800×600 de obsidiana, el
   pilar prisma, tres bandas sonoras rotando, el festín que escala). Usa
   SubworldLibrary de tML.
5. **LA SUPERFORJA (SForja)** — en juego vía menú del grimorio, en desarrollo
   vía `tools/sforja.py` (ver §8): la forja definitiva donde nacen las
   cuchillas (receta `cuchilla`) y las tres armas del juicio.
6. **LAS ALAS AURORA** — 11 alas 100% procedurales (ver §7.3): el motor
   vectorial de plumas/velas con anatomía, pares múltiples (hasta 3 pares =
   arcángel) y runas vivas.
7. **EL AURA DEL VACÍO** — La Forma Eterna: cobro por cercanía + la
   Quemadura del Vacío.
8. **EL ECO DE RED** — `EcoRed`/`EcoSistema`/`ShardSyncSystem`: la
   sincronización multiplayer de los shards y del escuadrón.
9. **EL DESPERTAR** — `DespertarGrimorio*` + `LaCaidaSistema`: la progresión
   del libro (devorar jefes → esencias → niveles → despertar → la caída).
10. **LOS JEFES** — NPCs/ + Projectiles/Jefes/: la Emperatriz, las Fauces,
    el Coro de los Profundos, los jefes de oleada (con GlobalNPC de XP).

---

## 3 · EL ENTORNO DE DESARROLLO (y cómo revivirlo)

### 3.1 Las rutas

| Ruta | Qué es |
|---|---|
| `/home/z/my-project/mod` | EL REPO (clon del GitHub) |
| `/home/z/my-project/scripts/` | Scripts de flujo de sesión (oráculo, restauración, guardianes) |
| `/home/z/my-project/download/` | **ENTREGABLES** (única carpeta visible al usuario) |
| `/home/z/my-project/upload/` | Archivos que sube el usuario |
| `/home/z/my-project/sdk.tar.gz` | Respaldo del dotnet SDK 8.0.404 (204 MB, md5 `ff27aff23e06e596a3e1dff73870061f`) |
| `/tmp/sdk` | SDK extraído (MUERE con el sandbox) |
| `/tmp/tml` | tModLoader 2026.09.3.1 (MUERE con el sandbox) |
| `/home/z/.verify/verify.csproj` | EL ORÁCULO (se regenera con el script) |
| `~/.local/share/Terraria/tModLoader/Mods/` | Donde cae el .tmod al compilar |
| `~/.local/share/Terraria/tModLoader/Mods/enabled.json` | `["AethonMod"]` — el mod activo para el servidor headless |

### 3.2 El sandbox se recicla — el protocolo de reanimación

El sandbox MUERE entre mensajes (procesos y /tmp no sobreviven; /home/z
sobrevive). Al empezar cualquier sesión:

```bash
# 1. ¿el entorno vive?
ls /tmp/tml/start-tModLoader.sh 2>/dev/null || echo "TMP-TML-GONE"
# 2. si murió → restaurar TODO con UN comando:
bash /home/z/my-project/scripts/restaura_entorno.sh
#    (v6.175: recreado tras el recicle TOTAL del sandbox — descarga el
#     SDK 8.0.404 de builds.dotnet.microsoft.com + el tML 2026.09.3.1
#     de GitHub releases → /tmp · oráculo y enabled.json listos)
# 3. pip install hjson   (el módulo se pierde también)
```

Y la regla de la casa: **primero sincronizar el repo** (§1.2 paso 1). Si el
árbol quedó en una versión vieja con WIP a medias, stash + reset a origin/main.

### 3.3 El compilador de la casa (build REAL)

```bash
bash /tmp/tml/start-tModLoader.sh -build /home/z/my-project/mod/AethonMod
# → "Compilation finished with 0 errors and 0 warnings" + Packaging
# el .tmod cae en ~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod
```

### 3.4 El servidor headless (prueba de carga real)

```bash
cd /tmp/tml && timeout 150 bash start-tModLoaderServer.sh -config serverconfig.txt
# gate: 0 "Exception" en el log + la línea "Sandboxing: Aethon, the Eternal Grimoire v6.138"
```

---

## 4 · LOS GATES DE CALIDAD (nada se publica sin TODOS)

| Gate | Comando | Criterio |
|---|---|---|
| **1. ORÁCULO** | `bash /home/z/my-project/scripts/genera_oraculo_v6121.sh` | Compila los ~462 .cs contra las 9 DLLs del tML real → **0 errores / 0 warnings** |
| **2. BUILD REAL** | §3.3 | tML -build → **0/0** + Packaging |
| **3. GUARDIÁN** | `python3 tools/audit_texturas_completas.py` | Toda clase con textura la tiene en disco → **TODO VIVE ✔** |
| **3b. GUARDIÁN DE LAS LLAVES** | `python3 tools/audit_claves_codigo_v6183.py` | Toda clave de loc que el código ARMA vive en los 3 hjson (R34) → **TODO VIVE ✔** |
| **4. AUDIT** | `python3 tools/audit_v6XXX_tmod.py` (se adapta de la anterior cada versión) | 30-40 checks del .tmod: EOF exacto · blobs inflados · hjson×3 == fuente · las firmas de la versión · no-regresiones de purgas · paridad de claves → **TODO VERIFICADO ✔** |
| **5. HEADLESS** | §3.4 | Carga el mod completo en servidor real → **0 excepciones** |
| **6. SIM 1:1 + VLM** | `python3 tools/sim_*.py` + `z-ai vision` | Los parámetros REALES del .cs portados a PIL → paneles medibles + QA visual |

### 4.1 El oráculo por dentro

`genera_oraculo_v6121.sh` escribe `/home/z/.verify/verify.csproj` con
`EnableDefaultCompileItems=false` + `<Compile Include>` recursivo de TODO el
árbol + las 9 referencias del tML (`tModLoader.dll`, `Terraria.dll`,
FNA/XNA…), `TargetFramework net8.0`, `NoWarn` VACÍO (regla R9: 0 warnings).
Es **más estricto que el build de tML** — si el oráculo pasa, el build pasa.

### 4.2 La AUDIT (cómo se adapta cada versión)

Se copia `audit_v6<anterior>_tmod.py` → `audit_v6<nueva>_tmod.py` y se
editan: el número de versión, la letra de la sesión, los checks de identidad
de la versión nueva, y los no-regresión (los muertos siguen muertos). Usa
`tools/parse_tmod.py` (el lector del formato: header tModLoader + índice +
blobs zlib; los .cs NO viajan — viajan COMPILADOS en la DLL, y los sprites
viajan como `.rawimg`).

### 4.2.1 LA LECCIÓN DE LA BARRA (v6.177, la letra: «la barra siempre empieza con 1/0»)

`ModBossBar.ModifyInfo` devuelve **true** para que TUS life/lifeMax
sean los que se dibujen — al devolver **null** la base PISA ambos con
los de la pieza seguida («life = Clamp(nPC.life…); lifeMax =
nPC.lifeMax»), y tu cálculo muere en la firma. Y los dos últimos
parámetros de la firma son **shield y shieldMax** — aunque el nombre
que les pongas diga otra cosa, un valor ≠ 0 ahí viste UN ESCUDO.
(Verificado contra el decompile del tML 2026.09.3.1.)

### 4.3 El publicador

`tools/publicar_v6XXX.sh` — crea el release GitHub (API REST con el PAT que
vive en `remote.origin.url`), sube el .tmod **por `uploads.github.com`**
(lección v6.135: api.github.com da 404 para assets), tolera re-ejecución
(release ya existe → continúa), y **verifica el CDN byte a byte** (md5 del
descargado == md5 del local). Las notas de release se escriben DESDE EL
PRINCIPIO con el título completo (lección v6.133: el heredoc arrastraba las
notas de una versión vieja).

---

## 5 · LAS TÉCNICAS DE LA CASA (el repertorio)

### 5.1 El motor VFX (la espina dorsal)

- **VFXCore** — el núcleo: buffer de GlowQuad + `Begin/Flush/Volcar` +
  `CerrarLoteSiAbierto`/`ReabrirLoteVanilla` (la convención del lote, R6).
  Todo efecto de la casa dibuja EN LOTES ADITIVOS premultiplicados (R4).
- **VFXPalettes** — las paletas canónicas (`Oro`, `Ambar`, `BlancoCalido`,
  la rampa del vacío 150,84,235 / 122,78,188 / 26,14,44, …).
- **Hash01** — el ruido determinista de la casa (R5).
- **EL PATRÓN FACADE**: cada renderer estelar (`*Renderer.cs`) es una fachada
  delgada sobre 2-3 librerías de dominio; `RuneSunRenderer.DrawSunBody` es
  la técnica compartida del "cuerpo solar".

### 5.2 Las técnicas por dominio

| Dominio | Técnica | Librerías |
|---|---|---|
| Rayos | El filamento de primitivas de vértices + el R63-a (rayos reales con investigación) | `RayoLib`, `RayoStrip` |
| Rayos concentrados | LAS SEIS CAPAS del plano del arma real (envolventes + cuerpo que fluye en tramos alineados + bordes que arden) | `HazProyectadoLib` |
| Cuerpos | Re-parametrización de cuerpos compuestos | `MoldeLib`, `FormaLib` |
| Estelas | Perfil de anchura a lo largo del tramo | `EstelaLib` |
| Ondas de choque | Falloff temporal | `OndaLib` + `OndaSystem` |
| Pulso | "El sistema nervioso" — game feel | `PulsoLib`, `PulsoSistema` |
| Cintas | "Una cinta para gobernarlas todas" | `TelaLib` |
| Fuego | Las 37 tablas de fuego | `PyraLib` + `MediaResLib` (pases a media resolución) |
| Calor | La cápsula del conducto | `CalorLib` |
| Tajos | La escritura del filorte (anime) | `TajoLib` |
| Impactos | El golpe de luz | `ImpactoLib` |
| Neblina | LA BRUMA (jamás óvalos, R3) | `Effects/Bruma/*` (Brushes/Noise/FX/System) |
| Nebulosas | Volumétricas | `NebulaLib` |
| Desgarros | Línea de vida del corte + glitch + portales | `RiftLib` (+2 parciales) |
| Sombras | Las devoradoras | `SombrasLib` |
| Espectros | Los fantasmas | `EspectroLib` |
| Sierpes | La camada | `SierpesLib` |
| Códigos vivos | Los códigos | `CodigosLib` |
| Runas/escritura | Los alfabetos vectoriales (Sol/Vacío/Corona/Abismo) | `SigiloLib`, `OrbitaLib` |
| Astrofísica | 18 cuerpos: agujeros negros ×12 (Bruma/Cosmic/Olvido/Umbral/Supremo/Aurora + Ascendidos), enanas, púlsar, magnetar… | `*Renderer.cs` |
| Lente gravitacional | La distorsión unificada | `GravLens` |
| Pantalla | Efectos fullscreen | `PantallaLib` |
| Voz/diálogos | La máquina de escribir dramática | `EcoLib` |
| Audio | La identidad sonora por familia | `AudioLib` |
| Color | El motor del prisma + cíclicas | `ColorPrisma`, `LumenLib` |
| Coreografía | La danza declarativa de ataques cíclicos | `CompasLib` |
| Tiempos | El congelado del mundo | `TiemposLib` |
| Píxel | El motor de píxel art por código | `PixelFxCore` + `PixelFxTresCasas` (10 proyectiles Tres Casas) + `PixelFxPlayer` (liberado para pruebas) |
| Alas | EL MOTOR AURORA | `AlasAuroraLib`, `AlasEstilo`, `AlasVacioLib` |

### 5.3 LA TÉCNICA DEL ALA AURORA (v6.135 — la joya actual)

`AlasEstilo.cs` declara el catálogo (`AlasCatalogo.Todos`, 12 estilos: [0]
default · [1] Solares · [2] Vacío (EL ARCÁNGEL, 3 pares) · [3] Eclipse · [4]
Juicio · [5] Corolas · [6-11] las seis mitológicas) — cada estilo es UNA
LLAMADA `Ala(...)` con ~36 parámetros: paleta Color[3] + tip + textura de
ruido (`Ruido_0..19`) + plumas/largo/plegado/abierto/inclinación/barrido/
curva/anclaje/vientre/afilada/filosa/redonda/runas/hueLargo/flapVel/… +
flags (`lisas`, `escalaGlobal`, `alitas`, `coronas`).

`AlasAuroraLib.Dibujar` construye **hasta TRES PARES** (el arcángel):
1. **CORONAS** (v6.135): ancla 16·esc px SOBRE la espalda, alzada −0.18,
   spread ×0.95, escala clampeada 0.25-0.8.
2. **PRINCIPAL**: la espalda (−4 px), la geometría del estilo.
3. **ALITAS** (v6.132): ancla +16 px (el abdomen), abatida +0.22, ×0.85.

Orden de dibujo: **corona → alitas → principal** (de atrás hacia adelante).
Cada par: `ConstruirPluma` (abanico de ejes con pozos de geometría
reutilizados `_pozosEje*`) → `EmitirMembrana`/`EmitirMembranaRuido` →
`Volcar` (lote aditivo). El ESPEJO: ángulo `−π/2 + lado·δ` + `Curva·lado`
(la cura v6.56.3 — Δ 0.000 px verificado). El ala lisa (`lisas: true`):
`EstirarVelaLisa` con LA LEY DEL FILO (el borde aporta ~0 — los dientes de
sierra son matemáticamente imposibles). Las runas del borde: `DibujarRunas`
con 4 alfabetos (Sol/Vacío/Abismo/Corona).

### 5.4 La SForja y la generación de assets

- **`tools/sforja.py`** — LA FORJA DEFINITIVA: el repertorio completo de
  generación de sprites por código (PIL): HD cuadrado ×8, el comando
  `proyectil` (con manifiesto de hitbox), el sol de la casa parametrizado
  (luz/oscuridad/fusión), fondos tileables, 27 efectos del alma, y las
  recetas de las cuchillas (`cuchilla`) y las tres armas del juicio.
- **`tools/taller_pixelart.py`** — el taller de pixel art.
- Los sprites de items (30×30) se generan con scripts `gen_*` o cirugías
  directas — estilo de la casa: silueta + rampa de paleta + brillo de punta.

### 5.5 La música y el sonido

TODO el audio (10 .ogg) es sintetizado por código (`tools/gen_musica_*.py`,
`gen_sonido_*.py`, numpy → ogg). La banda v1 (pre-v6.121) se conserva en
`BandaSonoraV1/` intacta (orden del usuario: «las viejas no las borres»).
El motor en vivo: `Systems/OleadaHambreMusica` + `VFX/AudioLib` (identidad
sonora por familia — ningún coro de impactos apila el mismo sample).

### 5.6 Los mocks y el QA VLM

- **SIM 1:1**: portar la geometría del C# a PIL leyendo los parámetros DEL
  .CS REAL (regex sobre el catálogo/estilo) — nunca valores a mano. Dibuja
  paneles, MIDE (distancias, ángulos, simetría — asserts numéricos) y suelta
  un veredicto.
- **QA VLM**: `z-ai vision -p "…" -i imagen.png` — preguntas concretas con
  escala 0-10. Lecciones: (a) VLM a escala pequeña = falso negativo → hacer
  zoom; (b) los scores SHIP son 7-9; (c) se itera el sprite hasta SHIP.

---

## 6 · EL CATÁLOGO VFX COMPLETO (78 librerías)

> Consulta obligatoria ANTES de crear cualquier efecto (regla de oro).
> Detalle completo de APIs: `EFECTOS-Y-LIBRERIAS.md`.

**Núcleo**: `VFXCore` (lotes/glowquads) · `VFXPalettes` · `Hash01` (determinista)

**Por dominio** (§5.2 la técnica, aquí el archivo):

- **Alas**: `AlasAuroraLib` (el motor) · `AlasEstilo` (el catálogo de 12) · `AlasVacioLib` (el contexto del vacío + Reiniciar)
- **Escrituras mágicas**: `SigiloLib` (el sol: alfabeto RunasSolares/RunasCorona, CuerpoOro/PuntaOro) · `OrbitaLib` (el vacío: RunasVacio/RunasAbismo, RunaVioleta) · `CodigosLib` · `TajoLib` · `EcoLib` (las voces/dialogo)
- **Rayos y haces**: `RayoLib` · `RayoStrip` · `HazLib` (el contrato del taper) · `HazConcentradoLib` (el destello de 4 puntas del sol rúnico) · `StormLib` (el taper curvo)
- **Cuerpos y formas**: `MoldeLib` · `FormaLib` · `EstelaLib` · `OndaLib`+`OndaSystem` · `PulsoLib`+`PulsoSistema` · `TelaLib` · `SierpesLib` · `SombrasLib` · `EspectroLib` · `RiftLib`(+`.Glitch`+`.Portales`+`.DesgarrosNuevos`) · `NebulaLib` · `CalorLib` · `PyraLib` · `MediaResLib` · `ImpactoLib` · `PantallaLib` · `GravLens` · `TiemposLib` · `CompasLib` · `AudioLib` · `ColorPrisma` · `LumenLib` · `DefinitivasLib` (las Doce Sentencias) · `AuraLib` (los patrones del aura)
- **Astrofísica** (18 renderers): `RuneSunRenderer` (los soles rúnicos + `DrawSunBody` compartido) · 12 agujeros negros (`Bruma/Cosmic/Olvido/Umbral/Supremo/Aurora` × base/`Ascendido`) · `WhiteDwarf/DeadStar/NeutronStar/Pulsar/Magnetar/RedSupergiant/CicloEstelar/CoroEspectral/EnjambrePrismatico/MareaGravitatoria/PenduloJuicio/RelojArena/EclipsePrimordial/AnilloDorsal/ArcCrown/RuneCrown` · `OcasoBurstFX`+`OcasoSystem`
- **Píxel**: `PixelFxCore` (el motor, casi todo internal) · `PixelFxTresCasas` (los 10 proyectiles) · `PixelFxPlayer` (el motor liberado, investigación)

## 7 · LOS RECURSOS (todo generado por la casa)

### 7.1 Texturas de efecto

| Carpeta | Contenido |
|---|---|
| `Content/Effects/Procedural/` | **66 png**: `Ruido_0..Ruido_19` (las 20 del ala/aurora — se usan por nombre en el catálogo), `SegundoRuido`, `SoftGlow`, `RampaGlow`, `Noise`, `Star`, `Trail`, `Vortex`, `Slash`, `DestelloFinal`, `BlackDisk`, `BitsChispa`, `SolFusion/SolLuz/SolOscuro`, `RiftCore/RiftLip/RiftTaper*`, `LumenBlade/LumenBloom/LumenFlare`, `BoltChain/BoltCore/BoltHalo/BoltImpact`, `Jellyfish*`, `NovaBurst`, `PulsarCore`, `CometCrown/CometHead`, `Crescent`, `CarnePlasma`, `OblivionBlob`, `FireRing`, `AgujaVacioProj`, `TajoOscuroProj` |
| `Content/Effects/Textures/` | `FireNoiseB`, `MapaDistorsion`, `RuidoManchas`, `RuidoDendritico`, `ResplandorChico`… |
| `Content/Effects/Bruma/` | `BrumaBrushes.cs`+png, `BrumaNoise.cs`, `BrumaFX.cs`, `BrumaSystem.cs` — LA NEBLINA (R3) |
| `Content/Effects/Shaders/` | 4 shaders `.fx` + `.fxc` compilados (pipeline fx_2_0: `tools/fx20_build.py` + `fx20_validate.py`, R10) |
| `Content/Effects/Investigacion/` | Solo para armas de investigación (§10) |

### 7.2 Audio

`Content/Music/`: `MundoSuenoVacio` (58 BPM) · `MundoGarganta` (92 BPM) ·
`MundoCorazon` (120 BPM) · `OleadaHambre` · `OleadaHambreEpica`.
`Content/Sounds/`: `RunaNaceSol/Vacio`, `RunaCarga`, `RayoConcentradoCarga/Loop`.
Respaldo histórico: `BandaSonoraV1/` (las 5 originales pre-v6.121 con sus md5).

### 7.3 Las alas (v6.135 — 11 items)

| Estilo | Item | Alma |
|---|---|---|
| 1 | Alas Rúnicas Solares | el oro de los soles |
| 2 | Alas Rúnicas del Vacío | **EL ARCÁNGEL: 3 pares (coronas 0.72 + alitas 0.55), ala lisa, runas del vacío** |
| 3 | Alas Rúnicas del Eclipse | el ámbar del cielo tapado |
| 4 | Alas Rúnicas del Juicio | el oro del veredicto |
| 5 | Corolas del Alba | el blanco cálido |
| 6 | Alas del Serafín del Vacío | fuego violeta, 3 pares, curva 0.34 |
| 7 | Alas de la Valquiria Rúnica | 7 plumas de lanza, plata |
| 8 | Alas del Quetzal Emplumado | la serpiente que ondea (curva 0.42), 3 niveles |
| 9 | Alas del Fénix del Renacer | 3 llamas anidadas, hueLargo 0.22 |
| 10 | Alas del Tengu del Crepúsculo | plumas primarias de tinta, glifos ámbar |
| 11 | Alas del Dragón del Vacío | membrana de 2 dedos, 6 velas rojo-vino |

Base común: `AlasPrismaBase` (inyecta `Equipada/AlaEstilo`, `noFallDmg`);
items en `Content/Items/Alas/AlasNuevasV6560.cs` + `AlasMitologicasV6135.cs`;
entrega: `BolsaDeAlasPrisma` (11 alas).

### 7.4 Sprites maestros

`SPRINTES-FAVORITOS/` (GrimorioHambriento, CoroDeLosProfundos,
PlumaDelEscribaSanto) — referencia, no viajan en el .tmod.

## 8 · LAS HERRAMIENTAS (tools/ — 436 archivos)

> Patrones de nombre: `<verbo>_<cosa>_v<versión>.py`. JAMÁS se borran (son
> la historia reproducible). El prefijo `v6XXX` = la versión que las nació.

| Familia | Uso | Ejemplos canónicos |
|---|---|---|
| **audit_** (125) | Auditorías del .tmod por versión (§4.2) | `audit_v6135_tmod.py` (la actual), `audit_texturas_completas.py` (EL GUARDIÁN) |
| **publicar_** (21) | El publicador de releases (§4.3) | `publicar_v6135.sh` (el actual — con lecciones: uploads.github.com, notas desde el principio) |
| **hjson_ / loc_ / insert_hjson_** (28) | Cirugía de localización ×3 | `loc_v6515.py` (la regla: JAMÁS el insert a mano) |
| **docs_** (18) | Cirugía de docs (CHANGES/README/SNAPSHOT) | `docs_v650114.py` |
| **cirugia_** (14) | Cirugías de código con asserts | `cirugia_v692_purga.py` |
| **gen_** (~60) | Generadores de assets (sprites, música, sonido, texturas) | `gen_grimorio_hambriento_v65085.py`, `gen_musica_interior_v6122.py` |
| **sim_** | Sims 1:1 (§5.6) | `sim_alas_v6135.py` |
| **vlm_** (.mjs, 17) | QA visual con el SDK | `vlm_sim_alas_v6129.mjs` |
| **fx20_** | La cadena de shaders | `fx20_build.py`, `fx20_validate.py`, `fx20_parse.py`, `fx20_ctab.py` |
| **parse_tmod / extraer_tmod / marca_estable** | El lector del .tmod y el manifiesto | `parse_tmod.py` |
| **sforja / superforja / taller_pixelart** | LA FORJA (§5.4) | `sforja.py` |
| **mock_** | Mocks 1:1 de sistemas para sims | `mock_rayo_fiel_v650105.py` |
| **sintetiza_runas** | Los alfabetos rúnicos | `sintetiza_runas_v664.py` |

**Scripts del workspace** (`/home/z/my-project/scripts/`):
`genera_oraculo_v6121.sh` (EL ORÁCULO) · `restaura_entorno_v6121.sh` (LA
REANIMACIÓN) · `guardian_v6123.py` · `audit_v6123_tmod.py` · `test_fna_png`
· `test_nvorbis` (tests de formato) · cirugías sueltas.

## 9 · CRONOLOGÍA (38 versiones — la letra de cada una en CHANGES.md)

**La era v6.183**: EL FESTÍN DE LA APARICIÓN (la tercera autopsia: el cerrojo quemado vivía en el .plr — la marca murió entera y la APARICIÓN del Devoramundo es la llave; los guardianes de oleada VETAN la mesa el mismo tick) · LOS ANILLOS SOLARES DEL SELLO (un anillo rúnico solar por FASE: 1 LA RUEDA DE RAYOS · 2 LA ENERGÍA CONCENTRADA · 3 la concentrada + LA CACERÍA del dios) · EL GUARDIÁN DE LAS LLAVES (R34: cinco crudos cazados — Items.TrioMarcaPuesta, ChatSellado0, Bestario, Subtitulo — el gate de loc que faltaba desde v6.29) · EL GUARDIÁN DEL HOGAR v2 (la rejilla de vanilla lee home±7 SIN clamp: margen 12 universal) · EL AJUAR DEL PORTADOR (los 13 regalos viven en su bolsa, dentro del Cofre) · EL ICONO ÉPICO DEL MUNDO INTERIOR (el eclipse con corona dorada + EL MARCO, 3/10 → 8/10) · LA MASCOTA LIMPIA (pixel art de verdad: 8,5/10) · LA VÍSPERA DEL LIBRO SE ESPERA (la llave del despertar 20 ya no se pierde) · LOS INFORMES DE DAÑO Y DE ITEMS (download/).

**La era v6.180**: EL ICONO QUE NACE (la causa raíz de los slots eternos: las texturas de ítems VANILLA son LAZY — `Main.LoadItem` del decompile; TODO icono pasa por `Main.GetItemDrawFrame`) · EL ARMA PROYECTADA MUERE ENTERA (la TÉCNICA vive — el Faro la usa; cero menciones externas en el repo) · EL PEDACITO DEL DEVORAMUNDOS (la aspiradora muere; UNA sección —la cola— a la primera aparición + «un pedacito es suficiente» + reset con la Página en Blanco) · EL ALTAR LEÍBLE (líneas 0.84-0.95 · botones con TEXTO COMPLETO piso 0.62 · «Consagrar» · la sección del MUNDO SUBE de puesto: el botón jamás cae fuera del papel) · EL LORE ENTRELAZADO (las 19 secciones del orden de la letra — el archivo viejo en 4·7·10·13·16 — TODO sellado por Despertar + Cronista menos el Compendio; el botón LORE + la autoría de la primera charla; LORE-ANTES.md + LORE-DESPUES.md) · EL RECUERDO LIMPIO (LA ESTRELLA LIMPIA: el arsenal del jefe 100 % procedural — la textura DestelloFinal muere de su dibujo) · EL SELLADO PEQUEÑO (44×58, el sprite del grimorio normal + SU ojo + la corona de 11 ojos a nivel máximo + las sombras 1→10 + FASE 2/3 con LAS DOS IRAS que persiguen lentas + la coreografía de tres actos).

**La era v6.178**: EL RAYO DE VERDAD (la técnica del arma real decompilada — HazProyectadoLib y sus cuatro texturas de la Forja) · LA BRUMA PEQUEÑA (la cura de los 5 FPS + R24) · LA NOCHE PRESTADA UNIVERSAL (todos los jefes se quedan en el interior) · LA VIDA CONTADA (R23) · EL BANCO DE PRUEBAS de la persistencia (DiagnosticoPersistenciaSistema — la ranura en blanco exorcizada con NOMBRE COSIDO) · LA CORONA FANTASMA curada (VFXCore.Quad solo acumula: sin Flush, NUNCA se dibuja).

**La era v6.177**: EL PARTIDO DE LOS GIGANTES (AuraLib/AethonBoss/AtaqueJefe partidos sin tocar comportamiento) · LA BARRA QUE LLEGA DE VERDAD (ModifyInfo devuelve true — §4.2.1) · LA ASPIRADORA UNIVERSAL (EL VIGÍA del OnSpawn: la 2ª invocación en TODAS partes) · LA PUERTA SIEMPRE ABIERTA (InteriorInvocaGlobalItem) · LA ESCALERA SIN TECHO (nivel 1+kills sin tope, movimientos por tramo) · LAS VOLEAS TEMÁTICAS VANILLA (ProyectilTematico) · EL VISOR DEL PROYECTADO (el plano del haz como plano: mecánica y espíritu, material de la casa) · EL ECLIPSE QUE BARRE (el ataque del Faro rediseñado) · LA PURGA DEL EN-US (paridad 1900=1900).

**La era v6.176**: LA ASPIRADORA QUE ESPERA (la vigilias de 5 s de la letra + el trago simultáneo) · EL GUSANO QUE CALLA (CongeladoPorAspiradora) · EL ARMA DEL HAZ (ProyecciónDelel plano) · LAS TIJERAS DEL LIBRO (nada desborda el papel) · LA PURGA DEL EN (194→0) · EL AZAR SEMBRADO (7 llamadas reales) · R21/R22 en el MAESTRO.

**La era actual (v6.12x-135, el Mundo Interior y las alas)**: v6.116 LA
MÁSCARA DEL PRISMA → v6.117 EL TRAGO DE VERDAD → v6.118 EL CUERPO DEL DIOS
(rework del Mundo Interior) → v6.119 LA CURA DEL CRASH → v6.120 EL LLANO DE
OBSIDIANA + LAS TRES BANDAS → v6.121 EL DEVORO DE LA PUERTA + LA MÚSICA →
v6.122 EL AURA DEL VACÍO + LAS TRES ARMAS DE LA SUPERFORJA → v6.123 LA BOLSA
DE LA SUPERFORJA + EL CUADRO NEGRO MUERTO → v6.124 LA SFORJA + LOS SOLES DE
VERDAD → v6.125 LA FORMA ORIGINAL Y LA LISURA + LOS SOLES RÚNICOS + EL
TAJO-HAZ → v6.126 LA RECONCILIACIÓN + LAS CINCO DAGAS → v6.127 LA CAUSA RAÍZ
DE LA SIERRA (muerta con el sim) + EL CETRO → v6.128 LAS DAGAS DE LA SFORJA
(la receta `daga` + LA LECCIÓN DEL LANCZOS) → v6.129 EL ALA LISA + LA
DEVORACIÓN DE LA PRIMERA VEZ → v6.130 LAS CUCHILLAS DEL ETERNO (la receta
`cuchilla`) + EL OJO ANIMADO → v6.131 LAS ALAS REPARADAS + LA CORONA DE
CUCHILLAS + EL JUGADOR COMPLETO DEVORADO → v6.132 LA MARIPOSA DEL VACÍO (las
alitas) + LA EMBOSCADA + LAS CUCHILLAS DEL STABBER → v6.133 LA COPIA EXACTA
DEL ECLIPSE + LA PURGA DEL STABBER → v6.134 LAS PLUMAS MUEREN → **v6.135 EL
ARCÁNGEL DE 6 ALAS + LAS SEIS MITOLÓGICAS** (la actual).

**Eras anteriores**: v6.89-v6.115 (las bolsas, las definitivas, la Emperatriz,
las Fauces, el Coro) · v6.50.xx (la gran era: 130+ micro-versiones — el
grimorio hambriento, los ojos, los iconos, la mascota) · v6.15-v6.49 (las
librerías VFX, los agujeros negros, los bastones estelares) · v6.0-v6.14
(el núcleo: VFXCore, las paletas, el libro).

**Las purgas** (órdenes del usuario, ya ejecutadas — no-regresión en cada
AUDIT): las QUINCE alas del prisma + Agujas + Cuchillas (v6.113) · el
Dazzling Stabber Staff de Calamity y TODA su referencia (v6.133) · la bolsa
de investigación y sus armas (v6.113) · el puerto del dios (v6.54.0: la
copia verbatim de material ajeno BORRADA — «cero material ajeno en el repo»).

## 10 · LAS INVESTIGACIONES — LA DEUDA CERRADA (v6.136)

La deuda quedó **RESUELTA el 2026-11-14** por orden del usuario: las dos
armas que recreaban material de un mod ajeno (el Arbitrador II y El
Principio y el Fin, pedidas como práctica en sesiones antiguas) **fueron
borradas con todo su árbol** (13 archivos + 4 carpetas: proyectiles,
dagas, almas, onda, 4 marcas, sigilo, GlobalNPC). Se quedan — 100 %
originales de la casa: la Mirada del Eterno (re-hecha limpia en la
v6.93), el Cetro de las Dagas del Eterno y sus cinco cuchillas (la
máquina forjada de cero en la v6.132), el Códice de habilidades y el
motor de píxeles (PixelFxPlayer). El muñeco de pruebas negro también es
de la casa desde la v6.136 (sprite por SForja, sin texturas del juego
base). **Verificación integral**: 0 menciones externas en TODO el
código y la localización; 0 texturas vanilla; las purgas viejas
siguen muertas (audit con no-regresión en cada versión). El informe
histórico queda en `ANALISIS-REFERENCIAS-EXTERNAS.md` (resuelto).

## 11 · CÓMO RETOMAR EL PROYECTO (el protocolo de arranque)

```bash
# 0. Si el sandbox murió: bash /home/z/my-project/scripts/restaura_entorno_v6121.sh
# 1. GitHub manda:
cd /home/z/my-project/mod && git fetch origin && git status --short
git stash list   # ¿hay WIP huérfano? evaluarlo antes de tocar nada
git log --oneline -3 && git ls-remote origin main | cut -c1-7   # ¿local == origin?
# 2. Leer el estado: este MAESTRO.md (§0 la versión) + STABLE-SNAPSHOT.md + el
#    inicio de CHANGES.md (las 3 últimas letras del usuario)
# 3. Verificar entorno (§3.2) y arrancar el plan (§1.2).
```

**El prompt de retomo** está en `PROMPT-RETOMAR.md` (raíz del repo, versión
con placeholder del token) y en `/home/z/my-project/download/` (copia con el
token real embebido — el token NUNCA se commitea al repo, R17).

## 12 · LOS DOCUMENTOS DEL PROYECTO (qué leer y cuándo)

| Documento | Leer cuando |
|---|---|
| **MAESTRO.md** (este) | SIEMPRE primero — la norma superior |
| **STABLE-SNAPSHOT.md** | Al retomar: el estado exacto de la última publicada |
| **CHANGES.md** | Para saber qué pasó en cada versión (la letra del usuario) |
| **EFECTOS-Y-LIBRERIAS.md** | ANTES de crear cualquier efecto (regla de oro) |
| **DISEÑO_DEL_MOD.md** | Para entender un sistema a fondo |
| **CARACTERISTICAS.md** | Para saber qué contenido existe |
| **COMPILACION.md** | Para compilar/publicar (usuario y desarrollo) |
| **PROMPT-RETOMAR.md** | Para arrancar una sesión nueva |
| **ANALISIS-REFERENCIAS-EXTERNAS.md** | La deuda de las Investigaciones |
| README.md | Presentación + aviso legal (los demás son históricos: HOJA-DE-RUTA, ANALISIS-1.0, PROMPT-MUNDO-INTERIOR, EL-PIPELINE-DE-CREACION, LA-SUPERFORJA, AUDITORIA-LIBRERIAS) |

---

*Fin del Libro Maestro. La casa es este documento; el código es su historia.*
