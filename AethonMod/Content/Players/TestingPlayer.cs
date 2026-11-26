using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Items.Bolsas;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.56.1 — EL MODELO ACTUAL (la purga v6.55.0 retiró la investigación
    /// y su bolsa; v6.56.0 subió el arsenal): OnEnterWorld (SP + jugador
    /// local) entrega:
    ///
    ///   1. EL COFRE DEL ARSENAL (la Bolsa Maestra: dentro van las
    ///      VEINTICUATRO bolsas de categoría — la de las alas incluida
    ///      desde la v6.55.0; cada una con semántica de garantía: clic
    ///      derecho y solo entrega lo que falte),
    ///   2. LA BOLSA DE LAS ALAS aparte (las veintidós alas procedurales),
    ///   3. LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (la escalera del rayo:
    ///      Alba/Veredicto/Juicio/Prisma/RC2 · las cuatro sentencias · y
    ///      las tres del grimorio v6.56.0: Índice/Pluma/Ojo),
    ///   4. LA LLAVE DEL DESPERTAR (EcoDelDespertar, el probador de la
    ///      progresión — va suelta) y
    ///   5. 99 DUMMIES DE PRUEBA (Target Dummy de vanilla).
    ///
    /// Histórico: v5.98 kit congelado + 40+ EnsureItem; v6.27 UNA bolsa
    /// (ArsenalBag); v6.29 una por categoría; v6.55.0 el cofre maestro.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            // ==============================================================
            //  v6.166 — LAS 9999 DE PRUEBA (la letra: «nunca le diste al
            //  jugador las 9999 esencias del eterno»): OnWorldLoad de
            //  EsenciasModSistema corría ANTES de que existiera jugador
            //  alguno — pagaba a NADIE. AQUÍ el jugador YA está en el
            //  mundo: top-up a 9999 Esencias del Eterno (cuenta lo que
            //  ya lleva y completa lo que falta). ES PRUEBA: en el mod
            //  final muere y las esencias se farmean (tasa de caída ×2
            //  tras el Muro de Carne).
            // ==============================================================
            try
            {
                int tipoEsencia = ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                int lleva = 0;
                for (int k = 0; k < 58; k++)
                {
                    Item it = Player.inventory[k];
                    if (it != null && !it.IsAir && it.type == tipoEsencia)
                        lleva += it.stack;
                }
                // v6.170 — EL CLICKER DEL UMBRAL MURIÓ (la letra: «el
                // arma que creaste el clicker umbral esta horrible, lo
                // puedes borrar»): la entrega de pruebas vuelve a ser
                // SOLO las esencias.

                int falta = 9999 - lleva;
                if (falta > 0)
                {
                    Player.QuickSpawnItem(Player.GetSource_FromThis(), tipoEsencia, falta);
                    if (Player.whoAmI == Main.myPlayer)
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Esencia.PruebaEsencias"),
                            new Microsoft.Xna.Framework.Color(196, 150, 255));
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); }

            // ==============================================================
            //  v6.175 — EL FARO DEL ECLIPSE, DIRECTO A LA MANO (la letra:
            //  «creaste un arma nueva, no olvides que debes darsela al
            //  jugador siempre de forma directa, ya que no pude probar
            //  esta arma»): el Faro nace en el inventario del portador
            //  en cuanto entra al mundo — SIEMPRE que no lo lleve (una
            //  por jugador; la receta de 5 maderas sigue de alternativa).
            // ==============================================================
            try
            {
                int tipoFaro = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Investigaciones.FaroDelEclipse>();
                bool llevaFaro = false;
                for (int k = 0; k < 58; k++)
                {
                    Item it = Player.inventory[k];
                    if (it != null && !it.IsAir && it.type == tipoFaro) { llevaFaro = true; break; }
                }
                if (!llevaFaro)
                {
                    Player.QuickSpawnItem(Player.GetSource_FromThis(), tipoFaro, 1);
                    if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Ira.FaroEntregado"),
                            new Microsoft.Xna.Framework.Color(255, 200, 120));
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); }


            // v6.50.10 — ARMADURA: PlayerLoader.OnEnterWorld NO envuelve el
            // hook en try/catch (IL-verificado contra el tML 2026.07.3.0 —
            // a diferencia de SaveData/LoadData) y este cuerpo muta el
            // INVENTARIO en cada entrada: una excepción aquí abortaría la
            // entrada al mundo. La armadura es gratis (mismo patrón que
            // SaveData/NetSend del ShardLevelItem).
            try
            {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === v6.55.0 — LA BOLSA DE LA INVESTIGACIÓN RETIRADA (la
            //     letra: «borra la bolsa de investigación con todas sus
            //     armas que ninguna sirve, deja la bolsa de definitiva y
            //     la bolsa de alas»): el kit entrega ahora —
            //       1. EL COFRE DEL ARSENAL (la Bolsa Maestra con TODAS
            //          las bolsas — incluida la Bolsa de las Alas desde
            //          esta versión),
            //       2. LA BOLSA DE LAS ALAS aparte (las quince alas
            //          procedurales — antes vivía dentro de la
            //          investigación retirada), y
            //       3. LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (las cinco
            //          sentencias + EL RAYO CONCENTRADO II). ===
            int bolsas = 0;
            bolsas += Entregar(ModContent.ItemType<BolsaMaestra>());
            bolsas += Entregar(ModContent.ItemType<BolsaDeAlasPrisma>());
            // v6.54.6 — LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (la
            // reforma: «El Veredicto de Aethon», las cinco sentencias
            // con sus siete capas + el rayo concentrado)
            bolsas += Entregar(ModContent.ItemType<BolsaDefinitivas>());
            // v6.85 — LA BOLSA DE LAS INVESTIGACIONES aparte (la letra de la
            // sesión: «todos los items creados con estas investigaciones
            // ponlos en una nueva bolsa y daselas al jugador» — también en
            // la entrega DIRECTA del kit, no solo dentro del cofre:
            // la lección v6.14.2 aplicada a la bolsa nueva).
            bolsas += Entregar(ModContent.ItemType<BolsaInvestigaciones>());
            // v6.104 — LA BOLSA DE LAS AURAS (la letra: «ademas todas
            // estas cosas nuevas ponlas en una nueva bolsa, esto sera
            // material de investigacion de auras»): el kit de estudio
            // de auras — el Aura de Juicio, el Estandarte y las Cinco
            // Formas — viaja también con el kit del probador.
            bolsas += Entregar(ModContent.ItemType<BolsaAuras>());
            // v6.100 — LA BOLSA DEL TALLER Y LA DE LAS TRES CASAS
            // MURIERON con sus armas (la letra: las armas creadas con
            // la herramienta — las de la bolsa del taller y las de las
            // tres casas — SE BORRAN). Sus dos favoritos (Coro y
            // Pluma) y la mascota del taller viajan ahora en la
            // ENTREGA DIRECTA del kit (ShardPlayer.EntregarSiNoTiene)
            // — la regla permanente: nada nuevo queda sin llegar al
            // jugador.

            // v6.51.0 — LA LLAVE DEL DESPERTAR sigue como entrega DIRECTA
            // (no es una bolsa — el probador de la progresión viaja suelto,
            // igual que las dummies: la letra de la reorganización habla de
            // LAS BOLSAS).
            bolsas += Entregar(ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.EcoDelDespertar>());

            // === LAS 99 DUMMIES DE PRUEBA (el campo de entrenamiento) ===
            int dummies = 0;
            if (!HasItem(ItemID.TargetDummy))
            {
                GiveItem(ItemID.TargetDummy, 99);
                dummies = 99;
            }

            // === v6.105 — LOS 30 MUÑECOS NEGROS (el banco de pruebas del
            //     grimorio: el mismo sprite del dummy de Terraria normal
            //     teñido de negro — y ESTOS SÍ reciben todos los ataques
            //     del grimorio; los dummies normales vuelven a comportarse
            //     como en el juego base) ===
            int tipoNegro = ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.DummyNegroItem>();
            if (!HasItem(tipoNegro))
                GiveItem(tipoNegro, 30);

            if (bolsas > 0 && Player.whoAmI == Main.myPlayer)
            {
                // v6.50.2 — FIX (strings hardcodeados → hjson, regla de la
                // casa): los avisos del kit de pruebas viajan por clave.
                Terraria.Main.NewText(
                    Terraria.Localization.Language.GetTextValue("Mods.AethonMod.TestingPlayer.Bolsas"),
                    new Microsoft.Xna.Framework.Color(230, 196, 255));
                if (dummies > 0)
                    Terraria.Main.NewText(
                        Terraria.Localization.Language.GetTextValue("Mods.AethonMod.TestingPlayer.Dummies", dummies),
                        new Microsoft.Xna.Framework.Color(255, 216, 107));
            }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); } // v6.50.10 — la armadura de arriba
        }

        /// <summary>Entrega el ítem si no se tiene; devuelve 1 si se entregó.</summary>
        private int Entregar(int itemType)
        {
            if (HasItem(itemType)) return 0;
            GiveItem(itemType, 1);
            return 1;
        }

        /// <summary>¿El jugador tiene este ítem? (incluido el slot del
        /// cursor — v6.56.1, la misma cura de BolsaCategoria.Tiene).</summary>
        private bool HasItem(int itemType)
        {
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == itemType)
                    return true;
            }
            return false;
        }

        private void GiveItem(int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] == null ||
                    Player.inventory[i].type == Terraria.ID.ItemID.None)
                {
                    Player.inventory[i].SetDefaults(itemType);
                    Player.inventory[i].stack = stack;
                    return;
                }
            }
            int drop = Item.NewItem(Player.GetSource_GiftOrReward(), Player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
