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
    /// EminenciaAtrozStaff — v6.29 — LA EMINENCIA ATROZ.
    ///
    /// LA SEGUNDA DE LOS DOS EXHUMADOS — el arma nacida de la investigación
    /// del GRUESOME EMINENCE de Calamity (research/rancor_v629/):
    ///
    ///   · LA CONGREGACIÓN: invoca una conglomeración GASEOSA de espíritus
    ///     cerca del cursor (BrumaFX — la masa pálida de hueso que OCLUYE).
    ///   · EL COMPORTAMIENTO SALVAJE: sigue el cursor FLOJAMENTE y SE LARGA
    ///     por su cuenta de cuando en cuando (la "wildly" de Calamity).
    ///   · LOS ESPÍRITUS MENORES: la congregación libera espíritus pequeños
    ///     que flotan, lingüean... y SON TIRADOS DE VUELTA (puramente
    ///     visuales, deterministas).
    ///   · LA ACUMULACIÓN (14 segundos de canal — el número EXACTO): los
    ///     espíritus se acumulan hasta crear LA ABOMINACIÓN: un monstruo
    ///     único TOTALMENTE CONTROLABLE (el spring se aprieta ×2.4).
    ///   · LA RAMPA: el daño crece del 100% al 185% del base (1 + 0.85·x —
    ///     EXACTO a Calamity).
    ///   · LA CARA: el interior Giygas — un ojo y una boca que asoman en
    ///     ventanas caóticas cuando la masa está madura.
    ///
    /// El precio del canal (Calamity drena maná constante; nosotros por la
    /// regla de la casa NO — pero SIN canal NO hay crecimiento, y sin
    /// crecimiento la congregación DECAE y se disipa).
    /// </summary>
    public class EminenciaAtrozStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;   // EL CANAL: se sostiene
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EminenciaAtrozProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item104.WithPitchOffset(-0.25f);
            Item.value = 15000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA REGLA DEL MONSTRUO ÚNICO: si YA vives una congregación, este
            // uso NO invoca otra — solo alimenta el canal (el crecimiento).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == type)
                    return false;
            }

            // Nace CERCA DEL CURSOR (clampeado al alcance de la casa).
            Vector2 target = Main.MouseWorld;
            Vector2 toTarget = target - player.Center;
            float len = toTarget.Length();
            if (len > 560f) toTarget *= 560f / len;

            Projectile.NewProjectile(source, player.Center + toTarget, Vector2.Zero,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FF9AA8:LA EMINENCIA ATROZ — la forma exhumada]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/C9E4DE:Sostén el uso: la congregación de espíritus canaliza cerca del cursor]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/C9E4DE:14 s de acumulación → LA ABOMINACIÓN, control total]"));
            tooltips.Add(new TooltipLine(Mod, "D4",
                "[c/FFD66B:El daño crece del 100% al 185% mientras se acumula]"));
            tooltips.Add(new TooltipLine(Mod, "D5",
                "[c/78788C:Si sueltas el canal, la masa decae y se disipa · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
