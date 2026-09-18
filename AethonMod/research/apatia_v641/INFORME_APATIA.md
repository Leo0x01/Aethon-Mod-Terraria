# INFORME DE INVESTIGACIÓN — EL ARMA DEL "TOMO DE LA APATÍA NULA" (réplica exacta)
**Task ID: 2-a · Mod destino: AethonMod (tModLoader 1.4.4) · Propósito: copiar EXACTAMENTE su proyectil como arma de pruebas**

Fuente primaria: el código fuente público del mod de origen (rama 1.4.4), localizado por búsqueda en la API de
GitHub (la grafía correcta del arma: `Apathanull` — Items/Weapons/Magic/Apathanull.cs → dispara
`CosmicTentacle`). Archivos descargados y leídos completos en esta carpeta (`.cs` de referencia).
Los PNG de las texturas de partículas se midieron pero NO se copiaron (AethonMod genera los suyos).

---

## 1) EL ARMA (Apathanull.cs — 56 líneas)

| Campo | Valor |
|---|---|
| daño / clase | 63 / Magic |
| crit | 12 |
| maná | 26 |
| useTime / useAnimation / reuseDelay | 8 / 20 / 8 |
| knockback | 5.5 |
| rareza / valor | Roja / precio rojo del ecosistema |
| shoot / shootSpeed | CosmicTentacle / 12 |
| autoReuse | sí · noMelee sí |
| **Shoot** | `velocity.RotatedByRandom(0.7f)` — la nube de gestación ±0.7 rad |
| sonido | un burn grave con pitch congelado −0.45..−0.6 (calco vanilla: Item12 pitch −0.5) |
| receta original | SpellTome + material del ecosistema ×18 + librería (la casa usa madera 5 — arma de pruebas) |

## 2) EL PROYECTIL (CosmicTentacle.cs — 207 líneas) — EL CEREBRO COMPLETO

- **Defaults exactos**: 90×90 (¡hitbox gigante!), friendly, penetrate −1, **extraUpdates 8** (9 AI-ticks por frame),
  tileCollide false, ignoreWater, Magic, usesLocalNPCImmunity, **localNPCHitCooldown = 8·extraUpdates = 64**.
- **El proyectil es INVISIBLE** (`Texture => Projectiles/InvisibleProj`): TODO el visual son partículas + dusts.
- **Fase 1 — preDamage (ticks 0..120)**: no puede dañar (`CanDamage => preDamage ? false : null`).
  Frena `velocity ×0.96` por tick; mientras `dist(dueño) < 1400 && time > 3` escupe CADA AI-tick:
  2 pulsos (negro AlphaBlend + verde aditivo, escala 0.6·scaler y 0.48·scaler con
  `scaler = GetLerpValue(−60, 120, time)`). En `time == 3`: 6 dusts custom con jitter.
- **El DESPERTAR (time == 120)**: 13 dusts (66/263), `preDamage = false`, `moving = true`,
  `scalingTimer = 90`, `velocity = rumbo` (rumbo = hacia la MIRA del dueño ×7, girado 0.2·sentido).
- **Fase 2 — EL AZOTE (moving, 3 curvas)**: `velocity = velocity.RotatedBy(curva·sentido)` con
  `curva = GetLerpValue(90, 0, scalingTimer)·0.04`; `scaling = GetLerpValue(0, 90, scalingTimer)`;
  chispas cada 2 ticks de timeLeft (negra AlphaBlend ×0.85 + verde aditiva ×0.75, escala 0.07·scaling,
  estirado (1.7−(1−sharp), 0.9+(1−sharp)·2)); dust 1/6 (278/267). `scalingTimer--`.
- **EL SALTO (scalingTimer ≤ 0)**: si curvas > 1 → teleport `NextVector2Circular(100,100)` + pulso negro
  0.6 + 2 pulsos verdes 0.36 vida 45 + velocity = 0; si no → Kill. `moving = false`.
- **LA RECARGA (!moving)**: `scalingTimer += 2`; al llegar a 90: `curves--`; si quedan → 7 dusts (191/custom),
  `velocity = rumbo` (RE-APUNTA a la mira), **sentido INVERTIDO**, `moving = true`, `numHits = 0`; si no → Kill.
- **LA LEY DEL DAÑO**: `ModifyHitNPC`: `damageMult = Clamp(GetLerpValue(5, 1, numHits), 0.7, 1)` —
  ×1.0 el primer golpe, decae a ×0.7 tras 5 impactos (el tentáculo se aburre). `OnHitNPC` (numHits==0):
  sonido + 6 dusts custom en el objetivo.
- **MUERTE**: 9 dusts (66/263) con la velocidad del tentáculo.

## 3) LAS PARTÍCULAS (CustomPulse / CustomSpark — las medidas exactas)

**CustomPulse** (los pulsos de vacío): textura LargeBloom **360×360**; escala `Lerp(original, final, PolyOut(4))`
(¡IMPLOE: original 0.6→final 0!); opacidad `sin(π/2 + completion·π/2)`; vida 4 ticks (los del salto: 45);
velocity ×0.95; rotación aleatoria al nacer (guardada en la partícula); luz del color ×MakeLight.
El NEGRO va con AlphaBlend (oscurece — el vacío); el verde aditivo.

**CustomSpark** (las chispas de cola): textura GlowSpark **2048×2048** (escala 0.07 → 143 px base);
`scale ×0.95`/frame; color `Lerp(color, Transparent, pow(completion, 3))`; velocity ×0.95;
**rotación = velocity.ToRotation() + π/2** (perpendicular al vuelo); estirado fijo al nacer;
la negra va en AlphaBlend ×0.85.

**El dust custom (VoidDustInverted)**: 3 capas a mano en PreDraw — núcleo NEGRO (SmallBloom 400² ×0.068
y ×0.057 = 27/23 px), halo de COLOR (BloomCircle 200² ×0.07 = 14 px), perla sólida (BasicCircle 64²
×0.075 = 4.8 px); `rotation += sign(vel.X)`, `velocity ×0.98`, `scale +0.02` sin gravedad / −0.01,
luz `clamp(scale·0.8)·color`.

## 4) LA TRADUCCIÓN A LA CASA (lo implementado en v6.41)

- El item: `TomoApatiaNula` (stats 1:1). El proyectil: `TentaculoCosmicoProjectile` (puerto línea a línea;
  la mira por `Main.MouseWorld` — exacto en un jugador, arma de pruebas).
- Los pulsos y chispas: búfers de instancia con la edad por `GameUpdateCount` (los originales envejecen
  por FRAME — con extraUpdates 8 sería invisible si envejecieran por AI-tick) y las curvas EXACTAS.
- El dust: `PolvoVacioInvertido` (Content/Dusts) con las 3 capas y las escalas mapeadas a las texturas de la casa.
- Los sonidos .ogg del original no se copian (calco vanilla Item12/Item14 con el mismo perfil de pitch).

## 5) FUENTES (en esta carpeta)
`Apathanull.cs` · `CosmicTentacle.cs` · `CustomPulse.cs` · `CustomSpark.cs` · `VoidDustInverted.cs`
(repo público del mod de origen, rama 1.4.4, vía API de GitHub autenticada; texturas medidas por PIL:
LargeBloom 360², GlowSpark 2048², BasicCircle 64², BloomCircle 200², SmallBloom 400²).
