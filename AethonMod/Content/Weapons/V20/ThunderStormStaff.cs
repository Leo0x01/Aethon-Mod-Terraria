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
    /// ThunderStormStaff — bastón que invoca un rayo desde el cielo sobre el cursor.
    ///
    /// Especificaciones:
    ///   - damage = 80 (DamageType.Magic)
    ///   - useTime = useAnimation = 35
    ///   - shootSpeed = 22f (caída rápida del rayo)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Override Shoot para spawnear el proyectil en X del cursor, Y=400px por
    /// encima del cursor, cayendo hacia abajo a alta velocidad. El proyectil
    /// dibuja BeamCyan vertical con flickering. Al impactar tile o NPC, suena
    /// SoundID.Thunder y genera una ráfaga de BlueTorch dust en círculo.
    /// penetrate=-1, timeLeft=60, tileCollide=true.
    /// </summary>
    public class ThunderStormStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<ThunderStormProjectile>();
            Item.shootSpeed = 22f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn en X del cursor, Y = cursor Y - 400 (400px por encima del cursor)
            Vector2 cursorWorld = Main.MouseWorld;
            Vector2 spawnPos = new Vector2(cursorWorld.X, cursorWorld.Y - 400f);
            // Caída vertical rápida
            Vector2 vel = new Vector2(0f, Item.shootSpeed);
            Projectile.NewProjectile(source, spawnPos, vel, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "TS_Title",
                "[c/55AAFF:═══ TORMENTA DE RAYOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "TS_Desc",
                "[c/B388FF:Relámpago que cae del cielo sobre el cursor]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
