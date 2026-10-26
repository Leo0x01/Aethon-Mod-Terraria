using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigacion
{
    /// <summary>
    /// RAYOSCONCENTRADOSHELD — v6.54.0 — EL CICLO DEL RAYO DEL JUGADOR.
    ///
    /// Las TRES fases del jefe (FusionRay.cs del puerto, la técnica
    /// documentada por la investigación 74-a):
    ///   · FASE 0 — LA CARGA (45 t): los resplandores de los SOLES
    ///     RÚNICOS (VFXCore.SoftGlow — la letra: «sustituir por los
    ///     brillos de los soles runicos») en 4 capas concéntricas
    ///     creciendo + la aguja telegráfica (el GlowRay de la casa
    ///     estirado, como el Glow[2] del jefe).
    ///   · FASE 1 — EL HAZ (150 t): el rayo concentrado de 2000 px
    ///     (RayosConcentradosLib.DibujarHaz — la fórmula del shader
    ///     resuelta por vértice), daño por SEGMENTOS de línea (el
    ///     cono del jefe hecho CheckAABBvLineCollision por tramos con
    ///     el grosor que respira), sacudida de cámara cada 4 t y las
    ///     chispas de SoftGlow titilando a lo largo.
    ///   · FASE 2 — LA DESPEDIDA (12 t): el haz se apaga suave.
    /// </summary>
    public class RayosConcentradosHeld : ModProjectile
    {
        // v6.54.0 — el placeholder de la casa (el cuerpo ES la librería).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int CargaTicks = 45;
        private const int HazTicks = 150;
        private const int ApagadoTicks = 12;
        private const float LargoHaz = 2000f;
        private const float AnchoMax = 90f;

        // === EL ESTADO (localAI: 0 reloj propio · 1 activación suave) ===
        private float Reloj { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }
        private float Activacion { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; }

        private static Texture2D _glowRay;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = CargaTicks + HazTicks + ApagadoTicks + 40;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            // PEGADO A LA MANO, APUNTANDO AL CURSOR (el patrón del held).
            Projectile.Center = duenio.MountedCenter;
            Projectile.velocity = Vector2.Zero;
            Vector2 mira = Main.MouseWorld - duenio.MountedCenter;
            if (mira != Vector2.Zero)
                Projectile.rotation = mira.ToRotation();
            Reloj += 1f;

            int fase = FaseActual();

            if (fase == 0)
            {
                // === LA CARGA: el mundo contiene la respiración ===
                if (Reloj % 15f == 0f && Main.netMode != NetmodeID.Server)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item92.WithPitchOffset(-0.4f),
                        Projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                    Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.8f);
            }
            else if (fase == 1)
            {
                // === EL HAZ: nace en 12 t y vive ===
                float objetivo = (Reloj - CargaTicks) / 12f;
                Activacion = MathHelper.Clamp(Math.Max(Activacion, objetivo), 0f, 1f);

                // LA SACUDIDA (el jefe sacude cada 4 t mientras dispara —
                // aquí: el latido de luz que late con el haz, sin shake
                // nativo portado: la casa no usa camera punch).

                // LA LUZ del haz (violeta vivo — el color de la casa).
                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 mitad = Projectile.Center +
                        new Vector2(MathF.Cos(Projectile.rotation), MathF.Sin(Projectile.rotation))
                            * (LargoHaz * 0.35f);
                    Lighting.AddLight(mitad, 0.7f, 0.3f, 1.1f);
                }

                // LAS CHISPAS (los brillos de los soles rúnicos — la letra):
                // SoftGlow como partículas de luz a lo largo del canal,
                // más densas hacia la PUNTA (donde viven los bits del jefe).
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
                {
                    Vector2 dir = new(MathF.Cos(Projectile.rotation), MathF.Sin(Projectile.rotation));
                    float p = Main.rand.NextFloat(0.25f, 1f);
                    Vector2 pos = Projectile.Center + dir * (LargoHaz * p);
                    Color chispa = ColorGoozma.ALoLargo(Reloj * 0.12f, p, 100f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.RainbowRod,
                        -dir * Main.rand.NextFloat(0.5f, 2f) +
                        Main.rand.NextVector2Circular(1.5f, 1.5f),
                        200, chispa, 0.65f);
                    d.noGravity = true;
                }

                // === EL DAÑO POR SEGMENTOS (el cono del jefe hecho
                //     tramos de línea con el GROSOR QUE RESPIRA) ===
                if (Main.netMode != NetmodeID.MultiplayerClient && Activacion > 0.25f)
                    DanarPorSegmentos(duenio);
            }
            else
            {
                // === LA DESPEDIDA: el haz exhala ===
                float t = (Reloj - CargaTicks - HazTicks) / (float)ApagadoTicks;
                Activacion = MathHelper.Clamp(1f - t, 0f, 1f);
                if (Activacion <= 0.02f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (Reloj > CargaTicks + HazTicks + ApagadoTicks)
                Projectile.Kill();
        }

        private int FaseActual()
        {
            if (Reloj < CargaTicks) return 0;
            if (Reloj < CargaTicks + HazTicks) return 1;
            return 2;
        }

        /// <summary>El cono del jefe: 12 tramos de línea con el ancho
        /// interpolado del haz — la hitbox QUE RESPIRA con el visual.</summary>
        private void DanarPorSegmentos(Player duenio)
        {
            Vector2 dir = new(MathF.Cos(Projectile.rotation), MathF.Sin(Projectile.rotation));
            const int tramos = 12;
            float largoEfectivo = LargoHaz * (0.5f + Activacion * 0.5f);
            for (int i = 0; i < tramos; i++)
            {
                float p0 = i / (float)tramos, p1 = (i + 1) / (float)tramos;
                float pm = (p0 + p1) * 0.5f;
                float ancho = AnchoMax * MathF.Pow(pm, 0.8f)
                    * MathF.Pow(MathHelper.Lerp(1.01f, 0.7f, pm), 1f / 3f) * Activacion;
                Vector2 a = Projectile.Center + dir * (largoEfectivo * p0);
                Vector2 b = Projectile.Center + dir * (largoEfectivo * p1);
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc == null || !npc.active || npc.friendly || npc.dontTakeDamage ||
                        Projectile.localNPCImmunity[n] > 0)
                        continue;
                    float puntoColision = 0f;
                    if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size,
                            a, b, Math.Max(10f, ancho * 1.6f), ref puntoColision))
                        continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        Projectile.damage, Projectile.knockBack, unico: false);
                }
            }
        }

        // ==================================================================
        //  EL DIBUJO (la carga con los brillos de los soles + el haz de la lib)
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;

            try
            {
                if (_glowRay == null || _glowRay.IsDisposed)
                    _glowRay = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowRay",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                Vector2 boca = Projectile.Center - Main.screenPosition;
                int fase = FaseActual();

                if (fase == 0)
                {
                    // === LA CARGA (el jefe: 4 resplandores concéntricos de
                    //     los SOLES RÚNICOS + la aguja telegráfica) ===
                    float carga = Reloj / CargaTicks;
                    Color color = ColorGoozma.ValueAt(Reloj * 0.12f);
                    for (int i = 0; i < 4; i++)
                    {
                        float esc = (0.35f + carga * 1.3f) * (1f - i * 0.2f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, boca, null,
                            color * (0.35f + 0.4f * carga) * 0.9f,
                            Projectile.rotation, VFXCore.SoftGlow.Size() * 0.5f,
                            esc, SpriteEffects.None, 0f);
                    }
                    // LA AGUJA (el GlowRay de la casa estirado — el
                    // telegraph del Glow[2] del jefe).
                    Main.spriteBatch.Draw(_glowRay, boca, null,
                        new Color(255, 240, 210) * (0.5f + 0.5f * carga),
                        Projectile.rotation,
                        new Vector2(0f, _glowRay.Height * 0.5f),
                        new Vector2(0.6f + carga * 1.2f, 0.12f + carga * 0.1f),
                        SpriteEffects.None, 0f);
                }
                else
                {
                    // === EL HAZ (la librería: la fórmula del shader del
                    //     jefe por vértice con las 4 texturas propias) ===
                    float activacion = fase == 1
                        ? MathHelper.Clamp(Math.Max(Activacion, (Reloj - CargaTicks) / 12f), 0f, 1f)
                        : Activacion;
                    RayosConcentradosLib.DibujarHaz(boca, Projectile.rotation,
                        LargoHaz * (0.5f + activacion * 0.5f), AnchoMax,
                        Reloj * 0.025f, activacion);

                    // LA BOCA ARDIENDO (los soles rúnicos — la letra).
                    Color color = ColorGoozma.ValueAt(Reloj * 0.12f);
                    for (int i = 0; i < 3; i++)
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, boca, null,
                            color * ((0.5f - i * 0.13f) * activacion),
                            0f, VFXCore.SoftGlow.Size() * 0.5f,
                            (1.4f - i * 0.35f) * activacion, SpriteEffects.None, 0f);
                }
            }
            catch { }

            return false;   // el cuerpo ES el haz (cero sprite propio)
        }
    }
}
