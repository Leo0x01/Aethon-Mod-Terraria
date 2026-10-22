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
    /// BrilloSolarHalo — v6.51.3 — EL BRILLO ESPECIAL DE LOS SOLES RÚNICOS,
    /// DESTILADO EN ADORNO.
    ///
    /// LA LETRA: «me acabo de dar cuenta que los soles runicos tienen como
    /// un brillo especial, bueno es como ese brillo… ademas crea un item
    /// cosmetico que sea solo ese brillo especial de los soles runicos,
    /// este brilo esta en los soles runicos de 3 anillos en adelante».
    ///
    /// ESE BRILLO ES DrawTwinkles (RuneSunRenderer — los soles de 3
    /// anillos+): LOS DESTELLOS DE 4 PUNTAS — dos cápsulas cruzadas de
    /// blanco incandescente + el corazón dorado, que nacen, arden y
    /// mueren, esparcidos en una banda elíptica alrededor de la estrella.
    /// ESTE halo pegado a su dueño dibuja EXACTAMENTE ESO — nada más:
    /// ni disco, ni anillos, ni aura: SOLO los destellos, flotando en
    /// la banda de siempre alrededor del portador (un trozo de sol
    /// caminando por el mundo) + la luz cálida y tenue que les
    /// corresponde.
    ///
    /// Patrón probado de AuraPortadorHalo (v6.48): pegado al dueño, el
    /// dueño local lo invoca y tML lo sincroniza, PreDraw por el camino
    /// aditivo de la casa (cerrar el lote del pase → vuelco propio →
    /// reabrir TAL CUAL).
    /// </summary>
    public class BrilloSolarHalo : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;      // cosmético: no toca nada
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;          // el dueño lo refresca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().BrilloSolar)
            {
                Projectile.Kill();
                return;
            }

            // Pegado al dueño, siempre.
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 6;

            // LA LUZ CÁLIDA Y TENUE (la de los destellos: apenas un
            // resplandor de atardecer — el adorno alumbra lo justo).
            Lighting.AddLight(Projectile.Center, 0.34f, 0.26f, 0.12f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return false;

            float t = Main.GlobalTimeWrappedHourly;
            Vector2 centro = Projectile.Center;
            int semilla = Projectile.identity % 97 + 3;

            // LA RECETA DE DrawTwinkles (RuneSunRenderer, tier 3+ —
            // verbatim en la matemática): destellos que nacen, arden y
            // mueren en la banda elíptica de siempre alrededor de la
            // estrella — aquí la estrella es el PORTADOR.
            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                const int count = 9;
                for (int i = 0; i < count; i++)
                {
                    float h = VFXCore.Hash01(semilla, 300 + i, 7);
                    float vida = (t * 0.5f + h * 3f) % 1f;
                    float arde = MathF.Sin(vida * MathHelper.Pi); // nace, arde, muere
                    if (arde < 0.05f) continue;

                    float ang = VFXCore.Hash01(semilla, 310 + i, 11) * MathHelper.TwoPi + t * 0.07f;
                    float dist = (1.25f + 1.55f * VFXCore.Hash01(semilla, 320 + i, 13)) * 34f;
                    Vector2 pos = centro + new Vector2(
                        MathF.Cos(ang) * dist, MathF.Sin(ang) * dist * 0.72f);

                    float size = (0.30f + 0.25f * VFXCore.Hash01(semilla, 330 + i, 17)) *
                                 34f * arde;
                    // EL DESTELLO DE 4 PUNTAS: dos cápsulas cruzadas + núcleo
                    // (el brillo especial del sol — la firma de los 3 anillos+).
                    VFXCore.Quad(pos, new Color(255, 248, 225) * (0.55f * arde),
                        new Vector2(size * 2.2f, Math.Max(1.6f, 0.035f * 34f) * arde));
                    VFXCore.Quad(pos, new Color(255, 248, 225) * (0.55f * arde),
                        new Vector2(Math.Max(1.6f, 0.035f * 34f) * arde, size * 2.2f));
                    VFXCore.Quad(pos, new Color(255, 214, 120) * (0.50f * arde),
                        new Vector2(size * 1.5f, size * 1.5f));
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
