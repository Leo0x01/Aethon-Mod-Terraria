using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// LivingPulsarStaff — EL PÚLSAR VIVO (arma invocadora, v5.99).
    ///
    /// Petición del usuario: "luego crea otro minion cosmico".
    ///
    /// EL MINION ES EL PROYECTIL: una estrella de neutrones VIVA que gira
    /// sobre su eje barriendo el campo con DOS HACES DE FARO opuestos de
    /// radiación — el ataque más raro del arsenal: el daño no es contacto
    /// ni proyectil, son los RAYOS GIRANDO (340 px, ELECTRIFIED — radiación
    /// de sincrotrón). En reposo deriva en una figura-8 perezosa sobre tu
    /// hombro; con objetivo se coloca en alto entre tú y la víctima para
    /// que sus haces la RAQUEN en cada giro. Donde gira, el fondo pulsa
    /// sutilmente curvado (lente del pase B).
    ///
    /// Especificaciones:
    ///   - damage = 30 (DamageClass.Summon; los haces golpean al 55% a 12/s)
    ///   - mana = 10, useTime = 36, minionSlots = 1 (apilable)
    /// </summary>
    public class LivingPulsarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Summon;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 10;
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<LivingPulsarMinion>();
            Item.shootSpeed = 10f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el púlsar nace en alto, sobre el jugador, ya girando
            player.AddBuff(ModContent.BuffType<LivingPulsarBuff>(), 2);
            Vector2 spawnPos = player.Center + new Vector2(
                velocity.SafeNormalize(Vector2.Zero).X * 40f, -70f);
            Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "LP_Title",
                "[c/9FD6FF:═══ EL PÚLSAR VIVO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "LP_Desc",
                "[c/D9ECFF:Invoca una estrella de neutrones VIVA: un faro del cosmos girando a tu lado]"));
            tooltips.Add(new TooltipLine(Mod, "LP_Desc2",
                "[c/9FC8FF:Sus DOS HACES DE RADIACIÓN barren el campo de batalla — todo lo que cruzan queda ELECTRIFICADO]"));
            tooltips.Add(new TooltipLine(Mod, "LP_Desc3",
                "[c/C8E8FF:Con objetivo a la vista se coloca en alto para rañar a tu víctima en cada giro]"));
            tooltips.Add(new TooltipLine(Mod, "LP_Desc4",
                "[c/9FD6FF:Invoca varios púlsares si tienes espacio de sirvientes]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
