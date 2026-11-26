using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Regla
{
    // =====================================================================
    //  LaReglaDelCielo — v6.175 — RECREADA DESDE CERO (la letra: «el
    //  tajo es una linea muy buena completamente liza, intenta
    //  investigar como son los tajos de los animes y otros mods y
    //  recrea el arma desde cero con una nueva animacion de tajo
    //  supercompleja»).
    //
    //  EL ARMA NUEVA dispara SAKUGADELCIELO — el tajo de DIECISÉIS
    //  capas del canon investigado (la secuencia de cine: LA
    //  ANTICIPACIÓN con su estrella de hoja y su tensión · EL FRAME
    //  DE IMPACTO monocromo con su panel de tinta y su sacudida · LA
    //  LUNA ÚNICA con DESPLAZAMIENTO CROMÁTICO (las lunas roja y cian
    //  fantasma) y su CORO DE ECO (3 arcos re-revelados) y LAS LÍNEAS
    //  DE VELOCIDAD · EL RASTRO con LA CICATRIZ de tinta que se
    //  desvanece lento y LOS PETALOS). EL CORTE ya no golpea al nacer:
    //  golpea EN EL FRAME DE IMPACTO — la anticipación existe, el
    //  castigo también.
    //
    //  EL SENTIDO DE LA HOJA (la v6.168 vive) y EL CRUZADO cada cuarto
    //  uso (la X: dos lunas cruzadas ±0.42 rad, doble impacto) siguen
    //  siendo la firma del arma.
    //
    //  EL CABLE: el centro del arco ES la posición del proyectil (viaja
    //  solo por la red de vanilla — nada de localAI) · ai[0] = modo ·
    //  ai[1] = inclinación hacia la presa ×1000 · ai[2] = sentido.
    // =====================================================================
    public class LaReglaDelCielo : ModItem
    {
        /// <summary>El radio de la media luna (px).</summary>
        private const float RADIO = 150f;

        /// <summary>EL CONTADOR DEL COMBO (0..3): al llegar a 4 usos el
        /// próximo tajo es EL CRUZADO (la X doble — el golpe ×1.45).</summary>
        private static int _combo;

        public override string Texture => "AethonMod/Content/Weapons/Regla/LaReglaDelCielo";

        public override void SetDefaults()
        {
            Item.damage = 58;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.mana = 6;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Terraria.Item.buyPrice(0, 20, 0, 0);
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Regla.SakugaDelCielo>();
            Item.shootSpeed = 1f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo fuente,
            Vector2 posicion, Vector2 velocidad, int tipo, int dano, float retroceso)
        {
            // ============================================================
            //  LA SIEMBRA DEL CINE: el tajo nace EN EL CURSOR (la posición
            //  del proyectil ES el centro del arco — viaja solo por el
            //  cable de vanilla):
            //   · ai[0] = el MODO (0 normal · 4 cruzado)
            //   · ai[1] = la INCLINACIÓN EXTRA hacia la presa ×1000
            //   · ai[2] = el SENTIDO de la hoja (0 baja · 1 sube)
            // ============================================================
            bool sube = Main.rand.NextBool();
            _combo = (_combo + 1) % 4;
            bool cruzado = _combo == 0;
            // LA ABRAZADA A LA PRESA: un blanco cerca del cursor
            // inclina el arco hacia él (±0.5 rad máximo) — en ai[1] ×1000
            float inclinacionExtra = 0f;
            NPC presa = PresaCerca(Main.MouseWorld, RADIO);
            if (presa != null)
            {
                Vector2 rel = presa.Center - Main.MouseWorld;
                if (rel.Length() > 24f)
                    inclinacionExtra = MathHelper.Clamp(
                        MathF.Atan2(rel.Y, rel.X) -
                        (sube ? -MathHelper.PiOver2 : MathHelper.PiOver2),
                        -0.5f, 0.5f);
            }
            int idx = Projectile.NewProjectile(fuente, Main.MouseWorld, Vector2.Zero,
                tipo, dano, 0f, player.whoAmI, cruzado ? 4f : 0f,
                inclinacionExtra * 1000f, sube ? 1f : 0f);
            if (idx >= 0 && idx < Main.maxProjectiles && Main.projectile[idx].active)
                Main.projectile[idx].originalDamage = Item.damage;
            // EL SONIDO DEL DESENVAINAR (el silbido corto de la hoja)
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72.WithPitchOffset(0.55f)
                .WithVolumeScale(0.28f), player.Center);
            return false;
        }

        /// <summary>El enemigo más cercano al punto (dentro del radio
        /// del arco) — para la abrazada de la media luna.</summary>
        private static NPC PresaCerca(Vector2 punto, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.friendly ||
                    n.townNPC || n.immortal || n.dontTakeDamage) continue;
                float d = Vector2.DistanceSquared(n.Center, punto);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 15)
                .AddIngredient(ItemID.FallenStar, 8)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(ModContent.TileType
                    <global::AethonMod.Content.Tiles.AltarOfrendasTile>())
                .Register();
        }
    }
}
