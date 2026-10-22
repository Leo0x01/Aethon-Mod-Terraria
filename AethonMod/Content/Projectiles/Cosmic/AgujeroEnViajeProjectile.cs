using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EL AGUJERO EN VIAJE (v6.51.3) — EL NACIMIENTO CONTINUO. LA LETRA:
    /// «los agujeros negros runicos no nacen correctamente donde tienen
    /// que nacer, aunque la animacion se hace este nace directamente en
    /// el enemigo».
    ///
    /// v6.51.4 — LA CURA DE LA CURA (la letra del playtest: «siguen sin
    /// nacer donde deben nacer, se hace la animacion pero aparecen en
    /// otro lugar»): la .51.3 le puso el VIAJE a la semilla pero la
    /// APERTURA seguía clavando el agujero en el CENTRO de la presa
    /// (dentro de su sprite — «nace directamente en el enemigo» otra
    /// vez) y la semilla era tan tenue que el viaje casi no se veía.
    /// AHORA: la semilla es un núcleo de VACÍO CON BRUMA (BrumaFX de la
    /// casa — nada de discos planos) que RESPLANDECE con su luz propia,
    /// frena a la distancia LIMPIA del BORDE de la presa (nunca dentro)
    /// y el agujero SE ABRE EXACTAMENTE DONDE LA SEMILLA TERMINÓ SU
    /// VUELO — donde la animación dijo, ahí nace: imposible leerlo mal.
    ///
    /// ai[0] = el TIPO de agujero que abrirá · ai[1] = el whoAmI del
    /// blanco · ai[2] = el daño combinado del ritual.
    /// </summary>
    public class AgujeroEnViajeProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        /// <summary>La vida máxima del viaje (~1,5 s — la semilla no se pierde).</summary>
        private const int VIDA_MAX = 90;

        /// <summary>¿A qué distancia del BORDE de la presa se ABRE? (la
        /// semilla jamás entra al cuerpo: el agujero nace ENCIMA de la
        /// presa, no DENTRO — la letra de la .51.3).</summary>
        private const float ABRE_DEL_BORDE = 34f;

        private int TipoAgujero => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;      // el viaje no daña: lo que ABRE daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA_MAX;
            Projectile.netImportant = true;   // el nacimiento es de todos los cables
        }

        public override void AI()
        {
            // === EL BLANCO (el del ritual — si murió a media carrera, el más
            //     cercano; si no hay nadie, la semilla se abre donde esté). ===
            NPC blanco = BlancoVivo();

            // === LA LUZ DE LA SEMILLA (v6.51.4: el viaje se VE — un
            //     foco violeta cruzando el bioma, del tamaño de lo que
            //     viene). ===
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.30f, 0.14f, 0.55f);

            // === EL VUELO: acelera hacia su cita (una semilla con HAMBRE —
            //     arranca rápida y APRIETA al ver la presa: cerca del final
            //     FRENA — el nacimiento se anuncia, no se teletransporta). ===
            if (blanco != null)
            {
                // LA DISTANCIA LIMPIA: del BORDE del cuerpo (nunca del
                // centro): la semilla para DONDE ACABA LA PRESA.
                float abreA = ABRE_DEL_BORDE +
                    MathHelper.Clamp(blanco.width * 0.5f, 10f, 130f);
                Vector2 hacia = blanco.Center - Projectile.Center;
                float dist = hacia.Length();
                if (dist > 0.01f)
                {
                    hacia /= dist;
                    // CERCA FRENA (la cita se anuncia): lejos acelera, a
                    // menos de 120 px el empuje se apaga suave.
                    float empuje = dist < 120f
                        ? MathHelper.Clamp(dist / 120f, 0f, 1f) : 1f;
                    Projectile.velocity += hacia * (1.15f * empuje);
                }
                float vmax = dist < 160f ? MathHelper.Lerp(6f, 15.5f,
                    MathHelper.Clamp((dist - 30f) / 130f, 0f, 1f)) : 15.5f;
                if (Projectile.velocity.Length() > vmax)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * vmax;

                // === LA CITA: la semilla LLEGÓ a su distancia limpia —
                //     el agujero se ABRE AQUÍ, donde el vuelo terminó
                //     (v6.51.4: EXACTAMENTE donde la animación lo dijo —
                //     jamás "en otro lugar"). ===
                if (dist <= abreA)
                {
                    AbrirAgujero(Projectile.Center);
                    return;
                }
            }
            else
            {
                Projectile.velocity *= 0.96f;
            }

            // === LA VOZ DE LA SEMILLA (el silbido del vacío acercándose). ===
            if (Projectile.timeLeft == VIDA_MAX - 30)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.55f), Projectile.Center);

            // === EL RELOJ DE SEGURIDAD: sin presa o demasiado lejos — la
            //     semilla se abre DONDE ESTÁ (un agujero rúnico suelto
            //     también sabe cazar: su homing hace el resto). ===
            if (Projectile.timeLeft <= 1)
            {
                AbrirAgujero(Projectile.Center);
                return;
            }
        }

        /// <summary>El blanco del ritual (o el más cercano vivo).</summary>
        private NPC BlancoVivo()
        {
            int idx = (int)Projectile.ai[1];
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                NPC n = Main.npc[idx];
                if (n != null && n.active && VFXCore.EsObjetivo(n)) return n;
            }
            NPC mejor = null;
            float mejorD = 1600f * 1600f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>LA APERTURA: el agujero rúnico de siempre nace AQUÍ —
        /// donde la semilla terminó su vuelo, con su daño del ritual, su
        /// vida de siempre y SU BRUMA (la de la casa, BrumaFX — la letra
        /// de la .51.4: cubrir con bruma, nada de geometría plana).
        /// SOLO la máquina dueña abre (el patrón de la casa:
        /// NewProjectile sincroniza).</summary>
        private void AbrirAgujero(Vector2 donde)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int dano = Math.Max(1, (int)Projectile.ai[2]);
                Projectile agujero = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    donde, Vector2.Zero, TipoAgujero, dano, 0f, Projectile.owner);
                agujero.timeLeft = 600;

                // v6.51.4 — LA BRUMA DE LA APERTURA (la letra: «solo se
                //     debe cubrir con bruma»): la nube violeta del
                //     nacimiento — BrumaFX de la casa, no discos.
                Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    donde, Vector2.Zero,
                    ModContent.ProjectileType<BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 2f, 1f);
            }

            // LAS ASCUAS (pocas — la firma viva entre la niebla).
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust m = Dust.NewDustPerfect(donde, DustID.Shadowflame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(1.2f, 3.4f),
                        150, new Color(140, 70, 210), Main.rand.NextFloat(1.2f, 2.0f));
                    m.noGravity = true;
                    m.fadeIn = 0f;
                }
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(0.35f), donde);
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center;
                int semilla = Projectile.identity % 71 + 3;

                // ============================================================
                //  v6.51.4 — LA SEMILLA DE BRUMA (la letra: «teniamos ya
                //  una buena libreria de brumas completamente funcional
                //  que no ponen esos circulos planos… solo se debe cubrir
                //  con bruma, nada geometria plana»): el cuerpo de la
                //  semilla YA NO son discos lisos — es UNA BOLA DE HUMO
                //  VIOLETA de la casa (BrumaFX, los puffs fBm que se
                //  desgarran) con su corazón de vacío y sus runas.
                // ============================================================
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    BrumaFX.Cloud(centro - Main.screenPosition, 26f,
                        new Color(46, 20, 74), semilla, t, 5, alpha: 0.62f);
                    BrumaFX.Cloud(centro - Main.screenPosition + new Vector2(0f, -6f), 15f,
                        new Color(66, 30, 100), semilla + 17, t * 1.2f, 3, alpha: 0.50f);
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                // EL CORAZÓN Y LAS RUNAS (el lote aditivo de la casa):
                // un núcleo pequeño (la semilla ARDE, no es un disco) +
                // dos aros de runas que giran + la estela del vuelo.
                VFXCore.Begin();
                try
                {
                    VFXCore.Quad(centro, new Color(190, 120, 255) * 0.50f,
                        new Vector2(16f, 16f));
                    VFXCore.Quad(centro, new Color(28, 10, 48) * 0.85f,
                        new Vector2(10f, 10f));

                    for (int a = 0; a < 2; a++)
                    {
                        float rA = 26f - a * 7f;
                        float giro = -t * (2.2f + a * 1.1f);
                        int runas = 6 + a * 2;
                        for (int k = 0; k < runas; k++)
                        {
                            float angR = giro + k / (float)runas * MathHelper.TwoPi;
                            Vector2 posR = centro + new Vector2(
                                MathF.Cos(angR) * rA, MathF.Sin(angR) * rA * 0.85f);
                            VFXCore.Quad(posR, new Color(210, 150, 255) * 0.75f,
                                new Vector2(2.8f, 6.2f), angR + MathHelper.PiOver2);
                        }
                    }

                    // LA ESTELA DEL VUELO: la semilla llora vacío.
                    Vector2 rumbo = Projectile.velocity.LengthSquared() > 0.1f
                        ? Vector2.Normalize(Projectile.velocity) : -Vector2.UnitY;
                    for (int e = 1; e <= 3; e++)
                    {
                        Vector2 posE = centro - rumbo * (e * 16f);
                        VFXCore.Quad(posE,
                            new Color(150, 80, 220) * (0.35f / e),
                            new Vector2(22f / e, 22f / e));
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }
}
