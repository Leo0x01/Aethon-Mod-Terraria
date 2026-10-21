using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalNPC que:
    /// - v6.46: otorga XP REAL (rareza del bestiario + dieta ×3 de
    ///   primera kill + jefes con fórmula FIJA) a todo Grimorio de la
    ///   BARRA RÁPIDA (slots 0–9) del jugador que mató — el inventario
    ///   visible de las teclas de número; sostenerlo también cuenta (el
    ///   sostenido ES uno de esos slots). Las kills de los propios
    ///   minions pagan igual (el minion acredita al dueño).
    /// - Aplica lifesteal si el Grimorio SOSTENIDO tiene nivel >= 7
    ///   (v6.46: el robo de vida es poder de combate — exige blandirlo).
    /// - v6.50.117 — LA PURGA DEL GÉNESIS: el drop del Fragmento murió;
    ///   LA CAÍDA (LaCaidaSistema) es la única vía del Grimorio Eterno.
    /// - Enciende el pulso de la barra dorada (ShardHUDSystem) y dispara
    ///   la VOZ del grimorio cuando cae un jefe (EcoSistema).
    /// </summary>
    public class GlobalNPCXP : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // v6.45: garantiza el crédito de la kill para FindKiller
            // (playerInteraction) aunque el motor no lo hubiera marcado.
            if (player != null && player.whoAmI >= 0 && player.whoAmI < Main.player.Length)
                npc.playerInteraction[player.whoAmI] = true;
            ApplyAethonLifesteal(player, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.player.Length) return;
            Player player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                // v6.45: LAS KILLS DE LOS MINIONS ACREDITAN AL DUEÑO. El Orbe
                // Cósmico del Grimorio (y cualquier proyectil del jugador)
                // deja marcado playerInteraction: FindKiller encuentra al
                // dueño aunque la kill la dé el minion, y el Grimorio en su
                // barra rápida cobra la XP. Idempotente: si el motor ya lo
                // marcó, esto no cambia nada.
                npc.playerInteraction[projectile.owner] = true;
                ApplyAethonLifesteal(player, damageDone);
            }
        }

        private void ApplyAethonLifesteal(Player player, int damageDone)
        {
            Item held = player.HeldItem;
            if (held == null || held.type != ModContent.ItemType<Weapons.GrimoireEternal>()) return;

            var sl = held.GetGlobalItem<ShardLevelItem>();
            if (sl == null) return;
            if (!WeaponScaling.HasLifesteal(sl.Level)) return;

            WeaponScaling.ApplyLifesteal(player, damageDone, sl.Level);
        }

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.townNPC) return;

            // v6.50.117 — LA PURGA DEL GÉNESIS: el drop del
            // Fragmento Génesis murió con el fragmento — LA CAÍDA es la
            // única vía canónica del Grimorio del Eterno (un cometa de
            // tinta por jugador, LaCaidaSistema).

            // === v6.46: OTORGAR XP AL GRIMORIO DE LA BARRA RÁPIDA ===
            // El libro come mientras esté en el INVENTARIO VISIBLE (la
            // barra de las teclas de número, slots 0–9 — el sostenido es
            // uno de ellos). Guardado más abajo, en la hucha/vaulta o en
            // un cofre NO come: hay que tenerlo a mano. TODAS las copias
            // visibles cobran (cada una su propio nivel); las stats solo
            // salen de la primera (ShardPlayer) — coherente anti-exploit.
            // Las kills de sus propios minions pagan igual: el orbe
            // acredita a su dueño vía playerInteraction.
            Player player = FindKiller(npc);
            if (player == null) return;

            try
            {
                // La fórmula de los JEFES escala con el nivel del libro:
                // usa el de la PRIMERA copia visible (la misma que manda
                // en las stats — una sola voz para una sola derrota).
                int nivelGrimorio = 0;
                bool libroVisible = false;
                for (int i = 0; i < 10; i++)
                {
                    Item inv = player.inventory[i];
                    if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                        continue;
                    if (!libroVisible)
                    {
                        var slx = inv.GetGlobalItem<ShardLevelItem>();
                        if (slx != null) nivelGrimorio = slx.Level;
                        libroVisible = true;
                    }
                }

                if (libroVisible)
                {
                    int baseXP = ShardLevelSystem.XPForNPC(npc, nivelGrimorio);
                    int xp = ShardLevelSystem.ApplyXPMultiplier(baseXP);

                    // v6.47/v6.48 — LA XP DE LAS OLEADAS: todo lo que muere
                    // convocado por la furia del grimorio paga ×(oleada+1)
                    // — la oleada 1 paga ×2 … la 10 paga ×11, LA ESPECIAL
                    // paga ×15 (jefes incluidos: son las "versiones
                    // especiales" que prometen más XP).
                    // v6.51.2 — TryGet (la lectura segura de siempre: el
                    // aislado no rompe la XP de nadie).
                    if (npc.TryGetGlobalNPC<OleadaNPC>(out var sello) && sello.EsDeOleada)
                        xp *= sello.MultiplicadorXP;

                    // v6.48 — LA CRÓNICA DEL TESTIGO: el libro devoró a ESTE
                    // jefe con este portador — el Testigo ganará su línea
                    // humana de la misma derrota (dos narradores, un hecho).
                    // v6.49: sin filtro de jugador local — en MP la
                    // autoridad es el SERVIDOR (la crónica se guarda en su
                    // réplica del jugador y viaja al reconectar).
                    // v6.50: la réplica del server YA NO basta — el Testigo
                    // lee la crónica en el CLIENTE; EcoRed.MsgCronica lleva
                    // la marca al portador YA (tienda de esencias y páginas
                    // del cronista funcionan en MP desde ahora).
                    // v6.50.3 — FIX (crónica fantasma de segmentos): la marca
                    // usa las MISMAS reglas que la voz (EcoSistema.EsDerrotaCompleta).
                    // Los curados de verdad (verificado al IL de este tML): el
                    // SEÑOR DE LA LUNA (cabeza 396 y manos 397 son boss=true y
                    // mueren como fases ANTES del núcleo: 3 páginas + 3 paquetes
                    // por derrota) y LOS GEMELOS (dos tipos con boss=true y una
                    // sola derrota: 2 páginas). (El Devorador, el ojo del Muro y
                    // las cabezas de Golem tienen boss=FALSE en vanilla — la
                    // nota v6.50.3 original exageraba; el guard los cubre igual
                    // por si un mod externo los marca.) Ahora: solo derrota
                    // completa, y el paquete SOLO si la página es nueva
                    // (CronicaMarcar devuelve si añadió).
                    // v6.50.116 — EL SELLO VERDADERO: el jefe de oleada
                    //     muere con type/netID en 0 para vanilla (el
                    //     aislamiento de la letra: «no deben contar para
                    //     los cambios del mundo»), pero la CRÓNICA del
                    //     Testigo es DEL GRIMORIO — se escribe con SU
                    //     especie verdadera.
                    if ((npc.boss || OleadaNPC.JefeVerdadero(npc)) &&
                        EcoSistema.EsDerrotaCompleta(npc))
                    {
                        if (player.GetModPlayer<Players.ShardPlayer>()?.CronicaMarcar(
                                OleadaNPC.TipoVerdadero(npc)) == true)
                            EcoRed.SincronizarCronica(player);
                    }

                    bool cobro = false;
                    // v6.50.2 — FIX (bandwidth — 3 paquetes por kill): el latido
                    // LLEVA el estado de la primera copia visible y la FOTO
                    // COMPLETA de MsgLibro solo viaja si algún nivel SUBIÓ.
                    bool subioNivel = false;
                    int slotPrimero = -1, nivelPrimero = 0, xpPrimero = 0;
                    if (xp > 0)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Item inv = player.inventory[i];
                            if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                                continue;
                            var sl = inv.GetGlobalItem<ShardLevelItem>();
                            if (sl != null)
                            {
                                int nivelAntes = sl.Level;
                                sl.GrantXP(inv, xp);
                                cobro = true;
                                if (sl.Level > nivelAntes)
                                    subioNivel = true; // la foto completa tiene motivo
                                if (slotPrimero < 0)
                                {
                                    // la PRIMERA copia (la que manda en stats/HUD)
                                    slotPrimero = i;
                                    nivelPrimero = sl.Level;
                                    xpPrimero = sl.XP;
                                }
                            }
                        }
                    }

                    // v6.50.104 — LA NUEVA DIETA DEL ETERNO (la letra:
                    // «el hambre en el grimorio del eterno no baja, el
                    // hambre deberia bajar usando la rareza de las
                    // criaturas como fuente, cada criatura de 1 estrella
                    // solo quita el 0.1% de hambre los jefes quitan 10%
                    // de hambre…»): cada baja que PAGA XP también alimenta
                    // el reloj nervioso del libro — mismo crédito, misma
                    // moneda: la RAREZA del bestiario (1★=0,1% … 5★=5%
                    // lineal; un jefe, 10%). Como AbsorberPresa, corre en
                    // la AUTORIDAD (en SP es la MISMA máquina del tooltip:
                    // la bolsa del hambre lo muestra al instante; la
                    // chusma nocturna natural y la comida de la oleada
                    // TAMBIÉN alimentan aquí — la letra no pide excepciones).
                    if (xp > 0)
                    {
                        float quita = Weapons.GrimoireEternal.HambrePorPresa(npc);
                        if (quita > 0f)
                        {
                            var et = Weapons.GrimoireEternal.EstadoEterno;
                            et.Hambre = System.Math.Max(0f, et.Hambre - quita);
                        }
                    }

                    // v6.47 — LA VOZ DEL HAMBRE: la kill ALIMENTA el libro —
                    // la hambre se perdona (susurros y barra palidecida
                    // vuelven a su sitio). v6.49: el servidor manda el
                    // estado nuevo al portador (EcoRed.SincronizarHambre
                    // corre dentro de RegistrarKill).
                    // v6.50.60 — LA CENA LLEVA SU NOMBRE (la cura del «por
                    // la noche la oleada no se activa»): RegistrarKill
                    // recibe QUÉ murió — la chusma nocturna NATURAL (los
                    // zombies de siempre) ya no alimenta el libro (la
                    // defensa de medianoche no es cena), la comida de la
                    // PROPIA oleada no toca el reloj (es SU festín) y el
                    // libro FURIOSO ya no se calma con bocados.
                    if (cobro || xp > 0)
                        player.GetModPlayer<Players.ShardPlayer>()?.RegistrarKill(
                            OleadaNPC.TipoVerdadero(npc),
                            sello != null && sello.EsDeOleada,
                            npc.boss || OleadaNPC.JefeVerdadero(npc));

                    // La barra dorada late en la pantalla del dueño del libro
                    // (v6.49: en MP el latido viaja por EcoRed al portador).
                    // v6.50.2 — EL LATIDO LLEVA EL LIBRO: (slot, nivel, XP)
                    // de la primera copia viaja en el MISMO paquete — el
                    // cliente aplica el estado y pulsa la barra con UN
                    // paquete por kill (antes: latido + hambre + foto).
                    if (cobro)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient &&
                            player.whoAmI == Main.myPlayer)
                            ShardHUDSystem.MarcarGanancia(xp); // SP: local
                        else
                            EcoRed.LatidoDeXp(player, xp, slotPrimero, nivelPrimero, xpPrimero); // MP: al portador
                    }

                    // v6.50 — LOS LIBROS CAMINAN: el server acaba de subir
                    // SU copia; EcoRed.MsgLibro lleva el nivel nuevo al
                    // portador (tooltips/daño/HUD viven en SU cliente).
                    // v6.50.2 — FIX (bandwidth): la FOTO COMPLETA (los 10
                    // slots) ya NO viaja por kill — SOLO cuando un nivel
                    // SUBIÓ (la celebración del delta necesita la foto), al
                    // entrar al mundo (MsgPedirLibros) y en la red de
                    // seguridad 600t. El resto de kills viaja en el latido
                    // extendido de arriba: la barra sigue EXACTA.
                    if (subioNivel)
                        EcoRed.SincronizarLibros(player);

                    // v6.47 — LA PRIMERA 5★ CON VOZ PROPIA: la primera
                    // criatura 5 estrellas que el libro se come merece su
                    // línea ("Lo más raro que ha comido jamás") — una sola
                    // vez por libro, en la primera copia visible.
                    if (!(npc.boss || OleadaNPC.JefeVerdadero(npc)) && cobro)
                    {
                        try
                        {
                            var slPrimero = player.inventory[0];
                            for (int i = 0; i < 10; i++)
                            {
                                Item inv = player.inventory[i];
                                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                                    continue;
                                slPrimero = inv;
                                break;
                            }
                            var sl5 = slPrimero.GetGlobalItem<ShardLevelItem>();
                            if (sl5 != null && !sl5.PrimeraCincoEstrellas &&
                                ShardLevelSystem.EstrellasDe(npc) >= 5)
                            {
                                sl5.PrimeraCincoEstrellas = true;
                                EcoSistema.SusurrarCincoEstrellas(player);
                            }
                        }
                        catch { }
                    }

                }

                // v6.50 — LA VOZ DEL GRIMORIO ES DEL MUNDO: la derrota de
                // un jefe dispara el mensaje para TODOS los portadores con
                // libro visible (aunque el que mató no cargue ninguno — el
                // diseño MP del usuario: "uno mata al Rey Gelatina, el
                // mensaje se activa para todos"). Cada portador oye SOLO
                // la línea de SU propio grimorio (EcoRed + variantes
                // repartidas por la autoridad). La XP, arriba, sigue siendo
                // del que mató. v6.50.116 — el jefe de oleada aislado
                // (boss=false para vanilla) TAMBIÉN habla: su voz es del
                // grimorio, no de la progresión.
                if (npc.boss || OleadaNPC.JefeVerdadero(npc))
                    EcoSistema.AnunciarJefeMuerto(npc, player);
            }
            catch { }
        }

        private Player FindKiller(NPC npc)
        {
            int killerWho = -1;
            if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.player.Length)
            {
                Player last = Main.player[npc.lastInteraction];
                if (last != null && last.active && !last.dead)
                    killerWho = npc.lastInteraction;
            }
            if (killerWho == -1)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    Player p = Main.player[i];
                    if (p != null && p.active && !p.dead && npc.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho == -1) return null;
            return Main.player[killerWho];
        }
    }
}
