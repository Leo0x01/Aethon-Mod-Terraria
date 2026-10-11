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
    /// OJO ETERNO MINION — v6.50.105 — LOS MINIONS SON LOS OJOS DEL LIBRO.
    ///
    /// La letra del usuario: «también debemos cambiar los minions, los
    /// minion que use el grimorio será su propio ojo […] es tiempo de
    /// animar al minion, yo te daré los sprite del ojo usando código […]
    /// ya sabes como animar los ojos, ahora ten en cuenta que el ojo se
    /// mueve en la direccion en la que mira, o sea para moverse primero
    /// mira y luego se mueve, algo mas, estos minion se posicionan encima
    /// del jugador como una corona, y si el grimorio esta fuera comienzan
    /// a orbitar al grimorio en 2 circulos uno de 5 minion, otro de 6 y
    /// asi aumentando los circulos orbitales a mas minion pueda lanzarce
    /// […] cada minion mira a su objetivo y usa el rayo con ruido de
    /// perlin ademas de que este rayo usado por los minion es negro, con
    /// bordes morado y con bruma en la punta que toca al monstruo, cada
    /// minion debe tener un pequeño circulo magico igual que el de los
    /// agujeros negros […] cuando el minion dispara tiene un pequeño
    /// retroceso y luego se reincorpora, los ojos de cada minion son
    /// independientes de cada uno de ellos y del libro y no se ven
    /// afectados por la animacion de hambre del grimorio».
    ///
    /// EL CUERPO: el ojo del grimorio DESPRENDIDO — el recorte del sprite
    /// EXACTO del usuario (los 3 estados en código: abierto / medio /
    /// cerrado), 44×37, con el iris de la .84 como CAPA que se desliza
    /// (la técnica del libro: la esclera es la base y el iris va encima).
    ///
    /// LA MIRADA MANDA («primero mira y luego se mueve»): el ojo fija su
    /// iris en el destino ANTES de moverse — una fase de mirada (freno,
    /// el iris gira) y luego el desplazamiento. La mirada del iris se
    /// guarda como ÁNGULO (localAI[2]) y barre suave hacia el foco.
    ///
    /// LA FORMACIÓN: CORONA sobre la cabeza del dueño (arco 200°→340°);
    /// con el ESPÍRITU FUERA (GrimorioNerviosoFlotante), ÓRBITAS
    /// CONCÉNTRICAS AL LIBRO: el anillo k lleva 5+k ojos (5, 6, 7…) —
    /// «uno de 5 minion, otro de 6 y así aumentando los círculos
    /// orbitales a más minion pueda lanzarse» — contrarrotando (par
    /// horario, impar antihorario, la firma de la casa).
    ///
    /// EL RAYO: Perlin negro con bordes morados — el canal meandrante
    /// (fBm, la matemática del PerlinBolt) se dibuja DOS veces: la funda
    /// morada ADITIVA (RayoStrip, el borde que arde) y encima el NÚCLEO
    /// NEGRO en AlphaBlend (cápsulas que OSCURECEN: el centro negro, el
    /// filo morado asomando en los bordes) — con la BRUMA de SombrasLib
    /// en la punta que toca al monstruo. El golpe va por GOLPEMOTOR (el
    /// cauce del motor: crítica, varianza, on-hit, crédito de la kill).
    ///
    /// EL CÍRCULO MÁGICO: OrbitaLib.SigiloErosionado — EL MISMO de los
    /// agujeros negros (runas de RunasAbismo, dorado-ámbar, el aro roto
    /// con huecos), en pequeño: el ojo flota en el centro de su sello.
    ///
    /// EL PARPADEO INDEPENDIENTE: cada ojo lleva SU reloj (localAI[0/1],
    /// hash propio por whoAmI) — la convención de fases del libro (la
    /// .86: fase>7||fase<4 = medio) — y NO lee NADA del hambre del
    /// grimorio: los ojos no heredan la ansiedad del libro. Al apuntar,
    /// el párpado se corta y el ojo queda ABIERTO en blanco (la mirada
    /// del cazador).
    ///
    /// MP: la IA corre en el dueño (tML); el rayo y el objetivo se
    /// re-derivan en el draw (el más cercano válido — la misma lista en
    /// todos los clientes); el golpe lo resuelve el dueño por GolpeMotor
    /// (el modelo vanilla). El parpadeo es visual-local (la deuda
    /// documentada de la casa — el reloj nervioso ya la carga).
    /// </summary>
    public class OjoEternoMinion : ModProjectile
    {
        // === LOS MODOS (ai[0]) — la máquina de la mirada ===
        private const float MODO_MIRANDO = 0f;     // frenando, el iris fija el destino
        private const float MODO_MOVIENDO = 1f;    // el desplazamiento (la mirada ya está puesta)
        private const float MODO_APUNTANDO = 2f;   // la carga: freno en seco, el círculo respira
        private const float MODO_DISPARANDO = 3f;  // EL RAYO (el canal vivo + la bruma en la presa)

        private const int GAZE_TICKS = 6;         // «primero mira»: la pausa del iris
        private const int AIM_TICKS = 14;         // la carga del disparo
        private const int FIRE_TICKS = 16;        // el rayo encendido

        // === EL RAYO: la paleta (negro con bordes morados) —
        //     calibrado con el mock VLM (r1 «sin borde morado» · r2 «demasiado
        //     tenue»): el violeta VIVO y el ancho 9,5 (la falda morada ≈29 px)
        //     para que el BORDE lea también sobre fondos claros ===
        private static readonly Color RayoMorado = new(190, 110, 255);   // la funda: el borde que arde
        private static readonly Color RayoClaro = new(240, 224, 255);    // la vena: el filo pálido
        private static readonly Color RayoNegro = new(8, 3, 14, 215);    // el núcleo que oscurece

        // === EL CÍRCULO MÁGICO (el de los agujeros negros, en pequeño) ===
        private static readonly Color SelloOro = new(240, 124, 65);      // dorado-ámbar (MEDIDO del Umbral)
        private static readonly Color SelloPunta = new(255, 205, 140);   // la punta pálida

        // === LA CORONA: el arco 200°→340° sobre la cabeza (rad) ===
        private const float CORONA_A0 = 3.49f;
        private const float CORONA_A1 = 5.93f;

        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;    // el parpadeo no usa frames: son 3 texturas por capas
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;                  // la almendra (el sprite 44×37 viste por fuera)
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
            Projectile.light = 0.35f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;   // el RAYO es el ataque: el ojo no muerde

        // ================================================================
        //  EL NIVEL (el patrón del Orbe Cósmico: ai[2] desde el spawn,
        //  con el fallback de HeldItem para los veteranos de la red)
        // ================================================================
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

        /// <summary>La cadencia del rayo por nivel: 110 ticks base, −1 por
        /// 3 niveles, piso 55 (nivel 165+). Un ojo dispara cada ~1,8 s de
        /// base y cada ~0,9 s al tope — la andanada la pone la CORONA.</summary>
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
            //     del libro — cada ojo lleva SU reloj; al apuntar, el párpado
            //     se corta y la mirada queda FIJA en blanco) ===
            if (modo != MODO_APUNTANDO && modo != MODO_DISPARANDO)
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
            else if (Projectile.localAI[1] > 1f)
                Projectile.localAI[1] = 1f;                        // la mirada del cazador: abrir YA

            // === EL FOCO DE LA MIRADA: a dónde apunta el iris ===
            NPC presa = ObjetivoDelOjo(dueño, nivel);
            Vector2? libro = PosicionDelLibro(dueño);
            Vector2 slot = SlotDeFormacion(dueño, libro);
            Vector2 foco = (modo == MODO_APUNTANDO || modo == MODO_DISPARANDO) && presa != null
                ? presa.Center
                : slot;

            // el ángulo de la mirada barre suave (0,22 rad/tick) hacia el foco
            float angMeta = foco == Projectile.Center
                ? Projectile.localAI[2]
                : (float)Math.Atan2(foco.Y - Projectile.Center.Y, foco.X - Projectile.Center.X);
            if (Projectile.localAI[2] == 0f) Projectile.localAI[2] = angMeta;
            float da = MathHelper.WrapAngle(angMeta - Projectile.localAI[2]);
            Projectile.localAI[2] = MathHelper.WrapAngle(
                Projectile.localAI[2] + MathHelper.Clamp(da, -0.22f, 0.22f));

            // === EL RETROCESO (física): la patada va fuera y la formación
            //     lo reincorpora — el ojo frena el disparo y luego vuelve ===
            if (modo == MODO_DISPARANDO)
                Projectile.velocity *= 0.86f;

            // === LA MÁQUINA ===
            switch (modo)
            {
                case MODO_APUNTANDO:
                    Projectile.velocity *= 0.72f;                  // freno en seco: la carga
                    if (presa == null) { SalirDeFuego(); break; }
                    if (t >= AIM_TICKS)
                    {
                        Projectile.ai[0] = MODO_DISPARANDO;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;

                        // === EL GOLPE (el cauce del motor — GolpeMotor) ===
                        if (presa != null && presa.active && presa.life > 0)
                        {
                            int dano = Math.Max(1, (int)(Projectile.damage *
                                WeaponScaling.MinionContactDamageMult(nivel)));
                            GolpeMotor.Golpear(Projectile, presa, dano, Projectile.knockBack * 0.6f);
                        }

                        // === LA PATADA («un pequeño retroceso») ===
                        Vector2 atrás = Projectile.Center - presa.Center;
                        if (atrás.LengthSquared() > 0.01f)
                        {
                            atrás.Normalize();
                            Projectile.velocity = atrás * 3.0f;
                        }

                        Sonar(SoundID.Item122.WithPitchOffset(-0.35f).WithVolumeScale(0.5f),
                            Projectile.Center);
                    }
                    break;

                case MODO_DISPARANDO:
                    if (presa == null) { SalirDeFuego(); break; }
                    if (t >= FIRE_TICKS) { SalirDeFuego(); break; }
                    break;

                default:
                    // === LA FORMACIÓN: primero MIRA (frena), luego SE MUEVE ===
                    Vector2 alSlot = slot - Projectile.Center;
                    float dist = alSlot.Length();

                    if (modo == MODO_MIRANDO)
                    {
                        Projectile.velocity *= 0.84f;              // el ojo se detiene a mirar
                        if (t >= GAZE_TICKS && dist > 14f)
                        {
                            Projectile.ai[0] = MODO_MOVIENDO;
                            Projectile.ai[1] = 0f;
                        }
                    }
                    else // MOVIENDO
                    {
                        if (dist < 12f)
                        {
                            Projectile.velocity *= 0.72f;
                            Projectile.ai[0] = MODO_MIRANDO;       // llegó: el ciclo recomienza
                            Projectile.ai[1] = 0f;
                        }
                        else if (dist > 0.1f)
                        {
                            float velocidad = 13f * WeaponScaling.MinionSpeedMult(nivel);
                            Vector2 deseada = Vector2.Normalize(alSlot) * velocidad;
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.16f);
                        }
                        // en órbita el slot gira: la persecución es continua
                    }

                    // === EL DISPARO: si hay presa y la cadencia está lista ===
                    if (presa != null && Projectile.frame <= 0)
                    {
                        Projectile.ai[0] = MODO_APUNTANDO;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                    }
                    break;
            }

            Projectile.ai[1]++;
            if (Projectile.frame > 0) Projectile.frame--;         // la cadencia (frame prestado: el draw es nuestro)

            // === LA PRESENCIA: el halo suave del ojo + el polvillo ===
            Lighting.AddLight(Projectile.Center, new Vector3(0.50f, 0.38f, 0.20f));
            if (Main.netMode != NetmodeID.Server && Projectile.ai[0] == MODO_DISPARANDO &&
                Main.rand.NextBool(3) && presa != null)
            {
                Dust d = Dust.NewDustPerfect(presa.Center, DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -0.5f),
                    128, new Color(128, 64, 200), 0.45f);
                d.noGravity = true;
            }
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
                if (Projectile.ai[0] == MODO_DISPARANDO)
                    DrawElRayo();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El fin del fuego: vuelve a MIRAR y arranca la cadencia.</summary>
        private void SalirDeFuego()
        {
            Projectile.ai[0] = MODO_MIRANDO;
            Projectile.ai[1] = 0f;
            Projectile.frame = Cadencia;
            Projectile.netUpdate = true;
        }

        // ================================================================
        //  LA FORMACIÓN — la corona y las órbitas
        // ================================================================

        /// <summary>LA CORONA sobre la cabeza del dueño (arco 200°→340°,
        /// el radio crece con la banda) o LAS ÓRBITAS al libro flotante:
        /// el anillo k lleva 5+k ojos (5, 6, 7… — la letra), radio
        /// 58+42k, contrarrotando (par horario, impar al revés).</summary>
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
                float radio = 58f + 42f * k;
                float vel = (k % 2 == 0) ? 0.34f : -0.27f;        // contrarrotación
                float ang = enAnillo / (float)capAnillo * MathHelper.TwoPi
                          + k * 0.7f + time * vel;
                return centro + new Vector2((float)Math.Cos(ang) * radio,
                                            (float)Math.Sin(ang) * radio);
            }

            // === LA CORONA: el arco sobre la cabeza ===
            int n = Math.Max(1, dueño.ownedProjectileCounts[Projectile.type]);
            int i = Math.Min(idx, n - 1);
            float f = (i + 0.5f) / n;
            float angC = MathHelper.Lerp(CORONA_A0, CORONA_A1, f);
            float radioC = Math.Min(52f + n * 2.2f, 86f);
            Vector2 pos = dueño.MountedCenter + new Vector2(
                (float)Math.Cos(angC) * radioC,
                (float)Math.Sin(angC) * radioC);
            pos.Y += MathF.Sin(time * 0.09f + i * 1.31f) * 2.5f;  // el vaivén de la corona
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
        /// Orbe Cósmico, heredado) — la misma lista en todos los clientes:
        /// el draw re-deriva el punto del rayo con ESTA.</summary>
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
        /// RESPIRA hacia afuera (r 13→16).</summary>
        private void DrawElSello()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Projectile.whoAmI * 13 + 7;

            // la carga: el sello inhala
            float r = 13f;
            if (Projectile.ai[0] == MODO_APUNTANDO)
                r = MathHelper.Lerp(13f, 16f, Math.Min(Projectile.ai[1] / (float)AIM_TICKS, 1f));
            else if (Projectile.ai[0] == MODO_DISPARANDO)
                r = 15f + MathF.Sin(time * 6f) * 0.8f;

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
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(2.6f, 2.2f),
                    SpriteEffects.None, 0f);
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>EL OJO — la base (abierto), el párpado (medio/cerrado,
        /// la convención de fases del libro: fase>7||fase<4 = medio) y el
        /// IRIS deslizándose por la esclera (la técnica de la .84).</summary>
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
                    // almendra (horizontal ±7,5 · vertical ±3,5 — la geometría del ojo)
                    float dist = Vector2.Distance(FocoVivo(), Projectile.Center);
                    float alcance = MathHelper.Clamp(dist / 90f, 0f, 1f);
                    Vector2 mirada = new Vector2(
                        (float)Math.Cos(Projectile.localAI[2]) * 7.5f,
                        (float)Math.Sin(Projectile.localAI[2]) * 3.5f) * alcance;

                    Texture2D iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    Main.spriteBatch.Draw(iris, pos + mirada, null, color, 0f,
                        iris.Size() * 0.5f, 0.52f, SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>EL FOCO VIVO (para el alcance de la mirada): el slot de
        /// formación, o la presa mientras apunta/dispara.</summary>
        private Vector2 FocoVivo()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active) return Projectile.Center;
            NPC presa = ObjetivoDelOjo(dueño, Level);
            if ((Projectile.ai[0] == MODO_APUNTANDO || Projectile.ai[0] == MODO_DISPARANDO)
                && presa != null)
                return presa.Center;
            return SlotDeFormacion(dueño, PosicionDelLibro(dueño));
        }

        /// <summary>
        /// EL RAYO PERLIN NEGRO — el canal meandrante (la matemática del
        /// PerlinBolt: fBm 3 octavas + jitter 2, el arco congelado, la
        /// envolvente que clava los anclajes, Chaikin) dibujado en DOS
        /// capas: la FUNDA MORADA aditiva (RayoStrip — el borde que arde)
        /// y el NÚCLEO NEGRO en AlphaBlend encima (las cápsulas que
        /// OSCURECEN: el centro negro, el morado asomando en los bordes).
        /// La BRUMA vive en la punta que toca al monstruo.
        /// </summary>
        private void DrawElRayo()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null) return;
            NPC presa = ObjetivoDelOjo(dueño, Level);
            if (presa == null) return;

            Vector2 ojo = Projectile.Center;
            Vector2 punta = presa.Center;
            Vector2 a = ojo - Main.screenPosition;
            Vector2 b = punta - Main.screenPosition;

            int flick = StormLib.FlickTick(Main.GlobalTimeWrappedHourly, 9f);
            int seed = Projectile.whoAmI * 31 + 5;
            Vector2[] canal = CanalPerlin(a, b, seed, flick, 0.10f);

            // el ramp: entra en 3 ticks, sale en 3
            float t = Projectile.ai[1];
            float alfa = 0.9f * Math.Min(1f, Math.Min(t / 3f, (FIRE_TICKS - t) / 3f));
            if (alfa <= 0.02f) return;

            // === 1. LA FUNDA MORADA (aditiva — RayoStrip hace su propio baile
            //     y al salir REABRE un lote aditivo: hay que cerrarlo antes
            //     de cambiar de modo — la lección de la .101) ===
            float ancho = 9.5f;
            RayoStrip.Filamento(canal, ancho, RayoMorado, RayoClaro, alfa,
                StormTaper.Center, seed, flick, suavizar: false, crackle: true);
            VFXCore.CerrarLoteSiAbierto();

            // === 2. EL NÚCLEO NEGRO (AlphaBlend: las cápsulas que oscurecen) ===
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Texture2D glow = VFXCore.SoftGlow;
                Vector2 medio = glow.Size() * 0.5f;

                // el camino a paso fijo (arcos acumulados — O(n), cero O(n²))
                int n = canal.Length;
                float[] arcos = new float[n];
                float total = 0f;
                for (int i = 1; i < n; i++)
                {
                    total += Vector2.Distance(canal[i - 1], canal[i]);
                    arcos[i] = total;
                }
                int seg = 0;
                for (float s = 0f; s <= total; s += 2.5f)
                {
                    while (seg + 1 < n && arcos[seg + 1] < s) seg++;
                    float tramo = arcos[seg + 1] - arcos[seg];
                    float f = tramo > 0.1f ? (s - arcos[seg]) / tramo : 0f;
                    Vector2 p = Vector2.Lerp(canal[seg], canal[seg + 1],
                        MathHelper.Clamp(f, 0f, 1f));

                    // el núcleo se afina hacia la punta (termina en aguja):
                    // 5,5→3 px — DENTRO de la zona brillante de la funda
                    // (la columna 0→0,42 de RayoStrip), para que el BORDE
                    // MORADO asome alrededor del negro («negro con bordes
                    // morado» — calibrado con el mock VLM: a 11 px el negro
                    // tapaba la vena entera y el filo no leía)
                    float k = s / Math.Max(1f, total);
                    float w = 5.5f - 2.5f * k;
                    Main.spriteBatch.Draw(glow, p, null, RayoNegro * alfa, 0f, medio,
                        new Vector2(w, w), SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }

            // === 3. LOS GORROS de descarga (el electrodo del ojo y la presa) ===
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                StormLib.EndCap(Main.spriteBatch, a, ancho * 0.8f, RayoMorado, RayoClaro, alfa);
                StormLib.EndCap(Main.spriteBatch, b, ancho * 0.66f, RayoMorado, RayoClaro, alfa);
            }
            finally { Main.spriteBatch.End(); }

            // === 4. LA BRUMA EN LA PUNTA (la que toca al monstruo) ===
            float prog = Math.Min(1f, t / (float)FIRE_TICKS);
            SombrasLib.Bruma(punta, 24f * (0.8f + 0.4f * prog), 0.55f * alfa,
                Main.GlobalTimeWrappedHourly, seed + 91, new Vector2(0f, -10f * prog));
        }

        /// <summary>
        /// EL CANAL PERLIN — el tronco del PerlinBolt (StormLib), VERBATIM
        /// en su matemática: el meandro fBm 3 octavas (2,8 ciclos), el
        /// jitter de 2 octavas (10 ciclos), el arco congelado por semilla,
        /// la envolvente exp(−5000·(t−½)^10) que clava los anclajes, 48
        /// puntos + UNA pasada de Chaikin — la curvatura continua del
        /// arco que serpentea. Sin ramas: el rayo del ojo es UN trazo.
        /// </summary>
        private static Vector2[] CanalPerlin(Vector2 start, Vector2 end, int seed, int flick, float ampFrac)
        {
            float len = Vector2.Distance(start, end);
            if (len < 4f) return new Vector2[] { start, end };

            Vector2 dir = (end - start) / len;
            Vector2 perp = new Vector2(-dir.Y, dir.X);

            float a = ampFrac > 0.001f ? ampFrac * len : 0.12f * len;
            a = MathF.Min(a, len * 0.30f);

            float faseM = flick * 0.15f;
            float faseJ = flick * 0.47f;

            const int N = 48;
            var pts = new Vector2[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N;
                float m = (StormLib.Fbm1D(t * 2.8f + faseM, seed, 3) - 0.5f) * 2f;
                float j = (StormLib.Fbm1D(t * 10f + faseJ, seed + 77, 2) - 0.5f) * 2f;
                float arco = (StormLib.Ruido1D(t * 0.5f + seed * 0.013f, seed + 55) - 0.5f) * 2f;
                float d = t - 0.5f;
                float d2 = d * d;
                float d5 = d2 * d2 * d;
                float env = MathF.Exp(-5000f * d5 * d5);
                float off = (m * 0.62f + j * 0.38f) * a + arco * 0.45f * a;
                pts[i] = start + dir * (len * t) + perp * (off * env);
            }
            pts[0] = start;
            pts[N] = end;
            return StormLib.Chaikin(pts, 1);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
