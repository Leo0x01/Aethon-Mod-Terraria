using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// PRIMERTRAZOFAJO — v6.52.0 — UNO DE LOS CINCO FAJOS del Primer
    /// Trazo (la receta vanilla 632 + el haz gamma de la investigación, según el
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
            // hitbox respira con el ancho visual del haz fusionado).
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
        //  v6.53.1 — EL RAYO DE FUSIÓN, 100 % MATERIAL DE LA CASA
        //
        //  La letra del usuario: «esta parte del jefe es solo para investigación,
        //  nunca lo usaré realmente y todo su material debería ser
        //  borrado antes del lanzamiento» — el material de referencia SALIÓ del
        //  mod (retirado y BORRADO del repo en la v6.54.0 — la orden del
        //  usuario tras terminar la investigación) y
        //  con él el shader y las texturas prestadas (que además JAMÁS
        //  habrían cargado: tML 2026.08 solo carga efectos .fxc
        //  compilados — los .fx prestados viajaban crudos). LA TÉCNICA
        //  APRENDIDA PERMANECE (las ideas no tienen dueño) y vive en el
        //  pipeline propio PROBADO de la .52.2 (el Rayo de Fusión
        //  histórico de esa versión):
        //
        //    · LA ESPINA RECTA (48 nodos en coordenadas de PANTALLA —
        //      el contrato de RayoStrip, la lección de las alas).
        //    · EL ANCHO es TODA la forma (la fórmula del jefe): p^0.8
        //      crece de la boca · cbrt capa al 70 % y muere en la punta ·
        //      ×(1.1 + cos·(0.08 − p·0.06)) — el VIVO que respira.
        //    · EL ARCOÍRIS VIAJA por el rayo: ColorPrisma.ALoLargo
        //      evaluado en (reloj − progreso·100) POR NODO — una REBANADA
        //      MÓVIL de la paleta de aceite, resuelta en CPU (cero
        //      shaders, la firma de la casa).
        //    · LA FUSIÓN LITERAL: separados, cada fajo viste SU hue del
        //      prisma (la firma del arma); al cargar, los CINCO colores
        //      se DISUELVEN en el arcoíris del dios (lerp por carga²).
        //    · TRES CAPAS en un solo lote: halo ×2.3 · cuerpo ×1 ·
        //      núcleo BLANCO-CALIENTE ×0.34 (banda V 0.36-0.64 de
        //      RuidoTinta — el corazón del gaussiano) con el flujo
        //      horneado en las UVs (periodo/flujo raíz→punta).
        // ==================================================================

        /// <summary>El reloj compartido del arma (todos los fajos leen el
        /// MISMO reloj global — la rebanada de paleta viaja coordinada).</summary>
        private static float Reloj => Main.GlobalTimeWrappedHourly * 2f;

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
            float tiempo = Main.GlobalTimeWrappedHourly;

            // === LA BOCA (el resplandor de la casa, a escala del dios) ===
            Color colorBoca = ColorPrisma.ValueAt(Reloj);
            Texture2D glow = VFXCore.SoftGlow;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null,
                colorBoca * 0.5f * _intensidad, 0f, glow.Size() * 0.5f,
                1.6f * _intensidad, 0f, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null,
                new Color(255, 255, 255, 0) * 0.4f * _intensidad, 0f, glow.Size() * 0.5f,
                0.8f * _intensidad, 0f, 0);

            // === LA ESPINA RECTA (48 nodos — la receta del jefe) ===
            const int NODOS = 48;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2[] espina = new Vector2[NODOS];
            float[] semiCuerpo = new float[NODOS];
            float[] semiHalo = new float[NODOS];
            float[] semiCore = new float[NODOS];
            Color[] colCuerpo = new Color[NODOS];
            Color[] colHalo = new Color[NODOS];
            Color[] colCore = new Color[NODOS];

            // EL HUE DEL PRISMA (la firma del fajo) que se disolverá en
            // el arcoíris del dios con la carga — LA FUSIÓN LITERAL.
            Color hue = Main.hslToRgb(Hue(), 1f, 0.72f);
            float funde = carga * carga;   // la fusión acelera al final

            for (int i = 0; i < NODOS; i++)
            {
                float p = i / (float)(NODOS - 1);
                espina[i] = Projectile.Center + dir * (Largo * p) - Main.screenPosition;

                // LA FORMA DEL ANCHO — la fórmula del jefe, a escala del
                // arma: nace en la boca, engorda hasta el 70 % y muere en
                // la punta, respirando con el compás.
                float rampa = (float)System.Math.Pow(p, 0.8f);
                float tapa = (float)System.Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, p, true));
                float vivo = 1.1f + (float)System.Math.Cos(Reloj * 2f) * (0.08f - p * 0.06f);
                float baseW = _anchoMax * 0.5f * rampa * tapa * vivo * _intensidad;

                semiCuerpo[i] = baseW;
                semiHalo[i] = baseW * 2.3f;
                semiCore[i] = MathF.Max(baseW * 0.34f, 1.2f);

                // LA REBANADA MÓVIL — el arcoíris corre a lo largo (100
                // ticks de paleta, del blanco de la boca al cian de la
                // punta) fundiéndose con el hue del prisma.
                Color c = Color.Lerp(hue, ColorPrisma.ALoLargo(Reloj, p, 100f), funde);
                colCuerpo[i] = StormLib.Tint(c, 0.60f * _intensidad);
                colHalo[i] = StormLib.Tint(c, 0.20f * _intensidad);
                colCore[i] = StormLib.Tint(new Color(255, 252, 245), 0.85f * _intensidad);
            }

            // === LAS TRES CAPAS (el lote único de la casa — cero shaders,
            //     el mismo baile probado del Rayo de Fusión de la .52.2) ===
            Texture2D tex = BeamLib.RuidoTinta;
            VFXCore.CerrarLoteSiAbierto();
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(espina, semiHalo, colHalo, Color.White, tex,
                    560f, 1.0f, tiempo, 0f, 1f);
                RayoStrip.FajaTexturadaEnLote(espina, semiCuerpo, colCuerpo, Color.White, tex,
                    420f, 1.3f, tiempo, 0f, 1f);
                RayoStrip.FajaTexturadaEnLote(espina, semiCore, colCore, Color.White, tex,
                    420f, 2.1f, tiempo, 0.36f, 0.64f);
            }
            finally { RayoStrip.CerrarLote(); }
            VFXCore.ReabrirLoteVanilla();

            return false; // el sprite NUNCA: el rayo ES la tira
        }
    }
}
