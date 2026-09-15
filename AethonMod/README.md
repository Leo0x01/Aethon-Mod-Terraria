# Aethon, la Luz Primordial

Un mod de Terraria (tModLoader) centrado en una entidad cósmica antigua que es el jefe final.

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
- **Bioma Sagrario Hueco** (stub lógico; 5 conceptos de bioma diseñados en
  `research/estrategia_v626/INFORME_BIOMAS.md`).
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
