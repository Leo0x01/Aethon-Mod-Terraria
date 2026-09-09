using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// ProjBeamAlt — versión ALTERNATIVA del ProjBeam del remote.
    /// NO toca el original del remote (namespace Weapons.ProjBeam).
    /// Esta versión usa el helper CosmicEffects.SpawnLightBeams.
    /// Dispara Nightglow (#931) con un rayo concentrado.
    /// </summary>
    public class ProjBeamAlt : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 35;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12; // sonido de rayo láser
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow
            Item.shootSpeed = 18f; // más rápido = efecto de rayo
            Item.mana = 6;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Rayo concentrado: línea de partículas desde el jugador hasta el cursor
            Vector2 start = player.Center;
            Vector2 end = Main.MouseWorld;
            Vector2 dir = end - start;
            float distance = dir.Length();
            if (distance > 0.1f) dir /= distance;

            int segments = (int)(distance / 12f);
            for (int i = 0; i < segments; i++)
            {
                Vector2 pos = start + dir * (12f * i);
                Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch,
                    dir * 0.5f,
                    150 - i * 2,
                    new Color(200, 230, 255, 100), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Haz de luz en el punto de impacto
            CosmicEffects.SpawnLightBeams(end, count: 4, length: 60f);
            return true;
        }
    }
}
