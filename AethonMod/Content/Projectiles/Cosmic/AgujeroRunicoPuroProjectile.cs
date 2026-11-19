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
    /// CosmicBlackHoleProjectile — v6.16 — EL AGUJERO NEGRO CÓSMICO.
    ///
    /// NACIDO DEL SCRIPT UNITY DEL USUARIO (CosmicBlackHole.cs): anillo
    /// energético MAGENTA (1.0, 0.2, 0.8) con las 20 BANDAS del shader
    /// sin(uv·20 + t·5) recorriéndolo a 5 rad/s, rotación de 20°/s,
    /// rayos eléctricos (lightningParticles), runas doradas flotando
    /// (runeParticles), distorsión sinusoidal global (0.3) y núcleo
    /// negro absoluto — más TODO lo que faltaba (aura oscura, nebulosas,
    /// ecos del anillo, corredores de fotones, destellos polares, ondas
    /// de distorsión y partículas radiales). 100% CÓDIGO, cero sprites.
    ///
    /// La FÍSICA de juego es la MISMA copia probada del agujero del
    /// olvido (pop elástico, atracción, aura con ticks acelerados,
    /// devora balas, persecución lenta, evaporación y anillo de Einstein
    /// final). Los demás agujeros quedan INTACTOS.
    ///
    /// ESCALA: esfera de 50px de radio, arte de ~7R de envergadura.
    /// </summary>
    public class AgujeroRunicoPuroProjectile : ModProjectile
    {
        /// <summary>Multiplicador del aura sobre la esfera visual.</summary>
        private const float ShieldRadiusMult = 4.6f;

        /// <summary>Multiplicador del radio de la lente gravitacional de pantalla.</summary>
        internal const float LensRadiusMult = 3.4f;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // Mismo hitbox que los agujeros hermanos (la VISUAL gigante es
            // independiente del hitbox: la escala del renderer no depende
            // de width).
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
                // Piso de escala (lección v6.10: el colapso jamás deja la
                // escala en ~0 — evita el crash del doble Begin).
                Projectile.scale *= Math.Max(1f - collapse, 0.06f);
                expansion = 1f;
            }

            // v6.154 — EL BOOST GLOBAL MUERE (la letra: «cada vez que
            // un agujero runico puro explota, hace que el resto
            // desaparezca en el instante de la explocion y luego se
            // vuelven a ver»): TODOS los agujeros compartían la
            // estática PullToGlobalBoost del ParticleManager — al
            // morir uno, el BOOST GLOBAL saltaba de 6× a 1× y la
            // materia orbitante de TODOS LOS DEMÁS se DESPLOMABA un
            // instante hacia sus centros y se recuperaba: el
            // parpadeo. LA CURA: ESTE agujero ya NO ESCRIBE el global
            // (ni en AI ni en OnKill) — la aceleración de la
            // evaporación es asunto de cada agujero, no del mundo.
            // (La secuencia de expansión visual del proyectil SIGUE:
            // el pop elástico de escala de arriba es la lectura de la
            // fase — lo que muere es el tirón GLOBAL ajeno.)

            // === MOVIMIENTO: deriva lenta y frenado (copia exacta) ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (copia exacta) ===
            NPC prey = FindNearestEnemy(650f);
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
                float topeH = 2.4f * Content.Systems.WeaponScaling.HomingTope(
                    Content.Systems.WeaponScaling.NivelDelDuenio(Projectile));
                if (spd > topeH)
                    Projectile.velocity *= topeH / spd;
            }

            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) — paleta del cósmico (magenta
            //     eléctrico + violeta + blanco-rosa + destellos dorados) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // v6.146 — CERO PARTÍCULAS (la letra: «los agujeros
                // negros runicos todos tienen unas especies de
                // particulas que le dan vueltan o van hacia el centro,
                // quiero que crees un agujero negro runico sin esas
                // particulas y que las sustituyas por algo que haga que
                // se vea mejor»): LAS CUATRO FAMILIAS MUEREN (polvo
                // absorbido, chispas, materia, disco de acreción). EL
                // REEMPLAZO: EL ANILLO DE RUNAS VIVAS — doce glifos
                // rúnicos que orbitan el horizonte en contrarrotação,
                // CADA UNO respirando su pulso (la runa se enciende al
                // pasar por el frente y se enfría por detrás — el
                // lenguaje rúnico de la casa, NADA de polvo).
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (copia exacta) ===
            float gravityRadius = 450f * (1f + expansion * 0.6f);
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

            // === AURA DE DAÑO: TICKS QUE ACELERAN CERCA DEL CENTRO (copia exacta) ===
            // v6.50 — el daño del aura va por GolpeMotor (el cauce del
            // motor); el exprimido y la quemadura siguen en el servidor.
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

                    // Lógica de servidor: el exprimido hacia el centro + quemadura.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 toCenter = Projectile.Center - npc.Center;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            npc.velocity += toCenter * 0.8f;
                        }
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 240); } catch { }
                    }
                    // v6.50 — GolpeMotor (el cauce del motor: crítica real,
                    // varianza, on-hit y sync MP del propio motor).
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 0f, true);
                }
            }

            // === ILUMINACIÓN PULSANTE — TRES puntos para el vórtice ===
            // Magenta eléctrico con un toque violeta (la estirpe cósmica).
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Vector3 light = new Vector3(1.00f * pulse, 0.24f * pulse, 0.82f * pulse);
            Lighting.AddLight(Projectile.Center, light);
            float le = AgujeroPuroRenderer.SpherePx * Math.Max(Projectile.scale, 0.1f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, -le * 1.5f), light * 0.55f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, le * 1.7f), light * 0.75f);
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
        //  PARTÍCULAS — paleta del cósmico: magenta eléctrico / violeta /
        //  blanco-rosa + destellos dorados.
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida (dusts) cayendo en espiral desde el anillo.</summary>


        /// <summary>Chispas rosas encantadas capturadas por el campo.</summary>


        /// <summary>Devora el polvo del ambiente en espiral.</summary>


        // ------------------------------------------------------------------
        //  PARTÍCULAS DE LA LIBRERÍA PROPIA (capa AboveLens)
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida: estelas TrailGlow cayendo en espiral.</summary>
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

        /// <summary>Estelas orbitando en la banda del anillo energético
        /// (2.3..5.8× la esfera, elipse del vórtice 1.18).</summary>
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
        //  RENDER — 100% CÓDIGO (AgujeroPuroRenderer v6.149 — el anillo de Einstein rúnico)
        // ------------------------------------------------------------------

        // ================================================================
        //  v6.152 — LAS CUATRO CAPAS NUEVAS DEL PURO (la letra: «mas
        //  vfx mas efectos visuales, mas de todo, que se vea super
        //  cargado, pero que siga siendo un agujero runico»):
        //   (a) EL TORBELLINO DE GLIFOS — 24 runas solares ESPIRALANDO
        //       hacia el horizonte (contrarrotación, tamaño creciendo
        //       al caer — la materia escrita siendo comida).
        //   (b) LA CORONA DE LENTEJUELAS — 12 destellos de 4 puntas
        //       orbitando EL BORDE de Einstein (el límite donde la luz
        //       se dobla — cada lentejuela con SU titileo).
        //   (c) LOS FILAMENTOS DE ACRECIÓN — 6 arcos eléctricos
        //       (ruido determinista) lamiendo el disco desde afuera
        //       hacia adentro (la cena en vivo).
        //   (d) EL ECO DOBLE — dos ondas concéntricas que respiran con
        //       el latido del corazón (la firma de la casa, ahora ×2).
        //  TODO determinista (Hash01 — R5) y por el lote aditivo.
        // ================================================================
        private void DibujarLasCapasNuevas(Vector2 centro, float t, float escala)
        {
            VFXCore.AbrirAdditive();
            try
            {
                Vector2 c = centro - Main.screenPosition;
                float radio = Projectile.width * 0.5f * escala;

                // === (a) EL TORBELLINO DE GLIFOS ===
                for (int i = 0; i < 24; i++)
                {
                    float h0 = VFXCore.Hash01(i, 1, 6101);
                    float h1 = VFXCore.Hash01(i, 2, 6103);
                    float caida = Frac01(t * (0.10f + 0.08f * h0) + h1);   // 0→1 cayendo
                    float ang = h1 * MathHelper.TwoPi - t * (0.9f + 0.6f * h0) + caida * 7f;
                    float r = MathHelper.Lerp(radio * 4.2f, radio * 1.15f, caida * caida);
                    Vector2 g = c + ang.ToRotationVector2() * r;
                    float tam = (7f + 10f * h0) * (0.5f + 0.8f * caida);
                    Color tinta = i % 3 == 0
                        ? new Color(150, 210, 255)
                        : new Color(255, 214, 130);
                    VFXCore.Quad(g, SigiloLib.Tint(tinta, 0.55f * (1f - caida * 0.4f)),
                        new Vector2(tam, tam * 0.4f), ang + MathHelper.PiOver2);
                }

                // === (b) LA CORONA DE LENTEJUELAS (destellos de 4 puntas) ===
                Texture2D dest = VFXCore.DestelloFinal;
                if (dest != null)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float h0 = VFXCore.Hash01(i, 3, 6111);
                        float titila = 0.5f + 0.5f * MathF.Sin(t * (2.2f + 1.3f * h0) + i * 2.1f);
                        float ang = h0 * MathHelper.TwoPi + t * 0.35f;
                        float rB = radio * (3.4f + 0.10f * MathF.Sin(t * 1.3f + i));
                        Vector2 l = c + ang.ToRotationVector2() * rB;
                        float esc = (26f + 20f * h0) * (0.55f + 0.45f * titila) / dest.Width;
                        Main.spriteBatch.Draw(dest, l, null,
                            SigiloLib.Tint(new Color(200, 235, 255), 0.75f * titila),
                            t * 0.4f + i, dest.Size() * 0.5f, esc,
                            SpriteEffects.None, 0f);
                    }
                }

                // === (d) EL ECO DOBLE (las ondas que respiran) ===
                float latido = 0.5f + 0.5f * MathF.Sin(t * 2.6f);
                for (int k = 0; k < 2; k++)
                {
                    float fase = Frac01(t * 0.22f + k * 0.5f);
                    float rO = radio * (1.4f + fase * 5.5f);
                    float alfa = (1f - fase) * (0.32f + 0.18f * latido);
                    int segs = 34;
                    for (int s = 0; s < segs; s++)
                    {
                        float a0 = s * MathHelper.TwoPi / segs;
                        Vector2 p0 = c + a0.ToRotationVector2() * rO;
                        Vector2 p1 = c + (a0 + MathHelper.TwoPi / segs).ToRotationVector2() * rO;
                        VFXCore.Line(p0, p1, SigiloLib.Tint(
                            k == 0 ? new Color(190, 120, 255) : new Color(255, 200, 120),
                            alfa), 2.2f);
                    }
                }

                // ============================================================
                //  v6.153 — LAS TRES CAPAS NUEVAS (la letra: «tambien
                //  dale mas efectos al agujero runico puro» — más de
                //  todo): (e) la espiral de polvo estelar · (f) los
                //  chorros polares · (g) el horizonte que respira —
                //  TODO dentro del lote aditivo, determinista.
                // ============================================================

                // === (e) LA ESPIRAL DE POLVO ESTELAR (3 brazos de motas) ===
                for (int i = 0; i < 60; i++)
                {
                    int brazo = i % 3;
                    float h0 = VFXCore.Hash01(i, 7, 6131);
                    float caida = Frac01(t * (0.16f + 0.10f * h0) + h0 + brazo * 0.333f);
                    float ang = brazo * MathHelper.TwoPi / 3f + t * 0.42f
                        + caida * 5.4f + h0 * 0.35f;
                    // espiral logarítmica: el radio CAE al horizonte
                    float rE = radio * (1.12f + 3.4f * MathF.Pow(1f - caida, 1.7f));
                    Vector2 m = c + ang.ToRotationVector2() * rE;
                    float tam = (3.2f + 4.6f * h0) * (0.45f + 0.75f * caida);
                    Color tintaE = (i & 3) == 0
                        ? new Color(200, 235, 255)
                        : new Color(255, 226, 160);
                    VFXCore.Quad(m, SigiloLib.Tint(tintaE, 0.62f * (1f - caida * 0.35f)),
                        new Vector2(tam, tam * 0.55f), ang + MathHelper.PiOver2);
                }

                // === (f) LOS CHORROS POLARES (los haces de los polos) ===
                for (int polo = 0; polo < 2; polo++)
                {
                    float dirY = polo == 0 ? -1f : 1f;
                    for (int i = 0; i < 9; i++)
                    {
                        float h0 = VFXCore.Hash01(i, 8 + polo, 6141);
                        float sube = Frac01(t * (0.34f + 0.20f * h0) + h0 * 0.9f);
                        float alto = radio * (1.15f + sube * 3.3f);
                        float lado = (h0 - 0.5f) * radio * (0.28f + sube * 0.85f);
                        Vector2 p = c + new Vector2(lado, dirY * alto);
                        float chispa = 0.55f + 0.45f * MathF.Sin(t * 7f + i * 1.9f + polo * 3.1f);
                        float tamF = (5f + 7f * h0) * (0.5f + 0.6f * sube) * chispa;
                        VFXCore.Quad(p, SigiloLib.Tint(
                            polo == 0 ? new Color(170, 220, 255) : new Color(255, 210, 140),
                            0.55f * (1f - sube * 0.30f) * chispa),
                            new Vector2(tamF * 0.65f, tamF), dirY < 0f ? -MathHelper.PiOver2 : MathHelper.PiOver2);
                    }
                }

                // === (g) EL HORIZONTE QUE RESPIRA (onda de choque del borde) ===
                float choque = Frac01(t * 0.58f);            // cada ~1,7 s
                float rChoque = radio * (1.18f + choque * 2.6f);
                float alfaChoque = (1f - choque) * (1f - choque) * 0.55f;
                int segsG = 44;
                for (int s = 0; s < segsG; s++)
                {
                    float a0 = s * MathHelper.TwoPi / segsG + t * 0.18f;
                    Vector2 p0 = c + a0.ToRotationVector2() * rChoque;
                    Vector2 p1 = c + (a0 + MathHelper.TwoPi / segsG).ToRotationVector2() * rChoque;
                    VFXCore.Line(p0, p1, SigiloLib.Tint(new Color(235, 245, 255), alfaChoque), 1.6f);
                }
            }
            finally { VFXCore.RestaurarLoteVanilla(); }

            // === (c) LOS FILAMENTOS DE ACRECIÓN (arcos eléctricos) ===
            float radioF = Projectile.width * 0.5f * escala;
            for (int i = 0; i < 6; i++)
            {
                float h0 = VFXCore.Hash01(i, 4, 6121);
                float ang = h0 * MathHelper.TwoPi + t * (0.5f + 0.3f * h0);
                float dentro = radioF * (1.05f + 0.35f * VFXCore.Hash01(i, 5, 6123));
                float fuera = radioF * (2.6f + 1.4f * VFXCore.Hash01(i, 6, 6125));
                Vector2 a = centro + ang.ToRotationVector2() * fuera;
                Vector2 b = centro + (ang + 0.22f).ToRotationVector2() * dentro;
                DibujarArcoAcrecion(a, b, t + i);
            }
        }

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

            // v6.152 — LAS CAPAS NUEVAS: el estilo SUPER CARGADO de la
            // letra (torbellino de glifos + corona de lentejuelas +
            // filamentos de acreción + eco doble) — todo determinista
            // y por los lotes de la casa, sobre EL MISMO cuerpo.
            try
            {
                DibujarLasCapasNuevas(Projectile.Center, Main.GlobalTimeWrappedHourly,
                    Math.Max(Projectile.scale, 0.05f));
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>
        /// Dibuja el Agujero Negro Cósmico completo. Compartido entre el
        /// pase del mundo (PreDraw) y el pase posterior a la lente.
        /// CONTRATO: el SpriteBatch llega CERRADO y queda CERRADO.
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p)
        {
            Vector2 drawPos = p.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = p.whoAmI * 13 + 7;

            AgujeroPuroRenderer.Draw(drawPos, p.scale, time, seed);
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
        //  IMPACTO Y MUERTE (física exacta, paleta magenta/violeta/dorada)
        // ------------------------------------------------------------------


        // ==============================================================
        //  v6.146 — EL ANILLO DE RUNAS VIVAS (el reemplazo de la letra):
        //  12 glifos que cabalgan el horizonte en contrarrotação — cada
        //  runa es un pequeño diamante de tinta y luz que SE ENCIENDE
        //  al cruzar el FRENTE (el lado que mira el rumbo) y se enfría
        //  por detrás: el anillo parece GIRAR MÁS RÁPIDO que el agujero
        //  (la ilusión del horizonte) SIN una sola partícula de polvo.
        // ==============================================================


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(255, 68, 26), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(255, 180, 130, 170), 22);

            // Implosión: 50 partículas convergiendo en espiral

            // Explosión: 40 radiales

            // Destellos encantados magenta y dorados

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

            // v6.154 — sin el reset del global (el boost murió arriba:
            // cada agujero acelera solo SU materia — la estática de
            // nadie se toca al explotar)

            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 7f, 10, 20, 0.45f,
                    "AethonCosmicBlackHoleFinalBlast"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // Presets — colapso gravitatorio cósmico (magenta + blanco)
            ParticlePresets.Implosion(Projectile.Center, 185f, 52,
                new Color(255, 68, 26), 28);
            ParticlePresets.Explosion(Projectile.Center, 145f, 30,
                new Color(255, 245, 230), new Color(200, 50, 25), 44);

            // Implosión: partículas convergiendo

            // Explosión: anillo expansivo

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (curva elástica de aparición — copia exacta).</summary>
        /// <summary>El resto positivo [0,1) — el módulo de la casa.</summary>
        private static float Frac01(float x)
        {
            float f = x - MathF.Floor(x);
            return f < 0f ? 0f : (f > 0.999f ? 0.999f : f);
        }

        /// <summary>v6.152 — EL ARCO DE ACRECIÓN: una descarga eléctrica
        /// determinista de 7 segmentos entre dos puntos (el zigzag con
        /// Hash01 — R5: cero azar, la misma chispa en todas las
        /// pantallas) dibujada en el lote aditivo.</summary>
        private static void DibujarArcoAcrecion(Vector2 a, Vector2 b, float semillaT)
        {
            VFXCore.AbrirAdditive();
            try
            {
                const int segs = 7;
                Vector2 prev = a;
                for (int s = 1; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    Vector2 baseP = Vector2.Lerp(a, b, u);
                    float desv = (VFXCore.Hash01(s, 7, 6131 + (int)(semillaT * 13f) % 997) - 0.5f) *
                                 22f * (1f - MathF.Abs(u - 0.5f) * 1.2f);
                    Vector2 dir = b - a;
                    if (dir.LengthSquared() < 0.01f) dir = Vector2.UnitY;
                    Vector2 perp = new(-dir.Y, dir.X);
                    perp.Normalize();
                    Vector2 p = baseP + perp * desv - Main.screenPosition;
                    VFXCore.Line(prev - Main.screenPosition, p,
                        SigiloLib.Tint(new Color(170, 220, 255), 0.55f), 2.4f);
                    prev = p;
                }
            }
            finally { VFXCore.RestaurarLoteVanilla(); }
        }

        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
