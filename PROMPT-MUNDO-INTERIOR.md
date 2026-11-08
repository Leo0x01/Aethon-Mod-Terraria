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
Trabajas con: `SubmundoGrimorio` (el motor, 800×450, SP), `AltarPrismaTile`
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
  plaza (v6.118: el terreno es continuo — la red queda para el fondo).
- **El festín es del libro**: las oleadas del interior NUNCA cruzan la
  frontera (`DetenerFestin` en entrar y salir) y nunca suben el nivel
  natural del portador (`natural:false` — probar no es progresar).
- **El prisma no se rompe** (MinPick 9999) — y LA PÁGINA tampoco (la
  veta es hueso del Eterno). Siempre hay vuelta.
- **La revelación y el faro** son los únicos overlays de pantalla: el
  velo viste la oscuridad por capas con BrumaFX (cero rects planos).
- **Cero círculos planos**: toda niebla es BrumaFX, todo destello
  monotónico (NovaBurst), las motas son ParticleManager — la letra
  v6.118 («te dije que usaras nuestras librerias») es LEY.
- Cada textura nueva nace en LA FORJA (`tools/superforja.py` o el PIL
  ×4 SS de la casa — v6.118: `tools/piel_eterno_v6118.py`), con su QA
  del ojo, ANTES de tocar el .cs.

---

## ESTADO (v6.118 — lo que YA vive)

- ✅ **EL REWORK COMPLETO** (la letra + el cuestionario): la SUPERFICIE
  continua de borde a borde (colinas BrumaNoise + corteza dorada + la
  contratapa abajo), la plaza con su prisma, LAS VETAS de LA PÁGINA DEL
  ETERNO, EL CORAZÓN geoda con su boquilla y LAS VENTANAS de vida.
- ✅ EL CIELO DEL CUERPO: `CieloInteriorEterno` (el CustomSky) — las TRES
  CAPAS de tentáculos con la PIEL DEL ETERNO de la Forja, los ojos que
  MIRAN, el polvo estelar, las brumas de paralaje y EL LATIDO del
  corazón hinchándolo todo (~5 s). EL OJO COLOSAL MURIÓ (el
  cuestionario: FUERA — los ojos del cielo lo reemplazan).
- ✅ LA OSCURIDAD POR CAPAS + LAS BRUMAS DE VERDAD: el velo viste el
  techo/horizonte/costados con BrumaFX (engordando con el latido), las
  COLUMNAS de las ventanas, las VOLUTAS y la bruma REACTIVA que se abre
  a tu paso. CERO círculos planos (la letra).
- ✅ LAS MOTES DE ORO (ParticleManager + EmitLight) y EL FARO de la
  devoración con masas fBm que aceleran (el hook OnPostDraw intacto).
- ✅ **EL CICLO ETERNO DE LAS OLEADAS DEL HAMBRE**: cada ~2,5 min de
  calma, `GrimorioFuriaSistema.Provocar` sirve un festín (enemigos de
  la casa, nivel del portador, `natural:false`) — LA REGLA «SIN FIERAS»
  MURIÓ con la letra nueva («debe ser el de las oleadas de hambre»):
  el interior ES el mundo del asedio eterno; fuera del festín, la
  soledad sigue mandando (el spawn gate solo abre para el festín).
- ✅ LA DEVORACIÓN ES EL CALCO DEL FESTÍN (el libro con párpado e iris,
  la sombra de la página, el cerco) y LA PUERTA VIVE EN LA BOCA (la
  colisión del jugador con el rectángulo de la boca — jamás un timer).
- ✅ Eclipse permanente (sol Sun3 del cenit + tinte + cementerio 0.90) y
  EL CEDIMIENTO: durante el festín, EL ECLIPSE DE LA OLEADA manda.
- ✅ El motor (entrar/salir), el cerrojo de guardado, LA LEY DE LAS
  SECCIONES del minimapa (800×450), la red de caída y la música.
- ⬜ Los escalones que QUEDAN de la lista vieja (cartografía viva ·
  vétebras · lluvia de esencias con susurros · juicio del eclipse ·
  sala del fusionado · multijugador) — leídos a la luz del rework.
