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
    /// Eco de la Arquera Estelar — el fantasma de la arquera que intentó
    /// derribar a Aethon, rehecha de cero en v6.48.
    ///
    /// LO QUE ERA (v5): flechas VortexBeaterRocket de vanilla y "minas"
    /// que eran ProjectileID.Bullet QUIETOS (ni armado, ni detonación —
    /// una bala tirada al suelo). LO QUE ES: LA ARQUERA DE LA CASA —
    /// · ARTE 100% CÓDIGO: la fantasma ámbar (capas translúcidas, la
    /// falda deshaciéndose), la CORONA DE ESTRELLAS, el ARCO VIVO (un
    /// arco de cápsulas que SE TENSIONA de verdad antes de soltar) y la
    /// ESTELA DE ECOS (EcosLib: los fantasmas de sus posiciones
    /// pasadas la siguen — es un eco, se dibuja como eco).
    /// · LAS FLECHAS ESTELARES: corrección de rumbo real y apuntado con
    ///   LEAD (predice tu rumbo — la arquera no dispara a donde ESTÁS).
    /// · LAS MINAS DE VERDAD: se siembran, se ARMAN con pulso de aviso
    ///   (OndaLib) y DETONAN en un ARCO VOLTAICO (StormLib.ChainBolt +
    ///   el cuerpo del estallido con hitbox honesta).
    /// · LA LLUVIA ESTELAR (&lt;50%): estrellas fugaces con MARCA de suelo
    ///   telegrafiada — el cielo cae, pero AVISA.
    ///
    /// 160.000 PV, se convoca con la Pluma de la Arquera (de día).
    /// </summary>
    public class EchoArcher : ModNPC
    {
        // === EL ESTADO DE LA ARQUERA ===
        private int _tickAtaque = 0;      // la tensión del arco
        private int _tickMina = 0;        // la siembra
        private int _tickLluvia = 0;      // la tormenta (fase 2)
        private int _dirStrafe = 1;       // el lado del esquivón
        private int _tickStrafe = 0;      // cambia de lado cada 180 t
        private EcosLib.Memoria _mem;     // la estela de ecos

        // === LA PALETA ÁMBAR DE LOS PORTADORES ===
        private static readonly Color AmbarFantasma = new(255, 178, 96);
        private static readonly Color AmbarTenue = new(190, 130, 70);
        private static readonly Color OroEstelar = new(255, 214, 130);
        private static readonly Color BlancoEstelar = new(255, 240, 190);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 44;
            NPC.damage = 45;
            NPC.defense = 18;
            NPC.lifeMax = 160_000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.05f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 12f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss1;
        }

        public override void AI()
        {
            if (_mem.Pos == null) _mem = EcosLib.Crear(); // la memoria nace con ella

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            bool furia = (float)NPC.life / NPC.lifeMax < 0.50f;
            if (furia && NPC.localAI[0] < 1f)
            {
                NPC.localAI[0] = 1f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Arquera.Furia"),
                    AmbarFantasma);
                NPC.netUpdate = true;
            }

            // === EL MOVIMIENTO DEL ARQUERA: el esquivón lateral ===
            Vector2 aTarget = target.Center - NPC.Center;
            float dist = aTarget.Length();
            Vector2 hacia = aTarget.SafeNormalize(Vector2.UnitX);
            Vector2 lateral = new Vector2(-hacia.Y, hacia.X) * _dirStrafe;

            _tickStrafe++;
            if (_tickStrafe >= 180) { _tickStrafe = 0; _dirStrafe = -_dirStrafe; }

            Vector2 deseada;
            if (dist < 240f)
            {
                // EL RETROCESO: la arquera NO se deja acorralar (salta hacia
                // atrás y al lado — la geometría del que no quiere melee).
                deseada = (-hacia * 3.2f + lateral * 2.6f) * (furia ? 1.25f : 1f);
            }
            else if (dist > 520f)
            {
                // v6.50 — LA CURVA DEL ARCO (EcosLib.CurvaAproximacion):
                // acercarse desde lejos ya no es línea recta — la
                // aproximación se anticipa con tangente y llega al anillo
                // de disparo (380) respirando con el vaivén inconmensurable.
                deseada = EcosLib.CurvaAproximacion(NPC.Center, target.Center,
                    380f, furia ? 5.2f : 4.2f, Main.GlobalTimeWrappedHourly,
                    NPC.whoAmI * 13);
            }
            else
            {
                // v6.50 — EL ANILLO CON VAIVÉN: la media distancia mantiene
                // la bandera lateral (su esquivón de identidad) pero la
                // curva le pone el vaivén y la corrección de radio — ya no
                // desliza en línea perfecta: ondula.
                deseada = lateral * (furia ? 3.4f : 2.6f) +
                    EcosLib.CurvaAproximacion(NPC.Center, target.Center,
                        380f, 1.1f, Main.GlobalTimeWrappedHourly,
                        NPC.whoAmI * 13);
            }
            NPC.velocity = Vector2.Lerp(NPC.velocity, deseada, 0.08f);

            // === LAS FLECHAS ESTELARES (60 t; 38 en furia) ===
            _tickAtaque++;
            int cadencia = furia ? 38 : 60;
            if (_tickAtaque >= cadencia)
            {
                _tickAtaque = 0;
                SoltarLaFlecha(target);
            }

            // === LA SIEMBRA DE MINAS (180 t; 110 en furia) ===
            _tickMina++;
            int cadenciaMina = furia ? 110 : 180;
            if (_tickMina >= cadenciaMina)
            {
                _tickMina = 0;
                SembrarMinas(target);
            }

            // === LA LLUVIA ESTELAR (la furia: cada 300 t) ===
            if (furia)
            {
                _tickLluvia++;
                if (_tickLluvia >= 300)
                {
                    _tickLluvia = 0;
                    LaLluviaEstelar(target);
                }
            }

            // LA MEMORIA: la estela de ecos (un registro por tick).
            EcosLib.Registrar(ref _mem, NPC.Center, NPC.velocity.ToRotation());

            Lighting.AddLight(NPC.Center, new Vector3(0.5f, 0.4f, 0.1f));
        }

        // ==================================================================
        //  LOS DIENTES DE LA ARQUERA
        // ==================================================================

        /// <summary>LA FLECHA: con LEAD (apunta a dónde ESTARÁS) + tensión.</summary>
        private void SoltarLaFlecha(Player target)
        {
            // EL LEAD: tu rumbo multiplicado por el tiempo de vuelo estimado.
            Vector2 pred = target.Center + target.velocity * 14f;
            Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                NPC.Center + dir * 22f, dir * 14f,
                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                (int)(NPC.damage * 0.78f), 2f, Main.myPlayer,
                AtaqueJefeProjectile.EstiloFlechaEstelar, 0f, NPC.whoAmI * 19);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
        }

        /// <summary>LA SIEMBRA: 3 (5 en furia) minas alrededor de la presa.</summary>
        private void SembrarMinas(Player target)
        {
            int n = (float)NPC.life / NPC.lifeMax < 0.50f ? 5 : 3;
            for (int i = 0; i < n; i++)
            {
                float ang = i * MathHelper.TwoPi / n + Main.rand.NextFloat(-0.4f, 0.4f);
                float r = Main.rand.NextFloat(130f, 260f);
                Vector2 pos = target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * r;
                // La mina CAE desde la arquera (arco corto) y se asienta.
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, (pos - NPC.Center) * 0.03f,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.66f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloMinaEstelar, 0f, NPC.whoAmI * 29 + i);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item65, NPC.Center);
        }

        /// <summary>LA LLUVIA: 8 estrellas fugaces con marca de suelo.</summary>
        private void LaLluviaEstelar(Player target)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = target.Center + new Vector2(
                    (i - 3.5f) * 110f + Main.rand.NextFloat(-40f, 40f), -460f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    pos, new Vector2(0f, 6f),
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.7f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloEstrellaFugaz, 0f, NPC.whoAmI * 37 + i);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, NPC.Center);
            Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Arquera.Lluvia"),
                OroEstelar);
        }

        // ==================================================================
        //  EL ARTE DE LA ARQUERA — 100% CÓDIGO (con ESTELA DE ECOS)
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
                bool furia = (float)NPC.life / NPC.lifeMax < 0.50f;
                Vector2 c = NPC.Center;
                Player presa = Main.player[NPC.target];
                float mira = (presa != null && presa.active)
                    ? (presa.Center - c).ToRotation() : NPC.direction * 0f;

                // LA RESPIRACIÓN FANTASMAL (el cuerpo late — es un eco).
                float aliento = 0.90f + 0.10f * MathF.Sin(t * 2.2f);

                // === FASE 1 — EL CUERPO (búfer de quads, coords de MUNDO) ===

                // LA FALDA DESHECHA (tres franjas que se apagan hacia abajo).
                for (int i = 0; i < 3; i++)
                {
                    float franja = (0.38f - i * 0.11f) * aliento;
                    VFXCore.Quad(c + new Vector2(0f, 12f + i * 9f),
                        AmbarTenue * franja, new Vector2(24f - i * 5f, 14f));
                }

                // EL TORSO (la arquera encogida apuntando).
                VFXCore.Quad(c + new Vector2(0f, -4f), AmbarTenue * (0.62f * aliento),
                    new Vector2(20f, 26f));

                // LA CAPUCHA (la cabeza del eco).
                VFXCore.Quad(c + new Vector2(0f, -19f), AmbarTenue * (0.75f * aliento),
                    new Vector2(16f, 15f));

                // LOS OJOS (los que aún miran a Aethon — dos rendijas claras).
                VFXCore.Quad(c + new Vector2(-4f, -19f), BlancoEstelar * (0.9f * aliento),
                    new Vector2(4f, 2.5f));
                VFXCore.Quad(c + new Vector2(4f, -19f), BlancoEstelar * (0.9f * aliento),
                    new Vector2(4f, 2.5f));

                // EL BRAZO DEL ARCO (extendido hacia la mira).
                Vector2 hombro = c + new Vector2(0f, -8f);
                Vector2 mano = hombro + new Vector2(MathF.Cos(mira), MathF.Sin(mira)) * 20f;
                VFXCore.Line(hombro, mano, AmbarFantasma * (0.65f * aliento), 4f);

                // EL BRAZO DE LA CUERDA (tira hacia atrás — MÁS atrás cuanto
                // más cerca del disparo: la tensión del arco VIVE).
                float tension = _tickAtaque / 60f; // 0..1 del ciclo
                Vector2 manoTirada = hombro - new Vector2(MathF.Cos(mira), MathF.Sin(mira)) *
                    (10f + tension * 12f);
                VFXCore.Line(hombro, manoTirada, AmbarFantasma * (0.55f * aliento), 4f);

                // LA FLECHA NOCKADA (la que está por salir).
                if (tension > 0.35f)
                {
                    VFXCore.Line(manoTirada, mano, OroEstelar * (0.7f * tension), 3f);
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — LOS ADITIVOS (lote de pantalla) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 pos = c - Main.screenPosition;
                Vector2 posMano = mano - Main.screenPosition;

                // === EL ARCO VIVO (el arco de cápsulas: 5 varillas en arco
                //     perpendicular a la mira, TENSIÓN real de la cuerda) ===
                for (int i = -2; i <= 2; i++)
                {
                    float desvio = i * 0.24f;
                    float angVarilla = mira + MathHelper.PiOver2 + desvio;
                    float len = 10f + 6f * (1f - Math.Abs(i) / 2.5f);
                    Vector2 mid = posMano + new Vector2(MathF.Cos(mira), MathF.Sin(mira)) * 0f +
                        new Vector2(MathF.Cos(angVarilla), MathF.Sin(angVarilla)) * 11f;
                    OrbitaLib.Capsule(mid, len, 3.2f, angVarilla,
                        OrbitaLib.Tint(AmbarFantasma, 0.70f * aliento));
                }
                // LA CUERDA (la línea que se tensa — de punta a punta).
                {
                    Vector2 tipA = posMano + new Vector2(MathF.Cos(mira + MathHelper.PiOver2),
                        MathF.Sin(mira + MathHelper.PiOver2)) * 20f;
                    Vector2 tipB = posMano + new Vector2(MathF.Cos(mira - MathHelper.PiOver2),
                        MathF.Sin(mira - MathHelper.PiOver2)) * 20f;
                    Vector2 nock = posMano - new Vector2(MathF.Cos(mira), MathF.Sin(mira)) *
                        (tension * 10f);
                    VFXCore.Line(tipA + Main.screenPosition, nock + Main.screenPosition,
                        BlancoEstelar * 0.5f, 1.5f);
                    VFXCore.Line(nock + Main.screenPosition, tipB + Main.screenPosition,
                        BlancoEstelar * 0.5f, 1.5f);
                    VFXCore.FlushAdditive(null, false);
                }

                // === LA CORONA DE ESTRELLAS (las tres que aún lleva) ===
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 estrella = pos + new Vector2(i * 9f, -31f - (i == 0 ? 4f : 0f));
                    float parpadeo = 0.7f + 0.3f * MathF.Sin(t * 3.1f + i * 2.1f);
                    LumenLib.Bloom(Main.spriteBatch, estrella, 9f * parpadeo,
                        OroEstelar, 0.8f * parpadeo, 2);
                }

                // EL ALIENTO DEL ECO (el pecho brilla tenue).
                LumenLib.Bloom(Main.spriteBatch, pos, 24f, AmbarTenue,
                    (furia ? 0.45f : 0.3f) * aliento);

                // === LA ESTELA DE ECOS (los fantasmas de EcosLib — 4
                //     posiciones pasadas desvaneciéndose detrás) ===
                if (EcosLib.Profundidad(in _mem) > 6)
                {
                    for (int g = 1; g <= 4; g++)
                    {
                        Vector2 pasado = EcosLib.Pasado(in _mem, g * 5);
                        if (pasado == Vector2.Zero) continue;
                        Vector2 eco = pasado - Main.screenPosition;
                        float alfa = 0.30f * (1f - g / 5f);
                        LumenLib.Bloom(Main.spriteBatch, eco + new Vector2(0f, -12f), 22f,
                            AmbarTenue, alfa, 2);
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
            return false; // la arquera SE dibuja a sí misma (cero sprite)
        }

        // ==================================================================
        //  LA MUERTE DEL ECO
        // ==================================================================
        public override void OnKill()
        {
            // v6.49 — SU ALMA: la Esencia de la Arquera Estelar (+1 nivel
            // al Grimorio; 10 por mundo — EsenciasModSistema).
            EsenciasModSistema.SoltarEsencia(NPC);

            // EL ECO SE APAGA: las estrellas de la corona caen.
            for (int d = 0; d < 32; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 28, 44, DustID.YellowStarDust,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-6f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);

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
                sp.ResonanceShards += 110;
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Resonancia",
                    NPC.FullName, 110), new Color(245, 196, 81));
            }
        }
    }
}
