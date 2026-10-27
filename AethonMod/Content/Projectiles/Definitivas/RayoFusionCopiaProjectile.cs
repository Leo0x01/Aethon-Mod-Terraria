// ============================================================================
// v6.54.8 — EL ARMA RÉPLICA: «EL RAYO DE FUSIÓN», LA COPIA EXACTA.
// ============================================================================
// LA LETRA: «intenta crear un arma que sea la copia exacta de el rayo de
// fusión usando sus texturas para fines de investigacion» (la letra del
// dueño, sobre el ataque del dios antiguo).
//
// Es la adaptación del ataque estrella del jefe (puerto de investigación,
// Calamity: Hunt of the Old God · Team Hypnos · GPL-3.0), con SUS
// texturas y SU shader originales — pedida por el dueño del proyecto
// como PATRÓN DE CALIDAD para comparar, lado a lado, con las armas de
// la casa. MATERIAL DE INVESTIGACIÓN: se retira antes de cualquier
// publicación pública (la regla de la casa desde la .53.1).
//
// QUÉ SE COPIÓ EXACTO (del original, verbatim donde se pudo):
//   · La fórmula de ancho del strip: p^0.8 · cbrt(lerp(1.01, 0.7, p)) ·
//     3500 · (1.1 + cos(Time)·(0.08 − p·0.06)) — la respiración ±8 %.
//   · EL BARRIDO: cos((T−Carga)/Duración·2π) · 2.5 rad con sus dos
//     envolventes de entrada y salida.
//   · El giro hacia el objetivo con paso decreciente (0.04 → 0.01).
//   · LA BOCA: los cinco glows apilados (5× · 4× · 2× · 14× · el
//     telegraph del rayo que apunta) con el color que viaja.
//   · EL SHADER COMPLETO: FusionRayEffect.fx compilado al vuelo por
//     FxcReader (verificado: Terraria.ModLoader.Assets.FxcReader existe
//     en tML 2026.08 — así se vio en la .53.0): las CUATRO texturas
//     multiplicadas (carne · segundo ruido · rampa · bits en AND doble),
//     el scroll doble t·3/t·4, el warp sinusoidal ±0.05, el núcleo
//     blanco por SATURACIÓN (length > 0.4 → return 1).
//   · El gradiente arcoíris VIAJERO por vértice: ValueAt(reloj − p·100).
//   · Los tiempos: 300 de carga + 800 de láser (EXACTOS — por eso el
//     arma tarda: es la réplica, no un arma de uso diario).
//   · El Kick de cámara cada 4 t, la música −30 % mientras vive, el
//     sonido en loop con pitch animado, el vuelo infinito y el
//     retroceso suave del portador.
//   · Los polvos cromáticos que recorren el haz (15/frame a lo largo
//     + los de la boca) — reimplementados locales, mismo tamaño y color.
//
// EL CAMBIO DE DUEÑO: el original vivía en el NPC (su boca a 13 px del
// centro, −20 arriba); aquí vive en el JUGADOR y apunta al CURSOR —
// el resto es el ataque tal cual era.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    public class RayoFusionCopiaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRay";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.manualDirectionChange = true;
            Projectile.netImportant = true;
        }

        public ref float Time => ref Projectile.ai[0];
        public ref float Modo => ref Projectile.ai[1];
        public ref float Owner => ref Projectile.ai[2];

        // LOS TIEMPOS EXACTOS DEL ORIGINAL.
        private static readonly int ChargeTime = 300;
        private static readonly int LaserDuration = 800;
        private const float LargoCono = 3500f;

        public float targetRotation;
        public float targetSize;
        public float realSize;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.direction == 1);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.direction = reader.ReadBoolean() ? 1 : -1;
            base.ReceiveExtraAI(reader);
        }

        // ================================================================
        //  EL CORAZÓN — la IA del ataque, adaptada al dueño jugador.
        // ================================================================
        public override void AI()
        {
            // EL DUEÑO: el jugador que la disparó (la boca del original
            // era la del jefe; esta es la del portador).
            Player dueno = Main.player[(int)Owner];
            if (Owner < 0 || dueno == null || !dueno.active || dueno.dead)
            {
                Projectile.Kill();
                return;
            }

            // EL RUMBO del portador (el original orientaba a su jefe con el
            // signo de la rotación ANTES de colocar la boca).
            dueno.direction = Math.Sign(Projectile.rotation.ToRotationVector2().X);
            dueno.itemLocation = dueno.MountedCenter;

            // LA BOCA: el offset del original (13 px del centro, 20 arriba).
            Projectile.Center = dueno.MountedCenter + new Vector2(13 * dueno.direction, -20f);

            // EL RETROCESO SUAVE del portador (el original empujaba a su
            // jefe hacia atrás mientras disparaba).
            dueno.velocity += Projectile.rotation.ToRotationVector2() * -0.1f;
            dueno.velocity *= 0.98f;
            // (la gravedad del juego pelea de vuelta — el efecto neto es
            // el arrastre del original, no un empujón).

            if (!Main.dedServ && Time % 4 == 0 && Time > ChargeTime && Time < ChargeTime + LaserDuration)
            {
                float shakeStrength = Utils.GetLerpValue(ChargeTime - LaserDuration * 0.5f, ChargeTime + LaserDuration, Time, true);
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2CircularEdge(1, 1), shakeStrength * 8f, 12, 20, 5000));
            }

            // EL BARRIDO — la fórmula EXACTA: un coseno completo de ida y
            // vuelta (±2.5 rad) acotado por sus dos envolventes.
            float totalOffRot = (float)Math.Cos((Time - ChargeTime) / LaserDuration * MathHelper.TwoPi) * 2.5f
                * Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + LaserDuration * 0.1f, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration, ChargeTime + LaserDuration * 0.9f, Time, true);

            if (Time < 3 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.direction = Main.rand.NextBool() ? -1 : 1;
                Projectile.netUpdate = true;
            }

            // EL GIRO HACIA EL CURSOR — el paso decreciente del original
            // (0.04 al principio, 0.01 cuando el barrido manda).
            targetRotation = targetRotation.AngleLerp(Projectile.AngleTo(Main.MouseWorld),
                Utils.GetLerpValue(ChargeTime + LaserDuration * 0.1f, ChargeTime, Time, true) * 0.04f
                + Utils.GetLerpValue(ChargeTime + LaserDuration * 0.3f, ChargeTime + LaserDuration * 0.6f, Time, true) * 0.01f);

            // EL TAMAÑO — la envolvente cúbica EXACTA (nace en 20 t, muere
            // al final; realSize la persigue a 0.1).
            targetSize = (float)Math.Pow(
                Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 60, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);

            float waveChange = MathHelper.SmoothStep(0f, 1f,
                Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + 20, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration * 0.9f, ChargeTime + LaserDuration * 0.5f, Time, true));
            Projectile.rotation = targetRotation + totalOffRot * Projectile.direction * waveChange;

            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2();

            // EL RELOJ DEL COLOR (el localAI[0] del jefe: el gradiente
            // camina con el mundo).
            Projectile.localAI[0] += 1f;

            // ============ LOS POLVOS CROMÁTICOS (los del original) ============
            // Los de la boca: 1/frame con la probabilidad decreciente del
            // original (1/(Time·0.5+15)).
            if (!Main.rand.NextBool((int)(Time * 0.5f + 15)))
            {
                SpawnPolvo(Projectile.Center,
                    Projectile.rotation.ToRotationVector2().RotatedByRandom(1f) * Main.rand.NextFloat(5f, 15f),
                    2f, Projectile.localAI[0]);
            }
            // Los del conducto: 15/frame mientras viva el láser — la firma
            // cromática con el offset de color POR DISTANCIA (p/3500·100 el
            // original en la boca; p/3300·60 en el conducto).
            if (Time > ChargeTime - 15 && Time < ChargeTime + LaserDuration + 60)
            {
                for (int i = 0; i < 15; i++)
                {
                    float grow = Utils.GetLerpValue(ChargeTime - 15, ChargeTime + 40, Time, true)
                        * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration, Time, true);
                    float progress = Main.rand.NextFloat(3300);
                    Vector2 position = Projectile.Center + new Vector2(progress, Main.rand.NextFloat(-80f, 80f) * (progress / 3300f)).RotatedBy(Projectile.rotation);
                    if (Main.rand.NextBool(5))
                    {
                        SpawnPolvo(position,
                            Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(15f, 45f),
                            4f * grow, Projectile.localAI[0] - progress / 3300f * 60f);
                    }
                }
            }

            // EL SIZZLE del final (el chisporroteo del original al apagarse).
            if (Time == ChargeTime + LaserDuration + 20)
                DefinitivasLib.Sonar(SoundID.Item34.WithPitchOffset(-0.4f), Projectile.Center);

            Time++;
            Projectile.timeLeft = 10;

            HandleSound();

            if (Time > ChargeTime + LaserDuration + 100)
            {
                raySound.StopSound();
                Projectile.Kill();
                return;
            }

            // EL VUELO INFINFINITO del original: mientras el rayo vive,
            // las alas del portador no se agotan.
            if (Main.netMode == NetmodeID.Server)
            {
                foreach (Player player in Main.player)
                {
                    if (player != null && player.active && !player.dead)
                        player.wingTime = player.wingTimeMax;
                }
            }
            else if (Main.LocalPlayer != null)
            {
                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;
            }
        }

        // ================================================================
        //  EL SONIDO EN LOOP — la clase del puerto (misma matemática:
        //  volumen por realSize, pitch animado por fase).
        // ================================================================
        internal SonidoEnLoop raySound;
        public float volume;
        public float pitch;
        public void HandleSound()
        {
            volume = 1.3f * Utils.GetLerpValue(0, 0.05f, realSize, true);
            pitch = MathHelper.SmoothStep(0.5f, 1.2f,
                Utils.GetLerpValue(ChargeTime - 45, ChargeTime + 350, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true))
                + Utils.GetLerpValue(ChargeTime, ChargeTime + LaserDuration, Time, true) * 0.2f * realSize - 1f;

            raySound ??= new SonidoEnLoop(
                new Terraria.Audio.SoundStyle("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRayLoop") { MaxInstances = 0 },
                new Terraria.Audio.ProjectileAudioTracker(Projectile).IsActiveAndInGame);
            raySound.PlaySound(() => Projectile.Center, () => volume, () => pitch);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time > ChargeTime + 20 && Time < ChargeTime + LaserDuration + 10)
            {
                float grow = (float)Math.Pow(
                    Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 40, Time, true) *
                    Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);
                // EL CONO del original: 3500·(0.5+grow·0.5) con el
                // semiángulo 0.28·grow — aquí con la colisión de cono
                // propia de la casa.
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center, LargoCono * (0.5f + grow * 0.5f),
                    Projectile.rotation, 0.28f * Math.Max(grow, 0.02f));
            }
            return false;
        }

        // ================================================================
        //  LA MÚSICA −30 % MIENTRAS VIVE (el original la callaba para que
        //  el rayo MANDARA — On_Main.UpdateAudio).
        // ================================================================
        public override void Load()
        {
            On_Main.UpdateAudio += CallarMusica;
        }

        public override void Unload()
        {
            On_Main.UpdateAudio -= CallarMusica;
        }

        private void CallarMusica(On_Main.orig_UpdateAudio orig, Main self)
        {
            orig(self);

            Projectile projectile = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i] != null && Main.projectile[i].active && Main.projectile[i].type == Type)
                {
                    projectile = Main.projectile[i];
                    break;
                }
            }
            if (projectile == null) return;

            for (int i = 0; i < Main.musicFade.Length; i++)
            {
                float grow = Utils.GetLerpValue(ChargeTime - 15, ChargeTime + 40, projectile.ai[0], true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration, projectile.ai[0], true);

                float vol = Main.musicFade[i] * Main.musicVolume * (1f - grow * 0.3f);
                float tempFade = Main.musicFade[i];
                Main.audioSystem.UpdateCommonTrackTowardStopping(i, vol, ref tempFade, Main.musicFade[i] > 0.15f);
                Main.musicFade[i] = tempFade;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            => overPlayers.Add(index);

        // ================================================================
        //  EL DIBUJO — verbatim del original: la boca de glows apilados,
        //  el telegraph, el strip de 1500 puntos y EL SHADER con las
        //  cuatro texturas multiplicadas.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRay_0");      // la carne
            Texture2D textureBits = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRay_1");  // los bits
            Texture2D textureGlow = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRay_2");  // la rampa
            Texture2D textureSecond = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRay_3");// el 2º ruido
            Texture2D glow = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/GlowRadial");          // Glow_0
            Texture2D ray = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/GlowRayo");             // Glow_2 (el telegraph)

            if (texture == null || textureBits == null || textureGlow == null || textureSecond == null || glow == null || ray == null)
                return false;

            Color startColor = ColorPrisma.ValueAt(Projectile.localAI[0]);
            startColor.A = 0;

            float laserCharge = Utils.GetLerpValue(50, ChargeTime, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration + 20, Time, true);
            float laserRayCharge = Utils.GetLerpValue(0, ChargeTime, Projectile.localAI[1], true)
                * Utils.GetLerpValue(ChargeTime + 10, ChargeTime - 30, Time, true);

            // LA BOCA — los cinco glows apilados del original (5× el color
            // al 20 %, 4× al 60 %, 2× el blanco, 14× el color al 10 %).
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, startColor * 0.2f * laserCharge, 0, glow.Size() * 0.5f, 5f * laserCharge, 0, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, startColor * 0.6f * laserCharge, 0, glow.Size() * 0.5f, 4f, 0, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, glow.Frame(), new Color(255, 255, 255, 0) * laserCharge, 0, glow.Size() * 0.5f, 2f * laserCharge, 0, 0);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, glow.Frame(), startColor * 0.1f * laserCharge, 0, glow.Size() * 0.5f, 14f * laserCharge, 0, 0);
            Main.EntitySpriteDraw(ray, Projectile.Center - Main.screenPosition, ray.Frame(), startColor * laserRayCharge * 0.5f, Projectile.rotation, ray.Size() * new Vector2(0.02f, 0.5f), new Vector2(3f, 2f), 0, 0);

            if (Time >= ChargeTime - 20)
            {
                float laserActive = Utils.GetLerpValue(ChargeTime - 30, ChargeTime + 70, Time, true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true);

                Vector2[] positions = new Vector2[1500];
                float[] rotations = new float[1500];
                for (int i = 0; i < 1500; i++)
                {
                    positions[i] = Projectile.Center + new Vector2(LargoCono * (i / 1500f) * (0.5f + laserActive * 0.5f), 0).RotatedBy(Projectile.rotation);
                    rotations[i] = Projectile.rotation;
                }

                VertexStrip strip = new VertexStrip();
                strip.PrepareStripWithProceduralPadding(positions, rotations, StripColor, StripWidth, -Main.screenPosition, true);

                // EL SHADER — el FusionRayEffect.fx original compilado al
                // vuelo por FxcReader (como se vio en la .53.0). Si no
                // cargara, el fallback de la casa (tres fajas texturizadas)
                // dibuja el strip — el arma NUNCA tira el juego.
                Effect lightningEffect = null;
                try
                {
                    lightningEffect = ModContent.Request<Effect>(
                        "AethonMod/Content/Weapons/Definitivas/RayoFusionTex/FusionRayEffect",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                catch { lightningEffect = null; }

                if (lightningEffect != null)
                {
                    lightningEffect.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                    lightningEffect.Parameters["uTexture0"].SetValue(texture);
                    lightningEffect.Parameters["uTexture1"].SetValue(textureSecond);
                    lightningEffect.Parameters["uGlow"].SetValue(textureGlow);
                    lightningEffect.Parameters["uBits"].SetValue(textureBits);
                    lightningEffect.Parameters["uTime"].SetValue(-Projectile.localAI[0] * 0.025f);
                    lightningEffect.Parameters["uFreq"].SetValue(1f);
                    lightningEffect.CurrentTechnique.Passes[0].Apply();
                    strip.DrawTrail();
                    Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                }
                else
                {
                    // EL FALLBACK de la casa: el strip con la pila de la
                    // librería (carne + remolino + núcleo) — el mismo haz
                    // sin el .fx.
                    var pts = new Vector2[64];
                    var semi = new float[64];
                    for (int i = 0; i < 64; i++)
                    {
                        float p = i / 63f;
                        pts[i] = positions[(int)(p * 1499)] - Main.screenPosition;
                        semi[i] = MathF.Max(StripWidth(p) * 0.5f, 1f);
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        Texture2D carne = DefinitivasLib.TexRuido;
                        Texture2D remolino = DefinitivasLib.TexRemolino;
                        if (carne != null)
                            RayoStrip.FajaTexturadaEnLote(pts, semi,
                                DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0]), 0.6f * realSize),
                                carne, 420f, 1.0f, Main.GlobalTimeWrappedHourly, 0f, 1f);
                        if (remolino != null)
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.7f),
                                DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0]), 0.45f * realSize),
                                remolino, 240f, 1.9f, Main.GlobalTimeWrappedHourly, 0f, 1f);
                        if (carne != null)
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.4f),
                                DefinitivasLib.Alfa(Color.White, 0.85f * realSize),
                                carne, 380f, 2.6f, Main.GlobalTimeWrappedHourly, 0.36f, 0.64f);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // LOS POLVOS CROMÁTICOS — el pase de las partículas que
            // recorren el haz (encima del strip, bajo la interfaz).
            DibujarPolvos();

            // v6.54.5 — EL CONTRATO DEL LOTE: salir con el lote vivo.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>EL COLOR DEL STRIP — el gradiente que VIAJA a lo largo
        /// (el ValueAt(reloj − p·100) del original) por el realSize.</summary>
        public Color StripColor(float progress)
        {
            Color color = ColorPrisma.ValueAt(Projectile.localAI[0] - progress * 100f);
            color.A = 0;
            return color * realSize;
        }

        /// <summary>EL ANCHO DEL STRIP — la fórmula EXACTA del original
        /// (p^0.8 · cbrt · 3500 · (1.1 + cos·(0.08 − p·0.06))).</summary>
        public float StripWidth(float progress)
        {
            float start = (float)Math.Pow(progress, 0.8f);
            float cap = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));
            return start * cap * realSize * 3500f * (1.1f + (float)Math.Cos(Time) * (0.08f - progress * 0.06f));
        }

        private static float[] ArraySemis(float[] perfil, float k)
        {
            var r = new float[perfil.Length];
            for (int i = 0; i < perfil.Length; i++) r[i] = perfil[i] * k;
            return r;
        }

        // ================================================================
        //  LOS POLVOS CROMÁTICOS — la reimplementación local de la
        //  partícula del original (misma vida, mismo tamaño, mismo
        //  color con offset; sin el pool del puerto).
        // ================================================================
        private struct PolvoCromatico
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Scale;
            public float Offset;
            public float Life;
        }

        private static readonly List<PolvoCromatico> _polvos = new();

        private static void SpawnPolvo(Vector2 pos, Vector2 vel, float scale, float offset)
        {
            if (Main.dedServ || _polvos.Count > 220) return;
            _polvos.Add(new PolvoCromatico
            {
                Pos = pos,
                Vel = vel,
                Scale = MathHelper.Clamp(scale, 0.2f, 4f),
                Offset = offset,
                Life = 0f
            });
        }

        public override void PostAI()
        {
            ActualizarPolvos();
        }

        private static void ActualizarPolvos()
        {
            if (Main.dedServ) return;
            for (int i = _polvos.Count - 1; i >= 0; i--)
            {
                var p = _polvos[i];
                p.Life += 0.1f;
                p.Scale *= 0.99f;
                p.Vel *= 0.97f;
                p.Vel = Vector2.Lerp(p.Vel, Main.rand.NextVector2Circular(5, 5), 0.02f + p.Life * 0.02f);
                p.Pos += p.Vel;
                if (p.Life > 4f) p.Scale *= 0.95f;
                _polvos[i] = p;
                if (p.Scale < 0.1f) _polvos.RemoveAt(i);
            }
        }

        private static void DibujarPolvos()
        {
            if (_polvos.Count == 0) return;
            Texture2D glow = DefinitivasLib.Tex("AethonMod/Content/Weapons/Definitivas/RayoFusionTex/GlowRadial");
            if (glow == null) return;
            for (int i = 0; i < _polvos.Count; i++)
            {
                var p = _polvos[i];
                Color c = ColorPrisma.ValueAt(p.Offset + p.Life * 0.05f);
                float alfa = MathHelper.Clamp(1f - p.Life / 6f, 0f, 1f);
                Main.EntitySpriteDraw(glow, p.Pos - Main.screenPosition, null,
                    DefinitivasLib.Alfa(c, 0.55f * alfa), 0, glow.Size() * 0.5f, p.Scale, 0, 0);
                Main.EntitySpriteDraw(glow, p.Pos - Main.screenPosition, null,
                    DefinitivasLib.Alfa(Color.White, 0.35f * alfa), 0, glow.Size() * 0.5f, p.Scale * 0.5f, 0, 0);
            }
        }

        public override void OnKill(int timeLeft)
        {
            _polvos.Clear();
        }
    }

    // ====================================================================
    //  EL SONIDO EN LOOP — adaptación de la utilidad del puerto (el
    //  LoopingSound original): un slot que se actualiza cada frame con
    //  posición/volumen/pitch, y muere solo cuando el proyectil muere.
    // ====================================================================
    internal sealed class SonidoEnLoop
        {
            private readonly Terraria.Audio.SoundStyle _estilo;
            private ReLogic.Utilities.SlotId _slot;
            private readonly Func<bool> _condicion;

            public SonidoEnLoop(Terraria.Audio.SoundStyle estilo, Func<bool> condicionViva)
            {
                _estilo = estilo with { IsLooped = true };
                _condicion = condicionViva;
                _slot = ReLogic.Utilities.SlotId.Invalid;
            }

        public void PlaySound(Func<Vector2> posicion, Func<float> volumen, Func<float> pitch)
        {
            bool activo = Terraria.Audio.SoundEngine.TryGetActiveSound(_slot, out Terraria.Audio.ActiveSound sonidoActivo);
            if ((!activo || !_slot.IsValid) && _condicion.Invoke())
            {
                _slot = Terraria.Audio.SoundEngine.PlaySound(_estilo, posicion.Invoke(), sonido =>
                {
                    if (sonido != null)
                    {
                        sonido.Position = posicion.Invoke();
                        sonido.Volume = volumen.Invoke();
                        sonido.Pitch = pitch.Invoke();
                    }
                    return _condicion.Invoke();
                });
            }
            if (activo && sonidoActivo != null)
            {
                sonidoActivo.Position = posicion.Invoke();
                sonidoActivo.Volume = volumen.Invoke();
                sonidoActivo.Pitch = pitch.Invoke();
            }
        }

        public void StopSound()
        {
            if (Terraria.Audio.SoundEngine.TryGetActiveSound(_slot, out Terraria.Audio.ActiveSound sonidoActivo) && sonidoActivo != null)
                sonidoActivo.Stop();
            _slot = ReLogic.Utilities.SlotId.Invalid;
        }
    }
}
