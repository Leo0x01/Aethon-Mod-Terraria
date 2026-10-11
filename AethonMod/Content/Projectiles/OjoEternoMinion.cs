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
    /// OJO ETERNO MINION — v6.50.106 — EL OJO DEL USUARIO, SIN RECORTE.
    ///
    /// La letra del usuario: «no debes hacer recorte ya que arruinaste el
    /// recorte que yo mismo hice, solo debes acomodar el iris para que
    /// pueda realmente funcionar […] la animacion que necesita de
    /// movimiento es la misma que este invocador Ruinous_Staff» +
    /// «el proyecril que debe usar el minion debe ser el mismo que Cetro
    /// de Trueno de Perlin pero teñido de negro en el centro y morado en
    /// los bordes con bruma en la punta».
    ///
    /// EL SPRITE (la lección de la .105): los 3 archivos del usuario YA
    /// ERAN su recorte perfecto (512×500: el ojo con su armazón dorado,
    /// sus 8 remaches y sus chispas moradas) — la .105 le hizo OTRO
    /// recorte encima y lo molió a 44×37. Ahora: CERO recorte; el
    /// contenido opaco del archivo ES el sprite (56×54 en juego) y el
    /// IRIS de la .84 va ACOMODADO en el centro de la ALMENDRA medida
    /// del arte (fracción 0.513, 0.543 del recorte).
    ///
    /// EL MOVIMIENTO (el Ruinous Staff — AI_206, wiki 1.4.5.7): UNA ley
    /// de planeo continuo, sin la máquina mirando/moviendo de la .105
    /// (frenaba en seco cerca del hueco, aceleraba al salir del umbral y
    /// rebotaba — el «se mueven erráticos de arriba hacia abajo»): lejos
    /// del hueco se dirige a él con inercia (arranque suave por
    /// distancia), cerca lo SIGUE con amortiguación crítica — el hueco
    /// se mueve (corona/órbita) y el ojo danza con él, sin sobrepasar
    /// jamás. La cadencia 110−nivel/3 (≈1,83 s — la del Forbidden Ruin).
    ///
    /// LA CORONA VIVA: el arco sobre la cabeza se BALANCEA (±11°), el
    /// radio RESPIRA y cada ojo va con su vaivén propio (5,7 s) — la
    /// corona de la .105 casi no se movía (0,09 rad/s = período de 70 s)
    /// y leía muerta.
    ///
    /// EL RAYO: el proyectil DEL CETRO DEL TRUENO PERLIN (el mismo
    /// TruenoPerlinProjectile en su modo ojo, ai[0]=1) — el ancla nace
    /// PEGADA al monstruo, el arco cuelga del OJO, y el golpe es el del
    /// cetro (radio + cadena + GolpeMotor): el daño de verdad. Teñido:
    /// negro en el centro, morado en los bordes, bruma en la punta.
    ///
    /// EL IRIS MANDA («primero mira y luego se mueve»): el iris barre a
    /// 0,22 rad/t hacia el foco — llega al ángulo mucho antes de que el
    /// cuerpo cruce la mitad del camino; en la carga del disparo el
    /// párpado se CORTA y el ojo queda ABIERTO clavado en su presa (la
    /// mirada del cazador — la .105 lo dejaba en «medio» por error de
    /// fase: forzar fase 1 = medio, no abierto).
    ///
    /// EL PARPADEO INDEPENDIENTE (la .105): reloj propio por ojo, cero
    /// RNG, SIN leer nada del hambre del libro.
    /// </summary>
    public class OjoEternoMinion : ModProjectile
    {
        // === LOS MODOS (ai[0]) — la carga y el crucero ===
        private const float MODO_CRUCERO = 0f;     // el planeo continuo (la ley única)
        private const float MODO_APUNTANDO = 1f;   // la carga: freno, el sello respira

        private const int AIM_TICKS = 12;          // la carga del disparo (0,2 s)

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

        /// <summary>La cadencia del rayo por nivel (≈1,83 s de base — la
        /// del Forbidden Ruin del Ruinous Staff), piso 55.</summary>
        private int Cadencia => Math.Clamp(110 - Level / 3, 55, 110);

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

        /// <summary>El fin del fuego: vuelve al crucero y arranca la cadencia.</summary>
        private void SalirDeFuego()
        {
            Projectile.ai[0] = MODO_CRUCERO;
            Projectile.ai[1] = 0f;
            Projectile.frame = Cadencia;
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

        /// <summary>LA CORONA sobre la cabeza del dueño — el arco se
        /// BALANCEA (±11° a 0,30 rad/s), el radio RESPIRA (±3 px) y cada
        /// ojo lleva su vaivén propio (5,7 s por ciclo) — la corona VIVA
        /// que la .105 no tenía. Con el ESPÍRITU FUERA: ÓRBITAS
        /// CONCÉNTRICAS AL LIBRO — el anillo k lleva 5+k ojos (5, 6, 7…
        /// — la letra), radio 66+44k, contrarrotando.</summary>
        private Vector2 SlotDeFormacion(Player dueño, Vector2? libro)
        {
            int idx = Math.Max(0, (int)Projectile.minionPos);
            float time = Main.GlobalTimeWrappedHourly;

            if (libro is Vector2 centro)
            {
                // === LA ÓRBITA: el anillo k lleva 5+k (5, 6, 7, …) ===
                int k = 0, baseIdx = 0;
                while (k < 9)
                {
                    int cap = 5 + k;
                    if (idx < baseIdx + cap) break;
                    baseIdx += cap;
                    k++;
                }
                int capAnillo = 5 + k;
                int enAnillo = idx - baseIdx;
                float radio = 66f + 44f * k;
                float vel = (k % 2 == 0) ? 0.32f : -0.26f;       // contrarrotación
                float ang = enAnillo / (float)capAnillo * MathHelper.TwoPi
                          + k * 0.7f + time * vel;
                return centro + new Vector2((float)Math.Cos(ang) * radio,
                                            (float)Math.Sin(ang) * radio)
                       // el vaivén propio del anillo (la corona también vive arriba)
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
            float radioC = Math.Min(64f + n * 3.2f, 98f)
                           + MathF.Sin(time * 0.23f) * 3f;       // el radio respira
            Vector2 pos = dueño.MountedCenter + new Vector2(
                (float)Math.Cos(angC) * radioC,
                (float)Math.Sin(angC) * radioC);
            pos.Y += MathF.Sin(time * 1.1f + i * 1.63f) * 3f;    // el vaivén propio
            return pos;
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
        /// RESPIRA hacia afuera (r 16→20).</summary>
        private void DrawElSello()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Projectile.whoAmI * 13 + 7;

            // la carga: el sello inhala
            float r = 16f;
            if (Projectile.ai[0] == MODO_APUNTANDO)
                r = MathHelper.Lerp(16f, 20f, Math.Min(Projectile.ai[1] / (float)AIM_TICKS, 1f));

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
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(3.3f, 2.8f),
                    SpriteEffects.None, 0f);
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>EL OJO — el sprite EXACTO del usuario (56×54, sin
        /// recorte): la base (abierto), el párpado (medio/cerrado, la
        /// convención de fases del libro: fase>7||fase<4 = medio) y el
        /// IRIS ACOMODADO deslizándose por la almendra (reposo en el
        /// centro medido del arte; viaje ±7,5 × ±2,5).</summary>
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
                    Main.spriteBatch.Draw(parpado, pos, null, color, 0f, origen, 1f,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    Main.spriteBatch.Draw(abierto, pos, null, color, 0f, origen, 1f,
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
                    Main.spriteBatch.Draw(iris, pos + IRIS_REST + mirada, null, color, 0f,
                        iris.Size() * 0.5f, IRIS_ESCALA, SpriteEffects.None, 0f);
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
            // el sprite del usuario viste 56×54; la almendra (la hitbox
            // lógica) queda adentro — el rayo es el ataque, no el contacto
            Projectile.width = 34;
            Projectile.height = 20;
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
