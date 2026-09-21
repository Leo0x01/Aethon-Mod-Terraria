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
    /// PenduloJuicioStaff — v6.26 — EL BASTÓN DEL PÉNDULO DEL JUICIO.
    ///
    /// Petición del usuario: "luego crea mas bastones con nuevos tipos de
    /// proyectiles creativos, al menos 5" — el cuarto: un péndulo con
    /// física REAL colgado del aire.
    ///
    /// EL ARMA QUE SENTENCIA: cada disparo clava un ANCLA de luz en el
    /// punto de emisión y de ella cuelga un PÉNDULO DE LUZ dorado — física
    /// de péndulo real (θ'' = −g/L·sen θ, integrada a mano, L = 180 px):
    /// la MAZA de runas grabadas (26 px) barre el arco DAÑANDO con el paso
    /// (daño ×1.2 y knockback TANGENCIAL fuerte). Con cada vaivén el arco
    /// SE AMPLÍA (energía inyectada: +8% por oscilación hasta ±150°) y el
    /// hilo brilla más. A los 8 s el hilo SE CORTA (chispas) y la maza
    /// VUELA balística hasta EXPLOTAR (onda de choque + Kick 8).
    ///
    /// Daño 250, useTime 65, sin maná, HoldUp, autoReuse, rareza Quest.
    /// </summary>
    public class PenduloJuicioStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip es la sentencia.
        }

        public override void SetDefaults()
        {
            Item.damage = 250;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 65;
            Item.useAnimation = 65;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PenduloJuicioProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 40000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El ANCLA se clava EN EL CURSOR: el péndulo cuelga de ahí.
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "PenduloHeader",
                "[c/FFD37A:EL PÉNDULO DEL JUICIO — física real colgada del aire]"));
            tooltips.Add(new TooltipLine(Mod, "PenduloLine1",
                "[c/D9A75A:La maza de runas barre el arco: daño ×1.2 y knockback TANGENCIAL fuerte]"));
            tooltips.Add(new TooltipLine(Mod, "PenduloLine2",
                "[c/D9A75A:Cada vaivén INYECTA energía: el arco crece +8% por oscilación hasta ±150°]"));
            tooltips.Add(new TooltipLine(Mod, "PenduloLine3",
                "[c/FFE7B0:A los 8 s el hilo SE CORTA: la maza vuela balística y revienta (onda + Kick 8)]"));
            tooltips.Add(new TooltipLine(Mod, "PenduloPie",
                "[c/78788C:El hilo brilla más cuanto más se abre la sentencia]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
