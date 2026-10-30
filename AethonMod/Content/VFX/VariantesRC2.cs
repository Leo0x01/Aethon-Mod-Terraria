using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.56.6 — LAS VARIANTES DEL RAYO CONCENTRADO 2 (la letra del
    //  usuario: «quiero que hagas otra variantes del rayo concentrado 2,
    //  al menos 9 variantes mas con diferentes efectos y tecnicas vfx,
    //  ya sabes, analiza las librerias para buscar efectos»).
    //
    //  NUEVE VARIANTES, cada una con SU TÉCNICA sacada de las librerías
    //  de la casa (el catálogo de 33 archivos de VFX/ — la auditoría de
    //  la .56.6 las recorrió para elegir):
    //    1 · PRISMA — el haz se parte en ROJO y CIAN (la aberración
    //        cromática del camino GPU: HazConcentradoLib.AberracionCromatica)
    //        y su gradiente es el ESPECTRO completo (el reloj arcoíris).
    //    2 · ECLIPSE — el haz VIOLETA OSCURO exhala BRUMA NEGRA por el
    //        conducto (SombrasLib.Bruma — la firma del grimorio).
    //    3 · TORMENTA — rayos que REPTAN por el haz (StormLib.Bolt, el
    //        estándar de la casa) en azul eléctrico.
    //    4 · SOLAR — lenguas de FUEGO solar brotan a lo largo
    //        (PyraLib.Flame con la rampa SolarFire).
    //    5 · VÓRTICE — nebulosas cian-violeta orbitan el conducto
    //        (NebulaLib.Nube — el gas circula, no oscila).
    //    6 · ESPECTRO — el haz tiene ECOS: dos fajas fantasma desfasadas
    //        que lo acompañan (EspectroLib es la inspiración; la técnica
    //        es la faja espejada a mediaalpha).
    //    7 · SIERPE — el EJE del conducto ONDULA como una serpiente
    //        (la técnica verlet de SierpesLib, destilada en la fase del
    //        eje — vive en el PreDraw del RC2).
    //    8 · CUÁNTICO — el haz se RAMIFICA en conductos cortos
    //        (FajaTexturadaEnLote sobre ejes derivados — el ForkTree de
    //        StormLib hecho faja).
    //    9 · NOVA — anillos de PULSO viajan por el conducto hacia
    //        afuera (OndaLib.Pulse — la onda que camina).
    //
    //  LA CONDUCTA ES UNA (la del RC2: carga 300 · láser 800 · temblor
    //  · soles rúnicos): cada variante cambia LA PIEL, no el alma.
    // ======================================================================
    public static class VariantesRC2
    {
        public const int Total = 10;   // 0 = el original + 9 variantes

        // ------------------------------------------------------------------
        //  EL GRADIENTE DE CADA VARIANTE — la identidad cromática que tiñe
        //  la tira, la boca, la luz y las capas. t en vueltas (el color
        //  VIAJA a lo largo del haz — la firma de la casa).
        // ------------------------------------------------------------------
        public static Color Gradiente(int variante, float t)
        {
            t -= MathF.Floor(t);
            switch (variante)
            {
                case 1:  // PRISMA — el espectro completo
                    return RuedaHue(t);
                case 2:  // ECLIPSE — violeta oscuro ↔ lila ceniza
                    return Color.Lerp(new Color(64, 26, 96), new Color(168, 120, 216), Tri(t));
                case 3:  // TORMENTA — azul eléctrico ↔ blanco frío
                    return Color.Lerp(new Color(64, 128, 255), new Color(214, 236, 255), Tri(t));
                case 4:  // SOLAR — oro ↔ brasa
                    return Color.Lerp(new Color(255, 196, 64), new Color(255, 96, 24), Tri(t));
                case 5:  // VÓRTICE — cian ↔ violeta profundo
                    return Color.Lerp(new Color(64, 226, 232), new Color(148, 72, 255), Tri(t));
                case 6:  // ESPECTRO — verde pálido ↔ blanco fantasma
                    return Color.Lerp(new Color(142, 224, 176), new Color(226, 246, 236), Tri(t));
                case 7:  // SIERPE — ámbar ↔ oro claro
                    return Color.Lerp(new Color(255, 176, 66), new Color(255, 234, 176), Tri(t));
                case 8:  // CUÁNTICO — magenta ↔ cian (la dualidad)
                    return Color.Lerp(new Color(255, 72, 200), new Color(72, 224, 255), Tri(t));
                case 9:  // NOVA — rosa nova ↔ blanco dorado
                    return Color.Lerp(new Color(255, 128, 196), new Color(255, 238, 208), Tri(t));
                default: // 0 — EL JUICIO (el original, intocable)
                    return DefinitivasLib.GradienteJuicio(t);
            }
        }

        private static float Tri(float t) => 1f - MathF.Abs(1f - 2f * t);

        // LOS POZOS (la ley de la casa: cero-GC — nada de new por frame)
        private static readonly Vector2[] _pozoMundo64 = new Vector2[64];
        private static readonly Vector2[] _pozoEco64 = new Vector2[64];
        private static readonly Vector2[] _pozoRama10 = new Vector2[10];
        private static readonly float[] _pozoRamaSemis10 = new float[10];

        /// <summary>La rueda de matiz HSV (para el PRISMA).</summary>
        private static Color RuedaHue(float h)
        {
            h *= 6f;
            float f = h - MathF.Floor(h);
            return h switch
            {
                < 1f => new Color(255, (int)(f * 255f), 0),
                < 2f => new Color((int)((1f - f) * 255f), 255, 0),
                < 3f => new Color(0, 255, (int)(f * 255f)),
                < 4f => new Color(0, (int)((1f - f) * 255f), 255),
                < 5f => new Color((int)(f * 255f), 0, 255),
                _ => new Color(255, 0, (int)((1f - f) * 255f)),
            };
        }

        // ------------------------------------------------------------------
        //  LAS TÉCNICAS — reciben el eje de 64 nodos (en PANTALLA, ya
        //  restado screenPosition) y sus semianchos, más el proyectil.
        // ------------------------------------------------------------------

        /// <summary>1 · PRISMA — el haz se parte en rojo y cian: la
        /// aberración cromática del camino GPU, encima del plasma.</summary>
        public static void Prisma(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            // los puntos que recibe AberracionCromatica van en MUNDO (pozo)
            Vector2[] mundo = _pozoMundo64;
            Vector2 sp = Main.screenPosition;
            for (int i = 0; i < 64; i++) mundo[i] = pts[i] + sp;
            HazConcentradoLib.AberracionCromatica(mundo, 64, semis,
                new Color(255, 64, 64), new Color(64, 232, 255),
                1.3f, Main.GlobalTimeWrappedHourly, 0.9f);
        }

        /// <summary>2 · ECLIPSE — bruma negra-violeta exhalada por el
        /// conducto: el haz de un dios que se apaga a su paso.</summary>
        public static void Eclipse(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            Vector2 sp = Main.screenPosition;
            for (int i = 6; i < 64; i += 12)
            {
                SombrasLib.Bruma(pts[i] + sp, 60f + semis[i] * 1.4f, 0.32f,
                    Main.GlobalTimeWrappedHourly, i * 17 + 3,
                    new Vector2(0f, -0.4f));
            }
        }

        /// <summary>3 · TORMENTA — los rayos REPTAN por el conducto:
        /// arcos cortos entre nodos, parpadeando (StormLib.Bolt).</summary>
        public static void Tormenta(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            int flick = (int)(Main.GlobalTimeWrappedHourly * 60f);
            int semilla = ((Entity)rc2.Projectile).whoAmI * 13 + 7;
            for (int k = 0; k < 5; k++)
            {
                int n0 = 4 + (int)(VFXCore.Hash01(semilla, 700 + k, 7) * 50f);
                int salto = 5 + (int)(VFXCore.Hash01(semilla, 710 + k, 11) * 6f);
                int n1 = Math.Min(n0 + salto, 63);
                StormLib.Bolt(Main.spriteBatch, pts[n0], pts[n1], semilla + k * 31, flick,
                    2.6f, new Color(96, 148, 255), new Color(232, 244, 255),
                    0.55f, 6, 18f);
            }
        }

        /// <summary>4 · SOLAR — lenguas de fuego solar brotan del conducto
        /// (PyraLib.Flame, la rampa SolarFire — el fuego de la casa).</summary>
        public static void Solar(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            int semilla = ((Entity)rc2.Projectile).whoAmI * 19 + 5;
            for (int i = 5; i < 64; i += 9)
            {
                PyraLib.Flame(Main.spriteBatch, pts[i], MathF.Max(26f, semis[i] * 1.6f),
                    PyraPalettes.SolarFire, semilla + i, Main.GlobalTimeWrappedHourly,
                    intensity: 0.75f);
            }
        }

        /// <summary>5 · VÓRTICE — nebulosas orbitando el conducto: el gas
        /// CIRCULA alrededor del haz (NebulaLib.Nube — advección de curl).</summary>
        public static void Vortice(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            Vector2 sp = Main.screenPosition;
            int semilla = ((Entity)rc2.Projectile).whoAmI * 23 + 9;
            for (int i = 8; i < 64; i += 14)
            {
                NebulaLib.Nube(pts[i] + sp, MathF.Max(46f, semis[i] * 2.1f),
                    Main.GlobalTimeWrappedHourly, semilla + i,
                    new Color(72, 226, 232), new Color(148, 72, 255), 0.12f, 10);
            }
        }

        /// <summary>6 · ESPECTRO — el haz tiene ECOS: dos fajas fantasma
        /// desfasadas perpendicularmente que lo escoltan, verdes pálidos.
        /// La técnica: la misma faja texturada, desplazada y a media
        /// alpha, con la fase del flujo INVERTIDA (el eco va al revés).</summary>
        public static void Espectro(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            Texture2D carne = DefinitivasLib.TexRuido;
            if (carne == null) return;
            Vector2[] eco = _pozoEco64;
            float t = Main.GlobalTimeWrappedHourly;
            float desfase = MathF.Sin(t * 2.1f) * 26f;
            Vector2 perp = new(-MathF.Sin(rc2.Projectile.rotation), MathF.Cos(rc2.Projectile.rotation));
            for (int lado = -1; lado <= 1; lado += 2)
            {
                for (int i = 0; i < 64; i++)
                {
                    float ond = MathF.Sin(i / 63f * MathHelper.TwoPi * 1.5f + t * 2.6f * lado) * 14f;
                    eco[i] = pts[i] + perp * (desfase * lado + ond * lado);
                }
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FajaTexturadaEnLote(eco, semis,
                        DefinitivasLib.Alfa(new Color(158, 236, 192), 0.34f * realSize),
                        carne, 420f, -1.2f, t);
                }
                finally { RayoStrip.CerrarLote(); }
            }
        }

        /// <summary>8 · CUÁNTICO — el conducto se RAMIFICA: fajitas cortas
        /// que nacen de nodos hash y se desvanecen (la superposición de
        /// caminos — el ForkTree de la casa hecho faja).</summary>
        public static void Cuantico(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            Texture2D carne = DefinitivasLib.TexRuido;
            if (carne == null) return;
            Vector2[] rama = _pozoRama10;
            float[] semiR = _pozoRamaSemis10;
            float t = Main.GlobalTimeWrappedHourly;
            int semilla = ((Entity)rc2.Projectile).whoAmI * 29 + 11;
            int vivas = 3 + (int)(VFXCore.Hash01(semilla, (int)rc2.Projectile.localAI[0] / 6, 5) * 3.99f);
            RayoStrip.AbrirLote();
            try
            {
                for (int b = 0; b < vivas; b++)
                {
                    int nodo = 4 + (int)(VFXCore.Hash01(semilla, 730 + b * 7, 17) * 52f);
                    float ang = rc2.Projectile.rotation +
                                (VFXCore.Hash01(semilla, 740 + b * 7, 19) - 0.5f) * 1.5f;
                    float largo = 120f + 240f * VFXCore.Hash01(semilla, 750 + b * 7, 23);
                    float vida = (t * 0.9f + VFXCore.Hash01(semilla, 760 + b * 7, 29)) % 1f;
                    float arde = MathF.Sin(vida * MathHelper.Pi);
                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                    Vector2 origen = pts[nodo];
                    for (int i = 0; i < 10; i++)
                    {
                        float q = i / 9f;
                        rama[i] = origen + dir * (largo * q);
                        semiR[i] = MathF.Max(semis[nodo] * 0.5f * (1f - q) * arde, 0.8f);
                    }
                    RayoStrip.FajaTexturadaEnLote(rama, semiR,
                        DefinitivasLib.Alfa(Gradiente(8, vida), 0.55f * arde * realSize),
                        carne, 160f, 2.2f, t, 0.36f, 0.64f);
                }
            }
            finally { RayoStrip.CerrarLote(); }
        }

        /// <summary>9 · NOVA — anillos de pulso que VIAJAN por el conducto
        /// hacia afuera (OndaLib.Pulse: la onda que camina).</summary>
        public static void Nova(Vector2[] pts, float[] semis, RayoConcentradoDosProjectile rc2, float realSize)
        {
            int semilla = ((Entity)rc2.Projectile).whoAmI * 31 + 13;
            for (int k = 0; k < 4; k++)
            {
                float fase = (VFXCore.Hash01(semilla, 770 + k, 7)
                              + rc2.Projectile.localAI[0] * 0.004f) % 1f;
                int nodo = (int)(fase * 63f);
                float arde = MathF.Sin(MathF.Min(fase * 3.2f, 1f) * MathHelper.Pi);
                if (arde < 0.06f) continue;
                OndaLib.Pulse(Main.spriteBatch, pts[nodo], fase,
                    MathF.Max(70f, semis[nodo] * 3.2f),
                    Gradiente(9, fase), 0.42f * arde, semilla + k * 37);
            }
        }
    }
}
