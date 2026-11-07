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
    //  v6.110 — LA FORMA ASCENDIDA 8: LA ESPADA DEL JUICIO (la octava
    //  luz — SAN MIGUEL, el arcángel guerrero).
    //
    //  LA LETRA DEL USUARIO: «y ademas crear la forma ascendida 8, 9 y
    //  10» (la continuación de «intenta ser creativo y puedes tomar
    //  como ejemplo a auras en internet y a los mismos angeles»).
    //
    //  LA INVESTIGACIÓN: el arcángel MIGUEL — «¿Quién como Dios?»: el
    //  que VENCE al dragón (Ap 12:7-8). Su iconografía eterna: LA
    //  ESPADA y LA BALANZA — la espada que decide, la balanza que
    //  pesa. El aura camina por SU propio camino (EmitirDivino8) con:
    //  EL MANTO DEL ARCÁNGEL, LA GRAN ESPADA DE LUZ alzada tras la
    //  espalda, LAS DOS BALANZAS orbitando, LOS PENACHOS DE PLUMAS
    //  BLANCAS cayendo como hojas de acero, EL ESCUDO DE LA FE (el
    //  aro rúnico que gira) y LOS TRAZOS DEL VEREDICTO. Paleta del
    //  arcángel: platino acero · oro del veredicto · violeta real.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaOchoItem — LA FORMA ASCENDIDA 8 (LA ESPADA DEL
    /// JUICIO — la octava luz, el arcángel). Cosmético puro + VUELO
    /// INFINITO (la detección la hacen los hooks vivos y
    /// CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaOchoItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaOchoItem";

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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaOcho = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaOcho = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // SIN RECETA (la casa desde la .50): se ENTREGA al entrar al
        // mundo (ShardPlayer.EntregarRegaloDePruebas) y viaja en la
        // Bolsa de los Cosméticos.

        // ==================================================================
        //  EL ANILLO DEL Sello (la firma de la familia de las formas —
        //  el aro pulsando alrededor del ícono): en la octava luz el
        //  aro es del PLATINO DEL ACERO — platino respirando hacia el
        //  violeta real (la paleta del arcángel de dentro afuera).
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

            // EL PULSO DEL COMBATE (el filo temblando — 2.1 Hz: el
            // arcángel está DESPIERTO, la espada nunca duerme).
            float alfa = 0.42f + 0.20f * MathF.Sin(t * 2.1f);

            spriteBatch.Draw(aro, centro, null,
                new Color(226, 236, 250, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
