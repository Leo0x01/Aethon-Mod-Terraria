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
    /// ArcoSobretensionStaff — v6.50.18 — EL ARCO DE SOBRETENSIÓN.
    ///
    /// LA SEGUNDA PETICIÓN CON NOMBRE Y APELLIDOS: "investiga el
    /// funcionamiento de Arc Surge, así es como son los rayos de verdad".
    /// LA INVESTIGACIÓN (R58-a): Arc Surge NO es de ningún mod — es el arma
    /// MÁGICA VANILLA de Terraria 1.4.5 (drop 1/50 del Platillo Marciano)
    /// que dispara ARCOS INSTANTÁNEOS ROJOS de la mano al cursor: rayos
    /// generados con el MISMO LightningGenerator (preset
    /// GetArcSurgeWeaponGenerator: 5 capas al 1.2, paso de 6 px, los
    /// parámetros remapeados por distancia), enganchados a la mano del
    /// jugador con caída cuártica, vida de 16 ticks con fondo de opacidad
    /// 0.3 (NUNCA invisible del todo) y hasta 2 arcos extra a enemigos
    /// cercanos en un cono de 60°.
    ///
    /// ESTE arma es ESO: el arco carmesí mano→cursor, instantáneo, con sus
    /// 2 arcos hermanos a los enemigos del cono. Sin maná (regla de la
    /// casa para las armas de prueba).
    /// </summary>
    public class ArcoSobretensionStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 180;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ArcoSobretensionProjectile>();
            Item.shootSpeed = 8f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            // EL ZAP del Arc Surge de vanilla (Item15).
            Item.UseSound = SoundID.Item15;
            Item.value = 15000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === EL PUNTO DE ALCANCE (el ritual de vanilla ItemCheck_Shoot
            //     del 6173): el cursor, empujado al alcance mínimo de 160 px
            //     de la mano si se acuesta al jugador, con el jitter ±20. ===
            Vector2 mano = player.RotatedRelativePoint(player.Center);
            Vector2 apuntado = Main.MouseWorld;
            Vector2 aim = Vector2.Normalize(apuntado - mano);
            if (!float.IsFinite(aim.X) || !float.IsFinite(aim.Y) || aim == Vector2.Zero)
                aim = Vector2.UnitX * player.direction;
            float dist = Vector2.Distance(apuntado, mano);
            if (dist < 160f)
                apuntado += aim * (160f - dist);
            apuntado += Main.rand.NextVector2Circular(20f, 20f);

            // El proyectil NACE en el punto de impacto (1 tick, como el
            // 1122 de vanilla: "solo duran un tick aunque el visual viva").
            Projectile.NewProjectile(source, apuntado, Vector2.Zero,
                type, damage, knockback, player.whoAmI, mano.X, mano.Y);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FF7070:Descarga el ARCO ROJO de la mano al cursor, instantáneo]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El rayo del Arc Surge de Terraria: enganchado a tu mano · 2 arcos extra · electrifica]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
