using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.51.6 — EL ALMA DEL ETERNO ES DEL LIBRO, NO DEL PORTADOR (la
    //  letra del usuario: «un problema que vea es que el despertar se
    //  comparte entre todos los grimorios, eso debe ser algo individual
    //  de cada grimorio del eterno, al igual que las estadisticas y su
    //  nivel, todos son caracteristicas exclusivas del grimorio del
    //  eterno»).
    //
    //  LO QUE VIVE AQUÍ (TODO per-ITEM, el patrón probado de
    //  ShardLevelItem — InstancePerEntity + SaveData/LoadData en el
    //  item + NetSend/NetReceive por el cable):
    //    · EL DESPERTAR (0..50) y su MASCOTA DEL NIVEL 20.
    //    · EL PODER DEVORADO (las esencias corruptas: +0,1% por esencia).
    //    · EL FESTÍN DE ARMAS (el EXP por rareza, los 5 stats por clase
    //      y las 6 almas únicas).
    //
    //  Cada Grimorio del Eterno es un libro con SU hambre, SU escalera
    //  y SU despertar: dos libros en la misma bolsa progresan SEPARADOS.
    //
    //  LA MIGRACIÓN (una sola vez): los guardados de la .51.0–.51.5
    //  traían todo esto en el JUGADOR (ShardPlayer) — al entrar al
    //  mundo, lo viejo camina AL PRIMER GRIMORIO del inventario y el
    //  jugador queda limpio (ShardPlayer.AlmaLegacyPendiente).
    // ======================================================================
    public class AlmaEternoGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        // === EL DESPERTAR ===
        /// <summary>El nivel de despertar de ESTE libro (0..50 — la letra:
        /// «siempre empieza desde nivel 0»).</summary>
        public int NivelDespertar = 0;

        /// <summary>¿ESTE libro ya escupió a su mascota (nivel 20)? La
        /// ceremonia no se repite por libro.</summary>
        public bool PetDelDespertar = false;

        // === EL PODER DEVORADO (las esencias corruptas) ===
        /// <summary>Cada esencia corrupta tragada: +0,1 % de daño
        /// permanente (sin tope práctico).</summary>
        public int DevoracionPoder = 0;

        // === EL FESTÍN DE ARMAS ===
        /// <summary>EL EXP por rareza de las armas comidas.</summary>
        public long DevoracionExp = 0;

        /// <summary>LA FUERZA (armas melee: +0,35 % daño/punto).</summary>
        public int DevoracionFuerza = 0;

        /// <summary>LA PRISA (armas a distancia: +0,25 % velocidad/punto).</summary>
        public int DevoracionPrisa = 0;

        /// <summary>LA MIRADA (armas mágicas: +0,30 % crítico/punto).</summary>
        public int DevoracionMirada = 0;

        /// <summary>LA CORONA (armas de invocación: 1 ojo más cada 5).</summary>
        public int DevoracionCorona = 0;

        /// <summary>LA MORDIDA (las otras: +0,6 % empuje/punto).</summary>
        public int DevoracionMordida = 0;

        // === LAS ALMAS ÚNICAS (las armas especiales) ===
        public int AlmaIgnea = 0;        // +6 % prender/copia (tope 30 %)
        public int AlmaSalvaje = 0;      // +6 % envenenar/copia (tope 30 %)
        public int AlmaVampira = 0;      // +2 % robo de vida/copia (tope 8 %)
        public int CorazonEstelar = 0;   // +1,5 % crítico/copia (tope +7,5 %)
        public int AlmaVeloz = 0;        // +2 % velocidad/copia (tope +10 %)
        public int PunoTitan = 0;        // +12 % empuje/copia (tope +60 %)

        // ================================================================
        //  EL REGISTRO (solo el Grimorio del Eterno)
        // ================================================================
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            try
            {
                return item.type == ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
            }
            catch { return false; }
        }

        // ================================================================
        //  v6.51.9 — EL PARÉNTESIS NACE CON EL LIBRO (la letra: «cuando
        //  agarre al grimorio dormido al llegar al inventario decia
        //  Grimorio del Eterno (Agil)»): dos curas de raíz —
        //  · EL LIBRO NUEVO ya viste su etiqueta de escalera al nacer al
        //    mundo («(Común)» — la peor: así empieza; el nombre la lleva
        //    puesta desde el primer instante, en el chat de recogida y en
        //    todas partes — GrimoireEternal la mantiene fresca por nivel).
        //  · EL YUNQUE SE LE NIEGA: el libro no se reforja — su paréntesis
        //    es SU escalera, no la lotería de vanilla (los prefijos
        //    naturales ya los mata GrimoireEternal.PrefixChance).
        // ================================================================

        public override void OnSpawn(Item item, Terraria.DataStructures.IEntitySource source)
        {
            try
            {
                if (item == null || item.IsAir) return;
                // El nivel REAL del item clonado (si venía de un clon con
                // sus globals) o el 1 del recién nacido.
                var sl = item.GetGlobalItem<ShardLevelItem>();
                int nivel = sl?.Level ?? 1;
                int idx = global::AethonMod.Content.Weapons.GrimoireEternal
                    .IndiceEtiqueta(nivel);
                string nombreBase = Terraria.Lang.GetItemNameValue(item.type);
                item.SetNameOverride(nombreBase + " (" +
                    global::AethonMod.Content.Weapons.GrimoireEternal.NombreEtiqueta(idx) + ")");
            }
            catch { }
        }

        // ================================================================
        //  v6.51.10 — LA ESCENA DEL YUNQUE (la letra del usuario: «El
        //  Duende Chapucero no puede reforjar el grimorio, cuando
        //  intentas darselo el debe tener un dialogo en donde se asusta
        //  y no quiere ni tocarlo, le da miedo a un nivel
        //  indescriptible y el grimorio del eterno se burla de ello en
        //  ese momento y lo menosprecia jajajaja»): el click en
        //  REFORJAR con el Eterno en el yunque dispara EL PÁNICO — la
        //  línea de chat del duende cambia AHORA (Miedo1..4) y el libro
        //  SUENA A RISA (LaBurlaDelEterno). El Eterno NO se reforja
        //  JAMÁS: su paréntesis es SU REFORJA VIVA (la escalera de los
        //  modificadores mágicos cada 33 niveles — GrimoireEternal
        //  .LaReforja), no la lotería del yunque.
        // ================================================================
        public override bool CanReforge(Item item)
        {
            try
            {
                if (Main.InReforgeMenu && !Main.dedServ && Main.netMode != 2)
                {
                    // EL DUENDE SE ASUSTA (su línea de la ventana de
                    // diálogo cambia en el acto: no quiere NI tocarlo).
                    Main.npcChatText = Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Duende.Miedo" + Main.rand.Next(1, 5));
                    // Y EL ETERNO SE BURLA DE ÉL (en ese momento — lo menosprecia).
                    PuebloChismesNPC.LaBurlaDelEterno();
                }
            }
            catch { }
            return false;
        }

        // ================================================================
        //  EL GRIMORIO ACTIVO (el que manda: el de la MANO primero — si
        //  no, el primero del inventario). Los poderes, los ojos y el
        //  despertar leen SIEMPRE el libro ACTIVO del portador.
        // ================================================================
        public static Item Activo(Player p)
        {
            if (p == null || !p.active) return null;
            int eterno = ModContent.ItemType
                <global::AethonMod.Content.Weapons.GrimoireEternal>();
            Item mano = p.HeldItem;
            if (mano != null && !mano.IsAir && mano.type == eterno) return mano;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == eterno) return it;
            }
            return null;
        }

        /// <summary>El alma de ESTE item (o null si no es un Eterno).</summary>
        public static AlmaEternoGlobalItem De(Item item)
        {
            try
            {
                if (item == null || item.IsAir) return null;
                if (item.type != ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>())
                    return null;
                return item.GetGlobalItem<AlmaEternoGlobalItem>();
            }
            catch { return null; }
        }

        /// <summary>El nivel de despertar de ESTE item (0 si no es Eterno).</summary>
        public static int NivelDe(Item item)
        {
            var a = De(item);
            if (a == null) return 0;
            return System.Math.Clamp(a.NivelDespertar, 0,
                Content.Systems.DespertarGrimorio.NivelMax);
        }

        // ================================================================
        //  LOS BONUS DE ESTE LIBRO (los mismos números de siempre — ahora
        //  leídos del ITEM, no del jugador)
        // ================================================================

        /// <summary>EL BONUS DE DAÑO por las esencias devoradas (0,1 %
        /// por esencia, tope suave 500 %).</summary>
        public static float BonusDanoDevorado(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f : System.Math.Min(a.DevoracionPoder, 5000) * 0.001f;
        }

        /// <summary>EL DAÑO DEL FESTÍN DE ARMAS: fuerza + EXP (tope +45 %).</summary>
        public static float BonusDanoArmasDevoradas(Item eterno)
        {
            var a = De(eterno);
            if (a == null) return 0f;
            return System.Math.Min(a.DevoracionFuerza * 0.0035f +
                System.Math.Min(a.DevoracionExp * 0.0002f, 0.40f), 0.45f);
        }

        /// <summary>LA VELOCIDAD DEL FESTÍN: prisa + alma veloz (tope +35 %).</summary>
        public static float BonusVelocidadArmasDevoradas(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f
                : System.Math.Min(a.DevoracionPrisa * 0.0025f + a.AlmaVeloz * 0.02f, 0.35f);
        }

        /// <summary>EL CRÍTICO DEL FESTÍN: mirada + corazón estelar (tope +30 %).</summary>
        public static float BonusCritArmasDevoradas(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f
                : System.Math.Min(a.DevoracionMirada * 0.0030f + a.CorazonEstelar * 0.015f, 0.30f);
        }

        /// <summary>EL EMPUJE DEL FESTÍN: mordida + puño titán (tope +75 %).</summary>
        public static float BonusEmpujeArmasDevoradas(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f
                : System.Math.Min(a.DevoracionMordida * 0.006f + a.PunoTitan * 0.12f, 0.75f);
        }

        /// <summary>LOS OJOS DE LA CORONA del festín (1 por cada 5 armas de
        /// invocación comidas, tope +3).</summary>
        public static int OjosCoronaDevorados(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0 : System.Math.Min(a.DevoracionCorona / 5, 3);
        }

        /// <summary>LOS OJOS EXTRA por el poder devorado (+1 cada 75, tope +3).</summary>
        public static int OjosExtraDevorados(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0 : System.Math.Min(a.DevoracionPoder / 75, 3);
        }

        /// <summary>EL ROBO DE VIDA del alma vampírica (+2 %/copia, tope 8 %).</summary>
        public static float RoboVidaAlmas(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f : System.Math.Min(a.AlmaVampira * 0.02f, 0.08f);
        }

        /// <summary>LA PROBABILIDAD DE PRENDER del alma ígnea (+6 %/copia, tope 30 %).</summary>
        public static float ProbIgnea(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f : System.Math.Min(a.AlmaIgnea * 0.06f, 0.30f);
        }

        /// <summary>LA PROBABILIDAD DE ENVENENAR del alma salvaje (+6 %/copia, tope 30 %).</summary>
        public static float ProbSalvaje(Item eterno)
        {
            var a = De(eterno);
            return a == null ? 0f : System.Math.Min(a.AlmaSalvaje * 0.06f, 0.30f);
        }

        // ================================================================
        //  LA PERSISTENCIA (por ITEM — el .plr guarda el item con SU alma)
        // ================================================================
        public override void SaveData(Item item, TagCompound tag)
        {
            // ARMADURA (el patrón de ShardLevelItem.SaveData): ItemIO
            // llama sin try/catch y corre en la cadena del guardado.
            try
            {
                if (NivelDespertar > 0) tag["almaDespertar"] = NivelDespertar;
                if (PetDelDespertar) tag["almaPet"] = true;
                if (DevoracionPoder > 0) tag["almaPoder"] = DevoracionPoder;
                if (DevoracionExp > 0) tag["almaExp"] = DevoracionExp;
                if (DevoracionFuerza > 0) tag["almaFuerza"] = DevoracionFuerza;
                if (DevoracionPrisa > 0) tag["almaPrisa"] = DevoracionPrisa;
                if (DevoracionMirada > 0) tag["almaMirada"] = DevoracionMirada;
                if (DevoracionCorona > 0) tag["almaCorona"] = DevoracionCorona;
                if (DevoracionMordida > 0) tag["almaMordida"] = DevoracionMordida;
                if (AlmaIgnea > 0) tag["almaIgnea"] = AlmaIgnea;
                if (AlmaSalvaje > 0) tag["almaSalvaje"] = AlmaSalvaje;
                if (AlmaVampira > 0) tag["almaVampira"] = AlmaVampira;
                if (CorazonEstelar > 0) tag["almaEstelar"] = CorazonEstelar;
                if (AlmaVeloz > 0) tag["almaVeloz"] = AlmaVeloz;
                if (PunoTitan > 0) tag["almaPuno"] = PunoTitan;
            }
            catch { }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try
            {
                NivelDespertar = System.Math.Max(0, System.Math.Min(
                    Content.Systems.DespertarGrimorio.NivelMax,
                    tag.GetInt("almaDespertar")));
                PetDelDespertar = tag.GetBool("almaPet");
                DevoracionPoder = System.Math.Max(0, System.Math.Min(999999, tag.GetInt("almaPoder")));
                DevoracionExp = System.Math.Max(0L, System.Math.Min(999999999L, tag.GetLong("almaExp")));
                DevoracionFuerza = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaFuerza")));
                DevoracionPrisa = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaPrisa")));
                DevoracionMirada = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaMirada")));
                DevoracionCorona = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaCorona")));
                DevoracionMordida = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaMordida")));
                AlmaIgnea = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaIgnea")));
                AlmaSalvaje = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaSalvaje")));
                AlmaVampira = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaVampira")));
                CorazonEstelar = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaEstelar")));
                AlmaVeloz = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaVeloz")));
                PunoTitan = System.Math.Max(0, System.Math.Min(99, tag.GetInt("almaPuno")));
            }
            catch
            {
                NivelDespertar = 0;
                PetDelDespertar = false;
                DevoracionPoder = 0;
                DevoracionExp = 0;
                DevoracionFuerza = 0;
                DevoracionPrisa = 0;
                DevoracionMirada = 0;
                DevoracionCorona = 0;
                DevoracionMordida = 0;
                AlmaIgnea = 0;
                AlmaSalvaje = 0;
                AlmaVampira = 0;
                CorazonEstelar = 0;
                AlmaVeloz = 0;
                PunoTitan = 0;
            }
        }

        // ================================================================
        //  EL CABLE (MP: el alma viaja con el item — los inventarios y el
        //  suelo sincronizan el item con SUS datos)
        // ================================================================
        public override void NetSend(Item item, BinaryWriter writer)
        {
            try
            {
                writer.Write((byte)1);                    // versión del alma
                writer.Write(NivelDespertar);
                writer.Write(PetDelDespertar);
                writer.Write(DevoracionPoder);
                writer.Write(DevoracionExp);
                writer.Write(DevoracionFuerza);
                writer.Write(DevoracionPrisa);
                writer.Write(DevoracionMirada);
                writer.Write(DevoracionCorona);
                writer.Write(DevoracionMordida);
                writer.Write(AlmaIgnea);
                writer.Write(AlmaSalvaje);
                writer.Write(AlmaVampira);
                writer.Write(CorazonEstelar);
                writer.Write(AlmaVeloz);
                writer.Write(PunoTitan);
            }
            catch { writer.Write((byte)0); }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try
            {
                byte ver = reader.ReadByte();
                if (ver < 1) return;
                NivelDespertar = System.Math.Max(0, System.Math.Min(
                    Content.Systems.DespertarGrimorio.NivelMax, reader.ReadInt32()));
                PetDelDespertar = reader.ReadBoolean();
                DevoracionPoder = System.Math.Max(0, reader.ReadInt32());
                DevoracionExp = System.Math.Max(0L, reader.ReadInt64());
                DevoracionFuerza = System.Math.Max(0, reader.ReadInt32());
                DevoracionPrisa = System.Math.Max(0, reader.ReadInt32());
                DevoracionMirada = System.Math.Max(0, reader.ReadInt32());
                DevoracionCorona = System.Math.Max(0, reader.ReadInt32());
                DevoracionMordida = System.Math.Max(0, reader.ReadInt32());
                AlmaIgnea = System.Math.Max(0, reader.ReadInt32());
                AlmaSalvaje = System.Math.Max(0, reader.ReadInt32());
                AlmaVampira = System.Math.Max(0, reader.ReadInt32());
                CorazonEstelar = System.Math.Max(0, reader.ReadInt32());
                AlmaVeloz = System.Math.Max(0, reader.ReadInt32());
                PunoTitan = System.Math.Max(0, reader.ReadInt32());
            }
            catch { }
        }
    }
}
