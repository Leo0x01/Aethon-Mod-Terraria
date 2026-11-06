#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.107 — LA CIRUGÍA DEL ANCLA (parte 2): chispas del corazón, bruma y cúpula."""
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

# ============================================================ (A) EL GENERADOR
reemplaza(
    """                // ----------------------------------------------------------
                //  v6.106 — LAS CHISPAS DEL ARCO: «las particulas deben
                //  moverse en la direccion opuesto al jugador, o sea el
                //  jugador esta a la derecha del enemigo y las
                //  particulas del enemigo se mueven a la izquierda, asi
                //  para todas las direcciones, ademas cada particula se
                //  debe liberar y deben tener el mismo recorrido que el
                //  semcirculo, la velocidad de las particulas debe ir
                //  cada vez mas rapido a medida que se acercan al
                //  jugador».
                //
                //  CADA CHISPA NACE LIBERADA con su arco propio — EL
                //  MISMO RECORRIDO del semicírculo Perlin: parte del
                //  cuerno de arranque ALEJÁNDOSE del portador (jugador a
                //  la derecha → nace por el costado y corre a la
                //  IZQUIERDA), envuelve la cúpula por la espalda del
                //  impío y llega al cuerno de llegada ACELERANDO (el
                //  tramo que VUELVE hacia el jugador es el MÁS RÁPIDO —
                //  allí se disuelve). Cuanto más pegado arde el impío,
                //  más rápida nace y más denso hierve el arco.
                // ----------------------------------------------------------
                int ritmo = (int)MathHelper.Clamp(4f - k * 2f, 1f, 4f);  // 1 de cada 4 lejos… de cada 2 pegado
                if ((Projectile.ai[0] + i) % ritmo == 0f && n.Center.DistanceSQ(dueno.Center) < 640f * 640f)
                {
                    // LA GEOMETRÍA DEL SEMICÍRCULO — el mismo arco que
                    // dibuja la medialuna: el cuerno de arranque cae al
                    // costado del impío y el barrido (ángulo CRECIENTE)
                    // sale ALEJÁNDOSE del portador, pasa por la espalda
                    // (phi+PI) y vuelve al cuerno de llegada.
                    float phi = dir.ToRotation();               // portador → impío
                    float radioDomo = RadioDomo(n, k);          // LA MISMA cúpula de la medialuna
                    float a0 = phi - MathHelper.PiOver2;        // el cuerno de ARRANQUE
                    float a1 = phi + MathHelper.PiOver2;        // el cuerno de LLEGADA

                    int rafaga = k > 0.55f && Main.rand.NextFloat() < 0.5f ? 2 : 1;
                    for (int s = 0; s < rafaga; s++)
                    {
                        // escalonadas por el arranque del arco (la
                        // corriente parte del costado y envuelve)
                        float ang = a0 + Main.rand.NextFloat(0f, 0.20f);
                        float radio = radioDomo * Main.rand.NextFloat(0.68f, 0.95f);

                        // LA VELOCIDAD — arranca suave y ACELERA SIN
                        // FRENO (cada vez más rápido hacia el cuerno de
                        // llegada); la furia escala con la cercanía.
                        float vBase = (0.045f + 0.038f * k) * Main.rand.NextFloat(0.85f, 1.2f);
                        float acel = vBase * (0.055f + 0.05f * k);

                        // LIBERADA — el centro queda congelado AL NACER:
                        // aunque el impío camine, la chispa termina SU
                        // semicírculo sola (PolvoArcoJuicio manda).
                        var arco = new PolvoArcoJuicio.Arco
                        {
                            Centro = n.Center,
                            Ang0 = a0,
                            Ang = ang,
                            AngFin = a1,
                            VelAng = vBase,
                            Acel = acel,
                            Radio = radio
                        };

                        // EL COLOR DE LA CASA — el halo violeta: más
                        // CLARO cuanto más cerca arde (la temperatura).
                        Color halo = Color.Lerp(
                            new Color(150, 84, 235), new Color(224, 176, 255), k);
                        Vector2 posChispa = n.Center
                            + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        Dust d = Dust.NewDustPerfect(posChispa,
                            ModContent.DustType<PolvoArcoJuicio>(), Vector2.Zero,
                            0, halo, Main.rand.NextFloat(0.24f, 0.38f));   // MENUDAS (la letra)
                        d.customData = arco;
                        d.noGravity = true;
                        d.noLight = false;
                    }
                }""",
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
                // ----------------------------------------------------------
                int ritmo = (int)MathHelper.Clamp(4f - k * 2f, 1f, 4f);  // 1 de cada 4 lejos… de cada 2 pegado
                if ((Projectile.ai[0] + i) % ritmo == 0f && n.Center.DistanceSQ(dueno.Center) < 640f * 640f)
                {
                    int rafaga = k > 0.55f && Main.rand.NextFloat() < 0.5f ? 2 : 1;
                    for (int s = 0; s < rafaga; s++)
                    {
                        // EL RUMBO — SIEMPRE OPUESTO AL PORTADOR: el
                        // abanico alrededor de la dirección portador →
                        // impío (TODAS las chispas comparten la MISMA
                        // fuga: la que se aleja del jugador).
                        float abanico = Main.rand.NextFloat(-0.85f, 0.85f);
                        Vector2 rumbo = dir.RotatedBy(abanico);

                        // LA CUNA — nace del CORAZÓN del impío (un
                        // temblor mínimo para que no nazcan todas del
                        // mismo pixel).
                        Vector2 cuna = n.Center + new Vector2(
                            Main.rand.NextFloat(-1f, 1f) * n.width * 0.08f,
                            Main.rand.NextFloat(-1f, 1f) * n.height * 0.08f);

                        // LA VELOCIDAD — la furia del vacío escala con la
                        // cercanía, y ACELERA sin freno durante el vuelo
                        // (la rabia crece al escapar).
                        float vBase = (0.9f + 1.1f * k) * Main.rand.NextFloat(0.85f, 1.2f);
                        float acel = 0.020f + 0.018f * k;

                        // LIBERADA — la cuna y el rumbo quedan congelados
                        // AL NACER: aunque el impío camine, la chispa
                        // termina SU vuelo sola (PolvoExpulsionJuicio
                        // manda). El camino: del corazón hacia afuera,
                        // hasta ~2 cúpulas de distancia.
                        float distMax = RadioDomo(n, k) * Main.rand.NextFloat(1.35f, 2.1f);
                        var chispa = new PolvoExpulsionJuicio.Chispa
                        {
                            Centro = cuna,
                            Dir = rumbo,
                            Vel = vBase,
                            Acel = acel,
                            DistMax = distMax
                        };

                        // EL COLOR DE LA CASA — el halo violeta: más
                        // CLARO cuanto más cerca arde (la temperatura).
                        Color halo = Color.Lerp(
                            new Color(150, 84, 235), new Color(224, 176, 255), k);
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<PolvoExpulsionJuicio>(), Vector2.Zero,
                            0, halo, Main.rand.NextFloat(0.24f, 0.38f));   // MENUDAS (la letra)
                        d.customData = chispa;
                        d.noGravity = true;
                        d.noLight = false;
                    }
                }""",
    "generador de chispas")

# ================================================================= (B) LA BRUMA
reemplaza(
    """            // ==============================================================
            //  v6.106 — LA BRUMA RECORRE EL MISMO ARCO — «en la misma
            //  direccion e intensidad variable como las particulas
            //  iniciales»: cada puff NACE en el cuerno de arranque y
            //  ACELERA por el semicírculo (prog^2.3: nace lenta, cada
            //  vez más rápida — la misma aceleración de las chispas),
            //  envolviendo la cúpula ALEJÁNDOSE del portador y volviendo
            //  por su espalda hasta el cuerno de llegada. INTENSIDAD
            //  VARIABLE: el alfa, el tamaño y el NÚMERO de puffs crecen
            //  con la cercanía del impío (cuanto más pegado arde, más
            //  bruma respira el arco).
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                float phi = dir.ToRotation();
                float radioDomo = RadioDomo(n, k);
                float a0 = phi - MathHelper.PiOver2;        // EL MISMO cuerno de arranque de las chispas
                int puffs = 3 + (k > 0.6f ? 1 : 0);         // INTENSIDAD VARIABLE: más puffs pegado
                for (int p = 0; p < puffs; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.085f + 0.075f * Frac(f0 * 9.7f);
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // EL MISMO RECORRIDO DEL SEMICÍRCULO — la posición es
                    // la del ARCO (el mismo que las chispas y la medialuna)
                    // y el progreso ACELERA (empieza lento, termina volando).
                    float prog = MathF.Pow(edad, 2.3f);
                    float ang = a0 + prog * MathHelper.Pi;
                    Vector2 pos = n.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang))
                        * (radioDomo * (1.00f + 0.24f * f0));

                    float radio = (6f + 9f * f0) * (0.7f + 0.8f * edad)
                        * (0.8f + 0.5f * k);                 // …y del TAMAÑO de su cuerpo
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.16f + 0.34f * k);               // INTENSIDAD VARIABLE (×k)
                    Color c = edad > 0.55f
                        ? Color.Lerp(new Color(30, 16, 50), new Color(96, 54, 150),
                            (edad - 0.55f) / 0.45f)
                        : new Color(30, 16, 50);
                    // LA TANGENTE del barrido — el humo corre por donde
                    // corre el arco (la MISMA dirección de las chispas).
                    Vector2 tangente = new Vector2(-MathF.Sin(ang), MathF.Cos(ang));
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.55f), quality: 0.5f,
                        velocity: tangente * (14f + 18f * f0));
                }
            }""",
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
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                int puffs = 3 + (k > 0.6f ? 1 : 0);         // INTENSIDAD VARIABLE: más puffs pegado
                for (int p = 0; p < puffs; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.085f + 0.075f * Frac(f0 * 9.7f);
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // EL RUMBO DEL PUFF — el MISMO abanico de fuga de
                    // las chispas: SIEMPRE la dirección OPUESTA al
                    // portador (determinista por puff).
                    float abanico = (Frac(f0 * 13.7f) * 2f - 1f) * 0.85f;
                    Vector2 rumbo = dir.RotatedBy(abanico);

                    // NACE EN EL CORAZÓN y se derrama HACIA FUERA por su
                    // rumbo (la MISMA dirección de las chispas).
                    float avance = edad * RadioDomo(n, k) * (1.15f + 0.55f * f0);
                    Vector2 pos = n.Center + rumbo * avance;

                    float radio = (6f + 9f * f0) * (0.7f + 0.8f * edad)
                        * (0.8f + 0.5f * k);                 // …y del TAMAÑO de su cuerpo
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.16f + 0.34f * k);               // INTENSIDAD VARIABLE (×k)
                    Color c = edad > 0.55f
                        ? Color.Lerp(new Color(30, 16, 50), new Color(96, 54, 150),
                            (edad - 0.55f) / 0.45f)
                        : new Color(30, 16, 50);
                    // LA DERIVA — el humo se derrama por donde vuelan
                    // las chispas (la MISMA dirección de la fuga).
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.55f), quality: 0.5f,
                        velocity: rumbo * (10f + 16f * f0));
                }
            }""",
    "bruma del corazón")

# ============================================================= (C) LA MEDIALUNA
reemplaza(
    """            // ==============================================================
            //  v6.106 — LA MEDIALUNA PERLIN — «el semicirculo esta al
            //  revez […] el semicirculo debe tener la parte recta mas
            //  difuminada algo asi como una cupula o medialuna». LA
            //  CURA: la textura REFORJADA disuelve la parte recta HACIA
            //  ADENTRO (isopletas circulares DENTRO de la cúpula — el
            //  rabo de la v6.105 que goteaba bajo el diámetro hacia el
            //  jugador MURIÓ): solo vive la CONCHA del arco, una
            //  MEDIALUNA de Perlin morada y negra. Y LA INCLINACIÓN:
            //  la parte PLANA (la disuelta) mira AL PORTADOR y el arco
            //  se abomba EN DIRECCIÓN OPUESTA a él — el jugador a la
            //  derecha → la medialuna abraza la espalda del impío
            //  abriéndose a la IZQUIERDA, y las chispas y la bruma
            //  RECORREN ese mismo arco alejándose del jugador.
            // ==============================================================
            Texture2D perlin = PerlinSemi;
            if (perlin != null && !perlin.IsDisposed)
            {
                float late = 0.94f + 0.06f * MathF.Sin(t * 7f + semilla * 1.7f);

                // EL TAMAÑO — EL RADIO DEL ARCO QUE CHISPAS Y BRUMA
                // RECORREN (TODO el teatro comparte LA MISMA cúpula:
                // RadioDomo) y crece con la cercanía (el fuego pegado
                // lo ENVUELVE), con la respiración de la casa.
                float ancho = RadioDomo(n, k) * 2f * late;
                float esc = ancho / 192f;                    // el semicírculo mide 2R=192 en la textura

                // LA INCLINACIÓN — la parte plana disuelta AL PORTADOR,
                // el arco ALEJÁNDOSE de él… y EL BAMBOLEO lento (la
                // medialuna respira sobre la espalda del impío).
                float rot = dir.ToRotation() + MathHelper.PiOver2
                    + 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);

                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): la
                // PARTE PLANA DISUELTA cae SOBRE el corazón del impío y
                // la concha abraza su espalda.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;""",
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
            // ==============================================================
            Texture2D perlin = PerlinSemi;
            if (perlin != null && !perlin.IsDisposed)
            {
                float late = 0.94f + 0.06f * MathF.Sin(t * 7f + semilla * 1.7f);

                // EL TAMAÑO — EL RADIO DE LA CÚPULA (TODO el teatro
                // comparte la MISMA: RadioDomo) y crece con la cercanía
                // (el fuego pegado lo ENVUELVE), con la respiración de
                // la casa.
                float ancho = RadioDomo(n, k) * 2f * late;
                float esc = ancho / 192f;                    // el semicírculo mide 2R=192 en la textura

                // LA ROTACIÓN VERTICAL — el domo DE PIE: la parte plana
                // disuelta cae sobre el corazón y la concha se corona
                // ARRIBA… y EL BAMBOLEO lento (la cúpula respira sobre
                // la cabeza del impío).
                float rot = 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);

                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): la
                // PARTE PLANA DISUELTA cae SOBRE el corazón del impío y
                // la concha lo CORONA desde arriba.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;""",
    "medialuna vertical")

io.open(RUTA, "w", encoding="utf-8", newline="\n").write(s)
print(f"\nESCRITO: {len(orig)} → {len(s)} chars")
