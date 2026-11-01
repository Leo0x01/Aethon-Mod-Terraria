using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons.Cosmic
{
    // ======================================================================
    //  v6.77 — LAS CATORCE ARMAS DEL CRECIMIENTO.
    //
    //  LA LETRA (la bolsa de las ideas, la ronda de las 14): «porque
    //  elegir una cuando se pueden hacer todas jajajajajaja, crea una
    //  bolsa nueva y dasela al jugador, esta sera una bolsa con un arma
    //  nueva por cada una de las ideas, 14 armas en total, asegurate que
    //  en su creacion usar nuestras librerias en caso de que necesites
    //  algo mas lo puedes crear e investigar en internet y en otros
    //  mods».
    //
    //  EL TEMA COMÚN (la palabra que las une): CRECER. Cada arma es una
    //  de las 14 ideas del usuario, cada una crece a su manera — por
    //  germinación, por fusión, por carga, por distancia, por rebote, por
    //  mordida, por caída, por órbita, por eco, por contacto...
    //
    //    1 · LA SEMILLA RÚNICA — planta una runa diminuta donde apuntas;
    //        GERMINA (glifo → sello en ~1 s) dañando en área creciente;
    //        clicar sobre una runa MADURA la detona con la esfera de luz.
    //    2 · LA PALABRA HUECA — escupe una palabra lenta; cada disparo
    //        nuevo la ABSORBE y la palabra CRECE por fusión — al cuarto
    //        refuerzo es un VERBO DE PODOR enorme.
    //    3 · LA VELA QUE ENGREÑE — mantener el clic hace CRECER la llama
    //        del libro; al soltar, la escupe con tamaño y daño según lo
    //        cargado.
    //    4 · EL COMETA DE PALABRAS — nace diminuto y CRECE con la
    //        distancia recorrida: a 5 bloques una chispa, a 30 un cometa.
    //    5 · EL SELLO DIGITAL — cada clic planta un sello instantáneo en
    //        el cursor (radio que crece con el nivel); cada sello suma
    //        PÁGINAS — a las 10, una PALABRA DE PODER (área + cadena +
    //        empuje). El círculo rúnico tenue alrededor del jugador.
    //    6 · EL LÁTIGO DE PERLIN — un brazo corto de rayo perlin (¡el
    //        idioma visual de los ojos!) que crece de 20 a 96 px con el
    //        nivel del libro.
    //    7 · EL DEVORADOR MENOR — una boquita diminuta que vuela, muerde
    //        varias veces y CRECE un 15 % con cada mordida hasta
    //        REVENTAR en una nube de bruma.
    //    8 · LA ESTRELLA DOMÉSTICA — nace como puntito sobre el libro y
    //        CAE hacia el cursor CRECIENDO durante la caída (la gravedad
    //        la alimenta); estalla en el punto exacto.
    //    9 · LA PÁGINA CORTANTE — una hoja doblada como daga que CRECE
    //        con cada rebote en las paredes (hasta ×3).
    //   10 · EL PRISMA FRAGMENTADO — el primer clic lanza un fragmento;
    //        el siguiente lo DIVIDE en dos menores que se RE-FUSIONAN en
    //        uno MAYOR que golpea.
    //   11 · LA RUNA ORBITAL — 3-5 glifos orbitan el libro mientras lo
    //        llevas (visibles incluso en idle); al clicar, uno SALE
    //        DISPARADO girando hacia el cursor. Crecen con la órbita.
    //   12 · LA ESPORA DEL ESCRIBA — proyectil lento que va plantando
    //        esporas; las esporas crecen en silencio y DETONAN cuando un
    //        enemigo se acerca — campos de trampas vivas.
    //   13 · EL ECO DEL TRAZO — el proyectil deja un trazo de tinta que
    //        CRECE hacia atrás (la estela se vuelve cuerpo) y REGRESA
    //        barriendo: cuanto más lejos viajó, más grueso el látigo.
    //   14 · LA TINTA VIVA — una gota semitransparente que se AVIVA con
    //        cada enemigo que toca (cada golpe la hace más grande y más
    //        opaca) — un snowball controlado con tope.
    //
    //  Como las Ideas de la v6.50.19: TODAS sin maná (la regla de las
    //  armas de pruebas de la casa), TODAS evolutivas con la ETAPA del
    //  GRIMORIO que llevas (el puente IdeasGrimorio — la MISMA escalera
    //  6 · 12 · 20) y TODAS en LA BOLSA DEL CRECIMIENTO (bolsa 26 del
    //  kit — la entrega directa de TestingPlayer).
    // ======================================================================

    /// <summary>1 · LA SEMILLA RÚNICA — la runa que germina.</summary>
    public class CrecimientoSemillaRunica : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 38;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.SemillaRunicaProjectile>();
            Item.shootSpeed = 22f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            // LA DETONACIÓN AL CLICAR: si el cursor está sobre una runa
            // MADURA (la letra: «clicar sobre una runa madura la detona
            // con la esfera de luz») — esa revienta y NO se dispara.
            int tipoRuna = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.SemillaRunicaProjectile>();
            Vector2 cursor = Main.MouseWorld;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipoRuna ||
                    p.owner != player.whoAmI) continue;
                if (p.ModProjectile is global::AethonMod.Content.Projectiles.Cosmic.SemillaRunicaProjectile runa &&
                    runa.EsRunaMadura && Vector2.Distance(p.Center, cursor) < 48f)
                {
                    runa.DetonarMadura();
                    return false;
                }
            }
            // LA SIEMBRA: la semilla vuela al punto y germina allí.
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/B8FFC7:La runa que germina: siembra, crece y detona]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Clic sobre una runa MADURA la detona · evolutiva con el GRIMORIO · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>2 · LA PALABRA HUECA — la palabra que crece por fusión.</summary>
    public class CrecimientoPalabraHueca : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.PalabraHuecaProjectile>();
            Item.shootSpeed = 5.5f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item45;
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
                "[c/FFE9B8:La palabra lenta que se alimenta de sus hermanas]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Cada disparo nuevo la ABSORBE y CRECE · al 4º refuerzo: EL VERBO DE PODER · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>3 · LA VELA QUE ENGREÑE — la llama que se carga.</summary>
    public class CrecimientoVelaEngrine : ModItem
    {
        /// <summary>LA CARGA contada en UseStyle mientras se mantiene el
        /// clic (SP-first: el arma de pruebas de la casa).</summary>
        internal static int TicksCargando;

        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.channel = true;   // MANTENER el clic: la llama CRECE
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.VelaEngrineProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = 12000;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // EL CONDENSADOR: mientras se mantiene, la cuenta SUBE (tope
            // 90 — 1,5 s de carga completa).
            if (player.channel && player.whoAmI == Main.myPlayer)
                TicksCargando = System.Math.Min(TicksCargando + 1, 90);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            // AL SOLTAR: la llama sale con SU carga (la cuenta viaja en
            // ai[1] y se apaga — la vela vuelve a nacer).
            int carga = TicksCargando;
            TicksCargando = 0;
            if (carga < 6) carga = 6;
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, nivel, carga, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD2A0:Mantén el clic: la llama del libro CRECE]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Al soltar, la escupe con tamaño y daño según lo cargado · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>4 · EL COMETA DE PALABRAS — crece con la distancia.</summary>
    public class CrecimientoCometaPalabras : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.CometaPalabrasProjectile>();
            Item.shootSpeed = 16f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9;
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
                "[c/C7E8FF:Nace chispa, llega cometa: CRECE con la distancia]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Recompensa el apuntado lejano, castiga el punto-blanco · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>5 · EL SELLO DIGITAL — el clicker de los sellos.</summary>
    public class CrecimientoSelloDigital : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 33;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 14; Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.SelloDigitalProjectile>();
            Item.shootSpeed = 0f;    // el sello nace EN el cursor (clamped)
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            // EL RADIO que crece con el nivel del libro (la letra: «dentro
            // de un radio que crece con el nivel del libro»): 180 → 400.
            float radio = MathHelper.Clamp(180f + nivel * 3f, 180f, 400f);
            Vector2 cursor = Main.MouseWorld;
            Vector2 desde = player.MountedCenter;
            Vector2 dir = cursor - desde;
            float d = dir.Length();
            if (d > radio) dir = dir / d * radio;   // CLAMP al borde del círculo
            Vector2 pos = desde + dir;
            // EL SELLO INSTANTÁNEO: nace clavado en el punto (velocidad 0).
            Projectile.NewProjectile(source, pos, Vector2.Zero, type,
                damage, knockback, player.whoAmI, nivel, radio, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFC766:El clicker puro: cada clic planta un sello]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:10 páginas → LA PALABRA DE PODER (área · cadena · empuje) · el círculo rúnico te rodea · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>6 · EL LÁTIGO DE PERLIN — el brazo corto de rayo.</summary>
    public class CrecimientoLatigoPerlin : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 42;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 12; Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.LatigoPerlinProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item13;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            Projectile.NewProjectile(source, position, Vector2.Zero, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/B8D9FF:El brazo de perlin: el idioma visual de los ojos]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Un latigazo corto que crece 20 → 96 px con el nivel del libro · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>7 · EL DEVORADOR MENOR — la boquita que crece mordiendo.</summary>
    public class CrecimientoDevoradorMenor : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 19;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.DevoradorMenorProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item3;
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
                "[c/FFB8C7:La boquita que muerde, crece y REVIENTA en bruma]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:+15 % de tamaño por mordida — el hambre del libro en miniatura · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>8 · LA ESTRELLA DOMÉSTICA — la caída que alimenta.</summary>
    public class CrecimientoEstrellaDomestica : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.EstrellaDomesticaProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item25;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            // LA ESTRELLA NACE SOBRE EL LIBRO (el puntito arriba) y cae al
            // punto del clic: la posición de nacimiento es la del jugador.
            Vector2 pos = player.MountedCenter + new Vector2(0f, -70f);
            Projectile.NewProjectile(source, pos, velocity, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFF3B8:Nace sobre el libro y CAE creciendo hacia el cursor]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:La gravedad la alimenta — estalla en el punto exacto · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>9 · LA PÁGINA CORTANTE — la daga que crece rebotando.</summary>
    public class CrecimientoPaginaCortante : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 28;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.PaginaCortanteProjectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item15;
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
                "[c/E8D9FF:La hoja doblada como daga — ninja de biblioteca maldita]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:CRECE con cada rebote en las paredes (hasta ×3) · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>10 · EL PRISMA FRAGMENTADO — la fusión que golpea.</summary>
    public class CrecimientoPrismaFragmentado : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 18; Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.PrismaFragmentadoProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item42;
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
                "[c/C7FFF2:El fragmento que se divide y se RE-FUSIONA MAYOR]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Crecimiento por fusión — la ciencia del grimorio · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>11 · LA RUNA ORBITAL — los glifos que esperan.</summary>
    public class CrecimientoRunaOrbital : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 31;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 15; Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.RunaOrbitalProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int nivel = IdeasGrimorio.NivelGrimorio(player);
            // EL ARSENAL ORBITAL: el clic (1) LANZA el glifo más viejo y
            // (2) rellena la órbita si hay hueco — el libro SIEMPRE listo.
            global::AethonMod.Content.Projectiles.Cosmic.RunaOrbitalProjectile.AlClic(source, player,
                type, damage, knockback, nivel);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFDFB8:Los glifos orbitan el libro — visibles incluso en idle]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Al clicar, uno SALE DISPARADO girando — crecen con la órbita · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>12 · LA ESPORA DEL ESCRIBA — las trampas vivas.</summary>
    public class CrecimientoEsporaDelEscriba : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 22;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 28; Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.EsporaDelEscribaProjectile>();
            Item.shootSpeed = 5f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item7;
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
                "[c/D9FFB8:La madre lenta que planta esporas en su camino]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Las esporas crecen en silencio y DETONAN al acercarse un enemigo · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>13 · EL ECO DEL TRAZO — el látigo que regresa.</summary>
    public class CrecimientoEcoDelTrazo : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 24;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.EcoDelTrazoProjectile>();
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
            Projectile.NewProjectile(source, position, velocity, type,
                damage, knockback, player.whoAmI, nivel, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/B8E8FF:El trazo de tinta que se vuelve cuerpo — y REGRESA]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Cuanto más lejos viajó, más grueso el látigo de tinta que barre al volver · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>14 · LA TINTA VIVA — el snowball con tope.</summary>
    public class CrecimientoTintaViva : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.TintaVivaProjectile>();
            Item.shootSpeed = 9f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item14;
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
                "[c/D9C7FF:La gota que se AVIVA con cada enemigo que toca]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Cada golpe la hace MÁS GRANDE y MÁS OPACA — snowball controlado con tope · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
