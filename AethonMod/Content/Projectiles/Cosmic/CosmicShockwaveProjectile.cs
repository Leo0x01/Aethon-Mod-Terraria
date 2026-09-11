using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CosmicShockwaveProjectile — onda expansiva con daño real por frente de onda.
    ///
    /// v5.92 — FIX del error del sol: "Begin has been called before calling
    /// End" (InvalidOperationException en PreDraw). Causa raíz: las ondas de
    /// fuego del sol nacen con retardo escalonado (edades -8/-16); en el tick
    /// EXACTO en que el retardo expira (edad 0) el frente mide 0 px y
    /// DrawWaveVisual devolvía SIN tocar el spriteBatch — pero el PreDraw
    /// restauraba el batch con un Begin INCONDICIONAL, re-abriendo el batch
    /// del juego YA ABIERTO. La excepción abortaba el dibujado de TODOS los
    /// proyectiles del frame (2 "Excepción silenciosa" por explosión del sol
    /// en el client.log). FIX: DrawWaveVisual ahora devuelve si tomó el batch
    /// (false = no lo tocó / true = lo dejó CERRADO) y el PreDraw solo
    /// restaura cuando corresponde.
    ///
    /// v5.88 — Fix del MissingResourceException: textura propia añadida
    /// (InvisiblePixel 1x1 — el dibujado es 100% manual vía DrawWaveVisual)
    /// y NewInstance con array de golpes fresco por onda.
    ///
    /// v5.90 — Los frentes de onda del arsenal cósmico:
    ///
    ///   ESTILO 0 — ONDA CROMÁTICA (LA explosión final del agujero negro):
    ///     Anillo RGB (aberración cromática real: los canales R/G/B se separan
    ///     radialmente) que se expande desde el centro. Se registra como fuente
    ///     del BlackHoleLensSystem → el FONDO del juego se distorsiona a su paso
    ///     ("distorsiona un poco"). Desde la v5.90 es la ÚNICA onda del agujero:
    ///     nace en OnKill (cuando el agujero termina de evaporarse) con el daño
    ///     COMPLETO del proyectil.
    ///     v5.91 — Daña A MEDIDA QUE AVANZA: cada NPC dentro de la BANDA del
    ///     frente recibe daño cada 0.1 s (6 ticks) mientras la onda lo barre.
    ///     v5.91 — TRANSPARENTE: los anillos RGB bajaron de alpha 230 → 140 y
    ///     el núcleo blanco de 150 → 95 (petición del usuario).
    ///
    ///   ESTILO 1 — ONDA CROMÁTICA INVERSA (LEGADO, sin uso desde v5.90):
    ///     Nace en el radio máximo y CONVERGE hacia el centro (el frente barre
    ///     el daño hacia dentro). El desfase de color está invertido (azul por
    ///     delante de rojo) y también distorsiona el fondo al pasar. Conservada
    ///     como parte del arsenal por si un arma futura la invoca (las 3 ondas
    ///     inversas de la implosión v5.86 se eliminaron a petición del usuario).
    ///
    ///   ESTILO 2 — ONDA DE FUEGO (nova final del sol / SupernovaStaff):
    ///     Triple anillo ardiente (rojo/naranja/amarillo) + llamas a lo largo
    ///     del frente. v5.91 — Daña A MEDIDA QUE AVANZA cada 0.1 s (6 ticks)
    ///     mientras el frente barre al enemigo, y aplica QUEMADURA de 10 s
    ///     (OnFire, 600 ticks — antes 5 s) a cada golpe.
    ///
    /// Campos AI:
    ///   ai[0] = edad (negativa = retardo escalonado aún activo)
    ///   ai[1] = estilo (0 cromática / 1 cromática inversa / 2 fuego)
    ///   ai[2] = radio máximo en píxeles
    ///   duración = derivada del radio (maxR/20 ticks ≈ frente de ~40 px/tick)
    ///   (la API de NewProjectile solo acepta 3 slots de ai: la duración se
    ///   deriva de forma determinista para que todas las máquinas coincidan)
    ///
    /// RENDER: los estilos 0/1 se dibujan ENCIMA de la lente gravitacional
    /// (el BlackHoleLensSystem los pinta tras compositar la distorsión), de
    /// modo que la lente nunca deforma sus propios anillos. El estilo 2 se
    /// dibuja en el pase normal del mundo.
    /// </summary>
    public class CosmicShockwaveProjectile : ModProjectile
    {
        /// <summary>Estilo: onda cromática expansiva.</summary>
        public const float StyleChromatic = 0f;

        /// <summary>Estilo: onda cromática inversa (convergente).</summary>
        public const float StyleChromaticInverse = 1f;

        /// <summary>Estilo: onda de fuego (con quemadura).</summary>
        public const float StyleFire = 2f;

        /// <summary>
        /// v5.91 — Intervalo de daño por tick: 6 ticks = 0.1 segundos EXACTOS
        /// (petición del usuario: "daño en area y daño por cada 0.1 segundo").
        /// SimpleStrikeNPC NO usa los immunity frames del NPC, así que cada
        /// tick de la banda registra su propio golpe limpio.
        /// </summary>
        private const int DamageTickInterval = 6;

        /// <summary>
        /// v5.91 — Próximo tick (edad de la onda) en el que cada NPC puede
        /// volver a recibir daño: mientras el frente de la onda lo BARRA, el
        /// enemigo recibe daño cada 0.1 s (antes era UN solo golpe por NPC en
        /// toda la vida de la onda — v5.90 usaba un bool[] de "ya golpeado").
        /// tML crea cada proyectil clonando el prototipo (MemberwiseClone):
        /// un array inicializado en el campo se COMPARTIRÍA entre todas las
        /// ondas simultáneas del mismo tipo. NewInstance le da a cada onda su
        /// propio array fresco.
        /// </summary>
        private int[] _nextHitAt = new int[Main.maxNPCs];

        /// <summary>
        /// v5.88/v5.91 — Cada proyectil nuevo recibe un array FRESCO de cooldowns.
        /// Sin esto, MemberwiseClone haría que todas las ondas activas del
        /// mismo tipo compartieran el MISMO array (bug de daño en cascada).
        /// </summary>
        public override ModProjectile NewInstance(Projectile entity)
        {
            CosmicShockwaveProjectile inst = (CosmicShockwaveProjectile)base.NewInstance(entity);
            inst._nextHitAt = new int[Main.maxNPCs];
            return inst;
        }

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Style => Projectile.ai[1];
        private float MaxRadius => Projectile.ai[2];

        /// <summary>Duración derivada del radio: frente de ~40 px/tick de pico.</summary>
        private float Duration => Math.Max(MaxRadius / 20f, 10f);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            // Daño manual por frente de onda (SimpleStrikeNPC): sin colisión vanilla.
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;

            // tML reutiliza instancias clonadas del prototipo: además del
            // array fresco de NewInstance, se limpia por si el clon se recicla.
            Array.Clear(_nextHitAt, 0, _nextHitAt.Length);
        }

        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        // ================================================================
        //  AI — frente de onda, daño y soporte visual
        // ================================================================
        public override void AI()
        {
            try
            {
                float age = Age;
                Age += 1f;

                // Retardo escalonado (ondas en secuencia): invisible e inofensiva.
                if (age < 0f)
                    return;

                float progress = MathHelper.Clamp(age / Math.Max(Duration, 1f), 0f, 1f);
                float front = FrontRadius(age, Style, MaxRadius);

                // === DAÑO POR FRENTE DE ONDA (solo autoridad) ===
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ApplyWaveDamage(front);

                // === SOPORTE VISUAL (solo cliente) ===
                if (Main.netMode != NetmodeID.Server)
                {
                    if (Style == StyleFire)
                        SpawnFireFrontDusts(front, progress);
                    else
                        SpawnChromaticFrontSparks(front, progress);
                }

                // === LUZ ===
                float alpha = WaveAlpha(age, Math.Max(Duration, 1f));
                if (Style == StyleFire)
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(1f, 0.55f, 0.2f) * 1.8f * alpha);
                else
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(0.6f, 0.5f, 1f) * 0.9f * alpha);

                // Frente completado → la onda se disipa.
                if (age >= Duration)
                    Projectile.Kill();
            }
            catch { }
        }

        // ================================================================
        //  FRENTE DE ONDA (compartido con el sistema de lente)
        // ================================================================

        /// <summary>Radio del frente en píxeles, o -1 si aún retrasada/inactiva.</summary>
        public static float GetFrontRadius(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return FrontRadius(age, p.ai[1], p.ai[2]);
        }

        /// <summary>Progreso 0..1 del frente (o -1 si retrasada).</summary>
        public static float GetProgress(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return MathHelper.Clamp(age / DurationOf(p.ai[2]), 0f, 1f);
        }

        /// <summary>Duración del frente derivada del radio máximo (determinista).</summary>
        private static float DurationOf(float maxR)
        {
            return Math.Max(maxR / 20f, 10f);
        }

        private static float FrontRadius(float age, float style, float maxR)
        {
            float duration = DurationOf(maxR);
            float p = MathHelper.Clamp(age / duration, 0f, 1f);
            if (style == StyleChromaticInverse)
            {
                // Convergencia acelerada: nace en maxR y colapsa hacia el centro.
                return maxR * (1f - p * p);
            }
            // Expansión ease-out: arranque veloz, frenado al final.
            return maxR * (1f - (1f - p) * (1f - p));
        }

        /// <summary>Envolvente de alpha: aparición rápida + desvanecimiento final.</summary>
        private static float WaveAlpha(float age, float duration)
        {
            float fadeIn = Utils.GetLerpValue(0f, duration * 0.15f, age, true);
            float fadeOut = 1f - Utils.GetLerpValue(duration * 0.7f, duration, age, true);
            return Math.Min(fadeIn, fadeOut);
        }

        // ================================================================
        //  DAÑO — v5.91: el frente BARRA a los NPC dañando cada 0.1 s
        // ================================================================
        // La zona activa es la BANDA del anillo visible (el frente de la onda
        // "a medida que avanza"): cualquier enemigo dentro de ella recibe
        // daño de área cada DamageTickInterval ticks (0.1 s exactos). La onda
        // expansiva nace en el centro (radio 0) y barre hacia fuera → todo
        // el área del disco queda cubierta; la convergente barre hacia dentro.
        private void ApplyWaveDamage(float front)
        {
            // Banda de daño = grosor del anillo visible que avanza.
            float bandInner, bandOuter;
            if (Style == StyleChromaticInverse)
            {
                // Convergente: el frente baja hacia el centro — la banda va
                // POR DELANTE del frente (entre el frente y el radio exterior).
                bandInner = front * 0.98f;
                bandOuter = front * 1.25f;
            }
            else
            {
                // Expansiva: la banda va POR DETRÁS del frente — coincide con
                // los anillos dibujados (front, 0.93·front, 0.86·front, 0.8·front
                // en fuego; fringe ±7 px en cromática) más un margen de barrido.
                bandInner = front * 0.72f;
                bandOuter = front * 1.02f;
            }
            if (bandOuter < 4f) return; // frente aún diminuto: nada que barrer

            int ageNow = (int)Age;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                // Cooldown de 0.1 s por NPC: "daño por cada 0.1 segundo".
                if (ageNow < _nextHitAt[i]) continue;
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.CanBeChasedBy()) continue;

                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bandInner || dist > bandOuter) continue;
                _nextHitAt[i] = ageNow + DamageTickInterval;

                // Dirección del empuje: hacia fuera en expansivas, hacia el
                // centro en la inversa (la implosión arrastra hacia dentro).
                int dir;
                float knockBack;
                if (Style == StyleChromaticInverse)
                {
                    dir = npc.Center.X < Projectile.Center.X ? 1 : -1;
                    knockBack = -4f;
                }
                else
                {
                    dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                    knockBack = Style == StyleFire ? 5f : 6f;
                }

                npc.SimpleStrikeNPC(Projectile.damage, dir, false, knockBack, DamageClass.Magic);

                // La onda de fuego aplica QUEMADURA de 10 s (v5.91: era 5 s).
                if (Style == StyleFire)
                    npc.AddBuff(BuffID.OnFire, 600);
            }
        }

        // ================================================================
        //  DUSTS DE APOYO
        // ================================================================

        /// <summary>LLamas vivas a lo largo del frente de la onda de fuego.</summary>
        private void SpawnFireFrontDusts(float front, float progress)
        {
            int count = 4;
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * front,
                    (float)Math.Sin(angle) * front);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(1.5f, 3.5f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(1.5f, 3.5f));
                Color color = Main.rand.NextBool(2)
                    ? new Color(255, 170, 60)
                    : new Color(255, 100, 30);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 220, color, 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Chispas blancas escasas sobre el frente cromático.</summary>
        private void SpawnChromaticFrontSparks(float front, float progress)
        {
            if (!Main.rand.NextBool(2)) return;
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + new Vector2(
                (float)Math.Cos(angle) * front,
                (float)Math.Sin(angle) * front);
            Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                Vector2.Zero, 200, new Color(230, 220, 255), 0.6f);
            d.noGravity = true;
            d.fadeIn = 0.2f;
        }

        // ================================================================
        //  RENDER
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            // Retardo escalonado (ondas en secuencia): invisible e inofensiva.
            if (Age < 0f) return false;

            // Las ondas cromáticas las pinta el sistema de lente ENCIMA de la
            // distorsión (para que la lente no las deforme a ellas). Si la
            // lente no está activa, caemos al dibujado normal del mundo.
            if ((Style == StyleChromatic || Style == StyleChromaticInverse) &&
                BlackHoleLensSystem.LensActive)
                return false;

            // v5.92 — FIX del error del sol ("Begin has been called before
            // calling End"): DrawWaveVisual SOLO toma el spriteBatch cuando
            // el frente ya es visible (front > 1 px). En el tick EXACTO en que
            // expira el retardo escalonado (edad 0 → frente 0 px) devolvía sin
            // tocar el batch, y el Begin de restauración INCONDICIONAL de la
            // v5.91 re-abría el batch del juego YA ABIERTO →
            // InvalidOperationException que abortaba el dibujado de TODOS los
            // proyectiles del frame ("Excepción silenciosa" ×2 por explosión
            // del sol: sus ondas de fuego nacen con retardos de -8 y -16
            // ticks; la onda cromática del agujero nace SIN retardo y por eso
            // nunca lo disparó). Ahora restauramos SOLO si la onda tomó el
            // batch (y lo dejó CERRADO); si no, el batch sigue exactamente
            // como tML lo dejó → nada que hacer.
            if (DrawWaveVisual(Projectile, true))
            {
                // Restaurar el SpriteBatch al estado que tML espera tras
                // PreDraw: el path de dibujado deja el batch CERRADO, basta
                // con re-abrirlo.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }

        /// <summary>
        /// Dibuja la onda completa (anillos con aberración cromática o triple
        /// anillo de fuego). Reutilizable desde el pase del mundo (PreDraw) y
        /// desde el pase posterior a la lente (BlackHoleLensSystem).
        /// <param name="endActiveBatch">true cuando existe un Begin del juego
        /// activo (pase del mundo); false en el hook de la lente (batch cerrado).</param>
        /// v5.92 — Ahora devuelve SI tomó el spriteBatch:
        ///   false = NO lo tocó (onda inactiva o frente aún invisible, p.ej.
        ///           edad 0 — el tick exacto en que expira el retardo
        ///           escalonado): el llamador NO debe restaurar nada;
        ///   true  = lo tomó y lo dejó CERRADO (End propio, o el defensivo
        ///           del catch): el llamador debe re-abrirlo con los
        ///           parámetros estándar de tML.
        /// (Antes devolvía void y el PreDraw restauraba el batch SIN SABER si
        /// esta función lo había tocado — la causa raíz del error del sol.)
        /// </summary>
        public static bool DrawWaveVisual(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active || p.ai[0] < 0f) return false;

                float age = p.ai[0];
                float style = p.ai[1];
                float duration = DurationOf(p.ai[2]);
                float front = FrontRadius(age, style, p.ai[2]);

                // v5.92 — Frente aún invisible (front <= 1 px): NO tocamos el
                // spriteBatch — el batch sigue exactamente como el llamador lo
                // dejó (abierto en el pase del mundo / cerrado en la lente).
                // Devolver void aquí dejaba el PreDraw creyendo que lo habíamos
                // cerrado → su Begin de restauración re-abría un batch YA
                // ABIERTO ("Begin has been called before calling End").
                if (front <= 1f) return false;

                float progress = MathHelper.Clamp(age / duration, 0f, 1f);
                float alpha = WaveAlpha(age, duration);

                // v5.93 — Texturas de ALTA CALIDAD (1024px): el Ring.png de
                // 64px se pixelaba al escalarlo al radio de la onda (hasta
                // 620px) y su anillo fino teñido se veía BLANCO plano.
                //   ring (fino)      — anillo blanco nítido (frentes de choque
                //                      y franjas de aberración cromática)
                //   ringShieldNebula — cuerpo de campo de fuerza CON COLOR
                //                      horneado (interior rosa → magenta →
                //                      borde cian, el escudo del Nebula Pillar)
                //   fireRing         — llamas con COLOR propio (blanco-amarillo
                //                      → naranja → rojo en las puntas)
                Texture2D ring = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D nebulaBody = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/RingShieldNebula").Value;
                Texture2D fireRing = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/FireRing").Value;
                Vector2 drawPos = p.Center - Main.screenPosition;
                float ringUnit = ring.Width / 2f;
                float nebUnit = nebulaBody.Width / 2f;
                float fireUnit = fireRing.Width / 2f;
                // El núcleo del Ring fino vive a 0.92 del radio de su textura:
                // factor de compensación para que un radio pedido R aparezca a R.
                float thinComp = 1f / 0.92f;

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (style == StyleFire)
                {
                    // === ANILLO DE FUEGO (v5.93: FireRing.png con llamas reales) ===
                    // La textura trae el COLOR propio (núcleo blanco-amarillo
                    // incandescente → naranja → ROJO en las puntas de las
                    // lengüetas): dos pasadas de distinta escala dan cuerpo y
                    // profundidad, y el Ring fino blanco marca el frente de
                    // choque caliente. La FireRing lleva su núcleo a 0.80 del
                    // radio de la textura (fix del corte de borde) → las
                    // escalas ×1.08/×0.93 cubren la banda de daño 0.72-1.02·front.
                    DrawRing(fireRing, drawPos, front * 1.08f, fireUnit,
                        new Color(255, 255, 255, (byte)(alpha * 215f)));
                    DrawRing(fireRing, drawPos, front * 0.93f, fireUnit,
                        new Color(255, 225, 170, (byte)(alpha * 195f)));
                    DrawRing(ring, drawPos, front * 0.97f * thinComp, ringUnit,
                        new Color(255, 250, 230, (byte)(alpha * 140f)));
                }
                else
                {
                    // === CAMPO DE FUERZA CROMÁTICO (v5.93 — estilo Columna de Nebulosa) ===
                    // La onda ES el campo de fuerza del agujero expandiéndose
                    // al destruirse (petición del usuario). Look validado con
                    // simulación: CUERPO translúcido magenta→cian (color horneado
                    // en RingShieldNebula — 1 pasada, sin lavado a blanco) + TRES
                    // AROS FINOS R/G/B con desfase radial = la aberración
                    // cromática del escudo del Nebula Pillar, SEPARADA y VISIBLE.
                    // La separación crece con la edad (dispersión real: el frente
                    // al desvanecerse dispersa más) y se INVIERTE en la onda
                    // convergente legada. Transparente: alphas moderados.
                    float fringe = front * (0.035f + 0.06f * progress) *
                                   (style == StyleChromaticInverse ? -1f : 1f);

                    // Cuerpo del campo (tenue, se desvanece con la envolvente).
                    DrawRing(nebulaBody, drawPos, front, nebUnit,
                        new Color(255, 255, 255, (byte)(alpha * 145f)));
                    // Franja ROJA exterior.
                    DrawRing(ring, drawPos, (front + fringe) * thinComp, ringUnit,
                        new Color(255, 60, 70, (byte)(alpha * 175f)));
                    // Franja VERDE al centro.
                    DrawRing(ring, drawPos, front * thinComp, ringUnit,
                        new Color(80, 255, 135, (byte)(alpha * 140f)));
                    // Franja AZUL interior.
                    DrawRing(ring, drawPos, (front - fringe) * thinComp, ringUnit,
                        new Color(75, 155, 255, (byte)(alpha * 175f)));
                }

                Main.spriteBatch.End();
                return true; // batch tomado y dejado CERRADO
            }
            catch
            {
                // v5.89/v5.92 — cierre defensivo solo si una excepción cortó el
                // Begin. Tras este End el batch queda CERRADO en TODOS los casos
                // (Begin interrumpido → lo cerramos; ya cerrado → el End lanza
                // y se ignora) → devolvemos true para que el llamador lo
                // re-abra con los parámetros estándar de tML.
                try { Main.spriteBatch.End(); } catch { }
                return true;
            }
        }

        /// <summary>Dibuja un anillo centrado en drawPos con el radio dado en píxeles.</summary>
        private static void DrawRing(Texture2D ring, Vector2 drawPos, float radiusPx,
            float ringUnit, Color color)
        {
            if (radiusPx <= 0.5f || color.A == 0) return;
            float scale = radiusPx / ringUnit;
            Main.spriteBatch.Draw(ring, drawPos, null, color, 0f,
                new Vector2(ring.Width * 0.5f, ring.Height * 0.5f), scale,
                SpriteEffects.None, 0f);
        }
    }
}
