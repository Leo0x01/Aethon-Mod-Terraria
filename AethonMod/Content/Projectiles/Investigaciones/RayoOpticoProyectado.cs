using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  RayoOpticoProyectado — v6.177 — EL RAYO CONTINUO DEL VISOR.
    //
    //  LA LETRA (el plano del .tmod del Cíclope Proyectado, recreado 100%
    //  por la casa): «mantén clic para soltar un rayo óptico continuo…
    //  el rayo sigue a tu cursor. No gasta maná: gasta tu energía
    //  proyectada». Nace del OJO (la posición de los ojos del portador),
    //  sigue al cursor con giro suave (lerp 0.35), mide hasta 900 px con
    //  el escáner de láser de vanilla, crece 70 px/t y golpea con inmunidad
    //  local (10 t) mientras el canal viva.
    //
    //  EL DIBUJO es LA PILA DE CAPAS de la casa (la ley v6.56.9): LA
    //  MISMA POLILÍNEA dibujada CUATRO veces (bruma → glow → cuerpo →
    //  núcleo) con RayoStrip.Filamento — cada borde se funde en curva,
    //  cero picos por construcción. Los colores del meme: el rojo de la
    //  proyección (210,10,10 · 255,55,40 · 255,215,200).
    // ======================================================================
    public class RayoOpticoProyectado : ModProjectile
    {
        public const float LargoMax = 900f;
        public const float AnchoBase = 18f;
        private const float Crecimiento = 70f;

        // LA PALETA DEL MEME (el rojo de la proyección)
        public static readonly Color Exterior = new(210, 10, 10);
        public static readonly Color Medio = new(255, 55, 40);
        public static readonly Color Nucleo = new(255, 215, 200);

        private bool _impacto;

        private float Largo
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private ref float Tiempo => ref Projectile.localAI[0];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>LA POSICIÓN DEL OJO del portador (el visor vive ahí).</summary>
        public static Vector2 PosicionOjos(Player p)
        {
            Vector2 ojo = new(p.direction * 6f, -9f * p.gravDir);
            return p.RotatedRelativePoint(p.MountedCenter + ojo, false, true);
        }

        private static Vector2 HaciaMouse(Player p, Vector2 actual)
        {
            Vector2 hacia = Main.MouseWorld - PosicionOjos(p);
            return hacia.LengthSquared() > 256f ? Vector2.Normalize(hacia) : actual;
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead) { Projectile.Kill(); return; }

            // EL CANAL: mientras el clic viva y el visor siga en la mano
            if (Projectile.owner == Main.myPlayer && (!dueno.channel || dueno.noItems ||
                dueno.CCed || dueno.HeldItem.type != ModContent.ItemType
                    <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>()))
            { Projectile.Kill(); return; }

            Tiempo += 1f;
            Projectile.Center = PosicionOjos(dueno);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * dueno.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                // EL GIRO SUAVE hacia el cursor (el plano: lerp 0.35)
                Vector2 quiero = HaciaMouse(dueno, Projectile.velocity);
                Vector2 nuevo = Vector2.Lerp(Projectile.velocity, quiero, 0.35f).SafeNormalize(quiero);
                if ((nuevo - Projectile.velocity).LengthSquared() > 0.0004f)
                    Projectile.netUpdate = true;
                Projectile.velocity = nuevo;
                // LA ENERGÍA: el rayo se paga con la barra propia
                if (!dueno.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>().GastarEnergia())
                { Projectile.Kill(); return; }
                if (Tiempo % 30f == 0f)
                    Projectile.damage = dueno.GetWeaponDamage(dueno.HeldItem);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.direction = Projectile.velocity.X > 0.2f ? 1 : (Projectile.velocity.X < -0.2f ? -1 : Projectile.direction);
            Projectile.timeLeft = 2;
            dueno.ChangeDir(Projectile.direction);
            dueno.heldProj = Projectile.whoAmI;
            dueno.SetDummyItemTime(2);

            // EL ESCÁNER de vanilla: ¿a qué distancia está el muro?
            float[] pasos = new float[3];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, 9f, LargoMax, pasos);
            float hasta = 0f;
            for (int i = 0; i < pasos.Length; i++) hasta += pasos[i];
            hasta /= pasos.Length;
            _impacto = hasta < LargoMax - 4f;
            Largo = Largo < hasta ? Math.Min(hasta, Largo + Crecimiento) : hasta;

            // LA LUZ del rayo (el pase de vanilla: CastTileLight por la línea)
            DelegateMethods.v3_1 = new Vector3(1f, 0.15f, 0.1f) * Encendido();
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Largo, 36f,
                DelegateMethods.CastLight);

            if (!Main.dedServ && Tiempo == 1f)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15.WithPitchOffset(-0.2f).WithVolumeScale(0.55f),
                    Projectile.Center);

            // EL POLVO del rayo (determinista: la semilla de la casa, no Main.rand)
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float d = Largo * Hash01(Projectile.whoAmI, (int)Tiempo, 77);
                Dust d1 = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * d +
                    new Vector2(Hash01((int)Tiempo, 1, 78) - 0.5f, Hash01((int)Tiempo, 2, 79) - 0.5f) * 9f,
                    60, Projectile.velocity * (2f + 3f * Hash01((int)Tiempo, 3, 80)), 0, default, 1.3f);
                d1.noGravity = true;
            }
        }

        private static float Hash01(int a, int b, int c) => VFXCore.Hash01(a, b, c);

        private float Encendido() => Utils.GetLerpValue(0f, 10f, Tiempo, true);

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28.WithPitchOffset(-0.3f).WithVolumeScale(0.5f),
                    Projectile.Center);
        }

        public override bool? CanDamage() => Projectile.timeLeft > 0;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, Projectile.Center + Projectile.velocity * Largo, 27f * Encendido(), ref _);
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Largo, 18f,
                DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            Vector2[] pts = PolylineDelHaz(Projectile.Center - Main.screenPosition,
                Projectile.velocity, Largo, Tiempo, Projectile.whoAmI, amplitud: 0.45f, largoOnda: 42f);
            DibujarHaz(pts, AnchoBase, Encendido() * 0.6f + 0.4f, Exterior, Medio, Nucleo,
                destello: 0.6f + 0.4f * Encendido(), impacto: _impacto);
            return false;
        }

        // ================================================================
        //  EL HAZ DE LA CASA (compartido con el rayo grande de la X):
        //  LA MISMA POLILÍNEA ×4 CAPAS + la boca y el impacto. Todo en el
        //  lote aditivo de la casa (R21/R6: AbrirAdditive y salir con
        //  RestaurarLoteVanilla).
        // ================================================================

        /// <summary>LA POLILÍNEA del haz: recta con vaivén determinista (la
        /// misma línea para TODAS las capas — la ley de la casa).</summary>
        public static Vector2[] PolylineDelHaz(Vector2 inicioPantalla, Vector2 dir, float largo,
            float tiempo, int semilla, float amplitud, float largoOnda)
        {
            dir = dir.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new(-dir.Y, dir.X);
            int n = 14;
            Vector2[] pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float f = i / (float)(n - 1);
                // EL VAIVÉN: la onda viaja por el haz (determinista)
                float onda = MathF.Sin(f * largo / largoOnda - tiempo * 0.35f + semille(semilla)) * amplitud
                            + MathF.Sin(f * largo / (largoOnda * 0.37f) - tiempo * 0.61f + semille(semilla) * 2.3f) * amplitud * 0.4f;
                // el arranque y la punta quedan CLAVADOS (la boca y el fin no bailan)
                float pinza = MathHelper.Clamp(f * 4f, 0f, 1f) * MathHelper.Clamp((1f - f) * 4f, 0f, 1f);
                pts[i] = inicioPantalla + dir * (largo * f) + perp * (onda * pinza);
            }
            return pts;
        }

        private static float semille(int s) => 0.5f + 0.5f * MathF.Sin(s * 2.399963f);

        /// <summary>EL HAZ DE CUATRO CAPAS: bruma → glow → cuerpo → núcleo +
        /// la boca (destello del ojo) y el impacto (el choque contra el muro).</summary>
        public static void DibujarHaz(Vector2[] pts, float ancho, float intensidad,
            Color exterior, Color medio, Color nucleo, float destello, bool impacto)
        {
            if (pts == null || pts.Length < 2 || intensidad <= 0.02f) return;
            VFXCore.AbrirAdditive();
            try
            {
                // (1) LA BRUMA — el aire quemado alrededor del haz
                RayoStrip.FilamentoEnLote(pts, ancho * 3.6f, exterior * 0.55f, exterior * 0.30f,
                    0.30f * intensidad, StormTaper.Center, 0, 0, true, false, false);
                // (2) EL GLOW — el color del meme alrededor del cuerpo
                RayoStrip.FilamentoEnLote(pts, ancho * 1.9f, medio * 0.8f, medio * 0.5f,
                    0.55f * intensidad, StormTaper.Center, 0, 0, true, false, false);
                // (3) EL CUERPO
                RayoStrip.FilamentoEnLote(pts, ancho * 1.0f, medio, medio * 0.75f,
                    0.95f * intensidad, StormTaper.Center, 0, 0, true, false, false);
                // (4) EL NÚCLEO — el blanco-cálido que atraviesa
                RayoStrip.FilamentoEnLote(pts, ancho * 0.38f, nucleo, nucleo * 0.9f,
                    intensidad, StormTaper.Center, 0, 0, true, false, false);

                // LA BOCA: el destello del ojo (deglow doble)
                Vector2 boca = pts[0];
                float latido = 1f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7.3f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, boca, null,
                    medio * (0.5f * destello), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2(ancho * 5.2f * destello * latido / 128f,
                                ancho * 1.3f * destello / 128f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, boca, null,
                    nucleo * (0.6f * destello), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2(ancho * 1.6f * destello / 128f,
                                ancho * 0.45f * destello / 128f), SpriteEffects.None, 0f);

                // EL IMPACTO: el choque al final del haz (cuando el muro para)
                if (impacto)
                {
                    Vector2 fin = pts[pts.Length - 1];
                    float late = 1f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8.1f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, fin, null,
                        medio * (0.75f * intensidad), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(ancho * 2.6f * late / 128f, ancho * 2.6f * late / 128f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, fin, null,
                        nucleo * (0.9f * intensidad), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(ancho * 1.2f / 128f, ancho * 1.2f / 128f), SpriteEffects.None, 0f);
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("RayoProyectado", exRegistro); }
            VFXCore.RestaurarLoteVanilla();
        }
    }
}
