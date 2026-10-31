using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// OJO ETERNO MINION — v6.50.109 — LA MITAD Y LA FORMACIÓN GIRATORIA
    /// DEL RUINOUS.
    ///
    /// La letra de la .109: «reduce el tamaño de los minion en un 50% y
    /// mejora la animación, que sea más parecida a Ruinous_Staff en el
    /// momento en el que están alrededor del libro» + «añadir una
    /// estadística para el grimorio eterno que mejore a los minion»
    /// (LA IRA EN CADENA: los rayos saltan a +1 enemigo cada 20 niveles,
    /// base 2 → tope 7 en nivel 100 — vive en WeaponScaling.
    /// MinionChainCount y en TruenoPerlinProjectile.Strike).
    ///
    /// EL TAMAÑO: ESCALA 0,375 — LA MITAD EXACTA de la .107 (0,75): el
    /// ojo 56×54 → 21×20 px en pantalla. Hitbox, sello, aliento, iris
    /// y mirada escalan con la MISMA constante.
    ///
    /// EL RUINOUS (la wiki del Forbidden Ruin — el minion del Ruinous
    /// Staff): «they form a spinning orbital formation» — los anillos
    /// GIRAN A VELOCIDAD CONSTANTE (MUERE la elipse kepleriana de la
    /// .107 con su aceleración de perigeo: el giro leía a tirones), el
    /// radio RESPIRA despacio (±3,5%) y cada ojo lleva su micro-deriva
    /// propia (1,6 px, 2D). Y en la carga: «continuously adjusts its
    /// position to stay along the line segment between the player and
    /// its target» — el ojo YA NO frena en seco: SE REPOSICIONA
    /// deslizándose hacia la línea ancla↔presa (una LEVANTADA hacia la
    /// presa — el movimiento NUNCA muere).
    ///
    /// EL RITMO PROPIO (la .107, INTACTO): cada ojo dispara a SU
    /// velocidad — 110 ticks de base divididos por la VELOCIDAD DE
    /// NIVEL (1,0× en nivel 1 subiendo LINEAL hasta 1,5× = un 50%
    /// más rápido en nivel 100) y por el RITMO PERSONAL del ojo
    /// (0,85–1,15 por hash de whoAmI). El primer disparo tras invocar
    /// cae entre el 35% y el 100% del intervalo: la banda NUNCA dispara
    /// en bloque — cada ojo es INDIVIDUAL.
    ///
    /// EL RELOJ DE LA ÓRBITA (v6.51.7): el ángulo del abanico
    /// vive en GlobalTimeWrappedHourly — determinista en todos
    /// los clientes, SIN campo por ojo, SIN netcode (la fase
    /// orbital de instancia murió con la danza atómica).
    ///
    /// EL RAYO (la finura de la .107): el proyectil DEL CETRO DEL TRUENO
    /// PERLIN en su modo ojo, un POCO MÁS FINO — funda 6,0 (era 7,5),
    /// cápsulas negras de 15 px (era 19), cadenas 2,9 (la cirugía vive
    /// en TruenoPerlinProjectile.DrawArcoOjo).
    ///
    /// (El resto del repertorio hereda de la .106: el sprite del usuario
    /// SIN recorte con el iris ACOMODADO, la ley del planeo del Ruinous
    /// Staff, el arco del Sanguine que respira, el iris que manda, el
    /// parpadeo independiente, el círculo mágico de los agujeros negros
    /// y el retroceso con reincorporación.)
    /// </summary>
    public class OjoEternoMinion : ModProjectile
    {
        // === LOS MODOS (ai[0]) — la carga y el crucero ===
        private const float MODO_CRUCERO = 0f;     // el planeo continuo (la ley única)
        private const float MODO_APUNTANDO = 1f;   // la carga: freno, el sello respira

        private const int AIM_TICKS = 12;          // la carga del disparo (0,2 s)

        // === v6.50.109 — EL TAMAÑO A LA MITAD (la letra: «reduce el
        //     tamaño de los minion en un 50%»): la .107 los puso al
        //     tamaño del libro (ESCALA 0,75 = 42×41 px); ahora ESCALA
        //     0,375 = 21×20 px en pantalla — LA MITAD EXACTA. Todo el
        //     layout (hitbox, iris, mirada, sello, aliento) escala con
        //     ESTA constante ===
        private const float ESCALA = 0.375f;

        // === v6.50.114 — EL RETROCESO DEL DISPARO (la letra: «sobre la
        //     animación de ataque de los minion haz que al atacar tenga
        //     un pequeño retroceso»): la patada de la .109 moría en 2-3
        //     ticks (la ley del planeo la comía viva). AHORA el disparo
        //     deja un ESTADO de retroceso (14 t) con su dirección: los
        //     primeros 8 t SOLO fricción suave (el tirón SE VE — el ojo
        //     rebota hacia atrás), luego la formación lo reincorpora;
        //     el dibujo lo lee para el SQUASH (se estira en el eje del
        //     tirón y se desplaza hacia atrás) ===
        private float _retroceso;
        private Vector2 _dirRetroceso;

        // === LA CADENCIA INDIVIDUAL (la .107: «cada ojo debe ser
        //     individual y deben tener la libertad de atacar cuando
        //     quieran y a la velocidad que deseen; la velocidad de ataque
        //     aumenta en cuanto más nivel tenga el grimorio, hasta un
        //     máximo de 50% más rápido a nivel 100, subiendo a partir de
        //     nivel 1»): BASE 110 ticks (1,83 s — la del Forbidden Ruin)
        //     dividida por la VELOCIDAD DE NIVEL y por el RITMO PROPIO ===
        private const int CADENCIA_BASE = 110;

        // === EL IRIS ACOMODADO (la medición del arte del usuario) ===
        //     la almendra (136,197)-(389,346) de 512×500 → centro
        //     (0.513, 0.543)·(56,54) = (28.7, 29.3) → +0.7,+2.3 del centro
        private static readonly Vector2 IRIS_REST = new(0.7f, 2.3f);
        private const float IRIS_ESCALA = 0.41f;   // caja 13 px → disco ~11 px
        private const float MIRA_X = 7.5f;         // el viaje: media almendra 13,85
        private const float MIRA_Y = 2.5f;         // el viaje: media altura 8,05

        // === EL CÍRCULO MÁGICO (el de los agujeros negros, en pequeño) ===
        private static readonly Color SelloOro = new(240, 124, 65);      // dorado-ámbar (MEDIDO del Umbral)
        private static readonly Color SelloPunta = new(255, 205, 140);   // la punta pálida

        // ================================================================
        //  v6.50.115 — LAS MEJORAS DEL ESCUADRÓN (la letra: «cuando
        //  llegue a 11 minions del grimorio en la siguiente invocación
        //  no invoca minion, lo que hace es fortalecer a uno que ya
        //  existe con un 50% de daño y una mejora visual en su
        //  proyectil… la máxima mejora sería cuando un minion gana 2
        //  anillos rúnicos de agujero negro cada uno con un color
        //  diferente y además 2 anillos rúnicos de sol a su alrededor…
        //  por lo tanto 4 mejoras, cada mejora aumenta el ataque del
        //  minion en un 50%"). Viven en la FRACCIÓN de ai[2] (entero =
        //  nivel, centésimas = mejoras: 5.03 = nivel 5 con 3 mejoras)
        //  y viajan solas con el netUpdate del proyectil.
        // ================================================================

        /// <summary>El tope de mejoras por ojo: 2 anillos rúnicos de
        /// agujero negro (colores distintos) + 2 anillos rúnicos de sol.</summary>
        public const int MejorasMax = 4;

        /// <summary>Las mejoras de UN ojo (0..MejorasMax — la centésima de ai[2]).</summary>
        public static int MejorasDe(Projectile p)
        {
            if (p == null || !p.active) return 0;
            float frac = p.ai[2] - MathF.Floor(p.ai[2]);
            int m = (int)Math.Round(frac * 100f);
            if (m < 0) m = 0;
            if (m > MejorasMax) m = MejorasMax;
            return m;
        }

        /// <summary>El multiplicador de daño de las mejoras (+50% por
        /// mejora — la letra: «cada mejora aumenta el ataque del minion
        /// en un 50%»: con las 4, el triple del rayo de siempre).</summary>
        public static float MultMejoras(Projectile p) => 1f + 0.5f * MejorasDe(p);

        // ================================================================
        //  v6.51.9 — LA AUDITORÍA DEL ×1,25 DEL DESPERTAR 50 (la letra:
        //  «asegurate de que todo los aumento de daño y estadisticas del
        //  grimorio se cumpla realmente y todas sumen y cuente para todos
        //  los calculos»): el bonus del nivel 50 («TODO el ataque del
        //  escuadrón ×1,25») se HORNEA en el daño del ojo AL INVOCARLO
        //  (DanoOjosDelEterno)… y las consumidoras (el rayo de aquí
        //  abajo, los ataques combinados) lo VOLVÍAN A multiplicar:
        //  contaba DOS veces. AHORA se marca al nacer si ya lo lleva
        //  dentro, y las consumidoras solo lo cobran EN VIVO cuando el
        //  ojo nació ANTES de que el libro llegara al 50 — UNA sola vez,
        //  en todas partes.
        // ================================================================

        /// <summary>¿EL DAÑO DE ESTE OJO nació ya con el ×1,25 del
        /// DESPERTAR 50 horneado dentro? (lo marca la invocación —
        /// GrimoireEternal.Shoot — justo tras DanoOjosDelEterno).</summary>
        public bool Bonus50Horneado = false;

        /// <summary>EL ×1,25 DEL DESPERTAR 50 QUE FALTA: 1,25 si ESTE ojo
        /// no trae el bonus horneado y el libro dueño YA está en el 50 —
        /// 1,0 en cualquier otro caso (lo lleva dentro o todavía no
        /// corresponde). LAS CONSUMIDORAS DEL DAÑO DEL OJO (el rayo, el
        /// sol, los agujeros, el supremo) lo multiplican por AQUÍ — nunca
        /// dos veces.</summary>
        public static float Bonus50Vivo(Projectile p)
        {
            try
            {
                if (p == null || !p.active) return 1f;
                if (!(p.ModProjectile is OjoEternoMinion ojo)) return 1f;
                if (ojo.Bonus50Horneado) return 1f;   // ya lo lleva dentro
                Player dueño = Main.player[p.owner];
                if (dueño == null || !dueño.active) return 1f;
                return Content.Systems.DespertarGrimorio.NivelDe(dueño) >=
                    Content.Systems.DespertarGrimorio.NivelMax ? 1.25f : 1f;
            }
            catch { return 1f; }
        }

        /// <summary>
        /// +1 MEJORA a un ojo vivo (la llama la compuerta del Eterno
        /// cuando el escuadrón de 10 ya está LLENO): +50% de daño y el
        /// ANILLO NUEVO de la mejora (1: agujero negro DORADO · 2:
        /// agujero negro VIOLETA · 3: SOL dorado · 4: SOL azul-estelar).
        /// </summary>
        public static void Fortalecer(Projectile p)
        {
            if (p == null || !p.active) return;
            int nivel = (int)MathF.Floor(p.ai[2]);
            int m = MejorasDe(p);
            if (m >= MejorasMax) return;
            // v6.51.0 — LAS MEJORAS SON DEL DESPERTAR (la letra: «con la
            // nueva mecánica las mejoras están sujetas a esa mecánica»):
            // nivel 5 → la 1 · 15 → la 2 · 30 → la 3 · 40 → la 4.
            Player dueño = Main.player[p.owner];
            if (m >= Content.Systems.DespertarGrimorio.MejorasDesbloqueadas(
                    Content.Systems.DespertarGrimorio.NivelDe(dueño)))
                return;
            m++;
            p.ai[2] = nivel + m * 0.01f;   // sin deriva: se REESCRIBE entero
            p.netUpdate = true;

            // EL ESTALLIDO del anillo nuevo (chispas doradas y violetas)
            if (Main.netMode != NetmodeID.Server)
            {
                for (int k = 0; k < 20; k++)
                {
                    Dust d = Dust.NewDustPerfect(p.Center,
                        k % 2 == 0 ? DustID.GoldFlame : DustID.Shadowflame,
                        Main.rand.NextVector2Circular(4.5f, 4.5f), 128, default, 1.3f);
                    d.noGravity = true;
                }
            }
        }

        // ================================================================
        //  EL NIVEL (el patrón del Orbe Cósmico: ai[2] desde el spawn)
        // ================================================================
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        private int Level
        {
            get
            {
                int nivel = (int)Projectile.ai[2];
                if (nivel < 1)
                {
                    Player dueño = Main.player[Projectile.owner];
                    if (dueño != null && dueño.active)
                    {
                        Item held = dueño.HeldItem;
                        if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                        {
                            try
                            {
                                var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                                if (sl != null) nivel = sl.Level;
                            }
                            catch { }
                        }
                    }
                    if (nivel < 1) nivel = 1;
                }
                return nivel;
            }
        }

        /// <summary>LA VELOCIDAD DE NIVEL (la .107): 1,0× en nivel 1
        /// subiendo LINEAL hasta 1,5× (un 50% más rápido) en nivel 100 —
        /// y CLAVADA ahí (el tope de la letra).</summary>
        private float VelocidadNivel =>
            1f + 0.5f * Math.Min(Level - 1, 99) / 99f;

        /// <summary>EL RITMO PROPIO: cada ojo hereda una velocidad
        /// personal (0,85–1,15) de su whoAmI — la banda JAMÁS dispara en
        /// bloque: cada uno ataca a la velocidad que LE da la gana.</summary>
        private float RitmoPropio =>
            0.85f + 0.30f * VFXCore.Hash01(Projectile.whoAmI, 577, 131);

        /// <summary>LA CADENCIA DE ESTE OJO: 110 / (nivel × ritmo) — el
        /// intervalo en ticks hasta SU próximo disparo.</summary>
        private int CadenciaPersonal =>
            Math.Max(20, (int)(CADENCIA_BASE / (VelocidadNivel * RitmoPropio)));

        // ================================================================
        //  LA IA
        // ================================================================
        public override void AI()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active || dueño.dead)
            {
                Projectile.Kill();
                return;
            }

            CheckMinionBuff(dueño);

            int nivel = Level;
            float modo = Projectile.ai[0];
            float t = Projectile.ai[1];

            // === v6.50.107 — EL DESFASE DE NACIMIENTO: el primer disparo
            //     de cada ojo cae entre el 35% y el 100% de SU intervalo
            //     — la banda invocada de golpe JAMÁS dispara en bloque ===
            if (Projectile.ai[1] <= 0f && Projectile.frame <= 0)
                Projectile.frame = (int)(CadenciaPersonal *
                    (0.35f + 0.65f * VFXCore.Hash01(Projectile.whoAmI, 811, 23)));

            // === EL PARPADEO INDEPENDIENTE (visual-local: NO lee el hambre
            //     del libro — cada ojo lleva SU reloj). En la carga, el
            //     párpado se CORTA y el ojo queda ABIERTO clavado en la
            //     presa (la mirada del cazador — fase 0, no «medio»). ===
            if (modo == MODO_APUNTANDO)
            {
                if (Projectile.localAI[1] > 0f) Projectile.localAI[1] = 0f;
            }
            else
            {
                if (Projectile.localAI[1] > 0f)
                    Projectile.localAI[1]--;                      // la fase 10→1
                else
                {
                    Projectile.localAI[0]--;
                    if (Projectile.localAI[0] <= 0f)
                    {   // el próximo parpadeo: 6–18 s, propio de ESTE ojo (hash, cero RNG)
                        Projectile.localAI[1] = 10f;
                        Projectile.localAI[0] = 360f + 720f *
                            VFXCore.Hash01(Projectile.whoAmI, 911, 77);
                    }
                }
            }

            // === EL FOCO DE LA MIRADA: a dónde apunta el iris ===
            NPC presa = ObjetivoDelOjo(dueño, nivel);
            // v6.59 — EL RITUAL DE LA IRA: mientras la ceremonia vive, el
            //     iris del escuadrón se CLAVA en el centro del círculo (la
            //     mirada colectiva alimenta el conjuro — y sus ataques
            //     normales ESPERAN: están rezando, no cazando).
            Projectile ritualIra = global::AethonMod.Content.Projectiles.Definitivas
                .IraSerExteriorCeremoniaProjectile.RitualDe(Projectile.owner);
            // v6.51.7 — EL ARCO DEL BÁCULO SANGUINARIO (la letra: «los
            //     minions siempre deben ir detras del jugador, pero
            //     cuando el libro sale a cazar, los minions comienza a
            //     orbitar al grimorio eterno… usa la forma en que se
            //     posicionan los murcielagos de El báculo sanguinario
            //     (Sanguine Staff) para posicionar a los minions del
            //     grimorio en el grimorio del eterno cuando sale a
            //     cazar»): el CENTRO de la formación es el ESPÍRITU
            //     mientras está fuera (los ojos orbitan AL LIBRO que
            //     caza) y EL JUGADOR mientras está en casa (el abanico
            //     quieto detrás del dueño — como los murciélagos del
            //     Sanguine). El libro EN LA MANO ya NO es centro: la
            //     .51.6 ataba la danza al vaivén del brazo y la órbita
            //     se leía rara.
            Vector2? libro = PosicionDelLibro(dueño);
            Vector2 slot = SlotDeFormacion(dueño, libro);
            Vector2 foco = modo == MODO_APUNTANDO && presa != null
                ? presa.Center
                : (ritualIra != null ? ritualIra.Center : slot);

            // el ángulo de la mirada barre suave (0,22 rad/t) hacia el foco —
            // el iris LLEGA mucho antes que el cuerpo: «primero mira»
            float angMeta = foco == Projectile.Center
                ? Projectile.localAI[2]
                : (float)Math.Atan2(foco.Y - Projectile.Center.Y, foco.X - Projectile.Center.X);
            if (Projectile.localAI[2] == 0f) Projectile.localAI[2] = angMeta;
            float da = MathHelper.WrapAngle(angMeta - Projectile.localAI[2]);
            Projectile.localAI[2] = MathHelper.WrapAngle(
                Projectile.localAI[2] + MathHelper.Clamp(da, -0.22f, 0.22f));

            // === LA MÁQUINA (dos modos — la carga y el crucero) ===
            if (modo == MODO_APUNTANDO)
            {
                // === v6.50.109 — EL PLANEO DEL RUINOUS EN LA CARGA (la
                //     wiki del Forbidden Ruin: «continuously adjusts its
                //     position to stay along the line segment between
                //     the player and its target»): MUERE el freno en seco
                //     (velocity *= 0,75 = el ojo se congelaba); el ojo
                //     AHORA SE REPOSICIONA deslizándose hacia la línea
                //     ancla↔presa — el movimiento NUNCA muere, la carga
                //     se lee como una LEVANTADA hacia la presa ===
                if (presa == null || !presa.active || presa.life <= 0)
                {
                    SalirDeFuego();                            // la presa murió esperando
                }
                else
                {
                    Vector2 refPt = libro is Vector2 centroLibro
                        ? centroLibro : dueño.MountedCenter;
                    Vector2 aLaPresa = presa.Center - refPt;
                    float dP = aLaPresa.Length();
                    if (dP > 0.01f)
                    {
                        // el punto de tiro: sobre la línea ancla↔presa,
                        // acercándose sin abandonar el vecindario del libro
                        Vector2 ancla = refPt + Vector2.Normalize(aLaPresa)
                            * Math.Min(dP * 0.35f, 220f);
                        Vector2 deseada = (ancla - Projectile.Center) * 0.12f;
                        float vmax = 6f * WeaponScaling.MinionSpeedMult(nivel);
                        if (deseada.Length() > vmax)
                            deseada = Vector2.Normalize(deseada) * vmax;
                        Projectile.velocity = Vector2.Lerp(
                            Projectile.velocity, deseada, 0.18f);
                    }

                    if (t >= AIM_TICKS)
                    {
                        Disparar(presa, nivel);
                        SalirDeFuego();
                    }
                }
            }
            else
            {
                // === v6.50.114 — EL TIRÓN VIVE: mientras dura el retroceso
                //     reciente (los primeros 8 t tras el disparo) la ley
                //     del planeo NO toca la velocidad — solo fricción
                //     suave: el rebote hacia atrás SE LEE completo; los
                //     últimos 6 t la formación lo reincorpora (el glide
                //     de siempre) ===
                bool enTirón = _retroceso > 0f && _retroceso > 6f;
                if (_retroceso > 0f) _retroceso--;

                if (enTirón)
                {
                    Projectile.velocity *= 0.93f;
                }
                else
                {
                // === LA LEY DEL PLANEO (el Ruinous Staff): UNA ley para
                //     todo el viaje — sin modos, sin frenos en seco, sin
                //     rebote. Lejos: inercia con arranque suave (la
                //     velocidad deseada se capped por distancia — jamás
                //     se dispara de golpe). Cerca: amortiguación crítica
                //     (la velocidad apunta al hueco proporcional a lo
                //     que falta: el ojo SIGUE al hueco móvil sin
                //     sobrepasarlo nunca). ===
                Vector2 alSlot = slot - Projectile.Center;
                float dist = alSlot.Length();
                // === v6.51.3 — LA LLEGADA EXACTA DE LA DANZA (la letra:
                //     «mas exacta»): cerca del puesto, el ojo lo PERSIGUE
                //     proporcional puro — SIN inercia, SIN sobrepaso (el
                //     controlador viejo conservaba velocidad y CRUZABA el
                //     hueco: los ojos se pisaban y la danza se leía
                //     desordenada): converge en subpíxel al hueco MÓVIL —
                //     cada ojo clavado a su órbita, la danza PERFECTA.
                //     v6.51.7: vale para el abanico del JUGADOR también
                //     (ya no solo junto al libro) — el arco del Sanguine
                //     se lee clavado en los DOS centros. ===
                if (dist <= 30f && dist > 0.01f)
                {
                    Projectile.velocity = alSlot * 0.55f;
                }
                else if (dist > 90f)
                {
                    float vmax = 13f * WeaponScaling.MinionSpeedMult(nivel);
                    Vector2 deseada = Vector2.Normalize(alSlot) *
                        Math.Min(vmax, dist * 0.22f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.15f);
                }
                else if (dist > 0.1f)
                {
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity, alSlot * 0.25f, 0.35f);
                }
                else
                {
                    Projectile.velocity *= 0.6f;
                }
                }

                // === EL DISPARO: si hay presa y la cadencia está lista ===
                // v6.59 — SALVO EN EL RITUAL DE LA IRA (la letra: los ojos
                //     están GIRANDO y rezando sus rayos al centro del
                //     círculo — no interrumpen la ceremonia con su caza).
                if (ritualIra == null && presa != null && Projectile.frame <= 0)
                {
                    Projectile.ai[0] = MODO_APUNTANDO;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.ai[1]++;
            if (Projectile.frame > 0) Projectile.frame--;         // la cadencia (frame prestado)

            // === LA PRESENCIA: el halo suave del ojo ===
            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.34f, 0.18f));
        }

        // ================================================================
        //  EL DISPARO — el proyectil DEL CETRO DEL TRUENO PERLIN
        //  (el mismo TruenoPerlinProjectile, modo ojo: ai[0]=1)
        // ================================================================
        private void Disparar(NPC presa, int nivel)
        {
            // === v6.50.114 — EL RETROCESO (la letra: «un pequeño
            //     retroceso»): patada atrás MÁS FUERTE (5,6) + estado de
            //     retroceso que la ley del planeo RESPETA 8 ticks (antes
            //     la patada de 3,2 se evaporaba en 2-3 t y no se veía) ===
            Vector2 atrás = Projectile.Center - presa.Center;
            if (atrás.LengthSquared() > 0.01f)
            {
                atrás.Normalize();
                _dirRetroceso = atrás;
                _retroceso = 14f;
                Projectile.velocity = atrás * 5.6f;
            }

            // === EL GOLPE: el cauce del cetro (radio + cadena + GolpeMotor
            //     — resuelto por su dueño). El daño del minion escalado
            //     por nivel, como el contacto de siempre — Y por SUS
            //     MEJORAS (v6.50.115: +50% por mejora del escuadrón
            //     lleno, hasta ×3 con las cuatro). ===
            if (Projectile.owner == Main.myPlayer)
            {
                // v6.51.0 — EL NIVEL 50 DEL DESPERTAR (la letra: «mejora
                // TODO el ataque de los minions y sus proyectiles así
                // como la cadencia de disparo»): el rayo pega ×1,25 —
                // v6.51.9 — PERO SOLO SI NO LO LLEVA HORNEADO (la
                // auditoría: antes se multiplicaba DOS veces en los ojos
                // invocados ya en el 50).
                float bonusFinal = Bonus50Vivo(Projectile);
                int dano = Math.Max(1, (int)(Projectile.damage *
                    WeaponScaling.MinionContactDamageMult(nivel) * MultMejoras(Projectile) * bonusFinal));
                try
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        presa.Center, Vector2.Zero,
                        ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.TruenoPerlinProjectile>(),
                        dano, Projectile.knockBack * 0.6f, Projectile.owner,
                        1f, Projectile.whoAmI, presa.whoAmI);
                }
                catch { }
            }

            Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.5f),
                Projectile.Center);
        }

        /// <summary>El fin del fuego: vuelve al crucero y arranca SU
        /// cadencia (la de ESTE ojo — nivel × ritmo propio).</summary>
        private void SalirDeFuego()
        {
            Projectile.ai[0] = MODO_CRUCERO;
            Projectile.ai[1] = 0f;
            Projectile.frame = CadenciaPersonal;
            Projectile.netUpdate = true;
        }

        // ================================================================
        //  EL DIBUJO — el contrato del lote (VFXCore.cs:1012): se entra
        //  cerrando, se sale SIEMPRE con el lote ABIERTO y vanilla.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawElSello();
                DrawElOjo();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ================================================================
        //  LA FORMACIÓN — el arco del báculo sanguinario (v6.51.7)
        // ================================================================

        /// <summary>LA FORMACIÓN — v6.51.7 — EL ARCO DEL BÁCULO
        /// SANGUINARIO (la letra: «los minions siempre deben ir detras
        /// del jugador, pero cuando el libro sale a cazar, los minions
        /// comienza a orbitar al grimorio eterno… en el juego terraria
        /// ya hay un arma que usa orbitas o similares, por ejemplo usa
        /// la forma en que se posicionan los murcielagos de El báculo
        /// sanguinario (Sanguine Staff, en inglés) para posicionar a
        /// los minions del grimorio en el grimorio del eterno cuando
        /// sale a cazar»).
        ///
        /// LA GEOMETRÍA ES LA DE VANILLA (AI_156_GetIdlePosition, el
        /// murciélago 755 del Sanguine Staff — extraída del decompile):
        /// UN ABANICO de ±126° centrado ARRIBA del centro, radio 40,
        /// con el puesto de cada ojo repartido PAR — ángulo =
        /// (7π/5)/n·(i − (n−1)/2), la constante EXACTA de vanilla
        /// (4,39823 rad). MUEREN la corona doble de la .117 (arcos
        /// 195°-345°) y la danza atómica de la .51.3 (anillos 5, 6,
        /// 7… inclinados): UN arco, DOS centros.
        ///
        /// EL CENTRO: el ESPÍRITU del Eterno mientras está fuera
        /// cazando (los ojos ORBITAN al grimorio — el abanico entero
        /// gira lento, ω = 0,30 rad/s ≈ 21 s por vuelta, reloj
        /// GlobalTime: determinista en todos los clientes) y EL JUGADOR
        /// mientras el libro está en casa (el abanico QUIETO detrás del
        /// dueño — como los murciélagos del Sanguine sobre su amo: «los
        /// minions siempre van detrás del jugador»).
        ///
        /// EL RADIO: 40 de vanilla, creciendo con la banda (6,2·n — la
        /// regla de la .51.3: NINGÚN ojo pisa a su vecino: a 10 ojos,
        /// 62 px de radio y ~27 px de paso, que con el cabeceo de ±1,5
        /// deja SIEMPRE aire entre sprites de 21 px) y la RESPIRACIÓN
        /// ÚNICA de la danza (un solo latido para TODOS — la sincronía
        /// de la .51.3 vive).
        ///
        /// v6.51.8 — EL CÍRCULO NO TIENE PUERTAS (la letra: «la orbita
        /// alrededor del grimorio como esta girando queda siempre con un
        /// agujero, asegurate de que los minion respeten la orbita pero
        /// rodeen a todo el grimorio sin dejar agujeros»): el abanico
        /// del Sanguine (252° de arco) dejaba SIEMPRE un hueco de 108°
        /// girando alrededor del libro — el agujero de la letra. Mientras
        /// el espíritu CAZA, el reparto ahora es el CÍRCULO COMPLETO
        /// (2π/n por ojo — rodean TODO el grimorio, sin puertas); el
        /// abanico EXACTO del murciélago 755 queda para la CASA (quieto
        /// detrás del dueño, como los murciélagos del Sanguine sobre su
        /// amo — esa parte de la letra .51.7 SIGUE intacta). Con el
        /// círculo, la cuerda entre vecinos es MÁS holgada que el abanico
        /// (2π·r/n: a 11 ojos, 61 px de paso — nadie se pisa).</summary>
        private Vector2 SlotDeFormacion(Player dueño, Vector2? libro)
        {
            int idx = Math.Max(0, (int)Projectile.minionPos);
            float time = Main.GlobalTimeWrappedHourly;

            // === v6.59 — EL RITUAL DE LA IRA DE UN SER EXTERIOR (la letra:
            //     «independientemente de donde esten los minions ya sean
            //     sobre el jugador o sobre el grimorio, estos deben
            //     comenzar a girar en circulos, o sea el circulo que forman
            //     debe cerrarse en caso de que este en el jugador al inicio
            //     de la animacion»): mientras la ceremonia vive, el puesto
            //     es LA CORONA COMPLETA alrededor de SU centro — el círculo
            //     SE CIERRA (2π/n: nada de abanico trasero) y GIRA, con la
            //     VUELTA DEL RITUAL que ACELERA (arranca como la órbita del
            //     libro, ω 0,30, y sube a 1,05 rad/s cuando el círculo ya
            //     cerró — el giro que se lee). El centro de la ceremonia
            //     YA nace elevado sobre el jugador (los ojos SE ELEVAN al
            //     orbitarlo) y ES el libro cuando el libro está fuera (no
            //     hace falta elevarse: ya giraban ahí).
            Projectile ritual = global::AethonMod.Content.Projectiles.Definitivas
                .IraSerExteriorCeremoniaProjectile.RitualDe(Projectile.owner);
            if (ritual != null)
            {
                // v6.62 — EL PUESTO DEL RITUAL, COMPARTIDO: la matemática
                //     de la corona vive AHORA en SlotDelRitual (UN solo
                //     lugar de la verdad) — la leen los ojos (SU puesto en
                //     el círculo) y los arcos del escuadrón (los rayitos
                //     solo cuentan ojos PUESTOS en la corona). La conducta
                //     es BYTE-IDÉNTICA a la rama que vivía aquí.
                Vector2? slotR = SlotDelRitual(Projectile, ritual, dueño);
                if (slotR is Vector2 sr) return sr;
                // (caída imposible con ritual y dueño vivos — sigue abajo
                // al puesto en persona: la guarda de siempre)
            }

            // EL CENTRO: el grimorio que caza si está fuera; el dueño si no
            Vector2 centro = libro is Vector2 c ? c : dueño.MountedCenter;

            int n = Math.Max(1, dueño.ownedProjectileCounts[Projectile.type]);
            int i = Math.Min(idx, n - 1);

            float ang;
            if (libro is Vector2)
            {
                // === v6.51.8 — LA CORONA COMPLETA: cada ojo a SU sector
                //     del círculo (2π/n) y la corona entera GIRANDO
                //     alrededor del grimorio (la revolución lenta de la
                //     .51.7, ω 0,30 rad/s — el reloj GlobalTime:
                //     determinista en MP sin netcode). Rodean TODO el
                //     libro — SIN AGUJEROS. ===
                ang = MathHelper.TwoPi / n * i + time * 0.30f;
            }
            else
            {
                // === EL PUESTO EN CASA: el reparto EXACTO del murciélago
                //     755 — 7π/5 es la constante de vanilla (4,39823: el
                //     abanico de ±126°), QUIETO sobre el dueño (la letra:
                //     «los minions siempre deben ir detras del jugador» —
                //     como los murciélagos del Sanguine sobre su amo). ===
                ang = 4.3982296f / n * (i - (n - 1) / 2f);
            }

            // EL RADIO: 40 de vanilla + el crecimiento de la banda (que
            // NADIE se pise, ni siquiera con el cabeceo) + la
            // respiración única de la danza
            float radio = Math.Max(40f, 6.2f * n) *
                (1f + 0.035f * MathF.Sin(time * 0.5f));

            // EL PUESTO EN PERSONA + el cabeceo mínimo (la vida propia)
            Vector2 slot = centro + (-Vector2.UnitY).RotatedBy(ang) * radio;
            slot.Y += MathF.Sin(time * 0.9f + i * 1.9f) * 1.5f;
            return slot;
        }

        /// <summary>¿DÓNDE ESTÁ EL LIBRO? (el espíritu fuera — el patrón
        /// del Nervioso, cacheado por frame para la banda entera).
        /// v6.51.7 — es EL CENTRO del abanico mientras está fuera: los
        /// ojos orbitan AL LIBRO que caza; en casa (null) orbitan al
        /// JUGADOR (el arco quieto detrás del dueño — el libro en la
        /// mano ya NO es centro).</summary>
        private static int _frameLibro = -1;
        private static int _ownerLibro = -1;
        private static Vector2? _posLibro = null;
        internal static Vector2? PosicionDelLibro(Player dueño)
        {
            if (_frameLibro == (int)Main.GameUpdateCount && _ownerLibro == dueño.whoAmI)
                return _posLibro;
            _frameLibro = (int)Main.GameUpdateCount;
            _ownerLibro = dueño.whoAmI;
            _posLibro = Weapons.GrimorioHambrientoNervioso.PosicionDelLibro(dueño.whoAmI);
            return _posLibro;
        }

        // ================================================================
        //  v6.62 — EL PUESTO DEL RITUAL, COMPARTIDO: la matemática de la
        //  corona del ritual (la rama de SlotDeFormacion), viva en UN
        //  solo lugar de la verdad — la leen los ojos (SU puesto en el
        //  círculo, la conducta de siempre, BYTE-IDÉNTICA) y los arcos
        //  del escuadrón (IraSerExteriorCeremonia.DibujarArcosDel
        //  Escuadron: los rayitos solo cuentan los ojos PUESTOS en la
        //  corona — a ≤28 px de SU puesto). Null = no hay ritual.
        // ================================================================
        internal static Vector2? SlotDelRitual(Projectile ojo, Projectile ancla, Player dueño)
        {
            if (ojo == null || !ojo.active || ancla == null || !ancla.active ||
                dueño == null || !dueño.active) return null;

            int idx = Math.Max(0, (int)ojo.minionPos);
            int nR = Math.Max(1, dueño.ownedProjectileCounts[ojo.type]);
            int iR = Math.Min(idx, nR - 1);
            float time = Main.GlobalTimeWrappedHourly;

            // LA VUELTA DEL RITUAL: ω 0,30 → 1,05 conforme el círculo
            // cierra (los primeros 150 ticks — la ceremonia).
            // v6.60.2 — Y SIGUE EN EL DISPARO: muerta la ceremonia, el
            // ANCLA es EL HAZ (RitualDe cae a él) — el círculo YA cerró:
            // la vuelta se queda A TOPE (ω 1,05) TODO el lanzamiento.
            // (ai[2] del haz es el dueño, NO la edad: por eso la rama —
            // la ceremonia acelera, el haz YA llegó.)
            float omega = 1.05f;
            if (ancla.ModProjectile is global::AethonMod.Content.Projectiles
                    .Definitivas.IraSerExteriorCeremoniaProjectile)
                omega = MathHelper.Lerp(0.30f, 1.05f, MathHelper.Clamp(
                    ancla.ai[2] / 150f, 0f, 1f));

            float angR = MathHelper.TwoPi / nR * iR + time * omega;
            float radioR = Math.Max(40f, 6.2f * nR)
                * (1f + 0.035f * MathF.Sin(time * 0.5f));
            Vector2 slot = ancla.Center + (-Vector2.UnitY).RotatedBy(angR) * radioR;
            slot.Y += MathF.Sin(time * 0.9f + iR * 1.9f) * 1.5f;
            return slot;
        }

        /// <summary>LA PRESA: la más cercana válida en rango (el filtro del
        /// Orbe Cósmico, heredado) — la misma lista en todos los clientes.</summary>
        private NPC ObjetivoDelOjo(Player dueño, int nivel)
        {
            float rango = WeaponScaling.MinionDetectionRange(nivel);
            NPC mejor = null;
            float mejorD = rango;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc == null || !npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue;
                if (npc.catchItem > 0) continue;
                if (npc.immortal) continue;
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = npc; }
            }
            return mejor;
        }

        private void CheckMinionBuff(Player dueño)
        {
            // v6.51.12 — LA CURA DE LA DESINVOCACIÓN SILENCIOSA (la letra:
            // «a veces los minions se desinvocan, o sea que estan
            // invocados y de pronto dejan de estarlo»): el ojo JAMÁS
            // muere por falta de buff — la barra de buffs llena de
            // vanilla REEMPLAZABA el icono (nuestro 18000 era el más
            // corto) y TODO el escuadrón moría en un tick, «de pronto».
            // AHORA: EscuadronUtil.Sostener (el reloj nunca expira, el
            // icono se re-ancla si hay hueco) — la única despedida es
            // el CLIC DERECHO del icono (OjoEternoBuff.RightClick →
            // EscuadronUtil.Despedir — «solo se deben desactivar si el
            // jugador los quita usando su buff de minion»).
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>();
            global::AethonMod.Content.Systems.EscuadronUtil.Sostener(Projectile, dueño, buffType);
        }

        /// <summary>EL CÍRCULO MÁGICO — el sello erosionado de los agujeros
        /// negros (OrbitaLib.SigiloErosionado, las runas de RunasAbismo),
        /// en pequeño alrededor del ojo. v6.50.109: TODO escala con ESCALA
        /// (el ojo es la mitad — el sello también: r = ESCALA·16 ≈ 6 px,
        /// y en la carga del disparo RESPIRA hacia afuera r → ESCALA·20
        /// ≈ 7,5 — antes 12→15 del ojo grande de la .107).</summary>
        private void DrawElSello()
        {
            // v6.50.114 — EL RETROCESO SE LEE: el sello se desplaza con el
            // rebote del disparo (la dirección de la patada) y encoge con él
            float squash = Math.Min(_retroceso / 14f, 1f);
            Vector2 pos = Projectile.Center - Main.screenPosition
                + _dirRetroceso * (6.5f * squash);
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Projectile.whoAmI * 13 + 7;

            // la carga: el sello inhala (ESCALA·16 → ESCALA·20)
            float r = ESCALA * 16f * (1f - 0.15f * squash);
            if (Projectile.ai[0] == MODO_APUNTANDO)
                r = MathHelper.Lerp(ESCALA * 16f, ESCALA * 20f,
                    Math.Min(Projectile.ai[1] / (float)AIM_TICKS, 1f));

            float glyph = Math.Max(r / 52f, 0.25f) * 1.75f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                // v6.51.7 — LA ESTELA DE LA ÓRBITA (la letra de la .114:
                // «mejora la animación… alrededor del grimorio»):
                // tres fantasmas de ESTE ojo ATRÁS en la REVOLUCIÓN
                // del abanico — SOLO mientras el espíritu está fuera
                // (el arco en casa está quieto sobre el dueño: no hay
                // giro que leer), rotados ATRÁS según el ω del abanico
                // (0,30 rad/s — el reloj GlobalTime: determinista)
                Player dueñoEstela = Main.player[Projectile.owner];
                if (dueñoEstela != null && dueñoEstela.active &&
                    PosicionDelLibro(dueñoEstela) is Vector2 centroEstela)
                {
                    Vector2 slotAhora = SlotDeFormacion(dueñoEstela, centroEstela);
                    Vector2 off = slotAhora - centroEstela;
                    float omega = 0.30f;                        // rad/s del abanico
                    for (int j = 1; j <= 3; j++)
                    {
                        float dt = j * 0.25f;                   // ~0,25 s de revolución por fantasma
                        Vector2 pasado = centroEstela + off.RotatedBy(-omega * dt);
                        float s = ESCALA * (40f - j * 9f);      // encoge: 15→12→9 px
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, pasado - Main.screenPosition, null,
                            new Color(240, 124, 65, 255) * (0.22f / j), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(s / 64f, s / 64f),
                            SpriteEffects.None, 0f);
                    }
                }

                OrbitaLib.SigiloErosionado(pos, r, time, seed,
                    2.55f, 8, 0.06f, glyph, SelloOro, SelloPunta,
                    offset: 0, hashOff: 0, spikeAngle: -0.55f, spikeWindow: 0.45f);

                // === v6.50.115 — LOS ANILLOS DE LAS MEJORAS (la letra:
                //     «cada minion con máxima mejora tendría su anillo
                //     rúnico de agujero negro con el que aparece al inicio
                //     más 2 anillos rúnicos de agujero negro extra más 2
                //     anillos rúnicos de sol») — el sello de arriba ES el
                //     anillo con el que NACE; las mejoras añaden los
                //     suyos, CADA UNO DE UN COLOR (los de agujero negro
                //     aquí en el lote aditivo; los de sol, debajo, en el
                //     lote VFX de sus elipses inclinadas) ===
                int mejoras = MejorasDe(Projectile);
                if (mejoras >= 1)
                {
                    // MEJORA 1 — EL ANILLO RÚNICO DE AGUJERO NEGRO DORADO
                    //     (8 runas de pie girando CW — el círculo dorado
                    //     de la triple corona, a escala de ojo)
                    float g1 = Math.Max(15f / 52f, 0.25f) * 1.15f;
                    OrbitaLib.CirculoRunico(pos, 15f, time, 1f, 8, 0.10f,
                        OrbitaLib.RunaDorada, OrbitaLib.RunaDoradaTip, g1,
                        null, 0, 1, 0.95f, 0.30f);
                }
                if (mejoras >= 2)
                {
                    // MEJORA 2 — EL ANILLO RÚNICO DE AGUJERO NEGRO VIOLETA
                    //     (6 runas CONTRARROTANDO — el contrarroto arcano,
                    //     OTRO color como pide la letra)
                    float g2 = Math.Max(20f / 52f, 0.25f) * 1.15f;
                    OrbitaLib.CirculoRunico(pos, 20f, time, 1f, 6, -0.075f,
                        OrbitaLib.RunaVioleta, OrbitaLib.RunaVioletaTip, g2,
                        null, 3, 1, 0.95f, 0.26f);
                }
                if (mejoras > 0)
                {
                    // EL RESPLANDOR de las mejoras: cada anillo ganado
                    // hace al ojo BRILLAR más (la presencia crece)
                    float brilloM = (0.05f + 0.035f * MathF.Sin(time * 3.2f + Projectile.whoAmI)) * mejoras;
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                        new Color(255, 200, 110, 255) * (brilloM * 0.8f), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2((14f + 5f * mejoras) / 64f, (14f + 5f * mejoras) / 64f),
                        SpriteEffects.None, 0f);
                }

                // la presencia: el aliento violáceo DETRÁS del ojo (la
                // lectura nocturna) — a la ESCALA del ojo (la mitad)
                float pulso = 0.09f + 0.05f * MathF.Sin(time * 2.6f + Projectile.whoAmI);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    new Color(96, 48, 150, 255) * pulso, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(ESCALA * 3.33f, ESCALA * 2.8f),
                    SpriteEffects.None, 0f);
            }
            finally { Main.spriteBatch.End(); }

            // === v6.50.115 — LOS ANILLOS RÚNICOS DE SOL de las mejoras
            //     3 y 4 (la letra: «y además 2 anillos rúnicos de sol a
            //     su alrededor»): las ELIPSES INCLINADAS de los soles
            //     (SigiloLib — el mismo emisor de los anillos de los
            //     soles rúnicos), en el lote VFX de coords de mundo ===
            int mejorasSol = MejorasDe(Projectile);
            if (mejorasSol >= 3)
            {
                VFXCore.Begin();
                try
                {
                    Vector2 centroMundo = Projectile.Center;
                    // MEJORA 3 — EL ANILLO DEL SOL DORADO (la elipse de los
                    //     soles, 8 runas cabalgando la tangente)
                    SigiloLib.AnilloRunico(centroMundo, 25f, 25f * 0.34f, -0.55f,
                        time * 0.26f, 8, Math.Max(25f / 52f, 0.25f) * 1.45f,
                        time, 0, SigiloLib.CuerpoOro, SigiloLib.PuntaOro, 0.95f);
                    if (mejorasSol >= 4)
                    {
                        // MEJORA 4 — EL SEGUNDO ANILLO DEL SOL (azul-estelar,
                        //     contrainclinado y CONTRARROTANDO — el sello frío)
                        SigiloLib.AnilloRunico(centroMundo, 30f, 30f * 0.41f, 0.35f,
                            -time * 0.31f, 6, Math.Max(30f / 52f, 0.25f) * 1.45f,
                            time, 2, SigiloLib.CuerpoAzul, SigiloLib.PuntaAzul, 0.95f);
                    }
                    VFXCore.FlushAdditive();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
            }
        }

        /// <summary>EL OJO — el sprite EXACTO del usuario (56×54, sin
        /// recorte) al TAMAÑO DEL LIBRO (ESCALA 0,75 = 42×41 en pantalla):
        /// la base (abierto), el párpado (medio/cerrado, la convención de
        /// fases del libro: fase>7||fase<4 = medio) y el IRIS ACOMODADO
        /// deslizándose por la almendra (reposo en el centro medido del
        /// arte; viaje ±7,5 × ±2,5 — TODO escalado con ESCALA).</summary>
        private void DrawElOjo()
        {
            // v6.50.114 — EL RETROCESO SE LEE: el ojo REBOTA hacia atrás
            // (la dirección de la patada, ~6,5 px) con un pop de escala
            // que decae con el estado — el pequeño tirón del disparo
            float squash = Math.Min(_retroceso / 14f, 1f);
            Vector2 pos = Projectile.Center - Main.screenPosition
                + _dirRetroceso * (6.5f * squash);
            float escOjo = ESCALA * (1f + 0.16f * squash);
            Texture2D abierto = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Vector2 origen = abierto.Size() * 0.5f;
            Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                // EL PÁRPADO: las fases del libro (10→1: medio, cerrado, medio)
                float fase = Projectile.localAI[1];
                if (fase > 0f)
                {
                    bool medio = fase > 7f || fase < 4f;
                    Texture2D parpado = ModContent.Request<Texture2D>(medio
                        ? "AethonMod/Content/Projectiles/OjoEterno_Medio"
                        : "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
                    Main.spriteBatch.Draw(parpado, pos, null, color, 0f, origen, escOjo,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    Main.spriteBatch.Draw(abierto, pos, null, color, 0f, origen, escOjo,
                        SpriteEffects.None, 0f);

                    // EL IRIS: el ángulo de la mirada → el desplazamiento en la
                    // almendra (±7,5 · ±2,5 — la geometría del arte del usuario)
                    float dist = Vector2.Distance(FocoVivo(), Projectile.Center);
                    float alcance = MathHelper.Clamp(dist / 110f, 0f, 1f);
                    Vector2 mirada = new Vector2(
                        (float)Math.Cos(Projectile.localAI[2]) * MIRA_X,
                        (float)Math.Sin(Projectile.localAI[2]) * MIRA_Y) * alcance;

                    Texture2D iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    Main.spriteBatch.Draw(iris,
                        pos + (IRIS_REST + mirada) * escOjo, null, color, 0f,
                        iris.Size() * 0.5f, IRIS_ESCALA * escOjo, SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>EL FOCO VIVO (para el alcance de la mirada): el slot de
        /// formación, o la presa mientras apunta.</summary>
        private Vector2 FocoVivo()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active) return Projectile.Center;
            NPC presa = ObjetivoDelOjo(dueño, Level);
            if (Projectile.ai[0] == MODO_APUNTANDO && presa != null)
                return presa.Center;
            return SlotDeFormacion(dueño, PosicionDelLibro(dueño));
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;    // el parpadeo no usa frames: son 3 texturas por capas
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            // v6.50.109 — el ojo a la MITAD (ESCALA 0,375 = 21×20 en
            // pantalla); la hitbox lógica (la almendra) va adentro — el
            // rayo es el ataque, no el contacto
            Projectile.width = 12;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.aiStyle = -1;
            Projectile.light = 0.3f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;   // el RAYO es el ataque: el ojo no muerde
    }
}
