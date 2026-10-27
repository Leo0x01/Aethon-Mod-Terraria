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
    /// LLUVIADEVERSOSPROJECTILE — v6.50.66 — ARMA 3: LA LLUVIA DE VERSOS.
    ///
    /// La receta del «Dance of Light» de Calamity: el CONDUCTOR vive
    /// ~210 t sobre la presa y cada 16 t borda UN PAR de versos — dos
    /// hojas de luz (una ORO, una VIOLETA) que nacen en lados OPUESTOS
    /// del cielo apuntando la una al punto de la otra: SE BUSCAN, se
    /// ATRAVIESAN y siguen de largo, cortando todo el camino. Cada hoja
    /// es un proyectil real (LluviaHojaProjectile) con su estela.
    ///
    /// A LOS DOCE PARES — EL DESTELLO: el conductor SE HACE NOVA (su
    /// hitbox estalla a 1500 px por un instante), la pantalla destella
    /// blanca, la onda de choque doble dorada-violeta cruza el campo y
    /// el temblor remata lo que quede de pie.
    ///
    /// ai[0] = presa+1 · ai[2] = tick.
    /// </summary>
    public class LluviaDeVersosProjectile : ModProjectile
    {
        private const int Pares = 12;
        private const int CadenciaPar = 16;
        private const int Vida = Pares * CadenciaPar + 30;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            bool presaViva = presa != null && presa.active && presa.life > 0;

            // EL ANCLA: el conductor cabalga sobre la presa (o deriva donde murió)
            if (presaViva) Projectile.Center = presa.Center;

            // LA MUERTE DE LA PRESA: el verso se disipa (sin festín aquí)
            if (!presaViva && t > 8)
            {
                Projectile.Kill();
                return;
            }

            // ==================== EL BORDADO DE PARES ====================
            int parActual = (int)(t / CadenciaPar);
            if (t % CadenciaPar == 2 && parActual < Pares)
            {
                float tiempo = Main.GlobalTimeWrappedHourly;
                float angBase = parActual * 2.39996f + _semilla();   // ángulo dorado: cada par desde otro lado
                Vector2 centro = Projectile.Center;

                // EL PUNTO DE CRUCE: la presa + un vaivén para barrerla entera
                Vector2 cruce = centro + new Vector2(MathF.Sin(angBase) * 60f, -20f + MathF.Cos(angBase) * 30f);

                // LAS DOS HOJAS: nacen OPUESTAS respecto al cruce, apuntándose mutuamente
                float separación = 340f + 60f * (parActual % 3);
                Vector2 dirA = new(MathF.Cos(angBase), MathF.Sin(angBase) * 0.6f - 0.45f);
                if (dirA.LengthSquared() < 0.1f) dirA = Vector2.UnitX;
                dirA.Normalize();

                Vector2 naceA = cruce - dirA * separación;
                Vector2 naceB = cruce + dirA * separación + new Vector2(0f, 46f);

                // la A vuela hacia DONDE NACE B (y viceversa): SE BUSCAN de verdad
                Vector2 vA = (naceB - naceA).SafeNormalize(Vector2.UnitX) * 16f;
                Vector2 vB = (naceA - naceB).SafeNormalize(-Vector2.UnitX) * 16f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), naceA, vA,
                        ModContent.ProjectileType<LluviaHojaProjectile>(),
                        Projectile.damage, Projectile.knockBack, Projectile.owner, parActual, 0f);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), naceB, vB,
                        ModContent.ProjectileType<LluviaHojaProjectile>(),
                        Projectile.damage, Projectile.knockBack, Projectile.owner, parActual, 1f);
                }

                // EL SOPLO del bordado (cada par suena más agudo)
                DefinitivasLib.Sonar(SoundID.Item72.WithPitchOffset(-0.4f + parActual * 0.05f).WithVolumeScale(0.5f),
                    Projectile.Center);
            }

            // ==================== EL DESTELLO FINAL (a los doce pares) ====================
            if (t >= Pares * CadenciaPar + 6 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                LA_NOVA();
            }

            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                // EL HALO DEL CONDUCTOR: la diana de luz donde caerán los versos
                Lighting.AddLight(Projectile.Center, 0.9f, 0.8f, 0.5f);
            }
        }

        private int _semilla() => (int)Projectile.position.X ^ 7;

        /// <summary>EL DESTELLO — la nova que cierra el poema: hitball de 1500 px por UN instante.</summary>
        private void LA_NOVA()
        {
            DefinitivasLib.Sonar(SoundID.Item88.WithPitchOffset(-0.2f).WithVolumeScale(1.0f), Projectile.Center);
            DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.1f).WithVolumeScale(0.8f), Projectile.Center);
            OndaLib.Flash(new Color(255, 244, 214), 0.38f, 14, Projectile.Center);
            OndaLib.Kick(13, 16);

            // EL GOLPE: el conductor se hace gigante por un instante (la casa lo hace con hitbox)
            Projectile.position = Projectile.Center - new Vector2(750f, 750f);
            Projectile.width = 1500;
            Projectile.height = 1500;
            Projectile.localNPCHitCooldown = 2;
            Projectile.netUpdate = true;
            // (muere 8 t después — los 1500 px golpean UNA vez con el cooldown local)

            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 60; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(4f, 16f), 0,
                        i % 2 == 0 ? DefinitivasLib.Oro : DefinitivasLib.Violeta, 1.3f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL ANILLO DEL POEMA: la diana que pulsa sobre la presa (el escenario)
            VFXCore.Begin();
            float pulso = 0.5f + 0.5f * MathF.Sin(tiempo * 4f);
            if (VFXCore.Ring != null)
            {
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Ring, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.25f + 0.2f * pulso), new Vector2(120f, 120f), tiempo * 0.5f);
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Ring, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.18f + 0.14f * pulso), new Vector2(190f, 190f), -tiempo * 0.3f);
            }
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.30f), new Vector2(46f, 46f));
            VFXCore.FlushAdditive();

            // LA NOVA: el destello que cubre todo (los 14 t tras el cierre)
            if (Projectile.localAI[0] == 1f && t < Pares * CadenciaPar + 22)
            {
                float vidaNova = (t - (Pares * CadenciaPar + 6)) / 16f;
                if (vidaNova >= 0f && vidaNova <= 1f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * (1f - vidaNova)),
                        new Vector2(1600f, 1600f) * (0.4f + vidaNova * 0.6f));
                    if (VFXCore.DestelloFinal != null)
                        DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                            DefinitivasLib.Alfa(Color.White, 0.9f * (1f - vidaNova)), new Vector2(900f, 900f), vidaNova * 1.2f);
                    VFXCore.FlushAdditive();

                    // LA DOBLE ONDA (dorada + violeta)
                    if (Main.spriteBatch != null)
                    {
                        VFXCore.CerrarLoteSiAbierto();
                        try
                        {
                            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                                null, Main.GameViewMatrix.TransformationMatrix);
                            OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vidaNova, 1300f,
                                DefinitivasLib.Oro, 1f, 31, 16f, OndaFalloff.Quadratic, true);
                            OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vidaNova * 0.8f, 1000f,
                                DefinitivasLib.Violeta, 0.8f, 77, 12f, OndaFalloff.Quadratic, false);
                            Main.spriteBatch.End();
                        }
                        catch { }
                        VFXCore.ReabrirLoteVanilla();
                    }
                }
            }

            // v6.54.5 — el contrato del lote: cubre los caminos sin nova
            // (el Flush del anillo dejaba el lote CERRADO).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// LLUVIAHOJAPROJECTILE — el verso suelto: UNA hoja de luz (oro o
    /// violeta según su lado) que vuela RECTA cruzándose con su gemela.
    /// LumenBlade estirada + estela Ribbon + su polvo. Vida corta (~52 t).
    /// ai[0] = n.º de par · ai[1] = lado (0 = oro / 1 = violeta).
    /// </summary>
    public class LluviaHojaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/LumenBlade";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 52;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.ai[2]++;

            // EL VAIVÉN del vuelo (nada corta en línea PERFECTA — la vida ondula)
            float ondita = MathF.Sin(Projectile.ai[2] * 0.22f + Projectile.ai[0] * 1.7f) * 0.035f;
            Projectile.velocity = Projectile.velocity.RotatedBy(ondita);

            bool oro = Projectile.ai[1] < 0.5f;
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                Color c = oro ? DefinitivasLib.Oro : DefinitivasLib.Violeta;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        -Projectile.velocity * 0.08f, 0, c, 0.7f);
                    d.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, c.R / 255f * 0.8f, c.G / 255f * 0.7f, c.B / 255f * 0.8f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            bool oro = Projectile.ai[1] < 0.5f;
            Color c = oro ? DefinitivasLib.Oro : DefinitivasLib.Violeta;
            float tiempo = Main.GlobalTimeWrappedHourly;

            // LA ESTELA (la cinta de la casa sobre los últimos 10 puntos)
            Vector2[] estela = new Vector2[11];
            int n = 0;
            for (int i = 0; i < 10 && i < Projectile.oldPos.Length; i++)
            {
                Vector2 p = Projectile.oldPos[i];
                if (p != Vector2.Zero) estela[n++] = p + Projectile.Size * 0.5f - Main.screenPosition;
            }
            if (n >= 3)
            {
                var recorte = new Vector2[n];
                Array.Copy(estela, recorte, n);
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    EstelaLib.Ribbon(Main.spriteBatch, recorte, 22f, EstelaProfile.Comet, c, 0.8f,
                        (int)Projectile.ai[0] + 3, tiempo, true, 0.6f);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }

            // EL HALO + LA HOJA (LumenBlade estirada en la dirección del vuelo)
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.45f), new Vector2(90f, 50f), Projectile.rotation);
            VFXCore.FlushAdditive();

            Texture2D hoja = DefinitivasLib.TexLumenBlade;
            if (hoja != null)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    DefinitivasLib.Blit(Main.spriteBatch, hoja, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.95f), new Vector2(78f, 22f), Projectile.rotation);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }
            // v6.54.5 — el contrato del lote (cubre el camino hoja == null:
            // el Flush del halo dejaba el lote CERRADO).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
