using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Clicker;

namespace AethonMod.Content.Weapons.Clicker
{
    // ======================================================================
    //  Ojo del Norte — v6.167 — LA BOLSA DE LOS VEINTE (la letra: «crea una
    //  bolsa con al menos 20 armas tipo clickler y daselas al jugador
    //  […] ser super creativo con cada una de ella»).
    //  El ojo de la estrella que no duerme; cada 9, EL VÓRTICE aspira (190px).
    // ======================================================================
    public class OjoDelNorte : ModItem
    {
        private const float Radio = 400f;
        private const int CadaN = 9;

        public override void SetStaticDefaults()
        {
            ClickerCasa.Registrar(Type, Radio, new Color(120,180,255),
                EstiloClicker.Estrella);
        }

        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;          // el clicker es UN clic por golpe
            Item.shoot = ModContent.ProjectileType<ClickerGolpeProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // la regla de la casa: SIN maná
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.MenuTick;
        }

        public override bool CanUseItem(Player player)
            => Vector2.DistanceSquared(Main.MouseWorld, player.Center) <= Radio * Radio;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool especial = player.GetModPlayer<ClickerJugador>()
                .RegistraClic(Type) % CadaN == 0;
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type,
                especial ? damage * 2 : damage, knockback, player.whoAmI,
                especial ? 4f : 0f, Main.rand.Next(9973), 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "OjoDelNorte1",
                "[c/78B4FF:LA MANO: cada clic golpea donde apuntas (radio " + (int)Radio + " px)]"));
            tooltips.Add(new TooltipLine(Mod, "OjoDelNorte2",
                $"[c/78B4FF:Cada {CadaN} clics EL ESPECIAL de la casa — EL VÓRTICE (la garganta que aspira 190 px)]"));
            tooltips.Add(new TooltipLine(Mod, "OjoDelNorte3",
                "[c/7B62C4:(la bolsa de los veinte · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
