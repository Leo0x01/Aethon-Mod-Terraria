// ======================================================================
    //  AuraLibDibujo — v6.177 — EL PARTIDO DE LA BIBLIOTECA DEL AURA
    //  (la letra de la sesión 30: partir los gigantes por
    //  responsabilidades SIN tocar comportamiento). Aquí viven los
    //  DIBUJOS del aura: los mismos métodos, INTACTOS byte a byte,
    //  mudados de AuraLib.cs por tools/parte_archivos_v6177.py — la
    //  clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. El MOTOR (Actualizar, emisores, CampoFbm) vive
    //  en AuraLib.cs.
    // =====================================================================
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.VFX
{
    public static partial class AuraLib
    {
        // ------------------------------------------------------------------
        //  EL RENDER — emisión de cuadros al búfer de VFXCore
        // ------------------------------------------------------------------

        /// <summary>
        /// Dibuja el aura de un NPC: emite TODOS los cuadros al búfer de
        /// VFXCore y lo vuelca ADITIVO. frontal=false → la capa trasera
        /// (llamar desde PreDraw, ANTES del sprite); frontal=true → el velo
        /// (PostDraw). Cierra el lote activo para volcar.
        ///
        /// DEVUELVE true si el lote del llamador fue CERRADO (y hay que
        /// reabrirlo con <see cref="ReabrirLoteVanilla"/> para que vanilla
        /// siga dibujando) — false si el búfer quedó vacío o hubo un error
        /// (en el error el lote ya se dejó ABIERTO aquí mismo).
        /// </summary>
        public static bool DibujarNPC(NPC npc, AuraPerfil p, bool frontal)
        {
            if (npc == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (frontal && !p.VeloFrontal) return false;

            // v6.50.23 — EL PATRÓN BRUMA camina por SU camino (la doble
            // fase: bruma ALFA + pila aditiva — el mismo contrato del bool).
            if (p.Patron == PatronAura.Bruma)
                return DibujarBrumaYPila(npc.Center, p.Radio * npcScale(npc), p, frontal, npc.whoAmI);

            try
            {
                float factor = npcScale(npc);
                Emitir(npc.Center, p.Radio * factor, p, frontal, npc.whoAmI);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false; // búfer vacío (sin textura aún / presupuesto): lote intacto
            }
            catch
            {
                // El contrato de rescate: el lote queda ABIERTO pase lo que
                // pase (si estaba cerrado se reabre; si estaba abierto, el
                // Begin falla sin tocar nada).
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        /// <summary>
        /// Dibuja el aura del JUGADOR por el camino DrawData (las capas de
        /// dibujado del jugador): emite los cuadros al búfer y los añade a
        /// la caché de DrawData del drawInfo — el pipeline oficial de tML,
        /// sin tocar el lote del renderer (el mismo contrato de las
        /// coronas de la casa).
        /// </summary>
        public static void DibujarJugador(ref PlayerDrawSet drawInfo, AuraPerfil p, bool frontal)
        {
            if (p == null) return;
            if (frontal && !p.VeloFrontal) return;
            try
            {
                Player pl = drawInfo.drawPlayer;
                if (pl == null || pl.dead) return;
                Emitir(pl.Center, p.Radio, p, frontal, 511, loteAlfa: true);
                VFXCore.AppendToPlayerDraw(ref drawInfo);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
        }
        /// <summary>
        /// v6.48 — EL CAMINO ADITIVO DEL JUGADOR (la mejora pedida: el
        /// camino del halo-proyectil). Los NPCs dibujan su aura ADITIVA
        /// (neón); el jugador iba por DrawData (AlphaBlend) y las auras
        /// de color vivos salían planas (la lección v6.40 de las manchas
        /// planas). ESTE método emite el aura del jugador al búfer y lo
        /// VUELCA ADITIVO — lo llama el PORTADOR (AuraPortadorHalo), un
        /// proyectil cosmético pegado al jugador: su PreDraw tiene el
        /// lote BAJO NUESTRO CONTROL y vanilla dibuja los proyectiles
        /// ANTES que los jugadores → la capa queda DETRÁS del cuerpo.
        /// El VELO FRONTAL sigue por DrawData (al 6% no necesita neón).
        /// DEVUELVE true si el lote del llamador fue CERRADO (reabrir
        /// con ReabrirLoteVanilla) — el contrato de la casa.
        /// </summary>
        public static bool DibujarJugadorAditivo(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            // v6.50.23 — EL PATRÓN BRUMA camina por SU camino (la doble
            // fase: bruma ALFA + pila aditiva — el mismo contrato del bool).
            if (p.Patron == PatronAura.Bruma)
                return DibujarBrumaYPila(pl.Center, p.Radio, p, frontal: false, 511);

            // v6.50.45 — EL PATRÓN DIVINO camina por SU camino (la Forma
            // Ascendida reescrita: el halo, el círculo rúnico y los
            // rayos — el mismo contrato del bool). v6.110: las alas de
            // la primera luz MURIERON por la letra del usuario.
            if (p.Patron == PatronAura.Divino)
                return DibujarDivino(pl, p);

            // v6.50.48 — EL PATRÓN DIVINO 2 (la Forma Ascendida 2: la
            // apoteosis absoluta — el mismo contrato del bool).
            if (p.Patron == PatronAura.Divino2)
                return DibujarDivino2(pl, p);

            // v6.50.53 — EL PATRÓN DIVINO 3 (la Forma Ascendida 3: EL
            // TRONO — el mismo contrato del bool). EL BUG DE LA .49→.52:
            // este caso NO ESTABA — DibujarDivino3 existía pero NADIE lo
            // llamaba y la Forma 3 caía al Emitir GENÉRICO (Perlin
            // plano): EL TRONO ENTERO — el nimbo, la cruz de luz, el mar
            // de vidrio, las lámparas, los ofanim, las alas prismáticas
            // y EL ARCOÍRIS de Ap 4:3 — JAMÁS se dibujaron («el arcoiris
            // deberia estar en la forma ascendida 3»). AHORA SÍ.
            if (p.Patron == PatronAura.Divino3)
                return DibujarDivino3(pl, p);

            // v6.50.53 — EL PATRÓN DIVINO 4 (la Forma Ascendida 4: EL
            // SERAFÍN — la cuarta luz, el que arde).
            if (p.Patron == PatronAura.Divino4)
                return DibujarDivino4(pl, p);

            // v6.112 — LOS PATRONES DIVINOS 5, 6, 7, 9 Y 10 MURIERON por la
            // letra del usuario («las otras formas 5, 6, 7, 9, 10 pueden ser
            // borradas») — sus despachos también.

            // v6.110/v6.111 — EL PATRÓN DIVINO 8 (la Forma Ascendida 8:
            // LAS OFANIM — la octava luz: las ruedas dentro de ruedas de
            // Ezequiel con sus ojos y sus runas — la que el usuario
            // conservó en la v6.112).
            if (p.Patron == PatronAura.Divino8)
                return DibujarDivino8(pl, p);

            // v6.50.54 — EL PATRÓN HALO ARCOÍRIS (el arcoíris de la Forma 3
            // destilado a adorno — el mismo contrato del bool).
            if (p.Patron == PatronAura.HaloArcoiris)
                return DibujarHaloArcoiris(pl, p);

            try
            {
                Emitir(pl.Center, p.Radio, p, frontal: false, 511);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        // ==================================================================
        //  v6.50.45 — EL PATRÓN DIVINO: LA FORMA ASCENDIDA REESCRITA
        //  (la petición: «La Forma Ascendida se sigue viendo simple, no
        //  es nada divina, además tiene 2 círculos de color plano
        //  semitransparentes lo que hace que se vea raro, investiga en
        //  internet y otros mods como darle una apariencia divina al
        //  jugador»). LA INVESTIGACIÓN (el lenguaje visual de lo divino
        //  en los juegos — halos de ángeles, god rays, alas de luz,
        //  círculos mágicos, ecos etéreos):
        //    · EL HALO DORADO sobre la cabeza — EL signo divino por
        //      excelencia: un aro inclinado de perlas con profundidad
        //      (las de delante brillan más) y 3 chispas girando en él.
        //    · LAS ALAS DE LUZ — dos abanicos de plumas de luz detrás
        //      del portador que se DESPLIEGAN al correr/volar (y
        //      respiran quietas — la presencia, no la anatomía).
        //    · EL CÍRCULO RÚNICO bajo los pies — la proyección achatada
        //      de un anillo de 16 segmentos girando + el anillo interno
        //      al revés (el magic circle de los hechiceros).
        //    · LOS RAYOS DIVINOS — columnas verticales de luz cayendo
        //      del cielo sobre el portador (crepusculares, sutiles).
        //    · LOS ECOS — tras el portador al moverse (el rastro etéreo).
        //    · LAS CHISPAS ESTELARES — cruces de luz parpadeando (el
        //      twinkle de lo sagrado).
        //  Y EL CUERPO: el flipbook Perlin suave de siempre (el respiro
        //  de luz — la base que SÍ funcionaba). CERO DISCOS PLANOS: el
        //  halo de glow plano (LOS 2 CÍRCULOS del reclamo) jamás se
        //  emite en este camino.
        // ==================================================================

        /// <summary>
        /// EL CAMINO ADITIVO DEL PATRÓN DIVINO (el contrato del bool de
        /// DibujarJugadorAditivo: devuelve true si el lote del llamador
        /// fue cerrado — reabrir con ReabrirLoteVanilla).
        /// </summary>
        private static bool DibujarDivino(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirDivino(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        /// <summary>
        /// v6.50.48 - EL CAMINO ADITIVO DEL PATRON DIVINO 2 (la Forma
        /// Ascendida 2: la apoteosis absoluta). El mismo contrato del
        /// bool de DibujarJugadorAditivo.
        /// </summary>
        private static bool DibujarDivino2(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirDivino2(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        // ==================================================================
        //  v6.50.49 — EL PATRÓN DIVINO 3: LA FORMA ASCENDIDA 3, EL TRONO
        //  (la letra: «agrega un 3 forma ascendida que se vea aun mas
        //  divino, investiga mods y busca en internet como es verse con
        //  divinidad celestial»). LA INVESTIGACIÓN — la iconografía del
        //  trono celestial, el lenguaje visual CANÓNICO de la divinidad
        //  (el que el arte sacro fijó y los juegos prestan — Diablo,
        //  Bayonetta, Final Fantasy):
        //    · «Y había un ARCOÍRIS ALREDEDOR DEL TRONO» (Ap 4:3) — DOS
        //      aros elípticos de perlas, cada una con SU color del
        //      espectro, contragirando alrededor del portador.
        //    · «Un MAR DE VIDRIO semejante al cristal» (Ap 4:6) — el
        //      suelo de cristal bajo los pies: la placa achatada + la
        //      retícula de destellos que titilan por hash.
        //    · «SIETE LÁMPARAS DE FUEGO ardiendo delante del trono»
        //      (Ap 4:5 — los siete espíritus de Dios) — siete llamas
        //      ORBITANDO al dios, cada una con su tempo.
        //    · «UNA RUEDA DENTRO DE OTRA RUEDA… y sus aros llenos de
        //      OJOS» (Ez 1 — los OFANIM, las ruedas del trono) — dos
        //      anillos de OJOS DE LUZ contrarrotando bajo los pies.
        //    · LA MAIESTAS DOMINI — LA CRUZ DE LUZ: la columna vertical
        //      del cielo + el brazo HORIZONTAL del horizonte, cruzando
        //      DETRÁS del dios (el mandorla cruzado del Cristo en
        //      gloria — el gesto divino por excelencia del arte sacro).
        //    · LA CORONA DE VEINTICUATRO ESTRELLAS (los veinticuatro
        //      ancianos, Ap 4:10) — tres arcos de ocho estrellas sobre
        //      el triple halo.
        //    · LAS ALAS PRISMÁTICAS — los CUATRO bancos de la Forma 2
        //      (28 plumas por lado, extensión completa de arriba abajo)
        //      con cada pluma llevando SU matiz del arcoíris: el serafín
        //      del trono. Y las plumas que caen SON arcoíris.
        // ==================================================================

        /// <summary>
        /// EL CAMINO ADITIVO DEL PATRÓN DIVINO 3 (EL TRONO) — el mismo
        /// contrato del bool de DibujarJugadorAditivo.
        /// </summary>
        private static bool DibujarDivino3(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirDivino3(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        // ==================================================================
        //  v6.50.53 — EL PATRÓN DIVINO 4: LA FORMA ASCENDIDA 4 (EL SERAFÍN)
        //
        //  La letra: «de hecho crea una 4 forma ascendida, asegurate de
        //  que sea algo divino, alas, halo, corona, aura celestial, luz,
        //  bruma, mas luz y destello, mejor iluminacion, para saber mas
        //  investiga en internet sobre el tema y mira otros mod».
        //
        //  LA INVESTIGACIÓN (Isaías 6 — la visión del templo, el PINÁCULO
        //  del lenguaje visual de la divinidad; el mismo que prestan
        //  Diablo, Bayonetta y Final Fantasy — verificado contra el arte
        //  sacro y los seis-alas del serafín de la iconografía):
        //    · EL SERAFÍN = «EL QUE ARDE» (saraph, la raíz hebrea): todo
        //      el ser es FUEGO — el cuerpo con lenguas de llama subiendo.
        //    · «SEIS ALAS TENÍA… CON DOS SE CUBRÍA EL ROSTRO, CON DOS SE
        //      CUBRÍA LOS PIES Y CON DOS VOLABA» (Is 6:2) — TRES PARES:
        //      el ALTO sobre la cabeza, el MEDIO en vuelo, el BAJO
        //      barriendo los pies. La firma inconfundible del serafín.
        //    · «EL UMBRAL SE CONMOVIÓ… Y LA CASA SE LLENÓ DE HUMO»
        //      (Is 6:4) — LA BRUMA SANTA: el velo de niebla luminosa
        //      alrededor (los god rays del cine sagrado).
        //    · EL TRISAGIÓN — «SANTO, SANTO, SANTO» (Is 6:3 · Ap 4:8): el
        //      HALO TRIPLE con sus tres órdenes de marcas girando.
        //    · LA CORONA DEL REY DE GLORIA (Ap 19:12 — «sobre su cabeza
        //      MUCHAS DIADEMAS»): el bedel de picos con sus joyas.
        //  Y LA LISTA DEL USUARIO, CUMPLIDA UNA A UNA: alas (las seis) ·
        //  halo (el triple mayor) · corona (la del Rey) · aura celestial
        //  (el nimbo mayor + el cuerpo que arde) · luz y MÁS LUZ (los
        //  rayos de Dios + la iluminación de mundo mayor de todas las
        //  formas — la pone CosmeticPlayer) · bruma (la santa) · destello
        //  (el del corazón + las chispas estelares) · mejor iluminación
        //  (todo lo anterior, junto, TODO aditivo).
        // ==================================================================

        // ==================================================================
        //  v6.50.54 — EL CAMINO ADITIVO DEL HALO ARCOÍRIS (el arcoíris de
        //  la Forma 3 destilado a adorno: la banda de siete franjas de
        //  VFXCore.Arcoiris en un ARO PEQUEÑO alrededor de la cabeza, con
        //  SUS doce perlas zodiacales de color, el rim blanco del borde y
        //  una atmósfera suave — nada más: la petición fue LITERAL,
        //  «SOLO ese arcoiris, pequeño, rodee la cabeza»).
        // ==================================================================

        /// <summary>
        /// EL CAMINO ADITIVO DEL PATRÓN HALO ARCOÍRIS — el mismo contrato
        /// del bool de DibujarJugadorAditivo.
        /// </summary>
        public static bool DibujarHaloArcoiris(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirHaloArcoiris(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        /// <summary>
        /// EL CAMINO ADITIVO DEL PATRÓN DIVINO 4 (EL SERAFÍN) — el mismo
        /// contrato del bool de DibujarJugadorAditivo.
        /// </summary>
        private static bool DibujarDivino4(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirDivino4(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        // ==================================================================
        //  v6.112 — LOS CAMINOS DE LAS LUCES 5, 6, 7, 9 Y 10 MURIERON por
        //  la letra del usuario («las otras formas 5, 6, 7, 9, 10 pueden
        //  ser borradas»): EmitirDivino5/6/7/9/10 y sus Dibujar se
        //  disolvieron con sus formas (EL RÍO DE PLUMAS, EL ESCRIBA, LA
        //  GALAXIA, LAS LÁMPARAS y EL TRONO). VIVEN los caminos de la 1,
        //  la 2, la 3, la 4 y la 8 (LAS OFANIM — la octava conservada).
        // ==================================================================

        // ==================================================================
        //  v6.110 → v6.111 — EL PATRÓN DIVINO 8 (la Forma Ascendida 8 —
        //  la octava luz, nacida LA ESPADA DEL JUICIO y RE-CREADA como
        //  LAS OFANIM: LAS RUEDAS DENTRO DE RUEDAS de la visión de
        //  Ezequiel 1). v6.112 — LA LUZ QUE EL USUARIO CONSERVÓ («la
        //  forma 1 y 8 se quedan») con DOS CIRUGÍAS de su letra:
        //    · LAS LENGÜETAS DE FUEGO (quads de ruido = RECTÁNGULOS)
        //      MURIERON CONVERTIDAS EN RUNAS — glifos de trazos sobre
        //      las llantas («convierte esos rectangulos pequeños en
        //      runas»).
        //    · EL ARO VERTICAL YA NO PRECESA: su plano quedó PLANTADO
        //      (azimut fijo — ya no camina alrededor del portador:
        //      «ese circulo… has que no gire»).
        // ==================================================================

        /// <summary>
        /// v6.110 — EL DIBUJO DE LAS OFANIM (la octava luz). El mismo
        /// contrato del bool que DibujarDivino4 (cierra el lote del
        /// llamador y devuelve true; en el error, ReabrirLoteVanilla).
        /// </summary>
        private static bool DibujarDivino8(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;

            try
            {
                EmitirDivino8(pl, p);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
        // ==================================================================
        //  v6.112 — LOS CAMINOS DE LAS LUCES 5, 6, 7, 9 Y 10 MURIERON por
        //  la letra del usuario («las otras formas 5, 6, 7, 9, 10 pueden
        //  ser borradas»): EmitirDivino5/6/7/9/10 y sus Dibujar se
        //  disolvieron con sus formas (EL RÍO DE PLUMAS, EL ESCRIBA, LA
        //  GALAXIA, LAS LÁMPARAS y EL TRONO). VIVEN los caminos de la 1,
        //  la 2, la 3, la 4 y la 8 (LAS OFANIM — la octava conservada).
        // ==================================================================
        // ==================================================================
        //  v6.50.23 — EL PATRÓN BRUMA: LA BRASA DEL ECLIPSE
        //  (la petición: «luz, bruma, desenfoque, distorsión, bloom, glow,
        //  ruido perlin — negra en los bordes, dorada en el medio y roja
        //  en el centro, todo por código» — la investigación R62-a)
        // ==================================================================

        /// <summary>
        /// v6.50.23 — EL DOBLE VOLCADO del patrón Bruma: la ÚNICA pila de
        /// la casa con una capa NO aditiva (la lección #1 de R62-a: en el
        /// lote aditivo negro = suma 0 = invisible — para oscurecer hay
        /// que SALIR del aditivo). FASE 1: LA BRUMA (humo negro-violeta)
        /// se vuelca con ALFA-blend OSCURECIENDO el mundo; FASE 2: la
        /// pila brillante (glow + núcleo + cuerpo + bloom + fantasmas +
        /// partículas) se vuelca ADITIVA encima — la separación
        /// humo/emisivo de Battlefront II (misma geometría, dos blends).
        /// CONTRATO: idéntico al de DibujarJugadorAditivo — DEVUELVE
        /// true si el lote del llamador fue CERRADO (reabrir con
        /// ReabrirLoteVanilla); en el error, el lote queda ABIERTO aquí.
        /// </summary>
        private static bool DibujarBrumaYPila(Vector2 centro, float radio, AuraPerfil p, bool frontal, int idEntidad)
        {
            try
            {
                // el presupuesto de la pila entera: bruma 2 + glow 2 +
                // núcleo 2 + cuerpo (1+bloom) + fantasmas 2 + partículas.
                if (!VFXCore.Presupuesto(10 + Math.Max(1, p.Bloom) + (p.Particulas?.Cantidad ?? 0)))
                    return false;

                bool cerroLote = false;

                // === FASE 1 — LA BRUMA NEGRA (alfa-blend): SOLO la capa
                // trasera — el frontal de los NPCs es aditivo (lo oscuro
                // no suma nada: el velo frontal va por DrawData/brasa) ===
                if (!frontal && p.AlfaBruma > 0.02f)
                {
                    EmitirBruma(centro, radio, p);
                    if (VFXCore.QuadCount > 0)
                    {
                        VFXCore.FlushAlpha(null, true); // cierra el lote del llamador
                        cerroLote = true;
                    }
                }

                // === FASE 2 — LA PILA BRILLANTE (aditiva) + partículas ===
                EmitirBrasa(centro, radio, p, frontal);
                EmitirParticulas(centro, p, frontal, idEntidad);
                if (VFXCore.QuadCount > 0)
                {
                    // (si la fase 1 ya cerró el lote del llamador, la sonda
                    // de CerrarLoteSiAbierto lo ignora — cero first-chance)
                    VFXCore.FlushAdditive(null, !cerroLote);
                    cerroLote = true;
                }
                return cerroLote;
            }
            catch
            {
                // el contrato de rescate de la casa: lote ABIERTO pase lo que pase.
                try { ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AuraLib", exRegistro); }
                return false;
            }
        }
    }
}
