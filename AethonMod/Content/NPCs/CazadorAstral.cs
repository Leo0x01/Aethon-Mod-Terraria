using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  CAZADOR ASTRAL (v6.50.45) — LA MANADA DEL JEFE.
    //
    // La petición, LITERAL: «el jefe deberia usar el proyectil de la
    // manada astral, pero con menos vida y mas lentos, pero en mayor
    // numero». Los cazadores del bastón LA MANADA ASTRAL, puestos al
    // servicio de la luz — pero de VERDAD: tML no permite golpear
    // proyectiles hostiles (verificado en el decompile del 2026.07.3.0:
    // ni ModProjectile ni ProjectileLoader exponen CanBeHit), así que
    // la «vida» de la manada es VIDA DE NPC — se MATAN, caen, dejan de
    // existir: MENOS VIDA (frágiles), MÁS LENTOS (la mitad de la
    // velocidad del bastón) y EN MAYOR NÚMERO (camadas de cinco, hasta
    // diez vivos — el bastón tenía seis).
    //
    // LA IA: el steering de la casa (boids-lite) — embiste a su presa
    // con vaivén de caza, se separa de sus hermanos y arrastra la COLA
    // de tres cuentas (la distancia elástica hecha visible — el mismo
    // look del bastón, dibujado por SierpesLib.ManadaAstral).
    //
    // LA CASA: sin luz propia que los mande se APAGAN (el jefe muere →
    // la manada se disuelve), vida corta (24 s) y CheckActive false
    // (son parte del ataque, no población del mundo).
    // ======================================================================
    public class CazadorAstral : ModNPC
    {
        /// <summary>La velocidad MÁXIMA del cazador (la mitad del bastón: 13 → 6.5).</summary>
        private const float VelMax = 6.5f;

        /// <summary>La vida de la camada en segundos (el ataque, no una mascota).</summary>
        private const int VidaTicks = 1440;

        /// <summary>La cuenta atrás silenciosa antes de apagarse.</summary>
        private int _age;

        /// <summary>LA COLA (tres cuentas — la distancia elástica visible).</summary>
        private readonly Vector2[] _cola = new Vector2[3];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El DisplayName vive en la localización (NPCs.CazadorAstral).
        }

        public override void SetDefaults()
        {
            NPC.width = 24;
            NPC.height = 24;
            NPC.lifeMax = 5000;             // MENOS VIDA: dos o tres golpes y cae
            NPC.damage = 70;                // el mordisco de la estrella
            NPC.defense = 10;
            NPC.knockBackResist = 0.4f;     // un golpe lo empuja: se siente vivo
            NPC.noGravity = true;
            NPC.noTileCollide = true;       // la manada vuela POR el mundo
            NPC.aiStyle = -1;
            NPC.npcSlots = 0.3f;            // la camada completa = 3 slots, no 30
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit52;
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.value = 0;                  // son un ataque, no una presa
        }

        public override bool CheckActive() => false;   // los maneja su IA (vida + jefe)

        public override void AI()
        {
            _age++;

            // === LA PRESA (ai[0] = quién era su presa al nacer; el resto
            //     de jugadores valen si aquella murió) ===
            int idxPresa = (int)NPC.ai[0];
            Player presa = (idxPresa >= 0 && idxPresa < Main.maxPlayers)
                ? Main.player[idxPresa] : null;
            if (presa == null || !presa.active || presa.dead)
            {
                NPC.TargetClosest(false);
                presa = Main.player[NPC.target];
                if (presa == null || !presa.active || presa.dead)
                {
                    NPC.active = false;     // sin presa no hay caza
                    NPC.netUpdate = true;
                    return;
                }
            }

            // === SIN LUZ QUE LOS MANDE NO HAY MANADA: si Aethon murió (o
            //     se fue), la camada se APAGA sola (el jefe también la
            //     disuelve al morir — esto cubre los exóticos). ===
            if (_age % 30 == 1 && AethonBoss.ContarCazadores() > 0 && !ViveAethon())
            {
                MorirEnPolvo();
                return;
            }

            // === LA VIDA CORTA (la camada es un ataque: 24 s y a casa) ===
            if (_age > VidaTicks)
            {
                MorirEnPolvo();
                return;
            }

            // === EL STEERING (boids-lite de la casa): embestir con
            //     vaivén — la caza zigzaguea, no persigue en línea ===
            Vector2 deseada = (presa.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * VelMax;
            // EL VAI VÉN: perpendicular senoidal (cada cazador con su fase).
            float fase = _age * 0.045f + NPC.whoAmI * 2.3f;
            Vector2 perp = new Vector2(-deseada.Y, deseada.X) / MathF.Max(VelMax, 0.1f);
            deseada += perp * (MathF.Sin(fase) * 1.8f);

            Vector2 steer = (deseada - NPC.velocity) * 0.07f;

            // LA SEPARACIÓN (nadie se monta encima de su hermano).
            int tipo = Type;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC otro = Main.npc[i];
                if (otro == null || otro == NPC || !otro.active || otro.type != tipo) continue;
                Vector2 d = NPC.Center - otro.Center;
                float len = d.Length();
                if (len > 0.001f && len < 46f)
                    steer += d / len * 0.55f;
            }

            // EL LÍMITE de fuerza (la suavidad de la manada).
            float flen = steer.Length();
            if (flen > 0.5f) steer = steer / flen * 0.5f;
            NPC.velocity += steer;
            float vel = NPC.velocity.Length();
            if (vel > VelMax) NPC.velocity = NPC.velocity / vel * VelMax;
            NPC.rotation = NPC.velocity.ToRotation();

            // === LA COLA (la cadena de cuentas a 11 px — el look del bastón) ===
            Vector2 padre = NPC.Center;
            for (int k = 0; k < 3; k++)
            {
                Vector2 d = _cola[k] - padre;
                float len = d.Length();
                if (len < 0.0001f) { d = -Vector2.UnitX; len = 1f; }
                _cola[k] = padre + d / len * 11f;
                padre = _cola[k];
            }
            if (_age == 1) { _cola[0] = _cola[1] = _cola[2] = NPC.Center; }

            // === LA LUZ AZUL DE LA MANADA (la del bastón) ===
            Lighting.AddLight(NPC.Center, 0.12f, 0.16f, 0.24f);

            // LA RESPIRACIÓN de la posición por el cable.
            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        /// <summary>¿Vive aún Aethon?</summary>
        private static bool ViveAethon()
        {
            int tipo = ModContent.NPCType<AethonBoss>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == tipo) return true;
            }
            return false;
        }

        /// <summary>El apagado silencioso: polvo estelar y fuera.</summary>
        private void MorirEnPolvo()
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-2.5f, 2.5f),
                            Main.rand.NextFloat(-2.5f, 1f)), 170,
                        new Color(150, 200, 255), 0.9f);
                    d.noGravity = true;
                }
            }
            NPC.active = false;
            NPC.netUpdate = true;
        }

        public override void OnKill()
        {
            // La caza muerta: su polvo estelar (la casa: el mundo recuerda).
            if (Main.dedServ) return;
            for (int i = 0; i < 14; i++)
            {
                float ang = i / 14f * MathHelper.TwoPi;
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(1f, 3f), 180,
                    new Color(170, 215, 255), 1.0f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos,
            Color drawColor)
        {
            if (Main.dedServ) return false;

            // EL CONTRATO DE LA CASA (el del proyectil del bastón):
            // cerrar el lote del pase → SierpesLib abre SU aditivo y
            // dibuja al cazador (cuerpo + cola + estelas) → reabrir el
            // lote del pase TAL CUAL.
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // el fade de la vida (los últimos 60 t se apaga).
                float alpha = _age > VidaTicks - 60
                    ? Math.Max(0f, (VidaTicks - _age) / 60f) : 1f;
                Vector2[] cazador = { NPC.Center };
                Vector2[] colas = { _cola[0], _cola[1], _cola[2] };
                float[] rapidez = { MathHelper.Clamp(NPC.velocity.Length() / VelMax, 0f, 1f) };
                SierpesLib.ManadaAstral(cazador, colas, 1, 0, rapidez,
                    Main.GlobalTimeWrappedHourly, NPC.whoAmI * 37 + 5, alpha);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;   // el cazador SE dibuja solo (100% luz)
        }
    }
}
