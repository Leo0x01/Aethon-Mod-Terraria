using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using System.Runtime.CompilerServices;

namespace AethonMod.Content.Projectiles
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class PrimerTrazoFajo : ModProjectile
    {
        [CompilerGenerated]
        private static class _003C_003EO
        {
            public static Utils.TileActionAttempt _003C0_003E__CutTiles;
        }

        private float _anchoMax = 40f;

        private float _intensidad = 1f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public int Indice => (int)((ModProjectile)this).Projectile.ai[0];

        public int PadreWho => (int)((ModProjectile)this).Projectile.ai[1];

        public float Largo
        {
            get
            {
                return ((ModProjectile)this).Projectile.localAI[1];
            }
            set
            {
                ((ModProjectile)this).Projectile.localAI[1] = value;
            }
        }

        public bool ChoquePared => ((ModProjectile)this).Projectile.localAI[0] >= 1f;

        private static float Reloj => Main.GlobalTimeWrappedHourly * 2f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[((ModProjectile)this).Projectile.type] = 2600;
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 22;
            ((Entity)((ModProjectile)this).Projectile).height = 22;
            ((ModProjectile)this).Projectile.aiStyle = -1;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.ignoreWater = true;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 10;
            ((ModProjectile)this).Projectile.timeLeft = 8;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            //IL_00f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0102: Unknown result type (might be due to invalid IL or missing references)
            //IL_0107: Unknown result type (might be due to invalid IL or missing references)
            //IL_010e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0113: Unknown result type (might be due to invalid IL or missing references)
            //IL_0126: Unknown result type (might be due to invalid IL or missing references)
            //IL_012d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0132: Unknown result type (might be due to invalid IL or missing references)
            //IL_0137: Unknown result type (might be due to invalid IL or missing references)
            //IL_0139: Unknown result type (might be due to invalid IL or missing references)
            //IL_0153: Unknown result type (might be due to invalid IL or missing references)
            //IL_0159: Unknown result type (might be due to invalid IL or missing references)
            //IL_015b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0160: Unknown result type (might be due to invalid IL or missing references)
            //IL_0165: Unknown result type (might be due to invalid IL or missing references)
            //IL_016a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0172: Unknown result type (might be due to invalid IL or missing references)
            //IL_017f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0181: Unknown result type (might be due to invalid IL or missing references)
            //IL_018e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0190: Unknown result type (might be due to invalid IL or missing references)
            Projectile val = Main.projectile[PadreWho];
            if (val == null || !((Entity)val).active || val.type != ModContent.ProjectileType<PrimerTrazoHeld>())
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            float num = MathHelper.Clamp((val.ai[0] - 1f) / 179f, 0f, 1f);
            bool flag = val.ai[1] >= 1f;
            float num2 = (flag ? MathHelper.Clamp(1f - (val.ai[2] - 6f) / 8f, 0f, 1f) : 1f);
            ((ModProjectile)this).Projectile.timeLeft = 4;
            if (num2 <= 0f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            float x = Main.GlobalTimeWrappedHourly * 0.55f + (float)Indice * ((float)Math.PI * 2f) / 5f;
            float num3 = MathHelper.Lerp(20f, 4f, num);
            Vector2 val2 = Utils.SafeNormalize(((Entity)val).velocity, Vector2.UnitX);
            Vector2 val3 = ((Entity)val).Center + val2 * 14f + new Vector2(MathF.Cos(x), MathF.Sin(x)) * num3;
            Vector2 val4 = Utils.SafeNormalize(Utils.RotatedBy(val2, (double)(MathF.Sin(x) * ((float)Math.PI / 6f) * (1f - num)), default(Vector2)), Vector2.UnitX);
            ((Entity)((ModProjectile)this).Projectile).Center = val3;
            ((Entity)((ModProjectile)this).Projectile).velocity = val4;
            float[] array = new float[3];
            Collision.LaserScan(val3, val4, 13.200001f, 2400f, array);
            float num4 = (array[0] + array[1] + array[2]) / 3f;
            if (Largo <= 0f)
            {
                Largo = num4;
            }
            Largo = MathHelper.Lerp(Largo, num4, 0.75f);
            ((ModProjectile)this).Projectile.localAI[0] = ((num4 < 2392f) ? 1f : 0f);
            int num5 = ((((ModProjectile)this).Projectile.originalDamage <= 0) ? ((ModProjectile)this).Projectile.damage : ((ModProjectile)this).Projectile.originalDamage);
            ((ModProjectile)this).Projectile.damage = Math.Max(1, (int)((float)num5 * (1f + 2f * num) * num2));
            ((ModProjectile)this).Projectile.localAI[2] = MathHelper.Clamp(0.35f + 0.65f * num + (flag ? 0f : 0f), 0f, 1f) * num2;
        }

        public override void CutTiles()
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_001c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0027: Unknown result type (might be due to invalid IL or missing references)
            //IL_002c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_004b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0051: Expected O, but got Unknown
            Vector2 center = ((Entity)((ModProjectile)this).Projectile).Center;
            Vector2 val = ((Entity)((ModProjectile)this).Projectile).Center + ((Entity)((ModProjectile)this).Projectile).velocity * Largo;
            object obj = _003C_003EO._003C0_003E__CutTiles;
            if (obj == null)
            {
                Utils.TileActionAttempt val2 = DelegateMethods.CutTiles;
                _003C_003EO._003C0_003E__CutTiles = val2;
                obj = (object)val2;
            }
            Utils.PlotTileLine(center, val, 10f, (Utils.TileActionAttempt)obj);
        }

        private float Hue()
        {
            return ((float)Indice / 5f + Main.GlobalTimeWrappedHourly * 0.07f) % 1f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_000c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Unknown result type (might be due to invalid IL or missing references)
            //IL_0022: Unknown result type (might be due to invalid IL or missing references)
            //IL_0027: Unknown result type (might be due to invalid IL or missing references)
            //IL_002c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0082: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_0088: Unknown result type (might be due to invalid IL or missing references)
            //IL_0089: Unknown result type (might be due to invalid IL or missing references)
            //IL_0094: Unknown result type (might be due to invalid IL or missing references)
            //IL_0099: Unknown result type (might be due to invalid IL or missing references)
            float num = 0f;
            Vector2 val = ((Entity)((ModProjectile)this).Projectile).Center + ((Entity)((ModProjectile)this).Projectile).velocity * Largo;
            Projectile val2 = Main.projectile[PadreWho];
            float num2 = ((val2 != null && ((Entity)val2).active) ? MathHelper.Clamp((val2.ai[0] - 1f) / 179f, 0f, 1f) : 1f);
            float num3 = MathHelper.Lerp(14f, 90f, num2);
            return Collision.CheckAABBvLineCollision(Utils.TopLeft(targetHitbox), Utils.Size(targetHitbox), ((Entity)((ModProjectile)this).Projectile).Center, val, num3, ref num);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Largo);
            writer.Write(ChoquePared);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            try
            {
                Largo = reader.ReadSingle();
                ((ModProjectile)this).Projectile.localAI[0] = (reader.ReadBoolean() ? 1f : 0f);
            }
            catch
            {
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_00bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00db: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ea: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0104: Unknown result type (might be due to invalid IL or missing references)
            //IL_010e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0130: Unknown result type (might be due to invalid IL or missing references)
            //IL_0135: Unknown result type (might be due to invalid IL or missing references)
            //IL_013a: Unknown result type (might be due to invalid IL or missing references)
            //IL_013f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0195: Unknown result type (might be due to invalid IL or missing references)
            //IL_019a: Unknown result type (might be due to invalid IL or missing references)
            //IL_01be: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0288: Unknown result type (might be due to invalid IL or missing references)
            //IL_0296: Unknown result type (might be due to invalid IL or missing references)
            //IL_029d: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ef: Unknown result type (might be due to invalid IL or missing references)
            //IL_0300: Unknown result type (might be due to invalid IL or missing references)
            //IL_0305: Unknown result type (might be due to invalid IL or missing references)
            //IL_0350: Unknown result type (might be due to invalid IL or missing references)
            //IL_0377: Unknown result type (might be due to invalid IL or missing references)
            //IL_0406: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_03df: Unknown result type (might be due to invalid IL or missing references)
            //IL_0460: Unknown result type (might be due to invalid IL or missing references)
            //IL_0465: Unknown result type (might be due to invalid IL or missing references)
            //IL_046a: Unknown result type (might be due to invalid IL or missing references)
            //IL_046f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0477: Unknown result type (might be due to invalid IL or missing references)
            //IL_047c: Unknown result type (might be due to invalid IL or missing references)
            //IL_048a: Unknown result type (might be due to invalid IL or missing references)
            //IL_048f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0494: Unknown result type (might be due to invalid IL or missing references)
            //IL_04af: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_04c5: Unknown result type (might be due to invalid IL or missing references)
            //IL_04d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_04e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_050e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0510: Unknown result type (might be due to invalid IL or missing references)
            //IL_0515: Unknown result type (might be due to invalid IL or missing references)
            //IL_052f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0540: Unknown result type (might be due to invalid IL or missing references)
            //IL_054e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0558: Unknown result type (might be due to invalid IL or missing references)
            if (Main.netMode == 2)
            {
                return false;
            }
            if (Largo < 16f)
            {
                return false;
            }
            Projectile val = Main.projectile[PadreWho];
            float num = ((val != null && ((Entity)val).active) ? MathHelper.Clamp((val.ai[0] - 1f) / 179f, 0f, 1f) : 1f);
            float num2 = ((((ModProjectile)this).Projectile.localAI[2] <= 0f) ? 1f : ((ModProjectile)this).Projectile.localAI[2]);
            _intensidad = (0.45f + 0.55f * num) * num2;
            _anchoMax = MathHelper.Lerp(34f, 250f, num);
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            Color c = ColorPrisma.ValueAt(Reloj);
            Texture2D softGlow = VFXCore.SoftGlow;
            Main.EntitySpriteDraw(softGlow, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, (Rectangle?)null, StormLib.Tint(c, 0.5f * _intensidad), 0f, Utils.Size(softGlow) * 0.5f, 1.5f * _intensidad, (SpriteEffects)0, 0f);
            Vector2 val2 = Utils.SafeNormalize(((Entity)((ModProjectile)this).Projectile).velocity, Vector2.UnitX);
            Vector2[] array = (Vector2[])(object)new Vector2[48];
            float[] array2 = new float[48];
            float[] array3 = new float[48];
            float[] array4 = new float[48];
            Color[] array5 = (Color[])(object)new Color[48];
            Color[] array6 = (Color[])(object)new Color[48];
            Color[] array7 = (Color[])(object)new Color[48];
            Color val3 = Main.hslToRgb(Hue(), 1f, 0.72f, byte.MaxValue);
            float num3 = num * num;
            for (int i = 0; i < 48; i++)
            {
                float num4 = (float)i / 47f;
                array[i] = ((Entity)((ModProjectile)this).Projectile).Center + val2 * (Largo * num4) - Main.screenPosition;
                float num5 = (float)Math.Pow(num4, 0.800000011920929);
                float num6 = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, num4, true));
                float num7 = 1.1f + (float)Math.Cos(Reloj * 2f) * (0.08f - num4 * 0.06f);
                float num8 = (array2[i] = _anchoMax * 0.5f * num5 * num6 * num7 * _intensidad);
                array3[i] = num8 * 2.3f;
                array4[i] = MathF.Max(num8 * 0.34f, 1.2f);
                Color c2 = Color.Lerp(val3, ColorPrisma.ALoLargo(Reloj, num4), num3);
                array5[i] = StormLib.Tint(c2, 0.6f * _intensidad);
                array6[i] = StormLib.Tint(c2, 0.2f * _intensidad);
                array7[i] = StormLib.Tint(new Color(255, 252, 245), 0.85f * _intensidad);
            }
            Texture2D texRuido = DefinitivasLib.TexRuido;
            Texture2D texRuido2 = DefinitivasLib.TexRuido;
            Texture2D texRemolino = DefinitivasLib.TexRemolino;
            Texture2D texRuido3 = DefinitivasLib.TexRuido;
            if (texRuido == null || texRuido2 == null)
            {
                return false;
            }
            VFXCore.CerrarLoteSiAbierto();
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(array, array3, array6, Color.White, texRuido, 560f, 0.9f, globalTimeWrappedHourly, 0.03f, 0.97f);
                RayoStrip.FajaTexturadaEnLote(array, array2, array5, Color.White, texRuido2, 420f, 1.3f, globalTimeWrappedHourly);
                if (texRemolino != null)
                {
                    float[] array8 = new float[array2.Length];
                    for (int j = 0; j < array8.Length; j++)
                    {
                        array8[j] = array2[j] * 0.8f;
                    }
                    RayoStrip.FajaTexturadaEnLote(array, array8, StormLib.Tint(Color.White, 0.35f * _intensidad), texRemolino, 260f, 1.9f, globalTimeWrappedHourly);
                }
                RayoStrip.FajaTexturadaEnLote(array, array4, array7, Color.White, texRuido3, 420f, 2.1f, globalTimeWrappedHourly, 0.36f, 0.64f);
            }
            finally
            {
                RayoStrip.CerrarLote();
            }
            if (ChoquePared && Largo > 30f && _intensidad > 0.15f)
            {
                Vector2 val4 = Utils.SafeNormalize(((Entity)((ModProjectile)this).Projectile).velocity, Vector2.UnitX);
                Vector2 val5 = ((Entity)((ModProjectile)this).Projectile).Center + val4 * (Largo - 14f);
                Texture2D glowOrb = VFXCore.GlowOrb;
                Texture2D val6 = DefinitivasLib.Tex("AethonMod/Content/Effects/SparkleStar");
                if (glowOrb != null)
                {
                    Main.EntitySpriteDraw(glowOrb, val5 - Main.screenPosition, (Rectangle?)null, StormLib.Tint(Color.White, 0.55f * _intensidad), 0f, Utils.Size(glowOrb) * 0.5f, 1.1f * _intensidad, (SpriteEffects)0, 0f);
                }
                if (val6 != null)
                {
                    Main.EntitySpriteDraw(val6, val5 - Main.screenPosition, (Rectangle?)null, StormLib.Tint(ColorPrisma.ValueAt(Reloj + 0.1f), 0.8f * _intensidad), globalTimeWrappedHourly * 3f, Utils.Size(val6) * 0.5f, 1.3f * _intensidad, (SpriteEffects)0, 0f);
                }
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
