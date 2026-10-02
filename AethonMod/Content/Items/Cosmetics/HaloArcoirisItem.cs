using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Cosmetics
{
    // ======================================================================
    //  v6.50.54 — EL HALO ARCOÍRIS (la petición: «toma el arcoiris de la
    //  forma ascendida 3 y crea un nuevo item solo con ese arcoiris, que
    //  sea pequeño y rodee la cabeza del jugador»).
    //
    //  EL ARCOÍRIS DE APOCALIPSIS 4:3, DESTILADO: nada de trono, nada de
    //  cruz, nada de mar de vidrio — SOLO LA BANDA de siete franjas que
    //  rodea al dios en la Forma 3, reducida a UN ARO PEQUEÑO alrededor
    //  de la cabeza (rx ~42 px — el tamaño del halo triple del trono):
    //  la banda horneada (VFXCore.Arcoiris — la MISMA textura de la
    //  Forma 3) girando despacito, montada con SUS doce perlas zodiacales
    //  de color y el rim blanco del borde. Combinable con CUALQUIER forma
    //  ascendida (vive en su propio modo del portador: el 7).
    // ======================================================================
    /// <summary>
    /// HaloArcoirisItem — EL HALO ARCOÍRIS.
    ///
    /// El arcoíris de la Forma Ascendida 3 hecho adorno: un aro pequeño
    /// de siete franjas alrededor de la cabeza. Cosmético puro + una luz
    /// blanca suave. Se entrega al entrar al mundo (RegaloDePruebas).
    /// </summary>
    public class HaloArcoirisItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;          // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest; // la familia de las formas
            Item.value = Item.buyPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // LA BANDERA EN VIVO (el patrón de la casa: el escaneo de
            // PostUpdate la ve en el MISMO tick).
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().HaloArcoiris = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().HaloArcoiris = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // v6.50.50 — SIN RECETA (la petición de la casa: los ítems nuevos
        // se ENTREGAN al entrar al mundo — ShardPlayer.EntregarRegaloDePruebas).

        // ==================================================================
        //  EL ANILLO ANIMADO DEL ICONO (el mismo contrato de la Forma 3:
        //  la banda de siete franjas girando alrededor del ícono en el
        //  MISMO lote de la UI — cero Begin/End, la lección de la .50).
        // ==================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D banda = VFXCore.Arcoiris;
            if (banda == null) return;

            float t = Main.GlobalTimeWrappedHourly;
            Vector2 centro = position + (frame.Size() * 0.5f - origin) * scale;

            float bordeExterno = frame.Width * scale * 0.62f;
            float quad = bordeExterno * 2.174f;
            Vector2 escala = new Vector2(quad / banda.Width, quad / banda.Height);

            float giro = t * 0.35f;
            float alfa = 0.55f + 0.20f * MathF.Sin(t * 2.2f);

            spriteBatch.Draw(banda, centro, null,
                new Color(255, 255, 255, 255) * alfa,
                giro, new Vector2(banda.Width, banda.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
