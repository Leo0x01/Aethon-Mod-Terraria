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
    //  DiagnosticoFestinSistema — v6.185 — EL BANCO DE PRUEBAS DEL FESTÍN
    //  DEL DEVORAMUNDO (v6.184: el banco nació para el pedacito; el
    //  pedacito murió con la letra «lo que sea que hayas hecho con el
    //  festín del Devoramundo bórralo» y el banco se ADAPTA al camino
    //  nuevo: EL DEVORAMUNDO CENA COMO LOS DEMÁS, al <99 % del primer
    //  golpe).
    //
    //  EL BANCO (decompile Main.cs:6038): el servidor dedicado VACÍO jamás
    //  llama Main.Update — «if (Netplay.HasClients)» — el mundo no tickea
    //  sin clientes. LA CURA DEL BANCO: Main.OnTickForThirdPartySoftwareOnly
    //  (el latido del bucle idle, corre SIEMPRE) planta un SOCKET FALSO en
    //  Netplay.Clients[0] → HasClients → Main.Update → el mundo vive → la
    //  escena corre con el camino EXACTO del juego (NPC.SpawnBoss/OnSpawn de
    //  verdad · PreAI de verdad · jugador con el Eterno en la barra).
    //
    //  LAS TRES PRUEBAS:
    //   A — EL DEVORAMUNDO CENA: la cabeza nace cerca del portador, la
    //       cadena se forma, el portador golpea UN segmento (<99 %) → la
    //       ÚLTIMA sección entra en LA PRIMERA CENA (estado 3), el cine
    //       nace y a los 430 t la sección MUERE DE VERDAD y el jefe queda
    //       registrado (JefesDevorados = EaterofWorldsHead).
    //   B — EL LEJOS SIN GOLPE NO CENA: un Devoramundo a 3000 px, sin
    //       daño y sin interacción → NINGUNA cena (el gatillo es el golpe,
    //       no la cercanía).
    //   C — EL DESTRUCTOR SIGUE FUERA: el gusano mecánico golpeado al 98 %
    //       NO cena (la exclusión de la v6.175 vive solo para él).
    //
    //  SOLO con AETHON_FESTIN_PROBE=1 (en partida normal y en el gate
    //  headless diario es un no-op total). El veredicto va al log con el
    //  prefijo [DiagnosticoFestin].
    // ======================================================================
    public class DiagnosticoFestinSistema : ModSystem
    {
        private static int _fase;             // 0=escena · 1=invoca A · 2=golpe A · 3=cine A · 4=lejos B · 5=destructor C · 9=fin
        private static int _ticks;
        private static int _piezasA;
        private static NPC _presaA;
        private static bool _cenaNacio;
        private static bool _cineNacio;
        private static string _veredictoA;
        private static NPC _munecoE1, _munecoE2;
        private static Projectile _estrellaE1, _estrellaE2;
        private static string _veredictoE1, _veredictoE2;

        private static void Decir(string linea)
        {
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
                Main.OnTickForThirdPartySoftwareOnly -= TickIdle;
                Main.OnTickForThirdPartySoftwareOnly += TickIdle;
                _fase = 0; _ticks = 0;
                _presaA = null; _cenaNacio = false; _cineNacio = false; _veredictoA = null;
                Decir("SONDA ACTIVA v6.185 — el banco del festín vive (la cena del Devoramundo al <99 %)");
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoFestin", exRegistro); }
        }

        /// <summary>EL LATIDO: planta el socket falso UNA vez — el mundo
        /// empieza a tickear y el resto vive en PreUpdateWorld.</summary>
        private static void TickIdle()
        {
            try
            {
                if (Main.netMode != 2 || _fase == 9) return;
                if (Netplay.Clients[0].Socket != null) return;   // ya vive
                Netplay.Clients[0].Socket = new SocketFalso();
                Netplay.Clients[0].IsActive = true;
                Decir("SOCKET FALSO plantado — el mundo del banco tickea de verdad");
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("DiagnosticoFestin", exRegistro); }
        }

        public override void PreUpdateWorld()
        {
            if (!SondaActiva || _fase == 9) return;
            try
            {
                _ticks++;
                Player yo = Main.player[0];
                // EL PORTADOR ES INMORTAL (el banco prueba EL FESTÍN, no la pelea)
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
                        // === LA ESCENA: un portador de verdad con el Eterno EN LA BARRA ===
                        yo.whoAmI = 0;
                        yo.active = true;
                        yo.dead = false;
                        yo.position = new Vector2(Main.spawnTileX * 16f, (Main.spawnTileY - 6) * 16f);
                        for (int k = 0; k < 59; k++) yo.inventory[k] = new Item();
                        yo.inventory[0].SetDefaults(ModContent.ItemType
                            <global::AethonMod.Content.Weapons.GrimoireEternal>());
                        var sp = yo.GetModPlayer<Players.ShardPlayer>();
                        sp.JefesDevorados.Clear();          // la primera cena es del que nunca lo devoró
                        Decir("ESCENA OK: portador en (" + (int)yo.Center.X + "," + (int)yo.Center.Y +
                            ") con el Eterno en la barra — devorados=" + sp.JefesDevorados.Count);
                        _fase = 1;
                        _ticks = 0;
                        return;

                    case 1:
                    {
                        // === FASE A1: la invocación CERCA — la cadena nace ===
                        if (_ticks < 30) return;
                        Vector2 pos = yo.Center + new Vector2(420f, -240f);
                        NPC.SpawnBoss((int)pos.X, (int)pos.Y, NPCID.EaterofWorldsHead, 0);   // void en este tML
                        NPC cabeza = CabezaMasCercana(yo.Center, 1200f);
                        if (cabeza == null)
                        { Decir("A-FALLO: la cabeza no nació"); _fase = 9; return; }
                        Decir("A-INVOCACIÓN: cabeza whoAmI=" + cabeza.whoAmI + " a " +
                            (int)Vector2.Distance(yo.Center, cabeza.Center) + " px · piezas=" + ContarPiezas());
                        _fase = 2;
                        _ticks = 0;
                        return;
                    }

                    case 2:
                    {
                        // === FASE A2: la cadena se forma (90 t) y EL PRIMER GOLPE ===
                        if (_ticks < 90) return;
                        _piezasA = ContarPiezas();
                        NPC golpeada = CabezaMasCercana(yo.Center, 2600f);
                        if (golpeada == null)
                        { Decir("A-FALLO: no hay cabeza viva al golpear"); _fase = 9; return; }
                        // EL PORTADOR LA PELEA (la interacción de vanilla) y la
                        // deja por debajo del 99 % — el gatillo de la letra.
                        golpeada.playerInteraction[0] = true;
                        golpeada.life = (int)(golpeada.lifeMax * 0.98f);
                        golpeada.netUpdate = true;
                        // LA PRESA ESPERADA: la ÚLTIMA sección viva de la cadena
                        _presaA = UltimaSeccion(golpeada);
                        Decir("A-GOLPE: cabeza whoAmI=" + golpeada.whoAmI + " vida " + golpeada.life + "/" +
                            golpeada.lifeMax + " (" + (100f * golpeada.life / golpeada.lifeMax).ToString("F1") +
                            "%) · piezas=" + _piezasA + " · la presa esperada es la última sección whoAmI=" +
                            (_presaA != null ? _presaA.whoAmI : -1));
                        _fase = 3;
                        _ticks = 0;
                        return;
                    }

                    case 3:
                    {
                        // === FASE A3: EL CINE — 430 t de Primera Cena + margen ===
                        if (_ticks == 5 || _ticks == 60 || _ticks == 200 || _ticks == 470)
                        {
                            NPC enCena = PresaEnCena();
                            bool cine = HayCine();
                            if (!_cenaNacio && enCena != null) _cenaNacio = true;
                            if (!_cineNacio && cine) _cineNacio = true;
                            Decir("A-T+" + _ticks + ": enCena=" + (enCena != null ? enCena.whoAmI.ToString() : "—") +
                                " · cine=" + cine + " · piezas=" + ContarPiezas() +
                                (_presaA != null ? " · presa viva=" + _presaA.active + " vida=" + _presaA.life : ""));
                        }
                        if (_ticks >= 470)
                        {
                            bool presaMurio = _presaA == null || !_presaA.active || _presaA.life <= 0;
                            var spA = yo.GetModPlayer<Players.ShardPlayer>();
                            bool registrado = spA.JefesDevorados.Contains(NPCID.EaterofWorldsHead);
                            bool exito = _cenaNacio && presaMurio && registrado;
                            _veredictoA = exito
                                ? "A-CENA DEL DEVORAMUNDO ✔ (la sección entró en la cena · murió de verdad · el jefe quedó registrado" + (_cineNacio ? " · el cine nació" : "") + ")"
                                : "A-CENA FALLIDA ✘ (cena=" + _cenaNacio + " · presaMurio=" + presaMurio +
                                  " · registrado=" + registrado + " · piezas " + _piezasA + "→" + ContarPiezas() + ")";
                            Decir(_veredictoA);
                            // LIMPIEZA: el resto del gusano muere para la prueba B
                            MatarDevoramundos();
                            _fase = 4;
                            _ticks = 0;
                        }
                        return;
                    }

                    case 4:
                    {
                        // === FASE B: la invocación LEJOS, SIN GOLPE → NO CENA ===
                        if (_ticks < 30) return;
                        Vector2 lejos = yo.Center + new Vector2(3000f, -300f);
                        lejos.X = Math.Min(lejos.X, (Main.maxTilesX - 10) * 16f);
                        lejos.Y = Math.Max(lejos.Y, 200f);
                        NPC.SpawnBoss((int)lejos.X, (int)lejos.Y, NPCID.EaterofWorldsHead, 0);
                        NPC cabezaB = CabezaMasCercana(lejos, 1200f);
                        Decir("B-INVOCACIÓN a " + (cabezaB != null ? (int)Vector2.Distance(yo.Center, cabezaB.Center) : -1) +
                            " px — SIN golpe, SIN interacción");
                        _fase = 5;
                        _ticks = 0;
                        return;
                    }

                    case 5:
                    {
                        if (_ticks < 150) return;   // la cadena nace y el PreAI corre >100 veces
                        NPC enCenaB = PresaEnCena();
                        Decir(enCenaB == null
                            ? "B-VEREDICTO: EL LEJOS SIN GOLPE NO CENA ✔ (el gatillo es el <99 %, no la cercanía)"
                            : "B-VEREDICTO: EL LEJOS SIN GOLPE CENÓ ✘ (whoAmI=" + enCenaB.whoAmI + ")");
                        MatarDevoramundos();
                        _fase = 6;
                        _ticks = 0;
                        return;
                    }

                    case 6:
                    {
                        // === FASE C: EL DESTRUCTOR golpeado al 98 % NO CENA ===
                        if (_ticks < 30) return;
                        Vector2 pos = yo.Center + new Vector2(420f, -240f);
                        NPC.SpawnBoss((int)pos.X, (int)pos.Y, NPCID.TheDestroyer, 0);
                        NPC destructor = CabezaMasCercana(yo.Center, 1200f, NPCID.TheDestroyer);
                        if (destructor == null)
                        { Decir("C-FALLO: el Destructor no nació"); _fase = 9; return; }
                        destructor.playerInteraction[0] = true;
                        destructor.life = (int)(destructor.lifeMax * 0.98f);
                        destructor.netUpdate = true;
                        Decir("C-INVOCACIÓN: Destructor whoAmI=" + destructor.whoAmI + " golpeado al 98 % · piezas=" +
                            ContarPiezas(NPCID.TheDestroyerBody, NPCID.TheDestroyerTail));
                        _fase = 7;
                        _ticks = 0;
                        return;
                    }

                    case 7:
                    {
                        if (_ticks < 150) return;
                        NPC enCenaC = PresaEnCena(NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail);
                        Decir(enCenaC == null
                            ? "C-VEREDICTO: EL DESTRUCTOR SIGUE FUERA ✔ (golpeado al 98 % y no cena — la exclusión vive solo para él)"
                            : "C-VEREDICTO: EL DESTRUCTOR CENÓ ✘ (whoAmI=" + enCenaC.whoAmI + ")");
                        Decir("FIN DEL FESTÍN — " + (_veredictoA ?? "sin veredicto A"));
                        // v6.185 — LA ESTRELLA DEL GRIMORIO (T6): el banco sigue
                        //     con la IA del ataque básico por despertar.
                        _fase = 10;
                        _ticks = 0;
                        return;
                    }

                    // ============================================================
                    //  v6.185 — E1: LA ESTRELLA AL 120 CONTRA UN MUÑECO
                    // ============================================================
                    case 10:
                    {
                        if (_ticks < 60) return;   // que el Destructor muerto se limpie
                        MatarTodoCosmico();
                        // EL DUEÑO DE PRUEBAS: en el servidor dedicado Main.myPlayer es
                        // el slot 255 — NewProjectile solo escribe los ai[] si
                        // Owner == Main.myPlayer (decompile) y GolpeMotor solo
                        // resuelve si player[owner] está activo. El banco activa al
                        // 255 (fuera del rango de red 0..254: jamás viaja) en el
                        // sitio del portador — así la estrella nace con SU nivel y
                        // el motor de daño puede golpear al muñeco.
                        Player duenoBanco = Main.player[Main.myPlayer];
                        duenoBanco.whoAmI = Main.myPlayer;
                        duenoBanco.active = true;
                        duenoBanco.dead = false;
                        duenoBanco.position = yo.position;
                        duenoBanco.statLife = 400; duenoBanco.statLifeMax = 400;
                        Vector2 posMuneco = yo.Center + new Vector2(300f, 0f);
                        int idxM = NPC.NewNPC(new Terraria.DataStructures.EntitySource_Misc("BancoEstrella"),
                            (int)posMuneco.X, (int)posMuneco.Y, ModContent.NPCType<global::AethonMod.Content.NPCs.DummyNegroNPC>());
                        if (idxM < 0) { Decir("E1-FALLO: el muñeco no nació"); _fase = 9; return; }
                        _munecoE1 = Main.npc[idxM];
                        _munecoE1.lifeMax = 5000; _munecoE1.life = 5000;
                        _munecoE1.velocity = Vector2.Zero;
                        // LA ESTRELLA al 120, lanzada desde el portador hacia el muñeco
                        Vector2 rumbo = (posMuneco - yo.Center).SafeNormalize(Vector2.UnitX);
                        int idxE = Projectile.NewProjectile(new Terraria.DataStructures.EntitySource_Misc("BancoEstrella"),
                            yo.Center, rumbo * 12f,
                            ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Grimorio.EstrellaGrimorioProjectile>(),
                            60, 0f, Main.myPlayer, 120f, 4242f, 0f);
                        if (idxE < 0) { Decir("E1-FALLO: la estrella no nació"); _fase = 9; return; }
                        _estrellaE1 = Main.projectile[idxE];
                        Decir("E1-INVOCACIÓN: muñeco whoAmI=" + _munecoE1.whoAmI + " a 300 px · estrella al 120 whoAmI=" + idxE +
                            " · daño=" + Main.projectile[idxE].damage + " · tileCollide=" + Main.projectile[idxE].tileCollide);
                        _fase = 11;
                        _ticks = 0;
                        return;
                    }

                    case 11:
                    {
                        if (_ticks < 60) return;    // vuelo (25 t a 12 px/t) + detonación (28 t) — la Tormenta vive de t≈25 a t≈145
                        bool munecoVivo = _munecoE1 != null && _munecoE1.active;
                        int vida = munecoVivo ? _munecoE1.life : -1;
                        bool golpeo = munecoVivo && _munecoE1.life < _munecoE1.lifeMax;
                        int quemadura = ModContent.BuffType<global::AethonMod.Content.Buffs.QuemaduraVacioExterior>();
                        bool conQuemadura = munecoVivo && _munecoE1.HasBuff(quemadura);
                        bool tormenta = HayTormenta();
                        bool estrellaViva = _estrellaE1 != null && _estrellaE1.active;
                        bool exito = golpeo && conQuemadura && tormenta && !estrellaViva;
                        Decir((exito ? "E1-ESTRELLA 120 ✔" : "E1-ESTRELLA 120 ✘") +
                            " (golpeó=" + golpeo + " vida " + vida + "/" + (munecoVivo ? _munecoE1.lifeMax : -1) +
                            " · quemadura=" + conQuemadura + " · tormenta=" + tormenta + " · estrella viva=" + estrellaViva + ")");
                        if (exito) _veredictoE1 = "E1-ESTRELLA 120 ✔";
                        MatarTodoCosmico();
                        _fase = 12;
                        _ticks = 0;
                        return;
                    }

                    // ============================================================
                    //  v6.185 — E2: LA ESTRELLA AL 0 — GOLPEA Y SE ESFUMA
                    // ============================================================
                    case 12:
                    {
                        if (_ticks < 30) return;
                        Vector2 posMuneco = yo.Center + new Vector2(300f, 0f);
                        int idxM = NPC.NewNPC(new Terraria.DataStructures.EntitySource_Misc("BancoEstrella"),
                            (int)posMuneco.X, (int)posMuneco.Y, ModContent.NPCType<global::AethonMod.Content.NPCs.DummyNegroNPC>());
                        if (idxM < 0) { Decir("E2-FALLO: el muñeco no nació"); _fase = 9; return; }
                        _munecoE2 = Main.npc[idxM];
                        _munecoE2.lifeMax = 5000; _munecoE2.life = 5000;
                        _munecoE2.velocity = Vector2.Zero;
                        Vector2 rumbo = (posMuneco - yo.Center).SafeNormalize(Vector2.UnitX);
                        int idxE = Projectile.NewProjectile(new Terraria.DataStructures.EntitySource_Misc("BancoEstrella"),
                            yo.Center, rumbo * 12f,
                            ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Grimorio.EstrellaGrimorioProjectile>(),
                            60, 0f, Main.myPlayer, 0f, 777f, 0f);
                        if (idxE < 0) { Decir("E2-FALLO: la estrella no nació"); _fase = 9; return; }
                        _estrellaE2 = Main.projectile[idxE];
                        Decir("E2-INVOCACIÓN: estrella al 0 whoAmI=" + idxE + " · tileCollide=" + Main.projectile[idxE].tileCollide);
                        _fase = 13;
                        _ticks = 0;
                        return;
                    }

                    case 13:
                    {
                        if (_ticks < 120) return;   // vuelo + contacto
                        bool munecoVivo = _munecoE2 != null && _munecoE2.active;
                        bool golpeo = munecoVivo && _munecoE2.life < _munecoE2.lifeMax;
                        bool tormenta = HayTormenta();
                        bool estrellaViva = _estrellaE2 != null && _estrellaE2.active;
                        bool exito = golpeo && !tormenta && !estrellaViva;
                        Decir((exito ? "E2-ESTRELLA 0 ✔ (golpea y se esfuma, sin explosión)" : "E2-ESTRELLA 0 ✘") +
                            " (golpeó=" + golpeo + " vida " + (munecoVivo ? _munecoE2.life : -1) +
                            " · tormenta=" + tormenta + " · estrella viva=" + estrellaViva + ")");
                        if (exito) _veredictoE2 = "E2-ESTRELLA 0 ✔";
                        Decir("FIN DEL BANCO — " + (_veredictoA ?? "sin veredicto A") + " · " +
                            (_veredictoE1 ?? "E1 sin veredicto") + " · " + (_veredictoE2 ?? "E2 sin veredicto"));
                        _fase = 9;
                        try { Main.player[Main.myPlayer].active = false; }
                        catch (System.Exception exCierre) { RegistroSistema.Fallo("DiagnosticoFestin.Cierre", exCierre); }
                        // v6.185 — EL BANCO CIERRA EL SERVIDOR (el gate headless
                        //     queda autónomo: sin matar por timeout — tML guarda
                        //     el mundo y sale con «Saving before exit...»).
                        try { Netplay.Disconnect = true; }
                        catch (System.Exception exCierre) { RegistroSistema.Fallo("DiagnosticoFestin.Cierre", exCierre); }
                        return;
                    }
                }
            }
            catch (System.Exception exRegistro)
            {
                RegistroSistema.Fallo("DiagnosticoFestin", exRegistro);
                Decir("FALLO DEL BANCO: " + exRegistro.GetType().Name + " — " + exRegistro.Message);
                _fase = 9;
                try { Netplay.Disconnect = true; }
                        catch (System.Exception exCierre) { RegistroSistema.Fallo("DiagnosticoFestin.Cierre", exCierre); }
            }
        }

        /// <summary>La cabeza viva más cercana a un punto (del tipo pedido).</summary>
        private static NPC CabezaMasCercana(Vector2 centro, float radio, int tipo = NPCID.EaterofWorldsHead)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipo) continue;
                float d = n.Center.DistanceSQ(centro);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>Las piezas vivas del EoW (o del tipo que se pida).</summary>
        private static int ContarPiezas(int cuerpo = NPCID.EaterofWorldsBody, int cola = NPCID.EaterofWorldsTail)
        {
            int n = 0;
            int cabeza = cuerpo == NPCID.EaterofWorldsBody ? NPCID.EaterofWorldsHead : NPCID.TheDestroyer;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC == null || !nPC.active) continue;
                if (nPC.type == cabeza || nPC.type == cuerpo || nPC.type == cola) n++;
            }
            return n;
        }

        /// <summary>La ÚLTIMA sección viva del gusano (la regla de la casa
        /// v6.154 — la que debe cenar).</summary>
        private static NPC UltimaSeccion(NPC cabeza)
        {
            if (cabeza == null) return null;
            NPC ultima = cabeza;
            float mejor = -1f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type != NPCID.EaterofWorldsHead && n.type != NPCID.EaterofWorldsBody &&
                    n.type != NPCID.EaterofWorldsTail) continue;
                if (n.Center.DistanceSQ(cabeza.Center) > 2600f * 2600f) continue;
                float d = n.Center.DistanceSQ(cabeza.Center);
                if (d > mejor) { mejor = d; ultima = n; }
            }
            return ultima;
        }

        /// <summary>¿Alguna pieza (de los tipos pedidos) está en LA PRIMERA CENA (estado 3)?</summary>
        private static NPC PresaEnCena(int tipo = NPCID.EaterofWorldsHead,
            int cuerpo = NPCID.EaterofWorldsBody, int cola = NPCID.EaterofWorldsTail)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type != tipo && n.type != cuerpo && n.type != cola) continue;
                if (n.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var g) &&
                    g.DevorarEstado == Globals.FaucesGlobalNPC.ESTADO_PRIMERA_CENA) return n;
            }
            return null;
        }

        /// <summary>¿Vive alguna Tormenta Nebular (la transformación del 100+)?</summary>
        private static bool HayTormenta()
        {
            int tipo = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Cosmic.TormentaNebularProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo) return true;
            }
            return false;
        }

        /// <summary>Limpieza del banco: los muñecos, las tormentas y las
        /// estrellas hijas mueren (sin cine) para la prueba siguiente.</summary>
        private static void MatarTodoCosmico()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == ModContent.NPCType<global::AethonMod.Content.NPCs.DummyNegroNPC>())
                    n.active = false;
            }
            int[] tipos =
            {
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.TormentaNebularProjectile>(),
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.MagnetarProjectile>(),
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.WhiteDwarfProjectile>(),
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.DeadStarProjectile>(),
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Grimorio.EstrellaGrimorioProjectile>(),
            };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;
                foreach (int t in tipos) if (p.type == t) { p.active = false; break; }
            }
        }

        /// <summary>¿Vive el proyectil del cine (PrimeraCenaProjectile)?</summary>
        private static bool HayCine()
        {
            int tipoCine = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Sombras.PrimeraCenaProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipoCine) return true;
            }
            return false;
        }

        /// <summary>Limpieza entre pruebas: todo el EoW muere normal (sin cena).</summary>
        private static void MatarDevoramundos()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type != NPCID.EaterofWorldsHead && n.type != NPCID.EaterofWorldsBody &&
                    n.type != NPCID.EaterofWorldsTail) continue;
                if (n.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var g)) g.DevorarEstado = 2;
                n.active = false;
            }
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
