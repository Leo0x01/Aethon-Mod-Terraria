using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Globals;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  EstandarteJuicioAuraProjectile — v6.98 → v6.100 → v6.101 — EL
    //  ESTANDARTE 1:1 AL ORIGINAL (LA COPIA FIEL DEL EFECTO).
    //
    //  LA LETRA DEL USUARIO: «lo que tienes que hacer es copiarlo 1 a 1,
    //  ya que lo mas importante es el efecto: un radio de accion
    //  pequeño, un aura que quema a los enemigos y los vuelve mas lento
    //  cuanto mas se acercan al jugador, cada enemigo que entre al radio
    //  de efecto del estandarte se quema y visualmente se nota como
    //  cada vez se quema mas […] esto sera una investigacion del efecto
    //  buff y debuff que tendra el grimorio en un futuro».
    //
    //  LA v6.100 NO SE PARECÍA EN NADA AL EFECTO (bendición dorada,
    //  cargas, estallido, radio 720 — TODO MUERE HOY). LA COPIA FIEL:
    //
    //   · EL RADIO PEQUEÑO: 300 px (la v6.100 abarcaba 720 — el doble y
    //     medio; el original es un aura ÍNTIMA, no un estadio).
    //   · LA QUEMADURA: cada impío que entra al radio recibe LA
    //     QUEMADURA DEL VACÍO EXTERIOR (QuemaduraVacioExterior — el
    //     debuff de llamas negras que investiga el grimorio), con el
    //     timer 180 del original (sigue ardiendo ya fuera).
    //   · LA INTENSIDAD ×1→×3 (la técnica Remap del original): más
    //     cerca del portador = más caliente el fuego; y LA RAMPA DEL
    //     TIEMPO: cada segundo ardiendo lo enciende más (hasta ×1.5).
    //   · LA LENTITUD: cuanto más se ACERCAN, más LENTO caminan (×1 en
    //     el borde → ×0.55 pegado) — vive en VacioGlobalNPC.PostAI
    //     (pisa DESPUÉS de la IA: frena a caminantes y voladores).
    //   · EL PAGO: mientras un impío arde, el PORTADOR cobra el buff
    //     VACÍO EXTERIOR (+10% ataque, +10 defensa, −15% maná) — lo
    //     refresca la propia quemadura cada tick que el fuego vive.
    //   · LA CAPA DE RESISTENCIA (la firma visual del meteoro entrando
    //     a la atmósfera: manto de partículas + cresta de choque +
    //     estelas de ablación, morado y negro) vive en
    //     VacioGlobalNPC.PostDraw — ES del debuff, no del arma: el
    //     grimorio la heredará entera.
    //
    //  EL VESTIDO DEL ANCLA (lo que SÍ es del estandarte): el Corazon-
    //  Corona del motor de píxeles latiendo bajo el portador EN VIOLETA
    //  Y NEGRO, el ANILLO DEL VACÍO marcando el radio pequeño en el
    //  suelo, el PULSO que respira hacia fuera y las LLAMAS NEGRAS que
    //  lamen al portador (el mismo fuego que paga su poder).
    // ======================================================================
    public class EstandarteJuicioAuraProjectile : ModProjectile
    {
        /// <summary>EL RADIO DEL VACÍO (px — 18-19 tiles: un aura
        /// ÍNTIMA, la letra del usuario: «radio de accion pequeño»).
        /// Público: VacioGlobalNPC lo lee para la lentitud local.</summary>
        public const float RADIO = 300f;

        /// <summary>La distancia ÍNTIMA: a 40 px del impío, el fuego
        /// pleno (la técnica Remap del original: radio→íntima = 0→1).</summary>
        private const float DIST_INTIMA = 40f;

        /// <summary>Ticks que el impío sigue ARDIENDO ya fuera del radio
        /// (el timer 180 del original del estandarte).</summary>
        private const int ARDE_FUERA = 180;

        /// <summary>La intensidad máxima por cercanía (×3 pegado — el
        /// ardor del original).</summary>
        private const float INTENSIDAD_CERCA = 3f;

        // EL MOTOR DE PÍXELES (el CorazonCorona violeta bajo los pies).
        private PixelFxPlayer _motor;

        // El caché del ruido procedural (las 20 láminas de la casa).
        private static Texture2D[] _ruidoCache;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead ||
                !dueno.GetModPlayer<JuicioPlayer>().EstandarteJuicio)
            {
                Projectile.Kill();
                return;
            }

            // EL ANCLA sigue al portador y no muere mientras él viva.
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;

            // EL MOTOR DE PÍXELES late (solo cliente).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Motor().Tick();
                if (Projectile.timeLeft <= 12)
                    Motor().Fx.Stop();
            }

            // ------------------------------------------------------------
            //  LA COPIA FIEL — el juicio del vacío (solo el dueño manda:
            //  la quemadura viaja dentro del propio debuff).
            // ------------------------------------------------------------
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                int nivelDespertar = dueno.GetModPlayer<ShardPlayer>().NivelDespertar;
                int danoBase = 20 + 6 * nivelDespertar;   // HP/s a intensidad ×1

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                        continue;

                    VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();

                    // FUERA DEL RADIO: si arde por NOSOTROS, el fuego se
                    // enfría poco a poco (el debuff muere solo a los 180).
                    Vector2 borde = Vector2.Clamp(Projectile.Center, n.TopLeft, n.BottomRight);
                    float dist = Vector2.Distance(Projectile.Center, borde);
                    float perdon = Math.Max(n.width, n.height) * 0.5f + 40f;
                    if (dist > RADIO + perdon)
                    {
                        if (vacio.DuenoVacio == dueno.whoAmI + 1 && vacio.IntensidadQuemadura > 0f)
                            vacio.IntensidadQuemadura *= 0.995f;
                        continue;
                    }

                    // DENTRO — LA MARCA: la Quemadura del Vacío Exterior
                    // (se refresca mientras permanezca; fuera, sus últimos
                    // 180 ticks de fuego — el timer del original).
                    n.AddBuff(ModContent.BuffType<QuemaduraVacioExterior>(), ARDE_FUERA, false);

                    // LA CUENTA DEL DUEÑO (quién cobra el buff del vacío).
                    vacio.DuenoVacio = dueno.whoAmI + 1;
                    vacio.DanoVacio = danoBase;

                    // LA INTENSIDAD — la técnica Remap del original:
                    // ×1 en el borde → ×3 PEGADO, más LA RAMPA DEL TIEMPO
                    // (cada segundo ardiendo lo enciende, hasta ×1.5).
                    float cercania = Remap(dist, RADIO + perdon, DIST_INTIMA, 0f, 1f);
                    float rampaTiempo = 1f + 0.5f * Math.Min(1f, vacio.VisualQuemadura);
                    vacio.IntensidadQuemadura = (1f + (INTENSIDAD_CERCA - 1f) * cercania)
                        * rampaTiempo;
                }
            }

            // ------------------------------------------------------------
            //  EL ALIENTO (solo cliente): la luz violeta del vacío y las
            //  brasas negras que suben alrededor del portador.
            // ------------------------------------------------------------
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.58f, 0.34f, 0.95f) * 1.1f);
            }
        }

        /// <summary>El Remap acotado del original del estandarte.</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }

        public override void OnKill(int timeLeft)
        {
            _motor?.Dispose();
            _motor = null;
        }

        /// <summary>EL MOTOR: el CorazonCorona del motor de píxeles —
        /// el corazón VIOLETA del vacío latiendo bajo el portador (la
        /// misma firma dorada de la v6.100, re-tintida con la energía
        /// morada y negra de la investigación).</summary>
        private PixelFxPlayer Motor()
        {
            if (_motor == null)
                _motor = new PixelFxPlayer(
                    PixelFxCasas.CorazonCorona(978,
                        PxColor.Rgb(224, 176, 255), PxColor.Rgb(88, 40, 150)),
                    1.6f);
            return _motor;
        }

        /// <summary>La lámina de ruido viva (el caché de las 20).</summary>
        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int i = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (i < 0) i += 20;
            return _ruidoCache[i] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + i,
                AssetRequestMode.ImmediateLoad).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta,
                AssetRequestMode.ImmediateLoad).Value;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu)
                return false;
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active)
                return false;
            if (dueno.GetModPlayer<JuicioPlayer>().EstandarteJuicioOculto)
                return false;     // el portador pidió silencio visual

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                SpriteBatch sb = Main.spriteBatch;
                float t = Main.GlobalTimeWrappedHourly;

                Vector2 centro = Projectile.Center - Main.screenPosition;
                Vector2 suelo = centro + new Vector2(0f, 26f);
                Color violeta = new(185, 110, 255);
                Color violetaAlto = new(224, 176, 255);
                Color negroVioleta = new(46, 22, 74);
                float pulso = 0.85f + 0.15f * MathF.Sin(t * 2.4f);

                // 1) EL MOTOR DE PÍXELES — el CorazonCorona VIOLETA (lote
                //    ambiente PointClamp, alfa recta del compositor).
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    PixelFxPlayer motor = Motor();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    motor.Draw(sb, dueno.MountedCenter - Main.screenPosition
                        - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
                    sb.End();
                }

                // 2) EL LOTE ADITIVO — el vestido del vacío (morado+negro).
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D glow = Tex("GlowCircleWhite");
                Texture2D anilloMag = Tex("MagicRing");
                Texture2D destello = Tex("SparkleStar");
                Texture2D orbe = Tex("GlowOrb");

                // EL SUELO DEL VACÍO — la mancha oscura bajo el portador
                // (el negro-violeta: donde el aire se espesa) y su ruido.
                Suelo(sb, glow, suelo, negroVioleta * 0.30f, 0f, RADIO * 1.7f, RADIO * 0.62f);
                Suelo(sb, Ruido(0, 2.4f), suelo, violeta * 0.06f,
                    t * 0.15f, RADIO * 1.6f, RADIO * 0.58f);

                // EL ANILLO DEL VACÍO — LA FRONTERA del radio pequeño
                // (300 px) dibujada en el suelo: la llanta violeta viva
                // girando despacio + su gemela interna al revés.
                Suelo(sb, anilloMag, suelo, violeta * (0.30f * pulso),
                    t * 0.35f, RADIO * 2.05f, RADIO * 0.72f);
                Suelo(sb, anilloMag, suelo, violetaAlto * (0.16f * pulso),
                    -t * 0.5f, RADIO * 1.86f, RADIO * 0.66f);

                // EL PULSO QUE RESPIRA — una onda nace en el portador y
                // viaja hasta la frontera, una y otra vez: el vacío INHALA.
                float onda = (t * 42f) % RADIO;
                float apaga = 1f - onda / RADIO;
                Suelo(sb, glow, suelo, violeta * (0.12f * apaga * apaga),
                    0f, (RADIO + onda) * 2f, (RADIO + onda) * 0.7f);

                // LAS LLAMAS DEL PORTADOR — el dueño TAMBIÉN arde: las
                // lenguas negras de violeta suben lamiendo su cuerpo
                // (el mismo fuego que paga su poder — 6, deterministas).
                for (int i = 0; i < 6; i++)
                {
                    float azar = Azar(i);
                    float sube = (t * 52f + azar * 120f) % 120f;
                    Vector2 pos = new(centro.X + (azar * 2f - 1f) * 26f,
                        centro.Y + 18f - sube);
                    float muere = 1f - sube / 120f;
                    Capa(sb, orbe, pos, violeta * (0.20f * muere),
                        0f, (7f + 5f * azar) / 128f);
                    Capa(sb, orbe, pos + new Vector2(0f, -3f),
                        Color.Black * (0.16f * muere), 0f, (5f + 4f * azar) / 128f);
                }

                // LOS TRES DESTELLOS — las chispas del vacío orbitando
                // bajito al portador (la guardia morada del estandarte).
                for (int i = 0; i < 3; i++)
                {
                    float ang = t * (1.5f + Azar(i + 11) * 1.2f) + Azar(i + 4) * MathF.PI * 2f;
                    Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f)
                        * (46f + Azar(i) * 22f);
                    Capa(sb, destello, pos, violetaAlto * (0.25f + 0.45f * Titila(i)),
                        ang, (11f + 8f * Titila(i)) / 32f);
                }

                sb.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El azar determinista de la casa.</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }

        /// <summary>El titilar determinista (0..1, coseno suave).</summary>
        private static float Titila(int i, float vel = 3.1f)
            => 0.5f - 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * vel + Azar(i) * MathF.PI * 2f);

        /// <summary>Una capa aditiva (solo si aporta algo visible).</summary>
        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float esc)
        {
            if (color.A != 0 && esc > 0f)
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f, esc,
                    SpriteEffects.None, 0f);
        }

        /// <summary>Un anillo del suelo (estirado a su rectángulo).</summary>
        private static void Suelo(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float anchoPx, float altoPx)
        {
            if (color.A != 0 && anchoPx > 0f)
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f,
                    new Vector2(anchoPx / tex.Width, altoPx / tex.Height),
                    SpriteEffects.None, 0f);
        }
    }
}
