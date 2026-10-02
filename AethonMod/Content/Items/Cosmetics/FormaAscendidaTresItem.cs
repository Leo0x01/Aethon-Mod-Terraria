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
    //  v6.50.49 — LA FORMA ASCENDIDA 3: EL TRONO.
    //
    //  La letra del usuario: «de hecho agrega un 3 forma ascendida que
    //  se vea aun mas divino, investiga mods y busca en internet como es
    //  verse con divinidad celestial para la 3ra forma ascendida».
    //
    //  LA INVESTIGACIÓN (iconografía del trono celestial — el lenguaje
    //  visual CANÓNICO de la divinidad, Apocalipsis 4 / Ezequiel 1 / la
    //  Maiestas Domini del arte sacro — el mismo que prestan Diablo,
    //  Bayonetta y Final Fantasy):
    //    · «Y había un ARCOÍRIS ALREDEDOR DEL TRONO» (Ap 4:3) — el
    //      anillo prismático: cada perla del aro lleva SU color del
    //      espectro, girando lento.
    //    · «Un MAR DE VIDRIO semejante al cristal» (Ap 4:6) — el suelo
    //      de cristal bajo los pies del portador.
    //    · «SIETE LÁMPARAS DE FUEGO ardiendo delante del trono» (Ap 4:5
    //      — los siete espíritus de Dios) — siete llamas orbitando.
    //    · «UNA RUEDA DENTRO DE OTRA RUEDA… y sus aros llenos de OJOS»
    //      (Ez 1 — los OFANIM, las ruedas del trono) — dos anillos
    //      contrarrotantes sembrados de ojos de luz.
    //    · LA MAIESTAS DOMINI — la CRUZ DE LUZ: la columna vertical del
    //      cielo y el brazo horizontal del horizonte DETRÁS del dios (el
    //      mandorla cruzado del Cristo en gloria — el gesto divino por
    //      excelencia del arte occidental).
    //    · LA CORONA DE VEINTICUATRO ESTRELLAS (los veinticuatro
    //      ancianos que arrojan sus coronas, Ap 4:10) — tres arcos de
    //      estrellas sobre la cabeza.
    //  Y LAS ALAS PRISMÁTICAS: las 28 plumas de la Forma 2, con cada
    //  pluma llevando SU matiz del arcoíris — el serafín del trono.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaTresItem — LA FORMA ASCENDIDA 3 (EL TRONO).
    ///
    /// La tercera luz: la divinidad celestial canónica — el arcoíris
    /// alrededor del trono, el mar de vidrio, las siete lámparas de
    /// fuego, las ruedas de ofanim y la cruz de luz de la Maiestas
    /// Domini. Cosmético puro + VUELO INFINITO (la detección la hacen
    /// los hooks vivos y CosmeticPlayer).
    /// </summary>
    public class FormaAscendidaTresItem : ModItem
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
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaTres = true;
        }

        public override void UpdateVanity(Player player)
        {
            // En el hueco de vanidad también: un cosmético es un
            // cosmético vista donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendidaTres = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        // v6.50.50 — SIN RECETA (la petición: «esos item nuevos no pongas
        // recetas, daselos directamente al jugador, recuerda que todo esto
        // es una prueba»): el trono se ENTREGA al entrar al mundo
        // (ShardPlayer.EntregarRegaloDePruebas).

        // ==================================================================
        //  v6.50.52 — EL ARCOÍRIS DEL ICONO («no se ve el arcoíris en el
        //  item de la forma ascendida 3»). El borde que traía el png era
        //  un matiz desaturado de 4-5 px que a escala de inventario nadie
        //  veía. DOS capas ahora:
        //    (1) EL ARO HORNEADO — el png del ítem lleva un borde
        //        ARCOÍRIS VIVO por ángulo (rojo arriba girando por el
        //        espectro) — visible en el inventario, el hotbar, el
        //        suelo y el boticario, SIN código;
        //    (2) EL ANILLO ANIMADO — la banda de siete franjas
        //        (VFXCore.Arcoiris — la MISMA banda que gira alrededor
        //        del trono de la Forma 3 en juego) girando despacito
        //        alrededor del icono con su pulso — dibujada en el
        //        MISMO lote de la UI (cero Begin/End: la lección de la
        //        .50 — nunca se toca el lote del llamador).
        // ==================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D banda = VFXCore.Arcoiris;
            if (banda == null) return;

            float t = Main.GlobalTimeWrappedHourly;

            // EL CENTRO EXACTO del ícono dibujado (independiente de la
            // convención de origin del llamador: el centro del sprite en
            // pantalla ES position + (medioFrame − origin)·scale).
            Vector2 centro = position + (frame.Size() * 0.5f - origin) * scale;

            // EL ALCANCE: la banda abrazando el borde del ícono (su borde
            // externo apenas por fuera del sprite) — la convención de la
            // casa: el cuadro del anillo mide 2.174× su radio visible.
            float bordeExterno = frame.Width * scale * 0.62f;
            float quad = bordeExterno * 2.174f;
            Vector2 escala = new Vector2(quad / banda.Width, quad / banda.Height);

            // EL GIRO LENTO + EL PULSO (la banda de Ap 4:3 alrededor del
            // trono — la misma que gira alrededor de la Forma 3 en juego).
            float giro = t * 0.35f;
            float alfa = 0.55f + 0.20f * MathF.Sin(t * 2.2f);

            spriteBatch.Draw(banda, centro, null,
                new Color(255, 255, 255, 255) * alfa,
                giro, new Vector2(banda.Width, banda.Height) * 0.5f,
                escala, SpriteEffects.None, 0f);
        }
    }
}
