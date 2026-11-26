using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    /// <summary>
    /// PROYECCIONCICLOPERAYO — v6.176 — EL RAYO DEL OJO DEL CÍCLOPE (el
    /// arma de la letra: «lo que se tenia que crear era un arma»).
    ///
    /// LA COREOGRAFÍA (la del vídeo «Me Proyecté», el NPC ya la baila):
    ///   FASE 0 (12 t) — EL TELEGRAPH: el OJO del cíclope se proyecta
    ///     sobre el portador (SombrasLib.Ojo — el ojo vivo que MIRA al
    ///     cursor) y GRITA creciendo (la escala sube, el parpadeo se
    ///     aprieta — el aviso del vídeo).
    ///   FASE 1 — EL RAYO: el doble filamento magenta (StormLib.Bolt ×2
    ///     cruzados, como el rayo del NPC) vuela a 26 px/t con
    ///     penetración infinita y SU estela de proyección (motas
    ///     violetas deterministas — R5, sembradas por whoAmI·edad).
    ///
    ///  ai[0] = fase (0 ojo · 1 rayo) · ai[1] = tick · ai[2] = ángulo del
    ///  disparo (nacido del rumbo de USE — el ojo mira, el rayo va).
    /// </summary>
    public class ProyeccionCiclopeRayo : ModProjectile
    {
        private const float T_GRITO = 12f;

        private int Fase => (int)Projectile.ai[0];
        private int Tick => (int)Projectile.ai[1];

        // LA PALETA DEL VÍDEO (la del NPC): magenta vivo + violeta proyección
        private static readonly Color MagNucleo = new(255, 90, 200);
        private static readonly Color MagHalo = new(150, 40, 160);
        private static readonly Color ViolProy = new(120, 60, 200);

        // LA TEXTURA FANTASMA de los proyectiles 100%-código (el patrón de
        // JefeInteriorVolea): tML exige el asset default aunque el PreDraw
        // jamás lo dibuje — sin override, MissingResourceException y TODO el
        // mod se desactiva (la lección v6.50.3).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;       // muerde desde SU nacimiento: el grito no dispara, el rayo sí
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            int tick = Tick;
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active) { Projectile.Kill(); return; }

            // === FASE 0 — EL GRITO (el telegraph del vídeo) ===
            if (Fase == 0)
            {
                if (Projectile.localAI[0] == 0f)
                {
                    // el rumbo del disparo nace del USE (la velocidad que el
                    // ítem ya puso — el ojo la guarda para el rayo)
                    Projectile.localAI[0] = 1f;
                    if (Projectile.velocity.LengthSquared() > 0.25f)
                        Projectile.ai[2] = Projectile.velocity.ToRotation();
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = false;   // el grito no muerde
                }
                // EL OJO VIVE SOBRE EL PORTADOR (la proyección)
                Projectile.Center = dueño.Center + new Vector2(0f, -58f);
                if (tick == (int)T_GRITO)
                {
                    Projectile.ai[0] = 1f;
                    // el rayo nace del rumbo que el ojo guardó del USE
                    Projectile.velocity = new Vector2(MathF.Cos(Projectile.ai[2]), MathF.Sin(Projectile.ai[2])) * 26f;
                    Projectile.friendly = true;
                    Projectile.netUpdate = true;
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.55f).WithVolumeScale(0.75f), Projectile.Center);
                }
                return;
            }

            // === FASE 1 — EL RAYO (vuela derecho, muere a lo lejos) ===
            if (tick > 160) Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL CHISPazo del impacto (visual puro, determinista — R5)
            if (Main.netMode != NetmodeID.Server)
                for (int i = 0; i < 6; i++)
                {
                    float ang = VFXCore.Hash01(Projectile.whoAmI * 31 + i, 1, 811) * MathF.PI * 2f;
                    Dust d = Dust.NewDustPerfect(target.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 8f,
                        DustID.Shadowflame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (1.4f + VFXCore.Hash01(Projectile.whoAmI + i, 2, 813)),
                        128, MagNucleo, 0.9f);
                    d.noGravity = true;
                }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 37 + 5;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Player dueño = Main.player[Projectile.owner];

            VFXCore.AbrirAdditive();
            try
            {
                if (Fase == 0)
                {
                    // ========================================================
                    //  EL OJO QUE GRITA: crece con el tick (4→16 px), MIRA al
                    //  cursor y LATE apretado (el grito del vídeo) — con SU
                    //  halo de proyección violeta y el temblor del aviso.
                    // ========================================================
                    float crece = MathHelper.Clamp(Tick / T_GRITO, 0f, 1f);
                    float tam = MathHelper.Lerp(5f, 16f, crece) * (1f + 0.12f * MathF.Sin(tiempo * 22f));
                    Vector2 temblor = new Vector2(
                        (VFXCore.Hash01(semilla + Tick, 1, 817) - 0.5f) * 3f,
                        (VFXCore.Hash01(semilla + Tick, 2, 819) - 0.5f) * 3f) * crece;
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pos + temblor, null,
                        ViolProy * (0.35f + 0.30f * crece), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, new Vector2(1.4f, 1.4f) * (0.6f + 0.8f * crece),
                        SpriteEffects.None, 0f);
                    // el ojo VIVO (coordenadas de MUNDO — SombrasLib las
                    // resta; SOLO AÑADE CUADROS al lote del llamador: hay
                    // que volcarlos — el contrato de Ojo de la casa)
                    SombrasLib.Ojo(Projectile.Center + temblor, tam,
                        Main.MouseWorld - Projectile.Center,
                        0.55f + 0.45f * MathF.Abs(MathF.Sin(tiempo * 9f)));
                    VFXCore.FlushAdditive();   // EL VUELO del ojo (su propio lote aditivo)
                }
                else
                {
                    // ========================================================
                    //  EL DOBLE RAYO MAGENTA (el del vídeo — dos filamentos
                    //  cruzados como el NPC) + SU estela de proyección.
                    // ========================================================
                    Vector2 punta = pos;
                    Vector2 cola = pos - Projectile.velocity * 0.85f;
                    StormLib.Bolt(Main.spriteBatch, cola, punta,
                        semilla + Tick % 7, 2, 3.4f, MagHalo, MagNucleo, 0.85f, 6, 16f);
                    Vector2 cruz = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X) *
                        (0.14f + 0.06f * MathF.Sin(tiempo * 15f));
                    StormLib.Bolt(Main.spriteBatch, cola + cruz, punta - cruz,
                        semilla + 91 + Tick % 5, 3, 2.6f, MagHalo * 0.8f, MagNucleo * 0.9f, 0.60f, 5, 12f);
                    // EL NÚCLEO (la vena del rayo)
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, punta, null,
                        MagNucleo * 0.9f, 0f, VFXCore.SoftGlow.Size() * 0.5f, new Vector2(0.9f, 0.9f),
                        SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, cola, null,
                        ViolProy * 0.5f, 0f, VFXCore.SoftGlow.Size() * 0.5f, new Vector2(1.3f, 1.3f),
                        SpriteEffects.None, 0f);
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("ProyeccionCiclopeRayo", exRegistro);
            }
            VFXCore.RestaurarLoteVanilla();
            return false;
        }
    }
}
