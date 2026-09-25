using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// GolpeMotor — v6.50 — LA MIGRACIÓN DEL DAÑO AL MOTOR.
    ///
    /// LA DEUDA (hallazgo AUD-B nº5): ~115 call-sites de la "escuela A"
    /// golpeaban con `npc.SimpleStrikeNPC(...)`: daño custom que IBALE a
    /// las reglas del juego — sin tirada de CRÍTICA con las stats del
    /// jugador, sin varianza ±15% ni SUERTE, sin penetración de armadura,
    /// sin TODOS los on-hit del motor (quemadura de la armadura de
    /// escarcha, venenos de poción, efectos de accesorios) y con la
    /// sincronización MP resuelta a mano.
    ///
    /// EL CAMINO DEL MOTOR: `Projectile.Damage()` — el mismo cauce que usa
    /// vanilla para cada espada y cada proyectil (verificado en el IL real
    /// de tModLoader 2026.07.3.0):
    ///   · tira la CRÍTICA con la chance REAL del dueño (CritChance del
    ///     proyectil + stats del jugador — el pipeline usa SOLO
    ///     Projectile.CritChance, calibrado aquí con las stats del dueño),
    ///   · aplica varianza de daño ±15% CON la suerte del jugador,
    ///   · respeta i-frames del NPC según el modo (npc.immune[owner] /
    ///     localNPCImmunity),
    ///   · dispara `ModifyHitNPCWithProj` + `OnHitNPCWithProj` → TODOS los
    ///     on-hit del motor y del mod (armaduras, pociones, ganchos),
    ///   · da el CRÉDITO de la kill al dueño (Player.OnKillNPC: drops,
    ///     banners, XP del grimorio vía GlobalNPCXP.OnHitByProjectile),
    ///   · en MP el CLIENTE DUEÑO resuelve el golpe y
    ///     `NetMessage.SendStrikeNPC` lo difunde — el modelo vanilla.
    ///
    /// v6.50.1 — LA PUERTA DEL BLANCO EXACTO (lección del IL): el bucle de
    /// `Damage()` recorre los 200 NPC y golpea a TODO lo que interseque la
    /// hitbox prestada — con la hitbox clonada sobre el blanco, cualquier
    /// NPC apilado sobre él (enjambres, gusanos, oleadas) recibía el golpe
    /// FUERA de la geometría del llamador, y en los `foreach` que golpean
    /// a varios NPC por tick, el MISMO enemigo podía recibir el golpe
    /// DOBLE. GolpeGateNPC/Proy (globals) responden al pipeline solo
    /// durante el instante del golpe: el blanco pasa por el camino vanilla
    /// (null — respeta el modo bala/ciudadano y sus i-frames), todo lo
    /// demás recibe false y queda fuera. La escuela A golpeaba a UN
    /// blanco: ahora el motor también.
    ///
    /// EL CONTRATO (la escuela A queda INTACTA en su lógica):
    /// - El llamador sigue decidiendo GEOMETRÍA (banda del tajo, aura del
    ///   agujero, nova del cometa) y CADENCIA (sus banderas de "una vez",
    ///   sus intervalos por enemigo): el golpe solo CAMBIA DE CAUCE.
    /// - `unico: true` (default): el golpe se comporta como UNA BALA del
    ///   motor (maxPenetrate==1: vanilla permite a los proyectiles de un
    ///   solo golpe saltarse los i-frames del NPC — así una andanada de
    ///   tajos simultáneos pega como una andanada de flechas). v6.50.1:
    ///   durante el golpe también se prestan usesLocalNPCImmunity/
    ///   usesIDStaticNPCImmunity (en false) — si el proyectil las usa, el
    ///   flag10 del motor (la llave de la bala) no se daba y el golpe
    ///   quedaba secuestrado por la inmunidad LOCAL del proyectil (el
    ///   contacto del propio minion ya la había marcado). Mantiene la
    ///   balanza EXACTA de la escuela A.
    /// - `unico: false`: el golpe ES un ciudadano de i-frames completo
    ///   (respeta npc.immune[owner] de 10 ticks — para auras/zonas
    ///   continuas, donde el intervalo del llamador ya es ≥ 10). Aquí las
    ///   inmunidades locales NO se tocan: el ciudadano usa las suyas.
    ///
    /// LA FOTOGRAFÍA: position/size/damage/penetrate/maxPenetrate/knockBack/
    /// CritChance/ArmorPenetration/friendly/hostile/direction (y las banderas
    /// de inmunidad local en modo bala) se prestan al golpe UN INSTANTE y
    /// vuelven — ni el render ni el resto de la IA ven el préstamo (AI corre
    /// antes del dibujo; todo se restaura dentro del mismo tick). La
    /// restauración vive en `finally`: si cualquier hook del pipeline
    /// (ModifyHitNPCWithProj de OTRO mod incluido) lanza, el proyectil NUNCA
    /// se queda con el cuerpo prestado — y en el catch de emergencia se
    /// recompone también npc.position (el motor la corre +netOffset antes de
    /// la colisión y la devuelve en cada salida: una excepción a mitad de
    /// cauce la dejaría desplazada).
    ///
    /// REGLAS DE LA CASA: cero alocaciones (todo por campos), cero
    /// Main.rand propio (la varianza la tira el motor), catch de contención
    /// (un golpe no puede tumbar un frame), gate reentrante (save/restore:
    /// un golpe puede nacer dentro del OnHitNPC de otro — Morir() del dardo
    /// de la supernova lo hace).
    /// </summary>
    public static class GolpeMotor
    {
        // === LA PUERTA (la consulta GolpeGate — ints, sin alocación) ===
        private static int _proyActivo = -1;   // whoAmI del proyectil que golpea
        private static int _npcObjetivo = -1;  // whoAmI del blanco exacto

        /// <summary>¿Hay un golpe de ESTE proyectil en curso?</summary>
        internal static bool EnCurso(int proyWhoAmI)
            => _proyActivo >= 0 && _proyActivo == proyWhoAmI;

        /// <summary>¿Es este el blanco del golpe en curso?</summary>
        internal static bool EsObjetivo(int npcWhoAmI)
            => _npcObjetivo == npcWhoAmI;

        /// <summary>
        /// EL GOLPE DEL MOTOR. Sustituye 1:1 a
        /// `npc.SimpleStrikeNPC(dano, dir, false, kb, DamageClass.X)`:
        /// mismo daño final, mismo knockback — otro cauce.
        /// </summary>
        /// <param name="p">El proyectil que golpea (el del llamador).</param>
        /// <param name="npc">El blanco (ya filtrado por el llamador).</param>
        /// <param name="dano">El daño final del sub-ataque (como hoy: el
        /// llamador lo calcula de Projectile.damage y sus escalas).</param>
        /// <param name="knockback">El empuje del sub-ataque.</param>
        /// <param name="unico">true (default): golpe tipo bala (sin i-frames,
        /// como la escuela A). false: respeta los i-frames del motor.</param>
        /// <returns>true si el golpe fue procesado por el cauce del motor.</returns>
        public static bool Golpear(Projectile p, NPC npc, int dano, float knockback = 0f, bool unico = true)
        {
            if (p == null || !p.active || npc == null || !npc.active) return false;

            // SOLO EL DUEÑO RESUELVE EL GOLPE (el modelo del motor): en
            // SP el dueño es el jugador local; en MP el cliente dueño
            // computa y NetMessage.SendStrikeNPC difunde; servidor y
            // demás clientes no repiten (reemplaza al viejo guard
            // "netMode != MultiplayerClient" — y lo corrige: el golpe
            // ahora lleva las stats DEL DUEÑO, no las del server).
            if (p.owner != Main.myPlayer) return false;

            Player dueno = Main.player[p.owner];
            if (dueno == null || !dueno.active) return false;

            // === LA FOTOGRAFÍA (todo se presta y se devuelve) ===
            Vector2 pos = p.position;
            int ancho = p.width, alto = p.height;
            int danoReal = p.damage;
            int penReal = p.penetrate;
            int maxPenReal = p.maxPenetrate;
            float kbReal = p.knockBack;
            int critReal = p.CritChance;
            int apReal = p.ArmorPenetration;
            bool friendlyReal = p.friendly;
            bool hostileReal = p.hostile;
            int dirReal = p.direction;
            bool usaInmunidadLocal = p.usesLocalNPCImmunity;
            bool usaInmunidadId = p.usesIDStaticNPCImmunity;
            Vector2 npcPos = npc.position;   // emergencia: el motor la corre +netOffset

            // === EL INSTANTE DEL GOLPE ===
            // La hitbox ADOPTA el cuerpo del blanco: Colliding pasa por
            // construcción (rectas idénticas se intersecan) y el motor
            // ve exactamente este blanco. La PUERTA (GolpeGate) se ocupa
            // de que los demás NPC apilados sobre él queden fuera.
            p.position = npc.position;
            p.width = npc.width;
            p.height = npc.height;

            // El daño del sub-ataque viaja por el cauce del motor.
            p.damage = dano > 0 ? dano : 1;
            p.knockBack = knockback;
            p.friendly = true;
            p.hostile = false;

            if (unico)
            {
                // GOLPE TIPO BALA (vanilla flag10): los proyectiles de
                // un solo golpe saltan los i-frames del NPC — cada tajo
                // de la andanada pega, como cada flecha de una ráfaga.
                // Las inmunidades LOCALES también se prestan en false:
                // flag10 solo se da con maxPenetrate==1 SIN inmunidad
                // local/por-ID (proyectiles de minion la usan).
                p.maxPenetrate = 1;
                p.penetrate = 1;
                p.usesLocalNPCImmunity = false;
                p.usesIDStaticNPCImmunity = false;
            }
            else
            {
                // CIUDADANO DE i-FRAMES: como el láser o la llama —
                // respeta npc.immune[owner] (10 ticks) y, si el
                // proyectil usa inmunidad local, las suyas propias.
                p.maxPenetrate = -1;
                p.penetrate = -1;
            }

            // LAS STATS DEL DUEÑO, DE VERDAD: la crítica y la penetración
            // del jugador (el pipeline del motor usa SOLO las propiedades
            // del proyectil — verificado en el IL: `rand.Next(100) <
            // CritChance` y `modifiers.ArmorPenetration += ArmorPenetration`)
            // — si el proyectil ya traía mejores (disparo de arma con
            // stats calzadas), se conservan.
            float critDueno = dueno.GetTotalCritChance(p.DamageType);
            if ((int)critDueno > p.CritChance) p.CritChance = (int)critDueno;
            float apDueno = dueno.GetTotalArmorPenetration(p.DamageType);
            if ((int)apDueno > p.ArmorPenetration) p.ArmorPenetration = (int)apDueno;

            // LA DIRECCIÓN DEL EMPUJE: el motor toma p.direction para el
            // HitDirection del golpe — la escuela A la pasaba explícita.
            // La geométrica: desde el CENTRO ORIGINAL del proyectil hacia
            // el blanco (el empuje SIEMPRE aleja del origen del ataque).
            Vector2 centroOriginal = new Vector2(pos.X + ancho * 0.5f, pos.Y + alto * 0.5f);
            p.direction = npc.Center.X >= centroOriginal.X ? 1 : -1;

            // LA PUERTA: mientras dura el golpe, el pipeline solo admite
            // al blanco exacto (save/restore — reentrante: un golpe puede
            // nacer del OnHitNPC de otro golpe en curso).
            int proyPrevio = _proyActivo;
            int npcPrevio = _npcObjetivo;
            _proyActivo = p.whoAmI;
            _npcObjetivo = npc.whoAmI;

            try
            {
                // EL CAUCE: crítica real, varianza ±15% con suerte, defensa,
                // armadura/penetración, on-hit de TODO el juego, crédito de
                // kill, y en MP la difusión por red del propio motor.
                p.Damage();
                return true;
            }
            catch
            {
                // Emergencia: el motor pudo dejar npc.position corrida por
                // netOffset a mitad de cauce (la devuelve en cada salida,
                // pero una excepción salta esas devoluciones). La foto es
                // el valor PRE-cauce: recomponerla es no-op si el motor ya
                // había devuelto la suya.
                try { if (npc != null && npc.active) npc.position = npcPos; } catch { }
                return false;
            }
            finally
            {
                // === LA RESTAURACIÓN (siempre — idempotente) ===
                _proyActivo = proyPrevio;
                _npcObjetivo = npcPrevio;
                p.position = pos;
                p.width = ancho;
                p.height = alto;
                p.damage = danoReal;
                p.penetrate = penReal;
                p.maxPenetrate = maxPenReal;
                p.knockBack = kbReal;
                p.CritChance = critReal;
                p.ArmorPenetration = apReal;
                p.friendly = friendlyReal;
                p.hostile = hostileReal;
                p.direction = dirReal;
                p.usesLocalNPCImmunity = usaInmunidadLocal;
                p.usesIDStaticNPCImmunity = usaInmunidadId;
            }
        }

        /// <summary>
        /// EL GOLPE POR ESCALA — el atajo para los sitios que calculaban
        /// `dano = (int)(Projectile.damage * f)`: la escala del sub-ataque
        /// y el resto igual que <see cref="Golpear"/>.
        /// </summary>
        public static bool Golpear(Projectile p, NPC npc, float escala, float knockback, bool unico)
            => Golpear(p, npc, Escalar(p, escala), knockback, unico);

        private static int Escalar(Projectile p, float escala)
        {
            if (escala <= 0f) escala = 1f;
            return System.Math.Max(1, (int)(p.damage * escala));
        }
    }

    /// <summary>
    /// v6.50.1 — LA PUERTA DEL GOLPE. `Projectile.Damage()` recorre los
    /// 200 NPC y golpea todo lo que interseca la hitbox; con la hitbox
    /// prestada sobre el blanco (GolpeMotor), eso incluía a cualquier NPC
    /// apilado sobre él — fuera de la geometría del llamador, y con doble
    /// golpe en los `foreach` multi-blanco. Dos globals que solo opinan
    /// DURANTE el instante de un golpe (dos comparaciones de int en el
    /// resto del juego):
    ///  · GolpeGateNPC (GlobalNPC.CanBeHitByProjectile — el gancho que el
    ///    CombinedHooks del motor consulta por cada NPC del bucle): el
    ///    blanco exacto → null (camino vanilla: el modo bala/ciudadano
    ///    decide los i-frames como el motor manda); cualquier otro NPC →
    ///    false (fuera del golpe).
    ///  · GolpeGateProy (GlobalProjectile.CanCutTiles) en false durante el
    ///    golpe: el cauce del motor siega hierba en la hitbox (SendData(17)
    ///    incluido en MP) — la escuela A jamás tocó el terreno con un
    ///    sub-ataque; el golpe tampoco.
    /// </summary>
    public class GolpeGateNPC : GlobalNPC
    {
        public override bool? CanBeHitByProjectile(NPC target, Projectile projectile)
        {
            if (GolpeMotor.EnCurso(projectile.whoAmI))
            {
                if (GolpeMotor.EsObjetivo(target.whoAmI))
                    return null;       // el blanco: camino vanilla completo
                return false;          // los apilados: fuera del golpe
            }
            return null;               // sin golpe en curso: el juego decide
        }
    }

    public class GolpeGateProy : GlobalProjectile
    {
        public override bool? CanCutTiles(Projectile projectile)
        {
            if (GolpeMotor.EnCurso(projectile.whoAmI))
                return false;          // el golpe no siega el terreno
            return null;
        }
    }
}
