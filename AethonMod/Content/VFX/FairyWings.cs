using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// FairyWings — v6.12 — LAS ALAS DE HADA DE POLVO ESTELAR.
    ///
    /// Cuatro lóbulos alargados y PUNTIAGUDOS por lado (la silueta de las
    /// hadas clásicas): dos superiores largos y dos inferiores cortos, todos
    /// de membrana DORADA translúcida con el borde ardiendo en ámbar.
    ///
    /// v6.12 — LA LECCIÓN DE VISIBILIDAD (AlphaBlend, no aditivo): cada
    /// lóbulo lleva ahora VOLUMEN (ámbar profundo sólido al 55%) debajo de
    /// la membrana luminosa y el borde arde a 0.9 — se lee como ala desde
    /// cualquier fondo, como las coronas.
    ///
    /// Su firma son los DESTELLOS: 7 chispas de polvo estelar repartidas por
    /// las alas que titilan con fases deterministas (cada una parpadea a su
    /// ritmo — el polvo estelar de un hada NUNCA está quieto) y una llovizna
    /// sutil de motas doradas que cae de los bordes.
    ///
    /// El aleteo es el de un COLIBRÍ: vibración RÁPIDA y superficial
    /// (FlapSpeedFlying 0.52 — casi 10 ciclos por segundo visible) que
    /// NUNCA cesa del todo (AlwaysFlutter: el hada vibra incluso parada).
    ///
    /// Paleta: dorado miel + blanco cálido con toques rosa pálido.
    /// </summary>
    public static class FairyWings
    {
        /// <summary>Destellos de polvo estelar por lado.</summary>
        private const int Sparkles = 7;

        /// <summary>Segmentos del borde de cada lóbulo.</summary>
        private const int RimSegs = 9;

        /// <summary>Pinta las alas de hada en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            // Vibración de colibrí: alta frecuencia, poca amplitud — y SIEMPRE
            // latiendo (el hada no para del todo ni en reposo).
            float flutter = (float)Math.Sin(ctx.FlapPhase) * (ctx.FlapAmp * 0.75f + 0.25f);
            float o = open * VFXCore.Breathe(ctx.Time, 2.6f, 0f, 0.04f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 4f, -3f * ctx.GravDir);

                // ============================================================
                //  LOS 4 LÓBULOS (silueta de hada clásica)
                // ============================================================
                for (int lobe = 0; lobe < 4; lobe++)
                {
                    // Configuración por lóbulo: (ángulo base, longitud, ancho).
                    // 0/1 = superiores (largos, arriba y afuera); 2/3 = inferiores.
                    float baseAng = lobe switch
                    {
                        0 => -1.28f,   // superior alto
                        1 => -0.88f,   // superior exterior
                        2 => -0.30f,   // inferior exterior
                        _ => 0.10f,    // inferior bajo
                    };
                    // v6.12: envergadura de hada vanilla (~30px por lóbulo mayor).
                    float len = lobe switch { 0 => 26f, 1 => 31f, 2 => 21f, _ => 16f };
                    float wid = lobe switch { 0 => 9f, 1 => 10.5f, 2 => 8.5f, _ => 6.5f };

                    // El lóbulo RETRASA su vibración con la distancia (onda
                    // continua a lo largo del ala: puntas siguiendo a la raíz).
                    float lag = lobe * 0.55f;
                    float ang = baseAng + flutter * (0.30f + 0.06f * lobe)
                                * (float)Math.Sin(ctx.FlapPhase - lag) * 1.6f;

                    // Apertura: en reposo los lóbulos se PLEGAN hacia el cuerpo
                    // (ángulos comprimidos hacia abajo).
                    float fold = MathHelper.Lerp(0.45f, 1f, o);
                    ang = -MathHelper.Lerp(-0.35f, -ang, fold);

                    float dirX = (float)Math.Cos(ang) * side;
                    float dirY = (float)Math.Sin(ang) * ctx.GravDir;

                    // --- VOLUMEN ÁMBAR (el cuerpo sólido del lóbulo) ---
                    // v6.12: media celda más ancha y alfa 0.55 — el lóbulo se
                    // lee como pétalo sólido translúcido, no como niebla.
                    for (int c = 0; c < 6; c++)
                    {
                        float t = (c + 0.5f) / 6f;
                        // Ancho del lóbulo: hinchado en el primer tercio, PUNTIAGUDO
                        // al final (perfil de hoja).
                        float profile = (float)Math.Sin(t * MathHelper.Pi) * (1f - t * 0.35f);
                        float w = wid * (0.55f + profile) * (0.5f + 0.5f * o);

                        Vector2 pos = root + new Vector2(dirX, dirY) * (len * t * fold)
                            + new Vector2(0f, flutter * 1.2f * t);
                        Vector2 memScale = new Vector2(w * 2.3f, len / 6f * 1.6f);
                        float memRot = (float)Math.Atan2(dirY, dirX);
                        Color vol = Color.Lerp(new Color(196, 112, 28),
                            new Color(232, 150, 60), t);
                        VFXCore.Quad(pos, vol * (0.55f * ctx.Alpha * (0.55f + 0.45f * o)),
                            memScale, memRot, VFXCore.SoftGlow);
                    }

                    // --- MEMBRANA LUMINOSA dorada (el brillo encima) ---
                    for (int c = 0; c < 6; c++)
                    {
                        float t = (c + 0.5f) / 6f;
                        float jitterW = (VFXCore.Hash01(side * 5 + lobe, c, 1) - 0.5f) * 0.4f;
                        float profile = (float)Math.Sin(t * MathHelper.Pi) * (1f - t * 0.35f);
                        float w = wid * (0.40f + profile * 0.6f + jitterW * 0.3f) * (0.5f + 0.5f * o);

                        Vector2 pos = root + new Vector2(dirX, dirY) * (len * t * fold)
                            + new Vector2(0f, flutter * 1.2f * t);
                        Vector2 memScale = new Vector2(w * 1.7f, len / 6f * 1.3f);
                        float memRot = (float)Math.Atan2(dirY, dirX);
                        Color mem = Color.Lerp(new Color(255, 218, 140),
                            new Color(255, 190, 120), t);
                        VFXCore.Quad(pos, mem * (0.45f * ctx.Alpha * (0.5f + 0.5f * o)),
                            memScale, memRot, VFXCore.SoftGlow);
                    }

                    // --- BORDE ámbar ARDIENDO (0.9: silueta nítida) ---
                    for (int s = 0; s <= RimSegs; s++)
                    {
                        float t = s / (float)RimSegs;
                        float profile = (float)Math.Sin(t * MathHelper.Pi);
                        float w = wid * (0.55f + profile) * (0.5f + 0.5f * o);
                        // Los DOS bordes del lóbulo (arriba y abajo del eje).
                        Vector2 axis = new Vector2(dirX, dirY);
                        Vector2 perp = new Vector2(-axis.Y, axis.X);
                        for (int b = -1; b <= 1; b += 2)
                        {
                            Vector2 pos = root + axis * (len * t * fold)
                                + perp * (w * b)
                                + new Vector2(0f, flutter * 1.2f * t);
                            Color rim = Color.Lerp(new Color(255, 240, 180),
                                new Color(255, 175, 80), t * 0.7f);
                            float tw = 0.8f + 0.2f * VFXCore.Hash01(side + lobe, s, b);
                            VFXCore.Quad(pos, rim * (0.90f * tw * ctx.Alpha * (0.55f + 0.45f * o)),
                                new Vector2(3.4f, 3.4f));
                        }
                    }

                    // La PUNTA: destello cálido.
                    Vector2 tip = root + new Vector2(dirX, dirY) * (len * fold)
                        + new Vector2(0f, flutter * 1.2f);
                    VFXCore.Quad(tip, new Color(255, 244, 200) * (0.85f * ctx.Alpha),
                        new Vector2(4.6f, 4.6f));
                }

                // ============================================================
                //  EL POLVO ESTELAR: 7 chispas titilando SOBRE las alas
                // ============================================================
                for (int k = 0; k < Sparkles; k++)
                {
                    // Cada chispa tiene su sitio fijo (determinista) cerca de un
                    // lóbulo y su PROPIO ritmo de titileo (hash de fase).
                    float u = VFXCore.Hash01(side, k, 11);
                    float v = VFXCore.Hash01(side, k, 12);
                    int lobeK = k % 4;
                    float baseAng = lobeK switch { 0 => -1.28f, 1 => -0.88f, 2 => -0.30f, _ => 0.10f };
                    float lenK = lobeK switch { 0 => 26f, 1 => 31f, 2 => 21f, _ => 16f };
                    float fold = MathHelper.Lerp(0.45f, 1f, o);
                    Vector2 axis = new Vector2((float)Math.Cos(baseAng) * side,
                        (float)Math.Sin(baseAng) * ctx.GravDir);
                    Vector2 perp = new Vector2(-axis.Y, axis.X);
                    Vector2 pos = root + axis * (lenK * u * fold) + perp * ((v - 0.5f) * 16f);

                    // Titileo: pulso agudo con pausa (como una estrella).
                    float phase = ctx.Time * (2.2f + 2.8f * v) + u * 37f;
                    float tw = (float)Math.Pow(0.5f + 0.5f * (float)Math.Sin(phase), 3.0);
                    // Solo visible ~70% del tiempo (parpadeo con pausas).
                    if (tw > 0.05f)
                    {
                        Color spark = v > 0.5f
                            ? new Color(255, 248, 220)
                            : new Color(255, 205, 240);
                        VFXCore.Quad(pos, spark * (tw * 0.95f * ctx.Alpha),
                            new Vector2(3.6f, 3.6f));
                        VFXCore.Quad(pos, spark * (tw * 0.35f * ctx.Alpha),
                            new Vector2(8.0f, 8.0f));
                    }
                }

                // El CORAZÓN del hada: punto de luz rosa-dorado en la raíz.
                float heart = 0.7f + 0.3f * (float)Math.Sin(ctx.Time * 5.5f + side);
                VFXCore.Quad(root, new Color(255, 214, 150) * (0.65f * heart * ctx.Alpha),
                    new Vector2(10f, 10f));
                VFXCore.Quad(root, Color.White * (0.7f * heart * ctx.Alpha),
                    new Vector2(4.2f, 4.2f));
            }
        }
    }
}
