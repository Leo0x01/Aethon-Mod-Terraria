using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  EL SISTEMA DEL DESPERTAR (v6.51.0) — EL COMENSAL Y SU ESCALERA.
    //
    // LA DEVORACIÓN (la letra: «cuando el arma es soltada por el jugador
    // y el grimorio está seleccionado, este puede devorar el arma»): cada
    // 15 t la AUTORIDAD mira los items CAÍDOS alrededor de quien blande
    // el Eterno — un ARMA CORRUPTA soltada ante él es un PLATO:
    //   · Si es LA OFRENDA del siguiente nivel y el portador trae las
    //     ESENCIAS y el DINERO del peldaño: NIVEL DE DESPERTAR +1 (las
    //     tres ofrendas se consumen — el arma, las esencias y las
    //     monedas) y los PODERES desbloqueados se anuncian.
    //   · Si es la ofrenda pero FALTA algo: el grimorio REHÚSA el plato
    //     y dice qué falta (el arma queda en el suelo, recogerla es
    //     gratis).
    //   · Si es CUALQUIER OTRA arma corrupta: EL BOCADO DE SIEMPRE (el
    //     festín de la .129 vive): EXP por rareza + UN STAT por clase +
    //     EL ALMA ÚNICA si era especial.
    //
    // LA ESENCIA CORRUPTA (la letra: «en el momento en el que el grimorio
    // hace su aparición en el mundo, su poder se filtra entre las
    // realidades… se pega a ellos»): desde LA LLEGADA, toda criatura
    // puede soltar 1 (0,1 %) y los jefes 1..10 (90 % → 1 · 5 % → 3 ·
    // 1 % → 10 · el resto repartido) — la chusma de las OLEADAS DE
    // HAMBRE la suelta al 2 % (el propio grimorio los llama) y sus
    // jefes pagan 10 (EsenciasJefesDrops).
    // ======================================================================
    public class DespertarGrimorioSistema : ModSystem
    {
        /// <summary>El pulso del comensal (cada 15 t — 4 veces por
        /// segundo: un plato no se enfría en el suelo).</summary>
        private const int PULSO_DEVORAR = 15;

        /// <summary>El radio del tenedor: items caídos a menos de 200 px
        /// del portador con el Eterno en la MANO.</summary>
        private const float RADIO_DEVORAR = 200f;

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if ((Main.GameUpdateCount % (ulong)PULSO_DEVORAR) != 0ul) return;

                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;

                    // EL GRIMORIO SELECCIONADO (la letra: «el grimorio
                    // está seleccionado» — en la MANO, no en la bolsa).
                    Item mano = p.HeldItem;
                    if (mano == null || mano.IsAir || mano.type != eterno) continue;

                    // Los platos caídos alrededor del portador.
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;   // SOLO items caídos vivos
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;
                        var sello = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (sello == null || !sello.Corrompido) continue;
                        if (!EsArma(drop)) continue;

                        // ¡UN PLATO! ¿Es la ofrenda del siguiente nivel?
                        var sp = p.GetModPlayer<Players.ShardPlayer>();
                        int proximo = System.Math.Min(DespertarGrimorio.NivelDespertarSiguiente(p), DespertarGrimorio.NivelMax);
                        if (DespertarGrimorio.EsArmaDelNivel(proximo, drop))
                        {
                            // ¿Las tres ofrendas completas? (las esencias
                            // primero: si faltan, el grimorio NI TOCA el
                            // dinero — el rechazo no cobra nada).
                            int esencias = DespertarGrimorio.EsenciasPara(proximo);
                            long monedas = DespertarGrimorio.MonedasPara(proximo);
                            bool tieneEsencias = DespertarGrimorio.EsenciasEnManos(p) >= esencias;

                            if (!tieneEsencias)
                            {
                                RechazarOfrenda(p, drop, false, esencias, monedas);
                            }
                            else if (!p.BuyItem(monedas, -1))
                            {
                                // SIN DINERO: el rechazo educado (BuyItem ya
                                // validó SIN cobrar — devolvió false).
                                RechazarOfrenda(p, drop, true, esencias, monedas);
                            }
                            else
                            {
                                DespertarGrimorio.ConsumirEsencias(p, esencias);
                                SubirNivel(p, drop);
                            }
                        }
                        else
                        {
                            // EL BOCADO DE SIEMPRE: el festín de la .129.
                            DevorarComoBocado(p, drop);
                        }
                        break;   // un plato por pulso (con calma, que es un libro)
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  LA SUBIDA DE NIVEL (las tres ofrendas ya están pagas)
        // ================================================================

        /// <summary>SUBE EL DESPERTAR (+1) y celebra: la voz del libro,
        /// el aviso dorado, los polvos, el hito si el nivel desbloquea
        /// algo… y en el 20, LA PELÍCULA DE LA MASCOTA.</summary>
        public static void SubirNivel(Player p, Item ofrenda)
        {
            try
            {
                var sp = p?.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;
                if (sp.NivelDespertar >= DespertarGrimorio.NivelMax) return;

                sp.NivelDespertar++;
                int nivel = sp.NivelDespertar;

                // LA OFRENDA SE LA COME (el item muere en todas las pantallas).
                if (ofrenda != null && !ofrenda.IsAir)
                {
                    ofrenda.TurnToAir();
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null,
                            Array.IndexOf(Main.item, ofrenda));
                }

                // LOS POLVOS DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ && p != null)
                {
                    for (int k = 0; k < 26; k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-3f, 3f),
                                Main.rand.NextFloat(-4f, 0.5f)), 160,
                            new Color(140, 60, 210), 1.1f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.3f), p.Center);
                }

                // EL AVISO DORADO (el patrón de las subidas de nivel).
                if (Main.netMode != NetmodeID.Server && p == Main.LocalPlayer)
                {
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Despertar.Subida", nivel),
                        new Color(196, 138, 255));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.Subida",
                        new Color(196, 138, 255), nivel);
                }

                // LA VOZ DEL LIBRO (los pensamientos del portador).
                EcoRed.HablarVarianteAlPortador(p, "Mods.AethonMod.Despertar.Voz",
                    4, new Color(178, 110, 236), rugido: true, escala: 0.62f);

                // EL HITO (si el nivel desbloquea un poder).
                string hito = DespertarGrimorio.ClaveHito(nivel);
                if (hito != null)
                {
                    if (Main.netMode != NetmodeID.Server && p == Main.LocalPlayer)
                        Main.NewText(Language.GetTextValue(hito), new Color(255, 215, 120));
                    else if (Main.netMode == NetmodeID.Server)
                        EcoRed.AnunciarAlPortador(p, hito, new Color(255, 215, 120));
                }

                // NIVEL 20 — LA PELÍCULA DE LA MASCOTA (una sola vez).
                if (nivel == 20 && !sp.PetDelDespertar)
                {
                    sp.PetDelDespertar = true;
                    EscupirMascota(p);
                }

                // EL NUEVO NIVEL CAMINA (el daño del portador lo lee su
                // máquina — el paquete del festín lleva el despertar).
                EcoRed.SincronizarDevoracion(p);
            }
            catch { }
        }

        /// <summary>EL RECHAZO: falta esencia o dinero — el grimorio
        /// aparta el plato y dice QUÉ falta (el arma queda en el suelo).</summary>
        private static void RechazarOfrenda(Player p, Item drop,
            bool tieneEsencias, int esencias, long monedas)
        {
            try
            {
                // El empujoncito: el plato vuelve hacia el portador (el
                // grimorio lo escupió — no es hora de comer).
                Vector2 empuje = p.Center - drop.Center;
                if (empuje.LengthSquared() > 1f)
                {
                    empuje.Normalize();
                    drop.velocity = -empuje * 3.2f + new Vector2(0f, -2.4f);
                    drop.noGrabDelay = 45;
                }

                long faltanMonedas = Math.Max(0L, monedas - ContarMonedas(p));
                if (Main.netMode != NetmodeID.Server && p == Main.LocalPlayer)
                {
                    string texto = tieneEsencias
                        ? Language.GetTextValue("Mods.AethonMod.Despertar.FaltaDinero",
                            ItemToText(monedas))
                        : Language.GetTextValue("Mods.AethonMod.Despertar.FaltaEsencia",
                            esencias, ItemToText(monedas));
                    Main.NewText(texto, new Color(255, 130, 130));
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    EcoRed.AnunciarAlPortador(p,
                        tieneEsencias ? "Mods.AethonMod.Despertar.FaltaDinero"
                                      : "Mods.AethonMod.Despertar.FaltaEsencia",
                        new Color(255, 130, 130),
                        tieneEsencias ? new object[] { ItemToText(monedas) }
                                      : new object[] { esencias, ItemToText(monedas) });
                }
            }
            catch { }
        }

        /// <summary>CUÁNTO DINERO lleva el jugador (en cobre — las 4
        /// monedas del inventario, contadas a mano: la casa no usa la
        /// banca para las ofrendas del despertar).</summary>
        public static long ContarMonedas(Player p)
        {
            if (p == null) return 0L;
            long total = 0L;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir) continue;
                if (it.type == ItemID.CopperCoin) total += it.stack;
                else if (it.type == ItemID.SilverCoin) total += (long)it.stack * 100;
                else if (it.type == ItemID.GoldCoin) total += (long)it.stack * 10000;
                else if (it.type == ItemID.PlatinumCoin) total += (long)it.stack * 1000000;
            }
            return total;
        }

        /// <summary>Un precio de cobre vuelto TEXTO de monedas (el formato
        /// de la casa: 1 de platino · 3 de oro · 50 de plata · 10 de cobre).</summary>
        public static string ItemToText(long cobre)
        {
            if (cobre <= 0L) return "0";
            long platino = cobre / 1_000_000L;
            long oro = (cobre % 1_000_000L) / 10_000L;
            long plata = (cobre % 10_000L) / 100L;
            long resto = cobre % 100L;
            var partes = new List<string>();
            if (platino > 0) partes.Add($"{platino} de platino");
            if (oro > 0) partes.Add($"{oro} de oro");
            if (plata > 0) partes.Add($"{plata} de plata");
            if (resto > 0) partes.Add($"{resto} de cobre");
            return string.Join(" · ", partes);
        }

        // ================================================================
        //  EL BOCADO DE SIEMPRE (el festín de la .129 — portado del altar)
        // ================================================================

        /// <summary>EL BOCADO: un arma corrupta que NO era la ofrenda se
        /// la come igual — EXP por rareza, UN stat por clase y EL ALMA
        /// ÚNICA si era especial (el festín del cráter de la .129, ahora
        /// servido en la mano del portador).</summary>
        private static void DevorarComoBocado(Player p, Item drop)
        {
            try
            {
                var sp = p.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;

                // EL EXP por rareza (la tabla de la .129).
                sp.DevoracionExp += ExpPorRareza(drop.rare);

                // EL STAT por clase.
                string stat = SubirStatPorClase(sp, drop);

                // EL ALMA ÚNICA (si era especial).
                string alma = DespertarAlma(sp, drop);

                // LA MUERTE DEL PLATO (en todas las pantallas).
                string nombre = drop.Name;
                drop.TurnToAir();
                int idx = Array.IndexOf(Main.item, drop);
                if (Main.netMode == NetmodeID.Server && idx >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);

                // LOS POLVOS Y LA VOZ.
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.2f, 2.2f),
                                Main.rand.NextFloat(-3f, 0.4f)), 150,
                            new Color(110, 40, 170), 0.9f);
                        d.noGravity = true;
                    }
                }
                EcoRed.HablarVarianteAlPortador(p, "Mods.AethonMod.Caida.DevoradoSabor",
                    4, new Color(178, 110, 236), rugido: true, escala: 0.58f);

                // EL AVISO DESGLOSADO (el plato contado).
                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.ArmaDevorada",
                    new Color(196, 138, 255), nombre, ExpPorRareza(drop.rare),
                    Language.GetTextValue($"Mods.AethonMod.Caida.{stat}"));
                if (alma != null)
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.AlmaDespertada",
                        new Color(255, 215, 120), nombre,
                        Language.GetTextValue($"Mods.AethonMod.Caida.{alma}"));

                // EL FESTÍN CAMINA (stats nuevos a la máquina del portador).
                EcoRed.SincronizarDevoracion(p);
            }
            catch { }
        }

        /// <summary>¿Es un ARMA? («un arma de cualquier tipo»): la única
        /// verdad de la corrupción — la leían el altar de la .129 y las
        /// recetas de la .130; ahora la leen las recetas y el comensal.</summary>
        public static bool EsArma(Item it)
        {
            return it != null && !it.IsAir && it.damage > 0 && !it.accessory &&
                !it.vanity && it.pick == 0 && it.axe == 0 && it.hammer == 0 &&
                it.ammo == 0 && it.type !=
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        /// <summary>EL EXP DE UN ARMA por su rareza (la escala de la .129:
        /// la basura alimenta poco, la mítica es un banquete).</summary>
        private static long ExpPorRareza(int rareza)
        {
            switch (rareza)
            {
                case -1: return 5;
                case 0: return 10;
                case 1: return 20;
                case 2: return 45;
                case 3: return 100;
                case 4: return 250;
                case 5: return 600;
                case 6: return 1500;
                case 7: return 4000;
                case 8: return 8000;
                case 9: return 8000;
                default: return rareza >= 10 ? 15000 : 10;
            }
        }

        /// <summary>EL STAT QUE ESTA ARMA SUBE (por clase — la .129).</summary>
        private static string SubirStatPorClase(Players.ShardPlayer sp, Item arma)
        {
            if (arma.DamageType == DamageClass.Melee) { sp.DevoracionFuerza++; return "StatFuerza"; }
            if (arma.DamageType == DamageClass.Ranged) { sp.DevoracionPrisa++; return "StatPrisa"; }
            if (arma.DamageType == DamageClass.Magic) { sp.DevoracionMirada++; return "StatMirada"; }
            if (arma.DamageType == DamageClass.Summon) { sp.DevoracionCorona++; return "StatCorona"; }
            sp.DevoracionMordida++;
            return "StatMordida";
        }

        /// <summary>EL ALMA ÚNICA (las armas especiales de la .129 — el
        /// fuego, el veneno, el vampiro, las estrellas, la velocidad, el
        /// peso: VIVEN en los disparos del Eterno).</summary>
        private static string DespertarAlma(Players.ShardPlayer sp, Item arma)
        {
            int t = arma.type;
            if (t == ItemID.FieryGreatsword || t == ItemID.Sunfury ||
                t == ItemID.Flamelash || t == ItemID.InfernoFork ||
                t == ItemID.Flamethrower || t == ItemID.VolcanoSmall ||
                t == ItemID.VolcanoLarge)
            { sp.AlmaIgnea++; return "AlmaIgnea"; }
            if (t == ItemID.BladeofGrass || t == ItemID.ThornChakram ||
                t == ItemID.BeeGun || t == ItemID.Beenade || t == ItemID.PoisonStaff)
            { sp.AlmaSalvaje++; return "AlmaSalvaje"; }
            if (t == ItemID.Muramasa || t == ItemID.Katana || t == ItemID.FetidBaghnakhs)
            { sp.AlmaVeloz++; return "AlmaVeloz"; }
            if (t == ItemID.Starfury || t == ItemID.StarWrath || t == ItemID.StarCannon)
            { sp.CorazonEstelar++; return "CorazonEstelar"; }
            if (t == ItemID.VampireKnives)
            { sp.AlmaVampira++; return "AlmaVampira"; }
            if (t == ItemID.PaladinsHammer || t == ItemID.GolemFist)
            { sp.PunoTitan++; return "PunoTitan"; }
            return null;
        }

        // ================================================================
        //  NIVEL 20 — LA PELÍCULA DE LA MASCOTA (el escupidizo)
        // ================================================================

        /// <summary>EL GRIMORIO ESCUPE AL MINIGRIMORIO (la letra: «a nivel
        /// 20 el grimorio escupe al minigrimorio con una animación donde
        /// el grimorio sale del jugador, se pone encima de él y en un mar
        /// de brumas escupe al minigrimorio como mascota… esto también
        /// debe revelar algún texto o algo que el grimorio le diga al
        /// jugador»): nace el CINE (EscupidizoProjectile — el mar de
        /// brumas encima del portador) y la mascota queda ENTREGADA (el
        /// item si no lo tenía, el buff si ya lo tenía).</summary>
        public static void EscupirMascota(Player p)
        {
            try
            {
                if (p == null || !p.active) return;

                // LA PELÍCULA (viaja sola a todas las pantallas).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int pr = Projectile.NewProjectile(p.GetSource_Misc("Despertar"),
                        p.Center, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Cosmetic.EscupidizoProjectile>(),
                        0, 0f, p.whoAmI);
                    if (pr >= 0)
                        Main.projectile[pr].timeLeft = 300;
                }

                // LA PALABRA DEL GRIMORIO (el texto de la letra).
                EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.Escupidizo",
                    new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);
            }
            catch { }
        }

        /// <summary>LA MASCOTA YA ESTÁ FUERA: la llama el cine al llegar
        /// su momento (le da el item si no lo tenía; si ya lo tenía, la
        /// invoca AHORA mismo — el escupido es una CEREMONIA).</summary>
        public static void EntregarMascota(Player p)
        {
            try
            {
                if (p == null || !p.active) return;
                int itemMascota = ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.GrimorioMenorItem>();

                bool loTenia = false;
                for (int k = 0; k < 58; k++)
                {
                    Item it = p.inventory[k];
                    if (it != null && !it.IsAir && it.type == itemMascota)
                    { loTenia = true; break; }
                }
                if (!loTenia)
                    p.QuickSpawnItem(p.GetSource_Misc("Despertar"), itemMascota, 1);

                // Y LA INVOCACIÓN YA (el buff + el engendro canónico).
                int buff = ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>();
                p.AddBuff(buff, 18000);
            }
            catch { }
        }

        // ================================================================
        //  EL FILTRO ENTRE LAS REALIDADES (la puerta de los drops de
        //  Esencia Corrupta — la letra: «en el momento en el que el
        //  grimorio hace su aparición en el mundo»)
        // ================================================================

        /// <summary>¿El poder del grimorio YA SE FILTRA en este mundo?
        /// (LA LLEGADA ocurrió aquí: alguien vio su bruma, alguien espera
        /// su libro, o alguien YA carga el Eterno).</summary>
        public static bool ElPoderSeFiltra()
        {
            try
            {
                var caida = ModContent.GetInstance<LaCaidaSistema>();
                if (caida != null && caida.ElPoderViveAqui()) return true;

                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active) continue;
                    for (int k = 0; k < 59; k++)
                    {
                        Item it = p.inventory[k];
                        if (it != null && !it.IsAir && it.type == eterno) return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }
    }
}
