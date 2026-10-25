using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Escriba
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
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, fin, 20f, ref ignorado);
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

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            if (Largo < 16f) return false;

            // === LA PILA COMPLETA (solo ENCOLA — BeamLib jamás toca el
            // lote: CERO riesgo de la clase de crash de la .51.13) ===
            Color color = Main.hslToRgb(Hue(), 1f, 0.72f);
            // LA CARGA DEL PADRE manda en el ANCHO (los fajos separados
            // nacen finos y ENGORDAN al fundirse — la curva del Prism).
            Projectile padre = Main.projectile[PadreWho];
            float carga = padre != null && padre.active
                ? MathHelper.Clamp((padre.ai[0] - 1f) / 179f, 0f, 1f) : 1f;
            float ancho = MathHelper.Lerp(9f, 30f, carga);
            float alfa = Projectile.localAI[2] <= 0f ? 1f : Projectile.localAI[2];
            BeamLib.Rayo(Projectile.Center, Projectile.velocity, Largo, ancho,
                color, Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 31 + Indice * 7,
                ChoquePared, 0.9f, alfa);

            // === EL VOLCADO ÚNICO (EL CONTRATO — la cura v6.51.14) ===
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }
}
