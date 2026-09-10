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
    /// NebulaCloudStaff — bastón que invoca una nube de nebulosa lenta.
    ///
    /// Especificaciones:
    ///   - damage = 55 (DamageType.Magic)
    ///   - useTime = useAnimation = 45
    ///   - shootSpeed = 6f (lento — la nube deriva)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// El proyectil se mueve muy lentamente (velocity *= 0.92). Dibuja Noise.png
    /// con escala grande y colores púrpura-magenta, rotando lentamente. Genera
    /// PurpleTorch dust en patrón de nube (posiciones aleatorias dentro de 40px
    /// del centro). Brillo pulsante. penetrate=-1, timeLeft=240, tileCollide=false,
    /// extraUpdates=0.
    /// </summary>
    public class NebulaCloudStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<NebulaCloudProjectile>();
            Item.shootSpeed = 6f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn ligeramente por encima del jugador
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "NC_Title",
                "[c/BE78FD:═══ NUBE DE NEBULOSA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "NC_Desc",
                "[c/B388FF:Nube púrpura lenta que envuelve todo a su paso]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
