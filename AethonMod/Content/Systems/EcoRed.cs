using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// EcoRed — v6.49 — LA VOZ CAMINA EN RED (el paquete MP nivel 2).
    ///
    /// EL DISEÑO DEL USUARIO, tal cual:
    /// · "La voz y el hambre son del portador del grimorio hambriento en
    ///   ese momento" — cada portador siente SOLO la voz y el hambre de
    ///   SU propio grimorio.
    /// · "Solo el portador correcto debe ver los reclamos de su propio
    ///   grimorio" — los paquetes de voz viajan AL CLIENTE DEL PORTADOR
    ///   (ModPacket.Send(whoAmI): nadie más los recibe siquiera).
    /// · "El evento de uno es el evento del mundo… pero los demás solo
    ///   ven al Testigo tomar notas" — los ANUNCIOS del festín (oleadas,
    ///   jefes que llegan) van a TODOS por chat de vanilla
    ///   (ChatHelper.BroadcastChatMessage); las VOCES solo al portador.
    ///
    /// ARQUITECTURA (mínima y autoritativa):
    /// · La AUTORIDAD de la hambre y del reparto de variantes es el
    ///   servidor (en SP: el propio proceso). EcoRed decide por netMode:
    ///   — SP: habla directo en la pantalla del portador (cero red).
    ///   — Server: empaqueta LA CLAVE hjson ya resuelta (variante
    ///     elegida por la autoridad) + color + escala + flags y la manda
    ///   — Client: la lógica local del propio portador (el Libro Celoso,
    ///     las burbujas del Testigo) habla directo — nunca por red.
    /// · El paquete lleva la CLAVE, no el texto: el portador la resuelve
    ///   en SU idioma y la línea ocupa lo mismo con 2 o 20 clientes.
    /// · LA HAMBRE EN RED: el servidor cuenta los momentos (régimen
    ///   autoritativo — el cliente MP ya no cuenta los suyos propios) y
    ///   sincroniza el estado (momentos + ticks) al portador para la
    ///   BARRA PALIDECIDA, el aura de ceniza y los celos del libro.
    /// · EL LATIDO DE XP: el pulso de la barra dorada (ShardHUDSystem)
    ///   viaja al portador cuando su libro cobra.
    ///
    /// CONTRATO:
    /// - HablarAlPortador(portador, clave, tinte, rugido, escala,
    ///   prioridad): la voz del grimorio de ESE portador.
    /// - HablarVarianteAlPortador(..., claveBase, variantes): el reparto
    ///   sin repetición resuelto POR LA AUTORIDAD (EcoLib.ElegirClave).
    /// - SincronizarHambre(portador): el estado del hambre al portador.
    /// - LatidoDeXp(portador, xp): el pulso de la barra dorada.
    /// - AnunciarMundo(clave, color, args): chat de vanilla a TODOS (el
    ///   festín es del mundo).
    /// - AnunciarAlPortador(portador, clave, color, args): chat solo al
    ///   portador (sus logros privados: el derecho a las esencias).
    /// - EnviarPrepararOleadas(oleadas): v6.50.2 — la selección de la
    ///   Carnada (clic derecho) viaja del cliente a la AUTORIDAD (el
    ///   estático por-máquina jamás llegó al server).
    ///
    /// v6.50.2 — BANDWIDTH (auditoría: 3 paquetes por kill):
    /// - MsgLatidoXp LLEVA el estado del libro (slot/nivel/XP de la
    ///   primera copia): un paquete por kill, la barra sigue exacta.
    /// - MsgLibro (la foto completa) SOLO al subir de nivel, en
    ///   OnEnterWorld y en la red de seguridad 600t — nunca por kill.
    /// - MsgHambre SOLO cuando el hambre cambia o en la red de
    ///   seguridad — y además lleva el FESTÍN (fase/oleada/total) para
    ///   el diagnóstico de furia de los clientes (antes siempre
    ///   "inactivo": las fases vivían solo en el server).
    ///
    /// REGLAS DE LA CASA: cero alocaciones en render (esto es lógica de
    /// juego — corre una vez por evento, no por frame); nada de texto
    /// hardcodeado: TODO viaja por clave hjson.
    /// </summary>
    public static class EcoRed
    {
        // === LOS TIPOS DE MENSAJE (continúan la serie de ShardSyncSystem) ===
        public const byte MsgVoz = 3;      // la voz del libro → SU portador
        public const byte MsgHambre = 4;   // el estado del hambre → SU portador
        public const byte MsgLatidoXp = 5; // el pulso de la barra dorada → SU portador
        // v6.50 — LOS LIBROS CAMINAN (la deuda nº1 de la auditoría MP):
        // la XP la cuenta el SERVIDOR, pero el nivel manda en tooltips,
        // daño y HUD del CLIENTE — sin este paquete la progresión era
        // fantasma en MP (nivel 1 eterno en la pantalla del portador).
        public const byte MsgLibro = 6;     // niveles/XP de los libros visibles → SU portador
        public const byte MsgCronica = 7;   // crónica del Testigo + DerrotaOleada10 → SU portador
        public const byte MsgPedirLibros = 8; // cliente → server: "dame mis libros" (al entrar)
        public const byte MsgPedirFragmento = 9; // cliente → server: el Altar pide el Fragmento Génesis
        // v6.50.2 — FIX (la selección de la carnada NO viajaba en MP):
        // OleadasPreparadas era un ESTÁTICO por máquina — el remoto ciclaba
        // SU contador y el server (que corre el clic izquierdo por el uso
        // sincronizado) desataba la furia con SU propio 3. La selección
        // viaja ahora del cliente del portador a la AUTORIDAD y vive en
        // ShardPlayer.OleadasPreparadasRemoto (toda la sesión).
        public const byte MsgPrepararOleadas = 10; // cliente → server: la Carnada prepara N oleadas

        // ================================================================
        //  LA VOZ — al portador correcto y a NADIE más
        // ================================================================

        /// <summary>
        /// LA VOZ DEL GRIMORIO de este portador. La clave llega YA
        /// resuelta (con variante si la había — EcoLib.ElegirClave).
        /// En SP habla directo; en MP el servidor empaqueta
        /// (destinatario+clave+color+escala+flags) y SOLO el cliente del
        /// portador la encola en su EcoLib. El texto lo resuelve cada
        /// quien con SU hjson.
        /// </summary>
        public static void HablarAlPortador(Player portador, string clave, Color tinte,
            bool rugido = true, float escala = 0.62f, bool prioridad = false)
        {
            try
            {
                if (portador == null || !portador.active || string.IsNullOrEmpty(clave))
                    return;

                switch (Main.netMode)
                {
                    case NetmodeID.MultiplayerClient:
                        // Lógica local del propio portador (celos, burbujas):
                        // su grimorio, su pantalla — sin red.
                        if (portador.whoAmI == Main.myPlayer)
                            EcoLib.Hablar(Language.GetTextValue(clave), tinte, rugido, escala, prioridad);
                        return;

                    case NetmodeID.Server:
                        EnviarPaquete(portador, p =>
                        {
                            p.Write(MsgVoz);
                            p.Write((byte)portador.whoAmI);
                            p.Write(clave);
                            p.Write(tinte.R);
                            p.Write(tinte.G);
                            p.Write(tinte.B);
                            p.Write(escala);
                            p.Write((byte)((prioridad ? 1 : 0) | (rugido ? 2 : 0)));
                        });
                        return;

                    default: // SP: el portador ES el jugador local
                        if (portador.whoAmI == Main.myPlayer)
                            EcoLib.Hablar(Language.GetTextValue(clave), tinte, rugido, escala, prioridad);
                        return;
                }
            }
            catch { }
        }

        /// <summary>
        /// EL SUSURRO: el atajo del hambre (escala menuda, sin rugido) —
        /// la voz que solo el portador oye de su propio grimorio.
        /// </summary>
        public static void SusurrarAlPortador(Player portador, string clave, Color tinte,
            bool prioridad = false)
        {
            HablarAlPortador(portador, clave, tinte, rugido: false, escala: 0.52f, prioridad);
        }

        /// <summary>
        /// LA VOZ CON REPARTO: elige la variante POR LA AUTORIDAD
        /// (EcoLib.ElegirClave — la memoria anti-repetición vive en el
        /// servidor, así el reparto es el mismo para quien mire) y la
        /// manda ya resuelta. "Matar al Rey Gelatina veinte veces no
        /// puede escuchar siempre la misma línea" — ni en SP ni en MP.
        /// </summary>
        public static void HablarVarianteAlPortador(Player portador, string claveBase,
            int variantes, Color tinte, bool rugido = true, float escala = 0.62f,
            bool prioridad = false)
        {
            string clave = EcoLib.ElegirClave(claveBase, variantes);
            HablarAlPortador(portador, clave, tinte, rugido, escala, prioridad);
        }

        // ================================================================
        //  LA HAMBRE — autoridad del servidor, pantalla del portador
        // ================================================================

        /// <summary>
        /// EL ESTADO DEL HAMBRE hacia SU portador: la barra dorada
        /// palidece, la ceniza del aura y los celos del libro leen los
        /// momentos en su propio cliente. La la llama ShardPlayer cuando
        /// la autoridad cambia algo (momento nuevo, kill que alimenta,
        /// perdón del festín). No-op en SP y en clientes.
        /// </summary>
        public static void SincronizarHambre(Player portador)
        {
            try
            {
                if (Main.netMode != NetmodeID.Server) return; // SP no necesita red
                if (portador == null || !portador.active) return;
                var sp = portador.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;

                EnviarPaquete(portador, p =>
                {
                    p.Write(MsgHambre);
                    p.Write((byte)portador.whoAmI);
                    p.Write((byte)System.Math.Min(sp.MomentosHambre, 255));
                    p.Write(sp.TicksSinMatar);
                    // v6.50.2 — FIX (furia siempre "inactiva" en clientes
                    // MP): EL FESTÍN CAMINA con el hambre — fase, oleada y
                    // total (3 bytes, simétricos con el case de recepción).
                    // Lo mandan la red de seguridad 600t y cada cambio de
                    // fase del ciclo (GrimorioFuriaSistema).
                    p.Write((byte)GrimorioFuriaSistema.FaseServidor);
                    p.Write((byte)System.Math.Max(0, System.Math.Min(
                        GrimorioFuriaSistema.OleadaServidor, 255)));
                    p.Write((byte)System.Math.Max(0, System.Math.Min(
                        GrimorioFuriaSistema.TotalesServidor, 255)));
                });
            }
            catch { }
        }

        // ================================================================
        //  EL LATIDO — el pulso de la barra dorada del portador
        // ================================================================

        /// <summary>
        /// EL PULSO DE XP: la barra dorada del portador late cuando su
        /// libro cobra (en MP el cobro lo hace el servidor — el HUD vive
        /// en el cliente del portador). No-op en SP y en clientes.
        /// v6.50.2 — FIX (bandwidth): EL LATIDO LLEVA EL LIBRO — (slot,
        /// nivel, XP) de la primera copia visible viaja en el MISMO
        /// paquete para que el cliente aplique el estado sin la FOTO
        /// COMPLETA de MsgLibro (esa ya solo viaja al subir de nivel, en
        /// OnEnterWorld y en la red de seguridad 600t — nunca por kill).
        /// Simétrico con el case de recepción.
        /// </summary>
        public static void LatidoDeXp(Player portador, int xp, int slot = -1,
            int nivel = 0, int xpLibro = 0)
        {
            try
            {
                if (Main.netMode != NetmodeID.Server) return;
                if (portador == null || !portador.active || xp <= 0) return;

                EnviarPaquete(portador, p =>
                {
                    p.Write(MsgLatidoXp);
                    p.Write((byte)portador.whoAmI);
                    p.Write(xp);
                    // v6.50.2 — el estado del libro en el mismo paquete
                    // (slot 255 = "sin libro que aplicar": solo pulsa la
                    // barra — la aplicación la cubre la foto completa).
                    p.Write((byte)(slot >= 0 && slot <= 9 ? slot : 255));
                    p.Write(nivel);
                    p.Write(xpLibro);
                });
            }
            catch { }
        }

        // ================================================================
        //  v6.50 — LOS LIBROS CAMINAN (nivel/XP del portador en MP)
        // ================================================================

        /// <summary>
        /// EL ESTADO DE LOS LIBROS hacia SU portador: la XP la cobra el
        /// SERVIDOR (autoridad), pero el NIVEL manda en el cliente
        /// (ModifyWeaponDamage, tooltips, ShardHUD, NivelLibro). Este
        /// paquete lleva (slot, nivel, XP, banderas) de cada Grimorio
        /// visible del portador — el cliente lo aplica a SUS copias y
        /// celebra la subida (FX locales) solo con el DELTA.
        /// La llama la autoridad tras cada cobro (GlobalNPCXP), tras las
        /// esencias (server) y como red de seguridad periódica. No-op en
        /// SP y en clientes.
        /// </summary>
        public static void SincronizarLibros(Player portador)
        {
            try
            {
                if (Main.netMode != NetmodeID.Server) return;
                if (portador == null || !portador.active) return;

                EnviarPaquete(portador, p =>
                {
                    p.Write(MsgLibro);
                    p.Write((byte)portador.whoAmI);
                    byte libros = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        Item inv = portador.inventory[i];
                        if (inv != null && !inv.IsAir &&
                            inv.type == ModContent.ItemType<Content.Weapons.GrimoireEternal>())
                            libros++;
                    }
                    p.Write(libros);
                    for (int i = 0; i < 10; i++)
                    {
                        Item inv = portador.inventory[i];
                        if (inv == null || inv.IsAir ||
                            inv.type != ModContent.ItemType<Content.Weapons.GrimoireEternal>())
                            continue;
                        var sl = inv.GetGlobalItem<Globals.ShardLevelItem>();
                        p.Write((byte)i);
                        p.Write(sl != null ? sl.Level : 1);
                        p.Write(sl != null ? sl.XP : 0);
                        p.Write((byte)((sl != null && sl.PrimeraCincoEstrellas) ? 1 : 0));
                    }
                });
            }
            catch { }
        }

        /// <summary>
        /// v6.50 — LA CRÓNICA CAMINA: el Testigo lee la crónica y la puerta
        /// de la tienda de esencias (DerrotaOleada10) en la copia del
        /// JUGADOR LOCAL — pero quien las marca es el servidor. Este
        /// paquete lleva la crónica completa hacia SU portador. La llama
        /// GlobalNPCXP al marcar jefe devorado y GrimorioFuriaSistema al
        /// cerrar la oleada 10.
        /// v6.50.1 — RESONANCIA TAMBIÉN CAMINA: sin SSC el .plr lo escribe
        /// el CLIENTE — los shards que acreditaba el server (OnKill de los
        /// 5 jefes) morían con la sesión. Viajan aquí y el portador aplica
        /// con merge (nunca degradar).
        /// </summary>
        public static void SincronizarCronica(Player portador)
        {
            try
            {
                if (Main.netMode != NetmodeID.Server) return;
                if (portador == null || !portador.active) return;
                var sp = portador.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;

                EnviarPaquete(portador, p =>
                {
                    p.Write(MsgCronica);
                    p.Write((byte)portador.whoAmI);
                    p.Write((byte)(sp.DerrotaOleada10 ? 1 : 0));
                    p.Write((byte)System.Math.Min(sp.CronicaJefes.Count, 255));
                    for (int i = 0; i < sp.CronicaJefes.Count && i < 255; i++)
                        p.Write((ushort)sp.CronicaJefes[i]);
                    p.Write(sp.CronicaNarrada);
                    p.Write(sp.ResonanceShards);
                });
            }
            catch { }
        }

        // ================================================================
        //  LOS ANUNCIOS — el festín es del MUNDO (vanilla chat)
        // ================================================================

        /// <summary>
        /// EL ANUNCIO PÚBLICO: chat de vanilla a TODOS los jugadores
        /// (oleadas, jefes que llegan, el Juicio). En SP es Main.NewText
        /// local; en MP el servidor difunde con ChatHelper (la
        /// localización la resuelve cada cliente — NetworkText.FromKey
        /// viaja por CLAVE, no por texto).
        /// </summary>
        public static void AnunciarMundo(string clave, Color color, params object[] args)
        {
            try
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.Chat.ChatHelper.BroadcastChatMessage(
                        NetworkText.FromKey(clave, args), color);
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.NewText(Language.GetTextValue(clave, args), color);
                }
            }
            catch { }
        }

        /// <summary>
        /// EL ANUNCIO PRIVADO: chat de vanilla SOLO al portador (el
        /// derecho a las esencias, los avisos de su propio festín).
        /// </summary>
        public static void AnunciarAlPortador(Player portador, string clave, Color color,
            params object[] args)
        {
            try
            {
                if (portador == null || !portador.active) return;
                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.Chat.ChatHelper.SendChatMessageToClient(
                        NetworkText.FromKey(clave, args), color, portador.whoAmI);
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient &&
                         portador.whoAmI == Main.myPlayer)
                {
                    Main.NewText(Language.GetTextValue(clave, args), color);
                }
            }
            catch { }
        }

        // ================================================================
        //  LA RECEPCIÓN (solo clientes) — cada quien su propia voz
        // ================================================================

        /// <summary>
        /// Procesa un paquete de EcoRed recibido en un CLIENTE. La regla
        /// de oro: si el destinatario no soy YO, el paquete se descarta
        /// en silencio — "solo el portador correcto ve los reclamos de
        /// su propio grimorio" (el Send(whoAmI) ya lo limita de red;
        /// este filtro es la doble puerta).
        /// v6.50: también procesa el PEDIDO del cliente (MsgPedirLibros,
        /// recibido en el SERVER al entrar al mundo) y aplica los libros
        /// (MsgLibro) y la crónica (MsgCronica) en el cliente dueño.
        /// </summary>
        public static void Recibir(BinaryReader reader, int quienEnvia = -1)
        {
            try
            {
                byte tipo = reader.ReadByte();

                // EL PEDIDO DE ENTRADA: el cliente recién llegado pide sus
                // libros — el server contesta con la foto completa.
                // v6.50.2 — también el pedido de la CARNADA (la selección
                // del clic derecho viaja a la autoridad).
                if (Main.netMode == NetmodeID.Server &&
                    (tipo == MsgPedirLibros || tipo == MsgPedirFragmento ||
                     tipo == MsgPrepararOleadas))
                {
                    Player solicitante = quienEnvia >= 0 && quienEnvia < Main.player.Length
                        ? Main.player[quienEnvia] : null;
                    if (solicitante != null && solicitante.active)
                    {
                        if (tipo == MsgPedirLibros)
                        {
                            SincronizarLibros(solicitante);
                            SincronizarCronica(solicitante);
                            SincronizarHambre(solicitante);
                        }
                        else if (tipo == MsgPrepararOleadas)
                        {
                            // v6.50.2 — LA SELECCIÓN DE LA CARNADA CAMINA: el
                            // clic derecho cicla un estático LOCAL — este
                            // paquete lo deja en la réplica del server para
                            // que SU UseItem (el uso sincronizado) lea el
                            // valor que el REMOTO preparó. Vive en el
                            // ShardPlayer (sesión completa, sin persistir).
                            byte oleadas = reader.ReadByte();
                            var spc = solicitante.GetModPlayer<Players.ShardPlayer>();
                            if (spc != null && oleadas >= 1 && oleadas <= 11)
                                spc.OleadasPreparadasRemoto = oleadas;
                        }
                        else
                        {
                            // EL ALTAR HABLA CON EL SERVER: el RightClick de
                            // tile corre SOLO en el cliente — el Fragmento
                            // Génesis lo spawn-ea la AUTORIDAD (sin drops
                            // fantasma) y vanilla lo difunde al mundo.
                            // v6.50.1 — FIX: la AUTORIDAD revalida (el chequeo
                            // del cliente no basta: un cliente modificado — o un
                            // doble click antes de recoger el ítem del suelo —
                            // pediría fragmentos infinitos).
                            bool yaTiene = false;
                            for (int k = 0; k < 58 && !yaTiene; k++)
                            {
                                Item inv = solicitante.inventory[k];
                                if (inv != null && !inv.IsAir &&
                                    inv.type == ModContent.ItemType<Content.Items.GenesisShard>())
                                    yaTiene = true;
                            }
                            // v6.50.2 — FIX (anti-dupe del SUELO): el
                            // escenario que el propio comentario de arriba
                            // describe — el DOBLE CLICK antes de recoger el
                            // ítem — dejaba nacer el segundo: la
                            // revalidación solo miraba el inventario. Si YA
                            // vive un Fragmento Génesis en el suelo a menos
                            // de 400px del solicitante, no nace otro.
                            bool enSuelo = false;
                            for (int k = 0; k < Main.maxItems && !enSuelo; k++)
                            {
                                Item suelto = Main.item[k];
                                if (suelto == null || !suelto.active || suelto.IsAir) continue;
                                if (suelto.type != ModContent.ItemType<Content.Items.GenesisShard>()) continue;
                                if (Vector2.DistanceSquared(suelto.Center, solicitante.Center) < 400f * 400f)
                                    enSuelo = true;
                            }
                            if (!yaTiene && !enSuelo)
                            {
                                int idx = Item.NewItem(solicitante.GetSource_GiftOrReward(),
                                    solicitante.Center,
                                    ModContent.ItemType<Content.Items.GenesisShard>());
                                if (idx >= 0 && idx < Main.item.Length)
                                    Main.item[idx].noGrabDelay = 0;
                            }
                        }
                    }
                    return;
                }

                byte destinatario = reader.ReadByte();

                // LA DOBLE PUERTA: la voz de OTRO grimorio no me llega.
                // v6.50.1 — el HOST (listen server: netMode Server con
                // pantalla propia) también es destinatario legítimo: sus
                // paquetes viajan por la entrega local de EnviarPaquete.
                // En dedicado el jugador 0 es REMOTO: Main.dedServ lo
                // distingue.
                bool soyYo = Main.netMode == NetmodeID.MultiplayerClient ||
                             (Main.netMode == NetmodeID.Server && !Main.dedServ);
                if (!soyYo || destinatario != Main.myPlayer)
                    return;

                switch (tipo)
                {
                    case MsgVoz:
                    {
                        string clave = reader.ReadString();
                        byte r = reader.ReadByte(), g = reader.ReadByte(), b = reader.ReadByte();
                        float escala = reader.ReadSingle();
                        byte flags = reader.ReadByte();
                        EcoLib.Hablar(Language.GetTextValue(clave),
                            new Color(r, g, b), (flags & 2) != 0,
                            escala, (flags & 1) != 0);
                        break;
                    }

                    case MsgHambre:
                    {
                        byte momentos = reader.ReadByte();
                        int ticks = reader.ReadInt32();
                        // v6.50.2 — EL FESTÍN CAMINA (3 bytes simétricos con
                        // el writer de SincronizarHambre): llenan los
                        // estáticos públicos que GrimorioFuriaSistema.
                        // Diagnostico lee en clientes MP (antes la furia
                        // aparecía siempre "inactiva": las fases vivían
                        // solo en la máquina del server).
                        GrimorioFuriaSistema.FaseCliente = reader.ReadByte();
                        GrimorioFuriaSistema.OleadaCliente = reader.ReadByte();
                        GrimorioFuriaSistema.TotalesCliente = reader.ReadByte();
                        var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
                        if (sp != null)
                        {
                            sp.MomentosHambre = momentos;
                            sp.TicksSinMatar = ticks;
                        }
                        break;
                    }

                    case MsgLatidoXp:
                    {
                        int xp = reader.ReadInt32();
                        // v6.50.2 — EL LIBRO EN EL LATIDO (simétrico con el
                        // writer de LatidoDeXp): el cliente aplica el estado
                        // de la primera copia Y pulsa la barra — sin la foto
                        // completa (MsgLibro ya solo viaja al subir nivel /
                        // entrar / red de seguridad).
                        byte slot = reader.ReadByte();
                        int nivel = reader.ReadInt32();
                        int xpLibro = reader.ReadInt32();
                        if (xp > 0) ShardHUDSystem.MarcarGanancia(xp);
                        if (slot < 10) // 255 = sin libro: solo el pulso
                        {
                            var yo = Main.LocalPlayer;
                            if (yo != null)
                            {
                                Item inv = yo.inventory[slot];
                                if (inv != null && !inv.IsAir &&
                                    inv.type == ModContent.ItemType<Content.Weapons.GrimoireEternal>())
                                {
                                    var sl = inv.GetGlobalItem<Globals.ShardLevelItem>();
                                    if (sl != null)
                                    {
                                        // MERGE (el contrato de MsgLibro):
                                        // jamás degradar. El latido solo
                                        // llega cuando NO subió nivel (la
                                        // subida dispara la foto completa):
                                        // a nivel igual gana la XP más rica;
                                        // a nivel mayor se asigna en
                                        // silencio — la fiesta la celebra
                                        // MsgLibro con su delta (una sola).
                                        if (nivel > sl.Level)
                                        {
                                            sl.Level = System.Math.Max(1, nivel);
                                            sl.XP = System.Math.Max(0, xpLibro);
                                        }
                                        else if (nivel == sl.Level)
                                            sl.XP = System.Math.Max(sl.XP, System.Math.Max(0, xpLibro));
                                    }
                                }
                            }
                        }
                        break;
                    }

                    case MsgLibro:
                    {
                        // LOS LIBROS DEL PORTADOR: aplicar (slot, nivel, XP,
                        // bandera) a las copias LOCALES — la subida se
                        // celebra solo con el DELTA (el servidor ya contó
                        // la XP; el cliente ya no desincroniza).
                        byte libros = reader.ReadByte();
                        var yo = Main.LocalPlayer;
                        if (yo == null) break;
                        for (int k = 0; k < libros; k++)
                        {
                            byte slot = reader.ReadByte();
                            int nivel = reader.ReadInt32();
                            int xp = reader.ReadInt32();
                            bool primera5 = reader.ReadByte() != 0;
                            if (slot >= 10) continue;
                            Item inv = yo.inventory[slot];
                            if (inv == null || inv.IsAir ||
                                inv.type != ModContent.ItemType<Content.Weapons.GrimoireEternal>())
                                continue;
                            var sl = inv.GetGlobalItem<Globals.ShardLevelItem>();
                            if (sl == null) continue;
                            // v6.50.1 — FIX (integridad de progresión): aplicar
                            // con MERGE, jamás degradar. Sin SSC la copia del
                            // server puede llegar “fresca” (nivel 1) antes de
                            // que ItemIO traiga los datos del libro — el
                            // overwrite incondicional borraba progresión real
                            // del .plr en cada reconexión. El NetSend/NetReceive
                            // de ShardLevelItem alimenta al server con los
                            // datos reales; este guard cubre la ventana previa
                            // y cualquier deriva.
                            if (nivel < sl.Level)
                            {
                                sl.PrimeraCincoEstrellas |= primera5;
                                continue;
                            }
                            int delta = nivel - sl.Level;
                            // v6.50.2 — FIX (XP degradada a nivel igual): el
                            // overwrite incondicional pisaba la XP local (más
                            // rica) cuando el nivel coincidía — la misma
                            // ventana pre-ItemIO que el guard del nivel cubre.
                            // A nivel igual: máximo. A nivel mayor: la del
                            // server (es la autoridad que acaba de contar).
                            bool mismoNivel = nivel == sl.Level;
                            sl.Level = System.Math.Max(1, nivel);
                            sl.XP = mismoNivel
                                ? System.Math.Max(sl.XP, System.Math.Max(0, xp))
                                : System.Math.Max(0, xp);
                            sl.PrimeraCincoEstrellas |= primera5;
                            if (delta > 0)
                                sl.CelebrarSubida(inv, delta);
                        }
                        break;
                    }

                    case MsgCronica:
                    {
                        byte derrota10 = reader.ReadByte();
                        byte total = reader.ReadByte();
                        var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
                        if (sp == null) break;
                        // v6.50.1 — FIX: MERGE, nunca wipe. La copia del
                        // server nace vacía cada sesión (sin SSC el .plr
                        // vive en el cliente): Clear() + overwrite borraba
                        // la crónica real y el derecho a las esencias de
                        // la copia persistente. Con el NetSend/NetReceive
                        // de ShardLevelItem + este merge, la crónica que
                        // manda es la MÁS RICA de las dos.
                        sp.DerrotaOleada10 |= derrota10 != 0;
                        if (total > 0)
                        {
                            // v6.50.2 — FIX (wipe disfrazado de merge): el
                            // Clear() destraba la unión — el Contains del
                            // bucle ya la hace (add si falta). Con Clear la
                            // crónica histórica del .plr se reemplazaba por
                            // la copia fresca del server y el Testigo callaba
                            // para siempre; SIN Clear la lista resultante es
                            // la MÁS RICA de las dos (la unión real).
                            for (int k = 0; k < total; k++)
                            {
                                int tipoJefe = reader.ReadUInt16();
                                if (!sp.CronicaJefes.Contains(tipoJefe))
                                    sp.CronicaJefes.Add(tipoJefe);
                            }
                        }
                        // total == 0: el cauce sigue alineado (0 ushorts) y
                        // la crónica local se conserva — nada que limpiar.
                        sp.CronicaNarrada = System.Math.Max(sp.CronicaNarrada, reader.ReadInt32());
                        // v6.50.1 — LA RESONANCIA CAMINA: acreditada por el
                        // server (OnKill), persistida por el cliente —
                        // merge máximo para que ningún lado la pierda.
                        sp.ResonanceShards = System.Math.Max(sp.ResonanceShards, reader.ReadInt32());
                        break;
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  EL PEDIDO DE ENTRADA — el cliente pide sus libros al conectar
        // ================================================================

        /// <summary>
        /// v6.50 — LA FOTO DE ENTRADA: el cliente que entra al mundo pide
        /// el estado de SUS libros (nivel/XP/crónica/hambre) — vanilla
        /// sincroniza el inventario, pero NO los datos de GlobalItem. Se
        /// llama desde ShardPlayer.OnEnterWorld (solo cliente MP).
        /// </summary>
        public static void PedirMisLibros()
        {
            try
            {
                if (Main.netMode != NetmodeID.MultiplayerClient) return;
                ModPacket p = AethonMod.Instance.GetPacket();
                p.Write(MsgPedirLibros);
                p.Send();
            }
            catch { }
        }

        /// <summary>
        /// v6.50 — EL ALTAR PIDE EL FRAGMENTO: el RightClick del tile corre
        /// SOLO en el cliente; en MP el Fragmento Génesis debe nacer del
        /// server (el drop client-side era fantasma — la auditoría nº5).
        /// Llamado desde AncientAltar.RightClick (cliente MP).
        /// </summary>
        public static void PedirFragmentoGenesis()
        {
            try
            {
                if (Main.netMode != NetmodeID.MultiplayerClient) return;
                ModPacket p = AethonMod.Instance.GetPacket();
                p.Write(MsgPedirFragmento);
                p.Send();
            }
            catch { }
        }

        /// <summary>
        /// v6.50.2 — LA CARNADA PREPARA: el clic derecho del ítem cicla un
        /// contador LOCAL (la UI del clic solo corre en el cliente que
        /// clica) — este paquete lleva la selección NUEVA a la AUTORIDAD
        /// para que el clic izquierdo (el uso SINCRONIZADO que corre el
        /// server) lea el número que el portador preparó y no el estático
        /// por defecto de la máquina del server. Llamado desde
        /// CarnadaDelGrimorio.UseItem (cliente MP).
        /// </summary>
        public static void EnviarPrepararOleadas(int oleadas)
        {
            try
            {
                if (Main.netMode != NetmodeID.MultiplayerClient) return;
                ModPacket p = AethonMod.Instance.GetPacket();
                p.Write(MsgPrepararOleadas);
                p.Write((byte)System.Math.Max(1, System.Math.Min(oleadas, 11)));
                p.Send();
            }
            catch { }
        }

        // ================================================================
        //  EL TRANSPORTE
        // ================================================================

        /// <summary>
        /// Empaqueta y envía SOLO al cliente del portador (ModPacket con
        /// delegado para escribir el cuerpo sin repetir el boilerplate).
        /// Lógica de servidor: una llamada por evento, no por frame.
        /// v6.50.1 — FIX (EL HOST SORDO): en un listen server (Host & Play)
        /// el host es el jugador 0 y NO tiene socket de vuelta —
        /// ModPacket.Send(0) no le llega a nadie. La entrega LOCAL
        /// atraviesa el MISMO cauce (el cuerpo se serializa a memoria y
        /// Recibir lo procesa como si viniera de red): la doble puerta ya
        /// admite al host como destinatario. En dedicado el jugador 0 es
        /// remoto y Main.dedServ mantiene el camino de red.
        /// </summary>
        private static void EnviarPaquete(Player portador, System.Action<BinaryWriter> escribir)
        {
            if (Main.netMode == NetmodeID.Server && !Main.dedServ &&
                portador.whoAmI == Main.myPlayer)
            {
                // EL HOST COMO DESTINATARIO: entrega local, mismo formato.
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
                        escribir(w);
                    ms.Position = 0;
                    using (var r = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
                        Recibir(r, -1);
                }
                return;
            }
            ModPacket p = AethonMod.Instance.GetPacket();
            escribir(p);
            p.Send(portador.whoAmI);
        }
    }
}
