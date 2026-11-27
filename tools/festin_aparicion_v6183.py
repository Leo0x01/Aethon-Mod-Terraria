#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — LA CIRUGÍA DEL FESTÍN DE LA APARICIÓN (la letra: «la
animacion para devorar al Devoramundos sigue sin activarse…
podrias hacer que a penas aparezca el Devoramundo el grimorio del
eterno arranque una seccion y se la coma cumpliendo asi el evento de
Festín»).

LA AUTOPSIA COMPLETA:
  · La v6.182 curó el FUTURO (los guardianes de oleada ya no queman
    la marca) pero dejó el PASADO clavado: los cerrojos
    DevoramundosPedacito ya quemados por las oleadas de la era
    v6.180/.181 viven en el .plr — y sin marca que gastar, el
    pedacito JAMÁS volvía a servirse («sigue sin activarse»).
  · La MARCA era un eslabón de más: el evento que el usuario quiere
    es LA APARICIÓN, no el camino de invocación.

EL DISEÑO NUEVO (la letra, exacta):
  1. OnSpawn de la cabeza de Devorador: LA APARICIÓN ES LA LLAVE —
     la primera vez que aparece cerca (≤2600 px) de un portador con
     el libro, el pedacito se sirve. Sin marca. (Cerrojo de
     una-vez-por-portador intacto; la Página en Blanco lo devuelve.)
  2. LOS GUARDIANES DE LAS OLEADAS VETAN la mesa el mismo tick en
     que nacen (GrimorioFuriaSistema apaga la espera y devuelve el
     cerrojo) — nacen en el borde del cuadro, no son «el
     Devoramundo» del festín.
  3. LA MIGRACIÓN del cerrojo quemado: al entrar al mundo, una sola
     vez, el cerrojo viejo vuelve a la mesa (la era de las oleadas
     quemó cerrojos que NUNCA vieron el festín).
  4. La marca de la llamada MUERE entera (métodos y las dos llamadas
     que la armaban).
Cirugía con asserts de la casa: si el patrón no está, MUERE.
"""
import io

BASE = '/home/z/my-project/mod/AethonMod/Content'


def parche(ruta, viejo, nuevo, nota):
    p = f'{BASE}/{ruta}'
    with io.open(p, encoding='utf-8') as f:
        s = f.read()
    assert viejo in s, f'{ruta}: no encuentro el patrón de «{nota}»'
    assert s.count(viejo) == 1, f'{ruta}: «{nota}» aparece {s.count(viejo)} veces'
    s = s.replace(viejo, nuevo, 1)
    with io.open(p, 'w', encoding='utf-8') as f:
        f.write(s)
    print(f'  ✔ {ruta}: {nota}')


# ── 1. ONSPAWN: LA APARICIÓN ES LA LLAVE ──────────────────────────
parche(
    'Globals/FaucesGlobalNPC.cs',
    '''                    Player portador = null;
                    float mejorD = 3000f * 3000f;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        float d = p.Center.DistanceSQ(npc.Center);
                        if (d < mejorD) { mejorD = d; portador = p; }
                    }
                    if (portador != null && npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var gPedacito))
                    {
                        var spPedacito = portador.GetModPlayer<Players.ShardPlayer>();
                        if (spPedacito != null && !spPedacito.DevoramundosPedacito &&
                            spPedacito.ConsumirLlamadaDevoramundos())
                        {
                            // el festín se SIRVE una sola vez por
                            // portador (la Página en Blanco la borra con
                            // el olvido)
                            spPedacito.DevoramundosPedacito = true;
                            gPedacito.PedacitoEspera = 150;   // ~2,5 s: que la cadena nazca
                            gPedacito.PedacitoPortador = portador.whoAmI;
                            npc.netUpdate = true;
                        }
                    }
                    return;   // la cabeza del gusano no sigue por el guardián''',
    '''                    // v6.183 — LA APARICIÓN ES LA LLAVE (la letra:
                    // «podrias hacer que a penas aparezca el
                    // Devoramundo el grimorio del eterno arranque una
                    // seccion y se la coma cumpliendo asi el evento de
                    // Festin»). LA MARCA DE LA LLAMADA MUERE: dependía
                    // del CAMINO de invocación (pilar · comida de gusano)
                    // y los cerrojos quemados en la era v6.180/.181 por
                    // los guardianes de oleada dejaron el festín muerto
                    // para siempre. AHORA: la PRIMERA VEZ que el
                    // Devoramundo APARECE a ≤2600 px de un portador CON
                    // EL LIBRO, el pedacito se sirve — el evento es LA
                    // APARICIÓN. (El ancla del pilar nace a 100 tiles =
                    // 1600 px del pilar: dentro. La comida de gusano y
                    // los orbes rotos: al lado del portador: dentro.)
                    // El cerrojo de una-vez-por-portador sigue vivo (la
                    // Página en Blanco lo devuelve a la mesa) y LOS
                    // GUARDIANES DE OLEADA VETAN la mesa el mismo tick
                    // en que nacen (abajo, en GrimorioFuriaSistema).
                    Player portador = null;
                    float mejorD = 2600f * 2600f;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        if (!TieneEternoEnInventario(p)) continue;   // el festín es del LIBRO: sin libro no hay festín
                        float d = p.Center.DistanceSQ(npc.Center);
                        if (d < mejorD) { mejorD = d; portador = p; }
                    }
                    if (portador != null && npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var gPedacito))
                    {
                        var spPedacito = portador.GetModPlayer<Players.ShardPlayer>();
                        if (spPedacito != null && !spPedacito.DevoramundosPedacito)
                        {
                            // el festín se SIRVE una sola vez por
                            // portador (la Página en Blanco la borra con
                            // el olvido)
                            spPedacito.DevoramundosPedacito = true;
                            gPedacito.PedacitoEspera = 150;   // ~2,5 s: que la cadena nazca
                            gPedacito.PedacitoPortador = portador.whoAmI;
                            npc.netUpdate = true;
                        }
                    }
                    return;   // la cabeza del gusano no sigue por el guardián''',
    'la aparición es la llave')

# ── 2. EL VETO DE LOS GUARDIANES (los dos sitios de nacimiento) ───
VETO = '''
                // v6.183 — EL VETO DEL GUARDIÁN DE OLEADA (la letra del
                // festín: «a penas aparezca el Devoramundo»): si ESTA
                // cabeza de Devorador prendió la mesa del pedacito al
                // nacer (apareció cerca de un portador), el Juicio la
                // APAGA el mismo tick: los guardianes de oleada nacen en
                // el BORDE DEL CUADRO y JAMÁS son el Devoramundo del
                // festín — el cerrojo vuelve a la mesa y la espera muere
                // sin servir nada.
                if (tipo == NPCID.EaterofWorldsHead &&
                    jefe.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var gVeto) &&
                    gVeto.PedacitoEspera > 0)
                {
                    gVeto.PedacitoEspera = 0;
                    if (gVeto.PedacitoPortador >= 0 && gVeto.PedacitoPortador < Main.maxPlayers)
                    {
                        var spVeto = Main.player[gVeto.PedacitoPortador]?
                            .GetModPlayer<Players.ShardPlayer>();
                        if (spVeto != null) spVeto.DevoramundosPedacito = false;
                    }
                    gVeto.PedacitoPortador = -1;
                }'''

parche(
    'Systems/GrimorioFuriaSistema.cs',
    '''                if (jefe.TryGetGlobalNPC<OleadaNPC>(out var selloJuicio))
                    selloJuicio.Marcar(jefe, 11, jefe: true, especial: true);
                jefe.netUpdate = true;''',
    '''                if (jefe.TryGetGlobalNPC<OleadaNPC>(out var selloJuicio))
                    selloJuicio.Marcar(jefe, 11, jefe: true, especial: true);
                jefe.netUpdate = true;''' + VETO,
    'veto del guardián del Juicio')

parche(
    'Systems/GrimorioFuriaSistema.cs',
    '''                if (jefe.TryGetGlobalNPC<OleadaNPC>(out var selloGuardian))
                    selloGuardian.Marcar(jefe, _oleadaActual, jefe: true, nivel: _nivelFuria);
                jefe.netUpdate = true;''',
    '''                if (jefe.TryGetGlobalNPC<OleadaNPC>(out var selloGuardian))
                    selloGuardian.Marcar(jefe, _oleadaActual, jefe: true, nivel: _nivelFuria);
                jefe.netUpdate = true;''' + VETO,
    'veto del guardián de la oleada')

# ── 3. LA MIGRACIÓN DEL CERROJO QUEMADO (ShardPlayer.OnEnterWorld) ─
parche(
    'Players/ShardPlayer.cs',
    '''            if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                EntregarRegaloDePruebas();
        }''',
    '''            if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                EntregarRegaloDePruebas();

            // v6.183 — LA MIGRACIÓN DEL CERROJO QUEMADO (la letra:
            // «la animacion para devorar al Devoramundos SIGUE sin
            // activarse»): la era v6.180/.181 dejó cerrojos
            // DevoramundosPedacito quemados por guardianes de OLEADA
            // (el pedacito servido en la bruma, lejos de la vista —
            // nadie lo vio) y v6.182 curó el futuro pero no el pasado:
            // el cerrojo muerto vivía en el .plr para siempre. UNA
            // sola vez por portador, el cerrojo viejo VUELVE A LA MESA
            // — la aparición del Devoramundo (v6.183) lo quemará de
            // verdad, a la vista de todos.
            if (DevoramundosPedacito && !DevoramundosPedacitoMigrado)
            {
                DevoramundosPedacito = false;
                DevoramundosPedacitoMigrado = true;
            }
        }''',
    'la migración del cerrojo quemado')

# el campo que viaja con el jugador
parche(
    'Players/ShardPlayer.cs',
    '''        public bool DevoramundosPedacito;''',
    '''        public bool DevoramundosPedacito;

        /// <summary>v6.183 — LA MIGRACIÓN ÚNICA del cerrojo quemado por
        /// las oleadas de la era v6.180/.181 (ver OnEnterWorld).</summary>
        public bool DevoramundosPedacitoMigrado;''',
    'el campo de la migración')

# y su persistencia
parche(
    'Players/ShardPlayer.cs',
    '''                tag["devoramundosPedacito"] = DevoramundosPedacito;''',
    '''                tag["devoramundosPedacito"] = DevoramundosPedacito;
                tag["devoramundosPedacitoMigrado"] = DevoramundosPedacitoMigrado;''',
    'el guardado de la migración')

parche(
    'Players/ShardPlayer.cs',
    '''                DevoramundosPedacito = tag.GetBool("devoramundosPedacito");''',
    '''                DevoramundosPedacito = tag.GetBool("devoramundosPedacito");
                DevoramundosPedacitoMigrado = tag.GetBool("devoramundosPedacitoMigrado");''',
    'la carga de la migración')

# ── 4. LA MARCA DE LA LLAMADA MUERE ENTERA ─────────────────────────
parche(
    'Players/ShardPlayer.cs',
    '''        private ulong _llamadaDevoramundosTick;

        /// <summary>v6.182 — EL PORTADOR LLAMA: marca la invocación real
        /// (10 s de vida — si vanilla rechaza el uso, la marca caduca
        /// sola sin quemar nada).</summary>
        public void MarcarLlamadaDevoramundos()
        {
            _llamadaDevoramundos = true;
            _llamadaDevoramundosTick = Main.GameUpdateCount;
        }

        /// <summary>v6.182 — ¿NACE UNA CABEZA DE LA LLAMADA? La consume
        /// (una invocación, una cabeza).</summary>
        public bool ConsumirLlamadaDevoramundos()
        {
            if (!_llamadaDevoramundos) return false;
            if (Main.GameUpdateCount - _llamadaDevoramundosTick > 600ul)
            {
                _llamadaDevoramundos = false;   // caducó: la llamada se enfrió
                return false;
            }
            _llamadaDevoramundos = false;       // consumida por ESTA cabeza
            return true;
        }

''',
    '''        // (v6.183 — LA MARCA DE LA LLAMADA MURIÓ: la aparición del
        // Devoramundo es la llave del festín ahora — OnSpawn la sirve
        // sin intermediarios. El campo _llamadaDevoramundos queda fuera
        // de servicio.)

''',
    'la muerte de la marca')

parche(
    'Globals/InteriorInvocaGlobalItem.cs',
    '''            // v6.182 — LA MARCA DE LA LLAMADA DEL DEVORAMUNDOS (la autopsia
            //  del festín que no iniciaba): la v6.180 armaba el pedacito
            //  con CUALQUIER cabeza de Devorador nacida cerca del portador
            //  — y los GUARDIANES DE LAS OLEADAS también son Devoradores
            //  (LosSeis): el primer guardián de oleada quemaba la marca
            //  sin que el portador viera nada. AHORA SOLO la COMIDA DE
            //  GUSANO marca la llamada (dentro Y fuera del interior — el
            //  camino de vanilla sigue a este return), y OnSpawn la
            //  consume al nacer ESA cabeza. La marca caduca sola si
            //  vanilla rechaza el uso.
            if (item.type == ItemID.WormFood)
            {
                var spMarca = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
                if (spMarca != null && !spMarca.DevoramundosPedacito)
                    spMarca.MarcarLlamadaDevoramundos();
            }

            // v6.179 — LA MARCA DE LA LLAMADA (la letra: «se invoca mas de
            // 2 veces despues de borrar la lista del festin y no lo hace»):
            // el ítem de invocación del Devorador en la MANO es una
            // llamada del PORTADOR — la marca pendiente vive en el
            // ShardPlayer y SOLO la cabeza de Devorador que nace con ella
            // cuenta una invocación (el vigía de FaucesGlobalNPC la gasta).
            // Cubre FUERA del interior (el camino de vanilla que sigue a
            // este return)…
            // SOLO dentro del Mundo Interior — afuera vanilla manda
            if (!Systems.SubmundoGrimorio.Activo || Main.gameMenu) return true;''',
    '''            // v6.183 — LA MARCA DE LA LLAMADA MURIÓ (la letra: «a penas
            //  aparezca el Devoramundo el grimorio arranque una seccion
            //  y se la coma»): la APARICIÓN de la cabeza es la llave del
            //  festín (FaucesGlobalNPC.OnSpawn) — este ítem ya no marca
            //  nada; come tu gusano en paz, dentro o fuera.
            // SOLO dentro del Mundo Interior — afuera vanilla manda
            if (!Systems.SubmundoGrimorio.Activo || Main.gameMenu) return true;''',
    'la marca muere en la comida de gusano')

parche(
    'UI/PrismaInteriorUI.cs',
    '''                // v6.182 — LA MARCA DE LA LLAMADA (la autopsia del festín
                //     que no iniciaba con el Devoramundos): la v6.180
                //     armaba el pedacito con CUALQUIER cabeza nacida
                //     cerca — y los guardianes de las OLEADAS también son
                //     Devoradores: el primero quemaba la marca sin que el
                //     portador viera nada. AHORA la invocación del
                //     PILAR marca la llamada aquí y OnSpawn la consume
                //     al nacer ESTA cabeza.
                if (tipoJefe == NPCID.EaterofWorldsHead && sp != null && !sp.DevoramundosPedacito)
                    sp.MarcarLlamadaDevoramundos();

''',
    '''                // v6.183 — LA MARCA DE LA LLAMADA MURIÓ: la APARICIÓN
                //     de la cabeza del Devorador es la llave del festín
                //     (FaucesGlobalNPC.OnSpawn la sirve a los 100 tiles
                //     del ancla) — el pilar ya no marca nada.

''',
    'la marca muere en el pilar')

print('CIRUGÍA DEL FESTÍN DE LA APARICIÓN: 9/9 parches vivos')
