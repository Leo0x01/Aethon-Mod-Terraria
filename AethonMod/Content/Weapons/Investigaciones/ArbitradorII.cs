using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  ARBITRADOR II — v6.124 — LA COPIA DE INVESTIGACIÓN del ARBITRATOR
    //  II de CALAMITY ENTROPY (la letra: «investiga y copia al 100% todo
    //  sobre el arma Arbitrator II en CALAMITY ENTROPY, esto sera usado
    //  como investigacion»).
    //
    //  LA FICHA DEL ORIGINAL (wiki oficial de Calamity Entropy —
    //  calentropy.miraheze.org):
    //    · TIPO: javalina RATERA, modo difícil, CRAFTABLE (4× Velo de
    //      Sombra Solar + 6× Lingote de Escoria + 1× Bote de Células de
    //      Peste, yunque de mitril/oricalco).
    //    · NÚMEROS: 250 daño · empuje 1 (muy débil) · tiempo de uso 32
    //      (muy lento) · 16% crítico · mejor modificador: 化境 (Godly).
    //    · EL ATAQUE NORMAL: arroja una jabalina QUE ATRAVIESA MUROS;
    //      al golpear a un objetivo ESTALLA, dañando a todos los
    //      objetivos del área.
    //    · EL ATAQUE SIGILOSO: la jabalina se CLAVA en el objetivo y a
    //      los 1 segundo libera una EXPLOSIÓN EN X de mayor alcance.
    //
    //  LAS ADAPTACIONES DE LA CASA (documentadas — Calamity Entropy es
    //  código cerrado; esta copia reproduce la ficha completa):
    //    · La clase RATERA de Calamity → DamageClass.Throwing (la más
    //      cercana; el mod no tiene rateros).
    //    · El STEALTH de Calamity → el SIGILO RATERO de la casa (2,5 s
    //      quieto cargan el tiro sigiloso — SigiloRatero).
    //    · La receta del original (materiales de Calamity Entropy) → el
    //      protocolo de la casa (la madera de siempre, la vía de
    //      investigación); la receta verdadera viaja en el tooltip.
    // ======================================================================
    public class ArbitradorII : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 250;                       // la ficha: 250
            Item.DamageType = DamageClass.Throwing;  // (ratero → arrojadiza)
            Item.useTime = 32;                       // la ficha: 32 (muy lento)
            Item.useAnimation = 32;
            Item.knockBack = 1f;                     // la ficha: 1 (muy débil)
            Item.crit = 16;                          // la ficha: 16%
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;                // la jabalina vuela (la dibuja el proyectil)
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.ArbitradorIIProyectil>();
            Item.shootSpeed = 14f;
            Item.rare = ItemRarityID.Pink;           // (difícil: el rosa del modo difícil)
            Item.value = Item.buyPrice(gold: 4);     // (la ficha: vende 4)
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // UNA JABALINA EN LA MANO: mientras una viva, no hay otra.
            int tipo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.ArbitradorIIProyectil>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.owner == player.whoAmI && pr.type == tipo)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL SIGILO DEL LADRÓN: 2,5 s quieto cargan EL ATAQUE
            // SIGILOSO (la jabalina que SE CLAVA y detona la X).
            var sigilo = player.GetModPlayer<global::AethonMod.Content.Players.Investigaciones.SigiloRatero>();
            bool sigiloso = sigilo.Gastar();

            Projectile.NewProjectile(source, position, velocity, type,
                sigiloso ? (int)(damage * 1.5f) : damage, knockback,
                player.whoAmI, sigiloso ? 1f : 0f);
            if (sigiloso && !Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item62.WithPitchOffset(-0.3f), player.Center);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            void Linea(string clave)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.ArbitradorII." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    return;
                tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo");
            Linea("Linea1");
            Linea("Linea2");
            Linea("Linea3");
            Linea("Receta");
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA para las armas de investigación:
            // la madera de siempre (la vía de prueba). La receta
            // VERDADERA del original viaja en el tooltip.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
