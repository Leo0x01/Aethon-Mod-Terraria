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
    //  SelloDeTinta — v6.100 — EL CLICKER #3: EL SELLO DEL ESCRIBA.
    //
    //  LA LETRA DEL USUARIO (v6.100): «crea armas tipo clicker» — la
    //  clase clicker de la casa ya vivía (Compas Rúnico y Furia
    //  Solar, v6.82) y esta sesión la extiende con LOS DOS clickers
    //  de la tinta, hechos con LAS HERRAMIENTAS (el motor de píxeles
    //  en el acto especial — la regla de la sesión: los proyectiles
    //  usan la herramienta de animación de ataques).
    //
    //  EL GESTO: cada clic ESTAMPA un sello violeta donde apuntas
    //  (dentro del radio de 340 px). Cada 10 sellos LA TINTA ESTALLA:
    //  el NOVA ACTO del motor de píxeles (NovaActo — la misma puerta
    //  del taller) con el golpe ×3 y el ancho de la casa.
    // ======================================================================
    public class SelloDeTinta : ModItem
    {
        /// <summary>EL RADIO del clic (px).</summary>
        private const float Radio = 340f;

        /// <summary>Cada N sellos, EL ESTALLIDO.</summary>
        private const int CadaN = 10;

        public override void SetStaticDefaults()
        {
            ClickerCasa.Registrar(Type, Radio, new Color(185, 120, 245), EstiloClicker.Tinta);
        }

        public override void SetDefaults()
        {
            Item.damage = 135;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;          // el clicker es UN clic por golpe
            Item.shoot = ModContent.ProjectileType<ClickerGolpeProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // SIN COSTE DE MANÁ (la regla v6.35)
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.MenuTick;   // el tick del sello
        }

        public override bool CanUseItem(Player player)
        {
            // EL RADIO: el sello solo cae dentro del alcance de la clase.
            return Vector2.DistanceSquared(Main.MouseWorld, player.Center) <= Radio * Radio;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CONTEO DE SELLOS: cada CadaN-ésimo clic, EL ESTALLIDO
            // (ai[0]=3 → el estilo Tinta del golpe: NovaActo ×3).
            bool estallido = player.GetModPlayer<ClickerJugador>().RegistraClic(Type) % CadaN == 0;
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type,
                estallido ? damage * 3 : damage, knockback, player.whoAmI,
                estallido ? 3f : 0f, Main.rand.Next(9973), 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SelloTinta1",
                "[c/B978F5:LA TINTA: cada clic estampa un sello violeta donde apuntas]"));
            tooltips.Add(new TooltipLine(Mod, "SelloTinta2",
                $"[c/B978F5:Cada {CadaN} sellos LA TINTA ESTALLA — el acto del motor de píxeles (daño ×3, golpe ancho)]"));
            tooltips.Add(new TooltipLine(Mod, "SelloTinta3",
                "[c/7B62C4:(la pluma del escriba hecha contrato · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
