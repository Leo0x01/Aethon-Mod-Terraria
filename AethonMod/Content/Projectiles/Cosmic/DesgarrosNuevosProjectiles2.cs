using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// PliegueEspacioProjectile — EL PLIEGUE DEL ESPACIO (v6.33).
    ///
    /// EL OJO (la referencia "apertura de portal dimensional" — la doble
    /// elipse con el núcleo negro): 240 ticks — 26 de APERTURA, 178 de
    /// CURVATURA y 36 de cierre. El pliegue DOBLA el espacio: los
    /// EsObjetivo en 350 px son ARRASTRADOS hacia el cuello del ojo con
    /// la succión FUERTE (0.30 — el espacio se curva hacia dentro) y al
    /// tocar el NÚCLEO NEGRO son COMPRIMIDOS (×1.3, knockback 0, cada 20
    /// ticks). La INCLINACIÓN del ojo sigue al enemigo más cercano (el
    /// pliegue lo mira).
    /// </summary>
    public class PliegueEspacioProjectile : ModProjectile
    {
        private const int Apertura = 26;
        private const int Cierre = 36;
        private const int Vida = 240;

        /// <summary>Radio del ojo (px).</summary>
        private const float Radio = 160f;

        /// <summary>Radio del NÚCLEO NEGRO (la compresión).</summary>
        private const float RadioNucleo = 46f;

        /// <summary>Radio de la curvatura (la succión fuerte).</summary>
        private const float RadioCurvatura = 350f;

        /// <summary>Fuerza del arrastre (px/tick² — FUERTE: dobla el espacio).</summary>
        private const float Traccion = 0.30f;

        /// <summary>Cadencia de la compresión.</summary>
        private const int CadaCompresion = 20;

        /// <summary>Daño relativo de la compresión (×1.3 — comprimido).</summary>
        private const float DañoCompresion = 1.3f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = (int)(RadioNucleo * 2);
            Projectile.height = (int)(RadioNucleo * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide — mismo fix que los desgarros de la primera
            // tanda: tML no dibuja los proyectivos ocultos y PreDraw nunca
            // corría. Sin hide el VFX de RiftLib vive.
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            Lighting.AddLight(Projectile.Center, 0.25f, 0.55f, 0.65f);

            NPC cercano = null; float mejorD = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; cercano = npc; }

                if (Main.netMode == NetmodeID.MultiplayerClient) continue;
                if (_age <= Apertura) continue;

                // === LA CURVATURA: arrastra a EsObjetivo hacia el cuello ===
                if (!npc.boss)
                {
                    Vector2 alCentro = Projectile.Center - npc.Center;
                    float dist = alCentro.Length();
                    if (dist < RadioCurvatura && dist > 6f)
                        npc.velocity += Vector2.Normalize(alCentro) * Traccion * (1f - dist / RadioCurvatura + 0.5f);
                }

                // === LA COMPRESIÓN DEL NÚCLEO NEGRO (cada 20 ticks) ===
                if ((_age - Apertura) % CadaCompresion == 0 &&
                    Vector2.DistanceSquared(npc.Center, Projectile.Center) < RadioNucleo * RadioNucleo)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoCompresion));
                    npc.SimpleStrikeNPC(dmg, 0, false, 0f, DamageClass.Magic);   // knockback 0: comprimido
                }
            }

            // LA INCLINACIÓN del ojo: sigue al enemigo más cercano (el pliegue
            // lo mira — lerp suave para que no tiemble).
            float tiltObjetivo = cercano != null
                ? (cercano.Center - Projectile.Center).ToRotation() * 0.5f
                : -0.55f + 0.10f * MathF.Sin(Main.GlobalTimeWrappedHourly * 0.6f);
            _tilt = MathHelper.Lerp(_tilt, tiltObjetivo, 0.04f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float progress = Progress();
                RiftLib.OjoEspacial(Projectile.Center - Main.screenPosition,
                    Radio, progress, Main.GlobalTimeWrappedHourly, Seed, 1f, _tilt);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        private float Progress()
        {
            if (_age < Apertura) return _age / (float)Apertura;
            if (_age > Vida - Cierre) return Math.Max(0f, (Vida - _age) / (float)Cierre);
            return 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _ancla = Projectile.Center;
            Seed = (int)Projectile.ai[0] % 9973;
        }

        private Vector2 _ancla;
        private float _age;
        private float _tilt = -0.55f;

        private int Seed { get; set; }
    }

    /// <summary>
    /// HeridaElectricaProjectile — LA HERIDA ELÉCTRICA (v6.33).
    ///
    /// LA GRIETA (la referencia "desgarro de realidad eléctrica"): la
    /// línea de ~620 px que abre desde el jugador HACIA el cursor — 210
    /// ticks de vida: 24 de APERTURA (la herida se abre de golpe), 160 de
    /// PICADURA y 26 de cierre. El interior lleva la ESTÁTICA y los ARCOS
    /// VOLTAICOS de StormLib.StormArc (la corriente NUNCA se corta: dos
    /// arcos entrelazados se relevan a 6 Hz — la técnica de los relevos). Pica
    /// en línea cada 10 ticks (×0.35) y APLICA ELECTRIFICADO 120 ticks
    /// (el debuff vanilla que castiga el movimiento).
    /// </summary>
    public class HeridaElectricaProjectile : ModProjectile
    {
        private const int Apertura = 24;
        private const int Cierre = 26;
        private const int Vida = 210;

        /// <summary>Longitud de la herida (px).</summary>
        private const float Largo = 620f;

        /// <summary>Ancho máximo del quad (px).</summary>
        private const float AnchoMax = 22f;

        /// <summary>Cadencia de la picadura en línea.</summary>
        private const int CadaPicotazo = 10;

        /// <summary>Daño relativo por picotazo.</summary>
        private const float DañoPico = 0.35f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide — mismo fix que los desgarros de la primera
            // tanda: tML no dibuja los proyectivos ocultos y PreDraw nunca
            // corría. Sin hide el VFX de RiftLib vive.
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _origin + _dir * (Largo * 0.5f);

            // La luz del arco (cian-violeta estroboscópico).
            float strobe = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 22f);
            Lighting.AddLight(Projectile.Center, 0.20f * strobe, 0.45f * strobe, 0.65f * strobe);

            if (Main.netMode != NetmodeID.MultiplayerClient &&
                _age > Apertura && _age < Vida - Cierre &&
                (_age - Apertura) % CadaPicotazo == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoPico));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (!RiftLib.LineaToca(_origin, _dir, Largo, AnchoMax + 8f, npc.Hitbox)) continue;
                    npc.SimpleStrikeNPC(dmg, npc.direction, false, 1.2f, DamageClass.Magic);
                    // ELECTRIFICADO: el debuff vanilla que castiga el movimiento.
                    try { npc.AddBuff(BuffID.Electrified, 120); } catch { }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float progress = Progress();
                RiftLib.HeridaElectrica(_origin - Main.screenPosition, _dir,
                    Largo, AnchoMax, progress, Main.GlobalTimeWrappedHourly, Seed);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        private float Progress()
        {
            if (_age < Apertura) return _age / (float)Apertura;
            if (_age > Vida - Cierre) return Math.Max(0f, (Vida - _age) / (float)Cierre);
            return 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            float rot = Projectile.ai[0];
            _dir = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
            _origin = Projectile.Center - _dir * 12f;
            Seed = (int)Projectile.ai[1] % 9973;
        }

        private Vector2 _origin;
        private Vector2 _dir = Vector2.UnitX;
        private float _age;

        private int Seed { get; set; }
    }
}
