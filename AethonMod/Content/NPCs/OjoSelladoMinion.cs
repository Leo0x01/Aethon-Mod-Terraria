using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  OJOSELLADOMINION — v6.184 — EL OJO DEL DIOS SELLADO, MINION DE
    //  VERDAD (la letra: «cuando haga un ataque en donde use un ojo,
    //  este ojo debe ser un minion con su animacion completa de
    //  parpadeo y el iris»).
    //
    //  LA SEPARACIÓN DE LÓGICA (la letra: «Aethon sellado tienes que
    //  programarlo y separar toda su logica del grimorio eterno… son
    //  logicas separadas»): el coro del Sellado ya NO dispara el
    //  ARSENAL PRESTADO del jefe Recuerdo (AtaqueJefeProjectile) — el
    //  ataque del ojo es UN MINION NPC PROPIO del dios: los mismos
    //  ojos del escuadrón (OjoEterno: abierto/medio/cerrado + el iris
    //  del grimorio que MIRA) con SU animación completa:
    //  · EL PARPADEO a su propio compás (abierto → medio → cerrado →
    //    medio → abierto, como la corona del dios);
    //  · EL IRIS clavado en la presa (el viaje por la almendra del
    //    ojo — la firma del escuadrón);
    //  · LA CAZA: flota, acecha en órbita viva y ZUMBA en picado
    //    contra la presa; muere sola (vida contada — R23 para NPCs:
    //    12 s de caza y se disuelve en tinta).
    //
    //  HOSTIL de nacimiento (la lógica del JEFE — jamás amistoso con
    //  su propio dios). Nace del coro del dios (DispararUnOjo) —
    //  máximo CUATRO vivos por dios (el rebaño de la corona).
    // ======================================================================
    public class OjoSelladoMinion : ModNPC
    {
        private const float ESCALA = 0.62f;        // el tamaño del minion (más grande que la corona: es un SOLDADO)
        private const int VIDA_TICKS = 720;         // 12 s de caza (R23 — la vida contada)
        private const float VUELO = 7.2f;

        private int Edad => (int)NPC.ai[1];
        private int Dueño => (int)NPC.ai[0];        // whoAmI del dios que lo parió

        // LA TEXTURA del NPC: el ojo del escuadrón (vive en el .tmod — el
        // dibujo COMPLETO lo hace PreDraw con su parpadeo y su iris; sin
        // está el guardián de texturas: un ModNPC sin png es el crash de
        // la v6.180)
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 30;
            NPC.lifeMax = 260;          // se mata a golpes — o muere sola
            NPC.damage = 62;
            NPC.defense = 18;
            NPC.knockBackResist = 0.15f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 0.5f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.netAlways = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
        {
            entry.Info.AddRange(new IBestiaryInfoElement[] {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.OjoSelladoMinion.Bestiario"),
            });
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
            => projectile.hostile ? (bool?)false : base.CanBeHitByProjectile(projectile);

        public override void AI()
        {
            try
            {
                NPC.ai[1]++;
                NPC.TargetClosest(true);
                Player presa = Main.player[NPC.target];
                if (presa == null || !presa.active || presa.dead)
                { MorirEnTinta(); return; }

                // LA VIDA CONTADA (R23): 12 s de caza y se disuelve
                if (Edad > VIDA_TICKS) { MorirEnTinta(); return; }

                // LA DANZA DE LA CAZA (dos tiempos):
                //  0-90 ACECHO: órbita viva alrededor de la presa
                //  91-130 ZUMBIDO: el picado contra ella
                int compas = Edad % 150;
                float t = Main.GlobalTimeWrappedHourly;
                if (compas < 90)
                {
                    float ang = t * 1.5f + NPC.whoAmI * 1.7f;
                    Vector2 órbita = presa.Center + ang.ToRotationVector2() * 240f
                        + new Vector2(0f, MathF.Sin(t * 2.1f + NPC.whoAmI) * 26f);
                    Vector2 dir = órbita - NPC.Center;
                    NPC.velocity = Vector2.Lerp(NPC.velocity,
                        dir.SafeNormalize(Vector2.UnitX) * VUELO, 0.06f);
                }
                else if (compas == 90)
                {
                    // EL TELEGRAFO (el iris se CLAVA: un latido y arranca)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.45f).WithVolumeScale(0.5f), NPC.Center);
                }
                else
                {
                    Vector2 rumbo = presa.Center - NPC.Center;
                    if (rumbo.LengthSquared() > 4f)
                        NPC.velocity = Vector2.Lerp(NPC.velocity,
                            rumbo.SafeNormalize(Vector2.UnitX) * (VUELO * 1.9f), 0.14f);
                }

                // LA APARIENCIA VIVA (el ojo respira y ARDE en el picado)
                NPC.rotation = NPC.velocity.X * 0.012f;
                NPC.netUpdate = Edad % 40 == 0;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("OjoSelladoMinion", exRegistro); }
        }

        /// <summary>LA DESPEDIDA: el ojo se disuelve en tinta (sin gore —
        /// es luz del dios, no carne).</summary>
        private void MorirEnTinta()
        {
            if (!Main.dedServ)
            {
                for (int k = 0; k < 8; k++)
                {
                    var d = Dust.NewDustPerfect(NPC.Center, DustID.Shadowflame,
                        new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-1.6f, 0.4f)) * 1.6f,
                        130, new Color(120, 40, 180), 0.85f);
                    d.noGravity = true;
                }
            }
            NPC.life = 0;
            NPC.active = false;
            NPC.netUpdate = true;
        }

        public override bool CheckActive() => false;   // la vida la cuenta su reloj

        public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 po = NPC.Center - screenPos;
                float t = Main.GlobalTimeWrappedHourly;

                // ══ SU PARPADEO COMPLETO (el de la corona del dios: cada
                //    ojo a SU compás — abierto → medio → cerrado → medio)
                int fase = ((int)(t * 60f) + NPC.whoAmI * 41) % 170;
                Texture2D texOjo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                if (fase > 158) texOjo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
                else if (fase > 150) texOjo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Medio").Value;

                float escViva = ESCALA * (1f + 0.07f * MathF.Sin(t * 3.4f + NPC.whoAmI));
                sb.Draw(texOjo, po, null,
                    Color.White * NPC.Opacity, NPC.rotation * 0.5f,
                    texOjo.Size() * 0.5f, escViva, SpriteEffects.None, 0f);

                // ══ EL IRIS CLAVADO (la firma del escuadrón: viaja por la
                //    almendra ±7,5 · ±2,5 — MIRA a la presa de verdad)
                Player presa = Main.player[NPC.target];
                if (texOjo != null && presa != null && presa.active && fase <= 150)
                {
                    Texture2D iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    float ang = (presa.Center - screenPos - po).ToRotation();
                    Vector2 mirada = new(MathF.Cos(ang) * 7.5f, MathF.Sin(ang) * 2.5f);
                    Vector2 posIris = po + (new Vector2(0.7f, 2.3f) + mirada) * escViva;
                    sb.Draw(iris, posIris, null,
                        Color.White * NPC.Opacity, 0f,
                        iris.Size() * 0.5f, 0.41f * escViva, SpriteEffects.None, 0f);
                }

                // ══ EL RASTRO DEL PICADO (el zumbo deja filos de tinta)
                if (NPC.velocity.LengthSquared() > 80f)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Vector2 cola = po - Vector2.Normalize(NPC.velocity) * 26f;
                        VFXCore.Quad(cola + screenPos,
                            new Color(216, 148, 255) * 0.35f,
                            new Vector2(30f, 3.2f), NPC.velocity.ToRotation());
                    }
                    finally { VFXCore.ReabrirLoteVanilla(); }
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("OjoSelladoMinion.Draw", exRegistro); }
            return false;   // el dibujo es TODO nuestro
        }
    }
}
