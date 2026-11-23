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
    //  LineaDelCielo — v6.170 — EL TAJO DE KATANA (la letra: «yo
    //  necesito tajos, tajos iguales a los de las katanas en los
    //  animes, intenta investigar esos tajos de katanas en anime e
    //  intenta recrearlos en el arma la regla del cielo»).
    //
    //  LA ANATOMÍA INVESTIGADA (el canon del tajo anime — la media
    //  luna cel-shaded que la investigación de la sesión describió:
    //  «electric-blue trails, glowing crescents, sharp edges, flat
    //  tones»):
    //   1. LA MEDIA LUNA — un arco de ~110° con bordes AFILADOS y
    //      tonos planos (nada de gradientes suaves): el corte YA
    //      ocurrió cuando lo ves.
    //   2. EL NÚCLEO BLANCO-CALIENTE en el filo interior (el filo
    //      de la hoja) + EL HALO AZUL ELÉCTRICO en el exterior (dos
    //      pasadas parpadeantes — el color del anime).
    //   3. EL ECO FANTASMA — un segundo arco más chico y tenue
    //      DETRÁS del corte (el afterimage del gesto).
    //   4. VIDA ULTRACORTA (13 ticks): nace AL 100 % y se apaga —
    //      el corte no crece, OCURRE.
    //   5. EL SENTIDO DIAGONAL del gesto: el arco vive INCLINADO
    //      (~35° del vertical) y el revelado de punta a punta corre
    //      ABAJO→ARRIBA o ARRIBA→ABajo al azar — LA LETRA de la
    //      v6.168 («un brillo de arriba hacia abajo o de abajo
    //      hacia arriba de forma aleatoria») vive como EL SENTIDO
    //      DE LA HOJA.
    //   6. LA LÍNEA DE VELOCIDAD — la estela recta y fina que une
    //      la mano del portador con el centro del corte (la
    //      trayectoria de la hoja, la firma del anime).
    //
    //  EL MOTOR: TajoLib.Tajo — EL PRIMITIVO DE LA CASA (el del
    //  Tajo Astral): eco fantasma + halo doble parpadeante + núcleo
    //  BoltCore, revelado de punta a punta con arranque elástico.
    //  CERO texturas del proyecto (solo SoftGlow procedural).
    // =====================================================================
    public class LineaDelCielo : ModProjectile
    {
        /// <summary>Vida total del tajo (ticks) — ultracorto.</summary>
        private const int VIDA = 13;

        /// <summary>El revelado de punta a punta (la hoja recorre el
        /// arco — 4 ticks: la velocidad del anime).</summary>
        private const int REVELA = 4;

        /// <summary>El grosor del filo (px en el centro — la lente).</summary>
        private const float ANCHO = 5.2f;

        /// <summary>La apertura de la media luna (~110°).</summary>
        private const float APERTURA = 1.92f;

        /// <summary>La inclinación diagonal del gesto (~35°).</summary>
        private const float INCLINACION = 0.61f;

        /// <summary>La banda del golpe (±px del radio: la boca del arco).</summary>
        private const float BANDA = 52f;

        // === LA PALETA (el cielo de la casa: azul eléctrico anime) ===
        private static readonly Color Filo = new(255, 252, 245);        // blanco-caliente
        private static readonly Color HaloCielo = new(120, 200, 255);   // azul eléctrico
        private static readonly Color HaloAlto = new(190, 235, 255);    // el borde casi blanco

        /// <summary>¿La hoja SUBE (1) o BAJA (0)? — al azar al nacer (la letra v6.168).</summary>
        private bool Sube => Projectile.ai[2] >= 1f;

        /// <summary>v6.172 — ¿EL TAJO CRUZADO? (ai[0] = el contador de
        /// usos del arma: cada CUARTO uso desata la X del combo — el
        /// doble corte cruzado del anime, la nueva variante de
        /// TajoLib.TajoCruzado).</summary>
        private bool Cruzado => Projectile.ai[0] >= 4f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
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
            // LAS MOTAS DEL CORTE (el aire herido: 2-3 chispas frías por
            // tick en la banda del arco — la firma del anime)
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                try
                {
                    float ang = AnguloMedio() + Main.rand.NextFloat(-APERTURA * 0.5f, APERTURA * 0.5f);
                    Vector2 p = CentroMundo() + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang)) * Radio() * Main.rand.NextFloat(0.85f, 1.05f);
                    Dust d = Dust.NewDustPerfect(p, DustID.BlueMoss,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -0.35f),
                        130, new Color(150, 215, 255), 0.5f);
                    d.noGravity = true;
                }
                catch { }
            }
        }

        // ================================================================
        //  LA GEOMETRÍA (v6.171 — EL CONTRATO SIN localAI[3]): LA
        //  AUTOPSIA DEL client.log — Projectile.localAI es float[3]
        //  (índices 0..2, la lección de la v6.52.2 documentada en
        //  BocadoDelSueloProjectile): la v6.170 escribía la
        //  inclinación en localAI[3] y la katana CRASHEABA en cada
        //  uso (IndexOutOfRange en AnguloMedio — 3 excepciones
        //  silenciosas del log del dueño). EL CONTRATO NUEVO:
        //   · localAI[0..1] = el CENTRO del arco (el cursor — el
        //     arma lo siembra directo: son SÓLO visuales, no viajan)
        //   · localAI[2] = el RADIO
        //   · ai[1] = la INCLINACIÓN EXTRA hacia la presa ×1000
        //     (ai SÍ viaja por el cable: el tajo se ve igual en MP)
        //   · ai[2] = el SENTIDO de la hoja (0 baja · 1 sube)
        // ================================================================
        private Vector2 CentroMundo() => new(Projectile.localAI[0], Projectile.localAI[1]);
        private float Radio() => Projectile.localAI[2] > 8f ? Projectile.localAI[2] : 150f;
        private float AnguloMedio()
        {
            float diag = Sube ? -MathHelper.PiOver2 + INCLINACION
                              : MathHelper.PiOver2 - INCLINACION;
            // ai[1] llega ×1000 (la precisión de la inclinación sobrevive
            // a la redondeo del cable) — sin marca = 0: vertical puro
            return diag + Projectile.ai[1] / 1000f;
        }

        public override void OnSpawn(IEntitySource fuente)
        {
            // EL GOLPE DE LA MEDIA LUNA: una pasada al nacer — todo lo que
            // vive en la BANDA del arco (|dist−radio| < BANDA && el ángulo
            // dentro de la boca) recibe el tajo (el mismo contrato del
            // Tajo Astral: la herida llega cuando el corte OCURRE).
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                Vector2 c = CentroMundo();
                float radio = Radio();
                float medio = AnguloMedio();
                float a0 = medio - APERTURA * 0.5f, a1 = medio + APERTURA * 0.5f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.life <= 0 || n.friendly ||
                        n.townNPC || n.immortal || n.dontTakeDamage) continue;
                    Vector2 rel = n.Center - c;
                    float dist = rel.Length();
                    if (Math.Abs(dist - radio) > BANDA + n.width * 0.5f) continue;   // fuera de la banda
                    float ang = MathF.Atan2(rel.Y, rel.X);
                    float delta = MathHelper.WrapAngle(ang - medio);
                    if (Math.Abs(delta) > APERTURA * 0.5f + 0.25f) continue;        // fuera de la boca
                    global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n, Projectile.damage, 1.25f);
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color luzColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // LA EDAD del tajo (de su nacimiento: timeLeft cuenta hacia 0)
            float t = VIDA - Projectile.timeLeft;

            // LA CAMPANA DE VIDA (nace AL 100 %: aguanta 60 % de la vida,
            // cae en el 40 % restante — el corte ya ocurrió)
            float campana = t < VIDA * 0.6f
                ? 1f
                : 1f - (t - VIDA * 0.6f) / (VIDA * 0.4f);
            campana = MathHelper.Clamp(campana, 0f, 1f);

            // EL REVELADO: la hoja recorre el arco de punta a punta en
            // REVELA ticks, con el POP elástico de la casa (el arranque
            // del anime: sale con impulso)
            float reveal = Elastico(Math.Min(t / (float)REVELA, 1f));

            // EL RADIO CRECE apenas al morir (×1.0 → ×1.15: el aliento)
            float radio = Radio() * (1f + 0.15f * (t / VIDA));

            Vector2 centro = CentroMundo() - Main.screenPosition;
            float medio = AnguloMedio();

            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    float time = Main.GlobalTimeWrappedHourly;
                    int seed = Math.Max(1, Projectile.identity + 37);

                    // === 1. LA LÍNEA DE VELOCIDAD (la trayectoria de la
                    //     hoja: el trazo recto y fino de la mano al
                    //     centro del corte — se apaga primero) ===
                    Vector2? origen = OrigenDelGolpe();
                    if (origen.HasValue)
                    {
                        Vector2 desde = origen.Value - Main.screenPosition;
                        Vector2 hasta = centro;
                        Vector2 mid = (desde + hasta) * 0.5f;
                        float len = (hasta - desde).Length();
                        float estela = 1f - Math.Min(t / (VIDA * 0.7f), 1f);
                        if (len > 30f && estela > 0.02f)
                            Quad(VFXCore.SoftGlow, mid, new Vector2(len, 2.4f),
                                (float)Math.Atan2(hasta.Y - desde.Y, hasta.X - desde.X),
                                VFXCore.TintAlfa(HaloCielo, 0.18f * estela));
                    }

                    // === 2. LA MEDIA LUNA (el corazón del anime: eco
                    //     fantasma + halo doble parpadeante + núcleo
                    //     blanco — TODO en TajoLib.TajoDoble, la
                    //     REFORJA v6.172 de la investigación: EL DOBLE
                    //     FILO (el segundo creciente en sentido
                    //     contrario, la firma de Hollow Knight), LOS
                    //     SMEARS (las hojas fantasma del swing), LAS
                    //     CHISPAS del filo exterior y LA ELIPSE (el
                    //     corte en perspectiva de la katana) + EL
                    //     FRAME DE IMPACTO (el flash monocromo del
                    //     sakuga en los 2 primeros ticks de vida) ===
                    float impacto = t / VIDA;
                    if (Cruzado)
                        TajoLib.TajoCruzado(centro, radio,
                            medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                            reveal, campana, ANCHO, HaloCielo, Filo, seed, time, 1f);
                    else
                        TajoLib.TajoDoble(centro, radio,
                            medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                            reveal, campana, ANCHO, HaloCielo, Filo, seed, time,
                            1f, impacto);

                    // === 3. EL DESTEMPLE (el flash de clausura: los
                    //     últimos 4 ticks, un destello alto y frío en el
                    //     centro del arco — el «clac» visual del corte
                    //     terminado) ===
                    if (t > VIDA - 4f)
                    {
                        float f = (t - (VIDA - 4f)) / 4f;
                        Quad(VFXCore.SoftGlow, centro,
                            new Vector2(44f * (1f - f), 44f * (1f - f)), 0f,
                            VFXCore.TintAlfa(HaloAlto, 0.4f * (1f - f)));
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }
            }
            catch { try { VFXCore.CerrarLoteSiAbierto(); } catch { } }
            return false;
        }

        /// <summary>EL ORIGEN del golpe (la mano que lo lanzó): el
        /// portador — para la línea de velocidad.</summary>
        private Vector2? OrigenDelGolpe()
        {
            try
            {
                Player p = Main.player[Projectile.owner];
                if (p != null && p.active) return p.MountedCenter;
            }
            catch { }
            return null;
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

        /// <summary>El easeOutBack de la casa (el POP del anime).</summary>
        private static float Elastico(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c = 1.35f;
            float u = t - 1f;
            return 1f + u * u * ((c + 1f) * u + c);
        }

        public override bool? CanHitNPC(NPC target) => false;   // el daño es la media luna de OnSpawn
    }
}
