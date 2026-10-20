using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    // ======================================================================
    //  EL ESCUPIDIZO (v6.51.0) — LA PELÍCULA DE LA MASCOTA DEL DESPERTAR
    //  (la letra del usuario: «a nivel 20 el grimorio escupe al
    //  minigrimorio con una animación donde el grimorio sale del jugador,
    //  se pone encima de él y en un mar de brumas escupe al minigrimorio
    //  como mascota, esto también debe revelar algún texto o algo que el
    //  grimorio le diga al jugador»).
    //
    //  LA ESCENA: el Eterno se DESPRENDE del portador, sube y queda
    //  FLOTANDO ENCIMA de su cabeza entre un mar de brumas; el libro se
    //  ABRE (el párpado de la textura, la boca de luz creciendo)… y de
    //  su interior ESCUPE al minigrimorio (la mascota queda entregada:
    //  el item si no lo tenía, el buff YA — el engendro lo hace el
    //  patrón canónico del buff). La bruma se disipa y el Eterno VUELVE
    //  a la mano del portador (visual: se desvanece hacia él).
    // ======================================================================
    public class EscupidizoProjectile : ModProjectile
    {
        /// <summary>La subida del libro (se desprende y flota encima).</summary>
        private const int T_SUBE = 70;

        /// <summary>La boca se abre y la bruma se arremolina.</summary>
        private const int T_ABRE = 80;

        /// <summary>EL ESCUPIDO (la mascota nace).</summary>
        private const int T_ESCUPE = 150;

        /// <summary>La despedida (bruma + libro vuelven al portador).</summary>
        private const int T_FIN = 300;

        public override string Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;       // el cine es de todos
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            float t = Projectile.localAI[0];

            Player portador = Main.player[Projectile.owner];
            if (portador == null || !portador.active) { Projectile.Kill(); return; }

            // === EL LIBRO SE PONE ENCIMA DEL JUGADOR (la letra): sube
            //     desde el portador hasta 110 px sobre su cabeza, y ahí
            //     FLOTA meciéndose (el mar de brumas lo abraza). ===
            Vector2 meta = portador.Top + new Vector2(
                MathF.Sin((float)Main.time * 0.8f) * 14f, -110f);
            if (t < T_SUBE)
            {
                float p = t / (float)T_SUBE;
                Vector2 arranque = portador.Center + new Vector2(
                    (Projectile.identity % 2 == 0 ? 1f : -1f) * 26f, -20f);
                Projectile.Center = Vector2.Lerp(arranque, meta,
                    p * p * (3f - 2f * p));
            }
            else
            {
                // LA FLOTACIÓN (persigue su puesto — sin teleport).
                Projectile.Center += (meta - Projectile.Center) * 0.18f;
            }

            // LA ROTACIÓN DEL CONJURO (se mece, cada vez más tenso).
            float tension = MathHelper.Clamp((t - T_SUBE) / (float)T_ESCUPE, 0f, 1f);
            Projectile.rotation = MathF.Sin((float)Main.time * 2.4f) *
                (0.06f + 0.10f * tension)
                + MathF.Sin((float)Main.time * 9f) * 0.02f * tension;

            // LA LUZ DEL EVENTO (violeta creciente: algo va a nacer).
            Lighting.AddLight(Projectile.Center,
                0.28f + 0.22f * tension, 0.16f, 0.48f + 0.20f * tension);

            // === EL ESCUPIDO (la AUTORIDAD entrega la mascota; las demás
            //     pantallas ven el mismo teatro por el cable). ===
            if (t >= T_ESCUPE && Projectile.ai[0] < 1f)
            {
                Projectile.ai[0] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Systems.DespertarGrimorioSistema.EntregarMascota(portador);

                // LA MOTA QUE NACE (en todas las pantallas: el mini sale
                // de la boca del grande hacia el hombro del portador).
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(0.5f), Projectile.Center);
                    for (int i = 0; i < 22; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            i % 3 == 0 ? DustID.PurpleTorch : DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-3f, 3f),
                                Main.rand.NextFloat(-2.5f, 1.5f)), 160,
                            new Color(170, 110, 240), 1.1f);
                        d.noGravity = true;
                    }
                }
            }

            // === LA BRUMA QUE RESPIRA (el mar de la escena). ===
            if (!Main.dedServ && Main.rand.NextBool(2) && t < T_FIN - 20)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) *
                    Main.rand.NextFloat(18f, 85f),
                    DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                        Main.rand.NextFloat(-0.9f, -0.2f)),
                    130, new Color(100, 50, 160), 0.9f);
                d.noGravity = true;
            }

            // LA DESPEDIDA (el libro se desvanece hacia su portador).
            if (t >= T_FIN) Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Projectile.localAI[0];
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
                Texture2D cerrado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (tex == null || glow == null) return false;

                Player portador = Main.player[Projectile.owner];
                float despedida = t > T_ESCUPE
                    ? MathHelper.Clamp((t - T_ESCUPE) / (float)(T_FIN - T_ESCUPE), 0f, 1f)
                    : 0f;
                float alfa = 1f - despedida * despedida;
                if (alfa <= 0.02f) return false;

                Vector2 pos = Projectile.Center - Main.screenPosition;
                float tiempo = (float)Main.time * 0.02f;
                Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
                Vector2 origen = frame.Size() * 0.5f;

                // === 1. EL MAR DE BRUMAS (la plataforma del evento:
                //     jirones oscuros + rim violeta debajo del libro). ===
                float escalaBruma = 0.7f + 0.5f * MathHelper.Clamp(t / (float)T_SUBE, 0f, 1f)
                    - 0.5f * despedida;
                for (int i = 0; i < 10; i++)
                {
                    float ang = i / 10f * MathHelper.TwoPi + tiempo * (0.5f + 0.1f * (i % 3));
                    float radio = (46f + 34f * ((i * 29) % 5) / 5f) * escalaBruma;
                    Vector2 p = pos + new Vector2(MathF.Cos(ang) * radio,
                        MathF.Sin(ang) * radio * 0.55f + 16f);
                    Color jiron = new Color(24, 10, 42) * (0.8f * alfa *
                        (0.7f + 0.3f * MathF.Sin(tiempo * 1.9f + i * 2.2f)));
                    float tam = (52f + 26f * ((i * 41) % 7) / 7f) * escalaBruma;
                    Main.spriteBatch.Draw(glow, p, null, jiron, ang,
                        glow.Size() * 0.5f,
                        new Vector2(tam, tam * 0.75f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
                Main.spriteBatch.Draw(glow, pos + new Vector2(0f, 14f), null,
                    new Color(96, 52, 150) * (0.25f * alfa), 0f,
                    glow.Size() * 0.5f,
                    new Vector2(190f, 130f) * escalaBruma / glow.Size(),
                    SpriteEffects.None, 0f);

                // === 2. EL LIBRO GRANDE: se ABRE conforme se acerca el
                //     escupido (el cuerpo del Eterno mientras la boca
                //     crece… y el párpado CERRADO ya escupido: duerme la
                //     siesta de después de comer). ===
                float abre = MathHelper.Clamp((t - T_SUBE) / (float)(T_ESCUPE - T_SUBE), 0f, 1f);
                float esc = 1.15f + 0.10f * MathF.Sin(tiempo * 5f) * abre;
                Color luzLibro = new Color(226, 214, 248) * alfa;
                if (t < T_ESCUPE)
                {
                    Main.spriteBatch.Draw(tex, pos, frame, luzLibro,
                        Projectile.rotation, origen, esc, SpriteEffects.None, 0f);

                    // LA BOCA DE LUZ creciendo (por donde saldrá el mini).
                    if (abre > 0.2f)
                        Main.spriteBatch.Draw(glow, pos + new Vector2(0f, 6f), null,
                            new Color(190, 120, 255) * (0.5f * abre * alfa),
                            0f, glow.Size() * 0.5f,
                            new Vector2(30f + 34f * abre, 20f + 22f * abre) / glow.Size(),
                            SpriteEffects.None, 0f);
                }
                else
                {
                    // ESCUPIÓ: el grande cierra el ojo (la saciedad).
                    Main.spriteBatch.Draw(cerrado, pos, frame, luzLibro,
                        Projectile.rotation, origen, esc, SpriteEffects.None, 0f);
                }

                // === 3. EL MINI NACIENDO (el escupido en persona: el
                //     pequeño sale de la boca y viaja al hombro). ===
                if (t >= T_ESCUPE - 6 && portador != null && portador.active)
                {
                    float viaje = MathHelper.Clamp(
                        (t - (T_ESCUPE - 6)) / 50f, 0f, 1f);
                    Vector2 hombro = portador.Top + new Vector2(-22f, -34f);
                    Vector2 posMini = Vector2.Lerp(
                        pos + new Vector2(0f, 8f), hombro,
                        viaje * viaje * (3f - 2f * viaje)) - Main.screenPosition;
                    float escMini = 0.42f * (0.6f + 0.4f * viaje);
                    Main.spriteBatch.Draw(tex, posMini, frame,
                        new Color(232, 222, 250) * alfa,
                        -0.25f + 0.5f * viaje, origen, escMini,
                        SpriteEffects.None, 0f);
                }

                return false;
            }
            catch { return false; }
        }
    }
}
