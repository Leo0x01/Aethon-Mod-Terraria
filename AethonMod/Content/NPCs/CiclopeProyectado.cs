using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // =====================================================================
    //  CÍCLOPEPROYECTADO — v6.175 — LA RECREACIÓN DEL VÍDEO (la letra:
    //  «quieres decir que no puedes recrear el Cíclope Proyectado con
    //  sus efectos» + el link de «Me Proyecte», el short de Chopilas).
    //
    //  LO QUE EL VÍDEO MUESTRA (análisis visual de la miniatura y tres
    //  fotogramas reales): un jefe HUMANOIDE PÁLIDO tipo maniquí con
    //  cabeza ovalada y UN SOLO OJO rosa-magenta, que NACE de un HAZ DE
    //  PROYECCIÓN vertical violeta-magenta (partículas rosas cayendo,
    //  cono de luz que «se va expandiendo hacia tu esencia»), flota a
    //  media altura con los brazos abiertos, pelea con su barra de vida
    //  de jefe ROSA y el jugador grita «¡ME PROYECTO!».
    //
    //  LA IA (cuatro estados, la coreografía del vídeo):
    //   0 NACIENDO (90 t) — el haz lo ESCUPE del suelo: invulnerable,
    //     sube lentamente, el cono violeta+magenta y sus partículas.
    //   1 FLOTANDO — el vaivén del maniquí: persigue la altura del
    //     jugador con deriva sinusoidal (flota, NO camina).
    //   2 EL RAYO DEL OJO — el telegraph (el ojo GRITA creciendo) y el
    //     haz magenta (StormLib.Bolt de la casa) que barre al jugador.
    //   3 EL DERRAME — la lluvia radial de OJOS pequeños (proyecciones
    //     hijas) que caen sobre el jugador (JefeInteriorVolea, tema 11).
    //  LA SEGUNDA MITAD (vida < 50 %): «se va expandiendo» — su escala
    //  CRECE ×1.3, sus cadencias se aprietan y convoca DOS Ecos
    //  (proyecciones menores de sí mismo).
    // =====================================================================
    public class CiclopeProyectado : ModNPC
    {
        private const float T_NACER = 90f;

        /// <summary>El estado vivo (0 naciendo · 1 flotando · 2 rayo ·
        /// 3 derrame).</summary>
        private float Estado => NPC.ai[0];

        /// <summary>El reloj del estado (ai[1]).</summary>
        private float Reloj => NPC.ai[1];

        /// <summary>¿Está en la segunda mitad (expandido)?</summary>
        private bool Expandido => NPC.life < NPC.lifeMax * 0.5f;

        public override void SetStaticDefaults()
        {
            // DisplayName: Mods.AethonMod.NPCs.CiclopeProyectado.DisplayName
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 76;
            NPC.damage = 38;
            NPC.defense = 12;
            NPC.lifeMax = 2600;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = Item.buyPrice(0, 1, 0, 0);
            NPC.knockBackResist = 0.35f;
            NPC.boss = true;                      // la barra de jefe ROSA del vídeo
            NPC.noGravity = true;                 // FLOTA: es una proyección
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 5f;
        }

        public override void AI()
        {
            if (NPC.ai[1]++ > 600000f) NPC.ai[1] = 0;    // el reloj no rebosa
            Player presa = Main.player[NPC.target];
            if (presa == null || !presa.active || presa.dead)
            {
                // sin presa: deambula y se apaga
                NPC.TargetClosest(true);
                if (!NPC.HasValidTarget)
                {
                    NPC.EncourageDespawn(60);
                    return;
                }
            }
            NPC.TargetClosest(false);
            presa = Main.player[NPC.target];

            // === ESTADO 0: NACIENDO (el haz lo escupe — invulnerable) ===
            if (NPC.ai[0] == 0f)
            {
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
                NPC.velocity *= 0.9f;
                // sube LENTO mientras el haz lo sostiene
                NPC.position.Y -= 0.45f;
                if (NPC.ai[1] >= T_NACER)
                {
                    NPC.ai[0] = 1f;
                    NPC.ai[1] = 0f;
                    NPC.dontTakeDamage = false;
                    NPC.netUpdate = true;
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.7f), NPC.Center);
                }
                return;
            }

            // LA EXPANSIÓN (la segunda mitad): el cuerpo del vídeo «se va
            // expandiendo» — la escala sube UNA vez (idempotente)
            if (Expandido && NPC.scale < 1.29f)
            {
                NPC.scale = 1.3f;
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                    for (int i = 0; i < 24; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(40f, 60f),
                            DustID.Shadowflame, Main.rand.NextVector2Circular(2f, 2f), 150,
                            new Color(255, 120, 220), 0.9f);
                        d.noGravity = true;
                    }
            }

            // === ESTADO 1: FLOTANDO (el vaivén del maniquí) ===
            if (NPC.ai[0] == 1f)
            {
                Vector2 meta = presa.Center + new Vector2(presa.direction * -160f, -90f);
                Vector2 rumbo = (meta - NPC.Center).SafeNormalize(Vector2.UnitX);
                float paso = Expandido ? 0.055f : 0.04f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, rumbo * (Expandido ? 6.5f : 5f), paso);
                NPC.velocity += new Vector2(MathF.Sin(NPC.ai[1] * 0.045f) * 0.12f,
                    MathF.Sin(NPC.ai[1] * 0.06f) * 0.1f);
                // EL CICLO DE ATAQUES: rayo · derrame · (expandido: los ecos)
                int cadencia = Expandido ? 150 : 210;
                if (NPC.ai[1] >= cadencia)
                {
                    NPC.ai[1] = 0f;
                    // alterna: rayo → derrame → rayo...
                    NPC.ai[0] = Main.rand.NextBool(2) ? 2f : 3f;
                    NPC.netUpdate = true;
                }
            }

            // === ESTADO 2: EL RAYO DEL OJO (telegraph → haz) ===
            if (NPC.ai[0] == 2f)
            {
                NPC.velocity *= 0.92f;
                float telegraph = 40f;
                float activo = telegraph + (Expandido ? 50f : 36f);
                if (NPC.ai[1] < telegraph)
                {
                    // EL TELEGRAPH: el ojo GRITA (crece — lo dibuja el PreDraw)
                    NPC.velocity *= 0.9f;
                    if (NPC.ai[1] == 1f)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item72.WithPitchOffset(-0.4f).WithVolumeScale(0.3f), NPC.Center);
                }
                else if (NPC.ai[1] < activo)
                {
                    // EL HAZ MAGENTA: barre hacia la presa (daño por contacto
                    // de línea — la casa NO dispara proyectiles hostiles
                    // invisible: el Bolt es visual, el daño lo pega la línea)
                    Vector2 origen = PosicionDelOjo();
                    Vector2 fin = presa.Center;
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[1] % 6f == 0f)
                    {
                        // el corridor del haz: si el jugador vive en la línea
                        // (±26 px) y a distancia, el rayo MORDERÁ
                        Vector2 dir = (fin - origen).SafeNormalize(Vector2.UnitX);
                        float largo = Vector2.Distance(origen, fin);
                        Vector2 rel = presa.Center - origen;
                        float proyeccion = Vector2.Dot(rel, dir);
                        if (proyeccion > 0f && proyeccion < largo + 40f)
                        {
                            float lateral = MathF.Abs(rel.X * dir.Y - rel.Y * dir.X);
                            if (lateral < 30f + presa.width * 0.5f)
                                presa.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI),
                                    NPC.damage / 2, NPC.direction);
                        }
                    }
                }
                else { NPC.ai[0] = 1f; NPC.ai[1] = 0f; NPC.netUpdate = true; }
            }

            // === ESTADO 3: EL DERRAME (la lluvia de ojitos) ===
            if (NPC.ai[0] == 3f)
            {
                NPC.velocity *= 0.9f;
                if (NPC.ai[1] == 30f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int tipo = ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile>();
                        int dano = Math.Max(10, (int)(NPC.damage * 0.5f));
                        int n = Expandido ? 7 : 5;
                        for (int i = 0; i < n; i++)
                        {
                            float dx = (i - (n - 1) * 0.5f) * 60f;
                            Vector2 pos = presa.Center + new Vector2(dx, -420f);
                            Vector2 vel = new Vector2(dx * 0.004f, 6.2f);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, vel, tipo,
                                dano, 2f, Main.myPlayer, 2f, 11f, 0f);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.6f), NPC.Center);
                }
                if (NPC.ai[1] >= 70f) { NPC.ai[0] = 1f; NPC.ai[1] = 0f; NPC.netUpdate = true; }
            }
        }

        /// <summary>LA POSICIÓN DEL OJO (la cabeza del sprite escalado).</summary>
        private Vector2 PosicionDelOjo()
            => NPC.Center - new Vector2(0f, NPC.height * 0.36f * NPC.scale) + new Vector2(NPC.direction * 1f, 0f);

        public override bool? CanBeHitByProjectile(Projectile projectile) => EstadoVulnerable();
        public override bool? CanBeHitByItem(Player player, Item item) => EstadoVulnerable();
        private bool? EstadoVulnerable() => NPC.ai[0] == 0f ? (bool?)false : null;

        public override void OnKill()
        {
            // LA CAÍDA: el botín de la proyección (esencias del Eterno + corazones)
            try
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int tipoEsencia = ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                    int n = Main.expertMode ? 6 : 4;
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, tipoEsencia, n);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, ItemID.Heart, 3);
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.NPCDeath6.WithPitchOffset(0.2f), NPC.Center);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("CiclopeProyectado", exRegistro); }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                float tiempo = Main.GlobalTimeWrappedHourly;
                int seed = 311 + NPC.whoAmI * 19;

                // ============================================================
                //  EL HAZ DE NACIMIENTO (estado 0 — el vídeo: el cono
                //  violeta→magenta que «se expande hacia tu esencia»)
                // ============================================================
                if (NPC.ai[0] == 0f)
                {
                    float f = MathHelper.Clamp(NPC.ai[1] / T_NACER, 0f, 1f);
                    Vector2 baseHaz = new Vector2(NPC.Center.X, NPC.Center.Y + 260f);
                    Vector2 punta = NPC.Center;
                    // EL CONO: dos capas de glow (violeta ancho + magenta íntimo)
                    for (int k = 0; k < 2; k++)
                    {
                        float ancho = k == 0 ? 90f : 46f;
                        Vector2 mid = (baseHaz + punta) * 0.5f;
                        float largo = Vector2.Distance(baseHaz, punta);
                        Vector2 posP = mid - Main.screenPosition;
                        spriteBatch.Draw(VFXCore.SoftGlow, posP, null,
                            (k == 0 ? new Color(150, 70, 220) : new Color(255, 90, 200)) *
                            (0.35f * (0.4f + 0.6f * f)), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(ancho / 64f, largo / 64f), SpriteEffects.None, 0f);
                    }
                    // LAS PARTÍCULAS del haz (deterministas)
                    for (int k = 0; k < 8; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 0.7f + k * 0.125f);
                        Vector2 pos = Vector2.Lerp(baseHaz, punta, prog * prog)
                            + new Vector2(MathF.Sin(k * 2.7f + tiempo * 2f) * 26f, 0f);
                        SombrasLib.Bruma(pos, 16f + 10f * SombrasLib.Frac(k * 0.71f),
                            0.4f * f, tiempo, seed + k * 9, new Vector2(0f, -34f));
                    }
                    // EL SUELLO: el aro de invocación
                    SombrasLib.OndaChoque(new Vector2(baseHaz.X, baseHaz.Y - 6f),
                        30f + 120f * f, 0.5f * f);
                }

                // ============================================================
                //  EL RAYO DEL OJO (estado 2 — el vídeo: el haz magenta)
                // ============================================================
                if (NPC.ai[0] == 2f && NPC.ai[1] >= 40f)
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active)
                    {
                        Vector2 origen = PosicionDelOjo() - Main.screenPosition;
                        Vector2 fin = presa.Center - Main.screenPosition;
                        float latido = 0.75f + 0.25f * MathF.Sin(tiempo * 18f);
                        StormLib.Bolt(spriteBatch, origen, fin,
                            seed + 7, 5, 9f * latido,
                            new Color(180, 60, 160), new Color(255, 130, 230),
                            0.9f, 8, 26f);
                        StormLib.Bolt(spriteBatch, origen, fin,
                            seed + 13, 6, 4f,
                            new Color(255, 220, 250), new Color(255, 255, 255),
                            0.65f, 6, 12f);
                    }
                }

                // ============================================================
                //  EL OJO VIVO (siempre): el ojo del sprite ES chico — el
                //  Cíclope del vídeo lleva el ojo GIGANTE: SombrasLib.Ojo
                //  encima de la cabeza, MIRANDO a la presa, con su parpadeo
                //  y su grito (en telegraph CRECE).
                // ============================================================
                {
                    Player presa = Main.player[NPC.target];
                    Vector2 posOjo = PosicionDelOjo() - Main.screenPosition;
                    Vector2 mirada = presa != null && presa.active
                        ? (presa.Center - PosicionDelOjo()).SafeNormalize(Vector2.UnitX)
                        : Vector2.UnitX;
                    // el parpadeo (abierto 0.9, cerrado un instante cada ~4 s)
                    float abre = SombrasLib.Frac(tiempo * 0.25f) < 0.94f ? 1f : 0.15f;
                    // EL GRITO del telegraph: el ojo crece
                    float tamano = 13f * NPC.scale;
                    if (NPC.ai[0] == 2f && NPC.ai[1] < 40f)
                        tamano *= 1f + 0.9f * MathHelper.Clamp(NPC.ai[1] / 40f, 0f, 1f);
                    VFXCore.AbrirAdditive();
                    try { SombrasLib.Ojo(posOjo, tamano, mirada, abre); }
                    finally { VFXCore.CerrarLoteSiAbierto(); }
                }

                // ============================================================
                //  EL AURA DE LA PROYECCIÓN (la envoltura magenta: respira
                //  alrededor del cuerpo — la casa de glow)
                // ============================================================
                {
                    Vector2 pos = NPC.Center - Main.screenPosition;
                    float respira = 1f + 0.08f * MathF.Sin(tiempo * 2.4f);
                    spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                        new Color(255, 120, 215, 255) * (Expandido ? 0.16f : 0.11f), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(1.4f * NPC.scale * respira, 1.9f * NPC.scale * respira),
                        SpriteEffects.None, 0f);
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("CiclopeProyectado", exRegistro); }
            return true;   // el sprite del cuerpo se dibuja con su luz normal
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // LA POLVORA del golpe: virutas pálidas (la piel del maniquí)
            if (Main.netMode != NetmodeID.Server && hit.Damage > 0)
            {
                int n = Math.Min(14, hit.Damage / 8 + 3);
                for (int i = 0; i < n; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone,
                        hit.HitDirection * 1.2f, -0.8f, 120, new Color(220, 210, 190), 0.8f);
            }
        }

        public override bool CheckActive() => NPC.ai[0] != 0f;   // naciendo JAMÁS despawnea
    }
}
