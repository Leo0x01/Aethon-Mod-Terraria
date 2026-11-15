# SNAPSHOT ESTABLE — v6.140 — LA SESIÓN DEL PERGAMINO Y LAS SEIS CURAS

> **PUBLICADA — [release 446259255](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.140) + CDN verificado (md5 `05f048733833b880247f494fbe7aacc9`, 19.376.095 B, 524 entradas)**

## LA LETRA (sesión 21)

El pergamino aprobado con EL OJO que mira al cursor + toda la info · el hambre al 4% y las oleadas · la llegada en el mundo interior · la regeneración de los jefes del interior (matar la inmortal, dejar la regen por nivel) · el altar en el minimapa + el ítem devorado · el grimorio fantasma del altar · el aura rara del jugador · las Alas Rúnicas como copia exacta de la Lisa (dorado→morado, sin giro) · la Forma Eterna 100→1000 con la manifestación · las ideas del ataque básico.

## LAS NUEVE ENTREGAS

1. **HAMBRE AL 90%** — la furia estallaba al 40% (MomentosParaFuria 4/10); ahora 9/10, y el 100% es SIEMPRE. El susurro sigue desde el 1 (informativo).
2. **EL AURA RARA EXPLICADA** — era el AuraPortadorHalo de hambre prendiendo al 10%: ahora solo al 90% (aura = oleada prometida).
3. **LA LLEGADA JAMÁS EN EL INTERIOR** — guarda SubmundoGrimorio.Activo en los TRES caminos + la Tinta del Cometa.
4. **LA REGEN DE LA ARENA** — la vida ya NO se restaura (life=lifeMax cada 30t era inmortal); REGEN LICITA: nv10+ = 1%/s + 0,1%/nivel, SIN tope; cadencia de combos aprieta sin tope (piso 18t).
5. **ALTAR EN EL MINIMAPA + FANTASMA** — el altar pinta sus 6 celdas al registrarse (Map.UpdateLighting 40 + UpdateType — la cadena del decompile); el ítem de los altares extra devorado SIN drop (KillTile noItem); NUNCA dos GrimoriosDormidos en el mundo.
6. **ALAS RÚNICAS v2** — COPIA EXACTA de la Lisa (misma runa nodo 8, escala 1.45+0,35n, brillo, retraso de membrana) + dorado→morado lento (~37 s, fase por posición) + ángulo TANGENTE FIJO (extendiéndose afuera, trote suave al aleteo).
7. **FORMA ETERNA 100→1000** — NivelMax 1000, Ira en 100 (NivelIra), monedas 100P/peldaño desde el 100, 101-999 sin armas/materiales; piezas 200-900 (Brasa·Anillo·Sello·AnillosSol·Brillo·Ascendida·Ofanim·AuraVacío); AL 1000 LA MANIFESTACIÓN: FormaEternaItem + el estallido (CalculateHitInfo MaxValue+InstantKill → StrikeNPC — muerte válida con loot, respeta festín y pueblos), ManifestacionVivida persiste por libro.
8. **EL PERGAMINO IN-GAME (PergaminoUI)** — la hoja procedural (arena-oro, vetas, varillas) con 8 secciones de TODO el mod en 3 idiomas (48 claves), EL OJO sigue al cursor (pupila desplazada al ratón + párpado que parpadea solo), rueda (PlayerInput.ScrollWheelDelta) + flechas + ESC, tecla P junto al altar.
9. **ENTREGABLES** — mockup v2 fullHD (LXGW WenKai manuscrita + el ojo con pestañas mirando al texto) + el doc de 9 ideas del ataque básico (5 vistosas · 4 no vistosas · la recomendación: Página Rasgada + Marca Silenciosa).

## PARA VERIFICAR EN JUEGO

- Dejar pasar el hambre: susurros al 10%+, el AURA solo al 90%, la oleada al 90/100%.
- Entrar al mundo interior: SIN llegada jamás (ni con la Tinta).
- Arena: el jefe ya no se cura solo; al nivel 10+ regenera 1%/s visible (baja el DPS y sube).
- Colocar el altar: aparece en el minimapa AL INSTANTE; un altar extra lo devora SIN soltar ítem.
- Alas Rúnicas: idénticas a la Lisa pero dorado→morado lento y sin giro circular.
- Subir al 200+: cada 100 llega una pieza; el tooltip del despertar cuenta hasta 1000.
- Junto al altar, tecla P: EL PERGAMINO con el ojo siguiendo el cursor.

## DOC-ROT CONOCIDA
CARACTERISTICAS.md / DISEÑO_DEL_MOD.md desactualizados — MAESTRO.md manda.
