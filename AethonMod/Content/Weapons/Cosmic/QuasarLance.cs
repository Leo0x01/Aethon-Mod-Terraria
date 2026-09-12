using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// QuasarLance — LA LANZA DEL QUÁSAR (arma mágica nueva, v5.99).
    ///
    /// Petición del usuario: "crea una nueva arma con un proyectil super
    /// cosmico".
    ///
    /// EL PROYECTIL: UN CHORRO RELATIVISTA — el objeto más brillante del
    /// universo (los chorros de los quásares superan el brillo de GALAXIAS
    /// enteras). Una lanza de luz larguísima y velocísima que ATRAVIESA
    /// hasta 10 enemigos, con 5 NUDOS DE SHOCK pulsando hacia la punta (los
    /// "knots" de los chorros reales), retorción helicoidal sutil y estela
    /// de polvo estelar. Al disiparse: EL FLORECIMIENTO DEL QUÁSAR —
    /// destello cruzado + anillo + AoE.
    ///
    /// Especificaciones:
    ///   - damage = 85 (DamageClass.Magic, escala con daño mágico)
    ///   - mana = 14, useTime = 22, knockBack 4, autoReuse
    ///   - dispara QuasarJetProjectile a 26 px/t (×3 updates ≈ relativista)
    /// </summary>
    public class QuasarLance : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 14;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 6, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12;
            Item.shoot = ModContent.ProjectileType<QuasarJetProjectile>();
            Item.shootSpeed = 26f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // nace ya EN MOVIMIENTO (la lanza no "acelera": ES luz)
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "QL_Title",
                "[c/AAD4FF:═══ LA LANZA DEL QUÁSAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "QL_Desc",
                "[c/D9EAFF:Dispara un CHORRO RELATIVISTA — el objeto más brillante del universo, plasma a casi la velocidad de la luz]"));
            tooltips.Add(new TooltipLine(Mod, "QL_Desc2",
                "[c/C8B8FF:La lanza de luz ATRAVIESA hasta 10 enemigos, con nudos de shock pulsando hacia la punta y retorción helicoidal]"));
            tooltips.Add(new TooltipLine(Mod, "QL_Desc3",
                "[c/9FD9FF:Al disiparse: EL FLORECIMIENTO DEL QUÁSAR — destello cruzado y estallido de plasma]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
