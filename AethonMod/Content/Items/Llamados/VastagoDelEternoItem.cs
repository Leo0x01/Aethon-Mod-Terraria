using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.91 — EL VÁSTAGO DEL ETERNO: LA MASCOTA NUEVA (la letra de la
    //  sesión: «crea una mascota nueva usando esto: Reel 1 —
    //  una sola imagen de personaje →
    //  sprites animados listos para el motor (bloquea la identidad del
    //  personaje, fondo a transparencia real, ciclos de idle/walk sin
    //  animar a mano, exporta atlas)»).
    //
    //  EL PIPELINE APLICADO (el método, documentado):
    //   1. UNA SOLA IMAGEN con identidad única (el librito alado de
    //      ojos grandes, kawaii, fondo croma) — generada con la skill.
    //   2. TRANSPARENCIA REAL por flood-fill desde los bordes (el
    //      método de la investigación: la identidad queda BLOQUEADA — nada de
    //      borrar colores globales) + limpieza de chispas y sombra por
    //      componentes conexas.
    //   3. LA REJILLA DEL RENDER (bloques 8×8 detectados) → escalado
    //      nativo NEAREST a 38×30.
    //   4. LOS CICLOS SIN ANIMAR A MANO: idle (respiración
    //      squash&amp;stretch + parpadeo) y vuelo (bob + inclinación)
    //      → EL ATLAS de 8 frames (40×384) del projectile.
    //   5. QA visual en 3 rondas con el VLM (el método del reel 2:
    //      borrador → render → mirar → refinar): 8/10 charm.
    //
    //  Vive en la Bolsa del Taller de Efectos (el fruto de la
    //  investigación de los reels) y llega como las demás.
    // ======================================================================
    /// <summary>
    /// VastagoDelEternoItem — EL VÁSTAGO DEL ETERNO (la mascota del
    /// pipeline de la investigación de los reels). SE EQUIPA en la ranura de mascota del
    /// equipo (vanityPet) — y responde a la tecla B SOLO EQUIPADA (el
    /// detour de la casa, MascotasTeclaB).
    /// </summary>
    public class VastagoDelEternoItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 1;
            Item.consumable = false;              // reutilizable: es de pruebas
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
            // EL BUFFTYPE: lo mete en la RANURA de mascota (ItemSlot caso
            // 19) y lo que la tecla B lee — SOLO EQUIPADA la activa.
            Item.buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.VastagoDelEternoBuff>();
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.VastagoDelEternoPet>();
        }

        public override bool? UseItem(Player player)
        {
            // El buff nace con el uso (y la mascota lo sostiene — el
            // patrón canónico de la casa).
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.VastagoDelEternoBuff>(), 3600, true);
            return true;
        }
    }
}
