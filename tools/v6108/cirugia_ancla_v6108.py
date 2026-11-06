#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.108 — LA CIRUGÍA DEL ANCLA: EL CALENTAMIENTO AERODINÁMICO.

La letra del usuario: «la parte abombada de la medialuna tiene que
mirarme a mi en todo momento, deben nacer del centro de la medialuna en
la parte abombada y extenderse en todas las direcciones pero siempre
alejandose de mi, a mayor cercania mas particulas y la medialuna cambia
de color volviendose ligeramente morada negra, y la bruma debe hacer lo
mismo que las particulas» — EL CONCEPTO: el portador es el PLANETA, su
aura la ATMÓSFERA y el impío cae en REENTRADA.

La cirugía:
  (1) LA MEDIALUNA = EL ARCO DE CHOQUE — rot = dir − π/2: la CONCHA
      ABOMBADA MIRA AL PORTADOR en todo momento; el centro del
      semicírculo cae sobre el corazón y la concha se abomba hacia el
      planeta hasta ~RadioDomo.
  (2) LAS CHISPAS = EL PLASMA — nacen del PUNTO DE ESTANCAMIENTO (el
      tope abombado), abanico de fuga COMPLETO ±π/2 (todas las
      direcciones que se alejan), DENSIDAD ∝ cercanía (ritmo 6→1,
      ráfagas 1→3) y estela más larga (1.6–2.6 cúpulas).
  (3) LA BRUMA = LA ESTELA — mismo punto de nacimiento, mismo abanico,
      puffs 2→5 con la cercanía.
  (4) EL COLOR — la medialuna se enfría en sombra con la cercanía:
      Lerp hacia MORADA NEGRA en ambos pases (el brillo violeta se
      apaga cuando el impío cae hondo).
"""
import io, sys

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs"
s = io.open(RUTA, encoding="utf-8").read()
orig = s

def reemplaza(viejo, nuevo, etiqueta):
    global s
    if viejo not in s:
        print(f"FALLO: {etiqueta} — no hallado")
        sys.exit(1)
    if s.count(viejo) != 1:
        print(f"FALLO: {etiqueta} — {s.count(viejo)} ocurrencias")
        sys.exit(1)
    s = s.replace(viejo, nuevo)
    print(f"OK: {etiqueta}")

# ------------------------------------------------- (0) cabecera: la versión
reemplaza(
    "    //  AuraJuicio2Ancla — v6.105 → v6.106 → v6.107 — EL ANCLA INVISIBLE DEL",
    "    //  AuraJuicio2Ancla — v6.105 → v6.106 → v6.107 → v6.108 — EL ANCLA INVISIBLE DEL",
    "cabecera versión")

# ------------------------------- (1) cabecera: LA LETRA v6.108 + EL CONCEPTO
reemplaza(
    """    //  direccion opuesta al jugador».
    //
    //  LA CURA v6.107 — EL TEATRO SE PONE DE PIE Y EXPULSA DEL CORAZÓN:""",
    """    //  direccion opuesta al jugador».
    //
    //  LA CORRECCIÓN DE LA LETRA (v6.108): «la parte abombada de la
    //  medialuna tiene que mirarme a mi en todo momento, deben nacer
    //  del centro de la medialuna en la parte abombada y extenderse
    //  en todas las direcciones pero siempre alejandose de mi, a
    //  mayor cercania mas particulas y la medialuna cambia de color
    //  volviendose ligeramente morada negra, y la bruma debe hacer
    //  lo mismo que las particulas. el objetivo es que parezca
    //  calentamiento aerodinamico igual que las cosas que entran a
    //  la atmosfera del planeta, pero en este caso yo seria el
    //  planeta y todo lo que entre en mi atmosfera que seria al aura
    //  del juicio, sufriria de calentamiento aerodinamico, entiendes
    //  el concepto».
    //
    //  EL CONCEPTO v6.108 — EL CALENTAMIENTO AERODINÁMICO: el
    //  portador es EL PLANETA, su aura LA ATMÓSFERA y cada impío un
    //  objeto CAYENDO EN REENTRADA — el ARCO DE CHOQUE (la medialuna)
    //  lo envuelve POR DELANTE con la CONCHA ABOMBADA MIRANDO AL
    //  PLANETA (en todo momento: el impío siempre «cae hacia ti»), y
    //  del PUNTO DE ESTANCAMIENTO (el tope abombado — donde el choque
    //  arde más) brota el PLASMA: chispas y bruma que se derraman
    //  hacia ATRÁS en TODAS las direcciones de fuga (siempre
    //  alejándose del portador), MÁS DENSAS cuanto más hondo cae el
    //  impío (a mayor cercanía, más partículas)… y el arco MISMO se
    //  enfría en sombra: la medialuna se vuelve LIGERAMENTE MORADA
    //  NEGRA con la cercanía.
    //
    //  LA CURA v6.107 — EL TEATRO SE PONE DE PIE Y EXPULSA DEL CORAZÓN:""",
    "cabecera: letra v6.108 + concepto")

# --------------------------------------- (2) cabecera: el resumen sección 2)
reemplaza(
    """    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo): LA MEDIALUNA VERTICAL (la
    //      cúpula) + LAS CHISPAS EXPULSADAS DEL CORAZÓN + LA BRUMA QUE
    //      SE DERRAMA POR EL MISMO RUMBO (ver LA CURA v6.107 arriba).""",
    """    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo): EL ARCO DE CHOQUE (la
    //      medialuna con la concha abombada MIRANDO al portador) +
    //      EL PLASMA del punto de estancamiento (chispas y bruma
    //      derramándose hacia atrás — ver EL CONCEPTO v6.108 arriba).""",
    "cabecera: resumen sección 2")

# ------------------------------------------- (3) el título de la sección AI
reemplaza(
    """            // ==============================================================
            //  2) LAS CHISPAS DEL CORAZÓN (solo cliente — el teatro
            //     de los impíos: NACEN del corazón y salen EXPULSADAS
            //     hacia fuera, SIEMPRE en la dirección OPUESTA al
            //     portador — la MISMA fuga de la bruma).
            // ==============================================================""",
    """            // ==============================================================
            //  2) EL PLASMA DE LA REENTRADA (solo cliente — el teatro
            //     de los impíos: NACE del PUNTO DE ESTANCAMIENTO — el
            //     tope abombado de la medialuna, donde el choque arde
            //     más — y se derrama hacia ATRÁS en TODAS las
            //     direcciones de fuga, SIEMPRE alejándose del
            //     portador — la MISMA fuga de la bruma).
            // ==============================================================""",
    "AI: título sección 2")

# ------------------------------------------- (4) el título del método Vestir
reemplaza(
    """        // ================================================================
        //  EL TEATRO DE LOS IMPIOS — LAS CHISPAS DEL CORAZÓN (fase de
        //  UPDATE: el spawn de polvos naciendo del corazón del impío; la
        //  bruma y la cúpula viven en el render).
        // ================================================================""",
    """        // ================================================================
        //  EL TEATRO DE LOS IMPIOS — EL PLASMA DE LA REENTRADA (fase de
        //  UPDATE: el spawn de polvos naciendo del PUNTO DE
        //  ESTANCAMIENTO — el tope abombado de la medialuna; la bruma y
        //  el arco de choque viven en el render).
        // ================================================================""",
    "método: título VestirALosImpios")

# ------------------------------------------------ (5) el bloque de chispas
reemplaza(
    """                // ----------------------------------------------------------
                //  v6.107 — LAS CHISPAS DEL CORAZÓN: «las particulas
                //  deben nacer del centro de los enemigos y luego
                //  dispersarce hacia fuera pero siempre en la direccion
                //  opuesta al jugador».
                //
                //  CADA CHISPA NACE EN EL CORAZÓN del impío (una cuna
                //  con un temblor mínimo) y sale EXPULSADA hacia FUERA,
                //  SIEMPRE en la dirección OPUESTA al portador (el
                //  jugador a la derecha → nace del pecho y vuela a la
                //  IZQUIERDA, abriéndose en abanico — así para TODAS las
                //  direcciones). Cuanto más pegado arde el impío, más
                //  rápida nace y más denso hierve su corazón.
                // ----------------------------------------------------------""",
    """                // ----------------------------------------------------------
                //  v6.108 — EL PLASMA DEL PUNTO DE ESTANCAMIENTO:
                //  «deben nacer del centro de la medialuna en la parte
                //  abombada y extenderse en todas las direcciones pero
                //  siempre alejandose de mi, a mayor cercania mas
                //  particulas».
                //
                //  CADA CHISPA NACE EN EL TOPE ABOMBADO de la medialuna
                //  (EL PUNTO DE ESTANCAMIENTO — el punto del arco que
                //  MIRA al portador, donde el choque comprime y arde)
                //  y se derrama hacia ATRÁS en TODAS las direcciones de
                //  fuga (el abanico trasero COMPLETO ±90°: el plasma
                //  rodea el cuerpo y deja la estela — así para TODAS
                //  las direcciones, SIEMPRE alejándose del portador).
                //  Cuanto más hondo cae el impío (más cercanía), MÁS
                //  DENSA hierve la reentrada.
                // ----------------------------------------------------------""",
    "chispas: bloque v6.108")

# ------------------------------------------------------- (6) el ritmo ∝ k
reemplaza(
    """                int ritmo = (int)MathHelper.Clamp(4f - k * 2f, 1f, 4f);  // 1 de cada 4 lejos… de cada 2 pegado""",
    """                // LA DENSIDAD DE LA ATMÓSFERA — a mayor cercanía, MÁS
                // PARTÍCULAS (el impío cae más hondo: cada 6 ticks en
                // el borde… CADA TICK pegado, en ráfagas).
                int ritmo = (int)MathHelper.Clamp(6f - k * 5f, 1f, 6f);""",
    "chispas: ritmo ∝ cercanía")

# ----------------------------------------------------- (7) la ráfaga ∝ k
reemplaza(
    """                    int rafaga = k > 0.55f && Main.rand.NextFloat() < 0.5f ? 2 : 1;""",
    """                    int rafaga = 1 + (k > 0.4f ? 1 : 0)
                        + (k > 0.75f && Main.rand.NextFloat() < 0.5f ? 1 : 0);""",
    "chispas: ráfaga ∝ cercanía")

# ------------------------------------------- (8) el abanico de fuga ±90°
reemplaza(
    """                        // EL RUMBO — SIEMPRE OPUESTO AL PORTADOR: el
                        // abanico alrededor de la dirección portador →
                        // impío (TODAS las chispas comparten la MISMA
                        // fuga: la que se aleja del jugador).
                        float abanico = Main.rand.NextFloat(-0.85f, 0.85f);
                        Vector2 rumbo = dir.RotatedBy(abanico);""",
    """                        // EL RUMBO — EL HEMISFERIO DE FUGA COMPLETO: el
                        // plasma se derrama en TODAS las direcciones que
                        // SE ALEJAN del portador (el abanico trasero
                        // ±90° alrededor de la fuga — el flujo del
                        // choque rodeando el cuerpo en su caída).
                        float abanico = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                        Vector2 rumbo = dir.RotatedBy(abanico);""",
    "chispas: abanico ±90°")

# ------------------------------------- (9) la cuna = punto de estancamiento
reemplaza(
    """                        // LA CUNA — nace del CORAZÓN del impío (un
                        // temblor mínimo para que no nazcan todas del
                        // mismo pixel).
                        Vector2 cuna = n.Center + new Vector2(
                            Main.rand.NextFloat(-1f, 1f) * n.width * 0.08f,
                            Main.rand.NextFloat(-1f, 1f) * n.height * 0.08f);""",
    """                        // LA CUNA — EL PUNTO DE ESTANCAMIENTO: el tope
                        // ABOMBADO de la medialuna (el punto del arco
                        // que MIRA al portador — allí el choque comprime
                        // y arde, y de allí nace el plasma), con un
                        // temblor mínimo para que no nazcan todas del
                        // mismo pixel.
                        Vector2 cuna = n.Center - dir * (RadioDomo(n, k) * 0.95f) + new Vector2(
                            Main.rand.NextFloat(-1f, 1f) * n.width * 0.08f,
                            Main.rand.NextFloat(-1f, 1f) * n.height * 0.08f);""",
    "chispas: cuna = punto de estancamiento")

# ------------------------------------------- (10) la estela más larga
reemplaza(
    """                        // LIBERADA — la cuna y el rumbo quedan congelados
                        // AL NACER: aunque el impío camine, la chispa
                        // termina SU vuelo sola (PolvoExpulsionJuicio
                        // manda). El camino: del corazón hacia afuera,
                        // hasta ~2 cúpulas de distancia.
                        float distMax = RadioDomo(n, k) * Main.rand.NextFloat(1.35f, 2.1f);""",
    """                        // LIBERADA — la cuna y el rumbo quedan congelados
                        // AL NACER: aunque el impío camine, la chispa
                        // termina SU vuelo sola (PolvoExpulsionJuicio
                        // manda). El camino: del punto de estancamiento
                        // hacia ATRÁS — la ESTELA de la reentrada, larga
                        // y desgarrada (hasta ~2.6 cúpulas).
                        float distMax = RadioDomo(n, k) * Main.rand.NextFloat(1.6f, 2.6f);""",
    "chispas: estela 1.6–2.6")

# ---------------------------------------------- (11) el título del PreDraw
reemplaza(
    """        // ================================================================
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  DEL CORAZÓN (los puffs de BrumaFX naciendo del corazón y
        //  derramándose hacia fuera, en la misma dirección e intensidad
        //  variable) y LA MEDIALUNA VERTICAL (la cúpula morada y negra
        //  que se corona sobre el impío).
        // ================================================================""",
    """        // ================================================================
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  DE LA REENTRADA (los puffs de BrumaFX naciendo del punto de
        //  estancamiento y derramándose hacia atrás, en la misma
        //  dirección e intensidad variable que las chispas) y EL ARCO
        //  DE CHOQUE (la medialuna morada y negra con la concha
        //  abombada MIRANDO al portador).
        // ================================================================""",
    "PreDraw: título")

# ------------------------------------------ (12) el doc de VestirUnImpio
reemplaza(
    """        /// <summary>EL VESTIDO DE UN IMPIO — la bruma del corazón (misma
        /// dirección e intensidad variable) y LA MEDIALUNA VERTICAL.</summary>""",
    """        /// <summary>EL VESTIDO DE UN IMPIO — la bruma de la reentrada
        /// (nace del punto de estancamiento, misma dirección e
        /// intensidad variable que las chispas) y EL ARCO DE CHOQUE (la
        /// medialuna con la concha abombada mirando al portador).</summary>""",
    "doc VestirUnImpio")

# ---------------------------------------------------- (13) LA BRUMA: bloque
reemplaza(
    """            // ==============================================================
            //  v6.107 — LA BRUMA DEL CORAZÓN — «en la misma direccion
            //  e intensidad variable como las particulas iniciales»:
            //  cada puff NACE en el corazón del impío y se derrama
            //  HACIA FUERA, SIEMPRE en la dirección OPUESTA al
            //  portador (la MISMA fuga de las chispas — el jugador a la
            //  derecha → la bruma exhala hacia la IZQUIERDA). INTENSIDAD
            //  VARIABLE: el alfa, el tamaño y el NÚMERO de puffs crecen
            //  con la cercanía del impío (cuanto más pegado arde, más
            //  bruma respira su corazón).
            // ==============================================================""",
    """            // ==============================================================
            //  v6.108 — LA BRUMA DE LA REENTRADA — «la bruma debe hacer
            //  lo mismo que las particulas»: cada puff NACE en el PUNTO
            //  DE ESTANCAMIENTO (el tope abombado de la medialuna) y se
            //  derrama hacia ATRÁS en TODAS las direcciones de fuga
            //  (el MISMO abanico ±90° de las chispas — SIEMPRE
            //  alejándose del portador). INTENSIDAD VARIABLE: el alfa,
            //  el tamaño y el NÚMERO de puffs crecen con la cercanía
            //  del impío (cuanto más hondo cae, más densa la estela).
            // ==============================================================""",
    "bruma: bloque v6.108")

# ------------------------------------------------------ (14) bruma: puffs
reemplaza(
    """                int puffs = 3 + (k > 0.6f ? 1 : 0);         // INTENSIDAD VARIABLE: más puffs pegado""",
    """                int puffs = 2 + (int)(k * 3f);              // LA DENSIDAD: 2 lejos → 5 pegado (más bruma cuanto más cerca)""",
    "bruma: puffs ∝ cercanía")

# --------------------------------------------------- (15) bruma: el rumbo
reemplaza(
    """                    // EL RUMBO DEL PUFF — el MISMO abanico de fuga de
                    // las chispas: SIEMPRE la dirección OPUESTA al
                    // portador (determinista por puff).
                    float abanico = (Frac(f0 * 13.7f) * 2f - 1f) * 0.85f;
                    Vector2 rumbo = dir.RotatedBy(abanico);""",
    """                    // EL RUMBO DEL PUFF — el MISMO abanico de fuga de
                    // las chispas: TODAS las direcciones que se ALEJAN
                    // del portador (el hemisferio trasero ±90°,
                    // determinista por puff).
                    float abanico = (Frac(f0 * 13.7f) * 2f - 1f) * MathHelper.PiOver2;
                    Vector2 rumbo = dir.RotatedBy(abanico);""",
    "bruma: abanico ±90°")

# ----------------------------------------- (16) bruma: nace del estancamiento
reemplaza(
    """                    // NACE EN EL CORAZÓN y se derrama HACIA FUERA por su
                    // rumbo (la MISMA dirección de las chispas).
                    float avance = edad * RadioDomo(n, k) * (1.15f + 0.55f * f0);
                    Vector2 pos = n.Center + rumbo * avance;""",
    """                    // NACE EN EL PUNTO DE ESTANCAMIENTO (el tope abombado
                    // de la medialuna) y se derrama HACIA ATRÁS por su
                    // rumbo (la MISMA estela de las chispas).
                    float avance = edad * RadioDomo(n, k) * (1.3f + 0.6f * f0);
                    Vector2 pos = n.Center - dir * (RadioDomo(n, k) * 0.95f) + rumbo * avance;""",
    "bruma: pos = estancamiento + rumbo")

# ------------------------------------------- (17) LA MEDIALUNA: el bloque
reemplaza(
    """            // ==============================================================
            //  v6.107 — LA MEDIALUNA VERTICAL (LA CÚPULA) — «intenta
            //  darle una rotacion vertical de 90 grados, o sea darle la
            //  vuelta»: la v6.106 ACOSTABA el arco en HORIZONTAL (la
            //  parte plana mirando al portador, el domo abombándose en
            //  dirección contraria) — LA CURA: LA ROTACIÓN VERTICAL DE
            //  90° PONE LA CÚPULA DE PIE — la parte PLANA (la disuelta)
            //  cae ABAJO, sobre el corazón del impío, y la concha se
            //  YERGUE cubriéndolo desde arriba (LA CÚPULA DEL JUICIO
            //  que pesa sobre su cabeza — la medialuna YA NO gira con el
            //  portador: solo el bamboleo lento la mece).
            // ==============================================================""",
    """            // ==============================================================
            //  v6.108 — LA MEDIALUNA = EL ARCO DE CHOQUE — «la parte
            //  abombada de la medialuna tiene que mirarme a mi en todo
            //  momento… la medialuna cambia de color volviendose
            //  ligeramente morada negra»: EL CALENTAMIENTO
            //  AERODINÁMICO — el portador es el PLANETA, su aura la
            //  ATMÓSFERA y el impío el objeto que CAE EN REENTRADA: el
            //  arco de choque lo envuelve POR DELANTE con la CONCHA
            //  ABOMBADA APUNTANDO SIEMPRE AL PORTADOR (en todo momento
            //  — si lo rodeas, el arco gira contigo), la parte plana
            //  disuelta abraza su corazón por detrás… y cuanto más
            //  HONDO cae (más cerca del portador), más MORADA NEGRA se
            //  vuelve (el choque se enfría en la sombra del vacío).
            // ==============================================================""",
    "medialuna: bloque v6.108")

# ------------------------------------------- (18) medialuna: LA ROTACIÓN
reemplaza(
    """                // LA ROTACIÓN VERTICAL — el domo DE PIE: la parte plana
                // disuelta cae sobre el corazón y la concha se corona
                // ARRIBA… y EL BAMBOLEO lento (la cúpula respira sobre
                // la cabeza del impío).
                float rot = 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);""",
    """                // LA ROTACIÓN DEL CHOQUE — LA CONCHA ME MIRA: el impío
                // «cae» hacia el portador (el planeta) y el arco de
                // choque lo envuelve POR DELANTE — la parte ABOMBADA
                // apunta SIEMPRE al portador (EN TODO MOMENTO: si lo
                // rodeas, el arco gira contigo)… y EL BAMBOLEO lento
                // (la concha respira en el viento de la reentrada).
                float rot = dir.ToRotation() - MathHelper.PiOver2
                    + 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);""",
    "medialuna: rot = dir − π/2 (la concha mira al portador)")

# ------------------------------------------------ (19) medialuna: LA BASE
reemplaza(
    """                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): la
                // PARTE PLANA DISUELTA cae SOBRE el corazón del impío y
                // la concha lo CORONA desde arriba.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;""",
    """                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): el
                // CENTRO del semicírculo cae SOBRE el corazón del impío
                // (la parte plana disuelta lo abraza por detrás) y la
                // CONCHA SE ABOMBA hacia el portador hasta ~RadioDomo
                // de su pecho — el choque se planta DELANTE del cuerpo.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;""",
    "medialuna: base (el centro del semicírculo en el corazón)")

# -------------------------------------------------- (20) medialuna: COLOR
reemplaza(
    """                Color negroVioleta = new(26, 12, 44);
                Color violeta = new(150, 84, 235);

                // (1) EL PASO ALFA — el negro-violeta que OSCURECE: la
                //     medialuna se lee TAMBIÉN DE DÍA (la lección de la
                //     .103: el aditivo se lava con el sol).
                VFXCore.Begin();
                VFXCore.Quad(pos, negroVioleta * ((0.40f + 0.26f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAlpha();

                // (2) EL PASO ADITIVO — la energía violeta que SUMA: las
                //     vetas del Perlin prenden en la oscuridad…
                VFXCore.Begin();
                VFXCore.Quad(pos, violeta * ((0.26f + 0.32f * k) * vive),
                    tamPx, rot, perlin);
                //     …Y SU ECO — una segunda medialuna más GRANDE y
                //     TENUE (la aura que gotea de la cúpula hacia afuera).
                VFXCore.Quad(pos, violeta * ((0.12f + 0.16f * k) * vive),
                    tamEco, rot, perlin);
                VFXCore.FlushAdditive();""",
    """                // EL COLOR DEL CHOQUE — v6.108: «la medialuna cambia de
                // color volviendose ligeramente morada negra»: cuanto
                // más HONDO cae el impío en la atmósfera del portador,
                // el arco SE ENFRÍA EN SOMBRA — el brillo violeta se
                // apaga hacia el MORADO NEGRO del vacío (de lejos es la
                // medialuna violeta de siempre; pegado, una concha
                // morada-negra).
                Color negroVioleta = new(26, 12, 44);
                Color violeta = new(150, 84, 235);
                Color moradaNegra = new(46, 20, 78);
                Color colorAlfa = Color.Lerp(negroVioleta, moradaNegra, k);
                Color colorLuz = Color.Lerp(violeta, moradaNegra, 0.85f * k);

                // (1) EL PASO ALFA — la sombra que OSCURECE (y se ahonda
                //     con la cercanía): la medialuna se lee TAMBIÉN DE
                //     DÍA (la lección de la .103: el aditivo se lava con
                //     el sol).
                VFXCore.Begin();
                VFXCore.Quad(pos, colorAlfa * ((0.42f + 0.30f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAlpha();

                // (2) EL PASO ADITIVO — la energía que SUMA se APAGA
                //     hacia el morado negro cuando el impío cae hondo
                //     (las vetas del Perlin se enfrían en sombra)…
                VFXCore.Begin();
                VFXCore.Quad(pos, colorLuz * ((0.26f + 0.26f * k) * vive),
                    tamPx, rot, perlin);
                //     …Y SU ECO — una segunda medialuna más GRANDE y
                //     TENUE (la aura que gotea del choque hacia afuera).
                VFXCore.Quad(pos, colorLuz * ((0.12f + 0.14f * k) * vive),
                    tamEco, rot, perlin);
                VFXCore.FlushAdditive();""",
    "medialuna: color morada-negra ∝ cercanía")

# ------------------------------------------------ (21) el doc de RadioDomo
reemplaza(
    """        /// <summary>v6.106 — EL RADIO DEL SEMICÍRCULO — LA MISMA CÚPULA
        /// para TODO el teatro: la medialuna dibujada, las chispas que la
        /// recorren y la bruma que la envuelve. Crece con la cercanía del
        /// impío (el fuego pegado lo ENVUELVE) y viste su cuerpo ENTERO.</summary>""",
    """        /// <summary>v6.106 → v6.108 — EL RADIO DEL ARCO DE CHOQUE — LA
        /// MISMA cúpula para TODO el teatro: la medialuna dibujada, el
        /// punto de estancamiento donde nace el plasma (chispas y
        /// bruma) y la estela que dejan. Crece con la cercanía del
        /// impío (el fuego pegado lo ENVUELVE) y viste su cuerpo ENTERO.</summary>""",
    "doc RadioDomo")

# ------------------------------------------------------------------ guardar
if s == orig:
    print("NADA CAMBIÓ — revisar los reemplazos")
    sys.exit(1)
io.open(RUTA, "w", encoding="utf-8", newline="").write(s)
print(f"\nGUARDADO: {RUTA} ({len(orig)} → {len(s)} bytes, {s.count(chr(10))} líneas)")
