using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.93 — EL SELLO DEL ARROJO (la letra del usuario): «recuerda todo
    //  lo que el grimorio vaya a consumir debe ser consagrado en el Altar,
    //  menos las esencias, pero para evitar que el grimorio se coma las
    //  esencias nada más caer, el grimorio solo comerá lo que el jugador
    //  arroje al suelo, cualquier cosa que se caiga de un enemigo no será
    //  consumida por el grimorio, entiendes».
    //
    //  LA MARCA: un item caído al suelo solo es PLATO si nació del gesto
    //  del portador — ARROJADO (tecla de soltar, arrastre fuera del
    //  inventario, o el spawn asistido del jugador). Los drops de los
    //  enemigos (EntitySource_Loot), los cofres y los escupitajos del
    //  propio sistema (GetSource_Misc) nacen SIN la marca: el grimorio
    //  NI LOS PRUEBA — ni las esencias recién caídas (el caso de la
    //  letra), ni el botín, ni el cambio del Eterno.
    //
    //  El que decide el devoro es la AUTORIDAD (ServirLaMesa corre solo
    //  en SP/servidor): OnSpawn marca en la máquina que nace el item, y
    //  en multijugador la marca VIAJA por el cable con el item (NetSend/
    //  NetReceive — la serialización de SyncItem la llama tML) y por el
    //  guardado del mundo (SaveData/LoadData). JAMÁS se muestra al
    //  jugador: es ley de la mesa, no etiqueta de tienda.
    // ======================================================================
    public class ArrojadoGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        /// <summary>¿Este item caído nació del gesto de ARROJAR de un
        /// jugador? (la marca del plato — sin ella, el Eterno no come).</summary>
        public bool DelJugador = false;

        public override void OnSpawn(Item item, IEntitySource source)
        {
            try
            {
                // EL GESTO (v6.93 — la fuente REAL de este tML): todo lo
                // que sale de las manos del portador — la tecla de soltar
                // y el arrastre fuera del inventario viajan vestidos con
                // EntitySource_DropAsItem (el sello del soltar); y el
                // nacimiento asistido con PADRE JUGADOR (EntitySource_
                // Parent con Entity = Player) también es gesto suyo.
                // Los enemigos (Loot), los cofres (ItemOpen), la pesca
                // (FishedOut) y el sistema (Misc) NO la llevan.
                DelJugador = source is EntitySource_DropAsItem
                    || (source is EntitySource_Parent padre && padre.Entity is Player);
            }
            catch { DelJugador = false; }
        }

        // ================================================================
        //  LA PERSISTENCIA (el .wld guarda los items del suelo con SUS
        //  globals — que la marca sobreviva a la recarga del mundo).
        // ================================================================
        public override void SaveData(Item item, TagCompound tag)
        {
            try { if (DelJugador) tag["arrojado"] = true; }
            catch { }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try { DelJugador = tag.GetBool("arrojado"); }
            catch { DelJugador = false; }
        }

        // ================================================================
        //  EL CABLE (MP: el item arrojado nace en el cliente de su dueño
        //  y camina al server con la marca puesta — sin esta vuelta, el
        //  servidor jamás la vería).
        // ================================================================
        public override void NetSend(Item item, BinaryWriter writer)
        {
            try { writer.Write(DelJugador); }
            catch { writer.Write(false); }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try { DelJugador = reader.ReadBoolean(); }
            catch { DelJugador = false; }
        }

        /// <summary>¿ESTE item caído es un plato válido para la mesa del
        /// Eterno? (la marca del arrojo — null-safe para la vieja usanza).</summary>
        public static bool EsArrojado(Item it)
        {
            try
            {
                if (it == null || it.IsAir) return false;
                var marca = it.GetGlobalItem<ArrojadoGlobalItem>();
                return marca != null && marca.DelJugador;
            }
            catch { return false; }
        }
    }
}
