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
    /// ASPIRADORADEVORADORPROJECTILE — v6.175 — LA DEVORACIÓN ESPACIAL DEL
    /// GUSANO (la letra del Devorador): «ese gusano al estar seccionado es
    /// complicado devorarlo, entonces hagamos algo, para ese jefe
    /// exclusivamente desactiva la devoracion normal, en cambio en el
    /// momento en el que sea invocado por segunda vez se hace una
    /// animacion espacial para el donde el grimorio del eterno lo devora
    /// por completo al mismo tiempo, todas sus secciones son separadas y
    /// devoradas como una aspiradora, luego el grimorio dice un
    /// comentario».
    ///
    /// LA PELÍCULA (cada fase con sus sombras — N = ContarSombras, la
    /// letra: «usar tantas como sea posible segun el nivel normal y el
    /// nivel de despertar del jugador… si no tiene suficiente nivel que
    /// usa 1»):
    ///    0-40    LA PRESENCIA — el libro desciende flotando sobre la
    ///            bestia; el velo de la sala cierra; TODO el gusano se
    ///            congela (daño 0, intocable, quieto).
    ///   40-110   LA SEPARACIÓN — cada sección se DESTACA de la cadena
    ///            (el abanico se abre) envuelta en SU columna de sombra.
    ///  110-250   LA ASPIRADORA — de la COLA a la CABEZA, sección por
    ///            sección: cada pieza VUELA a la boca del libro,
    ///            encogiendo (scale→0); al llegar, su muerte REAL (con
    ///            su loot — la letra: «cuenta como una muerte normal»).
    ///  250-310   EL ASCO — el libro SE ESTREMECE, se cierra de golpe y
    ///            SUELTA el comentario del grimorio.
    ///    310     EL POSO — bruma sobre el botín y adiós.
    ///
    /// ai[0] = whoAmI de la CABEZA · ai[1] = whoAmI del portador · ai[2] = tick.
    /// </summary>
    public class AspiradoraDevoradorProjectile : ModProjectile
    {
        private const float T_PRESENCIA = 40f;
        private const float T_SEPARACION = 110f;
        private const float T_ASPIRACION = 250f;
        private const float T_COMENTARIO = 260f;
        private const float T_CIERRE = 300f;
        private const float T_FINAL = 330f;

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
        private int _proximoATragar;     // índice en _secciones del siguiente
        private bool _comentarioDicho;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 400;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC cabeza = Cabeza;
            bool vivoCabeza = cabeza != null && cabeza.active;

            if (!vivoCabeza && t > T_COMENTARIO)
            {
                // el poso: la bruma de la entrega
                if (Projectile.timeLeft > 60) Projectile.timeLeft = 60;
                if (t >= T_FINAL) { Projectile.Kill(); return; }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;   // el cliente ve el cine por su PreDraw

            // EL ANCLA del libro: sobre el CENTRO de la bestia
            if (!_anclaLista && vivoCabeza)
            {
                _anclaLibro = cabeza.Center + new Vector2(0f, -340f);
                _anclaLista = true;
                CosecharSecciones(cabeza);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.8f), _anclaLibro);
            }
            if (!_anclaLista) return;

            // === EL CONGELADO: toda la bestia quieta, muda e intocable ===
            foreach (int idx in _secciones)
            {
                NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                if (seg == null || !seg.active) continue;
                seg.velocity = Vector2.Zero;
                seg.damage = 0;
                seg.dontTakeDamage = true;
                seg.chaseable = false;
                if (t % 10 == 0) seg.netUpdate = true;
            }

            // === FASE 1: LA PRESENCIA (el libro desciende) ===
            if (t < T_SEPARACION) { /* el dibujo respira; la bestia calla */ }

            // === FASE 2: LA SEPARACIÓN — el abanico se abre ===
            if (t >= T_SEPARACION && t < T_ASPIRACION && vivoCabeza)
            {
                float abre = MathHelper.Clamp((t - T_SEPARACION) / 50f, 0f, 1f);
                int n = Math.Max(1, _secciones.Count);
                for (int i = 0; i < n; i++)
                {
                    NPC seg = _secciones[i] < Main.npc.Length ? Main.npc[_secciones[i]] : null;
                    if (seg == null || !seg.active) continue;
                    // el abanico: cada sección se abre en SU ángulo
                    float ang = -MathF.PI / 2f + (i - (n - 1) * 0.5f) * 0.30f;
                    float radio = (30f + 16f * Math.Abs(i - n * 0.5f)) * abre;
                    Vector2 destino = cabeza.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                    destino += new Vector2(MathF.Sin(t * 0.09f + i) * 5f, MathF.Sin(t * 0.13f + i * 1.7f) * 4f);
                    seg.Center = Vector2.Lerp(seg.Center, destino, 0.22f);
                }
                if (t == (int)T_SEPARACION)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar.WithPitchOffset(-0.5f).WithVolumeScale(0.8f), cabeza.Center);
            }

            // === FASE 3: LA ASPIRADORA — sección a sección a la boca ===
            if (t >= T_ASPIRACION && _proximoATragar < _secciones.Count)
            {
                // cada 14 ticks una nueva pieza entra al tubo
                if ((t - T_ASPIRACION) % 14f == 0f && _proximoATragar < _secciones.Count)
                {
                    _proximoATragar++;    // la pieza i ya está siendo tragada (la dibuja y mueve el bucle)
                }
                for (int i = 0; i < _proximoATragar; i++)
                {
                    NPC seg = _secciones[i] < Main.npc.Length ? Main.npc[_secciones[i]] : null;
                    if (seg == null || !seg.active) continue;
                    float prog = MathHelper.Clamp((t - T_ASPIRACION - i * 14f) / 26f, 0f, 1f);
                    seg.Center = Vector2.Lerp(seg.Center, BocaLibro, 0.30f);
                    seg.scale = MathHelper.Lerp(seg.scale, 0.05f, 0.22f);
                    seg.velocity = Vector2.Zero;
                    if (prog >= 1f || Vector2.Distance(seg.Center, BocaLibro) < 24f)
                    {
                        // LA MUERTE REAL de la pieza: loot completo donde cae
                        SombrasLib.PurgaVisualMuerte(BocaLibro, 260f);
                        seg.life = 0;
                        seg.dontTakeDamage = false;
                        seg.netUpdate = true;
                        seg.checkDead();
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.NPCHit9.WithPitchOffset(-0.15f + (i % 5) * 0.08f).WithVolumeScale(0.45f), BocaLibro);
                    }
                }
            }

            // === FASE 4: EL COMENTARIO DEL ASCO ===
            if (t >= T_COMENTARIO && !_comentarioDicho)
            {
                _comentarioDicho = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.42f).WithVolumeScale(0.85f), BocaLibro);
                Systems.EcoRed.AnunciarMundo("Mods.AethonMod.Devorador.Asco",
                    new Color(226, 150, 255));
            }

            // === EL FINAL DE LA CADENA: la cabeza muere al final del todo ===
            if (t >= T_CIERRE && vivoCabeza && _proximoATragar >= _secciones.Count)
            {
                cabeza.life = 0;
                cabeza.dontTakeDamage = false;
                cabeza.netUpdate = true;
                cabeza.checkDead();
            }
            Projectile.Center = BocaLibro;
        }

        /// <summary>LA COSECHA: todas las piezas vivas del gusano (cola
        /// primero — la aspiradora se come la cadena de atrás adelante,
        /// la cabeza es el ÚLTIMO bocado).</summary>
        private void CosecharSecciones(NPC cabeza)
        {
            _secciones.Clear();
            try
            {
                int tipoCuerpo = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerBody : NPCID.EaterofWorldsBody;
                int tipoCola = cabeza.type == NPCID.TheDestroyer ? NPCID.TheDestroyerTail : NPCID.EaterofWorldsTail;
                // COLA → CUERPO → CABEZA (el orden del tragado)
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
                _secciones.Add(cabeza.whoAmI);   // la cabeza SIEMPRE al final
            }
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
                // LA SALA DE CINE: el velo
                float presencia = MathHelper.Clamp(t / 30f, 0f, 1f) *
                    (t > T_FINAL ? Math.Max(0f, 1f - (t - T_FINAL) / 30f) : 1f);
                SombrasLib.Vignette(0.42f * presencia);

                // ============================================================
                //  EL LIBRO GRANDE (el mismo contrato del festín: el sprite
                //  del ítem + su párpado MEDIO; al final se CIERRA)
                // ============================================================
                float esc = MathHelper.Lerp(1.2f, 3.3f, MathHelper.Clamp(t / T_SEPARACION, 0f, 1f));
                bool cerrado = t >= T_COMENTARIO;
                float temblor = t >= T_COMENTARIO && t < T_CIERRE
                    ? MathF.Sin(t * 1.7f) * 0.16f : 0f;   // EL ESTREMECIMIENTO del asco
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
                Vector2 posLibro = centro - Main.screenPosition;
                // el vaivén flotante + el temblor del asco
                posLibro += new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, MathF.Sin(tiempo * 1.3f) * 7f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Color luz = Lighting.GetColor(centro.ToTileCoordinates());
                    Main.spriteBatch.Draw(tex, posLibro, null, luz * presencia, 0.09f * MathF.Sin(tiempo) + temblor,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                    var parpado = ModContent.Request<Texture2D>(
                        cerrado ? "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado"
                                : "AethonMod/Content/Weapons/GrimorioHambriento_Medio").Value;
                    Main.spriteBatch.Draw(parpado, posLibro, null, luz * presencia, 0.09f * MathF.Sin(tiempo) + temblor,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                }
                finally { Main.spriteBatch.End(); }

                // EL HALO del libro (la boca encendida)
                Main.spriteBatch.Draw(VFXCore.SoftGlow, posLibro, null,
                    new Color(150, 90, 230, 255) * (0.30f * presencia), 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(3.6f, 3.6f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, posLibro, null,
                    new Color(240, 200, 255, 255) * (0.20f * presencia), 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(1.7f, 1.7f), SpriteEffects.None, 0f);

                // ============================================================
                //  LOS TUBOS DE LA ASPIRADORA: una columna de sombra por
                //  sección VIVA (de la boca del libro a la pieza) — tantas
                //  sombras como el nivel del portador mande (la letra).
                // ============================================================
                if (t >= T_SEPARACION - 20f && presencia > 0.05f)
                {
                    int dibujados = 0;
                    foreach (int idx in _secciones)
                    {
                        if (dibujados >= nSombras) break;    // N sombras VIVAS (la letra: las que se puedan)
                        NPC seg = idx >= 0 && idx < Main.npc.Length ? Main.npc[idx] : null;
                        if (seg == null || !seg.active) continue;
                        dibujados++;
                        Vector2 destino = seg.Center;
                        // en la aspiración, el tubo SIGUE a la pieza que vuela
                        Vector2[] tubo = SombrasLib.ColumnaViva(centro, destino, tiempo,
                            semilla + idx * 13, 18, 0.16f, gancho: 0.5f);
                        SombrasLib.Masa(tubo, 5f, 9f, 0.55f * presencia, semilla + idx * 13, tiempo, grosorCentro: 26f, lisa: true);
                        SombrasLib.BrumaColumna(tubo, 30f, 0.30f * presencia, tiempo, semilla + idx * 13, 7);
                        // el OJO de la sección (la sombra la mira mientras la tira)
                        SombrasLib.Ojo(destino + new Vector2(0f, -14f), 6f, centro - destino, 0.9f * presencia);
                    }
                }

                // LA CORRIENTE DE LA BOCA: bruma tragándose (tras la aspiración)
                if (t >= T_ASPIRACION)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 1.4f + k * 0.2f);
                        Vector2 pos = posLibro + new Vector2(MathF.Sin(k * 2.4f + tiempo) * 26f * (1f - prog),
                            -18f + 46f * prog);
                        SombrasLib.Bruma(pos, 20f + 10f * SombrasLib.Frac(k * 0.71f),
                            0.45f * presencia, tiempo, semilla + k * 9,
                            new Vector2(0f, 30f));
                    }
                }

                // EL POSO FINAL: bruma sobre el botín
                if (t > T_FINAL)
                {
                    SombrasLib.Bruma(centro, 120f, 0.55f * presencia, tiempo, semilla,
                        new Vector2(MathF.Sin(tiempo * 0.6f) * 20f, -12f));
                    SombrasLib.PurgaVisualMuerte(centro, 380f);
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("AspiradoraDevorador", exRegistro);
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
