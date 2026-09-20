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
    ///     proyectil + stats del jugador),
    ///   · aplica varianza de daño ±15% CON la suerte del jugador,
    ///   · respeta i-frames del NPC (npc.immune[owner] / localNPCImmunity),
    ///   · dispara `ModifyHitNPCWithProj` + `OnHitNPCWithProj` → TODOS los
    ///     on-hit del motor y del mod (armaduras, pociones, ganchos),
    ///   · da el CRÉDITO de la kill al dueño (Player.OnKillNPC: drops,
    ///     banners, XP del grimorio vía GlobalNPCXP.OnHitByProjectile),
    ///   · en MP el CLIENTE DUEÑO resuelve el golpe y
    ///     `NetMessage.SendStrikeNPC` lo difunde — el modelo vanilla.
    ///
    /// EL CONTRATO (la escuela A queda INTACTA en su lógica):
    /// - El llamador sigue decidiendo GEOMETRÍA (banda del tajo, aura del
    ///   agujero, nova del cometa) y CADENCIA (sus banderas de "una vez",
    ///   sus intervalos por enemigo): el golpe solo CAMBIA DE CAUCE.
    /// - `unico: true` (default): el golpe se comporta como UNA BALA del
    ///   motor (maxPenetrate==1: vanilla permite a los proyectiles de un
    ///   solo golpe saltarse los i-frames del NPC — así una andanada de
    ///   tajos simultáneos pega como una andanada de flechas). Mantiene la
    ///   balanza EXACTA de la escuela A.
    /// - `unico: false`: el golpe ES un ciudadano de i-frames completo
    ///   (respeta npc.immune[owner] de 10 ticks — para auras/zonas
    ///   continuas, donde el intervalo del llamador ya es ≥ 10).
    ///
    /// LA FOTOGRAFÍA: position/size/damage/penetrate/knockBack/CritChance/
    /// ArmorPenetration/friendly se prestan al golpe UN INSTANTE y vuelven
    /// — ni el render ni el resto de la IA ven el préstamo (AI corre antes
    /// del dibujo; todo se restaura dentro del mismo tick).
    ///
    /// REGLAS DE LA CASA: cero alocaciones (todo por campos), cero
    /// Main.rand propio (la varianza la tira el motor), catch de contención
    /// (un golpe no puede tumbar un frame).
    /// </summary>
    public static class GolpeMotor
    {
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
            try
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

                // === EL INSTANTE DEL GOLPE ===
                // La hitbox ADOPTA el cuerpo del blanco: Colliding pasa por
                // construcción (rectas idénticas se intersecan) y el motor
                // ve exactamente este blanco.
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
                    // GOLPE TIPO BALA (vanilla flag11): los proyectiles de
                    // un solo golpe saltan los i-frames del NPC — cada tajo
                    // de la andanada pega, como cada flecha de una ráfaga.
                    p.maxPenetrate = 1;
                    p.penetrate = 1;
                }
                else
                {
                    // CIUDADANO DE i-FRAMES: como el láser o la llama —
                    // respeta npc.immune[owner] (10 ticks).
                    p.maxPenetrate = -1;
                    p.penetrate = -1;
                }

                // LAS STATS DEL DUEÑO, DE VERDAD: la crítica y la penetración
                // del jugador (como todo golpe del motor) — si el proyectil
                // ya traía mejores (disparo de arma con stats calzadas), se
                // conservan.
                float critDueno = dueno.GetTotalCritChance(p.DamageType);
                if ((int)critDueno > p.CritChance) p.CritChance = (int)critDueno;
                float apDueno = dueno.GetTotalArmorPenetration(p.DamageType);
                if ((int)apDueno > p.ArmorPenetration) p.ArmorPenetration = (int)apDueno;

                // EL CAUCE: crítica real, varianza ±15% con suerte, defensa,
                // armadura/penetración, on-hit de TODO el juego, crédito de
                // kill, y en MP la difusión por red del propio motor.
                p.Damage();

                // === LA RESTAURACIÓN ===
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
                return true;
            }
            catch { return false; }
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
}
