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
    //  v6.50.53 — LA FORMA ASCENDIDA 4: EL SERAFÍN.
    //
    //  La letra del usuario: «el arcoiris deberia estar en la forma
    //  ascendida 3, de hecho crea una 4 forma ascendida, asegurate de
    //  que sea algo divino, alas, halo, corona, aura celestial, luz,
    //  bruma, mas luz y destello, mejor iluminacion, para saber mas
    //  investiga en internet sobre el tema y mira otros mod».
    //
    //  LA INVESTIGACIÓN (pedida — el serafín de Isaías 6, la visión del
    //  templo, el PINÁCULO del lenguaje visual de la divinidad; el mismo
    //  que prestan Diablo, Bayonetta y Final Fantasy):
    //    · EL SERAFÍN = «EL QUE ARDE» (saraph): el cuerpo entero es
    //      FUEGO — lenguas de llama subiendo por el contorno.
    //    · «SEIS ALAS TENÍA… CON DOS SE CUBRÍA EL ROSTRO, CON DOS SE
    //      CUBRÍA LOS PIES Y CON DOS VOLABA» (Is 6:2) — TRES PARES.
    //    · «EL UMBRAL SE CONMOVIÓ… Y LA CASA SE LLENÓ DE HUMO» (Is 6:4)
    //      — LA BRUMA SANTA.
    //    · EL TRISAGIÓN — «SANTO, SANTO, SANTO» (Is 6:3): el HALO TRIPLE.
    //    · LA CORONA DEL REY DE GLORIA (Ap 19:12): el bedel de picos.
    //  (El arcoíris se queda en la Forma 3 — EL TRONO de Ap 4:3, donde
    //  el usuario lo pidió; el serafín es FUEGO BLANCO Y ORO.)
    // ======================================================================
    /// <summary>
    /// FormaAscendidaCuatroItem — LA FORMA ASCENDIDA 4 (EL SERAFÍN).
    ///
    /// La cuarta luz: el que arde — las seis alas, el cuerpo de fuego,
    /// el halo triple con el trisagión, la corona del Rey de Gloria,
    /// los rayos de Dios, la bruma santa y el destello del corazón.
    /// Cosmético puro + VUELO INFINITO (la detección la hacen los hooks
    /// vivos y CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaCuatroItem : ModItem
    {
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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaCuatro = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaCuatro = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // SIN RECETA (la casa desde la .50): se ENTREGA al entrar al
        // mundo (ShardPlayer.EntregarRegaloDePruebas).

        // ==================================================================
        //  EL ANILLO DORADO DEL ICONO (la firma de la familia de las
        //  formas — la .52 le dio a la Forma 3 su aro ARCOÍRIS animado;
        //  el serafín lleva el suyo DORADO, el fuego blanco y oro de su
        //  paleta): un Ring pulsando alrededor del icono, dibujado en el
        //  MISMO lote de la UI (cero Begin/End: la lección de la .50 —
        //  nunca se toca el lote del llamador).
        // ==================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D aro = VFXCore.Ring;
            if (aro == null) return;

            float t = Main.GlobalTimeWrappedHourly;

            // EL CENTRO EXACTO del ícono dibujado (independiente de la
            // convención de origin del llamador).
            Vector2 centro = position + (frame.Size() * 0.5f - origin) * scale;

            // EL ALCANCE: el aro abrazando el borde del ícono.
            float bordeExterno = frame.Width * scale * 0.66f;
            float quad = bordeExterno * 2.174f;
            Vector2 escala = new Vector2(quad / aro.Width, quad / aro.Height);

            // EL PULSO DEL FUEGO (el latido del serafín — 2.4 Hz, el del
            // destello del corazón de EmitirDivino4).
            float alfa = 0.45f + 0.20f * MathF.Sin(t * 2.4f);

            spriteBatch.Draw(aro, centro, null,
                new Color(255, 224, 138, 255) * alfa,
                0f, new Vector2(aro.Width, aro.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
