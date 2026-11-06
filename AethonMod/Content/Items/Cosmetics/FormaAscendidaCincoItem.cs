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
    //  v6.102 — LA FORMA ASCENDIDA 5: EL VIENTO DEL ESPÍRITU (la quinta
    //  luz).
    //
    //  LA LETRA DEL USUARIO: «crear una nueva La Forma Ascendida,
    //  recuerda que esta forma ascendida, a esta nueva forma debes
    //  darle aura, ya sabes como crear aura y a esa aura añade una
    //  capa de turbulencia suave que solo afecte al aura».
    //
    //  LA INVESTIGACIÓN (la iconografía del ESPÍRITU — la tercera
    //  persona de la divinidad: la única que se pinta MOVIÉNDOSE):
    //  ruach/pneuma — EL VIENTO. El aura camina por el CAMINO
    //  ADITIVO del portador (AuraPortadorHalo, el modo 8) con el
    //  perfil de AuraLib y SU FIRMA: LA CAPA DE TURBULENCIA SUAVE
    //  (Turbulencia 1, 14 hebras — el cizallamiento de las dos
    //  corrientes que SOLO vive en la banda del aura, 0.68R..0.96R:
    //  el cuerpo del portador y TODO lo demás quedan intactos).
    //  La paleta del aliento: plata al centro, teal pálido al medio,
    //  violeta al borde.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaCincoItem — LA FORMA ASCENDIDA 5 (EL VIENTO DEL
    /// ESPÍRITU). La quinta luz: el aura de las cinco luces con su
    /// capa de turbulencia suave que solo afecta al aura. Cosmético
    /// puro + VUELO INFINITO (la detección la hacen los hooks vivos y
    /// CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaCincoItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaCincoItem";

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
            // LA BANDERA EN VIVO (el patrón del fix del vuelo de la
            // v6.50.49: UpdateEquips la enciende, PostUpdateEquips la ve
            // en el MISMO tick — el vuelo infinito corre de verdad).
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaCinco = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaCinco = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // SIN RECETA (la casa desde la .50): se ENTREGA al entrar al
        // mundo (ShardPlayer.EntregarRegaloDePruebas).

        // ==================================================================
        //  EL ANILLO DEL Sello (la firma de la familia de las formas —
        //  el aro pulsando alrededor del ícono): en la quinta luz el
        //  aro es el del ALIENTO — teal respirando hacia violeta (la
        //  paleta del aura de dentro afuera).
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

            // EL PULSO DEL ALIENTO (el vaivén lento del viento — 1.9 Hz:
            // la quinta luz respira MÁS lento que el fuego del serafín).
            float alfa = 0.42f + 0.20f * MathF.Sin(t * 1.9f);

            spriteBatch.Draw(aro, centro, null,
                new Color(150, 240, 210, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
