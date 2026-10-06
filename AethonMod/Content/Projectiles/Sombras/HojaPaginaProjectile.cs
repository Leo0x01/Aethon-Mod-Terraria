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
    /// HOJAPAGINAPROJECTILE — v6.50.68 — ARMA NUEVA 2: LA HOJA DE LA PÁGINA.
    ///
    /// La letra del usuario: «cambia los otros bastones por conceptos
    /// diferentes». EL JUEGO DE PALABRAS: «hoja» es página Y filo — esta
    /// arma ES la página doblada hasta cortar. Concepto DISTINTO al
    /// tentáculo: nada de agarrar ni drenar — es VELOCIDAD PURA.
    ///
    /// EL CONCEPTO: al usarla, CUATRO HOJAS-FILO (crescentes de sombra
    /// con filo de energía y punta de hueso — el mismo ADN visual de la
    /// casa) se materializan en bruma y ORBITAN al portador girando y
    /// acelerando (el MOLINO: dañan al contacto). Tras 44 ticks el
    /// molino SE DISPARA: las cuatro salen en abanico hacia el cursor,
    /// atravesando todo a su paso con estelas y after-images (el filo
    /// de la página no se detiene). Si un golpe mata a un jefe: LA
    /// DEVORACIÓN, con su firma — el festín estilo 9, EL MOLINO DE
    /// FILOS (el círculo que se ciñe).
    ///
    /// LA PUREZA: la posición de cada hoja es FUNCIÓN PURA del tick
    /// (determinista — la misma en cliente, server y dibujo). El ancla
    /// es el portador mientras orbita; al disparar se CONGELA (las
    /// hojas salen desde su última órbita). 100% código.
    /// </summary>
    public class HojaPaginaProjectile : ModProjectile
    {
        private const float T_SALE = 44f;      // ticks de órbita antes del disparo
        private const float VUELO = 34f;       // velocidad de vuelo px/t

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)T_SALE + 54;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        /// <summary>El rumbo del disparo (al cursor — lo trae la velocity del item).</summary>
        private float Rumbo => Projectile.ai[0];

        /// <summary>El ancla: el portador mientras orbita; congelada al disparar.</summary>
        private Vector2 Ancla
        {
            get
            {
                if (Projectile.ai[1] >= 1f)
                    return new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                Player dueño = Main.player[Projectile.owner];
                return dueño.MountedCenter;
            }
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];

            if (t < T_SALE)
            {
                // FASE 1 · EL MOLINO — las hojas orbitan al portador
                Projectile.Center = Main.player[Projectile.owner].MountedCenter;
                Projectile.velocity = Vector2.Zero;
                if (t == 1f)
                    Sonar(SoundID.Item122.WithPitchOffset(-0.18f).WithVolumeScale(0.5f), Projectile.Center);
            }
            else
            {
                // FASE 2 · EL DISPARO — congela el ancla y vuela en abanico
                if (Projectile.ai[1] < 1f)
                {
                    Projectile.ai[1] = 1f;
                    Player dueño = Main.player[Projectile.owner];
                    Projectile.localAI[0] = dueño.MountedCenter.X;
                    Projectile.localAI[1] = dueño.MountedCenter.Y;
                    Projectile.netUpdate = true;
                    Sonar(SoundID.Item122.WithPitchOffset(0.34f).WithVolumeScale(0.85f), Projectile.Center);
                    Sonar(SoundID.Item74.WithPitchOffset(-0.12f).WithVolumeScale(0.6f), Projectile.Center);
                }
                // el CUERPO del proyectil cabalga la hoja 0 (el hitbox
                // base no manda: Colliding prueba las cuatro)
                Projectile.Center = PosHoja(0, t);
                Projectile.velocity = DirVuelo(0) * VUELO;
            }

            if (t >= T_SALE + 52f) Projectile.Kill();
        }

        // ==================================================================
        //  LA GEOMETRÍA PURA — la posición de cada hoja es una función del
        //  tick (misma en AI, Colliding y Draw: determinismo gratis)
        // ==================================================================

        /// <summary>La posición de la hoja k en el tick t (órbita o vuelo).</summary>
        private Vector2 PosHoja(int k, float t)
        {
            Vector2 ancla = Ancla;
            if (t < T_SALE)
                return PosOrbita(k, t, ancla);

            // EL VUELO — desde su última posición de órbita, en abanico
            Vector2 salida = PosOrbita(k, T_SALE, ancla);
            return salida + DirVuelo(k) * (VUELO * (t - T_SALE));
        }

        /// <summary>La órbita de la hoja k: radio y velocidad angular que CRECEN.</summary>
        private Vector2 PosOrbita(int k, float t, Vector2 ancla)
        {
            float f = MathHelper.Clamp(t / T_SALE, 0f, 1f);
            float radio = MathHelper.Lerp(34f, 94f, f);
            float w = 0.17f + 0.15f * f;                       // el molino ACELERA
            float ang = Rumbo + t * w + k * MathHelper.PiOver2;
            return ancla + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
        }

        /// <summary>El rumbo de vuelo de la hoja k (abanico estrecho al cursor).</summary>
        private Vector2 DirVuelo(int k)
        {
            float spread = (k - 1.5f) * 0.075f;
            return (Rumbo + spread).ToRotationVector2();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA DEVORACIÓN: si ESTE golpe mata, la muerte es NUESTRA —
            // el festín estilo 9 (el molino de filos)
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 9);

            try { Terraria.Audio.SoundEngine.PlaySound(
                SoundID.NPCHit9.WithPitchOffset(0.1f).WithVolumeScale(0.3f), Projectile.Center); } catch { }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LAS CUATRO HOJAS: círculo de 26 px alrededor de cada una
            Rectangle expandido = targetHitbox;
            expandido.Inflate(26, 26);
            float t = Projectile.ai[2];
            for (int k = 0; k < 4; k++)
            {
                Vector2 pos = PosHoja(k, t);
                if (expandido.Contains((int)pos.X, (int)pos.Y)) return true;
            }
            return false;
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
            int semilla = Projectile.whoAmI * 19 + 5;
            float t = Projectile.ai[2];
            bool volando = t >= T_SALE;

            // === LA FORJA — la bruma del portador mientras se arma el
            // molino (y el anillo del radio, vivo) ===
            if (!volando)
            {
                SombrasLib.Bruma(Ancla, 46f, 0.35f, tiempo, semilla);
                float f = MathHelper.Clamp(t / T_SALE, 0f, 1f);
                float radio = MathHelper.Lerp(34f, 94f, f);
                VFXCore.Begin();
                VFXCore.Quad(Ancla, SombrasLib.Alfa(SombrasLib.Violeta, 0.16f + 0.10f * MathF.Sin(tiempo * 5f)),
                    VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                VFXCore.FlushAdditive();
            }

            // === LAS CUATRO HOJAS (cada una con su fantasma de giro) ===
            for (int k = 0; k < 4; k++)
            {
                Vector2 pos = PosHoja(k, t);
                Vector2 dir = volando ? DirVuelo(k) : DirOrbita(k, t);
                float lado = k % 2 == 0 ? 1f : -1f;
                float largo = volando ? 56f : 48f;

                // EL FANTASMA — el after-image del giro (la hoja hace 3
                // ticks: aún se ve dónde ESTABA cortando)
                Vector2 posFantasma = PosHoja(k, MathF.Max(t - 3f, 0f));
                Vector2 dirFantasma = volando ? dir : DirOrbita(k, t - 3f);
                SombrasLib.Garra(posFantasma - dirFantasma * largo * 0.45f, dirFantasma,
                    largo * 0.85f, 0.32f, 0.5f * lado);

                // LA HOJA — el crescente de sombra con punta de hueso
                SombrasLib.Garra(pos - dir * largo * 0.5f, dir, largo, 0.95f, 0.5f * lado);

                // LA ESTELA del vuelo — el corte escrito en el aire
                if (volando)
                {
                    VFXCore.Begin();
                    VFXCore.Line(pos - dir * 30f, pos - dir * 92f,
                        SombrasLib.Alfa(k > 1 ? SombrasLib.Rojo : SombrasLib.Violeta, 0.42f), 6f);
                    VFXCore.FlushAdditive(VFXCore.Pixel);
                }
            }
        }

        /// <summary>La tangente de la órbita de la hoja k (hacia dónde corta al girar).</summary>
        private Vector2 DirOrbita(int k, float t)
        {
            float f = MathHelper.Clamp(t / T_SALE, 0f, 1f);
            float w = 0.17f + 0.15f * f;
            float ang = Rumbo + t * w + k * MathHelper.PiOver2;
            return new Vector2(-MathF.Sin(ang), MathF.Cos(ang));   // tangente del giro
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // el filo se disuelve en bruma
            SombrasLib.Bruma(Projectile.Center, 40f, 0.5f, Main.GlobalTimeWrappedHourly,
                Projectile.whoAmI * 19 + 5);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
