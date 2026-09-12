using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// MedusaNebularStaff — LA MEDUSA NEBULAR (arma invocadora, v5.97).
    ///
    /// Petición del usuario: "crea una nueva arma que sea un invocador para
    /// un minion, este minion debe ser algo que hayas creado, crea un
    /// proyectil super creativo y cosmico, y este proyectil sera la
    /// invocacion".
    ///
    /// EL MINION ES EL PROYECTIL: una medusa nacida en el corazón de una
    /// nebulosa — la criatura del arsenal que NADIE ha visto: una campana
    /// translúcida de gas interestelar (teal-esmeralda con margen
    /// bioluminiscente rosa) que guarda dentro una MINIGALAXIA ESPIRAL que
    /// gira lentamente, y seis tentáculos de cuentas estelares que ondean
    /// con física propia. NO vuela como los demás minions: NADA — se
    /// propulsa con PULSOS rítmicos de su campana (contracción → impulso →
    /// deriva acuática), exactamente como una medusa real nada en el océano,
    /// solo que el océano es el AIRE y la medusa curva sutilmente el
    /// espaciotiempo a su paso (fuente sutil del pase B del
    /// BlackHoleLensSystem: donde nada, el fondo se dobla un poco).
    ///
    /// ATQUE — los NEMATOCISTOS: cuando la campana se contrae cerca de una
    /// víctima, DESCARGA UN RAYO QUE CAE DEL CIELO (NebulaLightning) con
    /// QUEMADURA DE HIELO (Frostburn — la quemadura fría del vacío) + el
    /// contacto de la campana daña. Al desvanecer: se disuelve en polvo de
    /// estrellas.
    ///
    /// Especificaciones:
    ///   - damage = 32 (DamageClass.Summon — escala con daño de invocación)
    ///   - mana = 10, useTime = 36, knockBack 2, autoReuse, noMelee
    ///   - minionSlots = 1 (invoca varias medusas con más espacio)
    /// </summary>
    public class MedusaNebularStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 32;
            Item.DamageType = DamageClass.Summon;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 10;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;           // el sello de invocación vanilla
            Item.shoot = ModContent.ProjectileType<NebulaJellyfishMinion>();
            Item.shootSpeed = 10f;
            // (Nota: este tML ya no tiene Item.buff — el buff lo aplica Shoot()
            // manualmente y la propia medusa lo sostiene, patrón CosmicOrb.)
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La medusa nace delante del jugador y emerge ABIERTO (el minion
            // sostiene el buff — patrón del CosmicOrb: 2 ticks bastan porque
            // su IA lo refresca mientras viva).
            player.AddBuff(ModContent.BuffType<NebulaJellyfishBuff>(), 2);
            Vector2 spawnPos = player.Center + velocity.SafeNormalize(Vector2.Zero) * 48f;
            Projectile.NewProjectile(source, spawnPos, velocity * 0.4f, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "MN_Title",
                "[c/17D6AA:═══ LA MEDUSA NEBULAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "MN_Desc",
                "[c/9EF2DC:Invoca una medusa de nebulosa: su campana translúcida guarda una MINIGALAXIA que gira, y nada por el aire a PULSOS — como en un océano que no existe]"));
            tooltips.Add(new TooltipLine(Mod, "MN_Desc2",
                "[c/C88BE8:Sus tentáculos de cuentas estelares ondean con vida propia, y donde nada el fondo se curva sutilmente a su paso]"));
            tooltips.Add(new TooltipLine(Mod, "MN_Desc3",
                "[c/FF9AD4:Cuando su campana se contrae junto a una víctima, cae un RAYO NEBULAR DEL CIELO sobre ella — con QUEMADURA DE HIELO del vacío]"));
            tooltips.Add(new TooltipLine(Mod, "MN_Desc4",
                "[c/17D6AA:Invoca varias medusas si tienes espacio de sirvientes]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
