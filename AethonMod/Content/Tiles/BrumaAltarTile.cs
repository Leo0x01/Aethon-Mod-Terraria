using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  LA TILE DE BRUMA (v6.50.130) — EL ALTAR PRINCIPAL DE LA LETRA:
    //  «este item de bruma sera usado como altar principal y cualquier
    //  cosa que se lance se convierte en Esencia Corrupta, pero las cosas
    //  aptas para corromper como las armas de cualquier tipo obtendran la
    //  etiqueta corrupto y ya sabes no se podran modificar, ademas que no
    //  sea necesario lanzar nada, que desde el inventario funcione en el
    //  apartado creacion».
    //
    //  LA TILE ES LA BRUMA COLOCADA: 1×1 (como el altar demoníaco de
    //  vanilla — NO sólida: se camina a través, los items caen a través
    //  hasta el piso y la ZONA los pesca), casi INVISIBLE por sí misma
    //  (su textura es un mechón tenue) porque EL ESPECTÁCULO lo pone la
    //  ZONA: al colocarla registra SU zona de corrupción
    //  (LaCaidaSistema.RegistrarAltar(colocada: true)) y el
    //  AltarDevoradorSistema viste el sitio con las columnas de bruma,
    //  el resplandor del piso y el vórtice al comer — el MISMO
    //  minibioma portátil.
    //
    //  LA ESTACIÓN DE FABRICACIÓN: las recetas de la corrupción
    //  (RecetasCorrupcionSistema) exigen ESTA tile — corromper armas y
    //  convertir todo en Esencia Corrupta DESDE EL INVENTARIO, sin
    //  lanzar nada.
    //
    //  Y ES RECOGIBLE: el pico la devuelve al inventario (KillTile →
    //  RemoverAltar — el altar VIAJA contigo, la letra: «colocarla
    //  donde quieran»).
    // ======================================================================
    public class BrumaAltarTile : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/BrumaAltarTile";

        public override void SetStaticDefaults()
        {
            // EL CUERPO: como el altar demoníaco — un hueco que NO estorba
            // el paso (los items caen a través y la zona los pesca abajo).
            Main.tileSolid[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.PreventsSandfall[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            // LA COLOCACIÓN 1×1 anclada al piso sólido (la bruma reposa
            // donde se pueda pisar alrededor).
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom =
                new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
            TileObjectData.addTile(Type);

            // EL MAPA: la bruma violeta (el altar portátil se anuncia).
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(150, 90, 220), nombre);
        }

        /// <summary>LO QUE SUELTA al picarla: la bruma misma (el altar
        /// VIAJA contigo — GetItemDrops, el hook verificado al IL de
        /// ESTE tML: la tile suelta EXACTAMENTE el item de bruma).</summary>
        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            Item bruma = new Item();
            bruma.SetDefaults(ModContent.ItemType<global::AethonMod.Content.Items.Altar.BrumaDelCrater>());
            yield return bruma;
        }

        /// <summary>LA LUZ DE LA BRUMA: violeta con su latido (la
        /// convención de la casa — ESTE tML 2026.08 no trae ModifyLight en
        /// ModTile: NearbyEffects + Lighting.AddLight, como el altar
        /// dorado de la .128 y el libro dormido).</summary>
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (!closer) return;
            float latido = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f + i * 0.7f);
            Lighting.AddLight(i * 16 + 8, j * 16 + 8,
                0.30f * latido, 0.16f * latido, 0.52f * latido);
        }

        /// <summary>LA BRUMA SE RECOGE: al picarla, la zona colocada se
        /// despide (RemoverAltar — su puerta era la bruma y la bruma se
        /// va en el bolsillo) y el item vuelve al inventario (ItemDrop).
        /// (En MP el server también corre KillTile al picar la tile: la
        /// autoridad limpia la lista; la validación de cada 10 s es la red
        /// de seguridad.)</summary>
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail || effectOnly) return;
            try
            {
                Systems.LaCaidaSistema.RemoverAltar(i, j);
                // La zona nueva viaja al aire YA (en listen-server/SP la
                // autoridad es esta misma máquina).
                if (Main.netMode == NetmodeID.Server)
                    Systems.EcoRed.DifundirZonas();
            }
            catch { }
        }
    }
}
