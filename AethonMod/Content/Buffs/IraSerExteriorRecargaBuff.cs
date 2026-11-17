using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// IraSerExteriorRecargaBuff — v6.150 — EL RELOJ VIVO DE LA IRA.
    ///
    /// LA LETRA NUEVA (v6.150): «en el buff de la ira de un ser exterior,
    /// no se muestra el contador, deberia tener un contador justo debajo,
    /// o de otra forma, por ejemplo que el buff este gris y que se vaya
    /// iluminando de abajo hacia arriba pitandose a color poco a poco,
    /// que al estar completo se ilumine con un brillo o resplandor
    /// morado».
    ///
    /// EL DISEÑO ELEGIDO (la «otra forma» de la letra — el contador en
    /// texto fallaba porque la rejilla de buffs no es un lugar fiable):
    /// EL ICONO SE PINTA GRIS y SE ENCIENDE DE ABAJO HACIA ARRIBA con el
    /// progreso del reloj de los 10 minutos — un recorte del sprite que
    /// sube como la arena de un reloj invertido. Al llegar al 100 % el
    /// icono entero ARDE con UN RESPLANDOR MORADO pulsante (y el reloj
    /// mm:ss vive DEBAJO del icono como texto dorado — las dos cosas:
    /// la forma pedida Y el contador de siempre, ahora en su sitio).
    ///
    /// La mecánica no cambia: mientras el reloj corre, el portador lleva
    /// ESTE buff; a cero, el buff se retira (el sistema lo administra).
    /// </summary>
    public class IraSerExteriorRecargaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;          // no es castigo: es un reloj
            Main.buffNoTimeDisplay[Type] = true; // el tiempo lo dibuja el icono
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip,
            ref int rare)
        {
            try
            {
                // v6.149 — la letra: «solo debe decir algo como, ataque de
                // ira masivo que barre con todo». SIN teclas.
                tip = "Ataque de ira masivo que barre con todo.";
            }
            catch { }
        }

        // ================================================================
        //  v6.150 — EL ICONO QUE SE ENCIENDE (PreDraw del ModBuff): el
        //  sprite gris de abajo + la parte encendida (recorte que sube
        //  con el progreso) + EL RESPLANDOR MORADO al 100 %. Devolvemos
        //  true para que vanilla NO lo dibuje de nuevo (lo pintamos
        //  nosotros completito — el texto de duración de vanilla sigue
        //  corriendo DESPUÉS de PreDraw y no lo tapamos).
        // ================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref Terraria.DataStructures.BuffDrawParams drawParams)
        {
            try
            {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return true;

                ulong resta = Systems.AtaquesCombinadosSistema.IraRestante(p);
                const ulong TOTAL = 600ul * 60ul;      // 10 minutos en ticks
                float progreso = resta <= 0ul
                    ? 1f
                    : 1f - MathHelper.Clamp(resta / (float)TOTAL, 0f, 1f);

                Texture2D tex = TextureAssets.Buff[Type].Value;
                var sb = spriteBatch;
                Vector2 pos = drawParams.Position;

                // === 1. LA BASE GRIS (el icono apagado — el reloj corriendo) ===
                Color gris = drawParams.DrawColor;
                gris.R = (byte)(gris.R * 150 / 255);
                gris.G = (byte)(gris.G * 150 / 255);
                gris.B = (byte)(gris.B * 150 / 255);
                sb.Draw(tex, pos, null, gris,
                    0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                if (progreso > 0.02f)
                {
                    // === 2. LA PARTE ENCENDIDA (recorte que SUBE — de abajo
                    //     hacia arriba, piteándose a color poco a poco) ===
                    int altoEncendido = (int)(tex.Height * progreso);
                    if (altoEncendido > 0)
                    {
                        var src = new Rectangle(0, tex.Height - altoEncendido,
                            tex.Width, altoEncendido);
                        sb.Draw(tex, pos + new Vector2(0f, tex.Height - altoEncendido), src,
                            drawParams.DrawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    }
                }

                // === 3. EL RESPLANDOR MORADO (al 100 % — la Ira LISTA) ===
                if (progreso >= 1f)
                {
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    float t = Main.GlobalTimeWrappedHourly;
                    float late = 0.7f + 0.3f * MathF.Sin(t * 3.2f);   // el latido
                    sb.Draw(glow, pos + tex.Size() * 0.5f, null,
                        new Color(190, 90, 255) * (0.45f * late),
                        0f, glow.Size() * 0.5f,
                        new Vector2(2.2f, 2.2f), SpriteEffects.None, 0f);
                }

                // === 4. EL CONTADOR mm:ss DEBAJO (el texto dorado de la
                //     casa — el reloj vivo de siempre, en su sitio) ===
                if (resta > 0ul)
                {
                    int s = (int)(resta / 60ul);
                    string texto = $"{s / 60:D2}:{s % 60:D2}";
                    var fuente = FontAssets.ItemStack.Value;
                    // LA POSICIÓN: debajo del icono (position es la
                    // esquina sup-izq del icono en coordenadas de UI).
                    Utils.DrawBorderString(sb, texto,
                        pos + new Vector2(tex.Width * 0.5f, tex.Height + 2f),
                        new Color(255, 224, 150), 0.85f, 0.5f, 0f);
                }
            }
            catch { }
            return true;   // nosotros lo dibujamos TODO (vanilla no repite)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // el buff vive mientras el reloj corra (lo administra el
            // propio sistema en su tick)
        }
    }
}
