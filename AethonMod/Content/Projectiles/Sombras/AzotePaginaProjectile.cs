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
    /// AZOTEPAGINAPROJECTILE — v6.50.69 — ARMA NUEVA 1: EL AZOTE DE LA PÁGINA.
    ///
    /// La letra del usuario: «esas tres armas nuevas se ven y funcionan
    /// horrible» (la Pluma/Hoja/Sello de la .68 murieron) — la familia
    /// vuelve al ADN que el usuario AMA («me gusta más el aspecto de La
    /// Sombra de la Página»): TENTÁCULOS DE CARNE con bruma — pero cada
    /// hermana con su OFICIO distinto.
    ///
    /// EL AZOTE ES EL LÁTIGO: un tentáculo LARGO Y FINO que sale DEL
    /// JUGADOR y CRUJA en línea recta — LANZA (la punta vuela al cursor
    /// atravesándolo TODO en su camino) · CRUJE (el overshoot del látigo
    /// y su onda) · RECOGE (vuelve al portador). Es el ALCANCE y la
    /// rabia: el filo de energía rojo del cuerpo (Masa) corre raíz→punta
    /// y las espinas de hueso SIEMPRE afiladas. Si su golpe mata a un
    /// jefe: LA DEVORACIÓN — el festín estilo 8, EL LATIGAZO TRIPLE.
    ///
    /// 100% código: ni un sprite del arma. Cero dependencias.
    /// </summary>
    public class AzotePaginaProjectile : ModProjectile
    {
        private const byte FASE_LANZA = 0;
        private const byte FASE_CRUJE = 1;
        private const byte FASE_RECOGE = 2;

        /// <summary>El alcance máximo del látigo (px — CRUZA media pantalla).</summary>
        private const float ALCANCE = 640f;

        private float Rumbo => Projectile.ai[0];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 44;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte fase = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = dueño.MountedCenter;      // nace DEL JUGADOR (regla .68)

            // LA DISTANCIA de la punta: lanza (de golpe) → cruje (el
            // overshoot del látigo) → recoge (vuelve volando)
            float d;
            switch (fase)
            {
                case FASE_LANZA:
                {
                    d = SombrasLib.DeGolpe(MathHelper.Clamp(t / 13f, 0f, 1f)) * ALCANCE;
                    if (t == 2f) Sonar(SoundID.Item122.WithPitchOffset(-0.38f).WithVolumeScale(0.7f), Projectile.Center);
                    if (t >= 13f) { Projectile.ai[1] = FASE_CRUJE; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_CRUJE:
                {
                    // EL CRUJE: la punta se pasa de larga y RETROCEDE de un
                    // tirón (el chasquido del látigo — la física de verlet
                    // hace el resto del drama)
                    d = ALCANCE * (1.06f - 0.10f * SombrasLib.DeGolpe(t / 7f));
                    if (t == 1f) Sonar(SoundID.Item74.WithPitchOffset(-0.12f).WithVolumeScale(0.8f), Projectile.Center);
                    if (t >= 7f) { Projectile.ai[1] = FASE_RECOGE; Projectile.ai[2] = 0f; }
                    break;
                }
                default:
                {
                    d = ALCANCE * 0.96f * (1f - SombrasLib.DeGolpe(MathHelper.Clamp(t / 15f, 0f, 1f)));
                    if (t >= 15f) { Projectile.Kill(); return; }
                    break;
                }
            }

            Vector2 rumbo = Rumbo.ToRotationVector2();
            Projectile.Center = raiz + rumbo * d;
            Projectile.rotation = Rumbo;

            // el splat de impacto decae (lo dibuja el PreDraw)
            if (Projectile.localAI[0] > 0f) Projectile.localAI[0]--;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA DEVORACIÓN: si ESTE golpe mata a un jefe, la muerte es
            // NUESTRA — el festín estilo 8 (el latigazo triple)
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 8);

            Projectile.localAI[0] = 8f;
            Projectile.localAI[1] = target.Center.X;
            Projectile.localAI[2] = target.Center.Y;
            Sonar(SoundID.NPCHit9.WithPitchOffset(0.15f).WithVolumeScale(0.35f), Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL CUERPO ENTERO es el arma: la línea del azote (determinista
            // — la misma matemática del dibujo, sin tocar la física verlet)
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = dueño.MountedCenter;
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 13 + 1, 12, 0.30f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 9, ref punto))
                    return true;
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
            int semilla = Projectile.whoAmI * 13 + 1;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = dueño.MountedCenter;

            // === EL CUERPO DEL LÁTIGO: LARGO, FINO, con GANCHO en la punta
            // (la curva del azote colgando) — la física verlet da el crujido
            float vivo = fase == FASE_RECOGE ? 1f - MathHelper.Clamp(t / 16f, 0f, 1f) * 0.4f : 1f;
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 22, 0.30f, gancho: 0.7f);
            SombrasLib.Masa(col, 17f, 4f, 0.95f * vivo, semilla, tiempo);

            // === LA BRUMA DEL RASTRO — DOS CAPAS (v6.50.69: más bruma en
            // TODA la familia)
            SombrasLib.BrumaColumna(col, 20f, 0.55f * vivo, tiempo, semilla, 10);
            SombrasLib.BrumaColumna(col, 13f, 0.40f * vivo, tiempo, semilla + 67, 7);

            // === LA PUNTA DEL AZOTE: la cabeza de bruma (la forma actual)
            // + EL ARPÓN DE HUESO — tres púas de Colmillo en abanico
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());
            SombrasLib.CabezaDeBruma(punta, rumbo, 0.9f * vivo, vivo, tiempo, semilla);

            if (VFXCore.Colmillo != null)
            {
                VFXCore.Begin();
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 dirK = rumbo.RotatedBy(k * 0.34f);
                    VFXCore.Quad(punta + dirK * 12f, SombrasLib.Alfa(SombrasLib.Blanco, 0.95f * vivo),
                        new Vector2(8f, 34f), dirK.ToRotation() + MathHelper.PiOver2, VFXCore.Colmillo);
                }
                VFXCore.FlushAdditive();
            }

            // === EL CRUJE — la onda del chasquido al pasar a FASE_CRUJE
            if (fase == FASE_CRUJE && t < 8f)
                SombrasLib.OndaChoque(Projectile.Center, 30f + 60f * (t / 8f), 0.5f * (1f - t / 8f), roja: true);

            // === EL SPLAT DEL IMPACTO (dónde cayó el azote)
            if (Projectile.localAI[0] > 0f)
            {
                float f = 1f - Projectile.localAI[0] / 8f;
                Vector2 pos = new(Projectile.localAI[1], Projectile.localAI[2]);
                SombrasLib.OndaChoque(pos, 18f + 40f * f, 0.5f * (1f - f), roja: false);
                SombrasLib.Bruma(pos, 26f + 14f * f, 0.5f * (1f - f * 0.5f), tiempo, semilla + 9);
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
