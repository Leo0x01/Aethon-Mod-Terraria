# SNAPSHOT ESTABLE — v6.141 — LA SESIÓN DEL LIBRO ABIERTO Y LAS SEIS VERDADES

> **PUBLICADA — [release 446319797](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.141) + CDN verificado (md5 `55cb6ef94085c995fb2f12efcec0686c`, 19.383.189 B, 524 entradas)**

## LA LETRA (sesión 22)

Todo gira a su ojo — EL OJO QUE MIRA como arma nueva (ojo cerca del enemigo → mirada → marca → quemadura del vacío en PvP) · la Forma Eterna de verdad (nivel NORMAL del libro desde 100, 12 partes, 10P+1000 esencias +10%/nivel, puestas en el jugador con el libro) · el libro de la Sforza con el ojo de los minions (el pergamino murió) · el ojo rojo como único indicador de hambre (el aura rara eliminada) · el fantasma del altar · el altar en el minimapa · los jefes del interior (lejos, sin clima, Skeletron normal) · las alas entregadas.

## LAS OCHO ENTREGAS

1. **EL OJO QUE MIRA (Weapons/Ojos/ElOjoQueMira.cs)**: LaMiradaProyectil (el ojo de LOS MINIONS se materializa a 140px de la presa bajo el cursor, carga 16 t, dispara) → LaMiradaRayo (rayo atravesante de 340 px, AABBvLineCollision) → LaMarcaSilenciosa (ModBuff: 2 s de glifo → enciende QuemaduraVacioExterior 5 s con intensidad 1,5 y dueño). PvP: OnHitPlayer + MarcarPlayer (la quemadura v6.141 Update(Player): 1% vida máx/medio s). Mejora: +1,5% daño y −0,5% maná POR NIVEL DEL LIBRO. ENTREGA directa + receta (15 esencias + estantería).
2. **LA FORMA ETERNA v2 (Players/FormaEternaPlayer.cs)**: el despertar REVERTIDO a 100 (NivelMax=100; piezas 200-1000 y NivelIra muertos). La construcción: NivelLibro (ShardLevelItem) ≥ 100 → 12 partes (la Escritura/AnilloDorsal · 3 Anillos · Primera Luz · Brasa · 3 Soles/BrilloSolar · Ofanim · Manto · Aura del Vacío). Coste: MonedasParte=10M·1,1^n + EsenciasParte=1000·1,1^n. Cada parte: +4% DamageClass.Generic y SU visual en PostUpdateEquips SOLO sosteniendo el libro. La 12: QuickSpawnItem(FormaEternaItem) + ManifestacionDeLaForma (públicada desde DespertarGrimorioSistema). El pago: SHIFT+clic derecho del libro (CanUseItem). Persistencia: fePartes/feManifestacion.
3. **EL LIBRO DEL ETERNO (UI/LibroDelEternoUI.cs)**: EL ICONO del grimorio junto al botón de logros (geometría vanilla del decompile: 20+560·escala+num; el nuestro a su izquierda, SOLO con el inventario abierto). EL LIBRO ABIERTO: tapa de cuero + chapas + lomo cosido con puntadas + DOS PÁGINAS (izq: altar/interior/oleadas/grimorio · der: despertar/ataques/forma/auras) + EL OJO DE LOS MINIONS (OjoEterno_Abierto) siguiendo al cursor con parpadeo. PergaminoUI = redirector (la tecla P vive).
4. **UNA SOLA HAMBRE**: GrimoireEternal.TasaHambre=0 (la del arma murió); ShardPlayer espeja et.Hambre=MomentosHambre/10 (y a 0 al comer). EL AURA DEL PORTADOR MUERTA (el bloque del halo v6.140 eliminado — el modo 0 no se engendra jamás). La furia sigue: 9/10=90% posible, 10/10=100% SIEMPRE.
5. **EL FANTASMA MUERTO**: RevivirLibro re-ancla (AltarUnicoSistema.ActivoX/Y: cima−40−21) y la pendiente APRENDE la posición nueva.
6. **LOS JEFES DEL INTERIOR**: JefeInteriorGlobalNPC.PreAI fuerza ai[1]=1 en SkeletronHead interior (el AI_011 de vanilla con IsItDay lo metía en el spin-guardián por el mediodía eterno — verificado en el decompile). InvocarJefeInterior: ancla a 100 tiles (dirX·100). Regen licita verificada (1%+0,1%/nv desde 10, sin tope).
7. **EL FARO DEL MINIMAPA**: AltarUnicoSistema.PostUpdateTime repinta las 6 celdas cada 60 ticks (RepintarEnMapa).
8. **LAS ALAS RÚNICAS ENTREGADAS** (EntregarSiNoTiene en el regalo de pruebas).

## PARA VERIFICAR EN JUEGO

- El OJO QUE MIRA (regalo de pruebas al entrar): clic sobre un enemigo — el ojo aparece a su lado, la mirada lo atraviesa, la marca y las llamas violetas.
- PvP: golpear a otro jugador con la mirada — la marca lo enciende y ARDE.
- Libro nivel 100+: SHIFT+clic derecho del libro — el aviso de la parte construida (10P+1000 esencias la primera).
- El icono del grimorio junto al botón de logros (abre el inventario): un clic — EL LIBRO, con el ojo siguiendo el cursor.
- Dejar pasar el hambre: el ojo del libro se pone ROJO e inquieto — sin aura sobre el jugador NUNCA.
- Cargar el mundo con una llegada pendiente: el libro re-nace ENCIMA del altar.
- Interior: invocar a Skeletron — la cabeza NORMAL (sin spin); todos los jefes nacen LEJOS.
- El minimapa: el altar activo SIEMPRE visible (faro de 1 s).

## DOC-ROT CONOCIDA
CARACTERISTICAS.md / DISEÑO_DEL_MOD.md desactualizados — MAESTRO.md manda.
