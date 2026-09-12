using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — una estrella de plasma viva (10 segundos de vida).
    ///
    /// v5.97 — UNA SOLA EXPLOSIÓN FINAL (petición del usuario: “el sol y el
    /// agujero negro tienen dos, digamos explosiones al terminar, solo deben
    /// tener una donde suceda todo”): el OnKill ya NO suelta 3 ondas de fuego
    /// + 1 onda de lente — suelta UNA SOLA ONDA NOVA DE LENTE (StyleNova):
    /// el frente de espaciotiempo QUE LLEVA EL FUEGO — triple anillo ardiente
    /// (FireRing) + frente fino blanco + aberración CÁLIDA (oro/brasa — sin
    /// RGB) — con el DAÑO DE LA NOVA COMPLETO (antes ×0.5 repartido en 4
    /// ondas) + quemadura 10 s, registrada como fuente del BlackHoleLensSystem
    /// → el fondo se curva a su paso. El AoE del núcleo (260px) golpea el
    /// mismo instante: TODO sucede en UNA explosión.
    ///
    /// v5.96 — GLOW CORONAL PERSISTENTE + AURA DE ÁREA CRECIENTE (dos
    /// peticiones del usuario): (1) "el brillo de PhoenixNovaStaff ya no debe
    /// parpadear — debe comenzar a crecer lentamente, sincronizado con el
    /// ciclo de vida del sol y con el tamaño del mismo": las llamaradas
    /// PhoenixNova periódicas (una nova de 60 frames cada 2 s — cada una un
    /// PARPADEO por diseño) se ELIMINARON; el sol lleva ahora un GLOW CORONAL
    /// CONTINUO (DrawCoronalGlowSprites) que nace con la estrella, crece SIN
    /// OSCILACIÓN (función pura del ciclo de vida) y se dimensiona con starR
    /// → la gigante roja lo ARRASTRA. (2) "todo el daño de ambos proyectiles
    /// deben ser daño de área que se extienda por fuera del proyectil y crezca
    /// conforme crece, se expande y explota": aura de daño cada 0.25 s cuyo
    /// radio (1.35→1.65× starR) crece con el ciclo de vida y con la gigante
    /// (110→205px), con quemadura OnFire que dobla en la fase final.
    ///
    /// v5.95 — SUS EFECTOS VAN DETRÁS DE ÉL (petición del usuario: "sus efectos
    /// SupernovaStaff y PhoenixNovaStaff deben estar detrás de él"): el SOL
    /// dibuja él mismo a sus hijos (carga de la Supernova) y su GLOW CORONAL
    /// ANTES de sus propias capas — detrás del disco SIEMPRE, sin
    /// depender del orden de índices de Main.projectile ni de DrawBehind
    /// (decompilado tML: el bucle principal solo excluye `hide` — sin
    /// hide=true la llamarada se dibujaba DOS VECES, una de ellas ENCIMA del
    /// sol: el "extraño parpadeo").
    ///
    /// v5.95 — LENTE GRAVITACIONAL EN LA GIGANTE ROJA (petición: "dale al sol
    /// un poco de lente gravitacional a medida que vaya creciendo como gigante
    /// roja"): el BlackHoleLensSystem recoge al sol como fuente de distorsión
    /// SUTIL (fuerza = progreso×0.4, en su PROPIO pase débil para no heredar la
    /// fuerza del agujero negro) y lo dibuja ENCIMA de la lente (DrawStarVisuals).
    ///
    /// v5.95 — ONDA DE LENTE EN LA EXPLOSIÓN (petición: "en ambas explosiones
    /// del sol y agujero negro también debe de haber una onda expansiva creada
    /// con lente gravitacional que tenga una ligera distorsión cromática en
    /// rgb"): además de las 3 ondas de fuego, el OnKill lanza una onda StyleLens
    /// SIN retardo (la onda gravitacional VIAJA DELANTE de la materia) con
    /// franjas R/G/B LIGERAS que curvan el fondo a su paso. (v5.97: las 3 ondas
    /// de fuego + esta onda se UNIFICARON en la StyleNova única — ver arriba.)
    ///
    /// RENDER (4 capas de profundidad):
    ///   0. EFECTOS HIJOS DETRÁS (v5.95): llamarada PhoenixNova (sprites
    ///      aditivos) + carga de la Supernova — el disco los OCULTA (alpha≈1):
    ///      se leen como backlight asomando por el limbo de la estrella.
    ///   1. Backglow con BloomCircleSmall: amarillo * 0.7 (escala 0.95) + rojo * 0.45 (escala 1.61).
    ///   2. RadialShineShader sobre WavyBlotchNoise: color (252, 212, 112) * 0.24,
    ///      escala = width * scale * 2.72 / tamaño de la textura.
    ///   3. SunShader sobre DendriticNoiseZoomedOut (canvas):
    ///      coronaIntensityFactor = 0.05, mainColor = blanco, darkerColor = (204, 92, 25),
    ///      subtractiveAccentFactor = (181, 0, 0), sphereSpinTime = GlobalTimeWrappedHourly * 0.9,
    ///      s1 = WavyBlotchNoise, s2 = PsychedelicWingTextureOffsetMap,
    ///      escala = width * scale * 1.5 / tamaño de la textura.
    ///
    /// CICLO DE VIDA (v5.85/v5.86/v5.94/v5.96) - el sol como cuerpo celeste completo:
    ///   - t=0s    : nace con pop elástico + el GLOW CORONAL PERSISTENTE enciende
    ///               tenue (v5.96: reemplaza las llamaradas periódicas — cada nova
    ///               de 60 frames era un PARPADEO; el glow crece SIN OSCILACIÓN
    ///               sincronizado con el ciclo de vida y con el tamaño de la
    ///               estrella, y se dibuja DETRÁS del disco como backlight).
    ///   - t=0-7s  : el glow corona crece LENTO (halo 1.30→2.0× starR) mientras
    ///               el AURA DE ÁREA quema alrededor (1.35× starR, +OnFire).
    ///   - t=7s    : aparece SUPERNOVAPROJECTILE centrado y sincronizado (dura 3s);
    ///               carga energía mientras la gravedad del sol AUMENTA progresivamente
    ///               y su luz se intensifica (materia convergiendo en espiral).
    ///               v5.91: nace con el flag ai[2]=1 ("invocada por el sol") para
    ///               NO duplicar ondas al morir - las ondas de fuego las genera
    ///               EL SOL (autoridad absoluta de la sincronización).
    ///   - t=7-10s : v5.94 - GIGANTE ROJA: la estrella amarilla se HINCHA hasta
    ///               x1.85 y ENROJECE (backglow, aura, SunShader, luz, dusts y
    ///               partículas se tiñen) mientras su DAÑO DE ÁREA crece con
    ///               ella: hitbox de contacto x1.85, daño x1.75, AURA que sigue
    ///               a la estrella (hasta 1.65× starR hinchada ≈ 205px), quemadura
    ///               de 10 s (petición del usuario: "una estrella amarilla que
    ///               se convierte en gigante roja y luego explota, todo esto
    ///               haciendo que su daño en area crezca junto con la estrella").
    ///               El GLOW CORONAL crece con ella (se dimensiona con starR).
    ///   - t=10s   : ambos proyectiles explotan SIMULTÁNEAMENTE - nova masiva con
    ///               3 ONDAS EXPANSIVAS DE FUEGO (v5.94: radii 240/300/360 -
    ///               antes 360/450/540, cubrían toda la pantalla) que BARRAN
    ///               dañando cada 0.1 s (v5.91) y aplicando QUEMADURA de 10 s,
    ///               más el estallido de dusts y temblor de pantalla.
    ///
    ///     /// v5.91 — LA EXPLOSIÓN FINAL ES LA SUPERNOVA (SupernovaStaff), SINCRONIZADA
    /// POR CONSTRUCCIÓN: el OnKill del sol es ahora la AUTORIDAD de la explosión
    /// final — (1) mata la Supernova hija EN EL MISMO TICK (su flash + partículas
    /// estallan exactamente con el sol, sin depender de la sincronización por
    /// índice de la v5.88, que era el punto único de fallo de la "onda expansiva
    /// que no se procesaba"), (2) genera las 3 ondas de fuego y (3) el AoE del
    /// núcleo con el daño de la nova (daño del sol × 1.25). La Supernova hija
    /// (ai[2]=1) se salta sus propias ondas/AoE: CERO doble explosión — su
    /// papel es ser EL ESPECTÁCULO FINAL (flash + estallido + viento estelar).
    ///
    /// GRAVEDAD (cuerpo celeste): atrae solo enemigos, con una fuerza ~10 veces
    /// menor que la del agujero negro. Durante la carga de la supernova (últimos
    /// 3 segundos) la fuerza se multiplica progresivamente (x4 en el pico).
    ///
    /// QUEMADURA: bola de plasma ardiente → inflama enemigos al contacto (OnFire).
    /// (La quemadura potenciada por daño mágico se implementará cuando este
    /// proyectil se integre en el Grimorio, el arma definitiva.)
    ///
    /// v5.84 — Capa de partículas de la librería propia (data-oriented, additive,
    /// render en PostDrawTiles): corona de glóbulos SoftGlow orbitando con ColorShift
    /// amarillo→naranja, viento solar de estelas TrailGlow radiales, destellos
    /// SparkleStar con FadeIn+EmitLight, arcos de prominencia con estrellas orbitando.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        /// <summary>Shader del cuerpo — estático (v5.95): compartido por todas
        /// las instancias; lo cargan PreDraw y el BlackHoleLensSystem por igual.</summary>
        private static Ref<Effect> _sunShader;
        private static Ref<Effect> _shineShader;
        private static bool _sunShaderFailed;
        private static bool _shineShaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico y el ritmo de llamaradas.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>Índice del proyectil Supernova hijo (-1 = aún no invocado).</summary>
        public ref float SupernovaIndex => ref Projectile.ai[1];

        /// <summary>Duración total del sol: 10 segundos exactos.</summary>
        internal const int SunLifetime = 600;

        /// <summary>Momento (ticks restantes) en el que nace la supernova: segundo 7.</summary>
        internal const int SupernovaSpawnAtRemaining = 180;

        /// <summary>Cadencia de las llamaradas solares — v5.96: OBSOLETA.
        /// Las llamaradas periódicas (una nova de 60 frames cada 2 s: cada una
        /// un PARPADEO por diseño) se eliminaron; el sol lleva ahora un GLOW
        /// CORONAL PERSISTENTE que crece con su ciclo de vida (petición del
        /// usuario). La constante se conserva documentada para el histórico.</summary>
        private const int FlareInterval = 120;

        /// <summary>
        /// v5.94 — Progreso de la fase GIGANTE ROJA: 0 durante la secuencia
        /// principal (estrella amarilla) y 0→1 en los últimos 3 s (la estrella
        /// se hincha y enrojece antes de la supernova). Petición del usuario:
        /// "es una estrella amarilla que se convierte en gigante roja y luego
        /// explota, todo esto haciendo que su daño en area crezca junto con
        /// la estrella".
        /// </summary>
        private float RedGiantProgress => GetRedGiantProgress(Projectile);

        /// <summary>
        /// v5.95 — Progreso de la gigante roja (0..1), ESTÁTICO: lo consultan
        /// el BlackHoleLensSystem (lente sutil de la gigante) y los hijos que
        /// el sol dibuja detrás (tinte rojo de la llamarada).
        /// </summary>
        internal static float GetRedGiantProgress(Projectile p)
        {
            return p.timeLeft <= SupernovaSpawnAtRemaining
                ? 1f - p.timeLeft / (float)SupernovaSpawnAtRemaining
                : 0f;
        }

        /// <summary>
        /// v5.96 — Radio visual de la estrella (px): la mitad del canvas del
        /// SunShader (width×scale×1.5/2). Crece con la gigante roja — el glow
        /// coronal persistente y la lente lo abrazan.
        /// </summary>
        internal static float GetStarVisualRadius(Projectile p)
        {
            return p.width * p.scale * 0.75f;
        }

        /// <summary>
        /// v6.00 — El enemigo chaseable más cercano dentro de maxDist (para la
        /// persecución LIGERA del sol — petición del usuario).
        /// </summary>
        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>v5.94 — Mezcla un color hacia el ROJO de la gigante (fase final).</summary>
        private Color ToRedGiant(Color c)
        {
            float p = RedGiantProgress;
            if (p <= 0f) return c;
            int g = Math.Max((int)(c.G * (1f - 0.7f * p)), 20);
            int b = Math.Max((int)(c.B * (1f - 0.85f * p)), 8);
            return new Color(c.R, g, b, c.A);
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SunLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === v5.96 — GLOW CORONAL PERSISTENTE (REEMPLAZA A LAS LLAMARADAS) ===
            // Petición del usuario: "el brillo de PhoenixNovaStaff ya no debe
            // parpadear — debe comenzar a crecer lentamente y su crecimiento
            // debe estar sincronizado con el ciclo de vida del sol y con el
            // tamaño del mismo". Las llamaradas periódicas (una nova de 60
            // frames cada 2 s que nacía y moría — cada una un PARPADEO por
            // diseño) se ELIMINARON: en su lugar el sol lleva un GLOW CORONAL
            // CONTINUO que nace con la estrella y crece SIN OSCILACIÓN durante
            // toda su vida (lo dibuja DrawStarVisuals — ver
            // DrawCoronalGlowSprites). Su daño pasó al AURA DE ÁREA creciente
            // (abajo), que reemplaza el daño de contacto de las llamaradas.

            // === POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, VisualsTime, true));
            VisualsTime += 1f;

            // === v5.94 — GIGANTE ROJA (últimos 3 s) ===
            // La estrella amarilla de la secuencia principal SE HINCHA y
            // ENROJECE (hasta ×1.85 de tamaño) mientras la supernova carga:
            // compresión anterior (×1.0008) e hinchazón final de 30 ticks
            // (×1.025) eliminadas — el crecimiento es ahora CONTINUO y el
            // DAÑO DE ÁREA crece JUNTO con la estrella (hitbox + daño).
            bool supernovaCharging = Projectile.timeLeft <= SupernovaSpawnAtRemaining;
            float redProgress = RedGiantProgress;
            if (supernovaCharging)
            {
                // Crecimiento suave de la gigante roja (smoothstep → ×1.85).
                float growEase = redProgress * redProgress * (3f - 2f * redProgress);
                Projectile.scale *= 1f + 0.85f * growEase;

                // Daño de contacto creciendo con la estrella: ×1 → ×1.75.
                // ai[2] guarda el daño base (registrado al nacer).
                float baseDmg = Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;
                Projectile.ai[2] = baseDmg;
                int newDmg = Math.Max(1, (int)(baseDmg * (1f + 0.75f * redProgress)));
                if (newDmg != Projectile.damage)
                    Projectile.damage = newDmg;

                // El hitbox de área CRECE con la estrella (Resize mantiene el
                // centro: verificado en el código de Terraria).
                int sz = Math.Max(16, (int)(92f * Projectile.scale));
                if (sz != Projectile.width)
                    Projectile.Resize(sz, sz);
            }
            else if (Projectile.ai[2] <= 0f)
            {
                // Daño base registrado al nacer (para el ramp de la gigante).
                Projectile.ai[2] = Projectile.damage;
            }

            // === v6.00 — AURA MÁS GRANDE + MÁS TICKS DE DAÑO ===
            // Petición del usuario: "aumentar los tick de daños del sol y
            // aumentar el área de daño del sol". El pulso pasa de 15 a 10
            // ticks (+50% de golpes/s) y el área crece: 1.75→2.30× el radio
            // visual (la gigante roja ×1.85 la ARRASTRA: ~150→310 px), daño
            // del aura 45%.
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                VisualsTime > 30f && VisualsTime % 10f == 0f && Projectile.scale > 0.25f)
            {
                float lifeT = MathHelper.Clamp(VisualsTime / SunLifetime, 0f, 1f);
                float auraRadius = GetStarVisualRadius(Projectile) * (1.75f + 0.55f * lifeT);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 2f, DamageClass.Magic);
                    // El plasma ardiente inflama: quemadura que DOBLA en gigante.
                    npc.AddBuff(BuffID.OnFire, RedGiantProgress > 0f ? 600 : 300);
                }
            }

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;

            // === v6.00 — PERSIGUE LIGERAMENTE A LOS ENEMIGOS ===
            // Petición del usuario: "tanto el sol como el agujero negro deben
            // perseguir ligeramente a los enemigos". La estrella SE DESLIZA
            // hacia la presa más cercana (tope 3 px/t): gravita hacia donde
            // está la materia — sin perseguir al sprint.
            NPC prey = FindNearestEnemy(560f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 120f)
                {
                    toPrey.Normalize();
                    Projectile.velocity += toPrey * 0.09f;
                }
                float spd = Projectile.velocity.Length();
                if (spd > 3f)
                    Projectile.velocity *= 3f / spd;
            }
            Projectile.rotation += 0.01f;

            // === SUPERNOVA SINCRONIZADA (aparece en el segundo 7) ===
            if (Projectile.timeLeft == SupernovaSpawnAtRemaining && Projectile.owner == Main.myPlayer)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.25f));
                // v5.91 — ai[2] = 1: flag "invocada por el sol". La Supernova
                // hija NO generará sus propias ondas/AoE al morir (el OnKill del
                // SOL es la autoridad de la explosión final: ondas + AoE salen
                // del sol, la hija aporta el espectáculo visual sincronizado).
                // Así jamás hay doble explosión ni dependencia frágil de índices.
                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Projectile.velocity,
                    ModContent.ProjectileType<V20.SupernovaProjectile>(),
                    novaDamage, Projectile.knockBack,
                    Projectile.owner,
                    0f, 0f, 1f); // ai[0]=edad, ai[1]=libre, ai[2]=SunInvoked
                SupernovaIndex = idx;
            }

            // Mantener la supernova PERFECTAMENTE centrada en el sol (y sincronizada).
            if (SupernovaIndex >= 0f)
            {
                int idx = (int)SupernovaIndex;
                if (idx >= 0 && idx < Main.maxProjectiles &&
                    Main.projectile[idx].active &&
                    Main.projectile[idx].type == ModContent.ProjectileType<V20.SupernovaProjectile>())
                {
                    // El sol arrastra a la supernova con él (deriva compartida).
                    Main.projectile[idx].Center = Projectile.Center;
                    Main.projectile[idx].velocity = Projectile.velocity;

                    // SINCRONIZACIÓN EXACTA: en los últimos ticks, la cuenta
                    // regresiva de la supernova se clava a la del sol → ambos
                    // mueren (y explotan) en el MISMO tick, sin deriva de índices.
                    if (Projectile.timeLeft <= 2)
                        Main.projectile[idx].timeLeft =
                            Math.Min(Main.projectile[idx].timeLeft, Projectile.timeLeft);
                }
                else
                {
                    SupernovaIndex = -1f;
                }
            }

            // === GRAVEDAD DEL SOL — 10 veces menor que el agujero negro, solo enemigos ===
            float gravityRadius = 280f;
            float baseStrength = 0.26f; // agujero negro: 2.6 → sol: 2.6 / 10
            // Durante la carga de la supernova la fuerza crece progresivamente (x4 pico).
            float chargeMult = 1f;
            if (supernovaCharging)
            {
                float chargeProgress = 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining;
                chargeMult = 1f + chargeProgress * 3f;
            }
            float sunGravity = baseStrength * chargeMult;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * sunGravity;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === v6.00 — LA GRAVEDAD DEL SOL TAMBIÉN DOBLA PROYECTILES ENEMIGOS ===
            // Petición del usuario: "deben ser capaces de afectar los
            // proyectiles con su gravedad". La gravedad estelar es DÉBIL (el
            // agujero es 10× más fuerte) pero las balas hostiles se curvan
            // hacia la estrella — y si tocan el plasma, SE EVAPORAN en polvo
            // de fuego (chispas naranjas).
            if (Main.netMode != NetmodeID.Server)
            {
                float starR = GetStarVisualRadius(Projectile);
                foreach (Projectile pr in Main.ActiveProjectiles)
                {
                    if (pr == null || !pr.active || !pr.hostile || pr.friendly) continue;
                    Vector2 toStar = Projectile.Center - pr.Center;
                    float d = toStar.Length();
                    if (d > gravityRadius || d < 4f) continue;

                    // ¿Tocó el plasma? → EVAPORADA (chispas de fuego).
                    if (d < starR * 0.7f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            pr.Kill();
                        for (int i = 0; i < 5; i++)
                        {
                            Dust d2 = Dust.NewDustPerfect(pr.Center, DustID.Torch,
                                new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(-2.4f, 0.6f)),
                                200, new Color(255, 180, 80), 0.7f);
                            d2.noGravity = true;
                            d2.fadeIn = 0f;
                        }
                        continue;
                    }

                    float ps = (1f - d / gravityRadius) * sunGravity * 0.3f;
                    if (toStar.LengthSquared() > 0.01f)
                    {
                        toStar.Normalize();
                        pr.velocity += toStar * ps;
                    }
                }
            }

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Dusts vanilla (capa frontal, se dibujan encima del canvas del shader)
                SpawnOrbitingSparks();
                SpawnFlames();
                SpawnSmoke();
                SpawnSolarFlare();
                SpawnTwinkles();

                // Partículas de la librería propia (capa de fondo aditiva)
                SpawnLibraryCorona();
                SpawnLibrarySolarWind();
                SpawnLibraryTwinkles();
                SpawnLibraryFlareLoop();

                // v5.85: materia convergiendo durante la carga de la supernova
                if (supernovaCharging && Projectile.scale > 0.3f)
                {
                    SpawnSupernovaChargeIntake();
                }
            }

            // === ILUMINACIÓN INTENSA (con pulso sutil + crecimiento en la carga) ===
            // v5.94 — la luz ENROJECE con la gigante: amarillo cálido → rojo.
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            float chargeLight = supernovaCharging
                ? 1f + (1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining) * 0.8f
                : 1f;
            Vector3 lightColor = Vector3.Lerp(
                new Vector3(1f, 0.9f, 0.5f),
                new Vector3(1f, 0.25f, 0.1f),
                redProgress);
            Lighting.AddLight(Projectile.Center,
                lightColor * 3.2f * pulse * chargeLight);
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS VANILLA (capa frontal)
        // ------------------------------------------------------------------

        /// <summary>Chispas de fuego (Torch) orbitando y cayendo hacia la superficie.</summary>
        private void SpawnOrbitingSparks()
        {
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(40f, 60f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = Projectile.Center - spawnPos;
                if (vel.LengthSquared() > 0.01f)
                {
                    vel.Normalize();
                    vel *= Main.rand.NextFloat(1f, 3f);
                    vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                        vel, 150, ToRedGiant(new Color(255, 150, 50)), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Llamas de GoldFlame escapando de la fotosfera.</summary>
        private void SpawnFlames()
        {
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(30f, 45f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * 2f,
                    (float)Math.Sin(angle) * 2f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                    vel, 200, ToRedGiant(new Color(255, 200, 100)), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Humo cálido ascendiendo desde la corona.</summary>
        private void SpawnSmoke()
        {
            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(50f, 70f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * 0.5f,
                    (float)Math.Sin(angle) * 0.5f - 1f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                    vel, 60, new Color(100, 60, 30), 0.6f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Prominencias periódicas: explosión radial de fuego desde el borde (~0.75 s).</summary>
        private void SpawnSolarFlare()
        {
            if (VisualsTime % 45f == 0f && VisualsTime > 30f)
            {
                float baseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = baseAngle + (MathHelper.TwoPi / count) * i * 0.35f;
                    float dist = Projectile.width * 0.55f * Projectile.scale;
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * Main.rand.NextFloat(3f, 6f),
                        (float)Math.Sin(angle) * Main.rand.NextFloat(3f, 6f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 220, ToRedGiant(new Color(255, 180, 80)), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Destellos encantados parpadeando alrededor de la estrella.</summary>
        private void SpawnTwinkles()
        {
            if (Main.rand.NextBool(20))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 110f) * Projectile.scale;
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    Vector2.Zero, 255, ToRedGiant(new Color(255, 240, 180)), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0.3f;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS DE LA LIBRERÍA PROPIA (capa de fondo aditiva)
        // ------------------------------------------------------------------

        /// <summary>Corona de plasma orbitando: SoftGlow con Orbit y ColorShift amarillo→naranja.</summary>
        private void SpawnLibraryCorona()
        {
            if (Main.rand.NextBool(2))
            {
                float radius = Main.rand.NextFloat(48f, 60f) * MathHelper.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = Main.rand.NextFloat(0.045f, 0.075f);

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = Vector2.One * Main.rand.NextFloat(0.55f, 0.95f),
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 235, 140, 150))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 235, 140, 150))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 110, 30, 30))),
                    TimeLeft = 55,
                    Duration = 55,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.UserData0 = Projectile.Center.X;
                p.UserData1 = Projectile.Center.Y;
                p.UserData2 = angVel;
                p.UserData3 = radius;
                p.EnableComponent(ComponentFlag.Orbit);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Viento solar radial: estelas TrailGlow fluyendo hacia fuera desde la fotosfera.</summary>
        private void SpawnLibrarySolarWind()
        {
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(34f, 44f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = outward * Main.rand.NextFloat(1.2f, 2.2f),
                    Scale = new Vector2(1.5f, 0.4f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 245, 190, 160))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 245, 190, 160))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 120, 40, 20))),
                    TimeLeft = 32,
                    Duration = 32,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Destellos luminosos: SparkleStar con FadeIn + FadeOut + EmitLight.</summary>
        private void SpawnLibraryTwinkles()
        {
            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(55f, 100f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 0.25f,
                    Scale = Vector2.One * Main.rand.NextFloat(0.5f, 0.9f),
                    Rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi),
                    RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 250, 210, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 250, 210, 200)),
                    TimeLeft = 40,
                    Duration = 40,
                    TextureId = ParticleTex.SparkleStar,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.UserData0 = 8f;
                p.EnableComponent(ComponentFlag.FadeIn);
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.EmitLight);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Arcos de prominencia: estrellas orbitando en el borde de cada llamarada (~0.75 s).</summary>
        private void SpawnLibraryFlareLoop()
        {
            if (VisualsTime % 45f == 0f && VisualsTime > 30f && Projectile.scale > 0.3f)
            {
                float flareRadius = Projectile.width * 0.62f * Projectile.scale;
                for (int i = 0; i < 7; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float radius = flareRadius + Main.rand.NextFloat(-4f, 8f);
                    float angVel = Main.rand.NextBool(2) ? 0.14f : -0.14f;

                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius);

                    var p = new ParticleData
                    {
                        Position = spawnPos,
                        Velocity = Vector2.Zero,
                        Scale = Vector2.One * Main.rand.NextFloat(0.4f, 0.8f),
                        Rotation = angle,
                        RotationSpeed = angVel * 2f,
                        PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 210, 110, 210))),
                        PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 230, 160, 210))),
                        PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 90, 20, 30))),
                        TimeLeft = 45,
                        Duration = 45,
                        TextureId = ParticleTex.Star,
                        BlendMode = 1,
                        LayerPriority = LayerPriorities.BeforeProjectiles,
                    };
                    p.UserData0 = Projectile.Center.X;
                    p.UserData1 = Projectile.Center.Y;
                    p.UserData2 = angVel;
                    p.UserData3 = radius;
                    p.EnableComponent(ComponentFlag.Orbit);
                    p.EnableComponent(ComponentFlag.ColorShift);
                    p.EnableComponent(ComponentFlag.FadeOut);
                    ParticleManager.Spawn(p);
                }
            }
        }

        /// <summary>
        /// v5.85 — Materia convergiendo durante la carga de la supernova:
        /// estelas doradas cayendo en espiral hacia el sol mientras la fuerza
        /// gravitatoria crece (los 3 segundos previos a la nova final).
        /// </summary>
        private void SpawnSupernovaChargeIntake()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(90f, 150f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() < 0.01f) continue;
                toCenter.Normalize();
                Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);

                // Cuanto más avanzada la carga, más rápido converge la materia.
                float chargeProgress = 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining;
                float speed = 2.2f + chargeProgress * 3.5f;

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = toCenter * speed + tangent * speed * 0.55f,
                    Scale = new Vector2(1.6f, 0.4f),
                    Rotation = (float)Math.Atan2(toCenter.Y, toCenter.X),
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 240, 170, 190))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 240, 170, 190))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 140, 40, 40))),
                    TimeLeft = 38,
                    Duration = 38,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(p);
            }
        }

        // ------------------------------------------------------------------
        //  RENDER
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // v5.95 — LA LENTE VA DETRÁS DEL SOL: con la lente activa (agujero
            // negro, ondas cromáticas/de lente o la propia gigante roja en
            // pantalla) el sol NO se dibuja en el pase del mundo (quedaría
            // dentro de screenTarget y la distorsión lo deformaría): el
            // BlackHoleLensSystem lo pinta ENCIMA de la lente llamando a
            // DrawStarVisuals — igual que hace con el núcleo del agujero negro.
            if (BlackHoleLensSystem.LensActive)
                return false;

            DrawStarVisuals(Projectile, true);

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// v5.95 — Dibuja el sol COMPLETO: primero sus EFECTOS HIJOS DETRÁS
        /// (llamarada PhoenixNova + carga de la Supernova — petición del
        /// usuario: "sus efectos SupernovaStaff y PhoenixNovaStaff deben estar
        /// detrás de él"), luego las capas propias de la estrella (backglow →
        /// aura → disco del SunShader). Compartido entre el pase del mundo
        /// (PreDraw, endActiveBatch=true) y el pase posterior a la lente
        /// (BlackHoleLensSystem, endActiveBatch=false: el batch llega cerrado).
        /// El disco del SunShader emite alpha≈1 en su centro → OCULTA lo que
        /// tiene detrás: la llamarada se lee como un backlight real asomando
        /// por el limbo de la estrella (la llamarada IGUALA el tamaño del sol
        /// — petición del usuario — porque se dimensiona con su radio real).
        /// </summary>
        internal static void DrawStarVisuals(Projectile p, bool endActiveBatch)
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _sunShaderFailed = true;
                }
            }
            if (!_shineShaderFailed && _shineShader == null)
            {
                try
                {
                    _shineShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shineShaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = p.scale;
                // v5.94 — GIGANTE ROJA: mezcla de color según la fase (0 = amarilla).
                float rg = GetRedGiantProgress(p);
                // v5.95 — Radio visual real de la estrella: la llamarada lo IGUALA.
                float starR = GetStarVisualRadius(p);

                // === 0. EFECTOS HIJOS DETRÁS DE LA ESTRELLA (v5.95) ===
                // v5.96 — GLOW CORONAL PERSISTENTE: reemplaza a las llamaradas
                // PhoenixNova periódicas (cada nova de 60 frames era un
                // PARPADEO por diseño — petición del usuario: "el brillo ya no
                // debe parpadear, debe comenzar a crecer lentamente,
                // sincronizado con el ciclo de vida del sol y con el tamaño del
                // mismo"). Este glow nace con la estrella y crece SIN
                // OSCILACIÓN durante toda su vida (función pura de lifeT), y
                // como se dimensiona con starR, la GIGANTE ROJA (×1.85) lo
                // arrastra con ella — el crecimiento sigue al TAMAÑO del sol.
                // La carga de la Supernova hija sigue dibujándose detrás también.
                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                float lifeT = MathHelper.Clamp(p.ai[0] / SunLifetime, 0f, 1f);
                DrawCoronalGlowSprites(drawPos, starR, lifeT, rg);

                // Carga de la Supernova hija (índice en ai[1], SINCRONIZADO en MP):
                // el halo dorado condensándose DETRÁS del disco, escalando con la
                // estrella (1.35× su radio base para asomar por el limbo).
                int novaIdx = (int)p.ai[1];
                if (novaIdx >= 0 && novaIdx < Main.maxProjectiles)
                {
                    Projectile nova = Main.projectile[novaIdx];
                    if (nova != null && nova.active &&
                        nova.type == ModContent.ProjectileType<V20.SupernovaProjectile>() &&
                        nova.ai[2] == 1f)
                    {
                        SupernovaProjectile.DrawChargeSprites(nova,
                            nova.Center - Main.screenPosition,
                            1.35f * starR / 69f);
                    }
                }
                Main.spriteBatch.End();

                // === 1. BACKGLOW ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/BloomCircleSmall").Value;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // Amarillo → ROJO brillante (pequeño e intenso, crece con la gigante)
                Color glowHot = Color.Lerp(new Color(255, 230, 100), new Color(255, 75, 25), rg);
                glowHot.A = 0; // igual que el original (alpha 0)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowHot * 0.7f, 0f,
                    bloomCircle.Size() * 0.5f, scale * (0.95f + 0.35f * rg), SpriteEffects.None, 0f);
                // Rojo profundo (grande y tenue — envuelve a la gigante)
                Color glowRed = Color.Lerp(new Color(255, 50, 0), new Color(255, 30, 10), rg);
                glowRed.A = 0; // igual que el original (alpha 0)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowRed * 0.45f, 0f,
                    bloomCircle.Size() * 0.5f, scale * (1.61f + 0.5f * rg), SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. RADIAL SHINE (aura con ruido animado) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/WavyBlotchNoise").Value;
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    shineShader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    Vector2 shineScale = Vector2.One * p.width * scale * 2.72f / wavyBlotch.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    _shineShader.Value.CurrentTechnique.Passes[0].Apply();
                    // v5.94 — el aura ENROJECE con la gigante.
                    Color shineColor = Color.Lerp(new Color(252, 212, 112), new Color(255, 95, 45), rg);
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        shineColor * 0.24f, p.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 3. SUNSHADER (la estrella) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/PsychedelicWingTextureOffsetMap").Value;
                    Texture2D dendritic = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/DendriticNoiseZoomedOut").Value;

                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    // v5.94 — GIGANTE ROJA: el cuerpo de la estrella enrojece.
                    shader.Parameters["mainColor"].SetValue(
                        Color.Lerp(new Color(255, 255, 255), new Color(255, 150, 120), rg).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(
                        Color.Lerp(new Color(204, 92, 25), new Color(150, 28, 12), rg).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.9f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // s1 = accentNoise, s2 = uvOffsetNoise
                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    Vector2 drawScale = Vector2.One * p.width * scale * 1.5f / dendritic.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, p.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(p, drawPos, scale);
                }
            }
            catch
            {
                // v5.90 — cierre defensivo SOLO en el path de error: si la
                // excepción interrumpió un Begin a medias, lo cerramos aquí.
                // (El try{End} incondicional de v5.88 disparaba una excepción
                // first-chance cada frame — tML la registraba como "Excepción
                // silenciosa" en el client.log.)
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// v5.96 — GLOW CORONAL PERSISTENTE (reemplaza a las llamaradas
        /// PhoenixNova periódicas — petición del usuario: "el brillo ya no
        /// debe parpadear, debe comenzar a crecer lentamente y su crecimiento
        /// debe estar sincronizado con el ciclo de vida del sol y con el
        /// tamaño del mismo").
        ///
        /// DOS capas de SoftGlow aditivas, TODO función PURA de lifeT (0..1 del
        /// ciclo de vida) — CERO oscilación: ningún sin(), ningún flash, ningún
        /// parpadeo. El halo exterior nace a 1.30×starR y crece LENTO hasta
        /// 2.35×; la corona interna abraza el disco (1.05×→1.55×). El brillo
        /// sube con el ciclo (alpha 70→195 y 55→145). El tinte rojo de la
        /// gigante (rg) tiñe ambas capas. Y como TODO se dimensiona con starR,
        /// el crecimiento de la gigante roja (×1.85) ARRASTRA al glow con la
        /// estrella: crecimiento SINCRONIZADO con el tamaño del sol.
        /// Requiere el batch ADITIVO ya abierto — lo gestiona DrawStarVisuals.
        /// </summary>
        internal static void DrawCoronalGlowSprites(Vector2 drawPos, float starR,
            float lifeT, float rg)
        {
            Texture2D softGlow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (softGlow == null) return;
            Vector2 glowOrigin = softGlow.Size() * 0.5f;

            // === HALO EXTERIOR (backlight): 1.30× → 2.35× starR ===
            float haloR = starR * (1.30f + 1.05f * lifeT);
            byte haloA = (byte)(70f + 125f * lifeT);
            Color haloCol = Color.Lerp(
                new Color(255, 150, 55, 255), new Color(255, 60, 25, 255), rg) * ((float)haloA / 255f);
            float haloScale = haloR / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, haloCol, 0f,
                glowOrigin, haloScale, SpriteEffects.None, 0f);

            // === CORONA INTERNA (abraza el disco): 1.05× → 1.55× starR ===
            float corR = starR * (1.05f + 0.50f * lifeT);
            byte corA = (byte)(55f + 90f * lifeT);
            Color corCol = Color.Lerp(
                new Color(255, 235, 170, 255), new Color(255, 135, 90, 255), rg) * ((float)corA / 255f);
            float corScale = corR / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, corCol, 0f,
                glowOrigin, corScale, SpriteEffects.None, 0f);
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            // v5.90 — Begin directo: el path normal deja el batch CERRADO (todas
            // las capas están balanceadas Begin→End); el cierre defensivo de
            // emergencia vive en el catch del PreDraw, no aquí cada frame
            // (disparaba una "Excepción silenciosa" por frame en el log).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Dibujado manual de respaldo (glow multicapa naranja).</summary>
        private static void DrawFallback(Projectile p, Vector2 drawPos, float scale)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                glowTex.Size() * 0.5f, 1.5f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                glowTex.Size() * 0.5f, 2.0f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                glowTex.Size() * 0.5f, 2.8f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Estallido solar de la librería sobre el objetivo
            ParticlePresets.Explosion(target.Center, 60f, 14,
                new Color(255, 240, 170), new Color(255, 110, 30), 26);
            ParticlePresets.RingPulse(target.Center, 85f, new Color(255, 200, 90, 180), 20);

            // Explosión radial de fuego sobre el objetivo
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 10f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 10f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Ráfaga de chispas Torch
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Torch,
                    dir, 180, new Color(255, 150, 50), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // QUEMADURA: el sol es una bola de plasma ardiente → inflama al enemigo.
            // v5.94 - GIGANTE ROJA: la estrella hinchada quema el DOBLE (10 s).
            target.AddBuff(BuffID.OnFire, RedGiantProgress > 0f ? 600 : 300);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // ================================================================
            //  v5.91 — LA EXPLOSIÓN FINAL ES LA SUPERNOVA, SINCRONIZADA POR
            //  CONSTRUCCIÓN: el OnKill del sol es la AUTORIDAD (el sol muere
            //  EXACTAMENTE a los 10 s — timeLeft fijo, nada lo mata antes).
            //  ================================================================
            //  1. Mata la Supernova hija EN ESTE MISMO TICK → su flash + estallido
            //     + viento estelar ocurren EXACTAMENTE con la muerte del sol
            //     (la v5.88 dependía de la sincronización por índice ai[1] + un
            //     clamp de timeLeft — punto único de fallo del "la onda no se
            //     procesó correctamente": si el índice cambiaba o el clamp no
            //     llegaba a aplicar, la nova moría antes/después del sol y la
            //     ola se perdía o descuadraba).
            //  2. Genera LAS 3 ONDAS EXPANSIVAS DE FUEGO (las mismas de la nova
            //     v5.88: radii 360/450/540, retardo escalonado de 8 ticks) con el
            //     daño de la nova (sol × 1.25 × 0.5) — cada una BARRA dañando
            //     cada 0.1 s a medida que avanza y aplicando QUEMADURA 10 s.
            //  3. AoE del núcleo (340 px) con el daño de la nova + quemadura 10 s.
            //  La Supernova hija (ai[2]=1) NO genera ondas/AoE propios → cero
            //  dobles. La SupernovaStaff standalone conserva su explosión completa.
            TryKillSupernova();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.25f));
                // v5.97 — UNA SOLA EXPLOSIÓN DONDE SUCEDE TODO (petición del
                // usuario: "el sol y el agujero negro tienen dos, digamos
                // explosiones al terminar, solo deben tener una"): la muerte
                // del sol ya no suelta 3 ondas de fuego + 1 onda de lente —
                // suelta UNA SOLA ONDA NOVA DE LENTE (StyleNova): el frente de
                // espaciotiempo QUE LLEVA EL FUEGO — triple anillo ardiente +
                // frente blanco de choque + aberración CÁLIDA (oro/brasa, no
                // RGB) — con el DAÑO DE LA NOVA COMPLETO (antes ×0.5 repartido
                // entre 4 ondas) + quemadura 10 s en la banda, y registrada
                // como fuente del BlackHoleLensSystem → el fondo se curva a su
                // paso. Radio 380: el de la antigua onda mayor (360) + margen
                // de lente.
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    novaDamage, 0f, Projectile.owner,
                    0f,                                          // edad: sin retardo — TODO sucede YA
                    CosmicShockwaveProjectile.StyleNova,
                    380f);                                       // radio máximo

                // Daño AoE del núcleo de la nova (el epicentro de la MISMA
                // explosión — la onda única barre desde el centro, el núcleo
                // golpea el punto ciego inicial). v5.94: 340 → 260.
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 260f)
                    {
                        npc.SimpleStrikeNPC(novaDamage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 600); // quemadura 10 s
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === NOVA MASIVA (segundo 10, sincronizada con la explosión de la Supernova) ===
            // Ráfaga principal con interpolación blanco→naranja
            ParticlePresets.Explosion(Projectile.Center, 170f, 40,
                new Color(255, 245, 200), new Color(255, 90, 20), 50);
            // DOBLE ONDA EXPANSIVA (dorada rápida + roja retardada)
            // v5.94 — tamaños proporcionales a las ondas nuevas (la librería
            // además ahora mide el radio REAL de la textura HD).
            ParticlePresets.RingPulse(Projectile.Center, 200f,
                new Color(255, 210, 100, 210), 34);
            ParticlePresets.RingPulse(Projectile.Center, 270f,
                new Color(255, 80, 30, 150), 46);
            // Ráfaga de viento solar radial de la librería
            for (int i = 0; i < 22; i++)
            {
                float angle = (MathHelper.TwoPi / 22) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var p = new ParticleData
                {
                    Position = Projectile.Center + outward * 30f,
                    Velocity = outward * Main.rand.NextFloat(3.5f, 7f),
                    Scale = new Vector2(2.2f, 0.5f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(new Color(255, 240, 180, 190)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 240, 180, 190)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 90, 20, 20)),
                    TimeLeft = 42,
                    Duration = 42,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }

            // Screenshake coordinado
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 8f, 12, 18, 0.45f,
                    "AethonSunNova"));
            }
            catch { }

            // === NOVA FINAL: explosión masiva de fuego (dusts, capa frontal) ===
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas Torch en todas direcciones
            for (int i = 0; i < 35; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f)),
                    200, new Color(255, 150, 50), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Núcleo de la nova: destellos encantados
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f)),
                    255, new Color(255, 240, 180), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Humo ascendente tras la explosión
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f)),
                    100, new Color(120, 70, 40), 1.0f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        /// <summary>
        /// v5.91 — Mata la Supernova hija EN EL MISMO TICK que el sol (si sigue
        /// viva): su OnKill visual (flash + estallido + viento) estalla
        /// EXACTAMENTE con la muerte del sol — sincronización perfecta POR
        /// CONSTRUCCIÓN, sin depender de índices ni clamps de timeLeft. La
        /// hija detecta el flag ai[2]=1 y NO genera ondas/AoE (el sol ya lo
        /// hizo): cero dobles explosiones.
        /// </summary>
        private void TryKillSupernova()
        {
            if (SupernovaIndex < 0f) return;
            int idx = (int)SupernovaIndex;
            if (idx >= 0 && idx < Main.maxProjectiles &&
                Main.projectile[idx].active &&
                Main.projectile[idx].type == ModContent.ProjectileType<V20.SupernovaProjectile>())
            {
                // Kill() dispara su OnKill AHORA — mismo tick que el sol.
                Main.projectile[idx].Kill();
            }
            SupernovaIndex = -1f;
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
