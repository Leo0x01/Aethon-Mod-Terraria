// =====================================================================
//  v6.135 — LAS SEIS MITOLÓGICAS (la letra: «teniendo en cuenta las
//  alas de arcangeles superiores que tiene 3 pares de alas o sea 6
//  alas y hacer otras versiones de las alas runicas del vacio
//  basandote en otras mitologias o creaciones y tipos de alas»).
//
//  TODAS nacen de las Runas del Vacío v6.134 (la geometría exacta del
//  Eclipse + el ala lisa): cada una cambia SU ALMA — paleta, runas,
//  ruido, plumas por lado, curva y el nuevo eje CORONAS (el tercer
//  par del arcángel, arriba de la espalda):
//    6 Serafín del Vacío — la hebrea de los ardiendo: TRES PARES.
//    7 Valquiria Rúnica — la nórdica: siete plumas de lanza, plata.
//    8 Quetzal Emplumado — la serpiente azteca que ondea en tres niveles.
//    9 Fénix del Renacer — la llama que renace: tres llamas anidadas.
//    10 Tengu del Crepúsculo — el cuervo japonés: plumas primarias negras.
//    11 Dragón del Vacío — la membrana de dos dedos: seis velas.
// =====================================================================


namespace AethonMod.Content.Items.Alas
{
    /// <summary>6 — EL SERAFÍN DEL VACÍO: tres pares de alas de fuego
    /// violeta que no quema — la pose del trono.</summary>
    public class AlasSerafinDelVacio : AlasPrismaBase
    {
        protected override byte Estilo => 6;
    }

    /// <summary>7 — LA VALQUIRIA RÚNICA: siete plumas de lanza rectas,
    /// la plata del cielo de batalla con el futhark de la casa.</summary>
    public class AlasValquiriaRunica : AlasPrismaBase
    {
        protected override byte Estilo => 7;
    }

    /// <summary>8 — EL QUETZAL EMPLUMADO: la serpiente de jade que
    /// ondea en tres niveles de plumas turquesa.</summary>
    public class AlasQuetzalEmplumado : AlasPrismaBase
    {
        protected override byte Estilo => 8;
    }

    /// <summary>9 — EL FÉNIX DEL RENACER: la llama anidada en tres
    /// fuegos — núcleo blanco, llama viva, brasa hondo.</summary>
    public class AlasFenixDelRenacer : AlasPrismaBase
    {
        protected override byte Estilo => 9;
    }

    /// <summary>10 — EL TENGU DEL CREPÚSCULO: las plumas primarias del
    /// cuervo — tinta azul-negra con glifos ámbar del abismo.</summary>
    public class AlasTenguDelCrepusculo : AlasPrismaBase
    {
        protected override byte Estilo => 10;
    }

    /// <summary>11 — EL DRAGÓN DEL VACÍO: la membrana de dos dedos —
    /// seis velas de escamas rojo-vino.</summary>
    public class AlasDragonDelVacio : AlasPrismaBase
    {
        protected override byte Estilo => 11;
    }

    // ====================================================================
    //  v6.137 — LAS DOS COPIAS DE LA VALQUIRIA RÚNICA (la letra: «hacer
    //  dos copias, en una quita las plumas y no toques nada mas, en la
    //  otra intenta darle una forma lisa a las plumas y no toques nada
    //  mas»): el alma plata intacta — SOLO el cuerpo cambia.
    // ====================================================================

    /// <summary>12 — LA VALQUIRIA SIN PLUMAS: sin el abanico de siete
    /// plumas de lanza — la vela estructural de la casa, nada más.</summary>
    public class AlasValquiriaSinPlumas : AlasPrismaBase
    {
        protected override byte Estilo => 12;
    }

    /// <summary>13 — LA VALQUIRIA LISA: las plumas dejan de ser tiras —
    /// la vela continua del borde maestro (la receta lisa de la casa).
    /// v6.139 — SOLO RUNAS (la letra: «las alas como tal quitalas y
    /// deja las runas que tiene como alas»): el cuerpo murió — quedan
    /// las runas del borde como alas.</summary>
    public class AlasValquiriaLisa : AlasPrismaBase
    {
        protected override byte Estilo => 13;
    }

    // ====================================================================
    //  v6.139 — LAS ALAS RÚNICAS (la letra: «mejora esas runas añadiendo
    //  unas alas de solo runas basandote en como se ve las alas valquiria
    //  lisa, estas se llamaran alas runicas, usa el mismo sistema de
    //  runas que usaste para valquiria lisa y mejoralo»): el ala hecha
    //  de ESCRITURA — el enjambre de glifos dibuja el borde maestro, la
    //  vela y las puntas (sin cuerpo, como la Lisa — pero DENSO).
    // ====================================================================

    /// <summary>14 — LAS ALAS RÚNICAS: el enjambre — tres corrientes de
    /// glifos (borde · vela · puntas) con la runa MAYOR de cada ala.</summary>
    public class AlasRunicas : AlasPrismaBase
    {
        protected override byte Estilo => 14;
    }
}
