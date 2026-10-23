using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.51.4 → v6.51.5 — LOS PROYECTILES DIGNOS, LA SEGUNDA AUDICIÓN.
    //
    //  LA LETRA DE LA .51.5: «sobre El Equinoccio Binario y las otras
    //  armas, bueno, están feas no me gustan y no tienen estilo, ni
    //  efectos, ni brillos, ni glow, ni nada, las puedes borrar, en
    //  cambio deja el El Equinoccio Binario y modificalo, para El
    //  Equinoccio Binario usa el mismo proyectil que El Equinoccio,
    //  pero dale el efecto que El Equinoccio Binario // y crea otras
    //  armas en consecuencia que creas que se sean buenas ideas,
    //  recuerda meterle tantos efectos chulos como puedas, tienes
    //  todas las librerias a tu disposicion».
    //
    //  LA PURGA: LAS COMAS QUE MUERDEN, LA PÁGINA ARRANCADA, EL OJO
    //  QUE CAZA, LA LÍNEA DE LECTURA y EL MARCAPÁGINAS murieron (sus
    //  cinco proyectiles y sus cinco iconos). EL EQUINOCCIO BINARIO
    //  vive RE-HECHO: dispara la MISMA PAREJA 931 de El Equinoccio
    //  (el dorado con su sol · el violeta con su eclipse — EmperatrizFX
    //  con las marcas binarias 9211/9212) — de día el dorado estalla
    //  en MINI ECLIPSE en área; de noche el violeta PERFORA 4 y DRENA
    //  vida (ver DignosWeapons + EmperatrizFX).
    //
    //  LAS TRES IDEAS NUEVAS (con TODAS las librerías de la casa):
    //    · EL ÍNDICE DEL GRIMORIO — el dedo de hueso que planta LA
    //      CITA: una línea de glifos… y la cita DETONA en VALLA de
    //      rayos de Perlin.
    //    · LA LLUVIA DE VERSOS — la grieta de bruma con costura
    //      dorada que llueve versos… y al cerrarse, EL CAPÍTULO SE
    //      CIERRA: las dos tapas y los dientes de garra.
    //    · EL OJO QUE JUZGA — el ojo colosal con velo de cine que
    //      mira… y su VEREDICTO: el rayo de la mirada.
    //    · EL CORAZÓN DEL MAGNETAR (la mezcla pedida: «una mezcla de
    //      Bastón del magnetar y el colapso del magnetar»): primero
    //      vive LA ÓRBITA del Bastón (aura + cadenas de rayo) y al
    //      soltar vive EL COLAPSO del padre (lanzamiento exacto, freno
    //      tembloroso, STARQUAKE). Los DOS PADRES quedan INTACTOS.
    //
    //  Convención de la casa: el daño manual de área va por GOLPEMOTOR
    //  (el cauce del motor, no SimpleStrikeNPC), el homing busca con
    //  VFXCore.EsObjetivo, y TODO PreDraw firma el contrato del lote
    //  (CerrarLoteSiAbierto → Begin → dibujar → FlushAdditive →
    //  ReabrirLoteVanilla, coords de MUNDO, try/catch de contención).
    // ======================================================================

    /// <summary>El puente común de los dignos: la presa más cercana y
    /// la paleta de la familia.</summary>
    internal static class DignosGrimorio
    {
        /// <summary>El ORO de la binaria (el disco del día).</summary>
        internal static readonly Color OroBinaria = new Color(255, 214, 120);

        /// <summary>El VIOLETA de la binaria (el disco de la noche).</summary>
        internal static readonly Color VioletaBinaria = new Color(178, 108, 240);

        /// <summary>El ORO CÁLIDO de los glifos (la tinta viva del libro).</summary>
        internal static readonly Color OroGlifo = new Color(255, 232, 170);

        /// <summary>El HUESO de las agujas del índice (la punta que cita).</summary>
        internal static readonly Color HuesoCita = new Color(235, 226, 205);

        /// <summary>La tinta VIOLETA de la grieta (el interior del libro).</summary>
        internal static readonly Color TintaGrieta = new Color(64, 34, 98);

        /// <summary>La presa válida más cercana dentro del rango (null si no hay).</summary>
        internal static NPC PresaCercana(Vector2 centro, float rango)
        {
            NPC mejor = null;
            float mejorD = rango * rango;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = Vector2.DistanceSquared(n.Center, centro);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }
    }

    // ======================================================================
    //  v6.51.5 — EL TEATRO DE LOS DIGNOS: un solo proyectil cosmético
    //  con los modos de fuego que quedan VIVOS (el patrón
    //  BrumaNacimientoFX: cada cable la suya, netImportant = false, sin
    //  daño). Los dignos lo invocan desde sus golpes.
    //
    //    ai[0] = MODO: 1 ONDA DE ECLIPSE (binario día) · 2 DRENO DE
    //    LUNA (binario noche) · 5 EL PARPADEO (el destello del
    //    veredicto) · 8 EL CIERRE DEL CAPÍTULO (la lluvia de versos:
    //    las dos tapas y los dientes de garra).
    //    ai[1] = fuerza visual (0,7..1,4).
    // ======================================================================
    public class DignosFXProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;      // teatro: nunca daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 22;
            Projectile.netImportant = false;  // cosmético: cada cable la suya
        }

        /// <summary>LA APARICIÓN: nace el teatro donde el golpe lo pidió
        /// (nada en el servidor dedicado — es puro dibujo).</summary>
        public static void Nacer(Projectile origen, Vector2 donde, int modo, float fuerza = 1f)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            try
            {
                int vida = modo switch
                {
                    1 => 22,   // la onda del eclipse
                    2 => 16,   // el dreno de luna
                    5 => 12,   // el parpadeo / el veredicto
                    8 => 26,   // el cierre del capítulo
                    _ => 12,
                };
                Projectile fx = Projectile.NewProjectileDirect(
                    origen.GetSource_FromAI(), donde, Vector2.Zero,
                    ModContent.ProjectileType<DignosFXProjectile>(),
                    0, 0f, Main.myPlayer, modo, fuerza);
                fx.timeLeft = vida;
            }
            catch { }
        }

        public override void AI()
        {
            // LA LUZ DEL TEATRO (el parpadeo y el cierre encienden).
            if (Main.netMode == NetmodeID.Server) return;
            int modo = (int)Projectile.ai[0];
            if (modo == 5) Lighting.AddLight(Projectile.Center, 1.2f, 1.0f, 0.6f);
            else if (modo == 8) Lighting.AddLight(Projectile.Center, 0.5f, 0.36f, 0.16f);
            else if (modo == 1)
                Lighting.AddLight(Projectile.Center, 0.5f, 0.38f, 0.16f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                int modo = (int)Projectile.ai[0];
                float vidaMax = Math.Max(1f, Projectile.timeLeft + Edad());
                float fase = Edad() / vidaMax;                       // 0 → 1
                float apagon = 1f - fase;                            // se muere apagándose
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center;
                int semilla = Projectile.identity % 97 + 7;

                if (modo == 8)
                {
                    // ========================================================
                    //  8 — EL CIERRE DEL CAPÍTULO: LAS DOS TAPAS (la de
                    //  arriba cae · la de abajo sube a su encuentro) con
                    //  sus LOMOS dorados… y al encontrarse, LOS NUEVE
                    //  DIENTES DE GARRA (SombrasLib.Garra — la librería
                    //  de la casa) mordiendo el plano. El libro se cierra
                    //  de golpe y lo atrapado dentro ya está leído.
                    // ========================================================
                    float cierre = MathHelper.Clamp(Edad() / 5f, 0f, 1f);   // el portazo
                    float golpe = 1f - cierre;
                    float fuerza = MathHelper.Clamp(Projectile.ai[1], 0.7f, 1.4f);
                    float alto = 86f * fuerza;

                    Vector2 tapaSup = centro + new Vector2(0f, -golpe * alto - 6f);
                    Vector2 tapaInf = centro + new Vector2(0f, golpe * alto + 6f);

                    VFXCore.CerrarLoteSiAbierto();

                    // LAS DOS TAPAS (capa ALFA — el negro no vive en el
                    // aditivo): portada y contratapa con su lomo.
                    VFXCore.Begin();
                    try
                    {
                        VFXCore.Quad(tapaSup, new Color(30, 20, 46) * 0.94f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaSup + new Vector2(2f, 3f),
                            new Color(18, 12, 30) * 0.85f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaInf, new Color(30, 20, 46) * 0.94f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaInf + new Vector2(-2f, -3f),
                            new Color(18, 12, 30) * 0.85f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAlpha();

                    VFXCore.Begin();
                    try
                    {
                        // LOS DOS LOMOS dorados (la costura del capítulo).
                        VFXCore.Line(tapaSup + new Vector2(0f, -alto * 0.5f),
                            tapaSup + new Vector2(0f, alto * 0.5f),
                            DignosGrimorio.OroBinaria * 0.85f, 4f);
                        VFXCore.Line(tapaInf + new Vector2(0f, -alto * 0.5f),
                            tapaInf + new Vector2(0f, alto * 0.5f),
                            DignosGrimorio.OroBinaria * 0.85f, 4f);

                        // EL ARO DEL PORTAZO (la onda blanca del cierre).
                        float radio = 26f + 74f * fase;
                        VFXCore.Quad(centro, Color.White * 0.55f * apagon,
                            VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                        VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.28f * apagon,
                            new Vector2(140f * fuerza, 120f * fuerza));
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAdditive();

                    // LOS NUEVE DIENTES: garras de la casa mordiendo desde
                    // ambas tapas al cerrarse (5 arriba · 4 abajo — la
                    // dentadura asimétrica del libro).
                    try
                    {
                        for (int k = 0; k < 9; k++)
                        {
                            bool arriba = k < 5;
                            float f = (arriba ? k : k - 5) / (arriba ? 4f : 3f);
                            float x = (f - 0.5f) * 88f * fuerza;
                            Vector2 baseG = centro + new Vector2(
                                x, (arriba ? -1f : 1f) * (alto * 0.5f + 10f));
                            // LA MORIDA: los dientes CRECEN al cerrarse.
                            float largo = (34f + 26f * cierre) * fuerza;
                            SombrasLib.Garra(baseG,
                                (arriba ? Vector2.UnitY : -Vector2.UnitY) *
                                largo / 34f,
                                largo, 0.92f * apagon,
                                (k % 2 == 0 ? 0.34f : -0.34f));
                        }
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }

                    VFXCore.ReabrirLoteVanilla();
                    return false;
                }

                // === EL RESTO DE MODOS: un solo lote aditivo. ===
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    switch (modo)
                    {
                        case 1:
                        {
                            // 1 — LA ONDA DEL ECLIPSE (día): el aro dorado que
                            //     se traga la luz alrededor del golpe.
                            float radio = 16f + 78f * fase;
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.5f * apagon,
                                VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.4f * apagon,
                                new Vector2(64f, 64f));
                            VFXCore.Quad(centro, Color.White * 0.5f * apagon,
                                new Vector2(22f, 22f));
                            break;
                        }
                        case 2:
                        {
                            // 2 — EL DRENO DE LUNA (noche): tres volutas
                            //     violetas que suben del mordisco.
                            for (int k = 0; k < 3; k++)
                            {
                                Vector2 pos = centro + new Vector2(
                                    MathF.Sin(t * 3.1f + k * 2.2f) * 7f,
                                    -(Edad() * (1.1f + k * 0.35f)) - k * 4f);
                                VFXCore.Quad(pos, DignosGrimorio.VioletaBinaria * (0.45f * apagon),
                                    new Vector2(9f, 15f),
                                    MathF.Sin(t * 2.4f + k) * 0.5f, VFXCore.Pixel);
                            }
                            VFXCore.Quad(centro, DignosGrimorio.VioletaBinaria * 0.35f * apagon,
                                new Vector2(30f, 30f));
                            break;
                        }
                        default:
                        {
                            // 5 — EL PARPADEO / EL VEREDICTO: el destello
                            //     blanco gordo + el aro dorado en DOS QUADS
                            //     CRUZADOS alargados.
                            float destello = MathHelper.Clamp(1f - fase * 1.6f, 0f, 1f);
                            VFXCore.Quad(centro, Color.White * 0.85f * destello,
                                new Vector2(120f, 120f));
                            float brazo = 30f + 60f * fase;
                            Vector2 dirA = new Vector2(MathF.Cos(0.7f), MathF.Sin(0.7f));
                            Vector2 dirB = new Vector2(-dirA.Y, dirA.X);
                            VFXCore.Line(centro - dirA * brazo, centro + dirA * brazo,
                                DignosGrimorio.OroBinaria * 0.6f * apagon, 5f);
                            VFXCore.Line(centro - dirB * brazo, centro + dirB * brazo,
                                DignosGrimorio.OroBinaria * 0.6f * apagon, 5f);
                            VFXCore.Quad(centro, Color.White * 0.6f * apagon,
                                new Vector2(18f, 18f));
                            break;
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>La edad del teatro (ticks vivos).</summary>
        private float Edad()
        {
            float vidaMax = (int)Projectile.ai[0] switch
            {
                1 => 22f, 2 => 16f, 5 => 12f, 8 => 26f, _ => 12f,
            };
            return MathHelper.Clamp(vidaMax - Projectile.timeLeft, 0f, vidaMax - 1f);
        }
    }
}
