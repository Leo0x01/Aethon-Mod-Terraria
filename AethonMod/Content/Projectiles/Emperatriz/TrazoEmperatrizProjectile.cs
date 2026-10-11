using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Emperatriz
{
    /// <summary>
    /// EL TRAZO DE LA EMPERATRIZ — OPCIÓN C: EL HÍBRIDO DE LA JEFA.
    ///
    /// El streak 873 de la Emperatriz de la Luz en versión AMISTOSA: la
    /// misma AI_171 pero con los PARÁMETROS DE LA JEFA (estudio §4, la
    /// tabla 873):
    ///   · umbral de persecución 140 (persigue más pronto, deriva menos:
    ///     fricción 0.98),
    ///   · steering 0.05→0.10 — giros anchos y majestuosos de jefa,
    ///   · ATRAVIESA PAREDES (tileCollide = false, como el 873),
    ///   · 40 afterimages a escala 1.4 (la estela DOBLE del 931),
    ///   · al morir: EXPLOSIÓN RADIAL en anillo completo (la muerte del
    ///     873) además de quemar la estela.
    ///
    /// El color es el ARCOÍRIS COMPLETO de su ataque (ai[1] = matiz que
    /// rota entre disparos — cada trazo nace con el siguiente color del
    /// ciclo) y la estela DERIVA de matiz: cada fantasma un poco más
    /// adelante en el arcoíris — el rastro prismático de la jefa.
    ///
    /// EL DESBORDE (ai[2]==1): el trazo MASIVO — escala 2.8, daño ×2.5
    /// (lo pasa el item), penetra 12, explosión radial triple y
    /// temblor de pantalla. El proyectil de los 5 disparos.
    /// </summary>
    public class TrazoEmperatrizProjectile : ModProjectile
    {
        // ai[0] = índice del NPC objetivo (la IA_171 de verdad)
        // ai[1] = matiz del arcoíris (0..1 — el render vanilla: (hue+0.5)%1)
        // ai[2] = 1 → EL DESBORDE (el trazo masivo)
        // localAI[0] = ya inicializado (engorda el masivo una sola vez)

        public override void SetStaticDefaults()
        {
            // LA ESTELA DOBLE de la jefa: 40 afterimages
            ProjectileID.Sets.TrailCacheLength[Type] = 40;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = 6;               // el trazo de la jefa barre
            Projectile.friendly = true;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;         // ★ ATRAVIESA PAREDES (el 873)
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.scale = 1.4f;                // la escala de la jefa
        }

        /// <summary>El color de render del trazo (la fórmula del 931: hue+0.5, S=1, L=0.5).</summary>
        private Color ColorTrazo(float corrimiento = 0f)
            => Main.hslToRgb((Projectile.ai[1] + corrimiento + 0.5f) % 1f, 1f, 0.5f);

        public override void AI()
        {
            // === EL DESBORDE: el trazo masivo se engorda una sola vez ===
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Projectile.ai[2] == 1f)
                {
                    Projectile.scale = 2.8f;
                    Projectile.width = 60;
                    Projectile.height = 60;
                    Projectile.penetrate = 12;      // el masivo lo cruza TODO
                }
            }

            // === FASE 0: fade-in (240→220) ===
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft);

            if (Projectile.timeLeft > 140f)         // ★ el umbral de la JEFA
            {
                // === FASE 1: deriva serpenteante (240→140) ===
                float onda = MathF.Cos(Projectile.whoAmI % 6f / 6f
                    + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity *= 0.98f;       // ★ la fricción de la jefa
                Projectile.velocity = Projectile.velocity.RotatedBy(
                    onda * MathHelper.TwoPi * 0.125f / 30f);
            }
            else
            {
                // === FASE 2: persecución (140→30) ===
                int objetivo = (int)Projectile.ai[0];
                if (Main.npc.IndexInRange(objetivo) && !Main.npc[objetivo].CanBeChasedBy(Projectile))
                {
                    Projectile.ai[0] = -1f;
                    Projectile.netUpdate = true;
                }
                if (Projectile.ai[0] == -1f)
                    Projectile.ai[0] = Projectile.FindTargetWithLineOfSight(800f);

                Vector2 deseada = Projectile.velocity;
                if (Projectile.friendly && Main.npc.IndexInRange((int)Projectile.ai[0]))
                {
                    if (Projectile.timeLeft < 10) Projectile.timeLeft = 10;
                    deseada = Projectile.DirectionTo(Main.npc[(int)Projectile.ai[0]].Center) * 30f;
                }
                else
                {
                    Projectile.timeLeft--;
                }

                // ★ el steering de la jefa: 0.05→0.10, giros anchos
                float k = MathHelper.Lerp(0.05f, 0.10f,
                    Utils.GetLerpValue(140f, 30f, Projectile.timeLeft));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, deseada, k);

                Projectile.velocity *= MathHelper.Lerp(0.85f, 1f,
                    Utils.GetLerpValue(0f, 90f, Projectile.timeLeft));
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // === LA LUZ DEL ARCOÍRIS ===
            if (Main.netMode != NetmodeID.Server)
            {
                Color c = ColorTrazo();
                float fuerza = 0.6f * (Projectile.scale / 1.4f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(c.R / 255f * fuerza, c.G / 255f * fuerza, c.B / 255f * fuerza));
            }
        }

        // ================================================================
        //  EL GOLPE: daño decreciente suave + RE-ENCADENA
        // ================================================================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // −15% por enemigo golpeado (la jefa no pierde filón tan rápido)
            modifiers.FinalDamage *= MathF.Pow(0.85f, Projectile.numHits);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // RE-ENCADENA contra el siguiente enemigo
            int siguiente = Projectile.FindTargetWithLineOfSight(800f);
            if (siguiente != -1)
            {
                Projectile.ai[0] = siguiente;
                Projectile.netUpdate = true;
            }
        }

        // ================================================================
        //  LA MUERTE: la EXPLOSIÓN RADIAL del 873 + la estela quemada
        // ================================================================
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            bool masivo = Projectile.ai[2] == 1f;
            int multiplicador = masivo ? 3 : 1;
            if (masivo)
                PulsoLib.Trauma(0.40f);   // el jugo del masivo (el shake de la casa)

            // === EL ANILLO RADIAL (la firma del 873) ===
            for (int i = 0; i < 20 * multiplicador; i++)
            {
                Vector2 dir = new Vector2(1f, 0f).RotatedBy(
                    MathHelper.TwoPi * i / (20f * multiplicador));
                // la velocidad del anillo se inclina hacia la propia
                // velocidad del trazo (el impulso final del 873)
                Vector2 impulso = dir * Main.rand.NextFloat(1.5f, 4f)
                    + Projectile.velocity * 0.12f;
                Color c = ColorTrazo(i / (20f * multiplicador) * 0.15f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch,
                    impulso, 100, c, Main.rand.NextFloat(1.0f, 2.2f) * Projectile.scale * 0.7f);
                d.noGravity = true;
                d.fadeIn = 1.2f;

                Dust clon = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch,
                    impulso * 0.7f, 100, new Color(255, 255, 255), d.scale * 0.5f);
                clon.noGravity = true;
                clon.fadeIn = 1.2f;
            }

            // === LA ESTELA QUEMADA (el rastro prismático se deshace) ===
            for (int i = 0; i < Projectile.oldPos.Length; i += 2)
            {
                int cantidad = 2 - i * 2 / Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                for (int j = 0; j < cantidad; j++)
                {
                    Color c = ColorTrazo(i * 0.01f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.RainbowTorch,
                        Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f), 100, c,
                        Main.rand.NextFloat(0.8f, 1.8f));
                    d.noGravity = true;
                    d.fadeIn = 1.1f;
                }
            }
        }

        // ================================================================
        //  EL DIBUJO — la estela DOBLE de la jefa (40 prismas)
        //
        //  v6.50.101 — EL CONTRATO DEL LOTE (lección del client.log del
        //  usuario 05:38:06): el FlushAdditive cierra el lote del pase
        //  de proyectiles y deja NINGUNO abierto → el motor crashea en
        //  el siguiente EntitySpriteDraw. El patrón de la casa: cerrar
        //  → dibujar → REABRIR SIEMPRE el lote vanilla al salir.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            float escala = Projectile.scale;
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft);
            float t = Main.GameUpdateCount;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();

                // === 40 AFTERIMAGES (escala 1.4 de la jefa) — el matiz
                // DERIVA a lo largo de la estela: cada fantasma un poco
                // más adelante en el arcoíris ===
                for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    float alfaCola = (1f - i / (float)Projectile.oldPos.Length) * 0.5f * fade;
                    Color cCola = ColorTrazo(i * 0.008f);
                    VFXCore.Quad(pos, cCola * alfaCola, new Vector2(62f * escala, 62f * escala), VFXCore.GlowOrb);
                }

                // === EL NÚCLEO ===
                Color c = ColorTrazo();
                VFXCore.Quad(Projectile.Center, c * (0.9f * fade), new Vector2(74f * escala, 74f * escala), VFXCore.GlowOrb);
                VFXCore.Quad(Projectile.Center, Color.White * (0.95f * fade), new Vector2(38f * escala, 38f * escala), VFXCore.GlowOrb);

                // === LA CRUZ PULSANTE (el glow del 873 — a escala de jefa) ===
                float pulso = 0.8f * (1f + 0.2f * MathF.Cos(t * 0.25f));
                float largo = 100f * escala * pulso;
                float grosor = 9f * escala;
                VFXCore.Line(Projectile.Center - new Vector2(largo, 0f),
                    Projectile.Center + new Vector2(largo, 0f), c * (0.6f * fade), grosor);
                VFXCore.Line(Projectile.Center - new Vector2(0f, largo),
                    Projectile.Center + new Vector2(0f, largo), c * (0.6f * fade), grosor);

                Vector2 diag = new Vector2(1f, 1f);
                diag.Normalize();
                diag *= largo * 0.62f;
                VFXCore.Line(Projectile.Center - diag, Projectile.Center + diag,
                    Color.White * (0.3f * fade), 6f * escala);
                VFXCore.Line(Projectile.Center - new Vector2(diag.X, -diag.Y),
                    Projectile.Center + new Vector2(diag.X, -diag.Y),
                    Color.White * (0.3f * fade), 6f * escala);

                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();   // ★ el contrato: salir con el lote VIVO
            return false;   // TODO el dibujo es de la casa
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
