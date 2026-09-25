using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// FragmentoSupernovaStaff — v6.41 — EL FRAGMENTO DE SUPERNOVA.
    ///
    /// ARMA DE PRUEBAS (la petición del usuario: el boss copiado como
    /// minion): invoca la SINGULARIDAD ALADA — el ojo alado con sus
    /// afterimages orbitales, su halo dorado pulsante y sus DOS ataques
    /// icónicos (la volea de cinco ráfagas nova y el beam telegrafiado
    /// de 900px con su flor de fuego).
    ///
    /// Es un MINION de verdad (el patrón de la cría estelar): ocupa 1
    /// espacio de sirviente, nunca expira, vuela al hombro con la
    /// gravedad suave de una singularidad.
    /// </summary>
    public class FragmentoSupernovaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Summon;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;    // SIN MANÁ (la regla de la casa)
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;    // el sello de invocación de la casa
            Item.shoot = ModContent.ProjectileType<FragmentoSupernovaMinion>();
            Item.shootSpeed = 10f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "F1",
                "[c/FFE1A8:═══ EL FRAGMENTO DE SUPERNOVA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "F2",
                "[c/FFC978:La singularidad alada hecha sirviente: el OJO ALADO con sus]"));
            tooltips.Add(new TooltipLine(Mod, "F3",
                "[c/FFB366:afterimages orbitando y respirando a su alrededor (la firma)]"));
            tooltips.Add(new TooltipLine(Mod, "F4",
                "[c/FF9E4D:VOLEA de 5 ráfagas nova en abanico · BEAM telegrafiado de 900px\n(×2.5 del daño del minion) · flor de fuego al morir cada ráfaga]"));
            tooltips.Add(new TooltipLine(Mod, "F5",
                "[c/78788C:MINION: ocupa 1 espacio de sirviente · nunca expira · SIN COSTE DE MANÁ\nARMA DE PRUEBAS (réplica del jefe de la singularidad alada)]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El fragmento nace delante del jugador (el minion sostiene el
            // buff — el patrón de la cría: 2 ticks bastan).
            player.AddBuff(ModContent.BuffType<FragmentoSupernovaBuff>(), 2);
            Vector2 spawnPos = player.MountedCenter +
                velocity.SafeNormalize(Vector2.Zero) * 48f;
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage,
                knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
