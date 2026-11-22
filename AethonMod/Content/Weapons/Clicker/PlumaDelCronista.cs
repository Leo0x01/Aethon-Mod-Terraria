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
    //  Pluma del Cronista — v6.167 — LA BOLSA DE LOS VEINTE (la letra: «crea una
    //  bolsa con al menos 20 armas tipo clickler y daselas al jugador
    //  […] ser super creativo con cada una de ella»).
    //  Cada clic estampa la palabra del Cronista; cada 10, LA PÁGINA ESTALLA.
    // ======================================================================
    public class PlumaDelCronista : ModItem
    {
        private const float Radio = 320f;
        private const int CadaN = 10;

        public override void SetStaticDefaults()
        {
            ClickerCasa.Registrar(Type, Radio, new Color(185,120,245),
                EstiloClicker.Tinta);
        }

        public override void SetDefaults()
        {
            Item.damage = 120;
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
                especial ? damage * 3 : damage, knockback, player.whoAmI,
                especial ? 3f : 0f, Main.rand.Next(9973), 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "PlumaDelCronista1",
                "[c/B978F5:LA MANO: cada clic golpea donde apuntas (radio " + (int)Radio + " px)]"));
            tooltips.Add(new TooltipLine(Mod, "PlumaDelCronista2",
                $"[c/B978F5:Cada {CadaN} clics EL ESPECIAL de la casa — LA TINTA ESTALLA (el NovaActo del motor de píxeles)]"));
            tooltips.Add(new TooltipLine(Mod, "PlumaDelCronista3",
                "[c/7B62C4:(la bolsa de los veinte · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
