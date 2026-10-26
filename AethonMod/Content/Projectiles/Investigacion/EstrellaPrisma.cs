using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigacion
{
    /// <summary>
    /// ESTRELLAPRISMA — v6.54.0 — LA ESTRELLA DE LA CONSTELACIÓN.
    ///
    /// La conducta del ConstellationStar del jefe (Task 74-a): orbita
    /// al dueño, se REPELE de sus hermanas y suelta chispas; cuando dos
    /// estrellas están cerca, CADA UNA traza LA LÍNEA (la aguja
    /// LineaConstelacion en su mitad superior, rotada desde cada
    /// extremo — 2 pasadas glow + núcleo) — y la línea TIENE DAÑO
    /// (CheckAABBvLineCollision, la colisión real del original). La
    /// primera del grupo dibuja EL ÁREA (AreaConstelacion — la viñeta
    /// ×2 giros, la técnica del marcador del StarSigns).
    /// </summary>
    public class EstrellaPrisma : ModProjectile
    {
        public override string Texture =>
            "AethonMod/Content/Effects/Procedural/Investigacion/EstrellaConstelacion";

        private const int Vida = 600;
        private const float RadioLinea = 340f;

        // ai[0] = ¿dibuja el área? · ai[1] = ángulo orbital propio
        private bool DibujaArea => Projectile.ai[0] > 0.5f;
        private float AnguloOrbital { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        private static Texture2D _linea, _area;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            // === LA ÓRBITA (ConstellationStar.cs:58-67 del puerto) ===
            AnguloOrbital += 0.022f;
            float radio = 96f + MathF.Sin(Projectile.timeLeft * 0.03f +
                Projectile.whoAmI * 2.1f) * 18f;
            Vector2 objetivo = duenio.Center + new Vector2(
                MathF.Cos(AnguloOrbital + Projectile.whoAmI * 1.3f) * radio,
                MathF.Sin(AnguloOrbital + Projectile.whoAmI * 1.3f) * radio * 0.55f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, objetivo, 0.12f);

            // === LA REPULSIÓN entre hermanas (la conducta del original:
            //     las estrellas NUNCA se tocan — el cielo se ordena solo) ===
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hermana = Main.projectile[i];
                if (hermana == null || !hermana.active || hermana.whoAmI == Projectile.whoAmI ||
                    hermana.type != Type || hermana.owner != Projectile.owner)
                    continue;
                float d = hermana.Center.Distance(Projectile.Center);
                if (d < 70f && d > 0.1f)
                {
                    Vector2 empuje = Vector2.Normalize(Projectile.Center - hermana.Center)
                        * (70f - d) * 0.14f;
                    Projectile.Center += empuje;
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.9f);
                if (Main.rand.NextBool(14))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        Main.rand.NextVector2Circular(0.6f, 0.6f), 180,
                        ColorGoozma.ValueAt(Main.GlobalTimeWrappedHourly * 8f), 0.55f);
                    d.noGravity = true;
                }
            }

            // === LAS LÍNEAS (cada par cercano: el daño de la
            ///    constelación — la colisión REAL del original) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Projectile.timeLeft % 12f == 0f)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile hermana = Main.projectile[i];
                    if (hermana == null || !hermana.active ||
                        hermana.whoAmI <= Projectile.whoAmI ||  // cada par UNA vez
                        hermana.type != Type || hermana.owner != Projectile.owner)
                        continue;
                    if (hermana.Center.Distance(Projectile.Center) > RadioLinea) continue;

                    for (int n = 0; n < Main.maxNPCs; n++)
                    {
                        NPC npc = Main.npc[n];
                        if (npc == null || !npc.active || npc.friendly || npc.dontTakeDamage)
                            continue;
                        float puntoColision = 0f;
                        if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size,
                                Projectile.Center, hermana.Center, 26f, ref puntoColision))
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                            (int)(Projectile.damage * 0.7f), 1.5f);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                if (_linea == null || _linea.IsDisposed)
                {
                    _linea = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/LineaConstelacion",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    _area = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/AreaConstelacion",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                float t = Main.GlobalTimeWrappedHourly;
                Color arco = ColorGoozma.ValueAt(t * 8f + Projectile.whoAmI);

                // === EL ÁREA (la viñeta del marcador ×2 giros — la
                //     técnica del StellarGeliath: tintes A=0 aditivos) ===
                if (DibujaArea)
                {
                    Player duenio = Main.player[Projectile.owner];
                    float latido = 0.75f + 0.25f * MathF.Sin(t * 2.2f);
                    Rectangle marco = _area.Frame();
                    for (int k = 0; k < 2; k++)
                    {
                        float escala = (2.1f + MathF.Sin(t * 0.8f) * 0.12f) * (1f - k * 0.02f);
                        Color tinte = k == 0
                            ? new Color(28, 10, 76) * (0.5f * latido)
                            : new Color(66, 24, 148) * (0.4f * latido);
                        Main.spriteBatch.Draw(_area,
                            duenio.Center - Main.screenPosition, marco, tinte,
                            t * (0.25f + k * 0.15f), _area.Size() * 0.5f,
                            escala, SpriteEffects.None, 0f);
                    }
                }

                // === LAS LÍNEAS hacia las hermanas (la aguja en su
                //     MITAD SUPERIOR con origen centro-abajo — la
                //     técnica del ConstellationLine del jefe) ===
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile hermana = Main.projectile[i];
                    if (hermana == null || !hermana.active ||
                        hermana.whoAmI <= Projectile.whoAmI ||
                        hermana.type != Type || hermana.owner != Projectile.owner)
                        continue;
                    float dist = hermana.Center.Distance(Projectile.Center);
                    if (dist > RadioLinea) continue;

                    DibujarLinea(Projectile.Center, hermana.Center, dist, t);
                }

                // === LA ESTRELLA (en CRUZ ×2 con wobble — la técnica
                //     del ConstellationStar: rot 0 y π/2 estirada) ===
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float wobble = 1f + MathF.Sin(t * 3f + Projectile.whoAmI) * 0.15f;
                Texture2D texPropia = TextureAssets.Projectile[Projectile.type].Value;
                Rectangle frame = texPropia.Frame();
                Vector2 origen = frame.Size() * 0.5f;
                for (int k = 0; k < 2; k++)
                {
                    float rot = k == 0 ? 0f : MathHelper.PiOver2;
                    Vector2 esc = k == 0 ? new Vector2(1.1f, 1.1f) * wobble
                                         : new Vector2(1.1f, 1.5f) * wobble;
                    Main.spriteBatch.Draw(texPropia,
                        pos, frame, arco * 0.9f, rot, origen, esc, SpriteEffects.None, 0f);
                }
                // EL GLOW de la estrella (los soles rúnicos de la casa).
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    arco * 0.45f, 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    0.9f * wobble, SpriteEffects.None, 0f);
            }
            catch { }
            return false;
        }

        /// <summary>LA LÍNEA entre dos estrellas: la aguja dibujada
        /// desde CADA extremo hacia el otro (2 pasadas: glow ancho +
        /// núcleo a escala X 0.33 — la receta del original).</summary>
        private static void DibujarLinea(Vector2 a, Vector2 b, float dist, float t)
        {
            float wobble = 1f + MathF.Sin(t * 0.5f + a.X * 0.01f) * 0.2f;
            Rectangle mitad = _linea.Frame(1, 2, 0, 0);   // SOLO la mitad superior
            Vector2 origen = mitad.Size() * new Vector2(0.5f, 1f);  // centro-ABAJO

            void Pasa(Vector2 desde, Vector2 hasta, Color color, float escalaX, float extra)
            {
                float rot = (hasta - desde).ToRotation() - MathHelper.PiOver2;
                float largoPx = dist / (mitad.Height * 1.5f);
                Main.spriteBatch.Draw(_linea, desde - Main.screenPosition, mitad, color,
                    rot, origen, new Vector2(escalaX * wobble, largoPx * (1f + extra)),
                    SpriteEffects.None, 0f);
            }

            Color arco = ColorGoozma.ValueAt(t * 6f + a.X * 0.05f);
            Color glowOscuro = new Color(84, 42, 168);
            // EL GLOW (2 pasadas anchas tenues).
            Pasa(a, b, glowOscuro * 0.35f, 1.1f, 0.05f);
            Pasa(b, a, glowOscuro * 0.35f, 1.1f, 0.05f);
            // EL NÚCLEO (2 pasadas finas a X 0.33 — la vena lila).
            Pasa(a, b, arco * 0.85f, 0.33f, 0.02f);
            Pasa(b, a, arco * 0.85f, 0.33f, 0.02f);
        }
    }
}
