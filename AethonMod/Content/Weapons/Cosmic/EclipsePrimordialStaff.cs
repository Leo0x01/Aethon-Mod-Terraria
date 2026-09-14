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
    /// EclipsePrimordialStaff — v6.22 — EL BASTÓN DEL ECLIPSE PRIMORDIAL.
    ///
    /// PETICIÓN DEL USUARIO: "crea un bastón nuevo que fusione el sol de
    /// 20 anillos rúnicos, más todos los agujeros negros; a esto dale
    /// efectos de luz, bruma, humo, rayos y otros efectos que creas
    /// convenientes".
    ///
    /// LA FUSIÓN TOTAL: un agujero negro supremo (la física más grande
    /// del mod: 600px de atracción) con el SISTEMA SOLAR RÚNICO COMPLETO
    /// orbitando el horizonte — los 20 anillos de la copia XX, el gran
    /// sellado maestro y su cometa. TODAS las herencias fundidas:
    ///   · del UMBRAL  → el disco Doppler oblicuo (ahora gradiente aurora).
    ///   · del CÓSMICO → el anillo de bandas (20 zonas de brillo viajando).
    ///   · del OLVIDO  → los brazos espirales con flujo hacia adentro.
    ///   · de la BRUMA → el halo de nubes, el humo que respira y las
    ///                   volutas cayendo al núcleo.
    ///   · del SUPREMO → el núcleo negro devorador + rim dorado + jets.
    ///   · del AURORA  → el gradiente negro→morado→azul→dorado.
    ///   · del SOL XX  → los 20 anillos rúnicos + el GRAN SELLADO.
    ///   · de STORMLIB → la corona de rayos de 2ª generación.
    ///   · de LUMENLIB → los rayos prismáticos + el destello del corazón.
    ///
    /// La muerte es LA NOVA DEL ECLIPSE: el anillo de Einstein + la nova
    /// rúnica del sol fundido — la explosión más grande del mod.
    /// </summary>
    public class EclipsePrimordialStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 640;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 55; Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EclipsePrimordialProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = Item.buyPrice(gold: 50);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/B46CFF:EL ECLIPSE — el Sol de los 20 Anillos fundido con TODOS los agujeros negros]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Gradiente aurora · 20 anillos rúnicos + gran sellado · bruma y humo · corona de rayos · luz prismática]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:La atracción más grande del mod (600px) · muere en el Anillo de Einstein + la Nova Rúnica]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
