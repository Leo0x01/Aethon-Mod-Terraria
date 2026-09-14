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
    /// EnjambrePrismaticoStaff — v6.26 — EL BASTÓN DEL ENJAMBRE PRISMÁTICO.
    ///
    /// Petición del usuario: "luego crea mas bastones con nuevos tipos de
    /// proyectiles creativos, al menos 5" — el tercero: un ENJAMBRE de
    /// avispas de luz.
    ///
    /// EL ARMA QUE PICA EN CORO: cada disparo lanza un NÚCLEO prismático
    /// que REVIENTA en 12 ABISPAJAS DE LUZ — cada una con su COLOR
    /// prismático propio (LumenLib.Hue distinto) y su ESTELA corta. Las
    /// avispas ORBITAN en enjambre real (boids: cohesión + separación +
    /// objetivo) alrededor del ENEMIGO MÁS CERCANO y lo PICAN con
    /// cooldown individual (45 ticks por avispa). Cuando la presa muere,
    /// el enjambre MIGRA a la siguiente — hasta que no queda nadie.
    ///
    /// Daño 190, useTime 45, sin maná, HoldUp, autoReuse, rareza Quest.
    /// </summary>
    public class EnjambrePrismaticoStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip es el zumbido del enjambre.
        }

        public override void SetDefaults()
        {
            Item.damage = 190;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EnjambrePrismaticoProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 30000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El NÚCLEO vuela hacia el cursor y revienta a los ~26 ticks.
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "EnjambreHeader",
                "[c/FF9EE8:EL ENJAMBRE PRISMÁTICO — doce avispas de luz, cada una de un color]"));
            tooltips.Add(new TooltipLine(Mod, "EnjambreLine1",
                "[c/D98ED4:El núcleo revienta en 12 avispas que ORBITAN en enjambre (boids real)]"));
            tooltips.Add(new TooltipLine(Mod, "EnjambreLine2",
                "[c/D98ED4:Cada avispa PICA con su propio cooldown (45 ticks) y deja estela corta]"));
            tooltips.Add(new TooltipLine(Mod, "EnjambreLine3",
                "[c/FFC9F0:Cuando la presa muere el enjambre MIGRA a la siguiente — hasta que no queda nadie]"));
            tooltips.Add(new TooltipLine(Mod, "EnjambrePie",
                "[c/78788C:14 segundos de cacería · el arcoíris entero picando]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
