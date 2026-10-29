// LA PLUMA DEL VEREDICTO (v6.56.0) — el sello glifo a glifo que detona en 12 lanzas.
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    public class LanzaVeredictoProjectile : ModProjectile
    {
        private int _semilla;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 20;
            ((Entity)((ModProjectile)this).Projectile).height = 20;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = 2;
            ((ModProjectile)this).Projectile.timeLeft = 42;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            //IL_0062: Unknown result type (might be due to invalid IL or missing references)
            //IL_008b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            //IL_0097: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            if (_semilla == 0)
            {
                _semilla = (int)((ModProjectile)this).Projectile.ai[0];
            }
            if (((ModProjectile)this).Projectile.ai[2] > 120f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity);
            Color val = DefinitivasLib.GradienteJuicio(VFXCore.Hash01(_semilla, 5, 9) + Main.GlobalTimeWrappedHourly * 0.12f);
            Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center, (float)(int)(val).R / 255f * 0.8f, (float)(int)(val).G / 255f * 0.8f, (float)(int)(val).B / 255f * 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_004f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0054: Unknown result type (might be due to invalid IL or missing references)
            //IL_0079: Unknown result type (might be due to invalid IL or missing references)
            //IL_007e: Unknown result type (might be due to invalid IL or missing references)
            //IL_006c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_008a: Unknown result type (might be due to invalid IL or missing references)
            //IL_008f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0094: Unknown result type (might be due to invalid IL or missing references)
            //IL_0099: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0102: Unknown result type (might be due to invalid IL or missing references)
            //IL_012a: Unknown result type (might be due to invalid IL or missing references)
            //IL_012c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0132: Unknown result type (might be due to invalid IL or missing references)
            //IL_0137: Unknown result type (might be due to invalid IL or missing references)
            //IL_0141: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            float num = MathHelper.Clamp((float)((ModProjectile)this).Projectile.timeLeft / 12f, 0f, 1f);
            Color color = DefinitivasLib.GradienteJuicio(VFXCore.Hash01(_semilla, 5, 9) + globalTimeWrappedHourly * 0.12f);
            Vector2 val = (((((Entity)((ModProjectile)this).Projectile).velocity).LengthSquared() > 0.01f) ? Vector2.Normalize(((Entity)((ModProjectile)this).Projectile).velocity) : Vector2.UnitX);
            Vector2 val2 = ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition;
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                    LumenLib.LanceTrail(Main.spriteBatch, val2, val, 78f, 7f, color, 0.8f * num, 6, 20f);
                    LumenLib.Lance(Main.spriteBatch, val2, val, 86f, 9f, color, num);
                    if (VFXCore.Hash01(_semilla, 31, (int)(globalTimeWrappedHourly * 8f)) > 0.35f)
                    {
                        LumenLib.Flare(Main.spriteBatch, val2 + val * 40f, 34f, DefinitivasLib.BlancoCalido, 0.6f * num, globalTimeWrappedHourly * 1.7f);
                    }
                    Main.spriteBatch.End();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }

    public class PlumaDelVeredictoProjectile : ModProjectile
    {
        private const int TConstruccion = 45;

        private const int TArdiendo = 120;

        private const int TReavivada = 105;

        private const int TVida = 190;

        private const float RadioSello = 120f;

        private const int NRunas = 8;

        private int _semilla;

        private bool _detono;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 20;
            ((Entity)((ModProjectile)this).Projectile).height = 20;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 190;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 60;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_0104: Unknown result type (might be due to invalid IL or missing references)
            //IL_0109: Unknown result type (might be due to invalid IL or missing references)
            //IL_0077: Unknown result type (might be due to invalid IL or missing references)
            //IL_007d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0082: Unknown result type (might be due to invalid IL or missing references)
            //IL_0087: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00af: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_012b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0130: Unknown result type (might be due to invalid IL or missing references)
            //IL_0139: Unknown result type (might be due to invalid IL or missing references)
            //IL_0144: Unknown result type (might be due to invalid IL or missing references)
            //IL_015a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0182: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_0203: Unknown result type (might be due to invalid IL or missing references)
            //IL_020c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0217: Unknown result type (might be due to invalid IL or missing references)
            //IL_022d: Unknown result type (might be due to invalid IL or missing references)
            //IL_024d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0261: Unknown result type (might be due to invalid IL or missing references)
            //IL_0273: Unknown result type (might be due to invalid IL or missing references)
            //IL_01aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0304: Unknown result type (might be due to invalid IL or missing references)
            //IL_0309: Unknown result type (might be due to invalid IL or missing references)
            //IL_0310: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            if (_semilla == 0)
            {
                _semilla = ((Entity)((ModProjectile)this).Projectile).whoAmI * 61 + 7;
            }
            if (num > 1200f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            // v6.56.1 — el sello lo ancla el DUEÑO con su ratón (sync al resto).
            if (num < 2f && Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)
            {
                Vector2 val2 = Main.MouseWorld - val.MountedCenter;
                if ((val2).LengthSquared() < 1f)
                {
                    val2 = new Vector2((float)((Entity)val).direction, 0f);
                }
                Vector2 val3 = Vector2.Normalize(val2);
                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + val3 * 96f;
                ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
                ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(val3);
                ((ModProjectile)this).Projectile.netUpdate = true;
            }
            ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
            SoundStyle val4;
            if (Main.netMode != 2 && !Main.dedServ)
            {
                if (num == 2f)
                {
                    val4 = SoundID.Item74;
                    DefinitivasLib.Sonar((val4).WithPitchOffset(-0.45f), ((Entity)((ModProjectile)this).Projectile).Center);
                    AudioLib.Sonar(AudioLib.Familia.Runico, "carga", ((Entity)((ModProjectile)this).Projectile).Center, 0.85f);
                }
                if (num == 30f)
                {
                    AudioLib.Sonar(AudioLib.Familia.Runico, "carga", ((Entity)((ModProjectile)this).Projectile).Center, 0.7f, 0.15f);
                }
                if (num == 168f)
                {
                    AudioLib.Sonar(AudioLib.Familia.Runico, "muerte", ((Entity)((ModProjectile)this).Projectile).Center, 0.7f, -0.2f);
                }
            }
            if (!(num >= 45f) || _detono)
            {
                return;
            }
            _detono = true;
            if (Main.netMode != 2 && !Main.dedServ)
            {
                val4 = SoundID.Item122;
                val4 = (val4).WithPitchOffset(-0.45f);
                DefinitivasLib.Sonar((val4).WithVolumeScale(1.25f), ((Entity)((ModProjectile)this).Projectile).Center);
                AudioLib.Sonar(AudioLib.Familia.Runico, "impacto", ((Entity)((ModProjectile)this).Projectile).Center);
                AudioLib.Sonar(AudioLib.Familia.Electrica, "apertura", ((Entity)((ModProjectile)this).Projectile).Center, 0.9f);
                OndaLib.Flash(DefinitivasLib.BlancoCalido, 0.32f, 12, ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Kick(9f);
            }
            if (Main.netMode != 1)
            {
                Vector2 val5 = default(Vector2);
                for (int i = 0; i < 12; i++)
                {
                    float num2 = ((ModProjectile)this).Projectile.rotation + MathHelper.Lerp(-0.55f, 0.55f, (float)i / 11f);
                    val5 = new Vector2(MathF.Cos(num2), MathF.Sin(num2));
                    Projectile.NewProjectile(((Entity)((ModProjectile)this).Projectile).GetSource_FromAI((string)null), ((Entity)((ModProjectile)this).Projectile).Center + val5 * 30f, val5 * 15f, ModContent.ProjectileType<LanzaVeredictoProjectile>(), (int)((float)((ModProjectile)this).Projectile.damage * 0.16f), ((ModProjectile)this).Projectile.knockBack * 0.5f, ((ModProjectile)this).Projectile.owner, (float)(_semilla + i * 13), num2, 0f);
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_0041: Unknown result type (might be due to invalid IL or missing references)
            //IL_0042: Unknown result type (might be due to invalid IL or missing references)
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            float num = ((ModProjectile)this).Projectile.ai[2];
            bool num2 = num >= 45f && num < 51f;
            bool flag = num >= 105f && num < 111f;
            if (!num2 && !flag)
            {
                return false;
            }
            return Vector2.DistanceSquared(Utils.Center(targetHitbox), ((Entity)((ModProjectile)this).Projectile).Center) < 90000f;
        }

        private static int RunasEncendidas(float t)
        {
            if (t < 45f)
            {
                return (int)MathHelper.Clamp(t / 45f * 9f, 0f, 8.99f);
            }
            int num = (int)((t - 45f) / 15f);
            return Math.Clamp(8 - num, 0, 8);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_05de: Unknown result type (might be due to invalid IL or missing references)
            //IL_0602: Unknown result type (might be due to invalid IL or missing references)
            //IL_039f: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b4: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_03be: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_07e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_07e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_07f1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0468: Unknown result type (might be due to invalid IL or missing references)
            //IL_0478: Unknown result type (might be due to invalid IL or missing references)
            //IL_047d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0482: Unknown result type (might be due to invalid IL or missing references)
            //IL_0487: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0129: Unknown result type (might be due to invalid IL or missing references)
            //IL_0584: Unknown result type (might be due to invalid IL or missing references)
            //IL_059a: Unknown result type (might be due to invalid IL or missing references)
            //IL_053d: Unknown result type (might be due to invalid IL or missing references)
            //IL_06a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_06b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_06d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_06d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_06e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_06f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0704: Unknown result type (might be due to invalid IL or missing references)
            //IL_0795: Unknown result type (might be due to invalid IL or missing references)
            //IL_079a: Unknown result type (might be due to invalid IL or missing references)
            //IL_07a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_07b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_04d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_0738: Unknown result type (might be due to invalid IL or missing references)
            //IL_073a: Unknown result type (might be due to invalid IL or missing references)
            //IL_073e: Unknown result type (might be due to invalid IL or missing references)
            //IL_074d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0207: Unknown result type (might be due to invalid IL or missing references)
            //IL_020c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0314: Unknown result type (might be due to invalid IL or missing references)
            //IL_0327: Unknown result type (might be due to invalid IL or missing references)
            //IL_017d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0182: Unknown result type (might be due to invalid IL or missing references)
            //IL_0260: Unknown result type (might be due to invalid IL or missing references)
            //IL_0265: Unknown result type (might be due to invalid IL or missing references)
            //IL_026e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0283: Unknown result type (might be due to invalid IL or missing references)
            //IL_029d: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02da: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d5: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d7: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            float num2 = MathHelper.Clamp((184f - num) / 16f, 0f, 1f);
            int num3 = RunasEncendidas(num);
            bool flag = num < 45f;
            float num4 = 0.22f;
            float num5 = globalTimeWrappedHourly * num4;
            float num6 = Math.Max(2.3076923f, 0.3f);
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    VFXCore.Begin();
                    float num7 = (flag ? (MathHelper.Clamp(num / 10f, 0f, 1f) * (0.3f + 0.7f * (float)num3 / 8f)) : ((float)num3 / 8f * num2));
                    if (num7 > 0.02f)
                    {
                        Color body = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.08f);
                        SigiloLib.AroEliptico(((Entity)((ModProjectile)this).Projectile).Center, 120f, 112.8f, 0.14f, num5, globalTimeWrappedHourly, 0, body, num7);
                        SigiloLib.AroEliptico(((Entity)((ModProjectile)this).Projectile).Center, 79.200005f, 74.4f, 0.14f, (0f - num5) * 1.35f, globalTimeWrappedHourly, 1, DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.08f + 0.3f), num7 * 0.85f);
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        if (flag ? (i < num3) : (i < num3))
                        {
                            float ang = (float)i / 8f * ((float)Math.PI * 2f) + num5;
                            Color body2 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.1f + (float)i * 0.125f);
                            float num8 = 0.85f * num2;
                            if (flag && i == num3 - 1)
                            {
                                num8 *= 1.25f + 0.45f * MathF.Sin(globalTimeWrappedHourly * 22f);
                            }
                            SigiloLib.RunaOrbitando(((Entity)((ModProjectile)this).Projectile).Center, 120f, 112.8f, 0.14f, ang, globalTimeWrappedHourly, 0, i, body2, DefinitivasLib.BlancoCalido, num6, num8);
                        }
                    }
                    if (num3 >= 8)
                    {
                        Color val = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.1f + 0.5f);
                        float escala = num6 * 2.3f * (flag ? 1f : 0.9f);
                        float num9 = 62.399998f * (0.85f + 0.15f * MathF.Sin(globalTimeWrappedHourly * 2f));
                        float num10 = Math.Max(1.8f, 2.6399999f);
                        VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(val, 0.35f * num2), new Vector2(num9 * 2f, num10 * 4f), 0f, VFXCore.Pixel);
                        VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(val, 0.35f * num2), new Vector2(num10 * 4f, num9 * 2f), 0f, VFXCore.Pixel);
                        SigiloLib.Runa(((Entity)((ModProjectile)this).Projectile).Center, (0f - num5) * 0.3f, SigiloLib.RunasSolares, 7, escala, globalTimeWrappedHourly, 3f, val, DefinitivasLib.BlancoCalido, 0.95f * num2);
                    }
                    if (num3 > 0)
                    {
                        SigiloLib.PolvoRunico(((Entity)((ModProjectile)this).Projectile).Center, 141.59999f, 9, globalTimeWrappedHourly, _semilla, DefinitivasLib.BlancoCalido, 0.8f * ((float)num3 / 8f) * num2);
                    }
                    VFXCore.FlushAdditive(null, endActiveBatch: false);
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
                if (flag && num > 4f)
                {
                    float num11 = num % 8f / 8f;
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        OndaLib.Pulse(Main.spriteBatch, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, num11, 204f, DefinitivasLib.GradienteJuicio(num * 0.05f), (0.35f + 0.45f * (num / 45f)) * (1f - num11 * 0.5f), _semilla);
                        Main.spriteBatch.End();
                    }
                    catch
                    {
                        VFXCore.CerrarLoteSiAbierto();
                    }
                    VFXCore.ReabrirLoteVanilla();
                }
                if (num >= 45f && num < 69f)
                {
                    float num12 = (num - 45f) / 24f;
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        Vector2 val2 = ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition;
                        for (int j = 0; j < 3; j++)
                        {
                            float num13 = MathHelper.Clamp((num - 45f - (float)j * 5f) / 18f, 0f, 1f);
                            if (!(num13 <= 0f) && !(num13 >= 1f))
                            {
                                OndaLib.Shock(Main.spriteBatch, val2, num13, 320f + (float)j * 150f, DefinitivasLib.GradienteJuicio((float)j * 0.33f), 0.85f * (1f - num12 * 0.5f), _semilla + j * 17, 12f, OndaFalloff.Quadratic, j == 2);
                            }
                        }
                        if (num12 < 0.7f)
                        {
                            PyraLib.Flame(Main.spriteBatch, val2, 240f * (1f - num12 * 0.55f), PyraPalettes.SolarFire, _semilla + 3, globalTimeWrappedHourly, 0.85f * (1f - num12 / 0.7f));
                        }
                        LumenLib.Flare(Main.spriteBatch, val2, 190f * (1f - num12 * 0.6f), DefinitivasLib.BlancoCalido, 0.75f * (1f - num12), globalTimeWrappedHourly * 0.4f);
                        Main.spriteBatch.End();
                    }
                    catch
                    {
                        VFXCore.CerrarLoteSiAbierto();
                    }
                    VFXCore.ReabrirLoteVanilla();
                    VFXCore.Begin();
                    DefinitivasLib.PinchosDifraccion(((Entity)((ModProjectile)this).Projectile).Center, ((ModProjectile)this).Projectile.rotation, 620f * (1f - num12 * 0.7f), DefinitivasLib.BlancoCalido, 0.45f * (1f - num12), globalTimeWrappedHourly);
                    VFXCore.FlushAdditive();
                }
                if (num >= 51f && num < 180f)
                {
                    float num14 = ((num3 > 0) ? 1f : 0.35f);
                    VFXCore.Begin();
                    for (int k = 0; k < 7; k++)
                    {
                        float num15 = VFXCore.Hash01(_semilla, k, 21);
                        float num16 = VFXCore.Hash01(_semilla, k, 22);
                        float num17 = DefinitivasLib.Frac(globalTimeWrappedHourly * (0.1f + num15 * 0.08f) + num16);
                        float x = num16 * ((float)Math.PI * 2f) + num5;
                        Vector2 position = ((Entity)((ModProjectile)this).Projectile).Center + new Vector2(MathF.Cos(x), MathF.Sin(x)) * (120f * (0.3f + 0.75f * num15)) - new Vector2(0f, num17 * 60f);
                        Color c = DefinitivasLib.GradienteJuicio(num15 + globalTimeWrappedHourly * 0.12f);
                        float f = MathF.Sin(num17 * (float)Math.PI) * 0.55f * Math.Max((float)num3 / 8f, 0.25f) * num14;
                        if (DefinitivasLib.TexStar != null)
                        {
                            VFXCore.Quad(position, DefinitivasLib.Alfa(c, f), new Vector2(9f, 9f), num17 * 4f, DefinitivasLib.TexStar);
                        }
                    }
                    float num18 = (0.2f + 0.14f * MathF.Sin(globalTimeWrappedHourly * 5f)) * num14;
                    VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(DefinitivasLib.Ambar, num18 * num2), new Vector2(150f, 150f));
                    VFXCore.FlushAdditive();
                }
                float num19 = (float)num3 / 8f * num2;
                if (num19 > 0.02f)
                {
                    Color val3 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.08f);
                    Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center, (float)(int)(val3).R / 255f * 1.4f * num19, (float)(int)(val3).G / 255f * 1.4f * num19, (float)(int)(val3).B / 255f * 1.4f * num19);
                }
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
