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
    /// DecretoEclipseStaff — v6.31 — "EL DECRETO DEL ECLIPSE"
    /// (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md ficha 4 + §6.4).
    ///
    /// El decreto del dragón, destilado a la casa como arma independiente:
    /// cada uso CLAVA un círculo de eclipse en el punto del cursor —
    ///
    ///   · EL CÍRCULO CRECE: nace de 80 px y gana +2 px/tick hasta 660 px
    ///     durante toda su vida (~290 ticks); mientras vive, el mundo se
    ///     OSCURECE (es un eclipse).
    ///   · LA EJECUCIÓN CÍCLICA: cada 15 ticks una onda viaja del centro
    ///     al borde (~8 ticks) y al llegar TODO enemigo del círculo recibe
    ///     un corte por PRIORIDAD — el más cercano al centro paga ×4.8,
    ///     decayendo hasta ×0.2 — más Quemadura Cósmica 3 s.
    ///   · LA MARCA DEL OJO: cada enemigo del área lleva un ojo que lo
    ///     mira; su pupila vertical SE CONTRAE justo antes del tajo.
    ///   · LOS GLIFOS: 24 glifos rúnicos alrededor del anillo que se
    ///     RE-ESCRIBEN en cada ejecución.
    ///
    /// Solo UN decreto en campo: el nuevo disparo se come al previo.
    /// v6.28 — sin maná (regla de la casa: bastones de prueba). Nada de
    /// otros mods: todo son nuestras librerías.
    /// </summary>
    public class DecretoEclipseStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            // El ciclo completo del decreto por click, una y otra vez.
            Item.useTime = 45; Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DecretoEclipseProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.3f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL TITULAR del arma (el nombre vive arriba; esto es el subtítulo).
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/9A5CFF:EL DECRETO DEL ECLIPSE]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/C9A0FF:Clava un CÍRCULO DE ECLIPSE que crece hasta 660 px mientras oscurece el mundo]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFD680:Cada 15 ticks la onda llega al borde y EJECUTA: el más cercano al centro paga ×4,8]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:La pupila del ojo se contrae antes del tajo · quemadura cósmica 3 s · un decreto en campo · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL TOPE DE 1 (la ficha 4): mientras vive el círculo no puede
            // abrirse otro decreto — el nuevo se come al previo.
            MatarPrevio(player);

            // EL CÍRCULO SE CLAVA en el punto del cursor: proyectil estático
            // en el mundo (como el desgarro), sin vuelo.
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type,
                damage, knockback, player.whoAmI);
            return false;
        }

        /// <summary>EL TOPE DE 1: busca en Main.projectile el círculo del
        /// decreto de este owner y lo mata — el decreto nuevo empieza limpio.</summary>
        private static void MatarPrevio(Player player)
        {
            int tipo = ModContent.ProjectileType<DecretoEclipseProjectile>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != player.whoAmI)
                    continue;
                p.Kill();
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
