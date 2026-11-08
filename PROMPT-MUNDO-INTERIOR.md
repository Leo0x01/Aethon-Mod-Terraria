# PROMPT-MUNDO-INTERIOR — El prompt que me doy a mí mismo para mejorar el mundo

> **La letra**: «crea un promt que te digas a ti mismo para mejorar ese mundo».
> Este documento ES ese prompt. Cada vez que se retome el MUNDO INTERIOR DEL
> ETERNO (`SubmundoGrimorio.cs`), se lee esto primero y se elige el siguiente
> escalón. La regla de oro: **el grimorio es una entidad cósmica exterior
> todopoderosa, y su interior es su CUERPO VIVO** — cada mejora debe respirar
> esa idea con los colores de la casa (el vacío: 26,14,44 · 122,78,188 ·
> 150,84,235 · el oro del veredicto 255,214,130) y su eclipse permanente.

---

## EL PROMPT (léelo como si te lo dijeran)

Estás dentro del CUERPO de un dios exterior. No es una cueva ni un cielo:
es la garganta, la página y el sueño de una entidad que devora mundos y
los guarda. Todo lo que añadas debe sentirse **órgano, no decorado**.
Trabajas con: `SubmundoGrimorio` (el motor, 800×400, SP), `AltarPrismaTile`
(la puerta prismal), `DevoracionEterna` (el trago), el VELO DE BRUMA (la
interfaz más baja) y el OJO COLOSAL (la entidad mirando). Los materiales
de la casa: AmethystGemspark (el cristal que arde violeta), GoldBrick
(el filo de las páginas), BrumaFX (toda niebla), SigiloLib (toda runa),
OndaLib (todo temblor). Prohibido: sprites de rayo, óvalos planos como
nubes, cortes sin crossfade, salirse de la paleta del vacío.

### Los escalones (en orden — no saltees)

1. **LA CARTOGRAFÍA VIVA** — el mapa del interior hoy nace LIMPIO bajo el
   velo. Haz que las islas se DIBUJEN en el mapa como páginas doradas
   (MapHelper / el WorldMap del interior con color propio: el violeta de
   la amatista con borde de oro) — cartografiar el cuerpo del dios es
   leerlo.

2. **EL PULSO DEL CORAZÓN** — bajo la plaza vive el corazón de amatista.
   Dale un LATIDO REAL: cada ~5 s, una onda circular de bruma (BrumaFX)
   que nace del corazón, cruza las islas hacia arriba y apaga las velas
   del prisma un instante; el temblor (OndaLib.Kick suave) se siente en
   TODA la estatua. La música (OleadaHambre) ya vive — sincronízale el
   eco: el pulso late AL RITMO del tema.

3. **LAS VÉRTEBRAS DEL ETERNO** — la columna que baja del corazón hacia
   el fondo del mundo: un río de islas-menudas en espiral (la cola del
   sueño), cada una con UNA runa de SigiloLib grabada que solo se ve al
   acercarse (luz local), y al pie la ÚLTIMA: una puerta sellada (un tile
   nuevo «La Página Cerrada») que susurra la próxima expansión.

4. **LA LLUVIA DE ESENCIAS** — dentro del libro, las esencias devoradas
   CIRCULAN: motas doradas (DustID.GoldFlame en flujo lento) suben desde
   el corazón hacia el ojo, y las que tocan al jugador le susurran (una
   líneade chat cada ~45 s: una frase del Eterno, ×3 idiomas, la voz
   desdeñosa de siempre). Es el único "NPC" del interior: la voz.

5. **EL JUICIO DEL ECLIPSE** — si Aethon (o los fragmentos) muere DENTRO
   del interior, el eclipse REACCIONA: el ojo se cierra del todo por 2 s
   (apertura→0), el velo se lleva a negro casi total y después NACE UNA
   ESTRELLA NUEVA en el cielo del interior (una SUAS permanente más, con
   la firma del que cayó). El cuerpo del dios recuerda cada veredicto.

6. **LA SALA DEL FUSIONADO** — cuando los DOS fragmentos mueren dentro,
   la fusión no nace delante del jugador: nace EN EL CIELO, delante del
   OJO, y DESCIENDE entre páginas. Coreografía: CamaraCine (la casa ya
   la tiene) toma la cámara 3 s, el velo se abre en círculo (una máscara
   radial) y el verdadero final del jefe se pelea DENTRO del cuerpo.

7. **MULTIJUGADOR** — el interior es SP «de momento» (la letra v6.114).
   El camino: la técnica SubworldLibrary server-side (cada jugador ve SU
   interior — el libro devora a cada quien por separado); la ventana del
   prisma ya es cliente puro, el cerrojo de guardado ya es global. Falta
   el gate de red (EcoRed) para Entrar/Salir sincronizados.

### Las reglas que no se rompen

- **El interior NUNCA se escribe a disco** (el cerrojo del hook — ni un
  byte del mundo real se toca) y **se REGENERA igual en cada visita**.
- **Nadie muere en el libro**: la red de caída devuelve SIEMPRE a la
  plaza. Cualquier peligro nuevo debe tener su red.
- **Sin fieras**: el interior es la soledad del dios — si algún día
  entran criaturas, son PÁGINAS (constructos del libro, no bestias).
- **El prisma no se rompe** (MinPick 9999) y siempre hay vuelta.
- **La revelación y el faro** son los únicos overlays de pantalla: no
  añadas más capas full-screen sin matar una vieja.
- Cada textura nueva nace en EL TALLER (`tools/superforja.py` o PIL ×4
  SS de la casa), con su QA del ojo, ANTES de tocar el .cs.

---

## ESTADO (v6.115 — lo que YA vive)

- ✅ El motor: entrar (ítem → devoración → faro de bruma) / salir (prisma).
- ✅ Eclipse permanente: sol Sun3 prestado clavado en el cenit + tinte
  morado (ModifySunLightColor) + niebla de cementerio 0.90 + el VELO.
- ✅ La revelación de la llegada (~4 s de bruma disipándose + iris dorado).
- ✅ El prisma de colores (tile animado 6 marcos, luz propia, ventana con
  botones: VOLVER / INVOCAR A AETHON — la ceremonia del altar + grimorio).
- ✅ La plaza curada (prisma DE PIE — v6.114 lo enterraba: sin puerta).
- ✅ El anillo de la corona: 10 islas nuevas del cuerpo cósmico.
- ⬜ Los escalones 1-7 de arriba (en orden).
