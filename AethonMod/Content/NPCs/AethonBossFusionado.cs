using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  v6.50.117 — EL RECUERDO COMPLETO (la letra del usuario): «cuando
    //  ambos lleguen a 1000, los dos jefes se fusionan en uno solo, ese
    //  sería más fuerte con ataques más vistosos».
    //
    //  Nace SOLO de LA FUSIÓN (la cinemática de la base lo trae al mundo
    //  con la vida entera, donde los dos fragmentos se abrazaron). Su
    //  cuerpo es UN ECLIPSE: el sol dorado de la base (heredado intacto)
    //  con LA MITAD OSCURA deslizándose encima — mitad oro, mitad
    //  negro-morado, el dios completo que fue.
    //
    //  EL MOVESET NUEVO (la letra, punto por punto — reusa las
    //  primitivas del dios: pernos, novas y columnas de siempre, con
    //  lógica nueva):
    //    a · EL ECLIPSE GEMELO — la media pantalla ACTIVA alterna: la
    //        lluvia cae del lado que ARDE y el color inactivo se dibuja
    //        tenue (corre al lado que no duele).
    //    b · EL SOL MORIBUNDO — el sol negro GIGANTE (×1.8) LENTO que
    //        ATRAE al jugador (<900 px): castiga la calma, no el reflejo.
    //    c · LA REJILLA LÁSER BICOLOR — barras horizontales y verticales
    //        alternando el color activo (uno duele, el otro es aviso).
    //    d · LA LLUVIA DE PÁGINAS — páginas de tinta girando en oleadas,
    //        con rayos dorados cosiendo entre ellas.
    //    e · LOS ANILLOS DE FUSIÓN — la nova del dios, duplicada: anillos
    //        expandiéndose que ALTERNAN luz y oscuridad.
    //    f · EL ÚLTIMO ALIENTO — a vida ≤1% deja de atacar: se separa
    //        3 s en las DOS siluetas (ambas MIRAN al jugador — el
    //        núcleo-ojo apunta), se disuelven en bruma… y el CINE DE
    //        MUERTE de siempre (con el botín COMPLETO + 1 esencia extra
    //        que compensa los fragmentos que nunca dropearon).
    //
    //  LAS REGLAS HEREDADAS: máximo UN ataque grande a la vez (ya es
    //  así por estados), el batón de turnos NO le aplica (no tiene
    //  hermano) y el mediodía eterno lo sostiene AethonLlegadaSistema
    //  (que ya cuenta al fusionado entre los vivos).
    // ======================================================================
    //  v6.50.120 — EL JEFE FINAL DE VERDAD (la letra del usuario:
    //  «haciéndose más grandes, más épicos e investiga jefes finales
    //  para crear ataques y escenarios que sirvan para eso»):
    //
    //  EL COLOSO — EscalaCuerpo 3.40 (los gemelos son 2.50: el recuerdo
    //  completo los DEVORA en tamaño — TODO su sol de código escala
    //  solo: núcleo, velo, halo, disco, rim y siluetas), hitbox de 770
    //  (48 tiles de sol) y 3.000.000 de vida (la fuerza de ambos).
    //
    //  LA NOCHE DEL ECLIPSE (EL ESCENARIO) — su mitad oscura SE COMIÓ
    //  EL SOL: el mundo se apaga un pelo alrededor de la pelea (Ilumina
    //  = false), solo la PENUMBRA violeta del recuerdo lo mantiene
    //  legible… y sus RELÁMPAGOS de fondo cada ~3-5 s (Moon Lord trae
    //  su cielo; el eclipse trae sus relámpagos — cada fase ABRE con
    //  trueno).
    //
    //  EL MENÚ QUE CRECE CON LA FASE (la escalada de los finales de
    //  Terraria — cada arma nueva es un homenaje investigado):
    //    g · EL JUICIO FANTASMAL (P5) — Moon Lord, el Phantasmal
    //        Deathray: DOS ruedas de rayos reusando la primitiva de la
    //        danza solar, una por flanco, barriendo CRUZADAS sobre la
    //        presa (F_RAYOS = 36).
    //    h · EL CONCILIO DE ESPEJOS (P3+) — Lunatic Cultist, el ritual:
    //        círculos de pernos que CARGAN quietos y CONVERGEN al punto
    //        donde estabas (el dodge legible: salir del círculo antes
    //        del cierre) — F_RITUAL = 37, el perno es el estilo NUEVO
    //        EstiloRitualConvergente del AtaqueFusionProjectile.
    //    i · LA TORMENTA DEL ABISMO (P4+) — Duke Fishron, el sharknado:
    //        tres vórtices que acechan a los flancos (jamás cierran a
    //        <600 px de la presa) escupiendo pernos en una espiral que
    //        gira (F_TORMENTA = 38 — sin proyectil nuevo: anclas + los
    //        pernos de siempre).
    // ======================================================================
    //  v6.50.121 — EL COLOSO FLOTA VIVO (la letra del usuario: «todas
    //  las mecánicas de su combate se sienten mal» — la auditoría: el
    //  gigante quedaba PLANTADO quieto en sus reposos mientras las
    //  armas trabajan): TODOS los reposos de combate respiran con una
    //  deriva lateral viva — puntoDeReposo + (sin(_tickEstado·0.028)·N, 0):
    //  ±80 px en ECLIPSE/SOLMOR/REJILLA/PÁGINAS/RAYOS, ±40 en TORMENTA
    //  (sus anclas acechan solas: el jefe no se aleja del centro del
    //  cerco) y ±50 en ANILLOS (la contracción deriva en vez de
    //  clavarse). SIN tocar: EL ÚLTIMO ALIENTO (la despedida quieta A
    //  PROPÓSITO), la cinemática de nacimiento y la muerte. Y RECORTE
    //  de las contemplaciones con colas muertas: ECLIPSE 340→300,
    //  REJILLA 470→390 y RAYOS 615→570 (la danza de las ruedas muere a
    //  ~t575 del estado: cierre limpio). El reposo vertical
    //  (−640…−700) NO se toca: con hitbox 770 el borde inferior queda
    //  a −255 del jugador — cerca sin contacto injusto.
    // ======================================================================
    [AutoloadBossHead]   // v6.51.1 — LA CURA DE LA BARRA: los atributos NO se heredan — el fusionado jamás registró SU cabeza
    public class AethonBossFusionado : AethonBoss
    {

        // === LOS ESTADOS PROPIOS DEL RECUERDO COMPLETO (los que la base
        //     no conoce — su switch se los pasa a EstadoFusionado) ===
        private const int F_ECLIPSE = 30;    // a — EL ECLIPSE GEMELO
        private const int F_SOLMOR = 31;     // b — EL SOL MORIBUNDO
        private const int F_REJILLA = 32;    // c — LA REJILLA LÁSER BICOLOR
        private const int F_PAGINAS = 33;    // d — LA LLUVIA DE PÁGINAS
        private const int F_ANILLOS = 34;    // e — LOS ANILLOS DE FUSIÓN
        private const int F_ALIENTO = 35;    // f — EL ÚLTIMO ALIENTO (≤1%)

        // === v6.50.120 — LOS TRES ATAQUES DE JEFE FINAL (la letra:
        //     «investiga jefes finales para crear ataques y escenarios
        //     que sirvan para eso»): cada uno es un HOMENAJE con
        //     mecánica propia, y el menú los va DESTAPANDO con la furia
        //     de la fase (ElegirAtaque) ===
        private const int F_RAYOS = 36;     // g — EL JUICIO FANTASMAL (Moon Lord · P5)
        private const int F_RITUAL = 37;    // h — EL CONCILIO DE ESPEJOS (Cultista · P3+)
        private const int F_TORMENTA = 38;  // i — LA TORMENTA DEL ABISMO (Duque · P4+)

        /// <summary>EL GATILLO DEL 1% vive UNA sola vez (la despedida no se repite).</summary>
        private bool _alientoDado = false;

        // === v6.50.120 — LA NOCHE DEL ECLIPSE y LA TORMENTA (estado propio) ===
        private int _tickRelampago = 120;       // el cielo del eclipse: cada ~3-5 s, UN relámpago
        private Vector2[] _anclasTormenta = new Vector2[3];  // los TRES vórtices (izq · ojo · der)
        private float _angTormenta = 0f;        // el ángulo de la espiral de ráfagas (+0.62 por ráfaga)
        private bool _tormentaSembrada = false; // el sembrado corre UNA sola vez por estado

        // El cuerpo es 100% código: se reusa la textura del jefe de luz
        // como ancla del motor (jamás se ve — el PreDraw manda).
        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        // La cabeza de la barra: EL SOL del recuerdo (el eclipse entero).
        public override string BossHeadTexture => "AethonMod/Content/NPCs/AethonBoss_Head_Boss";

        // === LOS ANCLAJES DE LA BASE (los tres que lo cambian todo) ===

        /// <summary>ES EL RECUERDO COMPLETO: sin hermano, sin órbita, sin fusión.</summary>
        protected override bool EsFusion => true;

        /// <summary>Nace del abrazo de la fusión: NUNCA corre la llegada
        /// (nace YA en la órbita de pelea, dentro del flash).</summary>
        protected override bool HaceLlegada => false;

        /// <summary>Su hermano no existe (0 = la IA de fusión lo salta).</summary>
        protected override int TipoHermano => 0;

        /// <summary>v6.50.120 — LA NOCHE DEL ECLIPSE: el Recuerdo
        /// Completo NO inunda el mundo con su luz — su mitad oscura SE
        /// COMIÓ EL SOL (la letra: «más épicos… y escenarios que sirvan
        /// para eso»): el mundo se apaga un pelo alrededor de la pelea,
        /// y solo SU penumbra violeta lo mantiene legible (la enciende
        /// PasoFusionPrevia, cada tick).</summary>
        protected override bool Ilumina => false;

        /// <summary>v6.50.120 — EL COLOSO (la letra: «haciéndose más
        /// grandes, más épicos»): 3.40 — los gemelos son 2.50 y el
        /// recuerdo completo los DEVORA en tamaño. TODO su sol de código
        /// escala SOLO con esta cifra: núcleo, velo, halo, el disco del
        /// eclipse, su rim y las siluetas del último aliento.</summary>
        protected override float EscalaCuerpo => 3.40f;

        /// <summary>En el ÚLTIMO ALIENTO todo calla (la despedida no dispara).</summary>
        protected override bool SilencioFinal => _estado == F_ALIENTO;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El DisplayName vive en la localización (NPCs.AethonBossFusionado).
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            // LA FUERZA DE AMBOS (la letra: «más fuerte»): la vida entera
            // del dios original — pero con el moveset nuevo completo.
            // v6.50.120 — EL COLOSO DE LA FASE FINAL (la letra:
            // «haciéndose más grandes»): 48 tiles de sol (los gemelos
            // son 34) y la fuerza de AMBOS en una sola barra — la letra
            // de la .117: «más fuerte con ataques más vistosos».
            NPC.width = 770;
            NPC.height = 770;
            NPC.lifeMax = 3_000_000;
        }

        protected override void PasoFusionPrevia()
        {
            base.PasoFusionPrevia();   // (la base vuelve YA: EsFusion no busca hermano)

            // EL ÚLTIMO ALIENTO (la letra): a vida ≤1% el recuerdo DEJA de
            // atacar y se DESPIDE — la separación en las dos siluetas la
            // dibuja el PreDraw, la disolución la corre el estado.
            if (!_alientoDado && !_muriendo &&
                _estado != F_ALIENTO && _estado != EST_MURIENDO &&
                NPC.life * 100f <= NPC.lifeMax)
            {
                _alientoDado = true;
                _coreoLargo = 0;
                _estado = F_ALIENTO;
                _tickEstado = 0;
                NPC.dontTakeDamage = true;   // 3 s de gracia: la despedida no se interrumpe a golpes
                NPC.netUpdate = true;        // MP: el cambio de estado viaja YA
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.5f), NPC.Center);
                OndaLib.Kick(9f, 20);
            }

            // v6.50.120 — LA PENUMBRA DEL ECLIPSE (la legibilidad de la
            // noche): el Recuerdo Completo NO alumbra el mundo (Ilumina
            // = false — su mitad oscura SE COMIÓ EL SOL), pero deja SU
            // PROPIA luz mínima: la penumbra violeta del eclipse, para
            // que el coloso jamás sea una silueta invisible.
            Lighting.AddLight(NPC.Center, new Vector3(0.55f, 0.40f, 0.70f));

            // v6.50.120 — LOS RELÁMPAGOS DE FONDO (el escenario de jefe
            // final): la tormenta del recuerdo — Moon Lord trae su
            // cielo; el eclipse trae sus relámpagos. Cada ~3-5 s UNA
            // vena dorada cae del techo de la pantalla cerca del jefe,
            // con su trueno grave y su temblor sutil (la pelea vive
            // DENTRO de una tormenta). Es puro teatro LOCAL (polvo +
            // sonido + temblor: nada viaja por el cable — cada máquina
            // arma su propio cielo). La despedida y la muerte callan
            // el firmamento.
            if (!_muriendo && _estado != EST_MURIENDO)
            {
                _tickRelampago--;
                if (_tickRelampago <= 0)
                {
                    _tickRelampago = 160 + Main.rand.Next(140);
                    RelampagoFondo(2f, 6);
                }
            }
        }

        /// <summary>
        /// v6.50.120 — LA VENA DEL RELÁMPAGO (el cielo de la noche del
        /// eclipse): UNA raya de 14 motas doradas cayendo del techo de
        /// la pantalla cerca del jefe (un SOLO x para las catorce — una
        /// vena, no una cortina), el trueno grave (Item122 bemol) y el
        /// temblor sutil. Moon Lord trae su cielo; el eclipse trae sus
        /// relámpagos.
        /// </summary>
        private void RelampagoFondo(float fuerza, int duracion)
        {
            if (Main.dedServ) return;   // el servidor no tiene cielo que partir
            float xVena = NPC.Center.X + Main.rand.NextFloat(-600f, 600f);
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    new Vector2(xVena + Main.rand.NextFloat(-9f, 9f),
                        NPC.Center.Y - 900f + i * 24f),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                        Main.rand.NextFloat(2.5f, 5f)),
                    150, new Color(255, 220, 120), 1.3f);
                d.noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.85f), NPC.Center);
            OndaLib.Kick(fuerza, duracion);
        }

        /// <summary>
        /// EL MOVESET DEL RECUERDO COMPLETO: las armas de la fusión (las
        /// CINCO de siempre + las TRES de jefe final de la v6.50.120),
        /// rotando SIN repetir (ni el último plato ni el de atrás — la
        /// memoria doble de la casa). Máximo UN ataque grande a la vez:
        /// ya es así por estados.
        /// </summary>
        protected override void ElegirAtaque(Player target)
        {
            // v6.50.120 — EL MENÚ QUE CRECE CON LA FASE (la escalada de
            // los finales de Terraria): P1-P2 los CINCO platos de la
            // fusión; P3 (vida <60%) se destapa EL CONCILIO (el ritual
            // del Cultista); P4 (<40%) LA TORMENTA (el sharknado del
            // Duque); P5 (<20%) EL JUICIO (los rayos del Moon Lord) —
            // TODO el menú, la furia final. El Cultista no abre con su
            // ritual ni el Duque con su tormenta: cada arma llega con su
            // furia (la Phase heredada ya lee la vida 80/60/40/20).
            int[] menu = Phase >= 5
                ? new[] { F_ECLIPSE, F_SOLMOR, F_REJILLA, F_PAGINAS, F_ANILLOS, F_RITUAL, F_TORMENTA, F_RAYOS }
                : Phase == 4
                    ? new[] { F_ECLIPSE, F_SOLMOR, F_REJILLA, F_PAGINAS, F_ANILLOS, F_RITUAL, F_TORMENTA }
                    : Phase == 3
                        ? new[] { F_ECLIPSE, F_SOLMOR, F_REJILLA, F_PAGINAS, F_ANILLOS, F_RITUAL }
                        : new[] { F_ECLIPSE, F_SOLMOR, F_REJILLA, F_PAGINAS, F_ANILLOS };
            int elegido = menu[Main.rand.Next(menu.Length)];
            int intentos = 0;
            while ((elegido == _ultimoAtaque || elegido == _anteultimoAtaque) &&
                intentos++ < menu.Length)
                elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            _anteultimoAtaque = _ultimoAtaque;
            _ultimoAtaque = elegido;
            _estado = elegido;
            _tickEstado = 0;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// LOS FONDOS DE FASE DEL RECUERDO COMPLETO: sin decreto y sin
        /// especial de fase (el moveset es SUYO) — pero la furia de
        /// siempre sigue: la cura de fase, el rugido y el anuncio.
        /// </summary>
        protected override void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            // v6.50.120 — EL FLASH DE FASE: cada fase ABRE con trueno
            // (el relámpago síncrono — la noche del eclipse responde a
            // la furia nueva: las mismas motas, el temblor más largo).
            RelampagoFondo(6f, 10);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                PaletaOro, Phase, PhaseName());
            NPC.netUpdate = true;
        }

        /// <summary>EL DESPACHO de los estados propios (los ≥30).</summary>
        protected override bool EstadoFusionado(int estado, Player target)
        {
            switch (estado)
            {
                case F_ECLIPSE: EclipseGemelo(target); return true;
                case F_SOLMOR: SolMoribundo(target); return true;
                case F_REJILLA: RejillaLaser(target); return true;
                case F_PAGINAS: LluviaPaginas(target); return true;
                case F_ANILLOS: AnillosFusion(target); return true;
                case F_ALIENTO: UltimoAliento(target); return true;
                case F_RAYOS: JuicioFantasmal(target); return true;
                case F_RITUAL: ConcilioEspejos(target); return true;
                case F_TORMENTA: TormentaAbismo(target); return true;
            }
            return false;
        }

        // ==================================================================
        //  (a) EL ECLIPSE GEMELO — la media pantalla que ARDE alterna
        // ==================================================================
        private void EclipseGemelo(Player target)
        {
            // EL EJE DEL ECLIPSE: el dios se alza sobre la arena (la
            // izquierda es ORO, la derecha OSCURIDAD — los velos los
            // pinta el PreDraw en cada pantalla).
            // v6.50.121 — EL COLOSO FLOTA VIVO: el reposo respira con
            // una deriva lateral de ±80 px (sin(_tickEstado·0.028)·80)
            // mientras la lluvia trabaja — el gigante VAIVÉA sobre la
            // arena en vez de clavarse como estatua (el eje del velo
            // del PreDraw sigue a posC: la media pantalla que arde
            // respira CON él).
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 80f, -640f);   // v6.50.120 — el coloso reposa más alto
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.35f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Decreto", PaletaOro);
                OndaLib.Kick(8f, 18);
                NPC.netUpdate = true;
            }

            // TRES RONDAS de 100 t: ronda 0 arde la MITAD DORADA
            // (izquierda), ronda 1 la OSCURA (derecha), ronda 2 la dorada
            // de nuevo — la lluvia cae SOLO del lado que arde.
            int ronda = Math.Min(2, _tickEstado / 100);
            bool ardeIzq = (ronda & 1) == 0;

            if (_tickEstado < 300 && (_tickEstado % 5) == 0 && (_tickEstado % 100) >= 15 &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = 0; k < 2; k++)
                {
                    float lado = ardeIzq ? -1f : 1f;
                    float x = NPC.Center.X + lado * Main.rand.NextFloat(80f, 620f);
                    float y = target.Center.Y - Main.rand.NextFloat(760f, 900f);
                    Vector2 v = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(8.5f, 11f));
                    // LA PALETA DEL LADO: izquierda = pernos de LUZ (ai 0),
                    // derecha = pernos de OSCURIDAD (ai 2) — el color que
                    // arde es el color que duele.
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        x, y, v.X, v.Y,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, ardeIzq ? 0f : 2f,
                        NPC.whoAmI * 61 + _tickEstado * 3 + k);
                }
            }

            // v6.50.121 — RECORTE DE CONTEMPLACIÓN: 340→300 — la lluvia
            // muere SOLA con la tercera ronda (su gate es _tickEstado <
            // 300, la última siembra cae a t=295): los 40 t de cola
            // muerta sobran, el eclipse suelta JUSTO al apagarse.
            if (_tickEstado >= 300) CerrarEstado();
        }

        // ==================================================================
        //  (b) EL SOL MORIBUNDO — el gigante lento que ATRAE
        // ==================================================================
        private void SolMoribundo(Player target)
        {
            // EL DIOS OBSERVA desde arriba: el sol negro caza solo.
            // v6.50.121 — EL COLOSO FLOTA VIVO: deriva lateral ±80 px
            // (sin(_tickEstado·0.028)·80) mientras el sol negro caza
            // solo — el gigante OBSERVA respirando, no clavado.
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 80f, -680f);   // v6.50.120 — el coloso reposa más alto
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.25f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Sol", PaletaOro);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            // EL LANZAMIENTO (t=20): el retroceso del cañonazo y el sol
            // negro nace rumbo a la presa — LENTO (6 px/t), GIGANTE (×1.8
            // la talla del sol del dios) y con ~9 s de agonía. La
            // ATRACCIÓN la aplica TraccionRecuerdoPlayer en cada jugador.
            if (_tickEstado == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dano = Math.Max(1, (int)(NPC.damage * 1.05f));
                Vector2 rumbo = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 6f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center.X, NPC.Center.Y, rumbo.X, rumbo.Y,
                    ModContent.ProjectileType<AtaqueFusionProjectile>(),
                    dano, 3f, Main.myPlayer,
                    AtaqueFusionProjectile.EstiloSolMoribundo, dano, NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                NPC.netUpdate = true;
            }
            else if (_tickEstado <= 20 && _tickEstado > 8)
            {
                // EL ARMA CARGA: retrocede un pelo antes del cañonazo.
                Vector2 atras = (NPC.Center - target.Center)
                    .SafeNormalize(Vector2.UnitY) * 1.2f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, atras, 0.08f);
            }

            // El estado suelta ANTES: el sol vive SOLO sus 9 s (el dios
            // sigue peleando mientras el moribundo caza — el hazardo
            // persistente de la casa, como el reloj de la fase 2).
            if (_tickEstado >= 120) CerrarEstado();
        }

        // ==================================================================
        //  (c) LA REJILLA LÁSER BICOLOR — el tablero que alterna
        // ==================================================================
        private void RejillaLaser(Player target)
        {
            // EL DIOS SE CLAVA sobre la presa: la rejilla se teje alrededor.
            // v6.50.121 — EL COLOSO FLOTA VIVO: deriva lateral ±80 px
            // (sin(_tickEstado·0.028)·80) mientras el tablero arde —
            // el dios vela la rejilla VAIVÉANDO de lado a lado.
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 80f, -700f);   // v6.50.120 — el coloso reposa más alto
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(0.15f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Danza", PaletaOro);
                OndaLib.Kick(7f, 16);
                NPC.netUpdate = true;
            }

            // LA SIEMBRA (t=30): SEIS barras alrededor de la presa — tres
            // HORIZONTALES (ai[1] 0-2) y tres VERTICALES (ai[1] 3-5), con
            // el COLOR por paridad del índice (par ORO, impar OSCURIDAD):
            // el tablero queda tejido y el ciclo alterna quién arde.
            if (_tickEstado == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    bool vert = i >= 3;
                    Vector2 pos = vert
                        ? new Vector2(target.Center.X + (i - 4) * 320f, target.Center.Y)
                        : new Vector2(target.Center.X, target.Center.Y + (i - 1) * 200f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos.X, pos.Y, 0f, 0f,
                        ModContent.ProjectileType<AtaqueFusionProjectile>(),
                        (int)(NPC.damage * 0.60f), 0f, Main.myPlayer,
                        AtaqueFusionProjectile.EstiloRejillaLaser, i, NPC.whoAmI * 53 + i);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.3f), NPC.Center);
                NPC.netUpdate = true;
            }

            // La rejilla vive SOLA su reloj (el proyectil se apaga a los
            // 450 t PROPIOS = ~t480 del estado). v6.50.121 — RECORTE DE
            // CONTEMPLACIÓN: 470→390 — la única actividad del jefe aquí
            // es la siembra del t=30; después de eso nada lo ata al
            // tablero: el dios se va a los 390 y la rejilla SIGUE
            // ardiendo sola (el hazardo persistente de la casa, como el
            // sol del moribundo: el tablero es autónomo).
            if (_tickEstado >= 390) CerrarEstado();
        }

        // ==================================================================
        //  (d) LA LLUVIA DE PÁGINAS — la biblioteca se vierte
        // ==================================================================
        private void LluviaPaginas(Player target)
        {
            // EL DIOS SE ALZA y arranca las páginas de tinta.
            // v6.50.121 — EL COLOSO FLOTA VIVO: deriva lateral ±80 px
            // (sin(_tickEstado·0.028)·80) mientras la biblioteca se
            // vierte — el dios VAIVÉA sobre su lluvia de páginas.
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 80f, -660f);   // v6.50.120 — el coloso reposa más alto
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70.WithPitchOffset(-0.3f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Coro", PaletaOro);
                OndaLib.Kick(6f, 14);
                NPC.netUpdate = true;
            }

            // LAS TRES OLEADAS (t=30/90/150): diez páginas de tinta cayendo
            // en abanico sobre la presa — girando, con estela morada,
            // rompiéndose contra el suelo.
            for (int o = 0; o < 3; o++)
            {
                if (_tickEstado == 30 + o * 60 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int p = 0; p < 10; p++)
                    {
                        float x = target.Center.X + (p - 4.5f) * 110f +
                            Main.rand.NextFloat(-40f, 40f);
                        float y = target.Center.Y - 780f - Main.rand.NextFloat(0f, 70f);
                        Vector2 v = new Vector2(Main.rand.NextFloat(-1f, 1f),
                            Main.rand.NextFloat(3f, 5f));
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            x, y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueFusionProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueFusionProjectile.EstiloPaginaTinta, 0f,
                            p + o * 16 + NPC.whoAmI * 7);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item9.WithPitchOffset(-0.2f), NPC.Center);
                    NPC.netUpdate = true;
                }
            }

            // LOS RAYOS DORADOS ENTRE OLEADAS (t=60/120/180): tres pernos
            // de LUZ cosiendo el hueco entre las páginas — la mitad dorada
            // del recuerdo reclama su turno.
            if ((_tickEstado == 60 || _tickEstado == 120 || _tickEstado == 180) &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pred = PredPresa(target);
                for (int r = 0; r < 3; r++)
                {
                    Vector2 pos = NPC.Center + new Vector2(
                        Main.rand.NextFloat(-160f, 160f), Main.rand.NextFloat(-20f, 60f));
                    float ang = (pred - pos).ToRotation() + (r - 1) * 0.13f;
                    Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 12f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos.X, pos.Y, v.X, v.Y,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                        NPC.whoAmI * 97 + _tickEstado + r);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
            }

            if (_tickEstado >= 240) CerrarEstado();
        }

        // ==================================================================
        //  (e) LOS ANILLOS DE FUSIÓN — la nova doble que alterna
        // ==================================================================
        private void AnillosFusion(Player target)
        {
            // LA CONTRACCIÓN: se llena (la nova de siempre).
            // v6.50.121 — EL COLOSO FLOTA VIVO: la nova ya no CLAVA al
            // jefe — la contracción deriva ±50 px de lado a lado: la
            // velocidad horizontal objetivo es LA DERIVADA de
            // sin(_tickEstado·0.028)·50 (cos·1.4 px/t), el mismo compás
            // de respiración de los otros reposos a la amplitud menor
            // de los anillos; la vertical sigue muriendo a cero (la
            // contracción apunta AL NÚCLEO, no a los lados).
            NPC.velocity = Vector2.Lerp(NPC.velocity,
                new Vector2(MathF.Cos(_tickEstado * 0.028f) * 1.4f, 0f), 0.15f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Nova", PaletaOro);
                OndaLib.Kick(6f, 12);
                NPC.netUpdate = true;
            }

            // TRES ANILLOS (t=45/70/95) que ALTERNAN luz y oscuridad —
            // cada uno MÁS RÁPIDO que el anterior (la expansión que se
            // come la arena), con los cuatro huecos de la nova de la casa.
            int[] ticksDet = { 45, 70, 95 };
            for (int a = 0; a < 3; a++)
            {
                if (_tickEstado == ticksDet[a] && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float vel = 6.5f + a * 2.4f;
                    float desfase = a * MathHelper.Pi / 9f;
                    float hueco = 0.20f;
                    float paleta = (a & 1) == 0 ? 0f : 2f;   // anillo 0 y 2: LUZ · 1: OSCURIDAD
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f + desfase;
                        float grad = Math.Abs(MathHelper.WrapAngle(ang - desfase));
                        if (grad < hueco || Math.Abs(grad - MathHelper.PiOver2) < hueco ||
                            Math.Abs(grad - MathHelper.Pi) < hueco ||
                            Math.Abs(grad - MathHelper.Pi * 1.5f) < hueco) continue;
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        Vector2 tan = new Vector2(-dir.Y, dir.X) * (1.1f + a * 0.55f);
                        Vector2 v = dir * vel + tan;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center.X, NPC.Center.Y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.60f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, paleta,
                            NPC.whoAmI * 71 + a * 19 + i);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    OndaLib.Kick(9f, 18);
                    NPC.netUpdate = true;
                }
            }

            if (_tickEstado >= 140) CerrarEstado();
        }

        // ==================================================================
        //  (f) EL ÚLTIMO ALIENTO — la despedida a vida ≤1%
        // ==================================================================
        private void UltimoAliento(Player target)
        {
            // SE DETIENE: ya no ataca, ya no esquiva — solo mira.
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.18f);
            NPC.dontTakeDamage = true;

            // LA BRUMA DE LA DISOLUCIÓN: las dos mitades se deshacen (la
            // separación y los OJOS que te miran los dibuja el PreDraw).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                int lado = Main.rand.NextBool() ? 1 : -1;
                Vector2 silueta = NPC.Center + new Vector2(lado * (120f * EscalaCuerpo), 0f);
                Dust d = Dust.NewDustPerfect(
                    silueta + Main.rand.NextVector2Circular(70f * EscalaCuerpo, 70f * EscalaCuerpo),
                    DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.5f, 1.6f)),
                    150, lado > 0 ? new Color(90, 20, 140) : PaletaOro, 1.2f);
                d.noGravity = true;
            }

            // 3 s (180 t) de despedida… y EL CINE DE MUERTE de siempre
            // (con el botín COMPLETO + la esencia extra).
            if (_tickEstado >= 180)
            {
                IniciarCineMuerte();
            }
        }

        // ==================================================================
        //  (g) EL JUICIO FANTASMAL — F_RAYOS (v6.50.120 · Moon Lord · el
        //      Phantasmal Deathray: rayos gigantes que CARGAN
        //      telegrafiados y luego BARREN en arco — y sus True Eyes
        //      orbitando al dueño)
        // ==================================================================
        private void JuicioFantasmal(Player target)
        {
            // v6.50.121 — EL COLOSO FLOTA VIVO: el juicio ya no se
            // contempla en estatua — deriva lateral ±80 px
            // (sin(_tickEstado·0.028)·80) mientras las ruedas barren.
            // OJO (verificado en AtaqueJefeProjectile): las ruedas NO
            // cabalgan al jefe — sus semillas viajan FUERA del rango
            // del cabalgador (ai[2] = maxNPCs+11+i ≥ 200 y el guard de
            // la base exige ai[2] < maxNPCs), así que quedan ANCLADAS
            // donde nacieron (los flancos del t=20): la deriva del
            // coloso NO las arrastra — siguen barriendo su sitio
            // CRUZADAS mientras el gigante respira encima, como los
            // ojos del Señor de la Luna orbitando a su dueño.
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 80f, -700f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.5f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Rayo", PaletaOro);
                OndaLib.Kick(10f, 22);
                NPC.netUpdate = true;
            }

            // EL LANZAMIENTO (t=20, SOLO la autoridad — el contrato MP de
            // la casa): DOS ruedas de rayos reusando LA PRIMITIVA de la
            // danza solar de la base (AtaqueJefeProjectile.EstiloDanzaSolar
            // — la rueda de SEIS rayos girando que BARRE en arco), una por
            // FLANCO del jefe (±220 px ≈ ±0.28 del cuerpo de 770) para que
            // sus rayos se CRUCEN barriendo sobre la presa. El giro de la
            // danza vive en su propio reloj (0.0125 rad/t) y NO admite
            // sentido por ai[] — dos semillas distintas, el giro natural
            // de la casa. Y las semillas viajan FUERA del rango del
            // cabalgador (≥ Main.maxNPCs): en la base la rueda CABALGA al
            // jefe (ai[2] = su whoAmI) — DOS cabalgando se apilarían en
            // una sola; así cada rueda queda ANCLADA a su flanco.
            if (_tickEstado == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    float lado = i == 0 ? -1f : 1f;
                    Vector2 pos = NPC.Center + new Vector2(lado * 220f, 0f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos.X, pos.Y, 0f, 0f,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.75f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloDanzaSolar,
                        Furia ? 1f : 0f, Main.maxNPCs + 11 + i);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                NPC.netUpdate = true;
            }

            // La danza vive ~555 t por su reloj propio (ruedas nacidas
            // a t=20 → mueren a ~t575 del estado). v6.50.121 — RECORTE
            // DE CONTEMPLACIÓN: 615→570 — el corte a 510 dejaba la
            // danza HUÉRFANA (66 t de ruedas barriendo con el jefe ya
            // en su siguiente ataque: DOS armas grandes a la vez,
            // contra la regla de la casa), así que el estado suelta 5 t
            // antes de que las ruedas mueran solas: cierre LIMPIO de
            // la danza contemplada.
            if (_tickEstado >= 570) CerrarEstado();
        }

        // ==================================================================
        //  (h) EL CONCILIO DE ESPEJOS — F_RITUAL (v6.50.120 · Lunatic
        //      Cultist · el ritual: círculos de proyectiles ANCLADOS
        //      alrededor del jugador que CONVERGEN al punto donde estabas)
        // ==================================================================
        private void ConcilioEspejos(Player target)
        {
            // SE ALZA a −680: el concilio se teje alrededor de la presa.
            Vector2 punto = target.Center + new Vector2(0f, -680f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item70.WithPitchOffset(-0.4f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Ritual", PaletaOro);
                OndaLib.Kick(7f, 16);
                NPC.netUpdate = true;
            }

            // LA PRIMERA SIEMBRA (t=30, autoridad): OCHO pernos en círculo
            // de 430 px alrededor de la presa, cada uno con el rumbo YA
            // CLAVADO al centro — el perno CARGA quieto y a los 55 t VUELA
            // (el dodge legible del Cultista: SALIR del círculo antes del
            // cierre).
            if (_tickEstado == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SembrarRitual(target, 430f, 0);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item4.WithPitchOffset(0.3f), NPC.Center);
                NPC.netUpdate = true;
            }

            // LA SEGUNDA SIEMBRA (t=150, autoridad): el ritual SE REPITE —
            // otro círculo de 8, radio 500 y desfase distinto (al que
            // esquivó el primero leyéndolo, el segundo lo cobra moviéndose).
            if (_tickEstado == 150 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SembrarRitual(target, 500f, 800);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item4.WithPitchOffset(0.3f), NPC.Center);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 300) CerrarEstado();
        }

        /// <summary>v6.50.120 — LA SIEMBRA DEL CONCILIO: OCHO pernos
        /// EstiloRitualConvergente en círculo alrededor de la presa, cada
        /// uno con velocity YA APUNTANDO al centro (donde ESTÁS ahora)
        /// × 11.5 px/t. La PARIDAD del índice viaja en ai[1] (el color del
        /// perno: par ORO, impar OSCURIDAD) y la semilla determinista en
        /// ai[2] (i×13 + whoAmI×7 + lote).</summary>
        private void SembrarRitual(Player target, float radio, int lote)
        {
            float desfase = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 8; i++)
            {
                float ang = desfase + i * MathHelper.TwoPi / 8f;
                Vector2 pos = target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                Vector2 v = (target.Center - pos).SafeNormalize(Vector2.UnitY) * 11.5f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    pos.X, pos.Y, v.X, v.Y,
                    ModContent.ProjectileType<AtaqueFusionProjectile>(),
                    (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                    AtaqueFusionProjectile.EstiloRitualConvergente, i,
                    i * 13 + NPC.whoAmI * 7 + lote);
            }
        }

        // ==================================================================
        //  (i) LA TORMENTA DEL ABISMO — F_TORMENTA (v6.50.120 · Duke
        //      Fishron · el sharknado: vórtices que emergen a los flancos
        //      y avanzan escupiendo proyectiles en espiral — SIN
        //      proyectil nuevo: anclas propias + los pernos de siempre)
        // ==================================================================
        private void TormentaAbismo(Player target)
        {
            // EL JEFE SUBE a −700: desde arriba contempla su tormenta.
            // v6.50.121 — EL COLOSO FLOTA VIVO, deriva MENOR: ±40 px
            // (sin(_tickEstado·0.028)·40) — las anclas acechan por SU
            // cuenta (nacen sobre la presa, no sobre el jefe) y el
            // coloso no debe alejarse del centro del cerco: apenas un
            // vaivén, el gigante CONTENIENDO la tormenta.
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_tickEstado * 0.028f) * 40f, -700f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                // El estado nace LIMPIO (el sembrado y la espiral se
                // re-arma en cada visita — la contabilidad de la casa).
                _tormentaSembrada = false;
                _angTormenta = 0f;
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(0.35f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Tormenta", PaletaOro);
                OndaLib.Kick(9f, 20);
                NPC.netUpdate = true;
            }

            // LA SIEMBRA (t=15, UNA vez): los TRES vórtices nacen a los
            // flancos de la presa (±700 px) y sobre su cabeza (−300) —
            // los sharknados del Duque emergiendo del agua a los costados.
            // (Corre en TODAS las máquinas: las anclas alimentan el teatro
            // local de cada pantalla; las ráfagas sí son de la autoridad.)
            if (_tickEstado == 15 && !_tormentaSembrada)
            {
                _tormentaSembrada = true;
                for (int a = 0; a < 3; a++)
                    _anclasTormenta[a] = new Vector2(
                        target.Center.X + (a - 1) * 700f, target.Center.Y - 300f);
            }

            if (_tormentaSembrada)
            {
                // EL ACECHO: cada ancla AVANZA horizontal hacia la presa
                // (2.2 px/t) pero el cerco JAMÁS cierra a <600 px — la
                // tormenta ACECHA, no envuelve (el tiburón no se traga la
                // arena). La del CENTRO es el OJO: nació bajo la presa y
                // la sigue suave; si la presa corre HACIA un vórtice, ese
                // no retrocede — la tormenta no persigue de cerca.
                for (int a = 0; a < 3; a++)
                {
                    float dx = _anclasTormenta[a].X - target.Center.X;
                    if (a == 1)
                        _anclasTormenta[a].X += MathHelper.Clamp(-dx, -2.2f, 2.2f);
                    else if (dx > 600f)
                        _anclasTormenta[a].X = Math.Max(target.Center.X + 600f,
                            _anclasTormenta[a].X - 2.2f);
                    else if (dx < -600f)
                        _anclasTormenta[a].X = Math.Min(target.Center.X - 600f,
                            _anclasTormenta[a].X + 2.2f);
                }

                // EL VÓRTICE (el teatro del tiburón): 2 motas por ancla en
                // órbita espiral — la del CENTRO ORO, las de afuera violeta
                // (la paridad de la casa).
                if (!Main.dedServ)
                {
                    for (int a = 0; a < 3; a++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            Vector2 off = Main.rand.NextVector2Circular(46f, 46f);
                            Dust d = Dust.NewDustPerfect(_anclasTormenta[a] + off,
                                DustID.PurpleTorch,
                                new Vector2(-off.Y, off.X) * (1.4f / 46f), 150,
                                a == 1 ? PaletaOro : new Color(90, 20, 140), 1.1f);
                            d.noGravity = true;
                        }
                    }
                }

                // LAS RÁFAGAS (cada 14 t, SOLO la autoridad): UN perno por
                // ancla, todos con el MISMO ángulo que gira 0.62 rad por
                // ráfaga — la espiral que gira del sharknado. La primera
                // llega a los 29 t: un compás para LEER los vórtices antes
                // de que escupan (la regla de legibilidad de la casa). El
                // perno del OJO es de LUZ (ai 0), los de afuera de
                // OSCURIDAD (ai 2); semilla determinista anclaIdx×29+tick.
                if (_tickEstado > 15 && ((_tickEstado - 15) % 14) == 0 &&
                    Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int a = 0; a < 3; a++)
                    {
                        Vector2 v = new Vector2(MathF.Cos(_angTormenta), MathF.Sin(_angTormenta)) * 7.5f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            _anclasTormenta[a].X, _anclasTormenta[a].Y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar,
                            a == 1 ? 0f : 2f, a * 29 + _tickEstado);
                    }
                    _angTormenta += 0.62f;
                    NPC.netUpdate = true;
                }
            }

            if (_tickEstado >= 340) CerrarEstado();
        }

        /// <summary>
        /// EL BOTÍN DEL RECUERDO COMPLETO: TODO el de siempre (esencia,
        /// la Forma Ascendida, el Aethon Menor, los 250 fragmentos)…
        /// y UNA ESENCIA DE AETHON EXTRA — la compensación por los dos
        /// fragmentos que se fusionaron sin dropear NADA. v6.50.118 —
        /// LA CRÍA GEMELA GARANTIZADA (la letra: «asegúrate de que el
        /// jugador pueda conseguir las mascotas»): el Aethon Menor ya
        /// NO es un 20% de azar ni un regalo de pruebas — ESTA es su
        /// vía canónica: el recuerdo recompuesto suelta a su cría, y el
        /// mismo ítem engendra a la PAREJA (la de luz y su gemela
        /// oscura — un solo ítem, las dos mascotas).
        /// v6.50.119 — la cría ahora viaja EN SU BOLSA (la letra:
        /// «crea una bolsa donde esten las mascotas, las 2»): el
        /// recuerdo suelta LA BOLSA DE LA CRÍA, y la bolsa despierta
        /// a la pareja al abrirse.
        /// </summary>
        protected override void DropBotin()
        {
            // v6.51.1 — EL BOTÍN DE LA LETRA: «cuando muere Aethon su
            // loot debe ser su mascota de luz, 20 esencia corruptas y 10
            // platino, al menos por ahora» — SOLO eso (el botín viejo —
            // la esencia de Aethon, la Forma Ascendida, los 250
            // fragmentos, la bolsa de la cría — se retira con el override
            // completo: la carta nueva manda).
            if (_yaDropeo) return;
            _yaDropeo = true;
            var src = NPC.GetSource_Loot();
            Item.NewItem(src, NPC.Center,
                ModContent.ItemType<Items.Llamados.AethonMenorItem>(), 1);
            Item.NewItem(src, NPC.Center,
                ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 20);
            Item.NewItem(src, NPC.Center, ItemID.PlatinumCoin, 10);
        }

        // ==================================================================
        //  EL ARTE — EL ECLIPSE (mitad dorada, mitad negro-morado)
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // EL SOL DORADO DE LA BASE (intacto — la mitad de luz del
            // recuerdo; su propio PreDraw abre y cierra sus lotes).
            base.PreDraw(spriteBatch, screenPos, drawColor);

            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 posC = NPC.Center - Main.screenPosition;
                float visibilidad = 1f - (NPC.alpha / 255f);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // === 1. EL DISCO DE LA OSCURIDAD: la mitad oscura del
                    //     recuerdo TAPA el sol dorado — deslizándose lenta
                    //     alrededor del núcleo: UN ECLIPSE VIVO ===
                    if (!_muriendo)
                    {
                        // v6.50.119 — el disco creció con el cuerpo (×2.5):
                        // la mitad oscura TAPA al sol gigante — un eclipse
                        // a la escala del recuerdo completo.
                        Vector2 offD = new Vector2(MathF.Cos(t * 0.35f), MathF.Sin(t * 0.35f)) * (46f * EscalaCuerpo);
                        LumenLib.Bloom(spriteBatch, posC + offD, 150f * EscalaCuerpo,
                            new Color(45, 12, 70), 0.85f * visibilidad, 3);
                        LumenLib.Bloom(spriteBatch, posC + offD, 92f * EscalaCuerpo,
                            new Color(90, 20, 140), 0.90f * visibilidad, 2);
                        // EL RIM DEL ECLIPSE (el borde morado que lo recorta).
                        Vector2 ringTam = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
                        spriteBatch.Draw(VFXCore.Ring, posC + offD, null,
                            new Color(140, 60, 210) * (0.35f * visibilidad), 0f,
                            ringTam * 0.5f, VFXCore.RingQuadSize(96f * EscalaCuerpo) / ringTam,
                            SpriteEffects.None, 0f);
                    }

                    // === 2. EL VELO DEL ECLIPSE GEMELO: la media pantalla
                    //     ACTIVA arde, la inactiva se dibuja TENUE (el
                    //     aviso legible — el lado tranquilo es donde se
                    //     vive) ===
                    if (_estado == F_ECLIPSE)
                    {
                        int ronda = Math.Min(2, _tickEstado / 100);
                        bool ardeIzq = (ronda & 1) == 0;
                        float eje = posC.X;
                        float anchoV = Math.Max(560f, Main.screenWidth * 0.55f);
                        float altoV = Math.Max(700f, Main.screenHeight * 1.05f);
                        Vector2 origenV = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;

                        // EL LADO DORADO (izquierda).
                        float aOro = (ardeIzq ? 0.26f : 0.09f) +
                            (ardeIzq ? 0.05f : 0f) * MathF.Sin(t * 5f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje - anchoV * 0.5f, Main.screenHeight * 0.5f), null,
                            new Color(255, 220, 120) * (aOro * visibilidad), 0f, origenV,
                            new Vector2(anchoV / VFXCore.SoftGlow.Width,
                                        altoV / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);

                        // EL LADO OSCURO (derecha).
                        float aOsc = (!ardeIzq ? 0.26f : 0.09f) +
                            (!ardeIzq ? 0.05f : 0f) * MathF.Sin(t * 5f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje + anchoV * 0.5f, Main.screenHeight * 0.5f), null,
                            new Color(90, 20, 140) * (aOsc * visibilidad), 0f, origenV,
                            new Vector2(anchoV / VFXCore.SoftGlow.Width,
                                        altoV / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);

                        // EL EJE (la línea que parte la pantalla en dos —
                        // vertical, fina, blanca-caliente: la frontera).
                        Vector2 origenE = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        float altoE = Math.Max(1200f, Main.screenHeight * 1.6f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje, Main.screenHeight * 0.5f), null,
                            new Color(255, 240, 210) * (0.5f * visibilidad), 0f, origenE,
                            new Vector2(7f / VFXCore.SoftGlow.Width,
                                        altoE / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);
                    }

                    // === 3. EL ÚLTIMO ALIENTO: las DOS SILUETAS — la de
                    //     luz y la de oscuridad, a ±120·EscalaCuerpo px
                    //     (v6.50.119: la despedida creció con el cuerpo),
                    //     AMBAS MIRANDO al jugador (el núcleo-ojo apunta)
                    //     — deshaciéndose en bruma a medida que los 3 s se
                    //     consumen ===
                    if (_estado == F_ALIENTO)
                    {
                        Player presa = Main.player[NPC.target];
                        Vector2 mira = presa != null && presa.active && !presa.dead
                            ? presa.Center : NPC.Center + Vector2.UnitX * 300f;
                        float disol = Math.Min(1f, _tickEstado / 180f);
                        float alfa = (1f - disol) * visibilidad;

                        for (int lado = -1; lado <= 1; lado += 2)
                        {
                            Vector2 silueta = posC + new Vector2(lado * (120f * EscalaCuerpo), 0f);
                            Color cSil = lado < 0 ? PaletaOro : new Color(90, 20, 140);

                            // EL CUERPO TENUE del fragmento (a la escala nueva).
                            LumenLib.Bloom(spriteBatch, silueta, 84f * EscalaCuerpo, cSil, 0.40f * alfa, 3);
                            LumenLib.Bloom(spriteBatch, silueta, 42f * EscalaCuerpo, cSil, 0.55f * alfa, 2);

                            // EL NÚCLEO-OJO QUE APUNTA: la pupila desplazada
                            // hacia la presa — las dos mitades MIRANDO al
                            // que las está matando.
                            Vector2 hacia = (mira - (silueta + Main.screenPosition))
                                .SafeNormalize(Vector2.UnitY) * (13f * EscalaCuerpo);
                            LumenLib.Bloom(spriteBatch, silueta + hacia, 20f * EscalaCuerpo,
                                new Color(255, 250, 240), 0.85f * alfa, 3);

                            // LA BRUMA QUE SE DESHACE (las volutas subiendo).
                            for (int v = 0; v < 3; v++)
                            {
                                float fV = (t * 0.8f + v * 0.33f + (lado + 1) * 0.5f) % 1f;
                                Vector2 voluta = silueta + new Vector2(
                                    MathF.Sin(fV * 6f + v * 2f) * (26f * EscalaCuerpo),
                                    -20f - fV * (90f * EscalaCuerpo));
                                LumenLib.Bloom(spriteBatch, voluta, 26f * EscalaCuerpo * (0.4f + fV),
                                    cSil, 0.25f * alfa * (1f - fV), 2);
                            }
                        }
                    }
                }
                finally { spriteBatch.End(); }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // El estado de salida de PreDraw es SIEMPRE el lote de
                // vanilla (el contrato de la casa).
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.ReabrirLoteVanilla();
            }
            return false;   // el eclipse se dibuja a sí mismo
        }
    }
}
