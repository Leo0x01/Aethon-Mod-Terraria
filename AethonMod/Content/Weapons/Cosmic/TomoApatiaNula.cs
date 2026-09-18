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
    /// TomoApatiaNula — v6.41 — EL TOMO DE LA APATÍA NULA.
    ///
    /// ARMA DE PRUEBAS (la petición del usuario): la réplica exacta del
    /// arma original cuyo proyectil se copió en TentaculoCosmicoProjectile
    /// — hasta los NÚMEROS son los del original (daño 63, maná 26,
    /// useTime 8 / useAnimation 20 / reuseDelay 8, knockback 5.5, rareza
    /// Roja) y el disparo con la dispersión ±0.7 rad del Shoot original.
    ///
    /// La apatía nula: el tomo que ya no siente nada — escupe tentáculos
    /// de vacío que no hacen daño durante SU GESTACIÓN (2 segundos de
    /// pulsos que imploden), luego azoztan en TRES CURVAS hacia donde
    /// apuntas (curvando el rumbo a cada tick, teletransportándose entre
    /// curva y curva) y cada golpe sucesivo pesa menos: el tentáculo se
    /// aburre de matar.
    /// </summary>
    public class TomoApatiaNula : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            // === LOS NÚMEROS DEL ORIGINAL (1:1 — arma de pruebas fiel). ===
            Item.width = 28;
            Item.height = 30;
            Item.damage = 63;
            Item.DamageType = DamageClass.Magic;
            Item.crit = 12;
            Item.mana = 26;
            Item.useTime = 8;
            Item.useAnimation = 20;
            Item.reuseDelay = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5.5f;
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TentaculoCosmicoProjectile>();
            Item.shootSpeed = 12f;
            // El sonido del original era un burn grave y rajado: el calco
            // vanilla más cercano con el mismo perfil de pitch (-0.45..-0.6).
            Item.UseSound = SoundID.Item12.WithPitchOffset(-0.5f);
            Item.value = Item.buyPrice(0, 7, 50, 0);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL DISPARO DEL ORIGINAL: cada tentáculo nace con una desviación
            // aleatoria de ±0.7 rad (la nube de gestación).
            Projectile.NewProjectileDirect(source, position, velocity.RotatedByRandom(0.7f),
                ModContent.ProjectileType<TentaculoCosmicoProjectile>(), damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T1",
                "[c/B8FFC8:═══ EL TOMO DE LA APATÍA NULA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "T2",
                "[c/8CE8A4:Escupe tentáculos de vacío que GESTAN 2 segundos sin dañar]"));
            tooltips.Add(new TooltipLine(Mod, "T3",
                "[c/6FD48C:(pulsos que imploden) y luego AZOTAN en tres curvas hacia tu mira]"));
            tooltips.Add(new TooltipLine(Mod, "T4",
                "[c/55B874:curvando el rumbo y teletransportándose entre curva y curva]"));
            tooltips.Add(new TooltipLine(Mod, "T5",
                "[c/78788C:Golpea todo lo que toca (90×90) · cada impacto sucesivo pesa menos\n(×1.0 → ×0.7: el tentáculo se aburre) · ARMA DE PRUEBAS (réplica exacta)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
