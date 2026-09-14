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
    /// TormentaNebularStaff — v6.24 — EL CETRO DE LA TORMENTA NEBULAR.
    ///
    /// LA SEGUNDA DE LAS TRES ARMAS DE LAS LIBRERÍAS (petición del
    /// usuario: "no olvides crear varias armas nuevas que usen todas
    /// nuestras librerías") — la tormenta es un CONCIERTO de las tres:
    ///
    ///   · BRUMAFX (la nube)  → LA TORMENTA EN SÍ: una nube viva de
    ///     niebla violeta-cian colgada del cielo, respirando y derivando.
    ///   · STORMLIB (el rayo) → las DESCARGAS: cada pocos latidos, un
    ///     multi-filamento CAE de la panza de la nube al punto marcado —
    ///     telegraph, golpe en área y trueno.
    ///   · LUMENLIB (la luz)  → el telegraph de cada descarga + el aurora
    ///     prismático latiendo DENTRO de la nube + los destellos.
    ///   · LAS RUNAS          → dos círculos rúnicos orbitando el borde de
    ///     la tormenta (la forma de los agujeros negros, CW/CCW).
    ///
    /// Invoca la tormenta sobre el cursor (560 px): durante 5 segundos
    /// la nube desencadena descargas repetidas en su zona.
    ///
    /// v6.24 — SIN MANA (regla del usuario: todos los bastones del mod
    /// son de prueba).
    /// </summary>
    public class TormentaNebularStaff : ModItem
    {
        /// <summary>Alcance máximo de la tormenta desde el jugador (px).</summary>
        private const float MaxRange = 560f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 48; Item.useAnimation = 48;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TormentaNebularProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.3f);
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA TORMENTA NACE SOBRE EL CURSOR (clampeada a 560 px).
            Vector2 muzzle = position + Vector2.Normalize(
                velocity == Vector2.Zero ? Vector2.UnitX : velocity) * 18f;
            Vector2 target = Main.MouseWorld;
            Vector2 toTarget = target - muzzle;
            float len = toTarget.Length();
            if (len < 24f)
                toTarget = Vector2.UnitX * 24f;
            else if (len > MaxRange)
                toTarget *= MaxRange / len;

            Projectile.NewProjectile(source, muzzle + toTarget, Vector2.Zero,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/C9B8FF:Invoca una tormenta nebular sobre el cursor (560 px)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Nube viva de bruma · descargas encadenadas durante 5 s · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
