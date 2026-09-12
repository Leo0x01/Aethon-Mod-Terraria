using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// LivingCometStaff — EL COMETA ESTELAR (arma invocadora, v5.99).
    ///
    /// Petición del usuario: "usando la segunda imagen como referencia,
    /// crea un minion cosmico con efectos, de la misma forma a como creaste
    /// la medusa" (cuidado: el CosmicOrbMinion existente usa ESA imagen —
    /// este es una criatura ORIGINAL hermana).
    ///
    /// EL MINION ES EL PROYECTIL: un cometa VIVO — núcleo de plasma
    /// blanco-oro con una CORONA DE 8 PUNTAS (la estrella de la referencia)
    /// que gira alrededor, cola de polvo estelar a su estela y chispas
    /// orbitando. Nada NO persigue: ORBITA al jugador en una elipse
    /// excéntrica, y para atacar CAE EN PICADO sobre la víctima como un
    /// meteoro — al rozarla estalla en una pequeña nova (materia estelar
    /// CALIENTE: OnFire, la firma opuesta a la quemadura fría de la medusa)
    /// y vuelve a su órbita. Donde vuela rápido, el fondo se curva un poco
    /// más (lente del pase B: velocidad = momento = curvatura).
    ///
    /// Especificaciones:
    ///   - damage = 38 (DamageClass.Summon)
    ///   - mana = 0, useTime = 36, knockBack 3, minionSlots = 1 (apilable)
    ///     (v6.00 — TODAS las armas cósmicas de prueba son SIN MANA,
    ///     petición del usuario; daño subido 38→46 — mejora de armas nuevas)
    /// </summary>
    public class LivingCometStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 46;   // v6.00 — mejora (antes 38)
            Item.DamageType = DamageClass.Summon;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;      // v6.00 — arma de prueba: SIN MANA
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<StellarCometMinion>();
            Item.shootSpeed = 10f;
            // (el buff lo aplica Shoot() manualmente — patrón del arsenal)
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el cometa nace en su APOAPSIS (lejos, arriba) y cae a la órbita
            player.AddBuff(ModContent.BuffType<StellarCometBuff>(), 2);
            Vector2 spawnPos = player.Center + new Vector2(
                -velocity.SafeNormalize(Vector2.Zero).X * 96f, -84f);
            Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "LC_Title",
                "[c/9FEFFF:═══ EL COMETA ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "LC_Desc",
                "[c/D9F6FF:Invoca un cometa VIVO: núcleo de plasma con una corona de 8 puntas girando y cola de polvo estelar]"));
            tooltips.Add(new TooltipLine(Mod, "LC_Desc2",
                "[c/AAC8FF:No persigue: ORBITA a tu lado en una elipse excéntrica — y para atacar CAE EN PICADO como un meteoro]"));
            tooltips.Add(new TooltipLine(Mod, "LC_Desc3",
                "[c/FFD9A0:Al rozar a su víctima estalla en una pequeña NOVA — materia estelar caliente que INFLAMA]"));
            tooltips.Add(new TooltipLine(Mod, "LC_Desc4",
                "[c/9FEFFF:Invoca varios cometas si tienes espacio de sirvientes]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
