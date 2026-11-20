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

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center - Main.screenPosition;
                Color tinta = new Color(120, 66, 190);       // la tinta del codex
                Color oro = new Color(255, 208, 120);        // el filo de oro

                // === (1) LA ESTELA DEL ARCO: los arcos ya barridos ===
                int pasos = 14;
                for (int i = 0; i < pasos; i++)
                {
                    float pp = i / (float)(pasos - 1) * pE;   // 0..pE (lo barrido)
                    float aA = _giroBase + _sentido * (-APERTURA + BARRIDO * pp);
                    float alfaArco = alfa * (0.28f + 0.55f * pp);  // más vivo al final
                    Vector2 dirA = new Vector2(MathF.Cos(aA), MathF.Sin(aA));
                    // EL ARCO FINO: un quad largo y delgado en el radio.
                    Vector2 medio = centro + dirA * (_radio * 0.62f);
                    VFXCore.Quad(medio,
                        tinta * alfaArco,
                        new Vector2(_radio * 0.55f, 3.5f + 2.5f * (1f - pp)),
                        aA);
                }

                // === (2) EL FILO: la palabra en el ángulo actual ===
                Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                // EL BORDE EXTERIOR (el corte que abre): la línea larga.
                VFXCore.Quad(centro + dir * (_radio * 0.55f),
                    Color.White * (0.75f * alfa),
                    new Vector2(_radio * 0.92f, 6.5f), ang);
                VFXCore.Quad(centro + dir * (_radio * 0.52f),
                    oro * (0.55f * alfa),
                    new Vector2(_radio * 0.86f, 13f), ang);

                VFXCore.FlushAdditive(null, false);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            // v6.159 — LA CURA DEL LOG (la excepción silenciosa: «Draw
            // was called, but Begin has not yet been called» desde
            // OrbitaLib.RunaVacia ← PalabraCortadaProjectile.PreDraw):
            // RunaVacia dibuja DIRECTO al spriteBatch de vanilla — y
            // este PreDraw corría con el lote de vanilla CERRADO por
            // CerrarLoteSiAbierto (el contrato de la casa: las runas
            // viven en el lote ABIERTO del llamador). LA CURA: la
            // pasada de los glifos va DESPUÉS de cerrar el lote del
            // tajo, con el lote de vanilla REABIERTO (ReabrirLote-
            // Vanilla) — la MISMA secuencia que usan las coronas de
            // OrbitaLib en todos los renders de la casa.
            VFXCore.ReabrirLoteVanilla();

            // === (3) LOS GLIFOS DEL FILO: las letras de la palabra ===
            // (pasada propia, lote vanilla abierto — el contrato de
            // OrbitaLib.RunaVacia: dibuja en el lote de SU llamador)
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                Color tinta = new Color(120, 66, 190);
                Color oro = new Color(255, 208, 120);
                for (int g = 0; g < _glifos; g++)
                {
                    float fr = 0.30f + 0.62f * (g + 0.5f) / _glifos;
                    Vector2 posG = Projectile.Center + dir * (_radio * fr) -
                        Main.screenPosition;
                    float gs = 0.85f + 0.30f * (_glifos - 1);   // el calibre de la letra
                    int letra = (g * 3 + Projectile.whoAmI) % OrbitaLib.RunasVacio.Length;
                    Color cuerpo = Color.Lerp(oro, tinta, g / (float)Math.Max(1, _glifos - 1));
                    OrbitaLib.RunaVacia(posG, t * 0.9f + g * 2.1f, g,
                        OrbitaLib.RunasVacio[letra],
                        cuerpo, Color.White * 0.65f, gs, 0.85f * alfa);
                }
            }
            catch { }
            return false;
        }
    }
}
