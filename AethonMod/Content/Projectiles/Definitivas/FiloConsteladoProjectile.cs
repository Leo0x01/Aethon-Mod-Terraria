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
    /// FILOCONSTELADOPROJECTILE — v6.50.66 — ARMA 4: EL FILO
    /// CONSTELADO.
    ///
    /// La receta de la «Galaxia» de Calamity (la espada de los CUATRO
    /// ATTUNEMENTS): el swing SIEMPRE dibuja el ARCO DE LUZ (la cimitarra
    /// de Slash + su estela) y encima dispara LA CONSTELACIÓN ACTIVA —
    ///
    ///   · EL SOL: LA ONDA SOLAR — un frente de FireRing que BARRE 460 px
    ///     hacia delante, con sus lenguas de fuego y su kick.
    ///   · LA ESTRELLA POLAR: TRES DARDOS estelares que persiguen (el
    ///     homing suave de la casa) con estela fría.
    ///   · ANDRÓMEDA: LA GALAXIA — un remolino espiral (Arcoiris girando)
    ///     que viaja LENTO engullendo todo a su paso (penetración total).
    ///   · ARIES: LA CADENA — seis nodos estelares en fila que golpean a
    ///     650 px de distancia, el rayo que SIEMPRE llega.
    ///
    /// Cada atavío con SU paleta y SU sonido. El atavío viaja en ai[1].
    /// </summary>
    public class FiloConsteladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private int _semilla;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 46;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte atavio = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0) _semilla = (int)Main.GameUpdateCount + Projectile.owner;

            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;

            switch (atavio)
            {
                // ==================== EL SOL: LA ONDA SOLAR ====================
                case 0:
                {
                    // La onda AVANZA desacelerando (nace rápida, muere abrazando)
                    Projectile.velocity *= 0.965f;
                    Projectile.Center += Projectile.velocity;
                    if (t == 1)
                    {
                        DefinitivasLib.Sonar(SoundID.Item45.WithPitchOffset(-0.3f).WithVolumeScale(0.8f), Projectile.Center);
                        OndaLib.Kick(5, 8);
                    }
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                    {
                        Vector2 perp = new(-rumbo.Y, rumbo.X);
                        Vector2 pos = Projectile.Center + perp * Main.rand.NextFloat(-70f, 70f) + rumbo * Main.rand.NextFloat(-30f, 30f);
                        Dust d = Dust.NewDustPerfect(pos, DustID.Torch,
                            -rumbo * Main.rand.NextFloat(1f, 3f) + perp * Main.rand.NextFloat(-1.5f, 1.5f), 0,
                            Main.rand.NextBool(3) ? DefinitivasLib.Oro : DefinitivasLib.Ambar, Main.rand.NextFloat(0.9f, 1.5f));
                        d.noGravity = true;
                    }
                    Lighting.AddLight(Projectile.Center, 1.4f, 1.0f, 0.5f);
                    break;
                }

                // ==================== LA ESTRELLA POLAR: LOS DARDOS ====================
                case 1:
                {
                    if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int k = -1; k <= 1; k++)
                        {
                            Vector2 v = rumbo.RotatedBy(k * 0.22f) * 21f;
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, v,
                                ModContent.ProjectileType<PolarDardoProjectile>(),
                                (int)(Projectile.damage * 0.62f), Projectile.knockBack * 0.5f, Projectile.owner);
                        }
                        DefinitivasLib.Sonar(SoundID.Item9.WithPitchOffset(0.35f).WithVolumeScale(0.7f), Projectile.Center);
                    }
                    // el arco de luz del swing vive solo 20 t (los dardos mandan)
                    if (t > 20) { Projectile.Kill(); return; }
                    break;
                }

                // ==================== ANDRÓMEDA: LA GALAXIA ====================
                case 2:
                {
                    Projectile.velocity *= 0.985f;
                    Projectile.Center += Projectile.velocity;
                    if (t == 1)
                        DefinitivasLib.Sonar(SoundID.Item34.WithPitchOffset(-0.5f).WithVolumeScale(0.7f), Projectile.Center);
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radio = 90f * (0.5f + 0.5f * MathF.Sin(t * 0.09f));
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                            (Projectile.Center - pos) * 0.02f, 0,
                            DefinitivasLib.GradienteJuicio(ang / MathHelper.TwoPi), 0.8f);
                        d.noGravity = true;
                    }
                    Lighting.AddLight(Projectile.Center, 1.0f, 0.8f, 1.2f);
                    break;
                }

                // ==================== ARIES: LA CADENA ====================
                case 3:
                {
                    if (t == 1)
                        DefinitivasLib.Sonar(SoundID.Item116.WithPitchOffset(-0.2f).WithVolumeScale(0.8f), Projectile.Center);
                    // la cadena se ESTIRA de golpe y sostiene (el latigazo que siempre llega)
                    Projectile.Center = Main.player[Projectile.owner].MountedCenter +
                        rumbo * MathHelper.Min(650f, 40f + t * 42f);
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && t % 3 == 0)
                        OndaLib.Kick(3, 6);
                    if (t > 30) { Projectile.Kill(); return; }
                    break;
                }
            }

            if (t > 44) Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            byte atavio = (byte)Projectile.ai[1];
            if (atavio == 3)
            {
                // LA CADENA golpea TODO el corredor de la cadena (segmento gordo)
                Player dueño = Main.player[Projectile.owner];
                Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                    ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
                return PrismaPrimordialProjectile.SegmentoGordo(targetHitbox,
                    dueño.MountedCenter, Projectile.Center, 90f);
            }
            if (atavio == 0)
            {
                // LA ONDA SOLAR: el frente es un segmento PERPENDICULAR de 150 px
                Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                    ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
                Vector2 perp = new(-rumbo.Y, rumbo.X);
                return PrismaPrimordialProjectile.SegmentoGordo(targetHitbox,
                    Projectile.Center - perp * 80f, Projectile.Center + perp * 80f, 70f);
            }
            return null;      // los demás usan el hitbox del proyectil
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            byte atavio = (byte)Projectile.ai[1];
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player dueño = Main.player[Projectile.owner];
            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;

            // ==================== EL ARCO DE LUZ (el swing de SIEMPRE) ====================
            // La cimitarra de luz que nace del portador y barre el frente —
            // Slash.png rotando con la estela de la casa.
            {
                float vida = MathHelper.Clamp(t / 16f, 0f, 1f);
                Vector2 posArco = dueño.MountedCenter + rumbo * MathHelper.Lerp(40f, 190f, vida);
                Texture2D slash = DefinitivasLib.TexSlash;
                Color cArco = atavio == 1 ? DefinitivasLib.Cian
                    : atavio == 2 ? DefinitivasLib.Violeta
                    : atavio == 3 ? DefinitivasLib.Oro
                    : DefinitivasLib.Oro;
                VFXCore.Begin();
                VFXCore.Quad(posArco, DefinitivasLib.Alfa(cArco, 0.35f * (1f - vida * 0.6f)),
                    new Vector2(210f, 210f), rumbo.ToRotation() + MathHelper.PiOver4);
                VFXCore.FlushAdditive();
                if (slash != null)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        DefinitivasLib.Blit(Main.spriteBatch, slash, posArco - Main.screenPosition,
                            DefinitivasLib.Alfa(Color.White, 0.85f * (1f - vida * 0.7f)),
                            new Vector2(240f, 190f), rumbo.ToRotation() + MathHelper.PiOver2);
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }
            }

            // ==================== LO PROPIO DE CADA CONSTELACIÓN ====================
            switch (atavio)
            {
                case 0: DibujarOndaSolar(rumbo, t, tiempo); break;
                case 2: DibujarGalaxia(t, tiempo); break;
                case 3: DibujarCadena(dueño, rumbo, t, tiempo); break;
            }
            return false;
        }

        /// <summary>EL SOL — el frente de FireRing con sus lenguas.</summary>
        private void DibujarOndaSolar(Vector2 rumbo, float t, float tiempo)
        {
            float vida = MathHelper.Clamp(t / 44f, 0f, 1f);
            float abre = MathHelper.Lerp(0.3f, 1.15f, vida);
            Texture2D fuego = DefinitivasLib.TexFireRing;

            VFXCore.Begin();
            // EL FRENTE: un ARCO de quads (la onda es CONCAVA hacia delante)
            for (int k = -3; k <= 3; k++)
            {
                float q = k / 3f;
                Vector2 perp = new(-rumbo.Y, rumbo.X);
                Vector2 pos = Projectile.Center + perp * (q * 100f) + rumbo * (-MathF.Abs(q) * 26f);
                float tam = (130f - MathF.Abs(q) * 40f) * (1f - vida * 0.35f);
                VFXCore.Quad(pos, DefinitivasLib.Alfa(DefinitivasLib.Ambar, (0.55f - vida * 0.3f)),
                    new Vector2(tam, tam), rumbo.ToRotation() + q * abre);
            }
            // EL NÚCLEO de la onda
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * (1f - vida * 0.5f)),
                new Vector2(90f, 160f), rumbo.ToRotation() + MathHelper.PiOver2);
            VFXCore.FlushAdditive();

            if (fuego != null)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    DefinitivasLib.Blit(Main.spriteBatch, fuego, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.8f * (1f - vida * 0.5f)),
                        new Vector2(230f, 150f), rumbo.ToRotation());
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }
        }

        /// <summary>ANDRÓMEDA — el remolino espiral (Arcoiris girando + brazos de polvo).</summary>
        private void DibujarGalaxia(float t, float tiempo)
        {
            float vida = MathHelper.Clamp(t / 46f, 0f, 1f);
            float tam = 320f * (0.55f + 0.45f * MathF.Sin(t * 0.06f)) * (1f - vida * 0.25f);

            VFXCore.Begin();
            // LOS DOS BRAZOS de la espiral (curvas de quads girando en sentidos opuestos)
            for (int brazo = 0; brazo < 2; brazo++)
            {
                for (int i = 0; i < 10; i++)
                {
                    float q = i / 9f;
                    float ang = tiempo * (brazo == 0 ? 2.2f : -2.2f) + q * 4.2f + brazo * MathHelper.Pi;
                    float radio = 30f + q * 140f;
                    Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                    Color c = DefinitivasLib.GradienteJuicio(q * 0.7f + tiempo * 0.05f + brazo * 0.5f);
                    float punto = (30f - q * 18f) * (1f - vida * 0.4f);
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.75f - q * 0.35f), new Vector2(punto, punto));
                }
            }
            // EL NÚCLEO brillante
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.75f * (1f - vida * 0.4f)),
                new Vector2(110f, 110f));
            if (VFXCore.Arcoiris != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Arcoiris, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.55f * (1f - vida)), new Vector2(tam, tam), tiempo * 0.6f);
            VFXCore.FlushAdditive();
        }

        /// <summary>ARIES — la cadena de nodos estelares con su cable de luz.</summary>
        private void DibujarCadena(Player dueño, Vector2 rumbo, float t, float tiempo)
        {
            float estira = MathHelper.Clamp(t / 16f, 0f, 1f);
            float largo = MathHelper.Min(650f, 40f + t * 42f);
            Vector2 a = dueño.MountedCenter;
            Vector2 b = a + rumbo * largo;

            // EL CABLE (la tira de primitivas con el vaivén del latigazo)
            const int N = 16;
            var pts = new Vector2[N];
            for (int i = 0; i < N; i++)
            {
                float q = i / (float)(N - 1);
                Vector2 perp = new(-rumbo.Y, rumbo.X);
                float onda = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 18f) * 14f * (1f - estira);
                pts[i] = Vector2.Lerp(a, b, q) + perp * onda - Main.screenPosition;
            }
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FilamentoEnLote(pts, 26f, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                    0.8f, StormTaper.Center, _semilla, 0, true, true, true, null, null);
            }
            finally { RayoStrip.CerrarLote(); }

            // LOS NODOS ESTELARES (6 estrellas crecientes a lo largo)
            VFXCore.Begin();
            Texture2D estrella = DefinitivasLib.TexStar;
            for (int k = 1; k <= 6; k++)
            {
                float q = k / 6f;
                Vector2 pos = Vector2.Lerp(a, b, q);
                float tam = 26f + 18f * MathF.Sin(q * MathHelper.Pi);
                VFXCore.Quad(pos, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f), new Vector2(tam * 2.2f, tam * 2.2f));
                if (estrella != null)
                    DefinitivasLib.Blit(Main.spriteBatch, estrella, pos - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.9f), new Vector2(tam, tam), tiempo * 2f + k);
            }
            // LA PUNTA de Aries (el carnero embiste)
            VFXCore.Quad(b, DefinitivasLib.Alfa(Color.White, 0.9f), new Vector2(90f, 90f));
            VFXCore.FlushAdditive();
        }
    }

    /// <summary>
    /// POLARDARDOPROJECTILE — el dardo de LA ESTRELLA POLAR: rápido,
    /// frío, con homing suave (persigue con giro limitado — la casa) y su
    /// estela cian. Vida 60 t.
    /// </summary>
    public class PolarDardoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // EL HOMING SUAVE (gira hacia la presa más cercana SIN meterse en órbitas)
            NPC presa = DefinitivasLib.BuscarPresa(Projectile.Center, false);
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float vel = Projectile.velocity.Length();
                Projectile.velocity = Vector2.Normalize(
                    Vector2.Normalize(Projectile.velocity).SafeNormalize(Vector2.UnitX) * 0.86f + hacia * 0.14f) * vel;
            }

            if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    -Projectile.velocity * 0.05f, 0, DefinitivasLib.Cian, 0.6f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;

            // LA ESTELA corta
            Vector2[] estela = new Vector2[9];
            int n = 0;
            for (int i = 0; i < 8 && i < Projectile.oldPos.Length; i++)
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
                    EstelaLib.Ribbon(Main.spriteBatch, recorte, 14f, EstelaProfile.Comet, DefinitivasLib.Cian,
                        0.8f, (int)Projectile.ai[2] + 5, tiempo, true, 0.5f);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }

            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.6f), new Vector2(60f, 60f));
            VFXCore.FlushAdditive();

            // LA ESTRELLA (dibujada a mano para que arda en aditivo)
            Texture2D estrella = DefinitivasLib.TexStar;
            if (estrella != null)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    DefinitivasLib.Blit(Main.spriteBatch, estrella, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.95f), new Vector2(34f, 34f), Projectile.rotation);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
