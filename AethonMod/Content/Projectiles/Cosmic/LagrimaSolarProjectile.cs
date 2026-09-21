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
    /// LagrimaSolarProjectile — v6.31 — LA LÁGRIMA DEL SOL MORIBUNDO
    /// (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md ficha 3 + §6.3,
    /// destilada a la casa).
    ///
    /// LA VIDA DE UNA LÁGRIMA (320 ticks):
    ///   · LA ÓRBITA (150 ticks = 2,5 s): la gota nace incandescente y gira
    ///     en círculo perfecto alrededor de su punto de nacimiento — radio
    ///     y velocidad angular PROPIOS viajan en ai[] (determinismo MP) —
    ///     SIN DAÑAR mientras se enciende: la temperatura sube de chamuscado
    ///     a oro y el núcleo blanco crece.
    ///   · EL ENCENDIDO (tick 150): destello + click + 4 brasas — la lágrima
    ///     "entra en filo".
    ///   · LA CACERÍA (150..280 suave, luego mordisco): persigue al enemigo
    ///     más cercano curvando con giro acelerado; tras 280 ticks apunta
    ///     DIRECTO a velocidad fija. Hasta 18 golpes con i-frames propios
    ///     de 5 ticks por objetivo y quemadura cósmica de 7 s.
    ///   · EL FINAL: al desvanecerse (o verter las 18 lágrimas) EXPLOTA —
    ///     daño de área ×1.5 en radio 90 con kick pequeño — y muere.
    ///
    /// EL VISUAL (lo distintivo de la ficha): NO es un orbe — es una GOTA
    /// DE METAL FUNDIDO dibujada proceduralmente: cadena de quads
    /// decrecientes con perfil de lágrima (cola fina + cabeza redonda con
    /// casquete), COSTRAS oscuras deterministas pegadas al borde (se funden
    /// al arder), MENISCO brillante en la cabeza, núcleo blanco que crece
    /// con el calor, y el cuerpo SE ESTIRA con la velocidad. Estela: cinta
    /// de 14 puntos propios con fase anclada (EstelaLib.Ribbon Comet).
    ///
    /// Contrato de la casa: batch PreDraw como SembradorPulsar · daño
    /// por v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
    /// on-hit y sync MP del propio motor) + EsObjetivo · cero Main.rand visual
    /// (Hash01 + identity).
    /// </summary>
    public class LagrimaSolarProjectile : ModProjectile
    {
        // === LA VIDA (ticks reales) ===
        private const int OrbitaTicks = 150;    // 2,5 s de órbita sin dañar
        private const int MordiscoTicks = 280;  // suave (150..280) → mordisco fijo
        private const int VidaTicks = 320;

        /// <summary>Los 18 golpes contados a mano (sin penetrate del motor).</summary>
        private const int MaxGolpes = 18;

        /// <summary>i-frames PROPIOS por objetivo (un mordisco cada 5 ticks).</summary>
        private const int Iframes = 5;

        /// <summary>El radio de contacto de la gota.</summary>
        private const float RadioGolpe = 28f;

        /// <summary>El alcance de la búsqueda del enemigo más cercano.</summary>
        private const float RadioBusqueda = 1600f;

        /// <summary>El radio de la explosión final.</summary>
        private const float RadioExplosion = 90f;

        /// <summary>El multiplicador de daño del área de la explosión.</summary>
        private const float DañoExplosion = 1.5f;

        /// <summary>La duración de la quemadura cósmica: 7 s.</summary>
        private const int QuemaduraTicks = 420;

        // === EL VISUAL ===
        private const int PuntosCinta = 14;
        private const int Segmentos = 8;
        private const int Costras = 4;

        private float _age;
        private bool _nacio;
        private Vector2 _centro;        // el centro de la órbita (detrás del nacimiento)
        private float _fase;            // el ángulo orbital
        private float _omega;           // la velocidad angular (con signo)
        private float _radio;           // el radio orbital propio
        private float _spd;             // el módulo de la persecución
        private Vector2 _eje;           // el eje visual (la dirección de marcha)
        private int _golpes;
        private bool _encendida;
        private float _encendidaAge;
        private bool _exploto;

        /// <summary>El camino propio de la cinta (mundo): [0] cola vieja → [último] cabeza.</summary>
        private readonly Vector2[] _cinta = new Vector2[PuntosCinta];

        /// <summary>i-frames por objetivo: whoAmI del NPC → tick del último mordisco.</summary>
        private readonly Dictionary<int, int> _ultimoGolpe = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks + 2;
            Projectile.ignoreWater = true;
            // La gota de un sol moribundo atraviesa paredes: es luz, no materia.
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red: la misma en todas las máquinas).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 61);

        /// <summary>EL ENCENDIDO 0..1: chamuscado → oro durante la órbita, núcleo blanco al filo.</summary>
        private float Heat01
        {
            get
            {
                if (_age <= OrbitaTicks)
                    return 0.75f * MathHelper.Clamp(_age / OrbitaTicks, 0f, 1f);
                return 0.75f + 0.25f * MathHelper.Clamp((_age - OrbitaTicks) / 25f, 0f, 1f);
            }
        }

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: decodificar el paquete orbital (ai[]) ===
            if (!_nacio)
            {
                _nacio = true;
                _radio = Projectile.ai[0] > 1f ? Projectile.ai[0] : 34f;
                _omega = Math.Abs(Projectile.ai[1]) > 0.01f ? Projectile.ai[1] : 0.08f;
                // El centro de la órbita queda ATRÁS del nacimiento, en la línea
                // de tiro: la gota nace en el borde de su círculo y lo rodea.
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                _centro = Projectile.Center - dir * _radio;
                _fase = dir.ToRotation();
                _eje = dir;
                _spd = MathF.Abs(_omega) * _radio;
            }

            if (_age <= OrbitaTicks)
            {
                // ============================================================
                //  LA ÓRBITA — girar SIN DAÑAR alrededor del nacimiento
                // ============================================================
                _fase += _omega;
                Projectile.Center = _centro + new Vector2(MathF.Cos(_fase), MathF.Sin(_fase)) * _radio;
                // La tangente (el momentum que heredará la cacería) y el eje visual.
                Vector2 tangente = new Vector2(-MathF.Sin(_fase), MathF.Cos(_fase)) * MathF.Sign(_omega);
                Projectile.velocity = tangente * _spd;
                _eje = Vector2.Normalize(Vector2.Lerp(_eje, tangente, 0.30f));
            }
            else
            {
                // ============================================================
                //  LA CACERÍA — suave al principio, mordisco al final
                // ============================================================
                NPC objetivo = BuscarObjetivo();
                float tSuave = MathHelper.Clamp((_age - OrbitaTicks) / (MordiscoTicks - OrbitaTicks), 0f, 1f);

                if (objetivo != null)
                {
                    Vector2 deseada = objetivo.Center - Projectile.Center;
                    if (deseada.LengthSquared() > 0.01f)
                        deseada = Vector2.Normalize(deseada);

                    if (_age < MordiscoTicks)
                    {
                        // LA CURVA SUAVE: acelera de la órbita (≈3 px/tick) a 16,
                        // girando cada vez más rápido hacia el objetivo.
                        _spd = MathHelper.Min(_spd + 0.35f, 16f);
                        float giroMax = MathHelper.Lerp(0.09f, 0.55f, tSuave);
                        float actual = Projectile.velocity.ToRotation();
                        float delta = MathHelper.WrapAngle(deseada.ToRotation() - actual);
                        actual += MathHelper.Clamp(delta, -giroMax, giroMax);
                        Projectile.velocity = actual.ToRotationVector2() * _spd;
                    }
                    else
                    {
                        // EL MORDISCO: dirección fija, módulo fijo — sin piedad.
                        _spd = 20f;
                        Projectile.velocity = deseada * _spd;
                    }
                }
                else
                {
                    // Sin presa a la vista: deriva estable de brasa.
                    _spd = MathHelper.Min(_spd + 0.10f, 8f);
                    Projectile.velocity = Projectile.velocity.SafeNormalize(_eje) * _spd;
                }

                _eje = Vector2.Normalize(Vector2.Lerp(_eje, Projectile.velocity.SafeNormalize(_eje), 0.40f));

                // EL MORDISCO DE LA LÁGRIMA: hasta 18 golpes contados.
                Golpear();
            }

            // === EL ENCENDIDO (tick 150): destello + click + brasas ===
            if (_age >= OrbitaTicks && !_encendida)
            {
                _encendida = true;
                _encendidaAge = 0f;
                if (Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Flash(PyraPalettes.Sample(PyraPalettes.SolarFire, 1f), 0.16f, 6);
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20.WithPitchOffset(0.4f), Projectile.Center); }
                    catch { }
                    PyraLib.Sparks(Projectile.Center, Vector2.Zero, 4, PyraPalettes.SolarFire,
                        Seed + 31, out ParticleData[] motas);
                    if (motas != null)
                        for (int i = 0; i < motas.Length; i++)
                            ParticleManager.Spawn(motas[i]);
                }
            }
            if (_encendida)
                _encendidaAge += 1f;

            // LA CINTA: registrar el camino propio (14 puntos, 1 por tick).
            for (int i = 0; i < PuntosCinta - 1; i++)
                _cinta[i] = _cinta[i + 1];
            _cinta[PuntosCinta - 1] = Projectile.Center;

            // LA LUZ DEL METAL FUNDIDO (sube con el encendido).
            Color luz = PyraPalettes.Sample(PyraPalettes.SolarFire, 0.30f + 0.65f * Heat01);
            float k = 0.45f + 0.55f * Heat01;
            Lighting.AddLight(Projectile.Center, luz.R / 255f * k, luz.G / 255f * k, luz.B / 255f * k);
        }

        /// <summary>EL ENEMIGO MÁS CERCANO en 1600 px (el filtro de la casa).</summary>
        private NPC BuscarObjetivo()
        {
            NPC mejor = null;
            float mejorDist = RadioBusqueda;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = npc;
                }
            }
            return mejor;
        }

        /// <summary>
        /// EL MORDISCO: contacto circular con i-frames PROPIOS de 5 ticks por
        /// objetivo, quemadura cósmica de 7 s y tope de 18 golpes (al 18º la
        /// gota REVIENTA). v6.50 — GolpeMotor (el cauce del motor: crítica
        /// real, varianza, on-hit y sync MP del propio motor).
        /// </summary>
        private void Golpear()
        {
            if (_golpes >= MaxGolpes) return;

            int dmg = Math.Max(1, Projectile.damage);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;

                float alcance = RadioGolpe + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.Distance(npc.Center, Projectile.Center) > alcance) continue;

                if (_ultimoGolpe.TryGetValue(npc.whoAmI, out int ultimo) && _age - ultimo < Iframes)
                    continue;
                _ultimoGolpe[npc.whoAmI] = (int)_age;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // El metal fundido se pega: quemadura cósmica 7 s (el debuff de la casa).
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), QuemaduraTicks); } catch { }

                    _golpes++;
                    if (_golpes >= MaxGolpes)
                    {
                        // LAS 18 LÁGRIMAS VERTIDAS: la gota no tiene más que llorar.
                        Projectile.Kill();
                        return;
                    }
                }
            }
        }

        public override bool PreKill(int timeLeft)
        {
            // PRIMERO explota, LUEGO muere (también en muerte externa).
            Explotar();
            return true;
        }

        /// <summary>
        /// LA EXPLOSIÓN DEL DESVANECIMIENTO: daño de área ×1.5 en radio 90
        /// con quemadura — y el kick pequeño + flash + brasas del final.
        /// v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
        /// on-hit y sync MP del propio motor).
        /// </summary>
        private void Explotar()
        {
            if (_exploto) return;
            _exploto = true;

            {
                int dmg = Math.Max(1, (int)(Projectile.damage * DañoExplosion));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float alcance = RadioExplosion + Math.Max(npc.width, npc.height) * 0.5f;
                    if (Vector2.Distance(npc.Center, Projectile.Center) > alcance) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 4f, true);
                    // La quemadura del área: server/SP (autoridad del debuff).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), QuemaduraTicks); } catch { }
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                OndaLib.Kick(1.6f, 8);
                OndaLib.Flash(PyraPalettes.Sample(PyraPalettes.SolarFire, 0.9f), 0.20f, 7);
                try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14.WithPitchOffset(-0.25f), Projectile.Center); }
                catch { }
                PyraLib.Sparks(Projectile.Center, Vector2.Zero, 12, PyraPalettes.SolarFire,
                    Seed + 99, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawLagrima(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del pase
        /// de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        /// <summary>LA LÁGRIMA DE METAL FUNDIDO: el lote aditivo (fondo, cinta,
        /// gota, menisco, núcleo, destello) y DESPUÉS el lote alfa con las
        /// costras — la doble cara de la gota que arde y se enfría.</summary>
        private void DrawLagrima()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            float heat = Heat01;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // La muerte se apaga suave (los últimos 12 ticks).
            float fade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            // La gota nace al 45 % y se condensa en 16 ticks (formGate).
            float env = (0.45f + 0.55f * MathHelper.Clamp(_age / 16f, 0f, 1f)) * (0.35f + 0.65f * fade);

            // EL ESTIRAMIENTO: la gota se ALARGA con la velocidad (y estrecha).
            float v = Projectile.velocity.Length();
            float stretch = MathHelper.Clamp(v * 0.022f, 0f, 0.8f);
            float halfLen = 40f * (1f + stretch) * env;
            float halfWid = 26f * (1f - 0.28f * stretch) * env;

            Vector2 eje = _eje;
            Vector2 perp = new Vector2(-eje.Y, eje.X);
            Color temp = PyraPalettes.Sample(PyraPalettes.SolarFire, 0.25f + 0.70f * heat);
            Color nucleo = PyraPalettes.Sample(PyraPalettes.SolarFire, 1f);
            // La cabeza de la gota (donde el ancho es máximo, x = 0.74).
            Vector2 posCabeza = drawPos + eje * (halfLen * 0.48f);

            // ============================================================
            //  EL LOTE ADITIVO — la gota que arde
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // 1. EL FONDO: doble resplandor del metal fundido (naranja + oro).
            LumenLib.Bloom(Main.spriteBatch, drawPos + eje * (halfLen * 0.25f),
                halfWid * 3.2f, temp, 0.40f * env, 2);
            LumenLib.Bloom(Main.spriteBatch, posCabeza,
                halfWid * 1.9f, nucleo, 0.28f * env, 1);

            // 2. LA CINTA: la estela de fuego fundido (14 puntos propios).
            DibujarCinta(time, heat, fade);

            // 3. EL CUERPO: la gota — cadena de quads decrecientes.
            DibujarGota(drawPos, eje, halfLen, halfWid, heat, env, seed, time);

            // 4. EL MENISCO: el brillo de tensión superficial en el borde de
            //    la cabeza (el arco fino perpendicular al eje).
            Vector2 posMenisco = posCabeza + perp * (halfWid * 0.40f);
            SegQuad(Main.spriteBatch, VFXCore.SoftGlow, posMenisco,
                halfWid * PerfilLagrima(0.74f) * 1.8f, MathF.Max(halfWid * 0.15f, 1.5f),
                eje.ToRotation() + MathHelper.PiOver2, Tint(nucleo, 0.55f * env));

            // 5. EL NÚCLEO BLANCO-CALIENTE: sube con el encendido, vive en la cabeza.
            float rNucleo = halfWid * (0.45f + 0.85f * heat);
            Main.spriteBatch.Draw(VFXCore.SoftGlow, posCabeza, null,
                Tint(nucleo, (0.30f + 0.70f * heat) * env), 0f,
                VFXCore.SoftGlow.Size() * 0.5f,
                new Vector2(rNucleo, rNucleo) / VFXCore.SoftGlow.Size(),
                SpriteEffects.None, 0f);

            // 6. EL DESTELLO DEL ENCENDIDO (los 10 ticks tras entrar en filo).
            if (_encendida && _encendidaAge < 10f)
            {
                float dk = 1f - _encendidaAge / 10f;
                LumenLib.Bloom(Main.spriteBatch, posCabeza,
                    halfWid * (3f + 4f * dk), nucleo, 0.60f * dk * fade, 3);
            }

            Main.spriteBatch.End();

            // ============================================================
            //  EL LOTE ALFA — LAS COSTRAS (las manchas oscuras que se
            //  forman al enfriar y se FUNDEN cuando la gota arde).
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            DibujarCostras(drawPos, eje, perp, halfLen, halfWid, heat, env, seed);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// EL CUERPO — la GOTA: cadena de quads solapados orientados al eje
        /// con el perfil de lágrima (cola fina → cabeza redonda con
        /// casquete); la cabeza arde más que la cola y la cola se desgarra
        /// con el flicker determinista.
        /// </summary>
        private void DibujarGota(Vector2 drawPos, Vector2 eje, float halfLen, float halfWid,
            float heat, float env, int seed, float time)
        {
            SpriteBatch batch = Main.spriteBatch;
            float segLen = (halfLen * 2f / Segmentos) * 1.30f;   // solape del 30 %
            float rot = eje.ToRotation();

            for (int i = 0; i < Segmentos; i++)
            {
                float x0 = i / (float)Segmentos;
                float x1 = (i + 1) / (float)Segmentos;
                float xm = (x0 + x1) * 0.5f;
                float w = PerfilLagrima(xm) * halfWid;
                if (w < 0.5f) continue;

                Vector2 pos = drawPos + eje * ((xm - 0.5f) * 2f * halfLen);
                // La temperatura del segmento: la cabeza arde, la cola chamusca.
                float tSeg = MathHelper.Clamp((0.25f + 0.70f * heat) * (0.72f + 0.28f * xm), 0f, 1f);
                Color cSeg = PyraPalettes.Sample(PyraPalettes.SolarFire, tSeg);
                // La cola desgarrada: flicker determinista (más mordido hacia atrás).
                float flicker = 1f - (1f - xm) * 0.35f * VFXCore.Hash01(seed, i, 77)
                    + 0.10f * MathF.Sin(time * 9f + i * 2.6f + seed);

                SegQuad(batch, VFXCore.SoftGlow, pos, segLen, w * 2f, rot,
                    Tint(cSeg, 0.65f * env * flicker));
            }
        }

        /// <summary>
        /// LAS COSTRAS — las manchas oscuras de baja frecuencia pegadas al
        /// borde de la gota (deterministas por Hash01): la piel de metal se
        /// forma al enfriar y se FUNDEN al arder (alpha = 0.75 − heat·0.3).
        /// </summary>
        private void DibujarCostras(Vector2 drawPos, Vector2 eje, Vector2 perp,
            float halfLen, float halfWid, float heat, float env, int seed)
        {
            float alpha = 0.55f * (0.75f - 0.30f * heat) * env;
            if (alpha <= 0.03f) return;

            SpriteBatch batch = Main.spriteBatch;
            for (int k = 0; k < Costras; k++)
            {
                float hx = VFXCore.Hash01(seed, k, 11);
                float hl = VFXCore.Hash01(seed, k, 13);
                float hs = VFXCore.Hash01(seed, k, 17);

                // Pegadas al borde del cuerpo (nunca en la cola fina).
                float x = 0.30f + 0.55f * hx;
                float w = PerfilLagrima(x) * halfWid;
                if (w < 1f) continue;
                Vector2 pos = drawPos + eje * ((x - 0.5f) * 2f * halfLen)
                            + perp * ((hl - 0.5f) * 1.5f * w);

                float s = halfWid * (0.26f + 0.34f * hs);
                batch.Draw(VFXCore.SoftGlow, pos, null,
                    Tint(new Color(52, 18, 8), alpha * (0.70f + 0.30f * hs)),
                    perp.ToRotation(),
                    VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2(s * 1.25f, s * 0.65f) / VFXCore.SoftGlow.Size(),
                    SpriteEffects.None, 0f);
            }
        }

        /// <summary>LA CINTA: el camino propio de 14 puntos → EstelaLib.Ribbon
        /// (perfil Comet: cola de 3 px → cabeza de 15) con la fase anclada
        /// por la semilla de identidad.</summary>
        private void DibujarCinta(float time, float heat, float fade)
        {
            var pts = new Vector2[PuntosCinta];
            int n = 0;
            for (int i = 0; i < PuntosCinta; i++)
            {
                if (_cinta[i] != Vector2.Zero)
                    pts[n++] = _cinta[i] - Main.screenPosition;
            }
            if (n < 3) return;

            if (n < PuntosCinta)
            {
                var recorte = new Vector2[n];
                Array.Copy(pts, recorte, n);
                pts = recorte;
            }

            Color cinta = PyraPalettes.Sample(PyraPalettes.SolarFire, 0.55f + 0.35f * heat);
            EstelaLib.Ribbon(Main.spriteBatch, pts, 15f, EstelaProfile.Comet, cinta,
                0.55f * fade, Seed + 7, time, false);
        }

        /// <summary>
        /// EL PERFIL DE LÁGRIMA: w = 0.56·rise·cap — la cola sube suave
        /// (smoothstep^0.62 hasta el 74 %) y la cabeza es un CASQUETE
        /// REDONDO (sqrt(1−((x−0.74)/0.26)²)): cola fina, cabeza gorda.
        /// </summary>
        private static float PerfilLagrima(float x)
        {
            x = MathHelper.Clamp(x, 0f, 1f);
            float s = MathHelper.Clamp((x - 0.02f) / 0.72f, 0f, 1f);
            float rise = MathF.Pow(s * s * (3f - 2f * s), 0.62f);
            float u = (x - 0.74f) / 0.26f;
            float cap = x <= 0.74f ? 1f : MathF.Sqrt(MathF.Max(1f - u * u, 0f));
            return 0.56f * rise * cap;
        }

        /// <summary>Segmento orientado por la tangente (el quad de un tramo).</summary>
        private static void SegQuad(SpriteBatch batch, Texture2D tex, Vector2 mid,
            float len, float w, float rot, Color tint)
        {
            if (tint.A == 0 || w < 0.5f) return;
            batch.Draw(tex, mid, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2(len, w) / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
