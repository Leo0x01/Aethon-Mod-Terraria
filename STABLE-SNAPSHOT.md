# SNAPSHOT ESTABLE — v6.142 — LA FORMA EN SU DESPERTAR Y EL RELOJ DE VERDAD

> **PUBLICADA — [release 446344047](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.142) + CDN verificado (md5 `2e8af7377e0c911eaa6850a5e07d4b24`, 19.368.967 B, 524 entradas)**

## LA LETRA (sesión 23)

La Forma Eterna debía estar en EL DESPERTAR (101-112, una parte por peldaño, 10P+1000 esencias +10%/nivel, +4% ataque a todo el grimorio, pieza visual puesta SOLO usando el libro; al 112 la MANIFESTACIÓN + el ítem nace + los visuales DESAPARECEN convertidos) · la escalera a 120 con el poder extra 113-120 (+10% ataque/nivel, coste +20%/nivel) · el indicador de hambre es LA MÁQUINA DEL GRIMORIO HAMBRIENTO NERVIOSO a velocidad NORMAL (el Nervioso es la demo acelerada) · la descripción del cosmético: lo que es + qué significa usarlo.

## LAS SIETE ENTREGAS

1. **EL DESPERTAR A LOS 120 (DespertarGrimorio.cs)**: NivelMax=120 · NivelCaza=100 (la cima de la caza no se mueve: Aethon y la Ira siguen en el 100) · NivelFormaInicio=101 · NivelManifestacion=112. BonusDano POR TRAMOS: +1%/nivel (1-100) · +4%/nivel (101-112) · +10%/nivel (113-120) — +228% acumulado a 120, a TODO el ataque del grimorio (mágico e invocación: Equinoccio y ojos pasan por él). MonedasPara/EsenciasPara: 10P+1000 la primera parte, ×1,10 hasta la 12, ×1,20 desde el 113 (verificado por código: 101=10,00P+1000 · 112=28,53P+2853 · 120=122,68P+12268).
2. **LAS MESAS SEGURAS**: ArmasDe()/GruposMaterialesDe()/CantidadMateriales protegen >100 (las tablas viven hasta 100 — los 4 accesos directos migraron) · LaMesaEstaCompleta: arriba del 100 SOLO esencias+monedas · SubirNivel no cobra Devorar arriba del 100 · los hitos del camino CaminoForma1-11 + CaminoManifestacion + CaminoPoderExtra (la escalera UI pinta las partes).
3. **LA CONSTRUCCIÓN (DespertarGrimorioSistema.SubirNivel → FormaEternaPlayer.ConstruirParte)**: cada peldaño 101-112 construye UNA PARTE (aviso + sonido) · al 112 LA CONVERSIÓN (ConvertirLaForma): el ítem cosmético nace + ManifestacionDeLaForma (una vez) + Convertida=true (los visuales YA NO visten al portador — la letra: «desaparece ya que es convertida en el item cosmetico»).
4. **EL VESTIDO (FormaEternaPlayer.PostUpdateEquips)**: las partes visuales SOLO sosteniendo el libro · la 12ª enciende EL AURA DEL VACÍO (el ancla) hasta la conversión · EL ATAQUE vive en BonusDano (no en el player — un solo sitio de verdad).
5. **EL RELOJ DE VERDAD**: GrimoireEternal.TasaHambre=1/216000 (LA HORA — la máquina del Nervioso a velocidad normal: ojo rojo 20-80%, inquietud 50%+, caza propia 25%+, festín por rareza) · ShardPlayer.SegundosPorMomento=360 (90% a los 54 min, 100% a la hora — LA OLEADA calzada al reloj del arma) · los espejos v6.141 muertos (subida y cena).
6. **LA DESCRIPCIÓN DEL COSMÉTICO (FormaEternaItem + hjson ×3)**: Titulo + Linea1-11 — LO QUE ES (doce partes, cada una creció puesta en el portador como un miembro más; la duodécima estalló y la forma se convirtió en el ítem) + QUÉ SIGNIFICA USARLO (vestir al dios entero; el poder quedó horneado en el grimorio). El lector extendido a Linea11.
7. **EL LIBRO ACTUALIZADO (hjson ×3)**: despertar 1-120 · la forma en su sección (101-112, la mesa, la conversión) · el hambre con su máquina de verdad · Auras3 sin el aura muerta. FIX: una comilla recta interna en en-US Linea11 rompía el parser hjson — el headless lo cazó (el oráculo C# no valida hjson); comillas tipográficas.

## PARA VERIFICAR EN JUEGO

- Despertar 100 pagado + 10P + 1000 esencias en el banco: la mesa del altar sube al 101 — LA ESCRITURA se construye y viste al portador CON el libro en la mano.
- Peldaño a peldaño hasta el 112: la 12ª parte — el estallido de LA MANIFESTACIÓN, el ítem LA FORMA ETERNA en el inventario… y los visuales del portador DESAPARECEN (la forma se convirtió).
- 113-120: cada peldaño +10% de ataque a TODO (el Equinoccio Y los ojos pegan más).
- Una hora sin comer: el ojo del libro enrojece y se inquieta progresivamente — al 90% (54 min) la oleada PUEDE estallar; a la hora, SEGURA.
- El tooltip del cosmético: LO QUE ES + QUÉ SIGNIFICA USARLO.

## DOC-ROT CONOCIDA
CARACTERISTICAS.md / DISEÑO_DEL_MOD.md desactualizados — MAESTRO.md manda.
