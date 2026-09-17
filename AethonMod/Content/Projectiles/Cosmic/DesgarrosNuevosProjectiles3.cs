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
    /// CorazonColapsoProjectile — EL CORAZÓN DEL COLAPSO (v6.35).
    ///
    /// La estrella de 4 puntas de fuego estelar: 240 ticks de vida en el
    /// punto del cursor — 26 de ENCENDIDO, 178 de COLAPSO y 36 de cierre.
    /// El daño vive en el círculo: cada 20 ticks TODO EsObjetivo dentro
    /// del radio es quemado (×1.0) y cada 30 ticks los filamentos LANZAN
    /// LLAMARADAS (lenguas de fuego — StormLib.Bolt naranja) contra los
    /// 2 enemigos más cercanos (×0.5).
    /// </summary>
    public class CorazonColapsoProjectile : ModProjectile
    {
        private const int Apertura = 26;
        private const int Cierre = 36;
        private const int Vida = 240;

        /// <summary>Radio del corazón (px).</summary>
        private const float Radio = 150f;

        /// <summary>i-frames de la quemadura del círculo.</summary>
        private const int Iframes = 20;

        /// <summary>Cadencia de las llamaradas.</summary>
        private const int CadaLlamarada = 30;

        /// <summary>Daño relativo de las llamaradas.</summary>
        private const float DañoLlamarada = 0.5f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radio * 2);
            Projectile.height = (int)(Radio * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide — la lección de los desgarros v6.33 (tML no
            // dibuja los proyectivos ocultos y PreDraw jamás corría).
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            // La luz del fuego estelar (naranja cálido).
            Lighting.AddLight(Projectile.Center, 0.50f, 0.28f, 0.06f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // === LA QUEMADURA DEL CÍRCULO (cada 20 ticks) ===
                if (_age > Apertura && _age < Vida - Cierre && (_age - Apertura) % Iframes == 0)
                {
                    int dmg = Math.Max(1, Projectile.damage);
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > Radio * Radio) continue;
                        npc.SimpleStrikeNPC(dmg, npc.direction, false, 3f, DamageClass.Magic);
                    }
                }

                // === LAS LLAMARADAS (cada 30 ticks — los 2 más cercanos) ===
                if (_age > Apertura + 10 && (_age - Apertura) % CadaLlamarada == 0)
                    DispararLlamaradas();
            }
        }

        /// <summary>Las lenguas de fuego: los 2 EsObjetivo más cercanos en 420 px.</summary>
        private void DispararLlamaradas()
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoLlamarada));
            int quedan = 2;
            for (int paso = 0; paso < 2 && quedan > 0; paso++)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (_golpeados.Contains(npc.whoAmI)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 420f * 420f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor == null) break;
                _golpeados.Add(mejor.whoAmI);
                // La llamarada pega y se recuerda 8 ticks para el visual.
                _llamaradas.Add((mejor.Center, 8));
                mejor.SimpleStrikeNPC(dmg, mejor.direction, false, 2f, DamageClass.Magic);
                quedan--;
            }
            if (_golpeados.Count > 16) _golpeados.Clear();
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
                float time = Main.GlobalTimeWrappedHourly;

                // EL CORAZÓN (la primitiva v6.35 — la estrella de 4 puntas).
                RiftLib.VorticeColapso(Projectile.Center - Main.screenPosition,
                    Radio, progress, time, Seed);

                // LAS LLAMARADAS VIVAS (las que acaban de lanzarse — 8 ticks):
                // lenguas de fuego fractales del borde del corazón al enemigo.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = _llamaradas.Count - 1; i >= 0; i--)
                {
                    var (target, vida) = _llamaradas[i];
                    if (vida <= 0) { _llamaradas.RemoveAt(i); continue; }
                    _llamaradas[i] = (target, vida - 1);
                    Vector2 dir = Vector2.Normalize(target - Projectile.Center);
                    if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
                    Vector2 bord = Projectile.Center - Main.screenPosition + dir * (Radio * 0.45f);
                    StormLib.Bolt(Main.spriteBatch, bord,
                        target - Main.screenPosition, Seed + i * 37, (int)(time * 21f),
                        5f, new Color(255, 120, 20), new Color(255, 250, 205),
                        0.60f * (vida / 8f), 4, 10f);
                }
                Main.spriteBatch.End();
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

        /// <summary>El progress 0→1→0 de la vida del corazón.</summary>
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
        private readonly System.Collections.Generic.List<(Vector2 target, int vida)> _llamaradas = new();
        private readonly System.Collections.Generic.HashSet<int> _golpeados = new();

        private int Seed { get; set; }
    }

    /// <summary>
    /// GargantaVacioProjectile — LA GARGANTA DEL VACÍO (v6.35).
    ///
    /// El vórtice devorador: 300 ticks — 30 de APERTURA, 230 de HAMBRE
    /// y 40 de cierre. La SUCCIÓN FUERTE tira de los enemigos en 320 px
    /// (los jefes no se dejan arrastrar), el NÚCLEO NEGRO DEVORA cada
    /// 15 ticks (×1.15, knockback 0) y LA LENTE gravitacional curva el
    /// fondo alrededor de la garganta todo el tiempo que vive.
    /// </summary>
    public class GargantaVacioProjectile : ModProjectile
    {
        private const int Apertura = 30;
        private const int Cierre = 40;
        private const int Vida = 300;

        /// <summary>Radio de la garganta (px).</summary>
        private const float Radio = 140f;

        /// <summary>Radio del NÚCLEO NEGRO (la boca).</summary>
        private const float RadioNucleo = 55f;

        /// <summary>Radio de la succión.</summary>
        private const float RadioSucion = 320f;

        /// <summary>Fuerza de la succión (px/tick² — FUERTE: es hambre).</summary>
        private const float Traccion = 0.30f;

        /// <summary>Cadencia del devorar del núcleo.</summary>
        private const int CadaDevora = 15;

        /// <summary>Daño relativo del devorar (×1.15).</summary>
        private const float DañoDevora = 1.15f;

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
            // v6.35: SIN hide (la lección v6.33).
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            // La luz del vórtice (magenta con alma violeta).
            Lighting.AddLight(Projectile.Center, 0.42f, 0.06f, 0.30f);

            // === LA LENTE (el signature): el fondo se curva alrededor de
            //     la garganta — patrón portal estable de GravLens (re-
            //     registro por tick con vida corta). ===
            GravLens.Registrar(Projectile.Center, Radio * 2.6f, 0.55f, 0.10f);

            if (Main.netMode != NetmodeID.MultiplayerClient && _age > Apertura)
            {
                // === LA SUCCIÓN FUERTE (la garganta tiene hambre) ===
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (npc.boss) continue;
                    Vector2 alCentro = Projectile.Center - npc.Center;
                    float d = alCentro.Length();
                    if (d > RadioSucion || d < 8f) continue;
                    npc.velocity += Vector2.Normalize(alCentro) * Traccion * (1f - d / RadioSucion + 0.5f);
                }

                // === EL DEVORAR DEL NÚCLEO NEGRO (cada 15 ticks) ===
                if ((_age - Apertura) % CadaDevora == 0)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoDevora));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioNucleo * RadioNucleo) continue;
                        npc.SimpleStrikeNPC(dmg, 0, false, 0f, DamageClass.Magic);   // kb 0: devorado
                    }
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
                RiftLib.GargantaVacio(Projectile.Center - Main.screenPosition,
                    Radio, progress, Main.GlobalTimeWrappedHourly, Seed);
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

        private int Seed { get; set; }
    }

    /// <summary>
    /// UmbralRotoProjectile — EL UMBRAL ROTO (v6.35).
    ///
    /// El corte horizontal perfecto: 210 ticks — 24 de RAJADO (el umbral
    /// se abre de golpe), 156 de VIGILIA y 30 de cierre. Pica en LÍNEA
    /// cada 12 ticks (×0.4, ancho 14) a lo largo de los 700 px del corte
    /// y cada 60 ticks LA OTRA REALIDAD MUERDE: 3 mandíbulas espectrales
    /// (×0.8) contra los enemigos más cercanos a la línea.
    /// </summary>
    public class UmbralRotoProjectile : ModProjectile
    {
        private const int Apertura = 24;
        private const int Cierre = 30;
        private const int Vida = 210;

        /// <summary>Ancho del corte horizontal (px).</summary>
        private const float Ancho = 700f;

        /// <summary>Ancho del golpe en línea (px).</summary>
        private const float AnchoGolpe = 14f;

        /// <summary>Cadencia de la picadura en línea.</summary>
        private const int CadaPico = 12;

        /// <summary>Daño relativo por picotazo.</summary>
        private const float DañoPico = 0.4f;

        /// <summary>Cadencia de la mordida de la otra realidad.</summary>
        private const int CadaMordida = 60;

        /// <summary>Daño relativo de cada mandíbula.</summary>
        private const float DañoMordida = 0.8f;

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
            // v6.35: SIN hide (la lección v6.33).
        }

        public override void AI()
        {
            _age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = _ancla;

            // La luz del umbral (cian frío parpadeante).
            float flick = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 19f);
            Lighting.AddLight(Projectile.Center, 0.15f * flick, 0.40f * flick, 0.45f * flick);

            if (Main.netMode != NetmodeID.MultiplayerClient &&
                _age > Apertura && _age < Vida - Cierre)
            {
                // === LA PICADURA EN LÍNEA (cada 12 ticks) ===
                if ((_age - Apertura) % CadaPico == 0)
                {
                    int dmg = Math.Max(1, (int)(Projectile.damage * DañoPico));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (!RiftLib.LineaToca(_ancla, Vector2.UnitX, Ancho, AnchoGolpe + 8f, npc.Hitbox)) continue;
                        npc.SimpleStrikeNPC(dmg, npc.direction, false, 1.2f, DamageClass.Magic);
                    }
                }

                // === LA OTRA REALIDAD MUERDE (cada 60 ticks — 3 mandíbulas) ===
                if ((_age - Apertura) % CadaMordida == 0)
                    Morder();
            }
        }

        /// <summary>Las mandíbulas: los 3 EsObjetivo más cercanos a la línea.</summary>
        private void Morder()
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoMordida));
            int quedan = 3;
            for (int paso = 0; paso < 3 && quedan > 0; paso++)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (_golpeados.Contains(npc.whoAmI)) continue;
                    // La distancia a la LÍNEA (la componente Y manda).
                    float dy = MathF.Abs(npc.Center.Y - _ancla.Y);
                    float dx = MathF.Abs(npc.Center.X - _ancla.X);
                    if (dx > Ancho * 0.75f || dy > 260f) continue;
                    float d = dy * dy + dx * 0.05f;
                    if (d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor == null) break;
                _golpeados.Add(mejor.whoAmI);
                _mordidas.Add((mejor.Center, 10));
                mejor.SimpleStrikeNPC(dmg, 0, false, 0f, DamageClass.Magic);   // kb 0: la mandíbula sujetó
                quedan--;
            }
            if (_golpeados.Count > 12) _golpeados.Clear();
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
                float time = Main.GlobalTimeWrappedHourly;

                // EL UMBRAL (la primitiva v6.35 — el corte horizontal).
                RiftLib.UmbralRoto(Projectile.Center - Main.screenPosition,
                    Ancho, progress, time, Seed);

                // LAS MORDIDAS VIVAS (las mandíbulas que acaban de cerrar —
                // 10 ticks): el destello espectral del otro lado.
                if (_mordidas.Count > 0)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    for (int i = _mordidas.Count - 1; i >= 0; i--)
                    {
                        var (target, vida) = _mordidas[i];
                        if (vida <= 0) { _mordidas.RemoveAt(i); continue; }
                        _mordidas[i] = (target, vida - 1);
                        float t = vida / 10f;
                        StormLib.SparkBurst(Main.spriteBatch,
                            target - Main.screenPosition, new Color(224, 224, 224),
                            26f, 4, Seed + 61 + i, 1f - t, 30f);
                    }
                    Main.spriteBatch.End();
                }
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
        private readonly System.Collections.Generic.List<(Vector2 target, int vida)> _mordidas = new();
        private readonly System.Collections.Generic.HashSet<int> _golpeados = new();

        private int Seed { get; set; }
    }

    /// <summary>
    /// LeviatanEspectralProjectile — EL LEVIATÁN ESPECTRAL (v6.35).
    ///
    /// La criatura de hueso etéreo NADANDO: avanza hacia delante con la
    /// ONDA en S (su rumbo oscila ~±0.30 rad a 2.5 ciclos/s — el pez
    /// empuja con la cola) y gira suave hacia el enemigo más cercano
    /// (0.03 rad/tick). Atraviesa todo (penetrate −1) durante 4 s. El
    /// cuerpo se dibuja en RiftLib.LeviatanEspectral con la MISMA fase
    /// de onda — el nado y la carne son una sola cosa.
    /// </summary>
    public class LeviatanEspectralProjectile : ModProjectile
    {
        private const int Vida = 240;

        /// <summary>Velocidad de nado (px/tick).</summary>
        private const float Velocidad = 14f;

        /// <summary>Amplitud del balanceo del rumbo (rad).</summary>
        private const float BalanceoRumbo = 0.30f;

        /// <summary>Frecuencia del nado (ciclos/s).</summary>
        private const float FrecNado = 2.5f;

        /// <summary>Giro del homing (rad/tick).</summary>
        private const float Homing = 0.03f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            // v6.35: SIN hide (la lección v6.33).
        }

        public override void AI()
        {
            _age++;

            // === EL HOMING SUAVE: el rumbo base gira despacio hacia el
            //     enemigo más cercano (el leviatán husmea). ===
            if (_age % 4 == 0)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                    if (d < 640f * 640f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                if (mejor != null)
                {
                    float deseado = (mejor.Center - Projectile.Center).ToRotation();
                    float delta = MathHelper.WrapAngle(deseado - _rumbo);
                    _rumbo += MathHelper.Clamp(delta, -Homing * 4f, Homing * 4f);
                }
            }

            // === EL NADO EN S: el rumbo efectivo oscila alrededor del
            //     base (la MISMA fase que dibuja el cuerpo — coherencia). ===
            float fase = Main.GlobalTimeWrappedHourly * FrecNado * MathHelper.TwoPi;
            float rumboVivo = _rumbo + MathF.Sin(fase) * BalanceoRumbo;
            Projectile.velocity = new Vector2(MathF.Cos(rumboVivo), MathF.Sin(rumboVivo)) * Velocidad;
            Projectile.rotation = rumboVivo;

            // La luz fría del espectro.
            Lighting.AddLight(Projectile.Center, 0.14f, 0.24f, 0.34f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float edad01 = 1f - Projectile.timeLeft / (float)Vida;
                RiftLib.LeviatanEspectral(Projectile.Center - Main.screenPosition,
                    new Vector2(MathF.Cos(Projectile.rotation), MathF.Sin(Projectile.rotation)),
                    edad01, Main.GlobalTimeWrappedHourly, Seed, 1f);
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

        public override void OnSpawn(IEntitySource source)
        {
            _rumbo = Projectile.ai[0];
            Seed = (int)Projectile.ai[1] % 9973;
        }

        private float _rumbo;
        private float _age;

        private int Seed { get; set; }
    }
}
