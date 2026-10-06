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
    /// FAUCEDEVORADORPROJECTILE — v6.50.62 — LA ANIMACIÓN DE MUERTE.
    ///
    /// «En el momento en que el jefe llega a 1 punto de vida, su
    /// animación original de muerte SE DETIENE y no avanza; en su lugar
    /// se activa la nueva: la sombra sale y LO DEVORA.»
    ///
    /// v6.50.64 — dos añadidos: (a) LA BRUMA NEGRA en todos los estilos
    /// (el tentáculo gigante, la esfera y la página exhalan puﬀs vivos
    /// — la petición del usuario); (b) el ESTILO 4 — LA PÁGINA FINAL del
    /// arma 4: el festín de CINE (libro gigante que se abre sobre el
    /// jefe, zarpos que lo agarran, el plano negro que SUBE comiéndoselo
    /// con una boca de colmillos en el borde, almas al libro y al final
    /// el libro se PLEGRA y estalla).
    ///
    /// Lo spawnea FaucesGlobalNPC al interceptar la muerte (o al clavar
    /// la vida en 1 con el drain). El jefe queda POSADO (PreAI false —
    /// ni IA ni animación) mientras ESTE proyectil ejecuta el festín:
    ///
    ///   0-30   MANIFESTACIÓN — la sombra ERUPCIONA del portador
    ///          (estilo 1/3: el tentáculo GIGANTE se alza del charco;
    ///           estilo 2: la esfera se cierra alrededor del jefe;
    ///           estilo 4: LA PÁGINA se abre en el cielo como un libro).
    ///   30-60  ENVOLVER — el tentáculo SERPENTEA hasta el jefe y lo
    ///          enrosca / la esfera aprieta (los ojos se abren TODOS) /
    ///          los ZARPOS bajan de la página y agarran.
    ///   60-160 EL FESTÍN — las fauces MASTICAN (3 mordidas sonoras),
    ///          la OSCURIDAD crece sobre el jefe hasta taparlo, las
    ///          ALMAS vuelan al portador (estilo 4: al LIBRO — y del
    ///          libro al portador al cierre).
    ///   160-210 LA DISIPACIÓN — bruma negra y polvo: el jefe se
    ///          desintegra (estilo 4: el libro se sella con una onda
    ///          de choque). A los 210 el motor mata de verdad (el loot
    ///          cae DENTRO de la bruma: el festín lo digiere todo).
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (1/2/3/4) · ai[2] = tick.
    /// </summary>
    public class FauceDevoradorProjectile : ModProjectile
    {
        private NPC Jefe => Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.npc.Length
            ? null : Main.npc[(int)Projectile.ai[0]];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 320;
            Projectile.netImportant = true;
        }

        /// <summary>El portador: el jugador más cercano al jefe (SP: el usuario).</summary>
        private Player Portador(NPC jefe)
        {
            Player mejor = Main.player[Projectile.owner];
            float d = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;
                float dd = Vector2.DistanceSquared(p.Center, jefe.Center);
                if (dd < d) { d = dd; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC jefe = Jefe;

            // el jefe se esfumó (no debería: CheckActive lo clava) → fuera
            if (jefe == null || !jefe.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = jefe.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // === LOS SONIDOS DEL FESTÍN DE LA PÁGINA (estilo 4 — v6.50.64):
            // suenan en SP y en los clientes (el server no tiene oídos) ===
            if ((byte)Projectile.ai[1] == 4)
            {
                if (t == 28) Sonar(SoundID.Item74.WithPitchOffset(-0.5f).WithVolumeScale(0.7f), Projectile.Center);   // el libro se abre
                if (t == 62) Sonar(SoundID.Item122.WithPitchOffset(-0.35f).WithVolumeScale(0.85f), Projectile.Center); // los zarpos caen
                if (t == 96 || t == 136) Sonar(SoundID.NPCHit9.WithPitchOffset(-0.3f).WithVolumeScale(0.7f), Projectile.Center); // mastica
                if (t == 168) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.7f), Projectile.Center);  // se sella
            }

            // === POLVO Y AMBIENTE (el cliente lo genera local, determinista basta) ===
            // durante el festín: motas de sombra cayendo del jefe
            if (t > 60 && t < 205 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size.Length() * 0.4f,
                    DustID.Shadowflame, new Vector2(0, 1.2f), 128, default, 0.9f);
                d.noGravity = false;
                d.fadeIn = 0.4f;
            }
            // la disipación final: POLVO de sombra que estalla
            if (t >= 160 && t < 205)
            {
                int n = 3;
                for (int k = 0; k < n; k++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(jefe.Center,
                        DustID.Smoke,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 7f),
                        100, new Color(12, 6, 10), 1.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC jefe = Jefe;
            if (jefe == null || !jefe.active) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                byte estilo = (byte)Projectile.ai[1];
                if (estilo == 2) DrawEsfera(jefe);
                else if (estilo == 4) DrawPagina(jefe);
                else DrawTentaculo(jefe, estilo == 3);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  ESTILOS 1 y 3 — EL TENTÁCULO GIGANTE (sprite / 100% código)
        // ==================================================================

        private void DrawTentaculo(NPC jefe, bool procedural)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);
            Vector2 raiz = portador.MountedCenter + new Vector2(0, portador.height * 0.42f);

            // fases del festín
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 30f, 0f, 1f));
            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 100f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;

            // === EL CHARCO DEL PORTADOR (más grande que nunca) ===
            SombrasLib.Charco(raiz, 64f * manifiesta, 0.85f * vivo, tiempo, semilla);

            // === EL CUERPO GIGANTE: raíz → jefe, en dos tramos ===
            // (a media caza la cabeza VIAJA; ya envuelto, MUERDE en el sitio)
            float alcance = manifiesta * 0.35f + envuelve * 0.65f;
            Vector2 destino = Vector2.Lerp(raiz + new Vector2(0, -160f), jefe.Center, alcance);
            Vector2[] col = SombrasLib.Columna(raiz, destino, tiempo, semilla, 22, 0.20f);
            SombrasLib.Masa(col, 58f, 20f, 0.96f * vivo, semilla, tiempo);

            // === LA BRUMA DEL CUERPO GIGANTE (v6.50.64): el tentáculo del
            // festín también EXHALA bruma negra a lo largo de todo el cuerpo ===
            SombrasLib.BrumaColumna(col, 54f, 0.45f * vivo, tiempo, semilla, 12);

            // === LOS OJOS: TODOS abiertos, TODOS mirando al jefe ===
            SombrasLib.OjosDeMasa(col, jefe.Center, (0.4f + 0.6f * envuelve) * vivo, semilla, tiempo, 7);

            // === LA OSCURIDAD SOBRE EL JEFE (crece hasta TAPARLO) ===
            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
            float tapa = MathHelper.Clamp(envuelve * 0.5f + festin * 0.5f, 0f, 1f);
            if (tapa > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.92f * tapa * vivo),
                    new Vector2(radio * 2.15f * (0.55f + 0.45f * tapa), radio * 2.15f * (0.55f + 0.45f * tapa)), 0f, VFXCore.GlowOrb);
                VFXCore.FlushAlpha();
            }

            // === LA CABEZA-BOCA ===
            if (procedural)
            {
                // estilo 3: FAUCES 100% CÓDIGO — mastica el ciclo del festín
                Vector2 rumbo = (jefe.Center - col[col.Length - 4]).SafeNormalize(Vector2.UnitX);
                float apertura = t < 60f
                    ? 0.9f * manifiesta
                    : CicloMasticar(t);
                SombrasLib.Fauces(destino, rumbo, apertura * vivo, 120f, semilla);

                // v6.50.64 — EL ALIENTO: las fauces del festín respiran bruma
                SombrasLib.BrumaBoca(destino, rumbo, apertura * vivo, 0.5f * vivo, tiempo, semilla + 5, 5);
            }
            else
            {
                // estilo 1: EL SPRITE GIGANTE del usuario ×2 — LA PINZA
                // DOBLE (v6.50.64: «el gif doble en la punta, uno en cada
                // esquina, se comporta como dientes») a escala ~0.95/cabeza
                Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[
                    ModContent.ProjectileType<FaucesTentaculoProjectile>()].Value;
                int frame = FrameFestin(t);
                Rectangle src = new Rectangle((frame % 6) * (tex.Width / 6),
                    (frame / 6) * (tex.Height / 3), tex.Width / 6, tex.Height / 3);
                float escala = 0.95f * manifiesta;
                if (t > 60f) escala *= 1f + 0.05f * MathF.Sin(tiempo * 8f);     // la pinza APRIETA
                escala *= 0.85f + 0.15f * vivo;

                float rot = (jefe.Center - raiz).ToRotation();
                Vector2 eje = new(MathF.Cos(rot), MathF.Sin(rot));
                Vector2 perp = new(-eje.Y, eje.X);

                // la pinza del festín: plegada al brotar · ABIERTA al
                // envolver · MASTICA apretando durante el festín
                float abre = t < 30f ? 0.14f
                    : t < 60f ? 0.36f
                    : 0.09f + 0.06f * (0.5f + 0.5f * MathF.Sin(tiempo * 8f));

                // las bases: en la punta del cuerpo; separadas a lo ancho
                // del jefe mientras lo comen (muerden desde esquinas opuestas)
                float agarre = t > 60f ? MathHelper.Clamp(jefe.width * 0.18f, 10f, 30f) : 0f;
                Vector2 baseA = destino + perp * agarre;
                Vector2 baseB = destino - perp * agarre;

                Vector2 origen = new Vector2(src.Width * 0.42f, src.Height * 0.94f);
                Color luz = Lighting.GetColor(destino.ToTileCoordinates());
                Color tint = new Color(luz.R, luz.G, luz.B, 255) * vivo;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
                Main.EntitySpriteDraw(tex, baseA - Main.screenPosition, src, tint, rot + abre,
                    origen, escala, SpriteEffects.None, 0f);
                Main.EntitySpriteDraw(tex, baseB - Main.screenPosition, src, tint, rot - abre,
                    origen, escala, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // la soldadura de la horquilla gigante + EL ALIENTO de las
                // dos bocas (v6.50.64 — la bruma negra de las fauces)
                VFXCore.Begin();
                VFXCore.Quad(destino, SombrasLib.Alfa(SombrasLib.Negro, 0.95f * vivo),
                    new Vector2(120f, 120f), 0f, VFXCore.GlowOrb);
                VFXCore.FlushAlpha();
                Vector2 dirA = new(MathF.Cos(rot + abre), MathF.Sin(rot + abre));
                Vector2 dirB = new(MathF.Cos(rot - abre), MathF.Sin(rot - abre));
                SombrasLib.BrumaBoca(baseA + dirA * (src.Width * 0.42f * escala), dirA,
                    0.85f, 0.5f * vivo, tiempo, semilla + 3, 4);
                SombrasLib.BrumaBoca(baseB + dirB * (src.Width * 0.42f * escala), dirB,
                    0.85f, 0.5f * vivo, tiempo, semilla + 9, 4);
            }

            // === LAS ALMAS: la vida del jefe vuela al portador ===
            if (t > 55f)
            {
                VFXCore.Begin();
                int nAlmas = 6;
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.016f + k * (1f / nAlmas));
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 120f, -170f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a casa
                }
                VFXCore.FlushAdditive();
            }

            // === LA DISIPACIÓN: bruma y polvo final ===
            if (disipa > 0.02f)
            {
                SombrasLib.Bruma(jefe.Center, radio * 1.4f, 0.9f * disipa, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 30f, -18f * disipa));
            }
        }

        // ==================================================================
        //  ESTILO 2 — LA ESFERA (la boca cerrada que desintegra)
        // ==================================================================

        private void DrawEsfera(NPC jefe)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 29 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);

            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 26f, 0f, 1f));
            float festin = MathHelper.Clamp((t - 45f) / 115f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;

            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.68f, 95f, 280f) * manifiesta;

            // === EL DISCO NEGRO ===
            VFXCore.Begin();
            VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.97f * manifiesta * vivo),
                new Vector2(radio * 2.1f, radio * 2.1f), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            // === LA BRUMA DEL ANILLO (v6.50.64): la esfera del festín
            // también EXHALA bruma por toda la circunferencia ===
            Vector2[] anillo = new Vector2[14];
            for (int i = 0; i < 14; i++)
            {
                float aI = i / 13f * MathHelper.TwoPi;
                anillo[i] = jefe.Center + new Vector2(MathF.Cos(aI), MathF.Sin(aI) * 0.88f) * radio;
            }
            SombrasLib.BrumaColumna(anillo, radio * 0.48f, 0.4f * manifiesta * vivo, tiempo, semilla, 12);

            // === LOS OJOS: TODOS, MIRANDO AL PORTADOR ===
            VFXCore.Begin();
            for (int k = 0; k < 14; k++)
            {
                float f = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.43f + k * 0.618034f);
                float ang = f * MathHelper.TwoPi;
                float rr = radio * (0.2f + 0.66f * SombrasLib.Frac(f * 8.1f));
                Vector2 pos = jefe.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.85f) * rr;
                float abierto = MathHelper.Clamp(manifiesta * 14f * 0.75f - k * 0.75f, 0f, 1f);
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.9f + k * 1.7f);
                if (abierto <= 0.05f) continue;
                SombrasLib.Ojo(pos, 14f + 13f * SombrasLib.Frac(f * 6.2f), portador.Center - pos, abierto);
            }
            VFXCore.FlushAdditive();

            // === LA BOCA: muerde el ciclo y se lo COME entero ===
            Vector2 rumbo = new(MathF.Cos(tiempo * 0.45f), MathF.Sin(tiempo * 0.3f));
            rumbo = rumbo.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(rumbo);
            float apertura = t < 45f ? 0.9f : CicloMasticar(t);
            SombrasLib.Fauces(jefe.Center + rumbo * radio * 0.12f, rumbo, apertura * vivo,
                radio * 0.95f * manifiesta, semilla);

            // v6.50.64 — EL ALIENTO: la boca de la esfera respira bruma negra
            SombrasLib.BrumaBoca(jefe.Center + rumbo * radio * 0.12f, rumbo, apertura * vivo,
                0.5f * vivo, tiempo, semilla + 5, 5);

            // === EL INTERIOR: el jefe se APAGA (oscuro creciente dentro) ===
            if (festin > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.9f * festin * vivo),
                    new Vector2(radio * 1.6f * (0.5f + 0.5f * festin), radio * 1.6f * (0.5f + 0.5f * festin)));
                VFXCore.FlushAlpha();
            }

            // === LAS ALMAS vuelan al portador ===
            if (t > 50f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 6; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.015f + k * 0.1667f);
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.6f + tiempo), MathF.Sin(k * 2.2f)) * radio * 0.5f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(0, -150f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Alma(pos, 11f, 0.85f);
                }
                VFXCore.FlushAdditive();
            }

            // === LA DISIPACIÓN: bruma y polvo ===
            if (disipa > 0.02f)
                SombrasLib.Bruma(jefe.Center, radio * 1.35f, 0.9f * disipa, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.8f) * 26f, -20f * disipa));
        }

        // ==================================================================
        //  ESTILO 4 — LA PÁGINA FINAL (v6.50.64 — el arma 4): EL FESTÍN
        //  DE CINE. La muerte del jefe como un ritual de libro: LA SALA
        //  SE APAGA (vignette) → EL LIBRO GIGANTE se abre en el cielo
        //  sobre el jefe con su ojo colosal y el lomo rojo → LOS ZARPOS
        //  bajan de sus páginas y lo AGARRAN → EL PLANO NEGRO SUBE por
        //  el cuerpo comiéndoselo de abajo arriba (el borde es una boca
        //  horizontal de colmillos que mastica) → LAS ALMAS suben al
        //  libro (él digiere) → EL LIBRO SE SELLA con una onda de choque
        //  y estalla en bruma. A los 210 el motor mata de verdad.
        // ==================================================================

        private void DrawPagina(NPC jefe)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 37 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);

            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 28f, 0f, 1f));
            float agarre = MathHelper.Clamp((t - 28f) / 34f, 0f, 1f);
            float tragado = MathHelper.Clamp((t - 62f) / 100f, 0f, 1f);
            float cierre = MathHelper.Clamp((t - 168f) / 40f, 0f, 1f);
            float vivo = 1f - cierre;

            // === 1. LA SALA DE CINE: los bordes se apagan (la penumbra de Pride) ===
            SombrasLib.Vignette(0.55f * manifiesta * vivo);

            // === 2. EL LIBRO GIGANTE (v6.50.65 — EL SPRITE REAL del
            // Grimorio del Eterno, colosal, envuelto en su aura violeta;
            // antes eran dos rectángulos negros) suspendido sobre el jefe,
            // con el LOMO ROJO latiendo y su ojo colosal encima ===
            float tam = MathHelper.Clamp(jefe.Size.Length() * 0.55f, 130f, 300f);
            Vector2 pagina = jefe.Center + new Vector2(0f, -(tam * 1.35f + 120f));
            // el libro: cerrado al brotar → abierto → se PLEGRA al cierre
            float abreLibro = manifiesta * (1f - SombrasLib.DeGolpe(cierre));
            float bob = MathF.Sin(tiempo * 1.3f) * 10f * manifiesta;
            Vector2 posLibro = pagina + new Vector2(0f, -bob);

            // 2a. EL AURA violeta del libro gigante
            VFXCore.Begin();
            VFXCore.Quad(posLibro, SombrasLib.Alfa(SombrasLib.Violeta,
                (0.16f + 0.06f * MathF.Sin(tiempo * 2.2f)) * manifiesta * vivo),
                new Vector2(tam * 2.6f, tam * 3.0f));
            VFXCore.Quad(posLibro, SombrasLib.Alfa(SombrasLib.RojoGarganta, 0.10f * abreLibro * vivo),
                new Vector2(tam * 1.2f, tam * 1.8f));
            VFXCore.FlushAdditive();

            // 2b. EL LIBRO DE VERDAD: el sprite del Grimorio del Eterno,
            //     GIGANTE (crece con tam), ligeramente inclinado
            Texture2D texLibro;
            try
            {
                texLibro = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<Content.Weapons.GrimoireEternal>()].Value;
            }
            catch { texLibro = null; }
            if (texLibro != null)
            {
                float escalaLibro = (tam * 2.6f / texLibro.Width) * (0.65f + 0.35f * abreLibro) * vivo;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                        null, Main.Transform);
                    Main.spriteBatch.Draw(texLibro, posLibro - Main.screenPosition, null,
                        new Color(225, 218, 240, (byte)(255 * vivo)), MathF.Sin(tiempo * 0.7f) * 0.06f,
                        texLibro.Size() * 0.5f, escalaLibro, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // 2c. el lomo rojo: la garganta del libro late entreabierto
            VFXCore.Begin();
            VFXCore.Quad(posLibro + new Vector2(0f, tam * 0.1f),
                SombrasLib.Alfa(SombrasLib.Rojo, (0.28f + 0.14f * MathF.Sin(tiempo * 5f)) * abreLibro * vivo),
                new Vector2(tam * 0.5f * abreLibro, tam * 1.6f * abreLibro));
            VFXCore.FlushAdditive();

            // === 3. EL OJO DEL LIBRO: se abre DE GOLPE y MIRA AL JEFE mientras lo come ===
            VFXCore.Begin();
            SombrasLib.Ojo(posLibro + new Vector2(0f, -tam * 0.85f), tam * 0.42f, jefe.Center - pagina,
                MathHelper.Clamp(abreLibro * 1.4f, 0f, 1f) * vivo);
            VFXCore.FlushAdditive();

            // === 4. LOS ZARPOS: cuatro tentáculos bajan de la página y AGARRAN
            // al jefe (cada uno con su masa, sus ojos y SU BRUMA NEGRA) ===
            for (int k = 0; k < 4; k++)
            {
                Vector2 raizZ = posLibro + new Vector2((k - 1.5f) * tam * 0.42f * abreLibro, tam * 0.55f);
                Vector2 puntoAgarre = jefe.Center + new Vector2(
                    MathF.Cos(k * MathHelper.PiOver2 + 0.7f),
                    MathF.Sin(k * MathHelper.PiOver2 + 0.7f) * 0.7f) * jefe.Size.Length() * 0.34f;
                Vector2 punta = Vector2.Lerp(raizZ + new Vector2(0f, 34f), puntoAgarre, agarre);
                Vector2[] colZ = SombrasLib.Columna(raizZ, punta, tiempo, semilla + k * 7, 12, 0.2f);
                SombrasLib.Masa(colZ, 36f, 10f, 0.95f * vivo, semilla + k * 7, tiempo);
                SombrasLib.BrumaColumna(colZ, 30f, 0.42f * vivo, tiempo, semilla + k * 7, 6);
                SombrasLib.OjosDeMasa(colZ, jefe.Center, agarre * vivo, semilla + k * 7, tiempo, 2);
            }

            // === 5. EL TRAGADO: el plano negro que SUBE por el jefe — el
            // borde es una BOCA horizontal de colmillos que mastica comiendo
            // hacia arriba; lo tragado TE MIRA (ojos en el plano) ===
            if (tragado > 0.01f)
            {
                float baseY = jefe.Bottom.Y + 90f;
                float bordeY = MathHelper.Lerp(jefe.Bottom.Y + 70f, jefe.Top.Y - 100f, tragado);
                float h = bordeY - baseY;
                float w = MathF.Max(jefe.width * 1.5f, 320f);
                if (h > 6f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(new Vector2(jefe.Center.X, (baseY + bordeY) * 0.5f),
                        SombrasLib.Alfa(SombrasLib.Negro, 0.97f * vivo), new Vector2(w, h), 0f, VFXCore.Pixel);
                    VFXCore.FlushAlpha(VFXCore.Pixel);
                }

                // v6.50.65 — EL BORDE DENTADO: el plano ya no termina recto —
                // picos negros alternados SOBRE el borde (la boca del vacío
                // MORDIENDO hacia arriba) con su punta de hueso: se lee como
                // una GARGANTA que sube comiendo, no como un rectángulo.
                int nDientes = 9;
                for (int k = 0; k < nDientes; k++)
                {
                    float fx = (k + 0.5f) / nDientes - 0.5f;
                    float altoD = (16f + 22f * SombrasLib.Frac(SombrasLib.semille(semilla) * 0.77f + k * 0.618f))
                                  * (0.8f + 0.4f * MathF.Sin(tiempo * 9f + k * 1.9f));
                    Vector2 baseD = new(jefe.Center.X + fx * w, bordeY + 4f);
                    Vector2 puntaD = baseD + new Vector2(MathF.Sin(k * 2.1f) * 6f, -altoD);
                    SombrasLib.Garra(baseD, puntaD - baseD, altoD * 1.05f, 0.95f * vivo,
                        (k % 2 == 0 ? 1f : -1f) * 0.35f);
                }

                // la boca del borde (come SUBIENDO) + su aliento de bruma
                Vector2 borde = new(jefe.Center.X, bordeY);
                SombrasLib.Fauces(borde, -Vector2.UnitY, CicloMasticar(t) * vivo, w * 0.4f, semilla);
                SombrasLib.BrumaBoca(borde, -Vector2.UnitY, 0.9f, 0.5f * vivo, tiempo, semilla + 11, 5);
                SombrasLib.Bruma(borde, w * 0.5f, 0.45f * vivo, tiempo, semilla + 13);

                // ojos sobre el plano: lo ya tragado te mira
                VFXCore.Begin();
                for (int k = 0; k < 4; k++)
                {
                    float f = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.29f + k * 0.618034f);
                    Vector2 pos = new(jefe.Center.X + (f - 0.5f) * w * 0.8f, baseY + f * h);
                    SombrasLib.Ojo(pos, 15f + 10f * f, portador.Center - pos,
                        vivo * MathHelper.Clamp(tragado * 2f - f, 0f, 1f));
                }
                VFXCore.FlushAdditive();
            }

            // === 6. LAS ALMAS: primero SUBEN AL LIBRO (él digiere)… ===
            if (tragado > 0.05f && cierre < 0.4f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 5; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.016f + k * 0.2f);
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.5f + tiempo * 0.8f), MathF.Sin(k * 2.1f)) * 60f;
                    Vector2 b = pagina;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 90f, -40f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Alma(pos, 10f + 5f * MathF.Sin(tiempo * 5f + k), 0.8f * vivo);
                }
                VFXCore.FlushAdditive();
            }
            // === …y al final el libro PAGA al portador (la tinta digerida) ===
            if (cierre > 0.1f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 4; k++)
                {
                    float prog = SombrasLib.Frac((cierre - 0.1f) * 1.6f + k * 0.25f);
                    Vector2 a = pagina;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.1f) * 110f, 60f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);     // el alma ES un ojo que mira a casa
                }
                VFXCore.FlushAdditive();
            }

            // === 7. EL CIERRE: el libro se sella con su ONDA DE CHOQUE
            // y estalla en bruma negra (el loot cae DENTRO de la nube) ===
            if (cierre > 0.05f)
            {
                SombrasLib.OndaChoque(pagina, tam * (0.4f + cierre * 1.8f), 0.5f * (1f - cierre), false);
                SombrasLib.Bruma(jefe.Center, tam * 1.3f,
                    0.9f * MathF.Sin(cierre * MathF.PI), tiempo, semilla + 17,
                    new Vector2(0f, -26f * cierre));
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>El masticar del festín: abre… ¡CIERRA de golpe! …mastica… abre.</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - 60f) % 32f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 6f)); // ¡CIERRA!
            if (m < 26f) return 0.15f + 0.1f * MathF.Sin(m * 1.4f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 26f) / 6f);                            // reabre
        }

        /// <summary>El frame del sprite gigante según el momento del festín.</summary>
        private static int FrameFestin(float t)
        {
            if (t < 30f) return (int)MathHelper.Clamp(t / 30f * 5f, 0f, 5f);        // erupción (0-5)
            if (t < 60f) return 6 + (int)((t - 30f) / 30f * 4f);                     // el latigazo (6-10)
            if (t < 160f)                                                           // muerde (10↔12)
            {
                float m = (t - 60f) % 24f;
                if (m < 10f) return 10;
                if (m < 20f) return 11;
                return 12;
            }
            return 12 + (int)MathHelper.Clamp((t - 160f) / 45f * 4f, 0f, 4f);        // disipa (12-16)
        }
    }
}
