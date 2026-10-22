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
    //  v6.51.4 → v6.51.5 — LAS ARMAS DIGNAS DEL GRIMORIO, LA SEGUNDA
    //  AUDICIÓN.
    //
    //  LA LETRA DE LA .51.5: «están feas no me gustan y no tienen
    //  estilo, ni efectos, ni brillos, ni glow, ni nada, las puedes
    //  borrar, en cambio deja el El Equinoccio Binario y modificalo,
    //  para El Equinoccio Binario usa el mismo proyectil que El
    //  Equinoccio, pero dale el efecto que El Equinoccio Binario // y
    //  crea otras armas en consecuencia que creas que se sean buenas
    //  ideas, recuerda meterle tantos efectos chulos como puedas».
    //
    //  LA PURGA: LAS COMAS QUE MUERDEN, LA PÁGINA ARRANCADA, EL OJO
    //  QUE CAZA, LA LÍNEA DE LECTURA y EL MARCAPÁGINAS murieron. Quedan
    //  CINCO — todas con las librerías de la casa a tope:
    //
    //    1 · EL EQUINOCCIO BINARIO (RE-HECHO): la MISMA pareja 931 de
    //        El Equinoccio con el efecto binario — de día el dorado
    //        estalla en MINI ECLIPSE en área; de noche el violeta
    //        PERFORA 4 y DRENA vida.
    //    2 · EL ÍNDICE DEL GRIMORIO — el dedo de hueso que planta LA
    //        CITA y la cita detona en VALLA de rayos de Perlin.
    //    3 · LA LLUVIA DE VERSOS — la grieta de bruma con costura
    //        dorada que llueve versos; al cerrar, EL CAPÍTULO SE
    //        CIERRA con las dos tapas y los dientes de garra.
    //    4 · EL OJO QUE JUZGA — el ojo colosal con velo de cine; su
    //        VEREDICTO: el rayo de la mirada.
    //    5 · EL CORAZÓN DEL MAGNETAR (la mezcla pedida): la órbita del
    //        Bastón (aura + cadenas) y al soltar, EL COLAPSO del padre
    //        (lanzamiento exacto, freno, STARQUAKE).
    //
    //  TODAS sin maná (la regla de las armas de pruebas de la casa),
    //  TODAS con receta de 5 madera (el protocolo v6.14.2) y TODAS en
    //  LA BOLSA DE LOS PROYECTILES DIGNOS.
    // ======================================================================

    // ======================================================================
    //  1 · EL EQUINOCCIO BINARIO — la binaria día/noche, RE-HECHA con
    //  el proyectil de El Equinoccio (la letra: «usa el mismo
    //  proyectil que El Equinoccio, pero dale el efecto que El
    //  Equinoccio Binario»).
    // ======================================================================
    public class DignosEquinoccioBinario : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = 931;   // Nightglow — EL MISMO de El Equinoccio (el número)
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item82;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === LA PAREJA DE EL EQUINOCCIO (el vanilla 931 — IA de
            //     Terraria intacta), con el RELOJ DEL MUNDO decidiendo
            //     quién GUIA: de DÍA el DORADO sale hacia el cursor y el
            //     violeta lo escolta; de NOCHE el VIOLETA guía. ===
            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // EL RUMBO DEL GUÍA: hacia el cursor (la binaria APUNTA).
            Vector2 guia = (Main.MouseWorld - pos).SafeNormalize(-Vector2.UnitY) * 15f;
            Vector2 escolta = guia.RotatedBy((Main.dayTime ? 1f : -1f) * 0.55f) * 0.85f;

            // === EL DORADO — EL DÍA (ai[2]=9211: la marca binaria del
            //     FX — el sol en miniatura + EL MINI ECLIPSE en área). ===
            Projectile.NewProjectile(source, pos, Main.dayTime ? guia : escolta,
                931, damage, knockback, player.whoAmI,
                -1f,     // ai[0]: sin objetivo aún (el homing del 931 decide)
                0.64f,   // ai[1]: EL MATIZ DORADO
                9211f);  // ai[2]: la marca binaria dorada

            // === EL NEGRO — LA NOCHE (ai[2]=9212: la marca binaria — el
            //     eclipse limpio + PERFORA 4 y DRENA vida). ===
            Projectile.NewProjectile(source, pos, Main.dayTime ? escolta : guia,
                931, damage, knockback, player.whoAmI,
                -1f,
                0.28f,   // ai[1]: el matiz violeta
                9212f);  // ai[2]: la marca binaria violeta

            return false;   // los dos spawns son nuestros
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/FFD678:El disparo de El Equinoccio — la pareja dorada y violeta— con el reloj del mundo:]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:DÍA: el dorado guía y su golpe estalla en MINI ECLIPSE en área · NOCHE: el violeta guía, PERFORA 4 y DRENA vida · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  2 · EL ÍNDICE DEL GRIMORIO — el dedo que cita
    // ======================================================================
    public class DignosIndiceGrimorio : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 62;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 26; Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<IndiceGrimorioProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item45;
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
                "[c/E8E2CE:El ÍNDICE del libro: el dedo de hueso vuela y se CLAVA en la presa]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Planta LA CITA (una línea de glifos)… y la cita DETONA: VALLA de CINCO rayos de Perlin caminando la frase · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  3 · LA LLUVIA DE VERSOS — la grieta que llora el capítulo
    // ======================================================================
    public class DignosLluviaVersos : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 75;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LluviaVersosProjectile>();
            Item.shootSpeed = 11f;
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
                "[c/C9A2FF:Abre LA GRIETA de bruma con su costura dorada — y llueve VERSOS que estallan al caer]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Al morder: EL CAPÍTULO SE CIERRA DE GOLPE — las DOS TAPAS y NUEVE DIENTES DE GARRA mordiendo (golpe ×1,5 en área) · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  4 · EL OJO QUE JUZGA — el veredicto de la mirada
    // ======================================================================
    public class DignosOjoQueJuzga : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 44; Item.useAnimation = 44;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OjoQueJuzgaProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL OJO SE INSTALLA donde apuntas (la posición de la
            // disparada — la velocidad solo le da la deriva).
            Vector2 pos = Main.MouseWorld;
            Projectile.NewProjectile(source, pos, velocity.SafeNormalize(-Vector2.UnitY) * 2f,
                type, damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/AEE8FF:El OJO COLOSAL se installa donde apuntas, corre su VELO DE CINE y JUZGA (el tono sube…)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:EL VEREDICTO: el RAYO DE LA MIRADA — atraviesa TODO lo que vive en la línea (×1,35) · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  5 · EL CORAZÓN DEL MAGNETAR — la mezcla pedida: la órbita del
    //  Bastón + el Colapso del padre, en un solo arma.
    // ======================================================================
    public class DignosCorazonMagnetar : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                 // MANTÉN: la órbita vive mientras cliques
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<CorazonMagnetarProjectile>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item122;
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CORAZÓN ÚNICO: si ya te orbita uno, el nuevo disparo lo
            // ENTREGA al colapso (el handbrake de la casa — nunca dos).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.owner != player.whoAmI) continue;
                if (p.type == type && p.ai[0] == 0f) return false;
            }
            Projectile.NewProjectile(source, position, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D1",
                "[c/C9A0F5:MANTÉN el clic: el CORAZÓN te orbita — aura magnética y CADENAS DE RAYO (la vida del Bastón)]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:SUELTA: EL COLAPSO — sale al cursor, frena TEMBLANDO… y EL STARQUAKE (el anillo del padre que golpea con el frente) · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
