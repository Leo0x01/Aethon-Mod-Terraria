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
    /// EnjambrePrismaticoProjectile — v6.26 — EL ENJAMBRE DE ABISPAJAS.
    ///
    /// EL PROYECTIL ES DOS FASES:
    ///
    ///   FASE NÚCLEO (0..26 ticks): un corazón de luz prismático vuela
    ///   hacia el cursor (deceleración suave) — bloom de drift + destello.
    ///
    ///   FASE ENJAMBRE (26..840): el núcleo REVIENTA en 12 ABISPAJAS que
    ///   viven COMO SUB-ENTIDADES LÓGICAS DENTRO de este proyectil (el
    ///   patrón de las cadenas de SinfoniaPrimordialProjectile — NADA de
    ///   proyectiles extra): arrays paralelos de posición/velocidad/hue/
    ///   cooldown/trail. Física BOIDS por avispa:
    ///     · COHESIÓN hacia el centroide del enjambre;
    ///     · SEPARACIÓN de las vecinas (radio 26 px);
    ///     · ATRACCIÓN hacia el OBJETIVO (el enemigo más cercano, 800 px);
    ///   velocidad tope 7 px/t, estelas de 6 puntos.
    ///
    ///   EL PICOTEO: cada avispa tiene su COOLDOWN individual (45 ticks);
    ///   al tocar al objetivo (26 px) pica: destello + daño. En MP el
    ///   daño del enjambre lo aplica la AUTORIDAD alrededor del CENTROIDE
    ///   (que sigue al objetivo — el proyectil NO sincroniza las avispas:
    ///   el visual es cliente, el daño es SimpleStrikeNPC MP-seguro).
    ///
    ///   LA MIGRACIÓN: cuando el objetivo muere, el enjambre elige al
    ///   siguiente enemigo más cercano (la nube entera se traslada).
    ///
    /// Vida total 840 ticks = 14 s; al final el enjambre asciende y se
    /// disuelve (fade + notas de luz sueltas).
    /// </summary>
    public class EnjambrePrismaticoProjectile : ModProjectile
    {
        /// <summary>Vida total del enjambre: 840 ticks = 14 s.</summary>
        public const int TotalTicks = 840;

        /// <summary>Ticks de vuelo del NÚCLEO antes de reventar.</summary>
        public const int NucleoTicks = 26;

        /// <summary>El número de ABISPAJAS del enjambre.</summary>
        public const int Avispas = 12;

        /// <summary>Cooldown de picadura POR AVISPA (ticks).</summary>
        public const int PicaCooldown = 45;

        /// <summary>El radio de picadura (px).</summary>
        private const float RadioPica = 26f;

        /// <summary>El tope de velocidad de una avispa (px/t).</summary>
        private const float TopeVel = 7f;

        // === EL ESTADO DEL ENJAMBRE (arrays paralelos — sub-entidades) ===
        private readonly Vector2[] _pos = new Vector2[Avispas];
        private readonly Vector2[] _vel = new Vector2[Avispas];
        private readonly float[] _hue = new float[Avispas];
        private readonly float[] _cool = new float[Avispas];
        private readonly float[] _flap = new float[Avispas];
        /// <summary>Las estelas cortas: 3 puntos históricos por avispa.</summary>
        private readonly Vector2[][] _trail = new Vector2[Avispas][];

        private float _age;
        private bool _nacioEnjambre;
        private int _target = -1;

        /// <summary>Semilla determinista (ai[1]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>El daño base del arma (ai[2]).</summary>
        private float BaseDamage => Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;

        /// <summary>¿Estamos en la fase NÚCLEO?</summary>
        private bool EsNucleo => !_nacioEnjambre;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // El CENTROIDE del enjambre (el dibujo es 100% código).
            Projectile.width = 20;
            Projectile.height = 20;
            // Daño 100% manual (el patrón de la casa): el picoteo.
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
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            Projectile.ai[2] = Projectile.damage;
            _age = 0f;
            for (int i = 0; i < Avispas; i++)
            {
                _hue[i] = i / (float)Avispas;                 // el arcoíris repartido
                _cool[i] = PicaCooldown * 0.5f;               // nacen medio listas
                _flap[i] = i * MathHelper.TwoPi / Avispas;    // aleteo desfasado
                _trail[i] = new Vector2[3];
            }
        }

        // ==================================================================
        //  LA MÁQUINA — núcleo, enjambre, picoteo y migración
        // ==================================================================

        public override void AI()
        {
            _age += 1f;

            // ================================================================
            //  FASE 1 · EL NÚCLEO (vuela y muere joven)
            // ================================================================
            if (EsNucleo)
            {
                Projectile.velocity *= 0.965f;
                EstelaLib.Track(Projectile.whoAmI, 14).Push(Projectile.Center);

                if (_age >= NucleoTicks)
                    Reventar();
                return;
            }

            // ================================================================
            //  FASE 2 · EL ENJAMBRE (boids + picoteo + migración)
            // ================================================================

            // === EL OBJETIVO (con MIGRACIÓN al morir) ===
            NPC presa = Main.npc[Math.Clamp(_target, 0, Main.maxNPCs)];
            if (presa == null || !presa.active || !presa.CanBeChasedBy() ||
                (presa.Center - Projectile.Center).Length() > 900f)
            {
                presa = BuscarPresa();
                _target = presa?.whoAmI ?? -1;
            }

            // === EL CENTROIDE sigue al objetivo (con vaivén de enjambre) ===
            // (sin presa: ronda su posición actual — el panal a la deriva.)
            Vector2 meta = presa != null ? presa.Center : Projectile.Center;
            Vector2 haciaMeta = meta - Projectile.Center;
            if (haciaMeta.Length() > 40f)
                Projectile.velocity += Vector2.Normalize(haciaMeta) * 0.35f;
            else
                Projectile.velocity *= 0.93f;
            if (Projectile.velocity.Length() > 5f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 5f;

            // === LAS AVISPAS (boids por unidad — el corazón del enjambre) ===
            Vector2 centroide = Centroide();
            int vivas = 0;
            for (int i = 0; i < Avispas; i++)
            {
                _trail[i][2] = _trail[i][1];
                _trail[i][1] = _trail[i][0];
                _trail[i][0] = _pos[i];

                if (_cool[i] > 0f) _cool[i] -= 1f;
                _flap[i] += 0.55f;

                // 1) COHESIÓN: hacia el centroide.
                Vector2 fuerza = (centroide - _pos[i]) * 0.010f;
                // 2) SEPARACIÓN: lejos de las vecinas (26 px).
                for (int j = 0; j < Avispas; j++)
                {
                    if (j == i) continue;
                    Vector2 d = _pos[i] - _pos[j];
                    float len = d.Length();
                    if (len > 0.01f && len < 26f)
                        fuerza += d / len * (26f - len) * 0.055f;
                }
                // 3) OBJETIVO: cada avispa quiere su sitio de picadura (un
                //    anillo de ataque alrededor de la presa — así ORBITAN).
                if (presa != null)
                {
                    float angAtaque = _flap[i] * 0.09f + i * MathHelper.TwoPi / Avispas;
                    Vector2 sitio = presa.Center + new Vector2(
                        MathF.Cos(angAtaque), MathF.Sin(angAtaque)) * 34f;
                    fuerza += (sitio - _pos[i]) * 0.016f;
                }
                else
                {
                    // SIN presa: orbitar el centroide (el panal vivo).
                    float angPanal = _flap[i] * 0.05f + i * MathHelper.TwoPi / Avispas;
                    Vector2 sitio = centroide + new Vector2(
                        MathF.Cos(angPanal), MathF.Sin(angPanal) * 0.7f) * 42f;
                    fuerza += (sitio - _pos[i]) * 0.012f;
                }

                _vel[i] += fuerza;
                // El ALA también empuja: vaivén perpendicular (el vuelo de
                // avispa ZIGZAGUEA — nadie vuela en línea recta).
                _vel[i] += new Vector2(-_vel[i].Y, _vel[i].X) * 0.06f *
                    MathF.Sin(_flap[i] * 1.7f);

                if (_vel[i].Length() > TopeVel)
                    _vel[i] = Vector2.Normalize(_vel[i]) * TopeVel;
                _pos[i] += _vel[i];
                vivas++;

                // 4) EL PICOTEO (visual cliente + cooldown individual).
                if (presa != null && _cool[i] <= 0f &&
                    (_pos[i] - presa.Center).Length() < RadioPica)
                {
                    _cool[i] = PicaCooldown;
                    if (Main.netMode != NetmodeID.Server)
                        PicaduraVisual(i);
                }
            }
            _ = vivas;

            // === EL DAÑO DE AUTORIDAD (MP-seguro: el enjambre pica EN SERIO
            //     alrededor del centroide que persigue a la presa) ===
            if (Main.netMode != NetmodeID.MultiplayerClient && _age % 9f == 0f && presa != null)
            {
                int dmg = Math.Max(1, (int)(BaseDamage * 0.26f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    if ((npc.Center - Projectile.Center).Length() > 170f) continue;
                    npc.SimpleStrikeNPC(dmg, npc.direction, false, 1.5f, DamageClass.Magic);
                }
            }

            // === EL ZUMBIDO periódico (el enjambre SUENA) ===
            if (Main.netMode != NetmodeID.Server && _age % 90f == 0f)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item93.WithPitchOffset(-0.15f).WithVolumeScale(0.35f),
                    Projectile.Center);

            // === LA LUZ PRISMÁTICA (el arcoíris entero) ===
            if (Main.netMode != NetmodeID.Server && _age % 3f == 0f)
            {
                Color c = LumenLib.Hue(Main.GlobalTimeWrappedHourly * 0.2f, 0.6f);
                Lighting.AddLight(Projectile.Center,
                    c.R / 255f * 0.5f, c.G / 255f * 0.5f, c.B / 255f * 0.5f);
            }
        }

        /// <summary>EL NÚCLEO REVIENTA: nace el enjambre.</summary>
        private void Reventar()
        {
            _nacioEnjambre = true;

            // Las avispas nacen en un anillo explosivo alrededor del núcleo.
            for (int i = 0; i < Avispas; i++)
            {
                float ang = i / (float)Avispas * MathHelper.TwoPi;
                _pos[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 10f;
                _vel[i] = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 4.5f;
                _trail[i][0] = _trail[i][1] = _trail[i][2] = _pos[i];
            }

            // El impulso inicial del enjambre: el del núcleo.
            Projectile.velocity *= 0.4f;

            if (Main.netMode == NetmodeID.Server) return;

            // LA EXPLOSIÓN del núcleo (visual + camera + pulso).
            OndaLib.Kick(3f, 10);
            ParticlePresets.RingPulse(Projectile.Center, 130f,
                new Color(255, 170, 240, 200), 30);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item93.WithPitchOffset(0.3f), Projectile.Center);
        }

        /// <summary>Busca la presa más cercana (800 px).</summary>
        private NPC BuscarPresa()
        {
            NPC best = null;
            float bestDist = 800f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        /// <summary>El centroide del enjambre.</summary>
        private Vector2 Centroide()
        {
            Vector2 sum = Vector2.Zero;
            for (int i = 0; i < Avispas; i++) sum += _pos[i];
            return sum / Avispas;
        }

        /// <summary>El destello de UNA picadura (visual cliente).</summary>
        private void PicaduraVisual(int i)
        {
            Color c = LumenLib.Hue(_hue[i], 0.65f);
            Dust d = Dust.NewDustPerfect(_pos[i], DustID.RainbowRod,
                -_vel[i] * 0.3f, 180, c, 0.6f);
            d.noGravity = true;
        }

        // ==================================================================
        //  EL ACCESO DEL RENDERER (las avispas para el pintado)
        // ==================================================================

        /// <summary>La posición de la avispa i (para el renderer).</summary>
        internal Vector2 PosAvispa(int i) => _pos[i];

        /// <summary>El hue prismático de la avispa i.</summary>
        internal float HueAvispa(int i) => _hue[i];

        /// <summary>El trail corto de la avispa i ([0] la más nueva).</summary>
        internal Vector2[] TrailAvispa(int i) => _trail[i];

        /// <summary>¿El enjambre ya nació? (fase núcleo vs enjambre).</summary>
        internal bool EnjambreVivo => _nacioEnjambre;

        /// <summary>El tick del último pica-flash de la avispa i (para el
        /// destello de picadura: cooldown restante > 42 = acabó de picar).</summary>
        internal float CoolAvispa(int i) => _cool[i];

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
                EnjambrePrismaticoRenderer.Draw(this, _age, Seed);
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
            // El enjambre SE DISUELVE: el arcoíris se suelta en motas.
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < Avispas; i++)
            {
                Color c = LumenLib.Hue(_hue[i], 0.6f);
                Dust d = Dust.NewDustPerfect(_pos[i], DustID.RainbowRod,
                    new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.8f)), 160, c, 0.7f);
                d.noGravity = true;
            }
        }
    }
}
