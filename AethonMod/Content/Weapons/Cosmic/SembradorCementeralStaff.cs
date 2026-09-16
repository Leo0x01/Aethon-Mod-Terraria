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
    /// SembradorCementeralStaff — v6.31 — EL SEMBRADOR DEL CEMENTERO ESTELAR.
    ///
    /// Petición del usuario (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md
    /// ficha 1 + §6.1): un bastón que SIEMBRA PÚLSARES — cadáveres estelares
    /// que frenan en seco y se ANCLAN donde estaba el cursor:
    ///
    ///   · EL VUELO: el púlsar sale RÁPIDO (v₀ = distancia/7.375, clamp
    ///     8..64 px/tick) y frena geométricamente (×0.885 por tick) hasta
    ///     DETENERSE exactamente en el punto objetivo.
    ///   · EL ANCLAJE: golpe ×1.0 + kick + retumbo grave.
    ///   · LA ESTRELLA ANCLADA: gira acelerando (tiempo²) y barre con DOS
    ///     HAZES-FARO OPUESTOS de 500 px — cada enemigo tocado recibe ×0.25
    ///     con i-frames propios de 10 ticks.
    ///   · EL TOPE: máximo 3 púlsares en campo — la 4ª siembra se COME a la
    ///     más vieja (la siembra nunca es un cast desperdiciado).
    ///
    /// v6.28 — sin maná (regla del usuario: todos los bastones del mod son
    /// de prueba). Nada de otros mods: todo son nuestras librerías.
    /// </summary>
    public class SembradorCementeralStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 180;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SembradorPulsarProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.35f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL TITULAR del arma (el nombre vive arriba; esto es el subtítulo).
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/8A2BE2:EL SEMBRADOR DEL CEMENTERO ESTELAR]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E6B0FF:Siembra púlsares que frenan en seco y se ANCLAN donde apuntaba tu cursor]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF9EC4:Golpe de anclaje a plena potencia · el giro se acelera y DOS haces-faro opuestos barren a ×0.25]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Máximo 3 púlsares: la nueva siembra se come a la más vieja · la quemadura cósmica · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL TOPE DE 3: la 4ª siembra devora a la estrella más vieja.
            MatarPulsarViejo(player);

            // EL PUNTO OBJETIVO: donde estaba el cursor al disparar. La
            // velocidad inicial resuelve la serie geométrica (0.885¹..0.885²⁶
            // = 7.375·v₀) para que la estrella ATERRICE EXACTO — con el
            // clamp de velocidad, el objetivo se recalcula al punto de
            // aterrizaje real (nunca hay salto en el snap).
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - position;
            Vector2 v0 = delta / SembradorPulsarProjectile.TravelFactor;

            float speed = v0.Length();
            if (speed > 64f) v0 *= 64f / speed;
            else if (speed < 8f)
                v0 = speed > 0.001f ? v0 * (8f / speed) : velocity.SafeNormalize(Vector2.UnitX) * 8f;

            // El punto objetivo EXACTO de aterrizaje (tras el clamp).
            Vector2 aterrizaje = position + v0 * SembradorPulsarProjectile.TravelFactor;

            // La fase del eje magnético al nacer (el desalineado del faro) —
            // viaja en ai[2]: la misma en servidor y clientes.
            float faseMagnetica = Main.rand.NextFloat(MathHelper.TwoPi);

            Projectile.NewProjectile(source, position, v0, type, damage, knockback,
                player.whoAmI, aterrizaje.X, aterrizaje.Y, faseMagnetica);
            return false;
        }

        /// <summary>EL TOPE: busca en Main.projectile los púlsares del mismo
        /// owner; si ya hay 3, mata al MÁS VIEJO (el que menos timeLeft le
        /// queda) — la siembra nunca es un cast desperdiciado.</summary>
        private static void MatarPulsarViejo(Player player)
        {
            int tipo = ModContent.ProjectileType<SembradorPulsarProjectile>();
            Projectile viejo = null;
            int cuenta = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != player.whoAmI)
                    continue;
                cuenta++;
                if (viejo == null || p.timeLeft < viejo.timeLeft)
                    viejo = p;
            }

            if (cuenta >= 3 && viejo != null)
                viejo.Kill();
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
