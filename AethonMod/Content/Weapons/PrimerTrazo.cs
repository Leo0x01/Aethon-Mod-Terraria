using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// PRIMERTRAZO — v6.52.0 — EL PRIMER TRAZO: el arma del RAYO MASIVO
    /// de energía (la letra del usuario: «e visto en varios mods qué sus
    /// jefes son capaces de lanzar rayos de energía masivos, que en teoría
    /// serían una texturas de ruido de perlin moviéndose en una dirección
    /// con iluminación, glow, desenfoque, desenfoque de movimiento… ¿por
    /// qué no podemos hacer eso?»).
    ///
    /// v6.53 — LA PURGA DEL ESCRIBA: el jefe demo y sus plumas murieron
    /// (la letra: «el arma La Pluma Consciente no sirve, bórrala al igual
    /// que Pluma Convocadora y EL ESCRIBA y todo lo referente») — el TRAZO
    /// SOBREVIVE y muda de familia: ya no es del ciclo del Escriba, es EL
    /// ARMA DEL RAYO del arsenal de la casa (Bolsa de las Armas de Rayo).
    /// Su render vive la técnica DEL RAYO DE FUSIÓN DE GOOZMA (Hunt of the
    /// Old God — la letra: «la respuesta es el rayo de fusión de goozma
    /// pero necesitamos saber cómo hacerlo»).
    ///
    /// LA RESPUESTA ARMADA: el Last Prism de vanilla llevado a la receta
    /// moderna de 7 capas (la investigación R2: Calamity + Fargo's +
    /// Stars Above + Calamity Overhaul decompilados). Canalizada (mantén
    /// clic): CINCO fajos convergen en UN rayo masivo de ruido de Perlin
    /// — espina fBm viva, halo + cuerpo + core, diamantes de energía que
    /// RECORREN el rayo, luz dinámica a lo largo de TODO el cuerpo y la
    /// punta que se DESLIZA al barrer (el motion-blur del Lerp 0.92).
    ///
    /// Arma de PRUEBA de la casa (como la Sombra II): SIN coste de maná.
    /// </summary>
    public class PrimerTrazo : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Tooltip viven en la localización (regla de la
            // casa) — Items.PrimerTrazo.
        }

        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 10;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                // LA CANALIZADA: mantén el clic
            Item.noMelee = true;
            Item.noUseGraphic = true;           // dibuja el proyectil sostenido
            Item.mana = 0;                      // arma de prueba: SIN maná
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PrimerTrazoHeld>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = null;               // el zumbido lo lleva el Held
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo trazo sostenido por portador (el patrón de la fauce).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PrimerTrazoHeld>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El CONTROLADOR nace en la mano (la receta vanilla 633): él
            // aiming, la convergencia y los CINCO fajos viven en su AI.
            Vector2 mira = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(mira.X) || float.IsNaN(mira.Y)) mira = -Vector2.UnitY;
            Projectile.NewProjectile(source, player.MountedCenter, mira,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE REPUESTO de la casa (la tradición de las armas de
            // prueba): madera de siempre — el trazo se practica gratis.
            CreateRecipe().AddIngredient(ItemID.Wood, 10)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // La línea de la casa para las armas de prueba (la misma voz
            // de la Sombra II): el jugador debe SABER que puede abusar.
            TooltipLine linea = new TooltipLine(Mod, "PrimerTrazoPrueba",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.PrimerTrazo.Prueba"));
            linea.OverrideColor = new Color(255, 240, 180);
            tooltips.Add(linea);
        }
    }
}
