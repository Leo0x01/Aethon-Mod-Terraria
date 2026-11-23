using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Clicker
{
    // =====================================================================
    //  MarcaDeTinta — v6.168 — LA MARCA DEL UMBRAL (el corazón del
    //  ClickerDelUmbral — la letra: «se super creativo»).
    //
    //  EL CICLO DE LA MARCA:
    //   1. NACE EN EL CIELO sobre su víctima (o el cursor si no hay
    //      blanco) y CAE en un ráfaga de tinta (la sentencia baja).
    //   2. SE CLAVA (12 ticks de caída) y VIVE 4 segundos: mientras
    //      vive, LA TINTA GOTEA del cielo hacia la marca (la lluvia
    //      fina — la firma visual del arma) y el blanco lleva SU
    //      marca visible (el sello violeta latiendo sobre su cabeza).
    //   3. LA DETONACIÓN: cada marca detona AL MORIR o cuando la
    //      octava clic la ROMPE — UN GOLPE + el destello de tinta.
    //      LA ROTURA DEL UMBRAL: TODAS las marcas vivas detonan EN
    //      CADENA (con 60 ticks de vida entre salto y salto: la
    //      sentencia VIAJA de víctima a víctima).
    //
    //  EL DAÑO: la marca golpea UNA VEZ al detonar (daño del arma);
    //  el combo de 8 detona TODAS → la cadena escala con lo sembrado.
    // =====================================================================
    public class MarcaDeTinta : ModProjectile
    {
        private const int TICKS_CAIDA = 12;
        private const int VIDA_MARCA = 240;        // 4 segundos de sentencia
        private const int TICKS_CADENA = 9;        // el salto de la rotura

        private NPC Victima => Projectile.ai[1] > 0 && Projectile.ai[1] <= Main.maxNPCs
            ? Main.npc[(int)Projectile.ai[1] - 1] : null;
        private bool RompeElUmbral => Projectile.ai[0] >= 1f;
        private int Cadena => (int)Projectile.localAI[0];   // los saltos dados

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA_MARCA + TICKS_CAIDA + 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TICKS_CADENA;
            Projectile.alpha = 255;               // invisible al pipeline vanilla
        }

        public override void AI()
        {
            NPC v = Victima;
            bool viva = v != null && v.active && v.life > 0;

            // ── LA CAÍDA DE LA SENTENCIA (los primeros ticks) ──
            if (Projectile.localAI[1] < TICKS_CAIDA)
            {
                Projectile.localAI[1]++;
                float fase = Projectile.localAI[1] / (float)TICKS_CAIDA;
                // nace 130 px sobre el blanco y cae
                Projectile.Center = (viva ? v.Center : Projectile.Center)
                    + new Vector2(0f, -130f * (1f - fase));
                // LA ESTELA: la tinta que baja con la marca
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-10f, 0f)),
                        DustID.Shadowflame, new Vector2(0f, 1.6f), 120,
                        new Color(148, 72, 178), 0.55f);
                    d.noGravity = true;
                }
                return;
            }

            // ── LA MARCA CLAVADA: sigue a su víctima ──
            if (viva) Projectile.Center = v.Center;

            // LA LLUVIA FINA: la tinta del cielo hacia la marca (la
            // firma del arma — dos gotas suaves cada 3 ticks)
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-26f, 26f), -Main.rand.NextFloat(80f, 130f)),
                    DustID.Shadowflame, new Vector2(0f, 2.4f), 140,
                    new Color(120, 60, 190), 0.45f);
                d.noGravity = true;
            }

            // ── LA VIDA: la marca respira SU latido (el sello) ──
            // (el dibujo vive en PreDraw — aquí solo la lógica)

            // ── LA MUERTE NATURAL: detona con su golpe ──
            if (Projectile.timeLeft <= 60 && !RompeElUmbral)
            {
                Detonar();
                Projectile.Kill();
                return;
            }

            // ── LA ROTURA DEL UMBRAL: la octava marca ROMPE todo ──
            if (RompeElUmbral && Projectile.localAI[1] >= TICKS_CAIDA && !Projectile.localAI[2].Equals(1f))
            {
                // (la rotura la dispara la marca con ai[0]=1 — el arma
                // la lanza con el octavo clic; el resto detona POR
                // CADENA: cada marca golpea a SU víctima y despierta
                // a las demás con SU salto)
                Projectile.localAI[2] = 1f;
                Detonar();
                DespertarLaCadena();
                Projectile.Kill();
                return;
            }
        }

        /// <summary>EL GOLPE de la marca: daño del arma a su víctima
        /// (o a quien esté bajo la marca) + el destello de tinta.</summary>
        private void Detonar()
        {
            NPC v = Victima;
            if (v != null && v.active && v.life > 0 && !v.dontTakeDamage)
            {
                // EL GOLPE DIRECTO (con la escala de la cadena: cada
                // salto pega MÁS FUERTE — la sentencia se enfada) — por
                // EL MOTOR DE LA CASA (GolpeMotor: el cauce que ya usan
                // todos los sub-ataques, con las stats del dueño).
                float mult = 1f + 0.25f * Cadena;
                int dano = (int)(Projectile.damage * mult);
                global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, v, dano, 0f);
                v.AddBuff(ModContent.BuffType
                    <global::AethonMod.Content.Buffs.QuemaduraVacioExterior>(), 60, false);
            }
            // EL TEATRO: el charco que salpica (12 gotas radiales)
            for (int k = 0; k < 12; k++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    Main.rand.NextVector2CircularEdge(3.2f, 3.2f) *
                    Main.rand.NextFloat(0.5f, 1.2f), 130, new Color(170, 90, 230), 0.75f);
                d.noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69.WithPitchOffset(-0.25f)
                .WithVolumeScale(0.5f), Projectile.Center);
        }

        /// <summary>LA CADENA: al romperse el umbral, las OTRAS marcas
        /// vivas despiertan una a una (la sentencia VIAJA — cada marca
        /// despierta a la siguiente con 9 ticks de salto).</summary>
        private void DespertarLaCadena()
        {
            int salto = Cadena + 1;
            if (salto > 12) return;   // la cadena se agota (12 saltos)
            int tipo = Projectile.type;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.whoAmI == Projectile.whoAmI)
                    continue;
                if (p.owner != Projectile.owner) continue;
                // SOLO las marcas CLAVADAS y no rotas
                if (p.localAI[1] < TICKS_CAIDA || p.localAI[2].Equals(1f)) continue;
                // LA MARCA DESPIERTA con su salto contado
                p.localAI[0] = salto;
                p.ai[0] = 1f;   // rompe en el siguiente tick
                break;          // UNA a la vez (la cadena en serie)
            }
        }

        public override bool PreDraw(ref Color luzColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                // ========================================================
                //  EL DIBUJO DE LA MARCA (100 % procedural — lote
                //  aditivo de la casa, CERO texturas del proyecto):
                //  la caída dibuja la estela; la marca clavada dibuja
                //  EL SELLO — un glifo de tinta latiendo sobre la
                //  víctima (círculo + la runa del umbral) + la gota
                //  que cae del cielo.
                // ========================================================
                VFXCore.AbrirAdditive();
                try
                {
                    Vector2 pos = Projectile.Center - Main.screenPosition;
                    float t = Main.GlobalTimeWrappedHourly;
                    bool cayendo = Projectile.localAI[1] < TICKS_CAIDA;

                    if (cayendo)
                    {
                        // LA ESTELA DE LA CAÍDA: el trazo vertical que baja
                        Quad(VFXCore.SoftGlow, pos, new Vector2(6f, 26f), 0f,
                            VFXCore.TintAlfa(new Color(170, 90, 230), 0.75f));
                        Quad(VFXCore.SoftGlow, pos, new Vector2(14f, 14f), 0f,
                            VFXCore.TintAlfa(new Color(216, 186, 255), 0.55f));
                    }
                    else
                    {
                        // EL SELLO CLAVADO: círculo rúnico + latido
                        float late = 0.75f + 0.25f * MathF.Sin(t * 6f);
                        VFXCore.RingQuad(pos, 15f * late, t * 0.8f,
                            VFXCore.TintAlfa(new Color(190, 120, 255), 0.80f));
                        // LA RUNA DEL UMBRAL: la barra vertical con su cruz
                        Quad(VFXCore.SoftGlow, pos, new Vector2(3.5f, 17f), 0f,
                            VFXCore.TintAlfa(new Color(148, 72, 178), 0.90f));
                        Quad(VFXCore.SoftGlow, pos, new Vector2(11f, 3f), 0f,
                            VFXCore.TintAlfa(new Color(148, 72, 178), 0.70f));
                        // EL HALO suave (la marca se ve SIEMPRE)
                        Quad(VFXCore.SoftGlow, pos, new Vector2(34f, 34f), 0f,
                            VFXCore.TintAlfa(new Color(120, 60, 190), 0.22f * late));
                        // LA CADENA: si esta marca ya saltó, el rayo que la une
                        if (Cadena > 0)
                        {
                            Vector2 origen = pos - new Vector2(0f, 90f + 12f * MathF.Sin(t * 4f));
                            Quad(VFXCore.SoftGlow, (pos + origen) * 0.5f,
                                new Vector2(2.4f, Vector2.Distance(pos, origen)), 0f,
                                VFXCore.TintAlfa(new Color(216, 186, 255), 0.38f));
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }
            }
            catch { try { VFXCore.CerrarLoteSiAbierto(); } catch { } }
            return false;
        }

        /// <summary>Quad centrado directo al lote aditivo activo.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        public override bool? CanHitNPC(NPC target) => false;   // el daño es de la detonación
    }
}
