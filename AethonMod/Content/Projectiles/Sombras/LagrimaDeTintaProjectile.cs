using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// LÁGRIMADETINTA — v6.50.95 — EL DISPARO PROPIO DEL LIBRO.
    /// La letra del usuario: «esto debe cambiar — la descarga prestada: el
    /// disparo del jugador sigue siendo el Nightglow vanilla (proyectil de
    /// prueba) […] te dare un sprite para un disparo, tu has que el
    /// disparo tenga efectos, te dare el codigo como siempre […] quiero
    /// que con este proyectil seas creativo, lo animes y crees una nueva
    /// arma con el proyectil».
    ///
    /// EL SPRITE EXACTO del usuario (607×1344, RLE «sin pérdida · sin
    /// manipulación» — tools/gen_tinta_viva_v65095.py) es una PÁGINA
    /// DOBLADA EN SOMBRA: el fragmento negro con borde violeta y CORAZÓN
    /// DE MARFIL. La .95 lo convierte en LA LÁGRIMA DE TINTA — lo que el
    /// grimorio llora cuando tiene hambre:
    ///
    /// · LA RESPIRACIÓN — 4 frames del strip (Main.projFrames): las
    ///   puntas laten (escala 0.94→1.0 a lo largo) y el corazón de marfil
    ///   se aviva ×1.0→×1.45 por frame (el pulso YA vive en los píxeles;
    ///   el halo aditivo del draw solo lo corona).
    /// · LA CACERÍA — homing al enemigo más cercano (900 px, re-busca
    ///   cada 8 t): arranca lenta y ACELERA (12→19 px/t) girando con
    ///   rumbo capado — como tinta que aprende a caer sobre su presa.
    /// · EL GOTEO — estela de polvo violeta sin gravedad + GOTAS que SÍ
    ///   caen (la tinta gotea de la lágrima en vuelo).
    /// · LAS ESTELAS — 5 fantasmas por oldPos desvaneciéndose (el rastro
    ///   de la página) + el VAIVÉN: un vaivén perpendicular SOLO en el
    ///   draw (el hitbox va honesto, el vuelo va vivo).
    /// · LA MUERTE — al agotar sus 3 penetraciones o su reloj: SALPICADURA
    ///   de tinta (anillo de polvo) + LA MANCHA (ManchaDeTinta: el charco
    ///   que queda un instante donde la lágrima se rompió).
    /// · LA LUZ — violeta de sombra + marfil que late con el frame.
    ///
    /// Coherencia total con el libro: es la SOMBRA DE LA PÁGINA hecha
    /// proyectil — atravesando muros (tileCollide false: las sombras no
    /// conocen puertas), persiguiendo como el Nervioso persigue.
    /// </summary>
    public class LagrimaDeTintaProjectile : ModProjectile
    {
        // LA CACERÍA
        private const float RADIO_BUSCA = 900f;    // a quién busca
        private const int TICKS_REBUSCA = 8;       // cada cuánto re-busca
        private const float VEL_INICIAL = 12f;     // arranca lenta…
        private const float VEL_MAXIMA = 19f;      // …y acelera al caer
        private const float GIRO_MAX = 0.11f;      // rad/t de rumbo

        // LA RESPIRACIÓN
        private const int TICKS_POR_FRAME = 5;

        // EL GOTEO
        private const int TICKS_GOTA = 7;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;   // la respiración del strip
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.tileCollide = false;         // las sombras no conocen puertas
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;               // tres presas por lágrima
            Projectile.timeLeft = 300;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            float edad = Projectile.ai[1]++;

            // === LA RESPIRACIÓN: el corazón late cada 5 t ===
            if (++Projectile.frameCounter >= TICKS_POR_FRAME)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) & 3;
            }

            // === LA CACERÍA ===
            if (edad % TICKS_REBUSCA == 0f)
            {
                NPC presa = Presa;
                if (presa == null || !presa.active || presa.life <= 0)
                    presa = BuscarPresa();
                Projectile.ai[0] = presa != null ? presa.whoAmI + 1 : 0f;
            }

            NPC objetivo = Presa;
            if (objetivo != null && objetivo.active && objetivo.life > 0)
            {
                // acelera como la tinta que aprende a caer
                float vel = MathHelper.Clamp(Projectile.velocity.Length() + 0.08f,
                    VEL_INICIAL, VEL_MAXIMA);
                Vector2 hacia = (objetivo.Center - Projectile.Center)
                    .SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float deseado = hacia.ToRotation();
                float actual = Projectile.velocity.ToRotation();
                float giro = MathHelper.Clamp(MathHelper.WrapAngle(deseado - actual),
                    -GIRO_MAX, GIRO_MAX);
                Projectile.velocity = (actual + giro).ToRotationVector2() * vel;
            }
            else if (Projectile.velocity.Length() < VEL_INICIAL)
            {
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * VEL_INICIAL;
            }

            // LA ROTACIÓN: alineada al vuelo con el temblor de la tinta
            Projectile.rotation = Projectile.velocity.ToRotation()
                + MathF.Sin(edad * 0.31f + Projectile.whoAmI) * 0.07f;

            // === LA LUZ — violeta de sombra + marfil que late con el frame ===
            if (Main.netMode != NetmodeID.Server)
            {
                float latido = 0.55f + 0.25f * (Projectile.frame == 2 ? 1f
                    : Projectile.frame == 1 ? 0.55f : Projectile.frame == 3 ? 0.2f : 0.4f);
                Lighting.AddLight(Projectile.Center, 0.30f * latido, 0.20f * latido, 0.48f * latido);
                Lighting.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * 10f,
                    0.45f * latido, 0.42f * latido, 0.34f * latido);
            }

            // === EL GOTEO — la estela de la lágrima ===
            if (Main.netMode != NetmodeID.Server)
            {
                // polvo violeta que flota (la sombra del vuelo)
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f,
                        DustID.Shadowflame,
                        -Projectile.velocity * 0.04f + new Vector2(
                            Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.6f, 0.2f)),
                        128, default, 0.65f);
                    d.noGravity = true;
                }
                // la GOTA: tinta que cae de verdad (con gravedad)
                if (edad % TICKS_GOTA == 0f)
                {
                    Dust g = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-3f, 3f), 8f),
                        DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.3f, 0.9f)),
                        128, default, 0.5f);
                    g.noGravity = false;
                }
            }

            // la aceleración del arranque: nace con un pequeño impulso extra
        }

        /// <summary>LA PRESA: el enemigo válido más cercano en RADIO_BUSCA
        /// (los vecinos de las casas, jamás — esta tinta no come amigos).</summary>
        private NPC BuscarPresa()
        {
            NPC mejor = null;
            float mejorD = RADIO_BUSCA * RADIO_BUSCA;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.townNPC || n.friendly || NPCID.Sets.ActsLikeTownNPC[n.type]) continue;
                float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA SALPICADURA: anillo de tinta donde mordió
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    float ang = (float)(i / 10.0 * Math.PI * 2.0);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        ang.ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f), 128,
                        default, 0.8f);
                    d.noGravity = true;
                }
            }
            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.25f).WithVolumeScale(0.7f), Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // LA MUERTE DE LA LÁGRIMA: se rompe en tinta — la SALPICADURA
            // grande + LA MANCHA que queda un instante en el suelo
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 16; i++)
                {
                    float ang = (float)(i / 16.0 * Math.PI * 2.0);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        ang.ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 128,
                        default, 1.0f);
                    d.noGravity = Main.rand.NextBool(2);
                }
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                    Vector2.Zero, ModContent.ProjectileType<ManchaDeTinta>(),
                    0, 0f, Projectile.owner, 0f, 0f);
            }
            Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.6f), Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(lightColor); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo(Color luz)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int alto = tex.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * alto, tex.Width, alto);
            Vector2 origen = new Vector2(tex.Width * 0.5f, alto * 0.5f);
            float edad = Projectile.ai[1];

            // EL VAIVÉN: la lágrima se mece PERPENDICULAR al vuelo — SOLO
            // en el draw (el hitbox viaja honesto)
            Vector2 rumbo = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-rumbo.Y, rumbo.X);
            Vector2 vaiven = perp * MathF.Sin(edad * 0.23f + Projectile.whoAmI * 1.7f) * 2.4f;

            // === LAS ESTELAS — 5 fantasmas por donde pasó la página (oldPos[0]
            //     = hace 1 t … oldPos[9] = hace 10 t: los RECIENCIOS) ===
            for (int i = 5; i >= 1; i--)
            {
                if (i >= Projectile.oldPos.Length) continue;
                Vector2 vieja = Projectile.oldPos[i];
                if (vieja == Vector2.Zero) continue;
                float f = 1f - i / 6f;                         // 0.17..0.83
                Color fantasma = SombrasLib.Alfa(SombrasLib.Violeta, 0.34f * f);
                float rotVieja = i < Projectile.oldRot.Length
                    ? Projectile.oldRot[i] : Projectile.rotation;
                Main.spriteBatch.Draw(tex, vieja + Projectile.Size * 0.5f - Main.screenPosition,
                    frame, fantasma, rotVieja, origen, 0.92f * f, SpriteEffects.None, 0f);
            }

            Vector2 pos = Projectile.Center - Main.screenPosition + vaiven;

            // === EL HALO ADITIVO — la corona del latido (violeta fuera,
            //     marfil dentro: el corazón manda en el frame 2) ===
            VFXCore.Begin();
            float pulso = Projectile.frame == 2 ? 1f : Projectile.frame == 1 ? 0.7f
                : Projectile.frame == 3 ? 0.45f : 0.55f;
            float latido = 0.55f + 0.45f * MathF.Sin(edad * 0.11f);
            VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta,
                (0.16f + 0.10f * pulso) * latido + 0.06f), new Vector2(72f, 72f) * (0.8f + 0.3f * pulso));
            VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Blanco,
                0.22f * pulso * latido), new Vector2(34f, 46f), Projectile.rotation);
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();

            // === LA LÁGRIMA — el sprite del usuario, respirando ===
            Main.spriteBatch.Draw(tex, pos, frame, luz, Projectile.rotation,
                origen, 1f, SpriteEffects.None, 0f);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }

    /// <summary>
    /// MANCHADETINTA — v6.50.95 — LO QUE DEJA LA LÁGRIMA AL ROMPERSE.
    /// La salpicadura de tinta: un charco violeta que se desvanece (36 t)
    /// donde la lágrima se agotó — puro draw (SombrasLib.Charco), sin
    /// daño, sin colisión, sin red. El recuerdo del disparo.
    /// </summary>
    public class ManchaDeTinta : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36;
        }

        public override void AI()
        {
            // LAS GOTAS DEL FINAL: dos últimas gotas que caen al nacer
            if (Projectile.ai[1] == 0f && Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-2.5f, -0.5f)),
                        128, default, 0.55f);
                    d.noGravity = false;
                }
            }
            Projectile.ai[1]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            float t = Projectile.ai[1];
            float desvanece = MathHelper.Clamp(1f - t / 36f, 0f, 1f);
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                SombrasLib.Charco(Projectile.Center, 26f + 14f * (1f - desvanece),
                    0.55f * desvanece, Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 13 + 7);
                // la última luz de la tinta
                if (desvanece > 0.4f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta,
                        0.14f * desvanece), new Vector2(40f, 40f));
                    VFXCore.FlushAdditive();
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
