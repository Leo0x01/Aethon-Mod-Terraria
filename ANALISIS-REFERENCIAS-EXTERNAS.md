# ANÁLISIS DE REFERENCIAS EXTERNAS — AethonMod v6.135

**Fecha**: 2026-11-14 · **Pedido**: «Luego analizar el proyecto en busca de referencias externas, el proyecto no debe tener referencia de ningún tipo y todo lo que tenga el código debe ser nuestro propio»

**Metodología**: escaneo completo de los 462 .cs + 3 hjson + build.txt con
grep de: URLs http(s) · nombres de otros mods (Calamity, Entropy, Fargo,
Thorium, Spirit) · créditos/adaptaciones · texturas de vanilla
(`"Terraria/…"`) · usings externos · paquetes NuGet/csproj · assets de audio
e imagen · fuentes tipográficas · el manifiesto del .tmod (526 entradas).

---

## RESUMEN EJECUTIVO

| # | Hallazgo | Gravedad | Estado |
|---|---|---|---|
| 1 | **Armas «de investigación» que recrean armas de Calamity Entropy** | ⚠️ **LA DEUDA REAL** — única referencia a terceros del proyecto | **PENDIENTE — decisión tuya** |
| 2 | Dos clases usan la textura vanilla `Terraria/Images/NPC_113` (el muñeco de entrenamiento) | 🟡 Menor — es la API de vanilla, no material ajeno | Preguntar (opciones abajo) |
| 3 | `SoundID`/`DustID`/`ItemID`… de Terraria (≈2000 referencias) | ✅ Nada que hacer — es el API del juego anfitrión, uso legítimo y necesario | Cerrado |
| 4 | URLs en el código: SOLO las del propio repo (aviso anti-copia) | ✅ Ninguna URL ajena | Cerrado |
| 5 | Música (10 .ogg), sonidos, 503 texturas, sprites | ✅ TODO generado por código de la casa (numpy/PIL — los generadores viven en `tools/`) | Cerrado |
| 6 | Sin paquetes NuGet, sin `modReferences`, sin csproj de terceros | ✅ Cero dependencias | Cerrado |
| 7 | Nombres/mitología de las alas nuevas (Serafín, Valquiria, Quetzal…) | ✅ Conceptos de dominio público, ejecución 100% propia | Cerrado |

**Conclusión**: el proyecto es material propio al 99,9%. La única deuda real
de material de terceros es **la carpeta de Investigaciones**, que tú pediste
crear en sesiones antiguas como material de práctica y que ya está marcada
con la regla «jamás debe salir en un lanzamiento» (R13 en MAESTRO.md).

---

## 1 · LAS ARMAS DE INVESTIGACIÓN (Calamity Entropy) — ⚠️ REQUIERE TU DECISIÓN

**El contexto histórico** (documentado en el propio código): en sesiones
antiguas pediste «investiga y copia al 100% todo sobre el arma Arbitrator II
en CALAMITY ENTROPY, esto sera usado como contenido de prueba» y «copia el
arma the beginning and the end de CALAMITY ENTROPY». La casa las construyó
como **recreaciones de mecánica** (nada de assets de Calamity viaja: ni
sprites ni sonidos ni código suyo — son re-implementaciones desde cero con
las técnicas VFX de la casa), documentadas con sus adaptaciones, y quedaron
separadas del contenido jugable como «práctica».

**La superficie exacta** (nada más toca el nombre):

| Archivo | Qué es | Referencias a «Calamity» |
|---|---|---|
| `Content/Weapons/Investigaciones/ArbitradorII.cs` (+ .png, proyectil) | Recreación del Arbitrator II | 7 menciones en comentarios (la ficha del original y las adaptaciones) |
| `Content/Weapons/Investigaciones/ElPrincipioYElFin.cs` (+ .png) | Recreación de The Beginning and the End | 6 menciones |
| `Content/Weapons/Investigaciones/MiradaDelEterno.cs` | Recreación de la Mirada (re-hecha en v6.93, la sesión de LA LIMPIEZA DE COPYRIGHT) | 0 menciones (ya limpiada) |
| `Content/Weapons/Investigaciones/CetroDagasEterno.cs` | Arma ORIGINAL de la casa (las 5 dagas) que vive en esa carpeta | 1 mención (la cita histórica de la letra) |
| `Content/Players/Investigaciones/SigiloRatero.cs` | El «sigilo» que imita el stealth de rogue de Calamity | 5 menciones |
| `Content/Buffs/Investigaciones/BuffsEternos.cs` | Los buffs de esas armas | 1 mención |
| `Content/Globals/Investigaciones/InvestigacionesGlobalNPC.cs` | El global de esas armas | 2 menciones |
| `Content/Projectiles/Investigaciones/DagaEterna.cs` + `DagaEternaMinion` | Las dagas del cetro (originales de la casa) | 1 mención histórica |
| `Content/Items/Investigaciones/CodiceHabilidades.cs` | El códice de pruebas | 0 |
| `Content/Effects/Procedural/Investigacion/` (4 png) | Texturas solo-para-pruebas (generadas por la casa) | 0 |
| `Content/Items/Bolsas/BolsasCategorias.cs` | La bolsa que las agrupa | 2 menciones |

**LO QUE NO HAY**: ningún asset de Calamity (ni un pixel, ni un byte de
audio, ni una línea de su código), ningún archivo de Calamity en el .tmod,
ninguna referencia en los hjson. Son re-implementaciones mecánicas con
nuestro motor VFX, en nuestra carpeta de pruebas.

**TUS OPCIONES** (decídeme una):

- **Opción A — PURGA TOTAL**: se borra la carpeta de Investigaciones entera
  (armas, player, buffs, global, bolsa, texturas) y toda mención. El
  proyecto queda 100% sin referencia de ningún tipo. *Coste*: se pierde el
  material de práctica y las 5 dagas del Cetro habría que mudarlas de
  carpeta (son originales y el usuario las usa — la v6.126-127 las creó).
- **Opción B — PURGA DE MENCIÓN, ARSENAL INTACTO**: se quedan las armas
  (funcionalmente ya son nuestras, con nuestro motor), pero se limpian los
  ~25 comentarios que nombran Calamity/Entropy y se renombran las armas a
  nombres propios. Cero referencia textual, cero pérdida de contenido.
- **Opción C — TODO IGUAL**: se documenta como está (ya está hecho en
  MAESTRO.md §10) y la carpeta sigue siendo solo-práctica. *Riesgo*: si
  algún día el repo se hace público, un lector verá los nombres de armas de
  Calamity en el código.
- **Opción D — OTRA COSA**: dime qué prefieres.

## 2 · LA TEXTURA VANILLA DEL MUÑECO (NPC_113) — 🟡 menor, tu llamada

`DummyNegroNPC.cs` y `DummyNegroItem.cs` apuntan a
`Terraria/Images/NPC_113` — el sprite del **Target Dummy de vanilla** que ya
viene instalado con Terraria en el ordenador de cada jugador. Es uso de la
API del juego anfitrión (como los 544 `SoundID` o los 446 `DustID`), no una
copia de material ajeno al paquete. Aun así, si quieres CERO referencias
literales a texturas de vanilla, puedo generar un sprite propio de muñeco
con la SForja en la próxima sesión (~30 min). **¿Lo cambio o lo dejo?**

## 3 · LO QUE YA ESTÁ CERRADO (verificado este análisis)

- **Terraria API** (SoundID/DustID/ItemID/ProjectileID/BuffID/NPCID/TileID,
  `Terraria.GameContent` como `FontAssets`): es el contrato del juego
  anfitrión — un mod POR DEFINICIÓN llama a estas APIs. No es material
  reproducible; sin esto no existe el mod. (Las fuentes tipográficas de
  vanilla se usan vía API, no se empaquetan.)
- **SubworldLibrary**: la menciona README/COMPILACION como técnica del
  Mundo Interior, pero `modReferences` está VACÍO y el .tmod no arrastra
  ningún paquete — el submundo usa la implementación propia
  (`SubmundoGrimorio`). Cero dependencia real.
- **La música y los sonidos** (10 .ogg): sintetizados por código propio
  (`tools/gen_musica_*.py` con numpy). La banda v1 se conserva con sus md5
  documentados. Cero samples de terceros.
- **Los sprites** (503 png): todos generados por los scripts de la casa
  (PIL). Los únicos png «prestados» en la historia del proyecto (el puerto
  del dios, v6.53) se BORRARON en la v6.54.0 por tu orden («cero material
  ajeno en el repo») — las AUDIT lo verifican como no-regresión desde
  entonces.
- **Las purgas ya ejecutadas** (el Dazzling Stabber Staff y toda su
  referencia murió en v6.133; las quince alas del prisma en v6.113): cada
  AUDIT verifica que siguen muertos.
- **Los nombres mitológicos de las alas nuevas** (Serafín, Valquiria,
  Quetzal, Fénix, Tengu, Dragón): conceptos culturales de dominio público;
  la ejecución (geometría, paletas, runas, nombres compuestos «del Vacío»)
  es 100% de la casa.
- **El aviso anti-copia dentro del .tmod** (`AethonMod.cs`): cita SOLO
  nuestro propio repo de GitHub.

---

## EL PLANTEO FINAL

El único asunto que de verdad importa es **la carpeta de Investigaciones**.
Las demás categorías o son la API del juego (necesarias), o ya son
generadas por la casa, o ya fueron purgadas y se vigilan. Dime la opción
(A/B/C/D) para las Investigaciones y si quieres el sprite propio para el
muñeco, y lo ejecuto con el protocolo completo (cirugía → gates → audit con
no-regresión «los muertos siguen muertos» → publicación).
