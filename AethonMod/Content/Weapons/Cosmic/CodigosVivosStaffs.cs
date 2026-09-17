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
    /// DanzaOrbesStaff — LA DANZA DE LOS ORBES (v6.36 — IMAGEN 1).
    ///
    /// EL PRIMER CÓDIGO VIVO: la cadena follow() de la imagen —
    /// "this.x = x + this.size * (this.x - x) / dist" — hecha arma. Un
    /// MINISISTEMA SOLAR orbita al portador: el sol dorado cabalgando
    /// su órbita, dos planetas colgados de él a SU distancia exacta y
    /// dos lunas por planeta, cada uno con su deriva angular — y la
    /// cadena recursiva manteniéndolo todo atado mientras el conjunto
    /// gira. Los enlaces de luz, las estelas tangenciales (el
    /// updateRelative del original) y las chispas del sol visten la
    /// danza.
    ///
    /// MECÁNICA: el sistema vive 12 s alrededor del jugador — TODO orbe
    /// quema al que toca (sol ×1.0, planetas ×0.7, lunas ×0.5, cada 10
    /// ticks). Un sistema a la vez. Sin maná.
    /// </summary>
    public class DanzaOrbesStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 96;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DanzaOrbesProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla v6.35)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.50f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFD54F:═══ LA DANZA DE LOS ORBES ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFE082:Invoca el código vivo de la cadena: un minisistema solar orbitándote —\nel sol, dos planetas y cuatro lunas, cada hijo atado a su padre a su distancia exacta]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFB300:TODO orbe quema al que toca: sol ×1.0 · planetas ×0.7 · lunas ×0.5 cada 10 ticks]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:12 segundos de danza alrededor del portador · un sistema a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarDanzaVieja(player);
            // El sistema nace EN el jugador: su raíz cabalga la órbita.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: cierra la danza anterior del mismo owner.</summary>
        private static void MatarDanzaVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<DanzaOrbesProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// LenteAbismoStaff — LA LENTE DEL ABISMO (v6.36 — IMAGEN 2).
    ///
    /// EL SEGUNDO CÓDIGO VIVO: el animate() del agujero negro de la
    /// imagen — el reloj alimentando los tres materiales (disco,
    /// estrellas, horizonte) y la posición proyectada a pantalla
    /// alimentando la lente. Aquí el disco de veinte bandas viaja con
    /// el uTime, el HORIZONTE NEGRO se come la luz, LAS ESTRELLAS
    /// DOBLADAS caen en espiral acelerando y destellan al cruzar el
    /// anillo de fotones — y TODO el fondo se curva alrededor (la
    /// lensingPass del original, la lente más fuerte del arsenal).
    ///
    /// MECÁNICA: planta el ojo en el cursor 6 s — succión en 300 px
    /// (los jefes no se dejan), el que cruza el horizonte es devorado
    /// (×1.2 cada 12 ticks) y cada 90 ticks LA LENTE ENFOCA: muerde al
    /// enemigo más cercano (×0.6). Sin maná.
    /// </summary>
    public class LenteAbismoStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 104;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 34; Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LenteAbismoProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla v6.35)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.50f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF40D0:═══ LA LENTE DEL ABISMO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E1BEE7:Planta el código vivo del agujero negro: el disco de veinte bandas viajando,\nel horizonte negro absoluto y las estrellas dobladas cayendo en espiral]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF80AB:LA LENTE curva el fondo (la más fuerte del arsenal) · succión en 300 px ·\nel que cruza el horizonte es DEVORADO ×1.2 · cada 90 ticks la lente ENFOCA y muerde ×0.6]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:6 segundos de ojo abierto donde apuntas · un ojo a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarLenteVieja(player);
            // El ojo nace EXACTO en el cursor: no viaja — MIRA desde ahí.
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: cierra el ojo anterior del mismo owner.</summary>
        private static void MatarLenteVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<LenteAbismoProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// SolVivoStaff — EL SOL VIVO (v6.36 — IMAGEN 3).
    ///
    /// EL TERCER CÓDIGO VIVO: el animate() del sol de la imagen — el
    /// pulso 0.5 + 0.5·sin(time·2.15) y el bloom 0.8 + 0.4·pulse
    /// latiendo en TODO (el render Y la luz del mundo, la MISMA
    /// fórmula), el núcleo girando a 0.05 rad/s (coreGroup) y los seis
    /// materiales vivos: el núcleo estelar, la cáscara que respira, el
    /// disco ecuatorial rotando, los anillos que nacen en cada pico,
    /// las prominencias arqueándose y las ascuas escapando.
    ///
    /// MECÁNICA: enciende el sol en el cursor 5 s — el aura quema
    /// (×0.55 cada 15 ticks a 130 px), en cada PICO del pulso el sol
    /// LATE (×0.7 a 170 px) y cada 45 ticks las PROMINENCIAS azotan a
    /// los 2 enemigos más cercanos (×0.85). Sin maná.
    /// </summary>
    public class SolVivoStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SolVivoProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla v6.35)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.15f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF9E40:═══ EL SOL VIVO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFCC80:Enciende el código vivo del sol: latiendo con el pulso exacto del original\n(sin(time·2.15)), el núcleo girando, la cáscara respirando y las prominencias arqueándose]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF6D00:El aura quema ×0.55 · cada PICO del pulso el sol LATE ×0.7 ·\nlas prominencias AZOTAN a los 2 más cercanos ×0.85]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:5 segundos de sol donde apuntas · un sol a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarSolViejo(player);
            // El sol nace EXACTO en el cursor: no viaja — ARDE ahí.
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.rand.Next(1000), 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: apaga el sol anterior del mismo owner.</summary>
        private static void MatarSolViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SolVivoProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// SierpeEstelarStaff — LA SIERPE ESTELAR (v6.36 — IMAGEN 4).
    ///
    /// EL CUARTO CÓDIGO VIVO: los elems de la imagen — la criatura de
    /// 16 segmentos con su jerarquía EXACTA (la CABEZA en el primero,
    /// las ALETAS en el octavo y el decimocuarto, la ESPINA en todo lo
    /// demás) nadando en vueltas alrededor del puntero con su radio
    /// (radm) y su vaivén de velocidad. La cadena que ata cada
    /// segmento al anterior es la MISMA LEY de la imagen 1 — el ADN
    /// compartido de las dos demos.
    ///
    /// MECÁNICA: libera la sierpe 8 s — la cabeza nada en círculos
    /// alrededor de donde apuntas y ATRAVIESA todo lo que toca; la
    /// espina quema a su paso (×0.4 cada 8 ticks). Una sierpe a la
    /// vez. Sin maná.
    /// </summary>
    public class SierpeEstelarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 32; Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SierpeEstelarProjectile>();
            Item.shootSpeed = 11f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla v6.35)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.62f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/E8F4FF:═══ LA SIERPE ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/B4E1FF:Libera el código vivo de la criatura: dieciséis segmentos — la CABEZA,\nlas ALETAS gemelas y la ESPINA — nadando en vueltas alrededor de tu cursor]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/82E9FF:La cabeza ATRAVIESA todo lo que toca · la espina QUEMA a su paso ×0.4 cada 8 ticks]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:8 segundos de nado estelar · una sierpe a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarSierpeVieja(player);

            // La sierpe nace del jugador hacia el cursor; el PUNTERO
            // alrededor del que nada viaja en ai[0..1], la semilla en ai[2].
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 11f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        /// <summary>EL TOPE DE 1: recoge la sierpe anterior del mismo owner.</summary>
        private static void MatarSierpeVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SierpeEstelarProjectile>())
                    p.Kill();
            }
        }
    }
}
