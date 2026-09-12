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
    /// VoidEyeStaff — EL OJO DEL VACÍO (arma nueva de terror cósmico, v5.96).
    ///
    /// Petición del usuario: "crea otra arma nueva de prueba con la que has
    /// aprendido y esta nueva arma debe tener un proyectil lo más cósmico y
    /// de terror cósmico que se te ocurra, lo dejo a tu imaginación".
    ///
    /// Lo aprendido, condensado en un solo horror: una ESTRELLA MUERTA con
    /// un OJO VIVO cuyo iris ámbar SIGUE a su víctima (y si no hay nadie...
    /// te mira a ti), con una PUPILA que es un micro agujero negro (lensing
    /// real de 75 pasos + disco de acreción carmesí) que se DILATA con el
    /// terror arrastrando consigo al aura de daño, a la lente gravitacional
    /// del fondo y a la gravedad. Parpadea — y en la oscuridad daña el doble.
    /// Al morir: EL GRITO (onda cromática inversa que colapsa el mundo hacia
    /// el ojo + anillo de Einstein desgarrando la realidad).
    ///
    /// Especificaciones:
    ///   - damage = 95 (DamageType.Generic, como el resto del arsenal cósmico)
    ///   - useTime = useAnimation = 70 (el ojo vive 12 s)
    ///   - shootSpeed = 4 (deriva; luego SE ARRASTRA hacia su objetivo)
    ///   - mana = 0, HoldUp, autoReuse, noMelee
    /// </summary>
    public class VoidEyeStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 70;
            Item.useAnimation = 70;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.MoonLord;
            Item.shoot = ModContent.ProjectileType<VoidEyeProjectile>();
            Item.shootSpeed = 4f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA GRIETA se abre donde apunta el jugador (no en su cuerpo):
            // el ojo emerge ahí, lentamente.
            Vector2 spawnPos = player.Center + velocity.SafeNormalize(Vector2.Zero) * 90f;
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "VE_Title",
                "[c/B80046:═══ EL OJO DEL VACÍO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "VE_Desc",
                "[c/FF7A9E:Invoca una estrella muerta con UN OJO VIVO de 12 segundos: su iris ámbar sigue a tu víctima (y si no hay nadie… te mira a TI)]"));
            tooltips.Add(new TooltipLine(Mod, "VE_Desc2",
                "[c/FFB0C4:La pupila es un micro agujero negro que se DILATA: el aura de daño, la lente gravitacional y su fuerza de arrastre crecen con ella]"));
            tooltips.Add(new TooltipLine(Mod, "VE_Desc3",
                "[c/9C6E78:Los enemigos observados quedan ralentizados por el pavor y arden en llama sombría — cuando el ojo PARPADEA, daña el doble en la oscuridad]"));
            tooltips.Add(new TooltipLine(Mod, "VE_Desc4",
                "[c/B80046:Al morir: EL GRITO — implosión cromática que colapsa el mundo hacia el ojo + el ANILLO DE EINSTEIN desgarrando la realidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
