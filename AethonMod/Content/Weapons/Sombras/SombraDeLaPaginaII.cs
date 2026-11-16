using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Sombras
{
    // ======================================================================
    //  SombraDeLaPaginaII — v6.144 — LA COPIA SIN ROSTRO.
    //
    //  LA LETRA DEL USUARIO: «crea una nueva arma de la sombra de la
    //  pagina, es que sea una copia del original, que es la numero 1,
    //  pero a esta copia quitale los ojos y dientes, y fusiona la
    //  nueva arma de la sombra de la pagina con una el ataque de
    //  Muñeca Maldita de Llama Sombría, pero modificalo para que tome
    //  el tono de la casa, o sea que se vea como la sombra de la
    //  pagina, ademas de que mejora su animacion para que parezca que
    //  se mueven despues de ser lanzados».
    //
    //  LA COPIA: el mismo cuerpo del tentáculo de la página 1 (la
    //  ColumnaViva de verlet, la masa lisa de la casa) PERO SIN OJOS
    //  NI DIENTES — sin OjosDeMasa, sin ColmilloTexturizado: solo la
    //  página, recordando.
    //  EL ATAQUE: LA MUÑECA MALDITA (el Shadowflame hex doll de
    //  vanilla: proyectiles que REBOTAN en paredes y sueltan llamas)
    //  VESTIDO DE CASA: la sombra rebota 5 veces exhalando bruma
    //  negra-violeta, y cada rebote la ACELERA.
    //  LA ANIMACIÓN POST-LANZAMIENTO (la letra: «parezca que se
    //  mueven despues de ser lanzados»): la masa ONDEA viva tras el
    //  disparo — la ColumnaViva con gancho, la punta que respira y un
    //  lagrimeo de bruma que NO se detiene mientras vuela.
    // ======================================================================
    public class SombraDeLaPaginaII : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Sombras/SombraDeLaPagina";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 220;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 28;
            Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Sombras.SombraPaginaIIProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0;                      // de prueba, como las hermanas
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA MUÑECA MALDITA DISPARA EN ABANICO: tres sombras con
            // velocidades escalonadas (el patrón del hex doll — pero
            // vestido de página). Cada una rebota por su cuenta.
            for (int i = -1; i <= 1; i++)
            {
                Vector2 v = velocity.RotatedBy(i * 0.14f) * (1f - Math.Abs(i) * 0.08f);
                Projectile.NewProjectile(source, player.MountedCenter, v, type,
                    damage, knockback, player.whoAmI, 0f, i);
            }
            return false;
        }
    }
}
