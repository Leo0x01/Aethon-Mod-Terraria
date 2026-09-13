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
    /// Hermano del BlackHoleStaff, Crimson, Fusión, Olvido y Cósmico —
    /// todos quedan INTACTOS.
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF6414:═══ AGUJERO NEGRO DEL UMBRAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF9966:100% creado por código — el agujero de la REFERENCIA: disco oblicuo con DOPPLER, el lado que se acerca BLANCO-incandescente y grueso, el que se aleja rojo profundo y fino]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Estrías de plasma pintadas fluyen del blanco-amarillo al naranja, rojo neón, magenta y púrpura — una PÚA de energía blanca brota del lado vivo y un RAYO NARANJA dentado cae ramificando, mientras filamentos violeta caen al vacío]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:El CÍRCULO DE RUNAS DORADAS — enorme, fino, con HUECOS donde el sigilo se ha perdido — abraza el conjunto entre velos de materia vaporizada y brasas con estelas]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF6414:GIGANTE: esfera de 46px, arte de ~7R. Atrae enemigos en 450px; al morir nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
