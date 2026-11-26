using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;
using AethonMod.Content.VFX;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SupremoAuroraBlackHoleProjectile — v6.20 — EL AGUJERO NEGRO
    /// SUPREMO AURORA.
    ///
    /// PETICIÓN DEL USUARIO (v6.20): "en el agujero negro supremo, cambiar
    /// el color: centro NEGRO → MORADO cerca del centro → AZUL y DORADO
    /// en los bordes. Guardar una copia del original y crear uno nuevo".
    /// EL ORIGINAL QUEDA INTACTO: SupremoBlackHoleProjectile no se toca.
    ///
    /// La FÍSICA de juego es la MISMA copia probada del Supremo (pop
    /// elástico, atracción en 550px, aura con ticks acelerados, devora
    /// balas, persecución lenta, evaporación y anillo de Einstein final)
    /// — esfera de 55px (la más grande del mod) — SOLO cambia la PALETA
    /// de partículas, luz e impactos: el GRADIENTE AURORA.
    ///
    /// PALETA AURORA de partículas: MORADO (185,105,255) cerca del
    /// centro / AZUL (92,150,255) al medio / DORADO (255,195,90) en los
    /// bordes — con blanco frío (238,242,255) en los picos.
    /// </summary>
    public class SupremoAuroraBlackHoleProjectile : ModProjectile
    {
        /// <summary>Multiplicador del aura sobre la esfera visual (supremo).</summary>
        private const float ShieldRadiusMult = 4.8f;

        /// <summary>Multiplicador del radio de la lente gravitacional de pantalla.</summary>
        internal const float LensRadiusMult = 3.6f;

        /// <summary>Radio de atracción gravitacional (px) — EL MÁS GRANDE.</summary>
        private const float GravityRadius = 550f;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // Mismo hitbox que los agujeros hermanos.
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN (copia exacta) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // ================================================================
            //  SECUENCIA DE MUERTE (copia exacta: crecimiento → evaporación)
            // ================================================================
            float expansion = 0f;
            if (Projectile.timeLeft > 36f && Projectile.timeLeft <= 90f)
            {
                expansion = 1f - (Projectile.timeLeft - 36f) / 54f;
                Projectile.scale *= 1f + expansion * 0.6f;
            }
            else if (Projectile.timeLeft <= 36f)
            {
                if (Projectile.localAI[1] <= 0f)
                    Projectile.localAI[1] = 0.3f * Projectile.width *
                                            Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;

                float collapse = Utils.GetLerpValue(36f, 0f, Projectile.timeLeft, true);
                // Piso de escala (lección v6.10).
                Projectile.scale *= Math.Max(1f - collapse, 0.06f);
                expansion = 1f;
            }

            // La materia absorbida ACELERA hacia el centro al evaporarse.
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // === MOVIMIENTO: deriva lenta y frenado (copia exacta) ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (copia exacta) ===
            NPC prey = FindNearestEnemy(750f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 120f)
                {
                    toPrey.Normalize();
                    // v6.51.2 — EL SEGUIMIENTO QUE CRECE CON EL NIVEL DEL
                    //     GRIMORIO (la letra: simple y lento por defecto,
                    //     más agresivo desde el nivel 100, MUY agresivo a
                    //     nivel 1000).
                    Projectile.velocity += toPrey * 0.07f *
                        Content.Systems.WeaponScaling.HomingAcel(
                            Content.Systems.WeaponScaling.NivelDelDuenio(Projectile));
                }
                float spd = Projectile.velocity.Length();
                float topeH = 2.6f * Content.Systems.WeaponScaling.HomingTope(
                    Content.Systems.WeaponScaling.NivelDelDuenio(Projectile));
                if (spd > topeH)
                    Projectile.velocity *= topeH / spd;
            }

            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) — PALETA AURORA: morado cerca
            //     del centro / azul al medio / dorado en los bordes ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (550px — supremo) ===
            float gravityRadius = GravityRadius * (1f + expansion * 0.6f);
            const float gravityStrength = 2.6f;
            // v6.50.3 — FIX (la promesa del SunProjectile v6.50.2 estaba rota:
            // "la familia de agujeros gatea la misma succión así" — solo el SOL
            // la gateaba): el empuje de npc.velocity corre SOLO en la autoridad
            // (SP + server); en un cliente remoto el server corrige la posición
            // por netUpdate y no hay doble empuje divergente (el jitter de
            // réplicas que v6.50.2 curó en el sol, ahora en toda la familia).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > gravityRadius || dist < 5f) continue;
                    float strength = (1f - dist / gravityRadius) * gravityStrength;
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        npc.velocity += toCenter * strength;
                    }
                }
            }

            // === LA GRAVEDAD DOBLA Y DEVORA LOS PROYECTILES ENEMIGOS (copia exacta) ===
            if (Main.netMode != NetmodeID.Server)
            {
                float projGravityRadius = gravityRadius * 0.9f;
            }

            // === AURA DE DAÑO: TICKS QUE ACELERAN CERCA DEL CENTRO (copia
            //     exacta) — v6.50 — GolpeMotor: el cauce del motor (el golpe
            //     corre en el cliente dueño; el arrastre y la quemadura
            //     siguen siendo autoridad). ===
            if (VisualsTime > 0f)
            {
                float lifeProgress = MathHelper.Clamp(1f - Projectile.timeLeft / 600f, 0f, 1f);
                float auraRadius = ShieldRadius * (1.15f + 0.75f * lifeProgress);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (0.35f + 0.30f * lifeProgress)));
                int t = (int)VisualsTime;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;

                    float prox = MathHelper.Clamp(dist / auraRadius, 0f, 1f);
                    int interval = 6 + (int)(prox * 18f); // 6 (centro) → 24 (borde)
                    if ((t + npc.whoAmI) % interval != 0) continue;

                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 0f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 toCenter = Projectile.Center - npc.Center;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            npc.velocity += toCenter * 0.8f;
                        }
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 480); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SupremoAuroraBlackHoleProjectile", exRegistro); }
                    }
                }
            }

            // === ILUMINACIÓN PULSANTE — TRES puntos para el vórtice ===
            // EL GRADIENTE AURORA EN LA LUZ: el centro arde MORADO, el
            // polo superior AZUL y la base DORADA — el alba polar viva.
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Vector3 light = new Vector3(0.62f * pulse, 0.38f * pulse, 1.00f * pulse);
            Lighting.AddLight(Projectile.Center, light);
            float le = SupremoAuroraBlackHoleRenderer.SpherePx * Math.Max(Projectile.scale, 0.1f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, -le * 1.5f),
                new Vector3(0.35f * pulse, 0.55f * pulse, 1.00f * pulse) * 0.75f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, le * 1.7f),
                new Vector3(1.00f * pulse, 0.78f * pulse, 0.40f * pulse) * 0.75f);
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist (copia exacta).</summary>
        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>Radio actual del campo (px) — abraza la mitad interior del anillo.</summary>
        private float ShieldRadius
        {
            get
            {
                if (Projectile.localAI[1] > 4f)
                    return Projectile.localAI[1];
                return 0.3f * Projectile.width * Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS — PALETA AURORA: morado / azul / dorado
        // ------------------------------------------------------------------

        /// <summary>Materia devorada (dusts) cayendo en espiral desde el anillo.</summary>


        /// <summary>Chispas moradas/azules capturadas por el campo.</summary>


        /// <summary>Devora el polvo del ambiente en espiral.</summary>


        // ------------------------------------------------------------------
        //  PARTÍCULAS DE LA LIBRERÍA PROPIA (capa AboveLens)
        // ------------------------------------------------------------------

        /// <summary>Materia devorada: estelas TrailGlow cayendo en espiral.</summary>
        private void SpawnLibraryAbsorbedMatter()
        
        {
            // v6.150 — EL POLVITO GIRATORIO MURIÓ (la letra: «todos mantienen el polvito
            // raro que gira todo el tiempo»): estas estelas de
            // la librería (TrailGlow con ComponentFlag.Orbit,
            // girando SIEMPRE) eran el polvito de verdad — la
            // v6.149 mató el polvo de vanilla y dejó vivo ESTE.
            // El agujero es 100% shader: el tratamiento
            // aprobado, puro renderer.
            return;
        }

        /// <summary>Estelas orbitando en la banda del anillo de bandas
        /// (2.3..5.8× la esfera, elipse 1.24 del anillo suprem).</summary>
        private void SpawnLibraryAccretionDisk()
        
        {
            // v6.150 — EL POLVITO GIRATORIO MURIÓ (la letra: «todos mantienen el polvito
            // raro que gira todo el tiempo»): estas estelas de
            // la librería (TrailGlow con ComponentFlag.Orbit,
            // girando SIEMPRE) eran el polvito de verdad — la
            // v6.149 mató el polvo de vanilla y dejó vivo ESTE.
            // El agujero es 100% shader: el tratamiento
            // aprobado, puro renderer.
            return;
        }

        // ------------------------------------------------------------------
        //  RENDER — 100% CÓDIGO (SupremoAuroraBlackHoleRenderer v6.20)
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS: el BlackHoleLensSystem pinta el núcleo
            // ENCIMA de la distorsión llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            // CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DrawCoreVisuals(Projectile);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>
        /// Dibuja el Agujero Negro Supremo Aurora completo. Compartido entre
        /// el pase del mundo (PreDraw) y el pase posterior a la lente.
        /// CONTRATO: el SpriteBatch llega CERRADO y queda CERRADO.
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p)
        {
            Vector2 drawPos = p.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = p.whoAmI * 17 + 5;

            SupremoAuroraBlackHoleRenderer.Draw(drawPos, p.scale, time, seed);
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE (física exacta, PALETA AURORA morado/azul/dorado)
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(185, 105, 255), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(205, 180, 255, 170), 22);

            // Implosión: 50 partículas convergiendo en espiral

            // Explosión: 40 radiales

            // Destellos de la corona: morado, azul y dorado

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // LA EXPLOSIÓN ES EL ANILLO DE EINSTEIN (única, daño COMPLETO).
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleEinstein,
                    560f);
            }

            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 7f, 10, 20, 0.45f,
                    "AethonSupremoAuroraFinalBlast"));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SupremoAuroraBlackHoleProjectile", exRegistro); }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // Presets — el COLAPSO AURORA (morado + dorado + blanco frío)
            ParticlePresets.Implosion(Projectile.Center, 185f, 52,
                new Color(185, 105, 255), 28);
            ParticlePresets.Explosion(Projectile.Center, 145f, 30,
                new Color(238, 242, 255), new Color(255, 195, 90), 44);

            // Implosión: partículas convergiendo

            // Explosión: anillo expansivo (morado + azul + dorado)

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (curva elástica de aparición — copia exacta).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
