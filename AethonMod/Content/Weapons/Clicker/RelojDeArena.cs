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
    //  Reloj de Arena del Fin — v6.167 — LA BOLSA DE LOS VEINTE (la letra: «crea una
    //  bolsa con al menos 20 armas tipo clickler y daselas al jugador
    //  […] ser super creativo con cada una de ella»).
    //  El último grano; el combo más largo, el vórtice definitivo ×5.
    // ======================================================================
    public class RelojDeArena : ModItem
    {
        private const float Radio = 330f;
        private const int CadaN = 18;

        public override void SetStaticDefaults()
        {
            ClickerCasa.Registrar(Type, Radio, new Color(255,240,200),
                EstiloClicker.RunasDobles);
        }

        public override void SetDefaults()
        {
            Item.damage = 210;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 13;
            Item.useAnimation = 13;
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
                especial ? damage * 5 : damage, knockback, player.whoAmI,
                especial ? 4f : 0f, Main.rand.Next(9973), 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "RelojDeArena1",
                "[c/FFF0C8:LA MANO: cada clic golpea donde apuntas (radio " + (int)Radio + " px)]"));
            tooltips.Add(new TooltipLine(Mod, "RelojDeArena2",
                $"[c/FFF0C8:Cada {CadaN} clics EL ESPECIAL de la casa — EL VÓRTICE (la garganta que aspira 190 px)]"));
            tooltips.Add(new TooltipLine(Mod, "RelojDeArena3",
                "[c/7B62C4:(la bolsa de los veinte · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
