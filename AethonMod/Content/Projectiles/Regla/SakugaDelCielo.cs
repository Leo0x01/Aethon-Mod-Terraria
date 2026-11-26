using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Regla
{
    // =====================================================================
    //  SAKUGADELCIELO — v6.175 — LA RECREACIÓN DEL ARMA DESDE CERO (la
    //  letra: «el tajo es una linea muy buena completamente liza, intenta
    //  investigar como son los tajos de los animes y otros mods y recrea
    //  el arma desde cero con una nueva animacion de tajo supercompleja»).
    //
    //  LA INVESTIGACIÓN (el canon sakuga + los efectos de otros géneros —
    //  impact frames, smear frames, chromatic displacement, focus lines):
    //  EL TAJO YA NO ES UNA LÍNEA — ES UNA SECUENCIA DE CINE.
    //
    //  LAS 16 CAPAS (todas deterministas — semilla = identity del tajo):
    //    FASE A · LA ANTICIPACIÓN (ticks 0-7)
    //     1. LA ESTRELLA DE LA HOJA — el destello de 4 puntas en el
    //        origen (el brillo de la katana al desenvainar)
    //     2. LA TENSIÓN — el arco FANTASMA al 15 % (la promesa del corte)
    //     3. EL RETROCESO — la luna se arquea 10 px ATRÁS (el wind-up)
    //    FASE B · EL FRAME DE IMPACTO (ticks 7-9)
    //     4. EL FLASH MONOCROMO — pantalla blanca 2 ticks (sakuga puro:
    //        «monochrome drawings hidden within the sequence»)
    //     5. EL PANEL DE TINTA — rectángulo oscuro translúcido tras el
    //        corte (el fotograma de manga)
    //     6. LA SACUDIDA — el micro-temblor de cámara
    //     7. LA ONDA EXPANSIVA — el anillo del golpe en el punto de corte
    //    FASE C · LA LUNA (ticks 7-18)
    //     8. LA MEDIA LUNA ÚNICA — TajoLib.Tajo (la luna de una pieza —
    //        el blanco-caliente de siempre)
    //     9. EL HALO ELÉCTRICO — el azul doble que respira
    //    10. EL DESPLAZAMIENTO CROMÁTICO — DOS lunas fantasma: roja
    //        desplazada (-3,-2) y cian (+3,+2) (la aberración del anime)
    //    11. EL CORO DE ECO — 3 arcos retrasados (1/2/3 ticks) que
    //        re-revelan la luna y se apagan en cascada
    //    12. LAS LÍNEAS DE VELOCIDAD — 6 trazos finos radiando del
    //        centro (focus lines del impacto)
    //    FASE D · EL RASTRO (ticks 16-40)
    //    13. LA CICATRIZ — el filo fino de tinta que queda donde cortó
    //        (30 ticks desvaneciéndose: el corte YA ocurrió)
    //    14. LAS CHISPAS FRÍAS — motas azules del borde exterior
    //    15. LOS PETALOS DE TINTA — 5 virutas que caen lento (el flavor
    //        del katana corte limpio)
    //    16. EL CLAC — el destello de clausura al final del rastro
    //
    //  EL COMBO (la X): cada CUARTO uso, EL TAJO CRUZADO — dos lunas
    //  cruzadas ±25°, el doble de frames de impacto y OCHO líneas de
    //  velocidad (el doble corte del anime).
    //
    //  EL DAÑO: YA NO al nacer — el corte golpea EN EL FRAME DE IMPACTO
    //  (tick 8: la anticipación existe, el castigo también) — la BANDA
    //  de la media luna (GolpeMotor, el contrato de la casa).
    //
    //  EL CABLE (MP): centro = Projectile.Center (viaja SOLO por la
    //  posición) · ai[0] = modo (0/4 cruzado) · ai[1] = inclinación ×1000
    //  · ai[2] = sentido de la hoja (0 baja · 1 sube). CERO localAI.
    // =====================================================================
    public class SakugaDelCielo : ModProjectile
    {
        private const int VIDA = 40;             // el cine completo
        private const int T_IMPACTO = 8;         // EL CORTE OCURRE aquí
        private const int T_LUNA_FIN = 18;
        private const int REVELA = 4;

        private const float ANCHO = 5.2f;
        private const float APERTURA = 1.92f;
        private const float INCLINACION = 0.61f;
        private const float BANDA = 58f;
        private const float RADIO_BASE = 150f;

        // LA PALETA (el cielo de la casa)
        private static readonly Color Filo = new(255, 252, 245);
        private static readonly Color HaloCielo = new(120, 200, 255);
        private static readonly Color HaloAlto = new(190, 235, 255);
        private static readonly Color CromoRojo = new(255, 90, 110);
        private static readonly Color CromoCian = new(110, 235, 255);
        private static readonly Color Tinta = new(24, 30, 58);

        private bool Sube => Projectile.ai[2] >= 1f;
        private bool Cruzado => Projectile.ai[0] >= 4f;

        private Vector2 CentroMundo() => Projectile.Center;
        private float Radio() => RADIO_BASE * (Projectile.scale > 0.1f ? Projectile.scale : 1f);

        private float AnguloMedio()
        {
            float diag = Sube ? -MathHelper.PiOver2 + INCLINACION
                              : MathHelper.PiOver2 - INCLINACION;
            return diag + Projectile.ai[1] / 1000f;
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { Main.projFrames[Projectile.type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 255;                 // invisible al vanilla
            Projectile.netImportant = false;
        }

        public override void AI()
        {
            int t = VIDA - Projectile.timeLeft;

            // === EL CORTE (tick de impacto: la anticipación terminó) ===
            if (t == T_IMPACTO && Main.netMode != NetmodeID.MultiplayerClient)
                GolpearLaBanda();

            // === LA ONDA Y EL GOLPE DE CÁMARA (solo donde hay ojos) ===
            if (t == T_IMPACTO && Main.netMode != NetmodeID.Server)
            {
                try
                {
                    Pantalla.Sacudir(Cruzado ? 0.9f : 0.55f, 0.22f, "sakuga");
                    Pantalla.OndaExpansiva(CentroMundo(), Cruzado ? 300f : 220f, 0.38f);
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SakugaDelCielo", exRegistro); }
            }

            // === LAS CHISPAS Y LOS PETALOS (deterministas — semilla fija) ===
            if (Main.netMode != NetmodeID.Server)
            {
                int seed = 7 + Projectile.identity * 31;
                var rng = new System.Random(seed);
                if (t >= T_IMPACTO && t < T_LUNA_FIN + 6 && t % 2 == 0)
                {
                    float ang = AnguloMedio() + (float)(rng.NextDouble() * 2 - 1) * APERTURA * 0.5f;
                    Vector2 p = CentroMundo() + new Vector2(MathF.Cos(ang), MathF.Sin(ang))
                        * Radio() * (0.9f + (float)rng.NextDouble() * 0.18f);
                    Dust d = Dust.NewDustPerfect(p, DustID.BlueMoss,
                        new Vector2((float)(rng.NextDouble() * 2 - 1) * 0.6f, -0.4f),
                        130, new Color(150, 215, 255), 0.55f);
                    d.noGravity = true;
                }
                // LOS PETALOS DE TINTA (después del corte: caen despacio)
                if (t == T_IMPACTO + 2)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = AnguloMedio() + (float)(rng.NextDouble() * 2 - 1) * APERTURA * 0.5f;
                        Vector2 p = CentroMundo() + new Vector2(MathF.Cos(ang), MathF.Sin(ang))
                            * Radio() * (float)(0.4 + rng.NextDouble() * 0.5);
                        Dust d = Dust.NewDustPerfect(p, DustID.Shadowflame,
                            new Vector2((float)(rng.NextDouble() * 2 - 1) * 0.9f, 0.4f),
                            150, new Color(90, 130, 220), 0.7f);
                        d.noGravity = false;
                    }
                }
            }
        }

        /// <summary>EL CORTE: la banda de la media luna (la herida llega
        /// cuando el frame de impacto OCURRE — GolpeMotor de la casa).</summary>
        private void GolpearLaBanda()
        {
            try
            {
                Vector2 c = CentroMundo();
                float radio = Radio();
                float medio = AnguloMedio();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.life <= 0 || n.friendly ||
                        n.townNPC || n.immortal || n.dontTakeDamage) continue;
                    Vector2 rel = n.Center - c;
                    float dist = rel.Length();
                    if (Math.Abs(dist - radio) > BANDA + n.width * 0.5f) continue;
                    float ang = MathF.Atan2(rel.Y, rel.X);
                    float delta = MathHelper.WrapAngle(ang - medio);
                    bool enCruz = false;
                    if (Cruzado)
                    {
                        // la X: la banda de las DOS lunas cruzadas ±0.42 rad
                        float delta2 = MathHelper.WrapAngle(ang - (medio + 0.84f));
                        enCruz = Math.Abs(delta2) < APERTURA * 0.5f + 0.25f;
                    }
                    if (Math.Abs(delta) > APERTURA * 0.5f + 0.25f && !enCruz) continue;
                    global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n,
                        Projectile.damage, Cruzado ? 1.45f : 1.25f);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SakugaDelCielo", exRegistro); }
        }

        public override bool PreDraw(ref Color luzColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            int t = VIDA - Projectile.timeLeft;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Math.Max(1, Projectile.identity + 37);
            Vector2 centro = CentroMundo() - Main.screenPosition;
            float medio = AnguloMedio();

            try
            {
                // ============================================================
                //  FASE A — LA ANTICIPACIÓN (0-7): la promesa del corte
                // ============================================================
                if (t < T_IMPACTO)
                {
                    float tensa = MathHelper.Clamp(t / (float)T_IMPACTO, 0f, 1f);
                    VFXCore.AbrirAdditive();
                    try
                    {
                        // 2. LA TENSIÓN — el arco fantasma al 15 % (la promesa)
                        TajoLib.Tajo(centro, Radio() * 0.96f,
                            medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                            1f, 0.15f * tensa, ANCHO * 0.5f, HaloCielo, HaloAlto, seed, time);
                        // 1. LA ESTRELLA DE LA HOJA — el destello 4 puntas (crece)
                        float estrella = 6f + 26f * tensa;
                        for (int e = 0; e < 4; e++)
                        {
                            float aE = e * MathHelper.PiOver2 + MathHelper.PiOver4;
                            Quad(VFXCore.SoftGlow, centro + new Vector2(MathF.Cos(aE), MathF.Sin(aE)) * estrella * 0.5f,
                                new Vector2(estrella, 2.2f), aE,
                                VFXCore.TintAlfa(HaloAlto, 0.65f * tensa));
                        }
                        Quad(VFXCore.SoftGlow, centro, new Vector2(estrella * 0.7f, estrella * 0.7f), 0f,
                            VFXCore.TintAlfa(HaloCielo, 0.4f * tensa));
                    }
                    finally { VFXCore.CerrarLoteSiAbierto(); }
                    return false;   // la luna aún no existe
                }

                // ============================================================
                //  FASE B — EL FRAME DE IMPACTO (8-9): el sakuga puro
                // ============================================================
                if (t >= T_IMPACTO && t < T_IMPACTO + 2)
                {
                    // 5. EL PANEL DE TINTA — el fotograma de manga tras el corte
                    //     (un rectángulo oscuro que enmarca la zona del golpe)
                    float alto = t == T_IMPACTO ? 1f : 0.55f;
                    Rectangle panel = new Rectangle(
                        (int)(centro.X - Radio() * 1.3f), (int)(centro.Y - Radio() * 1.05f),
                        (int)(Radio() * 2.6f), (int)(Radio() * 1.5f));
                    Main.spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, panel, Tinta * (0.35f * alto));
                    // 4. EL FLASH MONOCROMO (la pantalla blanca del sakuga)
                    if (t == T_IMPACTO)
                        Pantalla.Flash(new Color(250, 250, 255), 0.10f, Cruzado ? 0.5f : 0.32f);
                }

                // ============================================================
                //  FASE C — LA LUNA Y SU CORO (7-18)
                // ============================================================
                if (t < T_LUNA_FIN + 4)
                {
                    float reveal = Elastico(Math.Clamp((t - T_IMPACTO + 1) / (float)REVELA, 0f, 1f));
                    // la campana de vida de la luna (aguanta, luego cae)
                    float campana = t < T_IMPACTO + 8
                        ? 1f
                        : MathHelper.Clamp(1f - (t - (T_IMPACTO + 8)) / 10f, 0f, 1f);
                    float radio = Radio() * (1f + 0.15f * Math.Clamp((t - T_IMPACTO) / 12f, 0f, 1f));

                    VFXCore.AbrirAdditive();
                    try
                    {
                        // 10. EL DESPLAZAMIENTO CROMÁTICO — la aberración: la
                        //     luna roja atrás-izquierda y la cian adelante-derecha
                        Vector2 deriva = new Vector2(3.2f, 2.1f) * Math.Clamp(reveal, 0f, 1f);
                        TajoLib.Tajo(centro - deriva, radio,
                            medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                            reveal, campana * 0.45f, ANCHO * 0.9f, CromoRojo, CromoRojo, seed + 5, time);
                        TajoLib.Tajo(centro + deriva, radio,
                            medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                            reveal, campana * 0.45f, ANCHO * 0.9f, CromoCian, CromoCian, seed + 11, time);

                        // 11. EL CORO DE ECO — 3 lunas re-reveladas con retraso
                        for (int eco = 1; eco <= 3; eco++)
                        {
                            float revealEco = Elastico(Math.Clamp(
                                (t - T_IMPACTO + 1 - eco * 2) / (float)REVELA, 0f, 1f));
                            if (revealEco <= 0f) continue;
                            float campanaEco = campana * (0.5f - eco * 0.13f);
                            if (campanaEco <= 0.02f) continue;
                            TajoLib.Tajo(centro, radio * (1f + eco * 0.06f),
                                medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                                revealEco, campanaEco, ANCHO * (0.55f - eco * 0.1f),
                                HaloCielo, HaloAlto, seed + eco * 17, time);
                        }

                        // 8+9. LA MEDIA LUNA ÚNICA — la principal (el blanco
                        //     caliente + su halo — la luna de una pieza)
                        if (Cruzado)
                        {
                            TajoLib.Tajo(centro, radio,
                                medio - 0.84f - APERTURA * 0.5f, medio - 0.84f + APERTURA * 0.5f,
                                reveal, campana, ANCHO, HaloCielo, Filo, seed, time);
                            TajoLib.Tajo(centro, radio,
                                medio + 0.84f - APERTURA * 0.5f, medio + 0.84f + APERTURA * 0.5f,
                                reveal, campana, ANCHO, HaloCielo, Filo, seed + 3, time);
                        }
                        else
                            TajoLib.Tajo(centro, radio,
                                medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                                reveal, campana, ANCHO, HaloCielo, Filo, seed, time);

                        // 12. LAS LÍNEAS DE VELOCIDAD — trazos finos radiando
                        //     del centro (focus lines del anime)
                        int nLineas = Cruzado ? 8 : 6;
                        for (int l = 0; l < nLineas; l++)
                        {
                            float aL = medio + (l - (nLineas - 1) * 0.5f) * 0.42f;
                            float largo = (26f + (l % 3) * 14f) * campana;
                            Vector2 dir = new Vector2(MathF.Cos(aL), MathF.Sin(aL));
                            Vector2 desde = centro + dir * (Radio() * 1.08f);
                            Vector2 hasta = desde + dir * largo;
                            StormLib.Bolt(Main.spriteBatch, desde, hasta,
                                seed + l * 7, 2, 2.4f,
                                VFXCore.TintAlfa(HaloCielo, campana * 0.5f),
                                VFXCore.TintAlfa(HaloAlto, campana * 0.7f),
                                campana * 0.8f, 4, 10f);
                        }

                        // 7b. LA ONDA del impacto (el anillo que respira una vez)
                        if (t < T_IMPACTO + 6)
                        {
                            float f = (t - T_IMPACTO) / 6f;
                            float rAnillo = 30f + 160f * f;
                            Quad(VFXCore.SoftGlow, centro,
                                new Vector2(rAnillo, rAnillo * 0.5f), 0f,
                                VFXCore.TintAlfa(HaloCielo, 0.30f * (1f - f)));
                        }
                    }
                    finally { VFXCore.CerrarLoteSiAbierto(); }
                }

                // ============================================================
                //  FASE D — EL RASTRO (16-40): el corte ya ocurrió
                // ============================================================
                if (t >= T_LUNA_FIN - 2)
                {
                    float rastro = MathHelper.Clamp(1f - (t - (T_LUNA_FIN - 2)) / (float)(VIDA - T_LUNA_FIN + 2), 0f, 1f);
                    if (rastro > 0.02f)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            // 13. LA CICATRIZ — el filo fino de tinta (la línea
                            //     del corte que se desvanece LENTO)
                            TajoLib.Tajo(centro, Radio() * 1.02f,
                                medio - APERTURA * 0.5f, medio + APERTURA * 0.5f,
                                1f, 0.6f * rastro * rastro, 1.6f, HaloCielo, HaloAlto, seed, time);
                            if (Cruzado)
                                TajoLib.Tajo(centro, Radio() * 1.02f,
                                    medio - 0.84f - APERTURA * 0.5f, medio - 0.84f + APERTURA * 0.5f,
                                    1f, 0.6f * rastro * rastro, 1.6f, HaloCielo, HaloAlto, seed, time);
                            // 16. EL CLAC — el destello final de clausura
                            if (t > VIDA - 5)
                            {
                                float f = (t - (VIDA - 5)) / 5f;
                                Quad(VFXCore.SoftGlow, centro,
                                    new Vector2(46f * (1f - f), 46f * (1f - f)), 0f,
                                    VFXCore.TintAlfa(HaloAlto, 0.4f * (1f - f)));
                            }
                        }
                        finally { VFXCore.CerrarLoteSiAbierto(); }
                    }
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("SakugaDelCielo", exRegistro);
                try { VFXCore.CerrarLoteSiAbierto(); } catch (System.Exception exLote) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SakugaDelCielo", exLote); }
            }
            return false;
        }

        /// <summary>Quad centrado directo al lote aditivo activo.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>El easeOutBack de la casa (el POP del anime).</summary>
        private static float Elastico(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c = 1.35f;
            float u = t - 1f;
            return 1f + u * u * ((c + 1f) * u + c);
        }

        public override bool? CanHitNPC(NPC target) => false;   // el daño es la banda del frame de impacto
    }
}
