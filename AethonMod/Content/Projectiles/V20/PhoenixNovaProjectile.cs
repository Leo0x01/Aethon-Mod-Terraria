using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// PhoenixNovaProjectile — nova/llamarada solar centrada.
    ///
    /// v5.96 — SIN PARPADEO + AURA DE ÁREA CRECIENTE (dos peticiones del
    /// usuario): (1) "el brillo de PhoenixNovaStaff ya no debe parpadear —
    /// debe comenzar a crecer lentamente y su crecimiento debe estar
    /// sincronizado con el ciclo de vida del sol y con el tamaño del mismo":
    /// el PULSO sinusoidal (0.93±0.07, varias oscilaciones por segundo) y el
    /// FLASH del pico (frames 25-35) se ELIMINARON — la nova ahora CRECE DE
    /// FORMA CONTINUA: nace pequeña, se expande LENTO durante su vida y se
    /// desvanece al final (envolvente suave, CERO oscilación). El SOL ya no
    /// invoca llamaradas periódicas (SunProjectile lleva su GLOW CORONAL
    /// PERSISTENTE — cada nova de 60 frames era un PARPADEO por diseño);
    /// este proyectil queda como arma STANDALONE (PhoenixNovaStaff).
    /// (2) "todo el daño deben ser daño de área que se extienda por fuera del
    /// proyectil y crezca conforme crece, se expande y explota": aura de daño
    /// cada 0.166 s (10 ticks) cuyo radio CRECE con la nova (45→155 px a lo
    /// largo de su vida) con daño del 60% + OnFire — el daño ya no es solo el
    /// hitbox de contacto de 80px.
    ///
    /// v5.95 — histórico: nació como hija del sol (ai[2]=1, hide=true, el sol
    /// la dibujaba detrás de su disco dimensionada con su radio visual real).
    /// v5.96: el sol YA NO la invoca — ver SunProjectile (glow coronal).
    ///
    /// Físicas:
    ///   - Velocidad cero (estático). penetrate = -1. timeLeft = 60 (1 s).
    ///   - tileCollide = false. Aplica OnFire a los NPCs golpeados.
    /// </summary>
    public class PhoenixNovaProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                // Legado v5.95: hija del sol (hide — nadie la dibuja salvo el
                // propio sol). v5.96: el sol YA NO invoca llamaradas; la rama
                // queda como defensa para clones antiguos si existieran.
                if (Projectile.ai[2] == 1f)
                    Projectile.hide = true;

                float age = Projectile.ai[0];
                Projectile.ai[0] += 1f;

                // v5.78: spawn dust solo en cliente (no en server)
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        SpawnTorchDust(age);
                    }
                }

                // === v5.96 — AURA DE DAÑO DE ÁREA CRECIENTE ===
                // Petición del usuario: "todo el daño deben ser daño de área y
                // este debe extenderse por fuera del proyectil y crecer conforme
                // el proyectil crece, se expande y explota". El radio del aura
                // CRECE con la nova (45→155 px a lo largo de su vida — siempre
                // POR FUERA del hitbox de 80px de diámetro): el daño acompaña a
                // la expansión visual. Cada 10 ticks (0.166 s) + OnFire 5 s.
                if (Main.netMode != NetmodeID.MultiplayerClient &&
                    age > 4f && age % 10f == 0f)
                {
                    float progress = MathHelper.Clamp(age / 60f, 0f, 1f);
                    float auraRadius = 45f + 110f * progress;
                    int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.6f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy()) continue;
                        float dist = (npc.Center - Projectile.Center).Length();
                        if (dist > auraRadius) continue;
                        int dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                        npc.SimpleStrikeNPC(auraDamage, dir, false, 3f, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 300);
                    }
                }

                // Iluminación cálida: SIGUE el crecimiento (sin pico).
                // v5.96: la intensidad es función PURA de la edad — sube con la
                // expansión y baja solo al desvanecerse final. Sin parpadeo.
                float rise = Utils.GetLerpValue(0f, 18f, age, true);
                float fall = 1f - Utils.GetLerpValue(46f, 60f, age, true);
                float intensity = MathHelper.Clamp(rise * fall, 0f, 1f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f * intensity, 0.55f * intensity, 0.15f * intensity));
            }
            catch { }
        }

        // ================================================================
        //  Spawn DustID.Torch en direcciones radiales
        // ================================================================
        private void SpawnTorchDust(float age)
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            // Velocidad creciente con la edad (la nova se expande)
            float speed = 2f + age * 0.18f + Main.rand.NextFloat(-1f, 1f);
            Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                       (float)Math.Sin(angle) * speed);
            Color color = Main.rand.NextBool(2)
                ? new Color(255, 180, 60)
                : new Color(255, 100, 30);
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                vel, 180, color, 1.3f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        // ================================================================
        //  OnHitNPC — aplica OnFire debuff
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.OnFire, 300); // 5 segundos de OnFire
            }
            catch { }
        }

        // ================================================================
        //  RENDER
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                // Legado v5.95: hija del sol (hide=true — tML jamás la pinta).
                if (Projectile.ai[2] == 1f)
                    return false;

                // === STANDALONE (PhoenixNovaStaff): se dibuja a sí misma ===
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                DrawFlareSprites(Projectile, drawPos, 56f, 0f, true);

                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo solo si una excepción cortó un Begin a
                // medias (el path normal deja las capas balanceadas).
                try { Main.spriteBatch.End(); } catch { }
            }

            // Restauración EXACTA del estado que tML espera tras PreDraw.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        // ================================================================
        //  SPRITES DE LA NOVA (v5.96 — crecimiento continuo, sin parpadeo)
        // ================================================================

        /// <summary>
        /// v5.96 — Sprites de la nova standalone: CRECIMIENTO CONTINUO Y LENTO
        /// (petición del usuario: "el brillo ya no debe parpadear — debe
        /// comenzar a crecer lentamente"). El halo nace CONTENIDO (0.75×starR,
        /// apenas asoma del hitbox) y crece hasta 2.3×; el núcleo caliente
        /// crece de 0.8× a 1.25×. La envolvente de brillo es función PURA de
        /// la edad: sube en los primeros 12 frames (encendido), se mantiene
        /// plena durante la expansión y solo baja en los últimos 8 frames
        /// (desvanecimiento final) — CERO oscilación, CERO flashes.
        /// Requiere un batch ADITIVO ya abierto — lo gestiona el llamador.
        /// </summary>
        internal static void DrawFlareSprites(Projectile p, Vector2 drawPos, float starR,
            float redGiant, bool standalone)
        {
            Texture2D softGlow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D glowCircleWhite = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/GlowCircleWhite").Value;
            if (softGlow == null || glowCircleWhite == null) return;

            float age = p.ai[0];
            float progress = MathHelper.Clamp(age / 60f, 0f, 1f);

            // v5.96 — Envolvente LISA: encendido (12f) → plena → desvanecido
            // (últimos 8 frames). El pulso sinusoidal y el flash del pico de
            // la v5.95 eran EL PARPADEO — eliminados por petición del usuario.
            float rise = Utils.GetLerpValue(0f, 12f, age, true);
            float fall = 1f - Utils.GetLerpValue(52f, 60f, age, true);
            float vis = MathHelper.Clamp(rise * fall, 0f, 1f);

            // Crecimiento LENTO y CONTINUO: el halo comienza CONTENIDO (0.75×)
            // y llega a 2.3× — la nova se EXPANDE, no parpadea.
            float expand = 0.75f + 1.55f * progress;

            // Paleta cálida: naranja solar → ROJO de la gigante.
            Color halo = Color.Lerp(new Color(255, 150, 55, 195), new Color(255, 65, 25, 195), redGiant);
            Color core = Color.Lerp(new Color(255, 235, 170, 225), new Color(255, 135, 90, 225), redGiant);
            // v5.96: el brillo sigue a la envolvente suave (×vis).
            halo *= vis;
            core *= vis;

            Vector2 glowOrigin = softGlow.Size() * 0.5f;

            // === HALO: nace contenido y crece hasta 2.3× ===
            float haloScale = starR * expand / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin,
                haloScale, SpriteEffects.None, 0f);

            // === NÚCLEO CALIENTE: crece de 0.8× a 1.25× ===
            float coreScale = starR * (0.8f + 0.45f * progress) / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin,
                coreScale, SpriteEffects.None, 0f);

            // v5.95 — histórico: aquí vivía el FLASH del pico (frames 25-35,
            // rim blanco sobre el limbo). v5.96: ELIMINADO — era el segundo
            // componente del parpadeo. La nova simplemente CRECE y se apaga.
        }
    }
}
