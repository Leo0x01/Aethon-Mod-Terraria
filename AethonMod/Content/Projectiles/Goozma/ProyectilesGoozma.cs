using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Goozma
{
    // ======================================================================
    //  v6.52.2 — LOS PROYECTILES DEL DIOS ANTIGUO (la investigación del
    //  video del usuario: GOOZMA, el antiguo Dios de los Slimes, del mod
    //  «Hunt of the Old God» de Team Hypnos — UriBuilder/CalamityHunt,
    //  FUENTE ABIERTA, leída línea a línea). La copia EXACTA para la
    //  investigación profunda de código, técnica y visuales — adaptada al
    //  pipeline de la casa (RayoStrip.FajaTexturada + ColorGoozma + los
    //  lotes y contratos de siempre).
    // ======================================================================

    /// <summary>
    /// RAYOFUSIONPROJECTILE — EL RAYO DE FUSIÓN (la copia del FusionRay
    /// de Goozma — EL ataque del video, «centrate tambien en su ataque de
    /// haz de luz es muy importante»).
    ///
    /// LA TÉCNICA DEL ORIGINAL (FusionRay.cs + FusionRayEffect.fx,
    /// decodificada): una línea RECTA donde TODA la forma vive en el
    /// ANCHO (p^0.6 de boca · cbrt de punta · shimmer cos(t)·(0.08−0.06p)
    /// · sobres 1650 px) y TODO el color en el GRADIENTE A LO LARGO
    /// (ValueAt(reloj − progress·100) — el haz es una REBANADA MÓVIL del
    /// arcoíris de la paleta OIL); el ruido fluye raíz→punta por UVs
    /// (uTime NEGATIVO) y la punta se redondea con máscara de lente.
    /// Carga 300 t → fuego 800 t con barrido coseno ±2.5 rad. AQUÍ:
    /// escala de jugador (carga 70 t → fuego 130 t, barrido ±0.55 rad,
    /// largo 2200, cono de daño 0.20 rad) y el pipeline de la casa: TRES
    /// FajasTexturadas con color POR NODO (el arcoíris A LO LARGO, por
    /// vértice en CPU — cero shaders) + la telegraph de BeamLib con el
    /// color que camina.
    /// </summary>
    public class RayoFusionProjectile : ModProjectile
    {
        private const int T_CARGA = 70;
        private const int T_FUEGO = 130;
        private const float LARGO = 2200f;
        private const float ANCHO = 46f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CARGA + T_FUEGO + 30;
            Projectile.hide = true;
        }

        /// <summary>El sobre de tamaño (el targetSize→realSize del
        /// original: el haz FLORECE cúbico y se colapsa al final).</summary>
        private float Sobre(float t)
        {
            float abre = Utils.GetLerpValue(T_CARGA - 16, T_CARGA + 26, t, true);
            float cierra = Utils.GetLerpValue(T_CARGA + T_FUEGO + 24, T_CARGA + T_FUEGO - 8, t, true);
            return MathF.Pow(abre * cierra, 3f);
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            float t = Projectile.ai[0];
            Player duenio = Main.player[Projectile.owner];

            // ANCLADO A LA MANO (el proyectil ES el arma viva — como el
            // original, pegado a su dueño con el retroceso empujándolo)
            Projectile.Center = duenio.MountedCenter;
            Projectile.velocity = Vector2.Zero;

            float ang = Projectile.ai[1];
            float barridoDir = Projectile.ai[2] != 0f ? 1f : -1f;

            // === LA CARGA: apunta al cursor con suavizado (0.06 — el
            //     tracking del original que afloja a 0.01 al disparar) ===
            if (t < T_CARGA)
            {
                float hacia = (Main.MouseWorld - duenio.MountedCenter).ToRotation();
                ang += MathHelper.WrapAngle(hacia - ang) * 0.06f;
                Projectile.ai[1] = ang;

                // LAS CHISPAS CONVERGENTES (el «particle corridor» del
                // original: el haz se PRE-MATERIALIZA a lo largo del camino)
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    float prog = Main.rand.NextFloat();
                    Vector2 pos = Projectile.Center + ang.ToRotationVector2() * (prog * LARGO * 0.6f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.RainbowRod,
                        -ang.ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 128,
                        ColorGoozma.ValueAt(t * 1.6f - prog * 60f), 0.9f);
                    d.noGravity = true;
                }
            }
            // === EL FUEGO: EL BARRIDO COSENO del original (una periodo
            //     entero, con rampas de entrada/salida — aquí ±0.55 rad) ===
            else if (t < T_CARGA + T_FUEGO)
            {
                float u = (t - T_CARGA) / T_FUEGO;
                float entra = Utils.GetLerpValue(0f, 0.12f, u, true);
                float sale = Utils.GetLerpValue(1f, 0.88f, u, true);
                ang += MathF.Cos(u * MathHelper.TwoPi) * 0.55f * barridoDir * entra * sale * 0.055f;
                Projectile.ai[1] = ang;

                // EL SACUDÓN DE CÁMARA (cada 4 t — el PunchCamera del original:
                // 12 frames, falloff 5000)
                if (Main.netMode != NetmodeID.Server && t % 4f == 0f)
                {
                    float f2 = Sobre(t);
                    Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
                        Main.rand.NextVector2Unit(), 4f * f2, 10f, 12, 5000f, "RayoFusion"));
                }

                // EL DAÑO EN CONO (LA HITBOX del original, exacta:
                // IntersectsConeFastInaccurate — largo·(0.5+grow·0.5),
                // medio ángulo 0.20·grow)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float f2 = Sobre(t);
                    if (f2 > 0.25f)
                    {
                        float largoCono = LARGO * (0.5f + f2 * 0.5f);
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n == null || !n.active || n.life <= 0 || n.friendly || n.townNPC) continue;
                            if (Terraria.Utils.IntersectsConeFastInaccurate(n.Hitbox,
                                Projectile.Center, largoCono, ang, 0.20f * f2))
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), n.Center, Vector2.Zero,
                                    ModContent.ProjectileType<GolpeFusion>(), Projectile.damage, 0f,
                                    Projectile.owner, i, ang);
                            }
                        }
                    }
                }
            }
            else if (t > T_CARGA + T_FUEGO + 26) Projectile.Kill();

            // LA LUZ a lo largo (cada 96 px — la regla amable de la casa)
            if (Main.netMode != NetmodeID.Server)
            {
                float f = t > T_CARGA ? Sobre(t) : 0f;
                Color c = ColorGoozma.ValueAt(t * 1.6f);
                Vector3 v = c.ToVector3();
                Vector2 dir = ang.ToRotationVector2();
                int pasos = (int)(LARGO * (0.5f + f * 0.5f) / 96f);
                for (int k = 0; k <= pasos; k++)
                    Lighting.AddLight(Projectile.Center + dir * (k * 96f),
                        v.X * (0.22f + 0.55f * f), v.Y * (0.22f + 0.55f * f), v.Z * (0.22f + 0.55f * f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[0];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float ang = Projectile.ai[1];
            Vector2 dir = ang.ToRotationVector2();

            // === LA CARGA: LA TELEGRAPH con el color que CAMINA (la aguja
            //     de BeamLib + el reloj del arcoíris — la promesa completa) ===
            if (t < T_CARGA)
            {
                float fase = t / T_CARGA;
                BeamLib.Telegrafo(Projectile.Center, dir, LARGO,
                    ColorGoozma.ValueAt(t * 1.6f), fase, tiempo, ANCHO);
                return false;
            }

            // === EL FUEGO: LAS TRES TIRAS con el ARCOÍRIS A LO LARGO ===
            float f = Sobre(t);
            if (f <= 0.02f) return false;

            float largo = LARGO * (0.5f + f * 0.5f);
            int n = 40;
            Vector2[] espinaP = new Vector2[n];
            float[] semiCuerpo = new float[n];
            float[] semiHalo = new float[n];
            float[] semiCore = new float[n];
            Color[] colCuerpo = new Color[n];
            Color[] colHalo = new Color[n];
            Color[] colCore = new Color[n];

            // LA REBANADA MÓVIL: 100 ticks de paleta a lo largo (el span
            // del original) + LA FÓRMULA EXACTA del ancho (p^0.6 · cbrt ·
            // shimmer cos(t·8)·(0.08−0.06p))
            for (int i = 0; i < n; i++)
            {
                float p = i / (float)(n - 1);
                espinaP[i] = Projectile.Center + dir * (largo * p) - Main.screenPosition;

                float rampa = MathF.Pow(p, 0.6f);
                float tapa = MathF.Cbrt(Utils.GetLerpValue(1.01f, 0.72f, p, true));
                float shimmer = 1f + MathF.Cos(tiempo * 8f) * (0.08f - p * 0.06f);
                float baseW = ANCHO * 0.5f * rampa * tapa * shimmer * f;

                semiCuerpo[i] = baseW;
                semiHalo[i] = baseW * 2.3f;
                semiCore[i] = MathF.Max(baseW * 0.34f, 1.2f);

                Color c = ColorGoozma.ALoLargo(t * 1.6f, p, 100f);
                colCuerpo[i] = StormLib.Tint(c, 0.60f * f);
                colHalo[i] = StormLib.Tint(c, 0.20f * f);
                colCore[i] = StormLib.Tint(new Color(255, 252, 245), 0.85f * f);
            }

            Texture2D tex = BeamLib.RuidoTinta;
            VFXCore.CerrarLoteSiAbierto();
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(espinaP, semiHalo, colHalo, Color.White, tex,
                    560f, 1.0f, tiempo, 0f, 1f);
                RayoStrip.FajaTexturadaEnLote(espinaP, semiCuerpo, colCuerpo, Color.White, tex,
                    420f, 1.3f, tiempo, 0f, 1f);
                RayoStrip.FajaTexturadaEnLote(espinaP, semiCore, colCore, Color.White, tex,
                    420f, 2.1f, tiempo, 0.36f, 0.64f);
            }
            finally { RayoStrip.CerrarLote(); }
            VFXCore.ReabrirLoteVanilla();

            // LA BOCA + LA PUNTA (los sprites del remate — el lote VFXCore)
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, StormLib.Tint(ColorGoozma.ValueAt(t * 1.6f), 0.60f * f),
                new Vector2(ANCHO * 1.8f, ANCHO * 1.8f), VFXCore.GlowOrb);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(Color.White, 0.45f * f),
                new Vector2(ANCHO * 0.9f, ANCHO * 0.9f), ang, VFXCore.GlowOrb);
            Vector2 punta = Projectile.Center + dir * largo;
            VFXCore.Quad(punta, StormLib.Tint(ColorGoozma.ALoLargo(t * 1.6f, 1f), 0.5f * f),
                new Vector2(ANCHO * 1.4f, ANCHO * 1.4f), VFXCore.GlowOrb);
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }

    /// <summary>
    /// GOLPEFUSION — el portador del daño del cono (invisible, 2 ticks:
    /// el patrón de la casa para la inmunidad local por NPC — el trazo
    /// del ángulo viaja en ai[1] para el knockback direccional).
    /// </summary>
    public class GolpeFusion : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 2;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            int idx = (int)Projectile.ai[0];
            if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx] != null && Main.npc[idx].active)
                Projectile.Center = Main.npc[idx].Center;
            Projectile.velocity = Projectile.ai[1].ToRotationVector2() * 3f;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    /// <summary>
    /// LASERARCOIRIS — LA COPIA del RainbowLaser de Goozma (el láser-bala
    /// CURVO de los Goozmitas): extraUpdates 8, la velocidad CURVA un
    /// pelín por update (RotatedBy 0.0016·dir — el arco total ~0.3 rad)
    /// y la estela es la PILA DE SPRITES del original: cada punto del
    /// rastro dibujado DOS veces (núcleo fino estirado + halo ancho) con
    /// el color del RELOJ desfasado por tramo (ValueAt(reloj − i·5) — el
    /// arcoíris CORRE a lo largo de la estela).
    /// </summary>
    public class LaserArcoirisProjectile : ModProjectile
    {
        private const int ESTELA = 12;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = ESTELA;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 8;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            float dirCurva = Projectile.ai[1] != 0f ? 1f : -1f;
            Projectile.velocity = Projectile.velocity.RotatedBy(0.0016f * dirCurva);
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.netMode != NetmodeID.Server)
            {
                Color c = ColorGoozma.ValueAt(Projectile.ai[0] * 8f);
                Lighting.AddLight(Projectile.Center, c.R / 255f * 0.7f, c.G / 255f * 0.7f, c.B / 255f * 0.7f);
                if (Main.rand.NextBool(20))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        -Projectile.velocity * 0.1f, 128, ColorGoozma.ValueAt(Projectile.ai[0] * 8f + 30f), 0.7f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.Begin();
            // LA ESTELA: el doble dibujado por punto (núcleo + halo) con el
            // arcoíris que CORRE (i·5 ticks de desfase — la firma)
            for (int i = 1; i < ESTELA; i++)
            {
                Vector2 a = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float len = Vector2.Distance(a, b);
                if (len < 2f) continue;
                float u = i / (float)(ESTELA - 1);
                Color c = ColorGoozma.ValueAt(Projectile.ai[0] * 8f - i * 5f);
                float alfa = (1f - u) * 0.85f;
                float ang = (b - a).ToRotation();
                // EL HALO ANCHO (tenue)
                VFXCore.Quad((a + b) * 0.5f, StormLib.Tint(c, 0.22f * alfa),
                    new Vector2(len + 26f, 16f * (1f - u * 0.5f)), ang, VFXCore.GlowOrb);
                // EL NÚCLEO FINO (estirado, brillante)
                VFXCore.Quad((a + b) * 0.5f, StormLib.Tint(Color.Lerp(c, Color.White, 0.5f), 0.65f * alfa),
                    new Vector2(len + 12f, 5f), ang, VFXCore.GlowOrb);
            }
            // LA CABEZA (el par del original: color + blanco)
            Color cH = ColorGoozma.ValueAt(Projectile.ai[0] * 8f);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(cH, 0.5f), new Vector2(46f, 46f), Projectile.rotation, VFXCore.GlowOrb);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(Color.White, 0.65f), new Vector2(18f, 18f), Projectile.rotation, VFXCore.GlowOrb);
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// PRISMADESTRUCTOR — LA COPIA del PrismDestroyer de Goozma (el
    /// shuriken de 5 RAYOS de la Divina Gargoptuar): un prisma que
    /// ORBITA al jugador (radio que respira hasta 300, el giro que se
    /// CONGELA al disparar — el −0.002 del original) barriendo con 5
    /// rayos en abanico; cada rayo colisiona por TRES TRAMOS
    /// decrecientes (70/20 · 180/12 · 320/5 — la escalera del original)
    /// y todos viven en el arcoíris HSL que gira (hue 0.01/t).
    /// </summary>
    public class PrismaDestructorProjectile : ModProjectile
    {
        private const int RAYOS = 5;
        private const int VIDA = 150;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 700;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VIDA;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            float t = Projectile.ai[0];
            Player duenio = Main.player[Projectile.owner];

            // LA ÓRBITA: radio que respira hacia 300, giro RÁPIDO al nacer
            // (telegraph) que se CONGELA al disparar
            float radio = MathHelper.Lerp(90f, 300f, MathHelper.Clamp(t / 30f, 0f, 1f));
            float velGiro = t < 30f ? 0.16f : 0.02f;
            Projectile.ai[1] += velGiro * ((Projectile.whoAmI & 1) == 0 ? 1f : -1f);
            float fase = Projectile.ai[1] + Projectile.whoAmI * 1.7f;
            Projectile.Center = duenio.MountedCenter + fase.ToRotationVector2() * radio;
            Projectile.rotation = fase + MathHelper.PiOver2;

            // EL DAÑO: cada rayo, TRES tramos de línea (la escalera)
            if (Main.netMode != NetmodeID.MultiplayerClient && t > 20f)
            {
                for (int r = 0; r < RAYOS; r++)
                {
                    float angR = fase + r * MathHelper.TwoPi / RAYOS - MathHelper.PiOver4;
                    Vector2 dirR = angR.ToRotationVector2();
                    for (int tramo = 0; tramo < 3; tramo++)
                    {
                        float largoT = tramo == 0 ? 70f : tramo == 1 ? 180f : 320f;
                        float anchoT = tramo == 0 ? 20f : tramo == 1 ? 12f : 5f;
                        float desde = tramo == 0 ? 0f : tramo == 1 ? 70f : 180f;
                        Vector2 a = Projectile.Center + dirR * desde;
                        Vector2 b = Projectile.Center + dirR * largoT;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n == null || !n.active || n.life <= 0 || n.friendly || n.townNPC) continue;
                            float punto = 0f;
                            if (Collision.CheckAABBvLineCollision(n.Hitbox.TopLeft(), n.Hitbox.Size(),
                                a, b, anchoT, ref punto))
                            {
                                if (Projectile.owner == Main.myPlayer)
                                    n.StrikeNPC(new NPC.HitInfo
                                    {
                                        Damage = Projectile.damage,
                                        SourceDamage = Projectile.damage,
                                        DamageType = DamageClass.Magic,
                                        Crit = false,
                                        HitDirection = Math.Sign(dirR.X),
                                        Knockback = 3f,
                                    }, false, true);
                                break;
                            }
                        }
                    }
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Color c = Main.hslToRgb((t * 0.01f) % 1f, 1f, 0.7f);
                Lighting.AddLight(Projectile.Center, c.R / 255f * 0.8f, c.G / 255f * 0.8f, c.B / 255f * 0.8f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[0];
            float fase = Projectile.ai[1] + Projectile.whoAmI * 1.7f;
            float abre = Utils.GetLerpValue(16f, 34f, t, true) *
                         Utils.GetLerpValue(VIDA, VIDA - 20, t, true);

            VFXCore.Begin();
            // EL CENTRO: el prisma oscuro + SU halo (el disco del original)
            VFXCore.Quad(Projectile.Center, StormLib.Tint(new Color(30, 14, 46), 0.85f * abre),
                new Vector2(52f, 52f), 0f, VFXCore.GlowOrb);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(Color.White, 0.30f * abre),
                new Vector2(30f, 30f), 0f, VFXCore.GlowOrb);

            // LOS 5 RAYOS (el abanico a −45° del original, el HSL que gira)
            for (int r = 0; r < RAYOS; r++)
            {
                float angR = fase + r * MathHelper.TwoPi / RAYOS - MathHelper.PiOver4;
                Vector2 dirR = angR.ToRotationVector2();
                Color c = Main.hslToRgb((t * 0.01f + r / (float)RAYOS) % 1f, 1f, 0.7f);

                float largo = 300f * abre;
                VFXCore.Quad(Projectile.Center + dirR * (largo * 0.5f), StormLib.Tint(c, 0.45f * abre),
                    new Vector2(largo, 26f * abre), angR, VFXCore.GlowOrb);
                VFXCore.Quad(Projectile.Center + dirR * (largo * 0.5f),
                    StormLib.Tint(Color.Lerp(c, Color.White, 0.55f), 0.75f * abre),
                    new Vector2(largo * 0.96f, 9f * abre), angR, VFXCore.GlowOrb);
            }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// TRUENOGELATINA — LA COPIA del GooLightning de Goozma (el trueno
    /// arcoíris en ráfagas): el canal PERLIN de la casa (StormLib.
    /// PerlinBolt — la traducción natural de su VertexStrip) con el
    /// color del RELOJ (el arcoíris vivo, no fijo), el DOBLE pase (el
    /// trazo + la vena — su LightningGlow) y el fade (1 − t/30)^0.6.
    /// Golpea por LÍNEA al nacer (width 50, los ticks 0-20 del original).
    /// </summary>
    public class TruenoGelatinaProjectile : ModProjectile
    {
        private const int VIDA = 24;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VIDA;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            if (Main.netMode != NetmodeID.Server)
            {
                Color c = ColorGoozma.ValueAt(Projectile.ai[0] * 6f);
                Lighting.AddLight(Projectile.Center, c.R / 255f, c.G / 255f, c.B / 255f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[0];
            float fade = MathF.Pow(1f - t / VIDA, 0.6f);
            if (fade <= 0.02f) return false;

            float tiempo = Main.GlobalTimeWrappedHourly;
            int flick = StormLib.FlickTick(tiempo, 12f);
            int seed = Projectile.whoAmI * 37 + 3;
            Vector2 desde = Projectile.Center - Main.screenPosition;
            Vector2 hasta = Projectile.Center + Projectile.velocity - Main.screenPosition;

            // EL CANAL PERLIN ×2 (el trazo + la vena) — el arcoíris DEL RELOJ
            Color c = ColorGoozma.ValueAt(t * 6f);
            StormLib.PerlinBolt(Main.spriteBatch, desde, hasta, seed, flick, 4.2f,
                c, Color.Lerp(c, Color.White, 0.6f), 0.9f * fade, 0.24f);
            StormLib.PerlinBolt(Main.spriteBatch, desde, hasta, seed + 91, flick, 2.2f,
                Color.Lerp(c, Color.White, 0.4f), Color.White, 0.6f * fade, 0.18f);

            // EL FONDO GLOW (la campana que sostiene el canal)
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center + Projectile.velocity * 0.5f,
                StormLib.Tint(c, 0.30f * fade), new Vector2(140f, 90f),
                Projectile.velocity.ToRotation(), VFXCore.SoftGlow);
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
