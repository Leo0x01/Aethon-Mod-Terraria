#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v6566.py — CHANGES.md + STABLE-SNAPSHOT.md de la v6.56.6."""
import io

CAMBIOS = """# v6.56.6 — LA PURGA, LAS ALAS SIMÉTRICAS, EL RC2 DESNUDO Y LAS NUEVE VARIANTES

**La letra del usuario**: «faltan textos en muchos lugares… en la barra de exp en vez de decir nivel, dice mods.aethonmod.nivel o algo asi» + «las alas no están simétricas y siguen teniendo esa ligera rotación en el sentido contrario que no deben tener» + «borra todas las nuevas armas y solo deja al rayo concentrado 2» + «hay un círculo negro, no debe de haber ningún círculo que cubra al personaje… tampoco tiene que tener lente gravitación, lo único que debe tener son los círculos rúnicos y además deben ser al menos un 50% más pequeños» + «le quitaste los efectos y otras texturas, ahora solo usa 1 textura, le faltan más texturas y más efectos, como el desenfoque, el glow, el brillo» + «debes usar los mismos brillos que usa el accesorio de El brillo de soles rúnicos» + «debe ser lo más vistoso posible, lleno de efectos visuales… debe mantener un pequeño temblor» + «haz otra variantes del rayo concentrado 2, al menos 9 variantes más con diferentes efectos y técnicas vfx» + «haz una auditoría de todas las librerías».

## (1) LOS TEXTOS CRUDOS — diagnóstico forense: el en-US del fallback
Los hjson es-ES/es-MX del paquete estaban **completos** (verificado con el parser Hjson REAL de tML extraído del release: 1.390 claves, 0 avisos, todas las claves del código cubiertas; las 17 «faltantes» del primer análisis eran falsos positivos del esquema de variantes Voz1-4 y del segundo archivo del Escriba). **El bug real**: tModLoader cae SIEMPRE al **en-US** cuando la cultura activa no tiene una clave — y el en-US fue RETIRADO en la v6.56.4 («solo deja español es_es»). Un juego en inglés (o alemán, francés…) mostraba TODAS las claves localizadas CRUDAS («Mods.AethonMod.HUD.Nivel» en la barra de exp) mezcladas con los strings español-hardcodeado — de ahí «el archivo no está completo». **LA CURA**: el en-US VUELVE como **espejo de seguridad con el MISMO texto español** (la red de seguridad del motor — el mod habla español en CUALQUIER idioma del juego y las claves crudas son imposibles). La edición sigue siendo UNA: solo el es-ES; `tools/sync_es_mx_v6564.py` regenera los 4 espejos (es-MX ×2 + en-US ×2) con aserción de cuerpo idéntico. Bonus de la auditoría: **tML no tiene es-MX** (GameCulture.KnownCultures verificado por reflexión: en-US, de-DE, it-IT, fr-FR, es-ES, ru-RU, zh-Hans, pt-BR, pl-PL — el «Español» del juego ES es-ES; el es-MX era peso muerto inofensivo) y la clave del bioma `Biomes.HollowSanctumBiome` ya existía como `DisplayName` (el formato moderno — la auditoría la esperaba como `BiomeName` legacy).

## (2) LAS ALAS — el barrido rígido mataba la simetría (el sim lo cazó)
Reconstruido el sim 1:1 (`/home/z/sim/sim_alas_v6566.py` — puerto exacto de ConstruirPluma, la permuta filo/lomo, el espejo y la tira de vértices). **EL HALLAZGO**: el «Barrido del Heredero» de la v6.56.4 rotaba TODO el enjambre como cuerpo rígido (`RotatedBy(−barrido)`) antes del espejo — y **una rotación rígida NO conmuta con el espejo** (Mirror(Rot(θ)) = Rot(−θ) espejado): el ala izquierda dejaba de ser el espejo de la derecha (**Δ 12.0 px en reposo, 42.6 px a plena velocidad** — el par se leía torcido). Y el término `0.4` fijo del barrido dejaba un **rake de +5.13° incluso PARADO** — «ESA ligera rotación en el sentido contrario» que el usuario ve desde la v6.50. Tampoco podía ser una traslación (Mirror(x+d) = x−d: el par quedaría descentrado — la queja original «no están bien centradas»). **LA CURA**: el barrido rígido MUERE ENTERO. La respuesta al movimiento ya existe y es 100 % simétrica: **Spread** (las alas se abren con la velocidad) y **Flap** (el aleteo se acelera). **VALIDADO**: Δ par = **0.0000 px** en reposo/vuelo/backpedal × ambas direcciones + verificación visual VLM del render («par mariposa perfecto, sin rotaciones espurias»).

## (3) LA PURGA DE LAS ARMAS NUEVAS
«Borra todas las nuevas armas y solo deja al rayo concentrado 2»: la escalera del Rayo (Alba/Veredicto/Juicio), el Prisma, el Partidor, el Réquiem, la Ira, el Índice, la Pluma y el Ojo **MUEREN** — 12 archivos .cs, 10 sprites, sus 10 bloques hjson + 11 líneas de proyectiles del Escriba, y las 10 entradas de la bolsa. La exclusión de haces que vivía en `EscaleraDelRayo` se incrusta en el RC2 (UN solo haz a la vez — y como las variantes son EL MISMO proyectil, la exclusión las cubre a todas).

## (4) EL RC2 DESNUDO Y VESTIDO A LA VEZ
- **EL CÍRCULO NEGRO MUERE**: el disco AlphaBlend del «Portal del Conjuro» (el horizonte que OSCURECÍA al personaje) — retirado entero. **LA LENTE MUERE**: los dos `GravLens.Registrar` (carga y láser) — retirados.
- **LAS CORONAS AL 53%**: `Lerp(150,56) → Lerp(70,26)` — «los círculos rúnicos… al menos un 50% más pequeños»: la triple corona de OrbitaLib y los dos anillos finos, ahora a escala de conjuro íntimo.
- **LOS SOLES RÚNICOS DE LA CARGA**: «los mismos brillos del accesorio» — LA RECETA EXACTA de `BrilloSolarHalo` (DrawTwinkles de los soles de 3 anillos+): **EL DESTELLO DE 4 PUNTAS** (dos cápsulas cruzadas de blanco incandescente + corazón dorado), naciendo en la banda elíptica que ENCOGE con la carga (170→22 px — el arma los ABSORBE) y el ciclo nace-arde-muere ACELERANDO; al disparar, **EL ESTALLIDO**: 16 destellos EXPULSADOS en abanico por la dirección del rayo. Los viejos DestellosDeCarga/Estallido (SparkleStar genérico) MURIERON.
- **LA PILA DE CAPAS** («le faltan más texturas y más efectos»): encima de la tira del shader (las 4 texturas del plasma) viajan: **EL DESENFOQUE** (remolino a 1.8× de semiancho, al 30%), **EL GLOW** (carne fBm a 1.35× con la rampa del gradiente), **EL BRILLO** (la vena central blanca, ventana V 0.36-0.64) y **LOS SOLES DEL CONDUCTO** (7 destellos de 4 puntas corriendo por dentro del haz — la firma de la carga, ahora en el conducto).
- **EL TEMBLOR VIVE**: sostenido 6.5 (≈2.75 px constantes) + encendido 9 (≈7.3 px de pico) — «debe mantener un pequeño temblor de la pantalla mientras se esté ejecutando».

## (5) LAS NUEVE VARIANTES — una técnica por variante (del catálogo de 33 librerías)
La conducta es UNA (la del RC2); cada variante cambia la PIEL: el gradiente, la boca, la luz y **LA TÉCNICA** extra del haz. La variante viaja en `ai[1]` (el campo Modo — SIEMPRE 0 en el RC2; Projectile.ai solo tiene 3 slots en este tML, verificado por reflexión: un `ai[3]` habría reventado en runtime). **Las nueve**: **PRISMA** (aberración cromática: el haz partido en rojo/cian + gradiente arcoíris) · **ECLIPSE** (bruma negra-violeta exhalada por el conducto — SombrasLib) · **TORMENTA** (rayos que REPTAN por el haz — StormLib.Bolt) · **SOLAR** (lenguas de fuego solar — PyraLib.Flame con la rampa SolarFire) · **VÓRTICE** (nebulosas cian-violeta orbitando — NebulaLib.Nube) · **ESPECTRO** (dos fajas fantasma escoltando desfasadas, el flujo al revés) · **SIERPE** (el EJE del conducto ondula como serpiente — anclado a boca y punta) · **CUÁNTICO** (fajitas ramificadas que nacen y mueren — la superposición de caminos) · **NOVA** (anillos de pulso que VIAJAN hacia afuera — OndaLib.Pulse). Iconos procedurales de familia (orbe + haz diagonal + glifo de técnica + destello de 4 puntas; VLM 8/10 «sin problemas graves»). Todas en la Bolsa de las Definitivas junto al original.

## (6) LA AUDITORÍA DE LAS 33 LIBRERÍAS (el informe completo en el worklog)
**Salud global**: sana en lo grande (contratos de lote unificados, Hash01 determinista, presupuesto adaptativo). **3 duplicaciones estructurales**: la tríada de cámara (Pantalla/OndaLib.Kick/PulsoLib → fusionar en PantallaLib), la tríada de rayos (StormLib/RayoLib/BeamLib → BeamLib MUERTA, migrar las 3 armas a StormLib), la tríada de estelas (EstelaLib/Cinta → el pool anti-patinaje de Cinta a EstelaLib). **~40 métodos públicos muertos verificados** (BeamLib entera, LumenPalettes, VoidQueen, 10/16 de PulsoLib, 13/22 de DefinitivasLib…) y **57 copias privadas de Tint/Hash01/Ellipse** que deberían vivir solo en VFXCore. **Lo que falta para el futuro** (Tasks 86-103): `TiemposLib` (hitstop + slow-mo — la pieza de mayor impacto de las Tasks 87/88) y `PostFXLib` (aberración de pantalla completa sobre el pipeline blindado de BlackHoleLensSystem — Task 96), más 4 resurrecciones estratégicas esperando al jefe espejo (FormaLib, CompasLib, MoldeLib, MediaResLib). **Top 10 acciones** por valor/esfuerzo en el informe.

**VERIFICACIÓN**: oráculo 0/0 (entorno RECONSTRUIDO tras el wipe: SDK 8.0.412 re-descargado + tML 2026.08.3.0 md5 `dac04c7a` EXACTO, el zip verificado) · `-build` REAL 0/0 ×2 · `.tmod` v6.56.6 (6.385.927 B, md5 627e6e27…): **464 entradas** (+1 .tmod neto: −22 muertos +9 iconos +2 hjson en-US +…), EOF exacto, TODOS los blobs inflan, 4 .fxc DENTRO/0 .fx, **los 6 hjson** (es-ES/es-MX/en-US ×2) con espejos de cuerpo IDÉNTICO, las claves de las 9 variantes presentes, los 22 muertos AUSENTES, loop .ogg **byte a byte intacto** vs .56.5, las firmas nuevas (SolesRunicos/PilaDeCapas/VariantesRC2) PRESENTES y las muertas AUSENTES · headless: **0 EXCEPCIONES** (Sandboxing→Configuring→Finalizing→mundo generado).

---

"""

SNAPSHOT = """# v6.56.6 — LA PURGA + LAS ALAS SIMÉTRICAS + EL RC2 DESNUDO + LAS 9 VARIANTES — EL CHECKLIST

> **PUBLICADA** — release pendiente de llenar al publicar.

**Qué pasó** (la letra): textos crudos («mods.aethonmod.nivel» en la barra de exp) · alas aún asimétricas con «esa ligera rotación en el sentido contrario» · «borra todas las nuevas armas y solo deja al rayo concentrado 2» · el RC2 sin círculo negro, sin lente, coronas −50%, más texturas/efectos y LOS MISMOS brillos del accesorio de soles rúnicos · «al menos 9 variantes más con diferentes efectos y técnicas vfx» · auditoría de librerías.

1. **LOS TEXTOS CRUDOS (la cura del fallback)**: [ ] con el juego EN INGLÉS el mod se lee ENTERO en español (la barra de exp dice «Grimorio · Nv X», no «Mods.AethonMod.HUD.Nivel»); [ ] con el juego en Español idéntico que antes; [ ] ningún texto crudo «Mods.AethonMod…» en ninguna parte (el en-US espejo es la red de seguridad del motor).
2. **LA PURGA**: [ ] en la bolsa de las Definitivas SOLO el RC2 y sus 9 variantes; [ ] la escalera/Prisma/Partidor/Réquiem/Ira/Índice/Pluma/Ojo GONE (ni en recetas ni en el bestiario raro); [ ] los personajes con las armas viejas las ven como items desaparecidos (esperado — la purga es la purga).
3. **LAS ALAS**: [ ] el par ESPEJO PERFECTO parado (nada de un ala más abierta que otra); [ ] SIN rotación rara en reposo (el rake de 5° murió); [ ] al correr/volar responden igual que antes (Spread abre, Flap acelera) pero SIN torcerse el par.
4. **EL RC2 DESNUDO**: [ ] SIN círculo negro sobre el personaje (el disco murió); [ ] SIN lente gravitacional (la curvatura del espacio murió); [ ] las coronas rúnicas VISIBLES pero pequeñas (~la mitad); [ ] LA CARGA: destellos de 4 PUNTAS (cápsulas cruzadas + corazón dorado — LOS MISMOS del accesorio El brillo de soles rúnicos) reuniéndose/acelerando hacia el arma; [ ] AL DISPARAR: los destellos salen EXPULSADOS en abanico hacia el rayo; [ ] EL HAZ: desenfoque difuso en los bordes + glow del gradiente + vena central BLANCA incandescente + soles de 4 puntas corriendo por dentro (muchas texturas, muchos efectos); [ ] el temblor sostenido mientras el rayo vive + el golpe de encendido.
5. **LAS 9 VARIANTES** (todas cargan/disparan/temblan igual; SOLO cambia la piel): [ ] **PRISMA** rojo+cian cremando el costado, gradiente arcoíris; [ ] **ECLIPSE** violeta oscuro exhalando bruma negra; [ ] **TORMENTA** rayos azules reptando por el conducto; [ ] **SOLAR** lenguas de fuego doradas brotando; [ ] **VÓRTICE** nebulosas cian-violeta orbitando; [ ] **ESPECTRO** dos fajas fantasma verdes escoltando; [ ] **SIERPE** el haz entero ondulando como serpiente ámbar; [ ] **CUÁNTICO** ramitas magenta/cian naciendo y muriendo; [ ] **NOVA** anillos rosas viajando hacia afuera; [ ] solo UN haz a la vez (el original y las variantes se excluyen entre sí).
6. **REGRESIÓN**: [ ] el RC2 original (variante 0) conserva SU juicio violeta-oro; [ ] los sonidos idénticos (carga + bucle — el bucle byte a byte intacto); [ ] las demás armas del mod intactas; [ ] F8: presupuesto sano.

---

"""

with io.open('/home/z/my-project/download/Aethon-Mod-Terraria/CHANGES.md', 'r', encoding='utf-8') as f:
    viejo = f.read()
with io.open('/home/z/my-project/download/Aethon-Mod-Terraria/CHANGES.md', 'w', encoding='utf-8') as f:
    f.write(CAMBIOS + viejo)
print('CHANGES.md OK')

with io.open('/home/z/my-project/download/Aethon-Mod-Terraria/STABLE-SNAPSHOT.md', 'r', encoding='utf-8') as f:
    viejo = f.read()
with io.open('/home/z/my-project/download/Aethon-Mod-Terraria/STABLE-SNAPSHOT.md', 'w', encoding='utf-8') as f:
    f.write(SNAPSHOT + viejo)
print('STABLE-SNAPSHOT.md OK')
