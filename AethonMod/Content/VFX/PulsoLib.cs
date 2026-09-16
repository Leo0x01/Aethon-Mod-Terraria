using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PulsoLib — v6.31 — LA SUPER LIBRERÍA DEL GAME FEEL: "el sistema
    /// nervioso" del mod.
    ///
    /// Nace de la super investigación (INFORME_TOP_MODS_LIBRERIAS §C.2): lo
    /// que hace que un golpe SE SIENTA — el juicio diferido, el retroceso,
    /// la cámara, el sonido por material y el estado de pantalla — estaba
    /// disperso en improvisaciones de cada renderer. Los mods top lo llevan
    /// PEGADO a miles de archivos; NADIE lo publica como API. Esta es la
    /// primera: CINCO piezas unificadas, deterministas en MP y con la
    /// accesibilidad centralizada.
    ///
    ///   · EL JUICIO DIFERIDO (la joya — el iaijutsu destilado): se MARCA en
    ///     silencio (un eco sutil + un pitch que sube por marca), y TODO se
    ///     liquida de golpe en un "clang" único escalado por el número de
    ///     víctimas. EL FALLO NO SUENA NI BRILLA.
    ///   · EL RETROCESO: un struct readonly con Derivar(peso, ticksCarga) —
    ///     dos números configuran el feel de cientos de armas; el wobble
    ///     lleva la semilla whoAmI (determinismo MP gratis).
    ///   · LA CÁMARA: un solo punto de acceso al PunchCameraModifier real,
    ///     con try/catch a prueba de balas.
    ///   · EL SONIDO POR MATERIAL: máx 2/frame (presupuesto) y pitch por
    ///     combo — el contador de racha regala el pitch ascendente.
    ///   · LA PANTALLA: un estado push/decay con apilamiento limitado —
    ///     todos los flashes del mod pasan por aquí y decaen iguales.
    ///
    /// Contrato: NADA de Main.rand (todo determinista por semilla/whoAmI);
    /// el daño de Liquidar() solo corre en server/singleplayer.
    /// </summary>
    public static class PulsoLib
    {
        // ==================================================================
        //  1 — EL JUICIO DIFERADO (marcar en silencio, liquidar de golpe)
        // ==================================================================

        /// <summary>UNA marca del veredicto: el objetivo anotado y su precio.</summary>
        private struct Marca
        {
            public int IndiceNPC;
            public int Daño;
            public float Knockback;
        }

        /// <summary>UN veredicto abierto: las marcas acumuladas de un dueño.</summary>
        private sealed class Veredicto
        {
            public int Dueño;
            public int TickLiquidación;
            public readonly List<Marca> Marcas = new List<Marca>(16);
        }

        // (los veredictos abiertos, por dueño — a lo sumo un puñado)
        private static readonly List<Veredicto> _veredictos = new List<Veredicto>(8);

        /// <summary>
        /// ABRE UN VEREDICTO para este proyectil: a partir de ahora,
        /// <see cref="Marcar"/> anota objetivos en silencio y
        /// <see cref="Liquidar"/> los liquidará TODOS a la vez. Devuelve el
        /// id del veredicto (o −1 en cliente puro, donde el juicio es solo
        /// visual).
        /// </summary>
        /// <param name="dueño">El proyectil que juzga.</param>
        /// <param name="ticksHastaLiquidar">Ticks de gracia: si nadie llama a
        /// Liquidar, el veredicto vence solo (las marcas se disipan CALLADAS).</param>
        public static int AbrirVeredicto(Projectile dueño, int ticksHastaLiquidar = 90)
        {
            if (dueño == null || !dueño.active) return -1;
            CerrarVeredictosVencidos();

            var v = new Veredicto
            {
                Dueño = dueño.whoAmI,
                TickLiquidación = (int)Main.GameUpdateCount + Math.Max(5, ticksHastaLiquidar),
            };
            _veredictos.Add(v);
            return _veredictos.Count - 1;
        }

        /// <summary>
        /// MARCA un objetivo (SILENCIOSO): el daño se apunta pero NO se aplica;
        /// el visual es un eco sutil y el pitch de un "tic" sube con cada marca
        /// (0.55 + n·0.04 — el contador audible del iaijutsu). Sin víctimas
        /// visibles: la marca ES discreta a propósito.
        /// </summary>
        public static void Marcar(Projectile dueño, NPC objetivo, int daño, float knockback = 2f)
        {
            if (objetivo == null || !VFXCore.EsObjetivo(objetivo)) return;
            Veredicto v = VeredictoDe(dueño);
            if (v == null) return;

            // ¿ya marcado? refresca el precio, no lo duplica.
            for (int i = 0; i < v.Marcas.Count; i++)
                if (v.Marcas[i].IndiceNPC == objetivo.whoAmI)
                {
                    v.Marcas[i] = new Marca { IndiceNPC = objetivo.whoAmI, Daño = Math.Max(v.Marcas[i].Daño, daño), Knockback = knockback };
                    return;
                }

            v.Marcas.Add(new Marca { IndiceNPC = objetivo.whoAmI, Daño = daño, Knockback = knockback });

            // EL TIC ASCENDENTE (solo cliente — la voz del contador).
            if (Main.netMode != NetmodeID.Server)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item167.WithPitchOffset(0.55f + v.Marcas.Count * 0.04f).WithVolumeScale(0.30f),
                        objetivo.Center);
                }
                catch { }
            }
        }

        /// <summary>
        /// LIQUIDA el veredicto de este dueño: TODAS las marcas se cobran a la
        /// vez — un "clang" único, un destello por víctima y un shake escalado
        /// por el número de ellas. Devuelve el número de víctimas. SI NO HAY
        /// VÍCTIMAS: NO SUENA, NO BRILLA (el fallo es silencio — la regla).
        /// </summary>
        public static int Liquidar(Projectile dueño)
        {
            Veredicto v = VeredictoDe(dueño);
            if (v == null) return 0;

            int víctimas = 0;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (Marca m in v.Marcas)
                {
                    NPC npc = m.IndiceNPC >= 0 && m.IndiceNPC < Main.maxNPCs ? Main.npc[m.IndiceNPC] : null;
                    if (npc == null || !VFXCore.EsObjetivo(npc)) continue;
                    npc.SimpleStrikeNPC(Math.Max(1, m.Daño), npc.direction, false, m.Knockback, DamageClass.Magic);
                    víctimas++;
                }
            }
            else víctimas = v.Marcas.Count;   // el cliente cuenta para el feel

            if (víctimas > 0)
            {
                // EL CLANG ÚNICO + el shake ESCALADO por víctimas.
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item90.WithPitchOffset(-0.35f).WithVolumeScale(0.65f),
                        dueño != null && dueño.active ? dueño.Center : Main.LocalPlayer.Center);
                }
                catch { }
                GolpearCamara(-Vector2.UnitY, Math.Min(3f + víctimas * 1.6f, 12f), 12);
                EmpujarPantalla(new Color(255, 240, 220), 0.10f + 0.03f * víctimas, 10);
            }
            // (el fallo: silencio absoluto — ni sonido ni brillo)

            _veredictos.Remove(v);
            return víctimas;
        }

        /// <summary>¿Tiene este dueño un veredicto ABIERTO con marcas?</summary>
        public static bool TieneMarcas(Projectile dueño)
        {
            Veredicto v = VeredictoDe(dueño);
            return v != null && v.Marcas.Count > 0;
        }

        /// <summary>Cuántas marcas lleva el veredicto de este dueño (para escalar visuales).</summary>
        public static int ContarMarcas(Projectile dueño)
        {
            Veredicto v = VeredictoDe(dueño);
            return v?.Marcas.Count ?? 0;
        }

        private static Veredicto VeredictoDe(Projectile dueño)
        {
            if (dueño == null || !dueño.active) return null;
            for (int i = 0; i < _veredictos.Count; i++)
                if (_veredictos[i].Dueño == dueño.whoAmI)
                    return _veredictos[i];
            return null;
        }

        private static void CerrarVeredictosVencidos()
        {
            int t = (int)Main.GameUpdateCount;
            for (int i = _veredictos.Count - 1; i >= 0; i--)
                if (_veredictos[i].TickLiquidación < t)
                    _veredictos.RemoveAt(i);   // vencido: se disipa CALLADO
        }

        // ==================================================================
        //  2 — EL RETROCESO (struct readonly + Derivar: 2 números → el feel)
        // ==================================================================

        /// <summary>
        /// EL PERFIL DE RETROCESO (readonly struct — el patrón que configura
        /// cientos de armas con dos números): cuánto se desplaza el arma, con
        /// qué patada, a qué frecuencia tiembla el wobble y si sacude la
        /// pantalla por ser pesada.
        /// </summary>
        public readonly struct Retroceso
        {
            public readonly float Desplazamiento;   // px que el arma retrocede
            public readonly float Patada;           // fuerza del golpe de vuelta
            public readonly float HzWobble;         // Hz del temblor residual
            public readonly bool Pesado;            // ¿sacude la pantalla?

            public Retroceso(float desplazamiento, float patada, float hzWobble, bool pesado)
            {
                Desplazamiento = desplazamiento;
                Patada = patada;
                HzWobble = hzWobble;
                Pesado = pesado;
            }

            /// <summary>
            /// DERIVA el perfil de dos números (la magia): el peso del arma
            /// (0 = daga ligera · 1 = cañón) y los ticks de carga (0 = tiro
            /// instantáneo). Un arma pesada y cargada desplaza MÁS, late más
            /// lento y sacude la pantalla; una ligera apenas se mueve y
            /// tiemble rápido.
            /// </summary>
            public static Retroceso Derivar(float peso, int ticksCarga = 0)
            {
                peso = MathHelper.Clamp(peso, 0f, 1f);
                float carga = MathHelper.Clamp(ticksCarga / 60f, 0f, 1f);
                return new Retroceso(
                    desplazamiento: 2f + 12f * peso + 8f * peso * carga,
                    patada: 0.4f + 1.6f * peso,
                    hzWobble: 9f - 5.5f * peso,                       // ligera = rápida
                    pesado: peso > 0.62f);
            }
        }

        /// <summary>
        /// APLICA el retroceso al jugador (solo visual — la posición del ítem
        /// la dibuja el llamador con <see cref="OffsetRetroceso"/>): la
        /// PANTALLA empuja en dirección OPUESTA al disparo y las armas
        /// pesadas sacuden una vez.
        /// </summary>
        public static void Retroceder(Player p, in Retroceso r, Vector2 dirDisparo)
        {
            if (p == null || !p.active || Main.netMode == NetmodeID.Server) return;
            Vector2 dir = dirDisparo.LengthSquared() > 0.0001f ? Vector2.Normalize(dirDisparo) : -Vector2.UnitX;
            GolpearCamara(-dir, 1.2f + r.Patada * 2.4f, 8);
            if (r.Pesado)
                EmpujarPantalla(new Color(255, 245, 230), 0.05f + 0.03f * r.Patada, 6);
        }

        /// <summary>
        /// EL OFFSET VISUAL del arma en este instante (px, en coords de mundo):
        /// la patada inicial amortiguada + el WOBBLE residual — determinista
        /// por whoAmI (mismaarma = mismo temblor en todas las máquinas).
        /// </summary>
        public static Vector2 OffsetRetroceso(int whoAmI, in Retroceso r, float time, int ticksDesdeDisparo)
        {
            // La patada: exponencial amortiguada (fuerte y corta).
            float k = MathF.Exp(-ticksDesdeDisparo * 0.22f);
            // El wobble: seno amortiguado con la semilla del dueño.
            float wobble = MathF.Sin(time * r.HzWobble * MathHelper.TwoPi + whoAmI * 2.4f)
                           * MathF.Exp(-ticksDesdeDisparo * 0.06f) * r.Desplazamiento * 0.25f;
            return new Vector2(0f, -1f) * (r.Desplazamiento * k + wobble);
        }

        // ==================================================================
        //  3 — LA CÁMARA (un solo punto de acceso, a prueba de balas)
        // ==================================================================

        /// <summary>
        /// GOLPEA LA CÁMARA en una dirección (pásale la OPUESTA al golpe para
        /// el feel de reacción física). Envuelve el PunchCameraModifier real
        /// con try/catch — NUNCA revienta un tiro por la cámara.
        /// </summary>
        public static void GolpearCamara(Vector2 dirección, float fuerzaPx, int ticks)
        {
            if (Main.netMode == NetmodeID.Server || fuerzaPx <= 0.05f) return;
            try
            {
                Vector2 dir = dirección.LengthSquared() > 0.0001f ? Vector2.Normalize(dirección) : Vector2.UnitY;
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Main.LocalPlayer.Center, dir, fuerzaPx, ticks, Math.Min(ticks, 12), 0.35f,
                    "AethonPulso"));
            }
            catch { }
        }

        // ==================================================================
        //  4 — EL SONIDO POR MATERIAL (presupuesto 2/frame + pitch por combo)
        // ==================================================================

        /// <summary>El material de lo golpeado — cada uno con SU voz.</summary>
        public enum Material
        {
            /// <summary>Carne: húmedo, sordo.</summary>
            Carne,
            /// <summary>Acero: metálico, agudo.</summary>
            Acero,
            /// <summary>Piedra: seco, grave.</summary>
            Piedra,
            /// <summary>Vacío: el silencio que duele.</summary>
            Vacio,
        }

        private static int _sonidosDelFrame;
        private static uint _frameDeSonido;

        /// <summary>
        /// LA VOZ DEL GOLPE por material — con PRESUPUESTO (máx 2/frame: las
        /// ráfagas no ensordecen) y pitch por COMBO (la racha sube el tono:
        /// el contador musical del juego).
        /// </summary>
        public static void Sonido(Material m, Vector2 pos, int combo = 0)
        {
            if (Main.netMode == NetmodeID.Server) return;
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDeSonido)
            {
                _frameDeSonido = frame;
                _sonidosDelFrame = 0;
            }
            if (_sonidosDelFrame >= 2) return;   // EL PRESUPUESTO
            _sonidosDelFrame++;

            float pitch = MathHelper.Clamp(combo * 0.06f, 0f, 0.55f);
            try
            {
                var estilo = m switch
                {
                    Material.Carne => SoundID.Item167.WithPitchOffset(-0.25f + pitch).WithVolumeScale(0.5f),
                    Material.Acero => SoundID.Item90.WithPitchOffset(0.10f + pitch).WithVolumeScale(0.45f),
                    Material.Piedra => SoundID.Item70.WithPitchOffset(-0.40f + pitch).WithVolumeScale(0.5f),
                    _ => SoundID.Item8.WithPitchOffset(-0.60f + pitch).WithVolumeScale(0.4f),
                };
                Terraria.Audio.SoundEngine.PlaySound(estilo, pos);
            }
            catch { }
        }

        // ==================================================================
        //  5 — LA PANTALLA (estado push/decay con apilamiento limitado)
        // ==================================================================

        private static float _fuerzaPantalla;
        private static Color _tintePantalla = Color.White;
        private static int _ticksPantalla;
        private static uint _framePantalla;

        /// <summary>La fuerza ACTUAL del estado de pantalla (0..1+; decae sola).</summary>
        public static float FuerzaPantalla => _fuerzaPantalla;

        /// <summary>El TINTE actual del estado de pantalla (para el vignette).</summary>
        public static Color TintePantalla => _tintePantalla;

        /// <summary>
        /// EMPUJA el estado de pantalla (flash suave global): apila MÁXIMO 3
        /// empujes y decae exponencialmente — todos los flashes del mod
        /// pasan por aquí y mueren IGUAL (la consistencia que los
        /// improvisaciones no tenían).
        /// </summary>
        public static void EmpujarPantalla(Color tinte, float fuerza, int ticks)
        {
            if (Main.netMode == NetmodeID.Server) return;
            _fuerzaPantalla = MathHelper.Clamp(_fuerzaPantalla + fuerza, 0f, 0.85f);
            _tintePantalla = tinte;
            _ticksPantalla = Math.Max(_ticksPantalla, Math.Max(4, ticks));
            _framePantalla = Main.GameUpdateCount;
        }

        /// <summary>
        /// EL DECAIMIENTO (lo llama un sistema por frame — o el primer
        /// Empujar del frame siguiente): expónencial, sin estados colgados.
        /// </summary>
        public static void ActualizarPantalla()
        {
            if (_ticksPantalla <= 0) { _fuerzaPantalla = 0f; return; }
            uint frame = Main.GameUpdateCount;
            if (frame == _framePantalla) return;   // un decay por frame
            _framePantalla = frame;
            _ticksPantalla--;
            _fuerzaPantalla *= 0.86f;
            if (_ticksPantalla <= 0 || _fuerzaPantalla < 0.004f)
            {
                _fuerzaPantalla = 0f;
                _ticksPantalla = 0;
            }
        }
    }
}
