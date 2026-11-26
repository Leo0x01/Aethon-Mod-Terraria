using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// ASPIRADORADEVORADORPROJECTILE — v6.176 — LA DEVORACIÓN ESPACIAL DEL
    /// GUSANO, REESCRITA (la letra de la sesión: «la animacion de devorar
    /// al Devoramundos esta mal, para que esta animacion se ejecute sin
    /// falta el grimorio debe devorar al Devoramundos con una animacion
    /// especial, donde el Devoramundos flota completo en el aire por
    /// piezas, cada pieza individual flota y todas son devoradas al mismo
    /// tiempo, para que esto funcione el grimorio debe esperar a que el
    /// Devoramundos se genere por completo, tal vez en 5 segundos desde
    /// que es invocado y luego lo devora»).
    ///
    /// LA PELÍCULA NUEVA (la v6.175 aspiraba la cadena de la cola a la
    /// cabeza, pieza por pieza — y nacía ANTES de que el gusano terminara
    /// de generarse: por eso «la animacion esta mal»):
    ///    0-300   LA VIGILIA — el libro desciende y ESPERA a que el
    ///            Devoramundos se genere por completo (5 segundos de la
    ///            letra — o antes si la cadena lleva 90 ticks estable).
    ///            El gusano VIVE: se mueve y pelea mientras el libro
    ///            mira (su ojo lo sigue).
    ///   CAPTURA  LA CAPTURA — todas las piezas se cosechan VIVAS, se
    ///            CONGELAN (FaucesGlobalNPC.CongeladoPorAspiradora: ni
    ///            IA ni un proyectil — la letra de los jefes devorados)
    ///            y quedan intangibles e inalcanzables.
    ///  330-410   LA FLOTACIÓN — el gusano entero SUBE flotando POR
    ///            PIEZAS: cada pieza individual lleva SU vaivén propio
    ///            y la cadena se DESENREDA (las secciones se separan).
    ///  410-470   EL TRAGO — TODAS las piezas vuelan a la boca del libro
    ///            A LA VEZ y mueren A LA VEZ (muerte real, loot normal —
    ///            la letra: «cuenta como una muerte normal»).
    ///  470-540   EL ASCO — el libro SE ESTREMECE, se cierra de golpe y
    ///            habla EN LA MENTE del portador (EcoRed.HablarAlPortador
    ///            — la letra: el comentario «debe ser dicho con la
    ///            caracteristica forma del grimorio de hablar en la
    ///            mente del jugador no en el chat»).
    ///  540-640   EL POSO — bruma sobre el botín y adiós.
    ///
    /// ai[0] = whoAmI de la CABEZA · ai[1] = whoAmI del portador · ai[2] = tick.
    /// </summary>
    public class AspiradoraDevoradorProjectile : ModProjectile
    {
        // === LA LÍNEA DE TIEMPO DEL CINE (60 t = 1 s) ===
        private const float T_ESPERA = 300f;     // LA VIGILIA (la letra: «tal vez en 5 segundos»)
        private const float T_CAPTURA = 330f;    // la captura dura 30 t
        private const float T_FLOTACION = 410f;  // la flotación por piezas
        private const float T_TRAGO = 470f;      // EL TRAGO: todas a la vez
        private const float T_ASCO = 540f;       // el comentario en la mente
        private const float T_CIERRE = 610f;     // el libro cerrado del todo
        private const float T_FINAL = 650f;      // el poso se apaga

        private NPC Cabeza => Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.npc.Length
            ? null : Main.npc[(int)Projectile.ai[0]];

        private Player Portador
        {
            get
            {
                int i = (int)Projectile.ai[1];
                if (i < 0 || i >= Main.maxPlayers) return null;
                Player p = Main.player[i];
                return p != null && p.active ? p : null;
            }
        }

        /// <summary>LA BOCA del libro: flotando sobre la bestia.</summary>
        private Vector2 BocaLibro => _anclaLibro;

        private Vector2 _anclaLibro;
        private bool _anclaLista;
        private readonly List<int> _secciones = new();
        private readonly List<Vector2> _posiciones = new();   // dónde vivía cada pieza al capturarla
        private readonly List<Vector2> _posicionesTrago = new();   // dónde quedó al final de la flotación (el sim lo pidió)
        private bool _capturada;
        private bool _tragadas;                               // TODAS murieron a la vez
        private bool _comentarioDicho;
        private int _cuentaAnterior = -1;                     // LA ESTABILIDAD de la cadena
        private int _tickEstable;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 700;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC cabeza = Cabeza;
            bool vivoCabeza = cabeza != null && cabeza.active;

            // === LA CANCELACIÓN: la presa murió antes de la captura (el
            //     jugador la mató en la vigilias) — el cine se despide
            //     sin congelar a nadie (jamás un gusano zombi) ===
            if (!_capturada && !vivoCabeza && t > 30f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;   // el cliente ve el cine por su PreDraw

            // EL ANCLA del libro: sobre el CENTRO de la bestia
            if (!_anclaLista && vivoCabeza)
            {
                _anclaLibro = cabeza.Center + new Vector2(0f, -340f);
                _anclaLista = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.8f), _anclaLibro);
            }
            if (!_anclaLista) return;

            // ============================================================
            //  FASE 0 — LA VIGILIA: esperar a que el gusano se genere POR
            //  COMPLETO (la letra). LA ESTABILIDAD: la cadena está lista
            //  cuando su número de piezas NO CRECE durante 90 ticks; la
            //  ley de la letra manda el tope: 5 segundos.
            // ============================================================
            if (!_capturada)
            {
                int piezas = ContarPiezas();
                if (piezas != _cuentaAnterior)
                {
                    _cuentaAnterior = piezas;
                    _tickEstable = 0;
                }
                else if (++_tickEstable >= 90) _tickEstable = 90;
                bool completo = _tickEstable >= 90;
                if (t >= T_ESPERA || (completo && t >= 120f))
                    Capturar();
            }

            // ============================================================
            //  FASE 2 — LA FLOTACIÓN POR PIEZAS (la letra: «el
            //  Devoramundos flota completo en el aire por piezas, cada
            //  pieza individual flota»): cada sección sube desde SU
            //  posición capturada, con SU vaivén propio (fase y periodo
            //  por pieza), y la cadena se DESENREDA — las piezas se
            //  apartan unas de otras («todas sus secciones son
            //  separadas»).
            // ============================================================
            if (_capturada && !_tragadas && t < T_TRAGO && t >= T_CAPTURA + 10f)
            {
                float sube = MathHelper.Clamp((t - T_CAPTURA) / 60f, 0f, 1f);
                float desenreda = MathHelper.Clamp((t - T_CAPTURA - 25f) / 55f, 0f, 1f);
                int n = Math.Max(1, _secciones.Count);
                for (int i = 0; i < n; i++)
                {
                    NPC seg = _secciones[i] < Main.npc.Length ? Main.npc[_secciones[i]] : null;
                    if (seg == null || !seg.active) continue;
                    Vector2 base_ = _posiciones[i];
                    // EL ASCENSO: el gusano entero se alza al aire
                    base_ += new Vector2(0f, -(120f + 26f * (n - i) * 0.14f)) * sube;
                    // EL DESENREDO: cada pieza se abre de sus vecinas
                    // (la cadena SE LEE separada: +7 px por pieza — el
                    // sim v6.176 lo midió ×1.6 de separación)
                    base_ += new Vector2(((i - (n - 1) * 0.5f) * 7f), 0f) * desenreda;
                    // EL VAIVÉN INDIVIDUAL: cada pieza SU fase, SU periodo
                    float fase = i * 1.73f;
                    base_ += new Vector2(
                        MathF.Sin(t * (0.19f + 0.013f * (i % 5)) + fase) * 9f,
                        MathF.Cos(t * (0.16f + 0.011f * (i % 3)) + fase * 1.3f) * 7f) * sube;
                    seg.Center = Vector2.Lerp(seg.Center, base_, 0.16f);
                    seg.velocity = Vector2.Zero;
                }
            }

            // ============================================================
            //  FASE 3 — EL TRAGO: TODAS las piezas vuelan a la boca A LA
            //  VEZ y mueren A LA VEZ (la letra: «todas son devoradas al
            //  mismo tiempo»). La muerte de cada pieza es REAL: loot,
            //  banderas — una muerte normal por el gusano entero.
            // ============================================================
            if (_capturada && !_tragadas && t >= T_TRAGO)
            {
                // EL ORIGEN DEL TRAGO: la posición REAL donde quedó cada
                // pieza al final de la flotación (se captura UNA vez — sin
                // esto el cambio de fase SALTA: el sim v6.176 lo cazó)
                if (_posicionesTrago.Count == 0)
                {
                    foreach (int idx in _secciones)
                    {
                        NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                        _posicionesTrago.Add(seg != null && seg.active ? seg.Center : BocaLibro);
                    }
                }
                float prog = MathHelper.Clamp((t - T_TRAGO) / 55f, 0f, 1f);
                float ease = prog * prog * (3f - 2f * prog);   // suave al arrancar, certera al llegar
                int n = Math.Max(1, _secciones.Count);
                for (int i = 0; i < n; i++)
                {
                    NPC seg = _secciones[i] < Main.npc.Length ? Main.npc[_secciones[i]] : null;
                    if (seg == null || !seg.active) continue;
                    // el destino de CADA pieza: la boca del libro (todas
                    // juntas — el mismo progreso para TODAS)
                    Vector2 destino = BocaLibro + new Vector2(
                        MathF.Sin(i * 2.4f) * 14f * (1f - prog),
                        MathF.Cos(i * 1.7f) * 10f * (1f - prog));
                    Vector2 origen = i < _posicionesTrago.Count ? _posicionesTrago[i] : _posiciones[i] + new Vector2(0f, -120f);
                    seg.Center = Vector2.Lerp(origen, destino, ease);
                    seg.scale = MathHelper.Lerp(1f, 0.04f, ease);
                    seg.velocity = Vector2.Zero;
                }
                if (prog >= 1f)
                {
                    _tragadas = true;
                    // LA MUERTE SIMULTÁNEA: todas las piezas, el MISMO tick
                    for (int i = 0; i < n; i++)
                    {
                        NPC seg = _secciones[i] < Main.npc.Length ? Main.npc[_secciones[i]] : null;
                        if (seg == null || !seg.active) continue;
                        SombrasLib.PurgaVisualMuerte(BocaLibro, 300f);
                        seg.life = 0;
                        seg.dontTakeDamage = false;
                        seg.netUpdate = true;
                        seg.checkDead();
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.8f), BocaLibro);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.5f).WithVolumeScale(0.9f), BocaLibro);
                }
            }

            // ============================================================
            //  FASE 4 — EL ASCO: el comentario del grimorio EN LA MENTE
            //  del portador (la letra: «debe ser dicho con la
            //  caracteristica forma del grimorio de hablar en la mente
            //  del jugador no en el chat» — EcoRed.HablarAlPortador,
            //  la máquina de escribir dramática del EcoLib).
            // ============================================================
            if (t >= T_ASCO && !_comentarioDicho && _tragadas)
            {
                _comentarioDicho = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.42f).WithVolumeScale(0.85f), BocaLibro);
                Player portador = Portador ?? Main.player[Projectile.owner];
                if (portador != null && portador.active)
                    Systems.EcoRed.HablarAlPortador(portador, "Mods.AethonMod.Devorador.Asco",
                        new Color(226, 150, 255), rugido: false, escala: 0.68f, prioridad: true);
            }

            // LA DESPEDIDA: el poso se apaga y el cine muere limpio
            if (t >= T_FINAL)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.timeLeft < 90) Projectile.timeLeft = 90;   // el cine manda sobre el reloj
            Projectile.Center = BocaLibro;
        }

        /// <summary>LA CAPTURA: se cosechan TODAS las piezas VIVAS (la
        /// cabeza incluida — TODAS se comen a la vez), se CONGELAN (ni IA
        /// ni proyectiles — la letra de los jefes devorados) y quedan
        /// intangibles e inalcanzables hasta el trago.</summary>
        private void Capturar()
        {
            NPC cabeza = Cabeza;
            if (cabeza == null || !cabeza.active) return;
            _capturada = true;
            CosecharSecciones(cabeza);
            _posiciones.Clear();
            foreach (int idx in _secciones)
            {
                NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                _posiciones.Add(seg != null && seg.active ? seg.Center : cabeza.Center);
            }
            foreach (int idx in _secciones)
            {
                NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                if (seg == null || !seg.active) continue;
                if (seg.TryGetGlobalNPC<FaucesGlobalNPC>(out var g))
                    g.CongeladoPorAspiradora = true;   // ni IA, ni UN proyectil
                seg.velocity = Vector2.Zero;
                seg.damage = 0;
                seg.dontTakeDamage = true;
                seg.chaseable = false;
                seg.netUpdate = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar.WithPitchOffset(-0.5f).WithVolumeScale(0.85f), cabeza.Center);
        }

        /// <summary>LA COSECHA: todas las piezas vivas del gusano — de la
        /// COLA a la CABEZA (solo el ORDEN de la lista; el trago se las
        /// lleva a TODAS a la vez).</summary>
        private void CosecharSecciones(NPC cabeza)
        {
            _secciones.Clear();
            try
            {
                int tipoCuerpo = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerBody : NPCID.EaterofWorldsBody;
                int tipoCola = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerTail : NPCID.EaterofWorldsTail;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.whoAmI == cabeza.whoAmI) continue;
                    if (n.type == tipoCola) _secciones.Add(n.whoAmI);
                }
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.whoAmI == cabeza.whoAmI) continue;
                    if (n.type == tipoCuerpo) _secciones.Add(n.whoAmI);
                }
                _secciones.Add(cabeza.whoAmI);   // la cabeza también: TODAS a la vez
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AspiradoraDevorador", exRegistro); }
        }

        /// <summary>¿Cuántas piezas del gusano viven AHORA? (LA
        /// ESTABILIDAD de la vigilias: la cadena está completa cuando el
        /// número deja de crecer).</summary>
        private int ContarPiezas()
        {
            NPC cabeza = Cabeza;
            if (cabeza == null || !cabeza.active) return 0;
            int tipoCuerpo = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerBody : NPCID.EaterofWorldsBody;
            int tipoCola = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerTail : NPCID.EaterofWorldsTail;
            int c = 1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type == tipoCuerpo || n.type == tipoCola) c++;
            }
            return c;
        }

        /// <summary>EL CINE MUERE LIMPIO: nada queda congelado (la
        /// red de seguridad de FaucesGlobalNPC barre el mundo).
        /// (v6.176: el hook de tML 2026.09 es OnKill — Kill quedó
        /// obsoleto y el oráculo avisa (R9: 0 warnings).)</summary>
        public override void OnKill(int timeLeft)
        {
            try { FaucesGlobalNPC.DescongelarAspiradora(); }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AspiradoraDevorador", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            NPC cabeza = Cabeza;
            if (cabeza == null && !_anclaLista) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player portador = Portador ?? Main.player[Projectile.owner];
            int nSombras = FaucesGlobalNPC.ContarSombras(portador);
            int semilla = 511 + (int)Projectile.ai[0] * 17;
            Vector2 centro = _anclaLista ? BocaLibro : Projectile.Center;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // LA SALA DE CINE: el velo (desde la captura hasta el poso)
                float presencia = _capturada
                    ? MathHelper.Clamp((t - T_ESPERA) / 30f, 0f, 1f) *
                      (t > T_FINAL ? Math.Max(0f, 1f - (t - T_FINAL) / 40f) : 1f)
                    : 0.25f;   // en la vigilias solo un aire de espera
                SombrasLib.Vignette(0.42f * presencia);

                // ============================================================
                //  EL LIBRO GRANDE (el contrato del festín: el sprite del
                //  ítem + su párpado; el ojo ABIERTO en la vigilias, MEDIO
                //  en la flotación, CERRADO desde el asco)
                // ============================================================
                float esc = _capturada
                    ? MathHelper.Lerp(1.4f, 3.3f, MathHelper.Clamp((t - T_ESPERA) / 60f, 0f, 1f))
                    : 1.2f + 0.2f * MathF.Sin(tiempo * 0.7f);
                bool cerrado = t >= T_ASCO;
                float temblor = t >= T_ASCO && t < T_CIERRE
                    ? MathF.Sin(t * 1.7f) * 0.16f : 0f;   // EL ESTREMECIMIENTO del asco
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
                Vector2 posLibro = centro - Main.screenPosition;
                posLibro += new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, MathF.Sin(tiempo * 1.3f) * 7f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Color luz = Lighting.GetColor(centro.ToTileCoordinates());
                    Main.spriteBatch.Draw(tex, posLibro, null, luz * Math.Max(0.35f, presencia), 0.09f * MathF.Sin(tiempo) + temblor,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                    string parpadoTex = cerrado
                        ? "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado"
                        : "AethonMod/Content/Weapons/GrimorioHambriento_Medio";   // ojo medio: vigila y devora
                    var parpado = ModContent.Request<Texture2D>(parpadoTex).Value;
                    Main.spriteBatch.Draw(parpado, posLibro, null, luz * Math.Max(0.35f, presencia), 0.09f * MathF.Sin(tiempo) + temblor,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                }
                finally { Main.spriteBatch.End(); }

                // EL HALO del libro (la boca encendida) — v6.176: EN SU
                // PROPIO LOTE ADITIVO (la cura del crash del client.log:
                // dibujaba con el lote CERRADO tras el End del libro)
                VFXCore.AbrirAdditive();
                try
                {
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, posLibro, null,
                        new Color(150, 90, 230, 255) * (0.30f * presencia), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, new Vector2(3.6f, 3.6f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, posLibro, null,
                        new Color(240, 200, 255, 255) * (0.20f * presencia), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, new Vector2(1.7f, 1.7f), SpriteEffects.None, 0f);
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  FASE 0 — LA VIGILIA: el OJO DEL LIBRO sigue a la bestia
                //  (SombrasLib.Ojo — el ojo vivo que MIRA) y EL ANILLO DE
                //  LA ESPERA: el reloj de la generación — un aro de motas
                //  que se llena en los 5 segundos de la letra.
                // ============================================================
                if (!_capturada && cabeza != null && cabeza.active)
                {
                    Vector2 mira = cabeza.Center - centro;
                    SombrasLib.Ojo(centro + new Vector2(0f, -10f), 9f,
                        mira, 0.9f);
                    int lleno = (int)(MathHelper.Clamp(t / T_ESPERA, 0f, 1f) * 24f);
                    VFXCore.Begin();
                    for (int k = 0; k < lleno; k++)
                    {
                        float ang = k * (MathF.PI * 2f / 24f) - MathF.PI / 2f + tiempo * 0.22f;
                        Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.28f) * 150f;
                        VFXCore.Quad(pos, new Color(190, 120, 240, 255) * 0.55f,
                            new Vector2(9f, 9f));
                    }
                    VFXCore.FlushAdditive();
                }

                // ============================================================
                //  FASE 2/3 — LOS TUBOS DE LA ASPIRADORA: una columna de
                //  sombra por pieza VIVA (tantas como el nivel del
                //  portador mande — la letra del coro), de la boca del
                //  libro a CADA pieza flotante; en el trago, la bruma
                //  CORRE hacia la boca.
                // ============================================================
                if (_capturada && !_tragadas && presencia > 0.05f)
                {
                    int dibujados = 0;
                    foreach (int idx in _secciones)
                    {
                        if (dibujados >= nSombras) break;    // N sombras VIVAS (la letra: las que se puedan)
                        NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                        if (seg == null || !seg.active) continue;
                        dibujados++;
                        Vector2 destino = seg.Center;
                        Vector2[] tubo = SombrasLib.ColumnaViva(centro, destino, tiempo,
                            semilla + idx * 13, 18, 0.16f, gancho: 0.5f);
                        SombrasLib.Masa(tubo, 5f, 9f, 0.55f * presencia, semilla + idx * 13, tiempo, grosorCentro: 26f, lisa: true);
                        SombrasLib.BrumaColumna(tubo, 30f, 0.30f * presencia, tiempo, semilla + idx * 13, 7);
                        SombrasLib.Ojo(destino + new Vector2(0f, -14f), 6f, centro - destino, 0.9f * presencia);
                    }
                }

                // LA CORRIENTE DE LA BOCA: bruma tragándose (en el trago)
                // — coordenadas de MUNDO (SombrasLib resta la pantalla solo)
                if (t >= T_TRAGO && !_tragadas)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 1.6f + k * 0.167f);
                        Vector2 pos = centro + new Vector2(MathF.Sin(k * 2.4f + tiempo) * 30f * (1f - prog),
                            -20f + 50f * prog);
                        SombrasLib.Bruma(pos, 22f + 10f * SombrasLib.Frac(k * 0.71f),
                            0.50f * presencia, tiempo, semilla + k * 9,
                            new Vector2(0f, 34f));
                    }
                }

                // EL POSO FINAL: bruma sobre el botín
                if (t > T_TRAGO + 20f)
                {
                    SombrasLib.Bruma(centro, 120f, 0.55f * presencia, tiempo, semilla,
                        new Vector2(MathF.Sin(tiempo * 0.6f) * 20f, -12f));
                    if (_tragadas) SombrasLib.PurgaVisualMuerte(centro, 380f);
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("AspiradoraDevorador", exRegistro);
            }
            VFXCore.RestaurarLoteVanilla();
            return false;
        }
    }
}
