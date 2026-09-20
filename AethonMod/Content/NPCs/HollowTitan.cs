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
    /// El Titán Hueco — el coloso de cristal del Sagrario. PRIMER jefe del
    /// mod, rehecho de cero en v6.48.
    ///
    /// LO QUE ERA (v5): un sprite suelto que escupía CrystalBullet de
    /// vanilla. LO QUE ES: EL COLOSO DE LA CASA —
    /// · ARTE 100% CÓDIGO: columnas, pecho, hombros, ojo y espinas de
    ///   cristal dibujadas con los quads de VFXCore (nada de sprite: el
    ///   cuerpo LO construye el render, con el corazón en Bloom y el
    ///   andar mecánico en las columnas).
    /// · LOS DIENTES: PÚAS DEL SAGRARIO (espinas en arco que se clavan,
    ///   respiran y DETONAN en esquirlas), el CORO DE CRISTAL (la furia:
    ///   un anillo de esquirlas orbitando el coloso que se LANZA al
    ///   canto) y el PORRAZO (onda de suelo cuando te acercas).
    /// · LA CARGA TELEGRAFIADA: el coloso alinea el cuerno (Telegrafo),
    ///   espera y ARREMETE — el aviso es esquivable, el golpe no.
    /// · LA FURIA (&lt;50% vida): doble cadencia, coro en modo rápido y
    ///   el aviso de la casa (texto localizado + rugido).
    ///
    /// Sigue siendo el primero: 42.000 PV, daño honesto, el Sagrario
    /// lo convoca con el Cristal del Titán Hueco (de día).
    /// </summary>
    public class HollowTitan : ModNPC
    {
        // === EL ESTADO DEL COLOSO ===
        private int _tickPua = 0;        // cadencia de las púas
        private int _tickCoro = 0;       // cadencia del coro
        private int _tickPorrazo = 0;    // frío del porrazo
        private int _tickCarga = 0;      // LA CARGA (0 = libre)
        private Vector2 _dirCarga = Vector2.Zero;

        // === LA PALETA DEL SAGRARIO ===
        private static readonly Color Cristal = new(168, 232, 255);
        private static readonly Color CristalClaro = new(215, 244, 255);
        private static readonly Color CristalOscuro = new(86, 138, 180);
        private static readonly Color Corazon = new(120, 220, 255);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 60;
            NPC.height = 80;
            NPC.damage = 30;
            NPC.defense = 15;
            NPC.lifeMax = 42_000;
            NPC.HitSound = SoundID.NPCHit41; // cristal
            NPC.DeathSound = SoundID.NPCDeath43;
            NPC.knockBackResist = 0.05f;
            NPC.noGravity = false;
            NPC.boss = true;
            NPC.npcSlots = 5f;
            NPC.aiStyle = 2; // Fighter AI: el coloso CAMINA (el terreno es suyo)
            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            // === LA PRESA (y el despawn limpio de la casa) ===
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    // v5.59: sin presa no hay coloso (active=false = despawn
                    // limpio; life=0 solo NO dispara checkDead — R44).
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            // === LA FURIA (&lt;50%): el coloso redobla ===
            bool furia = (float)NPC.life / NPC.lifeMax < 0.5f;
            if (furia && _tickCoro == 0 && _tickPua == 0 && _tickPorrazo == 0 && _tickCarga == 0 &&
                NPC.localAI[0] < 1f)
            {
                NPC.localAI[0] = 1f; // una sola vez
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Titan.Furia"),
                    Cristal);
                NPC.netUpdate = true;
            }

            // === LA CARGA TELEGRAFIADA: 30 t de aviso y 26 de embestida ===
            if (_tickCarga > 0)
            {
                _tickCarga++;
                if (_tickCarga == 30)
                {
                    // EL ARRANQUE: la arremetida del coloso.
                    _dirCarga = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item67, NPC.Center);
                }
                if (_tickCarga > 30 && _tickCarga <= 56)
                {
                    // LA EMBESTIDA: pisa al fighter AI (la velocidad es MÍA).
                    NPC.velocity.X = _dirCarga.X * (furia ? 13f : 10f);
                    NPC.velocity.Y = _dirCarga.Y * 3f;
                    NPC.direction = _dirCarga.X > 0 ? 1 : -1;
                    NPC.netUpdate = true;
                }
                if (_tickCarga > 70) _tickCarga = 0;
                return; // mientras carga, no escupe (el cuerpo está ocupado)
            }

            // === EL SALTO DEL GUARDIÁN (acercarse por aire si la presa vuela) ===
            if (target.Center.Y < NPC.Center.Y - 90f && NPC.velocity.Y == 0f &&
                _tickPorrazo <= 0 && Math.Abs(target.Center.X - NPC.Center.X) < 300f)
            {
                NPC.velocity.Y = -9f;
            }

            // === LAS PÚAS DEL SAGRARIO (90 t; 45 en furia) ===
            _tickPua++;
            int cadenciaPua = furia ? 45 : 90;
            if (_tickPua >= cadenciaPua)
            {
                _tickPua = 0;
                EscupirPuas(target);
            }

            // === EL CORO DE CRISTAL (240 t; 120 en furia) ===
            _tickCoro++;
            int cadenciaCoro = furia ? 120 : 240;
            if (_tickCoro >= cadenciaCoro)
            {
                _tickCoro = 0;
                CantarElCoro();
            }

            // === EL PORRAZO (la presa se acerca demasiado) ===
            _tickPorrazo--;
            if (_tickPorrazo <= 0 && Vector2.Distance(target.Center, NPC.Center) < 130f)
            {
                _tickPorrazo = 240;
                ElPorrazo();
            }

            // === LA CARGA: cada 420 t (280 en furia) si la presa está lejos ===
            if (_tickPua == 0 && Vector2.Distance(target.Center, NPC.Center) > 260f &&
                Main.GameUpdateCount % (furia ? 280u : 420u) == 0u)
            {
                _tickCarga = 1; // arranca el aviso
            }

            NPC.ai[0]++;
        }

        // ==================================================================
        //  LOS DIENTES DEL COLOSO
        // ==================================================================

        /// <summary>LAS PÚAS: 3 espinas en arco que se clavan y detonan.</summary>
        private void EscupirPuas(Player target)
        {
            Vector2 baseDir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 vel = baseDir.RotatedBy(i * 0.18f) * (9f + Math.Abs(i));
                vel.Y -= 3f; // EL ARCO
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center + new Vector2(0f, -20f), vel,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.85f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloPuaSagrario, 0f, NPC.whoAmI * 31 + i);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27, NPC.Center);
        }

        /// <summary>EL CORO: 6 esquirlas orbitando el coloso (rápido en furia).</summary>
        private void CantarElCoro()
        {
            bool furia = (float)NPC.life / NPC.lifeMax < 0.5f;
            for (int i = 0; i < 6; i++)
            {
                float ang = i * MathHelper.TwoPi / 6f;
                Vector2 pos = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 118f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.75f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloCoroCristal,
                    furia ? -1f : ang, // Par &lt; 0 = coro rápido
                    NPC.whoAmI * 13 + i);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
        }

        /// <summary>EL PORRAZO: onda de suelo + esquirlas radiales + kick.</summary>
        private void ElPorrazo()
        {
            // 5 esquirlas raseras alrededor del golpe.
            for (int i = 0; i < 5; i++)
            {
                float ang = MathHelper.Pi + i * MathHelper.Pi / 4f; // el hemisferio del suelo
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * -0.3f) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center + new Vector2(0f, 30f), vel,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.7f), 3f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloPuaSagrario, 0f, NPC.whoAmI * 7 + i);
            }
            // EL KICK de cámara (el golpe del coloso se SIENTE).
            OndaLib.Kick(7f, 14);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
            for (int d = 0; d < 14; d++)
            {
                int idx = Dust.NewDust(NPC.Center + new Vector2(-30f, 26f), 60, 10,
                    DustID.IceTorch, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-3f, 0f));
                Main.dust[idx].noGravity = true;
            }
        }

        // ==================================================================
        //  EL ARTE DEL COLOSO — 100% CÓDIGO (quads de VFXCore + aditivos)
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                bool furia = (float)NPC.life / NPC.lifeMax < 0.5f;
                float paso = MathF.Abs(MathF.Sin(t * (furia ? 8f : 5f))) * MathHelper.Clamp(Math.Abs(NPC.velocity.X) / 4f, 0f, 1f);
                float frente = NPC.direction;
                Vector2 c = NPC.Center;

                // === FASE 1 — EL CUERPO (búfer de quads, coords de MUNDO) ===

                // LAS COLUMNAS (las piernas del coloso: andan con el paso).
                VFXCore.Quad(c + new Vector2(-13f, 22f - paso * 4f), CristalOscuro * 0.9f,
                    new Vector2(13f, 32f));
                VFXCore.Quad(c + new Vector2(13f, 22f + paso * 4f), CristalOscuro * 0.9f,
                    new Vector2(13f, 32f));
                // LAS RODILLAS (quiebre del paso).
                VFXCore.Quad(c + new Vector2(-13f, 8f - paso * 4f), Cristal * 0.8f,
                    new Vector2(15f, 10f));
                VFXCore.Quad(c + new Vector2(13f, 8f + paso * 4f), Cristal * 0.8f,
                    new Vector2(15f, 10f));

                // EL TORSO (el bloque del pecho).
                VFXCore.Quad(c + new Vector2(0f, -12f), Cristal * 0.95f, new Vector2(46f, 42f));
                VFXCore.Quad(c + new Vector2(0f, -14f), CristalClaro * 0.5f, new Vector2(34f, 30f));

                // LOS HOMBROS (dos bloques girados 45°).
                VFXCore.Quad(c + new Vector2(-27f, -26f), Cristal * 0.9f, new Vector2(18f, 18f), MathHelper.PiOver4);
                VFXCore.Quad(c + new Vector2(27f, -26f), Cristal * 0.9f, new Vector2(18f, 18f), MathHelper.PiOver4);

                // LOS BRAZOS (colgando, el pico del escudo del pecho).
                VFXCore.Quad(c + new Vector2(-31f, -6f), CristalOscuro * 0.85f, new Vector2(9f, 26f));
                VFXCore.Quad(c + new Vector2(31f, -6f), CristalOscuro * 0.85f, new Vector2(9f, 26f));

                // LA CABEZA (el yelmo sin cara).
                VFXCore.Quad(c + new Vector2(0f, -38f), Cristal * 0.95f, new Vector2(20f, 17f));
                VFXCore.Quad(c + new Vector2(0f, -43f), CristalClaro * 0.6f, new Vector2(10f, 8f));

                // EL OJO (la rendija que te mira).
                VFXCore.Quad(c + new Vector2(frente * 5f, -38f), Corazon * 0.95f, new Vector2(11f, 4f));

                // LAS ESPINAS DORSALES (la cresta del coloso).
                for (int i = 0; i < 3; i++)
                {
                    float h = 14f + 5f * (i == 1 ? 1f : 0f);
                    VFXCore.Quad(c + new Vector2(-frente * (14f + i * 12f), -24f - i * 6f),
                        CristalClaro * 0.75f, new Vector2(7f, h),
                        -frente * (0.45f + i * 0.12f));
                }

                // EL CORAZÓN (el núcleo del pecho: late).
                float latido = 0.85f + 0.15f * MathF.Sin(t * (furia ? 6f : 3f));
                VFXCore.Quad(c + new Vector2(0f, -14f), Corazon * (0.85f * latido),
                    new Vector2(14f, 14f) * latido);
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — LOS ADITIVOS (lote de pantalla) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 pos = c - Main.screenPosition;

                // EL CORAZÓN EN BLOOM (el Sagrario vive aquí).
                LumenLib.BloomPulse(Main.spriteBatch,
                    pos + new Vector2(0f, -14f - 0f), 26f, Corazon,
                    (furia ? 0.9f : 0.65f) * latido, t, furia ? 4f : 2f);

                // EL OJO en bloom (la rendija brilla).
                LumenLib.Bloom(Main.spriteBatch,
                    pos + new Vector2(frente * 5f, -38f), 12f, Corazon, 0.7f);

                // EL ANILLO DEL GUARDIÁN (la corona del altar).
                OrbitaLib.AnilloFino(pos, 58f, t * 0.5f,
                    OrbitaLib.Tint(Cristal, furia ? 0.30f : 0.18f));

                // === LA CARGA TELEGRAFIADA: el aviso del cuerno ===
                if (_tickCarga > 0 && _tickCarga <= 30)
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active)
                    {
                        Vector2 fin = presa.Center - Main.screenPosition;
                        OndaLib.Telegrafo(Main.spriteBatch, pos, fin,
                            _tickCarga / 30f, Corazon, 4f);
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
            finally
            {
                if (wasActive)
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false; // el cuerpo LO dibuja el coloso (cero sprite)
        }

        // ==================================================================
        //  LA MUERTE DEL COLOSO
        // ==================================================================
        public override void OnKill()
        {
            // v6.49 — SU ALMA: la Esencia del Titán Hueco (+1 nivel al
            // Grimorio; 10 por mundo, ni una más — EsenciasModSistema).
            EsenciasModSistema.SoltarEsencia(NPC);

            // LA LLUVIA DE CRISTAL (el coloso se deshace en su material).
            for (int d = 0; d < 40; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 60, 60, DustID.IceTorch,
                    Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-9f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath43, NPC.Center);

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
                sp.ResonanceShards += 8;
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Resonancia",
                    NPC.FullName, 8), new Color(245, 196, 81));
            }
        }
    }
}
