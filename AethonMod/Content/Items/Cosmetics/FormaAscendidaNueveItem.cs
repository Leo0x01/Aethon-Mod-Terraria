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
    //  v6.110 — LA FORMA ASCENDIDA 9: LA MÚSICA DE LAS ESFERAS (la
    //  novena luz — GABRIEL, la trompeta que ordena).
    //
    //  LA LETRA DEL USUARIO: «y ademas crear la forma ascendida 8, 9 y
    //  10». LA INVESTIGACIÓN: el arcángel GABRIEL — la VOZ, el que
    //  ANUNCIA (la trompeta que despierta a los muertos) — y LA
    //  MÚSICA DE LAS ESFERAS de Pitágoras: los cielos girando y cada
    //  órbita cantando SU nota; el universo como instrumento. El aura
    //  camina por SU propio camino (EmitirDivino9) con: EL VELO DEL
    //  CORO, LAS ONDAS DEL LLAMADO (anillos concéntricos pulsando al
    //  compás — el sonido hecho visible), LAS NOTAS ORBITANDO (los
    //  glifos de la melodía), LA CURTAINA DE AURORA (el velo de
    //  colores ondeando arriba) y EL DIAPASÓN bajo los pies (el sello
    //  del compás). Paleta de la voz: marfil · rosa alba · violeta
    //  melodía.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaNueveItem — LA FORMA ASCENDIDA 9 (LA MÚSICA DE
    /// LAS ESFERAS — la novena luz, la voz que ordena). Cosmético
    /// puro + VUELO INFINITO.
    /// </summary>
    public class FormaAscendidaNueveItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Cosmetics/FormaAscendidaNueveItem";

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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaNueve = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaNueve = true;
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
        //  pulsando alrededor del ícono): en la novena luz el aro es
        //  del ROSA DEL CORO — rosa alba respirando hacia el violeta
        //  melodía (la paleta de la voz de dentro afuera).
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

            // EL PULSO DEL COMPÁS (el vaivén del coro — 1.2 Hz: la
            // novena luz canta lento, el adagio del cielo).
            float alfa = 0.42f + 0.20f * MathF.Sin(t * 1.2f);

            spriteBatch.Draw(aro, centro, null,
                new Color(255, 178, 204, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
