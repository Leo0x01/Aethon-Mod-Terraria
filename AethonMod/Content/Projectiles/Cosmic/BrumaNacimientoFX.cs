using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.51.4 → v6.51.5 — LA BRUMA DEL NACIMIENTO, EN TRES ACTOS.
    //
    //  v6.51.4 (la letra: «teniamos ya una buena libreria de brumas
    //  completamente funcional que no ponen esos circulos planos… solo
    //  se debe cubrir con bruma, nada geometria plana»): el nacimiento
    //  se cubre con BrumaFX — los puffs de humo fBm de la casa.
    //
    //  v6.51.5 — LA LETRA DEL PARTO («el nacimiento del sol runico en la
    //  bruma es como demasiado espontaneo, necesita una mejor animacion
    //  de nacimiento»): la nube YA NO se queda quieta mientras el
    //  recién nacido aparece — ahora es UN PARTO:
    //
    //    · EL DESTELLO DE LA IGNICIÓN (ticks 0..8): el fogonazo blanco
    //      del nacimiento — la primera luz de lo que acaba de nacer.
    //    · LA CORONA DE EXPULSIÓN: la bruma se SOPA HACIA AFUERA en
    //      corona (siete cúmulos que se abren en anillo) para dejarte
    //      ver al recién nacido — que se enciende PEQUEÑO dentro de su
    //      nube y recién estalla a su tamaño de verdad cuando la nube
    //      se disuelve (la coreografía vive también en el propio sol).
    //
    //  Y EL MODO 3 — LA NOVA DE BRUMA (la letra: «a la explosion final
    //  del sol runico, en vez de ser un sprite de un circulo de fuego,
    //  no seria mejor que sea un circulo de bruma»): cuando el sol
    //  muere, su nova EXHALA SU CÍRCULO DE BRUMA DORADA — DOCE cúmulos
    //  que se expanden, se desgarran y se disuelven. Las runas de eco y
    //  la onda de lente siguen viviendo en el OnKill del sol.
    //
    //  ai[0] = 1 SOL (dorado) · 2 AGUJERO (violeta) · 3 NOVA DORADA ·
    //  ai[1] = la intensidad (0..1 — el nacimiento del supremo respira
    //  más; en la nova, la escala del círculo).
    // ======================================================================
    public class BrumaNacimientoFX : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int VIDA = 46;          // la nube del nacimiento
        private const int VIDA_NOVA = 44;     // el círculo de la nova

        private int Modo => (int)Projectile.ai[0];
        private bool EsSol => Modo == 1;
        private bool EsNova => Modo == 3;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA;
            Projectile.netImportant = false;   // cosmético: cada cable la suya
        }

        public override void AI()
        {
            // LA EXHALACIÓN SUBE (el humo frío de lo que acaba de nacer).
            Projectile.velocity *= 0.94f;
            Projectile.velocity.Y -= 0.02f;

            // LA LUZ: el destello de la ignición (los primeros ticks
            // ARDEN — la primera luz del recién nacido)… y la nova dorada
            // es un sol entero apagándose.
            if (Main.netMode == NetmodeID.Server) return;
            float edad = EsNova ? (VIDA_NOVA - Projectile.timeLeft) : (VIDA - Projectile.timeLeft);
            if (EsNova)
            {
                float f = MathHelper.Clamp(edad / VIDA_NOVA, 0f, 1f);
                float arde = (1f - f) * (f < 0.12f ? f / 0.12f : 1f);
                Lighting.AddLight(Projectile.Center,
                    0.95f * arde, 0.72f * arde, 0.30f * arde);
            }
            else if (edad < 9f)
            {
                float fogonazo = 1f - edad / 9f;
                if (EsSol) Lighting.AddLight(Projectile.Center,
                    1.05f * fogonazo, 0.85f * fogonazo, 0.45f * fogonazo);
                else Lighting.AddLight(Projectile.Center,
                    0.42f * fogonazo, 0.20f * fogonazo, 0.75f * fogonazo);
            }
            else if (!EsSol)
                Lighting.AddLight(Projectile.Center, 0.14f, 0.06f, 0.24f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                if (EsNova)
                {
                    DibujarNova();
                    return false;
                }

                float t = Main.GlobalTimeWrappedHourly;
                float edad = VIDA - Projectile.timeLeft;
                float vida = edad / VIDA;                       // 0 → 1
                // LA ENVOLVENTE: nace GORDA (el estallido se asienta) y se
                // disuelve lenta — la firma de la bruma de la casa.
                float densidad = MathHelper.Clamp(1.15f - vida * 1.05f, 0f, 1f)
                    * MathHelper.Clamp(vida * 6f, 0f, 1f);
                if (densidad <= 0.03f) return false;

                float escala = MathHelper.Clamp(
                    0.7f + 0.3f * Projectile.ai[1], 0.7f, 1.6f);
                int semilla = Projectile.identity % 113 + 5;
                Vector2 centro = Projectile.Center - Main.screenPosition;

                // LA NUBE DEL COLOR QUE NACE (BrumaFX: humo fBm de verdad).
                Color masa = EsSol
                    ? new Color(96, 66, 22)      // el humo DORADO (oscuro: la luz la pone el sol)
                    : new Color(52, 24, 84);     // el humo VIOLETA (la bruma del vacío)

                // ============================================================
                //  v6.51.5 — LA CORONA DE EXPULSIÓN: la bruma se SOPA
                //  HACIA AFUERA en un anillo de SIETE cúmulos que se abre
                //  (easeOutCubic) para dejarte ver al recién nacido — la
                //  nube ya no se queda quieta: el parto EMPUJA la niebla.
                // ============================================================
                float abertura = 1f - (1f - vida) * (1f - vida) * (1f - vida); // easeOutCubic
                float radioCorona = MathHelper.Lerp(14f, 96f, abertura) * escala;
                int cúmulos = 7;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    // EL VELO CENTRAL (los primeros ticks: la nube que
                    // esconde al recién nacido mientras se enciende).
                    if (vida < 0.42f)
                    {
                        float velo = 1f - vida / 0.42f;
                        BrumaFX.Cloud(centro, (58f - 20f * vida) * escala, masa,
                            semilla, t, 4, alpha: 0.60f * densidad * velo);
                    }
                    // LOS SIETE CÚMULOS DE LA CORONA (soplando hacia afuera).
                    for (int k = 0; k < cúmulos; k++)
                    {
                        float ang = k / (float)cúmulos * MathHelper.TwoPi
                            + semilla * 0.17f + t * 0.10f;
                        Vector2 pos = centro + new Vector2(
                            MathF.Cos(ang), MathF.Sin(ang) * 0.88f) * radioCorona;
                        BrumaFX.Cloud(pos,
                            (34f + 16f * vida) * escala,
                            masa, semilla + 13 + k * 29, t + k * 0.7f, 3,
                            alpha: 0.62f * densidad);
                    }
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                // EL ALIENTO (la lectura nocturna) + EL DESTELLO DE LA
                // IGNICIÓN (los primeros 8 ticks: el fogonazo de la
                // primera luz — dos cápsulas cruzadas de la casa).
                try
                {
                    VFXCore.Begin();
                    Color aliento = EsSol
                        ? new Color(255, 208, 110)
                        : new Color(150, 84, 226);
                    for (int k = 0; k < 3; k++)
                    {
                        float late = 0.6f + 0.4f * MathF.Sin(t * (1.2f + k * 0.31f) + k * 2.1f);
                        VFXCore.Quad(Projectile.Center +
                            new Vector2(MathF.Sin(t * 0.7f + k * 2.4f) * radioCorona * 0.3f,
                                        -radioCorona * 0.18f * k),
                            aliento * (0.10f * densidad * late),
                            new Vector2(radioCorona * (1.15f - k * 0.28f),
                                        radioCorona * (0.9f - k * 0.22f)));
                    }
                    // EL DESTELLO DE LA IGNICIÓN — el fogonazo de 4 puntas
                    // (la firma de los soles de la casa, chico y ardiente).
                    if (edad < 9f)
                    {
                        float fogonazo = (1f - edad / 9f);
                        float destello = fogonazo * fogonazo;
                        Color blanco = EsSol
                            ? new Color(255, 244, 200)
                            : new Color(232, 204, 255);
                        float largo = 78f * destello + 16f;
                        for (int e = 0; e < 2; e++)
                        {
                            float angE = e == 0 ? 0f : MathHelper.PiOver2;
                            angE += MathF.Sin(t * 3f) * 0.06f;
                            Vector2 dir = new Vector2(MathF.Cos(angE), MathF.Sin(angE));
                            VFXCore.Line(Projectile.Center - dir * largo,
                                Projectile.Center + dir * largo,
                                blanco * (0.85f * destello), 5.5f * destello + 1.5f);
                        }
                        VFXCore.Quad(Projectile.Center, blanco * (0.65f * destello),
                            new Vector2(52f * destello + 10f, 52f * destello + 10f));
                    }
                    VFXCore.FlushAdditive();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        // ==================================================================
        //  EL MODO 3 — LA NOVA DE BRUMA: el círculo de bruma dorada de la
        //  explosión final del sol. DOCE cúmulos en un anillo que se
        //  expande (easeOutCubic), SE DESGARRAN (cada cúmulos crece y
        //  deriva tangencialmente) y SE DISUELVEN. La escala del círculo
        //  viaja en ai[1] (la nova la llama con su radio real).
        // ==================================================================
        private void DibujarNova()
        {
            float t = Main.GlobalTimeWrappedHourly;
            float edad = VIDA_NOVA - Projectile.timeLeft;
            float vida = MathHelper.Clamp(edad / VIDA_NOVA, 0f, 1f);
            float fadeIn = MathHelper.Clamp(vida * 5f, 0f, 1f);
            float apagon = 1f - vida;                            // se muere disolviéndose
            if (apagon <= 0.03f) return;

            // EL RADIO DEL CÍRCULO: easeOutCubic — arranca EXPLOTA y frena.
            float abre = 1f - (1f - vida) * (1f - vida) * (1f - vida);
            float radioNova = MathHelper.Lerp(0.16f, 1f, abre)
                * 340f * MathHelper.Clamp(Projectile.ai[1], 0.5f, 1.8f);

            int semilla = Projectile.identity % 97 + 11;
            Vector2 centro = Projectile.Center - Main.screenPosition;
            Color masa = new Color(96, 66, 22);        // el humo DORADO (oscuro)
            Color masaClara = new Color(130, 96, 38);  // el oro profundo del interior

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                BrumaFX.BeginMass();
                // LA NUBE MADRE (el corazón de donde nació el círculo —
                // los primeros ticks todavía respira ahí).
                if (vida < 0.35f)
                {
                    float corazon = 1f - vida / 0.35f;
                    BrumaFX.Cloud(centro, 80f * escalaNova() * (0.6f + 0.4f * corazon),
                        masa, semilla, t, 5, alpha: 0.55f * apagon * corazon);
                }
                // LOS DOCE CÚMULOS DEL CÍRCULO — se expanden, SE
                // DESGARRAN (crecen) y DERIVAN tangencialmente.
                for (int k = 0; k < 12; k++)
                {
                    float ang = k / 12f * MathHelper.TwoPi
                        + semilla * 0.13f + vida * 0.55f;      // la deriva tangencial
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.92f) * radioNova;
                    // EL DESGARRE: el cúmulo NACE compacto y se ROMPE
                    // (crece) al expandirse.
                    float tamano = (52f + 46f * vida) * escalaNova();
                    BrumaFX.Cloud(pos, tamano, masa,
                        semilla + 17 + k * 31, t + k * 0.53f, 3,
                        alpha: 0.58f * apagon * fadeIn);
                    BrumaFX.Cloud(pos, tamano * 0.55f, masaClara,
                        semilla + 53 + k * 31, t * 0.8f + k * 0.4f, 2,
                        alpha: 0.34f * apagon * fadeIn);
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
                VFXCore.CerrarLoteSiAbierto();
            }

            // EL ALIENTO DORADO de la nova (la lectura nocturna — la
            // lección de la casa: toda bruma oscura necesita aliento
            // aditivo para leerse de noche).
            try
            {
                VFXCore.Begin();
                Color aliento = new Color(255, 208, 110);
                for (int k = 0; k < 12; k++)
                {
                    float ang = k / 12f * MathHelper.TwoPi + semilla * 0.13f + vida * 0.55f;
                    Vector2 pos = Projectile.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.92f) * radioNova;
                    float late = 0.65f + 0.35f * MathF.Sin(t * 2.1f + k * 1.31f);
                    VFXCore.Quad(pos, aliento * (0.13f * apagon * fadeIn * late),
                        new Vector2(64f * escalaNova(), 64f * escalaNova()));
                }
                // EL FOGONAZO INICIAL de la nova (la primera luz del círculo).
                if (edad < 10f)
                {
                    float fogonazo = 1f - edad / 10f;
                    VFXCore.Quad(Projectile.Center,
                        new Color(255, 246, 200) * (0.55f * fogonazo * fogonazo),
                        new Vector2(150f * fogonazo + 20f, 150f * fogonazo + 20f));
                }
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
        }

        /// <summary>La escala visual de la nova (ai[1] clamp — la llama
        /// el OnKill del sol con su radio real: la VEINTE respira más).</summary>
        private float escalaNova()
            => MathHelper.Clamp(0.75f + 0.45f * (Projectile.ai[1] - 0.75f), 0.75f, 1.6f);
    }
}
