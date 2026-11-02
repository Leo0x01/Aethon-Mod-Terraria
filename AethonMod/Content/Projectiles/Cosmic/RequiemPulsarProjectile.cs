using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RequiemPulsarProjectile — v6.82 — LA ESTRELLA DEL REQUIEM DEL PÚLSAR.
    ///
    /// LA LETRA: «investiga tecnicas vfx de otros mods, investiga armas
    /// de otros mod, y con esa investigacion y las librerias que conoces,
    /// dame una nueva arma de tipo cosmico».
    ///
    /// LA INVESTIGACIÓN (el informe de los mods top): las armas premium
    /// de Calamity/Stars Above comparten CINCO técnicas — (1) los FAROS
    /// ROTANTES (la antena de un púlsar barre TODO lo que toca), (2) las
    /// ESTELAS Catmull-Rom con la textura fluyendo (EstelaLib.Ribbon —
    /// la librería de la casa), (3) los COLORES OVERBRIGHT ×1.4 (el truco
    /// HDR de Scarlet Devil: el cian ×1.4 ARDE sobre aditivo), (4) los
    /// ANILLOS DE PULSO que se expanden periódicos (la firma de radio de
    /// un púlsar real — DistortionPulse de Starlight River), y (5) el
    /// COLAPSO al morir: implosión + anillo de choque + la última
    /// aberración (CalorLib.Estallido — el aire se parte en R/G/B).
    ///
    /// EL PÚLSAR (todo vive en la IA + el PreDraw, cero estado de red):
    ///   · LA ESTRELLA: núcleo blanco-platino ×1.4 overbright con SU
    ///     pulso cardíaco (la frecuencia del faro: 4 rev/s).
    ///   · LOS DOS FAROS: los haces gemelos opuestos barriendo el
    ///     espacio (cápsulas dobles: núcleo blanco + funda cian, la
    ///     punta afilada — EL LIGHTHOUSE de todo púlsar).
    ///   · LA ESTELA: Ribbon Comet cian→violeta con la anchura muriendo
    ///     hacia atrás (la historia del camino).
    ///   · LOS PULSOS DE RADIO: cada 12 ticks el anillo expandiéndose
    ///     (la señal del púlsar — dos vivas a la vez).
    ///   · LA PERSECUCIÓN: homing suave del sol de la casa.
    ///   · EL COLAPSO (OnKill): la implosión + el anillo de Einstein
    ///     pequeño + EL ESTALLIDO DE CALOR (la aberración cromática del
    ///     golpe — el aire se parte en R/G/B alrededor de la muerte).
    /// </summary>
    public class RequiemPulsarProjectile : ModProjectile
    {
        /// <summary>La frecuencia del faro (rev/s — un púlsar veloz).</summary>
        private const float RevolucionesSeg = 4f;

        /// <summary>Cada cuántos ticks nace un ANILLO DE RADIO.</summary>
        private const int TicksPorPulso = 12;

        /// <summary>La vida máxima (los pulsos que le alcanzan a emitir).</summary>
        private const int VidaMax = 240;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = VidaMax;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>El ángulo del faro (vive en Projectile.rotation — el giro del púlsar).</summary>
        private float Faro => Projectile.rotation;

        public override void AI()
        {
            // === EL GIRO DEL FARO (el corazón del púlsar) ===
            Projectile.rotation += RevolucionesSeg * MathHelper.TwoPi / 60f;

            // === LA PERSECUCIÓN SUAVE (el patrón del sol de la casa) ===
            NPC prey = FindNearestEnemy(560f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 60f)
                {
                    toPrey.Normalize();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                        toPrey * 14f, 0.035f);
                }
            }

            // === LA ESTELA (la historia del camino — la librería de la casa) ===
            EstelaLib.Track(Projectile.whoAmI, 24).Push(Projectile.Center);
            if (Projectile.timeLeft % 120 == 0) EstelaLib.PurgeTracks();

            if (Main.netMode == NetmodeID.Server) return;

            // === EL PULSO CARDÍACO (la luz que late con el faro) ===
            float latido = 0.75f + 0.25f * MathF.Sin(Projectile.rotation * 2f);
            Vector3 luz = new Vector3(0.55f, 0.95f, 1.15f) * (0.9f + 0.5f * latido);
            Lighting.AddLight(Projectile.Center, luz);

            // === EL RASTRO DE POLVO ESTELAR (escaso: el púlsar es limpio) ===
            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    -Projectile.velocity * 0.12f + new Vector2(
                        Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-0.8f, 0.8f)),
                    160, new Color(140, 225, 255), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === EL COLAPSO: la estrella muere como nació — GIRANDO ===
            // LA IMPLOSIÓN (las partículas del cielo caen al centro).
            ParticlePresets.Implosion(Projectile.Center, 90f, 22,
                new Color(140, 225, 255), 20);
            // EL ANILLO DE EINSTEIN PEQUEÑO (el horizonte que se escapa).
            ParticlePresets.RingPulse(Projectile.Center, 120f,
                new Color(200, 245, 255, 170), 26);
            // EL ESTALLIDO DE CALOR (la aberración cromática del golpe —
            // el aire se parte en R/G/B alrededor de la muerte: la
            // técnica del estallido de la casa, la firma del research).
            CalorLib.Estallido(Projectile.Center, 14f, 300f);

            // LOS ÚLTIMOS POLVOS (cian y violeta, girando hacia adentro).
            for (int i = 0; i < 26; i++)
            {
                float ang = i * MathHelper.TwoPi / 26f + Faro;
                float dist = Main.rand.NextFloat(40f, 90f);
                Vector2 pos = Projectile.Center + new Vector2(
                    MathF.Cos(ang) * dist, MathF.Sin(ang) * dist);
                Vector2 toC = Projectile.Center - pos;
                if (toC.LengthSquared() > 0.01f)
                {
                    toC.Normalize();
                    Dust d = Dust.NewDustPerfect(pos, DustID.Crimson,
                        toC * 6f, 210, i % 3 == 0
                            ? new Color(140, 90, 255)
                            : new Color(140, 225, 255), 1.1f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 4f, 8, 14, 0.35f,
                    "AethonRequiemPulsar"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item92.WithPitchOffset(0.25f), Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // EL MICRO-ESTALLIDO DEL GOLPE (la chispa del faro tocando materia).
            ParticlePresets.RingPulse(target.Center, 60f,
                new Color(190, 240, 255, 170), 14);
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist.</summary>
        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        // ==================================================================
        //  EL RENDER — 100% CÓDIGO (el púlsar entero, capas de la casa)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;

            // LAS COORDENADAS DE LA CASA: el lote aditivo abre con la
            // GameViewMatrix — coords de PANTALLA.
            Vector2 centro = Projectile.Center - Main.screenPosition;
            float t = Main.GlobalTimeWrappedHourly;
            float faro = Faro;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    DibujarLaEstrella(centro, t, faro);
                    DibujarLosFaros(centro, t, faro);
                    DibujarLosPulsos(centro, t);
                    DibujarLaEstela(t);
                }
                finally
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>LA ESTRELLA: el núcleo blanco-platino ×1.4 OVERBRIGHT
        /// (el truco HDR de las armas premium: el color ARDE sobre el
        /// aditivo) con SU pulso cardíaco y el rim cian.</summary>
        private static void DibujarLaEstrella(Vector2 centro, float t, float faro)
        {
            // EL LATIDO: la estrella respira al doble de la frecuencia del faro.
            float latido = 0.80f + 0.20f * MathF.Sin(faro * 2f);

            // EL HALO AMPLIO (cian ×1.4 — overbright de la casa).
            Quad(centro, new Vector2(64f, 64f) * latido, 0f,
                VFXCore.TintAlfa(new Color(110, 215, 255), 0.34f * latido));
            // LA CÁMARA DE PLASMA (violeta del vacío, más chico).
            Quad(centro, new Vector2(38f, 38f) * latido, 0f,
                VFXCore.TintAlfa(new Color(150, 100, 255), 0.30f * latido));
            // EL NÚCLEO (blanco-platino: el cuerpo de la estrella).
            Quad(centro, new Vector2(22f, 22f) * latido, 0f,
                VFXCore.TintAlfa(new Color(248, 252, 255), 0.95f * latido));
            // EL OJO (el punto cegador: nada más que luz).
            Quad(centro, new Vector2(9f, 9f) * latido, 0f,
                VFXCore.TintAlfa(new Color(255, 255, 255), 1f));
        }

        /// <summary>LOS DOS FAROS: los haces gemelos opuestos barriendo el
        /// espacio — cápsulas dobles (funda cian + núcleo blanco) con la
        /// punta AFILADA y el parpadeo vivo (EL LIGHTHOUSE del púlsar).</summary>
        private static void DibujarLosFaros(Vector2 centro, float t, float faro)
        {
            if (!VFXCore.Presupuesto(40)) return;

            for (int lado = 0; lado < 2; lado++)
            {
                float ang = faro + lado * MathHelper.Pi;
                Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));

                // EL PARPADEO (el rayo vivo: ~9 Hz con el hash del frame).
                float flick = 0.72f + 0.28f * MathF.Sin(t * 56f + lado * 2.4f);

                // EL HAZ: TRES tramos decrecientes (la aguja se afila al
                // final — 86 → 56 → 30 px, cada uno más fino).
                float[] largos = { 86f, 56f, 30f };
                float[] anchos = { 13f, 8f, 4.5f };
                float avance = 6f;
                for (int tramo = 0; tramo < largos.Length; tramo++)
                {
                    float len = largos[tramo] * flick;
                    float w = anchos[tramo];
                    Vector2 mid = centro + dir * (avance + len * 0.5f);
                    // LA FUNDA cian (la envolvente del haz).
                    VFXCore.Capsule(mid, len, w, ang,
                        VFXCore.TintAlfa(new Color(110, 215, 255), 0.30f * flick));
                    // EL NÚCLEO blanco (la vena que ARDE — overbright).
                    VFXCore.Capsule(mid, len * 0.86f, w * 0.42f, ang,
                        VFXCore.TintAlfa(new Color(235, 250, 255), 0.65f * flick));
                    avance += len;
                }

                // LA PERLA DE LA PUNTA (donde el faro termina).
                Vector2 punta = centro + dir * (6f + 86f * flick);
                Quad(punta, new Vector2(10f, 10f) * flick, 0f,
                    VFXCore.TintAlfa(new Color(255, 255, 255), 0.75f * flick));
            }
        }

        /// <summary>LOS PULSOS DE RADIO: los anillos que la estrella emite
        /// cada 12 ticks (dos vivos a la vez — la señal periódica del
        /// púlsar real, el ritmo que la hace reconocible).</summary>
        private void DibujarLosPulsos(Vector2 centro, float t)
        {
            if (!VFXCore.Presupuesto(20)) return;

            int edad = VidaMax - Projectile.timeLeft;
            for (int k = 0; k < 2; k++)
            {
                // El pulso k-ésimo vivo: su edad desde el último múltiplo.
                int fasePulso = edad - (edad / TicksPorPulso - k) * TicksPorPulso;
                if (fasePulso < 0 || fasePulso > 26) continue;
                float prog = fasePulso / 26f;
                float radio = 24f + prog * 92f;
                // EL ANILLO (cian muriendo — el eco de la señal).
                VFXCore.RingQuad(centro, radio, prog * 1.4f + k * 0.5f,
                    VFXCore.TintAlfa(new Color(150, 232, 255), 0.42f * (1f - prog) * (1f - prog)));
            }
        }

        /// <summary>LA ESTELA: la cinta Comet de la casa (la anchura muere
        /// hacia atrás) con el gradiente cian→violeta del más allá.</summary>
        private void DibujarLaEstela(float t)
        {
            if (!VFXCore.Presupuesto(90)) return;

            EstelaTrack track = EstelaLib.Track(Projectile.whoAmI, 24);
            Vector2[] camino = track.Points();
            if (camino == null || camino.Length < 3) return;

            // A coords de PANTALLA (el lote abrió con la GameViewMatrix).
            for (int i = 0; i < camino.Length; i++)
                camino[i] -= Main.screenPosition;

            // LA CINTA: gruesa en la cabeza, decae hacia atrás (Comet) —
            // el color viaja cian (la estrella) → violeta (el vacío).
            Color cinta = Color.Lerp(new Color(140, 225, 255), new Color(150, 100, 255), 0.35f);
            EstelaLib.Ribbon(Main.spriteBatch, camino, 16f,
                EstelaProfile.Comet, cinta, 0.55f,
                Projectile.whoAmI * 31 + 7, t, head: true, taper: 0.65f);
        }

        /// <summary>Quad centrado directo al lote aditivo activo (el
        /// pincel mínimo de la casa — SoftGlow estirado a size px).</summary>
        private static void Quad(Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Texture2D tex = VFXCore.SoftGlow;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }
    }
}
