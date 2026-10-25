using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.VFX;

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

        // === v6.51.11 — LA MESA DEL DESPERTAR (los contadores PARCIALES
        //     de la ofrenda del peldaño actual: la comida se sirve EN EL
        //     SUELO — el libro come SOLO lo que le falta; la letra:
        //     «necesita 70 lingotes de oro, y suelto 30… se come los 30…
        //     ahora necesita 40»). Persisten en el guardado y viajan por
        //     el cable: la lista del despertar los lee VIVA. ===

        /// <summary>Las esencias corruptas ya comidas de ESTE peldaño.</summary>
        public int OfrendaEsencias = 0;

        /// <summary>El dinero ya comido de ESTE peldaño (en cobre).</summary>
        public long OfrendaMonedas = 0L;

        /// <summary>Los lingotes ya comidos de ESTE peldaño (la ofrenda
        /// mineral — la alternativa de los peldaños de armas y LA
        /// ofrenda de la cima 45-50).</summary>
        public int OfrendaMineral = 0;

        /// <summary>¿Ya devoró EL ARMA CORRUPTA de ESTE peldaño? (con el
        /// arma O los lingotes basta — la regla de siempre).</summary>
        public bool OfrendaArma = false;

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
        //  v6.51.12 — LA PRESENCIA DEL LIBRO (la letra del usuario: «el
        //  daño aumentando por el festin… debe de tambien estar ligado
        //  al grimorio del eterno, aunque sea el jugador quien se coma
        //  1000 esencias corruptas solo con el grimorio se activa ese
        //  poder y es lo mismo con todas las stats que da el grimorio,
        //  solo mientras se use el grimorio o este en el inventario se
        //  activan para el jugador, si esta seleccionado el jugador
        //  tiene todo el poder del grimorio, si no esta seleccionado o
        //  esta guardado en el inventario el jugador solo obtiene 25%
        //  del poder del grimorio, si el grimorio esta guardado en
        //  cualquier tipo de cofre o este fue botado a la basura
        //  entonces el jugador no obtiene nada del poder del grimorio»).
        //  LA ÚNICA VERDAD de la presencia: EN LA MANO = 1,0 · EN EL
        //  INVENTARIO (cualquier slot, o al cursor) = 0,25 · NADA
        //  (cofre, hucha, vaulta, basura, suelo) = 0. TODAS las stats
        //  que el libro da al portador multiplican por aquí.
        // ================================================================

        /// <summary>EL FACTOR DE PRESENCIA del libro: 1,0 seleccionado ·
        /// 0,25 en el inventario · 0,0 en ningún sitio que cuente.</summary>
        public static float FactorDelLibro(Player p)
        {
            if (p == null || !p.active) return 0f;
            int eterno = ModContent.ItemType
                <global::AethonMod.Content.Weapons.GrimoireEternal>();

            // 1) EN LA MANO (seleccionado): TODO el poder.
            Item mano = p.HeldItem;
            if (mano != null && !mano.IsAir && mano.type == eterno) return 1f;

            // 2) AL CURSOR (moverlo entre slots también es "tenerlo"):
            //    solo el libro del jugador LOCAL viaja por SU cursor.
            if (p == Main.LocalPlayer && Main.mouseItem != null &&
                !Main.mouseItem.IsAir && Main.mouseItem.type == eterno)
                return 0.25f;

            // 3) EN EL INVENTARIO (hotbar o bolsa profunda): 25 %.
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == eterno) return 0.25f;
            }

            // 4) COFRE · HUCHA · VAULTA · BASURA · SUELO: NADA.
            return 0f;
        }

        // ================================================================
        //  v6.51.12 — EL LAZO DE LA SOMBRA DE LA PÁGINA (la letra: «algo
        //  importante, el grimorio no se puede dejar caer tan facilmente,
        //  si el jugador deja caer el grimorio y se aleja a mas de 20px
        //  el grimorio usa la sombra de la pagina para pegarse al
        //  jugador y volver a entrar a su inventario, si el inventario
        //  esta lleno el grimorio deja caer un item aleatorio y entra
        //  el grimorio»). El Eterno caído al suelo JAMÁS se queda: al
        //  20 px de su portador la SOMBRA SE ESTIRA y lo ARRASTRA de
        //  vuelta — y si la bolsa está llena, expulsa un item al azar
        //  y entra él. (En el cofre y en la basura sí se queda: ahí el
        //  poder apaga — la letra lo permite a propósito.)
        // ================================================================

        /// <summary>El umbral de la letra: a 20 px la sombra ya agarra.</summary>
        private const float LazoUmbral = 20f;

        /// <summary>La velocidad del arrastre (px/t del vuelo de vuelta).</summary>
        private const float LazoVelocidad = 15f;

        /// <summary>v6.52.2 — LA PACIENCIA DE LA SOMBRA (la letra: «esto
        /// sucede muy rapido, deberia demorar al menos 5 segundos para
        /// que suceda»): el libro caído DESCANSA en el suelo CINCO
        /// SEGUNDOS enteros antes de que la sombra se estire — el
        /// jugador puede verlo ahí, tocarlo, recogerlo a mano; solo tras
        /// la paciencia la página vuelve a por él.</summary>
        private const int LazoPaciencia = 300;   // 5 s × 60 t

        public override void PostUpdate(Item item)
        {
            try
            {
                if (item == null || item.IsAir || Main.gameMenu) return;

                // v6.52.2 — LA PACIENCIA: el reloj nace con el item al
                // caer; mientras no cumpla los 5 segundos, la sombra NO
                // se estira (el libro yace — la letra cumplida).
                if (item.timeSinceItemSpawned < LazoPaciencia) return;

                // EL PORTADOR MÁS CERCANO (el que lo soltó, en la
                // práctica): el libro se pega a QUIEN lo tenga encima.
                Player cercano = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player pl = Main.player[i];
                    if (pl == null || !pl.active || pl.dead) continue;
                    float d = Vector2.DistanceSquared(pl.Center, item.Center);
                    if (d < mejor) { mejor = d; cercano = pl; }
                }
                if (cercano == null) return;

                float dist = System.MathF.Sqrt(mejor);

                // (NI EL SHIMMER LO SUELTA: la sombra lo pesca hasta de
                // la piscina — el libro no sabe nadar, sabe volver.)
                if (dist <= LazoUmbral)
                {
                    // AÚN A LOS PIES (≤20 px): espera quieta — la sombra
                    // ya la sostiene (frena el rebote del drop).
                    item.velocity *= 0.90f;
                    return;
                }

                // > 20 PX: LA SOMBRA SE ESTIRA Y LO ARRASTRA (la velocidad
                // se REESCRIBE cada tick: la gravedad no opina).
                Vector2 hacia = cercano.Center - item.Center;
                if (hacia.LengthSquared() > 0.01f)
                {
                    hacia.Normalize();
                    item.velocity = Vector2.Lerp(item.velocity, hacia * LazoVelocidad, 0.28f);
                }

                // LA LLEGADA: entra al inventario (con expulsion si está lleno).
                if (dist < 36f || item.Hitbox.Intersects(cercano.Hitbox))
                    DevolverAlInventario(cercano, item);
            }
            catch { }
        }

        /// <summary>LA VUELTA A CASA: el libro ENTRA al inventario del
        /// portador — y si la bolsa está LLENA, deja caer un item
        /// aleatorio de la bolsa profunda y toma su sitio (la letra).</summary>
        private static void DevolverAlInventario(Player p, Item item)
        {
            // 1) Un slot libre (primero el hotbar — que esté a mano).
            int libre = -1;
            for (int k = 0; k < 50; k++)
            {
                if (p.inventory[k] == null || p.inventory[k].IsAir) { libre = k; break; }
            }

            // 2) ¿BOLSA LLENA? EL LIBRO EXPULSA UN ITEM AL AZAR (de la
            //    bolsa profunda 10..49 — jamás del hotbar ni de las
            //    monedas) y toma su lugar (la letra: «deja caer un item
            //    aleatorio y entra el grimorio»).
            if (libre < 0)
            {
                libre = 10 + Main.rand.Next(40);
                Item sacado = p.inventory[libre].Clone();
                // el expulsado cae al mundo a los pies del portador
                int caido = Item.NewItem(p.GetSource_Misc("LazoDelEterno"),
                    p.Center, sacado, noBroadcast: false);
                if (Main.netMode == NetmodeID.Server && caido >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, caido);
            }

            // 3) EL LIBRO ENTRA (con SU alma — el clon lleva los globals).
            p.inventory[libre] = item.Clone();

            // 4) El item del mundo MUERE (en todas las pantallas).
            int idx = System.Array.IndexOf(Main.item, item);
            item.TurnToAir();
            if (idx >= 0 && Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);

            // 5) EL TEATRO DE LA VUELTA: chispas de sombra + el trago.
            if (!Main.dedServ)
            {
                for (int k = 0; k < 16; k++)
                {
                    Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                        new Vector2(Main.rand.NextFloat(-2.4f, 2.4f),
                            Main.rand.NextFloat(-3.4f, 0.4f)), 150,
                        new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item113.WithPitchOffset(-0.35f), p.Center);
            }

            // 6) EL AVISO (el libro nunca se despide en silencio).
            if (p.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Despertar.LazoDevuelto"),
                    new Color(196, 138, 255));
            }
        }

        // ================================================================
        //  v6.51.12 — EL DIBUJO DEL LAZO: mientras el libro caído vuelve
        //  (o espera a los pies), LA SOMBRA DE LA PÁGINA lo tiene
        //  agarrado — un tentáculo fino de tinta viva desde el portador
        //  hasta la tapa, con su bruma y sus ojos entornados.
        // ================================================================
        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch,
            Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            try
            {
                if (Main.gameMenu) return true;
                // ¿Hay portador a la vista con el lazo activo (>0 px)?
                Player cercano = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player pl = Main.player[i];
                    if (pl == null || !pl.active || pl.dead) continue;
                    float d = Vector2.DistanceSquared(pl.Center, item.Center);
                    if (d < mejor) { mejor = d; cercano = pl; }
                }
                if (cercano == null) return true;
                float dist = System.MathF.Sqrt(mejor);
                if (dist <= 0.5f || dist > 1200f) return true;  // muy lejos: el lazo no llega (y no se ve)

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    float tiempo = Main.GlobalTimeWrappedHourly;
                    int semilla = 9001 + (int)(dist * 0.5f);
                    Vector2 raiz = cercano.MountedCenter;

                    // LA SOMBRA FINA: columna viva del portador al libro
                    Vector2[] col = SombrasLib.ColumnaViva(raiz, item.Center,
                        tiempo, semilla, 10, 0.22f, gancho: 0.4f);
                    SombrasLib.Masa(col, 3f, 6f, 0.55f, semilla, tiempo, grosorCentro: 12f);
                    SombrasLib.BrumaColumna(col, 18f, 0.30f, tiempo, semilla, 7);

                    // LOS OJOS ENTORNADOS a lo largo del lazo (me vigilan volver)
                    SombrasLib.OjosDeMasa(col, item.Center, 0.5f, semilla, tiempo, 4);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }
            catch { }
            return true;   // el sprite del libro lo dibuja vanilla
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
        /// por esencia, tope suave 500 %). v6.51.11 — MUERTO COMO BONUS
        /// DEL LIBRO (la letra: «las esencias consumidas por el grimorio
        /// no cuentan para el aumento general de daño, solo cuentan
        /// para su despertar»): el campo vive solo para la MIGRACIÓN de
        /// los libros viejos (DespertarGrimorioSistema.MigrarViejoPoder
        /// lo pasa al portador y lo deja en 0 para siempre) — este
        /// cálculo ya no lo lee nadie.</summary>
        public static float BonusDanoDevorado(Item eterno)
        {
            return 0f;
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

        /// <summary>LOS OJOS EXTRA por el poder devorado (+1 cada 75, tope
        /// +3). v6.51.11 — MUERTO con el poder devorado (la letra: lo que
        /// el libro come SOLO despierta): el método queda para la forma —
        /// ya no lo lee nadie.</summary>
        public static int OjosExtraDevorados(Item eterno)
        {
            return 0;
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
                // v6.51.11 — LA MESA DEL DESPERTAR (los contadores
                // parciales del peldaño vivo: la comida se sirve en el
                // suelo y el libro come solo lo que le falta).
                if (OfrendaEsencias > 0) tag["almaOfrendaEsencias"] = OfrendaEsencias;
                if (OfrendaMonedas > 0L) tag["almaOfrendaMonedas"] = OfrendaMonedas;
                if (OfrendaMineral > 0) tag["almaOfrendaMineral"] = OfrendaMineral;
                if (OfrendaArma) tag["almaOfrendaArma"] = true;
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
                // v6.51.11 — LA MESA DEL DESPERTAR (contadores parciales).
                OfrendaEsencias = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaOfrendaEsencias")));
                OfrendaMonedas = System.Math.Max(0L, System.Math.Min(9_999_999L, tag.GetLong("almaOfrendaMonedas")));
                OfrendaMineral = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaOfrendaMineral")));
                OfrendaArma = tag.GetBool("almaOfrendaArma");
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
                OfrendaEsencias = 0;
                OfrendaMonedas = 0L;
                OfrendaMineral = 0;
                OfrendaArma = false;
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
                writer.Write((byte)2);                    // versión del alma (v6.51.11: la mesa)
                writer.Write(NivelDespertar);
                writer.Write(PetDelDespertar);
                writer.Write(DevoracionPoder);
                // v6.51.11 — LA MESA DEL DESPERTAR (contadores parciales
                // del peldaño vivo — los tooltips del portador los leen).
                writer.Write(OfrendaEsencias);
                writer.Write(OfrendaMonedas);
                writer.Write(OfrendaMineral);
                writer.Write(OfrendaArma);
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
                if (ver >= 2)
                {
                    // v6.51.11 — LA MESA DEL DESPERTAR.
                    OfrendaEsencias = System.Math.Max(0, reader.ReadInt32());
                    OfrendaMonedas = System.Math.Max(0L, reader.ReadInt64());
                    OfrendaMineral = System.Math.Max(0, reader.ReadInt32());
                    OfrendaArma = reader.ReadBoolean();
                }
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
