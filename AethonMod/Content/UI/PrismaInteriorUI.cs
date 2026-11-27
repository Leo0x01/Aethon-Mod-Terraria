using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  PrismaInteriorUI — v6.115 → v6.174 — EL MOTOR DEL PILAR PRISMA:
    //  EL CONTROL DEL CUERPO ENTERO DEL DIOS.
    //
    //  LA LETRA (v6.137): «el pilar de prisma será capaz de activar las
    //  oleadas de hambre dependiendo del último nivel en el que quedó el
    //  jugador, también permitirá elegir el nivel de la misma pero
    //  siempre será hasta el último nivel desbloqueado por el jugador, y
    //  para desbloquear un nivel el jugador debe completar ese nivel de
    //  la oleada de hambre fuera del mundo interior» + «el pilar de
    //  prisma también permitirá invocar a todos los jefes que el jugador
    //  haya vencido y devorado, cada jefe devorado se podrá invocar en
    //  una versión aún más poderosa dentro del mundo interior».
    //
    //  v6.163 — EL PILAR VIVE EN EL LIBRO: el panel FLOTANTE murió — el
    //  contenido se dibuja como PESTAÑA del libro (PilarDelLibro, con
    //  la tipografía y la paleta EXACTAS de la página de información).
    //  Este sistema es EL MOTOR: la ley de distancia y TODA la lógica
    //  de clic (invocar, la oleada, los jefes). El libro llama; el
    //  pilar hace.
    //
    //  v6.174 — EL MOTOR SE QUEDA SOLO (la letra: «en el mundo interior,
    //  al usar el pilar los botones no hacen nada»): la causa estaba en
    //  el motor de clic del LIBRO (el «frame de gracia» con un reloj que
    //  vuelve a cero al cargar el mundo), no aquí — pero este archivo
    //  aún guardaba 500 líneas de dibujo MUERTO con su propio reloj de
    //  rueda, su `_ultimoClicIzq` y su ESC: un segundo motor fantasma
    //  que nada llamaba. Muere entero. Lo que queda es la lógica pura.
    // ======================================================================
    public class PrismaInteriorUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;

        /// <summary>¿La ventana está abierta?</summary>
        public static bool Abierta => _abierta;

        /// <summary>v6.164 — EL CIERRE POR ORDEN DEL LIBRO (ESC/X en
        /// la pestaña pilar).</summary>
        public static void ForzarCierre() => _abierta = false;

        // ================================================================
        //  LOS CLICS GRANULARES DEL PILAR: la página del libro llama a
        //  ESTOS métodos con el flanco de la casa — cada botón, su función.
        // ================================================================
        public static void ClicEnVolver()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                _abierta = false;
                if (LibroDelEternoUI.Abierto) LibroDelEternoUI.Cerrar();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                Systems.SubmundoGrimorio.Salir(yo);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        public static void ClicEnInvocar()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                InvocarLaLlamada(yo);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        public static void ClicEnSellado()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                if (!NPCs.AethonSellado.Invocar(yo))
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        public static void ClicEnLanzar(int nivel)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                nivel = Math.Max(1, nivel);   // v6.177: sin tope — el nivel crece por siempre
                LanzarLaOleada(yo, nivel);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        public static void ClicEnJefe(int tipoJefe)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                InvocarJefeInterior(yo, tipoJefe);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        /// <summary>EL PRISMA SE ABRE (o se cierra).</summary>
        public static void Alternar(int i, int j)
        {
            if (_abierta)
            {
                // YA ABIERTO: se cierra — y el libro con él (si está en SU pestaña)
                ForzarCierre();
                if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 2)
                    LibroDelEternoUI.Cerrar();
                return;
            }
            _abierta = true;
            _tileX = i;
            _tileY = j;
            // el selector arranca en el ÚLTIMO nivel desbloqueado
            try
            {
                var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
                if (sp != null)
                {
                    _nivelElegido = Math.Max(1, Math.Min(10, sp.FuriaNivel));
                    PilarDelLibro.NivelElegido = _nivelElegido;
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        private static int _nivelElegido = 1;

        public override void OnWorldUnload() { _abierta = false; }
        public override void Unload() { _abierta = false; }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // LA LEY DE DISTANCIA DEL PILAR FÍSICO: cada frame, sin pilar
            // (lejos) o sin mundo interior, se cierra; y si el libro está
            // en la pestaña pilar, se cierra TAMBIÉN.
            if (!_abierta) return;
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || Main.gameMenu || Main.dedServ ||
                    !Systems.SubmundoGrimorio.Activo)
                {
                    CerrarConElLibro();
                    return;
                }
                float dist = Vector2.Distance(yo.Center,
                    new Vector2(_tileX * 16f + 24f, _tileY * 16f + 8f));
                if (dist > 280f) CerrarConElLibro();
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        private static void CerrarConElLibro()
        {
            _abierta = false;
            if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 2)
                LibroDelEternoUI.Cerrar();
        }

        // ================================================================
        //  LA OLEADA DEL PILAR (v6.137): el festín nace DENTRO del cuerpo
        //  — nivel elegido en el selector (1..el último desbloqueado).
        //  natural:false para JAMÁS subir el FuriaNivel (probar no es
        //  progresar — la ley v6.50.74: desbloquear es COMPLETAR FUERA).
        // ================================================================
        private static void LanzarLaOleada(Player yo, int nivel)
        {
            try
            {
                if (Systems.GrimorioFuriaSistema.Activo) return;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                Systems.GrimorioFuriaSistema.Provocar(yo,
                    Math.Max(1, nivel), natural: false);   // v6.177: sin tope
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        // ================================================================
        //  EL JEFE INTERIOR (v6.137): el devorado elegido nace delante del
        //  pilar, sellado por la arena (aura de oleada + stats ×(1+0,25·
        //  kills) + IA por niveles). Solo AUTORIDAD.
        // ================================================================
        private static void InvocarJefeInterior(Player yo, int tipoJefe)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                // v6.175 — SIN LÍMITE (la letra: «en el mundo interior no
                // deben haber limites para la cantidad de jefes que se
                // pueden invocar»): la ley de «la arena es de UN duelista»
                // MUERE — cada clic del pilar invoca a SU jefe, vivan los
                // que vivan. La arena es del CUERPO ENTERO del dios: tan
                // grande como el hambre del portador.

                var sp = yo.GetModPlayer<Players.ShardPlayer>();
                int kills = sp != null ? sp.KillsInteriorDe(tipoJefe) : 0;
                // v6.177 — EL NIVEL SIN TOPE (la letra: «ademas en el mundo
                // interior los jefes no necesitan estar limitados a nivel
                // 10, que este nivel sea infitino y cuanto mas nivels mas
                // complicado, mas inteligente y tiene nuevos ataques,
                // combos y nuevos movimientos, asi como una IA mas
                // avanzada»): 1 + kills, SIN tope — la escalera de
                // desbloqueos del motor crece por siempre y la barra
                // muestra la estatura REAL.
                int nivelIA = 1 + kills;

                // v6.177 — LA ASPIRADORA YA NO NACE AQUÍ: el vigía
                // universal de FaucesGlobalNPC.OnSpawn cuenta TODAS las
                // invocaciones del Devorador (pilar · oleadas · comida de
                // gusano del mundo real) y arranca el cine espacial SOLO en
                // la SEGUNDA invocación — el pilar solo viste al jefe de
                // arena. (La letra: la aspiradora funciona en TODAS partes,
                // no solo en el interior.)

                // v6.141 — EL ANCLA LEJOS (la letra: «en el mundo interior
                // los jefes deben aparecer alejados del jugador»): 100
                // tiles de distancia del lado del jugador — el jefe NACE
                // lejos y CAMINA hacia la pelea.
                int dirX = yo.Center.X < _tileX * 16f ? -1 : 1;
                int tileX = _tileX + dirX * 100;
                int tileY = _tileY - 2;
                int suelo = -1;
                for (int y = tileY; y < tileY + 60 && y < Main.maxTilesY - 10; y++)
                {
                    if (WorldGen.SolidTile(tileX, y)) { suelo = y; break; }
                }
                Vector2 ancla = suelo >= 0
                    ? new Vector2(tileX * 16f + 8f, suelo * 16f - 40f)
                    : new Vector2(tileX * 16f + 8f, (_tileY + 2) * 16f);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                int idx = NPC.NewNPC(yo.GetSource_FromThis(), (int)ancla.X, (int)ancla.Y,
                    tipoJefe, 0, 0f, yo.whoAmI);
                if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active)
                {
                    Globals.JefeInteriorGlobalNPC.Sellar(Main.npc[idx], nivelIA);
                    Main.npc[idx].netUpdate = true;

                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("PrismaInteriorUI", exRegistro); }
        }

        // ================================================================
        //  LA LLAMADA NACE DEL PRISMA (v6.123 — LA CEREMONIA MURIÓ): los
        //  DOS fragmentos nacen AL INSTANTE delante del prisma con SUS
        //  estallidos de luz de nacimiento.
        // ================================================================
        private static void InvocarLaLlamada(Player yo)
        {
            // EL ANCLA: delante del prisma, del lado del jugador.
            int dirX = yo.Center.X < _tileX * 16f ? -1 : 1;
            int tileX = _tileX + dirX * 9;
            int tileY = _tileY - 2;
            int suelo = -1;
            for (int y = tileY; y < tileY + 60 && y < Main.maxTilesY - 10; y++)
            {
                if (WorldGen.SolidTile(tileX, y)) { suelo = y; break; }
            }
            Vector2 ancla = suelo >= 0
                ? new Vector2(tileX * 16f + 8f, suelo * 16f)
                : new Vector2(tileX * 16f + 8f, (_tileY + 2) * 16f);

            Vector2 posLuz = ancla + new Vector2(-600f, -600f) +
                Main.rand.NextVector2Circular(100f, 100f);
            NPC.SpawnBoss((int)posLuz.X, (int)posLuz.Y,
                ModContent.NPCType<NPCs.AethonBoss>(), yo.whoAmI);

            Vector2 posOscuro = ancla + new Vector2(600f, -600f) +
                Main.rand.NextVector2Circular(100f, 100f);
            NPC.SpawnBoss((int)posOscuro.X, (int)posOscuro.Y,
                ModContent.NPCType<NPCs.AethonBossOscuro>(), yo.whoAmI);
        }
    }
}
