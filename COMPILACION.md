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

Rutas habituales:

| | Ruta |
|---|---|
| ModSources | `Documentos\My Games\Terraria\tModLoader\ModSources` |
| Mods (.tmod) | `Documentos\My Games\Terraria\tModLoader\Mods` |

---

## 🚀 VÍA 1 — Descargar el `.tmod` oficial (sin git, 1 minuto)

1. Abre <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases>.
2. En la última versión, descarga **`AethonMod.tmod`**.
3. Cópialo a la carpeta **Mods** (tabla de arriba).
4. tModLoader → **Mods** → activa *Aethon, la Luz Primordial*.
5. **Verifica en la lista de mods que la versión sea la del release.**

El `.tmod` ya lleva dentro el nombre `AethonMod` y la versión correcta:
es la vía sin riesgo de identidad.

---

## 🚀 VÍA 2 — Clon del repo en ModSources (git, ideal para siempre-a-la-última)

```bash
# UNA SOLA VEZ — borra cualquier carpeta AethonMod vieja dentro de ModSources:
cd "Documentos\My Games\Terraria\tModLoader\ModSources"
# IMPORTANTE: el argumento final 'AethonMod' fija el nombre de la carpeta
# (el repo se llama distinto en GitHub, pero la carpeta del mod debe llamarse AethonMod):
git clone https://github.com/Leo0x01/Aethon-Mod-Terraria.git AethonMod

# CADA VEZ QUE SALGA UNA VERSIÓN NUEVA:
cd AethonMod
git pull
```

Después, en el juego: **Mods → Develop Mods → AethonMod → Build + Reload**.
El número de versión del menú de Mods debe subir (`6.50.13`, `6.50.14`, …).

> **¿Por qué antes no funcionaba?** El repo antiguo tenía el mod ANIDADO
> (`Aethon-Mod-Terraria/AethonMod/build.txt`): un clon no cabía en ModSources
> (tML solo escanea carpetas directas) y actualizar exigía copiar a mano —
> de ahí que las versiones nuevas nunca llegaban y el juego siguiera cargando
> builds viejas. Desde v6.50.13 **la raíz del repo ES la carpeta del mod** y
> `git pull` + Build & Reload basta.

### Si no quieres git nunca más

Descarga el ZIP del release ("Source code") o directamente el `.tmod` (VÍA 1).
Si usas el ZIP: descomprímelo DENTRO de ModSources con el nombre exacto
`AethonMod` y Build & Reload.

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
2. Selecciona **AethonMod** (la carpeta clonada en ModSources).
3. **Build + Reload**. El `.tmod` resultante cae en la carpeta **Mods** y se
   activa solo.

> El `.csproj` incluido NO participa en el build del mod (tML compila con
> Roslyn y empaqueta según `build.txt`); existe solo para IntelliSense.

### Vía IDE (opcional, para desarrollar)

Abre `AethonMod.csproj` en Rider/VS/VS Code: el `Import` de
`tModLoader.targets` se resuelve solo cuando el proyecto vive dentro de
ModSources.

### Vía línea de comandos (CI/automatización)

```bash
# Con el tModLoader instalado (ruta de ejemplo de Steam en Windows):
"C:\...\tModLoader\tModLoader.bat" -build "Documentos\My Games\Terraria\tModLoader\ModSources\AethonMod"
```

`tModLoader -build <carpeta>` compila, empaqueta y sale sin abrir ventana
(es la vía con la que se genera el `AethonMod.tmod` de los releases).

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
| El mod se llama distinto en la lista | La carpeta de fuentes no se llama `AethonMod`. Renómbrala (o borra el build del menú Mods y baja el `.tmod` oficial). |
| El número de versión no sube | Tu carpeta de fuentes no es un clon de git: no hay nada que actualizar. Bórrala y clona de nuevo (VÍA 2). |
| El mod no aparece en Develop Mods | La carpeta está anidada: `build.txt` debe estar directamente en `ModSources\AethonMod\build.txt`. |
| Build falla con errores CS | Falta el .NET 8 SDK, o hay archivos `.cs` ajenos dentro de la carpeta del mod (no metas carpetas raras en el clon). |
| Texturas moradas/faltantes | Mezcla de archivos de builds viejas: haz un clon limpio y recompila. |
| Error al cargar con mensaje de "carpeta debe llamarse AethonMod" | El guardián de identidad v6.50.13: renombra la carpeta exactamente a `AethonMod` y Build & Reload. |

---

## 🔧 Notas de los shaders

- Los shaders viven en `Content/Effects/Shaders/`: cada `.fx` es la FUENTE
  y cada `.fxc` es la versión compilada que el juego carga (tModLoader
  registra el lector para `.fxc`; los `.fx` no se compilan solos).
- Si se modifica un `.fx`, hay que recompilarlo al perfil `fx_2_0` para
  regenerar el `.fxc` correspondiente antes de empaquetar el mod.
- Las texturas de ruido/glow de `Content/Effects/Textures/` se regeneran
  con los scripts de `tools/` (ruido procedural determinista).

---

## 📁 Estructura del repo (v6.50.13+)

```
AethonMod/              <- RAÍZ del repo = carpeta del mod
├── build.txt           # Metadatos: ¡AQUÍ vive la versión!
├── description.txt
├── icon.png / icon_small.png
├── AethonMod.cs        # Punto de entrada + guardián de identidad
├── AethonMod.csproj    # Solo IDE
├── Content/            # 282 .cs: Items, Weapons, NPCs, Projectiles, VFX, Systems…
├── Localization/       # es-ES / en-US (hjson)
├── tools/              # Generadores de assets (excluidos del .tmod via buildIgnore)
├── _masters/           # Sprites maestros (excluidos del .tmod via buildIgnore)
└── *.md                # Docs (excluidas del .tmod via buildIgnore)
```
