using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EL RAYO CONVERGENTE (v6.51.2) — LA CEREMONIA DE LOS ATAQUES
    /// COMBINADOS. LA LETRA: «los rayos convergentes no [aparecen] o
    /// tal vez salen demasiado rápido, haz que para los soles rúnicos
    /// el rayo convergente tome 2 segundos en crearlo y lanzarlo al
    /// enemigo y para los agujeros negros rúnicos tome 3 segundos,
    /// esto es una animación que debe realizarse y deben aparecer
    /// encima del jugador».
    ///
    /// LA MÁQUINA: nace ENCIMA DEL JUGADOR (y lo SIGUE — el ritual
    /// camina con su dueño) y en su corona de anillos giratorios
    /// nacen LOS RAYOS DE PERLIN que CONVERGEN al punto focal: 2
    /// SEGUNDOS para el SOL RÚNICO (120 t) y 3 SEGUNDOS para los
    /// AGUJEROS NEGROS (180 t — el supremo también). En el punto
    /// focal la semilla CRECE mientras el mundo entero le SOPA
    /// partículas. Al cumplir el plazo: EL LANZAMIENTO.
    ///
    /// v6.51.5 — LA LETRA DEL RENACIMIENTO (tres curas de una pieza):
    ///   · EL HECHIZO NO SE CANCELA («eso no debe poder cancelarse si
    ///     el nacimiento ya inició, se debe completar, eso es algo así
    ///     como un hechizo»): si el dueño cae a media ceremonia, el
    ///     ritual FLOTA en su último punto y CUMPLE IGUAL; si la presa
    ///     muere, recuerda su ÚLTIMA MIRADA y nace igual hacia ese
    ///     rumbo. Empezó → se completa.
    ///   · EL AGUJERO NACE COMO EL SOL («el sol runico siempre nace en
    ///     el lugar correcto, mira cómo lo lograste»): el sol siempre
    ///     nació EN EL PUNTO FOCAL del ritual con impulso hacia su
    ///     presa — el agujero ahora hace EXACTAMENTE lo mismo (nace en
    ///     el foco con impulso y su propio homing hace el resto). La
    ///     semilla viajera MUERE DEL TODO.
    ///   · LA CONTRACCIÓN (los últimos 45 ticks): la cuna de bruma se
    ///     CIÑE, los rayos se cierran y giran más rápido, el núcleo se
    ///     aviva y el compás ACELERA — el parto se anuncia.
    ///
    /// ai[0] = modo (1 SOL · 2 AGUJERO · 3 SUPREMO) · ai[1] = el
    /// whoAmI del blanco · ai[2] = el daño combinado.
    /// </summary>
    public class RayoConvergenteProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        private const int MODO_SOL = 1;
        private const int MODO_AGUJERO = 2;
        private const int MODO_SUPREMO = 3;

        /// <summary>2 SEGUNDOS para el sol (la letra).</summary>
        private const int T_SOL = 120;

        /// <summary>3 SEGUNDOS para los agujeros (la letra).</summary>
        private const int T_AGUJERO = 180;

        private int Modo => (int)Projectile.ai[0];
        private int Duracion => Modo == MODO_SOL ? T_SOL : T_AGUJERO;
        private bool EsSol => Modo == MODO_SOL;

        // === v6.51.6 — LA ÚLTIMA MIRADA VIVE AQUÍ (no en localAI): el
        //     localAI de vanilla es float[3] (índices 0–2) — el [2..3]
        //     de la .51.5 tiraba IndexOutOfRange en CADA AI y el ritual
        //     NUNCA cumplía (los soles y agujeros rúnicos no nacían:
        //     el client.log lo confesó). Campo de INSTANCIA, el patrón
        //     de la casa (v6.50.108, OjoEternoMinion._faseOrbital): el
        //     ritual corre su AI en la máquina del dueño y ahí basta. ===
        private Vector2 _ultimaMirada = Vector2.Zero;
        private bool _vioMirada = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;      // el ritual no daña: lo que LANZA daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = T_AGUJERO + 30;
            Projectile.netImportant = true;   // la ceremonia es de todos los cables
        }

        public override void AI()
        {
            Projectile.ai[0] = Math.Max(1f, Projectile.ai[0]);
            float t = Projectile.localAI[0];
            Projectile.localAI[0] = t + 1f;
            float edad = Projectile.localAI[0];

            // === v6.51.5 — EL HECHIZO NO SE CANCELA (la letra: «eso no
            //     debe poder cancelarse si el nacimiento ya inició, se
            //     debe completar, es algo así como un hechizo»): el
            //     ritual YA NO MUERE con su dueño — si el portador cae
            //     a media ceremonia, el punto focal QUEDA FLOTANDO en
            //     su último sitio y la ceremonia CUMPLE IGUAL. ===
            Player dueno = Main.player[Projectile.owner];
            bool duenoVivo = dueno != null && dueno.active && !dueno.dead;
            if (duenoVivo)
            {
                Vector2 objetivo = dueno.Center + new Vector2(0f, -150f);
                Projectile.Center = Vector2.Lerp(Projectile.Center, objetivo, 0.18f);
            }
            // (dueño caído: el ritual flota en su último punto — se completa)
            Projectile.velocity = Vector2.Zero;

            // === EL BLANCO (la presa que los ojos eligieron). ===
            NPC blanco = BlancoVivo();

            // === v6.51.5 — LA ÚLTIMA MIRADA: el ritual recuerda hacia
            //     dónde miraba (v6.51.6: campos de instancia — localAI
            //     es float[3] y el [3] tiraba IndexOutOfRange en cada
            //     AI, matando el ritual entero). Si la presa muere a
            //     media ceremonia, el hechizo NACE IGUAL hacia ese
            //     rumbo — nada cancela lo empezado. ===
            if (blanco != null)
            {
                _ultimaMirada = blanco.Center;
                _vioMirada = true;
            }
            else if (!_vioMirada && duenoVivo)
            {
                // nunca vio presa: la mirada inicial es hacia donde mira el dueño
                _ultimaMirada = dueno.Center + new Vector2(dueno.direction * 260f, -40f);
                _vioMirada = true;
            }

            // === LA LUZ DEL RITUAL: el sol DORA el aire; el agujero lo
            //     APAGA con su violeta. ===
            float prog = MathHelper.Clamp(edad / Duracion, 0f, 1f);
            if (EsSol)
                Lighting.AddLight(Projectile.Center,
                    0.55f * prog, 0.42f * prog, 0.14f * prog);
            else
                Lighting.AddLight(Projectile.Center,
                    0.22f * prog, 0.10f * prog, 0.40f * prog);

            // === EL MUNDO LE SOPA PARTÍCULAS (la carga convergente). ===
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejos = Main.rand.NextFloat(90f, 260f);
                Vector2 posM = Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * lejos;
                Vector2 hacia = Projectile.Center - posM;
                Dust d = Dust.NewDustPerfect(posM,
                    EsSol ? DustID.GoldFlame : DustID.Shadowflame,
                    hacia.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.5f, 6f),
                    140, EsSol ? new Color(255, 214, 120) : new Color(120, 60, 190), 0.9f);
                d.noGravity = true;
            }

            // === v6.63 — EL COMPÁS RÚNICO (la letra: «todavía sigue
            //     apareciendo el original de terraria»): el tic-tac de
            //     Item122 — EL SONIDO DE VANILLA que sonaba TODO el
            //     parto, el que la letra manda matar — MUERE. Lo
            //     reemplaza LA VOZ PROPIA: RunaCompas, el pulso
            //     sintetizado de la casa (Content/Sounds), con el MISMO
            //     compás de siempre (cada 20 ticks, y en la contracción
            //     final la CUENTA ATRÁS que aprieta) y el pitch SUBIENDO
            //     con el ritual — ReplaceOldest para que la metralleta
            //     final siempre suene. ===
            if (!Main.dedServ && edad < Duracion)
            {
                float restante = Duracion - edad;
                bool late = restante > 45f
                    ? edad % 20f == 0f
                    : restante == 45f || restante == 34f || restante == 25f ||
                      restante == 18f || restante == 13f || restante == 9f ||
                      restante == 6f || restante == 4f || restante == 2f;
                if (late)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            new Terraria.Audio.SoundStyle(
                                "AethonMod/Content/Sounds/RunaCompas",
                                Terraria.Audio.SoundType.Sound)
                            {
                                MaxInstances = 3,
                                SoundLimitBehavior = Terraria.Audio.SoundLimitBehavior.ReplaceOldest,
                                Volume = 0.60f,
                                Pitch = -0.25f + 0.40f * prog +
                                    (restante <= 45f ? 0.25f : 0f)
                            },
                            Projectile.Center);
                    }
                    catch { }
                }
            }

            // === v6.51.3 → v6.51.4 — LA BRUMA DE LA GESTACIÓN (la
            //     letra: «agrega un poco de bruma… con la bruma del
            //     color correspondiente» — y la de la .51.4: «solo se
            //     debe cubrir con bruma, nada geometria plana»): la
            //     niebla de la cuna VIVE EN EL PreDraw (BrumaFX de la
            //     casa — ver abajo); aquí solo queda LA CARGA (el
            //     mundo le sopla partículas convergentes). ===

            // === EL CUMPLIMIENTO: EL LANZAMIENTO. ===
            if (edad >= Duracion)
            {
                Lanzar(blanco, dueno);
                Projectile.Kill();
            }
        }

        /// <summary>El blanco guardado (o el más cercano vivo al
        /// despertar el ritual — la presa murió a media ceremonia).</summary>
        private NPC BlancoVivo()
        {
            int idx = (int)Projectile.ai[1];
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                NPC n = Main.npc[idx];
                if (n != null && n.active && VFXCore.EsObjetivo(n)) return n;
            }
            NPC mejor = null;
            float mejorD = 1400f * 1400f;   // DistanceSQ: el radio al CUADRADO
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>EL RUMBO DEL HECHIZO: hacia la presa viva… y si la
        /// presa murió a media ceremonia, hacia SU ÚLTIMA MIRADA (el
        /// ritual recuerda — un hechizo empezado se cumple siempre).</summary>
        private Vector2 RumboHacia(NPC blanco)
        {
            Vector2 destino = blanco != null && blanco.active
                ? blanco.Center
                : _ultimaMirada;
            if (destino == Vector2.Zero)
                destino = Projectile.Center - Vector2.UnitY * 120f;
            Vector2 rumbo = destino - Projectile.Center;
            if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
            rumbo.Normalize();
            return rumbo;
        }

        /// <summary>EL LANZAMIENTO — lo que la ceremonia estaba
        /// creando sale al mundo: el SOL con sus anillos disparado al
        /// enemigo · el AGUJERO (uno de los rúnicos al azar, o el
        /// SUPREMO/AURORA) nacido EN EL PUNTO FOCAL del ritual —
        /// EXACTAMENTE como el sol (v6.51.5: donde la animación lo
        /// gestó, ahí nace, con impulso hacia la presa; su propio
        /// homing hace el resto).</summary>
        private void Lanzar(NPC blanco, Player dueno)
        {
            // v6.51.2 — SOLO LA MÁQUINA DUEÑA resuelve el lanzamiento (el
            // patrón de la casa): el dueño engendra y NewProjectile
            // sincroniza — las réplicas solo disfrutan el cine.
            if (Projectile.owner != Main.myPlayer) return;
            int dano = Math.Max(1, (int)Projectile.ai[2]);

            // v6.51.5 — NADA SE DISIPA (la letra: un hechizo empezado se
            //     cumple): sin presa viva, el rumbo es la ÚLTIMA MIRADA.
            Vector2 rumbo = RumboHacia(blanco);

            if (EsSol)
            {
                // === EL SOL RÚNICO: sus anillos por despertar, LANZADO
                //     CON FUERZA al enemigo (la velocidad fuerte de
                //     siempre + el homing del sol hace el resto). ===
                int despertar = Content.Systems.DespertarGrimorio.NivelDe(dueno);
                int anillos = Content.Systems.DespertarGrimorio.AnillosDelSol(despertar);
                Projectile sol = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, rumbo * 7.5f,
                    ModContent.ProjectileType<RuneSunProjectile>(),
                    dano, 4f, Projectile.owner, anillos,
                    (Projectile.owner * 31 + Main.rand.Next(9973)) * 1f);
                sol.timeLeft = 480;
                // v6.63 — LA VOZ DEL NACIMIENTO (la letra: «mejorar los
                //     sonidos de cuando aparecen los agujeros negros
                //     runicos y los soles runicos»): el Item122 de
                //     vanilla MUERE en el momento en que el sol APARECE
                //     — RunaNaceSol, la campana del encendido
                //     (sintetizada por la casa), suena AQUÍ: en el
                //     NACIMIENTO, donde la letra la pide (la v6.62 la
                //     ponía al ABRIR el ritual: se oía fuera de lugar y
                //     el nacimiento quedaba con la voz de vanilla).
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        new Terraria.Audio.SoundStyle(
                            "AethonMod/Content/Sounds/RunaNaceSol",
                            Terraria.Audio.SoundType.Sound)
                        { MaxInstances = 2, Volume = 1f },
                        Projectile.Center);
                }
                catch { }

                // (LA BRUMA DORADA DEL NACIMIENTO la exhila EL PROPIO SOL en
                //  su primer tick de vida — con su intensidad por anillos y
                //  la corona de expulsión de la .51.5; este spawn era un
                //  DOBLE de la .51.3 y murió.)
            }
            else
            {
                // === v6.51.5 — EL AGUJERO NACE COMO EL SOL (la letra:
                //     «el sol runico siempre nace en el lugar correcto,
                //     asi que mira como lograste que el sol nasca en el
                //     lugar correcto y has que los agujeros negros
                //     runicos nascan en el lugar correcto»): la SEMILLA
                //     VIAJERA MUERE DEL TODO — el agujero rúnico nace
                //     AQUÍ, EN EL PUNTO FOCAL del ritual (donde la
                //     ceremonia lo gestó y donde la animación lo dijo),
                //     con IMPULSO hacia su presa — y su propio homing
                //     hace el resto, EXACTAMENTE como el sol. Los 10
                //     agujeros rúnicos quedan INTACTOS. ===
                int tipo;
                if (Modo == MODO_SUPREMO)
                {
                    tipo = Main.rand.NextBool()
                        ? ModContent.ProjectileType<SupremoBlackHoleProjectile>()
                        : ModContent.ProjectileType<SupremoAuroraBlackHoleProjectile>();
                }
                else
                {
                    bool ascendidos = Content.Systems.DespertarGrimorio
                        .AgujerosAscendidos(Content.Systems.DespertarGrimorio.NivelDe(dueno));
                    tipo = (Main.rand.Next(4)) switch
                    {
                        0 => ascendidos ? ModContent.ProjectileType<OlvidoAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<OlvidoBlackHoleProjectile>(),
                        1 => ascendidos ? ModContent.ProjectileType<CosmicAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<CosmicBlackHoleProjectile>(),
                        2 => ascendidos ? ModContent.ProjectileType<UmbralAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<UmbralBlackHoleProjectile>(),
                        _ => ascendidos ? ModContent.ProjectileType<BrumaAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<BrumaBlackHoleProjectile>(),
                    };
                }

                // EL AGUJERO NACE EN EL FOCO, con impulso hacia su presa
                // (su propio homing hace el resto — el patrón del sol).
                Projectile agujero = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, rumbo * 13f,
                    tipo, dano, 0f, Projectile.owner);
                agujero.timeLeft = 600;

                // LA BRUMA VIOLETA DE SU NACIMIENTO — en el mismo foco
                // (con la corona de expulsión que revela al recién nacido).
                Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 2f, 1f);
                // v6.63 — LA VOZ DEL NACIMIENTO: RunaNaceVacio — el
                //     pozo que cae (el golpe seco + el descenso + la
                //     inhalación), en el momento en que la puerta SE
                //     ABRE (el Item122 murió aquí también). El SUPREMO
                //     lo dice una octava más abajo: es la puerta más
                //     honda.
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        new Terraria.Audio.SoundStyle(
                            "AethonMod/Content/Sounds/RunaNaceVacio",
                            Terraria.Audio.SoundType.Sound)
                        { MaxInstances = 2, Volume = 1f,
                          Pitch = Modo == MODO_SUPREMO ? -0.25f : 0f },
                        Projectile.Center);
                }
                catch { }
                // v6.51.8 — EL RUGIDO MURIÓ (la letra: «al parecer los 2
                //     agujeros negros runicos supremos hace ruido de
                //     jefes al aparecer, bueno no deben hacer ningun
                //     ruido, deben ser como los otros»): el SoundID.Roar
                //     del nacimiento supremo MUERE — nacen CON EL MISMO
                //     silencio de los otros agujeros (solo el trueno
                //     grave del ritual, como todos).
            }

            // EL ESTALLIDO DEL LANZAMIENTO (la puerta que se abre).
            if (!Main.dedServ)
            {
                for (int i = 0; i < 26; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        EsSol ? DustID.GoldFlame : DustID.PurpleTorch,
                        Main.rand.NextVector2Circular(7f, 7f), 170,
                        EsSol ? new Color(255, 214, 120) : new Color(190, 130, 255), 1.3f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float edad = Projectile.localAI[0];
                float prog = MathHelper.Clamp(edad / Duracion, 0f, 1f);
                // LA CURVA DEL RITUAL: la convergencia ACELERA al final
                // (el mundo se cierra de golpe — la firma de la casa).
                float conver = prog * prog * (3f - 2f * prog);

                // v6.51.5 — LA CONTRACCIÓN (la letra: «el nacimiento del
                //     sol runico en la bruma es como demasiado espontáneo,
                //     necesita una mejor animación de nacimiento»): en los
                //     ÚLTIMOS 45 TICKS el ritual SE CIÑE — la cuna se
                //     cierra, los rayos se aprietan y giran MÁS RÁPIDO y el
                //     núcleo se aviva. El parto se anuncia; no estalla de
                //     golpe nada.
                float contraccion = 1f - MathHelper.Clamp((Duracion - edad) / 45f, 0f, 1f);

                float time = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center;
                int semilla = Projectile.identity % 89 + 7;

                VFXCore.CerrarLoteSiAbierto();

                // ============================================================
                //  v6.51.4 — LA BRUMA DE LA CUNA (la letra: «teniamos ya
                //  una buena libreria de brumas completamente funcional
                //  que no ponen esos circulos planos… solo se debe cubrir
                //  con bruma, nada geometria plana»): alrededor del punto
                //  focal — donde la semilla crece — RESPIRA la niebla del
                //  color de lo que nace (ORO para el sol · VIOLETA para el
                //  agujero), BRUMAFX DE LA CASA (puffs fBm que se
                //  desgarran — jamás discos lisos). Crece con el ritual:
                //  al principio un susurro, al final la cuna entera.
                // ============================================================
                try
                {
                    Vector2 centroScr = centro - Main.screenPosition;
                    // LA CUNA: crece con el ritual… y en LA CONTRACCIÓN SE
                    // CIÑE (se cierra sobre lo que está por nacer).
                    float radioCuna = MathHelper.Lerp(34f, 92f, conver)
                        * (1f - 0.58f * contraccion);
                    Color masaCuna = EsSol
                        ? new Color(98, 68, 24)      // el humo DORADO (la luz la pone el sol)
                        : new Color(48, 22, 78);     // el humo VIOLETA (la bruma del vacío)
                    BrumaFX.BeginMass();
                    BrumaFX.Cloud(centroScr, radioCuna, masaCuna, semilla, time,
                        Math.Max(4, (int)(7 * conver) + 3),
                        alpha: 0.30f + 0.38f * conver + 0.14f * contraccion);
                    if (conver > 0.35f)
                        BrumaFX.Cloud(centroScr + new Vector2(0f, -radioCuna * 0.2f),
                            radioCuna * 0.6f,
                            EsSol ? new Color(122, 90, 36) : new Color(70, 36, 106),
                            semilla + 29, time * 0.85f, 4,
                            alpha: (0.24f + 0.26f * conver + 0.10f * contraccion));
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                VFXCore.Begin();
                try
                {
                    // === LOS RAYOS CONVERGENTES (la letra): nacen en una
                    //     corona de anillos giratorios y CONVERGEN al punto
                    //     focal — arrancan ANCHOS y lejanos, terminan
                    //     FINOS y pegados: la vista es un EMBUDO de luz.
                    //     En LA CONTRACCIÓN se CIERRAN y GIRAN MÁS RÁPIDO. ===
                    int rayos = EsSol ? 5 : 7;
                    float lejania = MathHelper.Lerp(1.0f, 0.22f, conver)
                        * (1f - 0.62f * contraccion);
                    float grosor = MathHelper.Lerp(9f, 3.5f, conver)
                        * (1f + 0.30f * contraccion);
                    float giroRayos = 0.55f * (1f + 1.8f * contraccion);
                    for (int i = 0; i < rayos; i++)
                    {
                        float ang = i / (float)rayos * MathHelper.TwoPi
                            + time * giroRayos * ((i % 2 == 0) ? 1f : -0.7f)
                            + semilla * 0.31f;
                        float radio = (150f + 60f * MathF.Sin(time * 0.9f + i * 1.7f)) * lejania;
                        Vector2 borde = centro + new Vector2(
                            MathF.Cos(ang) * radio, MathF.Sin(ang) * radio * 0.8f);
                        // EL RAYO DE PERLIN: vivo (el temblor de siempre) y
                        // CARGÁNDOSE (más opaco cuanto más cerca del final).
                        float vivo = 0.55f + 0.45f * MathF.Sin(time * 14f + i * 2.3f);
                        Color cRayo = EsSol
                            ? new Color(255, 214, 120)
                            : new Color(168, 96, 240);
                        VFXCore.Line(borde, centro, cRayo * (0.16f + 0.50f * prog) * vivo,
                            grosor * (0.65f + 0.35f * vivo));
                        VFXCore.Line(borde, centro,
                            (EsSol ? new Color(255, 246, 200) : new Color(226, 190, 255))
                            * (0.10f + 0.35f * prog),
                            grosor * 0.35f);
                    }

                    // === EL PUNTO FOCAL: la semilla que CRECE (y en
                    //     la contracción ARDE — el núcleo se aviva). ===
                    float nucleo = MathHelper.Lerp(14f, EsSol ? 46f : 40f, conver)
                        * (1f + 0.22f * contraccion);
                    float avivo = MathHelper.Clamp(prog + 0.55f * contraccion, 0f, 1f);

                    if (EsSol)
                    {
                        // EL SOL EN GESTACIÓN: el corazón dorado (v6.51.4 —
                        //     YA NO son discos gordos: un núcleo de LUZ
                        //     pequeño — la MASA la pone la bruma de arriba)
                        //     + sus ANILLOS rúnicos que giran.
                        VFXCore.Quad(centro, new Color(255, 236, 170) * (0.45f + 0.5f * avivo),
                            new Vector2(nucleo * 1.1f, nucleo * 1.1f));
                        VFXCore.Quad(centro, new Color(255, 170, 60) * (0.3f + 0.4f * avivo),
                            new Vector2(nucleo * 1.7f, nucleo * 1.7f));
                        for (int a = 0; a < 3; a++)
                        {
                            float rA = nucleo * (1.6f + a * 0.55f);
                            float giro = time * (1.1f - a * 0.25f) * (1f + 1.4f * contraccion) * (a % 2 == 0 ? 1f : -1f);
                            int runas = 6 + a * 2;
                            for (int k = 0; k < runas; k++)
                            {
                                float angR = giro + k / (float)runas * MathHelper.TwoPi;
                                Vector2 posR = centro + new Vector2(
                                    MathF.Cos(angR) * rA, MathF.Sin(angR) * rA * 0.82f);
                                VFXCore.Quad(posR, new Color(255, 208, 110) * (0.4f + 0.4f * avivo),
                                    new Vector2(nucleo * 0.16f, nucleo * 0.30f), angR + MathHelper.PiOver2);
                            }
                        }
                    }
                    else
                    {
                        // EL AGUJERO EN GESTACIÓN: el vórtice violeta que
                        // TRAGA — corazón pequeño (la MASA la pone la
                        // bruma de la casa de arriba) + sus runas.
                        VFXCore.Quad(centro, new Color(96, 40, 160) * (0.30f + 0.45f * avivo),
                            new Vector2(nucleo * 1.5f, nucleo * 1.5f));
                        VFXCore.Quad(centro, new Color(30, 10, 52) * (0.35f + 0.55f * avivo),
                            new Vector2(nucleo * 1.0f, nucleo * 1.0f));
                        for (int a = 0; a < 2; a++)
                        {
                            float rA = nucleo * (1.8f + a * 0.9f);
                            float giro = -time * (0.9f + a * 0.4f) * (1f + 1.5f * contraccion);
                            int runas = 8;
                            for (int k = 0; k < runas; k++)
                            {
                                float angR = giro + k / (float)runas * MathHelper.TwoPi;
                                Vector2 posR = centro + new Vector2(
                                    MathF.Cos(angR) * rA, MathF.Sin(angR) * rA * 0.85f);
                                VFXCore.Quad(posR, new Color(190, 120, 255) * (0.35f + 0.4f * avivo),
                                    new Vector2(nucleo * 0.14f, nucleo * 0.26f), angR + MathHelper.PiOver2);
                            }
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();

                // === EL VACÍO QUE OSCURECE (v6.51.4 — los agujeros comen
                //     la luz alrededor del ritual — PERO CON BRUMA DE LA
                //     CASA, la letra: «solo se debe cubrir con bruma, nada
                //     geometria plana»: el disco oscuro liso MUERE; lo que
                //     oscurece es UNA NUBE NEGRA-VIOLETA que se espesa con
                //     el ritual — BrumaFX, puffs que se desgarran). ===
                if (!EsSol)
                {
                    try
                    {
                        Vector2 centroScr = centro - Main.screenPosition;
                        // EL VACÍO TAMBIÉN SE CIÑE en la contracción.
                        float radioVac = MathHelper.Lerp(40f, 150f, conver)
                            * (1f - 0.45f * contraccion);
                        BrumaFX.BeginMass();
                        BrumaFX.Cloud(centroScr, radioVac,
                            SombrasLib.HumoNegro, semilla + 61, time,
                            Math.Max(5, (int)(8 * conver) + 4),
                            alpha: 0.34f + 0.42f * conver + 0.12f * contraccion);
                        BrumaFX.Cloud(centroScr + new Vector2(0f, -radioVac * 0.15f),
                            radioVac * 0.62f, SombrasLib.HumoVioleta,
                            semilla + 83, time * 1.1f, 4,
                            alpha: 0.26f + 0.30f * conver + 0.10f * contraccion);
                    }
                    catch { }
                    finally
                    {
                        try { Main.spriteBatch.End(); } catch { }
                        VFXCore.CerrarLoteSiAbierto();
                    }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }
}
