using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// RayoPrimordialStaff — v6.50.18 — EL BASTÓN DEL RAYO PRIMORDIAL.
    ///
    /// LA PETICIÓN CON NOMBRE Y APELLIDOS: "investiga los rayos originales
    /// de terraria y crea un arma que los use". Este arma dispara EL RAYO
    /// DEL CLIMA DE TERRARIA — el sistema LightningGenerator/StormLightning
    /// de 1.4.5, portado 1:1 en RayoLib (ver su cabecera): raymarch de 8 px
    /// con 4 capas de ángulo, timón hacia el blanco, des-randomización al
    /// 80%, horquillas reflejadas del quiebre superior, LA OLA de
    /// energizado que nace arriba y se retira apagando el canal — y CERO
    /// sprites (la sección del rayo la hornea código).
    ///
    /// El rayo CAE de 1000 px sobre el cursor (±20° de deriva, como el del
    /// clima), choca con tiles y líquidos (el generador de vanilla), y el
    /// impacto estalla en radial + salta a 2 enemigos + ELECTRIFICA. Sin
    /// maná (regla de la casa para las armas de prueba).
    /// </summary>
    public class RayoPrimordialStaff : ModItem
    {
        /// <summary>Alcance máximo del impacto desde el jugador (px).</summary>
        private const float MaxRange = 640f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoPrimordialProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(-0.35f);
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL RAYO CAE DEL CIELO sobre el cursor (clampeado al alcance):
            // el proyectil NACE en el punto de impacto y ancla la descarga
            // desde 1000 px arriba (el generador Tormenta de vanilla).
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
                "[c/9FE8FF:Invoca el RAYO DE TERRARIA de verdad sobre el cursor]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Cae de 1000 px · horquillas del propio canal · ola de energizado · sin sprite]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
