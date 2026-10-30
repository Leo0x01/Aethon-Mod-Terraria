using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using System.IO;
using Terraria.Audio;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public class PartidorDeMundosProjectile : ModProjectile
    {
        private const int TContencion = 22;

        private const int TParticion = 34;

        private bool _segundoGolpe;

        private float _ancho;

        private float _techo;

        private float _piso;

        private bool _anclaLista;

        // v6.56.4 — LOS POZOS DEL PARTIDOR (cero-GC, la ley de la casa): la
        // carga (14 nodos) y el corte (20) son fases SECUENCIALES — comparten
        // un solo set (antes: dos `new` por frame + el ArraySemis alocando).
        private static readonly Vector2[] _cacheNodos = new Vector2[20];

        private static readonly float[] _cachePerf = new float[20];

        private static readonly Color[] _cacheColores = new Color[20];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            ((Entity)((ModProjectile)this).Projectile).width = 260;
            ((Entity)((ModProjectile)this).Projectile).height = 1900;
            ((ModProjectile)this).Projectile.tileCollide = false;
            ((ModProjectile)this).Projectile.friendly = true;
            ((ModProjectile)this).Projectile.DamageType = DamageClass.Generic;
            ((ModProjectile)this).Projectile.penetrate = -1;
            ((ModProjectile)this).Projectile.timeLeft = 76;
            ((ModProjectile)this).Projectile.usesLocalNPCImmunity = true;
            ((ModProjectile)this).Projectile.localNPCHitCooldown = 50;
            ((ModProjectile)this).Projectile.netImportant = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_ancho);
            writer.Write(_techo);
            writer.Write(_piso);
            writer.Write(_anclaLista);
            // v6.56.1 — LA CURA DE LA RECURSIÓN (StackOverflow en MP):
            // el base-call del decompilador era una auto-llamada virtual.
            // La base es vacía: no hay nada que llamar.
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _ancho = reader.ReadSingle();
            _techo = reader.ReadSingle();
            _piso = reader.ReadSingle();
            _anclaLista = reader.ReadBoolean();
            // v6.56.2 — LA CURA QUE FALTÓ: esta auto-llamada virtual (artefacto
            // del decompilador) sobrevivió a la ronda .56.1 — el chequeo de
            // idempotencia del script saltó esta cura de pura eliminación
            // (el texto nuevo era subcadena del viejo). En MP: EndOfStream en
            // el primer sync del Partidor. La base es vacía: nada que llamar.
        }

        public override void AI()
        {
            //IL_0197: Unknown result type (might be due to invalid IL or missing references)
            //IL_0156: Unknown result type (might be due to invalid IL or missing references)
            //IL_0166: Unknown result type (might be due to invalid IL or missing references)
            //IL_016b: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0206: Unknown result type (might be due to invalid IL or missing references)
            //IL_020b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0213: Unknown result type (might be due to invalid IL or missing references)
            //IL_0218: Unknown result type (might be due to invalid IL or missing references)
            //IL_0220: Unknown result type (might be due to invalid IL or missing references)
            //IL_022b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0244: Unknown result type (might be due to invalid IL or missing references)
            //IL_0256: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0101: Unknown result type (might be due to invalid IL or missing references)
            //IL_0113: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_02ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_02d6: Unknown result type (might be due to invalid IL or missing references)
            //IL_02e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0373: Unknown result type (might be due to invalid IL or missing references)
            //IL_0392: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a1: Unknown result type (might be due to invalid IL or missing references)
            //IL_03a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_03c5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d4: Unknown result type (might be due to invalid IL or missing references)
            //IL_03d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0309: Unknown result type (might be due to invalid IL or missing references)
            //IL_031b: Unknown result type (might be due to invalid IL or missing references)
            ((ModProjectile)this).Projectile.ai[2] += 1f;
            float num = ((ModProjectile)this).Projectile.ai[2];
            _ = Main.player[((ModProjectile)this).Projectile.owner];
            _ = Main.GlobalTimeWrappedHourly;
            if (num > 1200f)
            {
                ((ModProjectile)this).Projectile.Kill();
                return;
            }
            if (num < 2f)
            {
                if (!_anclaLista)
                {
                    if (!Main.dedServ)
                    {
                        _ancho = Main.screenPosition.X + (float)Main.screenWidth * 0.5f;
                        _techo = Main.screenPosition.Y - 320f;
                        _piso = Main.screenPosition.Y + (float)Main.screenHeight + 320f;
                    }
                    else
                    {
                        Player val = Main.player[((ModProjectile)this).Projectile.owner];
                        Vector2 val2 = ((val != null && ((Entity)val).active) ? ((Entity)val).Center : ((Entity)((ModProjectile)this).Projectile).Center);
                        _ancho = val2.X;
                        _techo = val2.Y - 1100f;
                        _piso = val2.Y + 1100f;
                    }
                    _anclaLista = true;
                    ((ModProjectile)this).Projectile.netUpdate = true;
                }
                ((Entity)((ModProjectile)this).Projectile).Center = new Vector2(_ancho, (_techo + _piso) * 0.5f);
                ((Entity)((ModProjectile)this).Projectile).velocity = Vector2.Zero;
            }
            if (_anclaLista)
            {
                ((Entity)((ModProjectile)this).Projectile).Center = new Vector2(_ancho, (_techo + _piso) * 0.5f);
            }
            SoundStyle val3;
            if (num == 22f)
            {
                ((ModProjectile)this).Projectile.ai[0] = 1f;
                ((ModProjectile)this).Projectile.ai[2] = 0f;
                _segundoGolpe = false;
                val3 = SoundID.Item122;
                val3 = (val3).WithPitchOffset(-0.2f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(1.2f), ((Entity)((ModProjectile)this).Projectile).Center);
                val3 = SoundID.Item88;
                val3 = (val3).WithPitchOffset(-0.5f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(0.9f), ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Flash(new Color(255, 250, 235), 0.44f, 14, ((Entity)((ModProjectile)this).Projectile).Center);
                OndaLib.Kick(14f, 22);
            }
            bool num2 = ((ModProjectile)this).Projectile.ai[0] >= 1f;
            if (num2 && !_segundoGolpe && ((ModProjectile)this).Projectile.ai[2] >= 8f)
            {
                _segundoGolpe = true;
                val3 = SoundID.Item88;
                val3 = (val3).WithPitchOffset(-0.7f);
                DefinitivasLib.Sonar((val3).WithVolumeScale(0.8f), ((Entity)((ModProjectile)this).Projectile).Center);
                if (!Main.dedServ && Main.netMode != 2)
                {
                    OndaLib.Flash(new Color(255, 244, 210), 0.26f, 10, ((Entity)((ModProjectile)this).Projectile).Center);
                    OndaLib.Kick(9f, 14);
                }
            }
            if (num2 && !Main.dedServ && Main.netMode != 2 && ((ModProjectile)this).Projectile.ai[2] < 34f)
            {
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center, 2.2f, 2f, 1.5f);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + new Vector2(0f, -700f), 1.6f, 1.4f, 1.1f);
                Lighting.AddLight(((Entity)((ModProjectile)this).Projectile).Center + new Vector2(0f, 700f), 1.6f, 1.4f, 1.1f);
                if (((ModProjectile)this).Projectile.ai[2] % 6f == 0f)
                {
                    OndaLib.Kick(9f, 10);
                }
            }
            if (num2 && ((ModProjectile)this).Projectile.ai[2] > 50f)
            {
                ((ModProjectile)this).Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //IL_0060: Unknown result type (might be due to invalid IL or missing references)
            if (((ModProjectile)this).Projectile.ai[0] >= 1f && ((ModProjectile)this).Projectile.ai[2] < 42f && _anclaLista)
            {
                Rectangle val = new Rectangle((int)(_ancho - 100f), (int)_techo, 200, (int)(_piso - _techo));
                return (val).Intersects(targetHitbox);
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //IL_0cf4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d0a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d0f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d14: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d19: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d1e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d7b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d80: Unknown result type (might be due to invalid IL or missing references)
            //IL_0131: Unknown result type (might be due to invalid IL or missing references)
            //IL_0133: Unknown result type (might be due to invalid IL or missing references)
            //IL_0137: Unknown result type (might be due to invalid IL or missing references)
            //IL_0143: Unknown result type (might be due to invalid IL or missing references)
            //IL_0148: Unknown result type (might be due to invalid IL or missing references)
            //IL_014d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0152: Unknown result type (might be due to invalid IL or missing references)
            //IL_0157: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0574: Unknown result type (might be due to invalid IL or missing references)
            //IL_0df1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e0d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e12: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e17: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e1c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e21: Unknown result type (might be due to invalid IL or missing references)
            //IL_0195: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_0212: Unknown result type (might be due to invalid IL or missing references)
            //IL_0228: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e48: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e52: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e37: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e41: Unknown result type (might be due to invalid IL or missing references)
            //IL_0681: Unknown result type (might be due to invalid IL or missing references)
            //IL_0686: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_06b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_06bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_06cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_06d2: Unknown result type (might be due to invalid IL or missing references)
            //IL_06d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_06ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_06f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_06fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_070c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0713: Unknown result type (might be due to invalid IL or missing references)
            //IL_0728: Unknown result type (might be due to invalid IL or missing references)
            //IL_072d: Unknown result type (might be due to invalid IL or missing references)
            //IL_073a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0749: Unknown result type (might be due to invalid IL or missing references)
            //IL_0750: Unknown result type (might be due to invalid IL or missing references)
            //IL_0757: Unknown result type (might be due to invalid IL or missing references)
            //IL_076c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0771: Unknown result type (might be due to invalid IL or missing references)
            //IL_077e: Unknown result type (might be due to invalid IL or missing references)
            //IL_078d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0794: Unknown result type (might be due to invalid IL or missing references)
            //IL_0285: Unknown result type (might be due to invalid IL or missing references)
            //IL_0287: Unknown result type (might be due to invalid IL or missing references)
            //IL_028b: Unknown result type (might be due to invalid IL or missing references)
            //IL_02bc: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_02c6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e57: Unknown result type (might be due to invalid IL or missing references)
            //IL_07f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0807: Unknown result type (might be due to invalid IL or missing references)
            //IL_07b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_07b5: Unknown result type (might be due to invalid IL or missing references)
            //IL_07c2: Unknown result type (might be due to invalid IL or missing references)
            //IL_07d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_07d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e77: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e79: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e83: Unknown result type (might be due to invalid IL or missing references)
            //IL_05cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_05cf: Unknown result type (might be due to invalid IL or missing references)
            //IL_05d3: Unknown result type (might be due to invalid IL or missing references)
            //IL_05df: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e4: Unknown result type (might be due to invalid IL or missing references)
            //IL_05e9: Unknown result type (might be due to invalid IL or missing references)
            //IL_05ee: Unknown result type (might be due to invalid IL or missing references)
            //IL_05f3: Unknown result type (might be due to invalid IL or missing references)
            //IL_03ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b0: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b4: Unknown result type (might be due to invalid IL or missing references)
            //IL_03b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_03e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_03f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_061a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0624: Unknown result type (might be due to invalid IL or missing references)
            //IL_0609: Unknown result type (might be due to invalid IL or missing references)
            //IL_0613: Unknown result type (might be due to invalid IL or missing references)
            //IL_0850: Unknown result type (might be due to invalid IL or missing references)
            //IL_0852: Unknown result type (might be due to invalid IL or missing references)
            //IL_0856: Unknown result type (might be due to invalid IL or missing references)
            //IL_085b: Unknown result type (might be due to invalid IL or missing references)
            //IL_086e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0873: Unknown result type (might be due to invalid IL or missing references)
            //IL_0875: Unknown result type (might be due to invalid IL or missing references)
            //IL_0877: Unknown result type (might be due to invalid IL or missing references)
            //IL_0890: Unknown result type (might be due to invalid IL or missing references)
            //IL_089f: Unknown result type (might be due to invalid IL or missing references)
            //IL_08b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0310: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0629: Unknown result type (might be due to invalid IL or missing references)
            //IL_08c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_08ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_08e0: Unknown result type (might be due to invalid IL or missing references)
            //IL_08ef: Unknown result type (might be due to invalid IL or missing references)
            //IL_0315: Unknown result type (might be due to invalid IL or missing references)
            //IL_0317: Unknown result type (might be due to invalid IL or missing references)
            //IL_0319: Unknown result type (might be due to invalid IL or missing references)
            //IL_0323: Unknown result type (might be due to invalid IL or missing references)
            //IL_0332: Unknown result type (might be due to invalid IL or missing references)
            //IL_063a: Unknown result type (might be due to invalid IL or missing references)
            //IL_063c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0646: Unknown result type (might be due to invalid IL or missing references)
            //IL_0348: Unknown result type (might be due to invalid IL or missing references)
            //IL_034a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0351: Unknown result type (might be due to invalid IL or missing references)
            //IL_0360: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b2c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b41: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b46: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b4b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b57: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b79: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b7e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b83: Unknown result type (might be due to invalid IL or missing references)
            //IL_0b95: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bb7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bbc: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bc1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bdd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0c07: Unknown result type (might be due to invalid IL or missing references)
            //IL_0c0c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0c11: Unknown result type (might be due to invalid IL or missing references)
            //IL_0c43: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a0f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a1f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a29: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a2e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a43: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a48: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a52: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a57: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a9f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0aac: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ab1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0abe: Unknown result type (might be due to invalid IL or missing references)
            if (Main.dedServ || Main.netMode == 2)
            {
                return false;
            }
            float num = ((ModProjectile)this).Projectile.ai[2];
            float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
            bool flag = ((ModProjectile)this).Projectile.ai[0] >= 1f;
            float num2 = (flag ? (0.85f + 0.15f * MathF.Sin(globalTimeWrappedHourly * 12f)) : 1f);
            SombrasLib.Vignette(flag ? (MathHelper.Clamp(1f - ((ModProjectile)this).Projectile.ai[2] / 44f, 0f, 1f) * 0.3f * num2) : (0.2f + 0.22f * (num / 22f)));
            if (!flag)
            {
                float num3 = num / 22f;
                Vector2 val = new Vector2(_ancho, _techo);
                Vector2 val2 = new Vector2(_ancho, _piso);
                // v6.56.4 — LA CARGA POR EL CAMINO GPU DEL RC2 (la letra:
                // «solo debes adaptar el código del rayo concentrado 2…»):
                // las dos FajaTextura directas (la senoide de telegrafía)
                // MUEREN — la senoide viste el Oro y el shader pone el núcleo
                // blanco (la ley de los 6 nodos/onda q·9 se conserva: la
                // misma carne, sin dientes). Coordenadas de MUNDO (el contrato
                // de la librería — sin `- Main.screenPosition`).
                Vector2[] array = _cacheNodos;
                float[] array2 = _cachePerf;
                Color[] coloresC = _cacheColores;
                for (int i = 0; i < 14; i++)
                {
                    float num4 = (float)i / 13f;
                    // v6.56.5 — LA AUDITORÍA A (MEDIO-1): la caída del shader
                    // apagaba la MITAD INFERIOR de la telegrafía (el extremo
                    // Coord.x→1 era el PISO — donde va a caer el juicio; la
                    // vieja CPU no tenía NINGUNA caída). Espina invertida
                    // (nodo 0 = val2 = piso) y la senoide evaluada en la
                    // posición física original p = 1−num4 (el vaivén sigue
                    // creciendo hacia el piso, como siempre); uTime pasa a
                    // POSITIVO (la raíz ahora es el piso: el hervor sigue
                    // bajando hacia él, como en la .56.4).
                    float p = 1f - num4;
                    // v6.56.3 — LA LEY DE LOS 6 NODOS/ONDA: la senoide vivía a
                    // q·20 (3.18 ciclos en 14 nodos = 4.4 nodos/onda = SIERRA,
                    // la autopsia de la 3ª ronda de picos). A q·9 son 9.8 nodos
                    // por onda: la misma carne latiendo, SIN dientes.
                    float num5 = MathF.Sin(globalTimeWrappedHourly * 30f + p * 9f) * (2f + 8f * num3) * p;
                    array[i] = Vector2.Lerp(val2, val, num4) + new Vector2(num5, 0f);
                    array2[i] = 2f + 5f * num3;
                    coloresC[i] = DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * (0.5f + 0.5f * num3));
                }
                HazConcentradoLib.Dibujar(array, 14, array2, coloresC,
                    globalTimeWrappedHourly * 0.025f * (0.8f + num3 * 2.4f));
                VFXCore.Begin();
                int num7 = 1 + (int)(num3 * 4.999f);
                for (int j = 0; j < 5; j++)
                {
                    float num8 = 0.12f + (float)j * 0.19f;
                    Vector2 position = Vector2.Lerp(val, val2, num8) + new Vector2(MathF.Sin(globalTimeWrappedHourly * 26f + num8 * 20f) * (2f + 8f * num3) * num8, 0f);
                    bool num9 = j < num7;
                    float num10 = (num9 ? (0.55f + 0.35f * MathF.Sin(globalTimeWrappedHourly * 6f + (float)j)) : 0.08f);
                    Color c = (num9 ? DefinitivasLib.GradienteJuicio((float)j * 0.2f + globalTimeWrappedHourly * 0.1f) : DefinitivasLib.Oro);
                    VFXCore.Quad(position, DefinitivasLib.Alfa(c, num10 * 0.35f), new Vector2(70f, 70f));
                    if (DefinitivasLib.TexStar != null)
                    {
                        VFXCore.Quad(position, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, num10), new Vector2(34f, 34f), globalTimeWrappedHourly * 1.5f + (float)j, DefinitivasLib.TexStar);
                    }
                }
                for (int k = 0; k < 3; k++)
                {
                    float num11 = DefinitivasLib.Frac(globalTimeWrappedHourly * (0.5f + num3 * 1.8f) + (float)k / 3f);
                    VFXCore.Quad(Vector2.Lerp(val2, val, num11), DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.35f * (1f - num11 * 0.4f) * (0.4f + 0.6f * num3)), new Vector2(12f, 12f));
                }
                VFXCore.FlushAdditive();
            }
            if (flag)
            {
                float num12 = MathHelper.Clamp(((ModProjectile)this).Projectile.ai[2] / 34f, 0f, 1f);
                float num13 = DefinitivasLib.DeGolpe(MathHelper.Clamp(((ModProjectile)this).Projectile.ai[2] / 5f, 0f, 1f));
                float num14 = MathHelper.Clamp(1f - (((ModProjectile)this).Projectile.ai[2] - 34f) / 16f, 0f, 1f);
                float num15 = num13 * num14;
                Vector2 val3 = new Vector2(_ancho, _techo);
                Vector2 val4 = new Vector2(_ancho, _piso);
                // v6.56.4 — EL CORTE POR EL CAMINO GPU DEL RC2 (cero-GC: los
                // pozos compartidos con la carga): HazFusionEspina MUERE para
                // el Partidor — el borde vertical del juicio lo funde ahora el
                // shader POR PÍXEL.
                // v6.56.5 — LA AUDITORÍA A (MEDIO-1): mismo hallazgo que la
                // carga — la caída apagaba la MITAD INFERIOR del CORTE (el
                // juicio muerde el piso, no el techo). Espina invertida (nodo
                // 0 = val4 = piso); las fórmulas simétricas (perfil, lente)
                // no cambian; el color se evalúa en p = 1−num17 (viaja hacia
                // el piso como siempre) y uTime se NIEGA (el hervor sigue
                // subiendo hacia el techo — la raíz ahora es el piso).
                Vector2[] array3 = _cacheNodos;
                float[] array4 = _cachePerf;
                Color[] coloresJ = _cacheColores;
                float num16 = DefinitivasLib.Flicker(globalTimeWrappedHourly);
                for (int l = 0; l < 20; l++)
                {
                    float num17 = (float)l / 19f;
                    float p = 1f - num17;
                    array3[l] = Vector2.Lerp(val4, val3, num17);
                    float num18 = MathF.Sin(num17 * (float)Math.PI) * 1.4f - 0.25f;
                    float num19 = MathHelper.SmoothStep(0f, 0.75f, MathHelper.Clamp(num18, 0f, 0.75f));
                    array4[l] = (0.25f + 0.75f * num19) * 150f * num15 * num16;
                    // EL COLOR QUE VIAJA + EL FADE DE LENTE (la fórmula porNodo
                    // de la vieja HazFusionEspina — la identidad del corte,
                    // evaluada en la posición física original p).
                    float lente = 0.85f + 0.30f * MathF.Sin(num17 * MathHelper.Pi);
                    coloresJ[l] = DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f - p * 0.16f), 0.55f * num15 * lente);
                }
                HazConcentradoLib.Dibujar(array3, 20, array4, coloresJ,
                    -globalTimeWrappedHourly * 0.025f * 1.15f);
                for (int m = -1; m <= 1; m += 2)
                {
                    Vector2[] array5 = (Vector2[])(object)new Vector2[20];
                    for (int n = 0; n < 20; n++)
                    {
                        float num20 = (float)n / 19f;
                        float num21 = (float)m * (90f + 40f * MathF.Sin(num20 * (float)Math.PI));
                        array5[n] = Vector2.Lerp(val3, val4, num20) + new Vector2(num21, 0f) - Main.screenPosition;
                    }
                    Color halo = ((m < 0) ? DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.5f) : DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.5f));
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(array5, 26f * num15, halo, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.5f), 0.4f * num15, StormTaper.Flat, 9 + m, 0, suavizar: false, crackle: false, vena: false);
                    }
                    finally
                    {
                        RayoStrip.CerrarLote();
                    }
                }
                Color c2 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.09f);
                float num22 = 0.8f + 0.2f * MathF.Sin(globalTimeWrappedHourly * 12f);
                VFXCore.Begin();
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(c2, 0.2f * num15), new Vector2(1300f, 1300f) * num15 * num22);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(c2, 0.34f * num15), new Vector2(700f, 700f) * num15);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * num15), new Vector2(380f, 380f) * num15 * num22);
                VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(Color.White, 0.65f * num15), new Vector2(220f, 220f) * num15);
                if (VFXCore.DestelloFinal != null)
                {
                    VFXCore.Quad(((Entity)((ModProjectile)this).Projectile).Center, DefinitivasLib.Alfa(Color.White, 0.9f * num15), new Vector2(620f, 620f) * num15, num12 * 0.8f, VFXCore.DestelloFinal);
                }
                DefinitivasLib.PinchosDifraccion(((Entity)((ModProjectile)this).Projectile).Center, 0f, 900f * num15, DefinitivasLib.BlancoCalido, 0.5f * num15, globalTimeWrappedHourly);
                VFXCore.FlushAdditive();
                VFXCore.Begin();
                for (int num23 = 0; num23 < 3; num23++)
                {
                    float num24 = DefinitivasLib.Frac(globalTimeWrappedHourly * (0.42f + (float)num23 * 0.07f) + (float)num23 * 0.37f);
                    Vector2 position2 = Vector2.Lerp(val3, val4, num24);
                    Color c3 = DefinitivasLib.GradienteJuicio(globalTimeWrappedHourly * 0.07f + (float)num23 * 0.2f);
                    VFXCore.Quad(position2, DefinitivasLib.Alfa(c3, 0.55f * num15 * (1f - num24 * 0.35f)), new Vector2(26f, 220f) * (0.5f + num15 * 0.5f));
                    if (VFXCore.GlowOrb != null)
                    {
                        VFXCore.Quad(position2, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.65f * num15 * (1f - num24)), new Vector2(18f, 120f), 0f, VFXCore.GlowOrb);
                    }
                }
                VFXCore.FlushAdditive();
                if (_segundoGolpe && ((ModProjectile)this).Projectile.ai[2] < 26f)
                {
                    int num25 = (int)(((ModProjectile)this).Projectile.ai[2] - 8f);
                    float num26 = MathHelper.Clamp(1f - (float)num25 / 18f, 0f, 1f);
                    Vector2 val5 = default(Vector2);
                    for (int num27 = 0; num27 < 4; num27++)
                    {
                        int seed = 4700 + num27 * 53;
                        float x = VFXCore.Hash01(seed, 11, num27) * ((float)Math.PI * 2f);
                        float num28 = (140f + 190f * VFXCore.Hash01(seed, 7, num27)) * num26;
                        val5 = new Vector2(_ancho, _piso + (_techo - _piso) * (0.25f + 0.5f * VFXCore.Hash01(seed, 5, num27)));
                        Vector2[] array6 = (Vector2[])(object)new Vector2[5];
                        float[] array7 = new float[5];
                        for (int num29 = 0; num29 < 5; num29++)
                        {
                            float num30 = (float)num29 / 4f;
                            array6[num29] = val5 + new Vector2(MathF.Cos(x), MathF.Sin(x)) * (num28 * num30) + new Vector2(0f, -26f * num30 * num30) - Main.screenPosition;
                            array7[num29] = MathF.Max(1f - num30 * 0.85f, 0.06f);
                        }
                        RayoStrip.AbrirLote();
                        try
                        {
                            RayoStrip.FilamentoEnLote(array6, 9f * num26, DefinitivasLib.Alfa(DefinitivasLib.GradienteJuicio((float)num27 * 0.24f), 0.6f * num26), DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.5f * num26), 0.55f * num26, StormTaper.Flat, seed, 0, suavizar: false, crackle: false, vena: true, null, array7);
                        }
                        finally
                        {
                            RayoStrip.CerrarLote();
                        }
                    }
                }
                if (Main.spriteBatch != null && num12 < 1f)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        OndaLib.Shock(Main.spriteBatch, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, num12, 1500f, DefinitivasLib.Oro, 1f, 3, 20f, OndaFalloff.Quadratic, chromatic: true);
                        OndaLib.Shock(Main.spriteBatch, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, num12 * 0.75f, 1100f, DefinitivasLib.BlancoCalido, 0.7f, 4, 12f);
                        OndaLib.Shock(Main.spriteBatch, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, MathF.Max(0f, num12 - 0.25f), 800f, DefinitivasLib.Ambar, 0.5f, 8, 8f, OndaFalloff.Slow);
                        if (_segundoGolpe)
                        {
                            OndaLib.Shock(Main.spriteBatch, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition, MathHelper.Clamp((((ModProjectile)this).Projectile.ai[2] - 8f) / 22f, 0f, 1f), 1900f, DefinitivasLib.BlancoCalido, 0.45f, 6, 10f, OndaFalloff.Slow);
                        }
                        Main.spriteBatch.End();
                    }
                    catch
                    {
                        VFXCore.CerrarLoteSiAbierto();
                    }
                    VFXCore.ReabrirLoteVanilla();
                }
                for (int num31 = -1; num31 <= 1; num31 += 2)
                {
                    Vector2[] array8 = (Vector2[])(object)new Vector2[14];
                    float[] array9 = new float[14];
                    float num32 = 560f * MathHelper.Clamp(num12 * 2.2f, 0f, 1f) * num14;
                    for (int num33 = 0; num33 < 14; num33++)
                    {
                        float num34 = (float)num33 / 13f;
                        float num35 = MathF.Sin(num34 * 9f + (float)num31 * 3f + globalTimeWrappedHourly * 2f) * 6f;
                        array8[num33] = ((Entity)((ModProjectile)this).Projectile).Center + new Vector2((float)num31 * (60f + num32 * num34), num35) - Main.screenPosition;
                        array9[num33] = 1f - num34 * 0.8f;
                        if (array9[num33] < 0.1f)
                        {
                            array9[num33] = 0.1f;
                        }
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(array8, 34f * num15, DefinitivasLib.GradienteJuicio((float)num31 * 0.25f + globalTimeWrappedHourly * 0.1f), DefinitivasLib.BlancoCalido, 0.55f * num15, StormTaper.Flat, 12 + num31, 0, suavizar: false, crackle: true, vena: true, null, array9);
                    }
                    finally
                    {
                        RayoStrip.CerrarLote();
                    }
                    Vector2[] array10 = (Vector2[])(object)new Vector2[14];
                    for (int num36 = 0; num36 < 14; num36++)
                    {
                        float num37 = (float)num36 / 13f;
                        float num38 = MathF.Sin(num37 * 7f - (float)num31 * 5f + globalTimeWrappedHourly * 1.6f) * 4f;
                        array10[num36] = ((Entity)((ModProjectile)this).Projectile).Center + new Vector2((float)num31 * (150f + 360f * num12 * num37), num38) - Main.screenPosition;
                    }
                    Color halo2 = ((num31 < 0) ? DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.55f) : DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.55f));
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(array10, 16f * num15 * (1f - num12 * 0.5f), halo2, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.4f), 0.3f * num15, StormTaper.Flat, 16 + num31, 0, suavizar: false, crackle: false, vena: true, null, array9);
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

        // v6.56.4 — ArraySemis MUERE (0 consumidores tras el porte al camino
        // GPU; alocaba un array nuevo por llamada — deuda GC de la carga).
    }
}
