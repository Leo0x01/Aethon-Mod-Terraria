using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  AureumSkyStrike — v6.85 — EL RAYO DEL CIELO DEL COLMILLO DORADO.
    //
    //  LA INVESTIGACIÓN R7 (la sesión de las 9): el usuario pidió la
    //  mecánica del arma «Aureum Fang» — «caen rayos de energía del cielo
    //  sobre los enemigos». El mod exacto no existe en ningún índice
    //  (Google/Bing/Brave/YouTube/Steam/GitHub — ver /tmp/research/
    //  r7_aureum_fang.md); la referencia estrella documentada a fondo es
    //  EL DIVINE THUNDER del bastón Elysium (CalamityOverhaul, código
    //  completo leído): rayo sagrado que cae del cielo sobre el enemigo
    //  más cercano al cursor, telegrafiado, con doble bolt aditivo, anillo
    //  de choque y columna residual.
    //
    //  LA RECETA AQUÍ (la casa):
    //   · PRESAGIO (6 ticks): hilo de luz de 2 px cayendo de 760 px
    //     (α 0,25→0,75) + chispas de 4 puntas con la RAMPA DE VIDA DE
    //     MEAC (R3: dorado #D4A017 → blanco — la firma «EoLProj_Color»
    //     de XiHe, la técnica de gradient-map de VFX+ aplicada por
    //     EDAD en CPU) — el sabor del sol.
    //   · EL BOLT (tick 6): DOS canales del RayoSistema (la recreación
    //     1:1 del LightningGenerator de vanilla): el principal dorado
    //     (255,240,180) de 760 px + el secundario BLANCO ×0,7 desde
    //     720 px — el «double bolt» de la referencia; recada 3 (el
    //     re-random del zigzag), ancho 34.
    //   · VENTANA DE DAÑO (ticks 6-10): radio 112 px, un golpe por NPC
    //     (HashSet local — la inmunidad de la referencia).
    //   · RESPLANDOR: anillo 30→136 px EaseOutCubic con squish 0,45 en
    //     TRES colores (255,246,210)/(250,220,96)/(140,100,40) + columna
    //     residual RGB(255,236,170) α 0,4→0.
    //   · El golpe: Thunder + Item122, sacudida, 10 estrellas doradas
    //     radiales + 6 motas ascendentes.
    // ======================================================================
    public class AureumSkyStrike : ModProjectile
    {
        /// <summary>Ticks del presagio (el telegraph de la referencia: 6).</summary>
        private const int PresagioTicks = 6;

        /// <summary>Ventana de daño (ticks 6-10 de la referencia).</summary>
        private const int VenenoDesde = 6, VenenoHasta = 10;

        /// <summary>Radio de la ventana de daño (112 px en la referencia).</summary>
        private const float RadioGolpe = 112f;

        /// <summary>La altura del cielo de donde cae el hilo (760 px).</summary>
        private const float Cielo = 760f;

        private bool _nacio;
        private readonly HashSet<int> _golpeados = new();

        /// <summary>EL GUARD DE LA CASA (server y dedServ sin ojos).</summary>
        private static bool HayOjos =>
            Main.netMode != NetmodeID.Server && !Main.dedServ;

        /// <summary>LA RAMPA DE VIDA DE MEAC (R3): dorado #D4A017 → blanco
        /// en 60 ticks — la firma «EoLProj_Color» de XiHe (por edad, en
        /// CPU — la técnica de gradient-maps de VFX+ sin textura extra).</summary>
        private static Color RampaDoradoBlanco(float t) =>
            Color.Lerp(new Color(212, 160, 23), new Color(255, 255, 255),
                MathHelper.Clamp(t, 0f, 1f));

        // LA PALETA (la referencia exacta del informe):
        private static readonly Color OroBolt = new(255, 240, 180);
        private static readonly Color BlancoNucleo = new(255, 255, 250);
        private static readonly Color Columna = new(255, 236, 170);
        private static readonly Color Anillo1 = new(255, 246, 210);
        private static readonly Color Anillo2 = new(250, 220, 96);
        private static readonly Color Anillo3 = new(140, 100, 40);

        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetStaticDefaults()
        {
            // El hilo de 760 px no debe cull-earse (la lección de la
            // referencia: DrawScreenCheckFluff 900).
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 900;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;          // el daño va A MANO en la ventana
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 44;             // 6 presagio + 4 ventana + 34 resplandor
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            Vector2 c = Projectile.Center;
            int t = 44 - Projectile.timeLeft;     // la edad

            if (!_nacio)
            {
                _nacio = true;
                if (HayOjos)
                {
                    // EL ZAP del presagio (la referencia: 0,6 +0,6).
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.DD2_LightningAuraZap.WithPitchOffset(0.6f)
                            .WithVolumeScale(0.6f), c);
                }
            }

            if (!HayOjos) return;

            // === LA LUZ (la curva de la referencia: 0,3 → 1,1 → 0) ===
            float g = t < PresagioTicks
                ? MathHelper.Lerp(0.3f, 0.7f, t / (float)PresagioTicks)
                : MathHelper.Lerp(1.1f, 0f, MathHelper.Clamp((t - PresagioTicks) / 38f, 0f, 1f));
            Lighting.AddLight(c, 1.1f * g * 0.9f, 1.0f * g * 0.9f, 0.77f * g * 0.9f);

            if (t == PresagioTicks)
            {
                // === EL BOLT: el doble canal + el golpe del aterrizaje ===
                LanzarBolt(c);
                ElGolpe(c);
            }

            // === LA VENTANA DE DAÑO (posicional, un golpe por NPC) ===
            if (t >= VenenoDesde && t <= VenenoHasta && Projectile.owner == Main.myPlayer)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (_golpeados.Contains(npc.whoAmI) || !VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - c).Length() > RadioGolpe) continue;
                    _golpeados.Add(npc.whoAmI);
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        Projectile.damage, 4f, true);
                }
            }
        }

        /// <summary>EL DOBLE BOLT: principal dorado 760 px + secundario
        /// blanco ×0,7 desde 720 px (la receta exacta de la referencia).</summary>
        private static void LanzarBolt(Vector2 c)
        {
            // (1) EL PRINCIPAL — dorado (255,240,180), ancho 34, 26 ticks,
            //     recada 3 (el re-random del zigzag de la referencia).
            RayoParams p = RayoParams.Tormenta;
            p.Longitud = Cielo;
            RayoSistema.Lanzar(p, (uint)Main.rand.Next(1, int.MaxValue), c, null,
                OroBolt, BlancoNucleo, 34f, 26, AnimRayo.VenaTrueno,
                luzEstable: true, jugadorAncla: -1,
                recada: 3, parpadeo: false, anchoFin: 34f * 1.8f, aperturaColapso: true);

            // (2) EL SECUNDARIO — blanco ×0,7 desde 720 px, 20 ticks (la
            //     copia fría del núcleo: el «double bolt» de la referencia).
            RayoParams p2 = RayoParams.Tormenta;
            p2.Longitud = 720f;
            RayoSistema.Lanzar(p2, (uint)Main.rand.Next(1, int.MaxValue), c, null,
                BlancoNucleo, BlancoNucleo, 24f, 20, AnimRayo.VenaTrueno,
                luzEstable: true, jugadorAncla: -1,
                recada: 3, parpadeo: false, anchoFin: 24f * 1.8f, aperturaColapso: true);
        }

        /// <summary>EL GOLPE del aterrizaje (el cine de la referencia).</summary>
        private static void ElGolpe(Vector2 c)
        {
            // El trueno + el retumbo (Thunder 0,9 +0,3 · Item122 0,9 −0,1).
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Thunder.WithPitchOffset(0.3f).WithVolumeScale(0.9f), c);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.1f).WithVolumeScale(0.9f), c);

            // LA SACUDIDA de la casa (la referencia: «sacudida 3»).
            Pantalla.PresetImpacto(c, 0.9f, OroBolt);

            // 10 ESTRELLAS DORADAS radiales (vel 3-8) + 6 motas ascendentes.
            for (int i = 0; i < 10; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(c, DustID.Electric,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.75f) *
                        Main.rand.NextFloat(3f, 8f),
                    220, OroBolt, Main.rand.NextFloat(0.8f, 1.4f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    c + Main.rand.NextVector2Circular(40f, 18f), DustID.YellowTorch,
                    -Vector2.UnitY * Main.rand.NextFloat(1.5f, 3.5f),
                    220, Columna, Main.rand.NextFloat(0.6f, 1.1f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!HayOjos) return false;
            Vector2 c = Projectile.Center - Main.screenPosition;
            int t = 44 - Projectile.timeLeft;

            VFXCore.AbrirAdditive();
            try
            {
                if (t < PresagioTicks)
                {
                    // === EL HILO DEL PRESAGIO: 2 px cayendo de 760 px,
                    //     α 0,25→0,75 (el Pixel 1×1 del motor, estirado) ===
                    float a = MathHelper.Lerp(0.25f, 0.75f, t / (float)PresagioTicks);
                    var px = new Rectangle((int)c.X - 1, (int)(c.Y - Cielo), 2, (int)Cielo);
                    Main.spriteBatch.Draw(VFXCore.Pixel, px, RampaDoradoBlanco(t / 60f) * a);

                    // EL GLOW BASE 0,3 del pie del hilo.
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, c, null,
                        RampaDoradoBlanco(t / 60f) * 0.3f, 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);

                    // LAS CHISPAS DE 4 PUNTAS (el sabor XiHe: la rampa
                    // dorado→blanco por edad — 2 por tick, convergiendo).
                    for (int i = 0; i < 2; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Vector2 pos = c + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                            Main.rand.NextFloat(30f, 90f);
                        float sc = Main.rand.NextFloat(0.35f, 0.7f) * (1f - t / (float)PresagioTicks * 0.4f);
                        Main.spriteBatch.Draw(VFXCore.GlowOrb, pos, null,
                            RampaDoradoBlanco((t + i * 7f) / 60f) * 0.8f, ang,
                            VFXCore.GlowOrb.Size() * 0.5f, sc, SpriteEffects.None, 0f);
                    }
                }
                else
                {
                    // === EL RESPLANDOR (ticks 6-44): el anillo 30→136
                    //     EaseOutCubic con squish 0,45 en TRES colores +
                    //     la columna residual ===
                    float tt = MathHelper.Clamp((t - PresagioTicks) / 38f, 0f, 1f);
                    float ease = 1f - (float)Math.Pow(1f - tt, 3);          // EaseOutCubic
                    float radio = MathHelper.Lerp(30f, 136f, ease);

                    // LA COLUMNA RESIDUAL: SoftGlow ×(0,5, 2,6) α 0,4→0.
                    float colA = 0.4f * (1f - tt);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, c, null,
                        Columna * colA, 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(0.5f, 2.6f), SpriteEffects.None, 0f);

                    // EL ANILLO TRIPLE (squish 0,45: la elipse de choque).
                    for (int i = 0; i < 3; i++)
                    {
                        Color col = i == 0 ? Anillo1 : i == 1 ? Anillo2 : Anillo3;
                        float r = radio * (1f - i * 0.14f);
                        float alpha = (1f - tt) * (i == 0 ? 0.75f : i == 1 ? 0.5f : 0.3f);
                        var orig = VFXCore.Ring.Size() * 0.5f;
                        Main.spriteBatch.Draw(VFXCore.Ring, c, null, col * alpha,
                            0.15f * (1f - tt) + i * 0.7f, orig,
                            new Vector2(r / orig.X, r * 0.45f / orig.Y),
                            SpriteEffects.None, 0f);
                    }
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }
}
