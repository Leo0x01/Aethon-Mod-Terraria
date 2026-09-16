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
    /// NovaEncadenadaProjectile — LA NOVA ENCADENADA.
    ///
    /// UNA SOLA CLASE, CUATRO GENERACIONES (ai[1] = generación, tope 3):
    ///   · GEN 0 — LA ESFERA MADRE: vuela lenta hacia el cursor; al tocar
    ///     enemigo o suelo ESTALLA en una nova dorada (anillo OndaLib.Shock,
    ///     radio 140) cuyo FRENTE golpea ×1.0 a los que atraviesa.
    ///   · GEN 1..3 — LAS HIJAS: pequeñas novas homing hacia la presa que
    ///     les asignó la madre (ai[0]); estallan ×0.65 / ×0.42 / ×0.28 y
    ///     cada una escupe 2 hijas más (la gen 0 escupe 3) hacia los
    ///     enemigos más cercanos AÚN SIN GOLPEAR (radio de búsqueda 400).
    ///
    /// CERO Main.rand: las presas se eligen por DISTANCIA (determinista).
    /// Luz cálida + kick pequeño por cada nova. El daño SIEMPRE con guard
    /// MP + EsObjetivo + SimpleStrikeNPC (escuela A).
    /// </summary>
    public class NovaEncadenadaProjectile : ModProjectile
    {
        /// <summary>Duración de la animación de cada nova (ticks).</summary>
        private const int NovaTicks = 24;

        /// <summary>Tick de la animación en que la nova ESCUPE sus hijas.</summary>
        private const int TickHijas = 8;

        /// <summary>Radio de búsqueda de presas para las hijas.</summary>
        private const float BusquedaHijas = 400f;

        /// <summary>Radios de la nova por generación.</summary>
        private static readonly float[] Radios = { 140f, 110f, 85f, 65f };

        /// <summary>Multiplicadores de daño por generación.</summary>
        private static readonly float[] Mult = { 1f, 0.65f, 0.42f, 0.28f };

        // === LA PALETA DORADA (nova cálida) ===
        private static readonly Color ColorOro = new(255, 214, 120);
        private static readonly Color ColorOroClaro = new(255, 240, 190);
        private static readonly Color ColorBlancoCaliente = new(255, 252, 235);
        private static readonly Color ColorBrasa = new(255, 150, 60);

        private float _age;
        private bool _nacio;
        private bool _explotada;
        private float _explAge;
        private int _gen;
        private int _objetivo = -1;
        private Vector2 _ultimoPunto;
        private bool _hijasLanzadas;

        /// <summary>Enemigos ya golpeados por ESTA nova (frente + contacto).</summary>
        private readonly HashSet<int> _golpeados = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 320;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 53);

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _gen = Math.Clamp((int)Projectile.ai[1], 0, 3);
                _objetivo = (int)Projectile.ai[0];
                _ultimoPunto = Projectile.Center;
            }

            // LA LUZ CÁLIDA late en todas las fases.
            float pulso = 0.8f + 0.2f * MathF.Sin(_age * 0.35f + Seed);
            Lighting.AddLight(Projectile.Center, 0.55f * pulso, 0.42f * pulso, 0.16f * pulso);

            if (!_explotada)
            {
                if (_gen == 0) VolarMadre();
                else VolarHija();
            }
            else
            {
                // ============================================================
                //  LA NOVA: el anillo crece y su FRENTE golpea a su paso.
                // ============================================================
                _explAge += 1f;
                GolpearFrente();

                // Las hijas salen disparadas a mitad del estallido.
                if (!_hijasLanzadas && _explAge >= TickHijas)
                {
                    _hijasLanzadas = true;
                    LanzarHijas();
                }

                if (_explAge >= NovaTicks)
                    Projectile.Kill();
            }
        }

        /// <summary>GEN 0: la esfera madre vuela lenta; estalla al tocar.</summary>
        private void VolarMadre()
        {
            // Vuelo recto y lento (la velocidad la fijó el ítem: 7 px/tick).
            if (_age > 8f) Projectile.velocity *= 0.995f;

            if (TocaEnemigo(out int who))
            {
                _golpeados.Add(who);
                Explotar();
                return;
            }
            if (Collision.SolidCollision(Projectile.Center - Vector2.One * 5f, 10, 10))
            {
                Explotar();
                return;
            }
        }

        /// <summary>GEN 1+: las hijas vuelan hacia su presa asignada.</summary>
        private void VolarHija()
        {
            NPC npc = _objetivo >= 0 && _objetivo < Main.maxNPCs ? Main.npc[_objetivo] : null;
            if (npc != null && npc.active) _ultimoPunto = npc.Center;

            Vector2 deseada = _ultimoPunto - Projectile.Center;
            float speed = 9f + _gen * 1.5f;
            if (deseada.LengthSquared() > 0.001f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    Vector2.Normalize(deseada) * speed, 0.14f);

            if (deseada.LengthSquared() < 18f * 18f || _age > 150f)
            {
                Explotar();
                return;
            }
            if (TocaEnemigo(out int who))
            {
                _golpeados.Add(who);
                Explotar();
            }
        }

        /// <summary>¿La esfera toca ya a algún enemigo?</summary>
        private bool TocaEnemigo(out int who)
        {
            who = -1;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float alcance = 14f + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) < alcance * alcance)
                {
                    who = npc.whoAmI;
                    return true;
                }
            }
            return false;
        }

        /// <summary>EL ESTALLIDO: arranca la animación de nova (24 ticks).</summary>
        private void Explotar()
        {
            _explotada = true;
            _explAge = 0f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = NovaTicks + 2;
            Projectile.netUpdate = true;

            if (Main.netMode != NetmodeID.Server)
            {
                // Luz cálida + KICK pequeño por cada nova + retumbo.
                OndaLib.Kick(_gen == 0 ? 2.2f : 1.3f, 8);
                OndaLib.Flash(ColorOro, _gen == 0 ? 0.20f : 0.10f, 8);
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item14.WithPitchOffset(0.15f * _gen), Projectile.Center);
                }
                catch { }
                RiftLib.ChispasAnomalia(Projectile.Center, 8 + _gen * 3, PyraPalettes.SolarFire,
                    Seed + 3, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }
        }

        /// <summary>
        /// EL FRENTE DE LA NOVA: solo golpea a los que el anillo ATRAVIESA
        /// (|dist - frente| &lt; banda). Un golpe por enemigo por nova.
        /// Escuela A: solo server/singleplayer.
        /// </summary>
        private void GolpearFrente()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            float radio = Radios[_gen];
            float progreso = MathHelper.Clamp(_explAge / NovaTicks, 0f, 1f);
            float frente = radio * OndaLib.Expansion(progreso);
            int dmg = Math.Max(1, (int)(Projectile.damage * Mult[_gen]));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc) || _golpeados.Contains(npc.whoAmI)) continue;

                float banda = 30f + Math.Max(npc.width, npc.height) * 0.5f;
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (MathF.Abs(d - frente) > banda) continue;

                _golpeados.Add(npc.whoAmI);
                npc.SimpleStrikeNPC(dmg, npc.direction, false, 3f, DamageClass.Magic);
                try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 180); } catch { }
            }
        }

        /// <summary>
        /// LAS HIJAS: hasta 3 (gen 0) o 2 (gen 1+) nuevas novas hacia los
        /// enemigos más cercanos AÚN SIN GOLPEAR. Cero azar: distancia pura.
        /// </summary>
        private void LanzarHijas()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (_gen >= 3) return;   // TOPE de 3 generaciones

            int deseadas = _gen == 0 ? 3 : 2;

            // Los candidatos: vivos, objetivo, cerca y SIN golpear aún.
            List<NPC> candidatos = new();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc) || _golpeados.Contains(npc.whoAmI)) continue;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > BusquedaHijas * BusquedaHijas)
                    continue;
                candidatos.Add(npc);
            }

            // Orden POR DISTANCIA (inserción: listas diminutas, determinista).
            for (int i = 1; i < candidatos.Count; i++)
            {
                NPC key = candidatos[i];
                float dk = Vector2.DistanceSquared(key.Center, Projectile.Center);
                int j = i - 1;
                while (j >= 0 && Vector2.DistanceSquared(candidatos[j].Center, Projectile.Center) > dk)
                {
                    candidatos[j + 1] = candidatos[j];
                    j--;
                }
                candidatos[j + 1] = key;
            }

            int n = Math.Min(deseadas, candidatos.Count);
            for (int i = 0; i < n; i++)
            {
                NPC presa = candidatos[i];
                Vector2 dir = presa.Center - Projectile.Center;
                dir = dir.LengthSquared() > 1f ? Vector2.Normalize(dir) : Vector2.UnitX;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                    dir * 8f, Projectile.type, Projectile.damage, 2f,
                    Projectile.owner, presa.whoAmI, _gen + 1);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // Las brasas de la despedida (deterministas).
            RiftLib.ChispasAnomalia(Projectile.Center, 5, PyraPalettes.SolarFire,
                Seed + 8, out ParticleData[] motas);
            if (motas != null)
                for (int i = 0; i < motas.Length; i++)
                    ParticleManager.Spawn(motas[i]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar, dibujar, restaurar.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawNova(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private void DrawNova()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            if (!_explotada)
            {
                // ========================================================
                //  LA ESFERA DE NOVA — la madre es un sol en miniatura;
                //  las hijas, chispas doradas con estela.
                // ========================================================
                float R = _gen == 0 ? 20f : 13f - _gen * 1.5f;
                float latido = 0.85f + 0.15f * MathF.Sin(time * 6f + seed);

                LumenLib.Bloom(Main.spriteBatch, drawPos, R * 2.8f, ColorOro, 0.34f * latido, 3);

                // La estela cálida contra la velocidad.
                if (Projectile.velocity.LengthSquared() > 1f)
                {
                    Vector2 vdir = -Vector2.Normalize(Projectile.velocity);
                    LumenLib.Ray(Main.spriteBatch, drawPos, vdir, 34f, R * 0.9f,
                        ColorBrasa, 0.30f, 0.7f);
                }

                // El núcleo compacto.
                var texSize = new Vector2(VFXCore.GlowOrb.Width, VFXCore.GlowOrb.Height);
                Main.spriteBatch.Draw(VFXCore.GlowOrb, drawPos, null,
                    Tint(ColorOroClaro, 0.85f * latido), 0f, texSize * 0.5f,
                    new Vector2(R * 1.5f, R * 1.5f) / texSize, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.GlowOrb, drawPos, null,
                    Tint(ColorBlancoCaliente, 0.90f), 0f, texSize * 0.5f,
                    new Vector2(R * 0.7f, R * 0.7f) / texSize, SpriteEffects.None, 0f);

                // Las hijas titilan más rápido (más pequeñas, más nerviosas).
                if (_gen > 0)
                {
                    float tw = 0.5f + 0.5f * MathF.Sin(time * 9f + seed * 1.7f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, drawPos, null,
                        Tint(ColorOro, 0.30f * tw), 0f,
                        new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                        new Vector2(R * 2.2f, R * 2.2f) / new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height),
                        SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();

                // La madre es un SOL dorado (DrawSunBody gestiona sus lotes).
                if (_gen == 0)
                    RuneSunRenderer.DrawSunBody(drawPos, R, time * 1.2f, time,
                        new Color(255, 246, 220),   // main — oro claro
                        new Color(255, 196, 100),   // darker — ámbar
                        new Color(232, 128, 40),    // accent — brasa
                        new Color(255, 214, 130),   // backHot — halo dorado
                        new Color(255, 150, 60),    // backRed — halo cálido
                        new Color(255, 250, 230),   // shine — destello
                        2.6f, 0.92f);
            }
            else
            {
                // ========================================================
                //  LA NOVA: el ANILLO de choque dorado-blanco creciendo
                //  + el corazón que se apaga.
                // ========================================================
                float progreso = MathHelper.Clamp(_explAge / NovaTicks, 0f, 1f);
                float radio = Radios[_gen];
                float caida = 1f - progreso * progreso;

                OndaLib.Shock(Main.spriteBatch, drawPos, progreso, radio,
                    ColorOro, 0.85f * caida, seed, 12f);
                OndaLib.Shock(Main.spriteBatch, drawPos, MathHelper.Clamp(progreso * 1.15f, 0f, 1f),
                    radio * 0.72f, ColorBlancoCaliente, 0.55f * caida, seed + 7, 7f);

                LumenLib.Bloom(Main.spriteBatch, drawPos, radio * (0.35f + 0.65f * caida),
                    ColorOroClaro, 0.45f * caida, 3);

                Main.spriteBatch.End();
            }
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
