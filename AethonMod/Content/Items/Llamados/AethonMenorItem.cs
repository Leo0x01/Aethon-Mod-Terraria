using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.50.49 — EL AETHON MENOR: LA MASCOTA DE LUZ.
    //
    //  La letra del usuario: «ademas crea una pequeña mascota de luz que
    //  sea Aethon original pero mas pequeño». Una chispa viva de la Luz
    //  Primordial: su núcleo, su corona de perlas y su arcoíris en
    //  miniatura — MASCOTA DE LUZ de vanilla (la familia del fuego
    //  fatuo: alumbra el mundo con el arcoíris de la casa girando por
    //  el espectro).
    //
    //  El drop de AETHON, LA LUZ PRIMORDIAL (20% — su recuerdo vivo)
    //  + la receta de pruebas de la casa (el mod es de PRUEBAS).
    // ======================================================================
    /// <summary>
    /// AethonMenorItem — EL AETHON MENOR (la mascota de luz).
    ///
    /// Invoca a la miniatura viva de la Luz Primordial: flota a tu
    /// hombro, alumbra con el arcoíris y suelta chispas doradas.
    /// </summary>
    public class AethonMenorItem : ModItem
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
            Item.buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>();
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorPet>();
        }

        public override bool? UseItem(Player player)
        {
            // El buff nace con el uso (la mascota lo sostiene después).
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600, true);
            return true;
        }

        // v6.50.50 — SIN RECETA (la petición: «esos item nuevos no pongas
        // recetas, daselos directamente al jugador, recuerda que todo esto
        // es una prueba»): la mascota se ENTREGA al entrar al mundo
        // (ShardPlayer.EntregarRegaloDePruebas). El drop del 20% del jefe
        // se queda: es su recuerdo vivo, no una receta.
    }
}
