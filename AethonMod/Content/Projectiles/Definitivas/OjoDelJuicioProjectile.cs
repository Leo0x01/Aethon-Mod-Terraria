// EL OJO DEL JUICIO (v6.56.0) — el coloso del cielo con el haz masivo de BeamLib.
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.

using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    public class OjoDelJuicioProjectile : ModProjectile
    {
        private const int TApertura = 16;

        private const int TCarga = 60;

        private const int TBarrido = 90;

        private const int TFinal = 178;

        private const float TamanoOjo = 300f;

        private const float LargoHaz = 2600f;

        private const float SemiBarrido = 0.5f;

        private int _semilla;

        private bool _disparo;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private Vector2 OjoPos => ((Entity)((ModProjectile)this).Projectile).Center;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 30;
            ((Entity)((ModProjectile)this).Projectile).height = 30;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 184;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 45;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_011e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0123: Unknown result type (might be due to invalid IL or missing references)
            //IL_007e: Unknown result type (might be due to invalid IL or missing references)
            //IL_008d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0092: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0207: Unknown result type (might be due to invalid IL or missing references)
            //IL_022a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0246: Unknown result type (might be due to invalid IL or missing references)
            //IL_024b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0254: Unknown result type (might be due to invalid IL or missing references)
            //IL_0259: Unknown result type (might be due to invalid IL or missing references)
            //IL_0262: Unknown result type (might be due to invalid IL or missing references)
            //IL_0268: Unknown result type (might be due to invalid IL or missing references)
            //IL_0279: Unknown result type (might be due to invalid IL or missing references)
            //IL_028d: Unknown result type (might be due to invalid IL or missing references)
            //IL_029a: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d5: Unknown result type (might be due to invalid IL or missing references)
            //IL_02de: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0304: Unknown result type (might be due to invalid IL or missing references)
            //IL_030a: Unknown result type (might be due to invalid IL or missing references)
            //IL_031b: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            if (_semilla == 0)
            {
                _semilla = ((Entity)((ModProjectile)this).Projectile).whoAmI * 43 + 5;
            }
            if (num > 1200f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            // v6.56.1 — LA COLOCACIÓN LA DECIDE EL DUEÑO (su ratón); el
            // servidor y el resto la reciben por sync (antes el servidor
            // peleaba la rotación con su MouseWorld basura).
            if (num < 2f && Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)
            {
                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + new Vector2(0f, -430f);
                ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
                Vector2 val2 = Main.MouseWorld - val.MountedCenter;
                float num2 = (((val2).LengthSquared() > 1f) ? Utils.ToRotation(val2) : ((float)Math.PI / 2f));
                if (MathF.Sin(num2) < 0.18f)
                {
                    num2 = ((MathF.Cos(num2) < 0f) ? 2.9415927f : 0.2f);
                }
                ((ModProjectile)this).Projectile.rotation = num2;
                ((ModProjectile)this).Projectile.netUpdate = true;
            }
            ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
            if (num >= 60f && num < 150f)
            {
                if (!_disparo)
                {
                    _disparo = true;
                    ((ModProjectile)this).Projectile.localAI[0] = ((ModProjectile)this).Projectile.rotation;
                    ((ModProjectile)this).Projectile.netUpdate = true;
                }
                float num3 = MathHelper.Clamp((num - 60f) / 90f, 0f, 1f);
                ((ModProjectile)this).Projectile.rotation = ((ModProjectile)this).Projectile.localAI[0] - 0.5f + MathHelper.SmoothStep(0f, 1f, num3) * 1f;
            }
            if (Main.netMode != 2 && !Main.dedServ)
            {
                SoundStyle val3;
                if (num == 3f)
                {
                    val3 = SoundID.Item122;
                    DefinitivasLib.Sonar((val3).WithPitchOffset(-0.6f), OjoPos);
                    AudioLib.Sonar(AudioLib.Familia.Cosmica, "apertura", OjoPos, 0.9f);
                }
                if (num == 30f)
                {
                    AudioLib.Sonar(AudioLib.Familia.Electrica, "carga", OjoPos, 0.7f);
                }
                if (num == 60f)
                {
                    val3 = SoundID.Item122;
                    val3 = (val3).WithPitchOffset(-0.2f);
                    DefinitivasLib.Sonar((val3).WithVolumeScale(1.35f), OjoPos);
                    AudioLib.Sonar(AudioLib.Familia.Electrica, "impacto", OjoPos);
                    OndaLib.Flash(DefinitivasLib.BlancoCalido, 0.35f, 14, OjoPos);
                    OndaLib.Kick(12f, 18);
                }
                if (num == 105f)
                {
                    val3 = SoundID.Item122;
                    val3 = (val3).WithPitchOffset(-0.35f);
                    DefinitivasLib.Sonar((val3).WithVolumeScale(0.7f), OjoPos);
                }
                if (num == 150f)
                {
                    val3 = SoundID.Item74;
                    DefinitivasLib.Sonar((val3).WithPitchOffset(0.2f), OjoPos);
                    AudioLib.Sonar(AudioLib.Familia.Cosmica, "muerte", OjoPos, 0.85f);
                    OndaLib.Kick(7f, 10);
                }
            }
            if (num >= 178f)
            {
                ((ModProjectile)this).Projectile.Kill();
            }
        }

        private static float Apertura(float t)
        {
            if (t < 16f)
            {
                return SombrasLib.DeGolpe(t / 16f);
            }
            if (t >= 150f)
            {
                return MathHelper.Clamp(1f - (t - 150f) / 16f, 0f, 1f);
            }
            if (t >= 60f && t < 68f)
            {
                return MathHelper.Lerp(0.72f, 1f, (t - 60f) / 8f);
            }
            return 1f;
        }

        private static float ContraccionPupila(float t)
        {
            if (t < 16f)
            {
                return 1f;
            }
            if (t < 60f)
            {
                float num = MathHelper.Clamp((t - 16f) / 44f, 0f, 1f);
                return MathHelper.Lerp(1f, 0.3f, MathHelper.SmoothStep(0f, 1f, num));
            }
            if (t < 70f)
            {
                return MathHelper.Lerp(0.3f, 0.62f, (t - 60f) / 10f);
            }
            if (t < 150f)
            {
                return 0.62f;
            }
            return MathHelper.Lerp(0.62f, 1f, MathHelper.Clamp((t - 60f - 90f) / 16f, 0f, 1f));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0052: Unknown result type (might be due to invalid IL or missing references)
            //IL_0058: Unknown result type (might be due to invalid IL or missing references)
            //IL_005d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0062: Unknown result type (might be due to invalid IL or missing references)
            //IL_0063: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            //IL_006a: Unknown result type (might be due to invalid IL or missing references)
            //IL_006f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0074: Unknown result type (might be due to invalid IL or missing references)
            //IL_0075: Unknown result type (might be due to invalid IL or missing references)
            //IL_0076: Unknown result type (might be due to invalid IL or missing references)
            //IL_007b: Unknown result type (might be due to invalid IL or missing references)
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            float num = ((ModProjectile)this).Projectile.ai[2];
            if (num < 62f || num >= 150f)
            {
                return false;
            }
            Vector2 val = new Vector2(MathF.Cos(((ModProjectile)this).Projectile.rotation), MathF.Sin(((ModProjectile)this).Projectile.rotation));
            Vector2 val2 = OjoPos + val * 125.99999f;
            Vector2 b = val2 + val * 2600f;
            return FormaLib.DistanciaASegmento(Utils.Center(targetHitbox), val2, b) < 64f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_0349: Unknown result type (might be due to invalid IL or missing references)
            //IL_0353: Unknown result type (might be due to invalid IL or missing references)
            //IL_053b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0540: Unknown result type (might be due to invalid IL or missing references)
            //IL_0547: Unknown result type (might be due to invalid IL or missing references)
            //IL_054c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0551: Unknown result type (might be due to invalid IL or missing references)
            //IL_055a: Unknown result type (might be due to invalid IL or missing references)
            //IL_055f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0566: Unknown result type (might be due to invalid IL or missing references)
            //IL_0568: Unknown result type (might be due to invalid IL or missing references)
            //IL_0574: Unknown result type (might be due to invalid IL or missing references)
            //IL_0596: Unknown result type (might be due to invalid IL or missing references)
            //IL_0598: Unknown result type (might be due to invalid IL or missing references)
            //IL_05a5: Unknown result type (might be due to invalid IL or missing references)
            //IL_05b4: Unknown result type (might be due to invalid IL or missing references)
            //IL_05fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_0613: Unknown result type (might be due to invalid IL or missing references)
            //IL_05ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_05cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_05d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0393: Unknown result type (might be due to invalid IL or missing references)
            //IL_0376: Unknown result type (might be due to invalid IL or missing references)
            //IL_0378: Unknown result type (might be due to invalid IL or missing references)
            //IL_0382: Unknown result type (might be due to invalid IL or missing references)
            //IL_038c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0669: Unknown result type (might be due to invalid IL or missing references)
            //IL_069b: Unknown result type (might be due to invalid IL or missing references)
            //IL_06a0: Unknown result type (might be due to invalid IL or missing references)
            //IL_06b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_06d4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0398: Unknown result type (might be due to invalid IL or missing references)
            //IL_039b: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a0: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03f7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0403: Unknown result type (might be due to invalid IL or missing references)
            //IL_0411: Unknown result type (might be due to invalid IL or missing references)
            //IL_0420: Unknown result type (might be due to invalid IL or missing references)
            //IL_0422: Unknown result type (might be due to invalid IL or missing references)
            //IL_042e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0442: Unknown result type (might be due to invalid IL or missing references)
            //IL_0452: Unknown result type (might be due to invalid IL or missing references)
            //IL_0461: Unknown result type (might be due to invalid IL or missing references)
            //IL_0466: Unknown result type (might be due to invalid IL or missing references)
            //IL_046b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0477: Unknown result type (might be due to invalid IL or missing references)
            //IL_0486: Unknown result type (might be due to invalid IL or missing references)
            //IL_03be: Unknown result type (might be due to invalid IL or missing references)
            //IL_03c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_03c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e5: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00af: Unknown result type (might be due to invalid IL or missing references)
            //IL_013f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0144: Unknown result type (might be due to invalid IL or missing references)
            //IL_0149: Unknown result type (might be due to invalid IL or missing references)
            //IL_014e: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_020e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0213: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_02dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0307: Unknown result type (might be due to invalid IL or missing references)
            //IL_030d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0231: Unknown result type (might be due to invalid IL or missing references)
            //IL_022d: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            float num2 = Apertura(num);
            float num3 = ContraccionPupila(num);
            bool flag = num >= 60f && num < 150f;
            float num4 = MathHelper.Clamp(num / 6f, 0f, 1f) * ((num >= 150f) ? MathHelper.Clamp(1f - (num - 60f - 90f) / 20f, 0f, 1f) : 1f);
            Vector2 val = ((flag || num >= 60f) ? (new Vector2(MathF.Cos(((ModProjectile)this).Projectile.rotation), MathF.Sin(((ModProjectile)this).Projectile.rotation)) * 500f) : (Main.MouseWorld - OjoPos));
            try
            {
                SombrasLib.Vignette((num < 60f) ? (0.1f + 0.16f * MathHelper.Clamp(num / 60f, 0f, 1f)) : (flag ? (0.3f + 0.04f * MathF.Sin(globalTimeWrappedHourly * 6f)) : (0.32f * num4)));
                Vector2 val2 = OjoPos - Main.screenPosition;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    Color val3 = new Color(32, 22, 12);
                    Color val4 = new Color(56, 38, 16);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = (float)i / 5f * ((float)Math.PI * 2f) + globalTimeWrappedHourly * 0.07f * ((i % 2 == 0) ? 1f : (-1f));
                        float num5 = 300f * (0.85f + 0.3f * VFXCore.Hash01(_semilla, i, 41));
                        BrumaFX.Cloud(val2 + new Vector2(MathF.Cos(x), MathF.Sin(x) * 0.7f) * num5 + new Vector2(MathF.Sin(globalTimeWrappedHourly * 0.4f + (float)i) * 14f, 0f), 130f + (float)i * 26f, (i % 2 == 0) ? val3 : val4, _semilla + i * 19, globalTimeWrappedHourly * 0.22f + (float)i * 0.6f, 5, 0.42f * num4);
                    }
                    Main.spriteBatch.End();
                    BrumaFX.BeginGlow();
                    for (int j = 0; j < 3; j++)
                    {
                        float x2 = (float)j / 3f * ((float)Math.PI * 2f) - globalTimeWrappedHourly * 0.11f + 1.1f;
                        BrumaFX.Puff(val2 + new Vector2(MathF.Cos(x2), MathF.Sin(x2) * 0.6f) * 225f, 90f + 26f * (float)j, DefinitivasLib.Oro, _semilla + 71 + j * 7, globalTimeWrappedHourly * 0.5f, 0.22f * num4 * num2);
                    }
                    Main.spriteBatch.End();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    VFXCore.Begin();
                    SombrasLib.Ojo(OjoPos, 300f, val, num2, pupila: false);
                    if (num2 > 0.25f)
                    {
                        Vector2 val5 = (((val).LengthSquared() < 0.01f) ? Vector2.Zero : (Vector2.Normalize(val) * 300f * 0.18f));
                        Vector2 position = OjoPos + val5;
                        Color c = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f);
                        if (VFXCore.Ring != null)
                        {
                            VFXCore.Quad(position, DefinitivasLib.Alfa(c, 0.8f * num2), new Vector2(108.00001f, 125.99999f), 0f, VFXCore.Ring);
                        }
                        float num6 = 102f * num3;
                        VFXCore.Quad(position, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.95f * num2), new Vector2(num6, 114f * num3));
                        VFXCore.Quad(position, DefinitivasLib.Alfa(Color.White, 0.9f * num2), new Vector2(num6 * 0.45f, 60f * num3));
                        VFXCore.Quad(OjoPos + new Vector2(-66f, -48f), DefinitivasLib.Alfa(Color.White, 0.75f * num2), new Vector2(30f, 21f));
                    }
                    VFXCore.FlushAdditive();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
                if (flag)
                {
                    float num7 = MathHelper.Clamp((num - 60f) / 5f, 0f, 1f);
                    float num8 = MathHelper.Clamp((150f - num) / 12f, 0f, 1f);
                    float num9 = MathF.Sin(MathHelper.Clamp(num7 * num8, 0f, 1f) * ((float)Math.PI / 2f)) * DefinitivasLib.Flicker(globalTimeWrappedHourly);
                    Vector2 val6 = new Vector2(MathF.Cos(((ModProjectile)this).Projectile.rotation), MathF.Sin(((ModProjectile)this).Projectile.rotation));
                    Vector2 val7 = OjoPos + val6 * 125.99999f;
                    Color color = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f);
                    VFXCore.Begin();
                    BeamLib.Rayo(val7, val6, 2600f, 130f, color, globalTimeWrappedHourly, _semilla, choquePared: false, 0.9f, 0.55f + 0.45f * num9);
                    VFXCore.Quad(val7, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * num9), new Vector2(280f, 280f));
                    if (VFXCore.NovaBurst != null)
                    {
                        VFXCore.Quad(val7, DefinitivasLib.Alfa(Color.White, 0.75f * num9), new Vector2(340f, 340f), globalTimeWrappedHourly * 1.2f, VFXCore.NovaBurst);
                    }
                    DefinitivasLib.PinchosDifraccion(val7, ((ModProjectile)this).Projectile.rotation, 560f * num9, DefinitivasLib.BlancoCalido, 0.5f * num9, globalTimeWrappedHourly);
                    if (VFXCore.QuadCount > 0)
                    {
                        try
                        {
                            VFXCore.FlushAdditive();
                        }
                        finally
                        {
                            VFXCore.ReabrirLoteVanilla();
                        }
                    }
                }
                if (num >= 150f && num < 164f)
                {
                    float num10 = (num - 60f - 90f) / 14f;
                    SombrasLib.OndaChoque(OjoPos, 160f + 320f * DefinitivasLib.DeGolpe(num10), 0.6f * (1f - num10), roja: false);
                    VFXCore.Begin();
                    VFXCore.Quad(OjoPos, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.4f * (1f - num10)), new Vector2(360f * (1f + num10), 240f * (1f + num10)));
                    VFXCore.FlushAdditive();
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
