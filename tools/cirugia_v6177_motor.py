#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_motor.py — LA ESCALERA SIN TECHO + LAS VOLEAS TEMÁTICAS VANILLA.

v6.177 (la letra):
  · «parecen que los jefes en niveles altos dentro del mundo interior
    tienen el mismo ataque de proyectiles luminoso, eso esta mal, el
    propio terraria cuenta con proyectiles originales que pueden ser
    buenos para esos jefes segun el tema, un ejemplo es el
    Devoramundos, en su bioma estan los devoralmas que lanzan un
    proyectil especial, el Devoramundos puede usar esos proyectiles» —
    CADA jefe dispara SUS proyectiles de Terraria (verificados en el
    decompile: CursedFlameHostile para el gusano de la corrupción ·
    Stinger para la abeja · DeathLaser para los mecánicos · SeedPlantera
    · SharknadoBolt · HallowBossRainbowStreak · PhantasmalEye ·
    DeerclopsRangedProjectile · CultistBossFireBall…). El patrón de la
    volea (abanico · anillo · lluvia · espiral) es de la casa; el
    PROYECTIL es de Terraria — cada jefe con su propia temática.
  · «en el mundo interior los jefes no necesitan estar limitados a
    nivel 10, que este nivel sea infitino y cuanto mas nivels mas
    complicado, mas inteligente y tiene nuevos ataques, combos y
    nuevos movimientos, asi como una IA mas avanzada» — LA ESCALERA
    SIN TECHO: nivel 8+ EL DASH DOBLE · 10+ LA VOLEA DOBLE · 12+ EL
    SALTO DE SOMBRA (teletransporte detrás de la presa con telégrafo)
    · 15+ EL COMBO DE CUATRO PASOS · 20+ LA FURIA (todo 20% más
    rápido). El nivel ya no tiene tope en ninguna parte.
"""
# ══ 1. JefeInteriorVoleaProjectile: EL MAPA TEMÁTICO VANILLA ════════
R1 = 'AethonMod/Content/Projectiles/Jefes/JefeInteriorVoleaProjectile.cs'
s = open(R1, encoding='utf-8').read()

viejo = """        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";"""
nuevo = """        // ============================================================
        //  v6.177 — EL MAPA TEMÁTICO VANILLA (la letra: «parecen que los
        //  jefes en niveles altos dentro del mundo interior tienen el
        //  mismo ataque de proyectiles luminoso, eso esta mal, el propio
        //  terraria cuenta con proyectiles originales que pueden ser
        //  buenos para esos jefes segun el tema, un ejemplo es el
        //  Devoramundos, en su bioma estan los devoralmas que lanzan un
        //  proyectil especial»): CADA jefe dispara SUS proyectiles de
        //  Terraria — verificados contra el decompile del tML real:
        //   · El gusano de la CORRUPCIÓN escupe la LLAMA MALDITA
        //     (CursedFlameHostile — la corrupción de los devoralmas)
        //   · El Cerebro, el ICHOR de la carmesí (IchorSplash)
        //   · La Abeja, su AGUIJÓN (Stinger) y el de avispas
        //   · Esqueletrón, la CALAVERA maldita (Skull)
        //   · La Muralla, la BOLA DE FUEGO del infierno (Fireball)
        //   · Los MECÁNICOS: el LÁSER DE LA MUERTE (Retinazer, el
        //     Destructor) · la llama maldita (Spazmatism) · la BOMBA
        //     del Prime (BombSkeletronPrime)
        //   · Plantera, sus SEMILLAS y las VENENOSAS
        //   · Gólem, su bola de fuego solar
        //   · el Duque, el RAYO DEL TIBURÓN (SharknadoBolt)
        //   · la Emperatriz, su ESTELA ARCOÍRIS (HallowBossRainbowStreak)
        //   · el Señor de la Luna: el OJO FANTASMAL y la ESFERA
        //     (PhantasmalEye · PhantasmalSphere)
        //   · Deerclops, su FRAGMENTO DE HIELO (DeerclopsRangedProjectile)
        //   · el Sectario, su BOLA DE FUEGO y su ORBE DE RAYOS
        //  (El patrón — abanico · anillo · lluvia · espiral — sigue
        //  siendo de la casa; el PROYECTIL es de Terraria. Los jefes
        //  sin mapa — los del mod — siguen con el proyectil de la casa.)
        // ============================================================
        private static readonly int _fallback = 0;

        /// <summary>EL PROYECTIL TEMÁTICO de un jefe (su propio vanilla
        /// — 0 = el proyectil de la casa).</summary>
        public static int ProyectilTematico(int tipoJefe, int patron)
        {
            int id = tipoJefe switch
            {
                NPCID.KingSlime => ProjectileID.Spike,
                NPCID.EyeofCthulhu => ProjectileID.EyeLaser,
                NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail
                    => ProjectileID.CursedFlameHostile,
                NPCID.BrainofCthulhu => ProjectileID.IchorSplash,
                NPCID.QueenBee => patron == 2 ? ProjectileID.HornetStinger : ProjectileID.Stinger,
                NPCID.SkeletronHead => ProjectileID.Skull,
                NPCID.WallofFlesh => ProjectileID.Fireball,
                NPCID.QueenSlimeBoss => ProjectileID.Spike,
                NPCID.Retinazer => ProjectileID.DeathLaser,
                NPCID.Spazmatism => ProjectileID.CursedFlameHostile,
                NPCID.TheDestroyer => ProjectileID.DeathLaser,
                NPCID.SkeletronPrime => ProjectileID.BombSkeletronPrime,
                NPCID.Plantera => patron == 1
                    ? ProjectileID.PoisonSeedPlantera : ProjectileID.SeedPlantera,
                NPCID.Golem => ProjectileID.Fireball,
                NPCID.DukeFishron => ProjectileID.SharknadoBolt,
                NPCID.HallowBoss => ProjectileID.HallowBossRainbowStreak,
                NPCID.MoonLordCore or NPCID.MoonLordHead or NPCID.MoonLordHand
                    => patron == 1 ? ProjectileID.PhantasmalSphere : ProjectileID.PhantasmalEye,
                NPCID.Deerclops => ProjectileID.DeerclopsRangedProjectile,
                NPCID.CultistBoss => patron == 3
                    ? ProjectileID.CultistBossLightningOrbArc : ProjectileID.CultistBossFireBall,
                _ => 0,
            };
            return id;
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";"""
assert viejo in s, 'la textura de la volea no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R1, 'w', encoding='utf-8', newline='\n').write(s)
print('OK JefeInteriorVoleaProjectile: el mapa temático vanilla')

# ══ 2. JefeInteriorGlobalNPC: el disparo usa el mapa + LA ESCALERA ══
R2 = 'AethonMod/Content/Globals/JefeInteriorGlobalNPC.cs'
s = open(R2, encoding='utf-8').read()

# 2a — DispararVolea: el proyectil es el TEMÁTICO (vanilla) o el de la casa
viejo = """                int tipo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile>();
                int tema = global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile.TemaDe(npc.type);"""
nuevo = """                // v6.177 — EL PROYECTIL TEMÁTICO (la letra: cada jefe con SU
                // proyectil de Terraria — el mismo ataque luminoso para
                // todos MUERE): el mapa devuelve el vanilla del jefe; 0 =
                // el proyectil de la casa (los jefes del mod y el default)
                int tipoCasa = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile>();
                int tipo = global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile
                    .ProyectilTematico(npc.type, patronForzado >= 0 ? patronForzado : 2);
                if (tipo == 0) tipo = tipoCasa;
                int tema = global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile.TemaDe(npc.type);"""
assert viejo in s, 'el disparo de la volea no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2b — LOS CAMPOS DE LA ESCALERA (junto a los demás del motor)
viejo = """        private int _comboT;              // reloj interno del paso
        private bool _motorExterno;       // lo usan también los jefes de OLEADA"""
nuevo = """        private int _comboT;              // reloj interno del paso
        private bool _motorExterno;       // lo usan también los jefes de OLEADA

        // === v6.177 — LA ESCALERA SIN TECHO (la letra: «que este nivel
        //     sea infitino y cuanto mas nivels mas complicado, mas
        //     inteligente y tiene nuevos ataques, combos y nuevos
        //     movimientos, asi como una IA mas avanzada») ===
        private bool _dashDoble;          // nivel 8+: la segunda embestida
        private int _volea2Cd;            // nivel 10+: la volea doble
        private int _saltoCd;             // nivel 12+: el salto de sombra
        private int _saltoTele;           // el telégrafo del salto
        private int _saltoHacia;          // el jugador del salto"""
assert viejo in s, 'los campos del motor no se encontraron'
s = s.replace(viejo, nuevo, 1)

# 2c — LA FURIA + LA VOLEA DOBLE + EL SALTO DE SOMBRA en el motor
viejo = """                // === EL COMBO (nivel 6+): tres pasos encadenados ===
                if (nivel >= 6)
                {
                    if (_comboPaso > 0) { EjecutarPasoDeCombo(npc, presa, nivel); return; }
                    if (_comboCd > 0) _comboCd--;
                    else
                    {
                        _comboPaso = 1; _comboT = 0;
                        _comboCd = Math.Max(210, 480 - 18 * nivel);   // nivel 10: cada ~5 s
                        SonarEn(npc, SoundID.Roar, -0.25f, 0.55f);    // el rugido del combo
                        npc.netUpdate = true;
                    }
                }"""
nuevo = """                // ============================================================
                //  v6.177 — LA ESCALERA SIN TECHO (la letra: nivel infinito
                //  con nuevos ataques, combos y movimientos):
                //   · NIVEL 8+: EL DASH DOBLE — cerrada la embestida, si la
                //     presa sigue en rango, UNA SEGUNDA inmediata con
                //     telégrafo corto.
                //   · NIVEL 10+: LA VOLEA DOBLE — dos voleas seguidas de
                //     patrones DISTINTOS (abanico→anillo).
                //   · NIVEL 12+: EL SALTO DE SOMBRA — el jefe se desvanece
                //     y reaparece DETRÁS de la presa (el telégrafo del
                //     salto avisa la línea).
                //   · NIVEL 15+: EL COMBO DE CUATRO PASOS (ver
                //     EjecutarPasoDeCombo).
                //   · NIVEL 20+: LA FURIA — todo se acelera 20%.
                // ============================================================
                bool furia = nivel >= 20;
                float acelera = furia ? 0.8f : 1f;

                // === EL COMBO (nivel 6+): tres pasos encadenados (4 con 15+) ===
                if (nivel >= 6)
                {
                    if (_comboPaso > 0) { EjecutarPasoDeCombo(npc, presa, nivel); return; }
                    if (_comboCd > 0) _comboCd--;
                    else
                    {
                        _comboPaso = 1; _comboT = 0;
                        _comboCd = (int)(Math.Max(210, 480 - 18 * nivel) * acelera);
                        SonarEn(npc, SoundID.Roar, -0.25f, 0.55f);    // el rugido del combo
                        npc.netUpdate = true;
                    }
                }

                // === EL SALTO DE SOMBRA (nivel 12+): un movimiento NUEVO ===
                if (nivel >= 12 && puedeDashar)
                {
                    if (_saltoTele > 0)
                    {
                        // EL TELÉGRAFO: la línea del salto avisa
                        _saltoTele--;
                        npc.velocity *= 0.70f;
                        if (_saltoTele == 0)
                        {
                            Player victima = _saltoHacia >= 0 && _saltoHacia < Main.maxPlayers
                                ? Main.player[_saltoHacia] : null;
                            if (victima != null && victima.active && !victima.dead)
                            {
                                // EL SALTO: reaparece DETRÁS de la presa
                                Vector2 detras = victima.Center -
                                    victima.DirectionTo(npc.Center) * 190f;
                                npc.Center = detras;
                                npc.velocity = -victima.DirectionTo(npc.Center) * 6f;
                                npc.netUpdate = true;
                                SonarEn(npc, SoundID.Item8, 0.35f, 0.6f);
                                if (furia) { _saltoCd = (int)(190 * 0.8f); }
                            }
                        }
                    }
                    else if (_saltoCd > 0) _saltoCd--;
                    else if (Vector2.Distance(npc.Center, presa.Center) > 380f)
                    {
                        _saltoCd = Math.Max(340, 700 - 16 * nivel);
                        _saltoTele = 18;
                        _saltoHacia = presa.whoAmI;
                        SonarEn(npc, SoundID.Item117, -0.5f, 0.55f);
                        npc.netUpdate = true;
                    }
                }"""
assert viejo in s, 'el combo del motor no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2d — EL DASH DOBLE (nivel 8+): al cerrar la embestida, una segunda
viejo = """                    if (_dashVentana > 0)
                    {
                        // LA EMBESTIDA: la velocidad la manda el MOTOR (el
                        // PostAI corre DESPUÉS de la IA: su palabra es la
                        // última — el dash SIEMPRE gana este tick)
                        _dashVentana--;
                        float velocidad = Math.Min(15.5f, 8.5f + 0.5f * (nivel - 2));
                        npc.velocity = _dashRumbo * velocidad;
                        if (_dashVentana % 4 == 0) npc.netUpdate = true;
                    }"""
nuevo = """                    if (_dashVentana > 0)
                    {
                        // LA EMBESTIDA: la velocidad la manda el MOTOR (el
                        // PostAI corre DESPUÉS de la IA: su palabra es la
                        // última — el dash SIEMPRE gana este tick)
                        _dashVentana--;
                        float velocidad = Math.Min(15.5f, 8.5f + 0.5f * (nivel - 2));
                        npc.velocity = _dashRumbo * velocidad;
                        if (_dashVentana % 4 == 0) npc.netUpdate = true;
                        // v6.177 — EL DASH DOBLE (nivel 8+): cerrada la
                        // embestida, si la presa sigue en rango, la SEGUNDA
                        // embestida NACE INMEDIATA con telégrafo corto
                        if (_dashVentana == 0 && nivel >= 8 && !_dashDoble &&
                            Vector2.Distance(npc.Center, presa.Center) > 120f)
                        {
                            _dashDoble = true;
                            _dashTele = 8;
                            npc.netUpdate = true;
                        }
                        else if (_dashVentana == 0) _dashDoble = false;
                    }"""
assert viejo in s, 'la embestida no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2e — LA VOLEA DOBLE (nivel 10+): el disparo agenda la segunda volea
viejo = """                    if (_voleaCd > 0) _voleaCd--;
                    else
                    {
                        _voleaCd = Math.Max(70, 210 - 10 * (nivel - 3));
                        DispararVolea(npc, presa, nivel, patronForzado: -1, extra: 0);
                    }"""
nuevo = """                    if (_voleaCd > 0) _voleaCd--;
                    else
                    {
                        _voleaCd = (int)(Math.Max(70, 210 - 10 * (nivel - 3)) * acelera);
                        DispararVolea(npc, presa, nivel, patronForzado: -1, extra: 0);
                        // v6.177 — LA VOLEA DOBLE (nivel 10+): la segunda
                        // volea vuela con el patrón CONTRARIO (abanico→anillo)
                        if (nivel >= 10) _volea2Cd = 26;
                    }
                    // LA SEGUNDA VOLEA del par (cuando está agendada)
                    if (_volea2Cd > 0 && --_volea2Cd == 0)
                    {
                        DispararVolea(npc, presa, nivel, patronForzado: 1, extra: 2);
                        SonarEn(npc, SoundID.Item122, 0.2f, 0.45f);
                    }"""
assert viejo in s, 'la volea no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2f — EL COMBO DE CUATRO PASOS (nivel 15+)
viejo = """                    if (_comboT > 30) { _comboPaso = 0; _comboT = 0; }
                    break;
            }
            npc.netUpdate = true;"""
nuevo = """                    // v6.177 — EL CUARTO PASO (nivel 15+): EL DASH FINAL
                    // CERZERO — la embestida que cierra el combo largo
                    if (_comboT > 30)
                    {
                        if (nivel >= 15) { _comboPaso = 4; _comboT = 0; }
                        else { _comboPaso = 0; _comboT = 0; }
                    }
                    break;
                case 4: // v6.177 — EL DASH FINAL: corto y certero
                    if (_comboT == 1) { _dashTele = 8; SonarEn(npc, SoundID.Item74, -0.3f, 0.5f); }
                    if (_dashTele > 0)
                    {
                        _dashTele--;
                        npc.velocity *= 0.70f;
                        if (_dashTele == 0)
                        {
                            _dashVentana = 14;
                            _dashRumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                            SonarEn(npc, SoundID.Item122, 0.45f, 0.7f);
                        }
                    }
                    else if (_dashVentana > 0)
                    {
                        _dashVentana--;
                        npc.velocity = _dashRumbo * Math.Min(17f, 10f + 0.5f * (nivel - 2));
                    }
                    else if (_comboT > 30) { _comboPaso = 0; _comboT = 0; }
                    break;
            }
            npc.netUpdate = true;"""
assert viejo in s, 'el paso 3 del combo no se encontró'
s = s.replace(viejo, nuevo, 1)

open(R2, 'w', encoding='utf-8', newline='\n').write(s)
print('OK JefeInteriorGlobalNPC: disparo temático + escalera sin techo')

# ══ 3. PilarDelLibro: el nivel del poder SIN TOPE en la vitrina ════
R3 = 'AethonMod/Content/UI/LibroDelEternoUI.cs'
s = open(R3, encoding='utf-8').read()
viejo = "                    int nivelPoder = Math.Min(10, 1 + kills);"
nuevo = """                    // v6.177 — SIN TOPE (la letra: «que este nivel sea
                    // infitino»): la estatura del jefe es la REAL — 1+kills
                    int nivelPoder = 1 + kills;"""
assert viejo in s, 'el nivelPoder no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R3, 'w', encoding='utf-8', newline='\n').write(s)
print('OK LibroDelEternoUI: el nivel del poder sin tope')
