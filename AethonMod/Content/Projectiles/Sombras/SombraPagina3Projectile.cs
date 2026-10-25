using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// SOMBRAPAGINA3PROJECTILE — v6.52.2 — LA SOMBRA DE LA PÁGINA III:
    /// EL CORO DE IRISES (la letra: «crea un sombra de la pagina 3 y 4
    /// pero para estos solo usa el iris no el ojo completo»).
    ///
    /// EL TERCER TOMO — la página que ya no necesita párpados NI esclerótica:
    /// el cuerpo es UNA sola cuerda de sombra (la ColumnaViva del tomo uno)
    /// y la mirada la pone EL CORO: SIETE IRISES REALES flotando a lo largo
    /// del cuerpo, cada uno con SU latido propio, TODOS clavados en la
    /// presa (el iris viaja ±4 px — el gesto del escuadrón, sin el ojo).
    ///
    ///   · EL CUERPO: una sola cuerda fina + su bruma (la sombra es el
    ///     atril; los iris son los actores).
    ///   · EL CORO: 7 sprites de iris (nuestro GrimorioHambriento_Iris)
    ///     espaciados a lo largo de la espina, cada uno con SU pulso de
    ///     escala determinista y SU halo aditivo tenue — NADA de ojo
    ///     completo: SOLO EL IRIS, flotando en la tinta.
    ///   · LA CACERÍA: al volar, el coro se REPARTE (los exteriores se
    ///     abren en abanico — la página despliega su mirada).
    ///   · EL MORDISCO: el coro CONVERGE sobre la herida (los siete se
    ///     apiñan mirando la llaga) + el drenaje de siempre (3%/6t) y la
    ///     devoración a 1 HP (estilo 3 — el motor intacto).
    /// </summary>
    public class SombraPagina3Projectile : ModProjectile
    {
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MORDISCO = 2;
        private const byte FASE_DISIPAR = 3;

        private const int CORO = 7;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.extraUpdates = 2;
        }

        // ==================================================================
        //  LA VIDA (el motor del festín — intacto, el contrato de la casa)
        // ==================================================================

        private Vector2 RaizDeSombra(Player dueño) => dueño.MountedCenter;

        private Vector2 SombraProyectada(Player dueño)
        {
            Vector2 c = dueño.MountedCenter + new Vector2(0, dueño.height * 0.45f);
            int tx = (int)(c.X / 16f), ty = (int)(c.Y / 16f);
            int pasos = 0;
            while (pasos < 34 && !WorldGen.SolidTile(tx, ty)) { ty++; pasos++; }
            Vector2 suelo = new Vector2(tx * 16f + 8f, ty * 16f - 4f);
            return pasos < 34 ? suelo : c;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPAR;
                t = 0;
            }

            switch (fase)
            {
                case FASE_EMERGER:
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (30f + 160f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.5f).WithVolumeScale(0.7f), Projectile.Center);
                    // EL CORO DESPIERTA: una campanada por iris (la página abre ojos)
                    if (t == 10 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item122.WithPitchOffset(0.45f).WithVolumeScale(0.3f), Projectile.Center);
                    if (t >= 26) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.05f, 20f, 50f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.26f, 0.26f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
                            Sonar(SoundID.Item122.WithPitchOffset(-0.6f).WithVolumeScale(0.6f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0;
                            break;
                        }
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();

                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL DRENAJE DEL TERCER TOMO: 3% cada 6 t (el coro bebe por los iris)
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 18 && t % 6 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.03f, 30f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 3);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 36 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.1f).WithVolumeScale(0.5f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 52) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(24, 24), 48, 48,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.55f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 17 + 5, 10, 0.24f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 8, ref punto))
                    return true;
            return false;
        }

        // ==================================================================
        //  EL DIBUJO — la cuerda sola + EL CORO DE IRISES (solo el iris:
        //  la letra del usuario — NADA de ojo completo)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawLaCuerda();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // EL CORO va en el LOTE VANILLA (el contrato de los sprites,
            // blindado como la .51.14: SIEMPRE se sale con el lote vivo).
            try { DibujarElCoro(); }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>LA ESPINA (la única cuerda — el atril del coro).</summary>
        private Vector2[] Espina(float tiempo, int semilla, Vector2 raiz)
            => SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 20, 0.22f, gancho: 0.5f);

        private void DrawLaCuerda()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;

            // EL CHARCO con rim violeta (la lectura nocturna de la .66)
            SombrasLib.Charco(SombraProyectada(dueño), 54f, 0.55f * disipa, tiempo, semilla);

            // LA CUERDA ÚNICA — fina (los iris son los protagonistas)
            Vector2[] espina = Espina(tiempo, semilla, raiz);
            SombrasLib.Masa(espina, 5f, 10f, 0.90f * disipa * presencia, semilla, tiempo, grosorCentro: 24f);
            SombrasLib.BrumaColumna(espina, 44f, 0.50f * disipa * presencia, tiempo, semilla, 14);

            // EL MORDISCO: LA HERIDA + la llaga que el coro contempla
            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                Vector2 punta = espina[espina.Length - 1];
                float herida = MathHelper.Clamp(t / 22f, 0f, 1f);
                VFXCore.Begin();
                VFXCore.Quad(presa.Center, SombrasLib.Alfa(SombrasLib.Rojo,
                    (0.30f + 0.25f * herida) * disipa), new Vector2(56f, 56f), 0f);
                VFXCore.Quad(punta, SombrasLib.Alfa(new Color(140, 80, 220), 0.30f * disipa),
                    new Vector2(76f, 76f), 0f, VFXCore.SoftGlow);
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>
        /// EL CORO DE IRISES: SIETE sprites de iris SOLO (sin esclerótica,
        /// sin párpados — la letra) a lo largo de la espina, cada uno con
        /// SU latido y SU mirada; en CAZA se abren en abanico, en
        /// MORDISCO CONVERGEN sobre la herida.
        /// </summary>
        private void DibujarElCoro()
        {
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;
            if (disipa <= 0.02f) return;

            Texture2D iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            if (iris == null) return;

            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] espina = Espina(tiempo, semilla, raiz);
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;

            // LA CONVERGENCIA DEL MORDISCO: los iris se APIÑAN sobre la herida
            float conv = fase == FASE_MORDISCO ? MathHelper.Clamp(t / 30f, 0f, 1f) : 0f;
            // EL ABANICO DE CAZA: los exteriores se abren
            float abanico = fase == FASE_CAZA ? 1f : 0f;

            for (int k = 0; k < CORO; k++)
            {
                float u = k / (float)(CORO - 1);          // 0 raíz → 1 punta
                int idx = Math.Clamp((int)(u * (espina.Length - 1)), 0, espina.Length - 1);
                Vector2 pos = espina[idx];

                // EL ABANICO: cada iris se abre a SU lado de la cuerda
                Vector2 dir = espina[Math.Min(idx + 1, espina.Length - 1)] -
                    espina[Math.Max(idx - 1, 0)];
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                if (perp.LengthSquared() < 0.001f) perp = Vector2.UnitX;
                perp.Normalize();
                float lado = ((k % 2) * 2 - 1);           // alterna arriba/abajo
                float apertura = (0.5f - MathF.Abs(u - 0.5f)) * 26f * abanico;
                pos += perp * (lado * apertura);

                // LA CONVERGENCIA: hacia la herida (el apiño del coro)
                if (conv > 0f && presa != null && presa.active)
                {
                    Vector2 alrededor = presa.Center + new Vector2(
                        MathF.Cos(k * MathHelper.TwoPi / CORO + tiempo * 0.8f),
                        MathF.Sin(k * MathHelper.TwoPi / CORO + tiempo * 0.8f)) *
                        MathHelper.Lerp(60f, 22f, conv);
                    pos = Vector2.Lerp(pos, alrededor, conv);
                }

                // EL LATIDO propio (determinista — cada iris SU pulso)
                float latido = 0.8f + 0.25f * MathF.Sin(tiempo * (2.2f + 0.31f * k) +
                    SombrasLib.semille(semilla + k * 13) * 6.2f);

                // LA MIRADA: el iris viaja hacia la presa (±4 px — el gesto
                // del escuadrón, SIN el ojo que lo porte)
                Vector2 d = objetivo - pos;
                float len = d.Length();
                Vector2 mirada = len > 4f
                    ? new Vector2(d.X / len * 4f, d.Y / len * 2.6f) : Vector2.Zero;

                Color luz = Color.Lerp(Lighting.GetColor(pos.ToTileCoordinates()),
                    new Color(226, 218, 244), 0.45f) * (disipa * (fase == FASE_EMERGER
                        ? SombrasLib.DeGolpe(t / 26f) : 1f));

                const float esc = 0.34f;
                Vector2 pantalla = pos - Main.screenPosition;
                Main.spriteBatch.Draw(iris, pantalla + mirada * esc * 3f, null, luz, 0f,
                    iris.Size() * 0.5f, esc * latido, SpriteEffects.None, 0f);

                // EL HALO tenue de cada iris (encolado — el volcado único al final)
                VFXCore.Quad(pos, SombrasLib.Alfa(new Color(170, 110, 240), 0.14f * disipa),
                    new Vector2(26f * latido, 26f * latido), 0f);
            }

            // EL VOLCADO ÚNICO de los halos (el contrato blindado .51.14)
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
