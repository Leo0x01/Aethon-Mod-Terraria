using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using static Terraria.ModLoader.ModContent;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ======================================================================
    //  DevoracionEterna — v6.115 — EL GRIMORIO DEVORA AL PORTADOR.
    //
    //  LA LETRA DEL USUARIO: «al usar Mundo Interior del Eterno se hace
    //  una animacion donde el grimorio del eterno devora al jugador de
    //  la misma forma que devora a los jefes, pero sin hacerle daño, en
    //  el momento en que es devorado el mundo se llena de bruma solo en
    //  la animacion y el jugador es transportado el nuevo mundo».
    //
    //  LA COREOGRAFÍA DEL TRAGO (tres actos, ~2,5 s):
    //   · EL DESCENSO (0-35): el grimorio MATERIALIZA sobre el portador
    //     (el aura violeta, el corazón dorado y el anillo de seis runas
    //     — la misma vestimenta de LA LLAMADA) mientras el polvo de oro
    //     del suelo se alza.
    //   · LA DEVORACIÓN (35-115): EL LIBRO SE ABRE — la FAUCES crece en
    //     su centro (el disco del vacío con el aro dorado) y el portador
    //     SUBE hacia ella: la espiral de páginas y esencias lo envuelve,
    //     el temblor crece y el libro se inclina a recibirlo. SIN DAÑO:
    //     el trago lleva al interior, no a la nada.
    //   · EL TRAGO (115-130): el portador entra — el ESTALLIDO de 36
    //     páginas, el rugido, el libro SE CIERRA de un golpe… y EL MUNDO
    //     SE LLENA DE BRUMA (SubmundoGrimorio.BrumaFaro: la inundación
    //     violeta que lo tapa TODO mientras la puerta gira).
    //  Detrás de la bruma nace el interior — y su REVELACIÓN de niebla
    //  (la que se disipa mostrando el mundo nuevo) toma el relevo.
    // ======================================================================
    public class DevoracionEterna : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LOS TIEMPOS DE LA CASA ===
        private const int TAbre = 35;        // el libro se abre
        private const int TTrago = 115;      // el portador entra
        private const int TEntrar = 132;     // la puerta gira (detrás de la bruma)
        private const int TMuere = 165;      // la disolución

        private static Texture2D _libro;
        private static bool _texturasProbadas;

        private bool _entregado;
        private bool _estallo;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 200;
            Projectile.friendly = false;      // SIN DAÑO — la letra
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TMuere + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        /// <summary>EL LIBRO: anclado sobre el punto del uso, meciéndose.</summary>
        private Vector2 PosLibro => Projectile.Center + new Vector2(0f, -140f) +
            new Vector2(0f, MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f) * 5f);

        public override void AI()
        {
            float t = Projectile.ai[0];
            Projectile.ai[0] = t + 1f;
            Projectile.velocity = Vector2.Zero;

            Player p = Main.player[Projectile.owner];
            bool dueñoVivo = p != null && p.active && !p.dead;
            Vector2 libro = PosLibro;

            // === EL PORTADOR ES PRESA (controles fuera + la subida) ===
            if (dueñoVivo && p.whoAmI == Main.myPlayer)
            {
                // best effort: el lerp de POSICIÓN es el que manda
                p.controlLeft = false;
                p.controlRight = false;
                p.controlUp = false;
                p.controlDown = false;
                p.controlJump = false;
                p.controlUseItem = false;
                p.controlUseTile = false;

                if (Projectile.ai[0] >= TAbre && Projectile.ai[0] < TTrago)
                {
                    // LA SUBIDA: cada vez más decidida hacia las fauces.
                    float f = MathHelper.Clamp((Projectile.ai[0] - TAbre) / (float)(TTrago - TAbre), 0f, 1f);
                    float k = 0.045f + 0.075f * f * f;
                    try
                    {
                        p.Center = Vector2.Lerp(p.Center, libro + new Vector2(0f, 8f), k);
                        p.velocity = Vector2.Zero;
                        p.gravDir = 1f;
                        p.maxFallSpeed = 0f;
                    }
                    catch { }
                }
            }

            // === EL POLVO DE ORO DEL SUELO (el descenso) ===
            if (Projectile.ai[0] < TAbre + 12 && !Main.dedServ && Main.netMode != 2)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-44f, 44f), Main.rand.NextFloat(-4f, 10f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1.4f, 3.2f)),
                        160, new Color(255, 214, 130), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LA ESPIRAL DE LA DEVORACIÓN: páginas y esencias SUBEN
            //     del portador a las fauces (la misma dirección del
            //     festín: la presa hacia el libro) ===
            if (Projectile.ai[0] >= TAbre && Projectile.ai[0] < TTrago && !Main.dedServ && Main.netMode != 2)
            {
                if (Main.rand.NextBool(2) && dueñoVivo)
                {
                    Vector2 desde = p.Center + Main.rand.NextVector2Circular(26f, 34f);
                    Vector2 hacia = libro + new Vector2(0f, 6f);
                    Vector2 tang = new Vector2(-(hacia - desde).Y, (hacia - desde).X);
                    Vector2 vel = (hacia - desde) * 0.045f + tang * 0.016f;
                    Dust d = Dust.NewDustPerfect(desde,
                        Main.rand.NextBool(3) ? DustID.Enchanted_Pink : DustID.GoldFlame,
                        vel, 170,
                        Main.rand.NextBool(3) ? new Color(190, 150, 240) : new Color(255, 230, 160),
                        Main.rand.NextFloat(0.8f, 1.3f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // el temblor del conjuro crece con la cercanía.
                if (Main.rand.NextBool(5))
                {
                    float f = MathHelper.Clamp((Projectile.ai[0] - TAbre) / (float)(TTrago - TAbre), 0f, 1f);
                    OndaLib.Kick(2 + (int)(5f * f));
                }
            }

            // === EL TRAGO: el estallido de páginas + el rugido ===
            if (Projectile.ai[0] >= TTrago && !_estallo && !Main.dedServ)
            {
                _estallo = true;
                if (Main.netMode != 2)
                {
                    for (int i = 0; i < 36; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(libro,
                            i % 3 == 0 ? DustID.Enchanted_Pink : DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2.0f, 6.5f),
                            180, i % 3 == 0 ? new Color(190, 150, 240) : new Color(255, 230, 160), 1.1f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    OndaLib.Kick(9);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, libro);
                }
            }

            // === LA BRUMA INUNDA (solo en la animación — la letra): la
            //     transición vive ESCONDIDA detrás de la inundación. ===
            if (Projectile.ai[0] >= TTrago - 14)
            {
                float f = MathHelper.Clamp((Projectile.ai[0] - (TTrago - 14)) / (float)(TEntrar - (TTrago - 14)), 0f, 1f);
                SubmundoGrimorio.BrumaFaro = f * f * (3f - 2f * f); // smoothstep
            }

            // === LA PUERTA GIRA (detrás de la bruma llena) ===
            if (Projectile.ai[0] >= TEntrar && !_entregado)
            {
                _entregado = true;
                if (dueñoVivo)
                    SubmundoGrimorio.Entrar(p);
            }

            if (Projectile.ai[0] >= TMuere)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == 2) return false;
            try
            {
                if (!AsegurarTexturas()) return false;
                float t = Projectile.ai[0];
                Vector2 libroPantalla = PosLibro - Main.screenPosition;

                SpriteBatch sb = Main.spriteBatch;
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow", (AssetRequestMode)1).Value;

                // === EL NACIMIENTO DEL LIBRO (el descenso) ===
                float nace = MathHelper.Clamp(t / 24f, 0f, 1f);
                float mawF = MathHelper.Clamp((t - TAbre) / (float)(TTrago - TAbre), 0f, 1f);
                float cerro = t >= TTrago
                    ? MathHelper.Clamp((t - TTrago) / 14f, 0f, 1f)
                    : 0f; // el libro SE CIERRA tras el trago
                float vivo = 1f - cerro * 0.35f;

                // EL AURA VIOLETA (el vacío que lo abraza — pulsa).
                float pulso = 0.75f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
                sb.Draw(glow, libroPantalla, null,
                    new Color(150, 84, 235) * (0.36f * nace * pulso * vivo),
                    0f, new Vector2(32f, 32f), new Vector2(7.0f + 1.6f * mawF, 7.0f + 1.6f * mawF),
                    SpriteEffects.None, 0f);

                // EL CORAZÓN DORADO DE LA CARGA (crece con la devoración).
                sb.Draw(glow, libroPantalla, null,
                    new Color(255, 214, 130) * (0.16f + 0.40f * mawF * (1f - cerro)),
                    0f, new Vector2(32f, 32f),
                    new Vector2(3.2f + 2.8f * mawF, 3.2f + 2.8f * mawF),
                    SpriteEffects.None, 0f);

                // === LAS FAUCES (la boca del vacío que se abre en el
                //     centro del libro — el disco con su aro dorado) ===
                if (mawF > 0.01f)
                {
                    float fauces = mawF * (1f - cerro * 0.9f);
                    float radio = 8f + 46f * fauces;
                    // el velo interior del vacío (el fondo de la boca).
                    sb.Draw(glow, libroPantalla + new Vector2(0f, 4f), null,
                        new Color(30, 14, 52) * (0.85f * fauces),
                        0f, new Vector2(32f, 32f),
                        new Vector2(radio / 26f, radio / 30f),
                        SpriteEffects.None, 0f);
                    // el disco de la nada.
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)(libroPantalla.X - radio * 0.72f),
                                      (int)(libroPantalla.Y + 4f - radio * 0.50f),
                                      (int)(radio * 1.44f), (int)(radio * 1.0f)),
                        new Color(12, 6, 22) * (0.92f * fauces));
                    // el aro dorado de la boca (la hembra del trago).
                    sb.Draw(glow, libroPantalla + new Vector2(0f, 4f), null,
                        new Color(255, 214, 130) * (0.55f * fauces),
                        0f, new Vector2(32f, 32f),
                        new Vector2(radio / 22f, radio / 26f),
                        SpriteEffects.None, 0f);
                }

                // === EL LIBRO (36×49 ×3.2 — el mismo de LA LLAMADA):
                //     meciéndose, ABRIÉNDOSE (estira su X e inclina su
                //     frente hacia la presa) y CERRÁNDOSE de un golpe
                //     tras el trago. ===
                float esc = 3.2f * (0.55f + 0.45f * nace) * (1f + 0.16f * mawF * (1f - cerro));
                float rot = MathF.Sin(Main.GlobalTimeWrappedHourly * 0.9f) * 0.09f
                    + 0.38f * mawF * (1f - cerro); // la frente se rinde a la presa
                Vector2 orig = new Vector2(_libro.Width * 0.5f, _libro.Height * 0.5f);
                sb.Draw(_libro, libroPantalla, null,
                    Color.White * nace * vivo,
                    rot, orig, esc, SpriteEffects.None, 0f);

                // === EL ANILLO DE LAS SEIS RUNAS DORADAS (SigiloLib —
                //     la vestimenta de LA LLAMADA) ===
                VFXCore.Begin();
                try
                {
                    float giro = Main.GlobalTimeWrappedHourly * 1.1f;
                    float anillo = nace * vivo * (0.55f + 0.45f * mawF);
                    for (int r = 0; r < 6; r++)
                    {
                        float ang = giro + r * MathHelper.TwoPi / 6f;
                        Vector2 mundo = PosLibro +
                            new Vector2(MathF.Cos(ang) * 98f, MathF.Sin(ang) * 30f);
                        SigiloLib.Runa(mundo, -ang + MathHelper.PiOver2,
                            SigiloLib.RunasSolares,
                            (r * 3 + (int)Projectile.owner) % SigiloLib.RunasSolares.Length,
                            1.15f, Main.GlobalTimeWrappedHourly, r * 1.3f,
                            DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                            anillo);
                    }
                    VFXCore.FlushAdditive();
                }
                finally
                {
                    VFXCore.ReabrirLoteVanilla();
                }
            }
            catch { }
            return false;
        }

        private static bool AsegurarTexturas()
        {
            if (_texturasProbadas && _libro == null) return false;
            if (_libro != null) return true;
            try
            {
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
