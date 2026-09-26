using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.50.19 — LOS PROYECTILES DE LAS SEIS IDEAS DEL GRIMORIO.
    //
    //  Convención de campos: ai[0] = NIVEL DEL GRIMORIO (la escalera),
    //  ai[1] = MODO de la variante (0 normal, otras por concepto).
    //  La escalera: 1 simple → 6 crece → 12 madura → 20 final.
    // ======================================================================

    // ======================================================================
    //  1 · EL FOLIO ERRANTE — la página que se convierte en rebaño
    //      E1: página que ondula y perfora 1 · E2: perfora 3 y suelta
    //      motas de tinta · E3: al golpear SE ROMPE en 3 mini-páginas
    //      homing · E4: las mini orbitan el impacto antes de caer.
    // ======================================================================
    public class FolioErranteProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/FolioHoja";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.light = 0.35f;
            Projectile.extraUpdates = 1;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Etapa => IdeasGrimorio.Etapa(Nivel);
        private bool Mini => Projectile.ai[1] == 1f;

        public override void AI()
        {
            // LA PÁGINA ONDULA (el papel vivo no vuela recto).
            float wig = Mini ? 2f : 5f;
            Vector2 perp = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X)
                .SafeNormalize(Vector2.Zero);
            Projectile.position += perp * MathF.Sin(Projectile.timeLeft * 0.22f) * wig * 0.10f;

            // rotación: la página plana cayendo de canto.
            Projectile.rotation += 0.16f * (Projectile.ai[1] == 1f ? 1f : 0.6f);

            // E2+: LA ESTELA DE MOTAS (la tinta que va soltando).
            if (Etapa >= 2 && !Mini && Main.rand.NextBool(4))
            {
                Dust.NewDust(Projectile.Center - new Vector2(3f, 3f), 6, 6,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1f, 0.2f));
            }

            // E3+: el mini-homing (las páginas rotas persiguen).
            if (Mini || Etapa >= 3)
            {
                NPC presa = BuscarPresa(600f);
                if (presa != null)
                {
                    Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float giro = Etapa >= 4 ? 0.14f : 0.09f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 10f, giro * 0.08f);
                }
            }

            // perforación por etapa (la página rompe más huesos).
            if (Etapa >= 2) Projectile.penetrate = 3;
            if (Etapa >= 3) Projectile.penetrate = 4;
        }

        private NPC BuscarPresa(float rango)
        {
            NPC mejor = null; float mejorD = rango * rango;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - Projectile.Center).LengthSquared();
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // E3: LA PÁGINA SE ROMPE — 3 mini-páginas homing desde el golpe.
            if (Etapa >= 3 && !Mini && Projectile.penetrate <= 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    float ang = MathHelper.TwoPi * i / 3f + Main.rand.NextFloat(0.4f);
                    Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel, Type,
                        (int)(Projectile.damage * 0.45f), Projectile.knockBack * 0.5f,
                        Projectile.owner, Nivel, 1f, 0f);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < (Mini ? 4 : 10); i++)
            {
                int d = Dust.NewDust(Projectile.Center, 20, 26, DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, 1f));
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // la página brilla por dentro (la runa central viva).
            lightColor = Color.Lerp(lightColor, new Color(255, 240, 200), 0.55f);
            if (Mini) Projectile.scale = 0.55f;
            return true;
        }
    }

    // ======================================================================
    //  2 · LA PLUMA PRIMORDIAL — la pluma que escribe
    //      E1: dardo perforante · E2: abanico de 3 · E3: al golpear
    //      ESCRIBE una runa que estalla · E4: la pluma ORO del centro
    //      escribe EL VERSO: seis glifos secuenciales.
    // ======================================================================
    public class PlumaPrimordialProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/PlumaDardo";

        public override void SetDefaults()
        {
            Projectile.width = 10; Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 120;
            Projectile.light = 0.25f;
            Projectile.extraUpdates = 1;
        }

        private int Nivel => (int)Projectile.ai[0];
        private bool Dorada => Projectile.ai[1] == 2f;
        private int Etapa => IdeasGrimorio.Etapa(Nivel);

        public override void AI()
        {
            // la pluma APUNTA a donde vuela (dardo de verdad).
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Etapa >= 2) Projectile.penetrate = 3;
            if (Etapa >= 3) Projectile.penetrate = 4;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // E3: LA RUNA ESCRITA — la pluma escribe al clavar.
            if (Etapa >= 3 && !Dorada && Projectile.penetrate <= 1)
                EscribirRuna(target.Center, 1);

            // E4: EL VERSO — la pluma ORO escribe seis glifos secuenciales
            // (una línea de runas que arden una tras otra).
            if (Dorada && Projectile.penetrate <= 1)
                EscribirRuna(target.Center, 6);
        }

        private void EscribirRuna(Vector2 centro, int cuantas)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            for (int i = 0; i < cuantas; i++)
            {
                Vector2 pos = centro + perp * (i - (cuantas - 1) / 2f) * 26f;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    pos, Vector2.Zero,
                    ModContent.ProjectileType<VersoVivoProjectile>(),
                    (int)(Projectile.damage * 0.5f), 1.5f,
                    Projectile.owner, Nivel, 4f, i * 5f);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, centro);
        }

        public override void OnKill(int timeLeft)
        {
            int d = Dust.NewDust(Projectile.Center, 8, 20, DustID.Enchanted_Gold,
                -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f);
            Main.dust[d].noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Dorada
                ? Color.Lerp(lightColor, new Color(255, 230, 150), 0.8f)
                : Color.Lerp(lightColor, new Color(240, 240, 235), 0.5f);
            return true;
        }
    }

    // ======================================================================
    //  3 · EL SELLO ERRANTE — el círculo que camina, detona y ENCADENA
    //      E1: sello lento que detona en aro · E2: al detonar escupe 6
    //      pernos radiales · E3: ENCADENA: nace un sello nuevo junto al
    //      enemigo más cercano (hasta 2 saltos) · E4: TRES sellos.
    // ======================================================================
    public class SelloErranteProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/SelloRueda";

        public override void SetDefaults()
        {
            Projectile.width = 34; Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.light = 0.4f;
        }

        private int Nivel => (int)Projectile.ai[0];
        private int Saltos => (int)Projectile.ai[1]; // las cadenas ya dadas
        private int Etapa => IdeasGrimorio.Etapa(Nivel);

        public override void AI()
        {
            // el sello camina despacio y GIRA sobre sí mismo.
            Projectile.rotation += 0.18f;
            Projectile.velocity *= 0.985f;

            // LA RUNA LATE (se aprecerá cuando detone).
            if (Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center, 30, 30, DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1.5f, 0f));
                Main.dust[d].noGravity = true;
            }

            if (Projectile.timeLeft <= 1) Detonar();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Detonar();

        private void Detonar()
        {
            if (Projectile.localAI[0] > 0f) return; // ya detonó
            Projectile.localAI[0] = 1f;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 34, 34, DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Main.dust[d].noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // E2: LOS PERNOS RADIALES del sello.
                if (Etapa >= 2)
                {
                    int pernos = 6;
                    for (int i = 0; i < pernos; i++)
                    {
                        float ang = MathHelper.TwoPi * i / pernos + Projectile.rotation;
                        Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 6.5f;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center, vel,
                            ModContent.ProjectileType<VersoVivoProjectile>(),
                            (int)(Projectile.damage * 0.4f), 1f,
                            Projectile.owner, Nivel, 3f, 0f);
                    }
                }

                // E3: LA CADENA — un sello nuevo junto al enemigo más
                // cercano (máximo 2 saltos: sello → sello → sello).
                if (Etapa >= 3 && Saltos < 2)
                {
                    NPC presa = null; float mejorD = 460f * 460f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                        if (n.whoAmI == (int)Projectile.ai[2]) continue;
                        float d = (n.Center - Projectile.Center).LengthSquared();
                        if (d < mejorD) { mejorD = d; presa = n; }
                    }
                    if (presa != null)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            presa.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), -70f),
                            Vector2.UnitY * 2f, Type,
                            Projectile.damage, Projectile.knockBack,
                            Projectile.owner, Nivel, Saltos + 1f, presa.whoAmI);
                    }
                }
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new Color(255, 220, 140), 0.6f);
            if (Etapa >= 3) Projectile.scale = 1.15f; // el sello maduro es más grande
            return true;
        }
    }

    // ======================================================================
    //  4 · LA LENGUA DE TINTA — la sierpe de tinta
    //      E1: sierpe corta (8 segmentos) con mordisco · E2: 14 segmen-
    //      tos y más rápida · E3: al morder SE BIFURCA en dos lenguas
    //      cortas · E4: LA HYDRA (tres desde el arranque, lo pone el
    //      item).
    // ======================================================================
    public class LenguaTintaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/LenguaCabeza";

        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
        }

        private int Nivel => (int)Projectile.ai[0];
        private bool Bifurcada => Projectile.ai[1] == 1f; // hija de la bifurcación
        private int Etapa => IdeasGrimorio.Etapa(Nivel);

        private int Longitud => Bifurcada ? 7 : (Etapa >= 2 ? 14 : 8);

        public override void AI()
        {
            // LA SIERPE SERPENTEA: homing suave + onda lateral (la fórmula
            // ondulante de la casa: el cuerpo lo dibuja el PreDraw con la
            // MISMA fase, así el pez sigue a la cabeza de verdad).
            NPC presa = null; float mejorD = 700f * 700f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - Projectile.Center).LengthSquared();
                if (d < mejorD) { mejorD = d; presa = n; }
            }
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float vel = (Etapa >= 2 ? 12f : 9f) * (Bifurcada ? 1.15f : 1f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * vel, 0.07f);
            }

            // la motas de tinta que va dejando el cuerpo.
            if (Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center, 10, 10, DustID.CorruptGibs,
                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 0.5f));
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // E3: LA BIFURCACIÓN — la lengua se parte en dos más cortas.
            if (Etapa >= 3 && !Bifurcada && Projectile.penetrate <= 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 vel = Projectile.velocity.RotatedBy(i * 0.5f);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center, vel, Type,
                            (int)(Projectile.damage * 0.6f), Projectile.knockBack * 0.6f,
                            Projectile.owner, Nivel, 1f, 0f);
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // EL CUERPO DE LA SIERPE: la misma fase de la cabeza — cada
            // segmento es una bolita de tinta MÁS PEQUEÑA ondulando detrás.
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            float fase = Projectile.timeLeft * 0.35f;
            for (int i = 1; i <= Longitud; i++)
            {
                float esc = (1f - i * 0.06f) * (Bifurcada ? 0.75f : 0.95f);
                if (esc <= 0.15f) break;
                Vector2 pos = Projectile.Center - dir * (12f * i)
                    + perp * MathF.Sin(fase - i * 0.8f) * 6.5f;
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null,
                    new Color(40, 32, 60, 235),
                    0f, tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
            }
            // la CABEZA con su ojo dorado.
            lightColor = Color.Lerp(lightColor, new Color(255, 226, 140), 0.25f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 16, 16, DustID.CorruptGibs,
                    Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 1f));
                Main.dust[d].noGravity = true;
            }
        }
    }

    // ======================================================================
    //  5 · EL OJO DEL TEXTO — el sucesor del Nightglow
    //      E1: ojo homing (giro suave) · E2: +2 PUPILAS escoltando ·
    //      E3: el ojo LEE: cada 80 t dispara un glifo a la presa cer-
    //      cana (400 px) · E4: LA MIRADA: lee a TRES presas a la vez.
    // ======================================================================
    public class OjoTextoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/OjoIris";

        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.light = 0.45f;
        }

        private int Nivel => (int)Projectile.ai[0];
        private bool Pupila => Projectile.ai[1] == 1f;
        private int Etapa => IdeasGrimorio.Etapa(Nivel);

        public override void AI()
        {
            // EL HOMING DEL OJO (mira → gira → persigue).
            NPC presa = null; float mejorD = 800f * 800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - Projectile.Center).LengthSquared();
                if (d < mejorD) { mejorD = d; presa = n; }
            }
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float giro = 0.035f + Etapa * 0.012f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 11f, giro);
            }

            // el ojo siempre MIRA a su presa (el iris gira).
            Projectile.rotation = presa != null
                ? (presa.Center - Projectile.Center).ToRotation()
                : Projectile.velocity.ToRotation();

            // E3: EL OJO LEE — dispara glifos a lo que lee.
            if (Etapa >= 3 && !Pupila)
            {
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 80f)
                {
                    Projectile.localAI[0] = 0f;
                    DispararLectura();
                }
            }
        }

        private void DispararLectura()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int objetivos = Etapa >= 4 ? 3 : 1;
            int disparados = 0;
            for (int i = 0; i < Main.maxNPCs && disparados < objetivos; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - Projectile.Center).LengthSquared();
                if (d > 400f * 400f) continue;
                Vector2 vel = (n.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 13f;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, vel,
                    ModContent.ProjectileType<VersoVivoProjectile>(),
                    (int)(Projectile.damage * 0.35f), 0.5f,
                    Projectile.owner, Nivel, 3f, 0f);
                disparados++;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new Color(255, 235, 200), 0.5f);
            if (Pupila) Projectile.scale = 0.5f;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(10f, 10f), 20, 20,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2.5f, 0f));
                Main.dust[d].noGravity = true;
            }
        }
    }

    // ======================================================================
    //  6 · EL VERSO VIVO — la palabra proyectil
    //      Modos: 0 = glifo de palabra (E1 fila / E2 ola / E3 PALÍNDROMO:
    //      rebota y vuelve) · 2 = EL PUNTO FINAL (E4: detona) ·
    //      3 = glifo-cita (invocado por otras ideas: runas escritas,
    //      pernos del sello, lecturas del ojo).
    // ======================================================================
    public class VersoVivoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/VersoGlifo";

        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 160;
            Projectile.light = 0.3f;
        }

        private int Nivel => (int)Projectile.ai[0];
        private float Modo => Projectile.ai[1];

        public override void AI()
        {
            // EL GLIFO VIBRA (la letra viva tiembla en su sitio).
            Projectile.rotation = MathF.Sin(Projectile.timeLeft * 0.25f +
                Projectile.ai[2] * 1.7f) * 0.28f;

            if (Modo == 3f || Modo == 4f)
            {
                // GLIFO-CITA: el glifo invocado por las otras ideas.
                // 3 = VOLADOR (pernos del sello, lecturas del ojo) ·
                // 4 = RUNA ESCRITA (la pluma lo deja clavado ardiendo).
                if (Modo == 4f)
                {
                    Projectile.velocity *= 0.86f;
                    Projectile.scale = 0.9f + 0.25f * MathF.Sin(Projectile.timeLeft * 0.5f);
                    if (Projectile.timeLeft <= 30 && Main.rand.NextBool(2))
                    {
                        int d = Dust.NewDust(Projectile.Center - new Vector2(7f, 9f), 14, 18,
                            DustID.Enchanted_Gold,
                            Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2.5f, 0f));
                        Main.dust[d].noGravity = true;
                    }
                }
                else Projectile.scale = 0.75f;
                return;
            }

            if (Modo == 2f)
            {
                // EL PUNTO FINAL: más grande, dorado, camina al final de la
                // palabra y detona en aro.
                Projectile.scale = 1.25f;
                return;
            }

            if (Modo == 1f)
            {
                // EL PALÍNDROMO: a mitad de vida la palabra VUELVE (se lee
                // al revés y golpea de nuevo).
                if (Projectile.timeLeft == 80)
                {
                    Projectile.velocity *= -0.85f;
                    Projectile.penetrate = 2; // vuelve a poder golpear
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // EL PUNTO FINAL DETONA (la sentencia termina).
            if (Modo == 2f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, Projectile.Center);
                OndaExpansiva();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = MathHelper.TwoPi * i / 8f;
                        Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7f;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center, vel, Type,
                            (int)(Projectile.damage * 0.35f), 1f,
                            Projectile.owner, Nivel, 3f, 0f);
                    }
                }
                for (int i = 0; i < 18; i++)
                {
                    int d = Dust.NewDust(Projectile.Center, 30, 30, DustID.Enchanted_Gold,
                        Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f));
                    Main.dust[d].noGravity = true;
                }
            }
        }

        private void OndaExpansiva()
        {
            // EL EMPUJÓN del punto (los enemigos cerca saltan).
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.friendly) continue;
                Vector2 d = n.Center - Projectile.Center;
                if (d.Length() < 130f)
                    n.velocity += d.SafeNormalize(Vector2.Zero) * 4f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color oro = new Color(255, 226, 140);
            lightColor = Modo == 2f
                ? Color.Lerp(lightColor, oro, 0.85f)
                : Color.Lerp(lightColor, oro, 0.45f);
            return true;
        }
    }
}
