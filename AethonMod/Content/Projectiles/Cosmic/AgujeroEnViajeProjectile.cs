using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EL AGUJERO EN VIAJE (v6.51.3) — EL NACIMIENTO CONTINUO. LA LETRA:
    /// «los agujeros negros runicos no nacen correctamente donde tienen
    /// que nacer, aunque la animacion se hace este nace directamente en
    /// el enemigo».
    ///
    /// LA CAUSA: la ceremonia del rayo convergente gesta el vórtice en su
    /// punto focal — ENCIMA DEL JUGADOR — pero al cumplir, el agujero se
    /// materializaba DE GOLPE sobre la presa: el nacimiento se cortaba a
    /// mitad (la animación decía «aquí» y el juego decía «allá»).
    ///
    /// LA CURA: al cumplir el plazo, la SEMILLA del agujero nace EN EL
    /// PUNTO FOCAL del ritual (donde la animación la gestó) y VIAJA hacia
    /// la presa — una semilla de vacío que arrastra su bruma violeta,
    /// acelera hacia su cita y al llegar SE ABRE: el agujero rúnico de
    /// siempre (cualquiera de los 10, intacto) nace EN SU SITIO con su
    /// estallido de niebla morada. Los agujeros NO se tocan: este
    /// proyectil es solo el CORDÓN UMBILICAL del nacimiento.
    ///
    /// ai[0] = el TIPO de agujero que abrirá · ai[1] = el whoAmI del
    /// blanco · ai[2] = el daño combinado del ritual.
    /// </summary>
    public class AgujeroEnViajeProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        /// <summary>La vida máxima del viaje (~1,5 s — la semilla no se pierde).</summary>
        private const int VIDA_MAX = 90;

        /// <summary>¿A qué distancia de la presa se ABRE el agujero?</summary>
        private const float ABRE_A = 30f;

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

            // === EL VUELO: acelera hacia su cita (una semilla con HAMBRE —
            //     arranca rápida y aprieta al ver la presa). ===
            if (blanco != null)
            {
                Vector2 hacia = blanco.Center - Projectile.Center;
                float dist = hacia.Length();
                if (dist > 0.01f)
                {
                    hacia /= dist;
                    Projectile.velocity += hacia * 1.15f;
                }
                float vmax = 15.5f;
                if (Projectile.velocity.Length() > vmax)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * vmax;

                // === LA CITA: la semilla LLEGÓ — el agujero se ABRE aquí,
                //     con su bruma morada y su voz. ===
                if (dist < ABRE_A)
                {
                    AbrirAgujero(blanco.Center);
                    return;
                }
            }
            else
            {
                Projectile.velocity *= 0.96f;
            }

            // === LA BRUMA DEL VIAJE (la letra: la niebla del color del
            //     agujero — VIOLETA, la del vacío): jirones que se quedan
            //     atrás, girando lentos alrededor del rumbo. ===
            if (Main.netMode != NetmodeID.Server && (Main.GameUpdateCount & 1) == 0u)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 posM = Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) *
                    Main.rand.NextFloat(8f, 26f);
                Dust m = Dust.NewDustPerfect(posM, DustID.Shadowflame,
                    -Projectile.velocity * 0.08f +
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                                Main.rand.NextFloat(-0.8f, -0.1f)),
                    110, new Color(128, 62, 198), Main.rand.NextFloat(1.3f, 2.2f));
                m.noGravity = true;
                m.fadeIn = 0f;
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
        /// con su daño del ritual, su vida de siempre y su bruma morada.
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
            }

            // LA BRUMA DE LA APERTURA (la niebla violeta del nacimiento).
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 22; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust m = Dust.NewDustPerfect(donde, DustID.Shadowflame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(1.5f, 4.5f),
                        150, new Color(140, 70, 210), Main.rand.NextFloat(1.4f, 2.4f));
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

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    // === LA SEMILLA: un núcleo de vacío que GIRA (el vórtice
                    //     en miniatura que será el agujero — ya tiene su
                    //     hambre) + dos aros que se ciñen. ===
                    VFXCore.Quad(centro, new Color(120, 50, 190) * 0.55f,
                        new Vector2(30f, 30f));
                    VFXCore.Quad(centro, new Color(28, 10, 48) * 0.85f,
                        new Vector2(19f, 19f));

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
                            VFXCore.Quad(posR, new Color(190, 120, 255) * 0.7f,
                                new Vector2(2.4f, 5.2f), angR + MathHelper.PiOver2);
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
