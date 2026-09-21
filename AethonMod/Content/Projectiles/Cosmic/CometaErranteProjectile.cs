using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CometaErranteProjectile — EL COMETA ERRANTE.
    ///
    /// LA ÓRBITA (420 ticks ≈ 6 vueltas): el cometa gira en una ELIPSE
    /// achatada alrededor del dueño (semieje mayor 180 px, menor 110) con
    /// precesión lenta del plano — el perihelio camina. El sentido del giro
    /// y la fase inicial viajan en ai[] (determinismo MP).
    ///
    /// LA COLA DOBLE:
    ///   · COLA DE POLVO: cinta de 20 puntos con las POSICIONES PREVIAS del
    ///     cometa, sesgadas radialmente AFUERA de la órbita — como la cola
    ///     real de un cometa, que huye del sol. DAÑA ×0.4 (i-frames 8).
    ///   · COLA DE IONES: rayo azul recto apuntando lejos del jugador.
    ///
    /// EL NÚCLEO: hielo-fuego blanco-azul (DrawSunBody) — DAÑA ×1.0
    /// (i-frames 15).
    ///
    /// LA CACERÍA: tras 6 vueltas se LANZA contra el enemigo más cercano
    /// (aceleración fija 0.55/tick, tope 17) y al tocar ESTALLA en estelas
    /// de hielo-fuego (×1.2, radio 120). Vida 600 ticks.
    /// </summary>
    public class CometaErranteProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int OrbitaTicks = 420;      // ≈ 6 vueltas
        private const int VidaTicks = 600;

        /// <summary>Semieje mayor de la órbita (px).</summary>
        private const float SemiejeMayor = 180f;

        /// <summary>Semieje menor (órbita ACHATADA).</summary>
        private const float SemiejeMenor = 110f;

        /// <summary>ω: 6 vueltas en 420 ticks (rad/tick).</summary>
        private const float Omega = MathHelper.TwoPi * 6f / OrbitaTicks;

        /// <summary>Precesión lenta del plano de la órbita (rad/tick).</summary>
        private const float Precesion = 0.0016f;

        /// <summary>Puntos de la cola de polvo (posiciones previas).</summary>
        private const int ColaPuntos = 20;

        /// <summary>Radio de colisión del núcleo.</summary>
        private const float RadioNucleo = 26f;

        /// <summary>Ancho de colisión de la cinta de polvo.</summary>
        private const float AnchoCola = 30f;

        /// <summary>i-frames del NÚCLEO (×1.0).</summary>
        private const int IframesNucleo = 15;

        /// <summary>i-frames de la COLA (×0.4).</summary>
        private const int IframesCola = 8;

        /// <summary>Daño relativo de la cola (el núcleo pega ×1.0).</summary>
        private const float DañoCola = 0.4f;

        /// <summary>Daño del ESTALLIDO final de hielo-fuego.</summary>
        private const float DañoEstallido = 1.2f;

        /// <summary>Radio del estallido final.</summary>
        private const float RadioEstallido = 120f;

        /// <summary>Aceleración fija de la cacería (px/tick²).</summary>
        private const float AcelCaza = 0.55f;

        /// <summary>Velocidad tope de la cacería.</summary>
        private const float VelCaza = 17f;

        // === LA PALETA (hielo-fuego: blanco-azul) ===
        private static readonly Color ColorHielo = new(235, 246, 255);   // núcleo blanco-azul
        private static readonly Color ColorCian = new(140, 205, 255);    // cian de fusión
        private static readonly Color ColorAzul = new(60, 120, 235);     // azul iónico
        private static readonly Color ColorProfundo = new(22, 42, 120);  // cola profunda

        private float _age;
        private bool _nacio;
        private bool _lanzado;
        private float _fase;        // ángulo orbital
        private float _giro = 1f;   // sentido del giro (±1)
        private float _prec;        // precesión del plano
        private int _objetivo = -1; // whoAmI de la presa
        private Vector2 _ultimoPivot;

        /// <summary>Historial de posiciones (cinta circular de 20).</summary>
        private readonly Vector2[] _hist = new Vector2[ColaPuntos];
        private int _histN;

        /// <summary>i-frames por objetivo: whoAmI → tick del último golpe.</summary>
        private readonly Dictionary<int, int> _golpeNucleo = new();
        private readonly Dictionary<int, int> _golpeCola = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks + 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 41);

        /// <summary>El pivote de la órbita: el dueño (o el último punto válido).</summary>
        private Vector2 Pivot()
        {
            Player p = Main.player[Projectile.owner];
            if (p != null && p.active && !p.dead)
            {
                _ultimoPivot = p.MountedCenter;
            }
            return _ultimoPivot;
        }

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _giro = Projectile.ai[0] >= 0f ? 1f : -1f;
                _fase = Projectile.ai[1];
                _prec = Projectile.ai[2];
                _ultimoPivot = Projectile.Center;
            }

            if (!_lanzado && _age <= OrbitaTicks)
            {
                // ============================================================
                //  LA ÓRBITA ELÍPTICA — con precesión lenta del plano
                // ============================================================
                _fase += Omega * _giro;
                _prec += Precesion;

                Vector2 nuevo = VFXCore.Ellipse(Pivot(), SemiejeMayor, SemiejeMenor, _prec, _fase);
                Projectile.velocity = nuevo - Projectile.Center;
                Projectile.Center = nuevo;
                Projectile.rotation = _fase * _giro;

                LuzCometa();
                RegistrarHistorial();
                GolpearNucleoYCola();
            }
            else
            {
                // ============================================================
                //  LA CACERÍA — el cometa se suelta tras 6 vueltas
                // ============================================================
                if (!_lanzado)
                {
                    _lanzado = true;
                    ElegirPresa();
                }

                Vector2 presa = PuntoPresa();
                Vector2 deseada = presa - Projectile.Center;
                if (deseada.LengthSquared() > 0.001f)
                    Projectile.velocity += Vector2.Normalize(deseada) * AcelCaza;

                if (Projectile.velocity.LengthSquared() > VelCaza * VelCaza)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * VelCaza;

                LuzCometa();
                RegistrarHistorial();

                // El núcleo sigue golpeando durante la cacería.
                GolpearNucleoYCola();

                // CONTACTO con la presa (o pared): el ESTALLIDO de hielo-fuego.
                bool tocaPresa = false;
                NPC presaNpc = _objetivo >= 0 ? Main.npc[_objetivo] : null;
                if (presaNpc != null && presaNpc.active && VFXCore.EsObjetivo(presaNpc))
                {
                    float alcance = RadioNucleo + Math.Max(presaNpc.width, presaNpc.height) * 0.5f;
                    tocaPresa = Vector2.DistanceSquared(presaNpc.Center, Projectile.Center) < alcance * alcance;
                }

                bool tocaPared = Collision.SolidCollision(Projectile.Center - Vector2.One * 6f, 12, 12);
                if (tocaPresa || tocaPared || _age >= VidaTicks - 2f)
                {
                    Estallar();
                    return;
                }
            }
        }

        /// <summary>La presa más cercana al soltar la órbita (determinista).</summary>
        private void ElegirPresa()
        {
            float mejor = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < mejor)
                {
                    mejor = d;
                    _objetivo = npc.whoAmI;
                }
            }
        }

        /// <summary>El punto vivo de la presa (o su última posición conocida).</summary>
        private Vector2 PuntoPresa()
        {
            NPC npc = _objetivo >= 0 && _objetivo < Main.maxNPCs ? Main.npc[_objetivo] : null;
            if (npc != null && npc.active)
            {
                _ultimoPivot = npc.Center;
                return npc.Center;
            }
            // Sin presa (o muerta): el cometa sigue huyendo hacia afuera.
            return _ultimoPivot;
        }

        /// <summary>Empuja la posición actual al historial circular.</summary>
        private void RegistrarHistorial()
        {
            _hist[_histN % ColaPuntos] = Projectile.Center;
            _histN++;
        }

        /// <summary>
        /// LA CINTA DE POLVO: los 20 puntos previos (del más reciente al más
        /// viejo), SESGADOS radialmente afuera del pivote — la cola siempre
        /// apunta lejos del jugador/sol, curvándose contra el movimiento.
        /// </summary>
        private Vector2[] ColaPolvo()
        {
            Vector2 pivot = Pivot();
            Vector2[] pts = new Vector2[ColaPuntos];
            int n = Math.Min(_histN, ColaPuntos);

            for (int k = 0; k < ColaPuntos; k++)
            {
                // La dirección AFUERA del pivote (jugador/sol) para este punto.
                Vector2 salida;

                if (k >= n)
                {
                    // Aún no hay historial: la cola nace estirándose desde el cometa.
                    salida = Projectile.Center - pivot;
                    salida = salida.LengthSquared() > 1f ? Vector2.Normalize(salida) : Vector2.UnitX;
                    pts[k] = Projectile.Center + salida * (k * 9f);
                    continue;
                }

                int idx = (_histN - 1 - k + ColaPuntos * 4) % ColaPuntos;
                Vector2 p = _hist[idx];

                // EL SESGO RADIAL: cada punto de la cola se empuja AFUERA de la
                // órbita (crece con la edad del punto) — solo mientras orbita.
                float bias = _lanzado ? 0.35f : 2.4f;
                salida = p - pivot;
                salida = salida.LengthSquared() > 1f ? Vector2.Normalize(salida)
                    : new Vector2(MathF.Cos(_fase), MathF.Sin(_fase));
                pts[k] = p + salida * (k * bias);
            }
            return pts;
        }

        private void LuzCometa()
        {
            Lighting.AddLight(Projectile.Center, 0.34f, 0.42f, 0.62f);
        }

        /// <summary>
        /// EL NÚCLEO (×1.0, i-frames 15) y la COLA (×0.4, i-frames 8): dos
        /// diccionarios separados. v6.50 — GolpeMotor (el cauce del motor:
        /// crítica real, varianza, on-hit y sync MP del propio motor); la
        /// quemadura del núcleo es de servidor.
        /// </summary>
        private void GolpearNucleoYCola()
        {
            int dmgNucleo = Math.Max(1, Projectile.damage);
            int dmgCola = Math.Max(1, (int)(Projectile.damage * DañoCola));
            Vector2[] cola = ColaPolvo();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;

                // --- EL NÚCLEO ---
                float alcance = RadioNucleo + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) < alcance * alcance)
                {
                    if (!_golpeNucleo.TryGetValue(npc.whoAmI, out int u) || _age - u >= IframesNucleo)
                    {
                        _golpeNucleo[npc.whoAmI] = (int)_age;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmgNucleo, 3f, true);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 240); } catch { }
                    }
                }

                // --- LA CINTA DE POLVO (punto-segmento por toda la cola) ---
                if (!_golpeCola.TryGetValue(npc.whoAmI, out int uc) || _age - uc >= IframesCola)
                {
                    float radio = AnchoCola * 0.5f + Math.Max(npc.width, npc.height) * 0.5f;
                    if (TocaPolilinea(npc.Center, cola, radio))
                    {
                        _golpeCola[npc.whoAmI] = (int)_age;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmgCola, 1f, true);
                    }
                }
            }
        }

        /// <summary>EL ESTALLIDO DE HIELO-FUEGO (×1.2, radio 120) + despedida visual.</summary>
        private void Estallar()
        {
            // v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
            // on-hit y sync MP del propio motor); la quemadura es de servidor.
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoEstallido));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float alcance = RadioEstallido + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > alcance * alcance) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 300); } catch { }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                OndaLib.Kick(2.4f, 9);
                OndaLib.Flash(ColorCian, 0.20f, 8);
                try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27.WithPitchOffset(-0.25f), Projectile.Center); }
                catch { }
                RiftLib.ChispasAnomalia(Projectile.Center, 18, PyraPalettes.ColdFire, Seed + 5, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            RiftLib.ChispasAnomalia(Projectile.Center, 8, PyraPalettes.ColdFire, Seed + 9, out ParticleData[] motas);
            if (motas != null)
                for (int i = 0; i < motas.Length; i++)
                    ParticleManager.Spawn(motas[i]);
        }

        /// <summary>Punto → polilínea: distancia a algún segmento < radio.</summary>
        private static bool TocaPolilinea(Vector2 pt, Vector2[] pts, float radio)
        {
            if (pts == null || pts.Length < 2) return false;
            float r2 = radio * radio;
            for (int i = 0; i < pts.Length - 1; i++)
                if (DistSeg2(pt, pts[i], pts[i + 1]) < r2) return true;
            return false;
        }

        /// <summary>Distancia² punto → segmento.</summary>
        private static float DistSeg2(Vector2 pt, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 0.001f) return (pt - a).LengthSquared();
            float t = MathHelper.Clamp(Vector2.Dot(pt - a, ab) / len2, 0f, 1f);
            return (pt - a - ab * t).LengthSquared();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar el lote de tML antes del
            //  pase propio, restaurarlo al final, no dibujar nada más.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawCometa(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        /// <summary>El lote aditivo (halo, cola de iones, cinta de polvo) y
        /// DESPUÉS el núcleo con DrawSunBody (técnica del sol de la casa).</summary>
        private void DrawCometa()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 pivot = Pivot();

            float R = 21f * (0.8f + 0.2f * MathHelper.Clamp(_age / 8f, 0f, 1f));
            float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // 1. EL HALO DEL NÚCLEO (hielo-fuego).
            LumenLib.Bloom(Main.spriteBatch, drawPos, R * 2.6f, ColorCian, 0.38f * fade, 3);

            // 2. LA COLA DE IONES: rayo azul RECTO apuntando lejos del jugador.
            Vector2 outDir = Projectile.Center - pivot;
            outDir = outDir.LengthSquared() > 1f ? Vector2.Normalize(outDir) : -Vector2.Normalize(Projectile.velocity);
            if (outDir.LengthSquared() < 0.5f) outDir = Vector2.UnitX;
            LumenLib.Ray(Main.spriteBatch, drawPos + outDir * (R * 0.7f), outDir, 130f, R * 0.9f,
                ColorAzul, 0.42f * fade, 0.85f);

            // 3. LA CINTA DE POLVO: 20 puntos previos sesgados afuera.
            Vector2[] cola = ColaPolvo();
            for (int k = 0; k < cola.Length - 1; k++)
            {
                float t = k / (float)(cola.Length - 1);
                Vector2 a = cola[k] - Main.screenPosition;
                Vector2 b = cola[k + 1] - Main.screenPosition;
                Vector2 seg = b - a;
                float len = seg.Length();
                if (len < 0.5f) continue;

                float w = MathHelper.Lerp(17f, 2.5f, t);
                float alpha = (0.34f - 0.26f * t) * fade;
                Color c = Color.Lerp(ColorHielo, ColorProfundo, t * t);

                var texSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, (a + b) * 0.5f, null,
                    Tint(c, alpha), MathF.Atan2(seg.Y, seg.X), texSize * 0.5f,
                    new Vector2(len + w * 0.6f, w) / texSize, SpriteEffects.None, 0f);

                // Motas de polvo que titilan (deterministas: semilla + índice).
                if (k % 3 == 0)
                {
                    float tw = 0.4f + 0.6f * MathF.Abs(MathF.Sin(time * 3.1f + k * 2.7f + seed));
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, b, null,
                        Tint(ColorCian, 0.35f * tw * (1f - t) * fade), 0f, texSize * 0.5f,
                        new Vector2(w * 0.55f, w * 0.55f) / texSize, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.End();

            // 4. EL NÚCLEO DE HIELO-FUEGO (DrawSunBody gestiona sus lotes).
            float eje = Projectile.velocity.LengthSquared() > 0.5f
                ? Projectile.velocity.ToRotation()
                : _fase;
            RuneSunRenderer.DrawSunBody(drawPos, R, eje, time,
                new Color(238, 247, 255),   // main — blanco hielo
                new Color(130, 185, 250),   // darker — azul fusión
                new Color(40, 95, 225),     // accent — azul profundo
                new Color(150, 215, 255),   // backHot — halo cian
                new Color(70, 130, 255),    // backRed — halo azul
                new Color(220, 240, 255),   // shine — destello frío
                3.4f, fade * 0.9f);
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
