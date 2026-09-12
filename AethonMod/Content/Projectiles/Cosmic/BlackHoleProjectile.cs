using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// v5.95 — EL CAMPO DE FUERZA ES LA ONDA EXPANSIVA (petición del usuario):
    /// el escudo Perlin/ForceField YA NO vive alrededor del agujero durante
    /// su vida — al explotar, la burbuja PARTE del radio que tenía el escudo
    /// al morir y CABALGA el frente de la onda cromática expandiéndose con él
    /// mientras se desvanece (la secuencia de destrucción del escudo de una
    /// Columna Lunar CONVERTIDA en onda). FIX CRÍTICO del error del usuario:
    /// v5.94 guardaba el radio de la burbuja en `Projectile.ai[3]` — un índice
    /// que NO EXISTE (el array ai de tModLoader tiene solo 3 ranuras) →
    /// IndexOutOfRangeException en OnKill y DrawWaveVisual (client.log):
    /// la burbuja jamás se dibujó y el OnKill abortó antes de sonidos/dusts.
    /// Ahora viaja en localAI[0]. FIX del radio pre-colapso: localAI[1] se
    /// captura UNA VEZ al empezar la evaporación (antes se recalculaba cada
    /// tick con la escala ya colapsada y el escudo “que mantiene su tamaño”
    /// decaía de 101px a 5px).
    ///
    /// v5.94 — CAMPO DE FUERZA REAL DE LAS COLUMNAS LUNARES: investigado el
    /// código de Terraria (Main.DrawNPCDirect_Inner) y usado su mecanismo
    /// EXACTO — ruido Perlin ("Terraria/Images/Misc/Perlin", la textura del
    /// juego) en un quad 600×600 con el shader GameShaders.Misc["ForceField"]
    /// VANILLA (v5.95: solo en la onda de la explosión). AURA DE DAÑO: los
    /// enemigos dentro del campo reciben daño cada 0.5 s (límites de daño en
    /// área mejorados). Anillos de fotones eliminados (los anillos solo
    /// viven en la explosión final). Onda cromática: 620 → 420 px.
    ///
    /// BlackHoleProjectile — agujero negro con lensing gravitacional real.
    ///
    /// RENDER: RealBlackHoleShader.fx (lightmarch de 75 pasos con lensing gravitacional
    /// real) sobre un canvas de InvisiblePixel:
    ///   - v5.90 — CANVAS QUE CRECE CON LA ESCALA (256px * scale): antes el canvas
    ///     era FIJO de 256px y el zoom interno crecía con la escala → al hincharse
    ///     para morir el disco de acreción se salía del canvas y se CORTABA por
    ///     los lados con líneas verticales duras. Ahora la cobertura del shader
    /// es constante (zoom fijo = width/256*2) y el disco NUNCA cruza el borde.
    ///   - zoom fijo: width / 256 * 2 — accretionDiskRadius con tope (min(scale,1)*0.4)
    ///   - cameraRotationAxis: (velocity.Y * -0.022 + 1, 0, rotation)
    ///   - cameraAngle: 0.32 / accretionDiskScale: (1, 0.33, 1)
    ///
    /// v5.86 — LA LENTE VA DETRÁS DEL AGUJERO Y SUS EFECTOS: con la lente
    /// activa el núcleo NO se dibuja en el pase del mundo (PreDraw se salta);
    /// el BlackHoleLensSystem lo pinta ENCIMA de la distorsión vía
    /// DrawCoreVisuals(), de modo que la lente jamás deforma al propio
    /// agujero. Las partículas de sus efectos pasan a la capa AboveLens
    /// (también encima de la lente).
    ///
    /// v5.90 — SECUENCIA DE MUERTE simplificada (crecimiento → evaporación →
    /// UNA explosión cromática final):
    ///   - t-90..t-36: el ÁREA DE EFECTO crece de forma momentánea (+60% radio
    ///     de gravedad y +60% escala visual) — sin ondas ni estruendos: el
    ///     agujero simplemente se hincha devorando el espacio.
    ///   - t-36..t-0 (evaporación): colapso acelerado de la escala hacia 0.
    ///   - t-0 (desaparición): UNA SOLA ONDA EXPANSIVA CROMÁTICA (estilo 0) con
    ///     aberración cromática real (canales R/G/B separados radialmente) que
    ///     también distorsiona el fondo a su paso y HACE DAÑO por frente. Las
    ///     4 ondas intermedias/inversas de v5.86 se eliminaron por completo.
    ///
    /// v5.90 — PARTÍCULAS: SIN moradas. La materia devorada es CÁLIDA: estelas
    /// ámbar/blancas que caen en espiral al horizonte (componente PullTo de la
    /// librería — aceleran hacia el centro y mueren al llegar: absorbidas),
    /// polvo dorado en espiral, chispas capturadas, disco de acreción naranja y
    /// anillo de fotones. El halo exterior pasó de púrpura a ámbar profundo.
    ///
    /// v5.91 — MATERIA ABSORBIDA ORIENTADA AL CENTRO: las estelas ya no viajan
    /// tangenciales — nacen con velocidad RADIAL hacia el centro y su eje largo
    /// apunta AL CENTRO del agujero (petición del usuario: "deben estar ubicadas
    /// en dirección hacia el centro"), con una componente tangencial sutil que
    /// mantiene la caída en espiral del infalling. La ACELERACIÓN de absorción
    /// (PullTo) se dispara durante la secuencia de muerte (PullToGlobalBoost,
    /// hasta ×6 en el instante de explotar: "la velocidad de las partículas debe
    /// acelerarse en el momento de explotar").
    ///
    /// v5.91 — VIDA = 10 SEGUNDOS EXACTOS (600 ticks, ya estaba en 600 desde
    /// v5.86: t=0 pop elástico → t-90 crece hinchándose → t-36 evaporación →
    /// t-0 UNA explosión cromática). GRAVEDAD: fuerza 2.6 — el agujero negro
    /// tiene 10 VECES MÁS fuerza de atracción que el sol (0.26, ver
    /// SunProjectile): el sol nunca lo iguala ni en su pico de carga (×4 =
    /// 1.04 < 2.6).
    ///
    /// v5.85 — LENTE GRAVITACIONAL de pantalla (BlackHoleLensSystem): el fondo
    /// real del juego se distorsiona alrededor del horizonte de sucesos con el
    /// shader BlackHoleDistortionShader (formalismo relativista con decaimiento
    /// exponencial). Fuerza gravitatoria aumentada y radio de atracción de 450px.
    /// v5.90: la lente es DELGADA (ángulo pico ~0.8 rad, antes 14.9) — solo
    /// deforma un anillo estrecho alrededor del agujero, jamás toda la pantalla.
    ///
    /// Mejoras propias: pop elástico de aparición, succión espiral de materia
    /// cálida, atracción gravitacional de enemigos Y devoración del polvo
    /// cercano, refuerzo del event horizon.
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        /// <summary>Shader del núcleo — estático: compartido por todas las instancias
        /// (lo cargan PreDraw y el BlackHoleLensSystem por igual).</summary>
        private static Ref<Effect> _shader;
        private static bool _shaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>
        /// v5.94 — Multiplicador del radio del campo de fuerza sobre el horizonte
        /// de sucesos (2.2×: envuelve el disco de acreción, que llega a ~1.4×).
        /// </summary>
        private const float ShieldRadiusMult = 2.2f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // v5.85: área de daño ampliada (76 → 96): el hitbox y el canvas del
            // shader escalan con width, así que el agujero también se ve mayor.
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN ===
            // scale = ElasticOut(0..120) * sqrt(InverseLerp(0..60)) — el agujero "rebota" al nacer.
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // ================================================================
            //  v5.90 — SECUENCIA DE MUERTE (crecimiento → evaporación → explosión)
            // ================================================================
            // FASE 1 — CRECIMIENTO (t-90..t-36): el área de efecto crece de forma
            // momentánea (+60% radio de gravedad y +60% escala visual). v5.90:
            // SIN onda cromática ni estruendo aquí — toda la energía se libera
            // en la ÚNICA explosión cromática final (OnKill).
            float expansion = 0f;
            if (Projectile.timeLeft > 36f && Projectile.timeLeft <= 90f)
            {
                expansion = 1f - (Projectile.timeLeft - 36f) / 54f; // 0 → 1
                // La escala visual crece (hasta +60%): el horizonte de sucesos
                // se hincha devorando el espacio (v5.90: el canvas del shader
                // crece con la escala → el disco ya NO se corta por los lados).
                Projectile.scale *= 1f + expansion * 0.6f;
            }
            else if (Projectile.timeLeft <= 36f)
            {
                // v5.94/v5.95 — Radio del CAMPO DE FUERZA con la escala ANTES del
                // colapso de la evaporación: se captura UNA SOLA VEZ (v5.95 fix —
                // antes se recalculaba cada tick con la escala YA colapsada y el
                // radio decaía de 101px a 5px en vez de mantenerse). El OnKill lo
                // pasa a la onda cromática (localAI[0]) para que la burbuja que
                // CABALGA la onda arranque EXACTAMENTE de este radio.
                if (Projectile.localAI[1] <= 0f)
                    Projectile.localAI[1] = 0.3f * Projectile.width *
                                            Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;

                // FASE 2 — EVAPORACIÓN: colapso acelerado hacia la singularidad
                // (la escala cae a 0 justo cuando llega la implosión final).
                float collapse = Utils.GetLerpValue(36f, 0f, Projectile.timeLeft, true);
                Projectile.scale *= 1f - collapse;
                // El área de efecto se mantiene crecida hasta evaporarse.
                expansion = 1f;
            }

            // === v5.91 — ACELERACIÓN DE LA MATERIA ABSORBIDA HACIA LA EXPLOSIÓN ===
            // La fuerza PullTo de TODAS las partículas absorbidas se multiplica
            // por 1 + expansion·5 (hasta ×6 al evaporarse): la materia literalmente
            // ACELERA hacia el centro en el momento de explotar (petición del
            // usuario). El OnKill la resetea a 1f — jamás queda acelerada "colgada".
            // (Con dos agujeros simultáneos escribe el más avanzado por frame —
            // el boost es visual y no acumulativo.)
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // v5.94 — FLASH DEL ESCUDO eliminado en v5.95 (el campo de fuerza ya
            // no vive en vida del agujero: es la onda expansiva de la explosión).

            // === MOVIMIENTO: deriva lenta y frenado (el agujero flota) ===
            Projectile.velocity *= 0.97f;

            // Rotación suave hacia velocity.X * 0.04
            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // v5.90 — MATERIA ABSORBIDA (paleta cálida: ámbar/blanco, sin
                // nada morado): estelas de la librería con PullTo que caen en
                // espiral acelerando hacia el horizonte + polvo dorado + disco
                // de acreción naranja + anillo de fotones.
                SpawnAbsorbedDusts();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();

                // Partículas de la librería propia en la capa ENCIMA DE LA LENTE
                // (capa AboveLens: el BlackHoleLensSystem las pinta tras compositar
                // la distorsión → la lente queda DETRÁS de los efectos del agujero).
                // v5.94: el ANILLO DE FOTONES pulsante se ELIMINÓ — los anillos
                // ahora viven SOLO en la explosión final (onda cromática del
                // OnKill), petición del usuario.
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS ===
            // Radio 450 que crece +60% durante la secuencia de muerte (el área de
            // efecto se expande con la hinchazón final hasta la evaporación).
            // v5.91 — Fuerza 2.6: el agujero negro tiene 10× MÁS fuerza de
            // atracción que el sol (SunProjectile usa 2.6/10 = 0.26, e incluso
            // su pico de carga ×4 = 1.04 queda por debajo).
            float gravityRadius = 450f * (1f + expansion * 0.6f);
            const float gravityStrength = 2.6f; // sol: 0.26 (10 veces menos)
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * gravityStrength;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === v5.94 — AURA DE DAÑO DEL CAMPO DE FUERZA (límites mejorados) ===
            // Petición del usuario: "debe mejorar los limites de su daño en
            // area". Antes el daño solo existía en el hitbox de contacto (96px)
            // y en la onda final; ahora el CAMPO DE FUERZA quema a todo enemigo
            // atrapado dentro de su burbuja: cada 0.5 s, radio = escudo ×1.3
            // (crece con la hinchaZón de la muerte, +60% en la fase final) —
            // la gravedad los arrastra hacia dentro y el campo los desgasta.
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                VisualsTime > 0f && VisualsTime % 30f == 0f)
            {
                float auraRadius = ShieldRadius * 1.3f;
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;
                    // Empuje suave hacia el centro (el campo los exprime hacia
                    // el horizonte) + daño del aura.
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        npc.velocity += toCenter * 0.8f;
                    }
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 0f, DamageClass.Magic);
                }
            }

            // === ILUMINACIÓN PULSANTE (naranja incandescente del disco) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.15f * pulse));
        }

        /// <summary>
        /// v5.94 — Radio actual del campo de fuerza (px): 2.2× el horizonte,
        /// con la escala PRE-colapso durante la evaporación (localAI[1]) para
        /// que el escudo no encoja con el agujero mientras muere.
        /// </summary>
        private float ShieldRadius
        {
            get
            {
                if (Projectile.localAI[1] > 4f)
                    return Projectile.localAI[1];
                return 0.3f * Projectile.width * Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS
        // ------------------------------------------------------------------

        /// <summary>
        /// v5.90 — MATERIA ABSORBIDA (dusts): polvo dorado/ámbar cayendo en
        /// espiral hacia el horizonte. Reemplaza a la antigua succión
        /// multicolor (violeta/cian/magenta) y al humo púrpura — sin nada
        /// morado: la paleta es de materia incandescente (ámbar → blanco).
        /// </summary>
        private void SpawnAbsorbedDusts()
        {
            // v5.91 — Aceleración de la materia en la secuencia de muerte:
            // el polvo dorado también se acelera hacia el centro cuando el
            // agujero se hincha para explotar (hasta ×3 en la evaporación).
            float deathSpeedBoost = 1f;
            if (Projectile.timeLeft <= 90f)
                deathSpeedBoost = 1f + (90f - Projectile.timeLeft) / 90f * 2f;

            for (int i = 0; i < 2; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 2f) +
                              Main.rand.NextFloat(-0.25f, 0.25f);
                float dist = 100f + 55f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                // Más rápido cuanto más cerca del horizonte (y acelerado por
                // la muerte: la materia se precipita a la implosión).
                float speed = (4f + 4f * (1f - dist / 155f)) * deathSpeedBoost;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    // RotateTowards: convierte la caída radial en espiral
                    float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                    velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                    // v5.90 — materia cálida devorada: ámbar/oro que se vuelve
                    // incandescente al acercarse al horizonte (sin morados).
                    Color color;
                    if (dist < 60f)
                    {
                        color = new Color(255, 245, 205); // blanco incandescente al borde
                    }
                    else
                    {
                        color = Main.rand.Next(3) switch
                        {
                            0 => new Color(255, 190, 90),  // ámbar
                            1 => new Color(255, 220, 130), // oro claro
                            _ => new Color(255, 160, 60),  // brasa
                        };
                    }

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        velocity, 150, color, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                    d.scale = Main.rand.NextFloat(0.8f, 1.4f);
                }
            }
        }

        /// <summary>Chispas doradas encantadas capturadas por el campo gravitatorio.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(140f, 220f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    vel, 255, new Color(255, 230, 150), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>
        /// Efecto gravitacional sobre el polvo del ambiente: los dusts cercanos
        /// son atraídos hacia el horizonte de sucesos, como si el agujero devorase el entorno.
        /// </summary>
        private void AttractNearbyDust()
        {
            // v5.85: radio de devoración ampliado (190 → 260): el agujero devora
            // el polvo del entorno en un área mucho mayor.
            float radius = 260f;
            for (int i = 0; i < Main.maxDust; i++)
            {
                Dust d = Main.dust[i];
                if (!d.active || d.noGravity) continue;
                Vector2 toCenter = Projectile.Center - d.position;
                float dist = toCenter.Length();
                if (dist > radius || dist < 4f) continue;
                float strength = (1f - dist / radius) * 0.35f;
                toCenter.Normalize();
                // Componente tangencial sutil → espiral
                Vector2 pull = toCenter * strength + new Vector2(-toCenter.Y, toCenter.X) * strength * 0.35f;
                d.velocity += pull;
            }
        }

        // ------------------------------------------------------------------
        //  v5.90 — PARTÍCULAS DE LA LIBRERÍA PROPIA (capa AboveLens:
        //  ENCIMA de la lente gravitacional — la pinta el BlackHoleLensSystem
        //  tras compositar la distorsión, de modo que la lente quede detrás
        //  de los efectos del agujero negro)
        // ------------------------------------------------------------------

        /// <summary>
        /// v5.91 — MATERIA ABSORBIDA por el agujero negro: estelas TrailGlow
        /// cálidas (ámbar → blanco incandescente) que nacen en el borde del
        /// campo gravitatorio ORIENTADAS HACIA EL CENTRO — su eje largo apunta
        /// al centro del agujero y su velocidad inicial es RADIAL hacia dentro
        /// (con una componente tangencial sutil que mantiene la caída en
        /// espiral) — y usan el componente PullTo para acelerar hacia el
        /// centro, MURIENDO al llegar al horizonte (devoradas). Sin morados:
        /// la materia se calienta al caer, como un disco de acreción real.
        /// v5.91: durante la secuencia de muerte la fuerza de succión se
        /// dispara (PullToGlobalBoost — la materia ACELERA hacia el centro
        /// justo en el momento de explotar, petición del usuario).
        /// </summary>
        private void SpawnLibraryAbsorbedMatter()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(95f, 165f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                // v5.91 — Velocidad inicial RADIAL HACIA EL CENTRO (la
                // orientación que pidió el usuario): el vector unitario que
                // apunta al centro es -(cos θ, sin θ). Una componente
                // tangencial SUTIL mantiene la espiral de infalling, pero el
                // movimiento dominante es "hacia el centro del agujero".
                Vector2 inward = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 velocity = inward * Main.rand.NextFloat(1.8f, 2.8f) +
                                   tangent * Main.rand.NextFloat(0.25f, 0.5f);

                // Materia fría lejana → incandescente al rozar el horizonte.
                Color start = new Color(255, 185, 95, 190);
                Color end = new Color(255, 250, 235, 235);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = velocity,
                    Scale = new Vector2(1.35f, 0.42f), // estela estirada al movimiento
                    // v5.91 — el eje largo de la estela APUNTA AL CENTRO
                    // (angle + π: la dirección de la caída, "ubicadas en
                    // dirección hacia el centro del agujero").
                    Rotation = angle + MathHelper.Pi,
                    PackedColor = ParticleManager.PackColor(start),
                    PackedStartColor = ParticleManager.PackColor(start),
                    PackedEndColor = ParticleManager.PackColor(end),
                    TimeLeft = 75,
                    Duration = 75,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.AboveLens,
                };
                // PullTo: UserData0/1 = centro del agujero, UserData3 = fuerza
                // de succión (aceleración por tick hacia el centro). La fuerza
                // EFECTIVA se multiplica por PullToGlobalBoost en la librería
                // (×6 durante la evaporación: aceleración al explotar).
                p.UserData0 = Projectile.Center.X;
                p.UserData1 = Projectile.Center.Y;
                p.UserData3 = 0.09f;
                p.EnableComponent(ComponentFlag.PullTo);
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>
        /// Disco de acreción con estelas TrailGlow orbitando: usa el componente Orbit
        /// (centro/radio/velocidad angular) con la rotación sincronizada al mismo
        /// ritmo (RotationSpeed = angVel) para que las estelas queden siempre
        /// alineadas tangencialmente, como materia caliente arremolinada.
        /// </summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                float radius = Main.rand.NextFloat(26f, 46f) * MathHelper.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.22f; // rad/tick — el disco gira rápido

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(1.8f, 0.45f), // estirada tangencialmente (TrailGlow 32x8)
                    Rotation = angle + MathHelper.PiOver2, // alineada a la tangente
                    RotationSpeed = angVel, // sincronizada con la órbita → siempre tangencial
                    PackedColor = ParticleManager.PackColor(new Color(255, 190, 90, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 210, 120, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 80, 20, 40)),
                    TimeLeft = 48,
                    Duration = 48,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AboveLens,
                };
                // Orbit: UserData0/1 = centro, UserData2 = velocidad angular, UserData3 = radio
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

        /// <summary>
        /// v5.94 — ELIMINADO: el anillo de fotones pulsante (Ring con ScaleUp
        /// cada 36 ticks) era un anillo visible DURANTE TODA la vida del
        /// agujero; con la textura HD (1024px) además quedó 16× más grande
        /// (1.15×1024 = 1178px de diámetro). Los anillos ahora solo existen
        /// en la explosión final (ondas del OnKill) — petición del usuario:
        /// "solo deben salir al final".
        /// </summary>

        // ------------------------------------------------------------------
        //  RENDER
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // v5.86 — LA LENTE VA DETRÁS DEL AGUJERO NEGRO: con la lente activa el
            // núcleo NO se dibuja en el pase del mundo (quedaría dentro de
            // screenTarget y la distorsión lo deformaría). El BlackHoleLensSystem
            // lo pinta ENCIMA de la lente llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            DrawCoreVisuals(Projectile, true);

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja el núcleo completo del agujero negro (halo + RealBlackHoleShader
        /// + refuerzo del horizonte de sucesos). Compartido entre el pase del
        /// mundo (PreDraw, con endActiveBatch=true) y el pase posterior a la lente
        /// (BlackHoleLensSystem, con endActiveBatch=false: el batch llega cerrado).
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p, bool endActiveBatch)
        {
            if (!_shaderFailed && _shader == null)
            {
                try
                {
                    _shader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RealBlackHoleShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;

                // === 1. HALO CÁLIDO EXTERIOR (aura de brasa de fondo) ===
                // v5.90 — era púrpura (60,20,90): ahora ámbar profundo, en la
                // misma familia cálida del disco de acreción.
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float haloPulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(80, 36, 14, 40) * haloPulse * p.scale, 0f,
                    glowTex.Size() * 0.5f, 3.2f * p.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                if (_shader != null && _shader.Value != null)
                {
                    // === 2. REALBLACKHOLESHADER — lensing gravitacional de 75 pasos ===
                    Effect shader = _shader.Value;

                    // v5.90 — CANVAS QUE CRECE CON LA ESCALA (fix del corte por
                    // los lados): antes el canvas era FIJO (256px) y el zoom
                    // interno crecía con la escala → al hincharse para morir
                    // (scale hasta 1.6) el disco de acreción cruzaba el borde
                    // del canvas y quedaba CORTADO con líneas verticales duras.
                    // Ahora el zoom es CONSTANTE (la cobertura del shader en
                    // unidades del mundo no cambia) y el canvas escala con
                    // p.scale: el agujero entero crece en pantalla sin recorte.
                    // El radio del disco lleva un tope (min(scale,1)) para que
                    // el toro NUNCA cruce el borde del canvas a ninguna escala.
                    float targetSize = 256f;
                    float canvasPx = targetSize * Math.Max(p.scale, 0.08f);
                    float zoomBase = p.width / targetSize * 2f;

                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(p.velocity.Y * -0.022f + 1f, 0f, p.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * zoomBase);
                    shader.Parameters["accretionDiskRadius"].SetValue(Math.Min(p.scale, 1f) * 0.4f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // FireNoiseB como textura de ruido del disco de acreción (s1)
                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    // InvisiblePixel como canvas (s0)
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/InvisiblePixel").Value;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        pixel.Size() * 0.5f, canvasPx, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();

                    // === 3. REFUERZO DEL EVENT HORIZON ===
                    // radio del horizonte en píxeles = blackHoleRadius * zoom * (canvas / 2)
                    float eventHorizonPx = 0.3f * zoomBase * canvasPx * 0.5f;
                    if (eventHorizonPx > 2f)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        float horizonScale = (eventHorizonPx * 2.15f) / glowTex.Width;
                        Main.spriteBatch.Draw(glowTex, drawPos, null,
                            new Color(0, 0, 0, 215), 0f, glowTex.Size() * 0.5f,
                            horizonScale, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                    }
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(p, drawPos);
                }

                // v5.95 — SIN campo de fuerza en vida: el escudo Perlin/
                // ForceField VANILLA ya no envuelve al agujero durante su
                // existencia — ES la onda expansiva de la explosión (petición
                // del usuario: "el campo de fuerza debe ser usado como onda
                // expansiva"). Lo dibuja CosmicShockwaveProjectile.DrawWaveVisual
                // al morir (burbuja que cabalga el frente expandiéndose).
            }
            catch
            {
                // v5.89 — cierre defensivo SOLO en el path de error: si la
                // excepción interrumpió un Begin a medias, lo cerramos aquí
                // (si el batch ya estaba cerrado, el End lanza y se ignora —
                // caso raro y registrado una sola vez por tML, no cada frame).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// v5.95 — ELIMINADO DrawForceField (escudo en vida): el campo de fuerza
        /// ya NO se dibuja alrededor del agujero durante su vida — es la ONDA
        /// EXPANSIVA de la explosión (petición del usuario). La burbuja Perlin/
        /// ForceField la dibuja CosmicShockwaveProjectile.DrawWaveVisual al
        /// morir el agujero: parte del radio del escudo y cabalga el frente.
        /// </summary>

        /// <summary>
        /// v5.94 - Radio del campo de fuerza (px): 2.2x el horizonte, con la
        /// escala PRE-colapso durante la evaporacion (localAI[1]) para que el
        /// escudo no encoja mientras el agujero muere. Compartido por el
        /// dibujado, el aura de dano y el OnKill (radio de la burbuja final).
        /// </summary>
        internal static float GetShieldRadius(Projectile p)
        {
            if (p.localAI[1] > 4f)
                return p.localAI[1];
            return 0.3f * p.width * Math.Max(p.scale, 0.08f) * ShieldRadiusMult;
        }


        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            // v5.89 — el End defensivo se hizo SUMAMENTE costoso: como el path
            // normal deja el batch CERRADO (todas las capas están balanceadas
            // Begin→End), el try{End} disparaba una InvalidOperationException
            // capturada CADA FRAME (tML la registra como "Excepción silenciosa"
            // vía su handler de first-chance exceptions). Ahora el cierre
            // defensivo SOLO ocurre en el path de error (catch de DrawCoreVisuals),
            // donde de verdad puede haber un Begin interrumpido que cerrar.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Dibujado manual de respaldo (vórtice + anillo de fotones + aberración cromática).</summary>
        private static void DrawFallback(Projectile p, Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
            float s = p.scale;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200), p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            // Disco trasero (anillo de Einstein)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0f, 4f * s), null,
                new Color(200, 50, 0, 100), -p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f * s, 0f), null,
                new Color(255, 230, 150, 130), p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            Main.spriteBatch.End();

            // Event horizon
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f, glowTex.Size() * 0.5f,
                0.8f * s, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // Anillo de fotones + aberración cromática
            // v5.94 — escalas BASADAS EN RADIO (la textura Ring pasó de 64px a
            // 1024px en v5.93: la escala fija 0.6 dibujaba un anillo de 614px
            // en vez de los ~38px del horizonte). Radio visible = 1.3× el
            // horizonte (compensado por el núcleo del Ring a 0.92).
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            float horizonPx = 0.3f * p.width * Math.Max(p.scale, 0.08f);
            float photonR = horizonPx * 1.3f * pulse / 0.92f;
            float photonScale = photonR / (ringTex.Width * 0.5f);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f * s, 0f), null,
                new Color(255, 0, 0, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f * s, 0f), null,
                new Color(0, 100, 255, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // v5.90: micro-colapso de la librería sobre el objetivo (paleta cálida)
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(255, 170, 80), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(255, 220, 160, 170), 22);

            // Implosión: 50 partículas cálidas convergiendo en espiral
            for (int i = 0; i < 50; i++)
            {
                float angle = (MathHelper.TwoPi / 50) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 140f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 7f + tangent * 4f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 200, new Color(255, 200, 120), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 GoldFlame radiales
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos encantados
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // ================================================================
            //  v5.90 — LA ÚNICA EXPLOSIÓN CROMÁTICA
            // ================================================================
            // El agujero terminó de evaporarse: toda la energía acumulada se
            // libera en UNA SOLA onda expansiva cromática (estilo 0) con
            // aberración cromática REAL — los canales R/G/B del anillo van
            // separados radialmente — que además distorsiona el fondo a su
            // paso (se registra como fuente del BlackHoleLensSystem) y HACE
            // DAÑO a cada NPC cuando el frente lo alcanza. Las 4 ondas de la
            // v5.86 (1 intermedia + 3 inversas escalonadas) se eliminaron.
            // La genera la máquina dueña del agujero (en MP el NewProjectile del
            // owner-client se sincroniza con el resto; patrón v5.86 del t-90).
            if (Projectile.owner == Main.myPlayer)
            {
                int waveIdx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,                                      // edad: sin retardo
                    CosmicShockwaveProjectile.StyleChromatic,
                    420f);                                   // radio máximo (v5.94: 620 → 420)
                // v5.95 — Radio de la burbuja del campo de fuerza: la onda ES el
                // escudo destruido — la burbuja Perlin/ForceField parte de ESTE
                // radio y cabalga el frente expandiéndose (petición del usuario:
                // "el campo de fuerza debe ser usado como onda expansiva").
                // v5.95 FIX CRÍTICO: v5.94 lo escribía en ai[3] — un índice que
                // NO EXISTE (Projectile.ai solo tiene 3 ranuras en tModLoader)
                // → IndexOutOfRangeException (el error del client.log del
                // usuario: la burbuja jamás se dibujó y este OnKill abortaba
                // antes de sonidos/dusts/temblor). Ahora viaja en localAI[0]
                // (parámetro visual de cliente, lo fija el OnKill local) con
                // fallback determinista (ai[2]×0.22) para clientes remotos.
                if (waveIdx >= 0 && waveIdx < Main.maxProjectiles)
                {
                    float deathR = GetShieldRadius(Projectile);
                    if (deathR < 10f) deathR = 63f; // escala 1 típica
                    Main.projectile[waveIdx].localAI[0] = deathR;
                }
            }

            // v5.91 — reset del boost de succión: la materia absorbida ya no
            // acelera (el agujero explotó; la librería vuelve a su fuerza base).
            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            // Estruendo de la liberación final (todas las máquinas)
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 6f, 9, 18, 0.4f,
                    "AethonBlackHoleFinalBlast"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // === PRESETS DE LA LIBRERÍA — colapso gravitatorio completo ===
            // (v5.90: paleta cálida — antes los presets eran violeta)
            // Implosión: la materia visible colapsa hacia la singularidad
            ParticlePresets.Implosion(Projectile.Center, 165f, 46,
                new Color(255, 180, 90), 26);
            // Explosión: liberación de energía del colapso
            ParticlePresets.Explosion(Projectile.Center, 130f, 28,
                new Color(255, 240, 200), new Color(255, 120, 40), 40);

            // === COLAPSO FINAL: implosión + explosión (dusts, capa frontal) ===
            // Implosión: partículas cálidas convergiendo
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                float dist = Main.rand.NextFloat(100f, 170f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        toCenter * 9f + tangent * 5f, 220, new Color(255, 205, 120), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo de GoldFlame
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 230, new Color(255, 200, 100), 1.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }

    /// <summary>Extensiones vectoriales para las partículas en espiral.</summary>
    public static class Vector2Extensions
    {
        /// <summary>Rota el vector hacia el ángulo objetivo como máximo maxStep radianes, conservando la magnitud.</summary>
        public static Vector2 RotateTowards(this Vector2 current, float targetAngle, float maxStep)
        {
            float currentAngle = (float)Math.Atan2(current.Y, current.X);
            float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
            if (Math.Abs(diff) <= maxStep)
                return new Vector2((float)Math.Cos(targetAngle), (float)Math.Sin(targetAngle)) * current.Length();
            float newAngle = currentAngle + Math.Sign(diff) * maxStep;
            return new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * current.Length();
        }
    }
}
