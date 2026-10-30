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
using ReLogic.Content;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class RayoConcentradoDosProjectile : ModProjectile
    {
        private static readonly int ChargeTime = 300;

        private static readonly int LaserDuration = 800;

        private const float LargoCono = 3500f;

        public float targetRotation;

        public float targetSize;

        public float realSize;

        private static bool _shaderFallo;

        private static Effect _shaderCache;

        internal SonidoEnLoop raySound;

        // v6.56.5 — EL SLOT DE LA CARGA (auditoría B, MENOR-2): el one-shot
        // de 5.15 s quedaba huérfano si el dueño moría a mitad de carga
        // (seguía sonando hasta el final). Ahora viaja con el proyectil y
        // la muerte del dueño lo corta (CortarCarga).
        private ReLogic.Utilities.SlotId _cargaSlot;

        public float volume;

        public float pitch;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public ref float Time => ref ((ModProjectile)this).Projectile.ai[0];

        public ref float Modo => ref ((ModProjectile)this).Projectile.ai[1];

        public ref float Owner => ref ((ModProjectile)this).Projectile.ai[2];

        // v6.56.6 — LA VARIANTE (la letra: «quiero que hagas otra variantes
        // del rayo concentrado 2, al menos 9 variantes mas con diferentes
        // efectos y tecnicas vfx»): 0 = EL ORIGINAL (el juicio violeta-oro);
        // 1-9 = las variantes (Prisma · Eclipse · Tormenta · Solar ·
        // Vórtice · Espectro · Sierpe · Cuántico · Nova). Viaja en ai[1]
        // — el campo Modo, SIEMPRE 0 en el RC2 desde su nacimiento (la
        // escalera muerta lo usaba como nivel; el RC2 jamás lo tocó) —
        // porque Projectile.ai en este tML solo tiene 3 slots y NewProjectile
        // alimenta ai0-ai2: la variante viaja en el aire DEL MODO y tML la
        // sincroniza solo en MP. Tiñe gradiente, boca, luz y las capas
        // extra de cada técnica.
        public int Variante => (int)Modo;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[((ModProjectile)this).Type] = 5000;
            ProjectileID.Sets.TrailingMode[((ModProjectile)this).Type] = 2;
            ProjectileID.Sets.TrailCacheLength[((ModProjectile)this).Type] = 10;
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 80;
            ((Entity)((ModProjectile)this).Projectile).height = 80;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.hostile = false;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.aiStyle = -1;
            ((ModProjectile)this).Projectile.manualDirectionChange = true;
            ((ModProjectile)this).Projectile.netImportant = true;
            // v6.56.5 — LA AUDITORÍA B (MENOR-4): tML recicla las instancias
            // ModProjectile sin limpiar campos — un re-disparo tras una
            // muerte temprana encontraba el SonidoEnLoop (y su slot) de la
            // vida anterior. La pizarra nace limpia.
            raySound = null;
            _cargaSlot = default;
        }

        // v6.56.1 — LOS POZOS DEL RC2 (cero-GC): antes cada frame del láser
        // alocaba 2 arrays de 1500 + un VertexStrip nuevo (~36 KB/frame de
        // basura Gen0 mientras vivía el arma cima). Tamaños constantes.
        private static readonly Vector2[] _cachePuntos = (Vector2[])(object)new Vector2[1500];
        private static readonly float[] _cacheRotas = new float[1500];
        private static readonly Vector2[] _cachePuntos64 = (Vector2[])(object)new Vector2[64];
        private static readonly float[] _cacheSemis64 = new float[64];
        private static readonly float[] _cacheSemisA = new float[64];
        private static readonly float[] _cacheSemisB = new float[64];
        private static readonly VertexStrip _strip = new VertexStrip();

        private static Effect ObtenerShader()
        {
            if (_shaderFallo)
            {
                return null;
            }
            if (_shaderCache != null && !((GraphicsResource)_shaderCache).IsDisposed)
            {
                return _shaderCache;
            }
            try
            {
                _shaderCache = ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/HazConcentradoShader", (AssetRequestMode)1).Value;
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
            writer.Write(((Entity)((ModProjectile)this).Projectile).direction == 1);
            // v6.56.1 — LA CURA DE LA RECURSIÓN: el decompilador convirtió
            // el base-call en auto-llamada virtual (callvirt) — StackOverflow
            // en el primer sync de MP. La base es vacía: NO hay nada que llamar.
            writer.Write(targetRotation);   // v6.56.1: la mira del dueño viaja al server
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ((Entity)((ModProjectile)this).Projectile).direction = (reader.ReadBoolean() ? 1 : (-1));
            targetRotation = reader.ReadSingle();   // v6.56.1: la mira sincronizada
        }

        public override void AI()
        {
            //IL_0062: Unknown result type (might be due to invalid IL or missing references)
            //IL_0078: Unknown result type (might be due to invalid IL or missing references)
            //IL_007d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0089: Unknown result type (might be due to invalid IL or missing references)
            //IL_009d: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_00be: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0298: Unknown result type (might be due to invalid IL or missing references)
            //IL_0425: Unknown result type (might be due to invalid IL or missing references)
            //IL_042a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0461: Unknown result type (might be due to invalid IL or missing references)
            //IL_0466: Unknown result type (might be due to invalid IL or missing references)
            //IL_046f: Unknown result type (might be due to invalid IL or missing references)
            //IL_047a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0172: Unknown result type (might be due to invalid IL or missing references)
            //IL_0186: Unknown result type (might be due to invalid IL or missing references)
            //IL_019f: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a9: Expected O, but got Unknown
            //IL_04df: Unknown result type (might be due to invalid IL or missing references)
            //IL_04e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_04fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_0506: Unknown result type (might be due to invalid IL or missing references)
            //IL_055b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0560: Unknown result type (might be due to invalid IL or missing references)
            //IL_0567: Unknown result type (might be due to invalid IL or missing references)
            //IL_056c: Unknown result type (might be due to invalid IL or missing references)
            //IL_05c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_05c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_05cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_05d2: Unknown result type (might be due to invalid IL or missing references)
            if (Owner < 0f || Owner >= 255f)
            {
                CortarCarga();
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            Player val = Main.player[(int)Owner];
            if (val == null || !((Entity)val).active || val.dead)
            {
                CortarCarga();
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            ((Entity)val).direction = Math.Sign(Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation).X);
            val.itemLocation = val.MountedCenter;
            ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + new Vector2((float)(13 * ((Entity)val).direction), -20f);
            ((Entity)val).velocity = ((Entity)val).velocity + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * -0.1f;
            ((Entity)val).velocity = ((Entity)val).velocity * 0.98f;
            if (!Main.dedServ)
            {
                // v6.56.4 — EL PAQUETE DEL PODER (la letra: «mientras el rayo
                // concentrado 2 está siendo disparado el mundo debe temblar un
                // poco así el jugador nota el poder» + el reemplazo de la
                // aguja + el sonido de la carga). El Punch cada 4 ticks de la
                // v6.56.3 se sentía irregular y usaba Main.rand (ley §0.4).
                // Ahora: EL TEMBLOR por el modelo de trauma de la casa
                // (Pantalla.Sacudir: una sacudida fuerte al encendido y un
                // temblor sostenido suave durante el láser, muriendo solo
                // cuando el rayo se apaga); LOS DESTELLOS de los soles
                // rúnicos (se reúnen a gran velocidad en el centro durante
                // la carga y estallan hacia la dirección del rayo al
                // disparar — el reemplazo de la aguja: «esa línea debes
                // quitar, se ve horrible»); LA LENTE (GravLens: el espacio
                // se curva alrededor del conjuro) y EL SONIDO DE CARGA
                // (one-shot generado — gen_sonido_v6564.py: «en el momento
                // de carga necesita sonar que está cargando»).
                if (Time == 2f)
                {
                    SoundStyle carga = new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoCarga", SoundType.Sound)
                    {
                        MaxInstances = 1,
                        Volume = 1f
                    };
                    // v6.56.5 — el slot VIAJA con el proyectil (auditoría B,
                    // MENOR-2): si el dueño cae a mitad de carga, CortarCarga()
                    // lo apaga con él en vez de dejar 5 s de carga huérfana.
                    // (El .ogg además fue regenerado SIN el pitido del finale —
                    // gen_sonido_v6565.py: el seno puro de 1376 Hz a -37 dBFS
                    // con 31 dB de prominencia que el usuario oyó al final.)
                    try
                    {
                        _cargaSlot = Terraria.Audio.SoundEngine.PlaySound(carga, ((Entity)((ModProjectile)this).Projectile).Center);
                    }
                    catch { }
                }
                // v6.56.6 — LA CIRUGÍA DE LA LETRA: «no debe de haber
                // ningún círculo que cubra al personaje» (EL DISCO NEGRO
                // muere en PreDraw) · «tampoco tiene que tener lente
                // gravitación» (GravLens MUERE por completo — el registro
                // de la carga y el del láser) · «los brillos de soles
                // rúnicos absorbidos… los mismos del accesorio» (LOS
                // DESTELLOS DE 4 PUNTAS ahora viven en PreDraw:
                // HazConcentradoLib.SolesRunicosCarga/Estallido — puros,
                // deterministas, sin partículas genéricas).
                // LO QUE VIVE: EL TEMBLOR (la letra: «debe mantener un
                // pequeño temblor de la pantalla mientras el rayo
                // concentrado se esté ejecutando» — el 6.5 sostenido
                // ≈2.75 px constantes + el 9 de encendido ≈7.3 px de pico).
                if (Time > (float)ChargeTime && Time < (float)(ChargeTime + LaserDuration))
                {
                    Pantalla.Sacudir(6.5f, 0.4f, "RC2-sostenido");
                }
                if (Time == (float)(ChargeTime + 1))
                {
                    Pantalla.Sacudir(9f, 0.8f, "RC2-encendido");
                }
            }
            float num = (float)Math.Cos((Time - (float)ChargeTime) / (float)LaserDuration * ((float)Math.PI * 2f)) * 2.5f * Utils.GetLerpValue((float)ChargeTime * 0.8f, (float)ChargeTime + (float)LaserDuration * 0.1f, Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration), (float)ChargeTime + (float)LaserDuration * 0.9f, Time, true);
            if (Main.netMode != 1 && Time % 15f == 0f && Time < (float)ChargeTime + (float)LaserDuration * 0.6f)
            {
                ((Entity)((ModProjectile)this).Projectile).direction = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
                ((ModProjectile)this).Projectile.netUpdate = true;
            }
            // v6.56.1 — LA MIRA EN MP: solo el DUEÑO apunta con SU ratón.
            if (Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)
            {
                targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.1f, (float)ChargeTime, Time, true) * 0.04f + Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.3f, (float)ChargeTime + (float)LaserDuration * 0.6f, Time, true) * 0.01f);
            }
            targetSize = (float)Math.Pow(Utils.GetLerpValue((float)(ChargeTime - 20), (float)(ChargeTime + 60), Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 60), (float)(ChargeTime + LaserDuration), Time, true), 3.0);
            float num2 = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue((float)ChargeTime * 0.8f, (float)(ChargeTime + 20), Time, true) * Utils.GetLerpValue((float)ChargeTime + (float)LaserDuration * 0.9f, (float)ChargeTime + (float)LaserDuration * 0.5f, Time, true));
            ((ModProjectile)this).Projectile.rotation = targetRotation + num * (float)((Entity)((ModProjectile)this).Projectile).direction * num2;
            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            ((Entity)((ModProjectile)this).Projectile).velocity = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
            ((ModProjectile)this).Projectile.localAI[0] += 1f;
            if (Time == (float)(ChargeTime + LaserDuration + 20))
            {
                SoundStyle item = SoundID.Item34;
                DefinitivasLib.Sonar((item).WithPitchOffset(-0.4f), ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Kick(6f, 10);
            }
            if (!Main.dedServ && Time > (float)(ChargeTime - 10) && Time < (float)(ChargeTime + LaserDuration))
            {
                Color val2 = VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f);
                float num3 = realSize;
                Vector2 val3 = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center, (float)(int)(val2).R / 255f * 1.8f * num3, (float)(int)(val2).G / 255f * 1.7f * num3, (float)(int)(val2).B / 255f * 1.5f * num3);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + val3 * 1100f, (float)(int)(val2).R / 255f * 1.4f * num3, (float)(int)(val2).G / 255f * 1.3f * num3, (float)(int)(val2).B / 255f * 1.1f * num3);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + val3 * 2400f, (float)(int)(val2).R / 255f * 1f * num3, (float)(int)(val2).G / 255f * 0.9f * num3, (float)(int)(val2).B / 255f * 0.8f * num3);
            }
            Time += 1f;
            ((ModProjectile)this).Projectile.timeLeft = 10;
            HandleSound();
            if (Time > (float)(ChargeTime + LaserDuration + 50))
            {
                raySound?.StopSound();
                ((ModProjectile)this).Projectile.Kill();
            }
            // v6.56.1 — EL VUELO ES DEL DUEÑO: antes cualquier RC2 vivo en
            // el mundo le rellenaba el wingTime a TODOS los jugadores.
            else if (Main.netMode == 2)
            {
                Player val4 = Main.player[(int)Owner];
                if (val4 != null && ((Entity)val4).active && !val4.dead)
                {
                    val4.wingTime = val4.wingTimeMax;
                }
            }
            else if (Main.LocalPlayer != null && ((Entity)Main.LocalPlayer).whoAmI == (int)Owner)
            {
                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;
            }
        }

        public void HandleSound()
        {
            //IL_00a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Expected O, but got Unknown
            // v6.56.5 — LA AUDITORÍA B (MENOR-4): el servidor dedicado no
            // tiene audio — antes allocaba el SonidoEnLoop + su tracker en
            // vano en cada disparo del servidor.
            if (Main.dedServ)
            {
                return;
            }
            volume = 1.3f * Utils.GetLerpValue(0f, 0.05f, realSize, true);
            // v6.56.4 — EL PITCH DEL HAZ (la letra: «el sonido actual es muy
            // agudo para el tipo de haz de luz, el sonido que debe hacer tiene
            // que ser un poco más grave mientras se dispara y mantiene el rayo
            // concentrado, y en el momento en que el rayo comienza a apagarse
            // el sonido también debe [apagarse]"). Antes quedaba CONGELADO en
            // +0.2 durante todo el láser (SmoothStep(0.5,1.2)·1 − 1: el piche
            // agudo de la .56.3). Ahora: SOSTENIDO grave (−0.22) y plane a
            // −0.70 en los últimos 60 ticks — y el volumen ya viaja con
            // realSize, así que muere con el rayo. La 1ª gen del loop además
            // se re-sintetitzó una octava más oscura (gen_sonido_v6564.py).
            pitch = MathHelper.Lerp(-0.22f, -0.7f, 1f - Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 60), (float)(ChargeTime + LaserDuration), Time, true));
            if (raySound == null)
            {
                SoundStyle estilo = new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop", (SoundType)0);
                (estilo).MaxInstances = 0;
                raySound = new SonidoEnLoop(estilo, (Func<bool>)new ProjectileAudioTracker(((ModProjectile)this).Projectile).IsActiveAndInGame);
            }
            raySound.PlaySound(() => ((Entity)((ModProjectile)this).Projectile).Center, () => volume, () => pitch);
        }

        // v6.56.5 — LA CARGA MUERE CON SU DUEÑO (auditoría B, MENOR-2): si
        // el jugador cae a mitad de carga, el one-shot de 5.15 s seguía
        // sonando hasta el final, huérfano. Este corte lo apaga con él.
        private void CortarCarga()
        {
            try
            {
                if (_cargaSlot.IsValid && Terraria.Audio.SoundEngine.TryGetActiveSound(_cargaSlot, out Terraria.Audio.ActiveSound sonidoActivo) && sonidoActivo != null)
                {
                    sonidoActivo.Stop();
                }
            }
            catch
            {
            }
            _cargaSlot = default;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_0089: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            if (Time > (float)(ChargeTime + 20) && Time < (float)(ChargeTime + LaserDuration + 10))
            {
                float num = (float)Math.Pow(Utils.GetLerpValue((float)(ChargeTime - 20), (float)(ChargeTime + 40), Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 60), (float)(ChargeTime + LaserDuration), Time, true), 3.0);
                return DefinitivasLib.Cono(targetHitbox, ((Entity)((ModProjectile)this).Projectile).Center, 3500f * (0.5f + num * 0.5f), ((ModProjectile)this).Projectile.rotation, 0.28f * Math.Max(num, 0.02f));
            }
            return false;
        }

        public override void Load()
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Expected O, but got Unknown
            On_Main.UpdateAudio += new On_Main.hook_UpdateAudio(CallarMusica);
        }

        public override void Unload()
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Expected O, but got Unknown
            On_Main.UpdateAudio -= new On_Main.hook_UpdateAudio(CallarMusica);
        }

        private void CallarMusica(On_Main.orig_UpdateAudio orig, Main self)
        {
            orig.Invoke(self);
            Projectile val = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i] != null && ((Entity)Main.projectile[i]).active && Main.projectile[i].type == ((ModProjectile)this).Type)
                {
                    val = Main.projectile[i];
                    break;
                }
            }
            if (val != null)
            {
                for (int j = 0; j < Main.musicFade.Length; j++)
                {
                    float num = Utils.GetLerpValue((float)(ChargeTime - 15), (float)(ChargeTime + 40), val.ai[0], true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 50), (float)(ChargeTime + LaserDuration), val.ai[0], true);
                    float num2 = Main.musicFade[j] * Main.musicVolume * (1f - num * 0.3f);
                    float num3 = Main.musicFade[j];
                    Main.audioSystem.UpdateCommonTrackTowardStopping(j, num2, ref num3, Main.musicFade[j] > 0.15f);
                    Main.musicFade[j] = num3;
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_005a: Unknown result type (might be due to invalid IL or missing references)
            //IL_005f: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0112: Unknown result type (might be due to invalid IL or missing references)
            //IL_0136: Unknown result type (might be due to invalid IL or missing references)
            //IL_0142: Unknown result type (might be due to invalid IL or missing references)
            //IL_014c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0158: Unknown result type (might be due to invalid IL or missing references)
            //IL_0162: Unknown result type (might be due to invalid IL or missing references)
            //IL_017c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0188: Unknown result type (might be due to invalid IL or missing references)
            //IL_0195: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_0200: Unknown result type (might be due to invalid IL or missing references)
            //IL_0205: Unknown result type (might be due to invalid IL or missing references)
            //IL_0209: Unknown result type (might be due to invalid IL or missing references)
            //IL_022e: Unknown result type (might be due to invalid IL or missing references)
            //IL_023b: Unknown result type (might be due to invalid IL or missing references)
            //IL_024d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0257: Unknown result type (might be due to invalid IL or missing references)
            //IL_025c: Unknown result type (might be due to invalid IL or missing references)
            //IL_026f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0274: Unknown result type (might be due to invalid IL or missing references)
            //IL_0278: Unknown result type (might be due to invalid IL or missing references)
            //IL_029d: Unknown result type (might be due to invalid IL or missing references)
            //IL_02aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0304: Unknown result type (might be due to invalid IL or missing references)
            //IL_0311: Unknown result type (might be due to invalid IL or missing references)
            //IL_031b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0325: Unknown result type (might be due to invalid IL or missing references)
            //IL_0337: Unknown result type (might be due to invalid IL or missing references)
            //IL_0346: Unknown result type (might be due to invalid IL or missing references)
            //IL_034b: Unknown result type (might be due to invalid IL or missing references)
            //IL_035d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0516: Unknown result type (might be due to invalid IL or missing references)
            //IL_0522: Unknown result type (might be due to invalid IL or missing references)
            //IL_0547: Unknown result type (might be due to invalid IL or missing references)
            //IL_055c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0566: Unknown result type (might be due to invalid IL or missing references)
            //IL_0575: Unknown result type (might be due to invalid IL or missing references)
            //IL_0580: Unknown result type (might be due to invalid IL or missing references)
            //IL_0431: Unknown result type (might be due to invalid IL or missing references)
            //IL_0441: Unknown result type (might be due to invalid IL or missing references)
            //IL_0448: Unknown result type (might be due to invalid IL or missing references)
            //IL_044d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0452: Unknown result type (might be due to invalid IL or missing references)
            //IL_0454: Unknown result type (might be due to invalid IL or missing references)
            //IL_0470: Unknown result type (might be due to invalid IL or missing references)
            //IL_0477: Unknown result type (might be due to invalid IL or missing references)
            //IL_047c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0481: Unknown result type (might be due to invalid IL or missing references)
            //IL_0488: Unknown result type (might be due to invalid IL or missing references)
            //IL_048a: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0616: Unknown result type (might be due to invalid IL or missing references)
            //IL_063e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0651: Unknown result type (might be due to invalid IL or missing references)
            //IL_0657: Unknown result type (might be due to invalid IL or missing references)
            //IL_0659: Unknown result type (might be due to invalid IL or missing references)
            //IL_065e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0663: Unknown result type (might be due to invalid IL or missing references)
            //IL_0687: Unknown result type (might be due to invalid IL or missing references)
            //IL_068e: Expected O, but got Unknown
            //IL_069b: Unknown result type (might be due to invalid IL or missing references)
            //IL_06a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_06b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_06bd: Expected O, but got Unknown
            //IL_06bd: Expected O, but got Unknown
            //IL_06e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_080a: Unknown result type (might be due to invalid IL or missing references)
            //IL_080f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0814: Unknown result type (might be due to invalid IL or missing references)
            //IL_0819: Unknown result type (might be due to invalid IL or missing references)
            //IL_0875: Unknown result type (might be due to invalid IL or missing references)
            //IL_0886: Unknown result type (might be due to invalid IL or missing references)
            //IL_08d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_08e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_091e: Unknown result type (might be due to invalid IL or missing references)
            //IL_092f: Unknown result type (might be due to invalid IL or missing references)
            Texture2D texRuido = DefinitivasLib.TexRuido;
            Texture2D texBits = DefinitivasLib.TexBits;
            Texture2D texRampa = DefinitivasLib.TexRampa;
            Texture2D texRemolino = DefinitivasLib.TexRemolino;
            Texture2D softGlow = VFXCore.SoftGlow;
            Texture2D glowOrb = VFXCore.GlowOrb;
            Texture2D ring = VFXCore.Ring;
            if (texRuido == null || texBits == null || texRampa == null || texRemolino == null || softGlow == null || glowOrb == null || ring == null)
            {
                return false;
            }
            Color c = VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f);
            (c).A = 0;
            float num = Utils.GetLerpValue(50f, (float)ChargeTime, Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 50), (float)(ChargeTime + LaserDuration + 20), Time, true);
            // v6.56.4 — LA AGUJA MUERE (la letra: «esa línea debes quitar, se
            // ve horrible»): era un SoftGlow estirado a 900 px que avanzaba y
            // retrocedía. v6.56.6 — su reemplazo DEFINITIVO: LOS SOLES RÚNICOS
            // DE 4 PUNTAS del accesorio (abajo en este mismo PreDraw — la
            // banda elíptica encoge hacia el arma y el estallido los expulsa).
            float num3 = ((Entity)((ModProjectile)this).Projectile).Center.X - Main.screenPosition.X;
            float num4 = ((Entity)((ModProjectile)this).Projectile).Center.Y - Main.screenPosition.Y;
            Vector2 val = new Vector2(num3, num4);
            Main.EntitySpriteDraw(softGlow, val, (Rectangle?)null, DefinitivasLib.Alfa(c, 0.55f * num), 0f, Utils.Size(softGlow) * 0.5f, 2.2f * num, (SpriteEffects)0, 0f);
            // v6.56.6 — EL PORTAL DEL CONJURO MUERE (la letra: «hay un
            // circulo negro, no debe de haber ningun circulo que cubra al
            // personaje… tampoco tiene que tener lente gravitación, lo
            // unico que debe tener son los circulos runicos y ademas deben
            // ser al menos un 50% mas pequeños»). EL DISCO NEGRO (el
            // horizonte AlphaBlend que OSCURECÍA al personaje) y LA LENTE
            // (GravLens, muerta también en AI) se RETIRAN. Lo que queda y
            // brilla: LOS SOLES RÚNICOS DE LA CARGA (el brillo de 4 puntas
            // del accesorio — absorbidos por el arma, expulsados al
            // disparar) + LA CORONA RÚNICA (OrbitaLib.CoronaConjuro) al
            // 53% de su tamaño + los dos anillos finos, igual de pequeños.
            {
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    if (Time < (float)ChargeTime)
                    {
                        // LOS SOLES ABSORBIDOS: la banda elíptica ENCOJE con
                        // la carga (el hervidero de 4 puntas se aviva).
                        HazConcentradoLib.SolesRunicosCarga(val,
                            Utils.GetLerpValue(20f, (float)ChargeTime, Time, true),
                            Main.GlobalTimeWrappedHourly);
                    }
                    else if (Time < (float)(ChargeTime + 22))
                    {
                        // EL ESTALLIDO: lo absorbido SALE en abanico.
                        HazConcentradoLib.SolesRunicosEstallido(val,
                            ((ModProjectile)this).Projectile.rotation,
                            (Time - (float)ChargeTime) / 22f,
                            Main.GlobalTimeWrappedHourly);
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();

                // LA CORONA RÚNICA — el ÚNICO círculo permitido, al 53% del
                // tamaño de la .56.5 (Lerp(150,56) → Lerp(70,26)): «varios
                // círculos rúnicos de agujeros negros» a escala de conjuro
                // íntimo, no de eclipse.
                float numP = MathF.Pow(num, 0.8f);
                float breathe = 1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f);
                float rCorona = MathHelper.Lerp(70f, 26f, numP) * breathe;
                try
                {
                    OrbitaLib.AbrirAdditive();
                    OrbitaLib.CoronaConjuro(val, rCorona, Main.GlobalTimeWrappedHourly,
                        alphaMul: 0.28f + 0.62f * num, glyphMul: 1.8f);
                    OrbitaLib.CerrarBatch();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
            }
            // v6.56.6 — los anillos finos acompañan a la corona al 53%.
            float num5 = MathF.Pow(num, 0.8f);
            float num6 = MathHelper.Lerp(70f, 26f, num5) * (1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f));
            Vector2 val2 = VFXCore.RingQuadSize(num6);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f + 0.1f), 0.55f * num), Main.GlobalTimeWrappedHourly * 0.7f, Utils.Size(ring) * 0.5f, val2, (SpriteEffects)0, 0f);
            Vector2 val3 = VFXCore.RingQuadSize(num6 * 1.38f);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f + 0.3f), 0.3f * num), (0f - Main.GlobalTimeWrappedHourly) * 0.5f + 2.1f, Utils.Size(ring) * 0.5f, val3, (SpriteEffects)0, 0f);
            if (Time < (float)ChargeTime && Time % 14f < 3f)
            {
                float lerpValue = Utils.GetLerpValue(0f, (float)ChargeTime, Time, true);
                int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) & 0x3FF;
                int num7 = 1 + (int)(lerpValue * 3.99f);
                for (int i = 0; i < num7; i++)
                {
                    int seed = (int)(Time * 0.017f) * 131 + i * 297;
                    float num8 = VFXCore.Hash01(seed, i * 7 + 1, 3) * ((float)Math.PI * 2f);
                    float num9 = 10f + 6f * lerpValue;
                    float num10 = num9 + 26f + 34f * lerpValue;
                    Vector2 start = val + new Vector2(MathF.Cos(num8), MathF.Sin(num8)) * num9;
                    Vector2 end = val + new Vector2(MathF.Cos(num8 + 0.5f), MathF.Sin(num8 + 0.5f)) * num10;
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 2.2f, VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f + (float)i * 0.13f), DefinitivasLib.BlancoCalido, 0.35f + 0.45f * lerpValue, 5, 12f);
                }
            }
            if (Time > (float)ChargeTime && Time < (float)(ChargeTime + 8) && VFXCore.DestelloFinal != null)
            {
                Main.EntitySpriteDraw(VFXCore.DestelloFinal, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.9f * (1f - (Time - (float)ChargeTime) / 8f)), ((ModProjectile)this).Projectile.rotation, Utils.Size(VFXCore.DestelloFinal) * 0.5f, new Vector2(1.6f, 1.6f) * realSize, (SpriteEffects)0, 0f);
            }
            if (Time >= (float)(ChargeTime - 20))
            {
                float num11 = Utils.GetLerpValue((float)(ChargeTime - 30), (float)(ChargeTime + 70), Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 60), (float)(ChargeTime + LaserDuration), Time, true);
                Vector2[] array = _cachePuntos;
                float[] array2 = _cacheRotas;
                // v6.56.6 — LA TÉCNICA DE LA SIERPE (V7): el eje entero
                // ONDULA — una serpiente de luz que se desliza por el cielo
                // (SierpesLib vive en la casa; aquí el eje del conducto es el
                // cuerpo). Perpendicular al haz, sin fase por nodo corrupta:
                // q·3π − localAI·0.11 recorre la onda a lo largo.
                bool esSierpe = Variante == 7;
                float ondaSierpe = esSierpe ? 34f * realSize : 0f;
                Vector2 perpHaz = new((0f - MathF.Sin(((ModProjectile)this).Projectile.rotation)), MathF.Cos(((ModProjectile)this).Projectile.rotation));
                for (int j = 0; j < 1500; j++)
                {
                    float qj = (float)j / 1500f;
                    array[j] = ((Entity)((ModProjectile)this).Projectile).Center + Utils.RotatedBy(new Vector2(3500f * qj * (0.5f + num11 * 0.5f), 0f), (double)((ModProjectile)this).Projectile.rotation, default(Vector2));
                    if (esSierpe)
                    {
                        float fase = MathF.Sin(qj * 9.42f - ((ModProjectile)this).Projectile.localAI[0] * 0.11f)
                                     * MathF.Sin(qj * MathF.PI);   // anclado: 0 en la boca y la punta
                        array[j] += perpHaz * (fase * ondaSierpe);
                    }
                    array2[j] = ((ModProjectile)this).Projectile.rotation;
                }
                VertexStrip val4 = _strip;
                val4.PrepareStripWithProceduralPadding(array, array2, new VertexStrip.StripColorFunction(StripColor), new VertexStrip.StripHalfWidthFunction(StripWidth), -Main.screenPosition, true, true);
                Effect val5 = ObtenerShader();
                if (val5 != null)
                {
                    // v6.56.4 — EL LOTE ADITIVO DEL HAZ (la letra: «al
                    // disparar el haz de luz… en el momento de agrandarse y
                    // hacerse ancho en los bordes hay franjas negras»). El
                    // shader nació ADITIVO (su propio .fx lo declara) pero se
                    // dibujaba bajo el lote de entidades (AlphaBlend): las 4
                    // texturas multiplicadas llevan bordes V NEGRO PURO y bajo
                    // AlphaBlend el borde del haz OSCURECE lo de debajo — la
                    // franja negra justo donde el haz se ensancha. Se cierra
                    // el lote de entidades (lo encolado queda DEBAJO del haz),
                    // el dispositivo pasa a aditivo para el DrawTrail y la
                    // salida vuelve al pase vanilla (RestaurarLoteVanilla).
                    VFXCore.CerrarLoteSiAbierto();
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    gd.BlendState = BlendState.Additive;
                    try
                    {
                        val5.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                        val5.Parameters["uTexturaCarne"].SetValue((Texture)(object)texRuido);
                        val5.Parameters["uTexturaSegunda"].SetValue((Texture)(object)texRemolino);
                        val5.Parameters["uTexturaRampa"].SetValue((Texture)(object)texRampa);
                        val5.Parameters["uTexturaBits"].SetValue((Texture)(object)texBits);
                        val5.Parameters["uTime"].SetValue((0f - ((ModProjectile)this).Projectile.localAI[0]) * 0.025f);
                        val5.Parameters["uEscalaV"].SetValue(MathHelper.Clamp(realSize, 0.18f, 1f));
                        val5.CurrentTechnique.Passes[0].Apply();
                        val4.DrawTrail();
                        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                    }
                    finally
                    {
                        gd.BlendState = BlendState.AlphaBlend;
                        // v6.56.5 — LA AUDITORÍA B (MENOR-1): si algo tronara
                        // entre el cierre del lote (CerrarLoteSiAbierto) y la
                        // salida, el lote quedaba CERRADO — y tML mata al
                        // proyectil que dibuja con el lote cerrado. La cura
                        // ahora también corre por el camino de la excepción.
                        VFXCore.RestaurarLoteVanilla();
                    }
                }
                else
                {
                    Vector2[] array3 = _cachePuntos64;
                    float[] array4 = _cacheSemis64;
                    for (int k = 0; k < 64; k++)
                    {
                        float num12 = (float)k / 63f;
                        array3[k] = array[(int)(num12 * 1499f)] - Main.screenPosition;
                        array4[k] = MathF.Max(StripWidth(num12) * 0.5f, 1f);
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        Texture2D texRuido2 = DefinitivasLib.TexRuido;
                        Texture2D texRemolino2 = DefinitivasLib.TexRemolino;
                        if (texRuido2 != null)
                        {
                            RayoStrip.FajaTexturadaEnLote(array3, array4, DefinitivasLib.Alfa(VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f), 0.6f * realSize), texRuido2, 420f, 1f, Main.GlobalTimeWrappedHourly);
                        }
                        if (texRemolino2 != null)
                        {
                            RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.7f, _cacheSemisA), DefinitivasLib.Alfa(VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f + 0.2f), 0.45f * realSize), texRemolino2, 240f, 1.9f, Main.GlobalTimeWrappedHourly);
                        }
                        if (texRuido2 != null)
                        {
                            RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.4f, _cacheSemisB), DefinitivasLib.Alfa(Color.White, 0.85f * realSize), texRuido2, 380f, 2.6f, Main.GlobalTimeWrappedHourly, 0.36f, 0.64f);
                        }
                    }
                    finally
                    {
                        RayoStrip.CerrarLote();
                    }
                }
            }
            // ============================================================
            //  v6.56.6 — LA PILA DE CAPAS DEL HAZ (la letra: «le quitaste
            //  los efectos y otras texturas, ahora solo usa 1 textura, le
            //  faltan mas texturas y mas efectos, como el desenfoque, el
            //  glow, el brillo entre otras cosas… es un ataque definitivo,
            //  debe ser lo mas vistoso posible, lleno de efectos visuales,
            //  profesional y poderoso»). La tira del shader (las 4 texturas
            //  del plasma) NO basta sola: encima viaja LA PILA —
            //    · EL DESENFOQUE: remolino a 1.8× de semiancho, al 30% —
            //      el halo difuso que desdibuja los bordes;
            //    · EL GLOW: carne fBm a 1.35×, la rampa del gradiente que
            //      respira a lo largo;
            //    · EL BRILLO: la vena central blanca (ventana V 0.36-0.64)
            //      — el corazón incandescente del conducto;
            //    · LOS SOLES DEL CONDUCTO: destellos de 4 puntas corriendo
            //      por dentro (la firma de la carga, ahora en el haz);
            //    · LA TÉCNICA DE LA VARIANTE: cada RC2 su propio monstruo.
            // ============================================================
            if (Time >= (float)(ChargeTime - 20))
            {
                DibujarPilaDeCapas();
            }
            VFXCore.RestaurarLoteVanilla();
            return false;
        }

        // v6.56.6 — LOS POZOS DE LAS RAMAS CUÁNTICAS (V8): cero-GC.
        private static readonly Vector2[] _pozoRamaPts = (Vector2[])(object)new Vector2[16];
        private static readonly float[] _pozoRamaSemis = new float[16];

        /// <summary>LA PILA: desenfoque + glow + brillo + soles del conducto
        /// + la técnica de la variante. Se dibuja SIEMPRE que el haz vive
        /// (con shader GPU o sin él — las capas son el equipaje propio).</summary>
        private void DibujarPilaDeCapas()
        {
            Texture2D texRuido2 = DefinitivasLib.TexRuido;
            Texture2D texRemolino2 = DefinitivasLib.TexRemolino;
            if (texRuido2 == null || texRemolino2 == null) return;

            // el eje remuestreado a 64 nodos (los pozos de la casa)
            Vector2[] array3 = _cachePuntos64;
            float[] array4 = _cacheSemis64;
            for (int k = 0; k < 64; k++)
            {
                float num12 = (float)k / 63f;
                array3[k] = _cachePuntos[(int)(num12 * 1499f)] - Main.screenPosition;
                array4[k] = MathF.Max(StripWidth(num12) * 0.5f, 1f);
            }

            float t = Main.GlobalTimeWrappedHourly;
            Color cGrad = VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f);
            Color cGrad2 = VariantesRC2.Gradiente(Variante, ((ModProjectile)this).Projectile.localAI[0] / 60f + 0.25f);

            RayoStrip.AbrirLote();
            try
            {
                // (1) EL DESENFOQUE — el halo difuso del borde.
                RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 1.8f, _cacheSemisA),
                    DefinitivasLib.Alfa(cGrad2, 0.30f * realSize), texRemolino2, 240f, 1.9f, t);
                // (2) EL GLOW — la carne que respira el gradiente.
                RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 1.35f, _cacheSemisB),
                    DefinitivasLib.Alfa(cGrad, 0.50f * realSize), texRuido2, 420f, 1.0f, t);
                // (3) EL BRILLO — la vena central blanca.
                RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.55f, _cacheSemisA),
                    DefinitivasLib.Alfa(Color.White, 0.90f * realSize), texRuido2, 380f, 2.6f, t, 0.36f, 0.64f);
            }
            finally
            {
                RayoStrip.CerrarLote();
            }

            // (4) LA TÉCNICA DE LA VARIANTE — cada haz su monstruo. FUERA
            // del lote de RayoStrip: algunas técnicas administran sus
            // propios lotes (AberracionCromatica abre el suyo; Nube y Bruma
            // manejan VFXCore por dentro) y las de SpriteBatch directo
            // heredan el lote vanilla de entidades (el camino probado de
            // los bolts de la carga).
            switch (Variante)
            {
                case 1:
                    VariantesRC2.Prisma(array3, array4, this, realSize);
                    break;
                case 2:
                    VariantesRC2.Eclipse(array3, array4, this, realSize);
                    break;
                case 3:
                    VariantesRC2.Tormenta(array3, array4, this, realSize);
                    break;
                case 4:
                    VariantesRC2.Solar(array3, array4, this, realSize);
                    break;
                case 5:
                    VariantesRC2.Vortice(array3, array4, this, realSize);
                    break;
                case 6:
                    VariantesRC2.Espectro(array3, array4, this, realSize);
                    break;
                case 8:
                    VariantesRC2.Cuantico(array3, array4, this, realSize);
                    break;
                case 9:
                    VariantesRC2.Nova(array3, array4, this, realSize);
                    break;
            }

            // (5) LOS SOLES DEL CONDUCTO — destellos de 4 puntas corriendo
            // por dentro del haz (aditivo puro, lote VFXCore).
            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                int semilla = ((Entity)((ModProjectile)this).Projectile).whoAmI * 17 + 3;
                for (int i = 0; i < 7; i++)
                {
                    float h = VFXCore.Hash01(semilla, 600 + i, 7);
                    float viaje = (h + ((ModProjectile)this).Projectile.localAI[0] * (0.0022f + 0.0035f * h)) % 1f;
                    int nodo = (int)(viaje * 63f);
                    Vector2 pos = array3[nodo];
                    float vida = (t * 1.4f + h * 3f) % 1f;
                    float arde = MathF.Sin(vida * MathHelper.Pi);
                    float anchoNodo = array4[nodo];
                    HazConcentradoLib.SolRunico4Puntas(pos,
                        MathF.Max(2.5f, anchoNodo * 0.42f) * (0.7f + 0.5f * h), arde);
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
        }

        public Color StripColor(float progress)
        {
            // v6.56.6 — EL COLOR VIAJA CON LA VARIANTE (cada técnica tiene
            // su identidad cromática — ver VariantesRC2.Gradiente).
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0020: Unknown result type (might be due to invalid IL or missing references)
            //IL_0029: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            Color val = VariantesRC2.Gradiente(Variante, (((ModProjectile)this).Projectile.localAI[0] - progress * 100f) / 60f);
            (val).A = 0;
            return val * realSize;
        }

        public float StripWidth(float progress)
        {
            float num = (float)Math.Pow(progress, 0.800000011920929);
            float num2 = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));
            return num * num2 * realSize * 3500f * (1.1f + (float)Math.Cos(Time) * (0.08f - progress * 0.06f));
        }

        private static float[] ArraySemis(float[] perfil, float k, float[] destino)
        {
            // v6.56.1 — escribe en el pozo del llamador (cero alocaciones)
            for (int i = 0; i < perfil.Length; i++)
            {
                destino[i] = perfil[i] * k;
            }
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
