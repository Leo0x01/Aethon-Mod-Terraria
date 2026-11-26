#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Reemplaza el ACTO 2b de PrimeraCenaProjectile.cs con el CORO DE
SOMBRAS (v6.175): N sombras de la página según nivel+despertar."""
import io
import pathlib

F = pathlib.Path(__file__).resolve().parent.parent / "AethonMod" / "Content" / "Projectiles" / "Sombras" / "PrimeraCenaProjectile.cs"
lineas = io.open(F, encoding="utf-8").read().split("\n")

ini = fin = fin_bloque = None
for i, l in enumerate(lineas):
    if l.strip().startswith("if (t >= T_FESTIN - 20f && presencia > 0.02f && t >= T_LIBRO)"):
        ini = i
    if "LA CORRIENTE — la vida del preso baja a la boca del libro" in l:
        fin = i
        break
assert ini is not None and fin is not None, (ini, fin)
j = fin
while lineas[j].strip() != "}":
    j -= 1
fin_bloque = j

NUEVO = r'''                // ============================================================
                //  v6.175 — EL CORO DE SOMBRAS (la letra: «en ese momento
                //  cuenta cuantas sombras de la pagina puede usar, y las
                //  usa todas atrapando, paralizando al jefe»: N sombras
                //  según el nivel normal + el nivel de despertar del
                //  portador — FaucesGlobalNPC.ContarSombras (piso 1,
                //  tope 8). Cada una nace del libro, ORBITA al preso y
                //  lo CIÑE (la jaula de N brazos se aprieta con el
                //  festín); en EL TRAGO lo llevan a casa juntas. La
                //  sombra PRIMARIA (k=0) es la de siempre: su punta
                //  lleva la cobertura Devorador y la herida.
                // ============================================================
                int nSombras = FaucesGlobalNPC.ContarSombras(portador);
                Vector2 punta = centroJefe;      // la de la sombra primaria
                Vector2 rumbo = Vector2.UnitX;
                Vector2[] col = null;
                for (int k = 0; k < nSombras; k++)
                {
                    float retardo = k * 7f;   // cada sombra despierta a SU hora
                    if (t < T_FESTIN - 20f + retardo) continue;
                    float saleK = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 20f + retardo)) / 30f, 0f, 1f));
                    float alfaK = (k == 0 ? 1f : 0.82f) * vivoF;

                    // LA RAÍZ: el libro (todas brotan de sus páginas)
                    Vector2 haciaJefe = centroJefe - posLibro;
                    Vector2 rumboRaiz = haciaJefe.LengthSquared() > 4f
                        ? Vector2.Normalize(haciaJefe) : Vector2.UnitY;
                    Vector2 raiz = posLibro + rumboRaiz * 16f + (k > 0
                        ? new Vector2(MathF.Cos(k * 2.4f) * 12f, MathF.Sin(k * 1.7f) * 8f) : Vector2.Zero);

                    // EL DESTINO: cada sombra ORBITA al preso y SE CIÑE —
                    // la jaula de N brazos que se aprieta (radio 0.9→0.35)
                    float angOrbita = k * (MathF.PI * 2f / nSombras) + tiempo * (0.9f + 0.12f * k);
                    float radioOrbita = radioJefe * MathHelper.Lerp(0.90f, 0.35f, festin);
                    Vector2 destino = centroJefe + new Vector2(MathF.Cos(angOrbita), MathF.Sin(angOrbita) * 0.72f) * radioOrbita;
                    float alcance = saleK * 0.35f + festin * 0.65f;
                    destino = Vector2.Lerp(raiz + rumboRaiz * 42f, destino, alcance);
                    // EN EL TRAGO: todas cargan la masa a la boca del libro
                    destino = Vector2.Lerp(destino, raiz + rumboRaiz * 18f, inhala * 0.85f);

                    Vector2[] colK = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla + k * 13, 22, 0.20f, gancho: 0.45f);
                    // LA MASA: tinta lisa (la sombra renovada — v6.162)
                    float grosor = Math.Max(6f, 12f - nSombras * 0.8f);
                    SombrasLib.Masa(colK, grosor, grosor * 1.7f, 0.95f * alfaK, semilla + k * 13, tiempo, grosorCentro: 54f - k * 2f, lisa: true);

                    // LA BRUMA (una sola capa por sombra — el coro es MUCHOS)
                    SombrasLib.BrumaColumna(colK, 42f, 0.42f * alfaK, tiempo, semilla + k * 13, Math.Max(6, 12 - nSombras));

                    // LA CABEZA (la boca de cada brazo — muerde a SU tiempo)
                    Vector2 puntaK = colK[colK.Length - 1];
                    Vector2 rumboK = vivo
                        ? (centroJefe - puntaK).SafeNormalize(Vector2.UnitX)
                        : (raiz - puntaK).SafeNormalize(-Vector2.UnitY);
                    float muerdeK = vivo && t >= T_FESTIN + k * 4f ? CicloMasticar(t - k * 4f) : 0.4f;
                    float vigorK = MathHelper.Clamp(0.55f + 0.45f * muerdeK + 0.25f * inhala, 0f, 1f) * alfaK;
                    SombrasLib.CabezaDeBruma(puntaK, rumboK, vigorK, alfaK, tiempo, semilla + k * 13);

                    // LA PRIMARIA alimenta el resto del cine (herida, corriente)
                    if (k == 0) { punta = puntaK; rumbo = rumboK; col = colK; }
                }

                // EL CHARCO bajo el libro (la sombra que gotea de las páginas)
                SombrasLib.Charco(posLibro + new Vector2(0f, 30f), 52f * sale, 0.7f * vivoF, tiempo, semilla);

                // === EL DEVORADOR v2 — LA CUBIERTA TOTAL (la elipse REAL
                //     del preso) que crece con el festín y SE INHALA entera
                //     hacia la punta durante el trago ===
                if (vivo)
                {
                    float intensidad = MathHelper.Clamp(festin * 0.5f + MathHelper.Clamp((t - T_FESTIN - 85f) / 85f, 0f, 1f) * 0.5f, 0f, 1f);
                    SombrasLib.Devorador(punta, jefe, intensidad, vivoF, tiempo, semilla, col, trago: inhala);

                    // LA HERIDA: donde la punta ENTERRÓ brilla rojo (digestión)
                    if (col != null && col.Length > 0)
                    {
                        VFXCore.Begin();
                        VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.42f + 0.20f * inhala) * vivoF),
                            new Vector2(110f, 110f), rumbo.ToRotation());
                        VFXCore.FlushAdditive();
                    }
                }

                // === LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO
                //     (la digestión corre hacia el libro) ===
                if (col != null && (inhala > 0.05f || !vivo))
                {
                    float flujo = MathHelper.Max(inhala, vivo ? 0f : 1f - poso);
                    for (int k = 0; k < 5; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 0.8f + k * 0.2f);
                        float idxF = (1f - prog) * (col.Length - 1);
                        int idx = Math.Min((int)idxF, col.Length - 2);
                        if (idx < 0) idx = 0;
                        Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], SombrasLib.Frac(idxF))
                            + new Vector2(MathF.Sin(k * 2.1f + tiempo * 3f) * 14f, -8f);
                        SombrasLib.Bruma(pos, 26f + 14f * SombrasLib.Frac(k * 0.73f),
                            0.50f * flujo * vivoF, tiempo, semilla + k * 7,
                            new Vector2(0f, -26f * flujo));
                    }
                }'''

lineas[ini:fin_bloque + 1] = NUEVO.split("\n")
io.open(F, "w", encoding="utf-8").write("\n".join(lineas))
print("ACTO 2b reemplazado por el coro de sombras")
