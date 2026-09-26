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
    /// StormRuneStaff — v6.21 — EL CETRO DEL TRUENO RÚNICO.
    ///
    /// RECONSTRUIDO con StormLib (la librería de la investigación v6.21):
    /// el cetro INVOCA RAYOS DEL CIELO sobre el cursor — telegraph de
    /// aviso + descarga multi-filamento (tronco dorado + acompañantes
    /// azul-estelar + ramas) que CAE de ~700-980 px encima del objetivo,
    /// golpea en COLUMNA, estalla en radial, SALTA a 3 enemigos y
    /// ELECTRIFICA. Alcance 560 px.
    ///
    /// v6.21 — SIN MANA (regla del usuario: todos los bastones del mod
    /// son de prueba).
    /// </summary>
    public class StormRuneStaff : ModItem
    {
        /// <summary>Alcance máximo del rayo desde el jugador (px).</summary>
        private const float MaxRange = 560f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RunicLightning>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(-0.2f);
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL RAYO CAE DEL CIELO sobre el cursor (clampeado a 560 px del
            // jugador): el proyectil NACE en el punto de impacto y ancla
            // su descarga desde arriba.
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
            // v6.18: TOOLTIP CORTO — dos líneas, el nombre vive arriba.
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/BFE8FF:Invoca rayos del cielo sobre el cursor (560 px)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Golpea en columna · salta a 3 enemigos · electrifica · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
