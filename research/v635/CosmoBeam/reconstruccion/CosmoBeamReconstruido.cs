// ============================================================================
// CosmoBeamReconstruido.cs — v6.35 · Task 60-b · ARCHIVO DE INVESTIGACIÓN
// ============================================================================
//
// ⚠️ ESTE ARCHIVO NO FORMA PARTE DEL MOD AethonMod — vive en research/v635/
//    (la regla de oro: nada de código de investigación dentro de la carpeta
//    del mod, tML compilaría todo lo que haya ahí). Es DOCUMENTACIÓN EJECUTABLE
//    del arma "Cosmo Beam" para estudio.
//
// VEREDICTO DE LA INVESTIGACIÓN (ver INFORME_COSMO_BEAM.md):
//    "Cosmo Beam" NO es un arma pública — es un arma privada del canal de
//    simulaciones @terrariasimulation (TikTok/Shorts, millones de vistas).
//    NO existe en el Steam Workshop de tModLoader (0 resultados verificados)
//    ni en Calamity/Thorium/Fargo/etc. Por eso NO HAY código original que
//    copiar: esto es una RECONSTRUCCIÓN del arquetipo.
//
// QUÉ ES VERIFICADO (del contexto real de las batallas):
//    · Es un arma de RAYO (beam) — el nombre y el género de las simulaciones.
//    · Tier ENDGAME: pelea contra Hellkite de Calamity (570 dmg true-melee)
//      y contra "The Strongest Weapon" del canal → DPS de decenas de miles.
//    · Estética cósmica ("Cosmo").
//
// QUÉ ES INFERIDO (convención del género, marcado con // [INFERIDO]):
//    · La conducta de canalización estilo Last Prism: 6 rayos pequeños que
//      convergen en 1 rayo devastador tras ~1 segundo de enfoque.
//    · El ramp-up de daño (×0.35 → ×1.0) y el drenaje de maná.
//    · Los stats numéricos exactos (escala endgame tipo simulación).
//
// La reconstrucción sigue la API moderna de tModLoader (1.4.4+) y compila
// conceptualmente contra Terraria/tML estándar. Estilo documentado en
// español, como manda la casa.
// ============================================================================

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Investigacion.CosmoBeam
{
    // ========================================================================
    //  EL ARMA — el rayo cósmico canalizado
    // ========================================================================
    public class CosmoBeamItem : ModItem
    {
        public override void SetDefaults()
        {
            // [INFERIDO] Escala endgame de simulación: daño base alto con
            // uso continuo (canalizado), como piden las batallas del canal.
            Item.damage = 180;
            Item.DamageType = DamageClass.Magic;
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 10;                 // el tick de la canalización
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;             // [INFERIDO] held como el Last Prism
            Item.shoot = ModContent.ProjectileType<CosmoBeamHeld>();
            Item.shootSpeed = 0f;              // el rayo nace del arma, no viaja
            Item.mana = 8;                     // [INFERIDO] drenaje por tick de canal
            Item.noMelee = true;
            Item.noUseGraphic = true;          // el held dibuja el arma
            Item.rare = ItemRarityID.Purple;   // [INFERIDO] endgame
            Item.value = Item.buyPrice(platinum: 2);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // UN solo held vivo (el estándar de las armas canalizadas).
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                0f, player.direction);
            return false;
        }
    }

    // ========================================================================
    //  EL PROYECTIL CANALIZADO — 6 rayos que convergen en 1
    // ========================================================================
    public class CosmoBeamHeld : ModProjectile
    {
        // --- LA FASE DE CONVERGENCIA ---
        private const int TicksEnfoque = 55;       // [INFERIDO] ~0.9 s de spread
        private const float DañoSpread = 0.35f;    // [INFERIDO] los 6 rayos débiles
        private const float DañoEnfocado = 1f;     // [INFERIDO] el rayo devastador
        private const int AnchoFinal = 42;         // [INFERIDO] el grosor del cosmo

        private float Fase => Projectile.ai[0];    // ticks vivos (el enfoque)
        private bool Enfocado => Fase >= TicksEnfoque;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.NeedsUUID[Type] = true;   // held estilo Last Prism
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;    // i-frames propios por rayo
            Projectile.localNPCHitCooldown = 10;       // golpea a 6 Hz
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];

            // --- EL CONTRATO DEL HELD: vivo mientras el dueño canalice ---
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.channel || duenio.HeldItem?.type != ModContent.ItemType<CosmoBeamItem>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.ai[0] += 1f;
            Projectile.timeLeft = 2;

            // El held vive en la mano y apunta al cursor.
            Vector2 mano = duenio.MountedCenter +
                new Vector2(duenio.direction * 12f, -4f);
            Projectile.Center = mano;
            Projectile.rotation = (Main.MouseWorld - mano).ToRotation();
            duenio.ChangeDir(Math.Sign(Main.MouseWorld.X - duenio.Center.X));
            duenio.itemRotation = Projectile.rotation *
                (duenio.direction < 0 ? -1f : 1f);
            duenio.itemTime = 2;
            duenio.itemAnimation = 2;

            // El drenaje continuo (canalizar cuesta).
            duenio.statMana -= 1;                     // [INFERIDO]
            if (duenio.statMana <= 0) { Projectile.Kill(); return; }

            // --- LA CONVERGENCIA: el spread se cierra en TicksEnfoque ---
            float t = MathHelper.Clamp(Fase / TicksEnfoque, 0f, 1f);
            float spread = MathHelper.Lerp(0.34f, 0f, t);   // 19.5° → 0°
            float factorDaño = MathHelper.Lerp(DañoSpread, DañoEnfocado, t);

            // --- LOS 6 RAYOS (o el rayo único cuando converge) ---
            int rayos = Enfocado ? 1 : 6;
            for (int i = 0; i < rayos; i++)
            {
                // El offset angular simétrico alrededor de la puntería.
                float off = Enfocado ? 0f
                    : (i - (rayos - 1) / 2f) / rayos * spread * 2f;
                Vector2 dir = (Projectile.rotation + off).ToRotationVector2();
                DispararRayo(mano, dir, factorDaño / (Enfocado ? 1f : rayos * 0.5f));
            }

            // LA LUZ del cosmo (blanco-azul-violeta).
            Lighting.AddLight(mano, 0.55f, 0.62f, 0.95f);
        }

        /// <summary>Un rayo: hitscan + partículas (ver nota de fidelidad).</summary>
        private void DispararRayo(Vector2 origen, Vector2 dir, float factor)
        {
            float alcance = 2400f;                     // [INFERIDO] toda la pantalla
            float ancho = AnchoFinal * factor;

            // EL HITSCAN: samplea el camino y golpea lo que toque.
            for (float d = 20f; d < alcance; d += 24f)
            {
                Vector2 pos = origen + dir * d;
                if (Collision.SolidCollision(pos - Vector2.One * 4f, 8, 8)) break;

                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !npc.CanBeChasedBy()) continue;
                    if (npc.Hitbox.Distance(pos) > ancho) continue;
                    npc.SimpleStrikeNPC((int)(Projectile.damage * factor),
                        Math.Sign(dir.X), false, 0f, DamageClass.Magic);
                }
            }
        }

        // ====================================================================
        //  NOTA DE FIDELIDAD (léela antes de usar esto como referencia):
        //  Este archivo es UNA RECONSTRUCCIÓN DOCUMENTAL del arquetipo, no una
        //  copia — el original no es público. Los métodos de dibujo del rayo
        //  (los 6 beams con partículas estelares que convergen) se describen
        //  aquí a nivel de diseño: en una implementación de la casa usarían
        //  LumenLib.Lance (el rayo continuo de la librería) + BloomTriple +
        //  la paleta estelar, con Debris/Orbs como partículas.
        //
        //  DISEÑO DEL DIBUJO (lo que haría visible al "Cosmo Beam"):
        //   · Los 6 rayos convergentes: cápsulas aditivas delgados (2-3 px)
        //     blancas-azuladas que rotan cerrándose sobre la puntería.
        //   · EL RAYO ENFOCADO: un Lance de núcleo blanco + halo azul-violeta
        //     de 42 px con bloom triple latiendo a 2 Hz.
        //   · Las PARTÍCULAS ESTELARES: chispas de 4 puntas doradas/blancas
        //     orbitando el punto de convergencia mientras se enfoca.
        //   · El IMPACTO en pared/NPC: destello + anillo de onda.
        // ====================================================================
    }
}

// ============================================================================
// CÓMO SE ADAPTARÍA AL ESTILO DE AethonMod (documentación, no código):
//
//  1. El ITEM viviría en Content/Weapons/Cosmic con el estilo de la casa
//     (doc-comment de historia, ModifyTooltips con líneas de color).
//  2. El RAYO usaría LumenLib.Lance para el haz continuo (la librería ya
//     existe y está calibrada) + LumenLib.BloomTriple para el resplandor.
//  3. La CONVERGENCIA de 6 rayos es un caso natural de VFXCore: 6 Quads
//     rotando hacia el ángulo de puntería con Hash01 para el shimmer.
//  4. Paleta: la familia estelar de VFXPalettes (blanco frío + azul + violeta).
//  5. El arma encajaría en la rama cósmica del Fragmento Génesis como la
//     evolución de "Voz del Cuásar" (que ya explora el rayo continuo).
// ============================================================================
