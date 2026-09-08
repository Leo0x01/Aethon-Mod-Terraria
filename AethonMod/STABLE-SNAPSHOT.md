# AethonMod — Estado SUPER ESTABLE (stable-v5.27)

**Fecha:** 2026-09-08
**Commit:** e6cd894 (y subsiguientes hasta 401277a)
**Tag:** `stable-v5.27`
**Rama backup:** `stable-v5.27-backup`

## Estado verificado por el usuario: TODO FUNCIONA PERFECTAMENTE

### Grimorio del Eterno
- ✅ 1 proyectil por click (no doble) — CAUSA RAÍZ ARREGLADA
- ✅ 1 minion por click derecho (no doble)
- ✅ autoReuse (mantener click para disparar continuo)
- ✅ UseTimeMultiplier + UseAnimationMultiplier (velocidad escalada)
- ✅ Tooltip con 2 ventanas (básica/completa) — click derecho alterna
- ✅ Vista completa sin info vanilla ni modifiers
- ✅ Sin debug spameando el chat
- ✅ Sin cooldown artificial

### Minion (CosmicOrbMinion)
- ✅ Órbita compacta (no círculo gigante)
- ✅ Rotación sobre el centro (no sobre la parte superior)
- ✅ Visible (no transparente)

### Proyectil Nightglow (#931)
- ✅ Estela cósmica (dorado/cian/magenta/índigo)
- ✅ Partículas de explosión con duración corta

### Eventos eliminados (por request del usuario)
- ✅ LevelUpEventSystem eliminado (temblor, grano, time-skip, lore)
- ✅ CosmicEventSystem eliminado (Hitos cósmicos, Lluvia de Luz, Rifts)

### Sprites
- ✅ GrimoireEternal.png: 30×38 (sprite del usuario)
- ✅ CosmicOrbMinion.png: 32×32 (sprite del usuario)
- ✅ CosmicOrbBuff.png: 32×32

### Causa raíz del doble Shoot (RESUELTA)
UseTimeMultiplier reducía useTime (22→21) pero NO useAnimation (22).
Cuando useTime efectivo < useAnimation con autoReuse=true,
tModLoader dispara Shoot 2 veces por ciclo.
Fix: UseAnimationMultiplier aplica el mismo multiplier a useAnimation.

## NO MODIFICAR NADA DE ESTE ESTADO SIN CONFIRMACIÓN DEL USUARIO

## Restaurar este estado
```bash
git clone https://github.com/Leo0x01/Aethon-Mod-Terraria.git
cd Aethon-Mod-Terraria
git checkout stable-v5.27
```

O descargar ZIP:
https://github.com/Leo0x01/Aethon-Mod-Terraria/archive/refs/tags/stable-v5.27.zip

## Hashes SHA256 de archivos clave

048087e784a2fc3079f905289e72938125af6a87846e4547c42412b1c3c27268  AethonMod/description.txt
071fee4250aea9b500188e921c1f423512b7fe9b11618fe78d108d20ca6e0e64  AethonMod/Content/Items/BossSummonBag.cs
0844393e699c80ce67a7e942ad4b744d155e175733c7e60b2e3cbcccef502d37  AethonMod/Content/Globals/TooltipToggleItem.cs
0d31926c3ac4fcc45351fe11e45c8c26ee08e7eb1f628866721d36638f58001d  AethonMod/Content/Players/UIScrollBlockPlayer.cs
102055302ec839f00bb5abb829fccf46a83e4ae7b98df2ede3b6a8d92359e888  AethonMod/Content/Weapons/Projectiles/ArcaneBolt.png
13a81fd2cc4feca80323824e54f4137b3494b1a06c953de6cc9a5914bce99857  AethonMod/Content/NPCs/EchoBlade.cs
1a2292b336c59db6cd3dc565893f1624bb603e38b0f1316e595868aec33103b4  AethonMod/Content/Buffs/CosmicOrbBuff.cs
1ecffb8041c8214c6c0ea62061c8303c3a3fc3881d866701034411d625f011b1  AethonMod/Content/Globals/CosmicProjectileFX.cs
20e5a5304173882b66a6a52dd928c4d0e52d90aae5abe125b07359014ec6a1b3  AethonMod/Content/Tiles/AncientAltar.png
220b016d3ac9d38b14595082bcd04f39b3cc4237637fedd33d39ddded9e53900  AethonMod/Content/Items/Placeables/AncientAltarItem.png
284be76438b1937e68b877bd8bd20d4e25f29d0ebe35ee1b0e7bdeefce4181ae  AethonMod/Content/Projectiles/CosmicOrbMinion.cs
28c1a296c654cdac99aaaadbfa582b0092c9ff9785e04b2cdeacdfe88a494a5c  AethonMod/Content/NPCs/EchoArcher.png
2ae6d88d65443cf1a2507753f3adc3c440b3574ab2d668322306c2fb123e0568  AethonMod/build.txt
2bc343151dc7cbc94cb4c6a500e4669192050eb3e307972b097cef228e8d985e  AethonMod/Content/Tiles/AncientAltar.cs
2d353218737518b3ef1b0e4cfd7bba1393205d4322310940f97588f35d71e5d6  AethonMod/Content/NPCs/EchoBlade.png
3029a3a6f964914c46b0c403110e4d5afecdc71590316d39cbf9c491e1739fdd  AethonMod/Content/Buffs/CosmicOrbBuff.png
3029a3a6f964914c46b0c403110e4d5afecdc71590316d39cbf9c491e1739fdd  AethonMod/Content/Projectiles/CosmicOrbMinion.png
306efbf9e997854f924c64d58f45b416060ad472a086b5c826a04a99c0fbe06c  AethonMod/Content/Systems/UISystem.cs
322d7d089ebd3ac0e67d293b720bf339cc4be9379df8ec12ecf2026d0e099595  AethonMod/Content/NPCs/HollowTitan.cs
39d510377cfe97563729d0fe43670c58f86607f3f97a4a1d524d0744d5d5196b  AethonMod/Content/UI/ShardXPBarUI.cs
3a8fdd1512458f5f60ccff8b55b41a3c757e509dc5ed38d63c4efcfec0930a14  AethonMod/Content/NPCs/HollowTitan.png
3ca970288aefb06e98be2023a17a66ca7684e162705dcd803a6db179dea43a26  AethonMod/Content/Items/SeerOrb.cs
4329540a9a45394bb2448792351836806632ce0baadc230a868e6c986b6b379f  AethonMod/Content/NPCs/EchoArcher.cs
43548d8c6e2a10092cc378a26bc845c1f5626125967440687acf95440b5fa3e4  AethonMod/Content/Items/BossSummonBag.png
4cfcffae2cc7dbfc5cf5688b6ef3f4b48e458dad44e41c76cd3d7f7a728e06b1  AethonMod/Localization/es-ES_Mods.AethonMod.hjson
4de3984a3d3d485d6a894bfa239c4228acca1d86bcd7ed1252a02172883ffaa4  AethonMod/Localization/en-US_Mods.AethonMod.hjson
4deba471ec74ef38707e2039d7f9d538409ca73615729f08c77d3237003b4bb9  AethonMod/Content/Systems/AncientAltarWorldGen.cs
4f245a55d102ad469c18f3e2409bb6592250d794e3cb514cb0e277b4af6e509c  AethonMod/Content/Players/BranchType.cs
595242f9a05260ae0dd7f34cc3fc5d6d6d555e8244906612495e4a795d8db2fe  AethonMod/Content/Items/Placeables/AncientAltarItem.cs
59f82f0ef93b6b0839ee75aac6adf6d0a34614750691dfa90d57f33e19fac101  AethonMod/Content/Systems/ShardSyncSystem.cs
5b7dc67b03654dab01f7ac501c6978a1ecf29efbdd27f34e726c9d0abfc9366f  AethonMod/Content/Items/GenesisShard.cs
5d50246d1c9b9c799e453a406ecc017348ff4c4ffdcdc8f34bd98b904f692610  AethonMod/Content/NPCs/AethonBoss.cs
70596f5d0c4e2bd80ba8ff4094515c5c84f0ca5dd3a01043648446035522ad3e  AethonMod/Content/NPCs/AethonBoss.png
7e1db631ec2f5aa33ca9c1cf356cd71aa070cbd4bd9874f7c9699ba4c4333a78  AethonMod/Content/Items/SeerOrb.png
8135e26639cd4a6206a142848931c4cb07d4ec6d61e6e96b6d7dc9add60922e5  AethonMod/Content/Weapons/GrimoireEternal.cs
8507293b64eee4d071c2bf1749f6b1eef4ac153ef613c1dda55a4ed5c9595c0a  AethonMod/Content/Players/ShardPlayer.cs
85a2e491feea9f4468245dca9cf5c7d345143360f548213bb9364f7f37ded86f  AethonMod/Content/Systems/ShardLevelSystem.cs
85b52db6767cc0a14490dbcd77c3c36cec38797202e755d8eef1fa35a14b9d91  AethonMod/Content/Items/GenesisShard.png
85b52db6767cc0a14490dbcd77c3c36cec38797202e755d8eef1fa35a14b9d91  AethonMod/Content/Weapons/Projectiles/GenesisLight.png
8c3a5fc33615828acf73287c40f40c572140125344e56833d1eb29a6052ca4b0  AethonMod/Content/Systems/WeaponScaling.cs
8da8f648f4d774e0cbffe752958b3554b9ae97717f980b065528fba7d623ee0e  AethonMod/Content/Projectiles/CosmicOrbBolt.cs
8dbc18b98daaff22195466f1b8a348f777b82b2bebc5ffe603d1c15cf09a3f96  AethonMod/Content/Items/LevelUpTester.cs
9565c3aac79bfdd0b9c94ba80badedc65db87e61c7d51052bd37c027f1ef0dd0  AethonMod/Content/Weapons/Projectiles/GenesisLight.cs
b067a5be9e2ffb80b8bc2d5ba4c19818189fee0b09e64f7e1c5198fd7e9ecd58  AethonMod/Content/NPCs/RiftKeeper.cs
b638c99b2af520058e0d8c3c5aba3f2d3f6d02b4f265b978dbb00bcc308a001d  AethonMod/Content/NPCs/TheWitness.png
b84d3cf085d7fe8f7b0930af4687a3920c79b215b8e9151eeeb4db990b4a717c  AethonMod/Content/Globals/ShardLevelItem.cs
bc248586be24ac2e9720a64dd4d26e3ae1da934b53ddd545fc287dd5db9ab420  AethonMod/icon.png
c2b9429baa392efc9bed7f2882d45c87c8696a7a2451df1bde34b61f3071bc74  AethonMod/Content/Items/ResonanceShard.cs
c655729bc897ac7075fe9dd48c02e4e38510f643c99505b67b9e11e3af02ae29  AethonMod/Content/Biomes/HollowSanctumBiome.cs
d370f38431595f191253332aa1221b449a653598c2bcc5af17f06f04af3a2968  AethonMod/Content/Players/TestingPlayer.cs
d6feadef7209d8cdae7af55565bfd68ce9d84e3fc54c7ec6ee17aa478f1f4f80  AethonMod/Content/Globals/GlobalNPCXP.cs
dc1344c77168a087a96829a30482f56bd2f6b60669e09adb5c8a79ffd9dd8144  AethonMod/Content/Projectiles/CosmicOrbBolt.png
dce398dcd510487cf8545878b01abee1e3ed56e564b5228827283eb04e29152c  AethonMod/Content/Items/ResonanceShard.png
dd25d6477101525e13f4ea8de7572f45f430d00d80e9ebc021415f3a8cc136b3  AethonMod/Content/Weapons/Projectiles/ArcaneBolt.cs
e5f3012681e5f58f2d771fee5feb27175cbbbbaaf434934bafbc4ae8befffd95  AethonMod/Content/Weapons/GrimoireEternal.png
e7e6b702fba82c327bf992a0178aa8f6689e6d84f443a0bfb7e15ffac89eb903  AethonMod/AethonMod.cs
e84b83df913ecb1e5f0cfe53a48e1165baff65ee5c4e0d6e82e9285d3e2ffe71  AethonMod/Content/NPCs/TheWitness.cs
f541e4b650354a656b0dc5ccdc574d959340e982770b9c69ef972d3cc6beff61  AethonMod/Content/NPCs/RiftKeeper.png
f6d63cc2f954f32a876cee83a6b25dd036653371eaa4051fbbe4f58a62bc0c1f  AethonMod/Content/Items/LevelUpTester.png
f8e36528affff3610ddd96497111e04312b9f69b671dfef38d90cdc3a1e07d76  AethonMod/Content/AethonConfig.cs
