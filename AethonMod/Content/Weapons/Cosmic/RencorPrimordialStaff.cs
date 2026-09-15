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
    /// RencorPrimordialStaff — v6.29 — EL RENCOR PRIMORDIAL.
    ///
    /// LA PRIMERA DE LOS DOS EXHUMADOS — el arma nacida de la investigación
    /// del RANCOR de Calamity (research/rancor_v629/INFORME_EXHUMADOS.md):
    ///
    ///   · EL CÍRCULO DE TRANSMUTACIÓN rúnico a distancia del jugador (el
    ///     homenaje declarado de Calamity al círculo de Fullmetal Alchemist,
    ///     aquí con las runas de la casa: 10 doradas CW + 6 violetas CCW +
    ///     LA ESTRELLA de 5 puntas dibujada con cápsulas).
    ///   · 3 SEGUNDOS DE CARGA (180 ticks — el número exacto de Calamity):
    ///     las runas se encienden UNA A UNA, la bruma espirala HACIA el
    ///     círculo, las ascuas orbitan y el Telegraph del haz avisa.
    ///   · EL HAZ: "The Angy Beam" — un desgarro CONTINUO carmesí-ámbar
    ///     (RiftLib.Tear: UN SOLO QUAD, cero juntas) de 880 px que perfora
    ///     INFINITO (atraviesa paredes y todo lo que viva).
    ///   · AL TOCAR TILE: LOS BRAZOS ESPECTRALES (×0.66 — 4 brazos de
    ///     cápsulas que brotan de la superficie) + LAS ASCUAS (×0.33 —
    ///     PyraLib.Sparks de rampa SolarFire + daño de área) + la fog y el
    ///     resplandor de lava de Calamity (BrumaFX + luz ámbar).
    ///   · LOS ENEMIGOS DEL HAZ MUEREN EN CENIZA (la muerte con firma).
    ///
    /// Sin maná (regla de la casa). Rareza Purple — la jerarquía especial
    /// de los exhumados.
    /// </summary>
    public class RencorPrimordialStaff : ModItem
    {
        /// <summary>Distancia máxima del círculo al jugador (px).</summary>
        private const float MaxRange = 380f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RencorPrimordialProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.35f);
            Item.value = 15000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === EL CÍRCULO NACE A DISTANCIA DEL JUGADOR (la regla de Calamity) ===
            // Clampeado a MaxRange en la dirección del cursor; el haz disparará
            // EN LA MISMA DIRECCIÓN jugador→círculo (continúa a través del punto).
            Vector2 aim = Vector2.Normalize(
                Main.MouseWorld - player.Center);
            if (aim == Vector2.Zero) aim = Vector2.UnitX;

            Vector2 circlePos = player.Center + aim * MaxRange;

            Projectile.NewProjectile(source, circlePos, aim,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FF7A9A:EL RENCOR PRIMORDIAL — la forma exhumada]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/C9B8FF:Clava un círculo de transmutación rúnico a 380 px]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/C9B8FF:3 s de carga → EL HAZ CONTINUO carmesí-ámbar que perfora TODO]"));
            tooltips.Add(new TooltipLine(Mod, "D4",
                "[c/FFD66B:Al tocar pared: brazos espectrales (×0.66) y ascuas (×0.33)]"));
            tooltips.Add(new TooltipLine(Mod, "D5",
                "[c/78788C:Los enemigos del haz se desintegran en ceniza · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
