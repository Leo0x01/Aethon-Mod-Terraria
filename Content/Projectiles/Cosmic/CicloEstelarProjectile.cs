using System;
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
    /// CicloEstelarProjectile — v6.26 — EL BASTÓN DEL CICLO ESTELAR.
    ///
    /// Petición del usuario: "crea un baston que simule el ciclo de vida
    /// completo de una estrella que se convierte en super nova y luego lo
    /// que siga en su ciclo de vida".
    ///
    /// LA MÁQUINA DE ESTADOS DE LOS 5 ACTOS (vida total ~1080 ticks = 18 s,
    /// la línea de tiempo vive en CicloEstelarRenderer):
    ///
    ///   ACTO I · NEBULOSA (0..240) — la nube molecular se contrae
    ///     (escala 0.5→0.8), deriva sin presa. Daño: leve aura de FRÍO
    ///     (100 px, 25% cada 20 ticks). Sin luz apenas.
    ///
    ///   ACTO II · SECUENCIA PRINCIPAL (240..600) — LA IGNICIÓN: flash de
    ///     encendido (Kick 6 + presets) y la estrella VIVE (cuerpo ~40 px
    ///     por BodyPx). Persigue su presa (tope 3 px/t). Daño: aura
    ///     ardiente 140 px cada 10 ticks (45%) + OnFire. Luz solar cálida.
    ///
    ///   ACTO III · GIGANTE ROJA (600..810) — ENVEJECE: se hincha ×2.2
    ///     (escala smoothstep 1→2.2) y enrojece; pierde capas (el render
    ///     las suelta). Persigue LENTA (tope 1.6). Daño: aura 200 px (35%)
    ///     + knockback suave. La luz tiñe de rojo el mundo.
    ///
    ///   ACTO IV · COLAPSO + SUPERNOVA (810..870) — 15 ticks de implosión
    ///     (todo se encoge ×0.3, SILENCIO DE LUZ, anillo contráctil) → LA
    ///     SUPERNOVA: Kick 12, daño MASIVO ×2 del arma en 650 px + OnFire,
    ///     y el render pinta el estallido completo (Shock cromático ×2,
    ///     llamaradas SolarFire ×12, MultiBolt fugitivos ×8, nube
    ///     expansiva, ImpactFlash 130 px CONCENTRADO).
    ///
    ///   ACTO V · EL REMANENTE (870..1080) — "lo que siga en su ciclo de
    ///     vida": la ESTRELLA DE NEUTRONES enana (10 px) PULSA 2.5 s y se
    ///     APAGA (fade). Daño: aura pequeña 70 px pero ×1.5.
    ///
    /// LA ESTELA de todo el ciclo (EstelaLib.Track + Ribbon Comet en el
    /// renderer) cuenta la historia del camino: violeta → dorado → rojo →
    /// blanco → azul.
    ///
    /// Daño MP-seguro: v6.50 — GolpeMotor (el cauce del motor: crítica
    /// real, varianza, on-hit y sync MP del propio motor); visual solo
    /// cliente (`Main.netMode == Server`
    /// → return). Determinismo: semilla por identity.
    /// </summary>
    public class CicloEstelarProjectile : ModProjectile
    {
        // ==================================================================
        //  EL ESTADO (ai[] sincronizada + _age visual local)
        // ==================================================================

        /// <summary>La EDAD en ticks (ai[0] — se sincroniza en MP).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>El espejo visual local de la edad (animaciones).</summary>
        private float _age;

        /// <summary>Semilla determinista del disparo (ai[1]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>El daño base del arma registrado al nacer (ai[2]).</summary>
        private float BaseDamage => Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;

        /// <summary>El guard local de la nova (localAI[0] — el ciclo solo
        /// estalla UNA vez por máquina).</summary>
        private bool NovaDisparada
        {
            get => Projectile.localAI[0] > 0.5f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        /// <summary>El acto activo (1..5) según la línea de tiempo.</summary>
        private int Act => CicloEstelarRenderer.ActOf(_age);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = CicloEstelarRenderer.TotalTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // LA VIDA COMPLETA del ciclo: 1080 ticks (~18 s).
            Projectile.timeLeft = CicloEstelarRenderer.TotalTicks;
            // ai[1] = semilla determinista del disparo (visual).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            // ai[2] = el daño base del arma (el ramp de los actos lo lee).
            Projectile.ai[2] = Projectile.damage;
            _age = 0f;
        }

        // ==================================================================
        //  LA MÁQUINA DE ESTADOS — un acto, una física
        // ==================================================================

        public override void AI()
        {
            _age += 1f;
            Projectile.ai[0] = _age;   // la edad sincronizada (MP)

            int act = Act;

            // === LOS PROGRESOS de los actos que la FÍSICA necesita ===
            float a1 = Utils.GetLerpValue(0f, CicloEstelarRenderer.Act1End, _age, true);
            float a3 = Utils.GetLerpValue(CicloEstelarRenderer.Act2End, CicloEstelarRenderer.Act3End, _age, true);
            float col = Utils.GetLerpValue(CicloEstelarRenderer.Act3End, CicloEstelarRenderer.CollapseEnd, _age, true);
            float nova = Utils.GetLerpValue(CicloEstelarRenderer.CollapseEnd, CicloEstelarRenderer.NovaEnd, _age, true);
            float rem = Utils.GetLerpValue(CicloEstelarRenderer.NovaEnd, CicloEstelarRenderer.TotalTicks, _age, true);

            // ============================================================
            //  EL GUION DE LA ESCALA (el cuerpo físico de la estrella)
            // ============================================================
            float pop = ElasticOut(Utils.GetLerpValue(0f, 30f, _age, true));
            switch (act)
            {
                case 1:
                    // La NEBULOSA nace con pop y SE CONTRAE (0.5 → 0.8).
                    Projectile.scale = MathF.Max(pop * (0.50f + 0.30f * a1), 0.05f);
                    break;

                case 2:
                    // LA IGNICIÓN: el proto-núcleo salta a cuerpo pleno
                    // (0.8 → 1.0 en los primeros 20 ticks del acto).
                    Projectile.scale = 0.80f + 0.20f * Math.Min(1f,
                        (float)(_age - CicloEstelarRenderer.Act1End) / 20f);
                    break;

                case 3:
                    // LA GIGANTE ROJA: se hincha ×2.2 (smoothstep — el
                    // hinchazón del coloso que envejece).
                    {
                        float e = a3 * a3 * (3f - 2f * a3);
                        Projectile.scale = 1f + 1.2f * e;
                    }
                    break;

                case 4:
                    // EL COLAPSO: todo se encoge ×0.3 rápido (la nova
                    // congela la escala; el render pinta el estallido).
                    Projectile.scale = 2.2f * (1f - 0.70f * Math.Min(1f, col));
                    break;

                default:
                    // EL REMANENTE: la escala vuelve a 1 (el render usa su
                    // RemnantPx = 10 px directamente).
                    Projectile.scale = 1f;
                    break;
            }

            // ============================================================
            //  EL MOVIMIENTO (cada acto se mueve como lo que ES)
            // ============================================================
            switch (act)
            {
                case 1:
                    // La NUBE deriva (aún no vive — no persigue nada).
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += new Vector2(
                        MathF.Sin(_age * 0.021f + Seed * 0.7f),
                        MathF.Cos(_age * 0.017f + Seed * 1.3f)) * 0.02f;
                    if (Projectile.velocity.Length() > 0.8f)
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 0.8f;
                    break;

                case 2:
                    // LA ESTRELLA VIVE: persigue su presa (el patrón del sol).
                    Projectile.velocity *= 0.97f;
                    Perseguir(0.9f, 60f, 3f);
                    break;

                case 3:
                    // EL COLOSO: persigue LENTO (massive = perezoso).
                    Projectile.velocity *= 0.985f;
                    Perseguir(0.45f, 120f, 1.6f);
                    break;

                case 4:
                    // EL COLAPSO congela la deriva; la nova la CLAVA.
                    if (col < 1f) Projectile.velocity *= 0.75f;
                    else Projectile.velocity = Vector2.Zero;
                    break;

                default:
                    // EL CADÁVER: deriva fría, casi quieto.
                    Projectile.velocity *= 0.97f;
                    if (Projectile.velocity.Length() > 0.5f)
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 0.5f;
                    break;
            }

            // ============================================================
            //  EL AURA DE DAÑO (por acto) — v6.50: el daño va por GolpeMotor
            //  (el cauce del motor); la quemadura sigue en el servidor.
            // ============================================================
            if (act == 1 && _age > 30f && _age % 20f == 0f)
                Aura(0.25f, 100f, 0, 1.5f);            // el FRÍO leve de la nube
            else if (act == 2 && _age > 10f && _age % 10f == 0f)
                Aura(0.45f, 140f, 300, 2f);            // el ARDOR de la secuencia
            else if (act == 3 && _age % 10f == 0f)
                Aura(0.35f, 200f, 200, 3.5f);          // el horno + knockback suave
            else if (act == 5 && _age % 10f == 0f)
                Aura(0.675f, 70f, 0, 2f);              // la enana: ×1.5 del 45%

            // ============================================================
            //  LAS TRANSICIONES DEL GUION (una sola vez cada una)
            // ============================================================

            // --- LA IGNICIÓN (acto I → II): el nacimiento del sol ---
            if (MathF.Abs(_age - CicloEstelarRenderer.Act1End) < 0.5f)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Kick(6f, 14);
                    ParticlePresets.RingPulse(Projectile.Center, 170f,
                        new Color(255, 240, 200, 220), 30);
                    ParticlePresets.RingPulse(Projectile.Center, 250f,
                        new Color(255, 180, 90, 160), 40);
                    // EL POLVO de la nube despedido por el encendido.
                    for (int i = 0; i < 22; i++)
                    {
                        float ang = i / 22f * MathHelper.TwoPi;
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(2f, 6f),
                            220, new Color(216, 180, 255), 1.1f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
            }

            // --- EL INICIO DEL COLAPSO (acto III → IV): la vejez cae ---
            if (MathF.Abs(_age - CicloEstelarRenderer.Act3End) < 0.5f)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    ParticlePresets.Implosion(Projectile.Center, 230f, 30,
                        new Color(255, 140, 90), 24);
                    OndaLib.Kick(7f, 12);
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item70.WithPitchOffset(-0.55f), Projectile.Center);
            }

            // --- LA SUPERNOVA (el primer tick del acto IV-b): EL ESTALLIDO ---
            if (MathF.Abs(_age - CicloEstelarRenderer.CollapseEnd) < 0.5f && !NovaDisparada)
            {
                NovaDisparada = true;
                DispararLaNova();
            }

            // ============================================================
            //  LA ESTELA (la historia del camino — TODOS los actos)
            // ============================================================
            EstelaLib.Track(Projectile.whoAmI, 24).Push(Projectile.Center);
            if (_age % 120f == 0f) EstelaLib.PurgeTracks();

            // ============================================================
            //  LA LUZ DEL MUNDO (por acto — el ciclo tiñe el cielo)
            // ============================================================
            LuzPorActo(act, a3, col, nova, rem);

            // ============================================================
            //  EL AMBIENTE (dusts dispersos — visual SOLO cliente)
            // ============================================================
            if (Main.netMode == NetmodeID.Server) return;
            DustsPorActo(act, a1, a3);
        }

        // ==================================================================
        //  LA PERSECUCIÓN (el patrón del sol de la casa, calibrada por acto)
        // ==================================================================

        private void Perseguir(float aceleracion, float distanciaMin, float tope)
        {
            NPC prey = FindNearestEnemy(560f);
            if (prey == null) return;
            Vector2 toPrey = prey.Center - Projectile.Center;
            float len = toPrey.Length();
            if (len > distanciaMin && len > 0.01f)
                Projectile.velocity += toPrey / len * aceleracion;
            if (Projectile.velocity.Length() > tope)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * tope;
        }

        private NPC FindNearestEnemy(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        // ==================================================================
        //  EL AURA (daño MP-seguro — un multiplicador por acto)
        // ==================================================================

        /// <summary>Golpea a los enemigos en `radio` px con `mult`× el daño
        /// base del arma (+OnFire opcional y knockback propio del acto).
        /// v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
        /// on-hit y sync MP del propio motor); la quemadura es de servidor.</summary>
        private void Aura(float mult, float radio, int fuegoTicks, float knockback)
        {
            int dmg = Math.Max(1, (int)(BaseDamage * mult));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > radio) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, knockback, true);
                if (fuegoTicks > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    npc.AddBuff(BuffID.OnFire, fuegoTicks);
            }
        }

        // ==================================================================
        //  LA SUPERNOVA — el estallido total del ciclo
        // ==================================================================

        private void DispararLaNova()
        {
            // === EL DAÑO MASIVO (×2 del daño del arma en 650 px + OnFire) ===
            // v6.50 — GolpeMotor (el cauce del motor); la quemadura es de servidor.
            int novaDmg = Math.Max(1, (int)(BaseDamage * 2f));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > 650f) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, novaDmg, 8f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.AddBuff(BuffID.OnFire, 600);
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === EL SACUDÓN (Kick 12 — la casa entera tiembla) ===
            OndaLib.Kick(12f, 20);

            // === LOS PRESETS de la librería de partículas ===
            ParticlePresets.Explosion(Projectile.Center, 280f, 46,
                new Color(255, 250, 235), new Color(255, 90, 20), 50);
            ParticlePresets.RingPulse(Projectile.Center, 320f,
                new Color(255, 240, 210, 220), 34);
            ParticlePresets.RingPulse(Projectile.Center, 540f,
                new Color(255, 110, 60, 150), 50);

            // === LA EYECTA (dusts con la paleta SolarFire — la temperatura
            //     cae con la distancia: blanco → oro → granate) ===
            for (int i = 0; i < 90; i++)
            {
                float ang = i / 90f * MathHelper.TwoPi + Main.rand.NextFloat(-0.06f, 0.06f);
                float speed = Main.rand.NextFloat(5f, 15f);
                Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                Color c = PyraPalettes.Sample(PyraPalettes.SolarFire,
                    1f - 0.65f * (speed - 5f) / 10f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir * speed, 250, c, 1.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 40; i++)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(3f, 9f),
                    220, new Color(255, 150, 50), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === LOS SONIDOS (el triple estallido de la casa) ===
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item70.WithPitchOffset(-0.35f), Projectile.Center);
        }

        // ==================================================================
        //  LA LUZ DEL MUNDO (por acto)
        // ==================================================================

        private void LuzPorActo(int act, float a3, float col, float nova, float rem)
        {
            Vector2 c = Projectile.Center;
            switch (act)
            {
                case 1:
                    // SIN LUZ APENAS: el frío violeta de la nube vacía.
                    Lighting.AddLight(c, 0.10f, 0.07f, 0.20f);
                    break;

                case 2:
                    // EL SOL VIVE: cálida y plena.
                    Lighting.AddLight(c, 0.90f, 0.62f, 0.24f);
                    break;

                case 3:
                    // LA GIGANTE TIÑE DE ROJO el mundo (cálida, enorme).
                    {
                        float k = 0.55f + 0.65f * a3;
                        Lighting.AddLight(c, 1.00f * k, 0.34f * k, 0.12f * k);
                    }
                    break;

                case 4:
                    if (col < 1f)
                    {
                        // EL SILENCIO DE LUZ: el mundo se APAGA mientras la
                        // estrella cae (y la chispa azul del remanente crece).
                        float s = 1f - col;
                        Lighting.AddLight(c, 0.70f * s, 0.28f * s, 0.10f * s);
                        Lighting.AddLight(c, 0.50f * col, 0.60f * col, 0.90f * col);
                    }
                    else
                    {
                        // LA NOVA: blanca, cegadora, muriendo rápido.
                        float f = MathF.Max(0f, 1f - nova * 1.35f);
                        Lighting.AddLight(c, 1.30f * f, 1.25f * f, 1.15f * f);
                    }
                    break;

                default:
                    // EL PÚLSAR azul chispeante, apagándose con el acto.
                    {
                        float pulse = 0.5f + 0.5f * MathF.Sin(
                            Main.GlobalTimeWrappedHourly * MathHelper.TwoPi * 3.2f);
                        float fade = 1f - MathF.Max(0f, (rem - 0.715f) / 0.285f);
                        Lighting.AddLight(c,
                            (0.30f + 0.35f * pulse) * fade,
                            (0.42f + 0.35f * pulse) * fade,
                            (0.75f + 0.30f * pulse) * fade);
                    }
                    break;
            }
        }

        // ==================================================================
        //  EL AMBIENTE (dusts por acto — dispersos, solo cliente)
        // ==================================================================

        private void DustsPorActo(int act, float a1, float a3)
        {
            switch (act)
            {
                case 1:
                    // Las MOTAS de la nube cayendo (violeta frío hacia dentro).
                    if (_age % 7f == 0f && Projectile.scale > 0.1f)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Vector2 rim = Projectile.Center + new Vector2(
                            MathF.Cos(ang), MathF.Sin(ang)) * (110f * (1.1f - 0.3f * a1));
                        Dust d = Dust.NewDustPerfect(rim, DustID.Enchanted_Pink,
                            -new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.8f, 2.2f),
                            130, new Color(200, 165, 255), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    break;

                case 2:
                    // EL VIENTO SOLAR (llamas doradas subiendo del limbo).
                    if (_age % 8f == 0f)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Vector2 rim = Projectile.Center + new Vector2(
                            MathF.Cos(ang), MathF.Sin(ang)) *
                            (CicloEstelarRenderer.BodyPx * Projectile.scale);
                        Dust d = Dust.NewDustPerfect(rim, DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.6f, 1.8f),
                            190, new Color(255, 220, 130), 1.0f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    break;

                case 3:
                    // LAS BRASAS desprendiéndose del coloso.
                    if (_age % 8f == 0f)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Vector2 rim = Projectile.Center + new Vector2(
                            MathF.Cos(ang), MathF.Sin(ang)) *
                            (CicloEstelarRenderer.BodyPx * Projectile.scale * 0.75f);
                        Dust d = Dust.NewDustPerfect(rim, DustID.Torch,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.5f, 1.5f),
                            160, new Color(255, 120, 50), 1.1f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    break;

                case 5:
                    // LAS CHISPAS AZULES del cadáver pulsante.
                    if (_age % 12f == 0f)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.4f, 1.2f),
                            170, new Color(170, 210, 255), 0.7f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    break;
            }
        }

        // ==================================================================
        //  EL DIBUJO (contrato de batch v6.10 — a prueba de balas)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10 — la lección de
            //  los agujeros): durante PreDraw el batch de tML está ABIERTO;
            //  hay que CERRARLO antes de que el renderer llame a Begin()
            //  con sus propios estados. Sin esto:InvalidOperationException
            //  "Begin has been called before calling End" → la estrella
            //  NUNCA se pinta (proyectil invisible).
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                // La edad del render: el MÁXIMO entre el espejo local y la
                // ai[0] sincronizada (robusto en MP aunque el AI local
                // aún no haya arrancado en esta máquina).
                float drawAge = MathF.Max(_age, Projectile.ai[0]);
                CicloEstelarRenderer.Draw(Projectile, drawAge, Seed);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ==================================================================
        //  LA MUERTE (el fallback de la nova + el suspiro del remanente)
        // ==================================================================

        public override void OnKill(int timeLeft)
        {
            // === EL FALLBACK DEL CICLO: si el proyectil muere ANTES de su
            //     supernova (kills externos, mundos que descargan...), la
            //     nova SALE IGUAL — el ciclo no se le roba a la estrella.
            //     (El guard localAI evita el doble disparo del mismo tick.) ===
            if (!NovaDisparada && Projectile.ai[0] > 10f &&
                Projectile.ai[0] < CicloEstelarRenderer.CollapseEnd + 1f)
            {
                NovaDisparada = true;
                DispararLaNova();
                return;
            }

            // === LA MUERTE NATURAL del remanente: un suspiro azul (el
            //     cadáver se apaga en paz — visual SOLO cliente) ===
            if (Main.netMode == NetmodeID.Server) return;
            ParticlePresets.RingPulse(Projectile.Center, 90f,
                new Color(160, 200, 255, 180), 26);
            for (int i = 0; i < 10; i++)
            {
                float ang = i / 10f * MathHelper.TwoPi;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(0.8f, 2.4f),
                    150, new Color(190, 220, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item45.WithPitchOffset(0.45f), Projectile.Center);
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
