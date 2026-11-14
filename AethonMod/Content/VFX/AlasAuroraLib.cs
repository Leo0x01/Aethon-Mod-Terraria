using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using System.Collections.Generic;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public static class AlasAuroraLib
    {
        private struct Pluma
        {
            public Vector2[] Eje;

            public float[] SemiMas;

            public float[] SemiMenos;

            public float Largo;
        }

        public const int PlumasPorAla = 5;

        private const int PlumasMax = 7;

        public const int NodosPluma = 8;

        public const float OffPlegadoMin = 0.5f;

        public const float OffPlegadoMax = 0.92f;

        public const float OffAbiertoMin = 0.22f;

        public const float OffAbiertoMax = 1.54f;

        public const float LargoInterior = 88f;

        public const float LargoExterior = 48f;

        public const float RaizSemiancho = 3.5f;

        public const float PuntaSemianchoBase = 14f;

        public const float PuntaSemianchoAbierto = 8f;

        public const float HueEntrePlumas = 0.085f;

        public const float InclinacionPar = 0.12f;

        public const float BarridoAtras = 0.14f;

        public const float HueALoLargo = 0.16f;

        public const float HueTiempo = 0.015f;

        private static readonly Color BlancoOro = new Color(255, 243, 214);

        private static readonly Dictionary<string, Texture2D> _ruidos = new Dictionary<string, Texture2D>();

        private static BasicEffect _fxPlano;

        private static BasicEffect _fxTextura;

        private static Texture2D _ruido;

        private static bool _texturaProboFallar;

        // v6.118 — EL SOFT-ADD CANÓNICO DE LA CASA (la cura de LA SIERRA
        // QUE QUEDÓ). La reconstrucción v6.56.0 del binario dejó el
        // destino en ZERO — out = src·(1−dst) + 0: cada capa REEMPLAZABA
        // el fondo atenuado y los solapes del borde (velo + pluma + banda
        // + labio + ruido apilados) componían con ESCALONES
        // multiplicativos. Peor aún: donde la aguja de la pluma ya era
        // brillante (núcleo 0.88), la BANDA del borde maestro sumaba
        // ~0.53·(1−0.88) ≈ 0.06 — la curva continua DESAPARECÍA justo
        // sobre las puntas y brillaba en los valles: aguja · valle ·
        // aguja, LA SIERRA INVERTIDA que las dos curas geométricas
        // (v6.114 membrana + v6.115 borde maestro) no podían matar
        // porque NO era geometría — era el blend. El soft-add documentado
        // de RayoStrip.cs (v6.50.26, códice §3.2): dst += rgb·(1−dst) —
        // acumulación ASINTÓTICA que nunca clipea, funde cada borde EN
        // CURVA y la banda del borde maestro VIVE sobre las puntas.
        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private static VertexPositionColorTexture[] _verts = (VertexPositionColorTexture[])(object)new VertexPositionColorTexture[4096];

        private static readonly Pluma[] _plumas = new Pluma[14];

        private static readonly float[] _hues = new float[14];

        private static readonly float[] _fundidos = new float[14];

        private static readonly Vector2[][] _pozosEje = CrearPozosEje();

        private static readonly float[][] _pozosSemi = CrearPozosSemi();

        private static readonly float[][] _pozosSemi2 = CrearPozosSemi();

        private static bool _geoRunasValida;

        private static Vector2 _geoAnclaCentral;

        private static int _geoN;

        private const int GlifosAnillo = 8;

        private const float RadioAnillo = 46f;

        // v6.129 — LAS CONSTANTES DEL ALA LISA (la vela estirada al borde
        // liso): PAD (el borde por fuera de las puntas) · RNaceLisa (el
        // nacimiento junto al cuerpo) · DTHNace (la rotación de cierre de
        // los nacimientos) · UNace (el ancho del camino de los nacimientos).
        private const float PADLisa = 12f;

        private const float RNaceLisa = 16f;

        private const float DTHNace = 0.35f;

        private const float UNace = 0.35f;

        private static Vector2[][] CrearPozosEje()
        {
            Vector2[][] array = new Vector2[14][];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (Vector2[])(object)new Vector2[9];
            }
            return array;
        }

        private static float[][] CrearPozosSemi()
        {
            float[][] array = new float[14][];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new float[9];
            }
            return array;
        }

        private static Color ColorPrisma(float h)
        {
            //IL_00f0: Unknown result type (might be due to invalid IL or missing references)
            h -= MathF.Floor(h);
            float num;
            float num2;
            float num3;
            if (h < 1f / 6f)
            {
                num = 1f;
                num2 = h * 6f;
                num3 = 0f;
            }
            else if (h < 1f / 3f)
            {
                num = 2f - h * 6f;
                num2 = 1f;
                num3 = 0f;
            }
            else if (h < 0.5f)
            {
                num = 0f;
                num2 = 1f;
                num3 = h * 6f - 2f;
            }
            else if (h < 2f / 3f)
            {
                num = 0f;
                num2 = 4f - h * 6f;
                num3 = 1f;
            }
            else if (h < 5f / 6f)
            {
                num = h * 6f - 4f;
                num2 = 0f;
                num3 = 1f;
            }
            else
            {
                num = 1f;
                num2 = 0f;
                num3 = 6f - h * 6f;
            }
            return new Color((int)(num * 255f), (int)(num2 * 255f), (int)(num3 * 255f));
        }

        private static Color TonoPluma(float hue, float mezclaDia, float haciaBlanco)
        {
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            return TonoPluma(hue, mezclaDia, haciaBlanco, in AlasCatalogo.De(1));
        }

        private static Color TonoPluma(float hue, float mezclaDia, float haciaBlanco, in EstiloAlas estilo)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0018: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_001d: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e3: Unknown result type (might be due to invalid IL or missing references)
            Color val = DePaleta(in estilo, hue);
            Color val2 = ((estilo.Paleta == null) ? BlancoOro : estilo.ColorDia);
            float num = ((estilo.Paleta == null) ? 0.62f : estilo.FuerzaDia);
            Color val3 = new Color((int)((float)(int)(val).R + (float)((val2).R - (val).R) * (num * mezclaDia)), (int)((float)(int)(val).G + (float)((val2).G - (val).G) * (num * mezclaDia)), (int)((float)(int)(val).B + (float)((val2).B - (val).B) * (num * mezclaDia)));
            return new Color((int)((float)(int)(val3).R + (float)(255 - (val3).R) * haciaBlanco), (int)((float)(int)(val3).G + (float)(255 - (val3).G) * haciaBlanco), (int)((float)(int)(val3).B + (float)(255 - (val3).B) * haciaBlanco));
        }

        private static Color DePaleta(in EstiloAlas estilo, float h)
        {
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            //IL_0041: Unknown result type (might be due to invalid IL or missing references)
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_0052: Unknown result type (might be due to invalid IL or missing references)
            if (estilo.Paleta == null || estilo.Paleta.Length == 0)
            {
                return ColorPrisma(h);
            }
            Color[] paleta = estilo.Paleta;
            h -= MathF.Floor(h);
            float num = h * (float)paleta.Length;
            int num2 = (int)num % paleta.Length;
            int num3 = (num2 + 1) % paleta.Length;
            return Color.Lerp(paleta[num2], paleta[num3], num - (float)(int)num);
        }

        private static Pluma ConstruirPluma(Vector2 ancla, int i, float spread, float flap, float grav, int lado, float time, in EstiloAlas estilo, Vector2[] pozoEje, float[] pozoSemi, float[] pozoSemi2, float fundido, float rotCuerpo)
        {
            //IL_00b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ed: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0211: Unknown result type (might be due to invalid IL or missing references)
            //IL_0216: Unknown result type (might be due to invalid IL or missing references)
            //IL_021b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0289: Unknown result type (might be due to invalid IL or missing references)
            //IL_028b: Unknown result type (might be due to invalid IL or missing references)
            int num = ((estilo.Plumas < 2) ? 2 : ((estilo.Plumas > 7) ? 7 : estilo.Plumas));
            float num2 = (float)i / (float)(num - 1);
            float num3 = MathHelper.Lerp(estilo.PlegadoMin, estilo.PlegadoMax, num2);
            float num4 = MathHelper.Lerp(estilo.AbiertoMin, estilo.AbiertoMax, num2);
            float num5 = MathHelper.Lerp(num3, num4, spread);
            float num6 = 0.05f + 0.11f * spread;
            num5 += MathF.Sin(flap - (float)i * 0.45f) * num6;
            float num7 = -(float)Math.PI / 2f + (float)lado * (estilo.Inclinacion + num5);
            num7 += rotCuerpo * grav;
            Vector2 val = new Vector2(MathF.Cos(num7), MathF.Sin(num7) * grav);
            Vector2 val2 = new Vector2(0f - val.Y, val.X);
            float num8 = MathHelper.Lerp(estilo.LargoInterior, estilo.LargoExterior, num2) * (0.5f + 0.5f * spread);
            num8 *= 1f + MathF.Sin(flap * 0.7f + (float)i * 0.6f) * 0.045f;
            float num9 = MathHelper.Clamp(estilo.Vientre, 0.15f, 0.7f);
            float afilada = MathHelper.Clamp(estilo.Afilada, 0.35f, 2.5f);
            float num10 = MathHelper.Clamp(estilo.Filosa, 0f, 1f);
            // v6.114 — LA PUNTA REDONDA (la letra: «siguen teniendo picos
            // de sierra»): el perfil de la punta deja de ser SOLO la
            // aguja pow(1−t, y) (0.3 px en la última muestra — CADA pluma
            // moría en una punta de aguja) y se mezcla con el hombro
            // REDONDO del plectro: cos(t·π/2)^0.62 — llega a 0 por una
            // curva circular, no por un plomo.
            float redonda = MathHelper.Clamp(estilo.Redonda, 0f, 1f);
            float num11 = estilo.PuntaSemianchoBase + estilo.PuntaSemianchoAbierto * spread;
            float raizSemiancho = estilo.RaizSemiancho;
            float vientre = num9;
            float vientre2 = num9 * (1f - 0.45f * num10);
            float ancho = num11 * (1f + 0.18f * num10);
            float ancho2 = num11 * (1f - 0.3f * num10);
            Pluma result = default(Pluma);
            result.Eje = pozoEje;
            result.SemiMas = pozoSemi;
            result.SemiMenos = pozoSemi2;
            result.Largo = num8;
            int num12 = 9;
            for (int j = 0; j < num12; j++)
            {
                float num13 = (float)j / 8f;
                // v6.56.3 — LA CURVA ESPEJO: val2 (la perpendicular) apunta a +X
                // de pantalla en AMBOS lados — con Curva ≠ 0 las dos alas se
                // encorvaban hacia el MISMO lado (la izquierda se enganchaba
                // sobre el cuerpo: «no están bien centradas y están ligeramente
                // rotadas», la letra del usuario). Multiplicar por lado hace el
                // par espejo-simétrico: cada ala abre su garra hacia AFUERA.
                Vector2 val3 = ancla + val * (num8 * num13) + val2 * (estilo.Curva * (float)lado * num8 * num13 * num13);
                float num14 = Perfil(num13, raizSemiancho, vientre, ancho, afilada, redonda);
                float num15 = Perfil(num13, raizSemiancho, vientre2, ancho2, afilada, redonda);
                if (num13 > 0.7f)
                {
                    float num16 = (num13 - 0.7f) / 0.3f;
                    float num17 = MathHelper.Lerp(1f, 0.25f + 0.75f * fundido, num16 * num16);
                    num14 *= num17;
                    num15 *= num17;
                }
                pozoEje[j] = val3;
                // v6.56.4 — EL FILO DEL LADO (la asimetría que sobrevivió a la
                // v6.56.3): el ala izquierda ponía su borde GRUESO donde el
                // espejo pondría el fino de la derecha — perp(Mirror(T)) =
                // −Mirror(perp(T)): la perpendicular del ala izquierda es la
                // anti-espejada de la derecha, y el filo caía en el lado
                // anatómico EQUIVOCADO (3.5-14 px por pluma; hasta 68 px al
                // mirar a la izquierda). La PERMUTA: cada ala abre su filo
                // hacia AFUERA — el par queda espejo-simétrico de verdad (la
                // cura v6.56.3 solo simetrizó el EJE; la TIRA seguía sin
                // espejarse). Validado 1:1 en /home/z/sim/sim_alas.py:
                // par Δ=0.000px en eje y bordes; girar == espejo exacto.
                if (lado < 0)
                {
                    pozoSemi[j] = MathF.Max(num15, 0.3f);
                    pozoSemi2[j] = MathF.Max(num14, 0.3f);
                }
                else
                {
                    pozoSemi[j] = MathF.Max(num14, 0.3f);
                    pozoSemi2[j] = MathF.Max(num15, 0.3f);
                }
            }
            return result;
            static float Perfil(float q, float raiz, float num18, float num20, float y, float redonda)
            {
                if (q < num18)
                {
                    float num19 = q / num18;
                    return MathHelper.Lerp(raiz, num20, MathF.Pow(MathF.Sin(num19 * ((float)Math.PI / 2f)), 0.8f));
                }
                float num21 = (q - num18) / MathF.Max(1f - num18, 0.001f);
                // v6.114 — LA AGUJA Y EL PLECTRO: la taper clásica (pow
                // — la aguja) se mezcla con el hombro circular (cos — el
                // plectro) según Redonda: 0 = la aguja de siempre · 1 =
                // el perfil redondo completo de la pluma del vacío.
                float aguja = MathF.Pow(1f - num21, y);
                float plectro = MathF.Pow(MathF.Max(0f, MathF.Cos(num21 * MathF.PI * 0.5f)), 0.62f);
                return num20 * MathHelper.Lerp(aguja, plectro, redonda);
            }
        }

        /// <summary>
        /// v6.129 — LA VELA ESTIRADA AL BORDE LISO (el ala lisa, la letra:
        /// «esos dientes de sierra son supuestas plumas, esas plumas las
        /// puedes eliminar»).
        ///
        /// LA GEOMETRÍA: las plumas dejan de ser TIRAS y quedan como la
        /// ARMADURA invisible de UNA SOLA HOJA por lado. El borde es EL
        /// CAMINO u, parametrizado por CONSTRUCCIÓN (sin ambigüedades de
        /// atan2 ni envolturas por ±π):
        ///   · u=0 → LA CÚSPIDE (la pluma 0: la punta más alta y larga)
        ///   · u=1 → LA TENDIDA (la pluma 4: la punta horizontal)
        ///   · u=−U / u=1+U → LOS NACIMIENTOS junto al cuerpo (a RNACE px)
        /// R(u): la función SUAVE por tramos (smoothstep: nacimiento→
        /// cúspide→tendida→nacimiento — SIN Catmull: con los puntos del
        /// abanico desigualmente espaciados la Catmull hacía OVERSHOOT y
        /// la curva ONDULABA: la sierra de ±25 px que el sim cazó).
        /// θ(u): el ángulo lineal en [0,1] y el cierre hacia el cuerpo en
        /// los nacimientos.
        /// Cada eje se estira A LO LARGO DE SU DIRECCIÓN hasta SU punto
        /// del borde P(u_k) (el tramo final): la vela llega JUSTO al
        /// borde, sin dientes ni huecos. Las runas (nodo 8) coronan el
        /// borde en las direcciones de las plumas.
        /// </summary>
        private static int EstirarVelaLisa(Pluma[] plumas, float[] hues, float[] fundidos,
            Vector2 centroRot, Vector2 val2, int porLado, float fullRotation, float grav,
            in EstiloAlas estilo)
        {
            int nuevo = porLado + 2;
            if (nuevo * 2 > _pozosEje.Length)
            {
                return porLado * 2;      // (imposible: 14 pozos para 7+7 — la red)
            }
            // v6.131 — EL SNAPSHOT DE LA ARMADURA (la letra: «solo se ven
            // las alas de la izquierda y ademas parece que en algun punto
            // la textura de las alas se vuelve cuadrada o rectangular»).
            // LA AUTOPSIA: los pozos son COMPARTIDOS — las plumas vivas
            // del lado IZQUIERDO (índices 0..4) pisaban los mismos pozos
            // que el ESTIRADO iba a re-escribir ANTES de que el lado
            // DERECHO (índices 5..9) leyera SUS plumas viejas: las dos
            // primeras plumas del ala derecha nacían de los NACIMIENTOS
            // izquierdos ya escritos (stubs de 16 px casi sin masa) — y
            // con la punta de referencia pisoteada, el camino del borde
            // derecho se armaba con el ángulo de la punta IZQUIERDA: la
            // vela cruzaba LA ESPALDA (el bloque rectangular de la
            // captura) y el ala derecha MORÍA (solo se veía la
            // izquierda). LA CURA: TODO se calcula primero sobre UNA
            // COPIA de la armadura viva (los ejes viejos de AMBOS lados
            // intactos) y SOLO al final se piso el pozo — el lado
            // derecho lee SUS plumas de verdad, no los restos del
            // izquierdo.
            int totalViejo = porLado * 2;
            Vector2[][] ejesViejos = new Vector2[totalViejo][];
            for (int s = 0; s < totalViejo; s++)
            {
                ejesViejos[s] = (Vector2[])plumas[s].Eje.Clone();
            }
            for (int lado = -1; lado <= 1; lado += 2)
            {
                int bViejo = (lado < 0) ? 0 : porLado;
                int bNuevo = (lado < 0) ? 0 : nuevo;
                Vector2 ancla = val2 + new Vector2((float)lado * estilo.AnclajeSep, 0f);
                if (fullRotation != 0f)
                {
                    ancla = centroRot + Utils.RotatedBy(ancla - centroRot, (double)fullRotation, default(Vector2));
                }

                // === EL CAMINO DEL BORDE: P(u) y las fracciones u de las plumas ===
                float th0 = AnguloDe(ejesViejos[bViejo][8]);
                float th4 = AnguloDe(ejesViejos[bViejo + porLado - 1][8]);
                while (th4 > th0 + MathHelper.Pi) th4 -= MathHelper.TwoPi;
                while (th4 <= th0 - MathHelper.Pi) th4 += MathHelper.TwoPi;
                float r0 = Vector2.Distance(ejesViejos[bViejo][8], _geoAnclaCentral) + PADLisa;
                float r4 = Vector2.Distance(ejesViejos[bViejo + porLado - 1][8], _geoAnclaCentral) + PADLisa;
                float signo = (th4 >= th0) ? 1f : -1f;
                float thNaceSup = th0 - signo * DTHNace;
                float thNaceInf = th4 + signo * DTHNace;
                float[] us = new float[porLado];
                for (int k = 0; k < porLado; k++)
                {
                    float thk = AnguloDe(ejesViejos[bViejo + k][8]);
                    while (thk > th0 + MathHelper.Pi) thk -= MathHelper.TwoPi;
                    while (thk <= th0 - MathHelper.Pi) thk += MathHelper.TwoPi;
                    us[k] = (MathF.Abs(th4 - th0) > 1e-4f)
                        ? Math.Clamp((thk - th0) / (th4 - th0), 0f, 1f)
                        : 0f;
                }

                float ThetaDe(float u)
                {
                    if (u < 0f) return MathHelper.Lerp(th0, thNaceSup, -u / UNace);
                    if (u > 1f) return MathHelper.Lerp(th4, thNaceInf, (u - 1f) / UNace);
                    return MathHelper.Lerp(th0, th4, u);
                }
                float RadioDe(float u)
                {
                    if (u < 0f) return MathHelper.Lerp(RNaceLisa, r0, Suave(0f, 1f, (u + UNace) / UNace));
                    if (u > 1f) return MathHelper.Lerp(r4, RNaceLisa, Suave(0f, 1f, (u - 1f) / UNace));
                    return MathHelper.Lerp(r0, r4, Suave(0f, 1f, u));
                }
                Vector2 PuntoDe(float u)
                {
                    float th = ThetaDe(u);
                    return _geoAnclaCentral + new Vector2(MathF.Cos(th), MathF.Sin(th)) * RadioDe(u);
                }

                // === LOS EJES NUEVOS: [nacimiento, plumas estiradas, nacimiento] ===
                Vector2 nSup = PuntoDe(-UNace);
                Vector2 nInf = PuntoDe(1f + UNace);
                Vector2[][] ejesNuevos = new Vector2[nuevo][];
                for (int k = 0; k < nuevo; k++) ejesNuevos[k] = new Vector2[9];
                for (int j = 0; j < 9; j++)
                {
                    float q = (float)j / 8f;
                    // EL NACIMIENTO SUPERIOR: del ancla al punto P(−U)
                    ejesNuevos[0][j] = Vector2.Lerp(ancla, nSup, q);
                    // LAS PLUMAS ESTIRADAS: el tramo final corre a SU punto del borde
                    for (int k = 0; k < porLado; k++)
                    {
                        Vector2 punta = ejesViejos[bViejo + k][8];
                        Vector2 dirK = punta - ancla;
                        float l = dirK.Length();
                        if (l > 0.001f) dirK /= l; else dirK = new Vector2(0f, -grav);
                        Vector2 d = PuntoDe(us[k]) - punta;
                        float s = Vector2.Dot(d, dirK) * Suave(0.45f, 1f, q);
                        ejesNuevos[k + 1][j] = ejesViejos[bViejo + k][j] + dirK * s;
                    }
                    // EL NACIMIENTO INFERIOR: del ancla al punto P(1+U)
                    ejesNuevos[nuevo - 1][j] = Vector2.Lerp(ancla, nInf, q);
                }

                // PISO LOS POZOS (la armadura del borde), en ORDEN DE FRANJA
                for (int k = 0; k < nuevo; k++)
                {
                    Pluma p = default(Pluma);
                    p.Eje = _pozosEje[bNuevo + k];
                    p.SemiMas = _pozosSemi[bNuevo + k];
                    p.SemiMenos = _pozosSemi2[bNuevo + k];
                    for (int j = 0; j < 9; j++)
                    {
                        p.Eje[j] = ejesNuevos[k][j];
                        p.SemiMas[j] = 1f;
                        p.SemiMenos[j] = 1f;
                    }
                    p.Largo = Vector2.Distance(p.Eje[0], p.Eje[8]);
                    plumas[bNuevo + k] = p;
                    int vecino = Math.Clamp(k - 1, 0, porLado - 1);
                    hues[bNuevo + k] = hues[bViejo + vecino];
                    fundidos[bNuevo + k] = fundidos[bViejo + vecino];
                }
            }
            return nuevo * 2;
        }

        /// <summary>El ángulo de un punto del ala respecto del ancla central.</summary>
        private static float AnguloDe(Vector2 p)
        {
            return MathF.Atan2(p.Y - _geoAnclaCentral.Y, p.X - _geoAnclaCentral.X);
        }

        /// <summary>v6.129 — EL PUNTO DEL CAMINO DEL BORDE LISO (el mismo
        /// camino de EstirarVelaLisa, evaluado por el borde maestro): el
        /// ángulo lineal en [0,1] con el cierre hacia el cuerpo en los
        /// nacimientos, y el radio en smoothstep por tramos (nacimiento →
        /// cúspide → tendida → nacimiento).</summary>
        private static Vector2 PuntoCaminoLisa(float u,
            float th0, float th4, float r0, float r4, float thNS, float thNI)
        {
            float th;
            if (u < 0f) th = MathHelper.Lerp(th0, thNS, -u / UNace);
            else if (u > 1f) th = MathHelper.Lerp(th4, thNI, (u - 1f) / UNace);
            else th = MathHelper.Lerp(th0, th4, u);
            float r;
            if (u < 0f) r = MathHelper.Lerp(RNaceLisa, r0, Suave(0f, 1f, (u + UNace) / UNace));
            else if (u > 1f) r = MathHelper.Lerp(r4, RNaceLisa, Suave(0f, 1f, (u - 1f) / UNace));
            else r = MathHelper.Lerp(r0, r4, Suave(0f, 1f, u));
            return _geoAnclaCentral + new Vector2(MathF.Cos(th), MathF.Sin(th)) * r;
        }

        private static int EmitirCapa(int nVerts, Pluma[] plumas, float[] hues, float[] fundidos, float mezclaDia, int capa, in EstiloAlas estilo, int n)
        {
            //IL_00ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00de: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_0117: Unknown result type (might be due to invalid IL or missing references)
            //IL_0126: Unknown result type (might be due to invalid IL or missing references)
            //IL_012b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0101: Unknown result type (might be due to invalid IL or missing references)
            //IL_0106: Unknown result type (might be due to invalid IL or missing references)
            //IL_0130: Unknown result type (might be due to invalid IL or missing references)
            //IL_0140: Unknown result type (might be due to invalid IL or missing references)
            //IL_0142: Unknown result type (might be due to invalid IL or missing references)
            //IL_0156: Unknown result type (might be due to invalid IL or missing references)
            //IL_0158: Unknown result type (might be due to invalid IL or missing references)
            //IL_015d: Unknown result type (might be due to invalid IL or missing references)
            //IL_015f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0164: Unknown result type (might be due to invalid IL or missing references)
            //IL_0169: Unknown result type (might be due to invalid IL or missing references)
            //IL_016e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0152: Unknown result type (might be due to invalid IL or missing references)
            //IL_0154: Unknown result type (might be due to invalid IL or missing references)
            //IL_0184: Unknown result type (might be due to invalid IL or missing references)
            //IL_018c: Unknown result type (might be due to invalid IL or missing references)
            //IL_021b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0224: Unknown result type (might be due to invalid IL or missing references)
            //IL_0229: Unknown result type (might be due to invalid IL or missing references)
            //IL_024e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0257: Unknown result type (might be due to invalid IL or missing references)
            //IL_025c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0268: Unknown result type (might be due to invalid IL or missing references)
            //IL_026d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0271: Unknown result type (might be due to invalid IL or missing references)
            //IL_0276: Unknown result type (might be due to invalid IL or missing references)
            //IL_0280: Unknown result type (might be due to invalid IL or missing references)
            //IL_0285: Unknown result type (might be due to invalid IL or missing references)
            //IL_028e: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_02b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02de: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0304: Unknown result type (might be due to invalid IL or missing references)
            //IL_031a: Unknown result type (might be due to invalid IL or missing references)
            //IL_031f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0323: Unknown result type (might be due to invalid IL or missing references)
            //IL_0328: Unknown result type (might be due to invalid IL or missing references)
            //IL_0332: Unknown result type (might be due to invalid IL or missing references)
            //IL_0337: Unknown result type (might be due to invalid IL or missing references)
            //IL_0340: Unknown result type (might be due to invalid IL or missing references)
            //IL_0355: Unknown result type (might be due to invalid IL or missing references)
            //IL_0357: Unknown result type (might be due to invalid IL or missing references)
            //IL_0367: Unknown result type (might be due to invalid IL or missing references)
            //IL_0369: Unknown result type (might be due to invalid IL or missing references)
            //IL_0379: Unknown result type (might be due to invalid IL or missing references)
            //IL_037b: Unknown result type (might be due to invalid IL or missing references)
            //IL_038b: Unknown result type (might be due to invalid IL or missing references)
            //IL_038d: Unknown result type (might be due to invalid IL or missing references)
            //IL_039d: Unknown result type (might be due to invalid IL or missing references)
            //IL_039f: Unknown result type (might be due to invalid IL or missing references)
            //IL_03af: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_017e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0180: Unknown result type (might be due to invalid IL or missing references)
            float num;
            float num2;
            switch (capa)
            {
            case 0:
                num = estilo.NucleoFrac;
                num2 = estilo.NucleoInten;
                break;
            case 1:
                num = estilo.CuerpoFrac;
                num2 = estilo.CuerpoInten;
                break;
            default:
                num = 1f;
                num2 = estilo.HaloInten;
                break;
            }
            Vector2 val4 = default(Vector2);
            VertexPositionColorTexture val7 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val8 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val9 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val10 = default(VertexPositionColorTexture);
            for (int i = 0; i < n; i++)
            {
                ref Pluma reference = ref plumas[i];
                if (nVerts + 48 > _verts.Length)
                {
                    break;
                }
                float num3 = capa switch
                {
                    1 => 0.3f, 
                    0 => 0.72f, 
                    _ => 0.12f, 
                };
                for (int j = 0; j < 8; j++)
                {
                    float num4 = (float)j / 8f;
                    float num5 = (float)(j + 1) / 8f;
                    Vector2 val = ((j > 0) ? (reference.Eje[j] - reference.Eje[j - 1]) : (reference.Eje[j + 1] - reference.Eje[j]));
                    Vector2 val2 = ((j < 7) ? (reference.Eje[j + 2] - reference.Eje[j + 1]) : (reference.Eje[j + 1] - reference.Eje[j]));
                    if ((val).LengthSquared() < 0.0001f)
                    {
                        val = val2;
                    }
                    if ((val2).LengthSquared() < 0.0001f)
                    {
                        val2 = val;
                    }
                    Vector2 val3 = Vector2.Normalize(Vector2.Normalize(val) + Vector2.Normalize(val2));
                    if (!((val3).LengthSquared() >= 0.001f))
                    {
                        val3 = val2;
                    }
                    val4 = new Vector2(0f - val3.Y, val3.X);
                    float num6 = reference.SemiMas[j] * num;
                    float num7 = reference.SemiMas[j + 1] * num;
                    float num8 = reference.SemiMenos[j] * num;
                    float num9 = reference.SemiMenos[j + 1] * num;
                    float num10 = MathHelper.Lerp(0.22f, fundidos[i], num4 * num4);
                    float num11 = MathHelper.Lerp(0.22f, fundidos[i], num5 * num5);
                    // v6.127 — LA MUERTE DE LA AGUJA (la cura de la sierra,
                    // acto I): el perfil v6.56 SUBÍA hacia la punta (0.22 →
                    // fundido): el punto MÁS BRILLANTE de cada pluma era
                    // exactamente su PUNTA — la aguja que clavaba cada
                    // diente de la sierra en el borde. Con vela (Membrana > 0)
                    // las plumas son las VENAS de la vela: su brillo MUERE
                    // DENTRO (smoothstep 1→0 en el último tramo) y el filo
                    // SOLO pertenece a la curva del borde maestro — en q=1
                    // el aporte es CERO EXACTO.
                    if (estilo.Membrana > 0f)
                    {
                        num10 *= 1f - Suave(0.60f, 1f, num4);
                        num11 *= 1f - Suave(0.60f, 1f, num5);
                    }
                    Color val5 = Pre(TonoPluma(hues[i] + estilo.HueLargo * num4, mezclaDia, num3 * (1f - 0.3f * num4), in estilo), num2 * num10);
                    Color val6 = Pre(TonoPluma(hues[i] + estilo.HueLargo * num5, mezclaDia, num3 * (1f - 0.3f * num5), in estilo), num2 * num11);
                    val7 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] + val4 * num6, 0f), val5, new Vector2(num4, 0f));
                    val8 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] - val4 * num8, 0f), val5, new Vector2(num4, 1f));
                    val9 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] + val4 * num7, 0f), val6, new Vector2(num5, 0f));
                    val10 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] - val4 * num9, 0f), val6, new Vector2(num5, 1f));
                    _verts[nVerts++] = val7;
                    _verts[nVerts++] = val8;
                    _verts[nVerts++] = val9;
                    _verts[nVerts++] = val7;
                    _verts[nVerts++] = val9;
                    _verts[nVerts++] = val10;
                }
            }
            return nVerts;
        }

        /// <summary>v6.127 — El smoothstep de la casa para las curas de la
        /// sierra (nace 0, muere 1, derivada 0 en ambos cabos).</summary>
        private static float Suave(float e0, float e1, float x)
        {
            float t = MathHelper.Clamp((x - e0) / Math.Max(e1 - e0, 0.0001f), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static int EmitirCapaRuido(int nVerts, Pluma[] plumas, float[] hues, float mezclaDia, float time, int pasada, float fold, in EstiloAlas estilo, int n)
        {
            //IL_00e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_010f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0116: Unknown result type (might be due to invalid IL or missing references)
            //IL_011b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0170: Unknown result type (might be due to invalid IL or missing references)
            //IL_0180: Unknown result type (might be due to invalid IL or missing references)
            //IL_0185: Unknown result type (might be due to invalid IL or missing references)
            //IL_014d: Unknown result type (might be due to invalid IL or missing references)
            //IL_015b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0160: Unknown result type (might be due to invalid IL or missing references)
            //IL_018a: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_019c: Unknown result type (might be due to invalid IL or missing references)
            //IL_01aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_01af: Unknown result type (might be due to invalid IL or missing references)
            //IL_01db: Unknown result type (might be due to invalid IL or missing references)
            //IL_01eb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ed: Unknown result type (might be due to invalid IL or missing references)
            //IL_0201: Unknown result type (might be due to invalid IL or missing references)
            //IL_0203: Unknown result type (might be due to invalid IL or missing references)
            //IL_0208: Unknown result type (might be due to invalid IL or missing references)
            //IL_020a: Unknown result type (might be due to invalid IL or missing references)
            //IL_020f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0214: Unknown result type (might be due to invalid IL or missing references)
            //IL_0219: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_022f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0237: Unknown result type (might be due to invalid IL or missing references)
            //IL_0255: Unknown result type (might be due to invalid IL or missing references)
            //IL_0257: Unknown result type (might be due to invalid IL or missing references)
            //IL_025b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0260: Unknown result type (might be due to invalid IL or missing references)
            //IL_0262: Unknown result type (might be due to invalid IL or missing references)
            //IL_0264: Unknown result type (might be due to invalid IL or missing references)
            //IL_0268: Unknown result type (might be due to invalid IL or missing references)
            //IL_026d: Unknown result type (might be due to invalid IL or missing references)
            //IL_027a: Unknown result type (might be due to invalid IL or missing references)
            //IL_027f: Unknown result type (might be due to invalid IL or missing references)
            //IL_028b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0290: Unknown result type (might be due to invalid IL or missing references)
            //IL_029a: Unknown result type (might be due to invalid IL or missing references)
            //IL_029f: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a5: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_02cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02da: Unknown result type (might be due to invalid IL or missing references)
            //IL_02df: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e5: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0301: Unknown result type (might be due to invalid IL or missing references)
            //IL_030f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0314: Unknown result type (might be due to invalid IL or missing references)
            //IL_031e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0323: Unknown result type (might be due to invalid IL or missing references)
            //IL_0329: Unknown result type (might be due to invalid IL or missing references)
            //IL_0340: Unknown result type (might be due to invalid IL or missing references)
            //IL_0345: Unknown result type (might be due to invalid IL or missing references)
            //IL_0353: Unknown result type (might be due to invalid IL or missing references)
            //IL_0358: Unknown result type (might be due to invalid IL or missing references)
            //IL_0362: Unknown result type (might be due to invalid IL or missing references)
            //IL_0367: Unknown result type (might be due to invalid IL or missing references)
            //IL_036d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0382: Unknown result type (might be due to invalid IL or missing references)
            //IL_0384: Unknown result type (might be due to invalid IL or missing references)
            //IL_0394: Unknown result type (might be due to invalid IL or missing references)
            //IL_0396: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_03cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_03dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_03de: Unknown result type (might be due to invalid IL or missing references)
            //IL_0229: Unknown result type (might be due to invalid IL or missing references)
            //IL_022b: Unknown result type (might be due to invalid IL or missing references)
            float num;
            float num2;
            float num3;
            float num4;
            float num5;
            if (pasada == 0)
            {
                num = 1.7f * estilo.RuidoEscala;
                num2 = (0.2f - fold * 0.08f) * estilo.RuidoFlujo;
                num3 = 0.6f * estilo.RuidoEscala;
                num4 = 0.31f * estilo.RuidoFlujo;
                num5 = 0.46f;
            }
            else
            {
                num = 3.4f * estilo.RuidoEscala;
                num2 = -0.12f * estilo.RuidoFlujo;
                num3 = 1.15f * estilo.RuidoEscala;
                num4 = 0f;
                num5 = 0.3f;
            }
            num5 *= estilo.RuidoInten;
            // v6.118 → v6.123 — EL ENSOGLADO DEL RUIDO — INCONDICIONAL
            // (la letra: «las alas runicas del vacio siguen con picos o
            // mejor dicho dientes de cierra, eso debes quitarlo»). LA
            // AUTOPSIA: la cura de la v6.118 vivía tras «membrana>0» — y
            // la v6.120 convirtió las alas del vacío en copia del
            // eclipse (membrana 0): el ruido VOLVIÓ a correr al ANCHO
            // COMPLETO de la pluma (inset 1) y los blobs de la textura
            // con alfa en el borde volvieron a sembrar dientes FUERA de
            // la curva — el pico que el usuario ve. Ahora la niebla corre
            // por DENTRO del ala SIEMPRE (membrana o no): el filo SOLO
            // pertenece a la curva de la pluma.
            float inset = pasada == 0 ? 0.86f : 0.72f;
            Vector2 val6 = default(Vector2);
            VertexPositionColorTexture val9 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val10 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val11 = default(VertexPositionColorTexture);
            VertexPositionColorTexture val12 = default(VertexPositionColorTexture);
            for (int i = 0; i < n; i++)
            {
                ref Pluma reference = ref plumas[i];
                if (nVerts + 48 > _verts.Length)
                {
                    break;
                }
                float num6 = hues[i] * 7.3f;
                float num7 = (0f - time) * num2 + num6;
                float num8 = num6 * num4 * 0.1f;
                float num9 = num3 + num8;
                Color val = Pre(TonoPluma(hues[i], mezclaDia, 0.05f, in estilo), num5 * 0.3f);
                Color val2 = Pre(TonoPluma(hues[i] + estilo.HueLargo, mezclaDia, 0.05f, in estilo), num5);
                for (int j = 0; j < 8; j++)
                {
                    float num10 = (float)j / 8f;
                    float num11 = (float)(j + 1) / 8f;
                    Vector2 val3 = ((j > 0) ? (reference.Eje[j] - reference.Eje[j - 1]) : (reference.Eje[j + 1] - reference.Eje[j]));
                    Vector2 val4 = ((j < 7) ? (reference.Eje[j + 2] - reference.Eje[j + 1]) : (reference.Eje[j + 1] - reference.Eje[j]));
                    if ((val3).LengthSquared() < 0.0001f)
                    {
                        val3 = val4;
                    }
                    if ((val4).LengthSquared() < 0.0001f)
                    {
                        val4 = val3;
                    }
                    Vector2 val5 = Vector2.Normalize(Vector2.Normalize(val3) + Vector2.Normalize(val4));
                    if (!((val5).LengthSquared() >= 0.001f))
                    {
                        val5 = val4;
                    }
                    val6 = new Vector2(0f - val5.Y, val5.X);
                    float num12 = num7 + num * num10;
                    float num13 = num7 + num * num11;
                    Color val7 = Color.Lerp(val, val2, num10);
                    Color val8 = Color.Lerp(val, val2, num11);
                    val9 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] + val6 * (reference.SemiMas[j] * inset), 0f), val7, new Vector2(num12, num8));
                    val10 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] - val6 * (reference.SemiMenos[j] * inset), 0f), val7, new Vector2(num12, num9));
                    val11 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] + val6 * (reference.SemiMas[j + 1] * inset), 0f), val8, new Vector2(num13, num8));
                    val12 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] - val6 * (reference.SemiMenos[j + 1] * inset), 0f), val8, new Vector2(num13, num9));
                    _verts[nVerts++] = val9;
                    _verts[nVerts++] = val10;
                    _verts[nVerts++] = val11;
                    _verts[nVerts++] = val9;
                    _verts[nVerts++] = val11;
                    _verts[nVerts++] = val12;
                }
            }
            return nVerts;
        }

        // ====================================================================
        //  v6.114 — LA MEMBRANA DEL VACÍO (la letra: «las alas runicas del
        //  vacio siguen teniendo picos de sierra, debes arreglar eso»).
        //
        //  LA AUTOPSIA: la v6.113 redondeó cada pluma por dentro (afilada
        //  0.5 + puntas 20/14), pero el CONTORNO del ala seguía siendo el
        //  de SIEMPRE: CINCO tiras separadas en abanico — pico, valle,
        //  pico, valle, pico. Eso ES una sierra, por redondas que sean
        //  las puntas.
        //
        //  LA CURA SON DOS PIEZAS entre las plumas vecinas de cada lado:
        //   · EL VELO — quads entre los EJES de las plumas vecinas: los
        //     valles se RELLENAN con un tul que se apaga hacia afuera.
        //     El ala se lee como UNA superficie con venas (las plumas)
        //     por dentro, no como cinco tiras.
        //   · EL ARCO — la banda exterior de Bezier: de la punta de la
        //     pluma i a la de la i+1, curvándose HACIA AFUERA (el control
        //     se aleja del ancla central). El contorno exterior queda
        //     siendo UNA curva continua y convexa de punta a punta — el
        //     borde brilla (rim) fundiéndose en cada punta.
        //
        //  TODO se construye SOBRE la geometría ya espejada (el bloque
        //  del espejo corre antes): la membrana es simétrica por
        //  construcción, y el sentido del bulto sale de las POSICIONES
        //  reales (afuera del ancla) — el espejo lo respeta solo.
        // ====================================================================
        private static int EmitirMembrana(int nVerts, Pluma[] plumas, float[] hues, float[] fundidos, float mezclaDia, in EstiloAlas estilo, int n)
        {
            if (estilo.Membrana <= 0f || n < 4)
            {
                return nVerts;
            }
            int porLado = n / 2;
            float fuerza = estilo.Membrana;
            for (int lado = 0; lado < 2; lado++)
            {
                int b = lado * porLado;
                for (int i = 0; i < porLado - 1; i++)
                {
                    if (nVerts + 96 > _verts.Length)
                    {
                        break;
                    }
                    ref Pluma pA = ref plumas[b + i];
                    ref Pluma pB = ref plumas[b + i + 1];
                    float fundido = (fundidos[b + i] + fundidos[b + i + 1]) * 0.5f;
                    float hueMedio = (hues[b + i] + hues[b + i + 1]) * 0.5f;

                    // --- EL VELO: el relleno entre los ejes vecinos ---
                    // v6.115 — LA VELA (la autopsia de la sierra que
                    // QUEDÓ): el velo de la v6.114 brillaba al ~40 % del
                    // cuerpo — el interior del ala se leía como HUECOS
                    // entre plumas brillantes, no como UNA superficie.
                    // Ahora arranca a ~0.90×CuerpoInten en la raíz y
                    // sostiene ~0.52× en el borde: el ala es UNA vela y
                    // las plumas son las VENAS que la recorren.
                    for (int j = 0; j < 8; j++)
                    {
                        float q0 = (float)j / 8f;
                        float q1 = (float)(j + 1) / 8f;
                        // v6.127 — LA VELA CLAREA HACIA EL BORDILLO (la cura
                        // de la sierra, acto II — LA CAUSA RAÍZ): el velo
                        // heredaba la paleta VIAJERA al 5 % de blanco y CAÍA
                        // de 0.90 a 0.52 hacia el borde — entre las plumas
                        // 2-4 (hue 0.71-0.88) el tono viaja por el FONDO
                        // NEGRO de la paleta (26,14,44): el bordillo quedaba
                        // CASI INVISIBLE y el ojo solo veía las CINCO plumas
                        // brillantes = CINCO DIENTES (zigzag del contorno
                        // percibido: 31.9 px medidos en el sim 1:1). La vela
                        // ahora SUBE (0.55 → 1.17 ×CuerpoInten) y VIAJA HACIA
                        // EL BLANCO (0.05 → 0.43) al acercarse al borde: el
                        // bordillo es lo MÁS LUMINOSO del ala y la silueta
                        // pertenece a LA CURVA (zigzag 3.5 px — sim verificado).
                        // v6.129 — EL ALA LISA ES TODA VELA (la letra: «esas
                        // plumas las puedes eliminar»): sin las tiras, la
                        // vela ES el cuerpo ENTERO del ala — su base SUBE
                        // (0.55 → 0.80) para que la superficie no quede
                        // tenue donde vivían los núcleos de las plumas: el
                        // cristal del vacío se lee LLENO de la raíz al borde.
                        float velaBase = estilo.Lisas ? 0.80f : 0.55f;
                        float velaTope = estilo.Lisas ? 1.30f : 0.62f;
                        float sube0 = Suave(0.35f, 0.95f, q0);
                        float sube1 = Suave(0.35f, 0.95f, q1);
                        float i0 = (velaBase + velaTope * sube0) * estilo.CuerpoInten * fuerza * fundido;
                        float i1 = (velaBase + velaTope * sube1) * estilo.CuerpoInten * fuerza * fundido;
                        float hb0 = 0.05f + 0.38f * sube0;
                        float hb1 = 0.05f + 0.38f * sube1;
                        // v6.129 — EL TONO EN RAMPA RADIAL (el ala lisa): la
                        // vela de siempre viaja por franjas (hue_medio por
                        // cada par de plumas) — y la PALETA VIAJERA caía al
                        // FONDO NEGRO en las franjas del medio: el ojo leía
                        // CINCO TIRAS claras separadas por huecos oscuros
                        // (las «plumas» que el usuario ve como dientes).
                        // En el ala lisa NO hay franjas: el tono es LA RAMPA
                        // RADIAL de la casa (el violeta VIVO en la raíz → el
                        // HONDO hacia el borde → el BLANCO del bordillo):
                        // hue 0 (paleta[0], el vivo) → 1/3 (paleta[1], el
                        // hondo PURO) — el FONDO de la paleta JAMÁS se toca.
                        float hue0l = estilo.Lisas
                            ? (1f / 3f) * Suave(0f, 0.85f, q0)
                            : hueMedio + estilo.HueLargo * q0;
                        float hue1l = estilo.Lisas
                            ? (1f / 3f) * Suave(0f, 0.85f, q1)
                            : hueMedio + estilo.HueLargo * q1;
                        Color c0 = Pre(TonoPluma(hue0l, mezclaDia, hb0, in estilo), i0);
                        Color c1 = Pre(TonoPluma(hue1l, mezclaDia, hb1, in estilo), i1);
                        Vector2 a0 = pA.Eje[j];
                        Vector2 a1 = pA.Eje[j + 1];
                        Vector2 b0 = pB.Eje[j];
                        Vector2 b1 = pB.Eje[j + 1];
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a0, 0f), c0, new Vector2(q0, 0f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b0, 0f), c0, new Vector2(q0, 1f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b1, 0f), c1, new Vector2(q1, 1f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a0, 0f), c0, new Vector2(q0, 0f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b1, 0f), c1, new Vector2(q1, 1f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a1, 0f), c1, new Vector2(q1, 0f));
                    }
                }
            }
            // v6.115 — EL BORDE MAESTRO (la cura de verdad): UNA curva
            // continua por las CINCO puntas — sin los arcos por hueco.
            return EmitirBordeMaestro(nVerts, plumas, hues, fundidos, mezclaDia, 0f, flujo: false, 0f, in estilo, n);
        }

        /// <summary>v6.115 — La Catmull-Rom uniforme del borde maestro
        /// (C1: la tangente es CONTINUA de punta a punta — sin
        /// esquinas, sin dientes).</summary>
        private static Vector2 Catmull(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 +
                           (p2 - p0) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// <summary>
        /// v6.115 — EL BORDE MAESTRO: LA CURVA ÚNICA QUE MATA LA SIERRA.
        ///
        /// LA AUTOPSIA (por qué la v6.114 no curó): (1) cada pluma
        /// termina en una AGUJA — el perfil, aun «redondo», cae a 0 px
        /// en la última muestra: CINCO puntas = CINCO dientes; (2) el
        /// velo brillaba al 40 % del cuerpo (huecos oscuros entre las
        /// plumas); (3) el «arco» por hueco era un HILO: grosor 0 en
        /// cada punta, pandeo 0.17·|hueco| y brillo ×HaloInten (0.15
        /// — casi invisible). El ojo leía: aguja brillante · valle
        /// oscuro · aguja brillante = SIERRA.
        ///
        /// LA CURA GEOMÉTRICA: una Catmull-Rom C1-CONTINUA que pasa por
        /// las CINCO puntas del abanico — LA MISMA curva para todo el
        /// borde, sin costuras. Sobre ella:
        ///   · LA BANDA (~12 px hacia adentro): el cuerpo del borde al
        ///     nivel del ala — la aguja final de cada pluma TERMINA
        ///     DENTRO de la banda (su taper vive bajo el brillo).
        ///   · EL LABIO (2.5 px hacia afuera): el rim luminoso que se
        ///     apaga al exterior — la silueta ES una curva brillante
        ///     continua de punta a punta.
        /// El contorno del ala deja de ser «cinco dientes» y pasa a ser
        /// UNA curva convexa viva — las plumas se leen como venas que
        /// corren DENTRO de la vela hasta su borde.
        /// </summary>
        private static int EmitirBordeMaestro(int nVerts, Pluma[] plumas, float[] hues, float[] fundidos, float mezclaDia, float time, bool flujo, float fold, in EstiloAlas estilo, int n)
        {
            if (estilo.Membrana <= 0f || n < 4)
            {
                return nVerts;
            }
            int porLado = n / 2;
            if (porLado < 2)
            {
                return nVerts;
            }
            float fuerza = estilo.Membrana;
            // v6.127 — EL BORDILLO LUMINOSO (la cura de la sierra, acto
            // III): la banda heredaba el CUERPO (tope 0.72) — el borde era
            // lo MÁS TENUE del ala y la silueta la dibujaban las puntas de
            // las plumas. Ahora la banda sube ×1.5 (tope 1.05), engorda a
            // 16 px y CLAREA (hacia blanco +0.12/+0.20): la orilla de la
            // vela es lo MÁS LUMINOSO del ala — la silueta ES la curva
            // Catmull-Rom, de punta a punta.
            float brBorde = MathHelper.Clamp(estilo.CuerpoInten * 1.55f * 1.5f, 0.30f, 1.05f);
            float grosor = 12f + 4f * fuerza;
            float labio = 2.5f * fuerza;
            float rimBlanco = 0.20f;
            float flujoT = (0.2f - fold * 0.08f) * estilo.RuidoFlujo;
            float escalaU = 1.7f * estilo.RuidoEscala;
            float respiracion = 0.92f + 0.08f * MathF.Sin(time * 2.4f);
            for (int lado = 0; lado < 2; lado++)
            {
                int b = lado * porLado;
                int m = porLado;
                int muestras = (m - 1) * 6;
                // v6.129 — EL BORDE MUESTREADO (el ala lisa): el CAMINO u de
                // EstirarVelaLisa (nacimiento→cúspide→tendida→nacimiento)
                // evaluado en u EQUIESPACIADO — SIN Catmull (con los puntos
                // desiguales del abanico la Catmull hacía OVERSHOOT y la
                // curva ONDULABA: la sierra de ±25 px que el sim cazó y el
                // QA VLM cerró: «el borde es UNA CURVA CONTINUA y suave,
                // sin picos ni dentado» — 9/10).
                float th0l = 0f, th4l = 0f, r0l = 0f, r4l = 0f, signoL = 1f;
                float thNS = 0f, thNI = 0f;
                bool lisa = estilo.Lisas;
                if (lisa)
                {
                    th0l = AnguloDe(plumas[b + 1].Eje[8]);
                    th4l = AnguloDe(plumas[b + m - 2].Eje[8]);
                    while (th4l > th0l + MathHelper.Pi) th4l -= MathHelper.TwoPi;
                    while (th4l <= th0l - MathHelper.Pi) th4l += MathHelper.TwoPi;
                    r0l = Vector2.Distance(plumas[b + 1].Eje[8], _geoAnclaCentral);
                    r4l = Vector2.Distance(plumas[b + m - 2].Eje[8], _geoAnclaCentral);
                    signoL = (th4l >= th0l) ? 1f : -1f;
                    thNS = th0l - signoL * DTHNace;
                    thNI = th4l + signoL * DTHNace;
                }
                for (int s = 0; s < muestras; s++)
                {
                    if (nVerts + 12 > _verts.Length)
                    {
                        break;
                    }
                    int seg;
                    Vector2 p0, p1;
                    if (lisa)
                    {
                        float u0 = -UNace + (1f + 2f * UNace) * s / muestras;
                        float u1 = -UNace + (1f + 2f * UNace) * (s + 1) / muestras;
                        p0 = PuntoCaminoLisa(u0, th0l, th4l, r0l, r4l, thNS, thNI);
                        p1 = PuntoCaminoLisa(u1, th0l, th4l, r0l, r4l, thNS, thNI);
                        seg = Math.Min(m - 2, (int)(u0 / (1f + 2f * UNace) * (m - 1)));
                    }
                    else
                    {
                        seg = s / 6;
                        float k0 = (float)(s % 6) / 6f;
                        float k1 = (float)(s % 6 + 1) / 6f;
                        Vector2 q0p = (seg > 0) ? plumas[b + seg - 1].Eje[8] : plumas[b].Eje[8];
                        Vector2 q1p = plumas[b + seg].Eje[8];
                        Vector2 q2p = plumas[b + seg + 1].Eje[8];
                        Vector2 q3p = (seg + 2 < m) ? plumas[b + seg + 2].Eje[8] : plumas[b + m - 1].Eje[8];
                        p0 = Catmull(q0p, q1p, q2p, q3p, k0);
                        p1 = Catmull(q0p, q1p, q2p, q3p, k1);
                    }

                    // El tono viaja por el borde: el hue de la pluma que
                    // sostiene cada tramo, fundido con la siguiente.
                    // v6.129 — en el ala lisa el borde es UNA curva y UN
                    // tono: la RAMPA RADIAL en su extremo (el HONDO puro,
                    // hue 1/3 — sin las franjas de la paleta viajera).
                    float fSeg0 = estilo.Lisas
                        ? (float)s / muestras
                        : (seg + (float)(s % 6) / 6f) / (m - 1);
                    float fSeg1 = estilo.Lisas
                        ? (float)(s + 1) / muestras
                        : (seg + (float)(s % 6 + 1) / 6f) / (m - 1);
                    int iA0 = Math.Min(m - 1, (int)(fSeg0 * (m - 1) + 0.5f));
                    int iA1 = Math.Min(m - 1, (int)(fSeg1 * (m - 1) + 0.5f));
                    float hue0 = estilo.Lisas ? (1f / 3f) : hues[b + iA0];
                    float hue1 = estilo.Lisas ? (1f / 3f) : hues[b + iA1];
                    float fund = (fundidos[b + iA0] + fundidos[b + iA1]) * 0.5f;

                    // La dirección radial (del jugador hacia afuera) da
                    // el GROSOR de la banda: hacia adentro el cuerpo,
                    // hacia afuera el labio.
                    Vector2 rad0 = p0 - _geoAnclaCentral;
                    float len0 = rad0.Length();
                    if (len0 < 0.001f)
                    {
                        rad0 = new Vector2(0f, -1f);
                    }
                    else
                    {
                        rad0 /= len0;
                    }
                    Vector2 rad1 = p1 - _geoAnclaCentral;
                    float len1 = rad1.Length();
                    if (len1 < 0.001f)
                    {
                        rad1 = new Vector2(0f, -1f);
                    }
                    else
                    {
                        rad1 /= len1;
                    }
                    Vector2 in0 = p0 - rad0 * grosor;
                    Vector2 in1 = p1 - rad1 * grosor;
                    Vector2 out0 = p0 + rad0 * labio;
                    Vector2 out1 = p1 + rad1 * labio;

                    float sGlob0 = (float)s / muestras;
                    float sGlob1 = (float)(s + 1) / muestras;

                    // --- LA BANDA: el cuerpo del borde (in → borde) ---
                    Color bi0 = Pre(TonoPluma(hue0, mezclaDia, 0.18f + rimBlanco * 0.6f, in estilo), brBorde * 0.55f * fuerza * fund);
                    Color bi1 = Pre(TonoPluma(hue1, mezclaDia, 0.18f + rimBlanco * 0.6f, in estilo), brBorde * 0.55f * fuerza * fund);
                    Color bo0 = Pre(TonoPluma(hue0, mezclaDia, 0.28f + rimBlanco, in estilo), brBorde * 0.95f * fuerza * fund * respiracion);
                    Color bo1 = Pre(TonoPluma(hue1, mezclaDia, 0.28f + rimBlanco, in estilo), brBorde * 0.95f * fuerza * fund * respiracion);
                    Vector2 uvIn0 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob0 * 2.2f, 0.72f) : new Vector2(sGlob0, 0f);
                    Vector2 uvIn1 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob1 * 2.2f, 0.72f) : new Vector2(sGlob1, 0f);
                    Vector2 uvBd0 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob0 * 2.2f, 0.98f) : new Vector2(sGlob0, 0.5f);
                    Vector2 uvBd1 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob1 * 2.2f, 0.98f) : new Vector2(sGlob1, 0.5f);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(in0, 0f), bi0, uvIn0);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(in1, 0f), bi1, uvIn1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p1, 0f), bo1, uvBd1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(in0, 0f), bi0, uvIn0);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p1, 0f), bo1, uvBd1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p0, 0f), bo0, uvBd0);

                    // --- EL LABIO: el rim luminoso que se apaga afuera ---
                    Color lo0 = Pre(TonoPluma(hue0, mezclaDia, 0.42f + rimBlanco, in estilo), brBorde * 1.05f * fuerza * fund * respiracion);
                    Color lo1 = Pre(TonoPluma(hue1, mezclaDia, 0.42f + rimBlanco, in estilo), brBorde * 1.05f * fuerza * fund * respiracion);
                    Color lx0 = Pre(TonoPluma(hue0, mezclaDia, 0.42f + rimBlanco, in estilo), 0f);
                    Color lx1 = Pre(TonoPluma(hue1, mezclaDia, 0.42f + rimBlanco, in estilo), 0f);
                    Vector2 uvLx0 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob0 * 2.2f, 1.05f) : new Vector2(sGlob0, 0.75f);
                    Vector2 uvLx1 = flujo ? new Vector2(-time * flujoT + escalaU * sGlob1 * 2.2f, 1.05f) : new Vector2(sGlob1, 0.75f);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p0, 0f), lo0, uvBd0);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p1, 0f), lo1, uvBd1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(out1, 0f), lx1, uvLx1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(p0, 0f), lo0, uvBd0);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(out1, 0f), lx1, uvLx1);
                    _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(out0, 0f), lx0, uvLx0);
                }
            }
            return nVerts;
        }

        /// <summary>
        /// v6.114 — LA MEMBRANA CON RUIDO: el mismo velo y el mismo arco,
        /// pero con las coordenadas de FLUJO de la textura (la niebla del
        /// vacío CORRE por dentro del ala — la misma fórmula de flujo de
        /// EmitirCapaRuido, pasada 0). v6.115 — LA VELA + EL BORDE
        /// MAESTRO: el velo al nivel del cuerpo y LA CURVA ÚNICA por las
        /// cinco puntas con la bruma deslizándose POR EL BORDE.
        /// </summary>
        private static int EmitirMembranaRuido(int nVerts, Pluma[] plumas, float[] hues, float mezclaDia, float time, float fold, in EstiloAlas estilo, int n)
        {
            if (estilo.Membrana <= 0f || n < 4)
            {
                return nVerts;
            }
            int porLado = n / 2;
            float fuerza = estilo.Membrana;
            float flujo = (0.2f - fold * 0.08f) * estilo.RuidoFlujo;
            float escalaU = 1.7f * estilo.RuidoEscala;
            float inten = 0.46f * estilo.RuidoInten * fuerza;
            for (int lado = 0; lado < 2; lado++)
            {
                int b = lado * porLado;
                for (int i = 0; i < porLado - 1; i++)
                {
                    if (nVerts + 96 > _verts.Length)
                    {
                        break;
                    }
                    ref Pluma pA = ref plumas[b + i];
                    ref Pluma pB = ref plumas[b + i + 1];
                    float hueMedio = (hues[b + i] + hues[b + i + 1]) * 0.5f;
                    float uSemilla = hueMedio * 7.3f;

                    // --- el velo con la niebla corriendo por dentro
                    //     (v6.115: al nivel de la VELA — ×1.7 más vivo) ---
                    for (int j = 0; j < 8; j++)
                    {
                        float q0 = (float)j / 8f;
                        float q1 = (float)(j + 1) / 8f;
                        float ua0 = (0f - time) * flujo + uSemilla + escalaU * q0;
                        float ua1 = (0f - time) * flujo + uSemilla + escalaU * q1;
                        Color c0 = Pre(TonoPluma(hueMedio + estilo.HueLargo * q0, mezclaDia, 0.05f, in estilo), inten * 0.95f);
                        Color c1 = Pre(TonoPluma(hueMedio + estilo.HueLargo * q1, mezclaDia, 0.05f, in estilo), inten * 0.95f);
                        Vector2 a0 = pA.Eje[j];
                        Vector2 a1 = pA.Eje[j + 1];
                        Vector2 b0 = pB.Eje[j];
                        Vector2 b1 = pB.Eje[j + 1];
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a0, 0f), c0, new Vector2(ua0, uSemilla * 0.031f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b0, 0f), c0, new Vector2(ua0, uSemilla * 0.031f + 0.6f * estilo.RuidoEscala));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b1, 0f), c1, new Vector2(ua1, uSemilla * 0.031f + 0.6f * estilo.RuidoEscala));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a0, 0f), c0, new Vector2(ua0, uSemilla * 0.031f));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(b1, 0f), c1, new Vector2(ua1, uSemilla * 0.031f + 0.6f * estilo.RuidoEscala));
                        _verts[nVerts++] = new VertexPositionColorTexture(new Vector3(a1, 0f), c1, new Vector2(ua1, uSemilla * 0.031f));
                    }
                }
            }
            // v6.115 — EL BORDE MAESTRO con la niebla corriendo por el
            // borde (un SOLO flujo continuo a lo largo de TODA la curva —
            // la continuidad del ruido refuerza la continuidad del borde).
            return EmitirBordeMaestro(nVerts, plumas, hues, _fundidos, mezclaDia, time, flujo: true, fold, in estilo, n);
        }

        private static Color Pre(Color c, float f)
        {
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((int)(byte)(int)((float)(int)(c).R * f + 0.5f), (int)(byte)(int)((float)(int)(c).G * f + 0.5f), (int)(byte)(int)((float)(int)(c).B * f + 0.5f), 255);
        }

        // v6.113 — LA SOBRECARGA DE LA AURORA MURIÓ con la purga de las alas
        // (la De(1) del estilo muerto): TODO dibujado pasa por el catálogo
        // vivo — el halo llama SIEMPRE a la de CINCO argumentos.

        public static bool Dibujar(Player pl, in EstiloAlas estilo, float spread, float flap, float mezclaDia)
        {
            //IL_0072: Unknown result type (might be due to invalid IL or missing references)
            //IL_0077: Unknown result type (might be due to invalid IL or missing references)
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0081: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_0091: Unknown result type (might be due to invalid IL or missing references)
            //IL_0096: Unknown result type (might be due to invalid IL or missing references)
            //IL_009b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0102: Unknown result type (might be due to invalid IL or missing references)
            //IL_010d: Unknown result type (might be due to invalid IL or missing references)
            //IL_010f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0111: Unknown result type (might be due to invalid IL or missing references)
            //IL_0113: Unknown result type (might be due to invalid IL or missing references)
            //IL_011d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0123: Unknown result type (might be due to invalid IL or missing references)
            //IL_0125: Unknown result type (might be due to invalid IL or missing references)
            //IL_012a: Unknown result type (might be due to invalid IL or missing references)
            //IL_012f: Unknown result type (might be due to invalid IL or missing references)
            //IL_021b: Unknown result type (might be due to invalid IL or missing references)
            //IL_021d: Unknown result type (might be due to invalid IL or missing references)
            //IL_021f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0221: Unknown result type (might be due to invalid IL or missing references)
            //IL_022b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0231: Unknown result type (might be due to invalid IL or missing references)
            //IL_0233: Unknown result type (might be due to invalid IL or missing references)
            //IL_0238: Unknown result type (might be due to invalid IL or missing references)
            //IL_0217: Unknown result type (might be due to invalid IL or missing references)
            //IL_023d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0188: Unknown result type (might be due to invalid IL or missing references)
            if (Main.netMode == 2 || pl == null || pl.dead || spread <= 0.02f)
            {
                return false;
            }
            if (Main.gameMenu)
            {
                return false;
            }
            try
            {
                _geoRunasValida = false;
                GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
                if (!AsegurarEfectos(graphicsDevice, estilo.Ruido))
                {
                    return false;
                }
                float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
                float gravDir = pl.gravDir;
                float fold = 1f - spread;
                float fullRotation = pl.fullRotation;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Vector2 val = pl.MountedCenter - Main.screenPosition;
                    Vector2 val2 = val + new Vector2(0f, -4f * gravDir);
                    Pluma[] plumas = _plumas;
                    float[] hues = _hues;
                    float[] fundidos = _fundidos;
                    float num = flap * estilo.HueTiempo;
                    int num2 = ((estilo.Plumas < 2) ? 2 : ((estilo.Plumas > 7) ? 7 : estilo.Plumas));
                    int num3 = 0;
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 val3 = val2 + new Vector2((float)i * estilo.AnclajeSep, 0f);
                        if (fullRotation != 0f)
                        {
                            val3 = val + Utils.RotatedBy(val3 - val, (double)fullRotation, default(Vector2));
                        }
                        for (int j = 0; j < num2; j++)
                        {
                            // v6.56.4 — EL CENTELLEO SIMÉTRICO: la semilla por
                            // lado ((i+2)*31) hacía brillar más un ala que otra —
                            // el par parpadeaba DESPAREJO. Sin el término por lado,
                            // la pluma j de ambas alas centellea IGUAL.
                            float num4 = VFXCore.Hash01(((Entity)pl).whoAmI * 7 + j * 13, (int)(globalTimeWrappedHourly * 24f), 5);
                            float num5 = 0.55f + 0.45f * num4;
                            fundidos[num3] = num5 * (0.72f + 0.28f * spread);
                            plumas[num3] = ConstruirPluma(val3, j, spread, flap, gravDir, i, globalTimeWrappedHourly, in estilo, _pozosEje[num3], _pozosSemi[num3], _pozosSemi2[num3], fundidos[num3], fullRotation);
                            float num6 = (num + (float)j * estilo.HueEntre) % 1f;
                            if (num6 < 0f)
                            {
                                num6 += 1f;
                            }
                            hues[num3] = num6;
                            num3++;
                        }
                    }
                    _geoAnclaCentral = ((fullRotation != 0f) ? (val + Utils.RotatedBy(val2 - val, (double)fullRotation, default(Vector2))) : val2);
                    _geoN = num2 * 2;
                    _geoRunasValida = true;
                    // v6.129 — LA VELA ESTIRADA AL BORDE LISO (el ala lisa —
                    // la letra: «esos dientes de sierra son supuestas plumas,
                    // esas plumas las puedes eliminar»): con las tiras
                    // muertas, los ejes de las plumas quedan como ARMADURA —
                    // y la vela se ESTIRA hasta EL BORDE ENVOLVENTE: la
                    // curva que ya NO pasa por las cinco puntas del abanico
                    // (las puntas desiguales 90→52 px eran los DIENTES del
                    // contorno: la Catmull por ellas ondulaba pico-valle).
                    // El borde nuevo: la espiral del abanico INFLADA (cada
                    // punta +12 px por fuera, en su dirección VIVA) más los
                    // DOS NACIMIENTOS junto al cuerpo (la hoja cierra a 14
                    // px del anclaje): UNA CURVA CONTINUA de la espalda a la
                    // cúspide y de la cúspide a la punta tendida — y las
                    // runas coronan ese borde en las direcciones de las
                    // plumas. (Los pozos de 14 ya soportan los 7 ejes por
                    // lado: 5 plumas + 2 nacimientos.)
                    if (estilo.Lisas)
                    {
                        _geoN = EstirarVelaLisa(plumas, hues, fundidos, val, val2,
                            num2, fullRotation, gravDir, in estilo);
                    }
                    // v6.131 — EL N DE LOS EMISORES (la segunda mitad del
                    // bug de «solo se ve el ala izquierda»): la vela lisa
                    // estirada vive en _geoN ejes (7 por lado — 5 plumas +
                    // 2 nacimientos), pero los emisores recibían el n del
                    // ABANICO VIEJO (10): el "lado 1" emparejaba ejes del
                    // ala IZQUIERDA con stubs del ala DERECHA a través de
                    // la espalda — el bloque rectangular de la captura.
                    // AHORA el conteo viaja CON la geometría: en el ala
                    // lisa los emisores y el borde leen los 7 ejes por
                    // lado de la vela estirada.
                    int nEmisores = estilo.Lisas ? _geoN : num2 * 2;
                    // v6.56.6 — EL BARRIDO DEL HEREDERO MUERE ENTERO (la letra:
                    // «no están simétricas y siguen teniendo esa ligera rotación
                    // en el sentido contrario que no deben tener»). El sim de la
                    // casa (/home/z/sim/sim_alas_v6566.py, puerto 1:1) lo cazó:
                    // UNA ROTACIÓN RÍGIDA NO CONMUTA CON EL ESPEJO —
                    // Mirror(Rot(θ)) = Rot(−θ) espejado: al girar TODO el
                    // enjambre −barrido antes de espejar, el ala izquierda dejaba
                    // de ser el espejo de la derecha (Δ 12.0 px en reposo, 42.6 px
                    // a plena velocidad — el par se leía TORCIDO: «no están
                    // simétricas»). Y el término 0.4 fijo dejaba un RAKE de
                    // +5.13° incluso PARADO — «ESA ligera rotación en el sentido
                    // contrario» que el usuario ve desde la v6.50. Tampoco puede
                    // ser una traslación (Mirror(x+d) = x−d: el par quedaría
                    // DESCENTRADO — la queja original «no están bien centradas»).
                    // LA RESPUESTA AL MOVIMIENTO YA EXISTE y es 100 % simétrica:
                    // Spread (las alas se ABREN con la velocidad — el halo mide
                    // |vel|) y Flap (el aleteo se acelera al moverse/volar). El
                    // barrido era redundancia dañina. Validado: Δ par = 0.0000 px
                    // en reposo/vuelo/backpedal × ambas direcciones (VLM: par
                    // mariposa perfecto, sin rotaciones espurias).
                    // v6.56.3 — EL ESPEJO DEL HEREDERO (la letra: «cuando el
                    // personaje se mueve derecha o izquierda las alas siguen
                    // en la misma posición»): la reconstrucción v6.56.0 perdió
                    // el barrido direccional de la v6.52.2 (Barrido/BarridoAtras
                    // quedaron código muerto — v6.56.4 lo revive ARRIBA). Aquí
                    // no hay SpriteBatch que voltear — el espejo es GEOMÉTRICO:
                    // P → (2·cx − P.x, P.y) sobre el centro del jugador.
                    if (((Entity)pl).direction < 0)
                    {
                        float cx2 = val.X;
                        // v6.131 — EL ESPEJO RECORRE LA GEOMETRÍA VIVA: en
                        // el ala lisa son _geoN ejes (14 — con los DOS
                        // nacimientos por lado), no los 10 del abanico
                        // viejo: mirando a la izquierda, los 4 ejes que
                        // faltaban quedaban SIN espejar — la vela del ala
                        // derecha nacía en el lado del cuerpo equivocado
                        // (mitad del bug «solo se ve la izquierda»).
                        int nEspejo = estilo.Lisas ? _geoN : num3;
                        for (int m = 0; m < nEspejo; m++)
                        {
                            Vector2[] eje = _pozosEje[m];
                            for (int k2 = 0; k2 < eje.Length; k2++)
                            {
                                eje[k2].X = 2f * cx2 - eje[k2].X;
                            }
                            // v6.56.4 — EL ESPEJO COMPLETO: solo negar X INVIERTE
                            // el filo/lomo — la perpendicular se recalcula sobre
                            // puntos espejados (perp(Mirror(T)) = −Mirror(perp(T)))
                            // y el +borde caía del lado opuesto: mirando a la
                            // izquierda cada pluma NO era el espejo de sí misma
                            // mirando a la derecha (Δ ≈ 2·SemiMas = 50-68 px).
                            // Con la PERMUTA, el filo conserva su lado anatómico
                            // bajo el espejo (girar == espejo exacto, Δ 0.000px
                            // en el sim).
                            float[] semiMas = _pozosSemi[m];
                            float[] semiMenos = _pozosSemi2[m];
                            for (int k3 = 0; k3 < semiMas.Length; k3++)
                            {
                                float intercambio = semiMas[k3];
                                semiMas[k3] = semiMenos[k3];
                                semiMenos[k3] = intercambio;
                            }
                        }
                        _geoAnclaCentral.X = 2f * cx2 - _geoAnclaCentral.X;
                    }
                    int num7 = 0;
                    // v6.129 — LAS PLUMAS MUEREN EN EL ALA LISA (la letra:
                    // «esos dientes de sierra son supuestas plumas, esas
                    // plumas las puedes eliminar»): las CINCO TIRAS radiales
                    // — las capas sólidas (núcleo, cuerpo, halo) y SU niebla
                    // — ERAN los dientes de sierra (cinco rayos brillantes
                    // convergiendo en cinco puntas: pico-rayo-valle-rayo-
                    // pico). Para el estilo Liso NO se emiten: el ala ES LA
                    // VELA (la superficie continua de la raíz al borde
                    // maestro) y la silueta pertenece ENTERA a la curva C1.
                    // Las plumas viven SOLO como armadura invisible: sus
                    // ejes alimentan la vela, el borde y las runas.
                    if (!estilo.Lisas)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            num7 = EmitirCapa(num7, plumas, hues, fundidos, mezclaDia, k, in estilo, nEmisores);
                        }
                    }
                    // v6.114 — LA MEMBRANA (la cura de la sierra): el velo
                    // entre plumas y el arco exterior entran al MISMO lote
                    // de las capas sólidas — con la mezcla aditiva el orden
                    // es irrelevante y las plumas siguen leyéndose encima.
                    num7 = EmitirMembrana(num7, plumas, hues, fundidos, mezclaDia, in estilo, nEmisores);
                    if (num7 >= 3)
                    {
                        Volcar(graphicsDevice, _fxPlano, num7, textura: false);
                    }
                    if (!estilo.Lisas)
                    {
                        num7 = EmitirCapaRuido(0, plumas, hues, mezclaDia, globalTimeWrappedHourly, 0, fold, in estilo, nEmisores);
                    }
                    // v6.114 — y su niebla: el mismo velo/arco con el flujo
                    // de la textura — la bruma CORRE por dentro del ala.
                    // v6.129 — en el ala lisa es LA ÚNICA niebla (la niebla
                    // de las tiras murió con ellas): la superficie vive.
                    num7 = EmitirMembranaRuido(num7, plumas, hues, mezclaDia, globalTimeWrappedHourly, fold, in estilo, nEmisores);
                    if (num7 >= 3)
                    {
                        Volcar(graphicsDevice, _fxTextura, num7, textura: true);
                    }
                    if (!estilo.Lisas)
                    {
                        num7 = EmitirCapaRuido(0, plumas, hues, mezclaDia, globalTimeWrappedHourly, 1, fold, in estilo, nEmisores);
                        if (num7 >= 3)
                        {
                            Volcar(graphicsDevice, _fxTextura, num7, textura: true);
                        }
                    }
                }
                finally
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                return true;
            }
            catch
            {
                try
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                catch
                {
                }
                return false;
            }
        }

        public static bool DibujarRunas(Player pl, in EstiloAlas estilo, float spread, float flap)
        {
            //IL_0063: Unknown result type (might be due to invalid IL or missing references)
            //IL_0068: Unknown result type (might be due to invalid IL or missing references)
            //IL_0069: Unknown result type (might be due to invalid IL or missing references)
            //IL_006e: Unknown result type (might be due to invalid IL or missing references)
            //IL_007d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0082: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_0088: Unknown result type (might be due to invalid IL or missing references)
            //IL_0097: Unknown result type (might be due to invalid IL or missing references)
            //IL_009c: Unknown result type (might be due to invalid IL or missing references)
            //IL_009d: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_010f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0114: Unknown result type (might be due to invalid IL or missing references)
            //IL_0127: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01de: Unknown result type (might be due to invalid IL or missing references)
            //IL_025a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0273: Unknown result type (might be due to invalid IL or missing references)
            //IL_027d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0282: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_02db: Unknown result type (might be due to invalid IL or missing references)
            //IL_02dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_021b: Unknown result type (might be due to invalid IL or missing references)
            //IL_021d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0300: Unknown result type (might be due to invalid IL or missing references)
            //IL_0302: Unknown result type (might be due to invalid IL or missing references)
            //IL_031f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0321: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fe: Unknown result type (might be due to invalid IL or missing references)
            if (Main.netMode == 2 || pl == null || pl.dead || spread <= 0.02f)
            {
                return false;
            }
            if (Main.gameMenu || !_geoRunasValida || estilo.Runas == 0 || _geoN <= 0)
            {
                return false;
            }
            try
            {
                Vector2[][] array;
                Color val;
                Color val2;
                float num;
                switch (estilo.Runas)
                {
                case 1:
                    array = SigiloLib.RunasSolares;
                    val = SigiloLib.CuerpoOro;
                    val2 = SigiloLib.PuntaOro;
                    num = 0.26f;
                    break;
                case 2:
                    array = OrbitaLib.RunasVacio;
                    val = OrbitaLib.RunaVioleta;
                    val2 = OrbitaLib.RunaVioletaTip;
                    num = -0.3f;
                    break;
                case 3:
                    array = OrbitaLib.RunasAbismo;
                    val = DefinitivasLib.Ambar;
                    val2 = DefinitivasLib.Oro;
                    num = 0.22f;
                    break;
                default:
                    array = SigiloLib.RunasCorona;
                    val = DefinitivasLib.Oro;
                    val2 = DefinitivasLib.BlancoCalido;
                    num = 0.3f;
                    break;
                }
                float num2 = ((estilo.RunasInten <= 0f) ? 1f : estilo.RunasInten);
                float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
                float num3 = MathHelper.Clamp(spread, 0f, 1f) * num2;
                int num4 = Math.Max(_geoN / 2, 1);
                VFXCore.Begin();
                try
                {
                    Vector2 screenPosition = Main.screenPosition;
                    // v6.56.5 — LA AUDITORÍA C (MEDIO-1): EL GIRO DE RUNAS
                    // ESPEJADO, LA SEGUNDA VUELTA. La .56.4 añadió espejoLado
                    // (−1 izquierda / +1 derecha) — pero con Plumas IMPAR (las
                    // 4 alas rúnicas tienen 5) la paridad del índice YA se
                    // auto-invierte entre gemelas (parity(n4+j) = −parity(j))
                    // y el factor EXTRA dejaba a ambas girando AL MISMO
                    // SENTIDO — y un espejo geométrico INVIERTE el sentido de
                    // giro. La .56.3 (sin factor) era la correcta para impar;
                    // la .56.4 arregló par y rompió impar. La cuenta exacta:
                    // L_B = −(−1)^num4 con L_A = +1 — impar: ambas +1 (la
                    // paridad hace el trabajo); par: +1/−1. Además: el giro
                    // viaja con direction (al mirar a la izquierda TODO el
                    // enjambre se espeja — el glifo también, o las runas no
                    // eran el espejo de sí mismas) y las gemelas COMPARTEN
                    // semilla de fase, centelleo y glifo (antes el índice
                    // crudo i hacía que las plumas gemelas centellearan y
                    // giraran distinto — asimetría visible del par).
                    float dirGiro = (float)((Entity)pl).direction;
                    // v6.118 — LAS RUNAS DE LA PUNTA: el nodo del glifo.
                    // v6.122 lo escondió un nodo adentro (el 7) con la
                    // escala serena — y el usuario lo vio FLOTANDO EN EL
                    // CENTRO de cada ala («se ven raras con algo en el
                    // centro de cada ala», v6.124 — y ya lo había echado
                    // de menos en la v6.120: «perdio toda la estetica de
                    // runas que tenia la version original»). LA AUTOPSIA
                    // de los dientes de sierra cerró el caso: la única
                    // causa PROBADA fue el RUIDO A ANCHO COMPLETO (la
                    // cura del inset incondicional de la v6.123 — viva),
                    // no el glifo. v6.124 — LAS RUNAS VUELVEN A LAS
                    // PUNTAS (nodo 8, la escala completa de la versión
                    // original 1.45+0.35·n): el glifo CORONA la punta de
                    // la pluma, donde nació.
                    int nodoRuna = 8;
                    for (int i = 0; i < _geoN; i++)
                    {
                        // v6.129 — EN EL ALA LISA los NACIMIENTOS no llevan
                        // runa (la hoja cierra junto al cuerpo: el glifo ahí
                        // cabalgaría la espalda) — las runas coronan el borde
                        // en las DIRECCIONES DE LAS PLUMAS (el índice por lado
                        // es [nacimiento, plumas…, nacimiento]: el 0 y el
                        // último son los cierres).
                        if (estilo.Lisas)
                        {
                            int porLadoRuna = _geoN / 2;
                            int kEnLado = i % porLadoRuna;
                            if (kEnLado == 0 || kEnLado == porLadoRuna - 1)
                                continue;
                        }
                        Vector2 val3 = _pozosEje[i][nodoRuna];
                        // v6.127 — LA RUNA CORONA EL BORDILLO DESDE DENTRO
                        // (la cura de la sierra, acto IV): la runa nacía
                        // CENTRADA en el nodo 8 — el punto que está SOBRE la
                        // curva del borde — y su glifo (~12-13 px de medio
                        // alto) SOBRESALÍA fuera de la curva: cinco bultos
                        // brillantes cabalgando el borde liso = cinco
                        // dientes más. Con vela (Membrana > 0) la runa se
                        // ATRASA 9 px por el radio (vive en la banda
                        // luminosa, coronando la punta desde dentro) y su
                        // resplandor se recoge ×0.75 — la escala del glifo
                        // NO se toca (la letra de la v6.124: escala completa).
                        float glowK = 1f;
                        if (estilo.Membrana > 0f)
                        {
                            Vector2 radial = val3 - _geoAnclaCentral;
                            float rl = radial.Length();
                            if (rl > 0.001f)
                            {
                                val3 -= radial / rl * 9f;
                            }
                            glowK = 0.75f;
                        }
                        int gemela = i % num4;
                        float num5 = VFXCore.Hash01(((Entity)pl).whoAmI * 7 + gemela, 3, 5) * ((float)Math.PI * 2f);
                        float num6 = 0.55f + 0.45f * MathF.Sin(globalTimeWrappedHourly * 2.3f + num5 + flap * 0.35f);
                        num6 *= 0.8f + 0.2f * VFXCore.Hash01(((Entity)pl).whoAmI * 13 + gemela, (int)(globalTimeWrappedHourly * 12f), 17);
                        float num7 = (float)gemela / (float)(num4 - 1);
                        // v6.124 — LA ESCALA COMPLETA DE LA VERSIÓN
                        // ORIGINAL (1.45+0.35·n): la serena de la v6.122
                        // (1.26+0.28·n) empequeñecía al glifo — el usuario
                        // la vio como runas APAGADAS y un «algo» raro en
                        // el centro del ala. La runa CORONA la punta con
                        // todo su tamaño.
                        float escala = 1.45f + 0.35f * num7;
                        Color cuerpo = val;
                        Color punta = val2;
                        if (estilo.Runas == 4)
                        {
                            cuerpo = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f + (float)gemela / (float)num4);
                        }
                        float espejoLado = ((i < num4) || ((num4 & 1) == 1)) ? 1f : (-1f);
                    SigiloLib.Runa(val3 + screenPosition, (globalTimeWrappedHourly * 0.35f * ((i % 2 == 0) ? 1f : (-1f)) * espejoLado + num5) * dirGiro, array, (gemela * 3 + ((Entity)pl).whoAmI) % array.Length, escala, globalTimeWrappedHourly, num5, cuerpo, punta, num3 * num6, glowK: glowK);
                    }
                    for (int j = 0; j < 8; j++)
                    {
                        // v6.56.5 — LA AUDITORÍA C: el anillo central TAMBIÉN
                        // se espeja al mirar a la izquierda: π−θ es el espejo X
                        // de θ (cos(π−θ) = −cos θ), así el orbital REVERSA con
                        // la dirección (un espejo invierte el sentido de giro)
                        // y las posiciones estáticas quedan espejadas — el
                        // giro del glifo cae exacto (−θ−π/2 ≡ π−θ+π/2, mod 2π).
                        float num8 = ((float)j / 8f * ((float)Math.PI * 2f) + globalTimeWrappedHourly * num) * dirGiro + MathHelper.Pi * (1f - dirGiro) * 0.5f;
                        Vector2 val4 = _geoAnclaCentral + new Vector2(MathF.Cos(num8), MathF.Sin(num8) * 0.86f) * 46f;
                        float fase = VFXCore.Hash01(((Entity)pl).whoAmI * 11 + j, 5, 7) * ((float)Math.PI * 2f);
                        float brillo = num3 * (0.42f + 0.3f * MathF.Sin(globalTimeWrappedHourly * 2f + (float)j * 1.3f)) * (0.55f + 0.45f * spread);
                        Color cuerpo2 = val;
                        Color punta2 = val2;
                        if (estilo.Runas == 4)
                        {
                            cuerpo2 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f + (float)j / 8f);
                        }
                        SigiloLib.Runa(val4 + screenPosition, num8 + (float)Math.PI / 2f, array, j % array.Length, 1.05f, globalTimeWrappedHourly, fase, cuerpo2, punta2, brillo, perla: false);
                    }
                    VFXCore.FlushAdditive();
                }
                finally
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                return true;
            }
            catch
            {
                try
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                catch
                {
                }
                return false;
            }
        }

        private static bool AsegurarEfectos(GraphicsDevice gd, string ruido)
        {
            //IL_0008: Unknown result type (might be due to invalid IL or missing references)
            //IL_000d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0014: Unknown result type (might be due to invalid IL or missing references)
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0022: Unknown result type (might be due to invalid IL or missing references)
            //IL_0032: Expected O, but got Unknown
            //IL_003a: Unknown result type (might be due to invalid IL or missing references)
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0054: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Expected O, but got Unknown
            if (_fxPlano == null)
            {
                _fxPlano = new BasicEffect(gd)
                {
                    LightingEnabled = false,
                    VertexColorEnabled = true,
                    TextureEnabled = false,
                    Alpha = 1f
                };
            }
            if (_fxTextura == null)
            {
                _fxTextura = new BasicEffect(gd)
                {
                    LightingEnabled = false,
                    VertexColorEnabled = true,
                    TextureEnabled = true,
                    Alpha = 1f
                };
            }
            if (_ruido == null || ((GraphicsResource)_ruido).IsDisposed || ((GraphicsResource)_ruido).Tag as string != ruido)
            {
                if (!_ruidos.TryGetValue(ruido, out var value) || value == null || ((GraphicsResource)value).IsDisposed)
                {
                    if (_texturaProboFallar)
                    {
                        return false;
                    }
                    try
                    {
                        value = ModContent.Request<Texture2D>(ruido, (AssetRequestMode)1).Value;
                        _ruidos[ruido] = value;
                    }
                    catch
                    {
                        _texturaProboFallar = true;
                        return false;
                    }
                }
                _ruido = value;
                ((GraphicsResource)_ruido).Tag = ruido;
            }
            _fxTextura.Texture = _ruido;
            return true;
        }

        private static void Volcar(GraphicsDevice gd, BasicEffect fx, int nVerts, bool textura)
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_003c: Unknown result type (might be due to invalid IL or missing references)
            fx.World = Main.GameViewMatrix.TransformationMatrix;
            fx.View = Matrix.Identity;
            fx.Projection = Matrix.CreateOrthographicOffCenter(0f, (float)Main.screenWidth, (float)Main.screenHeight, 0f, 0f, -1f);
            gd.BlendState = _pantalla;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            if (textura)
            {
                gd.SamplerStates[0] = SamplerState.LinearWrap;
            }
            foreach (EffectPass pass in ((Effect)fx).CurrentTechnique.Passes)
            {
                pass.Apply();
                int val = _verts.Length / 3 * 3;
                int num;
                for (int i = 0; i < nVerts; i += num)
                {
                    num = Math.Min(nVerts - i, val);
                    int num2 = num / 3;
                    if (num2 > 0)
                    {
                        gd.DrawUserPrimitives<VertexPositionColorTexture>((PrimitiveType)0, _verts, i, num2);
                    }
                }
            }
        }

        public static void Reiniciar()
        {
            BasicEffect plano = _fxPlano;
            BasicEffect textura = _fxTextura;
            _fxPlano = null;
            _fxTextura = null;
            _ruido = null;
            _ruidos.Clear();
            // v6.56.1 — EL LATCH ETERNO: si UNA carga de textura fallaba
            // una vez, las alas de TODOS los estilos dejaban de intentar
            // cargar hasta reiniciar el juego. La descarga lo perdona.
            _texturaProboFallar = false;
            if (plano == null && textura == null)
            {
                return;
            }
            Main.QueueMainThreadAction((Action)delegate
            {
                try
                {
                    BasicEffect obj = plano;
                    if (obj != null)
                    {
                        ((GraphicsResource)obj).Dispose();
                    }
                }
                catch
                {
                }
                try
                {
                    BasicEffect obj3 = textura;
                    if (obj3 != null)
                    {
                        ((GraphicsResource)obj3).Dispose();
                    }
                }
                catch
                {
                }
            });
        }
    }
}
