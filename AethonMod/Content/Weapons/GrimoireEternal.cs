using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma de la rama de Artes Mágicas (Magic + Summoner fusionadas).
    /// Lanza proyectiles mágicos de luz y puede invocar minions estelares.
    /// Daño escala con el nivel: daño = nivel × 2.6 + (% maná faltante × 0.5).
    /// </summary>
    public class GrimoireEternal : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip cargados desde Localization.
        }

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 36;
            Item.height = 44;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.ArcaneBolt>();
            Item.shootSpeed = 12f;
            Item.mana = 8;
            Item.noMelee = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                damage += sp.ShardLevel * 2.6f;
            }
            // Aplicar efectos de nodos del árbol de Artes Mágicas.
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMagicEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Magic) += crit;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            float reduction = Systems.NodeEffectSystem.GetManaCostReduction(player);
            mult *= (1f - reduction);
        }

        public override bool CanUseItem(Player player)
        {
            // Reserva inagotable: lanzar con <20 maná es gratis.
            if (Systems.NodeEffectSystem.HasNode(player, "mana-4") && player.statMana < 20)
                return true;
            return player.statMana >= Item.mana;
        }
    }
}
