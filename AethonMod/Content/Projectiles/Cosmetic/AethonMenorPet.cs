using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AethonMenorPet — v6.50.49 — EL AETHON MENOR (la mascota de luz).
    ///
    /// La letra: «crea una pequeña mascota de luz que sea Aethon
    /// original pero mas pequeño». Es LA LUZ PRIMORDIAL EN MINIATURA:
    /// su mismo núcleo (el sprite del jefe a media escala, aditivo),
    /// su corona de perlas (ocho, girando en su elipse) y su halo —
    /// flotando a tu lado, con la LUZ DE MUNDO girando por el espectro
    /// (la luz del arcoíris de la casa, en pequeñito).
    ///
    /// EL PATRÓN DE MASCOTA DE LUZ de vanilla (verificado contra el
    /// tML real): Main.projPet (persiste) + ProjectileID.Sets.LightPet
    /// (la familia del fuego fatuo — no la matan las mascotas ajenas) +
    /// NeedsUUID (la instancia ÚNICA por jugador). La IA es la
    /// persecución suave de las mascotas de vanilla: orbita el hombro
    /// del dueño con su vaivén, sin prisa.
    /// </summary>
    public class AethonMenorPet : ModProjectile
    {
        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            ProjectileID.Sets.LightPet[Type] = true;      // MASCOTA DE LUZ (familia del fuego fatuo)
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;         // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la luz atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            float t = Main.GlobalTimeWrappedHourly;

            // === LA PERSECUCIÓN SUAVE (el vaivén de las mascotas de
            //     vanilla): orbita el hombro del dueño — el lado cambia
            //     LENTO con el seno (pasea de un hombro al otro) y sube
            //     y baja con su propio compás ===
            float lado = MathF.Sin(t * 0.35f) * 46f - duenio.direction * 14f;
            float bob = MathF.Sin(t * 1.9f) * 9f;
            Vector2 meta = duenio.Center + new Vector2(lado, -46f + bob);

            // La búsqueda serena: nunca teletransporta, siempre alcanza.
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (dist > 900f)            // el dueño se fue muy lejos: le alcanza volando
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);

            // El freno al llegar (flota, no vibra encima del punto).
            if (dist < 24f)
                Projectile.velocity *= 0.80f;

            Projectile.rotation += 0.02f;   // el núcleo gira despacito (el sol en miniatura)

            // === LA LUZ DE MUNDO (la firma de la mascota de LUZ): el
            //     arcoíris de la casa girando por el espectro — la
            //     misita del trono, alumbra TODO el arcoíris ===
            float h = Frac(t * 0.05f);
            Color prisma = Prisma(h);
            Lighting.AddLight(Projectile.Center,
                0.9f * (prisma.R / 255f) + 0.25f,
                0.9f * (prisma.G / 255f) + 0.25f,
                0.9f * (prisma.B / 255f) + 0.25f);

            // === LAS CHISPAS (el rastro de la criatura de luz) ===
            if (!Main.dedServ && Main.rand.NextBool(14))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                        Main.rand.NextFloat(-8f, 8f)),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                        Main.rand.NextFloat(-0.8f, -0.2f)),
                    170, new Color(255, 244, 200), 0.8f);
                d.noGravity = true;
            }
        }

        /// <summary>La parte fraccionaria (siempre positiva).</summary>
        private static float Frac(float x) => x - MathF.Floor(x);

        /// <summary>El color del espectro en el matiz h ∈ [0,1).</summary>
        private static Color Prisma(float h)
        {
            h = Frac(h);
            float r, g, b;
            if (h < 1f / 6f) { r = 1f; g = h * 6f; b = 0f; }
            else if (h < 2f / 6f) { r = 2f - h * 6f; g = 1f; b = 0f; }
            else if (h < 3f / 6f) { r = 0f; g = 1f; b = h * 6f - 2f; }
            else if (h < 4f / 6f) { r = 0f; g = 4f - h * 6f; b = 1f; }
            else if (h < 5f / 6f) { r = h * 6f - 4f; g = 0f; b = 1f; }
            else { r = 1f; g = 0f; b = 6f - h * 6f; }
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f));
        }

        // ==================================================================
        //  EL DIBUJADO — LA LUZ PRIMORDIAL EN MINIATURA (el contrato del
        //  bool de la casa: PreDraw cierra el lote del pase, vuela el
        //  SUYO aditivo y DEVUELVE true → el llamador reabre)
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // === 1. EL HALO (el resplandor de la chispa — dos velos) ===
                float aliento = 1f + 0.08f * MathF.Sin(t * 1.4f);
                VFXCore.Quad(c, new Color(255, 244, 200, 255) * 0.16f,
                    new Vector2(64f * aliento, 64f * aliento), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 255, 250, 255) * 0.10f,
                    new Vector2(110f * aliento, 110f * aliento), 0f, VFXCore.SoftGlow);

                // === 2. EL NÚCLEO (el sprite del JEFE a media escala —
                //     ES él, más pequeño — girando despacito) ===
                Texture2D nucleo = Terraria.GameContent.TextureAssets.Npc[
                    ModContent.NPCType<global::AethonMod.Content.NPCs.AethonBoss>()].Value;
                if (nucleo != null)
                    VFXCore.Quad(c, new Color(255, 255, 255, 230),
                        new Vector2(34f, 34f), Projectile.rotation, nucleo);

                // === 3. LA CORONA DE PERLAS (ocho perlas en su elipse —
                //     la doble órbita del jefe, en miniatura) ===
                for (int i = 0; i < 8; i++)
                {
                    float ang = t * 1.1f + i * MathHelper.TwoPi / 8f;
                    Vector2 perla = c + new Vector2(MathF.Cos(ang) * 30f, MathF.Sin(ang) * 12f - 2f);
                    float frente = (MathF.Sin(ang) + 1f) * 0.5f;
                    VFXCore.Quad(perla, new Color(255, 240, 190, 255) * (0.30f + 0.40f * frente),
                        new Vector2(6f, 6f), 0f, VFXCore.SoftGlow);
                }

                // === 4. EL ARCOÍRIS EN MINIATURA (v6.50.51 — «ni en la
                //     mascota»: antes «el arcoíris» de la criatura era una
                //     lucecita cambiando de color — NADIE lo veía. AHORA
                //     es LA BANDA HORNEADA de siete franjas (VFXCore.
                //     Arcoiris, la del trono), en pequeñito alrededor de
                //     la chispa: el anillo del espectro de su dios) ===
                Texture2D banda = VFXCore.Arcoiris;
                if (banda != null)
                    VFXCore.Quad(c, new Color(255, 255, 255, 255) * (0.50f * aliento),
                        new Vector2(38f * 2.174f, 14f * 2.174f), t * 0.30f, banda);

                // === 5. LA CHISPA PRISMA (el latido del arcoíris — la
                //     lucecita que cambia de color como su luz de mundo) ===
                Color prisma = Prisma(t * 0.05f);
                VFXCore.Quad(c + new Vector2(0f, -30f), new Color(prisma.R, prisma.G, prisma.B, 255) * 0.55f,
                    new Vector2(10f, 10f), 0f, VFXCore.SoftGlow);

                if (VFXCore.QuadCount > 0)
                {
                    // v6.50.50 — EL CRASH DE LA MASCOTA, MUERTO DE RAÍZ:
                    // el contrato viejo (FlushAdditive + return true) dejaba
                    // el lote CERRADO y tML dibujaba encima → «Draw was
                    // called, but Begin has not yet been called» y el End
                    // final del bucle mataba el motor. EL CONTRATO DE LA
                    // CASA: salir SIEMPRE con el lote ABIERTO y vanilla —
                    // y el vuelco YA fue el dibujo (return false: tML no
                    // añade sprite encima).
                    VFXCore.FlushAdditive(null, true); // vuela el lote aditivo (queda cerrado)
                    VFXCore.ReabrirLoteVanilla();      // y SE REABRE — lote vivo para el siguiente
                    return false;
                }
                return false;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }
    }
}
