using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Emperatriz
{
    /// <summary>
    /// LA ESTELA DE AETHON — OPCIÓN A (la recomendada).
    ///
    /// La evolución del Fragmento de Aethon (v6.50.98 — el proyectil del
    /// Grimorio Sellado del usuario): el sello se rompió y la estela
    /// aprendió a volar como el streak de la Emperatriz de la Luz. La
    /// AI_171_HallowBossRainbowStreak replicada al 100% (estudio
    /// ESTUDIO_PROYECTIL_NIGHTGLOW.md §11 — el blueprint exacto):
    ///   · nace con velocidad de FUENTE (siempre hacia arriba, la pasa
    ///     el item en el disparo) y fade-in 240→220,
    ///   · FASE 1 (240→180): deriva serpenteante — fricción 0.97 y la
    ///     onda cos(whoAmI%6/6 + x/320 + y/160) que hace que cada orbe
    ///     ondule con una fase distinta,
    ///   · FASE 2 (180→20): persecución con LÍNEA DE VISTA a 800px,
    ///     steering SmoothStep que crece 0.075→0.125, velocidad de caza
    ///     30px/update, clamp de timeLeft en 10 mientras persigue (no
    ///     expira con la presa viva) y doble decremento sin objetivo,
    ///   · REBOTA en los bloques (invierte la componente bloqueada),
    ///   · −20% de daño por enemigo golpeado (penetra 2, muere en el 3º)
    ///     y al golpear RE-ENCADENA contra el siguiente enemigo.
    ///
    /// Lo propio de la casa:
    ///   · la PALETA CÓSMICA del grimorio (la del ArcaneBolt): oro,
    ///     cian, magenta e índigo — cada estela lleva un color y la
    ///     estela de afterimages recorre la paleta,
    ///   · dibujo 100% por código: 20 afterimages GlowOrb + núcleo
    ///     blanco caliente + la CRUZ PULSANTE del glow vanilla,
    ///   · GANCHO OBSERVADO (el SISTEMA DEL USUARIO, v6.50.98 —
    ///     SistemaObservado): cada golpe añade una marca; a 5 marcas el
    ///     objetivo queda «Conocido» y los golpes son crítico
    ///     garantizado +50% (el patrón del FragmentoAethon: AddStack en
    ///     OnHitNPC, el chequeo en ModifyHitNPC ANTES de sumar la marca
    ///     de este golpe),
    ///   · EL DESBORDE: con ai[2]==1 la estela nace MASIVA (escala ×2,
    ///     daño ×2.5 que pasa el item, penetra 6 y anillo radial al
    ///     morir).
    /// </summary>
    public class EstelaAethonProjectile : ModProjectile
    {
        // La paleta cósmica del grimorio (la del ArcaneBolt):
        // oro (núcleo de galaxia), cian (estrella guía),
        // magenta (las gemas) e índigo (el fondo del portal).
        private static readonly Color[] Paleta = new Color[]
        {
            new Color(255, 217, 61),   // ORO
            new Color(0,   255, 255),  // CIAN
            new Color(255, 0,   102),  // MAGENTA
            new Color(122, 96,  255),  // ÍNDIGO
        };

        private static readonly int[] DustsCosmicos = new int[]
        {
            DustID.GoldFlame,      // oro
            DustID.BlueTorch,      // cian
            DustID.RainbowTorch,   // magenta
            DustID.PurpleTorch,    // índigo
        };

        // ai[0] = índice del NPC objetivo (la IA_171 de verdad)
        // ai[1] = índice de la paleta cósmica (0..3)
        // ai[2] = 1 → EL DESBORDE (la estela masiva)
        // localAI[0] = ya inicializado (engorda la masiva una sola vez)

        public override void SetStaticDefaults()
        {
            // 20 afterimages — la estela del 931 de verdad
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;                 // nace invisible → fade-in
            Projectile.penetrate = 3;               // toca 3 enemigos
            Projectile.friendly = true;
            Projectile.timeLeft = 240;              // presupuesto de vida (2 updates/tick)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;          // ...pero REBOTA
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;            // la IA corre 2 veces por tick
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        /// <summary>El índice de la paleta cósmica de ESTA estela.</summary>
        private int Indice() => (int)Projectile.ai[1] & 3;

        public override void AI()
        {
            // === EL DESBORDE: la estela masiva se engorda una sola vez ===
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Projectile.ai[2] == 1f)
                {
                    Projectile.scale = 2f;
                    Projectile.width = 60;
                    Projectile.height = 60;
                    Projectile.penetrate = 6;       // la masiva barre más
                }
            }

            // === FASE 0: fade-in (240→220) ===
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft);

            if (Projectile.timeLeft > 180f)
            {
                // === FASE 1: deriva serpenteante (240→180 = 0.5 s) ===
                float onda = MathF.Cos(Projectile.whoAmI % 6f / 6f
                    + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity *= 0.97f;
                Projectile.velocity = Projectile.velocity.RotatedBy(
                    onda * MathHelper.TwoPi * 0.125f / 30f);
            }
            else
            {
                // === FASE 2: persecución (180→20) ===
                int objetivo = (int)Projectile.ai[0];
                if (Main.npc.IndexInRange(objetivo) && !Main.npc[objetivo].CanBeChasedBy(Projectile))
                {
                    Projectile.ai[0] = -1f;         // el objetivo murió/inválido
                    Projectile.netUpdate = true;
                }
                if (Projectile.ai[0] == -1f)
                    Projectile.ai[0] = Projectile.FindTargetWithLineOfSight(800f);

                Vector2 deseada = Projectile.velocity;
                if (Projectile.friendly && Main.npc.IndexInRange((int)Projectile.ai[0]))
                {
                    // ★ clamp: mientras PERSIGUE no muere por tiempo
                    if (Projectile.timeLeft < 10) Projectile.timeLeft = 10;
                    deseada = Projectile.DirectionTo(Main.npc[(int)Projectile.ai[0]].Center) * 30f;
                }
                else
                {
                    Projectile.timeLeft--;          // ★ sin objetivo: vida baja al doble
                }

                // steering que crece 0.075→0.125 conforme se acerca a expirar
                float k = MathHelper.Lerp(0.075f, 0.125f,
                    Utils.GetLerpValue(180f, 30f, Projectile.timeLeft));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, deseada, k);

                // freno extra al final de la vida
                Projectile.velocity *= MathHelper.Lerp(0.85f, 1f,
                    Utils.GetLerpValue(0f, 90f, Projectile.timeLeft));
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // === LA LUZ CÓSMICA (el color de ESTA estela) ===
            if (Main.netMode != NetmodeID.Server)
            {
                Color c = Paleta[Indice()];
                float fuerza = 0.55f * Projectile.scale;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(c.R / 255f * fuerza, c.G / 255f * fuerza, c.B / 255f * fuerza));
            }
        }

        // ================================================================
        //  EL REBOTE (la colisión especial del 931: no muere, rebota)
        // ================================================================
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = oldVelocity.X * -1f;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = oldVelocity.Y * -1f;
            return false;   // rebota, no muere
        }

        // ================================================================
        //  EL GOLPE: daño decreciente + EL GANCHO OBSERVADO
        // ================================================================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // −20% de daño por enemigo golpeado (el decreciente del 931)
            modifiers.FinalDamage *= MathF.Pow(0.8f, Projectile.numHits);

            // EL GANCHO DEL SISTEMA DEL USUARIO (v6.50.98): a 5 marcas
            // el objetivo queda «Conocido» — crítico garantizado +50%
            // (el patrón del FragmentoAethon: el chequeo corre ANTES de
            // que OnHitNPC añada la marca de ESTE golpe)
            var sistema = ModContent.GetInstance<SistemaObservado>();
            if (sistema.GetStacks(target.whoAmI) >= 5)
            {
                modifiers.SetCrit();               // CRÍTICO GARANTIZADO (sin dados)
                modifiers.FinalDamage *= 1.5f;     // +50% daño extra

                // el juice de la estela: el ojo dorado se abre
                Terraria.CombatText.NewText(target.getRect(), new Color(255, 217, 61),
                    Language.GetTextValue("Mods.AethonMod.Desborde.Observado"),
                    dramatic: true);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, target.Center);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 anillo = new Vector2(1f, 0f).RotatedBy(MathHelper.TwoPi * i / 10f)
                        * (target.width * 0.6f);
                    Dust d = Dust.NewDustPerfect(target.Center + anillo, DustID.Enchanted_Gold,
                        anillo * 0.02f, 200, new Color(255, 217, 61), 0.9f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA MARCA (el sistema del usuario: OnHitNPC → AddStack — a
            // 5 marcas el SistemaObservado hace su destello dorado)
            ModContent.GetInstance<SistemaObservado>().AddStack(target.whoAmI);

            // RE-ENCADENA: al golpear busca el SIGUIENTE enemigo al
            // instante (por eso el Nightglow barre packs enteros)
            int siguiente = Projectile.FindTargetWithLineOfSight(800f);
            if (siguiente != -1)
            {
                Projectile.ai[0] = siguiente;
                Projectile.netUpdate = true;
            }
        }

        // ================================================================
        //  LA MUERTE: quema la estela (dusts cósmicos bicapa en oldPos)
        // ================================================================
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            bool masivo = Projectile.ai[2] == 1f;
            if (masivo)
                PulsoLib.Trauma(0.30f);   // el jugo del desborde (el shake de la casa)

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                // la cola se desvanece: menos dusts cuanto más atrás
                int cantidad = 2 - i * 2 / Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                for (int j = 0; j < cantidad; j++)
                {
                    int idx = (Indice() + i) & 3;
                    Color c = Paleta[idx];
                    Dust d = Dust.NewDustPerfect(pos, DustsCosmicos[idx],
                        Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f), 100, c,
                        Main.rand.NextFloat(0.9f, 2.1f) * Projectile.scale);
                    d.noGravity = true;
                    d.fadeIn = 1.2f;

                    // la capa clonada blanca (el núcleo brillante bicapa del 931)
                    Dust clon = Dust.NewDustPerfect(pos, DustsCosmicos[idx],
                        Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), 100,
                        new Color(255, 255, 255), d.scale * 0.5f);
                    clon.noGravity = true;
                    clon.fadeIn = 1.2f;
                }
            }

            // la masiva estalla además en anillo radial cósmico
            if (masivo)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector2 dir = new Vector2(1f, 0f).RotatedBy(MathHelper.TwoPi * i / 24f);
                    int idx = i & 3;
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustsCosmicos[idx],
                        dir * Main.rand.NextFloat(2f, 6f), 100, Paleta[idx],
                        Main.rand.NextFloat(1.2f, 2.4f));
                    d.noGravity = true;
                    d.fadeIn = 1.3f;
                }
            }
        }

        // ================================================================
        //  EL DIBUJO — 100% por código (la casa): estela + núcleo + cruz
        //
        //  v6.50.101 — EL CONTRATO DEL LOTE (lección del client.log del
        //  usuario 05:38:06): el FlushAdditive CIERRA el lote del pase
        //  de proyectiles y deja NINGUNO abierto → el siguiente
        //  EntitySpriteDraw de vanilla lanza «Draw was called, but Begin
        //  has not yet been called» y el End final del bucle tumba el
        //  motor. El patrón de la casa (GrietaDeTinta/TestAdvancedFX):
        //  cerrar → dibujar a prueba de todo → REABRIR SIEMPRE el lote
        //  vanilla antes de salir del PreDraw.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            float escala = Projectile.scale;
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft);
            float t = Main.GameUpdateCount;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();

                // === LA ESTELA: 20 afterimages (escala 0.7, alpha 50%) ===
                // cada fantasma lleva el SIGUIENTE color de la paleta: la
                // estela recorre oro→cian→magenta→índigo
                for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    float alfaCola = (1f - i / (float)Projectile.oldPos.Length) * 0.5f * fade;
                    Color cCola = Paleta[(Indice() + i) & 3];
                    VFXCore.Quad(pos, cCola * alfaCola, new Vector2(44f * escala, 44f * escala), VFXCore.GlowOrb);
                }

                // === EL NÚCLEO ===
                Color c = Paleta[Indice()];
                VFXCore.Quad(Projectile.Center, c * (0.85f * fade), new Vector2(52f * escala, 52f * escala), VFXCore.GlowOrb);
                VFXCore.Quad(Projectile.Center, Color.White * (0.95f * fade), new Vector2(26f * escala, 26f * escala), VFXCore.GlowOrb);

                // === LA CRUZ PULSANTE (el glow Extra[98] del 931) ===
                float pulso = 0.8f * (1f + 0.2f * MathF.Cos(t * 0.25f));
                float largo = 70f * escala * pulso;
                float grosor = 7f * escala;
                VFXCore.Line(Projectile.Center - new Vector2(largo, 0f),
                    Projectile.Center + new Vector2(largo, 0f), c * (0.55f * fade), grosor);
                VFXCore.Line(Projectile.Center - new Vector2(0f, largo),
                    Projectile.Center + new Vector2(0f, largo), c * (0.55f * fade), grosor);

                // la cruz diagonal, más tenue — el destello de 8 puntas
                Vector2 diag = new Vector2(1f, 1f);
                diag.Normalize();
                diag *= largo * 0.6f;
                VFXCore.Line(Projectile.Center - diag, Projectile.Center + diag,
                    Color.White * (0.25f * fade), 5f * escala);
                VFXCore.Line(Projectile.Center - new Vector2(diag.X, -diag.Y),
                    Projectile.Center + new Vector2(diag.X, -diag.Y),
                    Color.White * (0.25f * fade), 5f * escala);

                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();   // ★ el contrato: salir con el lote VIVO
            return false;   // TODO el dibujado es de la casa
        }

        /// <summary>El sprite base no se dibuja, pero su alfa es la del fade-in.</summary>
        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
