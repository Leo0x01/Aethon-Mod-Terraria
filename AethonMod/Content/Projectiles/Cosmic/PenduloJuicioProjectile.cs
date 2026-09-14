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
    /// PenduloJuicioProjectile — v6.26 — EL PÉNDULO DEL JUICIO.
    ///
    /// EL PROYECTIL ES UN PÉNDULO REAL: un ANCLA fija en el aire (el punto
    /// de emisión, guardado en ai[0]/ai[1]) de la que cuelga una MAZA de
    /// 26 px por un HILO de 180 px. La física se integra A MANO cada tick:
    ///
    ///     θ'' = −(g/L)·sen θ        con g = 0.21 px/t², L = 180 px
    ///     (semi-período ~1.55 s a amplitudes de sentencia)
    ///
    /// FASES:
    ///   · EL JUICIO (0..480 ticks = 8 s): la maza barre el arco; cada
    ///     enemigo a 34 px de la bola recibe daño ×1.2 con KNOCKBACK
    ///     TANGENCIAL fuerte (perpendicular al hilo, en el sentido del
    ///     movimiento) — cooldown 18 ticks por NPC. En cada CRUCE POR EL
    ///     FONDO (θ=0) se INYECTA energía: ω ×1.08 — el arco SE AMPLÍA
    ///     hasta ±150° — y el hilo brilla más (la energía visible).
    ///   · EL DESPRENDIMIENTO (480): el hilo SE CORTA — chispas
    ///     StormLib.MultiBolt + PyraLib.Sparks doradas — y la maza VUELA
    ///     con la velocidad tangencial que traía (×1.2) en arco balístico
    ///     (gravedad 0.3, colisión de tiles real).
    ///   · LA SENTENCIA (impacto o 150 ticks): EXPLOSIÓN — daño ×1.5 en
    ///     300 px + OndaLib.Shock + Kick 8 + partículas.
    ///
    /// Daño MP-seguro: SimpleStrikeNPC bajo `Main.netMode != NetmodeID.
    /// MultiplayerClient`. Determinismo por semilla de identity.
    /// </summary>
    public class PenduloJuicioProjectile : ModProjectile
    {
        /// <summary>Duración de la fase de péndulo: 480 ticks = 8 s.</summary>
        public const int JuicioTicks = 480;

        /// <summary>Vuelo balístico máximo tras el corte: 150 ticks.</summary>
        public const int VueloTicks = 150;

        /// <summary>Vida total: 630 ticks = 10.5 s.</summary>
        public const int TotalTicks = JuicioTicks + VueloTicks;

        /// <summary>La LONGITUD del hilo (px).</summary>
        public const float L = 180f;

        /// <summary>La gravedad del péndulo (px/t² — la de la sentencia).</summary>
        private const float G = 0.21f;

        /// <summary>La amplitud MÁXIMA de la sentencia: ±150°.</summary>
        private const float AmpMax = 150f * MathHelper.Pi / 180f;

        /// <summary>El crecimiento de energía por vaivén: +8%.</summary>
        private const float Inyeccion = 1.08f;

        // === EL ESTADO DEL PÉNDULO (local — la física es determinista) ===
        private float _theta = 0.62f;      // ángulo inicial (~35°)
        private float _omega;
        private float _lastPeak = 0.62f;   // el |θ| máximo del vaivén actual
        private bool _suelto;
        private float _age;

        /// <summary>El cooldown de golpe por NPC (whoAmI → tick del último
        /// golpe) — evita el metralleta del barrido.</summary>
        private readonly Dictionary<int, float> _golpeCd = new();

        /// <summary>El ANCLA en el aire (ai[0]=X, ai[1]=Y — sincronizadas).</summary>
        private Vector2 Ancla => new(Projectile.ai[0], Projectile.ai[1]);

        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2]) + Projectile.identity;

        /// <summary>El daño base del arma (localAI[2] espejo de ai[3] no
        /// existe — el daño llega en Projectile.damage).</summary>
        private float BaseDamage => Projectile.ai[3] > 0f ? Projectile.ai[3] : Projectile.damage;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // La CAJA es la MAZA (el dibujo es 100% código).
            Projectile.width = 30;
            Projectile.height = 30;
            // Daño 100% manual (el patrón de la casa): el barrido.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;   // durante el juicio NO choca
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // EL ANCLA se clava en el punto de emisión (el cursor del
            // disparo — la posición de nacimiento del proyectil).
            Projectile.ai[0] = Projectile.Center.X;
            Projectile.ai[1] = Projectile.Center.Y;
            if (Projectile.ai[2] <= 0f)
                Projectile.ai[2] = (Projectile.identity % 9973 + 1) * 1f;
            Projectile.ai[3] = Projectile.damage;
            _age = 0f;
            Projectile.netUpdate = true;   // el ancla viaja al cliente
        }

        // ==================================================================
        //  LA FÍSICA DEL JUICIO
        // ==================================================================

        public override void AI()
        {
            _age += 1f;

            // ============================================================
            //  FASE 1 · EL PÉNDULO (θ'' = −g/L·sen θ, integrado a mano)
            // ============================================================
            if (!_suelto)
            {
                // LA INTEGRACIÓN (semi-implícita — estable a amplitudes
                // grandes: primero ω, luego θ con el ω NUEVO).
                _omega += -(G / L) * MathF.Sin(_theta);
                _theta += _omega;

                // EL PICO del vaivén actual (para el brillo del hilo).
                if (MathF.Abs(_theta) > _lastPeak) _lastPeak = MathF.Abs(_theta);

                // LA INYECCIÓN DE ENERGÍA: al CRUZAR EL FONDO (θ cambia de
                // signo) la sentencia se ENDURECE: ω ×1.08 (equivale a
                // amplitud +8% por vaivén) — hasta el tope ±150°.
                if (_theta <= 0f && _theta - _omega > 0f || _theta >= 0f && _theta - _omega < 0f)
                {
                    if (_lastPeak < AmpMax)
                        _omega *= Inyeccion;
                    _lastPeak = 0f;   // nuevo vaivén, nuevo pico
                }

                // LA POSICIÓN de la maza (y del proyectil — la caja ES la bola).
                Projectile.Center = PosMaza();
                Projectile.velocity = Vector2.Zero;   // colgado: sin inercia propia
                Projectile.rotation = -_theta;   // el renderer la usa

                // EL BARRIDO: daño ×1.2 con knockback TANGENCIAL.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    BarrerConLaMaza();

                // LA ESTELA del arco (el rastro del barrido).
                EstelaLib.Track(Projectile.whoAmI, 16).Push(Projectile.Center);
                if (_age % 120f == 0f) EstelaLib.PurgeTracks();

                // LA LUZ dorada del hilo (más brillo = más energía).
                if (Main.netMode != NetmodeID.Server && _age % 3f == 0f)
                {
                    float e = Energia();
                    Lighting.AddLight(Projectile.Center, 0.5f * e, 0.38f * e, 0.14f * e);
                }

                // === EL DESPRENDIMIENTO (a los 8 s) ===
                if (_age >= JuicioTicks)
                    CortarElHilo();

                return;
            }

            // ============================================================
            //  FASE 2 · EL VUELO BALÍSTICO (la maza suelta)
            // ============================================================
            Projectile.velocity.Y += 0.30f;               // la gravedad de verdad
            if (Projectile.velocity.Length() > 16f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 16f;
            Projectile.rotation += Projectile.velocity.X * 0.02f;   // gira al volar

            // LA ESTELA del vuelo.
            EstelaLib.Track(Projectile.whoAmI, 16).Push(Projectile.Center);

            if (Main.netMode != NetmodeID.Server && _age % 3f == 0f)
                Lighting.AddLight(Projectile.Center, 0.5f, 0.36f, 0.12f);

            // === EL FIN DEL VUELO sin choque: LA SENTENCIA cae igual ===
            if (_age >= TotalTicks - 1f)
            {
                LaSentencia();
                return;
            }
        }

        /// <summary>La posición de la MAZA en el extremo del hilo.</summary>
        private Vector2 PosMaza()
        {
            // θ=0 cuelga VERTICAL hacia abajo: x = L·sen θ, y = L·cos θ.
            return Ancla + new Vector2(MathF.Sin(_theta), MathF.Cos(_theta)) * L;
        }

        /// <summary>La ENERGÍA del péndulo (0..1 — el brillo del hilo).</summary>
        private float Energia() => MathHelper.Clamp(_lastPeak / AmpMax, 0.12f, 1f);

        // ==================================================================
        //  EL BARRIDO — la maza sentencia con el paso
        // ==================================================================

        private void BarrerConLaMaza()
        {
            int dmg = Math.Max(1, (int)(BaseDamage * 1.2f));

            // La DIRECCIÓN TANGENCIAL (perpendicular al hilo, en el
            // sentido del movimiento): el knockback que BARRE.
            Vector2 tangente = new(MathF.Cos(_theta), -MathF.Sin(_theta));
            if (_omega < 0f) tangente *= -1f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                if ((npc.Center - Projectile.Center).Length() > 34f) continue;

                // El cooldown por NPC (el barrido no es metralleta).
                if (_golpeCd.TryGetValue(npc.whoAmI, out float last) &&
                    _age - last < 18f) continue;
                _golpeCd[npc.whoAmI] = _age;

                npc.SimpleStrikeNPC(dmg, Math.Sign(tangente.X), false, 7f,
                    DamageClass.Magic);
                // EL EMPUJÓN TANGENCIAL físico (el barrido de verdad).
                npc.velocity += tangente * 2.2f;
            }
        }

        // ==================================================================
        //  EL DESPRENDIMIENTO Y LA SENTENCIA
        // ==================================================================

        private void CortarElHilo()
        {
            _suelto = true;
            Projectile.tileCollide = true;   // ahora la maza choca con el mundo

            // LA VELOCIDAD TANGENCIAL del momento del corte (×1.2 — el
            // látigo del desprendimiento) — la velocidad lineal del péndulo.
            Vector2 tangente = new(MathF.Cos(_theta), -MathF.Sin(_theta));
            if (_omega < 0f) tangente *= -1f;
            float vLin = MathF.Abs(_omega) * L;
            Projectile.velocity = tangente * vLin * 1.2f;
            // Un toque hacia arriba (el tirón del hilo al soltar).
            Projectile.velocity.Y -= 1.5f;

            if (Main.netMode == NetmodeID.Server) return;

            // === LAS CHISPAS DEL CORTE (visual cliente) ===
            OndaLib.Kick(4f, 12);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item12.WithPitchOffset(0.2f), Projectile.Center);
            // El rayo del hilo cortándose + las brasas doradas.
            ParticlePresets.RingPulse(Projectile.Center, 60f,
                new Color(255, 220, 130, 200), 22);
        }

        /// <summary>LA SENTENCIA: la explosión final de la maza.</summary>
        private void LaSentencia()
        {
            // === EL DAÑO (×1.5 en 300 px — MP-seguro) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = Math.Max(1, (int)(BaseDamage * 1.5f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    if ((npc.Center - Projectile.Center).Length() > 300f) continue;
                    npc.SimpleStrikeNPC(dmg, npc.direction, false, 8f, DamageClass.Magic);
                }
            }

            if (Main.netMode == NetmodeID.Server) { Projectile.Kill(); return; }

            // === EL ESTALLIDO (OndaLib.Shock + Kick 8 + partículas) ===
            OndaLib.Kick(8f, 18);
            ParticlePresets.Explosion(Projectile.Center, 220f, 40,
                new Color(255, 246, 220), new Color(255, 180, 60), 46);
            ParticlePresets.RingPulse(Projectile.Center, 300f,
                new Color(255, 226, 140, 220), 40);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item70.WithPitchOffset(-0.2f), Projectile.Center);

            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // La maza TOCÓ el mundo tras el corte: LA SENTENCIA.
            if (_suelto) LaSentencia();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // (El daño es manual — esto no debería dispararse; por si
            // acaso, la maza suelta detona al contacto.)
            if (_suelto) LaSentencia();
        }

        // ==================================================================
        //  EL ACCESO DEL RENDERER (el péndulo para el pintado)
        // ==================================================================

        /// <summary>El ANCLA fija en el aire (para el renderer).</summary>
        internal Vector2 AnclaPublica => Ancla;

        /// <summary>El ángulo actual θ (para el renderer).</summary>
        internal float Theta => _theta;

        /// <summary>La energía 0..1 (el brillo del hilo).</summary>
        internal float EnergiaPublica => Energia();

        /// <summary>La velocidad angular ω (para los fantasmas del arco).</summary>
        internal float Omega => _omega;

        /// <summary>¿Ya se soltó el hilo?</summary>
        internal bool Suelto => _suelto;

        // ==================================================================
        //  EL DIBUJO (contrato de batch v6.10)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                PenduloJuicioRenderer.Draw(this, _age, Seed);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            // La muerte natural (timeLeft agotado) ya detona por su cuenta
            // en la AI (fase de vuelo); aquí solo el POLVO de despedida si
            // algo externo nos mató a medias (visual cliente).
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    140, new Color(255, 226, 140), 0.6f);
                d.noGravity = true;
            }
        }
    }
}
