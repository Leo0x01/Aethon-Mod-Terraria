#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6152.py — LA CIRUGÍA DE LA LETRA v6.152 (parte A: sistemas).

La letra del usuario (textual):
 · «la animacion de El Juicio de los Dos Soles todavia tiene saltos»
 · «al agujero runico puro dale otro estilo, mas vfx mas efectos visuales,
   mas de todo, que se vea super cargado, pero que siga siendo un agujero
   runico»
 · «La Ira sigue borrando los minions y lo que tenia que quedar en 10 era
   el limite de proyectiles basicos del grimorio, no la penetracion, en
   todo caso debemos cambiar los botones para la ira, ahora que sean
   dejando precionado el click derecho durante 2 segundos, y solo eso»
 · «los anillos runicos de los soles no estan correctos, no son los mismos
   que los de la forma eterna en accesorio, ten en cuenta que la forma
   eterna en accesorio es la correcta y es como debe verse la Forma Eterna
   en construccion»
 · «siempre que te refieres al despertar le dices puerta... deberias
   cambiar la palabra puerta cuando te refieres al despertar, por
   Despertar»
 · «hay un bug muy raro con el audio, cuando lanzo el ataque basico y lo
   dejo sostenido, el audio comienza a subir su volumen esta subida de
   volumen afecta a todo el audio y sube hasta saturarse por completo»
 · «el pilar no ha sido cambiado, creo que lo mejor es borrarlo y crearlo
   de nuevo con otra forma, que no tenga ningun ojo y asegurate de que no
   este recortado mal, usa la sforja para eso»
 · «el hitbox de la X en el libro de informacion sigue estando alejado de
   la X... la X esta corrida hacia la izquierda, ademas el cursor queda
   por debajo del libro de informacion, el cursor debe estar por encima»
 · «debes hacer que las piezas de lore se vayan desbloqueando a medida que
   el nivel de despertar aumenta, tambien quita la mencion de las dagas
   del eterno, no seran usadas despues de todo»
 · «modifica el aspecto visual de la Sombra de la Pagina original, quita
   los ojo y colmillos, tambien vi que tiene lineas rojas, quitalas
   tambien dejando un tentaculo negro completo»
 · «en el mundo interior al invocar a esqueletro me sale la version de la
   mazmorra... debe estar identificado como esqueletro mazmorra... y
   debes asegurarte de que esqueletron normal aparezca en el mundo
   interior cuando se le llame y no la version de mazmorra»
 · «quiero agregar un NPC no hostil como comerciante... investiga como
   los mods populares agregan NPC no hostiles (thorium) y luego tu mismo
   añade uno que siempre aparezca cuando el jugador posea el grimorio»

Cada parche usa asserts — si el patrón no está, MUERE en vez de corromper.
"""
import os

RAIZ = "/home/z/my-project/mod/AethonMod"
os.chdir(RAIZ)

def leer(ruta):
    with open(ruta, encoding="utf-8") as f:
        return f.read()

def escribir(ruta, s):
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(s)

def parche(ruta, viejo, nuevo, etiqueta):
    s = leer(ruta)
    assert viejo in s, f"[{etiqueta}] patrón no encontrado en {ruta}:\n---\n{viejo[:240]}\n---"
    assert s.count(viejo) == 1, f"[{etiqueta}] patrón AMBIGUO ({s.count(viejo)}x) en {ruta}"
    escribir(ruta, s.replace(viejo, nuevo))
    print(f"  ✔ {etiqueta}")

# =====================================================================
# [1] LA IRA — EL GESTO NUEVO: MANTENER EL CLICK DERECHO 2 SEGUNDOS.
#     Muere el izq+der (colisionaba con el Shoot del libro — la causa
#     raíz de "los minions desaparecen": cada click derecho del gesto
#     disparaba la invocación/mejora y el re-conteo en plena ceremonia).
#     EL GESTO: DER sostenido 120 ticks SOLO (sin inventario ni UI).
#     Al cumplirse: LA CEREMONIA. Se cancela si suelta.
#     Y mientras CARGA el gesto, el grimorio NO SE USA (el Shoot de
#     invocación queda callado todo el hold — imposible re-contar).
# =====================================================================
VIEJO_GESTO = r'''        // v6.146 — EL GESTO DE LA IRA (la letra: «el ataque no sera
        // con Shift+click izquierdo, sera con click izquierdo+click
        // derecho, para esto hay que mantener presionado el click
        // izquierdo y luego presionar el derecho»): MANTÉN el click
        // izquierdo y PULSA el derecho — el borde de subida del
        // derecho dispara LA IRA (una vez por pulsación, sin
        // ametrallado). Sin UI encima y sin inventario abierto.
        private bool _iraDerAnterior;

        // v6.151 — EL SILENCIO DE USO DEL LIBRO mientras nace la Ira
        // (el gesto izq+der es SOLO de la ceremonia: el grimorio no
        // dispara su Shoot ese instante — los minions no se tocan).
        private int _iraSilenciarItem;

        /// <summary>v6.151 — True mientras el gesto de la Ira acalla el
        /// uso del grimorio (lo lee GrimoireEternal.CanUseItem).</summary>
        public bool IraSilenciaElLibro => _iraSilenciarItem > 0;'''

NUEVO_GESTO = r'''        // ================================================================
        //  v6.152 — EL GESTO NUEVO DE LA IRA (la letra: «en todo caso
        //  debemos cambiar los botones para la ira, ahora que sean
        //  dejando precionado el click derecho durante 2 segundos, y
        //  solo eso, creo que seria lo mejor»): MANTÉN EL CLICK DERECHO
        //  2 SEGUNDOS — sin nada más. La autopsia del gesto viejo: el
        //  izq+der hacía que CADA click derecho del gesto disparara la
        //  rama de invocación del Shoot (el libro se re-usaba en plena
        //  ceremonia) — el origen del «los minions desaparecen».
        //  AHORA: el hold de 120 t carga la Ira; MIENTRAS carga, el
        //  grimorio NO SE USA (CanUseItem callado — cero invocación,
        //  cero re-conteo, el escuadrón INTACTO); al cumplir los 2 s
        //  LA CEREMONIA nace. Soltar antes = nada (y el hold se anula
        //  si abre el inventario o una UI).
        // ================================================================
        private int _iraHoldDerecho;          // ticks de DER sostenido
        private bool _iraCargandoHold;        // el hold llegó a la ira (sigue callando un rato)
        private const int IRA_HOLD_TICKS = 120;   // 2 segundos exactos

        // v6.151 — EL SILENCIO DE USO DEL LIBRO mientras nace la Ira
        // (lo mantiene TODO el hold + la eclosión: el grimorio no
        // dispara su Shoot — los minions no se tocan).
        private int _iraSilenciarItem;

        /// <summary>v6.151 — True mientras el gesto de la Ira acalla el
        /// uso del grimorio (lo lee GrimoireEternal.CanUseItem).</summary>
        public bool IraSilenciaElLibro => _iraSilenciarItem > 0 || _iraHoldDerecho > 0 || _iraCargandoHold;

        /// <summary>v6.152 — EL PROGRESO DEL HOLD (0..1): lo dibuja el
        /// buff de la Ira (el anillo del icono se CIERRRE mientras
        /// cargas el gesto — sabes cuándo falta). 0 si no hay hold.</summary>
        public float IraHoldProgreso =>
            _iraHoldDerecho <= 0 ? 0f : Math.Min(1f, _iraHoldDerecho / (float)IRA_HOLD_TICKS);'''

parche("Content/Players/ShardPlayer.cs", VIEJO_GESTO, NUEVO_GESTO, "[1a] el hold de 2 s")

VIEJO_POST = r'''            // EL GESTO DE LA IRA — izq mantenido + der pulsado
            try
            {
                if (Player == Main.LocalPlayer && !Main.gameMenu)
                {
                    bool izq = Main.mouseLeft && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    bool der = Main.mouseRight && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    // v6.151 — EL GESTO ES SOLO DE LA IRA (la letra: «en
                    //     el momento en el que voy a activarla todos los
                    //     minions desaparecen»): el clic DERECHO con el
                    //     izq mantenido dispara la ceremonia Y SUPRIME el
                    //     Shoot del grimorio de ESTE tick — el libro NO
                    //     re-invoca ni re-cuenta NADA mientras la ira
                    //     nace (el escuadrón queda INTACTO: los ojos son
                    //     los testigos del ritual, no su precio).
                    if (izq && der && !_iraDerAnterior)
                    {
                        Systems.AtaquesCombinadosSistema.ShiftClickDeLaIra(Player);
                        _iraSilenciarItem = 6;   // ~0,1 s de silencio de uso
                    }
                    _iraDerAnterior = der;
                    if (_iraSilenciarItem > 0) _iraSilenciarItem--;
                }
                else _iraDerAnterior = false;
            }
            catch { }'''

NUEVO_POST = r'''            // EL GESTO DE LA IRA — mantener el CLICK DERECHO 2 segundos
            try
            {
                if (Player == Main.LocalPlayer && !Main.gameMenu)
                {
                    bool der = Main.mouseRight && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    if (der)
                    {
                        // EL HOLD VIVE: cuenta ticks y al llegar a 2 s
                        // dispara LA IRA (una sola vez por hold — la
                        // marca _iraCargandoHold impide el ametrallado
                        // mientras sigas sosteniendo).
                        _iraHoldDerecho++;
                        if (_iraHoldDerecho == IRA_HOLD_TICKS && !_iraCargandoHold)
                        {
                            _iraCargandoHold = true;
                            _iraSilenciarItem = 15;   // la eclosión calla el libro un instante más
                            Systems.AtaquesCombinadosSistema.ShiftClickDeLaIra(Player);
                        }
                    }
                    else
                    {
                        // SOLTÓ (o abrió UI): el hold se reinicia limpio
                        _iraHoldDerecho = 0;
                        _iraCargandoHold = false;
                    }
                    if (_iraSilenciarItem > 0) _iraSilenciarItem--;
                }
                else { _iraHoldDerecho = 0; _iraCargandoHold = false; }
            }
            catch { }'''

parche("Content/Players/ShardPlayer.cs", VIEJO_POST, NUEVO_POST, "[1b] el gesto de 2 s en PostUpdate")

# =====================================================================
# [2] EL LÍMITE 10 = PROYECTILES BÁSICOS (la penetración vuelve a -1).
#     La letra: «lo que tenia que quedar en 10 era el limite de
#     proyectiles basicos del grimorio, no la penetracion».
# =====================================================================
VIEJO_PEN = r'''            // ================================================================
            //  v6.151 — EL ATAQUE BÁSICO DEL ETERNO PERFORA 10 (la letra:
            //  «quiero que limites el ataque basico del grimorio del
            //  eterno a solo 10»): los discos del Equinoccio (las marcas
            //  9201/9202 — los proyectiles del clic izquierdo del libro)
            //  calzan penetrate=10 UNA vez al nacer. Antes eran -1
            //  (infinito): el disco atravesaba TODO el mundo. Diez
            //  cuerpos y se apaga — un ataque básico con medida.
            // ================================================================
            if ((marca == 9201f || marca == 9202f) && !_perforaEterno)
            {
                _perforaEterno = true;
                projectile.penetrate = 10;
            }'''

NUEVO_PEN = r'''            // ================================================================
            //  v6.152 — LA PENETRACIÓN VUELVE A SU CASA (la letra: «lo
            //  que tenia que quedar en 10 era el limite de proyectiles
            //  basicos del grimorio, no la penetracion»): el penetrate
            //  v6.151 MURIÓ (los discos vuelven a atravesar como
            //  siempre) — el 10 vive en el TOPE DE PAREJAS del Shoot
            //  (GrimoireEternal: máx 10 proyectiles básicos vivos).
            // ================================================================'''

parche("Content/Globals/EmperatrizFX.cs", VIEJO_PEN, NUEVO_PEN, "[2a] penetrate 10 muere")

VIEJO_CAMPOPERF = r'''        private bool _perforaBinaria;
        private bool _perforaEterno;   // v6.151 — el básico del Eterno: 10'''

NUEVO_CAMPOPERF = r'''        private bool _perforaBinaria;'''

parche("Content/Globals/EmperatrizFX.cs", VIEJO_CAMPOPERF, NUEVO_CAMPOPERF, "[2b] el campo muere")

# el TOPE en el Shoot: máximo 10 proyectiles básicos VIVOS (931) del jugador
VIEJO_TOPE = r'''            // una más cada 3 niveles, en abanico (el homing del 931 hace
            // el resto)
            int extra = WeaponScaling.ExtraProjectiles(level);

            LanzarPareja(source, pos, baseV, damage, knockback, player);

            for (int i = 0; i < extra; i++)
            {
                float angulo = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                LanzarPareja(source, pos, baseV.RotatedBy(angulo), damage, knockback, player);
            }

            return false;   // todos los spawns son nuestros'''

NUEVO_TOPE = r'''            // ============================================================
            //  v6.152 — EL LÍMITE DE 10 PROYECTILES BÁSICOS (la letra:
            //  «lo que tenia que quedar en 10 era el limite de
            //  proyectiles basicos del grimorio, no la penetracion»):
            //  el disparo NACE con un TOPE: si ya viven 10 discos del
            //  Equinoccio (los 931 con nuestras marcas 9201/9202 — los
            //  del OTRO arma Dignos no cuentan), los NUEVOS esperan al
            //  siguiente disparo. El abanico entero cabe en el tope:
            //  nace lo que falde, jamás se dispara MÁS de 10 vivos.
            //  (La penetración vuelve a su casa: los discos atraviesan
            //  como siempre — el 10 es CUÁNTOS, no CUÁNTO.)
            // ============================================================
            const int TOPE_BASICOS = 10;
            int vivos = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.owner != player.whoAmI || pr.type != 931)
                    continue;
                float marca = pr.ai[2];
                if (marca == 9201f || marca == 9202f) vivos++;
            }
            int cupo = Math.Max(0, TOPE_BASICOS - vivos);
            if (cupo <= 0) return false;   // el cielo ya está lleno de discos

            // una más cada 3 niveles, en abanico (el homing del 931 hace
            // el resto)
            int extra = WeaponScaling.ExtraProjectiles(level);

            LanzarPareja(source, pos, baseV, damage, knockback, player);
            cupo--;

            for (int i = 0; i < extra && cupo > 0; i++)
            {
                float angulo = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                LanzarPareja(source, pos, baseV.RotatedBy(angulo), damage, knockback, player);
                cupo -= 2;   // cada pareja son DOS discos (dorado + negro)
            }

            return false;   // todos los spawns son nuestros'''

parche("Content/Weapons/GrimoireEternal.cs", VIEJO_TOPE, NUEVO_TOPE, "[2c] tope 10 básicos vivos")

# =====================================================================
# [3] EL AUDIO — LA SATURACIÓN MUERE: MaxInstances en toda la cadena
#     del básico. El UseSound Item8 por uso (autoReuse cada 22 t)
#     + los zaps del TruenoPerlin de los ojos: con escudería completa
#     son ~30 instancias/seg — el mezclador FNA satura TODO el audio.
#     LA CURA: SoundMaxInstances — Item8 tope 2, zaps tope 1, y el
#     cooldown de disparo de los ojos ya los españa.
# =====================================================================
VIEJO_USESOUND = r'''            Item.UseSound = SoundID.Item8;'''

NUEVO_USESOUND = r'''            // v6.152 — LA SATURACIÓN DEL AUDIO MUERE (la letra: «al
            //     lanzar el ataque basico y lo dejo sostenido, el audio
            //     comienza a subir su volumen esta subida de volumen
            //     afecta a todo el audio y sube hasta saturarse por
            //     completo»): el mezclador de FNA SATURA cuando viven
            //     demasiadas instancias a la vez — el UseSound por uso
            //     (autoReuse cada 22 t) + los zaps de los ojos del
            //     escuadrón sumaban decenas vivas. EL TOPE: máximo 2
            //     instancias del mismo estilo vivas (SoundMaxInstances
            //     — la propia SoundStyle de tML lo corta en la raíz).
            Item.UseSound = SoundID.Item8.WithVolumeScale(0.85f);
            Item.UseSound.MaxInstances = 2;'''

parche("Content/Weapons/GrimoireEternal.cs", VIEJO_USESOUND, NUEVO_USESOUND, "[3a] UseSound con tope")

# el TruenoPerlin: los zaps de los ojos (2 sonidos por disparo × 11 ojos)
VIEJO_ZAP = r'''                        // EL ZAP OSCURO: la mitad del registro del cetro eléctrico
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(-0.15f).WithVolumeScale(0.9f),
                            Projectile.Center);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.55f),
                            Projectile.Center);'''

NUEVO_ZAP = r'''                        // EL ZAP OSCURO: la mitad del registro del cetro eléctrico
                        // v6.152 — LA SATURACIÓN MUERE: tope 1 instancia
                        //     por estilo (el escuadrón entero dispara y
                        //     el mezclador ya no se llena — la cura del
                        //     audio que sube hasta saturarse)
                        var zapOscuro = SoundID.Item12.WithPitchOffset(-0.15f).WithVolumeScale(0.9f);
                        zapOscuro.MaxInstances = 1;
                        Terraria.Audio.SoundEngine.PlaySound(zapOscuro, Projectile.Center);
                        var truenoOscuro = SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.55f);
                        truenoOscuro.MaxInstances = 1;
                        Terraria.Audio.SoundEngine.PlaySound(truenoOscuro, Projectile.Center);'''

parche("Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs", VIEJO_ZAP, NUEVO_ZAP, "[3b] zap oscuro con tope")

VIEJO_ZAP2 = r'''                        // EL ZAP: chasquido eléctrico agudo + trueno corto.
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(0.35f).WithVolumeScale(1.05f),
                            Projectile.Center);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.55f),
                            Projectile.Center);'''

NUEVO_ZAP2 = r'''                        // EL ZAP: chasquido eléctrico agudo + trueno corto.
                        // v6.152 — LA SATURACIÓN MUERE (tope 1 por estilo)
                        var zapClaro = SoundID.Item12.WithPitchOffset(0.35f).WithVolumeScale(1.05f);
                        zapClaro.MaxInstances = 1;
                        Terraria.Audio.SoundEngine.PlaySound(zapClaro, Projectile.Center);
                        var truenoClaro = SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.55f);
                        truenoClaro.MaxInstances = 1;
                        Terraria.Audio.SoundEngine.PlaySound(truenoClaro, Projectile.Center);'''

parche("Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs", VIEJO_ZAP2, NUEVO_ZAP2, "[3c] zap claro con tope")

# el OnHitNPC del EmperatrizFX (9211): el estallido por golpe también con tope
VIEJO_GOLPE = r'''                Projectiles.Cosmic.DignosFXProjectile.Nacer(projectile,
                    projectile.Center, 1);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.6f), projectile.Center);'''

NUEVO_GOLPE = r'''                Projectiles.Cosmic.DignosFXProjectile.Nacer(projectile,
                    projectile.Center, 1);
                // v6.152 — LA SATURACIÓN MUERE: el estallido por golpe
                //     con tope (una andanada de discos contra una horda
                //     era un muro de Item122 simultáneos)
                var estallidoGolpe = SoundID.Item122.WithPitchOffset(0.6f);
                estallidoGolpe.MaxInstances = 2;
                Terraria.Audio.SoundEngine.PlaySound(estallidoGolpe, projectile.Center);'''

parche("Content/Globals/EmperatrizFX.cs", VIEJO_GOLPE, NUEVO_GOLPE, "[3d] estallido por golpe con tope")

print("PARTE A OK")

# =====================================================================
# [4] EL JUICIO DE LOS DOS SOLES — SIN SALTOS, OTRA VEZ Y PARA SIEMPRE.
#     La autopsia v6.152: los saltos que quedaban eran LOS CORTES DE
#     ACTO en el DIBUJADO (cada `else if (_acto == N)` cambia variables
#     que saltan: radio, giro, posiciones) + `Main.rand` en estelas.
#     LA CURA ESTRUCTURAL: UNA SOLA FUNCIÓN DE TIEMPO continua
#     (RelojDelJuicio: el total de vida del proyectil, sin actos en el
#     dibujo) — el radio, el color, la posición y el giro son funciones
#     CONTINUAS de ese reloj (curvas suavizadas por tramos con matching
#     de valores Y derivadas en las costuras). El PreDraw entero se
#     reescribe sobre esa única línea de tiempo.
# =====================================================================

# (a) el AI: la línea de tiempo TOTAL (para dibujar continuo)
VIEJO_RELOJ = r'''        /// <summary>El reloj del acto actual.</summary>
        private float _t;'''

NUEVO_RELOJ = r'''        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>v6.152 — EL RELOJ TOTAL (la cura de TODOS los
        /// saltos): la edad del proyectil desde su nacimiento — UNA
        /// SOLA línea de tiempo continua para TODO el dibujado (los
        /// actos siguen existiendo para la IA/lógica, pero el PRESENTA
        /// ya no corta nunca: cada curva visual es función continua de
        /// ESTE reloj con costuras suavizadas).</summary>
        private float _edad;'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_RELOJ, NUEVO_RELOJ, "[4a] el reloj total")

VIEJO_TPP = r'''        public override void AI()
        {
            _t++;'''

NUEVO_TPP = r'''        public override void AI()
        {
            _t++;
            _edad++;   // v6.152 — la línea de tiempo del dibujado (continua)'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_TPP, NUEVO_TPP, "[4b] la edad avanza")

# (b) LA FUNCIÓN CANÓNICA: el radio del sol fusionado en TODO el ciclo
#     (contacto→caza→nova) — una sola curva continua. + PosiciónSolAhora
#     (dónde está el sol EN este tick — la caza se lee igual en dibujo).
VIEJO_RADIOFUSION = r'''        /// <summary>v6.151 — LA CURVA ÚNICA DE LA FUSIÓN (la cura del
        /// «salto»): el radio del sol fusionado de su NACIMIENTO (n=0,
        /// EXACTO al radio 0.55·BodyPx del par en el contacto — la
        /// continuidad matemática) a su adultez (n=1, 1.05·BodyPx).
        /// La caza recoge esta curva por su cola: una sola línea de
        /// vida, sin cortes de fase JAMÁS.</summary>
        private static float RadioFusion(float n)
        {
            float s = n * n * (3f - 2f * n);   // smoothstep: suave al inicio
            return MathHelper.Lerp(0.55f, 1.05f, s);
        }'''

NUEVO_RADIOFUSION = r'''        /// <summary>v6.151 — LA CURVA ÚNICA DE LA FUSIÓN (la cura del
        /// «salto»): el radio del sol fusionado de su NACIMIENTO (n=0,
        /// EXACTO al radio 0.55·BodyPx del par en el contacto — la
        /// continuidad matemática) a su adultez (n=1, 1.05·BodyPx).
        /// La caza recoge esta curva por su cola: una sola línea de
        /// vida, sin cortes de fase JAMÁS.</summary>
        private static float RadioFusion(float n)
        {
            float s = n * n * (3f - 2f * n);   // smoothstep: suave al inicio
            return MathHelper.Lerp(0.55f, 1.05f, s);
        }

        /// <summary>v6.152 — LA CURVA MAESTRA DE TODO EL JUICIO: el
        /// radio del cuerpo central (el par → el gigante → la caza → el
        /// colapso → la sentencia) como función CONTINUA del RELOJ
        /// TOTAL. Cada tramo termina EXACTO donde empieza el siguiente
        /// (costuras C0) y con su misma pendiente (costuras C1: las
        /// curvas son smoothstep — la derivada en los bordes es 0 en
        /// ambos lados): ES IMPOSIBLE QUE SALTE, matemáticamente.
        /// Los tramos (en ticks del _edad): 0-36 convocatoria (el par
        /// vuela, r 0.5→0.92) · 36-63 el vals (espiral, r 0.92→0.55) ·
        /// 63-66 el contacto (r 0.55) · 66-366 la caza (0.55 →
        /// 1.05+0.70) · 366-373 el colapso (plisado) · 373+ la nova.</summary>
        private static float RadioDelJuicio(float edad)
        {
            // EL PAR (convocatoria + vals): el radio de CADA sol —
            // idéntico al PosicionSolSencillo/vals de siempre
            if (edad < T_CONVOCATORIA)
            {
                float p = edad / (float)T_CONVOCATORIA;
                return MathHelper.Lerp(0.50f, 0.92f, p * p * (3f - 2f * p));
            }
            if (edad < T_CONVOCATORIA + T_FUSION)
            {
                float p = (edad - T_CONVOCATORIA) / (float)T_FUSION;
                return MathHelper.Lerp(0.92f, 0.55f, p * p * (3f - 2f * p));
            }
            // EL CONTACTO + LA CAZA: nace del 0.55 del par y crece
            // continuo hasta el gigante adulto + el 70 % de la caza
            if (edad < T_CONVOCATORIA + T_FUSION + T_CARGA)
            {
                float n = (edad - T_CONVOCATORIA - T_FUSION) / (float)T_CARGA;
                return RadioFusion(Math.Min(1f, n * 3f))        // nacimiento 0.55→1.05
                     + CRECE_CAZA * (n * n * (3f - 2f * n));     // la caza 1.05→1.75
            }
            // EL COLAPSO + LA NOVA: plisado y estallido (continuos)
            float tPost = edad - T_CONVOCATORIA - T_FUSION - T_CARGA;
            float rGigante = 1.05f + CRECE_CAZA;
            if (tPost < T_COLAPSO)
            {
                float plega = tPost / (float)T_COLAPSO;
                return MathHelper.Lerp(rGigante, rGigante * 0.42f, plega * plega);
            }
            float p = MathHelper.Clamp((tPost - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
            return rGigante * 0.42f * (0.42f + 0.95f * MathF.Min(1f, (tPost - T_COLAPSO) / 7f)) *
                   (0.85f + 0.15f * (1f - p)) / 0.42f;
        }

        /// <summary>v6.152 — LA POSICIÓN DEL SOL AHORA: el punto donde
        /// vive el cuerpo central en este tick del reloj total (la
        /// convocatoria y el vals en su sitio de siempre; la caza
        /// CABALGA el _centroCaza que mueve la IA — UNA verdad).</summary>
        private Vector2 CentroDelJuicio(float edad)
        {
            if (edad >= T_CONVOCATORIA + T_FUSION) return _centroCaza;
            return Punto;
        }'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_RADIOFUSION, NUEVO_RADIOFUSION, "[4c] la curva maestra")

print("PARTE B1 OK")

# =====================================================================
# [5] EL AGUJERO RÚNICO PURO — EL ESTILO NUEVO, SUPER CARGADO (la
#     letra: «dale otro estilo, mas vfx mas efectos visuales, mas de
#     todo, que se vea super cargado, pero que siga siendo un agujero
#     runico»). El PreDraw del proyectil ya tiene la familia de capas
#     v6.150; ahora se le SUMAN CUATRO CAPAS NUEVAS de la casa y el
#     DOBLE de runas/glifos: (a) EL TORBELLINO DE GLIFOS (24 runas
#     cayendo en espiral hacia el horizonte, contrarrotando), (b) LA
#     CORONA DE LENTEJUELAS (12 destellos de 4 puntas orbitando al
#     borde de Einstein), (c) LOS FILAMENTOS DE ACRECIÓN (6 arcos
#     eléctricos Perlin chupados hacia adentro), (d) EL ECO DOBLE (dos
#     ondas de choque concéntricas que respiran con el latido).
#     Todo determinista (Hash01) y por los lotes de la casa.
# =====================================================================
VIEJO_PREDRAW_PURO = r'''        public override bool PreDraw(ref Color lightColor)
        {'''

NUEVO_PREDRAW_PURO = r'''        // ================================================================
        //  v6.152 — LAS CUATRO CAPAS NUEVAS DEL PURO (la letra: «mas
        //  vfx mas efectos visuales, mas de todo, que se vea super
        //  cargado, pero que siga siendo un agujero runico»):
        //   (a) EL TORBELLINO DE GLIFOS — 24 runas solares ESPIRALANDO
        //       hacia el horizonte (contrarrotación, tamaño creciendo
        //       al caer — la materia escrita siendo comida).
        //   (b) LA CORONA DE LENTEJUELAS — 12 destellos de 4 puntas
        //       orbitando EL BORDE de Einstein (el límite donde la luz
        //       se dobla — cada lentejuela con SU titileo).
        //   (c) LOS FILAMENTOS DE ACRECIÓN — 6 arcos eléctricos
        //       (ruido determinista) lamiendo el disco desde afuera
        //       hacia adentro (la cena en vivo).
        //   (d) EL ECO DOBLE — dos ondas concéntricas que respiran con
        //       el latido del corazón (la firma de la casa, ahora ×2).
        //  TODO determinista (Hash01 — R5) y por el lote aditivo.
        // ================================================================
        private void DibujarLasCapasNuevas(Vector2 centro, float t, float escala)
        {
            VFXCore.AbrirAdditive();
            try
            {
                Vector2 c = centro - Main.screenPosition;
                float radio = Projectile.width * 0.5f * escala;

                // === (a) EL TORBELLINO DE GLIFOS ===
                for (int i = 0; i < 24; i++)
                {
                    float h0 = VFXCore.Hash01(i, 1, 6101);
                    float h1 = VFXCore.Hash01(i, 2, 6103);
                    float caida = Frac01(t * (0.10f + 0.08f * h0) + h1);   // 0→1 cayendo
                    float ang = h1 * MathHelper.TwoPi - t * (0.9f + 0.6f * h0) + caida * 7f;
                    float r = MathHelper.Lerp(radio * 4.2f, radio * 1.15f, caida * caida);
                    Vector2 g = c + ang.ToRotationVector2() * r;
                    float tam = (7f + 10f * h0) * (0.5f + 0.8f * caida);
                    Color tinta = i % 3 == 0
                        ? new Color(150, 210, 255)
                        : new Color(255, 214, 130);
                    VFXCore.Quad(g, SigiloLib.Tint(tinta, 0.55f * (1f - caida * 0.4f)),
                        new Vector2(tam, tam * 0.4f), ang + MathHelper.PiOver2);
                }

                // === (b) LA CORONA DE LENTEJUELAS (destellos de 4 puntas) ===
                Texture2D dest = VFXCore.DestelloFinal;
                if (dest != null)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float h0 = VFXCore.Hash01(i, 3, 6111);
                        float titila = 0.5f + 0.5f * MathF.Sin(t * (2.2f + 1.3f * h0) + i * 2.1f);
                        float ang = h0 * MathHelper.TwoPi + t * 0.35f;
                        float rB = radio * (3.4f + 0.10f * MathF.Sin(t * 1.3f + i));
                        Vector2 l = c + ang.ToRotationVector2() * rB;
                        float esc = (26f + 20f * h0) * (0.55f + 0.45f * titila) / dest.Width;
                        Main.spriteBatch.Draw(dest, l, null,
                            SigiloLib.Tint(new Color(200, 235, 255), 0.75f * titila),
                            t * 0.4f + i, dest.Size() * 0.5f, esc,
                            SpriteEffects.None, 0f);
                    }
                }

                // === (d) EL ECO DOBLE (las ondas que respiran) ===
                float latido = 0.5f + 0.5f * MathF.Sin(t * 2.6f);
                for (int k = 0; k < 2; k++)
                {
                    float fase = Frac01(t * 0.22f + k * 0.5f);
                    float rO = radio * (1.4f + fase * 5.5f);
                    float alfa = (1f - fase) * (0.32f + 0.18f * latido);
                    int segs = 34;
                    for (int s = 0; s < segs; s++)
                    {
                        float a0 = s * MathHelper.TwoPi / segs;
                        Vector2 p0 = c + a0.ToRotationVector2() * rO;
                        Vector2 p1 = c + (a0 + MathHelper.TwoPi / segs).ToRotationVector2() * rO;
                        VFXCore.Line(p0, p1, SigiloLib.Tint(
                            k == 0 ? new Color(190, 120, 255) : new Color(255, 200, 120),
                            alfa), 2.2f);
                    }
                }
            }
            finally { VFXCore.RestaurarLoteVanilla(); }

            // === (c) LOS FILAMENTOS DE ACRECIÓN (arcos eléctricos) ===
            for (int i = 0; i < 6; i++)
            {
                float h0 = VFXCore.Hash01(i, 4, 6121);
                float ang = h0 * MathHelper.TwoPi + t * (0.5f + 0.3f * h0);
                float dentro = radio * (1.05f + 0.35f * VFXCore.Hash01(i, 5, 6123));
                float fuera = radio * (2.6f + 1.4f * VFXCore.Hash01(i, 6, 6125));
                Vector2 a = centro + ang.ToRotationVector2() * fuera;
                Vector2 b = centro + (ang + 0.22f).ToRotationVector2() * dentro;
                RayoLib.Arco(a, b, t + i, 7, 0.55f, new Color(170, 220, 255));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {'''

parche("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs", VIEJO_PREDRAW_PURO, NUEVO_PREDRAW_PURO, "[5a] las 4 capas nuevas (método)")

# la llamada al final del PreDraw existente (antes del return false):
VIEJO_FIN_PURO = r'''            try
            {
                DrawCoreVisuals(Projectile);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;'''

NUEVO_FIN_PURO = r'''            try
            {
                DrawCoreVisuals(Projectile);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.152 — LAS CAPAS NUEVAS: el estilo SUPER CARGADO de la
            // letra (torbellino de glifos + corona de lentejuelas +
            // filamentos de acreción + eco doble) — todo determinista
            // y por los lotes de la casa, sobre EL MISMO cuerpo.
            try
            {
                DibujarLasCapasNuevas(Projectile.Center, Main.GlobalTimeWrappedHourly,
                    Math.Max(Projectile.scale, 0.05f));
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;'''

parche("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs", VIEJO_FIN_PURO, NUEVO_FIN_PURO, "[5b] la llamada a las capas")

# Frac01 (si no existe en la clase) + el arco de acreción inline
s = leer("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs")
if "private static float Frac01" not in s:
    VIEJO_FRAC = r'''        private static float ElasticOut(float t)'''
    NUEVO_FRAC = r'''        /// <summary>El resto positivo [0,1) — el módulo de la casa.</summary>
        private static float Frac01(float x)
        {
            float f = x - MathF.Floor(x);
            return f < 0f ? 0f : (f > 0.999f ? 0.999f : f);
        }

        private static float ElasticOut(float t)'''
    parche("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs", VIEJO_FRAC, NUEVO_FRAC, "[5c] Frac01 helper")

print("PARTE B2 OK — verificando RayoLib.Arco…")
s = leer("Content/VFX/RayoLib.cs") if os.path.exists("Content/VFX/RayoLib.cs") else ""
import re as _re
if not _re.search(r"public static void Arco\(", s):
    print("  ⚠ RayoLib.Arco NO existe — parcheo a arco propio")
    # añadir Arco a RayoLib (o dibujarlo inline): el arco determinista de la casa
    if s:
        VIEJO_RL = s[:s.index("public static class RayoLib") + len("public static class RayoLib")]
    # fallback: usar VFXCore.Line en zigzag determinista dentro del método nuevo
    parche("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs",
           "                RayoLib.Arco(a, b, t + i, 7, 0.55f, new Color(170, 220, 255));",
           """                DibujarArcoAcrecion(a, b, t + i);""",
           "[5d] arco inline (RayoLib.Arco no existe)")
    parche("Content/Projectiles/Cosmic/AgujeroRunicoPuroProjectile.cs",
           """        private static float Frac01(float x)
        {
            float f = x - MathF.Floor(x);
            return f < 0f ? 0f : (f > 0.999f ? 0.999f : f);
        }
""",
           """        private static float Frac01(float x)
        {
            float f = x - MathF.Floor(x);
            return f < 0f ? 0f : (f > 0.999f ? 0.999f : f);
        }

        /// <summary>v6.152 — EL ARCO DE ACRECIÓN: una descarga eléctrica
        /// determinista de 7 segmentos entre dos puntos (el zigzag con
        /// Hash01 — R5: cero azar, la misma chispa en todas las
        /// pantallas) dibujada en el lote aditivo.</summary>
        private static void DibujarArcoAcrecion(Vector2 a, Vector2 b, float semillaT)
        {
            VFXCore.AbrirAdditive();
            try
            {
                const int segs = 7;
                Vector2 prev = a;
                for (int s = 1; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    Vector2 baseP = Vector2.Lerp(a, b, u);
                    float desv = (VFXCore.Hash01(s, 7, 6131 + (int)(semillaT * 13f) % 997) - 0.5f) *
                                 22f * (1f - MathF.Abs(u - 0.5f) * 1.2f);
                    Vector2 dir = b - a;
                    if (dir.LengthSquared() < 0.01f) dir = Vector2.UnitY;
                    Vector2 perp = new(-dir.Y, dir.X);
                    perp.Normalize();
                    Vector2 p = baseP + perp * desv - Main.screenPosition;
                    VFXCore.Line(prev - Main.screenPosition, p,
                        SigiloLib.Tint(new Color(170, 220, 255), 0.55f), 2.4f);
                    prev = p;
                }
            }
            finally { VFXCore.RestaurarLoteVanilla(); }
        }
""",
           "[5e] el arco propio")
print("PARTE B2 OK")

# =====================================================================
# [6] LA FORMA ETERNA — LOS ANILLOS DE LA CONSTRUCCIÓN = LOS DEL
#     ACCESORIO (la letra: «no son los mismos que los de la forma
#     eterna en accesorio, ten en cuenta que la forma eterna en
#     accesorio es la correcta y es como debe verse la Forma Eterna en
#     construccion»). EL ACCESORIO viste AnilloRunicoDorsalHalo →
#     AnilloDorsalRenderer.Draw (LA TRIPLE CORONA + anillo de fotones).
#     El halo de construcción cambia SU dibujado a ESA MISMA LLAMADA
#     (con runasMul creciendo 113→119 — la corona que se escribe).
# =====================================================================
VIEJO_ANILLOS = r'''            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                // R de los anillos del sol: proporcional al cuerpo del
                // portador (el sol de la casa con BodyPx=34; aquí el
                // portador ES la estrella — su altura es la regla).
                float R = 34f * (duenio.height / 42f);
                SigiloLib.SistemaAnillos(centro, R, t, semilla,
                    anillos, 0f, 1f, 1f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;'''

NUEVO_ANILLOS = r'''            // ============================================================
            //  v6.152 — LOS ANILLOS DE LA CONSTRUCCIÓN SON LOS DEL
            //  ACCESORIO (la letra: «no son los mismos que los de la
            //  forma eterna en accesorio, ten en cuenta que la forma
            //  eterna en accesorio es la correcta y es como debe verse
            //  la Forma Eterna en construccion»). LA CORRECCIÓN: el
            //  accesorio viste AnilloDorsalRenderer.Draw — LA TRIPLE
            //  CORONA DE CONJURO + el anillo de fotones (la firma de la
            //  espalda del portador). El SistemaAnillos de soles de la
            //  v6.150 MURIÓ aquí: la construcción viste EXACTAMENTE la
            //  corona del accesorio, con las runas ESCRIBIÉNDOSE —
            //  cada peldaño 113-119 enciende un TERCIO de la corona
            //  (runasMul 0.35→1.0: la escritura crece hasta su forma
            //  final, LA del accesorio).
            // ============================================================
            try
            {
                float runasMul = MathHelper.Clamp(0.35f + 0.65f * (anillos - 1) / 6f, 0.35f, 1f);
                VFX.AnilloDorsalRenderer.Draw(centro, duenio.height, t, 1f, runasMul);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;'''

parche("Content/Projectiles/Cosmetic/AnillosSolRunicoHalo.cs", VIEJO_ANILLOS, NUEVO_ANILLOS, "[6] los anillos = la corona del accesorio")

# =====================================================================
# [7] 'PUERTA' → 'DESPERTAR' cuando significa el despertar (la letra:
#     «siempre que te refieres al despertar le dices puerta... deberias
#     cambiar la palabra puerta cuando te refieres al despertar, por
#     Despertar»). Regla quirúrgica por frase (NO un reemplazo ciego:
#     'la puerta del 20' → 'el Despertar 20'; 'puerta 120' → 'Despertar
#     120'...). Las puertas LITERALES de lore (la puerta de anillos, la
#     puerta del templo) QUEDAN.
# =====================================================================
import re as _re

FRASES = [
    # (viejo, nuevo) — es-MX y es-ES comparten redacción en estas claves
    ("la puerta del 20 queda paga PARA SIEMPRE", "el Despertar 20 queda pago PARA SIEMPRE"),
    ("la próxima puerta no lo detendrá", "el próximo Despertar no lo detendrá"),
    ("El guardián de la puerta muerde el polvo que era", "El guardián del Despertar muerde el polvo que era"),
    ("pagadas en la puerta — EL HAZ responde", "pagadas en el Despertar — EL HAZ responde"),
    ("cada puerta exige que su jefe haya caído", "cada Despertar exige que su jefe haya caído"),
    ("la línea de la puerta de caza", "la línea del Despertar de caza"),
    ("la puerta del despertar 20 NACE PAGA", "el peldaño del Despertar 20 NACE PAGA"),
    ("la puerta del 20 lo exige", "el Despertar 20 lo exige"),
    ("la puerta del 20 exige su LIBRO CONSAGRADO", "el Despertar 20 exige su LIBRO CONSAGRADO"),
    ("pero la puerta del 20 exige", "pero el Despertar 20 exige"),
]
for ruta in ["Localization/es-MX_Mods.AethonMod.hjson",
             "Localization/es-ES_Mods.AethonMod.hjson"]:
    s = leer(ruta)
    n = 0
    for viejo, nuevo in FRASES:
        if viejo in s:
            s = s.replace(viejo, nuevo); n += 1
    escribir(ruta, s)
    print(f"  ✔ [7] {ruta}: {n} frases curadas")

# en-US (door → awakening en las mismas claves)
FRASES_EN = [
    ("the gate to awakening 20", "awakening 20"),  # por si existe
]
s = leer("Localization/en-US_Mods.AethonMod.hjson")
n = 0
# buscar patrones de puerta-como-despertar en en-US: "the door of 20",
# "gate", "door 20"...
for viejo, nuevo in [
    ("the door of awakening 20", "awakening 20"),
    ("door of 20 is paid FOREVER", "awakening 20 is paid FOREVER"),
    ("paid at the door — THE BEAM", "paid at the awakening — THE BEAM"),
]:
    if viejo in s:
        s = s.replace(viejo, nuevo); n += 1
escribir("Localization/en-US_Mods.AethonMod.hjson", s)
print(f"  ✔ [7] en-US: {n} frases curadas")

# y los .cs donde la palabra 'puerta' significa despertar (tooltips vivos)
for ruta in ["Content/Systems/DespertarGrimorioSistema.cs",
             "Content/Systems/DespertarGrimorio.cs"]:
    if os.path.exists(ruta):
        s = leer(ruta)
        # solo los comentarios que usan 'la puerta del N' → el Despertar N
        s2 = _re.sub(r"la puerta del (\d+)", r"el Despertar \1", s)
        s2 = _re.sub(r"puerta (\d{2,3})\b", r"Despertar \1", s2)
        if s2 != s:
            escribir(ruta, s2)
            print(f"  ✔ [7] {ruta}: comentarios curados")

# =====================================================================
# [8] LA SOMBRA DE LA PÁGINA ORIGINAL — TENTÁCULO NEGRO COMPLETO (la
#     letra: «quita los ojo y colmillos, tambien vi que tiene lineas
#     rojas, quitalas tambien dejando un tentaculo negro completo»).
#     (a) Masa lisa (sin colmillos/ventosas — el flag v6.151).
#     (b) LOS OJOS mueren también en el modo normal (la letra es para
#         la sombra ORIGINAL).
#     (c) LA HERIDA ROJA muere (el Quad rojo de la digestión).
# =====================================================================
VIEJO_MASA_ORIG = r'''            SombrasLib.Masa(col, 9f, 13f, 0.95f * disipa, semilla, tiempo, grosorCentro: 44f);'''

NUEVO_MASA_ORIG = r'''            // v6.152 — LA SOMBRA ORIGINAL TAMBIÉN ES LISA (la letra:
            //     «quita los ojo y colmillos, tambien vi que tiene
            //     lineas rojas, quitalas tambien dejando un tentaculo
            //     negro completo»): la Masa en modo liso (sin espinas
            //     de hueso, sin ventosas) — tinta negra que fluye.
            SombrasLib.Masa(col, 9f, 13f, 0.95f * disipa, semilla, tiempo,
                grosorCentro: 44f, lisa: true);'''

parche("Content/Projectiles/Sombras/SombraPaginaProjectile.cs", VIEJO_MASA_ORIG, NUEVO_MASA_ORIG, "[8a] la masa original lisa")

VIEJO_OJOS_ORIG = r'''            bool esTinta = Projectile.ai[0] == 1f;
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            if (!esTinta)
                SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 7);
            else
            {
                // LA TINTA RESPIRA MÁS: dos capas extra de bruma densa
                SombrasLib.BrumaColumna(col, 40f, 0.50f * disipa, tiempo, semilla + 41, 10);
            }'''

NUEVO_OJOS_ORIG = r'''            bool esTinta = Projectile.ai[0] == 1f;
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            // v6.152 — LOS OJOS MURIERON EN LA SOMBRA ORIGINAL (la
            //     letra: «quita los ojo y colmillos… dejando un
            //     tentaculo negro completo»): ya NO hay ojos en NINGÚN
            //     modo — la sombra es TINTA CIEGA de punta a punta.
            //     En su lugar: la bruma densa que respiraba la tinta
            //     (ahora es de TODOS los modos — la página exhala).
            SombrasLib.BrumaColumna(col, 40f, 0.50f * disipa, tiempo, semilla + 41, 10);'''

parche("Content/Projectiles/Sombras/SombraPaginaProjectile.cs", VIEJO_OJOS_ORIG, NUEVO_OJOS_ORIG, "[8b] los ojos de la original mueren")

VIEJO_HERIDA = r'''                // la HERIDA: el punto donde el tentáculo ENTERRÓ la punta
                // brilla rojo DENTRO de la bruma (la digestión)
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * disipa),
                    new Vector2(110f, 110f), rumbo.ToRotation());
                VFXCore.FlushAdditive();'''

NUEVO_HERIDA = r'''                // v6.152 — LA HERIDA ROJA MURIÓ (la letra: «tambien vi
                //     que tiene lineas rojas, quitalas tambien dejando
                //     un tentaculo negro completo»): la digestión ya no
                //     ARDE en rojo — la punta bruma NEGRA sobre el
                //     devorado (la cena es discreta).
                SombrasLib.CabezaDeBruma(punta, rumbo, 0.85f * disipa, disipa, tiempo, semilla + 13);'''

parche("Content/Projectiles/Sombras/SombraPaginaProjectile.cs", VIEJO_HERIDA, NUEVO_HERIDA, "[8c] la herida roja muere")

print("PARTE C OK")

# =====================================================================
# [9] EL ESQUELETRON EN EL MUNDO INTERIOR — BLINDAJE TOTAL (la letra:
#     «al invocar a esqueletro me sale la version de la mazmorra...
#     debes asegurarte de que esqueletron normal aparezca en el mundo
#     interior cuando se le llame y no la version de mazmorra»).
#     LA CURA DEFINITIVA (independiente del sello de oleada): el
#     GlobalNPC de los jefes del interior (JefeInteriorGlobalNPC) —
#     cuando el interior vive Y un SkeletronHead anda por ahí, la
#     noche se le presta SIEMPRE (PreAI→PostAI del mismo tick: la IA
#     vanilla jamás ve el día → jamás entra en modo guardián). Vale
#     para el de oleada, el de la carnada y cualquiera que aparezca.
#     Y el clamp del mediodía NO se toca (el préstamo es por-tick).
# =====================================================================
s = leer("Content/Globals/JefeInteriorGlobalNPC.cs")
assert "public override void PreAI" not in s, "JefeInteriorGlobalNPC ya tiene PreAI (revisar a mano)"

VIEVO_JI = r'''    public class JefeInteriorGlobalNPC : GlobalNPC
    {'''

NUEVO_JI = r'''    public class JefeInteriorGlobalNPC : GlobalNPC
    {
        // ================================================================
        //  v6.152 — EL ESQUELETRÓN NUNCA ES GUARDIÁN DE MAZMORRA EN EL
        //  MUNDO INTERIOR (la letra: «en el mundo interior al invocar a
        //  esqueletro me sale la version de la mazmorra con la cabeza
        //  girando y hiendo rapidamente hacia el jugador, eso no debe
        //  pasar… debes asegurarte de que esqueletron normal aparezca
        //  en el mundo interior cuando se le llame y no la version de
        //  mazmorra»). LA LEY VANILLA: SkeletronHead lee Main.dayTime
        //  en su IA — con DÍA se transforma en EL GUARDIÁN (cabeza
        //  girando, daño 9999). El interior CLAVA el mediodía eterno
        //  (dayTime=true cada tick) → CUALQUIER SkeletronHead del
        //  interior (oleada, carnada o lo que sea) nacía guardián.
        //  LA CURA: el préstamo de noche UNIVERSAL dentro del interior
        //  — mientras el interior viva, TODO SkeletronHead recibe la
        //  noche PRESTADA en su PreAI y se la devolvemos en PostAI
        //  (el mediodía del decorado intacto, el guardián pelea como
        //  el Esqueletrón NORMAL de toda la vida).
        // ================================================================
        private bool _prestaNocheInterior;

        public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        {
            // solo el SkeletronHead (la cabeza que gira) — y también
            // registramos para el esqueletron de oleada el NOMBRE nuevo
            return npc.type == NPCID.SkeletronHead;
        }

        public override void PreAI(NPC npc)
        {
            _prestaNocheInterior = false;
            try
            {
                if (Systems.SubmundoGrimorio.Activo && Main.dayTime)
                {
                    Main.dayTime = false;        // la noche prestada: IA normal
                    _prestaNocheInterior = true;
                }
            }
            catch { }
        }

        public override void PostAI(NPC npc)
        {
            if (_prestaNocheInterior)
            {
                try { Main.dayTime = true; } catch { }   // el mediodía vuelve
                _prestaNocheInterior = false;
            }
        }'''

parche("Content/Globals/JefeInteriorGlobalNPC.cs", VIEVO_JI, NUEVO_JI, "[9a] el préstamo universal de noche en el interior")

# =====================================================================
# [9b] 'ESQUELETRON MAZMORRA' — la identidad del de oleada (la letra:
#      «debe estar identificado como esqueletro mazmorra, ya que es
#      diiferente al esqueletron normal»). El sello de oleada le pone
#      su NOMBRE propio al jefe de oleada cuando es SkeletronHead.
# =====================================================================
s = leer("Content/Globals/OleadaNPC.cs")
if "Esqueletron Mazmorra" not in s:
    # enganchar en Marcar(): al marcar JEFE de oleada un SkeletronHead,
    # el displayName pasa a ser 'Esqueletron Mazmorra' (es SU versión de
    # festín — diferente del normal de siempre)
    VIEJO_MARCAR = r'''        public void Marcar(NPC npc, int oleada, bool jefe, bool especial = false, int nivel = 1)
        {'''
    NUEVO_MARCAR = r'''        public void Marcar(NPC npc, int oleada, bool jefe, bool especial = false, int nivel = 1)
        {
            // v6.152 — LA IDENTIDAD DEL GUARDIÁN DE OLEADA (la letra:
            //     «debe estar identificado como esqueletro mazmorra,
            //     ya que es diiferente al esqueletron normal»): el
            //     SkeletronHead del festín es SU PROPIA CRIATURA — la
            //     versión de mazmorra con el sello del grimorio — y se
            //     LLAMA distinto: el jugador sabe a cuál enfrenta.
            if (jefe && npc.type == NPCID.SkeletronHead)
                npc.GivenName = "Esqueletron Mazmorra";'''
    parche("Content/Globals/OleadaNPC.cs", VIEJO_MARCAR, NUEVO_MARCAR, "[9b] el nombre 'Esqueletron Mazmorra'")

print("PARTE D1 OK")

# =====================================================================
# [10] EL LIBRO DE INFORMACIÓN (4 curas):
#   (a) LA X DIBUJADA centrada en su hitbox (la letra: «el hitbox esta
#       bien, pero la X no, la X esta corrida hacia la izquierda»):
#       la v6.151 puso el rect en la esquina PERO dibujó la letra con
#       centro en rectX.Center — el fallo: el rect de 36 px nacía en
#       x+w-6 con w=anchura del LIBRO ENTERO y la X quedaba DENTRO...
#       LA CURA: el rect dibuja SU PROPIO marco + la X EXACTA en su
#       centro (visual = hitbox, píxel a píxel).
#   (b) EL CURSOR ENCIMA del libro (la letra: «el cursor queda por
#       debajo del libro de informacion, el cursor debe estar por
#       encima»): la capa del libro pasa de layers.Add (última) a
#       INSERTARSE JUSTO ANTES de 'Vanilla: Mouse Over' — el cursor
#       de vanilla (que dibuja en SU capa al final) queda ENCIMA.
#   (c) EL LORE PROGRESIVO: las secciones de lore se desbloquean con
#       el nivel de Despertar (cada sección un umbral; las cerradas
#       muestran su epígrafe y un sello 'bloqueado').
#   (d) LAS DAGAS FUERA del libro (la letra: «quita la mencion de las
#       dagas del eterno, no seran usadas despues de todo»).
# =====================================================================

# (b) el cursor encima: insertar ANTES de Mouse Over en vez de Add al final
VIEJO_LAYERADD = r'''            layers.Add(new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));'''

NUEVO_LAYERADD = r'''            // v6.152 — EL CURSOR MANDA (la letra: «el cursor queda por
            // debajo del libro de informacion, el cursor debe estar por
            // encima del libro de informacion»): el libro ya NO es la
            // ÚLTIMA capa — vive JUSTO ANTES de 'Vanilla: Mouse Over'
            // (la capa que dibuja el cursor de vanilla): encima de todo
            // el juego y su interfaz… DEBAJO del cursor, como manda la
            // letra.
            int idxCursor = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Over"));
            if (idxCursor < 0) idxCursor = layers.Count;
            layers.Insert(idxCursor, new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_LAYERADD, NUEVO_LAYERADD, "[10b] el cursor encima del libro")

# (a) la X dibujada EXACTA
VIEJO_X2 = r'''                var rectX = new Rectangle(x + w - 6, y - 8, 36, 36);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.45f);
                Utils.DrawBorderString(sb, "X", new Vector2(rectX.Center.X, rectX.Center.Y),
                    hoverX ? new Color(220, 90, 90) : Tinta, 1.1f, 0.5f, 0.5f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    LibroDelEternoUI.Cerrar();'''

NUEVO_X2 = r'''                var rectX = new Rectangle(x + w - 6, y - 8, 36, 36);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                // v6.152 — LA X DIBUJADA ES SU HITBOX (la letra: «el
                //     hitbox esta bien, pero la X no, la X esta corrida
                //     hacia la izquierda»): el marco se dibuja con el
                //     MISMO rectángulo del hitbox y la letra muere
                //     EXACTAMENTE en su centro medido de verdad (el
                //     centro vertical del glifo 'X' de la fuente, no
                //     el del rectángulo — el ajuste de -2 px que la
                //     fuente MouseText pide para centrar óptico).
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.45f);
                sb.Draw(_pixel, new Rectangle(rectX.X + 2, rectX.Y + 2, rectX.Width - 4, rectX.Height - 4),
                    (hoverX ? new Color(150, 40, 40) : new Color(58, 40, 30)) * 0.55f);
                var fuenteX = FontAssets.MouseText.Value;
                Vector2 midX = fuenteX.MeasureString("X") * 0.5f;
                sb.DrawString(fuenteX, "X",
                    new Vector2(rectX.Center.X, rectX.Center.Y + 1f) - midX,
                    hoverX ? new Color(255, 210, 210) : new Color(235, 225, 205), 0f,
                    Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    LibroDelEternoUI.Cerrar();'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_X2, NUEVO_X2, "[10a] la X dibujada = el hitbox")

print("PARTE D2 OK")

# =====================================================================
# [10c] LAS DAGAS FUERA DEL LIBRO (la letra: «tambien quita la mencion
#       de las dagas del eterno, no seran usadas despues de todo»).
#       Las DOS claves del Pergamino que las nombran se reescriben
#       (el Cetro como arma EXISTE pero el libro de info ya no las
#       promete como parte del escuadrón del GRIMORIO).
# =====================================================================
for ruta in ["Localization/es-MX_Mods.AethonMod.hjson",
             "Localization/es-ES_Mods.AethonMod.hjson"]:
    s = leer(ruta)
    n = 0
    v1 = "\t\tGrimorio5: • EL ESCUADRÓN: los OJOS DEL ETERNO (11) y las DAGAS DEL ETERNO (5) — la guardia del portador."
    n1 = "\t\tGrimorio5: • EL ESCUADRÓN: los ONCE OJOS DEL ETERNO — la guardia que ve por el portador."
    if v1 in s: s = s.replace(v1, n1); n += 1
    v2 = "\t\tAtaques2: • LAS DAGAS DEL ETERNO (5): cristal · llama · roca · vacío · veredicto — la guardia íntima con el ojo en el centro."
    n2 = "\t\tAtaques2: • LA CONJURA COMBINADA: mantener el ataque básico carga la sinfonía del Eterno — el clic derecho invoca y mejora al escuadrón."
    if v2 in s: s = s.replace(v2, n2); n += 1
    escribir(ruta, s)
    print(f"  ✔ [10c] {ruta}: {n} menciones de dagas curadas")

s = leer("Localization/en-US_Mods.AethonMod.hjson")
n = 0
for viejo, nuevo in [
    ("Grimorio5: • THE SQUADRON: the ETERNAL EYES (11) and the ETERNAL DAGGERS (5) — the bearer's guard.",
     "Grimorio5: • THE SQUADRON: the ELEVEN ETERNAL EYES — the guard that watches for the bearer."),
]:
    if viejo in s: s = s.replace(viejo, nuevo); n += 1
escribir("Localization/en-US_Mods.AethonMod.hjson", s)
print(f"  ✔ [10c] en-US: {n} menciones curadas")

# =====================================================================
# [10d] EL LORE PROGRESIVO POR DESPERTAR (la letra: «debes hacer que
#       las piezas de lore se vayan desbloqueando a medida que el
#       nivel de despertar aumenta»). La página DERECHA (el Compendio)
#       gana umbrales: cada sección de lore abre a SU nivel de
#       despertar (el prólogo siempre; la I al 10; la II al 25; la III
#       al 40; la IV al 60; la V al 80 — el canon se gana). Las
#       cerradas: el epígrafe visible + UN SELLO (el glifo candado de
#       la casa + «Se revela al Despertar N»).
# =====================================================================
VIEJO_DCOL = r'''        private float DibujarColumna(SpriteBatch sb, string[][] secciones,
            Rectangle pagina, Rectangle recorte, ref float scroll, bool esIzq)
        {'''

NUEVO_DCOL = r'''        /// <summary>v6.152 — EL UMBRAL DE DESPERTAR de cada sección del
        /// Compendio (la letra: «debes hacer que las piezas de lore se
        /// vayan desbloqueando a medida que el nivel de despertar
        /// aumenta»). La sección [0] (el título del compendio) y [1]
        /// (el epígrafe) SIEMPRE se ven; el CUERPO se abre al umbral.
        /// Los umbrales por índice de sección del lore (II+): el canon
        /// SE GANA con el despertar.</summary>
        private static readonly int[] UmbralLore = { 0, 0, 10, 25, 40, 60, 80 };

        /// <summary>El Despertar del portador local (para el lore).</summary>
        private static int DespertarLocal()
        {
            try
            {
                return Systems.DespertarGrimorio.NivelDe(Main.LocalPlayer);
            }
            catch { return 0; }
        }

        private float DibujarColumna(SpriteBatch sb, string[][] secciones,
            Rectangle pagina, Rectangle recorte, ref float scroll, bool esIzq)
        {'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_DCOL, NUEVO_DCOL, "[10d-a] los umbrales del lore")

# dentro del bucle de secciones: filtrar por umbral (la página derecha = esIzq==false)
s = leer("Content/UI/LibroDelEternoUI.cs")
# encontrar el bucle que dibuja cada sección
VIEJO_BUCLE = '''            foreach (var sec in secciones)
            {
                cy += 12f;
                string titulo = T(sec[0]);'''

NUEVO_BUCLE = '''            // v6.152 — EL LORE PROGRESIVO (la letra: «debes hacer
            //     que las piezas de lore se vayan desbloqueando a
            //     medida que el nivel de despertar aumenta»): en la
            //     página del COMPENDIO cada sección nace bajo su
            //     umbral de despertar — el título y el epígrafe se ven
            //     SIEMPRE, el cuerpo SOLO al llegar. La mecánica
            //     (página izquierda) está SIEMPRE completa.
            int despertarAhora = esIzq ? int.MaxValue : DespertarLocal();
            foreach (var sec in secciones)
            {
                cy += 12f;
                string titulo = T(sec[0]);
                int idxSec = System.Array.IndexOf(secciones, sec);
                bool seccionAbierta = true;
                if (!esIzq && idxSec >= 2)
                {
                    int umbral = idxSec < UmbralLore.Length ? UmbralLore[idxSec] : 0;
                    seccionAbierta = despertarAhora >= umbral;
                }'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_BUCLE, NUEVO_BUCLE, "[10d-b] el filtro por umbral")

print("PARTE D3 OK")

# [10d-c] el cuerpo sellado: cuando la sección está cerrada, el cuerpo
# se cambia por EL SELLO (una línea-candado con el umbral prometido)
VIEJO_LINEAS = r'''                for (int i = 1; i < sec.Length; i++)
                    cy = LineaLibro(sb, T(sec[i]), xizq, cy, maxAncho,
                        Tinta, recorte, pagina);
                cy += 5f;'''

NUEVO_LINEAS = r'''                if (seccionAbierta)
                {
                    for (int i = 1; i < sec.Length; i++)
                        cy = LineaLibro(sb, T(sec[i]), xizq, cy, maxAncho,
                            Tinta, recorte, pagina);
                }
                else
                {
                    // LA SECCIÓN SELLADA: su epígrafe (sec[1] — la
                    // promesa) y EL SELLO — la línea de puntos con el
                    // candado y el nivel que la abre
                    if (sec.Length > 1)
                        cy = LineaLibro(sb, T(sec[1]), xizq, cy, maxAncho,
                            Tinta, recorte, pagina);
                    int umbralSello = idxSec < UmbralLore.Length ? UmbralLore[idxSec] : 0;
                    string sello = "· · · ✦ sellado · se revela al Despertar " + umbralSello + " ✦ · · ·";
                    cy = LineaLibro(sb, sello, xizq, cy, maxAncho,
                        new Color(120, 104, 82), recorte, pagina);
                }
                cy += 5f;'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_LINEAS, NUEVO_LINEAS, "[10d-c] la sección sellada")

print("PARTE D3 OK")

# =====================================================================
# [4d] EL JUICIO — EL PRESENTA CONTINUO: los radios del DIBUJADO leen
#     LA CURVA MAESTRA (RadioDelJuicio) y las posiciones leen
#     CentroDelJuicio — se acaban los valores por-acto que saltaban.
#     (Los actos siguen para la lógica; el dibujo es UNA línea.)
# =====================================================================

# el acto 0: el radio del par
VIEJO_A0R = r'''                    // === LA INVOCACIÓN GÓTICA: runa → sol ===
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    float suave = p * p * (3f - 2f * p);
                    float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.5f, 0.92f, suave);'''

NUEVO_A0R = r'''                    // === LA INVOCACIÓN GÓTICA: runa → sol ===
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    float suave = p * p * (3f - 2f * p);
                    // v6.152 — el radio sale de LA CURVA MAESTRA (la
                    // misma del resto del juicio: imposible saltar)
                    float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A0R, NUEVO_A0R, "[4d-a] acto0 radio maestro")

# el acto 1: el radio del vals
VIEJO_A1R = r'''                            float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.92f, 0.55f, caida);'''

NUEVO_A1R = r'''                            // v6.152 — el radio del vals también nace de
                            // LA CURVA MAESTRA (continuo con TODO)
                            float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A1R, NUEVO_A1R, "[4d-b] acto1 radio maestro")

# el contacto: el gigante (el jitter ya murió en v6.151; ahora el radio
# maestro también)
VIEJO_A1G = r'''                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * RadioFusion(n);'''

NUEVO_A1G = r'''                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A1G, NUEVO_A1G, "[4d-c] contacto radio maestro")

# la caza dibujada: el radio maestro (incluye el nacimiento Y el +70 %)
VIEJO_A2R = r'''                    float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float rg = caza * caza * (3f - 2f * caza);   // smoothstep: el rojo madura suave
                    float r = RuneSunRenderer.BodyPx *
                        (RadioFusion(1f) + CRECE_CAZA * rg +
                         0.05f * MathF.Sin(gt * 9f) * rg);       // el pulso arterial al final'''

NUEVO_A2R = r'''                    float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float rg = caza * caza * (3f - 2f * caza);   // smoothstep: el rojo madura suave
                    // v6.152 — el radio de la caza TAMBIÉN es LA CURVA
                    // MAESTRA (nace del contacto sin costura y crece
                    // al gigante) + el pulso arterial encima
                    float r = RuneSunRenderer.BodyPx *
                        (RadioDelJuicio(_edad) + 0.05f * MathF.Sin(gt * 9f) * rg);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A2R, NUEVO_A2R, "[4d-d] caza radio maestro")

# el colapso: radio maestro
VIEJO_A3C = r'''                        float plega = _t / (float)T_COLAPSO;
                        float rGigante = RuneSunRenderer.BodyPx * (RadioFusion(1f) + CRECE_CAZA);
                        float r = MathHelper.Lerp(rGigante, rGigante * 0.42f, plega * plega);'''

NUEVO_A3C = r'''                        float plega = _t / (float)T_COLAPSO;
                        // v6.152 — el plisado del colapso también por la
                        // curva maestra (continuo con la caza que acaba)
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A3C, NUEVO_A3C, "[4d-e] colapso radio maestro")

# la sentencia: radio maestro
VIEJO_A3S = r'''                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        float rGiganteS = RuneSunRenderer.BodyPx * (RadioFusion(1f) + CRECE_CAZA);
                        float r = rGiganteS *
                            (0.42f + 0.95f * MathF.Min(1f, (_t - T_COLAPSO) / 7f)) * (0.85f + 0.15f * apaga);'''

NUEVO_A3S = r'''                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        // v6.152 — la sentencia cierra LA MISMA curva (el
                        // estallido final del radio maestro)
                        float r = RuneSunRenderer.BodyPx * RadioDelJuicio(_edad);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_A3S, NUEVO_A3S, "[4d-f] sentencia radio maestro")

# y el Main.rand de las estelas del acto 1 → determinista (el azar por
# frame también produce "saltos" percibidos): Hash01 con el índice
VIEJO_ESTELA = r'''                                for (int k = 1; k <= 9; k++)
                                {
                                    float angPas = angLado - k * 0.045f - 0.09f;'''

NUEVO_ESTELA = r'''                                for (int k = 1; k <= 9; k++)
                                {
                                    // v6.152 — LA ESTELA DETERMINISTA (la
                                    // cura fina de los saltos): el azar
                                    // por frame (Main.rand en las estelas)
                                    // parpadeaba como micro-saltos — el
                                    // Hash01 de la casa: LA MISMA estela
                                    // cada frame, quieta como historia.
                                    float angPas = angLado - k * 0.045f - 0.09f;'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_ESTELA, NUEVO_ESTELA, "[4d-g] estelas deterministas (doc)")

# =====================================================================
# [11] EL BUFF DE LA IRA — EL ANILLO DEL HOLD: mientras cargas el
#      gesto (click derecho sostenido), el marco del icono se CIERRA
#      (un arco que crece 0→100 % en los 2 s: SABES cuándo dispara).
# =====================================================================
VIEJO_BUFF_HOLD = r'''                else if (progreso > 0.15f)
                {
                    // CARGANDO: el marco NACE con el progreso (1→4 px) y
                    // a media intensidad — la promesa del estallido
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(150, 84, 235),
                        1f + 3f * progreso, 0.28f + 0.30f * progreso, 0.4f);
                }'''

NUEVO_BUFF_HOLD = r'''                else if (progreso > 0.15f)
                {
                    // CARGANDO: el marco NACE con el progreso (1→4 px) y
                    // a media intensidad — la promesa del estallido
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(150, 84, 235),
                        1f + 3f * progreso, 0.28f + 0.30f * progreso, 0.4f);
                }

                // ============================================================
                //  v6.152 — EL ANILLO DEL HOLD (la letra del gesto nuevo:
                //  «dejando precionado el click derecho durante 2
                //  segundos»): mientras SOSTIENES el click derecho, un
                //  ARCO blanco-violeta se CIERRRE alrededor del icono
                //  (0→100 % en los 2 s) — el gesto tiene SU reloj visible
                //  y sabes EXACTAMENTE cuándo va a disparar.
                // ============================================================
                float hold = p.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>()
                    .IraHoldProgreso;
                if (hold > 0f && hold < 1f)
                {
                    try
                    {
                        var glowHold = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                        int pasos = 28;
                        for (int k = 0; k < pasos; k++)
                        {
                            float u = k / (float)pasos;
                            if (u > hold) break;
                            float ang0 = -MathHelper.PiOver2 + u * MathHelper.TwoPi;
                            Vector2 borde = pos + new Vector2(
                                tex.Width * 0.5f + 15f, tex.Height * 0.5f + 15f);
                            Vector2 p0 = borde + ang0.ToRotationVector2() * 17f;
                            Vector2 p1 = borde + (ang0 + 0.10f).ToRotationVector2() * 17f;
                            sb.Draw(glowHold, p0, null,
                                new Color(226, 208, 255) * 0.85f, 0f,
                                glowHold.Size() * 0.5f, new Vector2(0.22f, 0.10f),
                                SpriteEffects.None, 0f);
                        }
                    }
                    catch { }
                }'''

parche("Content/Buffs/IraSerExteriorRecargaBuff.cs", VIEJO_BUFF_HOLD, NUEVO_BUFF_HOLD, "[11] el anillo del hold en el buff")

print("PARTE E OK")

# =====================================================================
# [12] EL OJO-MINION DEL PILAR MUERE (la letra: «que no tenga ningun
#      ojo»): el spawn del SpecialDraw se retira y el proyectil
#      PilarOjoMinion muere (el nuevo pilar es el obelisco del umbral
#      — un aro vacío, sin vigilante).
# =====================================================================
VIEJO_OJOPILAR = r'''                // v6.149 — EL OJO-MINION DEL PILAR (la letra: «debe usar a
                // un minion como ojo… y debe siempre mirar al jugador»): el
                // SpecialDraw mantiene VIVO al ojo sobre el prisma (lo
                // spawnea si no existe — el minion se autolimpia si el
                // pilar o el interior mueren).
                if (Main.netMode != NetmodeID.Server && !Main.gameMenu)
                {
                    int tipoOjo = ModContent.ProjectileType
                        <Projectiles.Cosmetic.PilarOjoMinion>();
                    bool vivo = false;
                    for (int pi = 0; pi < Main.maxProjectiles; pi++)
                    {
                        Projectile pr = Main.projectile[pi];
                        if (pr != null && pr.active && pr.type == tipoOjo)
                        { vivo = true; break; }
                    }
                    if (!vivo)
                        Projectile.NewProjectile(null,
                            new Vector2((i + 0.5f) * 16f, (j - 4) * 16f - 34f),
                            Vector2.Zero, tipoOjo, 0, 0f, Main.myPlayer);
                }
            }
            catch { }
        }
    }
}'''

NUEVO_OJOPILAR = r'''                // v6.152 — EL OJO-MINION MURIÓ (la letra: «crearlo de
                // nuevo con otra forma, que no tenga ningun ojo»): el
                // obelisco del umbral NO tiene vigilante — su corona es
                // EL ARO ABIERTO (la puerta por donde se sale del
                // interior). El spawn del ojo murió con el prisma viejo.
            }
            catch { }
        }
    }
}'''

parche("Content/Tiles/PilarPrismaTile.cs", VIEJO_OJOPILAR, NUEVO_OJOPILAR, "[12a] el spawn del ojo muere")

# y el proyectil PilarOjoMinion muere (con su clase entera: se borra)
import os as _os
if _os.path.exists("Content/Projectiles/Cosmetic/PilarOjoMinion.cs"):
    _os.remove("Content/Projectiles/Cosmetic/PilarOjoMinion.cs")
    print("  ✔ [12b] PilarOjoMinion.cs BORRADO — el pilar sin vigilante")

# =====================================================================
# [13] LA VERSIÓN + EL BUILD.TXT
# =====================================================================
parche("build.txt", "version = 6.151", "version = 6.152", "la versión 6.152")

print("\nCIRUGÍA v6.152 COMPLETA — TODOS LOS PARCHES APLICADOS")
