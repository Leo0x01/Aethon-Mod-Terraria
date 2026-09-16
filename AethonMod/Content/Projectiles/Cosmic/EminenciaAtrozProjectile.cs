using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Weapons.Cosmic;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EminenciaAtrozProjectile — v6.30 — LA CONGREGACIÓN DE ESPÍRITUS.
    ///
    /// El proyectil único de LA EMINENCIA ATROZ (research/v630 — MEDIDO del
    /// sprite de congregación de referencia: 45.8% NEGRO + 26.4%
    /// violeta oscuro + 10.4% rojo oscuro con las CARAS ardiendo
    /// ROJO-NARANJA (253,74,60) DENTRO de la masa — v6.29 la hizo PÁLIDA y
    /// no se parecía en nada; v6.30 la corrige con los colores medidos):
    ///
    ///   · LA MASA NEGRA: BrumaFX.Cloud DOBLE — negra-violeta fuera, corazón
    ///     NEGRO dentro (la silueta se lee sobre cualquier fondo).
    ///   · EL MOVIMIENTO SALVAJE: sigue el cursor CON spring flojo... y de
    ///     cuando en cuando SE LARGA con un impulso de dardo (la
    ///     "se mueve salvajemente" de la referencia). Al madurar obedece (spring
    ///     ×2.4 — la abominación es "fully controllable").
    ///   · LOS ESPÍRITUS MENORES: 3..12 espíritus paramétricos que SE
    ///     LIBERAN de la masa, flotan... y SON TIRADOS DE VUELTA (el ciclo
    ///     exacto de la referencia — puramente visuales, deterministas).
    ///   · LA ACUMULACIÓN: 840 ticks de canal (14 s EXACTOS) → crecimiento
    ///     0→1 → LA ABOMINACIÓN: la nube SE APRIETA, EL OJO MAYOR domina
    ///     con su pupila carmesí SIGUIENDO la dirección del vuelo, corona
    ///     de ojos menores, estela Comet de EstelaLib y rugido propio.
    ///   · LA RAMPA DE DAÑO: cada 6 ticks, área — con mult = 1 + 0.85·x
    ///     (100% → 185% — el número exacto).
    ///   · LA CARA (el interior caótico): desde crecimiento 0.55, un ojo
    ///     grande y una boca asoman en VENTANAS CAÓTICAS dentro de la masa.
    ///   · v6.30 — EL SISTEMA DE TRES PASES: alfa1 (masa negra + zócalos +
    ///     brasas base) → aditivo (los OJOS ROJO-NARANJA ardiendo + estelas)
    ///     → alfa2 (LAS PUPILAS Y LA BOCA NEGRAS ENCIMA del brillo — la
    ///     mirada corta el propio fuego).
    ///
    /// El canal: mientras el dueño SOSTIENE el arma, la congregación vive
    /// y crece; si la suelta, la masa decae y se disipa (el "mana drain"
    /// de la referencia traducido a nuestra regla de maná 0).
    /// </summary>
    public class EminenciaAtrozProjectile : ModProjectile
    {
        /// <summary>Los 14 segundos EXACTOS de acumulación.</summary>
        private const int CanalTotal = 840;

        /// <summary>Grace de canal roto antes de que el crecimiento decaiga.</summary>
        private const int GraceSinCanal = 240;

        /// <summary>Ticks entre golpes del cuerpo.</summary>
        private const int GolpeCada = 6;

        private float _age;
        private float _crecimiento;      // 0..1 — la acumulación
        private float _sinCanalTicks;    // cuánto lleva sin canal
        private bool _rugidoHecho;       // el rugido de la abominación
        private bool _muerteHecha;       // la dissipación final

        /// <summary>El anillo de posiciones recientes (la estela de la abominación).</summary>
        private readonly Vector2[] _estela = new Vector2[10];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 56;
            // Daño 100% manual de área (la escuela A).
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista de red.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 23);

        private static ReLogic.Content.Asset<Texture2D> _disk;
        private static Texture2D DiscoNegro =>
            (_disk ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < _estela.Length; i++)
                _estela[i] = Projectile.Center;
        }

        /// <summary>¿El dueño está canalizando (sosteniendo el arma)?</summary>
        private bool CanalActivo
        {
            get
            {
                Player dueño = Main.player[Projectile.owner];
                return dueño.active && !dueño.dead &&
                    dueño.HeldItem != null &&
                    dueño.HeldItem.type == ModContent.ItemType<EminenciaAtrozStaff>() &&
                    dueño.channel;
            }
        }

        public override void AI()
        {
            _age += 1f;
            bool canal = CanalActivo;

            // ============================================================
            //  EL CANAL: mientras se sostiene, la congregación VIVE y CRECE
            // ============================================================
            if (canal)
            {
                _sinCanalTicks = 0f;
                // LA ACUMULACIÓN: 14 s EXACTOS del ritual.
                if (_crecimiento < 1f)
                    _crecimiento = Math.Min(1f, _crecimiento + 1f / CanalTotal);
                // El canal la mantiene viva (6 s de aire si se suelta).
                Projectile.timeLeft = 360;
            }
            else
            {
                _sinCanalTicks += 1f;
                // Tras la gracia, el crecimiento SE DERRAMA (los espíritus escapan).
                if (_sinCanalTicks > GraceSinCanal && _crecimiento > 0f)
                    _crecimiento = Math.Max(0f, _crecimiento - 1f / 180f);
            }

            float growth = _crecimiento;

            // ============================================================
            //  EL RUGIDO DE LA ABOMINACIÓN (el tamaño completo)
            // ============================================================
            if (growth >= 1f && !_rugidoHecho)
            {
                _rugidoHecho = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(-0.5f).WithVolumeScale(0.8f),
                            Projectile.Center);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item117.WithPitchOffset(-0.4f).WithVolumeScale(0.6f),
                            Projectile.Center);
                    }
                    catch { }
                    // EL LATIGUEO de la masa al compactarse.
                    for (int i = 0; i < 24; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ghost,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 4.5f,
                            160, i % 3 == 0 ? new Color(253, 74, 60) : new Color(36, 14, 48), 0.9f);
                        d.noGravity = true;
                    }
                }
            }

            // ============================================================
            //  EL MOVIMIENTO (solo el cliente dueño manda — la velocidad
            //  se sincroniza sola; en SP es el único que corre)
            // ============================================================
            if (Projectile.owner == Main.myPlayer)
            {
                // EL OBJETIVO: el cursor, clampeado (rango de la casa + la
                // pantalla medida con 100 px de gracia).
                Vector2 objetivo = Main.MouseWorld;
                var pantalla = new Rectangle(
                    (int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100,
                    Main.screenWidth + 200, Main.screenHeight + 200);
                objetivo.X = MathHelper.Clamp(objetivo.X, pantalla.X, pantalla.Right);
                objetivo.Y = MathHelper.Clamp(objetivo.Y, pantalla.Y, pantalla.Bottom);
                Vector2 alJugador = Projectile.Center - Main.player[Projectile.owner].Center;
                if (alJugador.Length() > 760f)
                {
                    alJugador = Vector2.Normalize(alJugador) * 760f;
                    objetivo = Main.player[Projectile.owner].Center + alJugador;
                }

                // EL SPRING: flojo de joven (la congregación esquiva el
                // control), APRETADO de adulta (la abominación obedece ×2.4).
                float k = canal ? (0.018f + 0.045f * growth) : 0.004f;
                Projectile.velocity += (objetivo - Projectile.Center) * k;

                // EL DARDO SALVAJE: de cuando en cuando la masa SE LARGA
                // (joven = impulsos FUERTES; madura casi no se le va).
                int tick = (int)_age;
                if (tick > 30 && tick % 46 == 0 &&
                    VFXCore.Hash01(Seed, tick / 46, 71) < 0.42f)
                {
                    float ang = VFXCore.Hash01(Seed, tick / 46, 73) * MathHelper.TwoPi;
                    float fuerza = (3.5f + 5.5f * (1f - growth)) * (canal ? 1f : 1.4f);
                    Projectile.velocity += new Vector2(
                        (float)Math.Cos(ang), (float)Math.Sin(ang)) * fuerza;
                }

                // El arrastre y el tope (la abominación VUELA más rápido).
                Projectile.velocity *= 0.92f;
                float tope = 10f + 8f * growth;
                if (Projectile.velocity.Length() > tope)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * tope;

                // Sin canal la masa SUBE (los espíritus ascienden) y se mece.
                if (!canal)
                    Projectile.velocity += new Vector2(
                        0.25f * (float)Math.Sin(_age * 0.03f), -0.06f);
            }

            // ============================================================
            //  EL DAÑO DEL CUERPO (cada 6 ticks — con la rampa medida)
            // ============================================================
            if (_age % GolpeCada == 0f)
                GolpearCuerpo(growth);

            // ============================================================
            //  LA ESTELA (el anillo de posiciones de la abominación)
            // ============================================================
            if (_age % 2f == 0f)
            {
                for (int i = _estela.Length - 1; i > 0; i--)
                    _estela[i] = _estela[i - 1];
                _estela[0] = Projectile.Center;
            }

            // ============================================================
            //  LA VIDA DE LA MASA: la luz pálida-carmesí
            // ============================================================
            if (_age % 4f == 0f)
            {
                float fade = MathHelper.Clamp(Projectile.timeLeft / 90f, 0f, 1f);
                Lighting.AddLight(Projectile.Center,
                    0.34f * fade * (0.5f + 0.5f * growth),
                    0.40f * fade,
                    0.44f * fade);
            }

            // ============================================================
            //  LA DISIPACIÓN FINAL (la muerte de la congregación)
            // ============================================================
            if (Projectile.timeLeft <= 2 && !_muerteHecha)
            {
                _muerteHecha = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item9.WithPitchOffset(-0.7f).WithVolumeScale(0.7f),
                            Projectile.Center);
                    }
                    catch { }
                    // LOS ESPÍRITUS ESCAPAN: la última voluta de la masa.
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ghost,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 1.4f) *
                                Main.rand.NextFloat(2f, 5f),
                            170, i % 3 == 0 ? new Color(253, 74, 60) : new Color(40, 16, 52), 1.1f);
                        d.noGravity = true;
                    }
                }
            }
        }

        /// <summary>
        /// EL GOLPE DEL CUERPO: daño de área con LA RAMPA EXACTA medida
        /// (100% → 185%). Los enemigos que mueren aquí sueltan espíritu.
        /// </summary>
        private void GolpearCuerpo(float growth)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float radio = 42f + 30f * growth;
            float mult = 1f + 0.85f * growth;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > radio + npc.width * 0.5f)
                    continue;
                int dmg = (int)(Projectile.damage * mult);
                npc.SimpleStrikeNPC(dmg, npc.direction, false, 3f, DamageClass.Magic);
                if (npc.life <= 0) MuerteEspiritual(npc);
            }
        }

        /// <summary>La muerte espectral: el cuerpo se disuelve hacia ARRIBA.</summary>
        private static void MuerteEspiritual(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server) return;
            Vector2 c = npc.Center;
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(c + new Vector2(
                        Main.rand.NextFloat(-npc.width * 0.5f, npc.width * 0.5f),
                        Main.rand.NextFloat(-npc.height * 0.5f, npc.height * 0.5f)),
                    DustID.Ghost,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-2.4f, -1.0f)),
                    150, i % 3 == 0 ? new Color(253, 74, 60) : new Color(40, 16, 52), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }
        }

        // ==================================================================
        //  EL DIBUJO — la masa, los espíritus, los ojos, LA CARA, la abominación
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawCongregacion();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // --- LA PALETA (v6.30 — MEDIDA del sprite Spirit_Congregation de
        //     la referencia: 45.8% NEGRO + 26.4% violeta oscuro (32,0,32) + 10.4%
        //     rojo oscuro; las CARAS arden ROJO-NARANJA (253,74,60) DENTRO
        //     de la masa negra; acento pálido (192,224,224) 0.6%) ---
        private static readonly Color MasaNegra = new(24, 8, 34);        // el cuerpo de la congregación
        private static readonly Color MasaNucleo = new(10, 3, 16);       // el corazón negro de la masa
        private static readonly Color EspirituOscuro = new(40, 16, 52);  // los espíritus menores
        private static readonly Color RojoCara = new(253, 74, 60);       // las caras que ARDEN (medido)
        private static readonly Color NaranjaCara = new(255, 130, 70);   // las brasas calientes
        private static readonly Color BrasaBase = new(122, 28, 18);      // la base del ojo (alfa)
        private static readonly Color PálidoRasgo = new(192, 224, 224);  // el acento pálido (0.6%)
        private static readonly Color NegroRasgo = new(5, 2, 9);

        private void DrawCongregacion()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                float growth = _crecimiento;
                bool abominacion = growth >= 1f;

                // El desvanecido final (la disipación).
                float fade = MathHelper.Clamp(Projectile.timeLeft / 90f, 0f, 1f);

                // EL RADIO VIVO: crece con la acumulación; la abominación SE APRIETA.
                float respira = 1f + 0.06f * (float)Math.Sin(time * 0.65f);
                float radio = (44f + 26f * growth) * (abominacion ? 0.85f : 1f) * respira;
                Vector2 center = Projectile.Center - Main.screenPosition;

                // ============================================================
                //  1. EL PASE ALFA — LA MASA NEGRA DE VERDAD (v6.30: la
                //  congregación medida es NEGRA/VIOLETA OSCURA —
                //  NO pálida) + los zócalos + las brasas base de los ojos
                // ============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // LA MASA NEGRA (el cuerpo de la congregación — silueta que
                // se lee sobre CUALQUIER fondo: worldLit false, se autodefine).
                BrumaFX.Cloud(center, radio, MasaNegra, seed + 5, time,
                    puffs: 6 + (int)(3 * growth), alpha: 0.52f * fade, worldLit: false);

                // EL CORAZÓN NEGRO (la profundidad del vacío — más denso al centro).
                BrumaFX.Cloud(center - new Vector2(0f, 6f), radio * 0.62f, MasaNucleo,
                    seed + 41, time * 1.2f,
                    puffs: 4 + (int)(2 * growth), alpha: 0.58f * fade, worldLit: false);

                // LOS ZÓCALOS + LAS BRASAS BASE (los huecos donde arden los ojos).
                DibujarZocalos(center, radio, time, seed, growth, fade, abominacion);

                Main.spriteBatch.End();

                // ============================================================
                //  2. LO QUE ARDE (pase aditivo) — las CARAS ROJO-NARANJA
                //     brillando DENTRO de la masa negra (el 5.2% (253,74,60)
                //     del sprite real) + las estelas
                // ============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // --- EL RESPLANDOR DE LA MASA (rojo profundo que late — la
                //     congregación vista desde fuera ARDE por dentro) ---
                LumenLib.BloomPulse(Main.spriteBatch, center, radio * 0.8f,
                    RojoCara, (0.07f + 0.08f * growth) * fade, time, 0.9f, 2);

                // --- LOS ESPÍRITUS MENORES (sus OJOS rojos + estelas mientras
                //     se liberan y son tirados de vuelta) ---
                DibujarEspiritus(center, radio, time, seed, growth, fade);

                // --- LOS OJOS QUE ARDEN (el rojo-naranja medido) ---
                DibujarOjosBrillantes(center, radio, time, seed, growth, fade, abominacion);

                // --- LA ESTELA DE LA ABOMINACIÓN (el rastro carmesí del monstruo) ---
                if (abominacion)
                {
                    var camino = new Vector2[_estela.Length];
                    for (int i = 0; i < _estela.Length; i++)
                        camino[i] = _estela[i] - Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 26f,
                        EstelaProfile.Comet, RojoCara, 0.45f * fade, seed + 3, time);
                }

                Main.spriteBatch.End();

                // ============================================================
                //  3. EL SEGUNDO PASE ALFA — LAS PUPILAS Y LA BOCA NEGRAS
                //     ENCIMA del brillo (la mirada de la abominación corta
                //     el propio fuego que la enciende)
                // ============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                DibujarPupilas(center, radio, time, seed, growth, fade, abominacion);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// LOS ESPÍRITUS MENORES — paramétricos y deterministas: cada uno
        /// tiene su ciclo (se LIBERA de la masa a +62 px, flota, y es TIRADO
        /// DE VUELTA con aceleración — el ciclo exacto). v6.30:
        /// sus OJOS arden rojo-naranja (el espíritu del sprite real es oscuro
        /// con brillos rojos — no un fantasma pálido).
        /// </summary>
        private void DibujarEspiritus(Vector2 center, float radio, float time,
            int seed, float growth, float fade)
        {
            int n = 3 + (int)(9f * growth);
            for (int i = 0; i < n; i++)
            {
                float h1 = VFXCore.Hash01(seed, 300 + i, 13);
                float h2 = VFXCore.Hash01(seed, 310 + i, 17);
                float h3 = VFXCore.Hash01(seed, 320 + i, 19);

                // EL CICLO: liberado → flotando → tirado de vuelta (periodo ~9 s).
                float rate = 0.09f + 0.07f * h3;
                float ph = (time * rate + h2) % 1f;
                float fuera;
                if (ph < 0.45f) fuera = Smooth(ph / 0.45f);                       // se libera
                else if (ph < 0.62f) fuera = 1f;                                  // flota
                else fuera = 1f - (float)Math.Pow((ph - 0.62f) / 0.38f, 1.6f);    // TIRADO de vuelta

                // La órbita propia de cada espíritu.
                float angVel = (h1 - 0.5f) * 0.8f;
                float ang = h2 * MathHelper.TwoPi + time * angVel + i * 1.3f;
                float radioEsp = radio * (0.55f + 0.50f * h1) + 62f * fuera;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * radioEsp,
                    (float)Math.Sin(ang) * radioEsp * 0.82f - 8f);

                // La respiración del espíritu.
                float pulso = 0.7f + 0.3f * (float)Math.Sin(time * 2.2f + i * 2.0f);

                // EL CUERPO OSCURO del espíritu (masa negra pequeña — pase alfa).
                Quad(DiscoNegro, pos, new Vector2(15f, 15f) * pulso, 0f,
                    Tint(EspirituOscuro, 0.55f * pulso * fade));

                // EL OJO QUE ARDE (rojo-naranja — el espíritu TE MIRA).
                Quad(VFXCore.GlowOrb, pos, new Vector2(5.6f, 5.6f) * pulso, 0f,
                    Tint(RojoCara, 0.85f * pulso * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(2.6f, 2.6f) * pulso, 0f,
                    Tint(NaranjaCara, 0.95f * pulso * fade));

                // LA ESTELITA del espíritu (los primeros 6: ribbon carmesí corto).
                if (i < 6 && fuera > 0.25f)
                {
                    var pts = new Vector2[5];
                    for (int k = 0; k < pts.Length; k++)
                    {
                        float t2 = time - k * 0.14f;
                        float ph2 = (t2 * rate + h2) % 1f;
                        float f2 = ph2 < 0.45f ? Smooth(ph2 / 0.45f)
                            : ph2 < 0.62f ? 1f
                            : 1f - (float)Math.Pow((ph2 - 0.62f) / 0.38f, 1.6f);
                        float a2 = h2 * MathHelper.TwoPi + t2 * angVel + i * 1.3f;
                        float r2 = radio * (0.55f + 0.50f * h1) + 62f * f2;
                        pts[k] = center + new Vector2(
                            (float)Math.Cos(a2) * r2, (float)Math.Sin(a2) * r2 * 0.82f - 8f);
                    }
                    EstelaLib.Ribbon(Main.spriteBatch, pts, 6f,
                        EstelaProfile.Head, RojoCara, 0.35f * fade, seed + 60 + i, time);
                }
            }
        }

        // ==================================================================
        //  LOS OJOS — el sistema de TRES pases (v6.30, la lección v6.29+)
        // ==================================================================

        /// <summary>LOS ZÓCALOS + BRASAS BASE (pase alfa 1): los huecos negros
        /// donde arden los ojos + la base de brasa rojo-oscuro.</summary>
        private void DibujarZocalos(Vector2 center, float radio, float time,
            int seed, float growth, float fade, bool abominacion)
        {
            // === LOS OJOS MENORES (asoman con la acumulación: 2 + 6·x) ===
            int n = 2 + (int)(6f * growth);
            for (int i = 0; i < n; i++)
            {
                float h1 = VFXCore.Hash01(seed, 500 + i, 23);
                float h2 = VFXCore.Hash01(seed, 510 + i, 29);

                // LA POSICIÓN (dentro de la masa, orbitando MUY lento).
                float ang = h1 * MathHelper.TwoPi + time * 0.05f * (h2 > 0.5f ? 1f : -1f);
                float dist = radio * (0.25f + 0.45f * h2);
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);

                // EL PARPADEO (cada ojo tiene sus ventanas).
                int ventana = (int)(time * 0.6f);
                bool abierto = VFXCore.Hash01(seed, 520 + i, ventana) > 0.14f;
                if (!abierto) continue;

                float tam = (6f + 4f * h2) * (abominacion ? 1.15f : 1f);

                // EL ZÓCALO NEGRO (el hueco donde vive el ojo — más oscuro
                // que la masa: el contraste mide la mirada).
                Quad(DiscoNegro, pos, new Vector2(tam * 2.6f, tam * 2.6f), 0f,
                    Tint(NegroRasgo, 0.85f * fade));
                // LA BRASA BASE (rojo-oscuro — el rescoldo bajo el brillo).
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 1.55f, tam * 1.55f), 0f,
                    Tint(BrasaBase, 0.85f * fade));
            }

            // === LA CARA (el interior caótico — zócalos de la cara grande) ===
            if (growth > 0.55f && !abominacion)
                DibujarZocaloCara(center, radio, time, seed, fade);

            // === EL ZÓCALO DEL OJO MAYOR de la abominación ===
            if (abominacion)
            {
                Vector2 pos = center - new Vector2(0f, 6f);
                float tam = 30f * (1f + 0.06f * (float)Math.Sin(time * 3.1f));
                Quad(DiscoNegro, pos, new Vector2(tam * 2.8f, tam * 2.8f), 0f,
                    Tint(NegroRasgo, 0.90f * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 1.8f, tam * 1.8f), 0f,
                    Tint(BrasaBase, 0.90f * fade));
            }
        }

        /// <summary>EL ZÓCALO DE LA CARA (la ventana caótica).</summary>
        private void DibujarZocaloCara(Vector2 center, float radio, float time, int seed, float fade)
        {
            // LA VENTANA: caótica — abre ~30% del tiempo.
            int ventana = (int)(time * 0.5f);
            float h = VFXCore.Hash01(seed, 900, ventana);
            if (h > 0.30f) return;
            float dentro = 0.30f - h;
            float alphaVentana = MathHelper.Clamp(dentro / 0.06f, 0f, 1f) * fade;

            // EL OJO INMENSO (desplazado, mirando sin mirar).
            Vector2 posOjo = center + new Vector2(radio * 0.12f, -radio * 0.10f);
            float tam = radio * 0.55f;
            Quad(DiscoNegro, posOjo, new Vector2(tam * 2.5f, tam * 2.5f), 0f,
                Tint(NegroRasgo, 0.80f * alphaVentana));
            Quad(VFXCore.GlowOrb, posOjo, new Vector2(tam * 1.5f, tam * 1.5f), 0f,
                Tint(BrasaBase, 0.85f * alphaVentana));
        }

        /// <summary>LOS OJOS QUE ARDEN (pase aditivo): el ROJO-NARANJA medido
        /// (253,74,60) brillando en los zócalos — el 5.2% del sprite real que
        /// define TODA la personalidad de la congregación.</summary>
        private void DibujarOjosBrillantes(Vector2 center, float radio, float time,
            int seed, float growth, float fade, bool abominacion)
        {
            int n = 2 + (int)(6f * growth);
            for (int i = 0; i < n; i++)
            {
                float h1 = VFXCore.Hash01(seed, 500 + i, 23);
                float h2 = VFXCore.Hash01(seed, 510 + i, 29);
                int ventana = (int)(time * 0.6f);
                if (VFXCore.Hash01(seed, 520 + i, ventana) <= 0.14f) continue;

                float ang = h1 * MathHelper.TwoPi + time * 0.05f * (h2 > 0.5f ? 1f : -1f);
                float dist = radio * (0.25f + 0.45f * h2);
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float tam = (6f + 4f * h2) * (abominacion ? 1.15f : 1f);

                // EL OJO QUE ARDE (rojo-naranja — el fuego de la mirada).
                float lat = 0.75f + 0.25f * (float)Math.Sin(time * 5.1f + i * 2.2f);
                Quad(VFXCore.SoftGlow, pos, new Vector2(tam * 2.1f, tam * 2.1f), 0f,
                    Tint(RojoCara, 0.38f * lat * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 1.05f, tam * 1.05f), 0f,
                    Tint(RojoCara, 0.80f * lat * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 0.5f, tam * 0.5f), 0f,
                    Tint(NaranjaCara, 0.95f * lat * fade));
            }

            // === LA CARA QUE ARDE (la ventana caótica) ===
            if (growth > 0.55f && !abominacion)
                DibujarCaraBrillante(center, radio, time, seed, fade);

            // === EL OJO MAYOR DE LA ABOMINACIÓN (el dominante) ===
            if (abominacion)
            {
                Vector2 pos = center - new Vector2(0f, 6f);
                float tam = 30f;
                float lat = 0.8f + 0.2f * (float)Math.Sin(time * 3.1f);

                // EL ARO CARMESÍ del zócalo.
                Quad(VFXCore.Ring, pos, new Vector2(tam * 3.6f, tam * 3.6f), 0f,
                    Tint(RojoCara, 0.22f * fade));
                // EL OJO QUE ARDE.
                Quad(VFXCore.SoftGlow, pos, new Vector2(tam * 2.4f, tam * 2.4f), 0f,
                    Tint(RojoCara, 0.40f * lat * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 1.55f, tam * 1.55f), 0f,
                    Tint(RojoCara, 0.85f * lat * fade));
                Quad(VFXCore.GlowOrb, pos, new Vector2(tam * 0.8f, tam * 0.8f), 0f,
                    Tint(NaranjaCara, 0.95f * lat * fade));
                // EL DESTELLO PÁLIDO (el acento 0.6% del sprite real — la
                // chispa fría arriba-izquierda del ojo: la vida que le queda).
                Quad(VFXCore.SoftGlow, pos - new Vector2(tam * 0.55f, tam * 0.55f),
                    new Vector2(tam * 0.30f, tam * 0.30f), 0f,
                    Tint(PálidoRasgo, 0.75f * lat * fade));
            }
        }

        /// <summary>LA CARA QUE ARDE (aditivo — la ventana caótica).</summary>
        private void DibujarCaraBrillante(Vector2 center, float radio, float time, int seed, float fade)
        {
            int ventana = (int)(time * 0.5f);
            float h = VFXCore.Hash01(seed, 900, ventana);
            if (h > 0.30f) return;
            float dentro = 0.30f - h;
            float alphaVentana = MathHelper.Clamp(dentro / 0.06f, 0f, 1f) * fade;

            // EL OJO INMENSO ardiendo.
            Vector2 posOjo = center + new Vector2(radio * 0.12f, -radio * 0.10f);
            float tam = radio * 0.55f;
            Quad(VFXCore.SoftGlow, posOjo, new Vector2(tam * 2.0f, tam * 2.0f), 0f,
                Tint(RojoCara, 0.35f * alphaVentana));
            Quad(VFXCore.GlowOrb, posOjo, new Vector2(tam * 1.25f, tam * 1.25f), 0f,
                Tint(RojoCara, 0.75f * alphaVentana));
            Quad(VFXCore.GlowOrb, posOjo, new Vector2(tam * 0.6f, tam * 0.6f), 0f,
                Tint(NaranjaCara, 0.90f * alphaVentana));

            // LAS BRASAS DE LA BOCA (la voluta abierta — el grito ardiendo).
            Vector2 posBoca = center + new Vector2(-radio * 0.06f, radio * 0.42f);
            float anchoBoca = radio * (0.55f + 0.10f * (float)Math.Sin(time * 2.3f));
            for (int k = 0; k < 5; k++)
            {
                float t = k / 4f;
                Vector2 p = posBoca + new Vector2(
                    (t - 0.5f) * anchoBoca,
                    4f * (float)Math.Sin(t * MathHelper.Pi));
                Quad(VFXCore.GlowOrb, p, new Vector2(9f, 7f), 0f,
                    Tint(RojoCara, 0.45f * alphaVentana));
            }
        }

        /// <summary>LAS PUPILAS Y LA BOCA NEGRAS (pase alfa 2 — ENCIMA del
        /// brillo): la mirada corta el fuego. Solo los ojos GRANDES llevan
        /// pupila (los menores son pura brasa — como el sprite real).</summary>
        private void DibujarPupilas(Vector2 center, float radio, float time,
            int seed, float growth, float fade, bool abominacion)
        {
            // === LA PUPILA DE LA CARA (la ventana caótica) ===
            if (growth > 0.55f && !abominacion)
            {
                int ventana = (int)(time * 0.5f);
                float h = VFXCore.Hash01(seed, 900, ventana);
                if (h <= 0.30f)
                {
                    float dentro = 0.30f - h;
                    float alphaVentana = MathHelper.Clamp(dentro / 0.06f, 0f, 1f) * fade;
                    Vector2 posOjo = center + new Vector2(radio * 0.12f, -radio * 0.10f);
                    float tam = radio * 0.55f;
                    Vector2 pupila = posOjo + new Vector2(
                        0.10f * (float)Math.Sin(time * 1.7f),
                        0.07f * (float)Math.Cos(time * 1.3f)) * tam;
                    Quad(DiscoNegro, pupila, new Vector2(tam * 0.72f, tam * 0.72f), 0f,
                        Tint(NegroRasgo, 0.90f * alphaVentana));

                    // LA BOCA NEGRA (el grito de verdad — sobre las brasas).
                    Vector2 posBoca = center + new Vector2(-radio * 0.06f, radio * 0.42f);
                    float anchoBoca = radio * (0.55f + 0.10f * (float)Math.Sin(time * 2.3f));
                    var boca = new Vector2[5];
                    for (int k = 0; k < boca.Length; k++)
                    {
                        float t = k / (float)(boca.Length - 1);
                        boca[k] = posBoca + new Vector2(
                            (t - 0.5f) * anchoBoca,
                            4f * (float)Math.Sin(t * MathHelper.Pi));
                    }
                    for (int k = 0; k < boca.Length - 1; k++)
                    {
                        Vector2 a = boca[k], b = boca[k + 1];
                        Vector2 mid = (a + b) * 0.5f;
                        float len = (b - a).Length();
                        float rot = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
                        Quad(DiscoNegro, mid, new Vector2(len + 9f, 15f), rot,
                            Tint(NegroRasgo, 0.85f * alphaVentana));
                    }
                }
            }

            // === LA PUPILA DEL OJO MAYOR (siguiendo el vuelo — la abominación
            //     TE MIRA a donde va a ir) ===
            if (abominacion)
            {
                Vector2 dirV = Projectile.velocity.LengthSquared() > 0.01f
                    ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
                Vector2 pos = center - new Vector2(0f, 6f);
                float tam = 30f;
                Vector2 pupila = pos + dirV * tam * 0.40f;
                Quad(DiscoNegro, pupila, new Vector2(tam * 0.88f, tam * 0.88f), 0f,
                    Tint(NegroRasgo, 0.95f * fade));
            }
        }

        private static float Smooth(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------
        //  HELPERS DE DIBUJO (los de la casa)
        // ------------------------------------------------------------------

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
