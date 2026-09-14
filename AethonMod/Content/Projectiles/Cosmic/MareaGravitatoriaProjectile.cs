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
    /// MareaGravitatoriaProjectile — v6.26 — LA ONDA DE MAREA DE LUZ.
    ///
    /// EL PROYECTIL ES UNA OLA que AVANZA horizontal PEGADA AL TERRENO:
    /// cada tick mueve con `Collision.TileCollision` (sube y baja colinas
    /// con física real de tiles), se ENCAJA al suelo cuando hay terreno
    /// debajo (gravedad de marea) y ROTACIÓN POR PENDIENTE (muestrea el
    /// suelo delante/detrás y gira el cuerpo de la ola).
    ///
    /// FÍSICA:
    ///   · Empuje horizontal CONSTANTE (dir·6.2 px/t — la marea no para).
    ///   · Gravedad 0.34 px/t² solo cuando está EN EL AIRE (la ola cae a
    ///     buscar el siguiente valle).
    ///   · Si el tile-collision ANULA el avance X → GOLPEÓ UNA PARED:
    ///     la ola SE ROMPE en 3 OLAS MENORES diagonales (MareaChica, en
    ///     este mismo archivo) + salpicadura + Kick.
    ///
    /// MECÁNICA: cada 6 ticks golpea a los enemigos de su banda (ARRASTRE:
    /// empujón de velocidad + MOJADO: BuffID.Wet + daño 20% constante);
    /// detrás va dejando CHARCOS (puntos de contacto con el suelo para la
    /// bruma del renderer). Vida 900 ticks = 15 s de marea.
    ///
    /// Daño MP-seguro: SimpleStrikeNPC bajo `Main.netMode != NetmodeID.
    /// MultiplayerClient`. Determinismo por semilla de identity.
    /// </summary>
    public class MareaGravitatoriaProjectile : ModProjectile
    {
        /// <summary>Vida de la marea: 900 ticks = 15 s.</summary>
        public const int TotalTicks = 900;

        /// <summary>La velocidad de marea (px/t) — constante, imparable.</summary>
        public const float Velocidad = 6.2f;

        /// <summary>Gravedad de la ola cuando está en el aire (px/t²).</summary>
        private const float Gravedad = 0.34f;

        /// <summary>La dirección de la marea (+1 derecha / −1 izquierda;
        /// ai[0] — sincronizada: 0 = derecha, 1 = izquierda).</summary>
        private int Direccion => Projectile.ai[0] < 0.5f ? 1 : -1;

        /// <summary>Semilla determinista (ai[1]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>El daño base del arma (ai[2]).</summary>
        private float BaseDamage => Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;

        /// <summary>El espejo visual local de la edad.</summary>
        private float _age;

        /// <summary>La pendiente actual del terreno (rad — el renderer la lee
        /// por p.rotation).</summary>

        /// <summary>Los CHARCOS que la ola va dejando (máx 12 — el renderer
        /// los pinta con bruma que se seca con la edad).</summary>
        internal Vector2[] Charcos = new Vector2[12];
        internal float[] CharcoEdad = new float[12];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // La CAJA de la ola (el dibujo es 100% código).
            Projectile.width = 60;
            Projectile.height = 40;
            // Daño 100% manual (el patrón de la casa): el arrastre.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;   // la colisión es MANUAL (TileCollision)
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // La dirección de la marea: hacia donde mira el jugador.
            Projectile.ai[0] = Main.player[Projectile.owner].direction < 0 ? 1f : 0f;
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            Projectile.ai[2] = Projectile.damage;
            _age = 0f;
            for (int i = 0; i < Charcos.Length; i++) CharcoEdad[i] = -1f;
        }

        // ==================================================================
        //  LA MAREA — cabalgando el terreno
        // ==================================================================

        public override void AI()
        {
            _age += 1f;

            // === LA ESTELA DE LA CRESTA (el track del camino — EstelaLib) ===
            EstelaLib.Track(Projectile.whoAmI, 18).Push(Projectile.Center);
            if (_age % 120f == 0f) EstelaLib.PurgeTracks();

            // === EL EMPUJE DE MAREA (constante — el agua nunca se detiene;
            //     la velocidad es PERSISTENTE para que la caída acelere) ===
            Projectile.velocity.X = Direccion * Velocidad;
            Vector2 vel = Projectile.velocity;

            // === ¿HAY SUELO DEBAJO? (a 6 px: la ola se pega) ===
            bool enSuelo = SueloBajo(Projectile.Center + new Vector2(0f, Projectile.height * 0.5f), 6f);

            if (!enSuelo)
            {
                // EN EL AIRE: gravedad de marea (acelera — busca el valle).
                vel.Y += Gravedad;
                if (vel.Y > 6f) vel.Y = 6f;
            }
            else
            {
                // ENCARAR LA PENDIENTE: muestrear el suelo delante y detrás.
                float adelanta = SueloEn(Projectile.Center.X + Direccion * 16f,
                    Projectile.Center.Y + 6f, 44f);
                float atras = SueloEn(Projectile.Center.X - Direccion * 16f,
                    Projectile.Center.Y + 6f, 44f);
                if (adelanta >= 0f && atras >= 0f)
                {
                    float dy = adelanta - atras;
                    float pend = MathF.Atan2(dy, 32f);   // pendiente real del terreno
                    Projectile.rotation = pend;
                    // La ola RESBALA cuesta arriba/abajo por la pendiente.
                    vel.Y = MathF.Sin(pend) * Velocidad * 0.9f;
                }
                else
                {
                    vel.Y = 0f;   // suelo llano sin muestras: pegada al piso
                }
            }
            Projectile.velocity = vel;

            // === EL MOVIMIENTO CON COLISIÓN DE TILES (manual — la lección
            //     de las olas de verdad: suben por los bloques). ===
            Vector2 paso = Collision.TileCollision(Projectile.position, vel,
                Projectile.width, Projectile.height, true, false, 1);

            // === ¿GOLPEÓ UNA PARED? (el X anulado = algo sólido delante) ===
            if (paso.X == 0f && MathF.Abs(vel.X) > 0.1f)
            {
                // ¿ESCALÓN SUBIBLE? El suelo delante no sube más de 30 px:
                // la ola TREPA el escalón (sube y sigue — la marea sube
                // mareas). Si no, es UNA PARED: la ola se ROMPE.
                float sueloAhora = SueloEn(Projectile.Center.X,
                    Projectile.Center.Y + 6f, 60f);
                float sueloFrente = SueloEn(
                    Projectile.Center.X + Direccion * (Projectile.width * 0.5f + 8f),
                    Projectile.Center.Y - 24f, 96f);
                bool escalon = sueloAhora >= 0f && sueloFrente >= 0f
                    && sueloAhora - sueloFrente <= 30f
                    && sueloFrente >= sueloAhora - 60f;
                if (escalon)
                {
                    // TREPAR el escalón: asentarse sobre el nivel nuevo.
                    Projectile.position.Y = sueloFrente - Projectile.height * 0.5f - 2f;
                    Projectile.velocity.Y = 0f;
                }
                else
                {
                    RomperContraLaPared();
                    return;
                }
            }
            else
            {
                Projectile.position += paso;
            }

            // Si el paso fue cortado en Y (aterrizaje), quedar PEGADA.
            if (paso.Y == 0f && vel.Y > 0.1f)
            {
                // aterrizó: asentar la ola en el suelo de arriba.
                float y = SueloEn(Projectile.Center.X, Projectile.position.Y, 52f);
                if (y >= 0f)
                {
                    Projectile.position.Y = y - Projectile.height * 0.5f - 2f;
                    Projectile.velocity.Y = 0f;
                }
            }

            // === LOS CHARCOS (cada 18 ticks, en el punto de contacto) ===
            if (_age % 18f == 0f && enSuelo)
            {
                int idx = (int)(_age / 18f) % Charcos.Length;
                Charcos[idx] = Projectile.Center + new Vector2(0f, Projectile.height * 0.42f);
                CharcoEdad[idx] = _age;
            }

            // === EL ARRASTRE (daño + empujón + mojado, MP-seguro) ===
            if (Main.netMode != NetmodeID.MultiplayerClient && _age % 6f == 0f)
                ArrastrarEnemigos();

            // === EL SONIDO DE LA MAREA (el rumor periódico del agua) ===
            if (Main.netMode != NetmodeID.Server && _age == 1f)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item21.WithPitchOffset(-0.3f), Projectile.Center);

            // === LA LUZ DEL AGUA (azul frío a lo largo de la ola) ===
            if (Main.netMode != NetmodeID.Server && _age % 3f == 0f)
                Lighting.AddLight(Projectile.Center, 0.14f, 0.38f, 0.52f);
        }

        /// <summary>¿Hay tile sólido bajo el punto (dentro de maxAbajo px)?</summary>
        private static bool SueloBajo(Vector2 punto, float maxAbajo)
        {
            return SueloEn(punto.X, punto.Y, maxAbajo) >= 0f;
        }

        /// <summary>La Y del primer suelo bajo (x, yDesde) en maxAbajo px
        /// (escaneo de tiles cada 8 px); −1 si no hay suelo.</summary>
        internal static float SueloEn(float x, float yDesde, float maxAbajo)
        {
            int tx = (int)(x / 16f);
            for (float dy = 0f; dy <= maxAbajo; dy += 8f)
            {
                int ty = (int)((yDesde + dy) / 16f);
                Tile tile = Main.tile[tx, ty];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType]
                    && !Main.tileSolidTop[tile.TileType])
                    return yDesde + dy;
            }
            return -1f;
        }

        /// <summary>EL ARRASTRE: daña a los enemigos de la banda de la ola,
        /// los EMPUJA en la dirección de la marea y los MOJA.</summary>
        private void ArrastrarEnemigos()
        {
            int dmg = Math.Max(1, (int)(BaseDamage * 0.20f));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 rel = npc.Center - Projectile.Center;
                if (MathF.Abs(rel.X) > 64f) continue;
                if (rel.Y < -70f || rel.Y > 60f) continue;

                npc.SimpleStrikeNPC(dmg, Direccion, false, 3f, DamageClass.Magic);
                npc.AddBuff(BuffID.Wet, 240);
                // EL ARRASTRE físico: el agua se los LLEVA.
                npc.velocity.X += Direccion * 2.6f;
            }
        }

        // ==================================================================
        //  EL ROMPEOLAS — contra la pared, la ola se rompe
        // ==================================================================

        private void RomperContraLaPared()
        {
            // === LAS 3 OLAS MENORES (diagonales, rebotan solas) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    // −35°, −60°, −15° hacia arriba en la dirección de la marea.
                    float ang = (-15f - i * 22.5f) * MathHelper.Pi / 180f;
                    Vector2 dir = new(Direccion * MathF.Cos(ang), MathF.Sin(ang));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                        Projectile.Center, dir * MareaChicaProjectile.Velocidad,
                        ModContent.ProjectileType<MareaChicaProjectile>(),
                        (int)(BaseDamage * 0.6f), 3f, Projectile.owner,
                        ai0: Direccion, ai1: Projectile.ai[1] + i);
                }
            }

            if (Main.netMode == NetmodeID.Server) { Projectile.Kill(); return; }

            // === LA SALPICADURA del rompeolas (visual cliente) ===
            OndaLib.Kick(4f, 12);
            ParticlePresets.RingPulse(Projectile.Center, 120f,
                new Color(120, 200, 255, 200), 30);
            for (int i = 0; i < 14; i++)
            {
                float ang = -MathHelper.PiOver2 + (i / 14f - 0.5f) * 2.4f;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WaterCandle,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(1.5f, 4.5f),
                    150, new Color(160, 220, 255), 0.7f);
                d.noGravity = false;
            }
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item21.WithPitchOffset(0.25f), Projectile.Center);

            Projectile.Kill();
        }

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
                MareaGravitatoriaRenderer.Draw(this, _age, Seed);
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
            // La marea MUERE en la orilla: un último charco + suspiro de espuma.
            if (Main.netMode == NetmodeID.Server) return;
            ParticlePresets.RingPulse(Projectile.Center, 70f,
                new Color(110, 190, 255, 160), 24);
        }
    }

    // ======================================================================
    //  LA OLA MENOR — la hija del rompeolas
    // ======================================================================

    /// <summary>
    /// MareaChicaProjectile — v6.26 — LA OLA MENOR DE LA MAREA.
    ///
    /// Nace cuando la marea grande SE ROMPE contra una pared: viaja en
    /// DIAGONAL hacia arriba (tres ángulos distintos), rebota contra el
    /// terreno con `tileCollide` real (la colisión de vanilla invertida en
    /// Y — la ola MENOR salta como gota viva) y moja lo que toca. Vida
    /// corta (100 ticks): es solo la salpicadura organizada del impacto.
    ///
    /// Daño 60% del arma en contacto (manual, MP-seguro) + Wet.
    /// </summary>
    public class MareaChicaProjectile : ModProjectile
    {
        /// <summary>La velocidad de las olas menores (px/t).</summary>
        public const float Velocidad = 5.2f;

        /// <summary>Vida de la ola menor: 100 ticks = 1.7 s.</summary>
        public const int Vida = 100;

        /// <summary>La dirección de la marea madre (ai[0]).</summary>
        private int Direccion => Projectile.ai[0] < 0.5f ? 1 : -1;

        /// <summary>Semilla determinista (ai[1]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = false;          // daño manual (la casa)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;        // las menores REBOTAN (vanilla)
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            _age += 1f;

            // La gravedad de la gota viva.
            Projectile.velocity.Y += 0.12f;
            if (Projectile.velocity.Y > 4.5f) Projectile.velocity.Y = 4.5f;
            // El empujón diagonal constante (restaurado si un muro lo anuló).
            if (MathF.Abs(Projectile.velocity.X) < 0.5f)
                Projectile.velocity.X = Direccion * Velocidad * 0.75f;

            // EL DAÑO por contacto (cada 10 ticks, banda pequeña).
            if (Main.netMode != NetmodeID.MultiplayerClient && _age % 10f == 0f)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    if ((npc.Center - Projectile.Center).Length() > 34f) continue;
                    npc.SimpleStrikeNPC(Projectile.damage, Direccion, false, 2f,
                        DamageClass.Magic);
                    npc.AddBuff(BuffID.Wet, 180);
                }
            }

            if (Main.netMode != NetmodeID.Server && _age % 4f == 0f)
                Lighting.AddLight(Projectile.Center, 0.12f, 0.32f, 0.46f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // EL REBOTE de la gota (y una chispa de espuma).
            if (MathF.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.1f)
                Projectile.velocity.Y = -oldVelocity.Y * 0.55f;
            if (Main.netMode != NetmodeID.Server && Projectile.timeLeft % 30f == 0f)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item21.WithPitchOffset(0.4f), Projectile.Center);
            return false;   // NO muere: rebota (la ola menor vive su vida)
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                MareaGravitatoriaRenderer.DrawMinor(this, _age, Seed);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            }
            return false;
        }
    }
}
