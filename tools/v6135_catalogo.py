#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.135 — LA CIRUGÍA DEL CATÁLOGO (AlasEstilo.cs):
  (1) el struct: campo Coronas (el TERCER PAR del arcángel)
  (2) la firma Ala(): parámetro coronas
  (3) el catálogo Crear(): [6..11] LAS SEIS MITOLÓGICAS
  (4) el Vacío [2]: alitas 0.55 → coronas 0.72 (EL ARCÁNGEL DE 6)
  (5) las constantes del catálogo (Serafin..Dragon)
"""
P = 'Content/VFX/AlasEstilo.cs'
s = open(P, encoding='utf-8').read()

# ---------- (1) el struct ----------
viejo = '''        public bool Lisas;'''
nuevo = '''        public bool Lisas;

        /// <summary>v6.135 — LA CORONA (el TERCER PAR del arcángel):
        /// la escala del par superior (0 = sin corona). Como Alitas,
        /// pero el abanico nace SOBRE la espalda, alzado y abierto —
        /// las alas del arcángel que no se pliegan jamás.</summary>
        public float Coronas;'''
assert viejo in s and 'public float Coronas' not in s
s = s.replace(viejo, nuevo, 1)

# ---------- (2) la firma ----------
viejo = '''float escalaGlobal = 1f, float alitas = 0f)'''
nuevo = '''float escalaGlobal = 1f, float alitas = 0f, float coronas = 0f)'''
assert viejo in s
s = s.replace(viejo, nuevo, 1)

# ---------- (4) el Vacío a 6 alas ----------
viejo = '''lisas: true, escalaGlobal: 0.9f, alitas: 0.55f),'''
nuevo = '''lisas: true, escalaGlobal: 0.9f, alitas: 0.55f, coronas: 0.72f),'''
assert viejo in s
s = s.replace(viejo, nuevo, 1)

# ---------- (3) las entradas [6..11] ----------
viejo = '''Ala("Corolas del Alba", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.BlancoCalido,
                    DefinitivasLib.Oro,
                    new Color(255, 226, 160)
                }, new Color(255, 250, 240), "Ruido_6", 7, 86f, 54f, 3.5f, 16f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.14f, 9f, 0.4f, 0.5f, 0f, 0.085f, 0.16f, 0.018f, 0.7f, 0.6f, 0.9f, 0.3f, 0.34f, 0.62f, 0.17f, 1f, 1f, 1f, 1.1f, 0)
            };
        }'''
nuevo = '''Ala("Corolas del Alba", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.BlancoCalido,
                    DefinitivasLib.Oro,
                    new Color(255, 226, 160)
                }, new Color(255, 250, 240), "Ruido_6", 7, 86f, 54f, 3.5f, 16f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.14f, 9f, 0.4f, 0.5f, 0f, 0.085f, 0.16f, 0.018f, 0.7f, 0.6f, 0.9f, 0.3f, 0.34f, 0.62f, 0.17f, 1f, 1f, 1f, 1.1f, 0),
                // ==============================================================
                //  v6.135 — LAS SEIS MITOLÓGICAS (la letra: «teniendo en
                //  cuenta las alas de arcangeles superiores que tiene 3
                //  pares de alas o sea 6 alas y hacer otras versiones de
                //  las alas runicas del vacio basandote en otras
                //  mitologias o creaciones y tipos de alas»). EL NUEVO
                //  EJE: el campo Coronas — el TERCER PAR que nace SOBRE
                //  la espalda (alzado y abierto, escala clamped 0.25-0.8
                //  como las alitas). TODAS nacen del Vacío v6.134 (la
                //  geometría exacta del Eclipse que SÍ gustó + lisas +
                //  membrana + redonda): cada una solo cambia PALETA ·
                //  RUNAS · RUIDO · PLUMAS(nº) · CURVA · ESCALA ·
                //  ALITAS/CORONAS. El ALMA DE CADA MITO:
                //    [6] SERAFÍN — la hebrea: TRES PARES (coronas 0.78,
                //        alitas 0.55), la paleta del fuego violeta que
                //        NO quema (vivo 235,120,255 · hondo 158,66,214 ·
                //        ceniza 44,16,64), curva 0.34 (la llama se
                //        encorva), runas 2 (el alfabeto del vacío: el
                //        trono arde en glifos violetas).
                //    [7] VALQUIRIA — la nórdica: plumas 7 (el abanico
                //        de guerra MÁS DENSO), redonda 0 (las plumas
                //        filosas de lanza), runas 1 (el futhark solar de
                //        la casa), paleta del cielo de batalla (plata
                //        218,232,255 · acero 130,150,190 · noche
                //        22,28,48), curva 0 y filosa 0.3 (las alas de
                //        lanza rectas), sin corona (2 pares: el par de
                //        las valquirias no es serafín).
                //    [8] QUETZAL — la azteca-serpiente: plumas 7, curva
                //        0.42 (la serpiente ONDEA — la mayor de todas),
                //        barrido 0.22, paleta jade-pluma (turquesa
                //        94,222,198 · jade 46,168,146 · selva 10,44,40),
                //        runas 2, alitas 0.5 y coronas 0.66 (la serpiente
                //        emplumada trepa en tres niveles).
                //    [9] FÉNIX — la del renacimiento: paleta de llama
                //        viva (núcleo blanco 255,244,214 · fuego
                //        255,150,64 · brasa 150,40,20), curva 0.3,
                //        hueLargo 0.22 (el tono ARDE a lo largo), runas
                //        1, alitas 0.55 + coronas 0.7 (el fénix de tres
                //        llamas), flap 1.15 (aletea más vivo).
                //    [10] TENGU — la del cuervo: plumas 7, filosa 0.55
                //        (las plumas PRIMARIAS del cuervo — puntas
                //        separadas y negras), redonda 0.15, paleta
                //        cuervo-tinta (azul-negro 92,110,158 · tinta
                //        38,46,76 · negro 8,10,20), runas 3 (el alfabeto
                //        ámbar del abismo), curva 0.2, sin corona.
                //    [11] DRAGÓN — la de la membrana: la única SIN
                //        plumas del catálogo (Plumas=2: el ala de
                //        membrana de dos dedos), raíz 4.5 y punta 24
                //        (la vela gorda del murciélago), vientre 0.62,
                //        paleta escamas (rojo-vino 200,64,72 · escama
                //        108,28,40 · cueva 20,6,10), runas 3, curva
                //        0.24, coronas 0.62 + alitas 0.5 (el dragón de
                //        seis velas).
                // ==============================================================
                Ala("Serafín del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(235, 120, 255),
                    new Color(158, 66, 214),
                    new Color(44, 16, 64)
                }, new Color(120, 70, 170), "Ruido_9", 5, 90f, 52f, 3.5f, 19f, 12f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.34f, 5f, 0.45f, 0.55f, 0.05f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 2, 1.05f, 1f, 1f, lisas: true, escalaGlobal: 0.9f, alitas: 0.55f, coronas: 0.78f),
                Ala("Valquiria Rúnica", (Color[])(object)new Color[3]
                {
                    new Color(218, 232, 255),
                    new Color(130, 150, 190),
                    new Color(22, 28, 48)
                }, new Color(150, 170, 210), "Ruido_13", 7, 92f, 54f, 3.5f, 17f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0f, 5f, 0.45f, 0.5f, 0.3f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 1, 1f),
                Ala("Quetzal Emplumado", (Color[])(object)new Color[3]
                {
                    new Color(94, 222, 198),
                    new Color(46, 168, 146),
                    new Color(10, 44, 40)
                }, new Color(70, 160, 150), "Ruido_6", 7, 92f, 56f, 3.5f, 18f, 12f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.22f, 0.42f, 5f, 0.45f, 0.5f, 0.05f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 2, 1.05f, 1f, 1f, lisas: true, escalaGlobal: 0.92f, alitas: 0.5f, coronas: 0.66f),
                Ala("Fénix del Renacer", (Color[])(object)new Color[3]
                {
                    new Color(255, 244, 214),
                    new Color(255, 150, 64),
                    new Color(150, 40, 20)
                }, new Color(230, 140, 90), "Ruido_14", 5, 90f, 52f, 3.5f, 19f, 12f, 0.045f, 0.9f, 0.22f, 1.54f, 0.12f, 0.14f, 0.3f, 5f, 0.45f, 0.5f, 0.05f, 0.085f, 0.22f, 0.018f, 1.15f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 1, 1.05f, 1f, 1f, lisas: true, escalaGlobal: 0.9f, alitas: 0.55f, coronas: 0.7f),
                Ala("Tengu del Crepúsculo", (Color[])(object)new Color[3]
                {
                    new Color(92, 110, 158),
                    new Color(38, 46, 76),
                    new Color(8, 10, 20)
                }, new Color(70, 84, 120), "Ruido_6", 7, 90f, 52f, 3.5f, 17f, 11f, 0.5f, 0.045f, 0.22f, 1.54f, 0.12f, 0.14f, 0.2f, 5f, 0.45f, 0.5f, 0.55f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.8f, 0.3f, 0.36f, 2f, 0.15f, 1f, 1f, 1f, 1.05f, 3, 1.05f),
                Ala("Dragón del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(200, 64, 72),
                    new Color(108, 28, 40),
                    new Color(20, 6, 10)
                }, new Color(150, 60, 70), "Ruido_6", 2, 96f, 60f, 4.5f, 24f, 16f, 0.5f, 0.92f, 0.22f, 1.6f, 0.12f, 0.14f, 0.18f, 5f, 0.62f, 0.5f, 0.05f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.9f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 3, 1.05f, 1f, 1f, lisas: true, escalaGlobal: 0.95f, alitas: 0.5f, coronas: 0.62f)
            };
        }'''
assert viejo in s
s = s.replace(viejo, nuevo, 1)

# ---------- (5) las constantes ----------
viejo = '''        public const byte Corolas = 5;'''
nuevo = '''        public const byte Corolas = 5;
        public const byte Serafin = 6;
        public const byte Valquiria = 7;
        public const byte Quetzal = 8;
        public const byte Fenix = 9;
        public const byte Tengu = 10;
        public const byte Dragon = 11;'''
assert viejo in s
s = s.replace(viejo, nuevo, 1)

open(P, 'w', encoding='utf-8').write(s)
print("AlasEstilo.cs: campo Coronas + 6 mitológicas + Vacío a 6 alas")
