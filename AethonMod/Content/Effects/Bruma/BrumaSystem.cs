using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaSystem — v6.25 — EL CICLO DE VIDA DE LAS LIBRERÍAS DE VFX.
    ///
    /// La librería de humo/niebla/bruma procedural hornea sus pinceles
    /// (Texture2D nacidas de código) EN RUNTIME, la primera vez que se
    /// dibujan. Este ModSystem garantiza que, al recargar o descargar el
    /// mod, TODAS esas texturas se disponeN — cero fugas de VRAM entre
    /// sesiones (BrumaBrushes.Unload).
    ///
    /// v6.25: también vacía el estado estático de las librerías nuevas
    /// (los campos de brasas de PyraLib y los tracks de camino de
    /// EstelaLib) — la descarga queda LIMPIA de verdad.
    ///
    /// v6.49 — LA HIGIENE AMPLIADA (auditorías AUD-A/C): la purga
    /// periódica del pool de Cinta (cintas huérfanas por muertes sin
    /// OnKill) cuelga del PostUpdate de este sistema (ya era EL ciclo de
    /// vida de las librerías de VFX) y la descarga también suelta las
    /// cintas, VFXCore y AudioLib — el barrendero único de la casa.
    ///
    /// Las librerías en sí son INERTES: no dibujan nada por su cuenta;
    /// las usa quien las necesite desde su propio renderer con SU lote
    /// y SU contrato de batch.
    /// </summary>
    public class BrumaSystem : ModSystem
    {
        /// <summary>v6.49 — la purga del pool de Cinta (cada 120 ticks, barata).</summary>
        /// <summary>v6.56.1 — el podador de las marcas del festín: la
        /// caducidad de 5 s de FaucesGlobalNPC vivía SIN llamador desde
        /// una purga antigua — una marca sobreviviente interceptaba la
        /// muerte del jefe PARA SIEMPRE (18 s de cine por cualquier otra
        /// causa de muerte minutos después).</summary>
        private int _tickPodaMarcas;

        public override void PostUpdateWorld()
        {
            Cinta.TickPurga();
            if (++_tickPodaMarcas >= 60)
            {
                _tickPodaMarcas = 0;
                try { global::AethonMod.Content.Globals.FaucesGlobalNPC.PodarMarcas(); } catch { }
            }
        }

        public override void Unload()
        {
            // Disposición de TODAS las texturas horneadas en runtime.
            BrumaBrushes.Unload();

            // Estado estático de las librerías v6.25.
            PyraLib.ClearFields();
            EstelaLib.ClearTracks();

            // v6.49 — el barrendero único también en la descarga: el pool
            // de cintas y el NÚCLEO de VFX (búfer/presupuesto/factor) no
            // eran limpiados por NADIE (hallazgo AUD-C).
            try { Cinta.Purgar(); } catch { }
            try { VFXCore.Reiniciar(); } catch { }

            // v6.56.1 — las ALAS también: sus 2 BasicEffect solo se
            // limpiaban al salir del MUNDO (OnWorldUnload) — en la
            // descarga del mod fugaban en cada recarga.
            try { global::AethonMod.Content.VFX.AlasAuroraLib.Reiniciar(); } catch { }
        }
    }
}
