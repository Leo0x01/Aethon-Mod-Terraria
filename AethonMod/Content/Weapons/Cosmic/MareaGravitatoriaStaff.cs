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
    /// MareaGravitatoriaStaff — v6.26 — EL BASTÓN DE LA MAREA GRAVITATORIA.
    ///
    /// Petición del usuario: "luego crea mas bastones con nuevos tipos de
    /// proyectiles creativos, al menos 5" — el segundo: una OLA DE LUZ que
    /// cabalga el terreno.
    ///
    /// EL ARMA QUE ENCHARCA EL MUNDO: cada disparo suelta una ONDA DE MAREA
    /// de agua de luz azul que AVANZA horizontal PEGADA AL SUELO — sube y
    /// baja colinas con rotación por pendiente — con CRESTA brillante,
    /// ESPUMA de chispas blancas y un rastro de CHARCOS de bruma. ARRASTRA
    /// a los enemigos (los empuja con cada tick) y los MOJA (daño constante
    /// + debuff). Si golpea una pared, SE ROMPE en 3 olas menores
    /// diagonales que rebotan por su cuenta.
    ///
    /// Daño 210, useTime 60, sin maná, HoldUp, autoReuse, rareza Quest.
    /// </summary>
    public class MareaGravitatoriaStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip es el parte meteorológico.
        }

        public override void SetDefaults()
        {
            Item.damage = 210;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MareaGravitatoriaProjectile>();
            Item.shootSpeed = 8f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 30000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La ola nace a los PIES del jugador mirando hacia el cursor;
            // su velocidad de marea es propia (la AI la fija).
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "MareaHeader",
                "[c/6FD3FF:LA MAREA GRAVITATORIA — una ola de luz que cabalga el terreno]"));
            tooltips.Add(new TooltipLine(Mod, "MareaLine1",
                "[c/3FA8E0:Sube y baja colinas pegada al suelo — la cresta brilla y la espuma chispea]"));
            tooltips.Add(new TooltipLine(Mod, "MareaLine2",
                "[c/3FA8E0:ARRASTRA a los enemigos con cada tick y los MOJA (daño constante)]"));
            tooltips.Add(new TooltipLine(Mod, "MareaLine3",
                "[c/A8E8FF:Detrás deja CHARCOS de bruma · contra una pared SE ROMPE en 3 olas menores]"));
            tooltips.Add(new TooltipLine(Mod, "MareaPie",
                "[c/78788C:15 segundos de marea · el agua de luz nunca se detiene]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
