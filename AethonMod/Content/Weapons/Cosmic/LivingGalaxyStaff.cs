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
    /// LivingGalaxyStaff — EL BÁCULO DE LA GALAXIA VIVIENTE (arma nueva v6.00).
    ///
    /// Petición del usuario: "borra el ojo, se ve feo, mejor crea un arma
    /// nueva con un proyectil cosmico, este debe ser una galaxia, investiga
    /// galaxias en internet". EL PROYECTIL ES UNA GALAXIA ESPIRAL DE
    /// DISEÑO PERFECTO (la investigación — M51/M101/M74/M100 — dictó el
    /// look: bulbo dorado, brazos azules, HII rosas, polvo oscuro).
    ///
    /// La lanzas, VUELA y se ESTACIONA donde cayó; ahí gira y CABECEA en
    /// 3D, ARRASTRA a los enemigos con la gravedad de su disco, los quema
    /// con su aura estelar y SIEMBRA ESTRELLAS desde sus brazos mientras
    /// ACECHA flotando hacia la presa. Al morir: LA EXPLOSIÓN ESTELLAR —
    /// 14 semillas radiales + destello (cada una con el color de su origen:
    /// azul de brazo, oro de bulbo, rosa de HII).
    ///
    /// Especificaciones:
    ///   - damage = 110 (DamageClass.Magic)
    ///   - mana = 0 (v6.00 — TODAS las armas cósmicas de prueba son SIN
    ///     MANA, petición del usuario), useTime = 30, knockBack 3,
    ///     autoReuse, noMelee
    ///   - dispara LivingGalaxyProjectile (9 s de vida, disco de 118 px)
    /// </summary>
    public class LivingGalaxyStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 110;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;      // v6.00 — arma de prueba: SIN MANA
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 6, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<LivingGalaxyProjectile>();
            Item.shootSpeed = 13f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // nace EN MOVIMIENTO hacia el cursor (ella sola frena y se estaciona)
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "LGS_Title",
                "[c/AAD4FF:═══ LA GALAXIA VIVIENTE ═══]"));
            tooltips.Add(new TooltipLine(Mod, "LGS_Desc",
                "[c/D9EAFF:Lanza UNA GALAXIA ESPIRAL de diseño perfecto — cien mil millones de soles girando]"));
            tooltips.Add(new TooltipLine(Mod, "LGS_Desc2",
                "[c/C8B8FF:Se estaciona, CABECEA en 3D, ARRASTRA a los enemigos con su gravedad y SIEMBRA estrellas desde sus brazos]"));
            tooltips.Add(new TooltipLine(Mod, "LGS_Desc3",
                "[c/9FD9FF:Al morir: LA EXPLOSIÓN ESTELLAR — la galaxia se dispara en semillas de estrellas]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
