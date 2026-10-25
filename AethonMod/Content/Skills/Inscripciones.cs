using Terraria;

namespace AethonMod.Content.Skills
{
    // ======================================================================
    //  v6.52.0 — EL ROSTRO DE LAS INSCRIPCIONES: las 18 palabras que el
    //  Escriba puede grabar en su propia pluma (subclasificar
    //  InscripcionDefinicion ES registrarlas — el escáner de reflexión
    //  las encuentra solo).
    //
    //  LA REGLA DE ORO (robada de CWR verbatim): TODO poder lleva su
    //  CARGA — armar la pluma es una DECISIÓN, no una lista de compras.
    //
    //  EL MAPA DE DESBLOQUEO:
    //    · 3 DE NACIMIENTO (una por ranura — la pluma ya sabe lo básico)
    //    · 5 por HECHOS (retos de juego — HechosCatalogo)
    //    · 10 por REGALOS (la reacción del Escriba a cada jefe vencido
    //      con la pluma en el inventario — InscripcionJugador)
    // ======================================================================

    // ------------------------------------------------------------------
    //  FILIGRANA — la ornamentación (utilidad, economía, cadencia)
    // ------------------------------------------------------------------

    /// <summary>DE NACIMIENTO — la tinta que fluye ligera.</summary>
    public sealed class TintaLigera : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 10;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.VelocidadMul *= 1.10f;
            p.DañoMul *= 0.97f;   // la carga: letra más rápida, trazo más fino
        }
    }

    /// <summary>HECHO TintaSeca — la pluma que aprendió a no gastarse.</summary>
    public sealed class PlumaAjena : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 20;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.ManaMul *= 0.60f;   // el ahorro brutal
            p.DañoMul *= 0.96f;   // la carga: la tinta ajena muerde menos
        }
    }

    /// <summary>HECHO ManoFirme — el margen donde nada sobra.</summary>
    public sealed class MargenEstrecho : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 30;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.CriticoSuma += 4;
            p.DañoMul *= 0.98f;
        }
    }

    /// <summary>HECHO RachaViva — el interlineado: más líneas por página.</summary>
    public sealed class Interlineado : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 40;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.ProyectilesExtra += 2;
            p.DañoMul *= 0.90f;   // la carga: dos trazos más, cada uno más débil
        }
    }

    /// <summary>REGALO del Devorador de Mundos — la letra con la que nacen los títulos.</summary>
    public sealed class LetraCapital : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 50;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.VelocidadMul *= 1.15f;
            p.ManaMul *= 1.10f;
        }
    }

    /// <summary>REGALO del Gólem — la caligrafía perfecta, medida y paciente.</summary>
    public sealed class Caligrafia : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Filigrana;
        public override int Orden => 60;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.VelocidadMul *= 1.20f;
            p.DañoMul *= 0.97f;
        }
    }

    // ------------------------------------------------------------------
    //  VERSO — la ofensiva (daño, proyectiles, buscapresas)
    // ------------------------------------------------------------------

    /// <summary>DE NACIMIENTO — el verso que aprende todo escriba.</summary>
    public sealed class VersoSencillo : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 10;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.DañoMul *= 1.12f;
            p.ManaMul *= 1.08f;
        }
    }

    /// <summary>HECHO PrimerasPalabras — el eco del verso: se dice dos veces.</summary>
    public sealed class VersoDoble : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 20;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.ProyectilesExtra += 1;
            p.DañoMul *= 0.95f;
        }
    }

    /// <summary>REGALO del Esqueletrón — la tinta que corroe el hueso.</summary>
    public sealed class TintaCorrosiva : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 30;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.DañoMul *= 1.20f;
            p.ManaMul *= 1.20f;   // la carga: la tinta fuerte es cara
        }
    }

    /// <summary>REGALO de los Gemelos — el verso que encuentra solo a quién lee.</summary>
    public sealed class VersoCiego : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 40;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.BuscaPresa = true;
            p.DañoMul *= 0.93f;
        }
    }

    /// <summary>REGALO de Plantera — la rima que corre por dentro del trazo.</summary>
    public sealed class RimaInterna : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 50;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.EstelaTinta = true;
            p.DañoMul *= 1.05f;
        }
    }

    /// <summary>REGALO del Cultista — el hemistiquio: el verso partido que atraviesa.</summary>
    public sealed class Hemistiquio : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Verso;
        public override int Orden => 60;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.PerforacionExtra += 2;
            p.ManaMul *= 1.15f;
        }
    }

    // ------------------------------------------------------------------
    //  COLOFÓN — el remate (crítico, ejecución, robo de vida)
    // ------------------------------------------------------------------

    /// <summary>DE NACIMIENTO — el colofón en blanco: lo que todo libro lleva.</summary>
    public sealed class ColofonHumilde : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 10;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.CriticoSuma += 3;
        }
    }

    /// <summary>HECHO AlMargen — el punto final: lo que está casi muerto, MUERE.</summary>
    public sealed class PuntoFinal : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 20;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.Ejecutar = true;
            p.DañoMul *= 0.95f;
        }
    }

    /// <summary>REGALO del Ojo de Cthulhu — lo importante, SUBRAYADO.</summary>
    public sealed class Subrayado : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 30;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.CriticoSuma += 8;
            p.ManaMul *= 1.12f;
        }
    }

    /// <summary>REGALO del Muro de Carne — la firma: lo escrito con sangre devuelve sangre.</summary>
    public sealed class Firma : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 40;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.RoboVida = true;
            p.DañoMul *= 0.92f;
        }
    }

    /// <summary>REGALO del Señor de la Luna — la tinta de donde salieron TODAS.</summary>
    public sealed class TintaMadre : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 50;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.BuscaPresa = true;
            p.PerforacionExtra += 3;
            p.ManaMul *= 1.30f;
        }
    }

    /// <summary>REGALO DEL ESCRIBA MISMO (vencerlo con la pluma) — la última
    /// palabra del libro: el colofón que cierra TODAS las historias.</summary>
    public sealed class ElColofon : InscripcionDefinicion
    {
        public override RanuraInscripcion Ranura => RanuraInscripcion.Colofon;
        public override int Orden => 60;
        public override void Perfil(ref PerfilInscripcion p)
        {
            p.DañoMul *= 1.35f;
            p.ProyectilesExtra += 2;
            p.ManaMul *= 1.35f;
            p.VelocidadMul *= 0.90f;   // la carga: la última palabra es LENTA
        }
    }
}
