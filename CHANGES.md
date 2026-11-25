## v6.174 — LA INTERFAZ QUE RESPONDE, LA LUNA DE UNA PIEZA Y EL FARO DEL ECLIPSE — la letra del usuario (sesión 27, once puntos)

1. **LOS BOTONES (puntos 1 y 2)**: el «frame de gracia» de la v6.173 medía con el reloj del juego, que vuelve a cero al cargar un mundo, y ningún clic pasaba. Ahora: flanco simple, clic completado en finally, selector dueño único de su clic, ESC gastado, flancos reiniciados al abrir.
2. **LAS TINTAS (punto 1)**: altar y pilar con la paleta exacta del libro (148,72,178 · 168,108,44 · 118,96,70 · 140,40,40).
3. **CONSAGRAR SIN DUPLICAR (punto 3)**: nada sale del inventario hasta pulsar CONSAGRAR; la pila paga una unidad y la unidad sellada vuelve.
4. **LAS RANURAS DEL TRÍO (punto 4)**: el arma entra en la ranura clicada; el pilar pasa a motor puro; la ofrenda física se guarda solo en el mundo.
5. **EL TRÍO DISPARA (punto 5)**: cada arma pasa por el cañón real de vanilla (ItemCheck_Shoot); sin escribir en los ai de proyectiles ajenos. Albedrío por ranura (Original / Presa / Cursor) con su palomita, en dos bits de red.
6. **LA LUNA DE UNA PIEZA (punto 6)**: la causa real era la collera de 32 rectángulos con solape aditivo; TajoLib se reescribe sobre la malla continua de RayoStrip y nace TajoLib.Cinta.
7. **EL FARO DEL ECLIPSE (punto 10)**: arma original de rayo concentrado, 26 capas, 5 madera en el banco de trabajo; sprite, receta, bolsa y dos idiomas.
8. **LA LIBRERÍA DE HACES (punto 8)**: HazLib.Conducto.
9. **AUDITORÍAS (puntos 9 y 11)**: audfx_v6174.py y audita_v6174.py; el informe va aparte (Auditoria_AethonMod_v6.174.docx).

**GATES**: build 0 errores / 0 avisos · claves de las pantallas 0 faltantes · .tmod 21861448 bytes md5 834b3c221ac80ae4921f664a48d0f7cd · headless v6.174 hasta «Server started» con 0 excepciones.

## v6.172 — EL ARMA QUE NO LLEGABA, LA LIBRERÍA DE TAJOS REFORJADA Y UNA SOLA TINTA — la letra del usuario: «en el altar al poner el arma al slot para armas esta desaparece por completo del inventario, pero no llega al slot, el slot queda vacio» + «la libreria de tajos esta super obsoleta […] necesito que hagas una investigacion profunda en todo internet, en busca de mods y codigos […] hazle tantas mejoras como puedas» + «todavia el color de los textos en el altar y en el pilar estan mal, revisa los colores de la tipografia y has que los colores sean los mismos que la del libro de informacion» + «el cronista se ve bien, ahora en el mapa su cabeza tambien debe tener bruma o ser de bruma»

1. **EL ARMA DESAPARECIDA**: `ClicEnCandidato` hacía `TurnToAir()` sobre `elegido` (la MISMA referencia que `inventory[k]`) y DESPUÉS lo clonaba → clonaba aire → la ranura del trío quedaba vacía. La cura: el clon nace PRIMERO, el original muere después.
2. **LA LIBRERÍA DE TAJOS REFORJADA** (investigación: Sakugabooru, MonoGame docs, tML docs, easings.net, técnicas de Hollow Knight/Guilty Gear/Katana Zero, impact frames y smear frames del anime): EL DOBLE FILO (dos crecientes ×1.0/×0.82 en sentidos opuestos) · EL FRAME DE IMPACTO (flash monocromo los 2 primeros ticks) · LOS SMEARS (hojas fantasma con revelado adelantado) · LAS CHISPAS del filo exterior · EL ARCO ELÍPTICO (EscalaY 0.78) · TajoCruzado (la X de combo: cada 4 usos de la Regla del Cielo). Todos los llamadores viejos heredan el motor.
3. **UNA SOLA TINTA**: PrismaInteriorUI con los CUATRO valores EXACTOS del libro (148,72,178 · 168,108,44 · 118,96,70 · 28,20,16), el teal ajeno muere; los NewText del trío (160,230,255) y del pilar pasan a la familia clara de la casa.
4. **LA CABEZA DE BRUMA DEL MAPA**: ElCronista_Head.png regenerado — nube fBm violácea translúcida + corona dorada (el único sólido).
5. **LIMPIEZA**: ClickerGolpeProjectile huérfano + ClickerCasa.png extirpados, hjson ×2 comentado.
**GATES**: oráculo 0/0 (465 .cs) · -build 0/0 (532 entradas) · auditoría .tmod TODO VERIFICADO · headless sin excepciones · release 455814109 + CDN verificado md5 58813a1ad0f3e7e38d639c9207ff8a9e.

# AethonMod — Historial de Cambios

## v6.167 — EL REGRESO DEL EQUINOCCIO Y LA BOLSA DE LOS VEINTE (release 454373583 · md5 c7317205…)

1. **ATAQUE BÁSICO 931**: discos día/noche + OrigenDelDisparo (del libro) + tope que rota — La Palabra Cortada jubilada.
2. **CAZA**: la sombra re-busca presa si la suya muere (la causa: el jugador mataba la presa durante la persecución y la sombra moría sin morder).
3. **ALTAR/PILAR**: paleta del libro (papel + tinta).
4. **TRÍO**: izq equipa (sin duplicar — el arma SALE del inventario) · der desequipa (vuelve) · botón Desequipar todo · casilla bajo los slots.
5. **OFRENDA**: la bandeja vive en el libro + las Esencias del Eterno como candidatas (llave, tope 1000).
6. **DEVORACIÓN**: solo jefes en el cine (guard EsJefe); la chusma muere con checkDead. Jefe inobjetable y loot final intactos.
7. **CRONISTA v2**: la corona rúnica (melena + barba + túnica azul-noche). **NPC TEST**: clon exacto del jugador (SerializedClone + PlayerRenderer).
8. **MUNDO INTERIOR**: todas las zonas de bioma activas dentro — todo jefe invocable.
