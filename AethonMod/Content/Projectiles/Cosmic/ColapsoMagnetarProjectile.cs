using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// ColapsoMagnetarProjectile — v6.31 — LA ESTRELLA DEL COLAPSO del
    /// bastón "El Colapso del Magnetar" (research/v631,
    /// INFORME_STAR_TOMB_DRAGON_WORD.md ficha 2 + §6.2 — el freno
    /// magnético y el starquake, destilados a la casa como arma
    /// independiente: la estrella hace EL CICLO COMPLETO ella sola).
    ///
    /// LA CRONOLOGÍA DE LA ESTRELLA (~212 ticks):
    ///   · EL VUELO (26 ticks): sale RÁPIDA hacia el cursor y FRENA
    ///     geométricamente (×0.885/tick — v₀ = distancia/7.375 aterriza
    ///     EXACTO; el objetivo viaja en ai[0..1] del paquete de spawn).
    ///   · LA VIDA (12 ticks): gira acelerando con DOS HACES CORTOS
    ///     opuestos barriendo por el eje magnético desalineado (ai[2]).
    ///   · EL FRENO (55 ticks): la carga sube 1/55 por tick en
    ///     localAI[0]; el spin cae ×0.15, los haces se encogen ×0.55,
    ///     la estrella TIEMBLA, chispas de frenado saltan del cuerpo y
    ///     una jaula de anillos se ENREDA alrededor. Un anillo fino se
    ///     CONTRAE telegrafiando el estallido; el tono sube de pitch.
    ///   · EL STARQUAKE (34 ticks): ANILLO EXPANSIVO de radio
    ///     (250+330·carga)·easeOutCubic (hasta 580 px) que SOLO DAÑA
    ///     CON EL FRENTE (banda delgada barrida tick a tick — el vacío
    ///     central ya barrido NO golpea dos veces): daño
    ///     base·(1.4+1.1·carga) → 308..550, Quemadura Cósmica 20 s.
    ///   · EL OVERCLOCK (~85 ticks): la superviviente gira a ×4.7 con
    ///     haces ×1.55 y daño de haces ×1.6, y muere en un destello.
    ///
    /// Determinismo MP: objetivo y fase magnética viajan en ai[]; la
    /// cronología entera deriva de la edad (misma en todas las
    /// máquinas); la semilla visual es Projectile.identity; el daño
    /// v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
    /// on-hit y sync MP del propio motor); el visual solo cliente (PreDraw) y SIN Main.rand
    /// (Hash01 + identity).
    /// </summary>
    public class ColapsoMagnetarProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int VueloTicks = 26;
        private const int VidaTicks = 12;
        private const int FrenoTicks = 55;
        private const int QuakeTicks = 34;
        private const int OverclockTicks = 85;

        /// <summary>La vida total del ciclo completo.</summary>
        public const int VidaTotal = VueloTicks + VidaTicks + FrenoTicks + QuakeTicks + OverclockTicks;

        /// <summary>El freno geométrico del vuelo (pierde 11.5% de v por tick).</summary>
        private const float FrenoK = 0.885f;

        /// <summary>La serie 0.885¹..0.885²⁶: lo que viaja en unidades de v₀.</summary>
        public const float TravelFactor = 7.375f;

        // === LOS HACES (dos, cortos, opuestos) ===
        private const float HazLargo = 160f;
        private const float HazColision = 30f;

        /// <summary>i-frames PROPIOS por objetivo (un golpe de haz cada 10 ticks).</summary>
        private const int IframesHaz = 10;

        /// <summary>i-frames del frente del starquake (la casa del original: 8).</summary>
        private const int IframesQuake = 8;

        /// <summary>Daño de cada tick de haz (el starquake es la cosecha, no esto).</summary>
        private const float DañoHaz = 0.25f;

        /// <summary>El multiplicador de daño de haz en overclock (§ ficha 2: ×1.6).</summary>
        private const float DañoHazOverclock = DañoHaz * 1.6f;

        // === EL STARQUAKE (los números de la ficha 2) ===
        /// <summary>Radio del frente con carga 0.</summary>
        private const float QuakeRadioMin = 250f;

        /// <summary>Radio del frente con carga 1 (el máximo del anillo).</summary>
        private const float QuakeRadioMax = 580f;

        /// <summary>El semiancho de la banda del FRENTE (±20 px).</summary>
        private const float FrenteSemiancho = 20f;

        /// <summary>Duración del debuff de la casa al golpear el frente (20 s).</summary>
        private const int QuemaduraTicks = 1200;

        // === LA PALETA (el violeta magnetar medido del renderer de la casa) ===
        private static readonly Color ColorVioleta = new(168, 120, 255);  // violeta magnetar
        private static readonly Color ColorProfundo = new(108, 70, 220);  // violeta profundo
        private static readonly Color ColorBlanco = new(238, 224, 255);   // blanco-violeta caliente
        private static readonly Color ColorEje = new(128, 96, 190);       // eje apagado

        private static readonly Color[] Paleta = { ColorVioleta, ColorProfundo, ColorBlanco, ColorEje };

        private enum Fase { Vuelo, Vida, Freno, Quake, Overclock }

        private Fase _fase = Fase.Vuelo;
        private float _age;
        private float _faseAge;
        private bool _nacio;
        private float _spinPhase;
        private float _magPhase;
        private Vector2 _target;

        /// <summary>El radio del frente del quake en el tick ANTERIOR (la banda barrida).</summary>
        private float _prevReach;

        /// <summary>i-frames por objetivo: whoAmI del NPC → tick del último golpe.</summary>
        private readonly Dictionary<int, int> _ultimoGolpe = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTotal + 2;
            Projectile.ignoreWater = true;
            // Un cadáver estelar no choca con el mundo.
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red: la misma en todas las máquinas).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 41);

        /// <summary>LA CARGA del freno (0..1) — vive en localAI[0] como manda la ficha.</summary>
        private float Carga => MathHelper.Clamp(Projectile.localAI[0], 0f, 1f);

        /// <summary>El poder del starquake: la carga con la que se disparó.</summary>
        private float Poder => Carga;

        // ==============================================================
        //  LA CRONOLOGÍA — AI
        // ==============================================================

        public override void AI()
        {
            _age += 1f;
            _faseAge += 1f;

            // === EL NACIMIENTO: clavar el punto objetivo (viaja en ai[]) ===
            if (!_nacio)
            {
                _nacio = true;
                _target = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                _magPhase = Projectile.ai[2];
                if (_target == Vector2.Zero) _target = Projectile.Center;
            }

            switch (_fase)
            {
                case Fase.Vuelo: AIVuelo(); break;
                case Fase.Vida: AIVida(); break;
                case Fase.Freno: AIFreno(); break;
                case Fase.Quake: AIQuake(); break;
                case Fase.Overclock: AIOverclock(); break;
            }
        }

        private void CambiarFase(Fase nueva)
        {
            _fase = nueva;
            _faseAge = 0f;
        }

        /// <summary>EL VUELO — el freno geométrico: la estrella muere parada en el cursor.</summary>
        private void AIVuelo()
        {
            Projectile.velocity *= FrenoK;
            Lighting.AddLight(Projectile.Center, 0.34f, 0.28f, 0.62f);

            if (Main.netMode != NetmodeID.Server && _age % 2f == 0f)
            {
                RiftLib.ChispasAnomalia(Projectile.Center, 2, Paleta, Seed + (int)_age, out ParticleData[] motas);
                Spawn(motas);
            }

            if (_faseAge >= VueloTicks)
            {
                // ========================================================
                //  EL ATERRIZAJE — la estrella se CLAVA donde apuntabas
                // ========================================================
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = _target;
                CambiarFase(Fase.Vida);

                if (Main.netMode != NetmodeID.Server)
                {
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9.WithPitchOffset(-0.55f), Projectile.Center); }
                    catch { }
                    OndaLib.Kick(1.6f, 6);
                    RiftLib.ChispasAnomalia(Projectile.Center, 8, Paleta, Seed + 77, out ParticleData[] motas);
                    Spawn(motas);
                }
            }
        }

        /// <summary>LA VIDA — el spin-up con los dos haces cortos ya encendidos.</summary>
        private void AIVida()
        {
            float t = MathHelper.Clamp(_faseAge / VidaTicks, 0f, 1f);
            _spinPhase += MathHelper.Lerp(0.05f, 0.07f, t * t);

            if (_faseAge > 4f)
                GolpearHaces(1f, 1f, DañoHaz);

            LuzVida(1f);

            if (_faseAge >= VidaTicks)
            {
                // ========================================================
                //  EL FRENO EMPIEZA — el tono grave de la carga.
                // ========================================================
                CambiarFase(Fase.Freno);
                if (Main.netMode != NetmodeID.Server)
                {
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item77.WithPitchOffset(-0.35f), Projectile.Center); }
                    catch { }
                }
            }
        }

        /// <summary>EL FRENO — la carga 0→1 en localAI[0]: el spin cae ×0.15,
        /// los haces se encogen ×0.55, la estrella tiembla y supura chispas.</summary>
        private void AIFreno()
        {
            float carga = MathHelper.Clamp(_faseAge / FrenoTicks, 0f, 1f);
            Projectile.localAI[0] = carga;

            // El giro casi se detiene (×0.15 de su ritmo vivo).
            _spinPhase += 0.07f * 0.15f;

            // Los haces SEGUIRÁN picando, pero cortos (×0.55 al llenarse).
            GolpearHaces(MathHelper.Lerp(1f, 0.55f, carga), 1f, DañoHaz);

            // La luz se ENFRÍA y se CONCENTRA al frenar.
            float frio = 1f - 0.45f * carga;
            Lighting.AddLight(Projectile.Center,
                0.34f * frio + 0.10f * carga, 0.26f * frio, 0.62f * frio + 0.16f * carga);

            // CHISPAS DE FRENADO: escapan del cuerpo agrietado (cada 2 ticks
            // si la carga ya aprieta — como manda la ficha).
            if (Main.netMode != NetmodeID.Server && carga > 0.25f && _age % 2f == 0f)
            {
                RiftLib.ChispasAnomalia(Projectile.Center, 3, Paleta, Seed + (int)_age, out ParticleData[] motas);
                Spawn(motas);
            }

            // EL TONO QUE SUBE: el sonido grave gana pitch con la carga.
            if (Main.netMode != NetmodeID.Server && _faseAge % 10f == 0f && _faseAge > 0f)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item77.WithPitchOffset(MathHelper.Lerp(-0.35f, 0.30f, carga)),
                        Projectile.Center);
                }
                catch { }
            }

            if (carga >= 1f)
            {
                // ========================================================
                //  EL STARQUAKE — la corteza no aguanta más.
                // ========================================================
                _prevReach = 0f;
                CambiarFase(Fase.Quake);

                if (Main.netMode != NetmodeID.Server)
                {
                    float p = Poder;
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62.WithPitchOffset(-0.45f), Projectile.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94.WithPitchOffset(0.35f + 0.3f * p), Projectile.Center);
                    }
                    catch { }
                    // Sacudida 4+4·p + el destello de la sobre-exposición.
                    OndaLib.Kick(4f + 4f * p, 10);
                    OndaLib.Flash(ColorBlanco, 0.30f, 8, Projectile.Center);
                }
            }
        }

        /// <summary>EL STARQUAKE — el anillo expansivo que SOLO golpea con el frente.</summary>
        private void AIQuake()
        {
            // La estrella expulsada gira suelta mientras el frente avanza.
            _spinPhase += 0.02f;

            float progress = QuakeProgress();
            float reach = ReachQuake(progress);

            // EL FRENTE BARRIDO: de donde estaba el anillo el tick pasado a
            // donde está ahora (±20 px de banda) — el interior ya barrido NO
            // vuelve a golpear, y nada se cuela entre ticks.
            GolpearFrente(_prevReach, reach);
            _prevReach = reach;

            // La luz del frente inunda el campo.
            float brillo = FalloffQuake(progress);
            Lighting.AddLight(Projectile.Center,
                0.7f * brillo + 0.15f, 0.45f * brillo, 1.05f * brillo + 0.2f);

            if (_faseAge >= QuakeTicks)
            {
                // ========================================================
                //  EL OVERCLOCK — la superviviente se desata.
                // ========================================================
                CambiarFase(Fase.Overclock);
                if (Main.netMode != NetmodeID.Server)
                {
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94.WithPitchOffset(0.55f), Projectile.Center); }
                    catch { }
                    OndaLib.Flash(ColorVioleta, 0.16f, 6, Projectile.Center);
                }
            }
        }

        /// <summary>EL OVERCLOCK — spin ×4.7, haces ×1.55 y daño de haces ×1.6.</summary>
        private void AIOverclock()
        {
            // 0.07 × 4.7 ≈ 0.33 rad/tick — unas 3 revoluciones por segundo.
            _spinPhase += 0.07f * 4.7f;

            GolpearHaces(1.55f, 1f, DañoHazOverclock);

            // El final de vida se apaga con el destello de despedida.
            float fade = MathHelper.Clamp(_faseAge / OverclockTicks, 0f, 1f);
            Lighting.AddLight(Projectile.Center,
                (0.5f + 0.4f * fade) * 0.9f, 0.4f * fade + 0.2f, 1.0f);

            if (_faseAge >= OverclockTicks)
                Projectile.Kill();
        }

        // ==============================================================
        //  EL DAÑO (v6.50 — GolpeMotor: el cauce del motor)
        // ==============================================================

        /// <summary>LOS DOS HACES CORTOS: líneas-polares opuestas (largo ×mult)
        /// barriendo por el eje magnético. i-frames propios de 10 ticks.
        /// v6.50 — GolpeMotor (el cauce del motor); la quemadura es de servidor.</summary>
        private void GolpearHaces(float largoMult, float _, float dmgMult)
        {
            Vector2 eje = new(MathF.Cos(_spinPhase + _magPhase), MathF.Sin(_spinPhase + _magPhase));
            int dmg = Math.Max(1, (int)(Projectile.damage * dmgMult));
            float largo = HazLargo * largoMult;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                bool tocado = RiftLib.LineaToca(Projectile.Center, eje, largo, HazColision, npc.Hitbox)
                           || RiftLib.LineaToca(Projectile.Center, -eje, largo, HazColision, npc.Hitbox);
                if (!tocado) continue;

                if (_ultimoGolpe.TryGetValue(npc.whoAmI, out int ultimo) && _age - ultimo < IframesHaz)
                    continue;
                _ultimoGolpe[npc.whoAmI] = (int)_age;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                // El plasma del haz reaviva la quemadura (servidor).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 240); } catch { }
            }
        }

        /// <summary>
        /// EL FRENTE DEL STARQUAKE: solo golpea la CORONA que el anillo acaba
        /// de barrer este tick (de reachPrev a reach, ±20 px de margen) — el
        /// vacío central ya juzgado NO recibe un segundo golpe. Daño
        /// base·(1.4+1.1·poder) → 308..550. v6.50 — GolpeMotor (el cauce
        /// del motor: crítica real, varianza, on-hit y sync MP del propio
        /// motor); la quemadura es de servidor.
        /// </summary>
        private void GolpearFrente(float reachPrev, float reach)
        {
            float poder = Poder;
            int dmg = Math.Max(1, (int)(Projectile.damage * (1.4f + 1.1f * poder)));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                // La banda del frente, ensanchada un cuarto del cuerpo (los
                // gigantes también tienen corteza cerca del frente).
                float margen = FrenteSemiancho + Math.Max(npc.width, npc.height) * 0.25f;
                bool enFrente = dist >= reachPrev - margen && dist <= reach + margen;
                if (!enFrente) continue;

                if (_ultimoGolpe.TryGetValue(npc.whoAmI, out int ultimo) && _age - ultimo < IframesQuake)
                    continue;
                _ultimoGolpe[npc.whoAmI] = (int)_age;

                // El knockback del original: empuja HACIA FUERA del frente
                // (la dirección la computa el cauce del motor).
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, Projectile.knockBack * 2f, true);
                // La reconexión magnética chamusca durante 20 s (servidor).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), QuemaduraTicks); } catch { }
            }
        }

        // ==============================================================
        //  LAS CURVAS DEL QUAKE (las de la ficha 2)
        // ==============================================================

        /// <summary>El avance normalizado del starquake (0..1).</summary>
        private float QuakeProgress() => MathHelper.Clamp(_faseAge / QuakeTicks, 0f, 1f);

        /// <summary>Reach = (250+330·poder)·easeOutCubic(progress) — hasta 580 px.</summary>
        private float ReachQuake(float progress)
        {
            float ease = 1f - MathF.Pow(1f - progress, 3f);
            return MathHelper.Lerp(QuakeRadioMin, QuakeRadioMax, Poder) * ease;
        }

        /// <summary>El fade del quake: pleno hasta el 35% de vida.</summary>
        private static float FalloffQuake(float progress)
            => 1f - (progress * progress); // 1−t²: pleno al abrir, apagándose al final

        /// <summary>
        /// El progreso REMAPEADO para OndaLib.Shock: su curva interna es
        /// 1−(1−t)^2.2; invirtiéndola sobre el easeOutCubic del daño, el
        /// frente VISUAL coincide EXACTO con el frente que golpea.
        /// </summary>
        private static float ProgresoVisual(float easeOutCubic)
            => MathHelper.Clamp(1f - MathF.Pow(1f - easeOutCubic, 1f / 2.2f), 0f, 1f);

        // ==============================================================
        //  LA MUERTE — el destello final
        // ==============================================================

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // La superviviente agotada colapsa en un último destello.
            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(-0.2f), Projectile.Center); }
            catch { }
            OndaLib.Flash(ColorVioleta, 0.20f, 6, Projectile.Center);
            OndaLib.Kick(2.2f, 6);
            RiftLib.ChispasAnomalia(Projectile.Center, 14, Paleta, Seed + 99, out ParticleData[] motas);
            Spawn(motas);
        }

        private static void Spawn(ParticleData[] motas)
        {
            if (motas == null) return;
            for (int i = 0; i < motas.Length; i++)
                ParticleManager.Spawn(motas[i]);
        }

        // ==============================================================
        //  EL VISUAL — PreDraw (contrato de batch v6.10)
        // ==============================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try { DrawMagnetar(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla(); // v6.50.11 — curación (lote siempre abierto y vanilla)
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del pase
        /// de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private void LuzVida(float _)
        {
            // La luz violeta late con el faro del giro.
            float faro = Faro();
            Lighting.AddLight(Projectile.Center,
                0.34f + 0.22f * faro, 0.24f + 0.16f * faro, 0.62f + 0.28f * faro);
        }

        /// <summary>LA ENVOLVENTE DEL FARO: |cos(spin)|⁵ — pico dos veces por vuelta.</summary>
        private float Faro()
        {
            float c = MathF.Abs(MathF.Cos(_spinPhase));
            return c * c * c * c * c;
        }

        // ==============================================================
        //  EL DESENLACE VISUAL — lote aditivo + la técnica del sol
        // ==============================================================

        private void DrawMagnetar()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            float carga = Carga;

            // LA POSICIÓN: el centro, más el TEMBLOR del freno (visual puro,
            // determinista por tick — el daño usa el centro quieto).
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            if (_fase == Fase.Freno)
            {
                Vector2 temblor = new(
                    VFXCore.Hash01(seed, (int)_age, 3) - 0.5f,
                    VFXCore.Hash01(seed, (int)_age, 4) - 0.5f);
                drawPos += temblor * (5f * carga);
            }

            float faro = Faro();
            // El cuerpo late con el faro; en el freno se ENCOGE apretado.
            float R = 30f * (1f + 0.06f * faro) * (0.75f + 0.25f * MathHelper.Clamp(_age / 6f, 0f, 1f));
            if (_fase == Fase.Freno) R *= 1f - 0.18f * carga;
            if (_fase == Fase.Overclock) R *= 1f + 0.15f * MathHelper.Clamp(_faseAge / 10f, 0f, 1f);

            // El fade de vida (los últimos 30 ticks del overclock).
            float fade = _fase == Fase.Overclock
                ? MathHelper.Clamp((OverclockTicks - _faseAge) / 30f, 0f, 1f)
                : 1f;

            // ============================================================
            //  EL LOTE ADITIVO — magnetosfera, haces, jaula y starquake
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // 1. EL BLOOM LATIENDO (el resplandor de la estrella moribunda).
            LumenLib.Bloom(Main.spriteBatch, drawPos, R * (2.0f + 0.8f * faro),
                ColorVioleta, (0.40f + 0.25f * faro) * fade, 3);

            // 2. LA ESTELA DE VUELO (el plasma frenando).
            if (_fase == Fase.Vuelo && Projectile.velocity.LengthSquared() > 1f)
            {
                Vector2 vdir = Vector2.Normalize(Projectile.velocity);
                float v = Projectile.velocity.Length();
                LumenLib.Ray(Main.spriteBatch, drawPos, -vdir, 30f + v * 2.2f, R * 1.1f,
                    ColorProfundo, 0.35f, 0.6f);
            }

            // 3. EL ANILLO DE EMISIÓN (el ecuador + las perlas cayendo).
            if (_fase != Fase.Quake)
                DrawAnilloEmision(Main.spriteBatch, drawPos, R, time, seed, fade, carga);

            // 4. LOS DOS HACES CORTOS (viven, se encogen en el freno y se
            //    disparan en el overclock; en el quake ya los expulsó).
            if (_fase != Fase.Quake)
                DrawHazes(Main.spriteBatch, drawPos, R, faro, time, fade, carga);

            // 5. LA JAULA Y EL TELEGRAFO DEL FRENO.
            if (_fase == Fase.Freno)
            {
                DrawJaulaFreno(Main.spriteBatch, drawPos, R, time, seed, carga);
                DrawTelegrafo(Main.spriteBatch, drawPos, R, carga);
            }

            // 6. EL STARQUAKE: sobre-exposición, rayos de reconexión, los
            //    frentes del anillo y sus estrellas.
            if (_fase == Fase.Quake)
                DrawStarquake(Main.spriteBatch, drawPos, R, time, seed);

            // 7. EL OVERCLOCK: el aura desatada.
            if (_fase == Fase.Overclock)
            {
                LumenLib.Bloom(Main.spriteBatch, drawPos, R * (2.6f + 0.9f * faro),
                    ColorBlanco, 0.45f * fade, 2);
            }

            Main.spriteBatch.End();

            // ============================================================
            //  8. EL CUERPO — LA TÉCNICA DEL SOL DE LA CASA (DrawSunBody
            //     gestiona SUS lotes → DESPUÉS del aditivo): el disco
            //     violeta del magnetar (la paleta medida del renderer de
            //     la casa) girando con spinMul 1.1.
            // ============================================================
            float eje = _spinPhase + _magPhase;
            float cuerpo = fade * (_fase == Fase.Quake ? 0.55f : 0.82f + 0.18f * faro);
            RuneSunRenderer.DrawSunBody(drawPos, R, eje, time,
                new Color(238, 224, 255),   // main — blanco-violeta
                new Color(128, 96, 190),    // darker — violeta apagado
                new Color(70, 25, 130),     // accent — violeta profundo
                new Color(168, 120, 255),   // backHot — halo magnetar
                new Color(108, 70, 220),    // backRed — halo frío
                new Color(205, 175, 255),   // shine — destello
                1.1f, cuerpo);
        }

        /// <summary>EL ANILLO DE EMISIÓN: el aro fino solidario al giro + las
        /// perlas orbitando (deterministas).</summary>
        private void DrawAnilloEmision(SpriteBatch batch, Vector2 drawPos, float R,
            float time, int seed, float alpha, float carga)
        {
            float rot = _spinPhase + _magPhase + MathHelper.PiOver2;
            float escala = 1f - 0.30f * carga;   // el freno COMPRIME el ecuador

            Vector2 size = VFXCore.RingQuadSize(R * 2.3f * escala);
            batch.Draw(VFXCore.Ring, drawPos, null, Tint(ColorEje, 0.38f * alpha), rot,
                VFXCore.Ring.Size() * 0.5f, size / VFXCore.Ring.Size(), SpriteEffects.None, 0f);

            const int Perlas = 4;
            for (int i = 0; i < Perlas; i++)
            {
                float t = i / (float)Perlas * MathHelper.TwoPi - time * 1.3f;
                Vector2 local = new Vector2(MathF.Cos(t), MathF.Sin(t) * 0.32f) * (R * 2.3f * escala);
                Vector2 pearl = drawPos + local.RotatedBy(rot);
                float tw = 0.55f + 0.45f * MathF.Sin(time * 5f + i * 2.4f + seed);
                Vector2 scale = new Vector2(R * 0.30f, R * 0.30f) / VFXCore.SoftGlow.Size();
                batch.Draw(VFXCore.SoftGlow, pearl, null, Tint(ColorBlanco, 0.50f * tw * alpha), 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS DOS HAZES CORTOS: el par polar opuesto (160 px de base)
        /// — en el freno se encogen ×0.55 y adelgazan; en el overclock se
        /// estiran ×1.55 y arden.</summary>
        private void DrawHazes(SpriteBatch batch, Vector2 drawPos, float R,
            float faro, float time, float fade, float carga)
        {
            float largoMult = _fase switch
            {
                Fase.Freno => MathHelper.Lerp(1f, 0.55f, carga),
                Fase.Overclock => 1.55f,
                _ => 1f,
            };
            // El fadeIn del nacimiento de los haces.
            float beamFade = _fase == Fase.Vida
                ? MathHelper.Clamp(_faseAge / 6f, 0f, 1f)
                : 1f;
            // En el freno los haces ADELGAZAN (la energía se retira).
            float grosor = _fase == Fase.Freno ? 1f - 0.40f * carga : 1f;
            float brillo = _fase == Fase.Overclock ? 1.5f : 1f;

            float axisAngle = _spinPhase + _magPhase;
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = new(
                    MathF.Cos(axisAngle + side * MathHelper.Pi),
                    MathF.Sin(axisAngle + side * MathHelper.Pi));

                // a) EL ABANICO tenue tras el haz.
                for (int i = 0; i < 3; i++)
                {
                    float spread = (i / 2f - 0.5f) * 0.30f;
                    Vector2 coneDir = dir.RotatedBy(spread);
                    LumenLib.Ray(batch, drawPos + coneDir * (R * 0.7f), coneDir,
                        HazLargo * largoMult * 0.38f * beamFade, R * 0.85f * grosor,
                        ColorProfundo, 0.13f * beamFade * fade * brillo, 0.4f + 0.3f * faro);
                }

                // b) EL HAZ PRINCIPAL, corto y cortante.
                LumenLib.Ray(batch, drawPos + dir * (R * 0.55f), dir,
                    HazLargo * largoMult * (0.35f + 0.65f * beamFade),
                    R * (1.35f + 0.55f * faro) * grosor,
                    ColorVioleta, (0.50f + 0.28f * faro) * beamFade * fade * brillo, faro);

                // c) LA BOCA POLAR.
                LumenLib.Bloom(batch, drawPos + dir * (R * 0.55f), R * 1.3f,
                    ColorBlanco, 0.50f * beamFade * fade * brillo, 2);
            }
        }

        /// <summary>LA JAULA DEL FRENO: DOS anillos contra-rotando que se
        /// COMPRIMEN y ENREDAN sobre la estrella (ruido azimutal ×(1+0.8·carga))
        /// — las líneas de campo estrangulando la corteza.</summary>
        private void DrawJaulaFreno(SpriteBatch batch, Vector2 drawPos, float R,
            float time, int seed, float carga)
        {
            for (int j = 0; j < 2; j++)
            {
                float rot = time * (j == 0 ? 1.6f : -1.2f) + j * MathHelper.PiOver2;
                float radio = R * (2.9f - 1.1f * carga);          // se comprime
                float enredo = 1f + 0.8f * carga;                  // se enreda

                Vector2 size = VFXCore.RingQuadSize(radio);
                batch.Draw(VFXCore.Ring, drawPos, null, Tint(ColorVioleta, 0.30f * carga), rot,
                    VFXCore.Ring.Size() * 0.5f, size / VFXCore.Ring.Size(), SpriteEffects.None, 0f);

                // Los NUDOS de la jaula: perlas tambaleándose fuera de fase.
                const int Nudos = 6;
                for (int i = 0; i < Nudos; i++)
                {
                    float jitter = (VFXCore.Hash01(seed, i, j) - 0.5f) * enredo;
                    float t = i / (float)Nudos * MathHelper.TwoPi + time * 0.9f * (j == 0 ? 1f : -1f) + jitter;
                    float rr = radio * (0.86f + 0.28f * VFXCore.Hash01(seed, i, j + 9));
                    Vector2 nodo = drawPos + new Vector2(MathF.Cos(t), MathF.Sin(t) * (0.5f + 0.2f * jitter)) * rr;
                    Vector2 scale = new Vector2(R * 0.26f, R * 0.26f) / VFXCore.SoftGlow.Size();
                    batch.Draw(VFXCore.SoftGlow, nodo, null,
                        Tint(ColorBlanco, 0.40f * carga), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>EL TELEGRAFO: un anillo fino CONTRAYÉNDOSE sobre la estrella
        /// mientras la carga sube — el jugador LEE cuánto falta.</summary>
        private void DrawTelegrafo(SpriteBatch batch, Vector2 drawPos, float R, float carga)
        {
            float radio = MathHelper.Lerp(150f, R * 1.4f, carga * carga);
            Vector2 size = VFXCore.RingQuadSize(radio);
            Color color = Color.Lerp(ColorVioleta, ColorBlanco, carga);
            batch.Draw(VFXCore.Ring, drawPos, null, Tint(color, 0.15f + 0.45f * carga), 0f,
                VFXCore.Ring.Size() * 0.5f, size / VFXCore.Ring.Size(), SpriteEffects.None, 0f);
        }

        /// <summary>EL STARQUAKE VISTO: la sobre-exposición blanca del arranque,
        /// los rayos de reconexión en abanico, los DOS frentes concéntricos del
        /// anillo (el principal + el reflejo interior ×0.62) y las estrellas
        /// corriendo SOBRE el frente — todo determinista.</summary>
        private void DrawStarquake(SpriteBatch batch, Vector2 drawPos, float R,
            float time, int seed)
        {
            float progress = QuakeProgress();
            float poder = Poder;
            float reach = ReachQuake(progress);
            float reachMax = MathHelper.Lerp(QuakeRadioMin, QuakeRadioMax, poder);
            float fade = FalloffQuake(progress);

            // a) LA SOBRE-EXPOSICIÓN: dos ticks de blanco cegador al abrir.
            if (_faseAge < 5f)
            {
                float sobre = MathHelper.Clamp(1f - _faseAge / 5f, 0f, 1f);
                LumenLib.Bloom(batch, drawPos, R * (2.6f + 1.4f * poder) * sobre,
                    ColorBlanco, 0.65f * sobre, 3);
            }

            // b) LOS RAYOS DE RECONEXIÓN: 10+8·poder radios del centro, hot→violeta.
            if (progress < 0.45f)
            {
                int rayos = 10 + (int)(8f * poder);
                float rayFade = MathHelper.Clamp((0.45f - progress) / 0.45f, 0f, 1f) * fade;
                for (int i = 0; i < rayos; i++)
                {
                    float ang = VFXCore.Hash01(seed, i, 55) * MathHelper.TwoPi;
                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                    float largo = reachMax * (0.35f + 0.45f * VFXCore.Hash01(seed, i, 56));
                    Color c = Color.Lerp(ColorBlanco, ColorVioleta, VFXCore.Hash01(seed, i, 57));
                    LumenLib.Ray(batch, drawPos + dir * (R * 0.6f), dir, largo,
                        R * 0.9f, c, 0.22f * rayFade, 0.5f);
                }
            }

            // c) LOS FRENTES DEL ANILLO: el principal (con aberración
            //    cromática) + el reflejo interior desfasado — el progreso
            //    REMAPEADO para que el frente visual ES el frente que golpea.
            float ease = reach / Math.Max(1f, reachMax);
            float pVis = ProgresoVisual(MathHelper.Clamp(ease, 0f, 1f));
            OndaLib.Shock(batch, drawPos, pVis, reachMax, ColorVioleta,
                0.85f * fade, seed, 12f, OndaFalloff.Quadratic, true);
            OndaLib.Shock(batch, drawPos, MathHelper.Clamp(pVis - 0.12f, 0f, 1f),
                reachMax * 0.62f, ColorProfundo, 0.55f * fade, seed + 7, 8f);

            // d) LAS ESTRELLAS DEL FRENTE: 6/frame los primeros 6 ticks, luego
            //    2/frame — SIEMPRE sobre la superficie de choque.
            int estrellas = _faseAge < 6f ? 6 : 2;
            int tickSlot = (int)(_age * 0.5f);
            for (int i = 0; i < estrellas; i++)
            {
                float ang = VFXCore.Hash01(seed, i, tickSlot) * MathHelper.TwoPi;
                float rr = reach * (0.86f + 0.18f * VFXCore.Hash01(seed, i, tickSlot + 7));
                Vector2 pos = drawPos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                float tw = 0.55f + 0.45f * MathF.Sin(time * 7f + i * 2.1f + seed);
                Vector2 scale = new Vector2(22f, 22f) / VFXCore.SoftGlow.Size();
                batch.Draw(VFXCore.SoftGlow, pos, null,
                    Tint(ColorBlanco, 0.55f * tw * fade), 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.7 — REVERSIÓN AL PREMULTIPLICADO (la sonda v6.50.3
            // estaba incompleta): el pipeline REAL premultiplica los PNG al
            // cargar (ReLogic PngReader.PreMultiplyAlpha, verificado en el
            // decompilado del tML 2026.07.3.0) y el AlphaBlend de FNA es
            // (One, InvSourceAlpha) — compositing PREMULTIPLICADO, donde el
            // RGB del tinte ES la intensidad. El tinte lineal dejaba el
            // color SIN escalar en los lotes de masa (bruma fantasma
            // saturada) y sobrealimentaba los aditivos hasta ×10 (destellos
            // que inundaban la pantalla). El (RGB·f, A·f) de v6.25 es el
            // correcto para AMBOS presets de FNA.
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
