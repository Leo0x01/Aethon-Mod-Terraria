using System;
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
    /// RelojArenaProjectile — v6.26 — EL RELOJ DE ARENA CÓSMICO VIVO.
    ///
    /// EL PROYECTIL ES UN RELOJ DE ARENA FLOTANTE de ~50 px de alto cuya
    /// arena son MOTAS DE LUZ doradas (el renderer las pinta con quads
    /// VFXCore): caen de la cámara superior a la inferior por el CUELLO
    /// ESTRECHO (la corriente visible) y abajo SE ACUMULAN en un montículo
    /// creciente. Cuando la cámara superior SE VACÍA (~5 s) el reloj SE
    /// INVIERTE — gira 180° suave (~0.43 s) — y el ciclo renace: dos
    /// inversiones completas y pico en los 12 s de vida.
    ///
    /// LA LÍNEA DE TIEMPO (determinista, compartida con el renderer):
    ///   · CICLO = 316 ticks: 290 de CAÍDA + 26 de INVERSIÓN.
    ///   · 26 GRANOS: el grano i empieza a caer en i·290/26 y tarda 36
    ///     ticks en cruzar el cuello (acelerando — arena de verdad).
    ///
    /// LA MECÁNICA DEL "TIEMPO LENTO" (sin ralentizar NPCs — nada de
    /// hacks globales): los enemigos en 160 px reciben el PESO de la
    /// arena — daño cada 10 ticks mientras cae + knockback HACIA ABAJO
    /// (gravedad aumentada) — y en cada INVERSIÓN un pulso que los hunde
    /// con más fuerza. El renderer añade afterimages fantasma en los
    /// enemigos tocados (visual solo cliente).
    ///
    /// Daño por v6.50 — GolpeMotor (el cauce del motor: crítica real,
    /// varianza, on-hit y sync MP del propio motor). Determinismo:
    /// semilla por identity; TODO el estado de la arena se DERIVA de la
    /// edad (cero estado extra).
    /// </summary>
    public class RelojArenaProjectile : ModProjectile
    {
        // ==================================================================
        //  LA LÍNEA DE TIEMPO DEL RELOJ (compartida con el renderer)
        // ==================================================================

        /// <summary>Vida total: 720 ticks = 12 s (dos inversiones y pico).</summary>
        public const int TotalTicks = 720;

        /// <summary>Duración de la fase de CAÍDA (la cámara vaciándose).</summary>
        public const int CaidaTicks = 290;

        /// <summary>Duración de la INVERSIÓN (el giro 180° suave).</summary>
        public const int GiroTicks = 26;

        /// <summary>El ciclo completo: caída + inversión.</summary>
        public const int CicloTicks = CaidaTicks + GiroTicks;

        /// <summary>Los GRANOS de arena estelar del reloj.</summary>
        public const int Granos = 26;

        /// <summary>Lo que tarda UN grano en cruzar el cuello.</summary>
        public const int CruceTicks = 36;

        // === LA FÍSICA DE FLOTACIÓN (el reloj de relojería) ===
        private const float Frenazo = 0.86f;       // la llegada muere rápido
        private const float BobAmp = 5f;           // flotación vertical (px)
        private const float BobHz = 0.045f;        // lenta: un péndulo de mesa

        /// <summary>La EDAD en ticks (ai[0] — sincronizada en MP).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>El espejo visual local de la edad.</summary>
        private float _age;

        /// <summary>Semilla determinista del disparo (ai[1]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>El daño base del arma (ai[2]).</summary>
        private float BaseDamage => Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;

        /// <summary>Tick del ÚLTIMO pulso de daño (guard local, ai[3] no
        /// sincroniza bien — usamos localAI[1]).</summary>
        private float LastTickDamage
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // EL RELOJ: caja de 50 px de alto (el dibujo es 100% código).
            Projectile.width = 44;
            Projectile.height = 54;
            // Daño 100% manual (el patrón de la casa): el peso de la arena.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTicks;
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            Projectile.ai[2] = Projectile.damage;
            _age = 0f;
            // El reloj NACE YA con un toque de arena en el cuello (no vacío).
            LastTickDamage = -999f;
        }

        // ==================================================================
        //  LA MÁQUINA — el reloj da la hora
        // ==================================================================

        public override void AI()
        {
            _age += 1f;
            Projectile.ai[0] = _age;

            // === LA LLEGADA: el disparo frena y el reloj QUEDA FLOTANDO
            //     en su sitio (deriva casi nula — un objeto de escritorio). ===
            Projectile.velocity *= Frenazo;
            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = Vector2.Zero;

            // === LA FLOTACIÓN (visual — la posición lógica no se toca; el
            //     renderer suma el bob a su manera determinista). ===

            // === LA ROTACIÓN DE LA INVERSIÓN (p.rotation — el renderer la
            //     lee; el giro es SUAVE con smoothstep). ===
            float ciclo = _age % CicloTicks;
            if (ciclo >= CaidaTicks)
            {
                // En pleno GIRO: la rotación va de 0 a π con smoothstep.
                float g = (ciclo - CaidaTicks) / (float)GiroTicks;
                float suave = g * g * (3f - 2f * g);
                // Las inversiones se ACUMULAN (n·π): el reloj da la vuelta
                // y queda INVERTIDO hasta la siguiente.
                int vueltas = (int)(_age / CicloTicks);
                Projectile.rotation = (vueltas + suave) * MathHelper.Pi;
            }
            else
            {
                int vueltas = (int)(_age / CicloTicks);
                Projectile.rotation = vueltas * MathHelper.Pi;
            }

            // === EL PESO DE LA ARENA (v6.50 — GolpeMotor: el cauce del
            //     motor) ===
            {
                bool cayendo = ciclo < CaidaTicks;
                if (cayendo && _age - LastTickDamage >= 10f)
                {
                    LastTickDamage = _age;
                    PesarEnemigos(0.16f, 2.6f);
                }
            }

            // === LA INVERSIÓN: el pulso de arena que HUNDE ===
            // (el instante en que el giro ARRANCA: la cámara superior se
            //  VACIÓ y el reloj se da la vuelta — la onda del tiempo.)
            if (ciclo >= CaidaTicks && ciclo < CaidaTicks + 1.5f && _age > GiroTicks)
            {
                // v6.50 — GolpeMotor (el cauce del motor).
                PesarEnemigos(0.45f, 5.5f);

                if (Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Kick(3f, 10);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item45.WithPitchOffset(-0.35f), Projectile.Center);
                    // Las motas que SALPican al invertirse (determinista).
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i / 8f * MathHelper.TwoPi + Seed * 0.13f;
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(0.8f, 2.2f),
                            150, new Color(255, 220, 130), 0.7f);
                        d.noGravity = true;
                    }
                }
            }

            // === LA LUZ DEL RELOJ (dorada, puntual — un objeto del mundo) ===
            if (Main.netMode != NetmodeID.Server && _age % 4f == 0f)
                Lighting.AddLight(Projectile.Center, 0.55f, 0.42f, 0.16f);
        }

        /// <summary>EL PESO: daña a los enemigos en 160 px (v6.50 — GolpeMotor:
        /// el cauce del motor) y los EMPUJA hacia abajo — la gravedad
        /// aumentada del reloj.</summary>
        private void PesarEnemigos(float mult, float hundimiento)
        {
            int dmg = Math.Max(1, (int)(BaseDamage * mult));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > 160f) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1f, true);

                // EL PESO REAL: la velocidad del enemigo se hunde — gravedad
                // aumentada local (esto no necesita red: la gravedad vanilla
                // re-sincroniza el estado; el empujón es cosmético-físico).
                // v6.50: el empujón sigue en server/SP (autoridad del NPC).
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.noGravity == false)
                    npc.velocity.Y += hundimiento;
            }
        }

        // ==================================================================
        //  EL DIBUJO (contrato de batch v6.10 — a prueba de balas)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // CONTRATO DE BATCH (v6.10): durante PreDraw el batch de tML
            // está ABIERTO; hay que CERRARLO antes de que el renderer haga
            // sus Begin() propios, y restaurarlo al final.
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                float drawAge = MathF.Max(_age, Projectile.ai[0]);
                RelojArenaRenderer.Draw(Projectile, drawAge, Seed);
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
            // El reloj SE APAGA en paz: la última arena se suelta (visual
            // solo cliente — el polvo dorado del final).
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 12; i++)
            {
                float ang = i / 12f * MathHelper.TwoPi + Seed * 0.21f;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang) + 0.6f) *
                    Main.rand.NextFloat(0.6f, 1.8f),
                    140, new Color(255, 230, 150), 0.6f);
                d.noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item45.WithPitchOffset(0.2f), Projectile.Center);
        }
    }
}
