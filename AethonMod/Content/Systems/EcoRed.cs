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
        /// </summary>
        public static void LatidoDeXp(Player portador, int xp)
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
        /// </summary>
        public static void Recibir(BinaryReader reader)
        {
            try
            {
                byte tipo = reader.ReadByte();
                byte destinatario = reader.ReadByte();

                // LA DOBLE PUERTA: la voz de OTRO grimorio no me llega.
                if (Main.netMode != NetmodeID.MultiplayerClient || destinatario != Main.myPlayer)
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
                        if (xp > 0) ShardHUDSystem.MarcarGanancia(xp);
                        break;
                    }
                }
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
        /// </summary>
        private static void EnviarPaquete(Player portador, System.Action<ModPacket> escribir)
        {
            ModPacket p = AethonMod.Instance.GetPacket();
            escribir(p);
            p.Send(portador.whoAmI);
        }
    }
}
