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

        public float volume;

        public float pitch;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public ref float Time => ref ((ModProjectile)this).Projectile.ai[0];

        public ref float Modo => ref ((ModProjectile)this).Projectile.ai[1];

        public ref float Owner => ref ((ModProjectile)this).Projectile.ai[2];

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
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            Player val = Main.player[(int)Owner];
            if (val == null || !((Entity)val).active || val.dead)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            ((Entity)val).direction = Math.Sign(Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation).X);
            val.itemLocation = val.MountedCenter;
            ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + new Vector2((float)(13 * ((Entity)val).direction), -20f);
            ((Entity)val).velocity = ((Entity)val).velocity + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * -0.1f;
            ((Entity)val).velocity = ((Entity)val).velocity * 0.98f;
            if (!Main.dedServ && Time % 4f == 0f && Time > (float)ChargeTime && Time < (float)(ChargeTime + LaserDuration))
            {
                float lerpValue = Utils.GetLerpValue((float)ChargeTime - (float)LaserDuration * 0.5f, (float)(ChargeTime + LaserDuration), Time, true);
                Main.instance.CameraModifiers.Add((ICameraModifier)new PunchCameraModifier(((Entity)((ModProjectile)this).Projectile).Center, Utils.NextVector2CircularEdge(Main.rand, 1f, 1f), lerpValue * 8f, 12f, 20, 5000f, (string)null));
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
                Color val2 = DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f);
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
            volume = 1.3f * Utils.GetLerpValue(0f, 0.05f, realSize, true);
            pitch = MathHelper.SmoothStep(0.5f, 1.2f, Utils.GetLerpValue((float)(ChargeTime - 45), (float)(ChargeTime + 60), Time, true)) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 60), (float)(ChargeTime + LaserDuration), Time, true) - 1f;
            if (raySound == null)
            {
                SoundStyle estilo = new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop", (SoundType)0);
                (estilo).MaxInstances = 0;
                raySound = new SonidoEnLoop(estilo, (Func<bool>)new ProjectileAudioTracker(((ModProjectile)this).Projectile).IsActiveAndInGame);
            }
            raySound.PlaySound(() => ((Entity)((ModProjectile)this).Projectile).Center, () => volume, () => pitch);
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
            Color c = DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f);
            (c).A = 0;
            float num = Utils.GetLerpValue(50f, (float)ChargeTime, Time, true) * Utils.GetLerpValue((float)(ChargeTime + LaserDuration + 50), (float)(ChargeTime + LaserDuration + 20), Time, true);
            // v6.56.1 — LA AGUJA RECUPERADA: localAI[1] jamás se asignaba
            // (la aguja de puntería llevaba alfa 0 desde la v6.56.0). El
            // gemelo RayoConcentradoNivel usa Time aquí — esta era la idea.
            float num2 = Utils.GetLerpValue(0f, (float)ChargeTime, Time, true) * Utils.GetLerpValue((float)(ChargeTime + 10), (float)(ChargeTime - 30), Time, true);
            float num3 = ((Entity)((ModProjectile)this).Projectile).Center.X - Main.screenPosition.X;
            float num4 = ((Entity)((ModProjectile)this).Projectile).Center.Y - Main.screenPosition.Y;
            Vector2 val = new Vector2(num3, num4);
            Main.EntitySpriteDraw(softGlow, val, (Rectangle?)null, DefinitivasLib.Alfa(c, 0.55f * num), 0f, Utils.Size(softGlow) * 0.5f, 2.2f * num, (SpriteEffects)0, 0f);
            Main.EntitySpriteDraw(glowOrb, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * num), 0f, Utils.Size(glowOrb) * 0.5f, 1.05f * num, (SpriteEffects)0, 0f);
            float num5 = MathF.Pow(num, 0.8f);
            float num6 = MathHelper.Lerp(150f, 56f, num5) * (1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f));
            Vector2 val2 = VFXCore.RingQuadSize(num6);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + 0.1f), 0.55f * num), Main.GlobalTimeWrappedHourly * 0.7f, Utils.Size(ring) * 0.5f, val2, (SpriteEffects)0, 0f);
            Vector2 val3 = VFXCore.RingQuadSize(num6 * 1.38f);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + 0.3f), 0.3f * num), (0f - Main.GlobalTimeWrappedHourly) * 0.5f + 2.1f, Utils.Size(ring) * 0.5f, val3, (SpriteEffects)0, 0f);
            Main.EntitySpriteDraw(softGlow, val + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * 900f * num2, (Rectangle?)Utils.Frame(softGlow, 1, 1, 0, 0, 0, 0), DefinitivasLib.Alfa(c, 0.35f * num2), ((ModProjectile)this).Projectile.rotation, Utils.Size(softGlow) * new Vector2(0.5f, 0.5f), new Vector2(0.06f, 5.2f * num2), (SpriteEffects)0, 0f);
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
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 2.2f, DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + (float)i * 0.13f), DefinitivasLib.BlancoCalido, 0.35f + 0.45f * lerpValue, 5, 12f);
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
                for (int j = 0; j < 1500; j++)
                {
                    array[j] = ((Entity)((ModProjectile)this).Projectile).Center + Utils.RotatedBy(new Vector2(3500f * ((float)j / 1500f) * (0.5f + num11 * 0.5f), 0f), (double)((ModProjectile)this).Projectile.rotation, default(Vector2));
                    array2[j] = ((ModProjectile)this).Projectile.rotation;
                }
                VertexStrip val4 = _strip;
                val4.PrepareStripWithProceduralPadding(array, array2, new VertexStrip.StripColorFunction(StripColor), new VertexStrip.StripHalfWidthFunction(StripWidth), -Main.screenPosition, true, true);
                Effect val5 = ObtenerShader();
                if (val5 != null)
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
                            RayoStrip.FajaTexturadaEnLote(array3, array4, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f), 0.6f * realSize), texRuido2, 420f, 1f, Main.GlobalTimeWrappedHourly);
                        }
                        if (texRemolino2 != null)
                        {
                            RayoStrip.FajaTexturadaEnLote(array3, ArraySemis(array4, 0.7f, _cacheSemisA), DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + 0.2f), 0.45f * realSize), texRemolino2, 240f, 1.9f, Main.GlobalTimeWrappedHourly);
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
            VFXCore.RestaurarLoteVanilla();
            return false;
        }

        public Color StripColor(float progress)
        {
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0020: Unknown result type (might be due to invalid IL or missing references)
            //IL_0029: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            Color val = DefinitivasLib.GradienteJuicio((((ModProjectile)this).Projectile.localAI[0] - progress * 100f) / 60f);
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
