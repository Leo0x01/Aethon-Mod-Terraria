using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Clicker
{
    /// <summary>
    /// ClickerGolpeProjectile — v6.82 — EL GOLPE DEL CLIC (la casa).
    ///
    /// EL CORAZÓN DE LA CLASE CLICKER de la casa (la investigación del
    /// Clicker Class real): el clic ES el uso del arma — y ESTE
    /// proyectil es el golpe que cae DONDE clickeas (Main.MouseWorld):
    /// hitbox 30×30, atraviesa TODO, vive 10 ticks — el pipeline de
    /// vanilla lo sincroniza solo en MP.
    ///
    /// LOS TRES ESTILOS (ai[0]):
    ///   0 · EL CLIC — el golpe normal: el PING del anillo (la firma
    ///      del Clicker Class: partículas en círculo expandiéndose del
    ///      punto del clic) + su núcleo de luz.
    ///   1 · LA CONSTELACIÓN (el Compás Rúnico, cada 8 clics): 5 rayos
    ///      de StormLib convergiendo en el punto desde las estrellas
    ///      vecinas + el destello — el cielo entero responde al combo.
    ///   2 · LA NOVA (la Furia Solar, cada 12 clics): la flor de fuego
    ///      de PyraLib.Estallido + el pulso de onda + el flash — el
    ///      estallido solar del combo, con golpe ×3 y hitbox ancha.
    ///
    /// EL DAÑO: cada golpe pega el daño del arma (los especiales, el
    /// múltiplo que el arma pasa al nacer). Todo lo demás es VISUAL: el
    /// ping, los rayos y la nova viven en el PreDraw (el lote aditivo de
    /// la casa — cero sprites propios, cero estado).
    /// </summary>
    public class ClickerGolpeProjectile : ModProjectile
    {
        /// <summary>Estilo 0: el clic normal.</summary>
        public const byte EstiloClic = 0;

        /// <summary>Estilo 1: LA CONSTELACIÓN (5 rayos convergentes).</summary>
        public const byte EstiloConstelacion = 1;

        /// <summary>Estilo 2: LA NOVA (la flor de fuego + flash).</summary>
        public const byte EstiloNova = 2;

        /// <summary>La vida total del golpe (la línea de tiempo de los VFX).</summary>
        private const int Vida = 10;

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
            Projectile.timeLeft = Vida;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Vida;
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>El estilo del golpe (viaja en ai[0]).</summary>
        private byte Estilo => (byte)Projectile.ai[0];

        /// <summary>La semilla determinista del golpe (viaja en ai[1]).</summary>
        private int Semilla => (int)Projectile.ai[1];

        public override void AI()
        {
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
                }
            }

            // LA LUZ del punto del clic (la mancha de luz que lee el mundo).
            if (Main.netMode != NetmodeID.Server)
            {
                Color c = ColorDelEstilo(1f);
                float k = 1f - Projectile.timeLeft / (float)Vida;   // 0→1
                float intensidad = (1f - k) * (Estilo == EstiloClic ? 0.8f : 1.6f);
                Lighting.AddLight(Projectile.Center,
                    c.R / 255f * intensidad, c.G / 255f * intensidad, c.B / 255f * intensidad);
            }
        }

        /// <summary>El color del estilo (la paleta del arma que lo llamó).</summary>
        private Color ColorDelEstilo(float brillo)
        {
            return Estilo switch
            {
                EstiloConstelacion => new Color(140, 225, 255) * brillo,
                EstiloNova => new Color(255, 190, 90) * brillo,
                _ => new Color(200, 235, 255) * brillo,
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;

            float edad = 1f - Projectile.timeLeft / (float)Vida;    // 0→1
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
    }
}
