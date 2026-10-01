using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Jefes
{
    /// <summary>
    /// AtaqueJefe2Projectile — v6.50.48 — EL ARSENAL DE LA SEGUNDA LUZ.
    ///
    /// El set de AETHON, LA SEGUNDA LUZ (el jefe nuevo del invocador
    /// numero 2): cinco fases que AÑADEN armas (nunca retiran — la
    /// letra: «en cada fase tiene nuevos proyectiles mas lo de las
    /// fases anteriores»):
    ///
    ///   0  ASTILLA — el perno recto (P1): la ráfaga a la predicción.
    ///   1  PRISMA — el perno refractado (P2): uno de TRES colores (el
    ///      croma viaja en ai[1]: 0 rosa · 1 cian · 2 oro) con curva
    ///      suave hacia la presa — la dispersión que converge.
    ///   2  ANILLO SOLAR — la onda (P3): nace alrededor de la presa y
    ///      se ABRE frenando (el aro que empuja afuera).
    ///   3  LLUVIA PRISMÁTICA — la andanada del cielo (P4): cae con
    ///      gravedad de verdad y chispea al caer (el croma en ai[1]).
    ///   4  CORONA — la galaxia (P5, la furia): tangencial + colapso
    ///      (la receta del vórtice de la primera luz, en prisma).
    ///   5  CAMBIO DE PRISMA (cosmético, daño 0): el anillo que recorre
    ///      a la segunda luz cuando SUBE DE FASE — el Decreto de ella.
    ///
    /// LA LEY DE ORO DE LA .46 (intacta): el estilo vive en ai[0] y
    /// NADIE lo toca; el croma/param viaja en ai[1]; la semilla en
    /// ai[2]; el estado se computa de la edad. Daño por COLISIÓN del
    /// motor, cero Main.rand en el render, SIN hide, el contrato de
    /// lote cerrado->cerrado de la casa.
    /// </summary>
    public class AtaqueJefe2Projectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]) ===
        public const int EstiloAstilla = 0;
        public const int EstiloPrisma = 1;
        public const int EstiloAnilloSolar = 2;
        public const int EstiloLluvia = 3;
        public const int EstiloCorona = 4;
        public const int EstiloCambioPrisma = 5;

        /// <summary>El estilo (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>El croma/param (ai[1]: el prisma 0 rosa · 1 cian · 2 oro).</summary>
        private float Par => Projectile.ai[1];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === EL ESTADO ===
        private float _edad;
        private Vector2 _posAnterior;

        // LA PALETA DEL PRISMA (el croma de la segunda luz).
        private static readonly Color RosaPrisma = new(255, 190, 235);
        private static readonly Color CianPrisma = new(170, 240, 255);
        private static readonly Color OroPrisma = new(255, 240, 190);
        private static readonly Color BlancoSegunda = new(255, 253, 246);

        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.8f;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _posAnterior = Projectile.Center;
        }

        /// <summary>La presa más cercana.</summary>
        private Player Presa()
        {
            Player mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            _edad++;
            if (_posAnterior == Vector2.Zero) _posAnterior = Projectile.Center;
            Player presa = Presa();

            switch (Estilo)
            {
                // =============================================================
                //  (0) LA ASTILLA — el perno recto con el vaivén mínimo
                // =============================================================
                case EstiloAstilla:
                {
                    // el vaivén perpendicular (determinista por semilla).
                    Vector2 lado = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X)
                        .SafeNormalize(Vector2.Zero);
                    Projectile.Center += lado * MathF.Sin(_edad * 0.31f + Seed) * 0.9f;
                    break;
                }

                // =============================================================
                //  (1) EL PRISMA — el perno refractado que CONVERGE (curva
                //      suave hacia la presa: la dispersión cromática)
                // =============================================================
                case EstiloPrisma:
                {
                    if (presa != null && _edad > 6 && _edad < 70)
                    {
                        Vector2 deseada = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                            * Projectile.velocity.Length();
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.030f);
                    }
                    break;
                }

                // =============================================================
                //  (2) EL ANILLO SOLAR — la onda que se ABRE frenando
                // =============================================================
                case EstiloAnilloSolar:
                {
                    Projectile.velocity *= 0.965f;   // el aro se frena: la onda linge
                    break;
                }

                // =============================================================
                //  (3) LA LLUVIA PRISMÁTICA — la andanada del cielo (cae
                //      con gravedad de verdad y chispea)
                // =============================================================
                case EstiloLluvia:
                {
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.09f, 16f);
                    if (!Main.dedServ && _edad % 5 == 0)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                            Vector2.Zero, 160, ColorDelCroma((int)Par) * 0.6f, 0.7f);
                        d.noGravity = true;
                    }
                    break;
                }

                // =============================================================
                //  (4) LA CORONA — la galaxia: tangencial + HUNDIMIENTO
                //      (el remolino que gira y colapsa)
                // =============================================================
                case EstiloCorona:
                {
                    if (presa != null)
                    {
                        Vector2 dir = (presa.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                        Projectile.velocity += dir * 0.055f;
                        float vel = Projectile.velocity.Length();
                        if (vel > 11f) Projectile.velocity *= 11f / vel;
                    }
                    break;
                }

                // =============================================================
                //  (5) EL CAMBIO DE PRISMA (cosmético: daño 0, empuja a
                //      nadie — el anillo de la subida de fase)
                // =============================================================
                case EstiloCambioPrisma:
                {
                    Projectile.velocity = Vector2.Zero;
                    break;
                }
            }

            _posAnterior = Projectile.Center;

            // LA LUZ del proyectil (el color de su croma).
            if (Estilo == EstiloCambioPrisma)
                Lighting.AddLight(Projectile.Center, new Vector3(1.0f, 0.85f, 0.95f));
            else
            {
                Color c = ColorDelCroma((int)Par);
                Lighting.AddLight(Projectile.Center, new Vector3(c.R / 255f * 0.9f, c.G / 255f * 0.9f, c.B / 255f * 0.9f));
            }
        }

        /// <summary>El color del croma (0 rosa · 1 cian · 2 oro · lo demás: blanco).</summary>
        private static Color ColorDelCroma(int croma)
        {
            return croma switch
            {
                0 => RosaPrisma,
                1 => CianPrisma,
                2 => OroPrisma,
                _ => BlancoSegunda,
            };
        }

        // ==================================================================
        //  EL RENDER — coords de MUNDO al búfer + el lote aditivo de
        //  pantalla; el contrato de lote cerrado->cerrado de la casa.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Color croma = Estilo == EstiloCambioPrisma
                    ? BlancoSegunda : ColorDelCroma((int)Par);

                // === FASE 1 — EL BÚFER DE QUADS (coords de MUNDO) ===
                switch (Estilo)
                {
                    case EstiloAstilla:
                        VFXCore.Quad(Projectile.Center, Tint(croma, 0.85f),
                            new Vector2(26f, 8f), Projectile.velocity.ToRotation());
                        VFXCore.Quad(Projectile.Center, Tint(BlancoSegunda, 0.7f),
                            new Vector2(12f, 4f), Projectile.velocity.ToRotation());
                        break;

                    case EstiloPrisma:
                        VFXCore.Quad(Projectile.Center, Tint(croma, 0.85f),
                            new Vector2(22f, 9f), Projectile.velocity.ToRotation());
                        VFXCore.Line(_posAnterior - Projectile.velocity * 2f, Projectile.Center,
                            Tint(BlancoSegunda, 0.35f), 2.6f);
                        break;

                    case EstiloAnilloSolar:
                    {
                        float abre = MathF.Min(1f, _edad / 60f);
                        VFXCore.Quad(Projectile.Center, Tint(croma, 0.55f + 0.25f * (1f - abre)),
                            new Vector2(30f, 30f));
                        VFXCore.Quad(Projectile.Center, Tint(BlancoSegunda, 0.6f),
                            new Vector2(13f, 13f));
                        break;
                    }

                    case EstiloLluvia:
                        VFXCore.Quad(Projectile.Center, Tint(croma, 0.9f),
                            new Vector2(10f, 26f), Projectile.velocity.ToRotation() + MathHelper.PiOver2);
                        VFXCore.Quad(Projectile.Center, Tint(BlancoSegunda, 0.65f),
                            new Vector2(5f, 14f), Projectile.velocity.ToRotation() + MathHelper.PiOver2);
                        break;

                    case EstiloCorona:
                        VFXCore.Quad(Projectile.Center, Tint(croma, 0.85f), new Vector2(20f, 20f));
                        VFXCore.Quad(Projectile.Center, Tint(BlancoSegunda, 0.6f), new Vector2(9f, 9f));
                        break;

                    case EstiloCambioPrisma:
                    {
                        // EL ANILLO DE LA SUBIDA: nace pegado a la luz y SE
                        // ABRE (el radio es función de la edad — la ley).
                        float radio = MathF.Min(1f, _edad / 44f);
                        float r = 60f + 380f * radio;
                        float alfa = 0.5f * (1f - radio);
                        for (int i = 0; i < 3; i++)
                        {
                            float ang = radio * MathHelper.TwoPi * 1.4f + i * MathHelper.TwoPi / 3f;
                            Vector2 punto = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            VFXCore.Quad(punto, Tint(i == 0 ? RosaPrisma : (i == 1 ? CianPrisma : OroPrisma), alfa),
                                new Vector2(22f, 6f), ang, VFXCore.SoftGlow);
                        }
                        break;
                    }
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE PANTALLA ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    switch (Estilo)
                    {
                        case EstiloAstilla:
                            LumenLib.Bloom(Main.spriteBatch, pos, 30f, croma, 0.55f);
                            break;

                        case EstiloPrisma:
                            LumenLib.Bloom(Main.spriteBatch, pos, 32f, croma, 0.6f);
                            LumenLib.Bloom(Main.spriteBatch, pos, 12f, BlancoSegunda, 0.7f);
                            break;

                        case EstiloAnilloSolar:
                        {
                            float abre = MathF.Min(1f, _edad / 60f);
                            OrbitaLib.AnilloFino(pos, 40f + 60f * abre, t * 2.2f,
                                OrbitaLib.Tint(croma, 0.5f * (1f - abre)));
                            LumenLib.Bloom(Main.spriteBatch, pos, 44f, croma, 0.5f * (1f - abre));
                            break;
                        }

                        case EstiloLluvia:
                            LumenLib.Bloom(Main.spriteBatch, pos, 30f, croma, 0.6f);
                            break;

                        case EstiloCorona:
                            LumenLib.Bloom(Main.spriteBatch, pos, 36f, croma, 0.7f);
                            OrbitaLib.AnilloFino(pos, 26f, t * 3.1f, OrbitaLib.Tint(BlancoSegunda, 0.4f));
                            break;

                        case EstiloCambioPrisma:
                            LumenLib.Bloom(Main.spriteBatch, pos, 60f, BlancoSegunda,
                                0.35f * (1f - MathF.Min(1f, _edad / 44f)));
                            break;
                    }
                }
                finally { Main.spriteBatch.End(); }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }

        /// <summary>El tinte con alpha (el helper local).</summary>
        private static Color Tint(Color c, float alfa)
        {
            Color r = c;
            r.A = (byte)(255 * MathHelper.Clamp(alfa, 0f, 1f));
            return r;
        }
    }
}
