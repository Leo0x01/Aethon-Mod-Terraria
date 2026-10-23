using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AethonMenorPet — v6.50.49 — EL AETHON MENOR (la mascota de luz) ·
    /// v6.50.53 — EL JEFE EXACTO EN MINIATURA: «hazlo que sea exactamente
    /// el jefe pero mas pequeño» — LAS SEIS SECCIONES DEL SOL DE CÓDIGO
    /// del PreDraw del jefe (velo violeta, halo dorado, rueda de rayos,
    /// dos coronas de perlas, chispas orbitantes y núcleo blanco con
    /// corazón dorado) a la ESCALA 0.22.
    /// v6.50.55 — LA MINI-EXPLOSIÓN: «ya que la mascota seria aburrida si
    /// fuera un punto de luz constante, has que tenga una pequeña
    /// animacion, de vez en cuando y de forma aleatoria» — la mascota
    /// hace LA MISMA explosión de luz del jefe original, pero MUY
    /// PEQUEÑA (el MiniEstallidoPet: ~0.13× del estallido). LO PRIMERO
    /// que hace al ser invocada es el SALUDO DE LUZ (a los 45 t), y
    /// luego el compás aleatorio: de base ~1 vez cada 20 min, cada
    /// enemigo cerca del dueño SUBE la probabilidad (la animación
    /// «aumenta la probabilidad de hacerse cuantos mas enemigos haya
    /// en pantalla»), y EL GARANTE: a la hora exacta (216000 t) sale
    /// sí o sí — «al menos 1 vez cada hora».
    ///
    /// v6.50.122 — LA MASCOTA DE LUZ, PURA (la letra del usuario que
    /// desenredó los conceptos: «la mascota de luz es una representación
    /// minúscula del jefe… la mascota de luz solo debe iluminar y
    /// explotar en luz como el jefe, además de fusionarse al igual que
    /// el jefe que es de luz»): EL JEFE EN MINIATURA Y NADA MÁS —
    /// ALUMBRA, ESTALLA su mini explosión y vive su PAREJA con el gemelo
    /// oscuro (AethonMenorOscuroPet lo orbita; FUSIONADOS en los
    /// equinoccios del mundo — amanecer/crepúsculo: Main.time &lt; 1500 o
    /// &gt; 52500 — separados el resto del día; el claro es el líder).
    /// LA GRAN LIMPIEZA: la máquina de travesuras (el AFK que viajaba al
    /// pueblo, la sombrilla que mordía, los 3 mini ojos que embistían,
    /// el sol rúnico) NUNCA fue de la criatura de luz — es el repertorio
    /// del GRIMORIO: TODO ese cerebro se mudó al GRIMORIO MENOR
    /// (GrimorioMenorPet, la OTRA mascota: la miniatura del libro). La
    /// luz no molesta a nadie: solo alumbra.
    /// EL PATRÓN DE MASCOTA DE LUZ de vanilla (verificado contra el
    /// tML real): Main.projPet (persiste) + ProjectileID.Sets.LightPet
    /// (la familia del fuego fatuo — no la matan las mascotas ajenas) +
    /// NeedsUUID (la instancia ÚNICA por jugador). La IA es la
    /// persecución suave de las mascotas de vanilla: orbita el hombro
    /// del dueño con su vaivén, sin prisa.
    /// </summary>
    public class AethonMenorPet : ModProjectile
    {
        // === v6.50.55 — EL COMPÁS DE LA MINI-EXPLOSIÓN ===
        private float _tDesdeMini;   // ticks desde el último destello (o el nacimiento)
        private bool _saludoHecho;   // LO PRIMERO que hace la mascota ES la luz
        private bool _miniCerca;     // su propio destello está vivo a su lado (sincronizado: TODAS las pantallas lo ven igual)

        // === v6.51.10 — EL VUELO VIVO (la letra: «ademas creo que
        //     deberias mejorar las animaciones de (Aethon Menor)»):
        //     el sol en miniatura ya no flota tieso — VUELA. ===
        // · LOS CAPRICHOS (ai[0]): de vez en cuando el dueño decide un
        //   capricho y lo sincroniza (netUpdate — todas las pantallas
        //   corren LA MISMA coreografía): 1 EL RIZO (una vuelta
        //   completa alrededor del dueño) · 2 EL PASEO (dispara a un
        //   punto y vuelve) · 3 LA PIROUETA (espiral apretada). ai[1]
        //   cuenta atrás; ai[2] es la semilla (el ángulo del capricho).
        // · LA EMPATÍA (leída del estado SINCRONIZADO del dueño — sin
        //   paquetes nuevos, todas las pantallas de acuerdo): si el
        //   dueño ACABA DE SUFRIR DAÑO, la cría SE ABRAZA a él y su luz
        //   se apaga un poco (preocupación); si está PELEANDO, la cría
        //   ARDE más y su rueda se acelera (ánimo).
        // · LA INCLINACIÓN Y EL ESTIRO: el dibujo se inclina con la
        //   velocidad del vuelo y respira MÁS GRANDE cuando corre
        //   (PreDraw)… y EL RASTRO: un rastro de corazones dorados
        //   cuando vuela rápido.
        private bool _duenioHerido;    // la empatía: el dueño acaba de sufrir daño
        private bool _duenioPeleando;  // la empatía: el dueño está usando un arma
        private readonly Vector2[] _rastro = new Vector2[6];   // las posiciones recientes (EL RASTRO)
        private int _tickRastro;

        // v6.50.122 — SIN CEREBRO DE TRAVESURAS: el registro del pueblo
        // (TicksMolestado) y toda la máquina de molestar NPCs viven AHORA
        // en ParejaCerebro, y su huésped es el GRIMORIO MENOR — la luz no
        // molesta a nadie.

        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            ProjectileID.Sets.LightPet[Type] = true;      // MASCOTA DE LUZ (familia del fuego fatuo)
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;         // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la luz atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            // v6.50.121 — LA CURA DEL «LA MASCOTA SE DESACTIVA DESPUÉS DE
            //     UN TIEMPO»: el patrón vanilla de TODA mascota (Wisp,
            //     Corazón Carmesí, Fuego Fatuo…): el reloj del proyectil
            //     se refresca A CADA TICK — la muerte la decide SOLO el
            //     buff (el contrato de arriba), jamás el timeLeft. Sin
            //     este refresco, el reloj por defecto del proyectil
            //     expiraba solo y la cría se apagaba a los pocos minutos
            //     de invocarla (el buff moría con ella: ni rastro).
            Projectile.timeLeft = 2;

            float t = Main.GlobalTimeWrappedHourly;

            // === v6.51.10 — LA EMPATÍA (leída del estado sincronizado del
            //     dueño — sin paquetes, todas las pantallas iguales): la
            //     cría de luz SIENTE a su portador. ===
            _duenioHerido = duenio.immuneTime > 0;
            _duenioPeleando = duenio.itemAnimation > 0 && duenio.HeldItem != null &&
                !duenio.HeldItem.IsAir && duenio.HeldItem.damage > 0;

            // === v6.51.10 — LOS CAPRICHOS (el vuelo vivo): el dueño
            //     decide CUÁNDO y CUÁL, y el cable lo cuenta a todos.
            //     (Después del saludo de luz — primero se presenta, luego
            //     juega.) ===
            if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == 0f &&
                _saludoHecho && Main.rand.NextBool(1800))   // ~un capricho cada 30 s
            {
                Projectile.ai[0] = Main.rand.Next(1, 4);   // 1 EL RIZO · 2 EL PASEO · 3 LA PIROUETA
                Projectile.ai[1] = Projectile.ai[0] == 1f ? 130f :
                    (Projectile.ai[0] == 2f ? 110f : 95f);
                Projectile.ai[2] = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[1] -= 1f;
                if (Projectile.ai[1] <= 0f)
                {
                    Projectile.ai[0] = 0f;
                    if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
                }
            }

            // === LA PERSECUCIÓN SUAVE (el vaivén de las mascotas de
            //     vanilla): orbita el hombro del dueño — el lado cambia
            //     LENTO con el seno (pasea de un hombro al otro) y sube
            //     y baja con su propio compás. v6.50.122 — SIN MISIONES:
            //     la criatura de luz ya no viaja a ningún pueblo (esa
            //     máquina es del Grimorio Menor) — su repertorio es la
            //     luz, el estallido y la pareja, nada más.
            //     v6.51.10 — Y SUS CAPRICHOS: cuando juega, la META es la
            //     coreografía (y persigue con MÁS GANAS). ===
            float lado = MathF.Sin(t * 0.35f) * 46f - duenio.direction * 14f;
            float bob = MathF.Sin(t * 1.9f) * 9f;
            Vector2 meta = duenio.Center + new Vector2(lado, -46f + bob);

            // LA EMPATÍA MUEVE LA META: dueño herido → la cría SE ABRAZA
            // a él (la posición de un consuelo, encima de la cabeza).
            if (_duenioHerido)
                meta = duenio.Center + new Vector2(lado * 0.25f, -30f);

            // EL CAPRICHO MANDA: su coreografía reemplaza la meta.
            if (Projectile.ai[0] == 1f)          // EL RIZO: una vuelta alrededor del dueño
            {
                float ang = Projectile.ai[2] + (1f - Projectile.ai[1] / 130f) * MathHelper.TwoPi;
                meta = duenio.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.72f) * 88f;
            }
            else if (Projectile.ai[0] == 2f)     // EL PASEO: dispara a un punto… y vuelve
            {
                float fase = 1f - Projectile.ai[1] / 110f;
                Vector2 destino = duenio.Center + new Vector2(
                    MathF.Cos(Projectile.ai[2]), MathF.Sin(Projectile.ai[2])) * 150f;
                if (fase < 0.45f) meta = destino;
                else meta = duenio.Center + new Vector2(0f, -44f);
            }
            else if (Projectile.ai[0] == 3f)     // LA PIROUETA: espiral apretada
            {
                float prog = 1f - Projectile.ai[1] / 95f;
                float ang = Projectile.ai[2] + prog * 12f;
                float radio = 42f * (1f - 0.55f * MathF.Sin(prog * MathHelper.Pi));
                meta = duenio.Center + new Vector2(-duenio.direction * 28f, -46f) +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
            }

            // La búsqueda serena: nunca teletransporta, siempre alcanza.
            // (v6.51.10 — jugando o consolando persigue con MÁS PRISA.)
            float prisa = (Projectile.ai[0] > 0f || _duenioHerido) ? 1.8f : 1f;
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (dist > 900f)            // el dueño se fue muy lejos: le alcanza volando
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    hacia * 0.045f * prisa, 0.10f * prisa);

            // El freno al llegar (flota, no vibra encima del punto).
            if (dist < 24f)
                Projectile.velocity *= 0.80f;

            // === v6.51.10 — EL RASTRO (las posiciones recientes para el
            //     rastro de luz cuando corre) ===
            if (++_tickRastro >= 3)
            {
                _tickRastro = 0;
                for (int r = _rastro.Length - 1; r > 0; r--)
                    _rastro[r] = _rastro[r - 1];
                _rastro[0] = Projectile.Center;
            }

            Projectile.rotation += 0.02f;   // el núcleo gira despacito (el sol en miniatura)

            // === LA LUZ DE MUNDO (v6.50.53 — exactamente el JEFE: su luz
            //     dorada-blanca (1.55, 1.35, 0.95) a escala de mascota —
            //     el prisma rotativo murió: el jefe NO es arcoíris, es
            //     ORO Y NÚCLEO BLANCO, y la mascota es SU retrato).
            //     v6.50.56 — LA LETRA: «la mascota debe iluminar un poco
            //     mas el entorno, tiene poca iluminacion» — ×1.6: la
            //     criatura de luz ALUMBRA de verdad (1.35/1.15/0.78 —
            //     el rango de un antorcha solar).
            //     v6.51.10 — LA EMPATÍA TAMBIÉN ALUMBRÍA: con el dueño
            //     herido la luz se ENCOGE (está asustada); peleando,
            //     ARDE (le entra el fervor). ===
            float luzAnimo = _duenioHerido ? 0.75f : (_duenioPeleando ? 1.20f : 1f);
            Lighting.AddLight(Projectile.Center, 1.35f * luzAnimo, 1.15f * luzAnimo, 0.78f * luzAnimo);

            // === v6.50.55 — LA MINI-EXPLOSIÓN (el compás). Solo el cliente
            //     del dueño decide CUÁNDO (Main.rand del dueño, cero azar
            //     sincronizado): el destello nace por el cable y TODAS las
            //     pantallas ven la MISMA luz ===
            if (Projectile.owner == Main.myPlayer)
            {
                _tDesdeMini++;
                // LO PRIMERO que hace la mascota ES la luz: el saludo a los
                // 45 t de vida (llegó, se asentó... y ARDIÓ).
                bool toca = !_saludoHecho && _tDesdeMini >= 45f;
                if (!toca && _saludoHecho && _tDesdeMini > 600f)
                {
                    // LA COMPAÑÍA SUBE EL PULSO: cada enemigo cerca del dueño
                    // (a 1300 px — «en pantalla») suma su granito.
                    int enemigos = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n != null && n.active && !n.friendly && !n.townNPC &&
                            n.lifeMax > 5 &&
                            Vector2.DistanceSquared(n.Center, duenio.Center) < 1300f * 1300f)
                            enemigos++;
                    }
                    // EL RANURAZO: de base ~1 vez cada 20 min (1/72000 por
                    // tick), cada enemigo acelera (con 10 ≈ 1 vez cada 90 s,
                    // tope 20) — y EL GARANTE: a la hora exacta (216000 t)
                    // sale sí o sí («al menos 1 vez cada hora»).
                    float p = 1f / 72000f + Math.Min(enemigos, 20) * (1f / 60000f);
                    if (_tDesdeMini >= 216000f || Main.rand.NextFloat() < p)
                        toca = true;
                }
                if (toca)
                {
                    _saludoHecho = true;
                    _tDesdeMini = 0f;
                    // LA SEMILLA viaja en ai[0] (cada pantalla dibuja SU
                    // mismo estallido — Hash01, el patrón de la .53) y EL
                    // WHOAMI DE LA MASCOTA viaja en ai[1]: el destello le
                    // PEGA el centro a su fuente mientras viva (v6.50.56 —
                    // «la explocion debe seguir a la mascota no quedarse
                    // atras» — el patrón de los cabalgadores de la .54).
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<MiniEstallidoPet>(),
                        15, 1.5f, Main.myPlayer,
                        Main.rand.Next(1, 999999), Projectile.whoAmI, 0f);
                }
            }

            // === LA PROXIMIDAD DEL PROPIO DESTELLO (determinista en todas
            //     las pantallas: lee el proyectil SINCRONIZADO) — para
            //     engordar su brillo mientras carga y su goteo de chispas ===
            _miniCerca = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active &&
                    pr.type == ModContent.ProjectileType<MiniEstallidoPet>() &&
                    Vector2.DistanceSquared(pr.Center, Projectile.Center) < 56f * 56f)
                {
                    _miniCerca = true;
                    break;
                }
            }

            // === LAS CHISPAS (el rastro de la criatura de luz — y cuatriplica
            //     el goteo mientras carga su mini-estallido) ===
            if (!Main.dedServ && Main.rand.NextBool(_miniCerca ? 4 : 14))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                        Main.rand.NextFloat(-8f, 8f)),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                        Main.rand.NextFloat(-0.8f, -0.2f)),
                    170, new Color(255, 244, 200), 0.8f);
                d.noGravity = true;
            }
        }

        // ==================================================================
        //  EL DIBUJADO — v6.50.53 — EL JEFE EXACTO, EN MINIATURA (la
        //  letra: «las mascota del jefe no se parece en nada al jefe,
        //  hazlo que sea exactamente el jefe pero mas pequeño»). EL
        //  DIAGNÓSTICO: el jefe EN JUEGO NO es su sprite png — es EL SOL
        //  DE CÓDIGO del PreDraw (el velo violeta, el halo dorado, LA
        //  RUEDA DE RAYOS RADIALES, LAS DOS CORONAS DE PERLAS, las
        //  chispas orbitantes y el NÚCLEO BLANCO con su corazón dorado);
        //  la mascota dibujaba el PNG crudo a 34 px = un disco chico que
        //  NO se parecía en NADA. LA CURA: LAS SEIS SECCIONES DEL JEFE
        //  replicadas UNA A UNA a la ESCALA 0.22 (el velo del jefe mide
        //  540 px; el de la mascota, 119) — el MISMO dibujo, la MISMA
        //  proporción, el MISMO compás (el latido 1.6 Hz, el giro 0.10
        //  de la rueda, los sentidos opuestos de las coronas: +0.55 y
        //  −0.38). El contrato del bool de la casa: PreDraw cierra el
        //  lote del pase, vuela el SUYO aditivo y lo DEVUELVE ABIERTO
        //  (v6.50.50 — el crash de la .49 murió así).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // v6.50.122 — EL LOTE DEL CUERPO ARRANCA LIMPIO (antes lo
                //     abría el lote alfa de las travesuras — esa máquina
                //     se mudó al Grimorio Menor y con ella su Begin()).
                VFXCore.Begin();

                // === LA ESCALA: el jefe mide ~540 px de velo — la
                //     mascota es SU RETRATO a 0.22 ===
                const float S = 0.22f;

                // === v6.51.10 — EL VUELO VIVO DEL DIBUJO (la letra:
                //     «mejorar las animaciones de (Aethon Menor)"):
                //     · LA INCLINACIÓN — el sol se INCLINA hacia donde
                //       vuela (como inclinaría la cabeza un pajarillo).
                //     · EL ESTIRO — cuando corre, el cuerpo RESPIRA MÁS
                //       GRANDE (el vivo de la velocidad).
                //     · EL ÁNIMO — la rueda y las perlas corren al compás
                //       del dueño: PELEANDO se aceleran (fervor), HERIDO
                //       se aquietan (la cría se encoge de miedo)… y al
                //       CARGAR su destello (miniCerca) la rueda gira más
                //       rápido y las perlas se ACERCAN (la carga).
                //     · EL RASTRO — corazones dorados apagándose donde
                //       acaba de pasar (solo si corre de verdad). ===
                float inclinacion = MathHelper.Clamp(Projectile.velocity.X * 0.018f,
                    -0.30f, 0.30f);
                float velocidad = Projectile.velocity.Length();
                float estiro = 1f + MathHelper.Clamp(velocidad * 0.010f, 0f, 0.22f);
                float animo = _duenioPeleando ? 1.8f : (_duenioHerido ? 0.55f : 1f);
                float giroRueda = _miniCerca ? 1.35f : 1f;
                float comprimePerlas = _miniCerca ? 0.82f : 1f;

                // EL RASTRO (debajo del cuerpo: los corazones de hace un
                // instante, apagándose — el recuerdo del vuelo).
                if (velocidad > 7f && !Main.dedServ)
                {
                    for (int r = 0; r < _rastro.Length; r += 2)
                    {
                        Vector2 pos = _rastro[r];
                        if (pos == Vector2.Zero) continue;
                        float apaga = 1f - r / (float)_rastro.Length;   // viejo = tenue
                        float tam = (46f * S) * apaga * estiro;
                        VFXCore.Quad(pos, new Color(255, 236, 170, 255) * (0.16f * apaga),
                            new Vector2(tam * 1.5f, tam * 1.5f), 0f, VFXCore.SoftGlow);
                        VFXCore.Quad(pos, new Color(255, 252, 240, 255) * (0.20f * apaga),
                            new Vector2(tam * 0.6f, tam * 0.6f), 0f, VFXCore.SoftGlow);
                    }
                }

                // EL MISMO LATIDO y el MISMO BRILLO del jefe (su PreDraw).
                float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                // v6.50.55 — EL BRILLO DEL QUE ARDE: cuando su destello vive
                // a su lado, la fuente SE ENCIENDE (×1.45 — determinista:
                // lee el proyectil sincronizado, todas las pantallas igual).
                float brillo = _miniCerca ? 1.45f : 1f;

                // === 1. EL VELO VIOLETA (la profundidad del sol — la
                //     sección 1 del jefe: Bloom 540, VioletaLuz, 0.10) ===
                VFXCore.Quad(c, new Color(196, 150, 255, 255) * (0.055f * brillo),
                    new Vector2(540f * 1.5f * S, 540f * 1.5f * S), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(196, 150, 255, 255) * (0.10f * brillo),
                    new Vector2(540f * 0.8f * S, 540f * 0.8f * S), 0f, VFXCore.SoftGlow);

                // === 2. EL HALO DORADO (la corona del sol — la sección 2
                //     del jefe: BloomPulse 295·latido, OroLuz, 0.50) ===
                float pulsoH = 0.82f + 0.18f * MathF.Sin(t * 1.6f);
                float halo = 295f * latido * S;
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.32f * brillo * pulsoH),
                    new Vector2(halo * 2.85f, halo * 2.85f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 248, 225, 255) * (0.42f * brillo * pulsoH),
                    new Vector2(halo * 1.5f, halo * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (0.50f * brillo * pulsoH),
                    new Vector2(halo * 0.8f, halo * 0.8f), 0f, VFXCore.SoftGlow);

                // === 3. LA RUEDA DE RAYOS RADIALES (la sección 3 del
                //     jefe: 10 rayos de la fase 1 girando a 0.10 — el sol
                //     de Aethon es una RUEDA DE LUZ; el quad mide 2×largo
                //     × 2×ancho como el Draw del jefe: el rayo sale del
                //     centro hacia AMBOS lados). v6.51.10 — LA RUEDA VIVA:
                //     gira al ANIMO del dueño (fervor/miedo), se acelera
                //     al CARGAR su destello… y el sol entero se INCLINA
                //     hacia el vuelo. ===
                {
                    int nRayos = 10;
                    float giro = t * 0.10f * animo * giroRueda + inclinacion;
                    for (int i = 0; i < nRayos; i++)
                    {
                        float ang = giro + i * MathHelper.TwoPi / nRayos;
                        float largo = (255f + 165f * 0.55f) * S *          // faseInt 0.55 (la fase 2)
                            (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f)) * estiro;
                        float ancho = (34f - 12f * 0.55f) * S;
                        VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.30f * brillo),
                            new Vector2(largo * 2f, ancho * 2f), ang, VFXCore.SoftGlow);
                    }
                }

                // === 4. LAS DOS CORONAS DE PERLAS (la sección 4 del jefe:
                //     13 perlas por anillo, rx 218/ry 134 y rx 300/ry 90,
                //     sentidos OPUESTOS +0.55/−0.38 — el efecto 3D de
                //     Saturno). v6.51.10 — VIVEN AL ÁNIMO: giran con el
                //     compás del dueño y se ACERCAN al centro cuando la
                //     cría CARGA su destello (la espiral de la carga). ===
                for (int anillo = 0; anillo < 2; anillo++)
                {
                    float rx = (anillo == 0 ? 218f : 300f) * S * comprimePerlas;
                    float ry = (anillo == 0 ? 134f : 90f) * S * comprimePerlas;
                    float w = (anillo == 0 ? 0.55f : -0.38f) * animo;  // sentidos opuestos, al ánimo
                    Color cP = anillo == 0 ? new Color(255, 240, 190) : new Color(196, 150, 255);
                    for (int i = 0; i < 13; i++)
                    {
                        float ang = t * w + i * MathHelper.TwoPi / 13f;
                        Vector2 perla = c + new Vector2(MathF.Cos(ang) * rx,
                            MathF.Sin(ang) * ry);
                        float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                        // LA PERLA (Bloom 12 → 2.6 px + su halo).
                        VFXCore.Quad(perla, new Color(cP.R, cP.G, cP.B, 255) *
                            (0.42f * (0.5f + 0.5f * tw) * brillo),
                            new Vector2(12f * 1.5f * S, 12f * 1.5f * S), 0f, VFXCore.SoftGlow);
                        VFXCore.Quad(perla, new Color(255, 250, 235, 255) *
                            (0.42f * (0.5f + 0.5f * tw) * brillo),
                            new Vector2(12f * 0.8f * S, 12f * 0.8f * S), 0f, VFXCore.SoftGlow);
                    }
                }

                // === 5. LAS CHISPAS ORBITANTES (la sección 5 del jefe:
                //     6 motas en r 176·0.72 elíptico) ===
                for (int m = 0; m < 6; m++)
                {
                    float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                    float r = 176f * S + 3.5f * MathF.Sin(t * 2.4f + m);
                    Vector2 mota = c + new Vector2(MathF.Cos(ang) * r,
                        MathF.Sin(ang) * r * 0.72f);
                    VFXCore.Quad(mota, new Color(255, 240, 190, 255) * (0.40f * brillo),
                        new Vector2(20f * 1.5f * S, 20f * 1.5f * S), 0f, VFXCore.SoftGlow);
                    VFXCore.Quad(mota, new Color(255, 252, 240, 255) * (0.40f * brillo),
                        new Vector2(20f * 0.8f * S, 20f * 0.8f * S), 0f, VFXCore.SoftGlow);
                }

                // === 6. EL NÚCLEO (la sección 6 del jefe: el corazón
                //     BLANCO de 130·latido al 1.0 + su alma DORADA de
                //     0.42× al 0.65 — el centro del dios). v6.51.10 — Y
                //     EL ESTIRO DEL VUELO: el corazón respira MÁS
                //     GRANDE cuando corre (el vivo de la velocidad). ===
                float tamNucleo = 130f * latido * S * estiro;
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 2.85f, tamNucleo * 2.85f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 1.5f, tamNucleo * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 0.8f, tamNucleo * 0.8f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.65f * brillo),
                    new Vector2(tamNucleo * 0.42f * 1.5f, tamNucleo * 0.42f * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.65f * brillo),
                    new Vector2(tamNucleo * 0.42f * 0.8f, tamNucleo * 0.42f * 0.8f), 0f, VFXCore.SoftGlow);

                if (VFXCore.QuadCount > 0)
                {
                    // v6.50.50 — EL CRASH DE LA MASCOTA, MUERTO DE RAÍZ:
                    // el contrato viejo (FlushAdditive + return true) dejaba
                    // el lote CERRADO y tML dibujaba encima → «Draw was
                    // called, but Begin has not yet been called» y el End
                    // final del bucle mataba el motor. EL CONTRATO DE LA
                    // CASA: salir SIEMPRE con el lote ABIERTO y vanilla —
                    // y el vuelco YA fue el dibujo (return false: tML no
                    // añade sprite encima).
                    VFXCore.FlushAdditive(null, true); // vuela el lote aditivo (queda cerrado)
                    VFXCore.ReabrirLoteVanilla();      // y SE REABRE — lote vivo para el siguiente
                    return false;
                }
                return false;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }
    }
}
