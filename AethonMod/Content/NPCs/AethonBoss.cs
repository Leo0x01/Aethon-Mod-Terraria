using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// AETHON, LA LUZ PRIMORDIAL — LA ENCARNACIÓN (v6.50.36).
    ///
    /// Petición del usuario: «el jefe se ve feo, intenta mejorar por
    /// código… ese jefe se supone que es Aethon, creo que en vez de
    /// hacerlo una sierpe, mejor hacerlo una luz brillante, el jefe es
    /// una potente luz que ataca al jugador con ataques devastadores».
    ///
    /// LA SIERPE MUERE (v6.50.19→35: seis versiones de hueso). Si el
    /// jefe ES Aethon y Aethon ES la Luz Primordial — ¿por qué esconder
    /// la luz dentro de un esqueleto? EL JEFE ES LA LUZ MISMA: un SOL
    /// VIVO que flota sobre el mundo y lo castiga con ataques de luz.
    ///
    /// · EL CUERPO: UN SOLO NPC (la cadena de 68 vértebras MUERE —
    ///   AethonSierpeCuerpo/AethonSierpeCola borrados del mod). Esfera
    ///   pura: no hay anatomía que pueda verse fea — hay FENÓMENO.
    /// · EL ARTE (100% código, la técnica de la casa): EL NÚCLEO blanco
    ///   pulsante + EL HALO dorado + LOS RAYOS RADIALES rotando lento +
    ///   LAS DOS CORONAS DE PERLAS (órbitas elípticas de blooms — el
    ///   efecto 3D de Saturno) + LAS CHISPAS orbitantes + LA ESTELA del
    ///   destello. Todo aditivo: la luz no tiene silueta, tiene brillo.
    /// · LA IA — LA LUZ ATACA CON TODO (devastador, decretado):
    ///   - EL JUICIO DE LUZ: columnas de luz que CAEN DEL CIELO sobre
    ///     las posiciones PREDICHAS (el proyectil se telegrafea solo:
    ///     cae lento 45 t — la línea de luz — y luego ACELERA a 52).
    ///   - EL RAYO PRIMORDIAL: el arco PerlinBolt GRUESO boca→presa con
    ///     lluvia densa de pernos (heredero del aliento de la sierpe).
    ///   - LA NOVA: contracción telegrafiada y ESTALLIDO en anillos con
    ///     huecos — hay que BUSCAR EL HUECO (el anuncio lo dice).
    ///   - LA CRUZ DE LUZ (fase 3+): cuatro chorros de pernos girando —
    ///     la cruz que barre la arena.
    ///   - EL DESTELLO: la embestida a velocidad luz con línea guía
    ///     (heredera del ram: cadena 2-3 desde ángulos nuevos).
    ///   - EL ECLIPSE (fase 4+): LA LUZ SE APAGA — el núcleo se oscurece,
    ///     la atracción tira de ti hacia el cuerpo muerto y SOLO LAS
    ///     BALAS BRILLAN (bullet hell lento); al final, EL REGRESO: la
    ///     luz VUELVE con una nova gratis.
    ///   - LA ROTACIÓN heredada (v6.50.34): NUNCA el mismo ataque dos
    ///     veces — la luz es impredecible.
    ///   - EL ANTI-CAMPING heredado: presa quieta → el flotar se corta.
    /// · LAS FASES (los umbrales de siempre): P1 POLVO ESTELAR (juicio
    ///   + rayo) · P2 NEBULOSA (+nova) · P3 GRAVEDAD (+cruz, +destello,
    ///   el volteo) · P4 AGUJERO NEGRO (+eclipse, las runas recuerdan)
    ///   P5 RECONOCIMIENTO (la furia: TODO + El Recordar + cadenas).
    /// · LA LLEGADA: ColaSierpeSky (el cielo de la casa) ahora pinta EL
    ///   CIELO SE ENCIENDE — resplandor creciente + columnas lejanas +
    ///   la ventana del núcleo (el mismo contrato de lote del fondo).
    ///
    /// LO QUE VIVE DE LA SIERPE: el drop (La Forma Ascendida + esencia
    /// + 250 shards), la barra de jefe con icono (regenerado: EL SOL),
    /// el cine de muerte (ahora: LA CONTRACCIÓN y el estallido final),
    /// el contrato MP (ai[0]/ai[1]/ai[2]/ai[3]), las runas, el volteo
    /// de gravedad, la predicción adaptativa y la paciencia dinámica.
    /// </summary>
    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
        // === LOS ESTADOS DE LA LUZ ===
        private const int EST_NACIENDO = 0;     // el cielo se enciende
        private const int EST_FLOTAR = 1;       // la órbita serena
        private const int EST_JUICIO = 2;       // LAS COLUMNAS DEL CIELO
        private const int EST_RAYO = 3;         // el arco primordial
        private const int EST_NOVA = 4;         // la contracción y el estallido
        private const int EST_CRUZ = 5;         // los cuatro chorros girando
        private const int EST_DESTELLO = 6;     // la embestida a velocidad luz
        private const int EST_ECLIPSE = 7;      // la luz se apaga
        private const int EST_MURIENDO = 99;    // la contracción final

        private int _estado = EST_NACIENDO;
        private int _tickEstado = 0;
        private bool _nacio = false;

        // === EL RAYO (heredero del aliento: 1 carga · 2 fuego) ===
        private int _rayo = 0;
        private int _tickRayo = 0;

        // === LA ROTACIÓN (v6.50.34 — la luz nunca repite) ===
        private int _ultimoAtaque = 0;          // 0 ninguno · 1 juicio · 2 rayo · 3 nova · 4 cruz · 5 destello · 6 eclipse
        private int _cadenasDestello = 0;       // cuántos destellos lleva la cadena
        private float _angDestello = 0f;        // el rumbo del destello (viaja en ai[3])

        // === EL ANTI-CAMPING (heredado) ===
        private int _ticksPresaQuieta = 0;

        // === EL VOLTEO Y LAS RUNAS (heredados de la sierpe) ===
        private int _tickGravedad = 0;
        private int _tickRunas = 0;

        // === LA FASE (los umbrales de siempre) ===
        private int Phase = 1;

        /// <summary>LA FURIA: la fase final es MÁS RÁPIDA en todo.</summary>
        private bool Furia => Phase >= 5;

        // === LA ÓRBITA DE FLOTACIÓN ===
        private float _angOrbita = -MathHelper.PiOver2;
        private int _sentidoOrbita = 1;

        // === EL CINE DE MUERTE ===
        private bool _muriendo = false;
        private int _tickMuerte = 0;
        private bool _yaDropeo = false;

        // === LA PALETA DE LA LUZ PRIMORDIAL ===
        private static readonly Color OroLuz = new(255, 240, 190);
        private static readonly Color VioletaLuz = new(196, 150, 255);
        private static readonly Color NucleoBlanco = new(255, 252, 240);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 160;     // el sol (v6.50.36 — la luz no tiene huesos)
            NPC.height = 160;
            NPC.damage = 120;    // TOCAR LA LUZ quema (devastador, decretado)
            NPC.defense = 50;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit52;    // el golpe de energía
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;   // la luz atraviesa el mundo
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            Music = MusicID.Boss5;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        // ==================================================================
        //  LA IA — LA LUZ PRIMORDIAL
        // ==================================================================
        public override void AI()
        {
            // === EL CINE DE MUERTE (CheckDead manda aquí) ===
            if (_muriendo) { CineMuerte(); return; }

            // === LA PRESA (el despawn limpio de la casa) ===
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            // === EL DETECTOR DE PRESA QUIETA (heredado: pararse es invitar) ===
            if (target.velocity.LengthSquared() < 0.36f) _ticksPresaQuieta++;
            else _ticksPresaQuieta = 0;

            // === LA LLEGADA: nace EN EL CIELO (primer tick) ===
            if (!_nacio)
            {
                int lado = target.Center.X < NPC.Center.X ? 1 : -1;
                NPC.Center = target.Center + new Vector2(-lado * 420f, -980f);
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 255;          // se materializa al descender
                NPC.dontTakeDamage = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Presentacion", OroLuz);
                _nacio = true;
            }

            // === LA FASE POR VIDA (los umbrales de siempre, histéresis) ===
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int newPhase = 1;
            if (hpPct < 0.8f) newPhase = 2;
            if (hpPct < 0.6f) newPhase = 3;
            if (hpPct < 0.4f) newPhase = 4;
            if (hpPct < 0.2f) newPhase = 5;
            if (newPhase > Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
            }

            // === EL FADE DE NACIMIENTO (y la invulnerabilidad del arribo) ===
            if (NPC.alpha > 0)
            {
                NPC.alpha = Math.Max(0, NPC.alpha - 2);
                if (NPC.alpha == 0) NPC.dontTakeDamage = false;
            }

            // === EL MOTOR DE ESTADOS ===
            _tickEstado++;
            switch (_estado)
            {
                case EST_NACIENDO: EstadoNaciendo(target); break;
                case EST_FLOTAR: EstadoFlotar(target); break;
                case EST_JUICIO: EstadoJuicio(target); break;
                case EST_RAYO: EstadoRayo(target); break;
                case EST_NOVA: EstadoNova(target); break;
                case EST_CRUZ: EstadoCruz(target); break;
                case EST_DESTELLO: EstadoDestello(target); break;
                case EST_ECLIPSE: EstadoEclipse(target); break;
            }

            // === LOS ATAQUES DE FONDO (las runas recuerdan — heredado) ===
            switch (Phase)
            {
                case 4: Fase4Runas(); break;
                case 5: Fase5FuriaFondo(target); break;
            }

            // === LA LUZ DEL MUNDO: la luz inunda (y en el eclipse MUERE) ===
            if (_rayo == 3f || _estado == EST_ECLIPSE)
                Lighting.AddLight(NPC.Center, new Vector3(0.10f, 0.06f, 0.16f)); // la luz apagada
            else
                Lighting.AddLight(NPC.Center, new Vector3(1.1f, 0.95f, 0.65f));  // el sol vivo

            // === EL CONTRATO MP (la casa): estado/subfase/tick/param viajan ===
            NPC.ai[0] = _estado;
            NPC.ai[1] = _rayo;          // 0 nada · 1 rayo cargando · 2 rayo ardiendo · 3 ECLIPSE
            NPC.ai[2] = _tickEstado;
            NPC.ai[3] = _angDestello;   // el rumbo del destello (la línea guía del cliente)

            // MP: la luz respira por el cable cada 12 ticks.
            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        // ==================================================================
        //  LA PREDICCIÓN ADAPTATIVA (heredada de la v6.50.34)
        // ==================================================================
        private Vector2 PredPresa(Player target)
        {
            float dist = Vector2.Distance(NPC.Center, target.Center);
            float lead = MathHelper.Clamp(dist / 35f, 10f, 34f);
            return target.Center + target.velocity * lead;
        }

        // ==================================================================
        //  LOS ESTADOS
        // ==================================================================

        /// <summary>LA LLEGADA: 3.3 s descendiendo del cielo encendido.</summary>
        private void EstadoNaciendo(Player target)
        {
            Vector2 punto = target.Center + new Vector2(0f, -420f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.10f);
            if (_tickEstado >= 200)
            {
                _estado = EST_FLOTAR;
                _tickEstado = 0;
                _sentidoOrbita = Main.rand.NextBool() ? 1 : -1;
            }
        }

        /// <summary>
        /// LA ÓRBITA SERENA: la luz pasea en elipse sobre la presa —
        /// la fase de leerla. Al agotarse la paciencia (o la quietud de
        /// la presa), LA LUZ ELIGE su próximo castigo — NUNCA el último.
        /// </summary>
        private void EstadoFlotar(Player target)
        {
            float velAng = (0.014f + Phase * 0.003f) * _sentidoOrbita;
            _angOrbita += velAng;
            float radio = Furia ? 320f : (Phase >= 4 ? 380f : 440f);
            Vector2 punto = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * radio,
                -260f + MathF.Sin(_angOrbita) * 90f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.08f, 0.14f);

            // EL VOLTEO (P3+, heredado): el suelo deja de ser tuyo.
            if (Phase >= 3)
            {
                _tickGravedad++;
                if (_tickGravedad >= 480)
                {
                    _tickGravedad = 0;
                    FlipGravity(target);
                }
            }

            // LA PACIENCIA (heredada): cada fase flota MENOS… y la presa
            // quieta la derrite (anti-camping: el castigo llega YA).
            int paciencia = Math.Max(50, 120 - (Phase - 1) * 14);
            if (_ticksPresaQuieta > 90) paciencia = Math.Min(paciencia, 26);

            if (_tickEstado >= paciencia)
            {
                ElegirAtaque(target);
            }
        }

        /// <summary>
        /// LA ROTACIÓN (heredada de la v6.50.34, la firma de la casa):
        /// el menú crece con la fase y NUNCA sale el mismo plato dos
        /// veces — la luz no tiene patrón, tiene humor.
        /// </summary>
        private void ElegirAtaque(Player target)
        {
            // el menú de la fase
            int[] menu = Phase switch
            {
                1 => new[] { EST_JUICIO, EST_RAYO },
                2 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA },
                3 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO },
                4 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_ECLIPSE },
                _ => new[] { EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_ECLIPSE, EST_JUICIO },
            };
            int elegido = menu[Main.rand.Next(menu.Length)];
            if (elegido == _ultimoAtaque && menu.Length > 1)
                elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            _ultimoAtaque = elegido;

            _estado = elegido;
            _tickEstado = 0;
            NPC.netUpdate = true;

            if (elegido == EST_DESTELLO)
            {
                _cadenasDestello = 1;
                PrepararDestello(target);
            }
        }

        // ==================================================================
        //  EL JUICIO DE LUZ — LAS COLUMNAS DEL CIELO
        // ==================================================================

        /// <summary>
        /// LA PRIMERA LEY: la luz cae. 30 t de alzarse (el anuncio) y
        /// N COLUMNAS nacen ARRIBA de las posiciones PREDICHAS — el
        /// proyectil se telegrafea solo: cae LENTO (la línea de luz
        /// descendiendo) y a los 45 t ACELERA a toda velocidad. Donde
        /// estabas parado, ya no existe.
        /// </summary>
        private void EstadoJuicio(Player target)
        {
            // se ALZA sobre la órbita: el juez mira desde arriba.
            Vector2 punto = target.Center + new Vector2(0f, -480f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 30)
            {
                int n = Math.Min(6, 2 + Phase);       // 3 en P1 → 6 en P5
                float sep = 190f - Phase * 10f;       // más apretado con la fase
                float centro = PredPresa(target).X;
                for (int i = 0; i < n; i++)
                {
                    float x = centro + (i - (n - 1) * 0.5f) * sep +
                              Main.rand.NextFloat(-30f, 30f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            new Vector2(x, target.Center.Y - 1200f), new Vector2(0f, 7f),
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.95f), 3f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloColumnaJuicio, 0f,
                            NPC.whoAmI * 61 + i);
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Juicio", OroLuz);
                OndaLib.Kick(8f, 16);
            }

            if (_tickEstado >= 100) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        // ==================================================================
        //  EL RAYO PRIMORDIAL — EL ARCO QUE TE BUSCA
        // ==================================================================

        /// <summary>
        /// EL HEREDERO DEL ALIENTO: 40 t de CARGA (el núcleo crece, los
        /// anillos aceleran) y 70 de FUEGO: el ARCO PerlinBolt GRUESO
        /// del núcleo al pecho de la presa (CINE en el PreDraw de cada
        /// máquina) + la lluvia DENSA de pernos (el daño real). En P5,
        /// al cargar, EL RECORDAR: las runas escupen tu propio estilo.
        /// </summary>
        private void EstadoRayo(Player target)
        {
            if (_rayo == 0)
            {
                _rayo = 1;
                _tickRayo = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                NPC.netUpdate = true;
            }
            RayoTick(target);

            if (_rayo == 0 && _tickEstado >= 120)
            {
                _estado = EST_FLOTAR;
                _tickEstado = 0;
            }
        }

        private void RayoTick(Player target)
        {
            if (_rayo == 0) return;
            _tickRayo++;

            if (_rayo == 1)
            {
                // LA CARGA: el núcleo se llena (el cliente dibuja el crescendo).
                if (_tickRayo >= 40)
                {
                    _rayo = 2;
                    _tickRayo = 0;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Aliento", VioletaLuz);
                    NPC.netUpdate = true;
                }
                return;
            }

            // EL FUEGO: la luz escupe pernos SOBRE la presa (denso y rápido).
            if ((_tickRayo % (Furia ? 3 : 4)) == 0 && _tickRayo <= 64)
            {
                Vector2 pos = PredPresa(target) + new Vector2(
                    Main.rand.NextFloat(-150f, 150f),
                    Main.rand.NextFloat(-320f, -120f));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, new Vector2(0f, 11f),
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                        NPC.whoAmI * 97 + _tickRayo);
                }
            }
            if (_tickRayo >= 70)
            {
                _rayo = 0;
                _tickRayo = 0;
                NPC.netUpdate = true;
            }
        }

        // ==================================================================
        //  LA NOVA PRIMORDIAL — LA CONTRACCIÓN Y EL ESTALLIDO
        // ==================================================================

        /// <summary>
        /// 45 t de contracción (el núcleo CRECE y se APRIETA — el pulso
        /// acelera en el render) y EL ESTALLIDO: 2-3 anillos de orbes
        /// con HUECOS (esquivable por diseño — hay que BUSCAR EL HUECO,
        /// el anuncio lo enseña). En la furia, el tercer anillo.
        /// </summary>
        private void EstadoNova(Player target)
        {
            if (_tickEstado < 45)
            {
                // LA CONTRACCIÓN: se detiene y se llena.
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.15f);
                if (_tickEstado == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Nova", OroLuz);
                    OndaLib.Kick(6f, 12);
                }
                return;
            }

            if (_tickEstado == 45)
            {
                // EL ESTALLIDO: anillos con huecos (4 gaps de 30°).
                int anillos = Furia ? 3 : (Phase >= 3 ? 3 : 2);
                for (int a = 0; a < anillos; a++)
                {
                    float vel = 6.5f + a * 2.2f;
                    float desfase = a * MathHelper.Pi / 9f;
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f + desfase;
                        // LOS HUECOS: 4 sectores libres de 30° (la salida).
                        float grad = Math.Abs(MathHelper.WrapAngle(ang - desfase));
                        if (grad < 0.26f || Math.Abs(grad - MathHelper.PiOver2) < 0.26f ||
                            Math.Abs(grad - MathHelper.Pi) < 0.26f ||
                            Math.Abs(grad - MathHelper.Pi * 1.5f) < 0.26f) continue;
                        Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * vel;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                NPC.Center, v,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.60f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                                NPC.whoAmI * 71 + a * 19 + i);
                        }
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                OndaLib.Kick(11f, 22);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 90) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        // ==================================================================
        //  LA CRUZ DE LUZ — LOS CUATRO CHORROS GIRANDO (fase 3+)
        // ==================================================================

        /// <summary>
        /// LA ASPIRADORA: la luz se queda CASI quieta en su órbita y
        /// escupe CUATRO chorros de pernos en cruz — la cruz GIRA lento
        /// (0.011 rad/t: una vuelta cada ~9.5 s) y barre la arena. La
        /// única defensa es orbitarla a su ritmo.
        /// </summary>
        private void EstadoCruz(Player target)
        {
            // se aparta de la presa: la cruz necesita espacio.
            Vector2 lejos = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 520f, -300f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (lejos - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado >= 30 && (_tickEstado % (Furia ? 6 : 8)) == 0 && _tickEstado <= 200)
            {
                float giro = _tickEstado * 0.011f * (Furia ? 1.5f : 1f);
                for (int b = 0; b < 4; b++)
                {
                    float ang = giro + b * MathHelper.PiOver2;
                    Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 9f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, v,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                            NPC.whoAmI * 83 + _tickEstado + b * 7);
                    }
                }
                if ((_tickEstado % 24) == 0)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            }

            if (_tickEstado >= 240) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        // ==================================================================
        //  EL DESTELLO — LA EMBESTIDA A VELOCIDAD LUZ
        // ==================================================================

        /// <summary>Prepara un destello: rumbo a la presa PREDICHA.</summary>
        private void PrepararDestello(Player target)
        {
            Vector2 pred = PredPresa(target);
            _angDestello = (pred - NPC.Center).ToRotation();
            NPC.velocity *= 0.25f;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Carga", OroLuz);
            OndaLib.Kick(7f, 14);
            NPC.netUpdate = true;
        }

        /// <summary>
        /// EL HEREDERO DEL RAM: 20 t de LÍNEA GUÍA (el cliente dibuja el
        /// rayo de puntería creciendo) y EL CRUCE a 46 px/t A TRAVÉS de
        /// la posición predicha — con ESTELA de fantasmas. La CADENA
        /// (fase 3+): 2-3 destellos seguidos, cada uno con rumbo NUEVO
        /// (la vuelta en U de la v6.50.34, ahora en cualquier ángulo).
        /// </summary>
        private void EstadoDestello(Player target)
        {
            // === EL TELEGRAPH (0..20): la línea de luz que apunta ===
            if (_tickEstado <= 20)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.14f);
                if (_tickEstado == 1)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                return;
            }

            // === EL CRUCE: a velocidad luz por la línea ===
            float vel = 44f + Phase * 2.5f + (Furia ? 4f : 0f);
            Vector2 rumbo = new Vector2(MathF.Cos(_angDestello), MathF.Sin(_angDestello));
            NPC.velocity = Vector2.Lerp(NPC.velocity, rumbo * vel, 0.30f);

            // el cruce terminó: ¿OTRO destello o el descanso?
            bool cruzo = Vector2.Distance(NPC.Center, target.Center) > 1900f;
            if (_tickEstado > 90 || cruzo)
            {
                int maxCadena = Furia ? 3 : (Phase >= 3 ? 2 : 1);
                if (_cadenasDestello < maxCadena)
                {
                    _cadenasDestello++;
                    _tickEstado = 0;
                    PrepararDestello(target);   // rumbo NUEVO a la presa
                    return;
                }
                _estado = EST_FLOTAR;
                _tickEstado = 0;
            }
        }

        // ==================================================================
        //  EL ECLIPSE — LA LUZ SE APAGA (fase 4+)
        // ==================================================================

        /// <summary>
        /// EL ATAQUE FIRMA: la luz SE APAGA (ai[1]=3: el núcleo se
        /// oscurece en el render de cada máquina, la luz del mundo muere
        /// a un violeta tenue) y el campo se LLENA de orbes lentos que
        /// convergen sobre la presa — SOLO LAS BALAS BRILLAN. La
        /// atracción del núcleo muerto tira de ti hacia el cuerpo (la
        /// gravedad del agujero heredada). Al final: EL REGRESO — la
        /// luz VUELVE y estalla una nova GRATIS (el flash del alba).
        /// </summary>
        private void EstadoEclipse(Player target)
        {
            int duracion = Furia ? 200 : 260;
            _rayo = 3; // el contrato del eclipse (viaja en ai[1])

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, NPC.Center); // el apagón
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Eclipse", VioletaLuz);
                OndaLib.Kick(9f, 18);
            }

            // LA DERIVA LENTA: el cuerpo muerto se cierne.
            Vector2 punto = target.Center + new Vector2(0f, -340f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.06f);

            // LA ATRACCIÓN del cuerpo apagado (la herencia del agujero).
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player pl = Main.player[i];
                if (pl == null || !pl.active || pl.dead) continue;
                Vector2 aH = NPC.Center - pl.Center;
                float d = aH.Length();
                if (d > 900f || d < 60f) continue;
                float fuerza = 0.16f * (1f - d / 900f);
                pl.velocity += aH.SafeNormalize(Vector2.Zero) * fuerza;
            }

            // LAS BALAS: orbes LENTOS desde los bordes, convergiendo.
            if ((_tickEstado % 12) == 0 && _tickEstado < duracion - 40)
            {
                for (int o = 0; o < 2; o++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 borde = target.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * 820f;
                    Vector2 v = (target.Center - borde).SafeNormalize(Vector2.UnitY) * 3.4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            borde, v,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                            NPC.whoAmI * 89 + _tickEstado + o * 13);
                    }
                }
            }

            // EL REGRESO: la luz VUELVE — y trae una nova.
            if (_tickEstado >= duracion)
            {
                _rayo = 0;
                _tickEstado = 45;        // el estallido YA (EstadoNova salta al clímax)
                _estado = EST_NOVA;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                NPC.netUpdate = true;
            }
        }

        // ==================================================================
        //  LOS FONDOS DE FASE (heredados de la sierpe)
        // ==================================================================

        private void Fase4Runas()
        {
            _tickRunas++;
            if (_tickRunas >= 240 && ContarRunas() < 2)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        private void Fase5FuriaFondo(Player target)
        {
            _tickRunas++;
            if (_tickRunas >= 200 && ContarRunas() < 4)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        private void FlipGravity(Player player)
        {
            player.AddBuff(BuffID.Gravitation, 180);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, player.Center);
            OndaLib.Kick(6f, 12);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Gravedad", VioletaLuz);
        }

        // ==================================================================
        //  LAS RUNAS MEMORIZADAS (heredadas: la luz LEE tu estilo)
        // ==================================================================

        private static int ContarRunas()
        {
            int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == tipo &&
                    (int)p.ai[0] == AtaqueJefeProjectile.EstiloRunaMemorizada)
                    n++;
            }
            return n;
        }

        private void NacerRuna()
        {
            float fase = Main.rand.NextFloat(MathHelper.TwoPi);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.65f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloRunaMemorizada, fase, NPC.whoAmI * 97);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
        }

        /// <summary>EL CAMBIO DE FASE (el respiro curita de la casa).</summary>
        private void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                OroLuz, Phase, PhaseName());
            NPC.netUpdate = true;
        }

        private string PhaseName() => Phase switch
        {
            1 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre1"),
            2 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre2"),
            3 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre3"),
            4 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre4"),
            5 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre5"),
            _ => "?",
        };

        // ==================================================================
        //  LA MUERTE — LA CONTRACCIÓN Y EL ESTALLIDO FINAL
        // ==================================================================

        /// <summary>
        /// CheckDead → false: la luz NO muere por el camino normal —
        /// COLAPSA: 120 t de contracción total (TODO el brillo se
        /// recoge hacia el punto — el render lo dibuja) y EL ESTALLIDO:
        /// el flash que inunda la pantalla y la lluvia dorada.
        /// </summary>
        public override bool CheckDead()
        {
            if (_muriendo) return false;
            _muriendo = true;
            _tickMuerte = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            _estado = EST_MURIENDO;
            _rayo = 0;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Muerte", OroLuz);
            NPC.netUpdate = true;
            return false;
        }

        private void CineMuerte()
        {
            _tickMuerte++;
            // LA CONTRACCIÓN: todo el brillo se recoge (el sol se hace punto).
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.10f);

            if (_tickMuerte >= 120)
            {
                OndaLib.Kick(14f, 30);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath55, NPC.Center);
                if (!Main.dedServ)
                {
                    for (int d = 0; d < 90; d++)
                    {
                        int idx = Dust.NewDust(NPC.Center, 160, 160, DustID.GoldFlame,
                            Main.rand.NextFloat(-13f, 13f), Main.rand.NextFloat(-15f, 4f));
                        Main.dust[idx].noGravity = true;
                    }
                }
                DropBotin();
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        /// <summary>EL BOTÍN (el método de siempre, llamado por el cine).</summary>
        private void DropBotin()
        {
            if (_yaDropeo) return;
            _yaDropeo = true;
            EsenciasModSistema.SoltarEsencia(NPC);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>(), 1);

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
                        sp.ResonanceShards += 250;
                        EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                            new Color(245, 196, 81), NPC.FullName, 250);
                        EcoRed.SincronizarCronica(player);
                    }
                }
            }
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reconocimiento", OroLuz);
        }

        /// <summary>Fallback exótico (si algo mata por fuera del cine).</summary>
        public override void OnKill()
        {
            if (_yaDropeo) return;
            DropBotin();
        }

        // ==================================================================
        //  EL ARTE — EL SOL DE CÓDIGO (100% píxeles de luz, cero sprites)
        //
        //  LA LECCIÓN DE LAS 3 MUERTES (dragón de código «no se parece
        //  en nada», dragón de sprites «se ve horrible», sierpe «se ve
        //  feo»): NO MÁS ANATOMÍA. La luz no tiene forma que salir mal
        //  — tiene BRILLO: núcleo, halo, rayos, coronas y chispas. Todo
        //  aditivo (la luz no proyecta silueta: IRRADIA), todo
        //  sincronizado (ai[0]/ai[1]/ai[2]/ai[3] — el contrato de la
        //  casa), todo el contrato de lote cerrado→cerrado→vanilla.
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 posC = NPC.Center - Main.screenPosition;
                float visibilidad = 1f - (NPC.alpha / 255f);
                bool eclipse = NPC.ai[1] == 3f || (_muriendo && _tickMuerte < 120);
                float faseInt = 0.55f + 0.45f * (Phase - 1) / 4f;   // la furia brilla más

                // === LA MUERTE: la contracción (todo hacia el punto) ===
                float colapso = 1f;
                if (_muriendo) colapso = Math.Max(0.05f, 1f - _tickMuerte / 110f);

                // === EL ECLIPSE: el disco oscuro (el cuerpo muerto de la luz) ===
                if (eclipse)
                {
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    try
                    {
                        float pulsoE = 0.90f + 0.10f * MathF.Sin(t * 0.8f);
                        spriteBatch.Draw(VFXCore.SoftGlow, posC, null,
                            new Color(16, 8, 30) * (0.96f * visibilidad),
                            0f, new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                            new Vector2(2.2f * pulsoE, 2.2f * pulsoE), SpriteEffects.None, 0f);
                    }
                    finally { spriteBatch.End(); }
                }

                // === EL SOL (lote aditivo — TODO lo que brilla) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                    float brillo = eclipse ? 0.10f : (faseInt * colapso);

                    // === 1. EL VELO (el aura violeta de la profundidad) ===
                    LumenLib.Bloom(spriteBatch, posC, 360f, VioletaLuz,
                        0.10f * brillo * visibilidad, 2);

                    // === 2. EL HALO DORADO (la corona del sol) ===
                    LumenLib.BloomPulse(spriteBatch, posC, 200f * latido, OroLuz,
                        0.50f * brillo * visibilidad, t, 1.6f);

                    // === 3. LOS RAYOS RADIALES (la rueda de luz girando) ===
                    if (!eclipse)
                    {
                        int nRayos = 8 + Phase * 2;                       // 10 en P1 → 18 en P5
                        float giro = t * 0.10f;
                        Vector2 origen = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        for (int i = 0; i < nRayos; i++)
                        {
                            float ang = giro + i * MathHelper.TwoPi / nRayos;
                            float largo = (170f + 110f * faseInt) *
                                (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f)) * colapso;
                            float ancho = 26f - 10f * faseInt;
                            Color cR = (Phase >= 4) ?
                                Color.Lerp(OroLuz, VioletaLuz, 0.45f) : OroLuz;
                            spriteBatch.Draw(VFXCore.SoftGlow, posC, null,
                                cR * (0.30f * brillo * visibilidad), ang, origen,
                                new Vector2(largo / VFXCore.SoftGlow.Width * 2f,
                                            ancho / VFXCore.SoftGlow.Height * 2f),
                                SpriteEffects.None, 0f);
                        }
                    }

                    // === 4. LAS DOS CORONAS DE PERLAS (los anillos de
                    //     Saturno: órbitas elípticas de blooms girando
                    //     en sentidos opuestos — el efecto 3D) ===
                    for (int anillo = 0; anillo < 2; anillo++)
                    {
                        float rx = (anillo == 0 ? 150f : 205f) * colapso;
                        float ry = (anillo == 0 ? 92f : 62f) * colapso;
                        float w = anillo == 0 ? 0.55f : -0.38f;          // sentidos opuestos
                        Color cP = anillo == 0 ? OroLuz : VioletaLuz;
                        int nPerlas = 13;
                        for (int i = 0; i < nPerlas; i++)
                        {
                            float ang = t * w + i * MathHelper.TwoPi / nPerlas;
                            Vector2 perla = posC + new Vector2(MathF.Cos(ang) * rx,
                                MathF.Sin(ang) * ry);
                            float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                            LumenLib.Bloom(spriteBatch, perla, 12f * colapso, cP,
                                (eclipse ? 0.06f : 0.42f) * (0.5f + 0.5f * tw) *
                                brillo * visibilidad, 2);
                        }
                    }

                    // === 5. LAS CHISPAS ORBITANTES (el enjambre cercano) ===
                    int nChispas = 5 + Phase;
                    Color cMota = Phase >= 4 ? VioletaLuz : OroLuz;
                    for (int m = 0; m < nChispas; m++)
                    {
                        float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                        float r = 120f * colapso + 12f * MathF.Sin(t * 2.4f + m);
                        Vector2 mota = posC + new Vector2(MathF.Cos(ang) * r,
                            MathF.Sin(ang) * r * 0.72f);
                        LumenLib.Bloom(spriteBatch, mota, 14f * colapso, cMota,
                            (eclipse ? 0.05f : 0.40f) * brillo * visibilidad, 2);
                    }

                    // === 6. EL NÚCLEO (el corazón blanco de Aethon) ===
                    float tamNucleo = 88f * latido * colapso;
                    if (eclipse)
                    {
                        // EL ECLIPSE: solo el RIM dorado del cuerpo muerto.
                        LumenLib.Bloom(spriteBatch, posC, 74f, OroLuz,
                            0.10f * visibilidad, 2);
                        LumenLib.Bloom(spriteBatch, posC, 30f, new Color(120, 80, 190),
                            0.12f * visibilidad, 2);
                    }
                    else
                    {
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo, NucleoBlanco,
                            1.0f * brillo * visibilidad, 3);
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo * 0.42f, OroLuz,
                            0.65f * brillo * visibilidad, 2);
                    }

                    // === 7. LA CARGA DEL RAYO (el crescendo del núcleo) ===
                    if (NPC.ai[1] == 1f && !_muriendo)
                    {
                        float prog = Math.Min(1f, _tickRayoLocal / 40f);
                        LumenLib.Bloom(spriteBatch, posC, 60f + 90f * prog,
                            NucleoBlanco, 0.45f * prog * visibilidad, 3);
                    }

                    // === 8. LOS TELEGRAPHS (la casa: todo ataque se anuncia) ===
                    // EL DESTELLO: la LÍNEA GUÍA — el rayo de puntería
                    // creciendo en el rumbo del cruce (ai[3] = el ángulo).
                    if (NPC.ai[0] == EST_DESTELLO && !_muriendo && NPC.ai[2] <= 20f)
                    {
                        float prog = NPC.ai[2] / 20f;
                        float angD = NPC.ai[3];
                        Vector2 dirD = new Vector2(MathF.Cos(angD), MathF.Sin(angD));
                        Vector2 origenL = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        float largoL = 1900f * prog;
                        spriteBatch.Draw(VFXCore.SoftGlow, posC + dirD * (largoL * 0.5f), null,
                            OroLuz * (0.30f + 0.25f * prog), angD, origenL,
                            new Vector2(largoL / VFXCore.SoftGlow.Width,
                                        (7f + 9f * prog) / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);
                        // EL ANILLO que se cierra (el compás del cruce).
                        OndaLib.Pulse(spriteBatch, posC, prog, 220f, OroLuz, 0.55f, NPC.whoAmI);
                    }

                    // LA NOVA: la contracción (el pulso que se APRIETA).
                    if (NPC.ai[0] == EST_NOVA && !_muriendo && NPC.ai[2] <= 45f)
                    {
                        float prog = NPC.ai[2] / 45f;
                        OndaLib.Pulse(spriteBatch, posC, 1f - prog,
                            420f - 300f * prog, OroLuz, 0.60f, NPC.whoAmI + 3);
                    }

                    // === 9. LA ESTELA DEL DESTELLO (los fantasmas del cruce) ===
                    if (NPC.ai[0] == EST_DESTELLO && NPC.ai[2] > 20f && !_muriendo &&
                        NPC.velocity.LengthSquared() > 400f)
                    {
                        Vector2 atras = -Vector2.Normalize(NPC.velocity);
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = posC + atras * (g * 52f);
                            LumenLib.Bloom(spriteBatch, fantasma, 44f - g * 6f,
                                OroLuz, 0.26f / g * visibilidad, 2);
                        }
                    }

                    // === 10. LA MUERTE: EL ESTALLIDO FINAL (el flash) ===
                    if (_muriendo && _tickMuerte >= 110 && _tickMuerte <= 135)
                    {
                        float fp = (_tickMuerte - 110) / 25f;   // 0→1: la inundación
                        LumenLib.Bloom(spriteBatch, posC,
                            600f + 2600f * fp, NucleoBlanco,
                            0.9f * (1f - fp * 0.6f), 3);
                        LumenLib.Bloom(spriteBatch, posC,
                            400f + 1800f * fp, OroLuz,
                            0.6f * (1f - fp * 0.5f), 2);
                    }
                }
                finally { spriteBatch.End(); }

                // === EL RAYO PRIMORDIAL (PerlinBolt — el arco de la casa;
                //     corre en TODAS las máquinas leyendo ai[1]) ===
                if (NPC.ai[1] != _rayoPrevio)
                {
                    _rayoPrevio = NPC.ai[1];
                    _tickRayoLocal = 0f;
                }
                else if (NPC.ai[1] != 0f) _tickRayoLocal++;

                if (!_muriendo && (NPC.ai[1] == 1f || NPC.ai[1] == 2f))
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        bool cargando = NPC.ai[1] == 1f;
                        float prog = cargando ? Math.Min(1f, _tickRayoLocal / 40f) : 1f;
                        Vector2 nucleo = NPC.Center - Main.screenPosition;
                        Vector2 pecho = presa.Center - Main.screenPosition;
                        float ancho = (1.2f + 3.4f * prog) * (cargando ? 0.6f : 1f);
                        int flick = (int)(Main.GameUpdateCount / 3u);
                        StormLib.PerlinBolt(spriteBatch, nucleo, pecho,
                            NPC.whoAmI * 71 + 13, flick, ancho, OroLuz, NucleoBlanco,
                            (cargando ? 0.35f : 0.95f) * visibilidad);
                    }
                }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false; // el sol de código se dibuja a sí mismo
        }

        // EL TEMPO LOCAL del rayo (el cliente cuenta su propio tick para
        // el crescendo — ai[1] viaja por paquetes cada 12 ticks).
        private float _tickRayoLocal = 0f;
        private float _rayoPrevio = 0f;
    }
}
