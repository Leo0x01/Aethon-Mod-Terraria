using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Runa de Memoria — item que representa un arma memorizada del juego base.
    /// Equipada en el inventario del fragmento, aplica su comportamiento.
    ///
    /// Este item es un placeholder genérico; el comportamiento real se aplica
    /// leyendo la lista MemorizedRunes del ShardPlayer y modificando el arma activa.
    /// </summary>
    public class MemoryRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.LightRed;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Las runas equipadas se leen desde ShardPlayer.MemorizedRunes.
            // El comportamiento se aplica en NodeEffectSystem + GlobalItem.
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                // Bonus pasivo por cada runa equipada.
                player.GetDamage(DamageClass.Generic) += 0.02f * sp.MemorizedRunes.Count;
            }
        }
    }
}
