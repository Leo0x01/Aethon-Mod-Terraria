# AethonMod — Historial de Cambios

## Commit FIX-COSMIC-EVENTS-ELIMINADOS — Quitar sistema de eventos cósmicos (Hitos + Lluvia de Luz + Rifts)
- CosmicEventSystem.cs ELIMINADO por completo (165 líneas):
  * Anuncios "Hitos cósmico: Lluvia de Luz Estelar / Sagrario Hueco / Rifts Dimensionales / Aethon se agita / El Despertar" al alcanzar niveles 25/50/75/100/150
  * UpdateStarlightRain: spawn de meteoros dorados cada 10s al nivel 25+
  * UpdateDimensionalRifts: spawn de NPC RiftKeeper bajo tierra al nivel 75+
- AethonConfig.cs: eliminadas flags EnableCosmicEvents, EnableStarlightRain, EnableDimensionalRifts (ya no se usan)
- Se conservan EnableCosmicEvents/StarlightRain/Rifts eliminados del config (cualquier config.json antiguo simplemente ignora esas claves)
- Motivo: el usuario reportó que los mensajes "Hitos cósmico" seguían apareciendo en el juego y debían estar eliminados (formaban parte de la misma familia de eventos cinematográficos que ya se quitó)

## Commit FIX-SPRITES-FALTANTES — Agregar sprites PNG para LevelUpTester y BossSummonBag
- LevelUpTester.png generado (24x24, saco dorado con flecha ascendente)
- BossSummonBag.png generado (24x24, saco purpura con calavera roja)
- MissingResourceException al cargar el mod resuelto
- Verificado: 18/18 ModItem/Projectile/NPC/Buff/Tile tienen su .png

## Commit FIX-EVENTOS-ELIMINADOS — Quitar lore y eventos cinematográficos de subida de nivel
- LevelUpEventSystem.cs ELIMINADO por completo (temblor de pantalla, overlay con grano, time-skip de 1 día, texto de lore centrado)
- ShardLevelItem.OnLevelUp: removido el bloque que llamaba a LevelUpEventSystem.Trigger() en la primera subida de nivel
- GrimoireEternal.OnCraft: método eliminado (su único propósito era disparar el Trigger(showLore:false) al craftear)
- Se conservan los efectos simples de subida de nivel: mensaje dorado "✦ Nivel X!", sonido Item4, 40 partículas doradas, y hito cada 50 niveles
- ShardPlayer.FirstLevelUpTriggered: ahora es flag legacy (se persiste para no romper saves antiguos pero ya no dispara nada)
- Motivo: request directo del usuario de quitar el lore y los eventos como el que mueve la pantalla

## Commit FIX-7ERRORES-COMPILACION — Corregir 7 errores CS0103 reportados por el usuario
- TheWitness.OnChatButtonClicked: declarada variable 'level' (CS0103)
- GrimoireEternal.OnCraft: eliminada sp.ActiveBranch (propiedad inexistente)
- LevelUpEventSystem: agregada sobrecarga Trigger(bool showLore) — luego eliminada en el commit siguiente
- Items/LevelUpTester.cs CREADO: +10 niveles al Grimorio (reemplaza TestSlayer perdido en force-push)
- Items/BossSummonBag.cs CREADO: 999 invocadores de 16 jefes vanilla (recreado tras force-push)
- TestingPlayer reescrito: kit de testing con GenesisShard + 100 GoldBar + LevelUpTester + BossSummonBag
- Localization es-ES/en-US: limpiadas claves huérfanas (CosmicPetItem, TestSlayer, CosmicPet), añadidas LevelUpTester + BossSummonBag

## Commit b70d655 — Correcciones del commit 48688dd
- XPForNextLevel: eliminado if(Level<=1) return 1, fórmula normal para todos
- ExtraProjectiles: cambiado de level/5 a level/3 (cada 3 niveles)
- CanUseItem: click izquierdo retorna true (Mana Flower)
- CanUseItem: click derecho permite Mana Flower
- Shoot minion: maneja Mana Flower
- autoReuse = false (previene doble disparo)
- Item.shoot = 931 (Nightglow)
- Eliminado código duplicado en CanUseItem

## Commit d205222 — autoReuse false + Nightglow 931
- autoReuse cambiado a false (causa del doble disparo)
- Item.shoot = 931 (Nightglow restaurado)

## Commit 3aaec4f — GrimorioTest creada
- Nueva arma de prueba con lógica diferente
- Sin AltFunctionUse, sin autoReuse
- Click derecho en UseItem, click izquierdo en Shoot return true
- Sprite generado con AI

## Commit bd3a55c — Doble disparo solucionado con return true + reuseDelay
- Click izquierdo: return true (tModLoader dispara 1)
- Click derecho: return false + reuseDelay=10

## Commit 48688dd — Doble uso + Mana Flower para minion
- CanUseItem del minion permite Mana Flower
- Shoot del minion maneja 3 casos de mana

## Commit 1a1da42 — Mana Flower no permite disparar con 0 mana
- CanUseItem retorna true para click izquierdo

## Commit 2f4ae26 — Quitar disparo doble + XP nivel 1→2 normal
- Eliminada probabilidad de disparo doble
- XPForNextLevel: fórmula normal para nivel 1→2

## Commit b37ef0e — Proyectil sale doble: return false → return true
- return false causaba que tModLoader disparara adicional

## Commit 482a19d — Proyectil doble: return true → return false
- Cambio inicial de return true a return false

## Commit b507628 — Restaurar sprites + Nightglow + tooltip compacto
- GrimoireEternal.png = sprite libro.png
- CosmicOrbMinion.png = minion cosmico.png
- Item.shoot = 931 (Nightglow)
- Tooltip compactado (12→8 líneas)

## Commit 117bd03 — Trigger(bool) restaurada
- Sobrecarga Trigger(bool showLore) se perdió en force push
- _showLore flag restaurado

## Commit a81445b — ActiveBranch rezagado eliminado
- sp.ActiveBranch = BranchType.Magic eliminado

## Commit 480761e — NPC.HitInfo KnockBack eliminado
- KnockBack no existe en HitInfo

## Commit 982ea23 — BuffID.Terraprisma → ID 322
- Nombre constante no existe, usar ID numérico

## Commit c74e56a — Projectile.color eliminado
- Projectile no tiene propiedad .color

## Commit c8638c9 — WeaponScaling using agregado a CosmicOrbMinion
- Falta using AethonMod.Content.Systems

## Commit be0fee8 — Nightglow cósmico + Terraprisma minion + CosmicEventSystem eliminado
- CosmicProjectileFX mejorado con tinte dorado
- Grimorio invoca proyectil vanilla 946 (Terraprisma)
- CosmicMinionFX creado
- CosmicEventSystem.cs eliminado

## Commit 2ffddc7 — Minion cooldown quitado del tooltip + hitos mejorados
- Eliminado 'Minion cooldown: Xf' del tooltip
- Hitos actualizados: Mejora de velocidad, Mejora de minion

## Commit f284177 — Mana max + vida max + hit cooldown en tooltip
- +1 mana cada 4 niveles
- +2 vida cada 20 niveles
- Hit cooldown del minion en tooltip
- WeaponScaling: BonusMana, BonusLife, MinionHitCooldown

## Commit b728a96 — Bolts en línea + hit cooldown mejora con nivel
- Separación reducida de 0.08 a 0.02 rad
- Hit cooldown: 15 base, -1 cada 10 niveles, min 1

## Commit 52b6d4b — 1 bolt por click + minion custom con sprite
- return true cambiado a return false
- CosmicOrbMinion reescrito con IA tipo Terraprisma
- CosmicMinionFX eliminado

## Commit f6db599 — TODAS las mejoras del Grimorio (23 funciones)
- WeaponScaling: 23 funciones de escalado + MilestoneRewards
- GrimoireEternal: ModifyWeaponKnockback, disparo doble, tooltip completo
- ShardPlayer: PostUpdate (regen), ModifyHurt (reducción daño)
- CosmicOrbMinion: velocidad y rango escalados
- CosmicProjectileFX: recreado con daño en área

## Commit 093826c — 3 blockers de REVIEW-FINAL arreglados
- TestingPlayer: eliminadas refs a items inexistentes
- TheWitness: level declarado en OnChatButtonClicked
- CosmicOrbMinion: held redeclarado (CS0136)

## Commit 2f8a128 — BossSummonBag recreado
- Se perdió en force push, recreado

## Commit cb9e6cd — LevelUpTester recreado
- Se perdió en force push, recreado

## Commit 167ddec — Minions persisten al cambiar arma + Mana Flower
- Slots de minion en PostUpdateEquips (busca en todo el inventario)
- CanUseItem permite Mana Flower

## Commit 1038516 — Limpiar repositorio + sprite libro
- Eliminadas carpetas del sandbox de GitHub
- GrimoireEternal.png = sprite libro.png

## Commit c44a62b — Minions desaparecían al exceder limite
- Slots de minion movidos a ModifyWeaponDamage
- Conteo cambiado a ownedProjectileCounts

## Commit 66b6533 — Tooltip solo muestra próximo hito
- Eliminado el bloque que listaba todos los hitos acumulados

## Commit b0998c5 — Armas se craftean sin yunque
- Eliminado AddTile(TileID.Anvils) de las recetas

## Commit 5c684b4 — Quitar eventos + partículas + minion sprite + XP normal
- Eventos de subida de nivel removidos
- Partículas reducidas (scale + alpha)
- Minion sprite = minion cosmico.png
- XP nivel 1→2 normal

## Commit 458d30d — ThreadStateException en Unload
- Dispose envuelto en try/catch

## Commit b9905cd — Partículas carga minion + bolts 1+cada3 + LevelUpTester
- Partículas de carga restauradas
- Bolts: 1 base + 1 cada 3 niveles
- LevelUpTester: da +10 niveles al Grimorio

## Commit 224bb62 — LevelUpTester busca en todo el inventario
- No requiere sostener el Grimorio

## Commit 936add3 — Minion requiere mana (15 + nivel, tope 100)
- WeaponScaling.MinionManaCost creada
- CanUseItem verifica mana del minion
- Shoot cobra mana al invocar

## Commit 8e93990 — NPC.HitInfo no contiene KnockBack
- Eliminado hit.KnockBack

## Commit f3d84dc — CosmicOrbMinion namespace corregido
- global::AethonMod.Content.Projectiles.CosmicOrbMinion

## Commit 7c30f7a — BranchChoiceUI eliminado de UISystem
- UISystem simplificado

## Commit 1e9b277 — BranchChoiceUI eliminado de UIScrollBlockPlayer
- Simplificado a métodos vacíos

## Commit 4d67ba0 — try sin catch en CosmicOrbMinion
- Estructura try/catch reparada

## Commit 8eb24c7 — Grimorio reescrito desde cero
- Sin dependencias de ActiveBranch/IsImprinted
- WeaponScaling reescrito sin BranchType
- GlobalNPCXP solo otorga XP al Grimorio
- CosmicOrbMinion sin BranchType

## Commit 71fd964 — Rediseño: crafteo + boss drop
- BranchChoiceUI removido
- GenesisShard es arma de luz + material
- Las 3 armas se craftean con GenesisShard
- OnCraft dispara evento cinematográfico
- Boss drops de King Slime/Eye of Cthulhu

## Commit fa240e1 — HJSON malformado + revisión profunda
- HJSON con múltiples cierres } arreglado
- 3 riesgos arreglados (TestingPlayer, BossSummonBag, TestSlayer)
