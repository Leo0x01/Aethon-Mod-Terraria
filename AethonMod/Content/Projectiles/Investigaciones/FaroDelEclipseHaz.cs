using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  FaroDelEclipseHaz — v6.174 — EL HAZ CONCENTRADO ORIGINAL DE LA CASA
    //  (la letra: «crea un arma de rayo concentrado original con las
    //  librerías/VFX/texturas del proyecto, lo más impresionante posible,
    //  con al menos 10 a 20 capas de VFX»).
    //
    //  LA CONDUCTA: 24 ticks de carga (el anillo que se cierra sobre el
    //  faro) y después EL HAZ: un conducto de 900 px que sigue al cursor
    //  (giro suave 0,35), golpea a todo lo que viva en la línea y muere al
    //  soltar el botón. La geometría viaja en los ai (edad, ángulo, largo):
    //  el dueño manda, el resto del mundo dibuja lo mismo.
    //
    //  EL RENDER — 26 CAPAS, todas con primitivas DE LA CASA (TajoLib y
    //  VFXCore: cero material ajeno, cero shader nuevo):
    //   LA CARGA   [1] la bola que crece · [2] el anillo que se cierra ·
    //              [3] la cruz que apunta · [4] los doce polvos que caen ·
    //              [5] la aguja adelantada · [6] el resplandor en la mano
    //   LA BOCA    [7] el faro ámbar · [8] la garganta blanca ·
    //              [9] la cruz de lejos
    //   EL CONDUCTO [10-15] seis cintas EMBEBIDAS por la MALLA CONTINUA
    //              (90→5 px: violeta, ámbar, blanco) · [16-19] cuatro
    //              espirales serpenteadas · [20] los tres pulsos que
    //              viajan a la punta · [21] las chispas del conducto
    //   EL IMPACTO [22] la luna del eclipse · [23] el corazón ·
    //              [24] la cruz · [25] los dos anillos de choque
    //   LA PANTALLA [26] el velo ámbar
    // ======================================================================
    public class FaroDelEclipseHaz : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int Carga = 24;
        private const float Alcance = 900f;
        private const int Nodos = 48;

        private static readonly Color Corona = new Color(255, 196, 96);
        private static readonly Color Nucleo = new Color(255, 250, 236);
        private static readonly Color Tinta = new Color(148, 72, 178);

        // el telar (estático: cero GC por frame)
        private static readonly Vector2[] _eje = new Vector2[Nodos + 1];
        private static readonly float[] _sem = new float[Nodos + 1];
        private static readonly float[] _alf = new float[Nodos + 1];
        private static readonly float[] _w = new float[Nodos + 1];
        private static readonly float[] _f = new float[Nodos + 1];
        private static readonly Vector2[] _esp = new Vector2[Nodos + 1];

        private int _base;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active) { Projectile.Kill(); return; }
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 1) _base = Projectile.damage;

            if (Main.myPlayer == Projectile.owner)
            {
                // VIVO mientras el botón de uso esté apretado con el faro en la mano
                bool vivo = p.controlUseItem && !p.dead && !p.CCed && !p.noItems &&
                    p.HeldItem.shoot == Projectile.type;
                if (!vivo) { Projectile.Kill(); return; }

                Vector2 boca = p.MountedCenter + new Vector2(p.direction * 10f, -8f);
                Vector2 alCursor = Main.MouseWorld - boca;
                float deseado = alCursor.LengthSquared() > 1f ? alCursor.ToRotation() : Projectile.ai[1];
                if (Projectile.ai[0] <= 1) Projectile.ai[1] = deseado;
                else Projectile.ai[1] += MathHelper.WrapAngle(deseado - Projectile.ai[1]) * 0.35f;

                float crece = MathHelper.Clamp(Projectile.ai[0] / (float)Carga, 0f, 1f);
                Projectile.ai[2] = MathHelper.Clamp(alCursor.Length(), 60f, Alcance) * (0.06f + 0.94f * crece);

                if (Projectile.ai[0] == 1) Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43);
                if (Projectile.ai[0] == Carga) Terraria.Audio.SoundEngine.PlaySound(SoundID.Item157);

                // el uso del ítem late al ritmo del haz (el patrón del prisma)
                p.channel = true;
                p.itemAnimation = 2;
                p.itemTime = 2;
                p.direction = Math.Cos(Projectile.ai[1]) < 0f ? (sbyte)-1 : (sbyte)1;
                Projectile.netUpdate = true;
            }

            int d = Math.Max(1, (int)(_base * MathHelper.Clamp(Projectile.ai[0] / (float)Carga, 0.25f, 1f)));
            if (d != Projectile.damage) Projectile.damage = d;
            Projectile.timeLeft = 12;
            Projectile.Center = p.MountedCenter + new Vector2(p.direction * 10f, -8f);
        }

        private void Geometria(Player p, out Vector2 boca, out Vector2 punta)
        {
            boca = p.MountedCenter + new Vector2(p.direction * 10f, -8f);
            punta = boca + new Vector2((float)Math.Cos(Projectile.ai[1]), (float)Math.Sin(Projectile.ai[1])) * Projectile.ai[2];
        }

        /// <summary>EL GOLPE EN LÍNEA: todo lo que viva a menos de ~26 px
        /// del conducto recibe el tajo (con la inmunidad local de 10 ticks).</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] < Carga * 0.5f) return false;
            Vector2 a, b;
            Geometria(Main.player[Projectile.owner], out a, out b);
            Vector2 c = targetHitbox.Center.ToVector2();
            Vector2 ab = b - a;
            float l2 = ab.LengthSquared();
            float t = l2 > 0.0001f ? MathHelper.Clamp(Vector2.Dot(c - a, ab) / l2, 0f, 1f) : 0f;
            float dist = Vector2.Distance(c, a + ab * t);
            return dist < 26f + Math.Max(targetHitbox.Width, targetHitbox.Height) * 0.35f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            Player p = Main.player[Projectile.owner];
            Vector2 boca, punta;
            Geometria(p, out boca, out punta);
            boca -= Main.screenPosition;
            punta -= Main.screenPosition;
            float edad = Projectile.ai[0];
            float carga = MathHelper.Clamp(edad / Carga, 0f, 1f);
            float latido = 0.9f + 0.1f * (float)Math.Sin(edad * 0.5f);
            float ang = Projectile.ai[1];

            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    if (carga < 1f)
                    {
                        // [1] LA BOLA DE CARGA
                        Glo(boca, 40f + 80f * carga, TajoLib.Tint(Corona, 0.30f + 0.45f * carga));
                        // [2] EL ANILLO QUE SE CIERRA
                        TajoLib.Tajo(boca, 90f * (1f - carga) + 14f, 0f, MathHelper.TwoPi, 1f, 0.8f,
                            2.5f, Tinta, Nucleo, 7, edad * 0.1f);
                        // [3] LA CRUZ QUE APUNTA
                        Cruz(boca, 70f * carga + 8f, TajoLib.Tint(Nucleo, 0.55f * carga), ang);
                        // [4] LOS DOCE POLVOS QUE CAEN AL FARO
                        for (int i = 0; i < 12; i++)
                        {
                            float fase = (VFXCore.Hash01(i, 3, 1) + edad / 30f) % 1f;
                            float h2 = VFXCore.Hash01(i, 5, 2);
                            Vector2 q = boca + new Vector2((float)Math.Cos(h2 * 6.283f),
                                (float)Math.Sin(h2 * 6.283f)) * 110f * (1f - fase);
                            Glo(q, 5f, TajoLib.Tint(i % 2 == 0 ? Corona : Tinta, 0.6f * fase));
                        }
                        // [5] LA AGUJA ADELANTADA
                        Glo(boca + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 46f * carga,
                            12f, TajoLib.Tint(Nucleo, 0.7f * carga));
                        // [6] EL RESPLANDOR EN LA MANO
                        Glo(p.MountedCenter - Main.screenPosition, 60f * carga, TajoLib.Tint(Corona, 0.25f * carga));
                    }

                    // [7] EL FARO ÁMBAR · [8] LA GARGANTA BLANCA · [9] LA CRUZ DE LEJOS
                    Glo(boca, 50f * latido * (0.3f + carga), TajoLib.Tint(Corona, 0.55f));
                    Glo(boca, 18f * latido, TajoLib.Tint(Nucleo, 0.9f));
                    Cruz(boca, 170f * carga, TajoLib.Tint(Nucleo, 0.30f * latido), ang);

                    if (carga > 0.05f)
                    {
                        Vector2 eje = punta - boca;
                        Vector2 lado = new Vector2(-eje.Y, eje.X);
                        float le = lado.Length();
                        if (le < 0.0001f) le = 1f;
                        lado /= le;
                        ConstruirEje(boca, punta, edad);

                        // [10-15] EL CONDUCTO: seis cintas embebidas (la malla continua)
                        Cinta(90f, Tinta, 0.030f * latido);
                        Cinta(56f, Tinta, 0.055f);
                        Cinta(34f, Corona, 0.09f);
                        Cinta(20f, Corona, 0.15f);
                        Cinta(11f, Nucleo, 0.30f);
                        Cinta(5f, Nucleo, 0.95f);

                        // [16-19] LAS CUATRO ESPIRALES QUE SERPENTEAN
                        for (int s = 0; s < 4; s++)
                        {
                            for (int i = 0; i <= Nodos; i++)
                            {
                                float u = i / (float)Nodos;
                                float g = (float)Math.Sin(Math.PI * u);
                                float onda = (float)Math.Sin(u * 12.566f - edad * 0.8f + s * 1.5708f);
                                _esp[i] = _eje[i] + lado * onda * 17f * g;
                                _w[i] = 1.6f;
                                _f[i] = 0.55f * (0.3f + 0.7f * g);
                            }
                            TajoLib.Cinta(_esp, _w, s % 2 == 0 ? Corona : Tinta, _f);
                        }

                        // [20] LOS TRES PULSOS QUE VIAJAN A LA PUNTA
                        for (int wv = 0; wv < 3; wv++)
                        {
                            float u = (edad / 36f + wv / 3f) % 1f;
                            Glo(boca + eje * u, 30f, TajoLib.Tint(Nucleo, 0.30f * carga));
                        }

                        // [21] LAS CHISPAS DEL CONDUCTO
                        for (int i = 0; i < 10; i++)
                        {
                            float h1 = VFXCore.Hash01(i, 11, (int)edad % 7);
                            float h2 = VFXCore.Hash01(i, 13, 3);
                            Glo(boca + eje * h1 + lado * (h2 - 0.5f) * 70f, 4f,
                                TajoLib.Tint(Corona, 0.6f * h2 * carga));
                        }

                        // [22] LA LUNA DEL ECLIPSE (el creciente de la casa mirando a la boca)
                        TajoLib.TajoDoble(punta, 36f * latido, ang + MathHelper.Pi + 1.1f,
                            ang + MathHelper.Pi - 1.1f, 1f, 0.9f, 2.2f, Corona, Nucleo, 3, edad * 0.1f, 0f, 0f);
                        // [23] EL CORAZÓN · [24] LA CRUZ
                        Glo(punta, 64f * latido, TajoLib.Tint(Corona, 0.6f));
                        Glo(punta, 26f, TajoLib.Tint(Nucleo, 0.95f));
                        Cruz(punta, 150f * latido, TajoLib.Tint(Nucleo, 0.35f), ang);
                        // [25] LOS DOS ANILLOS DE CHOQUE
                        for (int k = 0; k < 2; k++)
                        {
                            float t = (edad / 30f + k * 0.5f) % 1f;
                            TajoLib.Tajo(punta, 20f + 130f * t, 0f, MathHelper.TwoPi, 1f, (1f - t) * 0.5f,
                                2f, Corona, Nucleo, 11 + k, edad * 0.1f);
                        }

                        // [26] EL VELO ÁMBAR DE LA PANTALLA
                        float z = Main.GameViewMatrix.Zoom.X;
                        if (z <= 0.01f) z = 1f;
                        Vector2 medio = Main.screenPosition + new Vector2(Main.screenWidth / (2f * z),
                            Main.screenHeight / (2f * z));
                        Glo(medio, Math.Max(Main.screenWidth, Main.screenHeight) * 1.3f / z,
                            TajoLib.Tint(Corona, 0.05f * carga));
                    }
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { try { VFXCore.CerrarLoteSiAbierto(); } catch { } }
            return false;
        }

        // ==================================================================
        //  LOS EMISORES (la malla continua del eje)
        // ==================================================================

        /// <summary>EL EJE: 49 nodos de la boca a la punta — el conducto
        /// RESPIRA (un vaivén determinista que crece hacia la punta) y se
        /// afila solo en los dos extremos.</summary>
        private static void ConstruirEje(Vector2 a, Vector2 b, float edad)
        {
            Vector2 eje = b - a;
            Vector2 dir = eje.LengthSquared() > 1f ? Vector2.Normalize(eje) : Vector2.UnitX;
            Vector2 lado = new Vector2(-dir.Y, dir.X);
            for (int i = 0; i <= Nodos; i++)
            {
                float u = i / (float)Nodos;
                float vaiven = (float)Math.Sin(u * 9.4248f - edad * 0.55f) * 2.2f * u
                    + (float)Math.Sin(u * 21f + edad * 0.9f) * 0.8f * u;
                _eje[i] = a + eje * u + lado * vaiven;
                float g = (float)Math.Sin(Math.PI * u);
                _sem[i] = 0.35f + 0.65f * Math.Min(1f, g * 3f);
                _alf[i] = 0.35f + 0.65f * Math.Min(1f, g * 2.5f);
            }
        }

        /// <summary>Una cinta del conducto con el semiancho y el alfa del eje.</summary>
        private static void Cinta(float semiancho, Color color, float alfa)
        {
            for (int i = 0; i <= Nodos; i++)
            {
                _w[i] = semiancho * _sem[i];
                _f[i] = alfa * _alf[i];
            }
            TajoLib.Cinta(_eje, _w, color, _f);
        }

        private static void Glo(Vector2 p, float d, Color c)
        {
            if (c.A == 0) return;
            Texture2D t = VFXCore.SoftGlow;
            Main.spriteBatch.Draw(t, p, null, c, 0f, t.Size() * 0.5f, d / t.Width,
                SpriteEffects.None, 0f);
        }

        private static void Cruz(Vector2 p, float largo, Color c, float rot)
        {
            if (c.A == 0) return;
            Main.spriteBatch.Draw(VFXCore.Pixel, p, null, c, rot, new Vector2(0.5f, 0.5f),
                new Vector2(largo, 2.2f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(VFXCore.Pixel, p, null, c, rot, new Vector2(0.5f, 0.5f),
                new Vector2(2.2f, largo), SpriteEffects.None, 0f);
        }
    }
}
