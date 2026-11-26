using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  RayoProyectado — v6.177 — LA PROYECCIÓN DE VERDAD (la tecla X).
    //
    //  LA LETRA (el plano del meme, recreado por la casa): «Presiona X
    //  para proyectarte de verdad: te quitas el visor, se te encienden
    //  los ojos y sueltas un rayo ENORME que ATRAVIESA TODO, con 20
    //  segundos de enfriamiento. No gasta energía».
    //
    //  LA LÍNEA DE TIEMPO (60 t = 1 s — el plano exacto):
    //     0-213  LA CARGA — el visor SE LEVANTA de la cara, el ojo de
    //            abajo SE ENCIENDE, las motas convergen al ojo y el
    //            mundo tiembla cada vez más fuerte.
    //       213  EL GRITO — «¡AAAAH ME PROYECTO!» + el disparo: sacudida
    //            de cámara ×7 y estallido de 40 motas.
    //   213-333  EL RAYO — el haz ENORME (100 de ancho, 2600 de largo):
    //            crece 180/t, ATRAVIESA todo (penetración infinita,
    //            inmune local 8 t), empuja al portador hacia atrás y
    //            arde la pantalla de rojo.
    //   333-361  EL REGRESO — el visor baja y el ojo se apaga. 20 s de
    //            enfriamiento (el buff los cuenta).
    // ======================================================================
    public class RayoProyectado : ModProjectile
    {
        public const int TiempoCarga = 213;
        public const int TiempoDisparo = 120;
        public const int TiempoCierre = 333;
        public const int Duracion = 361;
        public const float LargoMax = 2600f;
        public const float AnchoBase = 100f;

        public static readonly Color Exterior = new(255, 20, 10);
        public static readonly Color Medio = new(255, 75, 40);
        public static readonly Color Nucleo = new(255, 240, 225);

        private float Largo
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private ref float Tiempo => ref Projectile.localAI[0];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2900;
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
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = Duracion + 30;
        }

        public override bool ShouldUpdatePosition() => false;

        // === LA CURVA DE VIDA DEL RAYO ===
        private float Intensidad()
        {
            if (Tiempo < TiempoCarga) return 0f;
            float t = Tiempo - TiempoCarga;
            return Utils.GetLerpValue(0f, 6f, t, true) * Utils.GetLerpValue(138f, 120f, t, true);
        }

        private float Destello()
        {
            if (Tiempo < TiempoCarga)
                return 1.5f * (float)Math.Pow(Utils.GetLerpValue(117f, TiempoCarga, Tiempo, true), 2);
            return (0.4f + 0.8f * Intensidad()) * Utils.GetLerpValue(351f, 333f, Tiempo, true);
        }

        private float Retiro()
        {
            // EL VISOR SE LEVANTA (0→1 en la carga) y baja al final (1→0)
            if (Tiempo < TiempoCierre)
                return Suave(Utils.GetLerpValue(0f, 34f, Tiempo, true));
            return 1f - Suave(Utils.GetLerpValue(TiempoCierre, TiempoCierre + 28f, Tiempo, true));
        }

        private float BrilloOjos()
        {
            if (Tiempo < TiempoCarga)
            {
                float prog = Tiempo / TiempoCarga;
                return Utils.GetLerpValue(10f, 34f, Tiempo, true) *
                       (0.5f + 0.5f * prog * prog) * (1f + 0.12f * MathF.Sin(Tiempo * 0.45f) * (1f - prog));
            }
            if (Tiempo < TiempoCierre) return 1f;
            return 1f - Utils.GetLerpValue(TiempoCierre, TiempoCierre + 16.8f, Tiempo, true);
        }

        private static float Suave(float t) => t * t * (3f - 2f * t);

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead) { Projectile.Kill(); return; }
            Tiempo += 1f;
            if (Tiempo >= Duracion) { Projectile.Kill(); return; }

            Projectile.Center = RayoOpticoProyectado.PosicionOjos(dueno);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * dueno.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 quiero = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 nuevo = Vector2.Lerp(Projectile.velocity, quiero, 0.07f).SafeNormalize(quiero);
                if ((nuevo - Projectile.velocity).LengthSquared() > 0.0004f)
                    Projectile.netUpdate = true;
                Projectile.velocity = nuevo;
                // EL EMPUJE: el rayo enorme empuja al portador hacia atrás
                if (Tiempo >= TiempoCarga && Tiempo < TiempoCierre && Math.Abs(dueno.velocity.X) < 3f)
                    dueno.velocity.X -= Projectile.velocity.X * 0.12f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.timeLeft = 2;
            dueno.heldProj = Projectile.whoAmI;
            dueno.SetDummyItemTime(2);

            // LA ANIMACIÓN DEL PORTADOR: el visor se levanta y el ojo se enciende
            dueno.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>()
                .MarcarAnimacion(Retiro(), BrilloOjos());

            // LA LUZ del ojo encendido
            if (BrilloOjos() > 0.01f)
                Lighting.AddLight(Projectile.Center, 0.9f * BrilloOjos(), 0.12f * BrilloOjos(), 0.06f * BrilloOjos());

            // === LA CARGA: el grito del visor ===
            if (Tiempo == 1f)
            {
                if (dueno.whoAmI == Main.myPlayer)
                    CombatText.NewText(dueno.Hitbox, new Color(255, 60, 40),
                        Language.GetTextValue("Mods.AethonMod.Proyectado.Grito"), true);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.8f), Projectile.Center);
            }

            // === EL DISPARO: EL GRITO DE VERDAD ===
            if (Tiempo == TiempoCarga)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie104.WithVolumeScale(0.9f), Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74.WithPitchOffset(-0.4f), Projectile.Center);
                if (!Main.dedServ)
                {
                    Main.instance.CameraModifiers.Add(new PunchCameraModifier(dueno.Center,
                        Main.rand.NextVector2Unit(0f, MathF.PI * 2f), 7f, 9f, 120, 2000f, "Proyectado"));
                    for (int i = 0; i < 40; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, i % 3 == 0 ? 90 : 60,
                            Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 10f),
                            0, default, 2f);
                        d.noGravity = true;
                    }
                }
            }

            // === EL RAYO ENORME ===
            if (Tiempo >= TiempoCarga && Tiempo < 351f)
            {
                Largo = Math.Min(LargoMax, Largo + 180f);
                DelegateMethods.v3_1 = new Vector3(1.4f, 0.2f, 0.12f) * Intensidad();
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Largo, 250f,
                    DelegateMethods.CastLight);
                // EL TRUENO del rayo (cada 20 t mientras vive)
                if (Tiempo % 20f == 0f && Intensidad() > 0.5f)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15.WithPitchOffset(-0.9f).WithVolumeScale(0.8f),
                        Projectile.Center);
                // LAS MOTAS a lo largo del haz
                if (!Main.dedServ)
                    for (int i = 0; i < 5; i++)
                    {
                        float f = Main.rand.NextFloat(Largo);
                        Vector2 pos = Projectile.Center + Projectile.velocity * f +
                            new Vector2(-Projectile.velocity.Y, Projectile.velocity.X) *
                            Main.rand.NextFloat(-1f, 1f) * 70f * Intensidad();
                        Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(4) ? 90 : 60,
                            Projectile.velocity * Main.rand.NextFloat(4f, 10f) +
                            new Vector2(-Projectile.velocity.Y, Projectile.velocity.X) * Main.rand.NextFloat(-2f, 2f),
                            0, default, 1.8f);
                        d.noGravity = true;
                    }
            }
            else if (Tiempo == 351f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28.WithPitchOffset(-0.5f).WithVolumeScale(0.8f),
                    Projectile.Center);
            }

            // LA CARGA: MOTAS QUE CONVERGEN AL OJO (cada vez más rápido)
            if (Tiempo < TiempoCarga && !Main.dedServ)
            {
                float prog = Tiempo / (float)TiempoCarga;
                int n = 1 + (int)(4f * prog);
                for (int i = 0; i < n; i++)
                {
                    Vector2 alrededor = Projectile.Center + Main.rand.NextVector2CircularEdge(70f, 70f);
                    Dust d = Dust.NewDustPerfect(alrededor, 60,
                        (Projectile.Center - alrededor) * 0.08f, 0, default, 1.7f);
                    d.noGravity = true;
                }
                if (Tiempo % 20f == 0f)
                    Main.instance.CameraModifiers.Add(new PunchCameraModifier(dueno.Center,
                        Main.rand.NextVector2Unit(0f, MathF.PI * 2f), 0.5f + 3.5f * prog, 12f, 20, 1500f,
                        "ProyectadoCarga"));
            }
        }

        public override bool? CanDamage()
            => Tiempo >= TiempoCarga && Tiempo < TiempoCierre ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, Projectile.Center + Projectile.velocity * Largo,
                150f * Math.Max(Intensidad(), 0.3f), ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA CARNADA del meme: la quemadura de la proyección (Daybreak? no —
            // el plano pone 323 (BetsysCurse)... la casa usa el fuego naranja)
            target.AddBuff(BuffID.OnFire3, 240);
            if (Main.dedServ) return;
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, 60,
                    Main.rand.NextVector2Circular(7f, 7f), 0, default, 1.8f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            if (Tiempo >= TiempoCarga && Tiempo < 351f && Intensidad() > 0.01f)
            {
                // EL HAZ ENORME: la misma polilínea de la casa, ancho 100,
                // ondulación y turbulencia de cine (la X de la proyección)
                Vector2[] pts = RayoOpticoProyectado.PolylineDelHaz(
                    Projectile.Center - Main.screenPosition, Projectile.velocity, Largo,
                    Main.GlobalTimeWrappedHourly * 16f, Projectile.whoAmI, amplitud: 9f, largoOnda: 130f);
                RayoOpticoProyectado.DibujarHaz(pts, AnchoBase, Intensidad(),
                    Exterior, Medio, Nucleo, Destello() * 0.5f, impacto: false);
            }
            // EL OJO QUE CARGA: el destello del ojo encendido (la carga)
            if (Tiempo < TiempoCarga + 20f && BrilloOjos() > 0.3f)
            {
                VFXCore.AbrirAdditive();
                try
                {
                    Vector2 ojo = Projectile.Center - Main.screenPosition;
                    float brillo = BrilloOjos() * Destello();
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, ojo, null,
                        new Color(255, 40, 20) * (0.55f * brillo), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(2.4f * brillo, 2.4f * brillo), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, ojo, null,
                        new Color(255, 220, 200) * (0.5f * brillo), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(1.0f * brillo, 1.0f * brillo), SpriteEffects.None, 0f);
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            return false;
        }
    }
}
