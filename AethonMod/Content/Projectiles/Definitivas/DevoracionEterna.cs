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
    //  DevoracionEterna — v6.117 — EL GRIMORIO DEVORA AL PORTADOR.
    //
    //  LA LETRA DEL USUARIO: «al usar Mundo Interior del Eterno se hace
    //  una animacion donde el grimorio del eterno devora al jugador de
    //  la misma forma que devora a los jefes, pero sin hacerle daño, en
    //  el momento en que es devorado el mundo se llena de bruma solo en
    //  la animacion y el jugador es transportado el nuevo mundo».
    //
    //  v6.117 — LA CURA DE LA ANIMACIÓN (la letra: «arregla la
    //  animacion del grimorio devorando al jugador»). LA AUTOPSIA del
    //  client.log: el velo de la v6.115 moría al PRIMER FRAME
    //  (Begin-sobre-lote-abierto → la sonda mataba la función para
    //  TODA la sesión) — la inundación NUNCA existió y el trago se veía
    //  roto. Y el libro TAPABA SU PROPIA BOCA: el sprite entero (36×49
    //  opaco) se dibujaba ENCIMA del disco del vacío — las «fauces»
    //  quedaban escondidas detrás de la portada. LA CURA TRIPLE:
    //   · EL LIBRO SE PARTE EN DOS MITADES (la tapa y la contratapa
    //     giran sobre el lomo como mandíbulas) — el vacío queda
    //     EXPUESTO entre ellas y el trago por fin SE LEE;
    //   · EL PORTADOR GIRA (Player.fullRotation de vanilla) mientras
    //     sube en espiral hacia la boca — la presa cae vencida, no
    //     teletransportada;
    //   · AL ENTRAR, LA PRESA DESAPARECE (Tragado → hideEntirePlayer
    //     por ModifyDrawInfo): el estallido de páginas y el portazo de
    //     las tapas lo tragan entero — nada flota sobre el libro
    //     cerrado.
    //
    //  LA COREOGRAFÍA DEL TRAGO (cuatro actos, ~2,5 s):
    //   · EL DESCENSO (0-35): el grimorio MATERIALIZA sobre el portador
    //     (el aura violeta, el corazón dorado y el anillo de seis runas
    //     — la misma vestimenta de LA LLAMADA) mientras el polvo de oro
    //     del suelo se alza.
    //   · LA DEVORACIÓN (35-115): EL LIBRO SE ABRE EN FAUCES — las dos
    //     tapas giran sobre el lomo y EL DISCO DEL VACÍO crece entre
    //     ellas con su aro dorado. El portador SUBE EN ESPIRAL GIRANDO
    //     (fullRotation acelera con la cercanía), la espiral de páginas
    //     y esencias lo envuelve, el temblor crece y el libro se
    //     inclina a recibirlo. SIN DAÑO: el trago lleva al interior.
    //   · EL TRAGO (115-132): el portador ENTRA — desaparece dentro de
    //     la boca (oculto por completo), el ESTALLIDO de 36 páginas, el
    //     rugido, las tapas SE CIERRAN DE UN GOLPE… y EL MUNDO SE LLENA
    //     DE BRUMA (SubmundoGrimorio.BrumaFaro — la inundación violeta
    //     que ahora vive en OnPostDraw y tapa TAMBIÉN la carga).
    //   · LA PUERTA (132): detrás de la bruma llena nace el interior —
    //     y su REVELACIÓN de niebla toma el relevo del faro.
    // ======================================================================
    public class DevoracionEterna : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LOS TIEMPOS DE LA CASA ===
        private const int TAbre = 35;        // el libro se abre
        private const int TTrago = 115;      // el portador entra
        private const int TEntrar = 132;     // la puerta gira (detrás de la bruma)
        private const int TMuere = 165;      // la disolución

        /// <summary>
        /// v6.117 — ¿El portador ya está DENTRO de la boca? (la casa lo
        /// consulta en ShardPlayer.ModifyDrawInfo para ocultarlo del
        /// dibujado: la presa tragada no flota sobre el libro cerrado).
        /// </summary>
        public static bool Tragado;

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
                    // LA SUBIDA ESPIRAL: cada vez más decidida hacia las
                    // fauces, con un vaivén lateral que se apaga al
                    // llegar (la presa cae en espiral, no en línea recta).
                    float f = MathHelper.Clamp((Projectile.ai[0] - TAbre) / (float)(TTrago - TAbre), 0f, 1f);
                    float k = 0.045f + 0.075f * f * f;
                    Vector2 vaivén = new Vector2(
                        MathF.Sin(Projectile.ai[0] * 0.22f) * 34f * (1f - f * 0.7f), 0f);
                    try
                    {
                        p.Center = Vector2.Lerp(p.Center, libro + vaivén + new Vector2(0f, 8f), k);
                        p.velocity = Vector2.Zero;
                        p.gravDir = 1f;
                        p.maxFallSpeed = 0f;

                        // v6.117 — EL GIRO DE LA PRESA (Player.fullRotation
                        // de vanilla): la rotación nace suave y ACELERA con
                        // la cercanía a la boca — el portador cae vencido
                        // en las fauces, no teletransportado.
                        p.fullRotationOrigin = p.Center - p.position; // el eje: el centro del cuerpo
                        p.fullRotation += (0.055f + 0.13f * f * f) * (f > 0.55f ? 1.6f : 1f);
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

            // === EL TRAGO: el estallido de páginas + el rugido + LA
            //     PRESA DESAPARECE DENTRO DE LA BOCA ===
            if (Projectile.ai[0] >= TTrago && !_estallo && !Main.dedServ)
            {
                _estallo = true;
                // v6.117 — LA PRESA YA ESTÁ DENTRO: se oculta por
                // completo (ShardPlayer.ModifyDrawInfo →
                // hideEntirePlayer) y el giro muere con ella.
                if (dueñoVivo && p.whoAmI == Main.myPlayer)
                {
                    try
                    {
                        p.Center = libro;          // el último bocado: al centro de la boca
                        p.velocity = Vector2.Zero;
                        p.fullRotation = 0f;
                    }
                    catch { }
                    Tragado = true;
                }
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

        /// <summary>
        /// v6.117 — LA RED DE SEGURIDAD DEL TRAGO: si el proyectil muere
        /// SIN entregar la puerta (otro mod lo mató, el mundo se cayó,
        /// lo que sea), la inundación y la presa-oculta se liberan —
        /// nadie queda invisible ni atrapado en la bruma.
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (!_entregado)
            {
                Tragado = false;
                SubmundoGrimorio.BrumaFaro = 0f;
                try
                {
                    if (Main.myPlayer >= 0 && Main.player[Main.myPlayer] != null)
                        Main.player[Main.myPlayer].fullRotation = 0f;
                }
                catch { }
            }
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

                // v6.117 — LA APERTURA DE LAS FAUCES: la separación de
                // las tapas (0 = libro cerrado, 1 = boca abierta al
                // máximo). Se abre con la devoración y SE CIERRA DE UN
                // GOLPE al tragar (el mismo cerro del portazo).
                float abre = mawF * (1f - cerro * cerro);

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

                // ============================================================
                //  v6.117 — LAS FAUCES DE VERDAD: EL VACÍO PRIMERO (el
                //  disco y su aro dorado se dibujan DETRÁS de las tapas
                //  — en la v6.115 el libro entero los TAPABA y la boca
                //  no existía a la vista), y LUEGO las dos mitades del
                //  libro girando sobre el lomo como mandíbulas.
                // ============================================================
                if (abre > 0.01f)
                {
                    float fauces = abre;
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

                // === EL LIBRO EN DOS MITADES (36×49 ×3.2 — el mismo de
                //     LA LLAMADA): la TAPA (filas 0-23) y la
                //     CONTRATAPA (filas 24-48) giran sobre EL LOMO (el
                //     centro del libro) como mandíbulas — cerradas son
                //     el libro de siempre, abiertas dejan ver el vacío.
                // ===
                float esc = 3.2f * (0.55f + 0.45f * nace) * (1f + 0.16f * mawF * (1f - cerro));
                float rot = MathF.Sin(Main.GlobalTimeWrappedHourly * 0.9f) * 0.09f
                    + 0.38f * mawF * (1f - cerro); // la frente se rinde a la presa
                // el ángulo de la mandíbula: abre hasta ~53° con un
                // temblor vivo; el portazo lo devuelve a 0.
                float angMand = abre * 0.92f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f) * 0.025f * abre;

                int altoTap = _libro.Height / 2;                       // 24 filas
                Rectangle srcArriba = new Rectangle(0, 0, _libro.Width, altoTap);
                Rectangle srcAbajo = new Rectangle(0, altoTap, _libro.Width, _libro.Height - altoTap);
                // EL LOMO: ambas mitades pivotean sobre el MISMO punto
                // de pantalla — el centro del libro (donde estaban
                // cosidas antes de partirse).
                Vector2 lomo = new Vector2(_libro.Width * 0.5f, altoTap);
                Vector2 origenArriba = lomo;                                   // su borde inferior
                Vector2 origenAbajo = new Vector2(_libro.Width * 0.5f, 0f);    // su borde superior

                // LA TAPA: gira hacia arriba (la mandíbula superior).
                sb.Draw(_libro, libroPantalla, srcArriba,
                    Color.White * nace * vivo,
                    rot - angMand, origenArriba, esc, SpriteEffects.None, 0f);
                // LA CONTRATAPA: gira hacia abajo (la mandíbula inferior).
                sb.Draw(_libro, libroPantalla, srcAbajo,
                    Color.White * nace * vivo,
                    rot + angMand, origenAbajo, esc, SpriteEffects.None, 0f);

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
