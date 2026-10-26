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
    /// GUADANAVIEJODIOSPROJECTILE — v6.50.66 — ARMA 6: LA GUADAÑA DEL
    /// VIEJO DIOS.
    ///
    /// La receta de la «Scythe of the Old God» de el dios antiguo (la guadaña de
    /// los TRES TAJOS) sobre el lenguaje de sombras de la casa: el combo
    /// vive en una cuenta estática por jugador —
    ///
    ///   · TAJO I — la media luna violeta (140 px) que barre al frente.
    ///   · TAJO II — la media luna ESPEJADA y MÁS GRANDE (200 px).
    ///   · TAJO III — LA CRUZ: dos guadañas PERPENDICULARES de 300 px que
    ///     se cruzan sobre el enemigo + LA ONDA DE CHOQUE negra-violeta +
    ///     LAS ALMAS de lo cortado vuelan al portador.
    ///
    /// Cada media luna: Crescent.png con su estela Ribbon y su borde de
    /// masa (SombrasLib.Masa) — la guadaña es SOMBRILLADA por dentro y
    /// LUMINOSA por el filo (la dualidad del grimorio).
    ///
    /// ai[1] = n.º de tajo (0/1/2).
    /// </summary>
    public class GuadanaViejoDiosProjectile : ModProjectile
    {
        /// <summary>La cuenta del combo por jugador (0 → 1 → 2 → 0…).</summary>
        public static int[] Combo = new int[Main.maxPlayers];

        public override string Texture => "AethonMod/Content/Effects/Procedural/Crescent";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL TAJO que me tocó (el combo lo asigna al NACER — en el Shoot
            // no se puede: la cuenta vive aquí)
            if (t < 2f)
            {
                int c = Combo[Projectile.owner] % 3;
                Projectile.ai[1] = c;
                Combo[Projectile.owner] = (c + 1) % 3;
                DefinitivasLib.Sonar(SoundID.Item71.WithPitchOffset(-0.5f + c * 0.15f).WithVolumeScale(0.9f), Projectile.Center);
                if (c == 2)
                {
                    DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.1f).WithVolumeScale(1.0f), Projectile.Center);
                    OndaLib.Kick(11, 14);
                    OndaLib.Flash(new Color(190, 130, 255), 0.22f, 10, Projectile.Center);
                }
            }

            // LA DERIVA DEL ARCO: nace pegado al portador y SE PROYECTA al frente
            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            float avance = DefinitivasLib.DeGolpe(MathHelper.Clamp(t / 14f, 0f, 1f));
            float lejos = TamañoTajo() * (0.35f + 0.85f * avance);
            Projectile.Center = dueño.MountedCenter + rumbo * lejos;
            Projectile.rotation = rumbo.ToRotation() + avance * 2.4f;      // LA GUADAÑA GIRA al proyectarse

            // EL TERCER TAJO: la cruz + las almas
            if (Projectile.ai[1] >= 2f && t > 12 && !Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (t % 4 == 0)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float prog = Main.rand.NextFloat(1f);
                        Vector2 a = Projectile.Center + new Vector2(Main.rand.NextFloat(-160f, 160f), Main.rand.NextFloat(-160f, 160f));
                        Vector2 b = dueño.MountedCenter;
                        Vector2 ctrl = (a + b) * 0.5f + new Vector2(Main.rand.NextFloat(-120f, 120f), -100f);
                        float u = 1f - prog;
                        Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                        SombrasLib.Alma(pos, 8f + 4f * MathF.Sin(tiempo * 5f + k), 0.8f);
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.8f, 0.5f, 1.3f);
            }

            if (t > 38) Projectile.Kill();
        }

        private float TamañoTajo() => Projectile.ai[1] switch
        {
            0f => 150f,
            1f => 210f,
            _ => 300f,
        };

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // El golpe del arco: el corredor del avance (del portador al final del tajo)
            Player dueño = Main.player[Projectile.owner];
            Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            float grosor = TamañoTajo() * 0.9f;
            return PrismaPrimordialProjectile.SegmentoGordo(targetHitbox,
                dueño.MountedCenter, Projectile.Center + rumbo * 60f, grosor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player dueño = Main.player[Projectile.owner];
            float tamaño = TamañoTajo();
            bool cruz = Projectile.ai[1] >= 2f;

            // ==================== EL MANGO DE SOMBRA (la columna de masa del arma) ====================
            {
                Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                    ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
                Vector2 raiz = dueño.MountedCenter + rumbo * 10f;
                Vector2 cabeza = Projectile.Center;
                Vector2[] col = SombrasLib.Columna(raiz, cabeza, tiempo, 13, 5, 0.12f);
                SombrasLib.Masa(col, 22f, 10f, 0.85f, 13, tiempo);
            }

            // ==================== LAS MEDIAS LUNAS ====================
            // (el tajo 3 dibuja DOS perpendiculares — LA CRUZ)
            int lunas = cruz ? 2 : 1;
            for (int l = 0; l < lunas; l++)
            {
                float rotExtra = l == 1 ? MathHelper.PiOver2 : 0f;
                float vida = MathHelper.Clamp(t / 30f, 0f, 1f);
                float gira = Projectile.rotation + rotExtra + (l == 1 ? tiempo * 1.4f : 0f);
                Color cFilo = DefinitivasLib.GradienteJuicio(tiempo * 0.08f + l * 0.4f);

                // LA ESTELA del arco (la cinta de la casa)
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
                        EstelaLib.Ribbon(Main.spriteBatch, recorte, 26f, EstelaProfile.Comet, cFilo,
                            0.85f * (1f - vida * 0.5f), 17 + l, tiempo, true, 0.55f);
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }

                // EL FILO LUMINOSO (Crescent + su halo)
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(cFilo, 0.4f * (1f - vida * 0.4f)),
                    new Vector2(tamaño * 1.7f, tamaño * 1.7f), gira);
                VFXCore.FlushAdditive();

                Texture2D luna = DefinitivasLib.TexCrescent;
                if (luna != null)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        DefinitivasLib.Blit(Main.spriteBatch, luna, Projectile.Center - Main.screenPosition,
                            DefinitivasLib.Alfa(Color.White, 0.95f * (1f - vida * 0.6f)),
                            new Vector2(tamaño * 1.5f, tamaño * 1.5f), gira);
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }
            }

            // ==================== EL JUICE DE LA CRUZ (la onda negra-violeta) ====================
            if (cruz && t < 20)
            {
                SombrasLib.OndaChoque(Projectile.Center, 60f + t * 34f, 0.65f * (1f - t / 20f), false);
            }
            return false;
        }
    }
}
