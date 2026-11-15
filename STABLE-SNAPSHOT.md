# SNAPSHOT ESTABLE — v6.143 — EL ACCESORIO DE VERDAD

> **PUBLICADA — [release 446370078](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.143) + CDN verificado (md5 `e397f8c48f7c1181eb172b88850ffd3a`, 19.375.490 B, 524 entradas)**

## LA LETRA (sesión 24)

«Asegúrate que La Forma Eterna que sale en el despertar 112 sea el accesorio, ya que debe tener el efecto de aura de vacío, ya sabes, por lo tanto es un accesorio, recuerda el efecto de aura del vacío, con su respectiva quemadura y los buff y debuff».

## LA VERIFICACIÓN — el circuito completo, vivo y entero

- **ES EL ACCESORIO**: `FormaEternaItem` con `Item.accessory = true` — nace en el peldaño 112 (`ConvertirLaForma`) y funciona puesto Y en vanidad para el teatro visual.
- **EL CAMPO**: `UpdateAccessory` enciende el ancla `AuraJuicio2Ancla` — **19 tiles** (RADIO=300px), reconocida por `LlevaElAura` (v6.115: el vínculo ve la Forma Eterna con la misma fuerza que el ítem original).
- **LA QUEMADURA** (`QuemaduraVacioExterior`, debuff): los impíos que entran arden **×1→×3 por cercanía**, caminan a **×0,55** — hasta los **fantasmas**; al salir siguen ardiendo **1% de vida máx. por medio segundo durante 5 s** (y en PvP el jugador enemigo también arde — v6.141).
- **LOS BUFFS**: mientras un enemigo arde, el portador cobra **VACÍO EXTERIOR** (+10% ataque, +10 defensa, −15% coste de maná — `VacioExterior.cs:44-46`); **siempre**, el buff **AURA DEL VACÍO** (`AuraVacioBuff`, presa o no) y los **stats del panteón** del hueco funcional (+40 maná · +8% mágico · +4% crít. · +2 regen · +8% melée · +3% crít. · +3 def).

## EL TEXTO CURADO (lo que realmente faltaba)

El tooltip de la v6.142 decía «el poder de la forma ya no está aquí» — **falso**: el aura ES un efecto activo del accesorio. Reescrito ×3 idiomas: LO QUE ES (doce partes construidas en el despertar 101-112; la 12ª estalló LA MANIFESTACIÓN y la forma se CONVIRTIÓ en ESTE accesorio) + QUÉ SIGNIFICA USARLO (llevarla puesta enciende EL AURA DEL VACÍO: campo, quemadura, fuego de salida, Vacío Exterior, buff del aura y stats) — lector extendido a Linea13. Cazados los textos viejos: el bloque `FormaEterna` cantaba el SHIFT muerto de la v6.141 (→ LA MESA DEL ALTAR) y `CaminoManifestacion` decía «ítem cosmético» (→ EL ACCESORIO).

## PARA VERIFICAR EN JUEGO

- Despertar 112: la 12ª parte — el accesorio LA FORMA ETERNA nace (los visuales del portador desaparecen: la forma se convirtió).
- Puesta en el hueco de accesorio: el buff AURA DEL VACÍO aparece (siempre); un enemigo entra al campo — arde (la quemadura ×1→×3, lento) y TÚ cobras VACÍO EXTERIOR (+10% ataque, +10 defensa, −15% maná) mientras arda.
- El tooltip del ítem: LO QUE ES + QUÉ SIGNIFICA USARLO con el aura entera cantada.

## DOC-ROT CONOCIDA
CARACTERISTICAS.md / DISEÑO_DEL_MOD.md desactualizados — MAESTRO.md manda.
