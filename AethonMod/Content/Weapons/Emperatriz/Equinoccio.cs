using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons.Emperatriz
{
    /// <summary>
    /// EL EQUINOCCIO — LA CUARTA ARMA: OPCIÓN B DUAL.
    ///
    /// Mitad aurora, mitad eclipse. Dispara DOS Nightglow vanilla (931)
    /// por uso — la opción B de nuevo (IA de Terraria intacta), pero
    /// con las dos mitades del día en un solo libro:
    ///
    ///   · EL DORADO (ai[2]=9201, ai[1]=0.64 → render DORADO): sale con
    ///     LUZ DE SOL — luz dorada intensa, halo pulsante y los OCHO
    ///     RAYOS giratorios de la casa alrededor del orbe. El día.
    ///   · EL NEGRO (ai[2]=9202, ai[1]=0.28 → render VIOLETA): exhala
    ///     BRUMA — LA BRUMA DE VERDAD de la casa (BrumaFX, el humo fBm
    ///     que se desgarra) sobre todo su rastro, un DISCO DE ECLIPSE
    ///     (la capa alfa que oscurece) con rim violeta pulsante y
    ///     chispas de llama de sombra. La noche.
    ///
    /// La salida es la FUENTE de vanilla dividida: el dorado se va hacia
    /// un lado y el negro hacia el otro — el día y la noche separándose
    /// sobre tu cabeza, y luego cada uno caza por su cuenta (el homing
    /// del 931 hace el resto).
    ///
    /// Sin maná (regla de la casa — arsenal de pruebas).
    /// </summary>
    public class Equinoccio : ModItem
    {
        // el anti-doble de la casa (mismo frame)
        private static uint _ultimoFrame = 0;

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item82;
            Item.autoReuse = true;
            Item.shoot = 931;   // Nightglow — el vanilla de verdad (el número, como GrimoireEternal)
            Item.shootSpeed = 17f;
            Item.mana = 0;                    // regla de la casa: sin maná
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el anti-doble de la casa
            uint frame = Main.GameUpdateCount;
            if (frame == _ultimoFrame) return false;
            _ultimoFrame = frame;

            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // === LA FUENTE DIVIDIDA: la velocidad de vanilla (aleatoria,
            // siempre hacia arriba) separada en dos mitades — el dorado
            // a la izquierda, el negro a la derecha ===
            Vector2 baseV = Main.rand.NextVector2Circular(1f, 1f)
                          + Main.rand.NextVector2CircularEdge(3f, 3f);
            if (baseV.Y > 0f) baseV.Y *= -1f;

            // === EL DORADO — EL DÍA ===
            // ai[1] = 0.64: el render del 931 hace hslToRgb((hue+0.5)%1)
            // → matiz 0.14 = DORADO de verdad
            Projectile.NewProjectile(source, pos, baseV.RotatedBy(-0.35f),
                931, damage, knockback, player.whoAmI,
                -1f,     // ai[0]: sin objetivo aún
                0.64f,   // ai[1]: EL MATIZ DORADO
                9201f);  // ai[2]: la marca del FX dorado

            // === EL NEGRO — LA NOCHE ===
            // ai[1] = 0.28 → render matiz 0.78 = VIOLETA: el rim que
            // sangra a través de la bruma del eclipse
            Projectile.NewProjectile(source, pos, baseV.RotatedBy(0.35f),
                931, damage, knockback, player.whoAmI,
                -1f,     // ai[0]: sin objetivo aún
                0.28f,   // ai[1]: el matiz violeta (el rim del eclipse)
                9202f);  // ai[2]: la marca del FX negro

            return false;   // los dos spawns son nuestros
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
