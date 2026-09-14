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
    /// CoroEspectralStaff — v6.26 — EL BASTÓN DEL CORO ESPECTRAL.
    ///
    /// Petición del usuario: "luego crea mas bastones con nuevos tipos de
    /// proyectiles creativos, al menos 5" — el quinto: un coro de notas
    /// fantasmales que CANTA.
    ///
    /// EL ARMA QUE CANTA: cada disparo invoca 6 NOTAS DE LUZ fantasmales
    /// (figuras de nota musical dibujadas por código: cabeza circular +
    /// mástil + bandera ondulante) que flotan en ÓRBITAS lentas alrededor
    /// del punto de lanzamiento. EL CORO: cada 0.8 s la nota siguiente
    /// EMITE su anillo de onda sonora (OndaLib.Pulse expandiéndose) con su
    /// TONO de campana de cristal (pitch distinto por nota); los anillos
    /// golpean a los enemigos que atraviesan (daño bajo pero FRECUENTE, en
    /// un área de 260 px). Las notas visten los colores de la escala
    /// (dorado, ámbar, bronce, cobre...) y al final se APAGAN una a una —
    /// el coro se despide. Cada anillo deja su ECO visual tenue.
    ///
    /// Daño 160, useTime 50, sin maná, HoldUp, autoReuse, rareza Quest.
    /// </summary>
    public class CoroEspectralStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // El nombre vive arriba; el tooltip es el programa del concierto.
        }

        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CoroEspectralProjectile>();
            Item.shootSpeed = 11f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 30000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El CORO se reúne EN EL CURSOR: las notas orbitan ese punto.
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "CoroHeader",
                "[c/FFE2A8:EL CORO ESPECTRAL — seis notas de luz que cantan cada 0.8 s]"));
            tooltips.Add(new TooltipLine(Mod, "CoroLine1",
                "[c/D9B183:Cada nota emite su ANILLO SONORO de campana de cristal (tono por nota)]"));
            tooltips.Add(new TooltipLine(Mod, "CoroLine2",
                "[c/D9B183:Los anillos atraviesan y golpean en 260 px — daño bajo pero FRECUENTE]"));
            tooltips.Add(new TooltipLine(Mod, "CoroLine3",
                "[c/FFEBD0:Los colores de la escala (dorado, ámbar, bronce...) y un ECO tenue por anillo]"));
            tooltips.Add(new TooltipLine(Mod, "CoroPie",
                "[c/78788C:12 segundos de concierto · al final las notas se apagan UNA A UNA]"));
        }

        public override void AddRecipes()
        {
            // El patrón de la casa: madera 5 (el banco de pruebas cósmico).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
