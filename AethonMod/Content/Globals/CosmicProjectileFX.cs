using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// CosmicProjectileFX — GlobalProjectile que añade partículas cósmicas
    /// al proyectil Nightglow (ID 931) del Grimorio del Eterno.
    ///
    /// El Nightglow vanilla es solo azul. Este GlobalProjectile lo transforma
    /// en un proyectil cósmico añadiendo:
    /// - Estela dorada (cada frame)
    /// - Destellos cian (cada 2 frames)
    /// - Fragmentos magenta (cada 3 frames)
    /// - Halo índigo sutil (cada 4 frames)
    /// - Explosión cósmica al impactar
    ///
    /// Solo afecta al proyectil 931 (Nightglow) para no modificar otros proyectiles.
    /// </summary>
    public class CosmicProjectileFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // Solo aplica al Nightglow (931)
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == 931; // Nightglow
        }

        public override void AI(Projectile projectile)
        {
            // === ESTELA CÓSMICA (v5.2: duración CORTA) ===
            // El usuario reportó que la estela magenta duraba demasiado.
            // Fix: configurar cada dust con noGravity=true, fadeIn=0, y alpha alto
            // para que desaparezcan en ~10-15 frames (0.25 seg) en vez de 60+.

            // Dorado (núcleo de galaxia) — cada frame
            Dust d1 = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                -projectile.velocity * 0.05f + new Vector2(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f)),
                150, new Color(255, 217, 61), 0.7f);
            d1.noGravity = true;
            d1.fadeIn = 0f;

            // Cian (estrella guía) — cada 2 frames
            if (Main.rand.NextBool(2))
            {
                Dust d2 = Dust.NewDustPerfect(projectile.Center, DustID.BlueTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)),
                    180, new Color(0, 255, 255), 0.6f);
                d2.noGravity = true;
                d2.fadeIn = 0f;
            }

            // Magenta (gemas) — cada 3 frames — alpha ALTO para que dure poco
            if (Main.rand.NextBool(3))
            {
                Dust d3 = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    -projectile.velocity * 0.08f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 0, 102), 0.5f);
                d3.noGravity = true;
                d3.fadeIn = 0f;
            }

            // Índigo (fondo del portal) — cada 4 frames, muy sutil
            if (Main.rand.NextBool(4))
            {
                Dust d4 = Dust.NewDustPerfect(projectile.Center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)),
                    120, new Color(75, 0, 130), 0.5f);
                d4.noGravity = true;
                d4.fadeIn = 0f;
            }

            // Luz cósmica (más intensa que la default del Nightglow)
            Lighting.AddLight(projectile.Center, new Vector3(0.8f, 0.6f, 1.0f));
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // === v5.29: DAÑO EN ÁREA (BoltAreaDamage) ===
            // Finalmente implementado: daña NPCs cercanos al punto de impacto
            // según el nivel del Grimorio del jugador que disparó.
            try
            {
                if (projectile.owner >= 0 && projectile.owner < Main.player.Length)
                {
                    Player owner = Main.player[projectile.owner];
                    if (owner != null && owner.active)
                    {
                        // Buscar el Grimorio en el inventario del jugador (no solo HeldItem)
                        int grimorioLevel = 1;
                        for (int i = 0; i < 58; i++)
                        {
                            Item inv = owner.inventory[i];
                            if (inv != null && inv.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                            {
                                var sl = inv.GetGlobalItem<ShardLevelItem>();
                                if (sl != null) { grimorioLevel = sl.Level; break; }
                            }
                        }
                        int areaRadius = WeaponScaling.BoltAreaDamage(grimorioLevel);
                        if (areaRadius > 0)
                        {
                            int areaDamage = System.Math.Max(1, hit.Damage / 2); // 50% del daño original
                            foreach (NPC npc in Main.ActiveNPCs)
                            {
                                if (!npc.active || npc.whoAmI == target.whoAmI) continue;
                                if (npc.friendly || npc.townNPC) continue;
                                if (!npc.CanBeChasedBy()) continue;
                                // v5.30: Check de inmunidad para no dañar NPCs ya golpeados
                                if (npc.immune[projectile.owner] > 0) continue;
                                float dist = Vector2.Distance(npc.Center, target.Center);
                                if (dist < areaRadius)
                                {
                                    npc.SimpleStrikeNPC(areaDamage, projectile.direction,
                                        false, 0, DamageClass.Magic, false, 0, false);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // === EXPLOSIÓN CÓSMICA al impactar (v5.3: duración CORTA) ===
            // Mismo fix que la estela: noGravity + fadeIn=0 + alpha alto + scale pequeño

            // Núcleo dorado (supernova)
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-5f, 5f),
                        Main.rand.NextFloat(-5f, 5f)),
                    180, new Color(255, 217, 61), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos cian (estrella guía)
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)),
                    200, new Color(0, 255, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Fragmentos magenta (gemas) — alpha alto + scale pequeño = duración corta
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)),
                    220, new Color(255, 0, 102), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Halo índigo (fondo del portal)
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f)),
                    150, new Color(75, 0, 130), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destello blanco central (supernova)
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    255, default, 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            // Explosión cósmica al morir (sin impacto con enemigo) — v5.3: duración corta
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f)),
                    180, new Color(255, 217, 61), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    200, new Color(0, 255, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    220, new Color(255, 0, 102), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
