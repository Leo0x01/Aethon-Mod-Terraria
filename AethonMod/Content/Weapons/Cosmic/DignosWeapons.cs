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
    //  v6.51.4 — LAS SEIS ARMAS DIGNAS DEL GRIMORIO.
    //
    //  Petición del usuario: "crea un arma nueva por cada una de estas
    //  ideas que diste y crea una nueva bolsa para darsela al jugador".
    //  Las seis IDEAS de proyectil presentadas con la v6.50.19 (y re-
    //  presentadas al usuario con la entrega de la v6.51.3: «la binaria
    //  día/noche como evolución inmediata + las comas que muerden como
    //  mecánica nueva») maduran aquí en SEIS ARMAS COMPLETAS — una por
    //  idea, cada una con la mecánica final de una pieza (sin escaleras
    //  de nivel del grimorio: la idea YA digna):
    //
    //    1 · EL EQUINOCCIO BINARIO — la estrella doble que vive el
    //        reloj del mundo (día = eclipse en área · noche = perfora y
    //        drena vida).
    //    2 · LAS COMAS QUE MUERDEN — glifos suspendidos que azotan al
    //        soltar; seis en el mismo enemigo: LA FRASE DETONA.
    //    3 · LA PÁGINA ARRANCADA — la hoja-cuchilla de dos caras que se
    //        clava flotando y arde en EL VERSO (con cadena de versos).
    //    4 · EL OJO QUE CAZA — la mirada del escuadrón: viaja dormida,
    //        abre el párpado y el impacto es un PARPADEO.
    //    5 · LA LÍNEA DE LECTURA — hojea cargando; al soltar, un RAYO
    //        de texto puro que atraviesa todo (más hojeo = más frase).
    //    6 · EL MARCAPÁGINAS — dardos-marca clavados al enemigo; al
    //        quinto: FIN DEL CAPÍTULO, el libro se cierra de golpe.
    //
    //  TODAS sin maná (la regla de las armas de pruebas de la casa),
    //  TODAS con receta de 5 madera (el protocolo v6.14.2) y TODAS en
    //  LA BOLSA DE LOS PROYECTILES DIGNOS (la 22ª bolsa).
    // ======================================================================

    // ======================================================================
    //  1 · EL EQUINOCCIO BINARIO — la binaria día/noche
    // ======================================================================
    public class DignosEquinoccioBinario : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EquinoccioBinarioProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL RELOJ DEL MUNDO decide quién manda: el modo viaja en ai[0]
            // (la perforación de la noche la calza la IA del proyectil —
            // así todas las copias de la red la ponen solas).
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, Main.dayTime ? 1f : 2f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD678:El disparo es una estrella binaria: el disco dorado y el violeta orbitándose]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:DÍA: el oro guía y estalla en eclipse · NOCHE: el violeta guía, perfora y drena vida · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  2 · LAS COMAS QUE MUERDEN — el glifo suspendido
    // ======================================================================
    public class DignosComasQueMuerden : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 48;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 18; Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                 // el arma de canalización
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ComaRunicaProjectile>();
            Item.shootSpeed = 7f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item45;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA COMA sale a 7 exactos hacia el cursor (la velocidad del
            // arma es el rumbo; el módulo lo pone la mecánica).
            Vector2 vel = velocity.SafeNormalize(Vector2.UnitX) * 7f;
            Projectile.NewProjectile(source, position, vel, type,
                damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFE8AA:Mantén el clic: el glifo viaja y queda SUSPENDIDO como una coma]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Al soltar, TODAS azotan al enemigo más cercano · seis en el mismo: LA FRASE DETONA · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  3 · LA PÁGINA ARRANCADA — la hoja-cuchilla de dos caras
    // ======================================================================
    public class DignosPaginaArrancada : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PaginaArrancadaProjectile>();
            Item.shootSpeed = 12.5f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD882:Una página del libro que vuela GIRANDO como cuchilla: una cara dorada, una cara violeta]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Se clava y queda FLOTANDO… hasta EL VERSO: estalla en área · las páginas cercanas se ENCADENAN · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  4 · EL OJO QUE CAZA — la mirada del escuadrón
    // ======================================================================
    public class DignosOjoQueCaza : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OjoQueCazaProjectile>();
            Item.shootSpeed = 11f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/AEE8FF:La mirada del escuadrón en miniatura: viaja dormida y ABRE el párpado a mitad de camino]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:La pupila FIJA a su presa · el impacto es un PARPADEO con destello · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  5 · LA LÍNEA DE LECTURA — la frase cargada
    // ======================================================================
    public class DignosLineaDeLectura : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                 // hojea mientras mantengas
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LineaLecturaProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;       // la vuelta de página del hojeo
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA CARGA ES ÚNICA (el patrón "viva" de la Vela Solar): cada
            // vuelta de useTime re-intenta el disparo mientras se canaliza —
            // solo la PRIMERA siembra la carga; las demás alimentan el hojeo.
            bool viva = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.owner != player.whoAmI) continue;
                if (p.type == type && p.ai[0] == 0f) { viva = true; break; }
            }
            if (!viva)
                Projectile.NewProjectile(source, position, Vector2.Zero, type,
                    damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFF0C0:Mantén para HOJEAR el libro; al soltar, dispara un RAYO de texto puro]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:La línea de glifos atraviesa todo en recta · más hojeo = frase más larga, daño ×1 → ×3 · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  6 · EL MARCAPÁGINAS — el dardo que marca
    // ======================================================================
    public class DignosMarcapaginas : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MarcapaginasProjectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item5;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, -1f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD678:Los disparos dejan marcadores brillantes CLAVADOS en el enemigo]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Al quinto marcador: FIN DEL CAPÍTULO — el libro se cierra de golpe sobre él · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
