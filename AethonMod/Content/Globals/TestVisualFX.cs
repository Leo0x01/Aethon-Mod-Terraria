using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestVisualFX — GlobalProjectile que maneja los efectos visuales
    /// de las armas de prueba (TestVisuals.cs).
    ///
    /// Efectos:
    /// 1. Trail de estrellas (ai[1] == 9999): pequeñas estrellas doradas/cian
    /// 2. Color del proyectil (ai[1] == 1001/1002/1003): re-tinte dorado/cian/magenta
    /// 3. Halo del minion (ai[0] == 1 en CosmicOrbMinion): anillo dorado girando
    /// </summary>
    public class TestVisualFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            // Aplicar a Nightglow (931) y CosmicOrbMinion (custom)
            return projectile.type == 931 ||
                   projectile.type == ModContent.ProjectileType<Projectiles.CosmicOrbMinion>();
        }

        public override void AI(Projectile projectile)
        {
            // === TRAIL DE ESTRELLAS ===
            // ai[1] == 9999: trail de estrellas doradas/cian
            if (projectile.ai[1] == 9999)
            {
                // Estrella dorada cada frame
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                    -projectile.velocity * 0.05f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)),
                    150, new Color(255, 217, 61), 0.8f);
                d.noGravity = true; d.fadeIn = 0f;

                // Estrella cian cada 2 frames
                if (Main.rand.NextBool(2))
                {
                    Dust d2 = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Pink,
                        -projectile.velocity * 0.08f + new Vector2(
                            Main.rand.NextFloat(-1.5f, 1.5f),
                            Main.rand.NextFloat(-1.5f, 1.5f)),
                        180, new Color(0, 255, 255), 0.7f);
                    d2.noGravity = true; d2.fadeIn = 0f;
                }
            }

            // === v5.32: PARTÍCULAS DE ESTRELLAS PARA PROYECTILES TINTADOS ===
            // ai[1] == 1001 (dorado), 1002 (cian), 1003 (magenta)
            // Agrega partículas de estrellas con el color del tinte + glow
            if (projectile.ai[1] >= 1001 && projectile.ai[1] <= 1003)
            {
                Color starColor;
                int dustType;
                switch ((int)projectile.ai[1])
                {
                    case 1001: // Dorado
                        starColor = new Color(255, 217, 61);
                        dustType = DustID.Enchanted_Gold;
                        break;
                    case 1002: // Cian
                        starColor = new Color(0, 255, 255);
                        dustType = DustID.Enchanted_Pink; // usa pink para forma de estrella
                        break;
                    case 1003: // Magenta
                        starColor = new Color(255, 0, 255);
                        dustType = DustID.Enchanted_Pink;
                        break;
                    default:
                        return;
                }

                // Estrella con color del tinte cada frame
                Dust star = Dust.NewDustPerfect(projectile.Center, dustType,
                    -projectile.velocity * 0.03f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)),
                    150, starColor, 0.7f);
                star.noGravity = true; star.fadeIn = 0f;

                // Glow del color del tinte
                Lighting.AddLight(projectile.Center, new Vector3(
                    starColor.R / 255f * 0.5f,
                    starColor.G / 255f * 0.5f,
                    starColor.B / 255f * 0.5f));
            }

            // === HALO DEL MINION ===
            // ai[0] == 1 en CosmicOrbMinion: anillo dorado girando
            if (projectile.type == ModContent.ProjectileType<Projectiles.CosmicOrbMinion>() &&
                projectile.ai[0] == 1)
            {
                // Dibujar anillo dorado con 8 partículas girando
                float baseAngle = Main.GameUpdateCount * 0.05f;
                float radius = 25f;
                for (int i = 0; i < 8; i++)
                {
                    float angle = baseAngle + (System.MathF.PI * 2 / 8) * i;
                    Vector2 offset = new Vector2(
                        (float)System.Math.Cos(angle) * radius,
                        (float)System.Math.Sin(angle) * radius);
                    Dust d = Dust.NewDustPerfect(projectile.Center + offset,
                        DustID.GoldFlame, Vector2.Zero, 150,
                        new Color(255, 217, 61), 0.6f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }
        }

        /// <summary>
        /// PreDraw para re-tintar el proyectil según el color seleccionado.
        /// ai[1] == 1001: dorado, 1002: cian, 1003: magenta
        /// </summary>
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            // Solo aplicar color a Nightglow (931) con flag de color
            if (projectile.type != 931) return true;
            if (projectile.ai[1] < 1001 || projectile.ai[1] > 1003) return true;

            // Cargar textura
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[projectile.type].Value;
            if (texture == null) return true;

            // Origen = centro
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            // Determinar color de tinte
            Color tintColor;
            switch ((int)projectile.ai[1])
            {
                case 1001: // Dorado
                    tintColor = new Color(255, 217, 61, 255);
                    break;
                case 1002: // Cian
                    tintColor = new Color(0, 255, 255, 255);
                    break;
                case 1003: // Magenta
                    tintColor = new Color(255, 0, 255, 255);
                    break;
                default:
                    return true; // sin tinte
            }

            // Dibujar con tinte (multiplicar lightColor por el tinte)
            Main.spriteBatch.Draw(texture, drawPos, null,
                Color.Lerp(lightColor, tintColor, 0.6f), // 60% tinte, 40% luz natural
                projectile.rotation, origin, projectile.scale,
                SpriteEffects.None, 0f);

            return false; // no dibujar default
        }

        /// <summary>
        /// v5.30: Daño en área para las armas de prueba TestArea20 y TestArea60.
        /// ai[1] == 2001: radio 20px (estándar del Grimorio)
        /// ai[1] == 2002: radio 60px (ampliado, casi 4 tiles)
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Solo procesar flags de daño en área (2001/2002)
            if (projectile.ai[1] != 2001 && projectile.ai[1] != 2002) return;

            int areaRadius = (int)projectile.ai[1] == 2001 ? 20 : 60;
            int areaDamage = System.Math.Max(1, hit.Damage / 2); // 50% del daño

            try
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || npc.whoAmI == target.whoAmI) continue;
                    if (npc.friendly || npc.townNPC) continue;
                    if (!npc.CanBeChasedBy()) continue;
                    if (npc.immune[projectile.owner] > 0) continue;

                    float dist = Vector2.Distance(npc.Center, target.Center);
                    if (dist < areaRadius)
                    {
                        npc.SimpleStrikeNPC(areaDamage, projectile.direction,
                            false, 0, DamageClass.Magic, false, 0, false);

                        // Partículas visuales del daño en área
                        for (int i = 0; i < 5; i++)
                        {
                            Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldFlame,
                                new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                                150, new Color(255, 217, 61), 0.7f);
                            d.noGravity = true; d.fadeIn = 0f;
                        }
                    }
                }

                // Anillo visual del área de daño
                for (int i = 0; i < 12; i++)
                {
                    float angle = (System.MathF.PI * 2 / 12) * i;
                    Vector2 offset = new Vector2(
                        (float)System.Math.Cos(angle) * areaRadius,
                        (float)System.Math.Sin(angle) * areaRadius);
                    Dust d = Dust.NewDustPerfect(target.Center + offset,
                        DustID.BlueTorch, Vector2.Zero, 200,
                        new Color(0, 255, 255), 0.6f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }
            catch { }
        }
    }
}
