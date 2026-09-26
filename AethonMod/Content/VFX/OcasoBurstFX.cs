using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OcasoBurstFX — v6.27 — EL DESGARRO DE LA MUERTE DE ESTRELLA.
    ///
    /// Cuando una MUERTE DEL OCASO se apaga (OnKill), la realidad queda
    /// HERIDA un instante: un desgarro corto de RiftLib (la librería de
    /// las grietas de la casa) que respira ~40 ticks en el punto del
    /// apagón y se cierra solo — el broche visual del burst del arma
    /// suprema, sin daño (el daño ya lo puso el proyectil).
    ///
    /// El registro es un buffer estático que OcasoSystem envejece (por
    /// TICK) y dibuja (por frame) — el mismo ciclo vida/dibujo separado
    /// de OndaSystem/BrumaFX.
    /// </summary>
    public static class OcasoBurstFX
    {
        /// <summary>La vida del desgarro de la muerte (ticks).</summary>
        private const int Vida = 40;

        private struct Desgarro
        {
            public Vector2 Pos;
            public Vector2 Dir;
            public int Seed;
            public int Age;
        }

        private static readonly List<Desgarro> _vivos = new(6);

        /// <summary>Programa el desgarro del apagón (llamado desde OnKill).</summary>
        public static void ProgramarDesgarro(Vector2 pos, Vector2 dir, int seed)
        {
            // Presupuesto: máx 4 vivos (la lluvia del ocaso puede matar
            // varias estrellas casi a la vez — 4 heridas abiertas bastan).
            if (_vivos.Count >= 4) _vivos.RemoveAt(0);
            _vivos.Add(new Desgarro { Pos = pos, Dir = dir, Seed = seed, Age = 0 });
        }

        /// <summary>El envejecimiento por TICK (lo llama OcasoSystem).</summary>
        internal static void Tick()
        {
            for (int i = _vivos.Count - 1; i >= 0; i--)
            {
                _vivos[i] = new Desgarro
                {
                    Pos = _vivos[i].Pos,
                    Dir = _vivos[i].Dir,
                    Seed = _vivos[i].Seed,
                    Age = _vivos[i].Age + 1,
                };
                if (_vivos[i].Age > Vida) _vivos.RemoveAt(i);
            }
        }

        /// <summary>El dibujo por FRAME con el lote de la interfaz TAL CUAL
        /// (lo llama OcasoSystem en PostDrawInterface; RiftLib.Tear dibuja
        /// quads de paleta que componen igual en alfa que en aditivo).
        /// v6.50.3 — blindado con try/catch (el lote de UI puede venir
        /// CERRADO si otro mod lo dejó así) y el anclaje documentado: coords
        /// de MUNDO restadas a secas (aproximado con zoom ≠ 100%, el mismo
        /// anclaje deliberado de Pantalla.OndaExpansiva).</summary>
        internal static void Dibujar(SpriteBatch batch)
        {
            if (_vivos.Count == 0) return;
            float time = Main.GlobalTimeWrappedHourly;

            try
            {
                for (int i = 0; i < _vivos.Count; i++)
                {
                    Desgarro d = _vivos[i];
                    float t = d.Age / (float)Vida;

                    // LA APERTURA y el CIERRE de la herida (el arco de la casa:
                    // abre rápido, respira, cierra — nunca en seco).
                    float progress;
                    if (t < 0.15f) progress = t / 0.15f * 0.5f;          // abre
                    else if (t < 0.75f) progress = 0.5f + (t - 0.15f) / 0.6f * 0.5f; // respira llena
                    else progress = 1f;                                    // cierre

                    // La intensidad MUERE con la herida.
                    float intensidad = 0.8f * (1f - t * t);

                    RiftLib.Tear(batch, d.Pos - Main.screenPosition, d.Dir,
                        150f, progress, 22f, RiftPaletas.Carmesi,
                        intensidad, d.Seed, time + i * 0.7f);
                }
            }
            catch { }
        }

        /// <summary>Vaciado en las descargas (recargas limpias).</summary>
        internal static void Unload() => _vivos.Clear();
    }
}
