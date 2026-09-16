using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// PliegueEspacioStaff — EL PLIEGUE DEL ESPACIO (v6.33).
    ///
    /// La referencia del usuario: la APERTURA ESPACIAL — el ojo diagonal
    /// (la doble elipse con cuello), el rim cian y el horizonte ámbar, el
    /// NÚCLEO NEGRO absoluto y LA REJILLA QUE CONVERGE (la señal visual #1
    /// del espacio-tiempo doblado).
    ///
    /// MECÁNICA: el pliegue vive 4 s en el cursor y DOBLA el espacio: los
    /// enemigos en 350 px son ARRASTRADOS hacia el cuello del ojo (succión
    /// FUERTE — el espacio se curva hacia dentro) y al tocar el núcleo
    /// negro son COMPRIMIDOS (daño ×1.3, knockback 0). La inclinación del
    /// ojo apunta al primer enemigo arrastrado.
    /// </summary>
    public class PliegueEspacioStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 118;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 34; Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PliegueEspacioProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 26; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.15f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/00D4FF:EL PLIEGUE DEL ESPACIO]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFB800:Abre el OJO donde apuntas: la doble elipse con el núcleo negro y la rejilla que converge]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/00D4FF:DOBLA el espacio en 350 px: arrastra a los enemigos hacia el cuello del ojo]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El que toca el núcleo negro es COMPRIMIDO ×1.3 · 4 segundos de curvatura]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarPliegueViejo(player);
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: cierra el pliegue anterior del mismo owner.</summary>
        private static void MatarPliegueViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PliegueEspacioProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// HeridaElectricaStaff — LA HERIDA ELÉCTRICA (v6.33).
    ///
    /// La referencia del usuario: el DESGARRO DE REALIDAD ELÉCTRICA — la
    /// grieta lineal dentada cuyo interior lleva ESTÁTICA y scanlines (el
    /// buffer detrás de la realidad), arcos voltaicos corriendo POR DENTRO
    /// (la nueva StormLib.StormArc), strobe nervioso y chispas zig-zag.
    ///
    /// MECÁNICA: abre LA GRIETA desde el jugador HACIA el cursor (como el
    /// Bastón del Desgarro clásico): ~620 px de herida viva 3 s que pica
    /// en LÍNEA (×0.35, i-frames 10) y APLICA ELECTRIFICADO (el debuff
    /// vanilla que castiga el movimiento: 4 HP/s quieto, ~16 moviéndose).
    /// La corriente no se corta: SIEMPRE hay arco visible.
    /// </summary>
    public class HeridaElectricaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 74;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<HeridaElectricaProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 15; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.55f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/E0FFFF:LA HERIDA ELÉCTRICA]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/00FFFF:Abre LA GRIETA hacia donde apuntas: el vacío con estática y arcos voltaicos POR DENTRO]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/8A2BE2:Pica en línea ×0.35 y APLICA ELECTRIFICADO — el debuff que castiga el movimiento]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:La corriente nunca se corta: dos arcos entrelazados se relevan a 6 Hz · 3 segundos de herida viva]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La dirección de la herida: del jugador HACIA el cursor.
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 2f,
                type, damage, knockback, player.whoAmI,
                dir.ToRotation(), Main.rand.Next(1000), 0f);
            return false;
        }
    }
}
