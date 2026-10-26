using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics;
using AethonMod.Content.VFX;
using AethonMod.Content.GoozmaPort;
using AethonMod.Content.GoozmaPort.Common.Utilities;
using AethonMod.Content.GoozmaPort.Content.NPCs.Bosses.GoozmaBoss;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// PRIMERTRAZOFAJO — v6.52.0 — UNO DE LOS CINCO FAJOS del Primer
    /// Trazo (la receta vanilla 632 + GammaBeam de Calamity, según el
    /// blueprint R2 §B2):
    ///
    ///   · VIVE EN EL ANILLO DEL CONTROLADOR: boca = padre + anillo
    ///     giratorio de radio 20→4 px; abanico sin(anillo)·π/6·(1−carga)
    ///     — a plena carga los CINCO se funden en UN rayo (daño ×3).
    ///   · EL RAYCAST: Collision.LaserScan (2400 px) con la longitud
    ///     SUAVIZADA (localAI[1] = Lerp(actual, ideal, 0.75)) — la punta
    ///     del rayo se DESLIZA al barrer: el motion-blur de la receta.
    ///   · EL CUERPO: BeamLib.Rayo — la pila completa de 7 capas (espina
    ///     fBm al 3.5%, halo+cuerpo+core, diamantes viajeros sin^7, luz
    ///     cada 64 px, tapas y estallido de pared).
    ///   · EL DAÑO DE LÍNEA: Colliding → CheckAABBvLineCollision (el
    ///     hitbox ES el rayo), penetrate −1 con inmunidad local (10 t).
    ///   · EL HUE que gira: cada fajo lleva su color del arcoíris (la
    ///     firma del Prism) avanzando con el tiempo global.
    /// </summary>
    public class PrimerTrazoFajo : ModProjectile
    {
        // v6.52.1 — LA CURA DE LAS 9 TEXTURAS: el placeholder de la
        // casa (el rayo ES BeamLib — el asset jamás se dibuja).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>El ÍNDICE del fajo (0..4) — su hue y su asiento en el anillo.</summary>
        public int Indice => (int)Projectile.ai[0];

        /// <summary>El whoAmI del controlador (PrimerTrazoHeld).</summary>
        public int PadreWho => (int)Projectile.ai[1];

        /// <summary>La longitud ACTUAL (suavizada) — vive en localAI[1].</summary>
        public float Largo
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        /// <summary>¿El raycast cortó en tile? (el taper + el estallido de la receta).</summary>
        public bool ChoquePared => Projectile.localAI[0] >= 1f;

        public override void SetStaticDefaults()
        {
            // SIN culling en los bordes de pantalla: un rayo de 2400 px
            // NUNCA debe esfumarse cerca del borde (la lección de la
            // DrawScreenCheckFluff — el blueprint R2 §7).
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;      // la longitud ES la colisión
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 8;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // === EL PADRE: sin controlador no hay trazo (fade y fuera) ===
            Projectile padre = Main.projectile[PadreWho];
            if (padre == null || !padre.active ||
                padre.type != ModContent.ProjectileType<PrimerTrazoHeld>())
            {
                Projectile.Kill();
                return;
            }

            float carga = MathHelper.Clamp((padre.ai[0] - 1f) / 179f, 0f, 1f);
            bool soltado = padre.ai[1] >= 1f;
            float fade = soltado ? MathHelper.Clamp(1f - (padre.ai[2] - 6f) / 8f, 0f, 1f) : 1f;

            Projectile.timeLeft = 4;
            if (fade <= 0f)
            {
                Projectile.Kill();
                return;
            }

            // === LA GEOMETRÍA DEL ANILLO (vanilla 632: la convergencia) ===
            // Cada fajo se sienta en un anillo que GIRA lento alrededor de
            // la boca; el radio SE CIERRA (20→4) y el abanico sin(anillo)·
            // π/6·(1−carga) se APAGA con la carga — al llegar a 1 los cinco
            // ocupan el MISMO sitio apuntando al MISMO rumbo: UN rayo solo.
            float tiempo = Main.GlobalTimeWrappedHourly;
            float anguloAnillo = tiempo * 0.55f + Indice * MathHelper.TwoPi / 5f;
            float radio = MathHelper.Lerp(20f, 4f, carga);
            Vector2 rumbo = padre.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 boca = padre.Center + rumbo * 14f +
                new Vector2(MathF.Cos(anguloAnillo), MathF.Sin(anguloAnillo)) * radio;
            Vector2 dir = rumbo.RotatedBy(MathF.Sin(anguloAnillo) * (MathHelper.Pi / 6f) * (1f - carga))
                .SafeNormalize(Vector2.UnitX);

            // La posición del proyectil ES la boca (Center = boca).
            Projectile.Center = boca;
            Projectile.velocity = dir;

            // === EL RAYCAST + EL SUAVIZADO (la capa 1 de la receta) ===
            float[] muestras = new float[3];
            Collision.LaserScan(boca, dir, 22f * 0.6f, 2400f, muestras);
            float ideal = (muestras[0] + muestras[1] + muestras[2]) / 3f;
            if (Largo <= 0f) Largo = ideal;
            Largo = MathHelper.Lerp(Largo, ideal, 0.75f);
            Projectile.localAI[0] = ideal < 2392f ? 1f : 0f;   // ¿tocó pared?

            // === EL DAÑO DE LA FUSIÓN (vanilla: ×3 al bloquearse) ===
            // Determinista por (edad del padre): TODAS las máquinas
            // computan lo mismo (la lección MP de la receta).
            int dañoBase = Projectile.originalDamage <= 0 ? Projectile.damage : Projectile.originalDamage;
            Projectile.damage = Math.Max(1, (int)(dañoBase * (1f + 2f * carga) * fade));

            // === LA OPACIDAD (la curva del Prism: 0→0.4 mientras nace) ===
            Projectile.localAI[2] = MathHelper.Clamp(
                0.35f + 0.65f * carga + (soltado ? 0f : 0f), 0f, 1f) * fade;

            // === EL POLVO DEL IMPACTO (la receta vanilla: 2/t sin gravedad
            // + 1 de cada 5 la chispa dorada que rebota) ===
            if (Main.netMode != NetmodeID.Server && ChoquePared && !soltado &&
                Largo > 30f && Main.rand.NextBool(2))
            {
                Vector2 punta = boca + dir * (Largo - 14f);
                Dust d = Dust.NewDustPerfect(punta + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.RainbowMk2, -dir * Main.rand.NextFloat(0.5f, 2.2f),
                    180, Main.hslToRgb(Hue(), 1f, 0.75f), 1.4f);
                d.noGravity = true;
                if (Main.rand.NextBool(5))
                {
                    d.type = DustID.GoldFlame;
                    d.velocity *= 0.5f;
                    d.velocity.Y = -MathF.Abs(d.velocity.Y);
                }
            }

            // === EL ARADO DE HIERBA (KarnaLaser: el rayo CORTA como el
            // melee — vive en el HOOK CutTiles, el sitio de vanilla) ===
        }

        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center,
                Projectile.Center + Projectile.velocity * Largo, 10f,
                DelegateMethods.CutTiles);
        }

        /// <summary>El hue del fajo (la firma Prism: índice/5 + reloj lento).</summary>
        private float Hue() => (Indice / 5f + Main.GlobalTimeWrappedHourly * 0.07f) % 1f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL HITBOX DE LÍNEA (la receta universal): CheckAABBvLine.
            float ignorado = 0f;
            Vector2 fin = Projectile.Center + Projectile.velocity * Largo;
            Projectile padre = Main.projectile[PadreWho];
            float carga = padre != null && padre.active
                ? MathHelper.Clamp((padre.ai[0] - 1f) / 179f, 0f, 1f) : 1f;
            // v6.53 — EL GROSOR DE VERDAD: el rayo fusionado es GORDO (la
            // hitbox respira con el ancho visual del FusionRay).
            float grosor = MathHelper.Lerp(14f, 90f, carga);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, fin, grosor, ref ignorado);
        }

        // === MP: el largo y el choque viajan por el cable (los fajos
        // corren en TODAS las máquinas pero el raycast del servidor manda
        // — el estado extra viaja SIMÉTRICO, la lección v6.50.62) ===
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Largo);
            writer.Write(ChoquePared);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            try
            {
                Largo = reader.ReadSingle();
                Projectile.localAI[0] = reader.ReadBoolean() ? 1f : 0f;
            }
            catch { }
        }

        // ==================================================================
        //  v6.53 — EL RAYO DE FUSIÓN DE GOOZMA, LA TÉCNICA REAL
        //
        //  La letra: «el ataque del primer trazo todavía se ve mal… la
        //  respuesta es el rayo de fusión de Goozma pero necesitamos saber
        //  cómo hacerlo, necesitamos conocer la técnica». AHORA LA SABEMOS
        //  Y LA TENEMOS: el jefe fue portado verbatim (Content/GoozmaPort)
        //  y este fajo dibuja con SU MISMO MATERIAL —
        //
        //    · LA ESPINA RECTA (VertexStrip de vanilla con padding
        //      procedural — 1500 puntos en Goozma; 256 aquí para 2400 px).
        //    · EL ANCHO es TODA la forma (la fórmula exacta del jefe):
        //      p^0.8 crece de la boca · cbrt capa al 70 % y muere en la
        //      punta · ×(1.1 + cos·(0.08 − p·0.06)) — el VIVO que respira.
        //    · EL ARCOÍRIS VIAJA por el rayo: GradientColor(GoozColors)
        //      evaluado en (reloj − progreso·100) — una REBANADA MÓVIL de
        //      la paleta de aceite del dios.
        //    · EL SHADER VERDADERO: FusionRayEffect.fx con sus CUATRO
        //      texturas (ruido, bits, glow, segundo ruido) — el núcleo
        //      BLANCO-CALIENTE (length(laser) > 0.4 → 1), el fade en
        //      lente, el wobble vertical y los bits que chispean.
        //    · uTime NEGATIVO: el ruido fluye de la BOCA a la PUNTA.
        // ==================================================================

        /// <summary>La tira de vanilla (la misma del jefe).</summary>
        private static readonly VertexStrip _tira = new();

        /// <summary>El reloj compartido del arma (todas los fajos leen el
        /// MISMO reloj global — la rebanada de paleta viaja coordinada).</summary>
        private static float Reloj => Main.GlobalTimeWrappedHourly * 2f;

        /// <summary>LA REBANADA MÓVIL — el arcoíris corre a lo largo.</summary>
        private Color StripColor(float progress)
        {
            Color color = new GradientColor(SlimeUtils.GoozColors, 0.2f, 0.2f)
                .ValueAt(Reloj - progress * 100f);
            color.A = 0;
            return color * _intensidad;
        }

        /// <summary>LA FORMA DEL ANCHO — la fórmula del jefe, a escala del
        /// arma: nace en la boca, engorda hasta el 70 % y muere en la
        /// punta, respirando con el compás.</summary>
        private float StripWidth(float progress)
        {
            float start = (float)System.Math.Pow(progress, 0.8f);
            float cap = (float)System.Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));
            return start * cap * _anchoMax
                * (1.1f + (float)System.Math.Cos(Reloj * 2f) * (0.08f - progress * 0.06f));
        }

        // El estado vivo del frame (lo llena PreDraw).
        private float _anchoMax = 40f;
        private float _intensidad = 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            if (Largo < 16f) return false;

            // === EL ESTADO DEL FRAME ===
            Projectile padre = Main.projectile[PadreWho];
            float carga = padre != null && padre.active
                ? MathHelper.Clamp((padre.ai[0] - 1f) / 179f, 0f, 1f) : 1f;
            float alfa = Projectile.localAI[2] <= 0f ? 1f : Projectile.localAI[2];
            _intensidad = (0.45f + 0.55f * carga) * alfa;
            // EL ANCHO DEL FUSION: los fajos separados son FINOS; al
            // FUNDIRSE (carga 1) el rayo engorda a lo GRANDE (la escala
            // del arma — el jefe usa 3500; aquí 250 para el jugador).
            _anchoMax = MathHelper.Lerp(34f, 250f, carga);

            // === LA BOCA (los glows del jefe, a escala) ===
            Color colorBoca = new GradientColor(SlimeUtils.GoozColors, 0.2f, 0.2f).ValueAt(Reloj);
            colorBoca.A = 0;
            Microsoft.Xna.Framework.Graphics.Texture2D glow = AssetDirectory.Textures.Glow[0].Value;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null,
                colorBoca * 0.5f * _intensidad, 0f, glow.Size() * 0.5f,
                1.6f * _intensidad, 0f, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null,
                new Color(255, 255, 255, 0) * 0.4f * _intensidad, 0f, glow.Size() * 0.5f,
                0.8f * _intensidad, 0f, 0);

            // === LA ESPINA RECTA (256 puntos — la receta del jefe) ===
            const int PUNTOS = 256;
            Vector2[] posiciones = new Vector2[PUNTOS];
            float[] rotaciones = new float[PUNTOS];
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rumbo = dir.ToRotation();
            for (int i = 0; i < PUNTOS; i++)
            {
                posiciones[i] = Projectile.Center + dir * (Largo * (i / (float)(PUNTOS - 1)));
                rotaciones[i] = rumbo;
            }

            _tira.PrepareStripWithProceduralPadding(posiciones, rotaciones,
                StripColor, StripWidth, -Main.screenPosition, true);

            // === EL SHADER VERDADERO (FusionRayEffect + sus 4 texturas) ===
            Microsoft.Xna.Framework.Graphics.Effect efecto = AssetDirectory.Effects.FusionRay.Value;
            efecto.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
            efecto.Parameters["uTexture0"].SetValue(AssetDirectory.Textures.Goozma.FusionRay[0].Value);
            efecto.Parameters["uTexture1"].SetValue(AssetDirectory.Textures.Goozma.FusionRay[3].Value);
            efecto.Parameters["uGlow"].SetValue(AssetDirectory.Textures.Goozma.FusionRay[2].Value);
            efecto.Parameters["uBits"].SetValue(AssetDirectory.Textures.Goozma.FusionRay[1].Value);
            efecto.Parameters["uTime"].SetValue(-Reloj * 0.0125f); // fluye boca→punta
            efecto.Parameters["uFreq"].SetValue(1f);
            efecto.CurrentTechnique.Passes[0].Apply();
            _tira.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            return false; // el sprite NUNCA: el rayo ES la tira
        }
    }
}
