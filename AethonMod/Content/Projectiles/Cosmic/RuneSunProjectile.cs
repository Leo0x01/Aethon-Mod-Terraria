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
    /// RuneSunProjectile — v6.19 — LA FAMILIA DE LOS SOLES RÚNICOS.
    ///
    /// UN solo proyectil parametrizado: `ai[0]` = copia (1..20). La copia
    /// N lleva N ANILLOS RÚNICOS en planos orbitales distintos con GIROS
    /// ALTERNOS, y cada nivel añade una mejora más (ver RuneSunRenderer).
    ///
    /// EL SOL ORIGINAL NO SE TOCA: SunProjectile queda 100% intacto — esta
    /// familia es una construcción nueva que hereda la TÉCNICA visual del
    /// sol (disco SunShader + glow coronal creciente) por re-implementación,
    /// no por dependencia.
    ///
    /// FÍSICA (el patrón probado del sol, calibrado por copia):
    ///   · Vida: 10 s + 0.2 s por copia (la 10 dura 12 s).
    ///   · Pop elástico de aparición + deriva lenta con freno.
    ///   · Persigue suavemente a la presa más cercana (tope 3 px/t).
    ///   · Aura de daño cada 10 ticks (45% del daño, radio creciente).
    ///   · GIGANTE FINAL (últimos 1.5 s): se hincha ×1.5, enrojece y su
    ///     daño sube ×1.5 — la versión compacta del ciclo del sol.
    ///   · ONKill = UNA SOLA NOVA RÚNICA (la lección v5.97): onda nova de
    ///     lente + AoE del núcleo + explosión de runas de eco — TODO en
    ///     una explosión, escalado por copia.
    /// </summary>
    public class RuneSunProjectile : ModProjectile
    {
        /// <summary>Vida base en ticks (10 s) + 12 por copia.</summary>
        private const int BaseLifetime = 600;

        /// <summary>El daño base registrado al nacer (para el ramp de la gigante).</summary>
        private float BaseDamage
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        /// <summary>Edad visual en ticks (solo cliente — animaciones).</summary>
        private float _age;

        /// <summary>Vida total de ESTA copia (ticks) — se fija al nacer.</summary>
        private float _lifetime = BaseLifetime;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 104;
            Projectile.height = 104;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BaseLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            int tier = Tier;
            // v6.26 — LA MEJORA MAYOR DE LA VEINTE: +90 ticks de vida (15,3 s
            // totales) para que el GRAN SELLADO tenga tiempo de sonar entero.
            _lifetime = BaseLifetime + 12 * (tier - 1) + (tier >= 20 ? 90 : 0);
            Projectile.timeLeft = (int)_lifetime;
            // ai[1] = semilla determinista del disparo (visual).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            BaseDamage = Projectile.damage;
        }

        /// <summary>La copia (1..20), clampeada.</summary>
        private int Tier => System.Math.Clamp((int)Projectile.ai[0], 1, RuneSunRenderer.MaxTier);

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida (para el glow coronal).</summary>
        private float LifeT => MathHelper.Clamp(_age / Math.Max(_lifetime, 1f), 0f, 1f);

        /// <summary>0..1 de la GIGANTE FINAL (últimos 90 ticks).</summary>
        private float RedGiant
        {
            get
            {
                float t = 1f - Projectile.timeLeft / (float)RuneSunRenderer.RedGiantTicks;
                return MathHelper.Clamp(t, 0f, 1f);
            }
        }

        public override void AI()
        {
            _age += 1f;

            // === v6.51.5 — EL NACIMIENTO EN TRES ACTOS (la letra: «el
            //     nacimiento del sol runico en la bruma es como demasiado
            //     espontaneo, necesita una mejor animacion de
            //     nacimiento»): el sol YA NO aparece de golpe — primero
            //     SE ENCIENDE PEQUEÑO dentro de su nube (45 ticks — la
            //     bruma de su nacimiento lo esconde y se sopla hacia
            //     afuera en corona para revelarlo) y DESPUÉS REVIENTA a
            //     su tamaño de verdad con el pop elástico de siempre
            //     (EL ESTALLIDO, ticks 45→135).
            //     (v6.26 — LA VEINTE nace GRANDE: sistema ×1.30 — el GRAN
            //     SELLADO merece un cielo entero para él solo.) ===
            float nacimiento;
            if (_age < 45f)
            {
                // ACTO 3a — EL ENCENDIDO: una brasa pequeña que crece
                // dentro de su nube (la corona de expulsión la abre).
                nacimiento = 0.34f + 0.12f * (_age / 45f);
            }
            else
            {
                // ACTO 3b — EL ESTALLIDO: el pop elástico de siempre
                // (la firma de la casa — ahora sí, a su tamaño de verdad).
                nacimiento = MathHelper.Lerp(0.46f, 1f,
                    ElasticOut(Utils.GetLerpValue(45f, 135f, _age, true)));
            }
            Projectile.scale = nacimiento * (Tier >= 20 ? 1.30f : 1f);

            // === v6.51.3 → v6.51.4 — LA BRUMA DORADA DEL NACIMIENTO (la
            //     letra: «agrega un poco de bruma en las animacions de
            //     nacimiento… con la bruma del color correspondiente» — y
            //     la de la .51.4: «teniamos ya una buena libreria de
            //     brumas… solo se debe cubrir con bruma, nada geometria
            //     plana»): el sol estrena su vida EXHALANDO SU NUBE
            //     DORADA — BrumaNacimientoFX (BrumaFX de la casa, humo
            //     fBm que se desgarra — ya no polvos sueltos) + unas
            //     ascuas doradas que derivan arriba mientras arraiga. ===
            if (_age == 1f && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner,
                    1f,                       // SOL: la nube DORADA
                    MathHelper.Clamp(Tier / 20f, 0.7f, 1.5f)); // el GRAN SELLADO respira más
            }
            if (_age < 75f && _age % 9f < 1f && Main.netMode != NetmodeID.Server)
            {
                float angB = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejosB = Main.rand.NextFloat(34f, 96f) * Math.Max(Projectile.scale, 0.2f);
                Vector2 posBruma = Projectile.Center +
                    new Vector2(MathF.Cos(angB), MathF.Sin(angB) * 0.82f) * lejosB;
                Dust m = Dust.NewDustPerfect(posBruma, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                                Main.rand.NextFloat(-1.3f, -0.3f)),
                    80, new Color(255, 216, 130), Main.rand.NextFloat(1.4f, 2.2f));
                m.noGravity = true;
                m.fadeIn = 0f;
            }

            // === LA GIGANTE FINAL: se hincha ×1.5 y el daño sube ×1.5
            // (v6.22: la copia 20 — EL GRAN SELLADO — se hincha ×1.75) ===
            float rg = RedGiant;
            if (rg > 0f)
            {
                float ease = rg * rg * (3f - 2f * rg);
                float swell = Tier >= 20 ? 0.75f : 0.50f;
                Projectile.scale *= 1f + swell * ease;

                float baseDmg = BaseDamage > 0f ? BaseDamage : Projectile.damage;
                BaseDamage = baseDmg;
                int newDmg = Math.Max(1, (int)(baseDmg * (1f + swell * rg)));
                if (newDmg != Projectile.damage)
                    Projectile.damage = newDmg;

                int sz = Math.Max(16, (int)(104f * Projectile.scale));   // v6.31: 92→104 (BodyPx 46→52)
                if (sz != Projectile.width)
                    Projectile.Resize(sz, sz);
            }
            else if (BaseDamage <= 0f)
            {
                BaseDamage = Projectile.damage;
            }

            // === EL AURA DE DAÑO (cada 10 ticks — 45% del daño;
            //     LA VEINTE ARDE MÁS: 55% — la Mejora Mayor) — v6.50 —
            //     GolpeMotor: el cauce del motor, resuelto en el cliente
            //     dueño; la quemadura sigue siendo autoridad. ===
            if (_age > 30f && _age % 10f == 0f && Projectile.scale > 0.25f)
            {
                float auraRadius = RuneSunRenderer.BodyPx * Projectile.scale *
                                   (1.75f + 0.55f * LifeT);
                // Las copias altas tienen anillos más anchos: el aura crece
                // con la copia (la 10 abraza su 3er anillo).
                auraRadius *= 1f + 0.06f * (Tier - 1);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (Tier >= 20 ? 0.55f : 0.45f)));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 2f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.AddBuff(BuffID.OnFire, rg > 0f ? 600 : 300);
                }
            }

            // === MOVIMIENTO: deriva lenta + persecución suave ===
            // v6.51.2 — EL SEGUIMIENTO QUE CRECE CON EL NIVEL DEL
            //     GRIMORIO (la letra: «por defecto un seguimiento simple
            //     y lento después de ser lanzado por los minions, y a
            //     partir de nivel 100 del grimorio el seguimiento aumenta
            //     un poquito más hasta tener un seguimiento muy agresivo
            //     a nivel 1000 del grimorio»).
            Projectile.velocity *= 0.97f;

            int nivelLibro = Content.Systems.WeaponScaling.NivelDelDuenio(Projectile);
            float homAcel = Content.Systems.WeaponScaling.HomingAcel(nivelLibro);
            float homTope = Content.Systems.WeaponScaling.HomingTope(nivelLibro);

            NPC prey = FindNearestEnemy(560f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 60f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 0.9f * homAcel;
                float tope = 3f * homTope;
                if (Projectile.velocity.Length() > tope)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * tope;
            }

            // === LUZ DEL SOL (cálida, crece con la gigante) ===
            float lightR = (0.85f + 0.55f * rg) * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.9f * lightR, 0.62f * lightR, 0.24f * lightR);
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

        /// <summary>v6.30 — TODO SOL QUEMA (petición del usuario): el golpe de
        /// CONTACTO del sol también prende — OnFire 10 s en el cuerpo del sol.</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // MP-seguro: AddBuff corre en quien tenga autoridad sobre el NPC.
            try { target.AddBuff(BuffID.OnFire, 600); } catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10 — la lección de
            //  los agujeros): durante PreDraw el batch de tML está ABIERTO;
            //  hay que CERRARLO antes de que el renderer llame a Begin()
            //  con sus propios estados. Sin esto: InvalidOperationException
            //  "Begin has been called before calling End" → el sol NUNCA
            //  se pinta (proyectil invisible) — bug v6.19 corregido.
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                RuneSunRenderer.Draw(Projectile, Tier, LifeT, RedGiant, Seed);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles) — el mismo
        /// contrato validado de la familia de agujeros negros.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            int tier = Tier;

            // ============================================================
            //  LA NOVA RÚNICA — UNA SOLA EXPLOSIÓN (la lección v5.97):
            //  una onda nova (StyleNova, la de la familia del sol) + AoE
            //  del núcleo + la RÁFAGA DE RUNAS DE ECO — escalado por copia.
            //  v6.50 — GolpeMotor: el AoE del núcleo va por el cauce del
            //  motor (cliente dueño); la onda y la quemadura siguen
            //  siendo autoridad.
            // ============================================================
            // v6.26 — LA NOVA DEL SELLADO: la VEINTE revienta ×1.6 con
            // el radio más grande del arma alguna (766 px).
            bool supremo = tier >= 20;
            int novaDamage = Math.Max(1, (int)(Projectile.damage * (supremo ? 1.6f : 1.25f)));
            float novaRadius = 380f + 14f * (tier - 1) + (supremo ? 120f : 0f);

            // v6.50.2 — FIX (nova fantasma en MP): el gate netMode dejaba
            // el spawn en el SERVER con owner=índice de jugador ≠ myPlayer(255)
            // → NewProjectile JAMÁS lo difundía (solo difunde owner==myPlayer,
            // verificado en el IL) y el cliente dueño tampoco lo engendraba:
            // visual Y daño de la nova perdidos para cualquier jugador no-host.
            // El patrón de los agujeros (BlackHoleProjectile): lo engendra la
            // máquina DUEÑA y el propio NewProjectile sincroniza.
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    novaDamage, 0f, Projectile.owner,
                    0f,                                  // sin retardo — TODO sucede YA
                    CosmicShockwaveProjectile.StyleNova,
                    novaRadius);                         // radio escalado por copia
            }

            // AoE del núcleo (el epicentro de la MISMA explosión).
            float coreR = 260f + 12f * (tier - 1) + (supremo ? 90f : 0f);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < coreR)
                {
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, novaDamage,
                        Projectile.knockBack, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.AddBuff(BuffID.OnFire, 600);
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === v6.51.5 — LA NOVA DE BRUMA (la letra: «a la explosion
            //     final del sol runico, en vez de ser un sprite de un
            //     circulo de fuego, no seria mejor que sea un circulo de
            //     bruma»): EL CÍRCULO DE FUEGO MURIÓ (los dos anillos de
            //     polvos de llama y los pulsos circulares) — la nova
            //     ahora EXHALA SU CÍRCULO DE BRUMA DORADA: DOCE cúmulos
            //     que se expanden, se desgarran y se disuelven (BrumaFX
            //     de la casa — BrumaNacimientoFX modo 3, con el radio
            //     REAL de esta nova). Las runas de eco y la onda de
            //     lente SIGUEN viviendo abajo. ===
            Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<BrumaNacimientoFX>(),
                0, 0f, Projectile.owner, 3f,
                novaRadius / 340f);   // la escala del círculo (la VEINTE respira más)

            // === LA CHISPA DE LA NOVA (el fogonazo central que enciende
            //     el círculo — la primera luz del estallido). ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.Explosion(Projectile.Center, 150f * scale, 36,
                new Color(255, 245, 200), new Color(255, 170, 60), 44);

            // === LAS RUNAS DE ECO: la nova escupe glifos de luz ===
            int echoRunes = 12 + 2 * tier;
            for (int i = 0; i < echoRunes; i++)
            {
                float angle = (MathHelper.TwoPi / echoRunes) * i +
                              Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    outward * Main.rand.NextFloat(5f, 11f) * scale,
                    255, new Color(255, 235, 170), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // (v6.51.5 — EL FUEGO DE LA NOVA MURIÓ: los dos anillos de
            //  polvos de llama ya no existen — el círculo es de BRUMA.)

            // === Screenshake coordinado (LA VEINTE sacude más: 12) ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), tier >= 20 ? 12f : 8f, 12, 18, 0.45f,
                    "AethonRuneSunNova"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
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
