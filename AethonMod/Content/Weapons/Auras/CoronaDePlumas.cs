using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Auras;

namespace AethonMod.Content.Weapons.Auras
{
    // ======================================================================
    //  CoronaDePlumas — v6.100 — EL AURA #1: LA CORONA DE PLUMAS.
    //
    //  LA LETRA DEL USUARIO (v6.100): «crea armas tipo aura,
    //  investiga las armas tipo aura en internet» — LA INVESTIGACIÓN
    //  (la sesión de las auras): hay DOS familias de aura-arma — la
    //  CANALIZADA PERSONAL (el aura vive mientras sostienes el arma:
    //  el arco de la Pluma, las coronas orbitantes del Warbanner) y
    //  la ZONA ESTÁTICA (se lanza y queda: el charco, la runa en el
    //  suelo). LA CASA HACE UNA DE CADA:
    //
    //   · LA CORONA (esta): el aura personal CANALIZADA — mantén el
    //     uso y un ARO DE PLUMAS orbita al portador mordiendo a los
    //     que pisan tu cielo (150 px). Cada mordida ESCRIBE sobre el
    //     enemigo: +15% de mordida por marca, hasta 5 marcas — EL
    //     ANOTADO pica más cada vez.
    //   · EL DERRAME (la otra): la zona estática (DerrameDeTinta).
    // ======================================================================
    public class CoronaDePlumas : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 48;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.channel = true;             // CANALIZADA: vive mientras la sostengas
            Item.shoot = ModContent.ProjectileType<CoronaDePlumasProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // SIN COSTE DE MANÁ
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.35f);
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola corona viva (el canal es único).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == Item.shoot)
                    return false;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Corona1",
                "[c/D8F5EE:LA CORONA: mantén el uso — un aro de plumas orbita y muerde a los que pisan tu cielo]"));
            tooltips.Add(new TooltipLine(Mod, "Corona2",
                "[c/D8F5EE:EL ANOTADO: cada mordida ESCRIBE sobre el enemigo (+15% de mordida, hasta 5 marcas)]"));
            tooltips.Add(new TooltipLine(Mod, "Corona3",
                "[c/9ED8C4:(el nido de plumas vivas · sin knockback: la jaula no empuja · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
