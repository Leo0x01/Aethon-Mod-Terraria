using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigacion;

namespace AethonMod.Content.Weapons.Investigacion
{
    /// <summary>
    /// LODODEESENCIA — v6.54.0 — LOS CHARCOS BIOLUMINISCENTES DEL JUGADOR.
    ///
    /// La letra: «esta textura tambien es interesante
    /// DarkSludgeTextureGlow.png y DarkSludgeTexture.png, lo mismo
    /// investiga como las usas y crea tus propias versiones».
    ///
    /// LA INVESTIGACIÓN (Task 74-b): en el original son del ARMA RANGED
    /// «Dark Sludge» — globos que al caer forman CHARCOS de lodo que se
    /// apilan y crecen, dibujados a pantalla completa con un render
    /// target a media resolución y el shader SludgeEffect (warp
    /// senoidal de UV + scroll + la base oscura multiplicando la
    /// máscara + la textura de GLOW aportando los puestos
    /// bioluminiscentes azul-violeta).
    ///
    /// LA VERSIÓN CASA: los charcos viven como PROYECTILES pegados al
    /// suelo (sin render targets — tML 2026.08 no carga .fx crudos),
    /// dibujados con FajaCPU: la base LodoOscuro.png (value-noise
    /// cuantizado a 6 niveles casi negros con tinte morado) tintada de
    /// violeta profundo con WARP SENOIDAL del muestreo, y encima
    /// LodoOscuroBrillo.png (los ~400 puntos gaussianos VIOLETAS)
    /// aditiva — la bioluminiscencia morada de la casa.
    /// </summary>
    public class LodoDeEsencia : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName/Tooltip: Items.LodoDeEsencia (hjson).
        }

        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.mana = 14;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LodoGlob>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item20;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL ARCO PARABÓLICO del lanzado (la mano sube el globo).
            Vector2 mira = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(mira.X) || float.IsNaN(mira.Y)) mira = -Vector2.UnitY;
            Projectile.NewProjectile(source, player.MountedCenter + mira * 20f,
                mira * Item.shootSpeed + new Vector2(0f, -3f),
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine linea = new(Mod, "LodoPrueba",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.LodoDeEsencia.Prueba"))
            {
                OverrideColor = new Color(255, 240, 180),
            };
            tooltips.Add(linea);
        }
    }
}
