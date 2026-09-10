using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Weapons.V20
{
    /// <summary>
    /// FrostBlizzardStaff — bastón que invoca una tormenta de cristales de hielo.
    ///
    /// Especificaciones:
    ///   - damage = 45 (DamageType.Magic)
    ///   - useTime = useAnimation = 28
    ///   - shootSpeed = 10f (base, pero cada shard varía)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Override Shoot para crear 8 proyectiles en ángulos aleatorios con
    /// velocidades aleatorias. Cada proyectil es pequeño (Star.png, cyan-white),
    /// deja estela de IceTorch y aplica Frostburn al impactar.
    /// penetrate=2, timeLeft=90, extraUpdates=1.
    /// </summary>
    public class FrostBlizzardStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 28;
            Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<FrostBlizzardProjectile>();
            Item.shootSpeed = 10f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int count = 8;
            float baseSpeed = velocity.Length();
            if (baseSpeed < 0.1f) baseSpeed = Item.shootSpeed;

            for (int i = 0; i < count; i++)
            {
                // Ángulo aleatorio dentro de un cono amplio (centrado en la dirección base)
                float spread = MathHelper.ToRadians(80f);
                float angle = Main.rand.NextFloat(-spread * 0.5f, spread * 0.5f);
                Vector2 dir = velocity.SafeNormalize(new Vector2(0f, -1f)).RotatedBy(angle);

                // Velocidad aleatoria
                float spd = baseSpeed * Main.rand.NextFloat(0.7f, 1.3f);
                Vector2 vel = dir * spd;

                // Spawn ligeramente disperso alrededor del jugador
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-10f, 10f),
                    Main.rand.NextFloat(-10f, 10f));

                Projectile.NewProjectile(source, position + offset, vel, type, damage, knockback, player.whoAmI);
            }
            return false; // ya creamos los 8 proyectiles
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "FB_Title",
                "[c/55FFFF:═══ VENTISCA HELADA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "FB_Desc",
                "[c/B388FF:Tormenta de cristales de hielo con quemadura de escarcha]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
