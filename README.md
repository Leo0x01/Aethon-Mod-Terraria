# Aethon, la Luz Primordial

Un mod de Terraria (tModLoader) centrado en una entidad cósmica antigua que es el jefe final.

> **Estructura del repo (v6.50.14):** el mod vive DENTRO de la carpeta
> **`AethonMod/`** de este repositorio (todo lo que tModLoader compila:
> `build.txt`, `Content/`, `Localization/`, iconos…). Todo lo demás de este
> repo (esta documentación, `_masters/`, `tools/`) NO forma parte del mod y
> está fuera de esa carpeta. Para jugar solo necesitas la subcarpeta
> `AethonMod` o el `.tmod` oficial.

## 🔧 Instalar / actualizar (LEER PRIMERO)

> **Regla de oro:** en tModLoader el **nombre del mod es el nombre de su carpeta de
> fuentes**. Este mod se llama `AethonMod` y tus personajes/mundos guardan sus datos
> con ese nombre. Compilarlo desde una carpeta con OTRO nombre crea un mod
> "distinto" y tus guardados parecerán corruptos (el mod ahora lo detecta y te lo
> avisa al cargar, con instrucciones).

### Opción 1 — Descargar el `.tmod` (sin git, la más fácil)

1. Ve a **<https://github.com/Leo0x01/Aethon-Mod-Terraria/releases>**.
2. Descarga **`AethonMod.tmod`** de la última versión.
3. Cópialo en la carpeta **Mods** de tModLoader
   (Windows: `Documentos\My Games\Terraria\tModLoader\Mods`), **reemplazando**
   cualquier `AethonMod.tmod` viejo que hubiera.
4. Actívalo en **Mods → tModLoader**. Comprueba en la lista que la **versión**
   coincide con la del release.

### Opción 2 — Fuente en ModSources (git, siempre a la última)

El repo contiene el mod **dentro** de la subcarpeta `AethonMod/`; esa subcarpeta
(y SOLO ella) es lo que va a tu carpeta **ModSources**:

```bash
# UNA SOLA VEZ (clona donde quieras, p. ej. en Documentos):
cd "Documentos"
git clone https://github.com/Leo0x01/Aethon-Mod-Terraria.git
cd Aethon-Mod-Terraria

# Copia el mod a ModSources y actualízalo SIEMPRE con esto:
ACTUALIZAR-FUENTE.bat        # Windows
./actualizar-fuente.sh       # Linux / macOS
```

El script hace `git pull` + copia espejo de `AethonMod/` → `ModSources\AethonMod`
(ruta típica: `Documentos\My Games\Terraria\tModLoader\ModSources\AethonMod`).
Después, en el juego: **Mods → Develop Mods → AethonMod → Build + Reload**.

> A mano sería: copia la **subcarpeta `AethonMod`** del repo dentro de
> `ModSources`, con ese nombre exacto. **Ojo:** la carpeta del mod debe ser
> hija DIRECTA de ModSources (`ModSources\AethonMod\build.txt`) — no metas el
> repo entero ahí ni lo anides más. Si la carpeta se llama distinto, el propio
> mod te avisará al cargar con las instrucciones para arreglarlo.

## 📖 Descripción

Encuentra un altar subterráneo en el nuevo bioma "Sagrario Hueco". Bondéate a un fragmento de luz que evoluciona según tu rama de combate (Distancia, Cuerpo a Cuerpo, o Artes Mágicas). Sube de nivel infinitamente, desbloquea un árbol de habilidades procedural, y absorbe las habilidades de cualquier arma del juego.

Enfréntate a Aethon, la entidad cósmica de 5 fases, como jefe final opcional.

## ✨ Funciones

- **Fragmento Génesis** con niveles infinitos y 3 ramas principales.
- **Grimorio del Eterno**: arma híbrida mágica/de invocación con niveles infinitos.
- **Arsenal de pruebas cósmico — 47 armas** (soles rúnicos, 10 agujeros
  negros, 6 estrellas reales (agrandadas v6.28), el desgarro de realidad
  CONTINUO que se fractura, EL ECLIPSE TOTAL rediseñado, tormentas, ciclo
  estelar, enjambres, péndulos, coros…).
- **BOLSA DEL ARSENAL PRIMORDIAL**: al entrar al mundo recibes SOLO la
  bolsa (1 ranura); clic derecho despliega todo el arsenal — solo entrega
  lo que te falte y es permanente (v6.27).
- **EL OCASO DE AETHON**: el arma suprema del medidor gauge
  (carga → estallido ×3 → bloqueo) con aro medidor 100% por código (v6.27).
- **12 librerías VFX propias** (humo, fuego, rayos, estelas, ondas, luz,
  desgarros de realidad…) — render 100% procedural, 0 sprites de arte.
- **Árboles de habilidades procedurales** (95 nodos totales: 30+30+35).
- **Capstone de Absorción de Lore** — memoriza armas del juego base + mods.
- **Bioma Sagrario Hueco** (stub lógico; 5 conceptos de bioma diseñados).
- **6 jefes** (mini-jefe, ecos, cósmicos, jefe final Aethon de 5 fases).
- **Economía de Fragmentos de Resonancia**.
- **NPC "El Testigo"** que narra lore y vende runas.
- **UI interactiva**: árbol de habilidades (tecla K) + códex de memoria (tecla J).
- **Localización ES/EN**.
- **Sync multi-jugador**.

## 🎮 Cómo jugar

1. Compila el mod (ver `COMPILACION.md`).
2. Actívalo en tModLoader.
3. Entra a un mundo (un jugador): recibes **LA BOLSA DEL ARSENAL
   PRIMORDIAL** — el kit de pruebas completo en una sola ranura.
4. Clic derecho sobre la bolsa: se despliega TODO el arsenal (solo lo que
   te falte; reábrela cuando pierdas un arma).
5. Mata enemigos con daño Ranged/Melee/Magic → el fragmento se "imprime"
   con esa rama.
6. Pulsa **K** para abrir el árbol de habilidades.
7. Pulsa **J** para abrir el códex de memoria (nivel 50+).
8. Con el **BASTÓN DEL OCASO DE AETHON**: cada impacto de un fragmento
   carga el aro sobre tu cabeza; aro lleno → **CLIC DERECHO** y llueven
   8 segundos de muertes de estrella a daño ×3.

## 📦 Estructura del repo

```
Aethon-Mod-Terraria/      <- raíz del repo (clonalo con el nombre que sea)
├── AethonMod/            <- EL MOD (esto es lo que va a ModSources)
│   ├── build.txt              # Metadatos del mod (¡la versión vive aquí!)
│   ├── description.txt        # Descripción
│   ├── icon.png / icon_small.png
│   ├── AethonMod.cs           # Punto de entrada (+ guardián de identidad)
│   ├── AethonMod.csproj       # Proyecto .NET 8 (solo para el IDE)
│   ├── Content/               # TODO el contenido del mod (282 .cs)
│   └── Localization/          # es-ES / en-US (hjson)
├── README.md                  # Este archivo (fuera del mod)
├── COMPILACION.md             # Guía de compilación/actualización
├── CHANGES.md                 # Historial de versiones
├── CARACTERISTICAS.md / DISEÑO_DEL_MOD.md / STABLE-SNAPSHOT.md
├── ACTUALIZAR-FUENTE.bat      # Script: repo -> ModSources (Windows)
├── actualizar-fuente.sh       # Script: repo -> ModSources (Linux/macOS)
├── _masters/                  # Sprites maestros de referencia (NO son del mod)
└── tools/                     # Scripts generadores de assets (NO son del mod)
```

## 🛠️ Compilación

Ver `COMPILACION.md` para instrucciones detalladas (incluye la actualización).

La vía oficial del juego: **Mods → Develop Mods → AethonMod → Build + Reload**
(compila con Roslyn dentro del propio tModLoader; el `.csproj` es solo para
IntelliSense y NO participa en el build del mod).

## 📊 Estado

- **282 archivos C#** · **6.50.14**
- **352+ sprites** (estilo Terraria) + librerías VFX procedurales
- **2 UIs visuales** (árbol + códex)
- Historial completo de versiones en `CHANGES.md`

## 📜 Licencia

MIT License — ver `AethonMod/LICENSE`.

## 🤝 Contribuir

Pull requests bienvenidos. Reporta bugs en Issues.
