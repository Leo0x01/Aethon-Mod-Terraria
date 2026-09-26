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
    /// TruenoPerlinStaff — v6.50.17 — EL CETRO DEL TRUENO PERLIN.
    ///
    /// EL ARMA NUEVA DE RAYOS POR RUIDO (la petición del usuario: «crea
    /// otra arma que lance rayos usando otro método como rayos generados
    /// con ruido de perlin… el propio terraria tiene rayos, no uses esos,
    /// solo usa su idea y mejorala»).
    ///
    /// LA DIFERENCIA con el Cetro del Trueno Rúnico (que tira rayos del
    /// CIELO por midpoint displacement fractal): este cetro dispara un
    /// ARCO ELÉCTRICO CONTINUO de la punta del bastón al objetivo — un
    /// canal que MEANDRA con ruido Perlin (fBm de 3 octavas cuya fase
    /// RESBALA cada flick: el rayo serpentea en lugar de
    /// tele-transportarse), con ramas que germinan en los máximos locales
    /// del ruido y UNA ramita de segundo nivel. El arco SIGUE al cursor
    /// durante los primeros ticks (lo arrastras con la mirada) y después
    /// se clava, castiga en radio, SALTA a 2 enemigos y electrifica.
    ///
    /// SIN MANA (la regla de los bastones de prueba de la casa).
    /// </summary>
    public class TruenoPerlinStaff : ModItem
    {
        /// <summary>Alcance máximo del arco desde el jugador (px).</summary>
        private const float MaxRange = 520f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TruenoPerlinProjectile>();
            Item.shootSpeed = 21f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(0.15f);
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL ARCO NACE EN EL OBJETIVO (clampeado a 520 px): el proyectil
            // ES el punto de descarga y el arco cuelga del bastón a él.
            Vector2 muzzle = position + Vector2.Normalize(velocity) * 18f;
            Vector2 target = Main.MouseWorld;
            Vector2 toTarget = target - muzzle;
            float len = toTarget.Length();
            if (len < 24f)
                toTarget = Vector2.Normalize(velocity == Vector2.Zero ? Vector2.UnitX : velocity) * 24f;
            else if (len > MaxRange)
                toTarget *= MaxRange / len;

            Projectile.NewProjectile(source, muzzle + toTarget, Vector2.Zero,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/AEE8FF:Arco eléctrico de ruido Perlin que sigue tu cursor (520 px)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El rayo meandra y serpentrea · salta a 2 enemigos · electrifica · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
