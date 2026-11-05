using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Clicker;

namespace AethonMod.Content.Weapons.Clicker
{
    // ======================================================================
    //  ParpadeoDelEterno — v6.100 — EL CLICKER #4: EL PARPADEO DEL
    //  ETERNO.
    //
    //  EL CLICKER DEL OJO: el radio más ancho de la clase (460 px) y
    //  el estallido más raro: cada 14 parpadeos EL OJO SE ABRE — la
    //  GARGANTA DEL VÓRTICE del motor de píxeles nace EN EL PORTADOR
    //  y ATRAE a los vivos hacia su boca mientras los muele (daño ×3
    //  en área con succión).
    // ======================================================================
    public class ParpadeoDelEterno : ModItem
    {
        /// <summary>EL RADIO del clic (px — el más ancho de la clase).</summary>
        private const float Radio = 460f;

        /// <summary>Cada N parpadeos, EL OJO SE ABRE.</summary>
        private const int CadaN = 14;

        public override void SetStaticDefaults()
        {
            ClickerCasa.Registrar(Type, Radio, new Color(150, 90, 235), EstiloClicker.Ojo);
        }

        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 9;
            Item.useAnimation = 9;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<ClickerGolpeProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // SIN COSTE DE MANÁ
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.MenuTick;
        }

        public override bool CanUseItem(Player player)
        {
            return Vector2.DistanceSquared(Main.MouseWorld, player.Center) <= Radio * Radio;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PARPADEO: cada CadaN-ésimo clic el ojo se abre EN EL
            // PORTADOR (ai[0]=4 → el estilo Ojo: GargantaVortice ×3
            // con succión).
            bool abrir = player.GetModPlayer<ClickerJugador>().RegistraClic(Type) % CadaN == 0;
            Projectile.NewProjectile(source,
                abrir ? player.MountedCenter : Main.MouseWorld, Vector2.Zero, type,
                abrir ? damage * 3 : damage, knockback, player.whoAmI,
                abrir ? 4f : 0f, Main.rand.Next(9973), 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Parpadeo1",
                "[c/965AEB:EL PARPADEO: cada clic aplasta donde miras (el radio más ancho de la clase)]"));
            tooltips.Add(new TooltipLine(Mod, "Parpadeo2",
                $"[c/965AEB:Cada {CadaN} parpadeos EL OJO SE ABRE: la garganta del vórtice ATRAE a los vivos y los muele (daño ×3 en área)]"));
            tooltips.Add(new TooltipLine(Mod, "Parpadeo3",
                "[c/7B4EC4:(el ojo del Eterno hecho contrato · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
