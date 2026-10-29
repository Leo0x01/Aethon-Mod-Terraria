using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using Terraria.Audio;
using AethonMod.Content.Weapons.Definitivas;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class PrismaPrimordialProjectile : ModProjectile
    {
        private const float LargoHaz = 2400f;

        private const int TSentenciaTotal = 192;

        private const int NEspina = 16;

        private int _semilla;

        private bool _muriendo;

        private int _tMuerte;

        private Vector2[] _espina = (Vector2[])(object)new Vector2[16];

        private Vector2[] _espinaPrev = (Vector2[])(object)new Vector2[16];

        private bool _espinaLista;

        // v6.56.3 — EL REMUESTREO DE RENDER (la cura de los picos del Prisma):
        // la FÍSICA verlet vive a 16 nodos (160 px/nodo — la polilínea se leía
        // como quiebres angulares al latiguear: «casi no se notan por la
        // separación, pero los tiene», la letra). La física INTACTA; el RENDER
        // muestrea la misma curva a 44 nodos con Catmull-Rom — la forma vive
        // en la polilínea (la lección v6.55-f) y ahora es SUAVE.
        private static readonly Vector2[] _renderEspina = new Vector2[44];

        // v6.56.4 — LOS POZOS DEL RENDER DEL PRISMA (cero-GC): antes el
        // PreDraw alocaba `new float[44]` (y `new Vector2[16]` en el arranque)
        // cada frame del látigo.
        private static readonly float[] _renderPerf = new float[44];

        private static readonly Color[] _renderColores = new Color[44];

        private static Vector2[] ResampleCatmullRom(Vector2[] src, int nSrc, Vector2[] dest)
        {
            int nDest = dest.Length;
            for (int j = 0; j < nDest; j++)
            {
                float t = j * (nSrc - 1) / (float)(nDest - 1);
                int i = (int)t;
                if (i >= nSrc - 1) { dest[j] = src[nSrc - 1]; continue; }
                float f = t - i;
                Vector2 p0 = src[Math.Max(i - 1, 0)];
                Vector2 p1 = src[i];
                Vector2 p2 = src[Math.Min(i + 1, nSrc - 1)];
                Vector2 p3 = src[Math.Min(i + 2, nSrc - 1)];
                dest[j] = 0.5f * ((2f * p1)
                    + (-p0 + p2) * f
                    + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (f * f)
                    + (-p0 + 3f * p1 - 3f * p2 + p3) * (f * f * f));
            }
            return dest;
        }

        private readonly float[] _ecoRot = new float[4];

        private readonly float[] _ecoConv = new float[4];

        private int _tEco;

        private int _tEncendido = -1;

        private float _convPrev;

        private int _tArco;

        internal SonidoEnLoop raySound;

        public float volume;

        public float pitch;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 40;
            ((Entity)((ModProjectile)this).Projectile).height = 40;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 60;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 14;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void AI()
        {
            //IL_0123: Unknown result type (might be due to invalid IL or missing references)
            //IL_0129: Unknown result type (might be due to invalid IL or missing references)
            //IL_012e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0133: Unknown result type (might be due to invalid IL or missing references)
            //IL_0138: Unknown result type (might be due to invalid IL or missing references)
            //IL_013d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0146: Unknown result type (might be due to invalid IL or missing references)
            //IL_014b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0152: Unknown result type (might be due to invalid IL or missing references)
            //IL_0157: Unknown result type (might be due to invalid IL or missing references)
            //IL_0166: Unknown result type (might be due to invalid IL or missing references)
            //IL_016b: Unknown result type (might be due to invalid IL or missing references)
            //IL_017b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0313: Unknown result type (might be due to invalid IL or missing references)
            //IL_0318: Unknown result type (might be due to invalid IL or missing references)
            //IL_031d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0322: Unknown result type (might be due to invalid IL or missing references)
            //IL_0266: Unknown result type (might be due to invalid IL or missing references)
            //IL_026b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0274: Unknown result type (might be due to invalid IL or missing references)
            //IL_0279: Unknown result type (might be due to invalid IL or missing references)
            //IL_0282: Unknown result type (might be due to invalid IL or missing references)
            //IL_028d: Unknown result type (might be due to invalid IL or missing references)
            //IL_02a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0659: Unknown result type (might be due to invalid IL or missing references)
            //IL_065e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0667: Unknown result type (might be due to invalid IL or missing references)
            //IL_066c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0675: Unknown result type (might be due to invalid IL or missing references)
            //IL_0680: Unknown result type (might be due to invalid IL or missing references)
            //IL_0695: Unknown result type (might be due to invalid IL or missing references)
            //IL_069a: Unknown result type (might be due to invalid IL or missing references)
            //IL_06a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_06a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_06b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_06bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0357: Unknown result type (might be due to invalid IL or missing references)
            //IL_0359: Unknown result type (might be due to invalid IL or missing references)
            //IL_036a: Unknown result type (might be due to invalid IL or missing references)
            //IL_036f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0374: Unknown result type (might be due to invalid IL or missing references)
            //IL_0389: Unknown result type (might be due to invalid IL or missing references)
            //IL_038e: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_06f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_06f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0741: Unknown result type (might be due to invalid IL or missing references)
            //IL_0743: Unknown result type (might be due to invalid IL or missing references)
            //IL_074a: Unknown result type (might be due to invalid IL or missing references)
            //IL_074f: Unknown result type (might be due to invalid IL or missing references)
            //IL_079e: Unknown result type (might be due to invalid IL or missing references)
            //IL_07a0: Unknown result type (might be due to invalid IL or missing references)
            //IL_07a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_07ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_0820: Unknown result type (might be due to invalid IL or missing references)
            //IL_0803: Unknown result type (might be due to invalid IL or missing references)
            //IL_0805: Unknown result type (might be due to invalid IL or missing references)
            //IL_080c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0811: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_040c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0411: Unknown result type (might be due to invalid IL or missing references)
            //IL_041b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0420: Unknown result type (might be due to invalid IL or missing references)
            //IL_0432: Unknown result type (might be due to invalid IL or missing references)
            //IL_0437: Unknown result type (might be due to invalid IL or missing references)
            //IL_044a: Unknown result type (might be due to invalid IL or missing references)
            //IL_044f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0451: Unknown result type (might be due to invalid IL or missing references)
            //IL_0456: Unknown result type (might be due to invalid IL or missing references)
            //IL_0469: Unknown result type (might be due to invalid IL or missing references)
            //IL_046e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0478: Unknown result type (might be due to invalid IL or missing references)
            //IL_047d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0490: Unknown result type (might be due to invalid IL or missing references)
            //IL_0495: Unknown result type (might be due to invalid IL or missing references)
            //IL_049a: Unknown result type (might be due to invalid IL or missing references)
            //IL_03dd: Unknown result type (might be due to invalid IL or missing references)
            //IL_03df: Unknown result type (might be due to invalid IL or missing references)
            //IL_03eb: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ed: Unknown result type (might be due to invalid IL or missing references)
            //IL_0565: Unknown result type (might be due to invalid IL or missing references)
            //IL_0567: Unknown result type (might be due to invalid IL or missing references)
            //IL_0571: Unknown result type (might be due to invalid IL or missing references)
            //IL_0576: Unknown result type (might be due to invalid IL or missing references)
            //IL_057b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0597: Unknown result type (might be due to invalid IL or missing references)
            //IL_0599: Unknown result type (might be due to invalid IL or missing references)
            //IL_05a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_05a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_05af: Unknown result type (might be due to invalid IL or missing references)
            //IL_05c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_05d5: Unknown result type (might be due to invalid IL or missing references)
            //IL_05da: Unknown result type (might be due to invalid IL or missing references)
            //IL_05df: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_05fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_05fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_05fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_0600: Unknown result type (might be due to invalid IL or missing references)
            //IL_0619: Unknown result type (might be due to invalid IL or missing references)
            //IL_061e: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[1] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[1];
            Player val = Main.player[((ModProjectile)this).Projectile.owner];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0)
            {
                _semilla = (int)Main.GameUpdateCount;
            }
            if (num > 1200f)
            {
                raySound?.StopSound();
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            bool flag = ((ModProjectile)this).Projectile.ai[0] >= 1f;
            if (flag)
            {
                if (num > 192f)
                {
                    _muriendo = true;
                }
            }
            else if (!((Entity)val).active || val.dead || !val.channel || val.HeldItem == null || val.HeldItem.type != ModContent.ItemType<PrismaPrimordial>())
            {
                _muriendo = true;
            }
            if (_muriendo)
            {
                _tMuerte++;
                if ((float)_tMuerte > 12f)
                {
                    raySound?.StopSound();
                    ((ModProjectile)this).Projectile.Kill();
                    return;
                }
            }
            // v6.56.1 — LA MIRA EN MP: solo el dueño apunta con su ratón;
            // el servidor y los demás usan el Center/rotation sincronizados.
            if (Main.netMode != 2 && Main.myPlayer == ((ModProjectile)this).Projectile.owner)
            {
                Vector2 val2 = Utils.SafeNormalize(Main.MouseWorld - val.MountedCenter, Vector2.UnitX);
                ((Entity)((ModProjectile)this).Projectile).Center = val.MountedCenter + val2 * 26f + new Vector2(0f, -8f);
                ((ModProjectile)this).Projectile.rotation = Utils.ToRotation(val2);
            }
            float num2 = MathHelper.Clamp(num / (flag ? 70f : 150f), 0f, 1f);
            float num3 = (_muriendo ? MathHelper.Clamp(1f - (float)_tMuerte / 12f, 0f, 1f) : 1f);
            ((ModProjectile)this).Projectile.localAI[1] = MathHelper.Lerp(((ModProjectile)this).Projectile.localAI[1], num2 * num3, 0.12f);
            float num4 = ((ModProjectile)this).Projectile.localAI[1];
            ((ModProjectile)this).Projectile.damage = (int)((float)((ModProjectile)this).Projectile.originalDamage * (0.45f + 0.85f * num4));
            SoundStyle val3;
            if (_convPrev < 0.45f && num4 >= 0.45f && !_muriendo)
            {
                _tEncendido = 0;
                val3 = SoundID.Item122;
                val3 = (val3).WithPitchOffset(0.5f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(0.9f), ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Flash(new Color(255, 248, 235), 0.22f, 10, ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Kick(10f, 14);
            }
            _convPrev = num4;
            if (_tEncendido >= 0)
            {
                _tEncendido++;
                if (_tEncendido > 12)
                {
                    _tEncendido = -1;
                }
            }
            Vector2 val4 = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
            Vector2 val5 = PosFoco(num4);
            if (!Main.dedServ && Main.netMode != 2)
            {
                if (!_espinaLista || num4 < 0.05f)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        _espina[i] = val5 + val4 * (2400f * (float)i / 15f);
                        _espinaPrev[i] = _espina[i];
                    }
                    _espinaLista = true;
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                    {
                        Vector2 val6 = val5 + val4 * (2400f * (float)j / 15f);
                        if (j == 0)
                        {
                            _espina[0] = val6;
                            _espinaPrev[0] = val6;
                            continue;
                        }
                        Vector2 val7 = (_espina[j] - _espinaPrev[j]) * 0.86f;
                        _espinaPrev[j] = _espina[j];
                        ref Vector2 reference = ref _espina[j];
                        reference += val7;
                        ref Vector2 reference2 = ref _espina[j];
                        reference2 += (val6 - _espina[j]) * (0.085f + 0.065f * num4);
                    }
                }
                if (++_tEco >= 3)
                {
                    _tEco = 0;
                    for (int num5 = 3; num5 > 0; num5--)
                    {
                        _ecoRot[num5] = _ecoRot[num5 - 1];
                        _ecoConv[num5] = _ecoConv[num5 - 1];
                    }
                    _ecoRot[0] = ((ModProjectile)this).Projectile.rotation;
                    _ecoConv[0] = num4;
                }
                if (num4 > 0.8f && ++_tArco > 45)
                {
                    _tArco = 0;
                    float num6 = Utils.NextFloat(Main.rand, 0.2f, 0.85f);
                    Vector2 val8 = val5 + val4 * (2400f * num6);
                    float num7 = (Utils.NextBool(Main.rand) ? 1f : (-1f));
                    Vector2 val9 = val8 + new Vector2(0f - val4.Y, val4.X) * num7 * Utils.NextFloat(Main.rand, 34f, 95f) * (0.5f + num6);
                    RayoSistema.Lanzar(RayoParams.Arco(val8, val9), (uint)Main.rand.Next(1, int.MaxValue), val9, val8 - val9, DefinitivasLib.GradienteJuicio(Utils.NextFloat(Main.rand, 1f)), DefinitivasLib.BlancoCalido, Utils.NextFloat(Main.rand, 4f, 7f), Main.rand.Next(5, 9), AnimRayo.Arco, luzEstable: false);
                }
            }
            if (num == 2f)
            {
                val3 = SoundID.Item13;
                val3 = (val3).WithPitchOffset(0f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(0.6f), ((Entity)((ModProjectile)this).Projectile).Center);
            }
            if (flag && num == 180f)
            {
                val3 = SoundID.Item13;
                val3 = (val3).WithPitchOffset(-0.4f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(0.7f), ((Entity)((ModProjectile)this).Projectile).Center);
            }
            if (!Main.dedServ && Main.netMode != 2 && num4 > 0.08f)
            {
                Color val10 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f);
                Lighting.AddLight(val5, (float)(int)(val10).R / 255f * 1.7f * num4, (float)(int)(val10).G / 255f * 1.6f * num4, (float)(int)(val10).B / 255f * 1.4f * num4);
                Lighting.AddLight(val5 + val4 * 800f, (float)(int)(val10).R / 255f * 1.3f * num4, (float)(int)(val10).G / 255f * 1.2f * num4, (float)(int)(val10).B / 255f * 1.05f * num4);
                Lighting.AddLight(val5 + val4 * 1600f, (float)(int)(val10).R / 255f * 1f * num4, (float)(int)(val10).G / 255f * 0.9f * num4, (float)(int)(val10).B / 255f * 0.8f * num4);
                Lighting.AddLight(_espinaLista ? _espina[15] : (val5 + val4 * 2400f), (float)(int)(val10).R / 255f * 1.5f * num4, (float)(int)(val10).G / 255f * 1.35f * num4, (float)(int)(val10).B / 255f * 1.1f * num4);
            }
            if (!Main.dedServ && num4 > 0.7f && num % 4f == 0f)
            {
                OndaLib.Kick(4 + (int)(num4 * 4f), 8);
            }
            HandleSound();
            if (!_muriendo)
            {
                ((ModProjectile)this).Projectile.timeLeft = Math.Max(((ModProjectile)this).Projectile.timeLeft, 30);
            }
        }

        public void HandleSound()
        {
            //IL_003a: Unknown result type (might be due to invalid IL or missing references)
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_004f: Unknown result type (might be due to invalid IL or missing references)
            //IL_005f: Expected O, but got Unknown
            float num = ((ModProjectile)this).Projectile.localAI[1];
            volume = 0.6f * num;
            pitch = MathHelper.Lerp(-0.35f, 0.15f, num);
            if (raySound == null)
            {
                SoundStyle item = SoundID.Item122;
                (item).MaxInstances = 0;
                raySound = new SonidoEnLoop(item, (Func<bool>)new ProjectileAudioTracker(((ModProjectile)this).Projectile).IsActiveAndInGame);
            }
            raySound.PlaySound(() => ((Entity)((ModProjectile)this).Projectile).Center, () => volume, () => pitch);
        }

        private Vector2 PosFoco(float conv)
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0016: Unknown result type (might be due to invalid IL or missing references)
            //IL_002b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            return ((Entity)((ModProjectile)this).Projectile).Center + Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation) * MathHelper.Lerp(190f, 46f, conv);
        }

        private float AnchoHazEn(float p, float conv)
        {
            float x = DefinitivasLib.AnchoCono(p, 0f, 1f);
            float num = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f - p * 2f);
            float num2 = DefinitivasLib.Flicker(Main.GlobalTimeWrappedHourly);
            float num3 = 1f;
            if (_tEncendido >= 0)
            {
                float num4 = (float)_tEncendido / 12f;
                num3 = 1f + 0.14f * MathF.Sin(num4 * (float)Math.PI) * (1f - num4);
            }
            return MathF.Max(x, 0.04f) * (30f + 110f * conv) * num * num2 * num3;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_0028: Unknown result type (might be due to invalid IL or missing references)
            //IL_002d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            //IL_0035: Unknown result type (might be due to invalid IL or missing references)
            //IL_0036: Unknown result type (might be due to invalid IL or missing references)
            //IL_0037: Unknown result type (might be due to invalid IL or missing references)
            //IL_003d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0042: Unknown result type (might be due to invalid IL or missing references)
            //IL_0047: Unknown result type (might be due to invalid IL or missing references)
            //IL_0069: Unknown result type (might be due to invalid IL or missing references)
            //IL_006a: Unknown result type (might be due to invalid IL or missing references)
            //IL_006b: Unknown result type (might be due to invalid IL or missing references)
            float num = ((ModProjectile)this).Projectile.localAI[1];
            if (num <= 0.08f)
            {
                return false;
            }
            Vector2 val = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
            Vector2 val2 = PosFoco(num);
            Vector2 b = val2 + val * 2400f;
            float anchoTotal = (AnchoHazEn(0.15f, num) + AnchoHazEn(0.85f, num)) * 0.5f;
            return SegmentoGordo(targetHitbox, val2, b, anchoTotal);
        }

        internal static bool SegmentoGordo(Rectangle hitbox, Vector2 a, Vector2 b, float anchoTotal)
        {
            //IL_0000: Unknown result type (might be due to invalid IL or missing references)
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Unknown result type (might be due to invalid IL or missing references)
            //IL_000e: Unknown result type (might be due to invalid IL or missing references)
            //IL_001f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0020: Unknown result type (might be due to invalid IL or missing references)
            //IL_0021: Unknown result type (might be due to invalid IL or missing references)
            //IL_0026: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_004d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0052: Unknown result type (might be due to invalid IL or missing references)
            //IL_0054: Unknown result type (might be due to invalid IL or missing references)
            //IL_0055: Unknown result type (might be due to invalid IL or missing references)
            //IL_0063: Unknown result type (might be due to invalid IL or missing references)
            //IL_006a: Unknown result type (might be due to invalid IL or missing references)
            Vector2 val = Utils.Center(hitbox);
            Vector2 val2 = b - a;
            float num = (val2).LengthSquared();
            float num2 = ((num < 1f) ? 0f : MathHelper.Clamp(Vector2.Dot(val - a, val2) / num, 0f, 1f));
            Vector2 val3 = a + val2 * num2;
            return Vector2.Distance(val, val3) <= anchoTotal * 0.5f + MathF.Max(hitbox.Width, hitbox.Height) * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_003e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0043: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_004b: Unknown result type (might be due to invalid IL or missing references)
            //IL_005b: Unknown result type (might be due to invalid IL or missing references)
            //IL_005d: Unknown result type (might be due to invalid IL or missing references)
            //IL_011f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0124: Unknown result type (might be due to invalid IL or missing references)
            //IL_012c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0131: Unknown result type (might be due to invalid IL or missing references)
            //IL_0138: Unknown result type (might be due to invalid IL or missing references)
            //IL_013e: Unknown result type (might be due to invalid IL or missing references)
            //IL_014a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0154: Unknown result type (might be due to invalid IL or missing references)
            //IL_0159: Unknown result type (might be due to invalid IL or missing references)
            //IL_015e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0167: Unknown result type (might be due to invalid IL or missing references)
            //IL_0176: Unknown result type (might be due to invalid IL or missing references)
            //IL_0196: Unknown result type (might be due to invalid IL or missing references)
            //IL_019b: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_020d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0212: Unknown result type (might be due to invalid IL or missing references)
            //IL_0217: Unknown result type (might be due to invalid IL or missing references)
            //IL_0276: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float t = ((ModProjectile)this).Projectile.ai[1];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            float num = ((ModProjectile)this).Projectile.localAI[1];
            Vector2 val = Utils.ToRotationVector2(((ModProjectile)this).Projectile.rotation);
            Vector2 val2 = PosFoco(num);
            float rotation = ((ModProjectile)this).Projectile.rotation;
            DibujarFocoV6560(val2, val, rotation, num, globalTimeWrappedHourly, t);
            VeloPrismatico(0.08f + 0.2f * num + ((_tEncendido >= 0) ? 0.08f : 0f));
            for (int i = 1; i < 4; i++)
            {
                if (!(_ecoConv[i] < 0.25f))
                {
                    float alfaE = 0.24f * (1f - (float)i * 0.34f) * num;
                    DibujarEco(_ecoRot[i], _ecoConv[i], alfaE, globalTimeWrappedHourly);
                }
            }
            VFXCore.Begin();
            for (int j = 0; j < 3; j++)
            {
                float num2 = globalTimeWrappedHourly * (0.7f + (float)j * 0.2f) + (float)j * 2.09f;
                Color c = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.09f + (float)j * 0.33f);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center + new Vector2(0f - val.Y, val.X) * MathF.Sin(num2) * 6f, DefinitivasLib.Alfa(c, 0.3f * num), new Vector2(64f, 64f), num2);
            }
            VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(Color.White, 0.55f * num), new Vector2(38f, 38f));
            VFXCore.FlushAdditive();
            if (num > 0.12f)
            {
                Vector2[] array;
                if (_espinaLista)
                {
                    // v6.56.3 — la física verlet a 16, el RENDER a 44 (Catmull-Rom).
                    array = ResampleCatmullRom(_espina, 16, _renderEspina);
                }
                else
                {
                    // v6.56.4 — cero-GC: la recta de arranque también vive en el
                    // pozo _renderEspina (44 nodos, la misma línea, más densa).
                    for (int k = 0; k < 44; k++)
                    {
                        _renderEspina[k] = val2 + val * (2400f * (float)k / 43f);
                    }
                    array = _renderEspina;
                }
                int nRender = array.Length;
                float[] array2 = _renderPerf;
                Color[] coloresP = _renderColores;
                for (int l = 0; l < nRender; l++)
                {
                    float p = (float)l / (nRender - 1);
                    array2[l] = MathF.Max(AnchoHazEn(p, num) * 0.5f, 0.5f);
                    // EL COLOR QUE VIAJA + EL FADE DE LENTE (la fórmula porNodo
                    // de la vieja HazFusionEspina — la identidad del Prisma:
                    // el Juicio caminando por la espina viva).
                    float lente = 0.85f + 0.30f * MathF.Sin(p * MathHelper.Pi);
                    coloresP[l] = DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f - p * 0.16f), 0.55f * (num * 0.9f) * lente);
                }
                // v6.56.4 — LA ESPINA VIVA POR EL CAMINO GPU DEL RC2 (la letra:
                // «solo debes adaptar el código del rayo concentrado 2…»):
                // HazFusionEspina MUERE para el Prisma — el borde del látigo
                // lo funde ahora el shader POR PÍXEL (envV smoothstep). El
                // Catmull-Rom 44 ya era el insumo perfecto para el strip.
                HazConcentradoLib.Dibujar(array, nRender, array2, coloresP,
                    -globalTimeWrappedHourly * 0.025f);
            }
            VFXCore.RestaurarLoteVanilla();
            return false;
        }

        private void DibujarFocoV6560(Vector2 foco, Vector2 dir, float ang, float conv, float tiempo, float t)
        {
            //IL_0069: Unknown result type (might be due to invalid IL or missing references)
            //IL_006e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0078: Unknown result type (might be due to invalid IL or missing references)
            //IL_0079: Unknown result type (might be due to invalid IL or missing references)
            //IL_007e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_0086: Unknown result type (might be due to invalid IL or missing references)
            //IL_0092: Unknown result type (might be due to invalid IL or missing references)
            //IL_009b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00eb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_014c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0151: Unknown result type (might be due to invalid IL or missing references)
            //IL_0154: Unknown result type (might be due to invalid IL or missing references)
            //IL_016e: Unknown result type (might be due to invalid IL or missing references)
            //IL_017a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0188: Unknown result type (might be due to invalid IL or missing references)
            //IL_0192: Unknown result type (might be due to invalid IL or missing references)
            //IL_0197: Unknown result type (might be due to invalid IL or missing references)
            //IL_01aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_01af: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ed: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0231: Unknown result type (might be due to invalid IL or missing references)
            //IL_0233: Unknown result type (might be due to invalid IL or missing references)
            //IL_0239: Unknown result type (might be due to invalid IL or missing references)
            //IL_0240: Unknown result type (might be due to invalid IL or missing references)
            //IL_0245: Unknown result type (might be due to invalid IL or missing references)
            //IL_0251: Unknown result type (might be due to invalid IL or missing references)
            //IL_025b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0265: Unknown result type (might be due to invalid IL or missing references)
            //IL_026c: Unknown result type (might be due to invalid IL or missing references)
            //IL_027b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0280: Unknown result type (might be due to invalid IL or missing references)
            //IL_0292: Unknown result type (might be due to invalid IL or missing references)
            //IL_042c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0438: Unknown result type (might be due to invalid IL or missing references)
            //IL_0456: Unknown result type (might be due to invalid IL or missing references)
            //IL_0461: Unknown result type (might be due to invalid IL or missing references)
            //IL_046b: Unknown result type (might be due to invalid IL or missing references)
            //IL_047a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0484: Unknown result type (might be due to invalid IL or missing references)
            //IL_0495: Unknown result type (might be due to invalid IL or missing references)
            //IL_0360: Unknown result type (might be due to invalid IL or missing references)
            //IL_0370: Unknown result type (might be due to invalid IL or missing references)
            //IL_0377: Unknown result type (might be due to invalid IL or missing references)
            //IL_037c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0381: Unknown result type (might be due to invalid IL or missing references)
            //IL_0383: Unknown result type (might be due to invalid IL or missing references)
            //IL_039f: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b7: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03db: Unknown result type (might be due to invalid IL or missing references)
            Texture2D softGlow = VFXCore.SoftGlow;
            Texture2D glowOrb = VFXCore.GlowOrb;
            Texture2D ring = VFXCore.Ring;
            if (softGlow == null || glowOrb == null || ring == null)
            {
                return;
            }
            float num = Utils.GetLerpValue(0.02f, 0.25f, conv, true) * (_muriendo ? MathHelper.Clamp(1f - (float)_tMuerte / 12f, 0f, 1f) : 1f);
            Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.07f);
            (c).A = 0;
            Vector2 val = foco - Main.screenPosition;
            Main.EntitySpriteDraw(softGlow, val, (Rectangle?)null, DefinitivasLib.Alfa(c, 0.55f * num), 0f, Utils.Size(softGlow) * 0.5f, 1.8700001f * num, (SpriteEffects)0, 0f);
            Main.EntitySpriteDraw(glowOrb, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * num), 0f, Utils.Size(glowOrb) * 0.5f, 0.8925f * num, (SpriteEffects)0, 0f);
            float num2 = MathF.Pow(conv, 0.8f);
            float num3 = MathHelper.Lerp(150f, 56f, num2) * 0.85f * (1f + 0.04f * MathF.Sin(tiempo * 5.1f));
            Vector2 val2 = VFXCore.RingQuadSize(num3);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.1f), 0.55f * num), tiempo * 0.7f, Utils.Size(ring) * 0.5f, val2, (SpriteEffects)0, 0f);
            Vector2 val3 = VFXCore.RingQuadSize(num3 * 1.38f);
            Main.EntitySpriteDraw(ring, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.3f), 0.3f * num), (0f - tiempo) * 0.5f + 2.1f, Utils.Size(ring) * 0.5f, val3, (SpriteEffects)0, 0f);
            float num4 = Utils.GetLerpValue(0.02f, 0.15f, conv, true) * Utils.GetLerpValue(0.55f, 0.42f, conv, true);
            Main.EntitySpriteDraw(softGlow, val + dir * 624f * num4, (Rectangle?)Utils.Frame(softGlow, 1, 1, 0, 0, 0, 0), DefinitivasLib.Alfa(c, 0.35f * num4), ang, Utils.Size(softGlow) * new Vector2(0.5f, 0.5f), new Vector2(0.06f, 4.42f * num4), (SpriteEffects)0, 0f);
            if (conv > 0.02f && conv < 0.5f && t % 14f < 3f)
            {
                float lerpValue = Utils.GetLerpValue(0f, 0.5f, conv, true);
                int flick = (int)(tiempo * 60f) & 0x3FF;
                int num5 = 1 + (int)(lerpValue * 3.99f);
                for (int i = 0; i < num5; i++)
                {
                    int seed = (int)(t * 0.017f) * 131 + i * 297 + _semilla;
                    float num6 = VFXCore.Hash01(seed, i * 7 + 1, 3) * ((float)Math.PI * 2f);
                    float num7 = 10f + 6f * lerpValue;
                    float num8 = num7 + 26f + 34f * lerpValue;
                    Vector2 start = val + new Vector2(MathF.Cos(num6), MathF.Sin(num6)) * num7;
                    Vector2 end = val + new Vector2(MathF.Cos(num6 + 0.5f), MathF.Sin(num6 + 0.5f)) * num8;
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 1.8700001f, DefinitivasLib.GradienteJuicio(tiempo * 0.07f + (float)i * 0.13f), DefinitivasLib.BlancoCalido, 0.35f + 0.45f * lerpValue, 5, 12f);
                }
            }
            if (_tEncendido >= 0 && _tEncendido < 8 && VFXCore.DestelloFinal != null)
            {
                Main.EntitySpriteDraw(VFXCore.DestelloFinal, val, (Rectangle?)null, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.9f * (1f - (float)_tEncendido / 8f)), ang, Utils.Size(VFXCore.DestelloFinal) * 0.5f, new Vector2(1.6f, 1.6f) * 0.85f * MathF.Max(conv, 0.2f), (SpriteEffects)0, 0f);
            }
        }

        private void DibujarEco(float rotEco, float convEco, float alfaE, float tiempo)
        {
            //IL_000a: Unknown result type (might be due to invalid IL or missing references)
            //IL_000f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_0047: Unknown result type (might be due to invalid IL or missing references)
            //IL_0050: Unknown result type (might be due to invalid IL or missing references)
            //IL_0055: Unknown result type (might be due to invalid IL or missing references)
            //IL_005a: Unknown result type (might be due to invalid IL or missing references)
            //IL_005f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d3: Unknown result type (might be due to invalid IL or missing references)
            //IL_0101: Unknown result type (might be due to invalid IL or missing references)
            //IL_010d: Unknown result type (might be due to invalid IL or missing references)
            if (alfaE <= 0.02f)
            {
                return;
            }
            Vector2 val = Utils.ToRotationVector2(rotEco);
            Vector2 val2 = PosFoco(convEco);
            Texture2D texRuido = DefinitivasLib.TexRuido;
            if (texRuido == null)
            {
                return;
            }
            Vector2[] array = (Vector2[])(object)new Vector2[12];
            float[] array2 = new float[12];
            for (int i = 0; i < 12; i++)
            {
                float num = (float)i / 11f;
                array[i] = val2 + val * (2400f * num) - Main.screenPosition;
                float x = DefinitivasLib.AnchoCono(num, 0f, 1f);
                array2[i] = MathF.Max(x, 0.05f) * (26f + 84f * convEco) * 0.5f;
            }
            Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.07f - 0.08f);
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(array, array2, DefinitivasLib.Alfa(c, 0.3f * alfaE), texRuido, 480f, 1.1f, tiempo);
                RayoStrip.FajaTexturadaEnLote(array, ArraySemis(array2, 0.28f), DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.55f * alfaE), texRuido, 360f, 2f, tiempo, 0.36f, 0.64f);
            }
            finally
            {
                RayoStrip.CerrarLote();
            }
        }

        private static float[] ArraySemis(float[] perfilUnit, float semi)
        {
            float[] array = new float[perfilUnit.Length];
            for (int i = 0; i < perfilUnit.Length; i++)
            {
                array[i] = perfilUnit[i] * semi;
            }
            return array;
        }

        private static void VeloPrismatico(float alfa)
        {
            //IL_0017: Unknown result type (might be due to invalid IL or missing references)
            //IL_001c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            //IL_0049: Unknown result type (might be due to invalid IL or missing references)
            //IL_0056: Unknown result type (might be due to invalid IL or missing references)
            //IL_0065: Unknown result type (might be due to invalid IL or missing references)
            //IL_006a: Unknown result type (might be due to invalid IL or missing references)
            //IL_006c: Unknown result type (might be due to invalid IL or missing references)
            //IL_007a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0088: Unknown result type (might be due to invalid IL or missing references)
            //IL_008d: Unknown result type (might be due to invalid IL or missing references)
            //IL_009d: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_00cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0104: Unknown result type (might be due to invalid IL or missing references)
            //IL_0112: Unknown result type (might be due to invalid IL or missing references)
            //IL_0117: Unknown result type (might be due to invalid IL or missing references)
            //IL_0127: Unknown result type (might be due to invalid IL or missing references)
            //IL_013b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0149: Unknown result type (might be due to invalid IL or missing references)
            //IL_0157: Unknown result type (might be due to invalid IL or missing references)
            //IL_015c: Unknown result type (might be due to invalid IL or missing references)
            //IL_016c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0180: Unknown result type (might be due to invalid IL or missing references)
            //IL_018e: Unknown result type (might be due to invalid IL or missing references)
            //IL_019c: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c0: Unknown result type (might be due to invalid IL or missing references)
            if (!(alfa <= 0.02f))
            {
                float num = Main.screenWidth;
                float num2 = Main.screenHeight;
                Vector2 screenPosition = Main.screenPosition;
                float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
                float num3 = 0.9f + 0.1f * MathF.Sin(globalTimeWrappedHourly * 2.1f);
                Color c = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.05f);
                VFXCore.Begin();
                Color color = DefinitivasLib.Alfa(new Color(30, 20, 46), alfa * 0.62f * num3);
                VFXCore.Quad(new Vector2(screenPosition.X - num * 0.34f, screenPosition.Y + num2 * 0.5f), color, new Vector2(num * 1.1f, num2 * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(screenPosition.X + num * 1.34f, screenPosition.Y + num2 * 0.5f), color, new Vector2(num * 1.1f, num2 * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(screenPosition.X + num * 0.5f, screenPosition.Y - num2 * 0.36f), color, new Vector2(num * 2.4f, num2 * 1.1f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(screenPosition.X + num * 0.5f, screenPosition.Y + num2 * 1.36f), color, new Vector2(num * 2.4f, num2 * 1.1f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(screenPosition.X + num * 0.5f, screenPosition.Y + num2 * 0.5f), DefinitivasLib.Alfa(c, alfa * 0.05f * num3), new Vector2(num * 1.6f, num2 * 1.6f), 0f, VFXCore.SoftGlow);
                VFXCore.FlushAlpha();
            }
        }
    }
}
