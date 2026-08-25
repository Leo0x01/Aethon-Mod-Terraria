using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// Aethon, la Luz Primordial — jefe final del mod.
    /// 5 fases (Polvo estelar → Nebulosa → Gravedad → Agujero negro → Reconocimiento).
    ///
    /// Fase 1: Espiral de pernos estelares.
    /// Fase 2: Nubes AoE que ciegan + queman.
    /// Fase 3: Gravedad invertida cada 8s.
    /// Fase 4: Agujero negro que atrae + adds.
    /// Fase 5: Aethon empuña TUS runas memorizadas contra ti.
    ///
    /// Desbloqueo: el Fragmento Génesis del jugador alcanza nivel 150.
    /// </summary>
    public class AethonBoss : ModNPC
    {
        private int Phase = 1;
        private int AttackTimer = 0;
        private int PhaseTimer = 0;
        private int GravityFlipTimer = 0;
        private int BlackHoleTimer = 0;
        private int AddSpawnTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 80;
            NPC.defense = 40;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            Music = MusicID.LunarPillar;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void AI()
        {
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    return;
                }
            }

            // Determinar fase por HP.
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int newPhase = 1;
            if (hpPct < 0.8f) newPhase = 2;
            if (hpPct < 0.6f) newPhase = 3;
            if (hpPct < 0.4f) newPhase = 4;
            if (hpPct < 0.2f) newPhase = 5;

            if (newPhase != Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
            }

            // Movimiento: perseguir al jugador manteniendo distancia.
            Vector2 toTarget = target.Center - NPC.Center;
            float dist = toTarget.Length();
            if (dist > 400f)
            {
                NPC.velocity = toTarget.SafeNormalize(Vector2.Zero) * 8f;
            }
            else
            {
                NPC.velocity *= 0.95f;
            }

            PhaseTimer++;

            // Ejecutar AI según fase.
            switch (Phase)
            {
                case 1: Phase1Stardust(target); break;
                case 2: Phase2Nebula(target); break;
                case 3: Phase3Gravity(target); break;
                case 4: Phase4BlackHole(target); break;
                case 5: Phase5Acknowledgment(target); break;
            }

            // Brillo.
            Lighting.AddLight(NPC.Center, new Vector3(0.6f, 0.4f, 0.8f));
        }

        // ====================================================================
        // FASE 1 — Polvo estelar: espiral de pernos
        // ====================================================================
        private void Phase1Stardust(Player target)
        {
            AttackTimer++;
            if (AttackTimer >= 60)
            {
                AttackTimer = 0;
                // Espiral de 8 pernos.
                for (int i = 0; i < 8; i++)
                {
                    float angle = (System.MathF.PI * 2 / 8) * i + PhaseTimer * 0.05f;
                    Vector2 vel = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle)) * 6f;
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center, vel,
                        ProjectileID.CultistBossLightningOrbArc,
                        45, 2f, Main.myPlayer);
                }
            }
        }

        // ====================================================================
        // FASE 2 — Nebulosa: nubes AoE que ciegan + queman
        // ====================================================================
        private void Phase2Nebula(Player target)
        {
            AttackTimer++;
            if (AttackTimer >= 90)
            {
                AttackTimer = 0;
                // Lanzar 3 nubes de nebulosa cerca del jugador.
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2(
                        Main.rand.NextFloat(-300, 300),
                        Main.rand.NextFloat(-300, 300));
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        pos, Vector2.Zero,
                        ProjectileID.CultistBossLightningOrbArc,
                        50, 2f, Main.myPlayer,
                        0, 180); // 3 segundos
                }
            }
        }

        // ====================================================================
        // FASE 3 — Gravedad: invierte la gravedad cada 8s
        // ====================================================================
        private void Phase3Gravity(Player target)
        {
            GravityFlipTimer++;
            if (GravityFlipTimer >= 480) // 8 segundos @ 60fps
            {
                GravityFlipTimer = 0;
                FlipGravity(target);
            }

            // También dispara pernos normales.
            AttackTimer++;
            if (AttackTimer >= 45)
            {
                AttackTimer = 0;
                Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 8f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center, vel,
                    ProjectileID.CultistBossLightningOrbArc,
                    55, 2f, Main.myPlayer);
            }
        }

        private void FlipGravity(Player player)
        {
            // Invertir gravedad del jugador por 3 segundos.
            player.gravDir = -player.gravDir;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, player.Center);
            Main.NewText("Aethon distorsiona la gravedad!", new Color(179, 136, 255));

            // Programar el reset.
            // (tModLoader no tiene timer directo; usamos un buff temporal o reset en PostUpdate.)
            // Simplificado: el jugador se recupera cuando toca el suelo.
        }

        // ====================================================================
        // FASE 4 — Agujero negro: atracción + adds
        // ====================================================================
        private void Phase4BlackHole(Player target)
        {
            BlackHoleTimer++;
            if (BlackHoleTimer >= 300) // cada 5 segundos
            {
                BlackHoleTimer = 0;
                // Spawn un agujero negro (proyectil visual que atrae).
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    target.Center + new Vector2(0, -200), Vector2.Zero,
                    ProjectileID.CultistBossLightningOrbArc, // agujero negro visual
                    0, 0f, Main.myPlayer,
                    0, 240); // 4 segundos
            }

            // Atraer al jugador hacia el centro.
            Vector2 toCenter = NPC.Center - target.Center;
            float pullStrength = 0.3f;
            target.velocity += toCenter.SafeNormalize(Vector2.Zero) * pullStrength;

            // Spawn adds (Cultists).
            AddSpawnTimer++;
            if (AddSpawnTimer >= 600) // cada 10 segundos
            {
                AddSpawnTimer = 0;
                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X + Main.rand.Next(-200, 200),
                    (int)NPC.Center.Y,
                    NPCID.CultistBossClone); // placeholder
            }
        }

        // ====================================================================
        // FASE 5 — Reconocimiento: Aethon empuña TUS runas memorizadas
        // ====================================================================
        private void Phase5Acknowledgment(Player target)
        {
            var sp = target.GetModPlayer<Players.ShardPlayer>();
            AttackTimer++;
            if (AttackTimer >= 50)
            {
                AttackTimer = 0;

                // Si el jugador tiene runas memorizadas, Aethon las usa contra él.
                if (sp != null && sp.MemorizedRunes.Count > 0)
                {
                    // Elegir una runa aleatoria y disparar su comportamiento.
                    string rune = sp.MemorizedRunes[Main.rand.Next(sp.MemorizedRunes.Count)];
                    FireRuneAttack(target, rune);
                }
                else
                {
                    // Sin runas: ataque genérico potente.
                    Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                    for (int i = -2; i <= 2; i++)
                    {
                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            NPC.Center,
                            vel.RotatedBy(i * 0.1),
                            ProjectileID.CultistBossLightningOrbArc,
                            70, 3f, Main.myPlayer);
                    }
                }
            }
        }

        /// <summary>
        /// Dispara el ataque correspondiente a una runa memorizada del jugador.
        /// Aethon refleja el poder del jugador.
        /// </summary>
        private void FireRuneAttack(Player target, string runeName)
        {
            // Simplificado: mapear nombres a proyectiles.
            int projType = runeName switch
            {
                "Último Prisma" => ProjectileID.LastPrism,
                "Destello Lunar" => ProjectileID.CultistBossLightningOrbArc,
                "Tifón de Cuchillas" => ProjectileID.Typhoon,
                "Hoja Terra" => ProjectileID.TerraBeam,
                "Ira Estelar" => ProjectileID.StarWrath,
                "Zenith" => ProjectileID.StarWrath,
                _ => ProjectileID.CultistBossLightningOrbArc,
            };
            Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center, vel, projType,
                80, 3f, Main.myPlayer);
            Main.NewText($"Aethon empuña tu {runeName}!", new Color(255, 100, 100));
        }

        private void OnPhaseChange()
        {
            // Curar un poco al cambiar de fase (mecánica de pausa).
            NPC.life = System.Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            Main.NewText($"Aethon — Fase {Phase}: {PhaseName()}", new Color(245, 196, 81));
        }

        private string PhaseName()
        {
            return Phase switch
            {
                1 => "Polvo Estelar",
                2 => "Nebulosa",
                3 => "Gravedad",
                4 => "Agujero Negro",
                5 => "Reconocimiento",
                _ => "?",
            };
        }

        public override void OnKill()
        {
            // Drops: Fragmentos de Resonancia + acceso a New Game+.
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 250;
            }
            Main.NewText("Aethon te reconoce como un par. El Fragmento se despierta.", new Color(245, 196, 81));
            // TODO: drop de Forma Ascendida (cosmético).
        }
    }
}
