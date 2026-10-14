using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.50.122 — EL GRIMORIO MENOR: LA SEGUNDA MASCOTA (la miniatura
    //  del LIBRO — no confundir con el Aethon Menor, la miniatura del
    //  JEFE de luz: son DOS mascotas distintas, la letra del usuario
    //  que desenredó los conceptos).
    //
    //  Vive dentro de la Bolsa de la Cría junto a su hermana de luz —
    //  la bolsa llega en cada entrada al mundo (TestingPlayer, como las
    //  veintiuna bolsas del kit) y tras la primera victoria de la
    //  familia (la vía canónica de la .120).
    // ======================================================================
    /// <summary>
    /// GrimorioMenorItem — EL GRIMORIO MENOR (la mascota minigrimorio).
    ///
    /// Invoca la miniatura viva del Grimorio del Eterno: flota a tu
    /// hombro con su ojo despierto, se aburre si te quedas quieto y
    /// vuela a MOLESTAR a los vecinos con sus mini minions y su
    /// sombrilla de página.
    /// </summary>
    public class GrimorioMenorItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.consumable = false;              // reutilizable: es de pruebas
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
            Item.buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>();
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.GrimorioMenorPet>();
        }

        public override bool? UseItem(Player player)
        {
            // El buff nace con el uso (la mascota lo sostiene después).
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600, true);
            return true;
        }
    }
}
