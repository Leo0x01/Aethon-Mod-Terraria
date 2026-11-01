using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.77 — LAS ARMAS DEL CRECIMIENTO (1 · 7) — los proyectiles.
    //
    //  La primera mitad del arsenal de las 14 ideas (la letra completa en
    //  CrecimientoArsenal.cs). TODAS con las librerías de la casa
    //  (VFXCore, StormLib, BrumaFX en sus PreDraw) y texturas YA
    //  existentes de la casa (override — cero PNG nuevos de proyectiles:
    //  la lección v6.60.1 de la ceremonia).
    // ======================================================================

    // ======================================================================
    //  0 · EL ESTALLIDO — el AoE común de la casa (la palanca de área).
    //      Un proyectil de vida cortísima con hitbox grande: lo spawnean
    //      la semilla al madurar, la palabra al 5º refuerzo, la palabra
    //      de poder del sello, la estrella al llegar y la espora al
    //      detonar. El daño lo trae cada caller en su NewProjectile.
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
            Projectile.timeLeft = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.4f, 0.7f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // LA NOVA: un destello que se apaga — el AoE SE VE.
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 210), 0.7f);
            float fade = Projectile.timeLeft / 8f;
            Projectile.scale = 1.6f - fade * 0.9f;
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, new Color(255, 236, 200) * (0.55f * fade),
                new Vector2(90f * (1.4f - fade * 0.5f), 90f * (1.4f - fade * 0.5f)));
            VFXCore.Quad(Projectile.Center, new Color(196, 160, 255) * (0.40f * fade),
                new Vector2(130f * (1.2f - fade * 0.6f), 130f * (1.2f - fade * 0.6f)));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  1 · LA SEMILLA RÚNICA — la runa que germina
    //      E1: la runa germina y detona · E2: el área de la germinación
    //      crece más · E3: la runa madura vive más (12 s) · E4: al
    //      detentar deja DOS semillas hijas.
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

        public override void AI()
        {
            _edad++;
            if (!EsRuna)
            {
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
                for (int i = 0; i < 5; i++)
                {
                    float ang = MathHelper.TwoPi * i / 5f + Projectile.rotation;
                    Dust d = Dust.NewDustPerfect(Projectile.Center +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (14f + _edad * 0.35f),
                        DustID.Enchanted_Gold, Vector2.Zero, 180, new Color(230, 218, 255), 0.8f);
                    d.noGravity = true;
                }
            }

            // AL EXPIRAR: la runa madura detona sola (la esfera de luz).
            if (Projectile.timeLeft <= 2)
                DetonarMadura();
        }

        /// <summary>LA DETONACIÓN DE LA MADURA (la letra: «clicar sobre
        /// una runa madura la detona con la esfera de luz»): la nova del
        /// nacimiento — el estallido de la casa + las hijas de la E4.</summary>
        public void DetonarMadura()
        {
            if (Projectile.active)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!EsRuna) return;   // la semilla en vuelo muere en silencio
            // EL ESTALLIDO (la esfera de luz): AoE + las hijas de la E4.
            int radioDano = (int)(Projectile.damage * 1.5f);
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

        public override bool PreDraw(ref Color lightColor)
        {
            if (!EsRuna) { lightColor = Color.Lerp(lightColor, new Color(255, 250, 220), 0.6f); return true; }
            // LA RUNA: glifo dorado-violeta girando, más brillo al madurar.
            float madura = MathHelper.Clamp((_edad - 40f) / 20f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 246, 225), 0.45f + 0.4f * madura);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 18f + 26f * Projectile.scale + 16f * MathF.Sin((float)Projectile.timeLeft * 0.3f) * madura;
            VFXCore.Quad(Projectile.Center, new Color(214, 196, 255) * (0.35f + 0.25f * madura),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  2 · LA PALABRA HUECA — la palabra que crece por fusión
    //      E1: la palabra lenta · E2: fusiona desde más lejos · E3: el
    //      homing de la absorción es más vivo · E4: al 4º refuerzo el
    //      VERBO DE PODOR es ÁREA (estallido el doble de ancho).
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

        public override void AI()
        {
            _edad++;
            Projectile.rotation += 0.03f;

            // === LA ABSORCIÓN (la letra: «cada disparo nuevo la absorbe y
            //     la palabra CRECE por fusión»): la palabra NUEVA busca a
            //     su hermana MAYOR y se funde en ella — la hermana gana un
            //     refuerzo (más cuerpo, más daño). ===
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
            // LA PALABRA: más blanca y más GRANDE con cada refuerzo.
            float satisfecha = MathHelper.Clamp(Projectile.ai[1] / 5f, 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(255, 250, 235), 0.35f + 0.45f * satisfecha);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 16f + 30f * Projectile.scale;
            VFXCore.Quad(Projectile.Center, new Color(255, 236, 190) * (0.28f + 0.22f * satisfecha),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  3 · LA VELA QUE ENGREÑE — la llama que se carga
    //      El CLIC se mantiene (channel): la cuenta vive en el ítem
    //      (TicksCargando 6..90) y viaja en ai[1] al soltar. E2: la llama
    //      perfora más · E3: carga tope 120 · E4: deja un rastro de fuego.
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
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.4f, 0.18f) * (0.5f + _carga01));
            if (Main.rand.NextBool(2))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(6f, 6f), 12, 12,
                    DustID.Torch, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.4f),
                    160, new Color(255, 200, 130), 1f + _carga01);
                Main.dust[di].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new Color(255, 235, 200), 0.6f);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 20f + 40f * _carga01 * (0.7f + 0.3f * MathF.Sin((float)Projectile.timeLeft * 0.35f));
            VFXCore.Quad(Projectile.Center, new Color(255, 190, 120) * (0.30f + 0.30f * _carga01),
                new Vector2(rGlow, rGlow));
            VFXCore.Quad(Projectile.Center, new Color(255, 240, 210) * (0.5f * _carga01),
                new Vector2(rGlow * 0.55f, rGlow * 0.55f));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  4 · EL COMETA DE PALABRAS — crece con la distancia
    //      E1: chispa → cometa por distancia · E2: crece más rápido
    //      (700 px) · E3: al llegar a cometa PERFORA 3 · E4: la cola
    //      deja una estela de brillo (quads traseros).
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

        public override void OnKill(int timeLeft)
        {
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
            float viaje = MathHelper.Clamp(Dist / (Etapa >= 2 ? 700f : 900f), 0f, 1f);
            lightColor = Color.Lerp(lightColor, new Color(240, 246, 255), 0.35f + 0.4f * viaje);
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            // LA CABEZA + LA COLA (la E4: la estela de brillo detrás).
            float rGlow = 14f + 34f * viaje;
            VFXCore.Quad(Projectile.Center, new Color(208, 224, 255) * (0.25f + 0.35f * viaje),
                new Vector2(rGlow, rGlow));
            if (Etapa >= 4)
            {
                Vector2 detras = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for (int i = 1; i <= 3; i++)
                    VFXCore.Quad(Projectile.Center + detras * (26f * i),
                        new Color(196, 210, 255) * (0.22f * (1f - i * 0.22f)),
                        new Vector2(rGlow * (1f - i * 0.2f), rGlow * (1f - i * 0.2f)));
            }
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  5 · EL SELLO DIGITAL — el clicker de los sellos
    //      E1: planta sellos, a las 10 páginas la PALABRA DE PODER ·
    //      E2: las páginas van a 8 · E3: la palabra de poder ENCADENA a
    //      5 enemigos · E4: los sellos expiran en estallido doble.
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
                int cadena = Etapa >= 3 ? 5 : 3;
                int lanzados = 0;
                for (int i = 0; i < Main.maxNPCs && lanzados < cadena; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    float d = Vector2.Distance(n.Center, Projectile.Center);
                    if (d > 460f) continue;
                    Vector2 vel = (n.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 16f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel, Type,
                        (int)(Projectile.damage * 0.7f), Projectile.knockBack, Projectile.owner,
                        Nivel, Radio, 2f);   // ai[2]=2: PERNO (vuela y muere)
                    lanzados++;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
                Projectile.timeLeft = 4;   // la palabra de poder se consume YA
            }

            // EL PERNO (ai[2]=2): vuela recto y muere pronto.
            if (Projectile.ai[2] == 2f)
            {
                Projectile.rotation += 0.4f;
                if (Projectile.timeLeft > 30) Projectile.timeLeft = 30;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // EL EXPIRE del sello quieto: un estallido menor (la E4 lo
            // dobla) — pisar el campo de sellos SE PAGA.
            if (Projectile.ai[2] == 2f) return;   // el perno muere en silencio
            int mult = Etapa >= 4 ? 2 : 1;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CrecimientoEstallidoProjectile>(),
                (int)(Projectile.damage * (0.5f * mult)), Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 205), 0.5f);

            // === EL CÍRCULO RÚNICO TENUE (la letra: «el radio dibujado
            //     como un círculo rúnico tenue alrededor del jugador»):
            //     el ÚLTIMO sello vivo lo dibuja — el aro con SUS páginas
            //     encendidas (10 marcas: la cuenta va con las páginas). ===
            if (Projectile.timeLeft > 460f && !Main.gameMenu)
            {
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active)
                {
                    Vector2 centro = dueno.MountedCenter - Main.screenPosition;
                    float r = Radio;
                    float alfa = 0.16f + 0.05f * MathF.Sin((float)Projectile.timeLeft * 0.2f);
                    VFXCore.AbrirAdditive();
                    VFXCore.Begin();
                    // EL ARO (12 segmentos — el círculo rúnico tenue).
                    for (int s = 0; s < 12; s++)
                    {
                        float a0 = MathHelper.TwoPi * s / 12f;
                        float a1 = MathHelper.TwoPi * (s + 0.72f) / 12f;
                        VFXCore.Line(
                            centro + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * r,
                            centro + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * r,
                            new Color(232, 210, 255) * alfa, 2.6f);
                    }
                    // LAS PÁGINAS (10 marcas en el aro — encendidas las
                    // andadas: la cuenta que va con el clic).
                    int encendidas = Paginas;
                    for (int m = 0; m < 10; m++)
                    {
                        float am = MathHelper.TwoPi * (m + 0.5f) / 10f - MathHelper.PiOver2;
                        bool on = m < encendidas;
                        VFXCore.Quad(
                            centro + new Vector2(MathF.Cos(am), MathF.Sin(am)) * r,
                            on ? new Color(255, 232, 170) * 0.6f
                               : new Color(150, 140, 170) * 0.22f,
                            new Vector2(on ? 11f : 7f, on ? 11f : 7f));
                    }
                    VFXCore.FlushAdditive();
                    VFXCore.RestaurarLoteVanilla();
                }
            }
            return true;
        }
    }

    // ======================================================================
    //  6 · EL LÁTIGO DE PERLIN — el brazo corto de rayo
    //      El brazo nace de la mano y su PUNTA persigue el cursor
    //      clamped a la longitud (que crece con el nivel: 22 → 96 px).
    //      E3+: el brazo estira 12 px más.
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

            // EL CHASQUIDO: chispas en la punta.
            if (Main.rand.NextBool(3))
            {
                int di = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.Vortex, 0f, 0f, 170, new Color(210, 235, 255), 0.8f);
                Main.dust[di].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.4f, 0.55f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // === EL BRAZO DE PERLIN (¡el idioma visual de los ojos! — la
            //     letra: «un brazo corto de rayo perlin que sale del
            //     libro»): StormLib.PerlinBolt de la MANO a la PUNTA, en
            //     coords de pantalla sobre lote aditivo (el patrón de
            //     TruenoPerlin de la casa). ===
            Vector2 bocaScr = _boca - Main.screenPosition;
            Vector2 puntaScr = Projectile.Center - Main.screenPosition;
            VFXCore.AbrirAdditive();
            StormLib.PerlinBolt(Main.spriteBatch, bocaScr, puntaScr,
                Projectile.whoAmI * 17 + 3, (int)Main.GameUpdateCount,
                3.4f, new Color(140, 190, 255) * 0.55f, new Color(240, 250, 255),
                0.85f, 0.16f);
            VFXCore.RestaurarLoteVanilla();

            lightColor = Color.Lerp(lightColor, new Color(235, 248, 255), 0.65f);
            // la PUNTA brilla.
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, new Color(220, 240, 255) * 0.5f, new Vector2(30f, 30f));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }

    // ======================================================================
    //  7 · EL DEVORADOR MENOR — la boquita que crece mordiendo
    //      E1: muerde y crece · E2: revienta a ×2 · E3: la mordida pega
    //      más rápido (cooldown 10) · E4: la boca NACE grande (×1,3).
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
            lightColor = Color.Lerp(lightColor, new Color(240, 225, 255), 0.4f + 0.04f * Math.Min(Mordidas, 8));
            VFXCore.AbrirAdditive();
            VFXCore.Begin();
            float rGlow = 16f + 34f * Projectile.scale;
            VFXCore.Quad(Projectile.Center, new Color(170, 120, 235) * (0.25f + 0.35f * MathHelper.Clamp(Mordidas / 8f, 0f, 1f)),
                new Vector2(rGlow, rGlow));
            VFXCore.FlushAdditive();
            VFXCore.RestaurarLoteVanilla();
            return true;
        }
    }
}
