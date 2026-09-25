using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Guardián del Rift — el centinela entre mundos, rehecho de cero
    /// en v6.48.
    ///
    /// LO QUE ERA (v5): teleports secos con polvo morado y tres
    /// DeathLaser de vanilla. LO QUE ES: EL CENTINELA DE LA CASA —
    /// · ARTE 100% CÓDIGO: la figura encapuchada de luz tenue (capas de
    ///   cápsulas), la rendija de ojos turquesa, el halo del sello y las
    ///   LLAVES orbitando (los fragmentos del umbral).
    /// · EL TELETRANSPORTE DE VERDAD: NO es un pop — SE DESGARRA: abre
    ///   un desgarro vertical donde estaba (RiftLib.TearVacio), el
    ///   cuerpo se DISUELVE en la grieta, nace al otro lado y la grieta
    ///   SE CIERRA detrás. Con estela de ecos mientras cruza.
    /// · LOS VIROTES DE VACÍO: lanzas que PARPAJEAN entre fases (medio
    ///   dentro del desgarro) con abanico de 3 (5 en el sello).
    /// · EL SELLO (&lt;30% vida): la arena se CIERRA — cuatro PAREDES DE
    ///   DESGARRO (CorteRealidad) avanzando desde los puntos cardinales,
    ///   el teleport redoblado y el anuncio de la casa.
    ///
    /// 95.000 PV, se convoca con el Sello del Rift (de día).
    /// </summary>
    public class RiftKeeper : ModNPC
    {
        // === EL ESTADO DEL CENTINELA ===
        private int _tickTele = 0;        // el ciclo del desgarro
        private int _tickAtaque = 0;      // la cadencia de los virotes
        private int _tickSello = 0;       // el frío del sello
        private float _anguloOrbita = 0f; // el rumbo de la guardia
        // v6.50.2 — contador de cruces (la semilla determinista del teleport)
        private int _cruces = 0;
        private Vector2 _posSalida = Vector2.Zero; // dónde se desgarró

        // 0 = sólido · 1 = disolviéndose en la grieta · 2 = naciendo al otro lado
        private int _faseCruce = 0;

        // === LA PALETA DEL ENTRE-MUNDOS ===
        private static readonly Color TealUmbral = new(96, 224, 220);
        private static readonly Color VioletaVacio = new(140, 60, 220);
        private static readonly Color VeloTenue = new(70, 130, 150);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 50;
            NPC.damage = 55;
            NPC.defense = 25;
            NPC.lifeMax = 95_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss3;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void AI()
        {
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    // v6.44 (R44): active=false = el despawn limpio de la casa.
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            bool sello = (float)NPC.life / NPC.lifeMax < 0.30f;

            // === EL ANUNCIO DEL SELLO (una vez) ===
            if (sello && NPC.localAI[0] < 1f)
            {
                NPC.localAI[0] = 1f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                // v6.50.3 — FIX (anuncio invisible en MP): la IA corre en el
                // server — Main.NewText no llega a ninguna pantalla de remotos.
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Rift.Sello", TealUmbral);
                NPC.netUpdate = true;
            }

            // === EL CICLO DEL DESGARRO (el teleport de verdad) ===
            int cadenciaTele = sello ? 160 : 240;
            _tickTele++;
            if (_faseCruce == 0 && _tickTele >= cadenciaTele)
            {
                // SE DESGARRA: la grieta se abre donde está.
                _faseCruce = 1;
                _tickTele = 0;
                _posSalida = NPC.Center;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                OndaLib.Kick(4f, 8);
            }
            else if (_faseCruce == 1 && _tickTele >= 26)
            {
                // EL CRUCE: nace al otro lado (el ángulo opuesto de la
                // guardia — el centinela SIEMPRE te flanquea).
                // v6.50.2 — FIX (rubber-band en cada cruce MP): el ángulo
                // usaba Main.rand, que corre en TODAS las máquinas con
                // semillas DISTINTAS (NPC.AI corre en server Y clientes) →
                // el cliente teleportaba a OTRO punto que el server y el
                // netUpdate lo corregía con snap. Ángulo DETERMINISTA
                // (semilla por whoAmI + número de cruces — el mismo en
                // todas las pantallas) + clamp a los bordes del mundo
                // (vanilla clampa sus teleports: el centinela no nace en
                // el vacío si la presa está junto al borde).
                _faseCruce = 2;
                _tickTele = 0;
                _anguloOrbita += MathHelper.Pi + (((NPC.whoAmI * 97 + _cruces * 13) % 121) - 60) * 0.01f;
                _cruces++;
                Vector2 posNueva = target.Center + new Vector2(
                    MathF.Cos(_anguloOrbita), MathF.Sin(_anguloOrbita)) * 340f;
                posNueva.X = MathHelper.Clamp(posNueva.X,
                    Main.leftWorld + 256f, Main.rightWorld - 256f);
                posNueva.Y = MathHelper.Clamp(posNueva.Y,
                    Main.topWorld + 256f, Main.bottomWorld - 256f);
                NPC.Center = posNueva;
                NPC.velocity = Vector2.Zero;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                NPC.netUpdate = true;
            }
            else if (_faseCruce == 2 && _tickTele >= 22)
            {
                _faseCruce = 0; // sólido otra vez
                _tickTele = 0;
            }

            // === LA GUARDIA: orbita a la presa (solo sólido o naciendo) ===
            if (_faseCruce != 1)
            {
                // v6.50 — LA CURVA DE LA CASA (EspectroLib.CurvaAproximacion):
                // la órbita a 340 px ahora respira con frecuencias
                // INCONMENSURABLES (el strafe jamás sincroniza — no es
                // un péndulo de relojería) y la aproximación se anticipa
                // con un toque de tangente. El radio y el temple del
                // guardián quedan intactos; la matemática, de la librería.
                Vector2 deseada = EspectroLib.CurvaAproximacion(NPC.Center, target.Center,
                    340f, sello ? 6.0f : 4.2f, Main.GlobalTimeWrappedHourly,
                    NPC.whoAmI * 137);
                NPC.velocity = Vector2.Lerp(NPC.velocity, deseada, 0.10f);
            }
            else
            {
                // DISOLVIÉNDOSE: la grieta lo chupa (se hunde hacia el tear).
                NPC.velocity = Vector2.Zero;
            }

            // === LOS VIROTES DE VACÍO (75 t; 48 con el sello) ===
            if (_faseCruce == 0)
            {
                _tickAtaque++;
                int cadencia = sello ? 48 : 75;
                if (_tickAtaque >= cadencia)
                {
                    _tickAtaque = 0;
                    LanzarVirotes(target, sello);
                }
            }

            // === EL SELLO: las cuatro paredes (cada 360 t mientras dure) ===
            if (sello)
            {
                _tickSello++;
                if (_tickSello >= 360)
                {
                    _tickSello = 0;
                    SellarLaArena(target);
                }
            }
        }

        // ==================================================================
        //  LOS DIENTES DEL CENTINELA
        // ==================================================================

        /// <summary>LOS VIROTES: abanico de 3 (5 con el sello), con lead.</summary>
        private void LanzarVirotes(Player target, bool sello)
        {
            // v6.50.1 — FIX (PROYECTILES ×N+1): la IA de NPC corre en server
            // Y en todos los clientes — sin gate, cada máquina spawneaba SU
            // copia (NewProjectile auto-difunde con SendData(27) cuando el
            // owner es Main.myPlayer) y la pared quedaba multiplicada por
            // jugador. Vanilla cerca TODA spawn de IA con netMode != 1.
            // El sonido sigue sonando en todas las pantallas.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // EL LEAD: apunta a dónde ESTARÁS (el centinela conoce tu rumbo).
                Vector2 pred = target.Center + target.velocity * 18f;
                Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitX);
                int n = sello ? 5 : 3;
                for (int i = -(n / 2); i <= n / 2; i++)
                {
                    Vector2 vel = dir.RotatedBy(i * 0.14f) * 10.5f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.72f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloViroteVacio, 0f, NPC.whoAmI * 17 + i);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
        }

        /// <summary>
        /// EL SELLO: cuatro PAREDES DE DESGARRO avanzando desde los
        /// cardinales — la arena se cierra alrededor de la presa.
        /// </summary>
        private void SellarLaArena(Player target)
        {
            // v6.50.1 — FIX (PROYECTILES ×N+1): solo la AUTORIDAD siembra las
            // paredes (ver LanzarVirotes); el anuncio y el sonido son de todos.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float cardinal = i * MathHelper.PiOver2;
                    // Cada pared nace a 480 px del centro de la presa y AVANZA
                    // hacia ella (obliga a moverse: el sello estrecha).
                    Vector2 pos = target.Center + new Vector2(
                        MathF.Cos(cardinal), MathF.Sin(cardinal)) * 480f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.6f), 3f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloCorteRealidad,
                        cardinal + MathHelper.Pi, NPC.whoAmI * 23 + i);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
            // v6.50.3 — FIX (anuncio invisible en MP): vía EcoRed al mundo.
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Rift.Paredes", VioletaVacio);
        }

        // ==================================================================
        //  EL ARTE DEL CENTINELA — 100% CÓDIGO
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                bool sello = (float)NPC.life / NPC.lifeMax < 0.30f;
                Vector2 c = NPC.Center;

                // LA DISOLUCIÓN: el cuerpo se apaga mientras la grieta lo chupa.
                float alphaCuerpo = _faseCruce == 0 ? 1f :
                    (_faseCruce == 1 ? MathHelper.Clamp(1f - _tickTele / 26f, 0f, 1f)
                                    : MathHelper.Clamp(_tickTele / 22f, 0f, 1f));

                // === FASE 1 — EL CUERPO (búfer de quads, coords de MUNDO) ===

                // EL VELO: la capa del centinela — cápsulas apiladas.
                VFXCore.Quad(c + new Vector2(0f, 8f), VeloTenue * (0.45f * alphaCuerpo),
                    new Vector2(30f, 30f));
                VFXCore.Quad(c + new Vector2(0f, -4f), VeloTenue * (0.60f * alphaCuerpo),
                    new Vector2(26f, 34f));

                // LA CAPA EXTERIOR (la falda que se deshace abajo).
                for (int i = 0; i < 3; i++)
                {
                    float franja = 0.30f * (1f - i * 0.30f);
                    VFXCore.Quad(c + new Vector2(0f, 14f + i * 10f),
                        TealUmbral * (franja * alphaCuerpo), new Vector2(28f - i * 6f, 14f));
                }

                // LA CAPUCHA (el hueco de la cabeza).
                VFXCore.Quad(c + new Vector2(0f, -22f), VeloTenue * (0.80f * alphaCuerpo),
                    new Vector2(22f, 20f));
                VFXCore.Quad(c + new Vector2(0f, -27f), VeloTenue * (0.65f * alphaCuerpo),
                    new Vector2(14f, 12f));

                // LA RENDIJA DE OJOS (tres ojos del umbral — el del centro más largo).
                float parpadeo = 0.85f + 0.15f * MathF.Sin(t * 2.6f);
                VFXCore.Quad(c + new Vector2(0f, -24f), TealUmbral * (0.95f * alphaCuerpo * parpadeo),
                    new Vector2(12f, 3.5f));
                VFXCore.Quad(c + new Vector2(-7f, -25f), TealUmbral * (0.80f * alphaCuerpo * parpadeo),
                    new Vector2(5f, 3f));
                VFXCore.Quad(c + new Vector2(7f, -25f), TealUmbral * (0.80f * alphaCuerpo * parpadeo),
                    new Vector2(5f, 3f));

                // LOS HOMBROS DEL SELLO (los picos de la armadura del umbral).
                VFXCore.Quad(c + new Vector2(-17f, -14f), TealUmbral * (0.55f * alphaCuerpo),
                    new Vector2(10f, 18f), -0.5f);
                VFXCore.Quad(c + new Vector2(17f, -14f), TealUmbral * (0.55f * alphaCuerpo),
                    new Vector2(10f, 18f), 0.5f);

                // LAS LLAVES ORBITANDO (los fragmentos del sello — 4 picos).
                for (int i = 0; i < 4; i++)
                {
                    float ang = t * 1.1f + i * MathHelper.PiOver2;
                    Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 52f;
                    VFXCore.Quad(c + off + new Vector2(0f, -8f),
                        TealUmbral * (0.70f * alphaCuerpo), new Vector2(7f, 14f), ang);
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — LOS ADITIVOS (lote de pantalla) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 pos = c - Main.screenPosition;

                // EL HALO DEL SELLO (la corona giratoria del guardián).
                OrbitaLib.AnilloFino(pos + new Vector2(0f, -40f), 26f, t * 0.9f,
                    OrbitaLib.Tint(TealUmbral, 0.40f * alphaCuerpo));
                if (sello)
                    OrbitaLib.AnilloFino(pos + new Vector2(0f, -40f), 34f, -t * 1.4f,
                        OrbitaLib.Tint(VioletaVacio, 0.35f * alphaCuerpo));

                // EL CORAZÓN DEL UMBRAL (el pecho respira).
                LumenLib.BloomPulse(Main.spriteBatch, pos, 22f, TealUmbral,
                    (sello ? 0.75f : 0.5f) * alphaCuerpo, t, 2.4f);

                // === LA GRIETA DE SALIDA (donde se desgarró) ===
                if (_faseCruce == 1)
                {
                    Vector2 grieta = _posSalida - Main.screenPosition;
                    float abre = MathHelper.Clamp(_tickTele / 26f, 0f, 1f);
                    RiftLib.TearVacio(Main.spriteBatch, grieta, -Vector2.UnitY, 130f,
                        abre, 40f, NPC.whoAmI * 7, t);
                    // EL SIFÓN: chispas cayendo a la grieta.
                    for (int i = 0; i < 3; i++)
                    {
                        float h = VFXCore.Hash01(NPC.whoAmI, i, (int)(_edadGlobal / 12f));
                        Vector2 chispa = grieta + new Vector2(
                            (h - 0.5f) * 90f, -30f - h * 60f + _tickTele * 1.5f);
                        LumenLib.Bloom(Main.spriteBatch, chispa, 7f, TealUmbral, 0.5f, 2);
                    }
                }
                // === LA GRIETA DE LLEGADA (cerrándose tras el cruce) ===
                else if (_faseCruce == 2)
                {
                    float cierra = 1f - MathHelper.Clamp(_tickTele / 22f, 0f, 1f);
                    RiftLib.TearVacio(Main.spriteBatch, pos, -Vector2.UnitY, 130f,
                        cierra, 40f, NPC.whoAmI * 7 + 1, t);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
                // llegó cerrado por un mod ajeno, se cura — el restore condicional
                // devolvía el veneno y tML mataba al proyectil: active=false).
                VFXCore.ReabrirLoteVanilla();
            }
            return false; // el centinela LO dibuja la grieta (cero sprite)
        }

        /// <summary>La edad global determinista para las chispas del render.</summary>
        private static float _edadGlobal => Main.GlobalTimeWrappedHourly * 60f % 997f;

        // ==================================================================
        //  LA MUERTE DEL CENTINELA
        // ==================================================================
        public override void OnKill()
        {
            // v6.49 — SU ALMA: la Esencia del Guardián del Rift (+1 nivel
            // al Grimorio; 10 por mundo — EsenciasModSistema).
            EsenciasModSistema.SoltarEsencia(NPC);

            // EL COLAPSO DEL UMBRAL: el desgarro final se lo traga.
            for (int d = 0; d < 36; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 40, 50, DustID.PurpleTorch,
                    Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-8f, 2f));
                Main.dust[idx].noGravity = true;
            }
            OndaLib.Kick(9f, 18);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6, NPC.Center);

            // Otorgar resonancia al jugador que mató al NPC (no a LocalPlayer — bug en MP).
            int killerWho = NPC.lastInteraction;
            if (killerWho < 0 || killerWho >= Main.player.Length)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && NPC.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho < 0 || killerWho >= Main.player.Length) return;
            Player player = Main.player[killerWho];
            if (player == null || !player.active) return;

            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 45;
                // v6.50.1 — FIX (ONKILL INVISIBLE EN MP): OnKill solo corre
                // en server/SP — Main.NewText no llegaba a ninguna pantalla.
                // El aviso viaja al portador que mató (EcoRed + merge del
                // shard en MsgCronica para que el .plr del cliente lo guarde).
                EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                    new Color(245, 196, 81), NPC.FullName, 45);
                // v6.50.1 — entrega INMEDIATA (sin esperar la red de 10 s):
                // MsgCronica lleva el total con merge máximo al portador.
                EcoRed.SincronizarCronica(player);
            }
        }
    }
}
