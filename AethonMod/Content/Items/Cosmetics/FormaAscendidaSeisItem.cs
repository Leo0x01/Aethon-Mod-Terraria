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
    //  v6.109 — LA FORMA ASCENDIDA 6: EL ESCRIBA DE LA LUZ (la sexta
    //  luz).
    //
    //  LA LETRA DEL USUARIO: «ahora debes mejorar la forma ascendida 5
    //  y crear otras las forma ascendidas, intenta ser creativo y
    //  puedes tomar como ejemplo a auras en internet y a los mismos
    //  angeles».
    //
    //  LA INVESTIGACIÓN (la creatividad pedida — el ángel ESCRIBA):
    //  METATRÓN, la voz que escribe, el escriba celestial — EL ESPEJO
    //  DEL GRIMORIO (el mod ES un libro: ahora el portador también lo
    //  es). El aura camina por el CAMINO ADITIVO del portador
    //  (AuraPortadorHalo, el modo 9) con el perfil de AuraLib y SU
    //  FIRMA: LA GEOMETRÍA SAGRADA — EL CUBO DE METATRÓN (trece nodos
    //  de oro unidos por sus líneas sagradas, girando tras la
    //  espalda), EL HEXAGRAMA DOBLE en contra, EL HALO DE LOS GLIFOS
    //  con la escritura viva orbitando, LA LLUVIA DE VERSOS y EL
    //  SELLO PERFECTO bajo los pies. La paleta del escriba: pergamino
    //  al centro, oro vivo al medio, tinta violeta al borde.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaSeisItem — LA FORMA ASCENDIDA 6 (EL ESCRIBA DE LA
    /// LUZ). La sexta luz: la geometría sagrada de Metatrón hecha
    /// aura. Cosmético puro + VUELO INFINITO (la detección la hacen
    /// los hooks vivos y CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaSeisItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaSeisItem";

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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaSeis = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaSeis = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // SIN RECETA (la casa desde la .50): se ENTREGA al entrar al
        // mundo (ShardPlayer.EntregarRegaloDePruebas) y viaja en la
        // Bolsa de las Auras.

        // ==================================================================
        //  EL ANILLO DEL Sello (la firma de la familia de las formas —
        //  el aro pulsando alrededor del ícono): en la sexta luz el
        //  aro es del ORO DE LA GEOMETRÍA — oro respirando hacia la
        //  tinta violeta (la paleta del escriba de dentro afuera).
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

            // EL PULSO DE LA ESCRITURA (el vaivén lento de la pluma —
            // 1.5 Hz: el escriba escribe MÁS lento que el viento sopla).
            float alfa = 0.42f + 0.20f * MathF.Sin(t * 1.5f);

            spriteBatch.Draw(aro, centro, null,
                new Color(255, 214, 120, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
