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
    /// OJO ETERNO MINION — v6.50.107 — EL TAMAÑO DEL LIBRO, LA LUNA Y EL
    /// RITMO PROPIO.
    ///
    /// La letra de la .107: «los minion son demasiado grande, deben ser
    /// más pequeño, el tamaño debe ser el mismo que el del grimorio» +
    /// «ellos atacan al mismo tiempo, cada ojo debe ser individual y
    /// deben tener la libertad de atacar cuando quieran y a la velocidad
    /// que deseen, ya que la velocidad de ataque aumenta en cuanto más
    /// nivel tenga el grimorio, hasta un máximo de 50% más rápido a
    /// nivel 100, subiendo a partir de nivel 1» + «el proyectil que usan
    /// los minion es muy grueso, debe ser un poco más fino» + «la forma
    /// en la que deben moverse al rededor del libro es orbitar como una
    /// luna a un planeta, no solo dar vuelta en círculo».
    ///
    /// EL TAMAÑO: el espíritu viste el libro 36×49 a esc 1,15 = 41×56
    /// px; el ojo (56×54) se pone a ESCALA 0,75 = 42×41 en pantalla — LA
    /// LUNA CABE EN LA SOMBRA DEL PLANETA. Todo el layout (hitbox, iris,
    /// mirada, sello) escala con la MISMA constante.
    ///
    /// EL RITMO PROPIO: cada ojo dispara a SU velocidad — 110 ticks de
    /// base (1,83 s, la del Forbidden Ruin) divididos por la VELOCIDAD
    /// DE NIVEL (1,0× en nivel 1 subiendo LINEAL hasta 1,5× = un 50%
    /// más rápido en nivel 100, clavada ahí) y por el RITMO PERSONAL del
    /// ojo (0,85–1,15 por hash de whoAmI). El primer disparo tras
    /// invocar cae entre el 35% y el 100% del intervalo: la banda NUNCA
    /// dispara en bloque — cada ojo es INDIVIDUAL.
    ///
    /// LA LUNA: con el libro fuera, cada ojo recorre una ELIPSE (a =
    /// 62+42k, b = 0,8a) con precesión apsidal lenta y LA SEGUNDA LEY DE
    /// KEPLER — el área barrida por tick es CONSTANTE ⇒ ACELERA en el
    /// perigeo (cerca del libro) y FRENA en el apogeo (lejos): ORBITAR,
    /// no dar vuelta en círculo. La fase de cada ojo vive en el CAMPO DE
    /// INSTANCIA _faseOrbital (v6.50.108: localAI es float[3] en tML —
    /// localAI[3] tiraba IndexOutOfRange y los ojos no aparecían) y sólo
    /// la avanza PasoOrbita (UNA vez por tick, desde la AI — el draw la
    /// LEE, jamás la mueve).
    ///
    /// EL RAYO (la finura de la .107): el proyectil DEL CETRO DEL TRUENO
    /// PERLIN en su modo ojo, un POCO MÁS FINO — funda 6,0 (era 7,5),
    /// cápsulas negras de 15 px (era 19), cadenas 2,9 (la cirugía vive
    /// en TruenoPerlinProjectile.DrawArcoOjo).
    ///
    /// (El resto del repertorio hereda de la .106: el sprite del usuario
    /// SIN recorte con el iris ACOMODADO, la ley del planeo del Ruinous
    /// Staff, la corona viva que se balancea, el iris que manda, el
    /// parpadeo independiente, el círculo mágico de los agujeros negros
    /// y el retroceso con reincorporación.)
    /// </summary>
    public class OjoEternoMinion : ModProjectile
    {
        // === LOS MODOS (ai[0]) — la carga y el crucero ===
        private const float MODO_CRUCERO = 0f;     // el planeo continuo (la ley única)
        private const float MODO_APUNTANDO = 1f;   // la carga: freno, el sello respira

        private const int AIM_TICKS = 12;          // la carga del disparo (0,2 s)

        // === EL TAMAÑO DEL LIBRO (la .107: «deben ser más pequeño, el
        //     tamaño debe ser el mismo que el del grimorio») — el
        //     ESPÍRITU viste el sprite 36×49 a esc 1,15 = 41×56 px; el
        //     ojo 56×54 × 0,75 = 42×41: LA LUNA CABE EN LA SOMBRA DEL
        //     PLANETA. Todo el layout escala con ESTA constante ===
        private const float ESCALA = 0.75f;

        // === LA CADENCIA INDIVIDUAL (la .107: «cada ojo debe ser
        //     individual y deben tener la libertad de atacar cuando
        //     quieran y a la velocidad que deseen; la velocidad de ataque
        //     aumenta en cuanto más nivel tenga el grimorio, hasta un
        //     máximo de 50% más rápido a nivel 100, subiendo a partir de
        //     nivel 1»): BASE 110 ticks (1,83 s — la del Forbidden Ruin)
        //     dividida por la VELOCIDAD DE NIVEL y por el RITMO PROPIO ===
        private const int CADENCIA_BASE = 110;

        // === v6.50.108 — LA FASE ORBITAL VIVE AQUÍ (no en localAI[3]):
        //     el localAI de vanilla es float[3] (índices 0–2) — el [3]
        //     de la .107 tiraba IndexOutOfRangeException en cada AI y los
        //     ojos no aparecían. Campo de INSTANCIA: una fase POR OJO,
        //     determinista (nace del puesto en el anillo y avanza por
        //     PasoOrbita igual en todos los clientes) ===
        private float _faseOrbital;

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
            Vector2? libro = PosicionDelLibro(dueño);
            PasoOrbita(libro);   // v6.50.107: la fase de la luna avanza UNA vez por tick
            Vector2 slot = SlotDeFormacion(dueño, libro);
            Vector2 foco = modo == MODO_APUNTANDO && presa != null
                ? presa.Center
                : slot;

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
                Projectile.velocity *= 0.75f;                  // el freno de la carga
                if (presa == null || !presa.active || presa.life <= 0)
                {
                    SalirDeFuego();                            // la presa murió esperando
                }
                else if (t >= AIM_TICKS)
                {
                    Disparar(presa, nivel);
                    SalirDeFuego();
                }
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
                if (dist > 90f)
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

                // === EL DISPARO: si hay presa y la cadencia está lista ===
                if (presa != null && Projectile.frame <= 0)
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
            // === LA PATADA («un pequeño retroceso») — la formación lo
            //     reincorpora con la ley del planeo ===
            Vector2 atrás = Projectile.Center - presa.Center;
            if (atrás.LengthSquared() > 0.01f)
            {
                atrás.Normalize();
                Projectile.velocity = atrás * 3.2f;
            }

            // === EL GOLPE: el cauce del cetro (radio + cadena + GolpeMotor
            //     — resuelto por su dueño). El daño del minion escalado
            //     por nivel, como el contacto de siempre. ===
            if (Projectile.owner == Main.myPlayer)
            {
                int dano = Math.Max(1, (int)(Projectile.damage *
                    WeaponScaling.MinionContactDamageMult(nivel)));
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
        //  LA FORMACIÓN — la corona viva y las órbitas
        // ================================================================

        /// <summary>LA FORMACIÓN — la corona viva y las órbitas.
        /// v6.50.107 — LA ÓRBITA DE LA LUNA (la letra: «la forma en la
        /// que deben moverse al rededor del libro es orbitar como una
        /// luna a un planeta, no solo dar vuelta en círculo»): cada ojo
        /// recorre una ELIPSE con precesión lenta y LA SEGUNDA LEY DE
        /// KEPLER — el área barrida por tick es CONSTANTE, así que
        /// ACELERA al pasar cerca del libro y FRENA al alejarse. La fase
        /// de cada ojo vive en _faseOrbital (v6.50.108) y la avanza
        /// PasoOrbita (UNA vez por tick, desde la AI); AQUÍ sólo se LEE
        /// (el draw también la consulta). El anillo k lleva 5+k ojos
        /// (5, 6, 7… — la letra), contrarrotando.</summary>
        private Vector2 SlotDeFormacion(Player dueño, Vector2? libro)
        {
            int idx = Math.Max(0, (int)Projectile.minionPos);
            float time = Main.GlobalTimeWrappedHourly;

            if (libro is Vector2 centro)
            {
                // === LA ÓRBITA: la elipse del anillo k (5, 6, 7, …) ===
                AnilloDe(idx, out int k, out int enAnillo, out int capAnillo);
                float a = 62f + 42f * k;                     // semieje mayor
                float b = a * 0.80f;                         // semieje menor: la órbita INCLINADA
                float tilt = k * 0.9f
                    + 0.18f * MathF.Sin(time * 0.07f + k * 1.3f);  // precesión apsidal lenta
                float fase = FaseOrbital(enAnillo, capAnillo, k);
                Vector2 punto = new Vector2((float)Math.Cos(fase) * a,
                                             (float)Math.Sin(fase) * b);
                return centro + punto.RotatedBy(tilt)
                    // el vaivén propio del anillo (la corona también vive afuera)
                    + new Vector2(0f, MathF.Sin(time * 1.1f + enAnillo * 1.63f) * 3f);
            }

            // === LA CORONA: el arco sobre la cabeza — se abre con la
            //     banda (más ojos = arco más largo) y se BALANCEA entero ===
            int n = Math.Max(1, dueño.ownedProjectileCounts[Projectile.type]);
            int i = Math.Min(idx, n - 1);
            float f = (i + 0.5f) / n;
            float abre = MathHelper.ToRadians(Math.Min(1.5f * n, 7f));  // ±1,5° por ojo, tope ±7°
            float a0 = MathHelper.ToRadians(195f) - abre + MathF.Sin(time * 0.30f) * 0.20f;
            float a1 = MathHelper.ToRadians(345f) + abre + MathF.Sin(time * 0.30f) * 0.20f;
            float angC = MathHelper.Lerp(a0, a1, f);
            float radioC = Math.Min(56f + n * 2.8f, 88f)
                           + MathF.Sin(time * 0.23f) * 3f;       // el radio respira
            Vector2 pos = dueño.MountedCenter + new Vector2(
                (float)Math.Cos(angC) * radioC,
                (float)Math.Sin(angC) * radioC);
            pos.Y += MathF.Sin(time * 1.1f + i * 1.63f) * 3f;    // el vaivén propio
            return pos;
        }

        /// <summary>EL ANILLO k: la escalera 5, 6, 7… (la letra) — el
        /// índice global del minion se parte en anillo y puesto.</summary>
        private static void AnilloDe(int idx, out int k, out int enAnillo, out int capAnillo)
        {
            k = 0;
            int baseIdx = 0;
            while (k < 9)
            {
                int cap = 5 + k;
                if (idx < baseIdx + cap) break;
                baseIdx += cap;
                k++;
            }
            capAnillo = 5 + k;
            enAnillo = idx - baseIdx;
        }

        /// <summary>LA FASE ORBITAL de ESTE ojo (_faseOrbital — v6.50.108:
        /// era localAI[3] y localAI es float[3] en tML — el índice 3
        /// tiraba IndexOutOfRange): nace el día que el libro sale,
        /// repartida por su puesto en el anillo. Determinista (hash de
        /// whoAmI implícito en el puesto) — todos los clientes la
        /// calculan IGUAL.</summary>
        private float FaseOrbital(int enAnillo, int capAnillo, int k)
        {
            if (_faseOrbital <= 0f)
                _faseOrbital = 0.37f
                    + enAnillo / (float)capAnillo * MathHelper.TwoPi + k * 0.7f;
            return _faseOrbital;
        }

        /// <summary>v6.50.107 — EL PASO DE LA LUNA (UNA vez por tick,
        /// desde la AI): LA SEGUNDA LEY DE KEPLER — el área barrida por
        /// tick es constante ⇒ dθ = ω·a²/r²: el ojo ACELERA en el
        /// perigeo (r corto) y FRENA en el apogeo (r largo). ORBITAR,
        /// no dar vuelta en círculo. Contrarrotación par/impar; el
        /// reloj NUNCA se para (ni en la carga del disparo).</summary>
        private void PasoOrbita(Vector2? libro)
        {
            if (!(libro is Vector2)) return;            // en la corona no hay luna
            int idx = Math.Max(0, (int)Projectile.minionPos);
            AnilloDe(idx, out int k, out int enAnillo, out int capAnillo);

            float a = 62f + 42f * k;
            float b = a * 0.80f;

            float fase = FaseOrbital(enAnillo, capAnillo, k);
            float ca = (float)Math.Cos(fase), sa = (float)Math.Sin(fase);
            float r2 = (a * ca) * (a * ca) + (b * sa) * (b * sa);
            if (r2 < 1f) r2 = 1f;

            float omega = (k % 2 == 0) ? 0.32f : -0.27f;   // rad/s — contrarrotación
            float dFase = omega / 60f * (a * a) / r2;      // Kepler: dθ ∝ a²/r²
            fase += dFase;
            while (fase >= MathHelper.TwoPi) fase -= MathHelper.TwoPi;
            while (fase < 0f) fase += MathHelper.TwoPi;
            if (fase <= 0f) fase = 0.01f;                  // jamás volver a «sin nacer»
            _faseOrbital = fase;
        }

        /// <summary>¿DÓNDE ESTÁ EL LIBRO? (el espíritu fuera — el patrón
        /// del Nervioso, cacheado por frame para la banda entera).</summary>
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
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>();
            bool hasBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
                if (dueño.buffType[i] == buffType && dueño.buffTime[i] > 0) { hasBuff = true; break; }

            if (!hasBuff) Projectile.Kill();
            else
            {
                // el timeLeft nunca expira (el fix v6.50.3 del Orbe)
                Projectile.timeLeft = 2;
                dueño.AddBuff(buffType, 18000);
            }
        }

        /// <summary>EL CÍRCULO MÁGICO — el sello erosionado de los agujeros
        /// negros (OrbitaLib.SigiloErosionado, las runas de RunasAbismo),
        /// en pequeño alrededor del ojo. En la carga del disparo el sello
        /// RESPIRA hacia afuera (r 12→15 — v6.50.107: la escala del
        /// LIBRO, antes 16→20 del ojo grande).</summary>
        private void DrawElSello()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Projectile.whoAmI * 13 + 7;

            // la carga: el sello inhala
            float r = 12f;
            if (Projectile.ai[0] == MODO_APUNTANDO)
                r = MathHelper.Lerp(12f, 15f, Math.Min(Projectile.ai[1] / (float)AIM_TICKS, 1f));

            float glyph = Math.Max(r / 52f, 0.25f) * 1.75f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                OrbitaLib.SigiloErosionado(pos, r, time, seed,
                    2.55f, 8, 0.06f, glyph, SelloOro, SelloPunta,
                    offset: 0, hashOff: 0, spikeAngle: -0.55f, spikeWindow: 0.45f);

                // la presencia: el aliento violáceo DETRÁS del ojo (la lectura nocturna)
                float pulso = 0.09f + 0.05f * MathF.Sin(time * 2.6f + Projectile.whoAmI);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    new Color(96, 48, 150, 255) * pulso, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(2.5f, 2.1f),
                    SpriteEffects.None, 0f);
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>EL OJO — el sprite EXACTO del usuario (56×54, sin
        /// recorte) al TAMAÑO DEL LIBRO (ESCALA 0,75 = 42×41 en pantalla):
        /// la base (abierto), el párpado (medio/cerrado, la convención de
        /// fases del libro: fase>7||fase<4 = medio) y el IRIS ACOMODADO
        /// deslizándose por la almendra (reposo en el centro medido del
        /// arte; viaje ±7,5 × ±2,5 — TODO escalado con ESCALA).</summary>
        private void DrawElOjo()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
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
                    Main.spriteBatch.Draw(parpado, pos, null, color, 0f, origen, ESCALA,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    Main.spriteBatch.Draw(abierto, pos, null, color, 0f, origen, ESCALA,
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
                        pos + (IRIS_REST + mirada) * ESCALA, null, color, 0f,
                        iris.Size() * 0.5f, IRIS_ESCALA * ESCALA, SpriteEffects.None, 0f);
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
            // el ojo viste el TAMAÑO DEL LIBRO (ESCALA 0,75 = 42×41 en
            // pantalla); la hitbox lógica (la almendra) va adentro — el
            // rayo es el ataque, no el contacto
            Projectile.width = 25;
            Projectile.height = 15;
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
