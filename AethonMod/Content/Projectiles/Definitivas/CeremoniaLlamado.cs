using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;
using AethonMod.Content.VFX;
using static Terraria.ModLoader.ModContent;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ======================================================================
    //  CeremoniaLlamado — v6.114 — LA LLAMADA DEL ALTAR.
    //
    //  LA LETRA DEL USUARIO: «en la llamada debe aparecer el altar y
    //  justo encima debe estar el grimorio del eterno».
    //
    //  LA COREOGRAFÍA (el homenaje a la Emperatriz que la casa ya
    //  rendía en la entrada): al pronunciar EL NOMBRE DE AETHON no
    //  nacen los fragmentos del recuerdo de la nada — EL ALTAR DE LA
    //  CONSAGRACIÓN SE MATERIALIZA delante del portador (54×130 px de
    //  icono de la casa, alzado del suelo), y JUSTO ENCIMA flota EL
    //  GRIMORIO DEL ETERNO (el arma final: la textura del libro
    //  hambriento, ×2.4, meciéndose), rodeado del ANILLO DE SEIS RUNAS
    //  DORADAS (SigiloLib) mientras el polvo de oro del altar SUBE al
    //  libro. A los 130 ticks EL LIBRO SE ABRE: los DOS fragmentos del
    //  recuerdo nacen con la fórmula EXACTA de la Emperatriz (SpawnBoss
    //  ±600/−600 alrededor del ALTAR), la pantalla estalla en páginas y
    //  la ceremonia se disuelve.
    //
    //  MP: el servidor clava el ancla y corre la IA; tML sincroniza el
    //  proyectil y los jefes nacen del lado del servidor.
    // ======================================================================
    public class CeremoniaLlamado : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LOS TIEMPOS DE LA CASA ===
        private const int TAltarNace = 25;       // ticks 0-24: el altar se alza
        private const int TConvoca = 130;         // el libro se abre
        private const int TMuere = 186;           // la disolución termina

        private static Texture2D _altar;
        private static Texture2D _libro;
        private static bool _texturasProbadas;

        private bool _convocado;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 190;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TMuere + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            float t = Projectile.ai[0];
            Projectile.ai[0] = t + 1f;
            Projectile.velocity = Vector2.Zero;

            // === EL ALTAR SE ALZA: polvo de oro del suelo ===
            if (Projectile.ai[0] < TAltarNace + 10 && !Main.dedServ && Main.netMode != 2)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-52f, 52f), Main.rand.NextFloat(-6f, 4f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(1.2f, 3.0f)),
                        160, new Color(255, 214, 130), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LA CARGA: el polvo del altar SUBE al libro ===
            if (Projectile.ai[0] >= TAltarNace && Projectile.ai[0] < TConvoca && !Main.dedServ && Main.netMode != 2)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 desde = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-30f, 30f), -Main.rand.NextFloat(4f, 30f));
                    Vector2 hacia = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-14f, 14f), -148f);
                    Dust d = Dust.NewDustPerfect(desde, DustID.Enchanted_Gold,
                        (hacia - desde) * 0.02f + new Vector2(0f, -1.1f),
                        170, new Color(255, 226, 150), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // el temblor sutil del conjuro (el portador lo siente).
                if (Main.rand.NextBool(9))
                    OndaLib.Kick(3);
            }

            // === EL LIBRO SE ABRE: LA CONVOCATORIA (solo el servidor) ===
            if (Projectile.ai[0] >= TConvoca && !_convocado && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _convocado = true;
                AethonBoss.FusionDecretada = false;
                AethonBoss.TurnoAtaque = -1;

                Vector2 ancla = Projectile.Center;
                Vector2 posLuz = ancla + new Vector2(-600f, -600f) +
                    Main.rand.NextVector2Circular(100f, 100f);
                NPC.SpawnBoss((int)posLuz.X, (int)posLuz.Y,
                    NPCType<AethonBoss>(), Projectile.owner);

                Vector2 posOscuro = ancla + new Vector2(600f, -600f) +
                    Main.rand.NextVector2Circular(100f, 100f);
                NPC.SpawnBoss((int)posOscuro.X, (int)posOscuro.Y,
                    NPCType<AethonBossOscuro>(), Projectile.owner);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, ancla);

                if (Main.netMode == 0)
                {
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Aethon.LlamadaCeremonia"),
                        new Color(255, 226, 150));
                }
            }

            // === EL ESTALLIDO DE PÁGINAS (la apertura, todas las máquinas) ===
            if (Math.Abs(Projectile.ai[0] - TConvoca) < 0.5f && !Main.dedServ && Main.netMode != 2)
            {
                for (int i = 0; i < 36; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(0f, -148f),
                        i % 3 == 0 ? DustID.Enchanted_Pink : DustID.GoldFlame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2.0f, 6.5f),
                        180, i % 3 == 0 ? new Color(190, 150, 240) : new Color(255, 230, 160), 1.1f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                OndaLib.Kick(8);
            }

            if (Projectile.ai[0] >= TMuere)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == 2) return false;
            try
            {
                if (!AsegurarTexturas()) return false;
                float t = Projectile.ai[0];
                Vector2 basePantalla = Projectile.Center - Main.screenPosition;

                // === LOS TIEMPOS ===
                float altarAlfa = MathHelper.Clamp(t / 18f, 0f, 1f);
                float subirAltar = (1f - altarAlfa) * 44f;
                bool convocado = t >= TConvoca;
                float disolver = convocado
                    ? MathHelper.Clamp((t - TConvoca) / (TMuere - TConvoca), 0f, 1f)
                    : 0f;

                SpriteBatch sb = Main.spriteBatch;

                // === EL ALTAR (34×50 ×2.6 = 88×130, anclado abajo) ===
                if (altarAlfa > 0.01f && disolver < 0.98f)
                {
                    Vector2 pos = basePantalla + new Vector2(0f, -65f + subirAltar);
                    float escAltar = 2.6f * (0.55f + 0.45f * altarAlfa);
                    Vector2 orig = new Vector2(_altar.Width * 0.5f, _altar.Height);
                    // el resplandor dorado bajo el altar (el conjuro arde).
                    sb.Draw(ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow",
                        (AssetRequestMode)1).Value,
                        pos - new Vector2(0f, -14f), null,
                        new Color(255, 200, 110) * (0.30f * altarAlfa * (1f - disolver)),
                        0f, new Vector2(32f, 32f), new Vector2(4.4f, 2.2f), SpriteEffects.None, 0f);
                    sb.Draw(_altar, pos, null, Color.White * altarAlfa * (1f - disolver * 0.85f),
                        0f, orig, escAltar, SpriteEffects.None, 0f);
                }

                // === EL GRIMORIO DEL ETERNO (36×49 ×2.4, justo encima) ===
                if (t >= TAltarNace - 8 && disolver < 0.99f)
                {
                    float nace = MathHelper.Clamp((t - (TAltarNace - 8)) / 22f, 0f, 1f);
                    float mecer = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f) * 6f;
                    Vector2 posLibro = basePantalla + new Vector2(0f, -148f - (1f - nace) * 60f + mecer);
                    float escLibro = 2.4f * (0.6f + 0.4f * nace);
                    Vector2 origLibro = new Vector2(_libro.Width * 0.5f, _libro.Height * 0.5f);

                    // el aura violeta del libro (el vacío que lo abraza).
                    float pulso = 0.75f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
                    sb.Draw(ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow",
                        (AssetRequestMode)1).Value,
                        posLibro, null,
                        new Color(150, 84, 235) * (0.34f * nace * pulso * (1f - disolver)),
                        0f, new Vector2(32f, 32f), new Vector2(6.5f, 6.5f), SpriteEffects.None, 0f);
                    // el corazón dorado de la carga (crece hasta la apertura).
                    float carga = convocado ? 0.4f : MathHelper.Clamp((t - TAltarNace) / (TConvoca - TAltarNace), 0f, 1f);
                    sb.Draw(ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow",
                        (AssetRequestMode)1).Value,
                        posLibro, null,
                        new Color(255, 214, 130) * (0.16f + 0.34f * carga * (1f - disolver)),
                        0f, new Vector2(32f, 32f), new Vector2(3.0f + 2.6f * carga, 3.0f + 2.6f * carga),
                        SpriteEffects.None, 0f);

                    sb.Draw(_libro, posLibro, null,
                        Color.White * nace * (1f - disolver * 0.9f),
                        MathF.Sin(Main.GlobalTimeWrappedHourly * 0.9f) * 0.09f,
                        origLibro, escLibro, SpriteEffects.None, 0f);

                    // === EL ANILLO DE LAS SEIS RUNAS DORADAS (SigiloLib) ===
                    VFXCore.Begin();
                    try
                    {
                        float giro = Main.GlobalTimeWrappedHourly * 1.1f;
                        for (int r = 0; r < 6; r++)
                        {
                            float ang = giro + r * MathHelper.TwoPi / 6f;
                            Vector2 mundo = Projectile.Center + new Vector2(0f, -148f) +
                                new Vector2(MathF.Cos(ang) * 92f, MathF.Sin(ang) * 26f);
                            SigiloLib.Runa(mundo + Main.screenPosition * 0f,
                                -ang + MathHelper.PiOver2,
                                SigiloLib.RunasSolares,
                                (r * 3 + (int)Projectile.owner) % SigiloLib.RunasSolares.Length,
                                1.15f, Main.GlobalTimeWrappedHourly, r * 1.3f,
                                DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                                nace * (0.55f + 0.45f * carga) * (1f - disolver));
                        }
                        VFXCore.FlushAdditive();
                    }
                    finally
                    {
                        VFXCore.ReabrirLoteVanilla();
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool AsegurarTexturas()
        {
            if (_texturasProbadas && (_altar == null || _libro == null)) return false;
            if (_altar != null && _libro != null) return true;
            try
            {
                _altar = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Items/Altar/AltarOfrendasItem",
                    (AssetRequestMode)1).Value;
                _libro = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambrientoNervioso",
                    (AssetRequestMode)1).Value;
                return true;
            }
            catch
            {
                _texturasProbadas = true;
                return false;
            }
        }
    }
}
