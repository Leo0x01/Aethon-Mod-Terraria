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
    /// v5.97 — LA MEDUSA NEBULAR (minion invocador — el proyectil ES la
    /// invocación). Petición del usuario: "crea una nueva arma que sea un
    /// invocador para un minion, este minion debe ser algo que hayas creado,
    /// crea un proyectil super creativo y cosmico, y este proyectil sera la
    /// invocacion".
    ///
    /// LA CRIATURA: una medusa nacida en el corazón de una nebulosa — una
    /// campana translúcida de gas interestelar (teal-esmeralda, costillas
    /// radiales bioluminiscentes, margen rosa) que guarda dentro una
    /// MINIGALAXIA ESPIRAL que gira lentamente (JellyfishGalaxy) y seis
    /// tentáculos de cuentas estelares (JellyfishBead) con física de cuerda
    /// propia: anclas en el margen de la campana, gravedad suave, vaivén
    /// per-tentáculo y restricción de longitud — ondean CON VIDA PROPIA y
    /// quedan A LA ESTELA cuando la medusa nada.
    ///
    /// LOCOMOCIÓN POR PULSOS (nadie más en el arsenal se mueve así): NO hay
    /// vuelo continuo — cada 48 ticks la campana SE CONTRAE (squash visual,
    /// la galaxia interior destella, los tentáculos se recogen) y dispara un
    /// IMPULSO hacia su ancla; entre pulsos deriva con arrastre acuático
    /// (×0.955/tick) y un hundimiento sutil (siempre tiene por qué nadar).
    /// Exactamente como una medusa real en el océano — solo que el océano es
    /// el aire.
    ///
    /// EL ESPACIOTIEMPO: donde nada, el fondo se curva SUTILMENTE (fuente
    /// del pase B del BlackHoleLensSystem, fuerza 0.06→0.14 RESPIRANDO con
    /// el pulso — es un trozo de nebulosa con masa).
    ///
    /// ATAQUE — EL LÁTIGO ELÉCTRICO (v6.00): al contraer junto a una
    /// víctima (≤190 px) la medusa DESCARGA UN RAYO QUE SALE DE ELLA MISMA
    /// (NebulaLightning — petición del usuario: "el rayo debe salir de
    /// medusa no del cielo, y debe tener mas brillo") con QUEMADURA DE
    /// HIELO (Frostburn — la quemadura fría del vacío, firma que ningún
    /// otro arma cósmica usa) + daño de contacto de la campana. Al
    /// desvanecer se disuelve en polvo de estrellas.
    ///
    /// Ciclo de vida del sirviente: patrón CosmicOrb (buff NebulaJellyfishBuff
    /// sostenido por la propia medusa; muere sin él).
    /// </summary>
    public class NebulaJellyfishMinion : ModProjectile
    {
        // === ANATOMÍA ===
        private const int TentacleCount = 6;
        private const int SegmentsPerTentacle = 9;

        // === MOTOR DE PULSOS ===
        /// <summary>Ciclo completo de nado (ticks entre contracciones).</summary>
        private const int PulseCycle = 48;

        /// <summary>Impulso del pulso en reposo (suave — solo mantenerse).</summary>
        private const float IdleImpulse = 2.9f;

        /// <summary>Impulso del pulso atacando (nado de caza).</summary>
        private const float HuntImpulse = 6.6f;

        /// <summary>Distancia a la víctima para disparar nematocistos.</summary>
        private const float StingRange = 190f;

        // --- Estado de simulación local (visual: cada cliente lo simula igual
        // desde datos sincronizados; el daño/impulso van por la IA autoritaria) ---
        private Vector2[][] _tentacles;
        private float _swayTime;
        private float _galaxyRotation;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.aiStyle = -1;
            Projectile.light = 0.6f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => true;

        // ================================================================
        //  HELPERS ESTÁTICOS (lente + dibujado compartido)
        // ================================================================

        /// <summary>Radio visual de la campana (px): width·scale·0.5·1.32.</summary>
        internal static float GetBellVisualRadius(Projectile p)
        {
            return p.width * p.scale * 0.5f * 1.32f;
        }

        /// <summary>
        /// Contracción de la campana (0..1) — DETERMINISTA desde ai[0] (la
        /// edad sincronizada): aprieta rápido (6 ticks) y suelta lento. La
        /// usan la lente, el dibujado y (vía AI) el impulso: todas las
        /// máquinas ven el MISMO pulso.
        /// </summary>
        internal static float GetPulseContract(Projectile p)
        {
            int phase = ((int)(p.minionPos * 7f) % PulseCycle + PulseCycle) % PulseCycle;
            float cyc = ((int)p.ai[0] + phase) % PulseCycle;
            return PulseContractOf(cyc);
        }

        private static float PulseContractOf(float cyc)
        {
            if (cyc < 6f) return cyc / 6f;                  // apriete rápido
            return Math.Max(0f, 1f - (cyc - 6f) / 16f);     // liberación lenta
        }

        // ================================================================
        //  AI — EL MOTOR DE PULSOS
        // ================================================================

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            CheckMinionBuff(owner);

            float age = Projectile.ai[0];
            Projectile.ai[0] = age + 1f;
            float contract = GetPulseContract(Projectile);

            // === OBJETIVO (con el marcado por el ataque del jugador) ===
            NPC target = FindTarget(owner);

            // === ANCLA: el punto hacia el que NADA este pulso ===
            Vector2 anchor;
            float impulse;
            if (target != null)
            {
                // Caza: flota SOBRE la víctima (las medusas pican desde arriba).
                anchor = target.Center + new Vector2(0f, -46f);
                impulse = HuntImpulse;
            }
            else
            {
                // Reposo: cuelga del hombro del jugador, apilada con sus hermanas.
                anchor = owner.Center + new Vector2(
                    -42f - Projectile.minionPos * 24f,
                    -58f + (Projectile.minionPos % 3) * 16f);
                impulse = IdleImpulse;
            }

            // === EL PULSO: contracción → impulso → (nemalocistos si hay presa) ===
            int phase = ((int)(Projectile.minionPos * 7f) % PulseCycle + PulseCycle) % PulseCycle;
            int cycTick = ((int)age + phase) % PulseCycle;
            if (cycTick == 0)
            {
                Vector2 toAnchor = anchor - Projectile.Center;
                float dist = toAnchor.Length();
                if (dist > 4f)
                {
                    toAnchor /= dist;
                    // Más lejos → impulso más fuerte (viaje largo en menos pulsos).
                    float strength = MathHelper.Clamp(dist / 70f, 0.4f, 1.3f) * impulse;
                    Projectile.velocity += toAnchor * strength;
                }

                // EL LÁTIGO: la campana se contrae junto a la víctima →
                // sale el rayo DE LA MEDUSA hacia ella (solo la máquina
                // dueña, patrón del resto del arsenal).
                if (target != null && Projectile.owner == Main.myPlayer &&
                    Vector2.Distance(target.Center, Projectile.Center) < StingRange)
                {
                    FireLightningStrike(target);
                }
            }

            // === DERIVA ACUÁTICA: arrastre + hundimiento sutil ===
            Projectile.velocity *= 0.955f;
            Projectile.velocity.Y += 0.018f;

            // Velocidad tope (el impulso acumulado no la vuelve un cohete).
            float maxSpd = target != null ? 11.5f : 7f;
            float spd = Projectile.velocity.Length();
            if (spd > maxSpd)
                Projectile.velocity *= maxSpd / spd;

            // === NO PERDERSE: teletransporte al dueño si quedó atrás ===
            if (Vector2.Distance(Projectile.Center, owner.Center) > 1100f)
            {
                Projectile.Center = owner.Center - Vector2.UnitY * 42f;
                Projectile.velocity *= 0.2f;
                InitTentacles();
            }

            // === SIMULACIÓN VISUAL (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                UpdateTentacles(contract);
                _galaxyRotation += 0.012f + 0.02f * contract;

                // Polvo estelar ambiental: la nebulosa suelta materia al nadar.
                if (Main.rand.NextBool(7))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(
                            Main.rand.NextFloat(-24f, 24f),
                            Main.rand.NextFloat(-14f, 20f)),
                        DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -0.35f),
                        120, Main.rand.NextBool(2)
                            ? new Color(110, 235, 255)
                            : new Color(255, 170, 225), 0.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Destello del disparo: chispas alrededor de la campana al picar.
                if (cycTick >= 0 && cycTick < 3 && target != null &&
                    Vector2.Distance(target.Center, Projectile.Center) < StingRange + 40f &&
                    Main.rand.NextBool(2))
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                            GetBellVisualRadius(Projectile) * 0.9f,
                        DustID.BlueTorch,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 1.2f,
                        200, new Color(200, 245, 255), 0.55f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LUZ (la campana respira luz con el pulso) ===
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.30f, 0.72f, 0.62f) * (0.75f + 0.45f * contract));
        }

        // ================================================================
        //  TENTÁCULOS — física de cuerda propia
        // ================================================================

        private void InitTentacles()
        {
            _tentacles = new Vector2[TentacleCount][];
            for (int t = 0; t < TentacleCount; t++)
            {
                _tentacles[t] = new Vector2[SegmentsPerTentacle];
                for (int s = 0; s < SegmentsPerTentacle; s++)
                    _tentacles[t][s] = Projectile.Center + new Vector2(0f, 8f + s * 7f);
            }
            _swayTime = 0f;
        }

        private void UpdateTentacles(float contract)
        {
            if (_tentacles == null) { InitTentacles(); }
            _swayTime += 1f;

            float bellR = GetBellVisualRadius(Projectile);
            // Al contraer, las anclas se RECOGEN hacia el centro de la campana.
            float anchorR = bellR * (0.82f - 0.10f * contract);
            float segLen = 7.4f * (1f - 0.22f * contract);

            for (int t = 0; t < TentacleCount; t++)
            {
                // El abanico entero gira lentísimo + vaivén por tentáculo.
                float baseAng = MathHelper.TwoPi * (t + 0.5f) / TentacleCount +
                                0.13f * (float)Math.Sin(_swayTime * 0.031 + t * 1.7f);
                Vector2 anchor = Projectile.Center + new Vector2(
                    (float)Math.Cos(baseAng),
                    (float)Math.Sin(baseAng) * 0.9f) * anchorR;

                Vector2 prev = anchor;
                for (int s = 0; s < SegmentsPerTentacle; s++)
                {
                    Vector2 pos = _tentacles[t][s];

                    // Seguir al previo (más rígido cerca de la campana)…
                    Vector2 relaxed = prev + (pos - prev) * (0.82f - 0.045f * s);
                    // …gravedad propia del tentáculo…
                    relaxed.Y += 0.62f;
                    // …vaivén bioluminiscente individual…
                    relaxed.X += (float)Math.Sin(_swayTime * 0.045 + t * 2.3f + s * 0.55f) * 0.55f;

                    // Restricción de longitud (cuerda): distancia FIJA al previo.
                    Vector2 d = relaxed - prev;
                    float len = d.Length();
                    if (len > segLen)
                    {
                        d *= segLen / len;
                        relaxed = prev + d;
                    }
                    else if (len < segLen * 0.45f && len > 0.001f)
                    {
                        relaxed = prev + d * (segLen * 0.45f / len);
                    }

                    _tentacles[t][s] = relaxed;
                    prev = relaxed;
                }
            }
        }

        // ================================================================
        //  EL LÁTIGO ELÉCTRICO — el rayo SALE DE LA MEDUSA (v6.00)
        // ================================================================

        private void FireLightningStrike(NPC target)
        {
            // v6.00 — Petición del usuario: "el rayo debe salir de medusa no
            // del cielo, y debe tener mas brillo". Nace BAJO LA CAMPANA (la
            // "boca" de la medusa) y vuela RECTO hacia la víctima como un
            // látigo de plasma frío — el trazo dentado vive entre la campana
            // y el objetivo (más daño: el rayo es EL ataque de la medusa).
            int boltDamage = Math.Max(1, (int)(Projectile.damage * 1.0f));
            Vector2 origin = Projectile.Center + new Vector2(0f,
                GetBellVisualRadius(Projectile) * 0.45f);
            Vector2 toTarget = target.Center - origin;
            float dist = toTarget.Length();
            if (dist < 24f) return; // encima: el contacto de campana hace el resto
            toTarget /= dist;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), origin, toTarget * 14f,
                ModContent.ProjectileType<NebulaLightning>(),
                boltDamage, 1.5f, Projectile.owner,
                dist,                              // ai[0]: distancia al objetivo
                Main.rand.Next(1, 999999));        // ai[1]: semilla del zigzag

            if (Main.netMode != NetmodeID.Server)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12,
                    Projectile.Center);
        }

        // ================================================================
        //  OBJETIVO + BUFF (patrón del arsenal/CosmicOrb)
        // ================================================================

        private NPC FindTarget(Player owner)
        {
            // El objetivo marcado por el ataque del jugador manda.
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC marked = Main.npc[owner.MinionAttackTargetNPC];
                if (marked != null && marked.active && marked.CanBeChasedBy())
                    return marked;
            }

            NPC closest = null;
            float closestDist = 760f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage || npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = npc;
                }
            }
            return closest;
        }

        private void CheckMinionBuff(Player owner)
        {
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.NebulaJellyfishBuff>();
            bool hasBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (owner.buffType[i] == buffType && owner.buffTime[i] > 0)
                {
                    hasBuff = true;
                    break;
                }
            }
            if (!hasBuff) Projectile.Kill();
            else
            {
                owner.AddBuff(buffType, 18000);
                // Inmortal mientras el buff viva (semántica vanilla de minion:
                // el timeLeft nunca expira — solo muere si el buff se va).
                Projectile.timeLeft = 2;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // El cuerpo de la medusa también pica: quemadura fría del vacío.
            try { target.AddBuff(BuffID.Frostburn, 180); } catch { }
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)),
                    210, new Color(140, 240, 255), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Se disuelve en polvo de estrellas (solo cliente).
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 26; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(6f, 34f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.7f) * dist,
                    Main.rand.NextBool(2) ? DustID.BlueTorch : DustID.PinkTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * Main.rand.NextFloat(0.8f, 2.2f),
                    200, Main.rand.NextBool(2)
                        ? new Color(110, 235, 255)
                        : new Color(255, 170, 230), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — la medusa entera (reutilizable: pase del mundo + lente)
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // Con la lente activa el BlackHoleLensSystem la pinta ENCIMA de la
            // distorsión (igual que el sol y el ojo): su campana translúcida
            // jamás es deformada — solo curva lo que hay DETRÁS.
            if (BlackHoleLensSystem.LensActive)
                return false;

            if (DrawJellyfishVisuals(Projectile, true))
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja la medusa completa (protocolo del arsenal: devuelve true si
        /// tomó el spriteBatch y lo dejó CERRADO — el llamador del pase del
        /// mundo debe re-abrirlo; false = no lo tocó).
        /// Capas (de atrás hacia delante):
        ///   1. GLOW del océano aéreo (SoftGlow teal, respira con el pulso)
        ///   2. TENTÁCULOS — cuentas estelares tintadas (teal/rosa/menta),
        ///      de gruesas a finas, apagándose hacia la punta, MÁS BRILLANTES
        ///      al contraer (la descarga de la picadura recorre el tentáculo)
        ///   3. CAMPANA translúcida (AlphaBlend: la luz la ATRAVIESA) con
        ///      squash/stretch del pulso e inclinación hacia la deriva
        ///   4. AURA aditiva de la campana (la nebulosa emite)
        ///   5. MINIGALAXIA girando en el corazón, con destello al contraer
        /// </summary>
        internal static bool DrawJellyfishVisuals(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active) return false;
                var mp = p.ModProjectile as NebulaJellyfishMinion;
                if (mp == null) return false;

                float contract = GetPulseContract(p);
                float bellR = GetBellVisualRadius(p);
                Vector2 drawPos = p.Center - Main.screenPosition;

                Texture2D bellTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/JellyfishBell").Value;
                Texture2D galaxyTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/JellyfishGalaxy").Value;
                Texture2D beadTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/JellyfishBead").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                // Inclinación sutil hacia la deriva (una medusa no gira: se inclina).
                float tilt = MathHelper.Clamp(p.velocity.X * 0.006f, -0.14f, 0.14f);

                // Squash & stretch del pulso: se APLANA al contraer.
                float sy = 1f - 0.20f * contract;
                float sx = 1f + 0.09f * contract;

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. GLOW del océano aéreo (respira con el pulso) ===
                float glowA = 0.34f + 0.22f * contract;
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(60, 220, 190, (byte)(glowA * 255f)), 0f,
                    new Vector2(softGlow.Width * 0.5f, softGlow.Height * 0.5f),
                    (bellR * 1.9f) / (softGlow.Width * 0.5f), SpriteEffects.None, 0f);

                // === 2. TENTÁCULOS — cuentas estelares ===
                if (mp._tentacles != null)
                {
                    Vector2 beadOrigin = new Vector2(beadTex.Width * 0.5f, beadTex.Height * 0.5f);
                    for (int t = 0; t < TentacleCount; t++)
                    {
                        Color tint = TentacleTint(t);
                        for (int s = 0; s < SegmentsPerTentacle; s++)
                        {
                            Vector2 beadPos = mp._tentacles[t][s] - Main.screenPosition;
                            float ts = s / (float)(SegmentsPerTentacle - 1);
                            float sizePx = MathHelper.Lerp(7.6f, 2.6f, ts);
                            // La picadura recorre el tentáculo: brillo extra al contraer.
                            float alpha = (float)Math.Pow(1f - ts, 0.7f) *
                                          Math.Min(255f, 150f + 95f * contract);
                            Main.spriteBatch.Draw(beadTex, beadPos, null,
                                new Color(tint.R, tint.G, tint.B, (byte)alpha), 0f,
                                beadOrigin, sizePx / (beadTex.Width * 0.5f),
                                SpriteEffects.None, 0f);
                        }
                    }
                }

                // === 4. AURA aditiva de la campana (antes del cuerpo: queda por debajo) ===
                float auraA = 0.10f + 0.10f * contract;
                Main.spriteBatch.Draw(bellTex, drawPos, null,
                    new Color(120, 255, 225, (byte)(auraA * 255f)), tilt,
                    new Vector2(bellTex.Width * 0.5f, bellTex.Height * 0.5f),
                    new Vector2(bellR / (bellTex.Width * 0.5f) * 1.18f * sx,
                                bellR / (bellTex.Height * 0.5f) * 1.18f * sy),
                    SpriteEffects.None, 0f);

                // === 5. MINIGALAXIA girando en el corazón (destella al contraer) ===
                float galScale = (bellR * (0.62f + 0.10f * contract)) / (galaxyTex.Width * 0.5f);
                Main.spriteBatch.Draw(galaxyTex, drawPos + new Vector2(0f, -bellR * 0.12f), null,
                    new Color(255, 244, 214, (byte)Math.Min(255f, 165f + 90f * contract)),
                    mp._galaxyRotation,
                    new Vector2(galaxyTex.Width * 0.5f, galaxyTex.Height * 0.5f),
                    galScale, SpriteEffects.None, 0f);

                Main.spriteBatch.End();

                // === 3. CAMPANA translúcida (AlphaBlend: la luz la ATRAVIESA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(bellTex, drawPos, null,
                    Color.White * 0.94f, tilt,
                    new Vector2(bellTex.Width * 0.5f, bellTex.Height * 0.5f),
                    new Vector2(bellR / (bellTex.Width * 0.5f) * sx,
                                bellR / (bellTex.Height * 0.5f) * sy),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                return true; // batch tomado y dejado CERRADO
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                return true;
            }
        }

        /// <summary>Color de las cuentas de cada tentáculo (bioluminiscencia variada).</summary>
        private static Color TentacleTint(int t)
        {
            switch (t % 3)
            {
                case 0: return new Color(110, 235, 255); // teal
                case 1: return new Color(255, 160, 220); // rosa
                default: return new Color(150, 255, 205); // menta
            }
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
