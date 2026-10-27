using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// PRISMAPRIMORDIALPROJECTILE — v6.50.66 — ARMA 2: EL PRISMA
    /// PRIMORDIAL.
    ///
    /// La receta del «Yharim's Crystal» de Calamity (el Last Prism de
    /// vanilla elevado a arma final): SIETE haces finos brotan alrededor
    /// del portador girando despacio y CONVERGEN en un punto de foco que
    /// se acerca a medida que el prisma se calienta — del foco sale EL
    /// HAZ CENTRAL, una columna de luz de hasta 2400 px que CRECE con la
    /// convergencia (más cerca del foco = más gordo) y sigue al cursor
    /// con la suavidad del lerp del original (aim 12%).
    ///
    /// Los SIETE COLORES del prisma rotan por las bocas pequeñas (cada
    /// haz con SU color fijo del espectro, como el arcoíris de vanilla)
    /// y EL CENTRAL los funde a todos (blanco cálido con el borde del
    /// gradiente del Juicio). El daño ESCALA con la convergencia: el
    /// primer segundo es la promesa, el segundo es el veredicto.
    ///
    /// ai[1] = tick de canal.
    /// </summary>
    public class PrismaPrimordialProjectile : ModProjectile
    {
        private const int NHaz = 7;
        private const float LargoHaz = 2400f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private int _semilla;
        private bool _muriendo;       // la muerte suave (independiente del modo sentencia)
        private int _tMuerte;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            float t = Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0) _semilla = (int)Main.GameUpdateCount;

            // EL CANAL: suelto el clic → el prisma se apaga suave
            // (ai[0] = 1 → MODO SENTENCIA: invocado por El Testamento —
            // vive su convergencia completa sin canal)
            bool sentencia = Projectile.ai[0] >= 1f;
            bool vivo = sentencia || (dueño.active && !dueño.dead && dueño.channel &&
                        dueño.HeldItem != null && dueño.HeldItem.type == ModContent.ItemType<Content.Weapons.Definitivas.PrismaPrimordial>());
            if (!vivo) _muriendo = true;      // la muerte suave arranca y ya no se retracta

            // LA MUERTE SUAVE: 12 t de apagado
            if (_muriendo)
            {
                _tMuerte++;
                if (_tMuerte > 12f) { Projectile.Kill(); return; }
            }

            // === LA GEOMETRÍA DEL PRISMA ===
            // El prisma flota frente al portador (a la altura del pecho)
            Vector2 mira = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.Center = dueño.MountedCenter + mira * 26f + new Vector2(0f, -8f);
            Projectile.rotation = mira.ToRotation();

            // LA CONVERGENCIA: 0 → 1 en ~150 t de canal (y se queda)
            float calor = MathHelper.Clamp(t / 150f, 0f, 1f);
            float apagado = _muriendo ? MathHelper.Clamp(1f - _tMuerte / 12f, 0f, 1f) : 1f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], calor * apagado, 0.12f);
            float conv = Projectile.localAI[1];

            // EL DAÑO Y EL ANCHO ESCALAN con la convergencia (el veredicto crece)
            Projectile.damage = (int)(Projectile.originalDamage * (0.45f + 0.85f * conv));

            // LOS SONIDOS: el arrancar del prisma y el zumbar sostenido
            if (t == 2) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(0.0f).WithVolumeScale(0.6f), Projectile.Center);
            if (t == 90 && conv > 0.5f) DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.4f).WithVolumeScale(0.5f), Projectile.Center);

            // === EL DAÑO EN LÍNEA (el haz central golpea TODO su columna) ===
            // (Colliding con la línea gorda del haz central)

            // LAS CHISPAS del foco cuando converge alto
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && conv > 0.35f && Main.rand.NextBool(4))
            {
                Vector2 foco = PosFoco(conv);
                Dust d = Dust.NewDustPerfect(foco, DustID.GoldFlame,
                    new Vector2(-mira.Y, mira.X).RotatedByRandom(0.7f) * Main.rand.NextFloat(1f, 5f), 0,
                    DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.9f);
                d.noGravity = true;
            }
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center + mira * 400f, 1.5f * conv, 1.3f * conv, 1.0f * conv);

            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 30);
        }

        /// <summary>EL FOCO: donde convergen los haces pequeños (más cerca = más convergencia).</summary>
        private Vector2 PosFoco(float conv)
            => Projectile.Center + Projectile.rotation.ToRotationVector2() * MathHelper.Lerp(190f, 46f, conv);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float conv = Projectile.localAI[1];
            if (conv <= 0.08f) return false;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 foco = PosFoco(conv);
            float ancho = 24f + 88f * conv;

            // La línea gorda del foco hacia adelante (el chequeo de segmento de la casa)
            Vector2 fin = foco + dir * LargoHaz;
            return SegmentoGordo(targetHitbox, foco, fin, ancho);
        }

        /// <summary>El chequeo de segmento con grosor (la distancia punto-segmento contra el semiancho).</summary>
        internal static bool SegmentoGordo(Rectangle hitbox, Vector2 a, Vector2 b, float anchoTotal)
        {
            Vector2 c = hitbox.Center();
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            float u = len2 < 1f ? 0f : MathHelper.Clamp(Vector2.Dot(c - a, ab) / len2, 0f, 1f);
            Vector2 punto = a + ab * u;
            float dist = Vector2.Distance(c, punto);
            return dist <= anchoTotal * 0.5f + MathF.Max(hitbox.Width, hitbox.Height) * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[1];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float conv = Projectile.localAI[1];
            if (conv <= 0.02f) return false;

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 foco = PosFoco(conv);

            // ==================== EL PRISMA (la joya que gira) ====================
            VFXCore.Begin();
            for (int i = 0; i < 3; i++)
            {
                float giro = tiempo * (0.7f + i * 0.2f) + i * 2.09f;
                Color cPrisma = DefinitivasLib.GradienteJuicio(tiempo * 0.09f + i * 0.33f);
                VFXCore.Quad(Projectile.Center + perp * MathF.Sin(giro) * 6f,
                    DefinitivasLib.Alfa(cPrisma, 0.30f * conv), new Vector2(64f, 64f), giro);
            }
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.55f * conv), new Vector2(38f, 38f));
            VFXCore.FlushAdditive();

            // ==================== LOS SIETE HACES PEQUEÑOS ====================
            // Cada uno nace en un punto del círculo alrededor del portador y
            // vuela al foco — su color es SU color del espectro (el Last
            // Prism de verdad), el ángulo de nacimiento gira despacio.
            for (int k = 0; k < NHaz; k++)
            {
                float angNacimiento = Projectile.rotation + MathHelper.TwoPi * k / NHaz + tiempo * 0.5f;
                float radioN = MathHelper.Lerp(150f, 26f, conv);
                Vector2 nace = Projectile.Center - dir * 10f + new Vector2(MathF.Cos(angNacimiento), MathF.Sin(angNacimiento)) * radioN;

                // EL COLOR DEL ESPECTRO (7 colores fijos, como el arcoíris de vanilla)
                Color cHaz = ColorDePrisma(k, tiempo);

                // La línea del haz pequeño (tira de primitivas: nace en el círculo, muere en el foco)
                const int M = 10;
                var pts = new Vector2[M];
                for (int i = 0; i < M; i++)
                {
                    float q = i / (float)(M - 1);
                    pts[i] = Vector2.Lerp(nace, foco, q) - Main.screenPosition;
                }
                float anchoK = MathHelper.Lerp(7f, 16f, conv);
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(pts, anchoK, cHaz, DefinitivasLib.BlancoCalido,
                        (0.35f + 0.55f * conv), StormTaper.Linear, _semilla + k, 0, false, false, true, null, null);
                }
                finally { RayoStrip.CerrarLote(); }

                // El puntito de nacimiento de cada haz
                VFXCore.Begin();
                VFXCore.Quad(nace, DefinitivasLib.Alfa(cHaz, 0.7f * conv), new Vector2(26f, 26f));
                VFXCore.FlushAdditive();
            }

            // ==================== EL HAZ CENTRAL ====================
            if (conv > 0.12f)
            {
                const int N = 30;
                var pts = new Vector2[N];
                var ancho = new float[N];
                for (int i = 0; i < N; i++)
                {
                    float p = i / (float)(N - 1);
                    pts[i] = foco + dir * (LargoHaz * p) - Main.screenPosition;
                    // El perfil del haz: fino en el foco, gordo a los 300 px, suave hacia el fondo
                    ancho[i] = MathHelper.Clamp(p / 0.14f, 0f, 1f) * MathHelper.Lerp(1f, 0.42f, MathF.Pow(p, 0.5f));
                    if (ancho[i] < 0.05f) ancho[i] = 0.05f;
                }
                Color gBorde = DefinitivasLib.GradienteJuicio(tiempo * 0.06f);

                RayoStrip.AbrirLote();
                try
                {
                    // LA FUNDA (el borde del gradiente del Juicio)
                    RayoStrip.FilamentoEnLote(pts, (34f + 92f * conv), gBorde, DefinitivasLib.BlancoCalido,
                        0.42f * conv, StormTaper.Flat, _semilla + 20, 0, false, false, false, null, ancho);
                    // LA VENA BLANCA (la columna)
                    RayoStrip.FilamentoEnLote(pts, (16f + 40f * conv), DefinitivasLib.BlancoCalido, Color.White,
                        0.95f * conv, StormTaper.Flat, _semilla + 21, 0, false, false, true, null, ancho);
                }
                finally { RayoStrip.CerrarLote(); }

                // EL CRÁTER DE LUZ en el foco
                VFXCore.Begin();
                VFXCore.Quad(foco, DefinitivasLib.Alfa(gBorde, 0.35f * conv), new Vector2(340f, 340f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.65f * conv), new Vector2(150f, 150f) * conv);
                VFXCore.FlushAdditive();
            }

            // v6.54.5 — el contrato del lote: TODOS los Flush de arriba (prisma,
            // puntitos, cráter) dejaban el lote CERRADO al salir.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>EL COLOR DEL ESPECTRO del haz k (los siete del prisma, vivos).</summary>
        private static Color ColorDePrisma(int k, float tiempo)
        {
            // El espectro de vanilla (Last Prism) con el pulso del Juicio
            return k switch
            {
                0 => new Color(255, 80, 80),      // rojo
                1 => new Color(255, 160, 60),     // naranja
                2 => new Color(255, 230, 100),    // amarillo
                3 => new Color(110, 255, 130),    // verde
                4 => new Color(110, 220, 255),    // cian
                5 => new Color(150, 130, 255),    // azul
                _ => new Color(220, 120, 255),    // violeta
            };
        }
    }
}
