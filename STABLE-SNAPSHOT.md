# SNAPSHOT ESTABLE — v6.136 — LA PURGA DE LAS DOS ARMAS + EL MUÑECO DE LA CASA

> **PUBLICADA — [release 445612303](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.136) + CDN verificado byte a byte (md5 `c083e217dd39955ae63d119acb72e4bf`, 19.354.517 B, 521 entradas)**

## LA LETRA (el usuario — la sesión 17)

«Arbitrador II, El Principio y el Fin, puedes borrar esas armas, ya no son útiles, el resto de contenido de la carpeta se queda tal cual, ya que no tiene menciones externas ni nada de eso» + «Genera las texturas con SForja, y luego verifica que todo esté sin mencionar ni referencias externas»

## LAS DOS ARMAS DE INVESTIGACIÓN MURIERON

El Arbitrador II (la jabalina que atravesaba muros) y El Principio y el Fin (las dagas del canje de almas) desaparecen con TODO su árbol: proyectil, dagas voladoras, almas de eco y de luz inferior, onda sonora, las 4 marcas (Marca del Principio · Mirada del Fin · Supresión Vital · Servidumbre), el sigilo ratero y la memoria de capas del GlobalNPC — **13 archivos + 4 carpetas al cero**. **Se quedan tal cual**: la Mirada del Eterno, el Cetro de las Dagas con sus cinco cuchillas, el Códice y el motor de píxeles.

## EL LAVADO DE LAS MENCIONES (los que se quedan)

La máquina de las cuchillas (DagaEternaMinion — 100 % de la casa desde la v6.132) contaba su historia con nombres prestados: la cabecera citaba una ficha ajena «palabra por palabra» y 16 comentarios la llamaban por su nombre viejo. TODO lavado en términos de la casa. La Quemadura del Vacío ya no se presenta como «nuestra Holy Flames».

## EL MUÑECO DE PRUEBAS NEGRO ES DE LA CASA (SForja)

Perdió el override a la textura del juego base (NPC_113) y ganó SU sprite 40×56 nacido por código: arpillera gris-violeta cosida con parche, ojos violetas que brillan, boca cosida, cruzeta de madera negra con veta, manojos de paja violeta con puntas brillantes, petate atado, base clavada — la rampa negro-violeta completa. **Cadena**: 3 rondas pluma/refina (NO-SHIP 5,5→7→5,5) → generador PIL puro (`tools/gen_dummynegro_v6136.py`) → 2 iteraciones QA VLM → **SHIP 9/10**. El ítem lo toma prestado por override (lo resuelve el GUARDIÁN).

## PARA VERIFICAR

- Oráculo verify.csproj **0 errores / 0 warnings** · **-build REAL 0/0** (521 entradas, 19.354.517 B, md5 `c083e217dd39955ae63d119acb72e4bf`)
- **GUARDIÁN de texturas: TODO VIVE ✔** (el ítem prestado resuelto)
- **AUDIT v6.136 TODO VERIFICADO ✔** — 48 checks: EOF · blobs 521/521 · hjson==fuente ×3 · purga completa (árbol, .tmod, DLL) · claves condenadas 0 · **la verificación integral de la letra: 0 menciones externas en TODO cs+hjson** · vivos en fuente+DLL ×6 · no-regresión (stabber muerto · máquina viva · arcángel intacto · catálogo 12) · muñeco (rawimg viaja · 0 texturas vanilla · override del ítem · silueta 529 px) · paridad 1661³
- **headless**: Sandboxing v6.136, **0 excepciones**

## PENDIENTE PARA LA PRÓXIMA SESIÓN

1. **Verificación en juego** por el usuario: el nuevo sprite del muñeco en el mundo (el QA VLM dio SHIP 9/10; la palabra final es del jugador).
2. Doc-rot conocida: CARACTERISTICAS.md (v6.50.30) y DISEÑO_DEL_MOD.md (v6.50.61) desactualizados en conteos — MAESTRO.md lleva los datos vigentes.
3. Los mundos guardados no pierden nada: las armas borradas nunca salieron de la carpeta de pruebas y no había saves que dependieran de ellas.
