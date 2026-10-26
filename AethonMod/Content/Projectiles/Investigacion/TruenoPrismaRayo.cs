using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigacion
{
    /// <summary>
    /// TRUENOPRISMARAYO — v6.54.0 — LA POLILÍNEA QUE SE AQUIETA.
    ///
    /// La técnica del GooLightning del jefe (Task 74-b), 100 % casa:
    ///   · LA POLILÍNEA: 3 puntos — origen (600 px sobre el objetivo),
    ///     medio (jitter circular ±(100, 300)) y fin (el objetivo) —
    ///     curva cuadrática muestreada en 26 nodos.
    ///   · LA RELAJACIÓN (GooLightning.cs:123-127): cada tick los
    ///     puntos LERPAN hacia la recta — el rayo CRUJE al nacer y se
    ///     aquieta: la firma que lo hace vivo.
    ///   · EL DIBUJO: por nodo, DOS MUESTRAS de RayoRamo.png desfazadas
    ///     0.5 en U MULTIPLICADAS (solo sobreviven las vetas que
    ///     coinciden — el truco del shader original) + el halo
    ///     RayoRamoBrillo, con el ARCOÍRIS del dios desvaneciéndose
    ///     (1−t/30)^0.6 — la fórmula del jefe.
    ///   · EL DAÑO: línea AABB con grosor 50·grande·(1−t/60) — la
    ///     hitbox también se aquieta (el jefe: t=0..40).
    /// </summary>
    public class TruenoPrismaRayo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int Vida = 40;
        private const int Nodos = 26;

        // ai[0] = el objetivo whoAmI · ai[1] = ¿es el rayo grande? (1/0)
        private int Objetivo => (int)Projectile.ai[0];
        private bool Grande => Projectile.ai[1] > 0.5f;

        // LA POLILÍNEA VIVA (campos locales — el visual es de cada
        // cliente, como los drivers de las alas de la casa).
        private Vector2 _origen, _medio, _fin;
        private bool _nacio;
        private Vector2[] _espina = new Vector2[Nodos];
        private float[] _anchos = new float[Nodos];
        private Color[] _colores = new Color[Nodos];

        private static FajaCPU.Textura _fil, _glow;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;      // el rayo del jefe ATRAVIESA
            Projectile.ignoreWater = true;
            Projectile.netImportant = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!_nacio)
            {
                _nacio = true;
                NPC obj = Main.npc[Objetivo];
                _fin = obj != null && obj.active ? obj.Center : Projectile.Center;
                _origen = _fin - Vector2.UnitY * 600f;
                _medio = (_origen + _fin) * 0.5f +
                    new Vector2(Main.rand.NextFloat(-100f, 100f), Main.rand.NextFloat(-300f, 300f));
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        (Grande ? SoundID.Thunder : SoundID.Item92).WithPitchOffset(
                            Grande ? -0.3f : 0.25f), _fin);
                    Lighting.AddLight(_fin, 0.6f, 0.2f, 1f);
                }
            }

            // === LA RELAJACIÓN (el rayo se AQUIETA — la firma del jefe) ===
            float calma = 1f - Projectile.timeLeft / (float)Vida;    // 0→1
            Vector2 rectaMedio = (_origen + _fin) * 0.5f;
            _medio = Vector2.Lerp(_medio, rectaMedio, 0.16f);
            _origen = Vector2.Lerp(_origen, _origen * 0.999f + _fin * 0.001f, 0.05f);

            // === EL DAÑO POR LÍNEA (el jefe: grosor que decae) ===
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft > Vida - 30)
            {
                float grosor = Math.Max(10f, 50f * (Grande ? 1f : 0.5f) * (1f - calma * 0.6f));
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;
                    float puntoColision = 0f;
                    if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size,
                            _origen, _fin, grosor, ref puntoColision))
                        continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        Projectile.damage, Projectile.knockBack);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server || !_nacio) return false;
            try
            {
                if (_fil == null)
                {
                    _fil = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/RayoRamo");
                    _glow = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/RayoRamoBrillo");
                }

                float t = Vida - Projectile.timeLeft;
                float reloj = Main.GlobalTimeWrappedHourly * 2f + Projectile.whoAmI * 0.37f;
                // EL DESVANECIMIENTO DEL JEFE: (1−t/30)^0.6
                float vida01 = MathF.Pow(MathHelper.Clamp(1f - t / 30f, 0f, 1f), 0.6f);
                float anchoBase = (Grande ? 26f : 13f) * (0.6f + 0.4f * vida01);

                for (int i = 0; i < Nodos; i++)
                {
                    float p = i / (float)(Nodos - 1);
                    // LA CURVA CUADRÁTICA de los 3 puntos.
                    Vector2 ab = Vector2.Lerp(_origen, _medio, p);
                    Vector2 bc = Vector2.Lerp(_medio, _fin, p);
                    Vector2 pt = Vector2.Lerp(ab, bc, p);
                    _espina[i] = pt - Main.screenPosition;
                    _anchos[i] = anchoBase * MathF.Sin(p * MathHelper.Pi) * 0.7f
                        + anchoBase * 0.3f;

                    // EL ARCOÍRIS DEL DIOS a lo largo (el desvanecido del jefe).
                    Color arco = ColorPrisma.ALoLargo(reloj * 6f, p, 60f);

                    // LA TÉCNICA DEL SHADER: dos muestras desfazadas 0.5
                    // en U MULTIPLICADAS — solo las vetas que coinciden.
                    float m1 = _fil.M(reloj * 0.8f + p, 0.5f);
                    float m2 = _fil.M(reloj * 0.8f + p + 0.5f, 0.5f);
                    float fil = m1 * m2 * 2.2f;
                    float halo = _glow.M(reloj * 0.5f + p, 0.5f);

                    float brillo = Math.Min(1f, fil * 1.6f + halo * 0.7f);
                    int r = (int)Math.Min(255, arco.R * brillo + 255 * fil * 0.5f);
                    int g = (int)Math.Min(255, arco.G * brillo + 255 * fil * 0.5f);
                    int b = (int)Math.Min(255, arco.B * brillo + 255 * fil * 0.5f);
                    _colores[i] = new Color(
                        (int)(r * vida01), (int)(g * vida01), (int)(b * vida01), 255);
                }

                FajaCPU.Dibujar(_espina, _anchos, _colores);
            }
            catch { }
            return false;
        }
    }
}
