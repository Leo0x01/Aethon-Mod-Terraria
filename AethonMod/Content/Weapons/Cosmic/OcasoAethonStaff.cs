using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// OcasoAethonStaff — v6.27 — EL BASTÓN DEL OCASO DE AETHON.
    ///
    /// El ARMA SUPREMA del patrón gauge (petición del usuario: "aplica el
    /// patrón gauge del Cosmic Destroyer a un arma suprema"). La trinidad
    /// del informe de investigación v6.26 (el informe de investigación v6.26,
    /// lección 10), traducida al lenguaje del arsenal:
    ///
    ///   · CARGA (El Fragmento): disparo rápido de fragmentos del ocaso
    ///     (150) — CADA IMPACTO suma +3 al aro medidor de la cabeza.
    ///   · BURST (El Ocaso): aro lleno → CLIC DERECHO igniciona 8 s donde
    ///     cada disparo es UNA MUERTE DE ESTRELLA: mini-eclipse a daño ×3
    ///     (450) con EJECUCIÓN (+50%) bajo el 50% de vida del objetivo.
    ///   · LOCKOUT (La Sobrecalentada): al apagarse el ocaso, 2 s de
    ///     castigo donde el bastón HUMEA y no dispara; el aro renace en 0.
    ///
    /// Los números del informe: gauge 100 (34 impactos ≈ 12 s de fuego),
    /// modo 480 ticks, ×3 de daño, execute <50% HP, lockout de castigo —
    /// y las dos lecciones de peso: NUNCA cortar el burst en seco (el
    /// corte tiene su lamento de vapor) y la UI ligera con fade-out.
    /// Sin maná (regla de la casa v6.26): el costo del arma ES el gauge.
    /// </summary>
    public class OcasoAethonStaff : ModItem
    {
        /// <summary>El casteo normal del fragmento.</summary>
        private const int UsoNormal = 14;
        /// <summary>El casteo de la muerte de estrella (la lluvia del ocaso).</summary>
        private const int UsoOcaso = 20;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = UsoNormal;
            Item.useAnimation = UsoNormal;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OcasoShardProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0;                 // el costo ES el gauge
            Item.noMelee = true;
            Item.rare = ItemRarityID.Red;  // suprema
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.3f);
            Item.value = 50000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "O",
                "[c/FF8C40:EL OCASO DE AETHON]"));
            tooltips.Add(new TooltipLine(Mod, "O2",
                "[c/E6B0FF:El arma suprema del medidor: carga → estallido → bloqueo]"));
            tooltips.Add(new TooltipLine(Mod, "O3",
                "[c/FFD66B:Cada impacto de un fragmento carga el aro (34 impactos lo llenan)]"));
            tooltips.Add(new TooltipLine(Mod, "O4",
                "[c/FF7A40:Con el aro lleno, CLIC DERECHO igniciona EL OCASO: 8 s de muertes de estrella ×3]"));
            tooltips.Add(new TooltipLine(Mod, "O5",
                "[c/FF5A40:Bajo el 50% de vida del objetivo, la muerte EJECUTA (+50%)]"));
            tooltips.Add(new TooltipLine(Mod, "O6",
                "[c/78788C:Después, 2 s de sobrecalentada: el bastón humea y calla · sin maná]"));
        }

        // ==================================================================
        //  LA TRINIDAD — clic izquierdo dispara, clic derecho MANDA
        // ==================================================================

        /// <summary>El clic derecho es el ACTIVADOR del ocaso (el
        /// equivalente de la casa al clic derecho del gauge del informe).</summary>
        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            var op = player.GetModPlayer<OcasoPlayer>();

            // === CLIC DERECHO: SOLO con el aro lleno y fuera de estados ===
            if (player.altFunctionUse == 2)
                return op.Gauge >= OcasoPlayer.GaugeMax &&
                       !op.OcasoActivo && !op.Sobrecalentado;

            // === EL LOCKOUT: el bastón humea y calla ===
            if (op.Sobrecalentado) return false;

            // === LA CADENCIA: en el ocaso la lluvia es más lenta pero ×3 ===
            Item.useTime = Item.useAnimation = op.OcasoActivo ? UsoOcaso : UsoNormal;
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var op = player.GetModPlayer<OcasoPlayer>();

            if (player.altFunctionUse == 2)
            {
                // LA IGNICIÓN: el telegraph vive en el FX del propio gauge
                // (Kick + Flash + corona de chispas + el trueno grave).
                op.IntentarActivar();
                return true;
            }
            return null;   // el disparo normal fluye por Shoot()
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var op = player.GetModPlayer<OcasoPlayer>();
            op.UiFade = 1f;   // el aro medidor despierta al disparar

            if (op.OcasoActivo)
            {
                // === LA MUERTE DE ESTRELLA: daño ×3 (el número del informe)
                //     y knockback doble — la gravedad de una estrella que muere ===
                Projectile.NewProjectile(source, position, velocity,
                    ModContent.ProjectileType<OcasoBurstProjectile>(),
                    (int)(damage * OcasoPlayer.MultiplicadorOcaso),
                    knockback * 2f, player.whoAmI);
                return false;
            }

            // === EL FRAGMENTO DEL OCASO (la carga del aro) ===
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }

        public override void UpdateInventory(Player player)
        {
            // El ítem reajusta su cadencia si quedó mutada de un ocaso
            // anterior (higiene al soltar/entrar al mundo).
            var op = player.GetModPlayer<OcasoPlayer>();
            if (!op.OcasoActivo && !op.Sobrecalentado)
            {
                Item.useTime = UsoNormal;
                Item.useAnimation = UsoNormal;
            }
        }
    }
}
