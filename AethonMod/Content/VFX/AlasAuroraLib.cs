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

        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = (Blend)7,
            ColorDestinationBlend = (Blend)0,
            AlphaSourceBlend = (Blend)0,
            AlphaDestinationBlend = (Blend)0
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
                float num14 = Perfil(num13, raizSemiancho, vientre, ancho, afilada);
                float num15 = Perfil(num13, raizSemiancho, vientre2, ancho2, afilada);
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
            static float Perfil(float q, float raiz, float num18, float num20, float y)
            {
                if (q < num18)
                {
                    float num19 = q / num18;
                    return MathHelper.Lerp(raiz, num20, MathF.Pow(MathF.Sin(num19 * ((float)Math.PI / 2f)), 0.8f));
                }
                float num21 = (q - num18) / MathF.Max(1f - num18, 0.001f);
                return num20 * MathF.Pow(1f - num21, y);
            }
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
                    val9 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] + val6 * reference.SemiMas[j], 0f), val7, new Vector2(num12, num8));
                    val10 = new VertexPositionColorTexture(new Vector3(reference.Eje[j] - val6 * reference.SemiMenos[j], 0f), val7, new Vector2(num12, num9));
                    val11 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] + val6 * reference.SemiMas[j + 1], 0f), val8, new Vector2(num13, num8));
                    val12 = new VertexPositionColorTexture(new Vector3(reference.Eje[j + 1] - val6 * reference.SemiMenos[j + 1], 0f), val8, new Vector2(num13, num9));
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

        private static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            //IL_0000: Unknown result type (might be due to invalid IL or missing references)
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0003: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_000e: Unknown result type (might be due to invalid IL or missing references)
            return a + (b - a) * t;
        }

        private static Color Pre(Color c, float f)
        {
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((int)(byte)(int)((float)(int)(c).R * f + 0.5f), (int)(byte)(int)((float)(int)(c).G * f + 0.5f), (int)(byte)(int)((float)(int)(c).B * f + 0.5f), 255);
        }

        public static bool Dibujar(Player pl, float spread, float flap, float mezclaDia)
        {
            return Dibujar(pl, in AlasCatalogo.De(1), spread, flap, mezclaDia);
        }

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
                    // v6.56.4 — EL BARRIDO DEL HEREDERO (revive el campo muerto
                    // de la v6.52.2 — la letra: «cuando el personaje se mueve en
                    // una dirección, las alas no cambian la dirección junto al
                    // personaje»). El par es simétrico: el espejo solo produce
                    // la MISMA imagen — lo que faltaba es RESPUESTA DIRECCIONAL.
                    // Todo el enjambre se INCLINA hacia atrás respecto al
                    // MOVIMIENTO, calculado en el MARCO DE LA DERECHA (la
                    // velocidad se espeja con la dirección: velFrame =
                    // velX·direction — si el término de dirección entrase en la
                    // fórmula, se DOUBLE-VOLTEARÍA con el espejo y girar no
                    // cambiaría nada; el sim lo cazó: Δ 0.0px). 0.4 de reposo
                    // (el rake natural del ala plegada) + 0.6·velocidad
                    // saturada a 6 px/tick. Validado en /home/z/sim/sim_alas.py:
                    // corriendo, la punta se barre 11-13 px; girar en plena
                    // carrera la mueve 32-42 px; backpedal, 10-13 px.
                    float velFrame = ((Entity)pl).velocity.X * (float)((Entity)pl).direction;
                    float velN = MathHelper.Clamp(velFrame / 6f, -1f, 1f);
                    float barrido = estilo.Barrido * 1.6f * (0.4f + 0.6f * velN);
                    if (barrido != 0f && float.IsFinite(barrido))
                    {
                        for (int m = 0; m < num3; m++)
                        {
                            Vector2[] eje = _pozosEje[m];
                            for (int k2 = 0; k2 < eje.Length; k2++)
                            {
                                eje[k2] = val + Utils.RotatedBy(eje[k2] - val, (double)(-barrido), default(Vector2));
                            }
                        }
                        _geoAnclaCentral = val + Utils.RotatedBy(_geoAnclaCentral - val, (double)(-barrido), default(Vector2));
                    }
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
                        for (int m = 0; m < num3; m++)
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
                    for (int k = 0; k < 3; k++)
                    {
                        num7 = EmitirCapa(num7, plumas, hues, fundidos, mezclaDia, k, in estilo, num2 * 2);
                    }
                    if (num7 >= 3)
                    {
                        Volcar(graphicsDevice, _fxPlano, num7, textura: false);
                    }
                    num7 = EmitirCapaRuido(0, plumas, hues, mezclaDia, globalTimeWrappedHourly, 0, fold, in estilo, num2 * 2);
                    if (num7 >= 3)
                    {
                        Volcar(graphicsDevice, _fxTextura, num7, textura: true);
                    }
                    num7 = EmitirCapaRuido(0, plumas, hues, mezclaDia, globalTimeWrappedHourly, 1, fold, in estilo, num2 * 2);
                    if (num7 >= 3)
                    {
                        Volcar(graphicsDevice, _fxTextura, num7, textura: true);
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
                    for (int i = 0; i < _geoN; i++)
                    {
                        Vector2 val3 = _pozosEje[i][8];
                        int gemela = i % num4;
                        float num5 = VFXCore.Hash01(((Entity)pl).whoAmI * 7 + gemela, 3, 5) * ((float)Math.PI * 2f);
                        float num6 = 0.55f + 0.45f * MathF.Sin(globalTimeWrappedHourly * 2.3f + num5 + flap * 0.35f);
                        num6 *= 0.8f + 0.2f * VFXCore.Hash01(((Entity)pl).whoAmI * 13 + gemela, (int)(globalTimeWrappedHourly * 12f), 17);
                        float num7 = (float)gemela / (float)(num4 - 1);
                        float escala = 1.45f + 0.35f * num7;
                        Color cuerpo = val;
                        Color punta = val2;
                        if (estilo.Runas == 4)
                        {
                            cuerpo = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f + (float)gemela / (float)num4);
                        }
                        float espejoLado = ((i < num4) || ((num4 & 1) == 1)) ? 1f : (-1f);
                    SigiloLib.Runa(val3 + screenPosition, (globalTimeWrappedHourly * 0.35f * ((i % 2 == 0) ? 1f : (-1f)) * espejoLado + num5) * dirGiro, array, (gemela * 3 + ((Entity)pl).whoAmI) % array.Length, escala, globalTimeWrappedHourly, num5, cuerpo, punta, num3 * num6);
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
