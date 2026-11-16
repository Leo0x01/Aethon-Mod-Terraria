using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Sombras
{
    // ==================================================================
    //  SombraDeTinta — v6.145 — LA FUSIÓN (la letra: «crea la siguiente
    //  arma, sombra de tinta, donde uses la sombra de la pagina
    //  original, y a eso tienes que agregar tambien el ataque de
    //  Muñeca Maldita, es una fusion no crear un proyectil nuevo»).
    //
    //  LA FUSIÓN DE VERDAD: EL MISMO PROYECTIL de la Sombra de la
    //  Página (SombraPaginaProjectile — NADA nuevo) con SU modo
    //  MUÑECA MALDITA: tras el MORDISCO, la sombra NO se disipa —
    //  REBOTA (el bumerán maldito del hex doll: 5 rebotes, cada uno
    //  ×1,12 más rápido) antes de volver a casa. El arma porta el
    //  tono de tinta: la masa más oscura, la bruma más densa.
    // ==================================================================
    public class SombraDeTinta : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Sombras/SombraDeLaPagina";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Sombras.SombraPaginaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // la ley de la casa: UNA fauce viva por portador
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Sombras.SombraPaginaProjectile>() &&
                    p.ai[0] == 1f)   // modo tinta (véase el Shoot)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA PRESA (la regla de la página 1) + EL MODO TINTA:
            // ai[2] = 1 enciende los REBOTES de la muñeca en el
            // proyectil de siempre — la fusión es un PARÁMETRO, no un
            // proyectil nuevo (la letra lo manda).
            NPC presa = null;
            float mejor = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = n.Center.DistanceSQ(player.Center);
                if (d < mejor) { mejor = d; presa = n; }
            }
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            int idx = Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f,
                type, damage, knockback, player.whoAmI,
                presa != null ? presa.whoAmI + 1 : 0, 0f);
            if (idx >= 0 && idx < Main.maxProjectiles)
                Main.projectile[idx].ai[0] = 1f;   // MODO TINTA: rebotes de la muñeca
            return false;
        }
    }
}
