using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Projectiles.Aethon
{
    /// <summary>
    /// FRAGMENTO DE AETHON — v6.50.98 — EL DISPARO DEL GRIMORIO SELLADO.
    /// La letra del usuario: «te dare el codigo para un arma nueva de
    /// prueba, recuerda darcela al jugador, este sera el proyectil y su
    /// efecto» — el código venía con 4 errores de compilación (DustID.
    /// GoldFlare no existe en esta tML — lección .96 otra vez; Kill se
    /// llama OnKill en esta tML — lección .94; y el DisplayName vive en
    /// el hjson, no en SetDefault) y TODOS están muertos aquí.
    ///
    /// EL EFECTO, la letra del usuario: una ASTILLA DORADA del sello —
    /// vuela (8 frames que respiran), chispea oro (GoldFlame, el sustito
    /// de casa del GoldFlare imposible) con algún rayo violeta, gira
    /// apuntando adonde vuela, OPIONALMENTE persigue (ai[1] &gt; 0), y al
    /// morder: aplica OBSERVADO (debuff + un stack en el sistema), al
    /// morir abre LA APERTURA DE AETHON (la explosión), y si el enemigo
    /// ya llevaba 5 stacks («Conocido») el golpe es CRÍTICO GARANTIZADO
    /// con +50% de daño.
    ///
    /// La segunda tanda de errores del borrador (los que solo el BUILD
    /// REAL puede cazar — el oráculo de la casa): las firmas de golpe de
    /// ESTA tML son OnHitNPC(NPC, HitInfo, int) y ModifyHitNPC(NPC, ref
    /// HitModifiers) — el crit garantizado es modifiers.SetCrit() y el
    /// +50% es FinalDamage *= 1.5f (el patrón OcasoBurst); y el source
    /// desde un proyectil es GetSource_FromThis() (el
    /// GetProjectileSource_FromThis del borrador no existe aquí).
    /// </summary>
    public class FragmentoAethon : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 8 frames de animación, se reproducen en loop (respiración)
            Main.projFrames[Projectile.type] = 8;
            // (El DisplayName vive en el hjson de localización — la casa.)
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;            // hitbox == frame del strip
            Projectile.height = 24;
            Projectile.friendly = true;       // daña enemigos
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;         // atraviesa 1 enemigo
            Projectile.timeLeft = 120;        // 2 segundos de vida
            Projectile.alpha = 0;
            Projectile.light = 0.8f;          // la luz base (blanca)
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;      // va 2× rápido
        }

        // ===== LA IA DEL PROYECTIL =====
        public override void AI()
        {
            // --- ANIMACIÓN: ciclar los 8 frames (la astilla respira) ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)   // cada 5 ticks cambia frame
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
                if (Projectile.frame >= 8)
                    Projectile.frame = 0;     // loop
            }

            // --- LA LUZ DORADA (la casa: AddLight de color latiendo con
            //     la animación — Projectile.light solo da luz BLANCA) ---
            float late = 0.30f + 0.22f * (float)System.Math.Sin(Projectile.frameCounter * 0.35f);
            Lighting.AddLight(Projectile.Center, 0.52f * late, 0.42f * late, 0.12f * late);

            // --- EFECTO: chispas doradas (GoldFlame — GoldFlare NO existe
            //     en esta tML; el tinte de casa lo vuelve ORO de verdad) ---
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.GoldFlame,
                    0f, -1f,
                    100, new Color(255, 200, 100), 0.8f
                );
                dust.noGravity = true;
                dust.fadeIn = 0f;
                dust.velocity *= 0.5f;
            }

            // --- EFECTO: rayo violeta ocasional ---
            if (Main.rand.NextBool(10))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PurpleTorch,
                    Vector2.Zero,
                    150, new Color(170, 90, 255), 1.2f
                );
                dust.noGravity = true;
                dust.scale = 1.2f;
            }

            // --- ROTACIÓN: la astilla apunta hacia donde vuela (el strip
            //     es VERTICAL — por eso el +PiOver2) ---
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // --- HOMING (opcional — el arma lo enciende con ai[1] = 1) ---
            if (Projectile.ai[1] > 0)
            {
                NPC target = null;
                float maxDist = 300f;
                // Main.ActiveNPCs (la casa .95): solo los vivos, sin indexar
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (npc.friendly)
                        continue;             // la chusma del pueblo no es presa
                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist < maxDist)
                    {
                        target = npc;
                        maxDist = dist;
                    }
                }

                if (target != null)
                {
                    // dirigirse hacia el enemigo suavemente (sin perder velocidad)
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float speed = Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * speed, 0.1f);
                }
            }
        }

        // ===== AL GOLPEAR UN ENEMIGO =====
        // (la firma de ESTA tML: HitInfo + daño hecho — el borrador traía
        // la firma vieja (NPC, int, float, bool) que no compila aquí)
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL DEBUFF "Observado" (5 segundos — la letra del usuario lo
            // había dejado comentado; su diagrama de flujo lo pide: la
            // chispa visual del stack mientras Aethon mira)
            target.AddBuff(ModContent.BuffType<Observado>(), 300);

            // EL STACK en el sistema (quien lleva la cuenta del «Conocido»)
            ModContent.GetInstance<SistemaObservado>().AddStack(target.whoAmI);

            if (Main.netMode != NetmodeID.Server)     // el polvo y el sonido son del cliente
            {
                // EFECTO: chispas doradas al impactar
                for (int i = 0; i < 10; i++)
                {
                    Dust dust = Dust.NewDustDirect(
                        target.position,
                        target.width,
                        target.height,
                        DustID.GoldFlame,
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f),
                        100, new Color(255, 208, 120), 1.5f
                    );
                    dust.noGravity = true;
                }

                // SONIDO de impacto (el golpe mágico de la letra del usuario)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, Projectile.position);
            }
        }

        // ===== AL MORIR (impacto, muro o timeout) =====
        // (Kill se llama OnKill en esta tML — lección .94: CS0619)
        public override void OnKill(int timeLeft)
        {
            // GENERAR LA EXPLOSIÓN: LA APERTURA DE AETHON
            // (el source de ESTA tML es GetSource_FromThis — el
            // GetProjectileSource_FromThis del borrador no existe)
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ExplosionAethon>(),
                (int)(Projectile.damage * 0.5f),   // 50% del daño
                0f,
                Projectile.owner
            );

            // POLVO al morir: estallido de oro
            for (int i = 0; i < 15; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(10, 10),
                    20, 20,
                    DustID.GoldFlame,
                    Main.rand.NextFloat(-3f, 3f),
                    Main.rand.NextFloat(-3f, 3f),
                    100, new Color(255, 200, 100), 1.5f
                );
                dust.noGravity = true;
            }
        }

        // ===== CRÍTICO GARANTIZADO si el enemigo ya es «Conocido» =====
        // (ModifyHitNPC corre ANTES de OnHitNPC: el stack de ESTE golpe no
        // cuenta para su propio crítico — hacen falta 5 golpes PREVIOS)
        // (la firma de ESTA tML: ref HitModifiers — el borrador traía la
        // firma vieja (ref int/float/bool/int) que no compila aquí)
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var sistema = ModContent.GetInstance<SistemaObservado>();
            if (sistema.GetStacks(target.whoAmI) >= 5)
            {
                modifiers.SetCrit();               // CRÍTICO GARANTIZADO (sin dados)
                modifiers.FinalDamage *= 1.5f;     // +50% daño extra (patrón OcasoBurst)

                if (Main.netMode != NetmodeID.Server)
                {
                    // el efecto visual de la «revelación»: Aethon ya lo conocía
                    for (int i = 0; i < 20; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(
                            target.Center,
                            DustID.GoldFlame,
                            Vector2.UnitX.RotateRandom(MathHelper.TwoPi) * 4f,
                            220, new Color(255, 218, 94), 2f
                        );
                        dust.noGravity = true;
                        dust.scale = 2f;
                    }
                }
            }
        }
    }
}
