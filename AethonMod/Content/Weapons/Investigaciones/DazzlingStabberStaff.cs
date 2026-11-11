using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  DAZZLING STABBER STAFF — v6.124 — LA COPIA DE INVESTIGACIÓN (la
    //  letra del usuario: «copia el arma Dazzling Stabber Staff de
    //  Calamity Mod y crea una version con su codigo, sprite y textura
    //  al 100% como investigacion, luego daselas al jugador»).
    //
    //  EL CÓDIGO: calco del original de Calamity Mod
    //  (CalamityTeam/CalamityModPublic — Items/Weapons/Summon/
    //  DazzlingStabberStaff.cs, rama 1.4.4) — cada línea del original
    //  vive aquí, adaptada SOLO donde Calamity es propietario:
    //    · ILocalizedModType/LocalizationCategory → hjson de la casa.
    //    · CalamityItemSets.ExtraDebuffTooltip_Enemy → el tooltip del
    //      hjson lo cuenta (la línea del debuff viaja en el texto).
    //    · RarityTurquoise/Turquoise → ItemRarityID.Purple (el más
    //      cercano vanilla) + su precio de compra.
    //    · EL RESTO — el Shoot completo con el reparto de los TRES
    //      cuchillos por ranura de minion y el abanico de ángulos de
    //      descanso — BYTE A BYTE.
    //  EL SPRITE: el PNG ORIGINAL del arma + su GLOWMASK, copiados
    //  tal cual (44×60 y su máscara) — investigación de la casa.
    //  EL ORIGEN: drop de Providence, the Profaned Goddess (Calamity).
    // ======================================================================
    public class DazzlingStabberStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            // (El ExtraDebuffTooltip de Calamity vive en el tooltip del
            // hjson — la casa no tiene su item set: la línea de las
            // LLAMAS SAGRADAS viaja en el texto del pergamino.)
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 60;
            Item.damage = 69;                        // el número sagrado de Providence
            Item.useAnimation = Item.useTime = 24;
            Item.mana = 10;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.DD2_DarkMageHealImpact;
            Item.DamageType = DamageClass.Summon;
            Item.autoReuse = true;
            Item.buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.Investigaciones.DazzlingStabberBuff>();
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.DazzlingStabber>();

            // (Calamity: RarityTurquoiseBuyPrice + rare Turquoise — la
            // casa: el morado vanilla del nivel del arma + su precio.)
            Item.value = Item.buyPrice(gold: 30);
            Item.rare = ItemRarityID.Purple;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            // LA GLOWMASK del original (el pase de brillo del arma en el
            // suelo — la misma textura Glow de Calamity, copiada).
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/Investigaciones/DazzlingStabberStaffGlow",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            if (glow != null)
                spriteBatch.Draw(glow, Item.position - Main.screenPosition + new Vector2(
                    Item.width * 0.5f, Item.height * 0.5f) - glow.Size() * 0.5f * scale, null,
                    Color.White * 0.85f, rotation, glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === EL SHOOT DEL ORIGINAL (calco): las TRES DAGAS por ranura
            //     de minion (cristal / roca / llama) nacen en el cursor y
            //     se reparten el abanico de descanso ===
            float usedMinionSlots = 0;
            foreach (var minions in Main.ActiveProjectiles)
            {
                if (minions.owner == player.whoAmI)
                    usedMinionSlots += minions.minionSlots;
            }

            int minionsSummoned = 0;
            for (int i = 0; i < 3; i++)
            {
                if (usedMinionSlots + 1 + i > player.maxMinions)
                    break;

                var minion = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI, 0, 0, i + 1);
                minion.originalDamage = Item.damage;
                minionsSummoned++;
            }

            player.AddBuff(Item.buffType, 2);

            int projCount = player.ownedProjectileCounts[type] + minionsSummoned;
            float angleMax = MathHelper.ToRadians(360f);
            if (projCount == 100)
                angleMax = 0f;
            float index = 1f;
            if (projCount > 30)
            {
                angleMax += MathHelper.ToRadians((projCount - 30) * 2.5f);
            }
            angleMax = angleMax > MathHelper.ToRadians(360f) ? MathHelper.ToRadians(360f) : angleMax; // More intuitive than using a min function
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == type && p.owner == player.whoAmI)
                {
                    int adjustedProjCount = (int)(projCount);
                    p.ai[1] = index / adjustedProjCount * angleMax - angleMax / 2f;
                    p.netUpdate = true;
                    index++;
                }
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL PERGAMINO DE LA INVESTIGACIÓN — las claves viven en el
            // hjson (×3 idiomas; sin clave, la línea no se dibuja).
            void Linea(string clave)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.DazzlingStabberStaff." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    return;
                tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo");
            Linea("Linea1");
            Linea("Linea2");
            Linea("Linea3");
            Linea("Linea4");
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA para las armas de investigación:
            // la madera de siempre (la vía de prueba).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
