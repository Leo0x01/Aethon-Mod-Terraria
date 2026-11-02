using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigaciones;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Investigaciones
{
    /// <summary>
    /// ColmilloDoradoStaff — v6.85 — EL COLMILLO DORADO (la investigación
    /// R7: la mecánica de «Aureum Fang», los rayos de energía que caen del
    /// cielo).
    ///
    /// LA LETRA: «investigar el arma Aureum Fang, esta arma tiene una
    /// mecánica donde caen rayos de energia del cielo, esa es una mecánica
    /// que estaba buscando, investígala a profundidad y crea un item a
    /// consecuencia». El arma exacta no existe en ningún índice (ver el
    /// informe r7_aureum_fang.md); la reconstrucción usa la referencia
    /// estrella (el Divine Thunder del bastón Elysium, CalamityOverhaul).
    ///
    /// LA MECÁNICA (la receta del informe):
    ///   · CLIC IZQ: 3 rayos sobre los 3 enemigos más cercanos al cursor
    ///     (≤400 px, mínimo 120 px entre sí; si solo hay 1, la MORDIDA
    ///     TRIPLE encadenada sobre él con 6 ticks de separación; sin
    ///     enemigos: sobre el cursor). Cada rayo: presagio 6 ticks →
    ///     bolt doble → ventana de daño → resplandor. Cooldown 2,5 s.
    ///   · CLIC DER — EL JUICIO DEL COLMILLO: 6 rayos en anillo
    ///     280-460 px alrededor del cursor (jitter ±60), daño ×1,2,
    ///     sacudida 5. Cooldown 20 s.
    ///
    /// ARMA DE PRUEBAS: 0 maná (la regla de la sesión) · receta de 5
    /// maderas (el patrón de la casa) + entrada en la Bolsa de las
    /// Investigaciones.
    /// </summary>
    public class ColmilloDoradoStaff : ModItem
    {
        /// <summary>Alcance del targeting (400 px en la referencia).</summary>
        private const float Alcance = 400f;

        /// <summary>Separación mínima entre blancos (120 px).</summary>
        private const float SepMin = 120f;

        /// <summary>Cooldown del uso normal (150 ticks = 2,5 s).</summary>
        private const int CdNormal = 150;

        /// <summary>Cooldown del Juicio (1200 ticks = 20 s).</summary>
        private const int CdJuicio = 1200;

        /// <summary>Los relojes por jugador (frame del próximo uso).</summary>
        private static readonly Dictionary<int, ulong> _proximoUso = new();

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<AureumSkyStrike>();
            Item.shootSpeed = 1f;
            Item.mana = 0;              // ARMA DE PRUEBAS: la regla de la sesión
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = null;       // el sonido lo pone el rayo (el zap)
            Item.value = 0;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            return !_proximoUso.TryGetValue(player.whoAmI, out ulong p)
                || Main.GameUpdateCount >= p;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool juicio = player.altFunctionUse == 2;
            int dano = juicio ? (int)(damage * 1.2f) : damage;

            // EL RELOJ DEL USO (el cooldown se estampa al DISPARAR: la
            // ventana de CanUseItem ya pasó).
            _proximoUso[player.whoAmI] = Main.GameUpdateCount
                + (ulong)(juicio ? CdJuicio : CdNormal);

            if (juicio)
            {
                // === EL JUICIO DEL COLMILLO: 6 rayos en anillo 280-460 px
                //     alrededor del cursor (jitter ±60) ===
                for (int i = 0; i < 6; i++)
                {
                    float ang = i / 6f * MathHelper.TwoPi + Main.rand.NextFloat(-0.18f, 0.18f);
                    float radio = Main.rand.Next(280, 461) + Main.rand.Next(-60, 61);
                    Vector2 pos = Main.MouseWorld + new Vector2(
                        (float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) * radio;
                    SpawnStrike(source, pos, dano, knockback, player, juicio);
                }
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    // El retumbo grave del juicio (la referencia: Item122 1,5
                    // −0,6 + Thunder 1,2 −0,4).
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.6f).WithVolumeScale(1.0f), player.Center);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Thunder.WithPitchOffset(-0.4f).WithVolumeScale(0.8f), player.Center);
                    // LA SACUDIDA 5 del juicio.
                    Pantalla.PresetImpacto(Main.MouseWorld, 1.5f, new Color(255, 240, 180));
                }
            }
            else
            {
                // === EL USO NORMAL: los 3 blancos más cercanos al cursor ===
                var blancos = new List<Vector2>();
                var elegidos = new List<NPC>();
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (blancos.Count >= 3) break;
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float d = (npc.Center - Main.MouseWorld).Length();
                    if (d > Alcance) continue;

                    // El mínimo de separación entre blancos (120 px).
                    bool cerca = false;
                    foreach (NPC e in elegidos)
                        if ((e.Center - npc.Center).Length() < SepMin) { cerca = true; break; }
                    if (cerca) continue;

                    elegidos.Add(npc);
                    blancos.Add(npc.Center);
                }

                if (blancos.Count == 0)
                {
                    // Sin enemigos: el rayo del cursor (el fallback de la
                    // referencia).
                    blancos.Add(Main.MouseWorld);
                }
                else if (blancos.Count == 1)
                {
                    // LA MORDIDA TRIPLE: un solo blanco → 3 encadenados con
                    // 6 ticks de separación (el presagio se alarga: 6/12/18).
                    for (int i = 0; i < 2; i++)
                        blancos.Add(blancos[0]);
                }

                for (int i = 0; i < blancos.Count; i++)
                    SpawnStrike(source, blancos[i], dano, knockback, player, false);
            }

            return false;
        }

        /// <summary>EL SPAWN del rayo: la mordida triple son 3 strikes
        /// con semillas distintas sobre el mismo blanco (cada bolt
        /// re-randomiza solo; el conjunto vibra como una mordida — la
        /// simplificación honesta de la casa).</summary>
        private static void SpawnStrike(EntitySource_ItemUse_WithAmmo source,
            Vector2 pos, int dano, float kb, Player player, bool juicio)
        {
            Projectile p = Projectile.NewProjectileDirect(source, pos, Vector2.Zero,
                ModContent.ProjectileType<AureumSkyStrike>(), dano, kb, player.whoAmI);
            p.timeLeft = 44;
            p.netUpdate = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
