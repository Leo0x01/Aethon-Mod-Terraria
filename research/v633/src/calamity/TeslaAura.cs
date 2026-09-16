using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AcidRain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Typeless
{
    [PierceResistException]
    public class TeslaAura : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Typeless";
        private const float radius = 98f;
        private const int framesX = 3;
        private const int framesY = 6;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 218;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 18000;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.timeLeft *= 5;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
        }

        public override void AI()
        {
            // Protect against other mod projectile reflection like emode Granite Golems.
            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.localAI[0]++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.localAI[0] >= framesY)
            {
                Projectile.localAI[0] = 0;
                Projectile.localAI[1]++;
            }
            if (Projectile.localAI[1] >= framesX)
                Projectile.localAI[1] = 0;

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.15f / 255f, (255 - Projectile.alpha) * 0.15f / 255f, (255 - Projectile.alpha) * 0.01f / 255f);

            Player player = Main.player[Projectile.owner];
            Projectile.Center = player.Center;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), 90);
            target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 6);
        }
        // CIT 2MAY2025: Replaced old manual knockback code with setting HitDirectionOverride
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = (target.Center.X > Projectile.Center.X).ToDirectionInt();

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), 90);
            target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 6);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sprite = Terraria.GameContent.TextureAssets.Projectile[Type].Value;

            Color drawColour = Color.White;
            Rectangle sourceRect = new Rectangle(Projectile.width * (int)Projectile.localAI[1], Projectile.height * (int)Projectile.localAI[0], Projectile.width, Projectile.height);
            Vector2 origin = new Vector2(Projectile.width / 2, Projectile.height / 2);

            float opacity = Main.player[Projectile.owner].Calamity().teslaVisuals ? 1f : 0.25f;

            Main.EntitySpriteDraw(sprite, Projectile.Center - Main.screenPosition, sourceRect, drawColour * opacity, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, radius, targetHitbox);

        public override bool? CanHitNPC(NPC target)
        {
            if (NPCID.Sets.ProjectileNPC[target.type] || (target.catchItem != 0 && target.type != ModContent.NPCType<Radiator>()))
                return false;

            return null;
        }
    }
}
