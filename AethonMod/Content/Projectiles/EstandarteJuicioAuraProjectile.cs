using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  EstandarteJuicioAuraProjectile — v6.98 → v6.100 — EL JUICIO,
    //  FIEL AL ORIGINAL Y SUPERIOR.
    //
    //  LA LETRA DEL USUARIO: «el estandarte del juicio no tiene nada
    //  que ver con el original y se ve peor». LA INVESTIGACIÓN del
    //  original (research/README-ESTANDARTE.md) destapó las constantes
    //  exactas del concepto y las segundas mitades que la v6.98 no
    //  tenía:
    //
    //   · RADIO 720 px (la v6.98 juzgaba a 384 — la mitad).
    //   · LA BENDICIÓN: el impío MÁS CERCANO (clamp al rectángulo del
    //     NPC) alimenta un multiplicador de TODO el daño del portador
    //     — Remap(dist, 720→80, 0→0.30) (la v6.98 no premiaba nada).
    //   · EL ARDOR CON INTENSIDAD: cada impío dentro del radio se
    //     MARCA (timer 180) y su intensidad crece con la cercanía
    //     (Remap ×1→×3, con el hitbox generoso +100 del original).
    //   · EL DESGASTE POR MULTITUD: cada impío marcado de más resta
    //     poder al juicio de TODOS (el original: Remap(objetivos,
    //     7→1, 1→0.10) — la casa lo suaviza a 0.15 por cabeza).
    //   · LAS CARGAS: la novedad DE LA CASA sobre el concepto — cada
    //     impío marcado ACUMULA cargas (hasta 300); a las 300 EL
    //     ESTALLIDO DEL JUICIO (flash + estampa sonora), y la
    //     cadencia del ardor se ACELERA con las cargas (15→2 ticks).
    //   · EL REPEL: a intensidad >2.5 el impío es FRENO (y rechazo al
    //     que se pega) — «repels nearby enemies» del original.
    //
    //  EL VESTIDO (todo de la casa, el motor de capas + el motor de
    //  píxeles): el CorazonCorona del motor de píxeles latiendo bajo
    //  el portador (AlphaBlend PointClamp) + el lote aditivo con los
    //  CINCO halos concéntricos, los CUATRO anillos del suelo (la
    //  llanta del radio completo, el ruido, el anillo mágico y el
    //  anillo de fuego — el suelo del juicio), las brasas que suben,
    //  las ESTRELLAS de los marcados (más cerca = más brillantes — LA
    //  CULPA DELATA), el triple satélite y EL MEDIDOR DE LA BENDICIÓN
    //  (la estrella de 4 puntas sobre el portador crece con ella).
    // ======================================================================
    public class EstandarteJuicioAuraProjectile : ModProjectile
    {
        /// <summary>EL RADIO del juicio (px — 45 tiles, como el original).</summary>
        private const float RADIO = 720f;

        /// <summary>LA BENDICIÓN máxima (+30% de TODO el daño).</summary>
        private const float BENDICION_MAX = 0.3f;

        /// <summary>La distancia ÍNTIMA: a 80 px del impío, bendición plena.</summary>
        const float DIST_INTIMA = 80f;

        /// <summary>Cargas para EL ESTALLIDO.</summary>
        private const int CARGAS_MAX = 300;

        /// <summary>Ticks que el impío sigue ARDIENDO ya fuera del radio.</summary>
        private const int ARDE_FUERA = 180;

        // EL ESTADO por impío (whoAmI): cargas, timer de marca,
        // próximo ardor, intensidad y el estallido consumado.
        private static readonly Dictionary<int, int> _cargas = new();
        private static readonly Dictionary<int, int> _timer = new();
        private static readonly Dictionary<int, int> _proximoArdor = new();
        private static readonly Dictionary<int, float> _intensidad = new();
        private static readonly Dictionary<int, bool> _estallido = new();

        // EL MOTOR DE PÍXELES (el CorazonCorona latiendo bajo los pies).
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
            Projectile.ArmorPenetration = 50;    // el juicio no admite armadura
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
            //  LA CUENTA DE IMPÍOS (dentro del radio con su hitbox
            //  generoso — el original perdona +100 px de cuerpo).
            // ------------------------------------------------------------
            int dentro = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy())
                    continue;
                float perdon = Math.Max(n.width, n.height) * 0.5f + 100f;
                if (MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center)) <= RADIO + perdon)
                    dentro++;
            }

            // ------------------------------------------------------------
            //  LA BENDICIÓN — el impío MÁS CERCANO (clamp al rectángulo,
            //  como el original: los jefes enormes cuentan su borde).
            // ------------------------------------------------------------
            float bendicion = 0f;
            NPC masCerca = null;
            float distMin = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy())
                    continue;
                Vector2 borde = Vector2.Clamp(Projectile.Center, n.TopLeft, n.BottomRight);
                float d = Vector2.Distance(Projectile.Center, borde);
                if (d < distMin)
                {
                    distMin = d;
                    masCerca = n;
                }
            }
            if (masCerca != null)
                bendicion = Remap(distMin, RADIO, DIST_INTIMA, 0f, BENDICION_MAX);
            dueno.GetModPlayer<JuicioPlayer>().BendicionJuicio = bendicion;

            // ------------------------------------------------------------
            //  EL JUICIO (solo el dueño manda — el golpe viaja por GolpeMotor).
            // ------------------------------------------------------------
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                // EL DESGASTE POR MULTITUD: cada impío extra degrada el
                // ardor de TODOS (mínimo ×0.1 — el original castiga así).
                float porCabeza = dentro <= 1 ? 1f : MathF.Max(0.1f, 1f - (dentro - 1) * 0.15f);
                int nivelDespertar = dueno.GetModPlayer<ShardPlayer>().NivelDespertar;
                int danoBase = (int)((40 + 12 * nivelDespertar) * porCabeza);

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                        continue;

                    float perdon = Math.Max(n.width, n.height) * 0.5f + 100f;
                    float dist = MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center));

                    // FUERA DEL RADIO: la marca se apaga (ARDE_FUERA
                    // ticks de gracia, y las cargas se enfrían -10%/s
                    // en el último segundo — como el burn del original).
                    if (dist > RADIO + perdon)
                    {
                        if (!_timer.TryGetValue(n.whoAmI, out int t))
                            continue;
                        if (--t <= 0)
                        {
                            Desmarcar(n.whoAmI);
                            continue;
                        }
                        _timer[n.whoAmI] = t;
                        if (t <= 60 && _cargas.TryGetValue(n.whoAmI, out int c))
                            _cargas[n.whoAmI] = (int)(c * 0.9f);
                        continue;
                    }

                    // LA INTENSIDAD: más cerca = más juicio (×1 → ×3).
                    float intensidad = Remap(dist, RADIO + perdon, perdon, 1f, 3f);
                    _intensidad[n.whoAmI] = intensidad;
                    _timer[n.whoAmI] = ARDE_FUERA;

                    // LAS CARGAS suben — y a las máximas: EL ESTALLIDO.
                    _cargas.TryGetValue(n.whoAmI, out int cargas);
                    cargas = Math.Min(CARGAS_MAX + 1, cargas + 1);
                    _cargas[n.whoAmI] = cargas;
                    if (cargas == CARGAS_MAX && !_estallido.TryGetValue(n.whoAmI, out _))
                    {
                        _estallido[n.whoAmI] = true;
                        Estallido(n);
                    }

                    // EL REPEL: pegado al portador (intensidad >2.5) el
                    // impío es frenado — y el que embiste, RECHAZADO.
                    if (intensidad > 2.5f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float rechazo = (intensidad - 2.5f) / 0.5f;
                        n.velocity *= 1f - 0.25f * rechazo;
                        if (n.velocity.Length() > 5f && intensidad > 2.85f)
                            n.velocity = -n.velocity * 0.7f;
                    }

                    // EL ARDOR: la cadencia se ACELERA con las cargas
                    // (15 → 2 ticks) y con la cercanía.
                    if (!_proximoArdor.TryGetValue(n.whoAmI, out int proximo))
                        proximo = 0;
                    if (proximo <= 0)
                    {
                        Ardor(n, dueno, danoBase, intensidad);
                        float cadencia = Remap(cargas, 0f, CARGAS_MAX, 15f, 2f);
                        _proximoArdor[n.whoAmI] = (int)(cadencia + (3f - intensidad) * 4f);
                    }
                    else
                    {
                        _proximoArdor[n.whoAmI] = proximo - 1;
                    }
                }
            }

            // LA LIMPIEZA periódica de los muertos.
            if (Main.GameUpdateCount % 60uL == 0)
                LimpiarMuertos();

            // ------------------------------------------------------------
            //  EL ALIENTO (solo cliente): la luz dorada del estandarte
            //  y las brasas que suben del suelo del juicio.
            // ------------------------------------------------------------
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.35f) * 1.5f);
                if (Main.rand.NextBool(2) && !Main.gameMenu)
                {
                    Vector2 deriva = Main.rand.NextVector2Circular(10f, 10f);
                    Dust brasa = Dust.NewDustPerfect(Projectile.Center + deriva, 57,
                        deriva * Main.rand.NextFloat(0.1f, 0.4f), 150,
                        Main.rand.NextBool() ? new Color(255, 200, 60) : new Color(255, 140, 30),
                        0.6f);
                    brasa.noGravity = true;
                    brasa.noLightEmittence = true;
                }
            }
        }

        /// <summary>El Remap acotado del original (clamp 0..1).</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }

        /// <summary>EL ARDOR: el golpe del juicio con su Llama del Alba.</summary>
        private void Ardor(NPC n, Player dueno, int danoBase, float intensidad)
        {
            int dano = (int)(danoBase * intensidad);
            GolpeMotor.Golpear(Projectile, n, dano);
            n.AddBuff(ModContent.BuffType<Buffs.JuicioDelAlba>(), 90, false);
        }

        /// <summary>EL ESTALLIDO: el flash y la estampa de las cargas
        /// completas (solo visual — el castigo ya lo puso el ardor
        /// acelerado).</summary>
        private void Estallido(NPC n)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ)
                return;
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustPerfect(n.Center, 57,
                    Main.rand.NextVector2Circular(13f, 13f) * Main.rand.NextFloat(0.4f, 1f),
                    160, Main.rand.NextBool() ? new Color(255, 218, 96) : new Color(255, 160, 32),
                    0.85f).noGravity = true;
            }
            SoundStyle golpe = SoundID.NPCHit53;
            golpe.Pitch = 0.7f;
            golpe.Volume = 0.7f;
            SoundEngine.PlaySound(in golpe, n.Center);
        }

        /// <summary>Borra la marca completa de un impío.</summary>
        private static void Desmarcar(int who)
        {
            _cargas.Remove(who);
            _timer.Remove(who);
            _proximoArdor.Remove(who);
            _intensidad.Remove(who);
            _estallido.Remove(who);
        }

        /// <summary>La barredura de los que ya no viven.</summary>
        private static void LimpiarMuertos()
        {
            List<int> muertos = null;
            foreach (int k in _cargas.Keys)
            {
                NPC n = Main.npc[k];
                if (n == null || !n.active || n.life <= 0)
                    (muertos ??= new List<int>()).Add(k);
            }
            if (muertos == null)
                return;
            foreach (int k in muertos)
                Desmarcar(k);
        }

        public override void OnKill(int timeLeft)
        {
            // EL MOTOR Y LAS MARCAS se apagan con el ancla.
            _motor?.Dispose();
            _motor = null;
            _cargas.Clear();
            _timer.Clear();
            _proximoArdor.Clear();
            _intensidad.Clear();
            _estallido.Clear();
        }

        /// <summary>EL MOTOR: el CorazonCorona del motor de píxeles —
        /// el corazón dorado del juicio latiendo bajo el portador.</summary>
        private PixelFxPlayer Motor()
        {
            if (_motor == null)
                _motor = new PixelFxPlayer(
                    PixelFxCasas.CorazonCorona(977,
                        PxColor.Rgb(255, 208, 96), PxColor.Rgb(200, 120, 30)),
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
                float bendicion = dueno.GetModPlayer<JuicioPlayer>().BendicionJuicio;

                // LA VIDA del juicio: el pulso base + la bendición
                // alimentándolo (más bendición = todo más vivo).
                float vida = 0.3f + bendicion * 1.6f;
                float pulso = 0.85f + 0.15f * MathF.Sin(t * 3.1f);

                Vector2 centro = Projectile.Center - Main.screenPosition;
                Vector2 suelo = centro + new Vector2(0f, 26f);
                Color oro = new(255, 208, 96);
                Color marfil = new(255, 248, 224);

                // 1) EL MOTOR DE PÍXELES — el CorazonCorona (lote
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

                // 2) EL LOTE ADITIVO — el vestido del juicio.
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D glow = Tex("GlowCircleWhite");
                Texture2D anilloMag = Tex("MagicRing");
                Texture2D anilloFuego = Tex("Procedural/FireRing");
                Texture2D estrella = Tex("FlareEstrella4Puntas");
                Texture2D destello = Tex("SparkleStar");
                Texture2D orbe = Tex("GlowOrb");

                // LOS CINCO HALOS concéntricos del portador.
                float late = MathF.Abs(MathF.Sin(t * 0.9f)) * 0.2f + 0.8f;
                for (int i = 0; i < 5; i++)
                    Capa(sb, orbe, centro,
                        Color.Lerp(oro, Color.White, i * 0.15f)
                            * (0.1f * (1f - i * 0.12f) * late),
                        0f, (0.75f - i * 0.15f) * late * 1.6f);

                // LOS CUATRO ANILLOS DEL SUELO — la llanta del radio
                // completo (1512 px = 720×2 + generosidad), el ruido,
                // el anillo mágico y el anillo de fuego respirando.
                Suelo(sb, glow, suelo, oro * (0.07f * vida * pulso), 0f, 1512f, 576f);
                Suelo(sb, Ruido(0, 2.4f), suelo, oro * (0.05f * vida), t * 0.15f, 1440f, 540f);
                Suelo(sb, anilloMag, suelo, marfil * (0.14f * vida), t * 1.2f, 1476f, 540f);
                Suelo(sb, anilloFuego, suelo, oro * (0.09f + 0.07f * vida * pulso),
                    -t * 0.8f, 1548f * (1f + 0.04f * pulso), 612f);

                // LAS BRASAS que suben del anillo (6, deterministas).
                for (int i = 0; i < 6; i++)
                {
                    float azar = Azar(i);
                    float sube = (t * 46f + azar * 110f) % 110f;
                    Vector2 pos = new(suelo.X + (azar * 2f - 1f) * RADIO * 0.8f,
                        suelo.Y - sube);
                    Capa(sb, orbe, pos,
                        oro * (0.22f * (1f - sube / 110f) * vida + 0.04f),
                        0f, 13f / 128f);
                }

                // LAS ESTRELLAS DE LOS MARCADOS — LA CULPA DELATA: los
                // 5 impíos más marcados visten su estrella de 4 puntas
                // y su destello, ESCALADOS por la intensidad.
                int vestidos = 0;
                for (int i = 0; i < Main.maxNPCs && vestidos < 5; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy())
                        continue;
                    if (!_intensidad.TryGetValue(n.whoAmI, out float inten))
                        continue;
                    float culpable = (inten - 1f) / 2f;    // 0 lejos → 1 pegado
                    Vector2 pos = n.Center - Main.screenPosition;
                    float esc = (30f + 44f * culpable) / 256f;
                    Capa(sb, estrella, pos, oro * (0.35f + 0.55f * culpable),
                        t * 2.4f + i, esc);
                    Capa(sb, destello, pos, marfil * (0.3f + 0.45f * culpable),
                        -t * 3f, esc * 0.55f);
                    vestidos++;
                }

                // EL SATÉLITE TRIPLE — los tres destellos orbitando al
                // portador (la guardia del estandarte).
                Capa(sb, glow, suelo - new Vector2(0f, 6f),
                    marfil * (0.14f * vida * pulso), 0f, 1.40625f);
                for (int i = 0; i < 3; i++)
                {
                    float ang = t * (1.5f + Azar(i + 11) * 1.2f) + Azar(i + 4) * MathF.PI * 2f;
                    Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f)
                        * (54f + Azar(i) * 26f);
                    Capa(sb, destello, pos, oro * (0.25f + 0.45f * Titila(i)), ang,
                        (12f + 9f * Titila(i)) / 32f);
                }

                // EL MEDIDOR DE LA BENDICIÓN — la estrella de 4 puntas
                // sobre la cabeza del portador CRECE y BRILLA con la
                // bendición (0 → 0.30): el premio se VE.
                Vector2 arriba = centro - new Vector2(0f, 44f);
                Capa(sb, estrella, arriba,
                    marfil * (0.25f + 0.45f * bendicion / BENDICION_MAX),
                    t * 2.6f, (22f + 26f * bendicion / BENDICION_MAX) / 256f);

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
