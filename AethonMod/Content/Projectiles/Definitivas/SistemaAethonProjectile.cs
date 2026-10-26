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
    /// SISTEMAAETHONPROJECTILE — v6.50.66 — ARMA 7: EL SISTEMA DE AETHON.
    ///
    /// La receta de la «el dios antiguoga» de el dios antiguo (lanzas planetas que orbitan
    /// al enemigo) + el «Ultima Thule» de Stars Above (tu propio sistema
    /// solar): CINCO CUERPOS orbitan al portador —
    ///
    ///   0. EL SOL (oro, FireRing)      — dispara bolas de fuego lentas y gordas
    ///   1. LA LUNA (plata, Crescent)   — las cuchillas crecientes que rebotan
    ///   2. EL COMETA (cian, CometHead) — ráfagas rápidas con estela
    ///   3. LA ESTRELLA (violeta, Star) — dardos con homing suave
    ///   4. EL VACÍO (negro, rim)       — chispas que IMPLEN (mini colapso)
    ///
    /// Cada cuerpo dispara SOLO a su presa más cercana cada ~42 t. CLIC
    /// DERECHO (el item lo ordena): EL BOMBARDEO — los cinco salen
    /// DISPARADOS hacia el cursor y DETONAN al llegar (FireRing + Shock +
    /// Kick, cada uno con su firma).
    ///
    /// ai[1] = 0 (órbita) · 1 (bombardeo).
    /// </summary>
    public class SistemaAethonProjectile : ModProjectile
    {
        private const int NCuerpos = 5;
        private const float VidaOrbita = 620f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // Los offsets cromáticos y de órbita de cada cuerpo (radios, velocidades angulares)
        private static readonly float[] Radio = { 118f, 88f, 148f, 104f, 74f };
        private static readonly float[] VelAng = { 0.030f, -0.042f, 0.055f, -0.026f, 0.062f };
        private static readonly float[] Tamano = { 54f, 40f, 30f, 34f, 44f };

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)VidaOrbita + 90;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.netImportant = true;
        }

        /// <summary>La posición de un cuerpo (mundo). En bombardeo, viaja al cursor.</summary>
        public Vector2 PosCuerpo(int k, float tiempo)
        {
            float ang = tiempo * VelAng[k] + k * 2.19f;
            Vector2 orb = new(MathF.Cos(ang) * Radio[k], MathF.Sin(ang) * Radio[k] * 0.62f);
            if (Projectile.ai[1] < 1f)
                return Projectile.Center + orb;

            // EL BOMBARDEO: cada cuerpo VUELA al cursor con retardo escalonado
            float t = Projectile.localAI[0];
            float arranca = 4f + k * 5f;
            float vuelo = MathHelper.Clamp((t - arranca) / 20f, 0f, 1f);
            float eased = DefinitivasLib.DeGolpe(vuelo);
            Vector2 inicio = Projectile.Center + orb;
            Vector2 fin = new(Projectile.localAI[1], Projectile.localAI[2]);
            return Vector2.Lerp(inicio, fin, eased);
        }

        public override void AI()
        {
            float t = Projectile.ai[2]++;
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player dueño = Main.player[Projectile.owner];

            if (Projectile.ai[1] < 1f)
            {
                // ==================== LA ÓRBITA ====================
                Projectile.Center = dueño.MountedCenter + new Vector2(0f, -46f);
                Projectile.localAI[0] = t;

                // EL DISPARO AUTOMÁTICO: cada cuerpo busca SU presa y dispara
                if (Main.netMode != NetmodeID.MultiplayerClient && t > 30 && t % 42 == 0)
                {
                    for (int k = 0; k < NCuerpos; k++)
                    {
                        Vector2 desde = PosCuerpo(k, tiempo);
                        NPC presa = DefinitivasLib.BuscarPresa(desde, false);
                        if (presa == null || Vector2.DistanceSquared(presa.Center, desde) > 1400f * 1400f) continue;

                        Vector2 v = (presa.Center - desde).SafeNormalize(Vector2.UnitX) * (k == 2 ? 26f : k == 0 ? 9f : 15f);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), desde, v,
                            ModContent.ProjectileType<SistemaTiroProjectile>(),
                            (int)(Projectile.damage * (k == 0 ? 1.6f : k == 4 ? 1.3f : 1f)),
                            Projectile.knockBack * 0.5f, Projectile.owner, k);
                    }
                }

                if (!Main.dedServ && Main.netMode != NetmodeID.Server && t % 6 == 0)
                {
                    Vector2 pos = PosCuerpo(0, tiempo);
                    Lighting.AddLight(pos, 1.2f, 1.0f, 0.5f);
                }

                // El sistema se disuelve si el portador se va
                if (t > VidaOrbita) Projectile.Kill();
            }
            else
            {
                // ==================== EL BOMBARDEO ====================
                float tb = Projectile.localAI[0] = Projectile.localAI[0] + 1f;

                // LA ORDEN se ancla donde estaba el cursor al recibirla
                if (tb < 2f)
                {
                    Projectile.localAI[1] = Main.MouseWorld.X;
                    Projectile.localAI[2] = Main.MouseWorld.Y;
                    Projectile.netUpdate = true;
                }

                // LAS DETONACIONES (cada cuerpo al LLEGAR: t = arranca + 20)
                for (int k = 0; k < NCuerpos; k++)
                {
                    float momento = 4f + k * 5f + 20f;
                    if (MathF.Abs(tb - momento) < 0.5f)
                    {
                        Vector2 pos = PosCuerpo(k, tiempo);
                        DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.4f + k * 0.12f).WithVolumeScale(0.8f), pos);
                        OndaLib.Kick(8, 10);

                        // EL GOLPE del bombardeo: un proyectil de explosión real por cuerpo
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, Vector2.Zero,
                                ModContent.ProjectileType<SistemaDetonacionProjectile>(),
                                (int)(Projectile.damage * 3.1f), Projectile.knockBack, Projectile.owner, k);

                        if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                        {
                            for (int i = 0; i < 22; i++)
                            {
                                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(3f, 11f), 0,
                                    ColorDeCuerpo(k), 1.1f);
                                d.noGravity = true;
                            }
                        }
                    }
                }

                if (tb > 4f + NCuerpos * 5f + 20f + 26f)
                    Projectile.Kill();
            }
        }

        /// <summary>EL COLOR de cada cuerpo (la firma).</summary>
        public static Color ColorDeCuerpo(int k) => k switch
        {
            0 => DefinitivasLib.Oro,
            1 => new Color(216, 226, 240),      // plata lunar
            2 => DefinitivasLib.Cian,
            3 => DefinitivasLib.Violeta,
            _ => new Color(120, 60, 190),       // el vacío
        };

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player dueño = Main.player[Projectile.owner];

            // ==================== LAS ÓRBITAS (los aros elípticos de la maqueta) ====================
            if (Projectile.ai[1] < 1f)
            {
                VFXCore.Begin();
                for (int k = 0; k < NCuerpos; k++)
                {
                    if (VFXCore.Ring == null) break;
                    // EL ARO: una elipse de 26 puntos (quad Ring escaqueada no sirve: la casa dibuja elipse con puntos)
                    for (int i = 0; i < 26; i++)
                    {
                        float a = i / 26f * MathHelper.TwoPi;
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(a) * Radio[k], MathF.Sin(a) * Radio[k] * 0.62f);
                        VFXCore.Quad(pos, DefinitivasLib.Alfa(ColorDeCuerpo(k), 0.10f + 0.05f * MathF.Sin(tiempo * 2f + k)), new Vector2(9f, 9f));
                    }
                }
                VFXCore.FlushAdditive();
            }

            // ==================== LOS CUERPOS ====================
            for (int k = 0; k < NCuerpos; k++)
            {
                Vector2 pos = PosCuerpo(k, tiempo);
                Color c = ColorDeCuerpo(k);
                float tam = Tamano[k];

                VFXCore.Begin();
                if (k == 4)
                {
                    // EL VACÍO: disco negro con RIM violeta (la lección de la .65: negro puro = invisible)
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.5f), new Vector2(tam * 1.9f, tam * 1.9f));
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(Color.Black, 0.95f), new Vector2(tam * 1.15f, tam * 1.15f));
                }
                else
                {
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.45f), new Vector2(tam * 2.0f, tam * 2.0f));
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.75f), new Vector2(tam * 0.9f, tam * 0.9f));
                }
                VFXCore.FlushAdditive();

                // EL ROSTRO de cada cuerpo (su textura)
                Texture2D tex = k == 0 ? DefinitivasLib.TexFireRing
                    : k == 1 ? DefinitivasLib.TexCrescent
                    : k == 2 ? DefinitivasLib.TexCometHead
                    : DefinitivasLib.TexStar;
                if (k < 4 && tex != null)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        DefinitivasLib.Blit(Main.spriteBatch, tex, pos - Main.screenPosition,
                            DefinitivasLib.Alfa(k == 1 ? Color.White : c, 0.95f),
                            new Vector2(tam, tam), tiempo * (0.4f + k * 0.15f) * (VelAng[k] > 0 ? 1f : -1f));
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }
            }
            return false;
        }
    }

    /// <summary>
    /// SISTEMATIROPROJECTILE — el disparo de un cuerpo del sistema (ai[0]
    /// = n.º de cuerpo): bola de fuego lenta / creciente rebotona /
    /// cometa rápido con estela / dardo homing / chispa que IMPLODE.
    /// </summary>
    public class SistemaTiroProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 130;
        }

        public override void AI()
        {
            int k = (int)Projectile.ai[0];
            float tiempo = Main.GlobalTimeWrappedHourly;
            Projectile.ai[2]++;

            switch (k)
            {
                case 0:     // EL SOL: bola lenta y gordita que ACELERA al final
                    Projectile.velocity *= 1.012f;
                    break;
                case 2:     // EL COMETA: recto y veloz
                    break;
                case 3:     // LA ESTRELLA: homing suave (el dardo de la casa)
                {
                    NPC presa = DefinitivasLib.BuscarPresa(Projectile.Center, false);
                    if (presa != null)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float vel = Projectile.velocity.Length();
                        Projectile.velocity = (Vector2.Normalize(Projectile.velocity) * 0.9f + hacia * 0.1f).SafeNormalize(Vector2.UnitX) * vel;
                    }
                    break;
                }
                case 4:     // EL VACÍO: se FRENA y colapsa (implode al impactar — Kill con chispas hacia DENTRO)
                    Projectile.velocity *= 0.96f;
                    break;
            }

            if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.04f, 0,
                    SistemaAethonProjectile.ColorDeCuerpo(k), 0.7f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            int k = (int)Projectile.ai[0];
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return;
            Color c = SistemaAethonProjectile.ColorDeCuerpo(k);
            int n = k == 0 ? 18 : 10;
            for (int i = 0; i < n; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                // EL VACÍO implota (las chispas van hacia DENTRO)
                Vector2 vel = k == 4
                    ? new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(6f, 12f) * -1f
                    : new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 8f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, c, 0.9f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            int k = (int)Projectile.ai[0];
            Color c = SistemaAethonProjectile.ColorDeCuerpo(k);
            float tam = k == 0 ? 60f : k == 2 ? 44f : 36f;

            VFXCore.Begin();
            if (k == 4)
            {
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.6f), new Vector2(tam, tam));
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.Black, 0.9f), new Vector2(tam * 0.55f, tam * 0.55f));
            }
            else
            {
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.55f), new Vector2(tam, tam));
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.8f), new Vector2(tam * 0.45f, tam * 0.45f));
            }
            VFXCore.FlushAdditive();
            return false;
        }
    }

    /// <summary>
    /// SISTEMADETONACIONPROJECTILE — la explosión de un cuerpo del
    /// BOMBARDEO: vive 16 t, golpea UNA vez en 220 px de radio (hitbox
    /// gordo) y dibuja SU firma (FireRing + Shock del color del cuerpo).
    /// </summary>
    public class SistemaDetonacionProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.friendly = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            int k = (int)Projectile.ai[0];
            float vida = 1f - Projectile.timeLeft / 16f;
            Color c = SistemaAethonProjectile.ColorDeCuerpo(k);
            float tiempo = Main.GlobalTimeWrappedHourly;

            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.5f * (1f - vida)), new Vector2(560f, 560f) * (0.5f + vida * 0.5f));
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.6f * (1f - vida)), new Vector2(240f, 240f) * (0.6f + vida * 0.4f));
            if (VFXCore.DestelloFinal != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.8f * (1f - vida)), new Vector2(400f, 400f), vida * 1.5f);
            VFXCore.FlushAdditive();

            if (Main.spriteBatch != null && vida < 1f)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida, 300f,
                        c, 0.9f, 40 + k, 12f, OndaFalloff.Quadratic, true);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
