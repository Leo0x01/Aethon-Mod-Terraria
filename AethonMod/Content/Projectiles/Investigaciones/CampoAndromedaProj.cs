using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  CampoAndromedaProj — v6.85 — EL CAMPO DE FUERZA DE LOS GRILLETES
    //  DE ANDRÓMEDA (la investigación R9).
    //
    //  EL PROYECTIL PERSISTENTE (el patrón Nanomachina de la referencia):
    //  vive refrescado mientras el accesorio está equipado (timeLeft 10
    //  re-alimentado por AI) y dibuja el campo en TRES CAPAS (§d del
    //  informe r9_grilletes.md):
    //
    //   1. EL DOMO HEXAGONAL cian (89,229,255) — AnilloHexagonal.png —
    //      escala pulsante 0,9+0,06·sen(t/7) (el periodo del
    //      AndromedaDeathRay, el homenaje), lerp cíclico a blanco y
    //      opacidad PROPORCIONAL a las cargas (más cargas = campo más
    //      sólido).
    //   2. EL ANILLO RÚNICO DOBLE violeta (170,90,255): dos
    //      CirculoRunico de la casa contra-rotando +0,03 / −0,018
    //      rad/tick (el patrón del charge mode del mech).
    //   3. LOS POLVOS ORBITALES: 7 eléctricos cian en radio 60-70,
    //      ángulo t/60·2π + i·2π/n (el campo eléctrico de carga).
    //
    //  LA REACCIÓN A IMPACTOS (el «wow»): al romperse una carga el
    //  GrilletesPlayer dispara ShockTicks=8 — aquí se lee: el domo
    //  flasha ×1,35 con alfa 200 y arrastra 5 afterimages del anillo
    //  (las rotaciones viejas resbalando — el patrón
    //  DirectionalSolynForcefield). LUZ cian 1,5f cada tick.
    // ======================================================================
    public class CampoAndromedaProj : ModProjectile
    {
        private static Asset<Texture2D> _hex;
        private static Asset<Texture2D> _flare;

        private static readonly Color Cian = new(89, 229, 255);
        private static readonly Color Violeta = new(170, 90, 255);
        private static readonly Color VioletaTip = new(225, 190, 255);

        public override string Texture => "AethonMod/Content/Effects/AnilloHexagonal";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;              // refrescado por AI
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead ||
                !dueno.GetModPlayer<Content.Items.Investigaciones.GrilletesPlayer>().TieneGrilletes)
            {
                Projectile.Kill();
                return;
            }

            // EL CAMPO SIGUE AL JUGADOR (MountedCenter — el patrón).
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 10;             // la vida refrescada
            Projectile.localAI[0]++;              // el reloj propio (ticks)

            float t = Projectile.localAI[0];

            // === LA LUZ (cian 1,5f — intensidad ∝ cargas) ===
            var mp = dueno.GetModPlayer<Content.Items.Investigaciones.GrilletesPlayer>();
            float intensidad = 0.4f + 0.6f * (mp.Cargas / 100f);
            Vector3 luz = Cian.ToVector3() * 1.5f * intensidad;
            Lighting.AddLight(Projectile.Center, luz);

            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;

            // === LOS POLVOS ORBITALES (7 eléctricos, radio 60-70) ===
            if (t % 3f < 1f)
            {
                int n = 7;
                float fase = t / 60f * MathHelper.TwoPi;
                for (int i = 0; i < 2; i++)
                {
                    int idx = (int)(t / 3f) % n + i;
                    float ang = fase + idx * MathHelper.TwoPi / n;
                    float radio = 60f + (idx % 3) * 5f;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radio,
                        DustID.Electric, Vector2.Zero, 0, Cian, 0.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) return false;

            var mp = dueno.GetModPlayer<Content.Items.Investigaciones.GrilletesPlayer>();
            float t = Projectile.localAI[0];
            if (t < 1f) return false;

            if (_hex == null)
                _hex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/AnilloHexagonal",
                    AssetRequestMode.ImmediateLoad);
            if (_flare == null)
                _flare = ModContent.Request<Texture2D>("AethonMod/Content/Effects/FlareEstrella4Puntas",
                    AssetRequestMode.ImmediateLoad);

            Vector2 pos = Projectile.Center - Main.screenPosition;

            // EL PULSO (0,9 + 0,06·sen(t/7) — el homenaje al AndromedaDeathRay).
            float pulso = 0.9f + 0.06f * (float)Math.Sin(t / 7.0);

            // EL LERP CÍCLICO A BLANCO (60/20/20 en 1 s — el clon del
            // RarityColor del FlamsteedRing).
            float fase = (t % 60f) / 60f;
            Color baseCol = fase < 0.6f ? Cian
                : fase < 0.8f ? Color.Lerp(Cian, Color.White, (fase - 0.6f) / 0.2f)
                : Color.Lerp(Color.White, Cian, (fase - 0.8f) / 0.2f);

            // LA OPACIDAD ∝ CARGAS (90-150 pulsando × la proporción de escudo).
            float alfaPulso = 90f + 60f * (0.5f + 0.5f * (float)Math.Sin(t / 7.0));
            float proporcion = 0.35f + 0.65f * (mp.Cargas / 100f);
            float alfa = alfaPulso / 255f * proporcion;

            // EL FLASH DE LA ROTURA (×1,35 + alfa 200 durante el shock).
            bool shock = mp.ShockTicks > 0;
            if (shock)
            {
                alfa = Math.Max(alfa, 200f / 255f);
                pulso *= 1.35f;
            }

            var orig = _hex.Value.Size() * 0.5f;
            float escalaDomo = pulso;

            VFXCore.AbrirAdditive();
            try
            {
                // === 1. EL DOMO HEXAGONAL ===
                Main.spriteBatch.Draw(_hex.Value, pos, null, baseCol * alfa,
                    t * 0.004f, orig, new Vector2(escalaDomo * 1.4f), SpriteEffects.None, 0f);

                // LA ESTRELLA INTERIOR (el corazón del campo — 8 puntas:
                // dos flares cruzados a 30°).
                Main.spriteBatch.Draw(_flare.Value, pos, null, baseCol * (alfa * 0.5f),
                    0f, _flare.Value.Size() * 0.5f, 0.45f * escalaDomo, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(_flare.Value, pos, null, baseCol * (alfa * 0.5f),
                    MathHelper.Pi / 6f, _flare.Value.Size() * 0.5f, 0.45f * escalaDomo,
                    SpriteEffects.None, 0f);

                // === 2. EL ANILLO RÚNICO DOBLE (contra-rotante) ===
                // (orbit en rad/s: +0,03·60 = 1,8 / −0,018·60 = −1,08 —
                //  CirculoRunico gira el conjunto con time·orbit).
                float timeSeg = t / 60f;
                float r = 92f * escalaDomo;
                OrbitaLib.CirculoRunico(pos, r, timeSeg,
                    1.55f, 8, 1.8f, Violeta, VioletaTip,
                    1.35f, null, 0, 1, alfa, 0.2f);
                OrbitaLib.CirculoRunico(pos, r * 1.15f, timeSeg,
                    1.55f, 6, -1.08f, Violeta, VioletaTip,
                    1.1f, null, 3, 1, alfa * 0.8f, 0.16f);

                // === EL SHOCK: los 5 afterimages del anillo (las
                //     rotaciones viejas resbalando — el patrón del
                //     DirectionalSolynForcefield) ===
                if (shock)
                {
                    float fade = mp.ShockTicks / 8f;
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = mp.RotVieja[i] + (5 - i) * 0.12f - t * 0.02f;
                        float a2 = (0.25f * fade) * (1f - i / 5f);
                        Main.spriteBatch.Draw(_hex.Value, pos, null, Cian * a2,
                            ang, orig, new Vector2(1.4f * escalaDomo * (1f + i * 0.06f)),
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
