using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.38 — LA CAMADA DE LAS SIERPES (10 formas nuevas + la cría).
    //
    //  Cada proyectil de este archivo es una criatura segmentada con SU
    //  PROPIA forma de mover la cadena — nacida de un patrón distinto del
    //  informe de locomoción (research/sierpes_v638/INFORME_LOCOMOCION.md:
    //  14 patrones investigados en la web). LA SIERPE ESTELAR ORIGINAL
    //  (SierpeEstelarProjectile) NO SE TOCA: sigue siendo el ADN común.
    //
    //  La CARNE (el render) vive en SierpesLib; la FÍSICA (la simulación)
    //  vive aquí. Contratos de la casa: ai[0..1] = puntero/presa sincro-
    //  nizado (patrón congregación — el dueño local manda), ai[2] = semi-
    //  lla, SIN hide (la lección v6.33), cero Main.rand en render.
    // ======================================================================

    /// <summary>
    /// OuroborosAstralProjectile — EL OUROBOROS ASTRAL (v6.38 — PATRÓN 2).
    ///
    /// LA PERSECUCIÓN CÍCLICA (el mice problem): doce cuentas donde CADA
    /// UNA persigue a la SIGUIENTE apuntando ADELANTE (presa + velocidad·k
    /// — el adelanto que mantiene el anillo estable). Nadie manda: la
    /// causalidad es CIRCULAR — no hay cabeza ni cola, solo el anillo que
    /// gira alrededor del portador.
    ///
    /// MECÁNICA: el anillo vive 10 s barriendo lo que toca (×0.55 cada 10
    /// ticks por cuenta). Al morir, el k desaparece: las cuentas se
    /// persiguen A PELO y el mice problem hace lo suyo — la espiral
    /// logarítmica que colapsa al centro en 60 ticks y DETONA (×2.0 en
    /// 100 px).
    /// </summary>
    public class OuroborosAstralProjectile : ModProjectile
    {
        /// <summary>El N del anillo: doce cuentas.</summary>
        private const int N = 12;

        private const int Vida = 600;

        /// <summary>La distancia entre cuentas (el this.size de la cadena).</summary>
        private const float Tamano = 22f;

        /// <summary>La velocidad de persecución (px/tick).</summary>
        private const float Vel = 7f;

        /// <summary>El radio del anillo alrededor del portador.</summary>
        private const float Radio = 130f;

        /// <summary>La rotación del anillo (rad/tick — la deriva de la ranura).</summary>
        private const float Omega = 0.025f;

        /// <summary>Cadencia de la quemadura de las cuentas.</summary>
        private const int CadaCuenta = 10;

        /// <summary>Daño relativo de cada cuenta.</summary>
        private const float DañoCuenta = 0.55f;

        /// <summary>Daño de la DETONACIÓN del colapso.</summary>
        private const float DañoColapso = 2.0f;

        /// <summary>Radio de la detonación del colapso.</summary>
        private const float RadioColapso = 100f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide (la lección de los desgarros).
        }

        public override void OnSpawn(IEntitySource source)
        {
            Sembrar(desdeOnSpawn: true);
        }

        /// <summary>v6.50.3 — FIX (hallazgo V-1, la autocuración de la casa):
        /// netImportant corre la IA en los remotos pero OnSpawn NO viaja en
        /// el msg 27 — el anillo nacía vírgen (ceros) y la IA lo arrastraba a
        /// la esquina del mundo (Center=_segs[0]≈(0,0) + rubber-band contra
        /// el sync — el mismo diagnóstico de las órbitas de v6.50.2). Estado
        /// vírgen → se siembra alrededor de la posición SINCRONIZADA (el
        /// patrón de AtaqueJefe/AtaqueOleada con _centroOrbita).</summary>
        private void Sembrar(bool desdeOnSpawn)
        {
            Seed = (int)Projectile.ai[2] % 9973;

            // El anillo nace alrededor del dueño (nacimiento local) o de la
            // posición SINCRONIZADA (autocura de un remoto — la que trae el
            // msg 27).
            Vector2 centro;
            if (desdeOnSpawn)
            {
                Player duenio = Main.player[Projectile.owner];
                centro = duenio != null && duenio.active
                    ? duenio.Center : Projectile.Center;
            }
            else
            {
                centro = Projectile.Center;
            }
            for (int i = 0; i < N; i++)
            {
                float a = i * MathHelper.TwoPi / N;
                _segs[i] = centro + new Vector2(MathF.Cos(a), MathF.Sin(a)) * Radio;
                _vels[i] = new Vector2(MathF.Cos(a + MathHelper.PiOver2),
                    MathF.Sin(a + MathHelper.PiOver2)) * Vel;
            }
            Projectile.Center = centro;
            _sembrado = true;
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!_sembrado) Sembrar(desdeOnSpawn: false); // v6.50.3 — autocura del remoto

            float time = Main.GlobalTimeWrappedHourly;

            // === EL CENTRO (el puntero del anillo — patrón congregación):
            //     entre el jugador y el cursor, con correa de 600 px. ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 alCursor = Main.MouseWorld - duenio.Center;
                if (alCursor.Length() > 600f) alCursor = Vector2.Normalize(alCursor) * 600f;
                Vector2 centro = duenio.Center + alCursor * 0.5f;
                if (Vector2.DistanceSquared(centro,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 20f * 20f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = centro.X;
                    Projectile.ai[1] = centro.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 centroP = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === EL COLAPSO (el mice problem): en los últimos 60 ticks el
            //     adelanto k desaparece — cada cuenta persigue a la
            //     siguiente A PELO y la espiral logarítmica se cierra. ===
            float colapso = MathHelper.Clamp((Vida - Projectile.timeLeft) / 60f, 0f, 1f);
            bool muriendo = Projectile.timeLeft <= 60;

            // === LA PERSECUCIÓN CÍCLICA: cada cuenta persigue a la
            //     siguiente (¡el anillo se cierra — nadie manda!) ===
            _slotAng += Omega;
            for (int i = 0; i < N; i++)
            {
                int j = (i + 1) % N;

                // EL OBJETIVO: la siguiente cuenta con ADELANTO (presa +
                // vel·k — el adelanto que mantiene el radio estable).
                Vector2 objetivo = _segs[j];
                if (!muriendo) objetivo += _vels[j] * 2.5f;

                Vector2 deseada = objetivo - _segs[i];
                float len = deseada.Length();
                if (len > 0.0001f) deseada /= len; else deseada = Vector2.UnitX;
                Vector2 velIdeal = deseada * Vel;

                // LA RANURA: cada cuenta es atraída suavemente a su lugar
                // del anillo (la deriva que hace GIRAR al conjunto).
                if (!muriendo)
                {
                    float a = _slotAng + i * MathHelper.TwoPi / N;
                    Vector2 ideal = centroP + new Vector2(MathF.Cos(a), MathF.Sin(a)) * Radio;
                    velIdeal += (ideal - _segs[i]) * 0.055f;
                }

                _vels[i] = Vector2.Lerp(_vels[i], velIdeal, 0.22f);
                _segs[i] += _vels[i];
            }

            // El hitbox viaja con la cuenta 0 (la boca del ouroboros).
            Projectile.Center = _segs[0];
            Projectile.velocity = _vels[0];
            Projectile.rotation = _vels[0].ToRotation();

            // La luz cálida del anillo.
            Lighting.AddLight(Projectile.Center, 0.20f, 0.16f, 0.08f);

            // === LAS CUENTAS QUEMAN (cada 10 ticks — una cuenta por
            //     enemigo por cadencia, como la danza). ===
            if (_age > 8 && _age % CadaCuenta == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoCuenta));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 0; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _segs[i]) > 15f * 15f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                        break;
                    }
                }
            }

            // === LA DETONACIÓN DEL COLAPSO: la espiral se cierra y el
            //     anillo DETONA en el centro (×2.0 en 100 px). v6.50 —
            //     GolpeMotor: el golpe corre en el cliente dueño; el
            //     sonido y el trauma siguen en la autoridad. ===
            if (Projectile.timeLeft == 2)
            {
                Vector2 centro = Centro();
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoColapso));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, centro) > RadioColapso * RadioColapso)
                        continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2f, true);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AudioLib.Sonar(AudioLib.Familia.Vacia, "muerte", centro, 1f, -0.2f);
                    PulsoLib.Trauma(0.30f);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // El estallido del colapso (visual — el daño ya corrió en el AI).
            Vector2 centro = Centro();
            for (int i = 0; i < 18; i++)
            {
                Dust d = Dust.NewDustPerfect(centro, DustID.GoldFlame,
                    new Vector2(MathF.Cos(i * MathHelper.TwoPi / 18),
                        MathF.Sin(i * MathHelper.TwoPi / 18)) * 3.2f,
                    200, new Color(255, 208, 120), 0.4f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                float colapso = MathHelper.Clamp((Vida - Projectile.timeLeft) / 60f, 0f, 1f);
                SierpesLib.OuroborosAstral(_segs, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha, colapso);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private Vector2 Centro()
        {
            Vector2 suma = Vector2.Zero;
            for (int i = 0; i < N; i++) suma += _segs[i];
            return suma / N;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private readonly Vector2[] _vels = new Vector2[N];
        private bool _sembrado;   // v6.50.3 — la autocuración MP
        private float _slotAng;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// CaravanaEspectralProjectile — LA CARAVANA ESPECTRAL (v6.38 — PATRÓN 3).
    ///
    /// EL CAMINO-MEMORIA (el snake de cola de posiciones): el cuerpo NO
    /// reacciona a su padre — lee el HISTÓRICO del camino de la cabeza y
    /// muestrea el punto a arclength i·Tamano DETRÁS de ella. El cuerpo
    /// recorre la trayectoria EXACTA (nada de recortar esquinas): donde
    /// el farol pasó, la caravana permanece.
    ///
    /// MECÁNICA: el farol abre camino 9 s nadando hacia el cursor y
    /// cazando (cada 45 ticks fija presa dentro de 420 px); el rastro
    /// exacto LIMPIA el pasillo (×0.35 cada 12 ticks por eslabón) — con
    /// giros rápidos el farol DIBUJA muros en S de luz persistente.
    /// </summary>
    public class CaravanaEspectralProjectile : ModProjectile
    {
        /// <summary>El N de la caravana: dieciséis eslabones.</summary>
        private const int N = 16;

        private const int Vida = 540;

        /// <summary>La distancia entre eslabones (px de arclength).</summary>
        private const float Tamano = 18f;

        /// <summary>La velocidad del faro (px/tick).</summary>
        private const float Vel = 13f;

        /// <summary>El radio del nado alrededor del puntero.</summary>
        private const float Radm = 130f;

        /// <summary>La memoria del camino (ticks de historia).</summary>
        private const int Historia = 80;

        /// <summary>Cadencia de la limpieza del pasillo.</summary>
        private const int CadaRastro = 12;

        /// <summary>Daño relativo del rastro.</summary>
        private const float DañoRastro = 0.35f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation() : 0f;

            // Los eslabones nacen apilados tras el faro (la historia los
            // desplegará en los primeros ticks).
            Vector2 atras = new Vector2(MathF.Cos(_rumbo + MathHelper.Pi),
                MathF.Sin(_rumbo + MathHelper.Pi));
            for (int i = 0; i < N; i++)
            {
                _segs[i] = Projectile.Center + atras * (i * 2f);
                _ruta.Add(Projectile.Center);
            }
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL PUNTERO (patrón congregación): el faro nada alrededor
            //     del cursor, con correa de 760 px. ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 puntero = Main.MouseWorld;
                Vector2 alDueño = puntero - duenio.Center;
                if (alDueño.Length() > 760f)
                    puntero = duenio.Center + Vector2.Normalize(alDueño) * 760f;

                if (Vector2.DistanceSquared(puntero,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 24f * 24f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = puntero.X;
                    Projectile.ai[1] = puntero.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 objetivo = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === LA CAZA: cada 45 ticks el faro fija la presa más cercana
            //     a su alrededor (420 px) y la persigue directamente. ===
            if (_age % 45 == 0)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 420f * 420f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                _presa = mejor;
            }
            if (_presa != null && (!_presa.active || !_presa.CanBeChasedBy()))
                _presa = null;

            // === EL NADO DEL FARO: círculo alrededor del puntero (el
            //     punto tangente, como la sierpe) o embestida a la presa. ===
            float deseado;
            if (_presa != null)
                deseado = (_presa.Center - Projectile.Center).ToRotation();
            else
            {
                Vector2 alPuntero = Projectile.Center - objetivo;
                float distP = alPuntero.Length();
                if (distP > Radm * 2.2f && distP > 1f)
                    deseado = (objetivo - Projectile.Center).ToRotation();
                else if (distP > 1f)
                    deseado = alPuntero.ToRotation() + MathHelper.PiOver2;
                else
                    deseado = _rumbo;
            }
            float delta = MathHelper.WrapAngle(deseado - _rumbo);
            _rumbo += MathHelper.Clamp(delta, -0.09f, 0.09f);

            // El vaivén del faro (el tren respira al avanzar).
            float surge = 1f + 0.25f * MathF.Sin(time * MathHelper.TwoPi * 1.1f + Seed);
            Vector2 vel = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) * (Vel * surge);
            Projectile.Center += vel;
            Projectile.velocity = vel;
            Projectile.rotation = _rumbo;

            // === LA MEMORIA: el camino del faro se apunta (la cola del
            //     histórico — solo lo que el cuerpo aún necesita). ===
            _ruta.Add(Projectile.Center);
            // v6.50.3 — FIX (O(n²) → O(n)): el while con RemoveAt(0) corría
            // DESPLAZANDO la lista ENTERA por cada elemento sobrante (n
            // elementos muertos = n desplazamientos de n). RemoveRange: UN
            // solo desplazamiento.
            int excesoRuta = _ruta.Count - System.Math.Max(Historia + 2, N);
            if (excesoRuta > 0)
                _ruta.RemoveRange(0, excesoRuta);

            // === EL CUERPO: cada eslabón muestrea el camino a arclength
            //     i·Tamano DETRÁS del faro — la trayectoria EXACTA. ===
            _segs[0] = Projectile.Center;
            for (int i = 1; i < N; i++)
            {
                float falta = i * Tamano;
                Vector2 p = _ruta[_ruta.Count - 1];
                for (int j = _ruta.Count - 1; j > 0 && falta > 0f; j--)
                {
                    Vector2 paso = _ruta[j - 1] - _ruta[j];
                    float L = paso.Length();
                    if (L >= falta && L > 0.0001f)
                    {
                        p = _ruta[j] + paso * (falta / L);
                        falta = 0f;
                    }
                    else
                    {
                        p = _ruta[j - 1];
                        falta -= L;
                    }
                }
                _segs[i] = p;
            }

            // La luz fría del farol.
            Lighting.AddLight(Projectile.Center, 0.16f, 0.22f, 0.18f);

            // === EL RASTRO LIMPIA EL PASILLO (cada 12 ticks). ===
            if (_age > 12 && _age % CadaRastro == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoRastro));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 1; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _segs[i]) > 14f * 14f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.2f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                SierpesLib.CaravanaEspectral(_segs, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private readonly List<Vector2> _ruta = new List<Vector2>(Historia + 4);
        private NPC _presa;
        private float _rumbo;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// AnguilaSolarProjectile — LA ANGUILA SOLAR (v6.38 — PATRÓN 5).
    ///
    /// LA ONDA VIAJERA DE LA ANGUILA (anguilliforme): el cuerpo NO tiene
    /// dinámica de cadena — ES una fórmula: cada segmento está en cabeza
    /// − dir·s + normal·A(s)·sin(2π(s/λ − f·t)) con envolvente A(s)
    /// creciente hacia la cola y λ ≈ el largo del cuerpo (la relación
    /// documentada de la anguila real). La onda viaja hacia atrás y el
    /// pez parece empujarse del agua.
    ///
    /// MECÁNICA: la cabeza nada en círculos alrededor del cursor (la
    /// misma ley de la sierpe) 8 s; el daño enseña a rozar con la
    /// CRESTA: los segmentos con |lateral| máximo golpean ×1.0, los
    /// valles solo ×0.30 (cada 12 ticks).
    /// </summary>
    public class AnguilaSolarProjectile : ModProjectile
    {
        /// <summary>El N de la anguila: dieciséis segmentos.</summary>
        private const int N = 16;

        private const int Vida = 480;

        /// <summary>El paso entre segmentos (px de arclength).</summary>
        private const float Tamano = 13f;

        /// <summary>La velocidad del nado (px/tick).</summary>
        private const float Vel = 12f;

        /// <summary>El radio del nado alrededor del puntero.</summary>
        private const float Radm = 100f;

        /// <summary>λ: la longitud de onda ≈ el largo del cuerpo.</summary>
        private static readonly float Lambda = (N - 1) * Tamano * 0.95f;

        /// <summary>La frecuencia de la onda (Hz — 1 a 2 Hz documentado).</summary>
        private const float Frecuencia = 1.7f;

        /// <summary>La amplitud en la cabeza (px).</summary>
        private const float A0 = 6f;

        /// <summary>La amplitud en la cola (px — envolvente creciente).</summary>
        private const float A1 = 26f;

        /// <summary>Cadencia de la mordedura.</summary>
        private const int CadaMordida = 12;

        /// <summary>Daño relativo de la CRESTA.</summary>
        private const float DañoCresta = 1.0f;

        /// <summary>Daño relativo del valle.</summary>
        private const float DañoValle = 0.30f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation() : 0f;
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL PUNTERO (patrón congregación — correa 760 px). ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 puntero = Main.MouseWorld;
                Vector2 alDueño = puntero - duenio.Center;
                if (alDueño.Length() > 760f)
                    puntero = duenio.Center + Vector2.Normalize(alDueño) * 760f;

                if (Vector2.DistanceSquared(puntero,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 24f * 24f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = puntero.X;
                    Projectile.ai[1] = puntero.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 objetivo = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === EL NADO DE LA CABEZA: el punto tangente del círculo
            //     alrededor del puntero (la misma ley de la sierpe). ===
            Vector2 alPuntero = Projectile.Center - objetivo;
            float distP = alPuntero.Length();
            float deseado;
            if (distP > Radm * 2.4f && distP > 1f)
                deseado = (objetivo - Projectile.Center).ToRotation();
            else if (distP > 1f)
                deseado = alPuntero.ToRotation() + MathHelper.PiOver2;
            else
                deseado = _rumbo;

            float delta = MathHelper.WrapAngle(deseado - _rumbo);
            _rumbo += MathHelper.Clamp(delta, -0.07f, 0.07f);

            // El vaivén de la anguila (más suave que el de la sierpe).
            float surge = 1f + 0.22f * MathF.Sin(time * MathHelper.TwoPi * 1.3f + Seed);
            Vector2 dir = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo));
            Projectile.velocity = dir * (Vel * surge);
            Projectile.Center += Projectile.velocity;
            Projectile.rotation = _rumbo;

            // === EL CUERPO ES UNA FÓRMULA (cero dinámica de cadena): la
            //     envolvente creciente y la onda viajera hacia atrás. ===
            float L = (N - 1) * Tamano;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float faseOnda = MathHelper.TwoPi * (Frecuencia * time);
            for (int i = 1; i < N; i++)
            {
                float s = i * Tamano;
                float A = MathHelper.Lerp(A0, A1, s / L);
                float arg = MathHelper.TwoPi * (s / Lambda) - faseOnda;
                float lat = A * MathF.Sin(arg);
                _segs[i] = Projectile.Center - dir * s + normal * lat;
                _crestas[i] = MathF.Abs(MathF.Sin(arg)) > 0.85f;
            }
            _segs[0] = Projectile.Center;
            _crestas[0] = false;

            // La luz amarillo-solar.
            Lighting.AddLight(Projectile.Center, 0.22f, 0.20f, 0.06f);

            // === LA CRESTA MUERDE (cada 12 ticks): crestas ×1.0, valles
            //     ×0.30 — el jugador aprende a rozar con la cresta. ===
            if (_age > 10 && _age % CadaMordida == 0)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 1; i < N; i++)
                    {
                        bool cresta = _crestas[i];
                        float r = cresta ? 13f : 10f;
                        if (Vector2.DistanceSquared(npc.Center, _segs[i]) > r * r)
                            continue;
                        int dmg = Math.Max(1, (int)(Projectile.damage *
                            (cresta ? DañoCresta : DañoValle)));
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.4f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                SierpesLib.AnguilaSolar(_segs, _crestas, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private readonly bool[] _crestas = new bool[N];
        private float _rumbo;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// CienpiesRunicoProjectile — EL CIEMPIÉS RÚNICO (v6.38 — PATRÓN 6).
    ///
    /// LA MARCHA METACRONAL: el cuerpo camina pegado al SUELO (la cadena
    /// follow con gravedad y suelo propio — cada segmento sondea su
    /// terreno) y cada placa lleva una PATA con fase φ_i = φ₀ − i·π/4 —
    /// la onda de patas que recorre el cuerpo de atrás hacia delante
    /// (marcha retrograda, como el ciempiés real: patas a 8 segmentos de
    /// distancia van sincronizadas). La TRANSICIÓN DE MARCHA es real: la
    /// frecuencia sube cuando el objetivo está lejos (0.9 → 1.8 Hz).
    ///
    /// MECÁNICA: las patas PLANTADAS (media onda apoyada) dejan la RUNA
    /// en el suelo — la runa pica (×0.40 cada 12 ticks, r 14). La cabeza
    /// embiste por contacto. 10 s de marcha.
    /// </summary>
    public class CienpiesRunicoProjectile : ModProjectile
    {
        /// <summary>El N del ciempiés: doce placas.</summary>
        private const int N = 12;

        private const int Vida = 600;

        /// <summary>La distancia entre placas.</summary>
        private const float Tamano = 20f;

        /// <summary>La velocidad de marcha (px/tick).</summary>
        private const float Vel = 8f;

        /// <summary>La altura de vuelo sobre el suelo (px).</summary>
        private const float Hover = 26f;

        /// <summary>El desfase entre patas (π/4 = 45° — patas gemelas cada 8).</summary>
        private const float DeltaPhi = MathHelper.PiOver4;

        /// <summary>Cadencia de la picadura de las runas.</summary>
        private const int CadaRuna = 12;

        /// <summary>Daño relativo de la runa plantada.</summary>
        private const float DañoRuna = 0.40f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;

            // El ciempiés nace avanzando hacia el cursor (la X manda).
            Vector2 atras = new Vector2(
                -MathF.Sign(Projectile.velocity.X < 0f ? -1f : 1f), 0f);
            for (int i = 0; i < N; i++)
                _segs[i] = Projectile.Center + atras * (i * Tamano * 0.5f);
            _sueloY = SueloBajo(Projectile.Center);
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL OBJETIVO (patrón congregación): el enemigo más
            //     cercano al puntero si lo hay — si no, el puntero. ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 objetivo = Main.MouseWorld;
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, objetivo);
                    if (d < 500f * 500f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor != null) objetivo = mejor.Center;

                Vector2 alDueño = objetivo - duenio.Center;
                if (alDueño.Length() > 700f)
                    objetivo = duenio.Center + Vector2.Normalize(alDueño) * 700f;

                if (Vector2.DistanceSquared(objetivo,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 24f * 24f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = objetivo.X;
                    Projectile.ai[1] = objetivo.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 objetivoP = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === LA CABEZA CAMINA: la X persigue el objetivo con inercia
            ///    suave; la Y sondea el suelo y lo cabalga. ===
            float dirX = MathF.Sign(objetivoP.X - Projectile.Center.X);
            if (MathF.Abs(objetivoP.X - Projectile.Center.X) < 12f) dirX = 0f;
            _velX = MathHelper.Lerp(_velX, dirX * Vel, 0.12f);
            Projectile.Center += new Vector2(_velX, 0f);

            _sueloY = SueloBajo(Projectile.Center);
            float yDeseada = (_sueloY ?? Projectile.Center.Y) - Hover;
            Projectile.position.Y = MathHelper.Lerp(Projectile.position.Y,
                yDeseada - Projectile.height * 0.5f, 0.20f);

            // LA TRANSICIÓN DE MARCHA (documentada): lejos → frecuencia
            // ALTA (1.8 Hz); cerca → reposo (0.9 Hz).
            float distObjetivo = MathF.Abs(objetivoP.X - Projectile.Center.X);
            float gait = MathHelper.Lerp(0.9f, 1.8f,
                MathHelper.Clamp(distObjetivo / 420f, 0f, 1f));
            _fase += gait * MathHelper.TwoPi / 60f;

            // === EL CUERPO: la cadena follow CON GRAVEDAD — cada placa
            //     cae un poco y luego la ley la ata a su padre; y cada
            //     placa sondea SU suelo (la casa: cada 6 ticks, cacheado). ===
            _segs[0] = Projectile.Center;
            for (int i = 1; i < N; i++)
            {
                _segs[i] += new Vector2(0f, 0.30f);   // la gravedad de la marcha
                if (_age % 6 == 0) _suelos[i] = SueloBajo(_segs[i]);
                float? suelo = _suelos[i] ?? _sueloY;
                if (suelo.HasValue && _segs[i].Y > suelo.Value - 4f)
                    _segs[i].Y = suelo.Value - 4f;    // la placa no atraviesa el suelo

                Vector2 d = _segs[i] - _segs[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = new Vector2(MathF.Sign(_velX), 0f); dist = 1f; }
                _segs[i] = _segs[i - 1] + d / dist * Tamano;
            }

            Projectile.velocity = new Vector2(_velX, 0f);
            Projectile.rotation = _velX >= 0f ? 0f : MathHelper.Pi;

            // === LAS PATAS METACRONALES: φ_i = φ₀ − i·Δφ (la onda
            //     retrograda — de atrás hacia delante). ===
            for (int i = 0; i < N; i++)
                _patas[i] = _fase - i * DeltaPhi;

            // La luz cálida del ámbar rúnico.
            Lighting.AddLight(Projectile.Center, 0.20f, 0.14f, 0.06f);

            // === LAS RUNAS PLANTADAS PICAN (cada 12 ticks): la pata con
            //     sin(φ) < 0 está APOYADA — su pie es la runa. ===
            if (_age > 12 && _age % CadaRuna == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoRuna));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 0; i < N; i++)
                    {
                        if (MathF.Sin(_patas[i]) >= 0f) continue;   // solo plantadas
                        Vector2 pie = PieDe(i);
                        if (Vector2.DistanceSquared(npc.Center, pie) > 14f * 14f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.2f, true);
                        break;
                    }
                }
            }
        }

        /// <summary>El punto del pie de la pata i (la MISMA fórmula del render).</summary>
        private Vector2 PieDe(int i)
        {
            float phi = _patas[i];
            float barrido = MathF.Cos(phi) * 0.55f;
            float rd = MathHelper.Lerp(11f, 5f, i / (float)(N - 1));
            float largo = 13f + rd * 0.5f;
            float dirA = MathHelper.PiOver2 + barrido * ((i % 2 == 0) ? 1f : -1f);
            return _segs[i] + new Vector2(MathF.Cos(dirA), MathF.Sin(dirA)) * largo;
        }

        /// <summary>
        /// EL SONDEO DEL SUELO de la casa: la primera baldosa sólida bajo
        /// el punto (WorldGen.SolidTile, el patrón del Rencor). null si
        /// no hay suelo en 200 px — la placa sigue donde está.
        /// </summary>
        private static float? SueloBajo(Vector2 punto)
        {
            int tx = (int)(punto.X / 16f);
            int tyIni = (int)((punto.Y - 60f) / 16f);
            int tyFin = (int)((punto.Y + 200f) / 16f);
            if (tx < 12 || tx > Main.maxTilesX - 12) return null;
            for (int ty = Math.Max(10, tyIni); ty <= Math.Min(Main.maxTilesY - 10, tyFin); ty++)
                if (WorldGen.SolidTile(tx, ty))
                    return ty * 16f;
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                SierpesLib.CienpiesRunico(_segs, _patas, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private readonly float[] _patas = new float[N];
        private readonly float?[] _suelos = new float?[N];
        private float? _sueloY;
        private float _velX;
        private float _fase;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// FlageloEstelarProjectile — EL FLAGELO ESTELAR (v6.38 — PATRÓN 7).
    ///
    /// EL LÁTIGO (la física del chasquido): cadena Verlet con constraint
    /// repartido por MASA (m ∝ radio² — el taper 15→4.5 concentra la
    /// energía en la punta: v·√m ≈ const → la punta hereda ~×3 la
    /// velocidad del mango). El MANGO orbita al portador y cada 90 ticks
    /// BARRE media vuelta en 10 ticks — la energía viaja y la punta
    /// CHASQUEA.
    ///
    /// MECÁNICA: la punta golpea con daño ∝ su velocidad (×0.5 quieta →
    /// ×1.5 a tope, cada 8 ticks); cuando |v_punta| > 26 px/tick llega el
    /// CHASQUIDO: ×2.2 en 80 px + trauma de cámara. 8 s de flagelo.
    /// </summary>
    public class FlageloEstelarProjectile : ModProjectile
    {
        /// <summary>El N del látigo: catorce nudos.</summary>
        private const int N = 14;

        private const int Vida = 480;

        /// <summary>La distancia entre nudos.</summary>
        private const float Tamano = 16f;

        /// <summary>El radio de la órbita del mango.</summary>
        private const float Orbita = 44f;

        /// <summary>El período del barrido (ticks).</summary>
        private const int CadaBarrido = 90;

        /// <summary>La duración del barrido (ticks — media vuelta).</summary>
        private const int TicksBarrido = 10;

        /// <summary>El umbral del CHASQUIDO (px/tick).</summary>
        private const float UmbralCrack = 26f;

        /// <summary>Daño relativo del CHASQUIDO.</summary>
        private const float DañoCrack = 2.2f;

        /// <summary>Radio del CHASQUIDO.</summary>
        private const float RadioCrack = 80f;

        /// <summary>La masa de cada nudo (∝ radio² — el taper del látigo).</summary>
        private static float Masa(int i) =>
            MathHelper.Lerp(15f, 4.5f, i / (float)(N - 1)) *
            MathHelper.Lerp(15f, 4.5f, i / (float)(N - 1));

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;
            _anguloMango = Projectile.velocity.ToRotation();

            // Los nudos nacen colgando del mango (el Verlet los despliega).
            for (int i = 0; i < N; i++)
            {
                _pos[i] = Projectile.Center + new Vector2(-i * Tamano * 0.5f, 0f);
                _old[i] = _pos[i];
            }
            _prevPunta = _pos[N - 1];
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            // === EL MANGO: orbita al portador; cada 90 ticks BARRE media
            //     vuelta en 10 ticks (la energía que viaja a la punta). ===
            _ticksDesdeBarrido++;
            bool barriendo = _ticksDesdeBarrido <= TicksBarrido;
            _anguloMango += barriendo
                ? MathHelper.Pi / TicksBarrido
                : 0.030f;

            Vector2 mangoPrev = _pos[0];
            Vector2 mango = duenio.Center + new Vector2(
                MathF.Cos(_anguloMango), MathF.Sin(_anguloMango)) * Orbita;
            if (_ticksDesdeBarrido > CadaBarrido) _ticksDesdeBarrido = 0;

            // === LA INTEGRACIÓN VERLET: la velocidad vive en (pos − old) —
            //     INERCIA REAL (el cuerpo sigue volando cuando lo sueltas). ===
            _old[0] = mangoPrev;
            _pos[0] = mango;    // el mango PINNED: su velocidad entra sola
            for (int i = 1; i < N; i++)
            {
                Vector2 vel = (_pos[i] - _old[i]) * 0.992f;   // el drag estelar
                _old[i] = _pos[i];
                _pos[i] += vel;
            }

            // === EL CONSTRAINT POR MASA (3 iteraciones): el nudo pesado
            //     apenas se mueve, el liviano mucho — el momento se
            //     CONSERVA y la energía corre hacia la punta. ===
            for (int it = 0; it < 3; it++)
            {
                for (int i = 1; i < N; i++)
                {
                    Vector2 d = _pos[i] - _pos[i - 1];
                    float dist = d.Length();
                    if (dist < 0.0001f) continue;
                    float m1 = Masa(i - 1), m2 = Masa(i);
                    float corr = (dist - Tamano) / dist;
                    _pos[i - 1] += d * corr * (m2 / (m1 + m2));
                    _pos[i] -= d * corr * (m1 / (m1 + m2));
                }
                _pos[0] = mango;    // re-pin tras cada iteración
            }

            // === LA VELOCIDAD DE LA PUNTA (la heredera del mango). ===
            float vTip = Vector2.Distance(_pos[N - 1], _prevPunta);
            _prevPunta = _pos[N - 1];

            // El hitbox viaja con el mango (la punta daña por zone).
            Projectile.Center = _pos[0];
            Projectile.velocity = _pos[N - 1] - _pos[N - 2];
            Projectile.rotation = Projectile.velocity.ToRotation();

            // La luz carmesí del látigo (crece con la punta).
            float luz = 0.14f + 0.10f * MathHelper.Clamp(vTip / 26f, 0f, 1f);
            Lighting.AddLight(_pos[N - 1], luz, luz * 0.4f, luz * 0.25f);

            // === LA PUNTA GOLPEA ∝ SU VELOCIDAD (cada 8 ticks). ===
            if (_age > 10 && _age % 8 == 0)
            {
                float mult = 0.5f + MathHelper.Clamp(vTip / 30f, 0f, 1f) * 1.0f;
                int dmg = Math.Max(1, (int)(Projectile.damage * mult));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = N - 4; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _pos[i]) > 15f * 15f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.8f, true);
                        break;
                    }
                }
            }

            // === EL CHASQUIDO: la punta rompe el umbral — X2.2 en 80 px,
            //     trauma de cámara y el estruendo de la casa. ===
            if (_crackCd > 0) _crackCd--;
            if (vTip > UmbralCrack && _crackCd <= 0)
            {
                _crackCd = 30;
                // v6.50 — GolpeMotor: el chasquido corre en el cliente dueño
                // (el golpe va por el cauce del motor).
                Vector2 punta = _pos[N - 1];
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoCrack));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, punta) > RadioCrack * RadioCrack)
                        continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2.4f, true);
                }
                AudioLib.Sonar(AudioLib.Familia.Electrica, "impacto", _pos[N - 1], 1f, 0.10f);
                PulsoLib.Trauma(0.35f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                // EL CALOR por FRAME: la punta mide su velocidad de
                // DIBUJO (el AI corre 1×/tick — el render interpola).
                float vTipDraw = Vector2.Distance(_pos[N - 1], _prevPuntaDraw);
                _prevPuntaDraw = _pos[N - 1];
                SierpesLib.FlageloEstelar(_pos, N, vTipDraw,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // NOTA: la punta para el RENDER se mide una vez por frame (el AI
        // corre 1×/tick; el dibujo interpola) — el chasquido usa la del AI.
        private readonly Vector2[] _pos = new Vector2[N];
        private readonly Vector2[] _old = new Vector2[N];
        private Vector2 _prevPunta;
        private Vector2 _prevPuntaDraw;
        private float _anguloMango;
        private int _ticksDesdeBarrido = TicksBarrido + 1;
        private int _crackCd;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// ViboraGenesiacaProjectile — LA VÍBORA GENESÍACA (v6.38 — PATRÓN 9).
    ///
    /// LA DOBLE HÉLICE: la ley de cadena vive SOLO en la ESPINA (la
    /// MISMA ley de la sierpe, intacta); las dos hebras — la dorada y la
    /// violeta — son CAMPOS DE OFFSET alrededor de ella: hebra_i =
    /// espina_i ± (cos,sin)(φ₀ + i·2π/P)·R_i, con el radio creciendo
    /// hacia la cola (hélice cónica) y φ₀ girando vivo. Un esqueleto,
    /// dos cuerpos.
    ///
    /// MECÁNICA: las perlas de las hebras queman (×0.45 cada 9 ticks);
    /// cuando la víbora ENROSCA (giro cerrado del rumbo) la hélice se
    /// COMPRIME: las hebras convergen y arden ×1.6. La cabeza embiste
    /// por contacto. 9 s de víbora.
    /// </summary>
    public class ViboraGenesiacaProjectile : ModProjectile
    {
        /// <summary>El N de la espina: dieciséis vértebras.</summary>
        private const int N = 16;

        private const int Vida = 540;

        /// <summary>La distancia entre vértebras.</summary>
        private const float Tamano = 16f;

        /// <summary>La velocidad del nado.</summary>
        private const float Vel = 11f;

        /// <summary>El radio del nado alrededor del puntero.</summary>
        private const float Radm = 110f;

        /// <summary>Los segmentos por vuelta de la hélice (P).</summary>
        private const int P = 6;

        /// <summary>La velocidad de giro del entrelazado (rad/tick).</summary>
        private const float GiroTrenza = 0.10f;

        /// <summary>Cadencia de la quemadura de las perlas.</summary>
        private const int CadaPerla = 9;

        /// <summary>Daño relativo de las perlas.</summary>
        private const float DañoPerla = 0.45f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Sembrar();
        }

        /// <summary>v6.50.3 — FIX (hallazgo V-1): la autocuración MP de la
        /// casa — netImportant corre la IA en los remotos pero OnSpawn no
        /// viaja en el msg 27; sin esto la víbora nacía en (0,0) para los
        /// remotos (misma familia que las órbitas de v6.50.2).</summary>
        private void Sembrar()
        {
            Seed = (int)Projectile.ai[2] % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation() : 0f;

            Vector2 atras = new Vector2(MathF.Cos(_rumbo + MathHelper.Pi),
                MathF.Sin(_rumbo + MathHelper.Pi));
            for (int i = 0; i < N; i++)
                _espina[i] = Projectile.Center + atras * (i * 2f);
            _sembrado = true;
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!_sembrado) Sembrar(); // v6.50.3 — autocura del remoto

            float time = Main.GlobalTimeWrappedHourly;

            // === EL PUNTERO (patrón congregación — correa 760 px). ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 puntero = Main.MouseWorld;
                Vector2 alDueño = puntero - duenio.Center;
                if (alDueño.Length() > 760f)
                    puntero = duenio.Center + Vector2.Normalize(alDueño) * 760f;

                if (Vector2.DistanceSquared(puntero,
                    new Vector2(Projectile.ai[0], Projectile.ai[1])) > 24f * 24f ||
                    _age % 30 == 0)
                {
                    Projectile.ai[0] = puntero.X;
                    Projectile.ai[1] = puntero.Y;
                    Projectile.netUpdate = true;
                }
            }
            Vector2 objetivo = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === EL NADO DE LA ESPINA: la MISMA ley de la sierpe (el
            //     punto tangente del círculo — el ADN intacto). ===
            Vector2 alPuntero = _espina[0] - objetivo;
            float distP = alPuntero.Length();
            float deseado;
            if (distP > Radm * 2.4f && distP > 1f)
                deseado = (objetivo - _espina[0]).ToRotation();
            else if (distP > 1f)
                deseado = alPuntero.ToRotation() + MathHelper.PiOver2;
            else
                deseado = _rumbo;

            float delta = MathHelper.WrapAngle(deseado - _rumbo);
            float giro = MathHelper.Clamp(delta, -0.07f, 0.07f);
            _rumbo += giro;

            // LA COMPRESIÓN: el giro cerrado del rumbo enrosca la hélice.
            _giroSuave = MathHelper.Lerp(_giroSuave, MathF.Abs(giro), 0.10f);
            float compresion = MathHelper.Clamp(_giroSuave / 0.05f, 0f, 1f);

            float surge = 1f + 0.30f * MathF.Sin(time * MathHelper.TwoPi * 0.9f + Seed);
            Vector2 vel = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) * (Vel * surge);
            _espina[0] += vel;
            for (int i = 1; i < N; i++)
            {
                Vector2 d = _espina[i] - _espina[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = Vector2.UnitX; dist = 1f; }
                _espina[i] = _espina[i - 1] + d / dist * Tamano;
            }

            // === LAS DOS HEBRAS: el offset helicoidal que GIRA (la
            //     trenza viva) — radio cónico, comprimido al enroscar. ===
            _trenza += GiroTrenza;
            for (int i = 0; i < N; i++)
            {
                float t = i / (float)(N - 1);
                float R = MathHelper.Lerp(9f, 24f, t) * (1f - 0.4f * compresion);
                float a = _trenza + i * MathHelper.TwoPi / P;
                Vector2 u = new Vector2(MathF.Cos(a), MathF.Sin(a)) * R;
                _hebraA[i] = _espina[i] + u;
                _hebraB[i] = _espina[i] - u;
            }

            // El hitbox viaja con la cabeza de la espina.
            Projectile.Center = _espina[0];
            Projectile.velocity = vel;
            Projectile.rotation = _rumbo;

            // La luz doble (dorada + violeta).
            Lighting.AddLight(Projectile.Center, 0.14f, 0.10f, 0.18f);

            // === LAS PERLAS QUEMAN (cada 9 ticks — ×1.6 al comprimir). ===
            if (_age > 10 && _age % CadaPerla == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoPerla *
                    (1f + 0.6f * compresion)));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 1; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _hebraA[i]) > 12f * 12f &&
                            Vector2.DistanceSquared(npc.Center, _hebraB[i]) > 12f * 12f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.4f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                float compresion = MathHelper.Clamp(_giroSuave / 0.05f, 0f, 1f);
                SierpesLib.ViboraGenesiaca(_espina, _hebraA, _hebraB, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha, compresion);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _espina = new Vector2[N];
        private readonly Vector2[] _hebraA = new Vector2[N];
        private readonly Vector2[] _hebraB = new Vector2[N];
        private bool _sembrado;   // v6.50.3 — la autocuración MP
        private float _rumbo;
        private float _trenza;
        private float _giroSuave;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// BoaEclipseProjectile — LA BOA DEL ECLIPSE (v6.38 — PATRÓN 10).
    ///
    /// EL CONSTRICTOR: la cadena NO cambia — cambia el LÍDER: la cabeza
    /// hace la SERVO-ESPIRAL sobre la presa (θ += ω; radio que encoge en
    /// ~70 ticks hasta el hitbox + 22) y el cuerpo la sigue envolviendo.
    /// Cada VUELTA COMPLETA es un stack de APRIETE: el daño por tick
    /// crece con las vueltas (×0.5 → ×4.0), como la boa real que aprieta
    /// sincronizada con la exhalación de la presa.
    ///
    /// MECÁNICA: se lanza a la presa más cercana (800 px), enrosca 2-3
    /// vueltas apretando; la presa muere → se desenrosca y busca la
    /// siguiente. 10 s de abrazo.
    /// </summary>
    public class BoaEclipseProjectile : ModProjectile
    {
        /// <summary>El N de la boa: catorce vértebras.</summary>
        private const int N = 14;

        private const int Vida = 600;

        /// <summary>La distancia entre vértebras.</summary>
        private const float Tamano = 15f;

        /// <summary>La velocidad del lanzamiento (px/tick).</summary>
        private const float VelLanzar = 15f;

        /// <summary>La velocidad del enroscado (px/tick).</summary>
        private const float VelEnroscar = 11f;

        /// <summary>ω de la servo-espiral (rad/tick).</summary>
        private const float Omega = 0.16f;

        /// <summary>El apriete por vuelta: daño ×(0.5 + 0.35·vueltas).</summary>
        private const float AprietePorVuelta = 0.35f;

        /// <summary>Cadencia de la presión.</summary>
        private const int CadaApretar = 10;

        private const int EstadoLanzar = 0;
        private const int EstadoEnroscar = 1;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation() : 0f;

            Vector2 atras = new Vector2(MathF.Cos(_rumbo + MathHelper.Pi),
                MathF.Sin(_rumbo + MathHelper.Pi));
            for (int i = 0; i < N; i++)
                _segs[i] = Projectile.Center + atras * (i * 2f);
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === LA PRESA (dueño local manda — ai[0..1] la sincroniza
            //     para el render de los demás). ===
            if (Projectile.owner == Main.myPlayer && _age % 15 == 0)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 800f * 800f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                _presa = mejor;
                if (_presa != null && _estado == EstadoEnroscar)
                {
                    // cambio de presa: soltar el enrosque y lanzarse
                    _estado = EstadoLanzar;
                }
            }

            NPC presa = _presa;
            bool presaViva = presa != null && presa.active && presa.CanBeChasedBy();
            if (!presaViva && _estado == EstadoEnroscar)
            {
                _estado = EstadoLanzar;     // la presa murió: soltar
                _vueltas = 0f;
            }

            // === LA LEY DEL LÍDER (lo único que cambia de la sierpe). ===
            if (_estado == EstadoEnroscar && presaViva)
            {
                // LA SERVO-ESPIRAL: θ avanza, el radio ENCOGE.
                _theta += Omega;
                float radioMin = presa.width * 0.5f + 22f;
                _radio = MathF.Max(_radio * (1f - 1f / 70f), radioMin);
                _vueltas += Omega / MathHelper.TwoPi;

                Vector2 deseadoP = presa.Center + new Vector2(
                    MathF.Cos(_theta), MathF.Sin(_theta)) * _radio;

                Vector2 alPunto = deseadoP - Projectile.Center;
                float deseado = alPunto.LengthSquared() > 1f ? alPunto.ToRotation() : _rumbo;
                float delta = MathHelper.WrapAngle(deseado - _rumbo);
                _rumbo += MathHelper.Clamp(delta, -0.12f, 0.12f);

                Vector2 vel = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) * VelEnroscar;
                Projectile.Center += vel;
                Projectile.velocity = vel;

                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.ai[0] = presa.Center.X;
                    Projectile.ai[1] = presa.Center.Y;
                }
            }
            else
            {
                // EL LANZAMIENTO: embiste a la presa (o nada vueltas
                // alrededor del puntero si no hay nadie).
                // (El puntero se sincroniza en este estado — el fallback
                // de nado sigue al cursor, patrón congregación.)
                if (Projectile.owner == Main.myPlayer && _age % 20 == 0)
                {
                    Vector2 punteroS = Main.MouseWorld;
                    Vector2 alDueñoS = punteroS - duenio.Center;
                    if (alDueñoS.Length() > 760f)
                        punteroS = duenio.Center + Vector2.Normalize(alDueñoS) * 760f;
                    Projectile.ai[0] = punteroS.X;
                    Projectile.ai[1] = punteroS.Y;
                    Projectile.netUpdate = true;
                }
                float deseado;
                if (presaViva)
                    deseado = (presa.Center - Projectile.Center).ToRotation();
                else
                {
                    Vector2 puntero = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                    Vector2 alPuntero = Projectile.Center - puntero;
                    float distP = alPuntero.Length();
                    if (distP > 140f && distP > 1f)
                        deseado = (puntero - Projectile.Center).ToRotation();
                    else if (distP > 1f)
                        deseado = alPuntero.ToRotation() + MathHelper.PiOver2;
                    else
                        deseado = _rumbo;
                }
                float delta = MathHelper.WrapAngle(deseado - _rumbo);
                _rumbo += MathHelper.Clamp(delta, -0.10f, 0.10f);
                Vector2 vel = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) * VelLanzar;
                Projectile.Center += vel;
                Projectile.velocity = vel;

                // ¿LA MORDIDA? — cerca de la presa: comenzar el enrosque.
                if (presaViva)
                {
                    float dist = Vector2.Distance(Projectile.Center, presa.Center);
                    if (dist < presa.width * 0.5f + 56f)
                    {
                        _estado = EstadoEnroscar;
                        _theta = (Projectile.Center - presa.Center).ToRotation();
                        _radio = MathF.Max(dist, presa.width * 0.5f + 30f);
                        _vueltas = 0f;
                        AudioLib.Sonar(AudioLib.Familia.Runico, "apertura",
                            Projectile.Center, 0.8f, -0.15f);
                    }
                }
            }
            Projectile.rotation = _rumbo;

            // === LA CADENA (la MISMA ley — la boa envuelve). ===
            _segs[0] = Projectile.Center;
            for (int i = 1; i < N; i++)
            {
                Vector2 d = _segs[i] - _segs[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = Vector2.UnitX; dist = 1f; }
                _segs[i] = _segs[i - 1] + d / dist * Tamano;
            }

            // La luz del eclipse (blanco + violeta).
            Lighting.AddLight(Projectile.Center, 0.16f, 0.12f, 0.22f);

            // === EL APRIETE (cada 10 ticks): ×(0.5 + 0.35·vueltas). ===
            if (_age > 10 && _age % CadaApretar == 0)
            {
                int stacks = Math.Min((int)_vueltas, 10);
                int dmg = Math.Max(1, (int)(Projectile.damage *
                    (0.5f + AprietePorVuelta * stacks)));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 0; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _segs[i]) > 17f * 17f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.4f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                // La presa para los aros de apriete (la posición sincro-
                // nizada de ai — el cliente sin presa la dibuja igual).
                Vector2 centroPresa = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                float radioPresa = _estado == EstadoEnroscar && _radio > 8f
                    ? _radio : 60f;
                SierpesLib.BoaEclipse(_segs, N, (int)_vueltas, centroPresa,
                    radioPresa, Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private NPC _presa;
        private int _estado = EstadoLanzar;
        private float _rumbo;
        private float _theta;
        private float _radio = 60f;
        private float _vueltas;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// FarolGuardianProjectile — EL FAROL GUARDIÁN (v6.38 — PATRÓN 13).
    ///
    /// LA CINEMÁTICA INVERSA (FABRIK): el mando está INVERTIDO — la BASE
    /// es un farol colgante anclado sobre el portador y la PUNTA PERSIGUE
    /// a la presa con las DOS PASADAS del reach (adelante: punta→objetivo
    /// deslizando los eslabones; atrás: base→ancla conservando el largo).
    /// La criatura ALCANZA, no nada: el cuerpo siempre se TENSA como un
    /// arco hacia lo que toca.
    ///
    /// MECÁNICA: 12 s de guardián: la MANO muerde ×1.0 (cada 9 ticks), la
    /// cadena quema ×0.40 (cada 14). Si la presa está más lejos del
    /// alcance ((N−1)·Tamano), el farol se ESTIRA hacia ella sin tocarla.
    /// </summary>
    public class FarolGuardianProjectile : ModProjectile
    {
        /// <summary>El N del guardián: dieciocho eslabones.</summary>
        private const int N = 18;

        private const int Vida = 720;

        /// <summary>La distancia entre eslabones.</summary>
        private const float Tamano = 13f;

        /// <summary>El alcance máximo: (N−1)·Tamano px desde el ancla.</summary>
        private static readonly float Alcance = (N - 1) * Tamano;

        /// <summary>La cadencia de la mordida de la MANO.</summary>
        private const int CadaMano = 9;

        /// <summary>La cadencia de la quemadura de la cadena.</summary>
        private const int CadaEslabon = 14;

        /// <summary>Daño relativo de la MANO.</summary>
        private const float DañoMano = 1.0f;

        /// <summary>Daño relativo de los eslabones.</summary>
        private const float DañoEslabon = 0.40f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;

            // El cuerpo nace colgando del ancla (la FABRIK lo despliega).
            Player duenio = Main.player[Projectile.owner];
            Vector2 ancla = duenio != null && duenio.active
                ? AnclaDe(duenio, 0f) : Projectile.Center;
            for (int i = 0; i < N; i++)
            {
                _segs[i] = ancla + new Vector2(MathF.Sin(i * 0.35f) * 4f, i * Tamano * 0.5f);
            }
            _objetivo = Projectile.Center;
            Projectile.Center = _segs[N - 1];
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL OBJETIVO (dueño local manda — la presa más cercana
            //     al JUGADOR, si no el cursor, con correa 500 del ancla). ===
            if (Projectile.owner == Main.myPlayer && _age % 10 == 0)
            {
                Vector2 ancla = AnclaDe(duenio, time);
                Vector2 objetivo = Main.MouseWorld;
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, ancla);
                    if (d < 700f * 700f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor != null) objetivo = mejor.Center;

                Vector2 alAncla = objetivo - ancla;
                if (alAncla.Length() > 500f)
                    objetivo = ancla + Vector2.Normalize(alAncla) * 500f;

                Projectile.ai[0] = objetivo.X;
                Projectile.ai[1] = objetivo.Y;
                Projectile.netUpdate = true;
            }
            _objetivo = Vector2.Lerp(_objetivo,
                new Vector2(Projectile.ai[0], Projectile.ai[1]), 0.25f);

            // === EL ANCLA: el farol colgante (mece con el tiempo). ===
            Vector2 anclaViva = AnclaDe(duenio, time);

            // === FABRIK — PASADA 1 (ADELANTE): la PUNTA manda: se planta
            //     en el objetivo y los eslabones se deslizan conservando
            //     su largo (reach de punta a base). ===
            _segs[N - 1] = _objetivo;
            for (int i = N - 2; i >= 0; i--)
            {
                Vector2 d = _segs[i] - _segs[i + 1];
                float len = d.Length();
                if (len < 0.0001f) d = Vector2.UnitX; else d /= len;
                _segs[i] = _segs[i + 1] + d * Tamano;
            }

            // === FABRIK — PASADA 2 (ATRÁS): la BASE vuelve a su ancla y
            //     el resto se re-desliza — la cadena queda TENSA. ===
            _segs[0] = anclaViva;
            for (int i = 0; i < N - 1; i++)
            {
                Vector2 d = _segs[i + 1] - _segs[i];
                float len = d.Length();
                if (len < 0.0001f) d = Vector2.UnitX; else d /= len;
                _segs[i + 1] = _segs[i] + d * Tamano;
            }

            // El hitbox viaja con la MANO (la que muerde).
            Projectile.Center = _segs[N - 1];
            Projectile.velocity = _segs[N - 1] - _segs[N - 2];
            Projectile.rotation = Projectile.velocity.ToRotation();

            // La luz cálida del farol.
            Lighting.AddLight(anclaViva, 0.25f, 0.20f, 0.10f);
            Lighting.AddLight(_segs[N - 1], 0.10f, 0.14f, 0.20f);

            // === LA MANO MUERDE (cada 9 ticks) y LA CADENA QUEMA (cada
            //     14) — solo si ESTÁ ALCANZANDO presa de verdad. ===
            bool alcanzando = Vector2.Distance(anclaViva, _objetivo) <= Alcance * 1.04f;
            if (_age > 8)
            {
                if (_age % CadaMano == 0 && alcanzando)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoMano));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, _segs[N - 1]) > 13f * 13f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.6f, true);
                    }
                }
                if (_age % CadaEslabon == 0)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoEslabon));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        for (int i = 3; i < N - 2; i++)
                        {
                            if (Vector2.DistanceSquared(npc.Center, _segs[i]) > 11f * 11f)
                                continue;
                            Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.2f, true);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>EL ANCLA: el farol colgante sobre el hombro del dueño.</summary>
        private static Vector2 AnclaDe(Player duenio, float time)
        {
            float mecer = MathF.Sin(time * 0.8f) * 8f;
            return duenio.Center + new Vector2(-duenio.direction * 14f + mecer, -62f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 24;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                bool alcanzando = Vector2.Distance(_segs[0], _objetivo) <= Alcance * 1.04f;
                SierpesLib.FarolGuardian(_segs, N,
                    Main.GlobalTimeWrappedHourly, Seed, alpha, alcanzando);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private Vector2 _objetivo;
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// CintaAuroraProjectile — LA CINTA AURORA (v6.38 — PATRÓN 14).
    ///
    /// LA CINTA AL VIENTO: la espina es la cadena follow ANCLADA a la
    /// espalda del corredor (la espina flota con lift y vaivén propios);
    /// el cuerpo visible es el OFFSET LATERAL — dos senos INCONMENSURABLES
    /// (la regla de determinismo de la casa) cuya amplitud crece hacia la
    /// punta y se multiplica por el VIENTO — y el viento ES la velocidad
    /// de carrera del portador.
    ///
    /// MECÁNICA: 12 s de estandarte: la cinta CORTA lo que toca (×0.5 +
    /// ×0.4·viento cada 10 ticks) — corre fuerte y la aurora se vuelve
    /// látigo.
    /// </summary>
    public class CintaAuroraProjectile : ModProjectile
    {
        /// <summary>El N de la cinta: dieciséis nodos.</summary>
        private const int N = 16;

        private const int Vida = 720;

        /// <summary>La distancia entre nodos de la espina.</summary>
        private const float Tamano = 15f;

        /// <summary>El largo del cuerpo (px).</summary>
        private static readonly float L = (N - 1) * Tamano;

        /// <summary>La amplitud base de la ondulación (px).</summary>
        private const float Amplitud = 20f;

        /// <summary>Las frecuencias INCONMENSURABLES de los dos senos (rad/s).</summary>
        private const float W1 = 6.0f;
        private const float W2 = 9.9f;

        /// <summary>Los números de onda de los dos senos (rad/px).</summary>
        private static readonly float K1 = MathHelper.TwoPi * 1.5f / L;
        private static readonly float K2 = MathHelper.TwoPi * 2.4f / L;

        /// <summary>Cadencia del corte de la aurora.</summary>
        private const int CadaCorte = 10;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Seed = (int)Projectile.ai[2] % 9973;

            // La espina nace colgando del broche (el follow la despliega).
            for (int i = 0; i < N; i++)
                _espina[i] = Projectile.Center + new Vector2(0f, i * Tamano * 0.4f);
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly;

            // === EL VIENTO: la velocidad de carrera del portador — la
            //     amplitud de la aurora ES la carrera. ===
            float viento = MathHelper.Clamp(duenio.velocity.Length() / 8f, 0f, 1.2f);

            // === LA ESPINA ANCLADA: el broche en la espalda; los nodos
            //     flotan (lift + vaivén) y la ley los ata. ===
            Vector2 broche = duenio.Center + new Vector2(-duenio.direction * 6f, -18f);
            _espina[0] = broche;
            for (int i = 1; i < N; i++)
            {
                float t = i / (float)(N - 1);
                // EL LIFT: la cinta flota hacia arriba (más hacia la punta).
                _espina[i] += new Vector2(0f, -0.22f * t);
                // EL VAIVÉN del aire (determinista — senos de la casa).
                _espina[i] += new Vector2(
                    MathF.Sin(time * 1.7f + i * 0.6f) * 0.10f * t * (0.5f + viento), 0f);

                // LA LEY DE CADENA (la MISMA — el broche manda).
                Vector2 d = _espina[i] - _espina[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = Vector2.UnitY; dist = 1f; }
                _espina[i] = _espina[i - 1] + d / dist * Tamano;
            }

            // === EL CUERPO: el offset lateral de los dos senos
            //     inconmensurables — la amplitud crece hacia la punta y
            //     respira con el viento. ===
            _cuerpo[0] = _espina[0];
            for (int i = 1; i < N; i++)
            {
                float s = i * Tamano;
                float off = (0.62f * MathF.Sin(W1 * time + K1 * s) +
                             0.38f * MathF.Sin(W2 * time + K2 * s + 1.7f));
                off *= MathF.Pow(s / L, 1.1f) * Amplitud * (0.35f + 0.85f * viento);

                // La normal local de la espina (el frente y tras del nodo).
                Vector2 delante = _espina[Math.Min(i + 1, N - 1)] - _espina[Math.Max(i - 1, 0)];
                float len = delante.Length();
                if (len < 0.0001f) delante = Vector2.UnitY; else delante /= len;
                Vector2 normal = new Vector2(-delante.Y, delante.X);

                _cuerpo[i] = _espina[i] + normal * off;
            }

            // El hitbox del broche (la cinta es una zona — daña por corte).
            Projectile.Center = _cuerpo[0];
            Projectile.velocity = duenio.velocity;
            Projectile.rotation = 0f;

            // La luz de la aurora (verde → violeta).
            Lighting.AddLight(_cuerpo[N / 2], 0.10f, 0.16f, 0.12f);

            // === EL CORTE DE LA AURORA (cada 10 ticks): la cinta corta
            //     lo que toca — más duro con el viento (×0.5 → ×0.9). ===
            if (_age > 10 && _age % CadaCorte == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage *
                    (0.5f + 0.4f * MathHelper.Clamp(viento, 0f, 1f))));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 2; i < N; i++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _cuerpo[i]) > 14f * 14f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.2f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 24;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                Player duenio = Main.player[Projectile.owner];
                float viento = duenio != null && duenio.active
                    ? MathHelper.Clamp(duenio.velocity.Length() / 8f, 0f, 1.2f) : 0f;
                SierpesLib.CintaAurora(_cuerpo, N, viento,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _espina = new Vector2[N];
        private readonly Vector2[] _cuerpo = new Vector2[N];
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// ManadaAstralProjectile — LA MANADA ASTRAL (v6.38 — PATRÓN 8).
    ///
    /// LOS BOIDS / LA CADENA BLANDA: seis cazadores con distancia
    /// ELÁSTICA (steering suave hacia el punto d px detrás del de
    /// delante — nunca teletransportan, se ESTIRAN al acelerar y se
    /// comprimen al girar) y LIDERAZGO ROTATIVO: el mando SALTA al
    /// cazador más cercano a la presa — cuando la presa cae, la corona
    /// pasa a otro cuello.
    ///
    /// MECÁNICA: el líder embiste; cada cazador golpea CON SU PROPIA
    /// VELOCIDAD (×0.55 quieto → ×1.4 a tope, cada 10 ticks); las colas
    /// (3 cuentas por cazador, la distancia elástica hecha visible)
    /// queman ×0.30. 10 s de cacería.
    /// </summary>
    public class ManadaAstralProjectile : ModProjectile
    {
        /// <summary>Los cazadores de la manada.</summary>
        private const int Caz = 6;

        /// <summary>Las cuentas de la cola de cada cazador.</summary>
        private const int ColaN = 3;

        private const int Vida = 600;

        /// <summary>La velocidad máxima de la manada (px/tick).</summary>
        private const float VelMax = 13f;

        /// <summary>La distancia elástica tras el de delante (px).</summary>
        private const float Distancia = 26f;

        /// <summary>La cadencia del golpe de los cazadores.</summary>
        private const int CadaCaza = 10;

        /// <summary>La cadencia de la cola.</summary>
        private const int CadaCola = 12;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Sembrar();
        }

        /// <summary>v6.50.3 — FIX (hallazgo V-1): la autocuración MP de la
        /// casa — netImportant corre la IA en los remotos pero OnSpawn no
        /// viaja en el msg 27; sin esto la manada nacía en (0,0) para los
        /// remotos y su Center se arrastraba a la esquina del mundo
        /// (misma familia que las órbitas de v6.50.2).</summary>
        private void Sembrar()
        {
            Seed = (int)Projectile.ai[2] % 9973;

            // La manada nace en vuelo en círculo alrededor del punto.
            for (int c = 0; c < Caz; c++)
            {
                float a = c * MathHelper.TwoPi / Caz;
                _caz[c] = Projectile.Center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 24f;
                _vel[c] = new Vector2(MathF.Cos(a + MathHelper.PiOver2),
                    MathF.Sin(a + MathHelper.PiOver2)) * (VelMax * 0.5f);
                for (int k = 0; k < ColaN; k++)
                    _colas[c * ColaN + k] = _caz[c];
            }
            _orden[0] = 0;
            _sembrado = true;
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!_sembrado) Sembrar(); // v6.50.3 — autocura del remoto

            float time = Main.GlobalTimeWrappedHourly;

            // === LA PRESA (dueño local manda — ai[0..1] sincroniza). ===
            if (Projectile.owner == Main.myPlayer && _age % 15 == 0)
            {
                Vector2 objetivo = Main.MouseWorld;
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 900f * 900f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor != null) objetivo = mejor.Center;
                else
                {
                    Vector2 alDueño = objetivo - duenio.Center;
                    if (alDueño.Length() > 760f)
                        objetivo = duenio.Center + Vector2.Normalize(alDueño) * 760f;
                }
                Projectile.ai[0] = objetivo.X;
                Projectile.ai[1] = objetivo.Y;
                Projectile.netUpdate = true;
            }
            Vector2 objetivoP = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // === EL LIDERAZGO ROTATIVO (cada 15 ticks): el cazador más
            //     cercano a la presa toma la corona — el mando SALTA. ===
            if (_age % 15 == 0)
            {
                // El orden de la cadena: por cercanía a la presa.
                // v6.50.3 — FIX (cero GC): new int[6] + Array.Sort con CLOSURE
                // (delegado nuevo + array nuevo) cada 15 ticks — churn evitable.
                // Insertion-sort IN PLACE sobre _orden (Caz=6: el algoritmo
                // chico gana de sobra y no aloca NADA).
                if (!_ordenInit)
                {
                    for (int c = 0; c < Caz; c++) _orden[c] = c;
                    _ordenInit = true;
                }
                for (int a = 1; a < Caz; a++)
                {
                    int v = _orden[a];
                    float dv = Vector2.DistanceSquared(_caz[v], objetivoP);
                    int b = a - 1;
                    while (b >= 0 && Vector2.DistanceSquared(_caz[_orden[b]], objetivoP) > dv)
                    {
                        _orden[b + 1] = _orden[b];
                        b--;
                    }
                    _orden[b + 1] = v;
                }
            }
            int lider = _orden[0];

            // === EL STEERING (la cadena blanda): el líder embiste; cada
            //     seguidor busca el punto d px DETRÁS del de delante —
            //     con alineación y cohesión (los BOIDS de la casa). ===
            for (int m = 0; m < Caz; m++)
            {
                int c = _orden[m];
                Vector2 deseada;
                if (m == 0)
                    deseada = (objetivoP - _caz[c]).SafeNormalize(Vector2.UnitX) * VelMax;
                else
                {
                    int ahead = _orden[m - 1];
                    Vector2 velA = _vel[ahead];
                    float vLen = MathF.Max(velA.Length(), 0.5f);
                    Vector2 ancla = _caz[ahead] - velA / vLen * Distancia;
                    deseada = (ancla - _caz[c]).SafeNormalize(Vector2.UnitX) * (VelMax * 0.85f);
                }

                Vector2 steer = (deseada - _vel[c]) * 0.15f;
                if (m > 0)
                    steer += (_vel[_orden[m - 1]] - _vel[c]) * 0.10f;   // la alineación

                // EL LÍMITE de fuerza (la suavidad de la cadena blanda).
                float flen = steer.Length();
                if (flen > 0.55f) steer = steer / flen * 0.55f;
                _vel[c] += steer;
                _caz[c] += _vel[c];
            }

            // LA SEPARACIÓN (nadie se monta encima de nadie).
            for (int a = 0; a < Caz; a++)
                for (int b = a + 1; b < Caz; b++)
                {
                    Vector2 d = _caz[b] - _caz[a];
                    float len = d.Length();
                    if (len > 0.001f && len < 30f)
                    {
                        Vector2 empuje = d / len * 0.4f;
                        _caz[a] -= empuje;
                        _caz[b] += empuje;
                    }
                }

            // === LAS COLAS (la distancia elástica hecha visible). ===
            for (int c = 0; c < Caz; c++)
            {
                Vector2 padre = _caz[c];
                for (int k = 0; k < ColaN; k++)
                {
                    int idxC = c * ColaN + k;
                    Vector2 d = _colas[idxC] - padre;
                    float len = d.Length();
                    if (len < 0.0001f) { d = Vector2.UnitY; len = 1f; }
                    _colas[idxC] = padre + d / len * 10f;
                    padre = _colas[idxC];
                }
            }

            // El hitbox viaja con el líder.
            Projectile.Center = _caz[lider];
            Projectile.velocity = _vel[lider];
            Projectile.rotation = _vel[lider].ToRotation();

            // La luz azul de la manada (la del líder).
            Lighting.AddLight(_caz[lider], 0.12f, 0.16f, 0.24f);

            // === LA CAZA (cada 10 ticks): cada cazador golpea CON SU
            //     PROPIA VELOCIDAD — la manada entera se vuelve arma. ===
            if (_age > 10 && _age % CadaCaza == 0)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int c = 0; c < Caz; c++)
                    {
                        float rap = MathHelper.Clamp(_vel[c].Length() / VelMax, 0f, 1f);
                        if (Vector2.DistanceSquared(npc.Center, _caz[c]) > 13f * 13f)
                            continue;
                        int dmg = Math.Max(1, (int)(Projectile.damage * (0.55f + 0.85f * rap)));
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                        break;   // un cazador por enemigo en este tick
                    }
                }
            }

            // === LAS COLAS QUEMAN (cada 12 ticks — ×0.30). ===
            if (_age > 12 && _age % CadaCola == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * 0.30f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int idx = 0; idx < Caz * ColaN; idx++)
                    {
                        if (Vector2.DistanceSquared(npc.Center, _colas[idx]) > 9f * 9f)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.0f, true);
                        break;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                const int Funda = 20;
                float alpha = 1f;
                if (_age < Funda) alpha = _age / (float)Funda;
                else if (Projectile.timeLeft < Funda) alpha = Math.Max(0f, Projectile.timeLeft / (float)Funda);

                int lider = _orden[0];
                for (int c = 0; c < Caz; c++)
                    _rapidez[c] = MathHelper.Clamp(_vel[c].Length() / VelMax, 0f, 1f);
                SierpesLib.ManadaAstral(_caz, _colas, Caz, lider, _rapidez,
                    Main.GlobalTimeWrappedHourly, Seed, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private readonly Vector2[] _caz = new Vector2[Caz];
        private readonly Vector2[] _vel = new Vector2[Caz];
        private readonly Vector2[] _colas = new Vector2[Caz * ColaN];
        private readonly int[] _orden = new int[Caz];
        private bool _ordenInit;   // v6.50.3 — la inicialización perezosa del orden
        private bool _sembrado;   // v6.50.3 — la autocuración MP
        private readonly float[] _rapidez = new float[Caz];
        private float _age;

        private int Seed { get; set; }
    }

    /// <summary>
    /// CriaEstelarMinion — LA CRÍA ESTELAR (v6.38 — EL MINION).
    ///
    /// LA SIERPE ESTELAR HECHA SIRVIENTE: su hijita — diez segmentos con
    /// el MISMO ADN (la cadena follow, el nado del punto tangente, el
    /// vaivén) pero CACHORRA: cabezona, de ojos enormes y solo dos
    /// aletas. En REPOSO nada en círculos alrededor de su dueño (cada
    /// cría con su propia órbita — la camada no se monta encima); en
    /// CAZA la cabeza embiste a la presa (daño de contacto de minion) y
    /// la espinita quema a su paso.
    ///
    /// Es un MINION de verdad: buff sostenido (patrón de la medusa
    /// nebulosa), minionSlots 1, se invocan varias con espacio de
    /// sirvientes, la cría no expira nunca.
    /// </summary>
    public class CriaEstelarMinion : ModProjectile
    {
        /// <summary>El N de la cría: diez segmentitos.</summary>
        private const int N = 10;

        /// <summary>La distancia entre segmentitos.</summary>
        private const float Tamano = 12f;

        /// <summary>El radio del nado en reposo (px).</summary>
        private const float Radm = 78f;

        /// <summary>La velocidad del nado en reposo.</summary>
        private const float VelReposo = 7.5f;

        /// <summary>La velocidad de la caza.</summary>
        private const float VelCaza = 12.5f;

        /// <summary>La cadencia de la quemadura de la espinita.</summary>
        private const int CadaEspina = 12;

        /// <summary>Daño relativo de la espinita.</summary>
        private const float DañoEspina = 0.4f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;

        public override bool MinionContactDamage() => true;

        public override void OnSpawn(IEntitySource source)
        {
            Sembrar();
        }

        /// <summary>v6.50.3 — FIX (hallazgo V-1): la autocuración MP de la
        /// casa — netImportant corre la IA en los remotos pero OnSpawn no
        /// viaja en el msg 27; sin esto la cría nacía en (0,0) para los
        /// remotos (misma familia que las órbitas de v6.50.2). La semilla
        /// viaja con la identidad (determinista en todos los clientes —
        /// la regla de la casa para los renders).</summary>
        private void Sembrar()
        {
            Seed = (Projectile.identity * 31 + 17) % 9973;
            _rumbo = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation() : 0f;

            Vector2 atras = new Vector2(MathF.Cos(_rumbo + MathHelper.Pi),
                MathF.Sin(_rumbo + MathHelper.Pi));
            for (int i = 0; i < N; i++)
                _segs[i] = Projectile.Center + atras * (i * 2f);
            _sembrado = true;
        }

        public override void AI()
        {
            _age++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            CheckMinionBuff(duenio);

            if (!_sembrado) Sembrar(); // v6.50.3 — autocura del remoto

            float time = Main.GlobalTimeWrappedHourly;

            // === LA PRESA (con el objetivo manual del jugador si lo fijó). ===
            NPC presa = null;
            if (duenio.HasMinionAttackTargetNPC)
            {
                NPC candidato = Main.npc[duenio.MinionAttackTargetNPC];
                if (candidato != null && candidato.active && candidato.CanBeChasedBy())
                    presa = candidato;
            }
            if (presa == null && _age % 12 == 0)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, duenio.Center);
                    if (d < 650f * 650f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                _presaFija = mejor;
            }
            if (presa == null) presa = _presaFija;
            if (presa != null && (!presa.active || !presa.CanBeChasedBy()))
            {
                presa = null;
                _presaFija = null;
            }

            // === LA LEY DEL LÍDER: Caza (embiste) o Reposo (nada en
            //     círculos alrededor del dueño — cada cría con SU órbita). ===
            float deseado;
            float vel;
            if (presa != null)
            {
                deseado = (presa.Center - _segs[0]).ToRotation();
                vel = VelCaza;
            }
            else
            {
                // EL SLOT DE LA CAMADA: la i-ésima cría nada en su propio
                // ángulo de la órbita (las crías no se montan encima).
                int idx = IndiceEnCamada();
                int total = Math.Max(1, duenio.ownedProjectileCounts[Type]);
                float angSlot = (idx / (float)total) * MathHelper.TwoPi;
                float radio = Radm + 14f * VFXCore.Hash01(Seed, 31, 97);
                Vector2 ancla = duenio.Center + new Vector2(
                    MathF.Cos(angSlot + time * 0.30f), MathF.Sin(angSlot + time * 0.30f)) * radio;

                // El punto TANGENTE de la órbita (la MISMA ley de la madre).
                Vector2 alAncla = _segs[0] - ancla;
                float distA = alAncla.Length();
                if (distA > Radm * 2.0f && distA > 1f)
                    deseado = (ancla - _segs[0]).ToRotation();
                else if (distA > 1f)
                    deseado = alAncla.ToRotation() + MathHelper.PiOver2;
                else
                    deseado = _rumbo;
                vel = VelReposo;
            }

            float delta = MathHelper.WrapAngle(deseado - _rumbo);
            _rumbo += MathHelper.Clamp(delta, -0.09f, 0.09f);

            // El vaivén del cachorro (más juguetón que el de la madre).
            float surge = 1f + 0.30f * MathF.Sin(time * MathHelper.TwoPi * 1.4f + Seed);
            Vector2 velocidad = new Vector2(MathF.Cos(_rumbo), MathF.Sin(_rumbo)) * (vel * surge);
            _segs[0] += velocidad;

            // === LA CADENITA (la MISMA ley — la herencia). ===
            for (int i = 1; i < N; i++)
            {
                Vector2 d = _segs[i] - _segs[i - 1];
                float dist = d.Length();
                if (dist < 0.0001f) { d = Vector2.UnitX; dist = 1f; }
                _segs[i] = _segs[i - 1] + d / dist * Tamano;
            }

            Projectile.Center = _segs[0];
            Projectile.velocity = velocidad;
            Projectile.rotation = _rumbo;

            // La luz fría-estelar de la cría.
            Lighting.AddLight(Projectile.Center, 0.16f, 0.16f, 0.11f);

            // === LA ESPINITA QUEMA (cada 12 ticks — ×0.4, la firma de la
            //     casa: la cabeza muerde por contacto de minion). ===
            if (_age > 8 && _age % CadaEspina == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoEspina));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    for (int i = 1; i < N; i++)
                    {
                        float r = MathHelper.Lerp(12f, 5f, i / (float)(N - 1)) + 6f;
                        if (Vector2.DistanceSquared(npc.Center, _segs[i]) > r * r)
                            continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.2f, true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// EL SOSTÉN DEL BUFF (patrón de la medusa nebulosa): si el buff
        /// de la cría no vive, la cría se disuelve; si vive, se refresca
        /// y el timeLeft nunca expira.
        /// </summary>
        private void CheckMinionBuff(Player duenio)
        {
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.CriaEstelarBuff>();
            bool tieneBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (duenio.buffType[i] == buffType && duenio.buffTime[i] > 0)
                {
                    tieneBuff = true;
                    break;
                }
            }
            if (!tieneBuff) Projectile.Kill();
            else
            {
                duenio.AddBuff(buffType, 18000);
                Projectile.timeLeft = 2;
            }
        }

        /// <summary>El índice de esta cría dentro de la camada (por whoAmI).</summary>
        private int IndiceEnCamada()
        {
            int idx = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == Type)
                {
                    if (i == Projectile.whoAmI) return idx;
                    if (i < Projectile.whoAmI) idx++;
                }
            }
            return idx;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // La cría se disuelve en polvo de estrellas.
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(i * MathHelper.TwoPi / 14),
                        MathF.Sin(i * MathHelper.TwoPi / 14)) * 2.2f,
                    180, new Color(168, 224, 255), 0.45f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                SierpesLib.CriaEstelar(_segs, Angulos(), N,
                    Main.GlobalTimeWrappedHourly, Seed, 1f);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El rumbo de cada segmentito (para el render).</summary>
        private float[] Angulos()
        {
            for (int i = 0; i < N; i++)
                _angulos[i] = _rumbo;
            return _angulos;
        }

        private readonly Vector2[] _segs = new Vector2[N];
        private readonly float[] _angulos = new float[N];
        private NPC _presaFija;
        private bool _sembrado;   // v6.50.3 — la autocuración MP
        private float _rumbo;
        private float _age;

        private int Seed { get; set; }
    }
}
