# AethonMod — Características del Proyecto

## Información General
- **Nombre**: Aethon, la Luz Primordial
- **Versión**: 5.0
- **Framework**: tModLoader 1.4.4
- **Lenguaje**: C#
- **Repo**: https://github.com/Leo0x01/Aethon-Mod-Terraria

---

## Armas

### EL ARSENAL DE PRUEBAS — 47 armas cósmicas (v6.27)
Todas 100% por código, todas sin maná (regla de la casa v6.26), todas
entregadas por LA BOLSA DEL ARSENAL PRIMORDIAL:
- **4 tests clásicos**: TestMagicRing, TestSparkle, ProjBeam, TestMagicRingV2
- **4 V20**: Supernova, PlasmaStorm, PhoenixNova, QuantumSplit
- **4 cósmicas clásicas**: Sol, Medusa Nebular, Cometa Estelar, Púlsar Vivo
- **10 agujeros negros**: Olvido, Cósmico, Umbral, Bruma, Supremo,
  Supremo Aurora + 4 Ascendidos (Umbral/Bruma/Cósmico/Olvido)
- **6 soles rúnicos**: Sol 1..5 + EL SOL 20 (la corona de la familia)
- **Eclipse Primordial** (el Sol de los 20 Anillos + la mezcla de TODOS
  los agujeros) y **Cetro del Trueno** (StormLib)
- **3 armas de las librerías** (v6.24): Sinfonía Primordial, Tormenta
  Nebular, Lanza del Alba
- **El Desgarro en la Realidad** (RiftLib, atraviesa paredes)
- **6 estrellas reales** (v6.26): Estrella de Neutrones, Púlsar, Enana
  Blanca, Estrella Muerta, Supergigante Roja, Magnetar
- **El Ciclo Estelar** (nebulosa → gigante → supernova → remanente)
- **5 bastones creativos** (v6.26): Reloj de Arena Cósmico, Marea
  Gravitatoria, Enjambre Prismático, Péndulo del Juicio, Coro Espectral
- **EL OCASO DE AETHON** (v6.27): el arma suprema del patrón gauge —
  carga (+3 por impacto) → 8 s de muertes de estrella ×3 con ejecución
  <50% → 2 s de sobrecalentada; aro medidor de 20 segmentos 100% por
  código sobre la cabeza; clic derecho activa.

### Grimorio del Eterno (arma principal)
- **Tipo**: Híbrido mágico + invocación
- **Click izquierdo**: Dispara Nightglow (931) con partículas cósmicas
- **Click derecho**: Invoca CosmicOrbMinion (IA tipo Terraprisma)
- **Crafteo**: GenesisShard + 5 GoldBar/PlatinumBar (sin yunque)
- **autoReuse**: false (evita doble disparo)
- **Sprite**: sprite libro.png (38x46)

### Fragmento Génesis (arma de luz + material)
- **Tipo**: Genérico
- **Dispara**: GenesisLight (proyectil homing dorado)
- **Drop**: King Slime y Eye of Cthulhu
- **Uso**: Material para craftear el Grimorio

---

## LIBRERÍAS VFX DEL PROYECTO (el motor propio — v6.27)

El inventario completo. TODAS son de la casa (cero dependencias externas,
`modReferences` vacío — auditoría v6.26). Convención común: **el lote del
llamador** (las funciones de dibujo esperan el SpriteBatch ABIERTO; los
que abren, cierran — contrato v6.10):

| # | Librería | Archivo | Nació | Qué hace |
|---|---|---|---|---|
| 1 | **VFXCore** | `Content/VFX/VFXCore.cs` | v5.x | EL NÚCLEO: buffer de quads reutilizable (cero GC), `Hash01` determinista (la semilla de TODO el mod), pases aditivos con `FlushAdditive` |
| 2 | **VFXPalettes** | `Content/VFX/VFXPalettes.cs` | v5.x | Las paletas de color de la casa |
| 3 | **RuneSunRenderer + EMISOR DE ANILLOS** | `Content/VFX/RuneSunRenderer.cs` | v6.19/v6.26 | El sol rúnico de 20 tiers + `EmitRingSystem` público: el sistema de anillos EXACTO de los soles reutilizable (coronas, Ocaso) |
| 4 | **BlackHoleLensSystem** | `Content/Effects/BlackHoleLensSystem.cs` | v6.0x | La distorsión de lente a pantalla completa (2 render targets) — el sello del mod |
| 5 | **BrumaFX / BrumaBrushes / BrumaNoise** | `Content/Effects/Bruma/` | v6.17, reconstruida v6.25 | HUMO/BRUMA/NIEBLA: flipbook de ruido evolucionado premultiplicado, escalera 64/128/160, luz del mundo, viento, wisps, columnas — las 24 lecciones de 23 fuentes |
| 6 | **StormLib** | `Content/VFX/StormLib.cs` | v6.21 | RAYOS: ZigPath/Boil/ForkTree/Refine, Strand/ChainBolt/MultiBolt, ArcRing, ImpactFlash, telegraphs, luz a lo largo del camino |
| 7 | **LumenLib + LumenPalettes** | `Content/VFX/LumenLib.cs` | v6.2x | LUZ: Bloom/BloomPulse/Flare/Ray/Lance/LanceTrail/Telegraph, ciclos de paleta |
| 8 | **EstelaLib** | `Content/VFX/EstelaLib.cs` | v6.25 | ESTELAS/ribbons de grosor variable: Sanitize/Smooth/Resample, perfiles Head/Center/Comet/Alive, fantasmas con squash, track por identidad |
| 9 | **OndaLib + OndaSystem** | `Content/VFX/OndaLib.cs` | v6.25 | ONDAS DE IMPACTO: Shock/Pulse/Ground, Kick centralizado (ModifyScreenPosition) + Flash (PostDrawInterface) |
| 10 | **PyraLib + PyraPalettes** | `Content/VFX/PyraLib.cs` | v6.25 | FUEGO: tablas de 37 niveles, Tongue (lenguas erosionadas), Flame, EmberField (Doom Fire determinista), Sparks físicas — `Sample` a prueba de NaN desde v6.27 |
| 11 | **RiftLib + RiftPaletas + RiftMundoSystem** | `Content/VFX/RiftLib.cs` | v6.26 | DESGARROS DE REALIDAD: Tear/Grieta persistente/Interior/Estrellas/Shards/ChispasAnomalia/EcoGlitch/Oscurecer — el contrato completo de 16 fuentes |
| 12 | **ParticleManager** | `Content/Particles/ParticleManager.cs` | v5.x | El sistema data-oriented de partículas |

**Sistemas de soporte del arsenal**: un renderer dedicado por arma
(Cosmic/…Renderer.cs — 20+), `OcasoSystem` (la UI del gauge + las
heridas de las muertes de estrella, v6.27), `OndaSystem` (impactos de
pantalla), `BrumaSystem` (vida de las texturas de humo).

**Procedencia**: 10 nacieron del propio desarrollo; v6.25 añadió las 3
del análisis de huecos (Estela/Onda/Pyra — 23 fuentes de investigación);
v6.26 añadió RiftLib (16 fuentes). **Con la última investigación NO se
creó ninguna librería NUEVA** (la investigación v6.26 era de armas/biomas/
mercado) pero sí se MEJORARON dos: `RuneSunRenderer` (el emisor
compartido público) y `PyraPalettes.Sample` (endurecida contra NaN en
v6.27 tras el bug del client.log).

---

## Sistema de Niveles (por item)
- Cada Grimorio tiene su propio nivel/XP (persistente via TagCompound)
- XP por matar enemigos (basada en rareza del bestiario)
- Niveles infinitos
- Fórmula XP: 80 × nivel^1.5

### Mejoras por nivel (continuas)
| Mejora | Frecuencia | Máximo |
|--------|-----------|--------|
| Daño mágico | Cada nivel | Infinito (+2.2%) |
| Daño summon | Cada nivel | Infinito (+1%) |
| Crítico mágico | Cada nivel | 100% (+0.2%) |
| Armor penetration | Cada 5 niveles | 50% (+2%) |
| Use time | Cada nivel | -25% (-0.3%) |
| Mana máximo | Cada 4 niveles | Infinito (+1) |
| Vida máxima | Cada 20 niveles | Infinito (+2) |
| Bolts extra | Cada 3 niveles | Infinito (+1) |
| Slots minion | Cada 5 niveles | Infinito (+1) |
| Lifesteal | Cada 7 niveles (desde nv7) | Infinito (+0.1%) |
| Hit cooldown minion | Cada 10 niveles | 1 frame (-1) |
| Mana cost bolt | Cada 20 niveles | 30 (+3) |
| Mana cost minion | Cada nivel | 100 (+1) |
| Bonus mana faltante | Dinámico | +50% |
| Knockback | Cada 10 niveles | +100% |
| Regeneración mana | Cada 20 niveles | 10/seg |
| Regeneración vida | Cada 20 niveles | 5/seg |
| Daño contacto minion | Cada 15 niveles | +500% |
| Velocidad minion | Cada 30 niveles | +200% |
| Reducción daño | Cada 100 niveles | 10% |
| Rango detección minion | Cada 10 niveles | 1500px |
| Daño en área bolt | Cada 5 niveles | 20px |

### Hitos (cada 5 niveles, múltiples mejoras por hito)
1. +1 slot de minion
2. +1 bolt extra
3. Mejora de velocidad del minion
4. +10% crítico mágico
5. Mejora de minion
(Cicla cada 5 hitos / 25 niveles)

---

## Proyectiles

### Nightglow (931) — proyectil del Grimorio
- Homing agresivo hacia enemigos
- Partículas cósmicas via CosmicProjectileFX (dorado/cian/magenta)
- Daño en área al impactar (+1px cada 5 niveles, tope 20px)

### CosmicOrbMinion — minion del Grimorio
- IA tipo Terraprisma (vuela hacia enemigos, ataca por contacto)
- No dispara proyectiles
- Animación: idle orbita, ataque vuela hacia enemigo
- Hit cooldown mejora con nivel (15→1 frame)
- Velocidad escala con nivel (base 16, +50% cada 30 niveles)
- Rango de detección escala (500px→1500px)
- Sprite: minion cosmico.png (32x32)

### GenesisLight — proyectil del Fragmento Génesis
- Homing dorado
- 2 proyectiles en abanico por disparo

### CosmicOrbBolt — proyectil del minion (legacy, no usado actualmente)
- Homing cian/dorado

---

## NPCs (6)
- AethonBoss — jefe final, 5 fases
- HollowTitan — jefe del Sagrario Hueco
- TheWitness — NPC que vende Resonance (nivel 50+)
- EchoBlade, EchoArcher, RiftKeeper — NPCs hostiles

---

## Mundo
- AncientAltar — tile 3x2, se genera bajo tierra (3 por mundo)
- HollowSanctumBiome — sub-bioma cristalino (requiere 400 HP)

---

## Items
- GenesisShard — arma de luz + material de crafteo
- ResonanceShard — moneda de jefes
- AncientAltarItem — item colocable del altar
- LevelUpTester — da +10 niveles al Grimorio (testing)
- BossSummonBag — da 999 invocadores de cada jefe (testing)
- GrimoireTest — arma de prueba con lógica diferente (testing)

---

## Sistemas
- WeaponScaling — 23 funciones de escalado
- ShardLevelItem — niveles/XP por item (GlobalItem)
- GlobalNPCXP — otorga XP al Grimorio + lifesteal + boss drops
- ShardPlayer — mana/vida max, regen, reducción daño
- CosmicProjectileFX — partículas cósmicas en Nightglow
- AncientAltarWorldGen — genera altares bajo tierra
- ShardLevelSystem — XP por rareza del bestiario
- ~~LevelUpEventSystem~~ — ELIMINADO (temblor de pantalla, grano, time-skip y lore)
- ~~CosmicEventSystem~~ — ELIMINADO (anuncios "Hitos cósmicos", Lluvia de Luz Estelar, Rifts Dimensionales)

---

## Configuración
- AethonConfig — multiplicador XP, notificaciones de nivel/hitos, debug
- build.txt — version 5.0, side Both

---

## Localización
- Español (es-ES) e inglés (en-US)

---

## Testing
- TestingPlayer — al entrar al mundo (un jugador) entrega SOLO:
  - **LA BOLSA DEL ARSENAL PRIMORDIAL** (v6.27, 1 ranura, garantizada)
  - La bolsa se abre con CLIC DERECHO: despliega TODO el arsenal con
    semántica de garantía (solo lo que falte — reabrirla repone armas)
  - El listado completo vive en `ArsenalBag.Contenido()` (el punto único
    de la verdad: dar de alta un arma nueva = 1 línea ahí)
- BossSummonBag — da 999 invocadores de cada jefe (testing, aparte)
