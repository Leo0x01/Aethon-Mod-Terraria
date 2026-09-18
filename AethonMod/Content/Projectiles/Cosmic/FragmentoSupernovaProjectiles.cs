using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// FragmentoSupernovaMinion — v6.41 — EL FRAGMENTO DE SUPERNOVA.
    ///
    /// RÉPLICA (arma de pruebas — la petición del usuario: "es un boss,
    /// pero cópialo como proyectil o minion y esto es solo para pruebas")
    /// de la SINGULARIDAD ALADA: el artefacto-divinidad "ojo alado" cuyo
    /// look, afterimages y ataques icónicos se calcaron de la fuente
    /// original (investigación v6.41 en research/supernova_v641/ — el
    /// informe con las medidas exactas de sprite, paleta y cerebro).
    ///
    /// EL LOOK (las capas del original, sobre primitivas de la casa):
    ///   · EL OJO ALADO: cuerpo crema-oro (110×40) + DOS ALAS naranjas
    ///     inclinadas con puntas rojo oscuro + LA PUPILA oscura (pase
    ///     alpha — el vacío del ojo) — se INCLINA con la velocidad
    ///     (banking ×0.03, el número del original).
    ///   · LAS AFTERIMAGES ORBITALES — LA FIRMA: 4 clones crema (alfa 50)
    ///     orbitando a 8px + 3 clones naranjas (alfa 77) a 16px, girando
    ///     y RESPIRANDO con el pulso triangular de 4 segundos (el bucle
    ///     EXACTO del original, trasladado a EcosLib).
    ///   · EL HALO: anillo dorado + glow Goldenrod PULSANDO con periodo
    ///     1.4 s (el número exacto) + luz naranja ×1.25 (exacta).
    ///
    /// EL CEREBRO (el temperamento del original, domado a minion):
    ///   · HOVER suave y pesado al hombro (Lerp 0.1, amortiguación Y) —
    ///     la gravedad de singularidad, no un minion nervioso.
    ///   · CICLO DE 180 ticks alternando sus DOS ataques icónicos:
    ///     – LA VOLEA: 5 ráfagas nova en abanico ±1 rad (vel 6,
    ///       acelerando ×1.01 — los números del original).
    ///     – EL BEAM: 45 ticks de TELEGRAPH (la línea que se afila, el
    ///       anillo que se contrae) y 15 ticks de rayo de 900px con el
    ///       núcleo blanco + halo dorado→naranja del original.
    ///   · Al morir una ráfaga: LA FLOR DE FUEGO — el estallido radial de
    ///     seis lengüetas (PyraLib.Estallido, nacido de esta réplica).
    ///
    /// CONVENCIONES DE LA CASA: daño manual solo en autoridad
    /// (SimpleStrikeNPC + EsObjetivo), cero Main.rand en el render (el
    /// pulso y las órbitas son puro reloj), lote cerrado→cerrado.
    /// </summary>
    public class FragmentoSupernovaMinion : ModProjectile
    {
        // === EL CICLO (180 ticks: media vuelta de volea, media de beam). ===
        private const int Ciclo = 180;
        private const int TickVolea = 40;          // el disparo de la volea
        private const int BeamTelegraph = 45;      // el aviso (≤45: legible)
        private const float BeamLargo = 900f;      // px (el rayo del original)
        private const int BeamTicks = 15;          // la ráfaga del beam
        private const float VelHover = 9f;

        // === LA PALETA (las medidas EXACTAS del sprite original). ===
        private static readonly Color CuerpoOro = new(255, 220, 150);    // crema-oro
        private static readonly Color AlaNaranja = new(240, 120, 72);    // las alas
        private static readonly Color PuntaRoja = new(168, 48, 48);      // las puntas
        private static readonly Color EcoCrema = new(255, 233, 197, 50); // anillo interior
        private static readonly Color EcoNaranja = new(244, 142, 72, 77);// anillo exterior

        private int _edad;
        private NPC _presaFija;
        private Vector2 _dirBeam = Vector2.UnitX;
        private readonly Dictionary<int, int> _golpesBeam = new();
        private readonly Vector2[] _ecos = new Vector2[7];

        /// <summary>Semilla determinista por identidad (la regla MP de la casa).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 137);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;

        public override bool MinionContactDamage() => true;

        public override void AI()
        {
            _edad++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead)
            {
                Projectile.Kill();
                return;
            }

            CheckMinionBuff(duenio);

            // === LA PRESA (el objetivo manual del jugador si lo fijó). ===
            NPC presa = null;
            if (duenio.HasMinionAttackTargetNPC)
            {
                NPC candidato = Main.npc[duenio.MinionAttackTargetNPC];
                if (candidato != null && candidato.active && candidato.CanBeChasedBy())
                    presa = candidato;
            }
            if (presa == null && _edad % 12 == 0)
            {
                NPC mejor = null; float mejorD = float.MaxValue;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    float d = Vector2.DistanceSquared(npc.Center, duenio.Center);
                    if (d < 700f * 700f && d < mejorD) { mejorD = d; mejor = npc; }
                }
                _presaFija = mejor;
            }
            if (presa == null) presa = _presaFija;
            if (presa != null && (!presa.active || !presa.CanBeChasedBy()))
            {
                presa = null;
                _presaFija = null;
            }

            // === EL HOVER (la gravedad de singularidad: suave y pesado). ===
            int lado = duenio.direction > 0 ? 1 : -1;
            Vector2 ancla = duenio.Center + new Vector2(56f * lado, -64f);
            Vector2 dir = ancla - Projectile.Center;
            float dist = dir.Length();
            if (dist > 0.5f)
                dir /= dist;

            float deseada = Math.Min(dist * 0.12f, VelHover);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * deseada, 0.1f);
            Projectile.velocity.Y *= 0.94f;      // la amortiguación del original
            Projectile.rotation = Projectile.velocity.X * 0.03f;   // banking exacto

            // === EL CICLO DE ATAQUES (180 ticks, alternando). ===
            int fase = _edad % Ciclo;
            bool mitadBeam = (_edad / Ciclo) % 2 == 1;

            if (!mitadBeam && fase == TickVolea && presa != null)
            {
                // === LA VOLEA: 5 ráfagas en abanico ±1 rad, paso 0.5. ===
                Vector2 baseDir = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 vel = baseDir.RotatedBy(i * 0.5f) * 6f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel, ModContent.ProjectileType<RafagaNovaProjectile>(),
                        Projectile.damage, 2f, Projectile.owner);
                }
                SoundEngine.PlaySound(SoundID.Item12 with { Pitch = 0.35f }, Projectile.Center);
            }

            if (mitadBeam)
            {
                if (fase >= Ciclo / 2 && fase < Ciclo / 2 + BeamTelegraph && presa != null)
                {
                    // === EL TELEGRAPH: el rumbo del rayo persigue la presa
                    //     hasta el ÚLTIMO tick del aviso — luego el rayo
                    //     cae donde prometió. ===
                    _dirBeam = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                }

                int tBeam = fase - (Ciclo / 2 + BeamTelegraph);
                if (tBeam >= 0 && tBeam < BeamTicks)
                {
                    // === EL RAYO: 15 ticks de haz de 900px. ===
                    if (tBeam == 0)
                        SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.5f, Pitch = -0.3f },
                            Projectile.Center);

                    // El daño del rayo (solo autoridad — la escuela A).
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 a = Projectile.Center;
                        Vector2 b = a + _dirBeam * BeamLargo;
                        float puntoColision = 0f;
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (!VFXCore.EsObjetivo(npc)) continue;
                            if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size, a, b, 40f, ref puntoColision))
                                continue;

                            int key = npc.whoAmI;
                            _golpesBeam.TryGetValue(key, out int ultimo);
                            if (_edad - ultimo < 30) continue;
                            _golpesBeam[key] = _edad;

                            npc.SimpleStrikeNPC((int)(Projectile.damage * 2.5f),
                                npc.direction, false, 4f, DamageClass.Summon);
                        }
                    }
                }
            }

            // === LA LUZ (la del original: naranja ×1.25). ===
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 1.25f);
        }

        /// <summary>El patrón de la casa (la cría estelar): el minion sostiene su propio buff.</summary>
        private void CheckMinionBuff(Player duenio)
        {
            int tipoBuff = ModContent.BuffType<FragmentoSupernovaBuff>();
            bool tieneBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (duenio.buffType[i] == tipoBuff && duenio.buffTime[i] > 0)
                {
                    tieneBuff = true;
                    break;
                }
            }
            if (!tieneBuff) Projectile.Kill();
            else
            {
                duenio.AddBuff(tipoBuff, 18000);
                Projectile.timeLeft = 2;
            }
        }

        // ==================================================================
        //  EL RENDER — el ojo alado + las afterimages orbitales
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DibujarFragmento();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        private void DibujarFragmento()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            Vector2 centro = Projectile.Center;
            float rot = Projectile.rotation;

            // === EL PULSO TRIANGULAR DE 4s (el reloj de los espejos). ===
            float pulso = EcosLib.PulsoTriangular(tiempo + Seed * 0.13f);
            float tempo = EcosLib.TempoEspejos(tiempo, _edad);
            LlenarEcos(centro, tempo, pulso);

            // --- PASO 1 (aditivo): los ecos + el cuerpo + el halo. ---

            // (el orden importa: la PUPILA se dibuja AL FINAL en pase alpha —
            //  el vacío del ojo debe OSCURECER el brillo del cuerpo propio,
            //  no quedar enterrado debajo: la lección del mock v6.41.)
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // LAS AFTERIMAGES ORBITALES (LA FIRMA — el bucle del original:
            // 4 clones crema a 8px + 3 clones naranja a 16px, respirando).
            for (int i = 0; i < _ecos.Length; i++)
            {
                bool interior = i < 4;
                Color tinte = (interior ? EcoCrema : EcoNaranja) * 0.8f;
                ComponerOjo(_ecos[i], rot, interior ? 0.55f : 0.5f, tinte, teñir: true);
            }

            // EL CUERPO VIVO (con sus colores propios).
            ComponerOjo(centro, rot, 1f, Color.White, teñir: false);

            // EL GLOW GOLDENROD PULSANTE (periodo 1.4 s — el número exacto).
            float latido = (float)Math.Cos(tiempo % 1.4f / 1.4f * MathHelper.TwoPi) * 0.5f + 0.5f;
            Texture2D glow = VFXCore.SoftGlow;
            Main.spriteBatch.Draw(glow, centro - Main.screenPosition, null,
                Color.Goldenrod * (0.35f * latido), 0f, glow.Size() * 0.5f,
                new Vector2(150f, 70f) / glow.Size(), SpriteEffects.None, 0f);

            // EL HALO: el anillo fino dorado girando lento.
            Texture2D ring = VFXCore.Ring;
            Vector2 anillo = VFXCore.RingQuadSize(60f);
            Main.spriteBatch.Draw(ring, centro - Main.screenPosition, null,
                Color.Goldenrod * 0.30f, tiempo * 0.4f, ring.Size() * 0.5f,
                anillo / ring.Size(), SpriteEffects.None, 0f);

            // === EL TELEGRAPH Y EL RAYO (en su fase del ciclo). ===
            int fase = _edad % Ciclo;
            bool mitadBeam = (_edad / Ciclo) % 2 == 1;
            if (mitadBeam)
            {
                int tTele = fase - Ciclo / 2;
                int tBeam = tTele - BeamTelegraph;
                Vector2 fin = centro + _dirBeam * BeamLargo;

                if (tTele >= 0 && tTele < BeamTelegraph)
                    OndaLib.Telegrafo(Main.spriteBatch, centro - Main.screenPosition,
                        fin - Main.screenPosition, tTele / (float)BeamTelegraph,
                        Color.Goldenrod, 3f);

                if (tBeam >= 0 && tBeam < BeamTicks)
                {
                    // EL RAYO: la campana sin(π·t/B) del original (nace a
                    // tope y muere) — halo dorado→naranja + núcleo blanco.
                    float campana = MathF.Sin(tBeam / (float)BeamTicks * MathF.PI);
                    Color halo = Color.Lerp(Color.Goldenrod, Color.OrangeRed, 0.65f) * (0.55f * campana);
                    Color nucleo = Color.White * (0.85f * campana);

                    Vector2 pantalla = centro - Main.screenPosition;
                    DibujarLinea(glow, pantalla, fin - Main.screenPosition, 70f, halo);
                    DibujarLinea(glow, pantalla, fin - Main.screenPosition, 26f, nucleo);
                    DibujarLinea(glow, pantalla, fin - Main.screenPosition, 10f, Color.White * campana);
                }
            }

            Main.spriteBatch.End();

            // --- PASO 2 (alpha): LA PUPILA — el vacío del ojo ENCIMA de
            //     todo el brillo (oscurece el cuerpo vivo y sus ecos). ---
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            ComponerPupila(centro, rot, 0.6f);
            Main.spriteBatch.End();
        }

        /// <summary>Llena el búfer de las 7 posiciones de los ecos orbitales.</summary>
        private void LlenarEcos(Vector2 centro, float tempo, float pulso)
        {
            for (int i = 0; i < 4; i++)
                _ecos[i] = EcosLib.EcoOrbital(centro, tempo, i * 0.25f, 8f * (0.6f + 0.4f * pulso));
            for (int i = 0; i < 3; i++)
                _ecos[4 + i] = EcosLib.EcoOrbital(centro, tempo, i * 0.34f, 16f * (0.6f + 0.4f * pulso));
        }

        /// <summary>
        /// Compone EL OJO ALADO en una posición. Con <paramref name="teñir"/>
        /// TODO el ojo se dibuja del color del eco (el fantasma); si no,
        /// con sus colores propios (el cuerpo vivo).
        /// </summary>
        private void ComponerOjo(Vector2 pos, float rot, float escala, Color tinte, bool teñir)
        {
            Texture2D glow = VFXCore.SoftGlow;
            Vector2 pantalla = pos - Main.screenPosition;
            Vector2 tam = glow.Size();

            Color cuerpo = teñir ? tinte : CuerpoOro * 0.85f;
            Color ala = teñir ? tinte : AlaNaranja * 0.8f;
            Color punta = teñir ? tinte : PuntaRoja * 0.85f;

            // EL CUERPO: crema-oro horizontal (110×40).
            Main.spriteBatch.Draw(glow, pantalla, null, cuerpo, rot, tam * 0.5f,
                new Vector2(110f, 40f) * escala / tam, SpriteEffects.None, 0f);

            // LAS ALAS: naranjas, inclinadas ±25°, a los costados.
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Vector2 offset = new Vector2(38f * lado, -4f).RotatedBy(rot);
                Main.spriteBatch.Draw(glow, pantalla + offset, null, ala,
                    rot + lado * -0.44f, tam * 0.5f,
                    new Vector2(52f, 16f) * escala / tam, SpriteEffects.None, 0f);

                // LAS PUNTAS: rojo oscuro al final de cada ala.
                Vector2 puntaOffset = new Vector2(62f * lado, -12f).RotatedBy(rot);
                Main.spriteBatch.Draw(glow, pantalla + puntaOffset, null, punta,
                    rot + lado * -0.6f, tam * 0.5f,
                    new Vector2(22f, 10f) * escala / tam, SpriteEffects.None, 0f);
            }
        }

        /// <summary>LA PUPILA: el vacío horizontal del ojo (pase ALPHA — oscurece).</summary>
        private void ComponerPupila(Vector2 pos, float rot, float escala)
        {
            Texture2D glow = VFXCore.SoftGlow;
            Vector2 pantalla = pos - Main.screenPosition;
            Main.spriteBatch.Draw(glow, pantalla, null, Color.Black * 0.55f, rot,
                glow.Size() * 0.5f,
                new Vector2(34f, 12f) * escala / glow.Size(), SpriteEffects.None, 0f);
        }

        /// <summary>Un quad estirado de A a B sobre el lote abierto (el rayo).</summary>
        private void DibujarLinea(Texture2D tex, Vector2 a, Vector2 b, float grosor, Color color)
        {
            if (color.A == 0) return;
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 1f) return;
            Main.spriteBatch.Draw(tex, a + delta * 0.5f, null, color, delta.ToRotation(),
                tex.Size() * 0.5f, new Vector2(len, grosor) / tex.Size(),
                SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// RafagaNovaProjectile — v6.41 — LA RÁFAGA NOVA.
    ///
    /// El dardo ardiente de la volea del fragmento (el calco del
    /// proyectil de ataque del original): vuela ACELERANDO ×1.01 por
    /// tick con su cola de fantasmas (EcosLib — la memoria de la casa),
    /// y al morir FLORECE: la FLOR DE FUEGO radial de seis lengüetas
    /// (PyraLib.Estallido — nacido de esta réplica) con su daño en área.
    /// </summary>
    public class RafagaNovaProjectile : ModProjectile
    {
        /// <summary>Los ticks de la flor de fuego al morir.</summary>
        private const int MuerteTicks = 30;

        /// <summary>El radio de la flor (px).</summary>
        private const float RadioFlor = 92f;

        private int _edad;
        private bool _muriendo;
        private int _golpes;
        private EcosLib.Memoria _memoria;

        /// <summary>Semilla determinista por identidad.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 271);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            // v6.41: penetración INFINITA + contador de golpes propio — la
            // vainilla mataría el dardo al agotar penetración SIN pasar por
            // la flor; así el tercer golpe SIEMPRE florece.
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.aiStyle = -1;
            _memoria = EcosLib.Crear();
        }

        public override void AI()
        {
            _edad++;

            if (!_muriendo)
            {
                // === EL VUELO: aceleración ×1.01 (el número del original). ===
                Projectile.velocity *= 1.01f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                // La luz del dardo (naranja fuerte — la del original ×1.75).
                Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 1.75f);

                EcosLib.Registrar(ref _memoria, Projectile.Center, Projectile.rotation);

                // === LA MUERTE: tiempo agotado → LA FLOR. ===
                if (Projectile.timeLeft <= 1)
                    Morir();
            }
            else
            {
                // === LA FLOR DE FUEGO: quieta, ardiendo. ===
                Projectile.velocity = Vector2.Zero;
                Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 0.9f);

                if (Projectile.timeLeft <= 1)
                    Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            // El polvo de la flor (la lluvia del estallido).
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch);
                float ang = i / 14f * MathHelper.TwoPi;
                d.velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(1.5f, 4f);
                d.scale = Main.rand.NextFloat(0.8f, 1.5f);
                d.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Morir();          // la flor nace DONDE tocó — el dardo no rebota
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // El tercer golpe mata floreciendo (como la bomba del original:
            // el impacto ES la explosión) — con penetración infinita el
            // contador es NUESTRO y el dardo NUNCA muere sin flor.
            _golpes++;
            if (_golpes >= 3)
                Morir();
        }

        /// <summary>EL PASO A LA FLOR: daño en área + el cambio de fase.</summary>
        private void Morir()
        {
            if (_muriendo) return;
            _muriendo = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = MuerteTicks;
            Projectile.tileCollide = false;
            Projectile.friendly = false;

            // El golpe de área de la flor (solo autoridad — escuela A).
            if (Main.myPlayer == Projectile.owner)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.Distance(npc.Center, Projectile.Center) > RadioFlor * 0.8f) continue;
                    npc.SimpleStrikeNPC((int)(Projectile.damage * 0.8f),
                        npc.direction, false, 3f, DamageClass.Summon);
                }
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                if (!_muriendo)
                {
                    // === LA COLA DE FANTASMAS (búfer de VFXCore + su volcado
                    //     propio — no hay lote abierto: lo maneja Flush). ===
                    EcosLib.ColaHistoria(ref _memoria, 2, 5,
                        new Color(255, 216, 150), new Vector2(30f, 12f), 0.5f, 1.2f);
                    VFXCore.FlushAdditive(null, false);
                }

                // === EL DARDO / LA FLOR (mi pase aditivo propio). ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (!_muriendo)
                {
                    Texture2D glow = VFXCore.SoftGlow;
                    Vector2 pos = Projectile.Center - Main.screenPosition;
                    Main.spriteBatch.Draw(glow, pos, null, new Color(240, 120, 72) * 0.75f,
                        Projectile.rotation, glow.Size() * 0.5f,
                        new Vector2(46f, 18f) / glow.Size(), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glow, pos, null, new Color(255, 220, 150) * 0.95f,
                        Projectile.rotation, glow.Size() * 0.5f,
                        new Vector2(24f, 8f) / glow.Size(), SpriteEffects.None, 0f);
                }
                else
                {
                    // === LA FLOR DE FUEGO (el estallido del original). ===
                    float progreso = 1f - Projectile.timeLeft / (float)MuerteTicks;
                    PyraLib.Estallido(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                        RadioFlor, progreso, PyraPalettes.SolarFire, Seed,
                        Main.GlobalTimeWrappedHourly, 0.85f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
