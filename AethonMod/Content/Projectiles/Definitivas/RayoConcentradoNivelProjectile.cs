// LA ESCALERA DEL RAYO (v6.56.0) — el proyectil unificado de 5 niveles (el nivel viaja en ai[1]).
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    public class RayoConcentradoNivelProjectile : ModProjectile
    {
        private static readonly int[] TCarga = new int[3] { 90, 150, 210 };

        private static readonly int[] TLaser = new int[3] { 300, 450, 600 };

        private static readonly float[] TCono = new float[3] { 1500f, 2100f, 2800f };

        private static readonly float[] KAncho = new float[3] { 34f, 52f, 72f };

        private static readonly float[] Escala = new float[3] { 0.7f, 0.85f, 1f };

        private static readonly float[] Intensidad = new float[3] { 0.45f, 0.6f, 0.75f };

        private static readonly float[] SemiAng = new float[3] { 0.03f, 0.036f, 0.042f };

        public float targetRotation;

        public float targetSize;

        public float realSize;

        internal SonidoEnLoop raySound;

        public float volume;

        public float pitch;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public ref float Time => ref ((ModProjectile)this).Projectile.ai[0];

        public ref float Owner => ref ((ModProjectile)this).Projectile.ai[2];

        public ref float NivelRaw => ref ((ModProjectile)this).Projectile.ai[1];

        private int Niv
        {
            get
            {
                int num = (int)NivelRaw;
                if (num < 1)
                {
                    num = 1;
                }
                if (num > 3)
                {
                    num = 3;
                }
                return num - 1;
            }
        }

        private int Carga => TCarga[Niv];

        private int Laser => TLaser[Niv];

        private float Largo => TCono[Niv];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[((ModProjectile)this).Type] = 5000;
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 40;
            ((Entity)((ModProjectile)this).Projectile).height = 40;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.hostile = false;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.manualDirectionChange = true;
            ((ModProjectile)this).Projectile.netImportant = true;
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
            //IL_00d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0477: Unknown result type (might be due to invalid IL or missing references)
            //IL_047c: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b5: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_04c3: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_018b: Unknown result type (might be due to invalid IL or missing references)
            //IL_019f: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d0: Expected O, but got Unknown
            //IL_0539: Unknown result type (might be due to invalid IL or missing references)
            //IL_053e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0567: Unknown result type (might be due to invalid IL or missing references)
            //IL_056c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0574: Unknown result type (might be due to invalid IL or missing references)
            //IL_05c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_05ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_05dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0636: Unknown result type (might be due to invalid IL or missing references)
            //IL_063b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0649: Unknown result type (might be due to invalid IL or missing references)
            //IL_064e: Unknown result type (might be due to invalid IL or missing references)
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
            ((Entity)val).velocity = ((Entity)val).velocity + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * (0f - (0.04f + 0.02f * (float)Niv));
            ((Entity)val).velocity = ((Entity)val).velocity * 0.98f;
            if (!Main.dedServ && Time % 4f == 0f && Time > (float)Carga && Time < (float)(Carga + Laser))
            {
                float lerpValue = Utils.GetLerpValue((float)Carga - (float)Laser * 0.5f, (float)(Carga + Laser), Time, true);
                Main.instance.CameraModifiers.Add((ICameraModifier)new PunchCameraModifier(((Entity)((ModProjectile)this).Projectile).Center, Utils.NextVector2CircularEdge(Main.rand, 1f, 1f), lerpValue * (3.5f + 1.5f * (float)Niv), 12f, 20, 5000f, (string)null));
            }
            float num = (float)Math.Cos((Time - (float)Carga) / (float)Laser * ((float)Math.PI * 2f)) * 2.5f * Escala[Niv] * Utils.GetLerpValue((float)Carga * 0.8f, (float)Carga + (float)Laser * 0.1f, Time, true) * Utils.GetLerpValue((float)(Carga + Laser), (float)Carga + (float)Laser * 0.9f, Time, true);
            if (Main.netMode != 1 && Time % 15f == 0f && Time < (float)Carga + (float)Laser * 0.6f)
            {
                ((Entity)((ModProjectile)this).Projectile).direction = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
                ((ModProjectile)this).Projectile.netUpdate = true;
            }
            // v6.56.1 — LA MIRA EN MP: solo el DUEÑO apunta con SU ratón; el
            // servidor y el resto usan la rotación que viaja por ExtraAI.
            if (Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)
            {
                targetRotation = Utils.AngleLerp(targetRotation, ((Entity)((ModProjectile)this).Projectile).AngleTo(Main.MouseWorld), Utils.GetLerpValue((float)Carga + (float)Laser * 0.1f, (float)Carga, Time, true) * 0.04f + Utils.GetLerpValue((float)Carga + (float)Laser * 0.3f, (float)Carga + (float)Laser * 0.6f, Time, true) * 0.01f);
            }
            targetSize = (float)Math.Pow(Utils.GetLerpValue((float)(Carga - 20), (float)(Carga + 60), Time, true) * Utils.GetLerpValue((float)(Carga + Laser + 60), (float)(Carga + Laser), Time, true), 3.0);
            float num2 = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue((float)Carga * 0.8f, (float)(Carga + 20), Time, true) * Utils.GetLerpValue((float)Carga + (float)Laser * 0.9f, (float)Carga + (float)Laser * 0.5f, Time, true));
            ((ModProjectile)this).Projectile.rotation = targetRotation + num * (float)((Entity)((ModProjectile)this).Projectile).direction * num2;
            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            ((Entity)((ModProjectile)this).Projectile).velocity = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
            ((ModProjectile)this).Projectile.localAI[0] += 1f;
            if (Time == (float)(Carga + Laser + 20))
            {
                SoundStyle item = SoundID.Item34;
                DefinitivasLib.Sonar((item).WithPitchOffset(-0.4f), ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Kick(4 + Niv, 8);
            }
            if (!Main.dedServ && Time > (float)(Carga - 10) && Time < (float)(Carga + Laser))
            {
                Color val2 = DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f);
                float num3 = realSize * (0.55f + 0.15f * (float)Niv);
                Vector2 val3 = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center, (float)(int)(val2).R / 255f * 1.8f * num3, (float)(int)(val2).G / 255f * 1.7f * num3, (float)(int)(val2).B / 255f * 1.5f * num3);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + val3 * (Largo * 0.33f), (float)(int)(val2).R / 255f * 1.4f * num3, (float)(int)(val2).G / 255f * 1.3f * num3, (float)(int)(val2).B / 255f * 1.1f * num3);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + val3 * (Largo * 0.66f), (float)(int)(val2).R / 255f * 1f * num3, (float)(int)(val2).G / 255f * 0.9f * num3, (float)(int)(val2).B / 255f * 0.8f * num3);
            }
            Time += 1f;
            ((ModProjectile)this).Projectile.timeLeft = 10;
            HandleSound();
            if (Time > (float)(Carga + Laser + 50))
            {
                raySound?.StopSound();
                ((ModProjectile)this).Projectile.Kill();
            }
        }

        public void HandleSound()
        {
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c7: Expected O, but got Unknown
            volume = (0.55f + 0.15f * (float)Niv) * Utils.GetLerpValue(0f, 0.05f, realSize, true);
            pitch = MathHelper.SmoothStep(0.3f, 1f, Utils.GetLerpValue((float)(Carga - 40), (float)(Carga + 50), Time, true)) * Utils.GetLerpValue((float)(Carga + Laser + 50), (float)(Carga + Laser), Time, true) - 0.85f;
            if (raySound == null)
            {
                SoundStyle item = SoundID.Item122;
                (item).MaxInstances = 0;
                raySound = new SonidoEnLoop(item, (Func<bool>)new ProjectileAudioTracker(((ModProjectile)this).Projectile).IsActiveAndInGame);
            }
            raySound.PlaySound(() => ((Entity)((ModProjectile)this).Projectile).Center, () => volume, () => pitch);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_0092: Unknown result type (might be due to invalid IL or missing references)
            //IL_0099: Unknown result type (might be due to invalid IL or missing references)
            if (Time > (float)(Carga + 20) && Time < (float)(Carga + Laser + 10))
            {
                float num = (float)Math.Pow(Utils.GetLerpValue((float)(Carga - 20), (float)(Carga + 40), Time, true) * Utils.GetLerpValue((float)(Carga + Laser + 60), (float)(Carga + Laser), Time, true), 3.0);
                return DefinitivasLib.Cono(targetHitbox, ((Entity)((ModProjectile)this).Projectile).Center, Largo * (0.5f + num * 0.5f), ((ModProjectile)this).Projectile.rotation, SemiAng[Niv] * Math.Max(num, 0.02f));
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_003d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0042: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0100: Unknown result type (might be due to invalid IL or missing references)
            //IL_010a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0115: Unknown result type (might be due to invalid IL or missing references)
            //IL_011f: Unknown result type (might be due to invalid IL or missing references)
            //IL_013a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0146: Unknown result type (might be due to invalid IL or missing references)
            //IL_0153: Unknown result type (might be due to invalid IL or missing references)
            //IL_015e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0168: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_020c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0216: Unknown result type (might be due to invalid IL or missing references)
            //IL_021b: Unknown result type (might be due to invalid IL or missing references)
            //IL_022e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0233: Unknown result type (might be due to invalid IL or missing references)
            //IL_0236: Unknown result type (might be due to invalid IL or missing references)
            //IL_025b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0268: Unknown result type (might be due to invalid IL or missing references)
            //IL_0280: Unknown result type (might be due to invalid IL or missing references)
            //IL_028a: Unknown result type (might be due to invalid IL or missing references)
            //IL_028f: Unknown result type (might be due to invalid IL or missing references)
            //IL_029d: Unknown result type (might be due to invalid IL or missing references)
            //IL_02aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0307: Unknown result type (might be due to invalid IL or missing references)
            //IL_030c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0320: Unknown result type (might be due to invalid IL or missing references)
            //IL_061a: Unknown result type (might be due to invalid IL or missing references)
            //IL_061f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0516: Unknown result type (might be due to invalid IL or missing references)
            //IL_0522: Unknown result type (might be due to invalid IL or missing references)
            //IL_0548: Unknown result type (might be due to invalid IL or missing references)
            //IL_055d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0567: Unknown result type (might be due to invalid IL or missing references)
            //IL_0576: Unknown result type (might be due to invalid IL or missing references)
            //IL_057c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0587: Unknown result type (might be due to invalid IL or missing references)
            //IL_041e: Unknown result type (might be due to invalid IL or missing references)
            //IL_042e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0435: Unknown result type (might be due to invalid IL or missing references)
            //IL_043a: Unknown result type (might be due to invalid IL or missing references)
            //IL_043f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0441: Unknown result type (might be due to invalid IL or missing references)
            //IL_045d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0464: Unknown result type (might be due to invalid IL or missing references)
            //IL_0469: Unknown result type (might be due to invalid IL or missing references)
            //IL_046e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0475: Unknown result type (might be due to invalid IL or missing references)
            //IL_0477: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_063b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0640: Unknown result type (might be due to invalid IL or missing references)
            //IL_065a: Unknown result type (might be due to invalid IL or missing references)
            //IL_065f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0664: Unknown result type (might be due to invalid IL or missing references)
            //IL_06c1: Unknown result type (might be due to invalid IL or missing references)
            Texture2D softGlow = VFXCore.SoftGlow;
            Texture2D glowOrb = VFXCore.GlowOrb;
            Texture2D ring = VFXCore.Ring;
            if (softGlow == null || glowOrb == null || ring == null)
            {
                return false;
            }
            float num = Escala[Niv];
            Color c = DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f);
            (c).A = 0;
            float num2 = Utils.GetLerpValue((float)Carga * 0.25f, (float)Carga, Time, true) * Utils.GetLerpValue((float)(Carga + Laser + 50), (float)(Carga + Laser + 20), Time, true);
            float num3 = Utils.GetLerpValue(0f, (float)Carga, Time, true) * Utils.GetLerpValue((float)(Carga + 10), (float)(Carga - 30), Time, true);
            Vector2 val = ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition;
            Main.EntitySpriteDraw(softGlow, val, (Rectangle?)null, DefinitivasLib.Alfa(c, 0.55f * num2), 0f, Utils.Size(softGlow) * 0.5f, 2.2f * num * num2, (SpriteEffects)0, 0f);
            Main.EntitySpriteDraw(glowOrb, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * num2), 0f, Utils.Size(glowOrb) * 0.5f, 1.05f * num * num2, (SpriteEffects)0, 0f);
            float num4 = MathF.Pow(num2, 0.8f);
            float num5 = MathHelper.Lerp(150f, 56f, num4) * num * (1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f));
            Vector2 val2 = VFXCore.RingQuadSize(num5);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + 0.1f), 0.55f * num2), Main.GlobalTimeWrappedHourly * 0.7f, Utils.Size(ring) * 0.5f, val2, (SpriteEffects)0, 0f);
            Vector2 val3 = VFXCore.RingQuadSize(num5 * 1.38f);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + 0.3f), 0.3f * num2), (0f - Main.GlobalTimeWrappedHourly) * 0.5f + 2.1f, Utils.Size(ring) * 0.5f, val3, (SpriteEffects)0, 0f);
            Main.EntitySpriteDraw(softGlow, val + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * (Largo * 0.26f) * num3, (Rectangle?)Utils.Frame(softGlow, 1, 1, 0, 0, 0, 0), DefinitivasLib.Alfa(c, 0.35f * num3), ((ModProjectile)this).Projectile.rotation, Utils.Size(softGlow) * new Vector2(0.5f, 0.5f), new Vector2(0.06f, 5.2f * num * num3), (SpriteEffects)0, 0f);
            if (Time < (float)Carga && Time % 14f < 3f)
            {
                float lerpValue = Utils.GetLerpValue(0f, (float)Carga, Time, true);
                int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) & 0x3FF;
                int num6 = 1 + (int)(lerpValue * 3.99f);
                float num7 = 0.8f + 0.1f * (float)Niv;
                for (int i = 0; i < num6; i++)
                {
                    int seed = (int)(Time * 0.017f) * 131 + i * 297 + Niv * 977;
                    float num8 = VFXCore.Hash01(seed, i * 7 + 1, 3) * ((float)Math.PI * 2f);
                    float num9 = (10f + 6f * lerpValue) * num7;
                    float num10 = num9 + (26f + 34f * lerpValue) * num7;
                    Vector2 start = val + new Vector2(MathF.Cos(num8), MathF.Sin(num8)) * num9;
                    Vector2 end = val + new Vector2(MathF.Cos(num8 + 0.5f), MathF.Sin(num8 + 0.5f)) * num10;
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 1.6f + 0.3f * (float)Niv, DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f + (float)i * 0.13f), DefinitivasLib.BlancoCalido, 0.35f + 0.45f * lerpValue, 5, 12f);
                }
            }
            if (Time > (float)Carga && Time < (float)(Carga + 8) && VFXCore.DestelloFinal != null)
            {
                Main.EntitySpriteDraw(VFXCore.DestelloFinal, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.9f * (1f - (Time - (float)Carga) / 8f)), ((ModProjectile)this).Projectile.rotation, Utils.Size(VFXCore.DestelloFinal) * 0.5f, new Vector2(1.6f, 1.6f) * num * realSize, (SpriteEffects)0, 0f);
            }
            if (Time >= (float)(Carga - 20))
            {
                float num11 = Utils.GetLerpValue((float)(Carga - 30), (float)(Carga + 70), Time, true) * Utils.GetLerpValue((float)(Carga + Laser + 60), (float)(Carga + Laser), Time, true);
                Vector2[] array = (Vector2[])(object)new Vector2[24];
                float[] array2 = new float[24];
                Vector2 val4 = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
                for (int j = 0; j < 24; j++)
                {
                    float num12 = (float)j / 23f;
                    array[j] = ((Entity)((ModProjectile)this).Projectile).Center + val4 * (Largo * num12 * (0.5f + num11 * 0.5f));
                    array2[j] = MathF.Max(DefinitivasLib.AnchoCono(num12, Time, KAncho[Niv]) * num * realSize, 0.5f);
                }
                DefinitivasLib.HazFusionEspina(array, array2, DefinitivasLib.GradienteJuicio(((ModProjectile)this).Projectile.localAI[0] / 60f), Intensidad[Niv] * num11, Main.GlobalTimeWrappedHourly, (int)Owner * 131 + Niv * 977 + 17, arcoiris: true, 1f, Niv == 2);
            }
            VFXCore.RestaurarLoteVanilla();
            return false;
        }
    }
}
