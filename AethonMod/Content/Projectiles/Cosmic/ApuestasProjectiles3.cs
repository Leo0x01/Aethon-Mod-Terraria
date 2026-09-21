using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Players;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// TajoGuadanaProjectile — v6.42 — APUESTA 3: LA GUADAÑA DEL DESGARRO.
    ///
    /// EL CRECIENTE: el tajo de la guadaña (TajoLib — el creciente de la
    /// casa, nacido para los tajos del anime): vuela 220 px en la
    /// dirección del tajo y DONDE muere (impacto, alcance o tile) abre
    /// LA FISURA — la herida vertical que queda en el mundo.
    ///
    /// EL HAMBRE: si el jugador traga daño devorado por una fisura
    /// anterior, el creciente nace TEÑIDO de carmesí profundo y con
    /// cuentas de apetito orbitando la hoja (el daño extra se aplicó
    /// en el disparo — esto es la lectura visual).
    /// </summary>
    public class TajoGuadanaProjectile : ModProjectile
    {
        public const float AlcanceTajo = 220f;

        private float _recorrido;

        private int Seed => Math.Max(1, Projectile.identity + 859);

        private static readonly Color HaloTajo = new(255, 120, 90);
        private static readonly Color NucleoTajo = new(255, 240, 230);
        private static readonly Color HaloHambre = new(200, 20, 40);
        private static readonly Color NucleoHambre = new(255, 140, 120);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            _recorrido += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.3f, 0.25f) * 0.8f);

            if (_recorrido >= AlcanceTajo)
                AbrirFisura();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            AbrirFisura();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // La fisura nace DONDE mordió el tajo (la causalidad del arma).
            AbrirFisura();
        }

        public override void OnKill(int timeLeft)
        {
            // Muerte de verdad (agotó tiempo sin abrir): también hiere.
            if (_recorrido < AlcanceTajo)
                AbrirFisura();
        }

        private void AbrirFisura()
        {
            if (Projectile.ai[0] > 0.5f) return;    // ya abrió (guard de reentrada)
            Projectile.ai[0] = 1f;

            // v6.50.2 — FIX (host sordo a su fisura): AbrirFisura corre en
            // todas las máquinas con copia del tajo — el viejo `!= Server`
            // callaba al HOST (netMode 1 CON pantalla). "Con pantalla" =
            // !Main.dedServ.
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.4f, Pitch = -0.3f },
                    Projectile.Center);

            if (Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.SinglePlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<FisuraDesgarroProjectile>(),
                    Projectile.damage, 0f, Projectile.owner);
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // EL CRECIENTE: arco de ±0.55 rad mirando al vuelo.
                float edad = Math.Min(1f, (60 - Projectile.timeLeft) / 8f);   // reveal
                bool hambre = Main.player[Projectile.owner].GetModPlayer<ApuestasPlayer>()
                    .HambreGuadana > 0;
                TajoLib.Tajo(Projectile.Center - Main.screenPosition, 52f,
                    Projectile.rotation - 0.55f, Projectile.rotation + 0.55f,
                    edad, 1f, 15f,
                    hambre ? HaloHambre : HaloTajo,
                    hambre ? NucleoHambre : NucleoTajo,
                    Seed, Main.GlobalTimeWrappedHourly);

                // LAS CUENTAS DE APETITO: el hambre orbita la hoja.
                if (hambre)
                {
                    Texture2D glow = VFXCore.SoftGlow;
                    int cuentas = Math.Min(6, 1 + Main.player[Projectile.owner]
                        .GetModPlayer<ApuestasPlayer>().HambreGuadana / 25);
                    for (int i = 0; i < cuentas; i++)
                    {
                        float ang = Main.GlobalTimeWrappedHourly * 4f + i / (float)cuentas * MathHelper.TwoPi;
                        Vector2 orbita = Projectile.Center - Main.screenPosition +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 58f;
                        Main.spriteBatch.Draw(glow, orbita, null, HaloHambre * 0.8f,
                            ang, glow.Size() * 0.5f, new Vector2(9f, 9f) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }
    }

    /// <summary>
    /// FisuraDesgarroProjectile — v6.42/v6.43 — LA FISURA VERTICAL DE REALIDAD.
    ///
    /// LA HERIDA que el tajo deja en el mundo: una grieta carmesí de
    /// 16×128 px que vive 120 ticks (3·τ de la relajación de fractura
    /// — el "2 segundos" del diseño son la física hablando) y hace DOS
    /// cosas:
    ///
    ///   · MUERDE: todo lo que cruza LA GRIETA DE VERDAD recibe 0,3×
    ///     cada 10 ticks — desde v6.43 la detección es
    ///     FormaLib.NPCEnSegmento sobre el eje vertical de la herida con
    ///     el grosor REAL de 16 px (la sección eficaz de la FISURA, no
    ///     un rectángulo generoso: el AABB del NPC contra la franja de
    ///     la grieta, por el chequeo de línea del motor).
    ///   · TRAGA: los proyectiles ENEMIGOS que cruzan su banda (±32 px,
    ///     MISMO radio de siempre — ahora FormaLib.ProyectilHostilEnBanda)
    ///     desaparecen — la fisura se los come y su daño SE SUMA al
    ///     próximo tajo del portador (EL HAMBRE, con tope de 2× el
    ///     daño del arma: devorar spam no puede trivializar a los jefes).
    ///
    /// El borde rasgado con dientes congelados (CaminoDesgarro — la
    /// costura coincide diente a diente) y las chispas de anomalía.
    /// </summary>
    public class FisuraDesgarroProjectile : ModProjectile
    {
        public const int Vida = 120;
        public const float LargoFisura = 128f;
        public const float Banda = 32f;

        /// <summary>
        /// v6.43 — El ANCHO REAL de la herida (16 px, el ancho del sprite
        /// de la fisura): el grosor de la cápsula con la que MUERDE vía
        /// <see cref="FormaLib.NPCEnSegmento"/> — la sección eficaz real
        /// de la grieta. No confundir con <see cref="Banda"/>: esa es la
        /// calle ANCHA de devorar proyectiles (±32 px), que sigue igual.
        /// </summary>
        public const float GrosorMordida = 16f;

        private int _edad;

        private int Seed => Math.Max(1, Projectile.identity + 1049);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 128;
            Projectile.tileCollide = false;
            Projectile.friendly = false;        // la mordida es manual (GolpeMotor, v6.50)
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            _edad++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(0.7f, 0.12f, 0.15f) * 0.7f);

            // La banda de la fisura: de la punta superior a la inferior.
            Vector2 arriba = Projectile.Center - Vector2.UnitY * (LargoFisura * 0.5f);
            Vector2 abajo = Projectile.Center + Vector2.UnitY * (LargoFisura * 0.5f);

            // === LA MORDIDA (v6.50 — GolpeMotor: el cauce del motor; EsObjetivo, 0,3× cada 10 ticks). ===
            // v6.43 — LA SECCIÓN EFICAZ REAL: antes "centro del NPC a menos
            // de 32+ancho·0.35 px" (un círculo generoso que sobre-mordía);
            // ahora el AABB del NPC contra LA GRIETA de verdad — el eje
            // vertical de la herida con su grosor real de 16 px, por el
            // chequeo de línea del motor (broad-phase + AABB girado).
            // MISMO daño, MISMA cadencia: solo cambió la geometría.
            if (Main.myPlayer == Projectile.owner && _edad % 10 == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * 0.3f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (!FormaLib.NPCEnSegmento(npc, arriba, abajo, GrosorMordida)) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                }
            }

            // === LA TRAGA: devorar proyectiles hostiles que crucen. ===
            // v6.43 — la banda de ±32 px ahora vive en la librería:
            // FormaLib.ProyectilHostilEnBanda (ancho total 64 = Banda·2 →
            // MISMO radio; el filtro hostil && daño > 0 — los telegraphs
            // damage=0 quedan fuera por diseño — también vive allí; la
            // guardia friendly de la doble bandera se mantiene aquí).
            // v6.50.2 — FIX (la traga solo vivía en la pantalla del dueño):
            // el `p.Kill()` de la bala devorada corre ahora en la AUTORIDAD
            // (SP + server — el patrón de los agujeros, CosmicBlackHole
            // ~L160): en el dedicado la bala muere de verdad y deja de
            // dañar. El bookkeeping del HAMBRE y el FX se quedan en el
            // cliente dueño (donde estaban — el daño del próximo tajo se
            // computa ahí).
            if (_edad % 2 == 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.friendly) continue;
                    if (!FormaLib.ProyectilHostilEnBanda(p, arriba, abajo, Banda * 2f)) continue;

                    // LA DEVORACIÓN DE VERDAD — la AUTORIDAD mata (el Kill de
                    // un cliente no viaja: verificado en el IL) y el CLIENTE
                    // DUEÑO también tumba SU réplica local: el Hambre se
                    // cuenta ahí — sin el Kill local la MISMA bala seguiría
                    // alimentando el hambre cada 2 ticks y podría seguir
                    // dañando al dueño (el daño hostil→jugador se evalúa en
                    // su propio cliente). En SP la condición es una sola
                    // (autoridad = dueño) → nunca hay doble Kill.
                    if (Main.netMode != NetmodeID.MultiplayerClient ||
                        Main.myPlayer == Projectile.owner)
                        p.Kill();

                    if (Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.SinglePlayer)
                    {
                        // EL HAMBRE crece (tope 2× el daño del arma — el diseño).
                        ApuestasPlayer ap = Main.player[Projectile.owner].GetModPlayer<ApuestasPlayer>();
                        int tope = Projectile.damage * 2;
                        ap.HambreGuadana = Math.Min(tope, ap.HambreGuadana + p.damage);

                        // v6.50.2 — el FX de la devoración también era víctima
                        // del gate netMode: el HOST (listen server, dueño de
                        // la guadaña) no oía el tragón. "Con pantalla" es
                        // !Main.dedServ.
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.3f, Pitch = 0.5f },
                                Projectile.Center);
                            for (int k = 0; k < 6; k++)
                            {
                                Dust d = Dust.NewDustPerfect(p.Center, DustID.Crimson);
                                d.velocity = (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 2.5f;
                                d.scale = 1f;
                                d.noGravity = true;
                            }
                        }
                    }
                }
            }

            // Las chispas de anomalía de la herida (cada 18 ticks, 2).
            if (Main.netMode != NetmodeID.Server && _edad % 18 == 0)
            {
                float t = VFXCore.Hash01(Seed, _edad, 0) * 2f - 1f;
                Vector2 punto = Projectile.Center + Vector2.UnitY * (t * LargoFisura * 0.5f);
                Dust d = Dust.NewDustPerfect(punto, DustID.CrimsonTorch);
                d.velocity = Vector2.UnitX * (t > 0 ? 1 : -1) * 1.2f;
                d.scale = 0.8f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.43 — LA LUZ DE LAS FORMAS (solo con FormaLib.Depuracion):
            // la mordida REAL (la grieta de 16 px) en cian y la banda de la
            // traga (±32 px) en carmesí — para VER la sección eficaz de
            // verdad, no la que uno imagina.
            Vector2 punta = Vector2.UnitY * (LargoFisura * 0.5f);
            FormaLib.DepurarDibujar(Projectile.Center - punta, Projectile.Center + punta,
                GrosorMordida);
            FormaLib.DepurarDibujar(Projectile.Center - punta, Projectile.Center + punta,
                Banda * 2f, new Color(255, 60, 60, 140));

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 arriba = Projectile.Center - Main.screenPosition - Vector2.UnitY * (LargoFisura * 0.5f);
                float progress = _edad / (float)Vida;   // 0→1: la grieta nace y se apaga

                // LA HERIDA rasgada (dientes congelados, costura exacta).
                Vector2[] camino = RiftLib.CaminoDesgarro(arriba, Vector2.UnitY, LargoFisura,
                    Seed, time, 0.55f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                RiftLib.Grieta(Main.spriteBatch, camino, progress, 16f, RiftPaletas.Carmesi,
                    0.95f, Seed, time, plano: true, chispas: true);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }
    }
}
