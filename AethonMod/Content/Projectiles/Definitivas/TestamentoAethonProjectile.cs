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
    /// TESTAMENTOAETHONPROJECTILE — v6.50.66 — ARMA 12: EL TESTAMENTO DE
    /// AETHON — LA CORONA DE LA BOLSA.
    ///
    /// El patrón FUSIÓN de la investigación (el «Soul of Eternity» de
    /// Fargo's, el «Dragon Pow» de Calamity: TODAS las armas de la
    /// familia fundidas en una): el grimorio se ABRE sobre el portador
    /// (el sprite real del Grimorio del Eterno, como el arma 4 de las
    /// sombras), sus páginas vuelan… y DICTA LA SENTENCIA QUE TOCA —
    /// cada uso dispara UNA DE LAS ONCE SENTENCIAS de la bolsa con su
    /// poder acrecentado (×1.3), rotando para siempre.
    ///
    /// Once arsenales, un solo objeto. La rotación vive en
    /// DefinitivasLib.SentenciaActual (0..10).
    /// </summary>
    public class TestamentoAethonProjectile : ModProjectile
    {
        /// <summary>LOS NOMBRES de las once sentencias (el índice = DefinitivasLib.SentenciaActual).</summary>
        public static readonly string[] Sentencias =
        {
            "EL OJO DEL JUICIO", "EL PRISMA PRIMORDIAL", "LA LLUVIA DE VERSOS", "EL FILO CONSTELADO",
            "EL PARTIDOR DE MUNDOS", "LA GUADAÑA DEL VIEJO DIOS", "EL SISTEMA DE AETHON",
            "LA CONSTELACIÓN DEL JUICIO", "EL VÓRTICE DEVORADOR", "EL RÉQUIEM ESTELAR", "LA IRA DEL CIELO",
        };

        private const int TApertura = 18;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;            // el daño lo hacen las sentencias
            Projectile.penetrate = -1;
            Projectile.timeLeft = 64;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL LIBRO cabalga sobre el portador
            Projectile.Center = dueño.MountedCenter + new Vector2(0f, -108f);

            if (t == 2)
            {
                DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.4f).WithVolumeScale(0.8f), Projectile.Center);
                if (dueño.whoAmI == Main.myPlayer)
                    Main.NewText($"[c/FFD678:El Testamento dicta:] [c/B0A0FF:{Sentencias[DefinitivasLib.SentenciaActual]}]");
            }

            // ==================== LA DICTADURA DE LA SENTENCIA ====================
            if (t == TApertura)
            {
                int sentencia = DefinitivasLib.SentenciaActual;
                int daño = (int)(Projectile.damage * 1.3f);
                Vector2 rumbo = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
                var src = Projectile.GetSource_FromAI();

                switch (sentencia)
                {
                    case 0:     // EL OJO DEL JUICIO (sentencia: sin canal — vive entero)
                        Projectile.NewProjectile(src, Projectile.Center, Vector2.Zero,
                            ModContent.ProjectileType<OjoDelJuicioProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 1f, 0f, 0f);
                        break;

                    case 1:     // EL PRISMA PRIMORDIAL (sentencia: sin canal)
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<PrismaPrimordialProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 1f, 0f, 0f);
                        break;

                    case 2:     // LA LLUVIA DE VERSOS (con presa al cuello)
                    {
                        NPC presa = DefinitivasLib.BuscarPresa(dueño.Center, true) ??
                                    DefinitivasLib.BuscarPresa(dueño.Center, false);
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<LluviaDeVersosProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, presa != null ? presa.whoAmI + 1 : 0, 0f, 0f);
                        break;
                    }

                    case 3:     // EL FILO CONSTELADO (atavío al azar — el filo sorprende)
                        Projectile.NewProjectile(src, dueño.MountedCenter + rumbo * 30f, rumbo * 12f,
                            ModContent.ProjectileType<FiloConsteladoProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, Main.rand.Next(4), 0f);
                        break;

                    case 4:     // EL PARTIDOR DE MUNDOS
                        Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 10f,
                            ModContent.ProjectileType<PartidorDeMundosProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    case 5:     // LA GUADAÑA DEL VIEJO DIOS
                        Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 6f,
                            ModContent.ProjectileType<GuadanaViejoDiosProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    case 6:     // EL SISTEMA DE AETHON
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<SistemaAethonProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    case 7:     // LA CONSTELACIÓN DEL JUICIO (sentencia: siembra SOLA y juzga)
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<ConstelacionJuicioProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 1f, 0f, 0f);
                        break;

                    case 8:     // EL VÓRTICE DEVORADOR
                        Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 14f,
                            ModContent.ProjectileType<VorticeDevoradorProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    case 9:     // EL RÉQUIEM ESTELAR
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<RequiemEstelarProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    default:    // LA IRA DEL CIELO
                        Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<IraDelCieloProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;
                }

                // LA VUELTA (la próxima sentencia queda servida)
                DefinitivasLib.SentenciaActual = (DefinitivasLib.SentenciaActual + 1) % Sentencias.Length;
            }

            // LAS PÁGINAS que vuelan al dictar
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t > TApertura - 6 && Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-20f, 30f));
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f)), 0,
                    DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.8f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float abre = DefinitivasLib.DeGolpe(MathHelper.Clamp(t / (float)TApertura, 0f, 1f));
            float cierra = MathHelper.Clamp((Projectile.timeLeft - 14) / 20f, 0f, 1f);
            float vida = abre * cierra;
            if (vida <= 0.01f) return false;

            float bob = MathF.Sin(tiempo * 1.6f) * 6f * vida;
            Vector2 posLibro = Projectile.Center + new Vector2(0f, -bob);

            // === 1. EL AURA (el resplandor que enmarca la palabra) ===
            VFXCore.Begin();
            Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.1f);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad, 0.18f * vida), new Vector2(230f, 240f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.12f * vida), new Vector2(130f, 170f) * vida);
            VFXCore.FlushAdditive();

            // === 2. EL LIBRO DE VERDAD (el sprite real del Grimorio del Eterno) ===
            Texture2D tex = null;
            try
            {
                tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<Content.Weapons.GrimoireEternal>()].Value;
            }
            catch { }
            if (tex != null)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                        null, Main.Transform);
                    Main.spriteBatch.Draw(tex, posLibro - Main.screenPosition, null,
                        new Color(255, 246, 224, (byte)(255 * vida)), MathF.Sin(tiempo * 0.9f) * 0.09f,
                        tex.Size() * 0.5f, 3.0f * vida, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // === 3. LA VOZ DORADA (la garganta del libro late al dictar) ===
            VFXCore.Begin();
            float late = 0.35f + 0.2f * MathF.Sin(tiempo * 6f);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.Oro, late * vida), new Vector2(64f, 110f) * vida);
            // EL SELLO de la sentencia (la estrella que gira sobre el lomo)
            if (DefinitivasLib.TexStar != null)
                DefinitivasLib.Blit(Main.spriteBatch, DefinitivasLib.TexStar,
                    posLibro + new Vector2(0f, -70f) - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.9f * vida), new Vector2(64f, 64f) * vida, tiempo * 1.4f);
            VFXCore.FlushAdditive();
            // v6.54.5 — el contrato del lote: el Flush dejaba el lote CERRADO
            // (la corona mataba el juego al rotar las once sentencias).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
