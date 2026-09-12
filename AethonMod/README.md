# Aethon, la Luz Primordial

Un mod de Terraria (tModLoader) centrado en una entidad cósmica antigua que es el jefe final.

## 📖 Descripción

Encuentra un altar subterráneo en el nuevo bioma "Sagrario Hueco". Bondéate a un fragmento de luz que evoluciona según tu rama de combate (Distancia, Cuerpo a Cuerpo, o Artes Mágicas). Sube de nivel infinitamente, desbloquea un árbol de habilidades procedural, y absorbe las habilidades de cualquier arma del juego.

Enfréntate a Aethon, la entidad cósmica de 5 fases, como jefe final opcional.

## ✨ Funciones

- **Fragmento Génesis** con niveles infinitos y 3 ramas principales.
- **Grimorio del Eterno**: arma híbrida mágica/de invocación con niveles infinitos.
- **Arsenal de pruebas cósmico** (14 armas: sol, agujero negro, medusa, cometa, púlsar...).
- **Árboles de habilidades procedurales** (95 nodos totales: 30+30+35).
- **Capstone de Absorción de Lore** — memoriza armas del juego base + mods.
- **Bioma Sagrario Hueco** con altar, mineral y mobs propios.
- **6 jefes** (mini-jefe, ecos, cósmicos, jefe final Aethon de 5 fases).
- **Economía de Fragmentos de Resonancia**.
- **NPC "El Testigo"** que narra lore y vende runas.
- **UI interactiva**: árbol de habilidades (tecla K) + códex de memoria (tecla J).
- **Localización ES/EN**.
- **Sync multi-jugador**.

## 🎮 Cómo jugar

1. Compila el mod (ver `COMPILACION.md`).
2. Actívalo en tModLoader.
3. Craftea el **Fragmento Génesis** (1 Wood, receta de debug) o encuentra el **Altar Antiguo** bajo tierra.
4. Mata enemigos con daño Ranged/Melee/Magic → el fragmento se "imprprime" con esa rama.
5. Sigue matando → sube de nivel infinitamente.
6. Pulsa **K** para abrir el árbol de habilidades.
7. Pulsa **J** para abrir el códex de memoria (nivel 50+).

## 📦 Estructura

```
AethonMod/
├── build.txt              # Metadatos del mod
├── description.txt        # Descripción
├── icon.png               # Icono 80×80
├── AethonMod.csproj       # Proyecto .NET 8
├── COMPILACION.md         # Guía de compilación
├── README.md              # Este archivo
├── .gitignore
├── AethonMod.cs           # Punto de entrada
└── Content/
    ├── Items/             # ModItem (Fragmento, Resonancia, Runa, Altar)
    ├── Weapons/           # 3 armas + Projectiles/
    ├── NPCs/              # 6 NPCs (Aethon, Titan, RiftKeeper, Echoes, Witness)
    ├── Biomes/            # ModBiome
    ├── Tiles/             # ModTile (Altar)
    ├── Players/           # ModPlayer + enums
    ├── Globals/           # GlobalNPC (XP)
    ├── Systems/           # 6 ModSystem (XP, nodos, eventos, sync, UI, catálogo)
    └── UI/                # 2 UIState (árbol, códex)
```

## 🛠️ Compilación

Ver `COMPILACION.md` para instrucciones detalladas.

```bash
# Requisitos: .NET 8 SDK + tModLoader
cd AethonMod
dotnet build -c Debug
```

O desde tModLoader: Workshop → Develop Mods → seleccionar carpeta → Build + Reload.

## 📊 Estado

- **31 archivos C#** · **3432 líneas de código**
- **17 sprites pixel-art** (estilo Terraria)
- **2 UIs visuales** (árbol + códex)
- **Fases 0-15 del roadmap completas**

## 📜 Licencia

MIT License — ver `LICENSE`.

## 🤝 Contribuir

Pull requests bienvenidos. Reporta bugs en Issues.
