using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    // ======================================================================
    //  v6.50.19 — LAS SEIS IDEAS DE PROYECTILES DEL GRIMORIO.
    //
    //  Petición del usuario: "necesito un proyectil especial solo para
    //  el grimorio... si tienes ideas sobre tipos de proyectiles para el
    //  grimorio, crea varias armas con esas ideas y ponlas en una nueva
    //  bolsa de ideas de proyectiles para el grimorio, ten en cuenta que
    //  el proyectil será algo que empezará simple y cuanto más nivel
    //  consiga el grimorio este evolucionará y mejorará con el tiempo".
    //
    //  Seis conceptos DISTINTOS de lo que podría ser el proyectil que el
    //  Grimorio del Eterno aprende a lanzar (hoy dispara el Nightglow
    //  vanilla — el proyectil del arma de la Emperatriz de la Luz). Cada
    //  arma es una IDEA completa con su propia escalera de evolución
    //  atada al NIVEL DEL GRIMORIO (el libro que llevas encima):
    //
    //    1 · EL FOLIO ERRANTE — la página que se convierte en rebaño.
    //    2 · LA PLUMA PRIMORDIAL — la pluma que ESCRIBE runas al golpear.
    //    3 · EL SELLO ERRANTE — el círculo que camina, detona y ENCADENA.
    //    4 · LA LENGUA DE TINTA — la sierpe de tinta que se bifurca.
    //    5 · EL OJO DEL TEXTO — el ojo que telee y dispara (el sucesor
    //        natural del Nightglow — homing con identidad de libro).
    //    6 · EL VERSO VIVO — la palabra hecha proyectil (glifos en
    //        formación que rebotan y terminan en SENTENCIA).
    //
    //  LA ESCALERA (el nivel del GRIMORIO que llevas, no el del arma):
    //    Etapa 1 (nivel 1+): la idea simple.
    //    Etapa 2 (nivel 6+): crece (más cuerpo, más hijos, más filo).
    //    Etapa 3 (nivel 12+): la idea MADURA (cadena, escritura, eco).
    //    Etapa 4 (nivel 20+): la forma FINAL (rebaño, sentencia, hydra).
    //
    //  TODAS sin maná (la regla de las armas de pruebas de la casa) y
    //  TODAS en La Bolsa de las Ideas del Grimorio (la 17ª bolsa).
    // ======================================================================

    /// <summary>El puente con el libro: el nivel del GRIMORIO que llevas.</summary>
    internal static class IdeasGrimorio
    {
        /// <summary>El nivel del Grimorio del Eterno en el inventario (1 si no hay).</summary>
        internal static int NivelGrimorio(Player p)
        {
            try
            {
                int tipo = ModContent.ItemType<Weapons.GrimoireEternal>();
                for (int i = 0; i < 58; i++)
                {
                    Item it = p.inventory[i];
                    if (it != null && it.active && it.type == tipo)
                        return it.GetGlobalItem<Globals.ShardLevelItem>().Level;
                }
            }
            catch { }
            return 1;
        }

        /// <summary>La etapa de la escalera (1..4) según el nivel del grimorio.</summary>
        internal static int Etapa(int nivel) =>
            nivel >= 20 ? 4 : nivel >= 12 ? 3 : nivel >= 6 ? 2 : 1;
    }

    // ======================================================================
    //  1 · EL FOLIO ERRANTE — la página viva
    // ======================================================================
    public class IdeasFolioErrante : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 46;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FolioErranteProjectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/AEE8FF:La página del grimorio hecha proyectil]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Evolutiva con el NIVEL DEL GRIMORIO que lleves encima · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  2 · LA PLUMA PRIMORDIAL — la pluma que escribe
    // ======================================================================
    public class IdeasPlumaPrimordial : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 34;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 17; Item.useAnimation = 17;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PlumaPrimordialProjectile>();
            Item.shootSpeed = 16f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            int etapa = IdeasGrimorio.Etapa(nivel);
            int plumas = etapa >= 2 ? 3 : 1;
            for (int i = 0; i < plumas; i++)
            {
                Vector2 vel = velocity.RotatedBy((i - (plumas - 1) / 2f) * 0.11f);
                // la pluma del CENTRO es ORO en la etapa 4 (la que ESCRIBE
                // el verso entero al golpear).
                bool dorada = etapa >= 4 && i == (plumas - 1) / 2;
                Projectile.NewProjectile(source, position, vel, type,
                    damage, knockback, player.whoAmI, nivel, dorada ? 2f : 0f, 0f);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD9A0:La pluma que escribió el grimorio, hecha dardo]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Evolutiva con el nivel del GRIMORIO · perfora · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  3 · EL SELLO ERRANTE — el círculo que camina
    // ======================================================================
    public class IdeasSelloErrante : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SelloErranteProjectile>();
            Item.shootSpeed = 7f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            int etapa = IdeasGrimorio.Etapa(nivel);
            int sellos = etapa >= 4 ? 3 : 1;
            for (int i = 0; i < sellos; i++)
            {
                Vector2 vel = velocity.RotatedBy((i - (sellos - 1) / 2f) * 0.08f);
                Projectile.NewProjectile(source, position, vel, type,
                    damage, knockback, player.whoAmI, nivel, 0f, 0f);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFC766:El sello que camina lento, detona y ENCADENA]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Evolutivo con el nivel del GRIMORIO · detona en pernos · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  4 · LA LENGUA DE TINTA — la sierpe de tinta
    // ======================================================================
    public class IdeasLenguaTinta : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LenguaTintaProjectile>();
            Item.shootSpeed = 11f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item13;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            int etapa = IdeasGrimorio.Etapa(nivel);
            // Etapa 4: LA HYDRA — tres lenguas desde el arranque.
            int lenguas = etapa >= 4 ? 3 : 1;
            for (int i = 0; i < lenguas; i++)
            {
                Vector2 vel = velocity.RotatedBy((i - (lenguas - 1) / 2f) * 0.16f);
                Projectile.NewProjectile(source, position, vel, type,
                    damage, knockback, player.whoAmI, nivel, 0f, 0f);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/9A8FD4:La tinta del grimorio viva: una sierpe que serpentea]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Evolutiva con el nivel del GRIMORIO · se bifurca · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  5 · EL OJO DEL TEXTO — el sucesor del Nightglow
    // ======================================================================
    public class IdeasOjoTexto : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 44;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OjoTextoProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            int etapa = IdeasGrimorio.Etapa(nivel);
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            // Etapa 2+: LOS PUPILAS — mini ojos escoltando al grande.
            if (etapa >= 2)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 vel = velocity.RotatedBy(i * 0.18f) * 0.9f;
                    Projectile.NewProjectile(source, position, vel, type,
                        (int)(damage * 0.4f), knockback, player.whoAmI, nivel, 1f, 0f);
                }
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/AEE8FF:El ojo del libro: persigue y LEE a sus presas]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El sucesor conceptual del proyectil actual · evolutivo · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  6 · EL VERSO VIVO — la palabra proyectil
    // ======================================================================
    public class IdeasVersoVivo : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 24;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 28; Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VersoVivoProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item45;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            int etapa = IdeasGrimorio.Etapa(nivel);
            int glifos = etapa switch { >= 4 => 7, >= 2 => 5, _ => 3 };
            for (int i = 0; i < glifos; i++)
            {
                // la FORMACIÓN: fila apretada (1) → ola alterna (2+) → DOS
                // filas (4: la sentencia) — cada glifo desfasado detrás
                // del anterior, la palabra se ESCRIBE volando.
                Vector2 vel = velocity.RotatedBy((i % 2 == 0 ? 1 : -1) * 0.06f * (etapa >= 2 ? 1f : 0f));
                Vector2 pos = position - velocity.SafeNormalize(Vector2.UnitX) * i * 12f;
                // modo: 1 = palíndromo (rebota y vuelve a golpear).
                float modo = etapa >= 3 ? 1f : 0f;
                Projectile.NewProjectile(source, pos, vel, type,
                    damage, knockback, player.whoAmI, nivel, modo, i * 3f);
            }
            // Etapa 4: EL PUNTO FINAL — la sentencia termina en un punto
            // que detona (un glifo grande dorado al final de la palabra).
            if (etapa >= 4)
            {
                Vector2 pos = position - velocity.SafeNormalize(Vector2.UnitX) * glifos * 12f;
                Projectile.NewProjectile(source, pos, velocity, type,
                    (int)(damage * 1.6f), knockback * 1.5f, player.whoAmI, nivel, 2f, glifos * 3f);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD9A0:Una palabra viva: glifos en formación que golpear]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El palíndromo rebota · la sentencia detona · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
