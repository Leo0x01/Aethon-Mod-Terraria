using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class AlasAuroraHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private float Spread
        {
            get
            {
                return ((ModProjectile)this).Projectile.localAI[0];
            }
            set
            {
                ((ModProjectile)this).Projectile.localAI[0] = value;
            }
        }

        private float Flap
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

        private float MezclaDia
        {
            get
            {
                return ((ModProjectile)this).Projectile.localAI[2];
            }
            set
            {
                ((ModProjectile)this).Projectile.localAI[2] = value;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[((ModProjectile)this).Type] = 1;
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 16;
            ((Entity)((ModProjectile)this).Projectile).height = 16;
            ((ModProjectile)this).Projectile.friendly = false;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 6;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.ignoreWater = true;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            //IL_005b: Unknown result type (might be due to invalid IL or missing references)
            //IL_006b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0070: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cf: Unknown result type (might be due to invalid IL or missing references)
            //IL_0289: Unknown result type (might be due to invalid IL or missing references)
            //IL_028e: Unknown result type (might be due to invalid IL or missing references)
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            if (val == null || !((Entity)val).active || val.dead || !SigueViva(val))
            {
                if (Main.netMode != 2 && Spread > 0.05f)
                {
                    SuspiroFinal();
                }
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            ((Entity)((ModProjectile)this).Projectile).Center = ((Entity)val).Center;
            ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
            ((ModProjectile)this).Projectile.timeLeft = 6;
            float num = 1f;
            byte alaEstilo = val.GetModPlayer<AlasAuroraPlayer>().AlaEstilo;
            if (alaEstilo > 0)
            {
                num = AlasCatalogo.De(alaEstilo).FlapVel;
            }
            float num2 = 0f;
            bool flag = false;
            if (!val.GetModPlayer<AlasAuroraPlayer>().OcultarVisual && val.wings == 0)
            {
                float num3 = (((Entity)val).velocity).Length();
                flag = ((Entity)val).velocity.Y != 0f;
                num2 = MathHelper.Clamp(0.35f + num3 * 0.055f + (flag ? 0.25f : 0f), 0.35f, 1f);
            }
            Spread = MathHelper.Lerp(Spread, num2, 0.085f);
            Flap += (0.055f + (flag ? 0.15f : 0f) + Spread * 0.03f) * num;
            MezclaDia = MathHelper.Lerp(MezclaDia, Main.dayTime ? 1f : 0f, 0.025f);
            if (Spread > 0.05f && Main.netMode != 2)
            {
                float h = Flap * 0.02f % 1f;
                Color val2 = ((alaEstilo > 0) ? ColorDe(in AlasCatalogo.De(alaEstilo), h) : ColorPrisma(h));
                float num4 = ((alaEstilo > 0) ? AlasCatalogo.De(alaEstilo).Luz : 1f);
                float mezclaDia = MezclaDia;
                Vector3 val3 = new Vector3(((float)(int)(val2).R / 255f * (1f - mezclaDia) + 0.95f * mezclaDia) * 0.42f * Spread * num4, ((float)(int)(val2).G / 255f * (1f - mezclaDia) + 0.88f * mezclaDia) * 0.42f * Spread * num4, ((float)(int)(val2).B / 255f * (1f - mezclaDia) + 0.66f * mezclaDia) * 0.42f * Spread * num4);
                Lighting.AddLight(((Entity)val).Center, val3);
            }
        }

        private static Color ColorDe(in EstiloAlas estilo, float h)
        {
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            //IL_004e: Unknown result type (might be due to invalid IL or missing references)
            //IL_005a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            if (estilo.Paleta == null || estilo.Paleta.Length == 0)
            {
                return ColorPrisma(h);
            }
            h -= MathF.Floor(h);
            float num = h * (float)estilo.Paleta.Length;
            int num2 = (int)num % estilo.Paleta.Length;
            int num3 = (num2 + 1) % estilo.Paleta.Length;
            return Color.Lerp(estilo.Paleta[num2], estilo.Paleta[num3], num - (float)(int)num);
        }

        private static bool SigueViva(Player duenio)
        {
            return duenio.GetModPlayer<AlasAuroraPlayer>().VisualViva;
        }

        private static Color ColorPrisma(float h)
        {
            //IL_00f0: Unknown result type (might be due to invalid IL or missing references)
            h -= MathF.Floor(h);
            float num;
            float num2;
            float num3;
            if (h < 1f / 6f)
            {
                num = 1f;
                num2 = h * 6f;
                num3 = 0f;
            }
            else if (h < 1f / 3f)
            {
                num = 2f - h * 6f;
                num2 = 1f;
                num3 = 0f;
            }
            else if (h < 0.5f)
            {
                num = 0f;
                num2 = 1f;
                num3 = h * 6f - 2f;
            }
            else if (h < 2f / 3f)
            {
                num = 0f;
                num2 = 4f - h * 6f;
                num3 = 1f;
            }
            else if (h < 5f / 6f)
            {
                num = h * 6f - 4f;
                num2 = 0f;
                num3 = 1f;
            }
            else
            {
                num = 1f;
                num2 = 0f;
                num3 = 6f - h * 6f;
            }
            return new Color((int)(num * 255f), (int)(num2 * 255f), (int)(num3 * 255f));
        }

        private void SuspiroFinal()
        {
            //IL_001a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            //IL_0049: Unknown result type (might be due to invalid IL or missing references)
            //IL_0060: Unknown result type (might be due to invalid IL or missing references)
            for (int i = 0; i < 14; i++)
            {
                float x = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
                Dust.NewDustPerfect(((Entity)((ModProjectile)this).Projectile).Center, 173, (Vector2?)(new Vector2(MathF.Cos(x), MathF.Sin(x)) * Utils.NextFloat(Main.rand, 0.8f, 2.6f)), 160, ColorPrisma((float)i / 14f), 1.1f).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_0059: Unknown result type (might be due to invalid IL or missing references)
            //IL_0097: Unknown result type (might be due to invalid IL or missing references)
            //IL_009c: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c9: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            if (val == null || !((Entity)val).active || val.dead)
            {
                return false;
            }
            if (Spread <= 0.02f)
            {
                return false;
            }
            // v6.56.1 — DETERMINISMO DEL AURA (ley §0.4 del códice): el
            // brillo ya no tira de Main.rand en render — Hash01 con
            // semilla viva (misma probabilidad 1/22 y mismos rangos).
            if (VFXCore.Hash01(((Entity)val).whoAmI * 17 + 5, (int)Main.GameUpdateCount, 311) < 0.0455f)
            {
                int semillaAura = ((Entity)val).whoAmI * 31 + 7;
                int tick = (int)Main.GameUpdateCount;
                float dx = 20f + 40f * VFXCore.Hash01(semillaAura, tick, 313);
                float dy = 30f + 50f * VFXCore.Hash01(semillaAura + 1, tick, 317);
                Dust.NewDustPerfect(val.MountedCenter + new Vector2((float)(-((Entity)val).direction) * dx, (0f - dy) * val.gravDir), 173, (Vector2?)new Vector2(0f, 0.4f), 140, ColorPrisma(VFXCore.Hash01(semillaAura + 2, tick, 319)), 0.8f).noGravity = false;
            }
            try
            {
                byte alaEstilo = val.GetModPlayer<AlasAuroraPlayer>().AlaEstilo;
                if (alaEstilo == 1 || alaEstilo == 0)
                {
                    AlasAuroraLib.Dibujar(val, Spread, Flap, MezclaDia);
                }
                else
                {
                    AlasAuroraLib.Dibujar(val, in AlasCatalogo.De(alaEstilo), Spread, Flap, MezclaDia);
                }
                AlasAuroraLib.DibujarRunas(val, in AlasCatalogo.De(alaEstilo), Spread, Flap);
            }
            catch
            {
                try
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                catch
                {
                }
            }
            return false;
        }
    }
}
