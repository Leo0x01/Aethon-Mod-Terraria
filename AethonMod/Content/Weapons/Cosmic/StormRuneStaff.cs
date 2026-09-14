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
    /// StormRuneStaff — v6.19 — EL CETRO DEL TRUENO RÚNICO.
    ///
    /// El ARMA DE RAYOS del proyecto (petición del usuario: "crea un arma
    /// que use rayos usando nuestra librería, aplica varios efectos a
    /// estos rayos"). Dispara UNA DESCARGA INSTANTÁNEA al punto del
    /// cursor: rayo zigzag vivo de la librería LightningCore (doble tira
    /// cuerpo + núcleo, re-generado ~14 Hz) con:
    ///
    ///   · Daño EN LÍNEA (todo lo que cruza la descarga).
    ///   · CADENA eléctrica a 3 enemigos cercanos (60% del daño).
    ///   · ARCOS de impacto + ONDA DE CHOQUE expandiéndose.
    ///   · ELECTRIFICADO (240 ticks) + luz a lo largo del rayo.
    ///
    /// Alcance 560 px. Mana 12.
    /// </summary>
    public class StormRuneStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RunicLightning>();
            Item.shootSpeed = 14f;
            Item.mana = 12; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL VECTOR COMPLETO al cursor (clampeado a 560 px): el rayo es
            // INSTANTÁNEO — el proyectil lo ancla y NO viaja.
            Vector2 muzzle = position + Vector2.Normalize(velocity) * 18f;
            Vector2 toCursor = Main.MouseWorld - muzzle;
            float len = toCursor.Length();
            if (len < 24f)
                toCursor = Vector2.Normalize(velocity == Vector2.Zero ? Vector2.UnitX : velocity) * 24f;
            else if (len > 560f)
                toCursor *= 560f / len;

            Projectile.NewProjectile(source, muzzle, toCursor, type, damage, knockback,
                player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.18: TOOLTIP CORTO — dos líneas, el nombre vive arriba.
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/BFE8FF:Descarga instantánea de rayo rúnico al cursor (560 px)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Daña en línea · salta a 3 enemigos · arcos y onda de choque · electrifica]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
