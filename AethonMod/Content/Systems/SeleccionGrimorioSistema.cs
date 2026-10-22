using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  EL VAGABUNDO DEL SELECCIONADOR (v6.50.126) — el easteregg de la
    //  letra: «aunque como easteregg seria interesante que el minigrimorio
    //  mascota se ubica como las otras mascotas en el seleccionador de
    //  personaje, pero que despues de unos segundos, tal vez 5 este salga
    //  y se ponga a deambular por toda la pantalla atacando el titulo de
    //  terraria o cambiando el sol o la luna en el fondo de la pantalla
    //  de titulo desde el seleccionador de personaje».
    //
    //  v6.50.128 — EL VAGABUNDO VIVO · v6.50.129 — LA CURA DEL TELETRANSPORTE
    //  (MoverHacia + _posAnterior) · v6.50.130 — LA VALLA DE LA PANTALLA ·
    //  v6.50.131 — EL TÍTULO SE ATACA CON UN SOLO ATAQUE (la andanada de
    //  los minimiones) · EL ALETEO/EL BARREL ROLL/LA BRUMA DE PÁGINAS.
    //
    //  v6.51.3 — LA MANADA Y LA DANZA (la letra: «algo curioso, si hay
    //  mas de 2 vagabundos en la seleccion de personaje, deberian
    //  interactuar entre si, tambien mejora la animacion del vagabundo»):
    //
    //  · LA MANADA: el seleccionador puede mostrar VARIAS mascotas a la
    //    vez (una por personaje con su grimorio menor) — el singleton
    //    estático MURIÓ: cada recuadro que posa tiene SU PROPIO reloj de
    //    escape y su PROPIO vagabundo (el registro vive en _manada, la
    //    identidad del recuadro es su posición cuantizada). Con un solo
    //    libro: TODO como siempre (las fases, los actos, la curiosidad).
    //  · LAS INTERACCIONES (2+): el DIRECTOR DE LA MANADA alterna la
    //    deambulación libre con ACTOS DE MANADA —
    //      – EL CORRO: todos orbitan un centro común en puestos
    //        perfectamente repartidos (la danza geométrica), con un giro
    //        de campana SINCRONIZADO en cadena al despedirse;
    //      – LA PERSECUCIÓN: el líder deambula y los demás lo PERSIGUEN
    //        en tren (el rol de líder ROTA — la pídola del seleccionador);
    //      – EL CHOQUE (dos): cargas frente a frente con rebote y chispas
    //        (el topetazo de libros que se quieren).
    //    Y mientras UNO hace su acto solista (el título · el sol · la
    //    luna), los demás son ESPECTADORES: se acomodan en arco, MIRAN
    //    al actor (foco) y aletean — el público del crimen.
    //  · LA ANIMACIÓN MEJORADA: el ALETEO DE VUELO (las páginas baten
    //    X±5% cuando vuela rápido), el ATERRIZAJE CON BOTE (la escala Y
    //    se aplasta y rebota amortiguado al llegar a la pausa) y los
    //    GIROS SINCRONIZADOS del corro (la ola de campanas).
    //
    //  EL CIRCUITO (sin cambios): GrimorioMenorPet.PreDraw detecta el
    //  DUMMY de vista previa del seleccionador (isAPreviewDummy — lo crea
    //  UICharacter.DrawPets) y posa el libro EN SU RECUADRO como cualquier
    //  mascota. TickVistaPrevista (aquí) cuenta los segundos de pose de
    //  CADA recuadro. A los ~5 s (300 t) el libro ABANDONA su recuadro y
    //  ESTE sistema lo dibuja SOBRE TODA LA PANTALLA (hook a
    //  Main.PostDrawMenu).
    //
    //  v6.50.128 — EL LOTE PROPIO (la cura del error del client.log): un
    //  SpriteBatch DEDICADO (_lote) que jamás toca el de vanilla.
    // ======================================================================
    public class SeleccionGrimorioSistema : ModSystem
    {
        // === EL RELOJ DE LA POSE (la vista previa lo alimenta) ===

        /// <summary>Los ~5 segundos de pose en el recuadro (300 t a 60 fps).</summary>
        private const int UMBRAL_ESCAPE = 300;

        /// <summary>Fotogramas del menú sin vista previa que reinician todo.</summary>
        private const int FOTOS_MUERTOS = 90;

        // === LAS FASES DEL VAGABUNDO ===
        private const int FASE_ESCAPE = 0;    // sale del recuadro
        private const int FASE_VAGAR = 1;     // deriva orgánica con pausas vivas
        private const int FASE_CURIOSO = 2;   // estudia un punto de interés
        private const int FASE_IR = 3;        // vuela a su objetivo
        private const int FASE_ACTUAR = 4;    // el título / el sol / la luna
        private const int FASE_MANADA = 5;    // v6.51.3 — el acto de manada
        private const int FASE_ESPECTADOR = 6;// v6.51.3 — mira el acto de otro

        // === LOS ACTOS (qué hace al llegar) ===
        private const int ACTO_LOGO = 0;      // LA ANDANADA de minimiones
        private const int ACTO_SOL = 1;       // LOS ANILLOS RÚNICOS + el arrastre
        private const int ACTO_LUNA = 2;      // gira la fase de la luna

        // === LOS ACTOS DE MANADA (v6.51.3) ===
        private const int MANADA_CORRO = 0;   // la danza del corro
        private const int MANADA_PERSECUCION = 1; // la pídola
        private const int MANADA_CHOQUE = 2;  // el topetazo (dos)

        // ================================================================
        //  v6.51.3 — EL VAGABUNDO INDIVIDUAL (todo el estado que era
        //  estático, ahora es SUYO: cada libro del seleccionador tiene
        //  el suyo).
        // ================================================================
        private class Vagabundo
        {
            public Vector2 Ancla;             // el recuadro del que nació
            public Vector2 Pos, Vel, PosAnterior;
            public int Fase = FASE_ESCAPE;
            public int TFase;
            public int Pausa, TMirada;
            public Vector2 Foco;
            public Vector2 Objetivo, Reposo;
            public bool ReposoIzquierda;
            public int Acto, ActoProximo = ACTO_LOGO, Caminatas, GolpesLogo, TActo;
            // LA CURIOSIDAD
            public int CurioTipo, TCurio;
            public float AngCurio;
            public Vector2 PuntoCurio, CursorAlLlegar;
            public bool YaToque, Asustado;
            // LAS ANIMACIONES VIVAS
            public float GiroExtra;
            public int Aleteo, TRecoil;
            // v6.51.3 — LA DANZA Y EL TEATRO
            public float FaseCorro;           // su puesto en el corro
            public int IdxManada;             // su número en la manada
            public int TEspectador;           // el reloj del público
            // v6.51.3 — SU RASTRO (la historia de ESTE libro — los
            // fantasmas del vuelo rápido son SUYOS, no compartidos).
            public readonly Vector2[] Rastro = new Vector2[7];
            public int IdxRastro;

            /// <summary>v6.51.3 — EL ALETEO DE VUELO: las páginas BATEN
            /// (X±5% a 3 Hz) cuando vuela rápido — el libro no se
            /// desliza rígido: ALETEA.</summary>
            public float AleteoVuelo(float t)
            {
                float v = Vel.Length();
                if (v < 4f) return 1f;
                float k = MathHelper.Clamp((v - 4f) / 6f, 0f, 1f);
                return 1f + 0.05f * k * MathF.Sin(t * 3f * MathHelper.Pi + IdxManada * 1.3f);
            }

            /// <summary>v6.51.3 — EL ATERRIZAJE CON BOTE: al ARRANCAR una
            /// pausa la escala Y se APLASTA (0,88) y rebota amortiguado —
            /// el libro ATERRIZA, no se congela.</summary>
            public float BoteAterrizaje(float t)
            {
                if (Pausa <= 0) return 1f;
                float tp = Pausa;
                float b = MathF.Sin(tp * 0.85f) * MathF.Exp(-tp * 0.10f);
                return 1f - 0.12f * b;
            }
        }

        /// <summary>v6.51.3 — LA MANADA: todos los vagabundos vivos (uno
        /// por recuadro que ya escapó). Con UNO solo, todo es como
        /// siempre; con DOS o más, el director los hace interactuar.</summary>
        private static readonly List<Vagabundo> _manada = new();

        /// <summary>v6.51.3 — LOS ANCLAJES: cada recuadro que posa, con SU
        /// reloj de pose (la identidad es la posición cuantizada — cada
        /// tarjeta del seleccionador tiene la suya).</summary>
        private static readonly List<(Vector2 pos, int ticks, Vagabundo dueno)> _poses = new();

        private static int _fotosSinPrevista;

        // === EL DIRECTOR DE LA MANADA (v6.51.3) ===
        /// <summary>El acto de manada en curso (−1 = ninguno).</summary>
        private static int _actoManada = -1;
        /// <summary>El reloj del acto de manada.</summary>
        private static int _tManada;
        /// <summary>El descanso entre actos de manada (la manada deambula).</summary>
        private static int _descansoManada = 420;
        /// <summary>El centro del corro / el líder de la persecución.</summary>
        private static Vector2 _centroManada;
        private static int _liderManada;
        /// <summary>¿Quién hace el acto solista? (−1 nadie).</summary>
        private static int _actorActual = -1;

        // === LAS MANCHAS DEL LOGO (la tinta acumulada del título) ===
        private struct Mancha
        {
            public Vector2 Pos;
            public float Tam;
            public float Alfa;
            public float Nace;      // el instante del golpe (el "plop" inicial)
            public float Giro;      // v6.50.128 — cada mancha su charquito inclinado
        }

        private static readonly List<Mancha> _manchas = new();

        // === LAS CHISPAS DE TINTA (partículas de pantalla de los impactos) ===
        private struct Chispa
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Vida;
            public float Tam;
            public bool Violeta;
        }

        private static readonly List<Chispa> _chispas = new();

        // === LOS VIROTES DE LOS MINIMIONES (la andanada del título) ===
        private struct Virote
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public int Llega;       // ticks hasta el impacto
            public Vector2 Objetivo;
        }

        private static readonly List<Virote> _virotes = new();

        // === LOS PUFFS DE BRUMA (la estela de niebla del vagabundo) ===
        private struct Puff
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Vida;
            public float Tam;
        }

        private static readonly List<Puff> _puffs = new();

        // === v6.50.128 — LAS POSICIONES DE LA CORONA durante la andanada ===
        private static readonly Vector2[] _ojosActo = new Vector2[3];

        // === v6.50.128 — EL LOTE PROPIO (la cura del tanteo del client.log) ===
        private static SpriteBatch _lote;

        // ================================================================
        //  EL CICLO DE VIDA DEL HOOK
        // ================================================================

        public override void Load()
        {
            // EL ÚLTIMO PLANO DEL MENÚ: PostDrawMenu corre cuando TODO el
            // menú ya se dibujó (fondo + sol/luna + logo + lista) — el
            // vagabundo vuela POR ENCIMA de todo (y sus manchas caen
            // sobre el logo ya pintado).
            On_Main.PostDrawMenu += DibujarSobreElMenu;
        }

        public override void Unload()
        {
            On_Main.PostDrawMenu -= DibujarSobreElMenu;
            Reiniciar();
            _virotes.Clear();
            _puffs.Clear();
            _poses.Clear();
            _manada.Clear();
            _actoManada = -1;
            _actorActual = -1;
            try { _lote?.Dispose(); } catch { }
            _lote = null;
        }

        private static void DibujarSobreElMenu(Terraria.On_Main.orig_PostDrawMenu orig,
            Point tamanoCache, Point tamanoCacheEscalado)
        {
            orig(tamanoCache, tamanoCacheEscalado);
            try { Pintar(); }
            catch { }
        }

        // ================================================================
        //  EL RELOJ DE LA POSE (lo llama GrimorioMenorPet por cada dummy
        //  de vista previa que el seleccionador muestra)
        // ================================================================

        /// <summary>La vista previa posa mientras esto devuelva TRUE (los
        /// ~5 s de la letra); después el libro «se fue» y SU vagabundo de
        /// arriba toma el control. v6.51.3 — cada RECUADRO tiene su propio
        /// reloj: varios personajes con la mascota = varios vagabundos.</summary>
        public static bool TickVistaPrevista(Vector2 ancla)
        {
            _fotosSinPrevista = 0;

            // ¿Es un recuadro CONOCIDO? (la identidad: la posición —
            // cuantizada a 40 px, con histéresis de 56 px para el bobbing
            // de la mascota posada).
            int conocido = -1;
            for (int i = 0; i < _poses.Count; i++)
            {
                if (Vector2.DistanceSquared(_poses[i].pos, ancla) < 56f * 56f)
                { conocido = i; break; }
            }
            if (conocido < 0)
            {
                // Un recuadro NUEVO: su reloj nace (y con él, su vagabundo).
                var dueno = new Vagabundo { Ancla = ancla };
                _poses.Add((ancla, 0, dueno));
                conocido = _poses.Count - 1;
            }
            else
            {
                // El recuadro sigue vivo: su ancla se refresca (la tarjeta
                // puede moverse al hacer scroll) y su reloj avanza.
                var p = _poses[conocido];
                p.pos = ancla;
                p.ticks++;
                _poses[conocido] = p;
            }
            return _poses[conocido].ticks < UMBRAL_ESCAPE;
        }

        /// <summary>Todo vuelve a cero (otra pantalla, otro día: los libros
        /// vuelven a posar 5 s en sus recuadros antes de escaparse).</summary>
        private static void Reiniciar()
        {
            _poses.Clear();
            _manada.Clear();
            _actoManada = -1;
            _actorActual = -1;
            _descansoManada = 420;
            _fotosSinPrevista = 0;
            _manchas.Clear();
            _chispas.Clear();
        }

        // ================================================================
        //  EL MOTOR — un fotograma de la manada (update + draw)
        // ================================================================

        private static void Pintar()
        {
            // ¿Sigue viva alguna vista previa? (el seleccionador con las
            // mascotas a la vista). Si lleva 1,5 s muerta: TODO se reinicia.
            _fotosSinPrevista++;
            if (_fotosSinPrevista > FOTOS_MUERTOS)
            {
                Reiniciar();
                return;
            }

            // v6.51.3 — LA MANADA SE SIRVE: cada pose cumplida suelta a SU
            // vagabundo (los que ya escaparon siguen hasta el final).
            for (int i = _poses.Count - 1; i >= 0; i--)
            {
                var p = _poses[i];
                if (p.ticks >= UMBRAL_ESCAPE && !_manada.Contains(p.dueno))
                {
                    p.dueno.Pos = p.pos;
                    p.dueno.PosAnterior = p.pos;
                    p.dueno.Fase = FASE_ESCAPE;
                    p.dueno.TFase = 0;
                    _manada.Add(p.dueno);
                }
            }
            if (_manada.Count == 0) return;

            // LOS NÚMEROS DE LA MANADA (sus índices — para el corro, la
            // persecución y los espectadores).
            for (int i = 0; i < _manada.Count; i++)
                _manada[i].IdxManada = i;

            ActualizarDirector();
            Actualizar();
            Dibujar();
        }

        // ================================================================
        //  v6.51.3 — EL DIRECTOR DE LA MANADA: decide cuándo la manada
        //  hace UN ACTO JUNTO y quién hace los actos solistas. Con un
        //  solo vagabundo, TODO como siempre (nunca actos de manada).
        // ================================================================
        private static void ActualizarDirector()
        {
            // EL ACTOR SOLISTA: si nadie actúa y no hay acto de manada,
            // el PRÓXIMO vagabundo libre que decida actuar será EL actor.
            if (_actorActual >= 0 &&
                (_actorActual >= _manada.Count ||
                 _manada[_actorActual].Fase != FASE_ACTUAR))
            {
                _actorActual = -1;   // el acto terminó
            }

            // SIN MANADA SUFICIENTE: jamás actos de manada (la soledad
            // de siempre — ni el corro ni la pídola existen con uno).
            if (_manada.Count < 2)
            {
                if (_actoManada >= 0) TerminarActoManada();
                return;
            }

            // EL RELOJ DEL ACTO DE MANADA
            if (_actoManada >= 0)
            {
                _tManada++;
                int duracion = _actoManada == MANADA_CORRO ? 300
                    : _actoManada == MANADA_PERSECUCION ? 480 : 300;
                if (_tManada >= duracion)
                    TerminarActoManada();
                return;
            }

            // EL DESCANSO: la manada deambula libre…
            _descansoManada--;
            if (_descansoManada > 0) return;

            // …y entonces, EL ACTO DE MANADA — pero SOLO si TODOS están
            // libres (nadie volando a un acto ni en plena función: el
            // arrastre del sol JAMÁS se interrumpe a medias — el cielo
            // quedaría vestido).
            bool todosLibres = true;
            for (int i = 0; i < _manada.Count; i++)
            {
                int f = _manada[i].Fase;
                if (f != FASE_VAGAR && f != FASE_CURIOSO && f != FASE_ESPECTADOR)
                { todosLibres = false; break; }
            }
            if (!todosLibres) { _descansoManada = 120; return; }

            _actoManada = _manada.Count == 2
                ? (Main.rand.NextBool() ? MANADA_CHOQUE : MANADA_PERSECUCION)
                : (Main.rand.NextBool() ? MANADA_CORRO : MANADA_PERSECUCION);
            _tManada = 0;
            _liderManada = Main.rand.Next(_manada.Count);
            _centroManada = new Vector2(
                Main.screenWidth * Main.rand.NextFloat(0.3f, 0.7f),
                Main.screenHeight * Main.rand.NextFloat(0.25f, 0.55f));

            // TODOS al acto (los puestos del corro repartidos al aire).
            for (int i = 0; i < _manada.Count; i++)
            {
                var v = _manada[i];
                v.Fase = FASE_MANADA;
                v.TFase = 0;
                v.Pausa = 0;
                v.FaseCorro = i / (float)_manada.Count * MathHelper.TwoPi;
                v.Objetivo = _centroManada;
            }
        }

        /// <summary>El acto de manada termina: todos vuelven a deambular.</summary>
        private static void TerminarActoManada()
        {
            if (_actoManada == MANADA_CORRO)
            {
                // LA DESPEDIDA DEL CORRO: el GIRO DE CAMPANA SINCRONIZADO —
                // una OLA que recorre la manada (cada libro su pirueta,
                // escalonadas 3 frames: la danza se despide coordianda).
                for (int i = 0; i < _manada.Count; i++)
                {
                    var v = _manada[i];
                    v.GiroExtra = (i % 2 == 0) ? MathHelper.TwoPi : -MathHelper.TwoPi;
                    v.Vel = new Vector2(0f, -2.5f - i * 0.3f);
                }
            }
            _actoManada = -1;
            _descansoManada = 360 + Main.rand.Next(360);
            for (int i = 0; i < _manada.Count; i++)
            {
                var v = _manada[i];
                if (v.Fase == FASE_MANADA || v.Fase == FASE_ESPECTADOR)
                {
                    v.Fase = FASE_VAGAR;
                    v.TFase = 0;
                    v.Caminatas = 0;
                    EmpezarPausa(v);
                }
            }
        }

        // ================================================================
        //  LA ACTUALIZACIÓN — los FX comunes + cada vagabundo
        // ================================================================

        private static void Actualizar()
        {
            const float dt = 1f / 60f;

            // LAS CHISPAS viven su vida (pantalla, siempre).
            for (int i = _chispas.Count - 1; i >= 0; i--)
            {
                Chispa c = _chispas[i];
                c.Pos += c.Vel;
                c.Vel *= 0.94f;
                c.Vel.Y += 0.05f;
                c.Vida -= dt;
                if (c.Vida <= 0f) _chispas.RemoveAt(i);
                else _chispas[i] = c;
            }

            // LOS PUFFS de bruma se desgarran y mueren.
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                Puff p = _puffs[i];
                p.Pos += p.Vel;
                p.Vel *= 0.92f;
                p.Vida -= dt;
                if (p.Vida <= 0f) _puffs.RemoveAt(i);
                else _puffs[i] = p;
            }

            // LOS VIROTES vuelan hacia su cita con el título.
            for (int i = _virotes.Count - 1; i >= 0; i--)
            {
                Virote v = _virotes[i];
                v.Pos += v.Vel;
                v.Llega--;
                if (v.Llega <= 0)
                {
                    Manchar(v.Objetivo, 0.8f);
                    Rafaga(v.Objetivo, 8);
                    _virotes.RemoveAt(i);
                }
                else _virotes[i] = v;
            }

            // CADA VAGABUNDO vive su fotograma.
            for (int i = 0; i < _manada.Count; i++)
            {
                var v = _manada[i];
                v.TFase++;
                v.PosAnterior = v.Pos;
                ActualizarVagabundo(v, dt);
                // v6.50.129 — LA VELOCIDAD VERDADERA (el desplazamiento real
                // del frame — para las fases que fijan la posición a mano).
                v.Vel = v.Pos - v.PosAnterior;

                // LA VALLA DE LA PANTALLA (v6.50.130 — la letra: «se sale de
                // la pantalla a veces»): TODO el teatro vive DENTRO del
                // monitor, con rebote suave contra el marco.
                const float margenPantalla = 26f;
                if (v.Pos.X < margenPantalla)
                { v.Pos.X = margenPantalla; if (v.Vel.X < 0f) v.Vel.X *= -0.35f; }
                if (v.Pos.Y < margenPantalla)
                { v.Pos.Y = margenPantalla; if (v.Vel.Y < 0f) v.Vel.Y *= -0.35f; }
                if (v.Pos.X > Main.screenWidth - margenPantalla)
                { v.Pos.X = Main.screenWidth - margenPantalla; if (v.Vel.X > 0f) v.Vel.X *= -0.35f; }
                if (v.Pos.Y > Main.screenHeight - margenPantalla)
                { v.Pos.Y = Main.screenHeight - margenPantalla; if (v.Vel.Y > 0f) v.Vel.Y *= -0.35f; }
            }
        }

        private static void ActualizarVagabundo(Vagabundo v, float dt)
        {
            switch (v.Fase)
            {
                case FASE_ESCAPE:
                {
                    // EL LIBRO SE ESCAPA: del recuadro a un punto libre de
                    // la pantalla (el despegue — la letra: «este salga»).
                    if (v.TFase == 1)
                    {
                        v.Objetivo = new Vector2(
                            Main.rand.NextFloat(Main.screenWidth * 0.15f, Main.screenWidth * 0.85f),
                            Main.rand.NextFloat(Main.screenHeight * 0.2f, Main.screenHeight * 0.55f));
                        v.Pausa = 0;
                    }
                    Volar(v, v.Objetivo, 0.13f, 8.5f, dt);
                    if (Vector2.DistanceSquared(v.Pos, v.Objetivo) < 30f * 30f || v.TFase > 150)
                    {
                        v.Fase = FASE_VAGAR;
                        v.TFase = 0;
                        v.Caminatas = 0;
                        EmpezarPausa(v);
                    }
                    break;
                }

                case FASE_VAGAR:
                {
                    // ¿HAY UN ACTO DE MANADA ARRANCANDO? El director ya
                    // cambió las fases — este caso no debería verse.
                    if (_actoManada >= 0) { v.Fase = FASE_MANADA; v.TFase = 0; break; }

                    // ¿OTRO ESTÁ ACTUANDO? A SER PÚBLICO (v6.51.3 — la
                    // letra: «deberian interactuar entre si»).
                    if (_actorActual >= 0 && _actorActual != v.IdxManada && _actorActual < _manada.Count)
                    {
                        v.Fase = FASE_ESPECTADOR;
                        v.TFase = 0;
                        v.TEspectador = 0;
                        break;
                    }

                    // DEAMBULAR VIVO: curvas orgánicas, pausas que miran
                    // alrededor… y de tanto en tanto, LA CURIOSIDAD.
                    if (v.Pausa > 0)
                    {
                        v.Pausa--;
                        v.Vel *= 0.82f;
                        v.Pos += v.Vel;
                        v.TMirada++;
                        if (v.TMirada % 22 == 0)
                        {
                            v.Foco = v.Pos + new Vector2(
                                Main.rand.NextFloat(-150f, 150f),
                                Main.rand.NextFloat(-110f, 110f));
                        }
                        if (v.Pausa == 0) NuevoRumbo(v);
                        break;
                    }

                    Volar(v, v.Objetivo, 0.10f, 4.6f, dt);
                    v.Foco = v.Pos + v.Vel * 30f;

                    if (Vector2.DistanceSquared(v.Pos, v.Objetivo) < 28f * 28f || v.TFase > 300)
                    {
                        v.Caminatas++;
                        if (v.Caminatas >= 2 && Main.rand.NextBool(2))
                        {
                            // LA CURIOSIDAD GANA: un punto de interés lo
                            // llama antes que el próximo acto.
                            ElegirCuriosidad(v);
                            v.Fase = FASE_CURIOSO;
                            v.TFase = 0;
                            v.TCurio = 0;
                            v.AngCurio = Main.rand.NextFloat(MathHelper.TwoPi);
                            v.YaToque = false;
                            v.Asustado = false;
                            break;
                        }
                        if (v.Caminatas >= 2)
                        {
                            ElegirActo(v);
                            v.Fase = FASE_IR;
                            v.TFase = 0;
                        }
                        else EmpezarPausa(v);
                    }
                    break;
                }

                case FASE_CURIOSO:
                {
                    // LA CURIOSIDAD: vuela EN ARCO hasta su punto, lo
                    // RODEA estudiándolo (los ojos CLAVADOS en él), le da
                    // un TOQUE… y si el punto era el CURSOR y éste se movió
                    // mucho mientras lo estudiaba: SUSTO y a volar.
                    v.TCurio++;
                    v.Foco = v.PuntoCurio;

                    if (v.TCurio < 80)
                    {
                        Volar(v, v.PuntoCurio + new Vector2(0f, -34f), 0.16f, 6.5f, dt);
                        if (v.CurioTipo == 0)
                            v.CursorAlLlegar = new Vector2(Main.mouseX, Main.mouseY);
                        break;
                    }

                    // LA VUELTA: órbita de estudio (~2,4 s) — SIN teleport:
                    // la órbita se PERSIGUE con catch-up suave.
                    float velAng = v.Asustado ? 0.30f : 0.055f;
                    v.AngCurio += velAng;
                    float radio = 40f + MathF.Sin(v.TCurio * 0.05f) * 7f;
                    Vector2 deseo = v.PuntoCurio + new Vector2(MathF.Cos(v.AngCurio) * radio,
                        MathF.Sin(v.AngCurio) * radio * 0.86f - 6f);
                    v.Pos += (deseo - v.Pos) * 0.26f;

                    // ¿EL CURSOR SE MOVIÓ DEMASIADO? (el susto).
                    if (v.CurioTipo == 0 && !v.Asustado &&
                        Vector2.Distance(v.CursorAlLlegar, new Vector2(Main.mouseX, Main.mouseY)) > 150f)
                    {
                        v.Asustado = true;
                        Vector2 huida = v.Pos - v.PuntoCurio;
                        if (huida.LengthSquared() < 0.1f) huida = -Vector2.UnitY;
                        v.Vel = Vector2.Normalize(huida) * 10.5f;
                        Rafaga(v.Pos, 7);
                        v.TCurio = 130;
                    }

                    // EL TOQUE (a mitad de la vuelta, una sola vez).
                    if (!v.YaToque && v.TCurio == 110 && !v.Asustado)
                    {
                        v.YaToque = true;
                        Rafaga(v.PuntoCurio, 5);
                        v.Vel = (v.PuntoCurio - v.Pos).SafeNormalize(Vector2.UnitX) * 3.5f;
                    }

                    if (v.TCurio >= 150)
                    {
                        v.Fase = FASE_VAGAR;
                        v.TFase = 0;
                        EmpezarPausa(v);
                    }
                    break;
                }

                case FASE_IR:
                {
                    // v6.51.3 — LA FUNCIÓN ES DE UNO SOLO: si OTRO llegó
                    // primero al papel de actor, este libro se va de
                    // público (a mirar la función del hermano).
                    if (_actorActual >= 0 && _actorActual != v.IdxManada &&
                        _actorActual < _manada.Count)
                    {
                        v.Fase = FASE_ESPECTADOR;
                        v.TFase = 0;
                        v.TEspectador = 0;
                        break;
                    }
                    // VOLAR AL ACTO: el objetivo se recalcula CADA frame si
                    // es el sol o la luna (se MUEVEN — el cielo del título
                    // gira sin parar).
                    if (v.Acto == ACTO_SOL) v.Objetivo = PosicionDelSol();
                    else if (v.Acto == ACTO_LUNA) v.Objetivo = PosicionDeLaLuna();
                    Volar(v, v.Objetivo, 0.14f, 7.5f, dt);
                    v.Foco = v.Objetivo;
                    if (Vector2.DistanceSquared(v.Pos, v.Objetivo) < 26f * 26f)
                    {
                        v.Fase = FASE_ACTUAR;
                        v.TFase = 0;
                        v.TActo = 0;
                        v.GolpesLogo = 2;
                        _actorActual = v.IdxManada;   // v6.51.3 — ES EL ACTOR
                    }
                    break;
                }

                case FASE_ACTUAR:
                {
                    v.TActo++;
                    if (v.Acto == ACTO_LOGO) ActuarLogo(v);
                    else if (v.Acto == ACTO_SOL) ActuarSol(v);
                    else ActuarLuna(v);
                    break;
                }

                case FASE_MANADA:
                {
                    // v6.51.3 — LOS ACTOS DE MANADA (la letra: «deberian
                    // interactuar entre si»).
                    v.Foco = _centroManada;
                    if (_actoManada == MANADA_CORRO)
                    {
                        // === EL CORRO: la danza geométrica — todos orbitan
                        //     el centro común en puestos PERFECTAMENTE
                        //     repartidos, al MISMO compás, con el radio
                        //     respirando JUNTO (la sincronía total). ===
                        float n = _manada.Count;
                        float giro = _tManada * 0.024f;             // el compás común
                        float radio = 78f + 16f * MathF.Sin(_tManada * 0.045f);
                        float ang = v.FaseCorro + giro;
                        Vector2 deseo = _centroManada + new Vector2(
                            MathF.Cos(ang) * radio,
                            MathF.Sin(ang) * radio * 0.82f);
                        MoverHacia(v, deseo, 0.20f);

                        // EL ALETEO ORDENADO: cada libro aletea a su turno
                        // (la ola alrededor del corro).
                        if ((_tManada + v.IdxManada * 9) % (int)(n * 9) == 0 && v.Pausa <= 0)
                            v.Aleteo = 18;
                        if (v.Pausa > 0) v.Pausa--;
                    }
                    else if (_actoManada == MANADA_PERSECUCION)
                    {
                        // === LA PÍDOLA: el líder deambula orgánico y los
                        //     demás lo PERSIGUEN en tren (el rol ROTA cada
                        //     4 s — todos corren, todos escapan). ===
                        var lider = _manada[_liderManada];
                        if (v.IdxManada == _liderManada)
                        {
                            if (v.Pausa > 0) v.Pausa--;
                            if (v.Objetivo == Vector2.Zero ||
                                Vector2.DistanceSquared(v.Pos, v.Objetivo) < 40f * 40f ||
                                v.TFase > 150)
                            {
                                v.Objetivo = new Vector2(
                                    Main.rand.NextFloat(60f, Main.screenWidth - 60f),
                                    Main.rand.NextFloat(70f, Main.screenHeight - 70f));
                                v.TFase = 0;
                                // EL BARREL ROLL DEL LÍDER (1 de cada 4 rumbos).
                                if (Main.rand.Next(4) == 0)
                                    v.GiroExtra = Main.rand.NextBool() ? MathHelper.TwoPi : -MathHelper.TwoPi;
                            }
                            Volar(v, v.Objetivo, 0.13f, 7.2f, dt);
                            v.Foco = v.Pos + v.Vel * 30f;
                        }
                        else
                        {
                            // EL PERSEGUIDOR: apunta DETRÁS del líder (su
                            // puesto en el tren, escalonado en abanico).
                            float n = _manada.Count;
                            float ang = MathHelper.TwoPi * v.IdxManada / n + _tManada * 0.01f;
                            Vector2 puesto = lider.Pos + new Vector2(
                                MathF.Cos(ang) * 46f, MathF.Sin(ang) * 38f);
                            Volar(v, puesto, 0.16f, 8.0f, dt);
                            v.Foco = lider.Pos;
                        }
                        // EL RELEVO: cada 4 s, el líder pasa la mano.
                        if (_tManada % 240 == 0 && _manada.Count > 1)
                            _liderManada = (_liderManada + 1) % _manada.Count;
                    }
                    else
                    {
                        // === EL CHOQUE (la pareja): cargas frente a frente,
                        //     TOPETAZO con chispas y rebote, pausa de
                        //     asombro… y OTRA VEZ (tres rondas). ===
                        var otro = _manada[v.IdxManada == 0 ? 1 : 0];
                        v.Foco = otro.Pos;
                        // LAS RONDAS: 0..2 cargan, la 3 se despiden.
                        int ronda = Math.Min(_tManada / 100, 3);
                        int tRonda = _tManada % 100;
                        if (ronda >= 3)
                        {
                            // LA DESPEDIDA: cada uno a su lado, con una
                            // reverencia de campana.
                            if (tRonda == 1)
                            {
                                v.GiroExtra = v.IdxManada == 0 ? MathHelper.TwoPi : -MathHelper.TwoPi;
                                v.Vel = new Vector2(v.IdxManada == 0 ? -4f : 4f, -2f);
                            }
                            v.Pos += v.Vel;
                            v.Vel *= 0.96f;
                            break;
                        }
                        if (tRonda < 70)
                        {
                            // LA CARGA: directo al otro (aceleración de
                            // embestida — el libro se LLENA de propósito).
                            Vector2 hacia = otro.Pos - v.Pos;
                            float d = hacia.Length();
                            if (d > 1f)
                                v.Vel += Vector2.Normalize(hacia) * (0.5f + d * 0.004f);
                            if (v.Vel.Length() > 9f) v.Vel = Vector2.Normalize(v.Vel) * 9f;
                            v.Pos += v.Vel;
                        }
                        else if (tRonda < 100)
                        {
                            // EL REBOTE (el golpe lo marcan los dos con el
                            // cruce: la chispa sale UNA vez por ronda).
                            if (tRonda == 70 && v.IdxManada == 0)
                            {
                                Vector2 medio = (v.Pos + otro.Pos) * 0.5f;
                                Rafaga(medio, 12);
                                Manchar(medio, 0.6f);
                            }
                            v.Pos += v.Vel;
                            v.Vel *= 0.90f;
                        }
                        else v.Vel *= 0.9f;
                    }
                    break;
                }

                case FASE_ESPECTADOR:
                {
                    // v6.51.3 — EL PÚBLICO: mientras OTRO hace su acto, los
                    // demás se acomodan en un arco a distancia de teatro,
                    // MIRAN al actor y reaccionan (aleteo, chispitas).
                    var actor = _actorActual >= 0 && _actorActual < _manada.Count
                        ? _manada[_actorActual] : null;
                    if (actor == null)
                    {
                        v.Fase = FASE_VAGAR;
                        v.TFase = 0;
                        EmpezarPausa(v);
                        break;
                    }
                    v.TEspectador++;
                    v.Foco = actor.Pos;

                    // SU BUTACA: el arco alrededor del actor (del lado
                    // contrario a su objetivo — siempre con vista).
                    float n = _manada.Count;
                    float ang = MathHelper.Pi * 0.5f + v.IdxManada / n * MathHelper.Pi;
                    Vector2 butaca = actor.Pos +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * 190f;
                    // (la butaca vive dentro de la pantalla — clamp suave)
                    butaca.X = MathHelper.Clamp(butaca.X, 60f, Main.screenWidth - 60f);
                    butaca.Y = MathHelper.Clamp(butaca.Y, 60f, Main.screenHeight - 60f);
                    MoverHacia(v, butaca + new Vector2(0f, MathF.Sin(
                        v.TEspectador * 0.05f + v.IdxManada * 1.7f) * 8f), 0.10f);

                    // LA REACCIÓN: aleteos de vez en cuando (el público
                    // comenta) y una chispita en los impactos del título.
                    if (Main.rand.Next(240) == 0) v.Aleteo = 22;
                    if (actor.Acto == ACTO_LOGO && _virotes.Count == 0 &&
                        Main.rand.Next(180) == 0)
                        Rafaga(v.Pos + new Vector2(0f, -10f), 2);
                    break;
                }
            }

            // LAS ANIMACIONES VIVAS: el barrel roll se apaga solo
            // (exponencial), el aleteo cuenta sus 26 t y el retroceso
            // de la andanada decae.
            v.GiroExtra *= 0.94f;
            if (MathF.Abs(v.GiroExtra) < 0.015f) v.GiroExtra = 0f;
            if (v.TRecoil > 0) v.TRecoil--;
            if (v.Aleteo > 0)
            {
                v.Aleteo--;
                if (v.Aleteo == 13) Rafaga(v.Pos + new Vector2(0f, -8f), 3);
            }

            // LA ESTELA DE BRUMA: volando rápido, el vagabundo gotea
            // mechones de niebla de sus páginas.
            if (v.Vel.LengthSquared() > 26f && (v.TFase & 3) == 0)
                _puffs.Add(new Puff
                {
                    Pos = v.Pos + Main.rand.NextVector2Unit() * 7f,
                    Vel = -v.Vel * 0.14f + new Vector2(0f, 0.3f),
                    Vida = Main.rand.NextFloat(0.4f, 0.85f),
                    Tam = Main.rand.NextFloat(11f, 24f)
                });
        }

        /// <summary>v6.50.129 — LA CURA DEL TELETRANSPORTE: PERSIGUIR un
        /// destino en vez de escribirlo de golpe (avance exponencial suave).</summary>
        private static void MoverHacia(Vagabundo v, Vector2 destino, float mezcla)
        {
            v.Pos += (destino - v.Pos) * mezcla;
        }

        /// <summary>LA PAUSA VIVA: 0,7–1,8 s de flotación mirando la pantalla.</summary>
        private static void EmpezarPausa(Vagabundo v)
        {
            v.Pausa = Main.rand.Next(42, 108);
            v.TMirada = 0;
            v.TFase = 0;
            // EL ALETEO: 1 de cada 3 pausas, el libro SACUDE sus páginas.
            if (Main.rand.Next(3) == 0) v.Aleteo = 26;
        }

        /// <summary>EL PRÓXIMO RUMBO del vagar (un punto cualquiera, con margen).</summary>
        private static void NuevoRumbo(Vagabundo v)
        {
            v.Objetivo = new Vector2(
                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
            v.TFase = 0;
            // EL BARREL ROLL: 1 de cada 6 rumbos, el vagabundo viaja dando
            // UNA VUELTA de campana.
            if (Main.rand.Next(6) == 0)
                v.GiroExtra = Main.rand.NextBool() ? MathHelper.TwoPi : -MathHelper.TwoPi;
        }

        /// <summary>EL PUNTO DE INTERÉS de la curiosidad: ¡el CURSOR! (45 %),
        /// el PERSONAJE de la vista previa (25 %) o un rincón (30 %).</summary>
        private static void ElegirCuriosidad(Vagabundo v)
        {
            int dado = Main.rand.Next(100);
            if (dado < 45)
            {
                v.CurioTipo = 0;
                v.PuntoCurio = new Vector2(Main.mouseX, Main.mouseY);
            }
            else if (dado < 70)
            {
                v.CurioTipo = 1;
                v.PuntoCurio = v.Ancla + new Vector2(0f, -10f);
            }
            else
            {
                v.CurioTipo = 2;
                v.PuntoCurio = new Vector2(
                    Main.rand.NextFloat(80f, Main.screenWidth - 80f),
                    Main.rand.NextFloat(90f, Main.screenHeight - 90f));
            }
        }

        /// <summary>EL PRÓXIMO ACTO: alterna título ↔ cielo (el cielo es
        /// el sol de día, la luna de noche — y si la estrella ya se puso,
        /// el título manda otra vez).</summary>
        private static void ElegirActo(Vagabundo v)
        {
            if (v.ActoProximo == ACTO_LOGO)
            {
                v.Acto = ACTO_LOGO;
                v.ActoProximo = ACTO_SOL;
                // EL ESTACIONAMIENTO SE ELIGE AL VOLAR: un costado del
                // logo, alternando — y A ESO VUELA.
                v.ReposoIzquierda = !v.ReposoIzquierda;
                v.Reposo = new Vector2(
                    Main.screenWidth * 0.5f + (v.ReposoIzquierda ? -1f : 1f) * 175f,
                    100f + Main.rand.NextFloat(-30f, 0f));
                v.Objetivo = v.Reposo;
                return;
            }

            // EL CIELO: sol de día, luna de noche.
            v.ActoProximo = ACTO_LOGO;
            if (Main.dayTime)
            {
                Vector2 sol = PosicionDelSol();
                if (sol.X > 40f && sol.X < Main.screenWidth - 40f)
                {
                    v.Acto = ACTO_SOL;
                    return;
                }
            }
            else
            {
                Vector2 luna = PosicionDeLaLuna();
                if (luna.X > 40f && luna.X < Main.screenWidth - 40f)
                {
                    v.Acto = ACTO_LUNA;
                    return;
                }
            }
            v.Acto = ACTO_LOGO;
        }

        // ================================================================
        //  EL VUELO VIVO (v6.50.128 — la cura de lo «errático y robótico»)
        // ================================================================

        /// <summary>EL VUELO ORGÁNICO: muelle amortiguado + vaivén
        /// PERPENDICULAR al rumbo (el camino SIEMPRE es una curva) + la
        /// inercia hace el resto.</summary>
        private static void Volar(Vagabundo v, Vector2 objetivo, float muelle, float tope, float dt)
        {
            Vector2 hacia = objetivo - v.Pos;
            float dist = hacia.Length();
            if (dist > 0.001f) hacia /= dist;

            float cercania = MathHelper.Clamp(dist / 150f, 0f, 1f);
            Vector2 fuerza = hacia * muelle * (0.30f + 0.70f * cercania);

            // EL VAIVÉN: la deriva perpendicular que curva el rumbo.
            _tRuido += dt;
            Vector2 perp = new Vector2(-hacia.Y, hacia.X);
            float vaiven = MathF.Sin(_tRuido * 2.3f) * 0.60f
                         + MathF.Sin(_tRuido * 4.9f + 1.7f) * 0.28f;
            fuerza += perp * vaiven * muelle * 0.50f;

            v.Vel += fuerza * 60f * dt;
            v.Vel *= 1f - MathHelper.Clamp(2.4f * dt, 0f, 0.28f);

            if (v.Vel.Length() > tope) v.Vel = Vector2.Normalize(v.Vel) * tope;
            v.Pos += v.Vel * 60f * dt;
        }

        /// <summary>El reloj del vaivén (compartido: el mismo viento para
        /// toda la manada — las curvas se sienten hermanas).</summary>
        private static float _tRuido;

        // ================================================================
        //  LOS ACTOS
        // ================================================================

        /// <summary>ATACAR EL TÍTULO CON LOS MINIMIONES: DOS ANDANADAS —
        /// los TRES OJOS de la corona se despliegan frente al logo y
        /// DISPARAN sus virotes violetas.</summary>
        private static void ActuarLogo(Vagabundo v)
        {
            v.Foco = new Vector2(Main.screenWidth * 0.5f, 100f);

            {
                const int T_DESPLIEGUE = 30, T_FUEGO = 66, T_RECOGIDA = 30;
                int t = v.TActo;
                float lado = v.ReposoIzquierda ? -1f : 1f;

                if (t < T_DESPLIEGUE) Volar(v, v.Reposo, 0.20f, 5f, 1f / 60f);

                Vector2 anclaTiro = new Vector2(
                    Main.screenWidth * 0.5f + lado * 130f, 100f - 78f);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 destino = anclaTiro + new Vector2(i * 34f * -lado - 34f, i * 10f);
                    float ang = Main.GlobalTimeWrappedHourly * 0.75f + i * MathHelper.TwoPi / 3f;
                    Vector2 corona = v.Pos + new Vector2(MathF.Cos(ang) * 17f, MathF.Sin(ang) * 14f - 3f);
                    if (t < T_DESPLIEGUE)
                    {
                        float p = MathHelper.Clamp(t / (float)T_DESPLIEGUE, 0f, 1f);
                        _ojosActo[i] = Vector2.Lerp(corona, destino, p * p * (3f - 2f * p));
                    }
                    else if (t < T_DESPLIEGUE + T_FUEGO)
                    {
                        _ojosActo[i] = destino + new Vector2(
                            MathF.Sin(t * 0.11f + i * 2.1f) * 4f, MathF.Sin(t * 0.07f + i) * 3f);
                    }
                    else
                    {
                        float p = MathHelper.Clamp((t - T_DESPLIEGUE - T_FUEGO) / (float)T_RECOGIDA, 0f, 1f);
                        _ojosActo[i] = Vector2.Lerp(destino, corona, p * p * (3f - 2f * p));
                    }
                }

                // EL FUEGO: cada ojo suelta DOS virotes.
                if (t > T_DESPLIEGUE && t <= T_DESPLIEGUE + T_FUEGO &&
                    (t - T_DESPLIEGUE) % 11 == 0)
                {
                    int ronda = (t - T_DESPLIEGUE) / 11;
                    int ojo = ronda % 3;
                    Vector2 blanco = new Vector2(
                        Main.screenWidth * 0.5f + Main.rand.NextFloat(-150f, 150f),
                        100f + Main.rand.NextFloat(-22f, 26f));
                    Vector2 pos = _ojosActo[ojo];
                    Vector2 rumbo = blanco - pos;
                    float viaje = MathF.Max(rumbo.Length() / 13f, 6f);
                    _virotes.Add(new Virote
                    {
                        Pos = pos,
                        Vel = rumbo / viaje,
                        Llega = (int)viaje,
                        Objetivo = blanco
                    });
                    _ojosActo[ojo] -= rumbo.SafeNormalize(Vector2.UnitX) * 3f;
                    v.TRecoil = 7;
                }

                if (t >= T_DESPLIEGUE + T_FUEGO + T_RECOGIDA)
                {
                    if (v.GolpesLogo >= 3)
                    {
                        if (_virotes.Count == 0) FinDelActo(v);
                    }
                    else
                    {
                        v.GolpesLogo++;
                        v.TActo = 0;
                    }
                }
            }
        }

        /// <summary>AGARRAR Y ARRASTRAR EL SOL: LOS ANILLOS RÚNICOS ciñen
        /// al sol, lo APRIETAN… y se lo ARRASTRAN: sunModY lo hunde ~190
        /// px y Main.time AVANZA — el sol se DESLIZA por su arco. EL OJO
        /// GIGANTE MURIÓ: los anillos son los únicos protagonistas.</summary>
        private static void ActuarSol(Vagabundo v)
        {
            const int T_ANILLOS = 46, T_AGARRE = 30, T_TIRON = 15, T_ARRASTRE = 115, T_SUELTA = 36;
            Vector2 sol = PosicionDelSol();
            v.Foco = sol;
            int t = v.TActo;

            if (t < T_ANILLOS)
            {
                Volar(v, sol + new Vector2(38f, -30f), 0.16f, 5f, 1f / 60f);
            }
            else if (t < T_ANILLOS + T_AGARRE)
            {
                MoverHacia(v, sol + new Vector2(30f, -24f), 0.30f);
                if ((t & 3) == 0)
                    Rafaga(sol + Main.rand.NextVector2Unit() * 22f, 2);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON)
            {
                Main.sunModY = (short)MathHelper.Clamp(Main.sunModY - 3, -320, 320);
                MoverHacia(v, sol + new Vector2(12f, -44f), 0.30f);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
            {
                Main.sunModY = (short)MathHelper.Clamp(Main.sunModY + 2, -320, 320);
                Main.time += 26.0;
                MoverHacia(v, sol + new Vector2(26f, -28f), 0.30f);
                if ((t & 5) == 0)
                    Rafaga(sol + new Vector2(-20f, 18f) + Main.rand.NextVector2Unit() * 14f, 2);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE + T_SUELTA)
            {
                if (t == T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
                {
                    v.Vel = new Vector2(-5.5f, -3.2f);
                    Rafaga(sol, 10);
                }
                v.Pos += v.Vel;
                v.Vel *= 0.94f;
            }
            else FinDelActo(v);

            // SI EL SOL SE FUE DE LA PANTALLA a la fuerza: fin anticipado.
            if (sol.X > Main.screenWidth + 60f || sol.X < -60f) FinDelActo(v);
        }

        /// <summary>CAMBIAR LA LUNA: el libro le da una VUELTA a la luna
        /// (la órbita del pacto)… y al completarla, la FASE GIRA.</summary>
        private static void ActuarLuna(Vagabundo v)
        {
            const int T_VUELTA = 96;
            Vector2 luna = PosicionDeLaLuna();
            v.Foco = luna;
            float a = MathHelper.TwoPi * (v.TActo / (float)T_VUELTA) - MathHelper.PiOver2;
            MoverHacia(v, luna + new Vector2(MathF.Cos(a) * 48f, MathF.Sin(a) * 48f * 0.9f), 0.32f);

            if (v.TActo == T_VUELTA / 2)
            {
                Main.moonPhase = (Main.moonPhase + 1) % 8;
                Main.moonModY = (short)MathHelper.Clamp(Main.moonModY - 22, -320, 320);
                for (int i = 0; i < 12; i++)
                {
                    float ang = i / 12f * MathHelper.TwoPi;
                    _chispas.Add(new Chispa
                    {
                        Pos = luna + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 30f,
                        Vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.6f,
                        Vida = 0.7f,
                        Tam = 4f,
                        Violeta = true
                    });
                }
            }
            if (v.TActo >= T_VUELTA + 24)
                FinDelActo(v);
        }

        /// <summary>El acto terminó: de vuelta a deambular.</summary>
        private static void FinDelActo(Vagabundo v)
        {
            v.Fase = FASE_VAGAR;
            v.TFase = 0;
            v.Caminatas = 0;
            if (_actorActual == v.IdxManada) _actorActual = -1;
            EmpezarPausa(v);
        }

        // ================================================================
        //  LOS FX COMUNES (manchas + ráfagas de chispas)
        // ================================================================

        /// <summary>UNA MANCHA de tinta sobre el título.</summary>
        private static void Manchar(Vector2 donde, float fuerza)
        {
            _manchas.Add(new Mancha
            {
                Pos = donde,
                Tam = Main.rand.NextFloat(13f, 24f) * fuerza,
                Alfa = Main.rand.NextFloat(0.68f, 0.9f),
                Nace = Main.GlobalTimeWrappedHourly,
                Giro = Main.rand.NextFloat(MathHelper.TwoPi)
            });
            if (_manchas.Count > 16) _manchas.RemoveAt(0);
        }

        /// <summary>UNA RÁFAGA de chispas de tinta.</summary>
        private static void Rafaga(Vector2 donde, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                _chispas.Add(new Chispa
                {
                    Pos = donde,
                    Vel = new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) *
                          Main.rand.NextFloat(0.8f, 3.2f),
                    Vida = Main.rand.NextFloat(0.35f, 0.8f),
                    Tam = Main.rand.NextFloat(2.5f, 6f),
                    Violeta = Main.rand.NextBool()
                });
            }
        }

        // ================================================================
        //  EL CIELO DEL TÍTULO — las FÓRMULAS EXACTAS de vanilla
        //  (DrawSunAndMoon descompilado; en el menú bgTopY = 0)
        // ================================================================

        /// <summary>El centro del SOL en pantalla (de día).</summary>
        private static Vector2 PosicionDelSol()
        {
            Texture2D sol = Terraria.GameContent.TextureAssets.Sun.Value;
            float w = sol.Width;
            float tiempo = (float)Main.time;
            float parab = tiempo < 27000f
                ? MathF.Pow(1f - tiempo / 54000f * 2f, 2f)
                : MathF.Pow((tiempo / 54000f - 0.5f) * 2f, 2f);
            float x = (int)(tiempo / 54000f * (Main.screenWidth + w * 2f)) - w;
            float y = parab * 250f + 180f + Main.sunModY;
            return new Vector2(x, y);
        }

        /// <summary>El centro de la LUNA en pantalla (de noche).</summary>
        private static Vector2 PosicionDeLaLuna()
        {
            var lunas = Terraria.GameContent.TextureAssets.Moon;
            int idx = Main.moonType;
            if (idx < 0 || idx >= lunas.Length) idx = 0;
            Texture2D luna = lunas[idx].Value;
            float w = luna.Width;
            float tiempo = (float)Main.time;
            float parab = tiempo < 16200f
                ? MathF.Pow(1f - tiempo / 32400f * 2f, 2f)
                : MathF.Pow((tiempo / 32400f - 0.5f) * 2f, 2f);
            float x = (int)(tiempo / 32400f * (Main.screenWidth + w * 2f)) - w;
            float y = parab * 250f + 180f + Main.moonModY;
            return new Vector2(x, y);
        }

        // ================================================================
        //  EL DIBUJADO — la manada, sus manchas y sus chispas
        //  (TODO en el LOTE PROPIO — ni un End fantasma)
        // ================================================================

        private static void Dibujar()
        {
            try
            {
                if (_lote == null)
                    _lote = new SpriteBatch(Main.graphics.GraphicsDevice);
            }
            catch { return; }

            // El lote ALFA (los cuerpos, las manchas, la página).
            try
            {
                _lote.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, Main.UIScaleMatrix);
            }
            catch
            {
                // EL RESET DEL DISPOSITIVO: si el lote murió, se descarta y
                // RENACE al próximo frame.
                try { _lote.Dispose(); } catch { }
                _lote = null;
                return;
            }
            try
            {
                DibujarManchas(_lote);
                for (int i = 0; i < _manada.Count; i++)
                    DibujarElLibro(_lote, _manada[i]);
                DibujarVelosDelSol(_lote);
            }
            catch { }
            try { _lote.End(); } catch { }

            // El lote ADITIVO (la brasa violeta + las chispas + los
            // virotes + los anillos rúnicos).
            try
            {
                _lote.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, Main.UIScaleMatrix);
            }
            catch
            {
                try { _lote.Dispose(); } catch { }
                _lote = null;
                return;
            }
            try
            {
                DibujarBrasaYChispas(_lote);
                DibujarVirotes(_lote);
                DibujarPuffs(_lote);
                var actor = ActorDelSolOLuna();
                if (actor != null && actor.Acto == ACTO_SOL) DibujarActoDelSol(_lote);
                if (actor != null && actor.Acto == ACTO_LUNA) DibujarAnillosRunicos(_lote,
                    PosicionDeLaLuna(), 20f, Main.GlobalTimeWrappedHourly, 0.55f, 1f);
            }
            catch { }
            try { _lote.End(); } catch { }
        }

        /// <summary>v6.51.3 — ¿Quién está actuando el sol o la luna ahora?
        /// (solo UNO a la vez — el que está en FASE_ACTUAR).</summary>
        private static Vagabundo ActorDelSolOLuna()
        {
            if (_actorActual >= 0 && _actorActual < _manada.Count &&
                _manada[_actorActual].Fase == FASE_ACTUAR)
                return _manada[_actorActual];
            return null;
        }

        /// <summary>Las MANCHAS DE TINTA sobre el título.</summary>
        private static void DibujarManchas(SpriteBatch sb)
        {
            if (_manchas.Count == 0) return;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;
            float t = Main.GlobalTimeWrappedHourly;
            for (int i = 0; i < _manchas.Count; i++)
            {
                Mancha m = _manchas[i];
                float plop = MathHelper.Clamp((t - m.Nace) / 0.25f, 0f, 1f);
                float tam = m.Tam * (0.6f + 0.4f * plop);
                Color tinta = new Color(24, 8, 34) * (m.Alfa * (0.85f + 0.15f * MathF.Sin(t * 2f + i)));
                sb.Draw(glow, m.Pos, null, tinta, m.Giro, glow.Size() * 0.5f,
                    new Vector2(tam, tam * 0.6f) / glow.Size(), SpriteEffects.None, 0f);
                sb.Draw(glow, m.Pos, null, tinta * 0.8f, m.Giro, glow.Size() * 0.5f,
                    new Vector2(tam * 0.5f, tam * 0.5f) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>EL LIBRO VAGABUNDO (v6.51.3 — por cada uno de la
        /// manada): la miniatura con su ojo vivo (mirando al FOCO), su
        /// parpadeo, sus TRES OJOS en corona (o desplegados en la
        /// andanada), su RASTRO fantasma y la inclinación del giro —
        /// Y LAS MEJORAS DE ANIMACIÓN: el ALETEO DE VUELO (páginas que
        /// baten al volar rápido) y el ATERRIZAJE CON BOTE (la escala Y
        /// que se aplasta y rebota al llegar a la pausa).</summary>
        private static void DibujarElLibro(SpriteBatch sb, Vagabundo v)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            if (tex == null) return;

            const float esc = 0.52f;
            float bob = MathF.Sin(t * 1.75f + 0.8f + v.IdxManada * 1.9f) * 2.4f;
            // EL CINEO: la inclinación nace de la VELOCIDAD + su vaivén
            // perezoso + EL BARREL ROLL.
            float giro = MathHelper.Clamp(v.Vel.X * 0.028f, -0.30f, 0.30f)
                + MathF.Sin(t * 1.2f + v.IdxManada * 0.7f) * 0.05f + v.GiroExtra;

            // LA RESPIRACIÓN Y EL TEATRO DE ESCALA: el libro late ±2 %;
            // el ALETEO lo hincha en campana; la andanada lo hace INHALAR;
            // el retroceso lo empuja ATRÁS — y v6.51.3: el ALETEO DE VUELO
            // (batiendo X al volar rápido) y el ATERRIZAJE CON BOTE (Y se
            // aplasta y rebota al llegar).
            float escViva = esc * (1f + 0.02f * MathF.Sin(t * 2.1f + v.IdxManada * 1.1f));
            if (v.Aleteo > 0)
                escViva *= 1f + 0.06f * MathF.Sin((26 - v.Aleteo) / 26f * MathHelper.Pi);
            if (v.Fase == FASE_ACTUAR && v.Acto == ACTO_LOGO && v.GolpesLogo >= 2 && v.TActo < 30)
                escViva *= 1f + 0.04f * (v.TActo / 30f);          // la inhalación
            if (v.TRecoil > 0)
                escViva *= 1f - 0.025f * (v.TRecoil / 7f);        // el jadeo
            float escX = escViva * v.AleteoVuelo(t);              // v6.51.3 — el batir
            float escY = escViva * v.BoteAterrizaje(t);           // v6.51.3 — el bote
            Vector2 escala = new Vector2(escX, escY);

            Vector2 centro = v.Pos + new Vector2(0f, bob);
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Color luz = new Color(230, 224, 246);
            // La marca de la manada: cada libro con su tinte un PELÍN
            // distinto (los gemelos se distinguen — 4% hacia el cálido
            // o el frío según su número).
            if (v.IdxManada % 3 == 1) luz = new Color(236, 226, 240);
            else if (v.IdxManada % 3 == 2) luz = new Color(224, 226, 250);

            // EL RASTRO FANTASMA (solo cuando vuela de verdad): cada 2
            // frames SU posición queda grabada en SU historia (los
            // fantasmas son de ESTE libro — con varios vagabundos cada
            // rastro cuenta a su dueño).
            float rapidez = v.Vel.Length();
            if ((v.TFase & 1) == 0)
            {
                v.IdxRastro = (v.IdxRastro + 1) % v.Rastro.Length;
                v.Rastro[v.IdxRastro] = v.Pos;
            }
            if (rapidez > 3.4f)
            {
                float alfaR = MathHelper.Clamp((rapidez - 3.4f) / 5f, 0f, 1f) * 0.14f;
                for (int r = 1; r <= 3; r++)
                {
                    int idx = (v.IdxRastro - r * 2 + v.Rastro.Length * 2)
                        % v.Rastro.Length;
                    Vector2 fantasma = v.Rastro[idx];
                    if (fantasma == Vector2.Zero) continue;
                    float f = 1f - r / 3f;
                    sb.Draw(tex, fantasma, frame, luz * (alfaR * f), giro,
                        origen, esc * (0.96f + 0.04f * f), SpriteEffects.None, 0f);
                }
            }

            sb.Draw(tex, centro, frame, luz, giro, origen, escala, SpriteEffects.None, 0f);

            // EL OJO VIVO (mira hacia el FOCO de la curiosidad).
            float parp = (t * 0.9f + v.IdxManada * 1.37f) % 5.2f;
            if (parp > 4.90f)
            {
                var parpado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                sb.Draw(parpado, centro, frame, luz, giro, origen, escala, SpriteEffects.None, 0f);
            }
            else
            {
                Vector2 rumbo = v.Foco - centro;
                Vector2 mirada = rumbo.LengthSquared() > 4f
                    ? Vector2.Normalize(rumbo) * new Vector2(3.5f, 2.8f) * 0.9f
                    : Vector2.Zero;
                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * escala;
                Vector2 posOjo = centro + alOjo.RotatedBy(giro);
                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                sb.Draw(iris, posOjo, null, luz, giro, iris.Size() * 0.5f,
                    esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
            }

            // LOS TRES OJOS: en CORONA compacta… o DESPLEGADOS en plena
            // andanada contra el título (solo el actor los despliega).
            Texture2D ojoAbierto = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Texture2D ojoCerrado = ModContent.Request<Texture2D>(
                // v6.51.4 — LA CURA DEL ERROR DEL MENÚ (el client.log del
                // playtest: «Excepción silenciosa:
                // AssetLoadException: Content\Projectiles\OjoEterne_Cerrado»
                // — un typo de la manada: faltaba la "o" de Eterno y el
                // vagabundo REVIENTA el dibujo del menú de selección en
                // cada parpadeo).
                "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
            var irisOjo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            if (ojoAbierto == null || ojoCerrado == null || irisOjo == null) return;

            bool enAndanada = v.Fase == FASE_ACTUAR && v.Acto == ACTO_LOGO && v.GolpesLogo > 1;
            const float escOjo = 0.22f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 p;
                if (enAndanada)
                {
                    p = _ojosActo[i];
                }
                else
                {
                    float ang = t * 0.75f + i * MathHelper.TwoPi / 3f;
                    float radio = 17f + MathF.Sin(t * 0.9f) * 1.5f;
                    p = centro + new Vector2(MathF.Cos(ang) * radio,
                        MathF.Sin(ang) * radio * 0.82f + MathF.Sin(t * 1.6f + i * 2.4f) * 1.5f - 3f);
                }
                float parpOjo = (t * 1.15f + i * 2.61f + v.IdxManada * 0.9f) % 6.5f;
                bool cerrado = parpOjo <= 0.26f;
                Texture2D cuerpo = cerrado ? ojoCerrado : ojoAbierto;
                sb.Draw(cuerpo, p, null, luz, 0f, cuerpo.Size() * 0.5f, escOjo,
                    SpriteEffects.None, 0f);
                if (!cerrado)
                {
                    Vector2 rumbo = v.Foco - p;
                    Vector2 mira = rumbo.LengthSquared() > 4f
                        ? Vector2.Normalize(rumbo) * new Vector2(5f, 2f)
                        : Vector2.Zero;
                    sb.Draw(irisOjo, p + (new Vector2(0.7f, 2.3f) + mira) * escOjo, null,
                        luz, 0f, irisOjo.Size() * 0.5f, 0.41f * escOjo, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>LA BRASA del vagabundo + LAS CHISPAS de tinta.</summary>
        private static void DibujarBrasaYChispas(SpriteBatch sb)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;
            float t = Main.GlobalTimeWrappedHourly;

            for (int m = 0; m < _manada.Count; m++)
            {
                float pulso = 0.20f + 0.08f * MathF.Sin(t * 2.6f + m * 1.4f);
                sb.Draw(glow, _manada[m].Pos, null, new Color(110, 40, 170) * pulso, 0f,
                    glow.Size() * 0.5f, new Vector2(52f, 52f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }

            for (int i = 0; i < _chispas.Count; i++)
            {
                Chispa c = _chispas[i];
                Color col = (c.Violeta ? new Color(140, 60, 210) : new Color(230, 226, 245))
                    * MathHelper.Clamp(c.Vida * 1.6f, 0f, 1f);
                sb.Draw(glow, c.Pos, null, col, 0f, glow.Size() * 0.5f,
                    new Vector2(c.Tam, c.Tam) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS VIROTES DE LOS MINIMIONES (la andanada).</summary>
        private static void DibujarVirotes(SpriteBatch sb)
        {
            if (_virotes.Count == 0) return;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;
            for (int i = 0; i < _virotes.Count; i++)
            {
                Virote v = _virotes[i];
                float ang = v.Vel.ToRotation();
                sb.Draw(glow, v.Pos, null, new Color(120, 45, 200) * 0.85f, ang,
                    glow.Size() * 0.5f, new Vector2(22f, 4.5f) / glow.Size(),
                    SpriteEffects.None, 0f);
                sb.Draw(glow, v.Pos, null, new Color(235, 225, 255) * 0.9f, ang,
                    glow.Size() * 0.5f, new Vector2(9f, 2.2f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS PUFFS de bruma (la estela de vuelo).</summary>
        private static void DibujarPuffs(SpriteBatch sb)
        {
            if (_puffs.Count == 0) return;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;
            for (int i = 0; i < _puffs.Count; i++)
            {
                Puff p = _puffs[i];
                Color col = new Color(58, 34, 88) * (MathHelper.Clamp(p.Vida * 1.4f, 0f, 1f) * 0.55f);
                sb.Draw(glow, p.Pos, null, col, 0f, glow.Size() * 0.5f,
                    new Vector2(p.Tam, p.Tam * 0.8f) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>v6.50.130 — LOS VELOS DEL SOL (las bandas OSCURAS de
        /// contraste tras los anillos rúnicos — solo el ACTOR del sol).</summary>
        private static void DibujarVelosDelSol(SpriteBatch sb)
        {
            var actor = ActorDelSolOLuna();
            if (actor == null || actor.Acto != ACTO_SOL) return;

            const int T_ANILLOS = 46, T_AGARRE = 30, T_TIRON = 15, T_ARRASTRE = 115, T_SUELTA = 36;
            int t = actor.TActo;
            if (t >= T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE + T_SUELTA) return;

            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            Vector2 sol = PosicionDelSol();
            float time = Main.GlobalTimeWrappedHourly;

            float ciñe;
            if (t < T_ANILLOS)
            {
                float p = t / (float)T_ANILLOS;
                ciñe = MathHelper.Lerp(3.4f, 1.35f, p * p * (3f - 2f * p));
            }
            else if (t < T_ANILLOS + T_AGARRE)
            {
                float p = (t - T_ANILLOS) / (float)T_AGARRE;
                ciñe = MathHelper.Lerp(1.35f, 1.05f, p);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
            {
                ciñe = 1.05f + MathF.Sin(t * 0.55f) * 0.07f;
            }
            else
            {
                float p = MathHelper.Clamp(
                    (t - T_ANILLOS - T_AGARRE - T_TIRON - T_ARRASTRE) / 36f, 0f, 1f);
                ciñe = MathHelper.Lerp(1.05f, 2.6f, p);
            }
            float alphaVelo = t < T_ANILLOS ? t / (float)T_ANILLOS
                : MathHelper.Clamp(1f - (t - T_ANILLOS - T_AGARRE - T_TIRON - T_ARRASTRE) / 30f, 0f, 1f);
            if (alphaVelo <= 0.02f) return;

            Color velo = new Color(14, 6, 26);
            for (int k = 0; k < 3; k++)
            {
                float a = (VFX.SigiloLib.RingA0 + VFX.SigiloLib.RingAStep * k) * 26f * ciñe * 0.42f;
                float b = a * (0.34f + 0.07f * (k % 3));
                float tilt = -0.55f + 0.20f * k
                    + 0.10f * MathF.Sin(time * (0.35f + 0.06f * k) + k * 1.9f);
                float spin = time * ((k % 2 == 0 ? 1f : -1f) *
                    (VFX.SigiloLib.RingSpin0 + VFX.SigiloLib.RingSpinStep * k)) * 1.8f;

                for (int i = 0; i < 26; i++)
                {
                    float ang = i / 26f * MathHelper.TwoPi + spin * 0.4f;
                    Vector2 p = PuntoElipse(sol, a, b, tilt, ang);
                    sb.Draw(glow, p, null, velo * (0.42f * alphaVelo), 0f,
                        glow.Size() * 0.5f, new Vector2(13f, 13f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>EL ACTO DEL SOL (v6.50.129): LOS ANILLOS RÚNICOS de
        /// los agujeros negros sujetando al sol — solo el ACTOR.</summary>
        private static void DibujarActoDelSol(SpriteBatch sb)
        {
            const int T_ANILLOS = 46, T_AGARRE = 30, T_TIRON = 15, T_ARRASTRE = 115;
            var actor = ActorDelSolOLuna();
            if (actor == null) return;
            int t = actor.TActo;
            Vector2 sol = PosicionDelSol();

            float ciñe;
            if (t < T_ANILLOS)
            {
                float p = t / (float)T_ANILLOS;
                ciñe = MathHelper.Lerp(3.4f, 1.35f, p * p * (3f - 2f * p));
            }
            else if (t < T_ANILLOS + T_AGARRE)
            {
                float p = (t - T_ANILLOS) / (float)T_AGARRE;
                ciñe = MathHelper.Lerp(1.35f, 1.05f, p);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
            {
                ciñe = 1.05f + MathF.Sin(t * 0.55f) * 0.07f;
            }
            else
            {
                float p = MathHelper.Clamp(
                    (t - T_ANILLOS - T_AGARRE - T_TIRON - T_ARRASTRE) / 36f, 0f, 1f);
                ciñe = MathHelper.Lerp(1.05f, 2.6f, p);
            }
            float alfaAnillos = t < T_ANILLOS ? t / (float)T_ANILLOS
                : MathHelper.Clamp(1f - (t - T_ANILLOS - T_AGARRE - T_TIRON - T_ARRASTRE) / 30f, 0f, 1f);
            bool enPelea = t >= T_ANILLOS + T_AGARRE && t <= T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE;
            float apreto = enPelea ? 1f + MathF.Sin(t * 0.55f) * 0.25f + 0.45f : 1f;
            if (alfaAnillos > 0.02f)
                DibujarAnillosRunicos(sb, sol, 26f * ciñe, Main.GlobalTimeWrappedHourly,
                    alfaAnillos, apreto);

            // EL AURA DEL SOL AGARRADO.
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow != null && t >= T_ANILLOS + T_AGARRE && t <= T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
            {
                float jadeo = 0.26f + 0.14f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.2f);
                sb.Draw(glow, sol, null, new Color(150, 70, 230) * jadeo, 0f,
                    glow.Size() * 0.5f, new Vector2(74f, 74f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS ANILLOS RÚNICOS DE LOS AGUJEROS NEGROS: la
        /// geometría EXACTA de SigiloLib.SistemaAnillos, portada al
        /// espacio del menú (tier 3 — la triple corona).</summary>
        private static void DibujarAnillosRunicos(SpriteBatch sb, Vector2 centro,
            float R, float time, float alpha, float apreto = 1f)
        {
            if (R < 2f || alpha <= 0.02f) return;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            Color cuerpo = new Color(255, 190, 80);    // SigiloLib.CuerpoOro
            Color punta = new Color(255, 240, 185);    // SigiloLib.PuntaOro
            Color brasa = new Color(255, 150, 40);

            const float spinUp = 1.8f;

            for (int k = 0; k < 3; k++)
            {
                float a = (VFX.SigiloLib.RingA0 + VFX.SigiloLib.RingAStep * k) * R * 0.42f;
                float b = a * (0.34f + 0.07f * (k % 3));
                float tilt = -0.55f + 0.20f * k
                    + 0.10f * MathF.Sin(time * (0.35f + 0.06f * k) + k * 1.9f);
                float spin = time * ((k % 2 == 0 ? 1f : -1f) *
                    (VFX.SigiloLib.RingSpin0 + VFX.SigiloLib.RingSpinStep * k)) * spinUp;
                int runas = VFX.SigiloLib.Runes0 + VFX.SigiloLib.RuneStep * k;

                // EL HALO DEL ARO.
                for (int h = 0; h < 7; h++)
                {
                    float angH = h / 7f * MathHelper.TwoPi + spin * 0.2f;
                    Vector2 pH = PuntoElipse(centro, a, b, tilt, angH);
                    sb.Draw(glow, pH, null, brasa * (0.16f * alpha * apreto), tilt,
                        glow.Size() * 0.5f, new Vector2(a * 0.9f, 9f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // EL ARO (el trazo elíptico).
                for (int i = 0; i < 42; i++)
                {
                    float ang = i / 42f * MathHelper.TwoPi + spin * 0.4f;
                    Vector2 p = PuntoElipse(centro, a, b, tilt, ang);
                    float latir = 0.6f + 0.4f * MathF.Sin(time * 3f + i * 0.7f + k);
                    sb.Draw(glow, p, null, cuerpo * Math.Min(0.62f, 0.30f * apreto) * alpha * latir, 0f,
                        glow.Size() * 0.5f, new Vector2(5.2f, 5.2f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // LAS RUNAS: glifos MAYORES.
                for (int g = 0; g < runas; g++)
                {
                    float ang = g / (float)runas * MathHelper.TwoPi + spin;
                    Vector2 p = PuntoElipse(centro, a, b, tilt, ang);
                    float tang = AnguloTangente(a, b, tilt, ang);
                    float latido = 0.7f + 0.3f * MathF.Sin(time * 2.2f + g * 1.9f + k * 2.4f);
                    sb.Draw(glow, p, null, cuerpo * Math.Min(1.0f, 0.55f * apreto) * alpha * latido, tang,
                        glow.Size() * 0.5f, new Vector2(3.2f, 9.5f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    sb.Draw(glow, p, null, punta * Math.Min(0.95f, 0.50f * apreto) * alpha * latido, tang + 0.62f,
                        glow.Size() * 0.5f, new Vector2(2.6f, 6.6f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    sb.Draw(glow, p, null, punta * Math.Min(0.85f, 0.45f * apreto) * alpha * latido, 0f,
                        glow.Size() * 0.5f, new Vector2(3.4f, 3.4f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>Punto sobre una elipse girada (SigiloLib.EllipsePoint).</summary>
        private static Vector2 PuntoElipse(Vector2 center, float a, float b, float tilt, float t)
        {
            float ct = MathF.Cos(t), st = MathF.Sin(t);
            Vector2 local = new Vector2(a * ct, b * st);
            float cR = MathF.Cos(tilt), sR = MathF.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        /// <summary>Ángulo de la TANGENTE de la elipse en t (SigiloLib).</summary>
        private static float AnguloTangente(float a, float b, float tilt, float t)
        {
            float dx = -a * MathF.Sin(t), dy = b * MathF.Cos(t);
            return MathF.Atan2(dy, dx) + tilt;
        }
    }
}
