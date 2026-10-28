using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using Terraria.Audio;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class IraDelCieloProjectile : ModProjectile
    {
        private const int TTormenta = 150;

        private const int Cadencia = 17;

        private const int NJuicios = 8;

        private const int TAnuncio = 10;

        private int _golpeados;

        private int _tDesdeAmbiente;

        private int _semillaNube;

        private Vector2 _xRelampago;

        private int _tRelampago = 99;

        private Vector2 _zonaCaida;

        private float _zonaGrosor;

        private int _tZona = 99;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 20;
            ((Entity)((ModProjectile)this).Projectile).height = 20;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = false;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 190;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            //IL_023d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0191: Unknown result type (might be due to invalid IL or missing references)
            //IL_0192: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bd: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_0464: Unknown result type (might be due to invalid IL or missing references)
            //IL_0487: Unknown result type (might be due to invalid IL or missing references)
            //IL_0492: Unknown result type (might be due to invalid IL or missing references)
            //IL_0282: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_02dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f2: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_030b: Unknown result type (might be due to invalid IL or missing references)
            //IL_030d: Unknown result type (might be due to invalid IL or missing references)
            //IL_030f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0311: Unknown result type (might be due to invalid IL or missing references)
            //IL_031b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0320: Unknown result type (might be due to invalid IL or missing references)
            //IL_035b: Unknown result type (might be due to invalid IL or missing references)
            //IL_036e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0375: Unknown result type (might be due to invalid IL or missing references)
            //IL_037a: Unknown result type (might be due to invalid IL or missing references)
            //IL_037f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0383: Unknown result type (might be due to invalid IL or missing references)
            //IL_0388: Unknown result type (might be due to invalid IL or missing references)
            //IL_038a: Unknown result type (might be due to invalid IL or missing references)
            //IL_038f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0394: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_03af: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b7: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            _ = Main.GlobalTimeWrappedHourly;
            if (_semillaNube == 0)
            {
                _semillaNube = (int)(Main.GameUpdateCount + 1000);
            }
            if (num > 1200f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            if (num < 2f)
            {
                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter;
            }
            if (_tRelampago < 99)
            {
                _tRelampago++;
            }
            if (_tZona < 99)
            {
                _tZona++;
            }
            _tDesdeAmbiente++;
            if (!Main.dedServ && Main.netMode != 2 && num < 150f && _tDesdeAmbiente > 12 && Utils.NextBool(Main.rand, 5))
            {
                _tDesdeAmbiente = 0;
                float num2 = Utils.NextFloat(Main.rand, Main.screenPosition.X, Main.screenPosition.X + (float)Main.screenWidth);
                Vector2 val2 = new Vector2(num2, Main.screenPosition.Y + Utils.NextFloat(Main.rand, -40f, 120f));
                Vector2 val3 = new Vector2(num2 + Utils.NextFloat(Main.rand, -160f, 160f), Main.screenPosition.Y + Utils.NextFloat(Main.rand, 220f, 420f));
                RayoSistema.Lanzar(RayoParams.Arco(val2, val3), (uint)Main.rand.Next(1, int.MaxValue), val3, val2 - val3, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 5f, 10, AnimRayo.Arco, luzEstable: false);
                if (Utils.NextBool(Main.rand, 3))
                {
                    OndaLib.Flash(DefinitivasLib.Ambar, 0.06f, 4, val2);
                }
                _xRelampago = val2;
                _tRelampago = 0;
            }
            if (num > 20f && (num - 10f - 10f) % 17f == 0f && _golpeados < 8)
            {
                NPC val4 = BuscarNoGolpeado(((Entity)val).Center);
                if (val4 != null)
                {
                    _golpeados++;
                    bool final = _golpeados == 8;
                    if (!Main.dedServ && Main.netMode != 2)
                    {
                        Vector2 val5 = new Vector2(((Entity)val4).Center.X + Utils.NextFloat(Main.rand, -30f, 30f), ((Entity)val4).Center.Y - 1300f);
                        Vector2 val6 = new Vector2(((Entity)val4).Center.X + Utils.NextFloat(Main.rand, -14f, 14f), ((Entity)val4).Center.Y - 30f);
                        RayoSistema.Lanzar(RayoParams.Arco(val5, val6), (uint)Main.rand.Next(1, int.MaxValue), val6, val5 - val6, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 3.5f, 10, AnimRayo.Arco, luzEstable: false);
                        for (int i = 0; i < 10; i++)
                        {
                            float x = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
                            float num3 = 380f;
                            Vector2 val7 = ((Entity)val4).Center + new Vector2(MathF.Cos(x), MathF.Sin(x)) * num3;
                            Vector2 value = Utils.SafeNormalize(((Entity)val4).Center - val7, Vector2.UnitX) * (num3 / 10f);
                            Dust.NewDustPerfect(val7, 228, (Vector2?)value, 0, DefinitivasLib.Oro, 0.9f).noGravity = true;
                        }
                    }
                    Golpear(val4, final);
                }
                else
                {
                    _golpeados++;
                }
            }
            if (!Main.dedServ && Main.netMode != 2 && num < 150f)
            {
                int num4 = 3;
                for (int j = 0; j < num4; j++)
                {
                    Dust.NewDustPerfect(new Vector2(Utils.NextFloat(Main.rand, Main.screenPosition.X, Main.screenPosition.X + (float)Main.screenWidth), Main.screenPosition.Y + Utils.NextFloat(Main.rand, 0f, 160f)), 228, (Vector2?)new Vector2(0f, Utils.NextFloat(Main.rand, 2f, 5f)), 0, DefinitivasLib.Ambar, 0.8f).noGravity = true;
                }
            }
        }

        private void Golpear(NPC presa, bool final)
        {
            //IL_0010: Unknown result type (might be due to invalid IL or missing references)
            //IL_0015: Unknown result type (might be due to invalid IL or missing references)
            //IL_0027: Unknown result type (might be due to invalid IL or missing references)
            //IL_002c: Unknown result type (might be due to invalid IL or missing references)
            //IL_003e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            //IL_0060: Unknown result type (might be due to invalid IL or missing references)
            //IL_006d: Unknown result type (might be due to invalid IL or missing references)
            //IL_020d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0212: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_026b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0142: Unknown result type (might be due to invalid IL or missing references)
            //IL_0147: Unknown result type (might be due to invalid IL or missing references)
            //IL_0112: Unknown result type (might be due to invalid IL or missing references)
            //IL_0120: Unknown result type (might be due to invalid IL or missing references)
            //IL_0125: Unknown result type (might be due to invalid IL or missing references)
            //IL_038a: Unknown result type (might be due to invalid IL or missing references)
            //IL_038f: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_02cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0168: Unknown result type (might be due to invalid IL or missing references)
            //IL_0195: Unknown result type (might be due to invalid IL or missing references)
            //IL_019a: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0308: Unknown result type (might be due to invalid IL or missing references)
            //IL_033a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0354: Unknown result type (might be due to invalid IL or missing references)
            float num = (final ? 40f : 22f);
            SoundStyle val = SoundID.Item122;
            val = (val).WithPitchOffset(final ? (-0.5f) : (-0.1f));
            DefinitivasLib.Sonar((val).WithVolumeScale(final ? 1.2f : 0.8f), ((Entity)presa).Center);
            if (final)
            {
                OndaLib.Flash(new Color(255, 244, 210), 0.32f, 12, ((Entity)presa).Center);
                OndaLib.Kick(13f, 18);
            }
            else
            {
                OndaLib.Kick(6f, 8);
            }
            if (!Main.dedServ && Main.netMode != 2)
            {
                RayoSistema.Lanzar(RayoParams.Tormenta, (uint)Main.rand.Next(1, int.MaxValue), ((Entity)presa).Center, null, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, num, final ? 40 : 26, AnimRayo.VenaTrueno);
                if (final)
                {
                    RayoSistema.Lanzar(RayoParams.Tormenta, (uint)Main.rand.Next(1, int.MaxValue), ((Entity)presa).Center, null, DefinitivasLib.Ambar, Color.White, num * 0.6f, 40, AnimRayo.VenaTrueno, luzEstable: false);
                }
                _zonaCaida = ((Entity)presa).Center;
                _zonaGrosor = num;
                _tZona = 0;
                if (final)
                {
                    for (int i = 0; i < 14; i++)
                    {
                        Dust.NewDustPerfect(((Entity)presa).Center + new Vector2(Utils.NextFloat(Main.rand, -200f, 200f), Utils.NextFloat(Main.rand, -240f, -40f)), 222, (Vector2?)new Vector2(Utils.NextFloat(Main.rand, -2f, 2f), Utils.NextFloat(Main.rand, 2f, 6f)), 0, DefinitivasLib.Ambar, 1f).noGravity = false;
                    }
                }
            }
            if (Main.netMode != 1)
            {
                Projectile.NewProjectile(((Entity)((ModProjectile)this).Projectile).GetSource_FromAI((string)null), ((Entity)presa).Center, Vector2.Zero, ModContent.ProjectileType<ColumnaIraProjectile>(), ((ModProjectile)this).Projectile.damage * ((!final) ? 1 : 2), ((ModProjectile)this).Projectile.knockBack, ((ModProjectile)this).Projectile.owner, final ? 2f : 0f, 0f, 0f);
            }
            if (final)
            {
                return;
            }
            NPC[] array = BuscarVecinos(((Entity)presa).Center, 2);
            foreach (NPC val2 in array)
            {
                if (val2 == null)
                {
                    continue;
                }
                if (!Main.dedServ && Main.netMode != 2)
                {
                    RayoSistema.Lanzar(RayoParams.Arco(((Entity)presa).Center, ((Entity)val2).Center), (uint)Main.rand.Next(1, int.MaxValue), ((Entity)val2).Center, ((Entity)presa).Center - ((Entity)val2).Center, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 8f, 14, AnimRayo.Arco, luzEstable: false);
                    for (int k = 0; k < 4; k++)
                    {
                        Dust.NewDustPerfect(((Entity)val2).Center, 228, (Vector2?)new Vector2(Utils.NextFloat(Main.rand, -4f, 4f), Utils.NextFloat(Main.rand, -5f, 1f)), 0, DefinitivasLib.GradienteJuicio(Utils.NextFloat(Main.rand, 1f)), 0.9f).noGravity = true;
                    }
                }
                if (Main.netMode != 1)
                {
                    Projectile.NewProjectile(((Entity)((ModProjectile)this).Projectile).GetSource_FromAI((string)null), ((Entity)val2).Center, Vector2.Zero, ModContent.ProjectileType<ColumnaIraProjectile>(), (int)((float)((ModProjectile)this).Projectile.damage * 0.45f), ((ModProjectile)this).Projectile.knockBack * 0.4f, ((ModProjectile)this).Projectile.owner, 1f, 0f, 0f);
                }
            }
        }

        private NPC BuscarNoGolpeado(Vector2 desde)
        {
            //IL_0050: Unknown result type (might be due to invalid IL or missing references)
            //IL_0055: Unknown result type (might be due to invalid IL or missing references)
            NPC result = null;
            float num = 1210000f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC val = Main.npc[i];
                if (val != null && ((Entity)val).active && val.life > 0 && !val.dontTakeDamage && !val.friendly && !val.townNPC && val.immune[255] <= 0)
                {
                    float num2 = Vector2.DistanceSquared(((Entity)val).Center, desde);
                    if (num2 < num)
                    {
                        num = num2;
                        result = val;
                    }
                }
            }
            return result;
        }

        private static NPC[] BuscarVecinos(Vector2 centro, int cuantos)
        {
            //IL_006c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0071: Unknown result type (might be due to invalid IL or missing references)
            NPC[] array = (NPC[])(object)new NPC[cuantos];
            float[] array2 = new float[cuantos];
            for (int i = 0; i < cuantos; i++)
            {
                array2[i] = 270400f;
            }
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC val = Main.npc[j];
                if (val == null || !((Entity)val).active || val.life <= 0 || val.dontTakeDamage || val.friendly || val.townNPC)
                {
                    continue;
                }
                float num = Vector2.DistanceSquared(((Entity)val).Center, centro);
                for (int k = 0; k < cuantos; k++)
                {
                    if (num < array2[k])
                    {
                        for (int num2 = cuantos - 1; num2 > k; num2--)
                        {
                            array2[num2] = array2[num2 - 1];
                            array[num2] = array[num2 - 1];
                        }
                        array2[k] = num;
                        array[k] = val;
                        break;
                    }
                }
            }
            return array;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_006c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0071: Unknown result type (might be due to invalid IL or missing references)
            //IL_0087: Unknown result type (might be due to invalid IL or missing references)
            //IL_008c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0091: Unknown result type (might be due to invalid IL or missing references)
            //IL_03e6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_0402: Unknown result type (might be due to invalid IL or missing references)
            //IL_0407: Unknown result type (might be due to invalid IL or missing references)
            //IL_0425: Unknown result type (might be due to invalid IL or missing references)
            //IL_0427: Unknown result type (might be due to invalid IL or missing references)
            //IL_042b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0459: Unknown result type (might be due to invalid IL or missing references)
            //IL_045e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0463: Unknown result type (might be due to invalid IL or missing references)
            //IL_0468: Unknown result type (might be due to invalid IL or missing references)
            //IL_046d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0166: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01de: Unknown result type (might be due to invalid IL or missing references)
            //IL_0200: Unknown result type (might be due to invalid IL or missing references)
            //IL_0207: Unknown result type (might be due to invalid IL or missing references)
            //IL_022f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0184: Unknown result type (might be due to invalid IL or missing references)
            //IL_0180: Unknown result type (might be due to invalid IL or missing references)
            //IL_0283: Unknown result type (might be due to invalid IL or missing references)
            //IL_0288: Unknown result type (might be due to invalid IL or missing references)
            //IL_028d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0292: Unknown result type (might be due to invalid IL or missing references)
            //IL_02b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02df: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0321: Unknown result type (might be due to invalid IL or missing references)
            //IL_0327: Unknown result type (might be due to invalid IL or missing references)
            //IL_0339: Unknown result type (might be due to invalid IL or missing references)
            //IL_033e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0357: Unknown result type (might be due to invalid IL or missing references)
            //IL_037b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0381: Unknown result type (might be due to invalid IL or missing references)
            //IL_04c2: Unknown result type (might be due to invalid IL or missing references)
            //IL_04cf: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            if (num < 150f)
            {
                float num2 = MathHelper.Clamp(num / 30f, 0f, 1f) * MathHelper.Clamp((150f - num) / 30f, 0f, 1f);
                float num3 = Main.screenWidth;
                float num4 = ((_xRelampago != Vector2.Zero) ? (_xRelampago - Main.screenPosition).X : (num3 * 0.5f));
                float num5 = MathHelper.Clamp(num / 150f, 0f, 1f) * 0.2f;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    Color val = new Color(30, 20, 10);
                    Color val2 = new Color(52, 34, 14);
                    for (int i = 0; i < 13; i++)
                    {
                        float num6 = (globalTimeWrappedHourly * (9f + (float)i * 3f) + (float)i * num3 * 0.31f) % (num3 * 1.5f) - num3 * 0.25f;
                        num6 = MathHelper.Lerp(num6, num4, num5 * (0.35f + 0.65f * (float)(i % 3) / 2f));
                        BrumaFX.Cloud(new Vector2(num6, 22f + (float)(i % 4) * 52f + MathF.Sin(globalTimeWrappedHourly * 0.5f + (float)i) * 9f), 170f + (float)i * 26f, (i % 2 == 0) ? val : val2, _semillaNube + i * 13, globalTimeWrappedHourly * 0.22f + (float)i * 0.7f, 9, 0.62f * num2);
                    }
                    Rectangle area = new Rectangle(-100, -70, (int)num3 + 200, 540);
                    BrumaFX.MistBand(area, new Color(40, 26, 12), _semillaNube + 91, globalTimeWrappedHourly, 22f, 0, 0.18f * num2);
                    BrumaFX.MistBand(area, new Color(46, 30, 14), _semillaNube + 92, globalTimeWrappedHourly, 13f, 1, 0.15f * num2);
                    BrumaFX.MistBand(area, new Color(58, 38, 16), _semillaNube + 93, globalTimeWrappedHourly, 9f, 2, 0.11f * num2);
                    Main.spriteBatch.End();
                    if (_tRelampago < 10)
                    {
                        float num7 = 1f - (float)_tRelampago / 10f;
                        BrumaFX.BeginGlow();
                        Vector2 val3 = _xRelampago - Main.screenPosition;
                        BrumaFX.Cloud(val3, 190f + 80f * num7, new Color(255, 214, 130), _semillaNube + 71, globalTimeWrappedHourly * 0.6f, 6, 0.46f * num7);
                        BrumaFX.Puff(val3 + new Vector2(70f, 24f), 110f, new Color(255, 236, 180), _semillaNube + 72, globalTimeWrappedHourly * 0.8f, 0.34f * num7);
                        BrumaFX.Puff(val3 + new Vector2(-60f, 14f), 90f, new Color(255, 222, 150), _semillaNube + 73, globalTimeWrappedHourly * 0.7f, 0.28f * num7);
                        Main.spriteBatch.End();
                    }
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
                SombrasLib.Vignette(0.1f + 0.16f * num2);
            }
            if (_tZona < 12)
            {
                Texture2D texRuido = DefinitivasLib.TexRuido;
                if (texRuido != null)
                {
                    float num8 = 1f - (float)_tZona / 12f;
                    Vector2 val4 = _zonaCaida + new Vector2(0f, -900f);
                    Vector2 zonaCaida = _zonaCaida;
                    Vector2[] array = (Vector2[])(object)new Vector2[8];
                    for (int j = 0; j < 8; j++)
                    {
                        float num9 = (float)j / 7f;
                        array[j] = Vector2.Lerp(val4, zonaCaida, num9) + new Vector2(MathF.Sin(num9 * 6f + globalTimeWrappedHourly * 22f) * 10f * (1f - num9), 0f) - Main.screenPosition;
                    }
                    float[] array2 = new float[8];
                    for (int k = 0; k < 8; k++)
                    {
                        array2[k] = _zonaGrosor * (0.9f + 0.6f * ((float)k / 7f)) * num8;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FajaTexturadaEnLote(array, array2, DefinitivasLib.Alfa(DefinitivasLib.Ambar, 0.3f * num8), texRuido, 260f, 1.6f, globalTimeWrappedHourly);
                    }
                    finally
                    {
                        RayoStrip.CerrarLote();
                    }
                }
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// COLUMNAIRAPROJECTILE — LA COLUMNA del juicio: el daño real de cada
    /// rayo (110 px de radio, ~22 t, golpe único). v6.54.7 — LA COLUMNA DE
    /// CARNE: la pila de seis pasadas texturizadas con el flujo INVERTIDO
    /// (la carne hierve hacia arriba) + LA TERCERA ESPIRAL exterior +
    /// EL HUMO DEL JUICIO (BrumaFX.Column: columnas de humo oscuro que
    /// suben del punto de impacto) + EL RESPLANDOR DEL SUELO que se queda
    /// ardiendo mientras la columna vive.
    /// ai[0] = 1 si es rama (fina) · 2 si es LA COLUMNA FINAL (espiral triple).
    /// </summary>
    public class ColumnaIraProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            bool rama = Projectile.ai[0] >= 1f && Projectile.ai[0] < 2f;
            bool final = Projectile.ai[0] >= 2f;
            float vida = 1f - Projectile.timeLeft / 22f;
            float potencia = (1f - vida) * (rama ? 0.7f : final ? 1.4f : 1f);

            Vector2 arriba = Projectile.Center + new Vector2(0f, -1100f);
            Vector2 abajo = Projectile.Center + new Vector2(0f, 60f);
            Color c = final ? DefinitivasLib.Ambar : DefinitivasLib.Oro;
            Color gBorde = DefinitivasLib.GradienteJuicio(Main.GlobalTimeWrappedHourly * 0.1f);

            // ==================== LA COLUMNA DE CARNE (LA PILA DE SEIS PASADAS) ====================
            // (flujo NEGATIVO: la carne HIERVE hacia arriba — el calor del
            // juicio sube; el remolino y el núcleo corren en contra)
            {
                const int N = 16;
                var espina = new Vector2[N];
                var perfil = new float[N];
                float flicker = DefinitivasLib.Flicker(Main.GlobalTimeWrappedHourly);
                for (int i = 0; i < N; i++)
                {
                    float q = i / (float)(N - 1);
                    espina[i] = Vector2.Lerp(arriba, abajo, q);
                    // v6.55.0 — LA CURA DE LAS ESQUINAS DEL CLAMP (como el
                    // partidor): saturación suave en vez de Clamp duro.
                    float s = MathF.Sin(q * MathHelper.Pi) * 1.3f - 0.25f;
                    float sat = MathHelper.SmoothStep(0f, 0.65f, MathHelper.Clamp(s, 0f, 0.65f));
                    perfil[i] = (0.25f + 0.75f * sat) *
                        (rama ? 40f : final ? 72f : 52f) * potencia * flicker;
                }
                DefinitivasLib.HazFusionEspina(espina, perfil, c, potencia,
                    Main.GlobalTimeWrappedHourly, 110, true, -1.2f, !rama);
            }

            // ==================== LA ESPIRAL TRIPLE DEL FINAL ====================
            // (dos venas helicoidales + LA TERCERA EXTERIOR nueva — el remate
            // es un taladro de luz)
            if (final)
            {
                const int N = 16;
                float tiempo = Main.GlobalTimeWrappedHourly;
                for (int lado = 0; lado < 3; lado++)
                {
                    var ptsS = new Vector2[N];
                    for (int i = 0; i < N; i++)
                    {
                        float q = i / (float)(N - 1);
                        float fase = q * MathHelper.TwoPi * 2.5f + tiempo * 6f + lado * (MathHelper.TwoPi / 3f);
                        float radioS = (26f + 46f * MathF.Sin(q * MathHelper.Pi)) * (1f - vida * 0.5f) +
                            (lado == 2 ? 34f : 0f);
                        ptsS[i] = Vector2.Lerp(arriba, abajo, q) +
                            new Vector2(MathF.Cos(fase), MathF.Sin(fase) * 0.25f) * radioS - Main.screenPosition;
                    }
                    Color cS = lado == 0 ? DefinitivasLib.Ambar : lado == 1 ? DefinitivasLib.Oro : DefinitivasLib.BlancoCalido;
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsS, (lado == 2 ? 15f : 22f) * potencia, cS, DefinitivasLib.BlancoCalido,
                            (lado == 2 ? 0.6f : 0.75f) * potencia, StormTaper.Flat, 113 + lado, 0, false, false, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // ==================== EL PIE DEL IMPACTO (halos + nova + destello + difracción) ====================
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.20f * potencia), new Vector2(560f, 560f) * potencia);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.5f * potencia), new Vector2(300f, 300f) * potencia);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.4f * potencia), new Vector2(150f, 150f) * potencia);
            if (VFXCore.NovaBurst != null)
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.8f * potencia),
                    new Vector2(340f, 340f) * potencia, vida * 1.4f, VFXCore.NovaBurst);
            if (VFXCore.DestelloFinal != null && final)
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.85f * potencia),
                    new Vector2(520f, 520f) * potencia, vida, VFXCore.DestelloFinal);
            // LOS PINCHOS DE DIFRACCIÓN horizontales en la base
            DefinitivasLib.PinchosDifraccion(Projectile.Center, 0f, (final ? 700f : 420f) * potencia,
                DefinitivasLib.BlancoCalido, 0.45f * potencia, Main.GlobalTimeWrappedHourly);
            VFXCore.FlushAdditive();

            // ==================== EL HUMO DEL JUICIO (LA LIBRERÍA DE BRUMAS) ====================
            // (columnas de humo oscuro subiendo del punto de impacto mientras
            // la columna arde — el mundo se QUEDA quemado un instante)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    Vector2 baseP = Projectile.Center - Main.screenPosition;
                    BrumaFX.Column(baseP, 240f * potencia, 90f, new Color(36, 24, 12),
                        311, Main.GlobalTimeWrappedHourly, 0.30f * potencia, 0.6f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // ==================== EL ANILLO DE CHOQUE (el suelo estalla) ====================
            if (!rama && Main.spriteBatch != null && vida < 1f)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida,
                        final ? 900f : 480f, c, 0.8f, 5, 14f, OndaFalloff.Quadratic, final);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
