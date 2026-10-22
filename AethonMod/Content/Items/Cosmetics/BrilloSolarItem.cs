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
    //  v6.51.3 — EL BRILLO DE LOS SOLES RÚNICOS (la letra: «me acabo de
    //  dar cuenta que los soles runicos tienen como un brillo especial,
    //  bueno es como ese brillo… ademas crea un item cosmetico que sea
    //  solo ese brillo especial de los soles runicos, este brilo esta en
    //  los soles runicos de 3 anillos en adelante»).
    //
    //  ESE BRILLO ES DrawTwinkles — los DESTELLOS DE 4 PUNTAS de los
    //  soles de 3 anillos+ (dos cápsulas cruzadas de blanco incandescente
    //  + el corazón dorado, naciendo y muriendo en la banda de la
    //  estrella): este adorno es EXACTAMENTE ESO y NADA MÁS — el halo
    //  (BrilloSolarHalo) dibuja los destellos alrededor del portador (un
    //  trozo de sol caminando por el mundo) con su luz cálida tenue.
    //  Cosmético puro, combinable con todo (vive en su propio halo).
    // ======================================================================
    /// <summary>
    /// BrilloSolarItem — EL BRILLO ESPECIAL DE LOS SOLES RÚNICOS.
    ///
    /// Los destellos de 4 puntas de los soles de 3 anillos, destilados
    /// en adorno: flotan alrededor del portador. Se entrega al entrar al
    /// mundo (RegaloDePruebas).
    /// </summary>
    public class BrilloSolarItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;          // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest; // la familia de los adornos del sol
            Item.value = Item.buyPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // LA BANDERA EN VIVO (el patrón de la casa: el escaneo de
            // PostUpdate la ve en el MISMO tick).
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().BrilloSolar = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().BrilloSolar = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // ==================================================================
        //  EL DESTELLO ANIMADO DEL ICONO (el mismo contrato de la casa: el
        //  brillo de 4 puntas latiendo sobre el ícono en el MISMO lote de
        //  la UI — cero Begin/End).
        // ==================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            float t = Main.GlobalTimeWrappedHourly;
            Vector2 centro = position + (frame.Size() * 0.5f - origin) * scale;

            // EL LATIDO (el destello respira: nace, arde, descansa).
            float arde = MathF.Pow(0.5f + 0.5f * MathF.Sin(t * 1.8f), 1.6f);
            float size = frame.Width * scale * 0.95f * (0.55f + 0.45f * arde);

            // LAS CÁPSULAS CRUZADAS + EL CORAZÓN DORADO (DrawTwinkles).
            spriteBatch.Draw(glow, centro, null,
                new Color(255, 248, 225) * (0.55f + 0.30f * arde), 0f,
                glow.Size() * 0.5f,
                new Vector2(size * 1.9f, 2.4f) / glow.Size(),
                SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, centro, null,
                new Color(255, 248, 225) * (0.55f + 0.30f * arde), 0f,
                glow.Size() * 0.5f,
                new Vector2(2.4f, size * 1.9f) / glow.Size(),
                SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, centro, null,
                new Color(255, 214, 120) * (0.50f + 0.30f * arde), 0f,
                glow.Size() * 0.5f,
                new Vector2(size * 0.9f, size * 0.9f) / glow.Size(),
                SpriteEffects.None, 0f);
        }
    }
}
