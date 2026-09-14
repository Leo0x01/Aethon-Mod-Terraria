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
    ///   · EL DESTELLO (OndaLib.Flash): UN velo radial (SoftGlow a
    ///     pantalla completa — centro brillante, caída suave) dibujado en
    ///     PostDrawInterface con el LOTE DE LA INTERFAZ TAL CUAL (cero
    ///     manipulación de estado: el velo alfa es el look clásico).
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

        private static Asset<Texture2D> _glow;

        /// <summary>El brillo radial del velo del destello.</summary>
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

        /// <summary>Registra el destello (máx 1 activo + cooldown anti-mareo).</summary>
        internal static void Flash(Color color, float strength, int durationTicks)
        {
            if (_flashAge < _flashDuration || _flashCooldown > 0) return;   // presupuesto
            _flashColor = color;
            _flashStrength = MathHelper.Clamp(strength, 0.02f, 0.5f);
            _flashDuration = (int)MathHelper.Clamp(durationTicks, 2f, 30f);
            _flashAge = 0;
            _flashCooldown = 30;
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
        //  EL DESTELLO — el velo radial sobre TODO (lote de UI tal cual)
        // ==================================================================

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (_flashAge >= _flashDuration || _flashStrength <= 0f) return;
            if (Main.netMode == NetmodeID.Server) return;

            Texture2D tex = GlowTex;
            if (tex == null) return;

            // Alpha decayente del velo (cuadrático — muere como una onda).
            float vida = 1f - _flashAge / (float)_flashDuration;
            float a = _flashStrength * vida * vida;
            if (a <= 0.005f) return;

            // EL VELO RADIAL: SoftGlow a pantalla completa — centro
            // brillante, caída suave a las esquinas (el destello clásico
            // "center-weighted", no una placa plana). Con el lote de la
            // interfaz TAL CUAL (velo alfa, cero manipulación de estado).
            var c = new Color(
                (byte)(int)(_flashColor.R * a),
                (byte)(int)(_flashColor.G * a),
                (byte)(int)(_flashColor.B * a),
                (byte)(int)(255f * a));

            var destino = new Rectangle(
                -Main.screenWidth / 6, -Main.screenHeight / 6,
                Main.screenWidth * 4 / 3, Main.screenHeight * 4 / 3);
            spriteBatch.Draw(tex, destino, c);
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
        }
    }
}
