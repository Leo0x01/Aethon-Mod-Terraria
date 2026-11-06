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

        /// <summary>v6.104 — EL RADIO DE LA BOCA (donde nace cada
        /// renglón): 92 → 230 — LA LETRA: «el ataque el renglon final
        /// para el jefe es ligeramente diferente ya que los hazes estan
        /// MAS SEPARADOS y son 6 hazes» — el aro despejado alrededor
        /// del dios se ABRE: los seis renglones nacen LEJOS del cuerpo
        /// (el hueco de la rueda se lee y se puede habitar).</summary>
        private const float RadioBoca = 230f;

        /// <summary>El radio de la PUNTA (hasta donde escribe).
        /// v6.104 — 780 → 880: la rueda crece con la separación.</summary>
        private const float RadioPunta = 880f;

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
    //  v6.104 — EL MODO SINCRONIZADO (ai[2] > 0): cuando el dios mezcla
    //  EL RENGLÓN FINAL con LA MIRADA (la letra: «a veces en un 50% de
    //  las veces se mezcla con la mirada del eterno teniendo los dos el
    //  mismo sentido horario al usarse juntos»), el rayo NO persigue:
    //  GIRA a la MISMA velocidad angular de la rueda (ai[2] = rad/t del
    //  renglón), en el MISMO sentido horario — el ojo y la rueda giran
    //  como UNA sola máquina de escribir.
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

        /// <summary>v6.104 — LA VELOCIDAD SINCRONIZADA (ai[2]): 0 = el
        /// modo de siempre (el rayo BARRE persiguiendo a la presa);
        /// > 0 = el rayo GIRA a esa velocidad (rad/t, sentido horario —
        /// la misma de la rueda del Renglón Final: LA MEZCLA).</summary>
        private float VelSync => Projectile.ai[2];

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

                if (VelSync > 0f)
                {
                    // v6.104 — LA MEZCLA (la letra: «teniendo los dos el
                    // mismo sentido horario al usarse juntos»): el rayo
                    // GIRA con la rueda — la MISMA velocidad angular, el
                    // MISMO sentido: el ojo es el séptimo renglón.
                    Projectile.rotation += VelSync;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Player presa = Main.player[jefe.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        float objetivo = Projectile.AngleTo(presa.Center);
                        float delta = MathHelper.WrapAngle(objetivo - Projectile.rotation);
                        Projectile.rotation += delta * 0.06f;
                        if (Math.Abs(delta) > 0.0005f) Projectile.netUpdate = true;
                    }
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

    // =====================================================================
    //  v6.104 — JefeMiradaCieloProj — LA MIRADA QUE CAE DEL CIELO (la
    //  letra del usuario: «tambien usa en la fase 2 la mirada del eterno
    //  pero como hacez que caen del cielo y que sean 6 en vertical y 6
    //  en horizontal, luego se mueven en diagonal y vuelven a ser
    //  lanzado»): LA REJILLA DEL OJO — DOCE haces de La Mirada del
    //  Eterno (sus leyes de la casa, su fucsia del iris del libro) que
    //  CAEN DEL CIELO sobre la arena: SEIS VERTICALES y SEIS
    //  HORIZONTALES. La coreografía de la letra, en CINCO actos:
    //    · LA CAÍDA (telegrafo, 0-70): los doce haces descienden del
    //      cielo como TELEGRAFOS translúcidos que crecen — se LEEN
    //      antes de arder.
    //    · EL PRIMER FUEGO (70-130): los doces haces SE ENCIENDEN a
    //      plena potencia (la rejilla arde entera).
    //    · LA DIAGONAL (130-220): TODA la rejilla GIRA 45° y SE
    //      DESLIZA en diagonal por la arena (translúcida otra vez —
    //      el viaje no mata: AVISA).
    //    · EL RELANZAMIENTO (220-280): «vuelven a ser lanzado» — los
    //      doce haces vuelven a ARDER a plena potencia EN DIAGONAL.
    //    · LA DISOLUCIÓN (280-320): la rejilla se apaga.
    //  TODO HazLib (la librería de haces de la casa): el filo rgb=0
    //  en cada vértice — NADA de texturas planas ni rectángulos.
    // =====================================================================
    public class JefeMiradaCieloProj : ModProjectile
    {
        // === LOS ACTOS (ticks) ===
        private const float T_Caida = 70f;
        private const float T_Fuego1 = 130f;
        private const float T_Diagonal = 220f;
        private const float T_Fuego2 = 280f;
        private const float T_Fin = 320f;

        /// <summary>EL SEPARACIÓN entre verticales (px).</summary>
        private const float SepX = 190f;

        /// <summary>EL SEPARACIÓN entre horizontales (px).</summary>
        private const float SepY = 150f;

        /// <summary>EL LARGO de cada haz vertical (px).</summary>
        private const float LargoV = 1500f;

        /// <summary>EL LARGO de cada haz horizontal (px).</summary>
        private const float LargoH = 1700f;

        private float Tiempo { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Dueño => Projectile.ai[1];

        /// <summary>EL CENTRO de la rejilla (localAI[0]/[1]): donde estaba
        /// la presa al nacer — la rejilla NO persigue: se LEÉ.</summary>
        private float CentroX { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }
        private float CentroY { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; }

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
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>La POTENCIA del haz según el acto: 0 en la caída/diagonal
        /// (el telegrafo translúcido), 1 en los fuegos.</summary>
        private float Potencia()
        {
            if (Tiempo < T_Caida) return 0f;
            if (Tiempo < T_Fuego1) return 1f;
            if (Tiempo < T_Diagonal) return 0.30f;   // la diagonal AVISA viajando
            if (Tiempo < T_Fuego2) return 1f;
            return MathHelper.Clamp(1f - (Tiempo - T_Fuego2) / 40f, 0f, 1f);
        }

        /// <summary>EL ÁNGULO de la rejilla: 0 en la caída y el primer
        /// fuego; 45° (π/4) en la diagonal y el relanzamiento — el giro
        /// ES la transición (la rejilla ENROSCA en el acto tercero).</summary>
        private float AnguloRejilla()
        {
            if (Tiempo < T_Fuego1) return 0f;
            if (Tiempo < T_Diagonal)
                return MathHelper.Lerp(0f, MathHelper.PiOver4,
                    MathHelper.Clamp((Tiempo - T_Fuego1) / (T_Diagonal - T_Fuego1), 0f, 1f));
            return MathHelper.PiOver4;
        }

        /// <summary>LA DERIVA de la rejilla en la diagonal: TODO el
        /// enjambre se desliza en diagonal por la arena (la dirección del
        /// giro — abajo-derecha en pantalla: el rumbo de la letra).</summary>
        private Vector2 Deriva()
        {
            if (Tiempo < T_Fuego1 || Tiempo >= T_Fuego2) return Vector2.Zero;
            float f = MathHelper.Clamp((Tiempo - T_Fuego1) / (T_Fuego2 - T_Fuego1), 0f, 1f);
            // la deriva arranca suave tras el primer fuego y frena al llegar
            float k = MathF.Sin(f * MathHelper.Pi) * 1.6f;
            return new Vector2(1f, 0.65f) * k;
        }

        public override void AI()
        {
            NPC jefe = Main.npc[(int)Dueño];
            if (jefe == null || !jefe.active || jefe.life <= 0)
            {
                Projectile.Kill();
                return;
            }

            if (Tiempo == 0f)
            {
                // LA REJILLA NACE SOBRE LA PRESA (donde está — no persigue).
                Player presa = Main.player[jefe.target];
                Vector2 centro = presa != null && presa.active ? presa.Center : jefe.Center;
                CentroX = centro.X;
                CentroY = centro.Y;
                Projectile.Center = centro;
                Projectile.netUpdate = true;
            }

            Projectile.timeLeft = 10;
            Vector2 deriva = Deriva();
            Projectile.Center = new Vector2(CentroX, CentroY) + deriva * (Tiempo > T_Fuego1 ? Tiempo - T_Fuego1 : 0f);

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // LA LUZ FUCSIA de la rejilla (vive con la potencia).
                float k = 0.35f + 0.65f * Potencia();
                Vector3 luz = new Vector3(0.92f, 0.08f, 0.78f) * k;
                Lighting.AddLight(Projectile.Center, luz);
                Lighting.AddLight(Projectile.Center + new Vector2(SepX * 2.5f, 0f), luz * 0.7f);
                Lighting.AddLight(Projectile.Center - new Vector2(SepX * 2.5f, 0f), luz * 0.7f);

                // LAS CHISPAS de la caída: el polvo que trae cada haz del cielo.
                if (Tiempo < T_Caida && Tiempo % 4f == 0f)
                {
                    float ang = Main.rand.NextBool() ? 0f : MathHelper.PiOver2;
                    float f = Main.rand.NextFloat(0.15f, 0.85f);
                    Vector2 eje = ang.ToRotationVector2();
                    Vector2 pos = Projectile.Center + eje * (f - 0.5f) * (ang == 0f ? LargoH : LargoV)
                        + new Vector2(Main.rand.Next(-2, 3), Main.rand.Next(-2, 3)) * 60f;
                    Dust d = Dust.NewDustPerfect(pos,
                        Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                        new Vector2(0f, 4f), 0,
                        Main.rand.NextBool() ? new Color(255, 200, 80) : new Color(240, 0, 192), 0.9f);
                    d.noGravity = true;
                }
            }

            Tiempo++;
            if (Tiempo > T_Fin) Projectile.Kill();
        }

        /// <summary>LOS DOCE HACES: 6 verticales + 6 horizontales girando
        /// con la rejilla — cada uno su segmento A→B.</summary>
        private void Segmento(int i, out Vector2 a, out Vector2 b)
        {
            float giro = AnguloRejilla();
            bool vertical = i < 6;
            int k = vertical ? i : i - 6;
            float offset = (k - 2.5f) * (vertical ? SepX : SepY);
            float largo = vertical ? LargoV : LargoH;
            // LA REJILLA ENTERA gira: el eje del haz y su offset rotan JUNTOS.
            float ejeGirado = (vertical ? MathHelper.PiOver2 : 0f) + giro;
            Vector2 dirF = ejeGirado.ToRotationVector2();
            Vector2 perpF = new(-dirF.Y, dirF.X);
            Vector2 centroF = Projectile.Center + perpF * offset;
            a = centroF - dirF * (largo * 0.5f);
            b = centroF + dirF * (largo * 0.5f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float poder = Potencia();
            if (poder < 0.65f) return false;   // solo los FUEGOS matan
            MiradaRayoProj.RefrescarPara(1000f, Tiempo);
            float ancho = MiradaRayoProj.SemianchoDeCasa(0.25f) * 1.5f;
            float punto = 0f;
            for (int i = 0; i < 12; i++)
            {
                Segmento(i, out Vector2 a, out Vector2 b);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    a, b, ancho, ref punto))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                NPC jefe = Main.npc[(int)Dueño];
                if (jefe == null || !jefe.active) return false;

                float poder = Potencia();
                float nace = MathHelper.Clamp(Tiempo / T_Caida, 0f, 1f);   // la caída crece
                float alfaHaz = MathHelper.Max(poder, 0.22f + 0.20f * nace)
                    * MathHelper.Clamp((T_Fin - Tiempo) / 40f, 0f, 1f);
                if (alfaHaz > 0.02f)
                {
                    MiradaRayoProj.RefrescarPara(1000f, Tiempo);
                    HazLib.Abrir();
                    try
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Segmento(i, out Vector2 a, out Vector2 b);
                            a = a - Main.screenPosition;
                            b = b - Main.screenPosition;
                            // el haz crece de largo con la caída (nace del centro)
                            Vector2 m = (a + b) * 0.5f;
                            a = m + (a - m) * (0.25f + 0.75f * nace);
                            b = m + (b - m) * (0.25f + 0.75f * nace);

                            HazLib.PrepararPar(a, b, MiradaRayoProj.LeySemianchoVelo, 0f, 30);
                            HazLib.Dibujar(MiradaRayoProj.LeyTinteVelo, MiradaRayoProj.LeyVerticeVelo, HazLib.FilasVelo);

                            HazLib.PrepararPar(a, b, MiradaRayoProj.LeySemianchoCuerpo, 0f, 44);
                            HazLib.Dibujar(MiradaRayoProj.LeyTinteCuerpo, MiradaRayoProj.LeyVerticeCuerpo, HazLib.FilasNucleo);
                        }
                    }
                    finally { HazLib.Cerrar(); }
                }

                // LAS DOCE MARCAS del telegrafo: el destello fucsia en el
                // CENTRO de cada haz (la cuenta de la rejilla).
                if (RecursosMirada.AsegurarCargado())
                {
                    Texture2D flare = RecursosMirada.Flare;
                    if (flare != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            float pulso = 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
                            for (int i = 0; i < 12; i++)
                            {
                                Segmento(i, out Vector2 a, out Vector2 b);
                                Vector2 centroHaz = ((a + b) * 0.5f) - Main.screenPosition;
                                Color c = new Color(240, 0, 192, 0) * (alfaHaz * 0.7f * pulso);
                                Main.spriteBatch.Draw(flare, centroHaz, null, c,
                                    Main.GlobalTimeWrappedHourly * 0.8f, flare.Size() * 0.5f,
                                    0.35f + 0.25f * poder, SpriteEffects.None, 0f);
                            }
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

    // =====================================================================
    //  v6.104 — JefePruebaCProj — EL RAYO DE PRUEBA C DEL DIOS (la
    //  letra: «para la fase 3 se usara el rayo de prueba C un ataque que
    //  se usara siempre que se lancen al menos 2 veces los ataques
    //  anteriores en la fase 3»): la firma del ARMA DEL SUEÑO hecha
    //  ataque del dios — LA CARGA DE LOS SEIS SOLES (los destellos de 4
    //  puntas naciendo LEJOS — 320..520 px — y CAYENDO hacia la boca
    //  del dios: la implosión del conjuro) y EL CONDUCTO ONÍRICO: la
    //  pila del Prueba C adaptada a HazLib — EL DOBLE GLOW (dos velos
    //  envolventes, ×2.6 y ×1.9 — «EL DESENFOQUE MÁXIMO»), EL NÚCLEO
    //  SUAVE (×0.28 — la vena blanca fina) y LA AGUJA (la punta que se
    //  afila a ×0.06) con LA ESTRANGULACIÓN final (TODO el conducto
    //  colapsa antes de morir — «el haz se cierra como una boca»).
    // =====================================================================
    public class JefePruebaCProj : ModProjectile
    {
        /// <summary>LA CARGA de los seis soles (ticks).</summary>
        private const float Carga = 110f;

        /// <summary>EL LÁSER (ticks).</summary>
        private const float Laser = 300f;

        /// <summary>El largo del cono.</summary>
        private const float LargoCono = 2800f;

        private float Tiempo { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Dueño => Projectile.ai[1];

        /// <summary>LA MIRA (localAI[0]).</summary>
        private float Mira { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        // === EL ESTADO DE LAS LEYES (cero-GC) ===
        private static float _t;
        private static float _tamPrueba;

        private void RefrescarLeyes() => _t = Tiempo;

        private static readonly HazLib.LeyNodo LeyTintePrueba = TintePrueba;
        private static readonly HazLib.LeyVertice LeyVerticePrueba = VerticePrueba;

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

            // === LA MIRA: persigue MÁS VIVO que el RC2 (0.035 — el sueño
            //     es inquieto) pero se apaga al final del conducto. ===
            Player presa = Main.player[jefe.target];
            if (presa != null && presa.active && !presa.dead && Tiempo < Carga + Laser * 0.7f)
            {
                float objetivo = Projectile.AngleTo(presa.Center);
                float delta = MathHelper.WrapAngle(objetivo - Mira);
                Mira += delta * 0.035f;
                if (Math.Abs(delta) > 0.0005f) Projectile.netUpdate = true;
            }

            // === EL BARRIDO: la media onda del RC2 (±1.3 rad — un pelo
            //     más corta: el conducto onírico barre MÁS DEPRISA). ===
            float barrido = MathF.Sin((float)((Tiempo - Carga) / Laser) * MathHelper.Pi)
                * 1.3f * MathHelper.Clamp((Tiempo - Carga) / 26f, 0f, 1f);
            Projectile.rotation = Mira + (Tiempo < Carga ? 0f : barrido);

            // === EL TAMAÑO + LA ESTRANGULACIÓN (la firma del arma-C:
            //     al final TODO el conducto colapsa a ×0.07 antes de
            //     apagarse — «se cierra como una boca y muere»). ===
            float objetivoTam = MathF.Pow(
                Utils.GetLerpValue(Carga - 12f, Carga + 40f, Tiempo, true) *
                Utils.GetLerpValue(Carga + Laser + 40f, Carga + Laser, Tiempo, true), 3f);
            if (Tiempo > Carga + Laser - 26f)
                objetivoTam *= MathHelper.Clamp((Carga + Laser + 20f - Tiempo) / 45f, 0.07f, 1f);
            _tamPrueba = MathHelper.Lerp(_tamPrueba, objetivoTam, 0.1f);
            RefrescarLeyes();

            if (!Main.dedServ)
            {
                if (Tiempo == 2f)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            new Terraria.Audio.SoundStyle(
                                "AethonMod/Content/Sounds/RayoConcentradoCarga",
                                Terraria.Audio.SoundType.Sound)
                            { MaxInstances = 1, Volume = 0.9f, Pitch = 0.05f },
                            Projectile.Center);
                    }
                    catch { }
                }
                if (Tiempo > Carga && Tiempo < Carga + Laser)
                {
                    Pantalla.Sacudir(4.5f, 0.35f, "JefePruebaC-sostenido");
                }
                if (Tiempo == Carga + 1f)
                {
                    Pantalla.Sacudir(8f, 0.7f, "JefePruebaC-encendido");
                    OndaLib.Kick(6f, 12);
                }

                float vol = 1.0f * _tamPrueba;
                _loop ??= new SonidoEnLoop(
                    new Terraria.Audio.SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop")
                        { MaxInstances = 0 },
                    new Terraria.Audio.ProjectileAudioTracker(Projectile).IsActiveAndInGame);
                _loop.PlaySound(() => Projectile.Center, () => vol, () => -0.1f);
            }

            // LA LUZ del conducto: la paleta del conjuro en 3 puntos.
            if (!Main.dedServ && Tiempo > Carga - 10f)
            {
                Color cLuz = RayoConcentradoDosProjectile.ColorConjuro(Tiempo);
                float k = Math.Max(_tamPrueba, 0.15f);
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Lighting.AddLight(Projectile.Center,
                    cLuz.R / 255f * 1.6f * k, cLuz.G / 255f * 1.5f * k, cLuz.B / 255f * 1.4f * k);
                Lighting.AddLight(Projectile.Center + dir * 900f,
                    cLuz.R / 255f * 1.2f * k, cLuz.G / 255f * 1.1f * k, cLuz.B / 255f * 1.0f * k);
                Lighting.AddLight(Projectile.Center + dir * 1700f,
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
                    LargoCono * (0.55f + _tamPrueba * 0.45f),
                    Projectile.rotation, 0.18f * Math.Max(_tamPrueba, 0.02f));
            }
            return false;
        }

        // === LAS LEYES DEL ASPECTO — la pila del ARMA-C (el sueño) ===

        /// <summary>EL TINTE: la PALETA DEL CONJURO viajando (la del
        /// RC2/arma-C de la casa) con EL NACIMIENTO/MUERTE suaves.</summary>
        private static Color TintePrueba(float p)
        {
            Color c = RayoConcentradoDosProjectile
                .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + p * 48f);
            float nace = MathHelper.Clamp(p / 0.04f, 0f, 1f);
            // LA AGUJA: la punta se afila a ×0.06 (la letra del arma-C)
            float muere = MathHelper.Clamp((1f - p) / 0.08f, 0f, 1f);
            return c * (nace * muere * (0.72f + 0.28f * _tamPrueba));
        }

        /// <summary>EL VÉRTICE: el grumo de ruido diagonal MÁS SUAVE que
        /// el RC2 (el sueño está DESENFOCADO — el ruido al 60 %) y el
        /// brillo interior fino (×0.28 de la vena del arma-C).</summary>
        private static Color VerticePrueba(float x, float y, Color tinte)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float ruido = Frac(x * 2f - t * 2.4f) * Frac(y * 2f + x * 4f - t * 2.4f);
            float opacidad = ruido * 0.45f + 0.50f;

            float d = MathF.Abs(y - 0.5f);
            float brillo = d <= 0.0001f ? 1f : MathHelper.Clamp(0.26f / d, 0f, 1f);
            brillo *= 1f - MathHelper.Clamp((d - 0.06f) / 0.20f, 0f, 1f);

            Vector4 suma = tinte.ToVector4() * opacidad + new Vector4(brillo * 0.85f);
            return new Color(
                (byte)Math.Clamp((int)(suma.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Z * 255f + 0.5f), 0, 255), 255);
        }

        private static float Frac(float x) => x - MathF.Floor(x);

        // === EL RENDER: la carga de los SEIS SOLES + el conducto onírico ===
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                NPC jefe = Main.npc[(int)Dueño];
                if (jefe == null || !jefe.active) return false;
                RefrescarLeyes();

                Vector2 centro = Projectile.Center - Main.screenPosition;

                // === LA CARGA DE LOS SEIS SOLES (la firma del arma-C:
                //     nacen LEJOS — 320..520 px — y CAEN hacia la boca
                //     del dios, cayendo rectos, sin dar vueltas). ===
                if (Tiempo < Carga + 10f)
                {
                    Texture2D destello = DefinitivasLib.TexStar;
                    if (destello != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            float r = MathHelper.Clamp(Tiempo / Carga, 0f, 1f);
                            float caida = MathF.Pow(r, 0.8f);          // el sol cae
                            for (int i = 0; i < 6; i++)
                            {
                                float ang = i * MathHelper.TwoPi / 6f + r * 0.9f;
                                float radio = MathHelper.Lerp(
                                    320f + 200f * ((i * 0.37f) % 1f), 40f, caida);
                                Vector2 pos = centro + ang.ToRotationVector2() * radio;
                                Color c = RayoConcentradoDosProjectile
                                    .ColorConjuro(Tiempo * 1.4f + i * 5f);
                                Main.spriteBatch.Draw(destello, pos, null,
                                    c * (0.35f + 0.60f * r), ang + MathHelper.PiOver2,
                                    destello.Size() * 0.5f,
                                    (0.5f + 0.3f * r) * (1f - caida * 0.4f),
                                    SpriteEffects.None, 0f);
                            }
                        }
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }

                // === EL CONDUCTO ONÍRICO: LA PILA DEL ARMA-C sobre
                //     HazLib — EL DOBLE GLOW (dos velos: ×2.6 y ×1.9 —
                //     el desenfoque máximo) y EL NÚCLEO SUAVE (×0.28 —
                //     la vena fina del sueño). TRES capas, UN draw
                //     call. ===
                if (Tiempo > Carga - 8f && _tamPrueba > 0.01f)
                {
                    Vector2 dir = Projectile.rotation.ToRotationVector2();
                    Vector2 boca = centro + dir * 46f;
                    Vector2 punta = centro + dir * (LargoCono * (0.55f + _tamPrueba * 0.45f));

                    HazLib.Abrir();
                    try
                    {
                        // (1) EL GLOW EXTERIOR — la bruma del sueño (×2.6).
                        HazLib.PrepararCono(boca, punta, 60f * _tamPrueba + 5f, 210f * _tamPrueba + 10f, 0f, 48);
                        HazLib.Dibujar(p => RayoConcentradoDosProjectile
                            .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + p * 48f + 20f)
                            * (MathHelper.Clamp(p / 0.05f, 0f, 1f)
                               * MathHelper.Clamp((1f - p) / 0.12f, 0f, 1f)) * 0.16f,
                            null, HazLib.FilasVelo);

                        // (2) EL GLOW INTERIOR — la envolvente (×1.9).
                        HazLib.PrepararCono(boca, punta, 42f * _tamPrueba + 4f, 150f * _tamPrueba + 8f, 0f, 52);
                        HazLib.Dibujar(p => RayoConcentradoDosProjectile
                            .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + p * 48f + 10f)
                            * (MathHelper.Clamp(p / 0.05f, 0f, 1f)
                               * MathHelper.Clamp((1f - p) / 0.10f, 0f, 1f)) * 0.30f,
                            null, HazLib.FilasVelo);

                        // (3) EL NÚCLEO SUAVE — la vena del arma-C (×0.28).
                        HazLib.PrepararCono(boca, punta, 26f * _tamPrueba + 3f, 88f * _tamPrueba + 5f, 0f, 56);
                        HazLib.Dibujar(LeyTintePrueba, LeyVerticePrueba, HazLib.FilasNucleo);
                    }
                    finally { HazLib.Cerrar(); }

                    // (4) LOS SEIS SOLES CONDUCTO — los destellos de 4
                    //     puntas corriendo por las MÁRGENES del conducto
                    //     hacia la punta (la firma del arma-C).
                    Texture2D destello2 = DefinitivasLib.TexStar;
                    if (destello2 != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            float anchoBorde = 60f * _tamPrueba;
                            for (int i = 0; i < 6; i++)
                            {
                                float fase = (Main.GlobalTimeWrappedHourly * 0.55f + i * 0.167f) % 1f;
                                float p = 0.08f + 0.88f * fase;
                                float anchoP = 26f + 62f * MathF.Pow(p, 0.8f);
                                Vector2 posBorde = boca + dir * (punta - boca).Length() * p
                                    + new Vector2(-dir.Y, dir.X) * anchoBorde * ((i % 2 == 0) ? 1f : -1f);
                                Color c = RayoConcentradoDosProjectile
                                    .ColorConjuro(Main.GlobalTimeWrappedHourly * 60f + i * 12f);
                                float tam = (18f + 10f * (i % 3)) * _tamPrueba;
                                Main.spriteBatch.Draw(destello2, posBorde, null,
                                    c * (0.55f * _tamPrueba), 0f, destello2.Size() * 0.5f,
                                    tam / destello2.Width, SpriteEffects.None, 0f);
                            }
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
