using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Skills;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Escriba;

namespace AethonMod.Content.Projectiles.Escriba
{
    /// <summary>
    /// TINTABOLT — v6.52.0 — LA PALABRA DISPARADA: el proyectil de la
    /// Pluma Consciente (un trazo de tinta viva). Lleva DOS banderas al
    /// nacer (ai[0] = busca presa, ai[1] = estela de tinta) según las
    /// inscripciones equipadas, y AQUÍ viven los efectos de golpe:
    ///
    ///   · EL PUNTO FINAL (ejecución): lo que está a un 10% de vida
    ///     recibe ×2.5 — el remate del colofón.
    ///   · LA FIRMA (robo de vida): cada golpe devuelve un 4% del daño.
    ///   · EL HECHO ManoFirme avanza con cada golpe a un jefe.
    /// </summary>
    public class TintaBolt : ModProjectile
    {
        public bool BuscaPresa => Projectile.ai[0] >= 0.5f;
        public bool EstelaTinta => Projectile.ai[1] >= 0.5f;

        public override void SetStaticDefaults()
        {
            // DisplayName: Projectiles.TintaBolt.
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;          // + PerforacionExtra del perfil
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            // LA VELETA: el trazo de tenta ondea al volar (una vuelta
            // completa cada 20 t — determinista, para todos igual).
            Projectile.rotation = Projectile.velocity.ToRotation();

            // EL BUSCAPRESAS (Verso Ciego / Tinta Madre): giro SUAVE hacia
            // el enemigo más cercano en 500 px (fuerza 0.055 — persigue,
            // no imanta).
            if (BuscaPresa)
            {
                NPC presa = null;
                float mejor = 500f * 500f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                    if (d < mejor) { mejor = d; presa = n; }
                }
                if (presa != null)
                {
                    Vector2 hacia = Vector2.Normalize(presa.Center - Projectile.Center);
                    if (float.IsNaN(hacia.X) || float.IsNaN(hacia.Y) || hacia == Vector2.Zero)
                        hacia = -Vector2.UnitY;
                    Projectile.velocity = Vector2.Normalize(
                        Vector2.Lerp(Projectile.velocity, hacia, 0.055f)) *
                        Projectile.velocity.Length();
                }
            }

            // LA LUZ DEL TRAZO (violeta pálido).
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.30f, 0.22f, 0.48f);

            // EL POLVO DE TINTA (escaso: 1 de cada 6 t).
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(6))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.ShadowbeamStaff, -Projectile.velocity * 0.1f, 120,
                    new Color(150, 110, 210), 0.8f);
                d.noGravity = true;
            }
        }

        // === LOS EFECTOS DEL PERFIL EN EL GOLPE ===

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // EL PUNTO FINAL: ejecución contra heridas de muerte (≤10%).
            PerfilInscripcion perfil = PerfilDelDueño();
            if (perfil.Ejecutar && target.life < target.lifeMax * 0.10f)
                modifiers.FinalDamage *= 2.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player dueño = Main.player[Projectile.owner];
            PerfilInscripcion perfil = PerfilDelDueño();

            // LA FIRMA: la sangre vuelve al que firma.
            if (perfil.RoboVida && damageDone > 0 && dueño != null && dueño.active &&
                Projectile.owner == Main.myPlayer)
                dueño.Heal((int)MathF.Max(1f, damageDone * 0.04f));

            // EL HECHO ManoFirme (25 golpes al mismo jefe).
            if (dueño != null && dueño.active && target != null)
                InscripcionJugador.De(dueño)?.RegistrarGolpePluma(target);
        }

        /// <summary>El perfil de las inscripciones según el ítem en la mano del dueño.</summary>
        private PerfilInscripcion PerfilDelDueño()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active || dueño.HeldItem == null || dueño.HeldItem.IsAir)
                return PerfilInscripcion.Neutral();
            if (dueño.HeldItem.type != ModContent.ItemType<PlumaConsciente>())
                return PerfilInscripcion.Neutral();
            return InscripcionDefinicion.Resolver(dueño.HeldItem);
        }

        // === EL DIBUJO: puro VFXCore (la casa) — core + halo + la
        // ESTELA de tinta cuando RimaInterna vive en la pluma ===
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            float tiempo = Main.GlobalTimeWrappedHourly;
            Color tinta = new Color(178, 132, 255);
            Color blanca = new Color(255, 250, 245);

            // LA ESTELA DE TINTA (Rima Interna): una cintita corta que
            // sigue la velocidad — la rima que corre por dentro del trazo.
            if (EstelaTinta && VFXCore.Presupuesto(8))
            {
                Vector2 cola = Projectile.Center - Projectile.velocity * 4.2f;
                Vector2[] espina = { cola, Projectile.Center };
                float[] anchos = { 2f, 8f };
                VFXCore.Ribbon(espina, anchos, StormLib.Tint(tinta, 0.45f),
                    VFXCore.SoftGlow, tiempo * 1.2f);
            }

            // EL CUERPO DEL TRAZO: halo + core (el doble pase de siempre).
            VFXCore.Quad(Projectile.Center, StormLib.Tint(tinta, 0.55f),
                new Vector2(22f, 22f), VFXCore.SoftGlow);
            VFXCore.Quad(Projectile.Center, StormLib.Tint(blanca, 0.85f),
                new Vector2(9f, 9f), VFXCore.GlowOrb);

            // EL RASGO: la punta de la pluma por donde sangra la tinta.
            VFXCore.Quad(Projectile.Center + Projectile.velocity,
                StormLib.Tint(blanca, 0.5f),
                new Vector2(5f, 5f), Projectile.rotation, VFXCore.GlowOrb);

            // EL VOLCADO ÚNICO (EL CONTRATO).
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // EL SALPICÓN: la tinta muere salpicando (6 gotas).
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.ShadowbeamStaff,
                    new Vector2(0, -1).RotatedByRandom(MathHelper.TwoPi) *
                        Main.rand.NextFloat(1.5f, 4f),
                    120, new Color(150, 110, 210), 1.1f);
                d.noGravity = true;
            }
        }
    }
}
