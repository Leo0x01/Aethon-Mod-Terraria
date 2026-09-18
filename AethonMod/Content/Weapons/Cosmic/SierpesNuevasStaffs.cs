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
    // ======================================================================
    //  v6.38 — LOS ONCE BASTONES DE LA CAMADA DE LAS SIERPES.
    //
    //  La petición: "toma el bastón de La Sierpe Estelar y usándolo como
    //  base crea otros 10 que sean creativos y diferentes, además crea
    //  uno que funcione como minion, y mantén intacto al bastón de la
    //  sierpe estelar original". CADA bastón libera una criatura con SU
    //  PROPIA forma de mover la cadena (un patrón distinto del informe
    //  research/sierpes_v638/INFORME_LOCOMOCION.md) — y el ONCEavo es la
    //  cría: la sierpe hecha sirviente.
    //
    //  EL BASTÓN DE LA SIERPE ESTELAR ORIGINAL NO SE TOCA.
    // ======================================================================

    /// <summary>
    /// OuroborosAstralStaff — EL OUROBOROS ASTRAL (v6.38 — PATRÓN 2).
    ///
    /// La persecución cíclica: doce cuentas que se persiguen EN CÍRCULO
    /// apuntando adelante — nadie manda, el anillo ES la cadena. Al
    /// morir, el adelanto desaparece y el mice problem las hunde en
    /// espiral logarítmica hasta el centro: la detonación.
    /// </summary>
    public class OuroborosAstralStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 92;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OuroborosAstralProjectile>();
            Item.shootSpeed = 8f;
            Item.mana = 0; Item.noMelee = true;   // SIN MANÁ (la regla de la casa)
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.70f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFD08C:═══ EL OUROBOROS ASTRAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFE4BC:Doce cuentas se persiguen en círculo apuntando ADELANTE — nadie manda:\nla causalidad es un anillo, y el anillo gira alrededor del portador]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/78DCF0:Cada cuenta barre lo que toca ×0.55 · al expirar el adelanto desaparece\ny la espiral logarítmica se cierra — EL COLAPSO DETONA ×2.0 en 100 px]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:10 segundos de anillo · un ouroboros a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarViejo(player);
            // El anillo nace en el jugador; el centro cabalga el puntero.
            Projectile.NewProjectile(source, player.MountedCenter, velocity,
                type, damage, knockback, player.whoAmI,
                player.MountedCenter.X, player.MountedCenter.Y, Main.rand.Next(1000));
            return false;
        }

        /// <summary>EL TOPE DE 1: disuelve el anillo anterior.</summary>
        private static void MatarViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<OuroborosAstralProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// CaravanaEspectralStaff — LA CARAVANA ESPECTRAL (v6.38 — PATRÓN 3).
    ///
    /// El camino-memoria: el cuerpo NO reacciona a su padre — repite la
    /// trayectoria EXACTA del faro, muestreada a arclength fijo. Donde
    /// el farol pasó, la caravana permanece.
    /// </summary>
    public class CaravanaEspectralStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 88;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CaravanaEspectralProjectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.50f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/8CFFDC:═══ LA CARAVANA ESPECTRAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/B8FFE8:El cuerpo NO sigue a su padre — repite la TRAYECTORIA EXACTA del faro:\nel camino-memoria de la caravana, eslabón a esclabón de arclength fijo]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFD678:El farol abre camino y CAZA · el rastro exacto LIMPIA el pasillo ×0.35\n— con giros rápidos dibujas MUROS EN S de luz persistente]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:9 segundos de caravana · una a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 13f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<CaravanaEspectralProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// AnguilaSolarStaff — LA ANGUILA SOLAR (v6.38 — PATRÓN 5).
    ///
    /// La onda viajera anguiliforme: el cuerpo ES una fórmula — la
    /// envolvente creciente y la onda que viaja hacia atrás con λ ≈ el
    /// largo del cuerpo. Solo las CRESTAS muerden de verdad.
    /// </summary>
    public class AnguilaSolarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 98;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 28; Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AnguilaSolarProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.35f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFEC78:═══ LA ANGUILA SOLAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFF4B8:El cuerpo NO tiene dinámica de cadena — ES una fórmula: la onda viajera\nde la anguila, con λ ≈ el largo del cuerpo y amplitud creciendo a la cola]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFFCD8:Las CRESTAS de la onda muerden ×1.0 · los valles solo ×0.30\n— aprende a rozar con la cresta y la anguila es un látigo]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:8 segundos de nado en S · una anguila a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 12f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<AnguilaSolarProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// CienpiesRunicoStaff — EL CIEMPIÉS RÚNICO (v6.38 — PATRÓN 6).
    ///
    /// La marcha metacronal: doce placas pegadas al suelo con una pata
    /// por placa desfasada Δφ=45° — la onda de patas que recorre el
    /// cuerpo de atrás hacia delante. La transición de marcha es real.
    /// </summary>
    public class CienpiesRunicoStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<CienpiesRunicoProjectile>();
            Item.shootSpeed = 8f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.20f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFB060:═══ EL CIEMPIÉS RÚNICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFD098:Doce placas pegadas al SUELO con una pata desfasada Δφ=45° cada una —\nla marcha metacronal: la onda de patas va de atrás hacia delante]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/B4DCFF:Las patas PLANTADAS dejan su RUNA en el suelo — la runa pica ×0.40\n· lejos de la presa la marcha SUBE de frecuencia (0.9 → 1.8 Hz)]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:10 segundos de marcha · un ciempiés a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarViejo(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 8f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<CienpiesRunicoProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// FlageloEstelarStaff — EL FLAGELO ESTELAR (v6.38 — PATRÓN 7).
    ///
    /// El látigo de Verlet con constraint por masa: la energía del mango
    /// se concentra en la punta (v·√m ≈ const). Cada 90 ticks el mango
    /// barre media vuelta en 10 — y la punta CHASQUEA.
    /// </summary>
    public class FlageloEstelarStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<FlageloEstelarProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.20f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF6040:═══ EL FLAGELO ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFA080:El látigo de Verlet con constraint repartido por MASA — la energía\ndel mango viaja a la punta y se concentra en menos materia (v·√m ≈ const)]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFF0DC:La punta golpea ∝ su velocidad (×0.5 → ×1.5) · cada 90 ticks el mango\nBARRER media vuelta · sobre 26 px/tick llega EL CHASQUIDO: ×2.2 + sacudida]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:8 segundos de flagelo · un látigo a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarViejo(player);
            Projectile.NewProjectile(source, player.MountedCenter, velocity,
                type, damage, knockback, player.whoAmI,
                0f, 0f, Main.rand.Next(1000));
            return false;
        }

        private static void MatarViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<FlageloEstelarProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// ViboraGenesiacaStaff — LA VÍBORA GENESÍACA (v6.38 — PATRÓN 9).
    ///
    /// La doble hélice: la ley de cadena vive solo en la espina (la
    /// sierpe intacta); dos hebras — la dorada y la violeta — giran
    /// alrededor como los peldaños de una escalera viva.
    /// </summary>
    public class ViboraGenesiacaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 32; Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ViboraGenesiacaProjectile>();
            Item.shootSpeed = 11f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.10f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFD678:═══ LA VÍBORA GENESÍACA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/BE78FF:Un esqueleto, dos cuerpos: la espina es la MISMA cadena de la sierpe\ny las DOS HEBRAS — la dorada y la violeta — giran trenzadas a su alrededor]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFF4B8:Las perlas de las hebras queman ×0.45 · cuando la víbora ENROSCA su rumbo\nla hélice se COMPRIME: las hebras convergen y arden ×1.6]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:9 segundos de víbora · una a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 11f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<ViboraGenesiacaProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// BoaEclipseStaff — LA BOA DEL ECLIPSE (v6.38 — PATRÓN 10).
    ///
    /// El constrictor: la cadena no cambia — cambia el líder. La cabeza
    /// hace la servo-espiral sobre la presa y cada vuelta completa es un
    /// stack de APRIETE. Como la boa real: aprieta sincronizada con la
    /// exhalación de la víctima.
    /// </summary>
    public class BoaEclipseStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 86;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 34; Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BoaEclipseProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.45f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/F0F0E6:═══ LA BOA DEL ECLIPSE ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/96A0C8:La cadena no cambia — cambia el LÍDER: la cabeza hace la servo-espiral\nsobre la presa (θ avanza, el radio ENCOGE) y el cuerpo la envuelve]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFB060:Cada VUELTA COMPLETA es un stack de APRIETE: la presión crece\n×0.5 → ×4.0 · la presa cae y la boa SE DESENROSCA y busca la siguiente]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:10 segundos de abrazo · una boa a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 15f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<BoaEclipseProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// FarolGuardianStaff — EL FAROL GUARDIÁN (v6.38 — PATRÓN 13).
    ///
    /// La cinemática inversa (FABRIK): el mando está INVERTIDO — la base
    /// es un farol colgante anclado al portador y la PUNTA persigue a la
    /// presa con las dos pasadas del reach. La criatura ALCANZA, no nada.
    /// </summary>
    public class FarolGuardianStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<FarolGuardianProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.60f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFC45C:═══ EL FAROL GUARDIÁN ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/A8E0FF:El mando está INVERTIDO: la BASE es un farol colgante sobre tu hombro\ny la PUNTA persigue a la presa — el cuerpo se TENSA como un arco (FABRIK)]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFF4B8:La MANO muerde ×1.0 y la cadena quema ×0.40 · si la presa está más\nlejos del alcance, el farol se ESTIRA hacia ella sin tocarla]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:12 segundos de guardián · un farol a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarViejo(player);
            // El guardián nace del jugador; el ancla cuelga al instante.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarViejo(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<FarolGuardianProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// CintaAuroraStaff — LA CINTA AURORA (v6.38 — PATRÓN 14).
    ///
    /// La cinta al viento: la espina está anclada a la espalda y el
    /// cuerpo visible es el offset de dos senos inconmensurables — y la
    /// amplitud es el VIENTO: la velocidad de carrera del portador.
    /// </summary>
    public class CintaAuroraStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<CintaAuroraProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.30f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/78FFBE:═══ LA CINTA AURORA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/C882FF:El estandarte vivo: la espina anclada a tu espalda y el cuerpo ondeando\ncon DOS SENOS INCONMENSURABLES — la cinta de la aurora cortejando el aire]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/B8FFE8:La cinta CORTA lo que toca ×0.5 · la amplitud es el VIENTO y el viento\nes TU VELOCIDAD — corre fuerte y la aurora se vuelve látigo (×0.9)]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:12 segundos de estandarte · una cinta a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                type, damage, knockback, player.whoAmI,
                0f, 0f, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<CintaAuroraProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// ManadaAstralStaff — LA MANADA ASTRAL (v6.38 — PATRÓN 8).
    ///
    /// Los boids de la casa: seis cazadores con distancia ELÁSTICA (se
    /// estiran al acelerar, nunca teletransportan) y LIDERAZGO ROTATIVO
    /// — el mando salta al cuello más cercano a la presa.
    /// </summary>
    public class ManadaAstralStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ManadaAstralProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122.WithPitchOffset(0.62f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/8CC8FF:═══ LA MANADA ASTRAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/B8E0FF:Seis cazadores con distancia ELÁSTICA — se estiran al acelerar y se\ncomprimen en el giro (steering, nunca teletransporte) — y LIDERAZGO ROTATIVO]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFD678:El líder embiste y la CORONA SALTA al cuello más cercano a la presa\n· cada cazador golpea CON SU PROPIA VELOCIDAD: ×0.55 → ×1.4]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:10 segundos de cacería · una manada a la vez · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            MatarVieja(player);
            Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter + dir * 30f, dir * 12f,
                type, damage, knockback, player.whoAmI,
                Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.Next(1000));
            return false;
        }

        private static void MatarVieja(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<ManadaAstralProjectile>())
                    p.Kill();
            }
        }
    }

    /// <summary>
    /// CriaEstelarStaff — LA CRÍA ESTELAR (v6.38 — EL MINION DE LA CAMADA).
    ///
    /// La sierpe estelar hecha sirviente: su hijita — el mismo ADN (la
    /// cadena, el nado tangente, el vaivén) pero cachorra y despierta.
    /// En reposo nada alrededor del dueño; en caza embiste. ES UN
    /// MINION: buff sostenido, espacio de sirvientes, nunca expira.
    /// </summary>
    public class CriaEstelarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Summon;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;    // SIN MANÁ (la regla de la casa)
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item44;    // el sello de invocación de la casa
            Item.shoot = ModContent.ProjectileType<CriaEstelarMinion>();
            Item.shootSpeed = 10f;
            // (Este tML ya no tiene Item.buff — el buff lo aplica Shoot()
            // y la propia cría lo sostiene, patrón de la medusa nebulosa.)
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/E8F4FF:═══ LA CRÍA ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/B4E1FF:La hijita de la sierpe estelar: diez segmentos con el MISMO ADN —\nla cadena, el nado tangente, el vaivén — pero cachorra y despierta]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/82E9FF:En REPOSO nada en círculos a tu alrededor · en CAZA embiste a la presa\n(mordida de contacto) y la espinita quema ×0.4 a su paso]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:MINION: ocupa 1 espacio de sirviente · invoca una camada con más espacio\n· nunca expira · SIN COSTE DE MANÁ]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // La cría nace delante del jugador (el minion sostiene el
            // buff — patrón de la medusa: 2 ticks bastan).
            player.AddBuff(ModContent.BuffType<CriaEstelarBuff>(), 2);
            Vector2 spawnPos = player.MountedCenter +
                velocity.SafeNormalize(Vector2.Zero) * 48f;
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage,
                knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
