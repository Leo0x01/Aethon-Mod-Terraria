# EL PIPELINE DE CREACIÓN — Cómo se crea CUALQUIER cosa para AethonMod

> 📜 **DOC HISTÓRICA** — Este documento describe una era anterior del
> proyecto. Para el estado vigente y las reglas actuales manda
> **[MAESTRO.md](MAESTRO.md)**; esta copia se conserva como referencia.


> **LA LEY DE LA CASA** (la letra del usuario): *«recuerda poner las herramientas en la carpeta tools dentro del proyecto en github y ademas documentarlo para que puedas leerlo siempre que se necesite crear algo nuevo, ademas recuerda que todo esto se debe combinar con las librerias para la creacion de cualquier cosa o item para nuestro proyecto»*
>
> **Este documento se lee ANTES de crear cualquier cosa nueva** — un arma, un ítem, un NPC, un proyectil, una textura, un diálogo, una bolsa. Nada se crea a mano suelta: TODO pasa por LAS HERRAMIENTAS DE LA CASA (la carpeta `tools/`) y por SUS VERIFICACIONES (el guardián, el oráculo, la auditoría).

---

## 1 · EL MAPA DE LAS HERRAMIENTAS (`tools/`)

| Herramienta | Para qué se usa | Cuándo correrla |
|---|---|---|
| **`superforja.py`** | LA SUPERFORJA — los 12 comandos: crear sprites (la pluma IA), refinarlos (la refina), restaurar la rejilla nativa (el puente), el sprite de la casa (el molde), animar (el espejo), efectos VFX (el alma: 9 efectos), dithering (el tambor), rampas de color (la rampa), atlas y tiras (el atlas), QA visual (el ojo VLM), y LA CADENA completa | **SIEMPRE que se cree un sprite nuevo** — armas, ítems, NPCs, mascotas, altar, bolsas |
| **`gen_altar_v695.py`** | La fuente de verdad del molde: cutout flood-fill, rejilla, paleta del LIBRO, contorno Terraria, halo, highlight | Lo usa la superforja internamente — no se toca |
| **`sync_es_mx_v6564.py`** | EL SYNC v3 del español: es-ES es LA FUENTE ÚNICA; regenera es-MX byte a byte y RESPETA las traducciones del en-US (la lección v6.97.1) | **SIEMPRE que se edite un texto** del mod |
| **`audit_texturas_completas.py`** | **EL GUARDIÁN** — ninguna clase de contenido (ítem/proyectil/NPC/tile/buff…) puede quedarse sin su PNG en disco ni dentro del .tmod. **v6.116 — LA PASADA (4) NUEVA: LAS CONVENCIONES tML** (todo `ModTile` con `HasOutlines` EXIGE su `{textura}_Highlight` en disco Y dentro del .tmod — la lección v6.87 que estaba huérfana en el viejo audit_assets.py; la v6.115 murió en el cliente por exactamente eso). La lección de v6.87 + v6.97 (el servidor headless NO carga texturas; solo el cliente las exige — sin el guardián, el mod se desactiva EN LA MÁQUINA DEL USUARIO) | **EL GATE: TODO build pasa por aquí ANTES de publicarse** |
| **`regen_oracle.py`** | El oráculo: compila los 418+ .cs contra las DLLs reales del tML — 0 errores / 0 warnings | Tras cambiar CUALQUIER .cs |
| **`audit_vXXX_tmod.py`** | La auditoría de cada versión: blobs íntegros, EOF, firmas vivas/muertas, hjson ×3, guardián CJK/IP, LICENSE | Antes de cada release |
| **`parse_tmod.py`** | El lector del .tmod (lo usan las auditorías) | Interno |
| **`publicar_v6110.sh`** | **EL PUBLICADOR** — el paso 5 entero en un comando: valida el PAT, verifica el .tmod (md5+tamaño contra la AUDIT), push de main+tag, crea el release con notas nacidas del STABLE-SNAPSHOT, sube el asset, verifica el CDN byte a byte y rellena la cabecera con el release ID real (la lección de las filas pre-rellenadas: el ID solo se conoce DESPUÉS). Nació en la v6.110, cuando el corte de contexto se llevó el PAT y el release quedó colgado — que nunca más el paso 5 dependa de la memoria del agente | Cuando el gate (a-e) ya pasó y solo falta publicar | 
| **`pat_baul.py`** | **EL BAÚL DEL PAT** (v6.116) — el token de GitHub vive CIFRADO en `tools/pat_baul.dat` (PBKDF2-HMAC-SHA256 ×210.000 + SHA-256-CTR + red Feistel ×8 + rotación + HMAC-SHA256 de integridad); la FRASE queda SOLO con el dueño (jamás en el repo) — el PAT en texto plano no vuelve a tocar ni el repo ni los argumentos del shell | Cuando el publicador necesita el PAT: lo lee solo, o `AETHON_PAT="$(python3 tools/pat_baul.py leer)"` | 

**LA REGLA**: si algo nuevo se puede crear con una herramienta de la casa, SE CREA CON LA HERRAMIENTA. Los sprites con LA SUPERFORJA, los textos con el SYNC, las verificaciones con EL GUARDIÁN + EL ORÁCULO + LA AUDITORÍA. Nunca a mano lo que la casa ya automatizó.

---

## 2 · LA RECETA COMPLETA — crear un ítem/arma/NPC nuevo (de cero a release)

### Paso 1 — EL SPRITE (LA SUPERFORJA)

```bash
cd Aethon-Mod-Terraria

# LA CADENA COMPLETA: borrador IA → rejilla nativa → sprite de la casa →
# tira animada → efecto VFX → montaje → QA VLM (todo en un comando)
python3 tools/superforja.py cadena \
    --nombre MiArmaNueva \
    --prompt "una espada ritual de tinta azul-noche con runas doradas" \
    --ancho 36 --alto 44 --efecto fuego --animacion vida --frames 6 --qa
```

- **EL OJO** (QA VLM) da la nota y los defectos → si hay que corregir:
  `python3 tools/superforja.py refina --nombre MiArmaNueva --nota "…"` y re-forjar con `--borrador`.
- Las herramientas sueltas (`pluma`, `puente`, `molde`, `espejo`, `alma`, `tambor`, `rampa`, `atlas`, `ojo`) para cada caso particular — ver **LA-SUPERFORJA.md**.
- El sprite final queda en `/tmp/superforja_MiArmaNueva/sprite.png` (+ `highlight.png` si es un tile, + `tira.png` si anima).

### Paso 2 — LA CLASE C# (las convenciones de la casa)

- El .cs va en su carpeta de siempre (`Content/Weapons/…`, `Content/Items/…`).
- **El PNG va JUNTO al .cs, con el MISMO nombre de la clase** — la convención tML. (Si el proyectil es puro FX y comparte textura: `public override string Texture => "AethonMod/Content/Effects/…"` — el guardián lo entiende.)
- La tira animada (`tira.png`): `Main.projFrames[Type]` + `AnimationFrameHeight` (la era 2026).
- **Nada de código externo**: todo de la casa (HazLib para haces, PixelFxCore para el motor de píxel, TresCasasProjectile como motor de referencia).

### Paso 3 — LOS TEXTOS (el hjson ×3 con el SYNC v3)

1. Editar **SOLO** `Localization/es-ES_Mods.AethonMod.hjson` (la fuente única).
2. Correr `python3 tools/sync_es_mx_v6564.py`:
   - es-MX se regenera byte a byte (el espejo);
   - el en-US **se respeta** — si el contenido nuevo merece traducción al inglés, se edita el en-US A MANO después del sync (las traducciones viven).
3. Sin el sync, un juego en otra lengua muestra CLAVES CRUDAS.

### Paso 4 — LAS VERIFICACIONES (el gate de publicación)

```bash
# (a) EL ORÁCULO — 0 errores / 0 warnings contra el tML real
python3 tools/regen_oracle.py && cd /home/z/.verify && dotnet build verify.csproj -v q

# (b) EL BUILD REAL — ruta ABSOLUTA o sale un stub v1.0 (la lección v6.90)
bash /tmp/tml/start-tModLoader.sh -build "$PWD/AethonMod"

# (c) EL GUARDIÁN — NINGUNA textura puede faltar (la lección v6.87+v6.97)
python3 tools/audit_texturas_completas.py    # debe decir: TODO VIVE ✔

# (d) LA AUDITORÍA de la versión — copiar la anterior, ajustar versión y firmas
python3 tools/audit_vXXX_tmod.py             # debe decir: TODO VERIFICADO ✔

# (e) EL HEADLESS — el servidor arranca con 0 excepciones
bash /tmp/tml/start-tModLoaderServer.sh      # log: "Mod Load Completed", sin "Disabling Mod"
```

**NINGÚN build se publica si (c) o (d) fallan.** El headless NO prueba texturas — SOLO el guardián las prueba; por eso el guardián es el gate.

### Paso 5 — EL RELEASE

1. `version` en `AethonMod/build.txt` (ej. 6.97.1 para la cura).
2. Commit con el mensaje de la casa (la letra del usuario citada, qué cambió, las verificaciones).
3. Push con el PAT.
4. Release en GitHub con el `.tmod` de `~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod` adjunto.
5. El usuario descarga el `.tmod` del release → carpeta `Mods` del tModLoader.

---

## 3 · LAS LECCIONES QUE NUNCA SE OLVIDAN (el porqué de cada regla)

| Versión | La lección | La regla que nació |
|---|---|---|
| v6.86/87 | El `.tmod` empaquetó sin un PNG de tile y el mod se DESACTIVÓ en el cliente («MissingResourceException… HasOutlines») | `audit_assets.py` con las convenciones (_Highlight etc.) |
| v6.90 | `-build` con ruta relativa empaqueta un STUB v1.0 de 9 KB | Ruta ABSOLUTA siempre |
| **v6.97** | **Dos clases nuevas sin PNG (BolsaTresCasas + TresCasasProjectile): el mod se desactivó COMPLETO en la máquina del usuario** («Disabling Mod: AethonMod»). El headless NO las cazó porque EL SERVIDOR NO CARGA TEXTURAS | **EL GUARDIÁN `audit_texturas_completas.py`** — herencia transitiva (BolsaTresCasas : BolsaCategoria : ModItem), texturas propias y prestadas (el override Texture), las 3 pasadas |
| v6.97.1 | El sync del español BORRÓ las traducciones al inglés del en-US (espejo puro) | **SYNC v3**: es-MX espejo byte a byte, en-US RESPETADO con paridad de claves |
| **v6.115→v6.116** | **El prisma nació sin su `_Highlight` (encendió `HasOutlines`) y el mod se DESACTIVÓ en el CLIENTE al cargar** (`AssetLoadException: AltarPrismaTile_Highlight` en `ModTile.SetupContent:145`). El headless no lo cazó (el servidor no carga texturas) y el check de la v6.87 estaba HUÉRFANO en el viejo audit_assets.py que ya nadie corría | **LA PASADA (4) DEL GUARDIÁN**: `HasOutlines ⇒ {textura}_Highlight` EN DISCO y DENTRO DEL .tmod — el gate completo vive en UNA sola herramienta |
| — | El borrador IA llega siendo JPEG con abrigo de .png | La refina re-encodea a PNG real antes del data URL |
| — | `z-ai image-edit` (CLI) manda `image` y la API exige `images: [{url}]` | La refina llama directo al endpoint |

---

## 4 · LA CASA DE LAS TRES TINTAS (recordatorio de estilo)

- **La paleta del LIBRO**: tinta azul-noche `#182878` · sombras `#0A0A2A` · broches dorados `#D89030`/`#FFC850` · iris fucsia `#F000C0` · llamas teal `#5ADCB4` · runas violeta `#9643EB`. Para una rampa nueva: `python3 tools/superforja.py rampa --base "#D89030"`.
- **Los requisitos del Despertar**: VERDE cumplido · ROJO falta · GRIS OSCURO bloqueado.
- **El contorno Terraria**: lo pone el molde. **El highlight**: `highlight.png` para tiles.
- **QA VLM siempre**: nada entra al juego sin pasar por EL OJO (nota + defectos).

*Documento vivo de la casa — v6.97.1.*
