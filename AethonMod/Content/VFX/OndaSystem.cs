using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OndaSystem — v6.25 — EL APLICADOR DE IMPACTOS DE ONDALIB.
    ///
    /// Centraliza los DOS efectos de pantalla del paquete de impacto en
    /// UN punto del frame cada uno (antes había riesgo de que varias
    /// sacudidas ad-hoc se pisaran — el análisis de huecos v6.25 lo
    /// señaló como deuda):
    ///
    ///   · LA SACUDIDA (OndaLib.Kick): acumulador estático con máx 2
    ///     impulsos simultáneos (el mayor gana), aplicado SOLO en
    ///     ModifyScreenPosition — el hook OFICIAL de tML justo antes de
    ///     dibujar el mundo. Amplitud: strength·(1−age/dur)²·sin(age·2.1)
    ///     (frecuencia ~13 Hz con decay cuadrático), cap 14 px.
    ///
    ///   · EL DESTELLO (OndaLib.Flash): v6.50.7 — EL GRADIENTE LOCAL
    ///     DEL ARMA. Antes era UN velo radial a PANTALLA COMPLETA (SoftGlow
    ///     gigante centrado en la pantalla): ahogaba el cuadro entero en
    ///     color cada vez que un arma destellaba. Ahora nace en el CENTRO
    ///     DEL PROYECTIL del arma (coords de mundo, anclaje de
    ///     OndaExpansiva), con degradado radial que muere al alejarse del
    ///     centro — núcleo + falda ancha, lote ADITIVO propio en Identity,
    ///     restaurando el lote de interfaz con SU matriz (todo en
    ///     try/catch/finally).
    ///     Máx 1 activo + cooldown 30 ticks (presupuesto anti-mareo).
    ///
    /// Todo CLIENTE-ONLY (los servidores no ven pantallas).
    /// </summary>
    public class OndaSystem : ModSystem
    {
        // --- EL ACUMULADOR DE SACUDIDA (máx 2, la mayor gana) ---
        private struct KickImpulse
        {
            public float Strength;      // px de amplitud inicial
            public int Duration;        // ticks de vida
            public int Age;             // ticks vividos
            public float DirX, DirY;    // dirección (omni si 0,0)
        }

        private static readonly KickImpulse[] _kicks = new KickImpulse[2];

        // --- EL DESTELLO (máx 1 activo + cooldown) ---
        private static Color _flashColor = Color.White;
        private static float _flashStrength;
        private static int _flashDuration;
        private static int _flashAge;
        private static int _flashCooldown;

        // v6.50.7 — EL FOCO DEL ARMA: dónde nace el destello (coords de
        // MUNDO). Sin ancla (llamadores viejos) cae al centro del jugador.
        private static Vector2 _flashOrigen;
        private static bool _flashAnclado;

        private static Asset<Texture2D> _glow;

        /// <summary>El brillo radial del destello local.</summary>
        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  LA API INTERNA DE ONDALIB
        // ==================================================================

        /// <summary>Registra un impulso de sacudida (la mayor de las 2 gana el slot).</summary>
        internal static void Kick(float strengthPx, int durationTicks, float directionAngle)
        {
            strengthPx = MathHelper.Clamp(strengthPx, 0f, 14f);   // cap de la casa
            durationTicks = (int)MathHelper.Clamp(durationTicks, 2f, 60f);

            var k = new KickImpulse
            {
                Strength = strengthPx,
                Duration = durationTicks,
                Age = 0,
                DirX = 0f,
                DirY = 0f,
            };
            if (directionAngle >= 0f)
            {
                k.DirX = MathF.Cos(directionAngle);
                k.DirY = MathF.Sin(directionAngle);
            }

            // El slot con menos VIDA RESTANTE cede el sitio.
            int slot = 0;
            float resto0 = _kicks[0].Duration - _kicks[0].Age;
            float resto1 = _kicks[1].Duration - _kicks[1].Age;
            if (resto1 < resto0) slot = 1;
            if (strengthPx < _kicks[slot].Strength && resto0 > 0 && resto1 > 0)
                return;   // ya hay dos vivas y más fuertes: no entra

            _kicks[slot] = k;
        }

        /// <summary>Registra el destello (máx 1 activo + cooldown
        /// anti-mareo). v6.50.7 — LOCAL: <paramref name="centroMundo"/> es
        /// el CENTRO del proyectil del arma (null = el jugador local); el
        /// destello vive y muere ahí, con degradado radial hacia afuera.</summary>
        internal static void Flash(Color color, float strength, int durationTicks, Vector2? centroMundo)
        {
            if (_flashAge < _flashDuration || _flashCooldown > 0) return;   // presupuesto
            _flashColor = color;
            _flashStrength = MathHelper.Clamp(strength, 0.02f, 0.5f);
            _flashDuration = (int)MathHelper.Clamp(durationTicks, 2f, 30f);
            _flashAge = 0;
            _flashCooldown = 30;
            _flashAnclado = centroMundo.HasValue;
            _flashOrigen = centroMundo ?? Vector2.Zero;
        }

        // ==================================================================
        //  EL CICLO DE VIDA (envejece por TICK de juego, no por frame)
        // ==================================================================

        public override void PreUpdateEntities()
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < _kicks.Length; i++)
                if (_kicks[i].Age < _kicks[i].Duration)
                    _kicks[i].Age++;

            if (_flashAge < _flashDuration) _flashAge++;
            if (_flashCooldown > 0) _flashCooldown--;
        }

        // ==================================================================
        //  LA SACUDIDA — UN punto del frame (el hook oficial de tML)
        // ==================================================================

        public override void ModifyScreenPosition()
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < _kicks.Length; i++)
            {
                KickImpulse k = _kicks[i];
                if (k.Age >= k.Duration || k.Strength <= 0f) continue;

                // Decay cuadrático + frecuencia de sacudida ~13 Hz.
                float vida = 1f - k.Age / (float)k.Duration;
                float amp = k.Strength * vida * vida;
                float wave = MathF.Sin(k.Age * 2.1f + i * 1.7f);

                if (k.DirX == 0f && k.DirY == 0f)
                {
                    // OMNIDIRECCIONAL: el jitter vive por hash del tick.
                    int h = unchecked((int)Main.GameUpdateCount * 374761393 + i * 668265263);
                    h = unchecked(h ^ (h >> 13));
                    h = unchecked(h * 1274126177);
                    h ^= h >> 16;
                    float jx = ((h & 0xFFFF) / 65536f - 0.5f) * 2f;
                    float jy = (((h >> 8) & 0xFFFF) / 65536f - 0.5f) * 2f;
                    Main.screenPosition += new Vector2(jx * amp * wave, jy * amp * wave);
                }
                else
                {
                    Main.screenPosition += new Vector2(k.DirX, k.DirY) * (amp * wave);
                }
            }
        }

        // ==================================================================
        //  EL DESTELLO — el gradiente LOCAL del arma (núcleo + falda)
        // ==================================================================

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (_flashAge >= _flashDuration || _flashStrength <= 0f) return;
            if (Main.netMode == NetmodeID.Server) return;

            Texture2D tex = GlowTex;
            if (tex == null) return;

            // Alpha decayente del destello (cuadrático — muere como una onda).
            float vida = 1f - _flashAge / (float)_flashDuration;
            float a = MathHelper.Clamp(_flashStrength * 2.4f, 0f, 0.85f) * vida * vida;
            if (a <= 0.005f) return;

            // v6.50.7 — EL FOCO DEL ARMA: el destello nace en el CENTRO DEL
            // PROYECTIL (coords de mundo → pantalla restando screenPosition:
            // el anclaje documentado de OndaExpansiva, exacto a zoom 1). Sin
            // ancla (llamadores sin origen): el jugador local — el arma vive
            // cerca de su dueño. UN GRADIENTE LOCAL: el velo de pantalla
            // completa ahogaba TODO el cuadro en color a cada disparo.
            Vector2 centro = _flashAnclado ? _flashOrigen
                : (Main.LocalPlayer?.Center ?? Vector2.Zero);
            Vector2 pos = centro - Main.screenPosition;
            if (pos.X < -600f || pos.Y < -600f ||
                pos.X > Main.screenWidth + 600f || pos.Y > Main.screenHeight + 600f)
                return;   // fuera de cuadro: ni un quad

            // El radio RESPIRA: nace apretado y se abre ~26% al morir (el
            // "pop" del fogonazo que se disipa).
            float radio = (150f + 340f * _flashStrength) * (0.82f + 0.36f * (1f - vida));

            // EL TINTE LINEAL (deliberado, distinto del Tint premultiplicado
            // de la casa): Additive=(SourceAlpha,One) sobre la SoftGlow
            // premultiplicada ⇒ aporte = g²·color·a — núcleo a tope y
            // degradado empinado hacia afuera. EL FOCO, no el velo.
            var c = new Color(_flashColor.R, _flashColor.G, _flashColor.B,
                (byte)(int)(255f * a));
            var falda = new Color(_flashColor.R, _flashColor.G, _flashColor.B,
                (byte)(int)(255f * a * 0.28f));

            // v6.50.3 — FIX (guard de lote + anclaje exacto; v6.50.7 mantiene
            // el patrón): cerrar, dibujar en Identity (pantalla EXACTA) y
            // reabrir el lote de interfaz con SU matriz — todo en
            // try/catch/finally (la lección v6.41: nunca dejar el lote abierto).
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullCounterClockwise, null, Matrix.Identity);

                Vector2 size = new Vector2(radio * 2f, radio * 2f);
                Vector2 origen = tex.Size() * 0.5f;
                // EL NÚCLEO (el fogonazo del arma).
                Main.spriteBatch.Draw(tex, pos, null, c, 0f, origen,
                    size / tex.Size(), SpriteEffects.None, 0f);
                // LA FALDA (×1.6 de radio al 28%): el degradado ALCANZA más
                // lejos sin lavar el foco — dos quads, cero estado.
                Main.spriteBatch.Draw(tex, pos, null, falda, 0f, origen,
                    size * 1.6f / tex.Size(), SpriteEffects.None, 0f);
            }
            catch { }
            finally
            {
                // v6.50.11 — SONDA + CURACIÓN: cierra lo nuestro sin
                // first-chance y devuelve el lote de INTERFAZ (con SU
                // matriz) SIEMPRE que no haya ya un Begin vivo — si llegó
                // cerrado, se cura.
                VFXCore.CerrarLoteSiAbierto();
                if (!VFXCore.LoteAbierto)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                    }
                    catch { }
                }
            }
        }

        // ==================================================================
        //  DESCARGA (recargas limpias)
        // ==================================================================

        public override void Unload()
        {
            for (int i = 0; i < _kicks.Length; i++)
                _kicks[i] = default;
            _flashAge = 0;
            _flashDuration = 0;
            _flashCooldown = 0;
            _flashStrength = 0f;
            _flashAnclado = false;
            _flashOrigen = Vector2.Zero;
            // v6.49 — EL ASSET TAMBIÉN (hallazgo AUD-C: OcasoSystem sí lo
            // anulaba; OndaSystem no — el Asset<T> estático sobrevivía a
            // la recarga del mod).
            _glow = null;
        }

        /// <summary>
        /// v6.49 — EL MUNDO TAMBIÉN (hallazgo AUD-C): un flash/kick vivo
        /// al salir del mundo se colaba en el siguiente. Ahora muere con
        /// su mundo.
        /// </summary>
        public override void OnWorldUnload()
        {
            for (int i = 0; i < _kicks.Length; i++)
                _kicks[i] = default;
            _flashAge = 0;
            _flashDuration = 0;
            _flashCooldown = 0;
            _flashStrength = 0f;
            _flashAnclado = false;
            _flashOrigen = Vector2.Zero;
        }
    }
}
