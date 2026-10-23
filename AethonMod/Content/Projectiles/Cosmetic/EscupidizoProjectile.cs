using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

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

                // === v6.51.1 — EL MAR DE BRUMAS DE VERDAD (la cura de los
                //     óvalos planos: SombrasLib.Bruma, la librería fBm de
                //     la casa — puffs que se desgarran, no discos lisos).
                //     Se dibuja con SU propio lote (el patrón del Dormido):
                //     cerrar → bruma → reabrir. ===
                float escalaBruma = 0.7f + 0.5f * MathHelper.Clamp(t / (float)T_SUBE, 0f, 1f)
                    - 0.5f * despedida;
                float apertura = MathHelper.Clamp((t - T_SUBE) / (float)(T_ESCUPE - T_SUBE), 0f, 1f);
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    if (escalaBruma > 0.1f && alfa > 0.05f)
                    {
                        Vector2 centroMundo = Projectile.Center + new Vector2(0f, 14f);
                        // LA NUBE MADRE (debajo del libro, respirando).
                        SombrasLib.Bruma(centroMundo, 84f * escalaBruma,
                            alfa * (0.6f + 0.4f * apertura), tiempo, Projectile.identity % 89 + 5);
                        // LOS DOS BRAZOS (a los costados — el mar no es un
                        // punto: es una multitud que lo sostiene todo).
                        SombrasLib.Bruma(centroMundo + new Vector2(-70f * escalaBruma, 6f),
                            58f * escalaBruma, alfa * 0.8f, tiempo * 0.93f,
                            Projectile.identity % 89 + 17);
                        SombrasLib.Bruma(centroMundo + new Vector2(70f * escalaBruma, 6f),
                            58f * escalaBruma, alfa * 0.8f, tiempo * 1.07f,
                            Projectile.identity % 89 + 29);
                        // v6.51.6 — LA MEMBRANA DEL EVENTO (la que era un
                        //     óvalo plano de glow): ahora es bruma fBm de
                        //     la casa — la nube ANCHA que abraza a las tres
                        //     (el mar entero, respirando lento).
                        SombrasLib.Bruma(centroMundo,
                            118f * escalaBruma, alfa * 0.55f, tiempo * 0.81f,
                            Projectile.identity % 89 + 41);
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();

                // === v6.51.6 — EL RIM VIOLETA MUERE (la letra: «recuerda
                //     no usar eso [el ovalado plano] y siempre usar
                //     nuestro sistema de bruma»): el óvalo de glow
                //     estirado 190×130 era geometría plana — la membrana
                //     del evento ahora es UNA CUARTA NUBE de bruma fBm
                //     de la casa (más ancha que las tres de arriba: es
                //     el mar entero respirando). ===
                // (la cuarta nube vive en el lote de bruma de arriba)

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
                    //     v6.51.6 — la boca también era un óvalo plano de
                    //     glow estirado: ahora es UN PUNTO de luz REDONDO
                    //     pequeño (la garganta que se enciende — nada de
                    //     geometría plana) y la bruma creciente la tapa.
                    if (abre > 0.2f)
                        Main.spriteBatch.Draw(glow, pos + new Vector2(0f, 6f), null,
                            new Color(190, 120, 255) * (0.55f * abre * alfa),
                            0f, glow.Size() * 0.5f,
                            new Vector2(22f + 16f * abre, 22f + 16f * abre) / glow.Size(),
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
