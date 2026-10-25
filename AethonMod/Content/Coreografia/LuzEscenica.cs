using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Coreografia
{
    // ======================================================================
    //  v6.52.0 — LUZESCENICA: LA COREOGRAFÍA DE LUCES del jefe (la
    //  petición literal del usuario: «además de más efectos visuales,
    //  más luces, más… que se vea mucho más avanzado en su visual»).
    //
    //  LA RECETA (R3 §1.6 + §4.2 — la semántica de color del bullet-hell
    //  y el «stage lighting» de los cinemáticos de Lunar Veil): la luz
    //  del jefe NO es un constante — es un GUIÓN:
    //     · TELEGRAPH = violeta FRÍO latiendo a 4 Hz (crece con la fase)
    //     · ACTIVO    = destello BLANCO-CALIENTE (envolvente que decae)
    //     · RECUPERACIÓN = baño ÁMBAR tenue (la ventana que se VE)
    //  Las transiciones de fase lavan la ARENA ENTERA (3 canales).
    //
    //  EL CONTRATO DE TIEMPO DE LA CASA (PantallaLib): sin timers
    //  mutables — cada canal guarda SU MARCA DE FRAME y el estado se
    //  DERIVA de Main.GameUpdateCount (idempotente, determinista).
    // ======================================================================
    public static class LuzEscenica
    {
        private const int Canales = 4;

        // === EL ESTADO DERIVADO (todo por marcas de frame) ===
        private static readonly Vector2[] _pos = new Vector2[Canales];
        private static readonly Color[] _color = new Color[Canales];
        private static readonly float[] _intensidad = new float[Canales];
        private static readonly float[] _radio = new float[Canales];

        private static readonly uint[] _frameGolpe = new uint[Canales];
        private static readonly Color[] _colorGolpe = new Color[Canales];
        private static readonly float[] _picoGolpe = new float[Canales];
        private static readonly uint[] _frameLatido = new uint[Canales];
        private static readonly float[] _hzLatido = new float[Canales];
        private static readonly Color[] _colorLatido = new Color[Canales];

        /// <summary>La duración del destello de golpe (frames, decae suave).</summary>
        private const uint GolpeTicks = 22;

        // ------------------------------------------------------------------
        //  LA API DEL DIRECTOR (llamada por CoreoGuion / el jefe)
        // ------------------------------------------------------------------

        /// <summary>Un FOCO persistente este frame (el baño de arena).</summary>
        public static void Foco(int canal, Vector2 posMundo, Color color, float fuerza)
        {
            if (canal < 0 || canal >= Canales) return;
            _pos[canal] = posMundo;
            _color[canal] = color;
            _intensidad[canal] = fuerza;
            _radio[canal] = 22f;                     // el radio fijo del baño (tiles)
        }

        /// <summary>Un GOLPE de luz (el destello del «AHORA», decae en 22 t).</summary>
        public static void Golpe(int canal, Color color, float pico = 2.2f)
        {
            if (canal < 0 || canal >= Canales) return;
            _frameGolpe[canal] = Main.GameUpdateCount;
            _colorGolpe[canal] = color;
            _picoGolpe[canal] = pico;
        }

        /// <summary>EL LATIDO (la luz que PULSA mientras telegrafía — 4 Hz).</summary>
        public static void Latido(int canal, Color color, float hz = 4f)
        {
            if (canal < 0 || canal >= Canales) return;
            _frameLatido[canal] = Main.GameUpdateCount;
            _colorLatido[canal] = color;
            _hzLatido[canal] = hz;
        }

        /// <summary>Apaga TODO (transición terminada, jefe muerto).</summary>
        public static void Apagar()
        {
            for (int i = 0; i < Canales; i++)
            {
                _intensidad[i] = 0f;
                _frameLatido[i] = 0u;
                _frameGolpe[i] = 0u;
            }
        }

        // ------------------------------------------------------------------
        //  LA APLICACIÓN — cada tick, en clientes (AddLight es visual):
        //  la suma de foco + latido + golpe por canal.
        // ------------------------------------------------------------------
        public static void Aplicar()
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            uint ahora = Main.GameUpdateCount;
            for (int c = 0; c < Canales; c++)
            {
                // EL FOCO (persistente este frame).
                if (_intensidad[c] > 0.01f)
                {
                    Vector3 col = _color[c].ToVector3() * (_intensidad[c] * 0.9f);
                    Lighting.AddLight(_pos[c], col.X, col.Y, col.Z);
                }

                // EL LATIDO (mientras vivo — muere a los 6 t sin renovar).
                if (_frameLatido[c] != 0u && ahora - _frameLatido[c] < 6u)
                {
                    float pulso = 0.5f + 0.5f * MathF.Sin(ahora / 60f * _hzLatido[c] * MathHelper.TwoPi);
                    Vector3 col = _colorLatido[c].ToVector3() * (0.55f + 0.65f * pulso);
                    Lighting.AddLight(_pos[c], col.X, col.Y, col.Z);
                }

                // EL GOLPE (la envolvente que NACE y DECAE — derivada, no timer).
                if (_frameGolpe[c] != 0u && ahora - _frameGolpe[c] < GolpeTicks)
                {
                    float t = (ahora - _frameGolpe[c]) / (float)GolpeTicks;
                    float envolvente = (1f - t) * (1f - t);      // ease-out cuadrático
                    Vector3 col = _colorGolpe[c].ToVector3() * (_picoGolpe[c] * envolvente);
                    Lighting.AddLight(_pos[c], col.X, col.Y, col.Z);
                }
            }
        }

        /// <summary>El MODSYSTEM que aplica las luces cada tick (el ancla).</summary>
        public class LuzEscenicaSistema : ModSystem
        {
            public override void PostUpdateEverything()
            {
                Aplicar();
            }

            public override void OnWorldUnload() => Apagar();
            public override void Unload() => Apagar();
        }
    }
}
