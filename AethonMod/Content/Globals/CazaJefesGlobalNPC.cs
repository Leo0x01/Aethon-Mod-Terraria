using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.67 — EL TESTIGO DE LA CAZA: cuando un JEFE PRINCIPAL de
    //  Terraria muere, TODOS los jugadores vivos anotan SU puerta en el
    //  registro del cazador (CazaJefesPlayer — la letra: «estamos en
    //  nivel 1 y matamos a plantera… cuando lleguemos al nivel de
    //  despertar que la pide entonces este ya estaria marcado»).
    //
    //  El mapa NPC→puerta es el ESPEJO EXACTO de
    //  DespertarGrimorio.LosJefesDelDespertar (la escalera de la caza):
    //  el Rey Gelatina paga la puerta del 3 · Plantera la del 32 · el
    //  Señor de la Luna LA DEL 50 — LA CIMA (v6.75: la letra pidió al
    //  dios final como requisito del nivel máximo del despertar).
    //  Los segmentos del Devorador y los dos Gemelos comparten puerta
    //  (la pareja ES el jefe — la casa de EcoSistema ya los trataba
    //  igual).
    //
    //  (La banda del mundo de vanilla — NPC.downed* — se escribe sola:
    //  es la OTRA fuente del crédito, la que ya cubría las muertes de
    //  ANTES del mod. Este global cubre el registro personal: lo que el
    //  cazador carga a otros mundos.)
    // ======================================================================
    public class CazaJefesGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            try
            {
                int puerta = PuertaDe(npc.type);
                if (puerta > 0)
                {
                    // LA PUERTA SE PAGA PARA TODAS LAS PANTALLAS VIVAS (una
                    // cacería es de quien la ve — y del mundo, vía la bandera
                    // de vanilla, que ya se escribió sola).
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        p.GetModPlayer<Players.CazaJefesPlayer>()?.Pagar(puerta);
                    }
                }

                // ============================================================
                //  v6.86 — LOS CONTADORES DE LA IRA (la letra de la cima:
                //  «venciendo a moon lord al menos 5 veces, y venciendo al
                //  fragmento de un recuerdo lejano Aethon 1 vez»): el
                //  despertar 100 ya NO basta — estos relojes los paga el
                //  OnKill (todas las pantallas vivas, como las puertas).
                // ============================================================
                if (npc.type == NPCID.MoonLordCore || npc.type == NPCID.MoonLordHead)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador != null) cazador.VecesMoonLord++;
                    }
                }
                // v6.88 — EL CONTADOR DE LA EMPERATRIZ (la letra de la
                //     cima: «5 emperatris de la luz»): lo pagan las
                //     pantallas vivas, como el de Moon Lord.
                if (npc.type == NPCID.HallowBoss)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador != null) cazador.VecesEmperatriz++;
                    }
                }
                if (npc.type == ModContent.NPCType<NPCs.AethonBoss>() ||
                    npc.type == ModContent.NPCType<NPCs.AethonBossOscuro>() ||
                    npc.type == ModContent.NPCType<NPCs.AethonBossFusionado>())
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador == null) continue;
                        cazador.VecesAethon++;
                        cazador.Pagar(100);  // v6.88 — LA CIMA COMPUESTA (era la 59: Aethon sube a la CIEN)
                    }
                }

                // ============================================================
                //  v6.86 — LA MURALLA SUELTA EL LIBRO (la letra del
                //  peldaño 20): «o que directamente lo suelte la muralla
                //  de carne con una probabilidad del 50%» — el LIBRO
                //  CORRUPTO cae al suelo del mundo (lo levanta quien no
                //  tenga el suyo: CanPickup filtra al resto — 1 por
                //  cazador, para siempre).
                // ============================================================
                // ============================================================
                //  v6.88 — LAS BAJAS DEL LIBRO AZUL (el arma del árbol de
                //  habilidades sube de nivel con las bajas de quien la
                //  lleva: 8 por nivel, tope 100 — los puntos del árbol
                //  nacen de SU nivel (SkillTreePlayer), no del despertar.
                // ============================================================
                try
                {
                    int k = npc.lastInteraction;
                    if (k >= 0 && k < Main.maxPlayers)
                    {
                        Player pj = Main.player[k];
                        if (pj != null && pj.active)
                        {
                            var st = pj.GetModPlayer<Players.SkillTreePlayer>();
                        }
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("CazaJefesGlobalNPC", exRegistro); }

                // ============================================================
                //  v6.185 — TODO JEFE SUELTA EL LIBRO (la letra: «asegurate
                //  de que cada jefe tenga una probabilidad del 50% de
                //  soltar el Libro Consagrado, pero si ya esta en el
                //  inventario del jugador este no sale»). La Muralla
                //  (v6.86) deja de ser la única: CUALQUIER jefe — vanilla
                //  o del mod — tira UNA VEZ por jefe vencido.
                //
                //  UNA TIRADA POR JEFE, NO POR SEGMENTO: los gusanos y los
                //  jefes de varios cuerpos matan a cada pieza por separado
                //  (y en vanilla los segmentos del Devoramundo NI LLEVAN
                //  boss=true — decompile NPC.SetDefaults: solo la cabeza
                //  cuenta en ShouldBeCountedAsBoss). La tirada espera a la
                //  ÚLTIMA pieza de la familia: mientras quede una viva
                //  (excluyendo la que muere ahora), el jefe aún no cayó.
                //  Los pilares lunares y el Dios de las Antorchas (también
                //  boss=true en vanilla) NO son jefes de este botín.
                //
                //  LA REGLA DEL INVENTARIO (la letra): si el cazador que lo
                //  venció YA lleva el libro encima, o ya lo consumió, el
                //  libro NO sale. Lo recoge quien no lo lleve (OnPickup).
                // ============================================================
                if (EsJefeConBotin(npc) && EsLaUltimaPieza(npc))
                {
                    Player pjLibro = ElCazadorDe(npc);
                    var cazadorLibro = pjLibro != null
                        ? pjLibro.GetModPlayer<Players.CazaJefesPlayer>() : null;
                    if (cazadorLibro != null && Systems.DespertarGrimorioSistema.ElLibroPuedeCaerle(pjLibro, cazadorLibro)
                        && Main.rand.NextBool(2))
                    {
                        int idx = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox,
                            ModContent.ItemType<Items.Altar.LibroCorrupto>(), 1);
                        if (idx >= 0 && Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("CazaJefesGlobalNPC", exRegistro); }
        }

        /// <summary>v6.185 — ¿ESTE NPC es un JEFE con derecho al botín del
        /// libro? Todo jefe de vanilla y del mod (boss=true), el Devoramundo
        /// (sus segmentos entran por la familia), y FUERA los pilares
        /// lunares y el Dios de las Antorchas (boss=true en vanilla pero
        /// no son jefes que se venzan por el botín).</summary>
        private static bool EsJefeConBotin(NPC npc)
        {
            if (npc == null) return false;
            if (npc.type == NPCID.TorchGod) return false;
            if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex ||
                npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust)
                return false;
            return npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type] || FamiliaDelJefe(npc.type) > 0;
        }

        /// <summary>v6.185 — LA FAMILIA del jefe (0 = cuerpo único): los
        /// jefes que vanilla parte en varios NPC y que mueren EN CADENA.
        /// La tirada del libro espera a que no quede NINGUNO vivo.</summary>
        private static int FamiliaDelJefe(int tipo)
        {
            return tipo switch
            {
                NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail => 1,
                NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail => 2,
                NPCID.MoonLordHead or NPCID.MoonLordHand or NPCID.MoonLordCore => 3,
                NPCID.SkeletronHead or NPCID.SkeletronHand => 4,
                NPCID.Golem or NPCID.GolemHead or NPCID.GolemFistLeft or NPCID.GolemFistRight => 5,
                NPCID.WallofFlesh or NPCID.WallofFleshEye => 6,
                NPCID.SkeletronPrime or NPCID.PrimeCannon or NPCID.PrimeSaw or NPCID.PrimeVice or NPCID.PrimeLaser => 7,
                NPCID.MartianSaucerCore or NPCID.MartianSaucerTurret or NPCID.MartianSaucerCannon => 8,
                _ => 0,
            };
        }

        /// <summary>v6.185 — ¿Es ESTA la última pieza de su jefe? Para los
        /// cuerpos únicos, siempre. Para las familias: mientras quede OTRA
        /// pieza viva del mismo jefe (que no sea la que muere ahora), el
        /// jefe sigue en pie y la tirada espera. OnKill corre con el NPC
        /// aún «active»: se excluye por whoAmI.</summary>
        private static bool EsLaUltimaPieza(NPC npc)
        {
            int familia = FamiliaDelJefe(npc.type);
            if (familia == 0) return true;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC otro = Main.npc[i];
                if (otro == null || !otro.active || i == npc.whoAmI) continue;
                if (FamiliaDelJefe(otro.type) == familia) return false;
            }
            return true;
        }

        /// <summary>v6.185 — EL CAZADOR que venció al jefe (quien decide si
        /// el libro sale o no): el último jugador que lo golpeó; si la
        /// interacción no sirve (un jefe muerto por el entorno, una
        /// trampa), el jugador vivo más cercano.</summary>
        private static Player ElCazadorDe(NPC npc)
        {
            try
            {
                int k = npc.lastInteraction;
                if (k >= 0 && k < Main.maxPlayers)
                {
                    Player p = Main.player[k];
                    if (p != null && p.active) return p;
                }
                Player mejor = null;
                float mejorD = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active) continue;
                    float d = p.Center.DistanceSQ(npc.Center);
                    if (d < mejorD) { mejorD = d; mejor = p; }
                }
                return mejor;
            }
            catch { return null; }
        }

        /// <summary>LA PUERTA QUE PAGA ESTE NPC (0 = ninguno de la
        /// escalera): el espejo de LosJefesDelDespertar, por tipo.</summary>
        private static int PuertaDe(int tipo)
        {
            return tipo switch
            {
                NPCID.KingSlime            => 3,
                NPCID.EyeofCthulhu         => 5,
                NPCID.EaterofWorldsHead    => 8,   // el mal del mundo: la serpiente…
                NPCID.EaterofWorldsBody    => 8,   // …cualquier segmento que muere…
                NPCID.EaterofWorldsTail    => 8,   // …cierra la puerta igual
                NPCID.BrainofCthulhu       => 8,   // …o su primo carmesí
                NPCID.QueenBee             => 10,
                NPCID.SkeletronHead        => 13,
                // v6.88 — EL DUQUE FISHRON VUELVE COMO PUERTA (la letra
                // nueva: «ya que como requisitos pides items del Duque
                // Fishron entonces incluye a ese jefe entre los
                // requisitos») — la 37, con sus armas en el MISMO
                // peldaño (puerta Y botín). Deerclops sigue FUERA (la
                // letra v6.86 lo excluyó y nadie lo reclamó).
                NPCID.DukeFishron          => 37,
                NPCID.WallofFlesh          => 20,
                NPCID.QueenSlimeBoss       => 23,
                NPCID.Retinazer           => 25,   // los Gemelos: cada lente…
                NPCID.Spazmatism          => 25,   // …cierra la MISMA puerta
                NPCID.TheDestroyer         => 28,
                NPCID.SkeletronPrime       => 30,
                NPCID.Plantera             => 32,  // la puerta ALTA de la letra
                NPCID.Golem                => 35,
                NPCID.HallowBoss           => 38,  // la Emperatriz de la Luz — v6.86: SU puerta (era la 40)
                NPCID.CultistBoss          => 45,  // el Cultista Lunático (la antesala de la cima)
                NPCID.MoonLordCore         => 50,  // el corazón del Señor de la Luna — LA CIMA de vanilla
                NPCID.MoonLordHead         => 50,  // (la cabeza cuenta si el núcleo voló raro)
                // Aethon (59) NO vive aquí: su OnKill de arriba lo paga
                // (ModContent no entra en un switch de constantes).
                _ => 0,
            };
        }
    }
}
