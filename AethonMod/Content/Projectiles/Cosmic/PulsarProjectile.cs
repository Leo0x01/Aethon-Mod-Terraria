using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// PulsarProjectile — v6.26 — EL PÚLSAR (EL FARO).
    ///
    /// La estrella de neutrones GIRANDO con sus DOS HACES POLARES
    /// barriendo el mundo a ~1 rev/s.
    ///
    /// FÍSICA:
    ///   · El ángulo del haz es DETERMINISTA y SINCRONIZADO: se deriva de
    ///     ai[0] (edad) — servidor y clientes calculan EL MISMO barrido.
    ///   · Cada vez que el HAZ barre a un enemigo: GOLPE ×1.5 con
    ///     cooldown de 30 ticks POR NPC (Dictionary npc.whoAmI → tick).
    ///   · Deriva lenta + persecución suave (el faro ancla su cielo).
    ///   · Pulso de luz SINCRONIZADO: la luz del mundo late con el giro.
    ///   · Vida 6 s.
    ///
    /// Daño por v6.50 — GolpeMotor (el cauce del motor: crítica real,
    /// varianza, on-hit y sync MP del propio motor).
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class PulsarProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (6 s).</summary>
        private const int Lifetime = 360;

        /// <summary>Cooldown de golpe del haz POR NPC (ticks).</summary>
        private const int BeamCooldown = 30;

        /// <summary>Grosor efectivo del haz para el barrido (px del test).</summary>
        private const float BeamHitWidth = 30f;

        /// <summary>Los cooldowns por NPC (npc.whoAmI → tick del último golpe).</summary>
        private readonly Dictionary<int, int> _beamHits = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 49;
            Projectile.height = 49;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // ai[1] = semilla determinista del disparo (visual) + limpiar
            // los cooldowns (tML reutiliza instancias: empezar de cero).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            _beamHits.Clear();
            // Fase inicial del faro: hacia el enemigo más cercano si lo hay
            // (si no, hacia arriba — el haz nace mirando algo).
            float init = -MathHelper.PiOver2;
            NPC prey = null;
            float best = 900f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = (npc.Center - Projectile.Center).Length();
                if (d < best) { best = d; prey = npc; }
            }
            if (prey != null)
                init = (prey.Center - Projectile.Center).ToRotation();
            Projectile.rotation = init;
        }

        /// <summary>Edad en ticks (ai[0] — SINCRONIZADO: define el barrido).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida.</summary>
        private float LifeT => MathHelper.Clamp(Age / Lifetime, 0f, 1f);

        /// <summary>
        /// EL ÁNGULO DEL FARO (determinista): fase inicial (rotation) +
        /// edad × 1 rev/s. Server y clientes calculan LO MISMO.
        /// </summary>
        public float BeamAngle => Projectile.rotation + Age / 60f * MathHelper.TwoPi;

        public override void AI()
        {
            Projectile.ai[0] += 1f;

            // === EL POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 40f, Age, true));

            // === DERIVA LENTA + PERSECUCIÓN SUAVE (el faro ancla) ===
            Projectile.velocity *= 0.96f;
            NPC prey = FindNearestEnemy(520f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 70f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 0.45f;
                if (Projectile.velocity.Length() > 2f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 2f;
            }

            // === EL BARRIDO QUE GOLPEA: cada enemigo TOCADO por los haces
            //     recibe daño ×1.5 con cooldown de 30 ticks POR NPC (v6.50:
            //     GolpeMotor — el cauce del motor) ===
            if (Age > 24f)
            {
                int beamDamage = Math.Max(1, (int)(Projectile.damage * 1.5f));
                int now = (int)Age;

                for (int side = 0; side < 2; side++)
                {
                    Vector2 dir = AngleToDir(BeamAngle + side * MathHelper.Pi);
                    Vector2 a = Projectile.Center + dir * (NeutronStarRenderer.BodyPx * 0.5f);
                    Vector2 b = Projectile.Center + dir * PulsarRenderer.BeamLength;

                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        if (DistToSegment(npc.Center, a, b) > BeamHitWidth) continue;

                        // EL COOLDOWN POR NPC (el Dictionary de la misión).
                        if (_beamHits.TryGetValue(npc.whoAmI, out int lastHit) &&
                            now - lastHit < BeamCooldown) continue;
                        _beamHits[npc.whoAmI] = now;

                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, beamDamage, 2.5f, true);
                        // La electrificación del haz: server/SP.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            try { npc.AddBuff(BuffID.Electrified, 120); } catch { }
                    }
                }
            }

            // === EL PULSO DE LUZ SINCRONIZADO: la luz late CON el giro
            //     (dos máximos por revolución — uno por haz) ===
            float pulse = 0.5f + 0.5f * MathF.Sin(BeamAngle * 2f);
            float lightR = (0.55f + 0.75f * pulse) * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.55f * lightR, 0.70f * lightR, 1.00f * lightR);
            // Muestras a lo largo del haz activo (la lección del muestreo).
            Vector2 beamDir = AngleToDir(BeamAngle);
            LumenLib.LightAlong(Projectile.Center, Projectile.Center + beamDir * 200f,
                new Color(110, 170, 240), 0.45f * pulse, 100f);

            // === VISUAL SOLO CLIENTE: motas cayendo al disco de emisión ===
            if (Main.netMode == NetmodeID.Server) return;
            if (Age % 9f == 0f && Projectile.scale > 0.3f)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 26f,
                    DustID.BlueTorch,
                    -new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(0.5f, 1.4f),
                    140, new Color(150, 200, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private NPC FindNearestEnemy(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        /// <summary>Dirección unitaria de un ángulo.</summary>
        private static Vector2 AngleToDir(float ang) =>
            new Vector2(MathF.Cos(ang), MathF.Sin(ang));

        /// <summary>Distancia punto → segmento (el test del barrido).</summary>
        private static float DistToSegment(Vector2 pt, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 0.001f) return (pt - a).Length();
            float t = MathHelper.Clamp(Vector2.Dot(pt - a, ab) / len2, 0f, 1f);
            return (pt - a - ab * t).Length();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10): durante PreDraw
            //  el batch de tML está ABIERTO; cerrarlo antes del pase propio.
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                PulsarRenderer.Draw(Projectile, BeamAngle, LifeT, Seed);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            // === EL DAÑO FINAL: el faro se apaga de golpe (v6.50 — GolpeMotor) ===
            {
                int burst = Math.Max(1, (int)(Projectile.damage * 0.9f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > 160f) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, burst, 2f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 120); } catch { }   // v6.30: el pulso final ELECTRIFICA
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === EL APAGÓN DEL FARO: anillo + el último destello doble ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.RingPulse(Projectile.Center, 130f * scale,
                new Color(140, 200, 255, 220), 26);
            ParticlePresets.RingPulse(Projectile.Center, 90f * scale,
                new Color(240, 250, 255, 200), 18);

            for (int i = 0; i < 30; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(3f, 9f) * scale,
                    220, new Color(160, 205, 255), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 5f, 10, 14, 0.4f,
                    "AethonPulsarDeath"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.2f), Projectile.Center);
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
