using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AgujeroPuroRenderer — v6.150 — EL AGUJERO RÚNICO PURO, DE VUELTA A
    /// LA MAGIA (la letra: «cuando te pedi que le dieras otro estilo al
    /// agujero runico puro no te pedi que lo hicieras mas realista,
    /// estos agujeros negros runicos son runicos por se magicos, por lo
    /// tanto devuelvelo a su estado anterior pero sin el polvito ese que
    /// gira todo el tiempo, lo que tenias que hacer es meterle mas
    /// efectos mas vfx»).
    ///
    /// LA AUTOPSIA: la v6.149 lo vistió de FÍSICA (el anillo de Einstein,
    /// el disco de acreción «Gargantua» casi de canto — realismo). La
    /// letra manda: los agujeros rúnicos son MÁGICOS. EL REGRESO al
    /// estilo de la FAMILIA (el lenguaje del Cósmico, la casa desde
    /// v6.16): anillo energético OBLICUO con LAS 20 BANDAS del shader
    /// (sin(uv·20 + t·5) — la fórmula textual del script Unity del
    /// usuario), runas doradas orbitando, rayos danzantes, ecos que
    /// respiran, ondas de distorsión, aura de emisión — pero con la
    /// IDENTIDAD del Puro: la paleta VIOLETA-BLANCA del vacío puro y SU
    /// PROPIA corona de sellos.
    ///
    /// Y MÁS VFX (la letra: «lo que tenias que hacer es meterle mas
    /// efectos mas vfx»), CUATRO capas nuevas sobre la familia:
    ///   · EL SELLO DOBLE — dos aros rúnicos CONTRARROTANTES alrededor
    ///     del anillo (el círculo de conjuro de la casa, tallado con
    ///     glifos-cápsula como el SigiloLib).
    ///   · LOS FILAMENTOS DEL VACÍO — tres hebras de luz violeta que
    ///     SERPENTEAN desde el borde hacia afuera y se disuelven (la
    ///     firma del Puro: el vacío hilado).
    ///   · EL CORAZÓN PULSANTE — un núcleo blanco que LATE en el centro
    ///     del horizonte (1 Hz, el ritmo del Puro — vida donde la física
    ///     dice que no hay nada).
    ///   · LA CORONA DE DESTELLOS — picos de 4 puntas girando lento en
    ///     el plano del anillo (el brillo de los soles rúnicos de 3
    ///     anillos+ prestado al agujero).
    /// SIN POLVO: cero Dust, cero partículas de librería — todo dibujo
    /// puro de la casa (el tratamiento aprobado).
    ///
    /// CONTRATO DE LOTE: el batch llega CERRADO y queda CERRADO (el
    /// contrato del renderer del Cósmico — lo llama el PreDraw del Puro
    /// y el pase posterior a la lente).
    /// </summary>
    public static class AgujeroPuroRenderer
    {
        /// <summary>Radio del horizonte en px a escala 1 (mismo tamaño físico
        /// que el Cósmico: el arma, el daño y la lente no cambian).</summary>
        public const float SpherePx = 50f;

        // === LA GEOMETRÍA DEL ANILLO (las leyes de la familia — OrbitaLib) ===
        private const float RingA = OrbitaLib.RingA;        // semieje mayor (×R)
        private const float RingB = OrbitaLib.RingB;        // semieje menor (×R)
        private const float RingTilt = OrbitaLib.RingTilt;  // rad — la oblicuidad
        private const float RingSpin = OrbitaLib.RingSpin;  // 20°/s de la casa
        private const float BandFreq = OrbitaLib.BandFreq;  // 20 bandas
        private const float BandSpeed = OrbitaLib.BandSpeed;// 5 rad/s

        // === EL SELLO DOBLE (la capa nueva) ===
        private const float SelloRadio = 3.1f;    // ×R — el círculo de conjuro
        private const float SelloGiro = 0.14f;    // rad/s — lento y ceremonial

        // === LAS RUNAS (la familia: 8 glifos dorados) ===
        private const int RuneCount = 8;
        private const float RuneRadius = 2.55f;   // ×R
        private const float RuneOrbit = 0.10f;    // rad/s

        // === LOS FILAMENTOS DEL VACÍO (la capa nueva) ===
        private const int Filamentos = 3;

        // === LA PALETA DEL PURO (violeta-blanca — el vacío puro) ===
        private static readonly Color RingVioleta = new(168, 96, 255);   // el anillo vivo
        private static readonly Color HotWhite = new(255, 252, 255);    // BLANCO PURO (la emisión ×5)
        private static readonly Color DeepViolet = new(84, 30, 170);     // violeta profundo
        private static readonly Color BoltNucleo = new(190, 130, 255);   // rayo interior
        private static readonly Color BoltBorde = new(150, 80, 240);     // rayo del anillo
        private static readonly Color RuneOro = new(255, 214, 120);      // cuerpo de runa
        private static readonly Color RunePunta = new(255, 244, 214);    // punta de runa
        private static readonly Color NebVioleta = new(96, 44, 190);     // nebulosa violeta
        private static readonly Color NebAzul = new(60, 90, 220);        // nebulosa azul fría
        private static readonly Color AuraPura = new(140, 80, 255);      // aura de emisión
        private static readonly Color CorazonBlanco = new(255, 250, 255);// el corazón que late

        // === PINCELES (los tres genéricos de la casa) ===
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _disk;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;
        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;
        private static Texture2D Disk =>
            (_disk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  EL DRAW — contrato: batch CERRADO → CERRADO
        // ==================================================================
        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;

            try
            {
                // LA DISTORSIÓN GLOBAL DE LA FAMILIA: sin(t)·0.3 — el
                // pulso que atraviesa TODO (radio, ecos, aura).
                float distortion = (float)Math.Sin(time) * OrbitaLib.DistorsionFuerza;
                float breathe = 1f + 0.03f * distortion;
                float rr = r * breathe;

                // ============ 0. AURA OSCURA (alfa) — el vacío absorbe ============
                VFXCore.AbrirAlpha();
                Quad(Glow, center, new Vector2(7.0f * rr, 7.0f * rr), 0f,
                    new Color(8, 2, 14, 170));
                Main.spriteBatch.End();

                // ============ 1..14: TODO LO BRILLANTE (aditivo) ============
                VFXCore.AbrirAdditive();
                int flick = (int)(time * 12f);

                // --- 1. NEBULOSAS violeta/azul (el fondo del conjuro) ---
                Nebulosa(center, rr, time, seed, 0, NebVioleta);
                Nebulosa(center, rr, time, seed, 1, NebAzul);

                // --- 2. ECOS DEL ANILLO (dos fantasmas respirando) ---
                EcoAnillo(center, rr, time, 1.45f, 0.10f);
                EcoAnillo(center, rr, time, 1.85f, 0.07f);

                // --- 3. EL ANILLO ENERGÉTICO (trasero) con SUS 20 BANDAS ---
                AnilloEnergetico(center, rr, time, seed, frente: false);

                // --- 4. EL NÚCLEO negro absoluto + rim violeta ---
                Main.spriteBatch.End();
                VFXCore.AbrirAlpha();
                Quad(Disk, center, new Vector2(2.06f * rr, 2.06f * rr), 0f, Color.White);
                Main.spriteBatch.End();
                VFXCore.AbrirAdditive();
                RingQuad(center, 1.01f * rr, time * 0.1f,
                    Tint(RingVioleta, 0.55f + 0.12f * MathF.Sin(time * 2f)));

                // --- 5. EL CORAZÓN PULSANTE (la capa nueva) ---
                Corazon(center, rr, time);

                // --- 6. EL ANILLO ENERGÉTICO (delantero) ---
                AnilloEnergetico(center, rr, time, seed, frente: true);

                // --- 7. EL SELLO DOBLE CONTRARROTANTE (la capa nueva) ---
                SelloDoble(center, rr, time, seed);

                // --- 8. CORREDORES DE FOTONES (3 destellos orbitando) ---
                Corredores(center, rr, time, seed);

                // --- 9. RAYOS ELÉCTRICOS (2 danzan en el núcleo + 2 del anillo) ---
                Rayos(center, rr, time, seed, flick);

                // --- 10. LOS FILAMENTOS DEL VACÍO (la capa nueva) ---
                FilamentosDelVacio(center, rr, time, seed);

                // --- 11. LA CORONA DE DESTELLOS (la capa nueva) ---
                CoronaDestellos(center, rr, time, seed);

                // --- 12. RUNAS DORADAS (la familia) ---
                RunasDoradas(center, rr, time, seed);

                // --- 13. ONDAS DE DISTORSIÓN (2 anillos expandiéndose) ---
                Ondas(center, rr, time);

                // --- 14. AURA FINAL pulsante (la emisión de la casa) ---
                Quad(Glow, center, new Vector2(6.6f * rr, 6.6f * rr), 0f,
                    Tint(AuraPura, 0.10f + 0.05f * MathF.Sin(time * 2.4f)));
            }
            finally
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ==================================================================
        //  LAS CAPAS DE LA FAMILIA (el dialecto del Cósmico, paleta Pura)
        // ==================================================================

        private static void Nebulosa(Vector2 c, float r, float time, int seed, int idx, Color col)
        {
            float ang = time * 0.07f + idx * MathHelper.Pi;
            Vector2 pos = c + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * r * 1.9f;
            Quad(Glow, pos, new Vector2(4.6f * r, 4.6f * r), 0f, Tint(col, 0.06f + 0.02f * MathF.Sin(time + idx * 2f)));
        }

        private static void EcoAnillo(Vector2 c, float r, float time, float factor, float alfa)
        {
            float respira = 1f + 0.05f * MathF.Sin(time * BandSpeed * 0.4f);
            RingQuad(c, factor * r * respira, time * 0.05f, Tint(RingVioleta, alfa));
        }

        /// <summary>EL ANILLO ENERGÉTICO — cápsulas sobre la elipse con LAS
        /// 20 BANDAS del shader (sin(20t+5τ)·0.5+0.5 — la fórmula textual
        /// del script Unity del usuario, la ley de la casa).</summary>
        private static void AnilloEnergetico(Vector2 c, float r, float time, int seed, bool frente)
        {
            int seg = 40;
            float t0 = frente ? 0f : MathHelper.Pi;
            float base_ = frente ? 1f : 0.6f;
            for (int i = 0; i < seg; i++)
            {
                float t = t0 + (i / (float)seg) * MathHelper.Pi + time * RingSpin;
                // LA FÓRMULA DEL SHADER: sin(uv·20 + t·5) — las bandas VIAJAN
                float bandas = 0.5f + 0.5f * MathF.Sin(t * BandFreq + time * BandSpeed);
                Vector2 pos = VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
                float tx = -MathF.Sin(t) * RingA, ty = MathF.Cos(t) * RingB;
                float rot = new Vector2(
                    tx * MathF.Cos(RingTilt) - ty * MathF.Sin(RingTilt),
                    tx * MathF.Sin(RingTilt) + ty * MathF.Cos(RingTilt)).ToRotation();
                Color caliente = Color.Lerp(RingVioleta, HotWhite, bandas * bandas);
                VFXCore.Capsule(pos, 16f, 7f + 4f * bandas, rot,
                    Tint(caliente, (0.28f + 0.34f * bandas) * base_));
            }
        }

        private static void Corazon(Vector2 c, float r, float time)
        {
            // EL CORAZÓN QUE LATE (1 Hz): vida en el centro del vacío —
            // blanco puro que late y un halo que respira con él.
            float latido = 0.5f + 0.5f * MathF.Sin(time * MathHelper.TwoPi);
            float golpe = MathF.Pow(latido, 2.2f);           // el golpe seco
            Quad(Glow, c, new Vector2(0.9f * r, 0.9f * r) * (0.7f + 0.5f * golpe), 0f,
                Tint(CorazonBlanco, 0.20f + 0.30f * golpe));
            Quad(Glow, c, new Vector2(1.9f * r, 1.9f * r), 0f,
                Tint(AuraPura, 0.06f + 0.08f * golpe));
        }

        private static void SelloDoble(Vector2 c, float r, float time, int seed)
        {
            // EL SELLO DOBLE: dos aros CONTRARROTANTES con glifos tallados
            // (el círculo de conjuro de la casa — cápsulas como el SigiloLib).
            for (int aro = 0; aro < 2; aro++)
            {
                float dir = aro == 0 ? 1f : -1f;
                float radio = SelloRadio * r * (aro == 0 ? 1f : 1.14f);
                int glifos = aro == 0 ? 10 : 14;
                float rot0 = time * SelloGiro * dir + aro * 0.3f;
                for (int g = 0; g < glifos; g++)
                {
                    float ang = rot0 + g * MathHelper.TwoPi / glifos;
                    Vector2 pos = c + ang.ToRotationVector2() * radio;
                    float rot = ang + MathHelper.PiOver2;
                    // el glifo respira con su fase
                    float aliento = 0.5f + 0.5f * MathF.Sin(time * 1.7f + g * 2.4f + aro);
                    Color cuerpo = aro == 0 ? RuneOro : AuraPura;
                    VFXCore.Capsule(pos, 7f + 3f * aliento, 2.6f, rot,
                        Tint(cuerpo, (0.22f + 0.26f * aliento) * (0.7f + 0.3f * aliento)));
                }
                // el hilo del aro (tenue — la cuerda del sello)
                RingQuad(c, radio, rot0 * 0.3f, Tint(aro == 0 ? RuneOro : AuraPura, 0.08f));
            }
        }

        private static void Corredores(Vector2 c, float r, float time, int seed)
        {
            for (int k = 0; k < 3; k++)
            {
                float t = time * 1.5f + k * MathHelper.TwoPi / 3f;
                Vector2 pos = VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
                Vector2 tang = new(-MathF.Sin(t), MathF.Cos(t));
                VFXCore.Capsule(pos, 10f, 3.5f, tang.ToRotation(),
                    Tint(Color.Lerp(HotWhite, RingVioleta, k / 3f), 0.5f));
            }
        }

        private static void Rayos(Vector2 c, float r, float time, int seed, int flick)
        {
            // 2 RAYOS danzando EN EL NÚCLEO + 2 EMERGIENDO del anillo (el
            // lightningParticles del script — la firma de la familia).
            for (int k = 0; k < 2; k++)
            {
                // los del núcleo: zigzag vivo del centro al borde interior
                float ang = time * 3.1f + k * MathHelper.Pi;
                Vector2 a = c, b = c + ang.ToRotationVector2() * r * 0.95f;
                RayoZigzag(a, b, BoltNucleo, 0.4f, seed + k, flick);
            }
            for (int k = 0; k < 2; k++)
            {
                // los del anillo: del borde del anillo hacia afuera
                float ang = time * 1.2f + k * MathHelper.Pi + 0.8f;
                Vector2 a = VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, ang);
                Vector2 b = a + ang.ToRotationVector2() * r * 0.8f;
                RayoZigzag(a, b, BoltBorde, 0.3f, seed + 10 + k, flick);
            }
        }

        private static void RayoZigzag(Vector2 a, Vector2 b, Color col, float alfa, int sem, int flick)
        {
            // el rayo de 3 quiebres (determinista por flick — el patrón de
            // la casa: 12 regeneraciones por segundo)
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 4f) return;
            Vector2 dir = delta / len;
            Vector2 norm = new(-dir.Y, dir.X);
            Vector2 p0 = a;
            for (int s = 1; s <= 3; s++)
            {
                float u = s / 3f;
                float quiebre = (VFXCore.Hash01(sem, s, flick) - 0.5f) * len * 0.35f;
                Vector2 p1 = s == 3 ? b : a + dir * (len * u) + norm * quiebre;
                VFXCore.Capsule((p0 + p1) * 0.5f, Vector2.Distance(p0, p1), 2.6f,
                    (p1 - p0).ToRotation(), Tint(col, alfa));
                p0 = p1;
            }
        }

        private static void FilamentosDelVacio(Vector2 c, float r, float time, int seed)
        {
            // LOS FILAMENTOS: hebras violeta que SERPENTEAN desde el borde
            // del sello hacia afuera y se disuelven — el vacío hilado (la
            // firma del Puro). Cada hebra: 4 cápsulas siguiendo una onda.
            for (int k = 0; k < Filamentos; k++)
            {
                float ang = time * 0.22f + k * MathHelper.TwoPi / Filamentos;
                Vector2 dir = ang.ToRotationVector2();
                float fase = VFXCore.Hash01(seed, k, 71);
                for (int s = 0; s < 4; s++)
                {
                    float u = (s + 1) / 5f;                       // 0.2..0.8 a lo largo
                    float vida = 0.5f + 0.5f * MathF.Sin(time * 0.8f + fase * 6.28f + u * 3f);
                    float ondula = MathF.Sin(time * 1.3f + u * 5f + k * 2f) * r * 0.25f;
                    Vector2 pos = c + dir * (SelloRadio * r + u * r * 2.2f) +
                                  new Vector2(-dir.Y, dir.X) * ondula;
                    VFXCore.Capsule(pos, 9f, 2.2f, dir.ToRotation(),
                        Tint(Color.Lerp(AuraPura, HotWhite, u * 0.5f),
                            (0.10f + 0.16f * vida) * (1f - u * 0.6f)));
                }
            }
        }

        private static void CoronaDestellos(Vector2 c, float r, float time, int seed)
        {
            // LA CORONA: picos de 4 puntas (el brillo de los soles rúnicos
            // de 3 anillos+ — DrawTwinkles) girando lento en el plano del
            // anillo: el Puro también tiene su brillo especial.
            const int picos = 5;
            for (int k = 0; k < picos; k++)
            {
                float h = VFXCore.Hash01(seed, 300 + k, 7);
                float vida = (time * 0.35f + h * 3f) % 1f;
                float arde = MathF.Sin(vida * MathHelper.Pi);
                if (arde < 0.08f) continue;
                float t = time * 0.11f + k * MathHelper.TwoPi / picos;
                Vector2 pos = VFXCore.Ellipse(c, RingA * r * 1.35f, RingB * r * 1.35f, RingTilt, t);
                float size = (0.30f + 0.25f * VFXCore.Hash01(seed, 330 + k, 17)) * r * 0.7f * arde;
                // EL DESTELLO DE 4 PUNTAS: dos cápsulas cruzadas + núcleo
                VFXCore.Capsule(pos, size * 2.2f, MathF.Max(1.6f, 0.05f * r) * arde, 0f,
                    Tint(HotWhite, 0.40f * arde));
                VFXCore.Capsule(pos, size * 2.2f, MathF.Max(1.6f, 0.05f * r) * arde, MathHelper.PiOver2,
                    Tint(HotWhite, 0.40f * arde));
                Quad(Glow, pos, new Vector2(size * 1.5f, size * 1.5f), 0f,
                    Tint(RuneOro, 0.35f * arde));
            }
        }

        private static void RunasDoradas(Vector2 c, float r, float time, int seed)
        {
            // 8 GLIFOS DE CIRCUITO orbitando (la familia) + su aro tenue.
            for (int k = 0; k < RuneCount; k++)
            {
                float t = time * RuneOrbit + k * MathHelper.TwoPi / RuneCount;
                Vector2 pos = c + t.ToRotationVector2() * (RuneRadius * r);
                float rot = t + MathHelper.PiOver2;
                float aliento = 0.5f + 0.5f * MathF.Sin(time * 1.6f + k * 2.1f);
                // el glifo: trazo doble (cuerpo + punta brillante)
                VFXCore.Capsule(pos, 10f + 4f * aliento, 3.2f, rot,
                    Tint(RuneOro, 0.30f + 0.35f * aliento));
                VFXCore.Capsule(pos + rot.ToRotationVector2() * (5f + 2f * aliento), 4.5f, 2.4f, rot,
                    Tint(RunePunta, 0.25f + 0.30f * aliento));
            }
            RingQuad(c, RuneRadius * r, time * RuneOrbit * 0.3f, Tint(RuneOro, 0.07f));
        }

        private static void Ondas(Vector2 c, float r, float time)
        {
            // 2 ANILLOS expandiéndose (el ciclo de 2,4 s de la familia).
            for (int k = 0; k < 2; k++)
            {
                float fase = (time / 2.4f + k * 0.5f) % 1f;
                float radio = r * (1.1f + fase * 3.4f);
                RingQuad(c, radio, fase * 3f, Tint(RingVioleta, 0.16f * (1f - fase)));
            }
        }

        // ==================================================================
        //  HELPERS (el dialecto del renderer del Cósmico)
        // ==================================================================
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
            => VFXCore.RingQuad(pos, visibleRadius, rot, tint);
    }
}
