#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.135 — EL TERCER PAR (AlasAuroraLib.cs): LA CORONA DEL ARCÁNGEL.

El bloque de las alitas (v6.132) dibuja el 2º par con pozos propios y
lo vuelca. La CORONA replica el patrón: el 3º par nace SOBRE la
espalda (−16·escalaG px), MÁS ABIERTO (spread ·0.95) y ALZADO (inclinación
−0.18: el abanico mira hacia arriba). Se construye y EMITE ANTES de las
alitas: par de atrás hacia adelante = coronas → alitas → principal.
Reusa los POZOS de las alitas SEGUROS porque la geometría muerta no se
relee (cada par vuelca antes de que el siguiente pise el pozo).
"""
P = 'Content/VFX/AlasAuroraLib.cs'
s = open(P, encoding='utf-8').read()

# ---------- (1) el bloque de la CORONA, insertado ANTES del de alitas ----------
viejo = '''                    // ============================================================
                    //  v6.132 — EL PAR DE ALITAS (la letra: «le falta algo
                    //  abajo, como otro pequeño par de alitas ya sabes como
                    //  una mariposa»): LA SEGUNDA VELA — el abanico inferior'''
nuevo = '''                    // ============================================================
                    //  v6.135 — LA CORONA DEL ARCÁNGEL (la letra: «teniendo
                    //  en cuenta las alas de arcangeles superiores que
                    //  tiene 3 pares de alas o sea 6 alas»): EL TERCER PAR
                    //  — el abanico superior nacido SOBRE la espalda (16·esc
                    //  px más arriba), al 55-78 % del tamaño y ALZADO
                    //  (inclinación −0.18: el abanico abre hacia ARRIBA,
                    //  la pose del arcángel que no pliega las alas ni para
                    //  rezar). Se construye y EMITE ANTES de las alitas:
                    //  el orden del fondo hacia el frente es CORONA →
                    //  ALITAS → PRINCIPAL (el par superior queda DETRÁS de
                    //  todo — el arcángel superpone su par mayor encima).
                    //  Reusa los POZOS de las alitas (SEGUROS: cada par
                    //  vuelca sus triángulos antes de que el siguiente
                    //  escriba el pozo — la geometría muerta no se relee).
                    // ============================================================
                    if (estilo.Coronas > 0f && _geoN > 0)
                    {
                        float escC = MathHelper.Clamp(estilo.Coronas, 0.25f, 0.8f);
                        int nC = num2;                       // las mismas plumas por lado
                        int idxC = 0;
                        for (int ladoC = -1; ladoC <= 1; ladoC += 2)
                        {
                            // EL ANCLA SUPERIOR: 16·escalaG px sobre la
                            // espalda (los hombros — el ala alta del
                            // arcángel nace de la CLAVÍCULA, no del pecho).
                            Vector2 anclaC = val2 + new Vector2((float)ladoC * estilo.AnclajeSep * escC * 0.7f,
                                -16f * (estilo.EscalaGlobal > 0f ? estilo.EscalaGlobal : 1f) * gravDir);
                            if (fullRotation != 0f)
                            {
                                anclaC = val + Utils.RotatedBy(anclaC - val, (double)fullRotation, default(Vector2));
                            }
                            for (int jC = 0; jC < nC; jC++)
                            {
                                // EL ABANICO SUPERIOR: ConstruirPluma con la
                                // escala viva del par — el ALZADO vive en la
                                // inclinación local (−0.18: abre hacia
                                // arriba) y el spread más abierto (0.95).
                                EstiloAlas estC = estilo;
                                estC.EscalaGlobal = (estilo.EscalaGlobal > 0f ? estilo.EscalaGlobal : 1f) * escC;
                                estC.LargoInterior = estilo.LargoInterior;
                                estC.LargoExterior = estilo.LargoExterior;
                                estC.Inclinacion = estilo.Inclinacion - 0.18f;
                                plumas[idxC] = ConstruirPluma(anclaC, jC, Math.Min(spread * 0.95f, 1f), flap, gravDir, ladoC,
                                    globalTimeWrappedHourly, in estC, _pozosEjeAlitas[idxC], _pozosSemiAlitas[idxC],
                                    _pozosSemi2Alitas[idxC], fundidos[idxC], fullRotation);
                                hues[idxC] = hues[idxC % Math.Max(1, _geoN)];
                                idxC++;
                            }
                        }
                        // LA VELA DE LA CORONA — mismo lote aditivo.
                        int nCoronas = idxC;
                        try
                        {
                            if (((Entity)pl).direction < 0)
                            {
                                float cxC = val.X;
                                for (int mC = 0; mC < nCoronas; mC++)
                                {
                                    Vector2[] ejeC = _pozosEjeAlitas[mC];
                                    for (int kC = 0; kC < ejeC.Length; kC++)
                                        ejeC[kC].X = 2f * cxC - ejeC[kC].X;
                                    float[] smC = _pozosSemiAlitas[mC];
                                    float[] snC = _pozosSemi2Alitas[mC];
                                    for (int kC2 = 0; kC2 < smC.Length; kC2++)
                                    {
                                        float tmpC = smC[kC2];
                                        smC[kC2] = snC[kC2];
                                        snC[kC2] = tmpC;
                                    }
                                }
                            }
                            EstiloAlas estW = estilo;
                            estW.Lisas = false;          // la corona SIEMPRE es vela con membrana
                            int nvC = EmitirMembrana(0, plumas, hues, fundidos, mezclaDia, in estW, nCoronas);
                            if (nvC >= 3)
                            {
                                Volcar(graphicsDevice, _fxPlano, nvC, textura: false);
                            }
                            nvC = EmitirMembranaRuido(0, plumas, hues, mezclaDia, globalTimeWrappedHourly, fold, in estW, nCoronas);
                            if (nvC >= 3)
                            {
                                Volcar(graphicsDevice, _fxTextura, nvC, textura: true);
                            }
                        }
                        catch { }
                    }
                    // ============================================================
                    //  v6.132 — EL PAR DE ALITAS (la letra: «le falta algo
                    //  abajo, como otro pequeño par de alitas ya sabes como
                    //  una mariposa»): LA SEGUNDA VELA — el abanico inferior'''
assert viejo in s
s = s.replace(viejo, nuevo, 1)

open(P, 'w', encoding='utf-8').write(s)
print("AlasAuroraLib.cs: LA CORONA DEL ARCÁNGEL (el tercer par)")
