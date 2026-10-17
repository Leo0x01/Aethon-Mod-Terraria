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
    //  v6.50.128 — EL VAGABUNDO VIVO (la letra del usuario: «se mueve de
    //  forma erratica y muy robotica… cuando agarra al sol no lo mueve,
    //  deberie por ejemplo atrazarlo o cambiar la posicion del sol, ademas
    //  al agarrar al sol deberia usar los anillos runicos de los agujeros
    //  negros para sujetar al sol jajajaa, debes crear una IA curiosa que
    //  se mueve, sobre atacar al titulo, debe atacarlo con la sombra de
    //  la pagina o con el ataque de los minimion jajajajaja, intenta
    //  hacer que sea mas vivo»):
    //
    //  · EL MOVIMIENTO YA NO ES UN ROBOT: muelle amortiguado + vaivén
    //    PERPENDICULAR (el rumbo siempre es una curva, jamás una recta),
    //    pausas vivas al llegar (se detiene, flota y MIRA ALREDEDOR —
    //    los ojos vagan), estelas fantasma al volar rápido y la
    //    inclinación al girar. La IA CURIOSA: de tanto en tanto elige un
    //    PUNTO DE INTERÉS (¡el CURSOR!, el personaje de la vista previa,
    //    un rincón de la pantalla), vuela hasta él EN ARCO, lo RODEA
    //    estudiándolo, le da un TOQUE con chispas… y si el punto era el
    //    cursor y éste se movió demasiado, se ASUSTA y sale disparado.
    //  · EL SOL SE AGARRA CON LOS ANILLOS RÚNICOS: los anillos de los
    //    agujeros negros (la matemática EXACTA de SigiloLib: RingA/RingFlat/
    //    RingTilt/RingSpin/RunesOfRing, portada al espacio del menú) ciñen
    //    al sol, lo sujetan… y se lo ARRASTRAN de verdad: sunModY tira
    //    hacia abajo (+190 px visibles) y Main.time AVANZA — el sol se
    //    DESLIZA por su arco. El ojo eterno abre sobre él mientras tira.
    //  · EL TÍTULO SE ATACA CON LAS ARMAS: golpe 1 = LA SOMBRA DE LA
    //    PÁGINA (una página de tinta lo BARRE en diagonal, dejando sus
    //    manchas); golpes 2-3 = LA ANDANADA DE LOS MINIMIONES (los tres
    //    ojos de la corona se despliegan y DISPARAN virotes violetas que
    //    salpican el logo) — y las manchas de tinta siguen acumulándose.
    //
    //  EL CIRCUITO (sin cambios):
    //  · GrimorioMenorPet.PreDraw detecta el DUMMY de vista previa del
    //    seleccionador (isAPreviewDummy — lo crea UICharacter.DrawPets) y
    //    posa el libro EN SU RECUADRO como cualquier mascota.
    //  · TickVistaPrevista (aquí) cuenta los segundos de pose. A los ~5 s
    //    (300 t) el libro ABANDONA el recuadro y ESTE sistema lo dibuja
    //    SOBRE TODA LA PANTALLA (hook a Main.PostDrawMenu).
    //
    //  v6.50.128 — EL LOTE PROPIO (la cura del error del client.log: la
    //  .127 «tanteaba» el lote del menú con un try End catch — funcional
    //  pero tML registra cada InvalidOperationException first-chance como
    //  «Excepción silenciosa» y el log se llenaba de fantasmas). El
    //  Main.Draw descompilado (2026.08.3.0) lo confirma: DrawMenu termina
    //  con su propio End — a PostDrawMenu el lote de vanilla llega
    //  CERRADO y tras el hook no vuelve a abrirse. LA SOLUCIÓN LIMPIA:
    //  un SpriteBatch DEDICADO (_lote) que jamás toca el de vanilla — ni
    //  tanteos, ni excepciones, ni log: cero ruido.
    // ======================================================================
    public class SeleccionGrimorioSistema : ModSystem
    {
        // === EL RELOJ DE LA POSE (la vista previa lo alimenta) ===

        /// <summary>Los ~5 segundos de pose en el recuadro (300 t a 60 fps).</summary>
        private const int UMBRAL_ESCAPE = 300;

        /// <summary>Fotogramas del menú sin vista previa que reinician todo.</summary>
        private const int FOTOS_MUERTOS = 90;

        private static int _ticksPose;
        private static int _fotosSinPrevista;
        private static Vector2 _ancla;

        // === LAS FASES DEL VAGABUNDO ===
        private const int FASE_ESCAPE = 0;    // sale del recuadro
        private const int FASE_VAGAR = 1;     // deriva orgánica con pausas vivas
        private const int FASE_CURIOSO = 2;   // v6.50.128 — estudia un punto de interés
        private const int FASE_IR = 3;        // vuela a su objetivo
        private const int FASE_ACTUAR = 4;    // el título / el sol / la luna

        // === LOS ACTOS (qué hace al llegar) ===
        private const int ACTO_LOGO = 0;      // LA SOMBRA DE LA PÁGINA + la andanada de minimiones
        private const int ACTO_SOL = 1;       // LOS ANILLOS RÚNICOS + el arrastre del sol
        private const int ACTO_LUNA = 2;      // gira la fase de la luna

        private static int _fase;
        private static int _tFase;
        private static Vector2 _pos;
        private static Vector2 _vel;
        private static Vector2 _objetivo;
        private static int _acto;              // el acto EN CURSO
        private static int _actoProximo = ACTO_LOGO;   // alterna logo ↔ cielo
        private static int _caminatas;         // tramos entre acto y acto
        private static int _golpesLogo;        // el golpe en curso del acto título
        private static int _tActo;
        private static Vector2 _reposo;        // el punto de arranque de cada golpe
        private static bool _reposoIzquierda;  // de qué lado estaciona para el título

        // === EL MOVIMIENTO VIVO (v6.50.128) ===
        /// <summary>El reloj del vaivén (la deriva perpendicular del vuelo).</summary>
        private static float _tRuido;
        /// <summary>Ticks de PAUSA VIVA al llegar a un sitio (flota mirando alrededor).</summary>
        private static int _pausa;
        /// <summary>El reloj del vagar de la mirada durante las pausas.</summary>
        private static int _tMirada;
        /// <summary>El PUNTO que miran todos los ojos (cursor, logo, sol, la nada).</summary>
        private static Vector2 _foco;

        // === LA CURIOSIDAD (v6.50.128) ===
        private const int CURIO_CURSOR = 0;    // ¡el cursor del mouse!
        private const int CURIO_ANCLA = 1;     // el personaje de la vista previa
        private const int CURIO_PANTALLA = 2;  // un rincón cualquiera
        private static int _curioTipo;
        private static int _tCurio;
        private static float _angCurio;        // la vuelta alrededor del punto
        private static Vector2 _puntoCurio;
        private static Vector2 _cursorAlLlegar; // ¿se movió el cursor mientras lo estudiaba?
        private static bool _yaToque;
        /// <summary>v6.50.128 — EL SUSTO: el cursor se movió demasiado mientras lo estudiaba.</summary>
        private static bool _asustado;

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

        // === v6.50.128 — LOS VIROTES DE LOS MINIMIONES (la andanada del título) ===
        private struct Virote
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public int Llega;       // ticks hasta el impacto
            public Vector2 Objetivo;
        }

        private static readonly List<Virote> _virotes = new();

        // === v6.50.128 — LOS PUFFS DE BRUMA (la estela de la Sombra de la Página) ===
        private struct Puff
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Vida;
            public float Tam;
        }

        private static readonly List<Puff> _puffs = new();

        // === v6.50.128 — LA SOMBRA DE LA PÁGINA (el barrido del golpe 1) ===
        private static Vector2 _pagDe, _pagHacia;
        private static float _pagAng;

        // === v6.50.128 — LAS POSICIONES DE LA CORONA durante la andanada ===
        private static readonly Vector2[] _ojosActo = new Vector2[3];

        // === v6.50.128 — EL RASTRO (las estelas fantasma del vuelo rápido) ===
        private static readonly Vector2[] _rastro = new Vector2[7];
        private static int _idxRastro;

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
            // sobre el logo ya pintado). (El estilo de hooks de ESTE tML
            // 2026.08.3.0: TerrariaHooks.dll expone la clase On_Main —
            // verificado al descompilarla: orig/hook_PostDrawMenu(Point,
            // Point), el método es estático.)
            On_Main.PostDrawMenu += DibujarSobreElMenu;
        }

        public override void Unload()
        {
            On_Main.PostDrawMenu -= DibujarSobreElMenu;
            Reiniciar();
            _virotes.Clear();
            _puffs.Clear();
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
        //  EL RELOJ DE LA POSE (lo llama GrimorioMenorPet cada frame que
        //  el seleccionador muestra a su mascota)
        // ================================================================

        /// <summary>La vista previa posa mientras esto devuelva TRUE (los
        /// ~5 s de la letra); después el libro «se fue» y el vagabundo de
        /// arriba toma el control.</summary>
        public static bool TickVistaPrevista(Vector2 ancla)
        {
            _fotosSinPrevista = 0;
            _ancla = ancla;
            _ticksPose++;
            return _ticksPose < UMBRAL_ESCAPE;
        }

        /// <summary>Todo vuelve a cero (otra pantalla, otro día: el libro
        /// vuelve a posar 5 s en su recuadro antes de escaparse).</summary>
        private static void Reiniciar()
        {
            _ticksPose = 0;
            _fotosSinPrevista = 0;
            _fase = FASE_ESCAPE;
            _tFase = 0;
            _caminatas = 0;
            _golpesLogo = 0;
            _tActo = 0;
            _pausa = 0;
            _vel = Vector2.Zero;
            _manchas.Clear();
            _chispas.Clear();
            Array.Clear(_rastro, 0, _rastro.Length);
        }

        // ================================================================
        //  EL MOTOR — un fotograma del vagabundo (update + draw)
        // ================================================================

        private static void Pintar()
        {
            // ¿La vista previa sigue viva? (el seleccionador con la
            // mascota a la vista). Si lleva 1,5 s muerta: TODO se reinicia.
            _fotosSinPrevista++;
            if (_fotosSinPrevista > FOTOS_MUERTOS)
            {
                Reiniciar();
                return;
            }
            if (_ticksPose < UMBRAL_ESCAPE) return;   // aún posa en su recuadro

            Actualizar();
            Dibujar();
        }

        private static void Actualizar()
        {
            _tFase++;
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

            // LOS PUFFS de la Sombra de la Página se desgarran y mueren.
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
                    // EL IMPACTO: mancha + ráfaga de chispas.
                    Manchar(v.Objetivo, 0.8f);
                    Rafaga(v.Objetivo, 8);
                    _virotes.RemoveAt(i);
                }
                else _virotes[i] = v;
            }

            // EL RASTRO: cada 2 fotogramas, la posición queda grabada.
            if ((_tFase & 1) == 0)
            {
                _idxRastro = (_idxRastro + 1) % _rastro.Length;
                _rastro[_idxRastro] = _pos;
            }

            switch (_fase)
            {
                case FASE_ESCAPE:
                {
                    // EL LIBRO SE ESCAPA: del recuadro a un punto libre de
                    // la pantalla (el despegue — la letra: «este salga»).
                    if (_tFase == 1)
                    {
                        _pos = _ancla;
                        _objetivo = new Vector2(
                            Main.rand.NextFloat(Main.screenWidth * 0.15f, Main.screenWidth * 0.85f),
                            Main.rand.NextFloat(Main.screenHeight * 0.2f, Main.screenHeight * 0.55f));
                        _pausa = 0;
                    }
                    Volar(_objetivo, 0.13f, 8.5f, dt);
                    if (Vector2.DistanceSquared(_pos, _objetivo) < 30f * 30f || _tFase > 150)
                    {
                        _fase = FASE_VAGAR;
                        _tFase = 0;
                        _caminatas = 0;
                        EmpezarPausa();
                    }
                    break;
                }

                case FASE_VAGAR:
                {
                    // DEAMBULAR VIVO: curvas orgánicas, pausas que miran
                    // alrededor… y de tanto en tanto, LA CURIOSIDAD.
                    if (_pausa > 0)
                    {
                        // LA PAUSA VIVA: se asienta, flota, y los ojos
                        // vagan por la pantalla (curiosear sin moverse).
                        _pausa--;
                        _vel *= 0.82f;
                        _pos += _vel;
                        _tMirada++;
                        if (_tMirada % 22 == 0)
                        {
                            _foco = _pos + new Vector2(
                                Main.rand.NextFloat(-150f, 150f),
                                Main.rand.NextFloat(-110f, 110f));
                        }
                        if (_pausa == 0) NuevoRumbo();
                        break;
                    }

                    Volar(_objetivo, 0.10f, 4.6f, dt);
                    _foco = _pos + _vel * 30f;

                    if (Vector2.DistanceSquared(_pos, _objetivo) < 28f * 28f || _tFase > 300)
                    {
                        _caminatas++;
                        if (_caminatas >= 2 && Main.rand.NextBool(2))
                        {
                            // LA CURIOSIDAD GANA: un punto de interés lo
                            // llama antes que el próximo acto.
                            ElegirCuriosidad();
                            _fase = FASE_CURIOSO;
                            _tFase = 0;
                            _tCurio = 0;
                            _angCurio = Main.rand.NextFloat(MathHelper.TwoPi);
                            _yaToque = false;
                            _asustado = false;
                            break;
                        }
                        if (_caminatas >= 2)
                        {
                            ElegirActo();
                            _fase = FASE_IR;
                            _tFase = 0;
                        }
                        else EmpezarPausa();
                    }
                    break;
                }

                case FASE_CURIOSO:
                {
                    // v6.50.128 — LA CURIOSIDAD: vuela EN ARCO hasta su
                    // punto, lo RODEA estudiándolo (los ojos CLAVADOS en
                    // él), le da un TOQUE… y si el punto era el CURSOR y
                    // éste se movió mucho mientras lo estudiaba: SUSTO y
                    // a volar.
                    _tCurio++;
                    _foco = _puntoCurio;

                    if (_tCurio < 80)
                    {
                        // LA IDA (arco orgánico — el vaivén hace el resto).
                        Volar(_puntoCurio + new Vector2(0f, -34f), 0.16f, 6.5f, dt);
                        if (_curioTipo == CURIO_CURSOR)
                            _cursorAlLlegar = new Vector2(Main.mouseX, Main.mouseY);
                        break;
                    }

                    // LA VUELTA: órbita de estudio (~2,4 s), respirando.
                    float velAng = _asustado ? 0.30f : 0.055f;
                    _angCurio += velAng;
                    float radio = 40f + MathF.Sin(_tCurio * 0.05f) * 7f;
                    _pos = _puntoCurio + new Vector2(MathF.Cos(_angCurio) * radio,
                        MathF.Sin(_angCurio) * radio * 0.86f - 6f);
                    _vel = new Vector2(-MathF.Sin(_angCurio), MathF.Cos(_angCurio)) * radio * velAng;

                    // ¿EL CURSOR SE MOVIÓ DEMASIADO? (el susto).
                    if (_curioTipo == CURIO_CURSOR && !_asustado &&
                        Vector2.Distance(_cursorAlLlegar, new Vector2(Main.mouseX, Main.mouseY)) > 150f)
                    {
                        _asustado = true;
                        Vector2 huida = _pos - _puntoCurio;
                        if (huida.LengthSquared() < 0.1f) huida = -Vector2.UnitY;
                        _vel = Vector2.Normalize(huida) * 10.5f;
                        Rafaga(_pos, 7);
                        _tCurio = 130;   // el susto acorta la visita
                    }

                    // EL TOQUE (a mitad de la vuelta, una sola vez).
                    if (!_yaToque && _tCurio == 110 && !_asustado)
                    {
                        _yaToque = true;
                        Rafaga(_puntoCurio, 5);
                        _vel = (_puntoCurio - _pos).SafeNormalize(Vector2.UnitX) * 3.5f;
                    }

                    if (_tCurio >= 150)
                    {
                        _fase = FASE_VAGAR;
                        _tFase = 0;
                        EmpezarPausa();
                    }
                    break;
                }

                case FASE_IR:
                {
                    // VOLAR AL ACTO: el objetivo se recalcula CADA frame si
                    // es el sol o la luna (se MUEVEN — el cielo del título
                    // gira sin parar).
                    if (_acto == ACTO_SOL) _objetivo = PosicionDelSol();
                    else if (_acto == ACTO_LUNA) _objetivo = PosicionDeLaLuna();
                    Volar(_objetivo, 0.14f, 7.5f, dt);
                    _foco = _objetivo;
                    if (Vector2.DistanceSquared(_pos, _objetivo) < 26f * 26f)
                    {
                        _fase = FASE_ACTUAR;
                        _tFase = 0;
                        _tActo = 0;
                        _golpesLogo = 1;
                        if (_acto == ACTO_LOGO)
                        {
                            // EL ESTACIONAMIENTO: un costado del logo, alternando.
                            _reposoIzquierda = !_reposoIzquierda;
                            _reposo = new Vector2(
                                Main.screenWidth * 0.5f + (_reposoIzquierda ? -1f : 1f) * 175f,
                                100f + Main.rand.NextFloat(-30f, 0f));
                        }
                    }
                    break;
                }

                case FASE_ACTUAR:
                {
                    _tActo++;
                    if (_acto == ACTO_LOGO) ActuarLogo();
                    else if (_acto == ACTO_SOL) ActuarSol();
                    else ActuarLuna();
                    break;
                }
            }
        }

        /// <summary>LA PAUSA VIVA: 0,7–1,8 s de flotación mirando la pantalla.</summary>
        private static void EmpezarPausa()
        {
            _pausa = Main.rand.Next(42, 108);
            _tMirada = 0;
            _tFase = 0;
        }

        /// <summary>EL PRÓXIMO RUMBO del vagar (un punto cualquiera, con margen).</summary>
        private static void NuevoRumbo()
        {
            _objetivo = new Vector2(
                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
            _tFase = 0;
        }

        /// <summary>v6.50.128 — EL PUNTO DE INTERÉS de la curiosidad: ¡el
        /// CURSOR! (45 % — es lo más vivo de la pantalla), el PERSONAJE de
        /// la vista previa (25 %) o un rincón cualquiera (30 %).</summary>
        private static void ElegirCuriosidad()
        {
            int dado = Main.rand.Next(100);
            if (dado < 45)
            {
                _curioTipo = CURIO_CURSOR;
                _puntoCurio = new Vector2(Main.mouseX, Main.mouseY);
            }
            else if (dado < 70)
            {
                _curioTipo = CURIO_ANCLA;
                _puntoCurio = _ancla + new Vector2(0f, -10f);
            }
            else
            {
                _curioTipo = CURIO_PANTALLA;
                _puntoCurio = new Vector2(
                    Main.rand.NextFloat(80f, Main.screenWidth - 80f),
                    Main.rand.NextFloat(90f, Main.screenHeight - 90f));
            }
        }

        /// <summary>EL PRÓXIMO ACTO: alterna título ↔ cielo (el cielo es
        /// el sol de día, la luna de noche — y si la estrella ya se puso,
        /// el título manda otra vez).</summary>
        private static void ElegirActo()
        {
            if (_actoProximo == ACTO_LOGO)
            {
                _acto = ACTO_LOGO;
                _actoProximo = ACTO_SOL;
                return;
            }

            // EL CIELO: sol de día, luna de noche (si la estrella ya se
            // fue tras el borde, el título otra vez — la estrella vuelve).
            _actoProximo = ACTO_LOGO;
            if (Main.dayTime)
            {
                Vector2 sol = PosicionDelSol();
                if (sol.X > 40f && sol.X < Main.screenWidth - 40f)
                {
                    _acto = ACTO_SOL;
                    return;
                }
            }
            else
            {
                Vector2 luna = PosicionDeLaLuna();
                if (luna.X > 40f && luna.X < Main.screenWidth - 40f)
                {
                    _acto = ACTO_LUNA;
                    return;
                }
            }
            _acto = ACTO_LOGO;
        }

        // ================================================================
        //  EL VUELO VIVO (v6.50.128 — la cura de lo «errático y robótico»)
        // ================================================================

        /// <summary>EL VUELO ORGÁNICO: muelle amortiguado (acelera suave,
        /// frena llegando — nada de topes secos ni Lerp metronómico) +
        /// vaivén PERPENDICULAR al rumbo (el camino SIEMPRE es una curva,
        /// como el vuelo de una polilla) + la inercia hace el resto.</summary>
        private static void Volar(Vector2 objetivo, float muelle, float tope, float dt)
        {
            Vector2 hacia = objetivo - _pos;
            float dist = hacia.Length();
            if (dist > 0.001f) hacia /= dist;

            // LA FUERZA: muelle que se apaga al acercarse (llegada suave).
            float cercania = MathHelper.Clamp(dist / 150f, 0f, 1f);
            Vector2 fuerza = hacia * muelle * (0.30f + 0.70f * cercania);

            // EL VAIVÉN: la deriva perpendicular que curva el rumbo.
            _tRuido += dt;
            Vector2 perp = new Vector2(-hacia.Y, hacia.X);
            float vaiven = MathF.Sin(_tRuido * 2.3f) * 0.60f
                         + MathF.Sin(_tRuido * 4.9f + 1.7f) * 0.28f;
            fuerza += perp * vaiven * muelle * 0.50f;

            // LA FÍSICA: aceleración + arrastre (la inercia dibuja el arco).
            _vel += fuerza * 60f * dt;
            _vel *= 1f - MathHelper.Clamp(2.4f * dt, 0f, 0.28f);

            if (_vel.Length() > tope) _vel = Vector2.Normalize(_vel) * tope;
            _pos += _vel * 60f * dt;
        }

        // ================================================================
        //  LOS ACTOS
        // ================================================================

        /// <summary>ATACAR EL TÍTULO CON LAS ARMAS DEL GRIMORIO (v6.50.128
        /// — la letra: «debe atacarlo con la sombra de la pagina o con el
        /// ataque de los minimion»): GOLPE 1 = LA SOMBRA DE LA PÁGINA barre
        /// el logo en diagonal dejando sus manchas; GOLPES 2-3 = los TRES
        /// OJOS de la corona se despliegan y DISPARAN virotes violetas.</summary>
        private static void ActuarLogo()
        {
            _foco = new Vector2(Main.screenWidth * 0.5f, 100f);

            if (_golpesLogo == 1)
            {
                // ====== GOLPE 1 — LA SOMBRA DE LA PÁGINA ======
                const int T_CONJURO = 26, T_BARRIDO = 22, T_DISIPA = 24;
                int t = _tActo;
                float lado = _reposoIzquierda ? 1f : -1f;

                if (t <= T_CONJURO)
                {
                    // EL CONJURO: el libro se echa atrás y se le enciende
                    // el aura (la página se está escribiendo).
                    Volar(_reposo, 0.20f, 5f, 1f / 60f);
                }
                else if (t == T_CONJURO + 1)
                {
                    // EL BARRIDO NACE: cruza el logo EN DIAGONAL, de un
                    // costado al otro, inclinada como un tajo.
                    _pagDe = new Vector2(
                        Main.screenWidth * 0.5f - lado * 300f, 100f + 34f);
                    _pagHacia = new Vector2(
                        Main.screenWidth * 0.5f + lado * 300f, 100f - 34f);
                    _pagAng = lado * (0.62f + Main.rand.NextFloat(-0.08f, 0.08f));
                }
                else if (t <= T_CONJURO + T_BARRIDO)
                {
                    // LA PÁGINA VUELA: puffs de bruma tras de ella.
                    float p = (t - T_CONJURO - 1) / (float)T_BARRIDO;
                    Vector2 centro = Vector2.Lerp(_pagDe, _pagHacia, p);
                    if ((t & 1) == 0)
                        _puffs.Add(new Puff
                        {
                            Pos = centro + new Vector2(Main.rand.NextFloat(-18f, 18f),
                                Main.rand.NextFloat(-14f, 14f)),
                            Vel = new Vector2(-lado * 1.1f, 0.4f) + Main.rand.NextVector2Unit() * 0.4f,
                            Vida = Main.rand.NextFloat(0.35f, 0.7f),
                            Tam = Main.rand.NextFloat(16f, 30f)
                        });
                    // El libro SIGUE a su página a distancia (la dirige).
                    _pos = Vector2.Lerp(_reposo, _reposo + new Vector2(-lado * 90f, -20f), p);
                    _vel = new Vector2(-lado * 3f, 0f);
                }
                else if (t == T_CONJURO + T_BARRIDO + 1)
                {
                    // EL TAJO: la página CORTÓ el logo — tres manchas a lo
                    // largo del tajo y su ráfaga.
                    for (int i = 0; i < 3; i++)
                        Manchar(Vector2.Lerp(_pagDe, _pagHacia, 0.34f + i * 0.16f), 1.05f);
                    Rafaga(Vector2.Lerp(_pagDe, _pagHacia, 0.5f), 15);
                }
                else if (t >= T_CONJURO + T_BARRIDO + T_DISIPA)
                {
                    // La página se disipó: al siguiente golpe.
                    _golpesLogo = 2;
                    _tActo = 0;
                }
            }
            else
            {
                // ====== GOLPES 2-3 — LA ANDANADA DE LOS MINIMIONES ======
                const int T_DESPLIEGUE = 30, T_FUEGO = 66, T_RECOGIDA = 30;
                int t = _tActo;
                float lado = _reposoIzquierda ? -1f : 1f;

                // EL LIBRO estaciona y MIRA (dirige la andanada).
                if (t < T_DESPLIEGUE) Volar(_reposo, 0.20f, 5f, 1f / 60f);

                // LA LÍNEA DE TIRO de los tres ojos (frente al logo).
                Vector2 anclaTiro = new Vector2(
                    Main.screenWidth * 0.5f + lado * 130f, 100f - 78f);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 destino = anclaTiro + new Vector2(i * 34f * -lado - 34f, i * 10f);
                    if (t < T_DESPLIEGUE)
                    {
                        // SALEN de la corona (interpolación con su vaivén).
                        float p = MathHelper.Clamp(t / (float)T_DESPLIEGUE, 0f, 1f);
                        float ang = Main.GlobalTimeWrappedHourly * 0.75f + i * MathHelper.TwoPi / 3f;
                        Vector2 corona = _pos + new Vector2(MathF.Cos(ang) * 17f, MathF.Sin(ang) * 14f - 3f);
                        _ojosActo[i] = Vector2.Lerp(corona, destino, p * p * (3f - 2f * p));
                    }
                    else if (t < T_DESPLIEGUE + T_FUEGO)
                    {
                        _ojosActo[i] = destino + new Vector2(
                            MathF.Sin(t * 0.11f + i * 2.1f) * 4f, MathF.Sin(t * 0.07f + i) * 3f);
                    }
                    else
                    {
                        // RECOGIDA: vuelven a la corona.
                        float p = MathHelper.Clamp((t - T_DESPLIEGUE - T_FUEGO) / (float)T_RECOGIDA, 0f, 1f);
                        float ang = Main.GlobalTimeWrappedHourly * 0.75f + i * MathHelper.TwoPi / 3f;
                        Vector2 corona = _pos + new Vector2(MathF.Cos(ang) * 17f, MathF.Sin(ang) * 14f - 3f);
                        _ojosActo[i] = Vector2.Lerp(destino, corona, p * p * (3f - 2f * p));
                    }
                }

                // EL FUEGO: cada ojo suelta DOS virotes (descalzados).
                if (t > T_DESPLIEGUE && t <= T_DESPLIEGUE + T_FUEGO &&
                    (t - T_DESPLIEGUE) % 11 == 0)
                {
                    int ronda = (t - T_DESPLIEGUE) / 11;         // 0..5
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
                    // EL RETROCESO del ojo que dispara.
                    _ojosActo[ojo] -= rumbo.SafeNormalize(Vector2.UnitX) * 3f;
                }

                if (t >= T_DESPLIEGUE + T_FUEGO + T_RECOGIDA)
                {
                    if (_golpesLogo >= 3)
                    {
                        // El tercer golpe cumplió: solo espera a que caigan
                        // los últimos virotes… y se va.
                        if (_virotes.Count == 0) FinDelActo();
                    }
                    else
                    {
                        _golpesLogo++;
                        _tActo = 0;
                    }
                }
            }
        }

        /// <summary>AGARRAR Y ARRASTRAR EL SOL (v6.50.128 — la letra:
        /// «cuando agarra al sol no lo mueve, deberie por ejemplo
        /// atrazarlo o cambiar la posicion del sol, ademas al agarrar al
        /// sol deberia usar los anillos runicos de los agujeros negros
        /// para sujetar al sol jajajaa»): LOS ANILLOS RÚNICOS ciñen al
        /// sol (la matemática de SigiloLib, portada al menú), el OJO abre
        /// sobre él… y se lo ARRASTRAN: sunModY lo hunde ~190 px en el
        /// cielo y Main.time AVANZA — el sol se DESLIZA por su arco de
        /// verdad, a la vista de todos.</summary>
        private static void ActuarSol()
        {
            const int T_ANILLOS = 46, T_AGARRE = 30, T_TIRON = 15, T_ARRASTRE = 115, T_SUELTA = 36;
            Vector2 sol = PosicionDelSol();
            _foco = sol;
            int t = _tActo;

            if (t < T_ANILLOS)
            {
                // LOS ANILLOS LLEGAN: el libro se cierne a un costado
                // mientras la doble corona rúnica ciñe (dibujada aparte).
                Volar(sol + new Vector2(38f, -30f), 0.16f, 5f, 1f / 60f);
            }
            else if (t < T_ANILLOS + T_AGARRE)
            {
                // EL AGARRE: los anillos APRIETAN — chispas de fricción.
                _pos = sol + new Vector2(30f, -24f);
                _vel = Vector2.Zero;
                if ((t & 3) == 0)
                    Rafaga(sol + Main.rand.NextVector2Unit() * 22f, 2);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON)
            {
                // EL TIRÓN ARRIBA (el primer jalón — el sol se estira).
                Main.sunModY = (short)MathHelper.Clamp(Main.sunModY - 3, -320, 320);
                _pos = sol + new Vector2(12f, -44f);
                _vel = new Vector2(0f, -2.4f);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
            {
                // EL ARRASTRE (la letra: «atrazarlo o cambiar la posicion
                // del sol»): el sol SE HUNDE (~190 px netos) y ADEMÁS se
                // DESLIZA por su arco — el reloj del cielo avanza a la
                // fuerza. Los anillos resbalan, el libro se recuesta.
                Main.sunModY = (short)MathHelper.Clamp(Main.sunModY + 2, -320, 320);
                Main.time += 26.0;
                _pos = sol + new Vector2(26f, -28f);
                _vel = new Vector2(3.2f, 1.4f);
                if ((t & 5) == 0)
                    Rafaga(sol + new Vector2(-20f, 18f) + Main.rand.NextVector2Unit() * 14f, 2);
            }
            else if (t < T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE + T_SUELTA)
            {
                // LA SUELTA: los anillos se abren y se apagan; el libro
                // sale despedido del retroceso.
                if (t == T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
                {
                    _vel = new Vector2(-5.5f, -3.2f);
                    Rafaga(sol, 10);
                }
                _pos += _vel;
                _vel *= 0.94f;
            }
            else FinDelActo();

            // SI EL SOL SE FUE DE LA PANTALLA a la fuerza: fin anticipado.
            if (sol.X > Main.screenWidth + 60f || sol.X < -60f) FinDelActo();
        }

        /// <summary>CAMBIAR LA LUNA: el libro le da una VUELTA a la luna
        /// (la órbita del pacto)… y al completarla, la FASE GIRA (moonPhase
        /// +1: la luna cambia de cara) con su anillo violeta de anuncio.
        /// v6.50.128: de paso le da un TIRONSITO (moonModY) — también se
        /// deja arrastrar, solo que más timedita.</summary>
        private static void ActuarLuna()
        {
            const int T_VUELTA = 96;
            Vector2 luna = PosicionDeLaLuna();
            _foco = luna;
            float a = MathHelper.TwoPi * (_tActo / (float)T_VUELTA) - MathHelper.PiOver2;
            _pos = luna + new Vector2(MathF.Cos(a) * 48f, MathF.Sin(a) * 48f * 0.9f);
            _vel = new Vector2(-MathF.Sin(a), MathF.Cos(a)) * 3.2f;

            if (_tActo == T_VUELTA / 2)
            {
                // LA CARA NUEVA (una sola vez): la fase gira + el anillo.
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
            if (_tActo >= T_VUELTA + 24)
                FinDelActo();
        }

        /// <summary>El acto terminó: de vuelta a deambular (el bucle de la
        /// letra no termina mientras la pantalla viva).</summary>
        private static void FinDelActo()
        {
            _fase = FASE_VAGAR;
            _tFase = 0;
            _caminatas = 0;
            EmpezarPausa();
        }

        // ================================================================
        //  LOS FX COMUNES (manchas + ráfagas de chispas)
        // ================================================================

        /// <summary>UNA MANCHA de tinta sobre el título (el tamaño escala
        /// con <paramref name="fuerza"/> — el tajo de la página mancha MÁS
        /// GRANDE que un virote).</summary>
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

        /// <summary>El centro del SOL en pantalla (de día; de noche devuelve
        /// su fórmula igual — no se dibuja, pero la cuenta es la suya).</summary>
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

        /// <summary>El centro de la LUNA en pantalla (de noche — su reloj
        /// es time/32400 y su parábola la misma del sol; la textura es la
        /// de SU moonType, como vanilla).</summary>
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
        //  EL DIBUJADO — el vagabundo, sus ojos, sus manchas y sus chispas
        //  (v6.50.128: TODO en el LOTE PROPIO — ni un End fantasma)
        // ================================================================

        private static void Dibujar()
        {
            try
            {
                if (_lote == null)
                    _lote = new SpriteBatch(Main.graphics.GraphicsDevice);
            }
            catch { return; }

            // El lote ALFA (el cuerpo del libro, las manchas, la página).
            try
            {
                _lote.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, Main.UIScaleMatrix);
            }
            catch
            {
                // EL RESET DEL DISPOSITIVO: si el lote murió (cambio de
                // resolución en el menú), se descarta y RENACE al próximo
                // frame — el vagabundo jamás se queda mudo para siempre.
                try { _lote.Dispose(); } catch { }
                _lote = null;
                return;
            }
            try
            {
                DibujarManchas(_lote);
                DibujarElLibro(_lote);
                DibujarLaPagina(_lote);
            }
            catch { }
            try { _lote.End(); } catch { }

            // El lote ADITIVO (la brasa violeta + las chispas + los
            // virotes + los anillos rúnicos + el ojo del sol).
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
                if (_fase == FASE_ACTUAR && _acto == ACTO_SOL) DibujarActoDelSol(_lote);
                if (_fase == FASE_ACTUAR && _acto == ACTO_LUNA) DibujarAnillosRunicos(_lote,
                    PosicionDeLaLuna(), 20f, Main.GlobalTimeWrappedHourly, 0.55f);
            }
            catch { }
            try { _lote.End(); } catch { }
        }

        /// <summary>Las MANCHAS DE TINTA sobre el título (acumuladas por
        /// el tajo de la página y los virotes — la evidencia del crimen).</summary>
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
                // EL PLOP: la mancha nace y en 0,25 s se asienta a su tamaño.
                float plop = MathHelper.Clamp((t - m.Nace) / 0.25f, 0f, 1f);
                float tam = m.Tam * (0.6f + 0.4f * plop);
                Color tinta = new Color(24, 8, 34) * (m.Alfa * (0.85f + 0.15f * MathF.Sin(t * 2f + i)));
                sb.Draw(glow, m.Pos, null, tinta, m.Giro, glow.Size() * 0.5f,
                    new Vector2(tam, tam * 0.6f) / glow.Size(), SpriteEffects.None, 0f);
                sb.Draw(glow, m.Pos, null, tinta * 0.8f, m.Giro, glow.Size() * 0.5f,
                    new Vector2(tam * 0.5f, tam * 0.5f) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>EL LIBRO VAGABUNDO: la miniatura del mundo con su ojo
        /// vivo (mirando al FOCO — el cursor, el logo, el sol…), su
        /// parpadeo, su TRES OJOS en corona (o desplegados en la andanada),
        /// su RASTRO fantasma y la inclinación del giro.</summary>
        private static void DibujarElLibro(SpriteBatch sb)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            if (tex == null) return;

            const float esc = 0.52f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 2.4f;
            // EL CINEO: la inclinación nace de la VELOCIDAD (se recuesta
            // hacia donde vuela) + su vaivén perezoso de siempre.
            float giro = MathHelper.Clamp(_vel.X * 0.028f, -0.30f, 0.30f)
                + MathF.Sin(t * 1.2f) * 0.05f;
            Vector2 centro = _pos + new Vector2(0f, bob);
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Color luz = new Color(230, 224, 246);

            // EL RASTRO FANTASMA (solo cuando vuela de verdad).
            float rapidez = _vel.Length();
            if (rapidez > 3.4f)
            {
                float alfaR = MathHelper.Clamp((rapidez - 3.4f) / 5f, 0f, 1f) * 0.14f;
                for (int r = 1; r <= 3; r++)
                {
                    int idx = (_idxRastro - r * 2 + _rastro.Length * 2) % _rastro.Length;
                    Vector2 fantasma = _rastro[idx];
                    if (fantasma == Vector2.Zero) continue;
                    float f = 1f - r / 3f;
                    sb.Draw(tex, fantasma, frame, luz * (alfaR * f), giro,
                        origen, esc * (0.96f + 0.04f * f), SpriteEffects.None, 0f);
                }
            }

            sb.Draw(tex, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);

            // EL OJO VIVO (mira hacia el FOCO de la curiosidad).
            float parp = (t * 0.9f) % 5.2f;
            if (parp > 4.90f)
            {
                var parpado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                sb.Draw(parpado, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);
            }
            else
            {
                Vector2 rumbo = _foco - centro;
                Vector2 mirada = rumbo.LengthSquared() > 4f
                    ? Vector2.Normalize(rumbo) * new Vector2(3.5f, 2.8f) * 0.9f
                    : Vector2.Zero;
                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * esc;
                Vector2 posOjo = centro + alOjo.RotatedBy(giro);
                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                sb.Draw(iris, posOjo, null, luz, giro, iris.Size() * 0.5f,
                    esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
            }

            // LOS TRES OJOS: en CORONA compacta… o DESPLEGADOS en plena
            // andanada contra el título (v6.50.128).
            Texture2D ojoAbierto = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Texture2D ojoCerrado = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
            var irisOjo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            if (ojoAbierto == null || ojoCerrado == null || irisOjo == null) return;

            bool enAndanada = _fase == FASE_ACTUAR && _acto == ACTO_LOGO && _golpesLogo > 1;
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
                float parpOjo = (t * 1.15f + i * 2.61f) % 6.5f;
                bool cerrado = parpOjo <= 0.26f;
                Texture2D cuerpo = cerrado ? ojoCerrado : ojoAbierto;
                sb.Draw(cuerpo, p, null, luz, 0f, cuerpo.Size() * 0.5f, escOjo,
                    SpriteEffects.None, 0f);
                if (!cerrado)
                {
                    Vector2 rumbo = _foco - p;
                    Vector2 mira = rumbo.LengthSquared() > 4f
                        ? Vector2.Normalize(rumbo) * new Vector2(5f, 2f)
                        : Vector2.Zero;
                    sb.Draw(irisOjo, p + (new Vector2(0.7f, 2.3f) + mira) * escOjo, null,
                        luz, 0f, irisOjo.Size() * 0.5f, 0.41f * escOjo, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>LA SOMBRA DE LA PÁGINA (v6.50.128 — el golpe 1 del
        /// título): la página de tinta que BARRE el logo — un cuerpo
        /// oscuro ALARGADO e inclinado (el tajo), borde dentado, su ojo
        /// que mira hacia adelante y la bruma que le nace detrás. Vive
        /// solo durante el barrido (los puffs duran un poco más).</summary>
        private static void DibujarLaPagina(SpriteBatch sb)
        {
            const int T_CONJURO = 26, T_BARRIDO = 22, T_DISIPA = 24;
            if (_fase != FASE_ACTUAR || _acto != ACTO_LOGO || _golpesLogo != 1) return;
            int t = _tActo;
            if (t < T_CONJURO || t > T_CONJURO + T_BARRIDO + T_DISIPA) return;

            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            float p = MathHelper.Clamp((t - T_CONJURO) / (float)(T_BARRIDO + T_DISIPA), 0f, 1f);
            float vive = t <= T_CONJURO + T_BARRIDO
                ? 1f
                : 1f - (t - T_CONJURO - T_BARRIDO) / (float)T_DISIPA;
            Vector2 centro = Vector2.Lerp(_pagDe, _pagHacia,
                MathHelper.Clamp((t - T_CONJURO) / (float)T_BARRIDO, 0f, 1f));

            // EL CUERPO: la página de tinta — cápsula oscura inclinada (el
            // tajo). Negro con RIM violeta (la lección de la casa: el
            // negro puro es invisible sobre fondo oscuro).
            Color tinta = new Color(16, 6, 26) * (0.88f * vive);
            Vector2 tam = new Vector2(150f, 74f);
            sb.Draw(glow, centro, null, tinta, _pagAng, glow.Size() * 0.5f,
                tam / glow.Size(), SpriteEffects.None, 0f);
            sb.Draw(glow, centro, null, tinta * 0.85f, _pagAng, glow.Size() * 0.5f,
                new Vector2(96f, 40f) / glow.Size(), SpriteEffects.None, 0f);

            // EL BORDE DENTADO: colmillos a lo largo del borde inferior
            // (los dientes de la fauce de la que nació).
            Vector2 rumbo = new Vector2(MathF.Cos(_pagAng), MathF.Sin(_pagAng));
            Vector2 normal = new Vector2(-rumbo.Y, rumbo.X);
            for (int i = 0; i < 6; i++)
            {
                float f = (i - 2.5f) / 2.5f;
                Vector2 diente = centro + rumbo * f * 66f + normal * 34f;
                sb.Draw(glow, diente, null, tinta * 0.92f, _pagAng + f * 0.16f,
                    glow.Size() * 0.5f, new Vector2(17f, 21f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }

            // EL OJO DE LA PÁGINA (mira hacia adelante del barrido).
            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            Texture2D ojo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            if (ojo != null)
            {
                sb.Draw(ojo, centro - normal * 6f, null, Color.White * (0.92f * vive),
                    _pagAng, ojo.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
                if (iris != null)
                    sb.Draw(iris, centro - normal * 6f + rumbo * 5f, null,
                        Color.White * vive, _pagAng, iris.Size() * 0.5f, 0.24f,
                        SpriteEffects.None, 0f);
            }

            // EL RIM VIOLETA (aditivo no disponible aquí: una orla tenue
            // alfabeta — la firma de las sombras del mod).
            sb.Draw(glow, centro, null, new Color(64, 42, 96) * (0.30f * vive), _pagAng,
                glow.Size() * 0.5f, (tam + new Vector2(26f, 26f)) / glow.Size(),
                SpriteEffects.None, 0f);
        }

        /// <summary>LA BRASA del vagabundo (el aura violeta de las páginas
        /// — aditiva) + LAS CHISPAS de tinta de sus golpes.</summary>
        private static void DibujarBrasaYChispas(SpriteBatch sb)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;
            float t = Main.GlobalTimeWrappedHourly;

            float pulso = 0.20f + 0.08f * MathF.Sin(t * 2.6f);
            sb.Draw(glow, _pos, null, new Color(110, 40, 170) * pulso, 0f,
                glow.Size() * 0.5f, new Vector2(52f, 52f) / glow.Size(),
                SpriteEffects.None, 0f);

            for (int i = 0; i < _chispas.Count; i++)
            {
                Chispa c = _chispas[i];
                Color col = (c.Violeta ? new Color(140, 60, 210) : new Color(230, 226, 245))
                    * MathHelper.Clamp(c.Vida * 1.6f, 0f, 1f);
                sb.Draw(glow, c.Pos, null, col, 0f, glow.Size() * 0.5f,
                    new Vector2(c.Tam, c.Tam) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS VIROTES DE LOS MINIMIONES (la andanada): trazos
        /// violeta con núcleo claro, estirados en su rumbo.</summary>
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

        /// <summary>LOS PUFFS de bruma (la estela de la Sombra de la
        /// Página): humo violeta oscuro que se desgarrar a la deriva.</summary>
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

        /// <summary>EL ACTO DEL SOL (v6.50.128): LOS ANILLOS RÚNICOS de
        /// los agujeros negros sujetando al sol (la matemática de
        /// SigiloLib — RingA/RingFlat/RingTilt/RingSpin/RunesOfRing —
        /// portada al espacio del menú) + EL OJO ETERNO que abre sobre él
        /// mientras lo arrastran.</summary>
        private static void DibujarActoDelSol(SpriteBatch sb)
        {
            const int T_ANILLOS = 46, T_AGARRE = 30, T_TIRON = 15, T_ARRASTRE = 115;
            int t = _tActo;
            Vector2 sol = PosicionDelSol();

            // === LOS ANILLOS: ciñen de lejos (3,4×) a un abrazo (1,05×),
            //     giran, aprietan en el agarre, resbalan en el arrastre y
            //     se abren al soltar. ===
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
                // EL RESBALÓN: el abrazo vibra con el tira y afloja.
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
            if (alfaAnillos > 0.02f)
                DibujarAnillosRunicos(sb, sol, 26f * ciñe, Main.GlobalTimeWrappedHourly,
                    alfaAnillos);

            // === EL AURA DEL SOL AGARRADO: la estrella "manoseada" brilla
            //     violeta mientras la arrastran. ===
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow != null && t >= T_ANILLOS + T_AGARRE && t <= T_ANILLOS + T_AGARRE + T_TIRON + T_ARRASTRE)
                sb.Draw(glow, sol, null, new Color(150, 70, 230) * 0.34f, 0f,
                    glow.Size() * 0.5f, new Vector2(74f, 74f) / glow.Size(),
                    SpriteEffects.None, 0f);

            // === EL OJO ETERNO: abre con el agarre, VIGILA el arrastre
            //     (mira hacia donde tira) y se entorna al soltar. ===
            float crece, alfa;
            int tOjo = t - T_ANILLOS;
            const int T_CRECE = 26, T_VIVE = T_AGARRE + T_TIRON + T_ARRASTRE - 10, T_MUERE = 30;
            if (tOjo < 0) { crece = 0f; alfa = 0f; }
            else if (tOjo < T_CRECE) { crece = tOjo / (float)T_CRECE; alfa = crece; }
            else if (tOjo < T_CRECE + T_VIVE) { crece = 1f; alfa = 1f; }
            else if (tOjo < T_CRECE + T_VIVE + T_MUERE)
            {
                float f = (tOjo - T_CRECE - T_VIVE) / (float)T_MUERE;
                crece = 1f - f; alfa = 1f - f;
            }
            else { crece = 0f; alfa = 0f; }
            if (alfa > 0.02f)
            {
                float suave = crece * crece * (3f - 2f * crece);
                Texture2D ojo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                if (ojo != null)
                {
                    float escala = 1.35f * suave * Main.ForcedMinimumZoom;
                    sb.Draw(ojo, sol, null, Color.White * (0.92f * alfa), 0f,
                        ojo.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                    var iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    if (iris != null && alfa > 0.5f)
                    {
                        // LA MIRADA DEL SOL: hacia donde lo arrastran (abajo
                        // y adelante — el rumbo del secuestro).
                        Vector2 mirada = new Vector2(6.5f, 5.0f) *
                            (t < T_ANILLOS + T_AGARRE + T_TIRON ? -1f : 1f);
                        sb.Draw(iris, sol + (new Vector2(0.7f, 2.3f) + mirada) * escala, null,
                            Color.White * alfa, 0f, iris.Size() * 0.5f,
                            0.41f * escala, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        /// <summary>LOS ANILLOS RÚNICOS DE LOS AGUJEROS NEGROS (v6.50.128
        /// — la letra: «al agarrar al sol deberia usar los anillos runicos
        /// de los agujeros negros para sujetar al sol jajajaa»): la
        /// geometría EXACTA de SigiloLib.SistemaAnillos (RingA, RingFlat,
        /// RingTilt con precesión, RingSpin alterno, RunesOfRing y la
        /// elipse girada EllipsePoint), portada al espacio del menú con
        /// el oro de siempre (CuerpoOro/PuntaOro) — tier 3, como el
        /// cerco del devorado.</summary>
        private static void DibujarAnillosRunicos(SpriteBatch sb, Vector2 centro,
            float R, float time, float alpha)
        {
            if (R < 2f || alpha <= 0.02f) return;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            Color cuerpo = new Color(255, 190, 80);    // SigiloLib.CuerpoOro
            Color punta = new Color(255, 240, 185);    // SigiloLib.PuntaOro

            // El spin del menú: un poco más vivo que el mundo (la letra
            // pidió ESPECTÁCULO — el vagabundo presume sus juguetes).
            const float spinUp = 1.8f;

            for (int k = 0; k < 3; k++)
            {
                // LA GEOMETRÍA DE SIGILOLIB (tier 3 — la triple corona):
                float a = (VFX.SigiloLib.RingA0 + VFX.SigiloLib.RingAStep * k) * R * 0.42f;
                float b = a * (0.34f + 0.07f * (k % 3));
                float tilt = -0.55f + 0.20f * k
                    + 0.10f * MathF.Sin(time * (0.35f + 0.06f * k) + k * 1.9f);
                float spin = time * ((k % 2 == 0 ? 1f : -1f) *
                    (VFX.SigiloLib.RingSpin0 + VFX.SigiloLib.RingSpinStep * k)) * spinUp;
                int runas = VFX.SigiloLib.Runes0 + VFX.SigiloLib.RuneStep * k;

                // EL ARO (el trazo elíptico): puntitos de oro a lo largo.
                for (int i = 0; i < 30; i++)
                {
                    float ang = i / 30f * MathHelper.TwoPi + spin * 0.4f;
                    Vector2 p = PuntoElipse(centro, a, b, tilt, ang);
                    float latir = 0.6f + 0.4f * MathF.Sin(time * 3f + i * 0.7f + k);
                    sb.Draw(glow, p, null, cuerpo * (0.11f * alpha * latir), 0f,
                        glow.Size() * 0.5f, new Vector2(3.4f, 3.4f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // LAS RUNAS: glifos MAYORES (cápsulas cruzadas — cada una
                // un carácter de la escritura mágica del sol).
                for (int g = 0; g < runas; g++)
                {
                    float ang = g / (float)runas * MathHelper.TwoPi + spin;
                    Vector2 p = PuntoElipse(centro, a, b, tilt, ang);
                    float tang = AnguloTangente(a, b, tilt, ang);
                    float latido = 0.7f + 0.3f * MathF.Sin(time * 2.2f + g * 1.9f + k * 2.4f);
                    // El trazo vertical de la runa…
                    sb.Draw(glow, p, null, cuerpo * (0.30f * alpha * latido), tang,
                        glow.Size() * 0.5f, new Vector2(2.4f, 7.5f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    // …y su cruce (el carácter completo).
                    sb.Draw(glow, p, null, punta * (0.30f * alpha * latido), tang + 0.62f,
                        glow.Size() * 0.5f, new Vector2(2.0f, 5.0f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>Punto sobre una elipse girada (la matemática de
        /// SigiloLib.EllipsePoint, verbatim).</summary>
        private static Vector2 PuntoElipse(Vector2 center, float a, float b, float tilt, float t)
        {
            float ct = MathF.Cos(t), st = MathF.Sin(t);
            Vector2 local = new Vector2(a * ct, b * st);
            float cR = MathF.Cos(tilt), sR = MathF.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        /// <summary>Ángulo de la TANGENTE de la elipse en t (la runa
        /// "de pie" sobre su órbita — SigiloLib, verbatim).</summary>
        private static float AnguloTangente(float a, float b, float tilt, float t)
        {
            float dx = -a * MathF.Sin(t), dy = b * MathF.Cos(t);
            return MathF.Atan2(dy, dx) + tilt;
        }
    }
}
