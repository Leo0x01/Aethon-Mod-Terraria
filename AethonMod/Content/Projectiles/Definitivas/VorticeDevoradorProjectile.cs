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
    /// VORTICEDEVORADORPROJECTILE — v6.50.66 — ARMA 9: EL VÓRTICE
    /// DEVORADOR.
    ///
    /// La cara SOMBRAS de la bolsa (la receta del «Subsuming Vortex» de
    /// Calamity + el «Goomoire» de el dios antiguo, con el lenguaje de la casa):
    /// la semilla de tinta VUELA al cursor, GERMINA en un remolino
    /// negro-violeta y vive 240 t devorando —
    ///
    ///   · LA SUCCIÓN: los enemigos en 560 px son ARRASTRADOS al centro
    ///     (empuje creciente hacia la garganta — server-side).
    ///   · EL MOLINO: la espiral de masa gira con sus OJOS abiertos
    ///     (SombrasLib.OjosDeMasa sobre la espiral) y sus GARRAS por
    ///     fuera.
    ///   · EL TRITURADO: daño continuo a lo que toque el núcleo (el
    ///     hitbox gordo del centro).
    ///   · LA IMPLOSIÓN (al final): TODO colapsa hacia dentro (5 t) y
    ///     ESTALLA en almas + onda violeta — el pago por mirar de cerca.
    ///
    /// ai[1] = 0 (vuelo) · 1 (devorar) · 2 (implosión).
    /// </summary>
    public class VorticeDevoradorProjectile : ModProjectile
    {
        private const int TDevorar = 240;
        private const float RadioSucción = 560f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private int _semilla;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40 + TDevorar + 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0) _semilla = (int)Main.GameUpdateCount;

            switch (Projectile.ai[1])
            {
                case 0:     // ==================== EL VUELO DE LA SEMILLA ====================
                {
                    Projectile.velocity *= 0.93f;
                    if (Projectile.velocity.LengthSquared() < 2.5f || t > 26)
                    {
                        Projectile.ai[1] = 1f;
                        Projectile.ai[2] = 0;
                        Projectile.velocity = Vector2.Zero;
                        DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.6f).WithVolumeScale(0.9f), Projectile.Center);
                        OndaLib.Kick(7, 12);
                    }
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                        SombrasLib.Bruma(Projectile.Center, 30f, 0.5f, tiempo, _semille(t));
                    break;
                }

                case 1:     // ==================== EL DEVORAR ====================
                {
                    // LA SUCCIÓN (server-side: mueve NPCs de verdad)
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                            if (n.friendly || n.townNPC) continue;
                            if (n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type]) continue;   // los jefes RESISTEN
                            float d = Vector2.Distance(n.Center, Projectile.Center);
                            if (d > RadioSucción || d < 30f) continue;
                            Vector2 jala = (Projectile.Center - n.Center).SafeNormalize(Vector2.UnitX);
                            float fuerza = MathHelper.Lerp(0.42f, 0.08f, d / RadioSucción);
                            n.velocity += jala * fuerza;
                            n.position -= n.velocity * 0.06f;
                        }
                    }

                    // LA RESPIRACIÓN del molino (chispas + bruma continua)
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        if (Main.rand.NextBool(3))
                        {
                            float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                            float radio = Main.rand.NextFloat(80f, 400f);
                            Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            Vector2 vel = (Projectile.Center - pos).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(4f, 10f);
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 0, SombrasLib.HumoVioleta, 0.9f);
                            d.noGravity = true;
                        }
                        SombrasLib.Bruma(Projectile.Center, 200f, 0.35f, tiempo, _semille(t));
                        Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.9f);
                    }
                    break;
                }

                case 2:     // ==================== LA IMPLOSIÓN ====================
                {
                    if (t < 2f)
                    {
                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(1.0f), Projectile.Center);
                        OndaLib.Flash(new Color(180, 110, 255), 0.30f, 12, Projectile.Center);
                    }
                    // EL COLAPSO: todo lo vivo se arrastra AL CENTRO (los 5 t finales)
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && t < 10 && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radio = 300f * (1f - t / 10f) + 40f;
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                            (Projectile.Center - pos) * 0.12f, 0, SombrasLib.Violeta, 1.1f);
                        d.noGravity = true;
                    }
                    if (t == 10)
                    {
                        // ¡EL ESTALLIDO! (el golpe final gordo: 500 px)
                        Projectile.position = Projectile.Center - new Vector2(250f, 250f);
                        Projectile.width = 500;
                        Projectile.height = 500;
                        Projectile.localNPCHitCooldown = 2;
                        Projectile.netUpdate = true;
                        DefinitivasLib.Sonar(SoundID.Item88.WithPitchOffset(-0.3f).WithVolumeScale(1.0f), Projectile.Center);
                        OndaLib.Kick(13, 16);

                        if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                        {
                            for (int i = 0; i < 46; i++)
                            {
                                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(3f, 15f);
                                if (i % 3 == 0) SombrasLib.Alma(Projectile.Center + vel * 6f, 10f + Main.rand.NextFloat(8f), 0.9f);
                                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0,
                                    i % 2 == 0 ? SombrasLib.Violeta : DefinitivasLib.Oro, 1.2f);
                                d.noGravity = true;
                            }
                        }
                    }
                    if (t > 34) Projectile.Kill();
                    break;
                }
            }

            // EL FIN DEL DEVORAR → implosión
            if (Projectile.ai[1] == 1f && Projectile.ai[2] > TDevorar)
            {
                Projectile.ai[1] = 2f;
                Projectile.ai[2] = 0;
            }
        }

        private static int _semille(float t) => (int)(t * 0.5f);

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            byte fase = (byte)Projectile.ai[1];

            // ==================== EL MOLINO (la espiral devoradora) ====================
            if (fase == 1 || (fase == 2 && t < 10))
            {
                float nace = fase == 1 ? DefinitivasLib.DeGolpe(MathHelper.Clamp(t / 22f, 0f, 1f)) : 1f;
                float colapso = fase == 2 ? 1f - MathHelper.Clamp(t / 10f, 0f, 1f) : 1f;
                float crece = nace * colapso;

                // LA ESPIRAL: tres brazos de MASA que giran hacia dentro
                for (int brazo = 0; brazo < 3; brazo++)
                {
                    const int SEG = 12;
                    var col = new Vector2[SEG];
                    for (int i = 0; i < SEG; i++)
                    {
                        float q = i / (float)(SEG - 1);
                        float ang = -tiempo * 2.4f - q * 3.6f + brazo * 2.094f;
                        float radio = (30f + q * 230f) * crece;
                        col[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * radio;
                    }
                    SombrasLib.Masa(col, 26f * crece, 6f, 0.8f, _semilla + brazo, tiempo);
                    SombrasLib.BrumaColumna(col, 30f * crece, 0.35f, tiempo, _semilla + brazo, 5);
                }

                // EL OJO DE LA GARGANTA (te mira mientras te come)
                VFXCore.Begin();
                SombrasLib.Ojo(Projectile.Center, 26f * crece,
                    Main.player[Projectile.owner].MountedCenter - Projectile.Center, 0.9f * crece);
                // EL DISCO DE ACRECIÓN (el rim violeta que hace visible lo negro)
                if (VFXCore.Ring != null)
                {
                    DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Ring, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(SombrasLib.Violeta, 0.7f * crece), new Vector2(340f * crece, 280f * crece), tiempo * 0.4f);
                    DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Ring, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.25f * crece), new Vector2(500f * crece, 420f * crece), -tiempo * 0.2f);
                }
                VFXCore.FlushAdditive();

                // LAS GARRAS del borde (cuatro zarpas que se ABREN y CIERRAN)
                for (int k = 0; k < 4; k++)
                {
                    float ang = tiempo * 0.8f + k * MathHelper.PiOver2;
                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang) * 0.8f);
                    float abre = 0.5f + 0.5f * MathF.Sin(tiempo * 2.2f + k * 1.4f);
                    Vector2 posGarra = Projectile.Center + dir * (180f * crece + 40f * abre);
                    SombrasLib.Garra(posGarra, -dir, 90f * crece, 0.85f * crece, (k % 2 == 0 ? 1f : -1f) * 0.5f);
                }
            }

            // ==================== LA IMPLOSIÓN (el estallido violeta) ====================
            if (fase == 2 && t >= 10 && t < 26)
            {
                float vida = (t - 10f) / 16f;
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(SombrasLib.Violeta, 0.5f * (1f - vida)),
                    new Vector2(1100f, 1100f) * (0.4f + vida * 0.6f));
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.55f * (1f - vida)),
                    new Vector2(460f, 460f) * (0.5f + vida * 0.5f));
                if (VFXCore.DestelloFinal != null)
                    DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.85f * (1f - vida)), new Vector2(640f, 640f), vida * 1.3f);
                VFXCore.FlushAdditive();

                if (Main.spriteBatch != null)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida, 900f,
                            SombrasLib.Violeta, 1f, 92, 18f, OndaFalloff.Quadratic, true);
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }
            }
            return false;
        }
    }
}
