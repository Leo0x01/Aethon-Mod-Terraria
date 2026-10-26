using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigacion
{
    /// <summary>
    /// ESTELACOSMOSHALO — v6.54.0 — EL ANILLO SATURNO MORADO DEL JUGADOR.
    ///
    /// La técnica del StellarGeliath (Task 74-a) con texturas 100 %
    /// casa: el ANILLO INCLINADO (squash Y) con radio ONDULANTE, la
    /// banda EstelaEspacial.png con AUTO-INTERFERENCIA (la misma
    /// textura sampleada a 2 velocidades y MULTIPLICADA por nodo — el
    /// truco del shader StellarRing sin shader) y los CANALES COMO
    /// MÁSCARAS: G=la línea caliente LILA del borde, R=el cuerpo
    /// VIOLETA, B=el tinte AZUL-VIOLETA. La barra EstelaEspacialBrillo
    /// (la campana 1D) añade el resplandor exterior morado. Todo el
    /// anillo vive DETRÁS del cuerpo (el pase de proyectiles — el
    /// contrato halo de la casa: «la capa queda detrás del jugador»).
    /// </summary>
    public class EstelaCosmosHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int Nodos = 72;

        private static FajaCPU.Textura _banda, _brillo;
        private readonly Vector2[] _espina = new Vector2[Nodos];
        private readonly float[] _anchos = new float[Nodos];
        private readonly Color[] _colores = new Color[Nodos];

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;        // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<Players.InvestigacionCosmeticosPlayer>().EstelaCosmos)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(duenio.Center, 0.28f, 0.1f, 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                if (_banda == null)
                {
                    _banda = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/EstelaEspacial",
                        alpha: true);
                    _brillo = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/EstelaEspacialBrillo");
                }

                float t = Main.GlobalTimeWrappedHourly;
                float giro = t * 0.55f;
                float squashY = 0.26f;

                // === EL ANILLO (la vuelta completa — la mitad trasera y
                //     la delantera juntas: TODO el anillo vive detrás del
                //     cuerpo, el pase de proyectiles de la casa) ===
                for (int i = 0; i < Nodos; i++)
                {
                    float c = i / (float)Nodos;         // 0..1 la vuelta
                    float ang = c * MathHelper.TwoPi + giro;

                    // EL RADIO ONDULANTE (el anillo respira — el
                    // starnessSize del original).
                    float radio = 58f + MathF.Sin(t * 1.6f + c * MathHelper.TwoPi * 3f) * 7f;

                    // LA POSICIÓN (anillo inclinado — squash Y).
                    Vector2 mundo = Projectile.Center + new Vector2(
                        MathF.Cos(ang) * radio, MathF.Sin(ang) * radio * squashY - 14f);
                    _espina[i] = mundo - Main.screenPosition;
                    // EL ANCHO (las ondas líquidas del original).
                    _anchos[i] = (11f + MathF.Sin(t * 2.2f + c * MathHelper.TwoPi * 4f) * 4f);

                    // === LA AUTO-INTERFERENCIA (la misma banda a DOS
                    //     velocidades MULTIPLICADA — el shader StellarRing
                    //     resuelto por nodo) ===
                    float lenta = _banda.M(c + t * 0.10f, 0.35f);
                    float rapida = _banda.M(c + t * 0.17f, 0.35f);
                    float base_ = lenta * rapida * 1.8f;

                    // LOS CANALES COMO MÁSCARAS (la banda es RGBA: G=lila
                    // caliente, R=violeta, B=azul-violeta — alpha en A).
                    float g = _banda.M(c + t * 0.10f, 0.18f);
                    float r = _banda.M(c + t * 0.10f, 0.38f);
                    float b = _banda.M(c + t * 0.10f, 0.07f);

                    // EL COLOR de la casa: cuerpo violeta + línea lila
                    // caliente + borde azul-violeta + la barra glow.
                    float campana = _brillo.M(0.5f, 0.5f + MathF.Sin(c * MathHelper.Pi) * 0.5f);
                    int cr = (int)Math.Min(255, 150 * base_ + 190 * g * 1.2f + 60 * b + 40 * campana);
                    int cg = (int)Math.Min(255, 52 * base_ + 130 * g * 1.2f + 30 * b + 18 * campana);
                    int cb = (int)Math.Min(255, 220 * base_ + 255 * g * 1.2f + 190 * b + 120 * campana);
                    _colores[i] = new Color(cr, cg, cb, 255);
                }
                FajaCPU.Dibujar(_espina, _anchos, _colores);
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// TINTAVIVAHALO — v6.54.0 — EL CORDÓN LÍQUIDO QUE SIGUE AL JUGADOR.
    ///
    /// La técnica del cordón de Goozma (Task 74-b) con texturas 100 %
    /// casa: la cola sigue el RASTRO REAL del portador (un anillo de
    /// las últimas 36 posiciones, muestreadas cada 2 ticks), la espina
    /// se encrespa con la onda seno del original (±14 px decayendo) y
    /// CADA NODO resuelve el shader GoozmaCordMap en CPU: DOS CAPAS de
    /// TintaViva.png a escalas ×(3,1) y ×(2,1) y velocidades distintas
    /// MULTIPLICADAS (el encaje de agujeros), y el COLOR nace de la
    /// LUT MapaColorTinta.png (fila clara u oscura según el producto,
    /// con el canal R desplazando el tono — la variación a lo largo).
    /// </summary>
    public class TintaVivaHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int Nodos = 36;

        private static FajaCPU.Textura _tinta, _lut;
        private readonly Vector2[] _rastro = new Vector2[Nodos];
        private int _cabeza;
        private bool _lleno;
        private readonly Vector2[] _espina = new Vector2[Nodos];
        private readonly float[] _anchos = new float[Nodos];
        private readonly Color[] _colores = new Color[Nodos];

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<Players.InvestigacionCosmeticosPlayer>().TintaViva)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // EL RASTRO: cada 2 ticks la posición del dueño entra al anillo.
            if (Projectile.timeLeft % 2f == 0f || !_lleno && _cabeza == 0)
            {
                _rastro[_cabeza] = duenio.Center;
                _cabeza = (_cabeza + 1) % Nodos;
                if (_cabeza == 0) _lleno = true;
            }

            if (Main.netMode != NetmodeID.Server && _lleno)
                Lighting.AddLight(Projectile.Center, 0.2f, 0.06f, 0.4f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            if (!_lleno) return false;
            try
            {
                if (_tinta == null)
                {
                    _tinta = FajaCPU.Cargar(
                        "AethonMod/Content/Effects/Procedural/Investigacion/TintaViva",
                        alpha: true);
                    _lut = FajaCPU.CargarColor(
                        "AethonMod/Content/Effects/Procedural/Investigacion/MapaColorTinta");
                }

                float t = Main.GlobalTimeWrappedHourly;

                for (int i = 0; i < Nodos; i++)
                {
                    float p = i / (float)(Nodos - 1);
                    // EL RASTRO leído hacia atrás (0 = el dueño).
                    int idx = (_cabeza - 1 - i + Nodos * 2) % Nodos;
                    Vector2 pt = _rastro[idx];

                    // LA ONDA que se encrespa (±14·(1−p) — la del cordón).
                    float onda = MathF.Sin(t * 3f + p * 9f) * 14f * (1f - p * 0.7f);
                    Vector2 perpRastro = Vector2.UnitX;    // la onda lateral simple
                    if (i < Nodos - 1)
                    {
                        int idx2 = (_cabeza - 2 - i + Nodos * 2) % Nodos;
                        Vector2 d = _rastro[idx] - _rastro[idx2];
                        if (d != Vector2.Zero)
                        {
                            d.Normalize();
                            perpRastro = new Vector2(-d.Y, d.X);
                        }
                    }
                    _espina[i] = pt + perpRastro * onda - Main.screenPosition;

                    // EL ANCHO (18 px máx decayendo — el del jefe).
                    _anchos[i] = 20f * MathF.Pow(1f - p, 0.8f) + 2f;

                    // === EL SHADER GOOZMACORDMAP POR NODO: dos capas de
                    //     la MISMA tinta a escalas (3,1)/(2,1) y
                    //     velocidades 1×/3× MULTIPLICADAS ===
                    float c = p * 2.2f;   // a lo largo de la cola
                    float capaA = _tinta.M(c * 1.5f - t * 0.10f, p * 0.9f + 0.05f);
                    float capaB = _tinta.M(c * 1.0f - t * 0.30f, p * 0.9f + 0.05f);
                    float prodc = capaA * capaB * 2.4f;

                    // LA LUT: fila clara (V=0.2) u oscura (V=0.8) según
                    // el canal G del producto; el canal R desplaza el tono.
                    float u = p - capaA * 0.3f;
                    Color lut = prodc > 0.5f
                        ? _lut.MColor(u, 0.2f)
                        : _lut.MColor(u, 0.8f);

                    // EL REALCE (el goo se aclara donde el encaje aprieta).
                    float realce = Math.Min(1.6f, prodc);
                    _colores[i] = new Color(
                        (int)Math.Min(255, lut.R * realce + 30 * prodc),
                        (int)Math.Min(255, lut.G * realce + 12 * prodc),
                        (int)Math.Min(255, lut.B * realce + 55 * prodc), 255);
                }

                FajaCPU.Dibujar(_espina, _anchos, _colores);
            }
            catch { }
            return false;
        }
    }
}
