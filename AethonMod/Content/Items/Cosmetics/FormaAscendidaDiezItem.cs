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
    //  v6.110 — LA FORMA ASCENDIDA 10: EL ÁRBOL DE LA VIDA (la décima
    //  luz — LA CORONA, Keter: la forma que CIERRA la década).
    //
    //  LA LETRA DEL USUARIO: «y ademas crear la forma ascendida 8, 9 y
    //  10». LA INVESTIGACIÓN: el ÁRBOL DE LA VIDA de la cábala — los
    //  DIEZ SEFIROT (las diez emanaciones) unidos por sus veintidós
    //  senderos: el mapa del cielo entero en un solo diagrama. LA
    //  DÉCADA SE CIERRA CON SU PROPIA FORMA: diez luces, diez
    //  esferas — cada forma ascendida ES un sefirá, y el portador de
    //  la décima viste el ÁRBOL ENTERO con KETER (la corona) sobre su
    //  cabeza. El aura camina por SU propio camino (EmitirDivino10)
    //  con: EL VELO DE LA CORONA, EL ÁRBOL (las diez esferas de oro
    //  unidas por sus senderos de luz, respirando alrededor), KETER
    //  (la corona de tres picos sobre la cabeza), LOS SENDEROS que
    //  PRENDEN en secuencia (la luz corriendo por las venas del
    //  árbol), LAS ESTRELLAS DE LA DÉCADA y EL SELLO CIRCULAR bajo
    //  los pies. Paleta de la corona: blanco corona · oro corona ·
    //  violeta hondo.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaDiezItem — LA FORMA ASCENDIDA 10 (EL ÁRBOL DE LA
    /// VIDA — la décima luz, la corona). Cosmético puro + VUELO
    /// INFINITO.
    /// </summary>
    public class FormaAscendidaDiezItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaDiezItem";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;          // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest; // la familia de las formas
            Item.value = Item.buyPrice(gold: 15);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaDiez = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaDiez = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // SIN RECETA (la casa desde la .50): se ENTREGA al entrar al
        // mundo (ShardPlayer.EntregarRegaloDePruebas) y viaja en la
        // Bolsa de los Cosméticos.

        // ==================================================================
        //  EL ANILLO DEL Sello (la firma de la familia — el aro
        //  pulsando alrededor del ícono): en la décima luz el aro es
        //  del ORO DE LA CORONA — oro respirando hacia el violeta
        //  hondo (la paleta del árbol de dentro afuera).
        // ==================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D aro = VFXCore.Ring;
            if (aro == null) return;

            float t = Main.GlobalTimeWrappedHourly;

            // EL CENTRO EXACTO del ícono dibujado.
            Vector2 centro = position + (frame.Size() * 0.5f - origin) * scale;

            // EL ALCANCE: el aro abrazando el borde del ícono.
            float bordeExterno = frame.Width * scale * 0.66f;
            float quad = bordeExterno * 2.174f;
            Vector2 escala = new Vector2(quad / aro.Width, quad / aro.Height);

            // EL PULSO DE LA DÉCADA (el vaivén de la corona — 1.0 Hz:
            // la décima luz late LENTO, la eternidad no tiene prisa).
            float alfa = 0.42f + 0.20f * MathF.Sin(t * 1.0f);

            spriteBatch.Draw(aro, centro, null,
                new Color(255, 228, 128, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
