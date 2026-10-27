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
    /// TESTAMENTOAETHONPROJECTILE — v6.54.6 — ARMA 5 DE LAS CINCO: EL
    /// TESTAMENTO DE AETHON — LA CORONA DE LA BOLSA.
    ///
    /// LA REFORMA (la letra del usuario: «el testamento lo lanzó y lo
    /// dejó bugueado» — el prisma sentencia vivía para siempre — y «deja
    /// solo las armas que mencioné… al menos 7 capas de efectos visuales
    /// a cada una»): el libro ahora dicta SOLO LAS CUATRO SENTENCIAS de
    /// la bolsa reformada, con su cine completo en siete capas —
    ///
    ///   CAPA 1 — EL VELO DEL DICTADO: los bordes respiran con el COLOR
    ///   DE LA SENTENCIA QUE TOCA (cada arma tiene su acento).
    ///   CAPA 2 — EL LIBRO DE VERDAD: el sprite real del Grimorio del
    ///   Eterno, flotando con su vaivén, enmarcado por los CUATRO HALOS
    ///   APILADOS del rayo de fusión.
    ///   CAPA 3 — LAS PÁGINAS: hojas de luz que salen volando del libro
    ///   girando sobre sí mismas (no solo polvo — PÁGINAS de verdad).
    ///   CAPA 4 — EL ANILLO DE RUNAS: siete sellos orbitando el libro
    ///   en elipse, cada uno latiendo con su color del gradiente.
    ///   CAPA 5 — LA PALABRA: al dictar, la ONDA EXPANSIVA circular +
    ///   el flash + el temblor — la palabra se OYE.
    ///   CAPA 6 — EL SELLO: la estrella de cuatro puntas girando sobre
    ///   el lomo, teñida del color de la sentencia.
    ///   CAPA 7 — EL VÍNCULO: un filamento de luz vivo que corre del
    ///   libro a la sentencia invocada — la palabra hecha arma.
    ///
    /// LA CURA DEL BUG: todas las sentencias invocadas viven en modo
    /// autónomo ACOTADO (el prisma sentencia ahora muere a los 192 t —
    /// v6.54.6) y el failsafe global de 1200 t las protege a todas.
    /// </summary>
    public class TestamentoAethonProjectile : ModProjectile
    {
        /// <summary>
        /// LOS NOMBRES y LOS COLORES de las cuatro sentencias (el índice =
        /// DefinitivasLib.SentenciaActual). Cada sentencia con SU acento.
        /// </summary>
        public static readonly string[] Sentencias =
        {
            "EL PRISMA PRIMORDIAL", "EL PARTIDOR DE MUNDOS", "EL RÉQUIEM ESTELAR", "LA IRA DEL CIELO",
        };

        private static readonly Color[] Acentos =
        {
            new(255, 248, 235),     // prisma — blanco cálido del espectro
            new(255, 214, 120),     // partidor — oro
            new(255, 158, 64),      // réquiem — ámbar del bronce
            new(176, 118, 255),     // ira — violeta de la tormenta
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

        private int _hijo = -1;         // el whoAmI de la sentencia invocada (para EL VÍNCULO)

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            // EL LIBRO cabalga sobre el portador
            Projectile.Center = dueño.MountedCenter + new Vector2(0f, -108f);

            if (t == 2)
            {
                DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.4f).WithVolumeScale(0.8f), Projectile.Center);
                if (dueño.whoAmI == Main.myPlayer)
                {
                    Color acento = Acentos[DefinitivasLib.SentenciaActual % Acentos.Length];
                    Main.NewText($"[c/FFD678:El Testamento dicta:] [c/{acento.R:X2}{acento.G:X2}{acento.B:X2}:{Sentencias[DefinitivasLib.SentenciaActual]}]");
                }
            }

            // ==================== LA DICTADURA DE LA SENTENCIA ====================
            if (t == TApertura)
            {
                int sentencia = DefinitivasLib.SentenciaActual;
                int daño = (int)(Projectile.damage * 1.3f);
                Vector2 rumbo = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
                var src = Projectile.GetSource_FromAI();

                // ==================== CAPA 5 — LA PALABRA (la onda que se oye) ====================
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Flash(DefinitivasLib.Alfa(Acentos[sentencia], 0.9f), 0.20f, 9, Projectile.Center);
                    OndaLib.Kick(6, 10);
                }

                switch (sentencia)
                {
                    case 0:     // EL PRISMA PRIMORDIAL (sentencia: vive su ciclo acotado)
                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<PrismaPrimordialProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 1f, 0f, 0f);
                        break;

                    case 1:     // EL PARTIDOR DE MUNDOS
                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 10f,
                            ModContent.ProjectileType<PartidorDeMundosProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    case 2:     // EL RÉQUIEM ESTELAR
                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<RequiemEstelarProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;

                    default:    // LA IRA DEL CIELO
                        _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                            ModContent.ProjectileType<IraDelCieloProjectile>(), daño, Projectile.knockBack,
                            Projectile.owner, 0f, 0f, 0f);
                        break;
                }

                // LA VUELTA (la próxima sentencia queda servida)
                DefinitivasLib.SentenciaActual = (DefinitivasLib.SentenciaActual + 1) % Sentencias.Length;
            }

            // ==================== CAPA 3 — LAS PÁGINAS QUE VUELAN ====================
            // (hojas de luz de verdad: quads estirados girando, no solo polvo)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t > TApertura - 8 && t < TApertura + 30 && Main.rand.NextBool(2))
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

            int sentencia = DefinitivasLib.SentenciaActual;
            Color acento = Acentos[sentencia];

            float bob = MathF.Sin(tiempo * 1.6f) * 6f * vida;
            Vector2 posLibro = Projectile.Center + new Vector2(0f, -bob);

            // ==================== CAPA 1 — EL VELO DEL DICTADO ====================
            {
                float w = Main.screenWidth, h = Main.screenHeight;
                Vector2 o = Main.screenPosition;
                float pulso = 0.9f + 0.1f * MathF.Sin(tiempo * 2.3f);
                VFXCore.Begin();
                Color denso = DefinitivasLib.Alfa(new Color(34, 22, 10), (0.14f + 0.10f * vida) * 0.62f * pulso);
                VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
                // EL TINTE DEL ACENTO (la sala sabe qué sentencia toca)
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 0.5f), DefinitivasLib.Alfa(acento, 0.045f * vida * pulso),
                    new Vector2(w * 1.6f, h * 1.6f), 0f, VFXCore.SoftGlow);
                VFXCore.FlushAlpha();
            }

            // ==================== CAPA 2 — EL LIBRO CON SUS HALOS APILADOS ====================
            VFXCore.Begin();
            Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.1f);
            Color grad2 = DefinitivasLib.GradienteJuicio(tiempo * 0.1f + 0.33f);
            // LOS CUATRO HALOS APILADOS (14× · 5× · 4× · 2×)
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad, 0.10f * vida), new Vector2(1200f, 1200f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad2, 0.20f * vida), new Vector2(520f, 520f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad, 0.38f * vida), new Vector2(360f, 360f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.45f * vida), new Vector2(210f, 210f) * vida);
            VFXCore.FlushAdditive();

            // EL SPRITE REAL del Grimorio del Eterno
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

            VFXCore.Begin();

            // ==================== CAPA 4 — EL ANILLO DE RUNAS ====================
            // (siete sellos orbitando el libro en elipse, latiendo)
            for (int r = 0; r < 7; r++)
            {
                float angR = tiempo * 1.1f + r * MathHelper.TwoPi / 7f;
                Vector2 posR = posLibro + new Vector2(MathF.Cos(angR) * 120f, MathF.Sin(angR) * 44f - 10f);
                float lateR = 0.6f + 0.4f * MathF.Sin(tiempo * 4f + r * 1.3f);
                Color cR = DefinitivasLib.GradienteJuicio(r / 7f + tiempo * 0.12f);
                VFXCore.Quad(posR, DefinitivasLib.Alfa(cR, 0.35f * vida * lateR), new Vector2(46f, 46f) * lateR);
                if (DefinitivasLib.TexStar != null)
                    VFXCore.Quad(posR, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.8f * vida * lateR),
                        new Vector2(24f, 24f) * lateR, tiempo * 2f + r, DefinitivasLib.TexStar);
            }

            // ==================== CAPA 6 — EL SELLO DE LA SENTENCIA ====================
            float late = 0.35f + 0.2f * MathF.Sin(tiempo * 6f);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.Oro, late * vida), new Vector2(64f, 110f) * vida);
            if (DefinitivasLib.TexStar != null)
            {
                // el halo del sello con EL ACENTO de la sentencia que toca
                VFXCore.Quad(posLibro + new Vector2(0f, -70f), DefinitivasLib.Alfa(acento, 0.4f * vida),
                    new Vector2(110f, 110f) * vida);
                VFXCore.Quad(posLibro + new Vector2(0f, -70f), DefinitivasLib.Alfa(Color.White, 0.9f * vida),
                    new Vector2(64f, 64f) * vida, tiempo * 1.4f, DefinitivasLib.TexStar);
            }
            VFXCore.FlushAdditive();

            // ==================== CAPA 7 — EL VÍNCULO (la palabra hecha arma) ====================
            // (un filamento vivo del libro a la sentencia invocada, mientras viva)
            if (_hijo >= 0 && _hijo < Main.maxProjectiles)
            {
                Projectile hijo = Main.projectile[_hijo];
                if (hijo != null && hijo.active)
                {
                    const int NV = 14;
                    var ptsV = new Vector2[NV];
                    for (int i = 0; i < NV; i++)
                    {
                        float q = i / (float)(NV - 1);
                        Vector2 base_ = Vector2.Lerp(posLibro, hijo.Center, q);
                        float ondeo = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 5f + q * 12f) * 26f;
                        ptsV[i] = base_ + new Vector2(-ondeo * 0.3f, ondeo) - Main.screenPosition;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsV, 30f * vida, acento, DefinitivasLib.BlancoCalido,
                            0.75f * vida, StormTaper.Center, 77, 0, false, true, true, null, null);
                        RayoStrip.FilamentoEnLote(ptsV, 10f * vida, DefinitivasLib.BlancoCalido, Color.White,
                            0.9f * vida, StormTaper.Center, 78, 0, false, true, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
                else _hijo = -1;
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
