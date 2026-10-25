using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Escriba
{
    // ======================================================================
    //  v6.52.0 — LOS PROYECTILES DE TINTA DEL ESCRIBA: la munición de la
    //  coreografía (cada uno ES su telegraph — «el jefe ESCRIBE sus
    //  ataques en el aire antes de que ocurran», el concepto R3 §4.5).
    //
    //    · EscribaGota   — la gota de tinta (el chispazo básico)
    //    · EscribaPagina — la página que cae (su CHARCO DE SOMBRA es el
    //                      aviso del punto de impacto — el telegraph que
    //                      SE VE en el suelo)
    //    · EscribaPluma  — la pluma orbital (rodea… y dispara hacia dentro)
    //    · EscribaPared  — la pared de tinta (la jaula que cierra la arena)
    //    · EscribaVerso  — EL RAYO DEL ESCRIBA (el beam masivo hostil con
    //                      BeamLib — el cierre del círculo R2↔R3)
    // ======================================================================

    /// <summary>LA GOTA: el chispazo de tinta hostil (radiales del Borrón, fuentes de página).</summary>
    public class EscribaGota : ModProjectile
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.light = 0f;
        }

        public override void AI()
        {
            Projectile.rotation += 0.18f;
            if (Projectile.timeLeft < 30)
                Projectile.scale = Projectile.timeLeft / 30f;
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.16f, 0.10f, 0.30f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            Color tinta = new(120, 80, 200);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(tinta, 0.6f),
                new Vector2(20f, 20f) * Projectile.scale, VFXCore.SoftGlow);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(new Color(235, 228, 255), 0.75f),
                new Vector2(8f, 8f) * Projectile.scale, VFXCore.GlowOrb);
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }

    /// <summary>
    /// LA PÁGINA QUE CAE: nace arriba, apunta al suelo (ai[1] = Y del
    /// impacto) y su CHARCO DE SOMBRA avisa el punto de aterrizaje TODO
    /// el descenso — el telegraph que se ve (la receta «shadow pools»).
    /// </summary>
    public class EscribaPagina : ModProjectile
    {
        public float YImpacto { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 26;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            // LA CAÍDA: acelera suave (gravedad de papel: 0.18).
            Projectile.velocity.Y += 0.18f;
            Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y, 11f);
            Projectile.rotation = MathF.Sin(Projectile.localAI[0] * 0.11f) * 0.6f;
            Projectile.localAI[0] += 1f;

            // EL IMPACTO: al tocar la Y del suelo (o un tile).
            bool tocoSuelo = Projectile.Center.Y >= YImpacto;
            if (!tocoSuelo && Projectile.timeLeft < 298)
            {
                Vector2 abajo = new(0, 8);
                tocoSuelo = Collision.SolidCollision(Projectile.position + abajo * 2f,
                    Projectile.width, Projectile.height);
            }
            if (tocoSuelo)
            {
                // LA SALPICADA: 4 gotas en fuente + el charco muere.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 v = new Vector2(-1 + i * 0.66f, -1).RotatedByRandom(0.35f) *
                            Main.rand.NextFloat(5f, 9f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                            Projectile.Center, v,
                            ModContent.ProjectileType<EscribaGota>(),
                            (int)(Projectile.damage * 0.6f), 2f, Projectile.owner);
                    }
                }
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff,
                            new Vector2(0, -1).RotatedByRandom(MathHelper.PiOver2) *
                                Main.rand.NextFloat(2f, 6f),
                            120, new Color(110, 80, 170), 1.2f);
                        d.noGravity = true;
                    }
                }
                Projectile.Kill();
                return;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;

            // === EL CHARCO DE SOMBRA (el aviso del impacto) ===
            Vector2 suelo = new(Projectile.Center.X, YImpacto);
            float t = MathHelper.Clamp((YImpacto - Projectile.Center.Y) / 700f, 0f, 1f);
            // Crece conforme ESTÁ MÁS CERCA (1-t): el charco SE AVIVA al caer.
            float alfa = 0.18f + 0.5f * (1f - t) * (0.7f + 0.3f * MathF.Sin(tiempo * 9f));
            VFXCore.Quad(suelo, StormLib.Tint(new Color(150, 90, 230), alfa),
                new Vector2(46f, 18f), VFXCore.SoftGlow);
            VFXCore.Line(suelo - new Vector2(26f, 0), suelo + new Vector2(26f, 0),
                StormLib.Tint(new Color(190, 130, 255), alfa * 0.8f), 2.5f);

            // === LA PÁGINA (blanca, girando, con el filo de tinta) ===
            VFXCore.Quad(Projectile.Center, StormLib.Tint(new Color(244, 240, 232), 0.9f),
                new Vector2(16f, 22f), Projectile.rotation, VFXCore.SoftGlow);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(new Color(120, 80, 200), 0.5f),
                new Vector2(10f, 16f), Projectile.rotation, VFXCore.GlowOrb);

            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }

    /// <summary>
    /// LA PLUMA ORBITAL: rodea a la presa (ai[2] = whoAmI del jugador) y
    /// a los 60 t DISPARA hacia dentro — el «anillo con hueco» de la
    /// investigación (siempre queda una línea de escape detrás del jefe).
    /// LA ÓRBITA vive en ai[1] (sincronizada): todas las máquinas dibujan
    /// el MISMO anillo (la lección MP de la casa).
    /// </summary>
    public class EscribaPluma : ModProjectile
    {
        public int Fase { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        public float AnguloOrbita { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }
        public int TicksOrbita { get => (int)Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        /// <summary>El tick en que TODAS sueltan (determinista — sin aviso del jefe).</summary>
        public const int TickDisparo = 60;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
        }

        public override void AI()
        {
            Player presa = Main.player[(int)Projectile.ai[2]];
            bool presaViva = presa != null && presa.active && !presa.dead;

            if (Fase == 0)
            {
                // LA ÓRBITA: gira alrededor de la presa (0.035 rad/t).
                AnguloOrbita += 0.035f;
                Projectile.Center = (presaViva ? presa.Center : Projectile.Center) +
                    new Vector2(MathF.Cos(AnguloOrbita), MathF.Sin(AnguloOrbita)) * 250f;
                Projectile.rotation = AnguloOrbita + MathHelper.Pi;
                Projectile.velocity = Vector2.Zero;
                TicksOrbita++;

                // EL DISPARO PROPIO: al tick 60 TODAS sueltan a la vez
                // (determinista — el anillo cierra de golpe).
                if (TicksOrbita >= TickDisparo)
                {
                    Fase = 1;
                    if (presaViva)
                    {
                        Vector2 hacia = Vector2.Normalize(presa.Center - Projectile.Center);
                        if (float.IsNaN(hacia.X) || float.IsNaN(hacia.Y)) hacia = -Vector2.UnitY;
                        Projectile.velocity = hacia * 15f;
                    }
                    else Projectile.velocity = -Vector2.UnitY * 15f;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
                }
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Projectile.timeLeft < 40)
                    Projectile.scale = Projectile.timeLeft / 40f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            Color tinta = new(140, 100, 215);

            // EL CUERPO DE PLUMA: núcleo alargado + halo.
            VFXCore.Quad(Projectile.Center, StormLib.Tint(tinta, 0.55f),
                new Vector2(26f, 12f) * Projectile.scale,
                Projectile.rotation, VFXCore.SoftGlow);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(new Color(240, 236, 255), 0.8f),
                new Vector2(14f, 4f) * Projectile.scale,
                Projectile.rotation, VFXCore.GlowOrb);

            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }

    /// <summary>
    /// LA PARED DE TINTA: la jaula — telegraph de lavado (60 t) y luego
    /// zona activa dañina (su hitbox ES la pared).
    /// </summary>
    public class EscribaPared : ModProjectile
    {
        public int Fase { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 700;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 340;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
        }

        public override void AI()
        {
            // EL MOVIMIENTO DE CIERRE (ai[1] = velocidad X en décimas).
            if (Fase == 0)
                Projectile.position.X += Projectile.ai[1] * 0.1f * 0.4f;   // lento mientras avisa
            else
                Projectile.position.X += Projectile.ai[1] * 0.1f;

            if (Main.netMode != NetmodeID.Server && Fase == 0)
                Lighting.AddLight(Projectile.Center, 0.10f, 0.05f, 0.20f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;
            Color tinta = new(95, 62, 160);

            // LA COLUMNA: cinco glows apilados (la pared QUE RESPIRA).
            float alfa = Fase == 0 ? 0.25f + 0.15f * MathF.Sin(tiempo * 6f) : 0.75f;
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = Projectile.Center + new Vector2(0, (i - 2) * 68f) +
                    new Vector2(MathF.Sin(tiempo * 2.1f + i * 1.7f) * 5f, 0);
                VFXCore.Quad(pos, StormLib.Tint(tinta, alfa * 0.55f),
                    new Vector2(60f, 74f), 0f, VFXCore.SoftGlow);
            }
            // EL CORAZÓN: la línea vertical viva.
            VFXCore.Line(Projectile.Center + new Vector2(0, -170f),
                Projectile.Center + new Vector2(0, 170f),
                StormLib.Tint(new Color(200, 170, 255), alfa), 5f);

            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }

    /// <summary>
    /// EL VERSO — EL RAYO DEL ESCRIBA: el beam masivo HOSTIL (la misma
    /// BeamLib del arma del jugador, en tinta violeta — el cierre del
    /// círculo de la investigación). ai[1] = ángulo, ai[2] = barrido
    /// (rad/t), localAI[0] = fase (0 telegraph 40 t · 1 fuego 130 t).
    /// </summary>
    public class EscribaVerso : ModProjectile
    {
        public int Fase { get => (int)Projectile.localAI[0]; set => Projectile.localAI[0] = value; }
        public float Angulo { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        public const float LargoFijo = 1700f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.netImportant = true;
            Projectile.hide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // EL BARRIDO: el rayo GIRA lento (el «sweep» con inercia — el
            // jugador corre con la punta).
            Angulo += Projectile.ai[2];
            Projectile.rotation = Angulo;

            if (Fase == 0)
            {
                if (Projectile.timeLeft < 220) Fase = 1;    // 40 t de telegraph
            }

            // El daño SOLO en fase fuego (el telegraph es honrado).
            Projectile.hostile = Fase == 1;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Fase != 1) return false;
            float ignorado = 0f;
            Vector2 dir = new Vector2(MathF.Cos(Angulo), MathF.Sin(Angulo));
            Vector2 fin = Projectile.Center + dir * LargoFijo;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, fin, 26f, ref ignorado);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;
            Vector2 dir = new(MathF.Cos(Angulo), MathF.Sin(Angulo));
            Color tinta = new(150, 100, 235);

            if (Fase == 0)
            {
                // EL TELEGRAPH: la línea de aviso que LATE (BeamLib.Telegrafo).
                float fase01 = 1f - MathHelper.Clamp((Projectile.timeLeft - 220) / 40f, 0f, 1f);
                BeamLib.Telegrafo(Projectile.Center, dir, LargoFijo, tinta,
                    fase01, tiempo, 30f);
            }
            else
            {
                // EL FUEGO: la pila completa (¡el mismo motor del jugador!).
                float vida = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
                BeamLib.Rayo(Projectile.Center, dir, LargoFijo, 30f, tinta,
                    tiempo, Projectile.whoAmI * 53 + 11, false, 1.1f, 0.6f + 0.4f * vida);
            }

            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }
    }
}
