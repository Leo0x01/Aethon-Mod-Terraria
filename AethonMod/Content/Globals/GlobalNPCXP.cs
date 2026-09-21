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
    /// - Hace que King Slime y Eye of Cthulhu dropeen el Fragmento Génesis.
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

            // === DROP DEL FRAGMENTO GÉNESIS ===
            if (npc.type == NPCID.KingSlime || npc.type == NPCID.EyeofCthulhu)
            {
                Player killer = FindKiller(npc);
                if (killer != null)
                {
                    bool hasGrimoire = false;
                    for (int i = 0; i < 58; i++)
                    {
                        if (killer.inventory[i] != null &&
                            killer.inventory[i].type == ModContent.ItemType<Weapons.GrimoireEternal>())
                        {
                            hasGrimoire = true;
                            break;
                        }
                    }
                    if (!hasGrimoire)
                    {
                        int drop = Item.NewItem(npc.GetSource_Loot(), npc.Center,
                            ModContent.ItemType<Items.GenesisShard>(), 1);
                        if (drop >= 0 && drop < Main.item.Length)
                            Main.item[drop].noGrabDelay = 0;
                    }
                }
            }

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
                    var sello = npc.GetGlobalNPC<OleadaNPC>();
                    if (sello != null && sello.EsDeOleada)
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
                    if (npc.boss)
                    {
                        player.GetModPlayer<Players.ShardPlayer>()?.CronicaMarcar(npc.type);
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

                    // v6.47 — LA VOZ DEL HAMBRE: la kill ALIMENTA el libro —
                    // la hambre se perdona (susurros y barra palidecida
                    // vuelven a su sitio). v6.49: el servidor manda el
                    // estado nuevo al portador (EcoRed.SincronizarHambre
                    // corre dentro de RegistrarKill).
                    if (cobro || xp > 0)
                        player.GetModPlayer<Players.ShardPlayer>()?.RegistrarKill();

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
                    if (!npc.boss && cobro)
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
                // del que mató.
                if (npc.boss)
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
