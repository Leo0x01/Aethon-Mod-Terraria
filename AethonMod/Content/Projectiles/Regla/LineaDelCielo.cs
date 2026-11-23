using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Regla
{
    // =====================================================================
    //  LineaDelCielo — v6.168 — LA LÍNEA (el cuerpo del arma La Regla
    //  del Cielo — la letra: «lineas en el aire a gran velocidad,
    //  lineas con glow, desenfoque y un poco de turbulencia al final,
    //  estas lineas empezaran con un brillo de arriba hacia abajo o de
    //  abajo hacia arriba de forma aleatoria»).
    //
    //  LOS TRES ACTOS DE LA LÍNEA (~20 ticks de vida):
    //   ACTO 1 — EL DESTELLO INICIAL (ticks 0-8): un núcleo de luz
    //   BRILLA en uno de los extremos (al azar: arriba o abajo) y
    //   CORRE hacia el otro — «a gran velocidad»: el brillo recorre
    //   los 320 px en 8 ticks (40 px/tick, el latigazo).
    //   ACTO 2 — EL CUERPO (todo el rato): la línea larga y delgada —
    //   núcleo blanco de 2-3 px + EL GLOW (halo aditivo violeta-cielo)
    //   + EL DESENFOQUE (dos capas más anchas y tenues a los lados,
    //   el blur barato de la casa).
    //   ACTO 3 — LA TURBULENCIA AL FINAL (ticks 8-20): el EXTREMO
    //   donde MURIÓ el brillo se ONDEA (la punta tiembla ±3-4 px con
    //   ruido senoidal) y SUELTA 2-3 chispas por tick — la línea se
    //   disipa desde su punta turbulenta.
    //
    //  EL GOLPE: UNA pasada — al nacer, todo lo que vive en el
    //  rectángulo delgado (6 × 320) recibe el daño (la columna del
    //  cielo, sin i-frames compartidos).
    // =====================================================================
    public class LineaDelCielo : ModProjectile
    {
        private const float ALTO = 320f;      // «largas en longitud»
        private const float ANCHO = 6f;      // «muy delgadas»
        private const int TICKS_DESTELLO = 8;   // el latigazo del brillo
        private const int VIDA = 20;

        /// <summary>¿El brillo SUBE (1) o BAJA (0)? — al azar al nacer.</summary>
        private bool Sube => Projectile.ai[0] >= 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // UNA sola pasada
            Projectile.alpha = 255;               // invisible al pipeline vanilla
            Projectile.netImportant = false;
        }

        public override void AI()
        {
            // LA TURBULENCIA FINAL: desde que el destello llegó a su
            // extremo, la PUNTA ondea y escupe chispas (la letra: «un
            // poco de turbulencia al final»).
            if (Projectile.timeLeft <= VIDA - TICKS_DESTELLO && Main.rand.NextBool())
            {
                float puntaY = Sube ? -ALTO * 0.5f : ALTO * 0.5f;
                Vector2 punta = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-3f, 3f), puntaY + Main.rand.NextFloat(-6f, 6f));
                Dust d = Dust.NewDustPerfect(punta, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-1.4f, 1.4f),
                        Sube ? Main.rand.NextFloat(-1.8f, -0.3f) : Main.rand.NextFloat(0.3f, 1.8f)),
                    130, new Color(170, 210, 255), 0.55f);
                d.noGravity = true;
            }
        }

        public override void OnSpawn(IEntitySource fuente)
        {
            // EL GOLPE DE LA COLUMNA: una pasada al nacer — todo lo que
            // vive dentro del rectángulo delgado recibe su daño (el
            // rectángulo real de la línea: ancho 8, alto 320).
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Rectangle columna = new Rectangle(
                (int)(Projectile.Center.X - ANCHO), (int)(Projectile.Center.Y - ALTO * 0.5f),
                (int)(ANCHO * 2), (int)ALTO);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.friendly ||
                    n.townNPC || n.immortal || n.dontTakeDamage) continue;
                Rectangle cuerpo = new Rectangle((int)n.position.X, (int)n.position.Y,
                    n.width, n.height);
                if (!columna.Intersects(cuerpo)) continue;
                global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n, Projectile.damage, 1.2f);
            }
        }

        public override bool PreDraw(ref Color luzColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    Vector2 centro = Projectile.Center - Main.screenPosition;
                    float t = Main.GlobalTimeWrappedHourly;
                    int edad = VIDA - Projectile.timeLeft;
                    float vidaAlfa = Projectile.timeLeft / (float)VIDA;   // 1→0

                    // ========================================================
                    //  EL CUERPO — larga y delgada, con GLOW y DESENFOQUE:
                    //  · el NÚCLEO (2-3 px, blanco-cielo)
                    //  · el GLOW (la capa aditiva violeta-cielo, 8 px)
                    //  · EL DESENFOQUE (dos capas laterales 14/22 px, muy
                    //    tenues — el blur barato: el halo que sangra)
                    // ========================================================
                    float ondula = MathF.Sin(t * 3.7f + Projectile.whoAmI) * 0.8f;
                    Vector2 tam = new Vector2(2.6f, ALTO * vidaAlfa + 40f);
                    // EL NÚCLEO
                    Quad(VFXCore.SoftGlow, centro + new Vector2(ondula * 0.4f, 0f), tam, 0f,
                        VFXCore.TintAlfa(new Color(235, 245, 255), 0.95f * vidaAlfa));
                    // EL GLOW (la capa que la hace LÍNEA DE LUZ)
                    Quad(VFXCore.SoftGlow, centro, new Vector2(8f, ALTO * vidaAlfa + 52f), 0f,
                        VFXCore.TintAlfa(new Color(140, 190, 255), 0.65f * vidaAlfa));
                    // EL DESENFOQUE (el sangrado del blur — dos pasadas)
                    Quad(VFXCore.SoftGlow, centro, new Vector2(14f, ALTO * vidaAlfa + 60f), 0f,
                        VFXCore.TintAlfa(new Color(120, 160, 255), 0.28f * vidaAlfa));
                    Quad(VFXCore.SoftGlow, centro, new Vector2(22f, ALTO * vidaAlfa + 66f), 0f,
                        VFXCore.TintAlfa(new Color(100, 130, 255), 0.14f * vidaAlfa));

                    // ========================================================
                    //  ACTO 1 — EL DESTELLO INICIAL (el latigazo): un
                    //  núcleo BRILLA en su extremo de partida y CORRE la
                    //  línea (arriba→abajo o abajo→arriba, el azar del
                    //  nacimiento) — «a gran velocidad»: 40 px/tick.
                    // ========================================================
                    if (edad <= TICKS_DESTELLO)
                    {
                        float fase = edad / (float)TICKS_DESTELLO;      // 0→1
                        // el arranque: SUBE = nace abajo y corre arriba
                        float yDestello = Sube
                            ? (ALTO * 0.5f) - fase * ALTO
                            : (-ALTO * 0.5f) + fase * ALTO;
                        Vector2 posD = centro + new Vector2(0f, yDestello);
                        // EL NÚCLEO DEL DESTELLO (blanco puro)
                        Quad(VFXCore.SoftGlow, posD, new Vector2(10f, 26f), 0f,
                            VFXCore.TintAlfa(Color.White, 0.95f));
                        // SU GLOW (la bola de luz que corre)
                        Quad(VFXCore.SoftGlow, posD, new Vector2(26f, 26f), 0f,
                            VFXCore.TintAlfa(new Color(180, 220, 255), 0.70f));
                        // LA ESTELA del destello (el trazo que deja)
                        float estela = 46f;
                        Quad(VFXCore.SoftGlow, posD + new Vector2(0f, Sube ? estela * 0.5f : -estela * 0.5f),
                            new Vector2(6f, estela), 0f,
                            VFXCore.TintAlfa(new Color(200, 230, 255), 0.45f));
                    }

                    // ========================================================
                    //  ACTO 3 — LA TURBULENCIA AL FINAL: el extremo donde
                    //  MURIÓ el brillo ONDEA (±3-4 px de ruido) y brilla
                    //  más — la punta escribe su temblor.
                    // ========================================================
                    if (edad > TICKS_DESTELLO)
                    {
                        float puntaY = Sube ? -ALTO * 0.5f : ALTO * 0.5f;
                        Vector2 punta = centro + new Vector2(0f, puntaY) + new Vector2(
                            MathF.Sin(t * 22f + Projectile.whoAmI * 2.1f) * 3.6f, 0f);
                        Quad(VFXCore.SoftGlow, punta, new Vector2(12f, 18f), 0f,
                            VFXCore.TintAlfa(new Color(210, 235, 255), 0.75f * vidaAlfa));
                        // EL TEMBLOR: dos brazos cortos que vibran
                        for (int k = 0; k < 2; k++)
                        {
                            float ang = (Sube ? -MathHelper.PiOver2 : MathHelper.PiOver2) +
                                MathF.Sin(t * 17f + k * 2.4f) * 0.6f;
                            Quad(VFXCore.SoftGlow, punta + new Vector2(
                                MathF.Cos(ang), MathF.Sin(ang)) * 14f, new Vector2(3f, 12f), ang,
                                VFXCore.TintAlfa(new Color(160, 200, 255), 0.45f * vidaAlfa));
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }
            }
            catch { try { VFXCore.CerrarLoteSiAbierto(); } catch { } }
            return false;
        }

        /// <summary>Quad centrado directo al lote aditivo activo.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        public override bool? CanHitNPC(NPC target) => false;   // el daño es la columna de OnSpawn
    }
}
