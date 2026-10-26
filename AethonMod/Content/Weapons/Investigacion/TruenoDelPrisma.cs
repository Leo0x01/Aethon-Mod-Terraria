using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigacion;

namespace AethonMod.Content.Weapons.Investigacion
{
    /// <summary>
    /// TRUENODELPRISMA — v6.54.0 — EL RELÁMPAGO RAMIFICADO DEL JUGADOR.
    ///
    /// La letra: «otra textura interesante es LiquidTrail.png,
    /// Lightning.png, LightningGlow.png, tambien investiga como las usa
    /// y crea tus propias texturas basadas en esas que sean diferente y
    /// crea un item en consecuencia para el jugador».
    ///
    /// LA INVESTIGACIÓN (Task 74-b): los rayos del jefe (GooLightning)
    /// NO son sprites — son POLILÍNEAS de 3 puntos (curva cuadrática +
    /// jitter circular) que CADA TICK SE RELAJAN hacia la recta (el
    /// rayo «cruje» al impactar y luego se aquieta), dibujadas con la
    /// técnica del shader GooLightningEffect: DOS MUESTRAS de la MISMA
    /// textura de filamentos DESFAZADAS 0.5 en U y MULTIPLICADAS —
    /// solo sobreviven las vetas que coinciden = relámpago ramificado
    /// fino que se auto-anima con el scroll, + el halo suave
    /// (LightningGlow) y el arcoíris del dios desvaneciéndose con la
    /// vida. El ataque BurstLightning del jefe: 1 rayo GRANDE + rayos
    /// pequeños a los vecinos.
    ///
    /// NUESTRAS TEXTURAS (100 % casa): RayoRamo.png (filamentos finos
    /// en banda central, tileable U y V) y RayoRamoBrillo.png (el halo
    /// borroso) — nacidas de tools/gen_investigacion_v6540.py.
    /// </summary>
    public class TruenoDelPrisma : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName/Tooltip: Items.TruenoDelPrisma (hjson).
        }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.mana = 22;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TruenoPrismaRayo>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item92;    // el trueno del disparo
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL BURST DEL JEFE: 1 rayo GRANDE al objetivo más cercano +
            // hasta 2 pequeños a los vecinos (Goozma.cs:2179 — la letra
            // del ataque BurstLightning).
            NPC objetivo = null;
            float mejor = 900f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || npc.friendly || !npc.CanBeChasedBy())
                    continue;
                float d = npc.Distance(player.Center);
                if (d < mejor) { mejor = d; objetivo = npc; }
            }

            int grandes = 0;
            if (objetivo != null)
            {
                Projectile.NewProjectile(source, objetivo.Center, Vector2.Zero, type,
                    damage, knockback, player.whoAmI, objetivo.whoAmI, 1f);   // EL GRANDE
                grandes++;
            }
            // LOS PEQUEÑOS: hasta 2 enemigos extra en 900 px.
            int chicos = 0;
            for (int i = 0; i < Main.maxNPCs && chicos < 2; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || npc.friendly || !npc.CanBeChasedBy() ||
                    npc == objetivo)
                    continue;
                if (npc.Distance(player.Center) > 900f) continue;
                Projectile.NewProjectile(source, npc.Center, Vector2.Zero, type,
                    (int)(damage * 0.55f), knockback * 0.6f, player.whoAmI, npc.whoAmI, 0f);
                chicos++;
            }
            return false;   // los rayos ya nacieron
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine linea = new(Mod, "TruenoPrismaPrueba",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.TruenoDelPrisma.Prueba"))
            {
                OverrideColor = new Color(255, 240, 180),
            };
            tooltips.Add(linea);
        }
    }
}
