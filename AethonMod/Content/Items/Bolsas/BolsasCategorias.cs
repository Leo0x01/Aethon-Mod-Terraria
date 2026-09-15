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
    //  v6.29 — LAS DIEZ BOLSAS POR CATEGORÍA
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
            return l;
        }
    }

    /// <summary>2 — LOS BASTONES FUNDACIONALES: los cuatro del alba del mod.</summary>
    public class BolsaFundacionales : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Bastones Fundacionales";
        protected override string NombreCorto => "Bolsa de los Fundacionales";
        protected override Color ColorFiesta => new(90, 220, 255);
        protected override string Nota => "Los cuatro bastones que empezaron todo";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
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

    /// <summary>4 — LOS AGUJEROS NEGROS: los diez definitivos.</summary>
    public class BolsaAgujerosNegros : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Agujeros Negros";
        protected override string NombreCorto => "Bolsa de los Agujeros Negros";
        protected override Color ColorFiesta => new(170, 90, 255);
        protected override string Nota => "Diez formas de devorar la luz";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
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

    /// <summary>9 — LOS EXHUMADOS: las formas nuevas de v6.29 (Calamity research).</summary>
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

    /// <summary>10 — LOS COSMÉTICOS: las coronas de la casa.</summary>
    public class BolsaCosmeticos : BolsaCategoria
    {
        protected override string Titulo => "La Bolsa de los Cosméticos";
        protected override string NombreCorto => "Bolsa de los Cosméticos";
        protected override Color ColorFiesta => new(255, 140, 220);
        protected override string Nota => "Las coronas que se ciñen a tu personaje";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            l.Add((ModContent.ItemType<Cosmetics.VoidCrownItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.RuneCrownItem>(), 1));
            return l;
        }
    }
}
