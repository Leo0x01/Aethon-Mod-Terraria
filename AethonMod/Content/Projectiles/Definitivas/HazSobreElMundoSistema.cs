using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ====================================================================
    //  v6.59 — HAZSOBREELMUNDOSISTEMA — EL ATAQUE POR ENCIMA DE TODO EL
    //  MUNDO, POR DEBAJO DE LA INTERFAZ.
    //
    //  LA LETRA: «tienes que hacer que el rayo este por encima de todo,
    //  pero por debajo de la interfaz de usuario».
    //
    //  EL PUNTO EXACTO DEL FRAME (leído en el IL del tML 2026.08 real —
    //  Main.DoDraw → Main.DrawInterface):
    //    · El mundo entero — tiles, NPCs, proyectiles, JUGADOR, polvo,
    //      gore, las hojitas de los árboles, el texto de combate — se
    //      dibuja ANTES de Main.DrawInterface (la secuencia de vanilla
    //      cierra su lote en el spriteBatch.End() justo antes de la
    //      interfaz).
    //    · DrawInterface llama a SystemLoader.ModifyInterfaceLayers y
    //      luego dibuja la LISTA DE CAPAS en orden (el bucle
    //      `while (enumerator.MoveNext() && enumerator.Current.Draw())`).
    //    · PostDrawInterface (el punto de la v6.58) corre DENTRO de la
    //      capa "Vanilla: Mouse Text" — la ÚLTIMA — o sea ENCIMA de la
    //      interfaz: el haz TAPABA el hotbar, la vida y el mapa. La
    //      letra de la cura: el haz vive DEBAJO de la interfaz.
    //
    //  LA CURA — UNA CAPA PROPIA EN EL ÍNDICE 0 DE LA LISTA: esta casa
    //  inserta "AethonMod: El haz sobre el mundo" ANTES de la primera
    //  capa de vanilla ("Vanilla: Interface Logic 1"). El haz se dibuja
    //  después de TODO el mundo y antes de TODA la interfaz — nombres de
    //  jugadores, cursores inteligentes, barras de vida, minimapa: TODO
    //  por encima del conducto. Ni las hojas ni el polvo pueden taparlo
    //  (nacen antes), y la interfaz nunca queda sepultada (nace después).
    //
    //  EL CONTRATO DEL LOTE (leído en el IL de GameInterfaceLayer.Draw
    //  de este tML): la capa ABRE ELLA MISMA el lote antes de llamar al
    //  delegado — SetZoom_UI + Begin(Deferred, …, Main.UIScaleMatrix) —
    //  y lo CIERRA (End) justo después. El delegado, por tanto:
    //    (1) cierra el lote de la capa que lo recibió abierto,
    //    (2) abre EL LOTE DEL MUNDO (los parámetros EXACTOS del pase de
    //        entidades de vanilla: Deferred · AlphaBlend ·
    //        Main.DefaultSamplerState · None · Main.Rasterizer · la
    //        GameViewMatrix.TransformationMatrix — la misma que usan los
    //        ReabrirLoteVanilla/FlushAdditive del cuerpo: UNA sola
    //        verdad de coordenadas),
    //    (3) dibuja las ceremonias de la Ira (primero) y los haces
    //        (encima) — MISMA matemática, MISMO volumen,
    //    (4) CIERRRA su lote y REABRE el lote de interfaz que la capa
    //        esperaba encontrar (los parámetros del fix v6.50.2 de
    //        PantallaSistema: Deferred · AlphaBlend · LinearClamp ·
    //        None · CullCounterClockwise · Main.UIScaleMatrix) — para
    //        que el End() de la capa cierre algo VIVO y ni una
    //        first-chance toque el log.
    //  El frame SIN haz no toca el spriteBatch NI UNA vez: la capa se
    //  registra una vez y el delegado devuelve true sin abrir nada (la
    //  lección v6.90 — cero churn, cero first-chance).
    // ====================================================================
    public class HazSobreElMundoSistema : ModSystem
    {
        /// <summary>El tipo de las copias del RC2 (cacheado al cargar).</summary>
        private static int _tipoHaz;

        /// <summary>El tipo de la ceremonia de la Ira de un Ser Exterior.</summary>
        private static int _tipoCeremonia;

        /// <summary>Los pozos cero-GC de la casa: las listas se reusan cada
        /// frame (nunca un new por frame).</summary>
        private static readonly List<RayosPruebaProjectile> _haces = new List<RayosPruebaProjectile>(4);
        private static readonly List<IraSerExteriorCeremoniaProjectile> _rituales =
            new List<IraSerExteriorCeremoniaProjectile>(2);

        public override void OnModLoad()
        {
            _tipoHaz = ModContent.ProjectileType<RayosPruebaProjectile>();
            _tipoCeremonia = ModContent.ProjectileType<IraSerExteriorCeremoniaProjectile>();
        }

        /// <summary>EL PUNTO ÚNICO DEL ATAQUE ENCAPIMA DEL MUNDO: la capa
        /// propia en el ÍNDICE 0 de la lista de la interfaz — después de
        /// TODO el mundo, antes de TODA la interfaz.</summary>
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Las guardas de la casa: sin capa en el menú (no hay mundo
            // que golpear) — el delegado mismo filtra los frames vacíos.
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: El haz sobre el mundo",
                DibujarElConjuro, InterfaceScaleType.UI));
        }

        /// <summary>EL DELEGADO DE LA CAPA — el dibujado entero del
        /// ataque (ceremonias de la Ira + haces del RC2) en el punto del
        /// frame que vive ENCIMA del mundo y DEBAJO de la interfaz.</summary>
        private bool DibujarElConjuro()
        {
            // Las guardas de la casa: el menú no tiene mundo y el server
            // no tiene pantalla.
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return true;

            // === EL ESCANEO (cero-GC: los pozos se reusan) ===
            _rituales.Clear();
            _haces.Clear();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;
                if (p.type == _tipoHaz && p.ModProjectile is RayosPruebaProjectile haz)
                    _haces.Add(haz);
                else if (p.type == _tipoCeremonia &&
                    p.ModProjectile is IraSerExteriorCeremoniaProjectile ritual)
                    _rituales.Add(ritual);
            }
            // El frame SIN ataque no toca el spriteBatch NI UNA vez (la
            // lección v6.90: la capa de la vanilla ya dejó su lote abierto
            // — se devuelve tal cual y su End() lo cierra limpio).
            if (_haces.Count == 0 && _rituales.Count == 0) return true;

            // === EL BAILE DEL LOTE ===
            try
            {
                // (1) CERRAR el lote de interfaz que la capa abrió.
                VFXCore.CerrarLoteSiAbierto();

                // (2) EL LOTE DEL MUNDO: la MISMA matriz del pase de
                // entidades (los ReabrirLoteVanilla del cuerpo reabren
                // con GameViewMatrix.TransformationMatrix — el lote
                // propio abre con LA MISMA para que TODO dibuje en una
                // sola verdad de coordenadas).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null,
                    Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // (3) EL ATAQUE — primero LAS CEREMONIAS (la bruma y
                    // los círculos de la Ira, debajo), luego LOS HACES
                    // (el conducto, encima).
                    for (int i = 0; i < _rituales.Count; i++)
                    {
                        try { _rituales[i].DibujarRitual(); }
                        // El blindaje de la casa (la lección v6.41): un
                        // efecto que tire no puede arrastrar el frame.
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                    }
                    for (int i = 0; i < _haces.Count; i++)
                    {
                        try { _haces[i].DibujarAtaque(); }
                        catch { VFXCore.CerrarLoteSiAbierto(); }
                    }
                }
                finally
                {
                    // El dibujado puede dejar lotes propios vivos (los
                    // aditivos de los soles, el restore final): TODO lo
                    // que siga abierto se cierra antes de devolver el
                    // frame a la capa.
                    VFXCore.CerrarLoteSiAbierto();
                }
            }
            catch
            {
                // La puerta blindada: pase lo que pase, el frame sigue.
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            finally
            {
                // (4) LA DEVOLUCIÓN: el lote que la capa esperaba — la
                // capa llamará a su End() al volver del delegado, y el
                // End necesita un Begin VIVO. Los parámetros EXACTOS del
                // fix v6.50.2 de PantallaSistema (el lote de UI de este
                // punto del frame).
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
            return true;
        }
    }
}
