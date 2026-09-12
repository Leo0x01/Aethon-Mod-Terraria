using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// v5.99 — EL PÚLSAR VIVO (minion invocador — el proyectil ES la
    /// invocación). Petición del usuario: "luego crea otro minion cosmico".
    ///
    /// LA CRIATURA: una estrella de neutrones VIVA — un núcleo blanco-azul
    /// de densidad absurda (PulsarCore) con arcos magnéticos y bandas de
    /// giro, que GIRA sobre su eje y BARRE el campo con DOS HACES DE FARO
    /// opuestos de radiación (como un púlsar de verdad: el faro giratorio
    /// del cosmos). NADIE en el arsenal ataca así: el daño no viene de
    /// contacto ni de proyectiles — viene de los HAZES QUE BARRIEAN: dos
    /// rayos rotando que ELECTRIFICAN todo lo que cruzan.
    ///
    /// LOCOMOCIÓN: deriva en un LAZY LISSAJOUS (figura-8 perezosa) junto al
    /// jugador; con objetivo a la vista se coloca en alto, a media distancia
    /// entre el jugador y la víctima — la posición perfecta para que sus
    /// haces la RAQUEN en cada giro.
    ///
    /// LOS HACES: cada tick el AI comprueba qué enemigos caen dentro de los
    /// DOS rayos (distancia ≤ 340 px y ángulo alineado con el haz — la
    /// tolerancia angular crece con la distancia, como un haz real que se
    /// abre) y los golpea con enfriamiento local 20 ticks + ELECTRIFIED (la
    /// firma del púlsar: radiación de sincrotrón). Dibujados como rayos
    /// cónicos blancos-cian con núcleo, halo y chispas lanzadas desde la
    /// punta por la rotación.
    ///
    /// EL ESPACIOTIEMPO: fuente sutil del pase B que PULSA con el giro.
    ///
    /// Ciclo de vida: patrón CosmicOrb (buff LivingPulsarBuff sostenido
    /// por el propio púlsar). Campos: ai[0]=edad · ai[1]=fase de giro
    /// (rad, determinista = edad×omega) · todo derivado de la edad.
    /// </summary>
    public class LivingPulsarMinion : ModProjectile
    {
        // === EL GIRO ===
        /// <summary>Velocidad angular del faro (rad/tick) — vuelta cada ~6.9 s.</summary>
        internal const float SpinOmega = 0.015f;

        // === LOS HACES ===
        private const float BeamLength = 340f;
        private const float BeamHalfWidth = 16f;   // grosor en la base
        private const int BeamHitCooldown = 20;

        // --- Estado visual local (solo cliente) ---
        private Vector2[] _drift;   // historia corta para el rastro del giro

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BeamHitCooldown;
            Projectile.aiStyle = -1;
            Projectile.light = 0.85f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;  // el daño son los HACES

        // ================================================================
        //  HELPERS (lente + ángulo de giro determinista)
        // ================================================================

        /// <summary>Ángulo del haz principal en este tick (rad, determinista).</summary>
        internal static float GetSpinAngle(Projectile p)
        {
            return p.ai[0] * SpinOmega;
        }

        /// <summary>Radio visual del núcleo (px).</summary>
        internal static float GetPulsarVisualRadius(Projectile p)
        {
            return p.width * p.scale * 0.30f;
        }

        /// <summary>Fuerza de lente (pase B): PULSA con el giro (0.05→0.12).</summary>
        internal static float GetPulsarLensStrength(Projectile p)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(GetSpinAngle(p) * 2f);
            return 0.05f + 0.07f * pulse;
        }

        // ================================================================
        //  AI — el faro que barre
        // ================================================================

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            CheckMinionBuff(owner);

            float age = Projectile.ai[0];
            Projectile.ai[0] = age + 1f;
            float spin = GetSpinAngle(Projectile);

            NPC target = FindTarget(owner);

            // === POSICIÓN: lissajous perezoso / punto de raqueo ===
            Vector2 hover;
            if (target != null)
            {
                // en alto, a media distancia jugador-víctima: los haces la rañan
                Vector2 mid = (owner.Center + target.Center) * 0.5f;
                hover = mid + new Vector2(0f, -110f);
            }
            else
            {
                // figura-8 perezosa sobre el hombro (fase por minionPos)
                float t = age * 0.02f + Projectile.minionPos * 1.3f;
                hover = owner.Center + new Vector2(
                    (float)Math.Sin(t) * 66f,
                    -62f + (float)Math.Sin(t * 2f) * 26f);
            }
            Vector2 toHover = hover - Projectile.Center;
            Projectile.velocity += toHover * 0.02f;
            Projectile.velocity *= 0.90f;
            float spd = Projectile.velocity.Length();
            if (spd > 8f) Projectile.velocity *= 8f / spd;

            // === NO PERDERSE ===
            if (Vector2.Distance(Projectile.Center, owner.Center) > 1200f)
            {
                Projectile.Center = owner.Center - Vector2.UnitY * 62f;
                Projectile.velocity *= 0.2f;
            }

            // === LOS HACES BARREN (daño en rayo — SimpleStrikeNPC) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                age % 5f < 1f)   // cada 5 ticks: 12 golpes/s girando
            {
                int beamDamage = Math.Max(1, (int)(Projectile.damage * 0.55f));
                float mainAngle = spin;
                float antiAngle = spin + MathHelper.Pi;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 to = npc.Center - Projectile.Center;
                    float dist = to.Length();
                    if (dist > BeamLength || dist < 4f) continue;

                    float ang = (float)Math.Atan2(to.Y, to.X);
                    // tolerancia angular: el haz se ABRE con la distancia
                    float halfAngle = (float)Math.Atan2(
                        BeamHalfWidth + dist * 0.035f, dist);
                    bool hitMain = Math.Abs(AngleDiff(ang, mainAngle)) < halfAngle;
                    bool hitAnti = Math.Abs(AngleDiff(ang, antiAngle)) < halfAngle;
                    if (hitMain || hitAnti)
                    {
                        npc.SimpleStrikeNPC(beamDamage, npc.direction, false, 1.5f, DamageClass.Summon);
                        // radiación de sincrotrón: ELECTRIFIED (la firma)
                        try { npc.AddBuff(BuffID.Electrified, 160); } catch { }
                    }
                }
            }

            // === SIMULACIÓN VISUAL (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                UpdateDrift();

                // chispas lanzadas TANGENCIALMENTE desde la punta de los haces
                if (Main.rand.NextBool(2))
                {
                    bool main = Main.rand.NextBool(2);
                    float a = spin + (main ? 0f : MathHelper.Pi);
                    Vector2 tip = Projectile.Center + new Vector2(
                        (float)Math.Cos(a), (float)Math.Sin(a)) * BeamLength * 0.96f;
                    Vector2 tangent = new Vector2(
                        (float)Math.Cos(a + MathHelper.PiOver2),
                        (float)Math.Sin(a + MathHelper.PiOver2)) * 2.2f;
                    Dust d = Dust.NewDustPerfect(tip, DustID.BlueTorch,
                        tangent, 180, new Color(150, 225, 255), 0.55f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LUZ (azul-blanca, pulsa con el doble de la frecuencia de giro) ===
            float pulse2 = 0.75f + 0.25f * (float)Math.Sin(spin * 2f);
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.7f, 0.85f, 1.0f) * pulse2);
        }

        /// <summary>Diferencia angular envuelta a [-π, π].</summary>
        private static float AngleDiff(float a, float b)
        {
            float d = a - b;
            while (d > MathHelper.Pi) d -= MathHelper.TwoPi;
            while (d < -MathHelper.Pi) d += MathHelper.TwoPi;
            return d;
        }

        private void UpdateDrift()
        {
            const int Len = 12;
            if (_drift == null)
            {
                _drift = new Vector2[Len];
                for (int i = 0; i < Len; i++) _drift[i] = Projectile.Center;
                return;
            }
            for (int i = Len - 1; i > 0; i--) _drift[i] = _drift[i - 1];
            _drift[0] = Projectile.Center;
        }

        // ================================================================
        //  OBJETIVO + BUFF (patrón del arsenal)
        // ================================================================

        private NPC FindTarget(Player owner)
        {
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC marked = Main.npc[owner.MinionAttackTargetNPC];
                if (marked != null && marked.active && marked.CanBeChasedBy())
                    return marked;
            }
            NPC closest = null;
            float closestDist = 640f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage || npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < closestDist) { closestDist = dist; closest = npc; }
            }
            return closest;
        }

        private void CheckMinionBuff(Player owner)
        {
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.LivingPulsarBuff>();
            bool hasBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (owner.buffType[i] == buffType && owner.buffTime[i] > 0)
                { hasBuff = true; break; }
            }
            if (!hasBuff) Projectile.Kill();
            else
            {
                owner.AddBuff(buffType, 18000);
                Projectile.timeLeft = 2;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // el púlsar se apaga: destello final + chispas azules
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 20; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                        Main.rand.NextFloat(4f, 22f),
                    DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * Main.rand.NextFloat(1f, 3f),
                    210, new Color(160, 225, 255), 0.65f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — el faro giratorio
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (BlackHoleLensSystem.LensActive)
                return false;

            if (DrawPulsarVisuals(Projectile, true))
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja el púlsar completo (protocolo del arsenal: true = batch
        /// tomado y dejado CERRADO). Capas:
        ///   1. LOS DOS HACES cónicos (núcleo blanco + halo cian) con
        ///      franjas de brillo a lo largo (el pulso del faro).
        ///   2. RASTRO de giro (arco tenue que deja el haz — un remolino).
        ///   3. EL NÚCLEO blanco-azul con arcos magnéticos (PulsarCore).
        /// </summary>
        internal static bool DrawPulsarVisuals(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active) return false;
                var mp = p.ModProjectile as LivingPulsarMinion;
                if (mp == null) return false;

                float spin = GetSpinAngle(p);
                float coreR = GetPulsarVisualRadius(p);
                Vector2 drawPos = p.Center - Main.screenPosition;

                Texture2D coreTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/PulsarCore").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LOS DOS HACES (cónicos: gruesos en la base, finos fuera) ===
                for (int b = 0; b < 2; b++)
                {
                    float a = spin + b * MathHelper.Pi;
                    Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                    // el brillo del haz PULSA a lo largo (3 nudos de radiación)
                    for (int k = 0; k < 4; k++)
                    {
                        float t0 = 0.10f + k * 0.225f;
                        float segLen = 0.24f;
                        float width = MathHelper.Lerp(BeamHalfWidth, 3f, t0 + segLen * 0.5f);
                        // pulso viajando por el haz (la firma del faro)
                        float pulse = 0.55f + 0.45f * (float)Math.Sin(
                            spin * 8f - t0 * 9f);
                        Vector2 sPos = drawPos + dir * (coreR * 0.9f + BeamLength * t0);
                        Vector2 sPos2 = drawPos + dir * (coreR * 0.9f + BeamLength * (t0 + segLen));
                        Vector2 mid = (sPos + sPos2) * 0.5f;
                        float l = (sPos2 - sPos).Length();
                        float rot = (float)Math.Atan2(dir.Y, dir.X);
                        // halo cian + núcleo blanco
                        Main.spriteBatch.Draw(softGlow, mid, null,
                            new Color(90, 190, 255, (byte)(int)(70 * pulse)), rot,
                            new Vector2(0f, softGlow.Height * 0.5f),
                            new Vector2(l / (softGlow.Width * 0.5f), width * 1.9f / (softGlow.Height * 0.5f)),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(softGlow, mid, null,
                            new Color(220, 245, 255, (byte)(int)(160 * pulse)), rot,
                            new Vector2(0f, softGlow.Height * 0.5f),
                            new Vector2(l / (softGlow.Width * 0.5f), width * 0.75f / (softGlow.Height * 0.5f)),
                            SpriteEffects.None, 0f);
                    }
                }

                // === 2. RASTRO DE GIRO (el remolino que dejan los haces) ===
                if (mp._drift != null)
                {
                    for (int i = mp._drift.Length - 1; i >= 1; i -= 2)
                    {
                        float t = i / (float)mp._drift.Length;
                        Vector2 dp = mp._drift[i] - Main.screenPosition;
                        Main.spriteBatch.Draw(softGlow, dp, null,
                            new Color(120, 190, 255, (byte)(int)(36 * (1f - t))), spin,
                            softGlow.Size() * 0.5f,
                            (coreR * (1.2f + 0.5f * t)) / (softGlow.Width * 0.5f),
                            SpriteEffects.None, 0f);
                    }
                }

                // glow del núcleo (denso: brilla MUCHO)
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(170, 215, 255, 150), 0f,
                    softGlow.Size() * 0.5f,
                    (coreR * 3.2f) / (softGlow.Width * 0.5f), SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 3. EL NÚCLEO (AlphaBlend, sólido) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float coreScale = coreR / (coreTex.Width * 0.5f);
                Main.spriteBatch.Draw(coreTex, drawPos, null, Color.White, spin * 0.35f,
                    coreTex.Size() * 0.5f, coreScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                return true;
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                return true;
            }
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
