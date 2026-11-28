using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
    //  LAS FUENTES DE LA LETRA:
    //   · v6.185 — 50% AL VENCER CUALQUIER JEFE (CazaJefesGlobalNPC): la
    //     fuente principal — una tirada por jefe (la ÚLTIMA pieza de los
    //     jefes de varios cuerpos), y NUNCA si el cazador ya lleva el libro
    //     en el inventario o ya lo consumió.
    //   · 1% rompiendo LIBROS de la mazmorra (LibrosDeLaMazmorraTile,
    //     aquí mismo — el GlobalTile de los TileID.Books en la mazmorra).
    //   · 1% abriendo COFRES (LaRuletaDeLosCofres — vive en
    //     DespertarGrimorioSistema, el pulso de la casa).
    //  EL SELLO DEL INVENTARIO (v6.185, la letra: «si ya esta en el
    //  inventario del jugador este no sale»): OnPickup rebota el libro a
    //  quien ya lleva uno o ya consumió el suyo; quien lo perdió vuelve a
    //  tener derecho (ElLibroPuedeCaerle).
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

    /// <summary>LA RECOGIDA DEL LIBRO (v6.97 — la letra nueva: «el
    /// libro consagrado solo debe caer 1 vez […] una vez que el jugador
    /// consume el libro consagrado este deja de aparecer en el mundo
    /// para siempre y solo aparece para el jugador que no lo ha
    /// consumido en multijugador y el resto que lo consumio no lo
    /// ven»): quien ya CONSUMIÓ el suyo NI LO VE (rebota), quien ya
    /// RECIBIÓ el suyo tampoco (la única caída ya pasó) — SOLO puede
    /// recogerlo quien no ha recibido ninguno… o SU PROPIETARIO
    /// recogiéndolo del suelo (lo arrojó sin querer y el Eterno aún no
    /// lo prueba). Apenas el libro ENTRA al inventario nuevo, el
    /// sello de LA ÚNICA CAÍDA se pone y el grimorio HABLA.</summary>
    public class LibroCorruptoGlobalItem : GlobalItem
    {
        /// <summary>El campo del dueño viaja POR INSTANCIA (la regla del
        /// tML para los GlobalItem con campos: cada ejemplar del libro,
        /// SU dueño).</summary>
        public override bool InstancePerEntity => true;

        /// <summary>v6.97 — ¿QUIÉN recibió ESTE ejemplar? (el nombre del
        /// cazador que lo levantó — viaja con el item para que pueda
        /// recoger SU libro arrojado del suelo sin que le cuente como
        /// segunda caída).</summary>
        public string RecibidoPor = null;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<LibroCorrupto>();

        public override bool OnPickup(Item item, Player player)
        {
            try
            {
                // v6.91 — EL LÍMITE ES DE INVENTARIO: si ya llevas tu
                // libro, este rebota.
                if (LlevaElLibro(player)) return false;

                var cazador = player.GetModPlayer<Players.CazaJefesPlayer>();
                bool esDueno = !string.IsNullOrEmpty(RecibidoPor) &&
                    RecibidoPor == player.name;

                // v6.185 — LA REGLA DEL INVENTARIO (la letra: «si ya esta
                //     en el inventario del jugador este no sale»): quien
                //     ya consumió el suyo tampoco lo levanta; el sello del
                //     «recibido» YA NO BLOQUEA la recogida (los jefes son
                //     la fuente principal: quien perdió su libro vuelve a
                //     tener derecho). El dueño recoge el suyo del suelo.
                if (!esDueno && cazador != null && cazador.LibroUsado)
                    return false;

                // LA ÚNICA CAÍDA queda sellada con la recogida.
                if (cazador != null) cazador.LibroRecibido = true;
                RecibidoPor = player.name;

                // EL COMENTARIO DEL GRIMORIO (la letra: «a penas el libro
                // entre al inventario el grimorio debe decir un comentario
                // sobre eso, y pedir al jugador que lo deje caer al suelo»)
                // — EcoRed resuelve el texto con SU hjson y lo dice en la
                // pantalla del portador (SP directo, MP empaquetado).
                Systems.EcoRed.HablarAlPortador(player,
                    "Mods.AethonMod.Despertar.LibroEnInventario",
                    new Color(178, 110, 236));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroCorrupto", exRegistro); }
            return true;
        }

        // === LA PERSISTENCIA DEL DUEÑO (viaja con el item) ===

        public override void SaveData(Item item, TagCompound tag)
        {
            try { if (!string.IsNullOrEmpty(RecibidoPor)) tag["recibidoPor"] = RecibidoPor; }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroCorrupto", exRegistro); }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try { RecibidoPor = tag.GetString("recibidoPor"); }
            catch { RecibidoPor = null; }
        }

        /// <summary>¿El jugador ya lleva UN Libro Consagrado encima? (el
        /// límite de la letra: 1 por inventario).</summary>
        private static bool LlevaElLibro(Player p)
        {
            if (p == null) return false;
            int tipoLibro = ModContent.ItemType<LibroCorrupto>();
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == tipoLibro) return true;
            }
            return false;
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

                // v6.97 — SOLO SI ALGUIEN CERCA TODAVÍA NO RECIBIÓ el suyo
                //     (la letra: «el libro consagrado solo debe caer 1
                //     vez» — si todos los presentes ya recibieron o
                //     consumieron el suyo, el libro ni cae).
                if (!AlgunCazadorHambriento(i, j)) return;

                // EL 1% DE LA LETRA (cada libro roto tira una vez).
                if (!Main.rand.NextBool(100)) return;

                int idx = Item.NewItem(new EntitySource_TileBreak(i, j),
                    i * 16, j * 16, 16, 16,
                    ModContent.ItemType<LibroCorrupto>(), 1);
                if (idx >= 0 && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroCorrupto", exRegistro); }
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

        /// <summary>v6.185 — ¿Hay UN cazador cerca al que el libro le
        /// PUEDE caer? (la regla del INVENTARIO de la letra: quien ya lo
        /// lleva encima o ya lo consumió no lo recibe; quien lo perdió,
        /// sí). Si todos los presentes ya lo llevan, el libro NO cae.</summary>
        private static bool AlgunCazadorHambriento(int i, int j)
        {
            Vector2 pos = new Vector2(i * 16f + 8f, j * 16f + 8f);
            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player pl = Main.player[p];
                if (pl == null || !pl.active) continue;
                if (pl.Center.DistanceSQ(pos) > 700f * 700f) continue;
                var cazador = pl.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador == null) continue;
                if (Systems.DespertarGrimorioSistema.ElLibroPuedeCaerle(pl, cazador)) return true;
            }
            return false;
        }
    }
}
