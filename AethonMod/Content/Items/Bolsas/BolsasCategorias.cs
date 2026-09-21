using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons;
using AethonMod.Content.Weapons.V20;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.Items.Cosmetics;

namespace AethonMod.Content.Items.Bolsas
{
    // ======================================================================
    //  v6.29 — LAS BOLSAS POR CATEGORÍA (v6.50.3 — doc-rot: son DIECISÉIS)
    //
    //  Petición del usuario: "crea varias bolsas para todas las armas que me
    //  tienes que dar no solo una y separalas por categorías, una categoría
    //  por bolsa". La bolsa única del Arsenal (v6.27) se retira y el kit de
    //  pruebas entrega UNA BOLSA POR CATEGORÍA — cada una con la semántica
    //  de garantía de la casa (solo lo que falte, permanente, reabrible).
    //
    //  Las 99 Dummies de prueba se entregan APARTE (directo al inventario,
    //  garantizadas en cada entrada al mundo).
    // ======================================================================

    /// <summary>1 — EL KIT DEL PROBADOR: las herramientas de prueba del mod.</summary>
    public class BolsaProbador : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa del Probador";
        protected override string NombreCorto => "Bolsa del Probador";
        protected override Color ColorFiesta => new(255, 216, 107);
        protected override string Nota => "Las herramientas de pruebas de Aethon";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<GenesisShard>(), 1));
            l.Add((ItemID.GoldBar, 100));
            l.Add((ModContent.ItemType<LevelUpTester>(), 1));
            l.Add((ModContent.ItemType<BossSummonBag>(), 1));
            l.Add((ModContent.ItemType<Items.SeerOrb>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestMagicRing>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestSparkle>(), 1));
            l.Add((ModContent.ItemType<Weapons.ProjBeam>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestMagicRingV2>(), 1));
            // v6.43 — CieloLib: el prisma que despliega los paisajes de la
            // librería del cielo (el tester del fondo del juego).
            l.Add((ModContent.ItemType<Cosmetics.PrismaDePaisajesItem>(), 1));
            // v6.44 — EL LUGAR, COLÓCALO DONDE QUIERAS: el Altar Antiguo
            // colocable (5) para montar un Sagrario de pruebas en cualquier
            // mundo — los altares naturales solo generan en mundos NUEVOS,
            // y el flujo del Fragmento Génesis debe poder probarse en el
            // mundo de pruebas que ya tengas.
            l.Add((ModContent.ItemType<Items.Placeables.AncientAltarItem>(), 5));
            // v6.47 — LA CARNADA DEL GRIMORIO: el probador del evento de las
            // oleadas del hambre (clic der prepara 1..10 oleadas, clic izq
            // desata la furia). Sin esperas: el festín a la carta.
            l.Add((ModContent.ItemType<Items.CarnadaDelGrimorio>(), 1));
            // v6.47 — LOS LLAMADOS: los cinco invocadores de los jefes del
            // mod (de día) — el Testigo ya aparece solo; los jefes, a llamar.
            l.Add((ModContent.ItemType<Items.Llamados.CristalDelTitanHueco>(), 1));
            l.Add((ModContent.ItemType<Items.Llamados.SelloDelRift>(), 1));
            l.Add((ModContent.ItemType<Items.Llamados.PlumaDeLaArquera>(), 1));
            l.Add((ModContent.ItemType<Items.Llamados.SombraDelPortador>(), 1));
            l.Add((ModContent.ItemType<Items.Llamados.NombreDeAethon>(), 1));
            return l;
        }
    }

    /// <summary>2 — LOS BASTONES FUNDACIONALES: los que empezaron todo
    /// (v6.41: EL GRIMORIO ETERNO entra en la bolsa — el arma raíz del
    /// mod no estaba en NINGUNA y no había forma de probarla sin
    /// fabricarla; ahora los cinco del alba). </summary>
    public class BolsaFundacionales : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Bastones Fundacionales";
        protected override string NombreCorto => "Bolsa de los Fundacionales";
        protected override Color ColorFiesta => new(90, 220, 255);
        protected override string Nota => "El grimorio raíz y los cuatro bastones del alba";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.GrimoireEternal>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1));
            return l;
        }
    }

    /// <summary>3 — LOS CLÁSICOS CÓSMICOS: el sol y las criaturas vivas.</summary>
    public class BolsaClasicosCosmicos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Clásicos Cósmicos";
        protected override string NombreCorto => "Bolsa de los Clásicos Cósmicos";
        protected override Color ColorFiesta => new(255, 200, 90);
        protected override string Nota => "El sol primordial y sus criaturas";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LivingCometStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LivingPulsarStaff>(), 1));
            return l;
        }
    }

    /// <summary>4 — LOS AGUJEROS NEGROS: los once definitivos (doc-rot v6.50.3).</summary>
    public class BolsaAgujerosNegros : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Agujeros Negros";
        protected override string NombreCorto => "Bolsa de los Agujeros Negros";
        protected override Color ColorFiesta => new(170, 90, 255);
        protected override string Nota => "Once formas de devorar la luz";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.OlvidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CosmicBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.UmbralBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.BrumaBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SupremoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SupremoAuroraBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.UmbralAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.BrumaAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CosmicAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.OlvidoAscendidoBlackHoleStaff>(), 1));
            return l;
        }
    }

    /// <summary>5 — LOS SOLES RÚNICOS: la veintena completa.</summary>
    public class BolsaSolesRunicos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Soles Rúnicos";
        protected override string NombreCorto => "Bolsa de los Soles Rúnicos";
        protected override Color ColorFiesta => new(255, 150, 60);
        protected override string Nota => "Del sol de un anillo al Gran Sellado";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico1Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico2Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico3Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico4Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico5Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico6Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico7Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico8Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico9Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico10Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico11Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico12Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico13Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico14Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico15Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico16Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico17Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico18Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico19Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico20Staff>(), 1));
            return l;
        }
    }

    /// <summary>6 — LAS ESTRELLAS REALES: las que existen de verdad.</summary>
    public class BolsaEstrellasReales : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de las Estrellas Reales";
        protected override string NombreCorto => "Bolsa de las Estrellas Reales";
        protected override Color ColorFiesta => new(150, 190, 255);
        protected override string Nota => "Física de verdad convertida en armas";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.NeutronStarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.PulsarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.WhiteDwarfStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.DeadStarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.RedSupergiantStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MagnetarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CicloEstelarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SembradorCementeralStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.ColapsoMagnetarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LagrimasSolMoribundoStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.DecretoEclipseStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CometaErranteStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.NovaEncadenadaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.VozCuasarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.TelarConstelacionesStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LluviaMeteorosStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.AbrazoNebulosaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.FiloHorizonteStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.RayoGammaStaff>(), 1));
            return l;
        }
    }

    /// <summary>7 — LAS ARMAS DE LAS LIBRERÍAS: las que usan nuestras VFX.</summary>
    public class BolsaArmasLibrerias : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de las Armas de las Librerías";
        protected override string NombreCorto => "Bolsa de las Librerías";
        protected override Color ColorFiesta => new(190, 120, 255);
        protected override string Nota => "Cada una es un concierto de nuestras librerías VFX";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.SinfoniaPrimordialStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.TormentaNebularStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LanzaAlbaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.EclipsePrimordialStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.StormRuneStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.DesgarroRealityStaff>(), 1));
            return l;
        }
    }

    /// <summary>8 — LOS BASTONES CREATIVOS: los experimentos de la casa.</summary>
    public class BolsaBastonesCreativos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Bastones Creativos";
        protected override string NombreCorto => "Bolsa de los Creativos";
        protected override Color ColorFiesta => new(90, 230, 200);
        protected override string Nota => "El gauge, el péndulo, el coro y demás rarezas";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.RelojArenaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MareaGravitatoriaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.EnjambrePrismaticoStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.PenduloJuicioStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CoroEspectralStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.OcasoAethonStaff>(), 1));
            return l;
        }
    }

    /// <summary>9 — LOS EXHUMADOS: las formas nuevas de v6.29.</summary>
    public class BolsaExhumados : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Exhumados";
        protected override string NombreCorto => "Bolsa de los Exhumados";
        protected override Color ColorFiesta => new(255, 90, 110);
        protected override string Nota => "Las formas exhumadas: el rencor y la eminencia";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Weapons.Cosmic.RencorPrimordialStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.EminenciaAtrozStaff>(), 1));
            return l;
        }
    }

    /// <summary>10 — LOS COSMÉTICOS: las coronas de la casa + LOS TRES
    /// ACCESORIOS DE LAS LIBRERÍAS DE SIGNOS MÁGICOS (v6.35: los sellos y
    /// anillos que rodean al jugador) + EL ANILLO RÚNICO DORSAL (v6.36:
    /// la firma del vacío en la espalda) + LA CORONA RÚNICA DE AURA y LA
    /// FORMA ASCENDIDA (v6.48: las DOS auras de AURALIB por el PORTADOR
    /// aditivo — el pentágono violeta-oro y la luz primordial de Aethon).
    /// </summary>
    public class BolsaCosmeticos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Cosméticos";
        protected override string NombreCorto => "Bolsa de los Cosméticos";
        protected override Color ColorFiesta => new(255, 140, 220);
        protected override string Nota => "Coronas, sellos y anillos que se ciñen a tu personaje";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Cosmetics.VoidCrownItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.RuneCrownItem>(), 1));
            l.Add((ModContent.ItemType<Accessories.SelloGenesisItem>(), 1));
            l.Add((ModContent.ItemType<Accessories.AnillosSolRunicoItem>(), 1));
            l.Add((ModContent.ItemType<Accessories.AnillosHorizonteItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.AnilloRunicoDorsalItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.CoronaRunicoAuraItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.FormaAscendidaItem>(), 1));
            return l;
        }
    }

    /// <summary>11 — LAS DOS FORMAS (v6.33): LAS 4 ARMAS QUE NACIERON DE LAS
    /// DOS FORMAS DE USO de las armas investigadas — su propia bolsa para que
    /// sean IMPOSIBLES de perder de vista (la queja del usuario: "no veo las
    /// 4 armas nuevas de las dos formas").</summary>
    public class BolsaDosFormas : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de las Dos Formas";
        protected override string NombreCorto => "Bolsa de las Dos Formas";
        protected override Color ColorFiesta => new(120, 230, 180);
        protected override string Nota => "Cada arma nació de una forma de uso distinta";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<SembradorCementeralStaff>(), 1));
            l.Add((ModContent.ItemType<ColapsoMagnetarStaff>(), 1));
            l.Add((ModContent.ItemType<LagrimasSolMoribundoStaff>(), 1));
            l.Add((ModContent.ItemType<DecretoEclipseStaff>(), 1));
            return l;
        }
    }

    /// <summary>12 — LOS DESGARROS (v6.33 + v6.35): el clásico + LOS 4
    /// de la primera tanda + LOS 4 NUEVOS de la segunda (el corazón, la
    /// garganta, el umbral y el leviatán).</summary>
    public class BolsaDesgarros : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Desgarros de Realidad";
        protected override string NombreCorto => "Bolsa de los Desgarros";
        protected override Color ColorFiesta => new(0, 229, 255);
        protected override string Nota => "Nueve formas de romper el tejido del mundo";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<DesgarroRealityStaff>(), 1));
            l.Add((ModContent.ItemType<SuturaCuanticaStaff>(), 1));
            l.Add((ModContent.ItemType<PortalDimensionalStaff>(), 1));
            l.Add((ModContent.ItemType<PliegueEspacioStaff>(), 1));
            l.Add((ModContent.ItemType<HeridaElectricaStaff>(), 1));
            l.Add((ModContent.ItemType<CorazonColapsoStaff>(), 1));
            l.Add((ModContent.ItemType<GargantaVacioStaff>(), 1));
            l.Add((ModContent.ItemType<UmbralRotoStaff>(), 1));
            l.Add((ModContent.ItemType<LeviatanEspectralStaff>(), 1));
            return l;
        }
    }

    /// <summary>13 — LOS CÓDIGOS VIVOS (v6.36): LAS 4 ARMAS QUE NACIERON
    /// DE LOS CÓDIGOS DE LAS IMÁGENES DEL USUARIO — cada una es la
    /// traducción C# de una demo web hecha arma (la cadena follow, el
    /// agujero negro con lente, el sol que late y la sierpe de 16
    /// segmentos). Su propia bolsa: códigos ajenos que aprendieron a
    /// vivir aquí.</summary>
    public class BolsaCodigosVivos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Códigos Vivos";
        protected override string NombreCorto => "Bolsa de los Códigos Vivos";
        protected override Color ColorFiesta => new(120, 255, 190);
        protected override string Nota => "Cuatro códigos ajenos que aprendieron a vivir aquí";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<DanzaOrbesStaff>(), 1));
            l.Add((ModContent.ItemType<LenteAbismoStaff>(), 1));
            l.Add((ModContent.ItemType<SolVivoStaff>(), 1));
            l.Add((ModContent.ItemType<SierpeEstelarStaff>(), 1));
            return l;
        }
    }

    /// <summary>14 — LAS SIERPES (v6.38): LA FAMILIA COMPLETA — la sierpe
    /// estelar original (INTACTA) y sus once descendientes: diez formas
    /// nuevas de mover la misma cadena (el anillo, la caravana, la
    /// anguila, el ciempiés, el látigo, la víbora, la boa, el farol, la
    /// cinta y la manada — cada una nacida de un patrón distinto del
    /// informe de locomoción) y LA CRÍA: la sierpe hecha minion.</summary>
    public class BolsaSierpes : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de las Sierpes";
        protected override string NombreCorto => "Bolsa de las Sierpes";
        protected override Color ColorFiesta => new(170, 240, 255);
        protected override string Nota => "La sierpe estelar y sus once descendientes: cada una nada distinto";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<SierpeEstelarStaff>(), 1));       // LA MADRE (intacta)
            l.Add((ModContent.ItemType<OuroborosAstralStaff>(), 1));      // el anillo que se persigue
            l.Add((ModContent.ItemType<CaravanaEspectralStaff>(), 1));    // el tren del camino-memoria
            l.Add((ModContent.ItemType<AnguilaSolarStaff>(), 1));         // la fórmula ondulante
            l.Add((ModContent.ItemType<CienpiesRunicoStaff>(), 1));       // la marcha metacronal
            l.Add((ModContent.ItemType<FlageloEstelarStaff>(), 1));       // el látigo que chasquea
            l.Add((ModContent.ItemType<ViboraGenesiacaStaff>(), 1));      // la doble hélice
            l.Add((ModContent.ItemType<BoaEclipseStaff>(), 1));           // la que abraza
            l.Add((ModContent.ItemType<FarolGuardianStaff>(), 1));        // la que alcanza
            l.Add((ModContent.ItemType<CintaAuroraStaff>(), 1));          // la que ondea
            l.Add((ModContent.ItemType<ManadaAstralStaff>(), 1));         // la manada elástica
            l.Add((ModContent.ItemType<CriaEstelarStaff>(), 1));          // LA CRÍA (minion)
            return l;
        }
    }

    /// <summary>15 — LOS HUÉSPEDES (v6.41): LAS ARMAS DE PRUEBAS NACIDAS
    /// DE RÉPLICAS — el tentáculo del vacío (el tomo de la apatía nula)
    /// y el fragmento de supernova (la singularidad alada hecha minion),
    /// más el bastón de los tajos astrales (v6.40, que se quedó sin
    /// bolsa). Su propia bolsa: son armas de PRUEBAS fieles a su fuente
    /// de estudio — visitantes en el arsenal.</summary>
    public class BolsaHuespedes : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Huéspedes";
        protected override string NombreCorto => "Bolsa de los Huéspedes";
        protected override Color ColorFiesta => new(140, 255, 160);
        protected override string Nota => "Réplicas de prueba: el tentáculo, el fragmento y los tajos astrales";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<TomoApatiaNula>(), 1));            // el tentáculo del vacío
            l.Add((ModContent.ItemType<FragmentoSupernovaStaff>(), 1));   // la singularidad alada (minion)
            l.Add((ModContent.ItemType<TajosAstralesStaff>(), 1));        // v6.40: los tajos del anime
            return l;
        }
    }

    /// <summary>16 — LAS APUESTAS (v6.42): LAS CINCO ARMAS EN LAS QUE LA
    /// CASA APOSTÓ — cinco mecánicas distintas de juego (el ritmo, la
    /// cadencia creciente, la zona melee, el parry defensivo y el eco
    /// encadenado) más EL VERBO PRIMORDIAL: la palabra que las une a
    /// todas, el arma que habla con TODAS las bibliotecas de la casa
    /// en sus siete movimientos.</summary>
    public class BolsaApuestas : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de las Apuestas";
        protected override string NombreCorto => "Bolsa de las Apuestas";
        protected override Color ColorFiesta => new(255, 230, 140);
        protected override string Nota => "Cinco apuestas de la casa y la palabra que las une";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<MetronomoPulsar>(), 1));       // APUESTA 1: el ritmo
            l.Add((ModContent.ItemType<VelaSolar>(), 1));             // APUESTA 2: la cadencia
            l.Add((ModContent.ItemType<GuadanaDesgarro>(), 1));       // APUESTA 3: la zona melee
            l.Add((ModContent.ItemType<EgidaNova>(), 1));             // APUESTA 4: el parry
            l.Add((ModContent.ItemType<EcoCuantico>(), 1));           // APUESTA 5: el eco
            l.Add((ModContent.ItemType<VerboPrimordialStaff>(), 1));  // EL VERBO (todas las librerías)
            return l;
        }
    }
}
