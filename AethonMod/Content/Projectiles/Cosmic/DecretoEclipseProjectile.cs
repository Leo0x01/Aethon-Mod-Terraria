using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// DecretoEclipseProjectile — v6.31 — EL CÍRCULO DEL ECLIPSE del bastón
    /// "El Decreto del Eclipse" (research/v631,
    /// INFORME_STAR_TOMB_DRAGON_WORD.md ficha 4 + §6.4 — el decreto del
    /// dragón, destilado a la casa como arma independiente: el círculo
    /// vive SU vida entera clavado en el mundo, como el desgarro).
    ///
    /// LA VIDA DEL CÍRCULO (~290 ticks):
    ///   · NACE pequeño (80 px) en el punto del cursor y CRECE +2 px/tick
    ///     hasta 660 px mientras vive; mientras tanto el mundo se
    ///     OSCURECE (RiftLib.Oscurecer(0.15) — es un eclipse).
    ///   · LA EJECUCIÓN CÍCLICA: cada 15 ticks una ONDA VIAJERA sale del
    ///     centro y cuando llega al borde (~8 ticks) TODO enemigo del
    ///     círculo recibe un corte — daño por PRIORIDAD: ordenado por
    ///     distancia al centro, el primero paga ×4.8·base
    ///     (0.2 + 255/55 con num--) y decae hasta ×0.2 — más Quemadura
    ///     Cósmica 3 s.
    ///   · LA MARCA DEL OJO: cada enemigo del área lleva un OJO que lo
    ///     mira (esclerótica + iris + 3 zarpazos); su PUPILA VERTICAL se
    ///     CONTRAE los 6 ticks previos al tajo.
    ///   · LOS GLIFOS: 24 glifos rúnicos (cápsulas cortas con punta
    ///     clara) cabalgan el anillo y se RE-ESCRIBEN en cada ejecución.
    ///   · El anillo es fuego oscuro de eclipse: negro-violeta con borde
    ///     ámbar DESGARRADO (muescas/dientes deterministas por hash, no
    ///     liso) girando lento.
    ///
    /// Determinismo MP: la cronología entera deriva de la edad (misma en
    /// todas las máquinas); la semilla visual es Projectile.identity; el
    /// daño solo en `Main.netMode != MultiplayerClient` con
    /// SimpleStrikeNPC (escuela A) + EsObjetivo; el visual solo cliente
    /// (PreDraw) y SIN Main.rand (Hash01 + identity).
    /// </summary>
    public class DecretoEclipseProjectile : ModProjectile
    {
        // === LA VIDA DEL CÍRCULO (ticks / px) ===
        /// <summary>El radio al nacer (80 px).</summary>
        private const float RadioNacimiento = 80f;

        /// <summary>El radio máximo (660 px = 41,25 tiles).</summary>
        private const float RadioMax = 660f;

        /// <summary>El crecimiento: +2 px/tick.</summary>
        private const float Crecimiento = 2f;

        /// <summary>La vida total: (660−80)/2 = 290 ticks exactos.</summary>
        public const int VidaTicks = (int)((RadioMax - RadioNacimiento) / Crecimiento);

        /// <summary>El BEAT de la ejecución (15 ticks = 0,25 s).</summary>
        private const int Beat = 15;

        /// <summary>El viaje de la onda: del centro al borde en 8 ticks.</summary>
        private const int OndaTicks = 8;

        /// <summary>La contracción de la pupila: empieza 6 ticks antes del tajo.</summary>
        private const int ContraeTicks = 6;

        /// <summary>Los glifos rúnicos alrededor del anillo.</summary>
        private const int GlifosCuenta = 24;

        /// <summary>El tope de marcas del ojo por pase (la cuenta del original).</summary>
        private const int MarcaCap = 24;

        /// <summary>El contador del decaimiento: ×4.83 el primero, mín ×0.2.</summary>
        private const int NumInicial = 255;

        /// <summary>Quemadura Cósmica al caer el corte (3 s).</summary>
        private const int QuemaduraTicks = 180;

        // === LA PALETA (el eclipse: negro-violeta + fuego ámbar) ===
        private static readonly Color ColorOro = new(255, 214, 110);       // oro eclipse
        private static readonly Color ColorNaranja = new(255, 128, 36);    // naranja del borde
        private static readonly Color ColorBrasa = new(214, 58, 22);       // brasa arrastrada
        private static readonly Color ColorVioleta = new(150, 80, 255);    // fuego oscuro violeta
        private static readonly Color ColorBlancoCorona = new(255, 244, 214); // blanco-corona

        private static readonly Color[] PaletaChispa = { ColorOro, ColorBrasa, ColorVioleta };

        private float _age;
        private bool _nacio;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks;
            // El decreto importa en red: que nadie se lo pierda.
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red: la misma en todas las máquinas).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 57);

        // ==============================================================
        //  EL RELOJ DEL BEAT — todo deriva de la edad
        // ==============================================================

        /// <summary>La posición dentro del ciclo del beat (0..15).</summary>
        private float Cyc => _age % Beat;

        /// <summary>LA SOBRE-EXPOSICIÓN tras el impacto (los 5 ticks tras el beat).</summary>
        private float Flare => Cyc < 5f ? 1f - Cyc / 5f : 0f;

        /// <summary>LA CONTRACCIÓN de la pupila: 0→1 en los 6 ticks previos al tajo.</summary>
        private float Contraccion => Cyc >= Beat - ContraeTicks
            ? (Cyc - (Beat - ContraeTicks)) / ContraeTicks
            : 0f;

        /// <summary>El progreso de la ONDA VIAJERA (−1 = en pausa; 0..1 viajando).</summary>
        private float WaveT => Cyc >= Beat - OndaTicks
            ? (Cyc - (Beat - OndaTicks)) / OndaTicks
            : -1f;

        /// <summary>El número de ejecuciones ya caídas (re-escribe los glifos).</summary>
        private int BeatIndex => (int)(_age / Beat);

        /// <summary>El radio ACTUAL: 80 px + 2·edad, tope 660.</summary>
        private float Radio => MathHelper.Min(RadioNacimiento + Crecimiento * _age, RadioMax);

        // ==============================================================
        //  LA VIDA DEL CÍRCULO — AI
        // ==============================================================

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: el decreto se clava y el día se apaga ===
            if (!_nacio)
            {
                _nacio = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item77.WithPitchOffset(-0.5f), Projectile.Center); }
                    catch { }
                    OndaLib.Kick(1.4f, 6);
                    OndaLib.Flash(ColorVioleta, 0.14f, 6);
                    RiftLib.ChispasAnomalia(Projectile.Center, 8, PaletaChispa, Seed + 7, out ParticleData[] motas);
                    Spawn(motas);
                }
            }

            // === EL ECLIPSE: el mundo se oscurece mientras el círculo vive ===
            if (Main.netMode != NetmodeID.Server)
                RiftLib.Oscurecer(0.15f);

            // === LA EJECUCIÓN CÍCLICA: la onda LLEGÓ al borde ===
            if (_age % Beat == 0f)
                Ejecutar();

            // === LA LUZ DEL ECLIPSE: ámbar-violeta latiendo con el beat,
            //     más 6 puntos del borde (la corona iluminando el terreno).
            float latido = 0.8f + 0.5f * Flare;
            Lighting.AddLight(Projectile.Center, 0.60f * latido, 0.34f * latido, 0.55f * latido);
            for (int k = 0; k < 6; k++)
            {
                float ang = k / 6f * MathHelper.TwoPi + _age * 0.011f;
                Lighting.AddLight(Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Radio,
                    0.35f, 0.19f, 0.30f);
            }

            // === LAS BRASAS: motas ascendiendo dentro del círculo
            //     (presupuesto 1 + R/240 cada 4 ticks, hash puro).
            if (Main.netMode != NetmodeID.Server && _age % 4f == 0f)
            {
                int presupuesto = 1 + (int)(Radio / 240f);
                float ha = VFXCore.Hash01(Seed, (int)_age, 501) * MathHelper.TwoPi;
                float hr = MathF.Sqrt(VFXCore.Hash01(Seed, (int)_age, 502)) * Radio * 0.88f;
                Vector2 punto = Projectile.Center + new Vector2(MathF.Cos(ha), MathF.Sin(ha)) * hr;
                RiftLib.ChispasAnomalia(punto, presupuesto, PaletaChispa, Seed + (int)_age, out ParticleData[] motas);
                Spawn(motas);
            }
        }

        // ==============================================================
        //  LA EJECUCIÓN — el corazón (escuela A — solo server/singleplayer)
        // ==============================================================

        /// <summary>
        /// LA EJECUCIÓN CÍCLICA: la onda llegó al borde — TODO enemigo
        /// dentro del círculo recibe un corte ordenado por distancia al
        /// centro (daño `base·(0.2 + num/55)`, num=255 decreciente: ×4.83
        /// el primero, mínimo ×0.2) + Quemadura Cósmica 3 s. Game feel
        /// solo si hubo víctimas: flash + kick + tajo.
        /// </summary>
        private void Ejecutar()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            float radio = Radio;
            float radioSq = radio * radio;
            List<NPC> dentro = new();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).LengthSquared() <= radioSq)
                    dentro.Add(npc);
            }
            if (dentro.Count == 0) return;

            // LA PRIORIDAD: el más cercano al centro paga primero.
            dentro.Sort((a, b) =>
                (a.Center - Projectile.Center).LengthSquared()
                    .CompareTo((b.Center - Projectile.Center).LengthSquared()));

            int num = NumInicial;
            foreach (NPC npc in dentro)
            {
                float mult = MathF.Max(0.2f, 0.2f + num / 55f); // ×4.83 → ×0.2
                num--;
                int dmg = Math.Max(1, (int)(Projectile.damage * mult));
                int dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                npc.SimpleStrikeNPC(dmg, dir, false, 0f, DamageClass.Magic);
                // El decreto quema 3 s con cada corte.
                try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), QuemaduraTicks); } catch { }
            }

            // EL GAME FEEL del tajo (solo con víctimas).
            if (Main.netMode != NetmodeID.Server)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9.WithPitchOffset(0.35f), Projectile.Center);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.DD2_BetsyFireballShot.WithVolumeScale(0.35f).WithPitchOffset(-0.55f),
                        Projectile.Center);
                }
                catch { }
                OndaLib.Flash(ColorOro, 0.13f, 5);
                OndaLib.Kick(0.9f, 5);
            }
        }

        // ==============================================================
        //  LA MUERTE — el eclipse termina
        // ==============================================================

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // El círculo se cierra en un último destello.
            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(-0.2f), Projectile.Center); }
            catch { }
            OndaLib.Flash(ColorOro, 0.18f, 6);
            OndaLib.Kick(2.2f, 6);
            RiftLib.ChispasAnomalia(Projectile.Center, 14, PaletaChispa, Seed + 99, out ParticleData[] motas);
            Spawn(motas);
        }

        private static void Spawn(ParticleData[] motas)
        {
            if (motas == null) return;
            for (int i = 0; i < motas.Length; i++)
                ParticleManager.Spawn(motas[i]);
        }

        // ==============================================================
        //  EL VISUAL — PreDraw (contrato de batch v6.10)
        // ==============================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawDecreto(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del pase
        /// de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ==============================================================
        //  EL ECLIPSE ENTERO — la sombra (alpha) + el fuego (aditivo)
        // ==============================================================

        private void DrawDecreto()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            float R = Radio;
            float fade = MathHelper.Clamp(_age / 10f, 0f, 1f)
                       * MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float flare = Flare;
            float contraccion = Contraccion;
            float waveT = WaveT;
            int beatIndex = BeatIndex;
            float spin = _age * 0.011f; // el anillo gira LENTO

            // ============================================================
            //  PASO 1 — LA SOMBRA DE LA LUNA (alpha): el interior oscuro
            //  del eclipse + la banda negra-violeta pegada al borde.
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            DrawSombra(Main.spriteBatch, drawPos, R, fade, spin);
            Main.spriteBatch.End();

            // ============================================================
            //  PASO 2 — EL FUEGO DEL ECLIPSE (aditivo): la corona
            //  desgarrada, los glifos, la onda viajera, la garganta y
            //  las marcas del ojo sobre cada enemigo.
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            DrawCorona(Main.spriteBatch, drawPos, R, fade, flare, seed, spin, time);
            DrawGlifos(Main.spriteBatch, drawPos, R, fade, seed, beatIndex, flare, spin, time);
            if (waveT >= 0f)
                DrawOnda(Main.spriteBatch, drawPos, R, fade, waveT);

            // LA GARGANTA DEL DECRETO — el glow central respira con el beat.
            LumenLib.Bloom(Main.spriteBatch, drawPos, 95f * (0.9f + 0.25f * flare),
                ColorOro, (0.32f + 0.30f * flare) * fade, 3);
            LumenLib.Bloom(Main.spriteBatch, drawPos, 175f, ColorNaranja, 0.18f * fade, 2);

            DrawOjos(Main.spriteBatch, R, fade, contraccion, flare, seed, time);

            Main.spriteBatch.End();
        }

        /// <summary>LA SOMBRA: el lavado oscuro del interior (la noche del
        /// eclipse) + la banda chamuscada negra-violeta junto al borde.</summary>
        private static void DrawSombra(SpriteBatch batch, Vector2 drawPos, float R, float alpha, float spin)
        {
            Texture2D glow = VFXCore.SoftGlow;

            // EL INTERIOR: un disco oscuro suave (violeta-negro).
            Vector2 wash = new Vector2(R * 2.05f) / glow.Size();
            batch.Draw(glow, drawPos, null, new Color(24, 10, 44) * (0.40f * alpha),
                0f, glow.Size() * 0.5f, wash, SpriteEffects.None, 0f);

            // LA BANDA CHAMUSCADA: dos anillos oscuros pegados al borde.
            Texture2D ring = VFXCore.Ring;
            Vector2 size = VFXCore.RingQuadSize(R * 0.965f);
            batch.Draw(ring, drawPos, null, new Color(30, 12, 54) * (0.55f * alpha),
                spin * 0.5f, ring.Size() * 0.5f, size / ring.Size(), SpriteEffects.None, 0f);
            Vector2 size2 = VFXCore.RingQuadSize(R * 0.995f);
            batch.Draw(ring, drawPos, null, new Color(16, 5, 34) * (0.45f * alpha),
                -spin * 0.3f, ring.Size() * 0.5f, size2 / ring.Size(), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA CORONA DESGARRADA: el borde NUNCA es un círculo matemático —
        /// muescas/dientes deterministas por hash (algunos dientes LARGOS
        /// hacia fuera), fuego oscuro violeta ARRASTRADO hacia dentro y un
        /// corte exterior ámbar NÍTIDO. Grosor 30+R·0.02 (ficha 4).
        /// </summary>
        private static void DrawCorona(SpriteBatch batch, Vector2 drawPos, float R,
            float alpha, float flare, int seed, float spin, float time)
        {
            const int Tramos = 60;
            float th = 30f + R * 0.02f;
            Vector2 prevEdge = Vector2.Zero, prevInner = Vector2.Zero;

            for (int i = 0; i <= Tramos; i++)
            {
                int idx = i % Tramos;
                float ang = idx / (float)Tramos * MathHelper.TwoPi + spin;
                float h = VFXCore.Hash01(seed, idx, 17);
                float h2 = VFXCore.Hash01(seed, idx, 29);

                // LA MUESCA: diente fijo por celda + flujo lento del fuego.
                float diente = (h - 0.5f) * 0.08f;
                if (h2 > 0.74f) diente += (h2 - 0.74f) * 0.50f; // el diente LARGO
                diente *= 1f + 0.12f * MathF.Sin(time * 0.8f + idx * 1.9f);

                float rr = R * (1f + diente);
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                Vector2 edge = drawPos + dir * rr;
                Vector2 inner = drawPos + dir * (rr - th * 1.5f);

                if (i > 0)
                {
                    float brillo = alpha * (0.55f + 0.45f * h) * (1f + 0.9f * flare);

                    // EL FUEGO OSCURO: banda violeta arrastrada hacia dentro.
                    Barra(batch, prevInner, inner, th * 1.00f, Tint(ColorVioleta, 0.30f * brillo));
                    // LA BRASA: naranja entre medio.
                    Barra(batch, prevInner, inner, th * 0.55f, Tint(ColorBrasa, 0.45f * brillo));
                    // EL CORTE EXTERIOR NÍTIDO: ámbar claro, fino, en el borde.
                    Barra(batch, prevEdge, edge, th * 0.32f, Tint(ColorOro, 0.85f * brillo));
                }
                prevEdge = edge;
                prevInner = inner;
            }
        }

        /// <summary>
        /// LOS GLIFOS RÚNICOS: 24 cápsulas cortas con punta clara cabalgando
        /// el anillo — el hash VIAJA con el número de beat, así que TODO el
        /// anillo se RE-ESCRIBE en cada ejecución (algunas celdas quedan en
        /// blanco). Su brillo respira con el beat y dispara con el flare.
        /// </summary>
        private void DrawGlifos(SpriteBatch batch, Vector2 drawPos, float R, float alpha,
            int seed, int beatIndex, float flare, float spin, float time)
        {
            float respira = 0.42f + 0.58f * (Cyc / Beat);
            float brillo = (respira + 0.9f * flare) * alpha;
            Texture2D glow = VFXCore.SoftGlow;

            for (int g = 0; g < GlifosCuenta; g++)
            {
                float ang = g / (float)GlifosCuenta * MathHelper.TwoPi + spin;
                float breathe = 1f + 0.05f * MathF.Sin(time * 1.6f + g * 1.31f);
                Vector2 pos = drawPos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (R * 0.99f * breathe);

                // EL GLIFO SE RE-ESCRIBE: el hash cambia con el beat.
                float h1 = VFXCore.Hash01(seed + beatIndex * 613, g, 101);
                float h2 = VFXCore.Hash01(seed + beatIndex * 613, g, 113);
                if (h1 < 0.16f) continue; // algunas celdas quedan en blanco

                // El resplandor suave detrás (el grabado ardiendo).
                batch.Draw(glow, pos, null, Tint(ColorNaranja, 0.18f * brillo), 0f,
                    glow.Size() * 0.5f, new Vector2(26f) / glow.Size(), SpriteEffects.None, 0f);

                // EL TRAZO cápsula: cuerpo naranja → PUNTA CLARA.
                float tang = ang + MathHelper.PiOver2;
                float rot = tang + (h2 - 0.5f) * 1.1f;
                float largo = 9f + 9f * h2;
                Vector2 dir = new(MathF.Cos(rot), MathF.Sin(rot));
                float latido = 0.75f + 0.25f * MathF.Sin(time * 2.4f + g * 2.1f);
                Barra(batch, pos - dir * (largo * 0.5f), pos + dir * (largo * 0.5f),
                    3.4f, Tint(ColorNaranja, 0.80f * brillo * latido));
                batch.Draw(glow, pos + dir * (largo * 0.5f), null,
                    Tint(ColorBlancoCorona, 0.55f * brillo * latido), 0f,
                    glow.Size() * 0.5f, new Vector2(9f) / glow.Size(), SpriteEffects.None, 0f);
            }
        }

        /// <summary>LA ONDA VIAJERA: un anillo fino que viaja del centro al
        /// borde en 8 ticks (fast-out) — cuando llega, cae el golpe.</summary>
        private static void DrawOnda(SpriteBatch batch, Vector2 drawPos, float R,
            float alpha, float waveT)
        {
            float p = OndaLib.Expansion(waveT);
            float waveR = MathF.Max(6f, R * p);
            float a = (1f - waveT * 0.30f) * alpha;

            Texture2D ring = VFXCore.Ring;
            Vector2 size = VFXCore.RingQuadSize(waveR);
            batch.Draw(ring, drawPos, null, Tint(ColorBlancoCorona, 0.50f * a), 0f,
                ring.Size() * 0.5f, size / ring.Size(), SpriteEffects.None, 0f);
            Vector2 size2 = VFXCore.RingQuadSize(waveR * 0.90f);
            batch.Draw(ring, drawPos, null, Tint(ColorOro, 0.28f * a), 0f,
                ring.Size() * 0.5f, size2 / ring.Size(), SpriteEffects.None, 0f);
        }

        /// <summary>LA MARCA DEL OJO: hasta 24 enemigos dentro del círculo
        /// llevan un ojo que los mira — esclerótica tenue + iris ámbar +
        /// PUPILA VERTICAL que se CONTRAE antes del tajo + 3 zarpazos.</summary>
        private void DrawOjos(SpriteBatch batch, float R, float alpha,
            float contraccion, float flare, int seed, float time)
        {
            int marcas = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist > R) continue;

                // El desvanecido del borde: (R−d)/70 (ficha 4).
                float borde = MathHelper.Clamp((R - dist) / 70f, 0f, 1f);
                Vector2 pos = npc.Center - Main.screenPosition;
                float semi = MathHelper.Clamp(MathF.Max(npc.width, npc.height) * 0.75f, 22f, 95f);

                DrawOjo(batch, pos, semi, borde * alpha, contraccion, flare, seed, npc.whoAmI, time);

                if (++marcas >= MarcaCap) break;
            }
        }

        /// <summary>UN OJO del decreto: la rendija se estrecha como el enfoque
        /// de un depredador; la rotura del sello sobre-expone en el beat.</summary>
        private static void DrawOjo(SpriteBatch batch, Vector2 pos, float semi, float alpha,
            float contraccion, float flare, int seed, int whoAmI, float time)
        {
            float a = alpha * (1f + 0.4f * flare);
            Texture2D glow = VFXCore.SoftGlow;

            // 1. LA ESCLERÓTICA: un velo blanco-corona tenue.
            Vector2 scl = new Vector2(semi * 1.9f) / glow.Size();
            batch.Draw(glow, pos, null, Tint(ColorBlancoCorona, 0.14f * a), 0f,
                glow.Size() * 0.5f, scl, SpriteEffects.None, 0f);

            // 2. EL IRIS: anillo ámbar (la semilla del NPC lo rota).
            Texture2D ring = VFXCore.Ring;
            float rotSeed = VFXCore.Hash01(seed, whoAmI, 909) * MathHelper.TwoPi;
            Vector2 iri = VFXCore.RingQuadSize(semi * 0.82f);
            batch.Draw(ring, pos, null, Tint(ColorOro, 0.55f * a), rotSeed,
                ring.Size() * 0.5f, iri / ring.Size(), SpriteEffects.None, 0f);

            // 3. LA PUPILA VERTICAL: se CONTRAE antes del golpe
            //    (ancho 0.15 → 0.045 — el enfoque del depredador).
            float ancho = MathHelper.Lerp(0.15f, 0.045f, contraccion);
            Texture2D orb = VFXCore.GlowOrb;
            Vector2 pup = new Vector2(MathF.Max(2.5f, semi * ancho * 2f), semi * 1.5f) / orb.Size();
            batch.Draw(orb, pos, null, Tint(ColorBlancoCorona, a), 0f,
                orb.Size() * 0.5f, pup, SpriteEffects.None, 0f);

            // 4. LOS 3 ZARPAZOS: cos(3θ) — puntas radiales rotando con la
            //    semilla del NPC, encendiéndose en el beat.
            float baseRot = VFXCore.Hash01(seed, whoAmI, 919) * MathHelper.TwoPi
                          + time * 0.18f + contraccion * 0.5f;
            for (int z = 0; z < 3; z++)
            {
                float za = baseRot + z * (MathHelper.TwoPi / 3f);
                Vector2 dir = new(MathF.Cos(za), MathF.Sin(za));
                Vector2 a0 = pos + dir * (semi * 0.95f);
                Vector2 a1 = pos + dir * (semi * (1.22f + 0.12f * contraccion));
                Barra(batch, a0, a1, 3.2f, Tint(ColorBrasa, 0.55f * a * (1f + 0.6f * flare)));
            }
        }

        // ==============================================================
        //  LAS UTILIDADES DEL DIBUJO
        // ==============================================================

        /// <summary>Una CÁPSULA suave entre dos puntos: SoftGlow estirado
        /// (los segmentos se solapan un poco para no dejar juntas).</summary>
        private static void Barra(SpriteBatch batch, Vector2 a, Vector2 b, float grosor, Color color)
        {
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 0.01f || grosor < 0.5f) return;

            Texture2D tex = VFXCore.SoftGlow;
            Vector2 size = new(len + grosor * 0.6f, grosor);
            batch.Draw(tex, (a + b) * 0.5f, null, color, MathF.Atan2(delta.Y, delta.X),
                tex.Size() * 0.5f, size / tex.Size(), SpriteEffects.None, 0f);
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
