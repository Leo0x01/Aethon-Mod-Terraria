using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.CodiceVivo
{
    /// <summary>
    /// CODICEVIVOPROJECTILE — v6.50.72 — EL CÓDICE QUE VUELA Y DISPARA (la
    /// cura de «solo es la imagen fija subiendo en vertical sin animaciones
    /// ni nada… no tiene ningún ataque»).
    ///
    /// EL SPRITE (v2 — la animación que SE VE): el strip de 8 frames nacido
    /// de la MATRIZ del usuario (tools/gen_codice_vivo_v65072.py) — EL
    /// ALIENTO (todo el libro respira, onda triangular 0.76..1.24), LA ONDA
    /// (un anillo de tinta violeta-clara que viaja desde el ojo hacia afuera
    /// cada ciclo) y EL PARPADEO COMPLETO (frames 5/6/7: media, RENDIJA,
    /// media). Un frame cada 5 t de juego.
    ///
    /// EL VUELO (tres actos, ANCLADO AL CURSOR): LANZA (vuela al PUNTO donde
    /// clicaste — a 280 px de ti en la dirección de la mira — y frena al
    /// llegar, ya no pasa de largo) → VELA (flota AHÍ, vaivén de levitación,
    /// y cada 26 t escupe una ANDANADA de TRES chispas autoguiadas: al
    /// enemigo más cercano en 900 px… y si NO HAY NADIE, igual dispara hacia
    /// la mira — EL ATAQUE SIEMPRE SE VE) → RECOGE (bumerán acelerando al
    /// portador). Cuatro andanadas por lanzada.
    /// </summary>
    public class CodiceVivoProjectile : ModProjectile
    {
        private const byte FASE_LANZA = 0;
        private const byte FASE_VELA = 1;
        private const byte FASE_RECOGE = 2;

        private const float DIST_VELA = 280f;      // a dónde vuela (de la mira)
        private const int TICKS_ANDANADA = 26;     // una andanada cada…
        private const int ANDANADAS = 4;           // …hasta 4 por lanzada

        public override void SetStaticDefaults()
        {
            // EL STRIP ANIMADO: 8 frames — el libro respira, la tinta ondula
            // y el ojo PARPADEA (frames 5/6/7)
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.extraUpdates = 1;
        }

        /// <summary>El enemigo más cercano en radio (el ojo del códice busca).</summary>
        private static NPC PresaCercana(Vector2 desde, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>EL PUNTO DE VELA: donde el portador apuntó al lanzar.</summary>
        private Vector2 PuntoVela(Player dueño)
        {
            return dueño.MountedCenter + Projectile.ai[0].ToRotationVector2() * DIST_VELA;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte fase = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];

            // === LA ANIMACIÓN DEL SPRITE: un frame cada 5 t de juego (la AI
            // corre ×2 por el extraUpdates — el contador avanza al doble) ===
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 10)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 8;
            }

            // el vaivén del libro flotante (siempre vivo, incluso volando)
            Projectile.rotation = MathF.Sin(t * 0.045f) * 0.12f;

            switch (fase)
            {
                case FASE_LANZA:
                {
                    // vuela AL PUNTO de la mira y FRENA al llegar (de puro
                    // impulso ya no se escapa: persigue el punto, no el rumbo)
                    Vector2 destino = PuntoVela(dueño);
                    Vector2 hacia = destino - Projectile.Center;
                    if (t == 1f)
                        Sonar(SoundID.Item74.WithPitchOffset(-0.15f).WithVolumeScale(0.7f), Projectile.Center);
                    if (hacia.LengthSquared() > 26f * 26f && t <= 90f)
                    {
                        hacia = hacia.SafeNormalize(Vector2.UnitX);
                        Projectile.velocity += hacia * 1.15f;
                        float vel = Projectile.velocity.Length();
                        if (vel > 19f) Projectile.velocity *= 19f / vel;
                        // frena al acercarse (llega SUAVE, no rebota)
                        float cerca = MathHelper.Clamp(hacia.Length() / 120f, 0.25f, 1f);
                        Projectile.velocity *= 0.86f + 0.12f * cerca;
                    }
                    else
                    {
                        Projectile.ai[1] = FASE_VELA;
                        Projectile.ai[2] = 0f;
                    }
                    break;
                }
                case FASE_VELA:
                {
                    // flota ANCLADO al punto de la mira (muelle suave + bob)
                    Vector2 destino = PuntoVela(dueño) +
                        new Vector2(MathF.Sin(t * 0.07f) * 14f, MathF.Sin(t * 0.11f) * 20f);
                    Projectile.velocity += (destino - Projectile.Center) * 0.014f;
                    Projectile.velocity *= 0.90f;

                    // LAS ANDANADAS — cada 26 t, TRES chispas en abanico. CON
                    // presa: hacia ella. SIN presa: hacia la MIRA. Siempre
                    // se ve el ataque (la letra: «no tiene ningún ataque»).
                    if (Main.myPlayer == Projectile.owner && t % TICKS_ANDANADA == 6f)
                    {
                        NPC presa = PresaCercana(Projectile.Center, 900f);
                        float baseAng = presa != null
                            ? (presa.Center - Projectile.Center).ToRotation()
                            : Projectile.ai[0];
                        for (int k = -1; k <= 1; k++)
                        {
                            float ang = baseAng + k * 0.20f;
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                                ang.ToRotationVector2() * 12f, ModContent.ProjectileType<CodiceVivoChispa>(),
                                (int)(Projectile.damage * 0.6f), 2f, Projectile.owner,
                                presa != null ? presa.whoAmI + 1 : 0, 0);
                        }
                        Sonar(SoundID.Item12.WithPitchOffset(0.35f).WithVolumeScale(0.5f), Projectile.Center);
                    }

                    if (t >= (float)(TICKS_ANDANADA * ANDANADAS + 12))
                    {
                        Projectile.ai[1] = FASE_RECOGE;
                        Projectile.ai[2] = 0f;
                        Sonar(SoundID.Item74.WithPitchOffset(0.25f).WithVolumeScale(0.5f), Projectile.Center);
                    }
                    break;
                }
                case FASE_RECOGE:
                {
                    // EL BUMERÁN: acelera hacia su dueño y se guarda al tocarlo
                    Vector2 hacia = (dueño.MountedCenter - Projectile.Center).SafeNormalize(-Vector2.UnitY);
                    Projectile.velocity += hacia * 1.05f;
                    float vel = Projectile.velocity.Length();
                    if (vel > 26f) Projectile.velocity *= 26f / vel;

                    if (Vector2.DistanceSquared(Projectile.Center, dueño.MountedCenter) < 42f * 42f)
                    {
                        Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.35f), Projectile.Center);
                        Projectile.Kill();
                        return;
                    }
                    break;
                }
            }

            // polvillo violeta del vuelo
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(7))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(20, 20), 40, 40,
                    DustID.Shadowflame, 0f, -0.25f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // === EL HALO VIOLETA que respira (el aura del tomo vivo) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta,
                    (0.16f + 0.07f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f))),
                    new Vector2(160f, 160f));
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // === LAS ESTELAS + EL SPRITE (el strip animado: el frame actual) ===
            // v6.50.74 — FIX DEL ATAQUE INVISIBLE («el ataque no se ve y
            // salta un error»): aquí había un spriteBatch.Begin(...) propio
            // — pero el lote de vanilla YA ESTABA ABIERTO (ReabrirLoteVanilla
            // lo dejó listo): Begin sobre Begin → InvalidOperationException
            // TODOS los frames (client.log: «Begin has been called before
            // calling End») → el códice JAMÁS se dibujaba en vuelo. El
            // contrato correcto: tras ReabrirLoteVanilla se dibuja EN el
            // lote vivo (Main.EntitySpriteDraw) — sin tocar Begin/End.
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int alto = tex.Height / Main.projFrames[Projectile.type];
            Rectangle src = new(0, Projectile.frame * alto, tex.Width, alto);
            Vector2 origen = new(tex.Width * 0.5f, alto * 0.5f);
            Vector2 pantalla = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            // DOS ESTELAS del propio sprite (los frames recientes, tenues)
            for (int k = 2; k <= 4; k += 2)
            {
                if (Projectile.oldPos.Length <= k) break;
                Vector2 vieja = Projectile.oldPos[k];
                if (vieja == Vector2.Zero) continue;
                Vector2 posV = vieja + new Vector2(Projectile.width, Projectile.height) * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, posV, src, luz * (0.30f - k * 0.06f),
                    Projectile.rotation * 0.6f, origen, 1f - k * 0.06f, SpriteEffects.None, 0f);
            }
            // EL CÓDICE
            Main.EntitySpriteDraw(tex, pantalla, src, luz,
                Projectile.rotation, origen, 1f, SpriteEffects.None, 0f);
            return false;
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
