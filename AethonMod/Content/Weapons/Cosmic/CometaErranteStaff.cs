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
    /// CometaErranteStaff — EL COMETA ERRANTE.
    ///
    /// Un bastón que captura un cometa y lo ata a una ÓRBITA ELÍPTICA
    /// alrededor del lanzador (semieje mayor 180 px, achatada, con
    /// precesión lenta del plano). El núcleo de hielo-fuego golpea a
    /// plena potencia; la LARGA cola de polvo —que siempre apunta
    /// radialmente AFUERA de la órbita, como los cometas reales huyen
    /// del sol— pica más suave. Tras ~6 vueltas el cometa se suelta y
    /// se lanza en cacería contra el enemigo más cercano, estallando
    /// en estelas de hielo-fuego. Un solo cometa activo: el nuevo
    /// reemplaza al viejo.
    /// </summary>
    public class CometaErranteStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 140;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 35; Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CometaErranteProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.25f);
            Item.value = 22000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/A8D8FF:EL COMETA ERRANTE]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E8F4FF:Captura un cometa en ÓRBITA ELÍPTICA a tu alrededor durante 6 vueltas]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/6FB7FF:El núcleo de hielo-fuego golpea a plena potencia · la cola de polvo pica a ×0.4]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:La cola apunta SIEMPRE hacia afuera de la órbita · tras 420 ticks se lanza en cacería y estalla · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL TOPE DE 1: un solo cometa errante — el nuevo libera al viejo.
            MatarCometaViejo(player);

            // La dirección del cursor al disparar: el cometa NACE por ese lado
            // de la órbita (la fase inicial delipse apunta al cursor).
            Vector2 dir = velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(velocity)
                : Vector2.Normalize(Main.MouseWorld - position);

            // EL SENTIDO DEL GIRO: alternado por casteo — determinista, sin
            // Main.rand (viaja en ai[0]: la misma órbita en servidor y clientes).
            float giro = (Main.GameUpdateCount / 35) % 2 == 0 ? 1f : -1f;

            Projectile.NewProjectile(source, player.MountedCenter + dir * 34f, dir * 4f,
                type, damage, knockback, player.whoAmI,
                giro, dir.ToRotation(), 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: mata al cometa del mismo owner (si vive alguno).</summary>
        private static void MatarCometaViejo(Player player)
        {
            int tipo = ModContent.ProjectileType<CometaErranteProjectile>();
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
