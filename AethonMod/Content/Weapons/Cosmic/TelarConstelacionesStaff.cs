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
    /// TelarConstelacionesStaff — EL TELAR DE CONSTELACIONES.
    ///
    /// Cada uso CLAVA una estrellita fija (4 puntas, color propio por
    /// semilla) donde estaba el cursor. Las estrellas se CONECTAN solas
    /// con líneas finas de luz — la constelación se DIBUJA sola. Con 4 o
    /// más, cada 30 ticks LA FIGURA SE ENCIENDE: el polígono cerrado
    /// parpadea blanco y todo enemigo DENTRO recibe daño ×2.2 por
    /// estrella de la figura, con destello en cada vértice. Tope de 6
    /// estrellas: la 7ª reemplaza a la más vieja.
    /// </summary>
    public class TelarConstelacionesStaff : ModItem
    {
        /// <summary>Tope de estrellas activas por tejedor.</summary>
        public const int TopeEstrellas = 6;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TelarEstrellaProjectile>();
            Item.shootSpeed = 20f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9.WithPitchOffset(0.3f);
            Item.value = 20000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/B9E8FF:EL TELAR DE CONSTELACIONES]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E6F7FF:Cada uso clava una ESTRELLITA donde estaba tu cursor · se conectan solas con hilos de luz]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/9FD4FF:Con 4 o más, cada 30 ticks la FIGURA SE ENCIENDE: todo enemigo dentro del polígono recibe ×2.2 por estrella]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Tope de 6 estrellas: la 7ª reemplaza a la más vieja · viven 480 ticks y se apagan en orden · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PUNTO donde estaba el cursor (clamp de tejido: 700 px).
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - player.MountedCenter;
            if (delta.Length() > 700f)
                target = player.MountedCenter + Vector2.Normalize(delta) * 700f;

            // EL TOPE DE 6: la 7ª estrella reemplaza a la MÁS VIEJA.
            MatarEstrellaVieja(player);

            Projectile.NewProjectile(source, target, Vector2.Zero,
                type, damage, knockback, player.whoAmI, target.X, target.Y);
            return false;
        }

        /// <summary>EL TOPE: si el tejedor ya tiene 6 estrellas, mata a la de
        /// MENOS timeLeft (la más vieja del cielo).</summary>
        private static void MatarEstrellaVieja(Player player)
        {
            int tipo = ModContent.ProjectileType<TelarEstrellaProjectile>();
            Projectile vieja = null;
            int cuenta = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != player.whoAmI)
                    continue;
                cuenta++;
                if (vieja == null || p.timeLeft < vieja.timeLeft) vieja = p;
            }

            if (cuenta >= TopeEstrellas && vieja != null)
                vieja.Kill();
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
