using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  LA LLEGADA (v6.51.0) — EL NUEVO NACIMIENTO DEL GRIMORIO (la letra:
    //  «creo que lo mejor es que el grimorio simplemente caiga sin dañar
    //  el mapa, o sea no hay cráteres ni caída, será algo como un
    //  aparecimiento, en vez de la caída será la llegada, y consiste en
    //  mucha bruma hasta el punto de que no se pueda ver como el grimorio
    //  aparece y luego la bruma se disipa dejando al grimorio dormido
    //  flotando a la espera del jugador, el jugador toma al grimorio y el
    //  lugar donde estaba queda limpio sin bruma sin rastro de cómo
    //  llegó»).
    //
    //  EL COMETA Y EL CRÁTER MURIERON: este proyectil es la ESCENA —
    //  nace sobre el punto elegido (32–64 tiles a un lado del
    //  destinatario, la superficie de esa columna), hace CRECER un MAR
    //  DE BRUMA violeta-negra (3 s: nadie ve cómo el libro llega), al
    //  pico ENGENDRA al Grimorio Dormido DENTRO de la niebla (la
    //  autoridad lo registra: la pendiente + su libro) y luego la bruma
    //  SE DISIPA sola (~2,3 s) dejando al dormido flotando con sus Zzz.
    //  NI UNA TILE del mundo se toca — el mapa queda INTACTO (la cura
    //  del cráter de la .117-.130).
    //
    //  El destino viaja en ai[1]/ai[2] (X/1000, Y/1000 — la convención
    //  del cometa) y el destinatario es el owner.
    // ======================================================================
    public class LlegadaBrumaProjectile : ModProjectile
    {
        /// <summary>La BRUMA CRECE durante 3 segundos (nadie ve nada).</summary>
        private const int T_CRECE = 180;

        /// <summary>La bruma se disipa durante ~2,3 s (el dormido asoma).</summary>
        private const int T_DISIPA = 140;

        /// <summary>El pico de la nube (px de radio).</summary>
        private const float RADIO = 170f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;      // la bruma lo atraviesa TODO
            Projectile.ignoreWater = true;
            Projectile.timeLeft = T_CRECE + T_DISIPA + 10;
            Projectile.netImportant = true;      // la escena es de todos los cables
        }

        public override void AI()
        {
            // EL RELOJ DE LA ESCENA (localAI no viaja: cada pantalla la
            // vive a su tiempo — la NATIVIDAD del dormido la decide la
            // AUTORIDAD con su propio reloj, idéntico por construcción).
            Projectile.localAI[0] += 1f;
            float t = Projectile.localAI[0];

            // LA ESCENA NO SE MUEVE: flota sobre su punto (el destino en
            // ai[1]/ai[2] — nació ahí y ahí se queda).
            Projectile.velocity = Vector2.Zero;

            // LA LUZ DE LA BRUMA (violeta tenue: algo está naciendo).
            float pulso = 0.5f + 0.5f * MathF.Sin((float)Main.time * 0.06f);
            float intensidad = t < T_CRECE ? t / T_CRECE : 1f - (t - T_CRECE) / T_DISIPA;
            intensidad = MathHelper.Clamp(intensidad, 0f, 1f);
            Lighting.AddLight(Projectile.Center,
                0.20f * intensidad * (0.8f + 0.2f * pulso),
                0.12f * intensidad,
                0.34f * intensidad);

            // === EL PICO: EL DORMIDO NACE DENTRO DE LA NIEBLA (solo la
            //     AUTORIDAD lo engendra — la escena en cada pantalla es
            //     teatro; el NPC viaja por su propio cable netAlways). ===
            if (t >= T_CRECE && Projectile.ai[0] < 1f)
            {
                Projectile.ai[0] = 1f;   // una sola vez (viaja: todas las
                                          // pantallas saben que ya nació)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 destino = new Vector2(
                        Projectile.ai[1] * 1000f, Projectile.ai[2] * 1000f);
                    Systems.LaCaidaSistema.RegistrarLlegada(destino, Projectile.owner);

                    // EL ALIENTO DEL NACIMIENTO (el sonido del mundo
                    // cambiando de página).
                    if (!Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item123.WithPitchOffset(-0.4f), destino);
                }
            }

            // === LOS POLVOS DE LA BRUMA (la nube respira motas). ===
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) *
                    Main.rand.NextFloat(20f, RADIO * 0.9f),
                    DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                        Main.rand.NextFloat(-1.1f, -0.3f)),
                    140, new Color(110, 55, 170), 0.8f);
                d.noGravity = true;
            }

            // LA DESPEDIDA: la bruma murió y NO QUEDA RASTRO (la letra).
            if (t >= T_CRECE + T_DISIPA)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Projectile.localAI[0];
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow == null) return false;

                // LA INTENSIDAD DE LA ESCENA: crece hasta el pico (nadie
                // ve NADA de lo que pasa dentro) y luego se disipa (el
                // dormido asoma entre jirones).
                float vida = t < T_CRECE
                    ? MathHelper.Clamp(t / T_CRECE, 0f, 1f)
                    : MathHelper.Clamp(1f - (t - T_CRECE) / T_DISIPA, 0f, 1f);
                // LA CURVA DEL CRECIMIENTO: primero LENTA (la sospecha),
                // luego VORAZ (el mundo se cierra de golpe).
                float crecimiento = t < T_CRECE
                    ? vida * vida * (3f - 2f * vida)
                    : 1f;
                float escala = 0.25f + 0.75f * crecimiento;
                float alfa = 0.55f + 0.45f * crecimiento;
                if (t > T_CRECE) alfa = vida;

                Vector2 centro = Projectile.Center - Main.screenPosition;
                float tiempo = (float)Main.time * 0.02f;
                int semilla = Projectile.identity % 97;

                // === EL CORAZÓN OSCURO (lo que no deja ver) ===
                Color tinta = new Color(18, 8, 32) * (0.92f * alfa);
                Main.spriteBatch.Draw(glow, centro, null, tinta, 0f,
                    glow.Size() * 0.5f,
                    new Vector2(RADIO * 1.5f, RADIO * 1.1f) * escala / glow.Size(),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, centro, null, tinta * 0.9f, 0f,
                    glow.Size() * 0.5f,
                    new Vector2(RADIO * 1.05f, RADIO * 0.85f) * escala / glow.Size(),
                    SpriteEffects.None, 0f);

                // === LOS JIRONES (la nube viva: 14 mechones que respiran
                //     y derivan — cada uno SU tamaño y SU compás) ===
                for (int i = 0; i < 14; i++)
                {
                    float f = (i + 1) / 15f;
                    float ang = f * MathHelper.TwoPi + tiempo * (0.4f + 0.1f * (i % 3))
                        + semilla;
                    float radio = (0.45f + 0.55f * ((i * 37 + semilla * 11) % 10) / 10f)
                        * RADIO * escala;
                    float respira = 1f + 0.14f * MathF.Sin(tiempo * 2.1f + i * 2.4f);
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang) * radio,
                        MathF.Sin(ang) * radio * 0.62f
                        + MathF.Sin(tiempo * 1.3f + i) * 9f);
                    float tam = (0.55f + 0.5f * ((i * 53) % 7) / 7f) * RADIO * escala * respira;

                    Color jiron = new Color(26, 12, 44) * (0.85f * alfa *
                        (0.75f + 0.25f * MathF.Sin(tiempo * 1.7f + i * 1.9f)));
                    Main.spriteBatch.Draw(glow, pos, null, jiron, ang,
                        glow.Size() * 0.5f,
                        new Vector2(tam, tam * 0.8f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // === EL RIM VIOLETA (la firma de la casa: el negro puro
                //     no existe sin su orla — la membrana del evento) ===
                Color rim = new Color(96, 52, 150) * (0.30f * alfa *
                    (0.8f + 0.2f * MathF.Sin(tiempo * 3f)));
                Main.spriteBatch.Draw(glow, centro, null, rim, 0f,
                    glow.Size() * 0.5f,
                    new Vector2(RADIO * 2.0f, RADIO * 1.5f) * escala / glow.Size(),
                    SpriteEffects.None, 0f);

                // === EL FULGOR INTERNO (algo se está ESCRIBIENDO dentro) ===
                float nucleo = 0.10f + 0.08f * MathF.Sin(tiempo * 4.2f);
                Color fulgor = new Color(150, 90, 230) * (nucleo * alfa * crecimiento);
                Main.spriteBatch.Draw(glow, centro, null, fulgor, 0f,
                    glow.Size() * 0.5f,
                    new Vector2(RADIO * 0.6f, RADIO * 0.5f) * escala / glow.Size(),
                    SpriteEffects.None, 0f);

                return false;
            }
            catch { return false; }
        }
    }
}
