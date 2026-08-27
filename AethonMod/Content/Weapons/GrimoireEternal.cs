using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma de la rama de Artes Mágicas.
    ///
    /// MANA: Base 0 (no consume mana sin nodos).
    /// Cada nodo Notable del árbol aumenta el costo de mana en 5.
    /// Máximo: 20 de mana por uso (4 notables = 4×5 = 20).
    ///
    /// DAÑO: Escala con el nivel del fragmento.
    /// </summary>
    public class GrimoireEternal : ModItem
    {
        public override void SetStaticDefaults()
        {
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
            Item.mana = 0; // Base: NO consume mana.
            Item.noMelee = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                damage += sp.ShardLevel * 2.6f;
            }
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMagicEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Magic) += crit;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            // Base: 0 mana. Cada Notable asignado suma 5 de mana, hasta max 20.
            int notableCount = 0;
            // Contar nodos Notable del árbol PoE asignados (cached en PoETreeCatalog).
            var tree = Systems.PoETreeCatalog.GetTree(Players.BranchType.Magic);
            foreach (var node in tree.Nodes)
            {
                if (node.Type == Systems.NodeType.Notable && sp.AllocatedNodes.Contains(node.Id))
                    notableCount++;
            }

            int baseManaCost = System.Math.Min(notableCount * 5, 20);

            // Aplicar reducción de mana de nodos específicos (eficiencia, etc).
            // IMPORTANTE: aplicar la reducción SOLO via Item.mana, NO via mult (evitar double-counting).
            float reduction = Systems.NodeEffectSystem.GetManaCostReduction(player);
            int finalCost = (int)(baseManaCost * (1f - reduction));
            finalCost = System.Math.Max(0, finalCost);

            // Forzar el costo de mana del item (se recalcula cada uso).
            Item.mana = finalCost;
            // NO tocar `mult` ni `reduce` — la reducción ya está aplicada en Item.mana.
        }

        public override bool CanUseItem(Player player)
        {
            // Reserva Inagotable (Notable): lanzar con <20 maná es gratis.
            if (Systems.NodeEffectSystem.HasNode(player, "mana-notable") && player.statMana < 20)
                return true;
            return player.statMana >= Item.mana;
        }
    }
}
