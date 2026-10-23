using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    // ======================================================================
    //  v6.51.4 → v6.51.5 → v6.51.6 — LAS ARMAS DIGNAS DEL GRIMORIO.
    //
    //  LA LETRA DE LA .51.6: «bueno, las nuevas armas creadas no sirven,
    //  asi que las puedes borrar» — LA TERCERA PURGA: EL ÍNDICE DEL
    //  GRIMORIO, LA LLUVIA DE VERSOS, EL OJO QUE JUZGA y EL CORAZÓN DEL
    //  MAGNETAR (creadas en la .51.5) MURIERON — sus cuatro items, sus
    //  cuatro proyectiles y sus cuatro iconos. (El Ojo que Juzga y la
    //  Lluvia además tiraban «Begin has been called before End» en cada
    //  frame — el client.log lo confesó — sus PreDraw rompían el
    //  contrato del lote de la casa.)
    //
    //  QUEDA UNA — LA BINARÍA:
    //    1 · EL EQUINOCCIO BINARIO (RE-HECHA en la .51.5): la MISMA
    //        pareja 931 de El Equinoccio con el efecto binario — de día
    //        el dorado estalla en MINI ECLIPSE en área; de noche el
    //        violeta PERFORA 4 y DRENA vida.
    //
    //  Sin maná (la regla de las armas de pruebas de la casa), con
    //  receta de 5 madera (el protocolo v6.14.2) y en LA BOLSA DE LOS
    //  PROYECTILES DIGNOS.
    // ======================================================================

    // ======================================================================
    //  1 · EL EQUINOCCIO BINARIO — la binaria día/noche, RE-HECHA con
    //  el proyectil de El Equinoccio (la letra: «usa el mismo
    //  proyectil que El Equinoccio, pero dale el efecto que El
    //  Equinoccio Binario»).
    // ======================================================================
    public class DignosEquinoccioBinario : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = 931;   // Nightglow — EL MISMO de El Equinoccio (el número)
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item82;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === LA PAREJA DE EL EQUINOCCIO (el vanilla 931 — IA de
            //     Terraria intacta), con el RELOJ DEL MUNDO decidiendo
            //     quién GUIA: de DÍA el DORADO sale hacia el cursor y el
            //     violeta lo escolta; de NOCHE el VIOLETA guía. ===
            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // EL RUMBO DEL GUÍA: hacia el cursor (la binaria APUNTA).
            Vector2 guia = (Main.MouseWorld - pos).SafeNormalize(-Vector2.UnitY) * 15f;
            Vector2 escolta = guia.RotatedBy((Main.dayTime ? 1f : -1f) * 0.55f) * 0.85f;

            // === EL DORADO — EL DÍA (ai[2]=9211: la marca binaria del
            //     FX — el sol en miniatura + EL MINI ECLIPSE en área). ===
            Projectile.NewProjectile(source, pos, Main.dayTime ? guia : escolta,
                931, damage, knockback, player.whoAmI,
                -1f,     // ai[0]: sin objetivo aún (el homing del 931 decide)
                0.64f,   // ai[1]: EL MATIZ DORADO
                9211f);  // ai[2]: la marca binaria dorada

            // === EL NEGRO — LA NOCHE (ai[2]=9212: la marca binaria — el
            //     eclipse limpio + PERFORA 4 y DRENA vida). ===
            Projectile.NewProjectile(source, pos, Main.dayTime ? escolta : guia,
                931, damage, knockback, player.whoAmI,
                -1f,
                0.28f,   // ai[1]: el matiz violeta
                9212f);  // ai[2]: la marca binaria violeta

            return false;   // los dos spawns son nuestros
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD678:El disparo de El Equinoccio — la pareja dorada y violeta— con el reloj del mundo:]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:DÍA: el dorado guía y su golpe estalla en MINI ECLIPSE en área · NOCHE: el violeta guía, PERFORA 4 y DRENA vida · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

}
