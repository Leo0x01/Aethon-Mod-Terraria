using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Grimorio
{
    // ======================================================================
    //  LA PALABRA CORTADA — v6.158 — EL ATAQUE BÁSICO DEL GRIMORIO: LOS
    //  TAJOS DE LA PALABRA VIVA.
    //
    //  LA LETRA: «investiga un arma epica para que sea el ataque basico
    //  del grimorio, quiero tajos, pero los tajos no los haces bien,
    //  asi que investiga y crea un arma nueva».
    //
    //  LA INVESTIGACIÓN: los tajos épicos del ecosistema (los swinging
    //  swords de 1.4.4, las blade works de los mods grandes) son UN
    //  ARCO que NACE pegado al dueño, BARRE un sector y MUERE — no un
    //  proyectil que viaja. El arco de la casa: LA PALABRA CORTADA —
    //  el libro abre una página y de ella sale LA PALABRA, un glifo
    //  rúnico GIGANTE que se materializa en el aire y BARRA el sector
    //  frente al jugador (el tajo de la palabra que corta):
    //
    //   · NACE a 30° de la mira, BARRE 90° hasta el otro lado (el
    //     sector entero del golpe, de izquierda a derecha — o al
    //     revés si miras a la izquierda: SIEMPRE hacia delante);
    //   · EL FILO: la CUERDA del tajo — la banda entre el jugador y
    //     el radio del arco, EL GLIFO dibujado con la tinta del
    //     codex (la runa g del abecedario de la casa, la misma de
    //     las coronas: OrbitaLib.Glifo) GIGANTE, rotando con el
    //     barrido — la palabra que se escribe cortando;
    //   · LA ESTELA: el arco ya barrido queda un instante DIBUJADO
    //     (el rastro del tajo — arcos finos violeta-dorados con
    //     falloff, los pinceles de OndaLib/SombrasLib);
    //   · EL GOLPE: todo NPC dentro del SECTOR en el frame del
    //     barrido recibe el daño UNA vez (la banda de FormaLib en
    //     movimiento — cada frame el filo es un segmento del arco);
    //   · ESCALADO por nivel del libro: +radio, +daño (el daño lo
    //     lleva el item), +glifos del filo (1 → 3 → 5), y a niveles
    //     altos LA PALABRA SE REPITE: dos tajos cruzados.
    //
    //  TODO por código (la ley de la Sombra de Página): cero sprites.
    // ======================================================================
    public class PalabraCortadaProjectile : ModProjectile
    {
        // La textura FANTASMA de los proyectiles 100%-código (el patrón
        // de la casa: tML exige el asset aunque nunca se dibuje).
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        // EL COMPÁS DEL TAJO: nace a ±45° de la mira y barre 90°.
        private const float APERTURA = MathF.PI / 4f;    // 45°
        private const float BARRIDO = MathF.PI / 2f;     // 90°

        private float _edad;
        private float _giroBase;      // la dirección de la mira al nacer
        private int _sentido;         // +1 barre en sentido horario
        private float _radio;         // el alcance del tajo (px)
        private int _glifos;          // cuántas letras del filo
        private int _duplicado;       // 0 = el tajo · 1 = el cruce (el segundo)
        private readonly int[] _golpeados = new int[200];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;         // el tajo cruza TODO
            Projectile.timeLeft = 26;          // 16 de barrido + 10 de estela
            Projectile.tileCollide = false;    // la palabra atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;   // cada enemigo UNA vez
            Projectile.localNPCHitCooldown = -1;
            Projectile.netImportant = true;
        }

        /// <summary>EL TAJO NACE: la letra del compás (lo llama el
        /// grimorio al disparar — el sentido del barrido ALTERNA con
        /// cada disparo, como el vaivén de una espada de verdad).</summary>
        public void Vestir(int nivel, float direccionMira, int sentido, int duplicado)
        {
            _giroBase = direccionMira;
            _sentido = sentido;
            _duplicado = duplicado;
            // EL ALCANCE crece con el nivel del libro (90 px base →
            // 230 px a nivel 1000 — la palabra se alarga al despertar).
            _radio = 90f + 14f * MathF.Sqrt(Math.Max(0, nivel));
            // LAS LETRAS del filo: 1 → 2 (nv 20) → 3 (nv 50) → 4 (nv 100)
            // → 5 (nv 200) — la palabra gana sílabas.
            _glifos = 1 + (nivel >= 20 ? 1 : 0) + (nivel >= 50 ? 1 : 0) +
                      (nivel >= 100 ? 1 : 0) + (nivel >= 200 ? 1 : 0);
            if (_duplicado == 1)
            {
                // EL CRUCE: el segundo tajo barre al revés, con retardo —
                // nace media palabra después.
                _giroBase = direccionMira + (sentido > 0 ? -APERTURA * 2f : APERTURA * 2f);
                _sentido = -sentido;
            }
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            _edad++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            // EL TAJO VIVE PEGADO AL DUEÑO (es su brazo que corta).
            Projectile.Center = duenio.MountedCenter;
            Projectile.velocity = Vector2.Zero;

            // LA VENTANA ACTIVA del barrido: 16 ticks de barrido (el
            // cruce empieza 6 ticks después — la palabra cruzada).
            float inicio = _duplicado == 1 ? 6f : 0f;
            float tB = _edad - inicio;
            bool activo = tB >= 0f && tB <= 16f;

            if (activo)
            {
                // EL FILO de ESTE frame: el segmento del arco que la
                // palabra está cortando AHORA.
                float p = tB / 16f;                          // 0..1
                float ang = _giroBase + _sentido * (-APERTURA + BARRIDO * p);
                Vector2 dirFilo = new Vector2(MathF.Cos(ang), MathF.Sin(ang));

                // LA COSECHA DEL FILO: todo NPC en la BANDA del segmento
                // actual (del dueño al radio, con el grosor del filo).
                float grosor = 26f + _radio * 0.10f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.friendly || n.townNPC) continue;
                    if (Array.IndexOf(_golpeados, n.whoAmI) >= 0) continue;
                    float dist = Vector2.Distance(n.Center, Projectile.Center);
                    if (dist > _radio + n.width) continue;
                    // ¿está en el SECTOR del filo de este frame?
                    Vector2 hacia = n.Center - Projectile.Center;
                    if (hacia.Length() < 4f) continue;
                    float angNpc = MathF.Atan2(hacia.Y, hacia.X);
                    float dAng = Math.Abs(MathHelper.WrapAngle(angNpc - ang));
                    if (dAng <= 0.42f + grosor / Math.Max(20f, dist))
                    {
                        _golpeados[Array.IndexOf(_golpeados, 0)] = n.whoAmI;
                        // EL GOLPE (el daño lo trae el proyectil — el
                        // del arma, ya escalado por el nivel del libro).
                        var golpe = n.CalculateHitInfo(Projectile.damage, Projectile.owner);
                        golpe.Knockback = 3.5f * duenio.direction;
                        n.StrikeNPC(golpe);
                        if (Main.netMode == NetmodeID.Server)
                            Terraria.NetMessage.SendStrikeNPC(n, in golpe);
                        // LA CHISPA DEL CORTE: el brillo dorado en la
                        // herida de la palabra.
                        if (!Main.dedServ)
                            for (int k = 0; k < 6; k++)
                            {
                                var d = Dust.NewDustPerfect(n.Center, DustID.GoldFlame,
                                    -Vector2.UnitY.RotatedByRandom(1.2f) *
                                    Main.rand.NextFloat(1f, 3f), 150,
                                    new Color(255, 220, 140), 0.7f);
                                d.noGravity = true;
                            }
                    }
                }

                // LA LUZ DEL FILO (la palabra brilla al cortar).
                Lighting.AddLight(Projectile.Center + dirFilo * (_radio * 0.5f),
                    0.45f, 0.34f, 0.14f);

                // EL SONIDO DEL TAJO (una sola vez, al nacer el filo).
                if (tB <= 0.5f && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item60.WithPitchOffset(
                            _duplicado == 1 ? 0.25f : 0f).WithVolumeScale(0.55f),
                        Projectile.Center);
                }
            }
        }
        // ================================================================
        //  v6.164 — EL TAJO DE TINTA DE VERDAD (la letra: «el ataque
        //  basico del grimorio sigue siendo circulos planos y feos,
        //  asi que investiga profundamente porque el ataque basico
        //  siguen siendo lineas de circulos planos blancos y sin
        //  estilo vfx ni nada»). LA AUTOPSIA PROFUNDA: el culpable NO
        //  eran las cintas — era LA PASADA (3): los «glifos del filo»
        //  llamaban a OrbitaLib.RunaVacia por cada letra, y CADA runa
        //  dibuja un resplandor RADIAL (Quad Glow 36×36) + su PERLA
        //  blanca — una FILA de círculos planos blancos a lo largo
        //  del corte. Encima, las cintas usaban VFXCore.Carne (la
        //  textura de carne CARMESÍ del agujero — nunca fue tinta) y
        //  el RibbonTinted crudo, SIN las 3 capas de la casa. LA
        //  CURA RAÍZ — se rehace con ESTELALIB.RIBBON, LA técnica de
        //  tinta de la Grieta de Tinta (el lenguaje visual del
        //  hambre): banda de GROSOR VIVO con perfil, suavizado y
        //  TRES CAPAS por tramo (velo ×1.6 · cuerpo · núcleo blanco
        //  ×0.30 — la doble pasada de la casa en versión triple) +
        //  cabeza con bloom. EL CONCEPTO SIGUE (la Escritura del
        //  Corte): el libro ESCRIBE el tajo — la pluma corre por el
        //  arco y la estela de tinta QUEDA ESCRITA detrás; los
        //  glifos radiales murieron para siempre.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active) return false;

            float inicio = _duplicado == 1 ? 6f : 0f;
            float tB = _edad - inicio;
            if (tB < -0.5f) return false;   // el cruce aún no nace

            // EL PROGRESO del barrido (0..1) con easing (rápido al
            // principio, el golpe se asienta — la mecánica del filo).
            float p = MathHelper.Clamp(tB / 16f, 0f, 1f);
            float pE = 1f - MathF.Pow(1f - p, 1.6f);
            float ang = _giroBase + _sentido * (-APERTURA + BARRIDO * pE);

            // LA ESTELA se disuelve tras el barrido (10 ticks de gloria).
            float alfa = tB <= 16f ? 1f : Math.Max(0f, 1f - (tB - 16f) / 10f);
            if (alfa <= 0.02f) return false;

            float t = Main.GlobalTimeWrappedHourly;
            Vector2 centro = Projectile.Center - Main.screenPosition;
            Color tinta = new Color(126, 62, 200);        // la tinta del codex
            Color oro = new Color(255, 208, 120);         // el filo de oro

            // ============================================================
            //  (1) EL ARCO ESCRITO — la estela de tinta que QUEDÓ:
            //  EstelaLib.Ribbon sobre la trayectoria YA barrida (del
            //  arranque al filo actual), con EL PERFIL CALIGRÁFICO:
            //  nace fina (la punta de la pluma), ENGORDA en el medio
            //  (la carga del trazo) y afila al final. El camino lleva
            //  la ONDULACIÓN viva del pulso de la mano (senoidal) —
            //  la tinta respira, no es un arco geométrico.
            // ============================================================
            // EL ÁNGULO DE ARRANQUE del trazo (donde la pluma aterrizó).
            float angArranque = _giroBase + _sentido * -APERTURA;

            // v6.165 — EL LOTE DE LA GRIETA (la cura del error del
            // ataque básico): el ribbon NO vive en el lote de VFXCore
            // (Begin/FlushAdditive son el buffer de QUADS propio — ni
            // abren ni cierran el spriteBatch). El patrón de la casa
            // (GrietaDeTinta): cerrar el lote vivo SI HAY UNO,
            // abrir el spriteBatch en ADITIVO INMEDIATO y End al
            // terminar — las estelas se dibujan donde deben, con el
            // blend de la tinta.
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                int nPuntos = 22;
                var camino = new Vector2[nPuntos];
                for (int i = 0; i < nPuntos; i++)
                {
                    float f = i / (float)(nPuntos - 1);       // 0 raíz → 1 filo
                    float aP = angArranque + _sentido * BARRIDO * pE * f;
                    // EL PULSO DE LA MANO: la ondulación senoidal que
                    // hace del arco un TRAZO de pluma (2 medias ondas
                    // por tajo, desfasadas por tajo — cada palabra es
                    // distinta).
                    float ondula = MathF.Sin(f * MathF.PI * 1.9f +
                        Projectile.whoAmI * 0.7f) * (5f + _radio * 0.045f)
                        + MathF.Sin(t * 3.1f + f * 5f) * 2.2f;
                    Vector2 dirP = new Vector2(MathF.Cos(aP), MathF.Sin(aP));
                    camino[i] = centro + dirP * _radio +
                        new Vector2(-dirP.Y, dirP.X) * ondula;
                }
                // pts[ÚLTIMO] = CABEZA (el filo que avanza) — el contrato
                // de EstelaLib: la cabeza es donde la pluma va AHORA.
                EstelaLib.Ribbon(Main.spriteBatch, camino, 15f + _radio * 0.08f,
                    EstelaProfile.Alive, tinta, 0.85f * alfa,
                    Projectile.whoAmI, t, head: true, taper: 0.55f);

                // ========================================================
                //  (2) EL FILO — LA SEGUNDA ESCRITURA: una estela
                //  MENOR (la mitad de ancho, perfil Lanza) en ORO que
                //  sigue a la pluma por DENTRO del arco (radio·0.86):
                //  el doble trazo de la casa (tinta + filo) — el corte
                //  es lo que la pluma ESCRIBE, la tinta es la palabra.
                // ========================================================
                var caminoFilo = new Vector2[nPuntos];
                for (int i = 0; i < nPuntos; i++)
                {
                    float f = i / (float)(nPuntos - 1);
                    float aP = angArranque + _sentido * BARRIDO * pE * f;
                    float ondula = MathF.Sin(f * MathF.PI * 1.9f +
                        Projectile.whoAmI * 0.7f) * (4f + _radio * 0.035f);
                    Vector2 dirP = new Vector2(MathF.Cos(aP), MathF.Sin(aP));
                    caminoFilo[i] = centro + dirP * (_radio * 0.86f) +
                        new Vector2(-dirP.Y, dirP.X) * ondula;
                }
                EstelaLib.Ribbon(Main.spriteBatch, caminoFilo,
                    8f + _radio * 0.05f, EstelaProfile.Comet, oro,
                    0.75f * alfa, Projectile.whoAmI + 7, t, head: true, taper: 0.7f);

                // LAS GOTAS viven en el MISMO lote aditivo (la tinta
                // es una sola): se dibujan tras las estelas, dentro del
                // mismo Begin…End — sin reabrir nada.
                if (tB > 13f)
                {
                    float tension = MathHelper.Clamp((tB - 13f) / 5f, 0f, 1f);
                    for (int k = 0; k < 6; k++)
                    {
                        float fa = (k + 0.5f) / 6f;
                        float aD = angArranque + _sentido * BARRIDO * fa;
                        Vector2 dirD = new Vector2(MathF.Cos(aD), MathF.Sin(aD));
                        Vector2 baseG = centro + dirD * (_radio * (0.92f + 0.06f *
                            MathF.Sin(t * 4f + k * 2.4f)));
                        // CADA GOTA: un hilo corto de tinta que salta
                        // hacia afuera y se disuelve — vida fractal.
                        float vida = SombrasLib.Frac(t * 1.6f + k * 0.37f);
                        Vector2 salto = new Vector2(-dirD.Y, dirD.X) *
                            (6f + 16f * vida) * _sentido;
                        var hilo = new Vector2[]
                        {
                            baseG - salto * 0.30f,
                            baseG + salto * 0.25f,
                            baseG + salto,
                        };
                        EstelaLib.Ribbon(Main.spriteBatch, hilo,
                            5f * (1f - vida) + 1.5f, EstelaProfile.Alive,
                            k % 2 == 0 ? tinta : oro,
                            alfa * tension * (1f - vida) * 0.8f,
                            Projectile.whoAmI * 13 + k, t, head: false, taper: 0.9f);
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PalabraCortadaProjectile", exRegistro); }
            // EL CIERRE DEL LOTE ADITIVO + la vuelta al lote del juego
            // (el patrón de la Grieta: End SIEMPRE, y Reabrir para que
            // el resto del frame dibuje normal).
            try { Main.spriteBatch.End(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PalabraCortadaProjectile", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
