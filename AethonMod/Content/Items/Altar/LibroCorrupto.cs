using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    // ======================================================================
    //  LibroCorrupto — v6.86 — EL LIBRO CORRUPTO (la letra del peldaño 20
    //  del Despertar): « sera un libro con el mismo sprite que los libros
    //  normales de la mazmorra pero de color negro, y este solo se
    //  consigue con una probabilidad del 1% rompiendo libros en la
    //  mazmorra, o con una probabilidad de 1% abriendo cofres o que
    //  directamente lo suelte la muralla de carne con una probabilidad
    //  del 50% y solo se puede conseguir 1 por jugador, asi que una vez
    //  que un jugador consigue un libro ya no puede conseguir mas, ademas
    //  a penas el libro entre al inventario el grimorio debe decir un
    //  comentario sobre eso, y pedir al jugador que lo deje caer al
    //  suelo, una vez el libro este en el suelo el grimorio lo comera
    //  siempre que este por debajo del nivel 20 de despertar y con esto
    //  el requisito del despertar el nivel 20 sera marcado como valido
    //  sin importar en que nivel este su despertar actual».
    //
    //  LAS TRES FUENTES DE LA LETRA:
    //   · 1% rompiendo LIBROS de la mazmorra (LibrosDeLaMazmorraTile,
    //     aquí mismo — el GlobalTile de los TileID.Books en la mazmorra).
    //   · 1% abriendo COFRES (LaRuletaDeLosCofres — vive en
    //     DespertarGrimorioSistema, el pulso de la casa).
    //   · 50% la MURALLA DE CARNE lo suelta directo (CazaJefesGlobalNPC).
    //  EL SELLO DEL UNO POR CAZADOR: OnPickup — quien ya levantó el
    //  suyo NO puede levantar otro (return false: el libro rebota).
    // ======================================================================
    public class LibroCorrupto : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }

        // El PNG (mismo sprite que los libros de la mazmorra, negro):
        // lo genera tools/gen_libro_corrupto_v686.py.
    }

    /// <summary>EL SELLO DEL UNO POR CAZADOR: apenas el libro ENTRA al
    /// inventario, el grimorio HABLA (el comentario de la letra + el
    /// pedido de soltarlo al suelo) — y quien ya levantó el suyo, jamás
    /// levanta otro (OnPickup false: el libro rebota al suelo).</summary>
    public class LibroCorruptoGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<LibroCorrupto>();

        public override bool OnPickup(Item item, Player player)
        {
            try
            {
                var cazador = player.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador == null) return true;

                // UNO POR CAZADOR, PARA SIEMPRE (la letra): «una vez que un
                // jugador consigue un libro ya no puede conseguir mas».
                if (cazador.YaTuvoLibro) return false;

                cazador.YaTuvoLibro = true;

                // EL COMENTARIO DEL GRIMORIO (la letra: «a penas el libro
                // entre al inventario el grimorio debe decir un comentario
                // sobre eso, y pedir al jugador que lo deje caer al suelo»)
                // — EcoRed resuelve el texto con SU hjson y lo dice en la
                // pantalla del portador (SP directo, MP empaquetado).
                Systems.EcoRed.HablarAlPortador(player,
                    "Mods.AethonMod.Despertar.LibroEnInventario",
                    new Color(178, 110, 236));
            }
            catch { }
            return true;
        }
    }

    /// <summary>LA PRIMERA FUENTE (1% rompiendo libros en la mazmorra):
    /// el GlobalTile de los TileID.Books — al romper un libro COLOCADO
    /// en la mazmorra (con jugador cerca en zona de mazmorra), el 1% de
    /// las veces cae ADEMÁS un Libro Corrupto (el libro de vanilla sigue
    /// cayendo: esto es un extra, no un reemplazo).</summary>
    public class LibrosDeLaMazmorraTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type,
            ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            try
            {
                if (type != TileID.Books) return;
                if (fail || effectOnly || noItem) return;
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (!EnLaMazmorra(i, j)) return;

                // EL 1% DE LA LETRA (cada libro roto tira una vez).
                if (!Main.rand.NextBool(100)) return;

                int idx = Item.NewItem(new EntitySource_TileBreak(i, j),
                    i * 16, j * 16, 16, 16,
                    ModContent.ItemType<LibroCorrupto>(), 1);
                if (idx >= 0 && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
            }
            catch { }
        }

        /// <summary>¿Este tile está en la MAZMORRA? (la letra dice «rompiendo
        /// libros en la mazmorra»): la zona de mazmorra de cualquier jugador
        /// vivo cerca del tile — la regla de la casa para chequeos de zona
        /// fuera de jugador).</summary>
        private static bool EnLaMazmorra(int i, int j)
        {
            Vector2 pos = new Vector2(i * 16f + 8f, j * 16f + 8f);
            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player pl = Main.player[p];
                if (pl == null || !pl.active) continue;
                if (!pl.ZoneDungeon) continue;
                if (pl.Center.DistanceSQ(pos) > 700f * 700f) continue;
                return true;
            }
            return false;
        }
    }
}
