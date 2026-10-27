using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.54.8 — EL ARMA RÉPLICA (la petición literal): «intenta crear un
    //  arma que sea la copia exacta de el rayo de fusión usando sus
    //  texturas para fines de investigacion». LA RÉPLICA dispara EL
    //  ATAQUE TAL CUAL ERA: 300 ticks de carga (la boca de glows
    //  apilados creciendo + el telegraph que apunta) y 800 de láser —
    //  el cono de 3500 px con su barrido cosenoidal, SU shader de las
    //  cuatro texturas multiplicadas, SU gradiente viajero, SU kick de
    //  cámara, SU música callada al 30 %, SU sonido en loop y SUS
    //  polvos cromáticos recorriendo el conducto.
    //
    //  MATERIAL DE INVESTIGACIÓN (GPL-3.0, puerto del jefe): se retira
    //  antes de cualquier publicación pública — la regla de la casa.
    //  Sirve para comparar, lado a lado, con las armas de la casa: LOS
    //  PICOS NO EXISTEN AQUÍ — las cuatro texturas son fBm orgánico
    //  multiplicado, nunca geometría angular.
    // ======================================================================
    public class RayoFusion : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoFusionCopiaProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 100;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            // LA RÉPLICA es una sola a la vez: su ciclo completo dura
            // 1200 ticks (20 s) — como el ataque original del jefe.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<RayoFusionCopiaProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL DUEÑO viaja en ai[2] (el slot del portador — el arma lo
            // busca cada tick para colocar su boca).
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 0f, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
