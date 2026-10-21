using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Rarities
{
    // ======================================================================
    //  LA RAREZA CÓSMICA DEL GRIMORIO DEL ETERNO (v6.51.1) — LA LETRA:
    //  «mejorar la rareza del grimorio del eterno, es un arma super rara
    //  asi que su nombre debe tener una rareza mas que legendaria, mas
    //  que mitica, mas que unica, una rareza cosmica ademas con la
    //  etiqueta de legendario supremo».
    //
    //  MÁS ALLÁ del maestro (10) y de cualquier rareza de mod: la luz
    //  del ENTRE-LUGAR — un violeta profundo que RESPIRA hacia el
    //  índigo y el magenta (el color se calcula CADA vez que el juego
    //  pregunta por él: el nombre del arma VIVE, como las páginas).
    // ======================================================================
    public class LegendarioSupremo : ModRarity
    {
        public override Color RarityColor
        {
            get
            {
                // EL CICLO CÓSMICO: violeta primordial ↔ índigo del vacío ↔
                // magenta de la bruma — tres senos inconmensurables (el
                // color jamás se repite exactamente).
                float t = Main.GlobalTimeWrappedHourly;
                float r = 0.62f + 0.20f * MathF.Sin(t * 0.9f);
                float g = 0.22f + 0.14f * MathF.Sin(t * 0.53f + 2.1f);
                float b = 0.95f + 0.05f * MathF.Sin(t * 1.7f + 0.6f);
                return new Color(r, g, b);
            }
        }
    }
}
