using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigacion;

namespace AethonMod.Content.Weapons.Investigacion
{
    /// <summary>
    /// CETRODECONSTELACIONES — v6.54.0 — LAS CONSTELACIONES VIVAS DEL JUGADOR.
    ///
    /// La letra: «lo mismo con esta textura ConstellationArea.png, crea
    /// tu propia version e investiga como las usa».
    ///
    /// LA INVESTIGACIÓN (Task 74-a): es el ataque StarSigns del
    /// StellarGeliath — el esbirro estelar del dios: estrellas
    /// (ConstellationStar, 34², dibujada EN CRUZ ×2 con wobble) que se
    /// REPELEN entre sí y orbitan al dueño; el sistema empareja las
    /// estrellas cercanas y traza LÍNEAS entre ellas (ConstellationLine,
    /// 144², la MITAD SUPERIOR de la aguja con origen centro-abajo,
    /// rotada desde cada extremo — 2 pasadas glow + 2 núcleo a escala
    /// X 0.33), líneas con COLISIÓN REAL (CheckAABBvLineCollision);
    /// y el marcador del área: ConstellationArea (la viñeta radial)
    /// dibujada ×2 a velocidades de giro distintas con tintes A=0
    /// aditivos.
    ///
    /// NUESTRAS TEXTURAS (100 % casa): AreaConstelacion.png (viñeta
    /// radial 512² — regenerada: los 1624² del original eran derroche
    /// documentado), EstrellaConstelacion.png (la 4 puntas) y
    /// LineaConstelacion.png (la aguja-lente).
    /// </summary>
    public class CetroConstelaciones : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName/Tooltip: Items.CetroConstelaciones (hjson).
        }

        public override void SetDefaults()
        {
            Item.damage = 58;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.mana = 40;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<EstrellaPrisma>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item4;
        }

        public override bool CanUseItem(Player player)
        {
            // MÁXIMO 5 estrellas vivas del dueño (las constelaciones se
            // componen — el clic añade estrellas hasta el cielo).
            int vivas = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<EstrellaPrisma>())
                    vivas++;
            }
            return vivas < 5;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA ESTRELLA nace sobre el cursor; la PRIMERA del grupo
            // dibuja el ÁREA (la viñeta de la investigación).
            bool hayHermanas = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<EstrellaPrisma>())
                { hayHermanas = true; break; }
            }
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI, hayHermanas ? 0f : 1f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine linea = new(Mod, "CetroPrueba",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.CetroConstelaciones.Prueba"))
            {
                OverrideColor = new Color(255, 240, 180),
            };
            tooltips.Add(linea);
        }
    }
}
