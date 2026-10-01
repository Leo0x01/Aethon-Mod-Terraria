using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

        public override void AddRecipes()
        {
            // LA RECETA DEL TRONO: la Forma 2 + los fragmentos del
            // génesis de la Luz Primordial (su jefe la deja caer) — la
            // tercera luz se FORJA de las dos primeras.
            CreateRecipe()
                .AddIngredient< FormaAscendidaDosItem >(1)
                .AddIngredient< global::AethonMod.Content.Items.GenesisShard >(20)
                .AddTile(Terraria.ID.TileID.WorkBenches)
                .Register();
        }
    }
}
