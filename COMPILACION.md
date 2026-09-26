# 🛠️ Cómo instalar, compilar y ACTUALIZAR "Aethon, la Luz Primordial"

Este documento explica cómo recibir las versiones del mod de forma fiable y
cómo compilarlo en un `.tmod` jugable en tModLoader 2026.07.3.0 (.NET 8).

---

## ⚠️ LA REGLA DE ORO (léela una vez, ahórrate mundos corruptos)

En tModLoader **el nombre del mod es el nombre de la carpeta que contiene
`build.txt`** (no hay campo `name` en build.txt). Este mod se llama
**`AethonMod`** y tus personajes/mundos guardan sus datos con ese nombre:

- Si compilas el mod desde una carpeta llamada de otra forma, se convierte en
  un mod **distinto** y tus `.plr`/`.twld` aparecerán como corruptos o sin el
  mod instalado. Desde la v6.50.13 el mod **lo detecta al cargar** y te
  muestra cómo arreglarlo (fallar con instrucciones > guardar datos huérfanos).
- La carpeta del mod debe ser una carpeta **directa** de `ModSources`
  (tModLoader no escanea subcarpetas anidadas).

**Desde la v6.50.14 el repo tiene el mod dentro de la subcarpeta `AethonMod/`**
(más fácil de identificar: esa carpeta ES el mod; todo lo demás del repo son
docs y material de desarrollo que no se empaqueta). La carpeta que va a
`ModSources` es **esa subcarpeta**, no la raíz del repo.

Rutas habituales:

| | Ruta |
|---|---|
| ModSources | `Documentos\My Games\Terraria\tModLoader\ModSources` |
| Mods (.tmod) | `Documentos\My Games\Terraria\tModLoader\Mods` |

---

## 🚀 VÍA 1 — Descargar el `.tmod` oficial (sin git, 1 minuto)

1. Abre <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases>.
2. En la última versión, descarga **`AethonMod.tmod`**.
3. Cópialo a la carpeta **Mods** (tabla de arriba), reemplazando cualquier
   `AethonMod.tmod` viejo.
4. tModLoader → **Mods** → activa *Aethon, la Luz Primordial*.
5. **Verifica en la lista de mods que la versión sea la del release.**

El `.tmod` ya lleva dentro el nombre `AethonMod` y la versión correcta:
es la vía sin riesgo de identidad.

---

## 🚀 VÍA 2 — Fuente en ModSources (git, ideal para siempre-a-la-última)

```bash
# UNA SOLA VEZ (clona el repo donde quieras, p. ej. en Documentos):
cd "Documentos"
git clone https://github.com/Leo0x01/Aethon-Mod-Terraria.git
cd Aethon-Mod-Terraria

# INSTALAR LA FUENTE Y ACTUALIZAR SIEMPRE CON EL SCRIPT:
ACTUALIZAR-FUENTE.bat        # Windows
./actualizar-fuente.sh       # Linux / macOS
```

El script hace todo: `git pull` + copia espejo de la subcarpeta
**`AethonMod/`** del repo → `ModSources\AethonMod`. Después, en el juego:
**Mods → Develop Mods → AethonMod → Build + Reload**. El número de versión
del menú de Mods debe subir (`6.50.14`, `6.50.15`, …).

> **A mano** (si prefieres): copia la subcarpeta `AethonMod` del repo DENTRO
> de `ModSources` con ese nombre exacto, de forma que quede
> `ModSources\AethonMod\build.txt`. Para actualizar: `git pull` en el clon y
> vuelve a copiar. No metas el repo entero en ModSources (la raíz del repo no
> tiene `build.txt` y tModLoader no escanea niveles anidados).

> **¿Por qué cambió otra vez?** La v6.50.13 aplanó el repo (el mod en la
> raíz) para que un clon directo cupiera en ModSources. La contrapartida:
> la raíz del repo se confundía con la carpeta del mod y mezclaba docs con
> fuentes. La v6.50.14 vuelve a la estructura clásica y clara — **el mod es
> la subcarpeta `AethonMod`** — y el script de actualización automatiza el
> paso de copia para que "actualizar" siga siendo un solo comando.

### Si no quieres git nunca más

Descarga el `.tmod` de la VÍA 1 (recomendado) o el ZIP del release
("Source code"): descomprímelo y copia **la subcarpeta `AethonMod`** dentro
de `ModSources` (no la carpeta del repo entera), y Build & Reload.

---

## 📦 Requisitos para compilar

1. **.NET SDK 8.0** — <https://dotnet.microsoft.com/download/dotnet/8.0>
   (lo usa tModLoader internamente al compilar mods).
2. **tModLoader** 2026.07.3.0 (rama 1.4-stable, Steam o manual).
3. **Git** (solo para la VÍA 2).

---

## 🔨 Compilación

### Vía oficial (la que usa el juego)

1. tModLoader → **Mods → Develop Mods**.
2. Selecciona **AethonMod** (la carpeta `ModSources\AethonMod` que dejó el
   script — o tu copia manual).
3. **Build + Reload**. El `.tmod` resultante cae en la carpeta **Mods** y se
   activa solo.

> El `.csproj` incluido NO participa en el build del mod (tML compila con
> Roslyn y empaqueta según `build.txt`); existe solo para IntelliSense.

### Vía IDE (opcional, para desarrollar)

Abre `AethonMod/AethonMod.csproj` en Rider/VS/VS Code: el `Import` de
`tModLoader.targets` se resuelve solo cuando el proyecto vive dentro de
ModSources (`..\tModLoader.targets` apunta a ModSources).

### Vía línea de comandos (CI/automatización)

```bash
# Con el tModLoader instalado (ruta de ejemplo de Steam en Windows):
"C:\...\tModLoader\tModLoader.bat" -build "Documentos\My Games\Terraria\tModLoader\ModSources\AethonMod"

# También funciona directamente sobre la subcarpeta del clon (sin pasar por ModSources):
"C:\...\tModLoader\tModLoader.bat" -build "Aethon-Mod-Terraria\AethonMod"
```

`tModLoader -build <carpeta>` compila, empaqueta y sale sin abrir ventana
(es la vía con la que se genera el `AethonMod.tmod` de los releases). El
nombre del mod lo pone la carpeta que le pases: pásale la subcarpeta
`AethonMod`.

---

## 🧪 Testeo en juego

1. Compila/instala (arriba) y activa el mod.
2. Entra a un mundo (un jugador): recibes **LA BOLSA DEL ARSENAL
   PRIMORDIAL** — el kit de pruebas completo en una sola ranura.
3. Clic derecho sobre la bolsa: se despliega TODO el arsenal.
4. Mata enemigos con daño Ranged/Melee/Magic → el fragmento se "imprime".
5. **K** = árbol de habilidades · **J** = códex de memoria · **F8** = panel
   de diagnóstico VFX del probador.

---

## 🐛 Solución de problemas

| Síntoma | Causa y solución |
|---------|------------------|
| "Personaje/mundo corrupto" al cargar | Estás corriendo una build vieja con bugs ya reparados, o el mod se compiló desde una carpeta con otro nombre. Actualiza por VÍA 1 (`.tmod` del release) y comprueba la versión en el menú Mods. |
| El mod se llama distinto en la lista | La carpeta de fuentes no se llama `AethonMod`. El guardián de identidad te dirá cómo arreglarlo: la subcarpeta `AethonMod` del repo debe quedar como `ModSources\AethonMod`. |
| El número de versión no sube | Tu carpeta `ModSources\AethonMod` es una copia suelta: no hay nada que actualizar. Haz `git pull` en el clon y ejecuta `ACTUALIZAR-FUENTE.bat` (o re-copia la subcarpeta), o pasa a la VÍA 1. |
| El mod no aparece en Develop Mods | `build.txt` debe quedar en `ModSources\AethonMod\build.txt`: copiaste la carpeta del REPO en vez de la SUBCARPETA `AethonMod`, o la anidaste demasiado. Usa el script de actualización. |
| Build falla con errores CS | Falta el .NET 8 SDK, o hay archivos `.cs` ajenos dentro de la carpeta del mod (no metas carpetas raras dentro de `ModSources\AethonMod`). |
| Texturas moradas/faltantes | Mezcla de archivos de builds viejas: re-ejecuta el script de actualización (copia espejo) y recompila. |
| Error al cargar con mensaje de "carpeta debe llamarse AethonMod" | El guardián de identidad: la carpeta del mod debe llamarse EXACTAMENTE `AethonMod` (la subcarpeta del repo). Renombra/re-copia y Build & Reload. |

---

## 🔧 Notas de los shaders

- Los shaders viven en `AethonMod/Content/Effects/Shaders/`: cada `.fx` es la
  FUENTE y cada `.fxc` es la versión compilada que el juego carga (tModLoader
  registra el lector para `.fxc`; los `.fx` no se compilan solos).
- Si se modifica un `.fx`, hay que recompilarlo al perfil `fx_2_0` para
  regenerar el `.fxc` correspondiente antes de empaquetar el mod.
- Las texturas de ruido/glow de `Content/Effects/Textures/` se regeneran
  con los scripts de `tools/` (ruido procedural determinista; herramientas de
  desarrollo, fuera de la carpeta del mod).

---

## 📁 Estructura del repo (v6.50.14)

```
Aethon-Mod-Terraria/      <- raíz del repo (docs y herramientas FUERA del mod)
├── AethonMod/            <- EL MOD: esto es lo que se compila/empaqueta
│   ├── build.txt           # Metadatos: ¡AQUÍ vive la versión!
│   ├── description.txt
│   ├── icon.png / icon_small.png
│   ├── AethonMod.cs        # Punto de entrada + guardián de identidad
│   ├── AethonMod.csproj    # Solo IDE
│   ├── Content/            # 282 .cs: Items, Weapons, NPCs, Projectiles, VFX, Systems…
│   └── Localization/       # es-ES / en-US (hjson)
├── README.md / COMPILACION.md / CHANGES.md / …   # Documentación del repo
├── ACTUALIZAR-FUENTE.bat / actualizar-fuente.sh  # repo -> ModSources
├── _masters/               # Sprites maestros (referencia, NO del mod)
└── tools/                  # Generadores de assets (desarrollo, NO del mod)
```
