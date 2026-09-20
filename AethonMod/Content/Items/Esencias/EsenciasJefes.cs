using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Globals;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Esencias
{
    // ======================================================================
    //  v6.48 — LAS ESENCIAS DE LOS GUARDIANES (Los Huesos del Festín,
    //  versión metales del usuario: los jefes de oleada dejan caer el
    //  ALMA con su nombre — "Esencia del Rey Gelatina" — con el sprite
    //  de un alma del color del jefe que la suelta).
    //
    //  QUÉ HACEN: cada esencia sube UN NIVEL COMPLETO al Grimorio del
    //  Eterno (la primera copia de la barra rápida — la misma voz que
    //  manda en las stats). El Testigo también las VENDE a 10 monedas
    //  de platino… pero SOLO a quien derrotó la oleada 10.
    //
    //  Contrato de la casa: consumibles de pruebas (stack 30 — el festín
    //  paga en almas), rareza Quest, aviso localizado si no hay libro.
    // ======================================================================

    /// <summary>EL ALMA COMÚN: la maquinaria de las esencias de los jefes.</summary>
    public abstract class EsenciaDeJefeItem : ModItem
    {
        /// <summary>El nombre del guardián (para el aviso de "no hay libro").</summary>
        protected abstract string ClaveJefe { get; }

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 10, 0, 0); // el precio del Testigo
        }

        /// <summary>
        /// La ESENCIA del guardián caído: el alma del jefe de oleada —
        /// sube UN NIVEL COMPLETO al Grimorio del Eterno.
        /// v6.50 — EL ALMA ES DE LA AUTORIDAD: en MP el nivel real vive en
        /// el server (la XP la cuenta él); el uso sincronizado la sube en
        /// SU copia y EcoRed.MsgLibro lleva el nivel nuevo al portador
        /// (que celebra el delta en su pantalla). En SP: como siempre.
        /// </summary>
        public override bool? UseItem(Player player)
        {
            // LA PRIMERA COPIA VISIBLE del libro (slots 0–9).
            Item libro = null;
            for (int i = 0; i < 10; i++)
            {
                Item inv = player.inventory[i];
                if (inv != null && !inv.IsAir &&
                    inv.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                {
                    libro = inv;
                    break;
                }
            }

            if (libro == null)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Esencia.SinLibro"),
                        new Color(255, 160, 90));
                return false;
            }

            var sl = libro.GetGlobalItem<ShardLevelItem>();
            if (sl == null) return false;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // El cliente solo avisa: el server sube SU copia (uso
                // sincronizado) y MsgLibro trae el nivel (con su fiesta).
                return true;
            }

            sl.SubirNivelDirecto(libro, 1);
            // LA VOZ del libro probando el alma (variantes por jefe —
            // la clave general con 3 muestras para no repetir).
            string sabor = EcoLib.ElegirVariante("Mods.AethonMod.Esencia.Sabor", 3);
            if (!string.IsNullOrEmpty(sabor))
                EcoLib.Hablar(sabor, new Color(245, 196, 81),
                    rugido: false, escala: 0.55f);

            // El alma se disuelve en chispas doradas.
            for (int i = 0; i < 18; i++)
                Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-6f, 2f)),
                    100, new Color(245, 196, 81), 1.2f);

            // v6.50 — el nivel camina: el server acaba de subir su copia;
            // el cliente del portador recibe la nueva (y su celebración).
            EcoRed.SincronizarLibros(player);

            return true;
        }

        /// <summary>
        /// EL MAPA guardián → esencia (lo usa OleadaNPC al soltar el alma y
        /// el Testigo para vendérselas). Devuelve 0 si el NPC no es de los
        /// guardianes de oleada.
        /// </summary>
        public static int DeNPC(int npcType)
        {
            if (npcType == NPCID.KingSlime)
                return ModContent.ItemType<EsenciaDelReyGelatina>();
            if (npcType == NPCID.EyeofCthulhu)
                return ModContent.ItemType<EsenciaDelOjoDeCthulhu>();
            if (npcType == NPCID.Deerclops)
                return ModContent.ItemType<EsenciaDeDeerclops>();
            if (npcType == NPCID.QueenBee)
                return ModContent.ItemType<EsenciaDeLaAbejaReina>();
            if (npcType == NPCID.EaterofWorldsHead)
                return ModContent.ItemType<EsenciaDelDevoradorDeMundos>();
            if (npcType == NPCID.BrainofCthulhu)
                return ModContent.ItemType<EsenciaDelCerebroDeCthulhu>();
            if (npcType == NPCID.SkeletronHead)
                return ModContent.ItemType<EsenciaDeSkeletron>();
            return 0;
        }

        /// <summary>Las SIETE esencias, en orden de oleadas (la tienda del Testigo).</summary>
        public static int[] Todas()
        {
            return new int[]
            {
                ModContent.ItemType<EsenciaDelReyGelatina>(),
                ModContent.ItemType<EsenciaDelOjoDeCthulhu>(),
                ModContent.ItemType<EsenciaDeDeerclops>(),
                ModContent.ItemType<EsenciaDeLaAbejaReina>(),
                ModContent.ItemType<EsenciaDelDevoradorDeMundos>(),
                ModContent.ItemType<EsenciaDelCerebroDeCthulhu>(),
                ModContent.ItemType<EsenciaDeSkeletron>(),
            };
        }
    }

    /// <summary>EL REY GELATINA: el alma dulce del trono que rueda.</summary>
    public class EsenciaDelReyGelatina : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "KingSlime";
    }

    /// <summary>EL OJO DE CTHULHU: la vigilia hambrienta, ahora soñando contigo.</summary>
    public class EsenciaDelOjoDeCthulhu : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "EyeofCthulhu";
    }

    /// <summary>DEERCLOPS: la escarcha ancestral del invierno caminante.</summary>
    public class EsenciaDeDeerclops : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "Deerclops";
    }

    /// <summary>LA ABEJA REINA: la miel real con aguijón.</summary>
    public class EsenciaDeLaAbejaReina : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "QueenBee";
    }

    /// <summary>EL DEVORADOR DE MUNDOS: cien fauces corrompidas en un hilo.</summary>
    public class EsenciaDelDevoradorDeMundos : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "EaterofWorlds";
    }

    /// <summary>EL CEREBRO DE CTHULHU: los reflejos que gritan en rojo.</summary>
    public class EsenciaDelCerebroDeCthulhu : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "BrainofCthulhu";
    }

    /// <summary>SKELETRON: la maldición copiada a mano alzada.</summary>
    public class EsenciaDeSkeletron : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "Skeletron";
    }
}
