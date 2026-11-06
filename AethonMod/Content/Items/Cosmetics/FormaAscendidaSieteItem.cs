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
    //  v6.109 — LA FORMA ASCENDIDA 7: LA ESTRELLA VIVA (la séptima luz
    //  — EL SOL MENOR).
    //
    //  LA LETRA DEL USUARIO: «ahora debes mejorar la forma ascendida 5
    //  y crear otras las forma ascendidas, intenta ser creativo y
    //  puedes tomar como ejemplo a auras en internet y a los mismos
    //  angeles».
    //
    //  LA INVESTIGACIÓN (las auras de internet hechas carne + el ángel
    //  DE LUZ): el portador no VISTE un aura — ES la estrella. El aura
    //  camina por el CAMINO ADITIVO del portador (AuraPortadorHalo, el
    //  modo 10) con el perfil de AuraLib y SU FIRMA: LAS PROMINENCIAS
    //  — los arcos de fuego anclados al anillo que BROTAN, CRECEN y se
    //  DESGARRAN como las llamaradas de un sol de verdad — más EL
    //  NÚCLEO BLANCO latiendo en el pecho, EL CUERPO DE PLASMA con su
    //  granulación, LA CORONA SOLAR de picos ardientes, EL PARPADEO
    //  ESTELAR y LA LLUVIA DE FOTONES. La paleta de la estrella:
    //  nuclear al centro, oro vivo al medio, coral al borde — la luz
    //  que alimenta mundos.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaSieteItem — LA FORMA ASCENDIDA 7 (LA ESTRELLA
    /// VIVA). La séptima luz: el sol menor con sus prominencias.
    /// Cosmético puro + VUELO INFINITO (la detección la hacen los hooks
    /// vivos y CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaSieteItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaSieteItem";

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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaSiete = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaSiete = true;
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
        //  el aro pulsando alrededor del ícono): en la séptima luz el
        //  aro es del FUEGO DE LA CORONA — coral respirando hacia el
        //  oro (la paleta de la estrella de dentro afuera).
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

            // EL PULSO DE LA ESTRELLA (el latido de la fotosfera — dos
            // armónicos, como el sol de verdad que respira).
            float alfa = 0.44f + 0.18f * MathF.Sin(t * 2.3f) + 0.08f * MathF.Sin(t * 5.1f);

            spriteBatch.Draw(aro, centro, null,
                new Color(255, 158, 122, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
