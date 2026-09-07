# AethonMod — Características del Proyecto

## Información General
- **Nombre**: Aethon, la Luz Primordial
- **Versión**: 5.0
- **Framework**: tModLoader 1.4.4
- **Lenguaje**: C#
- **Repo**: https://github.com/Leo0x01/Aethon-Mod-Terraria

---

## Armas

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
- LevelUpEventSystem — evento cinematográfico (sin lore)
- AncientAltarWorldGen — genera altares bajo tierra
- ShardLevelSystem — XP por rareza del bestiario

---

## Configuración
- AethonConfig — multiplicador XP, eventos cósmicos, notificaciones
- build.txt — version 5.0, side Both

---

## Localización
- Español (es-ES) e inglés (en-US)

---

## Testing
- TestingPlayer — da items al entrar al mundo:
  - GenesisShard, 100 GoldBar, LevelUpTester, BossSummonBag
