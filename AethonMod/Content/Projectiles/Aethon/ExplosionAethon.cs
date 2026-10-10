using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Aethon
{
    /// <summary>
    /// LA APERTURA DE AETHON — v6.50.98 — LA EXPLOSIÓN DEL FRAGMENTO.
    /// Nace en el OnKill del FragmentoDeAethon con el 50% del daño: la
    /// grieta SE ABRE VIOLENTAMENTE (8 frames a 3 ticks — f4 es el
    /// máximo) y después SE SELLA (se queda en el frame 7: la cicatriz
    /// de oro). Daña en área mientras vive (penetrate -1, atraviesa
    /// bloques), NO añade stacks (solo el proyectil principal los
    /// añade) y exhala sus rayos violetas + la onda expansiva dorada.
    ///
    /// Los errores del código del usuario, muertos aquí: GoldFlare →
    /// GoldFlame (lección .96), Kill → OnKill (lección .94 — esta clase
    /// no lo necesitaba, pero la familia sí) y el DisplayName vive en el
    /// hjson (la casa).
    /// </summary>
    public class ExplosionAethon : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 8 frames: se abre violentamente → se sella
            Main.projFrames[Projectile.type] = 8;
            // (El DisplayName vive en el hjson de localización — la casa.)
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;             // hitbox == frame del strip
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;         // atraviesa todos
            Projectile.timeLeft = 30;          // 0.5 segundos
            Projectile.alpha = 0;
            Projectile.light = 1.2f;           // la luz base (blanca)
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;    // atraviesa bloques
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            // --- ANIMACIÓN: más rápida (efecto violento) ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)   // cada 3 ticks
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
                if (Projectile.frame >= 8)
                    Projectile.frame = 7;      // se queda en el último (sellada)
            }

            // --- SE DETIENE completamente (nace donde murió el fragmento) ---
            Projectile.velocity = Vector2.Zero;

            // --- LA LUZ: la apertura brilla ORO y se apaga al sellarse ---
            float vida = Projectile.timeLeft / 30f;    // 1.0 → 0.0
            Lighting.AddLight(Projectile.Center, 0.85f * vida, 0.68f * vida, 0.20f * vida);

            // --- EFECTO: rayos violetas radiales (los primeros ticks) ---
            if (Projectile.timeLeft > 20)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = MathHelper.TwoPi * Main.rand.NextFloat();
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.PurpleTorch,
                        new Vector2((float)System.Math.Cos(angle) * 3f,
                                    (float)System.Math.Sin(angle) * 3f),
                        150, new Color(170, 90, 255), 1.8f
                    );
                    dust.noGravity = true;
                    dust.scale = 1.8f;
                }
            }

            // --- EFECTO: la ONDA EXPANSIVA (círculo dorado completo) ---
            if (Projectile.timeLeft == 24)
            {
                for (int i = 0; i < 36; i++)
                {
                    float angle = MathHelper.TwoPi * i / 36f;
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GoldFlame,      // GoldFlare NO existe — la casa
                        new Vector2((float)System.Math.Cos(angle) * 5f,
                                    (float)System.Math.Sin(angle) * 5f),
                        100, new Color(255, 208, 120), 1.2f
                    );
                    dust.noGravity = true;
                }
            }

            // --- HACIENDO DAÑO EN ÁREA los primeros ticks (la apertura
            //     muerde todo lo que toca mientras está abierta) ---
            if (Projectile.timeLeft > 22)
                Projectile.ai[0] = 1;          // flag de fase de daño activo
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // No añade stacks (solo el proyectil principal los añade) —
            // la letra del usuario: la EXPLOSIÓN es la consecuencia, no
            // la mirada. (La firma es la de ESTA tML: HitInfo + daño
            // hecho — la firma vieja del borrador no compila aquí.)
        }
    }
}
