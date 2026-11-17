#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v6151.py — LA ENTRADA DE LA v6.151 EN CHANGES.md (la casa).
La letra del usuario citada textual, los 12 puntos, los gates."""
import os
RAIZ = "/home/z/my-project/mod"
os.chdir(RAIZ)

ENTRADA = """## v6.151 — LA SESIÓN DEL BRILLO CUADRADO Y LA CAZA DEL SOL: doce puntos, doce curas

**La letra del usuario (textual): «el brillo en el icono de buff de la ira, es solo un circulo plano demasiado grande… en este caso seria bueno un pequeño brillo cuadrado que sobresalga al menos 1 a 5 pixeles del icono» + «no puedo activar la ira de un ser exterior, en el momento en el que voy a activarla todos los minions desaparecen, ademas quiero que limites el ataque basico del grimorio del eterno a solo 10» + «el sol formado por la fusion no debe explotar asi nada mas, debe mantenerse, perseguir a los enemigos y despues de un tiempo debe crecer al menos 70% mas su tamaño, volverse completamente rojo y luego explotar» + «que sean la misma velocidad que los ticks de los agujeros runicos» + «en mi personaje de prueba sin importar cuantos libros lleve a despertar 101 a 120 no puedo ver como se construye» + «explota en nivel 112… el 113 y 119 no hacen nada, ademas la explosion es horrible… esa es la explosion que quiero para cuando termina la construccion de la forma eterna» + «esta cortado en la parte superior» + «deben estar los tentaculos de la sombra de la pagina en el fondo del mundo interior» + «que sean letras con bordes blanco centro negro» + «ese poder en el portador» + «el % de hambre sea el % de probabilidad de que se active una oleada» + «aparecio en modo guardian de la mazmorra… eso no debe pasar» + «sacale los colmillos y los ojitos».**

1. **GLOWUILIB — LA LIBRERÍA DE LA LUZ DE LA INTERFAZ** (la letra pidió «un shader o libreria que permita iluminar, opacar o desaturar partes de la interfaz o cualquier otra cosa en terraria»): `Content/VFX/GlowUILib.cs` — cuatro primitivas de UI (`MarcoLuz`: el marco cuadrado que SOBRESALE con degradado monotónico · `Iluminar`: velo aditivo · `Opacar`: velo que oscurece · `Desaturar`: velo gris), TODO determinista (R5) y con el SoftGlow de la casa estirado en barras (R2: jamás círculos planos).
2. **EL BRILLO CUADRADO DEL BUFF DE LA IRA**: el círculo plano MURIÓ — `MarcoLuz` dibuja el marco que SOBRESALE del icono (4 px al estar LISTA, latido 0.78+0.22·sin; 1→4 px creciendo durante la carga). **EL CONTADOR mm:ss MURIÓ** con la letra («si el brillo sale bien el contador no hace falta»). SIM 1:1 medido: sobresale 2-4 px (dentro del 1-5 pedido), degradado MONOTÓNICO verificado, el encendido gris→color de abajo hacia arriba sigue vivo.
3. **LA IRA NO DEVORA A SUS TESTIGOS**: el gesto izq+der es SOLO de la ceremonia — `IraSilenciaElLibro` calla el Shoot del grimorio ~0,1 s al dispararla (los minions ni se re-cuentan ni se tocan: son el círculo del ritual, no su precio).
4. **EL ATAQUE BÁSICO DEL ETERNO: PERFORACIÓN 10**: los discos del Equinoccio (marcas 9201/9202) calzan `penetrate=10` al nacer (antes -1 = infinito): diez cuerpos y se apaga.
5. **EL JUICIO DE LOS DOS SOLES — LA FUSIÓN SIN SALTO + LA CAZA**: el diagnóstico del «salto» — el gigante nacía a 0.7·BodyPx con jitter aleatorio cuando el vals moría a 0.55 (corte de fase). LA CURA: `RadioFusion` (una sola curva continua 0.55→1.05, smoothstep) y el temblor aleatorio MUERTO. Y EL ACTO NUEVO: **LA CAZA (5 s)** — el sol fusionado PERSIGUE a la presa más cercana (`PresaMasCercana`, homing que acelera), SE MANTIENE, **CRECE +70 %** (`CRECE_CAZA`), **ENROJECE por completo** (la paleta se pierde en el carmesí, el pulso arterial late al final) y SOLO ENTONCES explota — **su daño escala ×1→×1.85 y su hitbox 30→300 px** durante la caza (la letra: «su daño aumenta y ademas su caja de colisiones tambien»).
6. **LOS TICKS DE LA IRA = EL RITMO DE LOS AGUJEROS RÚNICOS**: `localNPCHitCooldown` 10→6 — el ritmo del CENTRO del agujero rúnico (su aura golpea cada 6-24 t según cercanía). Solo la velocidad: el cálculo de daño INTACTO.
7. **LA FORMA ETERNA SE CONSTRUYE HASTA EL 120 DE VERDAD**: la autopsia — el peldaño 112 llamaba `ConstruirParte(112-100)=ConstruirParte(12)`: construía LA 12ª EN EL 112, disparaba conversión+manifestación y `FormaEscupida` apagaba TODO (por eso 113-119 «no hacían nada»). LA ESCALERA EXACTA: 101-111 → partes 1-11 · 112-119 → los anillos de soles rúnicos (el vestido los enciende uno a uno) · 120 → la 12ª + LA CONVERSIÓN. Y el vestido YA NO se apaga por una escupida ajena: lo decide EL DESPERTAR DE ESTE LIBRO (cada grimorio construye SU forma — el personaje de prueba VUELVE A VERLA).
8. **LA EXPLOSIÓN DE LA FORMA ETERNA ES LUZ PURA**: el teatro de 220 polvos + el AtaqueJefeProjectile (las «rayas») MURIÓ — la manifestación del 120 es **EL ESTALLIDO DE LA MASCOTA DE LUZ** (`MiniEstallidoPet`): 22 rayos deterministas + cruz anamórfica + núcleo que inunda + estrella + onda + bokeh, 100 % código, cero texturas.
9. **EL PILAR SIN CORTE**: el SpecialDraw dibuja el marco de 86 px de UNA pieza con 6 px de aire sobre el capitel (la punta jamás roza el límite) + EL AURA violeta que abraza al pilar (SoftGlow con latido) + el OJO-MINION sigue naciendo sobre el prisma.
10. **EL FONDO DEL MUNDO INTERIOR ES LA SOMBRA DE LA PÁGINA**: la capa lejana se viste de TINTA — `tintaLejos` (el negro-violeta profundo de la sombra de página), tiras más anchas y el flujo más vivo; EL POLVO ESTELAR MURIÓ (no es un cielo estrellado: es el cuerpo del dios) — en su lugar LAS MOTAS DE TINTA que caen lento. Sin ojos ni colmillos (siguen muertos).
11. **EL LIBRO DE INFORMACIÓN**: el scroll se apaga con 30 px de aire sobre el texto · la capa del libro ABIERTO vive AL FINAL de la lista de interfaces (ENCIMA de TODO: inventario, barras, mapas) · LA X pegada a la esquina superior derecha con su hitbox de 36 px dibujada EXACTA (donde tocas es donde cae) · y EL CUERPO con BORDE BLANCO y CENTRO NEGRO (el doble pase de la casa — los títulos quedan como estaban).
12. **«ESE PODER EN EL PORTADOR»**: la descripción de la Forma Eterna ya no dice «jugador» — el portador es el portador ×3 idiomas (es-ES «en el portador» · en-US "inside the bearer").
13. **EL % DE HAMBRE ES LA PROBABILIDAD**: `ProbabilidadOleada` — ≤89 % → 1 % · 90 % → 5 % · 91 % → 10 % · … · 99 % → 50 % · **100 % → ACTIVACIÓN DIRECTA**, con el % CONTINUO del reloj del libro (`PorcientoHambreDelLibro` lee `Estado.Hambre` de verdad). Victoria sube el nivel de furia, derrota lo mantiene (la ley de siempre, intacta).
14. **EL ESQUELETRON YA NO ES GUARDIÁN DE MAZMORRA ADENTRO**: la autopsia — el mediodía eterno del interior (`Main.dayTime=true` cada tick en `PreUpdateWorld`) PISABA el préstamo de noche del sello de oleada → la IA vanilla veía DÍA → modo guardián (cabeza girando, embiste). LA CURA: `OleadaNPC.NochePrestadaActiva` — mientras un jefe de oleada pelea con SU noche, el clamp del mediodía LO RESPETA.
15. **LA SOMBRA DE LA PÁGINA DEL GRIMORIO, LISA**: `SombrasLib.Masa` gana el modo `lisa` (sin espinas de hueso, sin ventosas) y el lazo del Eterno lo usa — y los ojos entornados del lazo MURIERON. Tinta pura que fluye; el resto del mod queda IGUAL (las sombras de los jefes conservan sus colmillos).

— oráculo 0/0 (470 .cs, GlowUILib nueva) · build 0/0 (529 entradas, 19.398.750 B) · GUARDIÁN TODO VIVE ✔ · AUDIT v6.151 LIMPIA (firmas de la letra + no-regresiones v6.150) · headless: Sandboxing → Adding Content → Adding Recipes → Choose World SIN EXCEPCIONES · SIM 1:1 del brillo TODO MEDIDO Y VERDE.

"""

with open("CHANGES.md", encoding="utf-8") as f:
    s = f.read()
ancla = "# AethonMod — Historial de Cambios\n"
assert s.startswith(ancla), "el CHANGES no abre con el ancla"
# insertar justo tras el PRIMER ancla (la sección más nueva vive arriba)
s = ancla + "\n" + ENTRADA + s[len(ancla):]
with open("CHANGES.md", "w", encoding="utf-8") as f:
    f.write(s)
print("  ✔ CHANGES.md — la v6.151 al frente del historial")
