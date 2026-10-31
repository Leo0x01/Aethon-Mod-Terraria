using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // =====================================================================
    //  v6.56.9 — LAS COPIAS DEL RC2: LA PILA DE CAPAS GPU-UNIVERSAL.
    // =====================================================================
    //  LA LETRA del usuario: «lo que le faltan son varias capas de
    //  texturas con mas difuminado, glow entre demas efectos visuales…
    //  copia ese rc2 en varias armas con los cambios diseñados asi no
    //  dañamos el codigo original y cambiamos las copias hasta tener la
    //  correcta».
    //
    //  EL DIAGNÓSTICO (forense sobre la captura del jugador, píxel a
    //  píxel): el RC2 de la v6.56.8 dibuja TODO su universo visual en UN
    //  SOLO strip — cuerpo, venas, núcleo y halo viven PER-PIXEL dentro
    //  del mismo polígono. La medición: el núcleo saturado ocupa el 99 %
    //  del halo medido (núcleo 412 px vs halo 424 px en la boca) — NO
    //  EXISTE envolvente luminosa fuera de la banda. Por eso el ojo lee
    //  «una sola capa»: por más riqueza per-pixel que tenga el shader,
    //  la SILUETA del haz es una banda sola de borde firme. El shader NO
    //  está roto — le falta MUNDO alrededor.
    //
    //  LA CURA — LA PILA DE CAPAS: la MISMA polilínea de 1500 puntos de
    //  la técnica 607df36 se dibuja SEIS veces, de atrás hacia adelante,
    //  cada una con su propio ancho y su propio color:
    //    (1) LA BRUMA — ×2.6..3.6, RampaGlow, violeta profundo al 9-20%:
    //        el resplandor AMBIENTAL que tiñe el fondo (el bloom).
    //    (2) EL GLOW — ×1.75..2.5, RampaGlow, la paleta del conjuro
    //        VIAJANDO al 20-28%: la envolvente difusa del conducto.
    //    (3) EL GLOW² (receta C) — una segunda envolvente interior.
    //    (4) EL CUERPO — el strip del HazConcentradoShader EXACTO del
    //        RC2 (el esqueleto 607df36, INTACTO): el plasma que hierve.
    //    (5) EL NÚCLEO — ×0.28..0.42, RampaGlow, blanco-violeta al
    //        65-95%: la vena ardiente en el corazón del conducto.
    //    (6) LOS SOLES CONDUCTO — los destellos de 4 puntas del
    //        accesorio corriendo por las márgenes del haz hacia la punta.
    //
    //  POR QUÉ NO PUEDE PRODUCIR PICOS (la ley §5.4 del códice): las
    //  capas 1, 2, 3 y 5 se dibujan con el SHADER VANILLA de sprites
    //  (Main.pixelShader) + RampaGlow — el perfil gaussiano de la rampa
    //  (0 en el borde, pico al centro, medido: 24/255 a V=0.25) funde
    //  cada borde en CURVA por píxel. Todas las capas siguen la MISMA
    //  polilínea (mismos arrays de 1500 puntos, solo cambia el factor de
    //  ancho) — no hay esquinas poligonales duras (los picos de la
    //  v6.56.6 nacieron de los QUADS de RayoStrip y el remolino binario
    //  visto crudo, jamás del strip).
    //
    //  GPU-UNIVERSAL (la letra: «necesito que cualquier gpu incluso los
    //  que no son dedicadas deben ver el shader»): las capas 1, 2, 3, 5
    //  y 6 usan el pipeline de sprites VANILLA — el mismo que dibuja
    //  todo Terraria, presente en CUALQUIER GPU (integrada, vieja, lo
    //  que sea). El .fx del cuerpo es un PLUS: si carga, el plasma
    //  hierve; si no carga, el cuerpo cae a una tira de CarnePlasma por
    //  el shader vanilla y LA PILA SE VE IGUAL — seis capas visibles en
    //  cualquier máquina, shader o no shader.
    //
    //  LAS TRES RECETAS (la letra: «cambiamos las copias hasta tener la
    //  correcta»): A = LA EQUILIBRADA (el diseño base), B = EL TITÁN
    //  (bruma y glow al máximo, el haz más gordo), C = EL SUEÑO (el
    //  desenfoque máximo, doble glow, núcleo suave). La receta viaja en
    //  ai[1] (la lección de la .56.6: Projectile.ai solo tiene 3 slots
    //  en este tML).
    //
    //  LO DEMÁS ES EL RC2 (conducta intacta): 300 de carga + 800 de
    //  láser, el cono de 3500, el barrido cosenoidal, el temblor
    //  sostenido, la música al 30 %, el vuelo infinito, los pozos
    //  cero-GC y la implosión de soles de la carga (v6.56.9: los soles
    //  NACEN LEJOS y CAEN RECTOS al centro, sin dar vueltas).
    // =====================================================================
    public class RayosPruebaProjectile : ModProjectile
    {
        // LOS TIEMPOS DEL ATAQUE (idénticos al RC2).
        private static readonly int ChargeTime = 300;
        private static readonly int LaserDuration = 800;
        private const float LargoCono = 3500f;

        public float targetRotation;
        public float targetSize;
        public float realSize;

        // v6.55.1 — LA BANDERA DE FALLO DEL SHADER (el contrato del RC2).
        private static bool _shaderFallo;
        private static Effect _shaderCache;

        internal SonidoEnLoop raySound;
        public float volume;
        public float pitch;

        private ReLogic.Utilities.SlotId _cargaSlot;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // ================================================================
        //  LAS TRES RECETAS — A: LA EQUILIBRADA · B: EL TITÁN · C: EL
        //  SUEÑO. Cada columna es un mundo de look; el usuario dispara
        //  las tres y la casa copia la ganadora al RC2 original.
        // ================================================================
        private static readonly float[] KBruma = { 2.60f, 3.10f, 3.60f };
        private static readonly float[] AlfaBruma = { 0.09f, 0.14f, 0.20f };
        private static readonly float[] KGlow = { 1.75f, 2.10f, 2.50f };
        private static readonly float[] AlfaGlow = { 0.20f, 0.28f, 0.24f };
        private static readonly bool[] DobleGlow = { false, false, true };
        private static readonly float[] KGlow2 = { 1.30f, 1.55f, 1.90f };
        private static readonly float[] AlfaGlow2 = { 0.00f, 0.00f, 0.12f };
        private static readonly float[] KNucleo = { 0.34f, 0.42f, 0.28f };
        private static readonly float[] AlfaNucleo = { 0.80f, 0.95f, 0.65f };
        private static readonly int[] SolesConducto = { 5, 8, 6 };

        // LA PALETA DEL CONJURO (la del RC2, intacta): violeta arcano →
        // morado profundo (el «negro») → rojo sangre → magenta, viajando.
        private static readonly Color[] PaletaConjuro =
        {
            new(150, 62, 235),    // violeta arcano
            new(104, 34, 182),    // morado profundo — el «negro» del conjuro
            new(208, 46, 88),     // rojo sangre
            new(178, 52, 214),    // magenta del conjuro
        };

        private const float CicloConjuro = 48f;

        public static Color ColorConjuro(float reloj)
        {
            float t = reloj % CicloConjuro;
            if (t < 0f) t += CicloConjuro;
            float seg = t / 12f;
            int i = (int)seg % PaletaConjuro.Length;
            int j = (i + 1) % PaletaConjuro.Length;
            return Color.Lerp(PaletaConjuro[i], PaletaConjuro[j], seg - (int)seg);
        }

        public ref float Time => ref Projectile.ai[0];
        public ref float Owner => ref Projectile.ai[2];

        /// <summary>LA RECETA (ai[1]): 0 = A LA EQUILIBRADA · 1 = B EL
        /// TITÁN · 2 = C EL SUEÑO. La elige el arma al disparar.</summary>
        public ref float Receta => ref Projectile.ai[1];

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
            raySound = null;
            _cargaSlot = default;
        }

        // LOS POZOS DEL RC2 (cero-GC): la MISMA polilínea de 1500 puntos
        // alimenta TODAS las capas de la pila.
        private static readonly Vector2[] _cachePuntos = new Vector2[1500];
        private static readonly float[] _cacheRotas = new float[1500];
        private static readonly VertexStrip _strip = new VertexStrip();

        // EL ESTADO DE FRAME que leen los delegados de cada capa.
        private float _laserActivo;
        private float _kCapa;

        private static Effect ObtenerShader()
        {
            if (_shaderFallo)
                return null;
            if (_shaderCache != null && !_shaderCache.IsDisposed)
                return _shaderCache;
            try
            {
                _shaderCache = ModContent.Request<Effect>(
                    "AethonMod/Content/Effects/Shaders/HazConcentradoShader",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                return _shaderCache;
            }
            catch
            {
                _shaderFallo = true;
                return null;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.direction == 1);
            writer.Write(targetRotation);
            writer.Write(Receta);              // la receta viaja al server
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.direction = reader.ReadBoolean() ? 1 : -1;
            targetRotation = reader.ReadSingle();
            Receta = reader.ReadSingle();
        }

        // ================================================================
        //  EL CORAZÓN — la IA del haz (idéntica al RC2).
        // ================================================================
        public override void AI()
        {
            if (Owner < 0 || Owner >= Main.maxPlayers)
            {
                CortarCarga();
                Projectile.Kill();
                return;
            }
            Player dueno = Main.player[(int)Owner];
            if (dueno == null || !dueno.active || dueno.dead)
            {
                CortarCarga();
                Projectile.Kill();
                return;
            }

            dueno.direction = Math.Sign(Projectile.rotation.ToRotationVector2().X);
            dueno.itemLocation = dueno.MountedCenter;

            Projectile.Center = dueno.MountedCenter + new Vector2(13 * dueno.direction, -20f);

            dueno.velocity += Projectile.rotation.ToRotationVector2() * -0.1f;
            dueno.velocity *= 0.98f;

            if (!Main.dedServ)
            {
                if (Time == 2f)
                {
                    SoundStyle carga = new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoCarga", SoundType.Sound)
                    {
                        MaxInstances = 1,
                        Volume = 1f
                    };
                    try
                    {
                        _cargaSlot = SoundEngine.PlaySound(carga, Projectile.Center);
                    }
                    catch { }
                }

                if (Time > ChargeTime && Time < ChargeTime + LaserDuration)
                {
                    Pantalla.Sacudir(6.5f, 0.4f, "RC2P-sostenido");
                }
                if (Time == ChargeTime + 1)
                {
                    // v6.56.9 — LA IGNICIÓN DE CINE (la letra: el momento
                    // del disparo): EL FACADE del golpe de la casa — kick
                    // escalado + flash local + baño de luz + EL HITSTOP de
                    // TiemposLib (el mundo CONTIENE LA RESPIRACIÓN 6 ticks
                    // mientras el conducto se enciende; solo un jugador).
                    ImpactoLib.GolpeDeCine(Projectile.Center, 1.25f, 6,
                        new Color(255, 248, 235));
                }
            }

            float totalOffRot = (float)Math.Cos((Time - ChargeTime) / LaserDuration * MathHelper.TwoPi) * 2.5f
                * Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + LaserDuration * 0.1f, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration, ChargeTime + LaserDuration * 0.9f, Time, true);

            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 15 == 0 && Time < ChargeTime + LaserDuration * 0.6f)
            {
                Projectile.direction = Main.rand.NextBool() ? -1 : 1;
                Projectile.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.Server && Main.myPlayer == Projectile.owner)
            {
                targetRotation = targetRotation.AngleLerp(Projectile.AngleTo(Main.MouseWorld),
                    Utils.GetLerpValue(ChargeTime + LaserDuration * 0.1f, ChargeTime, Time, true) * 0.04f
                    + Utils.GetLerpValue(ChargeTime + LaserDuration * 0.3f, ChargeTime + LaserDuration * 0.6f, Time, true) * 0.01f);
            }

            targetSize = (float)Math.Pow(
                Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 60, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);

            float waveChange = MathHelper.SmoothStep(0f, 1f,
                Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + 20, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration * 0.9f, ChargeTime + LaserDuration * 0.5f, Time, true));
            Projectile.rotation = targetRotation + totalOffRot * Projectile.direction * waveChange;

            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2();

            Projectile.localAI[0] += 1f;

            if (!Main.dedServ && Time > ChargeTime - 10 && Time < ChargeTime + LaserDuration)
            {
                Color cLuz = ColorConjuro(Projectile.localAI[0]);
                float k = realSize;
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Lighting.AddLight(Projectile.Center,
                    cLuz.R / 255f * 1.8f * k, cLuz.G / 255f * 1.7f * k, cLuz.B / 255f * 1.5f * k);
                Lighting.AddLight(Projectile.Center + dir * 1100f,
                    cLuz.R / 255f * 1.4f * k, cLuz.G / 255f * 1.3f * k, cLuz.B / 255f * 1.1f * k);
                Lighting.AddLight(Projectile.Center + dir * 2400f,
                    cLuz.R / 255f * 1.0f * k, cLuz.G / 255f * 0.9f * k, cLuz.B / 255f * 0.8f * k);
            }

            if (Time == ChargeTime + LaserDuration + 20)
            {
                DefinitivasLib.Sonar(SoundID.Item34.WithPitchOffset(-0.4f), Projectile.Center);
                OndaLib.Kick(6f, 10);
            }

            Time++;
            Projectile.timeLeft = 10;

            HandleSound();

            if (Time > ChargeTime + LaserDuration + 50)
            {
                raySound?.StopSound();
                Projectile.Kill();
                return;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                Player p = Main.player[(int)Owner];
                if (p != null && p.active && !p.dead)
                    p.wingTime = p.wingTimeMax;
            }
            else if (Main.LocalPlayer != null && Main.LocalPlayer.whoAmI == (int)Owner)
            {
                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;
            }
        }

        public void HandleSound()
        {
            if (Main.dedServ)
                return;
            volume = 1.3f * Utils.GetLerpValue(0f, 0.05f, realSize, true);
            pitch = MathHelper.Lerp(-0.22f, -0.7f, 1f - Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true));

            raySound ??= new SonidoEnLoop(
                new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop") { MaxInstances = 0 },
                new ProjectileAudioTracker(Projectile).IsActiveAndInGame);
            raySound.PlaySound(() => Projectile.Center, () => volume, () => pitch);
        }

        private void CortarCarga()
        {
            try
            {
                if (_cargaSlot.IsValid && SoundEngine.TryGetActiveSound(_cargaSlot, out ActiveSound sonidoActivo) && sonidoActivo != null)
                    sonidoActivo.Stop();
            }
            catch { }
            _cargaSlot = default;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time > ChargeTime + 20 && Time < ChargeTime + LaserDuration + 10)
            {
                float grow = (float)Math.Pow(
                    Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 40, Time, true) *
                    Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center, LargoCono * (0.5f + grow * 0.5f),
                    Projectile.rotation, 0.28f * Math.Max(grow, 0.02f));
            }
            return false;
        }

        // LA MÚSICA −30 % MIENTRAS VIVE (el contrato del RC2).
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
        //  EL DIBUJO — LA BOCA DEL CONJURO (idéntica al RC2: implosión de
        //  soles + corona rúnica + micro-arcos + estrella conducto) y LA
        //  PILA DE CAPAS en el conducto.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = DefinitivasLib.TexRuido;
            Texture2D textureBits = DefinitivasLib.TexBits;
            Texture2D textureGlow = DefinitivasLib.TexRampa;
            Texture2D textureSecond = DefinitivasLib.TexRemolino;
            Texture2D ring = VFXCore.Ring;

            if (textureGlow == null || ring == null)
                return false;

            float laserCharge = Utils.GetLerpValue(50, ChargeTime, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration + 20, Time, true);

            Vector2 posPantalla = Projectile.Center - Main.screenPosition;

            // ============ LA IMPLOSIÓN DE SOLES DE LA CARGA ============
            // (v6.56.9 — la letra: «aparecer lejos y contraerse hacia el
            // centro pero sin dar vueltas»: cada sol nace LEJOS en su
            // ángulo FIJO y cae RECTO a la boca del arma).
            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                if (Time < ChargeTime)
                {
                    HazConcentradoLib.SolesRunicosCarga(Projectile.Center,
                        Utils.GetLerpValue(20f, ChargeTime, Time, true),
                        Main.GlobalTimeWrappedHourly);
                }
                else if (Time < ChargeTime + 22)
                {
                    HazConcentradoLib.SolesRunicosEstallido(Projectile.Center,
                        Projectile.rotation,
                        (Time - ChargeTime) / 22f,
                        Main.GlobalTimeWrappedHourly);
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();

            // ============ LOS CÍRCULOS RÚNICOS (la corona del RC2) ============
            {
                float numP = MathF.Pow(laserCharge, 0.8f);
                float breathe = 1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f);
                float rCorona = MathHelper.Lerp(70f, 26f, numP) * breathe;
                try
                {
                    OrbitaLib.AbrirAdditive();
                    OrbitaLib.CoronaConjuro(posPantalla, rCorona, Main.GlobalTimeWrappedHourly,
                        alphaMul: 0.28f + 0.62f * laserCharge, glyphMul: 1.8f,
                        blanco: new Color(255, 248, 235),
                        dorado: new Color(255, 128, 104),
                        violeta: new Color(176, 118, 255));
                    OrbitaLib.CerrarBatch();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();

                Vector2 tamAnillo = VFXCore.RingQuadSize(rCorona);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorConjuro(Projectile.localAI[0] + 6f), 0.55f * laserCharge),
                    Main.GlobalTimeWrappedHourly * 0.7f, ring.Size() * 0.5f, tamAnillo, SpriteEffects.None, 0f);
                Vector2 tamAnillo2 = VFXCore.RingQuadSize(rCorona * 1.38f);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorConjuro(Projectile.localAI[0] + 18f), 0.3f * laserCharge),
                    -Main.GlobalTimeWrappedHourly * 0.5f + 2.1f, ring.Size() * 0.5f, tamAnillo2, SpriteEffects.None, 0f);
            }

            // ============ LOS MICRO-ARCOS DE LA CARGA ============
            if (Time < ChargeTime && Time % 14f < 3f)
            {
                float progreso = Utils.GetLerpValue(0f, ChargeTime, Time, true);
                int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) & 0x3FF;
                int cuenta = 1 + (int)(progreso * 3.99f);
                for (int i = 0; i < cuenta; i++)
                {
                    int seed = (int)(Time * 0.017f) * 131 + i * 297;
                    float ang = VFXCore.Hash01(seed, i * 7 + 1, 3) * MathHelper.TwoPi;
                    float r0 = 10f + 6f * progreso;
                    float r1 = r0 + 26f + 34f * progreso;
                    Vector2 start = posPantalla + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r0;
                    Vector2 end = posPantalla + new Vector2(MathF.Cos(ang + 0.5f), MathF.Sin(ang + 0.5f)) * r1;
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 2.2f,
                        ColorConjuro(Projectile.localAI[0] + i * 8f),
                        DefinitivasLib.BlancoCalido,
                        0.35f + 0.45f * progreso, 5, 12f);
                }
            }

            // ============ EL DESTELLO DE LA IGNICIÓN ============
            if (Time > ChargeTime && Time < ChargeTime + 8 && VFXCore.DestelloFinal != null)
            {
                Main.EntitySpriteDraw(VFXCore.DestelloFinal, posPantalla, null,
                    DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.9f * (1f - (Time - ChargeTime) / 8f)),
                    Projectile.rotation, VFXCore.DestelloFinal.Size() * 0.5f,
                    new Vector2(1.6f, 1.6f) * realSize, SpriteEffects.None, 0f);
            }

            // ============ LA ESTRELLA CONDUCTO (la boca arde) ============
            if (Time > ChargeTime && laserCharge > 0.1f)
            {
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    float late = 0.60f + 0.40f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9.3f);
                    HazConcentradoLib.SolRunico4Puntas(Projectile.Center,
                        12f + 26f * realSize, late * laserCharge, doradoExtra: 0.35f);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }

            // ================================================================
            //  ============ LA PILA DE CAPAS DEL CONDUCTO ============
            //  La MISMA polilínea de 1500 puntos, SEIS pasadas: bruma →
            //  glow → (glow²) → CUERPO (el shader del RC2, o su fallback
            //  vanilla) → núcleo → soles conducto.
            // ================================================================
            if (Time >= ChargeTime - 20)
            {
                _laserActivo = Utils.GetLerpValue(ChargeTime - 30, ChargeTime + 70, Time, true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true);

                Vector2[] positions = _cachePuntos;
                float[] rotations = _cacheRotas;
                for (int i = 0; i < 1500; i++)
                {
                    positions[i] = Projectile.Center + new Vector2(LargoCono * (i / 1500f) * (0.5f + _laserActivo * 0.5f), 0f).RotatedBy(Projectile.rotation);
                    rotations[i] = Projectile.rotation;
                }

                int receta = (int)Receta;
                if (receta < 0 || receta > 2) receta = 0;

                VFXCore.CerrarLoteSiAbierto();
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                gd.BlendState = BlendState.Additive;
                try
                {
                    // (1) LA BRUMA — el resplandor AMBIENTAL: violeta
                    // profundo casi disuelto, la banda más ancha de todas
                    // (×2.6..3.6). Es el «bloom» que tiñe el fondo alrededor
                    // del conducto.
                    CapaRampa(gd, textureGlow, KBruma[receta], ColorBruma, positions, rotations);

                    // (2) EL GLOW — la envolvente DIFUSA: la paleta del
                    // conjuro VIAJANDO (violeta→morado→rojo→magenta, de
                    // boca a punta) al 20-28%.
                    CapaRampa(gd, textureGlow, KGlow[receta], ColorGlow, positions, rotations);

                    // (3) EL GLOW² (receta C — EL SUEÑO): la segunda
                    // envolvente interior, más caliente y más suave.
                    if (DobleGlow[receta])
                        CapaRampa(gd, textureGlow, KGlow2[receta], ColorGlow2, positions, rotations);

                    // (4) EL CUERPO — el strip del HazConcentradoShader del
                    // RC2 (el esqueleto 607df36 INTACTO). Si el .fxc no
                    // cargara, el cuerpo cae a una tira de CarnePlasma por
                    // el shader vanilla — LA PILA SE VE IGUAL (GPU-universal).
                    Effect lightningEffect = (texture != null && textureBits != null && textureSecond != null)
                        ? ObtenerShader() : null;
                    if (lightningEffect != null)
                    {
                        lightningEffect.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                        lightningEffect.Parameters["uTexturaCarne"].SetValue(texture);
                        lightningEffect.Parameters["uTexturaSegunda"].SetValue(textureSecond);
                        lightningEffect.Parameters["uTexturaRampa"].SetValue(textureGlow);
                        lightningEffect.Parameters["uTexturaBits"].SetValue(textureBits);
                        lightningEffect.Parameters["uTime"].SetValue(-Projectile.localAI[0] * 0.025f);
                        lightningEffect.Parameters["uEscalaV"].SetValue(MathHelper.Clamp(realSize, 0.18f, 1f));
                        lightningEffect.CurrentTechnique.Passes[0].Apply();
                        _strip.PrepareStripWithProceduralPadding(positions, rotations, StripColor, StripWidth,
                            -Main.screenPosition, true);
                        _strip.DrawTrail();
                        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                    }
                    else
                    {
                        // EL CUERPO DE RESPALDO — CarnePlasma (la textura
                        // anti-picos de la casa, envolvente suave en V) con
                        // la paleta viajando: plasma texturizado SIN .fx.
                        // (texture no puede ser null aquí: el guard del
                        // shader ya lo exigió — pero el respaldo es a prueba
                        // de todo: si faltara la carne, la rampa.)
                        CapaRampa(gd, texture ?? textureGlow, 1.0f, ColorCuerpo, positions, rotations);
                    }

                    // (5) EL NÚCLEO — la vena ardiente: blanco-violeta al
                    // 65-95% en la banda más estrecha (×0.28..0.42). El
                    // «láser dentro de la explosión de energía».
                    CapaRampa(gd, textureGlow, KNucleo[receta], ColorNucleo, positions, rotations);
                }
                finally
                {
                    gd.BlendState = BlendState.AlphaBlend;
                    VFXCore.RestaurarLoteVanilla();
                }

                // (6) LOS SOLES CONDUCTO — los destellos de 4 puntas del
                // accesorio corriendo por las MÁRGENES del conducto (sobre
                // la envolvente del glow, donde se leen contra el violeta),
                // viajando hacia la punta al ritmo de cada sol.
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    SolesDelConducto(SolesConducto[receta], positions);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }

            VFXCore.RestaurarLoteVanilla();
            return false;
        }

        /// <summary>UNA CAPA DE LA PILA: el shader VANILLA de sprites
        /// (Main.pixelShader — el mismo que dibuja todo Terraria, presente
        /// en cualquier GPU) + RampaGlow (perfil gaussiano: 0 en el borde,
        /// pico al centro) → el borde de la capa se funde EN CURVA por
        /// píxel. Cero picos por construcción.</summary>
        private void CapaRampa(GraphicsDevice gd, Texture2D rampa, float k,
            VertexStrip.StripColorFunction colorFn, Vector2[] positions, float[] rotations)
        {
            if (rampa == null) return;
            _kCapa = k;
            gd.Textures[0] = rampa;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            _strip.PrepareStripWithProceduralPadding(positions, rotations, colorFn, AnchoCapa,
                -Main.screenPosition, true);
            _strip.DrawTrail();
        }

        // ============ LOS COLORES DE CADA CAPA (por nodo, la convención
        // de la casa: rgb premultiplicado por el alfa de la capa) ============

        private Color ColorBruma(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            // OSCURECIDA — el «negro» del conjuro: la bruma es la sombra
            // luminosa (violeta profundo con un pulso de rojo al final).
            Color sombra = new Color((byte)(c.R * 0.42f), (byte)(c.G * 0.34f), (byte)(c.B * 0.52f));
            int receta = Math.Clamp((int)Receta, 0, 2);
            return sombra * (AlfaBruma[receta] * _laserActivo);
        }

        private Color ColorGlow(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            int receta = Math.Clamp((int)Receta, 0, 2);
            return c * (AlfaGlow[receta] * _laserActivo);
        }

        private Color ColorGlow2(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            // más caliente: medio tono hacia el blanco-violeta.
            Color caliente = Color.Lerp(c, new Color(255, 246, 255), 0.28f);
            int receta = Math.Clamp((int)Receta, 0, 2);
            return caliente * (AlfaGlow2[receta] * _laserActivo);
        }

        private Color ColorCuerpo(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            return c * (0.62f * _laserActivo);
        }

        private Color ColorNucleo(float p)
        {
            int receta = Math.Clamp((int)Receta, 0, 2);
            // BLANCO-VIOLETA incandescente — la vena del conducto.
            Color nucleo = Color.Lerp(new Color(255, 246, 255), new Color(214, 170, 255), 0.35f);
            return nucleo * (AlfaNucleo[receta] * _laserActivo);
        }

        /// <summary>EL ANCHO DE LA CAPA ACTIVA — el perfil del RC2 (p^0.8 ·
        /// cbrt · realSize · 3500 · respiración) por el factor de la capa:
        /// TODAS las capas respiran JUNTAS (cohesión del conjunto).</summary>
        private float AnchoCapa(float p) => StripWidth(p) * _kCapa;

        // ============ LOS SOLES CONDUCTO ============
        //  Cada sol vive en una MARGEN del conducto (sobre la envolvente
        //  del glow, alternando arriba/abajo), CORRE hacia la punta a su
        //  propio ritmo y late. Determinista por Hash01 (ley §0.4), cero
        //  estado, cero Dust (ley §0).
        private void SolesDelConducto(int cuenta, Vector2[] ptsMundo)
        {
            if (cuenta <= 0 || _laserActivo <= 0.05f) return;
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 perp = new(-MathF.Sin(Projectile.rotation), MathF.Cos(Projectile.rotation));
            for (int i = 0; i < cuenta; i++)
            {
                float ritmo = 0.045f + 0.05f * VFXCore.Hash01(97, 610 + i, 7);
                float p = DefinitivasLib.Frac(VFXCore.Hash01(97, 600 + i, 5) * 0.88f + t * ritmo);
                float medio = StripWidth(p);
                if (medio < 4f) continue;

                // LA MARGEN: entre el filo del cuerpo y la envolvente del
                // glow — ahí el sol se LEE contra el violeta (sobre el
                // núcleo saturado sería invisible).
                float lado = (i % 2 == 0) ? 1f : -1f;
                float margen = (0.62f + 0.30f * VFXCore.Hash01(97, 620 + i, 11)) * medio;
                Vector2 pos = ptsMundo[(int)(p * 1499f)] + perp * (margen * lado);

                // ARDE: el pulso propio + el nacimiento/muerte en los
                // extremos del eje (no asoman fuera del conducto).
                float arde = (0.42f + 0.58f * MathF.Sin(t * 6.3f + i * 2.39f))
                             * Utils.GetLerpValue(0f, 0.07f, p, true)
                             * Utils.GetLerpValue(1f, 0.86f, p, true);
                if (arde <= 0.04f) continue;

                float size = MathF.Max(9f, medio * (0.085f + 0.045f * VFXCore.Hash01(97, 630 + i, 13)));
                HazConcentradoLib.SolRunico4Puntas(pos, size, arde * _laserActivo, doradoExtra: 0.30f);
            }
        }

        /// <summary>EL COLOR DEL CUERPO (el strip del shader): la paleta
        /// del conjuro que VIAJA a lo largo — idéntico al RC2.</summary>
        public Color StripColor(float progress)
        {
            Color color = ColorConjuro(Projectile.localAI[0] - progress * CicloConjuro);
            color.A = 0;
            return color * realSize;
        }

        /// <summary>EL ANCHO DEL STRIP — idéntico al RC2 desde 607df36
        /// (verificado por diff): p^0.8 · cbrt · realSize · 3500 ·
        /// (1.1 + cos·(0.08 − p·0.06)).</summary>
        public float StripWidth(float progress)
        {
            float start = (float)Math.Pow(progress, 0.8f);
            float cap = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));
            return start * cap * realSize * 3500f * (1.1f + (float)Math.Cos(Time) * (0.08f - progress * 0.06f));
        }
    }
}
