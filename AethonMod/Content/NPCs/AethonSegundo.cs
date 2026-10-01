using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  v6.50.48 — AETHON, LA SEGUNDA LUZ (la petición del usuario, la
    //  letra entera): «ahora crea un nuevo jefe Aethon con un nuevo
    //  invocador, que sera el invocador numero 2, este jefe tambien es
    //  una luz, pero dale la entrada exacta que tiene la emperatriz de
    //  la luz, y dale 5 fases, cada una tiene una IA mejor que la otra,
    //  en cada fase tiene nuevos proyectiles mas lo de las fases
    //  anteriores, dale a este jefe un nuevo set de movimiento,
    //  investiga jefes similares en otros mods para saber como hacer
    //  con este jefe».
    //
    //  LA INVESTIGACIÓN (los jefes multi-fase de los mods, destilada):
    //  · Supreme Calamitas (Calamity): diez fases que AÑADEN armas al
    //    pool — cada fase hereda TODO lo anterior y suma lo suyo; sus
    //    ataques son LENTOS y telegrafiados (la debilidad de la bruja
    //    es la puntería: se esquiva leyéndola).
    //  · Calamitas (Calamity): al bajar de vida AGREGA cargas (embestidas
    //    encadenadas) entre las andanadas — el movimiento ES un arma.
    //  · Fargo's Eternity: bibliotecas de patrones que rotan; la variedad
    //    de MOVIMIENTO (cargas, órbitas, parpadeos) es lo que se recuerda.
    //  · La Emperatriz de la luz (vanilla, la referencia de la entrada):
    //    nace YA EN el mundo (200 px sobre el punto, jitter de 50, via
    //    SpawnBoss) y pelea al segundo — y sus CICLOS DE CARGA son su
    //    firma. De ella aprendió esta luz.
    //
    //  EL DISEÑO (5 fases; cada una MEJOR que la anterior — la IA y el
    //  arsenal CRECEN, nunca se resetean):
    //   · FASE 1 (100-80%): EL PASEO — la figura de ocho (lemniscata)
    //     alrededor de la presa + LAS ASTILLAS (ráfagas de pernos).
    //   · FASE 2 (80-60%): + LA CARRERA PRISMÁTICA (la firma de la
    //     Emperatriz: cadena de cargas telegrafiadas a través de la
    //     presa) + EL PRISMA (el abanico refractado de tres colores que
    //     converge).
    //   · FASE 3 (60-40%): + EL PARPADEO (se apaga, no está, y YA está
    //     a tu espalda) + EL ANILLO SOLAR (la onda que nace alrededor
    //     tuyo y se abre).
    //   · FASE 4 (40-20%): la carrera traza EL PENTAGRAMA (cinco puntas
    //     en orden de estrella: 0-2-4-1-3) + LA LLUVIA PRISMÁTICA (la
    //     andanada del cielo) — y la figura de ocho se cierra (440 ->
    //     300 px: más cerca, más honesta).
    //   · FASE 5 (20-0%, LA FURIA): TODO lo anterior sin respiro + LA
    //     CORONA (la galaxia: tres brazos que giran y colapsan sobre la
    //     presa) — la segunda luz no aprendió la piedad.
    //
    //  EL CONTRATO DE LA CASA (intacto): ai[0] = estado · ai[1] =
    //  subestado · ai[2] = tick del estado · ai[3] = param (el rumbo);
    //  NADIE toca el estilo de un proyectil (la ley de oro de la .46);
    //  el render en los clientes lee el espejo ai[] + la vida (la fase
    //  se deriva de life/lifeMax — determinista en todas las máquinas).
    // ======================================================================
    public class AethonSegundo : ModNPC
    {
        // === LOS ESTADOS (ai[0]) ===
        private const int EST_PRESENTA = 0;   // el encendido (30 t: la Emperatriz entra YA peleando)
        private const int EST_PASEO = 1;      // la figura de ocho (el default)
        private const int EST_CARRERA = 2;    // LA CARRERA PRISMÁTICA (cargas encadenadas)
        private const int EST_PARPADEO = 3;   // EL PARPADEO (la luz que no está y ya está)
        private const int EST_MUERTE = 99;    // el cine final

        // === LOS SUBESTADOS (ai[1]) ===
        public const int SUB_CARRERA_TELEGRAFO = 0;   // cargando la carga
        public const int SUB_CARRERA_VUELO = 1;       // atravesando la punta
        public const int SUB_PARPADEO_SE_APAGA = 0;   // el fade
        public const int SUB_PARPADEO_NO_ESTA = 1;    // el vacío
        public const int SUB_PARPADEO_YA_ESTA = 2;    // el regreso

        // === EL ESTADO LOCAL (servidor; el cliente lee el espejo ai[]) ===
        private int _estado = EST_PRESENTA;
        private int _tickEstado = 0;
        private int Phase = 1;

        // EL RITMO DEL PASEO (la lemniscata)
        private float _angPaseo = -MathHelper.PiOver2;

        // LA CARRERA: las puntas (server-side; el telegraph se dibuja
        // con la presa local en cada máquina).
        private Vector2[] _puntas = new Vector2[5];
        private int _puntaActual = 0;

        // EL TEMPO DE LOS SIRVES (la bolsa)
        private int _tickAtaque = 0;

        // EL CINE DE MUERTE
        private bool _muriendo = false;
        private int _tickMuerte = 0;
        private bool _yaDropeo = false;

        // EL DESPAWN LIMPIO (solo por presa muerta — la casa)
        private int _tickPresaMuerta = 0;

        // === LA FASE (los umbrales de la casa, con histéresis) ===
        private bool Furia => Phase >= 5;

        /// <summary>
        /// LA FASE DESDE LA VIDA — determinista en TODAS las máquinas (el
        /// render del cliente no tiene el campo Phase: lo deriva de
        /// life/lifeMax, que sí viaja).
        /// </summary>
        public static int FaseDesdeVida(float hpPct)
        {
            int f = 1;
            if (hpPct < 0.8f) f = 2;
            if (hpPct < 0.6f) f = 3;
            if (hpPct < 0.4f) f = 4;
            if (hpPct < 0.2f) f = 5;
            return f;
        }

        // === LA PALETA (la segunda luz: blanco-rosa-cian — la luz que
        //     aprendió de la Emperatriz) ===
        private static readonly Color BlancoSegunda = new(255, 253, 246);
        private static readonly Color RosaPrisma = new(255, 190, 235);
        private static readonly Color CianPrisma = new(170, 240, 255);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 200;
            NPC.height = 200;
            NPC.damage = 140;
            NPC.defense = 60;
            NPC.lifeMax = 2_800_000;
            NPC.HitSound = SoundID.NPCHit52;
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            Music = MusicID.Boss5;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override bool CheckActive()
        {
            // EL CINE MANDA (la casa): ni el tiempo ni la pantalla matan a
            // la luz mientras nace o se apaga. El despawn limpio SOLO por
            // presa muerta (nadie la vio irse).
            if (_muriendo || _estado == EST_PRESENTA) return false;
            if (_tickPresaMuerta > 540) return true;
            return false;
        }

        // ==================================================================
        //  LA IA
        // ==================================================================
        public override void AI()
        {
            // === EL CINE DE MUERTE ===
            if (_muriendo) { CineMuerte(); return; }

            Player target = Main.player[NPC.target];
            bool presaViva = target != null && target.active && !target.dead;

            // EL RELOJ DEL DESPAWN LIMPIO
            _tickPresaMuerta = presaViva ? 0 : _tickPresaMuerta + 1;
            if (!presaViva && _tickPresaMuerta > 120)
            {
                NPC.EncourageDespawn(10);
                return;
            }

            // === LA FASE POR VIDA (umbrales con histéresis — nunca baja) ===
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int nueva = FaseDesdeVida(hpPct);
            if (nueva > Phase)
            {
                Phase = nueva;
                OnPhaseChange();
            }

            _tickEstado++;

            switch (_estado)
            {
                case EST_PRESENTA: EstadoPresenta(target); break;
                case EST_PASEO: EstadoPaseo(target, presaViva); break;
                case EST_CARRERA: EstadoCarrera(target, presaViva); break;
                case EST_PARPADEO: EstadoParpadeo(target, presaViva); break;
            }

            // === LOS SIRVES DE FONDO (la bolsa por fase — en PASEO y
            //     PARPADEO; la carrera lleva los suyos en la mano) ===
            if ((_estado == EST_PASEO || _estado == EST_PARPADEO) && presaViva)
            {
                _tickAtaque++;
                int cadencia = Math.Max(50, 92 - 8 * Phase);
                if (_tickAtaque >= cadencia)
                {
                    _tickAtaque = 0;
                    ServirBolsa(target);
                }
            }

            // === LA ELECCION DEL MOVIMIENTO (cada fase tiene una IA mejor
            //     que la otra: la probabilidad de INTERRUMPIR el paseo
            //     crece con la fase) ===
            if (_estado == EST_PASEO && presaViva && _tickEstado > 90)
            {
                int apetito = 220 - 28 * Phase;          // P2: 164 t · P5: 80 t
                if (Furia) apetito = 80;
                if (_tickEstado % apetito == 0)
                {
                    // P3+: el parpadeo alternative; P2+: la carrera.
                    bool parpadea = Phase >= 3 && ((_tickEstado / apetito) % 2) == 1;
                    if (parpadea) EntrarParpadeo();
                    else EntrarCarrera(target);
                }
            }

            // === LA LUZ DEL MUNDO (la segunda luz inunda) ===
            Lighting.AddLight(NPC.Center, new Vector3(1.35f, 1.15f, 1.05f));

            // === EL CONTRATO MP (el de la casa) ===
            NPC.ai[0] = _estado;
            NPC.ai[1] = SubestadoActual();
            NPC.ai[2] = _tickEstado;
            NPC.ai[3] = NPC.velocity.Length() <= 0.01f && target != null
                ? (target.Center - NPC.Center).ToRotation()
                : NPC.velocity.ToRotation();   // el rumbo del telegraph

            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        /// <summary>EL SUBESTADO que viaja en ai[1] (el contrato del espejo).</summary>
        private float SubestadoActual()
        {
            switch (_estado)
            {
                case EST_CARRERA: return _puntaActual;                    // la punta en vuelo
                case EST_PARPADEO: return NPC.alpha > 40 ? 1f : 0f;      // el vacío o el regreso
            }
            return 0f;
        }

        // ==================================================================
        //  LA PRESENTACIÓN (la entrada de la Emperatriz es INSTANTÁNEA:
        //  SpawnBoss la trae al mundo YA; estos 30 t son solo el
        //  encendido del núcleo — la luz sube a su brillo de pelea)
        // ==================================================================
        private void EstadoPresenta(Player target)
        {
            NPC.velocity *= 0.90f;
            NPC.alpha = Math.Max(0, NPC.alpha - 9);
            NPC.dontTakeDamage = _tickEstado < 26;

            if (_tickEstado == 6)
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.AethonSegundo.Presentacion",
                    BlancoSegunda);

            if (_tickEstado >= 30)
            {
                _estado = EST_PASEO;
                _tickEstado = 0;
                NPC.dontTakeDamage = false;
                NPC.netUpdate = true;
            }
        }

        // ==================================================================
        //  EL PASEO — LA FIGURA DE OCHO (el set de movimiento nuevo: la
        //  lemniscata. La órbita de la primera luz era un círculo; esta
        //  recorre el ocho infinito alrededor de la presa, cerrando el
        //  paso con la fase)
        // ==================================================================
        private void EstadoPaseo(Player target, bool presaViva)
        {
            if (!presaViva) { NPC.velocity *= 0.92f; return; }

            float rx = MathHelper.Lerp(440f, 300f, (Phase - 1) / 4f);   // se cierra con la fase
            float ry = rx * 0.58f;
            _angPaseo += 0.016f * (1f + 0.13f * Phase) * (Furia ? 1.35f : 1f);

            // LA LEMNISCATA: x = sin(a)*Rx · y = sin(2a)*Ry (el ocho).
            Vector2 punto = target.Center + new Vector2(
                MathF.Sin(_angPaseo) * rx,
                MathF.Sin(2f * _angPaseo) * ry - 60f);

            Vector2 dir = punto - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, dir * 0.105f, 0.25f);
        }

        // ==================================================================
        //  LA CARRERA PRISMÁTICA (la firma de la Emperatriz — la aprendió
        //  de la fuente): cadena de cargas telegrafiadas A TRAVÉS de la
        //  presa. P4+ traza EL PENTAGRAMA (0-2-4-1-3: la figura de la
        //  estrella que la primera luz dibuja con su Telar).
        // ==================================================================
        private void EntrarCarrera(Player target)
        {
            _estado = EST_CARRERA;
            _tickEstado = 0;
            _puntaActual = 0;

            int n = Phase >= 4 ? 5 : 3;
            float radio = 470f;
            float baseAng = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < n; i++)
            {
                // P4+: el ORDEN DE LA ESTRELLA (paso +2); P2-3: abanico.
                int orden = Phase >= 4 ? (i * 2) % n : i;
                float ang = baseAng + orden * MathHelper.TwoPi / n;
                _puntas[i] = target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.72f) * radio;
                _puntas[i].Y -= 80f;
            }
            // la última punta ES la presa (la carga la atraviesa).
            _puntas[n - 1] = target.Center + target.velocity * 18f;

            NPC.netUpdate = true;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
        }

        private void EstadoCarrera(Player target, bool presaViva)
        {
            if (!presaViva) { _estado = EST_PASEO; _tickEstado = 0; return; }
            int n = Phase >= 4 ? 5 : 3;
            int telegrafo = Furia ? 18 : 26;

            if (_puntaActual >= n)
            {
                // LA SALIDA: frena y vuelve al paseo.
                NPC.velocity *= 0.86f;
                if (NPC.velocity.LengthSquared() < 4f)
                {
                    _estado = EST_PASEO;
                    _tickEstado = 0;
                    NPC.netUpdate = true;
                }
                return;
            }

            Vector2 punta = _puntas[_puntaActual];

            if (_tickEstado <= telegrafo)
            {
                // EL TELEGRAPH: la luz se recoge sobre la línea.
                NPC.velocity *= 0.80f;
                return;
            }

            // EL VUELO: atraviesa la punta a velocidad luz.
            Vector2 dir = punta - NPC.Center;
            float d = dir.Length();
            if (d < 70f || _tickEstado > telegrafo + 46)
            {
                // PUNTA TOCADA: su mini-abadanca (las astillas de la casa).
                ServirAstillas(target, 2 + Phase / 2);
                _puntaActual++;
                _tickEstado = 0;
                NPC.netUpdate = true;
                return;
            }
            NPC.velocity = dir / d * (24f + 2.4f * Phase);
        }

        // ==================================================================
        //  EL PARPADEO (P3+): la luz se apaga, no está… y YA está a tu
        //  espalda con el abanico refractado abierto.
        // ==================================================================
        private void EntrarParpadeo()
        {
            _estado = EST_PARPADEO;
            _tickEstado = 0;
            NPC.netUpdate = true;
        }

        private void EstadoParpadeo(Player target, bool presaViva)
        {
            if (!presaViva) { _estado = EST_PASEO; _tickEstado = 0; NPC.alpha = 0; return; }

            if (_tickEstado < 6)
            {
                // SE APAGA (el fade rápido).
                NPC.alpha = Math.Min(255, NPC.alpha + 45);
                NPC.velocity *= 0.8f;
            }
            else if (_tickEstado < 6 + 14)
            {
                // NO ESTÁ (el vacío: la luz se fue y el mundo la espera).
                NPC.alpha = 210;
                NPC.velocity = Vector2.Zero;
                if (_tickEstado == 6 + 13)
                {
                    // EL REGRESO: a la espalda de la presa.
                    Vector2 detras = target.Center + new Vector2(-target.direction * 380f, -150f);
                    detras.X = Math.Clamp(detras.X, Main.leftWorld + 400f, Main.rightWorld - 400f);
                    detras.Y = Math.Clamp(detras.Y, Main.topWorld + 400f, Main.bottomWorld - 400f);
                    NPC.Center = detras;
                    NPC.velocity = Vector2.Zero;
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 14; i++)
                        {
                            Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
                                new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 2f)),
                                180, RosaPrisma, 1.2f);
                            d.noGravity = true;
                        }
                    }
                    // YA ESTÁ: el abanico refractado a bocajarro.
                    ServirPrisma(target, 0.55f);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                }
            }
            else
            {
                // ENCIENDE de nuevo y vuelve al paseo.
                NPC.alpha = Math.Max(0, NPC.alpha - 40);
                if (_tickEstado >= 6 + 14 + 10)
                {
                    NPC.alpha = 0;
                    _estado = EST_PASEO;
                    _tickEstado = 0;
                    NPC.netUpdate = true;
                }
            }
        }

        // ==================================================================
        //  LA BOLSA POR FASE (cada fase AÑADE; nada se retira — la letra:
        //  «en cada fase tiene nuevos proyectiles mas lo de las fases
        //  anteriores»). La bolsa lee la fase y el momento.
        // ==================================================================
        private void ServirBolsa(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                // LA RULETA ponderada por fase: lo nuevo pesa el doble.
                int[] menu = MenuDeFase();
                int tiro = Main.rand.Next(menu.Length);
                switch (menu[tiro])
                {
                    case 0: ServirAstillas(target, 4); break;
                    case 1: ServirPrisma(target, 1f); break;
                    case 2: ServirAnilloSolar(target); break;
                    case 3: ServirLluvia(target); break;
                    case 4: ServirCorona(target); break;
                }
            }
            catch { }
        }

        /// <summary>EL MENÚ de la fase (lo nuevo primero: pesa el doble).</summary>
        private int[] MenuDeFase()
        {
            switch (Phase)
            {
                case 1: return new int[] { 0, 0, 0 };
                case 2: return new int[] { 0, 0, 1, 1 };
                case 3: return new int[] { 0, 0, 1, 2, 2 };
                case 4: return new int[] { 0, 1, 2, 3, 3 };
                default: return new int[] { 0, 1, 2, 3, 4, 4, 4 };   // la furia: la corona reina
            }
        }

        /// <summary>El daño de los pernos de la segunda luz.</summary>
        private static int DanoPerno => 190;

        /// <summary>LANZA un proyectil del arsenal (el estilo viaja en ai[0] — LA LEY DE ORO).</summary>
        private void Lanzar(int estilo, Vector2 pos, Vector2 vel, float par, int vida = 240)
        {
            int idx = Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                pos, vel,
                ModContent.ProjectileType<AtaqueJefe2Projectile>(),
                DanoPerno, 2f, Main.myPlayer,
                estilo, par, Main.rand.Next(9973));
            if (idx >= 0 && idx < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[idx];
                p.timeLeft = vida;
                p.netUpdate = true;
            }
        }

        // (0) LAS ASTILLAS — las ráfagas de pernos rectos a la predicción.
        private void ServirAstillas(Player target, int rafaga)
        {
            Vector2 pred = target.Center + target.velocity * 14f;
            Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < rafaga; i++)
            {
                Vector2 vel = dir.RotatedBy((i - (rafaga - 1) / 2f) * 0.09f) * (17f + 1.2f * Phase);
                Lanzar(AtaqueJefe2Projectile.EstiloAstilla, NPC.Center, vel, i);
            }
        }

        // (1) EL PRISMA — el abanico refractado: tres colores que parten
        //     de la luz y convergen en la presa (la dispersión cromática).
        private void ServirPrisma(Player target, float escala)
        {
            for (int chroma = 0; chroma < 3; chroma++)
            {
                Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                int porColor = (int)(3 * escala) + 1;
                for (int i = 0; i < porColor; i++)
                {
                    float abre = (chroma - 1f) * 0.30f + (i - (porColor - 1) / 2f) * 0.11f;
                    Vector2 vel = dir.RotatedBy(abre) * (15f + 1.1f * Phase);
                    Lanzar(AtaqueJefe2Projectile.EstiloPrisma, NPC.Center, vel, chroma);
                }
            }
        }

        // (2) EL ANILLO SOLAR — la onda que nace alrededor de la presa y
        //     se abre (el aro que empuja hacia fuera).
        private void ServirAnilloSolar(Player target)
        {
            int n = 14;
            for (int i = 0; i < n; i++)
            {
                float ang = i * MathHelper.TwoPi / n;
                Vector2 pos = target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.78f) * 340f;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 3.6f;
                Lanzar(AtaqueJefe2Projectile.EstiloAnilloSolar, pos, vel, ang, 260);
            }
        }

        // (3) LA LLUVIA PRISMÁTICA — la andanada del cielo en dos olas.
        private void ServirLluvia(Player target)
        {
            int n = 8 + Phase / 2;
            for (int i = 0; i < n; i++)
            {
                Vector2 pos = target.Center + new Vector2(
                    (i - (n - 1) / 2f) * 95f + Main.rand.NextFloat(-30f, 30f), -470f);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), 10f + 1.1f * Phase);
                Lanzar(AtaqueJefe2Projectile.EstiloLluvia, pos, vel, i % 3, 300);
            }
        }

        // (4) LA CORONA — la galaxia: tres brazos que giran alrededor de
        //     la presa y colapsan (la firma de la furia).
        private void ServirCorona(Player target)
        {
            int brazos = Furia ? 4 : 3;
            for (int b = 0; b < brazos; b++)
            {
                for (int k = 0; k < 10; k++)
                {
                    float r = 150f + k * 48f;                        // el brazo se abre
                    float ang = b * MathHelper.TwoPi / brazos + k * 0.19f;
                    Vector2 pos = target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * r;
                    // VELOCIDAD TANGENCIAL (omega*r) + HUNDIMIENTO: el
                    // remolino que gira y colapsa — la receta del vórtice
                    // de la primera luz, en prisma.
                    Vector2 vel = new Vector2(-MathF.Sin(ang), MathF.Cos(ang)) * (r * 0.021f);
                    vel += (target.Center - pos).SafeNormalize(Vector2.Zero) * (0.65f + 0.45f * k / 10f);
                    Lanzar(AtaqueJefe2Projectile.EstiloCorona, pos, vel, b, 320);
                }
            }
        }

        // ==================================================================
        //  EL CAMBIO DE FASE (el Decreto de la segunda luz: el ANILLO
        //  PRISMÁTICO que la recorre cuando sube de fase — cosmético y
        //  un respiro de 24 t: la ventana de la casa).
        // ==================================================================
        private void OnPhaseChange()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefe2Projectile>(),
                    0, 0f, Main.myPlayer, AtaqueJefe2Projectile.EstiloCambioPrisma, Phase, 1);
                if (idx >= 0 && idx < Main.maxProjectiles)
                {
                    Main.projectile[idx].timeLeft = 48;
                    Main.projectile[idx].netUpdate = true;
                }
            }
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.AethonSegundo.Fase",
                RosaPrisma, Phase, NombreDeFase(Phase));
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
            NPC.netUpdate = true;
        }

        /// <summary>El nombre de la fase (la identidad de la segunda luz).</summary>
        private static string NombreDeFase(int f)
        {
            return f switch
            {
                1 => Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Jefe.AethonSegundo.Nombre1"),
                2 => Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Jefe.AethonSegundo.Nombre2"),
                3 => Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Jefe.AethonSegundo.Nombre3"),
                4 => Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Jefe.AethonSegundo.Nombre4"),
                _ => Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Jefe.AethonSegundo.Nombre5"),
            };
        }

        // ==================================================================
        //  LA MUERTE (el cine de la casa)
        // ==================================================================
        public override bool CheckDead()
        {
            if (_muriendo) return false;
            _muriendo = true;
            _tickMuerte = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.AethonSegundo.Muerte", BlancoSegunda);
            NPC.netUpdate = true;
            return false;
        }

        private void CineMuerte()
        {
            _tickMuerte++;
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.10f);

            if (_tickMuerte >= 120)
            {
                OndaLib.Kick(14f, 30);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath55, NPC.Center);
                if (!Main.dedServ)
                {
                    for (int d = 0; d < 110; d++)
                    {
                        int idx = Dust.NewDust(NPC.Center, 190, 190, DustID.GoldFlame,
                            Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-16f, 4f));
                        Main.dust[idx].noGravity = true;
                    }
                }
                DropBotin();
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        /// <summary>EL BOTÍN de la segunda luz (la Forma Ascendida 2 cae aquí).</summary>
        private void DropBotin()
        {
            if (_yaDropeo) return;
            _yaDropeo = true;
            EsenciasModSistema.SoltarEsencia(NPC);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Cosmetics.FormaAscendidaDosItem>(), 1);

            int killerWho = NPC.lastInteraction;
            if (killerWho < 0 || killerWho >= Main.player.Length)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && NPC.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho >= 0 && killerWho < Main.player.Length)
            {
                Player player = Main.player[killerWho];
                if (player != null && player.active)
                {
                    var sp = player.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null)
                    {
                        sp.ResonanceShards += 300;
                        EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                            new Color(255, 224, 196), NPC.FullName, 300);
                        EcoRed.SincronizarCronica(player);
                    }
                }
            }
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.AethonSegundo.Reconocimiento", BlancoSegunda);
        }

        public override void OnKill()
        {
            if (_yaDropeo) return;
            DropBotin();
        }

        // ==================================================================
        //  EL ARTE (100% luz de código — la casa: sin anatomía, SOLO
        //  brillo). El núcleo blanco-rosa + los rayos que giran más
        //  rápido con la fase + el tinte por fase (P1 oro · P2 rosa ·
        //  P3 cian · P4 violeta · P5 el blanco puro) + la estela de la
        //  carrera. El cliente lo dibuja TODO desde el espejo ai[] y la
        //  vida (la fase derivada: determinista en todas las máquinas).
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                float hpPct = NPC.lifeMax > 0 ? (float)NPC.life / NPC.lifeMax : 1f;
                int fase = FaseDesdeVida(hpPct);

                // EL TINTe POR FASE (la segunda luz cambia de color al
                // subir: el prisma que se abre).
                Color tinte = fase switch
                {
                    1 => new Color(255, 240, 190),
                    2 => new Color(255, 196, 232),
                    3 => new Color(186, 240, 255),
                    4 => new Color(222, 188, 255),
                    _ => new Color(255, 253, 246),
                };
                Color blanco = new Color(255, 253, 246);
                Color rosa = RosaPrisma;
                Color cian = CianPrisma;

                // === EL NÚCLEO (el corazón blanco + el velo) ===
                VFXCore.Quad(NPC.Center, TintAditivo(tinte, 0.55f),
                    new Vector2(230f, 230f), t * 0.08f, VFXCore.SoftGlow);
                VFXCore.Quad(NPC.Center, TintAditivo(blanco, 0.5f),
                    new Vector2(110f, 110f), -t * 0.16f, VFXCore.SoftGlow);

                // === LOS RAYOS (el aro que gira — más rápido con la fase) ===
                float giro = t * (0.14f + 0.10f * fase);
                for (int i = 0; i < 8; i++)
                {
                    float ang = giro + i * MathHelper.PiOver4;
                    float largo = 150f + 55f * MathF.Sin(t * 1.1f + i * 1.3f);
                    Vector2 medio = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f) * (96f + largo * 0.5f);
                    VFXCore.Quad(medio, TintAditivo(i % 2 == 0 ? rosa : cian, 0.22f),
                        new Vector2(largo, 7f), ang, VFXCore.SoftGlow);
                }

                // === EL TELEGRAPH DE LA CARRERA (la línea de la carga:
                //     el cliente la dibuja hacia la presa local) ===
                if ((int)NPC.ai[0] == EST_CARRERA && Main.LocalPlayer != null)
                {
                    Vector2 presa = Main.LocalPlayer.Center;
                    Vector2 dir = presa - NPC.Center;
                    float largoT = Math.Min(dir.Length(), 900f);
                    if (largoT > 40f)
                    {
                        Vector2 medioT = NPC.Center + dir / dir.Length() * (largoT * 0.5f);
                        float alfaT = 0.10f + 0.10f * MathF.Sin(t * 9f);
                        VFXCore.Quad(medioT, TintAditivo(blanco, alfaT),
                            new Vector2(largoT, 5f), dir.ToRotation(), VFXCore.SoftGlow);
                        VFXCore.Quad(medioT, TintAditivo(rosa, alfaT * 0.7f),
                            new Vector2(largoT * 0.92f, 2.2f), dir.ToRotation(), VFXCore.SoftGlow);
                    }
                }

                // === LA ESTELA DE LA CARRERA (el cometa prismático) ===
                if ((int)NPC.ai[0] == EST_CARRERA && NPC.velocity.LengthSquared() > 60f)
                {
                    Vector2 cola = NPC.Center - NPC.velocity * 2.6f;
                    for (int g = 0; g < 3; g++)
                    {
                        Vector2 eco = NPC.Center - NPC.velocity * (1.4f + g * 1.3f);
                        VFXCore.Quad(eco, TintAditivo(g == 0 ? blanco : (g == 1 ? rosa : cian), 0.34f / (g + 1)),
                            new Vector2(150f / (g + 1), 150f / (g + 1)), 0f, VFXCore.SoftGlow);
                    }
                    VFXCore.Line(cola, NPC.Center, TintAditivo(blanco, 0.30f), 7f);
                }

                VFXCore.FlushAdditive(null, false);

                // === FASE 2: el lote aditivo de pantalla (el aro de
                //     perlas + el bloom del núcleo) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Vector2 pos = NPC.Center - Main.screenPosition;
                    LumenLib.Bloom(Main.spriteBatch, pos, 120f, tinte, 0.5f);
                    LumenLib.Bloom(Main.spriteBatch, pos, 54f, blanco, 0.7f);
                    OrbitaLib.AnilloFino(pos, 64f, t * 1.4f, OrbitaLib.Tint(rosa, 0.4f));
                    OrbitaLib.AnilloFino(pos, 46f, -t * 1.9f, OrbitaLib.Tint(cian, 0.34f));
                }
                finally { Main.spriteBatch.End(); }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }

        /// <summary>El tinte aditivo de la casa (el alpha que la luz lleva).</summary>
        private static Color TintAditivo(Color c, float alfa)
        {
            Color r = c;
            r.A = (byte)(255 * MathHelper.Clamp(alfa, 0f, 1f));
            return r;
        }
    }
}
