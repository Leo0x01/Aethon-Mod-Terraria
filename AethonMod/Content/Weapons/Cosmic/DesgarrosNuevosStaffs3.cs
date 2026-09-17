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
    /// CorazonColapsoStaff — EL CORAZÓN DEL COLAPSO (v6.35).
    ///
    /// Uno de LOS CUATRO DESGARROS NUEVOS de las referencias del usuario:
    /// la estrella de CUATRO PUNTAS de fuego estelar — cuatro filamentos
    /// de plasma curvándose tangencialmente (los brazos de una galaxia
    /// naciendo), el gradiente blanco amarillento → naranja → carmesí,
    /// el halo rojo oscuro y las chispas de oro orbitando hacia fuera.
    ///
    /// MECÁNICA: el corazón arde 4 s en el punto del cursor. TODO enemigo
    /// dentro del radio es quemado (i-frames 20) y cada 30 ticks los
    /// filamentos LANZAN LLAMARADAS a los 2 enemigos más cercanos (×0.5).
    /// Sin maná: el corazón se alimenta de sí mismo.
    /// </summary>
    public class CorazonColapsoStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 108;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 32; Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CorazonColapsoProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // v6.35: SIN MANÁ (petición del usuario)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.45f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF8C00:═══ EL CORAZÓN DEL COLAPSO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFD700:Enciende donde apuntas la ESTRELLA DE CUATRO PUNTAS: los filamentos de fuego estelar\ncurvándose como brazos de galaxia, del blanco al carmesí]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/DC143C:Quema a TODO enemigo dentro del corazón · cada 30 ticks los filarios LANZAN LLAMARADAS a los 2 más cercanos ×0.5]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:4 segundos de colapso ardiendo · SIN COSTE DE MANÁ — el corazón se alimenta de sí mismo]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El corazón nace EXACTO en el cursor: no viaja — ARDE ahí.
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }
    }

    /// <summary>
    /// GargantaVacioStaff — LA GARGANTA DEL VACÍO (v6.35).
    ///
    /// Uno de LOS CUATRO DESGARROS NUEVOS: el vórtice devorador — el
    /// NÚCLEO NEGRO ABSOLUTO con el anillo magenta (la fórmula fiel del
    /// shader), los tres brazos espirales de galaxia y el polvo estelar
    /// cayendo en espiral hacia la nada. Y EL MUNDO SE DOBLA: la lente
    /// gravitacional curva el fondo alrededor de la garganta.
    ///
    /// MECÁNICA: la garganta vive 5 s en el cursor con SUCCIÓN FUERTE
    /// (320 px — los jefes no se dejan arrastrar) y el que toca el
    /// NÚCLEO NEGRO es DEVORADO (×1.15, knockback 0, cada 15 ticks).
    /// Un solo vórtice. Sin maná.
    /// </summary>
    public class GargantaVacioStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 102;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 34; Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<GargantaVacioProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // v6.35: SIN MANÁ (petición del usuario)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.42f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF40D0:═══ LA GARGANTA DEL VACÍO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/5A0B7A:Abre donde apuntas el vórtice devorador: el núcleo negro absoluto con su anillo magenta,\nlos tres brazos de galaxia y el polvo estelar cayendo a la nada]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFE4FA:SUCCIÓN FUERTE en 320 px · LA LENTE curva el fondo · el que toca el núcleo negro es DEVORADO ×1.15]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:5 segundos de hambre · un solo vórtice activo · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarGargantaVieja(player);
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: cierra la garganta anterior del mismo owner.</summary>
        private static void MatarGargantaVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<GargantaVacioProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// UmbralRotoStaff — EL UMBRAL ROTO (v6.35).
    ///
    /// Uno de LOS CUATRO DESGARROS NUEVOS: el corte GEOMÉTRICO horizontal
    /// que parte la realidad — arriba EL VACÍO de la otra realidad con el
    /// ESQUELETO ESPECTRAL meciéndose en sus líneas blancas, abajo el
    /// mundo tal cual; la línea blanca con aberración cromática y las
    /// partículas de datos cian parpadeando.
    ///
    /// MECÁNICA: el umbral se raja 3.5 s a la altura del cursor (700 px
    /// de corte horizontal): pica en LÍNEA cada 12 ticks (×0.4) y cada
    /// 60 ticks LA OTRA REALIDAD MUERDE — 3 mandíbulas espectrales (×0.8)
    /// contra los enemigos más cercanos a la línea. Sin maná.
    /// </summary>
    public class UmbralRotoStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 28; Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<UmbralRotoProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // v6.35: SIN MANÁ (petición del usuario)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.10f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/00FFFF:═══ EL UMBRAL ROTO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E0E0E0:Raja la realidad con un CORTE HORIZONTAL PERFECTO: arriba el vacío con el esqueleto\nespectral de la otra realidad, abajo el mundo tal cual]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/00FFFF:Pica en línea ×0.4 cada 12 ticks · cada 60 ticks LA OTRA REALIDAD MUERDE: 3 mandíbulas ×0.8]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:3.5 segundos de umbral abierto con partículas de datos · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }
    }

    /// <summary>
    /// LeviatanEspectralStaff — EL LEVIATÁN ESPECTRAL (v6.35).
    ///
    /// Uno de LOS CUATRO DESGARROS NUEVOS: la criatura marina de HUESO
    /// ETÉREO — la columna de catorce vértebras nadando en S, las aletas
    /// curvas afiladas alternando lados, la cabeza con la mandíbula
    /// abierta y la cresta del dragón. La referencia del usuario: el
    /// esqueleto dibujado a línea, vivo.
    ///
    /// MECÁNICA: LANZA al leviatán hacia el cursor — atraviesa TODO lo
    /// que toca mientras nada con su onda (4 s de vida, giro suave hacia
    /// el enemigo más cercano). Sin maná.
    /// </summary>
    public class LeviatanEspectralStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 86;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LeviatanEspectralProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;   // v6.35: SIN MANÁ (petición del usuario)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.30f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/E8F4FF:═══ EL LEVIATÁN ESPECTRAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/9FD8FF:LANZA a la criatura de hueso etéreo: catorce vértebras nadando en S,\nsus aletas afiladas y su mandíbula abierta]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/E8F4FF:Atraviesa TODO lo que toca mientras nada · gira suave hacia el enemigo más cercano]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:4 segundos de nado espectral · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El leviatán nada del jugador HACIA el cursor.
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 14f,
                type, damage, knockback, player.whoAmI,
                dir.ToRotation(), Main.rand.Next(1000), 0f);
            return false;
        }
    }
}
