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
    /// SuturaCuanticaStaff — LA SUTURA CUÁNTICA (v6.33).
    ///
    /// Uno de LOS CUATRO DESGARROS NUEVOS nacidos de las referencias del
    /// usuario (el "desgarro de realidad cuántica"): el bastón abre en el
    /// cursor un DESGARRO CIRCULAR GLITCH — la realidad rota como código
    /// corrupto: el círculo irregular fragmentado con astillas rectangulares
    /// cian/violeta que parpadean inestables, el núcleo magenta nebuloso y
    /// rayos que escapan del borde.
    ///
    /// MECÁNICA: el desgarro vive 5 s en el punto del cursor. TODO enemigo
    /// dentro del círculo recibe el daño de la corrupción (i-frames 20) y
    /// cada 45 ticks el desgarro DISPARA RAYOS CUÁNTICOS desde su borde
    /// contra los 3 enemigos más cercanos (×0.6). Al cerrar: implosión.
    /// </summary>
    public class SuturaCuanticaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 96;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DesgarroCuanticoProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 18; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.35f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/00E5FF:LA SUTURA CUÁNTICA]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/7C4DFF:Abre un DESGARRO GLITCH donde apuntas: la realidad rota como código corrupto]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF4081:Corrompe a TODO enemigo dentro del círculo · cada 45 ticks dispara RAYOS CUÁNTICOS a los 3 más cercanos ×0.6]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El círculo irregular late inestable 5 segundos · al cerrar, la corrupción implode]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Nace EXACTO en el cursor: el desgarro no viaja — se ABRE ahí.
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }
    }

    /// <summary>
    /// PortalDimensionalStaff — EL PORTAL DIMENSIONAL (v6.33).
    ///
    /// La referencia del usuario: el portal de ANILLOS CONCÉNTRICOS
    /// perfectos — la puerta CONSTRUIDA (simetría, control): 6 anillos con
    /// rotación jerárquica, gradiente cian→magenta, glifos rúnicos y el
    /// núcleo blanco de la otra dimensión respirando.
    ///
    /// MECÁNICA: el portal vive 6 s en el cursor y ejerce SUCCIÓN suave
    /// sobre los enemigos en 300 px (la puerta tira de ellos) — al cruzar
    /// el núcleo (radio 60) los TRITURA cada 18 ticks. Un solo portal.
    /// </summary>
    public class PortalDimensionalStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 88;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 32; Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PortalDimensionalProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 22; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.30f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/00B0FF:EL PORTAL DIMENSIONAL]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/D500F9:Abre la puerta de anillos donde apuntas: 6 aros girando en jerarquía con glifos rúnicos]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/E0F7FA:SUCCIÓN suave en 300 px · el que cruza el núcleo blanco es TRITURADO por la otra dimensión]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El portal respira 6 segundos · un solo portal activo]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarPortalViejo(player);
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: cierra el portal anterior del mismo owner.</summary>
        private static void MatarPortalViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PortalDimensionalProjectile>())
                    p.Kill();
            }
        }
    }
}
