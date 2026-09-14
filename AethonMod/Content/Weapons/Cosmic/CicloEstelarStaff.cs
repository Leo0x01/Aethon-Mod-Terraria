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
    /// CicloEstelarStaff — v6.26 — EL BASTÓN DEL CICLO ESTELAR.
    ///
    /// Petición del usuario: "crea un baston que simule el ciclo de vida
    /// completo de una estrella que se convierte en super nova y luego lo
    /// que siga en su ciclo de vida".
    ///
    /// EL ARMA QUE DISPARA UNA VIDA: cada disparo engendra una estrella
    /// que VIVE su biografía completa (~18 s) en cinco actos — la
    /// nebulosa que se contrae, la ignición de la secuencia principal,
    /// el hinchazón de la gigante roja, el colapso y LA SUPERNOVA, y el
    /// remanente: la estrella de neutrones pulsante que se apaga. La
    /// estela del camino cuenta la historia (violeta → dorado → rojo →
    /// blanco → azul).
    ///
    /// Daño 300 (la nova revienta a ×2 = 600 en 650 px), useTime 60, sin
    /// maná, HoldUp, autoReuse, rareza Quest, valor alto — el bastón de
    /// los 5 actos.
    /// </summary>
    public class CicloEstelarStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip son LOS CINCO ACTOS.
        }

        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CicloEstelarProjectile>();
            Item.shootSpeed = 7f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 50000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El proyectil gestiona su semilla y su línea de tiempo en
            // OnSpawn; aquí solo le damos el impulso (deriva lenta).
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL GUION DE LOS 5 ACTOS — el tooltip ES el programa de la obra.
            tooltips.Add(new TooltipLine(Mod, "CicloHeader",
                "[c/C9A6FF:EL CICLO DE VIDA DE UNA ESTRELLA — vive, muere y deja un cadáver pulsante]"));
            tooltips.Add(new TooltipLine(Mod, "CicloActo1",
                "[c/9B7BD8:I · NEBULOSA — la nube se contrae (aura de frío 25%)]"));
            tooltips.Add(new TooltipLine(Mod, "CicloActo2",
                "[c/FFD080:II · SECUENCIA PRINCIPAL — la ignición (aura ardiente 45%)]"));
            tooltips.Add(new TooltipLine(Mod, "CicloActo3",
                "[c/FF8A5A:III · GIGANTE ROJA — se hincha ×2.2 y pierde capas (aura 35%)]"));
            tooltips.Add(new TooltipLine(Mod, "CicloActo4",
                "[c/FFF6E8:IV · SUPERNOVA — el colapso revienta: ×2 de daño en 650 px]"));
            tooltips.Add(new TooltipLine(Mod, "CicloActo5",
                "[c/9CD0FF:V · EL REMANENTE — la estrella de neutrones pulsante (aura ×1.5)]"));
            tooltips.Add(new TooltipLine(Mod, "CicloPie",
                "[c/78788C:18 segundos de astrofísica · la estela cuenta el camino: violeta → dorado → rojo → blanco → azul]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
