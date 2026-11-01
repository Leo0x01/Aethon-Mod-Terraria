using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PulsoSistema — v6.49 → v6.80 — EL CORAZÓN QUE FALTABA… Y AHORA
    /// TAMBIÉN LOS OJOS.
    ///
    /// v6.49 (hallazgo nº1 de la auditoría AUD-A): PulsoLib.ActualizarPantalla()
    /// NO TENÍA NINGÚN CALL-SITE — el trauma se acumulaba para siempre y los
    /// empujes de EmpujarPantalla() decaían en el vacío. Este sistema lo
    /// enchufó: un latido por frame + la higiene de las estáticas entre mundos.
    ///
    /// v6.80 — LA CURA DEL FLASH INVISIBLE (el bug nº1 del Grupo B de la
    /// auditoría v6.78): SEIS empujes (Apuestas×4 + VerboPrimordial×2)
    /// escribían _fuerzaPantalla/_tintePantalla desde la v6.31… y el ÚNICO
    /// lector de ese estado era el HUD del F8 (DiagnosticoVFXSystem) —
    /// NADIE lo dibujaba: cinco armas «iluminaban» una pantalla que no
    /// existía. ESTE override lo dibuja por fin: EL VELO DE TINTE, un quad
    /// plano de pantalla completa (AlphaBlend, alfa = fuerza·0,6 — el
    /// «latido del error» gris que ENFRÍA el mundo, el baño azul de la
    /// apertura de las apuestas, el resplandor cálido del estruendo del
    /// verbo). El diseño respeta la LECCIÓN DE LA VIÑETA (v6.50.28, la saga
    /// del círculo plano): un degradado radial SIEMPRE se lee como círculo —
    /// el velo es PLANO y uniforme, pixel 1×1 propio (nada de SoftGlow
    /// estirado). La cura NO toca ni una línea de las armas: los 6 empujes
    /// siguen llamando a EmpujarPantalla tal cual; ahora simplemente ALGUIEN
    /// los dibuja. Y el F8 ya no muestra un número fantasma: muestra lo que
    /// el jugador VE.
    ///
    /// SP y MP-cliente (en servidor no hay pantalla que sacudir ni teñir:
    /// los guards internos respetan eso — esto es la correa de transmisión,
    /// no el motor).
    /// </summary>
    public class PulsoSistema : ModSystem
    {
        // EL PIXEL PLANO DE LA CASA (1×1 blanco): el velo de tinte necesita
        // UNIFORMIDAD — la lección de la viñeta. Nace lazy en el hilo de
        // dibujado (patrón de las texturas procedurales de la casa) y muere
        // en Unload (cero fugas entre recargas).
        private static Texture2D _pixel;

        private static Texture2D Pixel
        {
            get
            {
                if (_pixel == null || _pixel.IsDisposed)
                {
                    try
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    catch { return null; }
                }
                return _pixel;
            }
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            try { PulsoLib.ActualizarPantalla(); }
            catch { }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            // Guardas de la casa: sin empuje vivo no se toca NADA (el frame
            // vacío es gratis), sin cliente no hay pantalla, y el velo pasa
            // por LA PUERTA DE CALIDAD de los efectos de pantalla completa
            // (familia PostProceso — la misma puerta que la viñeta).
            if (PulsoLib.FuerzaPantalla <= 0.004f) return;
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            if (!VFXCore.CalidadPermitida(VFXCore.CalidadFX.PostProceso)) return;

            Texture2D tex = Pixel;
            if (tex == null) return;

            // EL VELO: alfa proporcional a la fuerza que aún vive (la fuerza
            // nace 0,16-0,40 y decae ×0,86/tick — el alfa pico queda en
            // 0,10-0,24, un BAÑO sutil, jamás una tapa).
            float a = MathHelper.Clamp(PulsoLib.FuerzaPantalla * 0.60f, 0f, 0.55f);
            Color t = PulsoLib.TintePantalla;
            var velo = new Color(t.R, t.G, t.B, (byte)(int)(255f * a));

            // El baile de lote de la casa (patrón OndaSystem v6.50.11):
            // cerrar por sonda, dibujar en Identity (pantalla EXACTA) y
            // devolver el lote de interfaz con SU matriz — todo en
            // try/catch/finally (la lección v6.41: nunca dejar el lote abierto).
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullCounterClockwise, null, Matrix.Identity);

                Main.spriteBatch.Draw(tex,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), velo);
            }
            catch { }
            finally
            {
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

        public override void OnWorldUnload()
        {
            try { PulsoLib.Reset(); } catch { }
        }

        public override void Unload()
        {
            try { PulsoLib.Reset(); } catch { }
            try { _pixel?.Dispose(); } catch { }
            _pixel = null;
        }
    }
}
