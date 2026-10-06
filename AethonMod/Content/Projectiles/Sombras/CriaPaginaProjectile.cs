using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// CRIAPAGINAPROJECTILE — v6.50.69 — ARMA NUEVA 3: LA CRÍA DE LA PÁGINA.
    ///
    /// La letra del usuario: «esas tres armas nuevas se ven y funcionan
    /// horrible» — la familia vuelve al ADN de La Sombra (tentáculo de
    /// carne + bruma) con un OFICIO distinto por hermana.
    ///
    /// LA CRÍA ES LA CAMADA: TRES tentáculos pequeños que VIVEN en el
    /// portador (orbitan su cuerpo, colgados de él — la regla .68: la
    /// base SIEMPRE es el jugador) y CAZAN SOLOS: cuando un enemigo se
    /// acerca, la cría libre más cercana LE SALTA ENCIMA — latigazo
    /// rápido, mordisco, y vuelve a su sitio a esperar. Cada una tiene
    /// SU OJO RASGADO (la textura de la casa) que MIRA FIJO a su presa
    /// mientras la caza… y al portador cuando descansa. No hay que
    /// apuntar NI disparar: es la guardia de la página. Si su mordisco
    /// mata a un jefe: LA DEVORACIÓN — el festín estilo 10, LA MENADA.
    ///
    /// El item invoca UNA cría por uso (hasta TRES vivas — una por
    /// ángulo: 0°, 120°, 240° alrededor del portador).
    /// 100% código: ni un sprite del arma. Cero dependencias.
    /// </summary>
    public class CriaPaginaProjectile : ModProjectile
    {
        private const byte ESTADO_GUARDA = 0;
        private const byte ESTADO_SALTA = 1;
        private const byte ESTADO_VUELVE = 2;

        /// <summary>Radio de caza: la presa que entra AQUÍ despierta a la camada.</summary>
        private const float RADIO_CAZA = 440f;

        /// <summary>El slot de la cría (0/1/2 — su ángulo de guardia).</summary>
        private float Slot => Projectile.ai[0];

        /// <summary>La presa actual (whoAmI+1; 0 = ninguna).</summary>
        private NPC Presa => Projectile.localAI[0] <= 0 ? null : Main.npc[(int)Projectile.localAI[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1800;             // 30 s de camada por invocación
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 45;
            Projectile.extraUpdates = 1;
        }

        /// <summary>El PUESTO DE GUARDIA de esta cría: orbita al portador.</summary>
        private Vector2 Puesto(Player dueño, float tiempo)
        {
            float ang = Slot * 2.0944f + tiempo * 0.55f;
            return dueño.MountedCenter + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.82f) * 46f;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte estado = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // el portador murió o se fue: la cría se disuelve
            if (dueño == null || !dueño.active)
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
                Projectile.ai[1] = ESTADO_VUELVE;
                Projectile.ai[2] = 60f;
                return;
            }

            NPC presa = Presa;
            bool presaValida = presa != null && presa.active && presa.life > 0;

            switch (estado)
            {
                case ESTADO_GUARDA:
                {
                    // LA GUARDIA: rondas el cuerpo del portador, ondulando
                    // (la física verlet del dibujo pone la pereza)
                    Projectile.velocity = Vector2.Zero;   // el puesto MANDA
                    Vector2 puesto = Puesto(dueño, tiempo);
                    Projectile.Center = puesto;
                    Projectile.rotation = (puesto - dueño.MountedCenter).ToRotation() + MathHelper.PiOver2;

                    // EL RONRONEO: una motita cada tanto (la cría respira)
                    if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(14))
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                            new Vector2(0f, -0.4f), 128, default, 0.45f);
                        d.noGravity = true;
                    }

                    // LA VIGILIA cada 12 t: ¿entró algo al radio de caza?
                    // (la presa MÁS CERCANA AL PORTADOR — protegen al dueño)
                    if (t % 12f == 0f)
                    {
                        NPC candidata = BuscarCerca(dueño.MountedCenter, RADIO_CAZA);
                        if (candidata != null)
                        {
                            Projectile.localAI[0] = candidata.whoAmI + 1;
                            Projectile.ai[1] = ESTADO_SALTA;
                            Projectile.ai[2] = 0f;
                            Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.35f), Projectile.Center);
                        }
                    }
                    break;
                }
                case ESTADO_SALTA:
                {
                    if (!presaValida) { IrAVolver(); break; }
                    // EL SALTO: recto y rápido a la presa (giro cerrado — es
                    // un latigazo corto, no un misil) — el motor ya mueve por
                    // la velocity (NADA de += manual: sería doble)
                    Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    float vel = 27f;
                    float actual = Projectile.velocity.ToRotation();
                    float deseado = hacia.ToRotation();
                    float giro = MathHelper.WrapAngle(deseado - actual);
                    Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.34f, 0.34f)).ToRotationVector2() * vel;
                    Projectile.rotation = deseado;

                    // v6.50.69 — LA MARCA CONTINUA: si la presa es un JEFE,
                    // su muerte (por quien sea) durante la caza es NUESTRA
                    if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                        FaucesGlobalNPC.Marcar(presa, 10);

                    // ¿lo alcanzó? (el golpe real lo da el motor vía Colliding)
                    if (Projectile.Hitbox.Intersects(presa.Hitbox) ||
                        t > 26f)
                        IrAVolver();
                    break;
                }
                default: // ESTADO_VUELVE
                {
                    // LA VUELTA: un resorte al puesto de guardia
                    Projectile.velocity = Vector2.Zero;   // el resorte MANDA
                    Vector2 puesto = Puesto(dueño, tiempo);
                    Projectile.Center = Vector2.Lerp(Projectile.Center, puesto, 0.22f);
                    Projectile.rotation = (puesto - Projectile.Center).ToRotation();
                    if (Vector2.DistanceSquared(Projectile.Center, puesto) < 400f || t > 40f)
                    {
                        Projectile.ai[1] = ESTADO_GUARDA;
                        Projectile.ai[2] = 0f;
                    }
                    break;
                }
            }
        }

        private void IrAVolver()
        {
            Projectile.ai[1] = ESTADO_VUELVE;
            Projectile.ai[2] = 0f;
            Projectile.localAI[0] = 0f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // el mordisco de la cría marca: si mata a un jefe, es NUESTRO
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 10);
            Sonar(SoundID.NPCHit9.WithPitchOffset(0.35f).WithVolumeScale(0.3f), Projectile.Center);
            IrAVolver();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != ESTADO_SALTA) return false;
            Player dueño = Main.player[Projectile.owner];
            // la cría pega con TODO su cuerpecito (la línea corto del salto)
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - Projectile.velocity, Projectile.Center, 12f, ref punto);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 23 + 7;
            Player dueño = Main.player[Projectile.owner];
            byte estado = (byte)Projectile.ai[1];
            NPC presa = Presa;

            // === EL CUERPECITO: pequeño, vivo, con gancho — nace DEL
            // PORTADOR (vive colgada de él) y la punta es donde caza
            Vector2 raiz = Puesto(dueño, tiempo) - (Puesto(dueño, tiempo) - dueño.MountedCenter) * 0.25f;
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 9, 0.35f, gancho: 0.3f);
            float brío = estado == ESTADO_SALTA ? 1f : 0.85f;
            SombrasLib.Masa(col, 15f, 5f, 0.94f * brío, semilla, tiempo);

            // === LA BRUMA PEQUEÑA — una capa fina (las crías apenas exhalan)
            SombrasLib.BrumaColumna(col, 14f, 0.5f * brío, tiempo, semilla, 6);

            // === LA CABECITA de bruma + EL OJO RASGADO que mira SIEMPRE:
            // a la presa cuando caza, al portador cuando guarda (la firma
            // de la camada — la página te vigila por los tres)
            Vector2 punta = col[col.Length - 1];
            Vector2 mira = presa != null && presa.active ? presa.Center - punta
                : dueño.MountedCenter - punta;
            Vector2 rumbo = mira.SafeNormalize(Vector2.UnitX);
            SombrasLib.CabezaDeBruma(punta, rumbo, 0.75f * brío, brío, tiempo, semilla);
            SombrasLib.Ojo(punta + rumbo * 6f, 13f, mira, 0.95f, pupila: true, rasgada: true);

            // EL SALTO: la estela del latigazo (una viruta roja — son rápidas)
            if (estado == ESTADO_SALTA)
            {
                VFXCore.Begin();
                Vector2 cola = Projectile.Center - Projectile.velocity * 1.6f;
                VFXCore.Line(Projectile.Center, cola, SombrasLib.Alfa(SombrasLib.Rojo, 0.35f), 5f);
                VFXCore.FlushAdditive(VFXCore.Pixel);
            }
        }

        /// <summary>La presa más cercana al PORTADOR dentro del radio de caza.</summary>
        private static NPC BuscarCerca(Vector2 desde, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
