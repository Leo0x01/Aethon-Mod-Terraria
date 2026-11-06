using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;
using AethonMod.Content.Effects.AuraJuicio;
using AethonMod.Content.Items.Accessories;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects.AuraJuicio
{
    /// <summary>
    /// RecursosAuraJuicio — v6.104 — LAS TEXTURAS DEL AURA DE JUICIO,
    /// cacheadas y null-seguras (el patrón TentaculoAbismo de VFXCore:
    /// Asset diferido, IsLoaded antes de .Value, catch de recarga
    /// caliente — una textura que aún no vive JAMÁS tumba un frame).
    ///
    /// El set del original (WarbannerLight), recreado píxel a píxel por
    /// tools/aura_juicio_v6104.py — CERO arte ajeno (la ley v6.93):
    ///  · CirculoBloom (200×200) — el BloomCircle del original: el
    ///    gradiente radial blanco→transparente de las CINCO CAPAS.
    ///  · EstallidoFragmentado (512×512) — el ShatteredExplosion: el
    ///    destello radial con fracturas angulares de los DOS GIROS.
    ///  · BloomChico (200×200) — el SmallBloom: el bloom concentrado
    ///    de las chispas de la quemadura (lo viste el PolvoLuzLuciernaga).
    /// </summary>
    public static class RecursosAuraJuicio
    {
        private static Asset<Texture2D> _circuloBloom;
        private static Asset<Texture2D> _estallidoFragmentado;
        private static Asset<Texture2D> _bloomChico;

        /// <summary>EL BLOOM DE LAS CINCO CAPAS (200×200). Null-safe.</summary>
        public static Texture2D CirculoBloom
        {
            get
            {
                try
                {
                    _circuloBloom ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/AuraJuicio/CirculoBloom");
                    return _circuloBloom.IsLoaded ? _circuloBloom.Value : null;
                }
                catch { return null; }
            }
        }

        /// <summary>EL DESTELLO FRACTURADO de los dos giros (512×512).
        /// Null-safe.</summary>
        public static Texture2D EstallidoFragmentado
        {
            get
            {
                try
                {
                    _estallidoFragmentado ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/AuraJuicio/EstallidoFragmentado");
                    return _estallidoFragmentado.IsLoaded ? _estallidoFragmentado.Value : null;
                }
                catch { return null; }
            }
        }

        /// <summary>EL BLOOM PEQUEÑO de las chispas (200×200, más duro y
        /// concentrado que CirculoBloom — el SmallBloom del original).
        /// Null-safe.</summary>
        public static Texture2D BloomChico
        {
            get
            {
                try
                {
                    _bloomChico ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/AuraJuicio/BloomChico");
                    return _bloomChico.IsLoaded ? _bloomChico.Value : null;
                }
                catch { return null; }
            }
        }
    }
}

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  AuraJuicioLuz — v6.104 — EL ANCLA DE LUZ DEL AURA DE JUICIO (el
    //  puerto EXACTO de la técnica VFX del WarbannerLight original).
    //
    //  LA LETRA DEL USUARIO: «copia exactamente el codigo de Warbanner
    //  of the Righteous y sus textura, efectos vfx tecnica todo».
    //
    //  LA TÉCNICA DEL ORIGINAL, capa por capa:
    //   · EL CUERPO ES INVISIBLE — la textura declarada no se dibuja
    //     (SoftGlow de 1×... de la casa): TODO el vestido vive en
    //     PreDraw con return false. No daña, no choca, no se mueve.
    //   · EL PULSO rot2: |sin(tiempo·0.15/π)·0.2| + 0.8 → las capas
    //     RESPIRAN entre ×0.8 y ×1.0 de su escala.
    //   · LAS CINCO CAPAS de bloom concéntrico (BloomCircle 200×200):
    //     cada capa i se tiñe Color.Lerp(bColor, Blanco, i·0.15) y
    //     escala (0.75 − i·0.15)·rot2 — la torre de halos apilados
    //     que hace el “corazón ardiente” del original.
    //   · LOS DOS DESTELLOS (ShatteredExplosion 512×512) girando al
    //     azar a escala MÍNIMA (0.03 y 0.04·rot2) — el chisporroteo
    //     del núcleo. (La casa: giro determinista por semilla + el
    //     reloj — la ley de cero Main.rand en el render.)
    //   · LAS LUCIÉRNAGAS: cada 2 ticks un polvo dorado nace a
    //     (10,10).RotatedByRandom(100) del portador y se aleja suave
    //     (velocidad ×0.1..0.4) — el vuelo de las ascuas del campo.
    //   · LA LUZ: Goldenrod ×1.5 — el campo ILUMINA la batalla.
    //   · EL COLOR VIVO bColor: el ciclo Goldenrod↔Orange latiendo
    //     con el reloj del mundo (índice por (t·5/2) % 2, el frac
    //     como mezcla — el vaivén cálido del original).
    // ======================================================================
    public class AuraJuicioLuz : ModProjectile
    {
        /// <summary>EL COLOR VIVO del aura (Goldenrod↔Orange ciclando —
        /// el bColor del original; público: las capas lo leen).</summary>
        public Color ColorBase = Color.White;

        /// <summary>La edad del aura en ticks (alimenta el pulso rot2).</summary>
        public float Tiempo;

        /// <summary>EL PULSO de las capas (×0.8..×1.0 — respira).</summary>
        public float Rot2 = 0.8f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;          // es LUZ, no arma
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;       // el campo del portador: todos lo ven
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];

            // EL VÍNCULO — mientras el dueño viva y PORTÉ el estandarte
            // (slots 3..10), el ancla vive; si lo suelta o cae, muere
            // (el “glow && accessory” del original).
            if (dueno == null || !dueno.active || dueno.dead ||
                !AuraDeJuicioItem.LlevaElAura(dueno))
            {
                Projectile.Kill();
                return;
            }

            // EL ANCLA: clavada al pecho del portador, inmortal mientras
            // el vínculo viva (timeLeft++ del original).
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;

            // ==============================================================
            //  EL COLOR VIVO — el ciclo Goldenrod↔Orange (el reloj del
            //  mundo ×5; cada tramo de 2 s va y vuelve — el vaivén cálido
            //  del original: índice = (tasa/2) % 2, el frac la mezcla).
            // ==============================================================
            float tasa = Main.GlobalTimeWrappedHourly * 5f;
            float fase = tasa * 0.5f;
            int indice = (int)fase % 2;
            float frac = fase - (int)fase;
            ColorBase = indice == 0
                ? Color.Lerp(Color.Goldenrod, Color.Orange, frac)
                : Color.Lerp(Color.Orange, Color.Goldenrod, frac);

            // EL PULSO — la torre de capas respira (×0.8..×1.0).
            Rot2 = Math.Abs((float)Math.Sin(Tiempo * 0.15f / MathHelper.Pi) * 0.2f) + 0.8f;
            Tiempo++;

            // ==============================================================
            //  EL VESTIDO VIVO (solo cliente — la luz y las luciérnagas).
            // ==============================================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                // LA LUZ DEL CAMPO — Goldenrod ×1.5 (ilumina la batalla).
                Lighting.AddLight(Projectile.Center, Color.Goldenrod.ToVector3() * 1.5f);

                // LAS LUCIÉRNAGAS — cada 2 ticks: nacen a un vector (10,10)
                // girado al azar y REMAN alejándose (×0.1..0.4) — las ascuas
                // del campo volando (el LightDust del original, hecho casa).
                if ((int)Tiempo % 2 == 0)
                {
                    Vector2 velPolvo = new Vector2(10f, 10f).RotatedByRandom(100);
                    Dust d = Dust.NewDustPerfect(dueno.Center + velPolvo,
                        ModContent.DustType<PolvoLuzLuciernaga>(),
                        velPolvo * Main.rand.NextFloat(0.1f, 0.4f));
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(0.5f, 0.7f);
                    d.color = ColorBase;
                    d.noLightEmittence = true;   // la luz la pone el campo, no cada ascua
                }
            }
        }

        // ================================================================
        //  EL VESTIDO — TODO el dibujo del aura (return false: la
        //  textura declarada jamás se dibuja). El contrato de lote de la
        //  casa: cerrar por sonda, dibujar, devolver el lote vanilla.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server)
                return false;

            Texture2D bloom = RecursosAuraJuicio.CirculoBloom;
            Texture2D estallido = RecursosAuraJuicio.EstallidoFragmentado;
            if (bloom == null || estallido == null)
                return false;   // null-safe: sin recursos no hay vestido

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                SpriteBatch sb = Main.spriteBatch;
                Vector2 posBase = Projectile.Center - Main.screenPosition;
                float t = Main.GlobalTimeWrappedHourly;

                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                // ==========================================================
                //  LAS CINCO CAPAS DE BLOOM CONCÉNTRICO — la torre de
                //  halos: cada capa más BLANCA (i·0.15) y más PEQUEÑA
                //  (0.75 − i·0.15), todas latiendo con el pulso rot2 —
                //  el corazón ardiente del original.
                // ==========================================================
                for (int i = 0; i < 5; i++)
                {
                    Color c = Color.Lerp(ColorBase, Color.White, i * 0.15f);
                    c.A = 0;
                    sb.Draw(bloom, posBase, null, c, 0f,
                        bloom.Size() * 0.5f,
                        (0.75f - i * 0.15f) * Rot2 * Projectile.scale,
                        SpriteEffects.None, 0f);
                }

                // ==========================================================
                //  LOS DOS DESTELLOS FRACTURADOS — el chisporroteo del
                //  núcleo: escala MÍNIMA (0.03 y 0.04·rot2) y giro vivo.
                //  (El original tira rand(−2..2) cada frame; la casa lo
                //  hace determinista: el azar de la semilla + el reloj —
                //  la ley de cero Main.rand en el render.)
                // ==========================================================
                Color cDestello = ColorBase;
                cDestello.A = 0;
                float semilla = Projectile.whoAmI * 2.718f;
                float giro1 = Azar(semilla) * 4f - 2f + t * 0.35f;
                float giro2 = Azar(semilla + 1.618f) * 4f - 2f - t * 0.45f;
                sb.Draw(estallido, posBase, null, cDestello, giro1,
                    estallido.Size() * 0.5f,
                    0.03f * Rot2 * Projectile.scale, SpriteEffects.None, 0f);
                sb.Draw(estallido, posBase, null, cDestello, giro2,
                    estallido.Size() * 0.5f,
                    0.04f * Rot2 * Projectile.scale, SpriteEffects.None, 0f);

                sb.End();
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El azar determinista de la casa (semilla → 0..1).</summary>
        private static float Azar(float semilla)
        {
            float v = MathF.Sin(semilla * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }
    }
}
