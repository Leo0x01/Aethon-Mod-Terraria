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
    /// BrumaBlackHoleStaff — v6.17 — EL BASTÓN DEL AGUJERO NEGRO DE LA BRUMA.
    ///
    /// EL 7mo agujero negro del mod — nacido con la LIBRERÍA DE
    /// HUMO/NIEBLA/BRUMA PROCEDURAL del proyecto (Content/Effects/Bruma,
    /// creada por petición expresa investigando Diablo 3, Calamity,
    /// Starlight River, Book of Shaders e Inigo Quilez): un vacío GELIDO
    /// envuelto en bruma nebular fría teal/cian/violeta — el HALO son
    /// nubes (BrumaFX.Cloud), el anillo son FUMARELITOS (BrumaFX.Puff con
    /// texturas fBm horneadas 100% en runtime), las VOLUTAS espiralan al
    /// núcleo (BrumaFX.Tendril) y por los polos escapan CHIMENEAS de
    /// bruma (BrumaFX.Column). Todo por código, a CUALQUIER escala.
    ///
    /// Hermano del BlackHoleStaff, Crimson, Fusión, Olvido, Cósmico y
    /// Umbral — todos quedan INTACTOS.
    /// </summary>
    public class BrumaBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<BrumaBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/5AD0E6:═══ AGUJERO NEGRO DE LA BRUMA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/8FDCEF:100% creado por código con la LIBRERÍA DE BRUMA del proyecto — humo, niebla y bruma procedural con calidad a cualquier escala: texturas fBm nacidas en runtime, curl noise y senos inconmensurables]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Un vacío GELIDO envuelto en bruma nebular fría — el HALO son nubes de humo teal girando lento, el anillo son FUMARELITOS orbitando y las VOLUTAS espiralan hacia el núcleo donde la materia se disuelve en bruma]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Por los polos escapan chimeneas de niebla entre corredores de fotones cian, escarcha flotante y ondas de distorsión — la muerte llega EROSIONADA en grumos, como el humo de verdad]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/5AD0E6:GIGANTE: esfera de 48px, arte de ~7R. Atrae enemigos en 450px; al morir nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
