using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Projectiles.Jefes
{
    // =====================================================================
    //  v6.95 — LOS TRES HACES DEL DIOS (la letra del usuario: «el arma
    //  nueva, renglon final, o sea, su ataque es bueno para darselo al
    //  jefe pero en vez de 3 hazes de luz, que sean 6 y que este
    //  posicionado de forma pareja, giren a su alrededor, tambien hay que
    //  darle al jefe el Rayo concentrado 2 como ataque definitvo, y
    //  tambien darle la mirada del eterno como ataque medio raro»).
    //
    //  TODO es HazLib de la casa (la librería de haces de la v6.93): el
    //  filo de borde rgb=0 EXACTO en cada vértice — LOS PICOS SON
    //  MATEMÁTICAMENTE IMPOSIBLES, jefe o jugador, sin excepción. Y la
    //  CERO-GC de la casa: leyes estáticas cacheadas en static readonly,
    //  estado en casillas estáticas refrescadas al empezar cada hook.
    //
    //  LAS TRES ARMAS DEL DIOS:
    //    · LOS SEIS RENGLONES (JefeRenglonesProj) — el ataque del
    //      Renglón Final hecho rueda: SEIS barras de tinta, cada 60°
    //      (posición pareja), GIRANDO alrededor del dios. El renglón del
    //      jugador era UNA línea con dos copias de tinta (3 a la vista);
    //      el del dios son SEIS de verdad, radiando del centro.
    //    · EL RAYO CONCENTRADO DEL DIOS (JefeRC2Proj) — el ATAQUE
    //      DEFINITIVO: la carga del conjuro (la paleta morado/rojo/negro
    //      de la casa) y el CONO que barre al portador — la firma que
    //      abre LA FURIA (fase 5).
    //    · LA MIRADA DEL DIOS (JefeMiradaProj) — el ATAQUE MEDIO RARO:
    //      el MISMO rayo de La Mirada del Eterno (sus leyes, su fucsia
    //      del iris del libro, su velo morado) girado contra el
    //      portador. Peso 1 en el menú: el ojo se abre POCO — y cuando
    //      se abre, CASTIGA.
    // =====================================================================

    // =====================================================================
    //  JefeRenglonesProj — LOS SEIS RENGLONES DEL DIOS
    // =====================================================================
    public class JefeRenglonesProj : ModProjectile
    {
        /// <summary>La vida de la rueda (en ticks).</summary>
        private const float Vida = 220f;

        /// <summary>El radio de la BOCA (donde nace cada renglón).</summary>
        private const float RadioBoca = 92f;

        /// <summary>El radio de la PUNTA (hasta donde escribe).</summary>
        private const float RadioPunta = 780f;

        /// <summary>EL TIEMPO (ai[0]).</summary>
        private float Tiempo { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        /// <summary>EL DUEÑO: el whoAmI del jefe (ai[1]).</summary>
        private float Dueño => Projectile.ai[1];

        /// <summary>LA FURIA (ai[2] ≠ 0): la rueda gira más rápido.</summary>
        private bool Furia => Projectile.ai[2] != 0f;

        // === EL ESTADO DE LAS LEYES (cero-GC) ===
        private static float _t;         // el tiempo vivo
        private static float _furia;     // 1 en furia
        private static float _angulo;    // el giro de la rueda

        private void RefrescarLeyes()
        {
            _t = Tiempo;
            _furia = Furia ? 1f : 0f;
            _angulo = Projectile.rotation;
        }

        // === LAS LEYES CACHEADAS ===
        private static readonly HazLib.LeyAncho LeySemiancho = SemianoRenglon;
        private static readonly HazLib.LeyAncho LeySemianchoVelo = SemianoRenglonVelo;
        private static readonly HazLib.LeyNodo LeyTinte = TinteRenglon;
        private static readonly HazLib.LeyNodo LeyTinteVelo = TinteRenglonVelo;
        private static readonly HazLib.LeyVertice LeyVertice = VerticeRenglon;
        private static readonly HazLib.LeyVertice LeyVerticeVelo = VerticeRenglonVelo;

        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 225;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // EL DUEÑO manda: si el dios muere, la rueda se disuelve.
            NPC jefe = Main.npc[(int)Dueño];
            if (jefe == null || !jefe.active || jefe.life <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = jefe.Center;
            Projectile.rotation += Furia ? 0.024f : 0.015f;   // EL GIRO de la rueda

            Tiempo++;
            RefrescarLeyes();
            Projectile.timeLeft = 10;   // vive mientras la rueda viva

            // LA LUZ y LAS CHISPAS: la tinta del dios gotea del borde.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && Tiempo % 3 == 0)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.30f, 0.85f));
                float ang = Projectile.rotation + Main.rand.Next(6) * (MathHelper.Pi / 3f);
                Vector2 boca = Projectile.Center + ang.ToRotationVector2() * RadioBoca;
                Dust d = Dust.NewDustPerfect(boca,
                    Main.rand.NextBool() ? DustID.PortalBoltTrail : DustID.Enchanted_Pink,
                    ang.ToRotationVector2() * Main.rand.NextFloat(1f, 4f), 0,
                    Main.rand.NextBool() ? new Color(176, 118, 255) : new Color(255, 214, 120),
                    Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }

            if (Tiempo > Vida) Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            RefrescarLeyes();
            float ancho = SemianoRenglon(0.5f) * 1.6f;
            float punto = 0f;
            for (int i = 0; i < 6; i++)
            {
                float ang = _angulo + i * (MathHelper.Pi / 3f);
                Vector2 dir = ang.ToRotationVector2();
                Vector2 a = Projectile.Center + dir * (RadioBoca * 0.8f);
                Vector2 b = Projectile.Center + dir * (RadioPunta * 0.97f);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    a, b, ancho, ref punto))
                    return true;
            }
            return false;
        }

        // === LAS LEYES DEL ASPECTO — la tinta del Renglón Final del dios ===

        private static float Rampa(float a, float b, float v)
            => MathHelper.Clamp((v - a) / (b - a), 0f, 1f);

        /// <summary>EL SEMIANCHO de un renglón del dios: nace fino (10),
        /// crece a 30 con el bulto del arranque, late ±4 y se desinfla en
        /// los últimos 25 t — la punta siempre redonda (√(1.001−x²)).</summary>
        private static float SemianoRenglon(float p)
        {
            float arranque = Rampa(0f, 0.12f, _t) * Rampa(0.015f, 0.004f, p);
            arranque = MathF.Sqrt(1.001f - arranque * arranque);
            float cierre = Rampa(0f, 25f, Vida - _t);
            float ideal = 30f + MathF.Cos(Main.GlobalTimeWrappedHourly * 70f) * 4f;
            return MathHelper.Lerp(10f, ideal, Rampa(0f, 0.2f, p)) * arranque * cierre;
        }

        private static float SemianoRenglonVelo(float p) => SemianoRenglon(p) * 1.8f;

        /// <summary>EL TINTE: VIOLETA DE LA CASA (176,118,255) que nace y
        /// muere en transparente — la tinta del renglón.</summary>
        private static Color TinteRenglon(float p)
            => new Color(176, 118, 255)
             * (Rampa(0f, 0.06f, p) * Rampa(1f, 0.86f, p) * Rampa(0f, 8f, _t));

        /// <summary>EL TINTE del velo: violeta profundo (120,60,220) al
        /// 34 % — el halo que envuelve la rueda.</summary>
        private static Color TinteRenglonVelo(float p)
            => new Color(120, 60, 220)
             * (Rampa(0.02f, 0.06f, p) * Rampa(1f, 0.86f, p) * Rampa(0f, 8f, _t)) * 0.34f;

        /// <summary>EL VÉRTICE del cuerpo: el grumo de ruido diagonal
        /// (la tinta viva), el CHARCO DORADO junto al dios (x<0.3 vira al
        /// oro 255,214,120 — el broche del libro) y el núcleo blanco
        /// (1/d) — el filo lo pone HazLib encima.</summary>
        private static Color VerticeRenglon(float x, float y, Color tinte)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float ruido = Frac(x * 2f - t * 2.6f) * Frac(y * 2f + x * 4f - t * 2.6f);
            float opacidad = ruido * 0.75f + 0.35f;

            float d = MathF.Abs(y - 0.5f);
            float brillo = d <= 0.0001f ? 1f : MathHelper.Clamp(0.35f / d, 0f, 1f);
            brillo *= 1f - Rampa(0.12f, 0.3f, d);

            float dorado = (1f - Rampa(0.14f, 0.34f, x)) * 0.85f;   // el charco del dios
            Color cuerpo = Color.Lerp(tinte, new Color(255, 214, 120), dorado);

            Vector4 suma = cuerpo.ToVector4() * opacidad + new Vector4(brillo);
            return new Color(
                (byte)Math.Clamp((int)(suma.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Z * 255f + 0.5f), 0, 255), 255);
        }

        /// <summary>EL VÉRTICE del velo — la cúpula pow(4y(1−y), 3)·2.8.</summary>
        private static Color VerticeRenglonVelo(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * bulto * 2.8f;
            Vector4 v = tinte.ToVector4() * k;
            return new Color(
                (byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);
        }

        private static float Frac(float x) => x - MathF.Floor(x);

        // === EL RENDER: 12 PARES en UN draw call ===
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                NPC jefe = Main.npc[(int)Dueño];
                if (jefe == null || !jefe.active) return false;
                RefrescarLeyes();

                Vector2 centro = Projectile.Center - Main.screenPosition;
                HazLib.Abrir();
                try
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 dir = (_angulo + i * (MathHelper.Pi / 3f)).ToRotationVector2();
                        Vector2 boca = centro + dir * RadioBoca;
                        Vector2 punta = centro + dir * RadioPunta;

                        // LA COMBA: cada renglón ondea ±26 px — la tinta cede.
                        float comba = MathF.Sin((float)_t * 0.045f + i * 1.3f) * 26f;

                        HazLib.PrepararPar(boca, punta, LeySemianchoVelo, comba, 26);
                        HazLib.Dibujar(LeyTinteVelo, LeyVerticeVelo, HazLib.FilasVelo);

                        HazLib.PrepararPar(boca, punta, LeySemiancho, comba, 30);
                        HazLib.Dibujar(LeyTinte, LeyVertice, HazLib.FilasNucleo);
                    }
                }
                finally { HazLib.Cerrar(); }
            }
            catch { }
            return false;
        }
    }

    // =====================================================================
    //  JefeRC2Proj — EL RAYO CONCENTRADO DEL DIOS (el ATAQUE DEFINITIVO)
    // =====================================================================
    public class JefeRC2Proj : ModProjectile
    {
        /// <summary>LA CARGA del conjuro (ticks).</summary>
        private const float Carga = 90f;

        /// <summary>EL LÁSER (ticks).</summary>
        private const float Laser = 300f;

        /// <summary>El largo del cono.</summary>
        private const float LargoCono = 2600f;

        private float Tiempo { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Dueño => Projectile.ai[1];

        /// <summary>LA MIRA (localAI[0]): el rumbo que persigue.</summary>
        private float Mira { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        // === EL ESTADO DE LAS LEYES (cero-GC) ===
        private static float _t;
        private static float _tamaño;

        private void RefrescarLeyes()
        {
            _t = Tiempo;
        }

        private static readonly HazLib.LeyNodo LeyTinteCono = TinteCono;
        private static readonly HazLib.LeyNodo LeyTinteVelo = TinteVeloRC2;
        private static readonly HazLib.LeyVertice LeyVerticeCono = VerticeCono;
        private static readonly HazLib.LeyVertice LeyVerticeVelo = VerticeVeloRC2;

        // EL SONIDO EN LOOP — la utilidad de la casa (el RC2 del jugador).
        private SonidoEnLoop _loop;

        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 4200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            _loop = null;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC jefe = Main.npc[(int)Dueño];
            if (jefe == null || !jefe.active || jefe.life <= 0)
            {
                _loop?.StopSound();
                Projectile.Kill();
                return;
            }

            Projectile.Center = jefe.Center;
            Projectile.timeLeft = 10;

            // === LA MIRA: persigue al presa LENTO (0.02) — el definitivo
            //     se esquiva leyéndolo, no corriendo. ===
            Player presa = Main.player[jefe.target];
            if (presa != null && presa.active && !presa.dead && Tiempo < Carga + Laser * 0.7f)
            {
                float objetivo = Projectile.AngleTo(presa.Center);
                float delta = MathHelper.WrapAngle(objetivo - Mira);
                Mira += delta * 0.02f;
                if (Math.Abs(delta) > 0.0005f) Projectile.netUpdate = true;
            }

            // === EL BARRIDO: media onda de ida (±1.5 rad) — el cono BARRE
            //     la arena una vez, lento. ===
            float barrido = MathF.Sin((float)((Tiempo - Carga) / Laser) * MathHelper.Pi)
                * 1.5f * MathHelper.Clamp((Tiempo - Carga) / 30f, 0f, 1f);
            Projectile.rotation = Mira + (Tiempo < Carga ? 0f : barrido);

            // === EL TAMAÑO: la envolvente cúbica de siempre (nace en 20 t,
            //     muere al final). ===
            float objetivoTam = MathF.Pow(
                Utils.GetLerpValue(Carga - 12f, Carga + 40f, Tiempo, true) *
                Utils.GetLerpValue(Carga + Laser + 40f, Carga + Laser, Tiempo, true), 3f);
            _tamaño = MathHelper.Lerp(_tamaño, objetivoTam, 0.1f);
            RefrescarLeyes();

            if (!Main.dedServ)
            {
                // EL SONIDO DE CARGA (el one-shot de la casa).
                if (Tiempo == 2f)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            new Terraria.Audio.SoundStyle(
                                "AethonMod/Content/Sounds/RayoConcentradoCarga",
                                Terraria.Audio.SoundType.Sound)
                            { MaxInstances = 1, Volume = 0.9f, Pitch = -0.15f },
                            Projectile.Center);
                    }
                    catch { }
                }
                // EL TEMBLOR SOSTENIDO (el modelo de trauma de la casa).
                if (Tiempo > Carga && Tiempo < Carga + Laser)
                    Pantalla.Sacudir(6f, 0.4f, "JefeRC2-sostenido");
                if (Tiempo == Carga + 1f)
                {
                    Pantalla.Sacudir(9f, 0.8f, "JefeRC2-encendido");
                    OndaLib.Kick(7f, 14);
                }

                // EL LOOP GRAVE del láser — muere con el proyectil.
                float vol = 1.1f * _tamaño;
                _loop ??= new SonidoEnLoop(
                    new Terraria.Audio.SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop")
                        { MaxInstances = 0 },
                    new Terraria.Audio.ProjectileAudioTracker(Projectile).IsActiveAndInGame);
                _loop.PlaySound(() => Projectile.Center, () => vol, () => -0.3f);
            }

            // LA LUZ del conducto: la paleta del conjuro en 3 puntos.
            if (!Main.dedServ && Tiempo > Carga - 10f)
            {
                Color cLuz = RayoConcentradoDosProjectile.ColorConjuro(Tiempo);
                float k = Math.Max(_tamaño, 0.15f);
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Lighting.AddLight(Projectile.Center,
                    cLuz.R / 255f * 1.6f * k, cLuz.G / 255f * 1.5f * k, cLuz.B / 255f * 1.4f * k);
                Lighting.AddLight(Projectile.Center + dir * 900f,
                    cLuz.R / 255f * 1.2f * k, cLuz.G / 255f * 1.1f * k, cLuz.B / 255f * 1.0f * k);
                Lighting.AddLight(Projectile.Center + dir * 1800f,
                    cLuz.R / 255f * 0.8f * k, cLuz.G / 255f * 0.7f * k, cLuz.B / 255f * 0.6f * k);
            }

            Tiempo++;
            if (Tiempo > Carga + Laser + 40f)
            {
                _loop?.StopSound();
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Tiempo > Carga + 20f && Tiempo < Carga + Laser + 10f)
            {
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center,
                    LargoCono * (0.55f + _tamaño * 0.45f),
                    Projectile.rotation, 0.20f * Math.Max(_tamaño, 0.02f));
            }
            return false;
        }

        // === LAS LEYES DEL ASPECTO — la paleta del conjuro de la casa ===

        /// <summary>EL TINTE del cono: la PALETA DEL CONJURO (violeta
        /// arcano → morado profundo → rojo sangre → magenta) VIAJANDO a
        /// lo largo del eje — el gradiente camina con el mundo.</summary>
        private static Color TinteCono(float p)
        {
            Color c = RayoConcentradoDosProjectile
                .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + p * 48f);
            float nace = MathHelper.Clamp(p / 0.04f, 0f, 1f);
            float muere = MathHelper.Clamp((1f - p) / 0.10f, 0f, 1f);
            return c * (nace * muere * (0.75f + 0.25f * _tamaño));
        }

        private static Color TinteVeloRC2(float p)
        {
            Color c = RayoConcentradoDosProjectile
                .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + p * 48f + 24f);
            float nace = MathHelper.Clamp(p / 0.05f, 0f, 1f);
            float muere = MathHelper.Clamp((1f - p) / 0.12f, 0f, 1f);
            return c * (nace * muere) * 0.34f;
        }

        /// <summary>EL VÉRTICE del núcleo: el grumo de ruido diagonal y
        /// el brillo interior (1/d) — la vena blanca del conducto.</summary>
        private static Color VerticeCono(float x, float y, Color tinte)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float ruido = Frac(x * 2f - t * 3f) * Frac(y * 2f + x * 5f - t * 3f);
            float opacidad = ruido * 0.7f + 0.4f;

            float d = MathF.Abs(y - 0.5f);
            float brillo = d <= 0.0001f ? 1f : MathHelper.Clamp(0.30f / d, 0f, 1f);
            brillo *= 1f - MathHelper.Clamp((d - 0.10f) / 0.22f, 0f, 1f);

            Vector4 suma = tinte.ToVector4() * opacidad + new Vector4(brillo * 0.9f);
            return new Color(
                (byte)Math.Clamp((int)(suma.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Z * 255f + 0.5f), 0, 255), 255);
        }

        /// <summary>EL VÉRTICE del velo — la cúpula pow(4y(1−y), 3)·3.</summary>
        private static Color VerticeVeloRC2(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * bulto * 3f;
            Vector4 v = tinte.ToVector4() * k;
            return new Color(
                (byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);
        }

        private static float Frac(float x) => x - MathF.Floor(x);

        // === EL RENDER: la carga rúnica + el cono, TODO HazLib ===
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                NPC jefe = Main.npc[(int)Dueño];
                if (jefe == null || !jefe.active) return false;
                RefrescarLeyes();

                Vector2 centro = Projectile.Center - Main.screenPosition;

                // === LA CARGA: DOS ANILLOS DE RUNAS girando en sentidos
                //     opuestos con la paleta del conjuro — el círculo del
                //     conjuro creciendo (12→14 runas). ===
                if (Tiempo < Carga + 10f)
                {
                    float r = Tiempo / Carga;
                    Texture2D estrella = DefinitivasLib.TexStar;
                    if (estrella != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            Vector2 ojo = estrella.Size() * 0.5f;
                            float apertura = MathHelper.Clamp(r / 0.8f, 0f, 1f);
                            for (int anillo = 0; anillo < 2; anillo++)
                            {
                                int n = anillo == 0 ? 12 : 8;
                                float radio = (anillo == 0 ? 120f : 76f) * apertura;
                                float giro = (anillo == 0 ? 1f : -1.4f) * r * MathHelper.TwoPi;
                                float escala = (anillo == 0 ? 0.5f : 0.36f) * apertura;
                                for (int i = 0; i < n; i++)
                                {
                                    float ang = giro + i * MathHelper.TwoPi / n;
                                    Color runa = RayoConcentradoDosProjectile
                                        .ColorConjuro(Tiempo * 1.5f + i * 4f + anillo * 24f);
                                    Main.spriteBatch.Draw(estrella,
                                        centro + ang.ToRotationVector2() * radio, null,
                                        runa * 0.85f, ang + MathHelper.PiOver2, ojo,
                                        escala, SpriteEffects.None, 0f);
                                }
                            }
                        }
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }

                // === EL CONO: dos capas sobre HazLib, UN draw call —
                //     nace en la boca del dios y se abre p^0.8. ===
                if (Tiempo > Carga - 8f && _tamaño > 0.01f)
                {
                    Vector2 dir = Projectile.rotation.ToRotationVector2();
                    Vector2 boca = centro + dir * 46f;
                    Vector2 punta = centro + dir * (LargoCono * (0.55f + _tamaño * 0.45f));

                    HazLib.Abrir();
                    try
                    {
                        HazLib.PrepararCono(boca, punta, 46f * _tamaño + 4f, 150f * _tamaño + 8f, 0f, 56);
                        HazLib.Dibujar(LeyTinteVelo, LeyVerticeVelo, HazLib.FilasVelo);

                        HazLib.PrepararCono(boca, punta, 30f * _tamaño + 3f, 96f * _tamaño + 5f, 0f, 60);
                        HazLib.Dibujar(LeyTinteCono, LeyVerticeCono, HazLib.FilasNucleo);
                    }
                    finally { HazLib.Cerrar(); }
                }
            }
            catch { }
            return false;
        }
    }

    // =====================================================================
    //  JefeMiradaProj — LA MIRADA DEL DIOS (el ataque MEDIO RARO)
    // =====================================================================
    public class JefeMiradaProj : ModProjectile
    {
        /// <summary>LA CARGA: el ojo se abre (ticks).</summary>
        private const float Carga = 36f;

        /// <summary>EL RAYO (ticks).</summary>
        private const float Rayo = 170f;

        /// <summary>El largo máximo del rayo del dios.</summary>
        private const float LargoMax = 3000f;

        private float Tiempo { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Dueño => Projectile.ai[1];

        /// <summary>EL LARGO vivo (localAI[0]).</summary>
        private float Largo { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            Projectile.hide = true;             // se esconde…
        }

        // …y dibuja SOBRE los jugadores (la ley de la Mirada).
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
            => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC jefe = Main.npc[(int)Dueño];
            if (jefe == null || !jefe.active || jefe.life <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = jefe.Center;
            Projectile.timeLeft = 10;

            if (Tiempo < Carga)
            {
                // === EL OJO SE ABRE: la mira nace apuntando a la presa. ===
                Player presa = Main.player[jefe.target];
                if (presa != null && presa.active && !presa.dead)
                    Projectile.rotation = Projectile.AngleTo(presa.Center);

                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    // EL POLVO CONVERGENTE del ojo que se abre — fucsia y
                    // dorado, los colores del libro.
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(140f, 140f),
                        Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                        Vector2.Zero, 0,
                        Main.rand.NextBool() ? new Color(255, 200, 80) : new Color(240, 0, 192), 0.9f);
                    Vector2 hacia = Projectile.Center - d.position;
                    if (hacia.LengthSquared() > 1f) d.velocity = hacia / hacia.Length() * 9f;
                    d.noGravity = true;
                }
            }
            else
            {
                // === EL RAYO: crece +120/t hasta 3.000 y BARRE hacia la
                //     presa (lerp 0.06 — se esquiva, pero castiga). ===
                Largo = MathF.Min(Largo + 120f, LargoMax);
                Player presa = Main.player[jefe.target];
                if (presa != null && presa.active && !presa.dead)
                {
                    float objetivo = Projectile.AngleTo(presa.Center);
                    float delta = MathHelper.WrapAngle(objetivo - Projectile.rotation);
                    Projectile.rotation += delta * 0.06f;
                    if (Math.Abs(delta) > 0.0005f) Projectile.netUpdate = true;
                }

                // LAS LEYES DEL ARMA DE LA CASA leen el presente del dios.
                MiradaRayoProj.RefrescarPara(Largo, Tiempo - Carga);

                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    // LA LUZ FUCSIA del iris del libro (3 puntos).
                    Vector3 luz = new Vector3(0.92f, 0.08f, 0.78f);
                    Vector2 dir = Projectile.rotation.ToRotationVector2();
                    Lighting.AddLight(Projectile.Center, luz);
                    Lighting.AddLight(Projectile.Center + dir * (Largo * 0.5f), luz * 0.8f);
                    Lighting.AddLight(Projectile.Center + dir * Largo, luz * 0.6f);
                }
            }

            Tiempo++;
            if (Tiempo > Carga + Rayo) Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Tiempo <= Carga) return false;
            MiradaRayoProj.RefrescarPara(Largo, Tiempo - Carga);
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 a = Projectile.Center + dir * 70f;
            Vector2 b = Projectile.Center + dir * (Largo * 0.95f);
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, MiradaRayoProj.SemianchoDeCasa(0.25f) * 1.8f, ref punto);
        }

        // === EL RENDER: las leyes DE LA CASA (las del arma, sin tocar una
        //     coma — la letra: «darle la mirada del eterno») + el muzzle. ===
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                NPC jefe = Main.npc[(int)Dueño];
                if (jefe == null || !jefe.active) return false;

                if (Tiempo >= Carga && Largo > 10f)
                {
                    MiradaRayoProj.RefrescarPara(Largo, Tiempo - Carga);

                    Vector2 dir = Projectile.rotation.ToRotationVector2();
                    Vector2 raiz = Projectile.Center - Main.screenPosition;
                    Vector2 punta = raiz + dir * Largo;
                    Vector2 boca = raiz - dir * 10f;

                    HazLib.Abrir();
                    try
                    {
                        // CAPA 1 — EL VELO: la cúpula morada de la casa.
                        HazLib.PrepararPar(boca, punta, MiradaRayoProj.LeySemianchoVelo, 0f, 44);
                        HazLib.Dibujar(MiradaRayoProj.LeyTinteVelo, MiradaRayoProj.LeyVerticeVelo, HazLib.FilasVelo);

                        // CAPA 2 — EL CUERPO: la espina fucsia del libro.
                        HazLib.PrepararPar(boca, punta, MiradaRayoProj.LeySemianchoCuerpo, 0f, 72);
                        HazLib.Dibujar(MiradaRayoProj.LeyTinteCuerpo, MiradaRayoProj.LeyVerticeCuerpo, HazLib.FilasNucleo);
                    }
                    finally { HazLib.Cerrar(); }
                }

                // === EL OJO / MUZZLE: el flare estelar fucsia (la carga es
                //     el ojo que se abre; el rayo, el iris encendido). ===
                if (RecursosMirada.AsegurarCargado())
                {
                    Texture2D flare = RecursosMirada.Flare;
                    Texture2D glow = RecursosMirada.Glow;
                    if (flare != null && glow != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            float r = Tiempo < Carga ? Tiempo / Carga : 1f;
                            float latido = 0.5f + 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * 85f);
                            float brillo = (Tiempo < Carga
                                ? r * r * (1f - MathHelper.Clamp((r - 1f) / -0.1f, 0f, 1f))
                                : MathHelper.Clamp((Carga + Rayo - Tiempo) / 20f, 0f, 1f))
                                * MathHelper.Lerp(1f, 1.2f, latido) * 0.5f;
                            Color fogonazo = new Color(240, 0, 192, 0);
                            Vector2 pos = Projectile.Center - Main.screenPosition;
                            Vector2 ojoFlare = flare.Size() * 0.5f;
                            Vector2 ojoGlow = glow.Size() * 0.5f;
                            for (int i = 0; i < 3; i++)
                                Main.spriteBatch.Draw(flare, pos, null, fogonazo,
                                    Projectile.rotation * (Tiempo < Carga ? 2f : 0f), ojoFlare,
                                    brillo * 2f, SpriteEffects.None, 0f);
                            for (int i = 0; i < 2; i++)
                                Main.spriteBatch.Draw(glow, pos, null, fogonazo,
                                    0f, ojoGlow, brillo * 2f, SpriteEffects.None, 0f);
                        }
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
