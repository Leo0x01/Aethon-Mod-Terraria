using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigaciones;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  FaroDelEclipse — v6.174 — EL RAYO CONCENTRADO ORIGINAL (la letra:
    //  «crea un arma de rayo concentrado original con las librerías/VFX/
    //  texturas del proyecto, lo más impresionante posible, con al menos
    //  10 a 20 capas de VFX»). De la INVESTIGACIÓN (el plano del haz:
    //  el haz que NACE del arma y sigue al cursor — la idea; la letra, el
    //  render y las 26 capas son de la casa: TajoLib + VFXCore).
    //
    //  MANTÉN EL BOTÓN: 0,4 s de carga y el conducto sigue al cursor
    //  hasta 900 px. Daño mágico, sin maná (arma de investigación), la
    //  receta de la casa (5 madera en el banco de trabajo).
    // ======================================================================
    public class FaroDelEclipse : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 180;
            Item.DamageType = DamageClass.Magic;
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.mana = 0;
            Item.shoot = ModContent.ProjectileType<FaroDelEclipseHaz>();
            Item.shootSpeed = 0f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item43;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).AddTile(TileID.WorkBenches).Register();
        }
    }
}
