using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.77 — LAS ARMAS DEL CRECIMIENTO (1 · 7) — los proyectiles.
    //      La primera mitad del arsenal de las 14 ideas (la letra
    //      completa en CrecimientoArsenal.cs).
    //
    //  v6.79 — EL REDISEÑO VFX TOTAL (la letra del playtest: «todas se ven
    //      horribles en muchos sentidos... solo tomaste una textura y la
    //      lanzaste, eso no son armas creativas. Un arma creativa usa las
    //      librerías a su máxima expresión»). El gameplay de las 14 queda
    //      BYTE-INTACTO (la siembra, la fusión, los rebotes, la cuenta de
    //      páginas, el hambre — todo lo jugado); lo que se viste es la
    //      PIEL: cada arma usa ahora 4-6 librerías de la casa combinadas
    //      — estelas de cinta (EstelaLib), prismas vivos (ColorPrisma),
    //      fuego por rampas de temperatura (PyraLib), luz que emana
    //      (LumenLib), escritura rúnica (SigiloLib), los códigos vivos
    //      (CodigosLib), ondas de impacto (OndaLib), la tinta (TelaLib),
    //      los tajos (TajoLib) y la lente gravitacional (GravLens).
    //      Cero Main.rand en render (determinismo de la casa: whoAmI y
    //      GameUpdateCount), techo por presupuesto (VFXCore.Presupuesto).
    // ======================================================================

    // ======================================================================
    //  EL ESTALLIDO COMÚN — el AoE que comparten las 14 (v6.79: REESCRITO)
    //      Antes: dos Quads y un fade. Ahora: el frente ROTO de OndaLib
    //      (12-16 segmentos, CROMÁTICO), la flor de fuego de PyraLib
    //      (cuando el invocador trae rampa), el bloom + la cruz de
    //      LumenLib, el anillo fantasma de OrbitaLib, el destello final
    //      rotando — y el TINTE DEL INVOCADOR: cada arma tiñe su estallido
    //      (TinteProximo/RampaProxima justo antes de dispararlo).
    // ======================================================================
    public class CrecimientoEstallidoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/NovaBurst";

        public override void SetDefaults()
        {
            Projectile.width = 120; Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;   // v6.79: vida 26 t — pega como el AoE viejo (0 y 14)
        }

        /// <summary>La vida del estallido (la progresión que el frente ROTO necesita).</summary>
        private const int Vida = 26;

        // === EL TINTE DEL INVOCADOR (v6.79): quien dispara el AoE lo tiñe
        //     — se setean JUSTO antes del NewProjectile (mismo hilo, sin
        //     carrera) y el estallido los consume en su primer tick. ===
        internal static Color TinteProximo = new Color(255, 240, 215);
        /// <summary>Rampa de PyraLib opcional (SoloFire/verde...): null = estallido de LUZ (sin fuego).</summary>
        internal static Color[] RampaProxima;

        private Color _tinte;
        private Color[] _rampa;
        private bool _tomado;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            if (!_tomado)
            {
                _tomado = true;
                _tinte = TinteProximo;
                _rampa = RampaProxima;
                TinteProximo = new Color(255, 240, 215);
                RampaProxima = null;
            }
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.5f, 0.4f, 0.7f) * _tinte.ToVector3().Length() * 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float prog = 1f - Projectile.timeLeft / (float)Vida;        // 0→1
            float golpe = DefinitivasLib.DeGolpe(prog);                  // la aceleración brusca
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float esc = MathF.Max(0.7f, Projectile.scale);
            lightColor = Color.Lerp(lightColor, _tinte, 0.7f);

            // (1) LA FLOR DE FUEGO (solo si el invocador trae rampa — la
            //     vela/estrella/espora traen PyraPalettes; los de luz no).
            if (_rampa != null)
            {
                VFXCore.AbrirAdditive();
                PyraLib.Estallido(Main.spriteBatch, pos, 110f * esc, golpe, _rampa,
                    Projectile.whoAmI * 13 + 5, time, 1f + 0.4f * esc);
                VFXCore.RestaurarLoteVanilla();
            }

            // (2) EL FRENTE ROTO + EL BLOOM + LA CRUZ + EL ANILLO FANTASMA
            //     + EL DESTELLO FINAL — un solo lote aditivo.
            VFXCore.AbrirAdditive();
            OndaLib.Shock(Main.spriteBatch, pos, golpe,
                150f * esc, _tinte, 1f, Projectile.whoAmI * 7 + 1,
                9f, OndaFalloff.Quadratic, chromatic: true);
            LumenLib.BloomPulse(Main.spriteBatch, pos, 60f * esc, _tinte,
                0.9f * (1f - prog * 0.7f), time, 3.2f);
            LumenLib.Flare(Main.spriteBatch, pos, 70f * esc * (1f - prog * 0.5f),
                _tinte, 1.1f * (1f - prog * 0.6f), time * 0.9f);
            OrbitaLib.EcosAnillo(pos, 60f * esc, time,
                OrbitaLib.Distorsion(time), _tinte, _tinte * 0.5f);
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, _tinte * (0.5f * (1f - prog)),
                new Vector2(150f, 150f) * esc * (1f - prog * 0.4f),
                time * 1.7f, VFXCore.DestelloFinal);
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  1 · LA SEMILLA RÚNICA — la que germina
    //      E1: vuela y germina · E2: el pulso más ancho · E3: la runa
    //      vive más · E4: la detonación siembra dos hijas.
    //      v6.79 EL VESTIDO: la semilla vuela con ESTELA de cinta (Ribbon
    //      Comet dorada); la runa germinada viste su ANILLO RÚNICO de
    //      verdad (SigiloLib — 6 glifos solares orbitando), el pulso
    //      golpea con su onda visible (OndaLib.Pulse + EcosAnillo), la
    //      madurez prende su PERLA CARDINAL (el «estoy lista») y la
    //      detonación es CINE: hitstop + onda cromática + micro-lente.
    // ======================================================================
    public class SemillaRunicaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 486;
            Projectile.tileCollide = false;   // la semilla VUELA al punto (no rebota)
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private bool EsRuna => Projectile.ai[1] == 1f;
        /// <summary>¿La runa está MADURA (lista para detonar al clic)?</summary>
        public bool EsRunaMadura => EsRuna && _edad >= 60f;

        private float _edad;
        private int _pulsoT;    // v6.79: la vida restante del pulso visible

        public override void AI()
        {
            _edad++;
            if (!EsRuna)
            {
                // v6.79 — LA ESTELA DEL VUELO (el rastro de la siembra).
                EstelaLib.Track(Projectile.whoAmI, 10).Push(Projectile.Center);

                // LA SEMILLA VUELA: 6 ticks y GERMINA en el sitio.
                if (_edad >= 6f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = Etapa >= 3 ? 720 : 480;
                }
                return;
            }

            // === LA RUNA GERMINA (la letra: «la runa GERMINA: crece de
            //     glifo pequeño a sello completo en ~1 s dañando en área
            //     creciente»): el anillo de germinación CRECE con la edad
            //     — el área del pulso la pone la etapa (E2: más ancha). ===
            Projectile.rotation += 0.045f;
            Projectile.scale = 0.55f + MathHelper.Clamp(_edad / 60f, 0f, 1f) * 0.85f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.32f, 0.2f));

            // EL PULSO: cada 16 t el anillo golpea (dust de la corona).
            if (_edad % 16f < 1f && _edad > 20f)
            {
                _pulsoT = 8;   // v6.79: la onda del pulso VIVE 8 ticks
                for (int i = 0; i < 5; i++)
                {
                    float ang = MathHelper.TwoPi * i / 5f + Projectile.rotation;
                    Dust d = Dust.NewDustPerfect(Projectile.Center +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (14f + _edad * 0.35f),
                        DustID.Enchanted_Gold, Vector2.Zero, 180, new Color(230, 218, 255), 0.8f);
                    d.noGravity = true;
                }
            }
            if (_pulsoT > 0) _pulsoT--;

            // AL EXPIRAR: la runa madura detona sola (la esfera de luz).
            if (Projectile.timeLeft <= 2)
                DetonarMadura();
        }

        /// <summary>LA DETONACIÓN DE LA MADURA (la letra: «clicar sobre
        /// una runa madura la detona con la esfera de luz»): la nova del
        /// nacimiento — v6.79: el estallido dorado + EL CINE (hitstop, la
        /// onda cromática, la lente que curva la luz de la esfera).</summary>
        public void DetonarMadura()
        {
            if (!Projectile.active) return;
            // EL TINTE DEL ESTALLIDO (el dorado-violeta de la casa).
            CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 232, 170);
            // EL CINE: la nova se merece su medio segundo de gloria.
            ImpactoLib.GolpeDeCine(Projectile.Center, 1.45f, 5,
                new Color(255, 236, 190), suave: false, destello: true);
            GravLens.Registrar(Projectile.Center, 150f, 0.30f, 0.40f);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!EsRuna) return;   // la semilla en vuelo muere en silencio
            // EL ESTALLIDO (la esfera de luz): AoE + las hijas de la E4.
            int radioDano = (int)(Projectile.damage * 1.5f);
            if (CrecimientoEstallidoProjectile.TinteProximo == new Color(255, 240, 215) &&
                RampaProximaNula())
                CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 232, 170);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                radioDano, Projectile.knockBack * 1.5f, Projectile.owner);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 1f)),
                    160, new Color(240, 230, 255), 1.1f);
                d.noGravity = true;
            }
            // E4: LAS HIJAS — la detención siembra dos semillas nuevas.
            if (Etapa >= 4)
            {
                for (int i = 0; i < 2; i++)
                {
                    float ang = MathHelper.Pi + (i == 0 ? -0.7f : 0.7f);
                    Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 14f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel, Type,
                        Projectile.damage, Projectile.knockBack, Projectile.owner,
                        Nivel, 0f, 0f);
                }
            }
        }

        private static bool RampaProximaNula() => CrecimientoEstallidoProjectile.RampaProxima == null;

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            if (!EsRuna)
            {
                // === LA SEMILLA EN VUELO: la estela dorada de la siembra
                //     (Ribbon Comet — el rastro que muere al germinar) +
                //     el bloom de la chispa. ===
                lightColor = Color.Lerp(lightColor, new Color(255, 250, 220), 0.6f);
                Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 10).Points();
                VFXCore.AbrirAdditive();
                if (camino.Length >= 2)
                {
                    for (int i = 0; i < camino.Length; i++)
                        camino[i] -= Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 9f,
                        EstelaProfile.Comet, new Color(255, 226, 150), 0.55f,
                        Projectile.whoAmI * 31 + 7, time, head: true, taper: 60f);
                }
                LumenLib.Bloom(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                    26f, new Color(255, 238, 190), 0.7f);
                VFXCore.RestaurarLoteVanilla();
                return true;
            }

            // === LA RUNA GERMINADA: el ANILLO RÚNICO DE VERDAD (6 glifos
            //     solares orbitando su elipse — SigiloLib al buffer), el
            //     pulso con su onda visible, la perla cardinal de la
            //     madurez y el bloom que respira con la edad. ===
            float madura = MathHelper.Clamp((_edad - 40f) / 20f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 246, 225), 0.45f + 0.4f * madura);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            float rEl = 20f + 26f * Projectile.scale;
            float respirar = OrbitaLib.Distorsion(time);

            // (1) LA ONDA DEL PULSO (la vida de 8 ticks — su ease-out).
            VFXCore.AbrirAdditive();
            if (_pulsoT > 0)
            {
                float pPulso = 1f - _pulsoT / 8f;
                OndaLib.Pulse(Main.spriteBatch, pos, DefinitivasLib.DeGolpe(pPulso),
                    70f + 50f * (Etapa >= 2 ? 1.3f : 1f), new Color(255, 232, 170),
                    0.8f, Projectile.whoAmI * 7 + 3);
                OrbitaLib.EcosAnillo(pos, (50f + 30f * pPulso) * respirar, time,
                    OrbitaLib.Distorsion(time), new Color(255, 232, 170), new Color(196, 160, 255));
            }

            // (2) EL BLOOM QUE RESPIRA con la edad de la runa.
            LumenLib.BloomPulse(Main.spriteBatch, pos, 34f + 26f * Projectile.scale,
                new Color(214, 196, 255), 0.35f + 0.35f * madura, time, 1.6f);

            // (3) EL ANILLO RÚNICO (glifos solares — la runa ES un sello).
            VFXCore.Begin();
            if (VFXCore.Presupuesto(120))
            {
                SigiloLib.AnilloRunico(Projectile.Center, rEl, rEl * 0.62f, -0.34f,
                    0.26f + _edad * 0.0004f, 6, 0.85f * Projectile.scale, time, 0,
                    SigiloLib.CuerpoOro, SigiloLib.PuntaOro,
                    0.35f + 0.45f * madura + (_pulsoT > 0 ? 0.25f : 0f));
                // LA PERLA DE LA MADUREZ: el «estoy lista» que late al centro.
                if (madura > 0f)
                    SigiloLib.NodoCardinal(Projectile.Center, 6f + 4f * madura, time, 0f,
                        new Color(255, 232, 170), new Color(255, 252, 240), 0.6f + 0.4f * madura);
            }
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  2 · LA PALABRA HUECA — la que crece por fusión
    //      v6.79 EL VESTIDO: la palabra vuela con su cinta (Ribbon Alive)
    //      pintada con el gradiente del JUICIO (el oro→violeta→cian de
    //      DefinitivasLib); la que va a fundirse TI RAÍDA por una lanza de
    //      luz (Lance — la soga que la arrastra a su hermana); cada
    //      refuerzo estrena su anillo fantasma (EcosAnillo) y el VERBO DE
    //      PODER (el 5º) detona con CINE + lente.
    // ======================================================================
    public class PalabraHuecaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/VersoGlifo";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private int Refuerzos => (int)Projectile.ai[1];
        /// <summary>¿Sigo buscando a mi hermana mayor (fusión pendiente)?</summary>
        private bool Fusionable => Projectile.ai[2] == 0f;
        private float _edad;
        private Vector2 _posHermana;
        private bool _hayHermana;
        private int _refuerzoT;   // v6.79: el destello del refuerzo (10 ticks)

        public override void AI()
        {
            _edad++;
            Projectile.rotation += 0.03f;
            // v6.79 — LA ESTELA DE LA PALABRA (la cinta del verso).
            EstelaLib.Track(Projectile.whoAmI, 12).Push(Projectile.Center);
            if (_refuerzoT > 0) _refuerzoT--;

            // === LA ABSORCIÓN (la letra: «cada disparo nuevo la absorbe y
            //     la palabra CRECE por fusión»): la palabra NUEVA busca a
            //     su hermana MAYOR y se funde en ella — la hermana gana un
            //     refuerzo (más cuerpo, más daño). ===
            _hayHermana = false;
            if (Fusionable && _edad < 30f)
            {
                Projectile hermana = null; float mejorD = 1e9f;
                float rango = Etapa >= 2 ? 900f : 700f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != Type ||
                        p.owner != Projectile.owner || p.whoAmI == Projectile.whoAmI) continue;
                    float d = Vector2.Distance(p.Center, Projectile.Center);
                    if (d < mejorD) { mejorD = d; hermana = p; }
                }
                if (hermana != null && mejorD < rango)
                {
                    _hayHermana = true;
                    _posHermana = hermana.Center;
                    Vector2 hacia = (hermana.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float vivo = Etapa >= 3 ? 0.12f : 0.08f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 9f, vivo);
                    if (mejorD < 18f && hermana.ModProjectile is PalabraHuecaProjectile palabra)
                    {
                        // ¡FUSIÓN!: mi esencia pasa a ella.
                        palabra.Refuerzo();
                        Projectile.active = false;   // silenciosa (nada que dibujar)
                        return;
                    }
                }
                else if (_edad >= 28f) Projectile.ai[2] = 1f;   // no hay hermana: vivo como palabra
            }
        }

        /// <summary>UN REFUERZO MÁS (me lo gana la hermana al absorberme).</summary>
        public void Refuerzo()
        {
            if (Projectile.ai[1] >= 5f) return;
            Projectile.ai[1]++;
            _refuerzoT = 10;   // v6.79: el anillo fantasma del refuerzo
            // EL CRECIMIENTO: +35 % de cuerpo, +25 % de daño por refuerzo.
            Projectile.scale = 1f + Projectile.ai[1] * 0.35f;
            Projectile.width = (int)(24 * Projectile.scale);
            Projectile.height = (int)(24 * Projectile.scale);
            Projectile.damage = (int)(Projectile.damage * 1.25f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
            // EL VERBO DE PODER (la letra: «al tercer o cuarto refuerzo es
            // un verbo de poder enorme»): al 5º — el ÁREA.
            if (Projectile.ai[1] >= 5f)
            {
                // v6.79 — EL VERBO con CINE: el dorado del juicio + lente.
                CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 240, 200);
                ImpactoLib.GolpeDeCine(Projectile.Center, 1.6f, 5,
                    new Color(255, 240, 200), suave: false, destello: true);
                GravLens.Registrar(Projectile.Center, 170f, 0.30f, 0.4f);
                int pi = Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                    (int)(Projectile.damage * 1.6f), Projectile.knockBack * 2f, Projectile.owner);
                if (pi >= 0 && Etapa >= 4)
                    Main.projectile[pi].scale = 1.6f;   // E4: el verbo DOBLE de ancho
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float satisfecha = MathHelper.Clamp(Projectile.ai[1] / 5f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 250, 235), 0.35f + 0.45f * satisfecha);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color jugo = DefinitivasLib.GradienteJuicio(_edad * 0.0035f);

            VFXCore.AbrirAdditive();
            // (1) LA CINTA DEL VERSO (Ribbon Alive — el color del JUICIO).
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 12).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                EstelaLib.Ribbon(Main.spriteBatch, camino, 7f + 9f * satisfecha,
                    EstelaProfile.Alive, jugo, 0.35f + 0.4f * satisfecha,
                    Projectile.whoAmI * 31 + 11, time, head: true, taper: 90f);
            }
            // (2) LA SOGA DE LA FUSIÓN: la lanza de luz que la TI RAÍDA
            //     hacia su hermana (mientras viaja a fundirse).
            if (_hayHermana && Fusionable)
                LumenLib.Lance(Main.spriteBatch, pos,
                    (_posHermana - Projectile.Center).SafeNormalize(Vector2.UnitX),
                    Vector2.Distance(_posHermana, Projectile.Center) * 0.9f, 4f,
                    jugo * 0.8f, 0.8f);
            // (3) EL ANILLO FANTASMA DEL REFUERZO (la onda que estrena el
            //     cuerpo nuevo — vive 10 ticks).
            if (_refuerzoT > 0)
            {
                float pR = 1f - _refuerzoT / 10f;
                OrbitaLib.EcosAnillo(pos, (40f + 60f * pR) * OrbitaLib.Distorsion(time),
                    time, OrbitaLib.Distorsion(time),
                    new Color(255, 240, 200), new Color(190, 160, 255));
                OndaLib.Pulse(Main.spriteBatch, pos, DefinitivasLib.DeGolpe(pR),
                    80f + 50f * satisfecha, new Color(255, 240, 200), 0.7f,
                    Projectile.whoAmI * 7 + 5);
            }
            // (4) EL CUERPO: bloom del juicio + el núcleo que gana con la fusión.
            LumenLib.Bloom(Main.spriteBatch, pos, 30f + 40f * satisfecha,
                jugo, 0.45f + 0.35f * satisfecha);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  3 · LA VELA QUE ENGREÑE — la llama cargada
    //      v6.79 EL VESTIDO: la llama es FUEGO DE VERDAD (PyraLib: la
    //      hoguera de 4 lenguas + la lengua que estira el viento del
    //      vuelo + la luz con la rampa de temperatura) — el color y el
    //      tamaño viven en _carga01 como siempre; la muerte estalla con
    //      la rampa SolarFire y la onda cromática.
    // ======================================================================
    public class VelaEngrineProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/FireRing";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Carga => (int)Projectile.ai[1];   // 6..90 (tope E3: 120)
        private float _carga01;

        public override void AI()
        {
            if (_carga01 == 0f)
            {
                // PRIMER TICK: la carga se traduce en CUERPO.
                int tope = IdeasGrimorio.Etapa((int)Projectile.ai[0]) >= 3 ? 120 : 90;
                _carga01 = MathHelper.Clamp(Carga / (float)tope, 0.1f, 1f);
                Projectile.scale = 0.5f + _carga01 * 1.8f;
                Projectile.width = (int)(26 * Projectile.scale);
                Projectile.height = (int)(26 * Projectile.scale);
                Projectile.damage = (int)(Projectile.damage * (0.6f + _carga01 * 1.4f));
                Projectile.velocity *= 0.7f + _carga01 * 0.9f;
                if (IdeasGrimorio.Etapa((int)Projectile.ai[0]) >= 2) Projectile.penetrate = 5;
            }
            // LA LLAMA VIVE: ondea y suelta chispas (la E4: el rastro).
            Projectile.rotation += 0.10f * (1f + _carga01);
            // v6.79 — LA LUZ DE LA RAMPA (el color de la temperatura).
            PyraLib.Light(Projectile.Center, 80f + 70f * _carga01, PyraPalettes.SolarFire,
                0.35f + 0.6f * _carga01, 1f);
            if (Main.rand.NextBool(2))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(6f, 6f), 12, 12,
                    DustID.Torch, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.4f),
                    160, new Color(255, 200, 130), 1f + _carga01);
                Main.dust[di].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // v6.79 — LA MUERTE ESTALLA (la vela apagada suelta su calor):
            //     el AoE con la rampa SolarFire + la onda cromática.
            CrecimientoEstallidoProjectile.TinteProximo = new Color(255, 190, 120);
            CrecimientoEstallidoProjectile.RampaProxima = PyraPalettes.SolarFire;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 1.4f), Projectile.knockBack * 1.6f, Projectile.owner);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            lightColor = Color.Lerp(lightColor, new Color(255, 235, 200), 0.6f);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            float llama = 16f + 30f * _carga01;

            VFXCore.AbrirAdditive();
            // (1) LA HOGUERA (PyraLib.Flame — 4 lenguas de fuego REAL con
            //     la rampa SolarFire; el radio vive en _carga01).
            PyraLib.Flame(Main.spriteBatch, pos, llama, PyraPalettes.SolarFire,
                Projectile.whoAmI * 13 + 2, time, 0.55f + 0.6f * _carga01);
            // (2) LA LENGUA DEL VUELO (la que estira el viento de la
            //     caída — apuntando contra la marcha, como la llama real).
            Vector2 dirV = Projectile.velocity;
            if (dirV.LengthSquared() > 0.1f)
                PyraLib.Tongue(Main.spriteBatch, pos, llama * 1.15f, 12f + 10f * _carga01,
                    PyraPalettes.SolarFire, 0.5f + 0.5f * _carga01, Projectile.whoAmI * 13 + 6,
                    time, 0.8f + 0.5f * _carga01, dirV.X * 0.02f, 1f,
                    dirV.ToRotation() + MathHelper.PiOver2);
            // (3) EL BLOOM QUE RESPIRA con la llama (hz vivo — la vela).
            LumenLib.BloomPulse(Main.spriteBatch, pos, 30f + 44f * _carga01,
                new Color(255, 200, 130), 0.5f + 0.5f * _carga01, time,
                2.0f + 1.6f * _carga01);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  4 · EL COMETA DE PALABRAS — crece con la distancia
    //      E1: chispa → cometa por distancia · E2: crece más rápido
    //      (700 px) · E3: al llegar a cometa PERFORA 3 · E4: la cola
    //      deja una estela de brillo (quads traseros).
    //      v6.79 EL VESTIDO: el cometa es PRISMA VIVO (ColorPrisma: el
    //      cuerpo cicla el arcoíris y la cola es el GRADIENTE a lo largo
    //      — ALoLargo), con su cinta (Ribbon Comet), los fantasmas de la
    //      lanza (LanceTrail hacia atrás), la nebulosa de la cola
    //      (NebulaLib) y la NOVA del cometa maduro (Flare + Flash al
    //      llegar a cometa).
    // ======================================================================
    public class CometaPalabrasProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/CometHead";

        public override void SetDefaults()
        {
            Projectile.width = 22; Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private float Dist => Projectile.ai[2];   // px recorridos
        private int _danoBase;
        private int _novaT;    // v6.79: la vida del destello del cometa maduro

        public override void AI()
        {
            // LA DISTANCIA ALIMENTA: cada tick cuenta lo andado.
            if (_danoBase == 0) _danoBase = Math.Max(1, Projectile.damage);
            Projectile.ai[2] += Projectile.velocity.Length();
            float meta = Etapa >= 2 ? 700f : 900f;          // a 30 bloques (960 px) es cometa
            float viaje = MathHelper.Clamp(Dist / meta, 0f, 1f);
            Projectile.scale = 0.35f + viaje * 1.15f;
            Projectile.width = (int)(22 * Projectile.scale);
            Projectile.height = (int)(22 * Projectile.scale);
            // EL DAÑO VIAJA CON EL CUERPO (la chispa pica, el cometa rompe):
            // de la BASE (guardada al nacer) hacia el doble — nunca
            // multiplicativo en cadena (crece con el VIAJE, tope 2,15×).
            Projectile.damage = (int)(_danoBase * (1f + viaje * 1.15f));
            if (viaje >= 1f && Etapa >= 3) Projectile.penetrate = 3;

            // v6.79 — LA NOVA DEL COMETA MADURO: al llegar a cometa, UN
            //     destello (Flare grande + flash + el kick suave) — el
            //     momento en que la chispa SE VUELVE cometa.
            if (viaje >= 1f && _novaT == 0)
            {
                _novaT = 14;
                OndaLib.Flash(new Color(220, 235, 255), 0.20f, 10, Projectile.Center);
                OndaLib.Kick(2.5f, 6);
            }
            if (_novaT > 0) _novaT--;

            // v6.79 — LA ESTELA Y LA LUZ del camino (la cinta del cometa).
            EstelaLib.Track(Projectile.whoAmI, 16).Push(Projectile.Center);
            Color cPrisma = ColorPrisma.ValueAt(_relojCometa);
            LumenLib.LightAlong(Projectile.Center,
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 90f,
                cPrisma, 0.7f + 0.5f * viaje, 30f);

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.35f, 0.55f) * (0.4f + viaje));
            if (Main.rand.NextBool(3))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.PurpleTorch, -Projectile.velocity.X * 0.15f, -Projectile.velocity.Y * 0.15f,
                    170, new Color(225, 218, 255), 0.9f);
                Main.dust[di].noGravity = true;
            }
        }

        /// <summary>El reloj del prisma (ciclo de ~2,4 s — el cometa entero cambia de color).</summary>
        private float _relojCometa;
        private float RelojCometa => _relojCometa += 0.42f;

        public override void OnKill(int timeLeft)
        {
            // v6.79 — EL IMPACTO DEL COMETA: el AoE con el color prisma del
            //     momento (el cometa muere del color que llevaba).
            CrecimientoEstallidoProjectile.TinteProximo = ColorPrisma.ValueAt(_relojCometa);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * 1.3f), Projectile.knockBack * 1.5f, Projectile.owner);
            for (int i = 0; i < 8; i++)
            {
                int di = Dust.NewDust(Projectile.Center, 16, 16, DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 1f), 170,
                    new Color(230, 225, 255), 1f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float viaje = MathHelper.Clamp(Dist / (Etapa >= 2 ? 700f : 900f), 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(240, 246, 255), 0.35f + 0.4f * viaje);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color cPrisma = ColorPrisma.ValueAt(_relojCometa);

            // (1) LA NEBULOSA DE LA COLA (al buffer — la nube que sigue al
            //     cometa, del color del momento).
            VFXCore.Begin();
            NebulaLib.Nube(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 26f,
                30f + 50f * viaje, time, Projectile.whoAmI * 17 + 3,
                cPrisma, new Color(235, 240, 255), 0.06f + 0.06f * viaje, 10 + (int)(8f * viaje));
            VFXCore.FlushAdditive();

            VFXCore.AbrirAdditive();
            // (2) LA CINTA DEL COMETA (Ribbon Comet — la cola con el
            //     GRADIENTE PRISMA a lo largo: la boca del color de hace
            //     un momento, la punta del de ahora).
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 16).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                EstelaLib.Ribbon(Main.spriteBatch, camino, 8f + 16f * viaje,
                    EstelaProfile.Comet, cPrisma, 0.5f + 0.4f * viaje,
                    Projectile.whoAmI * 31 + 19, time, head: true, taper: 130f);
            }
            // (3) LOS FANTASMAS DE LA LANZA (la estela que se estira atrás).
            Vector2 atras = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            LumenLib.LanceTrail(Main.spriteBatch, pos, atras,
                40f + 90f * viaje, 3.5f, cPrisma, 0.7f + 0.4f * viaje, 7, 14f);
            // (4) LA CABEZA: bloom prisma + LA NOVA del maduro.
            LumenLib.Bloom(Main.spriteBatch, pos, 30f + 44f * viaje,
                cPrisma, 0.6f + 0.4f * viaje);
            if (_novaT > 0)
            {
                float pN = 1f - _novaT / 14f;
                LumenLib.Flare(Main.spriteBatch, pos, 90f * DefinitivasLib.DeGolpe(pN),
                    cPrisma, 1.3f * (1f - pN * 0.7f), time * 1.4f);
            }
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  5 · EL SELLO DIGITAL — el clicker de las páginas
    //      v6.79 EL VESTIDO: el círculo rúnico del jugador es un ANILLO
    //      RÚNICO de verdad (SigiloLib — los glifos orbitando) y las 10
    //      páginas son PERLAS CARDINALES (NodoCardinal: la encendida
    //      brilla, la apagada espera); la PALABRA DE PODER detona con
    //      CADENA de perlin visible (ChainBolt del sello a cada presa) +
    //      hitstop + flash + lente; los PERNOS vuelan como rayitos
    //      fractales (Bolt de su nacimiento a su posición).
    // ======================================================================
    public class SelloDigitalProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/SelloRueda";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private float Radio => Projectile.ai[1];        // el radio del círculo (lo pasa el ítem)
        /// <summary>¿SOY la palabra de poder de esta ronda?</summary>
        private bool PalabraDePoder => Projectile.ai[2] == 1f;

        /// <summary>EL CONTADOR DE PÁGINAS (SP-first: el arma de pruebas
        /// de la casa — la cuenta vive mientras el dueño use el arma).</summary>
        internal static int Paginas;

        // v6.79 — las presas de la cadena (para dibujar el ChainBolt).
        private readonly List<Vector2> _presasCadena = new List<Vector2>();
        private int _cadenaT;

        public override void AI()
        {
            Projectile.rotation += 0.05f;

            // === LA CUENTA DE PÁGINAS (la letra: «cada sello suma páginas
            //     — a las 10 páginas, una PALABRA DE PODER»): el PRIMER
            //     tick de cada sello suma UNA — y la que la complete nace
            //     como la palabra de poder (ai[2]=1) y detona. ===
            if (Projectile.timeLeft > 478f && Projectile.ai[2] == 0f)
            {
                Paginas++;
                if (Paginas >= (Etapa >= 2 ? 8 : 10))
                {
                    Paginas = 0;
                    Projectile.ai[2] = 1f;   // YO SOY la palabra de poder
                }
            }

            // === LA PALABRA DE PODER (la letra: «a las 10 páginas, una
            //     PALABRA DE PODOR (área, cadena, empuje)») ===
            if (PalabraDePoder && Projectile.timeLeft > 470f)
            {
                // ÁREA: el estallido de la casa.
                CrecimientoEstallidoProjectile.TinteProximo = new Color(232, 210, 255);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                    (int)(Projectile.damage * 1.8f), Projectile.knockBack * 2.2f, Projectile.owner);
                // EMPUJE: todo lo vivo en 160 px sale volando.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    Vector2 delta = n.Center - Projectile.Center;
                    float d = delta.Length();
                    if (d < 160f && d > 0.01f)
                        n.velocity += delta / d * 9f;
                }
                // CADENA (E3: 5 presas): pernos a los enemigos cercanos.
                _presasCadena.Clear();
                int cadena = Etapa >= 3 ? 5 : 3;
                int lanzados = 0;
                for (int i = 0; i < Main.maxNPCs && lanzados < cadena; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    float d = Vector2.Distance(n.Center, Projectile.Center);
                    if (d > 460f) continue;
                    _presasCadena.Add(n.Center);
                    Vector2 vel = (n.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 16f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel, Type,
                        (int)(Projectile.damage * 0.7f), Projectile.knockBack, Projectile.owner,
                        Nivel, Radio, 2f);   // ai[2]=2: PERNO (vuela y muere)
                    lanzados++;
                }
                // v6.79 — EL CINE DE LA PALABRA: el hitstop + el flash +
                //     la lente + la cadena VIVA 12 ticks.
                _cadenaT = 12;
                ImpactoLib.GolpeDeCine(Projectile.Center, 1.8f, 6,
                    new Color(232, 210, 255), suave: false, destello: true);
                OndaLib.Flash(new Color(232, 210, 255), 0.22f, 10, Projectile.Center);
                GravLens.Registrar(Projectile.Center, 190f, 0.34f, 0.45f);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
                Projectile.timeLeft = 4;   // la palabra de poder se consume YA
            }
            if (_cadenaT > 0) _cadenaT--;

            // EL PERNO (ai[2]=2): vuela recto y muere pronto.
            if (Projectile.ai[2] == 2f)
            {
                // v6.79 — el nacimiento del perno (para su rayito).
                if (Projectile.localAI[0] == 0f && Projectile.localAI[1] == 0f)
                {
                    Projectile.localAI[0] = Projectile.Center.X;
                    Projectile.localAI[1] = Projectile.Center.Y;
                }
                Projectile.rotation += 0.4f;
                if (Projectile.timeLeft > 30) Projectile.timeLeft = 30;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // EL EXPIRE del sello quieto: un estallido menor (la E4 lo
            // dobla) — pisar el campo de sellos SE PAGA.
            if (Projectile.ai[2] == 2f) return;   // el perno muere en silencio
            CrecimientoEstallidoProjectile.TinteProximo = new Color(232, 210, 255);
            int mult = Etapa >= 4 ? 2 : 1;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * (0.5f * mult)), Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 205), 0.5f);

            // === EL CÍRCULO RÚNICO DEL JUGADOR (v6.79: el ANILLO RÚNICO de
            //     SigiloLib — los glifos orbitando el aro — y las 10
            //     páginas como PERLAS CARDINALES: la encendida BRILLA, la
            //     apagada espera. El ÚLTIMO sello vivo lo dibuja). ===
            if (Projectile.timeLeft > 460f && !Main.gameMenu)
            {
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active)
                {
                    VFXCore.Begin();
                    if (VFXCore.Presupuesto(160))
                    {
                        float alfa = 0.30f + 0.08f * MathF.Sin((float)Projectile.timeLeft * 0.2f);
                        SigiloLib.AnilloRunico(dueno.MountedCenter, Radio, Radio * 0.60f, -0.30f,
                            0.26f, 8, 0.80f, time, 0,
                            SigiloLib.CuerpoAzul, SigiloLib.PuntaAzul, alfa);
                        // LAS PÁGINAS (10 perlas — la cuenta que va con el clic).
                        for (int m = 0; m < 10; m++)
                        {
                            float am = MathHelper.TwoPi * (m + 0.5f) / 10f - MathHelper.PiOver2;
                            bool on = m < Paginas;
                            SigiloLib.NodoCardinal(
                                dueno.MountedCenter + new Vector2(MathF.Cos(am), MathF.Sin(am)) * Radio,
                                on ? 7f : 4f, time, m * 0.63f,
                                on ? new Color(255, 232, 170) : new Color(150, 140, 170),
                                on ? new Color(255, 252, 240) : new Color(190, 185, 210),
                                on ? 1.0f : 0.28f);
                        }
                    }
                    VFXCore.FlushAdditive();
                }
            }

            Vector2 pos = Projectile.Center - Main.screenPosition;

            VFXCore.AbrirAdditive();
            // === EL SELLO: el aro fino girando + el bloom; la PALABRA DE
            //     PODER arde dorada; el PERNO es un rayito fractal de su
            //     nacimiento a su posición (Bolt). ===
            if (Projectile.ai[2] == 2f)
            {
                Vector2 origen = new(Projectile.localAI[0], Projectile.localAI[1]);
                if (origen != Vector2.Zero)
                    StormLib.Bolt(Main.spriteBatch, origen - Main.screenPosition, pos,
                        Projectile.whoAmI * 17 + 9, (int)Main.GameUpdateCount,
                        2.4f, new Color(140, 190, 255) * 0.55f, new Color(240, 250, 255), 0.85f, 6, 12f);
                LumenLib.Bloom(Main.spriteBatch, pos, 26f, new Color(140, 190, 255), 0.8f);
            }
            else
            {
                bool poder = PalabraDePoder && Projectile.timeLeft > 470f;
                OrbitaLib.AnilloFino(pos, 26f + 10f * MathF.Sin(time * 2.4f), time * 1.8f,
                    poder ? new Color(255, 232, 170) : new Color(232, 210, 255));
                LumenLib.BloomPulse(Main.spriteBatch, pos, 34f,
                    poder ? new Color(255, 232, 170) : new Color(232, 210, 255),
                    poder ? 1.0f : 0.55f, time, 2.2f);
            }
            // === LA CADENA VIVA (el ChainBolt del sello a cada presa —
            //     los 12 ticks tras la palabra de poder). ===
            if (_cadenaT > 0 && _presasCadena.Count > 0)
            {
                float vivo = _cadenaT / 12f;
                for (int i = 0; i < _presasCadena.Count; i++)
                    StormLib.ChainBolt(Main.spriteBatch, pos,
                        _presasCadena[i] - Main.screenPosition,
                        Projectile.whoAmI * 17 + 3 + i, (int)Main.GameUpdateCount,
                        3.2f, new Color(140, 190, 255) * (0.6f * vivo),
                        new Color(240, 250, 255), 0.9f * vivo, 10, 14f);
            }
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  6 · EL LÁTIGO DE PERLIN — el brazo corto de rayo
    //      El brazo nace de la mano y su PUNTA persigue el cursor
    //      clamped a la longitud (que crece con el nivel: 22 → 96 px).
    //      E3+: el brazo estira 12 px más.
    //      v6.79 EL VESTIDO: el brazo es un ÁRBOL de perlin (MultiBolt:
    //     el tronco + las ramas conectadas, azul y cian relevándose), la
    //      punta lleva su gorro (EndCap) y su destello (Flare); el GOLPE
    //      prende el ImpactFlash + el chispazo (SparkBurst) + el kick
    //      suave — el chasquido del látigo.
    // ======================================================================
    public class LatigoPerlinProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/BoltCore";

        public override void SetDefaults()
        {
            Projectile.width = 28; Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        private int Nivel => (int)Projectile.ai[0];
        private Vector2 _boca;
        private int _chispaT;   // v6.79: la vida del chasquido del golpe

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) { Projectile.Kill(); return; }

            // LA BOCA (la del arma-C de la casa: 13 px delante, 20 arriba).
            _boca = dueno.MountedCenter + new Vector2(13f * dueno.direction, -20f);

            // LA LONGITUD del brazo (la letra: «que sale del libro y crece
            // de 20 a 80+ px con el nivel»): 22 + 0,75·nivel, tope 96 —
            // E3: +12 (el brazo largo de la madurez).
            float len = MathHelper.Clamp(22f + Nivel * 0.75f, 22f, 96f);
            if (IdeasGrimorio.Etapa(Nivel) >= 3) len = Math.Min(len + 12f, 108f);

            // LA PUNTA: el cursor clamped a la longitud del brazo.
            Vector2 alCursor = Main.MouseWorld - _boca;
            if (alCursor.Length() > len) alCursor = alCursor.SafeNormalize(Vector2.Zero) * len;
            Vector2 meta = _boca + alCursor;
            // la punta PERSIGUE el punto (el latigazo tiene aceite).
            Projectile.Center = Vector2.Lerp(Projectile.Center, meta, 0.38f);
            Projectile.velocity = alCursor;   // la rotación y el empuje del golpe
            Projectile.rotation = alCursor.ToRotation();
            if (_chispaT > 0) _chispaT--;

            // EL CHASQUIDO: chispas en la punta.
            if (Main.rand.NextBool(3))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.Vortex, 0f, 0f, 170, new Color(210, 235, 255), 0.8f);
                Main.dust[di].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.4f, 0.55f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // v6.79 — EL CHASQUIDO DEL GOLPE: 8 ticks de chispazo + el kick
            //     suave (el látigo pega y la pantalla lo sabe).
            _chispaT = 8;
            OndaLib.Kick(2.5f, 6);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            lightColor = Color.Lerp(lightColor, new Color(235, 248, 255), 0.65f);

            Vector2 bocaScr = _boca - Main.screenPosition;
            Vector2 puntaScr = Projectile.Center - Main.screenPosition;
            float vivo = _chispaT > 0 ? 1f - _chispaT / 8f : 0f;

            VFXCore.AbrirAdditive();
            // (1) EL BRAZO: el ÁRBOL de perlin (tronco + ramas conectadas,
            //     azul y cian relevándose — MultiBolt).
            StormLib.MultiBolt(Main.spriteBatch, bocaScr, puntaScr,
                Projectile.whoAmI * 17 + 3, (int)Main.GameUpdateCount,
                4.2f, new Color(140, 190, 255) * 0.60f, new Color(120, 235, 255) * 0.55f,
                new Color(240, 250, 255), 0.9f,
                MathF.Min(0.17f, 14f / MathF.Max(1f, Vector2.Distance(bocaScr, puntaScr))), 10);
            // (2) EL GORRO DE LA PUNTA (el electrodo del látigo).
            StormLib.EndCap(Main.spriteBatch, puntaScr, 7f,
                new Color(140, 190, 255) * 0.7f, new Color(240, 250, 255), 1f);
            // (3) LA CHISPA DE LA PUNTA (el flare que va con el cursor).
            LumenLib.Flare(Main.spriteBatch, puntaScr, 26f,
                new Color(210, 240, 255), 0.9f, time * 2.1f);
            // (4) EL CHASQUIDO DEL GOLPE (el chispazo + el destello del
            //     impacto — la vida de 8 ticks).
            if (_chispaT > 0)
            {
                StormLib.ImpactFlash(Main.spriteBatch, puntaScr, 40f * DefinitivasLib.DeGolpe(vivo),
                    new Color(210, 240, 255), 1.2f * (1f - vivo), time);
                StormLib.SparkBurst(Main.spriteBatch, puntaScr, new Color(210, 240, 255),
                    18f, 10, Projectile.whoAmI * 7 + 2, vivo, 12f);
            }
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  7 · EL DEVORADOR MENOR — la boquita que crece mordiendo
    //      E1: muerde y crece · E2: revienta a ×2 · E3: la mordida pega
    //      más rápido (cooldown 10) · E4: la boca NACE grande (×1,3).
    //      v6.79 EL VESTIDO: la boca ES un abismo (CodigosLib.OjoAbismo:
    //      el horizonte negro con las estrellas cayendo en espiral — el
    //      devorador se traga la luz), el hambre CURVA el fondo
    //      (GravLens persistente que crece con las mordidas), el jadeo
    //      es una nebulosa morada (NebulaLib) y el REVENTÓN es CINE:
    //      hitstop + onda cromática + la lente del clímax.
    // ======================================================================
    public class DevoradorMenorProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/LenguaCabeza";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private int Mordidas => (int)Projectile.ai[1];
        /// <summary>El hambre 0..1 (cuánto le falta para reventar).</summary>
        private float Hambre => MathHelper.Clamp(Mordidas / (Etapa >= 2 ? 8f : 10f), 0f, 1f);

        public override void AI()
        {
            // LA BOQUITA VUELA: persigue a la presa más cercana.
            NPC presa = null; float mejorD = 900f * 900f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - Projectile.Center).LengthSquared();
                if (d < mejorD) { mejorD = d; presa = n; }
            }
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 11f, 0.14f);
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            // EL CRECIMIENTO: la escala VIVE con las mordidas (la letra:
            // «crece un 15 % con cada mordida hasta reventar»).
            Projectile.scale = (Etapa >= 4 ? 1.3f : 1f) * (1f + Mordidas * 0.15f);
            Projectile.width = (int)(24 * Projectile.scale);
            Projectile.height = (int)(24 * Projectile.scale);

            // v6.79 — EL HAMBRE CURVA EL FONDO: la lente persistente que
            //     crece con las mordidas (re-registrada cada tick — vida
            //     0,25 s: si muere, la lente se apaga sola).
            if (!Main.dedServ && Mordidas > 0)
                GravLens.Registrar(Projectile.Center, 60f + 70f * Hambre,
                    0.14f + 0.30f * Hambre, 0.25f);

            // LA NUBE: la boca grande jadea bruma (el polvo del hambre).
            if (Mordidas >= 3 && Main.rand.NextBool(5))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(5f, 5f), 10, 10,
                    DustID.Shadowflame, Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.8f, 0f),
                    150, new Color(140, 90, 200), 0.8f);
                Main.dust[di].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.2f, 0.45f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA MORDIDA CUENTA (la letra: «muerde varias veces y crece un
            // 15 % con cada mordida»): cada golpe la hace MÁS grande.
            Projectile.ai[1]++;
            if (Etapa >= 3 && Projectile.localNPCHitCooldown > 10)
                Projectile.localNPCHitCooldown = 10;
            // EL DAÑO CRECE con el cuerpo (la boca más grande muerde más).
            Projectile.damage = (int)(Projectile.damage * 1.08f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item3, Projectile.Center);

            // ¡REVENTÓ! (la letra: «hasta reventar en una nube de bruma»).
            float tope = Etapa >= 2 ? 8f : 10f;
            if (Projectile.ai[1] >= tope)
            {
                // v6.79 — EL REVENTÓN CON CINE: el violeta del hambre + la
                //     lente del clímax + el hitstop del final.
                CrecimientoEstallidoProjectile.TinteProximo = new Color(190, 130, 255);
                ImpactoLib.GolpeDeCine(Projectile.Center, 1.7f, 6,
                    new Color(190, 130, 255), suave: false, destello: true);
                GravLens.Registrar(Projectile.Center, 210f, 0.50f, 0.50f);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                    (int)(Projectile.damage * 2f), Projectile.knockBack * 2f, Projectile.owner);
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            // LA NUBE DE BRUMA del reventón (dust morado — el hambre satisfecha).
            for (int i = 0; i < 14; i++)
            {
                int di = Dust.NewDust(Projectile.Center, 20, 20, DustID.Shadowflame,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 1f), 170,
                    new Color(150, 100, 210), 1.2f);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly;
            lightColor = Color.Lerp(lightColor, new Color(240, 225, 255), 0.4f + 0.04f * Math.Min(Mordidas, 8));

            // (1) LA BOCA ES UN ABISMO (CodigosLib.OjoAbismo — el horizonte
            //     negro y las estrellas cayendo en espiral DETRÁS del
            //     sprite: el devorador se traga la luz). Se abre con el
            //     hambre: la boca vacía es una boquita; la llena, un abismo.
            if (Mordidas > 0 && !Main.gameMenu && VFXCore.Presupuesto(140))
                CodigosLib.OjoAbismo(Projectile.Center, 22f + 46f * Projectile.scale * 0.5f,
                    Hambre, time, Projectile.whoAmI * 11 + 4);

            // (2) EL JADEO: la nebulosa morada alrededor de la boca grande.
            VFXCore.Begin();
            if (Mordidas >= 3)
                NebulaLib.Nube(Projectile.Center, 30f + 30f * Hambre, time,
                    Projectile.whoAmI * 17 + 8, new Color(150, 100, 210),
                    new Color(90, 60, 160), 0.09f, 10);
            VFXCore.FlushAdditive();

            // (3) EL CUERPO: el bloom violeta que gana con el hambre.
            VFXCore.AbrirAdditive();
            LumenLib.BloomPulse(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                34f + 44f * Hambre, new Color(170, 120, 235),
                0.30f + 0.45f * Hambre, time, 1.2f + 2.2f * Hambre);
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }
}
