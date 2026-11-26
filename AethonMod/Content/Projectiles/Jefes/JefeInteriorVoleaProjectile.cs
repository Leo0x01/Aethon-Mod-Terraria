using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Jefes
{
    /// <summary>
    /// JEF EINTERIORVOLEAPROJECTILE — v6.175 — LOS NUEVOS PROYECTILES DE LA
    /// ARENA (la letra: jefes del mundo interior «con nuevos ataques y
    /// combos de movimientos, con dash, nuevos proyectiles basados en el
    /// tipo de jefe, invocaciones de minions, mas vida y una mejor IA de
    /// combate e incluso con nuevos ataques mejorados con muchos efectos
    /// visuales»).
    ///
    /// LA FAMILIA TEMÁTICA: cada jefe dispara SU color y SU patrón —
    /// el Rey Gelatina escupe bolas de gel, los mecánicos agujas de
    /// plasma, la Emperatriz esencia arcoíris, el Señor de la Luna ojos
    /// de vórtice. TODO dibujado con los pinceles de la casa (glow +
    /// estela + filo de rayo para los mecánicos), NADA de sprites
    /// ajenos: es una criatura del grimorio.
    ///
    ///  ai[0] = PATRÓN (0 apuntado · 1 anillo · 2 lluvia · 3 espiral)
    ///  ai[1] = TEMA (índice de la tabla de abajo)
    ///  ai[2] = tick de vida
    ///  localAI[0] = semilla determinista (whoAmI del padre × 31)
    /// </summary>
    public class JefeInteriorVoleaProjectile : ModProjectile
    {
        // === LA TABLA DE TEMAS (color núcleo · halo · ¿filo de rayo?) ===
        private static readonly (Color nucleo, Color halo, bool rayo)[] Temas =
        {
            (new Color(148, 72, 178), new Color(80, 30, 120), false), // 0 violeta de la casa (default)
            (new Color(90, 160, 255), new Color(40, 60, 160), false), // 1 gel del Rey (azul viscoso)
            (new Color(255, 60, 60), new Color(140, 10, 20), false),  // 2 ojo (rojo)
            (new Color(190, 90, 255), new Color(90, 20, 120), false), // 3 gusano (corrupto)
            (new Color(255, 170, 40), new Color(150, 70, 10), false), // 4 abeja (ámbar)
            (new Color(235, 235, 220), new Color(120, 110, 90), false),// 5 hueso (Skeletron)
            (new Color(255, 120, 40), new Color(160, 40, 10), false), // 7 infierno (Muralla)
            (new Color(80, 230, 255), new Color(20, 90, 160), true),  // 7 MECÁNICO (plasma — filo de rayo)
            (new Color(120, 255, 120), new Color(20, 120, 50), false),// 8 selva (Plantera)
            (new Color(255, 200, 80), new Color(170, 100, 20), false),// 9 sol (Gólem)
            (new Color(255, 120, 200), new Color(150, 30, 110), true),// 10 tormenta (Duque — filo)
            (new Color(255, 130, 240), new Color(120, 40, 200), true),// 11 arcoíris vivo (Emperatriz — filo)
            (new Color(160, 110, 255), new Color(60, 30, 140), true), // 12 vórtice (Señor Luna — filo)
        };

        /// <summary>EL TEMA que corresponde a un jefe (por tipo vanilla).</summary>
        public static int TemaDe(int tipoJefe)
        {
            return tipoJefe switch
            {
                NPCID.KingSlime => 1,
                NPCID.EyeofCthulhu => 2,
                NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail => 3,
                NPCID.BrainofCthulhu => 3,
                NPCID.QueenBee => 4,
                NPCID.SkeletronHead => 5,
                NPCID.WallofFlesh => 6,
                NPCID.QueenSlimeBoss or NPCID.Retinazer or NPCID.Spazmatism
                    or NPCID.TheDestroyer or NPCID.SkeletronPrime => 7,
                NPCID.Plantera => 8,
                NPCID.Golem => 9,
                NPCID.DukeFishron => 10,
                NPCID.HallowBoss => 11,
                NPCID.MoonLordCore or NPCID.MoonLordHead or NPCID.MoonLordHand => 12,
                _ => 0,
            };
        }

        /// <summary>EL PATRÓN que corresponde a un jefe (su forma de pelear).</summary>
        public static int PatronDe(int tipoJefe)
        {
            return tipoJefe switch
            {
                // los que ruedan y embisten: ANILLOS desde el cuerpo
                NPCID.KingSlime or NPCID.QueenSlimeBoss or NPCID.Golem => 1,
                // los ojos: ESPIRAL giratoria (el ojo clásico)
                NPCID.EyeofCthulhu or NPCID.Retinazer or NPCID.Spazmatism => 3,
                // la lluvia vertical: abejas, selvas y murallas
                NPCID.QueenBee or NPCID.Plantera or NPCID.WallofFlesh => 2,
                // el resto: abanico apuntado
                _ => 0,
            };
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            int tick = (int)Projectile.ai[2];
            int patron = (int)Projectile.ai[0];
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[0] = 31 + Projectile.whoAmI * 131;   // semilla determinista

            // LA LLUVIA cae; el espiral gira SUAVE (telemetría de la
            // arena, no misiles de precisión: el jugador las esquiva —
            // la puntería nació YA en el disparo, del lado del servidor)
            if (patron == 2)
                Projectile.velocity.Y += 0.16f;
            else if (patron == 3)
                Projectile.velocity = Projectile.velocity.RotatedBy(0.012f);

            // LA MUERTE a distancia (nada vaga por siempre)
            if (tick > 420) Projectile.Kill();

            // POLVO DETERMINISTA (la regla de la casa: cero azar en el dibujo
            // vive FUERA; aquí el motor lo siembra con semilla fija)
            if (Main.netMode != NetmodeID.Server && tick % 5 == 0)
            {
                int t = Dust.NewDust(Projectile.Center + new Vector2(-4f, -4f), 8, 8,
                    DustID.Shadowflame, 0f, 0f, 128, default, 0.7f);
                Main.dust[t].noGravity = true;
                Main.dust[t].fadeIn = 0.3f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // EL CHISPazo del impacto (visual puro, determinista)
            if (Main.netMode != NetmodeID.Server)
            {
                int t = Dust.NewDust(target.Center + new Vector2(-10f, -10f), 20, 20,
                    DustID.Shadowflame, 0f, -1.2f, 128, default, 1.1f);
                Main.dust[t].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            // ai[1] = TEMA (sembrado por el padre — ver TemaDe)
            int tema = Math.Clamp((int)Projectile.ai[1], 0, Temas.Length - 1);
            var (nucleo, halo, rayo) = Temas[tema];

            // v6.176 — EL LOTE DE LA CASA (la cura del crash del
            // client.log: «Draw was called, but Begin has not yet been
            // called» ×42.000 frames — la volea cerraba el lote de
            // vanilla y dibujaba SIN abrir el suyo). Ahora abre SU lote
            // aditivo (R6) y sale con RestaurarLoteVanilla — LA SALIDA
            // correcta de todo PreDraw que dibuja directo.
            VFXCore.AbrirAdditive();
            try
            {
                // LA ESTELA (tres fantasmas del camino recorrido)
                Vector2 pos = Projectile.Center - Main.screenPosition;
                for (int k = 1; k <= 3; k++)
                {
                    Vector2 eco = pos - Projectile.velocity * k * 0.9f;
                    float f = 1f - k * 0.24f;
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, eco, null,
                        Color.Lerp(nucleo, halo, 0.4f) * (0.30f * f), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f, new Vector2(0.55f * f, 0.55f * f),
                        SpriteEffects.None, 0f);
                }

                // EL CUERPO: doble glow (halo ancho + núcleo duro)
                float pulso = 1f + 0.14f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.whoAmI);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, halo * 0.85f, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(1.5f * pulso, 1.5f * pulso),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, nucleo, 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, new Vector2(0.72f, 0.72f),
                    SpriteEffects.None, 0f);

                // EL FILO DE RAYO (los temas de plasma: la aguja viva)
                if (rayo)
                {
                    Vector2 fin = pos - Projectile.velocity * 0.55f;
                    StormLib.Bolt(Main.spriteBatch, fin, pos, (int)(13 + Projectile.whoAmI * 7),
                        2, 3.2f, halo * 0.7f, nucleo, 0.75f, 4, 8f);
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorVolea", exRegistro);
            }
            VFXCore.RestaurarLoteVanilla();   // cierra NUESTRO aditivo y reabre el de vanilla
            return false;
        }
    }
}
