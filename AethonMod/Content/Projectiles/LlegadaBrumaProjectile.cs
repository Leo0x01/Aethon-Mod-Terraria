using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

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
                if (vida <= 0.02f) return false;

                // ============================================================
                //  v6.51.1 — LA BRUMA DE VERDAD (la letra: «la bruma se ve
                //  mal, en vez de bruma parecen óvalos casi planos»): la
                //  cura es LA LIBRERÍA DE LA CASA — SombrasLib.Bruma, los
                //  puffs de humo fBm que se DESGARRAN de verdad (los que
                //  visten al Grimorio Dormido y a las fauces). Los óvalos
                //  eran SoftGlow estirados (discos lisos); ahora cada
                //  mechón es una NUBE VIVA con su rampa de enfriamiento y
                //  su aliento violeta — un MAR de niebla de verdad.
                // ============================================================
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    float tiempo = Main.GlobalTimeWrappedHourly;
                    int semilla = Projectile.identity % 97 + 3;
                    Vector2 centro = Projectile.Center;
                    float alfa = 0.35f + 0.65f * crecimiento;

                    // EL CORAZÓN: la nube madre (gruesa, respirando).
                    SombrasLib.Bruma(centro, RADIO * (0.55f + 0.65f * crecimiento),
                        alfa, tiempo, semilla);

                    // LOS BRAZOS: cinco nubes satélite en corro alrededor
                    // (cada una SU tamaño y SU deriva — el mar no es un
                    // círculo: es una MULTITUD).
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = i / 5f * MathHelper.TwoPi + tiempo * 0.11f
                            + semilla * 0.13f;
                        float lejos = (0.55f + 0.35f * ((i * 37 + semilla * 11) % 10) / 10f)
                            * RADIO * crecimiento;
                        Vector2 pos = centro + new Vector2(
                            MathF.Cos(ang) * lejos,
                            MathF.Sin(ang) * lejos * 0.66f);
                        SombrasLib.Bruma(pos,
                            RADIO * (0.38f + 0.30f * ((i * 53) % 7) / 7f) * crecimiento,
                            alfa * 0.85f, tiempo * 0.9f, semilla + i * 7 + 11);
                    }

                    // LA CÚPULA: dos nubes ALTAS (el techo del evento — la
                    // bruma que SUBE, porque lo que llega no viene del
                    // suelo).
                    for (int i = 0; i < 2; i++)
                    {
                        SombrasLib.Bruma(
                            centro + new Vector2((i == 0 ? -1f : 1f) * RADIO * 0.45f * crecimiento,
                                -RADIO * 0.40f * crecimiento),
                            RADIO * 0.5f * crecimiento, alfa * 0.7f,
                            tiempo * 1.1f, semilla + 31 + i * 13);
                    }

                    // EL VACÍO CENTRAL (lo que no deja ver): un corazón
                    // oscuro con rim violeta — sutil, la niebla manda.
                    // v6.51.2 — LA CURA DEL DRAW DESNUDO (la excepción del
                    // client.log: «Draw was called, but Begin has not yet
                    // been called»): después de SombrasLib.Bruma el lote
                    // de vanilla está CERRADO — estos Draws viven ahora
                    // en SU PROPIO lote (abrir → dibujar → cerrar,
                    // blindado try/finally).
                    // v6.51.3 — LA MUERTE DEL ÓVALO (la letra: «al aparecer
                    // todavia aparecen ovalados junto a la bruma» — la .51.1
                    // curó los PUFFS pero el corazón seguía siendo un
                    // SoftGlow ESTIRADO ×1,25/×0,92 = un óvalo liso y
                    // plano): el corazón ahora es una CEBOLLA REDONDA —
                    // TRES capas concéntricas isótropas (grandes, medianas,
                    // pequeñas) con radios DESIGUALES (±7%) y un rim que
                    // RESPIRA: se lee como un GLOBO de oscuridad con
                    // profundidad, jamás como una elipse dibujada.
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    if (glow != null)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        try
                        {
                            Vector2 posScr = centro - Main.screenPosition;
                            // LAS TRES CAPAS DE LA CEBOLLA (cada una su
                            // disimetría fija por semilla — un globo vivo,
                            // no un círculo perfecto).
                            float respiracion = 0.94f + 0.06f * MathF.Sin(
                                Main.GlobalTimeWrappedHourly * 0.9f + semilla);
                            for (int capa = 0; capa < 3; capa++)
                            {
                                float f = 1f - capa * 0.26f;                     // 1.0 · 0.74 · 0.48
                                float asimetria = 0.93f + 0.07f *
                                    (((semilla * 7 + capa * 13) % 10) / 10f);   // su disimetría
                                float r = RADIO * (1.18f * f) * crecimiento * respiracion;
                                Main.spriteBatch.Draw(glow, posScr, null,
                                    new Color(16, 7, 28) * (0.62f * f * crecimiento),
                                    0f, glow.Size() * 0.5f,
                                    new Vector2(r * asimetria, r * (2f - asimetria)) / glow.Size(),
                                    SpriteEffects.None, 0f);
                            }
                            // EL RIM VIOLETA (el borde del globo — un aro
                            // tenue que también respira).
                            float rRim = RADIO * 1.02f * crecimiento * respiracion;
                            Main.spriteBatch.Draw(glow, posScr, null,
                                new Color(70, 38, 120) * (0.16f * vida),
                                0f, glow.Size() * 0.5f,
                                new Vector2(rRim, rRim) / glow.Size(),
                                SpriteEffects.None, 0f);
                        }
                        finally { Main.spriteBatch.End(); }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();

                return false;
            }
            catch { return false; }
        }
    }
}
