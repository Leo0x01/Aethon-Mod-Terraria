using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using global::AethonMod.Content.Buffs;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  SolEnMiniaturaProj — v6.85 — EL MINION DEL SOL EN MINIATURA (R3).
    //
    //  LA MÁQUINA (la receta del informe r3_meac_emperatriz.md §4):
    //   · ÓRBITA: radio 3,5 tiles (56 px), período 240 ticks alrededor
    //     del dueño.
    //   · EL NÚCLEO TÉRMICO: 4 glows concéntricos — blanco (255,255,240)
    //     · amarillo (255,255,0) · naranja (255,140,0) · borde rojo
    //     (255,99,71) — con la RAMPA DE VIDA de MEAC (dorado #D4A017 →
    //     blanco por edad — la firma «EoLProj_Color», la técnica de
    //     gradient-map de VFX+ aplicada por edad en CPU).
    //   · LOS 2 ANILLOS DORADOS (255,223,0): elípticos (perspectiva 25°:
    //     squash Y 0,906) contra-rotando ±40°/s.
    //   · EL HALO: estrella de 8 puntas (dos flares cruzados a 45°).
    //   · EL ATAQUE (cada 45 ticks): telegrafía 10 ticks (la bola crece
    //     0,2→1,0 — el aviso de XiHe) → EL LASER0 dorado (255,220,120):
    //     núcleo 8 px + glow 24 px, alcance 40 tiles (640 px), activo
    //     20 ticks siguiendo al blanco; daño posicional cada 4 ticks.
    //   · EL IMPACTO: onda 0,5→2,0 (α 0,6→0) + 8 motas de luz doradas.
    //
    //  PERSISTENTE (la letra del usuario): NO se mata cuando el dueño
    //  muere (el sol sigue orbitando) — solo la despedida (clic derecho
    //  del buff) o el sello de la re-invocación lo apagan.
    // ======================================================================
    public class SolEnMiniaturaProj : ModProjectile
    {
        private const int AtaqueCd = 45;
        private const int TelegrafiaTicks = 10;
        private const int LaserTicks = 20;
        private const float AlcanceLaser = 640f;

        private static Asset<Texture2D> _flare;

        // LA PALETA TÉRMICA (§4 del informe).
        private static readonly Color NucleoBlanco = new(255, 255, 240);
        private static readonly Color Amarillo = new(255, 255, 0);
        private static readonly Color Naranja = new(255, 140, 0);
        private static readonly Color RojoBorde = new(255, 99, 71);
        private static readonly Color LaserDorado = new(255, 220, 120);

        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.tileCollide = false;
            Projectile.friendly = false;          // el daño es el LASER (posicional)
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.light = 0.8f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }
            // v6.85 — PERSISTENTE: el dueño muerto NO lo mata (la letra:
            // «aunque el jugador muera… siempre permanecen invocados»).

            global::AethonMod.Content.Systems.EscuadronUtil.Sostener(
                Projectile, owner, ModContent.BuffType<SolEnMiniaturaBuff>());

            Projectile.ai[0]++;                   // la edad
            float t = Projectile.ai[0];

            // === LA ÓRBITA (radio 3,5 tiles, período 240) ===
            float ang = t / 240f * MathHelper.TwoPi + Projectile.minionPos * 1.7f;
            Vector2 hover = owner.Center + new Vector2(
                (float)Math.Cos(ang), (float)Math.Sin(ang) * 0.55f) * 56f;
            if (owner.dead) hover = owner.Center + new Vector2(0, -60f);
            Vector2 to = hover - Projectile.Center;
            Projectile.velocity += to * 0.045f;
            Projectile.velocity *= 0.88f;

            // === EL CICLO DEL ATAQUE (ai[1]: el reloj; ai[2]: la fase) ===
            // fases: 0 = reposo (cuenta atrás 45) · 1 = telegrafía (10) ·
            // 2 = láser (20) — con el blanco en localAI[1] (whoAmI).
            float fase = Projectile.ai[2];
            if (fase == 0f)
            {
                if (Projectile.ai[1]-- <= 0)
                {
                    NPC blanco = EncontrarBlanco(owner);
                    if (blanco != null)
                    {
                        Projectile.localAI[1] = blanco.whoAmI;
                        Projectile.ai[2] = 1f;
                        Projectile.ai[1] = TelegrafiaTicks;
                    }
                    else Projectile.ai[1] = AtaqueCd / 3;   // reintento pronto
                }
            }
            else if (fase == 1f)
            {
                // LA TELEGRAFÍA: la bola crece 0,2→1,0 (el aviso de XiHe).
                if (Projectile.ai[1]-- <= 0)
                {
                    Projectile.ai[2] = 2f;
                    Projectile.ai[1] = LaserTicks;
                    if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(0.3f).WithVolumeScale(0.7f),
                            Projectile.Center);
                }
            }
            else
            {
                // EL LÁSER: daño posicional cada 4 ticks a lo largo de la
                // línea hasta el blanco (siguiéndolo mientras viva).
                NPC blanco = Projectile.localAI[1] >= 0 && Projectile.localAI[1] < Main.maxNPCs
                    ? Main.npc[(int)Projectile.localAI[1]] : null;
                if (blanco == null || !blanco.active || !VFXCore.EsObjetivo(blanco))
                {
                    // El blanco murió a medias: el láser se apaga.
                    Projectile.ai[2] = 0f;
                    Projectile.ai[1] = AtaqueCd;
                }
                else if (Projectile.owner == Main.myPlayer && t % 4f < 1f)
                {
                    Vector2 dir = blanco.Center - Projectile.Center;
                    float dist = Math.Min(dir.Length(), AlcanceLaser);
                    if (dir.LengthSquared() > 1f) dir /= dir.Length();
                    Vector2 fin = Projectile.Center + dir * dist;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        // la distancia del npc al SEGMENTO (el láser atraviesa).
                        float d = DistanciaASegmento(npc.Center, Projectile.Center, fin);
                        if (d < 40f)
                            global::AethonMod.Content.Systems.GolpeMotor.Golpear(
                                Projectile, npc, 30, 1.2f, true);
                    }
                    // LA ONDA DEL IMPACTO (escala 0,5→2,0, α 0,6→0) + 8 motas.
                    if (Main.netMode != NetmodeID.Server && !Main.dedServ && t % 8f < 1f)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Dust d = Dust.NewDustPerfect(fin + Main.rand.NextVector2Circular(30f, 30f),
                                DustID.Electric, Main.rand.NextVector2Circular(1.2f, 1.2f),
                                0, RampaVida(t), Main.rand.NextFloat(0.7f, 1.3f));
                            d.noGravity = true;
                        }
                    }
                }

                if (Projectile.ai[1]-- <= 0)
                {
                    Projectile.ai[2] = 0f;
                    Projectile.ai[1] = AtaqueCd;
                }
            }

            // LA LUZ DEL SOL (dorado cálido).
            Lighting.AddLight(Projectile.Center, 0.85f, 0.72f, 0.35f);
        }

        /// <summary>EL BLANCO: el más cercano al sol (con el marcado por
        /// el jugador primero — el patrón de la casa).</summary>
        private static NPC EncontrarBlanco(Player owner)
        {
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC marcado = Main.npc[owner.MinionAttackTargetNPC];
                if (marcado != null && VFXCore.EsObjetivo(marcado)) return marcado;
            }
            NPC mejor = null;
            float mejorD = AlcanceLaser;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = Vector2.Distance(npc.Center, Main.player[npc.target]?.Center ?? owner.Center);
                if (d < mejorD) { mejorD = d; mejor = npc; }
            }
            return mejor;
        }

        private static float DistanciaASegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 1f) return Vector2.Distance(p, a);
            float s = MathHelper.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
            return Vector2.Distance(p, a + ab * s);
        }

        /// <summary>LA RAMPA DE VIDA de MEAC (la firma EoLProj_Color:
        /// dorado #D4A017 → blanco en 60 ticks).</summary>
        private static Color RampaVida(float t) =>
            Color.Lerp(new Color(212, 160, 23), new Color(255, 255, 255),
                MathHelper.Clamp(t % 60f / 60f, 0f, 1f));

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;

            if (_flare == null)
                _flare = ModContent.Request<Texture2D>("AethonMod/Content/Effects/FlareEstrella4Puntas",
                    AssetRequestMode.ImmediateLoad);

            Player owner = Main.player[Projectile.owner];
            float t = Projectile.ai[0];
            Vector2 pos = Projectile.Center - Main.screenPosition;
            if (pos.X < -200 || pos.X > Main.screenWidth + 200 ||
                pos.Y < -200 || pos.Y > Main.screenHeight + 200) return false;

            // LA TELEGRAFÍA: la bola crece 0,2→1,0 (fase 1).
            float crece = 1f;
            if (Projectile.ai[2] == 1f)
                crece = MathHelper.Lerp(0.2f, 1f,
                    1f - Projectile.ai[1] / (float)TelegrafiaTicks);

            VFXCore.AbrirAdditive();
            try
            {
                // === 1. EL NÚCLEO TÉRMICO (4 glows concéntricos) ===
                float r = 34f * Projectile.scale * crece;
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, RojoBorde * 0.55f, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(r * 2.6f / 32f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, Naranja * 0.6f, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(r * 1.9f / 32f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, Amarillo * 0.7f, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(r * 1.3f / 32f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.GlowOrb, pos, null, NucleoBlanco * 0.9f, 0f,
                    VFXCore.GlowOrb.Size() * 0.5f, new Vector2(r * 0.6f / 32f), SpriteEffects.None, 0f);

                // === 2. LOS 2 ANILLOS DORADOS (±40°/s, perspectiva 25°) ===
                float seg = t / 60f;
                float squash = 0.906f;   // cos(25°)
                var origR = VFXCore.Ring.Size() * 0.5f;
                Color anillo = RampaVida(t) * 0.75f;
                Main.spriteBatch.Draw(VFXCore.Ring, pos, null, anillo,
                    seg * 0.698f, origR,
                    new Vector2(r * 2.1f / origR.X, r * 2.1f * squash / origR.Y),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.Ring, pos, null, anillo * 0.8f,
                    -seg * 0.698f + 0.7f, origR,
                    new Vector2(r * 1.55f / origR.X, r * 1.55f * squash / origR.Y),
                    SpriteEffects.None, 0f);

                // === 3. EL HALO DE 8 PUNTAS (dos flares cruzados a 45°) ===
                Main.spriteBatch.Draw(_flare.Value, pos, null,
                    new Color(255, 223, 0) * 0.5f, 0f,
                    _flare.Value.Size() * 0.5f, new Vector2(0.4f * crece), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(_flare.Value, pos, null,
                    new Color(255, 223, 0) * 0.5f, MathHelper.Pi / 4f,
                    _flare.Value.Size() * 0.5f, new Vector2(0.4f * crece), SpriteEffects.None, 0f);

                // === 4. EL LÁSER ACTIVO (núcleo 8 px + glow 24 px) ===
                if (Projectile.ai[2] == 2f)
                {
                    NPC blanco = Projectile.localAI[1] >= 0 && Projectile.localAI[1] < Main.maxNPCs
                        ? Main.npc[(int)Projectile.localAI[1]] : null;
                    if (blanco != null && blanco.active)
                    {
                        Vector2 dir = blanco.Center - Projectile.Center;
                        float dist = Math.Min(dir.Length(), AlcanceLaser);
                        if (dir.LengthSquared() > 1f) dir /= dir.Length();
                        Vector2 a = pos, b = pos + dir * dist;
                        float ang = (float)Math.Atan2(dir.Y, dir.X);
                        float largo = dist;

                        // EL GLOW (24 px) y el NÚCLEO (8 px) — el Pixel estirado.
                        Main.spriteBatch.Draw(VFXCore.Pixel,
                            new Rectangle((int)a.X, (int)a.Y, (int)largo, 24),
                            new Rectangle(0, 0, 1, 1), LaserDorado * 0.45f,
                            ang, Vector2.Zero, SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.Pixel,
                            new Rectangle((int)a.X, (int)a.Y, (int)largo, 8),
                            new Rectangle(0, 0, 1, 1), NucleoBlanco * 0.85f,
                            ang, Vector2.Zero, SpriteEffects.None, 0f);

                        // LA ONDA DEL IMPACTO en el pie del láser (0,5→2,0).
                        float faseL = 1f - Projectile.ai[1] / (float)LaserTicks;
                        float esc = MathHelper.Lerp(0.5f, 2f, faseL);
                        float alfaO = 0.6f * (1f - faseL);
                        Main.spriteBatch.Draw(VFXCore.Ring, b, null,
                            LaserDorado * alfaO, 0.15f * t, origR,
                            new Vector2(esc * 60f / origR.X, esc * 60f * 0.5f / origR.Y),
                            SpriteEffects.None, 0f);
                    }
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }
}
