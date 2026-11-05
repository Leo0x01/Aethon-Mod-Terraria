using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles.Sombras
{
    // ======================================================================
    //  SombraPagina4Projectile — v6.100 — LA COPIA FORJADA DEL
    //  ORIGINAL, VESTIDA CON TODAS LAS HERRAMIENTAS.
    //
    //  LA LETRA DEL USUARIO: «crea la Sombra de la Página 4, una copia
    //  de la Sombra de la Página, hecha con la forja, las técnicas VFX
    //  y el motor de píxeles del taller de efectos». EL RITUAL ES EL
    //  DEL ORIGINAL (La Sombra de la Página, tomo I): el tentáculo de
    //  sombra EMERGE del portador, CAZA a la presa NO IMPORTA LA
    //  DISTANCIA, Muerde y DRENA 3% cada 6 t — y a 1 de vida, LA
    //  DEVORACIÓN (el festín de las sombras con su loot).
    //
    //  LO QUE LE VISTE LA FORJA (las herramientas de la casa):
    //   · LA GARGANTA DEL VÓRTICE — el motor de píxeles VIVO en la
    //     punta (GargantaVortice: el horizonte de sucesos con sus tres
    //     brazos de acreción) GIRANDO CON EL RUMBO del tentáculo: la
    //     boca de la sombra ES el vórtice.
    //   · EL VELO DE RUIDO — la lámina de ruido procedural sobre todo
    //     el cuerpo del tentáculo (el manto de sombra).
    //   · LA MASA VIVA — SombrasLib.ColumnaViva + Masa + OjosDeMasa +
    //     la BRUMA en dos capas (el cuerpo de la casa).
    //   · EL ANILLO DE TINTA — la presa mordida queda MARCADA con el
    //     doble anillo (violeta + teal) — el sello del festín.
    //
    //  LAS FASES: EMERGER (el brote DeGolpe, 26 t) → CAZA (el
    //  steering: gira máx 0.26 rad/t hacia la presa SIEMPRE) →
    //  MORDISCO (clavado a la presa: marca + dren 3%/6t → a 1 de vida
    //  FaucesGlobalNPC.Iniciar LA DEVORACIÓN) → DISIPAR (se deshace).
    // ======================================================================
    public class SombraPagina4Projectile : ModProjectile
    {
        /// <summary>FASE: el brote emergiendo del portador.</summary>
        private const byte FASE_EMERGER = 0;

        /// <summary>FASE: la caza de la presa.</summary>
        private const byte FASE_CAZA = 1;

        /// <summary>FASE: el mordisco drenando.</summary>
        private const byte FASE_MORDISCO = 2;

        /// <summary>FASE: la disipación.</summary>
        private const byte FASE_DISIPAR = 3;

        // EL MOTOR de píxeles: la garganta del vórtice (la boca).
        private PixelFxPlayer _motor;

        private static Texture2D[] _ruidoCache;

        /// <summary>LA PRESA (ai[0] = whoAmI+1; 0 = libre).</summary>
        private NPC Presa
        {
            get
            {
                if (Projectile.ai[0] <= 0f)
                    return null;
                return Main.npc[(int)Projectile.ai[0] - 1];
            }
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.extraUpdates = 2;          // la caza corre al triple
        }

        /// <summary>LA RAÍZ de la sombra: el pecho del portador.</summary>
        private Vector2 RaizDeSombra(Player dueño) => dueño.MountedCenter;

        /// <summary>LA SOMBRA PROYECTADA en el suelo (para el charco):
        /// baja hasta el primer sólido (34 tiles de caída).</summary>
        private Vector2 SombraProyectada(Player dueño)
        {
            Vector2 desde = dueño.MountedCenter + new Vector2(0f, dueño.height * 0.45f);
            int tx = (int)(desde.X / 16f);
            int ty = (int)(desde.Y / 16f);
            int i;
            for (i = 0; i < 34; i++)
            {
                if (WorldGen.SolidTile(tx, ty, false))
                    break;
                ty++;
            }
            if (i >= 34)
                return desde;                     // no hay suelo: sin charco
            return new Vector2(tx * 16f + 8f, ty * 16f - 4f);
        }

        /// <summary>EL MOTOR: LA GARGANTA DEL VÓRTICE — el horizonte de
        /// sucesos con sus tres brazos de acreción, la boca de la sombra.</summary>
        private PixelFxPlayer Motor()
        {
            if (_motor == null)
                _motor = new PixelFxPlayer(
                    PixelFxCasas.GargantaVortice(444,
                        PxColor.Rgb(150, 96, 235), PxColor.Rgb(28, 16, 46)),
                    1.35f);
            return _motor;
        }

        public override void AI()
        {
            Projectile.ai[2] += 1f;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            // EL MOTOR late (solo cliente) — en el mordisco se ENGRANDECE
            // (1.7: la boca abierta sobre la presa).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Motor().Tick();
                if (fase == FASE_DISIPAR)
                    Motor().Fx.Stop();
                Motor().Escala = fase == FASE_MORDISCO ? 1.7f : 1.35f;
                Lighting.AddLight(Projectile.Center, new Vector3(0.58f, 0.36f, 0.92f) * 0.9f);
            }

            bool viva = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !viva)
            {
                // LA PRESA MURIÓ (o nunca hubo): la sombra se disipa.
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0f;
                fase = FASE_DISIPAR;
                t = 0f;
            }

            switch (fase)
            {
                // ========================================================
                //  FASE 0 — EL EMERGER: el brote sale del pecho del
                //  portador hacia la presa (la curva DeGolpe de la casa).
                // ========================================================
                case FASE_EMERGER:
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? Utils.SafeNormalize(presa.Center - raiz, -Vector2.UnitY)
                        : -Vector2.UnitY;
                    float brote = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (30f + 170f * brote);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3f)
                        Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.75f),
                            Projectile.Center);
                    if (t >= 26f)
                    {
                        Projectile.ai[1] = FASE_CAZA;
                        Projectile.ai[2] = 0f;
                    }
                    break;
                }

                // ========================================================
                //  FASE 1 — LA CAZA: gira hacia la presa SIEMPRE (máx
                //  0.26 rad por tick — SIEMPRE llega, la regla del tomo).
                // ========================================================
                case FASE_CAZA:
                    if (presa != null && viva)
                    {
                        Vector2 hacia = Utils.SafeNormalize(presa.Center - Projectile.Center, Vector2.UnitX);
                        float rapidez = MathHelper.Clamp(
                            Vector2.Distance(presa.Center, Projectile.Center) * 0.05f, 20f, 52f);
                        float rumboActual = Projectile.velocity.ToRotation();
                        float rumboPresa = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(rumboPresa - rumboActual);
                        Projectile.velocity = (rumboActual + MathHelper.Clamp(giro, -0.26f, 0.26f))
                            .ToRotationVector2() * rapidez;
                        Projectile.rotation = rumboPresa;

                        // LA MARCA del festín cada 15 t (si es jefe).
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL CONTACTO: el mordisco.
                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0f;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f),
                                Projectile.Center);
                        }
                    }
                    if (t > 600f)
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0f;
                    }
                    break;

                // ========================================================
                //  FASE 2 — EL MORDISCO: clavado a la presa, DRENA 3%
                //  cada 6 t — y al 1 de vida, LA DEVORACIÓN.
                // ========================================================
                case FASE_MORDISCO:
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            // Otro tentáculo llegó primero: se disipa.
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0f;
                            break;
                        }
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL DREN (solo servidor/SP): 3% de la vida actual
                        // (mínimo 25) cada 6 t — al 1 de vida, EL FESTÍN.
                        if (Main.netMode == NetmodeID.MultiplayerClient || t <= 18f || t % 6f != 0f)
                            break;
                        NPC cabeza = FaucesGlobalNPC.DueñoDelPool(presa);
                        if (cabeza == null || !cabeza.active || cabeza.life <= 1)
                            break;
                        float quitar = MathF.Max(cabeza.life * 0.03f, 25f);
                        if (cabeza.life - quitar <= 1f)
                        {
                            FaucesGlobalNPC.Iniciar(cabeza, 3);   // LA DEVORACIÓN
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0f;
                            break;
                        }
                        cabeza.life -= (int)quitar;
                        cabeza.netUpdate = true;
                        if (t % 36f == 0f)
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.1f).WithVolumeScale(0.5f),
                                Projectile.Center);
                    }
                    else
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0f;
                    }
                    break;

                // ========================================================
                //  FASE 3 — LA DISIPACIÓN: se deshace en bruma.
                // ========================================================
                case FASE_DISIPAR:
                {
                    if (t == 1f)
                        Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f),
                            Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44f)
                        Projectile.Kill();
                    break;
                }
            }

            // EL RASTRO de sombra (la caída de tinta del cuerpo).
            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= 1f && Main.rand.NextBool(6))
            {
                int polvo = Dust.NewDust(Projectile.Center - new Vector2(24f, 24f),
                    48, 48, 27, 0f, -0.3f, 128, default, 0.55f);
                Main.dust[polvo].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            _motor?.Dispose();
            _motor = null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EN LA CAZA el cuerpo es la COLUMNA ENTERA (la curva viva
            // del tentáculo desde la raíz hasta la cabeza), no la caja.
            if (Projectile.ai[1] != FASE_CAZA)
                return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2[] columna = SombrasLib.Columna(RaizDeSombra(dueño), Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 17 + 5, 10, 0.24f);
            float punto = 0f;
            for (int i = 1; i < columna.Length; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        columna[i - 1], columna[i], 8f, ref punto))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
                return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawTodo();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>EL VESTIDO COMPLETO (la masa viva + el velo + la
        /// garganta del vórtice + el anillo de la presa).</summary>
        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            Vector2 raiz = RaizDeSombra(dueño);

            // 1) EL CUERPO DE LA CASA — el charco bajo el portador, la
            //    columna viva, la masa con ojos y la bruma en dos capas.
            SombrasLib.Charco(SombraProyectada(dueño), 44f, 0.55f, tiempo, semilla);
            Vector2[] columna = SombrasLib.ColumnaViva(raiz, Projectile.Center,
                tiempo, semilla, 20, 0.24f, 0.5f);
            float vive = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            SombrasLib.Masa(columna, 9f, 13f, 0.95f * vive, semilla, tiempo, 44f);
            SombrasLib.BrumaColumna(columna, 46f, 0.62f * vive, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(columna, 30f, 0.46f * vive, tiempo, semilla + 97, 12);

            // 2) LOS OJOS de la masa — TODOS clavados en la presa.
            Vector2 mirada = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float apertura = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            SombrasLib.OjosDeMasa(columna, mirada, apertura * vive, semilla, tiempo, 7);

            // 3) EL VELO DE RUIDO sobre el cuerpo + la CABEZA vestida:
            //    el orbe violeta, el NÚCLEO PULSAR teal girando al revés
            //    y los tres satélites de tinta.
            Vector2 cabeza = columna[^1];
            Vector2 rumboCabeza = Utils.SafeNormalize(columna[^1] - columna[Math.Max(columna.Length - 4, 0)],
                Projectile.rotation.ToRotationVector2());
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Color violeta = new(150, 96, 235);
                Color teal = new(80, 210, 170);
                Vector2 medio = (raiz + cabeza) * 0.5f - Main.screenPosition;
                float largo = Vector2.Distance(raiz, cabeza);

                // EL VELO DE RUIDO (el manto del tentáculo).
                Texture2D velo = Ruido(semilla, 4.4f);
                Main.spriteBatch.Draw(velo, medio, null, violeta * (0.1f * vive),
                    rumboCabeza.ToRotation(), Utils.Size(velo) * 0.5f,
                    new Vector2(largo / 512f, 0.13671875f), SpriteEffects.None, 0f);

                Vector2 cabezaPantalla = cabeza - Main.screenPosition;
                float respira = 0.8f + 0.2f * MathF.Sin(tiempo * 7f);
                Capa(Tex("GlowOrb"), cabezaPantalla, violeta * (0.5f * vive), 0f,
                    74f * respira / 128f);
                Capa(Tex("Procedural/PulsarCore"), cabezaPantalla, teal * (0.55f * vive),
                    -tiempo * 2.4f, 23f / 64f);
                for (int i = 0; i < 3; i++)
                {
                    float ang = tiempo * (2f + Azar(i) * 1.5f) + Azar(i + 7) * MathF.PI * 2f;
                    Vector2 pos = cabezaPantalla
                        + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * (34f + Azar(i) * 20f);
                    Capa(Tex("SparkleStar"), pos,
                        Color.White * (0.3f + 0.35f * Titila(i)) * vive, ang,
                        (12f + 8f * Titila(i)) / 32f);
                }

                // EL ANILLO DE TINTA — la presa mordida marcada con el
                // doble anillo (violeta + teal): el sello del festín.
                if (fase == FASE_MORDISCO && presa != null && presa.active)
                {
                    Vector2 posPresa = presa.Center - Main.screenPosition;
                    float radioPresa = MathF.Max(presa.width, presa.height) * 0.9f + 30f;
                    VFXCore.RingQuad(posPresa, radioPresa, tiempo * 1.4f,
                        VFXCore.TintAlfa(violeta, 0.45f * vive));
                    VFXCore.RingQuad(posPresa, radioPresa * 0.7f, -tiempo * 1.9f,
                        VFXCore.TintAlfa(teal, 0.3f * vive));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // 4) LA GARGANTA DEL VÓRTICE — el motor de píxeles en la
            //    cabeza, GIRANDO CON EL RUMBO del tentáculo: la boca de
            //    la sombra ES el horizonte de sucesos.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                try
                {
                    PixelFxPlayer motor = Motor();
                    motor.Fx.options.Angle = rumboCabeza.ToRotation();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    motor.Draw(Main.spriteBatch, cabeza - Main.screenPosition
                        - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
                    Main.spriteBatch.End();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
            }

            // 5) EL MORDISCO VIVO — la boca devoradora de la casa sobre
            //    la presa + el destello rojo del dren.
            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                float intensidad = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(cabeza, presa, intensidad, vive, tiempo, semilla, columna);
                VFXCore.Begin();
                VFXCore.Quad(cabeza, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * vive),
                    new Vector2(110f, 110f), rumboCabeza.ToRotation());
                VFXCore.FlushAdditive();
                return;
            }

            // 6) LA CABEZA DE BRUMA (en emerger/caza/disipar).
            float brillo = fase switch
            {
                FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                _ => 0.5f * vive,
            };
            SombrasLib.CabezaDeBruma(cabeza, rumboCabeza, brillo * vive, vive, tiempo, semilla);
            SombrasLib.Bruma(cabeza, 40f, 0.5f * vive, tiempo, semilla + 31,
                new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));
        }

        /// <summary>La lámina de ruido viva (el caché de las 20).</summary>
        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int i = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (i < 0) i += 20;
            return _ruidoCache[i] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + i,
                AssetRequestMode.ImmediateLoad).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta,
                AssetRequestMode.ImmediateLoad).Value;

        /// <summary>El azar determinista de la casa.</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }

        /// <summary>El titilar determinista (0..1, coseno suave).</summary>
        private static float Titila(int i, float vel = 3.1f)
            => 0.5f - 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * vel + Azar(i) * MathF.PI * 2f);

        private static void Capa(Texture2D tex, Vector2 pos, Color color, float rot, float esc)
        {
            if (color.A != 0 && esc > 0f)
                Main.spriteBatch.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f, esc,
                    SpriteEffects.None, 0f);
        }

        /// <summary>El sonido blindado (la lección de las sombras: en
        /// AI gated != Server ya suena en SP y clientes).</summary>
        private static void Sonar(SoundStyle estilo, Vector2 pos)
        {
            try
            {
                SoundEngine.PlaySound(in estilo, pos);
            }
            catch { /* el sonido jamás tumba el ritual */ }
        }
    }
}
