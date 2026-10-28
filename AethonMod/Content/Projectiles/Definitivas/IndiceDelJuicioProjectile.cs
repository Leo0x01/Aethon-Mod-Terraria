// EL ÍNDICE DEL JUICIO (v6.56.0) — el libro real + 8 páginas fantasma.
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Projectiles.Definitivas
{
    public class IndiceDelJuicioProjectile : ModProjectile
    {
        private const int TApertura = 20;

        private const int NPaginas = 8;

        private const int Cadencia = 6;

        private const int TPrimera = 26;

        private const int TCierre = 104;

        private const int TVida = 132;

        private int _lanzadas;

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
            ((ModProjectile)this).Projectile.friendly = false;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 132;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_0089: Unknown result type (might be due to invalid IL or missing references)
            //IL_008e: Unknown result type (might be due to invalid IL or missing references)
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0163: Unknown result type (might be due to invalid IL or missing references)
            //IL_00df: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_018d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0192: Unknown result type (might be due to invalid IL or missing references)
            //IL_0193: Unknown result type (might be due to invalid IL or missing references)
            //IL_0198: Unknown result type (might be due to invalid IL or missing references)
            //IL_019d: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0246: Unknown result type (might be due to invalid IL or missing references)
            //IL_0247: Unknown result type (might be due to invalid IL or missing references)
            //IL_024e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0218: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            if (_semilla == 0)
            {
                _semilla = ((Entity)((ModProjectile)this).Projectile).whoAmI * 97 + 13;
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
            Vector2 val2 = PosLibro(val, num);
            if (Main.netMode != 2 && !Main.dedServ)
            {
                SoundStyle item;
                if (num == 2f)
                {
                    item = SoundID.Item74;
                    DefinitivasLib.Sonar((item).WithPitchOffset(-0.35f), val2);
                    AudioLib.Sonar(AudioLib.Familia.Runico, "apertura", val2, 0.9f);
                }
                if (num == 104f)
                {
                    item = SoundID.Item74;
                    DefinitivasLib.Sonar((item).WithPitchOffset(0.25f), val2);
                    AudioLib.Sonar(AudioLib.Familia.Runico, "muerte", val2, 0.8f);
                    OndaLib.Kick(4f, 8);
                }
            }
            if (num >= 26f && _lanzadas < 8 && (num - 26f) % 6f == 0f)
            {
                _lanzadas++;
                int num2 = _lanzadas - 1;
                NPC val3 = Candidato(val2, num2);
                float num3 = MathHelper.Lerp(-0.85f, 0.85f, (float)num2 / 7f);
                Vector2 val4 = default(Vector2);
                if (val3 != null)
                {
                    val4 = Utils.SafeNormalize(((Entity)val3).Center - val2, Vector2.UnitX);
                }
                else
                {
                    Vector2 val5 = Main.MouseWorld - val2;
                    float num4 = (((val5).LengthSquared() > 1f) ? Utils.ToRotation(val5) : Utils.ToRotation(new Vector2((float)((Entity)val).direction, -0.3f)));
                    val4 = new Vector2(MathF.Cos(num4 + num3), MathF.Sin(num4 + num3));
                }
                if (Main.netMode != 2 && !Main.dedServ && (num2 & 1) == 0)
                {
                    AudioLib.Sonar(AudioLib.Familia.Runico, "zona", val2, 0.5f, -0.15f + (float)num2 * 0.05f);
                }
                if (Main.netMode != 1)
                {
                    Projectile.NewProjectile(((Entity)((ModProjectile)this).Projectile).GetSource_FromAI((string)null), val2, val4 * 10.5f, ModContent.ProjectileType<PaginaIndiceProjectile>(), ((ModProjectile)this).Projectile.damage, ((ModProjectile)this).Projectile.knockBack, ((ModProjectile)this).Projectile.owner, (val3 != null) ? ((float)((Entity)val3).whoAmI) : (-1f), (float)(_semilla + num2 * 31), 0f);
                }
            }
            if (num >= 131f)
            {
                ((ModProjectile)this).Projectile.Kill();
            }
        }

        private Vector2 PosLibro(Player dueño, float t)
        {
            //IL_0071: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a7: Unknown result type (might be due to invalid IL or missing references)
            float num = MathHelper.Clamp(t / 20f, 0f, 1f);
            float num2 = MathHelper.Lerp(-46f, -78f, MathHelper.SmoothStep(0f, 1f, num));
            float num3 = ((t > 104f) ? MathHelper.Clamp((t - 104f) / 16f, 0f, 1f) : 0f);
            num2 = MathHelper.Lerp(num2, -42f, num3);
            return dueño.MountedCenter + new Vector2(MathF.Sin(t * 0.09f) * 3f, num2 + MathF.Sin(t * 0.13f + 1.7f) * 3f);
        }

        private static NPC Candidato(Vector2 desde, int indice)
        {
            //IL_006b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0070: Unknown result type (might be due to invalid IL or missing references)
            NPC[] array = (NPC[])(object)new NPC[8];
            float[] array2 = new float[8];
            for (int i = 0; i < 8; i++)
            {
                array2[i] = 2250000f;
            }
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC val = Main.npc[j];
                if (val == null || !((Entity)val).active || val.life <= 0 || val.dontTakeDamage || val.friendly || val.townNPC)
                {
                    continue;
                }
                float num = Vector2.DistanceSquared(((Entity)val).Center, desde);
                for (int k = 0; k < 8; k++)
                {
                    if (num < array2[k])
                    {
                        for (int num2 = 7; num2 > k; num2--)
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
            int num3 = 0;
            for (int l = 0; l < 8; l++)
            {
                if (array[l] != null)
                {
                    num3++;
                }
            }
            if (num3 == 0)
            {
                return null;
            }
            return array[indice % num3];
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_003a: Unknown result type (might be due to invalid IL or missing references)
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_026e: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0109: Unknown result type (might be due to invalid IL or missing references)
            //IL_0118: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_02df: Unknown result type (might be due to invalid IL or missing references)
            //IL_0443: Unknown result type (might be due to invalid IL or missing references)
            //IL_0458: Unknown result type (might be due to invalid IL or missing references)
            //IL_045f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0464: Unknown result type (might be due to invalid IL or missing references)
            //IL_0473: Unknown result type (might be due to invalid IL or missing references)
            //IL_0486: Unknown result type (might be due to invalid IL or missing references)
            //IL_0495: Unknown result type (might be due to invalid IL or missing references)
            //IL_0182: Unknown result type (might be due to invalid IL or missing references)
            //IL_018c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0191: Unknown result type (might be due to invalid IL or missing references)
            //IL_0192: Unknown result type (might be due to invalid IL or missing references)
            //IL_0197: Unknown result type (might be due to invalid IL or missing references)
            //IL_02fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0302: Unknown result type (might be due to invalid IL or missing references)
            //IL_0328: Unknown result type (might be due to invalid IL or missing references)
            //IL_032d: Unknown result type (might be due to invalid IL or missing references)
            //IL_034d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0352: Unknown result type (might be due to invalid IL or missing references)
            //IL_0354: Unknown result type (might be due to invalid IL or missing references)
            //IL_0361: Unknown result type (might be due to invalid IL or missing references)
            //IL_0370: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ea: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0203: Unknown result type (might be due to invalid IL or missing references)
            //IL_0208: Unknown result type (might be due to invalid IL or missing references)
            //IL_020d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0214: Unknown result type (might be due to invalid IL or missing references)
            //IL_0215: Unknown result type (might be due to invalid IL or missing references)
            //IL_021a: Unknown result type (might be due to invalid IL or missing references)
            //IL_021f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0221: Unknown result type (might be due to invalid IL or missing references)
            //IL_0226: Unknown result type (might be due to invalid IL or missing references)
            //IL_0236: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            Vector2 val2 = PosLibro(val, num);
            float num2 = MathHelper.Clamp(num / 20f, 0f, 1f);
            float num3 = ((num > 104f) ? MathHelper.Clamp((num - 104f) / 16f, 0f, 1f) : 0f);
            float num4 = (1f - num3) * MathHelper.Clamp(num / 4f, 0f, 1f);
            try
            {
                VFXCore.Begin();
                Color c = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.1f);
                VFXCore.Quad(val2, DefinitivasLib.Alfa(c, 0.3f * num4 * num2), new Vector2(240f, 190f) * (0.8f + 0.25f * num2));
                VFXCore.Quad(val2, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.35f * num4 * num2), new Vector2(120f, 100f));
                VFXCore.FlushAdditive();
                if (num > 4f && num < 38f)
                {
                    float progress = MathHelper.Clamp(num / 26f, 0f, 1f);
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        Vector2 val3 = Main.MouseWorld - val2;
                        float num5 = (((val3).LengthSquared() > 1f) ? Utils.ToRotation(val3) : Utils.ToRotation(new Vector2((float)((Entity)val).direction, -0.3f)));
                        for (int i = 0; i < 8; i++)
                        {
                            float x = num5 + MathHelper.Lerp(-0.85f, 0.85f, (float)i / 7f);
                            Vector2 val4 = val2 + new Vector2(MathF.Cos(x), MathF.Sin(x)) * 430f;
                            OndaLib.Telegrafo(Main.spriteBatch, val2 - Main.screenPosition, val4 - Main.screenPosition, progress, DefinitivasLib.GradienteJuicio((float)i * 0.125f), 1.8f);
                        }
                        Main.spriteBatch.End();
                    }
                    catch
                    {
                        VFXCore.CerrarLoteSiAbierto();
                    }
                    VFXCore.ReabrirLoteVanilla();
                }
                DibujarLibro(val2, num, num2, num3, globalTimeWrappedHourly);
                if (num < 26f && num2 > 0.2f)
                {
                    VFXCore.Begin();
                    for (int j = 0; j < 2; j++)
                    {
                        float num6 = VFXCore.Hash01(_semilla + j, 17, (int)(num * 0.5f));
                        float num7 = MathHelper.Clamp((num - 20f) / 26f + num6 * 0.5f, 0f, 1f);
                        Vector2 position = val2 + new Vector2((float)((j != 0) ? 1 : (-1)) * 8f, -10f - num7 * 46f) + new Vector2(MathF.Sin(globalTimeWrappedHourly * 3f + (float)j * 2.1f) * 4f, 0f);
                        Color c2 = DefinitivasLib.GradienteJuicio(VFXCore.Hash01(_semilla + j * 7, 3, 11) + globalTimeWrappedHourly * 0.08f);
                        VFXCore.Quad(position, DefinitivasLib.Alfa(c2, 0.45f * num7 * num2), new Vector2(14f, 20f), MathF.Sin(globalTimeWrappedHourly * 2f + (float)j) * 0.3f);
                    }
                    VFXCore.FlushAdditive();
                }
                if (num >= 104f && num < 118f)
                {
                    float num8 = (num - 104f) / 14f;
                    SombrasLib.OndaChoque(val2, 90f + 260f * DefinitivasLib.DeGolpe(num8), 0.55f * (1f - num8), roja: false);
                    VFXCore.Begin();
                    for (int k = 0; k < 6; k++)
                    {
                        float num9 = VFXCore.Hash01(_semilla, k, 3);
                        float num10 = VFXCore.Hash01(_semilla, k, 5);
                        float num11 = num9 * ((float)Math.PI * 2f);
                        float num12 = 30f + 110f * DefinitivasLib.DeGolpe(num8) * (0.4f + num10);
                        VFXCore.Quad(val2 + new Vector2(MathF.Cos(num11), MathF.Sin(num11) * 0.7f) * num12, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(num10 + globalTimeWrappedHourly * 0.1f), 0.35f * (1f - num8)), new Vector2(12f, 17f), num11);
                    }
                    VFXCore.FlushAdditive();
                }
            }
            finally
            {
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }

        private void DibujarLibro(Vector2 posMundo, float t, float abre, float cierre, float tiempo)
        {
            //IL_00a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00be: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_011a: Unknown result type (might be due to invalid IL or missing references)
            //IL_012a: Unknown result type (might be due to invalid IL or missing references)
            //IL_012c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0132: Unknown result type (might be due to invalid IL or missing references)
            //IL_0136: Unknown result type (might be due to invalid IL or missing references)
            //IL_013c: Unknown result type (might be due to invalid IL or missing references)
            Texture2D val = null;
            try
            {
                val = TextureAssets.Item[ModContent.ItemType<GrimoireEternal>()].Value;
            }
            catch
            {
            }
            if (val != null)
            {
                float num = MathHelper.Lerp(0.85f, 1.12f, MathHelper.SmoothStep(0f, 1f, abre));
                num = MathHelper.Lerp(num, 0.8f, cierre);
                float num2 = MathHelper.Lerp(-0.1f, -0.52f, MathHelper.SmoothStep(0f, 1f, abre)) + MathF.Sin(tiempo * 1.1f) * 0.03f;
                num2 = MathHelper.Lerp(num2, -0.06f, cierre);
                Rectangle val2 = new Rectangle(0, 0, val.Width, val.Height);
                Vector2 val3 = Utils.Size(val2) * 0.5f;
                Vector2 val4 = posMundo - Main.screenPosition;
                Color color = Lighting.GetColor(Utils.ToTileCoordinates(posMundo));
                float num3 = MathHelper.Clamp(abre * 2f, 0f, 1f) * (1f - cierre * 0.7f);
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                    Main.spriteBatch.Draw(val, val4, (Rectangle?)val2, color * num3, num2, val3, num, (SpriteEffects)0, 0f);
                }
                finally
                {
                    Main.spriteBatch.End();
                }
                VFXCore.ReabrirLoteVanilla();
            }
        }
    }

    public class PaginaIndiceProjectile : ModProjectile
    {
        private EspectroLib.Memoria _mem;

        private int _tTajo = 99;

        private Vector2 _posTajo;

        private float _angTajo;

        private int _semilla;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private float Fase => VFXCore.Hash01(_semilla, 11, 29);

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 22;
            ((Entity)((ModProjectile)this).Projectile).height = 30;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 110;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 60;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_01a5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0110: Unknown result type (might be due to invalid IL or missing references)
            //IL_011b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0132: Unknown result type (might be due to invalid IL or missing references)
            //IL_013d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0148: Unknown result type (might be due to invalid IL or missing references)
            //IL_014d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0188: Unknown result type (might be due to invalid IL or missing references)
            //IL_018f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0194: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            if (_semilla == 0)
            {
                _semilla = (int)((ModProjectile)this).Projectile.ai[1];
            }
            if (_mem.Pos == null)
            {
                _mem = EspectroLib.Crear();
            }
            if (num > 300f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            NPC val = ((((ModProjectile)this).Projectile.ai[0] >= 0f && ((ModProjectile)this).Projectile.ai[0] < (float)Main.maxNPCs) ? Main.npc[(int)((ModProjectile)this).Projectile.ai[0]] : null);
            if (val != null && (!((Entity)val).active || val.life <= 0 || val.friendly))
            {
                val = null;
            }
            if (val == null && num % 12f == 0f)
            {
                val = DefinitivasLib.BuscarPresa(((Entity)((ModProjectile)this).Projectile).Center, soloJefes: false);
                if (val != null)
                {
                    ((ModProjectile)this).Projectile.ai[0] = ((Entity)val).whoAmI;
                }
            }
            if (val != null && Vector2.Distance(((Entity)val).Center, ((Entity)((ModProjectile)this).Projectile).Center) > 150f)
            {
                float actual = Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity);
                float objetivo = Utils.ToRotation(((Entity)val).Center - ((Entity)((ModProjectile)this).Projectile).Center);
                float x = DefinitivasLib.AnguloHacia(actual, objetivo, 0.085f);
                float num2 = (((Entity)((ModProjectile)this).Projectile).velocity).Length();
                ((Entity)((ModProjectile)this).Projectile).velocity = new Vector2(MathF.Cos(x), MathF.Sin(x)) * num2;
            }
            ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity) + MathF.Sin(num * 0.31f + Fase * ((float)Math.PI * 2f)) * 0.3f;
            if (_tTajo < 99)
            {
                _tTajo++;
            }
            EspectroLib.Registrar(ref _mem, ((Entity)((ModProjectile)this).Projectile).Center, ((ModProjectile)this).Projectile.rotation);
            if (num > 105f)
            {
                ((ModProjectile)this).Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //IL_000a: Unknown result type (might be due to invalid IL or missing references)
            //IL_000f: Unknown result type (might be due to invalid IL or missing references)
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0040: Unknown result type (might be due to invalid IL or missing references)
            _tTajo = -4;
            _posTajo = ((Entity)target).Center;
            _angTajo = Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity);
            if (Main.netMode != 2 && !Main.dedServ)
            {
                AudioLib.Sonar(AudioLib.Familia.Electrica, "impacto", ((Entity)target).Center, 0.55f, 0.2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            //IL_005b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0066: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0093: Unknown result type (might be due to invalid IL or missing references)
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_0109: Unknown result type (might be due to invalid IL or missing references)
            //IL_0124: Unknown result type (might be due to invalid IL or missing references)
            //IL_0129: Unknown result type (might be due to invalid IL or missing references)
            //IL_0130: Unknown result type (might be due to invalid IL or missing references)
            //IL_0135: Unknown result type (might be due to invalid IL or missing references)
            //IL_0140: Unknown result type (might be due to invalid IL or missing references)
            //IL_0145: Unknown result type (might be due to invalid IL or missing references)
            //IL_014c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0151: Unknown result type (might be due to invalid IL or missing references)
            //IL_0156: Unknown result type (might be due to invalid IL or missing references)
            //IL_0162: Unknown result type (might be due to invalid IL or missing references)
            //IL_017e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0183: Unknown result type (might be due to invalid IL or missing references)
            //IL_018a: Unknown result type (might be due to invalid IL or missing references)
            //IL_018f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0194: Unknown result type (might be due to invalid IL or missing references)
            //IL_019c: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_0247: Unknown result type (might be due to invalid IL or missing references)
            //IL_0252: Unknown result type (might be due to invalid IL or missing references)
            //IL_0257: Unknown result type (might be due to invalid IL or missing references)
            //IL_025c: Unknown result type (might be due to invalid IL or missing references)
            //IL_029c: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a1: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            float num2 = MathHelper.Clamp((110f - num) / 26f, 0f, 1f);
            Color val = DefinitivasLib.GradienteJuicio(Fase);
            try
            {
                VFXCore.Begin();
                EspectroLib.ColaHistoria(ref _mem, 2, 6, val, new Vector2(20f, 30f), 0.42f * num2);
                Vector2 val2 = (((((Entity)((ModProjectile)this).Projectile).velocity).LengthSquared() > 0.01f) ? Vector2.Normalize(((Entity)((ModProjectile)this).Projectile).velocity) : Vector2.UnitX);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(val, 0.55f * num2), new Vector2(26f, 38f), ((ModProjectile)this).Projectile.rotation);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.7f * num2), new Vector2(13f, 26f), ((ModProjectile)this).Projectile.rotation);
                VFXCore.Line(((Entity)((ModProjectile)this).Projectile).Center - val2 * 12f, ((Entity)((ModProjectile)this).Projectile).Center + val2 * 12f, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * num2), 2.4f);
                if (DefinitivasLib.TexStar != null)
                {
                    VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center + val2 * 15f, DefinitivasLib.Alfa(val, 0.65f * num2), new Vector2(16f, 16f), ((ModProjectile)this).Projectile.rotation, DefinitivasLib.TexStar);
                }
                VFXCore.FlushAdditive();
                if (_tTajo >= 0 && _tTajo < 14)
                {
                    float num3 = (float)_tTajo / 14f;
                    float prog = DefinitivasLib.DeGolpe(num3 / 0.36f);
                    float brillo = MathF.Sin(MathHelper.Clamp(num3, 0f, 1f) * (float)Math.PI);
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        TajoLib.Tajo(_posTajo - Main.screenPosition, 44f + 30f * num3, _angTajo - 0.95f, _angTajo + 0.95f, prog, brillo, 8f, DefinitivasLib.GradienteJuicio(Fase + 0.2f), DefinitivasLib.BlancoCalido, _semilla, globalTimeWrappedHourly);
                        Main.spriteBatch.End();
                    }
                    catch
                    {
                        VFXCore.CerrarLoteSiAbierto();
                    }
                    VFXCore.ReabrirLoteVanilla();
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
