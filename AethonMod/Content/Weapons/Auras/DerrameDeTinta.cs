using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Auras;

namespace AethonMod.Content.Weapons.Auras
{
    // ======================================================================
    //  DerrameDeTinta — v6.100 — EL AURA #2: EL DERRAME DE TINTA.
    //
    //  LA ZONA ESTÁTICA de la investigación de auras: derrama un
    //  charco de tinta-sombra donde apuntas (7 tiles) y HIERVE 10
    //  segundos — los que lo pisan reciben LA MORDIDA DEL VERSO y la
    //  tinta les atrapa las piernas (−30% de velocidad). Suelta el
    //  arma y sigue peleando: el charco es de los que no piden
    //  permiso (máximo 2 vivos — el tercero se bebe al primero).
    // ======================================================================
    public class DerrameDeTinta : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DerrameDeTintaProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // SIN COSTE DE MANÁ
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.3f);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Derrame1",
                "[c/C79AEB:EL CHARCO: derrama tinta-sombra donde apuntas — 7 tiles que hierve 10 segundos]"));
            tooltips.Add(new TooltipLine(Mod, "Derrame2",
                "[c/C79AEB:Los que la pisan reciben la mordida del verso y la tinta les atrapa las piernas (−30% velocidad)]"));
            tooltips.Add(new TooltipLine(Mod, "Derrame3",
                "[c/8B6FC4:(máximo 2 charcos vivos · suelta el arma y sigue peleando · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
