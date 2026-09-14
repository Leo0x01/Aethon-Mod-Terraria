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
    /// RelojArenaStaff — v6.26 — EL BASTÓN DEL RELOJ DE ARENA CÓSMICO.
    ///
    /// Petición del usuario: "luego crea mas bastones con nuevos tipos de
    /// proyectiles creativos, al menos 5" — el primero: un reloj de arena
    /// VIVO hecho de estrellas.
    ///
    /// EL ARMA QUE PESA EL TIEMPO: cada disparo planta un reloj de arena
    /// flotante (~50 px) cuya arena son MOTAS DE LUZ doradas cayendo de la
    /// cámara superior a la inferior por el cuello estrecho; el montículo
    /// de abajo CRECE con cada grano. Cuando la cámara superior se vacía
    /// (~5 s) el reloj SE INVIERTE — gira 180° suave y el ciclo renace
    /// (dos inversiones y pico por disparo, 12 s de vida).
    ///
    /// MECÁNICA: el "tiempo ralentizado" de los enemigos cercanos se
    /// traduce en mecánica REAL sin hacks globales: los enemigos en 160 px
    /// reciben el peso de cada tick de arena (daño + knockback HACIA ABAJO
    /// como gravedad aumentada) y en cada inversión un pulso de arena los
    /// hunde aún más.
    ///
    /// Daño 170, useTime 55, sin maná, HoldUp, autoReuse, rareza Quest.
    /// </summary>
    public class RelojArenaStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip es el guion del reloj.
        }

        public override void SetDefaults()
        {
            Item.damage = 170;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 55;
            Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RelojArenaProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 30000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El reloj nace EN EL CURSOR (velocidad solo de llegada; la AI
            // la frena y lo deja flotando en su sitio de relojería).
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "RelojHeader",
                "[c/FFD97A:EL RELOJ DE ARENA CÓSMICO — el tiempo hecho de motas de luz]"));
            tooltips.Add(new TooltipLine(Mod, "RelojLine1",
                "[c/D9B25A:La arena son estrellas cayendo: cada tick de caída PESA sobre los enemigos en 160 px]"));
            tooltips.Add(new TooltipLine(Mod, "RelojLine2",
                "[c/D9B25A:Knockback hacia ABAJO — el peso de la gravedad aumentada del reloj]"));
            tooltips.Add(new TooltipLine(Mod, "RelojLine3",
                "[c/FFE9B0:Cuando la cámara superior se vacía (~5 s) el reloj SE INVIERTA: pulso de arena que hunde]"));
            tooltips.Add(new TooltipLine(Mod, "RelojPie",
                "[c/78788C:12 segundos · varias inversiones · el montículo de abajo crece grano a grano]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
