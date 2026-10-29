using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// TESTAMENTOAETHONPROJECTILE — v6.54.7 — ARMA 5 DE LAS CINCO: EL
    /// TESTAMENTO DE AETHON — LA CORONA DE LA BOLSA.
    ///
    /// LA TRANSCENDENCIA (la letra: «mejorar aún más los efectos
    /// visuales»): el libro ahora dicta con MÁS cine —
    ///
    ///   CAPA 1 — EL VELO DEL DICTADO (heredado, con el color de la sentencia).
    ///   CAPA 2 — EL LIBRO: el sprite real + los CUATRO HALOS APILADOS +
    ///   EL ARO ARCOÍRIS girando + LAS PESTAÑAS de estrella (nuevo — el
    ///   sándwich completo del foco del prisma, ahora en el libro).
    ///   CAPA 3 — LAS PÁGINAS: el polvo dorado + EL ALIENTO DE TINTA
    ///   (nuevo: un puff de LA LIBRERÍA DE BRUMAS violeta que respira
    ///   bajo el libro mientras dicta) + LA RÁFAGA DE PÁGINAS (nuevo):
    ///   al dictar, SEIS hojas de estrella salen volando del libro
    ///   girando y desvaneciéndose — la palabra se ESCONDE del libro.
    ///   CAPA 4 — EL ANILLO DE RUNAS + LOS ARCOS ENTRE RUNAS (nuevo):
    ///   cada tanto, un arco fino SALTA entre dos runas vecinas — el
    ///   anillo está CARGADO, la máquina piensa.
    ///   CAPA 5 — LA PALABRA: el flash + el Kick + LA ONDA DEL DICTADO
    ///   (nuevo): un aro que se expande desde el libro durante 12 t —
    ///   la palabra tiene frentes de onda.
    ///   CAPA 6 — EL SELLO (heredado).
    ///   CAPA 7 — EL VÍNCULO TEXTURIZADO (nuevo): el filamento del libro
    ///   a la sentencia ahora es una FAJA DE RUIDO FLUYENDO con la carne
    ///   corriendo del libro al arma + la vena blanca — la palabra viva.
    /// </summary>
    public class TestamentoAethonProjectile : ModProjectile
    {
        /// <summary>
        /// LOS NOMBRES y LOS COLORES de las cuatro sentencias (el índice =
        /// DefinitivasLib.SentenciaActual). Cada sentencia con SU acento.
        /// </summary>
        public static readonly string[] Sentencias =
        {
            "EL PRISMA PRIMORDIAL", "EL PARTIDOR DE MUNDOS", "EL RÉQUIEM ESTELAR", "LA IRA DEL CIELO",
        };

        private static readonly Color[] Acentos =
        {
            new(255, 248, 235),     // prisma — blanco cálido del espectro
            new(255, 214, 120),     // partidor — oro
            new(255, 158, 64),      // réquiem — ámbar del bronce
            new(176, 118, 255),     // ira — violeta de la tormenta
        };

        private const int TApertura = 18;
        private const int NPaginas = 6;     // LA RÁFAGA DE PÁGINAS (estado propio)

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;            // el daño lo hacen las sentencias
            Projectile.penetrate = -1;
            Projectile.timeLeft = 64;
            Projectile.netImportant = true;
        }

        private int _hijo = -1;         // el whoAmI de la sentencia invocada (para EL VÍNCULO)

        // LA RÁFAGA DE PÁGINAS (visual, solo cliente)
        private readonly Vector2[] _paginaPos = new Vector2[NPaginas];
        private readonly Vector2[] _paginaVel = new Vector2[NPaginas];
        private readonly float[] _paginaRot = new float[NPaginas];
        private readonly bool[] _paginaViva = new bool[NPaginas];
        private int _tPalabra = -1;     // LA ONDA DEL DICTADO (el aro que se expande)
        private int _tArcoRunas;        // LOS ARCOS ENTRE RUNAS

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            // EL LIBRO cabalga sobre el portador
            Projectile.Center = dueño.MountedCenter + new Vector2(0f, -108f);

            if (t == 2)
            {
                DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.4f).WithVolumeScale(0.8f), Projectile.Center);
                if (dueño.whoAmI == Main.myPlayer)
                {
                    Color acento = Acentos[DefinitivasLib.SentenciaActual % Acentos.Length];
                    Main.NewText($"[c/FFD678:El Testamento dicta:] [c/{acento.R:X2}{acento.G:X2}{acento.B:X2}:{Sentencias[DefinitivasLib.SentenciaActual]}]");
                }
            }

            // ==================== CAPA 4b — LOS ARCOS ENTRE RUNAS ====================
            // (cada ~11 t, un arco fino salta entre dos runas vecinas: el
            // anillo está CARGADO — la máquina piensa antes de hablar)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t > 4 && ++_tArcoRunas > 11)
            {
                _tArcoRunas = 0;
                int r = Main.rand.Next(7);
                float a0 = tiempo * 1.1f + r * MathHelper.TwoPi / 7f;
                float a1 = tiempo * 1.1f + (r + 1) * MathHelper.TwoPi / 7f;
                Vector2 bob0 = new(0f, -MathF.Sin(tiempo * 1.6f) * 6f);
                Vector2 p0 = Projectile.Center + bob0 + new Vector2(MathF.Cos(a0) * 120f, MathF.Sin(a0) * 44f - 10f);
                Vector2 p1 = Projectile.Center + bob0 + new Vector2(MathF.Cos(a1) * 120f, MathF.Sin(a1) * 44f - 10f);
                RayoSistema.Lanzar(RayoParams.Arco(p0, p1),
                    (uint)Main.rand.Next(1, int.MaxValue), p1, p0 - p1,
                    DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.8f), DefinitivasLib.BlancoCalido,
                    3.5f, 6, AnimRayo.Arco, false);
            }

            // ==================== LA DICTADURA DE LA SENTENCIA ====================
            if (t == TApertura)
            {
                int sentencia = DefinitivasLib.SentenciaActual;
                int daño = (int)(Projectile.damage * 1.3f);
                // v6.56.1 — el servidor no tiene ratón: usa el rumbo del
                // dueño que llega sincronizado (dirección + elevación).
                Vector2 rumbo = (Main.netMode == NetmodeID.Server)
                    ? new Vector2(dueño.direction, -0.35f)
                    : (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
                var src = Projectile.GetSource_FromAI();

                // ==================== CAPA 5 — LA PALABRA ====================
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Flash(DefinitivasLib.Alfa(Acentos[sentencia], 0.9f), 0.20f, 9, Projectile.Center);
                    OndaLib.Kick(6, 10);
                    _tPalabra = 0;

                    // LA RÁFAGA DE PÁGINAS: seis hojas de estrella salen volando
                    for (int i = 0; i < NPaginas; i++)
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        _paginaPos[i] = Projectile.Center + new Vector2(0f, -MathF.Sin(tiempo * 1.6f) * 6f);
                        _paginaVel[i] = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f - 0.8f) * Main.rand.NextFloat(5f, 10f);
                        _paginaRot[i] = Main.rand.NextFloat(MathHelper.TwoPi);
                        _paginaViva[i] = true;
                    }
                }

                // v6.56.1 — EL GATE DE RED: la sentencia la spawnea la
                // AUTORIDAD (server/SP). Antes el switch corría también en
                // el cliente dueño y la sentencia nacía DOS veces en MP.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    switch (sentencia)
                    {
                        case 0:     // EL PRISMA PRIMORDIAL (sentencia: vive su ciclo acotado)
                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                                ModContent.ProjectileType<PrismaPrimordialProjectile>(), daño, Projectile.knockBack,
                                Projectile.owner, 1f, 0f, 0f);
                            break;

                        case 1:     // EL PARTIDOR DE MUNDOS
                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, rumbo * 10f,
                                ModContent.ProjectileType<PartidorDeMundosProjectile>(), daño, Projectile.knockBack,
                                Projectile.owner, 0f, 0f, 0f);
                            break;

                        case 2:     // EL RÉQUIEM ESTELAR
                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                                ModContent.ProjectileType<RequiemEstelarProjectile>(), daño, Projectile.knockBack,
                                Projectile.owner, 0f, 0f, 0f);
                            break;

                        default:    // LA IRA DEL CIELO
                            _hijo = Projectile.NewProjectile(src, dueño.MountedCenter, Vector2.Zero,
                                ModContent.ProjectileType<IraDelCieloProjectile>(), daño, Projectile.knockBack,
                                Projectile.owner, 0f, 0f, 0f);
                            break;
                    }
                }
                else if (Main.myPlayer == Projectile.owner)
                {
                    // el cliente dueño NO spawnea (el servidor ya lo hizo):
                    // BUSCA al hijo para el vínculo visual del libro.
                    _hijo = BuscarHijoDeSentencia();
                }

                // LA VUELTA (la próxima sentencia queda servida)
                DefinitivasLib.SentenciaActual = (DefinitivasLib.SentenciaActual + 1) % Sentencias.Length;
            }

            // ==================== LA VIDA DE LAS PÁGINAS (vuelan, giran, mueren) ====================
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (_tPalabra >= 0) { _tPalabra++; if (_tPalabra > 12) _tPalabra = -1; }
                for (int i = 0; i < NPaginas; i++)
                {
                    if (!_paginaViva[i]) continue;
                    _paginaPos[i] += _paginaVel[i];
                    _paginaVel[i] *= 0.94f;
                    _paginaVel[i].Y += 0.12f;         // la gravedad suave de la hoja
                    _paginaRot[i] += 0.28f;
                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(_paginaPos[i], DustID.GoldFlame,
                            -_paginaVel[i] * 0.15f, 0, DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.7f);
                        d.noGravity = true;
                    }
                    if ((_paginaPos[i] - Projectile.Center).LengthSquared() > 700f * 700f)
                        _paginaViva[i] = false;
                }
            }

            // ==================== CAPA 3 — LAS PÁGINAS QUE VUELAN (el polvo) ====================
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t > TApertura - 8 && t < TApertura + 30 && Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-20f, 30f));
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f)), 0,
                    DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.8f);
                d.noGravity = true;
            }
        }

        /// <summary>v6.56.1 — En MP el hijo lo spawnea el servidor; el
        /// cliente dueño lo BUSCA para el vínculo visual (el filamento
        /// libro→arma de la CAPA 7).</summary>
        private int BuscarHijoDeSentencia()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.owner != Projectile.owner) continue;
                if (p.type == ModContent.ProjectileType<PrismaPrimordialProjectile>() ||
                    p.type == ModContent.ProjectileType<PartidorDeMundosProjectile>() ||
                    p.type == ModContent.ProjectileType<RequiemEstelarProjectile>() ||
                    p.type == ModContent.ProjectileType<IraDelCieloProjectile>())
                    return i;
            }
            return -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float abre = DefinitivasLib.DeGolpe(MathHelper.Clamp(t / (float)TApertura, 0f, 1f));
            float cierra = MathHelper.Clamp((Projectile.timeLeft - 14) / 20f, 0f, 1f);
            float vida = abre * cierra;
            if (vida <= 0.01f) return false;

            int sentencia = DefinitivasLib.SentenciaActual;
            Color acento = Acentos[sentencia];

            float bob = MathF.Sin(tiempo * 1.6f) * 6f * vida;
            Vector2 posLibro = Projectile.Center + new Vector2(0f, -bob);

            // ==================== CAPA 1 — EL VELO DEL DICTADO ====================
            {
                float w = Main.screenWidth, h = Main.screenHeight;
                Vector2 o = Main.screenPosition;
                float pulso = 0.9f + 0.1f * MathF.Sin(tiempo * 2.3f);
                VFXCore.Begin();
                Color denso = DefinitivasLib.Alfa(new Color(34, 22, 10), (0.14f + 0.10f * vida) * 0.62f * pulso);
                VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
                // EL TINTE DEL ACENTO (la sala sabe qué sentencia toca)
                VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 0.5f), DefinitivasLib.Alfa(acento, 0.045f * vida * pulso),
                    new Vector2(w * 1.6f, h * 1.6f), 0f, VFXCore.SoftGlow);
                VFXCore.FlushAlpha();
            }

            // ==================== CAPA 3b — EL ALIENTO DE TINTA ====================
            // (LA LIBRERÍA DE BRUMAS: el puff violeta que respira bajo el
            // libro mientras dicta — la tinta tiene vapor)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginGlow();
                    BrumaFX.Puff(posLibro - Main.screenPosition + new Vector2(0f, 30f), 70f + 14f * MathF.Sin(tiempo * 1.3f),
                        new Color(150, 96, 220), 501, tiempo * 0.5f, 0.16f * vida);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // ==================== CAPA 2 — EL LIBRO CON SU SÁNDWICH COMPLETO ====================
            VFXCore.Begin();
            Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.1f);
            Color grad2 = DefinitivasLib.GradienteJuicio(tiempo * 0.1f + 0.33f);
            // LOS CUATRO HALOS APILADOS (14× · 5× · 4× · 2×)
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad, 0.10f * vida), new Vector2(1200f, 1200f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad2, 0.20f * vida), new Vector2(520f, 520f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(grad, 0.38f * vida), new Vector2(360f, 360f) * vida);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.45f * vida), new Vector2(210f, 210f) * vida);
            // EL ARO ARCOÍRIS + LAS PESTAÑAS (el sándwich del foco del prisma, en el libro)
            if (VFXCore.Arcoiris != null)
                VFXCore.Quad(posLibro, DefinitivasLib.Alfa(Color.White, 0.4f * vida),
                    new Vector2(280f, 280f) * vida, tiempo * 0.2f, VFXCore.Arcoiris);
            if (DefinitivasLib.TexStar != null)
                VFXCore.Quad(posLibro, DefinitivasLib.Alfa(Color.White, 0.4f * vida),
                    new Vector2(300f, 300f) * vida, tiempo * 0.35f, DefinitivasLib.TexStar);
            VFXCore.FlushAdditive();

            // EL SPRITE REAL del Grimorio del Eterno
            Texture2D tex = null;
            try
            {
                tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<Content.Weapons.GrimoireEternal>()].Value;
            }
            catch { }
            if (tex != null)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                        null, Main.Transform);
                    Main.spriteBatch.Draw(tex, posLibro - Main.screenPosition, null,
                        new Color(255, 246, 224, (byte)(255 * vida)), MathF.Sin(tiempo * 0.9f) * 0.09f,
                        tex.Size() * 0.5f, 3.0f * vida, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            VFXCore.Begin();

            // ==================== CAPA 4 — EL ANILLO DE RUNAS ====================
            // (siete sellos orbitando el libro en elipse, latiendo — y los
            // ARCOS entre runas ya viven en el AI, saltando entre estas
            // mismas posiciones)
            for (int r = 0; r < 7; r++)
            {
                float angR = tiempo * 1.1f + r * MathHelper.TwoPi / 7f;
                Vector2 posR = posLibro + new Vector2(MathF.Cos(angR) * 120f, MathF.Sin(angR) * 44f - 10f);
                float lateR = 0.6f + 0.4f * MathF.Sin(tiempo * 4f + r * 1.3f);
                Color cR = DefinitivasLib.GradienteJuicio(r / 7f + tiempo * 0.12f);
                VFXCore.Quad(posR, DefinitivasLib.Alfa(cR, 0.35f * vida * lateR), new Vector2(46f, 46f) * lateR);
                if (DefinitivasLib.TexStar != null)
                    VFXCore.Quad(posR, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.8f * vida * lateR),
                        new Vector2(24f, 24f) * lateR, tiempo * 2f + r, DefinitivasLib.TexStar);
            }

            // ==================== CAPA 6 — EL SELLO DE LA SENTENCIA ====================
            float late = 0.35f + 0.2f * MathF.Sin(tiempo * 6f);
            VFXCore.Quad(posLibro, DefinitivasLib.Alfa(DefinitivasLib.Oro, late * vida), new Vector2(64f, 110f) * vida);
            if (DefinitivasLib.TexStar != null)
            {
                // el halo del sello con EL ACENTO de la sentencia que toca
                VFXCore.Quad(posLibro + new Vector2(0f, -70f), DefinitivasLib.Alfa(acento, 0.4f * vida),
                    new Vector2(110f, 110f) * vida);
                VFXCore.Quad(posLibro + new Vector2(0f, -70f), DefinitivasLib.Alfa(Color.White, 0.9f * vida),
                    new Vector2(64f, 64f) * vida, tiempo * 1.4f, DefinitivasLib.TexStar);
            }

            // ==================== CAPA 5b — LA ONDA DEL DICTADO ====================
            // (el aro que se EXPANDE desde el libro durante 12 t tras dictar —
            // la palabra tiene frentes de onda)
            if (_tPalabra >= 0)
            {
                float q = _tPalabra / 12f;
                if (VFXCore.Ring != null)
                    VFXCore.Quad(posLibro, DefinitivasLib.Alfa(acento, 0.55f * (1f - q)),
                        new Vector2(140f, 140f) + new Vector2(560f, 560f) * DefinitivasLib.DeGolpe(q),
                        q * 0.4f, VFXCore.Ring);
            }

            // ==================== CAPA 3c — LA RÁFAGA DE PÁGINAS ====================
            // (las hojas de estrella que salen volando del libro al dictar)
            if (DefinitivasLib.TexStar != null)
            {
                for (int i = 0; i < NPaginas; i++)
                {
                    if (!_paginaViva[i]) continue;
                    float alfaP = MathHelper.Clamp(1f - (_paginaPos[i] - Projectile.Center).Length() / 600f, 0f, 1f);
                    VFXCore.Quad(_paginaPos[i], DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * alfaP),
                        new Vector2(30f, 38f), _paginaRot[i], DefinitivasLib.TexStar);
                }
            }
            VFXCore.FlushAdditive();

            // ==================== CAPA 7 — EL VÍNCULO TEXTURIZADO ====================
            // (la palabra hecha arma: la FAJA DE RUIDO con la carne CORRIENDO
            // del libro a la sentencia + la vena blanca — mientras viva)
            // v6.56.1 — en MP el hijo puede llegar 1-2 ticks tarde:
            // ventana de reintento de la búsqueda para el vínculo.
            if (_hijo < 0 && Projectile.ai[2] > TApertura && Projectile.ai[2] < TApertura + 90
                && Main.myPlayer == Projectile.owner)
            {
                _hijo = BuscarHijoDeSentencia();
            }
            if (_hijo >= 0 && _hijo < Main.maxProjectiles)
            {
                Projectile hijo = Main.projectile[_hijo];
                if (hijo != null && hijo.active)
                {
                    const int NV = 14;
                    var ptsV = new Vector2[NV];
                    for (int i = 0; i < NV; i++)
                    {
                        float q = i / (float)(NV - 1);
                        Vector2 base_ = Vector2.Lerp(posLibro, hijo.Center, q);
                        float ondeo = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 5f + q * 12f) * 26f;
                        ptsV[i] = base_ + new Vector2(-ondeo * 0.3f, ondeo) - Main.screenPosition;
                    }
                    Texture2D carne = DefinitivasLib.TexRuido;
                    RayoStrip.AbrirLote();
                    try
                    {
                        if (carne != null)
                        {
                            // EL AURA — la carne del vínculo fluyendo libro→arma
                            var semisV = new float[NV];
                            for (int i = 0; i < NV; i++)
                                semisV[i] = 16f * vida * (0.6f + 0.4f * MathF.Sin(i / (float)(NV - 1) * MathHelper.Pi));
                            RayoStrip.FajaTexturadaEnLote(ptsV, semisV,
                                DefinitivasLib.Alfa(acento, 0.35f * vida), carne,
                                200f, 1.5f, tiempo, 0f, 1f);
                        }
                        // LA VENA blanca (heredada)
                        RayoStrip.FilamentoEnLote(ptsV, 10f * vida, DefinitivasLib.BlancoCalido, Color.White,
                            0.9f * vida, StormTaper.Center, 78, 0, false, true, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
                else _hijo = -1;
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.RestaurarLoteVanilla();
            return false;
        }
    }
}
