// =====================================================================
//  v6.113 — LAS CINCO ALAS DE LA CASA (la letra: «guardar las siguientes
//  alas: alas runicas del eclipse, corolas del alba, alas runicas del
//  vacio, alas runicas solares, alas runicas del juicio — el resto de
//  alas las puedes borrar»).
//
//  LAS CINCO: 1 Solares · 2 Vacío (la copia mejorada del eclipse) ·
//  3 Eclipse · 4 Juicio · 5 Corolas del Alba. MURIERON con la purga: la
//  Aurora de Tinta original, LAS QUINCE del prisma (Tinta Serena, Eclipse,
//  Plasma, Nebulosa, Cometario, Medianoche, Prisma, Fuego Frío, Tormenta,
//  Marioneta, Aurora Ártica, Orquídea, Vacío, Solares Rúnicas y Sangre de
//  Tinta), las Agujas del Vacío y las Cuchillas del Eclipse.
//
//  (v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del
//  sandbox: la fuente original se perdió; este código viene del .tmod
//  (md5 3b45a5fe…) y se verificó idéntico al recompilar. Los comentarios
//  de la fuente original no viajan dentro del .dll.)
// =====================================================================


namespace AethonMod.Content.Items.Alas
{
    public static class AlasNuevasV6560
    {
    }

    /// <summary>1 — LAS RUNAS SOLARES: el oro de los soles del grimorio.</summary>
    public class AlasRunicasSolares : AlasPrismaBase
    {
        protected override byte Estilo => 1;
    }

    /// <summary>2 — LAS RUNAS DEL VACÍO (v6.113 — LA COPIA MEJORADA DEL
    /// ECLIPSE: sin picos, simétricas, del color del vacío y con sus
    /// efectos — el anillo rúnico violeta y LAS MIGAS que caen de las
    /// alas, en AlasAuroraHalo).</summary>
    public class AlasRunicasDelVacio : AlasPrismaBase
    {
        protected override byte Estilo => 2;
    }

    /// <summary>3 — LAS RUNAS DEL ECLIPSE: el ámbar del cielo tapado.</summary>
    public class AlasRunicasDelEclipse : AlasPrismaBase
    {
        protected override byte Estilo => 3;
    }

    /// <summary>4 — LAS RUNAS DEL JUICIO: el oro del veredicto.</summary>
    public class AlasRunicasDelJuicio : AlasPrismaBase
    {
        protected override byte Estilo => 4;
    }

    /// <summary>5 — LAS COROLAS DEL ALBA: el blanco cálido de la aurora.</summary>
    public class CorolasDelAlba : AlasPrismaBase
    {
        protected override byte Estilo => 5;
    }
}
