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
    /// VenaTruenoStaff — v6.50.24 — EL COLMILLO DE VENA TRUENO.
    ///
    /// LA PETICIÓN: "para los rayos crea una nueva arma basada en
    /// thundervein dragon Coralite mod terraria". El Thundervein Dragon
    /// (荒雷龙) es el wyvern eléctrico de Coralite — el jefe que vive en las
    /// nubes de tormenta, cuerpo negro cruzado por VENAS DE TRUENO
    /// amarillas y naranjas. Este arma es su colmillo: convoca LA CAÍDA
    /// DEL DRAGÓN sobre el cursor — el ThunderFalling (落雷) del jefe,
    /// TRES rayos en trío (1 naranja 219,114,22 + 2 amarillos 255,202,101
    /// — los colores exactos de su código) que caen del cielo creciendo,
    /// PARPADEANDO y desintegrándose al morir.
    ///
    /// El rayo es el de RayoLib (el puerto 1:1 del LightningGenerator de
    /// vanilla 1.4.5) con la FIRMA VENA TRUENO del RayoSistema: CERO
    /// sprites — el pincel es el pixel 1×1 del motor. Telegrafiada como
    /// la caída del dragón: 26 ticks de caída, el golpe aterriza al final
    /// (radial + cadena de 3 + Electrified). Sin maná (regla de la casa
    /// para las armas de prueba).
    /// </summary>
    public class VenaTruenoStaff : ModItem
    {
        /// <summary>Alcance máximo del blanco desde el jugador (px).</summary>
        private const float MaxRange = 680f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 155;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VenaTruenoProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(-0.5f);
            Item.value = 16000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA CAÍDA DEL DRAGÓN sobre el cursor (clampeado al alcance):
            // el proyectil vive EN EL BLANCO y telegrafía la caída — el
            // trío germina de 1000 px encima y desciende.
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
                "[c/FFCA65:Convoca LA CAÍDA DEL DRAGÓN DE VENA TRUENO sobre el cursor]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El trío de Coralite (1 naranja + 2 amarillos) · telegrafiada: golpea al aterrizar · cadena de 3 · sin sprite]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
