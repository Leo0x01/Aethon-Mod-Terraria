using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ====================================================================
    //  v6.58 — RAYOSPRUEBAENCIMASISTEMA — EL ATAQUE SOBRE TODO EL MUNDO.
    //
    //  LA LETRA: «algunas sprites del juego como las hojitas que caen de
    //  los árboles se ven sobre el ataque y eso no debe ser así, el
    //  ataque debe estar sobre todo».
    //
    //  EL DIAGNÓSTICO: vanilla dibuja los proyectiles ANTES del polvo,
    //  el gore y las partículas ambientales (las hojitas de los árboles
    //  viven en esos pases tardíos) — TODO lo que cae después del pase
    //  de proyectiles pasa POR ENCIMA del conducto. No era un bug del
    //  haz: era EL LUGAR del frame donde vivía.
    //
    //  LA CURA — EL PATRÓN ENCIMA-DE-TODO (el punto aprobado de la casa,
    //  el mismo de PantallaSistema/OndaSystem/OcasoSystem):
    //  PostDrawInterface corre DENTRO de la capa "Vanilla: Mouse Text"
    //  (InterfaceScaleType.UI), DESPUÉS de que TODO el mundo ya se
    //  dibujó — hojas, polvos y gore incluidos. Ahí se vuelca el
    //  dibujado ENTERO del ataque (DibujarAtaque: la boca del conjuro +
    //  la pila de 7 capas) con la MISMA matemática y el MISMO volumen
    //  (cero costo nuevo, cero picos nuevos — solo cambia EL LUGAR del
    //  frame).
    //
    //  EL BAILE DEL LOTE (la parte delicada — cada paso es un patrón
    //  verificado de la casa):
    //    (1) CerrarLoteSiAbierto — cierra el lote de INTERFAZ abierto en
    //        este punto del frame (la sonda: cero first-chance).
    //    (2) EL LOTE DEL MUNDO: se abre con los parámetros EXACTOS del
    //        pase de entidades de vanilla (los medidos de
    //        ReabrirLoteVanilla: Deferred · AlphaBlend ·
    //        DefaultSamplerState · None · Main.Rasterizer) y la MATRIZ
    //        DEL MUNDO (GameViewMatrix.TransformationMatrix — la misma
    //        de FlushAdditive/AbrirAdditive).
    //        EL DESCUBRIMIENTO DEL ORÁCULO (verificado en el IL del tML
    //        2026.08 real): Main.Transform es SOLO-LECTURA (CS0200) y
    //        SIEMPRE devuelve GameViewMatrix.TransformationMatrix —
    //        sea cual sea el contexto de dibujo. Consecuencias: (a) TODO
    //        ReabrirLoteVanilla del cuerpo reabre EN MUNDO aun estando
    //        en la interfaz — EXACTO lo que el haz necesita; (b) los
    //        PlayerInput.SetZoom_* de este tML SON REMAPEADORES DE
    //        RATÓN (SetZoom_Scaled/MouseInWorld solo escriben mouseX/
    //        mouseY/screenWidth…) — NO se tocan: llamarlos aquí habría
    //        CORROMPIDO el puntero durante el haz.
    //    (3) DibujarAtaque — el cuerpo intacto del haz.
    //    (4) finally: se cierra lo que quedó vivo y se reabre EL LOTE
    //        DE UI con los parámetros del fix v6.50.2 de PantallaSistema
    //        (Deferred · AlphaBlend · LinearClamp · None ·
    //        CullCounterClockwise · Main.UIScaleMatrix) — el frame sin
    //        haz no toca el spriteBatch NI UNA vez (la lección v6.90).
    //
    //  ANTI-PICOS (la regla de la casa, verificada por capa): no se
    //  añade NI UNA capa nueva de dibujado — la pila es la misma de
    //  siempre, en otro punto del frame. El costo nuevo por frame es UN
    //  escaneo de Main.projectile (una comparación de int por slot) y,
    //  solo mientras vive un haz, un End+Begin de lote. El dibujado del
    //  strip (DrawUserPrimitives) no pasa por el SpriteBatch y sus
    //  shaders llevan SU matriz (uTransformMatrix =
    //  GameViewMatrix.NormalizedTransformationmatrix): intactos.
    //
    //  NOTA DE FUTURO: cuando el ganador (la receta C) se copie de
    //  vuelta al RC2 original, el escaneo de abajo solo necesita sumar
    //  el tipo del original a la lista.
    // ====================================================================
    public class RayosPruebaEncimaSistema : ModSystem
    {
        /// <summary>El tipo de las copias del RC2 (cacheado al cargar).</summary>
        private static int _tipo;

        /// <summary>El pozo cero-GC de la casa: la lista se reusa cada
        /// frame (nunca un new por frame).</summary>
        private static readonly List<RayosPruebaProjectile> _vivos = new List<RayosPruebaProjectile>(4);

        public override void OnModLoad()
        {
            _tipo = ModContent.ProjectileType<RayosPruebaProjectile>();
        }

        /// <summary>EL PUNTO ÚNICO DEL ATAQUE ENCAPIMA: todo el dibujado
        /// de las copias del RC2 vive aquí, después de TODO el mundo.</summary>
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            // Las guardas de la casa: el menú no tiene mundo y el server
            // no tiene pantalla.
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return;

            // === EL ESCANEO (cero-GC: el pozo se reusa) ===
            _vivos.Clear();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == _tipo &&
                    p.ModProjectile is RayosPruebaProjectile haz)
                {
                    _vivos.Add(haz);
                }
            }
            // El frame SIN haz no toca el spriteBatch NI UNA vez (la
            // lección v6.90: cero churn, cero first-chance).
            if (_vivos.Count == 0) return;

            // === EL BAILE DEL LOTE ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // (2) EL LOTE DEL MUNDO: la MISMA matriz del pase de
                // entidades (Main.Transform en este tML es SIEMPRE
                // GameViewMatrix.TransformationMatrix — leído en su IL:
                // los ReabrirLoteVanilla del cuerpo reabren con ESA,
                // así que el lote propio abre con la MISMA para que TODO
                // dibuje en una sola verdad de coordenadas). Los
                // SetZoom_* del PlayerInput NO SE TOCAN: en este tML son
                // remapeadores de RATÓN, no de transform (llamarlos
                // habría corrompido el puntero).
                Matrix matrizMundo = Main.GameViewMatrix.TransformationMatrix;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, matrizMundo);
                try
                {
                    // (3) EL ATAQUE — la boca + la pila, cuerpo intacto.
                    for (int i = 0; i < _vivos.Count; i++)
                    {
                        try { _vivos[i].DibujarAtaque(); }
                        // El blindaje de la casa (la lección v6.41): un
                        // efecto que tire no puede arrastrar el frame —
                        // se cierra lo suelto y sigue el siguiente.
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                    }
                }
                finally
                {
                    // El dibujado puede dejar lotes propios vivos (los
                    // aditivos de los soles, el restore final): TODO lo
                    // que siga abierto se cierra antes de devolver el
                    // frame a la interfaz.
                    VFXCore.CerrarLoteSiAbierto();
                }
            }
            finally
            {
                // (4) LA DEVOLUCIÓN: el lote de UI se reabre con los
                // parámetros EXACTOS del fix v6.50.2 de PantallaSistema
                // (este punto del frame — Mouse Text — corre con la
                // UIScaleMatrix). Nada de contextos de zoom que
                // devolver: nunca se tocaron.
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
    }
}
