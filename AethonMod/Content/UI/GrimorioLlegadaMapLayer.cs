using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.NPCs;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// EL FARO DEL GRIMORIO (v6.56.3) — el icono de la llegada en el mapa.
    ///
    /// LA LETRA DEL USUARIO: «La llegada del grimorio al hacerse me di
    /// cuenta de que nunca se muestra en el mapa jajajajaja, necesitamos
    /// que se muestre en el mapa el icono de la ubicación del grimorio
    /// cuando llega al mundo».
    ///
    /// LA ENTIDAD A MARCAR: el NPC <see cref="GrimorioDormido"/> — vive
    /// desde que la bruma se disipa hasta que el destinatario EXTIENDE
    /// LA MANO (o el libro es devorado): horas, sesiones, lo que haga
    /// falta. Marcar el projectile de la bruma no sirve (vive 5,3 s y
    /// ocupa el mismo sitio); marcar al NPC ES marcar la llegada — y el
    /// icono se apaga SOLO con la toma (libro.active = false), sin
    /// condiciones extra que mantener.
    ///
    /// EL MAPA JAMÁS LO SUPO: el grimorio dormido es townNPC sin textura
    /// de cabeza — el layer de cabezas de vanilla lo salta (documentado
    /// en el propio GrimorioDormido.cs: «el libro no tiene textura de
    /// cabeza, el mapa nunca lo supo»). Este ModMapLayer es la cura.
    ///
    /// LA API REAL (tML 2026.08, verificada por reflexión contra el
    /// tModLoader.dll del juego): Draw(ref MapOverlayDrawContext, ref
    /// string) — el contexto recibe coordenadas de TILE (mundo/16) y
    /// dibuja con SU propia escala de mapa; el ref string es el texto
    /// de hover.
    ///
    /// LA INTIMIDAD DEL PACTO (la ley de la v6.50.126): en MP solo el
    /// DESTINATARIO ve el faro — igual que el cuerpo (el PreDraw ya
    /// oculta el libro a los extraños). En SP siempre.
    /// </summary>
    public class GrimorioLlegadaMapLayer : ModMapLayer
    {
        private static Asset<Texture2D> _icono;

        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (Main.gameMenu || Main.dedServ || Main.netMode == 2)
                return;

            if (_icono == null || !_icono.IsLoaded)
            {
                try
                {
                    _icono = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado",
                        AssetRequestMode.ImmediateLoad);
                }
                catch
                {
                    return;   // sin icono no hay faro — jamás romper el mapa
                }
            }
            Texture2D tex = _icono.Value;
            if (tex == null || tex.IsDisposed)
                return;

            int tipoLibro = ModContent.NPCType<GrimorioDormido>();
            Player local = Main.LocalPlayer;
            float pulso = 0.72f + 0.28f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.5f);
            var frame = new SpriteFrame(1, 1);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipoLibro)
                    continue;

                // LA INTIMIDAD: en MP, solo el destinatario del libro lo ve.
                if (Main.netMode == 1 && !LaCaidaSistema.EsElDestinatario(n, local))
                    continue;

                // MUNDO → TILE: el contexto del mapa mapea solo (la
                // convención de los pilones de vanilla).
                var posicion = new Vector2(n.Center.X / 16f, n.Center.Y / 16f);
                var resultado = context.Draw(tex, posicion, Color.White * pulso,
                    frame, 0.55f, 0.55f, Alignment.Center);

                if (resultado.IsMouseOver)
                    text = "El Grimorio espera a su destinatario…";
            }
        }
    }
}
