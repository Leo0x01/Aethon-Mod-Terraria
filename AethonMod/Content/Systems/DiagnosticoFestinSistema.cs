using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  DiagnosticoFestinSistema — v6.184 — EL BANCO DE PRUEBAS DEL FESTÍN
    //  DEL DEVORAMUNDOS (la letra: «EL FESTÍN DEL DEVORAMUNDO no inicia,
    //  para solucionar esto ve a como estaba 30 o 50 commit atras o cuando
    //  e investiga proque no inicia ahora»).
    //
    //  CUATRO versiones de trigger (v6.180 aparición-cercana · v6.182 marca
    //  de la llamada · v6.183 aparición ≤2600 px) y el usuario sigue sin
    //  ver NADA: esta vez el festín se REPRODUCE EN EL SERVIDOR de la casa
    //  CON EL MUNDO TICKEANDO DE VERDAD.
    //
    //  EL DESCUBRIMIENTO DEL BANCO (decompile Main.cs:6038): el servidor
    //  dedicado VACÍO jamás llama Main.Update — «if (Netplay.HasClients)»
    //  — el mundo no tickea sin clientes. LA CURA DEL BANCO: Main.
    //  OnTickForThirdPartySoftwareOnly (el latido del bucle idle, corre
    //  SIEMPRE) planta un SOCKET FALSO en Netplay.Clients[0] → HasClients
    //  → Main.Update → el mundo vive → la escena corre con el camino
    //  EXACTO del juego (NPC.SpawnBoss/OnSpawn de verdad · PreAI de
    //  verdad · jugador con el Eterno en el inventario).
    //
    //  SOLO con AETHON_FESTIN_PROBE=1 (en partida normal y en el gate
    //  headless diario es un no-op total). El veredicto va al log con el
    //  prefijo [DiagnosticoFestin].
    // ======================================================================
    public class DiagnosticoFestinSistema : ModSystem
    {
        private static int _fase;             // 0=escena · 1=invoca A · 2=cine A · 3=lejos B · 4=cine B · 5=fin
        private static int _ticks;
        private static int _piezasA;
        private static NPC _colaA;
        private static bool _mesaServida;
        private static bool _fauceNacido;
        private static string _veredicto;

        private static void Decir(string linea)
        {
            // v6.180 — R19/R20: NINGÚN catch mudo (el loop lo caza)
            try { Terraria.ModLoader.Logging.PublicLogger.Error("[DiagnosticoFestin] " + linea); }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DiagnosticoFestin", exRegistro); }
        }

        private static bool SondaActiva =>
            Main.dedServ &&
            Environment.GetEnvironmentVariable("AETHON_FESTIN_PROBE") == "1";

        public override void OnWorldLoad()
        {
            if (!SondaActiva) return;
            try
            {
                // EL LATIDO DEL IDLE: corre en cada vuelta del bucle del
                // servidor AUNQUE nadie esté conectado
                Main.OnTickForThirdPartySoftwareOnly -= TickIdle;
                Main.OnTickForThirdPartySoftwareOnly += TickIdle;
                _fase = 0; _ticks = 0;
                Decir("SONDA ACTIVA — el banco del festín vive");
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoFestin", exRegistro); }
        }

        /// <summary>EL LATIDO: planta el socket falso UNA vez — el mundo
        /// empieza a tickear y el resto vive en PreUpdateWorld.</summary>
        private static void TickIdle()
        {
            try
            {
                if (Main.netMode != 2 || _fase == 5) return;
                if (Netplay.Clients[0].Socket != null) return;   // ya vive
                Netplay.Clients[0].Socket = new SocketFalso();
                Netplay.Clients[0].IsActive = true;
                Decir("SOCKET FALSO plantado — el mundo del banco tickea de verdad");
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoFestin", exRegistro); }
        }

        public override void PreUpdateWorld()
        {
            if (!SondaActiva || _fase >= 5) return;
            try
            {
                _ticks++;
                Player yo = Main.player[0];
                // EL PORTADOR ES INMORTAL (el banco prueba EL FESTÍN, no la
                // pelea: si el gusano lo matara, vanilla despawnearía al jefe)
                if (_fase > 0 && yo != null && yo.active)
                {
                    if (yo.dead) { yo.dead = false; yo.respawnTimer = 0; }
                    if (yo.statLife < 400) yo.statLife = 400;
                    if (yo.statLifeMax < 400) yo.statLifeMax = 400;
                }
                switch (_fase)
                {
                    case 0:
                        if (_ticks < 90) return;   // que el mundo se asiente
                        // === LA ESCENA: un portador de verdad con el Eterno ===
                        yo.whoAmI = 0;
                        yo.active = true;
                        yo.dead = false;
                        yo.position = new Vector2(Main.spawnTileX * 16f, (Main.spawnTileY - 6) * 16f);
                        for (int k = 0; k < 59; k++) yo.inventory[k] = new Item();
                        yo.inventory[0].SetDefaults(ModContent.ItemType
                            <global::AethonMod.Content.Weapons.GrimoireEternal>());
                        var sp = yo.GetModPlayer<Players.ShardPlayer>();
                        sp.DevoramundosPedacito = false;       // la página en blanco del banco
                        sp.DevoramundosPedacitoMigrado = true; // sin migraciones: lo que se ve es lo que hay
                        Decir("ESCENA OK: portador en (" + (int)yo.Center.X + "," + (int)yo.Center.Y +
                            ") con el Eterno — pedacito=" + sp.DevoramundosPedacito);
                        _fase = 1;
                        return;

                    case 1:
                    {
                        // === FASE A: la invocación CERCA (el camino del ítem
                        //     dentro del interior, InteriorInvocaGlobalItem) ===
                        Vector2 pos = yo.Center + new Vector2(420f, -240f);
                        NPC.SpawnBoss((int)pos.X, (int)pos.Y, NPCID.EaterofWorldsHead, 0);   // void en este tML
                        NPC cabeza = CabezaMasCercana(yo.Center, 1200f);
                        if (cabeza == null)
                        { Decir("A-FALLO: la cabeza no nació"); _fase = 5; return; }
                        _piezasA = ContarPiezas();
                        var spA = yo.GetModPlayer<Players.ShardPlayer>();
                        bool cerrojo = spA.DevoramundosPedacito;
                        int espera = cabeza.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var gA) ? gA.PedacitoEspera : -1;
                        Decir("A-INVOCACIÓN: cabeza whoAmI=" + cabeza.whoAmI + " a " +
                            (int)Vector2.Distance(yo.Center, cabeza.Center) + " px · piezas=" + _piezasA +
                            " · cerrojoQuemado=" + cerrojo + " · PedacitoEspera=" + espera);
                        _mesaServida = espera > 0;
                        _fauceNacido = false;
                        _ticks = 0;
                        if (!_mesaServida)
                        {
                            _veredicto = "A-FALLA EN EL ONSPAWN: la cabeza nació a " +
                                (int)Vector2.Distance(yo.Center, cabeza.Center) +
                                " px de un portador con el Eterno y la mesa NO se sirvió (espera=" + espera + ")";
                            Decir(_veredicto);
                            _fase = 5;      // sin mesa no hay cine que probar
                            return;
                        }
                        _fase = 2;
                        return;
                    }

                    case 2:
                    {
                        // === EL CINE A: 150 ticks de espera → el pedacito se
                        //     sirve sobre LA COLA → 210 ticks más → la cola
                        //     muere de verdad ===
                        if (_ticks == 160 || _ticks == 200)
                        {
                            _colaA = BuscarPedacitoVivo();
                            bool fauce = HayFauce();
                            if (!_fauceNacido && fauce) _fauceNacido = true;
                            Decir("A-T+" + _ticks + ": pedacitoVivo=" + (_colaA != null) +
                                " · fauce=" + fauce + " · piezas=" + ContarPiezas());
                        }
                        if (_ticks >= 460)
                        {
                            bool colaMurio = _colaA == null || !_colaA.active;
                            bool exito = _mesaServida && _fauceNacido && colaMurio;
                            _veredicto = exito
                                ? "A-FESTIN COMPLETO ✔ (mesa servida · fauce nació · la sección murió de verdad)"
                                : "A-FESTIN FALLIDO ✘ (mesa=" + _mesaServida + " · fauce=" + _fauceNacido +
                                  " · colaMurio=" + colaMurio + " · piezas " + _piezasA + "→" + ContarPiezas() + ")";
                            Decir(_veredicto);
                            _fase = 3;
                            _ticks = 0;
                        }
                        return;
                    }

                    case 3:
                    {
                        // === FASE B: la invocación LEJOS (3.000 px > 2.600:
                        //     la mesa NO se sirve — el borde del festín) ===
                        if (_ticks < 30) return;
                        Vector2 lejos = yo.Center + new Vector2(3000f, -300f);
                        lejos.X = Math.Min(lejos.X, (Main.maxTilesX - 10) * 16f);
                        lejos.Y = Math.Max(lejos.Y, 200f);
                        NPC.SpawnBoss((int)lejos.X, (int)lejos.Y, NPCID.EaterofWorldsHead, 0);   // void en este tML
                        NPC cabezaB = CabezaMasCercana(lejos, 1200f);
                        int esperaB = -1;
                        if (cabezaB != null && cabezaB.active &&
                            cabezaB.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var gB))
                            esperaB = gB.PedacitoEspera;
                        Decir("B-INVOCACIÓN a " + (cabezaB != null ? (int)Vector2.Distance(yo.Center, cabezaB.Center) : -1) +
                            " px — PedacitoEspera=" + esperaB);
                        if (esperaB > 0)
                            Decir("B-VEREDICTO: EL LEJOS SE SIRVIÓ ✘ (una cabeza lejana quemó el cerrojo)");
                        else
                            Decir("B-VEREDICTO: EL LEJOS NO CENA ✔ (la mesa solo se sirve a ≤2600 px)");
                        Decir("FIN DEL BANCO — " + (_veredicto ?? "sin veredicto A"));
                        _fase = 5;
                        return;
                    }
                }
            }
            catch (System.Exception exRegistro)
            {
                RegistroSistema.Fallo("DiagnosticoFestin", exRegistro);
                Decir("FALLO DEL BANCO: " + exRegistro.GetType().Name + " — " + exRegistro.Message);
                _fase = 5;
            }
        }

        /// <summary>La cabeza del EoW viva más cercana a un punto.</summary>
        private static NPC CabezaMasCercana(Vector2 centro, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != NPCID.EaterofWorldsHead) continue;
                float d = n.Center.DistanceSQ(centro);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>Las piezas vivas del EoW.</summary>
        private static int ContarPiezas()
        {
            int n = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC == null || !nPC.active) continue;
                if (nPC.type == NPCID.EaterofWorldsHead || nPC.type == NPCID.EaterofWorldsBody ||
                    nPC.type == NPCID.EaterofWorldsTail) n++;
            }
            return n;
        }

        /// <summary>La pieza viva con el pedacito en marcha.</summary>
        private static NPC BuscarPedacitoVivo()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type != NPCID.EaterofWorldsBody && n.type != NPCID.EaterofWorldsTail &&
                    n.type != NPCID.EaterofWorldsHead) continue;
                if (n.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var g) && g.PedacitoVivo) return n;
            }
            return null;
        }

        /// <summary>¿Vive algún FauceDevoradorProjectile (la bruma del festín)?</summary>
        private static bool HayFauce()
        {
            int tipo = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Sombras.FauceDevoradorProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo) return true;
            }
            return false;
        }
    }

    /// <summary>EL SOCKET FALSO DEL BANCO: un ISocket conectado para
    /// siempre que no dice nada — el servidor cree que hay un cliente y
    /// Main.Update tickea el mundo (decompile Main.cs:6038: el bucle idle
    /// solo actualiza con Netplay.HasClients).</summary>
    public class SocketFalso : ISocket
    {
        public void Close() { }
        public bool IsConnected() => true;
        public void Connect(RemoteAddress address) { }
        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null) { }
        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null) { }
        public bool IsDataAvailable() => false;
        public void SendQueuedPackets() { }
        public bool StartListening(SocketConnectionAccepted callback) => true;
        public void StopListening() { }
        public RemoteAddress GetRemoteAddress() => null;
    }
}
