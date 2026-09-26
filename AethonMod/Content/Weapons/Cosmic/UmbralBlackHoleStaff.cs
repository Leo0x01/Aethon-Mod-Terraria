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
    /// UmbralBlackHoleStaff — v6.16 — EL BASTÓN DEL AGUJERO NEGRO DEL UMBRAL.
    ///
    /// EL 6to agujero negro del mod, EL AGUJERO DE LA REFERENCIA
    /// (petición expresa: "pon la referencia de fondo y comienza a
    /// agregarle cosas hasta llegar al agujero negro de la referencia,
    /// todo por código"): disco de acreción OBLICUO con asimetría
    /// DOPPLER — el lado derecho BLANCO-incandescente y grueso, el
    /// izquierdo rojo profundo y fino —, estrías pintadas largas y
    /// curvas con gradiente blanco-amarillo → naranja → rojo neón →
    /// magenta → púrpura, PÚA de energía blanco-rosa, rayo naranja
    /// dentado, velos de materia vaporizada, filamentos violeta cayendo
    /// al núcleo, CÍRCULO DE RUNAS doradas ENORME CON HUECOS y brasas
    /// con estelas. 100% CÓDIGO.
    ///
    /// Hermano del BlackHoleStaff, Olvido y Cósmico (v6.50.3 — doc
    /// podrida retirada: "Crimson" y "Fusión" no existen en el código
    /// actual — solo en el CHANGES.md histórico).
    /// </summary>
    public class UmbralBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<UmbralBlackHoleProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.18: TOOLTIP CORTO — la ventana de info ya no es un muro de
            // texto (el nombre ahora vive en la localización, arriba).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF9966:100% por código — el disco de la referencia con Doppler: lado cercano blanco, lado lejano rojo]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
