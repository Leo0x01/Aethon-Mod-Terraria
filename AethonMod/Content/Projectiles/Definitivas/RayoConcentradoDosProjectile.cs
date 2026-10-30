using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // =====================================================================
    //  v6.56.7 — EL RC2 VUELVE A SU TÉCNICA (commit 607df36) + LOS CÍRCULOS
    //  RÚNICOS + LOS SOLES DEL ACCESORIO + EL SHADER MEJORADO.
    // =====================================================================
    //  LA LETRA del usuario: «el commit donde estaba correcta la tecnica de
    //  haz es el commit 607df36, solo que aqui tenia los circulos planos que
    //  estaban incorrecto y los bordes oscuros… usa el codigo de 607df36
    //  para reparar el problema de los picos y la falta de efecto que tenia
    //  ya que en el commit 607df36 estaba correcta esa parte y solo
    //  necesitaba que le quitaran los circulos planos, y agregaras los
    //  circulos runicos como en la version actual».
    //
    //  EL DIAGNÓSTICO DE LOS PICOS (forense, documentado en el códice §5.4):
    //  el RC2 de la v6.56.6 dibujaba LA PILA DE CAPAS — tres fajitas CPU
    //  (RayoStrip.FajaTexturadaEnLote) SIEMPRE ENCIMA del strip GPU. Las
    //  fajitas son tiras de QUADS de borde POLIGONAL duro, y su capa
    //  «desenfoque» usaba SegundoRuido — una textura BINARIA (52 % negro /
    //  22 % saturado, blobs duros) diseñada para MULTIPLICARSE dentro del
    //  shader, jamás para verse cruda. El resultado: peines de blobs duros
    //  flotando fuera del borde del haz (el simulador 1:1 de la casa lo
    //  midió: pico-valle 222 px contra 163 px de la técnica correcta, y la
    //  verificación visual lo confirmó: «dientes rectangulares duros
    //  saliendo del borde»). En 607df36 las fajitas solo vivían en el
    //  FALLBACK sin shader — con el .fxc cargado, el usuario jamás las vio:
    //  POR ESO «antes no había picos».
    //
    //  LA CURA (la letra aplicada punto por punto):
    //    · LA TÉCNICA DE 607df36 EXACTA: el strip de 1500 puntos con EL
    //      SHADER DE LAS CUATRO TEXTURAS (CarnePlasma × SegundoRuido ×
    //      RampaGlow + BitsChispa en doble AND), la boca de glows
    //      APILADOS (cuatro: 5× color al 20 %, 4× al 60 %, 2× blanco,
    //      14× al 10 % — un bloom de verdad, JAMÁS un círculo plano),
    //      PrepareStripWithProceduralPadding de 6 argumentos y el fallback
    //      de fajitas SOLO si el .fxc no cargó.
    //    · LOS POLVOS CROMÁTICOS MUEREN (eran LOS «círculos planos» de
    //      607df36 — la cura que la v6.56.0 ya había hecho): nada de
    //      partículas genéricas en el conducto.
    //    · LOS BORDES OSCUROS: el strip se dibuja en LOTE ADITIVO explícito
    //      (la cura v6.56.4 — el shader nació aditivo y bajo el pase de
    //      entidades sus bordes V negros OSCURECÍAN lo de debajo).
    //    · EL COLOR DEL MOD: ColorPrisma.ValueAt — el reloj de la paleta
    //      oil (magenta → naranja → amarillo → verde → cian) que recorre
    //      TODO el mod, el gradiente que viaja a lo largo del haz (100
    //      ticks de paleta de la boca a la punta). La reconstrucción
    //      binaria de la v6.56.0 lo cambió por el gradiente del juicio sin
    //      que nadie lo pidiera — EL RC2 VUELVE AL COLOR DEL MOD.
    //    · LOS CÍRCULOS RÚNICOS de la versión actual (la letra: «agregaras
    //      los circulos runicos como en la version actual»): LA CORONA
    //      RÚNICA (OrbitaLib.CoronaConjuro — los círculos dorado/violeta/
    //      blanco de los agujeros negros) al 53 % de su tamaño + los dos
    //      anillos finos — y son ELLOS el indicador de la carga (la letra:
    //      «si necesitas un indicador de carga usa los mismos anillos
    //      runicos»): la corona ENCOJE con la carga (70 → 26 px).
    //    · LOS SOLES RÚNICOS DEL ACCESORIO (la letra: «debes usar los
    //      mismos brillos que usa el accesorio de El brillo de soles
    //      runicos»): el destello de 4 PUNTAS (dos cápsulas cruzadas de
    //      blanco incandescente + el corazón dorado — la receta verbatim
    //      de BrilloSolarHalo.DrawTwinkles) absorbiéndose en la banda
    //      elíptica que encoge (170 → 22 px) y el estallido en abanico al
    //      disparar. LA CURA DE LAS COORDENADAS: VFXCore.Quad vive en
    //      coordenadas de MUNDO (el volcado resta screenPosition) — la
    //      v6.56.6 le pasaba PANTALLA y los destellos nacían FUERA de
    //      pantalla: jamás se vieron. Ahora viven de verdad.
    //    · EL SHADER MEJORADO (la letra: «dale una mejora de shaders
    //      tambien»): EL HALO per-pixel (el desenfoque que la .56.6 buscó
    //      con fajitas — aquí es una curva pow³ que muere exactamente en
    //      el borde del polígono: picos IMPOSIBLES), LOS FILAMENTOS (la
    //      carne multiplicada por una copia de sí misma a media escala —
    //      cordones grandes que serpentean bajo el hervor fino), EL
    //      NÚCLEO QUE LATE (el umbral de la vena blanca respira) y LA
    //      FRANJA PRISMÁTICA (el halo muestreado tres veces con
    //      microdesfase por canal en el filo).
    //    · LO QUE VIVE DE LAS VERSIONES SANAS: el temblor sostenido
    //      (Pantalla.Sacudir — «el mundo debe temblar un poco»), el
    //      sonido de carga + el bucle grave, la música al −30 %, el vuelo
    //      infinito DEL DUEÑO, la mira en MP, los pozos cero-GC y la
    //      exclusión de un solo RC2 a la vez.
    // =====================================================================
    public class RayoConcentradoDosProjectile : ModProjectile
    {
        // LOS TIEMPOS DEL ATAQUE (la conducta intacta desde su nacimiento).
        private static readonly int ChargeTime = 300;
        private static readonly int LaserDuration = 800;
        private const float LargoCono = 3500f;

        public float targetRotation;
        public float targetSize;
        public float realSize;

        // v6.55.1 — LA BANDERA DE FALLO DEL SHADER: si el .fxc no cargara
        // (asset ausente, driver sin soporte), NO se reintenta cada frame —
        // el fallback de tiras asume el mando de inmediato.
        private static bool _shaderFallo;
        private static Effect _shaderCache;

        internal SonidoEnLoop raySound;
        public float volume;
        public float pitch;

        // v6.56.5 — EL SLOT DE LA CARGA (auditoría B, MENOR-2): el one-shot
        // de 5.15 s quedaba huérfano si el dueño moría a mitad de carga.
        private ReLogic.Utilities.SlotId _cargaSlot;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public ref float Time => ref Projectile.ai[0];
        public ref float Owner => ref Projectile.ai[2];

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
            // v6.56.5 — tML recicla las instancias sin limpiar campos: la
            // pizarra nace limpia (ni el slot ni el loop de la vida anterior).
            raySound = null;
            _cargaSlot = default;
        }

        // v6.56.1 — LOS POZOS DEL RC2 (cero-GC): antes cada frame del láser
        // alocaba 2 arrays de 1500 + un VertexStrip nuevo (~36 KB/frame de
        // basura Gen0). Tamaños constantes, reutilizados por siempre.
        private static readonly Vector2[] _cachePuntos = new Vector2[1500];
        private static readonly float[] _cacheRotas = new float[1500];
        private static readonly Vector2[] _cachePuntos64 = new Vector2[64];
        private static readonly float[] _cacheSemis64 = new float[64];
        private static readonly float[] _cacheSemisA = new float[64];
        private static readonly float[] _cacheSemisB = new float[64];
        private static readonly VertexStrip _strip = new VertexStrip();

        /// <summary>
        /// Carga el shader UNA vez (caché + bandera de fallo). Devuelve
        /// null si no está disponible — el llamador usa el fallback.
        /// </summary>
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
            writer.Write(targetRotation);   // v6.56.1: la mira del dueño viaja al server
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.direction = reader.ReadBoolean() ? 1 : -1;
            targetRotation = reader.ReadSingle();   // v6.56.1: la mira sincronizada
        }

        // ================================================================
        //  EL CORAZÓN — la IA del haz (el dueño es el jugador).
        // ================================================================
        public override void AI()
        {
            // EL DUEÑO: el jugador que la disparó (la boca se coloca sobre
            // su hombro, mirando donde él mira).
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

            // EL RUMBO del portador (se orienta con el signo de la
            // rotación ANTES de colocar la boca).
            dueno.direction = Math.Sign(Projectile.rotation.ToRotationVector2().X);
            dueno.itemLocation = dueno.MountedCenter;

            // LA BOCA: el offset (13 px del centro, 20 arriba).
            Projectile.Center = dueno.MountedCenter + new Vector2(13 * dueno.direction, -20f);

            // EL RETROCESO SUAVE del portador mientras dispara (el arrastre,
            // no un empujón — la gravedad del juego pelea de vuelta).
            dueno.velocity += Projectile.rotation.ToRotationVector2() * -0.1f;
            dueno.velocity *= 0.98f;

            if (!Main.dedServ)
            {
                // EL SONIDO DE CARGA (one-shot de 5.15 s — el slot VIAJA con
                // el proyectil: si el dueño cae a mitad de carga,
                // CortarCarga() lo apaga con él).
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

                // EL TEMBLOR (la letra: «debe mantener un pequeño temblor de
                // la pantalla mientras el rayo concentrado se esté
                // ejecutando»): el modelo de trauma de la casa — una
                // sacudida al encendido (≈7.3 px de pico) y el sostenido
                // suave (≈2.75 px constantes) hasta que el rayo se apaga.
                if (Time > ChargeTime && Time < ChargeTime + LaserDuration)
                {
                    Pantalla.Sacudir(6.5f, 0.4f, "RC2-sostenido");
                }
                if (Time == ChargeTime + 1)
                {
                    Pantalla.Sacudir(9f, 0.8f, "RC2-encendido");
                }
            }

            // EL BARRIDO — un coseno completo de ida y vuelta (±2.5 rad)
            // acotado por sus dos envolventes.
            float totalOffRot = (float)Math.Cos((Time - ChargeTime) / LaserDuration * MathHelper.TwoPi) * 2.5f
                * Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + LaserDuration * 0.1f, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration, ChargeTime + LaserDuration * 0.9f, Time, true);

            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 15 == 0 && Time < ChargeTime + LaserDuration * 0.6f)
            {
                Projectile.direction = Main.rand.NextBool() ? -1 : 1;
                Projectile.netUpdate = true;
            }

            // EL GIRO HACIA EL CURSOR — v6.56.1: solo el DUEÑO apunta con SU
            // ratón (el paso decreciente: 0.04 al principio, 0.01 cuando el
            // barrido manda).
            if (Main.netMode != NetmodeID.Server && Main.myPlayer == Projectile.owner)
            {
                targetRotation = targetRotation.AngleLerp(Projectile.AngleTo(Main.MouseWorld),
                    Utils.GetLerpValue(ChargeTime + LaserDuration * 0.1f, ChargeTime, Time, true) * 0.04f
                    + Utils.GetLerpValue(ChargeTime + LaserDuration * 0.3f, ChargeTime + LaserDuration * 0.6f, Time, true) * 0.01f);
            }

            // EL TAMAÑO — la envolvente cúbica (nace en 20 t, muere al
            // final; realSize la persigue a 0.1).
            targetSize = (float)Math.Pow(
                Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 60, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);

            float waveChange = MathHelper.SmoothStep(0f, 1f,
                Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + 20, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration * 0.9f, ChargeTime + LaserDuration * 0.5f, Time, true));
            Projectile.rotation = targetRotation + totalOffRot * Projectile.direction * waveChange;

            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2();

            // EL RELOJ DEL COLOR (el gradiente camina con el mundo).
            Projectile.localAI[0] += 1f;

            // LA LUZ DEL CONDUCTO — el color del mod en tres puntos (la
            // boca, el medio y la punta del cono).
            if (!Main.dedServ && Time > ChargeTime - 10 && Time < ChargeTime + LaserDuration)
            {
                Color cLuz = ColorPrisma.ValueAt(Projectile.localAI[0]);
                float k = realSize;
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Lighting.AddLight(Projectile.Center,
                    cLuz.R / 255f * 1.8f * k, cLuz.G / 255f * 1.7f * k, cLuz.B / 255f * 1.5f * k);
                Lighting.AddLight(Projectile.Center + dir * 1100f,
                    cLuz.R / 255f * 1.4f * k, cLuz.G / 255f * 1.3f * k, cLuz.B / 255f * 1.1f * k);
                Lighting.AddLight(Projectile.Center + dir * 2400f,
                    cLuz.R / 255f * 1.0f * k, cLuz.G / 255f * 0.9f * k, cLuz.B / 255f * 0.8f * k);
            }

            // EL SIZZLE del final (el chisporroteo al apagarse).
            if (Time == ChargeTime + LaserDuration + 20)
            {
                DefinitivasLib.Sonar(SoundID.Item34.WithPitchOffset(-0.4f), Projectile.Center);
                OndaLib.Kick(6f, 10);
            }

            Time++;
            Projectile.timeLeft = 10;

            HandleSound();

            // EL CIERRE del ciclo: cuando el láser muere, el sonido muere
            // con él.
            if (Time > ChargeTime + LaserDuration + 50)
            {
                raySound?.StopSound();
                Projectile.Kill();
                return;
            }

            // EL VUELO INFINITO: mientras el rayo vive, las alas del
            // PORTADOR no se agotan (v6.56.1: solo el dueño — antes
            // cualquier RC2 vivo rellenaba el ala de TODO el mundo).
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

        // ================================================================
        //  EL SONIDO EN LOOP — el volumen por realSize, el pitch grave
        //  sostenido (−0.22) planeando a −0.70 en el apagado.
        // ================================================================
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

        // v6.56.5 — LA CARGA MUERE CON SU DUEÑO: el one-shot de 5.15 s no
        // suena huérfano si el dueño cae a mitad de carga.
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
                // EL CONO: 3500·(0.5+grow·0.5) con el semiángulo 0.28·grow
                // — la colisión de cono propia de la casa.
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center, LargoCono * (0.5f + grow * 0.5f),
                    Projectile.rotation, 0.28f * Math.Max(grow, 0.02f));
            }
            return false;
        }

        // ================================================================
        //  LA MÚSICA −30 % MIENTRAS VIVE (el haz MANDA sobre la orquesta).
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
        //  EL DIBUJO — LA TÉCNICA DE 607df36 (la boca de glows apilados,
        //  el strip de 1500 puntos y EL SHADER DE LAS CUATRO TEXTURAS en
        //  lote aditivo) + LOS CÍRCULOS RÚNICOS + LOS SOLES DEL ACCESORIO.
        //  NADA ENCIMA DEL STRIP: sin pila de capas, sin polvos, sin aguja.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            // LAS CUATRO TEXTURAS DEL PLASMA — las procedurales de la casa.
            Texture2D texture = DefinitivasLib.TexRuido;          // la carne (CarnePlasma)
            Texture2D textureBits = DefinitivasLib.TexBits;       // las chispas (BitsChispa)
            Texture2D textureGlow = DefinitivasLib.TexRampa;      // la rampa (RampaGlow)
            Texture2D textureSecond = DefinitivasLib.TexRemolino; // el 2º ruido (SegundoRuido)
            Texture2D glow = DefinitivasLib.Tex("AethonMod/Content/Effects/Procedural/SoftGlow"); // la boca
            Texture2D ring = VFXCore.Ring;                         // los anillos finos

            if (texture == null || textureBits == null || textureGlow == null || textureSecond == null || glow == null || ring == null)
                return false;

            // EL COLOR DEL MOD — ColorPrisma: el reloj de la paleta oil que
            // recorre TODO el mod (v6.56.7: de vuelta tras la .56.0).
            Color startColor = ColorPrisma.ValueAt(Projectile.localAI[0]);
            startColor.A = 0;

            float laserCharge = Utils.GetLerpValue(50, ChargeTime, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration + 20, Time, true);

            Vector2 posPantalla = Projectile.Center - Main.screenPosition;

            // ============ LA BOCA DE GLOWS APILADOS (607df36) ============
            // Cuatro SoftGlow apilados con el alfa a CERO (el truco de la
            // casa: bajo el pase de entidades, A=0 y rgb>0 = suma pura) —
            // un bloom con NÚCLEO BLANDO y halo ancho. La .56.4-.56.6 lo
            // había reducido a UN glow con alfa lleno: UN CÍRCULO PLANO
            // que TAPABA (el «círculo de la carga» de la letra). NUNCA MÁS.
            Main.EntitySpriteDraw(glow, posPantalla, null, startColor * 0.2f * laserCharge, 0f, glow.Size() * 0.5f, 5f * laserCharge, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, posPantalla, null, startColor * 0.6f * laserCharge, 0f, glow.Size() * 0.5f, 4f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, posPantalla, glow.Frame(), new Color(255, 255, 255, 0) * laserCharge, 0f, glow.Size() * 0.5f, 2f * laserCharge, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, posPantalla, glow.Frame(), startColor * 0.1f * laserCharge, 0f, glow.Size() * 0.5f, 14f * laserCharge, SpriteEffects.None, 0f);

            // ============ LOS SOLES RÚNICOS DEL ACCESORIO ============
            // EL DESTELLO DE 4 PUNTAS de BrilloSolarHalo (las cápsulas
            // cruzadas + el corazón dorado): en la carga, la banda elíptica
            // ENCOGE absorbiéndolos hacia el arma; al disparar, el ESTALLIDO
            // los escupe en abanico. v6.56.7 — LA CURA DE LAS COORDENADAS:
            // VFXCore.Quad vive en MUNDO (el volcado resta screenPosition);
            // la .56.6 pasaba PANTALLA y nacían fuera de la pantalla —
            // ahora se ven DE VERDAD.
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

            // ============ LOS CÍRCULOS RÚNICOS (la .56.6, al 53 %) ============
            // LA CORONA RÚNICA — el ÚNICO círculo del conjuro (la letra:
            // «lo unico que debe tener son los circulos runicos y ademas
            // deben ser al menos un 50 % mas pequeños»): el círculo dorado
            // de 8 runas + el violeta de 6 contrarrotando + el blanco
            // íntimo — y ENCOGE con la carga (70 → 26 px): EL INDICADOR
            // DE CARGA SON LOS ANILLOS RÚNICOS (la letra), no un disco.
            {
                float numP = MathF.Pow(laserCharge, 0.8f);
                float breathe = 1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f);
                float rCorona = MathHelper.Lerp(70f, 26f, numP) * breathe;
                try
                {
                    OrbitaLib.AbrirAdditive();
                    OrbitaLib.CoronaConjuro(posPantalla, rCorona, Main.GlobalTimeWrappedHourly,
                        alphaMul: 0.28f + 0.62f * laserCharge, glyphMul: 1.8f);
                    OrbitaLib.CerrarBatch();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();

                // LOS DOS ANILLOS FINOS — acompañan a la corona al 53 %,
                // contrarrotando, con el color del mod viajero.
                Vector2 tamAnillo = VFXCore.RingQuadSize(rCorona);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0] + 6f), 0.55f * laserCharge),
                    Main.GlobalTimeWrappedHourly * 0.7f, ring.Size() * 0.5f, tamAnillo, SpriteEffects.None, 0f);
                Vector2 tamAnillo2 = VFXCore.RingQuadSize(rCorona * 1.38f);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0] + 18f), 0.3f * laserCharge),
                    -Main.GlobalTimeWrappedHourly * 0.5f + 2.1f, ring.Size() * 0.5f, tamAnillo2, SpriteEffects.None, 0f);
            }

            // ============ LOS MICRO-ARCOS DE LA CARGA ============
            // Los arcos de StormLib (cero sprites, deterministas por hash)
            // que chisporrotean alrededor de la boca mientras carga — más
            // avivados cuanto más llena está la carga.
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
                        ColorPrisma.ValueAt(Projectile.localAI[0] + i * 8f),
                        DefinitivasLib.BlancoCalido,
                        0.35f + 0.45f * progreso, 5, 12f);
                }
            }

            // ============ EL DESTELLO DE LA IGNICIÓN ============
            // La cruz de 8 brazos de la casa en el tick del disparo — el
            // golpe de luz al nacer el conducto.
            if (Time > ChargeTime && Time < ChargeTime + 8 && VFXCore.DestelloFinal != null)
            {
                Main.EntitySpriteDraw(VFXCore.DestelloFinal, posPantalla, null,
                    DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.9f * (1f - (Time - ChargeTime) / 8f)),
                    Projectile.rotation, VFXCore.DestelloFinal.Size() * 0.5f,
                    new Vector2(1.6f, 1.6f) * realSize, SpriteEffects.None, 0f);
            }

            // ============ EL STRIP DEL SHADER (607df36 EXACTO) ============
            // 1500 puntos, la llamada de 6 argumentos de 607df36, el shader
            // de las cuatro texturas con su uTime y su uEscalaV — y TODO EL
            // HAZ DENTRO DE UN LOTE ADITIVO EXPLÍCITO (la cura v6.56.4 de
            // los bordes oscuros: el shader nació aditivo y bajo el pase de
            // entidades sus bordes V negros OSCURECÍAN lo de debajo).
            if (Time >= ChargeTime - 20)
            {
                float laserActive = Utils.GetLerpValue(ChargeTime - 30, ChargeTime + 70, Time, true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true);

                Vector2[] positions = _cachePuntos;
                float[] rotations = _cacheRotas;
                for (int i = 0; i < 1500; i++)
                {
                    positions[i] = Projectile.Center + new Vector2(LargoCono * (i / 1500f) * (0.5f + laserActive * 0.5f), 0f).RotatedBy(Projectile.rotation);
                    rotations[i] = Projectile.rotation;
                }

                _strip.PrepareStripWithProceduralPadding(positions, rotations, StripColor, StripWidth, -Main.screenPosition, true);

                Effect lightningEffect = ObtenerShader();
                if (lightningEffect != null)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    gd.BlendState = BlendState.Additive;
                    try
                    {
                        lightningEffect.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                        lightningEffect.Parameters["uTexturaCarne"].SetValue(texture);
                        lightningEffect.Parameters["uTexturaSegunda"].SetValue(textureSecond);
                        lightningEffect.Parameters["uTexturaRampa"].SetValue(textureGlow);
                        lightningEffect.Parameters["uTexturaBits"].SetValue(textureBits);
                        lightningEffect.Parameters["uTime"].SetValue(-Projectile.localAI[0] * 0.025f);
                        // LA ESCALA COMPENSADA (la cura de los dientes de
                        // sierra del ruido fino): los rasgos del plasma
                        // miden SIEMPRE lo mismo en pantalla.
                        lightningEffect.Parameters["uEscalaV"].SetValue(MathHelper.Clamp(realSize, 0.18f, 1f));
                        lightningEffect.CurrentTechnique.Passes[0].Apply();
                        _strip.DrawTrail();
                        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                    }
                    finally
                    {
                        gd.BlendState = BlendState.AlphaBlend;
                        // El lote se restaura TAMBIÉN por el camino de la
                        // excepción (auditoría v6.56.5: tML mata al
                        // proyectil que dibuja con el lote cerrado).
                        VFXCore.RestaurarLoteVanilla();
                    }
                }
                else
                {
                    // EL FALLBACK de la casa (SOLO si el .fxc no cargó —
                    // jamás junto al shader): el strip de 64 nodos con la
                    // pila de tiras de la librería (carne + remolino +
                    // núcleo) — el mismo haz sin el .fx.
                    Vector2[] pts = _cachePuntos64;
                    float[] semi = _cacheSemis64;
                    for (int i = 0; i < 64; i++)
                    {
                        float p = i / 63f;
                        pts[i] = positions[(int)(p * 1499)] - Main.screenPosition;
                        semi[i] = MathF.Max(StripWidth(p) * 0.5f, 1f);
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        if (texture != null)
                            RayoStrip.FajaTexturadaEnLote(pts, semi,
                                DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0]), 0.6f * realSize),
                                texture, 420f, 1.0f, Main.GlobalTimeWrappedHourly);
                        if (textureSecond != null)
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.7f, _cacheSemisA),
                                DefinitivasLib.Alfa(ColorPrisma.ValueAt(Projectile.localAI[0] + 12f), 0.45f * realSize),
                                textureSecond, 240f, 1.9f, Main.GlobalTimeWrappedHourly);
                        if (texture != null)
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.4f, _cacheSemisB),
                                DefinitivasLib.Alfa(Color.White, 0.85f * realSize),
                                texture, 380f, 2.6f, Main.GlobalTimeWrappedHourly, 0.36f, 0.64f);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // v6.56.7 — NADA ENCIMA DEL STRIP. La «pila de capas» de la
            // v6.56.6 (las fajitas CPU SIEMPRE visibles) ERA LA CAUSA DE
            // LOS PICOS: quads de borde poligonal duro y el remolino
            // BINARIO visto crudo — peines de blobs fuera del borde del
            // haz. TODO lo que el haz necesita de más (el desenfoque, el
            // glow, la riqueza de textura) vive AHORA PER-PIXEL dentro del
            // shader mejorado — un borde fundido por curva no puede
            // producir picos. (El diagnóstico completo: códice §5.4.)

            // EL CONTRATO DEL LOTE: salir con el lote vivo y en estado vanilla.
            VFXCore.RestaurarLoteVanilla();
            return false;
        }

        /// <summary>EL COLOR DEL STRIP — el gradiente del mod que VIAJA a lo
        /// largo (100 ticks de paleta de la boca a la punta, TODO se
        /// desplaza cada frame) por el realSize.</summary>
        public Color StripColor(float progress)
        {
            Color color = ColorPrisma.ValueAt(Projectile.localAI[0] - progress * 100f);
            color.A = 0;
            return color * realSize;
        }

        /// <summary>EL ANCHO DEL STRIP — p^0.8 · cbrt · realSize · 3500 ·
        /// (1.1 + cos·(0.08 − p·0.06)) — nace estrecho, engorda y respira.
        /// Idéntico desde 607df36 (verificado por diff).</summary>
        public float StripWidth(float progress)
        {
            float start = (float)Math.Pow(progress, 0.8f);
            float cap = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));
            return start * cap * realSize * 3500f * (1.1f + (float)Math.Cos(Time) * (0.08f - progress * 0.06f));
        }

        /// <summary>Escala un perfil de semianchos (escribe en el pozo del
        /// llamador — cero alocaciones, la ley de la casa).</summary>
        private static float[] ArraySemis(float[] perfil, float k, float[] destino)
        {
            for (int i = 0; i < perfil.Length; i++)
                destino[i] = perfil[i] * k;
            return destino;
        }
    }

    // ====================================================================
    //  EL SONIDO EN LOOP — la utilidad de la casa: un slot que se
    //  actualiza cada frame con posición/volumen/pitch, y muere solo
    //  cuando el proyectil muere.
    // ====================================================================
    internal sealed class SonidoEnLoop
    {
        private readonly SoundStyle _estilo;
        private ReLogic.Utilities.SlotId _slot;
        private readonly Func<bool> _condicion;

        public SonidoEnLoop(SoundStyle estilo, Func<bool> condicionViva)
        {
            _estilo = estilo with { IsLooped = true };
            _condicion = condicionViva;
            _slot = ReLogic.Utilities.SlotId.Invalid;
        }

        public void PlaySound(Func<Vector2> posicion, Func<float> volumen, Func<float> pitch)
        {
            bool activo = SoundEngine.TryGetActiveSound(_slot, out ActiveSound sonidoActivo);
            if ((!activo || !_slot.IsValid) && _condicion.Invoke())
            {
                _slot = SoundEngine.PlaySound(_estilo, posicion.Invoke(), sonido =>
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
            if (SoundEngine.TryGetActiveSound(_slot, out ActiveSound sonidoActivo) && sonidoActivo != null)
                sonidoActivo.Stop();
            _slot = ReLogic.Utilities.SlotId.Invalid;
        }
    }
}
