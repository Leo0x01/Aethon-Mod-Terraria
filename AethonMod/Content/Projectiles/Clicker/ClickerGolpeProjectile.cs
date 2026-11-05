using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles.Clicker
{
    /// <summary>
    /// ClickerGolpeProjectile — v6.82 → v6.100 — EL GOLPE DEL CLIC.
    ///
    /// EL CORAZÓN DE LA CLASE CLICKER de la casa (la investigación del
    /// Clicker Class real): el clic ES el uso del arma — y ESTE
    /// proyectil es el golpe que cae DONDE clickeas (Main.MouseWorld):
    /// hitbox 30×30, atraviesa TODO — el pipeline de vanilla lo
    /// sincroniza solo en MP.
    ///
    /// LOS CINCO ESTILOS (ai[0]):
    ///   0 · EL CLIC — el golpe normal: el PING del anillo + su núcleo.
    ///   1 · LA CONSTELACIÓN (el Compás Rúnico, cada 8 clics): 5 rayos
    ///      de StormLib convergiendo + el destello.
    ///   2 · LA NOVA (la Furia Solar, cada 12 clics): la flor de fuego
    ///      de PyraLib + el pulso + el flash — golpe ×3.
    ///   3 · v6.100 — LA TINTA (el Sello del Escriba, cada 10 sellos):
    ///      EL NOVA ACTO del MOTOR DE PÍXELES (la puerta del taller)
    ///      estallando en el punto + el doble anillo violeta/teal —
    ///      golpe ×3 con 52 ticks de acto completo.
    ///   4 · v6.100 — EL OJO (el Parpadeo del Eterno, cada 14): LA
    ///      GARGANTA DEL VÓRTICE del motor de píxeles ATRAYENDO a los
    ///      vivos (succión 190 px) + los anillos contra-rotando —
    ///      golpe ×3 en área con succión.
    ///
    /// EL DAÑO: cada golpe pega el daño del arma (los especiales, el
    /// múltiplo que el arma pasa al nacer). Todo lo demás es VISUAL.
    /// </summary>
    public class ClickerGolpeProjectile : ModProjectile
    {
        /// <summary>Estilo 0: el clic normal.</summary>
        public const byte EstiloClic = 0;

        /// <summary>Estilo 1: LA CONSTELACIÓN (5 rayos convergentes).</summary>
        public const byte EstiloConstelacion = 1;

        /// <summary>Estilo 2: LA NOVA (la flor de fuego + flash).</summary>
        public const byte EstiloNova = 2;

        /// <summary>v6.100 — Estilo 3: LA TINTA (el NovaActo de píxeles).</summary>
        public const byte EstiloTinta = 3;

        /// <summary>v6.100 — Estilo 4: EL OJO (la GargantaVortice con succión).</summary>
        public const byte EstiloOjo = 4;

        /// <summary>La vida total del golpe según su estilo (la línea de
        /// tiempo de los VFX: los actos de píxeles viven su acto entero).</summary>
        private int VidaTotal => Estilo switch
        {
            EstiloTinta => 52,
            EstiloOjo => 46,
            _ => 10,
        };

        /// <summary>v6.100 — EL MOTOR de píxeles de los estilos especiales
        /// (la tinta: NovaActo · el ojo: GargantaVortice).</summary>
        private PixelFxPlayer _motor;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTotal;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = VidaTotal;
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>El estilo del golpe (viaja en ai[0]).</summary>
        private byte Estilo => (byte)Projectile.ai[0];

        /// <summary>La semilla determinista del golpe (viaja en ai[1]).</summary>
        private int Semilla => (int)Projectile.ai[1];

        public override void AI()
        {
            // v6.100 — LA VIDA del acto: los estilos de píxeles viven su
            // acto entero (52/46 ticks) — el tope por si vanilla le dio
            // más tiempo de vida del nacimiento.
            if (Projectile.timeLeft > VidaTotal)
                Projectile.timeLeft = VidaTotal;

            // EL NACIMIENTO: el ping de partículas en círculo (LA firma
            // del Clicker Class — 20 chispas radiales del punto clickeado)
            // y los JUICIOS de los especiales (el flash y el kick suave
            // nacen UNA vez, con el golpe).
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.netMode != NetmodeID.Server)
                {
                    int n = Estilo == EstiloClic ? 14 : 24;
                    for (int i = 0; i < n; i++)
                    {
                        float ang = i * MathHelper.TwoPi / n + Semilla * 0.13f;
                        Color c = ColorDelEstilo(0.9f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            Estilo == EstiloNova ? DustID.Crimson : DustID.Enchanted_Pink,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(1.8f, 3.4f),
                            180, c, 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }

                    // LOS ESPECIALES JUZGAN AL NACER: la nova prende el
                    // flash de pantalla + el temblor suave (la receta de
                    // cine de la casa — nada de kick seco).
                    if (Estilo == EstiloNova)
                    {
                        OndaLib.Flash(new Color(255, 230, 160), 0.16f, 8);
                        Pantalla.Sacudir(2.2f, 0.18f, "AethonClickerNova");
                    }
                    else if (Estilo == EstiloConstelacion)
                    {
                        OndaLib.Flash(new Color(170, 225, 255), 0.10f, 6);
                    }
                    // v6.100 — LA TINTA estalla (violeta) y EL OJO se abre
                    // (púrpura profundo, con el rugido grave del vórtice).
                    else if (Estilo == EstiloTinta)
                    {
                        OndaLib.Flash(new Color(190, 130, 250), 0.13f, 7);
                        Pantalla.Sacudir(1.8f, 0.16f, "AethonClickerTinta");
                    }
                    else if (Estilo == EstiloOjo)
                    {
                        OndaLib.Flash(new Color(120, 70, 200), 0.18f, 9);
                        Pantalla.Sacudir(2.6f, 0.22f, "AethonClickerOjo");
                        SoundStyle rugido = SoundID.Item122.WithPitchOffset(-0.5f);
                        SoundEngine.PlaySound(in rugido, Projectile.Center);
                    }
                }
            }

            // v6.100 — LOS MOTORES DE PÍXELES laten su acto (solo cliente)
            // y se apagan suave en el fade final.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ &&
                (Estilo == EstiloTinta || Estilo == EstiloOjo))
            {
                Motor(Estilo).Tick();
                if (Projectile.timeLeft <= 10)
                    Motor(Estilo).Fx.Stop();
            }

            // v6.100 — LA SUCCIÓN DEL OJO: la garganta del vórtice ATRAE
            // a los vivos hacia su boca (190 px, server-side).
            if (Estilo == EstiloOjo && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float atraccion = 190f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy())
                        continue;
                    Vector2 rel = n.Center - Projectile.Center;
                    float dist = rel.Length();
                    if (dist > atraccion || dist < 8f)
                        continue;
                    n.velocity += -rel / dist * 0.35f;
                }
            }

            // LA LUZ del punto del clic (la mancha de luz que lee el mundo).
            if (Main.netMode != NetmodeID.Server && Estilo != EstiloTinta && Estilo != EstiloOjo)
            {
                Color c = ColorDelEstilo(1f);
                float k = 1f - Projectile.timeLeft / (float)VidaTotal;   // 0→1
                float intensidad = (1f - k) * (Estilo == EstiloClic ? 0.8f : 1.6f);
                Lighting.AddLight(Projectile.Center,
                    c.R / 255f * intensidad, c.G / 255f * intensidad, c.B / 255f * intensidad);
            }
        }

        public override void OnKill(int timeLeft)
        {
            _motor?.Dispose();
            _motor = null;
        }

        /// <summary>v6.100 — EL MOTOR de píxeles del estilo: la tinta es
        /// EL NOVA ACTO (el acto completo del taller) y el ojo es LA
        /// GARGANTA DEL VÓRTICE (el horizonte de sucesos).</summary>
        private PixelFxPlayer Motor(byte estilo)
        {
            if (_motor == null)
                _motor = estilo == EstiloTinta
                    ? new PixelFxPlayer(PixelFxCasas.NovaActo(Semilla,
                        PxColor.Rgb(170, 110, 240), PxColor.Rgb(70, 40, 110)), 2.2f)
                    : new PixelFxPlayer(PixelFxCasas.GargantaVortice(Semilla,
                        PxColor.Rgb(150, 90, 235), PxColor.Rgb(30, 18, 50)), 2.4f);
            return _motor;
        }

        /// <summary>El color del estilo (la paleta del arma que lo llamó).</summary>
        private Color ColorDelEstilo(float brillo)
        {
            return Estilo switch
            {
                EstiloConstelacion => new Color(140, 225, 255) * brillo,
                EstiloNova => new Color(255, 190, 90) * brillo,
                EstiloTinta => new Color(185, 120, 245) * brillo,
                EstiloOjo => new Color(150, 90, 235) * brillo,
                _ => new Color(200, 235, 255) * brillo,
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;

            float edad = 1f - Projectile.timeLeft / (float)VidaTotal;    // 0→1
            // LAS COORDENADAS DE LA CASA: el lote aditivo abre con la
            // GameViewMatrix (zoom, sin traslación de pantalla) — las
            // posiciones viajan en coords de PANTALLA (el patrón de TODOS
            // los renderers del mod).
            Vector2 centro = Projectile.Center - Main.screenPosition;
            float t = Main.GlobalTimeWrappedHourly;

            // EL BAILE DEL LOTE de la casa: cerrar el del pase de
            // proyectiles, abrir el aditivo del mundo, devolver limpio.
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.AbrirAdditive();
                try
                {
                    if (Estilo == EstiloConstelacion)
                        DibujarConstelacion(centro, edad, t);
                    else if (Estilo == EstiloNova)
                        DibujarNova(centro, edad, t);
                    else if (Estilo == EstiloTinta)
                        DibujarTinta(centro, edad, t);
                    else if (Estilo == EstiloOjo)
                        DibujarOjo(centro, edad, t);
                    else
                        DibujarClic(centro, edad, t);
                }
                finally
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ------------------------------------------------------------------
        //  ESTILO 0 — EL CLIC: el ping del anillo + el núcleo
        // ------------------------------------------------------------------

        private static void DibujarClic(Vector2 centro, float edad, float t)
        {
            // EL PING: el anillo que nace con el clic y se disuelve.
            float radio = 14f + edad * 30f;
            VFXCore.RingQuad(centro, radio, edad * 1.2f,
                VFXCore.TintAlfa(new Color(200, 235, 255), 0.55f * (1f - edad)));

            // EL NÚCLEO: la chispa del clic muriendo rápido.
            VFXCore.Capsule(centro, 0f, (12f - edad * 9f), 0f,
                VFXCore.TintAlfa(new Color(235, 250, 255), 0.80f * (1f - edad)));
        }

        // ------------------------------------------------------------------
        //  ESTILO 1 — LA CONSTELACIÓN: 5 rayos de StormLib convergentes
        // ------------------------------------------------------------------

        private static void DibujarConstelacion(Vector2 centro, float edad, float t)
        {
            // CINCO ESTRELLAS VECINAS apuntan al clic: los rayos nacen en
            // un círculo de 150px y CONVERGEN — el cielo responde al combo.
            float parpadeo = 0.75f + 0.25f * MathF.Sin(t * 46f);
            float vida = (1f - edad) * (1f - edad * 0.4f);
            for (int i = 0; i < 5; i++)
            {
                float ang = i * MathHelper.TwoPi / 5f + 0.6f + i * 0.21f;
                float r = 150f * (0.75f + 0.25f * (i % 2));
                Vector2 estrella = centro + new Vector2(MathF.Cos(ang) * r, MathF.Sin(ang) * r);

                // EL RAYO (compacto: 6 segmentos — el caller barato de la
                // librería; el techo vive dentro de StormLib).
                StormLib.Bolt(Main.spriteBatch, estrella, centro,
                    700 + i * 31, (int)(t * 60f) % 7, 4.5f,
                    VFXCore.TintAlfa(new Color(110, 180, 255), 0.45f * vida),
                    VFXCore.TintAlfa(new Color(225, 245, 255), 0.85f * vida * parpadeo),
                    alpha: vida, segments: 6, amp: 14f);

                // LA ESTRELLA de donde nace el rayo (su perla en el cielo).
                Vector2 medio = new Vector2(7f, 7f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, estrella, null,
                    VFXCore.TintAlfa(new Color(180, 230, 255), 0.70f * vida), 0f,
                    new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                    medio / new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height),
                    SpriteEffects.None, 0f);
            }

            // EL OJO DE LA CONSTELACIÓN: el destello del punto convergido.
            VFXCore.RingQuad(centro, 20f + edad * 55f, edad * 2.0f,
                VFXCore.TintAlfa(new Color(160, 230, 255), 0.50f * (1f - edad)));
            VFXCore.Capsule(centro, 0f, (22f - edad * 15f), 0f,
                VFXCore.TintAlfa(new Color(240, 250, 255), 0.85f * (1f - edad)));
        }

        // ------------------------------------------------------------------
        //  ESTILO 2 — LA NOVA: la flor de fuego de PyraLib + el pulso
        // ------------------------------------------------------------------

        private static void DibujarNova(Vector2 centro, float edad, float t)
        {
            // LA FLOR DE FUEGO — la rampa solar de la casa (blanco → oro
            // → naranja → rojo profundo).
            Color[] rampa =
            {
                new(255, 250, 225),
                new(255, 210, 110),
                new(255, 150, 60),
                new(190, 60, 30),
            };
            PyraLib.Estallido(Main.spriteBatch, centro, 130f, edad, rampa,
                811, t, intensity: 1f - edad * 0.35f);

            // EL ANILLO DE CHOQUE que acompaña la flor.
            OndaLib.Pulse(Main.spriteBatch, centro, edad,
                60f + edad * 110f, new Color(255, 225, 150), 0.55f * (1f - edad), 811);

            // EL NÚCLEO cegador (los primeros ticks: la nova ES un sol).
            if (edad < 0.6f)
            {
                float corazon = 1f - edad / 0.6f;
                VFXCore.Capsule(centro, 0f, (30f - edad * 20f), 0f,
                    VFXCore.TintAlfa(new Color(255, 252, 240), 0.90f * corazon));
            }
        }

        // ------------------------------------------------------------------
        //  v6.100 — ESTILO 3 — LA TINTA: el NovaActo del motor de píxeles
        //  (el acto COMPLETO del taller estallando donde cayó el sello)
        //  + el doble anillo violeta/teal del contrato roto.
        // ------------------------------------------------------------------

        private void DibujarTinta(Vector2 centro, float edad, float t)
        {
            Main.spriteBatch.End();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                PixelFxPlayer motor = Motor(EstiloTinta);
                motor.Draw(Main.spriteBatch, centro
                    - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
            }
            catch { /* el motor jamás tumba el golpe */ }
            Main.spriteBatch.End();
            VFXCore.AbrirAdditive();

            // EL DOBLE ANILLO del estallido de tinta (violeta fuera, teal
            // contra-el-reloj dentro).
            VFXCore.RingQuad(centro, 30f + edad * 150f, edad * 1.4f,
                VFXCore.TintAlfa(new Color(185, 120, 245), 0.55f * (1f - edad)));
            VFXCore.RingQuad(centro, 18f + edad * 96f, -edad * 1.9f,
                VFXCore.TintAlfa(new Color(80, 230, 190), 0.35f * (1f - edad)));
        }

        // ------------------------------------------------------------------
        //  v6.100 — ESTILO 4 — EL OJO: la GargantaVortice del motor de
        //  píxeles (el horizonte de sucesos ATRAYENDO) + los anillos
        //  contra-rotando de la revelación.
        // ------------------------------------------------------------------

        private void DibujarOjo(Vector2 centro, float edad, float t)
        {
            Main.spriteBatch.End();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                PixelFxPlayer motor = Motor(EstiloOjo);
                motor.Draw(Main.spriteBatch, centro
                    - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
            }
            catch { /* el motor jamás tumba el golpe */ }
            Main.spriteBatch.End();
            VFXCore.AbrirAdditive();

            // LA REVELACIÓN: el anillo que abre RÁPIDO y frena + el anillo
            // interior respirando (la pupila dilatándose).
            float abre = MathF.Min(1f, edad * 6f);
            VFXCore.RingQuad(centro, (14f + 150f * abre) * (1f - edad * 0.3f), t * 0.8f,
                VFXCore.TintAlfa(new Color(150, 90, 235), 0.5f * (1f - edad)));
            VFXCore.RingQuad(centro, (10f + 96f * abre) * (1f + 0.05f * MathF.Sin(t * 9f)),
                -t * 1.3f,
                VFXCore.TintAlfa(new Color(220, 190, 255), 0.3f * (1f - edad)));
        }
    }
}
