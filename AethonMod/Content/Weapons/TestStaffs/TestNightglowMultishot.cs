using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowMultishot — Nightglow que dispara 3 proyectiles en abanico.
    /// Cada disparo lanza 3 Nightglow con ángulos ligeramente diferentes.
    /// </summary>
    public class TestNightglowMultishot : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 22; // menor daño para compensar el multishot
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana // más mana por los 3 proyectiles
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 3 proyectiles en abanico
            for (int i = -1; i <= 1; i++)
            {
                float angle = i * 0.12f; // ~7 grados de separación
                Vector2 perturbed = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbed, type, damage, knockback, player.whoAmI);
            }

            // Anillo cósmico pequeño en cada disparo
            CosmicEffects.SpawnMagicRing(position, CosmicEffects.Cyan, 12, 30f, 2f);
            return false; // nosotros disparamos los proyectiles manualmente
        }
    }
}
