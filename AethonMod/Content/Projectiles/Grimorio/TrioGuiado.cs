// ======================================================================
//  TRIOGUIADO — v6.174 — EL ALBEDRÍO DEL TRÍO (la letra: «todas
//  deberían disparar sin importar compatibilidad O crear un selector
//  tipo checklist para dar inteligencia a los proyectiles (perseguir
//  al enemigo, al cursor, o dejarlos como son originalmente)»).
//
//  QUÉ ES: un GlobalProjectile que LLEVA LA INTELIGENCIA del trío.
//  Cada ranura del trío elige un albedrío en el altar del libro:
//     0 · ORIGINAL  — el proyectil nace y vive EXACTAMENTE como lo
//                     ideó su arma (nada lo toca);
//     1 · PRESA     — el proyectil corrige su rumbo hacia el enemigo
//                     vivo más cercano a la vista (900 px), con un
//                     giro limitado (0,26 rad por tick: persigue, no
//                     se teleporta); sin presa sigue su rumbo propio;
//     2 · CURSOR    — el proyectil vuela hacia el cursor del dueño y
//                     al llegar orbita a su alrededor (48 px) hasta
//                     morir.
//
//  LA LECCIÓN DE LA v6.173 (el client.log del dueño):
//  `System.IndexOutOfRangeException en Terraria.Projectile.AI_001()`.
//  El trío sembraba su puntería en los `ai[0]`/`ai[1]` del proyectil
//  del arma («ángulo × 1000») — y la IA 1 de vanilla (los pernos
//  mágicos teledirigidos) lee `ai[0]` como el ÍNDICE de su presa:
//  `Main.npc[(int)ai[0]]`. Un ángulo×1000 (0..6283) caía fuera de la
//  tabla de NPCs. Por eso: ESTE ARCHIVO JAMÁS ESCRIBE `ai` NI
//  `localAI`. La inteligencia solo DOBLA LA VELOCIDAD del proyectil
//  (después de que SU propia IA haya corrido) y viaja por la red en
//  SendExtraAI — el canal de la casa.
// ======================================================================
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Grimorio
{
    public class TrioGuiado : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // ── LA CAPTURA: solo vive durante el disparo del trío. Todo
        //    proyectil que nazca bajo ella recibe el albedrío de SU
        //    ranura (el arma, sus extras y hasta el rescoldo). ──
        internal static bool Capturando;
        internal static int ModoCaptura;
        internal static int Nacidos;

        /// <summary>0 = original · 1 = persigue · 2 = al cursor.</summary>
        private int _modo;
        private int _tick;
        private int _presa = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!Capturando) return;
            _modo = Math.Clamp(ModoCaptura, 0, 2);
            Nacidos++;
        }

        public override void Unload()
        {
            Capturando = false;
            ModoCaptura = 0;
            Nacidos = 0;
        }

        // ── EL CANAL DE LA RED: el albedrío viaja aparte de los ai — en
        //    los BITS de sobra de un GlobalProjectile (un global NO tiene
        //    canal de bytes propio: solo este canal de bits) ──

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit((_modo & 1) == 1);
            bitWriter.WriteBit((_modo & 2) == 2);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            int modo = (bitReader.ReadBit() ? 1 : 0) | (bitReader.ReadBit() ? 2 : 0);
            _modo = Math.Clamp(modo, 0, 2);
        }

        // ── LA INTELIGENCIA: después de la IA del proyectil ──

        public override void PostAI(Projectile projectile)
        {
            if (_modo == 0 || !projectile.active || !projectile.friendly || projectile.hostile) return;
            float rapidez = projectile.velocity.Length();
            if (float.IsNaN(rapidez) || rapidez < 1f) return;   // los quietos (nubes, prismas) siguen siendo suyos
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;

            _tick++;

            if (_modo == 1)
            {
                // LA PRESA: el enemigo vivo más cercano A LA VISTA
                if (_presa < 0 || _presa >= Main.maxNPCs || !Main.npc[_presa].active ||
                    !Main.npc[_presa].CanBeChasedBy(projectile, false))
                    _presa = PresaMasCercana(projectile);
                if (_presa < 0) return;                        // sin presa: su rumbo original
                GirarHacia(projectile, Main.npc[_presa].Center - projectile.Center, rapidez, 0.26f);
                if (!Main.dedServ && _tick % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Shadowflame,
                        -projectile.velocity * 0.08f, 140, new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
                return;
            }

            if (_modo == 2)
            {
                // EL CURSOR: solo el dueño conoce el SUYO
                if (projectile.owner != Main.myPlayer) return;
                projectile.tileCollide = false;                // el viaje al cursor no muere en una pared
                Vector2 cursor = Main.MouseWorld;
                Vector2 hasta = cursor - projectile.Center;
                float dist = hasta.Length();
                if (EsInvalido(hasta)) return;
                Vector2 quiero = hasta;
                if (dist <= 150f && dist > 0.5f)
                {
                    // LA ÓRBITA: llegó — rodea el cursor a 48 px
                    float ang = hasta.ToRotation() + 0.16f;
                    quiero = cursor + ang.ToRotationVector2() * 48f - projectile.Center;
                }
                GirarHacia(projectile, quiero, rapidez, 0.34f);
                if (_tick > 300 && projectile.timeLeft > 20) projectile.timeLeft = 20;   // la órbita termina
                if (_tick % 12 == 0) projectile.netUpdate = true;
                if (!Main.dedServ && _tick % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                        -projectile.velocity * 0.06f, 120, default, 0.8f);
                    d.noGravity = true;
                }
            }
        }

        private static bool EsInvalido(Vector2 v) => float.IsNaN(v.X) || float.IsNaN(v.Y) ||
            float.IsInfinity(v.X) || float.IsInfinity(v.Y);

        /// <summary>El enemigo vivo más cercano, a 900 px y A LA VISTA
        /// (sin muros en medio).</summary>
        private static int PresaMasCercana(Projectile p)
        {
            int mejor = -1;
            float mejorDist = 900f * 900f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy(p, false)) continue;
                float d = n.Center.DistanceSQ(p.Center);
                if (d >= mejorDist) continue;
                if (!Collision.CanHit(p.Center, 1, 1, n.Center, n.width, n.height)) continue;
                mejor = i;
                mejorDist = d;
            }
            return mejor;
        }

        /// <summary>DOBLA la velocidad hacia `quiero` con un giro máximo
        /// por tick — la rapidez se conserva: solo cambia el rumbo.</summary>
        private static void GirarHacia(Projectile p, Vector2 quiero, float rapidez, float giroMax)
        {
            if (EsInvalido(quiero)) return;
            float largo = quiero.Length();
            if (largo < 1f) return;
            Vector2 rumbo = p.velocity / rapidez;
            Vector2 objetivo = quiero / largo;
            float delta = (float)Math.Acos(MathHelper.Clamp(Vector2.Dot(rumbo, objetivo), -1f, 1f));
            if (delta <= 0.0005f) return;
            if (delta > giroMax)
            {
                float cruz = rumbo.X * objetivo.Y - rumbo.Y * objetivo.X;
                objetivo = rumbo.RotatedBy((cruz >= 0f ? 1f : -1f) * giroMax);
            }
            p.velocity = objetivo * rapidez;
        }
    }

    // ======================================================================
    //  TRIORESCOLDO — v6.174 — LA BRASA DEL ARMA MUDA. Si un arma de la
    //  ranura no logra parir NINGÚN proyectil (un arma de mods cuyo
    //  disparo depende de su propia animación, o una que falla), el
    //  trío no deja el clic en vacío: lanza esta brasa de tinta — un
    //  proyectil mágico pequeño y honrado, con el daño del arma.
    // ======================================================================
    public class TrioRescoldoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.aiStyle = -1;
            Projectile.extraUpdates = 1;
            Projectile.light = 0.6f;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // EL PULSO: la brasa late y siembra su estela
            Projectile.rotation += 0.15f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Shadowflame, -Projectile.velocity * 0.05f, 120, new Color(140, 60, 210), 0.9f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 10; k++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    Main.rand.NextVector2Circular(2.4f, 2.4f), 120, new Color(140, 60, 210), 1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // LA BRASA: estela + halo + núcleo, en el lote aditivo de la casa
            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    SpriteBatch sb = Main.spriteBatch;
                    Texture2D glow = VFXCore.SoftGlow;
                    Vector2 centro = glow.Size() / 2f;
                    Vector2 pos = Projectile.Center - Main.screenPosition;
                    float vida = Math.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
                    float pulso = 1f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f);

                    for (int k = 1; k <= 4; k++)       // la estela (4 pasos atrás)
                    {
                        Vector2 atras = Projectile.oldPos[Math.Min(k, Projectile.oldPos.Length - 1)] +
                            new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;
                        float f = 1f - k / 5f;
                        sb.Draw(glow, atras, null, new Color(120, 80, 210) * (0.30f * f * vida),
                            0f, centro, 0.42f * f, SpriteEffects.None, 0f);
                    }
                    sb.Draw(glow, pos, null, new Color(120, 70, 200) * (0.55f * vida),
                        0f, centro, 0.9f * pulso, SpriteEffects.None, 0f);      // el halo
                    sb.Draw(glow, pos, null, new Color(196, 110, 228) * (0.9f * vida),
                        0f, centro, 0.5f * pulso, SpriteEffects.None, 0f);      // el alma
                    sb.Draw(glow, pos, null, Color.White * (0.95f * vida),
                        0f, centro, 0.17f, SpriteEffects.None, 0f);             // el núcleo
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }
}
