using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  EL SELLO DEL ARROJO (la letra del usuario, v6.93): «recuerda todo
    //  lo que el grimorio vaya a consumir debe ser consagrado en el Altar,
    //  menos las esencias […] el grimorio solo comerá lo que el jugador
    //  arroje al suelo, cualquier cosa que se caiga de un enemigo no será
    //  consumida por el grimorio».
    //
    //  v6.96 — LA CURA DEL HAMBRE MUERTA (la letra: «deje caer cosas para
    //  que el libro las devorara y no lo hace, el grimorio no se come
    //  nada»): LA AUTOPSIA DEL tML. El tiro del jugador (tecla T, arrastre
    //  fuera de la ventana) nace en Player.DropSelectedItem vestido con
    //  una fuente MISC de contexto "ThrowItem" (ItemSourceID.ThrowItem)
    //  — y PEOR: el item que queda viviendo en el suelo es la INSTANCIA
    //  ORIGINAL del inventario (Main.item[idx] = theItemWeDrop): el clon
    //  que recibió OnSpawn muere reemplazado UNA instrucción después.
    //  El marcador viejo (OnSpawn mirando EntitySource_DropAsItem /
    //  EntitySource_Parent) JAMÁS vio un solo plato: por eso el Eterno se
    //  murió de hambre aunque la mesa estuviera llena — ni las esencias,
    //  ni el dinero, ni lo consagrado: NADA llevaba la marca.
    //
    //  LA CURA (tres dedos de la misma mano):
    //   1. OnSpawn reconoce el contexto "ThrowItem" y ANOTA la casilla
    //      del mundo donde vive el clon (con su huella: tipo/pila/prefijo
    //      — el clon y el original son gemelos idénticos).
    //   2. ArrojadoTirosSistema.PostUpdatePlayers corre UN INSTANTE
    //      después (cuando el original YA reemplazó al clon) y marca al
    //      SUPERVIVIENTE de esa casilla — huella verificada: si la
    //      casilla cambió de dueño en el mismo tick, no se marca a nadie.
    //   3. En MP, el cliente que tiró avisa a la AUTORIDAD por la EcoRed
    //      (MsgTiro): el server busca el plato gemelo cerca del tirador
    //      (huella + posición) y lo marca en SU mundo — el mensaje 13
    //      del tML NO viaja con controlThrow (el server JAMÁS simula el
    //      tiro remoto por su cuenta: la marca tiene que caminar).
    //
    //  LA MARCA: un item caído al suelo solo es PLATO si nació del gesto
    //  del portador. Los drops de los enemigos (EntitySource_Loot), los
    //  cofres, la pesca y los escupitajos del sistema (GetSource_Misc de
    //  la casa) nacen SIN la marca: el grimorio NI LOS PRUEBA.
    //
    //  El que decide el devoro es la AUTORIDAD (ServirLaMesa corre solo
    //  en SP/servidor): la marca VIAJA por el cable con el item
    //  (NetSend/NetReceive — la serialización de SyncItem la llama tML)
    //  y por el guardado del mundo (SaveData/LoadData). JAMÁS se muestra
    //  al jugador: es ley de la mesa, no etiqueta de tienda.
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
                DelJugador = EsGestoDelPortador(source);

                // EL REGISTRO DEL TIRO: el clon vive YA en su casilla del
                // mundo y va a morir reemplazado por el ORIGINAL (mismos
                // datos exactos). Se anota la casilla; el superviviente
                // se marca en cuanto el polvo baje (PostUpdatePlayers).
                if (source is EntitySource_Misc misc && misc.Context == "ThrowItem")
                    AnotarElTiro(item);
            }
            catch { DelJugador = false; }
        }

        // ================================================================
        //  v6.96 — ¿ES GESTO DEL PORTADOR? (las cuatro puertas de la
        //  marca — la letra: «solo come lo que el jugador suelta»).
        // ================================================================
        private static bool EsGestoDelPortador(IEntitySource source)
        {
            if (source == null) return false;

            // (1) EL NACIMIENTO ASISTIDO (de siempre): el padre es un
            //     JUGADOR (el spawn asistido del portador).
            if (source is EntitySource_Parent padre && padre.Entity is Player)
                return true;

            // (2) EL ARMA ARROJADA QUE CLAVA (EntitySource_DropAsItem: un
            //     proyectil que se vuelve item al tocar bloque) — SOLO si
            //     el proyectil salió de las manos de un jugador (owner
            //     válido): los de los monstruos (owner 255) NO llevan la
            //     marca («cualquier cosa que se caiga de un enemigo no
            //     será consumida por el grimorio»).
            if (source is EntitySource_DropAsItem caida &&
                caida.Entity is Projectile proyectil &&
                proyectil.owner >= 0 && proyectil.owner < Main.maxPlayers)
                return true;

            // (3) LA BOLSA LLENA (EntitySource_OverfullInventory: lo que
            //     el inventario no pudo guardar cae delante del portador
            //     — la clase lleva AL JUGADOR dentro: gesto suyo es).
            if (source is EntitySource_OverfullInventory)
                return true;

            // (4) EL TIRO DE VERDAD — tecla T / arrastre fuera de la
            //     ventana: el contexto del tML (ItemSourceID.ThrowItem,
            //     el sello del DropSelectedItem).
            if (source is EntitySource_Misc tiro && tiro.Context == "ThrowItem")
                return true;

            return false;
        }

        // ================================================================
        //  v6.96 — EL REGISTRO DE LOS TIROS (la casilla + la huella del
        //  clon). Vivir solo UN tick: OnSpawn anota, PostUpdatePlayers
        //  cobra y limpia — un puente de un solo paso.
        // ================================================================

        /// <summary>LA HUELLA del clon del tiro (el original que lo
        /// reemplaza es su gemelo exacto: mismo tipo, misma pila, mismo
        /// prefijo — la verificación de que la casilla sigue suya).</summary>
        private struct HuellaDelTiro { public int Tipo, Pila, Prefijo; }

        /// <summary>LOS TIROS PENDIENTES: casilla del mundo → huella del
        /// clon que la ocupó al nacer (puede haber varios en el mismo
        /// tick: un tiro por portador).</summary>
        private static readonly Dictionary<int, HuellaDelTiro> _tirosPendientes = new();

        private static void AnotarElTiro(Item clon)
        {
            try
            {
                if (clon == null || clon.IsAir) return;
                int idx = Array.IndexOf(Main.item, clon);
                // OJO: Main.item tiene 401 casillas — la 400 es el
                // ESCENARIO del cliente en MP (NewItem devuelve 400 sin
                // buscar slot cuando netMode==1): el tiro del cliente
                // VIVE ahí hasta que el server le da su casilla de
                // verdad. La anota igual: es SU plato.
                if (idx < 0 || idx >= Main.item.Length) return;
                _tirosPendientes[idx] = new HuellaDelTiro
                {
                    Tipo = clon.type,
                    Pila = clon.stack,
                    Prefijo = clon.prefix
                };
            }
            catch { }
        }

        /// <summary>¿VIVE todavía el gemelo del tiro en esta casilla? (la
        /// verificación de la huella: la casilla solo se marca si su
        /// inquilino actual es el gemelo exacto del clon que la ocupó al
        /// nacer — si el slot cambió de dueño en el mismo tick, nadie
        /// come marcado por equivocación).</summary>
        private static bool LaHuellaVive(Item vivo, HuellaDelTiro huella)
            => vivo != null && !vivo.IsAir
               && vivo.type == huella.Tipo && vivo.stack == huella.Pila
               && vivo.prefix == huella.Prefijo;

        // ================================================================
        //  v6.96 — EL SISTEMA DE LOS TIROS: marca al superviviente de
        //  cada casilla anotada EN CUANTO los jugadores terminaron su
        //  tick (el original ya reemplazó al clon). Corre ANTES del
        //  pulso devorador de la mesa (SystemLoader.PostUpdatePlayers →
        //  … → PreUpdateTime): el plato cae y el Eterno lo ve EN EL
        //  MISMO TICK.
        // ================================================================
        public class ArrojadoTirosSistema : ModSystem
        {
            public override void PostUpdatePlayers()
            {
                try
                {
                    if (Main.gameMenu) return;
                    if (_tirosPendientes.Count == 0) return;

                    foreach (var par in _tirosPendientes)
                    {
                        Item vivo = Main.item[par.Key];
                        if (!LaHuellaVive(vivo, par.Value)) continue;

                        var marca = vivo.GetGlobalItem<ArrojadoGlobalItem>();
                        if (marca == null) continue;
                        bool yaLaTenia = marca.DelJugador;
                        marca.DelJugador = true;

                        // MP (cliente): el mensaje 21 del tiro salió ANTES
                        // de que la marca existiera — la AUTORIDAD se
                        // entera por la EcoRed (MsgTiro: la huella + el
                        // lugar del plato). En SP la marca YA vive en la
                        // única máquina que decide.
                        if (!yaLaTenia && Main.netMode == NetmodeID.MultiplayerClient)
                            global::AethonMod.Content.Systems.EcoRed.AvisarTiroDelPortador(
                                Main.myPlayer, par.Value.Tipo, par.Value.Pila,
                                par.Value.Prefijo, vivo.Center.X, vivo.Center.Y);
                    }
                    _tirosPendientes.Clear();
                }
                catch { try { _tirosPendientes.Clear(); } catch { } }
            }
        }

        // ================================================================
        //  v6.96 — LA MARCA POR CABLE (server): el cliente del portador
        //  tiró el plato y avisó — la AUTORIDAD busca el gemelo cerca del
        //  tirador y lo marca en SU mundo (el que come es el que manda).
        //  La llama EcoRed.Recibir (MsgTiro, solo server).
        // ================================================================
        public static void MarcarPorRed(int quienTiro, int tipo, int pila,
            int prefijo, float x, float y)
        {
            try
            {
                if (Main.netMode != NetmodeID.Server) return;
                Player tirador = quienTiro >= 0 && quienTiro < Main.maxPlayers
                    ? Main.player[quienTiro] : null;
                if (tirador == null || !tirador.active) return;

                // EL GEMELO: mismo tipo, misma pila, mismo prefijo — y
                // nació DELANTE del tirador (600 px de margen: el plato
                // cae a los pies de quien lo suelta).
                Vector2 lugar = new Vector2(x, y);
                int mejor = -1;
                float mejorDist = 600f * 600f;
                for (int d = 0; d < Main.maxItems; d++)
                {
                    Item it = Main.item[d];
                    if (it == null || it.IsAir || !it.active) continue;
                    if (it.type != tipo || it.stack != pila || it.prefix != prefijo) continue;
                    var marca = it.GetGlobalItem<ArrojadoGlobalItem>();
                    if (marca == null || marca.DelJugador) continue;
                    float dist = it.Center.DistanceSQ(lugar);
                    if (dist < mejorDist) { mejorDist = dist; mejor = d; }
                }
                if (mejor >= 0)
                    Main.item[mejor].GetGlobalItem<ArrojadoGlobalItem>().DelJugador = true;
            }
            catch { }
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
