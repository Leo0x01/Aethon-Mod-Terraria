// LAS ALAS RÚNICAS + LAS 3 ANATOMÍAS (v6.56.0) — 7 alas nuevas (bolsa 15→22).
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.


namespace AethonMod.Content.Items.Alas
{
    public static class AlasNuevasV6560
    {
    }

    public class AlasRunicasSolares : AlasPrismaBase
    {
        protected override byte Estilo => 17;
    }

    public class AlasRunicasDelVacio : AlasPrismaBase
    {
        protected override byte Estilo => 18;
    }

    public class AlasRunicasDelEclipse : AlasPrismaBase
    {
        protected override byte Estilo => 19;
    }

    public class AlasRunicasDelJuicio : AlasPrismaBase
    {
        protected override byte Estilo => 20;
    }

    public class AgujasDelVacio : AlasPrismaBase
    {
        protected override byte Estilo => 21;
    }

    public class CuchillasDelEclipse : AlasPrismaBase
    {
        protected override byte Estilo => 22;
    }

    public class CorolasDelAlba : AlasPrismaBase
    {
        protected override byte Estilo => 23;
    }
}
