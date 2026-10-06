using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// PAGINAFINALPROJECTILE — v6.50.64 — ARMA 4: LA PÁGINA FINAL.
    ///
    /// «Como el arma La Sombra de la Página, pero mejorada MUCHO» — la
    /// petición del usuario, con la investigación aplicada: el diseño de
    /// las armas AAA (Calamity/Stars Above/Destiny-exotic + los 12
    /// principios de la animación) traducido al lenguaje de Pride:
    ///
    ///   · ANTICIPACIÓN — LA APERTURA (46 t): la sala se apaga (vignette
    ///     en los bordes), el LIBRO despliega sus dos hojas sobre el
    ///     portador, su OJO se abre DE GOLPE mirando a la presa y tres
    ///     tallitos de sombra se alzan del charco. El golpe se ANUNCIA.
    ///   · STAGING — LOS TRES VERSOS (66 t): tres zarpos parten de la
    ///     sombra del portador por ÁNGULOS DISTINTOS (izquierda, derecha
    ///     y un salto por arriba) y CONVERGEN en el jefe — la imagen se
    ///     lee desde cualquier esquina de la pantalla. Cada uno con su
    ///     masa, sus ojos, su boca (el central) y SU BRUMA NEGRA.
    ///   · EL JUICE — EL IMPACTO: onda de choque + destello rojo + el
    ///     crujido (sonido), todo en los primeros 14 t de la jaula.
    ///   · LA MECÁNICA ÚNICA — LA JAULA DE TINTA: un anillo de 12 púas
    ///     negras de punta blanca se CIERRA de golpe al ritmo de las
    ///     mordidas (4% cada 7 t — el drain más fuerte de las 4 armas),
    ///     con ojos entre las púas que TE MIRAN mientras comen y las
    ///     almas volando al portador.
    ///   · EL FESTÍN — estilo 4: a 1 HP FaucesGlobalNPC.Iniciar(4) → LA
    ///     PÁGINA FINAL del FauceDevorador: el libro GIGANTE se abre en
    ///     el cielo, sus zarpos agarran al jefe, el plano negro SUBE
    ///     comiéndoselo con una boca de colmillos en el borde, las almas
    ///     suben al libro… y al final el libro se sella con una onda de
    ///     choque y PAGA al portador.
    ///   · FOLLOW-THROUGH — LA DISIPACIÓN (64 t): todo se pliega, la
    ///     bruma estalla y el velo de la sala se levanta.
    ///
    /// 100% CÓDIGO (la herencia del arma 3): ni un sprite — todo con los
    /// pinceles del motor vía SombrasLib.
    ///
    /// ai[0] = whoAmI de la presa + 1 · ai[1] = fase · ai[2] = tick.
    /// </summary>
    public class PaginaFinalProjectile : ModProjectile
    {
        // fases (ai[1])
        private const byte FASE_APERTURA = 0;
        private const byte FASE_VERSOS = 1;
        private const byte FASE_JAULA = 2;
        private const byte FASE_DISIPAR = 3;

        /// <summary>ai[0] = whoAmI de la presa + 1 (0 = sin presa).</summary>
        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1600;             // no muere por edad
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 28;
            Projectile.extraUpdates = 2;
        }

        // ==================================================================
        //  LA GEOMETRÍA DEL ARMA
        // ==================================================================

        /// <summary>EL LIBRO del portador: flota sobre su cabeza todo el ataque (la «stance»).</summary>
        private static Vector2 PosLibro(Player dueño) => dueño.MountedCenter + new Vector2(0f, -122f);

        /// <summary>La sombra del suelo (herencia del arma 3): donde hay sombra, la página mira.</summary>
        private static Vector2 RaizDeSombra(Player dueño)
        {
            Vector2 c = dueño.MountedCenter + new Vector2(0, dueño.height * 0.45f);
            int tx = (int)(c.X / 16f), ty = (int)(c.Y / 16f);
            int pasos = 0;
            while (pasos < 34 && !WorldGen.SolidTile(tx, ty)) { ty++; pasos++; }
            Vector2 suelo = new(tx * 16f + 8f, ty * 16f - 4f);
            return pasos < 34 ? suelo : c;
        }

        /// <summary>
        /// LOS TRES VERSOS (staging): el punto de control de cada zarpo —
        /// el izquierdo rodea por un lado, el derecho por el otro, y el
        /// central SALTA por encima. Tres trayectorias, un destino.
        /// </summary>
        private static Vector2 CtrlVerso(int k, Vector2 raiz, Vector2 presa)
        {
            Vector2 medio = (raiz + presa) * 0.5f;
            Vector2 seg = presa - raiz;
            Vector2 perp = new(-seg.Y, seg.X);
            perp = perp.LengthSquared() < 1f ? Vector2.UnitY : Vector2.Normalize(perp);
            float dist = seg.Length();
            float lado = MathF.Min(dist * 0.42f, 560f);
            return k switch
            {
                0 => medio + perp * lado + new Vector2(0f, -60f),          // el verso IZQUIERDO rodea
                1 => medio - perp * lado + new Vector2(0f, -60f),          // el verso DERECHO rodea
                _ => medio + new Vector2(0f, -MathF.Min(dist * 0.5f, 620f)), // el central SALTA por arriba
            };
        }

        /// <summary>El despegue escalonado de cada verso (8 t entre lanzamientos — el tridente).</summary>
        private static float ProgresoVerso(int k, float t)
            => SombrasLib.DeGolpe(MathHelper.Clamp((t - 8f - k * 7f) / 44f, 0f, 1f));

        /// <summary>La PUNTA del verso k sobre su arco Bézier (raíz → control → jefe).</summary>
        private static Vector2 PuntaVerso(int k, Vector2 raiz, Vector2 presa, float t)
        {
            float p = ProgresoVerso(k, t);
            Vector2 c = CtrlVerso(k, raiz, presa);
            float u = 1f - p;
            return u * u * raiz + 2f * u * p * c + p * p * presa;
        }

        // ==================================================================
        //  LA IA
        // ==================================================================

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPAR;
                t = 0;
            }

            switch (fase)
            {
                case FASE_APERTURA:
                {
                    // el libro se despliega sobre el portador; la sombra se agita
                    Projectile.Center = PosLibro(dueño);
                    if (t == 3 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item74.WithPitchOffset(-0.55f).WithVolumeScale(0.7f), Projectile.Center);    // el libro se abre
                    if (t == 28 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.4f), Projectile.Center);    // el ojo abre
                    if (t >= 46)
                    {
                        Projectile.ai[1] = FASE_VERSOS;
                        Projectile.ai[2] = 0;
                        if (Main.netMode != NetmodeID.Server)
                            Sonar(SoundID.Item122.WithPitchOffset(-0.4f).WithVolumeScale(0.85f), Projectile.Center); // ¡los tres zarpos!
                    }
                    break;
                }
                case FASE_VERSOS:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 raiz = RaizDeSombra(dueño);
                        // el proyectil ES la punta del verso central (la boca)
                        Projectile.Center = PuntaVerso(2, raiz, presa.Center, t);

                        // los lanzamientos escalonados (el tridente sale en tres tiempos)
                        if (Main.netMode != NetmodeID.Server)
                        {
                            if (t == 8f || t == 15f || t == 22f)
                                Sonar(SoundID.Item122.WithPitchOffset(0.1f + (t - 8f) * 0.015f).WithVolumeScale(0.25f), Projectile.Center);
                        }

                        // el verso central llegó: EL IMPACTO
                        if (ProgresoVerso(2, t) >= 1f)
                        {
                            Projectile.ai[1] = FASE_JAULA;
                            Projectile.ai[2] = 0;
                            if (Main.netMode != NetmodeID.Server)
                            {
                                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.45f).WithVolumeScale(0.95f), Projectile.Center);
                                Sonar(SoundID.Item122.WithPitchOffset(-0.15f).WithVolumeScale(0.7f), Projectile.Center);
                            }
                        }
                    }
                    if (t > 400) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_JAULA:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;

                        // === EL DREN DE LA JAULA (server/SP): 4% cada 7 t —
                        // el drain más fuerte de las cuatro armas ===
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 14 && t % 7 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.04f, 50f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: EL LIBRO SE QUEDA CON ÉL → estilo 4 ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 4);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 28 == 0 && Main.netMode != NetmodeID.Server)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.15f).WithVolumeScale(0.45f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    if (t > 900) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }   // red de seguridad
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.35f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 64) Projectile.Kill();
                    break;
                }
            }

            // === EL POLVO: la sombra se agita (apertura) y los zarpos humean ===
            if (Main.netMode != NetmodeID.Server)
            {
                byte f = (byte)Projectile.ai[1];
                if (f == FASE_APERTURA && Main.rand.NextBool(4))
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Dust d = Dust.NewDustPerfect(raiz + new Vector2(Main.rand.NextFloat(-40, 40), 0),
                        DustID.Shadowflame, new Vector2(0f, -1.4f), 128, default, 0.7f);
                    d.noGravity = true;
                }
                if (f == FASE_VERSOS && Main.rand.NextBool(6))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        DustID.Shadowflame, -Projectile.velocity * 0.08f, 128, default, 0.6f);
                    d.noGravity = true;
                }
            }
        }

        /// <summary>El daño de contacto cubre los TRES zarpos (Colliding sobre las tres curvas).</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            byte fase = (byte)Projectile.ai[1];
            if (fase != FASE_VERSOS && fase != FASE_JAULA) return false;   // el libro abierto no muerde todavía
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            if (presa == null || !presa.active) return false;
            Vector2 raiz = RaizDeSombra(dueño);
            float punto = 0f;
            for (int k = 0; k < 3; k++)
            {
                Vector2 punta = fase == FASE_JAULA ? presa.Center : PuntaVerso(k, raiz, presa.Center, Projectile.ai[2]);
                Vector2[] col = SombrasLib.Columna(raiz, punta,
                    Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 41 + k * 7, 10, 0.2f);
                for (int i = 1; i < col.Length; i++)
                    if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        col[i - 1], col[i], 8, ref punto))
                        return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  EL DIBUJO — el ritual completo
        // ==================================================================

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 41 + 3;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 64f, 0f, 1f) : 1f;

            Vector2 raiz = RaizDeSombra(dueño);

            // === LA SALA DE CINE (anticipación): el velo de los bordes
            // CRECE durante la apertura y se levanta en la disipación ===
            float velo = fase switch
            {
                FASE_APERTURA => 0.42f * SombrasLib.DeGolpe(t / 40f),
                _ => 0.42f,
            } * disipa;
            SombrasLib.Vignette(velo);

            // === EL LIBRO DEL PORTADOR (la stance: flota sobre la cabeza) ===
            DibujarLibro(PosLibro(dueño), presa, fase, t, disipa, tiempo);

            // === EL CHARCO RAÍZ (la sombra del suelo se agita) ===
            float brota = fase == FASE_APERTURA ? SombrasLib.DeGolpe(t / 34f) : 1f;
            SombrasLib.Charco(raiz, 58f * brota, 0.8f * disipa, tiempo, semilla);

            // === FASE 0 — LOS TALLITOS (la anticipación hecha carne):
            // tres brotes de sombra que se alzan ANTES del latigazo ===
            if (fase == FASE_APERTURA)
            {
                for (int k = 0; k < 3; k++)
                {
                    float ang = -MathHelper.PiOver2 + (k - 1) * 0.55f;
                    Vector2 punta = raiz + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (26f + 130f * SombrasLib.DeGolpe(t / 40f));
                    Vector2[] c = SombrasLib.Columna(raiz, punta, tiempo, semilla + k * 5, 8, 0.3f);
                    SombrasLib.Masa(c, 26f, 8f, 0.9f * disipa, semilla + k * 5, tiempo);
                    SombrasLib.BrumaColumna(c, 22f, 0.4f * disipa, tiempo, semilla + k * 5, 5);
                }
                return;
            }

            // === LOS TRES VERSOS (staging): tres zarpos, tres ángulos ===
            Vector2 objetivo = presa != null && presa.active ? presa.Center : dueño.MountedCenter;
            for (int k = 0; k < 3; k++)
            {
                Vector2 punta = presa != null && presa.active
                    ? (fase == FASE_VERSOS ? PuntaVerso(k, raiz, presa.Center, t) : presa.Center)
                    : Vector2.Lerp(Projectile.Center, raiz, 1f - disipa);
                Vector2[] col = SombrasLib.Columna(raiz, punta, tiempo, semilla + k * 7, 14, 0.2f);
                SombrasLib.Masa(col, 34f - k * 4f, 9f, 0.95f * disipa, semilla + k * 7, tiempo);

                // LA BRUMA NEGRA en cada zarpo (la petición del usuario)
                SombrasLib.BrumaColumna(col, 30f, 0.45f * disipa, tiempo, semilla + k * 7, 6);

                // los ojos de cada zarpo (el central lleva MÁS: es la boca)
                SombrasLib.OjosDeMasa(col, objetivo, 0.9f * disipa, semilla + k * 7, tiempo, k == 2 ? 3 : 2);
            }

            // === LA BOCA DEL VERSO CENTRAL (100% código — herencia del arma 3) ===
            if (presa != null && presa.active)
            {
                float apertura = fase switch
                {
                    FASE_VERSOS => 0.9f + 0.1f * MathF.Sin(tiempo * 8f),    // volando, bien abierta
                    FASE_JAULA => CicloMordida(t),
                    _ => 0.5f * disipa,
                };
                Vector2 rumbo = fase == FASE_JAULA
                    ? (presa.Center - raiz).SafeNormalize(Vector2.UnitX)
                    : (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 posBoca = fase == FASE_JAULA ? presa.Center : Projectile.Center;
                SombrasLib.Fauces(posBoca, rumbo, apertura * disipa, 66f, semilla + 2);

                // EL ALIENTO: la boca del verso central respira bruma negra
                SombrasLib.BrumaBoca(posBoca, rumbo, apertura * disipa, 0.5f * disipa, tiempo, semilla + 8, 5);
            }

            // === LA JAULA DE TINTA (la mecánica única del arma 4) ===
            if (fase == FASE_JAULA && presa != null && presa.active)
                DibujarJaula(presa, dueño, t, disipa, tiempo, semilla);
        }

        /// <summary>
        /// EL LIBRO DEL PORTADOR (v6.50.65 — LA CURA DE LOS RECTÁNGULOS):
        /// el sprite REAL del Grimorio del Eterno flotando sobre la cabeza
        /// (grande, con vaivén e inclinación) envuelto en su AURA violeta,
        /// con EL OJO colosal desplegado encima (mirando a la presa), la
        /// GARGANTA roja latiendo en el lomo y un hilo de BRUMA que baja
        /// de sus páginas — el libro de verdad, no dos barras negras.
        /// </summary>
        private static void DibujarLibro(Vector2 pos, NPC presa, byte fase, float t, float disipa, float tiempo)
        {
            float despliegue = fase == FASE_APERTURA ? SombrasLib.DeGolpe(t / 34f) : 1f;

            // EL VAIVÉN del libro flotante (nada flota quieto)
            float bob = MathF.Sin(tiempo * 1.6f) * 7f * despliegue;
            float inclina = MathF.Sin(tiempo * 0.9f) * 0.09f;
            Vector2 posLibro = pos + new Vector2(0f, -bob);

            // === 1. EL AURA: el resplandor violeta que enmarca al libro ===
            VFXCore.Begin();
            float pulsoAura = 0.16f + 0.07f * MathF.Sin(tiempo * 2.6f);
            VFXCore.Quad(posLibro, SombrasLib.Alfa(SombrasLib.Violeta, pulsoAura * despliegue * disipa),
                new Vector2(210f * despliegue, 230f * despliegue));
            VFXCore.Quad(posLibro, SombrasLib.Alfa(SombrasLib.RojoGarganta, 0.10f * despliegue * disipa),
                new Vector2(120f * despliegue, 160f * despliegue));
            VFXCore.FlushAdditive();

            // === 2. EL LIBRO DE VERDAD: el sprite del Grimorio del Eterno ===
            Texture2D tex;
            try
            {
                tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<Content.Weapons.GrimoireEternal>()].Value;
            }
            catch { tex = null; }
            if (tex != null)
            {
                float escala = (2.5f + 0.55f * despliegue) * disipa;
                if (fase == FASE_JAULA) escala *= 1.16f + 0.05f * MathF.Sin(tiempo * 9f);  // hora de comer: CRECE

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                        null, Main.Transform);
                    Main.spriteBatch.Draw(tex, posLibro - Main.screenPosition, null,
                        new Color(222, 214, 240, (byte)(255 * disipa)), inclina,
                        tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // === 3. LA GARGANTA ROJA: el lomo late entre las páginas ===
            if (despliegue > 0.5f)
            {
                VFXCore.Begin();
                float late = 0.30f + 0.16f * MathF.Sin(tiempo * 5.2f);
                VFXCore.Quad(posLibro + new Vector2(0f, 6f),
                    SombrasLib.Alfa(SombrasLib.Rojo, late * despliegue * disipa),
                    new Vector2(58f * despliegue, 108f * despliegue));
                VFXCore.FlushAdditive();
            }

            // === 4. EL OJO COLOSAL: desplegado SOBRE el libro, mirando a la presa ===
            float abre = fase == FASE_APERTURA
                ? MathHelper.Clamp(SombrasLib.DeGolpe((t - 26f) / 8f), 0f, 1f)
                : 1f;
            Vector2 posOjo = posLibro + new Vector2(0f, -(64f + 26f * despliegue) + bob * 0.5f);
            Vector2 mira = presa != null && presa.active ? presa.Center - posOjo : new Vector2(0f, 200f);
            float tamOjo = (30f + 12f * despliegue) * (fase == FASE_JAULA ? 1.35f : 1f);
            VFXCore.Begin();
            SombrasLib.Ojo(posOjo, tamOjo, mira, abre * disipa);
            VFXCore.FlushAdditive();

            // === 5. LA BRUMA DEL LIBRO: un hilo de sombra que baja de las
            // páginas al charco — el libro respira hacia la raíz ===
            if (despliegue > 0.6f)
            {
                Vector2 arriba = posLibro + new Vector2(0f, -14f);
                Vector2 abajo = posLibro + new Vector2(0f, 78f);
                Vector2[] hilo = SombrasLib.Columna(arriba, abajo, tiempo, 7, 6, 0.10f);
                SombrasLib.BrumaColumna(hilo, 24f, 0.35f * disipa, tiempo, 7, 3);
            }
        }

        /// <summary>
        /// LA JAULA DE TINTA: 12 púas negras de punta BLANCA alrededor del
        /// jefe que se CIERRAN de golpe al ritmo de las mordidas (cada 28
        /// t — el snap), ojos entre las púas que miran al portador, la
        /// bruma del anillo, el destello y la onda de choque del impacto
        /// (los primeros 14 t) y las almas volando a casa.
        /// </summary>
        private static void DibujarJaula(NPC presa, Player dueño, float t, float disipa, float tiempo, int semilla)
        {
            float radio = MathHelper.Clamp(presa.Size.Length() * 0.62f, 95f, 250f);

            // EL RITMO DE LA JAULA: se abre lenta… ¡CIERRA de golpe! …mastica… reabre
            float m = t % 28f;
            float contracción = m < 5f
                ? MathHelper.Lerp(1f, 0.7f, SombrasLib.DeGolpe(m / 5f))                    // ¡CIERRA!
                : m < 17f ? 0.7f + 0.03f * MathF.Sin(m * 1.5f)                              // sostiene masticando
                : MathHelper.Lerp(0.7f, 1f, (m - 17f) / 11f);                               // reabre

            float rActual = radio * contracción;
            float largoPúa = radio * 0.44f;
            int nPúas = 12;

            // === LAS GARRAS (v6.50.65 — la cura de las «púas rectangulares»):
            // 12 zarpos AFILADOS de tres segmentos que se estrechan y se
            // curvan en gancho alternado hacia el jefe, con la punta de
            // HUESO blanca — leen como zarpas de sombra, no como barras ===
            for (int k = 0; k < nPúas; k++)
            {
                float ang = k / (float)nPúas * MathHelper.TwoPi + tiempo * 0.12f;   // la jaula gira lenta
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang) * 0.86f);
                Vector2 fuera = presa.Center + dir * (rActual + largoPúa * 0.18f);
                float gancho = (k % 2 == 0 ? 1f : -1f) * 0.55f;
                SombrasLib.Garra(fuera, -dir, largoPúa, 0.95f * disipa, gancho);
            }

            // LOS OJOS ENTRE LAS GARRAS: la jaula TE MIRA mientras come
            // (v6.50.65 — MÁS GRANDES y con el halo de la casa: PRENDEN)
            VFXCore.Begin();
            for (int k = 0; k < 6; k++)
            {
                float ang = (k + 0.5f) / 6f * MathHelper.TwoPi + tiempo * 0.12f;
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang) * 0.86f);
                Vector2 pos = presa.Center + dir * (rActual + 30f);
                float abierto = 0.75f + 0.25f * MathF.Sin(tiempo * 3.5f + k * 1.9f);
                SombrasLib.Ojo(pos, 18f + 9f * (k % 3), dueño.MountedCenter - pos, abierto * disipa);
            }
            VFXCore.FlushAdditive();

            // LA BRUMA DEL ANILLO (la jaula también exhala)
            Vector2[] anillo = new Vector2[14];
            for (int i = 0; i < 14; i++)
            {
                float aI = i / 13f * MathHelper.TwoPi + tiempo * 0.12f;
                anillo[i] = presa.Center + new Vector2(MathF.Cos(aI), MathF.Sin(aI) * 0.86f) * rActual;
            }
            SombrasLib.BrumaColumna(anillo, radio * 0.5f, 0.4f * disipa, tiempo, semilla + 19, 12);

            // === EL JUICE DEL IMPACTO (los primeros 18 t — v6.50.65 ×2):
            // destello rojo GRANDE + doble onda de choque + destello blanco ===
            if (t < 10f)
            {
                VFXCore.Begin();
                VFXCore.Quad(presa.Center, SombrasLib.Alfa(SombrasLib.Rojo, 0.40f * (1f - t / 10f) * disipa),
                    new Vector2(460f, 460f));
                VFXCore.Quad(presa.Center, SombrasLib.Alfa(SombrasLib.Blanco, 0.25f * (1f - t / 10f) * disipa),
                    new Vector2(220f, 220f));
                VFXCore.FlushAdditive();
            }
            if (t < 18f)
            {
                SombrasLib.OndaChoque(presa.Center, 70f + t * 32f, 0.60f * (1f - t / 18f) * disipa);
                if (t > 4f)
                    SombrasLib.OndaChoque(presa.Center, 40f + (t - 4f) * 26f, 0.45f * (1f - t / 18f) * disipa, false);
            }

            // LAS ALMAS: la vida vuela al portador mientras la jaula aprieta
            VFXCore.Begin();
            for (int k = 0; k < 5; k++)
            {
                float prog = SombrasLib.Frac(t * 0.014f + k * 0.2f);
                Vector2 a = presa.Center + new Vector2(MathF.Cos(k * 2.4f + tiempo * 0.7f), MathF.Sin(k * 2.1f)) * radio * 0.4f;
                Vector2 b = dueño.MountedCenter;
                Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 110f, -140f);
                float u = 1f - prog;
                Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                SombrasLib.Alma(pos, 9f + 5f * MathF.Sin(tiempo * 5f + k), 0.8f * disipa);
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>El ciclo de la mordida de la jaula: abre · ¡CIERRA de golpe! · mastica.</summary>
        private static float CicloMordida(float t)
        {
            float m = t % 22f;
            if (m < 5f) return MathHelper.Lerp(0.9f, 0.1f, SombrasLib.DeGolpe(m / 5f));   // ¡CIERRA!
            if (m < 12f) return MathHelper.Lerp(0.1f, 0.95f, (m - 5f) / 7f);              // reabre
            return 0.85f + 0.1f * MathF.Sin(m * 1.1f);                                    // sostiene
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
