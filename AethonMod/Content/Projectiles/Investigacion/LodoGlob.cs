using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigacion
{
    /// <summary>
    /// LODOGLOB — v6.54.0 — EL GLOBO QUE CAE Y SE ROMPE EN CHARCO.
    /// El arco parabólico del lanzado (gravedad vanilla); al tocar
    /// suelo (o enemigo) nace LodoCharco y el globo muere con un
    /// chapoteo de partículas violetas.
    /// </summary>
    public class LodoGlob : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.28f;             // el arco del globo
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            if (Main.netMode != NetmodeID.Server)
            {
                Lighting.AddLight(Projectile.Center, 0.25f, 0.08f, 0.5f);
                if (Main.rand.NextBool(6))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleCrystalShard,
                        -Projectile.velocity * 0.1f, 150, default, 0.7f);
                    d.noGravity = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Romper();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => Romper();

        private void Romper()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                    Projectile.Bottom - Vector2.UnitY * 6f, Vector2.Zero,
                    ModContent.ProjectileType<LodoCharco>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner,
                    0f, 1f);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 12; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleCrystalShard,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.5f, 2.5f), 160, default, 0.9f);
                    d.noGravity = i < 6;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item21.WithPitchOffset(-0.2f),
                    Projectile.Center);
            }
            Projectile.Kill();
        }
    }

    /// <summary>
    /// LODOCHARCO — v6.54.0 — EL CHARCO BIOLUMINISCENTE MORADO.
    ///
    /// La técnica del Dark Sludge original (Task 74-b) SIN render
    /// targets: pegado al suelo, la faja de 7 nodos dibuja la BASE
    /// (LodoOscuro.png — value-noise cuantizado casi negro con tinte
    /// morado) con WARP SENOIDAL del muestreo UV (la superficie ondula
    /// orgánicamente como el shader SludgeEffect) + la capa de GLOW
    /// (LodoOscuroBrillo.png — los puntos bioluminiscentes violetas)
    /// aditiva encima. Los charcos SE ACUMULAN: cada globo que cae
    /// sobre uno existente lo HINCHA (ai[1] += 1, techo 4). Daña a los
    /// enemigos que lo pisan.
    /// </summary>
    public class LodoCharco : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int Vida = 480;
        private const int Nodos = 7;

        /// <summary>La escala de acumulación (ai[1] — crece con cada globo).</summary>
        private float Hinchazon => MathHelper.Clamp(Projectile.ai[1], 1f, 4f);

        private static FajaCPU.Textura _base, _brillo;
        private readonly Vector2[] _espina = new Vector2[Nodos];
        private readonly float[] _anchos = new float[Nodos];
        private readonly Color[] _colores = new Color[Nodos];

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Lighting.AddLight(Projectile.Center,
                    0.3f * Hinchazon * 0.5f, 0.1f, 0.6f * Hinchazon * 0.5f);
                // LOS MOTEOS: alguna mota violeta sube del charco.
                if (Main.rand.NextBool(10))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), -4f),
                        DustID.PurpleCrystalShard, -Vector2.UnitY * Main.rand.NextFloat(0.3f, 0.9f),
                        140, default, 0.6f);
                    d.noGravity = true;
                }
            }

            // === LA FUSIÓN DE CHARCOS (el original se apila — aquí se
            //     HINCHA): un globo que cae sobre el charco lo engorda. ===
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft == Vida - 1)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || i == Projectile.whoAmI ||
                        p.type != Type || p.owner != Projectile.owner)
                        continue;
                    if (p.Center.Distance(Projectile.Center) < 60f)
                    {
                        // el VIEJO se hincha con la juventud del nuevo; el nuevo muere.
                        p.ai[1] = Math.Min(4f, p.ai[1] + Projectile.ai[1]);
                        p.timeLeft = Math.Max(p.timeLeft, Vida);
                        Projectile.Kill();
                        return;
                    }
                }
            }

            // === EL DAÑO: los enemigos que PISAN el charco ===
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 22f == 0f)
            {
                float radio = 34f * Hinchazon;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;
                    if (npc.Bottom.Y < Projectile.Center.Y - 30f) continue;   // debe PISARLO
                    if (npc.Center.Distance(Projectile.Center) > radio) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        Projectile.damage, 2f, unico: false);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                if (_base == null)
                {
                    _base = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/LodoOscuro");
                    _brillo = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/LodoOscuroBrillo");
                }

                float t = Main.GlobalTimeWrappedHourly;
                float anchoTotal = 68f * Hinchazon;
                float vida01 = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);

                // === LA CAPA BASE (el lodo oscuro con WARP senoidal) ===
                for (int i = 0; i < Nodos; i++)
                {
                    float p = i / (float)(Nodos - 1);
                    float x = (p - 0.5f) * anchoTotal;
                    _espina[i] = Projectile.Center + new Vector2(x, 0f) - Main.screenPosition;
                    // EL SEMIELIPSE del charco.
                    _anchos[i] = MathF.Sin(p * MathHelper.Pi) * 9f * Hinchazon;

                    // EL WARP SENOIDAL del shader SludgeEffect (la
                    // superficie ondula: sin((y+t)·80)·0.015).
                    float warpU = p * 3f + MathF.Sin(t * 1.4f + p * 9f) * 0.06f
                        + MathF.Sin(t * 0.7f) * 0.02f;
                    float v = 0.5f + MathF.Sin(p * MathHelper.Pi) * 0.3f;
                    float m = _base.M(warpU, v);

                    // LA TINTA MORADA: la base casi negra × el violeta de la casa.
                    int r = (int)(38 * m + 14);
                    int g = (int)(16 * m + 5);
                    int b = (int)(70 * m + 26);
                    _colores[i] = new Color((int)(r * vida01), (int)(g * vida01),
                        (int)(b * vida01), 255);
                }
                FajaCPU.Dibujar(_espina, _anchos, _colores);

                // === LA CAPA BIOLUMINISCENTE (los puntos violetas) ===
                for (int i = 0; i < Nodos; i++)
                {
                    float p = i / (float)(Nodos - 1);
                    float m = _brillo.M(p * 2.5f + t * 0.05f, 0.5f + MathF.Sin(p * 3.1f) * 0.2f);
                    float latido = 0.7f + 0.3f * MathF.Sin(t * 3f + p * 6f);
                    _colores[i] = new Color(
                        (int)(110 * m * latido * vida01),
                        (int)(40 * m * latido * vida01),
                        (int)(235 * m * latido * vida01), 255);
                }
                FajaCPU.Dibujar(_espina, _anchos, _colores);
            }
            catch { }
            return false;
        }
    }
}
