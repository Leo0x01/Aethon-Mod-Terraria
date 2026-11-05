using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// EstandarteJuicioAuraProjectile — v6.98 — EL AURA DEL JUICIO.
    ///
    /// LA LETRA: «un accesorio que crea un aura alrededor del jugador
    /// que daña a los enemigos si se acercan y cuanto mas se acerquen
    /// mas se dañan […] con la superforja y nuestras librerias
    /// mejoradas ahora serias capaz de crearlo algo similar o incluso
    /// superior al concepto original con mejores VFX y texturas».
    ///
    /// EL CONCEPTO (la idea del estandarte de la referencia del
    /// usuario, la implementación 100% de la casa): un ANCLA invisible que vive pegada al portador
    /// mientras lleva el estandarte. Cada 12 ticks JUZGA a los 8
    /// enemigos más cercanos dentro del aura (24 tiles):
    ///   · EL DAÑO crece con la PROXIMIDAD: ×1 al borde, ×2,5 pegado
    ///     al portador (la letra exacta: cuanto más cerca, más duele).
    ///   · EL JUICIO SE ARMA: +8% por segundo que el impío permanece
    ///     dentro, hasta +80% (la rampa del concepto original).
    ///   · LA BASE escala con el nivel de despertar: 40 + 12×nivel.
    ///   · LA LLAMA DEL ALBA: cada juicio aplica el debuff dorado
    ///     (40 HP/s) y REPELE al que toca al portador.
    ///
    /// EL VESTIDO (10 CAPAS — el motor de capas de la v6.98):
    ///   1. EL BANNER EN LA ESPALDA (el estandarte ondeando detrás
    ///      del jugador — los proyectiles dibujan antes que el
    ///      jugador: queda DETRÁS de verdad) — tira de 4 marcos
    ///      forjada con la superforja, en lote ALPHA.
    ///   2. EL ANILLO DEL SUELO (la frontera del juicio).
    ///   3. EL VELO DE RUIDO (el flipbook procedural Ruido_0..19 —
    ///      el suelo dorado que vive).
    ///   4. EL ARO RÚNICO girando (MagicRing — la ley del alba).
    ///   5. EL FIRERING PULSANTE del borde (las brasas de la frontera).
    ///   6. LAS BRASAS QUE SUBEN (motas doradas deterministas).
    ///   7. LAS LLAMAS DEL JUICIO sobre cada enemigo dentro — SU
    ///      BRILLO ES SU CULPA: más cerca del portador = más intensa
    ///      (¡la mecánica hecha VISIBLE!).
    ///   8. EL CORAZÓN bajo los pies del portador.
    ///   9. LAS CHISPAS ORBITANDO (SparkleStar deterministas).
    ///  10. LA CORONA DEL BANNER (el destello de la punta).
    ///  TODO lo demás en UN lote aditivo (el patrón de la casa:
    ///  VFXCore cierra · el lote viste · VFXCore reabre el vanilla).
    /// </summary>
    public class EstandarteJuicioAuraProjectile : ModProjectile
    {
        public override string Texture =>
            "AethonMod/Content/Projectiles/EstandarteJuicioAuraBanner";

        /// <summary>EL RADIO del juicio (px — 24 tiles).</summary>
        private const float RADIO = 384f;

        /// <summary>LA CADENCIA: ticks entre juicios por impío.</summary>
        private const int CADENCIA = 12;

        /// <summary>CUÁNTOS impíos a la vez (los más cercanos).</summary>
        private const int MAX_BLANCOS = 8;

        /// <summary>El reloj del juicio (npc.whoAmI → el tick que podrá volver a ser juzgado).</summary>
        private static readonly Dictionary<int, int> _proximoJuicio = new();

        /// <summary>LA RAMPA (npc.whoAmI → ticks que lleva DENTRO del aura).</summary>
        private static readonly Dictionary<int, int> _rampa = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;      // la tira del banner (26×160 = 4×40)
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead ||
                !dueno.GetModPlayer<JuicioPlayer>().EstandarteJuicio)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;        // inmortal mientras el flag viva

            // === LA LIMPIEZA (cada 60 t: las rampas de los muertos se van) ===
            if (Main.GameUpdateCount % 60ul == 0ul)
            {
                List<int> muertos = null;
                foreach (int k in _rampa.Keys)
                {
                    NPC n = Main.npc[k];
                    if (n == null || !n.active || n.life <= 0)
                        (muertos ??= new List<int>()).Add(k);
                }
                if (muertos != null)
                    foreach (int k in muertos) { _rampa.Remove(k); _proximoJuicio.Remove(k); }
            }

            // === LA LECTURA DEL CAMPO (todos los clientes la ven: el Vestido) ===
            float sumaProx = 0f;
            int dentro = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float dist = MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center));
                if (dist > RADIO) continue;
                float prox = 1f - dist / RADIO;
                dentro++;
                sumaProx += prox;
            }

            // === EL JUICIO (SOLO el cliente dueño golpea: MP-seguro) ===
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                // LOS MÁS CERCANOS PRIMERO (la selección de la casa: los 8
                // mejores por proximidad — sin ordenar nada, cero GC).
                for (int elegido = 0; elegido < MAX_BLANCOS; elegido++)
                {
                    int mejor = -1;
                    float mejorProx = -1f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                        if (_proximoJuicio.TryGetValue(n.whoAmI, out int hasta) &&
                            hasta > (int)Main.GameUpdateCount) continue;   // ya juzgado este ciclo
                        float dist = MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center));
                        if (dist > RADIO) continue;
                        float prox = 1f - dist / RADIO;
                        if (prox > mejorProx) { mejorProx = prox; mejor = i; }
                    }
                    if (mejor < 0) break;
                    Juzgar(Main.npc[mejor], dueno, mejorProx);
                }
            }

            // === LA INTENSIDAD (el motor del Vestido: cuánta culpa hay dentro) ===
            float inten = 0.30f + Math.Min(0.70f, sumaProx * 0.22f);
            Projectile.localAI[0] = inten;
            Projectile.localAI[1] = dentro;
        }

        /// <summary>EL JUICIO: el golpe que escala con la proximidad, se
        /// arma con el tiempo, quema con el alba y repele al osado.</summary>
        private void Juzgar(NPC n, Player dueno, float prox)
        {
            int nivel = dueno.GetModPlayer<ShardPlayer>().NivelDespertar;
            _rampa.TryGetValue(n.whoAmI, out int ticksDentro);
            _rampa[n.whoAmI] = ticksDentro + CADENCIA;
            _proximoJuicio[n.whoAmI] = (int)Main.GameUpdateCount + CADENCIA;

            float proxMult = 1f + 1.5f * prox;                       // ×1 al borde → ×2,5 pegado
            float rampaMult = 1f + Math.Min(0.80f, ticksDentro * (0.08f / 60f)); // +8%/s → +80%
            int dano = (int)((40 + 12 * nivel) * proxMult * rampaMult);

            int dir = n.Center.X >= dueno.Center.X ? 1 : -1;
            dueno.ApplyDamageToNPC(n, dano, 2.5f, dir, false);
            n.AddBuff(ModContent.BuffType<JuicioDelAlba>(), 120);

            // EL CHISPAZO DORADO del veredicto.
            if (!Main.dedServ)
            {
                for (int k = 0; k < 3; k++)
                {
                    Dust d = Dust.NewDustPerfect(n.Center,
                        DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2.6f, -0.4f)),
                        160, new Color(255, 208, 96), 0.6f);
                    d.noGravity = true;
                }
            }
        }

        // ================================================================
        //  EL VESTIDO (las 10 capas — el motor de la v6.98)
        // ================================================================
        private static Texture2D[] _ruidoCache;

        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int n = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (n < 0) n += 20;
            return _ruidoCache[n] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + n).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta).Value;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) return false;
            if (dueno.GetModPlayer<JuicioPlayer>().EstandarteJuicioOculto) return false;

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                SpriteBatch sb = Main.spriteBatch;
                float t = Main.GlobalTimeWrappedHourly;
                float inten = MathHelper.Clamp(Projectile.localAI[0], 0.25f, 1f);
                float late = 0.85f + 0.15f * MathF.Sin(t * 3.1f);
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Vector2 suelo = pos + new Vector2(0f, 26f);
                Color oro = new(255, 208, 96);
                Color luz = new(255, 248, 224);

                // ── CAPA 1 — EL BANNER EN LA ESPALDA (lote ALPHA: el
                //    paño es tela, no luz — y los proyectiles dibujan
                //    ANTES que el jugador: queda detrás de verdad)
                Texture2D banner = ModContent.Request<Texture2D>(Texture).Value;
                int frame = (int)(t * 7.5f) % 4;
                Rectangle src = new(0, frame * 40, 26, 40);
                Vector2 anclaBanner = pos + new Vector2(-dueno.direction * 7f,
                    -30f + MathF.Sin(t * 2.2f) * 1.5f);
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                sb.Draw(banner, anclaBanner, src, Color.White * 0.96f,
                    MathF.Sin(t * 2.2f) * 0.06f, new Vector2(13f, 36f), 1f,
                    SpriteEffects.None, 0f);
                sb.End();

                // ── CAPAS 2..10 (UN lote aditivo: el juicio BRILLA)
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                Texture2D circulo = Tex("GlowCircleWhite");
                Texture2D anillo = Tex("MagicRing");
                Texture2D fuego = Tex("Procedural/FireRing");
                Texture2D destello = Tex("FlareEstrella4Puntas");
                Texture2D chispa = Tex("SparkleStar");
                Texture2D orbe = Tex("GlowOrb");

                // CAPA 2 — EL ANILLO DEL SUELO (la frontera del juicio)
                Suelo(sb, circulo, suelo, oro * (0.11f * inten * late), 0f, RADIO * 2.15f, RADIO * 0.85f);
                // CAPA 3 — EL VELO DE RUIDO (el suelo dorado que vive)
                Suelo(sb, Ruido(0, 2.4f), suelo, oro * (0.08f * inten), t * 0.15f, RADIO * 2f, RADIO * 0.8f);
                // CAPA 4 — EL ARO RÚNICO (la ley del alba, girando)
                Suelo(sb, anillo, suelo, luz * (0.20f * inten), t * 1.2f, RADIO * 2.05f, RADIO * 0.8f);
                // CAPA 5 — EL FIRERING PULSANTE (las brasas de la frontera)
                Suelo(sb, fuego, suelo, oro * (0.13f + 0.10f * inten * late), -t * 0.8f,
                    RADIO * 2.2f * (1f + 0.04f * late), RADIO * 0.9f);
                // CAPA 6 — LAS BRASAS QUE SUBEN (motas deterministas)
                for (int i = 0; i < 6; i++)
                {
                    float az = Azar(i);
                    float sube = (t * 46f + az * 110f) % 110f;
                    Vector2 p = new(suelo.X + (az * 2f - 1f) * RADIO * 0.8f, suelo.Y - sube);
                    Capa(sb, orbe, p, oro * (0.30f * (1f - sube / 110f) * inten + 0.05f), 0f, 13f / 128f);
                }
                // CAPA 7 — LAS LLAMAS DEL JUICIO: SU BRILLO ES SU CULPA
                //    (más cerca del portador = la llama MÁS intensa)
                int llamas = 0;
                for (int i = 0; i < Main.maxNPCs && llamas < 5; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    float dist = MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center));
                    if (dist > RADIO) continue;
                    float prox = 1f - dist / RADIO;
                    Vector2 p = n.Center - Main.screenPosition;
                    float esc = (30f + 44f * prox) / 256f;
                    Capa(sb, destello, p, oro * (0.35f + 0.55f * prox), t * 2.4f + i, esc);
                    Capa(sb, chispa, p, luz * (0.30f + 0.45f * prox), -t * 3f, esc * 0.55f);
                    llamas++;
                }
                // CAPA 8 — EL CORAZÓN bajo los pies del portador
                Capa(sb, circulo, suelo - new Vector2(0f, 6f),
                    luz * (0.14f * inten * late), 0f, 90f / 64f);
                // CAPA 9 — LAS CHISPAS ORBITANDO
                for (int i = 0; i < 3; i++)
                {
                    float a = t * (1.5f + Azar(i + 11) * 1.2f) + Azar(i + 4) * MathHelper.TwoPi;
                    Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.55f) * (54f + Azar(i) * 26f);
                    Capa(sb, chispa, p, oro * (0.25f + 0.45f * Titila(i)), a,
                        (12f + 9f * Titila(i)) / 32f);
                }
                // CAPA 10 — LA CORONA DEL BANNER (el destello de la punta)
                Vector2 punta = anclaBanner + new Vector2(0f, -38f);
                Capa(sb, destello, punta, luz * (0.4f + 0.35f * Titila(9, 5f)),
                    t * 2.6f, (34f + 14f * Titila(9, 5f)) / 256f);
                sb.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>EL AZAR DETERMINISTA (estable entre frames).</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
            return v - MathF.Floor(v);
        }

        /// <summary>EL TITILAR (0..1..0).</summary>
        private static float Titila(int i, float vel = 3.1f)
            => 0.5f - 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * vel + Azar(i) * MathHelper.TwoPi);

        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float esc)
        {
            if (color.A == 0 || esc <= 0f) return;
            sb.Draw(tex, pos, null, color, rot, tex.Size() * 0.5f, esc,
                SpriteEffects.None, 0f);
        }

        private static void Suelo(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float anchoPx, float altoPx)
        {
            if (color.A == 0 || anchoPx <= 0f) return;
            sb.Draw(tex, pos, null, color, rot, tex.Size() * 0.5f,
                new Vector2(anchoPx / tex.Width, altoPx / tex.Height),
                SpriteEffects.None, 0f);
        }
    }
}
