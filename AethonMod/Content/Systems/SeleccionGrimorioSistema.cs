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
    //  EL CIRCUITO:
    //  · GrimorioMenorPet.PreDraw detecta el DUMMY de vista previa del
    //    seleccionador (isAPreviewDummy — lo crea UICharacter.DrawPets) y
    //    posa el libro EN SU RECUADRO como cualquier mascota.
    //  · TickVistaPrevista (aquí) cuenta los segundos de pose. A los ~5 s
    //    (300 t) el libro ABANDONA el recuadro (la vista previa ya no lo
    //    dibuja) y ESTE sistema lo dibuja SOBRE TODA LA PANTALLA (hook a
    //    Main.PostDrawMenu — el último plano del menú: por encima incluso
    //    de la lista de personajes):
    //      — DEAMBULA por waypoints al azar;
    //      — ATACA EL TÍTULO de Terraria: embiste el logo y le deja
    //        MANCHAS DE TINTA acumuladas (el logo termina salpicado);
    //      — CAMBIA EL SOL (le planta EL OJO ETERNO encima y lo EMPUJA
    //        hacia abajo — sunModY, el mismo campo que arrastra el mouse
    //        en el título de vanilla) o CAMBIA LA LUNA (le gira la fase
    //        — moonPhase — con un destello violeta);
    //      — y vuelve a deambular (el bucle no termina mientras la
    //        pantalla siga abierta).
    //  · Si el jugador sale del seleccionador (o el personaje con la
    //    mascota sale de la lista), ~1,5 s sin vista previa REINICIAN
    //    todo (las manchas del logo se borran con la pantalla).
    //
    //  LA EXCEPCIÓN A LA REGLA DE LA CASA (documentada): aquí Main.rand
    //  es LEGAL — es teatro 100% local del MENÚ (una sola máquina, nada
    //  que sincronizar: el determinismo es para el mundo, no para el
    //  título). Las posiciones del sol y la luna se calculan con las
    //  FÓRMULAS EXACTAS de vanilla (DrawSunAndMoon descompilado: en el
    //  menú bgTopY = 0 — DrawBackground lo fija) para que el libro vuele
    //  a donde el cielo REAL tiene su estrella.
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
        private const int FASE_VAGAR = 1;     // deambula por la pantalla
        private const int FASE_IR = 2;        // vuela a su objetivo
        private const int FASE_ACTUAR = 3;    // el título / el sol / la luna

        // === LOS ACTOS (qué hace al llegar) ===
        private const int ACTO_LOGO = 0;      // embiste el título (manchas de tinta)
        private const int ACTO_SOL = 1;       // el ojo eterno sobre el sol (+ empujón)
        private const int ACTO_LUNA = 2;      // gira la fase de la luna

        private static int _fase;
        private static int _tFase;
        private static Vector2 _pos;
        private static Vector2 _vel;
        private static Vector2 _objetivo;
        private static int _acto;              // el acto EN CURSO
        private static int _actoProximo = ACTO_LOGO;   // alterna logo ↔ cielo
        private static int _caminatas;         // waypoints entre acto y acto
        private static int _golpesLogo;        // embestidas del acto actual
        private static int _tActo;
        private static Vector2 _reposo;        // el punto de arranque de cada embestida

        // === LAS MANCHAS DEL LOGO (la tinta acumulada del título) ===
        private struct Mancha
        {
            public Vector2 Pos;
            public float Tam;
            public float Alfa;
            public float Nace;      // el instante del golpe (el "plop" inicial)
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
            _vel = Vector2.Zero;
            _manchas.Clear();
            _chispas.Clear();
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
            float dt = 1f / 60f;

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
                    }
                    Volar(_objetivo, 0.06f, 8f);
                    if (Vector2.DistanceSquared(_pos, _objetivo) < 30f * 30f || _tFase > 120)
                    {
                        _fase = FASE_VAGAR;
                        _tFase = 0;
                        _caminatas = 0;
                    }
                    break;
                }

                case FASE_VAGAR:
                {
                    // DEAMBULAR: waypoints al azar por TODA la pantalla (la
                    // letra: «se ponga a deambular por toda la pantalla»);
                    // cada 2 caminatas, un ACTO (título ↔ cielo, alternando).
                    if (Vector2.DistanceSquared(_pos, _objetivo) < 26f * 26f || _tFase > 240)
                    {
                        _caminatas++;
                        if (_caminatas >= 2)
                        {
                            ElegirActo();
                            _fase = FASE_IR;
                            _tFase = 0;
                        }
                        else
                        {
                            _objetivo = new Vector2(
                                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
                            _tFase = 0;
                        }
                    }
                    Volar(_objetivo, 0.045f, 5.5f);
                    break;
                }

                case FASE_IR:
                {
                    // VOLAR AL ACTO: el objetivo se recalcula CADA frame si
                    // es el sol o la luna (se MUEVEN — el cielo del título
                    // gira sin parar).
                    if (_acto == ACTO_SOL) _objetivo = PosicionDelSol();
                    else if (_acto == ACTO_LUNA) _objetivo = PosicionDeLaLuna();
                    Volar(_objetivo, 0.05f, 7f);
                    if (Vector2.DistanceSquared(_pos, _objetivo) < 24f * 24f)
                    {
                        _fase = FASE_ACTUAR;
                        _tFase = 0;
                        _tActo = 0;
                        _golpesLogo = 0;
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

        /// <summary>EL PRÓXIMO ACTO: alterna título ↔ cielo (el cielo es
        /// el sol de día, la luna de noche — y si la estrella ya se puso,
        /// el título manda otra vez).</summary>
        private static void ElegirActo()
        {
            if (_actoProximo == ACTO_LOGO)
            {
                _acto = ACTO_LOGO;
                _actoProximo = ACTO_SOL;
                // Un punto al AZAR del logo (su centro es
                // (screenWidth/2, 100) — decompilado de DrawMenu).
                _objetivo = new Vector2(
                    Main.screenWidth * 0.5f + Main.rand.NextFloat(-150f, 150f),
                    100f + Main.rand.NextFloat(-22f, 26f));
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
                    _objetivo = sol;
                    return;
                }
            }
            else
            {
                Vector2 luna = PosicionDeLaLuna();
                if (luna.X > 40f && luna.X < Main.screenWidth - 40f)
                {
                    _acto = ACTO_LUNA;
                    _objetivo = luna;
                    return;
                }
            }
            _acto = ACTO_LOGO;
            _objetivo = new Vector2(
                Main.screenWidth * 0.5f + Main.rand.NextFloat(-150f, 150f),
                100f + Main.rand.NextFloat(-22f, 26f));
        }

        // ================================================================
        //  LOS ACTOS
        // ================================================================

        /// <summary>ATACAR EL TÍTULO (la letra: «atacando el titulo de
        /// terraria»): 3 embestidas con rebote, cada una deja su MANCHA
        /// DE TINTA en el logo (acumulan — el título termina salpicado) y
        /// su ráfaga de chispas.</summary>
        private static void ActuarLogo()
        {
            // LA EMBESTIDA: del reposo al logo (12 t de carga), el GOLPE
            // (mancha + chispas + rebote), y vuelta al reposo (16 t) —
            // tres veces.
            int t = _tActo % 30;
            if (_golpesLogo < 3)
            {
                if (t == 0)
                    _reposo = _objetivo + new Vector2(
                        (Main.rand.NextBool() ? 1f : -1f) * 130f,
                        Main.rand.NextFloat(-40f, -10f));
                if (t < 12)
                {
                    float p = t / 12f;
                    _pos = Vector2.Lerp(_reposo, _objetivo, p * p * (3f - 2f * p));
                }
                else if (t == 12)
                {
                    // EL GOLPE: la mancha vive en el logo, las chispas vuelan.
                    _manchas.Add(new Mancha
                    {
                        Pos = _objetivo,
                        Tam = Main.rand.NextFloat(13f, 24f),
                        Alfa = Main.rand.NextFloat(0.68f, 0.9f),
                        Nace = Main.GlobalTimeWrappedHourly
                    });
                    if (_manchas.Count > 14) _manchas.RemoveAt(0);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = Main.rand.NextFloat(MathHelper.TwoPi);
                        _chispas.Add(new Chispa
                        {
                            Pos = _objetivo,
                            Vel = new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f) *
                                  Main.rand.NextFloat(0.8f, 3.2f),
                            Vida = Main.rand.NextFloat(0.35f, 0.8f),
                            Tam = Main.rand.NextFloat(2.5f, 6f),
                            Violeta = Main.rand.NextBool()
                        });
                    }
                    _golpesLogo++;
                    // El próximo golpe, OTRA parte del logo.
                    _objetivo = new Vector2(
                        Main.screenWidth * 0.5f + Main.rand.NextFloat(-150f, 150f),
                        100f + Main.rand.NextFloat(-22f, 26f));
                }
                else
                {
                    float p = MathHelper.Clamp((t - 12) / 18f, 0f, 1f);
                    _pos = Vector2.Lerp(_objetivo, _reposo, p * p * (3f - 2f * p));
                }
            }
            else if (_tActo > 100)
            {
                FinDelActo();
            }
        }

        /// <summary>CAMBIAR EL SOL (la letra: «cambiando el sol o la luna
        /// en el fondo de la pantalla de titulo»): el libro se POSA en el
        /// sol… y le planta EL OJO ETERNO encima (el sol MIRA — crece,
        /// parpadea, se encoge) + un EMPUJÓN hacia abajo (sunModY: el
        /// mismo campo que el mouse arrastra en el título de vanilla — el
        /// sol VISIBLEMENTE baja y su propia inercia 0,999 lo devuelve).</summary>
        private static void ActuarSol()
        {
            const int T_CRECE = 40, T_VIVE = 160, T_MUERE = 30;
            Vector2 sol = PosicionDelSol();

            // EL EMPUJÓN (una sola vez, al llegar).
            if (_tActo == 1)
                Main.sunModY = (short)Math.Min(120, Main.sunModY + 34);

            // EL LIBRO SE POSA en el borde del sol (lo sigue mientras el
            // cielo lo mueve).
            _pos = sol + new Vector2(34f, -26f);

            if (_tActo >= T_CRECE + T_VIVE + T_MUERE)
                FinDelActo();
        }

        /// <summary>CAMBIAR LA LUNA: el libro le da una VUELTA a la luna
        /// (la órbita del pacto)… y al completarla, la FASE GIRA (moonPhase
        /// +1: la luna cambia de cara) con su anillo violeta de anuncio.</summary>
        private static void ActuarLuna()
        {
            const int T_VUELTA = 90;
            Vector2 luna = PosicionDeLaLuna();
            float a = MathHelper.TwoPi * (_tActo / (float)T_VUELTA) - MathHelper.PiOver2;
            _pos = luna + new Vector2(MathF.Cos(a) * 46f, MathF.Sin(a) * 46f * 0.9f);

            if (_tActo == T_VUELTA / 2)
            {
                // LA CARA NUEVA (una sola vez): la fase gira + el anillo.
                Main.moonPhase = (Main.moonPhase + 1) % 8;
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
            _objetivo = new Vector2(
                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
        }

        /// <summary>El vuelo suave del vagabundo (muelle hacia el objetivo).</summary>
        private static void Volar(Vector2 objetivo, float muelle, float tope)
        {
            Vector2 hacia = objetivo - _pos;
            _vel = Vector2.Lerp(_vel, hacia * muelle * 12f, 0.12f);
            if (_vel.Length() > tope) _vel = Vector2.Normalize(_vel) * tope;
            _pos += _vel;
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
        //  (el último plano del menú: dos lotes propios, alfa y aditivo)
        // ================================================================

        private static void Dibujar()
        {
            SpriteBatch sb = Main.spriteBatch;

            // EL ESTADO DEL MENÚ (leído del Main.Draw descompilado): antes
            // de DrawMenu el motor abre SU lote (Deferred·AlphaBlend·
            // LinearClamp·None·Rasterizer·UIScaleMatrix) y JUSTO después
            // de PostDrawMenu la rama del menú hace return — el lote que
            // dejemos aquí es el lote que se lleva el frame. Se restaura
            // EXACTO; y si por cualquier razón no hubiera lote abierto, el
            // primer End falla y el vagabundo simplemente no molesta.
            try { sb.End(); }
            catch { return; }

            // El lote ALFA (el cuerpo del libro, los ojos, las manchas del logo).
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.UIScaleMatrix);
            try
            {
                DibujarManchas(sb);
                DibujarElLibro(sb);
            }
            catch { }

            // El lote ADITIVO (la brasa violeta del vagabundo + sus chispas
            // + el OJO DEL SOL).
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.UIScaleMatrix);
            try
            {
                DibujarBrasaYChispas(sb);
                if (_fase == FASE_ACTUAR && _acto == ACTO_SOL) DibujarOjoDelSol(sb);
            }
            catch { }

            // EL RETORNO EXACTO al estado del menú (la rama del menú
            // devuelve el frame con este lote abierto).
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.UIScaleMatrix);
        }

        /// <summary>Las MANCHAS DE TINTA sobre el título (acumuladas por
        /// las embestidas — la evidencia del crimen).</summary>
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
                sb.Draw(glow, m.Pos, null, tinta, 0f, glow.Size() * 0.5f,
                    new Vector2(tam, tam * 0.6f) / glow.Size(), SpriteEffects.None, 0f);
                sb.Draw(glow, m.Pos, null, tinta * 0.8f, 0f, glow.Size() * 0.5f,
                    new Vector2(tam * 0.5f, tam * 0.5f) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>EL LIBRO VAGABUNDO: la misma miniatura del mundo (el
        /// cuerpo del Eterno a 0,52 con su ojo vivo y su parpadeo) con sus
        /// TRES OJOS en corona compacta — volando donde quiera.</summary>
        private static void DibujarElLibro(SpriteBatch sb)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            if (tex == null) return;

            const float esc = 0.52f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 2.4f;
            float giro = MathHelper.Clamp(_vel.X * 0.02f, -0.22f, 0.22f)
                + MathF.Sin(t * 1.2f) * 0.05f;
            Vector2 centro = _pos + new Vector2(0f, bob);
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Color luz = new Color(230, 224, 246);

            sb.Draw(tex, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);

            // EL OJO VIVO (mira hacia donde vuela — el rumbo del vagabundo).
            float parp = (t * 0.9f) % 5.2f;
            if (parp > 4.90f)
            {
                var parpado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                sb.Draw(parpado, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);
            }
            else
            {
                Vector2 rumbo = _vel.LengthSquared() > 0.04f ? Vector2.Normalize(_vel) : Vector2.UnitX;
                Vector2 mirada = rumbo * new Vector2(3.5f, 2.8f) * 0.9f;
                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * esc;
                Vector2 posOjo = centro + alOjo.RotatedBy(giro);
                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                sb.Draw(iris, posOjo, null, luz, giro, iris.Size() * 0.5f,
                    esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
            }

            // LOS TRES OJOS (la corona compacta del vagabundo).
            Texture2D ojoAbierto = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Texture2D ojoCerrado = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
            var irisOjo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            if (ojoAbierto == null || ojoCerrado == null || irisOjo == null) return;
            const float escOjo = 0.22f;
            for (int i = 0; i < 3; i++)
            {
                float ang = t * 0.75f + i * MathHelper.TwoPi / 3f;
                float radio = 17f + MathF.Sin(t * 0.9f) * 1.5f;
                Vector2 p = centro + new Vector2(MathF.Cos(ang) * radio,
                    MathF.Sin(ang) * radio * 0.82f + MathF.Sin(t * 1.6f + i * 2.4f) * 1.5f - 3f);
                float parpOjo = (t * 1.15f + i * 2.61f) % 6.5f;
                bool cerrado = parpOjo <= 0.26f;
                Texture2D cuerpo = cerrado ? ojoCerrado : ojoAbierto;
                sb.Draw(cuerpo, p, null, luz, 0f, cuerpo.Size() * 0.5f, escOjo,
                    SpriteEffects.None, 0f);
                if (!cerrado)
                {
                    Vector2 rumbo = _vel.LengthSquared() > 0.04f ? Vector2.Normalize(_vel) : Vector2.UnitX;
                    sb.Draw(irisOjo, p + (new Vector2(0.7f, 2.3f)
                        + rumbo * new Vector2(5f, 2f)) * escOjo, null, luz, 0f,
                        irisOjo.Size() * 0.5f, 0.41f * escOjo, SpriteEffects.None, 0f);
                }
            }
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

        /// <summary>EL OJO DEL SOL: mientras el libro lo visita, EL OJO
        /// ETERNO se abre SOBRE el sol (crece, mira de lado a lado, se
        /// encoge al despedirse — el sol de Terraria poseso por el
        /// grimorio, la letra: «cambiando el sol»).</summary>
        private static void DibujarOjoDelSol(SpriteBatch sb)
        {
            const int T_CRECE = 40, T_VIVE = 160, T_MUERE = 30;
            int t = _tActo;
            if (t > T_CRECE + T_VIVE + T_MUERE) return;

            float crece, alfa;
            if (t < T_CRECE) { crece = t / (float)T_CRECE; alfa = crece; }
            else if (t < T_CRECE + T_VIVE) { crece = 1f; alfa = 1f; }
            else { float f = (t - T_CRECE - T_VIVE) / (float)T_MUERE; crece = 1f - f; alfa = 1f - f; }
            float suave = crece * crece * (3f - 2f * crece);

            Vector2 sol = PosicionDelSol();
            float tReloj = Main.GlobalTimeWrappedHourly;
            Texture2D ojo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            if (ojo == null) return;
            var iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;

            // El sol de vanilla mide ~24-30 px y su escala vive en el cielo
            // (~1,2×ForcedMinimumZoom en su cénit): el ojo lo CUBRE.
            float escala = 1.35f * suave * Main.ForcedMinimumZoom;
            sb.Draw(ojo, sol, null, Color.White * (0.92f * alfa), 0f,
                ojo.Size() * 0.5f, escala, SpriteEffects.None, 0f);

            // EL IRIS QUE MIRA (el reloj del menú — el sol te sigue un
            // instante con la mirada… y se va).
            if (iris != null && alfa > 0.5f)
            {
                Vector2 mirada = new Vector2(MathF.Sin(tReloj * 0.7f) * 7.5f,
                    MathF.Sin(tReloj * 0.43f) * 2.5f);
                sb.Draw(iris, sol + (new Vector2(0.7f, 2.3f) + mirada) * escala, null,
                    Color.White * alfa, 0f, iris.Size() * 0.5f,
                    0.41f * escala, SpriteEffects.None, 0f);
            }
        }
    }
}
