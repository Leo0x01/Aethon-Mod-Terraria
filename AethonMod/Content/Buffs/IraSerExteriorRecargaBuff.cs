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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("IraSerExteriorRecargaBuff", exRegistro); }
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

                // ============================================================
                //  v6.185 — LA CELDA DEL BUFF ES SAGRADA (la letra: «los buff
                //  se siguen solapando… el buff de la ira de un ser exterior
                //  y el debuff de ahogo»). LA MEDIDA (sim 1:1 de la rejilla
                //  de vanilla, decompile Main.DrawInterface_Resources_Buffs:
                //  iconos nativos de 32 px en celdas de 38×50): el marco de
                //  4 px y SOBRE TODO el anillo del hold (radio 24,6 — la
                //  media diagonal) DERRAMABAN sobre la celda del vecino (28
                //  píxeles violetas sobre el icono del Ahogamiento en el
                //  sim). AHORA: el marco sale 2 px (carga: 1→2) y el anillo
                //  ABRAZA el icono con radio 18 — TODO dentro de SU celda.
                //  El resto de buffs del mod no dibuja nada fuera de su icono
                //  (auditoría v6.185: 18 ModBuff, solo este tenía PreDraw).
                // ============================================================
                var rect = new Rectangle((int)pos.X, (int)pos.Y, tex.Width, tex.Height);
                if (progreso >= 1f)
                {
                    // LISTA: el marco entero ARDE — 2 px de salida (dentro de
                    // la celda) con el latido de la casa y el velo interior
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(196, 108, 255),
                        2f, 0.95f, 1f);
                    VFX.GlowUILib.Iluminar(sb, rect, new Color(150, 84, 235), 0.35f);
                }
                else if (progreso > 0.15f)
                {
                    // CARGANDO: el marco NACE con el progreso (1→2 px) y
                    // a media intensidad — la promesa del estallido
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(150, 84, 235),
                        1f + 1f * progreso, 0.28f + 0.30f * progreso, 0.4f);
                }

                // ============================================================
                //  v6.152 — EL ANILLO DEL HOLD (la letra del gesto nuevo:
                //  «dejando precionado el click derecho durante 2
                //  segundos»): mientras SOSTIENES el click derecho, un
                //  ARCO blanco-violeta se CIERRRE alrededor del icono
                //  (0→100 % en los 2 s) — el gesto tiene SU reloj visible
                //  y sabes EXACTAMENTE cuándo va a disparar.
                // ============================================================
                float hold = p.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>()
                    .IraHoldProgreso;
                if (hold > 0f && hold < 1f)
                {
                    try
                    {
                        // v6.153 — EL ANILLO, CENTRADO DE VERDAD (la
                        // letra: «ese circulo esta decentrado, deberia
                        // estar todo en el centro del buff de la ira»):
                        // el centro ES EL CENTRO DEL ICONO (la v6.152 le
                        // sumaba +15 px en X e Y — el radio viejo
                        // contaminando el centro) y el radio ABRAZA el
                        // icono. v6.185 — RADIO 18 (antes la media diagonal,
                        // 24,6): el arco abraza el icono SIN cruzar a la celda
                        // del vecino (sim 1:1: 28 px violetas sobre el
                        // Ahogamiento → 0).
                        var glowHold = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                        Vector2 centro = pos + tex.Size() * 0.5f;
                        float radio = 18f;
                        int pasos = 28;
                        for (int k = 0; k < pasos; k++)
                        {
                            float u = k / (float)pasos;
                            if (u > hold) break;
                            float ang0 = -MathHelper.PiOver2 + u * MathHelper.TwoPi;
                            Vector2 p0 = centro + ang0.ToRotationVector2() * radio;
                            Vector2 p1 = centro + (ang0 + MathHelper.TwoPi / pasos)
                                .ToRotationVector2() * radio;
                            Vector2 medio = (p0 + p1) * 0.5f;
                            float largo = Vector2.Distance(p0, p1) + 2.5f;
                            sb.Draw(glowHold, medio, null,
                                new Color(226, 208, 255) * 0.85f,
                                ang0 + MathHelper.PiOver2, glowHold.Size() * 0.5f,
                                new Vector2(largo / glowHold.Width * 0.30f, 0.10f),
                                SpriteEffects.None, 0f);
                        }
                    }
                    catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("IraSerExteriorRecargaBuff", exRegistro); }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("IraSerExteriorRecargaBuff", exRegistro); }
            // v6.184 — EL SOLAPAMIENTO MUERE (la letra: «todos los buff
            // que tenemos son creados usando buff de terraria como base…
            // cuando el buff base se activa entonces el buff base y el
            // que tenemos se solapan… ninguno de los buff o debuff que
            // tenemos debe solaparse con otro»). LA CAUSA (verificada
            // contra el decompile Main.DrawBuffIcon:56397): PreDraw
            // devolviendo TRUE manda a vanilla a dibujar el icono OTRA
            // VEZ ENCIMA del nuestro — el icono DOBLE sobre SU propia
            // celda y el marco de luz derramándose en la del vecino
            // (el Ahogamiento del usuario). Nosotros lo dibujamos TODO
            // (la base gris, el recorte encendido y el marco): vanilla
            // NO repite — return FALSE.
            return false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // el buff vive mientras el reloj corra (lo administra el
            // propio sistema en su tick)
        }
    }
}
